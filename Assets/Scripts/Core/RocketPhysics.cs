using System.IO;
using UnityEngine;

/// <summary>
/// Компонент сцени для 1-го ступеня: тонка обгортка над чистим ядром <see cref="LandingSimulation"/>.
/// Відповідає лише за зв’язок із Unity: такт FixedUpdate, трансформ, лог, лінію траєкторії, камеру, UI.
/// Уся фізика і GNC — у <see cref="LandingSimulation"/> (те саме ядро, що в Monte-Carlo і навчанні ES).
/// </summary>
[RequireComponent(typeof(DataLogger))]
public class RocketPhysics : MonoBehaviour
{
    [Header("Параметри апарата і ПУ")]
    public SimulationParameters parameters;

    [Header("Режим керування (A–D)")]
    public ControlMode controlMode = ControlMode.Hybrid;

    [Header("Стратегії керування")]
    public ControllerSet controllers = new ControllerSet();

    [Header("Запуск")]
    [Tooltip("false = ступінь чекає кнопки «Запустити посадку»")]
    public bool simulationArmed;
    /// <summary>Пауза польоту — такт пропускається, стан зберігається.</summary>
    public bool simulationPaused;

    /// <summary>Ядро симуляції (plant + GNC).</summary>
    public readonly LandingSimulation simulation = new LandingSimulation();

    public RocketState state => simulation.State;
    public LandingMetrics metrics => simulation.Metrics;
    public NavigationEstimator navigation => simulation.Navigation;
    public FuzzyLandingController fuzzyController => controllers.fuzzy;
    public NeuralController neuralController => controllers.neural;
    public HybridController hybridController => controllers.hybrid;
    public Vector3 windVelocity => simulation.Wind;

    /// <summary>Каталог проєкту з BestWeights_*.json.</summary>
    public static string WeightsDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    /// <summary>Опис завантажених ваг MLP (для UI / звіту).</summary>
    public string WeightsInfo { get; private set; } = "";

    public static string EnvironmentLabel => "Earth LZ · Stage-1 · after separation";

    DataLogger logger;
    TrajectoryVisualizer cachedVisualizer;
    bool finishHandled;

    void Awake()
    {
        logger = GetComponent<DataLogger>();
        ReloadWeights();
        PrepareMode(controlMode);
    }

    /// <summary>Перечитати навчені ваги з диска (після навчання ES).</summary>
    public void ReloadWeights()
    {
        WeightsInfo = controllers.LoadWeights(WeightsDirectory);
        Debug.Log("[NN] " + WeightsInfo);
    }

    LandingSetup CurrentSetup()
        => parameters != null ? parameters.ToSetup() : LandingSetup.Default;

    void FixedUpdate()
    {
        if (!simulationArmed || simulationPaused || simulation.Finished) return;

        float dt = simulation.Setup.Dt;
        if (!Mathf.Approximately(Time.fixedDeltaTime, dt)) Time.fixedDeltaTime = dt;

        simulation.Step();
        SyncTransformWithState();
        if (logger != null) logger.Log(state);
        Visualizer()?.SampleFlight(force: false);

        if (simulation.Finished) OnLandingFinished();
    }

    /// <summary>
    /// Старт посадки з поточних ПУ і збурень (один детермінований сценарій з seed).
    /// </summary>
    public void StartLanding(in DisturbanceSettings disturbances, int seed)
    {
        simulation.Controller = controllers.Get(controlMode);
        simulation.Reset(CurrentSetup(), LandingScenario.Sample(disturbances, seed));
        simulationArmed = true;
        simulationPaused = false;
        finishHandled = false;
        Time.fixedDeltaTime = simulation.Setup.Dt;
        SyncTransformWithState();
        if (logger != null) logger.Initialize();
        Visualizer()?.Clear();
        MissionControlUI.Instance?.HideLandingResult();
        SnapCamera();
    }

    /// <summary>Старт у номінальних умовах (без збурень).</summary>
    public void ResetSimulation() => StartLanding(DisturbanceSettings.None, 1);

    /// <summary>Вибір режиму без старту: ступінь у ПУ, очікує запуску.</summary>
    public void PrepareMode(ControlMode mode)
    {
        controlMode = mode;
        simulation.Controller = controllers.Get(mode);
        simulation.Reset(CurrentSetup(), LandingScenario.Nominal());
        simulationArmed = false;
        simulationPaused = false;
        finishHandled = false;
        SyncTransformWithState();
        if (logger != null) logger.Initialize();
        Visualizer()?.Clear();
        MissionControlUI.Instance?.HideLandingResult();
        SnapCamera();
    }

    /// <summary>Прев’ю ПУ в режимі очікування (слайдери UI) без інших побічних ефектів.</summary>
    public void PreviewInitialConditions()
    {
        if (simulationArmed) return;
        simulation.Reset(CurrentSetup(), LandingScenario.Nominal());
        SyncTransformWithState();
    }

    /// <summary>Живе оновлення вітру зі слайдера (напрям зберігається; новий — випадковий).</summary>
    public void ApplyLiveWind(float windStrength)
        => simulation.SetWindStrength(windStrength, SimRng.Range(0f, Mathf.PI * 2f));

    /// <summary>Примусове завершення з метриками (СТОП з оцінкою).</summary>
    public void ForceFinish(bool asTimeout)
    {
        if (simulation.Finished) return;
        simulation.ForceFinish(asTimeout);
        OnLandingFinished();
    }

    /// <summary>Зупинити політ (кнопка СТОП). keepPosition=false — повернути в ПУ.</summary>
    public void StopSimulation(bool keepPosition = true)
    {
        simulationArmed = false;
        simulationPaused = false;
        Time.timeScale = 1f;
        if (keepPosition)
        {
            state.simulationFinished = true;
            state.isLanded = true;
            state.currentThrust = 0f;
            state.velocity = Vector3.zero;
            state.angularVelocity = Vector3.zero;
            SyncTransformWithState();
            FindAnyObjectByType<CameraFollow>()?.StayOnRocketAfterAbort();
        }
        else
        {
            PrepareMode(controlMode);
        }
    }

    void OnLandingFinished()
    {
        if (finishHandled) return;
        finishHandled = true;
        SyncTransformWithState();
        if (logger != null) logger.Save();
        metrics.PrintResults(GetModeDisplayName());
        Visualizer()?.OnSimulationFinished(metrics.isSuccessfulLanding);
        FindAnyObjectByType<CameraFollow>()?.StayOnRocketAfterAbort();
        MissionControlUI.Instance?.ShowLandingResult(metrics);
    }

    public string GetLastTrajectoryPath()
    {
        if (logger == null) logger = GetComponent<DataLogger>();
        return logger != null ? logger.LastFilePath : null;
    }

    public void SyncTransformWithState()
    {
        transform.position = state.position;
        transform.rotation = state.rotation;
    }

    TrajectoryVisualizer Visualizer()
    {
        if (cachedVisualizer == null) cachedVisualizer = FindAnyObjectByType<TrajectoryVisualizer>();
        return cachedVisualizer;
    }

    static void SnapCamera() => FindAnyObjectByType<CameraFollow>()?.SnapNow();

    public string GetModeDisplayName() => ModeDisplayName(controlMode);

    public static string ModeDisplayName(ControlMode mode) => mode switch
    {
        ControlMode.Fuzzy => "Нечітка логіка (Sugeno)",
        ControlMode.Neural => "Нейромережа (MLP, ES)",
        ControlMode.Hybrid => "Гібрид Neuro-Fuzzy",
        _ => "Класичний PID"
    };
}
