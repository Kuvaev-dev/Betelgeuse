using UnityEngine;

/// <summary>
/// Структура для зберігання та аналізу результатів посадки.
/// Містить ключові метрики та автоматично розраховує загальну оцінку успішності.
/// </summary>
[System.Serializable]
public class LandingMetrics
{
    [Header("Метрики посадки")]
    public float touchdownVelocity; // м/с — швидкість торкання платформи
    public float landingAngleError; // градуси — відхилення від вертикалі
    public float fuelRemaining; // кг — залишок палива
    public float maxAltitude; // м — максимальна висота під час польоту
    public float totalFlightTime; // с — загальний час польоту
    public bool isSuccessfulLanding; // чи посадка вважається успішною

    /// <summary>
    /// Загальна оцінка успішності посадки (0..100).
    /// Чим вища — тим краща посадка.
    /// </summary>
    public float SuccessScore
    {
        get
        {
            // Вага критеріїв: швидкість (50%), кут (30%), паливо (20%)
            float velScore = Mathf.Clamp01(1f - (touchdownVelocity / 5f)); // ідеально < 2 м/с
            float angleScore = Mathf.Clamp01(1f - (landingAngleError / 10f));
            float fuelScore = Mathf.Clamp01(fuelRemaining / 6000f);

            return (velScore * 0.5f + angleScore * 0.3f + fuelScore * 0.2f) * 100f;
        }
    }

    /// <summary>
    /// Виводить детальні результати посадки у консоль Unity.
    /// </summary>
    /// <param name="algorithmName">Назва використаного алгоритму керування</param>
    public void PrintResults(string algorithmName = "Unknown")
    {
        Debug.Log($"Результати посадки — {algorithmName}");
        Debug.Log($"Успішна посадка: {(isSuccessfulLanding ? "ТАК ✓" : "НІ ✗")}");
        Debug.Log($"Швидкість при торканні: {touchdownVelocity:F2} м/с");
        Debug.Log($"Кут нахилу при посадці: {landingAngleError:F2}°");
        Debug.Log($"Залишок палива: {fuelRemaining:F1} кг");
        Debug.Log($"Максимальна висота: {maxAltitude:F1} м");
        Debug.Log($"Час польоту: {totalFlightTime:F1} с");
        Debug.Log($"Загальна оцінка: {SuccessScore:F1} / 100");
    }
}