using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Порівняльний Monte-Carlo експеримент A–D (paired seeds).
/// Прогони виконуються у фонових потоках на чистому ядрі <see cref="LandingSimulation"/>
/// (без сцени), корутина лише показує прогрес. Стартує тільки через <see cref="RequestFullExperiment"/>.
/// Ваги MLP — навчені (BestWeights_*.json), під час пакета заморожені.
/// </summary>
public class SimulationManager : MonoBehaviour
{
    [Header("Основні посилання")]
    public RocketPhysics rocketPhysics;
    public ExperimentDashboard dashboard;

    [Header("Налаштування експерименту")]
    public int testsPerAlgorithm = 15;
    public bool includeHybrid = true;
    /// <summary>Фіксований seed → ідентичні пакети (відтворюваність на захисті).</summary>
    public int experimentSeed = 42;

    [Header("Збурення (Monte-Carlo)")]
    public bool enableNoise = true;
    [Range(0f, 25f)] public float windStrength = 5f;
    [Range(0f, 20f)] public float massVariationPercent = 5f;
    [Range(0f, 15f)] public float angleVariationDegrees = 4f;
    [Range(0f, 40f)] public float positionJitterMeters = 10f;
    public bool continuousWind = true;

    [Header("Початкові умови")]
    public float startHeight = 1600f;
    public float startDescentSpeed = 60f;
    public float startTiltDeg = 2f;

    public bool IsExperimentRunning { get; private set; }
    public string ProgressLabel { get; private set; } = "";
    public float Progress01 { get; private set; }

    readonly List<LandingMetrics> pidResults = new List<LandingMetrics>();
    readonly List<LandingMetrics> fuzzyResults = new List<LandingMetrics>();
    readonly List<LandingMetrics> neuralResults = new List<LandingMetrics>();
    readonly List<LandingMetrics> hybridResults = new List<LandingMetrics>();

    CancellationTokenSource cancel;

    public static event System.Action OnExperimentStarted;
    public static event System.Action OnExperimentFinished;
    public static event System.Action<string> OnExperimentProgress;

    void Awake()
    {
        if (rocketPhysics == null) rocketPhysics = FindAnyObjectByType<RocketPhysics>();
        if (dashboard == null) dashboard = FindAnyObjectByType<ExperimentDashboard>();
    }

    void OnDestroy() => cancel?.Cancel();

    /// <summary>Єдиний спосіб старту з UI.</summary>
    public void RequestFullExperiment()
    {
        if (IsExperimentRunning || rocketPhysics == null) return;
        StartCoroutine(RunComparison());
    }

    public void CancelExperiment() => cancel?.Cancel();

    /// <summary>Збурення пакета з поточних полів (UI-слайдери або протокол).</summary>
    public DisturbanceSettings CurrentDisturbances() => new DisturbanceSettings
    {
        WindStrength = Mathf.Clamp(windStrength, 0f, 25f),
        ContinuousWind = continuousWind,
        MassVariationPercent = Mathf.Clamp(massVariationPercent, 0f, 20f),
        AngleVariationDeg = Mathf.Clamp(angleVariationDegrees, 0f, 15f),
        PositionJitterMeters = enableNoise ? Mathf.Clamp(positionJitterMeters, 0f, 40f) : 0f,
        NavNoiseScale = enableNoise ? DisturbanceSettings.DefaultNavNoise : 0f
    };

    /// <summary>ПУ пакета зі слайдерів.</summary>
    public LandingSetup CurrentSetup()
    {
        var p = rocketPhysics != null ? rocketPhysics.parameters : null;
        if (p != null)
        {
            p.ApplyStage1Vehicle();
            p.SetInitialConditions(Mathf.Clamp(startHeight, 800f, 3000f),
                Mathf.Clamp(startDescentSpeed, 30f, 120f), Mathf.Clamp(startTiltDeg, 0f, 12f));
            return p.ToSetup();
        }
        return LandingSetup.FromInitialConditions(startHeight, startDescentSpeed, startTiltDeg);
    }

