using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public class ResearchExporterTests
{
    [Test]
    public void ComputeStats_EmptyList_Zeros()
    {
        var s = ResearchExporter.ComputeStats("PID", new List<LandingMetrics>());
        Assert.AreEqual("PID", s.name);
        Assert.AreEqual(0, s.tests);
        Assert.AreEqual(0f, s.successRate);
    }

    [Test]
    public void ComputeStats_MixedResults_CorrectRate()
    {
        var list = new List<LandingMetrics>
        {
            Ok(1f), Ok(2f), Fail(8f), Ok(1.5f)
        };
        var s = ResearchExporter.ComputeStats("Hybrid", list);
        Assert.AreEqual(4, s.tests);
        Assert.AreEqual(3, s.successCount);
        Assert.AreEqual(75f, s.successRate, 1e-3f);
        Assert.AreEqual(1f, s.minTouchdownVelocity, 1e-3f);
        Assert.AreEqual(8f, s.maxTouchdownVelocity, 1e-3f);
        Assert.Greater(s.avgSuccessScore, 0f);
    }

    [Test]
    public void BuildLandingJson_ContainsAlgorithmAndFlags()
    {
        var data = SampleLanding();
        string json = ResearchExporter.BuildLandingJson(data);
        StringAssert.Contains("\"algorithm\": \"Hybrid Neuro-Fuzzy\"", json);
        StringAssert.Contains("\"successful\": true", json);
        StringAssert.Contains("touchdownVelocity_mps", json);
        Assert.IsTrue(json.TrimStart().StartsWith("{"));
        Assert.IsTrue(json.TrimEnd().EndsWith("}"));
    }

    [Test]
    public void BuildLandingMarkdown_HasTableAndStatus()
    {
        var data = SampleLanding();
        string md = ResearchExporter.BuildLandingMarkdown(data);
        StringAssert.Contains("# Betelgeuse", md);
        StringAssert.Contains("| Параметр |", md);
        StringAssert.Contains("УСПІШНА", md);
    }

    [Test]
    public void BuildComparisonCsv_HasHeaderAndRows()
    {
        var data = SampleComparison();
        string csv = ResearchExporter.BuildComparisonCsv(data);
        var lines = csv.Split('\n');
        Assert.GreaterOrEqual(lines.Length, 3);
        StringAssert.Contains("Algorithm", lines[0]);
        StringAssert.Contains("PID", csv);
        StringAssert.Contains("Fuzzy", csv);
    }

    [Test]
    public void BuildComparisonJson_ValidStructure()
    {
        string json = ResearchExporter.BuildComparisonJson(SampleComparison());
        StringAssert.Contains("monte_carlo_comparison", json);
        StringAssert.Contains("algorithms", json);
        StringAssert.Contains("successRate_pct", json);
    }

    [Test]
    public void BuildComparisonMarkdown_DeclaresWinner()
    {
        string md = ResearchExporter.BuildComparisonMarkdown(SampleComparison());
        StringAssert.Contains("Переможець", md);
        StringAssert.Contains("Fuzzy Sugeno", md);
    }

    [Test]
    public void ExportLanding_WritesFullPackage()
    {
        var data = SampleLanding();
        data.timestamp = "TEST_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
        data.trajectoryRows = new List<string>
        {
            "step,time_s,posY_m",
            "1,0.000,100",
            "2,1.000,50"
        };
        data.samples = new List<DataLogger.Sample>
        {
            new() { time = 0f, posX = 0, posY = 100, posZ = 0, velY = -10f, thrustKn = 0f },
            new() { time = 1f, posX = 1, posY = 80, posZ = 0, velY = -25f, thrustKn = 200f },
            new() { time = 2f, posX = 2, posY = 20, posZ = 0, velY = -5f, thrustKn = 350f },
            new() { time = 3f, posX = 1, posY = 2, posZ = 0, velY = -1.5f, thrustKn = 280f }
        };

        string dir = ResearchExporter.ExportLanding(data);
        Assert.IsTrue(Directory.Exists(dir));
        Assert.IsTrue(File.Exists(Path.Combine(dir, "00_README.md")));
        Assert.IsTrue(File.Exists(Path.Combine(dir, "01_SUMMARY.md")));
        Assert.IsTrue(File.Exists(Path.Combine(dir, "02_metrics.json")));
        Assert.IsTrue(File.Exists(Path.Combine(dir, "03_timeseries.csv")));
        Assert.IsTrue(File.Exists(Path.Combine(dir, "04_analysis.md")));
        Assert.IsTrue(File.Exists(Path.Combine(dir, "charts", "altitude_vs_time.svg")));
        Assert.IsTrue(File.Exists(Path.Combine(dir, "charts", "track_XZ.svg")));
        StringAssert.Contains("Landing_", Path.GetFileName(dir));

        try { Directory.Delete(dir, true); } catch { /* ignore */ }
    }

    [Test]
    public void BuildSvgSeries_ContainsPolyline()
    {
        var samples = new List<DataLogger.Sample>
        {
            new() { time = 0, posY = 100 },
            new() { time = 1, posY = 50 },
            new() { time = 2, posY = 0 }
        };
        string svg = ResearchExporter.BuildSvgSeries(samples, s => s.time, s => s.posY,
            "Test", "t", "h", "#000", true);
        StringAssert.Contains("<polyline", svg);
        StringAssert.Contains("Test", svg);
    }

    [Test]
    public void ExportComparison_WritesFiles()
    {
        var data = SampleComparison();
        data.timestamp = "TEST_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
        string dir = ResearchExporter.ExportComparison(data);
        Assert.IsTrue(Directory.Exists(dir));
        StringAssert.Contains("Comparison_", Path.GetFileName(dir));
        Assert.IsTrue(File.Exists(Path.Combine(dir, "00_README.md")));
        Assert.IsTrue(File.Exists(Path.Combine(dir, "01_SUMMARY.md")));
        Assert.IsTrue(File.Exists(Path.Combine(dir, "02_results.csv")));
        Assert.IsTrue(File.Exists(Path.Combine(dir, "03_results.json")));
        try { Directory.Delete(dir, true); } catch { /* ignore */ }
    }

    static LandingMetrics Ok(float v) => new()
    {
        isSuccessfulLanding = true,
        touchdownVelocity = v,
        landingAngleError = 2f,
        horizontalMiss = 3f,
        horizontalSpeed = 1f,
        fuelRemaining = 4000f,
        totalFlightTime = 40f
    };

    static LandingMetrics Fail(float v) => new()
    {
        isSuccessfulLanding = false,
        touchdownVelocity = v,
        landingAngleError = 15f,
        horizontalMiss = 40f,
        horizontalSpeed = 8f,
        fuelRemaining = 100f,
        totalFlightTime = 50f
    };

    static ResearchExporter.LandingExportData SampleLanding() => new()
    {
        algorithm = "Hybrid Neuro-Fuzzy",
        timestamp = "20260101_120000",
        metrics = Ok(1.2f)
    };

    static ResearchExporter.ComparisonExportData SampleComparison()
    {
        var d = new ResearchExporter.ComparisonExportData
        {
            timestamp = "20260101_120000",
            testsPerAlgorithm = 10,
            enableNoise = true,
            windStrength = 10f,
            massVariationPercent = 6f,
            angleVariationDegrees = 7f
        };
        d.algorithms.Add(ResearchExporter.ComputeStats("PID", new List<LandingMetrics> { Ok(2f), Fail(5f) }));
        d.algorithms.Add(ResearchExporter.ComputeStats("Fuzzy Sugeno", new List<LandingMetrics> { Ok(1f), Ok(1.5f) }));
        return d;
    }
}
