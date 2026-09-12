using UnityEngine;

/// <summary>
/// Режим A — класичний PID (pure class). Gains з Ideal presets / UI.
/// </summary>
public sealed class PidLandingStrategy : ILandingController
{
    public RocketPhysics.ControlMode Mode => RocketPhysics.ControlMode.PID;
    public string DisplayName => "PID";
    public bool IsAvailable => true;

    public readonly PIDController Pitch = new PIDController { Kp = 0.55f, Ki = 0.04f, Kd = 0.48f };
    public readonly PIDController Yaw = new PIDController { Kp = 0.55f, Ki = 0.04f, Kd = 0.48f };
    public readonly PIDController Thrust = new PIDController { Kp = 2.8f, Ki = 0.25f, Kd = 1.4f };

    public void SetGains(float thrustKp, float thrustKi, float thrustKd,
        float attKp, float attKi, float attKd)
    {
        // Ki=0 на тязі: I-windup на довгому спуску → hover-out of fuel
        Thrust.Kp = thrustKp; Thrust.Ki = 0f; Thrust.Kd = thrustKd;
        Pitch.Kp = attKp; Pitch.Ki = attKi; Pitch.Kd = attKd;
        Yaw.Kp = attKp; Yaw.Ki = attKi; Yaw.Kd = attKd;
    }

    public void ResetSession()
    {
        Pitch.Reset();
        Yaw.Reset();
        Thrust.Reset();
    }

    public ControlCommand Evaluate(in ControlContext ctx)
    {
        float thrust = CalculateThrust(ctx);
        Vector3 baseGimbal = SoftLandingGuidance.AttitudeGimbal(
            ctx.Rotation, ctx.AngularVelocity, maxDeg: 16f, kp: 0.7f, kd: 0.92f);
        float pc = Pitch.Calculate(0f, ctx.PitchErrorDeg, ctx.Dt);
        float yc = Yaw.Calculate(0f, ctx.YawErrorDeg, ctx.Dt);
        // Ideal: less PID-on-top-of-PD (was double-loop rock near pad)
        float pidLean = IdealLandingPresets.Active ? 0.18f : 0.35f;
        var g = new Vector3(
            Mathf.Clamp(baseGimbal.x + pc * pidLean, -16f, 16f),
            0f,
            Mathf.Clamp(baseGimbal.z + yc * pidLean, -16f, 16f));
        // Найслабше бічне — baseline; при сильному вітрі/jitter програє Hybrid
        float lat = IdealLandingPresets.Active ? 0.55f : 0.62f;
        float gb = IdealLandingPresets.Active ? 0.75f : 0.45f;
        return new ControlCommand(thrust, g, lateralScale: lat, gimbalBlend: gb);
    }

    float CalculateThrust(in ControlContext ctx)
    {
        float h = Mathf.Max(0f, ctx.Height);
        float mass = ctx.Mass;
        float g = AtmosphereModel.GetGravity(h);
        float hover = mass * g;
        float target = SoftLandingGuidance.TargetDescentRate(h);
        float pid = Thrust.Calculate(target, ctx.VerticalVelocity, ctx.Dt);
        float thrust = hover + pid * 16000f;
        float maxT = ctx.MaxThrust > 0.1f ? ctx.MaxThrust : hover * 3f;
        thrust = Mathf.Clamp(thrust, hover * 0.15f, maxT);

        // Профіль лише біля pad (інакше PID ≈ ідеальний soft-landing завжди)
        if (h < 30f)
        {
            float profile = SoftLandingGuidance.ProfileThrust(h, ctx.VerticalVelocity, mass);
            float t = 1f - Mathf.Clamp01(h / 30f);
            thrust = Mathf.Lerp(thrust, profile, t * 0.75f);
        }
        return thrust;
    }
}
