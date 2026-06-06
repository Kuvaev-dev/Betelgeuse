using UnityEngine;

/// <summary>
/// Класичний PID-регулятор з анти-інтегральним віндапом.
/// Використовується для стабілізації кутів тангажу та рискання ракети.
/// </summary>
[System.Serializable]
public class PIDController
{
    [Header("PID Коефіцієнти")]
    public float Kp = 0.5f; // Пропорційний
    public float Ki = 0.1f; // Інтегральний
    public float Kd = 0.3f; // Диференціальний

    // ВНутрішній стан регулятора (не серіалізується, скидається при перезапуску симуляції)
    private float integral;
    private float previousError;

    /// <summary>
    /// Обчислює керуючий сигнал PID-регулятора.
    /// </summary>
    /// <param name="setpoint">Бажане значення (зазвичай 0)</param>
    /// <param name="currentValue">Поточне значення (помилка = setpoint - currentValue)</param>
    /// <param name="dt">Крок часу (с)</param>
    /// <returns>Керуючий сигнал (вихід PID)</returns>
    public float Calculate(float setpoint, float currentValue, float dt)
    {
        if (dt <= 0f)
            return 0f;

        float error = setpoint - currentValue;
        integral += error * dt;

        // Анти-інтегральний віндап (обмеження інтегральної складової)
        integral = Mathf.Clamp(integral, -15f, 15f);

        float derivative = (error - previousError) / dt;
        previousError = error;

        return Kp * error + Ki * integral + Kd * derivative;
    }

    /// <summary>
    /// Скидає внутрішній стан регулятора (інтеграл та попередню помилку).
    /// Викликається при перезапуску симуляції.
    /// </summary>
    public void Reset()
    {
        integral = 0f;
        previousError = 0f;
    }
}