using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Порівняльні Monte-Carlo експерименти: PID / Fuzzy / Neural / Hybrid.
/// </summary>
public class SimulationManager : MonoBehaviour
{
    [Header("Основні посилання")]
    public RocketPhysics rocketPhysics;
    public ExperimentDashboard dashboard;

    [Header("Налаштування експерименту")]
    public int testsPerAlgorithm = 25;
    public float delayBetweenTests = 0.05f;
    public bool includeHybrid = true;
    [Range(1f, 50f)] public float experimentTimeScale = 20f;

    [Header("Невизначеність (Monte-Carlo)")]
    public bool enableNoise = true;
    [Range(0f, 25f)] public float windStrength = 10f;
    [Range(0f, 15f)] public float massVariationPercent = 6f;
    [Range(0f, 10f)] public float angleVariationDegrees = 7f;
    public bool continuousWind = true;

    [Header("Запуск")]
    public bool runFullExperiment;

    readonly List<LandingMetrics> pidResults = new();
    readonly List<LandingMetrics> fuzzyResults = new();
    readonly List<LandingMetrics> neuralResults = new();
    readonly List<LandingMetrics> hybridResults = new();

    float originalFuelMass;
    bool experimentRunning;
    TrajectoryVisualizer visualizer;

    void Awake()
    {
        if (rocketPhysics == null)
            rocketPhysics = FindFirstObjectByType<RocketPhysics>();
        if (dashboard == null)
            dashboard = FindFirstObjectByType<ExperimentDashboard>();
        visualizer = FindFirstObjectByType<TrajectoryVisualizer>();

        if (rocketPhysics != null && rocketPhysics.parameters != null)
            originalFuelMass = rocketPhysics.parameters.fuelMass;
    }

    void Update()
    {
        if (!runFullExperiment || experimentRunning) return;
        runFullExperiment = false;
        StartCoroutine(RunFullComparisonExperiment());
    }

    IEnumerator RunFullComparisonExperiment()
    {
        experimentRunning = true;
        float prevScale = Time.timeScale;
        float prevFixed = Time.fixedDeltaTime;

        // Прискорення batch: timeScale↑, fixedDeltaTime = крок інтегратора
        float step = rocketPhysics.parameters != null ? rocketPhysics.parameters.fixedTimeStep : 0.005f;
        Time.timeScale = Mathf.Clamp(experimentTimeScale, 1f, 50f);
        Time.fixedDeltaTime = step;

        Debug.Log("══ Повний порівняльний експеримент (PID · Fuzzy · Neural · Hybrid) ══");

        rocketPhysics.controlMode = RocketPhysics.ControlMode.PID;
        yield return RunTestsForAlgorithm("PID", pidResults);

        rocketPhysics.controlMode = RocketPhysics.ControlMode.Fuzzy;
        yield return RunTestsForAlgorithm("Fuzzy Sugeno", fuzzyResults);

        rocketPhysics.controlMode = RocketPhysics.ControlMode.Neural;
        yield return RunTestsForAlgorithm("Neural ES", neuralResults);

        if (includeHybrid)
        {
            rocketPhysics.controlMode = RocketPhysics.ControlMode.Hybrid;
            yield return RunTestsForAlgorithm("Hybrid Neuro-Fuzzy", hybridResults);
        }

        ShowFinalComparison();

        float pid = GetSuccessRate(pidResults);
        float fuzzy = GetSuccessRate(fuzzyResults);
        float neural = GetSuccessRate(neuralResults);
        float hybrid = GetSuccessRate(hybridResults);

        if (dashboard != null)
            dashboard.UpdateStatistics(pid, fuzzy, neural, hybrid);
        else
            FindFirstObjectByType<ExperimentDashboard>()
                ?.UpdateStatistics(pid, fuzzy, neural, hybrid);

        if (MissionControlUI.Instance != null)
            MissionControlUI.Instance.UpdateStatistics(pid, fuzzy, neural, hybrid);

        SaveComparisonToCSV();

        Time.timeScale = prevScale;
        Time.fixedDeltaTime = prevFixed;
        experimentRunning = false;
        Debug.Log("══ Експеримент завершено ══");
    }

    float GetSuccessRate(List<LandingMetrics> list)
        => list.Count > 0
            ? (float)list.FindAll(m => m.isSuccessfulLanding).Count / list.Count * 100f
            : 0f;

