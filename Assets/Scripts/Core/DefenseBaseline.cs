/// <summary>
/// Фіксований протокол Monte-Carlo для захисту (кнопка / клавіша B → слайдери, далі P).
/// Умови навмисно жорсткі (сильний вітер + розкид + шум навігації), щоб різниця між A–D була видна;
/// повна залежність від вітру — Results/robustness_wind.csv (Tools/HeadlessSim run.cmd robustness).
/// </summary>
public static class DefenseBaseline
{
    /// <summary>v15: чисте ядро LandingSimulation, каскадне наведення, MLP навчені ES(1+λ) офлайн.</summary>
    public const int ProtocolVersion = 15;
    public const int Seed = 42;
    public const int TestsPerAlgorithm = 100;
    public const float WindStrength = 15f;
    public const float MassVariationPercent = 8f;
    public const float AngleVariationDegrees = 8f;
    public const float PositionJitterMeters = 25f;
    public const bool EnableNoise = true;
    public const bool ContinuousWind = true;
    public const bool HybridResidualOn = true;
    public const float StartHeight = 1800f;
    public const float StartDescentSpeed = 72f;
    public const float StartTiltDeg = 3.5f;

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