    IEnumerator RunComparison()
    {
        IsExperimentRunning = true;
        testsPerAlgorithm = Mathf.Clamp(testsPerAlgorithm, 5, 200);
        cancel = new CancellationTokenSource();
        var token = cancel.Token;

        IdealLandingPresets.ClearActive();
        rocketPhysics.StopSimulation(keepPosition: false);
        var setup = CurrentSetup();
        var dist = CurrentDisturbances();
        var template = rocketPhysics.controllers.Clone();
        int n = testsPerAlgorithm, seed = experimentSeed;
        var modes = new List<ControlMode> { ControlMode.PID, ControlMode.Fuzzy, ControlMode.Neural };
        if (includeHybrid) modes.Add(ControlMode.Hybrid);
        var lists = new Dictionary<ControlMode, List<LandingMetrics>>
        {
            [ControlMode.PID] = pidResults, [ControlMode.Fuzzy] = fuzzyResults,
            [ControlMode.Neural] = neuralResults, [ControlMode.Hybrid] = hybridResults
        };
        foreach (var l in lists.Values) l.Clear();

        Debug.Log($"[MC] paired n={n} seed={seed} wind={dist.WindStrength:F1} m/s jitter={dist.PositionJitterMeters:F0} m " +
                  $"nav={dist.NavNoiseScale:F2} residual={template.hybrid.useNeuralResidual} | {rocketPhysics.WeightsInfo}");

        MissionControlUI.Instance?.SetBatchMode(true);
        OnExperimentStarted?.Invoke();
        SetProgress(UILocale.T("prog_start"), 0f);

        int done = 0, total = n * modes.Count;
        var results = new Dictionary<ControlMode, LandingMetrics[]>();
        var task = Task.Run(() =>
        {
            foreach (var mode in modes)
            {
                if (token.IsCancellationRequested) break;
                results[mode] = LandingExperiment.RunPaired(setup, template, mode, dist, seed, n,
                    onTrialDone: _ => Interlocked.Increment(ref done), cancel: token);
            }
        }, token);

        while (!task.IsCompleted)
        {
            int d = Volatile.Read(ref done);
            int algo = Mathf.Clamp(d / Mathf.Max(1, n), 0, modes.Count - 1);
            SetProgress(string.Format(UILocale.T("prog_run"), modes[algo], d % n + 1, n), (float)d / total);
            yield return null;
        }

        bool ok = !token.IsCancellationRequested && task.Status == TaskStatus.RanToCompletion;
        if (ok)
        {
            foreach (var kv in results)
                if (kv.Value != null) lists[kv.Key].AddRange(kv.Value);
            ShowFinalComparison();
            RepublishComparisonStatistics();
            string exportDir = SaveComparisonReports();
            SetProgress(UILocale.T("prog_done"), 1f);
            MissionControlUI.Instance?.NotifyInfo(string.Format(UILocale.T("msg_compare_export"), exportDir));
        }
        else
        {
            if (task.Exception != null) Debug.LogException(task.Exception);
            SetProgress(UILocale.T("prog_cancel"), Progress01);
            MissionControlUI.Instance?.NotifyInfo(UILocale.T("msg_compare_stopped"));
        }

        IsExperimentRunning = false;
        cancel.Dispose();
        cancel = null;
        MissionControlUI.Instance?.SetBatchMode(false);
        OnExperimentFinished?.Invoke();
    }

    void SetProgress(string label, float p01)
    {
        ProgressLabel = label;
        Progress01 = Mathf.Clamp01(p01);
        OnExperimentProgress?.Invoke(label);
        MissionControlUI.Instance?.SetExperimentProgress(label, Progress01);
    }

    static float GetSuccessRate(List<LandingMetrics> list)
        => list.Count > 0 ? (float)list.FindAll(m => m.isSuccessfulLanding).Count / list.Count * 100f : 0f;

    static float GetAverage(List<LandingMetrics> list, System.Func<LandingMetrics, float> selector)
    {
        if (list.Count == 0) return 0f;
        float sum = 0f;
        foreach (var item in list) sum += selector(item);
        return sum / list.Count;
    }

