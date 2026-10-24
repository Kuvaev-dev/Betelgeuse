using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Єдина конвенція знаків орієнтації: помилки по осях корпусу розв’язані, а PD/нечіткий/PID
/// закони дають відновлювальний момент у моделі <see cref="LandingSimulation"/>.
/// </summary>
public class AttitudeGimbalSignTests
{
    static ControlContext Ctx(Quaternion rot, Vector3 desiredUp)
        => ControlContext.Create(new Vector3(0, 800, 0), new Vector3(0, -30, 0), rot, Vector3.zero,
            35000f, 400000f, Stage1Vehicle.LandingThrustN, desiredUp, 0.005f);

    [Test]
    public void PureTiltAboutX_GivesOnlyPitchError()
    {
        var c = Ctx(Quaternion.Euler(6f, 0f, 0f), Vector3.up);
        Assert.AreEqual(6f, Mathf.Abs(c.PitchErrorDeg), 0.05f);
        Assert.AreEqual(0f, c.YawErrorDeg, 0.05f);
        Assert.AreEqual(6f, c.TiltDeg, 0.05f);
    }

    [Test]
    public void PureTiltAboutZ_GivesOnlyYawError()
    {
        var c = Ctx(Quaternion.Euler(0f, 0f, -4f), Vector3.up);
        Assert.AreEqual(0f, c.PitchErrorDeg, 0.05f);
        Assert.AreEqual(4f, Mathf.Abs(c.YawErrorDeg), 0.05f);
    }

    [Test]
    public void TrackingDesiredAxis_ZeroError()
    {
        var rot = Quaternion.Euler(5f, 0f, 3f);
        var c = Ctx(rot, rot * Vector3.up);
        Assert.AreEqual(0f, c.PitchErrorDeg, 0.05f);
        Assert.AreEqual(0f, c.YawErrorDeg, 0.05f);
        Assert.AreEqual(c.TiltDeg, c.DesiredTiltDeg, 0.05f);
    }

    [Test]
    public void PdGimbal_PositiveErrorAndRate_PositiveCommand()
    {
        var g = SoftLandingGuidance.AttitudeGimbal(4f, -3f, 1f, -1f);
        Assert.Greater(g.x, 0f);
        Assert.Less(g.z, 0f);
        Assert.AreEqual(0f, g.y);
    }

    [TestCase(ControlMode.PID)]
    [TestCase(ControlMode.Fuzzy)]
    [TestCase(ControlMode.Neural)]
    [TestCase(ControlMode.Hybrid)]
    public void EveryStrategy_RestoresTiltedBody(ControlMode mode)
    {
        var setup = LandingSetup.FromInitialConditions(1500f, 50f, 0f);
        var sc = LandingScenario.Nominal(3);
        sc.TiltXDeg = 8f;
        sc.TiltZDeg = -6f;
        var sim = new LandingSimulation { Controller = new ControllerSet().Get(mode) };
        sim.Reset(setup, sc);
        float tilt0 = Vector3.Angle(sim.State.rotation * Vector3.up, Vector3.up);
        for (int i = 0; i < 1600; i++) sim.Step(); // 8 с
        float tilt1 = Vector3.Angle(sim.State.rotation * Vector3.up, Vector3.up);
        Assert.Less(tilt1, tilt0 * 0.6f, $"{mode}: tilt {tilt0:F1}° → {tilt1:F1}°");
    }
}
