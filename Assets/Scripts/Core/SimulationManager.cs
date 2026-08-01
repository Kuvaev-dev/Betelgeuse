using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Менеджер порівняльних Monte-Carlo експериментів.
/// НЕ стартує сам — лише через RequestFullExperiment() з UI.
/// Послідовно: PID → Fuzzy → Neural → Hybrid (N запусків кожен),
/// з випадковим вітром/масою/кутом. Результати → UI + ResearchExporter.
/// </summary>
public class SimulationManager : MonoBehaviour
{
    [Header("Основні посилання")]
    public RocketPhysics rocketPhysics;
    public ExperimentDashboard dashboard;

    [Header("Налаштування експерименту")]
    public int testsPerAlgorithm = 15;
    public float delayBetweenTests = 0.05f;
    public bool includeHybrid = true;
    [Range(1f, 50f)] public float experimentTimeScale = 20f;

    [Header("Невизначеність (Monte-Carlo)")]
    public bool enableNoise = true;
    [Range(0f, 25f)] public float windStrength = 10f;
    [Range(0f, 15f)] public float massVariationPercent = 6f;
    [Range(0f, 10f)] public float angleVariationDegrees = 7f;
    public bool continuousWind = true;

    // Internal flag — never leave true in inspector permanently
    [HideInInspector] public bool runFullExperiment;

    public bool IsExperimentRunning { get; private set; }
    public string ProgressLabel { get; private set; } = "";
    public float Progress01 { get; private set; }

    readonly List<LandingMetrics> pidResults = new();
    readonly List<LandingMetrics> fuzzyResults = new();
    readonly List<LandingMetrics> neuralResults = new();
    readonly List<LandingMetrics> hybridResults = new();

    float originalFuelMass;
    bool cancelRequested;
    TrajectoryVisualizer visualizer;
    RocketPhysics.ControlMode modeBeforeExperiment;
    Coroutine running;

    public static event System.Action OnExperimentStarted;
    public static event System.Action OnExperimentFinished;
    public static event System.Action<string> OnExperimentProgress;

    void Awake()
    {
        // CRITICAL: never auto-start from a checked inspector box
        runFullExperiment = false;
        IsExperimentRunning = false;

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
        if (!runFullExperiment || IsExperimentRunning) return;
        runFullExperiment = false;
        running = StartCoroutine(RunFullComparisonExperiment());
    }

    /// <summary>Єдиний правильний спосіб старту з UI.</summary>
    public void RequestFullExperiment()
    {
        if (IsExperimentRunning) return;
        runFullExperiment = true;
    }

    public void CancelExperiment()
    {
        cancelRequested = true;
        if (!IsExperimentRunning)
        {
            runFullExperiment = false;
            return;
        }
        // Coroutine checks cancelRequested each loop
    }

    IEnumerator RunFullComparisonExperiment()
    {
        if (rocketPhysics == null)
        {
            Debug.LogError("[Sim] RocketPhysics missing");
            yield break;
        }

        IsExperimentRunning = true;
        cancelRequested = false;
        modeBeforeExperiment = rocketPhysics.controlMode;
        float prevScale = Time.timeScale;
        float prevFixed = Time.fixedDeltaTime;

        float step = rocketPhysics.parameters != null ? rocketPhysics.parameters.fixedTimeStep : 0.005f;
        Time.timeScale = Mathf.Clamp(experimentTimeScale, 1f, 50f);
        Time.fixedDeltaTime = step;

        // Hide landing result popups during batch
        MissionControlUI.Instance?.SetBatchMode(true);
        OnExperimentStarted?.Invoke();
        SetProgress("Старт авто-тесту…", 0f);

        int algos = includeHybrid ? 4 : 3;
        int doneAlgos = 0;

        yield return RunAlgoBlock(RocketPhysics.ControlMode.PID, "PID", pidResults, doneAlgos, algos);
        doneAlgos++;
        if (cancelRequested) goto cleanup;

        yield return RunAlgoBlock(RocketPhysics.ControlMode.Fuzzy, "Нечітка логіка", fuzzyResults, doneAlgos, algos);
        doneAlgos++;
        if (cancelRequested) goto cleanup;

        yield return RunAlgoBlock(RocketPhysics.ControlMode.Neural, "Нейромережа", neuralResults, doneAlgos, algos);
        doneAlgos++;
        if (cancelRequested) goto cleanup;

        if (includeHybrid)
        {
            yield return RunAlgoBlock(RocketPhysics.ControlMode.Hybrid, "Гібрид", hybridResults, doneAlgos, algos);
            doneAlgos++;
        }

        if (!cancelRequested)
        {
            ShowFinalComparison();
            float pid = GetSuccessRate(pidResults);
            float fuzzy = GetSuccessRate(fuzzyResults);
            float neural = GetSuccessRate(neuralResults);
            float hybrid = GetSuccessRate(hybridResults);

            dashboard?.UpdateStatistics(pid, fuzzy, neural, hybrid);
            MissionControlUI.Instance?.UpdateStatistics(pid, fuzzy, neural, hybrid);
            string exportDir = SaveComparisonReports();
            SetProgress("Авто-тест завершено", 1f);
            MissionControlUI.Instance?.NotifyInfo(
                $"✓ Авто-тест завершено. Звіти CSV/JSON/MD збережено:\n{exportDir}");
        }
        else
        {
            SetProgress("Авто-тест скасовано", Progress01);
            MissionControlUI.Instance?.NotifyInfo("Авто-тест зупинено користувачем.");
        }

        cleanup:
        // Restore user's chosen algorithm and idle state
        rocketPhysics.controlMode = modeBeforeExperiment;
        rocketPhysics.StopSimulation(keepPosition: false);
        Time.timeScale = prevScale > 0.01f ? prevScale : 1f;
        Time.fixedDeltaTime = prevFixed;
        IsExperimentRunning = false;
        running = null;
        MissionControlUI.Instance?.SetBatchMode(false);
        OnExperimentFinished?.Invoke();
        Debug.Log(cancelRequested ? "══ Експеримент скасовано ══" : "══ Експеримент завершено ══");
    }

