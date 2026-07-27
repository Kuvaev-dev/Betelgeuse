using UnityEngine;
using System.IO;

/// <summary>
/// MLP-контролер посадки: 5 → H → 2 (tanh hidden, linear out).
/// Навчання: еволюційна стратегія (1+λ) — мутація ваг, елітизм за cost посадки.
/// Входи: h, vy, mass, tilt, |v_horizontal|; виходи: thrust mult, pitch gimbal bias.
/// </summary>
public class NeuralController : MonoBehaviour
{
    public const int InputSize = 5;
    public const int HiddenSize = 8;
    public const int OutputSize = 2;

    [Header("Neural Network (ES 1+λ)")]
    public bool isActive = true;
    public bool enableTraining = true;
    [Range(4, 16)] public int hiddenNeurons = HiddenSize;
    [Range(1, 12)] public int lambda = 4;
    public float mutationSigma = 0.08f;
    public float sigmaDecay = 0.995f;

    [Header("Стан навчання")]
    public int generation;
    public float bestCost = float.MaxValue;

    float[] wIH; // hidden * input
    float[] bH;
    float[] wHO; // output * hidden
    float[] bO;

    float[] bestWIH, bestBH, bestWHO, bestBO;
    string weightsFilePath;
    int hSize;

    void Awake()
    {
        hSize = Mathf.Clamp(hiddenNeurons, 4, 16);
        weightsFilePath = Path.Combine(Application.dataPath, "..", "BestWeights_Neural.json");
        InitRandomWeights(0.35f);
        SnapshotBest();
    }

    void InitRandomWeights(float scale)
    {
        wIH = new float[hSize * InputSize];
        bH = new float[hSize];
        wHO = new float[OutputSize * hSize];
        bO = new float[OutputSize];

        // Фізично обґрунтована ініціалізація: висота↓ → тяга↑, |vy|↑ → тяга↑
        for (int i = 0; i < wIH.Length; i++)
            wIH[i] = (Random.value - 0.5f) * 2f * scale;
        for (int i = 0; i < bH.Length; i++)
            bH[i] = (Random.value - 0.5f) * scale * 0.5f;
        for (int i = 0; i < wHO.Length; i++)
            wHO[i] = (Random.value - 0.5f) * 2f * scale;
        bO[0] = 0.15f; // bias thrust mult ~ hover+
        bO[1] = 0f;

        // Підсилення корисних входів
        for (int h = 0; h < hSize; h++)
        {
            wIH[h * InputSize + 0] += -0.6f; // height
            wIH[h * InputSize + 1] += 0.9f;  // descent speed
            wIH[h * InputSize + 3] += 0.4f;  // tilt
        }
    }

    void SnapshotBest()
    {
        bestWIH = (float[])wIH.Clone();
        bestBH = (float[])bH.Clone();
        bestWHO = (float[])wHO.Clone();
        bestBO = (float[])bO.Clone();
    }

    void RestoreBest()
    {
        wIH = (float[])bestWIH.Clone();
        bH = (float[])bestBH.Clone();
        wHO = (float[])bestWHO.Clone();
        bO = (float[])bestBO.Clone();
    }

    float[] Forward(float[] x)
    {
        var hidden = new float[hSize];
        for (int h = 0; h < hSize; h++)
        {
            float s = bH[h];
            int baseIdx = h * InputSize;
            for (int i = 0; i < InputSize; i++)
                s += wIH[baseIdx + i] * x[i];
            hidden[h] = System.MathF.Tanh(s);
        }

        var y = new float[OutputSize];
        for (int o = 0; o < OutputSize; o++)
        {
            float s = bO[o];
            int baseIdx = o * hSize;
            for (int h = 0; h < hSize; h++)
                s += wHO[baseIdx + h] * hidden[h];
            y[o] = s;
        }
        return y;
    }

