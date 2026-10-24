using NUnit.Framework;
using UnityEngine;

public class LandingMetricsTests
{
    [Test]
    public void SuccessScore_Timeout_IsZero()
    {
        var m = new LandingMetrics
        {
            timedOut = true,
            touchdownVelocity = 1f,
            landingAngleError = 1f,
            fuelRemaining = 5000f,
            horizontalMiss = 1f,
            horizontalSpeed = 1f
        };
        Assert.AreEqual(0f, m.SuccessScore, 1e-5f);
    }

    [Test]
    public void SuccessScore_PerfectSoftLanding_IsHigh()
    {
        var m = new LandingMetrics
        {
            timedOut = false,
            touchdownVelocity = 0.5f,
            landingAngleError = 0.5f,
            fuelRemaining = 6000f,
            horizontalMiss = 1f,
            horizontalSpeed = 0.5f
        };
        Assert.Greater(m.SuccessScore, 85f);
        Assert.LessOrEqual(m.SuccessScore, 100f);
    }

    [Test]
    public void SuccessScore_HardImpact_IsLow()
    {
        var m = new LandingMetrics
        {
            timedOut = false,
            touchdownVelocity = 20f,
            landingAngleError = 30f,
            fuelRemaining = 0f,
            horizontalMiss = 100f,
            horizontalSpeed = 20f
        };
        Assert.Less(m.SuccessScore, 15f);
    }

    [Test]
    public void BuildUserSummary_Success_ContainsKeyMetrics()
    {
        var m = new LandingMetrics
        {
            isSuccessfulLanding = true,
            touchdownVelocity = 2.1f,
            landingAngleError = 3f,
            horizontalMiss = 5f,
            horizontalSpeed = 1f,
            fuelRemaining = 2000f
        };
        string s = m.BuildUserSummary(true);
        StringAssert.Contains("УСПІШНО", s);
        Assert.IsTrue(s.Contains("2.1") || s.Contains("2,1"), s); // десятковий роздільник залежить від культури ОС
    }

    [Test]
    public void BuildUserSummary_Failure_ListsReasons()
    {
        var m = new LandingMetrics
        {
            isSuccessfulLanding = false,
            timedOut = false,
            touchdownVelocity = 10f,
            landingAngleError = 20f,
            horizontalMiss = 50f,
            horizontalSpeed = 12f
        };
        string s = m.BuildUserSummary(true, 3.5f, 7f, 25f, 5f);
        StringAssert.Contains("НЕВДАЛА", s);
        StringAssert.Contains("Причини", s);
        StringAssert.Contains("Швидкість", s);
    }

    [Test]
    public void BuildUserSummary_Timeout_MentionsTime()
    {
        var m = new LandingMetrics { isSuccessfulLanding = false, timedOut = true };
        string s = m.BuildUserSummary(true);
        StringAssert.Contains("Час", s);
    }
}
