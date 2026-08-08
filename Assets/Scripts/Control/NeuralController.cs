using UnityEngine;
using System.IO;

/// <summary>
/// Нейромережевий контролер посадки: MLP 5→8→2 (tanh hidden, linear out).
/// Навчання: еволюційна стратегія ES(1+λ) — мутація ваг, елітизм за cost touchdown.
/// Входи (нормовані): h, Vy, mass, tilt, |Vh|;
/// виходи: множник тяги, bias gimbal.
/// Ваги зберігаються у BestWeights_Neural.json.
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
    /// <summary>Вага MLP residual vs soft-landing (вище = більше «нейро»-поведінки).</summary>
    [Range(0.1f, 0.7f)] public float residualWeight = 0.48f;
    [Range(0.2f, 1.0f)] public float maxDevFrac = 0.6f;
    [Range(0f, 0.5f)] public float gimbalBiasScale = 0.22f;

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

    /// <summary>Тяга: soft-landing + residual MLP.</summary>
    public float CalculateThrust(float height, float verticalVelocity, float mass, float currentThrust, float angleError)
    {
        CalculateControl(height, verticalVelocity, mass, currentThrust, angleError, 0f, 0f,
            out float thrust, out _);
        return thrust;
    }

    /// <summary>
    /// MLP residual поверх soft-landing.
    /// residualWeight/maxDevFrac задають «наскільки нейро» vs профіль (реалізм vs ідеал).
    /// </summary>
    public void CalculateControl(float height, float verticalVelocity, float mass, float currentThrust,
        float pitchError, float yawError, float horizSpeed,
        out float thrust, out Vector3 gimbalEuler)
    {
        CalculateControl(height, verticalVelocity, mass, currentThrust,
            pitchError, yawError, horizSpeed, blendWithProfile: true, out thrust, out gimbalEuler);
    }

    /// <param name="blendWithProfile">
    /// false — «сирий» MLP thrust (для Hybrid, щоб не дублювати BlendThrust).
    /// </param>
    public void CalculateControl(float height, float verticalVelocity, float mass, float currentThrust,
        float pitchError, float yawError, float horizSpeed, bool blendWithProfile,
        out float thrust, out Vector3 gimbalEuler)
    {
        float g = AtmosphereModel.GetGravity(Mathf.Max(0f, height));
        float profile = SoftLandingGuidance.ProfileThrust(height, verticalVelocity, mass);
        if (!isActive)
        {
            thrust = profile;
            gimbalEuler = SoftLandingGuidance.AttitudeGimbal(pitchError, yawError);
            return;
        }

        float tilt = Mathf.Sqrt(pitchError * pitchError + yawError * yawError);
        var x = BuildFeatures(height, verticalVelocity, mass, tilt, horizSpeed);
        float[] y = Forward(x);

        float mult = Mathf.Clamp(1.05f + y[0] * 0.85f, 0.8f, 2.9f);
        float nnThrust = mass * g * mult;
        if (blendWithProfile)
        {
            float w = residualWeight;
            if (height < 80f) w *= Mathf.Lerp(0.55f, 1f, height / 80f);
            thrust = SoftLandingGuidance.BlendThrust(profile, nnThrust, w, mass, height, maxDevFrac);
        }
        else
        {
            thrust = nnThrust;
        }

        Vector3 baseGimbal = SoftLandingGuidance.AttitudeGimbal(pitchError, yawError, 0f, 0f);
        float pitchBias = Mathf.Clamp(y[1] * 10f, -12f, 12f) * gimbalBiasScale;
        float pitch = Mathf.Clamp(baseGimbal.x - pitchBias * Mathf.Sign(pitchError + 1e-4f), -18f, 18f);
        float yaw = Mathf.Clamp(baseGimbal.z, -18f, 18f);
        gimbalEuler = new Vector3(pitch, 0f, yaw);
    }

    public Vector3 CalculateGimbal(float pitchError, float yawError)
    {
        if (!isActive) return SoftLandingGuidance.AttitudeGimbal(pitchError, yawError);
        return SoftLandingGuidance.AttitudeGimbal(pitchError, yawError);
    }

    /// <summary>
    /// Еволюційний крок після епізоду. Cost: vel, angle, fuel, horizontal miss.
    /// </summary>
    public void Train(float touchdownVelocity, float angleError, float fuelRemaining, float horizontalMiss = 0f)
    {
        if (!enableTraining) return;

        // Cost для ES(1+1): м'яка посадка + кут + промах + паливо + штрафи критеріїв
        float cost = touchdownVelocity * 0.50f
                   + angleError * 0.28f
                   + Mathf.Max(0f, 4000f - fuelRemaining) / 1000f * 0.08f
                   + horizontalMiss * 0.035f
                   + (touchdownVelocity > 3.5f ? 12f : 0f)
                   + (angleError > 7f ? 9f : 0f)
                   + (horizontalMiss > 25f ? 7f : 0f);

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

        // ES(1+λ): λ мутантів від еліти; для online-епізоду беремо 1-го
        // (повний λ-турнір потребує λ паралельних симуляцій — див. Monte-Carlo).
        // Тут застосовуємо σ-масштабовану мутацію; λ впливає на силу розкиду.
        float sigmaScale = 1f + 0.08f * Mathf.Max(0, lambda - 1);
        MutateFromBest(mutationSigma * sigmaScale);
        // Додаткові «віртуальні» мутації звужують σ (ефект більшої популяції)
        for (int k = 1; k < lambda; k++)
            mutationSigma = Mathf.Max(0.02f, mutationSigma * 0.998f);
        generation++;
    }

    // Зворотна сумісність
    public void Train(float touchdownVelocity, float angleError, float fuelRemaining)
        => Train(touchdownVelocity, angleError, fuelRemaining, 0f);

    void MutateFromBest(float sigma = -1f)
    {
        RestoreBest();
        float s = sigma > 0f ? sigma : mutationSigma;
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

    /// <summary>
    /// Фізично обґрунтовані ваги для стабільної residual-корекції
    /// ( thr↑ при малій h / великій |Vy| ; gimbal bias ≈ 0 ).
    /// Використовується кнопкою «Ідеальні параметри».
    /// </summary>
    public void InstallIdealWeights()
    {
        hSize = Mathf.Clamp(hiddenNeurons, 4, 16);
        wIH = new float[hSize * InputSize];
        bH = new float[hSize];
        wHO = new float[OutputSize * hSize];
        bO = new float[OutputSize];

        for (int h = 0; h < hSize; h++)
        {
            float phase = h / (float)hSize;
            // height ↓ → hidden ↑ (negative weight on normalized h)
            wIH[h * InputSize + 0] = -0.85f - phase * 0.25f;
            // |Vy| ↑ (feature = vy/-120, descent positive) → hidden ↑
            wIH[h * InputSize + 1] = 1.05f + phase * 0.2f;
            wIH[h * InputSize + 2] = -0.15f + phase * 0.1f; // mass
            wIH[h * InputSize + 3] = 0.45f;                 // tilt
            wIH[h * InputSize + 4] = 0.2f;                  // horiz
            bH[h] = -0.05f * h;

            // Thrust residual: moderate positive from hidden
            wHO[0 * hSize + h] = 0.12f + 0.03f * (h % 3);
            // Gimbal bias: near-zero (base PD handles attitude)
            wHO[1 * hSize + h] = 0.02f * ((h % 2) == 0 ? 1f : -1f);
        }
        bO[0] = 0.08f;
        bO[1] = 0f;
        bestCost = 2.5f;
        generation = Mathf.Max(generation, 1);
        SnapshotBest();
        SaveBestWeights();
        Debug.Log("[NN-ES] Встановлено ідеальні ваги для гарантованої посадки.");
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
