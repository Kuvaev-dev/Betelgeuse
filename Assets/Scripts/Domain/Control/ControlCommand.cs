using UnityEngine;

/// <summary>
/// Вихід стратегії керування за один тік: тяга, TVC, масштаб бічного наведення.
/// </summary>
public readonly struct ControlCommand
{
    /// <summary>Бажана тяга, Н.</summary>
    public readonly float Thrust;

    /// <summary>Кут TVC (град), зазвичай X/Z.</summary>
    public readonly Vector3 GimbalEuler;

    /// <summary>Масштаб lateral guidance (слабкий у PID … сильний у Hybrid).</summary>
    public readonly float LateralScale;

    /// <summary>Blend gimbal стратегії з safety PD upright [0..1].</summary>
    public readonly float GimbalBlend;

    public ControlCommand(float thrust, Vector3 gimbalEuler, float lateralScale = 1f, float gimbalBlend = 0.5f)
    {
        Thrust = thrust;
        GimbalEuler = gimbalEuler;
        LateralScale = lateralScale;
        GimbalBlend = Mathf.Clamp01(gimbalBlend);
    }

    public static ControlCommand ProfileFallback(in ControlContext ctx)
    {
        float thrust = SoftLandingGuidance.ProfileThrust(ctx.Height, ctx.VerticalVelocity, ctx.Mass);
        Vector3 g = SoftLandingGuidance.AttitudeGimbal(
            ctx.Rotation, ctx.AngularVelocity, maxDeg: 16f, kp: 0.7f, kd: 0.92f);
        return new ControlCommand(thrust, g, lateralScale: 0.55f, gimbalBlend: 0f);
    }
}
