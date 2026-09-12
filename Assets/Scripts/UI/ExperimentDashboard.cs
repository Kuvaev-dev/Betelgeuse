using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Legacy scene dashboard кнопок режимів / Monte-Carlo. Основний UI — <see cref="MissionControlUI"/>.
/// </summary>
public class ExperimentDashboard : MonoBehaviour
{
    [Header("Посилання")]
    public SimulationManager simulationManager;
    public RocketPhysics rocketPhysics;

    [Header("Кнопки")]
    public Button btnRunPID;
    public Button btnRunFuzzy;
    public Button btnRunNeural;
    public Button btnRunHybrid;
    public Button btnRunFullTest;
    public Button btnReset;

    [Header("Параметри")]
    public TMP_InputField testsCountInput;
    public Toggle noiseToggle;
    public Slider windSlider;
    public Toggle enableNeuralTrainingToggle;

    [Header("Статистика")]
    public TMP_Text pidStatsText;
    public TMP_Text fuzzyStatsText;
    public TMP_Text neuralStatsText;
    public TMP_Text hybridStatsText;
    public TMP_Text winnerText;

    [Header("Theme")]
    public bool ensureTheme = true;

    void Start()
    {
        // Повний HUD будує MissionControlUI — не створювати старий theme skin.
        ensureTheme = false;

        if (simulationManager == null)
            simulationManager = FindAnyObjectByType<SimulationManager>();
        if (rocketPhysics == null)
            rocketPhysics = FindAnyObjectByType<RocketPhysics>();

        // Якщо активний сучасний UI, ніколи не підключати legacy-кнопки сцени (давали «випадкові» зміни режиму)
        if (FindAnyObjectByType<MissionControlUI>() != null)
        {
            if (btnRunPID) btnRunPID.interactable = false;
            if (btnRunFuzzy) btnRunFuzzy.interactable = false;
            if (btnRunNeural) btnRunNeural.interactable = false;
            if (btnRunHybrid) btnRunHybrid.interactable = false;
            if (btnRunFullTest) btnRunFullTest.interactable = false;
            if (btnReset) btnReset.interactable = false;
            return;
        }

        if (btnRunPID) btnRunPID.onClick.AddListener(() => RunSingleTest(RocketPhysics.ControlMode.PID));
        if (btnRunFuzzy) btnRunFuzzy.onClick.AddListener(() => RunSingleTest(RocketPhysics.ControlMode.Fuzzy));
        if (btnRunNeural) btnRunNeural.onClick.AddListener(() => RunSingleTest(RocketPhysics.ControlMode.Neural));
        if (btnRunHybrid) btnRunHybrid.onClick.AddListener(() => RunSingleTest(RocketPhysics.ControlMode.Hybrid));
        if (btnRunFullTest) btnRunFullTest.onClick.AddListener(RunFullExperiment);
        if (btnReset) btnReset.onClick.AddListener(ResetSimulation);

        if (testsCountInput && simulationManager != null)
            testsCountInput.text = simulationManager.testsPerAlgorithm.ToString();

        if (enableNeuralTrainingToggle != null && rocketPhysics != null && rocketPhysics.neuralController != null)
        {
            enableNeuralTrainingToggle.isOn = rocketPhysics.neuralController.enableTraining;
            enableNeuralTrainingToggle.onValueChanged.AddListener(v =>
            {
                if (rocketPhysics.neuralController != null)
                    rocketPhysics.neuralController.enableTraining = v;
            });
        }

        // Підписи, якщо порожні
        SetIfEmpty(pidStatsText, "PID     —");
        SetIfEmpty(fuzzyStatsText, "FUZZY   —");
        SetIfEmpty(neuralStatsText, "NEURAL  —");
        SetIfEmpty(hybridStatsText, "HYBRID  —");
        SetIfEmpty(winnerText, "BEST    — awaiting experiment");
    }

    static void SetIfEmpty(TMP_Text t, string value)
    {
        if (t != null && string.IsNullOrWhiteSpace(t.text)) t.text = value;
    }

