using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Branded loading screen — clean Mission Control look, no blue Outline frames,
/// animated spinner + dots so load state is obvious.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class SplashScreenUI : MonoBehaviour
{
    public static SplashScreenUI Instance { get; private set; }

    static readonly Color ColBgDeep = new(0.014f, 0.016f, 0.022f, 1f);
    static readonly Color ColBgMid = new(0.03f, 0.034f, 0.048f, 1f);
    static readonly Color ColCard = new(0.045f, 0.05f, 0.068f, 0.97f);
    static readonly Color ColAccent = new(0.55f, 0.86f, 0.98f, 1f);
    static readonly Color ColAmber = new(1f, 0.78f, 0.38f, 1f);
    static readonly Color ColMuted = new(0.62f, 0.66f, 0.74f, 1f);
    static readonly Color ColTrack = new(0.12f, 0.14f, 0.18f, 1f);
    static readonly Color ColFill = new(0.35f, 0.78f, 0.95f, 1f);
    static readonly Color ColBtn = new(0.12f, 0.14f, 0.18f, 0.95f);
    static readonly Color ColClose = new(0.7f, 0.24f, 0.26f, 0.95f);
    static readonly Color ColHair = new(1f, 1f, 1f, 0.08f);

    Image barFill;
    Image barShimmer;
    RectTransform spinnerRt;
    Image[] spinnerSegs;
    Image[] waitDots;
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

    void Update()
    {
        animT += Time.unscaledDeltaTime;

        // Smooth progress bar
        displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, Time.unscaledDeltaTime * 1.2f);
        if (barFill != null)
        {
            var rt = barFill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(Mathf.Clamp01(displayProgress), 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        if (txtPct != null)
            txtPct.text = $"{Mathf.RoundToInt(displayProgress * 100f)}%";

        // Shimmer sliding across the filled portion
        if (barShimmer != null && barFill != null)
        {
            float w = Mathf.Clamp01(displayProgress);
            if (w < 0.02f)
            {
                barShimmer.enabled = false;
            }
            else
            {
                barShimmer.enabled = true;
                float phase = Mathf.Repeat(animT * 0.85f, 1.2f) - 0.1f; // -0.1..1.1
                var srt = barShimmer.rectTransform;
                srt.anchorMin = new Vector2(Mathf.Clamp01(phase - 0.12f) * w, 0f);
                srt.anchorMax = new Vector2(Mathf.Clamp01(phase + 0.02f) * w, 1f);
                srt.offsetMin = Vector2.zero;
                srt.offsetMax = Vector2.zero;
            }
        }

        // Spinner ring rotation + segment chase
        if (spinnerRt != null)
            spinnerRt.localEulerAngles = new Vector3(0f, 0f, -animT * 140f);
        if (spinnerSegs != null)
        {
            int n = spinnerSegs.Length;
            float head = Mathf.Repeat(animT * 2.2f, n);
            for (int i = 0; i < n; i++)
            {
                if (spinnerSegs[i] == null) continue;
                float d = Mathf.Min(Mathf.Abs(i - head), n - Mathf.Abs(i - head));
                float a = Mathf.Lerp(0.15f, 1f, 1f - Mathf.Clamp01(d / 3.5f));
                var c = ColAccent;
                c.a = a;
                spinnerSegs[i].color = c;
            }
        }

        // Bouncing wait dots
        if (waitDots != null)
        {
            for (int i = 0; i < waitDots.Length; i++)
            {
                if (waitDots[i] == null) continue;
                float bounce = Mathf.Sin(animT * 5.5f - i * 0.55f);
                float a = 0.25f + 0.75f * (0.5f + 0.5f * bounce);
                float y = 2f * Mathf.Max(0f, bounce);
                var c = ColAmber;
                c.a = a;
                waitDots[i].color = c;
                var rt = waitDots[i].rectTransform;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
            }
        }

        // Animated ellipsis on status
        if (txtStatus != null && !fading)
        {
            int dots = 1 + (int)(animT * 2.5f) % 3;
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

        // Full-screen background (no colored frames)
        var bg = MakeImage(transform, "Bg", ColBgDeep);
        Stretch(bg.rectTransform, 0, 0, 0, 0);
        bg.raycastTarget = true;

        var wash = MakeImage(transform, "Wash", new Color(ColBgMid.r, ColBgMid.g, ColBgMid.b, 0.5f));
        var wr = wash.rectTransform;
        wr.anchorMin = new Vector2(0f, 0.4f);
        wr.anchorMax = Vector2.one;
        wr.offsetMin = Vector2.zero;
        wr.offsetMax = Vector2.zero;

        BuildStars(transform, 40);

        // Soft moon — muted gray only, no cyan rim
        BuildMoon(transform);

        // Center card — flat, subtle white hairline only (no blue Outline)
        var card = MakeImage(transform, "Card", ColCard);
        var crt = card.rectTransform;
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.52f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(560f, 360f);

        // Neutral hairlines (white, very soft) — not cyan frames
        MakeHairline(card.transform, true, ColHair);
        MakeHairline(card.transform, false, new Color(1f, 1f, 1f, 0.05f));

        // Spinner above title
        BuildSpinner(card.transform);

        var txtTitle = MakeText(card.transform, "BETELGEUSE", 38, ColAccent, FontStyles.Bold);
        var tr = txtTitle.rectTransform;
        tr.anchorMin = new Vector2(0f, 0.62f);
        tr.anchorMax = new Vector2(1f, 0.78f);
        tr.offsetMin = new Vector2(24f, 0f);
        tr.offsetMax = new Vector2(-24f, 0f);
        txtTitle.alignment = TextAlignmentOptions.Center;
        txtTitle.characterSpacing = 5f;

        var txtSub = MakeText(card.transform,
            UILocale.IsUK
                ? "Автономна посадка  ·  GNC Mission Control"
                : "Autonomous Landing  ·  GNC Mission Control",
            14, ColMuted, FontStyles.Normal);
        var sr = txtSub.rectTransform;
        sr.anchorMin = new Vector2(0f, 0.52f);
        sr.anchorMax = new Vector2(1f, 0.62f);
        sr.offsetMin = new Vector2(28f, 0f);
        sr.offsetMax = new Vector2(-28f, 0f);
        txtSub.alignment = TextAlignmentOptions.Center;

        var txtStage = MakeText(card.transform, "PID  ·  FUZZY  ·  NEURAL  ·  HYBRID",
            11, new Color(ColAmber.r, ColAmber.g, ColAmber.b, 0.8f), FontStyles.Bold);
        var stg = txtStage.rectTransform;
        stg.anchorMin = new Vector2(0f, 0.44f);
        stg.anchorMax = new Vector2(1f, 0.52f);
        stg.offsetMin = new Vector2(20f, 0f);
        stg.offsetMax = new Vector2(-20f, 0f);
        txtStage.alignment = TextAlignmentOptions.Center;

        // Progress track — no outline
        var trackGo = MakeImage(card.transform, "Track", ColTrack);
        var trk = trackGo.rectTransform;
        trk.anchorMin = new Vector2(0.1f, 0.28f);
        trk.anchorMax = new Vector2(0.9f, 0.34f);
        trk.offsetMin = Vector2.zero;
        trk.offsetMax = Vector2.zero;

        barFill = MakeImage(trackGo.transform, "Fill", ColFill);
        var fr = barFill.rectTransform;
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = new Vector2(0.02f, 1f);
        fr.offsetMin = Vector2.zero;
        fr.offsetMax = Vector2.zero;

        barShimmer = MakeImage(trackGo.transform, "Shimmer", new Color(1f, 1f, 1f, 0.35f));
        var shm = barShimmer.rectTransform;
        shm.anchorMin = Vector2.zero;
        shm.anchorMax = new Vector2(0.05f, 1f);
        shm.offsetMin = Vector2.zero;
        shm.offsetMax = Vector2.zero;

        // Status + dots row
        txtStatus = MakeText(card.transform, "…", 13, ColMuted, FontStyles.Normal);
        var st = txtStatus.rectTransform;
        st.anchorMin = new Vector2(0.1f, 0.14f);
        st.anchorMax = new Vector2(0.62f, 0.24f);
        st.offsetMin = Vector2.zero;
        st.offsetMax = Vector2.zero;
        txtStatus.alignment = TextAlignmentOptions.MidlineLeft;
        txtStatus.overflowMode = TextOverflowModes.Ellipsis;

        BuildWaitDots(card.transform);

        txtPct = MakeText(card.transform, "0%", 15, ColAmber, FontStyles.Bold);
        var pr = txtPct.rectTransform;
        pr.anchorMin = new Vector2(0.72f, 0.14f);
        pr.anchorMax = new Vector2(0.9f, 0.24f);
        pr.offsetMin = Vector2.zero;
        pr.offsetMax = Vector2.zero;
        txtPct.alignment = TextAlignmentOptions.MidlineRight;

        // Footer
        var foot = MakeText(transform,
            UILocale.IsUK
                ? "МКР 2026  ·  Soft-landing GNC  ·  v1.0.0"
                : "MSc Thesis 2026  ·  Soft-landing GNC  ·  v1.0.0",
            12, new Color(ColMuted.r, ColMuted.g, ColMuted.b, 0.65f), FontStyles.Normal);
        var ft = foot.rectTransform;
        ft.anchorMin = new Vector2(0f, 0f);
        ft.anchorMax = new Vector2(1f, 0f);
        ft.pivot = new Vector2(0.5f, 0f);
        ft.sizeDelta = new Vector2(0f, 36f);
        ft.anchoredPosition = new Vector2(0f, 18f);
        foot.alignment = TextAlignmentOptions.Center;

        // Brand chip top-left
        var brandChip = MakeImage(transform, "BrandChip", new Color(ColBtn.r, ColBtn.g, ColBtn.b, 0.8f));
        var bcr = brandChip.rectTransform;
        bcr.anchorMin = bcr.anchorMax = new Vector2(0f, 1f);
        bcr.pivot = new Vector2(0f, 1f);
        bcr.anchoredPosition = new Vector2(14f, -10f);
        bcr.sizeDelta = new Vector2(160f, 28f);
        var brandTxt = MakeText(brandChip.transform, "BETELGEUSE MC", 11, ColAccent, FontStyles.Bold);
        Stretch(brandTxt.rectTransform, 8, 2, 8, 2);
        brandTxt.alignment = TextAlignmentOptions.Center;

        BuildCaptionButtons();
    }

    void BuildSpinner(Transform card)
    {
        var root = new GameObject("Spinner", typeof(RectTransform));
        root.transform.SetParent(card, false);
        spinnerRt = root.GetComponent<RectTransform>();
        spinnerRt.anchorMin = spinnerRt.anchorMax = new Vector2(0.5f, 0.88f);
        spinnerRt.pivot = new Vector2(0.5f, 0.5f);
        spinnerRt.sizeDelta = new Vector2(44f, 44f);

        const int segs = 10;
        spinnerSegs = new Image[segs];
        float radius = 16f;
        for (int i = 0; i < segs; i++)
        {
            float ang = i * (360f / segs) * Mathf.Deg2Rad;
            var seg = MakeImage(root.transform, "Seg" + i, ColAccent);
            var rt = seg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(4.5f, 10f);
            rt.anchoredPosition = new Vector2(Mathf.Sin(ang) * radius, Mathf.Cos(ang) * radius);
            rt.localEulerAngles = new Vector3(0f, 0f, -i * (360f / segs));
            spinnerSegs[i] = seg;
        }
    }

    void BuildWaitDots(Transform card)
    {
        var row = new GameObject("WaitDots", typeof(RectTransform));
        row.transform.SetParent(card, false);
        var rr = row.GetComponent<RectTransform>();
        rr.anchorMin = rr.anchorMax = new Vector2(0.64f, 0.19f);
        rr.pivot = new Vector2(0f, 0.5f);
        rr.sizeDelta = new Vector2(48f, 12f);

        waitDots = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var d = MakeImage(row.transform, "D" + i, ColAmber);
            var rt = d.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(5f, 5f);
            rt.anchoredPosition = new Vector2(8f + i * 12f, 0f);
            waitDots[i] = d;
        }
    }

    static void MakeHairline(Transform parent, bool top, Color c)
    {
        var line = MakeImage(parent, top ? "HairTop" : "HairBot", c);
        var rt = line.rectTransform;
        if (top)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
        }
        else
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
        }
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 1f);
    }

    void BuildCaptionButtons()
    {
        const float capW = 48f;
        const float capH = 34f;

        var bar = MakeImage(transform, "CaptionBar", new Color(ColCard.r, ColCard.g, ColCard.b, 0.95f));
        var brt = bar.rectTransform;
        brt.anchorMin = brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(1f, 1f);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(capW * 3f, capH);

        // Neutral bottom edge only
        var edge = MakeImage(bar.transform, "CapEdge", new Color(1f, 1f, 1f, 0.1f));
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

        for (int i = 0; i < count; i++)
        {
            float x = (float)rng.NextDouble();
            float y = 0.28f + (float)rng.NextDouble() * 0.72f;
            float s = 1.2f + (float)rng.NextDouble() * 2.2f;
            float a = 0.12f + (float)rng.NextDouble() * 0.45f;
            var star = MakeImage(starsRoot.transform, "S" + i, new Color(0.9f, 0.93f, 1f, a));
            var rt = star.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(x, y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(s, s);
        }
    }

    void BuildMoon(Transform parent)
    {
        var moon = MakeImage(parent, "Moon", new Color(0.5f, 0.52f, 0.56f, 0.1f));
        var rt = moon.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.88f, 0.16f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(260f, 260f);

        var body = MakeImage(moon.transform, "Body", new Color(0.55f, 0.57f, 0.6f, 0.16f));
        var br = body.rectTransform;
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
        br.pivot = new Vector2(0.5f, 0.5f);
        br.sizeDelta = new Vector2(190f, 190f);

        float[,] craters = { { 0.35f, 0.55f, 26f }, { 0.62f, 0.4f, 16f }, { 0.48f, 0.32f, 12f } };
        for (int i = 0; i < craters.GetLength(0); i++)
        {
            var c = MakeImage(body.transform, "Cr" + i, new Color(0.22f, 0.24f, 0.28f, 0.3f));
            var cr = c.rectTransform;
            cr.anchorMin = cr.anchorMax = new Vector2(craters[i, 0], craters[i, 1]);
            cr.pivot = new Vector2(0.5f, 0.5f);
            cr.sizeDelta = new Vector2(craters[i, 2], craters[i, 2]);
        }
    }

    public void SetProgress(float t01, string status)
    {
        targetProgress = Mathf.Clamp01(t01);
        if (!string.IsNullOrEmpty(status))
        {
            // Strip trailing dots — Update appends animated ellipsis
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
