using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class SimulationManager : MonoBehaviour
{
    [Header("Основні посилання")]
    public RocketPhysics rocketPhysics;

    [Header("Налаштування експерименту")]
    public int testsPerAlgorithm = 30;
    public float delay = 0.6f;

    [Header("Невизначеність (Monte-Carlo)")]
    public bool enableNoise = true;
    [Range(0f, 20f)] public float windStrength = 8f;
    [Range(0f, 10f)] public float massVariation = 5f;      // %
    [Range(0f, 8f)] public float angleVariation = 6f;      // градуси

    private List<LandingMetrics> pidResults = new List<LandingMetrics>();
    private List<LandingMetrics> fuzzyResults = new List<LandingMetrics>();
    private List<LandingMetrics> neuralResults = new List<LandingMetrics>();

    [Header("Запуск")]
    public bool runFullExperiment = false;

    void Awake()
    {
        if (rocketPhysics == null)
            rocketPhysics = FindObjectOfType<RocketPhysics>();
    }

    void Update()
    {
        if (runFullExperiment)
        {
            runFullExperiment = false;
            StartCoroutine(RunAdvancedExperiment());
        }
    }

    private IEnumerator RunAdvancedExperiment()
    {
        Debug.Log("🔬 ПОЧАТОК РОЗШИРЕНОГО ЕКСПЕРИМЕНТУ З НЕВИЗНАЧЕНІСТЮ");

        // Тест PID
        rocketPhysics.controlMode = RocketPhysics.ControlMode.PID;
        yield return StartCoroutine(RunTestsWithNoise("PID", pidResults));

        // Тест Fuzzy
        rocketPhysics.controlMode = RocketPhysics.ControlMode.Fuzzy;
        yield return StartCoroutine(RunTestsWithNoise("Fuzzy Logic", fuzzyResults));

        // Тест Neural
        rocketPhysics.controlMode = RocketPhysics.ControlMode.Neural;
        yield return StartCoroutine(RunTestsWithNoise("Neural Network", neuralResults));

        ShowAdvancedComparison();
        SaveAdvancedCSV();
    }

    private IEnumerator RunTestsWithNoise(string modeName, List<LandingMetrics> results)
    {
        results.Clear();
        for (int i = 0; i < testsPerAlgorithm; i++)
        {
            ApplyRandomNoise();
            rocketPhysics.ResetSimulation();

            yield return new WaitForSeconds(delay);

            while (!rocketPhysics.state.simulationFinished)
                yield return null;

            results.Add(rocketPhysics.metrics);
            Debug.Log($"[{modeName}] Тест {i + 1}/{testsPerAlgorithm} | Успіх: {rocketPhysics.metrics.isSuccessfulLanding}");
        }
    }

    private void ApplyRandomNoise()
    {
        if (!enableNoise || rocketPhysics == null) return;

        // Випадковий вітер (бічний вплив)
        rocketPhysics.state.velocity += new Vector3(
            Random.Range(-windStrength, windStrength),
            0,
            Random.Range(-windStrength * 0.6f, windStrength * 0.6f)
        );

        // Розкид маси палива
        float massNoise = Random.Range(-massVariation, massVariation);
        rocketPhysics.parameters.fuelMass = rocketPhysics.parameters.fuelMass * (1 + massNoise / 100f);

        // Розкид початкового кута
        Vector3 currentEuler = rocketPhysics.parameters.startEulerAngles;
        currentEuler.z += Random.Range(-angleVariation, angleVariation);
        rocketPhysics.parameters.startEulerAngles = currentEuler;
    }

    private void ShowAdvancedComparison()
    {
        Debug.Log("══════════════════════════════════════════════");
        Debug.Log("       РЕЗУЛЬТАТИ ЕКСПЕРИМЕНТУ З НЕВИЗНАЧЕНІСТЮ");
        Debug.Log("══════════════════════════════════════════════");
        PrintStats("PID", pidResults);
        PrintStats("Fuzzy Logic", fuzzyResults);
    }

    private void PrintStats(string name, List<LandingMetrics> list)
    {
        float success = list.Count > 0 ? (float)list.FindAll(m => m.isSuccessfulLanding).Count / list.Count * 100f : 0;
        float avgScore = list.Count > 0 ? GetAverage(list, m => m.SuccessScore) : 0;

        Debug.Log($"📈 {name} → Успішність: {success:F1}% | Середня оцінка: {avgScore:F1}/100");
    }

    private float GetAverage(List<LandingMetrics> list, System.Func<LandingMetrics, float> selector)
    {
        float sum = 0;
        foreach (var m in list) sum += selector(m);
        return sum / list.Count;
    }

    private void SaveAdvancedCSV()
    {
        string path = Path.Combine(Application.dataPath, "..", "SimulationLogs",
            $"Experiment_With_Noise_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");

        var lines = new List<string> { "Algorithm,Tests,SuccessRate(%),AvgScore" };
        lines.Add($"PID,{testsPerAlgorithm},{GetSuccessRate(pidResults):F2},{GetAverage(pidResults, m => m.SuccessScore):F2}");
        lines.Add($"Fuzzy,{testsPerAlgorithm},{GetSuccessRate(fuzzyResults):F2},{GetAverage(fuzzyResults, m => m.SuccessScore):F2}");

        File.WriteAllLines(path, lines);
        Debug.Log($"💾 Розширений експеримент збережений: {path}");
    }

    private float GetSuccessRate(List<LandingMetrics> list)
    {
        return list.Count > 0 ? (float)list.FindAll(m => m.isSuccessfulLanding).Count / list.Count * 100f : 0;
    }
}