/// <summary>
/// Фіксований протокол Monte-Carlo для захисту диплома.
/// Застосовувати через <see cref="ApplyTo"/> перед Comparison-пакетом, щоб результати були порівнянні.
/// </summary>
public static class DefenseBaseline
{
    public const int ProtocolVersion = 14; // docs: RELEASE.md / HOW_TO_RUN.md
    public const int Seed = 42;
    public const int TestsPerAlgorithm = 15;
    // v13: wind = true m/s ambient; Isp SL; moderate fins; gates 3.5/7°/40m/6.5
    public const float WindStrength = 5f;
    public const float MassVariationPercent = 5f;
    public const float AngleVariationDegrees = 4f;
    public const float PositionJitterMeters = 10f;
    public const bool EnableNoise = true;
    public const bool ContinuousWind = true;
    public const bool HybridResidualOn = true;
    public const float StartHeight = 1600f;
    public const float StartDescentSpeed = 60f;
    public const float StartTiltDeg = 2f;

    /// <summary>
    /// Очікуване якісне ранжування за протоколом (success %):
    /// Hybrid ≥ Fuzzy і Hybrid ≥ PID (paired seeds, однакові ПУ/збурення).
    /// </summary>
    public const string RankingNote =
        "Expected: Hybrid ≥ Fuzzy and Hybrid ≥ PID under DefenseBaseline protocol (seed 42).";

    /// <summary>
    /// Опційний пресет захисту (не викликається автоматично на P).
    /// UI: лише якщо користувач явно просить baseline.
    /// </summary>
    public static void ApplyTo(SimulationManager sim)
    {
        if (sim == null) return;
        sim.experimentSeed = Seed;
        sim.testsPerAlgorithm = TestsPerAlgorithm;
        sim.windStrength = WindStrength;
        sim.massVariationPercent = MassVariationPercent;
        sim.angleVariationDegrees = AngleVariationDegrees;
        sim.positionJitterMeters = PositionJitterMeters;
        sim.enableNoise = EnableNoise;
        sim.continuousWind = ContinuousWind;
        sim.includeHybrid = true;
        sim.startHeight = StartHeight;
        sim.startDescentSpeed = StartDescentSpeed;
        sim.startTiltDeg = StartTiltDeg;
    }
}
