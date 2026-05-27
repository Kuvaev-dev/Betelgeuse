using UnityEngine;

public class FuzzyLandingController : MonoBehaviour
{
    [Header("Fuzzy Logic - Налаштування")]
    [Range(0.5f, 4f)] public float heightGain = 2.0f;
    [Range(0.5f, 4f)] public float velocityGain = 1.6f;

    public bool isActive = true;

    public float CalculateThrust(float height, float verticalVelocity, float mass)
    {
        if (!isActive)
            return mass * 9.81f * 1.1f;

        float normHeight = Mathf.Clamp01(height / 3000f);
        float normVel = Mathf.Clamp(verticalVelocity / -100f, -1.5f, 1f);

        float thrustMult = 1.1f;

        // Покращена нечітка логіка
        if (normHeight > 0.7f)                    // Високо
            thrustMult = 1.05f;
        else if (normHeight > 0.4f)               // Середня висота
            thrustMult = 1.3f + normVel * 0.5f;
        else                                      // Низько — посадка
        {
            if (normVel < -1.0f)                  // Дуже швидке падіння
                thrustMult = 2.4f;
            else if (normVel < -0.6f)
                thrustMult = 1.95f;
            else if (normVel < -0.3f)
                thrustMult = 1.6f;
            else
                thrustMult = 1.25f;
        }

        return Mathf.Clamp(mass * 9.81f * thrustMult, 0f, 1200000f);
    }

    public Vector3 CalculateGimbal(float pitchError, float yawError)
    {
        if (!isActive) return Vector3.zero;

        float pitchCorr = Mathf.Clamp(pitchError * 1.15f, -30f, 30f);
        float yawCorr = Mathf.Clamp(yawError * 1.15f, -30f, 30f);

        return new Vector3(pitchCorr, 0, yawCorr);
    }
}