    float[] BuildFeatures(float height, float verticalVelocity, float mass, float angleErrorDeg, float horizSpeed)
    {
        float g = AtmosphereModel.GetGravity(Mathf.Max(0f, height));
        float hover = Mathf.Max(1f, mass * g);
        return new[]
        {
            Mathf.Clamp01(height / 2500f),
            Mathf.Clamp(verticalVelocity / -120f, -0.5f, 2f),
            Mathf.Clamp01((mass - 25000f) / 20000f),
            Mathf.Clamp(angleErrorDeg / 30f, -2f, 2f),
            Mathf.Clamp01(horizSpeed / 40f)
        };
    }

    /// <summary>Тяга за MLP (вихід 0 → множник mg).</summary>
    public float CalculateThrust(float height, float verticalVelocity, float mass, float currentThrust, float angleError)
    {
        float g = AtmosphereModel.GetGravity(Mathf.Max(0f, height));
        if (!isActive) return mass * g * 1.1f;

        float horiz = 0f; // fallback; RocketPhysics передає через CalculateControl
        var x = BuildFeatures(height, verticalVelocity, mass, angleError, horiz);
        // легкий feedback поточної тяги через bias-корекцію
        float tNorm = currentThrust / Mathf.Max(1f, mass * g);
        x[2] = Mathf.Clamp01(x[2] * 0.7f + Mathf.Clamp01(tNorm / 3f) * 0.3f);

        float[] y = Forward(x);
        float mult = Mathf.Clamp(1.15f + y[0] * 0.85f, 0.8f, 2.9f);
        return mass * g * mult;
    }

    /// <summary>Повний вихід MLP: thrust + gimbal pitch/yaw bias.</summary>
    public void CalculateControl(float height, float verticalVelocity, float mass, float currentThrust,
        float pitchError, float yawError, float horizSpeed,
        out float thrust, out Vector3 gimbalEuler)
    {
        float g = AtmosphereModel.GetGravity(Mathf.Max(0f, height));
        if (!isActive)
        {
            thrust = mass * g * 1.1f;
            gimbalEuler = Vector3.zero;
            return;
        }

        float tilt = Mathf.Sqrt(pitchError * pitchError + yawError * yawError);
        var x = BuildFeatures(height, verticalVelocity, mass, tilt, horizSpeed);
        float[] y = Forward(x);

        float mult = Mathf.Clamp(1.15f + y[0] * 0.85f, 0.8f, 2.9f);
        thrust = mass * g * mult;

        float pitchBias = Mathf.Clamp(y[1] * 12f, -20f, 20f);
        // пропорційна стабілізація + нейро-корекція
        float pitch = Mathf.Clamp(pitchError * 0.85f + pitchBias * Mathf.Sign(pitchError + 1e-4f) * 0.25f, -28f, 28f);
        float yaw = Mathf.Clamp(yawError * 0.85f, -28f, 28f);
        gimbalEuler = new Vector3(pitch, 0f, yaw);
    }

    public Vector3 CalculateGimbal(float pitchError, float yawError)
    {
        if (!isActive) return Vector3.zero;
        return new Vector3(
            Mathf.Clamp(pitchError * 0.9f, -28f, 28f),
            0f,
            Mathf.Clamp(yawError * 0.9f, -28f, 28f));
    }

    /// <summary>
    /// Еволюційний крок після епізоду. Cost: vel, angle, fuel, horizontal miss.
    /// </summary>
    public void Train(float touchdownVelocity, float angleError, float fuelRemaining, float horizontalMiss = 0f)
    {
        if (!enableTraining) return;

        float cost = touchdownVelocity * 0.45f
                   + angleError * 0.25f
                   + Mathf.Max(0f, 4000f - fuelRemaining) / 1000f * 0.1f
                   + horizontalMiss * 0.02f
                   + (touchdownVelocity > 3.5f ? 8f : 0f)
                   + (angleError > 7f ? 6f : 0f);

        if (cost < bestCost)
        {
            bestCost = cost;
            SnapshotBest();
            SaveBestWeights();
            mutationSigma = Mathf.Max(0.02f, mutationSigma * 0.97f);
            Debug.Log($"[NN-ES] gen={generation} NEW best cost={bestCost:F3} σ={mutationSigma:F3}");
        }
        else
        {
            RestoreBest();
            mutationSigma = Mathf.Min(0.25f, mutationSigma / sigmaDecay); // mild reheating on stall
        }

        // (1+λ): мутуємо від еліти
        MutateFromBest();
        generation++;
    }

