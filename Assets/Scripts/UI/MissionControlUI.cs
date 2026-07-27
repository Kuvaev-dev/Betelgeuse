using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Mission Control UI (runtime): top bar, left telemetry+graphs, right controls.
/// Ховає зламаний scene-UI; центр екрана вільний для 3D.
/// </summary>
[DefaultExecutionOrder(-50)]
public class MissionControlUI : MonoBehaviour
{
    public static MissionControlUI Instance { get; private set; }

    RocketPhysics rocket;
    SimulationManager sim;

    TMP_Text txtAlt, txtVel, txtThr, txtTilt, txtFuel, txtMiss, txtMode, txtStatus, txtTime, txtScore;
    TMP_Text txtPid, txtFuzzy, txtNeural, txtHybrid, txtWinner, txtInfo;
    TMP_Text txtWindVal, txtTestsVal;

    Slider windSlider, testsSlider, timeScaleSlider;
    Toggle noiseToggle, trainToggle;
    Image thrBarFill, fuelBarFill, tiltBarFill;
    TelemetryGraph graphAlt, graphVel, graphThr;
    readonly List<Button> modeButtons = new();

    float sampleTimer;
    bool built;

    static readonly Color C_Panel = new(0.04f, 0.07f, 0.13f, 0.94f);
    static readonly Color C_PanelSoft = new(0.06f, 0.1f, 0.18f, 0.9f);
    static readonly Color C_Edge = new(0.15f, 0.28f, 0.45f, 1f);
    static readonly Color C_Cyan = new(0.25f, 0.9f, 1f, 1f);
    static readonly Color C_Amber = new(1f, 0.72f, 0.2f, 1f);
    static readonly Color C_Ok = new(0.3f, 1f, 0.55f, 1f);
    static readonly Color C_Alert = new(1f, 0.35f, 0.4f, 1f);
    static readonly Color C_Text = new(0.9f, 0.94f, 1f, 1f);
    static readonly Color C_Muted = new(0.5f, 0.58f, 0.7f, 1f);
    static readonly Color C_Btn = new(0.08f, 0.16f, 0.28f, 1f);
    static readonly Color C_BtnActive = new(0.1f, 0.35f, 0.45f, 1f);

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
            // Don't hide our own canvas
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
    }

    void BuildTopBar(Transform parent)
    {
        var bar = CreatePanel("TopBar", parent, C_Panel);
        DockTop(bar.GetComponent<RectTransform>(), 64f);
        Outline(bar);

        var title = CreateText(bar.transform, "BETELGEUSE  ·  GNC MISSION CONTROL", 20, C_Cyan, FontStyles.Bold);
        Pin(title.rectTransform, 0, 0.5f, 0, 0.5f, 20, 0, 480, 36);

        txtMode = CreateText(bar.transform, "MODE  —", 17, C_Amber, FontStyles.Bold);
        Pin(txtMode.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 300, 34);

        txtTime = CreateText(bar.transform, "T+ 0.0 s", 15, C_Text);
        Pin(txtTime.rectTransform, 1, 0.5f, 1, 0.5f, -320, 0, 140, 30);

        txtStatus = CreateText(bar.transform, "STATUS  READY", 15, C_Muted, FontStyles.Bold);
        Pin(txtStatus.rectTransform, 1, 0.5f, 1, 0.5f, -20, 0, 280, 30);
        txtStatus.alignment = TextAlignmentOptions.Right;
    }

    void BuildLeftPanel(Transform parent)
    {
        var panel = CreatePanel("LeftPanel", parent, C_Panel);
        DockLeft(panel.GetComponent<RectTransform>(), 16, 64, 16, 340);
        Outline(panel);

        float y = -14f;
        Header(panel.transform, "TELEMETRY", ref y);

        txtAlt = Metric(panel.transform, "ALTITUDE", ref y);
        txtVel = Metric(panel.transform, "VERT. VEL", ref y);
        txtThr = Metric(panel.transform, "THRUST", ref y);
        thrBarFill = MakeBar(panel.transform, ref y, C_Cyan);
        txtTilt = Metric(panel.transform, "TILT", ref y);
        tiltBarFill = MakeBar(panel.transform, ref y, C_Amber);
        txtFuel = Metric(panel.transform, "FUEL", ref y);
        fuelBarFill = MakeBar(panel.transform, ref y, C_Ok);
        txtMiss = Metric(panel.transform, "PAD MISS", ref y);
        txtScore = Metric(panel.transform, "SCORE", ref y);

        y -= 6f;
        Header(panel.transform, "LIVE CHARTS", ref y);
        graphAlt = MakeGraph(panel.transform, "ALTITUDE  m", C_Cyan, ref y);
        graphVel = MakeGraph(panel.transform, "VELOCITY  m/s", C_Amber, ref y);
        graphThr = MakeGraph(panel.transform, "THRUST  kN", new Color(0.45f, 0.95f, 0.45f), ref y);
    }

    void BuildRightPanel(Transform parent)
    {
        var panel = CreatePanel("RightPanel", parent, C_Panel);
        DockRight(panel.GetComponent<RectTransform>(), 16, 64, 16, 340);
        Outline(panel);

        float y = -14f;
        Header(panel.transform, "CONTROL MODE", ref y);

        modeButtons.Clear();
        modeButtons.Add(ModeButton(panel.transform, "PID", RocketPhysics.ControlMode.PID, ref y));
        modeButtons.Add(ModeButton(panel.transform, "FUZZY  SUGENO", RocketPhysics.ControlMode.Fuzzy, ref y));
        modeButtons.Add(ModeButton(panel.transform, "NEURAL  ES", RocketPhysics.ControlMode.Neural, ref y));
        modeButtons.Add(ModeButton(panel.transform, "HYBRID  N-F", RocketPhysics.ControlMode.Hybrid, ref y));

        y -= 4f;
        ActionButton(panel.transform, "▶   START / RESET", new Color(0.08f, 0.38f, 0.32f), ref y, () =>
        {
            if (rocket == null) return;
            ClearGraphs();
            rocket.ResetSimulation();
            FindFirstObjectByType<TrajectoryVisualizer>()?.Clear();
            if (txtInfo) txtInfo.text = $"Started {rocket.GetModeDisplayName()}";
        });

        ActionButton(panel.transform, "⚡   FULL MONTE-CARLO", new Color(0.38f, 0.28f, 0.08f), ref y, () =>
        {
            if (sim == null) { if (txtInfo) txtInfo.text = "SimulationManager not found"; return; }
            ApplySettings();
            sim.runFullExperiment = true;
            if (txtInfo) txtInfo.text = "Monte-Carlo running (accelerated)…";
        });

        y -= 6f;
        Header(panel.transform, "SETTINGS", ref y);
        txtTestsVal = SliderLine(panel.transform, "Runs / algorithm", 5, 40, 15, ref y, out testsSlider);
        txtWindVal = SliderLine(panel.transform, "Wind strength", 0, 25, 10, ref y, out windSlider);
        SliderLine(panel.transform, "Time scale", 1, 40, 20, ref y, out timeScaleSlider);
        noiseToggle = ToggleLine(panel.transform, "Monte-Carlo noise", true, ref y);
        trainToggle = ToggleLine(panel.transform, "NN online training", true, ref y);

        y -= 6f;
        Header(panel.transform, "SUCCESS RATE  %", ref y);
        txtPid = Stat(panel.transform, "PID", ref y);
        txtFuzzy = Stat(panel.transform, "FUZZY", ref y);
        txtNeural = Stat(panel.transform, "NEURAL", ref y);
        txtHybrid = Stat(panel.transform, "HYBRID", ref y);

        y -= 4f;
        txtWinner = CreateText(panel.transform, "BEST  — awaiting run", 14, C_Ok, FontStyles.Bold);
        PinTL(txtWinner.rectTransform, 16, y, 308, 26);
        y -= 30f;

        txtInfo = CreateText(panel.transform, "Оберіть режим і натисніть START. Графіки та телеметрія оновлюються live.", 12, C_Muted);
        txtInfo.enableWordWrapping = true;
        txtInfo.alignment = TextAlignmentOptions.TopLeft;
        PinTL(txtInfo.rectTransform, 16, y, 308, 60);
    }

    void BuildBottomBar(Transform parent)
    {
        var bar = CreatePanel("BottomBar", parent, C_PanelSoft);
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.offsetMin = new Vector2(370, 8);
        rt.offsetMax = new Vector2(-370, 52);
        Outline(bar);

        var t = CreateText(bar.transform,
            "OK: |Vy|<3.5 m/s · tilt<7° · miss<25 m · |Vh|<5   ·   RK4 · Sugeno-0 · MLP ES(1+λ) · Hybrid Neuro-Fuzzy",
            12, C_Muted);
        StretchFull(t.rectTransform, 8, 4, 8, 4);
        t.alignment = TextAlignmentOptions.Center;
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

        Write(txtAlt, $"{s.position.y:F1} m", s.position.y < 80f ? C_Amber : C_Text);
        float av = Mathf.Abs(s.velocity.y);
        Write(txtVel, $"{s.velocity.y:F1} m/s", av > 25f ? C_Alert : av > 8f ? C_Amber : C_Ok);
        Write(txtThr, $"{s.currentThrust / 1000f:F0} kN  ({thrPct * 100f:F0}%)", C_Text);
        Write(txtTilt, $"{tilt:F1} °", tilt > 7f ? C_Alert : tilt > 3f ? C_Amber : C_Text);
        Write(txtFuel, $"{s.currentFuelMass:F0} kg", fuelPct < 0.15f ? C_Alert : C_Text);
        Write(txtMiss, $"{miss:F1} m", miss > 25f ? C_Alert : C_Text);

        SetBar(thrBarFill, thrPct, C_Cyan);
        SetBar(fuelBarFill, fuelPct, C_Ok);
        SetBar(tiltBarFill, Mathf.Clamp01(tilt / 15f), tilt > 7f ? C_Alert : C_Amber);

        if (txtMode) txtMode.text = $"MODE  {rocket.GetModeDisplayName()}";
        if (txtTime) txtTime.text = $"T+ {s.time:F1} s";

        if (txtStatus)
        {
            if (s.simulationFinished)
            {
                bool ok = rocket.metrics != null && rocket.metrics.isSuccessfulLanding;
                txtStatus.text = ok ? "STATUS  TOUCHDOWN OK" : "STATUS  LANDING FAIL";
                txtStatus.color = ok ? C_Ok : C_Alert;
                if (rocket.metrics != null)
                    Write(txtScore, $"{rocket.metrics.SuccessScore:F0} / 100", ok ? C_Ok : C_Alert);
            }
            else if (s.time > 0.05f)
            {
                txtStatus.text = "STATUS  DESCENT";
                txtStatus.color = C_Cyan;
            }
            else
            {
                txtStatus.text = "STATUS  READY";
                txtStatus.color = C_Muted;
            }
        }

        foreach (var b in modeButtons)
        {
            if (b == null) continue;
            bool active = b.gameObject.name == "Mode_" + rocket.controlMode;
            var img = b.GetComponent<Image>();
            if (img) img.color = active ? C_BtnActive : C_Btn;
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
        if (fuzzy >= max) { max = fuzzy; winner = "Fuzzy Sugeno"; }
        if (neural >= max) { max = neural; winner = "Neural ES"; }
        if (hybrid > max) { max = hybrid; winner = "Hybrid N-F"; }
        if (txtWinner) { txtWinner.text = $"BEST  {winner}   ({max:F1}%)"; txtWinner.color = C_Ok; }
        if (txtInfo) txtInfo.text = $"Експеримент завершено. Переможець: {winner}.";
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
        // Prefer Input System package module if present
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

    static void Outline(GameObject go)
    {
        var o = go.AddComponent<UnityEngine.UI.Outline>();
        o.effectColor = C_Edge;
        o.effectDistance = new Vector2(1.2f, -1.2f);
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
        // Use default TMP font (LiberationSans SDF from project)
        return tmp;
    }

    void Header(Transform parent, string title, ref float y)
    {
        var t = CreateText(parent, title, 12, C_Cyan, FontStyles.Bold);
        PinTL(t.rectTransform, 14, y, 300, 20);
        y -= 22f;
        var line = CreatePanel("line", parent, C_Edge);
        PinTL(line.GetComponent<RectTransform>(), 14, y + 2, 312, 1);
        y -= 8f;
    }

    TMP_Text Metric(Transform parent, string key, ref float y)
    {
        var k = CreateText(parent, key, 11, C_Muted);
        PinTL(k.rectTransform, 16, y, 110, 18);
        var v = CreateText(parent, "—", 15, C_Text, FontStyles.Bold);
        v.alignment = TextAlignmentOptions.Right;
        PinTL(v.rectTransform, 130, y - 1, 190, 20);
        y -= 24f;
        return v;
    }

    static void Write(TMP_Text t, string value, Color c)
    {
        if (t == null) return;
        // Keep key prefix if metric stores only value
        t.text = value;
        t.color = c;
    }

    Image MakeBar(Transform parent, ref float y, Color fill)
    {
        var bg = CreatePanel("Bar", parent, new Color(0.02f, 0.04f, 0.07f, 1f));
        PinTL(bg.GetComponent<RectTransform>(), 16, y, 308, 9);
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
        y -= 17f;

        var go = new GameObject("Graph", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(parent, false);
        PinTL(go.GetComponent<RectTransform>(), 14, y, 312, 96);
        var raw = go.GetComponent<RawImage>();
        raw.color = Color.white;
        var g = go.AddComponent<TelemetryGraph>();
        g.lineColor = line;
        g.title = title;
        g.autoScale = true;
        y -= 106f;
        return g;
    }

    Button ModeButton(Transform parent, string label, RocketPhysics.ControlMode mode, ref float y)
    {
        var go = CreatePanel("Mode_" + mode, parent, C_Btn);
        PinTL(go.GetComponent<RectTransform>(), 14, y, 312, 34);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.75f, 0.92f, 1f);
        colors.pressedColor = C_Cyan;
        btn.colors = colors;

        var txt = CreateText(go.transform, label, 13, C_Text, FontStyles.Bold);
        StretchFull(txt.rectTransform, 4, 2, 4, 2);
        txt.alignment = TextAlignmentOptions.Center;

        btn.onClick.AddListener(() =>
        {
            if (rocket == null) return;
            rocket.controlMode = mode;
            ClearGraphs();
            rocket.ResetSimulation();
            FindFirstObjectByType<TrajectoryVisualizer>()?.Clear();
            if (txtInfo) txtInfo.text = $"Режим {label} — симуляція запущена";
        });
        y -= 40f;
        return btn;
    }

    void ActionButton(Transform parent, string label, Color col, ref float y, UnityEngine.Events.UnityAction action)
    {
        var go = CreatePanel("Action", parent, col);
        PinTL(go.GetComponent<RectTransform>(), 14, y, 312, 36);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        var txt = CreateText(go.transform, label, 13, C_Text, FontStyles.Bold);
        StretchFull(txt.rectTransform, 4, 2, 4, 2);
        txt.alignment = TextAlignmentOptions.Center;
        btn.onClick.AddListener(action);
        y -= 42f;
    }

    TMP_Text SliderLine(Transform parent, string label, float min, float max, float val, ref float y, out Slider slider)
    {
        var k = CreateText(parent, label, 11, C_Muted);
        PinTL(k.rectTransform, 16, y, 160, 16);
        var v = CreateText(parent, val.ToString("F0"), 12, C_Cyan, FontStyles.Bold);
        v.alignment = TextAlignmentOptions.Right;
        PinTL(v.rectTransform, 250, y, 70, 16);
        y -= 18f;

        var sGo = CreatePanel("SliderBG", parent, new Color(0.03f, 0.05f, 0.09f, 1f));
        PinTL(sGo.GetComponent<RectTransform>(), 16, y, 308, 16);
        slider = sGo.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = true;
        slider.value = val;

        var fillArea = CreatePanel("Fill Area", sGo.transform, new Color(0, 0, 0, 0));
        StretchFull(fillArea.GetComponent<RectTransform>(), 2, 3, 2, 3);
        fillArea.GetComponent<Image>().raycastTarget = false;
        var fill = CreatePanel("Fill", fillArea.transform, C_Cyan * 0.65f);
        StretchFull(fill.GetComponent<RectTransform>(), 0, 0, 0, 0);

        var handle = CreatePanel("Handle", sGo.transform, C_Amber);
        var hrt = handle.GetComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(12, 16);
        hrt.anchorMin = hrt.anchorMax = new Vector2(0, 0.5f);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = hrt;
        slider.targetGraphic = handle.GetComponent<Image>();
        y -= 24f;
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
        trt.sizeDelta = new Vector2(260, 20);

        var toggle = row.AddComponent<Toggle>();
        toggle.targetGraphic = box.GetComponent<Image>();
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = on;
        y -= 28f;
        return toggle;
    }

    TMP_Text Stat(Transform parent, string name, ref float y)
    {
        var k = CreateText(parent, name, 13, C_Muted);
        PinTL(k.rectTransform, 16, y, 100, 20);
        var v = CreateText(parent, "—", 14, C_Text, FontStyles.Bold);
        v.alignment = TextAlignmentOptions.Right;
        PinTL(v.rectTransform, 140, y, 170, 20);
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
        if (Mathf.Approximately(ax0, ax1) && Mathf.Approximately(ay0, ay1))
        {
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }
        else
        {
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }
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
