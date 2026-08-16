using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Branded loading screen — starfield only, spinning loader, refined status card.
/// Animations use unscaled time so they keep moving during bootstrap.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class SplashScreenUI : MonoBehaviour
{
    public static SplashScreenUI Instance { get; private set; }

    static readonly Color ColBg = new(0.012f, 0.014f, 0.02f, 1f);
    static readonly Color ColCard = new(0.04f, 0.045f, 0.062f, 0.94f);
    static readonly Color ColCardEdge = new(0.55f, 0.78f, 0.95f, 0.22f);
    static readonly Color ColAccent = new(0.58f, 0.88f, 1f, 1f);
    static readonly Color ColAmber = new(1f, 0.8f, 0.42f, 1f);
    static readonly Color ColMuted = new(0.58f, 0.62f, 0.72f, 1f);
    static readonly Color ColDim = new(0.42f, 0.46f, 0.55f, 1f);
    static readonly Color ColTrack = new(0.1f, 0.12f, 0.16f, 1f);
    static readonly Color ColFill = new(0.38f, 0.8f, 0.96f, 1f);
    static readonly Color ColBtn = new(0.1f, 0.12f, 0.16f, 0.92f);
    static readonly Color ColClose = new(0.68f, 0.22f, 0.24f, 0.95f);

    Image barFill;
    Image barGlow;
    RectTransform spinnerRt;
    Image spinnerArc;
    Image[] starImgs;
    float[] starPhase;
    float[] starBaseA;
    TMP_Text txtStatus;
    TMP_Text txtPct;
    string statusBase = "";
    float displayProgress;
    float targetProgress;
    bool fading;
    float animT;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EarlyBoot()
    {
        if (Instance != null) return;
        BorderlessWindow.ApplyBorderlessChrome();
        var go = new GameObject("Betelgeuse_Splash");
        DontDestroyOnLoad(go);
        go.AddComponent<SplashScreenUI>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureEventSystem();
        BuildUi();
        SetProgress(0.02f, UILocale.IsUK ? "Ініціалізація" : "Initializing");
    }

    void Update() => TickVisuals();

    void LateUpdate() => TickSpinnerOnly();

    /// <summary>
    /// Wall-clock spin so the arc keeps the correct angle after frame hitches
    /// and still advances every frame the main thread is free.
    /// </summary>
    void TickSpinnerOnly()
    {
        float wall = Time.realtimeSinceStartup;
        if (spinnerRt != null)
            spinnerRt.localRotation = Quaternion.Euler(0f, 0f, -wall * 280f);
        if (spinnerArc != null)
        {
            float breathe = 0.72f + 0.28f * (0.5f + 0.5f * Mathf.Sin(wall * 4f));
            var c = ColAccent;
            c.a = breathe;
            spinnerArc.color = c;
        }
    }

    void TickVisuals()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) dt = 1f / 60f;
        animT += dt;

        TickSpinnerOnly();

        displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, dt * 0.85f);
        if (barFill != null)
        {
            var rt = barFill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(Mathf.Clamp01(displayProgress), 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        if (barGlow != null)
        {
            float w = Mathf.Clamp01(displayProgress);
            if (w < 0.03f)
            {
                barGlow.enabled = false;
            }
            else
            {
                barGlow.enabled = true;
                float tip = Mathf.Clamp01(w);
                var gr = barGlow.rectTransform;
                gr.anchorMin = new Vector2(Mathf.Max(0f, tip - 0.06f), 0f);
                gr.anchorMax = new Vector2(tip, 1f);
                gr.offsetMin = Vector2.zero;
                gr.offsetMax = Vector2.zero;
                float pulse = 0.45f + 0.35f * (0.5f + 0.5f * Mathf.Sin(animT * 6f));
                var gc = barGlow.color;
                gc.a = pulse;
                barGlow.color = gc;
            }
        }
        if (txtPct != null)
            txtPct.text = $"{Mathf.RoundToInt(displayProgress * 100f)}%";

        if (starImgs != null)
        {
            for (int i = 0; i < starImgs.Length; i++)
            {
                if (starImgs[i] == null) continue;
                float tw = 0.55f + 0.45f * Mathf.Sin(animT * (1.1f + starPhase[i] * 1.8f) + starPhase[i] * 6.28f);
                var c = starImgs[i].color;
                c.a = starBaseA[i] * tw;
                starImgs[i].color = c;
            }
        }

        if (txtStatus != null && !fading)
        {
            int dots = 1 + (int)(animT * 2.4f) % 3;
            txtStatus.text = statusBase + new string('.', dots);
        }

        if (Input.GetKeyDown(KeyCode.F11))
            BorderlessWindow.ToggleFullscreen();
        if (Input.GetKeyDown(KeyCode.Escape))
            QuitApp();
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var esGo = new GameObject("SplashEventSystem");
        DontDestroyOnLoad(esGo);
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();
    }

    void BuildUi()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        var bg = MakeImage(transform, "Bg", ColBg);
        Stretch(bg.rectTransform, 0, 0, 0, 0);
        bg.raycastTarget = true;

        // Starfield only — no moon, wash, or chrome chips on the backdrop
        BuildStars(transform, 90);

        BuildCard(transform);
        BuildFooter(transform);
        BuildCaptionButtons();
    }

    void BuildCard(Transform parent)
    {
        var card = MakeImage(parent, "Card", ColCard);
        var crt = card.rectTransform;
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(520f, 280f);

        // Thin cyan edge frame (1 px via 4 sides)
        MakeFrame(card.transform, ColCardEdge);

        // Top accent bar
        var accent = MakeImage(card.transform, "AccentBar",
            new Color(ColAccent.r, ColAccent.g, ColAccent.b, 0.85f));
        var ar = accent.rectTransform;
        ar.anchorMin = new Vector2(0f, 1f);
        ar.anchorMax = new Vector2(1f, 1f);
        ar.pivot = new Vector2(0.5f, 1f);
        ar.anchoredPosition = Vector2.zero;
        ar.sizeDelta = new Vector2(0f, 2.5f);

        BuildSpinner(card.transform);

        var txtTitle = MakeText(card.transform, "BETELGEUSE", 34, ColAccent, FontStyles.Bold);
        var tr = txtTitle.rectTransform;
        tr.anchorMin = new Vector2(0f, 0.58f);
        tr.anchorMax = new Vector2(1f, 0.74f);
        tr.offsetMin = new Vector2(28f, 0f);
        tr.offsetMax = new Vector2(-28f, 0f);
        txtTitle.alignment = TextAlignmentOptions.Center;
        txtTitle.characterSpacing = 8f;

        var txtSub = MakeText(card.transform,
            UILocale.IsUK
                ? "Автономна посадка  ·  GNC Mission Control"
                : "Autonomous Landing  ·  GNC Mission Control",
            13, ColMuted, FontStyles.Normal);
        var sr = txtSub.rectTransform;
        sr.anchorMin = new Vector2(0f, 0.46f);
        sr.anchorMax = new Vector2(1f, 0.58f);
        sr.offsetMin = new Vector2(32f, 0f);
        sr.offsetMax = new Vector2(-32f, 0f);
        txtSub.alignment = TextAlignmentOptions.Center;

        // Divider under subtitle
        var div = MakeImage(card.transform, "Div", new Color(1f, 1f, 1f, 0.08f));
        var dr = div.rectTransform;
        dr.anchorMin = new Vector2(0.18f, 0.44f);
        dr.anchorMax = new Vector2(0.82f, 0.44f);
        dr.pivot = new Vector2(0.5f, 0.5f);
        dr.sizeDelta = new Vector2(0f, 1f);

        var txtStage = MakeText(card.transform, "PID  ·  FUZZY  ·  NEURAL  ·  HYBRID",
            11, new Color(ColAmber.r, ColAmber.g, ColAmber.b, 0.75f), FontStyles.Bold);
        var stg = txtStage.rectTransform;
        stg.anchorMin = new Vector2(0f, 0.34f);
        stg.anchorMax = new Vector2(1f, 0.44f);
        stg.offsetMin = new Vector2(24f, 0f);
        stg.offsetMax = new Vector2(-24f, 0f);
        txtStage.alignment = TextAlignmentOptions.Center;
        txtStage.characterSpacing = 1.5f;

        // Progress track
        var trackGo = MakeImage(card.transform, "Track", ColTrack);
        var trk = trackGo.rectTransform;
        trk.anchorMin = new Vector2(0.1f, 0.22f);
        trk.anchorMax = new Vector2(0.9f, 0.28f);
        trk.offsetMin = Vector2.zero;
        trk.offsetMax = Vector2.zero;

        barFill = MakeImage(trackGo.transform, "Fill", ColFill);
        var fr = barFill.rectTransform;
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = new Vector2(0.02f, 1f);
        fr.offsetMin = Vector2.zero;
        fr.offsetMax = Vector2.zero;

        barGlow = MakeImage(trackGo.transform, "TipGlow", new Color(1f, 1f, 1f, 0.55f));
        var bgr = barGlow.rectTransform;
        bgr.anchorMin = Vector2.zero;
        bgr.anchorMax = new Vector2(0.05f, 1f);
        bgr.offsetMin = Vector2.zero;
        bgr.offsetMax = Vector2.zero;

        // Status row
        txtStatus = MakeText(card.transform, "…", 13, ColMuted, FontStyles.Normal);
        var st = txtStatus.rectTransform;
        st.anchorMin = new Vector2(0.1f, 0.08f);
        st.anchorMax = new Vector2(0.7f, 0.2f);
        st.offsetMin = Vector2.zero;
        st.offsetMax = Vector2.zero;
        txtStatus.alignment = TextAlignmentOptions.MidlineLeft;
        txtStatus.overflowMode = TextOverflowModes.Ellipsis;

        txtPct = MakeText(card.transform, "0%", 16, ColAmber, FontStyles.Bold);
        var pr = txtPct.rectTransform;
        pr.anchorMin = new Vector2(0.7f, 0.08f);
        pr.anchorMax = new Vector2(0.9f, 0.2f);
        pr.offsetMin = Vector2.zero;
        pr.offsetMax = Vector2.zero;
        txtPct.alignment = TextAlignmentOptions.MidlineRight;
    }

    void BuildFooter(Transform parent)
    {
        var foot = MakeText(parent,
            UILocale.IsUK
                ? "МКР 2026  ·  Soft-landing GNC  ·  v1.0.0"
                : "MSc Thesis 2026  ·  Soft-landing GNC  ·  v1.0.0",
            12, new Color(ColDim.r, ColDim.g, ColDim.b, 0.85f), FontStyles.Normal);
        var ft = foot.rectTransform;
        ft.anchorMin = new Vector2(0f, 0f);
        ft.anchorMax = new Vector2(1f, 0f);
        ft.pivot = new Vector2(0.5f, 0f);
        ft.sizeDelta = new Vector2(0f, 36f);
        ft.anchoredPosition = new Vector2(0f, 20f);
        foot.alignment = TextAlignmentOptions.Center;
    }

    void BuildSpinner(Transform card)
    {
        var root = new GameObject("Spinner", typeof(RectTransform));
        root.transform.SetParent(card, false);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.86f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.sizeDelta = new Vector2(48f, 48f);

        // Static dim ring
        var ring = MakeImage(root.transform, "Ring", new Color(1f, 1f, 1f, 0.1f));
        ring.sprite = RingSprite(64, 5f, 1f);
        ring.type = Image.Type.Simple;
        ring.preserveAspect = true;
        Stretch(ring.rectTransform, 0, 0, 0, 0);

        // Spinning arc (partial ring)
        var spinGo = new GameObject("ArcSpin", typeof(RectTransform));
        spinGo.transform.SetParent(root.transform, false);
        spinnerRt = spinGo.GetComponent<RectTransform>();
        Stretch(spinnerRt, 0, 0, 0, 0);

        spinnerArc = MakeImage(spinGo.transform, "Arc", ColAccent);
        spinnerArc.sprite = RingSprite(64, 5.5f, 0.28f);
        spinnerArc.type = Image.Type.Simple;
        spinnerArc.preserveAspect = true;
        Stretch(spinnerArc.rectTransform, 0, 0, 0, 0);

        // Center dot
        var core = MakeImage(root.transform, "Core", new Color(ColAccent.r, ColAccent.g, ColAccent.b, 0.35f));
        var cr = core.rectTransform;
        cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0.5f);
        cr.pivot = new Vector2(0.5f, 0.5f);
        cr.sizeDelta = new Vector2(6f, 6f);
    }

    /// <summary>Procedural ring / arc sprite. fill01 = fraction of circumference drawn.</summary>
    static Sprite RingSprite(int size, float thicknessPx, float fill01)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float outer = size * 0.5f - 1f;
        float inner = Mathf.Max(1f, outer - thicknessPx);
        float fillAngle = Mathf.Clamp01(fill01) * Mathf.PI * 2f;

        var clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r < inner - 0.6f || r > outer + 0.6f)
                {
                    tex.SetPixel(x, y, clear);
                    continue;
                }
                // 0 = up, increases clockwise
                float ang = Mathf.Atan2(dx, dy);
                if (ang < 0f) ang += Mathf.PI * 2f;
                float edge = 1f;
                if (r < inner) edge = 1f - (inner - r);
                else if (r > outer) edge = 1f - (r - outer);
                edge = Mathf.Clamp01(edge);

                float a = ang <= fillAngle + 0.02f ? edge : 0f;
                if (fill01 < 0.99f && ang > fillAngle - 0.12f && ang <= fillAngle)
                    a *= (fillAngle - ang) / 0.12f;

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
            }
        }
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static void MakeFrame(Transform parent, Color c)
    {
        void Edge(string name, Vector2 aMin, Vector2 aMax, Vector2 size)
        {
            var img = MakeImage(parent, name, c);
            var rt = img.rectTransform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            if (size.x > 0f || size.y > 0f)
                rt.sizeDelta = size;
            // For stretch edges keep sizeDelta on the thin axis
            if (Mathf.Approximately(aMin.x, 0f) && Mathf.Approximately(aMax.x, 1f))
            {
                rt.pivot = new Vector2(0.5f, aMin.y > 0.5f ? 1f : 0f);
                rt.sizeDelta = new Vector2(0f, 1f);
            }
            else
            {
                rt.pivot = new Vector2(aMin.x > 0.5f ? 1f : 0f, 0.5f);
                rt.sizeDelta = new Vector2(1f, 0f);
            }
        }
        Edge("FrT", new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero);
        Edge("FrB", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero);
        Edge("FrL", new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero);
        Edge("FrR", new Vector2(1f, 0f), new Vector2(1f, 1f), Vector2.zero);
    }

    void BuildCaptionButtons()
    {
        const float capW = 48f;
        const float capH = 34f;

        var bar = MakeImage(transform, "CaptionBar", new Color(ColCard.r, ColCard.g, ColCard.b, 0.9f));
        var brt = bar.rectTransform;
        brt.anchorMin = brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(1f, 1f);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(capW * 3f, capH);

        var edge = MakeImage(bar.transform, "CapEdge", new Color(1f, 1f, 1f, 0.08f));
        var ert = edge.rectTransform;
        ert.anchorMin = new Vector2(0f, 0f);
        ert.anchorMax = new Vector2(1f, 0f);
        ert.pivot = new Vector2(0.5f, 0f);
        ert.anchoredPosition = Vector2.zero;
        ert.sizeDelta = new Vector2(0f, 1f);

        var hlg = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 1f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        MakeCapBtn(bar.transform, "−", capW, capH, ColBtn, Color.white,
            () => BorderlessWindow.Minimize());
        MakeCapBtn(bar.transform, "□", capW, capH, ColBtn, Color.white,
            () => BorderlessWindow.ToggleFullscreen());
        MakeCapBtn(bar.transform, "×", capW, capH, ColClose, Color.white, QuitApp);
    }

    static void MakeCapBtn(Transform parent, string glyph, float w, float h,
        Color bg, Color fg, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Cap_" + glyph,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = w;
        le.minWidth = w;
        le.flexibleWidth = 1f;
        le.preferredHeight = h;

        var img = go.GetComponent<Image>();
        img.sprite = WhiteSprite();
        img.type = Image.Type.Simple;
        img.color = Color.white;
        img.raycastTarget = true;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        var c = ColorBlock.defaultColorBlock;
        c.normalColor = bg;
        c.highlightedColor = Color.Lerp(bg, Color.white, 0.2f);
        c.pressedColor = Color.Lerp(bg, Color.black, 0.25f);
        c.selectedColor = bg;
        c.disabledColor = new Color(bg.r, bg.g, bg.b, 0.35f);
        c.colorMultiplier = 1f;
        c.fadeDuration = 0.04f;
        btn.colors = c;
        btn.onClick.AddListener(onClick);

        var tmp = MakeText(go.transform, glyph, 18, fg, FontStyles.Bold);
        Stretch(tmp.rectTransform, 0, 0, 0, 0);
        tmp.alignment = TextAlignmentOptions.Center;
    }

    static void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void BuildStars(Transform parent, int count)
    {
        var rng = new System.Random(42);
        var starsRoot = new GameObject("Stars", typeof(RectTransform));
        starsRoot.transform.SetParent(parent, false);
        Stretch(starsRoot.GetComponent<RectTransform>(), 0, 0, 0, 0);

        starImgs = new Image[count];
        starPhase = new float[count];
        starBaseA = new float[count];

        for (int i = 0; i < count; i++)
        {
            float x = (float)rng.NextDouble();
            float y = (float)rng.NextDouble();
            float s = 1.1f + (float)rng.NextDouble() * 2.4f;
            // Occasional brighter star
            if (rng.NextDouble() < 0.08)
                s += 1.6f;
            float a = 0.18f + (float)rng.NextDouble() * 0.55f;
            var star = MakeImage(starsRoot.transform, "S" + i, new Color(0.88f, 0.92f, 1f, a));
            var rt = star.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(x, y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(s, s);
            starImgs[i] = star;
            starPhase[i] = (float)rng.NextDouble();
            starBaseA[i] = a;
        }
    }

    public void SetProgress(float t01, string status)
    {
        targetProgress = Mathf.Clamp01(t01);
        if (!string.IsNullOrEmpty(status))
        {
            statusBase = status.TrimEnd('.', '…', ' ');
            if (txtStatus != null && fading)
                txtStatus.text = statusBase;
        }
    }

    public void FadeOutAndDestroy(float duration = 0.6f)
    {
        if (fading) return;
        fading = true;
        targetProgress = 1f;
        StartCoroutine(FadeCo(duration));
    }

    IEnumerator FadeCo(float duration)
    {
        statusBase = UILocale.IsUK ? "Готово" : "Ready";
        if (txtStatus != null) txtStatus.text = statusBase;
        float guard = 0f;
        while (displayProgress < 0.98f && guard < 1.5f)
        {
            guard += Time.unscaledDeltaTime;
            yield return null;
        }
        displayProgress = 1f;

        float t = 0f;
        var cg = gameObject.GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            cg.alpha = 1f - u * u * (3f - 2f * u);
            yield return null;
        }
        if (Instance == this) Instance = null;
        Destroy(gameObject);
    }

    static Sprite _whiteSprite;
    static Sprite WhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;
        var tex = Texture2D.whiteTexture;
        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f);
        return _whiteSprite;
    }

    static Image MakeImage(Transform parent, string name, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = WhiteSprite();
        img.type = Image.Type.Simple;
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    static TMP_Text MakeText(Transform parent, string msg, float size, Color c, FontStyles style)
    {
        var go = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        try { UiTypography.Apply(tmp, size, c, style); }
        catch
        {
            tmp.fontSize = size;
            tmp.color = c;
            tmp.fontStyle = style;
        }
        tmp.text = msg ?? "";
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    static void Stretch(RectTransform rt, float l, float b, float r, float t)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }
}
