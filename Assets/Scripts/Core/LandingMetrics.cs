using UnityEngine;
using System.Text;

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

    /// <summary>Людською мовою: чому успіх / невдача.</summary>
    public string BuildUserSummary(float maxV = 3.5f, float maxA = 7f, float maxM = 25f, float maxH = 5f)
    {
        var sb = new StringBuilder();
        if (isSuccessfulLanding)
        {
            sb.AppendLine("ПОСАДКУ ВИКОНАНО УСПІШНО");
            sb.AppendLine();
            sb.AppendLine($"• Швидкість приземлення: {touchdownVelocity:F1} м/с  (норма < {maxV})");
            sb.AppendLine($"• Нахил корпусу: {landingAngleError:F1}°  (норма < {maxA}°)");
            sb.AppendLine($"• Відхилення від pad: {horizontalMiss:F1} м  (норма < {maxM} м)");
            sb.AppendLine($"• Бічна швидкість: {horizontalSpeed:F1} м/с  (норма < {maxH})");
            sb.AppendLine($"• Оцінка: {SuccessScore:F0} / 100");
            return sb.ToString().TrimEnd();
        }

        sb.AppendLine("ПОСАДКА НЕВДАЛА");
        sb.AppendLine();
        sb.AppendLine("Причини:");
        if (timedOut)
            sb.AppendLine("• Час симуляції вичерпано (ракета не сіла вчасно)");
        if (touchdownVelocity >= maxV)
            sb.AppendLine($"• Занадто швидке приземлення: {touchdownVelocity:F1} м/с  (треба < {maxV})");
        if (landingAngleError >= maxA)
            sb.AppendLine($"• Занадто великий нахил: {landingAngleError:F1}°  (треба < {maxA}°)");
        if (horizontalMiss >= maxM)
            sb.AppendLine($"• Промах повз pad: {horizontalMiss:F1} м  (треба < {maxM} м)");
        if (horizontalSpeed >= maxH)
            sb.AppendLine($"• Велика бічна швидкість: {horizontalSpeed:F1} м/с  (треба < {maxH})");
        sb.AppendLine();
        sb.AppendLine($"Оцінка: {SuccessScore:F0} / 100");
        return sb.ToString().TrimEnd();
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
