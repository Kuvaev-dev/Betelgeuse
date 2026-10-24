using UnityEngine;

/// <summary>
/// Дискретний PID з anti-windup і фільтрованою похідною:
/// u = Kp·e + Ki·∫e + Kd·ḋ, e = setpoint − value, ḋ — похідна, пропущена через фільтр 1-го порядку (τ).
/// Похідна на першому такті після <see cref="Reset"/> дорівнює 0 (без «удару» похідної).
/// <see cref="CalculateWithRate"/> — варіант «похідна по вимірюванню» (напр. з гіроскопа).
/// </summary>
[System.Serializable]
public class PIDController
{
    public float Kp = 0.5f;
    public float Ki = 0.1f;
    public float Kd = 0.3f;
    /// <summary>Межа модуля інтеграла помилки (anti-windup).</summary>
    public float IntegralLimit = 15f;
    /// <summary>Стала часу фільтра похідної, с (0 — без фільтра).</summary>
    public float DerivativeFilterTau = 0f;

    float integral;
    float previousError;
    float filteredDerivative;
    bool hasPrevious;

    public float Calculate(float setpoint, float currentValue, float dt)
    {
        if (dt <= 0f) return 0f;
        float error = setpoint - currentValue;
        float raw = hasPrevious ? (error - previousError) / dt : 0f;
        previousError = error;
        hasPrevious = true;
        float a = DerivativeFilterTau > 0f ? dt / (DerivativeFilterTau + dt) : 1f;
        filteredDerivative += a * (raw - filteredDerivative);
        return Output(error, filteredDerivative, dt);
    }

    /// <summary>PID з відомою швидкістю зміни помилки (похідна по вимірюванню, без шуму диференціювання).</summary>
    public float CalculateWithRate(float error, float errorRate, float dt)
    {
        if (dt <= 0f) return 0f;
        return Output(error, errorRate, dt);
    }

    float Output(float error, float derivative, float dt)
    {
        integral = Mathf.Clamp(integral + error * dt, -IntegralLimit, IntegralLimit);
        return Kp * error + Ki * integral + Kd * derivative;
    }

    public void Reset()
    {
        integral = 0f;
        previousError = 0f;
        filteredDerivative = 0f;
        hasPrevious = false;
    }
}
