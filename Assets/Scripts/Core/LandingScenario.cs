using UnityEngine;

/// <summary>
/// Параметри апарата та початкові умови ділянки посадки 1-го ступеня.
/// Чиста структура — не залежить від сцени Unity (спільна для гри, Monte-Carlo, тестів і навчання).
/// </summary>
public struct LandingSetup
{
    public float DryMass;
    public float FuelMass;
    public float MaxThrust;
    public float Isp;
    public float Dt;
    public float MaxTime;
    public Vector3 StartPosition;
    public Vector3 StartVelocity;
    public Vector3 StartEulerDeg;
    public LandingCriteria.Limits Limits;

    public static LandingSetup Default => FromInitialConditions(1600f, 60f, 2f);

    /// <summary>Номінал Stage1Vehicle з заданими h₀, |Vy₀| і нахилом.</summary>
    public static LandingSetup FromInitialConditions(float startHeight, float descentSpeed, float tiltDeg)
        => new LandingSetup
        {
            DryMass = Stage1Vehicle.DryMassKg,
            FuelMass = Stage1Vehicle.LandingFuelKg,
            MaxThrust = Stage1Vehicle.LandingThrustN,
            Isp = Stage1Vehicle.IspLandingS,
            Dt = 0.005f,
            MaxTime = 400f,
            StartPosition = new Vector3(0f, startHeight, 0f),
            StartVelocity = new Vector3(0f, -Mathf.Abs(descentSpeed), 0f),
            StartEulerDeg = new Vector3(0f, 0f, tiltDeg),
            Limits = LandingCriteria.Limits.Default
        };
}

/// <summary>
/// Налаштування збурень (слайдери UI / протокол / розподіл навчання).
/// </summary>
public struct DisturbanceSettings
{
    /// <summary>Модуль ambient-вітру, м/с.</summary>
    public float WindStrength;
    public bool ContinuousWind;
    /// <summary>± відхилення маси палива, %.</summary>
    public float MassVariationPercent;
    /// <summary>± початкове відхилення осі корпусу по кожній осі, град.</summary>
    public float AngleVariationDeg;
    /// <summary>± зміщення точки входу по X/Z, м.</summary>
    public float PositionJitterMeters;
    /// <summary>Множник шуму сенсорів навігації (0 — ідеальна навігація).</summary>
    public float NavNoiseScale;

    public static DisturbanceSettings None => default;

    /// <summary>Шум сенсорів у режимі «шум увімкнено» (1σ = 0.35 від базових σ IMU/GPS/висотоміра).</summary>
    public const float DefaultNavNoise = 0.35f;
}

/// <summary>
/// Одна конкретна реалізація збурень (trial). Однакова для всіх алгоритмів при однаковому seed
/// (paired Monte-Carlo).
/// </summary>
public struct LandingScenario
{
    public int Seed;
    public Vector3 Wind;
    public bool ContinuousWind;
    public float FuelScale;
    public float TiltXDeg;
    public float TiltZDeg;
    public float OffsetX;
    public float OffsetZ;
    public float NavNoiseScale;

    public static LandingScenario Nominal(int seed = 1)
        => new LandingScenario { Seed = seed, FuelScale = 1f };

    /// <summary>
    /// Детермінована вибірка збурень з seed (власний ГПВЧ — потокобезпечно, без глобального стану).
    /// </summary>
    public static LandingScenario Sample(in DisturbanceSettings s, int seed)
    {
        var rng = new System.Random(seed);
        float U(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        var sc = Nominal(seed);
        sc.NavNoiseScale = Mathf.Max(0f, s.NavNoiseScale);

        float yaw = U(0f, Mathf.PI * 2f);
        float wMag = Mathf.Max(0f, s.WindStrength) * U(0.85f, 1f);
        if (s.WindStrength > 0.05f)
        {
            sc.Wind = new Vector3(Mathf.Cos(yaw) * wMag, 0f, Mathf.Sin(yaw) * wMag);
            sc.ContinuousWind = s.ContinuousWind;
        }

        float massNoise = U(-1f, 1f) * s.MassVariationPercent / 100f;
        sc.FuelScale = 1f + massNoise;

        sc.TiltXDeg = U(-1f, 1f) * s.AngleVariationDeg;
        sc.TiltZDeg = U(-1f, 1f) * s.AngleVariationDeg;

        sc.OffsetX = U(-1f, 1f) * s.PositionJitterMeters;
        sc.OffsetZ = U(-1f, 1f) * s.PositionJitterMeters;
        return sc;
    }
}
