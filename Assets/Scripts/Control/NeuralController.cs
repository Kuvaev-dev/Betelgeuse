using UnityEngine;

public class NeuralController : MonoBehaviour
{
    [Header("Neural Network (з навчанням)")]
    public bool isActive = true;
    public bool enableTraining = true;

    // Вага (можна буде навчати)
    [Header("Ваги мережі")]
    public float[] weightsInputHidden = { 0.8f, -1.2f, 0.6f, 1.1f };
    public float[] weightsHiddenOutput = { 1.3f, -0.9f };

    [Header("Параметри навчання")]
    public float learningRate = 0.05f;

    private float lastError = 0f;

    public float CalculateThrust(float height, float verticalVelocity, float mass, float currentThrust, float angleError)
    {
        if (!isActive) return mass * 9.81f * 1.1f;

        float h = Mathf.Clamp01(height / 2500f);
        float v = Mathf.Clamp(verticalVelocity / -100f, -2f, 1f);
        float a = Mathf.Clamp01(Mathf.Abs(angleError) / 45f);
        float t = currentThrust / (mass * 9.81f);

        float hidden = h * weightsInputHidden[0] + v * weightsInputHidden[1] +
                       a * weightsInputHidden[2] + t * weightsInputHidden[3];

        hidden = (float)System.Math.Tanh(hidden);

        float output = hidden * weightsHiddenOutput[0] + weightsHiddenOutput[1];
        float thrustMult = Mathf.Clamp(output + 1.2f, 0.8f, 2.8f);

        return mass * 9.81f * thrustMult;
    }

    public Vector3 CalculateGimbal(float pitchError, float yawError)
    {
        if (!isActive) return Vector3.zero;
        return new Vector3(pitchError * 0.9f, 0, yawError * 0.9f);
    }

    /// <summary>
    /// Просте навчання на основі помилки посадки
    /// </summary>
    public void Train(float touchdownVelocity, float angleError, float fuelRemaining)
    {
        if (!enableTraining) return;

        float error = touchdownVelocity * 0.6f + angleError * 0.3f + (5000f - fuelRemaining) / 1000f * 0.1f;

        // Якщо помилка менша за попередню — підкріплюємо ваги
        if (error < lastError)
        {
            for (int i = 0; i < weightsInputHidden.Length; i++)
                weightsInputHidden[i] += (Random.value - 0.5f) * learningRate;

            for (int i = 0; i < weightsHiddenOutput.Length; i++)
                weightsHiddenOutput[i] += (Random.value - 0.5f) * learningRate * 0.5f;
        }

        lastError = error;
    }
}