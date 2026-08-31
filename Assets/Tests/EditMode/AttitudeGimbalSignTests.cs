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
        // Euler Z > 0: корпус нахиляється, axisBody.z > 0 ⇒ cmdZ > 0 ⇒ td.x < 0 ⇒ τz < 0
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
        // Лише pitch tip (Euler X)
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
        // Лише yaw tip (Euler Z)
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
        // PD: x>0 ⇒ gz<0 ⇒ td.x>0 ⇒ τz>0 ⇒ lean to −X (toward pad)
        const float kPos = 0.22f;
        float px = 40f, pz = 0f;
        float gx = +(kPos * pz);
        float gz = -(kPos * px);
        Assert.Less(gz, 0f, "x>0 must command gz<0 (tail TVC toward pad)");

        Vector3 td = (Quaternion.Euler(gx, 0f, gz) * Vector3.up).normalized;
        Assert.Greater(td.x, 0f, "gz<0 ⇒ td.x>0 ⇒ τz>0 ⇒ lean to −X");

        // z>0 ⇒ gx>0 ⇒ td.z>0 ⇒ τx=-td.z<0 ⇒ lean to −Z
        float gxZ = +(kPos * 40f);
        Assert.Greater(gxZ, 0f, "z>0 must command gx>0");
        Vector3 tdZ = (Quaternion.Euler(gxZ, 0f, 0f) * Vector3.up).normalized;
        Assert.Greater(tdZ.z, 0f, "gx>0 ⇒ td.z>0 ⇒ lean to −Z");
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
