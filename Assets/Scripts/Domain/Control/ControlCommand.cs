using UnityEngine;

/// <summary>
/// Вихід стратегії керування за один такт: тяга, кути TVC, масштаб бічного наведення.
/// </summary>
public readonly struct ControlCommand
{
    /// <summary>Бажана тяга, Н.</summary>
    public readonly float Thrust;

    /// <summary>Кути TVC, град (x — навколо осі X корпусу, z — навколо осі Z; y не використовується).</summary>
    public readonly Vector3 GimbalEuler;

    /// <summary>
    /// Масштаб команди бічного наведення (1 = спільний закон).
    /// Змінюють його лише навчені MLP-стратегії (C, D) — адаптивний коефіцієнт підсилення.
    /// </summary>
    public readonly float LateralScale;

    public ControlCommand(float thrust, Vector3 gimbalEuler, float lateralScale = 1f)
    {
        Thrust = thrust;
        GimbalEuler = gimbalEuler;
        LateralScale = lateralScale;
    }

    /// <summary>Резервний закон: профіль soft-landing + PD орієнтації.</summary>
    public static ControlCommand ProfileFallback(in ControlContext ctx)
        => new ControlCommand(
            SoftLandingGuidance.ProfileThrust(ctx.Height, ctx.VerticalVelocity, ctx.Mass),
            SoftLandingGuidance.AttitudeGimbal(in ctx));
}