    // Зворотна сумісність
    public void Train(float touchdownVelocity, float angleError, float fuelRemaining)
        => Train(touchdownVelocity, angleError, fuelRemaining, 0f);

    void MutateFromBest()
    {
        RestoreBest();
        float s = mutationSigma;
        for (int i = 0; i < wIH.Length; i++)
            wIH[i] = bestWIH[i] + Gaussian() * s;
        for (int i = 0; i < bH.Length; i++)
            bH[i] = bestBH[i] + Gaussian() * s * 0.5f;
        for (int i = 0; i < wHO.Length; i++)
            wHO[i] = bestWHO[i] + Gaussian() * s;
        for (int i = 0; i < bO.Length; i++)
            bO[i] = bestBO[i] + Gaussian() * s * 0.5f;
    }

    static float Gaussian()
    {
        // Box-Muller
        float u1 = Mathf.Max(1e-6f, Random.value);
        float u2 = Random.value;
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
    }

    public void SaveBestWeights()
    {
        var data = new NeuralWeights
        {
            inputSize = InputSize,
            hiddenSize = hSize,
            outputSize = OutputSize,
            generation = generation,
            bestCost = bestCost,
            wIH = bestWIH,
            bH = bestBH,
            wHO = bestWHO,
            bO = bestBO,
            // legacy fields for old files
            weightsInputHidden = bestWIH != null && bestWIH.Length >= 4
                ? new[] { bestWIH[0], bestWIH[1], bestWIH[2], bestWIH[3] }
                : new[] { 0.8f, -1.2f, 0.6f, 1.1f },
            weightsHiddenOutput = bestBO != null && bestWHO != null && bestWHO.Length > 0
                ? new[] { bestWHO[0], bestBO[0] }
                : new[] { 1.3f, -0.9f }
        };
        File.WriteAllText(weightsFilePath, JsonUtility.ToJson(data, true));
    }

    public void LoadBestWeights()
    {
        if (!File.Exists(weightsFilePath)) return;
        try
        {
            var data = JsonUtility.FromJson<NeuralWeights>(File.ReadAllText(weightsFilePath));
            if (data == null) return;

            if (data.wIH != null && data.wIH.Length == hSize * InputSize
                && data.wHO != null && data.bH != null && data.bO != null)
            {
                wIH = data.wIH;
                bH = data.bH;
                wHO = data.wHO;
                bO = data.bO;
                bestCost = data.bestCost > 0f ? data.bestCost : bestCost;
                generation = data.generation;
                SnapshotBest();
                Debug.Log($"[NN-ES] Завантажено ваги MLP {InputSize}×{hSize}×{OutputSize}, gen={generation}, cost={bestCost:F3}");
                return;
            }

            // Legacy 4→1→1
            if (data.weightsInputHidden != null && data.weightsInputHidden.Length >= 4)
            {
                InitRandomWeights(0.2f);
                for (int h = 0; h < hSize; h++)
                {
                    for (int i = 0; i < 4 && i < InputSize; i++)
                        wIH[h * InputSize + i] = data.weightsInputHidden[i] * (0.6f + 0.1f * h);
                }
                if (data.weightsHiddenOutput != null && data.weightsHiddenOutput.Length >= 2)
                {
                    for (int h = 0; h < hSize; h++)
                        wHO[h] = data.weightsHiddenOutput[0] / hSize;
                    bO[0] = data.weightsHiddenOutput[1];
                }
                SnapshotBest();
                Debug.Log("[NN-ES] Міграція legacy-ваг у MLP завершена.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NN-ES] Не вдалося завантажити ваги: {e.Message}");
        }
    }
}

[System.Serializable]
public class NeuralWeights
{
    public int inputSize;
    public int hiddenSize;
    public int outputSize;
    public int generation;
    public float bestCost;
    public float[] wIH;
    public float[] bH;
    public float[] wHO;
    public float[] bO;
    // legacy
    public float[] weightsInputHidden;
    public float[] weightsHiddenOutput;
}
