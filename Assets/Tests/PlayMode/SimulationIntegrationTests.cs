using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Інтеграційні тести: фізика + контролери + логер у Play Mode.
/// </summary>
public class SimulationIntegrationTests
{
    SimulationParameters CreateParams()
    {
        var p = ScriptableObject.CreateInstance<SimulationParameters>();
        p.startPosition = new Vector3(0f, 400f, 0f);
        p.startVelocity = new Vector3(0f, -40f, 0f);
        p.startEulerAngles = new Vector3(0f, 0f, 2f);
        p.dryMass = 25600f;
        p.fuelMass = 14000f;
        p.maxThrust = 845000f;
        p.isp = 311f;
        p.fixedTimeStep = 0.01f;
        p.maxSimulationTime = 120f;
        p.maxTouchdownVelocity = LandingCriteria.DefaultMaxTouchdownVelocity;
        p.maxLandingAngle = LandingCriteria.DefaultMaxLandingAngle;
        p.maxHorizontalMiss = LandingCriteria.DefaultMaxHorizontalMiss;
        p.maxHorizontalSpeed = LandingCriteria.DefaultMaxHorizontalSpeed;
        return p;
    }

    GameObject BuildRocket(SimulationParameters p, RocketPhysics.ControlMode mode)
    {
        var go = new GameObject("TestRocket");
        var logger = go.AddComponent<DataLogger>();
        var fuzzy = go.AddComponent<FuzzyLandingController>();
        var neural = go.AddComponent<NeuralController>();
        neural.enableTraining = false;
        var hybrid = go.AddComponent<HybridController>();
        hybrid.fuzzy = fuzzy;
        hybrid.neural = neural;

        var rp = go.AddComponent<RocketPhysics>();
        rp.parameters = p;
        rp.controlMode = mode;
        rp.fuzzyController = fuzzy;
        rp.neuralController = neural;
        rp.hybridController = hybrid;
        rp.simulationArmed = false;
        return go;
    }

    [UnityTest]
    public IEnumerator Rocket_StartsDisarmed_DoesNotFall()
    {
        var p = CreateParams();
        var go = BuildRocket(p, RocketPhysics.ControlMode.PID);
        var rp = go.GetComponent<RocketPhysics>();

        yield return null;
        float y0 = rp.state.position.y;
        yield return new WaitForSeconds(0.2f);
        Assert.IsFalse(rp.simulationArmed);
        Assert.AreEqual(y0, rp.state.position.y, 1f);

        Object.Destroy(go);
        Object.Destroy(p);
    }

    [UnityTest]
    public IEnumerator PID_Landing_FinishesWithinTimeout()
    {
        var p = CreateParams();
        p.startPosition = new Vector3(0f, 250f, 0f);
        p.startVelocity = new Vector3(0f, -30f, 0f);
        p.startEulerAngles = Vector3.zero;

        var go = BuildRocket(p, RocketPhysics.ControlMode.PID);
        var rp = go.GetComponent<RocketPhysics>();
        yield return null;

        rp.ResetSimulation();
        Assert.IsTrue(rp.simulationArmed);

        float t = 0f;
        while (!rp.state.simulationFinished && t < 90f)
        {
            t += Time.deltaTime;
            yield return null;
        }

        Assert.IsTrue(rp.state.simulationFinished, "Simulation should finish");
        Assert.IsTrue(rp.state.isLanded || rp.metrics.timedOut);
        Assert.Greater(rp.metrics.totalFlightTime, 0.5f);
        Assert.GreaterOrEqual(rp.metrics.touchdownVelocity, 0f);

        var logger = go.GetComponent<DataLogger>();
        Assert.Greater(logger.SampleCount, 0);

        Object.Destroy(go);
        Object.Destroy(p);
    }

