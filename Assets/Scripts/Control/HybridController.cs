using UnityEngine;

/// <summary>
/// Гібридний Neuro-Fuzzy контролер — центральна ідея магістерської роботи.
/// База: zero-order Sugeno (інтерпретовані правила);
/// корекція: обмежений residual від MLP, щоб мережа не «зламала» стійку fuzzy-базу.
/// thrust = clamp(lerp(fuzzy, nn, α), fuzzy ± residualMax)
/// </summary>
public class HybridController : MonoBehaviour
{
    [Header("Hybrid Neuro-Fuzzy")]
    public bool isActive = true;
    /// <summary>Частка нейро-корекції тяги (α ≈ 0.20).</summary>
    [Range(0f, 0.45f)] public float neuralThrustBlend = 0.20f;
    /// <summary>Частка нейро-корекції gimbal (β ≈ 0.15).</summary>
    [Range(0f, 0.40f)] public float neuralGimbalBlend = 0.15f;
    /// <summary>Макс. residual відносно mg (захист від нестабільної NN).</summary>
    [Range(0.05f, 0.55f)] public float maxResidualMult = 0.30f;

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
        if (!isActive || fuzzy == null)
        {
            thrust = mass * g * 1.1f;
            gimbalEuler = Vector3.zero;
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

        float blended = Mathf.Lerp(fuzzyThrust, nnThrust, neuralThrustBlend);
        float maxRes = mass * g * maxResidualMult;
        thrust = Mathf.Clamp(blended, fuzzyThrust - maxRes, fuzzyThrust + maxRes);

        gimbalEuler = Vector3.Lerp(fuzzyGimbal, nnGimbal, neuralGimbalBlend);
        gimbalEuler.x = Mathf.Clamp(gimbalEuler.x, -28f, 28f);
        gimbalEuler.y = 0f;
        gimbalEuler.z = Mathf.Clamp(gimbalEuler.z, -28f, 28f);
    }
}