    IEnumerator RunAlgoBlock(RocketPhysics.ControlMode mode, string label,
        List<LandingMetrics> results, int algoIndex, int algoTotal)
    {
        rocketPhysics.controlMode = mode;
        results.Clear();
        Debug.Log($"▶ {label}: {testsPerAlgorithm} симуляцій...");

        float maxT = rocketPhysics.parameters != null
            ? rocketPhysics.parameters.maxSimulationTime + 5f
            : 420f;

        for (int i = 0; i < testsPerAlgorithm; i++)
        {
            if (cancelRequested) yield break;

            float local = (i + 1f) / testsPerAlgorithm;
            float global = (algoIndex + local) / algoTotal;
            SetProgress($"Авто-тест: {label}  ·  запуск {i + 1}/{testsPerAlgorithm}", global);

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
                if (cancelRequested) yield break;
                waited += Time.deltaTime;
                yield return null;
            }

            if (!rocketPhysics.state.simulationFinished)
            {
                rocketPhysics.state.simulationFinished = true;
                rocketPhysics.state.isLanded = true;
            }

            results.Add(CloneMetrics(rocketPhysics.metrics));
        }
    }

    void SetProgress(string label, float p01)
    {
        ProgressLabel = label;
        Progress01 = Mathf.Clamp01(p01);
        OnExperimentProgress?.Invoke(label);
        MissionControlUI.Instance?.SetExperimentProgress(label, p01);
    }

    float GetSuccessRate(List<LandingMetrics> list)
        => list.Count > 0
            ? (float)list.FindAll(m => m.isSuccessfulLanding).Count / list.Count * 100f
            : 0f;

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
        rocketPhysics.windVelocity = continuousWind ? windKick * 0.35f : Vector3.zero;

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

    /// <summary>Повний експорт порівняння (CSV + JSON + Markdown).</summary>
    public string SaveComparisonReports()
    {
        var data = BuildComparisonExportData();
        // Legacy short CSV for backwards compatibility
        string dir = ResearchExporter.LogsDirectory;
        string legacy = Path.Combine(dir, $"Final_Comparison_{data.timestamp}.csv");
        var lines = new List<string>
        {
            "Algorithm,Tests,SuccessRate(%),AvgTouchdownVelocity,AvgAngleError,AvgHorizontalMiss,AvgFuelRemaining,AvgSuccessScore"
        };
        foreach (var a in data.algorithms)
        {
            lines.Add($"{a.name},{a.tests},{a.successRate:F2},{a.avgTouchdownVelocity:F2}," +
                      $"{a.avgAngleError:F2},{a.avgHorizontalMiss:F2},{a.avgFuelRemaining:F2},{a.avgSuccessScore:F2}");
        }
        File.WriteAllLines(legacy, lines);
        return ResearchExporter.ExportComparison(data);
    }

    public ResearchExporter.ComparisonExportData BuildComparisonExportData()
    {
        string stamp = ResearchExporter.Stamp();
        var data = new ResearchExporter.ComparisonExportData
        {
            timestamp = stamp,
            testsPerAlgorithm = testsPerAlgorithm,
            enableNoise = enableNoise,
            windStrength = windStrength,
            massVariationPercent = massVariationPercent,
            angleVariationDegrees = angleVariationDegrees
        };
        data.algorithms.Add(ResearchExporter.ComputeStats("PID", pidResults));
        data.algorithms.Add(ResearchExporter.ComputeStats("Fuzzy Sugeno", fuzzyResults));
        data.algorithms.Add(ResearchExporter.ComputeStats("Neural ES", neuralResults));
        if (includeHybrid)
            data.algorithms.Add(ResearchExporter.ComputeStats("Hybrid Neuro-Fuzzy", hybridResults));
        return data;
    }

    /// <summary>Доступ до останніх результатів (для UI-експорту / тестів).</summary>
    public IReadOnlyList<LandingMetrics> PidResults => pidResults;
    public IReadOnlyList<LandingMetrics> FuzzyResults => fuzzyResults;
    public IReadOnlyList<LandingMetrics> NeuralResults => neuralResults;
    public IReadOnlyList<LandingMetrics> HybridResults => hybridResults;

    public bool HasComparisonResults =>
        pidResults.Count > 0 || fuzzyResults.Count > 0 || neuralResults.Count > 0 || hybridResults.Count > 0;
}
