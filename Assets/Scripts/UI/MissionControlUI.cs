using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
/// <summary>
/// Центр керування місією (runtime HUD).
/// Ліва панель: телеметрія, критерії soft-landing, висновок, live-графіки.
/// Права панель: алгоритм A–D, запуск, камера (Follow/Повна траєкторія/Manual),
/// умови Monte-Carlo, експорт CSV/JSON/Markdown.
/// Інтерфейс українською, орієнтований на демонстрацію дипломної роботи.
/// </summary>
[DefaultExecutionOrder(-50)]
public class MissionControlUI : MonoBehaviour
{
    public static MissionControlUI Instance { get; private set; }

    RocketPhysics rocket;
    SimulationManager sim;
    CameraFollow cameraFollow;
    DataLogger dataLogger;

    TMP_Text txtAlt, txtVel, txtThr, txtTilt, txtFuel, txtMiss, txtMode, txtStatus, txtTime, txtScore;
    TMP_Text txtHVel, txtMass, txtTwr, txtEta, txtAcc, txtRate;
    TMP_Text txtPeakVy, txtPeakTilt, txtMinH, txtDeltaStrip;
    TMP_Text txtCritV, txtCritA, txtCritM, txtCritH;
    TMP_Text txtInsight, txtFuelPct;
    TMP_Text txtPid, txtFuzzy, txtNeural, txtHybrid, txtWinner, txtInfo, txtHint;
    TMP_Text txtWindVal, txtTestsVal;
    TMP_Text txtResultTitle, txtResultBody, txtProgress, txtCamMode, txtCamHelp;
    TMP_Text txtTrajBtn, txtTitle, txtSubtitle, txtBottom, txtHow, txtGraphHint;
    TMP_Text txtHdrTelem, txtHdrLive, txtHdrCrit, txtHdrInsight, txtHdrGraphs;
    Button trajToggleBtn;

    // Metric label texts (for language refresh)
    readonly List<TMP_Text> metricLabels = new();

    Slider windSlider, testsSlider, timeScaleSlider;
    Toggle noiseToggle, trainToggle;
    Image thrBarFill, fuelBarFill, tiltBarFill, statusDot, progressFill, resultPanelBg;
    GameObject resultRoot, progressRoot, canvasRoot;
    GameObject leftPanelGo, rightPanelGo, bottomBarGo, centerHintGo, topBarGo, topMenuGo;
    bool panelsHidden;
    TMP_Text txtHideBtn;
    TelemetryGraph graphAlt, graphVel, graphThr;
    readonly List<Button> modeButtons = new();
    readonly List<Image> modeButtonImages = new();

    float sampleTimer;
    float prevVyForAcc;
    float prevAlt, prevAbsVy, prevTilt, prevThr;
    float smoothedAcc;
    float peakVy, peakTilt, minAltLive;
    bool flightPeaksActive;
    bool built;
    bool batchMode;
    bool overviewCam;
    bool resultShown;
    bool trajVisible = true;
    bool rebuilding;
    string lastExportPath;

