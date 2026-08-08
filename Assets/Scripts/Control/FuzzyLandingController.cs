using UnityEngine;

/// <summary>
/// Zero-order Sugeno (TSK-0) — нечітке керування посадкою.
/// Фазифікація 5 MF (h, |Vy|); база 5×5 product-AND; дефазифікація = зважене середнє.
/// Окремий fuzzy-канал gimbal (|error|×|rate|).
/// За замовчуванням smartWeight високий — алгоритм помітно відрізняється від PID.
/// </summary>
public class FuzzyLandingController : MonoBehaviour
{
    [Header("Fuzzy Logic (Sugeno 0-order)")]
    public bool isActive = true;

    [Header("Межі фазифікації")]
    public float heightScale = 2800f;
    public float velocityScale = 110f;
    public float maxGimbalDeg = 18f;
    /// <summary>Вага Sugeno vs soft-landing профіль (0=тільки профіль, 1=тільки таблиця).</summary>
    [Range(0.1f, 0.85f)] public float fuzzyThrustWeight = 0.55f;
    /// <summary>Макс. відхилення від профілю (частка mg).</summary>
    [Range(0.2f, 1.0f)] public float maxDevFrac = 0.55f;
    [Range(0f, 1f)] public float gimbalBlend = 0.5f;

    // Множники до mg — рядки VL..VH висота; стовпці VS..VF |Vy|
    static readonly float[,] ThrustTable =
    {
        { 1.12f, 1.45f, 1.95f, 2.40f, 2.75f },
        { 1.10f, 1.38f, 1.80f, 2.25f, 2.60f },
        { 1.05f, 1.28f, 1.55f, 1.95f, 2.35f },
        { 0.98f, 1.15f, 1.38f, 1.70f, 2.10f },
        { 0.92f, 1.05f, 1.22f, 1.50f, 1.90f }
    };

    static readonly float[,] GimbalTable =
    {
        { 0f,  4f,  9f, 14f, 18f },
        { 2f,  7f, 12f, 16f, 20f },
        { 5f, 10f, 15f, 18f, 22f },
        { 8f, 13f, 17f, 20f, 24f },
        { 11f, 16f, 19f, 22f, 26f }
    };

    public float CalculateThrust(float height, float verticalVelocity, float mass)
    {
        float profile = SoftLandingGuidance.ProfileThrust(height, verticalVelocity, mass);
        if (!isActive) return profile;

        float fuzzyThrust = EvaluateSugenoThrust(height, verticalVelocity, mass);
        return SoftLandingGuidance.BlendThrust(profile, fuzzyThrust, fuzzyThrustWeight, mass, height, maxDevFrac);
    }

    /// <summary>
    /// «Сирий» вихід Sugeno (без blend з soft-landing).
    /// Hybrid викликає саме його, щоб не робити подвійний BlendThrust.
    /// </summary>
    public float EvaluateSugenoThrust(float height, float verticalVelocity, float mass)
    {
        float g = AtmosphereModel.GetGravity(Mathf.Max(0f, height));
        float h = Mathf.Clamp01(height / Mathf.Max(1f, heightScale));
        float v = Mathf.Clamp(Mathf.Abs(Mathf.Min(0f, verticalVelocity)) / Mathf.Max(1f, velocityScale), 0f, 1.5f);

        float[] muH = Membership5(h, 0f, 0.12f, 0.28f, 0.50f, 0.72f, 1f);
        float[] muV = Membership5(Mathf.Clamp01(v), 0f, 0.15f, 0.35f, 0.55f, 0.78f, 1f);

        float sumW = 0f, sumY = 0f;
        for (int i = 0; i < 5; i++)
        {
            if (muH[i] <= 0f) continue;
            for (int j = 0; j < 5; j++)
            {
                if (muV[j] <= 0f) continue;
                float w = muH[i] * muV[j]; // product t-norm
                sumW += w;
                sumY += w * ThrustTable[i, j];
            }
        }

        float mult = sumW > 1e-6f ? sumY / sumW : 1.15f;
        return mass * g * Mathf.Clamp(mult, 0.85f, 2.9f);
    }

    public Vector3 CalculateGimbal(float pitchErrorDeg, float yawErrorDeg, float pitchRateDeg = 0f, float yawRateDeg = 0f)
    {
        Vector3 pd = SoftLandingGuidance.AttitudeGimbal(pitchErrorDeg, yawErrorDeg, pitchRateDeg, yawRateDeg, maxGimbalDeg);
        if (!isActive) return pd;

        float pitch = FuzzyAxis(pitchErrorDeg, pitchRateDeg);
        float yaw = FuzzyAxis(yawErrorDeg, yawRateDeg);
        float b = Mathf.Clamp01(gimbalBlend);
        return new Vector3(
            Mathf.Clamp(Mathf.Lerp(pd.x, pitch, b), -maxGimbalDeg, maxGimbalDeg),
            0f,
            Mathf.Clamp(Mathf.Lerp(pd.z, yaw, b), -maxGimbalDeg, maxGimbalDeg));
    }

    public Vector3 CalculateGimbal(float pitchError, float yawError)
        => CalculateGimbal(pitchError, yawError, 0f, 0f);

    float FuzzyAxis(float errorDeg, float rateDeg)
    {
        float e = Mathf.Clamp01(Mathf.Abs(errorDeg) / 35f);
        float r = Mathf.Clamp01(Mathf.Abs(rateDeg) / 40f);
        float[] muE = Membership5(e, 0f, 0.12f, 0.30f, 0.50f, 0.72f, 1f);
        float[] muR = Membership5(r, 0f, 0.15f, 0.35f, 0.55f, 0.75f, 1f);

        float sumW = 0f, sumY = 0f;
        for (int i = 0; i < 5; i++)
        {
            if (muE[i] <= 0f) continue;
            for (int j = 0; j < 5; j++)
            {
                if (muR[j] <= 0f) continue;
                float w = muE[i] * muR[j];
                sumW += w;
                sumY += w * GimbalTable[i, j];
            }
        }

        float mag = sumW > 1e-6f ? sumY / sumW : 0f;
        float rateDamp = Mathf.Clamp(rateDeg * 0.28f, -10f, 10f);
        // Негативний FB: проти помилки кута
        return -Mathf.Sign(errorDeg) * mag - rateDamp;
    }

    static float[] Membership5(float x, float p0, float p1, float p2, float p3, float p4, float p5)
    {
        return new[]
        {
            Trap(x, p0 - 0.01f, p0, p1, p2),
            Tri(x, p0, p1, p3),
            Tri(x, p1, p2, p4),
            Tri(x, p2, p3, p5),
            Trap(x, p3, p4, p5, p5 + 0.01f)
        };
    }

    static float Tri(float x, float a, float b, float c)
    {
        if (x <= a || x >= c) return 0f;
        if (Mathf.Approximately(x, b)) return 1f;
        return x < b ? (x - a) / Mathf.Max(1e-6f, b - a) : (c - x) / Mathf.Max(1e-6f, c - b);
    }

    static float Trap(float x, float a, float b, float c, float d)
    {
        if (x <= a || x >= d) return 0f;
        if (x >= b && x <= c) return 1f;
        if (x < b) return (x - a) / Mathf.Max(1e-6f, b - a);
        return (d - x) / Mathf.Max(1e-6f, d - c);
    }
}
