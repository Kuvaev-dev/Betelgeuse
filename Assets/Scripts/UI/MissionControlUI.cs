using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Центр керування місією: телеметрія, режими посадки, експерименти.
/// Мова інтерфейсу — зрозуміла для рядового користувача (UA).
/// </summary>
[DefaultExecutionOrder(-50)]
public class MissionControlUI : MonoBehaviour
{
    public static MissionControlUI Instance { get; private set; }

    RocketPhysics rocket;
    SimulationManager sim;

    TMP_Text txtAlt, txtVel, txtThr, txtTilt, txtFuel, txtMiss, txtMode, txtStatus, txtTime, txtScore;
    TMP_Text txtPid, txtFuzzy, txtNeural, txtHybrid, txtWinner, txtInfo, txtHint;
    TMP_Text txtWindVal, txtTestsVal;
    TMP_Text txtResultTitle, txtResultBody, txtProgress, txtCamMode;

    Slider windSlider, testsSlider, timeScaleSlider;
    Toggle noiseToggle, trainToggle;
    Image thrBarFill, fuelBarFill, tiltBarFill, statusDot, progressFill, resultPanelBg;
    GameObject resultRoot, progressRoot;
    TelemetryGraph graphAlt, graphVel, graphThr;
    readonly List<Button> modeButtons = new();
    readonly List<Image> modeButtonImages = new();

    float sampleTimer;
    bool built;
    bool batchMode;
    bool overviewCam;
    bool resultShown;

    // Space mission palette
    static readonly Color C_Panel = new(0.03f, 0.04f, 0.09f, 0.92f);
    static readonly Color C_PanelSoft = new(0.04f, 0.055f, 0.12f, 0.88f);
    static readonly Color C_Edge = new(0.2f, 0.45f, 0.75f, 0.55f);
    static readonly Color C_Cyan = new(0.35f, 0.85f, 1f, 1f);
    static readonly Color C_Amber = new(1f, 0.72f, 0.25f, 1f);
    static readonly Color C_Ok = new(0.35f, 0.95f, 0.55f, 1f);
    static readonly Color C_Alert = new(1f, 0.38f, 0.42f, 1f);
    static readonly Color C_Text = new(0.92f, 0.95f, 1f, 1f);
    static readonly Color C_Muted = new(0.55f, 0.62f, 0.75f, 1f);
    static readonly Color C_Btn = new(0.07f, 0.12f, 0.22f, 0.98f);
    static readonly Color C_BtnActive = new(0.08f, 0.32f, 0.42f, 1f);
    static readonly Color C_BtnHover = new(0.12f, 0.22f, 0.38f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (FindFirstObjectByType<MissionControlUI>() != null) return;
        if (FindFirstObjectByType<RocketPhysics>() == null) return;
        new GameObject("MissionControlUI").AddComponent<MissionControlUI>();
    }

    void Awake()
    {
        Instance = this;
        rocket = FindFirstObjectByType<RocketPhysics>();
        sim = FindFirstObjectByType<SimulationManager>();
    }

    void Start()
    {
        HideLegacyUI();
        Build();
        WireLegacyDashboard();
        built = true;
    }

    void HideLegacyUI()
    {
        string[] hideNames =
        {
            "TelemetryHUD", "Experiment Dashboard", "HeightText", "VelocityText",
            "ThrustText", "AngleText", "ControlModeText", "Pid Success", "Fuzzy Success",
            "Comparison", "Run PID", "Run Fuzzy", "Run Neural", "Run Full Test",
            "Reset", "Noise", "Wind Slider", "Tests Count", "Background"
        };

        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t == null) continue;
            var canvas = t.GetComponentInParent<Canvas>();
            if (canvas == null) continue;
            if (canvas.transform.IsChildOf(transform) || canvas.transform == transform) continue;

