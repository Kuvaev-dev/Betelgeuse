using System.Collections;
using UnityEngine;

/// <summary>
/// Ініціалізація сцени після Load: контролери, візуал, середовище (кроками зі splash).
/// </summary>
public static class SceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBootstrap()
    {
        var host = new GameObject("BootstrapRunner");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<BootstrapRunner>();
    }
}

/// <summary>Runs heavy bootstrap across frames so splash can animate.</summary>
[DefaultExecutionOrder(-100)]
public class BootstrapRunner : MonoBehaviour
{
    void Start() => StartCoroutine(Run());

    IEnumerator Run()
    {
        var splash = SplashScreenUI.Instance;
        void Prog(float t, string uk, string en)
        {
            if (splash != null)
                splash.SetProgress(t, UILocale.IsUK ? uk : en);
        }

        // Let splash paint + spin for a couple of frames before heavy work
        yield return null;
        yield return null;

        // Lightweight defaults
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        BorderlessWindow.ApplyBorderlessChrome();
        if (SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 9000)
            QualitySettings.SetQualityLevel(Mathf.Min(QualitySettings.GetQualityLevel(), 1), true);
        if (SystemInfo.graphicsMemorySize > 0 && SystemInfo.graphicsMemorySize < 3000)
            QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, 400f);

        Prog(0.08f, "Пошук ракети…", "Finding rocket…");
        yield return Breath();

        var rocket = Object.FindAnyObjectByType<RocketPhysics>();
        if (rocket == null)
        {
            Prog(1f, "Сцена без RocketPhysics", "No RocketPhysics in scene");
            splash?.FadeOutAndDestroy(0.4f);
            Destroy(gameObject);
            yield break;
        }

        Prog(0.18f, "Контролери GNC…", "GNC controllers…");
        yield return Breath();
        EnsureControllers(rocket);

        IdealLandingPresets.ApplyDefaultControllerTuning(
            rocket,
            rocket.GetComponent<FuzzyLandingController>(),
            rocket.GetComponent<NeuralController>(),
            rocket.GetComponent<HybridController>());

        Prog(0.35f, "Модель ракетоносія…", "Building rocket…");
        yield return Breath();
        yield return RocketVisualBuilder.BuildRoutine(rocket);
        yield return Breath();

        Prog(0.55f, "Місяць і посадковий майданчик…", "Moon & landing pad…");
        yield return Breath();
        yield return EnvironmentBuilder.BuildRoutine();
        yield return Breath();

        Prog(0.72f, "Стан симуляції…", "Simulation state…");
        yield return Breath();

        rocket.simulationArmed = false;
        if (rocket.parameters != null)
        {
            rocket.state.position = rocket.parameters.startPosition;
            rocket.state.velocity = rocket.parameters.startVelocity;
            rocket.state.rotation = Quaternion.Euler(rocket.parameters.startEulerAngles);
            rocket.state.angularVelocity = Vector3.zero;
            rocket.state.currentThrust = 0f;
            rocket.state.currentFuelMass = rocket.parameters.fuelMass;
            rocket.state.dryMass = rocket.parameters.dryMass;
            rocket.state.maxThrust = rocket.parameters.maxThrust;
            rocket.state.isLanded = false;
            rocket.state.simulationFinished = false;
            rocket.state.time = 0f;
            rocket.SyncTransformWithState();
        }

        if (Object.FindAnyObjectByType<SimulationManager>() == null)
        {
            var smGo = new GameObject("SimulationManager");
            var sm = smGo.AddComponent<SimulationManager>();
            sm.rocketPhysics = rocket;
        }

        Prog(0.82f, "Камера…", "Camera…");
        yield return Breath();
        SetupCamera(rocket);

        if (Object.FindAnyObjectByType<TrajectoryVisualizer>() == null)
        {
            var tv = new GameObject("TrajectoryVisualizer");
            var vis = tv.AddComponent<TrajectoryVisualizer>();
            vis.rocketPhysics = rocket;
            vis.baseLineWidth = 6f;
        }

        foreach (var theme in Object.FindObjectsByType<MissionControlTheme>())
            theme.styleOnAwake = false;

        Prog(0.92f, "Mission Control HUD…", "Mission Control HUD…");
        yield return Breath();
        // MissionControlUI auto-creates via its own RuntimeInitialize — give it a frame
        yield return Breath();

        Prog(1f, "Готово", "Ready");
        yield return Breath();

        splash?.FadeOutAndDestroy(0.6f);
        Destroy(gameObject);
    }

    /// <summary>Yield a few frames so splash spinner/progress can animate between stalls.</summary>
    static IEnumerator Breath()
    {
        yield return null;
        yield return null;
        // Short realtime pause keeps the arc spinning even when next step is heavy
        float t = 0f;
        while (t < 0.05f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    static void EnsureControllers(RocketPhysics rocket)
    {
        if (rocket.GetComponent<FuzzyLandingController>() == null)
            rocket.gameObject.AddComponent<FuzzyLandingController>();
        if (rocket.GetComponent<NeuralController>() == null)
            rocket.gameObject.AddComponent<NeuralController>();
        if (rocket.GetComponent<HybridController>() == null)
        {
            var h = rocket.gameObject.AddComponent<HybridController>();
            h.fuzzy = rocket.GetComponent<FuzzyLandingController>();
            h.neural = rocket.GetComponent<NeuralController>();
        }
        if (rocket.GetComponent<DataLogger>() == null)
            rocket.gameObject.AddComponent<DataLogger>();

        rocket.fuzzyController = rocket.GetComponent<FuzzyLandingController>();
        rocket.neuralController = rocket.GetComponent<NeuralController>();
        rocket.hybridController = rocket.GetComponent<HybridController>();

        // Hybrid wiring + stable NN weights for presentation
        if (rocket.hybridController != null)
        {
            rocket.hybridController.fuzzy = rocket.fuzzyController;
            rocket.hybridController.neural = rocket.neuralController;
        }
        if (rocket.neuralController != null)
        {
            rocket.neuralController.LoadBestWeights();
            if (rocket.neuralController.generation <= 0
                && rocket.neuralController.bestCost >= float.MaxValue * 0.5f)
                rocket.neuralController.InstallIdealWeights();
            // Demo-safe default; UI «Train» can re-enable ES
            if (!UserSettings.Train)
                rocket.neuralController.enableTraining = false;
        }

        // Default mode Hybrid (тема МКР) if not overridden later by UI settings
        if (rocket.controlMode != RocketPhysics.ControlMode.Hybrid
            && UserSettings.ControlMode == 3)
            rocket.controlMode = RocketPhysics.ControlMode.Hybrid;
    }

    static void SetupCamera(RocketPhysics rocket)
    {
        var cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        try { cam.tag = "MainCamera"; } catch { /* tag missing */ }

        cam.farClipPlane = 16000f;
        cam.fieldOfView = 48f;
        cam.nearClipPlane = 0.3f;

        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.rocket = rocket;
        follow.trajectory = Object.FindAnyObjectByType<TrajectoryVisualizer>();
        follow.SnapNow();
    }
}
