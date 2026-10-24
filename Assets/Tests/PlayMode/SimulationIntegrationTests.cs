using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Інтеграційні тести Play Mode: компонент <see cref="RocketPhysics"/> поверх ядра, камера, логер.
/// </summary>
public class SimulationIntegrationTests
{
    static SimulationParameters CreateParams()
    {
        var p = ScriptableObject.CreateInstance<SimulationParameters>();
        p.ApplyStage1Vehicle();
        p.SetInitialConditions(IdealLandingPresets.StartHeight, IdealLandingPresets.StartDescentSpeed, 0f);
        return p;
    }

    static RocketPhysics BuildRocket(SimulationParameters p, ControlMode mode)
    {
        var go = new GameObject("TestRocket");
        var rp = go.AddComponent<RocketPhysics>();
        rp.parameters = p;
        rp.PrepareMode(mode);
        return rp;
    }

    [UnityTest]
    public IEnumerator Rocket_StartsDisarmed_DoesNotFall()
    {
        var p = CreateParams();
        var rp = BuildRocket(p, ControlMode.PID);
        yield return null;
        float y0 = rp.state.position.y;
        yield return new WaitForSeconds(0.2f);
        Assert.IsFalse(rp.simulationArmed);
        Assert.AreEqual(y0, rp.state.position.y, 1e-3f);
        Object.Destroy(rp.gameObject);
        Object.Destroy(p);
    }

    [UnityTest]
    public IEnumerator PrepareMode_DoesNotArmSimulation()
    {
        var p = CreateParams();
        var rp = BuildRocket(p, ControlMode.Hybrid);
        yield return null;
        rp.PrepareMode(ControlMode.Fuzzy);
        Assert.AreEqual(ControlMode.Fuzzy, rp.controlMode);
        Assert.IsFalse(rp.simulationArmed);
        Assert.IsFalse(rp.state.simulationFinished);
        Object.Destroy(rp.gameObject);
        Object.Destroy(p);
    }

    /// <summary>Номінальна посадка Hybrid через FixedUpdate (той самий код, що в Monte-Carlo).</summary>
    [UnityTest]
    public IEnumerator NominalHybrid_SoftLands_ThroughFixedUpdate()
    {
        var p = CreateParams();
        var rp = BuildRocket(p, ControlMode.Hybrid);
        yield return null;

        float prevScale = Time.timeScale;
        Time.timeScale = 20f;
        rp.StartLanding(DisturbanceSettings.None, 1);
        float guard = 0f;
        while (!rp.state.simulationFinished && guard < 30f)
        {
            guard += Time.unscaledDeltaTime;
            yield return null;
        }
        Time.timeScale = prevScale;

        Assert.IsTrue(rp.state.simulationFinished, "landing should finish");
        Assert.IsTrue(rp.metrics.isSuccessfulLanding,
            $"Vy={rp.metrics.touchdownVelocity:F2} tilt={rp.metrics.landingAngleError:F2} miss={rp.metrics.horizontalMiss:F1}");
        Object.Destroy(rp.gameObject);
        Object.Destroy(p);
    }

    [UnityTest]
    public IEnumerator CameraFollow_Modes_SwitchCleanly()
    {
        var camGo = new GameObject("Cam");
        camGo.AddComponent<Camera>();
        var follow = camGo.AddComponent<CameraFollow>();
        var rocketGo = new GameObject("R");
        rocketGo.transform.position = new Vector3(0, 100, 0);
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
            currentFuelMass = 500f
        };
        for (int i = 0; i < 20; i++)
        {
            state.time = i * 0.05f;
            state.position.y = 100f - i;
            logger.Log(state);
        }
        Assert.Greater(logger.SampleCount, 0);
        // Без каталогу запуску лог лишається в пам’яті (його збирає ResearchExporter)
        logger.Save();
        Assert.IsNull(logger.LastFilePath);
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "betelgeuse_log_test");
        System.IO.Directory.CreateDirectory(dir);
        logger.Save(dir);
        Assert.IsTrue(System.IO.File.Exists(logger.LastFilePath));
        try { System.IO.Directory.Delete(dir, true); } catch { /* ignore */ }
        Object.Destroy(go);
        yield return null;
    }
}
