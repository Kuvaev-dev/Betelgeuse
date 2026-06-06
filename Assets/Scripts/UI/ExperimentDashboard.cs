using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Dashboard для керування експериментами та відображення статистики.
/// Дозволяє запускати окремі тести або повний порівняльний експеримент.
/// </summary>
public class ExperimentDashboard : MonoBehaviour
{
    [Header("Посилання на компоненти")]
    public SimulationManager simulationManager;
    public RocketPhysics rocketPhysics;

    [Header("Кнопки керування")]
    public Button btnRunPID;
    public Button btnRunFuzzy;
    public Button btnRunNeural;
    public Button btnRunFullTest;
    public Button btnReset;

    [Header("Параметри експерименту")]
    public TMP_InputField testsCountInput;
    public Toggle noiseToggle;
    public Slider windSlider;
    public Toggle enableNeuralTrainingToggle;

    [Header("Відображення статистики")]
    public TMP_Text pidStatsText;
    public TMP_Text fuzzyStatsText;
    public TMP_Text neuralStatsText;
    public TMP_Text winnerText;

    private void Start()
    {
        // Прив'язка кнопок до методів
        if (btnRunPID) btnRunPID.onClick.AddListener(() => RunSingleTest(RocketPhysics.ControlMode.PID));
        if (btnRunFuzzy) btnRunFuzzy.onClick.AddListener(() => RunSingleTest(RocketPhysics.ControlMode.Fuzzy));
        if (btnRunNeural) btnRunNeural.onClick.AddListener(() => RunSingleTest(RocketPhysics.ControlMode.Neural));
        if (btnRunFullTest) btnRunFullTest.onClick.AddListener(RunFullExperiment);
        if (btnReset) btnReset.onClick.AddListener(ResetSimulation);

        // Ініціалізація поля введення
        if (testsCountInput)
            testsCountInput.text = simulationManager.testsPerAlgorithm.ToString();
    }

    /// <summary>
    /// Запускає одну симуляцію в обраному режимі керування.
    /// </summary>
    private void RunSingleTest(RocketPhysics.ControlMode mode)
    {
        if (rocketPhysics == null)
            return;

        rocketPhysics.controlMode = mode;
        Debug.Log($"▶ Запуск симуляції: {mode}");
        rocketPhysics.ResetSimulation();
    }

    /// <summary>
    /// Запускає повний порівняльний експеримент з поточними налаштуваннями.
    /// </summary>
    private void RunFullExperiment()
    {
        if (simulationManager == null)
            return;

        // Оновлення параметрів з UI
        if (testsCountInput && int.TryParse(testsCountInput.text, out int count))
            simulationManager.testsPerAlgorithm = count;

        if (noiseToggle != null)
            simulationManager.enableNoise = noiseToggle.isOn;

        if (windSlider != null)
            simulationManager.windStrength = windSlider.value;

        // Запуск експерименту
        simulationManager.runFullExperiment = true;
        Debug.Log("Запущено повний порівняльний експеримент (PID vs Fuzzy vs Neural)");
    }

    /// <summary>
    /// Скидає поточну симуляцію.
    /// </summary>
    private void ResetSimulation()
    {
        if (rocketPhysics != null)
            rocketPhysics.ResetSimulation();
    }

    /// <summary>
    /// Оновлює текстові поля статистики на Dashboard (викликається з SimulationManager).
    /// </summary>
    public void UpdateStatistics(float pidSuccess, float fuzzySuccess, float neuralSuccess)
    {
        if (pidStatsText) pidStatsText.text = $"PID: {pidSuccess:F1}%";
        if (fuzzyStatsText) fuzzyStatsText.text = $"Fuzzy: {fuzzySuccess:F1}%";
        if (neuralStatsText) neuralStatsText.text = $"Neural: {neuralSuccess:F1}%";

        // Визначення переможця
        string winner = "Neural Network";
        float max = Mathf.Max(pidSuccess, fuzzySuccess, neuralSuccess);

        if (max == fuzzySuccess) winner = "Fuzzy Logic";
        else if (max == pidSuccess) winner = "PID";

        if (winnerText)
            winnerText.text = $"Найкращий: {winner}";
    }
}
