using UnityEngine;

/// <summary>
/// Гібрид Neuro-Fuzzy — ядро теми МКР:
/// сирий Sugeno + сирий MLP residual → один BlendThrust з профілем soft-landing.
/// Біля землі (h&lt;50 м) α,β→0: пріоритет інтерпретованих fuzzy-правил.
/// IdealLandingPresets зменшує residual для гарантованого soft-landing.
/// </summary>
public class HybridController : MonoBehaviour
{
    [Header("Hybrid Neuro-Fuzzy")]
    public bool isActive = true;
    [Range(0f, 0.5f)] public float neuralThrustBlend = 0.25f;
    [Range(0f, 0.45f)] public float neuralGimbalBlend = 0.2f;
    [Range(0.05f, 0.6f)] public float maxResidualMult = 0.3f;
    /// <summary>Вага smart (fuzzy/nn) vs чистий soft-landing профіль.</summary>
    [Range(0.2f, 0.8f)] public float smartWeight = 0.5f;
    [Range(0.2f, 0.9f)] public float maxDevFrac = 0.4f;

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

        // Сирі виходи (без внутрішнього blend) — blend лише один раз нижче
        float fuzzyThrust = fuzzy.EvaluateSugenoThrust(height, verticalVelocity, mass);
        Vector3 fuzzyGimbal = fuzzy.CalculateGimbal(pitchError, yawError, pitchRate, yawRate);

        float nnThrust = fuzzyThrust;
        Vector3 nnGimbal = fuzzyGimbal;
        if (neural != null && neural.isActive)
        {
            neural.CalculateControl(height, verticalVelocity, mass, currentThrust,
                pitchError, yawError, horizSpeed, blendWithProfile: false,
                out nnThrust, out nnGimbal);
        }

        float alpha = neuralThrustBlend;
        float beta = neuralGimbalBlend;
        // Біля землі — пріоритет fuzzy (інтерпретовані правила)
        if (height < 50f)
        {
            float t = Mathf.Clamp01(height / 50f);
            alpha *= t;
            beta *= t;
        }

        float smart = Mathf.Lerp(fuzzyThrust, nnThrust, alpha);
        // Обмежити NN-residual відносно fuzzy ДО blend (не після — інакше
        // cap може «перебити» термінал soft-landing h&lt;25 м).
        float maxRes = mass * g * maxResidualMult;
        smart = Mathf.Clamp(smart, fuzzyThrust - maxRes, fuzzyThrust + maxRes);
        thrust = SoftLandingGuidance.BlendThrust(profile, smart, smartWeight, mass, height, maxDevFrac);

        gimbalEuler = Vector3.Lerp(fuzzyGimbal, nnGimbal, beta);
        gimbalEuler.x = Mathf.Clamp(gimbalEuler.x, -18f, 18f);
        gimbalEuler.y = 0f;
        gimbalEuler.z = Mathf.Clamp(gimbalEuler.z, -18f, 18f);
    }
}
