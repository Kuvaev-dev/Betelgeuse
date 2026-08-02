using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Логіка фазифікації через публічний API Fuzzy (потребує GO у EditMode — створюємо тимчасово).
/// </summary>
public class FuzzyControllerLogicTests
{
    FuzzyLandingController fuzzy;
    GameObject go;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("FuzzyTest");
        fuzzy = go.AddComponent<FuzzyLandingController>();
    }

    [TearDown]
    public void TearDown()
    {
        if (go != null) Object.DestroyImmediate(go);
    }

    [Test]
    public void Thrust_HighSpeedLowAltitude_IsAggressive()
    {
        float hover = 30000f * AtmosphereModel.GetGravity(20f);
        float thrust = fuzzy.CalculateThrust(20f, -40f, 30000f);
        Assert.Greater(thrust, hover * 1.2f);
    }

    [Test]
    public void Thrust_HighAltitudeSlow_IsNearHover()
    {
        float mass = 30000f;
        float h = 2000f;
        float g = AtmosphereModel.GetGravity(h);
        float thrust = fuzzy.CalculateThrust(h, -15f, mass);
        Assert.Greater(thrust, mass * g * 0.7f);
        Assert.Less(thrust, mass * g * 3f);
    }

    [Test]
    public void Gimbal_ZeroError_NearZero()
    {
        Vector3 g = fuzzy.CalculateGimbal(0f, 0f, 0f, 0f);
        Assert.AreEqual(0f, g.x, 0.5f);
        Assert.AreEqual(0f, g.z, 0.5f);
    }

    [Test]
    public void Gimbal_LargeError_SaturatedWithinLimit()
    {
        Vector3 g = fuzzy.CalculateGimbal(40f, -40f, 30f, -30f);
        Assert.LessOrEqual(Mathf.Abs(g.x), fuzzy.maxGimbalDeg + 0.01f);
        Assert.LessOrEqual(Mathf.Abs(g.z), fuzzy.maxGimbalDeg + 0.01f);
        Assert.Greater(Mathf.Abs(g.x) + Mathf.Abs(g.z), 5f);
    }

    [Test]
    public void Inactive_ReturnsFallbackThrust()
    {
        fuzzy.isActive = false;
        float mass = 25000f;
        float thrust = fuzzy.CalculateThrust(500f, -50f, mass);
        float expected = mass * AtmosphereModel.GetGravity(500f) * 1.1f;
        Assert.AreEqual(expected, thrust, 1f);
    }

    [Test]
    public void Gimbal_OpposesPitchError_NegativeFeedback()
    {
        // Позитивна помилка кута → від'ємний gimbal (стабілізація)
        Vector3 g = fuzzy.CalculateGimbal(15f, 0f, 0f, 0f);
        Assert.Less(g.x, 0f);
        Vector3 gNeg = fuzzy.CalculateGimbal(-15f, 0f, 0f, 0f);
        Assert.Greater(gNeg.x, 0f);
    }

    [Test]
    public void Thrust_NeverNegative_OrNaN()
    {
        float t = fuzzy.CalculateThrust(0.5f, -80f, 30000f);
        Assert.IsFalse(float.IsNaN(t));
        Assert.GreaterOrEqual(t, 0f);
    }
}
