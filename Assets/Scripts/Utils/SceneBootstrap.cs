using UnityEngine;

/// <summary>
/// Автоматична ініціалізація сцени після Load (RuntimeInitializeOnLoad).
/// Гарантує наявність контролерів GNC, 3D-візуалу, середовища, камери
/// та TrajectoryVisualizer — навіть якщо сцена «порожня».
/// </summary>
public static class SceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBootstrap()
    {
        var rocket = Object.FindFirstObjectByType<RocketPhysics>();
        if (rocket == null) return;

        EnsureControllers(rocket);

        RocketVisualBuilder.Build(rocket);
        EnvironmentBuilder.Build();

        // Hold until user starts
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

        if (Object.FindFirstObjectByType<SimulationManager>() == null)
        {
            var smGo = new GameObject("SimulationManager");
            var sm = smGo.AddComponent<SimulationManager>();
            sm.rocketPhysics = rocket;
        }

        SetupCamera(rocket);

        if (Object.FindFirstObjectByType<TrajectoryVisualizer>() == null)
        {
            var tv = new GameObject("TrajectoryVisualizer");
            var vis = tv.AddComponent<TrajectoryVisualizer>();
            vis.rocketPhysics = rocket;
            vis.baseLineWidth = 6f;
        }

        foreach (var theme in Object.FindObjectsByType<MissionControlTheme>(FindObjectsSortMode.None))
            theme.styleOnAwake = false;
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
        var cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        try { cam.tag = "MainCamera"; } catch { /* tag missing */ }

        cam.farClipPlane = 16000f;
        cam.fieldOfView = 48f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.01f, 0.012f);
        cam.allowHDR = true;

        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.rocket = rocket;
        follow.target = rocket.transform;
        follow.bodyLookHeight = 18f;
        follow.defaultDistance = 105f;
        follow.defaultYaw = 28f;
        follow.defaultPitch = 22f;
        follow.SnapNow();
    }
}
