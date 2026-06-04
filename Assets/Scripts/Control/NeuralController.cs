using UnityEngine;

public class NeuralController : MonoBehaviour
{
    [Header("Neural Network Controller (Demo)")]
    public bool isActive = true;

    // Вага
    private float[] weights = { 0.8f, -1.2f, 0.6f, 1.1f };
    private float[] outputWeights = { 1.3f, -0.9f };

    public float CalculateThrust(float height, float verticalVelocity, float mass, float currentThrust, float angleError)
    {
        if (!isActive) return mass * 9.81f * 1.1f;

        float h = Mathf.Clamp01(height / 2500f);
        float v = Mathf.Clamp(verticalVelocity / -100f, -2f, 1f);
        float a = Mathf.Clamp01(Mathf.Abs(angleError) / 45f);
        float t = currentThrust / (mass * 9.81f);

        float hidden = h * weights[0] + v * weights[1] + a * weights[2] + t * weights[3];
        hidden = (float)System.Math.Tanh(hidden);

        float output = hidden * outputWeights[0] + outputWeights[1];
        float thrustMult = Mathf.Clamp(output + 1.2f, 0.8f, 2.8f);

        return mass * 9.81f * thrustMult;
    }

    public Vector3 CalculateGimbal(float pitchError, float yawError)
    {
        if (!isActive) return Vector3.zero;
        return new Vector3(pitchError * 0.9f, 0, yawError * 0.9f);
    }
}