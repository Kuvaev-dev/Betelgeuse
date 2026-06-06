using UnityEngine;
using System.IO;

/// <summary>
/// Нейромережевий контролер посадки ракети з еволюційним навчанням (спрямований стохастичний пошук).
/// Мережа: 4 входи → 1 прихований нейрон (tanh) → 1 вихід.
/// Ваги оптимізуються методом мутацій на основі найкращого результату (best weights).
/// </summary>
public class NeuralController : MonoBehaviour
{
    [Header("Neural Network (Еволюційний випадковий пошук)")]
    [Tooltip("Увімкнути/вимкнути нейромережеве керування")]
    public bool isActive = true;

    [Tooltip("Дозволити онлайн-навчання (мутацію ваг) після кожної посадки")]
    public bool enableTraining = true;

    [Header("Поточні ваги мережі")]
    public float[] weightsInputHidden = { 0.8f, -1.2f, 0.6f, 1.1f };
    public float[] weightsHiddenOutput = { 1.3f, -0.9f };

    [Header("Параметри навчання")]
    [Tooltip("Швидкість мутації (learning rate)")]
    public float learningRate = 0.05f;

    // Зберігання кращих ваг
    private float bestError = float.MaxValue;
    private float[] bestWeightsInputHidden;
    private float[] bestWeightsHiddenOutput;

    private string weightsFilePath;

    private void Awake()
    {
        weightsFilePath = Path.Combine(Application.dataPath, "..", "BestWeights_Neural.json");

        // Ініціалізація масивів для найкращих ваг
        bestWeightsInputHidden = (float[])weightsInputHidden.Clone();
        bestWeightsHiddenOutput = (float[])weightsHiddenOutput.Clone();
    }

    /// <summary>
    /// Обчислює тягу за допомогою простої нейромережі прямого поширення.
    /// Входи: нормалізована висота, швидкість, помилка кута, поточна тяга.
    /// </summary>
    /// <param name="height">Висота (м)</param>
    /// <param name="verticalVelocity">Вертикальна швидкість (м/с)</param>
    /// <param name="mass">Маса ракети (кг)</param>
    /// <param name="currentThrust">Поточна тяга (Н)</param>
    /// <param name="angleError">Помилка орієнтації (градуси)</param>
    /// <returns>Необхідна тяга двигуна (Н)</returns>
    public float CalculateThrust(float height, float verticalVelocity, float mass, float currentThrust, float angleError)
    {
        if (!isActive)
            return mass * 9.81f * 1.1f;

        // Нормалізація входів
        float h = Mathf.Clamp01(height / 2500f);
        float v = Mathf.Clamp(verticalVelocity / -100f, -2f, 1f);
        float a = Mathf.Clamp01(Mathf.Abs(angleError) / 45f);
        float t = currentThrust / (mass * 9.81f);

        // Пряме поширення (forward pass)
        float hidden = h * weightsInputHidden[0] + v * weightsInputHidden[1] +
                       a * weightsInputHidden[2] + t * weightsInputHidden[3];

        // Активація прихованого шару — гіперболічний тангенс (стабільний)
        hidden = (float)System.Math.Tanh(hidden);

        // Вихідний шар (лінійний)
        float output = hidden * weightsHiddenOutput[0] + weightsHiddenOutput[1];

        // Обмеження множника тяги
        float thrustMult = Mathf.Clamp(output + 1.2f, 0.8f, 2.8f);

        return mass * 9.81f * thrustMult;
    }

    /// <summary>
    /// Обчислює корекцію вектора тяги (gimbal) для нейромережевого режиму.
    /// </summary>
    public Vector3 CalculateGimbal(float pitchError, float yawError)
    {
        if (!isActive)
            return Vector3.zero;

        return new Vector3(pitchError * 0.9f, 0, yawError * 0.9f);
    }

    /// <summary>
    /// Онлайн-навчання: мутація ваг на основі найкращого результату.
    /// Фітнес-функція враховує швидкість торкання, кут нахилу та залишок палива.
    /// </summary>
    /// <param name="touchdownVelocity">Швидкість при посадці (м/с)</param>
    /// <param name="angleError">Кут нахилу при посадці (градуси)</param>
    /// <param name="fuelRemaining">Залишок палива (кг)</param>
    public void Train(float touchdownVelocity, float angleError, float fuelRemaining)
    {
        if (!enableTraining)
            return;

        // Фітнес-функція помилки (чим менша — тим краще)
        float currentRunError = touchdownVelocity * 0.6f +
                                angleError * 0.3f +
                                (5000f - fuelRemaining) / 1000f * 0.1f;

        // Якщо поточний запуск кращий — зберігаємо ваги як еталонні
        if (currentRunError < bestError)
        {
            bestError = currentRunError;
            bestWeightsInputHidden = (float[])weightsInputHidden.Clone();
            bestWeightsHiddenOutput = (float[])weightsHiddenOutput.Clone();
            SaveBestWeights();

            Debug.Log($"🎯 Знайдено кращу конфігурацію ваг! Помилка: {bestError:F4}");
        }

        // Мутація всіх ваг на основі найкращих (directed stochastic search)
        for (int i = 0; i < weightsInputHidden.Length; i++)
            weightsInputHidden[i] = bestWeightsInputHidden[i] + (Random.value - 0.5f) * learningRate;

        for (int i = 0; i < weightsHiddenOutput.Length; i++)
            weightsHiddenOutput[i] = bestWeightsHiddenOutput[i] + (Random.value - 0.5f) * learningRate * 0.5f;
    }

    /// <summary>
    /// Зберігає найкращі ваги у JSON-файл (для перенесення між запусками).
    /// </summary>
    public void SaveBestWeights()
    {
        string json = JsonUtility.ToJson(new NeuralWeights
        {
            weightsInputHidden = this.bestWeightsInputHidden,
            weightsHiddenOutput = this.bestWeightsHiddenOutput
        }, true);

        File.WriteAllText(weightsFilePath, json);
    }

    /// <summary>
    /// Завантажує найкращі ваги з JSON-файлу при старті симуляції.
    /// </summary>
    public void LoadBestWeights()
    {
        if (File.Exists(weightsFilePath))
        {
            string json = File.ReadAllText(weightsFilePath);
            NeuralWeights data = JsonUtility.FromJson<NeuralWeights>(json);

            this.weightsInputHidden = data.weightsInputHidden;
            this.weightsHiddenOutput = data.weightsHiddenOutput;

            this.bestWeightsInputHidden = (float[])data.weightsInputHidden.Clone();
            this.bestWeightsHiddenOutput = (float[])data.weightsHiddenOutput.Clone();

            Debug.Log("✅ Еталонні ваги нейромережі успішно завантажено!");
        }
    }
}

/// <summary>
/// Допоміжний клас для серіалізації ваг нейромережі у JSON.
/// </summary>
[System.Serializable]
public class NeuralWeights
{
    public float[] weightsInputHidden;
    public float[] weightsHiddenOutput;
}
