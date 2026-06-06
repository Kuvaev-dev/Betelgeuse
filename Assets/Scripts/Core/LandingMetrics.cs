using UnityEngine;

/// <summary>
/// Структура для зберігання та аналізу результатів посадки.
/// Містить ключові метрики та автоматично розраховує загальну оцінку успішності.
/// </summary>
[System.Serializable]
public class LandingMetrics
{
    public float touchdownVelocity;      // м/с
    public float landingAngleError;      // градуси
    public float fuelRemaining;          // кг
    public float maxAltitude;            // м
    public float totalFlightTime;        // секунди
    public bool isSuccessfulLanding;

    /// <summary>
    /// Загальна оцінка успішності посадки (0..100).
    /// </summary>
    public float SuccessScore
    {
        get
        {
            float velScore = Mathf.Clamp01(1f - (touchdownVelocity / 5f));
            float angleScore = Mathf.Clamp01(1f - (landingAngleError / 10f));
            float fuelScore = Mathf.Clamp01(fuelRemaining / 6000f);
            return (velScore * 0.5f + angleScore * 0.3f + fuelScore * 0.2f) * 100f;
        }
    }

    /// <summary>
    /// Виводить детальні результати посадки у консоль Unity.
    /// </summary>
    public void PrintResults(string algorithmName = "Unknown")
    {
        Debug.Log($"Результати посадки — {algorithmName}");
        Debug.Log($"Успішна посадка: {(isSuccessfulLanding ? "ТАК" : "НІ")}");
        Debug.Log($"Швидкість при торканні: {touchdownVelocity:F2} м/с");
        Debug.Log($"Кут нахилу при посадці: {landingAngleError:F2}°");
        Debug.Log($"Залишок палива: {fuelRemaining:F1} кг");
        Debug.Log($"Максимальна висота: {maxAltitude:F1} м");
        Debug.Log($"Час польоту: {totalFlightTime:F1} с");
        Debug.Log($"Загальна оцінка: {SuccessScore:F1} / 100");
    }
}
