using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Регресія: quaternion-AttitudeGimbal має давати restoring torque
/// (узгоджено з error-based API та моделлю TVC τ∝(−td.z, td.x)).
/// </summary>
public class AttitudeGimbalSignTests
{
    [Test]
    public void QuaternionGimbal_RestoresPositiveZTilt()
    {
        // Euler Z > 0: body tips, axisBody.z > 0 ⇒ cmdZ > 0 ⇒ td.x < 0 ⇒ τz < 0
        var rot = Quaternion.Euler(0f, 0f, 8f);
        Vector3 g = SoftLandingGuidance.AttitudeGimbal(rot, Vector3.zero);
        Assert.Greater(g.z, 0.5f, "cmdZ must be >0 for +Z tilt (restoring TVC)");
    }

    [Test]
    public void QuaternionGimbal_RestoresNegativeZTilt()
    {
        var rot = Quaternion.Euler(0f, 0f, -8f);
        Vector3 g = SoftLandingGuidance.AttitudeGimbal(rot, Vector3.zero);
        Assert.Less(g.z, -0.5f, "cmdZ must be <0 for −Z tilt");
    }

    [Test]
    public void QuaternionGimbal_MatchesErrorApi_Sign_IndependentAxes()
    {
        // Pitch-only tip (Euler X)
        {
            float tip = 10f;
            var rot = Quaternion.Euler(tip, 0f, 0f);
            Vector3 bodyUp = rot * Vector3.up;
            float pitchErr = Vector3.SignedAngle(bodyUp, Vector3.up, Vector3.right);
            var fromErr = SoftLandingGuidance.AttitudeGimbal(pitchErr, 0f, 0f, 0f);
            var fromQuat = SoftLandingGuidance.AttitudeGimbal(rot, Vector3.zero);
            Assert.AreEqual(Mathf.Sign(fromErr.x), Mathf.Sign(fromQuat.x),
                "Pitch channel signs must agree");
        }
        // Yaw-only tip (Euler Z)
        {
            float tip = -8f;
            var rot = Quaternion.Euler(0f, 0f, tip);
            Vector3 bodyUp = rot * Vector3.up;
            float yawErr = Vector3.SignedAngle(bodyUp, Vector3.up, Vector3.forward);
            var fromErr = SoftLandingGuidance.AttitudeGimbal(0f, yawErr, 0f, 0f);
            var fromQuat = SoftLandingGuidance.AttitudeGimbal(rot, Vector3.zero);
            Assert.AreEqual(Mathf.Sign(fromErr.z), Mathf.Sign(fromQuat.z),
                "Yaw channel signs must agree");
        }
    }

    [Test]
    public void RateDamping_OpposesOmega()
    {
        var upright = Quaternion.identity;
        Vector3 gPos = SoftLandingGuidance.AttitudeGimbal(upright, new Vector3(0.4f, 0f, 0f));
        Vector3 gNeg = SoftLandingGuidance.AttitudeGimbal(upright, new Vector3(-0.4f, 0f, 0f));
        Assert.Greater(gPos.x, 0f);
        Assert.Less(gNeg.x, 0f);
    }

    [Test]
    public void LateralTvc_SignsPullTowardOrigin()
    {
        // x>0 ⇒ gz>0 ⇒ td.x < 0 (тяга до −x)
        Vector3 tdEast = (Quaternion.Euler(0f, 0f, 5f) * Vector3.up).normalized;
        Assert.Less(tdEast.x, 0f);
        // z>0 ⇒ gx<0 ⇒ td.z < 0
        Vector3 tdNorth = (Quaternion.Euler(-5f, 0f, 0f) * Vector3.up).normalized;
        Assert.Less(tdNorth.z, 0f);
    }

    [Test]
    public void IdealPreset_ProfileSoft()
    {
        Assert.IsTrue(IdealLandingPresets.ProfileGuaranteesSoftLanding(out float v),
            $"Ideal profile |Vy|={v:F2} must be < 3.5");
        Assert.Less(v, 2f);
    }

    [Test]
    public void TvC_TorqueSign_FromPositiveCmdZ()
    {
        // cmdZ > 0 → td.x < 0 → τz = T·L·td.x < 0
        Vector3 td = (Quaternion.Euler(0f, 0f, 10f) * Vector3.up).normalized;
        Assert.Less(td.x, 0f);
        float tauZ = td.x;
        Assert.Less(tauZ, 0f);
    }
}
