using UnityEngine;

public class FuzzyLandingController : MonoBehaviour
{
    [Header("Fuzzy Logic")]
    public bool isActive = true;

    public float CalculateThrust(float height, float verticalVelocity, float mass)
    {
        if (!isActive) return mass * 9.81f * 1.1f;

        float normHeight = Mathf.Clamp01(height / 3000f);
        float normVel = Mathf.Clamp(verticalVelocity / -120f, -2f, 1f);

        float thrustMult = 1.1f;

        if (normHeight > 0.75f)           // Високо
            thrustMult = 1.04f;
        else if (normHeight > 0.45f)      // Середня висота
            thrustMult = 1.35f + normVel * 0.55f;
        else                              // Посадка (критична фаза)
        {
            if (normVel < -1.2f) thrustMult = 2.5f;
            else if (normVel < -0.8f) thrustMult = 2.1f;
            else if (normVel < -0.4f) thrustMult = 1.75f;
            else thrustMult = 1.35f;
        }

        return mass * 9.81f * thrustMult;
    }

    public Vector3 CalculateGimbal(float pitchError, float yawError)
    {
        if (!isActive) return Vector3.zero;
        float pitchCorr = Mathf.Clamp(pitchError * 1.2f, -30f, 30f);
        float yawCorr = Mathf.Clamp(yawError * 1.2f, -30f, 30f);
        return new Vector3(pitchCorr, 0, yawCorr);
    }
}