    // Палітра з активної UiTheme (динамічна)
    static Color C_Panel => UiTypography.Panel;
    static Color C_PanelSoft => UiTypography.PanelSoft;
    static Color C_Edge => UiTypography.Edge;
    static Color C_Cyan => UiTypography.Accent;
    static Color C_Accent => UiTypography.Accent;
    static Color C_Amber => UiTypography.Amber;
    static Color C_Ok => UiTypography.Ok;
    static Color C_Alert => UiTypography.Alert;
    static Color C_Text => UiTypography.Text;
    static Color C_Muted => UiTypography.Muted;
    static Color C_Btn => UiTypography.Btn;
    static Color C_BtnActive => UiTypography.BtnActive;
    static Color C_BtnHover => UiTheme.Current.BtnHover;
    static Color C_GraphA => UiTheme.Current.GraphA;
    static Color C_GraphB => UiTheme.Current.GraphB;
    static Color C_GraphC => UiTheme.Current.GraphC;

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
        cameraFollow = FindFirstObjectByType<CameraFollow>();
        if (rocket != null) dataLogger = rocket.GetComponent<DataLogger>();
    }

    void Start()
    {
        HideLegacyUI();
        UILocale.OnLanguageChanged -= OnLanguageChanged;
        UILocale.OnLanguageChanged += OnLanguageChanged;
        UiTheme.OnThemeChanged -= OnThemeChanged;
        UiTheme.OnThemeChanged += OnThemeChanged;
        Build();
        WireLegacyDashboard();
        built = true;
    }

    void OnDestroy()
    {
        UILocale.OnLanguageChanged -= OnLanguageChanged;
        UiTheme.OnThemeChanged -= OnThemeChanged;
        if (Instance == this) Instance = null;
    }

    void OnLanguageChanged()
    {
        if (!built || rebuilding) return;
        RebuildUi();
    }

    void OnThemeChanged()
    {
        if (!built || rebuilding) return;
        RebuildUi();
    }

    void RebuildUi()
    {
        rebuilding = true;
        built = false;
        modeButtons.Clear();
        modeButtonImages.Clear();
        metricLabels.Clear();
        if (canvasRoot != null) Destroy(canvasRoot);
        Build();
        WireLegacyDashboard();
        built = true;
        rebuilding = false;
        RefreshCamLabel();
        UpdateTrajButtonLabel();
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
        canvasRoot = canvasGo;
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;
        canvasGo.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        BuildTopBar(canvasGo.transform);
        BuildTopMenu(canvasGo.transform);
        BuildLeftPanel(canvasGo.transform);
        BuildRightPanel(canvasGo.transform);
        BuildBottomBar(canvasGo.transform);
        BuildCenterHint(canvasGo.transform);
        BuildResultOverlay(canvasGo.transform);
        BuildProgressBar(canvasGo.transform);
        ApplyPanelsVisibility();
    }

    void BuildTopMenu(Transform parent)
    {
        // Компактна смуга: лише політ (без дублів з правої панелі / top bar)
        topMenuGo = CreatePanel("TopMenu", parent, UiTheme.DarkChrome);
        var rt = topMenuGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(0, -102);
        rt.offsetMax = new Vector2(0, -70);
        Outline(topMenuGo, 1f);

        float x = 16f;
        MenuBtn(topMenuGo.transform, ref x, UILocale.IsUK ? "Старт [Space]" : "Start [Space]", OnStartLanding, true);
        MenuBtn(topMenuGo.transform, ref x, UILocale.IsUK ? "Ідеал [I]" : "Ideal [I]", OnApplyIdealPresets, false);
        MenuBtn(topMenuGo.transform, ref x, UILocale.IsUK ? "Стоп [Esc]" : "Stop [Esc]", OnStop, false);
        MenuBtn(topMenuGo.transform, ref x, UILocale.IsUK ? "Порівняти [P]" : "Compare [P]", OnStartCompare, false);
        MenuBtn(topMenuGo.transform, ref x, UILocale.IsUK ? "Траєкторія [L]" : "Trajectory [L]", OnToggleTrajectoryLine, false);
        MenuBtn(topMenuGo.transform, ref x, UILocale.IsUK ? "Огляд [T]" : "Overview [T]", OnFullTrajectoryView, false);
        MenuBtn(topMenuGo.transform, ref x, UILocale.IsUK ? "Експорт [E]" : "Export [E]", OnExportResults, false);

        var tip = CreateText(topMenuGo.transform,
            UILocale.IsUK
                ? "1–4 алгоритм  ·  H панелі  ·  F/C/R камера  ·  Y тема"
                : "1–4 algorithm  ·  H panels  ·  F/C/R camera  ·  Y theme",
            12, UiTheme.ChromeText);
        var trt = tip.rectTransform;
        trt.anchorMin = trt.anchorMax = new Vector2(1, 0.5f);
        trt.pivot = new Vector2(1, 0.5f);
        trt.anchoredPosition = new Vector2(-16, 0);
        trt.sizeDelta = new Vector2(420, 26);
        tip.alignment = TextAlignmentOptions.Right;
    }

    void MenuBtn(Transform parent, ref float x, string label, UnityEngine.Events.UnityAction action, bool primary)
    {
        Color bg = primary
            ? (UiTheme.IsLightBackground
                ? new Color(0.12f, 0.42f, 0.28f, 1f)
                : new Color(0.14f, 0.32f, 0.22f, 1f))
            : C_Btn;
        if (!primary && UiTheme.IsLightBackground)
            bg = new Color(0.86f, 0.88f, 0.92f, 1f);

        var go = CreatePanel("MBtn", parent, bg);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0);
        rt.sizeDelta = new Vector2(Mathf.Max(92f, label.Length * 8.6f + 24f), 26);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        Color txtCol = primary || !UiTheme.IsLightBackground
            ? UiTheme.TextOnDark
            : C_Text;
        if (primary) txtCol = UiTheme.TextOnDark;
        var t = CreateText(go.transform, label, 12, txtCol, FontStyles.Bold);
        StretchFull(t.rectTransform, 4, 2, 4, 2);
        t.alignment = TextAlignmentOptions.Center;
        btn.onClick.AddListener(action);
        x += rt.sizeDelta.x + 7f;
    }

    void HandleHotkeys()
    {
        // Не перехоплювати, якщо UI InputField у фокусі
        if (UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null)
        {
            var sel = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            if (sel.GetComponent<TMPro.TMP_InputField>() != null
                || sel.GetComponent<UnityEngine.UI.InputField>() != null)
                return;
        }

        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (Input.GetKeyDown(KeyCode.H) && !ctrl) TogglePanels();
        if (Input.GetKeyDown(KeyCode.Space)) OnStartLanding();
        if (Input.GetKeyDown(KeyCode.I)) OnApplyIdealPresets();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (resultShown) HideLandingResult();
            else OnStop();
        }
        if (Input.GetKeyDown(KeyCode.L)) OnToggleTrajectoryLine();
        if (Input.GetKeyDown(KeyCode.F)) OnCamFollow();
        if (Input.GetKeyDown(KeyCode.T)) OnFullTrajectoryView();
        if (Input.GetKeyDown(KeyCode.C)) OnCamManual();
        if (Input.GetKeyDown(KeyCode.R)) OnCamReset();
        if (Input.GetKeyDown(KeyCode.E)) OnExportResults();
        if (Input.GetKeyDown(KeyCode.O)) OnOpenExportFolder();
        if (Input.GetKeyDown(KeyCode.G)) UILocale.Toggle();
        if (Input.GetKeyDown(KeyCode.Y)) UiTheme.Cycle();

        // Алгоритми 1–4
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            SelectMode(RocketPhysics.ControlMode.PID);
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            SelectMode(RocketPhysics.ControlMode.Fuzzy);
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            SelectMode(RocketPhysics.ControlMode.Neural);
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            SelectMode(RocketPhysics.ControlMode.Hybrid);

        if (Input.GetKeyDown(KeyCode.P)) OnStartCompare();
        if (Input.GetKeyDown(KeyCode.X)) OnCancelCompare();
    }

    void SelectMode(RocketPhysics.ControlMode mode)
    {
        if (rocket == null) return;
        if (sim != null && sim.IsExperimentRunning) return;
        HideLandingResult();
        ClearGraphs();
        ResetFlightPeaks();
        overviewCam = false;
        ResolveCamera()?.SetMode(CameraFollow.ViewMode.Follow);
        // Зміна режиму: робочі GNC + номінальні (складніші) IC, не «залиплий» ідеал
        IdealLandingPresets.ApplyDefaultControllerTuning(
            rocket, rocket.fuzzyController, rocket.neuralController, rocket.hybridController);
        RestoreNominalInitialConditions();
        rocket.PrepareMode(mode);
        RefreshCamLabel();
        NotifyInfo(string.Format(UILocale.T("msg_selected"), UILocale.ModeName(mode))
                   + "\n" + UILocale.T("ins_ideal_hint"));
        if (txtHint)
        {
            txtHint.gameObject.SetActive(true);
            txtHint.text = UILocale.ModeName(mode) + "  →  [I] ідеал / Space старт";
        }
    }

    void TogglePanels()
    {
        panelsHidden = !panelsHidden;
        ApplyPanelsVisibility();
        NotifyInfo(panelsHidden
            ? (UILocale.IsUK ? "Панелі сховано (H — показати)" : "Panels hidden (H — show)")
            : (UILocale.IsUK ? "Панелі показано" : "Panels visible"));
    }

    void ApplyPanelsVisibility()
    {
        bool show = !panelsHidden;
        if (leftPanelGo) leftPanelGo.SetActive(show);
        if (rightPanelGo) rightPanelGo.SetActive(show);
        if (bottomBarGo) bottomBarGo.SetActive(show);
        if (centerHintGo) centerHintGo.SetActive(show && txtHint != null && txtHint.gameObject.activeSelf);
        // top bar + menu always visible for settings
        if (txtHideBtn != null)
            txtHideBtn.text = panelsHidden
                ? (UILocale.IsUK ? "Показати" : "Show UI")
                : (UILocale.IsUK ? "Сховати" : "Hide UI");
    }

    void BuildTopBar(Transform parent)
    {
        var bar = CreatePanel("TopBar", parent, C_Panel);
        topBarGo = bar;
        DockTop(bar.GetComponent<RectTransform>(), 64f);
        Outline(bar, 1.5f);

        // Тонка лінія знизу
        var accent = CreatePanel("TopAccent", bar.transform, new Color(C_Edge.r, C_Edge.g, C_Edge.b, 0.7f));
        var art = accent.GetComponent<RectTransform>();
        art.anchorMin = new Vector2(0, 0);
        art.anchorMax = new Vector2(1, 0);
        art.pivot = new Vector2(0.5f, 0);
        art.anchoredPosition = Vector2.zero;
        art.sizeDelta = new Vector2(0, 2);

        // ── ЛІВОРУЧ: назва + підзаголовок ──
        txtTitle = CreateText(bar.transform, UILocale.T("app_title"), 20, C_Accent, FontStyles.Bold);
        var trTitle = txtTitle.rectTransform;
        trTitle.anchorMin = trTitle.anchorMax = new Vector2(0, 0.5f);
        trTitle.pivot = new Vector2(0, 0.5f);
        trTitle.anchoredPosition = new Vector2(20, 8);
        trTitle.sizeDelta = new Vector2(220, 28);

        txtSubtitle = CreateText(bar.transform, UILocale.T("app_sub"), 11, C_Muted);
        var trSub = txtSubtitle.rectTransform;
        trSub.anchorMin = trSub.anchorMax = new Vector2(0, 0.5f);
        trSub.pivot = new Vector2(0, 0.5f);
        trSub.anchoredPosition = new Vector2(20, -12);
        trSub.sizeDelta = new Vector2(340, 18);

        // ── ЦЕНТР: поточний алгоритм ──
        txtMode = CreateText(bar.transform, UILocale.T("algo_fmt").Replace("{0}", "—"), 14, C_Amber, FontStyles.Bold);
        txtMode.alignment = TextAlignmentOptions.Center;
        var trMode = txtMode.rectTransform;
        trMode.anchorMin = trMode.anchorMax = new Vector2(0.5f, 0.5f);
        trMode.pivot = new Vector2(0.5f, 0.5f);
        trMode.anchoredPosition = Vector2.zero;
        trMode.sizeDelta = new Vector2(380, 28);

        // ── ПРАВОРУЧ (зліва направо до краю): час | мова | сховати | статус ──
        // Статус — крайній справа
        var statusRow = CreatePanel("StatusRow", bar.transform, new Color(0, 0, 0, 0));
        statusRow.GetComponent<Image>().raycastTarget = false;
        var srRt = statusRow.GetComponent<RectTransform>();
        srRt.anchorMin = srRt.anchorMax = new Vector2(1, 0.5f);
        srRt.pivot = new Vector2(1, 0.5f);
        srRt.anchoredPosition = new Vector2(-16, 0);
        srRt.sizeDelta = new Vector2(150, 32);

        var dot = CreatePanel("Dot", statusRow.transform, C_Muted);
        var drt = dot.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0, 0.5f);
        drt.pivot = new Vector2(0, 0.5f);
        drt.anchoredPosition = new Vector2(0, 0);
        drt.sizeDelta = new Vector2(9, 9);
        statusDot = dot.GetComponent<Image>();

        txtStatus = CreateText(statusRow.transform, UILocale.T("st_ready"), 13, C_Muted, FontStyles.Bold);
        var srt = txtStatus.rectTransform;
        srt.anchorMin = srt.anchorMax = new Vector2(0, 0.5f);
        srt.pivot = new Vector2(0, 0.5f);
        srt.anchoredPosition = new Vector2(16, 0);
        srt.sizeDelta = new Vector2(130, 26);

        // Сховати UI
        var hideGo = CreatePanel("HideBtn", bar.transform, C_BtnActive);
        var hideRt = hideGo.GetComponent<RectTransform>();
        hideRt.anchorMin = hideRt.anchorMax = new Vector2(1, 0.5f);
        hideRt.pivot = new Vector2(1, 0.5f);
        hideRt.anchoredPosition = new Vector2(-178, 0);
        hideRt.sizeDelta = new Vector2(100, 30);
        var hideBtn = hideGo.AddComponent<Button>();
        hideBtn.targetGraphic = hideGo.GetComponent<Image>();
        txtHideBtn = CreateText(hideGo.transform, UILocale.IsUK ? "Сховати" : "Hide UI", 12,
            UiTheme.ContrastOn(C_BtnActive), FontStyles.Bold);
        StretchFull(txtHideBtn.rectTransform, 4, 2, 4, 2);
        txtHideBtn.alignment = TextAlignmentOptions.Center;
        hideBtn.onClick.AddListener(TogglePanels);

        // Мова
        var langGo = CreatePanel("LangBtn", bar.transform, C_Btn);
        var langRt = langGo.GetComponent<RectTransform>();
        langRt.anchorMin = langRt.anchorMax = new Vector2(1, 0.5f);
        langRt.pivot = new Vector2(1, 0.5f);
        langRt.anchoredPosition = new Vector2(-288, 0);
        langRt.sizeDelta = new Vector2(100, 30);
        var langBtn = langGo.AddComponent<Button>();
        langBtn.targetGraphic = langGo.GetComponent<Image>();
        var langTxt = CreateText(langGo.transform, UILocale.T("btn_lang"), 12,
            UiTheme.ContrastOn(C_Btn), FontStyles.Bold);
        StretchFull(langTxt.rectTransform, 4, 2, 4, 2);
        langTxt.alignment = TextAlignmentOptions.Center;
        langBtn.onClick.AddListener(() => UILocale.Toggle());

        // Тема UI (поруч з мовою)
        var themeGo = CreatePanel("ThemeBtn", bar.transform, C_BtnActive);
        var themeRt = themeGo.GetComponent<RectTransform>();
        themeRt.anchorMin = themeRt.anchorMax = new Vector2(1, 0.5f);
        themeRt.pivot = new Vector2(1, 0.5f);
        themeRt.anchoredPosition = new Vector2(-398, 0);
        themeRt.sizeDelta = new Vector2(100, 30);
        var themeBtn = themeGo.AddComponent<Button>();
        themeBtn.targetGraphic = themeGo.GetComponent<Image>();
        string themeLabel = UILocale.IsUK ? UiTheme.ButtonLabelUk : UiTheme.ButtonLabel;
        var themeTxt = CreateText(themeGo.transform, themeLabel, 12,
            UiTheme.ContrastOn(C_BtnActive), FontStyles.Bold);
        StretchFull(themeTxt.rectTransform, 4, 2, 4, 2);
        themeTxt.alignment = TextAlignmentOptions.Center;
        themeBtn.onClick.AddListener(() =>
        {
            UiTheme.Cycle();
            NotifyInfo(UILocale.IsUK
                ? "Тема: " + UiTheme.ButtonLabelUk
                : "Theme: " + UiTheme.ButtonLabel);
        });

        // Час симуляції
        txtTime = CreateText(bar.transform, string.Format(UILocale.T("time_fmt"), 0f), 13, C_Text);
        txtTime.alignment = TextAlignmentOptions.Right;
        var trTime = txtTime.rectTransform;
        trTime.anchorMin = trTime.anchorMax = new Vector2(1, 0.5f);
        trTime.pivot = new Vector2(1, 0.5f);
        trTime.anchoredPosition = new Vector2(-518, 0);
        trTime.sizeDelta = new Vector2(120, 28);
    }

    void BuildLeftPanel(Transform parent)
    {
        var panel = CreatePanel("LeftPanel", parent, C_Panel);
        leftPanelGo = panel;
        DockLeft(panel.GetComponent<RectTransform>(), 14, 108, 58, 330);
        Outline(panel);

        var viewport = CreatePanel("LViewport", panel.transform, new Color(0, 0, 0, 0));
        viewport.GetComponent<Image>().raycastTarget = false;
        var vrt = viewport.GetComponent<RectTransform>();
        StretchFull(vrt, 0, 0, 0, 0);
        viewport.AddComponent<RectMask2D>();

        var content = CreatePanel("LContent", viewport.transform, new Color(0, 0, 0, 0));
        content.GetComponent<Image>().raycastTarget = false;
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1);
        crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 1);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(0, 400); // перерахується після контенту

        var scroll = panel.AddComponent<ScrollRect>();
        scroll.viewport = vrt;
        scroll.content = crt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        Transform root = content.transform;
        float y = -16f;
        txtHdrTelem = Header(root, UILocale.T("h_telem"), ref y);

        txtAlt = Metric(root, UILocale.T("m_alt"), UILocale.T("u_m"), ref y);
        txtVel = Metric(root, UILocale.T("m_vy"), UILocale.T("u_ms"), ref y);
        txtHVel = Metric(root, UILocale.T("m_vh"), UILocale.T("u_ms"), ref y);
        txtThr = Metric(root, UILocale.T("m_thr"), UILocale.T("u_kn"), ref y);
        thrBarFill = MakeBar(root, ref y, C_Cyan);
        txtTwr = Metric(root, UILocale.T("m_twr"), "—", ref y);
        txtTilt = Metric(root, UILocale.T("m_tilt"), UILocale.T("u_deg"), ref y);
        tiltBarFill = MakeBar(root, ref y, C_Amber);
        txtRate = Metric(root, UILocale.T("m_rate"), UILocale.T("u_dps"), ref y);
        txtFuel = Metric(root, UILocale.T("m_fuel"), UILocale.T("u_kg"), ref y);
        fuelBarFill = MakeBar(root, ref y, C_Ok);
        txtFuelPct = Metric(root, UILocale.T("m_fuel_pct"), UILocale.T("u_pct"), ref y);
        txtMass = Metric(root, UILocale.T("m_mass"), UILocale.T("u_t"), ref y);
        txtMiss = Metric(root, UILocale.T("m_miss"), UILocale.T("u_m"), ref y);
        txtAcc = Metric(root, UILocale.T("m_acc"), UILocale.T("u_ms2"), ref y);
        txtEta = Metric(root, UILocale.T("m_eta"), UILocale.T("u_s"), ref y);
        txtScore = Metric(root, UILocale.T("m_score"), UILocale.T("u_score"), ref y);

        y -= 6f;
        txtHdrLive = Header(root, UILocale.T("h_live"), ref y);
        txtPeakVy = Metric(root, UILocale.T("m_peak_vy"), UILocale.T("u_ms"), ref y);
        txtPeakTilt = Metric(root, UILocale.T("m_peak_tilt"), UILocale.T("u_deg"), ref y);
        txtMinH = Metric(root, UILocale.T("m_min_h"), UILocale.T("u_m"), ref y);
        txtDeltaStrip = CreateText(root, "Δ —", 12, C_Muted);
        txtDeltaStrip.enableWordWrapping = true;
        PinTL(txtDeltaStrip.rectTransform, 14, y, 300, 36);
        y -= 40f;

        y -= 4f;
        txtHdrCrit = Header(root, UILocale.T("h_crit"), ref y);
        txtCritV = CriterionLine(root, "|Vy| < 3.5", ref y);
        txtCritA = CriterionLine(root, "tilt < 7°", ref y);
        txtCritM = CriterionLine(root, "miss < 25 m", ref y);
        txtCritH = CriterionLine(root, "|Vh| < 5", ref y);

        y -= 6f;
        txtHdrInsight = Header(root, UILocale.T("h_insight"), ref y);
        txtInsight = CreateText(root, UILocale.T("ins_wait"), 14, C_Text);
        txtInsight.enableWordWrapping = true;
        txtInsight.overflowMode = TextOverflowModes.Truncate;
        txtInsight.enableWordWrapping = true;
        txtInsight.alignment = TextAlignmentOptions.TopLeft;
        txtInsight.overflowMode = TextOverflowModes.Overflow;
        PinTL(txtInsight.rectTransform, 14, y, 300, 72);
        y -= 78f;

        y -= 4f;
        txtHdrGraphs = Header(root, UILocale.T("h_graphs"), ref y);
        txtGraphHint = CreateText(root, UILocale.T("graph_hint"), 10, C_Muted);
        PinTL(txtGraphHint.rectTransform, 16, y, 310, 14);
        y -= 16f;
        graphAlt = MakeGraph(root, UILocale.T("m_alt"), UILocale.T("u_m"), C_GraphA, ref y, null, "F0");
        graphVel = MakeGraph(root, "Vy", UILocale.T("u_ms"), C_GraphB, ref y, -3.5f, "F1");
        graphThr = MakeGraph(root, UILocale.T("m_thr"), UILocale.T("u_kn"), C_GraphC, ref y, null, "F0");

        // Точна висота контенту — без зайвого порожнього низу
        crt.sizeDelta = new Vector2(0, Mathf.Max(200f, -y + 28f));
    }

    void BuildRightPanel(Transform parent)
    {
        const float padX = 14f;
        const float innerW = 300f;

        var panel = CreatePanel("RightPanel", parent, C_Panel);
        rightPanelGo = panel;
        DockRight(panel.GetComponent<RectTransform>(), 12, 108, 52, 328);
        Outline(panel);

        var viewport = CreatePanel("Viewport", panel.transform, new Color(0, 0, 0, 0));
        viewport.GetComponent<Image>().raycastTarget = false;
        var vrt = viewport.GetComponent<RectTransform>();
        StretchFull(vrt, 0, 4, 0, 4);
        viewport.AddComponent<RectMask2D>();

        var content = CreatePanel("Content", viewport.transform, new Color(0, 0, 0, 0));
        content.GetComponent<Image>().raycastTarget = false;
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1);
        crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 1);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(0, 400);

        var scroll = panel.AddComponent<ScrollRect>();
        scroll.viewport = vrt;
        scroll.content = crt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 36f;

        float y = -12f;
        txtHow = CreateText(content.transform, UILocale.T("how"), 11, C_Accent, FontStyles.Bold);
        PinTL(txtHow.rectTransform, padX, y, innerW, 18);
        y -= 22f;

        // ── 1. Алгоритм ──
        Header(content.transform, UILocale.T("h_step1"), ref y);
        modeButtons.Clear();
        modeButtonImages.Clear();
        modeButtons.Add(ModeButton(content.transform, UILocale.T("mode_btn_a"),
            UILocale.T("mode_sub_a"), RocketPhysics.ControlMode.PID, ref y));
        modeButtons.Add(ModeButton(content.transform, UILocale.T("mode_btn_b"),
            UILocale.T("mode_sub_b"), RocketPhysics.ControlMode.Fuzzy, ref y));
        modeButtons.Add(ModeButton(content.transform, UILocale.T("mode_btn_c"),
            UILocale.T("mode_sub_c"), RocketPhysics.ControlMode.Neural, ref y));
        modeButtons.Add(ModeButton(content.transform, UILocale.T("mode_btn_d"),
            UILocale.T("mode_sub_d"), RocketPhysics.ControlMode.Hybrid, ref y));

        y -= 8f;
        // ── 2. Керування (без дублів top-menu: старт/ідеал/стоп/експорт там) ──
        Header(content.transform, UILocale.T("h_step2"), ref y);
        ActionButton(content.transform, UILocale.T("btn_compare"),
            UiTheme.IsLightBackground ? new Color(0.15f, 0.35f, 0.55f, 1f) : C_BtnActive, ref y, OnStartCompare);
        ActionButton(content.transform, UILocale.T("btn_cancel"), C_Btn, ref y, OnCancelCompare);

        txtCamMode = CreateText(content.transform, UILocale.T("cam_prefix") + UILocale.T("cam_follow"), 11, C_Accent, FontStyles.Bold);
        PinTL(txtCamMode.rectTransform, padX + 2, y, innerW - 4, 18);
        y -= 18f;
        txtCamHelp = CreateText(content.transform,
            UILocale.IsUK ? "F follow · T огляд · C ручне · R скинути · L траєкторія"
                          : "F follow · T overview · C manual · R reset · L trajectory",
            10, C_Muted);
        PinTL(txtCamHelp.rectTransform, padX + 2, y, innerW - 4, 28);
        txtCamHelp.enableWordWrapping = true;
        y -= 32f;

        // ── Умови тесту ──
        Header(content.transform, UILocale.T("h_step3"), ref y);
        txtTestsVal = SliderLine(content.transform, UILocale.T("sl_tests"), 5, 40, 15, ref y, out testsSlider);
        txtWindVal = SliderLine(content.transform, UILocale.T("sl_wind"), 0, 25, 10, ref y, out windSlider);
        SliderLine(content.transform, UILocale.T("sl_time"), 1, 40, 20, ref y, out timeScaleSlider);
        noiseToggle = ToggleLine(content.transform, UILocale.T("tg_noise"), true, ref y);
        trainToggle = ToggleLine(content.transform, UILocale.T("tg_train"), true, ref y);

        y -= 6f;
        // ── Результати порівняння ──
        Header(content.transform, UILocale.T("h_results"), ref y);
        txtPid = Stat(content.transform, UILocale.T("stat_a"), ref y);
        txtFuzzy = Stat(content.transform, UILocale.T("stat_b"), ref y);
        txtNeural = Stat(content.transform, UILocale.T("stat_c"), ref y);
        txtHybrid = Stat(content.transform, UILocale.T("stat_d"), ref y);

        y -= 4f;
        txtWinner = CreateText(content.transform, UILocale.T("winner_none"), 12, C_Ok, FontStyles.Bold);
        PinTL(txtWinner.rectTransform, padX, y, innerW, 20);
        y -= 24f;

        // Плашка повідомлень (фон + текст)
        var infoBg = CreatePanel("InfoBox", content.transform, C_PanelSoft);
        PinTL(infoBg.GetComponent<RectTransform>(), padX, y, innerW, 72);
        Outline(infoBg, 1f);
        txtInfo = CreateText(infoBg.transform, UILocale.T("tip"), 11, C_Muted);
        StretchFull(txtInfo.rectTransform, 8, 6, 8, 6);
        txtInfo.enableWordWrapping = true;
        txtInfo.overflowMode = TextOverflowModes.Truncate;
        txtInfo.alignment = TextAlignmentOptions.TopLeft;
        y -= 80f;

        crt.sizeDelta = new Vector2(0, Mathf.Max(180f, -y + 16f));
    }

    // ─── User actions ───

    void OnStartLanding()
    {
        if (rocket == null) return;
        if (sim != null && sim.IsExperimentRunning)
        {
            NotifyInfo(UILocale.T("msg_cancel_first"));
            return;
        }
        HideLandingResult();
        ClearGraphs();
        ResetFlightPeaks();
        overviewCam = false;
        ResolveCamera()?.SetMode(CameraFollow.ViewMode.Follow);
        RefreshCamLabel();
        ApplySettings();
        rocket.ResetSimulation();

        // UI-вітер/шум → реальна одиночна посадка (не лише Monte-Carlo)
        float wind = windSlider != null ? windSlider.value : 0f;
        bool noise = noiseToggle != null && noiseToggle.isOn;
        // Після Ideal massVar/angVar могли стати 0 — відновлюємо робочі default
        float massVar = 6f;
        float angVar = 7f;
        if (sim != null)
        {
            if (sim.massVariationPercent > 0.05f) massVar = sim.massVariationPercent;
            if (sim.angleVariationDegrees > 0.05f) angVar = sim.angleVariationDegrees;
            sim.windStrength = wind;
            sim.enableNoise = noise;
            if (noise)
            {
                sim.massVariationPercent = massVar;
                sim.angleVariationDegrees = angVar;
            }
        }
        rocket.ApplyFlightDisturbances(wind, noise, massVar, angVar);

        string dist = wind < 0.05f && !noise
            ? (UILocale.IsUK ? "без збурень" : "no disturbances")
            : (UILocale.IsUK
                ? $"вітер≈{wind:F0} · шум={(noise ? "ON" : "OFF")}"
                : $"wind≈{wind:F0} · noise={(noise ? "ON" : "OFF")}");
        NotifyInfo(string.Format(UILocale.T("msg_started"), UILocale.ModeName(rocket.controlMode)) + "\n" + dist);
        if (txtHint) txtHint.gameObject.SetActive(false);
    }

    /// <summary>
    /// Виставляє для поточного (і всіх) алгоритмів значення,
    /// при яких номінальна посадка стабільно успішна.
    /// </summary>
    void OnApplyIdealPresets()
    {
        if (rocket == null) return;
        if (sim != null && sim.IsExperimentRunning)
        {
            NotifyInfo(UILocale.T("msg_cancel_first"));
            return;
        }

        IdealLandingPresets.Apply(rocket, sim, out string uk, out string en);
        string msg = UILocale.IsUK ? uk : en;

        // Синхронізувати UI-слайдери з пресетом
        if (windSlider) windSlider.value = 0f;
        if (noiseToggle) noiseToggle.isOn = false;
        if (trainToggle) trainToggle.isOn = false;
        ApplySettings();

        HideLandingResult();
        ClearGraphs();
        ResetFlightPeaks();
        NotifyInfo(msg);
        if (txtHint) txtHint.gameObject.SetActive(false);
    }

    void OnToggleTrajectoryLine()
    {
        var tv = FindFirstObjectByType<TrajectoryVisualizer>();
        if (tv == null) return;
        trajVisible = !tv.IsVisible;
        tv.SetVisible(trajVisible);
        UpdateTrajButtonLabel();
        NotifyInfo(trajVisible ? UILocale.T("msg_traj_on") : UILocale.T("msg_traj_off"));
    }

    void UpdateTrajButtonLabel()
    {
        var tv = FindFirstObjectByType<TrajectoryVisualizer>();
        if (tv != null) trajVisible = tv.IsVisible;
        // Кнопка траєкторії тепер лише в top-menu — label опційний
        if (txtTrajBtn != null)
            txtTrajBtn.text = trajVisible ? UILocale.T("btn_traj_on") : UILocale.T("btn_traj_off");
    }

    void ResetFlightPeaks()
    {
        flightPeaksActive = true;
        peakVy = 0f;
        peakTilt = 0f;
        minAltLive = float.MaxValue;
        prevAlt = prevAbsVy = prevTilt = prevThr = 0f;
        prevVyForAcc = 0f;
        if (txtPeakVy) txtPeakVy.text = "—";
        if (txtPeakTilt) txtPeakTilt.text = "—";
        if (txtMinH) txtMinH.text = "—";
        if (txtDeltaStrip) txtDeltaStrip.text = "Δ —";
    }

    void OnStop()
    {
        if (sim != null && sim.IsExperimentRunning)
        {
            sim.CancelExperiment();
            NotifyInfo(UILocale.IsUK ? "Авто-тест зупиняється..." : "Stopping auto-test...");
            return;
        }
        if (rocket == null) return;
        rocket.StopSimulation(keepPosition: true);
        HideLandingResult();
        NotifyInfo(UILocale.T("msg_stopped"));
        if (txtStatus)
        {
            txtStatus.text = UILocale.T("st_stop");
            txtStatus.color = C_Amber;
        }
    }

    void OnToggleTrajectoryView() => OnFullTrajectoryView();

    /// <summary>
    /// Переводить камеру в позицію, з якої видно повну траєкторію
    /// (стартова висота → поточна точка → landing pad).
    /// </summary>
    void OnFullTrajectoryView()
    {
        var cam = ResolveCamera();
        if (cam == null) return;
        overviewCam = true;
        cam.SnapToFullTrajectoryView();
        RefreshCamLabel();
        NotifyInfo(UILocale.T("msg_cam_traj"));
    }

    void OnCamFollow()
    {
        overviewCam = false;
        ResolveCamera()?.SetMode(CameraFollow.ViewMode.Follow);
        RefreshCamLabel();
        NotifyInfo(UILocale.T("msg_cam_follow"));
    }

    void OnCamManual()
    {
        overviewCam = false;
        var cam = ResolveCamera();
        if (cam == null) return;
        cam.SetMode(CameraFollow.ViewMode.Manual);
        RefreshCamLabel();
        NotifyInfo(UILocale.T("msg_cam_manual"));
    }

    void OnCamReset()
    {
        var cam = ResolveCamera();
        if (cam == null) return;
        if (cam.mode == CameraFollow.ViewMode.Overview)
        {
            cam.SnapToFullTrajectoryView();
            overviewCam = true;
            NotifyInfo(UILocale.T("msg_cam_reset"));
        }
        else
        {
            cam.ResetManualOrbit();
            cam.SetMode(CameraFollow.ViewMode.Follow);
            overviewCam = false;
            NotifyInfo(UILocale.T("msg_cam_reset"));
        }
        RefreshCamLabel();
    }

    void OnExportResults()
    {
        try
        {
            // Prefer comparison export if available
            if (sim != null && sim.HasComparisonResults)
            {
                lastExportPath = sim.SaveComparisonReports();
                NotifyInfo(string.Format(UILocale.T("msg_export_cmp"), lastExportPath));
                return;
            }

            if (rocket == null || rocket.metrics == null)
            {
                NotifyInfo(UILocale.T("msg_no_data"));
                return;
            }

            bool hasFlight = rocket.metrics.totalFlightTime > 0.05f
                             || rocket.state.simulationFinished
                             || (dataLogger != null && dataLogger.SampleCount > 0);
            if (!hasFlight && (rocket.metrics.touchdownVelocity <= 0f && !rocket.metrics.isSuccessfulLanding))
            {
                NotifyInfo(UILocale.T("msg_no_data"));
                return;
            }

            if (dataLogger == null && rocket != null)
                dataLogger = rocket.GetComponent<DataLogger>();
            dataLogger?.Save();

            lastExportPath = ExportCurrentLandingPackage();
            NotifyInfo(string.Format(UILocale.T("msg_export_ok"), lastExportPath));
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            NotifyInfo(ex.Message);
        }
    }

    /// <summary>Повний пакет: CSV кроків + JSON + MD + SVG графіки траєкторії.</summary>
    string ExportCurrentLandingPackage(LandingMetrics metrics = null, float maxV = -1f, float maxA = -1f, float maxM = -1f, float maxH = -1f)
    {
        if (dataLogger == null && rocket != null) dataLogger = rocket.GetComponent<DataLogger>();
        dataLogger?.Save();
        var p = rocket != null ? rocket.parameters : null;
        var m = metrics ?? rocket?.metrics;
        if (m == null) throw new System.InvalidOperationException("No metrics");

        var export = new ResearchExporter.LandingExportData
        {
            algorithm = FriendlyMode(rocket != null ? rocket.controlMode : RocketPhysics.ControlMode.PID),
            timestamp = ResearchExporter.Stamp(),
            metrics = m,
            maxTouchdownVelocity = maxV > 0 ? maxV : (p != null ? p.maxTouchdownVelocity : 3.5f),
            maxLandingAngle = maxA > 0 ? maxA : (p != null ? p.maxLandingAngle : 7f),
            maxHorizontalMiss = maxM > 0 ? maxM : (p != null ? p.maxHorizontalMiss : 25f),
            maxHorizontalSpeed = maxH > 0 ? maxH : (p != null ? p.maxHorizontalSpeed : 5f),
            trajectoryCsvPath = dataLogger != null ? dataLogger.LastFilePath : null,
            trajectoryRows = dataLogger != null ? dataLogger.CloneRows() : null,
            samples = dataLogger != null ? dataLogger.CloneSamples() : null
        };
        return ResearchExporter.ExportLanding(export);
    }

    void OnOpenExportFolder()
    {
        try
        {
            ResearchExporter.OpenLogsFolder();
            NotifyInfo(string.Format(UILocale.T("msg_folder"), ResearchExporter.LogsDirectory));
        }
        catch (System.Exception ex)
        {
            NotifyInfo("Не вдалося відкрити папку: " + ex.Message);
        }
    }

    CameraFollow ResolveCamera()
    {
        if (cameraFollow == null) cameraFollow = FindFirstObjectByType<CameraFollow>();
        return cameraFollow;
    }

    void RefreshCamLabel()
    {
        var cam = ResolveCamera();
        if (txtCamMode == null) return;
        if (cam == null) { txtCamMode.text = UILocale.T("cam_prefix") + "—"; return; }
        overviewCam = cam.mode == CameraFollow.ViewMode.Overview;
        txtCamMode.text = UILocale.T("cam_prefix") + UILocale.CamLabel(cam.mode);
        txtCamMode.color = cam.mode == CameraFollow.ViewMode.Manual ? C_Amber : C_Cyan;
    }

    void OnStartCompare()
    {
        if (sim == null) { NotifyInfo("SimulationManager missing"); return; }
        if (sim.IsExperimentRunning) { NotifyInfo(UILocale.T("st_batch") + "…"); return; }
        HideLandingResult();
        ApplySettings();
        sim.RequestFullExperiment();
        NotifyInfo(UILocale.T("msg_compare"));
        if (txtHint) txtHint.gameObject.SetActive(false);
    }

    void OnCancelCompare()
    {
        if (sim == null) return;
        sim.CancelExperiment();
        NotifyInfo(UILocale.T("btn_cancel"));
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

        var p = rocket?.parameters;
        float maxV = p != null && p.maxTouchdownVelocity > 0.1f ? p.maxTouchdownVelocity : 3.5f;
        float maxA = p != null && p.maxLandingAngle > 0.1f ? p.maxLandingAngle : 7f;
        float maxM = p != null && p.maxHorizontalMiss > 0.1f ? p.maxHorizontalMiss : 25f;
        float maxH = p != null && p.maxHorizontalSpeed > 0.1f ? p.maxHorizontalSpeed : 5f;

        bool ok = m.isSuccessfulLanding;
        if (txtResultTitle)
        {
            txtResultTitle.text = ok ? UILocale.T("res_ok") : UILocale.T("res_fail");
            txtResultTitle.color = ok ? C_Ok : C_Alert;
        }
        if (txtResultBody)
        {
            // Без повторюваного заголовка (він уже в txtResultTitle)
            txtResultBody.text = m.BuildUserSummary(maxV, maxA, maxM, maxH, includeTitle: false)
                                 + "\n" + UILocale.T("res_footer");
            txtResultBody.color = C_Text;
        }
        // Не дублювати повний текст у лівій панелі «Висновок»
        if (txtInsight != null)
        {
            txtInsight.text = ok
                ? string.Format(UILocale.T("ins_ok"), m.SuccessScore)
                : (UILocale.IsUK ? "Див. вікно результату →" : "See result dialog →");
            txtInsight.color = ok ? C_Ok : C_Alert;
        }

        try
        {
            lastExportPath = ExportCurrentLandingPackage(m, maxV, maxA, maxM, maxH);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Export] auto landing report failed: " + ex.Message);
        }
        if (resultPanelBg)
        {
            if (UiTheme.IsLightBackground)
                resultPanelBg.color = ok
                    ? new Color(0.92f, 0.97f, 0.94f, 0.99f)
                    : new Color(0.98f, 0.92f, 0.92f, 0.99f);
            else
                resultPanelBg.color = ok
                    ? new Color(0.12f, 0.14f, 0.13f, 0.96f)
                    : new Color(0.16f, 0.11f, 0.11f, 0.96f);
        }

        if (txtStatus)
        {
            txtStatus.text = ok ? UILocale.T("st_success") : UILocale.T("st_fail");
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
        resultRoot = CreatePanel("ResultOverlay", parent, UiTheme.ModalScrim);
        var rt = resultRoot.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        resultRoot.GetComponent<Image>().raycastTarget = true;
        resultRoot.transform.SetAsLastSibling();

        // Компактна картка: мінімум порожнього місця між текстом і кнопками
        var card = CreatePanel("ResultCard", resultRoot.transform, UiTheme.ModalCard);
        resultPanelBg = card.GetComponent<Image>();
        var crt = card.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(520, 300);
        Outline(card, 2.5f);

        // Заголовок
        txtResultTitle = CreateText(card.transform, "РЕЗУЛЬТАТ", 22, C_Ok, FontStyles.Bold);
        var trtTitle = txtResultTitle.rectTransform;
        trtTitle.anchorMin = new Vector2(0, 1);
        trtTitle.anchorMax = new Vector2(1, 1);
        trtTitle.pivot = new Vector2(0.5f, 1);
        trtTitle.anchoredPosition = new Vector2(0, -14);
        trtTitle.sizeDelta = new Vector2(-32, 30);
        txtResultTitle.alignment = TextAlignmentOptions.Center;

        // Тіло одразу під заголовком, низ щільно до кнопок
        txtResultBody = CreateText(card.transform, "", 15, C_Text);
        txtResultBody.enableWordWrapping = true;
        txtResultBody.overflowMode = TextOverflowModes.Overflow;
        txtResultBody.alignment = TextAlignmentOptions.TopLeft;
        txtResultBody.lineSpacing = 2f;
        var brt = txtResultBody.rectTransform;
        brt.anchorMin = new Vector2(0, 0);
        brt.anchorMax = new Vector2(1, 1);
        brt.offsetMin = new Vector2(24, 88);   // кнопки ~80 px
        brt.offsetMax = new Vector2(-24, -48); // заголовок ~44 px

        // Кнопки внизу, компактно
        float btnY = 44f;
        var trGo = CreatePanel("ShowTraj", card.transform, C_Btn);
        var trt = trGo.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0);
        trt.pivot = new Vector2(0.5f, 0);
        trt.anchoredPosition = new Vector2(-105, btnY);
        trt.sizeDelta = new Vector2(190, 34);
        var tbtn = trGo.AddComponent<Button>();
        tbtn.targetGraphic = trGo.GetComponent<Image>();
        var ttxt = CreateText(trGo.transform, UILocale.T("btn_show_traj"), 13,
            UiTheme.ContrastOn(C_Btn), FontStyles.Bold);
        StretchFull(ttxt.rectTransform, 4, 3, 4, 3);
        ttxt.alignment = TextAlignmentOptions.Center;
        tbtn.onClick.AddListener(() => { HideLandingResult(); OnFullTrajectoryView(); });

        var exGo = CreatePanel("ExportResult", card.transform, C_Btn);
        var ert = exGo.GetComponent<RectTransform>();
        ert.anchorMin = ert.anchorMax = new Vector2(0.5f, 0);
        ert.pivot = new Vector2(0.5f, 0);
        ert.anchoredPosition = new Vector2(105, btnY);
        ert.sizeDelta = new Vector2(190, 34);
        var ebtn = exGo.AddComponent<Button>();
        ebtn.targetGraphic = exGo.GetComponent<Image>();
        var etxt = CreateText(exGo.transform, UILocale.T("btn_export_short"), 13,
            UiTheme.ContrastOn(C_Btn), FontStyles.Bold);
        StretchFull(etxt.rectTransform, 4, 3, 4, 3);
        etxt.alignment = TextAlignmentOptions.Center;
        ebtn.onClick.AddListener(OnExportResults);

        var closeGo = CreatePanel("CloseResult", card.transform, C_BtnActive);
        var clrt = closeGo.GetComponent<RectTransform>();
        clrt.anchorMin = clrt.anchorMax = new Vector2(0.5f, 0);
        clrt.pivot = new Vector2(0.5f, 0);
        clrt.anchoredPosition = new Vector2(0, 8);
        clrt.sizeDelta = new Vector2(200, 32);
        var cbtn = closeGo.AddComponent<Button>();
        cbtn.targetGraphic = closeGo.GetComponent<Image>();
        var ctxt = CreateText(closeGo.transform, UILocale.T("btn_ok"), 14,
            UiTheme.ContrastOn(C_BtnActive), FontStyles.Bold);
        StretchFull(ctxt.rectTransform, 4, 3, 4, 3);
        ctxt.alignment = TextAlignmentOptions.Center;
        cbtn.onClick.AddListener(HideLandingResult);

        resultRoot.SetActive(false);
    }

    void BuildProgressBar(Transform parent)
    {
        progressRoot = CreatePanel("ProgressRoot", parent, UiTheme.DarkChrome);
        var rt = progressRoot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -108);
        rt.sizeDelta = new Vector2(560, 48);
        Outline(progressRoot);

        txtProgress = CreateText(progressRoot.transform, "Авто-тест…", 13, UiTheme.ChromeText, FontStyles.Bold);
        Pin(txtProgress.rectTransform, 0.5f, 1, 0.5f, 1, 0, -6, 540, 22);
        txtProgress.alignment = TextAlignmentOptions.Center;

        var bg = CreatePanel("PBg", progressRoot.transform, UiTheme.IsLightBackground
            ? new Color(0.8f, 0.84f, 0.9f, 1f)
            : new Color(0.05f, 0.06f, 0.08f, 1f));
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
        bottomBarGo = bar;
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.offsetMin = new Vector2(358, 8);
        rt.offsetMax = new Vector2(-378, 50);
        Outline(bar);

        txtBottom = CreateText(bar.transform, UILocale.T("bottom"), 12, C_Muted);
        StretchFull(txtBottom.rectTransform, 10, 4, 10, 4);
        txtBottom.alignment = TextAlignmentOptions.Center;
        txtBottom.enableWordWrapping = true;
    }

    void BuildCenterHint(Transform parent)
    {
        var hint = CreatePanel("CenterHint", parent, UiTheme.DarkChrome);
        centerHintGo = hint;
        var rt = hint.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 64);
        rt.sizeDelta = new Vector2(560, 48);
        Outline(hint);

        txtHint = CreateText(hint.transform, UILocale.T("hint"), 13, UiTheme.ChromeText);
        StretchFull(txtHint.rectTransform, 12, 6, 12, 6);
        txtHint.alignment = TextAlignmentOptions.Center;
        txtHint.enableWordWrapping = true;
    }

    void Update()
    {
        if (!built) return;

        HandleHotkeys();

        if (rocket == null) return;
        var s = rocket.state;

        float tilt = Vector3.Angle(s.rotation * Vector3.up, Vector3.up);
        float miss = new Vector2(s.position.x, s.position.z).magnitude;
        float thrPct = s.maxThrust > 1f ? s.currentThrust / s.maxThrust : 0f;
        float fuelPct = rocket.parameters != null && rocket.parameters.fuelMass > 1f
            ? s.currentFuelMass / rocket.parameters.fuelMass : 0f;

        float hVel = new Vector2(s.velocity.x, s.velocity.z).magnitude;
        float angRate = s.angularVelocity.magnitude * Mathf.Rad2Deg;
        float mass = Mathf.Max(1f, s.TotalMass);
        float g = AtmosphereModel.GetGravity(Mathf.Max(0f, s.position.y));
        float twr = s.currentThrust / (mass * g);
        float dt = Mathf.Max(Time.unscaledDeltaTime, 1e-4f);
        float rawAcc = (s.velocity.y - prevVyForAcc) / dt;
        prevVyForAcc = s.velocity.y;
        smoothedAcc = Mathf.Lerp(smoothedAcc, rawAcc, 1f - Mathf.Exp(-6f * dt));
        // Оцінка часу до землі (лінійна, лише для індикації)
        float eta = 0f;
        if (s.velocity.y < -0.15f && s.position.y > 0.5f)
            eta = s.position.y / Mathf.Abs(s.velocity.y);

        float maxV = rocket.parameters != null ? rocket.parameters.maxTouchdownVelocity : 3.5f;
        float maxA = rocket.parameters != null ? rocket.parameters.maxLandingAngle : 7f;
        float maxM = rocket.parameters != null ? rocket.parameters.maxHorizontalMiss : 25f;
        float maxH = rocket.parameters != null ? rocket.parameters.maxHorizontalSpeed : 5f;

        Write(txtAlt, $"{s.position.y:F1}", s.position.y < 80f ? C_Amber : C_Text);
        float av = Mathf.Abs(s.velocity.y);
        Write(txtVel, $"{av:F2}", av > 25f ? C_Alert : av > maxV ? C_Amber : C_Ok);
        Write(txtHVel, $"{hVel:F2}", hVel > maxH ? C_Alert : hVel > maxH * 0.6f ? C_Amber : C_Text);
        Write(txtThr, $"{s.currentThrust / 1000f:F1}", C_Text);
        Write(txtTwr, $"{twr:F2}", twr < 0.9f ? C_Amber : twr > 2.5f ? C_Alert : C_Ok);
        Write(txtTilt, $"{tilt:F2}", tilt > maxA ? C_Alert : tilt > maxA * 0.5f ? C_Amber : C_Text);
        Write(txtRate, $"{angRate:F1}", angRate > 15f ? C_Alert : angRate > 6f ? C_Amber : C_Text);
        Write(txtFuel, $"{s.currentFuelMass:F0}", fuelPct < 0.15f ? C_Alert : C_Text);
        Write(txtFuelPct, $"{fuelPct * 100f:F1}", fuelPct < 0.15f ? C_Alert : C_Text);
        Write(txtMass, $"{mass / 1000f:F2}", C_Text);
        Write(txtMiss, $"{miss:F2}", miss > maxM ? C_Alert : miss > maxM * 0.5f ? C_Amber : C_Text);
        Write(txtAcc, $"{smoothedAcc:F1}", Mathf.Abs(smoothedAcc) > 25f ? C_Amber : C_Text);
        Write(txtEta, eta > 0.05f && rocket.simulationArmed && !s.simulationFinished
            ? $"{eta:F1}" : "—", eta > 0f && eta < 8f ? C_Amber : C_Muted);

        // Live peak / change tracking during flight
        if (rocket.simulationArmed && !s.simulationFinished)
        {
            if (!flightPeaksActive) ResetFlightPeaks();
            if (av > peakVy) peakVy = av;
            if (tilt > peakTilt) peakTilt = tilt;
            if (s.position.y < minAltLive) minAltLive = s.position.y;
            Write(txtPeakVy, $"{peakVy:F2}", C_Text);
            Write(txtPeakTilt, $"{peakTilt:F2}", C_Text);
            Write(txtMinH, minAltLive < 1e8f ? $"{minAltLive:F1}" : "—", C_Text);

            float dAlt = s.position.y - prevAlt;
            float dVy = av - prevAbsVy;
            float dTilt = tilt - prevTilt;
            float dThr = s.currentThrust / 1000f - prevThr;
            if (txtDeltaStrip)
            {
                txtDeltaStrip.text =
                    $"Δh {Arrow(dAlt)}{Mathf.Abs(dAlt):F1}  ·  Δ|Vy| {Arrow(dVy)}{Mathf.Abs(dVy):F2}  ·  " +
                    $"Δ∠ {Arrow(dTilt)}{Mathf.Abs(dTilt):F2}  ·  ΔF {Arrow(dThr)}{Mathf.Abs(dThr):F1}";
                txtDeltaStrip.color = C_Muted;
            }
            prevAlt = s.position.y;
            prevAbsVy = av;
            prevTilt = tilt;
            prevThr = s.currentThrust / 1000f;
        }
        else if (!rocket.simulationArmed && !s.simulationFinished)
        {
            flightPeaksActive = false;
        }

        SetBar(thrBarFill, thrPct, C_Cyan);
        SetBar(fuelBarFill, fuelPct, fuelPct < 0.15f ? C_Alert : C_Ok);
        SetBar(tiltBarFill, Mathf.Clamp01(tilt / 15f), tilt > maxA ? C_Alert : C_Amber);

        bool nearGround = s.position.y < 40f || s.isLanded || s.simulationFinished;
        string lim = UILocale.IsUK ? "норма" : "limit";
        UpdateCriterion(txtCritV, av < maxV, $"|Vy|={av:F2}  ({lim} < {maxV})", nearGround || av < maxV);
        UpdateCriterion(txtCritA, tilt < maxA, $"∠={tilt:F2}°  ({lim} < {maxA}°)", true);
        UpdateCriterion(txtCritM, miss < maxM, $"Δ={miss:F1}  ({lim} < {maxM})", true);
        UpdateCriterion(txtCritH, hVel < maxH, $"|Vh|={hVel:F2}  ({lim} < {maxH})", true);

        UpdateInsight(s, av, hVel, tilt, miss, twr, fuelPct, eta, maxV, maxA, maxM, maxH);

        bool exp = sim != null && sim.IsExperimentRunning;
        if (txtMode && !exp)
            txtMode.text = string.Format(UILocale.T("algo_fmt"), UILocale.ModeName(rocket.controlMode));
        if (txtTime) txtTime.text = string.Format(UILocale.T("time_fmt"), s.time);

        if (txtStatus && !resultShown)
        {
            if (exp)
            {
                txtStatus.text = UILocale.T("st_batch");
                txtStatus.color = C_Amber;
                if (statusDot) statusDot.color = C_Amber;
            }
            else if (s.simulationFinished && rocket.simulationArmed == false && rocket.metrics != null
                     && (rocket.metrics.totalFlightTime > 0.1f || rocket.metrics.isSuccessfulLanding || rocket.metrics.timedOut))
            {
                // stopped mid-flight — OnStop
            }
            else if (s.simulationFinished && rocket.metrics != null && rocket.metrics.totalFlightTime > 0.05f)
            {
                bool ok = rocket.metrics.isSuccessfulLanding;
                txtStatus.text = ok ? UILocale.T("st_success") : UILocale.T("st_fail");
                txtStatus.color = ok ? C_Ok : C_Alert;
                if (statusDot) statusDot.color = ok ? C_Ok : C_Alert;
                Write(txtScore, $"{rocket.metrics.SuccessScore:F0}", ok ? C_Ok : C_Alert);
            }
            else if (rocket.simulationArmed && s.time > 0.05f)
            {
                txtStatus.text = UILocale.T("st_descent");
                txtStatus.color = C_Cyan;
                if (statusDot) statusDot.color = C_Cyan;
            }
            else if (rocket.simulationArmed)
            {
                txtStatus.text = UILocale.T("st_start");
                txtStatus.color = C_Amber;
                if (statusDot) statusDot.color = C_Amber;
            }
            else
            {
                txtStatus.text = UILocale.T("st_wait");
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
                    modeButtonImages[i].color = active ? C_Amber * 0.85f : C_Btn * 0.7f;
                else if (UiTheme.IsLightBackground)
                    modeButtonImages[i].color = active
                        ? new Color(0.55f, 0.72f, 0.92f, 1f)
                        : C_Btn;
                else
                    modeButtonImages[i].color = active ? C_BtnActive : C_Btn;
            }
        }

        // Щільніший семплінг під час польоту — зручніше стежити за змінами
        sampleTimer += Time.unscaledDeltaTime;
        float sampleDt = s.position.y < 200f ? 0.04f : 0.07f;
        if (sampleTimer >= sampleDt && !s.simulationFinished && s.time > 0f && rocket.simulationArmed)
        {
            sampleTimer = 0f;
            graphAlt?.Push(s.position.y);
            graphVel?.Push(s.velocity.y);
            graphThr?.Push(s.currentThrust / 1000f);
        }

        if (txtWindVal && windSlider) txtWindVal.text = $"{windSlider.value:F0}";
        if (txtTestsVal && testsSlider) txtTestsVal.text = $"{testsSlider.value:F0}";

        // Keep camera label in sync if user used hotkeys
        if (txtCamMode && cameraFollow != null && Time.frameCount % 15 == 0)
            RefreshCamLabel();
    }

    public void UpdateStatistics(float pid, float fuzzy, float neural, float hybrid = -1f)
    {
        Write(txtPid, $"{pid:F1} %", RateColor(pid));
        Write(txtFuzzy, $"{fuzzy:F1} %", RateColor(fuzzy));
        Write(txtNeural, $"{neural:F1} %", RateColor(neural));
        if (hybrid >= 0f) Write(txtHybrid, $"{hybrid:F1} %", RateColor(hybrid));

        string winner = UILocale.T("mode_pid");
        float max = pid;
        if (fuzzy >= max) { max = fuzzy; winner = UILocale.T("mode_fuzzy"); }
        if (neural >= max) { max = neural; winner = UILocale.T("mode_neural"); }
        if (hybrid > max) { max = hybrid; winner = UILocale.T("mode_hybrid"); }
        if (txtWinner)
        {
            txtWinner.text = string.Format(UILocale.T("winner_fmt"), winner, max);
            txtWinner.color = C_Ok;
        }
        if (txtInfo)
            txtInfo.text = string.Format(UILocale.T("msg_compare_done"), winner, max);
    }

    static Color RateColor(float pct)
    {
        if (pct >= 80f) return C_Ok;
        if (pct >= 50f) return C_Amber;
        if (pct > 0f) return C_Alert;
        return C_Muted;
    }

    static string FriendlyMode(RocketPhysics.ControlMode m) => UILocale.ModeName(m);

    static string Arrow(float d)
    {
        if (d > 0.05f) return "↑";
        if (d < -0.05f) return "↓";
        return "→";
    }

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

    /// <summary>Номінальні IC (складніші за Ideal). Слайдери вітру/шуму не чіпає.</summary>
    void RestoreNominalInitialConditions()
    {
        if (rocket?.parameters == null) return;
        var p = rocket.parameters;
        p.startPosition = new Vector3(0f, 1800f, 0f);
        p.startVelocity = new Vector3(0f, -72f, 0f);
        p.startEulerAngles = new Vector3(0f, 0f, 3.5f);
        p.dryMass = 25600f;
        p.fuelMass = 14000f;
        p.maxThrust = 845000f;
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
        // Подвійна обводка: зовнішня + внутрішня — чіткі рамки на світлих темах
        Color e = C_Edge;
        float d = dist;
        if (UiTheme.IsLightBackground)
        {
            e = new Color(0.18f, 0.22f, 0.3f, 0.85f);
            d = Mathf.Max(dist, 1.4f);
        }
        else
        {
            e.a = Mathf.Max(e.a, 0.9f);
        }

        var o1 = go.AddComponent<UnityEngine.UI.Outline>();
        o1.effectColor = e;
        o1.effectDistance = new Vector2(d, -d);
        o1.useGraphicAlpha = true;

        if (UiTheme.IsLightBackground)
        {
            var o2 = go.AddComponent<UnityEngine.UI.Outline>();
            o2.effectColor = new Color(0.05f, 0.06f, 0.08f, 0.55f);
            o2.effectDistance = new Vector2(d + 1.2f, -(d + 1.2f));
            o2.useGraphicAlpha = true;
        }
    }

    static TMP_Text CreateText(Transform parent, string text, float size, Color col, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        UiTypography.Apply(tmp, size, col, style);
        tmp.text = text ?? "";
        tmp.alignment = TextAlignmentOptions.Left;
        return tmp;
    }

    TMP_Text Header(Transform parent, string title, ref float y)
    {
        var t = CreateText(parent, title, 11, C_Accent, FontStyles.Bold);
        PinTL(t.rectTransform, 14, y, 300, 18);
        y -= 20f;
        var line = CreatePanel("line", parent, new Color(C_Edge.r, C_Edge.g, C_Edge.b, 0.35f));
        PinTL(line.GetComponent<RectTransform>(), 14, y + 4, 300, 1);
        y -= 6f;
        return t;
    }

    TMP_Text Metric(Transform parent, string key, string unit, ref float y)
    {
        // Підпис зліва · число біля правого краю · одиниця впритул справа
        var k = CreateText(parent, key, 12, C_Muted);
        PinTL(k.rectTransform, 14, y, 155, 20);
        metricLabels.Add(k);

        var v = CreateText(parent, "—", 15, C_Text, FontStyles.Bold);
        v.alignment = TextAlignmentOptions.Right;
        // Правий край числа ~ x=278 (панель ~330)
        PinTL(v.rectTransform, 168, y, 110, 22);

        var u = CreateText(parent, unit, 11, C_Muted);
        u.alignment = TextAlignmentOptions.Left;
        PinTL(u.rectTransform, 282, y, 40, 20);
        y -= 26f;
        return v;
    }

    static void Write(TMP_Text t, string value, Color c)
    {
        if (t == null) return;
        t.text = value;
        t.color = c;
    }

    TMP_Text CriterionLine(Transform parent, string label, ref float y)
    {
        var t = CreateText(parent, "[ ]  " + label, 12, C_Muted);
        PinTL(t.rectTransform, 16, y, 300, 22);
        y -= 24f;
        return t;
    }

    static void UpdateCriterion(TMP_Text t, bool ok, string detail, bool emphasize)
    {
        if (t == null) return;
        t.text = (ok ? "[+]  " : "[ ]  ") + detail;
        t.color = ok ? C_Ok : (emphasize ? C_Alert : C_Amber);
    }

    void UpdateInsight(RocketState s, float av, float hVel, float tilt, float miss,
        float twr, float fuelPct, float eta,
        float maxV, float maxA, float maxM, float maxH)
    {
        if (txtInsight == null) return;

        if (batchMode || (sim != null && sim.IsExperimentRunning))
        {
            txtInsight.text = UILocale.T("ins_batch");
            txtInsight.color = C_Amber;
            return;
        }

        if (resultShown)
            return; // текст уже в модалці — не дублювати в лівій панелі

        if (s.simulationFinished && rocket.metrics != null && rocket.metrics.totalFlightTime > 0.05f)
        {
            bool ok = rocket.metrics.isSuccessfulLanding;
            txtInsight.text = ok
                ? string.Format(UILocale.T("ins_ok"), rocket.metrics.SuccessScore)
                : (UILocale.IsUK ? "Див. вікно результату" : "See result dialog");
            txtInsight.color = ok ? C_Ok : C_Alert;
            return;
        }

        if (!rocket.simulationArmed)
        {
            txtInsight.text = UILocale.T("ins_wait");
            txtInsight.color = C_Muted;
            return;
        }

        if (s.position.y > 400f)
        {
            txtInsight.text = twr < 0.95f ? UILocale.T("ins_high_low_twr") : UILocale.T("ins_high_ok");
            txtInsight.color = C_Cyan;
        }
        else if (s.position.y > 80f)
        {
            if (av > 40f) { txtInsight.text = UILocale.T("ins_fast"); txtInsight.color = C_Alert; }
            else if (tilt > maxA) { txtInsight.text = UILocale.T("ins_tilt"); txtInsight.color = C_Alert; }
            else if (miss > maxM) { txtInsight.text = UILocale.T("ins_miss"); txtInsight.color = C_Amber; }
            else
            {
                txtInsight.text = string.Format(UILocale.T("ins_mid"), eta);
                txtInsight.color = C_Ok;
            }
        }
        else
        {
            int bad = 0;
            if (av >= maxV) bad++;
            if (tilt >= maxA) bad++;
            if (miss >= maxM) bad++;
            if (hVel >= maxH) bad++;
            if (fuelPct < 0.05f) bad++;

            if (bad == 0) { txtInsight.text = UILocale.T("ins_term_ok"); txtInsight.color = C_Ok; }
            else if (av >= maxV)
            {
                txtInsight.text = string.Format(UILocale.T("ins_term_v"), av, maxV);
                txtInsight.color = C_Alert;
            }
            else
            {
                txtInsight.text = string.Format(UILocale.T("ins_term_bad"), bad);
                txtInsight.color = C_Amber;
            }
        }
    }

    Image MakeBar(Transform parent, ref float y, Color fill)
    {
        Color barBg = UiTheme.IsLightBackground
            ? new Color(0.78f, 0.8f, 0.84f, 1f)
            : new Color(0.05f, 0.05f, 0.06f, 1f);
        var bg = CreatePanel("Bar", parent, barBg);
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

    TelemetryGraph MakeGraph(Transform parent, string title, string unit, Color line, ref float y,
        float? threshold, string fmt)
    {
        var go = new GameObject("Graph_" + title, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(parent, false);
        PinTL(go.GetComponent<RectTransform>(), 14, y, 312, 96);
        var raw = go.GetComponent<RawImage>();
        raw.color = Color.white;
        // Рамка-підкладка під графік
        var frame = CreatePanel("GFrame", parent, UiTheme.IsLightBackground
            ? new Color(0.9f, 0.92f, 0.95f, 1f)
            : new Color(0.05f, 0.06f, 0.08f, 1f));
        var frt = frame.GetComponent<RectTransform>();
        PinTL(frt, 12, y + 2, 316, 100);
        frame.transform.SetSiblingIndex(go.transform.GetSiblingIndex());
        Outline(frame, 1f);

        var g = go.AddComponent<TelemetryGraph>();
        g.autoScale = true;
        g.showFill = true;
        g.showZeroLine = true;
        g.valueFormat = fmt ?? "F1";
        g.Configure(title, unit, line, threshold);
        y -= 108f;
        return g;
    }

    Button ModeButton(Transform parent, string title, string subtitle, RocketPhysics.ControlMode mode, ref float y)
    {
        const float w = 300f;
        var go = CreatePanel("Mode_" + mode, parent, C_Btn);
        PinTL(go.GetComponent<RectTransform>(), 14, y, w, 34);
        var btn = go.AddComponent<Button>();
        var img = go.GetComponent<Image>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.14f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.8f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.06f;
        btn.colors = colors;
        modeButtonImages.Add(img);

        var txt = CreateText(go.transform, title, 12, C_Text, FontStyles.Bold);
        StretchFull(txt.rectTransform, 10, 3, 10, 3);
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        txt.enableWordWrapping = false;
        txt.overflowMode = TextOverflowModes.Ellipsis;
        txt.raycastTarget = false;
        txt.transform.SetAsLastSibling();

        btn.onClick.AddListener(() =>
        {
            if (rocket == null) return;
            if (sim != null && sim.IsExperimentRunning)
            {
                NotifyInfo(UILocale.T("msg_cancel_first"));
                return;
            }
            HideLandingResult();
            ClearGraphs();
            ResetFlightPeaks();
            overviewCam = false;
            ResolveCamera()?.SetMode(CameraFollow.ViewMode.Follow);
            rocket.PrepareMode(mode);
            RefreshCamLabel();
            NotifyInfo(string.Format(UILocale.T("msg_selected"), title));
            if (txtHint)
            {
                txtHint.gameObject.SetActive(true);
                txtHint.text = title + "  ->  Space";
            }
        });
        y -= 42f;
        return btn;
    }

    void ActionButton(Transform parent, string label, Color col, ref float y, UnityEngine.Events.UnityAction action)
    {
        const float w = 300f;
        // Світла тема: не лишаємо «чорні» кнопки з темним текстом
        Color bg = col;
        if (UiTheme.IsLightBackground)
        {
            float luma = 0.2126f * col.r + 0.7152f * col.g + 0.0722f * col.b;
            if (luma < 0.45f)
                bg = Color.Lerp(col, new Color(0.2f, 0.45f, 0.7f, 1f), 0.35f);
        }
        var go = CreatePanel("Action", parent, bg);
        PinTL(go.GetComponent<RectTransform>(), 14, y, w, 32);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.88f);
        btn.colors = colors;
        Color tc = UiTheme.ActionBtnText(bg);
        var txt = CreateText(go.transform, label, 12, tc, FontStyles.Bold);
        StretchFull(txt.rectTransform, 6, 2, 6, 2);
        txt.alignment = TextAlignmentOptions.Center;
        txt.overflowMode = TextOverflowModes.Ellipsis;
        txt.raycastTarget = false;
        btn.onClick.AddListener(action);
        y -= 38f;
    }

    TMP_Text SliderLine(Transform parent, string label, float min, float max, float val, ref float y, out Slider slider)
    {
        var k = CreateText(parent, label, 11, C_Muted);
        PinTL(k.rectTransform, 14, y, 200, 16);
        var v = CreateText(parent, val.ToString("F0"), 12, C_Accent, FontStyles.Bold);
        v.alignment = TextAlignmentOptions.Right;
        PinTL(v.rectTransform, 220, y, 90, 16);
        y -= 18f;

        Color track = UiTheme.IsLightBackground
            ? new Color(0.82f, 0.85f, 0.9f, 1f)
            : new Color(0.08f, 0.09f, 0.12f, 1f);
        Color fillCol = UiTheme.IsLightBackground
            ? new Color(0.15f, 0.45f, 0.75f, 1f)
            : C_Cyan * 0.65f;
        Color handleCol = UiTheme.IsLightBackground
            ? new Color(0.12f, 0.35f, 0.6f, 1f)
            : C_Amber;

        var sGo = CreatePanel("SliderBG", parent, track);
        PinTL(sGo.GetComponent<RectTransform>(), 14, y, 300, 16);
        slider = sGo.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = true;
        slider.value = val;

        var fillArea = CreatePanel("Fill Area", sGo.transform, new Color(0, 0, 0, 0));
        StretchFull(fillArea.GetComponent<RectTransform>(), 2, 3, 2, 3);
        fillArea.GetComponent<Image>().raycastTarget = false;
        var fill = CreatePanel("Fill", fillArea.transform, fillCol);
        StretchFull(fill.GetComponent<RectTransform>(), 0, 0, 0, 0);

        var handle = CreatePanel("Handle", sGo.transform, handleCol);
        var hrt = handle.GetComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(14, 16);
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
        PinTL(row.GetComponent<RectTransform>(), 14, y, 300, 26);

        var box = CreatePanel("Box", row.transform, C_Btn);
        var brt = box.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
        brt.pivot = new Vector2(0, 0.5f);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(22, 22);

        var check = CreatePanel("Check", box.transform, C_Accent);
        StretchFull(check.GetComponent<RectTransform>(), 4, 4, 4, 4);

        var txt = CreateText(row.transform, label, 11, C_Text);
        var trt = txt.rectTransform;
        trt.anchorMin = trt.anchorMax = new Vector2(0, 0.5f);
        trt.pivot = new Vector2(0, 0.5f);
        trt.anchoredPosition = new Vector2(32, 0);
        trt.sizeDelta = new Vector2(260, 20);

        var toggle = row.AddComponent<Toggle>();
        toggle.targetGraphic = box.GetComponent<Image>();
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = on;
        y -= 30f;
        return toggle;
    }

    TMP_Text Stat(Transform parent, string name, ref float y)
    {
        var k = CreateText(parent, name, 11, C_Muted);
        PinTL(k.rectTransform, 14, y, 160, 18);
        var v = CreateText(parent, "—", 12, C_Text, FontStyles.Bold);
        v.alignment = TextAlignmentOptions.Right;
        PinTL(v.rectTransform, 170, y, 140, 18);
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
