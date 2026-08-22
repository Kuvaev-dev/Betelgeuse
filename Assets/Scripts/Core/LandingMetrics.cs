using UnityEngine;
using System.Text;

/// <summary>
/// Метрики однієї посадки для UI, експорту та Monte-Carlo порівняння.
/// SuccessScore ∈ [0;100] — зважена якість touchdown (без timeout = 0).
/// </summary>
[System.Serializable]
public class LandingMetrics
{
    /// <summary>Модуль вертикальної швидкості в момент торкання, м/с.</summary>
    public float touchdownVelocity;
    /// <summary>Кут між віссю корпусу та вертикаллю, градуси.</summary>
    public float landingAngleError;
    /// <summary>Залишок палива після посадки, кг.</summary>
    public float fuelRemaining;
    /// <summary>Максимальна висота за політ, м.</summary>
    public float maxAltitude;
    /// <summary>Тривалість польоту, с.</summary>
    public float totalFlightTime;
    /// <summary>Горизонтальна відстань до центру pad, м.</summary>
    public float horizontalMiss;
    /// <summary>Модуль горизонтальної швидкості на touchdown, м/с.</summary>
    public float horizontalSpeed;
    /// <summary>true, якщо вичерпано maxSimulationTime.</summary>
    public bool timedOut;
    /// <summary>true, якщо всі критерії soft-landing виконані.</summary>
    public bool isSuccessfulLanding;

    /// <summary>
    /// Інтегральна оцінка якості посадки (0…100).
    /// Ваги: швидкість 35%, кут 25%, паливо 15%, промах 15%, бічна 10%.
    /// </summary>
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

    /// <summary>Текстовий висновок UA/EN для експорту та діалогів.</summary>
    /// <param name="includeTitle">false — якщо заголовок уже в UI</param>
    public string BuildUserSummary(float maxV = 3.5f, float maxA = 7f, float maxM = 25f, float maxH = 5f,
        bool includeTitle = true)
    {
        bool uk = UILocale.IsUK;
        var sb = new StringBuilder();
        if (isSuccessfulLanding)
        {
            if (includeTitle)
            {
                sb.AppendLine(uk ? "ПОСАДКУ ВИКОНАНО УСПІШНО" : "LANDING SUCCESSFUL");
                sb.AppendLine();
            }
            if (uk)
            {
                sb.AppendLine($"• Швидкість: {touchdownVelocity:F1} м/с  (норма < {maxV})");
                sb.AppendLine($"• Нахил: {landingAngleError:F1}°  (норма < {maxA}°)");
                sb.AppendLine($"• Промах: {horizontalMiss:F1} м  (норма < {maxM} м)");
                sb.AppendLine($"• Бічна V: {horizontalSpeed:F1} м/с  (норма < {maxH})");
                sb.AppendLine($"• Оцінка: {SuccessScore:F0} / 100");
            }
            else
            {
                sb.AppendLine($"• Velocity: {touchdownVelocity:F1} m/s  (limit < {maxV})");
                sb.AppendLine($"• Tilt: {landingAngleError:F1}°  (limit < {maxA}°)");
                sb.AppendLine($"• Miss: {horizontalMiss:F1} m  (limit < {maxM} m)");
                sb.AppendLine($"• Lateral V: {horizontalSpeed:F1} m/s  (limit < {maxH})");
                sb.AppendLine($"• Score: {SuccessScore:F0} / 100");
            }
            return sb.ToString().TrimEnd();
        }

        if (includeTitle)
        {
            sb.AppendLine(uk ? "ПОСАДКА НЕВДАЛА" : "LANDING FAILED");
            sb.AppendLine();
        }
        sb.AppendLine(uk ? "Причини:" : "Reasons:");
        if (timedOut)
            sb.AppendLine(uk ? "• Час симуляції вичерпано" : "• Simulation time exhausted");
        if (touchdownVelocity >= maxV)
            sb.AppendLine(uk
                ? $"• Швидкість {touchdownVelocity:F1} м/с  (треба < {maxV})"
                : $"• Velocity {touchdownVelocity:F1} m/s  (need < {maxV})");
        if (landingAngleError >= maxA)
            sb.AppendLine(uk
                ? $"• Нахил {landingAngleError:F1}°  (треба < {maxA}°)"
                : $"• Tilt {landingAngleError:F1}°  (need < {maxA}°)");
        if (horizontalMiss >= maxM)
            sb.AppendLine(uk
                ? $"• Промах {horizontalMiss:F1} м  (треба < {maxM} м)"
                : $"• Miss {horizontalMiss:F1} m  (need < {maxM} m)");
        if (horizontalSpeed >= maxH)
            sb.AppendLine(uk
                ? $"• Бічна V {horizontalSpeed:F1} м/с  (треба < {maxH})"
                : $"• Lateral V {horizontalSpeed:F1} m/s  (need < {maxH})");
        sb.AppendLine();
        sb.AppendLine(uk
            ? $"Оцінка: {SuccessScore:F0} / 100"
            : $"Score: {SuccessScore:F0} / 100");
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
