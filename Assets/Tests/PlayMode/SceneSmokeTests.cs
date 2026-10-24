using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Димовий тест повної сцени: bootstrap → номінальна посадка Hybrid → Monte-Carlo A–D з експортом.
/// Будь-який Debug.LogError/виняток під час тесту робить його червоним (Unity Test Framework).
/// </summary>
public class SceneSmokeTests
{
    [UnityTest]
    public IEnumerator SampleScene_BootstrapsLandsAndRunsMonteCarlo()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;
        var runner = new GameObject("BootstrapRunner").AddComponent<BootstrapRunner>();

        float t = 0f;
        while (runner != null && t < 120f) { t += Time.unscaledDeltaTime; yield return null; }
        Assert.IsTrue(runner == null, "bootstrap should finish and destroy itself");

        var rocket = Object.FindAnyObjectByType<RocketPhysics>();
        var sim = Object.FindAnyObjectByType<SimulationManager>();
        Assert.IsNotNull(rocket);
        Assert.IsNotNull(sim);
        StringAssert.Contains("ES(1+", rocket.WeightsInfo, "trained weights must be loaded");

        // Номінальна посадка Hybrid
        rocket.PrepareMode(ControlMode.Hybrid);
        float prevScale = Time.timeScale;
        Time.timeScale = 20f;
        rocket.StartLanding(DisturbanceSettings.None, 1);
        t = 0f;
        while (!rocket.state.simulationFinished && t < 30f) { t += Time.unscaledDeltaTime; yield return null; }
        Time.timeScale = prevScale;
        Assert.IsTrue(rocket.metrics.isSuccessfulLanding,
            $"Vy={rocket.metrics.touchdownVelocity:F2} tilt={rocket.metrics.landingAngleError:F2} miss={rocket.metrics.horizontalMiss:F1}");

        // Monte-Carlo протоколу захисту (скорочено)
        DefenseBaseline.ApplyTo(sim);
        sim.testsPerAlgorithm = 20;
        sim.RequestFullExperiment();
        yield return null;
        t = 0f;
        while (sim.IsExperimentRunning && t < 60f) { t += Time.unscaledDeltaTime; yield return null; }
        Assert.IsFalse(sim.IsExperimentRunning, "MC should finish");
        Assert.AreEqual(20, sim.HybridResults.Count);
        Assert.AreEqual(20, sim.PidResults.Count);
    }
}
