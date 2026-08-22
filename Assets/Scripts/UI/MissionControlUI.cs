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

    TMP_Text txtAlt, txtVel, txtThr, txtTilt, txtFuel, txtMiss, txtMode, txtStatus, txtTime, txtScore, txtSpeed;
    TMP_Text txtHVel, txtMass, txtTwr, txtEta, txtAcc, txtRate;
    TMP_Text txtPeakVy, txtPeakTilt, txtMinH, txtDeltaStrip;
    TMP_Text txtCritV, txtCritA, txtCritM, txtCritH;
    TMP_Text txtInsight, txtFuelPct;
    TMP_Text txtPid, txtFuzzy, txtNeural, txtHybrid, txtWinner, txtInfo;
    TMP_Text txtWindVal, txtTestsVal;
    TMP_Text txtResultTitle, txtResultBody, txtResultScore, txtProgress, txtCamMode, txtCamHelp;
    TMP_Text[] resultMetricKeys;
    TMP_Text[] resultMetricVals;
    Image resultAccentBar, resultScoreBg;
    TMP_Text txtTrajBtn, txtTitle, txtHow, txtGraphHint;
    TMP_Text txtHdrTelem, txtHdrLive, txtHdrCrit, txtHdrInsight, txtHdrGraphs;
    TMP_Text txtStep;
    Button trajToggleBtn, viewToggleBtn;
    Image trajToggleImg, hideBtnImg, viewToggleImg;
    TMP_Text txtViewBtn;

    // Metric label texts (for language refresh)
    readonly List<TMP_Text> metricLabels = new();

    Slider windSlider, testsSlider, timeScaleSlider;
    Toggle noiseToggle, trainToggle;
    Image thrBarFill, fuelBarFill, tiltBarFill, statusDot, progressFill, resultPanelBg;
    GameObject resultRoot, progressRoot, canvasRoot, stepBarGo;
    GameObject leftPanelGo, rightPanelGo, topBarGo, topMenuGo;
    GameObject captionRoot; // separate canvas — no flicker on theme rebuild
    bool panelsHidden;
    TMP_Text txtHideBtn;
    TMP_Text txtLangBtn;
    TMP_Text txtThemeBtn;
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
    bool loadingSettings;
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
        if (FindAnyObjectByType<MissionControlUI>() != null) return;
        if (FindAnyObjectByType<RocketPhysics>() == null) return;
        new GameObject("MissionControlUI").AddComponent<MissionControlUI>();
    }

    void Awake()
    {
        Instance = this;
        rocket = FindAnyObjectByType<RocketPhysics>();
        sim = FindAnyObjectByType<SimulationManager>();
        cameraFollow = FindAnyObjectByType<CameraFollow>();
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
        LoadUserSettingsIntoUi();
        WireSettingsPersistence();
        built = true;
    }

    void OnApplicationQuit() => SaveUserSettingsFromUi();

    void OnDisable()
    {
        if (!rebuilding)
            SaveUserSettingsFromUi();
    }

    void OnDestroy()
    {
        SaveUserSettingsFromUi();
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
        // Recolor window caption in-place (no destroy → no flicker on − □ ×)
        ApplyCaptionTheme();
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

        // Restore session state (not from disk — live values across theme/lang rebuild)
        loadingSettings = true;
        if (windSlider) windSlider.value = windV;
        if (testsSlider) testsSlider.value = testsV;
        if (timeScaleSlider) timeScaleSlider.value = timeV;
        if (noiseToggle) noiseToggle.isOn = noiseOn;
        if (trainToggle) trainToggle.isOn = trainOn;
        loadingSettings = false;
        if (snapAlt != null && snapAlt.Length > 0) graphAlt?.RestoreSamples(snapAlt);
        if (snapVel != null && snapVel.Length > 0) graphVel?.RestoreSamples(snapVel);
        if (snapThr != null && snapThr.Length > 0) graphThr?.RestoreSamples(snapThr);
        if (infoSnap != null && txtInfo != null) txtInfo.text = infoSnap;
        panelsHidden = hide;
        ApplyPanelsVisibility();
        if (hadResult && rocket != null && rocket.metrics != null
            && rocket.metrics.totalFlightTime > 0.05f)
            ShowLandingResult(rocket.metrics);

        WireSettingsPersistence();
        built = true;
        rebuilding = false;
        RefreshCamLabel();
        UpdateTrajButtonLabel();
        if (rocket != null) UpdateFlightStep(rocket.state);
    }

    void LoadUserSettingsIntoUi()
    {
        loadingSettings = true;
        if (windSlider != null) windSlider.value = UserSettings.Wind;
        if (testsSlider != null) testsSlider.value = UserSettings.Tests;
        if (timeScaleSlider != null) timeScaleSlider.value = UserSettings.TimeScale;
        if (noiseToggle != null) noiseToggle.isOn = UserSettings.Noise;
        if (trainToggle != null) trainToggle.isOn = UserSettings.Train;

        trajVisible = UserSettings.TrajectoryVisible;
        panelsHidden = UserSettings.PanelsHidden;
        ApplyPanelsVisibility();

        var tv = EnsureTrajectoryVisualizer();
        tv?.SetVisible(trajVisible);
        UpdateTrajButtonLabel();

        if (rocket != null)
        {
            var mode = (RocketPhysics.ControlMode)UserSettings.ControlMode;
            rocket.controlMode = mode;
            SelectModeVisualOnly(mode);
        }

        ApplySettings();
        // Live clock stays x1; TimeScale pref is for Monte-Carlo burst / slider
        ApplyLiveTimeScale(1f);
        loadingSettings = false;
    }

    void SelectModeVisualOnly(RocketPhysics.ControlMode mode)
    {
        // Update mode pill + button colors without PrepareMode/reset
        if (txtMode != null)
            txtMode.text = UILocale.ModeNameShort(mode);
        for (int i = 0; i < modeButtons.Count && i < modeButtonImages.Count; i++)
        {
            var m = (RocketPhysics.ControlMode)i;
            bool active = m == mode;
            if (modeButtonImages[i] != null)
            {
                modeButtonImages[i].color = active ? C_BtnActive : C_Btn;
            }
        }
    }

    void WireSettingsPersistence()
    {
        if (windSlider != null)
        {
            windSlider.onValueChanged.RemoveListener(OnWindChanged);
            windSlider.onValueChanged.AddListener(OnWindChanged);
        }
        if (testsSlider != null)
        {
            testsSlider.onValueChanged.RemoveListener(OnTestsChanged);
            testsSlider.onValueChanged.AddListener(OnTestsChanged);
        }
        if (timeScaleSlider != null)
        {
            timeScaleSlider.onValueChanged.RemoveListener(OnTimeScaleChangedPersist);
            timeScaleSlider.onValueChanged.AddListener(OnTimeScaleChangedPersist);
        }
        if (noiseToggle != null)
        {
            noiseToggle.onValueChanged.RemoveListener(OnNoiseChanged);
            noiseToggle.onValueChanged.AddListener(OnNoiseChanged);
        }
        if (trainToggle != null)
        {
            trainToggle.onValueChanged.RemoveListener(OnTrainChanged);
            trainToggle.onValueChanged.AddListener(OnTrainChanged);
        }
    }

    void OnWindChanged(float v)
    {
        if (loadingSettings) return;
        UserSettings.Wind = v;
        UserSettings.Save();
        ApplySettings();
    }

    void OnTestsChanged(float v)
    {
        if (loadingSettings) return;
        UserSettings.Tests = Mathf.RoundToInt(v);
        UserSettings.Save();
        ApplySettings();
    }

    void OnTimeScaleChangedPersist(float v)
    {
        if (loadingSettings) return;
        UserSettings.TimeScale = v;
        UserSettings.Save();
        if (sim != null && sim.IsExperimentRunning)
            sim.experimentTimeScale = v;
        else
            ApplyLiveTimeScale(Mathf.Clamp(v, 0.25f, 8f));
        RefreshSpeedLabel();
    }

    void OnNoiseChanged(bool on)
    {
        if (loadingSettings) return;
        UserSettings.Noise = on;
        UserSettings.Save();
        ApplySettings();
    }

    void OnTrainChanged(bool on)
    {
        if (loadingSettings) return;
        UserSettings.Train = on;
        UserSettings.Save();
        ApplySettings();
    }

    void SaveUserSettingsFromUi()
    {
        if (windSlider != null) UserSettings.Wind = windSlider.value;
        if (testsSlider != null) UserSettings.Tests = Mathf.RoundToInt(testsSlider.value);
        if (timeScaleSlider != null) UserSettings.TimeScale = timeScaleSlider.value;
        if (noiseToggle != null) UserSettings.Noise = noiseToggle.isOn;
        if (trainToggle != null) UserSettings.Train = trainToggle.isOn;
        UserSettings.TrajectoryVisible = trajVisible;
        UserSettings.PanelsHidden = panelsHidden;
        if (rocket != null)
            UserSettings.ControlMode = (int)rocket.controlMode;
        UserSettings.Save();
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

        foreach (var t in FindObjectsByType<Transform>())
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

        foreach (var h in FindObjectsByType<TelemetryHUD>(FindObjectsInactive.Include))
            h.enabled = false;
    }

    void WireLegacyDashboard()
    {
        var dash = FindAnyObjectByType<ExperimentDashboard>(FindObjectsInactive.Include);
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
        // 0 = width: integer-friendlier scale on 16:9 desktop (sharper TMP)
        scaler.matchWidthOrHeight = 0f;
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
        // Leave top-right free for caption (− □ ×)
        const float capW = 46f;
        const float captionW = capW * 3f;
        r1.offsetMax = new Vector2(-(captionW + 10f), -4);

        // Brand | mode | time
        txtTitle = CreateText(row1.transform, UILocale.T("app_title"), 16, C_Accent, FontStyles.Bold);
        var trTitle = txtTitle.rectTransform;
        trTitle.anchorMin = new Vector2(0, 0);
        trTitle.anchorMax = new Vector2(0, 1);
        trTitle.pivot = new Vector2(0, 0.5f);
        trTitle.anchoredPosition = new Vector2(0, 0);
        trTitle.sizeDelta = new Vector2(108, 0);
        txtTitle.alignment = TextAlignmentOptions.MidlineLeft;
        txtTitle.overflowMode = TextOverflowModes.Ellipsis;
        txtTitle.raycastTarget = false;

        var modeBg = CreatePanel("ModePill", row1.transform, C_PanelSoft);
        modeBg.GetComponent<Image>().raycastTarget = false;
        var mrt = modeBg.GetComponent<RectTransform>();
        mrt.anchorMin = mrt.anchorMax = new Vector2(0, 0.5f);
        mrt.pivot = new Vector2(0, 0.5f);
        mrt.anchoredPosition = new Vector2(110, 0);
        mrt.sizeDelta = new Vector2(96, 26);
        txtMode = CreateText(modeBg.transform, "PID", 12, C_Amber, FontStyles.Bold);
        StretchFull(txtMode.rectTransform, 4, 2, 4, 2);
        txtMode.alignment = TextAlignmentOptions.Center;
        txtMode.overflowMode = TextOverflowModes.Overflow;
        txtMode.textWrappingMode = TextWrappingModes.NoWrap;
        txtMode.raycastTarget = false;

        txtTime = CreateText(row1.transform, string.Format(UILocale.T("time_fmt"), 0f), 12, C_Text, FontStyles.Bold);
        var trTime = txtTime.rectTransform;
        trTime.anchorMin = trTime.anchorMax = new Vector2(0, 0.5f);
        trTime.pivot = new Vector2(0, 0.5f);
        trTime.anchoredPosition = new Vector2(206, 0);
        trTime.sizeDelta = new Vector2(96, 26);
        txtTime.alignment = TextAlignmentOptions.MidlineLeft;
        txtTime.overflowMode = TextOverflowModes.Overflow;
        txtTime.raycastTarget = false;
        txtSpeed = null;

        // Caption lives on its own canvas (not destroyed with theme RebuildUi → no flicker)
        EnsureCaptionBar();
        ApplyCaptionTheme();

        // ── ROW 2 bottom: LEFT flight | RIGHT tools (Hide Theme Lang) ──
        const float chipW = 78f; // Hide / Lang (same as Start)
        const float themeW = 118f; // full theme name + " Y" without truncating
        const float gap = 5f;
        const float rightInset = 16f;
        // Only theme is wider — flight row loses ~40px total, not a full chip
        float toolsW = chipW * 2f + themeW + gap * 2f;

        var row2 = CreatePanel("Row2", chrome.transform, new Color(0, 0, 0, 0));
        row2.GetComponent<Image>().raycastTarget = false;
        var r2 = row2.GetComponent<RectTransform>();
        r2.anchorMin = new Vector2(0, 0);
        r2.anchorMax = new Vector2(1, 0.5f);
        r2.offsetMin = new Vector2(10, 6);
        r2.offsetMax = new Vector2(-(toolsW + rightInset + 12f), -2);

        var hlg = row2.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = gap;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        MenuBtn(row2.transform, (UILocale.T("top_start") + "  SP").ToUpperInvariant(), OnStartLanding, MenuBtnKind.Start, chipW);
        MenuBtn(row2.transform, (UILocale.T("top_stop") + "  ESC").ToUpperInvariant(), OnStop, MenuBtnKind.Stop, chipW);
        MenuBtn(row2.transform, (UILocale.T("top_ideal") + "  I").ToUpperInvariant(), OnApplyIdealPresets, MenuBtnKind.Normal, chipW);
        trajToggleBtn = MenuBtn(row2.transform, PathButtonLabel(), OnToggleTrajectoryLine, MenuBtnKind.Normal, chipW, out txtTrajBtn);
        trajToggleImg = trajToggleBtn != null ? trajToggleBtn.targetGraphic as Image : null;
        trajVisible = true;
        EnsureTrajectoryVisualizer()?.SetVisible(true);
        UpdateTrajButtonVisual();
        viewToggleBtn = MenuBtn(row2.transform, ViewButtonLabel(), OnFullTrajectoryView, MenuBtnKind.Normal, chipW, out txtViewBtn);
        viewToggleImg = viewToggleBtn != null ? viewToggleBtn.targetGraphic as Image : null;
        UpdateViewButtonVisual();
        MenuBtn(row2.transform, (UILocale.T("top_export") + "  E").ToUpperInvariant(), OnExportResults, MenuBtnKind.Normal, chipW);

        // Right→left: Lang [G], Theme [Y] (wider), Hide [H]
        float xR = -rightInset;
        PlaceEdgeBtn(chrome.transform, "LangBtn", EdgeLangLabel(),
            ref xR, chipW, C_Btn, () => UILocale.Toggle(), out txtLangBtn);
        xR -= gap;
        PlaceEdgeBtn(chrome.transform, "ThemeBtn", EdgeThemeLabel(),
            ref xR, themeW, C_Btn, () =>
        {
            UiTheme.Cycle();
            if (txtThemeBtn != null) txtThemeBtn.text = EdgeThemeLabel();
            NotifyInfo(UILocale.IsUK
                ? "Тема: " + UiTheme.ButtonLabelUk
                : "Theme: " + UiTheme.ButtonLabel);
        }, out txtThemeBtn);
        if (txtThemeBtn != null)
        {
            txtThemeBtn.fontSize = 11f;
            txtThemeBtn.overflowMode = TextOverflowModes.Overflow;
            txtThemeBtn.textWrappingMode = TextWrappingModes.NoWrap;
        }
        xR -= gap;
        PlaceEdgeBtn(chrome.transform, "HideBtn", HideButtonLabel(),
            ref xR, chipW, C_Btn, TogglePanels, out txtHideBtn);
        hideBtnImg = txtHideBtn != null ? txtHideBtn.transform.parent.GetComponent<Image>() : null;
        UpdateHideButtonVisual();
    }

    static string PathButtonLabel() =>
        (UILocale.T("top_path") + "  L").ToUpperInvariant();

    static string HideButtonLabel() =>
        (UILocale.T("top_hide") + "  H").ToUpperInvariant();

    static string EdgeLangLabel() =>
        (UILocale.IsUK ? "EN" : "UA") + "  G";

    static string EdgeThemeLabel()
    {
        string themeLbl = UILocale.IsUK ? UiTheme.ButtonLabelUk : UiTheme.ButtonLabel;
        if (string.IsNullOrEmpty(themeLbl)) themeLbl = UILocale.IsUK ? "ТЕМА" : "THEME";
        return themeLbl + "  Y";
    }

    /// <summary>
    /// Edge tool chip: same visual size as Start (full bottom-row height), pinned from right.
    /// xR = right edge of chip (negative from chrome right).
    /// </summary>
    void PlaceEdgeBtn(Transform chrome, string name, string label, ref float xR, float w,
        Color bg, UnityEngine.Events.UnityAction onClick, out TMP_Text labelTxt)
    {
        var go = CreatePanel(name, chrome, bg);
        var rt = go.GetComponent<RectTransform>();
        // Stretch vertically across entire bottom half of chrome (same band as Start row)
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.offsetMin = new Vector2(xR - w, 6f);
        rt.offsetMax = new Vector2(xR, -2f);
        xR -= w;

        if (onClick != null)
        {
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.12f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.88f);
            colors.fadeDuration = 0.05f;
            btn.colors = colors;
            btn.onClick.AddListener(onClick);
        }

        labelTxt = CreateText(go.transform, label ?? "", 12, UiTheme.ContrastOn(bg), FontStyles.Bold);
        StretchFull(labelTxt.rectTransform, 4, 2, 4, 2);
        labelTxt.alignment = TextAlignmentOptions.Center;
        labelTxt.overflowMode = TextOverflowModes.Overflow;
        labelTxt.textWrappingMode = TextWrappingModes.NoWrap;
        labelTxt.raycastTarget = false;
    }

    // Caption on dedicated overlay canvas (survives RebuildUi)
    Image capBarImg, capMinImg, capMaxImg, capCloseImg, capEdgeImg;
    TMP_Text capMinTxt, capMaxTxt, capCloseTxt;
    Button capMinBtn, capMaxBtn, capCloseBtn;

    void EnsureCaptionBar()
    {
        if (captionRoot != null) return;

        captionRoot = new GameObject("CaptionCanvas");
        captionRoot.transform.SetParent(transform, false);
        var canvas = captionRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // above main HUD
        UiTypography.ConfigureCanvas(canvas);
        var scaler = captionRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        captionRoot.AddComponent<GraphicRaycaster>();

        const float capW = 46f;
        const float captionW = capW * 3f;

        var bar = new GameObject("CaptionBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bar.transform.SetParent(captionRoot.transform, false);
        capBarImg = bar.GetComponent<Image>();
        StyleSimpleImage(capBarImg, Color.white);
        capBarImg.raycastTarget = false;
        var brt = bar.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(1f, 1f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(1f, 1f);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(captionW, 32f);

        var edge = new GameObject("CapEdge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        edge.transform.SetParent(bar.transform, false);
        capEdgeImg = edge.GetComponent<Image>();
        StyleSimpleImage(capEdgeImg, Color.white);
        capEdgeImg.raycastTarget = false;
        var ert = edge.GetComponent<RectTransform>();
        ert.anchorMin = new Vector2(0f, 0f);
        ert.anchorMax = new Vector2(1f, 0f);
        ert.pivot = new Vector2(0.5f, 0f);
        ert.anchoredPosition = Vector2.zero;
        ert.sizeDelta = new Vector2(0f, 1f);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 1f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        MakeCapBtn(bar.transform, "−", capW, out capMinImg, out capMinTxt, out capMinBtn,
            () => { BorderlessWindow.Minimize(); NotifyInfo(UILocale.IsUK ? "Згорнуто" : "Minimized"); });
        MakeCapBtn(bar.transform, "□", capW, out capMaxImg, out capMaxTxt, out capMaxBtn,
            () =>
            {
                BorderlessWindow.ToggleFullscreen();
                bool fs = Screen.fullScreen || Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
                NotifyInfo(fs
                    ? (UILocale.IsUK ? "Повний екран" : "Fullscreen")
                    : (UILocale.IsUK ? "Вікно" : "Windowed"));
            });
        MakeCapBtn(bar.transform, "×", capW, out capCloseImg, out capCloseTxt, out capCloseBtn, OnExitApp);

        ApplyCaptionTheme();
    }

    void MakeCapBtn(Transform parent, string glyph, float w,
        out Image img, out TMP_Text label, out Button btn, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Cap_" + glyph,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = w;
        le.minWidth = w;
        le.flexibleWidth = 1f;
        le.preferredHeight = 32f;

        img = go.GetComponent<Image>();
        StyleSimpleImage(img, Color.white);
        img.raycastTarget = true;

        btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        btn.onClick.AddListener(onClick);

        label = CreateText(go.transform, glyph, 18, Color.white, FontStyles.Bold);
        StretchFull(label.rectTransform, 0, 0, 0, 0);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }

    /// <summary>Recolor − □ × from current theme without destroying them (no flicker).</summary>
    void ApplyCaptionTheme()
    {
        if (captionRoot == null) return;

        Color bar = C_Panel; bar.a = 1f;
        Color idle = C_Btn;
        Color ink = UiTheme.ContrastOn(idle);
        Color hover = C_BtnHover.a < 0.01f ? Color.Lerp(idle, C_Accent, 0.35f) : C_BtnHover;
        Color closeBg = Color.Lerp(C_Alert, C_Btn, 0.12f); closeBg.a = 1f;
        Color closeInk = UiTheme.ContrastOn(closeBg);
        Color closeHover = Color.Lerp(closeBg, Color.white, 0.2f);

        if (capBarImg != null) capBarImg.color = bar;
        if (capEdgeImg != null) { var e = C_Edge; e.a = 0.85f; capEdgeImg.color = e; }

        void Paint(Image img, TMP_Text txt, Button btn, Color bg, Color fg, Color hi)
        {
            if (img == null || btn == null) return;
            img.color = Color.white; // ColorTint multiplies this
            var c = ColorBlock.defaultColorBlock;
            c.normalColor = bg;
            c.highlightedColor = hi;
            c.pressedColor = Color.Lerp(bg, Color.black, 0.3f);
            c.selectedColor = bg;
            c.disabledColor = new Color(bg.r, bg.g, bg.b, 0.35f);
            c.colorMultiplier = 1f;
            c.fadeDuration = 0f; // no tween flash
            btn.colors = c;
            if (txt != null) txt.color = fg;
        }

        Paint(capMinImg, capMinTxt, capMinBtn, idle, ink, hover);
        Paint(capMaxImg, capMaxTxt, capMaxBtn, idle, ink, hover);
        Paint(capCloseImg, capCloseTxt, capCloseBtn, closeBg, closeInk, closeHover);
    }

    void OnExitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void NudgeSimSpeed(float factor)
    {
        if (sim != null && sim.IsExperimentRunning)
        {
            // Monte-Carlo speed = burst multiplier via experimentTimeScale
            sim.experimentTimeScale = Mathf.Clamp(sim.experimentTimeScale * factor, 1f, 40f);
            if (timeScaleSlider) timeScaleSlider.SetValueWithoutNotify(sim.experimentTimeScale);
            RefreshSpeedLabel();
            return;
        }
        float s = Mathf.Clamp(Time.timeScale * factor, 0.25f, 8f);
        ApplyLiveTimeScale(s);
    }

    void ApplyLiveTimeScale(float s)
    {
        s = Mathf.Clamp(s, 0.25f, 8f);
        if (sim != null && sim.IsExperimentRunning) return;
        Time.timeScale = s;
        // Keep fixed step close to base (don't explode fixedDt on high scale)
        float baseDt = rocket != null && rocket.parameters != null
            ? rocket.parameters.fixedTimeStep : 0.005f;
        Time.fixedDeltaTime = baseDt;
        RefreshSpeedLabel();
    }

    void RefreshSpeedLabel()
    {
        if (txtSpeed == null) return;
        float s = (sim != null && sim.IsExperimentRunning)
            ? sim.experimentTimeScale
            : Time.timeScale;
        txtSpeed.text = $"x{s:0.#}";
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
        le.flexibleHeight = 1f; // stretch with row like Start when HLG expands height

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.1f);
        colors.pressedColor = new Color(0.88f, 0.88f, 0.9f);
        colors.fadeDuration = 0.05f;
        btn.colors = colors;

        labelTxt = CreateText(go.transform, label, 12, txtCol, FontStyles.Bold);
        StretchFull(labelTxt.rectTransform, 4, 2, 4, 2);
        labelTxt.alignment = TextAlignmentOptions.Center;
        labelTxt.overflowMode = TextOverflowModes.Ellipsis;
        labelTxt.textWrappingMode = TextWrappingModes.NoWrap;
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

        // Sim speed: , .  or  - =
        if (Input.GetKeyDown(KeyCode.Comma) || Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            NudgeSimSpeed(1f / 1.5f);
        if (Input.GetKeyDown(KeyCode.Period) || Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            NudgeSimSpeed(1.5f);
        if (Input.GetKeyDown(KeyCode.F11)) BorderlessWindow.ToggleFullscreen();
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
        UserSettings.ControlMode = (int)mode;
        UserSettings.Save();
        NotifyInfo(string.Format(UILocale.T("msg_selected"), UILocale.ModeName(mode))
                   + "\n" + UILocale.T("ins_ideal_hint"));
    }

    void TogglePanels()
    {
        panelsHidden = !panelsHidden;
        UserSettings.PanelsHidden = panelsHidden;
        UserSettings.Save();
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
            txtHideBtn.text = HideButtonLabel();
        if (txtLangBtn != null)
            txtLangBtn.text = EdgeLangLabel();
        if (txtThemeBtn != null)
            txtThemeBtn.text = EdgeThemeLabel();
        UpdateHideButtonVisual();
    }

    void UpdateHideButtonVisual()
    {
        // Highlight when panels are hidden (toggle is "on")
        Color bg = panelsHidden ? C_BtnActive : C_Btn;
        if (UiTheme.IsLightBackground && !panelsHidden)
            bg = new Color(0.88f, 0.9f, 0.93f, 1f);
        if (hideBtnImg != null)
            hideBtnImg.color = bg;
        if (txtHideBtn != null)
        {
            txtHideBtn.text = HideButtonLabel();
            txtHideBtn.color = UiTheme.ContrastOn(bg);
        }
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
        labelTxt.textWrappingMode = TextWrappingModes.NoWrap;
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
            // Same language as landing-gate badges: bold label + accent on soft tinted chip
            txtStatus.text = UILocale.T(key);
            txtStatus.color = accent;
            txtStatus.fontStyle = FontStyles.Bold;
        }
        if (statusDot != null)
        {
            // Soft fill like criterion badges: tinted PanelSoft, solid alpha for chrome
            float t = UiTheme.IsLightBackground ? 0.55f : 0.42f;
            Color c = Color.Lerp(C_PanelSoft, accent, t);
            c.a = 1f;
            statusDot.color = c;
        }
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
        PinTL(insightBg.GetComponent<RectTransform>(), pad, y, inner, 48);
        txtInsight = CreateText(insightBg.transform, UILocale.T("ins_wait"), 12, C_Text);
        txtInsight.textWrappingMode = TextWrappingModes.Normal;
        txtInsight.alignment = TextAlignmentOptions.MidlineLeft;
        txtInsight.overflowMode = TextOverflowModes.Ellipsis;
        StretchFull(txtInsight.rectTransform, 10, 6, 10, 6);
        y -= 54f;

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
        txtDeltaStrip.textWrappingMode = TextWrappingModes.Normal;
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
        y -= cellH + gap;

        // Status strip under miss/vh row — same badge family as criteria
        float statusH = 32f;
        var statusBg = CreatePanel("StatusBadge", root, C_PanelSoft);
        statusDot = statusBg.GetComponent<Image>();
        statusDot.raycastTarget = false;
        PinTL(statusBg.GetComponent<RectTransform>(), pad, y, inner, statusH);
        txtStatus = CreateText(statusBg.transform, UILocale.T("st_ready"), 12, C_Muted, FontStyles.Bold);
        txtStatus.alignment = TextAlignmentOptions.Center;
        txtStatus.overflowMode = TextOverflowModes.Ellipsis;
        txtStatus.characterSpacing = 0.8f;
        StretchFull(txtStatus.rectTransform, 8, 4, 8, 4);
        SetStatusVisual("st_ready", C_Muted);
        y -= statusH + 6f;
    }

    TMP_Text MakeCriterionBadge(Transform parent, float x, float y, float w, float h, string title)
    {
        var bg = CreatePanel("CritBadge", parent, C_PanelSoft);
        bg.GetComponent<Image>().raycastTarget = false;
        PinTL(bg.GetComponent<RectTransform>(), x, y, w, h);

        var t = CreateText(bg.transform, title + "\n--", 11, C_Muted, FontStyles.Bold);
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
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
        PinTL(howBg.GetComponent<RectTransform>(), pad, y, inner, 36);
        txtHow = CreateText(howBg.transform, UILocale.T("how"), 12, C_Accent, FontStyles.Bold);
        txtHow.alignment = TextAlignmentOptions.Center;
        txtHow.textWrappingMode = TextWrappingModes.Normal;
        txtHow.overflowMode = TextOverflowModes.Ellipsis;
        StretchFull(txtHow.rectTransform, 10, 5, 10, 5);
        y -= 42f;

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
            5, 40, UserSettings.Tests, ref y, out testsSlider, pad, inner);
        txtWindVal = SliderLine(root, UILocale.T("sl_wind"), UILocale.T("sl_wind_u"),
            0, 25, UserSettings.Wind, ref y, out windSlider, pad, inner);
        SliderLine(root, UILocale.T("sl_time"), UILocale.T("sl_time_u"),
            1, 40, UserSettings.TimeScale, ref y, out timeScaleSlider, pad, inner);
        // toggles side by side (defaults applied in LoadUserSettingsIntoUi)
        float togY = y;
        noiseToggle = ToggleAt(root, pad, togY, halfW, 26f, UILocale.T("tg_noise"), UserSettings.Noise);
        trainToggle = ToggleAt(root, pad + halfW + gap, togY, halfW, 26f, UILocale.T("tg_train"), UserSettings.Train);
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
        PinTL(infoBg.GetComponent<RectTransform>(), pad, y, inner, 52);
        txtInfo = CreateText(infoBg.transform, UILocale.T("tip"), 11, C_Muted);
        StretchFull(txtInfo.rectTransform, 10, 6, 10, 6);
        txtInfo.textWrappingMode = TextWrappingModes.Normal;
        txtInfo.overflowMode = TextOverflowModes.Ellipsis;
        txtInfo.alignment = TextAlignmentOptions.TopLeft;
        y -= 58f;

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
        rocket.batchDrivenTicks = false; // ensure FixedUpdate + trajectory run
        rocket.ResetSimulation();
        var tv = EnsureTrajectoryVisualizer();
        tv?.Clear();
        tv?.SetVisible(trajVisible);

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
        var tv = EnsureTrajectoryVisualizer();
        if (tv == null) return;
        trajVisible = !tv.IsVisible;
        tv.SetVisible(trajVisible);
        UserSettings.TrajectoryVisible = trajVisible;
        UserSettings.Save();
        UpdateTrajButtonLabel();
        NotifyInfo(trajVisible ? UILocale.T("msg_traj_on") : UILocale.T("msg_traj_off"));
    }

    void UpdateTrajButtonLabel() => UpdateTrajButtonVisual();

    void UpdateTrajButtonVisual()
    {
        var tv = FindAnyObjectByType<TrajectoryVisualizer>();
        if (tv != null) trajVisible = tv.IsVisible;

        // Fixed label; active state = accent highlight (no "ON" text)
        Color bg = trajVisible ? C_BtnActive : C_Btn;
        if (UiTheme.IsLightBackground && !trajVisible)
            bg = new Color(0.88f, 0.9f, 0.93f, 1f);

        if (trajToggleImg == null && trajToggleBtn != null)
            trajToggleImg = trajToggleBtn.targetGraphic as Image;
        if (trajToggleImg != null)
            trajToggleImg.color = bg;
        if (txtTrajBtn != null)
        {
            txtTrajBtn.text = PathButtonLabel();
            txtTrajBtn.color = UiTheme.ContrastOn(bg);
        }
    }

    static TrajectoryVisualizer EnsureTrajectoryVisualizer()
    {
        var tv = FindAnyObjectByType<TrajectoryVisualizer>();
        if (tv != null) return tv;
        var go = new GameObject("TrajectoryVisualizer");
        tv = go.AddComponent<TrajectoryVisualizer>();
        tv.rocketPhysics = FindAnyObjectByType<RocketPhysics>();
        tv.baseLineWidth = 6f;
        return tv;
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
    /// Toggle full-trajectory overview. Press again (or F) to return to follow.
    /// </summary>
    void OnFullTrajectoryView()
    {
        var cam = ResolveCamera();
        if (cam == null) return;

        bool inOverview = cam.mode == CameraFollow.ViewMode.Overview || overviewCam;
        if (inOverview)
        {
            overviewCam = false;
            cam.SetMode(CameraFollow.ViewMode.Follow);
            RefreshCamLabel();
            UpdateViewButtonVisual();
            NotifyInfo(UILocale.T("msg_cam_follow"));
            return;
        }

        overviewCam = true;
        cam.SnapToFullTrajectoryView();
        RefreshCamLabel();
        UpdateViewButtonVisual();
        NotifyInfo(UILocale.T("msg_cam_traj"));
    }

    void OnCamFollow()
    {
        overviewCam = false;
        var cam = ResolveCamera();
        if (cam != null)
        {
            cam.userOrbitLock = false;
            cam.SetMode(CameraFollow.ViewMode.Follow);
        }
        RefreshCamLabel();
        UpdateViewButtonVisual();
        NotifyInfo(UILocale.T("msg_cam_follow"));
    }

    void OnCamManual()
    {
        overviewCam = false;
        var cam = ResolveCamera();
        if (cam == null) return;
        cam.SetMode(CameraFollow.ViewMode.Manual);
        RefreshCamLabel();
        UpdateViewButtonVisual();
        NotifyInfo(UILocale.T("msg_cam_manual"));
    }

    void OnCamReset()
    {
        var cam = ResolveCamera();
        if (cam == null) return;
        // Always leave overview and restore default follow orbit
        overviewCam = false;
        cam.userOrbitLock = false;
        cam.ResetManualOrbit();
        cam.SetMode(CameraFollow.ViewMode.Follow);
        RefreshCamLabel();
        UpdateViewButtonVisual();
        NotifyInfo(UILocale.T("msg_cam_reset"));
    }

    static string ViewButtonLabel() =>
        (UILocale.T("top_view") + "  T").ToUpperInvariant();

    void UpdateViewButtonVisual()
    {
        var cam = ResolveCamera();
        bool on = overviewCam || (cam != null && cam.mode == CameraFollow.ViewMode.Overview);
        Color bg = on ? C_BtnActive : C_Btn;
        if (UiTheme.IsLightBackground && !on)
            bg = new Color(0.88f, 0.9f, 0.93f, 1f);

        if (viewToggleImg == null && viewToggleBtn != null)
            viewToggleImg = viewToggleBtn.targetGraphic as Image;
        if (viewToggleImg != null)
            viewToggleImg.color = bg;
        if (txtViewBtn != null)
        {
            txtViewBtn.text = ViewButtonLabel();
            txtViewBtn.color = UiTheme.ContrastOn(bg);
        }
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
        if (cameraFollow == null) cameraFollow = FindAnyObjectByType<CameraFollow>();
        return cameraFollow;
    }

    void RefreshCamLabel()
    {
        var cam = ResolveCamera();
        if (cam != null)
            overviewCam = cam.mode == CameraFollow.ViewMode.Overview;
        if (txtCamMode != null)
        {
            if (cam == null) txtCamMode.text = UILocale.T("cam_prefix") + "—";
            else
            {
                txtCamMode.text = UILocale.T("cam_prefix") + UILocale.CamLabel(cam.mode);
                txtCamMode.color = cam.mode == CameraFollow.ViewMode.Manual ? C_Amber : C_Cyan;
            }
        }
        UpdateViewButtonVisual();
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
        Color status = ok ? C_Ok : C_Alert;

        if (txtResultTitle)
        {
            txtResultTitle.text = ok ? UILocale.T("res_ok") : UILocale.T("res_fail");
            txtResultTitle.color = status;
        }
        if (txtResultScore)
        {
            txtResultScore.text = $"{m.SuccessScore:F0}";
            txtResultScore.color = status;
        }
        if (resultScoreBg != null)
        {
            var c = status;
            c.a = UiTheme.IsLightBackground ? 0.16f : 0.22f;
            resultScoreBg.color = c;
        }
        if (resultAccentBar != null)
        {
            var c = status;
            c.a = 0.85f;
            resultAccentBar.color = c;
        }

        // Compact metric cards (localized units)
        string ums = UILocale.T("u_ms");
        string um = UILocale.T("u_m");
        SetResultMetric(0, UILocale.T("res_m_v"),
            $"{m.touchdownVelocity:F1} {ums}", m.touchdownVelocity < maxV);
        SetResultMetric(1, UILocale.T("res_m_tilt"),
            $"{m.landingAngleError:F1}°", m.landingAngleError < maxA);
        SetResultMetric(2, UILocale.T("res_m_miss"),
            $"{m.horizontalMiss:F1} {um}", m.horizontalMiss < maxM);
        SetResultMetric(3, UILocale.T("res_m_hv"),
            $"{m.horizontalSpeed:F1} {ums}", m.horizontalSpeed < maxH);

        if (txtResultBody)
        {
            if (ok)
            {
                txtResultBody.text = string.Format(UILocale.T("res_ok_sub"),
                    m.totalFlightTime, m.fuelRemaining);
                txtResultBody.color = C_Muted;
            }
            else if (m.timedOut)
            {
                txtResultBody.text = UILocale.IsUK
                    ? "Час симуляції вичерпано"
                    : "Simulation time exhausted";
                txtResultBody.color = C_Alert;
            }
            else
            {
                txtResultBody.text = UILocale.T("res_fail_sub");
                txtResultBody.color = C_Muted;
            }
        }

        if (txtInsight != null)
        {
            txtInsight.text = ok
                ? string.Format(UILocale.T("ins_ok"), m.SuccessScore)
                : (UILocale.IsUK ? "Див. вікно результату →" : "See result dialog →");
            txtInsight.color = status;
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
                    ? Color.Lerp(C_Panel, new Color(0.82f, 0.94f, 0.88f, 1f), 0.45f)
                    : Color.Lerp(C_Panel, new Color(0.97f, 0.86f, 0.86f, 1f), 0.45f);
            else
                resultPanelBg.color = ok
                    ? Color.Lerp(C_Panel, new Color(0.09f, 0.16f, 0.13f, 1f), 0.55f)
                    : Color.Lerp(C_Panel, new Color(0.18f, 0.09f, 0.09f, 1f), 0.55f);
            var a = resultPanelBg.color; a.a = 0.98f; resultPanelBg.color = a;
        }

        SetStatusVisual(ok ? "st_success" : "st_fail", status);
        Write(txtScore, $"{m.SuccessScore:F0}", status);
    }

    void SetResultMetric(int i, string key, string value, bool pass)
    {
        if (resultMetricKeys == null || i < 0 || i >= resultMetricKeys.Length) return;
        if (resultMetricKeys[i] != null)
        {
            resultMetricKeys[i].text = key;
            resultMetricKeys[i].color = C_Muted;
        }
        if (resultMetricVals != null && resultMetricVals[i] != null)
        {
            resultMetricVals[i].text = value;
            resultMetricVals[i].color = pass ? C_Ok : C_Alert;
        }
        if (resultMetricKeys[i] == null) return;
        var chip = resultMetricKeys[i].transform.parent;
        if (chip == null) return;
        var img = chip.GetComponent<Image>();
        if (img == null) return;
        Color c = pass ? C_Ok : C_Alert;
        c.a = UiTheme.IsLightBackground ? 0.10f : 0.14f;
        img.color = c;
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

        // Compact card — content-tight, no dead air
        var card = CreatePanel("ResultCard", resultRoot.transform, C_Panel);
        resultPanelBg = card.GetComponent<Image>();
        var crt = card.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(492f, 212f);
        Outline(card, 1.5f);

        // Status accent bar
        var accent = CreatePanel("ResAccent", card.transform, new Color(C_Ok.r, C_Ok.g, C_Ok.b, 0.9f));
        resultAccentBar = accent.GetComponent<Image>();
        resultAccentBar.raycastTarget = false;
        var art = accent.GetComponent<RectTransform>();
        art.anchorMin = new Vector2(0f, 1f);
        art.anchorMax = new Vector2(1f, 1f);
        art.pivot = new Vector2(0.5f, 1f);
        art.anchoredPosition = Vector2.zero;
        art.sizeDelta = new Vector2(0f, 3f);

        // Header row: title (left) + score pill (right)
        txtResultTitle = CreateText(card.transform, UILocale.T("res_ok"), 16, C_Ok, FontStyles.Bold);
        var trtTitle = txtResultTitle.rectTransform;
        trtTitle.anchorMin = new Vector2(0f, 1f);
        trtTitle.anchorMax = new Vector2(1f, 1f);
        trtTitle.pivot = new Vector2(0f, 1f);
        trtTitle.anchoredPosition = new Vector2(18f, -14f);
        trtTitle.sizeDelta = new Vector2(-118f, 24f);
        txtResultTitle.alignment = TextAlignmentOptions.MidlineLeft;
        txtResultTitle.characterSpacing = 0.6f;
        txtResultTitle.overflowMode = TextOverflowModes.Ellipsis;

        var scorePill = CreatePanel("ScorePill", card.transform, new Color(C_Ok.r, C_Ok.g, C_Ok.b, 0.18f));
        resultScoreBg = scorePill.GetComponent<Image>();
        resultScoreBg.raycastTarget = false;
        var sprt = scorePill.GetComponent<RectTransform>();
        sprt.anchorMin = sprt.anchorMax = new Vector2(1f, 1f);
        sprt.pivot = new Vector2(1f, 1f);
        sprt.anchoredPosition = new Vector2(-14f, -11f);
        sprt.sizeDelta = new Vector2(78f, 40f);

        txtResultScore = CreateText(scorePill.transform, "—", 18, C_Ok, FontStyles.Bold);
        var srt = txtResultScore.rectTransform;
        srt.anchorMin = new Vector2(0f, 0.28f);
        srt.anchorMax = new Vector2(1f, 1f);
        srt.offsetMin = new Vector2(2f, 0f);
        srt.offsetMax = new Vector2(-2f, -2f);
        txtResultScore.alignment = TextAlignmentOptions.Center;
        txtResultScore.characterSpacing = 0.5f;

        var scoreUnit = CreateText(scorePill.transform, UILocale.T("u_score"), 9, C_Muted, FontStyles.Bold);
        var surt = scoreUnit.rectTransform;
        surt.anchorMin = new Vector2(0f, 0f);
        surt.anchorMax = new Vector2(1f, 0.36f);
        surt.offsetMin = new Vector2(2f, 2f);
        surt.offsetMax = new Vector2(-2f, 0f);
        scoreUnit.alignment = TextAlignmentOptions.Center;
        scoreUnit.raycastTarget = false;

        // One-line subtitle
        txtResultBody = CreateText(card.transform, "", 11, C_Muted);
        txtResultBody.textWrappingMode = TextWrappingModes.NoWrap;
        txtResultBody.overflowMode = TextOverflowModes.Ellipsis;
        txtResultBody.alignment = TextAlignmentOptions.MidlineLeft;
        var brt = txtResultBody.rectTransform;
        brt.anchorMin = new Vector2(0f, 1f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(0f, 1f);
        brt.anchoredPosition = new Vector2(18f, -44f);
        brt.sizeDelta = new Vector2(-36f, 16f);

        // Four metric chips in one tight row
        resultMetricKeys = new TMP_Text[4];
        resultMetricVals = new TMP_Text[4];
        string[] keyPh = {
            UILocale.T("res_m_v"), UILocale.T("res_m_tilt"),
            UILocale.T("res_m_miss"), UILocale.T("res_m_hv")
        };
        float rowChipW = 106f;
        float rowGap = 8f;
        float rowW = rowChipW * 4f + rowGap * 3f;
        float rowX0 = (492f - rowW) * 0.5f;
        for (int i = 0; i < 4; i++)
        {
            var chip = CreatePanel($"Metric_{i}", card.transform, C_PanelSoft);
            chip.GetComponent<Image>().raycastTarget = false;
            var crtChip = chip.GetComponent<RectTransform>();
            crtChip.anchorMin = crtChip.anchorMax = new Vector2(0f, 1f);
            crtChip.pivot = new Vector2(0f, 1f);
            crtChip.anchoredPosition = new Vector2(rowX0 + i * (rowChipW + rowGap), -66f);
            crtChip.sizeDelta = new Vector2(rowChipW, 56f);

            resultMetricKeys[i] = CreateText(chip.transform, keyPh[i], 10, C_Muted, FontStyles.Bold);
            var krt = resultMetricKeys[i].rectTransform;
            krt.anchorMin = new Vector2(0f, 1f);
            krt.anchorMax = new Vector2(1f, 1f);
            krt.pivot = new Vector2(0.5f, 1f);
            krt.anchoredPosition = new Vector2(0f, -8f);
            krt.sizeDelta = new Vector2(-10f, 16f);
            resultMetricKeys[i].alignment = TextAlignmentOptions.Center;

            resultMetricVals[i] = CreateText(chip.transform, "—", 15, C_Text, FontStyles.Bold);
            var vrt = resultMetricVals[i].rectTransform;
            vrt.anchorMin = new Vector2(0f, 0f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.offsetMin = new Vector2(6f, 8f);
            vrt.offsetMax = new Vector2(-6f, -24f);
            resultMetricVals[i].alignment = TextAlignmentOptions.Center;
        }

        // Single action row — three equal buttons, flush under metrics
        float btnH = 32f;
        float btnY = 12f;
        float btnGap = 8f;
        float btnW = 144f;
        float btnsW = btnW * 3f + btnGap * 2f;
        float btnX0 = (492f - btnsW) * 0.5f;

        void MakeResultBtn(string name, string label, Color bg, float x, System.Action onClick)
        {
            var go = CreatePanel(name, card.transform, bg);
            var br = go.GetComponent<RectTransform>();
            br.anchorMin = br.anchorMax = new Vector2(0f, 0f);
            br.pivot = new Vector2(0f, 0f);
            br.anchoredPosition = new Vector2(x, btnY);
            br.sizeDelta = new Vector2(btnW, btnH);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            var t = CreateText(go.transform, label, 12, UiTheme.ContrastOn(bg), FontStyles.Bold);
            StretchFull(t.rectTransform, 4, 2, 4, 2);
            t.alignment = TextAlignmentOptions.Center;
            btn.onClick.AddListener(() => onClick());
        }

        MakeResultBtn("ShowTraj", UILocale.T("btn_show_traj"), C_Btn, btnX0,
            () => { HideLandingResult(); OnFullTrajectoryView(); });
        MakeResultBtn("ExportResult", UILocale.T("btn_export_short"), C_Btn, btnX0 + btnW + btnGap,
            OnExportResults);
        MakeResultBtn("CloseResult", UILocale.T("btn_ok"), C_BtnActive, btnX0 + (btnW + btnGap) * 2f,
            HideLandingResult);

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

        string winner = "—";
        float max = -1f;
        void Consider(string name, float rate)
        {
            if (rate < 0f) return;
            if (rate > max + 1e-4f) { max = rate; winner = name; }
        }
        Consider(UILocale.T("mode_pid"), pid);
        Consider(UILocale.T("mode_fuzzy"), fuzzy);
        Consider(UILocale.T("mode_neural"), neural);
        Consider(UILocale.T("mode_hybrid"), hybrid);
        if (max < 0f) max = 0f;

        if (txtWinner)
        {
            if (max <= 0.05f)
            {
                txtWinner.text = UILocale.T("winner_none");
                txtWinner.color = C_Muted;
            }
            else
            {
                txtWinner.text = string.Format(UILocale.T("winner_fmt"), winner, max);
                txtWinner.color = C_Ok;
            }
        }
        if (txtInfo)
        {
            txtInfo.text = max <= 0.05f
                ? UILocale.T("msg_compare_zero")
                : string.Format(UILocale.T("msg_compare_done"), winner, max);
        }
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
        var all = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
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
        // Section label + hairline — tight, consistent rhythm
        var t = CreateText(parent, title, 10, C_Accent, FontStyles.Bold);
        t.characterSpacing = 3.2f;
        PinTL(t.rectTransform, pad, y, width, 15);
        y -= 16f;

        Color lineCol = Color.Lerp(C_Accent, UiTheme.IsLightBackground
            ? new Color(0.55f, 0.58f, 0.62f, 1f)
            : new Color(0.85f, 0.88f, 0.92f, 1f), 0.42f);
        lineCol.a = UiTheme.IsLightBackground ? 0.65f : 0.48f;

        var line = CreatePanel("HeaderLine", parent, lineCol);
        line.GetComponent<Image>().raycastTarget = false;
        PinTL(line.GetComponent<RectTransform>(), pad, y, width, 1.5f);
        y -= 10f;
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
        k.textWrappingMode = TextWrappingModes.NoWrap;
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
        const float fX = 10f;
        const float fW = 318f;
        const float gH = 100f;

        // Root holds frame + plot + labels (labels last = on top)
        var root = CreatePanel("GraphRoot_" + title, parent, new Color(0, 0, 0, 0));
        root.GetComponent<Image>().raycastTarget = false;
        var rootRt = root.GetComponent<RectTransform>();
        PinTL(rootRt, fX, y, fW, gH + 4f);

        Color frameFill = Color.Lerp(C_PanelSoft, C_Panel, 0.35f);
        frameFill.a = 1f;
        Color frameEdge = C_Edge;
        frameEdge.a = UiTheme.IsLightBackground ? 0.5f : 0.65f;

        var frame = CreatePanel("GFrame", root.transform, frameFill);
        frame.GetComponent<Image>().raycastTarget = false;
        StretchFull(frame.GetComponent<RectTransform>(), 0, 0, 0, 0);
        var edge = frame.AddComponent<UnityEngine.UI.Outline>();
        edge.effectColor = frameEdge;
        edge.effectDistance = new Vector2(1f, -1f);
        edge.useGraphicAlpha = true;

        var plotGo = new GameObject("Plot", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        plotGo.transform.SetParent(root.transform, false);
        var plotRt = plotGo.GetComponent<RectTransform>();
        StretchFull(plotRt, 2, 2, 2, 2);
        var raw = plotGo.GetComponent<RawImage>();
        raw.color = Color.white;
        raw.raycastTarget = false;

        var g = plotGo.AddComponent<TelemetryGraph>();
        g.autoScale = true;
        g.showFill = true;
        g.showZeroLine = true;
        g.valueFormat = fmt ?? "F1";
        g.BindLabelRoot(rootRt); // labels as siblings of plot, on top
        g.Configure(title, unit, line, threshold);

        y -= gH + 10f;
        return g;
    }

    void BuildStepBar(Transform parent)
    {
        // Floating phase strip — matches side panel chrome, left accent stripe
        stepBarGo = CreatePanel("StepBar", parent, C_Panel);
        stepBarGo.GetComponent<Image>().raycastTarget = false;
        Outline(stepBarGo, 1f);
        var rt = stepBarGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 14f);
        rt.sizeDelta = new Vector2(540f, 34f);

        var stripe = CreatePanel("StepAccent", stepBarGo.transform, C_Accent);
        stripe.GetComponent<Image>().raycastTarget = false;
        var srt = stripe.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 0f);
        srt.anchorMax = new Vector2(0f, 1f);
        srt.pivot = new Vector2(0f, 0.5f);
        srt.anchoredPosition = Vector2.zero;
        srt.sizeDelta = new Vector2(3f, 0f);

        txtStep = CreateText(stepBarGo.transform, UILocale.T("step_ready"), 12, C_Text, FontStyles.Bold);
        StretchFull(txtStep.rectTransform, 16, 5, 14, 5);
        txtStep.alignment = TextAlignmentOptions.Center;
        txtStep.overflowMode = TextOverflowModes.Ellipsis;
        txtStep.textWrappingMode = TextWrappingModes.NoWrap;
        txtStep.raycastTarget = false;
    }

    void UpdateFlightStep(RocketState s)
    {
        if (txtStep == null || rocket == null) return;

        void PaintStep(string key, Color col)
        {
            txtStep.text = UILocale.T(key);
            txtStep.color = col;
            var stripe = stepBarGo != null ? stepBarGo.transform.Find("StepAccent") : null;
            if (stripe != null)
            {
                var img = stripe.GetComponent<Image>();
                if (img != null)
                {
                    var c = col;
                    c.a = 0.9f;
                    img.color = c;
                }
            }
        }

        if (sim != null && sim.IsExperimentRunning)
        {
            PaintStep("step_batch", C_Amber);
            return;
        }

        float h = s.position.y;
        string key;
        Color col = C_Text;

        if (s.simulationFinished && rocket.metrics != null && rocket.metrics.totalFlightTime > 0.05f)
        {
            bool ok = rocket.metrics.isSuccessfulLanding;
            PaintStep(ok ? "step_ok" : "step_fail", ok ? C_Ok : C_Alert);
            return;
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
        var stripeTf = stepBarGo != null ? stepBarGo.transform.Find("StepAccent") : null;
        if (stripeTf != null)
        {
            var img = stripeTf.GetComponent<Image>();
            if (img != null)
            {
                var c = col;
                c.a = 0.9f;
                img.color = c;
            }
        }
    }

    Button ModeButtonAt(Transform parent, float x, float y, float w, float h,
        string title, string subtitle, RocketPhysics.ControlMode mode)
    {
        title = (title ?? "").ToUpperInvariant();
        subtitle = (subtitle ?? "").ToUpperInvariant();
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
        txt.textWrappingMode = TextWrappingModes.NoWrap;
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
        txt.textWrappingMode = TextWrappingModes.NoWrap;
        txt.raycastTarget = false;
        btn.onClick.AddListener(action);
    }

    TMP_Text SliderLine(Transform parent, string label, string unit, float min, float max, float val,
        ref float y, out Slider slider, float pad = 12f, float width = 314f)
    {
        // Fixed geometry — identical for every slider (label+value+track in one block)
        const float blockH = 44f;
        const float labelH = 16f;
        const float trackH = 4f;
        const float knob = 12f;
        const float trackPadX = 6f;

        Color trackCol = Color.Lerp(C_Edge, C_PanelSoft, UiTheme.IsLightBackground ? 0.25f : 0.4f);
        trackCol.a = 1f;
        Color fillCol = C_Accent; fillCol.a = 1f;
        Color handleCol = C_Amber; handleCol.a = 1f;
        Color labelCol = C_Text; labelCol.a = 0.92f;

        string unitS = string.IsNullOrEmpty(unit) ? "" : (" " + unit);

        // ── Block container ──
        var block = CreatePanel("SliderBlock", parent, C_PanelSoft);
        block.GetComponent<Image>().raycastTarget = false;
        PinTL(block.GetComponent<RectTransform>(), pad, y, width, blockH);

        // Label (left) — inside block so it never "vanishes" under siblings
        var k = CreateText(block.transform, label ?? "", 12, labelCol);
        k.raycastTarget = false;
        k.overflowMode = TextOverflowModes.Ellipsis;
        k.textWrappingMode = TextWrappingModes.NoWrap;
        var krt = k.rectTransform;
        krt.anchorMin = new Vector2(0f, 1f);
        krt.anchorMax = new Vector2(1f, 1f);
        krt.pivot = new Vector2(0f, 1f);
        krt.anchoredPosition = new Vector2(8f, -4f);
        krt.sizeDelta = new Vector2(-100f, labelH); // leave room for value

        // Value (right)
        var v = CreateText(block.transform, Mathf.RoundToInt(val) + unitS, 12, C_Accent, FontStyles.Bold);
        v.raycastTarget = false;
        v.alignment = TextAlignmentOptions.Right;
        v.overflowMode = TextOverflowModes.Overflow;
        v.textWrappingMode = TextWrappingModes.NoWrap;
        var vrt = v.rectTransform;
        vrt.anchorMin = new Vector2(1f, 1f);
        vrt.anchorMax = new Vector2(1f, 1f);
        vrt.pivot = new Vector2(1f, 1f);
        vrt.anchoredPosition = new Vector2(-8f, -4f);
        vrt.sizeDelta = new Vector2(88f, labelH);

        // ── Slider hit area (lower half of block) ──
        var slideGo = new GameObject("Slider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        slideGo.transform.SetParent(block.transform, false);
        var srt = slideGo.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 0f);
        srt.anchorMax = new Vector2(1f, 0f);
        srt.pivot = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(0f, 2f);
        srt.sizeDelta = new Vector2(0f, 24f);
        var slideImg = slideGo.GetComponent<Image>();
        StyleSimpleImage(slideImg, new Color(0f, 0f, 0f, 0.001f));
        slideImg.raycastTarget = true;

        slider = slideGo.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = true;
        slider.direction = Slider.Direction.LeftToRight;
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slideGo.AddComponent<SliderScrollLock>();

        // Background track (fixed height via center anchors + sizeDelta.y)
        var bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGo.transform.SetParent(slideGo.transform, false);
        StyleSimpleImage(bgGo.GetComponent<Image>(), trackCol);
        bgGo.GetComponent<Image>().raycastTarget = false;
        var bgr = bgGo.GetComponent<RectTransform>();
        bgr.anchorMin = new Vector2(0f, 0.5f);
        bgr.anchorMax = new Vector2(1f, 0.5f);
        bgr.pivot = new Vector2(0.5f, 0.5f);
        bgr.anchoredPosition = Vector2.zero;
        bgr.sizeDelta = new Vector2(-trackPadX * 2f, trackH);

        // Fill Area — standard Unity layout (height locked)
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(slideGo.transform, false);
        var far = fillArea.GetComponent<RectTransform>();
        far.anchorMin = new Vector2(0f, 0.5f);
        far.anchorMax = new Vector2(1f, 0.5f);
        far.pivot = new Vector2(0.5f, 0.5f);
        far.anchoredPosition = Vector2.zero;
        far.sizeDelta = new Vector2(-trackPadX * 2f - knob, trackH);

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGo.transform.SetParent(fillArea.transform, false);
        StyleSimpleImage(fillGo.GetComponent<Image>(), fillCol);
        fillGo.GetComponent<Image>().raycastTarget = false;
        var fr = fillGo.GetComponent<RectTransform>();
        // Unity Slider drives anchorMax.x; keep y anchors full of fill area
        fr.anchorMin = new Vector2(0f, 0f);
        fr.anchorMax = new Vector2(0f, 1f);
        fr.offsetMin = Vector2.zero;
        fr.offsetMax = Vector2.zero;
        fr.pivot = new Vector2(0f, 0.5f);

        // Handle Slide Area
        var hArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        hArea.transform.SetParent(slideGo.transform, false);
        var har = hArea.GetComponent<RectTransform>();
        har.anchorMin = new Vector2(0f, 0f);
        har.anchorMax = new Vector2(1f, 1f);
        har.offsetMin = new Vector2(trackPadX + knob * 0.5f, 0f);
        har.offsetMax = new Vector2(-(trackPadX + knob * 0.5f), 0f);

        var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handleGo.transform.SetParent(hArea.transform, false);
        var hImg = handleGo.GetComponent<Image>();
        StyleSimpleImage(hImg, handleCol);
        hImg.raycastTarget = true;
        var hr = handleGo.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0f, 0.5f);
        hr.anchorMax = new Vector2(0f, 0.5f);
        hr.pivot = new Vector2(0.5f, 0.5f);
        hr.sizeDelta = new Vector2(knob, knob);

        slider.fillRect = fr;
        slider.handleRect = hr;
        slider.targetGraphic = hImg;

        // Re-lock handle size after Slider mutates anchors on first Set
        void LockHandle()
        {
            if (hr == null) return;
            // Unity sets handle anchors to (n,0)-(n,1); keep equal size via sizeDelta
            hr.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, knob);
            hr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, knob);
        }

        slider.onValueChanged.AddListener(x =>
        {
            if (v != null) v.text = Mathf.RoundToInt(x) + unitS;
            LockHandle();
        });
        slider.SetValueWithoutNotify(val);
        slider.onValueChanged.Invoke(val); // refresh value text + lock
        // Force correct fill/handle layout once
        Canvas.ForceUpdateCanvases();
        LockHandle();

        y -= blockH + 6f;
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
        txt.textWrappingMode = TextWrappingModes.NoWrap;
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

/// <summary>
/// While dragging a Slider inside a ScrollRect, disable the scroll so the handle moves.
/// </summary>
public class SliderScrollLock : MonoBehaviour,
    UnityEngine.EventSystems.IPointerDownHandler,
    UnityEngine.EventSystems.IPointerUpHandler,
    UnityEngine.EventSystems.IBeginDragHandler,
    UnityEngine.EventSystems.IEndDragHandler,
    UnityEngine.EventSystems.IDragHandler
{
    ScrollRect scroll;
    bool locked;

    void Awake() => scroll = GetComponentInParent<ScrollRect>();

    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData) => Lock();
    public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData) => Lock();
    public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData) { }
    public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData) => Unlock();
    public void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData) => Unlock();
    void OnDisable() => Unlock();

    void Lock()
    {
        if (scroll == null || locked) return;
        scroll.StopMovement();
        scroll.enabled = false;
        locked = true;
    }

    void Unlock()
    {
        if (!locked) return;
        if (scroll != null) scroll.enabled = true;
        locked = false;
    }
}
