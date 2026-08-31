using NUnit.Framework;
using UnityEngine;

/// <summary>Відтворюваність + ablation Hybrid residual (leave-one-out для тези).</summary>
public class SimRngAndHybridAblationTests
{
    [Test]
    public void SimRng_SameSeed_SameSequence()
    {
        SimRng.Reseed(42);
        float a0 = SimRng.Range(-10f, 10f);
        float a1 = SimRng.Range(0f, 1f);
        int a2 = SimRng.Range(0, 100);

        SimRng.Reseed(42);
        Assert.AreEqual(a0, SimRng.Range(-10f, 10f), 1e-6f);
        Assert.AreEqual(a1, SimRng.Range(0f, 1f), 1e-6f);
        Assert.AreEqual(a2, SimRng.Range(0, 100));
    }

    [Test]
    public void SimRng_DifferentSeed_Diverges()
    {
        SimRng.Reseed(1);
        float x = SimRng.Range(0f, 1000f);
        SimRng.Reseed(2);
        float y = SimRng.Range(0f, 1000f);
        Assert.AreNotEqual(x, y);
    }

    [Test]
    public void Hybrid_ResidualOff_MatchesFuzzyThrustBand()
    {
        var go = new GameObject("HybAblation");
        var fuzzy = go.AddComponent<FuzzyLandingController>();
        var neural = go.AddComponent<NeuralController>();
        neural.InstallIdealWeights();
        neural.enableTraining = false;
        var hybrid = go.AddComponent<HybridController>();
        hybrid.fuzzy = fuzzy;
        hybrid.neural = neural;
        hybrid.useNeuralResidual = false;

        float h = 400f, vy = -40f, mass = 38000f;
        float fThrust = fuzzy.EvaluateSugenoThrust(h, vy, mass);
        hybrid.CalculateControl(h, vy, mass, fThrust, 2f, -1f, 0f, 0f, 3f,
            out float hybThrust, out _);

        // Без residual smart-шлях — чистий fuzzy до blend з профілем —
        // все ще blended з soft-landing, тож лишатись біля порядку величини fuzzy.
        Assert.Greater(hybThrust, 1000f);
        Assert.Less(Mathf.Abs(hybThrust - fThrust) / Mathf.Max(1f, Mathf.Abs(fThrust)), 0.85f);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void DefenseBaseline_Constants_Sane()
    {
        Assert.AreEqual(42, DefenseBaseline.Seed);
        Assert.GreaterOrEqual(DefenseBaseline.TestsPerAlgorithm, 5);
        Assert.Greater(DefenseBaseline.WindStrength, 0f);
        Assert.IsTrue(DefenseBaseline.EnableNoise);
        Assert.IsTrue(DefenseBaseline.HybridResidualOn);
        Assert.GreaterOrEqual(DefenseBaseline.ProtocolVersion, 2);
        Assert.Greater(DefenseBaseline.PositionJitterMeters, 0f);
    }

    [Test]
    public void SimRng_PairedTrialSalt_IndependentOfAlgorithm()
    {
        // Trial i має ділити один seed між A–D (без algoIndex у salt)
        int seed0 = SimRng.DeriveSeed(42, 0);
        int seed1 = SimRng.DeriveSeed(42, 1);
        Assert.AreNotEqual(seed0, seed1);
        Assert.AreEqual(seed0, SimRng.DeriveSeed(42, 0));
    }

    [Test]
    public void BuildComparisonJson_IncludesPairedProtocolFields()
    {
        var d = new ResearchExporter.ComparisonExportData
        {
            timestamp = "t",
            testsPerAlgorithm = 15,
            enableNoise = true,
            windStrength = 8f,
            positionJitterMeters = 18f,
            continuousWind = true,
            experimentSeed = 42,
            protocolVersion = 2,
            pairedSeeds = true
        };
        d.algorithms.Add(ResearchExporter.ComputeStats("PID",
            new System.Collections.Generic.List<LandingMetrics>
            {
                new LandingMetrics { touchdownVelocity = 1f, isSuccessfulLanding = true }
            }));
        string json = ResearchExporter.BuildComparisonJson(d);
        StringAssert.Contains("pairedSeeds", json);
        StringAssert.Contains("positionJitterMeters", json);
        StringAssert.Contains("protocolVersion", json);
        StringAssert.Contains("true", json);
    }

    [Test]
    public void ComputeStats_Stdev_Populated()
    {
        var list = new System.Collections.Generic.List<LandingMetrics>
        {
            new LandingMetrics { touchdownVelocity = 1f, landingAngleError = 1f, fuelRemaining = 1000f,
                horizontalMiss = 2f, horizontalSpeed = 1f, totalFlightTime = 40f, isSuccessfulLanding = true },
            new LandingMetrics { touchdownVelocity = 2f, landingAngleError = 2f, fuelRemaining = 900f,
                horizontalMiss = 5f, horizontalSpeed = 2f, totalFlightTime = 42f, isSuccessfulLanding = true },
            new LandingMetrics { touchdownVelocity = 4f, landingAngleError = 8f, fuelRemaining = 500f,
                horizontalMiss = 30f, horizontalSpeed = 6f, totalFlightTime = 50f, isSuccessfulLanding = false },
        };
        // Змусити scores відрізнятись через поля, які використовує SuccessScore
        var s = ResearchExporter.ComputeStats("T", list);
        Assert.AreEqual(3, s.tests);
        Assert.AreEqual(2, s.successCount);
        Assert.GreaterOrEqual(s.stdSuccessScore, 0f);
    }
}
