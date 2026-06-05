using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class SimulationManager : MonoBehaviour
{
    [Header("Основні посилання")]
    [Tooltip("Якщо не призначено — шукає автоматично")]
    public RocketPhysics rocketPhysics;

    [Header("Налаштування експерименту")]
    public int testsPerAlgorithm = 25;
    public float delayBetweenTests = 0.7f;

    [Header("Невизначеність (Monte-Carlo)")]
    public bool enableNoise = true;
    [Range(0f, 25f)] public float windStrength = 10f;
    [Range(0f, 15f)] public float massVariationPercent = 6f;
    [Range(0f, 10f)] public float angleVariationDegrees = 7f;

    private List<LandingMetrics> pidResults = new List<LandingMetrics>();
    private List<LandingMetrics> fuzzyResults = new List<LandingMetrics>();
    private List<LandingMetrics> neuralResults = new List<LandingMetrics>();

    [Header("Запуск тестів")]
    [Tooltip("Постав галочку і натисни Play, щоб запустити повний експеримент")]
    public bool runFullExperiment = false;

    private float originalFuelMass;

    private void Awake()
    {
        if (rocketPhysics == null)
            rocketPhysics = FindObjectOfType<RocketPhysics>();

        if (rocketPhysics != null && rocketPhysics.parameters != null)
            originalFuelMass = rocketPhysics.parameters.fuelMass;
    }

    private void Update()
    {
        if (runFullExperiment)
        {
            runFullExperiment = false;
            StartCoroutine(RunFullComparisonExperiment());
        }
    }

    private IEnumerator RunFullComparisonExperiment()
    {
        Debug.Log("══════════════════════════════════════════════════════════════");
        Debug.Log("🚀 ПОЧАТОК ПОВНОГО ПОРІВНЯЛЬНОГО ЕКСПЕРИМЕНТУ (PID vs Fuzzy vs Neural)");
        Debug.Log("══════════════════════════════════════════════════════════════");

        // === PID ===
        rocketPhysics.controlMode = RocketPhysics.ControlMode.PID;
        yield return StartCoroutine(RunTestsForAlgorithm("PID", pidResults));

        // === Fuzzy Logic ===
        rocketPhysics.controlMode = RocketPhysics.ControlMode.Fuzzy;
        yield return StartCoroutine(RunTestsForAlgorithm("Fuzzy Logic", fuzzyResults));

        // === Neural Network ===
        rocketPhysics.controlMode = RocketPhysics.ControlMode.Neural;
        yield return StartCoroutine(RunTestsForAlgorithm("Neural Network", neuralResults));

        // Показати результати
        ShowFinalComparison();

        // Оновлюємо Dashboard
        if (FindObjectOfType<ExperimentDashboard>() != null)
        {
            float pid = GetSuccessRate(pidResults);
            float fuzzy = GetSuccessRate(fuzzyResults);
            float neural = GetSuccessRate(neuralResults);

            FindObjectOfType<ExperimentDashboard>().UpdateStatistics(pid, fuzzy, neural);
        }

        // Зберегти в CSV
        SaveComparisonToCSV();

        Debug.Log("🎉 ЕКСПЕРИМЕНТ ЗАВЕРШЕНО");
    }

    private float GetSuccessRate(List<LandingMetrics> list)
    {
        return list.Count > 0 ? (float)list.FindAll(m => m.isSuccessfulLanding).Count / list.Count * 100f : 0;
    }

    private IEnumerator RunTestsForAlgorithm(string algorithmName, List<LandingMetrics> resultsList)
    {
        resultsList.Clear();
        Debug.Log($"\n▶ Запуск {testsPerAlgorithm} симуляцій для {algorithmName}...");

        for (int i = 0; i < testsPerAlgorithm; i++)
        {
            // Відновлюємо оригінальну масу палива перед кожним тестом
            if (rocketPhysics != null && rocketPhysics.parameters != null)
                rocketPhysics.parameters.fuelMass = originalFuelMass;

            if (enableNoise)
                ApplyRandomNoise();

            rocketPhysics.ResetSimulation();

            yield return new WaitForSeconds(delayBetweenTests);

            while (!rocketPhysics.state.simulationFinished)
                yield return null;

            resultsList.Add(rocketPhysics.metrics);
            Debug.Log($"   [{algorithmName}] Тест {i + 1}/{testsPerAlgorithm} | Успіх: {rocketPhysics.metrics.isSuccessfulLanding}");
        }
    }

    private void ApplyRandomNoise()
    {
        if (rocketPhysics == null) return;

        // Випадковий бічний вітер
        rocketPhysics.state.velocity += new Vector3(
            Random.Range(-windStrength, windStrength),
            0,
            Random.Range(-windStrength * 0.5f, windStrength * 0.5f)
        );

        // Розкид маси палива
        float massNoise = Random.Range(-massVariationPercent, massVariationPercent) / 100f;
        rocketPhysics.parameters.fuelMass *= (1f + massNoise);

        // Розкид початкового кута нахилу
        Vector3 euler = rocketPhysics.parameters.startEulerAngles;
        euler.z += Random.Range(-angleVariationDegrees, angleVariationDegrees);
        rocketPhysics.parameters.startEulerAngles = euler;
    }

    private void ShowFinalComparison()
    {
        Debug.Log("\n══════════════════════════════════════════════════════════════");
        Debug.Log("                    ФІНАЛЬНЕ ПОРІВНЯННЯ АЛГОРИТМІВ");
        Debug.Log("══════════════════════════════════════════════════════════════");

        PrintStats("PID", pidResults);
        PrintStats("Fuzzy Logic", fuzzyResults);
        PrintStats("Neural Network", neuralResults);

        Debug.Log("══════════════════════════════════════════════════════════════\n");
    }

    private void PrintStats(string name, List<LandingMetrics> list)
    {
        if (list.Count == 0) return;

        float successRate = (float)list.FindAll(m => m.isSuccessfulLanding).Count / list.Count * 100f;
        float avgVelocity = GetAverage(list, m => m.touchdownVelocity);
        float avgAngle = GetAverage(list, m => m.landingAngleError);
        float avgFuel = GetAverage(list, m => m.fuelRemaining);
        float avgScore = GetAverage(list, m => m.SuccessScore);

        Debug.Log($"📊 {name.ToUpper()}");
        Debug.Log($"   Успішність:          {successRate:F1}%");
        Debug.Log($"   Сер. швидкість:      {avgVelocity:F2} м/с");
        Debug.Log($"   Сер. кут нахилу:     {avgAngle:F2}°");
        Debug.Log($"   Сер. залишок палива: {avgFuel:F1} кг");
        Debug.Log($"   Сер. оцінка:         {avgScore:F1}/100");
        Debug.Log("──────────────────────────────────────────────────────────────");
    }

    private float GetAverage(List<LandingMetrics> list, System.Func<LandingMetrics, float> selector)
    {
        if (list.Count == 0) return 0f;
        float sum = 0f;
        foreach (var item in list) sum += selector(item);
        return sum / list.Count;
    }

    private void SaveComparisonToCSV()
    {
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string path = Path.Combine(Application.dataPath, "..", "SimulationLogs",
            $"Final_Comparison_PID_vs_Fuzzy_vs_Neural_{timestamp}.csv");

        var lines = new List<string>
        {
            "Algorithm,Tests,SuccessRate(%),AvgTouchdownVelocity,AvgAngleError,AvgFuelRemaining,AvgSuccessScore"
        };

        lines.Add(CreateCSVLine("PID", pidResults));
        lines.Add(CreateCSVLine("Fuzzy Logic", fuzzyResults));
        lines.Add(CreateCSVLine("Neural Network", neuralResults));

        File.WriteAllLines(path, lines);
        Debug.Log($"💾 Порівняльна таблиця збережена: {path}");
    }

    private string CreateCSVLine(string name, List<LandingMetrics> list)
    {
        if (list.Count == 0)
            return $"{name},0,0,0,0,0,0";

        float success = (float)list.FindAll(m => m.isSuccessfulLanding).Count / list.Count * 100f;
        float vel = GetAverage(list, m => m.touchdownVelocity);
        float angle = GetAverage(list, m => m.landingAngleError);
        float fuel = GetAverage(list, m => m.fuelRemaining);
        float score = GetAverage(list, m => m.SuccessScore);

        return $"{name},{list.Count},{success:F2},{vel:F2},{angle:F2},{fuel:F2},{score:F2}";
    }
}