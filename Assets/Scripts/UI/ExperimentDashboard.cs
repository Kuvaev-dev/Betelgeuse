using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mission-Control панель експериментів: PID / Fuzzy / Neural / Hybrid + Monte-Carlo.
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
        // Full HUD is built by MissionControlUI — do not spawn old theme skin.
        ensureTheme = false;

        if (simulationManager == null)
            simulationManager = FindFirstObjectByType<SimulationManager>();
        if (rocketPhysics == null)
            rocketPhysics = FindFirstObjectByType<RocketPhysics>();

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

        // Labels if empty
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
        FindFirstObjectByType<TrajectoryVisualizer>()?.Clear();
    }

    void RunFullExperiment()
    {
        if (simulationManager == null) return;
        if (testsCountInput && int.TryParse(testsCountInput.text, out int count))
            simulationManager.testsPerAlgorithm = Mathf.Clamp(count, 1, 200);

        simulationManager.enableNoise = noiseToggle == null || noiseToggle.isOn;
        simulationManager.windStrength = windSlider != null ? windSlider.value : 10f;
        simulationManager.runFullExperiment = true;
        Debug.Log("▶ Full Monte-Carlo: PID · Fuzzy · Neural · Hybrid");
    }

    void ResetSimulation()
    {
        rocketPhysics?.ResetSimulation();
        FindFirstObjectByType<TrajectoryVisualizer>()?.Clear();
    }

    /// <summary>Зворотна сумісність (3 алгоритми).</summary>
    public void UpdateStatistics(float pidSuccess, float fuzzySuccess, float neuralSuccess)
        => UpdateStatistics(pidSuccess, fuzzySuccess, neuralSuccess, -1f);

    public void UpdateStatistics(float pidSuccess, float fuzzySuccess, float neuralSuccess, float hybridSuccess)
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

        string winner = "PID";
        float max = pidSuccess;
        if (fuzzySuccess >= max) { max = fuzzySuccess; winner = "Fuzzy Sugeno"; }
        if (neuralSuccess >= max) { max = neuralSuccess; winner = "Neural ES"; }
        if (hybridSuccess > max) { max = hybridSuccess; winner = "Hybrid Neuro-Fuzzy"; }

        if (winnerText)
        {
            winnerText.text = $"BEST    {winner}  ({max:F1}%)";
            winnerText.color = MissionControlTheme.Ok;
        }

        if (MissionControlUI.Instance != null)
            MissionControlUI.Instance.UpdateStatistics(pidSuccess, fuzzySuccess, neuralSuccess, hybridSuccess);
    }
}
