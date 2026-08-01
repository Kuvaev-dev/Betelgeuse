using UnityEngine;

/// <summary>
/// Нечіткий контролер посадки: zero-order Sugeno (TSK-0), не Mamdani.
/// Канали: тяга (фазифікація висоти × |Vy|) та gimbal (|кут| × |ω|).
/// AND = product; дефазифікація = зважене середнє чітких консеквентів таблиці 5×5.
/// Біля землі (&lt;25 м) — м'яка корекція під soft-landing профіль.
/// </summary>
public class FuzzyLandingController : MonoBehaviour
{
    [Header("Fuzzy Logic (Sugeno 0-order)")]
    public bool isActive = true;

    [Header("Межі фазифікації")]
    public float heightScale = 3000f;
    public float velocityScale = 120f;
    public float maxGimbalDeg = 28f;

    // Консеквенти тяги (множник до mg) — 5×5 правило
    // Рядки: VL, L, M, H, VH висота; стовпці: VS, S, M, F, VF швидкість спуску
    static readonly float[,] ThrustTable =
    {
        { 1.05f, 1.35f, 1.90f, 2.45f, 2.85f }, // Very Low
        { 1.08f, 1.40f, 1.85f, 2.30f, 2.70f }, // Low
        { 1.02f, 1.25f, 1.55f, 2.00f, 2.40f }, // Medium
        { 0.95f, 1.10f, 1.35f, 1.70f, 2.10f }, // High
        { 0.88f, 0.98f, 1.15f, 1.45f, 1.85f }  // Very High
    };

    // Консеквенти gimbal (градуси абсолютної корекції)
    static readonly float[,] GimbalTable =
    {
        { 0f,  4f, 10f, 18f, 26f }, // мала помилка кута
        { 2f,  8f, 14f, 22f, 28f },
        { 6f, 12f, 18f, 24f, 30f },
        { 10f, 16f, 22f, 28f, 32f },
        { 14f, 20f, 26f, 30f, 34f } // велика помилка
    };

    public float CalculateThrust(float height, float verticalVelocity, float mass)
    {
        float g = AtmosphereModel.GetGravity(Mathf.Max(0f, height));
        if (!isActive) return mass * g * 1.1f;

        float h = Mathf.Clamp01(height / heightScale);
        // 0 = майже зависання, 1 = номінальний спуск, >1 небезпечно швидко
        float v = Mathf.Clamp(Mathf.Abs(Mathf.Min(0f, verticalVelocity)) / velocityScale, 0f, 1.5f);

        float[] muH = Membership5(h, 0f, 0.12f, 0.28f, 0.50f, 0.72f, 1f);
        float[] muV = Membership5(Mathf.Clamp01(v), 0f, 0.15f, 0.35f, 0.55f, 0.78f, 1f);

        float sumW = 0f, sumY = 0f;
        for (int i = 0; i < 5; i++)
        {
            if (muH[i] <= 0f) continue;
            for (int j = 0; j < 5; j++)
            {
                if (muV[j] <= 0f) continue;
                float w = muH[i] * muV[j];
                sumW += w;
                sumY += w * ThrustTable[i, j];
            }
        }

        float mult = sumW > 1e-6f ? sumY / sumW : 1.1f;

        // М'який soft-landing профіль біля землі
        if (height < 25f)
        {
            float targetVy = height < 6f ? -1.2f : -Mathf.Sqrt(2f * 1.4f * height);
            float err = targetVy - verticalVelocity; // >0 якщо падаємо швидше за профіль
            mult += Mathf.Clamp(err * 0.04f, -0.15f, 0.55f);
        }

        mult = Mathf.Clamp(mult, 0.75f, 2.95f);
        return Mathf.Min(mass * g * mult, mass * g * 2.95f);
    }

    /// <summary>
    /// Нечіткий gimbal: фазифікація |pitch/yaw error| та |angular rate proxy|.
    /// </summary>
    public Vector3 CalculateGimbal(float pitchErrorDeg, float yawErrorDeg, float pitchRateDeg = 0f, float yawRateDeg = 0f)
    {
        if (!isActive) return Vector3.zero;

        float pitch = FuzzyAxis(pitchErrorDeg, pitchRateDeg);
        float yaw = FuzzyAxis(yawErrorDeg, yawRateDeg);
        return new Vector3(
            Mathf.Clamp(pitch, -maxGimbalDeg, maxGimbalDeg),
            0f,
            Mathf.Clamp(yaw, -maxGimbalDeg, maxGimbalDeg));
    }

    // Зворотна сумісність зі старим API
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
        return -Mathf.Sign(errorDeg) * mag;
    }

    /// <summary>
    /// 5 трикутних/трапецієподібних MF на [0,1]: NB-NS-Z-PS-PB стиль для скаляра.
    /// centers: c0..c4 на відрізку [lo, hi] через p0..p5.
    /// </summary>
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
        if (x == b) return 1f;
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
