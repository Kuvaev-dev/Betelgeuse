using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Чесність Monte-Carlo: paired seeds, детермінізм, однакове наведення для A–D, ваги не змінюються під час пакета.
/// </summary>
public class MonteCarloFairnessTests
{
    static readonly DisturbanceSettings Protocol = new DisturbanceSettings
    {
        WindStrength = DefenseBaseline.WindStrength,
        ContinuousWind = true,
        MassVariationPercent = DefenseBaseline.MassVariationPercent,
        AngleVariationDeg = DefenseBaseline.AngleVariationDegrees,
        PositionJitterMeters = DefenseBaseline.PositionJitterMeters,
        NavNoiseScale = DisturbanceSettings.DefaultNavNoise
    };

    static LandingSetup Setup => LandingSetup.FromInitialConditions(
        DefenseBaseline.StartHeight, DefenseBaseline.StartDescentSpeed, DefenseBaseline.StartTiltDeg);

    [Test]
    public void TrialSeeds_AreIdenticalForAllAlgorithms()
    {
        for (int i = 0; i < 5; i++)
        {
            var a = LandingScenario.Sample(Protocol, LandingExperiment.TrialSeed(42, i));
            var b = LandingScenario.Sample(Protocol, LandingExperiment.TrialSeed(42, i));
            Assert.AreEqual(a.Wind, b.Wind);
            Assert.AreEqual(a.OffsetX, b.OffsetX);
        }
    }

    [Test]
    public void RunPaired_IsDeterministic_RegardlessOfThreads()
    {
        var set = new ControllerSet();
        var r1 = LandingExperiment.RunPaired(Setup, set, ControlMode.Hybrid, Protocol, 42, 6, maxParallel: 1);
        var r2 = LandingExperiment.RunPaired(Setup, set, ControlMode.Hybrid, Protocol, 42, 6, maxParallel: 4);
        for (int i = 0; i < r1.Length; i++)
        {
            Assert.AreEqual(r1[i].touchdownVelocity, r2[i].touchdownVelocity, $"trial {i}");
            Assert.AreEqual(r1[i].horizontalMiss, r2[i].horizontalMiss, $"trial {i}");
        }
    }

    [Test]
    public void Batch_DoesNotModifyTemplateWeights()
    {
        var set = new ControllerSet();
        var before = set.neural.policy.GetParameters();
        LandingExperiment.RunPaired(Setup, set, ControlMode.Neural, Protocol, 42, 4);
        CollectionAssert.AreEqual(before, set.neural.policy.GetParameters());
    }

    [Test]
    public void BaseStrategies_DoNotChangeSharedLateralGuidance()
    {
        var ctx = ControlContext.FromState(new RocketState
        {
            position = new Vector3(20f, 500f, -10f), velocity = new Vector3(2f, -30f, 1f),
            rotation = Quaternion.identity, dryMass = 25600f, currentFuelMass = 8000f,
            currentThrust = 400000f, maxThrust = Stage1Vehicle.LandingThrustN
        }, 0.005f);
        var set = new ControllerSet();
        Assert.AreEqual(1f, set.Get(ControlMode.PID).Evaluate(ctx).LateralScale);
        Assert.AreEqual(1f, set.Get(ControlMode.Fuzzy).Evaluate(ctx).LateralScale);
        // Нейтральна (ненавчена) мережа не змінює спільне наведення
        Assert.AreEqual(1f, set.Get(ControlMode.Neural).Evaluate(ctx).LateralScale, 1e-6f);
    }

    [Test]
    public void ProtocolConditions_AllAlgorithmsMostlySucceed()
    {
        var set = new ControllerSet();
        foreach (ControlMode mode in System.Enum.GetValues(typeof(ControlMode)))
        {
            var r = LandingExperiment.RunPaired(Setup, set, mode, Protocol, DefenseBaseline.Seed, 10);
            Assert.GreaterOrEqual(LandingExperiment.SuccessRate(r), 0.8f, mode.ToString());
        }
    }
}
