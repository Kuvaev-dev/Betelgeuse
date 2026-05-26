using UnityEngine;

public class FuzzyLandingController : MonoBehaviour
{
    [Header("Налаштування Fuzzy Logic")]
    [Range(0.5f, 3f)] public float heightImportance = 1.8f;
    [Range(0.5f, 3f)] public float velocityImportance = 1.4f;

    [Header("Режим")]
    public bool isActive = true;

    /// <summary>
    /// Основний метод нечіткої логіки для розрахунку тяги
    /// </summary>
    public float CalculateThrust(float currentHeight, float verticalVelocity, float currentMass)
    {
        if (!isActive)
            return currentMass * 9.81f * 1.1f;

        // Нормалізація вхідних величин
        float normalizedHeight = Mathf.Clamp01(currentHeight / 2500f);
        float normalizedVelocity = Mathf.Clamp(verticalVelocity / -80f, -1f, 1f); // негативна = падаємо

        float thrustMultiplier = 1.1f;

        // Нечіткі правила (спрощена Mamdani-подібна логіка)
        if (normalizedHeight > 0.65f) // Високо — економимо паливо
        {
            thrustMultiplier = 1.05f;
        }
        else if (normalizedHeight > 0.35f) // Середня висота
        {
            thrustMultiplier = 1.25f + normalizedVelocity * 0.4f;
        }
        else // Низько — критична фаза посадки
        {
            if (normalizedVelocity < -0.75f)        // Дуже швидко падаємо
                thrustMultiplier = 2.3f;
            else if (normalizedVelocity < -0.4f)    // Середня швидкість падіння
                thrustMultiplier = 1.85f;
            else
                thrustMultiplier = 1.45f;
        }

        return Mathf.Clamp(currentMass * 9.81f * thrustMultiplier, 0f, state.maxThrust * 1.1f);
    }

    /// <summary>
    /// Нечітке керування вектором тяги (gimbal)
    /// </summary>
    public Vector3 CalculateGimbal(float pitchError, float yawError)
    {
        if (!isActive)
            return Vector3.zero;

        // Розмиття помилок
        float pitchCorr = Mathf.Clamp(pitchError * 1.1f, -28f, 28f);
        float yawCorr = Mathf.Clamp(yawError * 1.1f, -28f, 28f);

        return new Vector3(pitchCorr, 0, yawCorr);
    }

    [HideInInspector] public RocketState state;
}