using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
/// <summary>
/// Основний runtime HUD: UA/EN, 8 тем, GATE, телеметрія, Monte-Carlo, модалка результату.
/// Rebuild при зміні теми/мови зберігає samples графіків і setup-стан.
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
    TMP_Text txtResultTitle, txtResultBody, txtResultScore, txtProgress, txtCamMode, txtCamHelp;
    TMP_Text[] resultMetricKeys;
    TMP_Text[] resultMetricVals;
    Image resultAccentBar, resultScoreBg;
    TMP_Text txtTrajBtn, txtTitle, txtGraphHint;
    TMP_Text txtHdrTelem, txtHdrLive, txtHdrCrit, txtHdrInsight, txtHdrGraphs;
    TMP_Text txtStep;
    Button trajToggleBtn, viewToggleBtn, pauseBtn;
    Image trajToggleImg, hideBtnImg, viewToggleImg, pauseBtnImg;
    TMP_Text txtViewBtn, txtPauseBtn;

    // Тексти підписів метрик (для оновлення мови)
    readonly List<TMP_Text> metricLabels = new();

    Slider windSlider, testsSlider, timeScaleSlider, liveSpeedSlider, seedSlider;
    Slider heightSlider, descentSlider, tilt0Slider, massNoiseSlider, angleNoiseSlider;
    Toggle noiseToggle, trainToggle, residualToggle;
    Image thrBarFill, fuelBarFill, tiltBarFill, statusDot, progressFill, resultPanelBg;
    GameObject resultRoot, progressRoot, canvasRoot, stepBarGo, helpRoot;
    GameObject leftPanelGo, rightPanelGo, topBarGo, topMenuGo;
    GameObject captionRoot; // окремий canvas — без мерехтіння при rebuild теми
    /// <summary>Переживає RebuildUi — слайдери/інпути умов ніколи не знищуються → без миготіння NumField.</summary>
    GameObject conditionSectionGo;
    float conditionSectionHeight;
    const int kSliderLook = 4;
    int conditionLookVer;
    readonly List<(TMP_Text label, TMP_Text unit, string labelKey, string unitKey)> conditionLabelBindings = new();
    readonly List<(Toggle toggle, TMP_Text label, string key)> conditionToggleBindings = new();
    bool panelsHidden;
    bool helpVisible;
    Coroutine defenseDemoCo;
    TMP_Text txtSeedVal;
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
    /// <summary>Вторинні підписи (результати, підказки) — чорнило теми, сильніше за muted.</summary>
    static Color C_Secondary
    {
        get
        {
            // Підтягнути muted до body text, щоб сірий не вимивався на paper чи темних панелях
            if (UiTheme.IsLightBackground)
                return Color.Lerp(C_Text, C_Muted, 0.22f);
            return Color.Lerp(C_Text, C_Muted, 0.38f);
        }
    }
    static Color C_Btn => UiTypography.Btn;
    static Color C_BtnActive => UiTypography.BtnActive;
    static Color C_BtnHover => UiTheme.Current.BtnHover;
    static Color C_GraphA => UiTheme.Current.GraphA;
    static Color C_GraphB => UiTheme.Current.GraphB;
    static Color C_GraphC => UiTheme.Current.GraphC;

    /// <summary>
    /// Заголовки секцій: текст теми (не neon-акцент — уникає постійного зеленого на Green-темі).
    /// </summary>
    static Color C_Header
    {
        get
        {
            // М’яке змішування text + accent, щоб заголовки стежили за темою без вигляду «завжди зелені»
            Color t = C_Text;
            Color a = C_Accent;
            // Більша вага тексту, щоб neon Green/Cyan не домінував у підписах
            return Color.Lerp(t, a, UiTheme.IsLightBackground ? 0.22f : 0.28f);
        }
    }

    static Color HeaderLineColor
    {
        get
        {
            Color lineCol = Color.Lerp(C_Header, UiTheme.IsLightBackground
                ? new Color(0.5f, 0.54f, 0.6f, 1f)
                : new Color(0.85f, 0.88f, 0.92f, 1f), 0.4f);
            lineCol.a = UiTheme.IsLightBackground ? 0.7f : 0.5f;
            return lineCol;
        }
    }

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
        // Перебудувати chrome/left/labels, але condition NumFields лишаються живими (stashed)
        StartCoroutine(RebuildUiSmooth());
    }

    void OnThemeChanged()
    {
        if (!built || rebuilding) return;
        ApplyCaptionTheme();
        ApplyThemeInPlace();
    }

    System.Collections.IEnumerator RebuildUiSmooth()
    {
        if (rebuilding) yield break;
        rebuilding = true;
        // Тримати canvas видимим — поля умов не знищуються, перебудовується лише chrome
        RebuildUiCore();
        Canvas.ForceUpdateCanvases();
        yield return null;
        rebuilding = false;
    }

    void RebuildUi()
    {
        if (rebuilding) return;
        rebuilding = true;
        RebuildUiCore();
        rebuilding = false;
    }

    void RebuildUiCore()
    {
        built = false;

        float[] snapAlt = graphAlt != null ? graphAlt.GetSamples() : null;
        float[] snapVel = graphVel != null ? graphVel.GetSamples() : null;
        float[] snapThr = graphThr != null ? graphThr.GetSamples() : null;
        // Значення слайдерів живуть у conditionSectionGo — зберегти, від’єднавши секцію
        DetachConditionSection();

        bool hide = panelsHidden;
        bool helpWas = helpVisible;
        bool hadResult = resultShown;
        string infoSnap = txtInfo != null ? txtInfo.text : null;

        modeButtons.Clear();
        modeButtonImages.Clear();
        metricLabels.Clear();
        if (canvasRoot != null) Destroy(canvasRoot);
        // conditionSectionGo прив’язано до цього MonoBehaviour — переживає Destroy(canvasRoot)
        Build();
        WireLegacyDashboard();

        // Повторно застосувати тему до reused condition controls + решти нового chrome
        ApplyThemeInPlace();
        RefreshConditionLabels();

        loadingSettings = false;
        SetHelpVisible(helpWas);
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
        RefreshCamLabel();
        RefreshSpeedLabel();
        UpdateTrajButtonLabel();
        if (rocket != null) UpdateFlightStep(rocket.state);
    }

    void DetachConditionSection()
    {
        if (conditionSectionGo == null) return;
        // Припаркувати під об’єкт MissionControlUI, щоб destroy canvas не стер інпути
        conditionSectionGo.transform.SetParent(transform, false);
        conditionSectionGo.SetActive(false);
    }

    void RefreshConditionLabels()
    {
        for (int i = 0; i < conditionLabelBindings.Count; i++)
        {
            var b = conditionLabelBindings[i];
            if (b.label != null && !string.IsNullOrEmpty(b.labelKey))
                b.label.text = UILocale.T(b.labelKey);
            if (b.unit != null && !string.IsNullOrEmpty(b.unitKey))
                b.unit.text = UILocale.T(b.unitKey);
            // У заголовків немає binding одиниць — той самий колір, що в інших section titles
            if (b.label != null)
                b.label.color = b.unit == null ? C_Header : C_Text;
            if (b.unit != null) b.unit.color = C_Muted;
        }
        for (int i = 0; i < conditionToggleBindings.Count; i++)
        {
            var b = conditionToggleBindings[i];
            if (b.label != null && !string.IsNullOrEmpty(b.key))
            {
                b.label.text = UILocale.T(b.key);
                b.label.color = C_Text;
            }
        }
    }

    void SyncAllConditionFieldsFromSliders()
    {
        void Sync(Slider s)
        {
            if (s == null) return;
            var input = s.transform.parent != null
                ? s.transform.parent.GetComponentInChildren<TMP_InputField>(true)
                : null;
            if (input == null) return;
            input.SetTextWithoutNotify(Mathf.RoundToInt(s.value).ToString());
        }
        Sync(windSlider); Sync(testsSlider); Sync(timeScaleSlider); Sync(liveSpeedSlider);
        Sync(seedSlider); Sync(heightSlider); Sync(descentSlider); Sync(tilt0Slider);
        Sync(massNoiseSlider); Sync(angleNoiseSlider);
    }

    /// <summary>
    /// Перефарбувати наявний HUD без Destroy/Build — зберігає екземпляри TMP_InputField
    /// щоб поля умов не миготіли при зміні теми.
    /// </summary>
    void ApplyThemeInPlace()
    {
        if (canvasRoot == null) return;

        foreach (var img in canvasRoot.GetComponentsInChildren<Image>(true))
        {
            if (img == null) continue;
            string n = img.gameObject.name;

            // Пропускати повністю прозорі hit boxes
            if (img.color.a < 0.01f && (n == "Row2" || n == "Row1" || n == "Viewport"
                || n == "Content" || n == "LViewport" || n == "LContent"
                || n.StartsWith("GraphRoot")))
                continue;

            if (img.GetComponent<TMP_InputField>() != null)
            {
                RefreshNumFieldTheme(img);
                continue;
            }

            var sld = img.GetComponentInParent<Slider>();
            if (sld != null)
            {
                if (n == "Fill") { var c = C_Accent; c.a = 1f; img.color = c; continue; }
                if (n == "Handle") { var c = C_Amber; c.a = 1f; img.color = c; continue; }
                if (n == "Background")
                {
                    Color track = Color.Lerp(C_Edge, C_PanelSoft, UiTheme.IsLightBackground ? 0.25f : 0.4f);
                    track.a = 1f;
                    img.color = track;
                    continue;
                }
            }

            switch (n)
            {
                case "LeftPanel":
                case "RightPanel":
                case "TopChrome":
                case "ResultCard":
                case "StepBar":
                    img.color = C_Panel;
                    break;
                case "HelpCard":
                    {
                        var hc = C_Panel; hc.a = 1f; img.color = hc;
                    }
                    break;
                case "SliderBlock":
                case "ToggleRow":
                case "CritBadge":
                case "StatusBadge":
                case "InsightBg":
                case "WinnerBg":
                case "CamBg":
                case "InfoBox":
                case "ModePill":
                case "GFrame":
                case "StatBadge":
                case "ScorePill":
                    img.color = n == "ScorePill"
                        ? new Color(C_Ok.r, C_Ok.g, C_Ok.b, 0.18f)
                        : C_PanelSoft;
                    break;
                case "TopAccent":
                case "StepAccent":
                case "HeaderLine":
                case "ResAccent":
                    {
                        if (n == "ResAccent")
                            img.color = new Color(C_Ok.r, C_Ok.g, C_Ok.b, 0.9f);
                        else if (n == "StepAccent")
                            img.color = C_Accent;
                        else if (n == "HeaderLine")
                            img.color = HeaderLineColor;
                        else
                        {
                            var e = C_Edge; e.a = 0.5f;
                            img.color = e;
                        }
                    }
                    break;
                case "PFill":
                    img.color = C_Cyan;
                    break;
                case "ProgressRoot":
                    img.color = UiTheme.DarkChrome;
                    break;
                case "ResultOverlay":
                    img.color = UiTheme.ModalScrim;
                    break;
                case "HelpOverlay":
                    img.color = HelpScrimColor();
                    break;
                case "Box":
                    img.color = C_Btn;
                    break;
                case "Check":
                    img.color = C_Accent;
                    break;
                default:
                    if (n.StartsWith("Mode_"))
                        img.color = C_Btn;
                    else if (n == "MBtn" || n == "Action" || n.EndsWith("Btn")
                             || n == "LangBtn" || n == "ThemeBtn" || n == "HideBtn")
                    {
                        // Базова заливка chrome/action — спеціалізовані нижче
                        if (img.GetComponent<Button>() != null)
                            img.color = C_Btn;
                    }
                    else if (n == "Bar")
                    {
                        img.color = UiTheme.IsLightBackground
                            ? new Color(0.78f, 0.8f, 0.84f, 1f)
                            : new Color(0.05f, 0.05f, 0.06f, 1f);
                    }
                    break;
            }
        }

        // Також темизувати секцію умов (може бути «припаркована» під цим transform)
        if (conditionSectionGo != null)
        {
            foreach (var img in conditionSectionGo.GetComponentsInChildren<Image>(true))
            {
                if (img == null) continue;
                string n = img.gameObject.name;
                if (img.GetComponent<TMP_InputField>() != null) { RefreshNumFieldTheme(img); continue; }
                var sld = img.GetComponentInParent<Slider>();
                if (sld != null)
                {
                    if (n == "Fill") { var c = C_Accent; c.a = 1f; img.color = c; continue; }
                    if (n == "Handle") { var c = C_Amber; c.a = 1f; img.color = c; continue; }
                    if (n == "Background")
                    {
                        Color track = Color.Lerp(C_Edge, C_PanelSoft, UiTheme.IsLightBackground ? 0.25f : 0.4f);
                        track.a = 1f; img.color = track; continue;
                    }
                }
                if (n is "SliderBlock" or "ToggleRow") img.color = C_PanelSoft;
                else if (n == "Box") img.color = C_Btn;
                else if (n == "Check") img.color = C_Accent;
                else if (n == "HeaderLine")
                    img.color = HeaderLineColor;
                else if (n == "SecHdr")
                {
                    // текст — TMP, не Image
                }
            }
            foreach (var tmp in conditionSectionGo.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp != null && tmp.gameObject.name == "SecHdr")
                    tmp.color = C_Header;
            }
        }

        // Контури панелей
        void StyleOutlines(Transform root)
        {
            if (root == null) return;
            foreach (var outline in root.GetComponentsInChildren<UnityEngine.UI.Outline>(true))
            {
                if (outline == null) continue;
                if (outline.GetComponent<TMP_InputField>() != null) continue;
                if (outline.gameObject.name == "Handle") continue;
                if (UiTheme.IsLightBackground)
                {
                    outline.effectColor = new Color(0.55f, 0.62f, 0.72f, 0.42f);
                    outline.effectDistance = new Vector2(0.7f, -0.7f);
                }
                else
                {
                    var e = C_Edge;
                    e.a = Mathf.Clamp(e.a, 0.55f, 0.85f);
                    outline.effectColor = e;
                }
            }
        }
        StyleOutlines(canvasRoot.transform);
        if (conditionSectionGo != null) StyleOutlines(conditionSectionGo.transform);

        // —— Кнопки: суцільні заливки + завжди читабельні підписи ——
        void PaintButton(Button btn, Color bg)
        {
            if (btn == null) return;
            bg.a = 1f;
            var img = btn.targetGraphic as Image;
            if (img != null) img.color = bg;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            if (UiTheme.IsLightBackground)
            {
                // Light-теми: піднімати на hover/press — ніколи не давити в темно-сірий
                cb.highlightedColor = new Color(1.06f, 1.07f, 1.1f, 1f);
                cb.pressedColor = new Color(0.94f, 0.96f, 1f, 1f);
                cb.selectedColor = new Color(1.04f, 1.05f, 1.08f, 1f);
            }
            else
            {
                cb.highlightedColor = new Color(1.12f, 1.12f, 1.14f, 1f);
                cb.pressedColor = new Color(0.85f, 0.85f, 0.88f, 1f);
                cb.selectedColor = Color.white;
            }
            btn.colors = cb;
            // Title = контраст на заливці; ModeSub = вторинний читабельний рядок
            var tmps = btn.GetComponentsInChildren<TMP_Text>(true);
            for (int ti = 0; ti < tmps.Length; ti++)
            {
                var tmp = tmps[ti];
                if (tmp == null) continue;
                tmp.raycastTarget = false;
                if (tmp.gameObject.name == "ModeSub")
                    tmp.color = ModeSubtitleOn(bg);
                else
                    tmp.color = ButtonLabelOn(bg);
            }
        }

        foreach (var btn in canvasRoot.GetComponentsInChildren<Button>(true))
        {
            if (btn == null) continue;
            string n = btn.gameObject.name;
            Color bg;
            if (n == "MBtn_Start")
                bg = BtnGreen();
            else if (n == "MBtn_Stop")
                bg = BtnRed();
            else if (n == "MBtn_Pause")
                bg = BtnBlue();
            else if (n == "MBtn_Demo")
                bg = BtnAmber();
            else if (n.StartsWith("MBtn_"))
                bg = C_Btn;
            else if (n == "Action_Demo")
                bg = BtnAmber();
            else if (n == "Action_Compare")
                bg = BtnViolet();
            else if (n == "Action_Cancel")
                bg = BtnPink();
            else if (n == "Action")
                bg = BtnViolet();
            else if (n == "HelpOkBtn" || n == "CloseResult" || n == "ShowTraj" || n == "ExportResult")
                bg = C_Btn; // chrome-кнопки як Help у top menu
            else if (n.StartsWith("Mode_"))
            {
                bool active = rocket != null && n == "Mode_" + rocket.controlMode;
                bg = active ? C_BtnActive : C_Btn;
            }
            else if (n is "LangBtn" or "ThemeBtn" or "HideBtn")
            {
                bg = (n == "HideBtn" && panelsHidden) ? C_BtnActive : C_Btn;
            }
            else
            {
                bg = C_Btn;
            }
            PaintButton(btn, bg);
        }

        UpdateTrajButtonVisual();
        UpdateViewButtonVisual();
        UpdateHideButtonVisual();
        UpdatePauseButtonVisual();

        // —— Лише body-текст (ніколи не перезаписувати підписи кнопок) ——
        void FixTmp(TMP_Text t)
        {
            if (t == null) return;
            if (t.GetComponentInParent<Button>() != null) return; // тримати ButtonLabelOn
            if (t.GetComponentInParent<TMP_InputField>() != null)
            {
                t.color = C_Text;
                return;
            }
            Color c = t.color;
            float l = Luma(c);
            bool chromatic = Mathf.Abs(c.r - c.g) > 0.08f
                || Mathf.Abs(c.g - c.b) > 0.08f
                || Mathf.Abs(c.r - c.b) > 0.08f;
            if (chromatic && l > 0.15f && l < 0.95f)
            {
                if (UiTheme.IsLightBackground && l > 0.55f)
                    t.color = Color.Lerp(c, C_Text, 0.55f);
                else if (!UiTheme.IsLightBackground && l < 0.4f)
                    t.color = Color.Lerp(c, C_Text, 0.5f);
                return;
            }
            if (UiTheme.IsLightBackground && l > 0.65f)
                t.color = C_Text;
            else if (!UiTheme.IsLightBackground && l < 0.4f)
                t.color = C_Text;
            else if (!chromatic)
                t.color = C_Text;
        }

        foreach (var tmp in canvasRoot.GetComponentsInChildren<TMP_Text>(true))
            FixTmp(tmp);
        if (conditionSectionGo != null)
            foreach (var tmp in conditionSectionGo.GetComponentsInChildren<TMP_Text>(true))
                FixTmp(tmp);

        // Заголовки секцій всюди (включно з блоком умов)
        foreach (var tmp in canvasRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp != null && tmp.gameObject.name == "SecHdr")
                tmp.color = C_Header;
        }

        // Відомі ролі (перекривають generic fix)
        void T(TMP_Text t, Color c) { if (t != null) t.color = c; }
        T(txtTitle, C_Header);
        T(txtMode, C_Amber);
        T(txtTime, C_Text);
        T(txtInsight, C_Text);
        T(txtInfo, C_Secondary);
        T(txtWinner, C_Ok);
        T(txtCamMode, C_Cyan);
        T(txtCamHelp, C_Secondary);
        T(txtGraphHint, C_Secondary);
        T(txtProgress, UiTheme.ChromeText);
        T(txtStep, C_Text);
        // Краї чіпів уже розфарбовано вище; знову форсувати чорнило підпису після FixTmp skip
        if (txtLangBtn != null && hideBtnImg == null) { /* no-op */ }
        if (txtLangBtn != null)
        {
            var p = txtLangBtn.transform.parent != null
                ? txtLangBtn.transform.parent.GetComponent<Image>() : null;
            txtLangBtn.color = ButtonLabelOn(p != null ? p.color : C_Btn);
        }
        if (txtThemeBtn != null)
        {
            var p = txtThemeBtn.transform.parent != null
                ? txtThemeBtn.transform.parent.GetComponent<Image>() : null;
            txtThemeBtn.color = ButtonLabelOn(p != null ? p.color : C_Btn);
        }
        if (txtHideBtn != null)
        {
            var p = hideBtnImg != null ? hideBtnImg
                : (txtHideBtn.transform.parent != null
                    ? txtHideBtn.transform.parent.GetComponent<Image>() : null);
            txtHideBtn.color = ButtonLabelOn(p != null ? p.color : C_Btn);
        }
        foreach (var m in metricLabels) T(m, C_Secondary);
        if (txtStep != null) txtStep.color = C_Text;
        // Вторинні підписи result / compare стежать за чорнилом теми
        if (canvasRoot != null)
        {
            foreach (var img in canvasRoot.GetComponentsInChildren<Image>(true))
            {
                if (img == null) continue;
                string n = img.gameObject.name;
                if (n is not ("StatBadge" or "Metric_0" or "Metric_1" or "Metric_2" or "Metric_3"
                    or "ScorePill")) continue;
                var tmps = img.GetComponentsInChildren<TMP_Text>(true);
                if (tmps == null || tmps.Length == 0) continue;
                if (n == "StatBadge" && tmps.Length >= 1)
                    tmps[0].color = C_Secondary;
                if (n.StartsWith("Metric_") && tmps.Length >= 1)
                    tmps[0].color = C_Secondary;
                if (n == "ScorePill" && tmps.Length >= 2)
                    tmps[1].color = C_Secondary;
            }
        }
        if (txtResultBody != null)
        {
            bool likelyAlert = txtResultBody.color.r > txtResultBody.color.g + 0.15f
                && txtResultBody.color.r > txtResultBody.color.b + 0.1f;
            if (!likelyAlert)
                txtResultBody.color = C_Secondary;
        }
        RefreshConditionLabels();

        graphAlt?.ApplyThemeColors();
        graphVel?.ApplyThemeColors();
        graphThr?.ApplyThemeColors();

        PaintModeButtons();

        RefreshSpeedLabel();
        if (rocket != null) UpdateFlightStep(rocket.state);
        if (resultShown && rocket?.metrics != null)
            ShowLandingResult(rocket.metrics);
    }

    static float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

    /// <summary>Заливка NumField — має контрастувати зі SliderBlock/panel на кожній темі.</summary>
    static Color NumFieldBg()
    {
        var p = UiTheme.Current;
        if (UiTheme.IsLightBackground)
        {
            // Виразний холодний чіп на білій панелі (раніше майже невидимий)
            Color bg = new Color(0.86f, 0.89f, 0.94f, 1f);
            bg = Color.Lerp(bg, p.Btn, 0.35f);
            bg = Color.Lerp(bg, p.Accent, 0.1f);
            bg.a = 1f;
            return bg;
        }
        // Темні теми: panel + btn + edge + accent, щоб Cyan/Amber/Violet/Green відрізнялись
        Color baseBg = Color.Lerp(p.PanelSoft, p.Btn, 0.5f);
        baseBg = Color.Lerp(baseBg, p.Edge, 0.22f);
        baseBg = Color.Lerp(baseBg, p.Accent, 0.1f);
        baseBg = Color.Lerp(baseBg, Color.white, 0.06f);
        baseBg.a = 1f;
        return baseBg;
    }

    static Color NumFieldEdge()
    {
        var e = UiTheme.Current.Edge;
        if (UiTheme.IsLightBackground)
        {
            // Сильніша hairline, щоб поля читались як бокси на paper UI
            e = Color.Lerp(e, UiTheme.Current.Accent, 0.25f);
            e.a = 0.85f;
        }
        else
            e.a = Mathf.Clamp(e.a, 0.55f, 0.9f);
        return e;
    }

    static Color NumFieldFocusBg(Color fieldBg)
    {
        var accent = UiTheme.Current.Accent;
        float t = UiTheme.IsLightBackground ? 0.24f : 0.42f;
        Color focus = Color.Lerp(fieldBg, accent, t);
        focus.a = 1f;
        return focus;
    }

    static void RefreshNumFieldTheme(Image fieldImg)
    {
        if (fieldImg == null) return;
        var input = fieldImg.GetComponent<TMP_InputField>();
        Color fieldBg = NumFieldBg();
        Color focusBg = NumFieldFocusBg(fieldBg);

        // ColorTint множить graphic.color — лишати білим
        fieldImg.color = Color.white;
        if (input != null)
        {
            var ic = input.colors;
            ic.normalColor = fieldBg;
            ic.highlightedColor = Color.Lerp(fieldBg, UiTheme.Current.Accent, 0.18f);
            ic.pressedColor = focusBg;
            ic.selectedColor = focusBg;
            ic.disabledColor = new Color(fieldBg.r, fieldBg.g, fieldBg.b, 0.45f);
            ic.colorMultiplier = 1f;
            ic.fadeDuration = 0.06f;
            input.colors = ic;
            input.caretColor = UiTheme.Current.Accent;
            input.selectionColor = new Color(
                UiTheme.Current.Accent.r, UiTheme.Current.Accent.g, UiTheme.Current.Accent.b,
                UiTheme.IsLightBackground ? 0.28f : 0.4f);
            if (input.textComponent != null)
                input.textComponent.color = UiTheme.Current.Text;
            if (input.placeholder is TMP_Text ph)
                ph.color = UiTheme.Current.Muted;
        }
        var outline = fieldImg.GetComponent<UnityEngine.UI.Outline>();
        if (outline != null)
        {
            // Idle: видимий край; focused outline і далі з акцентом (увімк. при select)
            bool focused = input != null && input.isFocused;
            if (focused)
            {
                var oc = UiTheme.Current.Accent; oc.a = 1f;
                outline.effectColor = oc;
                outline.effectDistance = new Vector2(2.5f, -2.5f);
                outline.enabled = true;
            }
            else
            {
                outline.effectColor = NumFieldEdge();
                outline.effectDistance = UiTheme.IsLightBackground
                    ? new Vector2(1.2f, -1.2f)
                    : new Vector2(1f, -1f);
                outline.enabled = true; // завжди показувати бокс на light-темах
            }
        }
    }

    void LoadUserSettingsIntoUi()
    {
        loadingSettings = true;
        if (windSlider != null) windSlider.value = UserSettings.Wind;
        if (testsSlider != null) testsSlider.value = UserSettings.Tests;
        if (timeScaleSlider != null) timeScaleSlider.value = UserSettings.TimeScale;
        if (liveSpeedSlider != null) liveSpeedSlider.value = UserSettings.LiveTimeScale;
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

        if (residualToggle != null) residualToggle.isOn = UserSettings.HybridResidual;
        if (seedSlider != null) seedSlider.value = UserSettings.ExperimentSeed;
        if (heightSlider != null) heightSlider.value = UserSettings.StartHeight;
        if (descentSlider != null) descentSlider.value = UserSettings.StartDescentSpeed;
        if (tilt0Slider != null) tilt0Slider.value = UserSettings.StartTilt;
        if (massNoiseSlider != null) massNoiseSlider.value = UserSettings.MassNoise;
        if (angleNoiseSlider != null) angleNoiseSlider.value = UserSettings.AngleNoise;
        if (rocket?.hybridController != null)
            rocket.hybridController.useNeuralResidual = UserSettings.HybridResidual;
        if (sim != null) sim.experimentSeed = UserSettings.ExperimentSeed;

        ApplySettings();
        // Відновити останню live-швидкість; Monte-Carlo burst використовує слайдер TimeScale окремо
        ApplyLiveTimeScale(UserSettings.LiveTimeScale);
        loadingSettings = false;
    }

    void SelectModeVisualOnly(RocketPhysics.ControlMode mode)
    {
        // Оновити mode pill + кольори кнопок без PrepareMode/reset
        if (txtMode != null)
            txtMode.text = UILocale.ModeNameShort(mode);
        for (int i = 0; i < modeButtons.Count && i < modeButtonImages.Count; i++)
        {
            var m = (RocketPhysics.ControlMode)i;
            bool active = m == mode;
            Color bg = active ? C_BtnActive : C_Btn;
            if (modeButtonImages[i] != null)
                modeButtonImages[i].color = bg;
            if (modeButtons[i] == null) continue;
            foreach (var tmp in modeButtons[i].GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null) continue;
                tmp.color = tmp.gameObject.name == "ModeSub"
                    ? ModeSubtitleOn(bg)
                    : ButtonLabelOn(bg);
            }
        }
    }

    void PaintModeButtons()
    {
        for (int i = 0; i < modeButtons.Count && i < modeButtonImages.Count; i++)
        {
            if (modeButtons[i] == null || modeButtonImages[i] == null) continue;
            bool active = rocket != null
                && modeButtons[i].gameObject.name == "Mode_" + rocket.controlMode;
            Color bg = active ? C_BtnActive : C_Btn;
            modeButtonImages[i].color = bg;
            foreach (var tmp in modeButtons[i].GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null) continue;
                tmp.color = tmp.gameObject.name == "ModeSub"
                    ? ModeSubtitleOn(bg)
                    : ButtonLabelOn(bg);
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
        if (liveSpeedSlider != null)
        {
            liveSpeedSlider.onValueChanged.RemoveListener(OnLiveSpeedChanged);
            liveSpeedSlider.onValueChanged.AddListener(OnLiveSpeedChanged);
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
        if (residualToggle != null)
        {
            residualToggle.onValueChanged.RemoveListener(OnResidualChanged);
            residualToggle.onValueChanged.AddListener(OnResidualChanged);
        }
        if (seedSlider != null)
        {
            seedSlider.onValueChanged.RemoveListener(OnSeedChanged);
            seedSlider.onValueChanged.AddListener(OnSeedChanged);
        }
        WireSliderPersist(heightSlider, OnHeightChanged);
        WireSliderPersist(descentSlider, OnDescentChanged);
        WireSliderPersist(tilt0Slider, OnTilt0Changed);
        WireSliderPersist(massNoiseSlider, OnMassNoiseChanged);
        WireSliderPersist(angleNoiseSlider, OnAngleNoiseChanged);
    }

    void WireSliderPersist(Slider s, UnityEngine.Events.UnityAction<float> handler)
    {
        if (s == null) return;
        s.onValueChanged.RemoveListener(handler);
        s.onValueChanged.AddListener(handler);
    }

    void OnHeightChanged(float v)
    {
        if (loadingSettings) return;
        UserSettings.StartHeight = v;
        UserSettings.Save();
        ApplySettings();
    }

    void OnDescentChanged(float v)
    {
        if (loadingSettings) return;
        UserSettings.StartDescentSpeed = v;
        UserSettings.Save();
        ApplySettings();
    }

    void OnTilt0Changed(float v)
    {
        if (loadingSettings) return;
        UserSettings.StartTilt = v;
        UserSettings.Save();
        ApplySettings();
    }

    void OnMassNoiseChanged(float v)
    {
        if (loadingSettings) return;
        UserSettings.MassNoise = v;
        UserSettings.Save();
        ApplySettings();
    }

    void OnAngleNoiseChanged(float v)
    {
        if (loadingSettings) return;
        UserSettings.AngleNoise = v;
        UserSettings.Save();
        ApplySettings();
    }

    void OnResidualChanged(bool on)
    {
        if (loadingSettings) return;
        UserSettings.HybridResidual = on;
        UserSettings.Save();
        if (rocket?.hybridController != null)
            rocket.hybridController.useNeuralResidual = on;
        NotifyInfo(on
            ? UILocale.T("msg_residual_on")
            : UILocale.T("msg_residual_off"));
    }

    void OnSeedChanged(float v)
    {
        if (loadingSettings) return;
        int s = Mathf.RoundToInt(v);
        UserSettings.ExperimentSeed = s;
        UserSettings.Save();
        if (sim != null) sim.experimentSeed = s;
        if (seedSlider != null && Mathf.Abs(seedSlider.value - s) > 0.01f)
            seedSlider.SetValueWithoutNotify(s);
        if (txtSeedVal != null) txtSeedVal.text = s.ToString();
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
        if (sim != null)
            sim.experimentTimeScale = v;
    }

    void OnLiveSpeedChanged(float v)
    {
        if (loadingSettings) return;
        // Slider зберігає 1..8 як int; map 1→0.5 опційно? Лишити 1..8 як Time.timeScale
        float s = Mathf.Clamp(v, 0.25f, 8f);
        UserSettings.LiveTimeScale = s;
        UserSettings.Save();
        ApplyLiveTimeScale(s);
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
        if (liveSpeedSlider != null) UserSettings.LiveTimeScale = liveSpeedSlider.value;
        if (seedSlider != null) UserSettings.ExperimentSeed = Mathf.RoundToInt(seedSlider.value);
        if (heightSlider != null) UserSettings.StartHeight = heightSlider.value;
        if (descentSlider != null) UserSettings.StartDescentSpeed = descentSlider.value;
        if (tilt0Slider != null) UserSettings.StartTilt = tilt0Slider.value;
        if (massNoiseSlider != null) UserSettings.MassNoise = massNoiseSlider.value;
        if (angleNoiseSlider != null) UserSettings.AngleNoise = angleNoiseSlider.value;
        if (noiseToggle != null) UserSettings.Noise = noiseToggle.isOn;
        if (trainToggle != null) UserSettings.Train = trainToggle.isOn;
        if (residualToggle != null) UserSettings.HybridResidual = residualToggle.isOn;
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
        // 0 = ширина: ціліші масштаби на 16:9 desktop (чіткіший TMP)
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
        BuildHelpOverlay(canvasGo.transform);
        ApplyPanelsVisibility();
    }

    /// <summary>Білий спрайт 1×1 для Simple UI (уникає артефактів товщини 9-slice).</summary>
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

    static Sprite s_uiCircle;
    static Sprite s_uiRound;

    static Sprite UiCircle()
    {
        if (s_uiCircle != null) return s_uiCircle;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float r = (n - 1) * 0.5f;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r));
            float a = Mathf.Clamp01(r - d + 0.65f);
            px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        s_uiCircle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return s_uiCircle;
    }

    static Sprite UiRound()
    {
        if (s_uiRound != null) return s_uiRound;
        const int n = 32;
        const int rad = 14;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float cx = Mathf.Clamp(x, rad, n - 1 - rad);
            float cy = Mathf.Clamp(y, rad, n - 1 - rad);
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            float a = Mathf.Clamp01(rad - d + 0.65f);
            px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        s_uiRound = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(rad, rad, rad, rad));
        return s_uiRound;
    }

    static void StyleTrack(Image img, Color c)
    {
        if (img == null) return;
        img.sprite = UiRound();
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 6.5f;
        img.color = c;
        img.raycastTarget = false;
        img.preserveAspect = false;
    }

    static void StyleKnob(Image img, Color c)
    {
        if (img == null) return;
        img.sprite = UiCircle();
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        img.color = c;
        img.raycastTarget = true;
    }

    static Color SliderTrackColor()
    {
        Color track = UiTheme.IsLightBackground
            ? new Color(0.80f, 0.83f, 0.88f, 1f)
            : Color.Lerp(C_Edge, C_Panel, 0.45f);
        track.a = 1f;
        return track;
    }

    static Color SliderFillColor()
    {
        var c = Color.Lerp(C_Accent, C_Cyan, 0.22f);
        c.a = 1f;
        return c;
    }

    static Color SliderKnobColor()
    {
        return UiTheme.IsLightBackground
            ? Color.white
            : Color.Lerp(C_Amber, Color.white, 0.55f);
    }

    enum MenuBtnKind { Normal, Start, Stop, Pause, Demo }

    /// <summary>
    /// Єдиний top chrome: identity + flight state | actions | settings.
    /// Два ряди в одній панелі — без подвійної рамки TopBar+TopMenu.
    /// </summary>
    void BuildTopChrome(Transform parent)
    {
        const float H = 84f;
        var chrome = CreatePanel("TopChrome", parent, C_Panel);
        topBarGo = chrome;
        topMenuGo = chrome; // той самий root — завжди видимий з top bar
        var rt = chrome.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(0, -H);
        rt.offsetMax = Vector2.zero;
        Outline(chrome, 1.2f);

        // Нижня акцентна волосина-лінія
        var accent = CreatePanel("TopAccent", chrome.transform, new Color(C_Edge.r, C_Edge.g, C_Edge.b, 0.5f));
        accent.GetComponent<Image>().raycastTarget = false;
        var art = accent.GetComponent<RectTransform>();
        art.anchorMin = new Vector2(0, 0);
        art.anchorMax = new Vector2(1, 0);
        art.pivot = new Vector2(0.5f, 0);
        art.anchoredPosition = Vector2.zero;
        art.sizeDelta = new Vector2(0, 2);

        // ── РЯД 1: identity | mode+time | status | settings ──
        var row1 = CreatePanel("Row1", chrome.transform, new Color(0, 0, 0, 0));
        row1.GetComponent<Image>().raycastTarget = false;
        var r1 = row1.GetComponent<RectTransform>();
        r1.anchorMin = new Vector2(0, 0.5f);
        r1.anchorMax = new Vector2(1, 1);
        r1.offsetMin = new Vector2(12, 2);
        // Лишити правий верх вільним для caption (− □ ×)
        const float capW = 46f;
        const float captionW = capW * 3f;
        r1.offsetMax = new Vector2(-(captionW + 10f), -4);

        // Бренд | режим | час
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

        // Caption на окремому canvas (не знищується з RebuildUi теми → без мерехтіння)
        EnsureCaptionBar();
        ApplyCaptionTheme();

        // ── РЯД 2 низ: ЛІВОРУЧ flight | ПРАВОРУЧ tools (Hide Theme Lang) ──
        const float chipW = 78f; // Hide / Lang (як Start)
        const float themeW = 118f; // повна назва теми + " Y" без обрізання
        const float gap = 5f;
        const float rightInset = 16f;
        // Hide + Theme(wide) + Lang (Help стоїть після Export у flight-ряду)
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
        pauseBtn = MenuBtn(row2.transform, PauseButtonLabel(), OnPause, MenuBtnKind.Pause, chipW, out txtPauseBtn);
        pauseBtnImg = pauseBtn != null ? pauseBtn.targetGraphic as Image : null;
        UpdatePauseButtonVisual();
        MenuBtn(row2.transform, (UILocale.T("top_demo") + "  D").ToUpperInvariant(), OnDefenseDemo, MenuBtnKind.Demo, chipW);

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
        MenuBtn(row2.transform, (UILocale.T("top_help") + "  F1").ToUpperInvariant(), ToggleHelp, MenuBtnKind.Normal, chipW);

        // Справа→ліворуч: Lang [G], Theme [Y] (ширший), Hide [H]
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
    /// Крайовий tool-чіп: той самий візуальний розмір, що Start (повна висота нижнього ряду), притиснутий справа.
    /// xR = правий край чіпа (від’ємно від правого краю chrome).
    /// </summary>
    void PlaceEdgeBtn(Transform chrome, string name, string label, ref float xR, float w,
        Color bg, UnityEngine.Events.UnityAction onClick, out TMP_Text labelTxt)
    {
        var go = CreatePanel(name, chrome, bg);
        var rt = go.GetComponent<RectTransform>();
        // Розтягнути вертикально на всю нижню половину chrome (та сама смуга, що ряд Start)
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
            ApplyBtnColorBlock(ref colors);
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

    // Caption на окремому overlay canvas (переживає RebuildUi)
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
        canvas.sortingOrder = 500; // над основним HUD
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

    /// <summary>Перефарбувати − □ × з поточної теми без destroy (без мерехтіння).</summary>
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
            img.color = Color.white; // ColorTint множить це
            var c = ColorBlock.defaultColorBlock;
            c.normalColor = bg;
            c.highlightedColor = hi;
            c.pressedColor = UiTheme.IsLightBackground
                ? Color.Lerp(bg, Color.white, 0.35f)
                : Color.Lerp(bg, Color.black, 0.3f);
            c.selectedColor = bg;
            c.disabledColor = new Color(bg.r, bg.g, bg.b, 0.35f);
            c.colorMultiplier = 1f;
            c.fadeDuration = 0f; // без tween-спалаху
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

    void ApplyLiveTimeScale(float s)
    {
        s = Mathf.Clamp(s, 0.25f, 8f);
        if (sim != null && sim.IsExperimentRunning) return;
        Time.timeScale = s;
        // Тримати fixed step близько до бази (не роздувати fixedDt на високому scale)
        float baseDt = rocket != null && rocket.parameters != null
            ? rocket.parameters.fixedTimeStep : 0.005f;
        Time.fixedDeltaTime = baseDt;
        if (!loadingSettings)
        {
            UserSettings.LiveTimeScale = s;
            UserSettings.Save();
        }
    }

    void RefreshSpeedLabel() { /* speed chips removed — slider only */ }

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
                bg = BtnGreen();
                txtCol = ButtonLabelOn(bg);
                break;
            case MenuBtnKind.Stop:
                bg = BtnRed();
                txtCol = ButtonLabelOn(bg);
                break;
            case MenuBtnKind.Pause:
                bg = BtnBlue();
                txtCol = ButtonLabelOn(bg);
                break;
            case MenuBtnKind.Demo:
                bg = BtnAmber();
                txtCol = ButtonLabelOn(bg);
                break;
            default:
                bg = C_Btn;
                txtCol = ButtonLabelOn(bg);
                break;
        }

        var go = CreatePanel("MBtn_" + kind, parent, bg);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.minWidth = width;
        le.flexibleWidth = 0f;
        le.preferredHeight = 28f;
        le.flexibleHeight = 1f; // розтягуватись з рядом як Start, коли HLG збільшує висоту

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        var colors = btn.colors;
        ApplyBtnColorBlock(ref colors);
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
        if (Input.GetKeyDown(KeyCode.U)) OnPause();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (helpVisible) { SetHelpVisible(false); return; }
            if (resultShown) HideLandingResult();
            else OnStop();
        }
        if (Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.Slash) || Input.GetKeyDown(KeyCode.Question))
            ToggleHelp();
        if (Input.GetKeyDown(KeyCode.D) && !ctrl)
            OnDefenseDemo();
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
        // top chrome завжди видимий для налаштувань / дій польоту
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
        Color bg = panelsHidden ? C_BtnActive : C_Btn;
        if (hideBtnImg != null)
            hideBtnImg.color = bg;
        if (txtHideBtn != null)
        {
            txtHideBtn.text = HideButtonLabel();
            txtHideBtn.color = ButtonLabelOn(bg);
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
        ApplyBtnColorBlock(ref colors);
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
            return Color.Lerp(accent, new Color(0.96f, 0.97f, 0.985f, 1f), 0.82f);
        return Color.Lerp(accent, new Color(0.08f, 0.09f, 0.12f, 1f), 0.55f);
    }

    void SetStatusVisual(string key, Color accent)
    {
        if (txtStatus != null)
        {
            // Та сама мова, що бейджі landing-gate: bold підпис + висококонтрастне чорнило на чіпі
            txtStatus.text = UILocale.T(key);
            // На light-темах тримати глибоке чорнило (ніколи блідий neon на блідій заливці)
            txtStatus.color = UiTheme.IsLightBackground
                ? Color.Lerp(accent, C_Text, 0.28f)
                : accent;
            txtStatus.fontStyle = FontStyles.Bold;
        }
        if (statusDot != null)
        {
            // М’яка заливка: light-теми лишаються блідими, щоб темне status-чорнило читалось
            float t = UiTheme.IsLightBackground ? 0.22f : 0.42f;
            Color c = Color.Lerp(C_PanelSoft, accent, t);
            c.a = 1f;
            statusDot.color = c;
        }
    }

    void BuildLeftPanel(Transform parent)
    {
        // Ліва колонка mission-control: спочатку GATE → primary flight → решта → charts
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

        // ── 1. LANDING GATE (завжди перший — рішення з першого погляду) ──
        txtHdrCrit = Header(root, UILocale.T("h_crit"), ref y, pad, inner);
        BuildCriterionGrid(root, ref y, pad, inner);

        // ── 2. GUIDANCE (одне речення, висока цінність) ──
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

        // ── 3. ОСНОВНИЙ СТАН ПОЛЬОТУ ──
        y -= 2f;
        txtHdrTelem = Header(root, UILocale.T("h_primary"), ref y, pad, inner);
        txtAlt = Metric(root, UILocale.T("m_alt"), UILocale.T("u_m"), ref y, pad, inner, primary: true);
        txtVel = Metric(root, UILocale.T("m_vy"), UILocale.T("u_ms"), ref y, pad, inner, primary: true);
        txtTilt = Metric(root, UILocale.T("m_tilt"), UILocale.T("u_deg"), ref y, pad, inner, primary: true);
        tiltBarFill = MakeBar(root, ref y, C_Amber, pad, inner);
        txtMiss = Metric(root, UILocale.T("m_miss"), UILocale.T("u_m"), ref y, pad, inner, primary: true);

        // ── 4. ДИНАМІКА ──
        y -= 4f;
        Header(root, UILocale.T("h_dyn"), ref y, pad, inner);
        txtHVel = Metric(root, UILocale.T("m_vh"), UILocale.T("u_ms"), ref y, pad, inner);
        txtThr = Metric(root, UILocale.T("m_thr"), UILocale.T("u_kn"), ref y, pad, inner);
        thrBarFill = MakeBar(root, ref y, C_Cyan, pad, inner);
        txtTwr = Metric(root, UILocale.T("m_twr"), "x", ref y, pad, inner);
        txtRate = Metric(root, UILocale.T("m_rate"), UILocale.T("u_dps"), ref y, pad, inner);
        txtAcc = Metric(root, UILocale.T("m_acc"), UILocale.T("u_ms2"), ref y, pad, inner);
        txtEta = Metric(root, UILocale.T("m_eta"), UILocale.T("u_s"), ref y, pad, inner);

        // ── 5. РУШІЙНА УСТАНОВКА ──
        y -= 4f;
        Header(root, UILocale.T("h_prop"), ref y, pad, inner);
        // Паливо одним рядом: значення кг + % через текст fuelPct під смугою
        txtFuel = Metric(root, UILocale.T("m_fuel"), UILocale.T("u_kg"), ref y, pad, inner);
        fuelBarFill = MakeBar(root, ref y, C_Ok, pad, inner);
        txtFuelPct = Metric(root, UILocale.T("m_fuel_pct"), UILocale.T("u_pct"), ref y, pad, inner);
        txtMass = Metric(root, UILocale.T("m_mass"), UILocale.T("u_t"), ref y, pad, inner);
        txtScore = Metric(root, UILocale.T("m_score"), UILocale.T("u_score"), ref y, pad, inner);

        // ── 6. ПІКИ / DELTA (компактно) ──
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

        // ── 7. ГРАФІКИ ──
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
        // GATE 2×2: назва + live vs ліміт + статус (OK / FAIL / WATCH)
        float gap = 6f;
        float cellW = (inner - gap) * 0.5f;
        float cellH = 52f;
        float row0 = y;

        txtCritV = MakeCriterionBadge(root, pad, row0, cellW, cellH, UILocale.T("crit_vy"));
        txtCritA = MakeCriterionBadge(root, pad + cellW + gap, row0, cellW, cellH, UILocale.T("crit_tilt"));
        y -= cellH + gap;
        float row1 = y;
        txtCritM = MakeCriterionBadge(root, pad, row1, cellW, cellH, UILocale.T("crit_miss"));
        txtCritH = MakeCriterionBadge(root, pad + cellW + gap, row1, cellW, cellH, UILocale.T("crit_vh"));
        y -= cellH + 4f;

        // Однорядкова підказка під сіткою
        var hint = CreateText(root, UILocale.T("crit_gate_hint"), 10, C_Muted);
        hint.alignment = TextAlignmentOptions.MidlineLeft;
        hint.overflowMode = TextOverflowModes.Ellipsis;
        hint.textWrappingMode = TextWrappingModes.NoWrap;
        PinTL(hint.rectTransform, pad, y, inner, 16);
        y -= 18f;

        // Смуга статусу — та сама родина бейджів, що критерії
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

        // 3 рядки: назва / значення vs ліміт / статус — без загадкових OK/NO/..
        string idle = title + "\n" + UILocale.T("crit_idle") + "\n—";
        var t = CreateText(bg.transform, idle, 11, C_Muted, FontStyles.Normal);
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Truncate;
        t.lineSpacing = -4f;
        StretchFull(t.rectTransform, 5, 4, 5, 4);
        return t;
    }

    void BuildRightPanel(Transform parent)
    {
        // Колонка керування: режим → умови тесту → порівняння → результати
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

        // ── 1. Алгоритм 2x2 ──
        Header(root, UILocale.T("h_step1"), ref y, pad, inner);
        modeButtons.Clear();
        modeButtonImages.Clear();
        float cellW = (inner - gap) * 0.5f;
        float cellH = 48f;
        float halfW = (inner - gap) * 0.5f;
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

        // ── 2–3. Умови (persistent-секція — NumFields ніколи не знищуються) ──
        PlaceConditionSection(root, ref y, pad, inner, gap);

        // ── 4. Дії порівняння ──
        Header(root, UILocale.T("h_step2"), ref y, pad, inner);
        float btnH = 34f;
        // Та сама родина тону/яскравості, що top Start (зелений) / Stop (червоний) / Pause (синій)
        ActionButtonAt(root, pad, y, halfW, btnH, UILocale.T("btn_compare"),
            "Action_Compare", BtnViolet(), OnStartCompare);
        ActionButtonAt(root, pad + halfW + gap, y, halfW, btnH, UILocale.T("btn_cancel"),
            "Action_Cancel", BtnPink(), OnCancelCompare);
        y -= btnH + 10f;

        // ── 4. Результати порівняння 2x2 ──
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

        // ── 5. Камера (над повідомленнями) ──
        Header(root, UILocale.T("h_cam"), ref y, pad, inner);
        var camBg = CreatePanel("CamBg", root, C_PanelSoft);
        camBg.GetComponent<Image>().raycastTarget = false;
        PinTL(camBg.GetComponent<RectTransform>(), pad, y, inner, 48);
        txtCamMode = CreateText(camBg.transform, UILocale.T("cam_prefix") + UILocale.T("cam_follow"),
            12, C_Accent, FontStyles.Bold);
        var cmRt = txtCamMode.rectTransform;
        cmRt.anchorMin = new Vector2(0, 0.48f);
        cmRt.anchorMax = new Vector2(1, 1);
        cmRt.offsetMin = new Vector2(8, 0);
        cmRt.offsetMax = new Vector2(-8, -4);
        txtCamMode.alignment = TextAlignmentOptions.BottomLeft;
        txtCamMode.overflowMode = TextOverflowModes.Ellipsis;

        txtCamHelp = CreateText(camBg.transform, UILocale.T("cam_keys"), 10, C_Muted);
        var chRt = txtCamHelp.rectTransform;
        chRt.anchorMin = new Vector2(0, 0);
        chRt.anchorMax = new Vector2(1, 0.52f);
        chRt.offsetMin = new Vector2(8, 4);
        chRt.offsetMax = new Vector2(-8, 0);
        txtCamHelp.alignment = TextAlignmentOptions.TopLeft;
        txtCamHelp.overflowMode = TextOverflowModes.Ellipsis;
        txtCamHelp.textWrappingMode = TextWrappingModes.Normal;
        y -= 54f;

        // ── 6. Статус / підказки ──
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

    // ─── Дії користувача ───

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
        // Зберегти режим камери (T огляд / C manual) — не форсувати Follow
        var cam = ResolveCamera();
        if (cam != null)
        {
            bool keepOverview = overviewCam || cam.mode == CameraFollow.ViewMode.Overview;
            bool keepManual = cam.mode == CameraFollow.ViewMode.Manual;
            if (keepOverview)
            {
                overviewCam = true;
                cam.SnapToFullTrajectoryView();
            }
            else if (keepManual)
            {
                // лишити Manual як є
                cam.SetMode(CameraFollow.ViewMode.Manual);
            }
            // інакше Follow лишається Follow — без примусового reset
        }
        RefreshCamLabel();
        UpdateViewButtonVisual();
        ApplySettings();
        ApplyExperimentInitialConditions();
        // Детерміновані збурення одиночного запуску з поточного seed
        SimRng.Reseed(sim != null ? sim.experimentSeed : UserSettings.ExperimentSeed);
        rocket.batchDrivenTicks = false; // гарантувати FixedUpdate + trajectory
        // Звичайний старт: пакет → sep → Stage1 (Ideal лишає skipStackPhase=true)
        rocket.ResetSimulation();
        UpdatePauseButtonVisual();
        var tv = EnsureTrajectoryVisualizer();
        tv?.Clear();
        tv?.SetVisible(trajVisible);

        // Вітер/шум — лише після відділення (ApplyFlightDisturbances відкладає на Stack)
        float wind = windSlider != null ? windSlider.value : 0f;
        bool noise = noiseToggle != null && noiseToggle.isOn;
        float massVar = massNoiseSlider != null ? massNoiseSlider.value : UserSettings.MassNoise;
        float angVar = angleNoiseSlider != null ? angleNoiseSlider.value : UserSettings.AngleNoise;
        if (sim != null)
        {
            sim.windStrength = wind;
            sim.enableNoise = noise;
            sim.massVariationPercent = massVar;
            sim.angleVariationDegrees = angVar;
        }
        rocket.ApplyFlightDisturbances(wind, noise, massVar, angVar);

        float h0 = rocket.state.position.y;
        string dist = UILocale.IsUK
            ? $"Earth LZ · 1-й ступінь · h={h0:F0} м · вітер≈{wind:F0}"
            : $"Earth LZ · first stage · h={h0:F0} m · wind≈{wind:F0}";
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

        // Синхронізувати UI-слайдери з пресетом Ideal
        loadingSettings = true;
        if (heightSlider) heightSlider.value = IdealLandingPresets.StartHeight;
        if (descentSlider) descentSlider.value = IdealLandingPresets.StartDescentSpeed;
        if (tilt0Slider) tilt0Slider.value = Mathf.Round(IdealLandingPresets.StartTiltDeg);
        if (windSlider) windSlider.value = 0f;
        if (noiseToggle) noiseToggle.isOn = false;
        if (trainToggle) trainToggle.isOn = false;
        loadingSettings = false;
        UserSettings.StartHeight = IdealLandingPresets.StartHeight;
        UserSettings.StartDescentSpeed = IdealLandingPresets.StartDescentSpeed;
        UserSettings.StartTilt = Mathf.Round(IdealLandingPresets.StartTiltDeg);
        UserSettings.Wind = 0f;
        UserSettings.Noise = false;
        UserSettings.Save();
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

        Color bg = trajVisible ? C_BtnActive : C_Btn;

        if (trajToggleImg == null && trajToggleBtn != null)
            trajToggleImg = trajToggleBtn.targetGraphic as Image;
        if (trajToggleImg != null)
            trajToggleImg.color = bg;
        if (txtTrajBtn != null)
        {
            txtTrajBtn.text = PathButtonLabel();
            txtTrajBtn.color = ButtonLabelOn(bg);
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
        UpdatePauseButtonVisual();
    }

    string PauseButtonLabel()
    {
        bool paused = rocket != null && rocket.simulationPaused;
        return (UILocale.T(paused ? "top_resume" : "top_pause") + "  U").ToUpperInvariant();
    }

    void UpdatePauseButtonVisual()
    {
        bool paused = rocket != null && rocket.simulationPaused;
        Color bg = BtnBlue();
        if (paused)
            bg = Color.Lerp(bg, Color.white, 0.12f);
        if (pauseBtnImg == null && pauseBtn != null)
            pauseBtnImg = pauseBtn.targetGraphic as Image;
        if (pauseBtnImg != null)
            pauseBtnImg.color = bg;
        if (txtPauseBtn != null)
        {
            txtPauseBtn.text = PauseButtonLabel();
            txtPauseBtn.color = ButtonLabelOn(bg);
        }
    }

    void OnPause()
    {
        if (rocket == null) return;
        if (sim != null && sim.IsExperimentRunning)
        {
            NotifyInfo(UILocale.T("msg_cancel_first"));
            return;
        }
        if (!rocket.simulationArmed || rocket.state.simulationFinished || rocket.state.isLanded)
        {
            NotifyInfo(UILocale.IsUK
                ? "Немає активного польоту для паузи."
                : "No active flight to pause.");
            return;
        }

        rocket.simulationPaused = !rocket.simulationPaused;
        UpdatePauseButtonVisual();
        if (rocket.simulationPaused)
        {
            NotifyInfo(UILocale.T("msg_paused"));
            SetStatusVisual("st_pause", new Color(0.35f, 0.55f, 0.95f, 1f));
        }
        else
        {
            NotifyInfo(UILocale.T("msg_resumed"));
            SetStatusVisual(rocket.state.time > 0.05f ? "st_descent" : "st_start",
                rocket.state.time > 0.05f ? C_Cyan : C_Amber);
        }
    }

    void OnToggleTrajectoryView() => OnFullTrajectoryView();

    /// <summary>
    /// Toggle overview повної траєкторії. Ще раз (або F) — повернутись у follow.
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
            // Явний F — увімкнути слідкування (скинути freeze огляду)
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
        // Завжди виходити з overview і відновлювати follow orbit за замовчуванням
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

        if (viewToggleImg == null && viewToggleBtn != null)
            viewToggleImg = viewToggleBtn.targetGraphic as Image;
        if (viewToggleImg != null)
            viewToggleImg.color = bg;
        if (txtViewBtn != null)
        {
            txtViewBtn.text = ViewButtonLabel();
            txtViewBtn.color = ButtonLabelOn(bg);
        }
    }

    void OnExportResults()
    {
        try
        {
            // Віддавати перевагу comparison export, якщо доступний
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
            maxTouchdownVelocity = maxV > 0 ? maxV : (p != null ? p.maxTouchdownVelocity : LandingCriteria.DefaultMaxTouchdownVelocity),
            maxLandingAngle = maxA > 0 ? maxA : (p != null ? p.maxLandingAngle : LandingCriteria.DefaultMaxLandingAngle),
            maxHorizontalMiss = maxM > 0 ? maxM : (p != null ? p.maxHorizontalMiss : LandingCriteria.DefaultMaxHorizontalMiss),
            maxHorizontalSpeed = maxH > 0 ? maxH : (p != null ? p.maxHorizontalSpeed : LandingCriteria.DefaultMaxHorizontalSpeed),
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
        if (defenseDemoCo != null) { StopCoroutine(defenseDemoCo); defenseDemoCo = null; }
        HideLandingResult();
        ClearGraphs();
        sampleTimer = 0f;
        ResetFlightPeaks();
        // Зафіксувати справедливий paired Monte-Carlo протокол (однакові ПУ + збурення для A–D)
        DefenseBaseline.ApplyTo(sim);
        if (rocket?.hybridController != null)
            rocket.hybridController.useNeuralResidual = DefenseBaseline.HybridResidualOn;
        SyncUiFromDefenseBaseline();
        ApplySettings();
        sim.RequestFullExperiment();
        NotifyInfo(UILocale.T("msg_compare"));
    }

    /// <summary>Віддзеркалити константи DefenseBaseline на слайдери, щоб export/UI збігались із запуском.</summary>
    void SyncUiFromDefenseBaseline()
    {
        loadingSettings = true;
        if (testsSlider) testsSlider.value = DefenseBaseline.TestsPerAlgorithm;
        if (windSlider) windSlider.value = DefenseBaseline.WindStrength;
        if (noiseToggle) noiseToggle.isOn = DefenseBaseline.EnableNoise;
        if (seedSlider) seedSlider.value = DefenseBaseline.Seed;
        if (massNoiseSlider) massNoiseSlider.value = DefenseBaseline.MassVariationPercent;
        if (angleNoiseSlider) angleNoiseSlider.value = DefenseBaseline.AngleVariationDegrees;
        if (heightSlider) heightSlider.value = DefenseBaseline.StartHeight;
        if (descentSlider) descentSlider.value = DefenseBaseline.StartDescentSpeed;
        if (tilt0Slider) tilt0Slider.value = DefenseBaseline.StartTiltDeg;
        if (residualToggle) residualToggle.isOn = DefenseBaseline.HybridResidualOn;
        if (trainToggle) trainToggle.isOn = false;
        loadingSettings = false;

        UserSettings.Tests = DefenseBaseline.TestsPerAlgorithm;
        UserSettings.Wind = DefenseBaseline.WindStrength;
        UserSettings.Noise = DefenseBaseline.EnableNoise;
        UserSettings.ExperimentSeed = DefenseBaseline.Seed;
        UserSettings.MassNoise = DefenseBaseline.MassVariationPercent;
        UserSettings.AngleNoise = DefenseBaseline.AngleVariationDegrees;
        UserSettings.StartHeight = DefenseBaseline.StartHeight;
        UserSettings.StartDescentSpeed = DefenseBaseline.StartDescentSpeed;
        UserSettings.StartTilt = DefenseBaseline.StartTiltDeg;
        UserSettings.HybridResidual = DefenseBaseline.HybridResidualOn;
        UserSettings.Train = false;
        UserSettings.Save();
        // Підписи значень слайдера оновлюються через onValueChanged (шлях SetValue)
    }

    void OnDefenseDemo()
    {
        if (rocket == null) return;
        if (sim != null && sim.IsExperimentRunning)
        {
            NotifyInfo(UILocale.T("msg_cancel_first"));
            return;
        }
        if (defenseDemoCo != null) StopCoroutine(defenseDemoCo);
        defenseDemoCo = StartCoroutine(DefenseDemoRoutine());
    }

    System.Collections.IEnumerator DefenseDemoRoutine()
    {
        NotifyInfo(UILocale.T("msg_demo_start"));
        SetHelpVisible(false);
        HideLandingResult();

        // Hybrid Ideal: посадка лише 1-го ступеня (Earth LZ)
        SelectMode(RocketPhysics.ControlMode.Hybrid);
        yield return null;

        if (heightSlider != null) heightSlider.value = IdealLandingPresets.StartHeight;
        if (descentSlider != null) descentSlider.value = IdealLandingPresets.StartDescentSpeed;
        if (tilt0Slider != null) tilt0Slider.value = IdealLandingPresets.StartTiltDeg;
        if (windSlider != null) windSlider.value = 0f;
        if (noiseToggle != null) noiseToggle.isOn = false;
        if (sim != null)
        {
            sim.enableNoise = false;
            sim.windStrength = 0f;
            sim.startHeight = IdealLandingPresets.StartHeight;
            sim.startDescentSpeed = IdealLandingPresets.StartDescentSpeed;
            sim.startTiltDeg = IdealLandingPresets.StartTiltDeg;
        }

        IdealLandingPresets.ApplyDefaultControllerTuning(
            rocket, rocket.fuzzyController, rocket.neuralController, rocket.hybridController);
        if (rocket.neuralController != null)
        {
            rocket.neuralController.enableTraining = false;
            rocket.neuralController.InstallIdealWeights();
        }
        rocket.skipStackPhase = true;

        yield return null;
        ApplyLiveTimeScale(Mathf.Max(1f, Time.timeScale));
        OnStartLanding();
        NotifyInfo(UILocale.T("msg_demo_flight"));

        float wall = 0f;
        while (rocket != null && rocket.simulationArmed && !rocket.state.simulationFinished && wall < 240f)
        {
            wall += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(1.2f);
        if (rocket != null && rocket.state.simulationFinished)
        {
            // Не зривати ручний огляд (Manual / user orbit) — лише з Follow без lock
            var cam = ResolveCamera();
            bool userInspecting = cam != null && (
                cam.mode == CameraFollow.ViewMode.Manual
                || cam.userOrbitLock
                || cam.mode == CameraFollow.ViewMode.Overview);
            if (!userInspecting)
                OnFullTrajectoryView();
            NotifyInfo(UILocale.T("msg_demo_done"));
        }
        defenseDemoCo = null;
    }

    void ToggleHelp() => SetHelpVisible(!helpVisible);

    void SetHelpVisible(bool on)
    {
        helpVisible = on;
        if (helpRoot != null)
        {
            helpRoot.SetActive(on);
            // Оновити scrim під поточну тему (завжди легкий)
            var img = helpRoot.GetComponent<Image>();
            if (img != null) img.color = HelpScrimColor();
        }
    }

    /// <summary>Напівпрозорий scrim — інтерфейс позаду лишається читабельним.</summary>
    static Color HelpScrimColor()
    {
        if (UiTheme.IsLightBackground)
            return new Color(0.15f, 0.18f, 0.22f, 0.28f);
        return new Color(0.02f, 0.03f, 0.06f, 0.32f);
    }

    void BuildHelpOverlay(Transform parent)
    {
        // Легкий scrim — HUD/сцена лишаються видимими (не «сіра стіна»)
        helpRoot = CreatePanel("HelpOverlay", parent, HelpScrimColor());
        var rt = helpRoot.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var dimBtn = helpRoot.AddComponent<Button>();
        dimBtn.targetGraphic = helpRoot.GetComponent<Image>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(() => SetHelpVisible(false));

        // Компактна картка: title + body + кнопка
        const float cardW = 540f;
        const float cardH = 430f;
        const float titleH = 28f;
        const float titleTop = 14f;
        const float bodyTop = titleTop + titleH + 8f;
        const float btnH = 34f;
        const float btnBottom = 14f;
        const float bodyBottom = btnBottom + btnH + 10f;

        // Непрозора картка поверх легкого scrim
        Color cardBg = C_Panel;
        cardBg.a = 1f;
        var card = CreatePanel("HelpCard", helpRoot.transform, cardBg);
        var crt = card.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(cardW, cardH);
        Outline(card, 1.5f);
        // Клік по картці не закриває help
        var block = card.AddComponent<Button>();
        block.targetGraphic = card.GetComponent<Image>();
        block.transition = Selectable.Transition.None;

        var title = CreateText(card.transform, UILocale.T("help_title"), 17, C_Accent, FontStyles.Bold);
        var tr = title.rectTransform;
        tr.anchorMin = new Vector2(0, 1);
        tr.anchorMax = new Vector2(1, 1);
        tr.pivot = new Vector2(0.5f, 1);
        tr.anchoredPosition = new Vector2(0, -titleTop);
        tr.sizeDelta = new Vector2(-28, titleH);
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;

        var body = CreateText(card.transform, UILocale.T("help_body"), 12, C_Text);
        var br = body.rectTransform;
        br.anchorMin = new Vector2(0, 0);
        br.anchorMax = new Vector2(1, 1);
        br.offsetMin = new Vector2(20, bodyBottom);
        br.offsetMax = new Vector2(-20, -bodyTop);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Ellipsis;
        body.raycastTarget = false;
        body.lineSpacing = 2f;

        float btnY = -(cardH - btnBottom - btnH);
        float btnX = (cardW - 200f) * 0.5f;
        // Як кнопка Help у top menu: C_Btn + ButtonLabelOn (не фіолетовий Action)
        ActionButtonAt(card.transform, btnX, btnY, 200f, btnH, UILocale.T("btn_ok"),
            "HelpOkBtn", C_Btn, () => SetHelpVisible(false));

        helpRoot.SetActive(false);
        helpVisible = false;
    }

    void OnCancelCompare()
    {
        if (sim == null) return;
        sim.CancelExperiment();
        NotifyInfo(UILocale.T("btn_cancel"));
    }

    public void NotifyInfo(string msg)
    {
        if (txtInfo == null) return;
        txtInfo.text = msg;
        // Рядки Success / compare-done мають лишатись читабельними на світлих panel chips
        bool successTone = !string.IsNullOrEmpty(msg) && (
            msg.IndexOf("успіх", System.StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("success", System.StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("перемож", System.StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("winner", System.StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("експорт", System.StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("export", System.StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("завершен", System.StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("complete", System.StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("звіти", System.StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("готов", System.StringComparison.OrdinalIgnoreCase) >= 0);
        txtInfo.color = successTone ? C_Ok : C_Text;
    }

    public void SetBatchMode(bool on)
    {
        batchMode = on;
        if (on)
        {
            // Новий comparison pack → стерти попередні сліди landing/batch
            ClearGraphs();
            sampleTimer = 0f;
            HideLandingResult();
            // Запобігти зависанням PATH під час / одразу після Monte-Carlo
            var tv = EnsureTrajectoryVisualizer();
            tv?.Clear();
            if (tv != null) tv.SetVisible(false);
            trajVisible = false;
            UpdateTrajButtonVisual();
            // Широкий overview під час batch A–D (pad + коридор зниження)
            EnterOverviewForCompare();
        }
        // on=false: лишити charts, щоб завершене порівняння лишалось видимим
        if (progressRoot != null) progressRoot.SetActive(on);
        RefreshSpeedLabel();
    }

    /// <summary>Форсувати overview повної траєкторії (без toggle-off) для Monte-Carlo.</summary>
    void EnterOverviewForCompare()
    {
        var cam = ResolveCamera();
        if (cam == null) return;
        overviewCam = true;
        cam.SnapToFullTrajectoryView();
        RefreshCamLabel();
        UpdateViewButtonVisual();
    }

    public void SetExperimentProgress(string label, float p01)
    {
        if (txtProgress) txtProgress.text = label;
        if (progressFill != null)
            progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(p01), 1f);
        // Mode pill лишається коротким — деталі batch лише на progress bar
        if (txtMode && rocket != null)
            txtMode.text = UILocale.ModeNameShort(rocket.controlMode);
    }

    public void ShowLandingResult(LandingMetrics m)
    {
        if (batchMode || m == null || resultRoot == null) return;
        resultShown = true;
        resultRoot.SetActive(true);

        var p = rocket?.parameters;
        float maxV = p != null && p.maxTouchdownVelocity > 0.1f ? p.maxTouchdownVelocity : LandingCriteria.DefaultMaxTouchdownVelocity;
        float maxA = p != null && p.maxLandingAngle > 0.1f ? p.maxLandingAngle : LandingCriteria.DefaultMaxLandingAngle;
        float maxM = p != null && p.maxHorizontalMiss > 0.1f ? p.maxHorizontalMiss : LandingCriteria.DefaultMaxHorizontalMiss;
        float maxH = p != null && p.maxHorizontalSpeed > 0.1f ? p.maxHorizontalSpeed : LandingCriteria.DefaultMaxHorizontalSpeed;

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

        // Компактні картки метрик (локалізовані одиниці)
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
                txtResultBody.color = C_Secondary;
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
                txtResultBody.color = C_Secondary;
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
            resultMetricKeys[i].color = C_Secondary;
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

        // Компактна картка — щільно за змістом, без «мертвого» повітря
        var card = CreatePanel("ResultCard", resultRoot.transform, C_Panel);
        resultPanelBg = card.GetComponent<Image>();
        var crt = card.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(492f, 212f);
        Outline(card, 1.5f);

        // Акцентна смуга статусу
        var accent = CreatePanel("ResAccent", card.transform, new Color(C_Ok.r, C_Ok.g, C_Ok.b, 0.9f));
        resultAccentBar = accent.GetComponent<Image>();
        resultAccentBar.raycastTarget = false;
        var art = accent.GetComponent<RectTransform>();
        art.anchorMin = new Vector2(0f, 1f);
        art.anchorMax = new Vector2(1f, 1f);
        art.pivot = new Vector2(0.5f, 1f);
        art.anchoredPosition = Vector2.zero;
        art.sizeDelta = new Vector2(0f, 3f);

        // Ряд заголовка: title (ліворуч) + score pill (праворуч)
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

        var scoreUnit = CreateText(scorePill.transform, UILocale.T("u_score"), 9, C_Secondary, FontStyles.Bold);
        var surt = scoreUnit.rectTransform;
        surt.anchorMin = new Vector2(0f, 0f);
        surt.anchorMax = new Vector2(1f, 0.36f);
        surt.offsetMin = new Vector2(2f, 2f);
        surt.offsetMax = new Vector2(-2f, 0f);
        scoreUnit.alignment = TextAlignmentOptions.Center;
        scoreUnit.raycastTarget = false;

        // Однорядковий підзаголовок
        txtResultBody = CreateText(card.transform, "", 11, C_Secondary);
        txtResultBody.textWrappingMode = TextWrappingModes.NoWrap;
        txtResultBody.overflowMode = TextOverflowModes.Ellipsis;
        txtResultBody.alignment = TextAlignmentOptions.MidlineLeft;
        var brt = txtResultBody.rectTransform;
        brt.anchorMin = new Vector2(0f, 1f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(0f, 1f);
        brt.anchoredPosition = new Vector2(18f, -44f);
        brt.sizeDelta = new Vector2(-36f, 16f);

        // Чотири чіпи метрик в одному щільному ряду
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

            resultMetricKeys[i] = CreateText(chip.transform, keyPh[i], 10, C_Secondary, FontStyles.Bold);
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

        // Один ряд дій — три рівні кнопки, впритул під метриками
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
        MakeResultBtn("CloseResult", UILocale.T("btn_ok"), C_Btn, btnX0 + (btnW + btnGap) * 2f,
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

        txtProgress = CreateText(progressRoot.transform, UILocale.T("prog_start"), 13, UiTheme.ChromeText, FontStyles.Bold);
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

        float maxV = rocket.parameters != null ? rocket.parameters.maxTouchdownVelocity : LandingCriteria.DefaultMaxTouchdownVelocity;
        float maxA = rocket.parameters != null ? rocket.parameters.maxLandingAngle : LandingCriteria.DefaultMaxLandingAngle;
        float maxM = rocket.parameters != null ? rocket.parameters.maxHorizontalMiss : LandingCriteria.DefaultMaxHorizontalMiss;
        float maxH = rocket.parameters != null ? rocket.parameters.maxHorizontalSpeed : LandingCriteria.DefaultMaxHorizontalSpeed;

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

        // Живе відстеження піків / змін під час польоту
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
                // Лише ASCII-роздільники (без middle-dot «тофу»)
                float navR = rocket.navigation != null && rocket.navigation.Current.valid
                    ? rocket.navigation.Current.horizResid : 0f;
                txtDeltaStrip.text =
                    $"dh {Arrow(dAlt)}{Mathf.Abs(dAlt):F1}  |  dVy {Arrow(dVy)}{Mathf.Abs(dVy):F2}  |  " +
                    $"dTilt {Arrow(dTilt)}{Mathf.Abs(dTilt):F2}  |  NAV {navR:F1}m";
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

        // Soft-landing gate: name + current ≤ limit + NОРМА/ПОРУШЕННЯ/БЛИЗЬКО
        bool flying = rocket.simulationArmed && (s.time > 0.05f || s.simulationFinished || s.isLanded);
        bool landed = s.simulationFinished || s.isLanded;
        // Після touchdown віддавати перевагу замороженим метрикам
        if (landed && rocket.metrics != null && rocket.metrics.totalFlightTime > 0.05f)
        {
            av = rocket.metrics.touchdownVelocity;
            tilt = rocket.metrics.landingAngleError;
            miss = rocket.metrics.horizontalMiss;
            hVel = rocket.metrics.horizontalSpeed;
        }
        string ums = UILocale.T("u_ms");
        string um = UILocale.T("u_m");
        string udeg = "°";
        UpdateLandingGate(txtCritV, flying, av, maxV, 0.85f,
            UILocale.T("crit_vy"), ums, "F2", "F1");
        UpdateLandingGate(txtCritA, flying, tilt, maxA, 0.7f,
            UILocale.T("crit_tilt"), udeg, "F1", "F0");
        UpdateLandingGate(txtCritM, flying, miss, maxM, 0.7f,
            UILocale.T("crit_miss"), um, "F1", "F0");
        UpdateLandingGate(txtCritH, flying, hVel, maxH, 0.7f,
            UILocale.T("crit_vh"), ums, "F2", "F1");

        UpdateInsight(s, av, hVel, tilt, miss, twr, fuelPct, eta, maxV, maxA, maxM, maxH);

        bool exp = sim != null && sim.IsExperimentRunning;
        if (txtMode && !exp)
            txtMode.text = UILocale.ModeNameShort(rocket.controlMode);
        if (txtTime) txtTime.text = string.Format(UILocale.T("time_fmt"), s.time);

        if (txtStatus && !resultShown)
        {
            if (exp)
                SetStatusVisual("st_batch", C_Amber);
            else if (rocket.simulationPaused && rocket.simulationArmed && !s.simulationFinished)
                SetStatusVisual("st_pause", new Color(0.35f, 0.55f, 0.95f, 1f));
            else if (s.simulationFinished && rocket.simulationArmed == false && rocket.metrics != null
                     && (rocket.metrics.totalFlightTime > 0.1f || rocket.metrics.isSuccessfulLanding || rocket.metrics.timedOut))
            {
                // зупинено mid-flight — OnStop уже виставив badge
            }
            else if (s.simulationFinished && rocket.metrics != null && rocket.metrics.totalFlightTime > 0.05f)
            {
                bool ok = rocket.metrics.isSuccessfulLanding;
                SetStatusVisual(ok ? "st_success" : "st_fail", ok ? C_Ok : C_Alert);
                Write(txtScore, $"{rocket.metrics.SuccessScore:F0}", ok ? C_Ok : C_Alert);
            }
            else if (rocket.simulationArmed && rocket.phase == RocketPhysics.FlightPhase.Stack)
                SetStatusVisual("st_stack", C_Amber);
            else if (rocket.simulationArmed && s.time > 0.05f)
                SetStatusVisual("st_descent", C_Cyan);
            else if (rocket.simulationArmed)
                SetStatusVisual("st_start", C_Amber);
            else
                SetStatusVisual("st_wait", C_Muted);
        }

        // Підсвітити обраний режим; приглушити інші під час експерименту (завжди оновлювати чорнило підписів)
        for (int i = 0; i < modeButtons.Count; i++)
        {
            var b = modeButtons[i];
            if (b == null) continue;
            b.interactable = !exp;
            bool active = b.gameObject.name == "Mode_" + rocket.controlMode;
            Color bg;
            if (exp)
                bg = active ? Color.Lerp(C_Amber, C_Btn, 0.15f) : Color.Lerp(C_Btn, C_PanelSoft, 0.35f);
            else
                bg = active ? C_BtnActive : C_Btn;
            if (i < modeButtonImages.Count && modeButtonImages[i] != null)
                modeButtonImages[i].color = bg;
            foreach (var tmp in b.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null) continue;
                tmp.color = tmp.gameObject.name == "ModeSub"
                    ? ModeSubtitleOn(bg)
                    : ButtonLabelOn(bg);
            }
        }

        // Стрічкові графіки: одиночний політ + live MC-порівняння (reset лише на старті нового compare)
        bool expRunning = batchMode || (sim != null && sim.IsExperimentRunning);
        sampleTimer += Time.unscaledDeltaTime;
        // Швидший sample у batch, щоб multi-trial bursts лишали видимий слід
        float sampleDt = expRunning
            ? 0.03f
            : (s.position.y < 200f ? 0.04f : 0.07f);
        if (sampleTimer >= sampleDt
            && rocket.simulationArmed
            && s.time > 0.01f
            && !s.simulationFinished)
        {
            sampleTimer = 0f;
            graphAlt?.Push(s.position.y);
            graphVel?.Push(s.velocity.y);
            graphThr?.Push(s.currentThrust / 1000f);
        }

        // Підписи значень слайдера оновлюються через onValueChanged (з одиницями)
        UpdateFlightStep(s);

        // Тримати підпис камери синхронним, якщо користувач тиснув гарячі клавіші
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
            if (max <= 0.05f)
            {
                txtInfo.text = UILocale.T("msg_compare_zero");
                txtInfo.color = C_Alert;
            }
            else
            {
                txtInfo.text = string.Format(UILocale.T("msg_compare_done"), winner, max);
                txtInfo.color = C_Ok;
            }
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
            // Бліда заливка на light-темах — тримати чорнило (c) домінантним для контрасту
            float a = UiTheme.IsLightBackground ? 0.12f : 0.14f;
            img.color = new Color(c.r, c.g, c.b, a);
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
        // Лише ASCII (unicode-стрілки → «тофу» на деяких шрифтах)
        if (d > 0.05f) return "+";
        if (d < -0.05f) return "-";
        return "=";
    }

    void ApplySettings()
    {
        if (sim != null)
        {
            if (testsSlider) sim.testsPerAlgorithm = Mathf.RoundToInt(testsSlider.value);
            if (windSlider) sim.windStrength = windSlider.value;
            if (noiseToggle) sim.enableNoise = noiseToggle.isOn;
            if (timeScaleSlider) sim.experimentTimeScale = timeScaleSlider.value;
            else sim.experimentTimeScale = UserSettings.TimeScale;
            if (seedSlider) sim.experimentSeed = Mathf.RoundToInt(seedSlider.value);
            else sim.experimentSeed = UserSettings.ExperimentSeed;
            if (massNoiseSlider) sim.massVariationPercent = massNoiseSlider.value;
            else sim.massVariationPercent = UserSettings.MassNoise;
            if (angleNoiseSlider) sim.angleVariationDegrees = angleNoiseSlider.value;
            else sim.angleVariationDegrees = UserSettings.AngleNoise;
            if (heightSlider) sim.startHeight = heightSlider.value;
            else sim.startHeight = UserSettings.StartHeight;
            if (descentSlider) sim.startDescentSpeed = descentSlider.value;
            else sim.startDescentSpeed = UserSettings.StartDescentSpeed;
            if (tilt0Slider) sim.startTiltDeg = tilt0Slider.value;
            else sim.startTiltDeg = UserSettings.StartTilt;
        }
        if (trainToggle && rocket?.neuralController != null)
            rocket.neuralController.enableTraining = trainToggle.isOn;
        if (rocket?.hybridController != null)
        {
            bool res = residualToggle != null ? residualToggle.isOn : UserSettings.HybridResidual;
            rocket.hybridController.useNeuralResidual = res;
        }
        // Тримати ПУ ракети синхронними в idle (прев’ю на наступний Start / зміну режиму)
        if (rocket != null && !rocket.simulationArmed && (sim == null || !sim.IsExperimentRunning))
            ApplyExperimentInitialConditions();
    }

    /// <summary>Застосувати гнучкі ПУ з UI/налаштувань у SimulationParameters.</summary>
    void ApplyExperimentInitialConditions()
    {
        if (rocket?.parameters == null) return;
        float h0 = heightSlider != null ? heightSlider.value : UserSettings.StartHeight;
        float vy = descentSlider != null ? descentSlider.value : UserSettings.StartDescentSpeed;
        float tilt = tilt0Slider != null ? tilt0Slider.value : UserSettings.StartTilt;
        h0 = Mathf.Clamp(h0, 800f, 3000f);
        vy = Mathf.Clamp(vy, 30f, 120f);
        tilt = Mathf.Clamp(tilt, 0f, 12f);

        var p = rocket.parameters;
        p.startPosition = new Vector3(0f, h0, 0f);
        p.startVelocity = new Vector3(0f, -vy, 0f);
        p.startEulerAngles = new Vector3(0f, 0f, tilt);
        p.dryMass = 25600f;
        p.fuelMass = 14000f;
        p.maxThrust = 845000f;

        if (sim != null)
        {
            sim.startHeight = h0;
            sim.startDescentSpeed = vy;
            sim.startTiltDeg = tilt;
        }

        // Прев’ю припаркованої ракети на нових ПУ, коли не летить
        if (!rocket.simulationArmed && !rocket.state.simulationFinished)
        {
            rocket.state.position = p.startPosition;
            rocket.state.velocity = p.startVelocity;
            rocket.state.rotation = Quaternion.Euler(p.startEulerAngles);
            rocket.SyncTransformWithState();
        }
    }

    /// <summary>Номінальні IC з поточних слайдерів експерименту.</summary>
    void RestoreNominalInitialConditions() => ApplyExperimentInitialConditions();

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
        // Простий білий спрайт — уникає глітчів товщини default 9-slice UI sprite
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
        // Підпис секції + hairline — той самий рецепт, що SectionHeader
        var t = CreateText(parent, title, 10, C_Header, FontStyles.Bold);
        t.gameObject.name = "SecHdr";
        t.characterSpacing = 3.2f;
        PinTL(t.rectTransform, pad, y, width, 15);
        y -= 16f;

        var line = CreatePanel("HeaderLine", parent, HeaderLineColor);
        line.GetComponent<Image>().raycastTarget = false;
        PinTL(line.GetComponent<RectTransform>(), pad, y, width, 1.5f);
        y -= 10f;
        return t;
    }

    TMP_Text Metric(Transform parent, string key, string unit, ref float y,
        float pad = 14f, float width = 300f, bool primary = false)
    {
        // підпис ліворуч | значення right-aligned | одиниця край праворуч
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

    /// <summary>
    /// Soft-landing gate cell: title, "value ≤ limit unit", status NОРМА/ПОРУШЕННЯ/БЛИЗЬКО/—.
    /// </summary>
    static void UpdateLandingGate(TMP_Text t, bool active, float value, float limit, float warnFrac,
        string title, string unit, string valFmt, string limFmt)
    {
        if (t == null) return;

        bool pass = value < limit;
        bool warn = pass && value >= limit * Mathf.Clamp01(warnFrac);

        string status;
        Color ink;
        if (!active)
        {
            status = "—";
            ink = C_Muted;
        }
        else if (!pass)
        {
            status = UILocale.T("crit_fail");
            ink = C_Alert;
        }
        else if (warn)
        {
            status = UILocale.T("crit_warn");
            ink = C_Amber;
        }
        else
        {
            status = UILocale.T("crit_ok");
            ink = C_Ok;
        }

        string lim = limit.ToString(limFmt);
        string valLine;
        if (!active)
            valLine = $"{UILocale.T("crit_idle")} ≤ {lim} {unit}";
        else if (pass)
            valLine = $"{value.ToString(valFmt)} ≤ {lim} {unit}";
        else
            valLine = $"{value.ToString(valFmt)} > {lim} {unit}";

        t.text = title + "\n" + valLine + "\n" + status;
        t.color = ink;

        var img = t.transform.parent != null ? t.transform.parent.GetComponent<Image>() : null;
        if (img != null)
        {
            float a = UiTheme.IsLightBackground
                ? (!active ? 0.06f : 0.12f)
                : (!active ? 0.08f : pass ? (warn ? 0.14f : 0.12f) : 0.18f);
            Color wash = !active ? C_PanelSoft : ink;
            img.color = new Color(wash.r, wash.g, wash.b, a);
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

        if (rocket.phase == RocketPhysics.FlightPhase.Stack)
        {
            txtInsight.text = UILocale.T("ins_stack");
            txtInsight.color = C_Amber;
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
        // зсунути bar трохи під колонку значень метрик
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

        // Root тримає frame + plot + labels (labels останні = зверху)
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
        g.BindLabelRoot(rootRt); // підписи як siblings plot, зверху
        g.Configure(title, unit, line, threshold);

        y -= gH + 10f;
        return g;
    }

    void BuildStepBar(Transform parent)
    {
        // Плаваюча смуга фази — узгоджена з chrome бічної панелі, ліва акцентна смужка
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
        else if (rocket.phase == RocketPhysics.FlightPhase.Stack)
        {
            key = "step_stack";
            col = C_Amber;
        }
        else if (h >= 400f) { key = "step_high"; col = C_Cyan; }
        else if (h >= 100f) { key = "step_approach"; col = C_Cyan; }
        else if (h >= 25f) { key = "step_powered"; col = C_Amber; }
        else if (h >= 6f) { key = "step_terminal"; col = C_Amber; }
        else if (h >= 2f) { key = "step_soft"; col = C_Ok; }
        else { key = "step_touch"; col = C_Ok; }

        string mode = rocket.phase == RocketPhysics.FlightPhase.Stack
            ? "STACK"
            : UILocale.ModeNameShort(rocket.controlMode);
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
        ApplyBtnColorBlock(ref colors);
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
            // Читабельно на світлих чіпах (сам C_Muted був надто блідий / зеленуватий на деяких темах)
            var sub = CreateText(go.transform, subtitle, 10, ModeSubtitleOn(C_Btn));
            sub.gameObject.name = "ModeSub";
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
        => ActionButtonAt(parent, x, y, w, h, label, "Action", col, action);

    void ActionButtonAt(Transform parent, float x, float y, float w, float h,
        string label, string goName, Color col, UnityEngine.Events.UnityAction action)
    {
        // Тримати суцільні акцентні заливки (сімейство Start/Stop) — не розмивати на light-темах
        Color bg = col;
        bg.a = 1f;
        var go = CreatePanel(goName, parent, bg);
        PinTL(go.GetComponent<RectTransform>(), x, y, w, h);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        var colors = btn.colors;
        ApplyBtnColorBlock(ref colors);
        btn.colors = colors;
        Color tc = ButtonLabelOn(bg);
        var txt = CreateText(go.transform, label, 11, tc, FontStyles.Bold);
        StretchFull(txt.rectTransform, 4, 2, 4, 2);
        txt.alignment = TextAlignmentOptions.Center;
        txt.overflowMode = TextOverflowModes.Ellipsis;
        txt.textWrappingMode = TextWrappingModes.NoWrap;
        txt.raycastTarget = false;
        btn.onClick.AddListener(action);
    }

    /// <summary>Theme-aware підпис на заливці кнопки (та сама родина, що текст панелі).</summary>
    static Color ButtonLabelOn(Color bg) => UiTheme.LabelOnFill(bg);

    /// <summary>Множники ColorTint — light-теми лишаються яскравими при натисканні.</summary>
    static void ApplyBtnColorBlock(ref ColorBlock colors)
    {
        colors.normalColor = Color.white;
        colors.fadeDuration = 0.05f;
        if (UiTheme.IsLightBackground)
        {
            colors.highlightedColor = new Color(1.05f, 1.06f, 1.09f, 1f);
            colors.pressedColor = new Color(0.95f, 0.97f, 1f, 1f);
            colors.selectedColor = new Color(1.03f, 1.04f, 1.07f, 1f);
        }
        else
        {
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.12f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.88f, 1f);
            colors.selectedColor = Color.white;
        }
    }

    // Спільна палітра дій — top Start/Stop/Pause/Demo і right Compare/Cancel
    static Color BtnGreen() => UiTheme.IsLightBackground
        ? new Color(0.12f, 0.50f, 0.30f, 1f)
        : new Color(0.14f, 0.40f, 0.26f, 1f);
    static Color BtnRed() => UiTheme.IsLightBackground
        ? new Color(0.70f, 0.18f, 0.18f, 1f)
        : new Color(0.50f, 0.14f, 0.14f, 1f);
    static Color BtnBlue() => UiTheme.IsLightBackground
        ? new Color(0.14f, 0.32f, 0.55f, 1f)
        : new Color(0.12f, 0.26f, 0.45f, 1f);
    /// <summary>Amber тієї ж насиченості, що Start/Stop (не розмитий theme-amber).</summary>
    static Color BtnAmber() => UiTheme.IsLightBackground
        ? new Color(0.78f, 0.48f, 0.06f, 1f)
        : new Color(0.68f, 0.42f, 0.08f, 1f);
    /// <summary>Violet — та сама смуга яскравості, що Pause blue.</summary>
    static Color BtnViolet() => UiTheme.IsLightBackground
        ? new Color(0.42f, 0.22f, 0.62f, 1f)
        : new Color(0.36f, 0.20f, 0.52f, 1f);
    /// <summary>Rose/pink — та сама смуга яскравості, що Stop red.</summary>
    static Color BtnPink() => UiTheme.IsLightBackground
        ? new Color(0.72f, 0.22f, 0.42f, 1f)
        : new Color(0.55f, 0.16f, 0.32f, 1f);

    /// <summary>Другий рядок на картках режимів — theme muted з контрастом на заливці.</summary>
    static Color ModeSubtitleOn(Color bg)
    {
        float l = 0.2126f * bg.r + 0.7152f * bg.g + 0.0722f * bg.b;
        Color muted = UiTheme.Current.Muted;
        if (l > 0.55f)
        {
            // Блідий чіп: muted має лишатись достатньо темним
            float ml = 0.2126f * muted.r + 0.7152f * muted.g + 0.0722f * muted.b;
            if (ml > 0.5f)
                return Color.Lerp(muted, UiTheme.Current.Text, 0.55f);
            return muted;
        }
        // Темний/active чіп: підтягнути muted до тексту light-теми
        return Color.Lerp(muted, UiTheme.TextOnDark, 0.45f);
    }

    void PlaceConditionSection(Transform root, ref float y, float pad, float inner, float gap)
    {
        float halfW = (inner - gap) * 0.5f;
        if (conditionSectionGo != null && conditionLookVer != kSliderLook)
        {
            Destroy(conditionSectionGo);
            conditionSectionGo = null;
            conditionLabelBindings.Clear();
            conditionToggleBindings.Clear();
            heightSlider = descentSlider = tilt0Slider = windSlider = null;
            massNoiseSlider = angleNoiseSlider = liveSpeedSlider = null;
            testsSlider = timeScaleSlider = seedSlider = null;
            noiseToggle = trainToggle = residualToggle = null;
        }
        bool fresh = conditionSectionGo == null;

        if (fresh)
        {
            conditionSectionGo = CreatePanel("ConditionSection", transform, new Color(0, 0, 0, 0));
            conditionSectionGo.GetComponent<Image>().raycastTarget = false;
            conditionLabelBindings.Clear();
            conditionToggleBindings.Clear();

            var sec = conditionSectionGo.transform;
            float ly = 0f;

            SectionHeader(sec, "h_step3", ref ly, pad, inner);
            SliderLine(sec, UILocale.T("sl_h0"), UILocale.T("sl_h0_u"),
                800, 3000, UserSettings.StartHeight, ref ly, out heightSlider, pad, inner, "sl_h0", "sl_h0_u");
            SliderLine(sec, UILocale.T("sl_vy0"), UILocale.T("sl_vy0_u"),
                30, 120, UserSettings.StartDescentSpeed, ref ly, out descentSlider, pad, inner, "sl_vy0", "sl_vy0_u");
            SliderLine(sec, UILocale.T("sl_tilt0"), UILocale.T("sl_tilt0_u"),
                0, 12, UserSettings.StartTilt, ref ly, out tilt0Slider, pad, inner, "sl_tilt0", "sl_tilt0_u");
            txtWindVal = SliderLine(sec, UILocale.T("sl_wind"), UILocale.T("sl_wind_u"),
                0, 25, UserSettings.Wind, ref ly, out windSlider, pad, inner, "sl_wind", "sl_wind_u");
            SliderLine(sec, UILocale.T("sl_massn"), UILocale.T("sl_massn_u"),
                0, 15, UserSettings.MassNoise, ref ly, out massNoiseSlider, pad, inner, "sl_massn", "sl_massn_u");
            SliderLine(sec, UILocale.T("sl_angn"), UILocale.T("sl_angn_u"),
                0, 15, UserSettings.AngleNoise, ref ly, out angleNoiseSlider, pad, inner, "sl_angn", "sl_angn_u");
            SliderLine(sec, UILocale.T("sl_live"), UILocale.T("sl_live_u"),
                1, 8, UserSettings.LiveTimeScale, ref ly, out liveSpeedSlider, pad, inner, "sl_live", "sl_live_u");

            noiseToggle = ToggleAt(sec, pad, ly, halfW, 26f, UILocale.T("tg_noise"), UserSettings.Noise, "tg_noise");
            trainToggle = ToggleAt(sec, pad + halfW + gap, ly, halfW, 26f, UILocale.T("tg_train"), UserSettings.Train, "tg_train");
            ly -= 32f;

            SectionHeader(sec, "h_mc", ref ly, pad, inner);
            txtTestsVal = SliderLine(sec, UILocale.T("sl_tests"), UILocale.T("sl_tests_u"),
                5, 40, UserSettings.Tests, ref ly, out testsSlider, pad, inner, "sl_tests", "sl_tests_u");
            SliderLine(sec, UILocale.T("sl_time"), UILocale.T("sl_time_u"),
                1, 40, UserSettings.TimeScale, ref ly, out timeScaleSlider, pad, inner, "sl_time", "sl_time_u");
            txtSeedVal = SliderLine(sec, UILocale.T("sl_seed"), UILocale.T("sl_seed_u"),
                1, 999, UserSettings.ExperimentSeed, ref ly, out seedSlider, pad, inner, "sl_seed", "sl_seed_u");
            residualToggle = ToggleAt(sec, pad, ly, inner, 26f, UILocale.T("tg_residual"), UserSettings.HybridResidual, "tg_residual");
            ly -= 32f;

            conditionSectionHeight = Mathf.Max(40f, -ly);
            conditionLookVer = kSliderLook;
        }
        else
        {
            // Перев’язати refs (ті самі об’єкти, новий parent правої панелі)
            var sliders = conditionSectionGo.GetComponentsInChildren<Slider>(true);
            if (sliders.Length >= 10)
            {
                heightSlider = sliders[0];
                descentSlider = sliders[1];
                tilt0Slider = sliders[2];
                windSlider = sliders[3];
                massNoiseSlider = sliders[4];
                angleNoiseSlider = sliders[5];
                liveSpeedSlider = sliders[6];
                testsSlider = sliders[7];
                timeScaleSlider = sliders[8];
                seedSlider = sliders[9];
            }
            var toggles = conditionSectionGo.GetComponentsInChildren<Toggle>(true);
            if (toggles.Length >= 3)
            {
                noiseToggle = toggles[0];
                trainToggle = toggles[1];
                residualToggle = toggles[2];
            }
            // Посилання display-value досі вказують на текстові компоненти NumField
            if (windSlider != null)
            {
                var inp = windSlider.transform.parent.GetComponentInChildren<TMP_InputField>(true);
                txtWindVal = inp != null ? inp.textComponent : txtWindVal;
            }
            if (testsSlider != null)
            {
                var inp = testsSlider.transform.parent.GetComponentInChildren<TMP_InputField>(true);
                txtTestsVal = inp != null ? inp.textComponent : txtTestsVal;
            }
            if (seedSlider != null)
            {
                var inp = seedSlider.transform.parent.GetComponentInChildren<TMP_InputField>(true);
                txtSeedVal = inp != null ? inp.textComponent : txtSeedVal;
            }
            RefreshConditionLabels();
        }

        conditionSectionGo.SetActive(true);
        conditionSectionGo.transform.SetParent(root, false);
        var rt = conditionSectionGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(0f, conditionSectionHeight);
        y -= conditionSectionHeight + 4f;
    }

    void SectionHeader(Transform parent, string localeKey, ref float y, float pad, float width)
    {
        // Точно збігтись з геометрією й кольорами Header() (раніше зсув лінії + accent-green)
        var t = CreateText(parent, UILocale.T(localeKey), 10, C_Header, FontStyles.Bold);
        t.gameObject.name = "SecHdr";
        t.characterSpacing = 3.2f;
        conditionLabelBindings.Add((t, null, localeKey, null));
        PinTL(t.rectTransform, pad, y, width, 15f);
        y -= 16f;
        var line = CreatePanel("HeaderLine", parent, HeaderLineColor);
        line.GetComponent<Image>().raycastTarget = false;
        PinTL(line.GetComponent<RectTransform>(), pad, y, width, 1.5f);
        y -= 10f;
    }

    TMP_Text SliderLine(Transform parent, string label, string unit, float min, float max, float val,
        ref float y, out Slider slider, float pad = 12f, float width = 314f,
        string labelKey = null, string unitKey = null)
    {
        // Фіксована геометрія — підпис + числове поле + одиниця + доріжка
        const float blockH = 48f;
        const float labelH = 18f;
        const float trackH = 4f;
        const float knob = 12f;
        const float trackPadX = 6f;
        const float inputW = 58f;
        const float unitW = 36f;

        Color trackCol = Color.Lerp(C_Edge, C_PanelSoft, UiTheme.IsLightBackground ? 0.25f : 0.35f);
        trackCol.a = 1f;
        Color fillCol = C_Accent; fillCol.a = 1f;
        Color handleCol = C_Amber; handleCol.a = 1f;
        Color labelCol = C_Text; labelCol.a = 0.92f;
        Color fieldBg = NumFieldBg();

        string unitS = string.IsNullOrEmpty(unit) ? "" : unit;
        val = Mathf.Clamp(val, min, max);

        // ── Контейнер блоку ──
        var block = CreatePanel("SliderBlock", parent, C_PanelSoft);
        block.GetComponent<Image>().raycastTarget = false;
        PinTL(block.GetComponent<RectTransform>(), pad, y, width, blockH);

        // Підпис (ліворуч)
        var k = CreateText(block.transform, label ?? "", 12, labelCol, FontStyles.Normal);
        k.gameObject.name = "SliderLabel";
        k.raycastTarget = false;
        k.overflowMode = TextOverflowModes.Ellipsis;
        k.textWrappingMode = TextWrappingModes.NoWrap;
        k.fontWeight = FontWeight.Regular;
        var krt = k.rectTransform;
        krt.anchorMin = new Vector2(0f, 1f);
        krt.anchorMax = new Vector2(1f, 1f);
        krt.pivot = new Vector2(0f, 1f);
        krt.anchoredPosition = new Vector2(8f, -4f);
        krt.sizeDelta = new Vector2(-(inputW + unitW + 20f), labelH);

        // Одиниця (край праворуч)
        var uLab = CreateText(block.transform, unitS, 11, C_Muted, FontStyles.Normal);
        uLab.gameObject.name = "SliderUnit";
        uLab.raycastTarget = false;
        uLab.alignment = TextAlignmentOptions.MidlineLeft;
        uLab.overflowMode = TextOverflowModes.Overflow;
        var urt = uLab.rectTransform;
        urt.anchorMin = urt.anchorMax = new Vector2(1f, 1f);
        urt.pivot = new Vector2(1f, 1f);
        urt.anchoredPosition = new Vector2(-6f, -4f);
        urt.sizeDelta = new Vector2(unitW, labelH);

        if (!string.IsNullOrEmpty(labelKey) || !string.IsNullOrEmpty(unitKey))
            conditionLabelBindings.Add((k, uLab, labelKey, unitKey));

        // Числове введення (лише цифри) — клік для вводу; focus має бути очевидним
        var fieldGo = new GameObject("NumField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fieldGo.transform.SetParent(block.transform, false);
        var frtIn = fieldGo.GetComponent<RectTransform>();
        frtIn.anchorMin = frtIn.anchorMax = new Vector2(1f, 1f);
        frtIn.pivot = new Vector2(1f, 1f);
        frtIn.anchoredPosition = new Vector2(-(unitW + 8f), -3f);
        frtIn.sizeDelta = new Vector2(inputW, labelH + 2f);
        var fieldImg = fieldGo.GetComponent<Image>();
        StyleSimpleImage(fieldImg, fieldBg);
        fieldImg.raycastTarget = true;

        // Завжди видима рамка, щоб light-теми показували поле; акцент товщає у фокусі
        var focusOutline = fieldGo.AddComponent<Outline>();
        focusOutline.effectColor = NumFieldEdge();
        focusOutline.effectDistance = UiTheme.IsLightBackground
            ? new Vector2(1.2f, -1.2f)
            : new Vector2(1f, -1f);
        focusOutline.useGraphicAlpha = false;
        focusOutline.enabled = true;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(fieldGo.transform, false);
        var v = textGo.GetComponent<TextMeshProUGUI>();
        UiTypography.Apply(v, 12, C_Text, FontStyles.Normal);
        v.fontStyle = FontStyles.Normal;
        v.fontWeight = FontWeight.Regular;
        v.alignment = TextAlignmentOptions.MidlineRight;
        v.overflowMode = TextOverflowModes.Overflow;
        v.textWrappingMode = TextWrappingModes.NoWrap;
        v.raycastTarget = true;
        v.text = Mathf.RoundToInt(val).ToString();
        StretchFull(v.rectTransform, 4, 1, 4, 1);

        var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        phGo.transform.SetParent(fieldGo.transform, false);
        var ph = phGo.GetComponent<TextMeshProUGUI>();
        UiTypography.Apply(ph, 12, C_Muted, FontStyles.Normal);
        ph.alignment = TextAlignmentOptions.MidlineRight;
        ph.text = "";
        ph.raycastTarget = false;
        StretchFull(ph.rectTransform, 4, 1, 4, 1);

        var input = fieldGo.AddComponent<TMP_InputField>();
        input.textViewport = frtIn;
        input.textComponent = v;
        input.placeholder = ph;
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.characterValidation = TMP_InputField.CharacterValidation.Integer;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 5;
        input.caretWidth = 2;
        input.caretBlinkRate = 0.85f;
        input.customCaretColor = true;
        input.caretColor = C_Accent;
        input.selectionColor = new Color(C_Accent.r, C_Accent.g, C_Accent.b,
            UiTheme.IsLightBackground ? 0.28f : 0.4f);
        input.targetGraphic = fieldImg;
        input.transition = Selectable.Transition.ColorTint;
        var ic = ColorBlock.defaultColorBlock;
        Color idleBg = fieldBg;
        Color focusBg = NumFieldFocusBg(fieldBg);
        ic.normalColor = idleBg;
        ic.highlightedColor = Color.Lerp(idleBg, C_Accent, 0.18f);
        ic.pressedColor = focusBg;
        ic.selectedColor = focusBg;
        ic.disabledColor = new Color(idleBg.r, idleBg.g, idleBg.b, 0.45f);
        ic.colorMultiplier = 1f;
        ic.fadeDuration = 0.06f;
        input.colors = ic;
        // ColorTint множить graphic.color — лишати білим, щоб ColorBlock керував заливкою
        fieldImg.color = Color.white;
        input.text = Mathf.RoundToInt(val).ToString();
        // Зберігати посилання display TMP_Text для legacy-оновлень txtSeedVal
        TMP_Text displayVal = v;

        // ── Hit-зона слайдера (нижня половина блоку) ──
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
        // Локальне захоплення — out-параметр не можна в лямбдах (CS1628)
        var sld = slider;
        float minV = min;
        float maxV = max;
        sld.minValue = minV;
        sld.maxValue = maxV;
        sld.wholeNumbers = true;
        sld.direction = Slider.Direction.LeftToRight;
        sld.transition = Selectable.Transition.None;
        sld.navigation = new Navigation { mode = Navigation.Mode.None };
        slideGo.AddComponent<SliderScrollLock>();

        // Фонова доріжка (фіксована висота через center anchors + sizeDelta.y)
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

        // Fill Area — стандартний layout Unity (висота зафіксована)
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
        // Unity Slider керує anchorMax.x; y-anchors тримати на всю fill area
        fr.anchorMin = new Vector2(0f, 0f);
        fr.anchorMax = new Vector2(0f, 1f);
        fr.offsetMin = Vector2.zero;
        fr.offsetMax = Vector2.zero;
        fr.pivot = new Vector2(0f, 0.5f);

        // Область руху handle
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

        sld.fillRect = fr;
        sld.handleRect = hr;
        sld.targetGraphic = hImg;

        // Повторно зафіксувати розмір handle після того, як Slider змінює anchors на першому Set
        void LockHandle()
        {
            if (hr == null) return;
            hr.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, knob);
            hr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, knob);
        }

        bool syncing = false;
        void SetNumText(float x)
        {
            if (input == null) return;
            string t = Mathf.RoundToInt(x).ToString();
            if (input.text != t)
            {
                syncing = true;
                input.SetTextWithoutNotify(t);
                syncing = false;
            }
            if (displayVal != null && !input.isFocused)
                displayVal.text = t;
        }

        sld.onValueChanged.AddListener(x =>
        {
            if (!input.isFocused)
                SetNumText(x);
            LockHandle();
        });

        input.onValueChanged.AddListener(s =>
        {
            if (syncing || loadingSettings) return;
            if (string.IsNullOrEmpty(s)) return;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] < '0' || s[i] > '9')
                {
                    syncing = true;
                    input.SetTextWithoutNotify(FilterDigits(s));
                    syncing = false;
                    return;
                }
            }
        });

        input.onEndEdit.AddListener(s =>
        {
            if (loadingSettings) return;
            if (!int.TryParse(FilterDigits(s), out int n))
                n = Mathf.RoundToInt(sld.value);
            n = Mathf.Clamp(n, Mathf.RoundToInt(minV), Mathf.RoundToInt(maxV));
            syncing = true;
            input.SetTextWithoutNotify(n.ToString());
            syncing = false;
            if (Mathf.Abs(sld.value - n) > 0.01f)
                sld.value = n;
            else
                SetNumText(n);
            LockHandle();
        });

        input.onSelect.AddListener(_ =>
        {
            input.selectionAnchorPosition = 0;
            input.selectionFocusPosition = input.text.Length;
            if (focusOutline != null)
            {
                var oc = C_Accent; oc.a = 1f;
                focusOutline.effectColor = oc;
                focusOutline.effectDistance = new Vector2(2.5f, -2.5f);
                focusOutline.enabled = true;
            }
            input.caretWidth = 2;
            input.customCaretColor = true;
            input.caretColor = C_Accent;
        });
        input.onDeselect.AddListener(_ =>
        {
            if (focusOutline != null)
            {
                focusOutline.effectColor = NumFieldEdge();
                focusOutline.effectDistance = UiTheme.IsLightBackground
                    ? new Vector2(1.2f, -1.2f)
                    : new Vector2(1f, -1f);
                focusOutline.enabled = true;
            }
        });

        sld.SetValueWithoutNotify(val);
        SetNumText(val);
        LockHandle();
        Canvas.ForceUpdateCanvases();
        LockHandle();

        y -= blockH + 6f;
        return displayVal;
    }

    static string FilterDigits(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new System.Text.StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
            if (s[i] >= '0' && s[i] <= '9') sb.Append(s[i]);
        return sb.ToString();
    }

    /// <summary>Допоміжний numeric display (input field — основний).</summary>
    static string FormatSliderValue(float val, string unitS)
    {
        int n = Mathf.RoundToInt(val);
        string num = n.ToString();
        return string.IsNullOrEmpty(unitS) ? num : (num + " " + unitS.Trim());
    }

    Toggle ToggleAt(Transform parent, float x, float y, float w, float h, string label, bool on,
        string labelKey = null)
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
        txt.gameObject.name = "ToggleLabel";
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
        if (!string.IsNullOrEmpty(labelKey))
            conditionToggleBindings.Add((toggle, txt, labelKey));
        return toggle;
    }

    TMP_Text StatBadge(Transform parent, float x, float y, float w, float h, string name)
    {
        var bg = CreatePanel("StatBadge", parent, C_PanelSoft);
        bg.GetComponent<Image>().raycastTarget = false;
        PinTL(bg.GetComponent<RectTransform>(), x, y, w, h);

        var k = CreateText(bg.transform, name, 10, C_Secondary);
        k.gameObject.name = "StatLabel";
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
        // Цілі позиції/розміри → чіткіший TMP під CanvasScaler
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
/// Під час drag Slider у ScrollRect вимикати scroll, щоб рухався handle.
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
