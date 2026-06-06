using UnityEngine;

/// <summary>
/// Нечіткий контролер посадки ракети на основі логіки Сугено (Sugeno).
/// Використовує фазифікацію висоти та вертикальної швидкості для обчислення необхідної тяги двигуна.
/// </summary>
public class FuzzyLandingController : MonoBehaviour
{
    [Header("Fuzzy Logic")]
    [Tooltip("Увімкнути/вимкнути нечітке керування")]
    public bool isActive = true;

    /// <summary>
    /// Обчислює необхідну тягу двигуна на основі поточної висоти, вертикальної швидкості та маси ракети.
    /// Використовує Mamdani-подібну фазифікацію та дефазифікацію методом Сугено (зважене середнє).
    /// </summary>
    /// <param name="height">Поточна висота над поверхнею (м)</param>
    /// <param name="verticalVelocity">Вертикальна швидкість (м/с, від'ємна при падінні)</param>
    /// <param name="mass">Поточна маса ракети (кг)</param>
    /// <returns>Необхідна тяга двигуна (Н)</returns>
    public float CalculateThrust(float height, float verticalVelocity, float mass)
    {
        if (!isActive)
            return mass * 9.81f * 1.1f; // Базова тяга при вимкненому контролері

        // 1. Фазифікація (Нормалізація та функції належності)

        // Нормалізація висоти (0..1 для діапазону 0..3000 м)
        float normHeight = Mathf.Clamp01(height / 3000f);

        // Нормалізація швидкості (0 = спокій, 1 = номінал, >1 = небезпечно)
        float normVel = Mathf.Clamp(verticalVelocity / -120f, 0f, 2f);

        // Функції належності для ВИСОТИ (Low, Medium, High)
        float hLow = Mathf.Clamp01(1f - normHeight / 0.3f);
        float hMedium = Mathf.Max(0f, 1f - Mathf.Abs(normHeight - 0.5f) / 0.25f);
        float hHigh = Mathf.Clamp01((normHeight - 0.6f) / 0.4f);

        // Функції належності для ШВИДКОСТІ (Slow, Medium, Fast)
        float vSlow = Mathf.Clamp01(1f - normVel / 0.5f);
        float vMedium = Mathf.Max(0f, 1f - Mathf.Abs(normVel - 1.0f) / 0.5f);
        float vFast = Mathf.Clamp01((normVel - 1.2f) / 0.8f);

        // 2. База правил (Rule Base) та дефазифікація (Sugeno)

        float sumWeights = 0f;
        float sumOutputs = 0f;

        // Локальна функція для активації правила
        void EvaluateRule(float membershipValue, float outputThrustMultiplier)
        {
            if (membershipValue > 0f)
            {
                sumWeights += membershipValue;
                sumOutputs += membershipValue * outputThrustMultiplier;
            }
        }

        // Активація нечітких правил (продукційні правила)
        EvaluateRule(hHigh, 1.02f);                        // Високо — економимо паливо (мала тяга)
        EvaluateRule(hMedium * vSlow, 1.08f);              // Середня висота + нормальна швидкість
        EvaluateRule(hMedium * vMedium, 1.40f);            // Середня висота + середня швидкість
        EvaluateRule(hMedium * vFast, 1.95f);              // Середня висота + швидке падіння
        EvaluateRule(hLow * vSlow, 1.15f);                 // Низько + безпечна швидкість
        EvaluateRule(hLow * vMedium, 1.85f);               // Низько + помірна швидкість
        EvaluateRule(hLow * vFast, 2.70f);                 // Критично: низько + швидко — максимальний реверс

        // Дефазифікація — зважене середнє (Sugeno)
        float thrustMult = sumWeights > 0f ? (sumOutputs / sumWeights) : 1.1f;

        return mass * 9.81f * thrustMult;
    }

    /// <summary>
    /// Обчислює необхідне відхилення вектора тяги (gimbal) для корекції кутів тангажу та рискання.
    /// </summary>
    /// <param name="pitchError">Помилка тангажу (градуси)</param>
    /// <param name="yawError">Помилка рискання (градуси)</param>
    /// <returns>Вектор корекції кутів (pitch, 0, yaw) у градусах</returns>
    public Vector3 CalculateGimbal(float pitchError, float yawError)
    {
        if (!isActive)
            return Vector3.zero;

        float pitchCorr = Mathf.Clamp(pitchError * 1.2f, -30f, 30f);
        float yawCorr = Mathf.Clamp(yawError * 1.2f, -30f, 30f);

        return new Vector3(pitchCorr, 0, yawCorr);
    }
}