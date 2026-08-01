using UnityEngine;

/// <summary>
/// Спрощена модель атмосфери та гравітації Землі.
/// Густина — експоненційна апроксимація; g(h) — закон обернених квадратів.
/// Достатня для порівняння GNC-алгоритмів у симуляторі посадки.
/// </summary>
public static class AtmosphereModel
{
    /// <summary>
    /// Густина повітря ρ(h), кг/м³. На h&lt;0 — рівень моря; вище 85 км — 0.
    /// </summary>
    public static float GetDensity(float altitude)
    {
        if (altitude < 0) return 1.225f;
        if (altitude > 85000f) return 0f;
        return 1.225f * Mathf.Exp(-altitude * 0.0001184f);
    }

    /// <summary>
    /// Прискорення вільного падіння g(h), м/с² (R_Earth ≈ 6371 км).
    /// </summary>
    public static float GetGravity(float altitude)
    {
        const float R = 6371000f;
        return 9.80665f * Mathf.Pow(R / (R + altitude), 2);
    }
}