    [UnityTest]
    public IEnumerator Fuzzy_ProducesPositiveThrust_NearGround()
    {
        var go = new GameObject("FuzzyOnly");
        var fuzzy = go.AddComponent<FuzzyLandingController>();
        yield return null;

        float thrust = fuzzy.CalculateThrust(50f, -20f, 30000f);
        Assert.Greater(thrust, 0f);
        Assert.LessOrEqual(thrust, 30000f * AtmosphereModel.GetGravity(50f) * 3f);

        Vector3 g = fuzzy.CalculateGimbal(10f, -5f, 2f, -1f);
        Assert.LessOrEqual(Mathf.Abs(g.x), fuzzy.maxGimbalDeg + 0.01f);
        Assert.LessOrEqual(Mathf.Abs(g.z), fuzzy.maxGimbalDeg + 0.01f);

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator Hybrid_CombinesControllers_WithoutNaN()
    {
        var go = new GameObject("HybridOnly");
        var fuzzy = go.AddComponent<FuzzyLandingController>();
        var neural = go.AddComponent<NeuralController>();
        neural.enableTraining = false;
        var hybrid = go.AddComponent<HybridController>();
        hybrid.fuzzy = fuzzy;
        hybrid.neural = neural;
        yield return null;

        hybrid.CalculateControl(
            800f, -60f, 35000f, 200000f,
            5f, -3f, 1f, -0.5f, 4f,
            out float thrust, out Vector3 gimbal);

        Assert.IsFalse(float.IsNaN(thrust));
        Assert.IsFalse(float.IsNaN(gimbal.x));
        Assert.IsFalse(float.IsNaN(gimbal.z));
        Assert.GreaterOrEqual(thrust, 0f);

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator CameraFollow_Modes_SwitchCleanly()
    {
        var camGo = new GameObject("Cam");
        var cam = camGo.AddComponent<Camera>();
        var follow = camGo.AddComponent<CameraFollow>();

        var rocketGo = new GameObject("R");
        rocketGo.transform.position = new Vector3(0, 100, 0);
        // minimal rocket state via RocketPhysics would need more setup — just target
        follow.target = rocketGo.transform;
        follow.rocket = null;

        yield return null;
        follow.SetMode(CameraFollow.ViewMode.Follow);
        Assert.AreEqual(CameraFollow.ViewMode.Follow, follow.mode);

        follow.SetMode(CameraFollow.ViewMode.Manual);
        Assert.IsTrue(follow.IsManual);
        Assert.AreEqual("cam_manual", follow.ModeLabelKey);

        follow.SnapToFullTrajectoryView();
        Assert.AreEqual(CameraFollow.ViewMode.Overview, follow.mode);
        Assert.IsTrue(follow.IsOverview);
        Assert.AreEqual("cam_overview", follow.ModeLabelKey);

        // Після snap камера має лишатись у межах сцени
        float maxR = follow.worldBoundRadius + follow.worldBoundCenter.magnitude + 50f;
        Assert.Less(Vector3.Distance(camGo.transform.position, follow.worldBoundCenter), maxR);
        Assert.Greater(camGo.transform.position.y, 3f);

        follow.ResetManualOrbit();
        follow.SnapNow();

        Object.Destroy(camGo);
        Object.Destroy(rocketGo);
    }

    [UnityTest]
    public IEnumerator DataLogger_LogsAndSaves()
    {
        var go = new GameObject("LogTest");
        var logger = go.AddComponent<DataLogger>();
        logger.Initialize();

        var state = new RocketState
        {
            position = new Vector3(1, 100, 2),
            velocity = new Vector3(0, -10, 0),
            rotation = Quaternion.identity,
            currentThrust = 1000f,
            dryMass = 1000f,
            currentFuelMass = 500f,
            time = 1f
        };

        for (int i = 0; i < 20; i++)
        {
            state.time = i * 0.05f;
            state.position.y = 100f - i;
            logger.Log(state);
        }

        Assert.Greater(logger.SampleCount, 0);
        logger.Save();
        Assert.IsTrue(System.IO.File.Exists(logger.LastFilePath));

        // cleanup test file
        try { System.IO.File.Delete(logger.LastFilePath); } catch { /* ignore */ }

        Object.Destroy(go);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PrepareMode_DoesNotArmSimulation()
    {
        var p = CreateParams();
        var go = BuildRocket(p, RocketPhysics.ControlMode.Hybrid);
        var rp = go.GetComponent<RocketPhysics>();
        yield return null;

        rp.PrepareMode(RocketPhysics.ControlMode.Fuzzy);
        Assert.AreEqual(RocketPhysics.ControlMode.Fuzzy, rp.controlMode);
        Assert.IsFalse(rp.simulationArmed);
        Assert.IsFalse(rp.state.simulationFinished);

        Object.Destroy(go);
        Object.Destroy(p);
    }

    /// <summary>Golden: Ideal Hybrid soft-lands under seeded zero-disturbance.</summary>
    [UnityTest]
    public IEnumerator IdealHybrid_SoftLands()
    {
        var p = CreateParams();
        p.startPosition = new Vector3(0f, IdealLandingPresets.StartHeight, 0f);
        p.startVelocity = new Vector3(0f, IdealLandingPresets.StartVy, 0f);
        p.startEulerAngles = new Vector3(0f, 0f, IdealLandingPresets.StartTiltDeg);
        p.dryMass = IdealLandingPresets.DryMass;
        p.fuelMass = IdealLandingPresets.FuelMass;
        p.maxThrust = IdealLandingPresets.MaxThrust;
        p.fixedTimeStep = 0.01f;
        p.maxSimulationTime = 300f;

        var go = BuildRocket(p, RocketPhysics.ControlMode.Hybrid);
        var rp = go.GetComponent<RocketPhysics>();
        yield return null;

        IdealLandingPresets.ApplyDefaultControllerTuning(
            rp, rp.fuzzyController, rp.neuralController, rp.hybridController);
        if (rp.hybridController != null)
            rp.hybridController.useNeuralResidual = true;
        if (rp.neuralController != null)
        {
            rp.neuralController.InstallIdealWeights();
            rp.neuralController.enableTraining = false;
        }

        SimRng.Reseed(DefenseBaseline.Seed);
        rp.ResetSimulation();
        rp.ApplyFlightDisturbances(0f, false);

        // Burst ticks (no FixedUpdate wait) for speed
        rp.batchDrivenTicks = true;
        int guard = 0;
        int maxSteps = Mathf.CeilToInt(p.maxSimulationTime / p.fixedTimeStep) + 64;
        while (!rp.state.simulationFinished && guard < maxSteps)
        {
            for (int i = 0; i < 40 && !rp.state.simulationFinished && guard < maxSteps; i++)
            {
                rp.SimulationTick();
                guard++;
            }
            yield return null;
        }

        Assert.IsTrue(rp.state.simulationFinished, "Ideal Hybrid should finish");
        Assert.IsFalse(rp.metrics.timedOut, "should not timeout");
        Assert.IsTrue(rp.metrics.isSuccessfulLanding,
            $"Ideal Hybrid fail: V={rp.metrics.touchdownVelocity:F2} tilt={rp.metrics.landingAngleError:F2} miss={rp.metrics.horizontalMiss:F1}");

        Object.Destroy(go);
        Object.Destroy(p);
    }
}
