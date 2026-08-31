/// <summary>
/// Фіксований протокол Monte-Carlo для захисту диплома.
/// Застосовувати через <see cref="ApplyTo"/> перед Comparison-пакетом, щоб результати були порівнянні.
/// </summary>
public static class DefenseBaseline
{
    public const int ProtocolVersion = 5;
    public const int Seed = 42;
    public const int TestsPerAlgorithm = 15;
    // v4: м’якший wind/jitter, щоб lateral GNC диференціював A–D замість універсального промаху
    public const float WindStrength = 5.5f;
    public const float MassVariationPercent = 6f;
    public const float AngleVariationDegrees = 5f;
    public const float PositionJitterMeters = 12f;
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

    /// <summary>Записати константи протоколу в живий SimulationManager.</summary>
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
