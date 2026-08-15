using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
/// <summary>
/// Runtime HUD Mission Control (UA/EN, 8 тем UI).
/// Top chrome: brand · mode · time | flight actions | Theme/Lang над Status/Hide.
/// Ліворуч: landing GATE → guidance → primary/dynamics/propulsion → charts.
/// Праворуч: quick-start → algorithm 2×2 → compare → cam → test sliders → %.
/// Низ: смуга поточного кроку (фази посадки поточного алгоритму).
/// Rebuild UI (тема/мова) зберігає samples графіків, слайдери й toggles.
/// </summary>
[DefaultExecutionOrder(-50)]
public class MissionControlUI : MonoBehaviour
{
    public static MissionControlUI Instance { get; private set; }

    static Sprite s_uiWhite;

    RocketPhysics rocket;
    SimulationManager sim;
    CameraFollow cameraFollow;
    DataLogger dataLogger;

    TMP_Text txtAlt, txtVel, txtThr, txtTilt, txtFuel, txtMiss, txtMode, txtStatus, txtTime, txtScore;
    TMP_Text txtHVel, txtMass, txtTwr, txtEta, txtAcc, txtRate;
    TMP_Text txtPeakVy, txtPeakTilt, txtMinH, txtDeltaStrip;
    TMP_Text txtCritV, txtCritA, txtCritM, txtCritH;
    TMP_Text txtInsight, txtFuelPct;
    TMP_Text txtPid, txtFuzzy, txtNeural, txtHybrid, txtWinner, txtInfo;
    TMP_Text txtWindVal, txtTestsVal;
    TMP_Text txtResultTitle, txtResultBody, txtProgress, txtCamMode, txtCamHelp;
    TMP_Text txtTrajBtn, txtTitle, txtSubtitle, txtHow, txtGraphHint;
    TMP_Text txtHdrTelem, txtHdrLive, txtHdrCrit, txtHdrInsight, txtHdrGraphs;
    TMP_Text txtStep;
    Button trajToggleBtn;

    // Metric label texts (for language refresh)
    readonly List<TMP_Text> metricLabels = new();

    Slider windSlider, testsSlider, timeScaleSlider;
    Toggle noiseToggle, trainToggle;
    Image thrBarFill, fuelBarFill, tiltBarFill, statusDot, progressFill, resultPanelBg;
    GameObject resultRoot, progressRoot, canvasRoot, stepBarGo;
    GameObject leftPanelGo, rightPanelGo, topBarGo, topMenuGo;
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

        // Preserve live state across theme/language rebuild
        float[] snapAlt = graphAlt != null ? graphAlt.GetSamples() : null;
        float[] snapVel = graphVel != null ? graphVel.GetSamples() : null;
        float[] snapThr = graphThr != null ? graphThr.GetSamples() : null;
        float windV = windSlider != null ? windSlider.value : 10f;
        float testsV = testsSlider != null ? testsSlider.value : 15f;
        float timeV = timeScaleSlider != null ? timeScaleSlider.value : 20f;
        bool noiseOn = noiseToggle == null || noiseToggle.isOn;
        bool trainOn = trainToggle == null || trainToggle.isOn;
        bool hide = panelsHidden;
        bool hadResult = resultShown;
        string infoSnap = txtInfo != null ? txtInfo.text : null;

        modeButtons.Clear();
        modeButtonImages.Clear();
        metricLabels.Clear();
        if (canvasRoot != null) Destroy(canvasRoot);
        Build();
        WireLegacyDashboard();

        // Restore
        if (windSlider) windSlider.value = windV;
        if (testsSlider) testsSlider.value = testsV;
        if (timeScaleSlider) timeScaleSlider.value = timeV;
        if (noiseToggle) noiseToggle.isOn = noiseOn;
        if (trainToggle) trainToggle.isOn = trainOn;
        if (snapAlt != null && snapAlt.Length > 0) graphAlt?.RestoreSamples(snapAlt);
        if (snapVel != null && snapVel.Length > 0) graphVel?.RestoreSamples(snapVel);
        if (snapThr != null && snapThr.Length > 0) graphThr?.RestoreSamples(snapThr);
        if (infoSnap != null && txtInfo != null) txtInfo.text = infoSnap;
        panelsHidden = hide;
        ApplyPanelsVisibility();
        if (hadResult && rocket != null && rocket.metrics != null
            && rocket.metrics.totalFlightTime > 0.05f)
            ShowLandingResult(rocket.metrics);

