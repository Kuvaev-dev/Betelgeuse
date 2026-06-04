using UnityEngine;

public class NeuralController : MonoBehaviour
{
    [Header("Neural Network Controller")]
    public bool isActive = true;

    // Проста нейронна мережа (один прихований шар)
    private float[] weightsInputHidden = { 0.8f, -1.2f, 0.6f, 1.1f }; // висота, швидкість, кут, попередня тяга
    private float[] weightsHiddenOutput = { 1.3f, -0.9f };

    public float CalculateThrust(float height, float verticalVelocity, float mass, float currentThrust)
    {
        if (!isActive) return mass * 9.81f * 1.1f;

        // Нормалізація
        float h = Mathf.Clamp01(height / 2500f);
        float v = Mathf.Clamp(verticalVelocity / -100f, -2f, 1f);
        float angle = Mathf.Abs(Vector3.Angle(transform.up, Vector3.up)) / 45f;

        // Простий MLP (1 прихований нейрон)
        float hidden = h * weightsInputHidden[0] + v * weightsInputHidden[1] +
                       angle * weightsInputHidden[2] + (currentThrust / (mass * 9.81f)) * weightsInputHidden[3];

        hidden = Mathf.Tan(hidden); // Активація

        float output = hidden * weightsHiddenOutput[0] + 1.0f * weightsHiddenOutput[1];

        float thrustMult = Mathf.Clamp(output + 1.2f, 0.8f, 2.8f);

        return mass * 9.81f * thrustMult;
    }

    public Vector3 CalculateGimbal(float pitchError, float yawError)
    {
        if (!isActive) return Vector3.zero;
        return new Vector3(pitchError * 0.9f, 0, yawError * 0.9f);
    }
}