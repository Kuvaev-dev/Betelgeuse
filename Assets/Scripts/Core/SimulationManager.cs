using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    // Диференційовані ставки A–D (надто жорстко → універсальний timeout/0%; надто м’яко → усі 100%)
    [Range(0f, 25f)] public float windStrength = 10f;
    [Range(0f, 20f)] public float massVariationPercent = 8f;
    [Range(0f, 15f)] public float angleVariationDegrees = 8f;
    [Range(0f, 80f)] public float positionJitterMeters = 22f;
    public bool continuousWind = true;
    /// <summary>Фіксований seed → ідентичні Comparison-пакети (відтворюваність на захисті).</summary>
    public int experimentSeed = 42;

    [Header("Початкові умови (з UI)")]
    public float startHeight = 1800f;
    public float startDescentSpeed = 72f;
    public float startTiltDeg = 3.5f;

    // Внутрішній прапорець — ніколи не лишати true в inspector назавжди
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
        // КРИТИЧНО: ніколи не автозапускати з увімкненого прапорця в inspector
        runFullExperiment = false;
        IsExperimentRunning = false;

        if (rocketPhysics == null)
            rocketPhysics = FindAnyObjectByType<RocketPhysics>();
        if (dashboard == null)
            dashboard = FindAnyObjectByType<ExperimentDashboard>();
        visualizer = FindAnyObjectByType<TrajectoryVisualizer>();

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
        // Корутина перевіряє cancelRequested на кожній ітерації
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
        rocketPhysics.batchDrivenTicks = true;
        // MC лише на ділянці посадки 1-го ступеня (після sep)
        rocketPhysics.skipStackPhase = true;

        // Справедливий paired Monte-Carlo протокол (seeded, однакові ПУ/збурення для A–D)
        DefenseBaseline.ApplyTo(this);
        if (rocketPhysics.hybridController != null)
            rocketPhysics.hybridController.useNeuralResidual = DefenseBaseline.HybridResidualOn;

        // Обмежити збурення робочою смугою (збережені prefs можуть бути екстремальними → універсальні 0%)
        windStrength = Mathf.Clamp(windStrength, 0f, 18f);
        massVariationPercent = Mathf.Clamp(massVariationPercent, 0f, 12f);
        angleVariationDegrees = Mathf.Clamp(angleVariationDegrees, 0f, 12f);
        positionJitterMeters = Mathf.Clamp(positionJitterMeters, 0f, 40f);
        experimentTimeScale = Mathf.Clamp(experimentTimeScale, 4f, 50f);
        testsPerAlgorithm = Mathf.Clamp(testsPerAlgorithm, 5, 40);

        SimRng.Reseed(experimentSeed);

        // Monte-Carlo бере ПУ DefenseBaseline (не залишкову «м’якість» Ideal [I])
        RestoreHardInitialConditions();
        IdealLandingPresets.ApplyDefaultControllerTuning(
            rocketPhysics,
            rocketPhysics.fuzzyController,
            rocketPhysics.neuralController,
            rocketPhysics.hybridController);

        // Стабільні ваги NN для справедливого порівняння A–D (без ES drift mid-pack)
        if (rocketPhysics.neuralController != null)
        {
            rocketPhysics.neuralController.enableTraining = false;
            // Завжди фіксувати детерміновані ваги для відтворюваних Comparison-пакетів
            rocketPhysics.neuralController.InstallIdealWeights();
        }

        // Тримати realtime clock; швидкість з burst SimulationTick (не timeScale).
        float step = rocketPhysics.parameters != null ? rocketPhysics.parameters.fixedTimeStep : 0.005f;
        step = Mathf.Clamp(step, 0.002f, 0.02f);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = step;

        // Ховати попапи результату посадки під час batch
        // Лінія шляху має лишатись вимк/очищеною під час batch — увімкнення mid-pack зависає
        visualizer?.Clear();
        visualizer?.SetVisible(false);

        MissionControlUI.Instance?.SetBatchMode(true);
        OnExperimentStarted?.Invoke();
        SetProgress(UILocale.T("prog_start"), 0f);

        int algos = includeHybrid ? 4 : 3;
        int doneAlgos = 0;

        yield return RunAlgoBlock(RocketPhysics.ControlMode.PID, "PID", pidResults, doneAlgos, algos);
        doneAlgos++;
        if (cancelRequested) goto cleanup;

        yield return RunAlgoBlock(RocketPhysics.ControlMode.Fuzzy, "Fuzzy", fuzzyResults, doneAlgos, algos);
        doneAlgos++;
        if (cancelRequested) goto cleanup;

        yield return RunAlgoBlock(RocketPhysics.ControlMode.Neural, "Neural", neuralResults, doneAlgos, algos);
        doneAlgos++;
        if (cancelRequested) goto cleanup;

        if (includeHybrid)
        {
            yield return RunAlgoBlock(RocketPhysics.ControlMode.Hybrid, "Hybrid", hybridResults, doneAlgos, algos);
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
            SetProgress(UILocale.T("prog_done"), 1f);
            MissionControlUI.Instance?.NotifyInfo(
                string.Format(UILocale.T("msg_compare_export"), exportDir));
        }
        else
        {
            SetProgress(UILocale.T("prog_cancel"), Progress01);
            MissionControlUI.Instance?.NotifyInfo(UILocale.T("msg_compare_stopped"));
        }

        cleanup:
        // Відновити обраний користувачем алгоритм і idle-стан
        if (rocketPhysics != null)
            rocketPhysics.batchDrivenTicks = false;
        if (rocketPhysics != null)
            rocketPhysics.controlMode = modeBeforeExperiment;
        rocketPhysics?.StopSimulation(keepPosition: false);
        visualizer?.Clear();
        Time.timeScale = prevScale > 0.01f ? prevScale : 1f;
        Time.fixedDeltaTime = prevFixed;
        IsExperimentRunning = false;
        running = null;
        MissionControlUI.Instance?.SetBatchMode(false);
        OnExperimentFinished?.Invoke();
        Debug.Log(cancelRequested ? "[MC] cancelled" : "[MC] finished");
    }

    IEnumerator RunAlgoBlock(RocketPhysics.ControlMode mode, string label,
        List<LandingMetrics> results, int algoIndex, int algoTotal)
    {
        rocketPhysics.controlMode = mode;
        rocketPhysics.batchDrivenTicks = true;
        rocketPhysics.simulationPaused = false;
        results.Clear();
        Debug.Log($"[MC] {label}: {testsPerAlgorithm} runs, seed={experimentSeed}, paired=true, protocol=v{DefenseBaseline.ProtocolVersion}");

        float maxT = rocketPhysics.parameters != null
            ? Mathf.Max(120f, rocketPhysics.parameters.maxSimulationTime)
            : 400f;

        for (int i = 0; i < testsPerAlgorithm; i++)
        {
            if (cancelRequested) yield break;

            float local = (i + 1f) / testsPerAlgorithm;
            float global = (algoIndex + local) / algoTotal;
            SetProgress(string.Format(UILocale.T("prog_run"), label, i + 1, testsPerAlgorithm), global);

            if (rocketPhysics.parameters != null)
            {
                rocketPhysics.parameters.fuelMass = originalFuelMass;
                // Тримати крок стабільним для RK4 burst
                if (rocketPhysics.parameters.fixedTimeStep < 0.001f
                    || rocketPhysics.parameters.fixedTimeStep > 0.02f)
                    rocketPhysics.parameters.fixedTimeStep = 0.005f;
            }

            // Paired seed: trial i використовує ТІ САМІ збурення для кожного алгоритму
            SimRng.Reseed(SimRng.DeriveSeed(experimentSeed, i));

            rocketPhysics.ResetSimulation();
            rocketPhysics.controlMode = mode;
            rocketPhysics.batchDrivenTicks = true;
            rocketPhysics.simulationArmed = true;
            rocketPhysics.simulationPaused = false;

            // Вітер + маса/кут/offset (ідентично для A–D на цьому індексі trial)
            ApplyRandomNoiseToState();

            float dt = rocketPhysics.parameters != null ? rocketPhysics.parameters.fixedTimeStep : 0.005f;
            dt = Mathf.Clamp(dt, 0.002f, 0.02f);
            int maxSteps = Mathf.CeilToInt(maxT / dt) + 128;
            // Великий burst — завершити кожен trial за кілька кадрів (logger вимкнено в batch)
            int burst = Mathf.Clamp(Mathf.RoundToInt(experimentTimeScale * 8f), 40, 400);
            int steps = 0;
            while (!rocketPhysics.state.simulationFinished && steps < maxSteps)
            {
                if (cancelRequested) yield break;
                for (int b = 0; b < burst && !rocketPhysics.state.simulationFinished && steps < maxSteps; b++)
                {
                    rocketPhysics.SimulationTick();
                    steps++;
                }
                // Періодичний yield, щоб UI progress оновлювався (не кожен micro-burst)
                if ((steps / burst) % 2 == 0)
                    yield return null;
            }

            if (!rocketPhysics.state.simulationFinished)
            {
                // Біля поверхні pad без finish-прапорця → рахувати touchdown, не timeout
                float ground = EnvironmentBuilder.PadSurfaceY;
                bool nearPad = rocketPhysics.state.position.y < ground + 2f;
                rocketPhysics.ForceFinish(asTimeout: !nearPad);
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

    /// <summary>ПУ зі слайдерів UI (гнучке налаштування експерименту).</summary>
    void RestoreHardInitialConditions()
    {
        if (rocketPhysics?.parameters == null) return;
        var p = rocketPhysics.parameters;
        float h0 = Mathf.Clamp(startHeight, 800f, 3000f);
        float vy = Mathf.Clamp(startDescentSpeed, 30f, 120f);
        float tilt = Mathf.Clamp(startTiltDeg, 0f, 12f);
        p.startPosition = new Vector3(0f, h0, 0f);
        p.startVelocity = new Vector3(0f, -vy, 0f);
        p.startEulerAngles = new Vector3(0f, 0f, tilt);
        p.dryMass = 25600f;
        p.fuelMass = 14000f;
        p.maxThrust = 845000f;
        // Тримати soft-landing gates узгодженими з defaults LandingCriteria
        p.maxTouchdownVelocity = LandingCriteria.DefaultMaxTouchdownVelocity;
        p.maxLandingAngle = LandingCriteria.DefaultMaxLandingAngle;
        p.maxHorizontalMiss = LandingCriteria.DefaultMaxHorizontalMiss;
        p.maxHorizontalSpeed = LandingCriteria.DefaultMaxHorizontalSpeed;
        originalFuelMass = p.fuelMass;
    }

    void ApplyRandomNoiseToState()
    {
        if (rocketPhysics?.state == null) return;

        // Вітер завжди в протоколі MC, коли strength > 0 (не gated toggle шуму)
        float w = Mathf.Max(windStrength, 0f);
        Vector3 windKick = new Vector3(
            SimRng.Range(-w, w),
            0f,
            SimRng.Range(-w * 0.55f, w * 0.55f));
        // М’який kick + легкий постійний вітер — відновлювано; все ще тисне слабкий lateral (PID)
        rocketPhysics.state.velocity += windKick * 0.45f;
        rocketPhysics.windVelocity = continuousWind && w > 0.05f ? windKick * 0.1f : Vector3.zero;
        rocketPhysics.applyContinuousWind = continuousWind && w > 0.05f;

        if (enableNoise)
        {
            float massNoise = 1f + SimRng.Range(-massVariationPercent, massVariationPercent) / 100f;
            rocketPhysics.state.currentFuelMass = Mathf.Max(800f, rocketPhysics.state.currentFuelMass * massNoise);

            float ax = SimRng.Range(-angleVariationDegrees, angleVariationDegrees);
            float az = SimRng.Range(-angleVariationDegrees, angleVariationDegrees);
            rocketPhysics.state.rotation = Quaternion.Normalize(
                rocketPhysics.state.rotation * Quaternion.Euler(ax, 0f, az));

            // Бічний offset — головний диференціатор (PID слабкий / Hybrid сильний lateral)
            float jit = Mathf.Max(0f, positionJitterMeters);
            if (jit > 0.1f)
            {
                rocketPhysics.state.position.x += SimRng.Range(-jit, jit);
                rocketPhysics.state.position.z += SimRng.Range(-jit, jit);
            }
        }

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

    /// <summary>Повний експорт порівняння в окремий каталог Comparison_*.</summary>
    public string SaveComparisonReports()
    {
        var data = BuildComparisonExportData();
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
            angleVariationDegrees = angleVariationDegrees,
            positionJitterMeters = positionJitterMeters,
            continuousWind = continuousWind,
            experimentSeed = experimentSeed,
            protocolVersion = DefenseBaseline.ProtocolVersion,
            pairedSeeds = true,
            hybridResidual = rocketPhysics != null
                && rocketPhysics.hybridController != null
                && rocketPhysics.hybridController.useNeuralResidual,
            startHeight = startHeight,
            startDescentSpeed = startDescentSpeed,
            startTiltDeg = startTiltDeg
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
