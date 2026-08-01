using UnityEngine;

/// <summary>
/// Класичний PID-регулятор з обмеженням інтеграла (anti-windup).
/// Застосування: стабілізація тангажу/рискання та вертикальний канал тяги.
/// u = Kp·e + Ki·∫e + Kd·de/dt
/// </summary>
[System.Serializable]
public class PIDController
{
    /// <summary>Пропорційний коефіцієнт.</summary>
    public float Kp = 0.5f;
    /// <summary>Інтегральний коефіцієнт.</summary>
    public float Ki = 0.1f;
    /// <summary>Диференціальний коефіцієнт.</summary>
    public float Kd = 0.3f;

    private float integral;
    private float previousError;

    /// <summary>
    /// Обчислює керуючий сигнал за заданим setpoint і поточним значенням.
    /// dt ≤ 0 → повертає 0 (захист від ділення на нуль).
    /// </summary>
    public float Calculate(float setpoint, float currentValue, float dt)
    {
        if (dt <= 0f) return 0f;
        float error = setpoint - currentValue;
        integral += error * dt;
        integral = Mathf.Clamp(integral, -15f, 15f); // Anti-Windup
        float derivative = (error - previousError) / dt;
        previousError = error;
        return Kp * error + Ki * integral + Kd * derivative;
    }

    /// <summary>Скидає інтеграл і попередню помилку (перед новим запуском).</summary>
    public void Reset()
    {
        integral = 0f;
        previousError = 0f;
    }
}