            foreach (var n in hideNames)
            {
                if (t.name == n)
                {
                    t.gameObject.SetActive(false);
                    break;
                }
            }
        }

        foreach (var h in FindObjectsByType<TelemetryHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            h.enabled = false;
    }

    void WireLegacyDashboard()
    {
        var dash = FindFirstObjectByType<ExperimentDashboard>(FindObjectsInactive.Include);
        if (dash == null) return;
        dash.enabled = true;
        dash.pidStatsText = txtPid;
        dash.fuzzyStatsText = txtFuzzy;
        dash.neuralStatsText = txtNeural;
        dash.hybridStatsText = txtHybrid;
        dash.winnerText = txtWinner;
        if (sim != null) dash.simulationManager = sim;
        if (rocket != null) dash.rocketPhysics = rocket;
    }

    void Build()
    {
        var canvasGo = new GameObject("MC_Canvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        BuildTopBar(canvasGo.transform);
        BuildLeftPanel(canvasGo.transform);
        BuildRightPanel(canvasGo.transform);
        BuildBottomBar(canvasGo.transform);
        BuildCenterHint(canvasGo.transform);
        BuildResultOverlay(canvasGo.transform);
        BuildProgressBar(canvasGo.transform);
    }

    void BuildTopBar(Transform parent)
    {
        var bar = CreatePanel("TopBar", parent, C_Panel);
        DockTop(bar.GetComponent<RectTransform>(), 70f);
        Outline(bar, 1.5f);

        // Accent line under top bar
        var accent = CreatePanel("TopAccent", bar.transform, C_Cyan * 0.7f);
        var art = accent.GetComponent<RectTransform>();
        art.anchorMin = new Vector2(0, 0);
        art.anchorMax = new Vector2(1, 0);
        art.pivot = new Vector2(0.5f, 0);
        art.anchoredPosition = Vector2.zero;
        art.sizeDelta = new Vector2(0, 2);

        var title = CreateText(bar.transform, "BETELGEUSE", 22, C_Cyan, FontStyles.Bold);
        Pin(title.rectTransform, 0, 0.55f, 0, 0.55f, 24, 4, 200, 30);

        var subtitle = CreateText(bar.transform, "Автономна посадка ракетоносія", 13, C_Muted);
        Pin(subtitle.rectTransform, 0, 0.28f, 0, 0.28f, 24, 0, 320, 22);

        txtMode = CreateText(bar.transform, "Алгоритм: —", 16, C_Amber, FontStyles.Bold);
        Pin(txtMode.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 420, 34);
        txtMode.alignment = TextAlignmentOptions.Center;

        txtTime = CreateText(bar.transform, "Час  0.0 с", 15, C_Text);
        Pin(txtTime.rectTransform, 1, 0.5f, 1, 0.5f, -300, 0, 130, 30);

        // Status with dot
        var statusRow = CreatePanel("StatusRow", bar.transform, new Color(0, 0, 0, 0));
        statusRow.GetComponent<Image>().raycastTarget = false;
        Pin(statusRow.GetComponent<RectTransform>(), 1, 0.5f, 1, 0.5f, -16, 0, 200, 36);

        var dot = CreatePanel("Dot", statusRow.transform, C_Muted);
        var drt = dot.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0, 0.5f);
        drt.pivot = new Vector2(0, 0.5f);
        drt.anchoredPosition = new Vector2(0, 0);
        drt.sizeDelta = new Vector2(10, 10);
        statusDot = dot.GetComponent<Image>();

        txtStatus = CreateText(statusRow.transform, "ГОТОВО", 14, C_Muted, FontStyles.Bold);
        var srt = txtStatus.rectTransform;
        srt.anchorMin = srt.anchorMax = new Vector2(0, 0.5f);
        srt.pivot = new Vector2(0, 0.5f);
        srt.anchoredPosition = new Vector2(18, 0);
        srt.sizeDelta = new Vector2(170, 28);
    }

    void BuildLeftPanel(Transform parent)
    {
        var panel = CreatePanel("LeftPanel", parent, C_Panel);
        DockLeft(panel.GetComponent<RectTransform>(), 14, 78, 58, 330);
        Outline(panel);

        float y = -16f;
        Header(panel.transform, "ТЕЛЕМЕТРІЯ ПОЛЬОТУ", ref y);

        txtAlt = Metric(panel.transform, "Висота", "м", ref y);
        txtVel = Metric(panel.transform, "Швидкість вниз", "м/с", ref y);
        txtThr = Metric(panel.transform, "Тяга двигуна", "кН", ref y);
        thrBarFill = MakeBar(panel.transform, ref y, C_Cyan);
        txtTilt = Metric(panel.transform, "Нахил корпусу", "°", ref y);
        tiltBarFill = MakeBar(panel.transform, ref y, C_Amber);
        txtFuel = Metric(panel.transform, "Паливо", "кг", ref y);
        fuelBarFill = MakeBar(panel.transform, ref y, C_Ok);
        txtMiss = Metric(panel.transform, "Відхилення від pad", "м", ref y);
        txtScore = Metric(panel.transform, "Оцінка посадки", "/100", ref y);

        y -= 8f;
        Header(panel.transform, "ГРАФІКИ В РЕАЛЬНОМУ ЧАСІ", ref y);
        graphAlt = MakeGraph(panel.transform, "Висота, м", C_Cyan, ref y);
        graphVel = MakeGraph(panel.transform, "Швидкість, м/с", C_Amber, ref y);
        graphThr = MakeGraph(panel.transform, "Тяга, кН", new Color(0.45f, 0.95f, 0.55f), ref y);
    }

    void BuildRightPanel(Transform parent)
    {
        var panel = CreatePanel("RightPanel", parent, C_Panel);
        DockRight(panel.GetComponent<RectTransform>(), 14, 78, 58, 360);
        Outline(panel);

        // Scrollable content for short screens
        var viewport = CreatePanel("Viewport", panel.transform, new Color(0, 0, 0, 0));
        viewport.GetComponent<Image>().raycastTarget = false;
        var vrt = viewport.GetComponent<RectTransform>();
        StretchFull(vrt, 0, 0, 0, 0);
        viewport.AddComponent<RectMask2D>();

        var content = CreatePanel("Content", viewport.transform, new Color(0, 0, 0, 0));
        content.GetComponent<Image>().raycastTarget = false;
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1);
        crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 1);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(0, 980);

        var scroll = panel.AddComponent<ScrollRect>();
        scroll.viewport = vrt;
        scroll.content = crt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        float y = -14f;
        var how = CreateText(content.transform,
            "① Алгоритм  →  ② Запуск  →  ③ Дивіться результат",
            12, C_Cyan, FontStyles.Bold);
        PinTL(how.rectTransform, 14, y, 330, 20);
        y -= 26f;

        Header(content.transform, "КРОК 1 · ОБЕРІТЬ АЛГОРИТМ", ref y);

        modeButtons.Clear();
        modeButtonImages.Clear();
        modeButtons.Add(ModeButton(content.transform, "A  Класичний PID",
            "Простий еталон", RocketPhysics.ControlMode.PID, ref y));
        modeButtons.Add(ModeButton(content.transform, "B  Нечітка логіка",
            "Sugeno — правила «як пілот»", RocketPhysics.ControlMode.Fuzzy, ref y));
        modeButtons.Add(ModeButton(content.transform, "C  Нейромережа",
            "Машинне навчання", RocketPhysics.ControlMode.Neural, ref y));
        modeButtons.Add(ModeButton(content.transform, "D  Гібрид ★ рекомендовано",
            "Нечітка + нейромережа", RocketPhysics.ControlMode.Hybrid, ref y));

        y -= 4f;
        Header(content.transform, "КРОК 2 · КЕРУВАННЯ", ref y);

        ActionButton(content.transform, "▶  ЗАПУСТИТИ ПОСАДКУ", new Color(0.05f, 0.48f, 0.36f), ref y, OnStartLanding);
        ActionButton(content.transform, "⏹  СТОП / ПАУЗА", new Color(0.45f, 0.12f, 0.15f), ref y, OnStop);
        ActionButton(content.transform, "👁  ВСЯ ТРАЄКТОРІЯ", new Color(0.12f, 0.22f, 0.4f), ref y, OnToggleTrajectoryView);
        ActionButton(content.transform, "⚡  ПОРІВНЯТИ ВСІ АЛГОРИТМИ", new Color(0.45f, 0.3f, 0.06f), ref y, OnStartCompare);
        ActionButton(content.transform, "✖  СКАСУВАТИ ПОРІВНЯННЯ", new Color(0.3f, 0.15f, 0.1f), ref y, OnCancelCompare);

        txtCamMode = CreateText(content.transform, "Камера: слідкування за ракетою", 11, C_Muted);
        PinTL(txtCamMode.rectTransform, 16, y, 320, 18);
        y -= 22f;

        y -= 2f;
        Header(content.transform, "КРОК 3 · УМОВИ ТЕСТУ (опційно)", ref y);
        txtTestsVal = SliderLine(content.transform, "Запусків на алгоритм", 5, 40, 15, ref y, out testsSlider);
        txtWindVal = SliderLine(content.transform, "Сила вітру", 0, 25, 10, ref y, out windSlider);
        SliderLine(content.transform, "Прискорення часу (тест)", 1, 40, 20, ref y, out timeScaleSlider);
        noiseToggle = ToggleLine(content.transform, "Випадкові збурення", true, ref y);
        trainToggle = ToggleLine(content.transform, "Навчати нейромережу", true, ref y);

        y -= 4f;
        Header(content.transform, "РЕЗУЛЬТАТИ ПОРІВНЯННЯ (% успіху)", ref y);
        txtPid = Stat(content.transform, "A  PID", ref y);
        txtFuzzy = Stat(content.transform, "B  Нечітка", ref y);
        txtNeural = Stat(content.transform, "C  Нейромережа", ref y);
        txtHybrid = Stat(content.transform, "D  Гібрид", ref y);

        y -= 2f;
        txtWinner = CreateText(content.transform, "Переможець: ще не визначено", 13, C_Ok, FontStyles.Bold);
        PinTL(txtWinner.rectTransform, 14, y, 330, 22);
        y -= 26f;

        txtInfo = CreateText(content.transform,
            "Порада: D → ЗАПУСТИТИ. Після посадки з’явиться великий банер УСПІХ / НЕВДАЧА. «ВСЯ ТРАЄКТОРІЯ» показує весь шлях.",
            12, C_Muted);
        txtInfo.enableWordWrapping = true;
        txtInfo.alignment = TextAlignmentOptions.TopLeft;
        PinTL(txtInfo.rectTransform, 14, y, 330, 80);

        crt.sizeDelta = new Vector2(0, Mathf.Max(1100f, -y + 40f));
    }

    // ─── User actions ───

    void OnStartLanding()
    {
        if (rocket == null) return;
        if (sim != null && sim.IsExperimentRunning)
        {
            NotifyInfo("Спочатку скасуйте авто-тест (✖).");
            return;
        }
        HideLandingResult();
        ClearGraphs();
        overviewCam = false;
        FindFirstObjectByType<CameraFollow>()?.SetMode(CameraFollow.ViewMode.Follow);
        if (txtCamMode) txtCamMode.text = "Камера: слідкування за ракетою";
        rocket.ResetSimulation();
        if (txtInfo) txtInfo.text = $"▶ Посадка: {FriendlyMode(rocket.controlMode)}. Дивіться центр екрана.";
        if (txtHint) txtHint.gameObject.SetActive(false);
    }

    void OnStop()
    {
        if (sim != null && sim.IsExperimentRunning)
        {
            sim.CancelExperiment();
            NotifyInfo("⏹ Авто-тест зупиняється…");
            return;
        }
        if (rocket == null) return;
        rocket.StopSimulation(keepPosition: true);
        HideLandingResult();
        NotifyInfo("⏹ Політ зупинено. Оберіть алгоритм і натисніть ЗАПУСТИТИ знову.\n" +
                   "Щоб вийти з Play Mode у Unity: кнопка ■ зверху (або Ctrl+P).");
        if (txtStatus)
        {
            txtStatus.text = "ЗУПИНЕНО";
            txtStatus.color = C_Amber;
        }
    }

    void OnToggleTrajectoryView()
    {
        var cam = FindFirstObjectByType<CameraFollow>();
        if (cam == null) return;
        overviewCam = !overviewCam;
        cam.SetMode(overviewCam ? CameraFollow.ViewMode.Overview : CameraFollow.ViewMode.Follow);
        if (txtCamMode)
            txtCamMode.text = overviewCam
                ? "Камера: ОГЛЯД УСІЄЇ ТРАЄКТОРІЇ"
                : "Камера: слідкування за ракетою";
        NotifyInfo(overviewCam
            ? "👁 Огляд: видно весь шлях від старту до pad (блакитна/зелена/червона лінія)."
            : "Камера знову за ракетою.");
    }

    void OnStartCompare()
    {
        if (sim == null) { NotifyInfo("Помилка: SimulationManager не знайдено"); return; }
        if (sim.IsExperimentRunning) { NotifyInfo("Тест уже йде…"); return; }
        HideLandingResult();
        ApplySettings();
        sim.RequestFullExperiment();
        NotifyInfo("⚡ Авто-тест: алгоритми змінюються САМІ (PID→Fuzzy→NN→Hybrid) — це нормально. Прогрес зверху.");
        if (txtHint) txtHint.gameObject.SetActive(false);
    }

    void OnCancelCompare()
    {
        if (sim == null) return;
        sim.CancelExperiment();
        NotifyInfo("✖ Скасування авто-тесту…");
    }

    public void NotifyInfo(string msg)
    {
        if (txtInfo) txtInfo.text = msg;
    }

    public void SetBatchMode(bool on)
    {
        batchMode = on;
        if (on) HideLandingResult();
        if (progressRoot != null) progressRoot.SetActive(on);
    }

    public void SetExperimentProgress(string label, float p01)
    {
        if (txtProgress) txtProgress.text = label;
        if (progressFill != null)
            progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(p01), 1f);
        if (txtMode) txtMode.text = label;
    }

    public void ShowLandingResult(LandingMetrics m)
    {
        if (batchMode || m == null || resultRoot == null) return;
        resultShown = true;
        resultRoot.SetActive(true);

        float maxV = rocket?.parameters != null ? rocket.parameters.maxTouchdownVelocity : 3.5f;
        float maxA = rocket?.parameters != null ? rocket.parameters.maxLandingAngle : 7f;
        float maxM = rocket?.parameters != null ? rocket.parameters.maxHorizontalMiss : 25f;
        float maxH = rocket?.parameters != null ? rocket.parameters.maxHorizontalSpeed : 5f;

        bool ok = m.isSuccessfulLanding;
        if (txtResultTitle)
        {
            txtResultTitle.text = ok ? "✓  ПОСАДКА УСПІШНА" : "✗  ПОСАДКА НЕВДАЛА";
            txtResultTitle.color = ok ? C_Ok : C_Alert;
        }
        if (txtResultBody)
        {
            txtResultBody.text = m.BuildUserSummary(maxV, maxA, maxM, maxH)
                + "\n\n«ВСЯ ТРАЄКТОРІЯ» — побачити шлях  ·  ЗАПУСТИТИ — ще раз";
            txtResultBody.color = C_Text;
        }
        if (resultPanelBg)
            resultPanelBg.color = ok
                ? new Color(0.04f, 0.18f, 0.1f, 0.94f)
                : new Color(0.2f, 0.05f, 0.07f, 0.94f);

        if (txtStatus)
        {
            txtStatus.text = ok ? "УСПІХ" : "НЕВДАЧА";
            txtStatus.color = ok ? C_Ok : C_Alert;
        }
        if (statusDot) statusDot.color = ok ? C_Ok : C_Alert;
        Write(txtScore, $"{m.SuccessScore:F0}", ok ? C_Ok : C_Alert);
    }

    public void HideLandingResult()
    {
        resultShown = false;
        if (resultRoot != null) resultRoot.SetActive(false);
    }

    void BuildResultOverlay(Transform parent)
    {
        resultRoot = CreatePanel("ResultOverlay", parent, new Color(0, 0, 0, 0.35f));
        var rt = resultRoot.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(360, 70);
        rt.offsetMax = new Vector2(-380, -90);
        resultRoot.GetComponent<Image>().raycastTarget = true;

        var card = CreatePanel("ResultCard", resultRoot.transform, new Color(0.04f, 0.18f, 0.1f, 0.94f));
        resultPanelBg = card.GetComponent<Image>();
        var crt = card.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(520, 320);
        Outline(card, 2f);

        txtResultTitle = CreateText(card.transform, "РЕЗУЛЬТАТ", 26, C_Ok, FontStyles.Bold);
        Pin(txtResultTitle.rectTransform, 0.5f, 1, 0.5f, 1, 0, -24, 480, 40);
        txtResultTitle.alignment = TextAlignmentOptions.Center;

        txtResultBody = CreateText(card.transform, "", 15, C_Text);
        txtResultBody.enableWordWrapping = true;
        txtResultBody.overflowMode = TextOverflowModes.Overflow;
        txtResultBody.alignment = TextAlignmentOptions.TopLeft;
        var brt = txtResultBody.rectTransform;
        brt.anchorMin = new Vector2(0, 0);
        brt.anchorMax = new Vector2(1, 1);
        brt.offsetMin = new Vector2(28, 70);
        brt.offsetMax = new Vector2(-28, -70);

        // Close button
        var closeGo = CreatePanel("CloseResult", card.transform, new Color(0.1f, 0.25f, 0.4f));
        var clrt = closeGo.GetComponent<RectTransform>();
        clrt.anchorMin = new Vector2(0.5f, 0);
        clrt.anchorMax = new Vector2(0.5f, 0);
        clrt.pivot = new Vector2(0.5f, 0);
        clrt.anchoredPosition = new Vector2(0, 16);
        clrt.sizeDelta = new Vector2(220, 40);
        var cbtn = closeGo.AddComponent<Button>();
        cbtn.targetGraphic = closeGo.GetComponent<Image>();
        var ctxt = CreateText(closeGo.transform, "ЗРОЗУМІЛО", 14, C_Text, FontStyles.Bold);
        StretchFull(ctxt.rectTransform, 4, 4, 4, 4);
        ctxt.alignment = TextAlignmentOptions.Center;
        cbtn.onClick.AddListener(HideLandingResult);

        // Secondary: show trajectory
        var trGo = CreatePanel("ShowTraj", card.transform, new Color(0.08f, 0.2f, 0.35f));
        var trt = trGo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 0);
        trt.anchorMax = new Vector2(0.5f, 0);
        trt.pivot = new Vector2(0.5f, 0);
        trt.anchoredPosition = new Vector2(0, 62);
        trt.sizeDelta = new Vector2(280, 36);
        var tbtn = trGo.AddComponent<Button>();
        tbtn.targetGraphic = trGo.GetComponent<Image>();
        var ttxt = CreateText(trGo.transform, "Показати всю траєкторію", 13, C_Text, FontStyles.Bold);
        StretchFull(ttxt.rectTransform, 4, 4, 4, 4);
        ttxt.alignment = TextAlignmentOptions.Center;
        tbtn.onClick.AddListener(() =>
        {
            HideLandingResult();
            if (!overviewCam) OnToggleTrajectoryView();
        });

        resultRoot.SetActive(false);
    }

    void BuildProgressBar(Transform parent)
    {
        progressRoot = CreatePanel("ProgressRoot", parent, new Color(0.05f, 0.08f, 0.15f, 0.92f));
        var rt = progressRoot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -78);
        rt.sizeDelta = new Vector2(560, 48);
        Outline(progressRoot);

        txtProgress = CreateText(progressRoot.transform, "Авто-тест…", 13, C_Amber, FontStyles.Bold);
        Pin(txtProgress.rectTransform, 0.5f, 1, 0.5f, 1, 0, -6, 540, 22);
        txtProgress.alignment = TextAlignmentOptions.Center;

        var bg = CreatePanel("PBg", progressRoot.transform, new Color(0.02f, 0.03f, 0.06f));
        var bgrt = bg.GetComponent<RectTransform>();
        bgrt.anchorMin = new Vector2(0, 0);
        bgrt.anchorMax = new Vector2(1, 0);
        bgrt.pivot = new Vector2(0.5f, 0);
        bgrt.anchoredPosition = new Vector2(0, 8);
        bgrt.offsetMin = new Vector2(16, 8);
        bgrt.offsetMax = new Vector2(-16, 18);

        var fill = CreatePanel("PFill", bg.transform, C_Cyan);
        var frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = new Vector2(0.01f, 1f);
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        progressFill = fill.GetComponent<Image>();

        progressRoot.SetActive(false);
    }

    void BuildBottomBar(Transform parent)
    {
        var bar = CreatePanel("BottomBar", parent, C_PanelSoft);
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.offsetMin = new Vector2(358, 8);
        rt.offsetMax = new Vector2(-378, 50);
        Outline(bar);

        var t = CreateText(bar.transform,
            "Успіх: швидкість < 3.5 м/с · нахил < 7° · промах < 25 м · бічна < 5  |  СТОП = зупинити політ  |  Unity ■ / Ctrl+P = вийти з Play",
            12, C_Muted);
        StretchFull(t.rectTransform, 10, 4, 10, 4);
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = true;
    }

    void BuildCenterHint(Transform parent)
    {
        var hint = CreatePanel("CenterHint", parent, new Color(0.02f, 0.04f, 0.1f, 0.6f));
        var rt = hint.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 64);
        rt.sizeDelta = new Vector2(560, 48);
        Outline(hint);

        txtHint = CreateText(hint.transform,
            "① Справа: алгоритм (D)   ② «ЗАПУСТИТИ ПОСАДКУ»   ③ Банер УСПІХ/НЕВДАЧА",
            13, C_Text);
        StretchFull(txtHint.rectTransform, 12, 6, 12, 6);
        txtHint.alignment = TextAlignmentOptions.Center;
        txtHint.enableWordWrapping = true;
    }

    void Update()
    {
        if (!built || rocket == null) return;
        var s = rocket.state;

        float tilt = Vector3.Angle(s.rotation * Vector3.up, Vector3.up);
        float miss = new Vector2(s.position.x, s.position.z).magnitude;
        float thrPct = s.maxThrust > 1f ? s.currentThrust / s.maxThrust : 0f;
        float fuelPct = rocket.parameters != null && rocket.parameters.fuelMass > 1f
            ? s.currentFuelMass / rocket.parameters.fuelMass : 0f;

        Write(txtAlt, $"{s.position.y:F0}", s.position.y < 80f ? C_Amber : C_Text);
        float av = Mathf.Abs(s.velocity.y);
        Write(txtVel, $"{Mathf.Abs(s.velocity.y):F1}", av > 25f ? C_Alert : av > 8f ? C_Amber : C_Ok);
        Write(txtThr, $"{s.currentThrust / 1000f:F0}", C_Text);
        Write(txtTilt, $"{tilt:F1}", tilt > 7f ? C_Alert : tilt > 3f ? C_Amber : C_Text);
        Write(txtFuel, $"{s.currentFuelMass:F0}", fuelPct < 0.15f ? C_Alert : C_Text);
        Write(txtMiss, $"{miss:F1}", miss > 25f ? C_Alert : C_Text);

        SetBar(thrBarFill, thrPct, C_Cyan);
        SetBar(fuelBarFill, fuelPct, C_Ok);
        SetBar(tiltBarFill, Mathf.Clamp01(tilt / 15f), tilt > 7f ? C_Alert : C_Amber);

        bool exp = sim != null && sim.IsExperimentRunning;
        if (txtMode && !exp)
            txtMode.text = $"Алгоритм:  {FriendlyMode(rocket.controlMode)}";
        if (txtTime) txtTime.text = $"Час  {s.time:F1} с";

        if (txtStatus && !resultShown)
        {
            if (exp)
            {
                txtStatus.text = "АВТО-ТЕСТ";
                txtStatus.color = C_Amber;
                if (statusDot) statusDot.color = C_Amber;
            }
            else if (s.simulationFinished && rocket.simulationArmed == false && rocket.metrics != null
                     && (rocket.metrics.totalFlightTime > 0.1f || rocket.metrics.isSuccessfulLanding || rocket.metrics.timedOut))
            {
                // stopped mid-flight without finish metrics — handled by OnStop
            }
            else if (s.simulationFinished && rocket.metrics != null && rocket.metrics.totalFlightTime > 0.05f)
            {
                bool ok = rocket.metrics.isSuccessfulLanding;
                txtStatus.text = ok ? "УСПІХ" : "НЕВДАЧА";
                txtStatus.color = ok ? C_Ok : C_Alert;
                if (statusDot) statusDot.color = ok ? C_Ok : C_Alert;
                Write(txtScore, $"{rocket.metrics.SuccessScore:F0}", ok ? C_Ok : C_Alert);
            }
            else if (rocket.simulationArmed && s.time > 0.05f)
            {
                txtStatus.text = "СПУСК";
                txtStatus.color = C_Cyan;
                if (statusDot) statusDot.color = C_Cyan;
            }
            else if (rocket.simulationArmed)
            {
                txtStatus.text = "СТАРТ";
                txtStatus.color = C_Amber;
                if (statusDot) statusDot.color = C_Amber;
            }
            else
            {
                txtStatus.text = "ОЧІКУВАННЯ";
                txtStatus.color = C_Muted;
                if (statusDot) statusDot.color = C_Muted;
            }
        }

        // Highlight selected mode; dim others during experiment
        for (int i = 0; i < modeButtons.Count; i++)
        {
            var b = modeButtons[i];
            if (b == null) continue;
            b.interactable = !exp;
            bool active = b.gameObject.name == "Mode_" + rocket.controlMode;
            if (i < modeButtonImages.Count && modeButtonImages[i] != null)
            {
                if (exp)
                    modeButtonImages[i].color = active ? C_Amber * 0.7f : C_Btn * 0.6f;
                else
                    modeButtonImages[i].color = active ? C_BtnActive : C_Btn;
            }
        }

        sampleTimer += Time.unscaledDeltaTime;
        if (sampleTimer >= 0.07f && !s.simulationFinished && s.time > 0f)
        {
            sampleTimer = 0f;
            graphAlt?.Push(s.position.y);
            graphVel?.Push(s.velocity.y);
            graphThr?.Push(s.currentThrust / 1000f);
        }

        if (txtWindVal && windSlider) txtWindVal.text = $"{windSlider.value:F0}";
        if (txtTestsVal && testsSlider) txtTestsVal.text = $"{testsSlider.value:F0}";
    }

    public void UpdateStatistics(float pid, float fuzzy, float neural, float hybrid = -1f)
    {
        Write(txtPid, $"{pid:F1} %", C_Text);
        Write(txtFuzzy, $"{fuzzy:F1} %", C_Text);
        Write(txtNeural, $"{neural:F1} %", C_Text);
        if (hybrid >= 0f) Write(txtHybrid, $"{hybrid:F1} %", C_Text);

        string winner = "PID";
        float max = pid;
        if (fuzzy >= max) { max = fuzzy; winner = "Нечітка логіка"; }
        if (neural >= max) { max = neural; winner = "Нейромережа"; }
        if (hybrid > max) { max = hybrid; winner = "Гібрид Neuro-Fuzzy"; }
        if (txtWinner) { txtWinner.text = $"Переможець: {winner}  ({max:F1}%)"; txtWinner.color = C_Ok; }
        if (txtInfo) txtInfo.text = $"✓ Авто-тест завершено.\nПереможець: {winner} ({max:F1}% успішних).\nАлгоритм користувача відновлено.";
    }

    static string FriendlyMode(RocketPhysics.ControlMode m) => m switch
    {
        RocketPhysics.ControlMode.Fuzzy => "Нечітка логіка (Sugeno)",
        RocketPhysics.ControlMode.Neural => "Нейромережа (ES)",
        RocketPhysics.ControlMode.Hybrid => "Гібрид Neuro-Fuzzy",
        _ => "Класичний PID"
    };

    void ApplySettings()
    {
        if (sim == null) return;
        if (testsSlider) sim.testsPerAlgorithm = Mathf.RoundToInt(testsSlider.value);
        if (windSlider) sim.windStrength = windSlider.value;
        if (noiseToggle) sim.enableNoise = noiseToggle.isOn;
        if (timeScaleSlider) sim.experimentTimeScale = timeScaleSlider.value;
        if (trainToggle && rocket?.neuralController != null)
            rocket.neuralController.enableTraining = trainToggle.isOn;
    }

    void ClearGraphs()
    {
        graphAlt?.Clear();
        graphVel?.Clear();
        graphThr?.Clear();
    }

    // ═══════════════ builders ═══════════════

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var t = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (t != null) es.AddComponent(t);
        else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    static GameObject CreatePanel(string name, Transform parent, Color col)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = col;
        img.raycastTarget = true;
        return go;
    }

    static void Outline(GameObject go, float dist = 1.2f)
    {
        var o = go.AddComponent<UnityEngine.UI.Outline>();
        o.effectColor = C_Edge;
        o.effectDistance = new Vector2(dist, -dist);
    }

    static TMP_Text CreateText(Transform parent, string text, float size, Color col, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = col;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    void Header(Transform parent, string title, ref float y)
    {
        var t = CreateText(parent, title, 12, C_Cyan, FontStyles.Bold);
        PinTL(t.rectTransform, 14, y, 320, 20);
        y -= 20f;
        var line = CreatePanel("line", parent, C_Edge);
        PinTL(line.GetComponent<RectTransform>(), 14, y + 2, 322, 1);
        y -= 8f;
    }

    TMP_Text Metric(Transform parent, string key, string unit, ref float y)
    {
        var k = CreateText(parent, key, 12, C_Muted);
        PinTL(k.rectTransform, 16, y, 160, 18);
        var v = CreateText(parent, "—", 16, C_Text, FontStyles.Bold);
        v.alignment = TextAlignmentOptions.Right;
        PinTL(v.rectTransform, 160, y - 1, 100, 20);
        var u = CreateText(parent, unit, 11, C_Muted);
        u.alignment = TextAlignmentOptions.Left;
        PinTL(u.rectTransform, 264, y, 50, 18);
        y -= 24f;
        return v;
    }

    static void Write(TMP_Text t, string value, Color c)
    {
        if (t == null) return;
        t.text = value;
        t.color = c;
    }

    Image MakeBar(Transform parent, ref float y, Color fill)
    {
        var bg = CreatePanel("Bar", parent, new Color(0.02f, 0.03f, 0.06f, 1f));
        PinTL(bg.GetComponent<RectTransform>(), 16, y, 308, 8);
        var f = CreatePanel("Fill", bg.transform, fill);
        var frt = f.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = new Vector2(0.01f, 1f);
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        y -= 14f;
        return f.GetComponent<Image>();
    }

    static void SetBar(Image fill, float t, Color c)
    {
        if (fill == null) return;
        fill.color = c;
        fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(t), 1f);
    }

    TelemetryGraph MakeGraph(Transform parent, string title, Color line, ref float y)
    {
        var lab = CreateText(parent, title, 11, C_Muted);
        PinTL(lab.rectTransform, 16, y, 200, 16);
        y -= 16f;

        var go = new GameObject("Graph", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(parent, false);
        PinTL(go.GetComponent<RectTransform>(), 14, y, 312, 88);
        var raw = go.GetComponent<RawImage>();
        raw.color = Color.white;
        var g = go.AddComponent<TelemetryGraph>();
        g.lineColor = line;
        g.title = title;
        g.autoScale = true;
        y -= 96f;
        return g;
    }

    Button ModeButton(Transform parent, string title, string subtitle, RocketPhysics.ControlMode mode, ref float y)
    {
        var go = CreatePanel("Mode_" + mode, parent, C_Btn);
        PinTL(go.GetComponent<RectTransform>(), 14, y, 322, 44);
        var btn = go.AddComponent<Button>();
        var img = go.GetComponent<Image>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.85f, 0.95f, 1f);
        colors.pressedColor = C_Cyan;
        colors.fadeDuration = 0.08f;
        btn.colors = colors;
        modeButtonImages.Add(img);

        var txt = CreateText(go.transform, title, 14, C_Text, FontStyles.Bold);
        PinTL(txt.rectTransform, 12, -6, 298, 20);

        var sub = CreateText(go.transform, subtitle, 11, C_Muted);
        PinTL(sub.rectTransform, 12, -24, 298, 16);

        btn.onClick.AddListener(() =>
        {
            if (rocket == null) return;
            if (sim != null && sim.IsExperimentRunning)
            {
                NotifyInfo("Під час авто-тесту алгоритм змінюється автоматично. Натисніть ✖ щоб скасувати.");
                return;
            }
            HideLandingResult();
            ClearGraphs();
            overviewCam = false;
            FindFirstObjectByType<CameraFollow>()?.SetMode(CameraFollow.ViewMode.Follow);
            rocket.PrepareMode(mode);
            if (txtCamMode) txtCamMode.text = "Камера: слідкування за ракетою";
            if (txtInfo) txtInfo.text = $"✓ Обрано: {title}\nНатисніть зелену «ЗАПУСТИТИ ПОСАДКУ».";
            if (txtHint)
            {
                txtHint.gameObject.SetActive(true);
                txtHint.text = $"Обрано: {title}  →  «ЗАПУСТИТИ ПОСАДКУ»";
            }
        });
        y -= 50f;
        return btn;
    }

    void ActionButton(Transform parent, string label, Color col, ref float y, UnityEngine.Events.UnityAction action)
    {
        var go = CreatePanel("Action", parent, col);
        PinTL(go.GetComponent<RectTransform>(), 14, y, 322, 40);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        btn.colors = colors;
        var txt = CreateText(go.transform, label, 14, C_Text, FontStyles.Bold);
        StretchFull(txt.rectTransform, 4, 2, 4, 2);
        txt.alignment = TextAlignmentOptions.Center;
        btn.onClick.AddListener(action);
        y -= 46f;
    }

    TMP_Text SliderLine(Transform parent, string label, float min, float max, float val, ref float y, out Slider slider)
    {
        var k = CreateText(parent, label, 12, C_Muted);
        PinTL(k.rectTransform, 16, y, 180, 16);
        var v = CreateText(parent, val.ToString("F0"), 13, C_Cyan, FontStyles.Bold);
        v.alignment = TextAlignmentOptions.Right;
        PinTL(v.rectTransform, 250, y, 70, 16);
        y -= 18f;

        var sGo = CreatePanel("SliderBG", parent, new Color(0.02f, 0.03f, 0.07f, 1f));
        PinTL(sGo.GetComponent<RectTransform>(), 16, y, 308, 14);
        slider = sGo.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = true;
        slider.value = val;

        var fillArea = CreatePanel("Fill Area", sGo.transform, new Color(0, 0, 0, 0));
        StretchFull(fillArea.GetComponent<RectTransform>(), 2, 3, 2, 3);
        fillArea.GetComponent<Image>().raycastTarget = false;
        var fill = CreatePanel("Fill", fillArea.transform, C_Cyan * 0.55f);
        StretchFull(fill.GetComponent<RectTransform>(), 0, 0, 0, 0);

        var handle = CreatePanel("Handle", sGo.transform, C_Amber);
        var hrt = handle.GetComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(12, 14);
        hrt.anchorMin = hrt.anchorMax = new Vector2(0, 0.5f);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = hrt;
        slider.targetGraphic = handle.GetComponent<Image>();
        y -= 22f;
        return v;
    }

    Toggle ToggleLine(Transform parent, string label, bool on, ref float y)
    {
        var row = CreatePanel("ToggleRow", parent, new Color(0, 0, 0, 0));
        row.GetComponent<Image>().raycastTarget = false;
        PinTL(row.GetComponent<RectTransform>(), 16, y, 308, 24);

        var box = CreatePanel("Box", row.transform, C_Btn);
        var brt = box.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
        brt.pivot = new Vector2(0, 0.5f);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(20, 20);

        var check = CreatePanel("Check", box.transform, C_Cyan);
        StretchFull(check.GetComponent<RectTransform>(), 3, 3, 3, 3);

        var txt = CreateText(row.transform, label, 12, C_Text);
        var trt = txt.rectTransform;
        trt.anchorMin = trt.anchorMax = new Vector2(0, 0.5f);
        trt.pivot = new Vector2(0, 0.5f);
        trt.anchoredPosition = new Vector2(30, 0);
        trt.sizeDelta = new Vector2(270, 20);

        var toggle = row.AddComponent<Toggle>();
        toggle.targetGraphic = box.GetComponent<Image>();
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = on;
        y -= 26f;
        return toggle;
    }

    TMP_Text Stat(Transform parent, string name, ref float y)
    {
        var k = CreateText(parent, name, 13, C_Muted);
        PinTL(k.rectTransform, 16, y, 160, 20);
        var v = CreateText(parent, "—", 14, C_Text, FontStyles.Bold);
        v.alignment = TextAlignmentOptions.Right;
        PinTL(v.rectTransform, 170, y, 150, 20);
        y -= 22f;
        return v;
    }

    // ═══════════════ layout ═══════════════

    static void DockTop(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(0, -height);
        rt.offsetMax = Vector2.zero;
    }

    static void DockLeft(RectTransform rt, float left, float top, float bottom, float width)
    {
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(left + width, -top);
    }

    static void DockRight(RectTransform rt, float right, float top, float bottom, float width)
    {
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 0.5f);
        rt.offsetMin = new Vector2(-right - width, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static void Pin(RectTransform rt, float ax0, float ay0, float ax1, float ay1,
        float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(ax0, ay0);
        rt.anchorMax = new Vector2(ax1, ay1);
        rt.pivot = new Vector2((ax0 + ax1) * 0.5f, (ay0 + ay1) * 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    static void PinTL(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    static void StretchFull(RectTransform rt, float l, float b, float r, float t)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }
}
