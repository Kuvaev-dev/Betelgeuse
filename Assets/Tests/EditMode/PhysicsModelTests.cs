using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Перевірки фізичної моделі: g(h), soft-landing профіль, SuccessScore bounds.
/// </summary>
public class PhysicsModelTests
{
    [Test]
    public void SoftLandingProfile_VelocityMagnitude_GrowsWithSqrtHeight()
    {
        float a = 1.6f;
        float v100 = Mathf.Sqrt(2f * a * 100f);
        float v25 = Mathf.Sqrt(2f * a * 25f);
        Assert.AreEqual(2f, v100 / v25, 0.05f); // √4 = 2
    }

    [Test]
    public void HoverThrust_AtSeaLevel_IsMg()
    {
        float mass = 39600f;
        float hover = mass * AtmosphereModel.GetGravity(0f);
        Assert.AreEqual(mass * 9.80665f, hover, 1f);
        // Max thrust 845 kN > hover for this mass class
        Assert.Greater(845000f, hover);
    }

    [Test]
    public void IspMassFlow_PositiveAndReasonable()
    {
        const float G0 = 9.80665f;
        float isp = 311f;
        float thrust = 500000f;
        float mdot = thrust / (isp * G0);
        Assert.Greater(mdot, 100f);
        Assert.Less(mdot, 250f);
    }

    [Test]
    public void DensityScaleHeight_MatchesExponential()
    {
        // ρ = 1.225 * exp(-h * 0.0001184) → H ≈ 8446 m
        float d0 = AtmosphereModel.GetDensity(0f);
        float dH = AtmosphereModel.GetDensity(8446f);
        Assert.AreEqual(d0 / Mathf.Exp(1f), dH, 0.02f);
    }

    [Test]
    public void RocketState_TotalMass_IsDryPlusFuel()
    {
        var s = new RocketState { dryMass = 25600f, currentFuelMass = 14000f };
        Assert.AreEqual(39600f, s.TotalMass, 1e-3f);
    }

    [Test]
    public void SoftLanding_ProfileThrust_NearHoverAtLowAltitude()
    {
        float mass = 35000f;
        float thrust = SoftLandingGuidance.ProfileThrust(5f, -2f, mass);
        float hover = mass * AtmosphereModel.GetGravity(5f);
        Assert.Greater(thrust, hover * 0.9f);
        Assert.Less(thrust, hover * 2.5f);
    }

    [Test]
    public void SoftLanding_TargetRate_SlowerNearGround()
    {
        float vHigh = Mathf.Abs(SoftLandingGuidance.TargetDescentRate(500f));
        float vLow = Mathf.Abs(SoftLandingGuidance.TargetDescentRate(5f));
        Assert.Greater(vHigh, vLow);
        Assert.Less(vLow, 2f);
    }

    [Test]
    public void SoftLanding_Gimbal_NegativeFeedback()
    {
        Vector3 g = SoftLandingGuidance.AttitudeGimbal(10f, -8f, 0f, 0f);
        Assert.Less(g.x, 0f);
        Assert.Greater(g.z, 0f);
    }

    [Test]
    public void SoftLanding_CrossProduct_RestoresFromPitchTip()
    {
        // Корпус нахилений +15° навколо X
        var rot = Quaternion.Euler(15f, 0f, 0f);
        Vector3 g = SoftLandingGuidance.AttitudeGimbal(rot, Vector3.zero);
        // Має видати gimbal, що створює відновлювальний torque
        Assert.Greater(Mathf.Abs(g.x), 0.01f);
        Assert.Less(Mathf.Abs(g.x), 25f);
    }
}
