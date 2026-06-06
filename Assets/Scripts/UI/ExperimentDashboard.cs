using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Dashboard для керування експериментами та відображення статистики.
/// Дозволяє запускати окремі тести або повний порівняльний експеримент.
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
    public TMP_Text winnerText;

    private void Start()
    {
        if (btnRunPID) btnRunPID.onClick.AddListener(() => RunSingleTest(RocketPhysics.ControlMode.PID));
        if (btnRunFuzzy) btnRunFuzzy.onClick.AddListener(() => RunSingleTest(RocketPhysics.ControlMode.Fuzzy));
        if (btnRunNeural) btnRunNeural.onClick.AddListener(() => RunSingleTest(RocketPhysics.ControlMode.Neural));
        if (btnRunFullTest) btnRunFullTest.onClick.AddListener(RunFullExperiment);
        if (btnReset) btnReset.onClick.AddListener(ResetSimulation);

        if (testsCountInput)
            testsCountInput.text = simulationManager.testsPerAlgorithm.ToString();
    }

    private void RunSingleTest(RocketPhysics.ControlMode mode)
    {
        if (rocketPhysics == null) return;
        rocketPhysics.controlMode = mode;
        Debug.Log($"Запуск симуляції: {mode}");
        rocketPhysics.ResetSimulation();
    }

    private void RunFullExperiment()
    {
        if (simulationManager == null) return;
        if (testsCountInput && int.TryParse(testsCountInput.text, out int count))
            simulationManager.testsPerAlgorithm = count;

        simulationManager.enableNoise = noiseToggle != null && noiseToggle.isOn;
        simulationManager.windStrength = windSlider != null ? windSlider.value : 10f;
        simulationManager.runFullExperiment = true;
        Debug.Log("Запущено повний порівняльний експеримент (PID vs Fuzzy vs Neural)");
    }

    private void ResetSimulation()
    {
        if (rocketPhysics != null)
            rocketPhysics.ResetSimulation();
    }

    public void UpdateStatistics(float pidSuccess, float fuzzySuccess, float neuralSuccess)
    {
        if (pidStatsText) pidStatsText.text = $"PID: {pidSuccess:F1}%";
        if (fuzzyStatsText) fuzzyStatsText.text = $"Fuzzy: {fuzzySuccess:F1}%";
        if (neuralStatsText) neuralStatsText.text = $"Neural: {neuralSuccess:F1}%";

        string winner = "Neural Network";
        float max = Mathf.Max(pidSuccess, fuzzySuccess, neuralSuccess);

        if (max == fuzzySuccess) winner = "Fuzzy Logic";
        else if (max == pidSuccess) winner = "PID";

        if (winnerText)
            winnerText.text = $"Найкращий: {winner}";
    }
}