        built = true;
        rebuilding = false;
        RefreshCamLabel();
        UpdateTrajButtonLabel();
        if (rocket != null) UpdateFlightStep(rocket.state);
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
        UiTypography.ConfigureCanvas(canvas);
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        // Prefer height match → fewer fractional scales on typical 16:9 (less pixelated TMP)
        scaler.matchWidthOrHeight = 1f;
        scaler.referencePixelsPerUnit = 100f;
        canvasGo.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        BuildTopChrome(canvasGo.transform);
        BuildLeftPanel(canvasGo.transform);
        BuildRightPanel(canvasGo.transform);
        BuildResultOverlay(canvasGo.transform);
        BuildProgressBar(canvasGo.transform);
        BuildStepBar(canvasGo.transform);
        ApplyPanelsVisibility();
    }

    /// <summary>1×1 white sprite for Simple UI images (avoids 9-slice thickness bugs).</summary>
    static Sprite UiWhite()
    {
        if (s_uiWhite != null) return s_uiWhite;
        var tex = Texture2D.whiteTexture;
        s_uiWhite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f);
        return s_uiWhite;
    }

    static void StyleSimpleImage(Image img, Color c)
    {
        if (img == null) return;
        img.sprite = UiWhite();
        img.type = Image.Type.Simple;
        img.color = c;
        img.preserveAspect = false;
    }

    enum MenuBtnKind { Normal, Start, Stop }

    /// <summary>
    /// Єдиний top chrome: identity + flight state | actions | settings.
    /// Два ряди в одній панелі — без подвійної рамки TopBar+TopMenu.
    /// </summary>
    void BuildTopChrome(Transform parent)
    {
        const float H = 84f;
        var chrome = CreatePanel("TopChrome", parent, C_Panel);
        topBarGo = chrome;
        topMenuGo = chrome; // same root — always visible with top bar
        var rt = chrome.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(0, -H);
        rt.offsetMax = Vector2.zero;
        Outline(chrome, 1.2f);

        // Bottom accent hairline
        var accent = CreatePanel("TopAccent", chrome.transform, new Color(C_Edge.r, C_Edge.g, C_Edge.b, 0.5f));
        accent.GetComponent<Image>().raycastTarget = false;
        var art = accent.GetComponent<RectTransform>();
        art.anchorMin = new Vector2(0, 0);
        art.anchorMax = new Vector2(1, 0);
        art.pivot = new Vector2(0.5f, 0);
        art.anchoredPosition = Vector2.zero;
        art.sizeDelta = new Vector2(0, 2);

        // ── ROW 1: identity | mode+time | status | settings ──
        var row1 = CreatePanel("Row1", chrome.transform, new Color(0, 0, 0, 0));
        row1.GetComponent<Image>().raycastTarget = false;
        var r1 = row1.GetComponent<RectTransform>();
        r1.anchorMin = new Vector2(0, 0.5f);
        r1.anchorMax = new Vector2(1, 1);
        r1.offsetMin = new Vector2(12, 2);
        r1.offsetMax = new Vector2(-230, -4); // leave room for right 2×2 cluster

        // Brand (left)
        txtTitle = CreateText(row1.transform, UILocale.T("app_title"), 16, C_Accent, FontStyles.Bold);
        var trTitle = txtTitle.rectTransform;
        trTitle.anchorMin = new Vector2(0, 0);
        trTitle.anchorMax = new Vector2(0, 1);
        trTitle.pivot = new Vector2(0, 0.5f);
        trTitle.anchoredPosition = new Vector2(0, 0);
        trTitle.sizeDelta = new Vector2(132, 0);
        txtTitle.alignment = TextAlignmentOptions.MidlineLeft;
        txtTitle.overflowMode = TextOverflowModes.Ellipsis;
        txtTitle.raycastTarget = false;

        // Mode pill — wide enough for short names (PID/Fuzzy/Neural/Hybrid)
        var modeBg = CreatePanel("ModePill", row1.transform, C_PanelSoft);
        modeBg.GetComponent<Image>().raycastTarget = false;
        var mrt = modeBg.GetComponent<RectTransform>();
        mrt.anchorMin = mrt.anchorMax = new Vector2(0, 0.5f);
        mrt.pivot = new Vector2(0, 0.5f);
        mrt.anchoredPosition = new Vector2(138, 0);
        mrt.sizeDelta = new Vector2(110, 26);
        txtMode = CreateText(modeBg.transform, "PID", 12, C_Amber, FontStyles.Bold);
        StretchFull(txtMode.rectTransform, 6, 2, 6, 2);
        txtMode.alignment = TextAlignmentOptions.Center;
        txtMode.overflowMode = TextOverflowModes.Overflow;
        txtMode.enableWordWrapping = false;
        txtMode.raycastTarget = false;

        // Time chip next to mode
        txtTime = CreateText(row1.transform, string.Format(UILocale.T("time_fmt"), 0f), 12, C_Text, FontStyles.Bold);
        var trTime = txtTime.rectTransform;
        trTime.anchorMin = trTime.anchorMax = new Vector2(0, 0.5f);
        trTime.pivot = new Vector2(0, 0.5f);
        trTime.anchoredPosition = new Vector2(258, 0);
        trTime.sizeDelta = new Vector2(96, 26);
        txtTime.alignment = TextAlignmentOptions.MidlineLeft;
        txtTime.overflowMode = TextOverflowModes.Overflow;
        txtTime.raycastTarget = false;

        txtSubtitle = null;

        // ── RIGHT cluster 2×2 — inset so chips never clip off-screen ──
        //   [ Theme ] [ Lang ]
        //   [ Status] [ Hide ]
        const float chipW = 72f;
        const float chipH = 28f;
        const float chipGap = 5f;
        const float rightW = chipW * 2f + chipGap; // 149
        const float rightPad = 64f; // inset from right edge (Hide fully on-screen)

        var right = CreatePanel("RightCluster", chrome.transform, new Color(0, 0, 0, 0));
        right.GetComponent<Image>().raycastTarget = false;
        var rr = right.GetComponent<RectTransform>();
        rr.anchorMin = new Vector2(1f, 0f);
        rr.anchorMax = new Vector2(1f, 1f);
        rr.pivot = new Vector2(1f, 0.5f);
        rr.offsetMin = new Vector2(-(rightPad + rightW), 6f);
        rr.offsetMax = new Vector2(-rightPad, -6f);

        // Top row HLG: Theme | Lang (same metrics as left flight buttons)
        var topRow = CreatePanel("RightTop", right.transform, new Color(0, 0, 0, 0));
        topRow.GetComponent<Image>().raycastTarget = false;
        var trt = topRow.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0.5f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = new Vector2(0f, 2f);
        trt.offsetMax = Vector2.zero;
        var topH = topRow.AddComponent<HorizontalLayoutGroup>();
        topH.spacing = chipGap;
        topH.childAlignment = TextAnchor.MiddleRight;
        topH.childControlWidth = false;
        topH.childControlHeight = true;
        topH.childForceExpandWidth = false;
        topH.childForceExpandHeight = true;
        topH.padding = new RectOffset(0, 0, 0, 0);

        MenuBtn(topRow.transform, UILocale.IsUK ? UiTheme.ButtonLabelUk : UiTheme.ButtonLabel,
            () =>
            {
                UiTheme.Cycle();
                NotifyInfo(UILocale.IsUK
                    ? "Тема: " + UiTheme.ButtonLabelUk
                    : "Theme: " + UiTheme.ButtonLabel);
            }, MenuBtnKind.Normal, chipW);
        MenuBtn(topRow.transform, UILocale.IsUK ? "EN" : "UA",
            () => UILocale.Toggle(), MenuBtnKind.Normal, chipW);

        // Bottom row HLG: Status | Hide — under Theme/Lang
        var botRow = CreatePanel("RightBot", right.transform, new Color(0, 0, 0, 0));
        botRow.GetComponent<Image>().raycastTarget = false;
        var brt = botRow.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(1f, 0.5f);
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = new Vector2(0f, -2f);
        var botH = botRow.AddComponent<HorizontalLayoutGroup>();
        botH.spacing = chipGap;
        botH.childAlignment = TextAnchor.MiddleRight;
        botH.childControlWidth = false;
        botH.childControlHeight = true;
        botH.childForceExpandWidth = false;
        botH.childForceExpandHeight = true;
        botH.padding = new RectOffset(0, 0, 0, 0);

        // Status as layout chip (same size)
        var statusBadge = CreatePanel("StatusBadge", botRow.transform, StatusBadgeBg(C_Muted));
        var stLe = statusBadge.AddComponent<LayoutElement>();
        stLe.preferredWidth = chipW;
        stLe.minWidth = chipW;
        stLe.preferredHeight = chipH;
        stLe.flexibleWidth = 0f;
        statusDot = statusBadge.GetComponent<Image>();
        txtStatus = CreateText(statusBadge.transform, UILocale.T("st_ready"), 11, C_Text, FontStyles.Bold);
        StretchFull(txtStatus.rectTransform, 4, 2, 4, 2);
        txtStatus.alignment = TextAlignmentOptions.Center;
        txtStatus.overflowMode = TextOverflowModes.Ellipsis;
        txtStatus.enableWordWrapping = false;
        txtStatus.raycastTarget = false;
        SetStatusVisual("st_ready", C_Muted);

        MenuBtn(botRow.transform, UILocale.T("top_hide"), TogglePanels, MenuBtnKind.Normal, chipW, out txtHideBtn);
        // Re-tint Hide as active style
        if (txtHideBtn != null)
        {
            var hideImg = txtHideBtn.transform.parent.GetComponent<Image>();
            if (hideImg != null) hideImg.color = C_BtnActive;
            txtHideBtn.color = UiTheme.ContrastOn(C_BtnActive);
        }

        // ── ROW 2: flight actions (same height 28, gap 5 as right chips) ──
        var row2 = CreatePanel("Row2", chrome.transform, new Color(0, 0, 0, 0));
        row2.GetComponent<Image>().raycastTarget = false;
        var r2 = row2.GetComponent<RectTransform>();
        r2.anchorMin = new Vector2(0, 0);
        r2.anchorMax = new Vector2(1, 0.5f);
        r2.offsetMin = new Vector2(10, 6);
        r2.offsetMax = new Vector2(-(rightPad + rightW + 8f), -2);

        var hlg = row2.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = chipGap;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        MenuBtn(row2.transform, UILocale.T("top_start") + "  Sp", OnStartLanding, MenuBtnKind.Start, chipW);
        MenuBtn(row2.transform, UILocale.T("top_stop") + "  Esc", OnStop, MenuBtnKind.Stop, chipW);
        MenuBtn(row2.transform, UILocale.T("top_ideal") + "  I", OnApplyIdealPresets, MenuBtnKind.Normal, chipW);
        trajToggleBtn = MenuBtn(row2.transform, UILocale.T("top_path_off") + "  L", OnToggleTrajectoryLine, MenuBtnKind.Normal, chipW, out txtTrajBtn);
        MenuBtn(row2.transform, UILocale.T("top_view") + "  T", OnFullTrajectoryView, MenuBtnKind.Normal, chipW);
        MenuBtn(row2.transform, UILocale.T("top_export") + "  E", OnExportResults, MenuBtnKind.Normal, chipW);
    }

    Button MenuBtn(Transform parent, string label, UnityEngine.Events.UnityAction action, MenuBtnKind kind,
        float width = 80f)
    {
        return MenuBtn(parent, label, action, kind, width, out _);
    }

    Button MenuBtn(Transform parent, string label, UnityEngine.Events.UnityAction action, MenuBtnKind kind,
        float width, out TMP_Text labelTxt)
    {
        Color bg;
        Color txtCol;
        switch (kind)
        {
            case MenuBtnKind.Start:
                bg = UiTheme.IsLightBackground
                    ? new Color(0.12f, 0.52f, 0.32f, 1f)
                    : new Color(0.14f, 0.42f, 0.28f, 1f);
                txtCol = UiTheme.TextOnDark;
                break;
            case MenuBtnKind.Stop:
                bg = UiTheme.IsLightBackground
                    ? new Color(0.68f, 0.2f, 0.2f, 1f)
                    : new Color(0.48f, 0.16f, 0.16f, 1f);
                txtCol = UiTheme.TextOnDark;
                break;
            default:
                bg = UiTheme.IsLightBackground
                    ? new Color(0.88f, 0.9f, 0.93f, 1f)
                    : C_Btn;
                txtCol = UiTheme.IsLightBackground ? C_Text : UiTheme.TextOnDark;
                break;
        }

        var go = CreatePanel("MBtn", parent, bg);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.minWidth = width;
        le.flexibleWidth = 0f;
        le.preferredHeight = 28f;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.1f);
        colors.pressedColor = new Color(0.88f, 0.88f, 0.9f);
        colors.fadeDuration = 0.05f;
        btn.colors = colors;

        labelTxt = CreateText(go.transform, label, 11, txtCol, FontStyles.Bold);
        StretchFull(labelTxt.rectTransform, 4, 2, 4, 2);
        labelTxt.alignment = TextAlignmentOptions.Center;
        labelTxt.overflowMode = TextOverflowModes.Ellipsis;
        labelTxt.enableWordWrapping = false;
        labelTxt.raycastTarget = false;
        btn.onClick.AddListener(action);
        return btn;
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
        if (stepBarGo) stepBarGo.SetActive(show);
        // top chrome always visible for settings / flight actions
        if (txtHideBtn != null)
            txtHideBtn.text = panelsHidden ? UILocale.T("top_show") : UILocale.T("top_hide");
    }

    /// <summary>Chip, прив'язаний до правого краю parent. xR — права грань (≤0). yOfs — вертикальний зсув від центру.</summary>
    void PlaceTopChip(Transform parent, string name, ref float xR, float w, float h, Color bg,
        string label, out TMP_Text labelTxt, UnityEngine.Events.UnityAction onClick, float yOfs = 0f)
    {
        var go = CreatePanel(name, parent, bg);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(xR, yOfs);
        xR -= w;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.1f);
        colors.pressedColor = new Color(0.88f, 0.88f, 0.9f);
        colors.fadeDuration = 0.05f;
        btn.colors = colors;

        labelTxt = CreateText(go.transform, label, 11, UiTheme.ContrastOn(bg), FontStyles.Bold);
        StretchFull(labelTxt.rectTransform, 4, 2, 4, 2);
        labelTxt.alignment = TextAlignmentOptions.Center;
        labelTxt.overflowMode = TextOverflowModes.Ellipsis;
        labelTxt.enableWordWrapping = false;
        labelTxt.raycastTarget = false;
        btn.onClick.AddListener(onClick);
    }

    static Color StatusBadgeBg(Color accent)
    {
        if (UiTheme.IsLightBackground)
            return Color.Lerp(accent, new Color(0.92f, 0.94f, 0.97f, 1f), 0.72f);
        return Color.Lerp(accent, new Color(0.08f, 0.09f, 0.12f, 1f), 0.55f);
    }

    void SetStatusVisual(string key, Color accent)
    {
        if (txtStatus != null)
        {
            txtStatus.text = UILocale.T(key);
            // На бейджі — контрастний текст
            float luma = 0.2126f * accent.r + 0.7152f * accent.g + 0.0722f * accent.b;
            if (UiTheme.IsLightBackground)
                txtStatus.color = luma > 0.55f ? new Color(0.08f, 0.1f, 0.14f) : accent;
            else
                txtStatus.color = Color.Lerp(accent, Color.white, 0.35f);
        }
        if (statusDot != null)
            statusDot.color = StatusBadgeBg(accent);
    }

    void BuildLeftPanel(Transform parent)
    {
        // Mission-control left column: GATE first → primary flight → rest → charts
        const float W = 338f;
        const float pad = 12f;
        const float inner = W - pad * 2f; // 314

        var panel = CreatePanel("LeftPanel", parent, C_Panel);
        leftPanelGo = panel;
        DockLeft(panel.GetComponent<RectTransform>(), 12, 96, 14, W);
        Outline(panel);

        var viewport = CreatePanel("LViewport", panel.transform, new Color(0, 0, 0, 0));
        viewport.GetComponent<Image>().raycastTarget = false;
        var vrt = viewport.GetComponent<RectTransform>();
        StretchFull(vrt, 0, 2, 0, 2);
        viewport.AddComponent<RectMask2D>();

        var content = CreatePanel("LContent", viewport.transform, new Color(0, 0, 0, 0));
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
        scroll.scrollSensitivity = 32f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.12f;

        Transform root = content.transform;
        float y = -10f;

        // ── 1. LANDING GATE (always first — decision at a glance) ──
        txtHdrCrit = Header(root, UILocale.T("h_crit"), ref y, pad, inner);
        BuildCriterionGrid(root, ref y, pad, inner);

        // ── 2. GUIDANCE (one sentence, high value) ──
        y -= 4f;
        txtHdrInsight = Header(root, UILocale.T("h_insight"), ref y, pad, inner);
        var insightBg = CreatePanel("InsightBg", root, C_PanelSoft);
        insightBg.GetComponent<Image>().raycastTarget = false;
        PinTL(insightBg.GetComponent<RectTransform>(), pad, y, inner, 56);
        txtInsight = CreateText(insightBg.transform, UILocale.T("ins_wait"), 13, C_Text);
        txtInsight.enableWordWrapping = true;
        txtInsight.alignment = TextAlignmentOptions.TopLeft;
        txtInsight.overflowMode = TextOverflowModes.Ellipsis;
        StretchFull(txtInsight.rectTransform, 8, 6, 8, 6);
        y -= 62f;

        // ── 3. PRIMARY FLIGHT STATE ──
        y -= 2f;
        txtHdrTelem = Header(root, UILocale.T("h_primary"), ref y, pad, inner);
        txtAlt = Metric(root, UILocale.T("m_alt"), UILocale.T("u_m"), ref y, pad, inner, primary: true);
        txtVel = Metric(root, UILocale.T("m_vy"), UILocale.T("u_ms"), ref y, pad, inner, primary: true);
        txtTilt = Metric(root, UILocale.T("m_tilt"), UILocale.T("u_deg"), ref y, pad, inner, primary: true);
        tiltBarFill = MakeBar(root, ref y, C_Amber, pad, inner);
        txtMiss = Metric(root, UILocale.T("m_miss"), UILocale.T("u_m"), ref y, pad, inner, primary: true);

        // ── 4. DYNAMICS ──
        y -= 4f;
        Header(root, UILocale.T("h_dyn"), ref y, pad, inner);
        txtHVel = Metric(root, UILocale.T("m_vh"), UILocale.T("u_ms"), ref y, pad, inner);
        txtThr = Metric(root, UILocale.T("m_thr"), UILocale.T("u_kn"), ref y, pad, inner);
        thrBarFill = MakeBar(root, ref y, C_Cyan, pad, inner);
        txtTwr = Metric(root, UILocale.T("m_twr"), "x", ref y, pad, inner);
        txtRate = Metric(root, UILocale.T("m_rate"), UILocale.T("u_dps"), ref y, pad, inner);
        txtAcc = Metric(root, UILocale.T("m_acc"), UILocale.T("u_ms2"), ref y, pad, inner);
        txtEta = Metric(root, UILocale.T("m_eta"), UILocale.T("u_s"), ref y, pad, inner);

        // ── 5. PROPULSION ──
        y -= 4f;
        Header(root, UILocale.T("h_prop"), ref y, pad, inner);
        // Fuel as single row: kg value + % unit context via fuelPct text below bar
        txtFuel = Metric(root, UILocale.T("m_fuel"), UILocale.T("u_kg"), ref y, pad, inner);
        fuelBarFill = MakeBar(root, ref y, C_Ok, pad, inner);
        txtFuelPct = Metric(root, UILocale.T("m_fuel_pct"), UILocale.T("u_pct"), ref y, pad, inner);
        txtMass = Metric(root, UILocale.T("m_mass"), UILocale.T("u_t"), ref y, pad, inner);
        txtScore = Metric(root, UILocale.T("m_score"), UILocale.T("u_score"), ref y, pad, inner);

        // ── 6. PEAKS / DELTA (compact) ──
        y -= 4f;
        txtHdrLive = Header(root, UILocale.T("h_live"), ref y, pad, inner);
        txtPeakVy = Metric(root, UILocale.T("m_peak_vy"), UILocale.T("u_ms"), ref y, pad, inner);
        txtPeakTilt = Metric(root, UILocale.T("m_peak_tilt"), UILocale.T("u_deg"), ref y, pad, inner);
        txtMinH = Metric(root, UILocale.T("m_min_h"), UILocale.T("u_m"), ref y, pad, inner);
        txtDeltaStrip = CreateText(root, "d —", 11, C_Muted);
        txtDeltaStrip.enableWordWrapping = true;
        txtDeltaStrip.overflowMode = TextOverflowModes.Ellipsis;
        PinTL(txtDeltaStrip.rectTransform, pad, y, inner, 32);
        y -= 36f;

        // ── 7. CHARTS ──
        y -= 2f;
        txtHdrGraphs = Header(root, UILocale.T("h_graphs"), ref y, pad, inner);
        txtGraphHint = CreateText(root, UILocale.T("graph_hint"), 10, C_Muted);
        PinTL(txtGraphHint.rectTransform, pad + 2, y, inner - 4, 14);
        y -= 16f;
        graphAlt = MakeGraph(root, UILocale.T("m_alt"), UILocale.T("u_m"), C_GraphA, ref y, null, "F0");
        graphVel = MakeGraph(root, "|Vy|", UILocale.T("u_ms"), C_GraphB, ref y, -3.5f, "F1");
        graphThr = MakeGraph(root, UILocale.T("m_thr"), UILocale.T("u_kn"), C_GraphC, ref y, null, "F0");

        crt.sizeDelta = new Vector2(0, Mathf.Max(200f, -y + 24f));
    }

    void BuildCriterionGrid(Transform root, ref float y, float pad, float inner)
    {
        // 2x2 gate badges — instant GO / NO-GO scan
        float gap = 6f;
        float cellW = (inner - gap) * 0.5f;
        float cellH = 36f;
        float row0 = y;

        txtCritV = MakeCriterionBadge(root, pad, row0, cellW, cellH, UILocale.T("crit_vy"));
        txtCritA = MakeCriterionBadge(root, pad + cellW + gap, row0, cellW, cellH, UILocale.T("crit_tilt"));
        y -= cellH + gap;
        float row1 = y;
        txtCritM = MakeCriterionBadge(root, pad, row1, cellW, cellH, UILocale.T("crit_miss"));
        txtCritH = MakeCriterionBadge(root, pad + cellW + gap, row1, cellW, cellH, UILocale.T("crit_vh"));
        y -= cellH + 6f;
    }

    TMP_Text MakeCriterionBadge(Transform parent, float x, float y, float w, float h, string title)
    {
        var bg = CreatePanel("CritBadge", parent, C_PanelSoft);
        bg.GetComponent<Image>().raycastTarget = false;
        PinTL(bg.GetComponent<RectTransform>(), x, y, w, h);

        var t = CreateText(bg.transform, title + "\n--", 11, C_Muted, FontStyles.Bold);
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = true;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.lineSpacing = -6f;
        StretchFull(t.rectTransform, 4, 3, 4, 3);
        return t;
    }

    void BuildRightPanel(Transform parent)
    {
        // Control column: pick mode → compare → setup → results (mirrors left scan style)
        const float W = 338f;
        const float pad = 12f;
        const float inner = W - pad * 2f; // 314
        const float gap = 6f;

        var panel = CreatePanel("RightPanel", parent, C_Panel);
        rightPanelGo = panel;
        DockRight(panel.GetComponent<RectTransform>(), 12, 96, 14, W);
        Outline(panel);

        var viewport = CreatePanel("Viewport", panel.transform, new Color(0, 0, 0, 0));
        viewport.GetComponent<Image>().raycastTarget = false;
        var vrt = viewport.GetComponent<RectTransform>();
        StretchFull(vrt, 0, 2, 0, 2);
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
        scroll.scrollSensitivity = 32f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.12f;

        Transform root = content.transform;
        float y = -10f;

        // ── Quick start strip (short, fully visible) ──
        Header(root, UILocale.T("h_how"), ref y, pad, inner);
        var howBg = CreatePanel("HowBg", root, C_PanelSoft);
        howBg.GetComponent<Image>().raycastTarget = false;
        PinTL(howBg.GetComponent<RectTransform>(), pad, y, inner, 40);
        txtHow = CreateText(howBg.transform, UILocale.T("how"), 12, C_Accent, FontStyles.Bold);
        txtHow.alignment = TextAlignmentOptions.Center;
        txtHow.enableWordWrapping = true;
        txtHow.overflowMode = TextOverflowModes.Overflow;
        StretchFull(txtHow.rectTransform, 10, 6, 10, 6);
        y -= 46f;

        // ── 1. Algorithm 2x2 ──
        Header(root, UILocale.T("h_step1"), ref y, pad, inner);
        modeButtons.Clear();
        modeButtonImages.Clear();
        float cellW = (inner - gap) * 0.5f;
        float cellH = 48f;
        float row0 = y;
        modeButtons.Add(ModeButtonAt(root, pad, row0, cellW, cellH,
            UILocale.T("mode_btn_a"), UILocale.T("mode_sub_a"), RocketPhysics.ControlMode.PID));
        modeButtons.Add(ModeButtonAt(root, pad + cellW + gap, row0, cellW, cellH,
            UILocale.T("mode_btn_b"), UILocale.T("mode_sub_b"), RocketPhysics.ControlMode.Fuzzy));
        y -= cellH + gap;
        float row1 = y;
        modeButtons.Add(ModeButtonAt(root, pad, row1, cellW, cellH,
            UILocale.T("mode_btn_c"), UILocale.T("mode_sub_c"), RocketPhysics.ControlMode.Neural));
        modeButtons.Add(ModeButtonAt(root, pad + cellW + gap, row1, cellW, cellH,
            UILocale.T("mode_btn_d"), UILocale.T("mode_sub_d"), RocketPhysics.ControlMode.Hybrid));
        y -= cellH + 8f;

        // ── 2. Compare (pair side-by-side) ──
        Header(root, UILocale.T("h_step2"), ref y, pad, inner);
        float btnH = 34f;
        float halfW = (inner - gap) * 0.5f;
        ActionButtonAt(root, pad, y, halfW, btnH, UILocale.T("btn_compare"),
            UiTheme.IsLightBackground ? new Color(0.18f, 0.42f, 0.68f, 1f) : C_BtnActive, OnStartCompare);
        ActionButtonAt(root, pad + halfW + gap, y, halfW, btnH, UILocale.T("btn_cancel"),
            C_Btn, OnCancelCompare);
        y -= btnH + 10f;

        // ── 3. Camera (status only — controls in top menu / hotkeys) ──
        Header(root, UILocale.T("h_cam"), ref y, pad, inner);
        var camBg = CreatePanel("CamBg", root, C_PanelSoft);
        camBg.GetComponent<Image>().raycastTarget = false;
        PinTL(camBg.GetComponent<RectTransform>(), pad, y, inner, 44);
        txtCamMode = CreateText(camBg.transform, UILocale.T("cam_prefix") + UILocale.T("cam_follow"),
            12, C_Accent, FontStyles.Bold);
        var cmRt = txtCamMode.rectTransform;
        cmRt.anchorMin = new Vector2(0, 0.45f);
        cmRt.anchorMax = new Vector2(1, 1);
        cmRt.offsetMin = new Vector2(8, 0);
        cmRt.offsetMax = new Vector2(-8, -4);
        txtCamMode.alignment = TextAlignmentOptions.BottomLeft;
        txtCamMode.overflowMode = TextOverflowModes.Ellipsis;

        txtCamHelp = CreateText(camBg.transform, UILocale.T("cam_keys"), 10, C_Muted);
        var chRt = txtCamHelp.rectTransform;
        chRt.anchorMin = new Vector2(0, 0);
        chRt.anchorMax = new Vector2(1, 0.5f);
        chRt.offsetMin = new Vector2(8, 4);
        chRt.offsetMax = new Vector2(-8, 0);
        txtCamHelp.alignment = TextAlignmentOptions.TopLeft;
        txtCamHelp.overflowMode = TextOverflowModes.Ellipsis;
        y -= 50f;

        // ── 4. Test setup ──
        Header(root, UILocale.T("h_step3"), ref y, pad, inner);
        txtTestsVal = SliderLine(root, UILocale.T("sl_tests"), UILocale.T("sl_tests_u"),
            5, 40, 15, ref y, out testsSlider, pad, inner);
        txtWindVal = SliderLine(root, UILocale.T("sl_wind"), UILocale.T("sl_wind_u"),
            0, 25, 10, ref y, out windSlider, pad, inner);
        SliderLine(root, UILocale.T("sl_time"), UILocale.T("sl_time_u"),
            1, 40, 20, ref y, out timeScaleSlider, pad, inner);
        // toggles side by side
        float togY = y;
        noiseToggle = ToggleAt(root, pad, togY, halfW, 26f, UILocale.T("tg_noise"), true);
        trainToggle = ToggleAt(root, pad + halfW + gap, togY, halfW, 26f, UILocale.T("tg_train"), true);
        y -= 32f;

        // ── 5. Comparison results 2x2 ──
        y -= 4f;
        Header(root, UILocale.T("h_results"), ref y, pad, inner);
        float statH = 40f;
        float sW = (inner - gap) * 0.5f;
        float sRow0 = y;
        txtPid = StatBadge(root, pad, sRow0, sW, statH, UILocale.T("stat_a"));
        txtFuzzy = StatBadge(root, pad + sW + gap, sRow0, sW, statH, UILocale.T("stat_b"));
        y -= statH + gap;
        float sRow1 = y;
        txtNeural = StatBadge(root, pad, sRow1, sW, statH, UILocale.T("stat_c"));
        txtHybrid = StatBadge(root, pad + sW + gap, sRow1, sW, statH, UILocale.T("stat_d"));
        y -= statH + 8f;

        var winBg = CreatePanel("WinnerBg", root, C_PanelSoft);
        winBg.GetComponent<Image>().raycastTarget = false;
        PinTL(winBg.GetComponent<RectTransform>(), pad, y, inner, 28);
        txtWinner = CreateText(winBg.transform, UILocale.T("winner_none"), 12, C_Ok, FontStyles.Bold);
        txtWinner.alignment = TextAlignmentOptions.Center;
        StretchFull(txtWinner.rectTransform, 6, 4, 6, 4);
        y -= 34f;

        // ── 6. Status / tips ──
        Header(root, UILocale.T("h_msg"), ref y, pad, inner);
        var infoBg = CreatePanel("InfoBox", root, C_PanelSoft);
        infoBg.GetComponent<Image>().raycastTarget = false;
        PinTL(infoBg.GetComponent<RectTransform>(), pad, y, inner, 64);
        txtInfo = CreateText(infoBg.transform, UILocale.T("tip"), 11, C_Muted);
        StretchFull(txtInfo.rectTransform, 8, 6, 8, 6);
        txtInfo.enableWordWrapping = true;
        txtInfo.overflowMode = TextOverflowModes.Ellipsis;
        txtInfo.alignment = TextAlignmentOptions.TopLeft;
        y -= 70f;

        crt.sizeDelta = new Vector2(0, Mathf.Max(180f, -y + 20f));
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
        if (txtTrajBtn != null)
            txtTrajBtn.text = (trajVisible ? UILocale.T("top_path_on") : UILocale.T("top_path_off")) + "  L";
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
        SetStatusVisual("st_stop", C_Amber);
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
        // Mode pill stays short — batch detail lives only on progress bar
        if (txtMode && rocket != null)
            txtMode.text = UILocale.ModeNameShort(rocket.controlMode);
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

        SetStatusVisual(ok ? "st_success" : "st_fail", ok ? C_Ok : C_Alert);
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
        rt.anchoredPosition = new Vector2(0, -92);
        rt.sizeDelta = new Vector2(560, 44);
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
                // ASCII-only separators (no middle-dot tofu)
                txtDeltaStrip.text =
                    $"dh {Arrow(dAlt)}{Mathf.Abs(dAlt):F1}  |  dVy {Arrow(dVy)}{Mathf.Abs(dVy):F2}  |  " +
                    $"dTilt {Arrow(dTilt)}{Mathf.Abs(dTilt):F2}  |  dF {Arrow(dThr)}{Mathf.Abs(dThr):F1}";
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
        // Gate badges: short title + live value vs limit (ASCII-safe)
        UpdateCriterion(txtCritV, av < maxV, UILocale.T("crit_vy"),
            $"{av:F2} / {maxV:F1}", nearGround || av >= maxV * 0.85f);
        UpdateCriterion(txtCritA, tilt < maxA, UILocale.T("crit_tilt"),
            $"{tilt:F1} / {maxA:F0}", true);
        UpdateCriterion(txtCritM, miss < maxM, UILocale.T("crit_miss"),
            $"{miss:F1} / {maxM:F0}", true);
        UpdateCriterion(txtCritH, hVel < maxH, UILocale.T("crit_vh"),
            $"{hVel:F2} / {maxH:F0}", true);

        UpdateInsight(s, av, hVel, tilt, miss, twr, fuelPct, eta, maxV, maxA, maxM, maxH);

        bool exp = sim != null && sim.IsExperimentRunning;
        if (txtMode && !exp)
            txtMode.text = UILocale.ModeNameShort(rocket.controlMode);
        if (txtTime) txtTime.text = string.Format(UILocale.T("time_fmt"), s.time);

        if (txtStatus && !resultShown)
        {
            if (exp)
                SetStatusVisual("st_batch", C_Amber);
            else if (s.simulationFinished && rocket.simulationArmed == false && rocket.metrics != null
                     && (rocket.metrics.totalFlightTime > 0.1f || rocket.metrics.isSuccessfulLanding || rocket.metrics.timedOut))
            {
                // stopped mid-flight — OnStop already set badge
            }
            else if (s.simulationFinished && rocket.metrics != null && rocket.metrics.totalFlightTime > 0.05f)
            {
                bool ok = rocket.metrics.isSuccessfulLanding;
                SetStatusVisual(ok ? "st_success" : "st_fail", ok ? C_Ok : C_Alert);
                Write(txtScore, $"{rocket.metrics.SuccessScore:F0}", ok ? C_Ok : C_Alert);
            }
            else if (rocket.simulationArmed && s.time > 0.05f)
                SetStatusVisual("st_descent", C_Cyan);
            else if (rocket.simulationArmed)
                SetStatusVisual("st_start", C_Amber);
            else
                SetStatusVisual("st_wait", C_Muted);
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

        // Slider value labels update via onValueChanged (with units)
        UpdateFlightStep(s);

        // Keep camera label in sync if user used hotkeys
        if (txtCamMode && cameraFollow != null && Time.frameCount % 15 == 0)
            RefreshCamLabel();
    }

    public void UpdateStatistics(float pid, float fuzzy, float neural, float hybrid = -1f)
    {
        WriteStatBadge(txtPid, pid);
        WriteStatBadge(txtFuzzy, fuzzy);
        WriteStatBadge(txtNeural, neural);
        if (hybrid >= 0f) WriteStatBadge(txtHybrid, hybrid);

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

    static void WriteStatBadge(TMP_Text t, float pct)
    {
        if (t == null) return;
        Color c = RateColor(pct);
        Write(t, $"{pct:F0} %", c);
        var img = t.transform.parent != null ? t.transform.parent.GetComponent<Image>() : null;
        if (img != null)
        {
            Color bg = new Color(c.r, c.g, c.b, UiTheme.IsLightBackground ? 0.18f : 0.14f);
            img.color = bg;
        }
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
        // ASCII-only (no unicode arrows → tofu on some fonts)
        if (d > 0.05f) return "+";
        if (d < -0.05f) return "-";
        return "=";
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

    /// <summary>
    /// Гарантує робочий EventSystem + UI input module.
    /// Input System package без AssignDefaultActions ламає кліки мишкою.
    /// Проєкт використовує legacy Input.* — StandaloneInputModule як надійний fallback.
    /// </summary>
    static void EnsureEventSystem()
    {
        var all = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        UnityEngine.EventSystems.EventSystem es;
        if (all == null || all.Length == 0)
        {
            var go = new GameObject("EventSystem");
            es = go.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }
        else
        {
            es = all[0];
            for (int i = 1; i < all.Length; i++)
            {
                if (all[i] != null && all[i].gameObject != es.gameObject)
                    Object.Destroy(all[i].gameObject);
            }
        }

        // DestroyImmediate: Destroy() відкладає видалення → дублікати модулів у тому ж кадрі
        var existingModules = es.GetComponents<UnityEngine.EventSystems.BaseInputModule>();
        for (int i = 0; i < existingModules.Length; i++)
        {
            if (existingModules[i] != null)
                Object.DestroyImmediate(existingModules[i]);
        }

        // Чи доступний legacy Input Manager? (Both / Old). Проєкт повністю на Input.GetKey.
        bool legacyInput =
#if ENABLE_LEGACY_INPUT_MANAGER
            true;
#elif ENABLE_INPUT_SYSTEM
            false;
#else
            true;
#endif

        bool hasWorkingModule = false;

        if (legacyInput)
        {
            // Standalone надійно клікає при Input Manager / Both
            var standalone = es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            standalone.enabled = true;
            standalone.forceModuleActive = true;
            hasWorkingModule = true;
        }
        else
        {
            // Тільки Input System — обов'язково AssignDefaultActions
            var isType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (isType != null)
            {
                var mod = es.gameObject.AddComponent(isType) as Behaviour;
                var assign = isType.GetMethod("AssignDefaultActions",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (mod != null && assign != null)
                {
                    try
                    {
                        assign.Invoke(mod, null);
                        mod.enabled = true;
                        hasWorkingModule = true;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning("[UI] AssignDefaultActions failed: " + ex.Message);
                        Object.DestroyImmediate(mod);
                    }
                }
                else if (mod != null)
                {
                    Object.DestroyImmediate(mod);
                }
            }

            // Останній шанс: Standalone (іноді ще працює)
            if (!hasWorkingModule)
            {
                var standalone = es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                standalone.enabled = true;
                standalone.forceModuleActive = true;
                hasWorkingModule = true;
            }
        }

        es.enabled = true;
        if (UnityEngine.EventSystems.EventSystem.current == null)
            UnityEngine.EventSystems.EventSystem.current = es;

        if (!hasWorkingModule)
            Debug.LogError("[UI] No UI input module available — mouse clicks will not work.");
    }

    static GameObject CreatePanel(string name, Transform parent, Color col)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        // Simple white sprite — avoids 9-slice default UI sprite thickness glitches
        if (s_uiWhite == null)
        {
            var tex = Texture2D.whiteTexture;
            s_uiWhite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
        }
        img.sprite = s_uiWhite;
        img.type = Image.Type.Simple;
        img.color = col;
        img.raycastTarget = true;
        return go;
    }

    static void Outline(GameObject go, float dist = 1.2f)
    {
        // Світлі теми: м'яка hairline (без чорних рамок). Темні: контрастний edge.
        Color e;
        float d;
        if (UiTheme.IsLightBackground)
        {
            e = new Color(0.55f, 0.62f, 0.72f, 0.42f);
            d = Mathf.Max(0.6f, dist * 0.55f);
        }
        else
        {
            e = C_Edge;
            e.a = Mathf.Clamp(e.a, 0.55f, 0.85f);
            d = dist;
        }

        var o1 = go.AddComponent<UnityEngine.UI.Outline>();
        o1.effectColor = e;
        o1.effectDistance = new Vector2(d, -d);
        o1.useGraphicAlpha = true;
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

    TMP_Text Header(Transform parent, string title, ref float y, float pad = 14f, float width = 300f)
    {
        // Title → gap → visible underline → gap (Критерії / Швидкий старт / усі секції)
        var t = CreateText(parent, title, 11, C_Accent, FontStyles.Bold);
        t.characterSpacing = 4f;
        PinTL(t.rectTransform, pad, y, width, 16);
        y -= 18f;

        // 2 px theme-tinted rule — always readable on first section too
        Color lineCol = Color.Lerp(C_Accent, UiTheme.IsLightBackground
            ? new Color(0.55f, 0.58f, 0.62f, 1f)
            : new Color(0.85f, 0.88f, 0.92f, 1f), 0.45f);
        lineCol.a = UiTheme.IsLightBackground ? 0.7f : 0.55f;

        var line = CreatePanel("HeaderLine", parent, lineCol);
        line.GetComponent<Image>().raycastTarget = false;
        PinTL(line.GetComponent<RectTransform>(), pad, y, width, 2f);
        y -= 12f;
        return t;
    }

    TMP_Text Metric(Transform parent, string key, string unit, ref float y,
        float pad = 14f, float width = 300f, bool primary = false)
    {
        // label left | value right-aligned | unit at far right
        float rowH = primary ? 24f : 22f;
        float valSize = primary ? 16f : 14f;
        float labelW = width * 0.42f;
        float unitW = 40f;
        float valW = width - labelW - unitW - 4f;

        var k = CreateText(parent, key, primary ? 12f : 11f, C_Muted);
        PinTL(k.rectTransform, pad, y, labelW, rowH);
        k.overflowMode = TextOverflowModes.Ellipsis;
        k.enableWordWrapping = false;
        metricLabels.Add(k);

        var u = CreateText(parent, unit, 11, C_Muted);
        u.alignment = TextAlignmentOptions.Left;
        PinTL(u.rectTransform, pad + width - unitW, y, unitW, rowH);
        u.overflowMode = TextOverflowModes.Overflow;

        var v = CreateText(parent, "--", valSize, C_Text, FontStyles.Bold);
        v.alignment = TextAlignmentOptions.Right;
        PinTL(v.rectTransform, pad + labelW, y, valW, rowH);
        v.overflowMode = TextOverflowModes.Overflow;
        y -= primary ? 26f : 23f;
        return v;
    }

    static void Write(TMP_Text t, string value, Color c)
    {
        if (t == null) return;
        t.text = value;
        t.color = c;
    }

    static void UpdateCriterion(TMP_Text t, bool ok, string title, string detail, bool emphasize)
    {
        if (t == null) return;
        // Two-line badge: TITLE + value/limit
        string mark = ok ? "OK" : (emphasize ? "NO" : "..");
        t.text = title + "  " + mark + "\n" + detail;
        t.color = ok ? C_Ok : (emphasize ? C_Alert : C_Amber);
        // Tint parent badge background if present
        var img = t.transform.parent != null ? t.transform.parent.GetComponent<Image>() : null;
        if (img != null)
        {
            Color baseBg = ok
                ? new Color(C_Ok.r, C_Ok.g, C_Ok.b, 0.14f)
                : emphasize
                    ? new Color(C_Alert.r, C_Alert.g, C_Alert.b, 0.16f)
                    : new Color(C_Amber.r, C_Amber.g, C_Amber.b, 0.12f);
            // Keep readable on light themes
            if (UiTheme.IsLightBackground)
                baseBg.a = Mathf.Max(baseBg.a, 0.22f);
            img.color = baseBg;
        }
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

    Image MakeBar(Transform parent, ref float y, Color fill, float pad = 12f, float width = 314f)
    {
        Color barBg = UiTheme.IsLightBackground
            ? new Color(0.78f, 0.8f, 0.84f, 1f)
            : new Color(0.05f, 0.05f, 0.06f, 1f);
        var bg = CreatePanel("Bar", parent, barBg);
        bg.GetComponent<Image>().raycastTarget = false;
        // indent bar slightly under the metric value column
        float barX = pad + 2f;
        float barW = width - 4f;
        PinTL(bg.GetComponent<RectTransform>(), barX, y, barW, 7);
        var f = CreatePanel("Fill", bg.transform, fill);
        f.GetComponent<Image>().raycastTarget = false;
        var frt = f.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = new Vector2(0.01f, 1f);
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        y -= 12f;
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
        const float gX = 12f;
        const float gW = 314f;
        const float fX = 10f;
        const float fW = 318f;

        var go = new GameObject("Graph_" + title, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(parent, false);
        PinTL(go.GetComponent<RectTransform>(), gX, y, gW, 88);
        var raw = go.GetComponent<RawImage>();
        raw.color = Color.white;
        raw.raycastTarget = false;

        // Frame follows theme panel + edge colors
        Color frameFill = Color.Lerp(C_PanelSoft, C_Panel, 0.35f);
        frameFill.a = 1f;
        Color frameEdge = C_Edge;
        frameEdge.a = UiTheme.IsLightBackground ? 0.55f : 0.7f;

        var frame = CreatePanel("GFrame", parent, frameFill);
        frame.GetComponent<Image>().raycastTarget = false;
        var frt = frame.GetComponent<RectTransform>();
        PinTL(frt, fX, y + 2, fW, 92);
        frame.transform.SetSiblingIndex(go.transform.GetSiblingIndex());

        var edge = frame.AddComponent<UnityEngine.UI.Outline>();
        edge.effectColor = frameEdge;
        edge.effectDistance = new Vector2(1.1f, -1.1f);
        edge.useGraphicAlpha = true;

        var g = go.AddComponent<TelemetryGraph>();
        g.autoScale = true;
        g.showFill = true;
        g.showZeroLine = true;
        g.valueFormat = fmt ?? "F1";
        g.Configure(title, unit, line, threshold);
        y -= 98f;
        return g;
    }

    void BuildStepBar(Transform parent)
    {
        // Same panel chrome as left/right sidebars
        stepBarGo = CreatePanel("StepBar", parent, C_Panel);
        stepBarGo.GetComponent<Image>().raycastTarget = false;
        Outline(stepBarGo, 1f);
        var rt = stepBarGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 12f);
        rt.sizeDelta = new Vector2(520f, 32f);

        txtStep = CreateText(stepBarGo.transform, UILocale.T("step_ready"), 12, C_Text, FontStyles.Bold);
        StretchFull(txtStep.rectTransform, 12, 4, 12, 4);
        txtStep.alignment = TextAlignmentOptions.Center;
        txtStep.overflowMode = TextOverflowModes.Ellipsis;
        txtStep.enableWordWrapping = false;
        txtStep.raycastTarget = false;
    }

    void UpdateFlightStep(RocketState s)
    {
        if (txtStep == null || rocket == null) return;

        if (sim != null && sim.IsExperimentRunning)
        {
            txtStep.text = UILocale.T("step_batch");
            txtStep.color = C_Amber;
            return;
        }

        float h = s.position.y;
        string key;
        Color col = C_Text;

        if (s.simulationFinished && rocket.metrics != null && rocket.metrics.totalFlightTime > 0.05f)
        {
            bool ok = rocket.metrics.isSuccessfulLanding;
            key = ok ? "step_ok" : "step_fail";
            col = ok ? C_Ok : C_Alert;
        }
        else if (!rocket.simulationArmed)
        {
            key = s.time > 0.05f ? "step_stop" : "step_ready";
            col = C_Muted;
        }
        else if (h >= 400f) { key = "step_high"; col = C_Cyan; }
        else if (h >= 100f) { key = "step_approach"; col = C_Cyan; }
        else if (h >= 25f) { key = "step_powered"; col = C_Amber; }
        else if (h >= 6f) { key = "step_terminal"; col = C_Amber; }
        else if (h >= 2f) { key = "step_soft"; col = C_Ok; }
        else { key = "step_touch"; col = C_Ok; }

        string mode = UILocale.ModeNameShort(rocket.controlMode);
        txtStep.text = mode + "  |  " + UILocale.T(key);
        txtStep.color = col;
    }

    Button ModeButtonAt(Transform parent, float x, float y, float w, float h,
        string title, string subtitle, RocketPhysics.ControlMode mode)
    {
        var go = CreatePanel("Mode_" + mode, parent, C_Btn);
        PinTL(go.GetComponent<RectTransform>(), x, y, w, h);
        var btn = go.AddComponent<Button>();
        var img = go.GetComponent<Image>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.12f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.84f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.06f;
        btn.colors = colors;
        modeButtonImages.Add(img);

        var txt = CreateText(go.transform, title, 13, C_Text, FontStyles.Bold);
        var tr = txt.rectTransform;
        tr.anchorMin = new Vector2(0, 0.42f);
        tr.anchorMax = new Vector2(1, 1);
        tr.offsetMin = new Vector2(8, 0);
        tr.offsetMax = new Vector2(-6, -3);
        txt.alignment = TextAlignmentOptions.BottomLeft;
        txt.overflowMode = TextOverflowModes.Ellipsis;
        txt.enableWordWrapping = false;
        txt.raycastTarget = false;

        if (!string.IsNullOrEmpty(subtitle))
        {
            var sub = CreateText(go.transform, subtitle, 10, C_Muted);
            var sr = sub.rectTransform;
            sr.anchorMin = new Vector2(0, 0);
            sr.anchorMax = new Vector2(1, 0.48f);
            sr.offsetMin = new Vector2(8, 4);
            sr.offsetMax = new Vector2(-6, 0);
            sub.alignment = TextAlignmentOptions.TopLeft;
            sub.overflowMode = TextOverflowModes.Ellipsis;
            sub.raycastTarget = false;
        }

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
            string nice = UILocale.ModeName(mode);
            NotifyInfo(string.Format(UILocale.T("msg_selected"), nice)
                       + "\n" + UILocale.T("ins_ideal_hint"));
        });
        return btn;
    }

    void ActionButtonAt(Transform parent, float x, float y, float w, float h,
        string label, Color col, UnityEngine.Events.UnityAction action)
    {
        Color bg = col;
        if (UiTheme.IsLightBackground)
        {
            float luma = 0.2126f * col.r + 0.7152f * col.g + 0.0722f * col.b;
            if (luma < 0.45f)
                bg = Color.Lerp(col, new Color(0.2f, 0.45f, 0.7f, 1f), 0.35f);
        }
        var go = CreatePanel("Action", parent, bg);
        PinTL(go.GetComponent<RectTransform>(), x, y, w, h);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.88f);
        btn.colors = colors;
        Color tc = UiTheme.ActionBtnText(bg);
        var txt = CreateText(go.transform, label, 11, tc, FontStyles.Bold);
        StretchFull(txt.rectTransform, 4, 2, 4, 2);
        txt.alignment = TextAlignmentOptions.Center;
        txt.overflowMode = TextOverflowModes.Ellipsis;
        txt.enableWordWrapping = false;
        txt.raycastTarget = false;
        btn.onClick.AddListener(action);
    }

    TMP_Text SliderLine(Transform parent, string label, string unit, float min, float max, float val,
        ref float y, out Slider slider, float pad = 12f, float width = 314f)
    {
        // Label + value (units)
        var k = CreateText(parent, label, 11, C_Muted);
        PinTL(k.rectTransform, pad, y, width * 0.68f, 16);
        k.overflowMode = TextOverflowModes.Ellipsis;
        k.enableWordWrapping = false;

        string unitS = string.IsNullOrEmpty(unit) ? "" : (" " + unit);
        var v = CreateText(parent, val.ToString("F0") + unitS, 12, C_Accent, FontStyles.Bold);
        v.alignment = TextAlignmentOptions.Right;
        PinTL(v.rectTransform, pad + width * 0.58f, y, width * 0.42f, 16);
        v.overflowMode = TextOverflowModes.Overflow;
        y -= 17f;

        // ONE track line only (no progress fill) — identical thickness for every slider
        const float boxH = 20f;
        const float lineH = 3f;
        const float knob = 14f;

        Color trackCol = UiTheme.IsLightBackground
            ? new Color(0.62f, 0.66f, 0.72f, 1f)
            : new Color(0.28f, 0.30f, 0.34f, 1f);
        Color handleCol = C_Amber; handleCol.a = 1f;

        var root = new GameObject("SliderRoot", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        PinTL(root.GetComponent<RectTransform>(), pad, y, width, boxH);

        var hitGo = new GameObject("Hit", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        hitGo.transform.SetParent(root.transform, false);
        var hitImg = hitGo.GetComponent<Image>();
        StyleSimpleImage(hitImg, new Color(1f, 1f, 1f, 0.001f));
        hitImg.raycastTarget = true;
        StretchFull(hitGo.GetComponent<RectTransform>(), 0, 0, 0, 0);

        slider = hitGo.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = true;
        slider.direction = Slider.Direction.LeftToRight;
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.fillRect = null;

        // Track: stretch X, FIXED pixel height via sizeDelta.y (never modified after create)
        var trackGo = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        trackGo.transform.SetParent(root.transform, false);
        var trackImg = trackGo.GetComponent<Image>();
        StyleSimpleImage(trackImg, trackCol);
        trackImg.raycastTarget = false;
        var tr = trackGo.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 0.5f);
        tr.anchorMax = new Vector2(1f, 0.5f);
        tr.pivot = new Vector2(0.5f, 0.5f);
        tr.anchoredPosition = Vector2.zero;
        tr.sizeDelta = new Vector2(0f, lineH);
        tr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lineH);

        var hAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
        hAreaGo.transform.SetParent(root.transform, false);
        var ha = hAreaGo.GetComponent<RectTransform>();
        ha.anchorMin = new Vector2(0f, 0.5f);
        ha.anchorMax = new Vector2(1f, 0.5f);
        ha.pivot = new Vector2(0.5f, 0.5f);
        ha.anchoredPosition = Vector2.zero;
        ha.sizeDelta = new Vector2(-knob, knob);

        var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handleGo.transform.SetParent(hAreaGo.transform, false);
        var hImg = handleGo.GetComponent<Image>();
        StyleSimpleImage(hImg, handleCol);
        hImg.raycastTarget = true;
        var hr = handleGo.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0f, 0.5f);
        hr.anchorMax = new Vector2(0f, 0.5f);
        hr.pivot = new Vector2(0.5f, 0.5f);
        hr.sizeDelta = new Vector2(knob, knob);

        slider.handleRect = hr;
        slider.targetGraphic = hImg;
        slider.onValueChanged.AddListener(x =>
        {
            if (v != null) v.text = Mathf.RoundToInt(x).ToString() + unitS;
        });
        slider.value = val;
        if (v != null) v.text = Mathf.RoundToInt(val).ToString() + unitS;

        y -= 24f;
        return v;
    }

    Toggle ToggleAt(Transform parent, float x, float y, float w, float h, string label, bool on)
    {
        var row = CreatePanel("ToggleRow", parent, C_PanelSoft);
        row.GetComponent<Image>().raycastTarget = true;
        PinTL(row.GetComponent<RectTransform>(), x, y, w, h);

        var box = CreatePanel("Box", row.transform, C_Btn);
        var brt = box.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
        brt.pivot = new Vector2(0, 0.5f);
        brt.anchoredPosition = new Vector2(6, 0);
        brt.sizeDelta = new Vector2(18, 18);

        var check = CreatePanel("Check", box.transform, C_Accent);
        StretchFull(check.GetComponent<RectTransform>(), 3, 3, 3, 3);

        var txt = CreateText(row.transform, label, 10, C_Text);
        var trt = txt.rectTransform;
        trt.anchorMin = new Vector2(0, 0);
        trt.anchorMax = new Vector2(1, 1);
        trt.offsetMin = new Vector2(28, 2);
        trt.offsetMax = new Vector2(-4, -2);
        txt.alignment = TextAlignmentOptions.Left;
        txt.overflowMode = TextOverflowModes.Ellipsis;
        txt.enableWordWrapping = false;
        txt.raycastTarget = false;

        var toggle = row.AddComponent<Toggle>();
        toggle.targetGraphic = box.GetComponent<Image>();
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = on;
        return toggle;
    }

    TMP_Text StatBadge(Transform parent, float x, float y, float w, float h, string name)
    {
        var bg = CreatePanel("StatBadge", parent, C_PanelSoft);
        bg.GetComponent<Image>().raycastTarget = false;
        PinTL(bg.GetComponent<RectTransform>(), x, y, w, h);

        var k = CreateText(bg.transform, name, 10, C_Muted);
        var kr = k.rectTransform;
        kr.anchorMin = new Vector2(0, 0.48f);
        kr.anchorMax = new Vector2(1, 1);
        kr.offsetMin = new Vector2(6, 0);
        kr.offsetMax = new Vector2(-6, -3);
        k.alignment = TextAlignmentOptions.BottomLeft;
        k.overflowMode = TextOverflowModes.Ellipsis;

        var v = CreateText(bg.transform, UILocale.T("stat_none"), 14, C_Text, FontStyles.Bold);
        var vr = v.rectTransform;
        vr.anchorMin = new Vector2(0, 0);
        vr.anchorMax = new Vector2(1, 0.55f);
        vr.offsetMin = new Vector2(6, 4);
        vr.offsetMax = new Vector2(-6, 0);
        v.alignment = TextAlignmentOptions.TopLeft;
        v.overflowMode = TextOverflowModes.Overflow;
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
        // Integer positions/sizes → sharper TMP under CanvasScaler
        rt.anchoredPosition = new Vector2(Mathf.Round(x), Mathf.Round(y));
        rt.sizeDelta = new Vector2(Mathf.Round(w), Mathf.Round(h));
    }

    static void StretchFull(RectTransform rt, float l, float b, float r, float t)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }
}
