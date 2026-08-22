using UnityEngine;

/// <summary>
/// Спільний soft-landing профіль v_target=−√(2ah) + PD; TVC upright; BlendThrust.
/// База для всіх режимів і термінал h&lt;~25 м.
/// </summary>
public static class SoftLandingGuidance
{
    /// <summary>Цільова вертикальна швидкість (від’ємна = вниз).</summary>
    public static float TargetDescentRate(float height)
    {
        float h = Mathf.Max(0f, height);
        if (h < 2f) return -0.4f;
        if (h < 6f) return Mathf.Lerp(-0.4f, -1.0f, (h - 2f) / 4f);
        if (h < 25f) return -Mathf.Sqrt(2f * 1.0f * h);
        if (h < 100f) return -Mathf.Sqrt(2f * 1.15f * h);
        return -Mathf.Clamp(Mathf.Sqrt(2f * 1.3f * h), 4f, 50f);
    }

    /// <summary>Тяга профілю, Н. Без bounce біля pad.</summary>
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

        float err = target - v;
        float kp = h < 80f ? 0.055f : 0.04f;
        mult += Mathf.Clamp(err * kp, -0.4f, 0.9f);

        if (v < target - 10f)
            mult += Mathf.Clamp((-v + target - 10f) * 0.022f, 0f, 0.75f);

        if (h < 15f)
        {
            if (v < -2.5f) mult = Mathf.Max(mult, 1.28f);
            else if (v > -0.3f) mult = Mathf.Min(mult, 0.78f);
            else mult = Mathf.Clamp(1.0f + err * 0.1f, 0.88f, 1.28f);
        }
        if (h < 4f)
        {
            if (v < -1.0f) mult = Mathf.Clamp(1.18f + (-v - 1.0f) * 0.18f, 1.1f, 1.55f);
            else mult = Mathf.Clamp(0.92f + (-v) * 0.12f, 0.72f, 1.12f);
        }

        return hover * Mathf.Clamp(mult, 0.15f, 2.5f);
    }

    /// <summary>
    /// TVC-стабілізація: τ ∝ (−td.z, td.x), td = R(cmd)·up.
    /// Cross(worldUp, bodyUp) &gt; 0 ⇒ cmd &gt; 0 (restoring).
    /// </summary>
    public static Vector3 AttitudeGimbal(
        Quaternion rotation, Vector3 angularVelocityBody,
        float maxDeg = 14f, float kp = 0.72f, float kd = 0.95f)
    {
        Vector3 bodyUp = rotation * Vector3.up;
        Vector3 axisWorld = Vector3.Cross(Vector3.up, bodyUp);
        Vector3 axisBody = Quaternion.Inverse(rotation) * axisWorld;

        float cmdX = axisBody.x * kp + angularVelocityBody.x * kd;
        float cmdZ = axisBody.z * kp + angularVelocityBody.z * kd;

        return new Vector3(
            Mathf.Clamp(cmdX * Mathf.Rad2Deg, -maxDeg, maxDeg),
            0f,
            Mathf.Clamp(cmdZ * Mathf.Rad2Deg, -maxDeg, maxDeg));
    }

    /// <summary>PD по SignedAngle-помилках (градуси) + rate damp.</summary>
    public static Vector3 AttitudeGimbal(float pitchErrorDeg, float yawErrorDeg,
        float pitchRateDeg = 0f, float yawRateDeg = 0f, float maxDeg = 14f)
    {
        return new Vector3(
            Mathf.Clamp(-pitchErrorDeg * 0.55f - pitchRateDeg * 0.65f, -maxDeg, maxDeg),
            0f,
            Mathf.Clamp(-yawErrorDeg * 0.55f - yawRateDeg * 0.65f, -maxDeg, maxDeg));
    }

    public static float UprightThrustScale(float tiltDeg)
    {
        if (tiltDeg < 10f) return 1f;
        if (tiltDeg > 50f) return 0.25f;
        return Mathf.Lerp(1f, 0.25f, (tiltDeg - 10f) / 40f);
    }

    /// <summary>
    /// Змішує профіль і «розумну» тягу.
    /// Біля землі (h&lt;25 м) smartWeight → 0 — гарантія soft contact.
    /// maxDevFrac — наскільки smart може відхилятись від профілю (реалізм алгоритмів).
    /// </summary>
    public static float BlendThrust(float profileThrust, float smartThrust, float smartWeight,
        float mass, float height, float maxDevFrac = 0.45f)
    {
        float w = Mathf.Clamp01(smartWeight);
        // Термінал: усі алгоритми сходяться до soft-landing профілю
        if (height < 25f) w *= Mathf.Clamp01(height / 25f);

        float blended = Mathf.Lerp(profileThrust, smartThrust, w);
        float g = AtmosphereModel.GetGravity(Mathf.Max(0f, height));
        float hover = mass * g;
        float maxDev = hover * Mathf.Clamp(maxDevFrac, 0.1f, 1.2f);
        if (height < 30f) maxDev = Mathf.Min(maxDev, hover * 0.22f);
        return Mathf.Clamp(blended, profileThrust - maxDev, profileThrust + maxDev);
    }

    /// <summary>1D симуляція для EditMode-тестів. Повертає |Vy| на touchdown.</summary>
    public static float SimulateVerticalLanding(
        float startH, float startVy, float mass, float maxThrust,
        float dt = 0.005f, float maxTime = 200f)
    {
        float h = startH;
        float v = startVy;
        float t = 0f;
        while (h > 0.05f && t < maxTime)
        {
            float g = AtmosphereModel.GetGravity(Mathf.Max(0f, h));
            float thrust = Mathf.Min(ProfileThrust(h, v, mass), maxThrust);
            float a = thrust / Mathf.Max(1f, mass) - g;
            v += a * dt;
            h += v * dt;
            t += dt;
            if (h < 0f) { h = 0f; break; }
        }
        return Mathf.Abs(v);
    }
}
