using NUnit.Framework;
using UnityEngine;

/// <summary>Фізична модель: g(h), ρ(h), витрата палива, профіль soft-landing.</summary>
public class PhysicsModelTests
{
    [Test]
    public void HoverThrust_AtSeaLevel_IsMg_AndBelowMaxThrust()
    {
        float hover = Stage1Vehicle.LandingMassKg * AtmosphereModel.GetGravity(0f);
        Assert.AreEqual(Stage1Vehicle.LandingMassKg * 9.80665f, hover, 1f);
        Assert.Greater(Stage1Vehicle.LandingThrustN, hover);
    }

    [Test]
    public void IspMassFlow_PositiveAndReasonable()
    {
        float mdot = 500000f / (Stage1Vehicle.IspLandingS * 9.80665f);
        Assert.Greater(mdot, 100f);
        Assert.Less(mdot, 250f);
    }

    [Test]
    public void DensityScaleHeight_MatchesExponential()
    {
        float d0 = AtmosphereModel.GetDensity(0f);
        float dH = AtmosphereModel.GetDensity(8446f);
        Assert.AreEqual(d0 / Mathf.Exp(1f), dH, 0.02f);
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
        Assert.Greater(Mathf.Abs(SoftLandingGuidance.TargetDescentRate(500f)),
                       Mathf.Abs(SoftLandingGuidance.TargetDescentRate(5f)));
        Assert.Less(Mathf.Abs(SoftLandingGuidance.TargetDescentRate(5f)), 2f);
    }

    [Test]
    public void SoftLanding_VerticalProfile_TouchesDownSoftly()
    {
        Assert.IsTrue(IdealLandingPresets.ProfileGuaranteesSoftLanding(out float v));
        Assert.Less(v, LandingCriteria.DefaultMaxTouchdownVelocity);
    }
}
