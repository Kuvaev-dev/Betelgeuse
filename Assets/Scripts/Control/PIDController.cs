using UnityEngine;

/// <summary>
/// Класичний PID-регулятор з анти-інтегральним віндапом.
/// Використовується для стабілізації кутів тангажу та рискання ракети.
/// </summary>
[System.Serializable]
public class PIDController
{
    public float Kp = 0.5f;
    public float Ki = 0.1f;
    public float Kd = 0.3f;

    private float integral;
    private float previousError;

    /// <summary>
    /// Обчислює керуючий сигнал PID-регулятора.
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

    /// <summary>
    /// Скидає внутрішній стан регулятора (інтеграл та попередню помилку).
    /// </summary>
    public void Reset()
    {
        integral = 0f;
        previousError = 0f;
    }
}
