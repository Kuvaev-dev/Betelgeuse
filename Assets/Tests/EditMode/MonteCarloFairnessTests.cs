using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Справедливість + відновлюваність протоколу порівняння Monte-Carlo (PlayMode не потрібен).
/// </summary>
public class MonteCarloFairnessTests
{
    SimulationParameters MakeParams()
    {
        var p = ScriptableObject.CreateInstance<SimulationParameters>();
        p.startPosition = new Vector3(0f, DefenseBaseline.StartHeight, 0f);
        p.startVelocity = new Vector3(0f, -DefenseBaseline.StartDescentSpeed, 0f);
        p.startEulerAngles = new Vector3(0f, 0f, DefenseBaseline.StartTiltDeg);
        p.dryMass = 25600f;
        p.fuelMass = 14000f;
        p.maxThrust = 845000f;
        p.isp = 311f;
        p.fixedTimeStep = 0.01f; // трохи грубше заради швидкості тестів
        p.maxSimulationTime = 400f;
        p.maxTouchdownVelocity = LandingCriteria.DefaultMaxTouchdownVelocity;
        p.maxLandingAngle = LandingCriteria.DefaultMaxLandingAngle;
        p.maxHorizontalMiss = LandingCriteria.DefaultMaxHorizontalMiss;
        p.maxHorizontalSpeed = LandingCriteria.DefaultMaxHorizontalSpeed;
        return p;
    }

    RocketPhysics BuildRocket(SimulationParameters p)
    {
        var go = new GameObject("MCTestRocket");
        var fuzzy = go.AddComponent<FuzzyLandingController>();
        var neural = go.AddComponent<NeuralController>();
        var hybrid = go.AddComponent<HybridController>();
        hybrid.fuzzy = fuzzy;
        hybrid.neural = neural;
        hybrid.useNeuralResidual = true;
        neural.enableTraining = false;
        neural.InstallIdealWeights();

        var rp = go.AddComponent<RocketPhysics>();
        rp.parameters = p;
        rp.fuzzyController = fuzzy;
        rp.neuralController = neural;
        rp.hybridController = hybrid;
        rp.batchDrivenTicks = true;
        IdealLandingPresets.ApplyDefaultControllerTuning(rp, fuzzy, neural, hybrid);
        return rp;
    }

    LandingMetrics RunOne(RocketPhysics rp, RocketPhysics.ControlMode mode, int trialIndex)
    {
        SimRng.Reseed(SimRng.DeriveSeed(DefenseBaseline.Seed, trialIndex)); // paired

        rp.controlMode = mode;
        rp.batchDrivenTicks = true;
        rp.ResetSimulation();
        rp.controlMode = mode;
        rp.simulationArmed = true;
        rp.simulationPaused = false;

        // Той самий рецепт збурень, що SimulationManager.ApplyRandomNoiseToState
        float w = DefenseBaseline.WindStrength;
        Vector3 windKick = new Vector3(
            SimRng.Range(-w, w), 0f, SimRng.Range(-w * 0.55f, w * 0.55f));
        rp.state.velocity += windKick * 0.45f;
        rp.windVelocity = windKick * 0.1f;
        rp.applyContinuousWind = true;

        float massNoise = 1f + SimRng.Range(-DefenseBaseline.MassVariationPercent,
            DefenseBaseline.MassVariationPercent) / 100f;
        rp.state.currentFuelMass = Mathf.Max(800f, rp.state.currentFuelMass * massNoise);
        float ax = SimRng.Range(-DefenseBaseline.AngleVariationDegrees, DefenseBaseline.AngleVariationDegrees);
        float az = SimRng.Range(-DefenseBaseline.AngleVariationDegrees, DefenseBaseline.AngleVariationDegrees);
        rp.state.rotation = Quaternion.Normalize(rp.state.rotation * Quaternion.Euler(ax, 0f, az));
        float jit = DefenseBaseline.PositionJitterMeters;
        rp.state.position.x += SimRng.Range(-jit, jit);
        rp.state.position.z += SimRng.Range(-jit, jit);
        rp.SyncTransformWithState();
        rp.NavNoiseScale = 0f;
        rp.AlignNavigationToTruth();

        float dt = rp.parameters.fixedTimeStep;
        int maxSteps = Mathf.CeilToInt(400f / dt) + 64;
        int steps = 0;
        while (!rp.state.simulationFinished && steps < maxSteps)
        {
            rp.SimulationTick();
            steps++;
        }
        if (!rp.state.simulationFinished)
            rp.ForceFinish(asTimeout: rp.state.position.y >= EnvironmentBuilder.PadSurfaceY + 2f);

        return new LandingMetrics
        {
            touchdownVelocity = rp.metrics.touchdownVelocity,
            landingAngleError = rp.metrics.landingAngleError,
            fuelRemaining = rp.metrics.fuelRemaining,
            maxAltitude = rp.metrics.maxAltitude,
            totalFlightTime = rp.metrics.totalFlightTime,
            horizontalMiss = rp.metrics.horizontalMiss,
            horizontalSpeed = rp.metrics.horizontalSpeed,
            timedOut = rp.metrics.timedOut,
            isSuccessfulLanding = rp.metrics.isSuccessfulLanding
        };
    }

