using UnityEngine;

/// <summary>
/// Спільні закони вертикального наведення та орієнтації, які використовують усі режими A–D:
/// профіль зниження v* = −√(2ah), тяга профілю, PD орієнтації (TVC), змішування тяги.
/// </summary>
public static class SoftLandingGuidance
{
    /// <summary>Цільова вертикальна швидкість (від’ємна = вниз), м/с.</summary>
    public static float TargetDescentRate(float height)
    {
        float h = Mathf.Max(0f, height);
        if (h < 2f) return -0.4f;
        if (h < 6f) return Mathf.Lerp(-0.4f, -1.0f, (h - 2f) / 4f);
        if (h < 25f) return -Mathf.Sqrt(2f * 1.0f * h);
        if (h < 100f) return -Mathf.Sqrt(2f * 1.15f * h);
        return -Mathf.Clamp(Mathf.Sqrt(2f * 1.3f * h), 4f, 50f);
    }

    /// <summary>
    /// Тяга профілю soft-landing, Н: hover·(1 + a/g) + P-корекція по помилці швидкості
    /// + термінальні обмеження (h &lt; 25 м) для м’якого контакту.
    /// </summary>
    public static float ProfileThrust(float height, float verticalVelocity, float mass)
    {
        float g = AtmosphereModel.GetGravity(Mathf.Max(0f, height));
        float hover = Mathf.Max(1f, mass * g);
        float h = Mathf.Max(0f, height);
        float v = verticalVelocity;
        float target = TargetDescentRate(h);

        if (v > 0.3f)
            return hover * Mathf.Clamp(0.25f - v * 0.05f, 0.1f, 0.5f);

        float aUp = h < 8f ? 0.35f : (h < 80f ? 1.05f : 1.2f);
        float mult = 1f + aUp / Mathf.Max(0.1f, g);

        float err = target - v; // > 0 — знижуємось надто швидко
        float kp = h < 80f ? 0.055f : 0.04f;
        mult += Mathf.Clamp(err * kp, -0.4f, 0.9f);

        if (v < target - 10f)
            mult += Mathf.Clamp((-v + target - 10f) * 0.022f, 0f, 0.75f);

        if (h < 25f)
        {
            if (v < -3.2f) mult = Mathf.Max(mult, 1.35f);
            else if (v < -2.2f) mult = Mathf.Max(mult, 1.22f);
        }
        if (h < 15f)
        {
            if (v < -2.5f) mult = Mathf.Max(mult, 1.32f);
            else if (v > -0.3f) mult = Mathf.Min(mult, 0.78f);
            else mult = Mathf.Clamp(1.0f + err * 0.12f, 0.88f, 1.32f);
        }
        if (h < 4f)
        {
            if (v < -1.0f) mult = Mathf.Clamp(1.22f + (-v - 1.0f) * 0.22f, 1.12f, 1.65f);
            else mult = Mathf.Clamp(0.92f + (-v) * 0.12f, 0.72f, 1.12f);
        }

        return hover * Mathf.Clamp(mult, 0.15f, 2.5f);
    }

    /// <summary>Типові коефіцієнти PD орієнтації (град TVC на град помилки / на град/с).</summary>
    public const float AttitudeKp = 0.72f;
    public const float AttitudeKd = 1.6f;
    public const float AttitudeMaxDeg = 14f;

    /// <summary>
    /// PD орієнтації: δ = Kp·e + Kd·ω (однакова конвенція знаків для всіх режимів,
    /// додатні e/ω ⇒ додатний δ створює відновлювальний момент).
    /// </summary>
    public static Vector3 AttitudeGimbal(float errXDeg, float errZDeg, float rateXDeg, float rateZDeg,
        float kp = AttitudeKp, float kd = AttitudeKd, float maxDeg = AttitudeMaxDeg)
    {
        return new Vector3(
            Mathf.Clamp(kp * errXDeg + kd * rateXDeg, -maxDeg, maxDeg),
            0f,
            Mathf.Clamp(kp * errZDeg + kd * rateZDeg, -maxDeg, maxDeg));
    }

    public static Vector3 AttitudeGimbal(in ControlContext ctx,
        float kp = AttitudeKp, float kd = AttitudeKd, float maxDeg = AttitudeMaxDeg)
        => AttitudeGimbal(ctx.PitchErrorDeg, ctx.YawErrorDeg, ctx.PitchRateDeg, ctx.YawRateDeg, kp, kd, maxDeg);

    /// <summary>Зменшення тяги лише при аварійному нахилі (&gt; 28°).</summary>
    public static float UprightThrustScale(float tiltDeg)
    {
        if (tiltDeg < 28f) return 1f;
        if (tiltDeg > 60f) return 0.55f;
        return Mathf.Lerp(1f, 0.55f, (tiltDeg - 28f) / 32f);
    }

    /// <summary>
    /// Змішує тягу профілю з «розумною» тягою з вагою w та обмеженням відхилення від профілю.
    /// Біля землі (h &lt; 25 м) вага плавно зменшується до 38 % — гарантія м’якого контакту.
    /// </summary>
    public static float BlendThrust(float profileThrust, float smartThrust, float smartWeight,
        float mass, float height, float maxDevFrac = 0.45f)
    {
        float w = Mathf.Clamp01(smartWeight);
        if (height < 25f)
            w *= Mathf.Lerp(0.38f, 1f, Mathf.Clamp01(height / 25f));

        float blended = Mathf.Lerp(profileThrust, smartThrust, w);
        float hover = mass * AtmosphereModel.GetGravity(Mathf.Max(0f, height));
        float maxDev = hover * Mathf.Clamp(maxDevFrac, 0.1f, 1.2f);
        if (height < 30f)
            maxDev = Mathf.Min(maxDev, hover * 0.42f);
        return Mathf.Clamp(blended, profileThrust - maxDev, profileThrust + maxDev);
    }

    /// <summary>1D-симуляція вертикального каналу (EditMode-тести профілю). Повертає |Vy| на touchdown.</summary>
    public static float SimulateVerticalLanding(
        float startH, float startVy, float mass, float maxThrust,
        float dt = 0.005f, float maxTime = 200f)
    {
        float h = startH, v = startVy, t = 0f;
        while (h > 0.05f && t < maxTime)
        {
            float g = AtmosphereModel.GetGravity(Mathf.Max(0f, h));
            float thrust = Mathf.Min(ProfileThrust(h, v, mass), maxThrust);
            v += (thrust / Mathf.Max(1f, mass) - g) * dt;
            h += v * dt;
            t += dt;
        }
        return Mathf.Abs(v);
    }
}
