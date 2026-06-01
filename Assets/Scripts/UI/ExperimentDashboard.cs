using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExperimentDashboard : MonoBehaviour
{
    public SimulationManager simulationManager;
    public RocketPhysics rocketPhysics;

    [Header("Кнопки")]
    public Button btnRunPID;
    public Button btnRunFuzzy;
    public Button btnRunFullTest;
    public Button btnReset;

    [Header("Параметри")]
    public TMP_InputField testsCountInput;
    public Toggle noiseToggle;
    public Slider windSlider;

    [Header("Статистика")]
    public TMP_Text pidSuccessText;
    public TMP_Text fuzzySuccessText;
    public TMP_Text comparisonText;

    private void Start()
    {
        // Прив'язка кнопок
        if (btnRunPID != null) btnRunPID.onClick.AddListener(RunPIDTest);
        if (btnRunFuzzy != null) btnRunFuzzy.onClick.AddListener(RunFuzzyTest);
        if (btnRunFullTest != null) btnRunFullTest.onClick.AddListener(RunFullExperiment);
        if (btnReset != null) btnReset.onClick.AddListener(ResetAll);

        if (testsCountInput != null)
            testsCountInput.text = simulationManager.testsPerAlgorithm.ToString();
    }

    private void RunPIDTest()
    {
        if (rocketPhysics == null) return;
        rocketPhysics.controlMode = RocketPhysics.ControlMode.PID;
        Debug.Log("▶ Запуск одиночної симуляції PID");
        rocketPhysics.ResetSimulation();
    }

    private void RunFuzzyTest()
    {
        if (rocketPhysics == null) return;
        rocketPhysics.controlMode = RocketPhysics.ControlMode.Fuzzy;
        Debug.Log("▶ Запуск одиночної симуляції Fuzzy Logic");
        rocketPhysics.ResetSimulation();
    }

    private void RunFullExperiment()
    {
        if (simulationManager == null) return;

        if (testsCountInput != null && int.TryParse(testsCountInput.text, out int count))
            simulationManager.testsPerAlgorithm = count;

        simulationManager.enableNoise = noiseToggle.isOn;
        simulationManager.windStrength = windSlider.value;

        simulationManager.runFullExperiment = true;
        Debug.Log("🚀 Запущено повний порівняльний експеримент");
    }

    private void ResetAll()
    {
        if (rocketPhysics != null)
            rocketPhysics.ResetSimulation();
    }

    public void UpdateComparisonUI(float pidSuccess, float fuzzySuccess)
    {
        if (pidSuccessText != null)
            pidSuccessText.text = $"PID: {pidSuccess:F1}% успішності";

        if (fuzzySuccessText != null)
            fuzzySuccessText.text = $"Fuzzy: {fuzzySuccess:F1}% успішності";

        if (comparisonText != null)
            comparisonText.text = fuzzySuccess > pidSuccess ?
                "Fuzzy Logic кращий у цій серії" :
                "PID кращий у цій серії";
    }
}