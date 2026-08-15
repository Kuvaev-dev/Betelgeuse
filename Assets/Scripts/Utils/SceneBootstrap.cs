using System.Collections;
using UnityEngine;

/// <summary>
/// Scene init after Load: controllers, visuals, environment — stepped with splash progress.
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

        // Lightweight defaults
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        BorderlessWindow.ApplyBorderlessChrome();
        if (SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 9000)
            QualitySettings.SetQualityLevel(Mathf.Min(QualitySettings.GetQualityLevel(), 1), true);
        if (SystemInfo.graphicsMemorySize > 0 && SystemInfo.graphicsMemorySize < 3000)
            QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, 400f);

        Prog(0.08f, "Пошук ракети…", "Finding rocket…");
        yield return null;

        var rocket = Object.FindAnyObjectByType<RocketPhysics>();
        if (rocket == null)
        {
            Prog(1f, "Сцена без RocketPhysics", "No RocketPhysics in scene");
            splash?.FadeOutAndDestroy(0.4f);
            Destroy(gameObject);
            yield break;
        }

        Prog(0.18f, "Контролери GNC…", "GNC controllers…");
        yield return null;
        EnsureControllers(rocket);

        IdealLandingPresets.ApplyDefaultControllerTuning(
            rocket,
            rocket.GetComponent<FuzzyLandingController>(),
            rocket.GetComponent<NeuralController>(),
            rocket.GetComponent<HybridController>());

        Prog(0.35f, "Модель ракетоносія…", "Building rocket…");
        yield return null;
        RocketVisualBuilder.Build(rocket);

        Prog(0.55f, "Місяць і посадковий майданчик…", "Moon & landing pad…");
        yield return null;
        EnvironmentBuilder.Build();

        Prog(0.72f, "Стан симуляції…", "Simulation state…");
        yield return null;

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
        yield return null;
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
        yield return null;
        // MissionControlUI auto-creates via its own RuntimeInitialize — give it a frame
        yield return null;

        Prog(1f, "Готово", "Ready");
        yield return null;

        splash?.FadeOutAndDestroy(0.6f);
        Destroy(gameObject);
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
