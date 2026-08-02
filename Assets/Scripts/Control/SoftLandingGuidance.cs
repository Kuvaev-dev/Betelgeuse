using UnityEngine;

/// <summary>
/// Soft-landing профіль без bounce: feedforward m(g+a) + PD по v_target.
/// Перевірено 1D-симуляцією (LandingSimulationTests).
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

    /// <summary>Тяга для профілю, Н. Без «підскоку» біля pad.</summary>
    public static float ProfileThrust(float height, float verticalVelocity, float mass)
    {
        float g = AtmosphereModel.GetGravity(Mathf.Max(0f, height));
        float hover = Mathf.Max(1f, mass * g);
        float h = Mathf.Max(0f, height);
        float v = verticalVelocity;
        float target = TargetDescentRate(h);

        // Підйом / зависання — ріжемо тягу, щоб сісти
        if (v > 0.3f)
            return hover * Mathf.Clamp(0.25f - v * 0.05f, 0.1f, 0.5f);

        // Feedforward: a_up ≈ profileAccel для v=−√(2ah)
        float aUp = h < 8f ? 0.4f : 1.15f;
        float mult = 1f + aUp / Mathf.Max(0.1f, g);

        // PD: err>0 якщо падаємо швидше за профіль (v більш від’ємна)
        float err = target - v;
        float kp = h < 80f ? 0.07f : 0.045f;
        mult += Mathf.Clamp(err * kp, -0.55f, 1.1f);

        // Аварійне гальмування
        if (v < target - 12f)
            mult += Mathf.Clamp((-v + target - 12f) * 0.025f, 0f, 0.85f);

        // Термінал: м’який контакт, без відскоку
        if (h < 12f)
        {
            if (v < -2.8f)
                mult = Mathf.Max(mult, 1.35f);      // ще швидко — гальмуй
            else if (v > -0.35f)
                mult = Mathf.Min(mult, 0.82f);      // майже стоїмо / вгору — опусти
            else
                mult = Mathf.Clamp(1.02f + err * 0.12f, 0.88f, 1.35f);
        }
        if (h < 3f)
        {
            // Фінальні метри: майже hover, трохи гальмування якщо треба
            if (v < -1.2f) mult = Mathf.Clamp(1.25f + (-v - 1.2f) * 0.2f, 1.15f, 1.7f);
            else mult = Mathf.Clamp(0.95f + (-v) * 0.15f, 0.75f, 1.2f);
        }

        return hover * Mathf.Clamp(mult, 0.15f, 2.75f);
    }

    public static Vector3 AttitudeGimbal(
        Quaternion rotation, Vector3 angularVelocityBody,
        float maxDeg = 18f, float kp = 1.15f, float kd = 0.55f)
    {
        Vector3 bodyUp = rotation * Vector3.up;
        Vector3 axisWorld = Vector3.Cross(Vector3.up, bodyUp);
        Vector3 axisBody = Quaternion.Inverse(rotation) * axisWorld;

        float cmdX = -axisBody.x * kp - angularVelocityBody.x * kd;
        float cmdZ = -axisBody.z * kp - angularVelocityBody.z * kd;

        return new Vector3(
            Mathf.Clamp(cmdX * Mathf.Rad2Deg, -maxDeg, maxDeg),
            0f,
            Mathf.Clamp(cmdZ * Mathf.Rad2Deg, -maxDeg, maxDeg));
    }

    public static Vector3 AttitudeGimbal(float pitchErrorDeg, float yawErrorDeg,
        float pitchRateDeg = 0f, float yawRateDeg = 0f, float maxDeg = 18f)
    {
        return new Vector3(
            Mathf.Clamp(-pitchErrorDeg * 0.9f - pitchRateDeg * 0.42f, -maxDeg, maxDeg),
            0f,
            Mathf.Clamp(-yawErrorDeg * 0.9f - yawRateDeg * 0.42f, -maxDeg, maxDeg));
    }

    public static float UprightThrustScale(float tiltDeg)
    {
        if (tiltDeg < 10f) return 1f;
        if (tiltDeg > 50f) return 0.25f;
        return Mathf.Lerp(1f, 0.25f, (tiltDeg - 10f) / 40f);
    }

    public static float BlendThrust(float profileThrust, float smartThrust, float smartWeight, float mass, float height)
    {
        float w = Mathf.Clamp01(smartWeight);
        if (height < 45f) w *= Mathf.Clamp01(height / 45f);
        float blended = Mathf.Lerp(profileThrust, smartThrust, w);
        float g = AtmosphereModel.GetGravity(Mathf.Max(0f, height));
        float hover = mass * g;
        float maxDev = hover * (height < 30f ? 0.25f : 0.4f);
        return Mathf.Clamp(blended, profileThrust - maxDev, profileThrust + maxDev);
    }

    /// <summary>1D симуляція для тестів. Повертає |Vy| на touchdown.</summary>
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
