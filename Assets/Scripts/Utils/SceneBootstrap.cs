using UnityEngine;

/// <summary>
/// Після завантаження сцени: візуал ракети, середовище, камера, Hybrid, UI.
/// </summary>
public static class SceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBootstrap()
    {
        var rocket = Object.FindFirstObjectByType<RocketPhysics>();
        if (rocket == null) return;

        // Controllers
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

        // Visuals
        RocketVisualBuilder.Build(rocket);
        EnvironmentBuilder.Build();

        // Simulation manager
        if (Object.FindFirstObjectByType<SimulationManager>() == null)
        {
            var smGo = new GameObject("SimulationManager");
            var sm = smGo.AddComponent<SimulationManager>();
            sm.rocketPhysics = rocket;
        }

        // Camera
        SetupCamera(rocket.transform);

        // Trajectory visualizer
        if (Object.FindFirstObjectByType<TrajectoryVisualizer>() == null)
        {
            var tv = new GameObject("TrajectoryVisualizer");
            var vis = tv.AddComponent<TrajectoryVisualizer>();
            vis.rocketPhysics = rocket;
        }

        // Disable old MissionControlTheme full-screen dim on canvas
        foreach (var theme in Object.FindObjectsByType<MissionControlTheme>(FindObjectsSortMode.None))
            theme.styleOnAwake = false;
    }

    static void SetupCamera(Transform rocket)
    {
        var cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        try { cam.tag = "MainCamera"; } catch { /* tag missing */ }

        cam.farClipPlane = 12000f;
        cam.fieldOfView = 48f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.42f, 0.6f, 0.82f);
        cam.transform.position = new Vector3(80f, 2580f, -140f);

        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = rocket;
        follow.baseOffset = new Vector3(55f, 30f, -100f);

        // Start near rocket initial altitude if params known
        var rp = rocket.GetComponent<RocketPhysics>();
        if (rp != null && rp.parameters != null)
        {
            Vector3 p = rp.parameters.startPosition;
            cam.transform.position = p + follow.baseOffset;
            cam.transform.LookAt(p + Vector3.up * 15f);
        }
    }
}