    void ShowFinalComparison()
    {
        PrintStats("PID", pidResults);
        PrintStats("Fuzzy Sugeno", fuzzyResults);
        PrintStats("Neural MLP", neuralResults);
        if (includeHybrid) PrintStats("Hybrid Neuro-Fuzzy", hybridResults);
    }

    static void PrintStats(string name, List<LandingMetrics> list)
    {
        if (list.Count == 0) return;
        int timeouts = list.FindAll(m => m.timedOut).Count;
        Debug.Log($"{name.ToUpperInvariant()} | success={GetSuccessRate(list):F1}% | " +
                  $"V={GetAverage(list, m => m.touchdownVelocity):F2} | ∠={GetAverage(list, m => m.landingAngleError):F2}° | " +
                  $"miss={GetAverage(list, m => m.horizontalMiss):F1}m | Vh={GetAverage(list, m => m.horizontalSpeed):F1} | " +
                  $"timeouts={timeouts}/{list.Count} | score={GetAverage(list, m => m.SuccessScore):F1}");
    }

    public string SaveComparisonReports() => ResearchExporter.ExportComparison(BuildComparisonExportData());

    public ResearchExporter.ComparisonExportData BuildComparisonExportData()
    {
        var data = new ResearchExporter.ComparisonExportData
        {
            timestamp = ResearchExporter.Stamp(),
            testsPerAlgorithm = testsPerAlgorithm,
            enableNoise = enableNoise,
            windStrength = windStrength,
            massVariationPercent = massVariationPercent,
            angleVariationDegrees = angleVariationDegrees,
            positionJitterMeters = enableNoise ? positionJitterMeters : 0f,
            continuousWind = continuousWind,
            experimentSeed = experimentSeed,
            protocolVersion = DefenseBaseline.ProtocolVersion,
            pairedSeeds = true,
            hybridResidual = rocketPhysics != null && rocketPhysics.hybridController.useNeuralResidual,
            weightsInfo = rocketPhysics != null ? rocketPhysics.WeightsInfo : "",
            startHeight = startHeight,
            startDescentSpeed = startDescentSpeed,
            startTiltDeg = startTiltDeg
        };
        data.algorithms.Add(ResearchExporter.ComputeStats("PID", pidResults));
        data.algorithms.Add(ResearchExporter.ComputeStats("Fuzzy Sugeno", fuzzyResults));
        data.algorithms.Add(ResearchExporter.ComputeStats("Neural MLP", neuralResults));
        if (includeHybrid)
            data.algorithms.Add(ResearchExporter.ComputeStats("Hybrid Neuro-Fuzzy", hybridResults));
        return data;
    }

    public IReadOnlyList<LandingMetrics> PidResults => pidResults;
    public IReadOnlyList<LandingMetrics> FuzzyResults => fuzzyResults;
    public IReadOnlyList<LandingMetrics> NeuralResults => neuralResults;
    public IReadOnlyList<LandingMetrics> HybridResults => hybridResults;

    /// <summary>Повторно передати останні % в UI (після перебудови мови/теми).</summary>
    public void RepublishComparisonStatistics()
    {
        if (!HasComparisonResults) return;
        float pid = GetSuccessRate(pidResults), fuzzy = GetSuccessRate(fuzzyResults);
        float neural = GetSuccessRate(neuralResults), hybrid = GetSuccessRate(hybridResults);
        float pidSc = GetAverage(pidResults, m => m.SuccessScore), fzSc = GetAverage(fuzzyResults, m => m.SuccessScore);
        float nnSc = GetAverage(neuralResults, m => m.SuccessScore), hySc = GetAverage(hybridResults, m => m.SuccessScore);
        dashboard?.UpdateStatistics(pid, fuzzy, neural, hybrid, pidSc, fzSc, nnSc, hySc);
        MissionControlUI.Instance?.UpdateStatistics(pid, fuzzy, neural, hybrid, pidSc, fzSc, nnSc, hySc);
    }

    public bool HasComparisonResults =>
        pidResults.Count > 0 || fuzzyResults.Count > 0 || neuralResults.Count > 0 || hybridResults.Count > 0;
}
