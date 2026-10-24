using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Перевірка відповідності теми МКР: fuzzy + ML + hybrid + soft-landing + strategy layer.
/// </summary>
public class ThesisCoverageTests
{
    [Test]
    public void Stage1Vehicle_MatchesThesisObject()
    {
        Assert.AreEqual(25600f, Stage1Vehicle.DryMassKg);
        Assert.AreEqual(3.66f, Stage1Vehicle.DiameterM, 1e-3f);
        Assert.AreEqual(9, Stage1Vehicle.EngineCount);
        Assert.AreEqual(1, Stage1Vehicle.LandingEngineCount);
        Assert.AreEqual(4, Stage1Vehicle.GridFinCount);
        Assert.AreEqual(4, Stage1Vehicle.LandingLegCount);
        Assert.AreEqual(282f, Stage1Vehicle.IspLandingS, 0.1f);
        StringAssert.Contains("first stage", Stage1Vehicle.AnalogueName.ToLowerInvariant());
    }

    [Test]
    public void HelpLocale_CoversCommitteeQuestions()
    {
        foreach (var key in new[]
        {
            "help_topic", "help_gnc", "help_algo", "help_limits", "help_defense",
            "explain_a", "explain_b", "explain_c", "explain_d"
        })
        {
            string uk = UILocale.T(key);
            Assert.AreNotEqual(key, uk, key);
            Assert.Greater(uk.Length, 20, key);
        }
        StringAssert.Contains("Sugeno", UILocale.T("help_algo"));
        StringAssert.Contains("MLP", UILocale.T("help_algo"));
        StringAssert.Contains("Hybrid", UILocale.T("help_algo"));
        StringAssert.Contains("ES(1+λ)", UILocale.T("help_algo"));
        StringAssert.Contains("ANFIS", UILocale.T("help_limits"));
        StringAssert.Contains("3.5", UILocale.T("help_landing"));
        StringAssert.Contains("40", UILocale.T("help_landing"));
        StringAssert.Contains("6.5", UILocale.T("help_landing"));
    }

    [Test]
    public void SoftLanding_Ideal_MeetsVelocityCriterion()
    {
        Assert.IsTrue(IdealLandingPresets.ProfileGuaranteesSoftLanding(out float v));
        Assert.Less(v, LandingCriteria.DefaultMaxTouchdownVelocity);
    }

    [Test]
    public void EarthAtmosphere_GravityNearNinePointEight()
    {
        float g0 = AtmosphereModel.GetGravity(0f);
        Assert.AreEqual(9.80665f, g0, 0.01f);
        float g1 = AtmosphereModel.GetGravity(1600f);
        Assert.Greater(g1, 9.7f);
        Assert.Less(g1, 9.81f);
        Assert.Greater(AtmosphereModel.GetDensity(0f), 1.0f);
    }

    [Test]
    public void SimulationParameters_ToSetup_UsesStage1Vehicle()
    {
        var p = ScriptableObject.CreateInstance<SimulationParameters>();
        p.ApplyStage1Vehicle();
        p.SetInitialConditions(1700f, 65f, 3f);
        var s = p.ToSetup();
        Assert.AreEqual(Stage1Vehicle.DryMassKg, s.DryMass);
        Assert.AreEqual(Stage1Vehicle.LandingThrustN, s.MaxThrust);
        Assert.AreEqual(1700f, s.StartPosition.y);
        Assert.AreEqual(-65f, s.StartVelocity.y);
        Object.DestroyImmediate(p);
    }

    [Test]
    public void LandingCriteria_RejectsTimeoutAndHardTouchdown()
    {
        var lim = new LandingCriteria.Limits(3.5f, 7f, 25f, 5f);
        var bad = new LandingMetrics
        {
            timedOut = true,
            touchdownVelocity = 1f,
            landingAngleError = 1f,
            horizontalMiss = 1f,
            horizontalSpeed = 1f
        };
        Assert.IsFalse(LandingCriteria.IsSuccessful(bad, lim));

        var hard = new LandingMetrics
        {
            timedOut = false,
            touchdownVelocity = 5f,
            landingAngleError = 1f,
            horizontalMiss = 1f,
            horizontalSpeed = 1f
        };
        Assert.IsFalse(LandingCriteria.IsSuccessful(hard, lim));

        var ok = new LandingMetrics
        {
            timedOut = false,
            touchdownVelocity = 2f,
            landingAngleError = 3f,
            horizontalMiss = 10f,
            horizontalSpeed = 2f
        };
        Assert.IsTrue(LandingCriteria.IsSuccessful(ok, lim));
    }

    [Test]
    public void Controllers_ImplementStrategy_AndReturnFiniteCommands()
    {
        var set = new ControllerSet();
        var ctx = ControlContext.FromState(new RocketState
        {
            position = new Vector3(15f, 700f, -8f), velocity = new Vector3(1f, -40f, 0.5f),
            rotation = Quaternion.Euler(3f, 0f, -2f), angularVelocity = new Vector3(0.01f, 0f, 0.02f),
            dryMass = Stage1Vehicle.DryMassKg, currentFuelMass = 9000f,
            currentThrust = 420000f, maxThrust = Stage1Vehicle.LandingThrustN
        }, 0.005f);
        foreach (ControlMode mode in System.Enum.GetValues(typeof(ControlMode)))
        {
            var c = set.Get(mode);
            Assert.IsNotNull(c, mode.ToString());
            Assert.AreEqual(mode, c.Mode);
            var cmd = c.Evaluate(ctx);
            Assert.IsFalse(float.IsNaN(cmd.Thrust) || float.IsNaN(cmd.GimbalEuler.x) || float.IsNaN(cmd.GimbalEuler.z), mode.ToString());
            Assert.Greater(cmd.Thrust, 0f, mode.ToString());
            Assert.Greater(cmd.LateralScale, 0f, mode.ToString());
        }
    }

    [Test]
    public void ControllerSet_Clone_IsIndependent()
    {
        var a = new ControllerSet();
        var b = a.Clone();
        Assert.AreNotSame(a.neural.policy, b.neural.policy);
        Assert.AreNotSame(a.pid.Thrust, b.pid.Thrust);
        Assert.AreSame(b.fuzzy, b.hybrid.fuzzy);
    }
}