    void RunSingleTest(RocketPhysics.ControlMode mode)
    {
        if (rocketPhysics == null) return;
        rocketPhysics.controlMode = mode;
        Debug.Log($"▶ Single run: {mode}");
        rocketPhysics.ResetSimulation();
        FindAnyObjectByType<TrajectoryVisualizer>()?.Clear();
    }

    void RunFullExperiment()
    {
        if (simulationManager == null) return;
        // Умови з legacy dashboard controls (не форсувати DefenseBaseline)
        if (testsCountInput && int.TryParse(testsCountInput.text, out int n))
            simulationManager.testsPerAlgorithm = Mathf.Clamp(n, 5, 40);
        if (noiseToggle) simulationManager.enableNoise = noiseToggle.isOn;
        if (windSlider) simulationManager.windStrength = windSlider.value;
        simulationManager.RequestFullExperiment();
        Debug.Log("▶ Full Monte-Carlo (user settings, paired seeds): PID · Fuzzy · Neural · Hybrid");
    }

    void ResetSimulation()
    {
        rocketPhysics?.ResetSimulation();
        FindAnyObjectByType<TrajectoryVisualizer>()?.Clear();
    }

    /// <summary>Зворотна сумісність (3 алгоритми).</summary>
    public void UpdateStatistics(float pidSuccess, float fuzzySuccess, float neuralSuccess)
        => UpdateStatistics(pidSuccess, fuzzySuccess, neuralSuccess, -1f);

    public void UpdateStatistics(float pidSuccess, float fuzzySuccess, float neuralSuccess, float hybridSuccess)
        => UpdateStatistics(pidSuccess, fuzzySuccess, neuralSuccess, hybridSuccess, -1f, -1f, -1f, -1f);

    public void UpdateStatistics(float pidSuccess, float fuzzySuccess, float neuralSuccess, float hybridSuccess,
        float pidScore, float fuzzyScore, float neuralScore, float hybridScore)
    {
        if (pidStatsText)
        {
            pidStatsText.text = $"PID     {pidSuccess,5:F1}%";
            pidStatsText.color = MissionControlTheme.Text;
        }
        if (fuzzyStatsText)
        {
            fuzzyStatsText.text = $"FUZZY   {fuzzySuccess,5:F1}%";
            fuzzyStatsText.color = MissionControlTheme.Text;
        }
        if (neuralStatsText)
        {
            neuralStatsText.text = $"NEURAL  {neuralSuccess,5:F1}%";
            neuralStatsText.color = MissionControlTheme.Text;
        }
        if (hybridStatsText && hybridSuccess >= 0f)
        {
            hybridStatsText.text = $"HYBRID  {hybridSuccess,5:F1}%";
            hybridStatsText.color = MissionControlTheme.Text;
        }

        string winner = "—";
        float bestRate = -1f;
        float bestScore = -1f;
        void Consider(string name, float rate, float score)
        {
            if (rate < 0f) return;
            if (rate > bestRate + 1e-4f
                || (Mathf.Abs(rate - bestRate) <= 1e-4f && score > bestScore + 1e-4f))
            {
                bestRate = rate;
                bestScore = score;
                winner = name;
            }
        }
        Consider("PID", pidSuccess, pidScore);
        Consider("Fuzzy Sugeno", fuzzySuccess, fuzzyScore);
        Consider("Neural ES", neuralSuccess, neuralScore);
        Consider("Hybrid Neuro-Fuzzy", hybridSuccess, hybridScore);
        if (bestRate < 0f) bestRate = 0f;

        if (winnerText)
        {
            if (bestRate <= 0.05f)
            {
                winnerText.text = "BEST    — (all 0%)";
                winnerText.color = MissionControlTheme.Muted;
            }
            else
            {
                winnerText.text = $"BEST    {winner}  ({bestRate:F1}%)";
                winnerText.color = MissionControlTheme.Ok;
            }
        }

        if (MissionControlUI.Instance != null)
            MissionControlUI.Instance.UpdateStatistics(
                pidSuccess, fuzzySuccess, neuralSuccess, hybridSuccess,
                pidScore, fuzzyScore, neuralScore, hybridScore);
    }
}
