using UnityEngine;

/// <summary>
/// Статична модель атмосфери та гравітації Землі.
/// Використовується для реалістичного моделювання аеродинамічного опору та зміни сили тяжіння з висотою.
/// </summary>
public static class AtmosphereModel
{
    /// <summary>
    /// Повертає густину повітря на заданій висоті (експоненціальна модель).
    /// </summary>
    public static float GetDensity(float altitude)
    {
        if (altitude < 0) return 1.225f;
        if (altitude > 85000f) return 0f;
        return 1.225f * Mathf.Exp(-altitude * 0.0001184f);
    }

    /// <summary>
    /// Повертає прискорення вільного падіння на заданій висоті.
    /// </summary>
    public static float GetGravity(float altitude)
    {
        const float R = 6371000f;
        return 9.80665f * Mathf.Pow(R / (R + altitude), 2);
    }
}
