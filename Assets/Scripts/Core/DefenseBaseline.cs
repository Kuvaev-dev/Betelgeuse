/// <summary>
/// Fixed protocol for diploma defense Monte-Carlo.
/// Apply via <see cref="ApplyTo"/> before a Comparison pack so results are comparable.
/// </summary>
public static class DefenseBaseline
{
    public const int ProtocolVersion = 4;
    public const int Seed = 42;
    public const int TestsPerAlgorithm = 15;
    // v4: milder wind/jitter so lateral GNC differentiates A–D instead of universal miss
    public const float WindStrength = 5.5f;
    public const float MassVariationPercent = 6f;
    public const float AngleVariationDegrees = 5f;
    public const float PositionJitterMeters = 12f;
    public const bool EnableNoise = true;
    public const bool ContinuousWind = true;
    public const bool HybridResidualOn = false;
    public const float StartHeight = 1600f;
    public const float StartDescentSpeed = 60f;
    public const float StartTiltDeg = 2f;

    /// <summary>
    /// Expected qualitative ranking under protocol (success %):
    /// Hybrid ≥ Fuzzy and Hybrid ≥ PID (paired seeds, same IC/disturbances).
    /// </summary>
    public const string RankingNote =
        "Expected: Hybrid ≥ Fuzzy and Hybrid ≥ PID under DefenseBaseline protocol (seed 42).";

    /// <summary>Push protocol constants into a live SimulationManager.</summary>
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