    [Test]
    public void PairedDisturbances_SameTrial_SameInitialState()
    {
        // Два незалежні потоки з одного (seed, trial) мають збігатись на перших samples
        SimRng.Reseed(SimRng.DeriveSeed(42, 3));
        float a0 = SimRng.Range(-8f, 8f);
        float a1 = SimRng.Range(-8f, 8f);
        SimRng.Reseed(SimRng.DeriveSeed(42, 3));
        Assert.AreEqual(a0, SimRng.Range(-8f, 8f), 1e-6f);
        Assert.AreEqual(a1, SimRng.Range(-8f, 8f), 1e-6f);
    }

    [Test]
    public void MiniMonteCarlo_NotUniversalZero_AndHybridCompetitive()
    {
        var p = MakeParams();
        var rp = BuildRocket(p);
        IdealLandingPresets.ClearActive();

        const int n = 6;
        var pid = new List<LandingMetrics>();
        var fuzzy = new List<LandingMetrics>();
        var neural = new List<LandingMetrics>();
        var hybrid = new List<LandingMetrics>();

        try
        {
            for (int i = 0; i < n; i++)
            {
                pid.Add(RunOne(rp, RocketPhysics.ControlMode.PID, i));
                fuzzy.Add(RunOne(rp, RocketPhysics.ControlMode.Fuzzy, i));
                neural.Add(RunOne(rp, RocketPhysics.ControlMode.Neural, i));
                hybrid.Add(RunOne(rp, RocketPhysics.ControlMode.Hybrid, i));
            }

            float rPid = Success(pid);
            float rFz = Success(fuzzy);
            float rNn = Success(neural);
            float rHy = Success(hybrid);

            Debug.Log($"[MC-test] success% PID={rPid:F0} Fuzzy={rFz:F0} NN={rNn:F0} Hybrid={rHy:F0} | " +
                      $"miss PID={AvgMiss(pid):F1} Hybrid={AvgMiss(hybrid):F1}");

            // Не універсальний нуль — хоча б один алгоритм інколи сідає
            Assert.Greater(Mathf.Max(rPid, Mathf.Max(rFz, Mathf.Max(rNn, rHy))), 0.1f,
                "All algorithms 0% — lateral GNC / protocol still too harsh");

            // PID must not be the only non-zero algorithm
            bool pidOnly = rPid > 0.1f && rFz <= 0.1f && rNn <= 0.1f && rHy <= 0.1f;
            Assert.IsFalse(pidOnly,
                $"PID-only-nonzero (PID={rPid:F0}% Fuzzy={rFz:F0} NN={rNn:F0} Hybrid={rHy:F0}%)");

            float vyFz = AvgVy(fuzzy);
            float vyNn = AvgVy(neural);
            float vyHy = AvgVy(hybrid);
            Debug.Log($"[MC-test] mean |Vy| Fuzzy={vyFz:F1} NN={vyNn:F1} Hybrid={vyHy:F1}");
            // Ballistic (no landing burn) is ~21 / 31 m/s for this IC
            const float ballisticVy = 18f;
            Assert.Less(vyFz, ballisticVy, $"Fuzzy mean Vy={vyFz:F1} looks ballistic (~21/31)");
            Assert.Less(vyNn, ballisticVy, $"Neural mean Vy={vyNn:F1} looks ballistic (~21/31)");
            Assert.Less(vyHy, ballisticVy, $"Hybrid mean Vy={vyHy:F1} looks ballistic (~21/31)");

            // Hybrid має перевершити або зрівнятись з PID за success rate (або mean miss, якщо обидва ~0)
            if (rHy + rPid > 0.1f)
            {
                Assert.GreaterOrEqual(rHy + 1e-3f, rPid - 15f,
                    $"Hybrid ({rHy:F0}%) should be competitive with PID ({rPid:F0}%)");
            }

            // Mean miss: Hybrid не має бути різко гіршим за PID
            Assert.LessOrEqual(AvgMiss(hybrid), AvgMiss(pid) * 1.35f + 15f);
        }
        finally
        {
            Object.DestroyImmediate(rp.gameObject);
            Object.DestroyImmediate(p);
        }
    }

    static float Success(List<LandingMetrics> list)
    {
        if (list.Count == 0) return 0f;
        int ok = 0;
        foreach (var m in list) if (m.isSuccessfulLanding) ok++;
        return ok * 100f / list.Count;
    }

    static float AvgMiss(List<LandingMetrics> list)
    {
        if (list.Count == 0) return 0f;
        float s = 0f;
        foreach (var m in list) s += m.horizontalMiss;
        return s / list.Count;
    }

    static float AvgVy(List<LandingMetrics> list)
    {
        if (list.Count == 0) return 0f;
        float s = 0f;
        foreach (var m in list) s += m.touchdownVelocity;
        return s / list.Count;
    }
}