    IEnumerator RunTestsForAlgorithm(string algorithmName, List<LandingMetrics> resultsList)
    {
        resultsList.Clear();
        Debug.Log($"▶ {algorithmName}: {testsPerAlgorithm} симуляцій...");

        float maxT = rocketPhysics.parameters != null
            ? rocketPhysics.parameters.maxSimulationTime + 5f
            : 420f;

        for (int i = 0; i < testsPerAlgorithm; i++)
        {
            if (rocketPhysics.parameters != null)
                rocketPhysics.parameters.fuelMass = originalFuelMass;

            rocketPhysics.ResetSimulation();
            visualizer?.Clear();

            if (enableNoise)
                ApplyRandomNoiseToState();

            if (delayBetweenTests > 0f)
                yield return new WaitForSeconds(delayBetweenTests);

            float waited = 0f;
            while (!rocketPhysics.state.simulationFinished && waited < maxT)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (!rocketPhysics.state.simulationFinished)
            {
                // safety stop
                rocketPhysics.state.simulationFinished = true;
                rocketPhysics.state.isLanded = true;
            }

            // deep copy metrics
            resultsList.Add(CloneMetrics(rocketPhysics.metrics));
        }
    }

    static LandingMetrics CloneMetrics(LandingMetrics m)
    {
        return new LandingMetrics
        {
            touchdownVelocity = m.touchdownVelocity,
            landingAngleError = m.landingAngleError,
            fuelRemaining = m.fuelRemaining,
            maxAltitude = m.maxAltitude,
            totalFlightTime = m.totalFlightTime,
            horizontalMiss = m.horizontalMiss,
            horizontalSpeed = m.horizontalSpeed,
            timedOut = m.timedOut,
            isSuccessfulLanding = m.isSuccessfulLanding
        };
    }

    void ApplyRandomNoiseToState()
    {
        if (rocketPhysics?.state == null) return;

        Vector3 windKick = new Vector3(
            Random.Range(-windStrength, windStrength),
            0f,
            Random.Range(-windStrength * 0.5f, windStrength * 0.5f));

        rocketPhysics.state.velocity += windKick;

        if (continuousWind)
            rocketPhysics.windVelocity = windKick * 0.35f;
        else
            rocketPhysics.windVelocity = Vector3.zero;

        float massNoise = 1f + Random.Range(-massVariationPercent, massVariationPercent) / 100f;
        rocketPhysics.state.currentFuelMass = Mathf.Max(0f, rocketPhysics.state.currentFuelMass * massNoise);

        float ax = Random.Range(-angleVariationDegrees, angleVariationDegrees);
        float az = Random.Range(-angleVariationDegrees, angleVariationDegrees);
        rocketPhysics.state.rotation *= Quaternion.Euler(ax, 0f, az);
        rocketPhysics.SyncTransformWithState();
    }

    void ShowFinalComparison()
    {
        Debug.Log("── Фінальне порівняння ──");
        PrintStats("PID", pidResults);
        PrintStats("Fuzzy Sugeno", fuzzyResults);
        PrintStats("Neural ES", neuralResults);
        if (includeHybrid) PrintStats("Hybrid Neuro-Fuzzy", hybridResults);
    }

    void PrintStats(string name, List<LandingMetrics> list)
    {
        if (list.Count == 0) return;
        float successRate = GetSuccessRate(list);
        Debug.Log($"{name.ToUpperInvariant()} | success={successRate:F1}% | " +
                  $"V={GetAverage(list, m => m.touchdownVelocity):F2} | " +
                  $"∠={GetAverage(list, m => m.landingAngleError):F2}° | " +
                  $"miss={GetAverage(list, m => m.horizontalMiss):F1}m | " +
                  $"score={GetAverage(list, m => m.SuccessScore):F1}");
    }

    float GetAverage(List<LandingMetrics> list, System.Func<LandingMetrics, float> selector)
    {
        if (list.Count == 0) return 0f;
        float sum = 0f;
        foreach (var item in list) sum += selector(item);
        return sum / list.Count;
    }

    void SaveComparisonToCSV()
    {
        string dir = Path.Combine(Application.dataPath, "..", "SimulationLogs");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"Final_Comparison_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");

        var lines = new List<string>
        {
            "Algorithm,Tests,SuccessRate(%),AvgTouchdownVelocity,AvgAngleError,AvgHorizontalMiss,AvgFuelRemaining,AvgSuccessScore"
        };
        lines.Add(CreateCSVLine("PID", pidResults));
        lines.Add(CreateCSVLine("Fuzzy Sugeno", fuzzyResults));
        lines.Add(CreateCSVLine("Neural ES", neuralResults));
        if (includeHybrid)
            lines.Add(CreateCSVLine("Hybrid Neuro-Fuzzy", hybridResults));

        File.WriteAllLines(path, lines);
        Debug.Log($"CSV: {path}");
    }

    string CreateCSVLine(string name, List<LandingMetrics> list)
    {
        if (list.Count == 0) return $"{name},0,0,0,0,0,0,0";
        return $"{name},{list.Count},{GetSuccessRate(list):F2}," +
               $"{GetAverage(list, m => m.touchdownVelocity):F2}," +
               $"{GetAverage(list, m => m.landingAngleError):F2}," +
               $"{GetAverage(list, m => m.horizontalMiss):F2}," +
               $"{GetAverage(list, m => m.fuelRemaining):F2}," +
               $"{GetAverage(list, m => m.SuccessScore):F2}";
    }
}
