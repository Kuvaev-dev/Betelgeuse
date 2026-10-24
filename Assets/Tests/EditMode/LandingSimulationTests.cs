using NUnit.Framework;
using UnityEngine;

/// <summary>Ядро <see cref="LandingSimulation"/>: номінальна посадка A–D, детермінізм, timeout, витрата палива.</summary>
public class LandingSimulationTests
{
    static LandingSetup Nominal => LandingSetup.FromInitialConditions(
        IdealLandingPresets.StartHeight, IdealLandingPresets.StartDescentSpeed, IdealLandingPresets.StartTiltDeg);

    [TestCase(ControlMode.PID)]
    [TestCase(ControlMode.Fuzzy)]
    [TestCase(ControlMode.Neural)]
    [TestCase(ControlMode.Hybrid)]
    public void NominalConditions_SoftLanding(ControlMode mode)
    {
        var m = LandingExperiment.RunTrial(Nominal, new ControllerSet().Get(mode), LandingScenario.Nominal());
        Assert.IsFalse(m.timedOut);
        Assert.IsTrue(m.isSuccessfulLanding,
            $"{mode}: Vy={m.touchdownVelocity:F2} tilt={m.landingAngleError:F2} miss={m.horizontalMiss:F1} Vh={m.horizontalSpeed:F2}");
    }

    [Test]
    public void SameScenario_SameResult()
    {
        var set = new ControllerSet();
        var sc = LandingScenario.Sample(new DisturbanceSettings { WindStrength = 7f, ContinuousWind = true,
            AngleVariationDeg = 3f, PositionJitterMeters = 12f, NavNoiseScale = 0.35f }, 99);
        var a = LandingExperiment.RunTrial(Nominal, set.Get(ControlMode.PID), sc);
        var b = LandingExperiment.RunTrial(Nominal, set.Get(ControlMode.PID), sc);
        Assert.AreEqual(a.touchdownVelocity, b.touchdownVelocity);
        Assert.AreEqual(a.horizontalMiss, b.horizontalMiss);
    }

    [Test]
    public void ShortMaxTime_TimesOutAndFails()
    {
        var setup = Nominal;
        setup.MaxTime = 2f;
        var m = LandingExperiment.RunTrial(setup, new ControllerSet().Get(ControlMode.Fuzzy), LandingScenario.Nominal());
        Assert.IsTrue(m.timedOut);
        Assert.IsFalse(m.isSuccessfulLanding);
        Assert.AreEqual(0f, m.SuccessScore);
    }

    [Test]
    public void Landing_BurnsFuel_AndEndsOnPad()
    {
        var sim = new LandingSimulation { Controller = new ControllerSet().Get(ControlMode.Hybrid) };
        sim.Reset(Nominal, LandingScenario.Nominal());
        var m = sim.RunToEnd();
        Assert.Less(m.fuelRemaining, Nominal.FuelMass);
        Assert.Greater(m.fuelRemaining, 0f);
        Assert.AreEqual(LandingSimulation.PadSurfaceY, sim.State.position.y, 1e-4f);
    }

    [Test]
    public void ThrustDirection_IsRateLimited()
    {
        var sim = new LandingSimulation { Controller = new ControllerSet().Get(ControlMode.PID) };
        var sc = LandingScenario.Nominal();
        sc.TiltXDeg = 20f;
        sim.Reset(Nominal, sc);
        Vector3 prev = sim.State.thrustDirection;
        for (int i = 0; i < 200; i++)
        {
            sim.Step();
            Assert.LessOrEqual(Vector3.Angle(prev, sim.State.thrustDirection), 12f * 0.005f + 0.05f);
            prev = sim.State.thrustDirection;
        }
    }
}
