using UnityEngine;

/// <summary>
/// Гібридний Neuro-Fuzzy — тема магістерської роботи.
/// База: soft-landing + Sugeno; корекція: обмежений residual MLP.
/// </summary>
public class HybridController : MonoBehaviour
{
    [Header("Hybrid Neuro-Fuzzy")]
    public bool isActive = true;
    [Range(0f, 0.45f)] public float neuralThrustBlend = 0.22f;
    [Range(0f, 0.40f)] public float neuralGimbalBlend = 0.18f;
    [Range(0.05f, 0.55f)] public float maxResidualMult = 0.28f;

    public FuzzyLandingController fuzzy;
    public NeuralController neural;

    void Awake()
    {
        if (fuzzy == null) fuzzy = GetComponent<FuzzyLandingController>();
        if (neural == null) neural = GetComponent<NeuralController>();
    }

    public void CalculateControl(
        float height, float verticalVelocity, float mass, float currentThrust,
        float pitchError, float yawError, float pitchRate, float yawRate, float horizSpeed,
        out float thrust, out Vector3 gimbalEuler)
    {
        float g = AtmosphereModel.GetGravity(Mathf.Max(0f, height));
        float profile = SoftLandingGuidance.ProfileThrust(height, verticalVelocity, mass);

        if (!isActive || fuzzy == null)
        {
            thrust = profile;
            gimbalEuler = SoftLandingGuidance.AttitudeGimbal(pitchError, yawError, pitchRate, yawRate);
            return;
        }

        float fuzzyThrust = fuzzy.CalculateThrust(height, verticalVelocity, mass);
        Vector3 fuzzyGimbal = fuzzy.CalculateGimbal(pitchError, yawError, pitchRate, yawRate);

        float nnThrust = fuzzyThrust;
        Vector3 nnGimbal = fuzzyGimbal;
        if (neural != null && neural.isActive)
        {
            neural.CalculateControl(height, verticalVelocity, mass, currentThrust,
                pitchError, yawError, horizSpeed, out nnThrust, out nnGimbal);
        }

        float alpha = neuralThrustBlend;
        float beta = neuralGimbalBlend;
        if (height < 45f)
        {
            float t = Mathf.Clamp01(height / 45f);
            alpha *= t;
            beta *= t;
        }

        float smart = Mathf.Lerp(fuzzyThrust, nnThrust, alpha);
        float maxRes = mass * g * maxResidualMult;
        // Тришаровий захист: profile ↔ fuzzy/nn
        float blended = SoftLandingGuidance.BlendThrust(profile, smart, 0.55f, mass, height);
        thrust = Mathf.Clamp(blended, fuzzyThrust - maxRes, fuzzyThrust + maxRes);
        thrust = Mathf.Clamp(thrust, profile * 0.7f, profile * 1.45f + mass * g * 0.2f);

        gimbalEuler = Vector3.Lerp(fuzzyGimbal, nnGimbal, beta);
        gimbalEuler.x = Mathf.Clamp(gimbalEuler.x, -28f, 28f);
        gimbalEuler.y = 0f;
        gimbalEuler.z = Mathf.Clamp(gimbalEuler.z, -28f, 28f);
    }
}
