using UnityEngine;

/// <summary>
/// Статична модель атмосфери та гравітації Землі.
/// Використовується для реалістичного моделювання аеродинамічного опору та зміни сили тяжіння з висотою.
/// </summary>
public static class AtmosphereModel
{
    /// <summary>
    /// Повертає густину повітря на заданій висоті (експоненціальна модель).
    /// На рівні моря ≈ 1.225 кг/м³.
    /// </summary>
    /// <param name="altitude">Висота над рівнем моря (м)</param>
    /// <returns>Густина повітря (кг/м³)</returns>
    public static float GetDensity(float altitude)
    {
        if (altitude < 0)
            return 1.225f;

        if (altitude > 85000f) // Мезопауза
            return 0f;

        // Експоненціальне зменшення густини з висотою
        return 1.225f * Mathf.Exp(-altitude * 0.0001184f);
    }

    /// <summary>
    /// Повертає прискорення вільного падіння на заданій висоті (з урахуванням обертання Землі не враховано).
    /// </summary>
    /// <param name="altitude">Висота над поверхнею (м)</param>
    /// <returns>Прискорення гравітації (м/с²)</returns>
    public static float GetGravity(float altitude)
    {
        const float R = 6371000f; // Середній радіус Землі (м)
        return 9.80665f * Mathf.Pow(R / (R + altitude), 2);
    }
}