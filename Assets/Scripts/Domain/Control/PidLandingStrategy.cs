using UnityEngine;

/// <summary>
/// Режим A — класичний PID (еталон порівняння).
/// Тяга: hover + PID по помилці вертикальної швидкості відносно профілю v* = −√(2ah).
/// Орієнтація: два незалежні PID по осях корпусу (помилка відносно бажаної осі від наведення,
/// D-канал — кутова швидкість з гіроскопа).
/// </summary>
[System.Serializable]
public sealed class PidLandingStrategy : ILandingController
{
    public ControlMode Mode => ControlMode.PID;
    public string DisplayName => "PID";

    public PIDController Thrust = new PIDController { Kp = 2.8f, Ki = 0f, Kd = 1.4f, DerivativeFilterTau = 0.15f };
    public PIDController Pitch = new PIDController { Kp = 0.75f, Ki = 0.02f, Kd = 1.3f, IntegralLimit = 10f };
    public PIDController Yaw = new PIDController { Kp = 0.75f, Ki = 0.02f, Kd = 1.3f, IntegralLimit = 10f };
    /// <summary>Масштаб виходу PID тяги, Н на (м/с).</summary>
    public float thrustGainN = 16000f;
    public float maxGimbalDeg = 14f;

    public void SetGains(float thrustKp, float thrustKd, float attKp, float attKi, float attKd)
    {
        Thrust.Kp = thrustKp; Thrust.Ki = 0f; Thrust.Kd = thrustKd; // Ki=0: windup на довгому спуску
        Pitch.Kp = attKp; Pitch.Ki = attKi; Pitch.Kd = attKd;
        Yaw.Kp = attKp; Yaw.Ki = attKi; Yaw.Kd = attKd;
    }

    public void ResetSession()
    {
        Thrust.Reset();
        Pitch.Reset();
        Yaw.Reset();
    }

    public ControlCommand Evaluate(in ControlContext ctx)
    {
        float h = Mathf.Max(0f, ctx.Height);
        float hover = ctx.Mass * ctx.Gravity;
        float target = SoftLandingGuidance.TargetDescentRate(h);
        float u = Thrust.Calculate(target, ctx.VerticalVelocity, ctx.Dt);
        float maxT = ctx.MaxThrust > 0.1f ? ctx.MaxThrust : hover * 3f;
        float thrust = Mathf.Clamp(hover + u * thrustGainN, hover * 0.15f, maxT);

        // u = Kp·e + Ki·∫e + Kd·ω: D-канал по гіроскопу (похідна по вимірюванню, без «удару» від зміни уставки)
        float gx = Mathf.Clamp(Pitch.CalculateWithRate(ctx.PitchErrorDeg, ctx.PitchRateDeg, ctx.Dt), -maxGimbalDeg, maxGimbalDeg);
        float gz = Mathf.Clamp(Yaw.CalculateWithRate(ctx.YawErrorDeg, ctx.YawRateDeg, ctx.Dt), -maxGimbalDeg, maxGimbalDeg);
        return new ControlCommand(thrust, new Vector3(gx, 0f, gz));
    }
}
