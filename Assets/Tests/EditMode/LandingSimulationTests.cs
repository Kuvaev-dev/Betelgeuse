using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Soft-landing профіль реально садить апарат (усі A/B/C/D базуються на ньому).
/// 1D-симуляція без Unity physics — швидка перевірка критерію |Vy|&lt;3.5.
/// </summary>
public class LandingSimulationTests
{
    const float Mass = 39600f;
    const float MaxThrust = 845000f;

    [Test]
    public void Profile_IdealStart_TouchdownBelow3_5()
    {
        float vTouch = SoftLandingGuidance.SimulateVerticalLanding(
            startH: IdealLandingPresets.StartHeight,
            startVy: IdealLandingPresets.StartVy,
            mass: Mass, maxThrust: MaxThrust);
        Assert.Less(vTouch, 3.5f, $"Touchdown |Vy|={vTouch:F2} м/с");
        Assert.Less(vTouch, 1.5f, "Ideal should be very soft");
    }

    [Test]
    public void Profile_HardNominal_StillSoftIn1D()
    {
        // 1D профіль справляється і з жорсткішим стартом (3D attitude — окремо)
        float vTouch = SoftLandingGuidance.SimulateVerticalLanding(
            startH: 1800f, startVy: -72f, mass: Mass, maxThrust: MaxThrust);
        Assert.Less(vTouch, 3.5f, $"Hard |Vy|={vTouch:F2}");
    }

    [Test]
    public void Profile_IdealPresetStart_VerySoft()
    {
        float vTouch = SoftLandingGuidance.SimulateVerticalLanding(
            IdealLandingPresets.StartHeight, IdealLandingPresets.StartVy,
            IdealLandingPresets.DryMass + IdealLandingPresets.FuelMass,
            IdealLandingPresets.MaxThrust);
        Assert.Less(vTouch, 2.0f, $"Ideal |Vy|={vTouch:F2}");
    }

    [Test]
    public void Profile_HighStart_StillSoft()
    {
        float vTouch = SoftLandingGuidance.SimulateVerticalLanding(
            startH: 2500f, startVy: -100f, mass: Mass, maxThrust: MaxThrust);
        Assert.Less(vTouch, 3.5f, $"High start |Vy|={vTouch:F2}");
    }

    [Test]
    public void Profile_LegacyStart_StillSoft()
    {
        float vTouch = SoftLandingGuidance.SimulateVerticalLanding(
            startH: 1800f, startVy: -70f, mass: Mass, maxThrust: MaxThrust);
        Assert.Less(vTouch, 3.5f, $"Legacy |Vy|={vTouch:F2}");
    }

    [Test]
    public void Profile_LowFuelMass_StillSoft()
    {
        float mass = 25600f + 5000f;
        float vTouch = SoftLandingGuidance.SimulateVerticalLanding(
            startH: 1600f, startVy: -60f, mass: mass, maxThrust: MaxThrust);
        Assert.Less(vTouch, 3.5f, $"Light mass |Vy|={vTouch:F2}");
    }

    [Test]
    public void MaxThrust_ExceedsHover()
    {
        float hover = Mass * AtmosphereModel.GetGravity(0f);
        Assert.Greater(MaxThrust, hover * 1.5f);
    }

    [Test]
    public void TargetRate_MonotonicWithHeight()
    {
        float prev = 0f;
        for (float h = 5f; h <= 500f; h += 20f)
        {
            float speed = Mathf.Abs(SoftLandingGuidance.TargetDescentRate(h));
            Assert.GreaterOrEqual(speed, prev - 0.01f);
            prev = speed;
        }
    }

    [Test]
    public void AttitudeGimbal_FiniteAndBounded()
    {
        var rot = Quaternion.Euler(8f, 0f, -5f);
        Vector3 g = SoftLandingGuidance.AttitudeGimbal(rot, Vector3.zero);
        Assert.IsFalse(float.IsNaN(g.x));
        Assert.IsFalse(float.IsNaN(g.z));
        Assert.LessOrEqual(Mathf.Abs(g.x), 20f);
        Assert.LessOrEqual(Mathf.Abs(g.z), 20f);
    }
}
