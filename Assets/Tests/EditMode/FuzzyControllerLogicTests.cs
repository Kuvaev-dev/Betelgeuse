using NUnit.Framework;
using UnityEngine;

/// <summary>Нечіткий регулятор Sugeno-0: розбиття термів, монотонність тяги, симетрія орієнтації.</summary>
public class FuzzyControllerLogicTests
{
    readonly FuzzyLandingController fuzzy = new FuzzyLandingController();

    [TestCase(-1f)] [TestCase(-0.7f)] [TestCase(-0.25f)] [TestCase(0f)] [TestCase(0.4f)] [TestCase(0.9f)] [TestCase(1f)]
    public void SignedTerms_FormPartitionOfUnity(float x)
    {
        float s = 0f;
        foreach (var mu in FuzzyLandingController.SignedMembership5(x)) s += mu;
        Assert.AreEqual(1f, s, 1e-4f);
    }

    [Test]
    public void SugenoThrust_GrowsWithDescentSpeed()
    {
        float m = 35000f;
        float slow = fuzzy.EvaluateSugenoThrust(600f, -10f, m);
        float fast = fuzzy.EvaluateSugenoThrust(600f, -70f, m);
        Assert.Greater(fast, slow);
    }

    [Test]
    public void SugenoThrust_HigherNearGroundForSameSpeed()
    {
        float m = 35000f;
        Assert.Greater(fuzzy.EvaluateSugenoThrust(100f, -40f, m), fuzzy.EvaluateSugenoThrust(2400f, -40f, m));
    }

    [Test]
    public void AttitudeRules_ZeroAtOrigin_OddSymmetric_Restoring()
    {
        Assert.AreEqual(0f, fuzzy.CalculateGimbal(0f, 0f, 0f, 0f).x, 1e-4f);
        var a = fuzzy.CalculateGimbal(4f, -4f, 1f, -1f);
        Assert.Greater(a.x, 0f);
        Assert.AreEqual(-a.x, a.z, 1e-4f);
    }

    [Test]
    public void AttitudeRules_Saturate()
    {
        var g = fuzzy.CalculateGimbal(90f, 0f, 90f, 0f);
        Assert.AreEqual(fuzzy.maxGimbalDeg, g.x, 1e-3f);
    }

    [Test]
    public void Thrust_StaysWithinBlendEnvelopeOfProfile()
    {
        float m = 35000f;
        float profile = SoftLandingGuidance.ProfileThrust(800f, -40f, m);
        float t = fuzzy.CalculateThrust(800f, -40f, m);
        Assert.LessOrEqual(Mathf.Abs(t - profile), m * AtmosphereModel.GetGravity(800f) * fuzzy.maxDevFrac + 1f);
    }
}
