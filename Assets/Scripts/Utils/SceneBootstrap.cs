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

/// <summary>Важкий bootstrap рознесено по кадрах, щоб splash міг анімуватись.</summary>
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

        // Один кадр, щоб splash встиг намалюватись до важкої роботи
        yield return null;

        // Легкі defaults
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        BorderlessWindow.ApplyBorderlessChrome();
        if (SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 9000)
            QualitySettings.SetQualityLevel(Mathf.Min(QualitySettings.GetQualityLevel(), 1), true);
        // Не обмежувати shadowDistance тут — CameraFollow.FitShadows масштабує його з zoom.
        // Фіксований ліміт 400 м скидав далекі orbit-ракурси в розмиті far-cascades.

        Prog(0.08f, "Пошук ракети…", "Finding rocket…");
        yield return null;

        var rocket = Object.FindAnyObjectByType<RocketPhysics>();
        if (rocket == null)
        {
            Prog(1f, "Сцена без RocketPhysics", "No RocketPhysics in scene");
            splash?.FadeOutAndDestroy(0.25f);
            Destroy(gameObject);
            yield break;
        }

        Prog(0.18f, "Контролери GNC…", "GNC controllers…");
        yield return null;
        EnsureControllers(rocket);

        Prog(0.30f, "Модель ракетоносія…", "Building rocket…");
        yield return null;
        yield return RocketVisualBuilder.BuildRoutine(rocket);
        Prog(0.45f, "Модель ракетоносія ✓", "Rocket mesh ✓");
        yield return null;

        // Довгий крок LZ — кілька проміжних Prog, щоб бар не «застигав/зникав» на ~52%
        Prog(0.50f, "Земний аеродром / LZ…", "Earth airfield / LZ…");
        yield return null;
        Prog(0.55f, "Рельєф і текстури…", "Terrain & textures…");
        yield return null;
        yield return EnvironmentBuilder.BuildRoutine();
        Prog(0.78f, "Середовище ✓", "Environment ✓");
        yield return null;

        Prog(0.84f, "Стан симуляції…", "Simulation state…");
        yield return null;

        // Idle: 1-й ступінь у ПУ посадки, очікує запуску
        rocket.PrepareMode(rocket.controlMode);

        if (Object.FindAnyObjectByType<SimulationManager>() == null)
        {
            var smGo = new GameObject("SimulationManager");
            var sm = smGo.AddComponent<SimulationManager>();
            sm.rocketPhysics = rocket;
        }

        Prog(0.88f, "Камера…", "Camera…");
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

        Prog(0.94f, "Mission Control HUD…", "Mission Control HUD…");
        yield return null;

        Prog(1f, "Готово", "Ready");
        // Дати splash 2–3 кадри дотягнути бар до 100%
        for (int i = 0; i < 4; i++)
            yield return null;

        splash?.FadeOutAndDestroy(0.45f);
        Destroy(gameObject);
    }

    static void EnsureControllers(RocketPhysics rocket)
    {
        if (rocket.GetComponent<DataLogger>() == null)
            rocket.gameObject.AddComponent<DataLogger>();
        // Навчені ваги MLP (BestWeights_Neural.json / BestWeights_Hybrid.json)
        rocket.ReloadWeights();
        // Режим за замовчуванням — Hybrid (тема МКР), якщо UI не зберіг інший
        if (UserSettings.ControlMode == 3)
            rocket.controlMode = ControlMode.Hybrid;
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

        cam.farClipPlane = 18000f;
        cam.fieldOfView = 48f;
        cam.nearClipPlane = 0.3f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        EnvironmentTextures.EnsureLoaded();
        cam.backgroundColor = Color.Lerp(EnvironmentTextures.FogColor, EnvironmentTextures.SkyZenith, 0.42f);

        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.rocket = rocket;
        follow.trajectory = Object.FindAnyObjectByType<TrajectoryVisualizer>();
        follow.SnapNow();
    }
}
