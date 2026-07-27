using UnityEngine;

/// <summary>
/// Метрики посадки для аналізу та Monte-Carlo порівняння алгоритмів.
/// </summary>
[System.Serializable]
public class LandingMetrics
{
    public float touchdownVelocity;
    public float landingAngleError;
    public float fuelRemaining;
    public float maxAltitude;
    public float totalFlightTime;
    public float horizontalMiss;
    public float horizontalSpeed;
    public bool timedOut;
    public bool isSuccessfulLanding;

    /// <summary>Комплексна оцінка 0..100 (швидкість, кут, паливо, промах).</summary>
    public float SuccessScore
    {
        get
        {
            if (timedOut) return 0f;
            float velScore = Mathf.Clamp01(1f - touchdownVelocity / 5f);
            float angleScore = Mathf.Clamp01(1f - landingAngleError / 10f);
            float fuelScore = Mathf.Clamp01(fuelRemaining / 6000f);
            float missScore = Mathf.Clamp01(1f - horizontalMiss / 30f);
            float hVelScore = Mathf.Clamp01(1f - horizontalSpeed / 8f);
            return (velScore * 0.35f + angleScore * 0.25f + fuelScore * 0.15f
                    + missScore * 0.15f + hVelScore * 0.10f) * 100f;
        }
    }

    public void PrintResults(string algorithmName = "Unknown")
    {
        Debug.Log($"── Результати посадки — {algorithmName} ──");
        Debug.Log($"Успішна посадка: {(isSuccessfulLanding ? "ТАК" : "НІ")}" +
                  (timedOut ? " (timeout)" : ""));
        Debug.Log($"V_touch: {touchdownVelocity:F2} м/с | V_horiz: {horizontalSpeed:F2} м/с");
        Debug.Log($"Кут нахилу: {landingAngleError:F2}° | Промах: {horizontalMiss:F1} м");
        Debug.Log($"Паливо: {fuelRemaining:F1} кг | H_max: {maxAltitude:F1} м | t: {totalFlightTime:F1} с");
        Debug.Log($"Оцінка: {SuccessScore:F1} / 100");
    }
}
