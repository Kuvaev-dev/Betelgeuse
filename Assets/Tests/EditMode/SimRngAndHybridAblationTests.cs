using NUnit.Framework;
using UnityEngine;

/// <summary>Відтворюваність ГПВЧ і сценаріїв + ablation Hybrid residual (D без MLP ≡ B).</summary>
public class SimRngAndHybridAblationTests
{
    [Test]
    public void SimRng_SameSeed_SameSequence()
    {
        SimRng.Reseed(42);
        float a0 = SimRng.Range(-10f, 10f);
        int a1 = SimRng.Range(0, 100);
        SimRng.Reseed(42);
        Assert.AreEqual(a0, SimRng.Range(-10f, 10f), 1e-6f);
        Assert.AreEqual(a1, SimRng.Range(0, 100));
    }

    [Test]
    public void Scenario_SameSeed_Identical_DifferentSeed_Differs()
    {
        var d = new DisturbanceSettings { WindStrength = 8f, ContinuousWind = true, MassVariationPercent = 5f,
            AngleVariationDeg = 4f, PositionJitterMeters = 15f, NavNoiseScale = 0.35f };
        var a = LandingScenario.Sample(d, 123);
        var b = LandingScenario.Sample(d, 123);
        var c = LandingScenario.Sample(d, 124);
        Assert.AreEqual(a.Wind.x, b.Wind.x);
        Assert.AreEqual(a.OffsetZ, b.OffsetZ);
        Assert.AreEqual(a.TiltXDeg, b.TiltXDeg);
        Assert.AreNotEqual(a.Wind.x, c.Wind.x);
    }

    [Test]
    public void HybridWithoutResidual_EqualsFuzzyExactly()
    {
        var set = new ControllerSet();
        set.hybrid.useNeuralResidual = false;
        var setup = LandingSetup.FromInitialConditions(1600f, 60f, 2f);
        var d = new DisturbanceSettings { WindStrength = 6f, ContinuousWind = true, MassVariationPercent = 5f,
            AngleVariationDeg = 4f, PositionJitterMeters = 10f, NavNoiseScale = 0.35f };
        var sc = LandingScenario.Sample(d, 7);
        var mf = LandingExperiment.RunTrial(setup, set.Get(ControlMode.Fuzzy), sc);
        var mh = LandingExperiment.RunTrial(setup, set.Get(ControlMode.Hybrid), sc);
        Assert.AreEqual(mf.touchdownVelocity, mh.touchdownVelocity);
        Assert.AreEqual(mf.horizontalMiss, mh.horizontalMiss);
        Assert.AreEqual(mf.totalFlightTime, mh.totalFlightTime);
    }
}
