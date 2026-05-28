using UnityEngine;

[System.Serializable]
public class LandingMetrics
{
    public float touchdownVelocity;      // м/с
    public float landingAngleError;      // градуси
    public float fuelRemaining;          // кг
    public float maxAltitude;            // м
    public float totalFlightTime;        // секунди
    public bool isSuccessfulLanding;

    public float SuccessScore
    {
        get
        {
            float velScore = Mathf.Clamp01(1f - (touchdownVelocity / 5f));     // ідеально < 2 м/с
            float angleScore = Mathf.Clamp01(1f - (landingAngleError / 10f));
            float fuelScore = Mathf.Clamp01(fuelRemaining / 6000f);

            return (velScore * 0.5f + angleScore * 0.3f + fuelScore * 0.2f) * 100f;
        }
    }

    public void PrintResults(string algorithmName = "Unknown")
    {
        Debug.Log("══════════════════════════════════════");
        Debug.Log($"РЕЗУЛЬТАТИ ПОСАДКИ — {algorithmName}");
        Debug.Log($"Успішна посадка: {(isSuccessfulLanding ? "ТАК" : "НІ")}");
        Debug.Log($"Швидкість при торканні: {touchdownVelocity:F2} м/с");
        Debug.Log($"Кут нахилу при посадці: {landingAngleError:F2}°");
        Debug.Log($"Залишок палива: {fuelRemaining:F1} кг");
        Debug.Log($"Максимальна висота: {maxAltitude:F1} м");
        Debug.Log($"Час польоту: {totalFlightTime:F1} с");
        Debug.Log($"Загальна оцінка: {SuccessScore:F1} / 100");
        Debug.Log("══════════════════════════════════════");
    }
}