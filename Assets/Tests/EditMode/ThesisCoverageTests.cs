using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Перевірка відповідності теми МКР: fuzzy + ML + hybrid + soft-landing + strategy layer.
/// </summary>
public class ThesisCoverageTests
{
    [Test]
    public void SoftLanding_Ideal_MeetsVelocityCriterion()
    {
        Assert.IsTrue(IdealLandingPresets.ProfileGuaranteesSoftLanding(out float v));
        Assert.Less(v, LandingCriteria.DefaultMaxTouchdownVelocity);
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
        var go = new GameObject("ThesisCtrl");
        try
        {
            var fuzzy = go.AddComponent<FuzzyLandingController>();
            var neural = go.AddComponent<NeuralController>();
            var hybrid = go.AddComponent<HybridController>();
            hybrid.fuzzy = fuzzy;
            hybrid.neural = neural;
            neural.InstallIdealWeights();
            neural.enableTraining = false;

            ILandingController[] all =
            {
                new PidLandingStrategy(),
                fuzzy,
                neural,
                hybrid
            };

            var ctx = new ControlContext(
                800f, -40f, 38000f, 400000f, 845000f,
                2f, -1f, 0f, 0f, 5f, 3f, 0.005f,
                Quaternion.identity, Vector3.zero);

            Assert.AreEqual(RocketPhysics.ControlMode.PID, all[0].Mode);
            Assert.AreEqual(RocketPhysics.ControlMode.Fuzzy, all[1].Mode);
            Assert.AreEqual(RocketPhysics.ControlMode.Neural, all[2].Mode);
            Assert.AreEqual(RocketPhysics.ControlMode.Hybrid, all[3].Mode);

            foreach (var c in all)
            {
                Assert.IsTrue(c.IsAvailable, c.DisplayName);
                var cmd = c.Evaluate(in ctx);
                Assert.IsFalse(float.IsNaN(cmd.Thrust), c.DisplayName + " thrust NaN");
                Assert.IsFalse(float.IsInfinity(cmd.Thrust), c.DisplayName + " thrust Inf");
                Assert.Greater(cmd.Thrust, 0f, c.DisplayName);
                Assert.LessOrEqual(Mathf.Abs(cmd.GimbalEuler.x), 25f);
                Assert.LessOrEqual(Mathf.Abs(cmd.GimbalEuler.z), 25f);
            }

            // Hybrid must actually use fuzzy+neural paths (thrust near hover band)
            float hover = 38000f * AtmosphereModel.GetGravity(800f);
            var hCmd = hybrid.Evaluate(in ctx);
            Assert.Greater(hCmd.Thrust, hover * 0.5f);
            Assert.Less(hCmd.Thrust, hover * 3.5f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Resolver_DispatchesByMode()
    {
        var go = new GameObject("ThesisResolver");
        try
        {
            var rp = go.AddComponent<RocketPhysics>();
            var fuzzy = go.AddComponent<FuzzyLandingController>();
            var neural = go.AddComponent<NeuralController>();
            var hybrid = go.AddComponent<HybridController>();
            rp.fuzzyController = fuzzy;
            rp.neuralController = neural;
            rp.hybridController = hybrid;
            hybrid.fuzzy = fuzzy;
            hybrid.neural = neural;

            var pid = new PidLandingStrategy();
            var resolver = LandingControllerResolver.CreateDefault(rp, pid);

            Assert.AreEqual(RocketPhysics.ControlMode.PID, resolver.Resolve(RocketPhysics.ControlMode.PID).Mode);
            Assert.AreEqual(RocketPhysics.ControlMode.Fuzzy, resolver.Resolve(RocketPhysics.ControlMode.Fuzzy).Mode);
            Assert.AreEqual(RocketPhysics.ControlMode.Neural, resolver.Resolve(RocketPhysics.ControlMode.Neural).Mode);
            Assert.AreEqual(RocketPhysics.ControlMode.Hybrid, resolver.Resolve(RocketPhysics.ControlMode.Hybrid).Mode);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Sugeno_RawThrust_DiffersFromPureProfile_WhenAggressive()
    {
        var go = new GameObject("SugenoDiff");
        try
        {
            var fuzzy = go.AddComponent<FuzzyLandingController>();
            fuzzy.fuzzyThrustWeight = 0.7f;
            float mass = 30000f;
            float h = 30f;
            float vy = -45f;
            float profile = SoftLandingGuidance.ProfileThrust(h, vy, mass);
            float sugeno = fuzzy.EvaluateSugenoThrust(h, vy, mass);
            // Aggressive low-alt high-speed: Sugeno table should push above hover-ish profile band
            Assert.Greater(sugeno, mass * AtmosphereModel.GetGravity(h) * 1.1f);
            Assert.Greater(Mathf.Abs(profile - sugeno), 100f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
