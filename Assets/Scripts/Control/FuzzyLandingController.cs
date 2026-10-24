using UnityEngine;

/// <summary>
/// Режим B — нечіткий регулятор Sugeno нульового порядку (дві бази правил 5×5, t-норма добутку,
/// дефазифікація зваженим середнім).
/// • Тяга: входи — висота h і швидкість зниження |Vy|; виходи — множники до mg;
///   результат змішується з профілем soft-landing (вага <see cref="fuzzyThrustWeight"/>).
/// • Орієнтація (TVC): входи — помилка кута e і кутова швидкість ė (зі знаком) по кожній осі;
///   виходи — нормовані кути TVC (нечіткий PD із насиченням).
/// </summary>
[System.Serializable]
public sealed class FuzzyLandingController : ILandingController
{
    public ControlMode Mode => ControlMode.Fuzzy;
    public string DisplayName => "Fuzzy Sugeno";

    [Header("Тяга (h × |Vy|)")]
    public float heightScale = 2800f;
    public float velocityScale = 110f;
    [Range(0f, 1f)] public float fuzzyThrustWeight = 0.3f;
    [Range(0.1f, 1f)] public float maxDevFrac = 0.3f;

    [Header("Орієнтація (e × ė)")]
    /// <summary>Помилка кута, що відповідає терму PB (центр PS = половина), град.</summary>
    public float errorScaleDeg = 10f;
    /// <summary>Кутова швидкість, що відповідає терму PB, град/с.</summary>
    public float rateScaleDeg = 5f;
    public float maxGimbalDeg = 12f;

    // Рядки — висота VL..VH, стовпці — |Vy| VS..VF; множники до mg.
    static readonly float[,] ThrustTable =
    {
        { 1.12f, 1.45f, 1.95f, 2.40f, 2.75f },
        { 1.10f, 1.38f, 1.80f, 2.25f, 2.60f },
        { 1.05f, 1.28f, 1.55f, 1.95f, 2.35f },
        { 0.98f, 1.15f, 1.38f, 1.70f, 2.10f },
        { 0.92f, 1.05f, 1.22f, 1.50f, 1.90f }
    };

    // Рядки — e: NB NS ZE PS PB; стовпці — ė: NB NS ZE PS PB; вихід у частках maxGimbalDeg.
    static readonly float[,] AttitudeTable =
    {
        { -1.00f, -1.00f, -0.67f, -0.33f,  0.00f },
        { -1.00f, -0.67f, -0.33f,  0.00f,  0.33f },
        { -0.67f, -0.33f,  0.00f,  0.33f,  0.67f },
        { -0.33f,  0.00f,  0.33f,  0.67f,  1.00f },
        {  0.00f,  0.33f,  0.67f,  1.00f,  1.00f }
    };

    public void ResetSession() { /* без внутрішнього стану */ }

    public ControlCommand Evaluate(in ControlContext ctx)
    {
        float thrust = CalculateThrust(ctx.Height, ctx.VerticalVelocity, ctx.Mass);
        Vector3 gimbal = CalculateGimbal(ctx.PitchErrorDeg, ctx.YawErrorDeg, ctx.PitchRateDeg, ctx.YawRateDeg);
        return new ControlCommand(thrust, gimbal);
    }

    /// <summary>Тяга режиму B: профіль ⊕ Sugeno (вага fuzzyThrustWeight).</summary>
    public float CalculateThrust(float height, float verticalVelocity, float mass)
    {
        float profile = SoftLandingGuidance.ProfileThrust(height, verticalVelocity, mass);
        float fuzzy = EvaluateSugenoThrust(height, verticalVelocity, mass);
        return SoftLandingGuidance.BlendThrust(profile, fuzzy, fuzzyThrustWeight, mass, height, maxDevFrac);
    }

    /// <summary>«Сирий» вихід бази правил тяги, Н.</summary>
    public float EvaluateSugenoThrust(float height, float verticalVelocity, float mass)
    {
        float g = AtmosphereModel.GetGravity(Mathf.Max(0f, height));
        float h = Mathf.Clamp01(height / Mathf.Max(1f, heightScale));
        float v = Mathf.Clamp01(Mathf.Abs(Mathf.Min(0f, verticalVelocity)) / Mathf.Max(1f, velocityScale));
        float mult = Infer(ThrustTable, Membership5(h, 0f, 0.12f, 0.28f, 0.50f, 0.72f, 1f),
                                         Membership5(v, 0f, 0.15f, 0.35f, 0.55f, 0.78f, 1f), 1.15f);
        return mass * g * Mathf.Clamp(mult, 0.85f, 2.9f);
    }

    /// <summary>Кути TVC за нечіткою базою правил орієнтації, град.</summary>
    public Vector3 CalculateGimbal(float errXDeg, float errZDeg, float rateXDeg, float rateZDeg)
        => new Vector3(FuzzyAxis(errXDeg, rateXDeg), 0f, FuzzyAxis(errZDeg, rateZDeg));

    float FuzzyAxis(float errorDeg, float rateDeg)
    {
        float e = Mathf.Clamp(errorDeg / Mathf.Max(0.1f, errorScaleDeg), -1f, 1f);
        float r = Mathf.Clamp(rateDeg / Mathf.Max(0.1f, rateScaleDeg), -1f, 1f);
        float u = Infer(AttitudeTable, SignedMembership5(e), SignedMembership5(r), 0f);
        return Mathf.Clamp(u, -1f, 1f) * maxGimbalDeg;
    }

    /// <summary>Sugeno-0: Σ μᵢ(a)·μⱼ(b)·yᵢⱼ / Σ μᵢ(a)·μⱼ(b).</summary>
    static float Infer(float[,] table, float[] muA, float[] muB, float fallback)
    {
        float sumW = 0f, sumY = 0f;
        for (int i = 0; i < 5; i++)
        {
            if (muA[i] <= 0f) continue;
            for (int j = 0; j < 5; j++)
            {
                if (muB[j] <= 0f) continue;
                float w = muA[i] * muB[j];
                sumW += w;
                sumY += w * table[i, j];
            }
        }
        return sumW > 1e-6f ? sumY / sumW : fallback;
    }

    /// <summary>П’ять термів на [−1;1]: NB, NS, ZE, PS, PB (трикутні, крайні — трапеції).</summary>
    public static float[] SignedMembership5(float x)
    {
        return new[]
        {
            Trap(x, -1.01f, -1f, -1f, -0.5f),
            Tri(x, -1f, -0.5f, 0f),
            Tri(x, -0.5f, 0f, 0.5f),
            Tri(x, 0f, 0.5f, 1f),
            Trap(x, 0.5f, 1f, 1f, 1.01f)
        };
    }

    /// <summary>П’ять термів на [0;1] з центрами p1..p4 (тяга: h і |Vy|).</summary>
    public static float[] Membership5(float x, float p0, float p1, float p2, float p3, float p4, float p5)
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
        return x < b ? (x - a) / Mathf.Max(1e-6f, b - a) : (c - x) / Mathf.Max(1e-6f, c - b);
    }

    static float Trap(float x, float a, float b, float c, float d)
    {
        if (x <= a || x >= d) return 0f;
        if (x >= b && x <= c) return 1f;
        if (x < b) return (x - a) / Mathf.Max(1e-6f, b - a);
        return (d - x) / Mathf.Max(1e-6f, d - c);
    }

    public FuzzyLandingController Clone() => (FuzzyLandingController)MemberwiseClone();
}
