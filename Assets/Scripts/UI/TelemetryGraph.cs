using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Pixel-buffer realtime telemetry graph. Labels are siblings under <see cref="labelRoot"/>
/// (not under the RawImage) so TMP stays readable and clickable when interactive.
/// Optional view window: zoom/pan over live data without fighting RestoreSamples.
/// Detail/interactive mode: denser Y ticks, hover point highlight + value tooltip.
/// X-axis labels along the bottom show sample time (seconds) for the visible window.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class TelemetryGraph : MonoBehaviour,
    IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler,
    IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    public string title = "GRAPH";
    public string unit = "";
    public Color lineColor = new(0.3f, 0.65f, 1f, 1f);
    public Color fillColor = new(0.3f, 0.65f, 1f, 0.15f);
    public Color gridColor = new(1f, 1f, 1f, 0.1f);
    public Color bgColor = new(0.07f, 0.08f, 0.1f, 1f);
    public Color axisColor = new(0.75f, 0.78f, 0.85f, 0.4f);
    public Color thresholdColor = new(0.95f, 0.4f, 0.4f, 0.55f);
    public Color borderColor = new(0.5f, 0.55f, 0.6f, 0.55f);
    public Color labelColor = new(0.8f, 0.84f, 0.9f, 1f);
    public int maxSamples = 280;
    public bool autoScale = true;
    public bool showFill = true;
    public bool showZeroLine = true;
    public float? thresholdY;
    public string valueFormat = "F1";

    /// <summary>When true, wheel/drag/double-click adjust the view window; hover shows tooltip.</summary>
    public bool interactiveView;

    /// <summary>Parent for TMP labels (usually frame above RawImage). Null = this.transform.</summary>
    public RectTransform labelRoot;

    RawImage image;
    Texture2D tex;
    readonly List<float> samples = new();
    readonly List<float> sampleTimes = new();
    int w = 300, h = 90;
    bool dirty = true;

    // View window in normalized full-plot space: X in [0,1] across maxSamples slots,
    // Y relative to the locked auto-fit base range when custom.
    float viewZoom = 1f;
    float viewPanX = 0f;
    float viewPanY = 0f;
    bool customView;
    float baseYMin, baseYMax;
    bool baseYValid;
    bool dragging;
    float lastClickTime = -10f;

    const float MinZoom = 1f;
    const float MaxZoom = 20f;
    const float WheelZoomStep = 1.12f;
    const float ButtonZoomStep = 1.25f;
    const int YTickCapacity = 7;
    const int XTickCapacity = 5;

    TMP_Text lblTitle, lblCur;
    TMP_Text[] lblYTicks;
    TMP_Text[] lblXTicks;

    // Hover readouts (interactive detail modal only)
    bool pointerInside;
    bool hoverValid;
    int hoverSampleIndex = -1;
    float hoverSampleValue;
    Vector2 hoverLocalInImage;
    RectTransform hoverMarker;
    Image hoverMarkerHaloImg, hoverMarkerCoreImg;
    RectTransform tooltipRoot;
    Image tooltipBg;
    TMP_Text lblTooltip;
    static Sprite s_hoverDotSprite;

    // Cached plot rect in texture pixels (updated in Draw)
    int plotL, plotR, plotB, plotT, plotW, plotH;

    public float LastValue => samples.Count > 0 ? samples[samples.Count - 1] : 0f;
    public float DisplayMin { get; private set; }
    public float DisplayMax { get; private set; }
    public bool HasCustomView => customView;
    public float ViewZoom => viewZoom;

    void Awake()
    {
        image = GetComponent<RawImage>();
        if (labelRoot == null)
            labelRoot = transform as RectTransform;
        // Mini charts stay non-raycast so parent click-to-detail buttons keep working.
        if (image != null) image.raycastTarget = interactiveView;
        ApplyThemeColors();
        EnsureLabels();
        RebuildTexture();
    }

    public void BindLabelRoot(RectTransform root)
    {
        labelRoot = root != null ? root : transform as RectTransform;
        DestroyLabels();
        EnsureLabels();
        dirty = true;
    }

    public void SetInteractiveView(bool on)
    {
        interactiveView = on;
        if (image == null) image = GetComponent<RawImage>();
        if (image != null) image.raycastTarget = on;
        if (!on) ClearHover();
        EnsureLabels();
        if (on)
            EnsureHoverUi(labelRoot != null ? labelRoot : transform);
        LayoutYTicks();
        dirty = true;
    }

    void DestroyLabels()
    {
        void Kill(TMP_Text t)
        {
            if (t != null) Destroy(t.gameObject);
        }
        Kill(lblTitle); Kill(lblCur); Kill(lblTooltip);
        if (lblYTicks != null)
        {
            for (int i = 0; i < lblYTicks.Length; i++) Kill(lblYTicks[i]);
        }
        if (lblXTicks != null)
        {
            for (int i = 0; i < lblXTicks.Length; i++) Kill(lblXTicks[i]);
        }
        lblTitle = lblCur = lblTooltip = null;
        lblYTicks = null;
        lblXTicks = null;
        if (hoverMarker != null) Destroy(hoverMarker.gameObject);
        if (tooltipRoot != null) Destroy(tooltipRoot.gameObject);
        hoverMarker = tooltipRoot = null;
        hoverMarkerHaloImg = hoverMarkerCoreImg = null;
        tooltipBg = null;
    }

    public void ApplyThemeColors()
    {
        bool light = UiTheme.IsLightBackground;
        var edge = UiTheme.Current.Edge;
        if (light)
        {
            bgColor = new Color(0.985f, 0.988f, 0.992f, 1f);
            gridColor = new Color(0.65f, 0.7f, 0.76f, 0.25f);
            axisColor = new Color(0.45f, 0.5f, 0.55f, 0.45f);
            borderColor = new Color(edge.r, edge.g, edge.b, 0.45f);
            labelColor = new Color(0.2f, 0.24f, 0.3f, 1f);
            thresholdColor = new Color(0.8f, 0.25f, 0.25f, 0.55f);
        }
        else
        {
            bgColor = new Color(0.06f, 0.07f, 0.09f, 1f);
            gridColor = new Color(1f, 1f, 1f, 0.1f);
            axisColor = new Color(0.75f, 0.78f, 0.85f, 0.4f);
            borderColor = new Color(edge.r, edge.g, edge.b, 0.55f);
            labelColor = new Color(0.82f, 0.86f, 0.92f, 1f);
            thresholdColor = new Color(0.95f, 0.45f, 0.45f, 0.55f);
        }
        fillColor = new Color(lineColor.r, lineColor.g, lineColor.b, light ? 0.18f : 0.15f);
        dirty = true;
        ApplyLabelStyle();
        ApplyHoverTheme();
    }

    void EnsureLabels()
    {
        if (lblTitle != null && lblYTicks != null && lblYTicks.Length == YTickCapacity
            && lblXTicks != null && lblXTicks.Length == XTickCapacity) return;
        Transform p = labelRoot != null ? labelRoot : transform;

        if (lblTitle == null)
        {
            lblTitle = MakeLabel(p, "GTitle", 11f, labelColor, TextAlignmentOptions.MidlineLeft);
            Stretch(lblTitle.rectTransform, 8f, -2f, 140f, 18f, 0f, 1f, 0f, 1f);
        }

        if (lblCur == null)
        {
            lblCur = MakeLabel(p, "GCur", 12f, lineColor, TextAlignmentOptions.MidlineRight);
            lblCur.fontStyle = FontStyles.Bold;
            Stretch(lblCur.rectTransform, -8f, -2f, 130f, 18f, 1f, 1f, 1f, 1f);
        }

        if (lblYTicks == null || lblYTicks.Length != YTickCapacity)
        {
            if (lblYTicks != null)
            {
                for (int i = 0; i < lblYTicks.Length; i++)
                    if (lblYTicks[i] != null) Destroy(lblYTicks[i].gameObject);
            }
            lblYTicks = new TMP_Text[YTickCapacity];
            for (int i = 0; i < YTickCapacity; i++)
            {
                lblYTicks[i] = MakeLabel(p, "GY" + i, 10f, labelColor, TextAlignmentOptions.MidlineLeft);
                lblYTicks[i].text = "-";
            }
        }

        if (lblXTicks == null || lblXTicks.Length != XTickCapacity)
        {
            if (lblXTicks != null)
            {
                for (int i = 0; i < lblXTicks.Length; i++)
                    if (lblXTicks[i] != null) Destroy(lblXTicks[i].gameObject);
            }
            lblXTicks = new TMP_Text[XTickCapacity];
            for (int i = 0; i < XTickCapacity; i++)
            {
                lblXTicks[i] = MakeLabel(p, "GX" + i, 9f, labelColor, TextAlignmentOptions.Midline);
                lblXTicks[i].text = "";
            }
        }

        if (interactiveView)
            EnsureHoverUi(p);
        LayoutYTicks();
        LayoutXTicks();

        if (labelRoot != null)
        {
            BringLabelsForward();
        }

        ApplyLabelStyle();
        if (lblTitle) lblTitle.text = title;
        if (lblCur) lblCur.text = "-";
        for (int i = 0; i < lblYTicks.Length; i++)
            if (lblYTicks[i]) lblYTicks[i].text = "-";
        if (lblXTicks != null)
            for (int i = 0; i < lblXTicks.Length; i++)
                if (lblXTicks[i]) lblXTicks[i].text = "";
    }

    static Sprite HoverDotSprite()
    {
        if (s_hoverDotSprite != null) return s_hoverDotSprite;
        const int n = 32;
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
        s_hoverDotSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return s_hoverDotSprite;
    }

    void EnsureHoverUi(Transform p)
    {
        if (hoverMarker == null)
        {
            var go = new GameObject("GHoverMarker", typeof(RectTransform));
            go.transform.SetParent(p, false);
            hoverMarker = go.GetComponent<RectTransform>();
            hoverMarker.anchorMin = hoverMarker.anchorMax = new Vector2(0.5f, 0.5f); // match parent-pivot local space
            hoverMarker.pivot = new Vector2(0.5f, 0.5f);
            hoverMarker.sizeDelta = new Vector2(16f, 16f);

            var haloGo = new GameObject("Halo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            haloGo.transform.SetParent(go.transform, false);
            var haloRt = haloGo.GetComponent<RectTransform>();
            haloRt.anchorMin = Vector2.zero;
            haloRt.anchorMax = Vector2.one;
            haloRt.offsetMin = Vector2.zero;
            haloRt.offsetMax = Vector2.zero;
            hoverMarkerHaloImg = haloGo.GetComponent<Image>();
            hoverMarkerHaloImg.sprite = HoverDotSprite();
            hoverMarkerHaloImg.type = Image.Type.Simple;
            hoverMarkerHaloImg.preserveAspect = true;
            hoverMarkerHaloImg.raycastTarget = false;

            var coreGo = new GameObject("Core", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            coreGo.transform.SetParent(go.transform, false);
            var coreRt = coreGo.GetComponent<RectTransform>();
            coreRt.anchorMin = coreRt.anchorMax = new Vector2(0.5f, 0.5f);
            coreRt.pivot = new Vector2(0.5f, 0.5f);
            coreRt.sizeDelta = new Vector2(7f, 7f);
            hoverMarkerCoreImg = coreGo.GetComponent<Image>();
            hoverMarkerCoreImg.sprite = HoverDotSprite();
            hoverMarkerCoreImg.type = Image.Type.Simple;
            hoverMarkerCoreImg.preserveAspect = true;
            hoverMarkerCoreImg.raycastTarget = false;

            go.SetActive(false);
        }
        if (tooltipRoot == null)
        {
            var go = new GameObject("GHoverTip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(p, false);
            tooltipRoot = go.GetComponent<RectTransform>();
            tooltipBg = go.GetComponent<Image>();
            tooltipBg.raycastTarget = false;
            tooltipRoot.anchorMin = tooltipRoot.anchorMax = new Vector2(0.5f, 0.5f); // match parent-pivot local space
            tooltipRoot.pivot = new Vector2(0f, 1f);
            tooltipRoot.sizeDelta = new Vector2(72f, 24f);

            lblTooltip = MakeLabel(go.transform, "GHoverTipText", 12f, Color.white, TextAlignmentOptions.MidlineLeft);
            var tr = lblTooltip.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(8f, 3f);
            tr.offsetMax = new Vector2(-8f, -3f);
            lblTooltip.textWrappingMode = TextWrappingModes.NoWrap;
            lblTooltip.overflowMode = TextOverflowModes.Overflow;
            go.SetActive(false);
        }
        // Re-assert center anchors (fixes leftover bottom-left anchors from older builds).
        if (hoverMarker != null)
        {
            hoverMarker.anchorMin = hoverMarker.anchorMax = new Vector2(0.5f, 0.5f);
            hoverMarker.pivot = new Vector2(0.5f, 0.5f);
        }
        if (tooltipRoot != null)
        {
            tooltipRoot.anchorMin = tooltipRoot.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRoot.pivot = new Vector2(0f, 1f);
        }
        ApplyHoverTheme();
    }

    void ApplyHoverTheme()
    {
        bool light = UiTheme.IsLightBackground;
        Color halo = lineColor;
        halo.a = light ? 0.35f : 0.45f;
        Color core = lineColor;
        core.a = 1f;
        if (light)
        {
            // Slightly darker core on light plots for contrast.
            core = new Color(lineColor.r * 0.75f, lineColor.g * 0.75f, lineColor.b * 0.85f, 1f);
        }
        if (hoverMarkerHaloImg) hoverMarkerHaloImg.color = halo;
        if (hoverMarkerCoreImg) hoverMarkerCoreImg.color = core;
        if (tooltipBg)
        {
            tooltipBg.color = light
                ? new Color(0.96f, 0.97f, 0.99f, 0.94f)
                : new Color(0.08f, 0.1f, 0.14f, 0.92f);
        }
        if (lblTooltip)
        {
            lblTooltip.color = light
                ? new Color(0.12f, 0.16f, 0.22f, 1f)
                : new Color(0.92f, 0.95f, 1f, 1f);
        }
    }

    void BringLabelsForward()
    {
        if (lblYTicks != null)
            for (int i = 0; i < lblYTicks.Length; i++)
                if (lblYTicks[i]) lblYTicks[i].transform.SetAsLastSibling();
        if (lblXTicks != null)
            for (int i = 0; i < lblXTicks.Length; i++)
                if (lblXTicks[i]) lblXTicks[i].transform.SetAsLastSibling();
        if (lblTitle) lblTitle.transform.SetAsLastSibling();
        if (lblCur) lblCur.transform.SetAsLastSibling();
        if (hoverMarker) hoverMarker.SetAsLastSibling();
        if (tooltipRoot) tooltipRoot.SetAsLastSibling();
    }

    void LayoutYTicks()
    {
        if (lblYTicks == null) return;
        for (int i = 0; i < YTickCapacity; i++)
        {
            var t = lblYTicks[i];
            if (t == null) continue;
            bool on = interactiveView ? true : (i == 0 || i == YTickCapacity / 2 || i == YTickCapacity - 1);
            t.gameObject.SetActive(on);
            if (!on) continue;

            float frac;
            if (interactiveView)
                frac = i / (float)(YTickCapacity - 1); // 0=min(bottom) .. 1=max(top)
            else if (i == 0) frac = 0f;
            else if (i == YTickCapacity - 1) frac = 1f;
            else frac = 0.5f;

            float font = interactiveView ? 9f : 10f;
            t.fontSize = font;
            float h = interactiveView ? 13f : 14f;
            // Top padding for title row (~20px when interactive denser stack)
            float topPad = interactiveView ? -18f : -20f;
            float botPad = interactiveView ? 18f : 16f;
            // Place along left edge; y from bottom of parent
            Stretch(t.rectTransform, 4f, 0f, interactiveView ? 78f : 70f, h, 0f, frac, 0f, 0.5f);
            // Nudge ends so they don't sit under title / clip bottom
            var rt = t.rectTransform;
            if (frac >= 0.99f) rt.anchoredPosition = new Vector2(4f, topPad);
            else if (frac <= 0.01f) rt.anchoredPosition = new Vector2(4f, botPad);
            else
            {
                // Mid ticks: anchoredPosition y unused when ay is frac â€” Stretch already set ay=frac
                // Re-apply with slight inset from edges for readability
                rt.anchorMin = new Vector2(0f, frac);
                rt.anchorMax = new Vector2(0f, frac);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(4f, 0f);
                rt.sizeDelta = new Vector2(interactiveView ? 78f : 70f, h);
            }
        }
    }

    void LayoutXTicks()
    {
        if (lblXTicks == null) return;
        int shown = interactiveView ? XTickCapacity : 3;
        for (int i = 0; i < XTickCapacity; i++)
        {
            var t = lblXTicks[i];
            if (t == null) continue;
            bool on = i < shown;
            t.gameObject.SetActive(on);
            if (!on) continue;

            float frac = shown <= 1 ? 0.5f : i / (float)(shown - 1);
            bool has = TryTimeAtPlotFrac(frac, out float time);
            t.text = has ? FmtTime(time) : "";
            if (!has)
            {
                t.gameObject.SetActive(false);
                continue;
            }

            float px = plotW > 0 ? plotL + frac * plotW : frac * Mathf.Max(1, w);
            PlaceXLabel(t.rectTransform, px / Mathf.Max(1f, w), i == 0, i == shown - 1);
        }
    }

    void PlaceXLabel(RectTransform rt, float nxInImage, bool first, bool last)
    {
        RectTransform root = labelRoot != null ? labelRoot : (transform as RectTransform);
        RectTransform img = image != null ? image.rectTransform : root;
        if (root == null || img == null) return;

        float nx = Mathf.Clamp01(nxInImage);
        Vector3 world = img.TransformPoint(new Vector3(
            Mathf.Lerp(img.rect.xMin, img.rect.xMax, nx),
            img.rect.yMin + 1f,
            0f));
        Vector3 local = root.InverseTransformPoint(world);

        float pivotX = first ? 0f : (last ? 1f : 0.5f);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(pivotX, 0f);
        rt.anchoredPosition = new Vector2(local.x, local.y);
        rt.sizeDelta = new Vector2(interactiveView ? 56f : 44f, 12f);
        rt.SetAsLastSibling();
    }

    bool TryTimeAtPlotFrac(float frac, out float time)
    {
        time = 0f;
        int n = samples.Count;
        if (n == 0 || sampleTimes.Count != n) return false;

        GetVisibleX(out float vx0, out float vx1);
        float nx = vx0 + Mathf.Clamp01(frac) * Mathf.Max(1e-6f, vx1 - vx0);
        float shift = maxSamples - n;
        float denom = Mathf.Max(1, maxSamples - 1);
        float idx = nx * denom - shift;
        if (idx < -0.02f || idx > n - 0.98f) return false;
        idx = Mathf.Clamp(idx, 0f, n - 1f);
        int i0 = Mathf.Clamp(Mathf.FloorToInt(idx), 0, n - 1);
        int i1 = Mathf.Min(i0 + 1, n - 1);
        float u = Mathf.Clamp01(idx - i0);
        time = Mathf.Lerp(sampleTimes[i0], sampleTimes[i1], u);
        return true;
    }

    static string FmtTime(float t)
    {
        float a = Mathf.Abs(t);
        if (a < 10f) return t.ToString("0.0") + "s";
        return Mathf.RoundToInt(t).ToString() + "s";
    }

    static void Stretch(RectTransform rt, float x, float y, float w, float h,
        float ax, float ay, float px, float py)
    {
        rt.anchorMin = new Vector2(ax, ay);
        rt.anchorMax = new Vector2(ax, ay);
        rt.pivot = new Vector2(px, py);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    void ApplyLabelStyle()
    {
        Color muted = new Color(labelColor.r, labelColor.g, labelColor.b, 0.9f);
        if (lblTitle) { lblTitle.color = labelColor; lblTitle.fontSize = 11f; }
        if (lblYTicks != null)
        {
            float fs = interactiveView ? 9f : 10f;
            for (int i = 0; i < lblYTicks.Length; i++)
                if (lblYTicks[i]) { lblYTicks[i].color = muted; lblYTicks[i].fontSize = fs; }
        }
        if (lblXTicks != null)
        {
            float fs = interactiveView ? 9f : 8.5f;
            for (int i = 0; i < lblXTicks.Length; i++)
                if (lblXTicks[i]) { lblXTicks[i].color = muted; lblXTicks[i].fontSize = fs; }
        }
        if (lblCur) { lblCur.color = lineColor; lblCur.fontSize = 12f; }
    }

    static TMP_Text MakeLabel(Transform parent, string name, float size, Color c, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        try { UiTypography.Apply(tmp, size, c, FontStyles.Normal); }
        catch
        {
            tmp.fontSize = size;
            tmp.color = c;
        }
        if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        tmp.enableAutoSizing = false;
        return tmp;
    }

    void RebuildTexture()
    {
        if (image == null) image = GetComponent<RawImage>();
        if (tex != null) Destroy(tex);
        var rt = image.rectTransform.rect;
        w = Mathf.Max(64, Mathf.RoundToInt(rt.width > 8f ? rt.width : 300f));
        h = Mathf.Max(40, Mathf.RoundToInt(rt.height > 8f ? rt.height : 90f));
        tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        image.texture = tex;
        image.color = Color.white;
        dirty = true;
    }

    public void Clear()
    {
        samples.Clear();
        sampleTimes.Clear();
        dirty = true;
        ClearHover();
        EnsureLabels();
        if (lblCur) lblCur.text = "-";
        if (lblYTicks != null)
            for (int i = 0; i < lblYTicks.Length; i++)
                if (lblYTicks[i]) lblYTicks[i].text = "-";
        if (lblXTicks != null)
            for (int i = 0; i < lblXTicks.Length; i++)
                if (lblXTicks[i]) lblXTicks[i].text = "";
    }

    public float[] GetSamples()
    {
        return samples.Count == 0 ? System.Array.Empty<float>() : samples.ToArray();
    }

    public float[] GetSampleTimes()
    {
        return sampleTimes.Count == 0 ? System.Array.Empty<float>() : sampleTimes.ToArray();
    }

    public void RestoreSamples(float[] data)
    {
        RestoreSamples(data, null);
    }

    public void RestoreSamples(float[] data, float[] times)
    {
        samples.Clear();
        sampleTimes.Clear();
        if (data != null && data.Length > 0)
        {
            samples.AddRange(data);
            while (samples.Count > maxSamples) samples.RemoveAt(0);
            if (times != null && times.Length > 0)
            {
                int start = Mathf.Max(0, times.Length - samples.Count);
                for (int i = start; i < times.Length && sampleTimes.Count < samples.Count; i++)
                    sampleTimes.Add(times[i]);
            }
            while (sampleTimes.Count < samples.Count)
            {
                float prev = sampleTimes.Count > 0 ? sampleTimes[sampleTimes.Count - 1] + 1f : 0f;
                sampleTimes.Add(prev);
            }
            while (sampleTimes.Count > samples.Count)
                sampleTimes.RemoveAt(0);
        }
        dirty = true;
    }

    public void Configure(string graphTitle, string graphUnit, Color color, float? threshold = null)
    {
        title = graphTitle ?? "GRAPH";
        unit = graphUnit ?? "";
        lineColor = color;
        thresholdY = threshold;
        EnsureLabels();
        ApplyThemeColors();
        if (lblTitle)
            lblTitle.text = string.IsNullOrEmpty(unit) ? title : title + "  (" + unit + ")";
        if (lblCur) lblCur.color = color;
        dirty = true;
    }

    public void Push(float value)
    {
        float next = sampleTimes.Count > 0 ? sampleTimes[sampleTimes.Count - 1] + 1f : 0f;
        Push(value, next);
    }

    public void Push(float value, float time)
    {
        samples.Add(value);
        sampleTimes.Add(time);
        while (samples.Count > maxSamples)
        {
            samples.RemoveAt(0);
            if (sampleTimes.Count > 0) sampleTimes.RemoveAt(0);
        }
        while (sampleTimes.Count > samples.Count)
            sampleTimes.RemoveAt(0);
        dirty = true;
    }

    public void ResetView()
    {
        viewZoom = 1f;
        viewPanX = 0f;
        viewPanY = 0f;
        customView = false;
        baseYValid = false;
        dirty = true;
    }

    public void ZoomIn() => ZoomBy(ButtonZoomStep, 0.5f, 0.5f);
    public void ZoomOut() => ZoomBy(1f / ButtonZoomStep, 0.5f, 0.5f);

    public void ZoomBy(float factor, float pivotNX = 0.5f, float pivotNY = 0.5f)
    {
        if (factor <= 0f || Mathf.Approximately(factor, 1f)) return;
        EnsureCustomView();

        float oldZoom = viewZoom;
        float newZoom = Mathf.Clamp(viewZoom * factor, MinZoom, MaxZoom);
        if (Mathf.Approximately(oldZoom, newZoom)) return;

        // Keep the data point under pivotNX/NY stable while zoom changes.
        float halfOld = 0.5f / oldZoom;
        float x0 = 0.5f + viewPanX - halfOld;
        float y0 = 0.5f + viewPanY - halfOld;
        float dataX = x0 + pivotNX / oldZoom;
        float dataY = y0 + pivotNY / oldZoom;

        viewZoom = newZoom;
        float half = 0.5f / viewZoom;
        viewPanX = dataX - pivotNX / viewZoom - 0.5f + half;
        viewPanY = dataY - pivotNY / viewZoom - 0.5f + half;
        ClampPan();

        if (viewZoom <= MinZoom + 1e-4f && Mathf.Abs(viewPanX) < 1e-4f && Mathf.Abs(viewPanY) < 1e-4f)
            ResetView();
        else
            dirty = true;
    }

    public void PanByNormalized(float dxVisible, float dyVisible)
    {
        EnsureCustomView();
        float span = 1f / viewZoom;
        viewPanX -= dxVisible * span;
        viewPanY -= dyVisible * span;
        ClampPan();
        dirty = true;
    }

    void EnsureCustomView()
    {
        if (customView && baseYValid) return;
        // Lock current auto Y as the zoom/pan base so live RestoreSamples won't fight the view.
        ComputeAutoY(out float lo, out float hi);
        baseYMin = lo;
        baseYMax = hi;
        baseYValid = true;
        customView = true;
    }

    void ClampPan()
    {
        // Keep at least a sliver of the [0,1] data range visible; allow pan at zoom==1.
        float half = 0.5f / viewZoom;
        float maxPan = half + 0.45f;
        viewPanX = Mathf.Clamp(viewPanX, -maxPan, maxPan);
        viewPanY = Mathf.Clamp(viewPanY, -maxPan, maxPan);
    }

    void ComputeAutoY(out float lo, out float hi)
    {
        lo = 0f;
        hi = 1f;
        if (samples.Count == 0) return;

        lo = float.MaxValue;
        hi = float.MinValue;
        for (int i = 0; i < samples.Count; i++)
        {
            float s = samples[i];
            if (s < lo) lo = s;
            if (s > hi) hi = s;
        }
        if (thresholdY.HasValue)
        {
            lo = Mathf.Min(lo, thresholdY.Value);
            hi = Mathf.Max(hi, thresholdY.Value);
        }
        if (showZeroLine)
        {
            lo = Mathf.Min(lo, 0f);
            hi = Mathf.Max(hi, 0f);
        }
        if (Mathf.Approximately(lo, hi)) { lo -= 1f; hi += 1f; }
        float pad = Mathf.Max(0.15f, (hi - lo) * 0.1f);
        lo -= pad;
        hi += pad;
        NiceBounds(ref lo, ref hi, out _);
    }

    void GetVisibleY(out float lo, out float hi)
    {
        if (!customView || !baseYValid)
        {
            ComputeAutoY(out lo, out hi);
            return;
        }

        float half = 0.5f / viewZoom;
        float n0 = 0.5f + viewPanY - half;
        float n1 = 0.5f + viewPanY + half;
        float span = baseYMax - baseYMin;
        lo = baseYMin + n0 * span;
        hi = baseYMin + n1 * span;
        if (hi - lo < 1e-6f)
        {
            lo -= 1f;
            hi += 1f;
        }
    }

    void GetVisibleX(out float x0, out float x1)
    {
        float half = 0.5f / Mathf.Max(MinZoom, viewZoom);
        x0 = 0.5f + viewPanX - half;
        x1 = 0.5f + viewPanX + half;
        if (!customView)
        {
            x0 = 0f;
            x1 = 1f;
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!interactiveView) return;
        float scroll = eventData.scrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        RectTransform rt = image != null ? image.rectTransform : transform as RectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;

        Rect r = rt.rect;
        float pivotNX = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
        float pivotNY = Mathf.InverseLerp(r.yMin, r.yMax, local.y);
        // Account for left gutter (~48px of texture) roughly via plot fraction if sizes known.
        if (w > 0 && plotW > 0)
        {
            float gutter = plotL / (float)w;
            float plotFrac = plotW / (float)w;
            pivotNX = Mathf.Clamp01((pivotNX - gutter) / Mathf.Max(1e-4f, plotFrac));
        }

        float factor = scroll > 0f ? WheelZoomStep : 1f / WheelZoomStep;
        ZoomBy(factor, pivotNX, pivotNY);
        RefreshHoverFromScreen(eventData.position, eventData.pressEventCamera);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!interactiveView) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        dragging = true;
        EnsureCustomView();
        SetHoverUiVisible(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!interactiveView || !dragging) return;
        RectTransform rt = image != null ? image.rectTransform : transform as RectTransform;
        float pw = Mathf.Max(1f, rt.rect.width);
        float ph = Mathf.Max(1f, rt.rect.height);
        // Grab-the-paper: drag right moves content right => show earlier samples (decrease pan? )
        // Visible window moves opposite to drag delta.
        float dx = eventData.delta.x / pw;
        float dy = eventData.delta.y / ph;
        PanByNormalized(dx, dy);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        if (pointerInside)
            RefreshHoverFromScreen(eventData.position, eventData.pressEventCamera);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactiveView) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (eventData.dragging) return;
        float t = Time.unscaledTime;
        if (t - lastClickTime < 0.35f)
        {
            ResetView();
            lastClickTime = -10f;
        }
        else lastClickTime = t;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        if (interactiveView && !dragging)
            RefreshHoverFromScreen(eventData.position, eventData.enterEventCamera);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        ClearHover();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!interactiveView || dragging) return;
        pointerInside = true;
        RefreshHoverFromScreen(eventData.position, eventData.enterEventCamera != null ? eventData.enterEventCamera : eventData.pressEventCamera);
    }

    void ClearHover()
    {
        hoverValid = false;
        hoverSampleIndex = -1;
        SetHoverUiVisible(false);
    }

    void SetHoverUiVisible(bool on)
    {
        if (hoverMarker) hoverMarker.gameObject.SetActive(on);
        if (tooltipRoot) tooltipRoot.gameObject.SetActive(on);
    }

    void RefreshHoverFromScreen(Vector2 screenPos, Camera cam)
    {
        if (!interactiveView || image == null)
        {
            ClearHover();
            return;
        }

        RectTransform imgRt = image.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(imgRt, screenPos, cam, out Vector2 local))
        {
            ClearHover();
            return;
        }

        hoverLocalInImage = local;
        Rect r = imgRt.rect;
        float nxImg = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
        float nyImg = Mathf.InverseLerp(r.yMin, r.yMax, local.y);

        if (w <= 0 || plotW <= 0)
        {
            ClearHover();
            return;
        }

        float gutter = plotL / (float)w;
        float plotFracX = plotW / (float)w;
        float botFrac = plotB / (float)h;
        float plotFracY = plotH / (float)h;
        float nx = (nxImg - gutter) / Mathf.Max(1e-4f, plotFracX);
        float ny = (nyImg - botFrac) / Mathf.Max(1e-4f, plotFracY);
        if (nx < 0f || nx > 1f || ny < 0f || ny > 1f || samples.Count == 0)
        {
            ClearHover();
            return;
        }

        GetVisibleX(out float vx0, out float vx1);
        float vxSpan = Mathf.Max(1e-6f, vx1 - vx0);
        float dataX = vx0 + nx * vxSpan;

        int n = samples.Count;
        float shift = maxSamples - n;
        float denom = (float)Mathf.Max(1, maxSamples - 1);
        float iFloat = dataX * denom - shift;
        int idx = Mathf.Clamp(Mathf.RoundToInt(iFloat), 0, n - 1);
        hoverSampleIndex = idx;
        hoverSampleValue = samples[idx];
        hoverValid = true;

        UpdateHoverUi(imgRt);
    }

    void UpdateHoverUi(RectTransform imgRt)
    {
        EnsureHoverUi(labelRoot != null ? labelRoot : transform);
        SetHoverUiVisible(true);

        RectTransform root = labelRoot != null ? labelRoot : transform as RectTransform;
        if (root == null || imgRt == null) return;

        // Nearest sample position in image-normalized space (marker follows curve)
        GetVisibleX(out float vx0, out float vx1);
        GetVisibleY(out float lo, out float hi);
        float vxSpan = Mathf.Max(1e-6f, vx1 - vx0);
        int n = samples.Count;
        float shift = maxSamples - n;
        float denom = (float)Mathf.Max(1, maxSamples - 1);
        float sampleNxData = (hoverSampleIndex + shift) / denom;
        float sampleNxPlot = (sampleNxData - vx0) / vxSpan;
        float sampleNyPlot = Mathf.InverseLerp(lo, hi, hoverSampleValue);
        float sampleNxImg = (plotL + sampleNxPlot * plotW) / (float)w;
        float sampleNyImg = (plotB + sampleNyPlot * plotH) / (float)h;
        sampleNxImg = Mathf.Clamp01(sampleNxImg);
        sampleNyImg = Mathf.Clamp01(sampleNyImg);

        Rect ir = imgRt.rect;
        Vector2 sampleLocalInImage = new Vector2(
            Mathf.Lerp(ir.xMin, ir.xMax, sampleNxImg),
            Mathf.Lerp(ir.yMin, ir.yMax, sampleNyImg));
        // Image and labelRoot may differ (inset plot vs host) â€” convert via world.
        Vector2 sampleLocalInRoot = root.InverseTransformPoint(imgRt.TransformPoint(sampleLocalInImage));

        if (hoverMarker != null)
            hoverMarker.anchoredPosition = sampleLocalInRoot;

        // Compact tooltip: sample value (+ unit) â€” position follows cursor, not sample.
        string u = string.IsNullOrEmpty(unit) ? "" : " " + unit;
        string tipText = Fmt(hoverSampleValue) + u;
        if (lblTooltip != null)
            lblTooltip.text = tipText;

        if (tooltipRoot != null)
        {
            float tipW = 64f;
            float tipH = 22f;
            if (lblTooltip != null)
            {
                lblTooltip.ForceMeshUpdate();
                var pref = lblTooltip.GetPreferredValues(tipText, 160f, 40f);
                tipW = Mathf.Clamp(pref.x + 16f, 48f, 160f);
                tipH = Mathf.Clamp(pref.y + 8f, 20f, 36f);
            }
            tooltipRoot.sizeDelta = new Vector2(tipW, tipH);

            // Cursor in labelRoot local space (same space as center-anchored tip).
            Vector2 cursorLocalInRoot = root.InverseTransformPoint(imgRt.TransformPoint(hoverLocalInImage));
            // Slightly below/right of pointer so the tip does not cover the cursor hotspot.
            Vector2 tipPos = cursorLocalInRoot + new Vector2(14f, -12f);

            Rect rr = root.rect;
            // Pivot is top-left (0,1): tip extends +X and -Y from tipPos.
            tipPos.x = Mathf.Clamp(tipPos.x, rr.xMin + 4f, rr.xMax - tipW - 4f);
            tipPos.y = Mathf.Clamp(tipPos.y, rr.yMin + tipH + 4f, rr.yMax - 4f);

            tooltipRoot.anchoredPosition = tipPos;
            if (hoverMarker != null) hoverMarker.SetAsLastSibling();
            tooltipRoot.SetAsLastSibling();
        }
    }

    void LateUpdate()
    {
        EnsureLabels();
        if (image == null) return;

        BringLabelsForward();

        if (dirty || tex == null
            || (image.rectTransform.rect.width > 8f
                && (Mathf.Abs(image.rectTransform.rect.width - w) > 6f
                    || Mathf.Abs(image.rectTransform.rect.height - h) > 6f)))
        {
            if (tex == null
                || (image.rectTransform.rect.width > 8f
                    && (Mathf.Abs(image.rectTransform.rect.width - w) > 6f
                        || Mathf.Abs(image.rectTransform.rect.height - h) > 6f)))
                RebuildTexture();

            Draw();
            dirty = false;

            // Re-sync hover after redraw (plot metrics may have changed)
            if (hoverValid && pointerInside && interactiveView && !dragging)
            {
                // Keep last local; remap with updated plotL etc.
                // Use stored image-local point
                RectTransform imgRt = image.rectTransform;
                Rect r = imgRt.rect;
                float nxImg = Mathf.InverseLerp(r.xMin, r.xMax, hoverLocalInImage.x);
                float nyImg = Mathf.InverseLerp(r.yMin, r.yMax, hoverLocalInImage.y);
                // Recompute sample under same cursor X
                float gutter = plotL / (float)w;
                float plotFracX = plotW / (float)w;
                float botFrac = plotB / (float)h;
                float plotFracY = plotH / (float)h;
                float nx = (nxImg - gutter) / Mathf.Max(1e-4f, plotFracX);
                float ny = (nyImg - botFrac) / Mathf.Max(1e-4f, plotFracY);
                if (nx >= 0f && nx <= 1f && ny >= 0f && ny <= 1f && samples.Count > 0)
                {
                    GetVisibleX(out float vx0, out float vx1);
                    float dataX = vx0 + nx * Mathf.Max(1e-6f, vx1 - vx0);
                    int n = samples.Count;
                    float shift = maxSamples - n;
                    float denom = (float)Mathf.Max(1, maxSamples - 1);
                    hoverSampleIndex = Mathf.Clamp(Mathf.RoundToInt(dataX * denom - shift), 0, n - 1);
                    hoverSampleValue = samples[hoverSampleIndex];
                    UpdateHoverUi(imgRt);
                }
                else ClearHover();
            }
        }
        else if (hoverValid && interactiveView && tooltipRoot != null && tooltipRoot.gameObject.activeSelf)
        {
            // Keep marker + tooltip above siblings even when not redrawing
            if (hoverMarker != null) hoverMarker.SetAsLastSibling();
            tooltipRoot.SetAsLastSibling();
        }
    }

    string Fmt(float v) => v.ToString(valueFormat);

    void Draw()
    {
        if (tex == null) return;
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = bgColor;

        // Border
        for (int x = 0; x < w; x++)
        {
            pixels[x] = borderColor;
            pixels[(h - 1) * w + x] = borderColor;
        }
        for (int y = 0; y < h; y++)
        {
            pixels[y * w] = borderColor;
            pixels[y * w + w - 1] = borderColor;
        }

        GetVisibleY(out float lo, out float hi);
        DisplayMin = lo;
        DisplayMax = hi;

        plotL = interactiveView ? 56 : 48;
        plotR = w - 3;
        plotB = interactiveView ? 18 : 14;
        plotT = h - 3;
        plotH = Mathf.Max(1, plotT - plotB);
        plotW = Mathf.Max(1, plotR - plotL);

        GetVisibleX(out float vx0, out float vx1);
        float vxSpan = Mathf.Max(1e-6f, vx1 - vx0);

        Color gut = Color.Lerp(bgColor, borderColor, 0.12f);
        for (int y = 1; y < h - 1; y++)
        for (int x = 1; x < plotL; x++)
            pixels[y * w + x] = gut;
        for (int y = 1; y < plotB; y++)
        for (int x = plotL; x < w - 1; x++)
            pixels[y * w + x] = gut;
        for (int y = 1; y < h - 1; y++)
            pixels[y * w + plotL] = Color.Lerp(borderColor, axisColor, 0.4f);
        for (int x = plotL; x < w - 1; x++)
            pixels[plotB * w + x] = Color.Lerp(borderColor, axisColor, 0.55f);

        int xShown = interactiveView ? XTickCapacity : 3;
        for (int i = 0; i < xShown; i++)
        {
            float frac = xShown <= 1 ? 0.5f : i / (float)(xShown - 1);
            if (!TryTimeAtPlotFrac(frac, out _)) continue;
            int tx = Mathf.Clamp(Mathf.RoundToInt(plotL + frac * plotW), plotL, plotR);
            for (int y = 1; y < plotB; y++)
                pixels[y * w + tx] = axisColor;
        }

        int hDiv = interactiveView ? (YTickCapacity - 1) : 4;
        for (int i = 1; i < hDiv; i++)
        {
            int gy = plotB + plotH * i / hDiv;
            for (int x = plotL + 1; x < plotR; x++)
                pixels[gy * w + x] = gridColor;
        }
        int gxStep = Mathf.Max(10, plotW / 5);
        for (int gx = plotL + gxStep; gx < plotR; gx += gxStep)
            for (int y = plotB; y <= plotT; y++)
                pixels[y * w + gx] = Color.Lerp(pixels[y * w + gx], gridColor, 0.7f);

        if (thresholdY.HasValue && thresholdY.Value >= lo && thresholdY.Value <= hi)
        {
            int ty = plotB + Mathf.RoundToInt(Mathf.InverseLerp(lo, hi, thresholdY.Value) * plotH);
            ty = Mathf.Clamp(ty, plotB, plotT);
            for (int x = plotL + 1; x < plotR; x += 2)
                pixels[ty * w + x] = thresholdColor;
        }

        if (showZeroLine && lo < -1e-4f && hi > 1e-4f)
        {
            int zy = plotB + Mathf.RoundToInt(Mathf.InverseLerp(lo, hi, 0f) * plotH);
            zy = Mathf.Clamp(zy, plotB, plotT);
            for (int x = plotL + 1; x < plotR; x++)
                pixels[zy * w + x] = axisColor;
        }

        int n = samples.Count;
        if (n >= 2)
        {
            float shift = maxSamples - n;
            float denom = (float)Mathf.Max(1, maxSamples - 1);
            float XOf(int i)
            {
                float nx = (i + shift) / denom;
                return plotL + (nx - vx0) / vxSpan * plotW;
            }
            float YOf(float v) => plotB + Mathf.InverseLerp(lo, hi, v) * plotH;

            if (showFill)
            {
                // Fill toward zero so zooming past 0 does not flip the band
                // (e.g. all-negative window used to snap baseline to plotB).
                int zeroY;
                if (showZeroLine && lo < 0f && hi > 0f)
                    zeroY = Mathf.RoundToInt(YOf(0f));
                else if (showZeroLine && hi <= 0f)
                    zeroY = plotT; // zero is above the view
                else
                    zeroY = plotB; // zero at/below view, or zero line off
                zeroY = Mathf.Clamp(zeroY, plotB, plotT);
                for (int i = 0; i < n; i++)
                {
                    float xf = XOf(i);
                    if (xf < plotL - 1 || xf > plotR + 1) continue;
                    int x = Mathf.Clamp(Mathf.RoundToInt(xf), plotL + 1, plotR - 1);
                    int y = Mathf.Clamp(Mathf.RoundToInt(YOf(samples[i])), plotB, plotT);
                    int y0 = Mathf.Min(y, zeroY);
                    int y1 = Mathf.Max(y, zeroY);
                    for (int yy = y0; yy <= y1; yy++)
                        pixels[yy * w + x] = Color.Lerp(pixels[yy * w + x], fillColor, 0.85f);
                }
            }

            for (int i = 1; i < n; i++)
            {
                float xA = XOf(i - 1), xB = XOf(i);
                // Skip segments fully outside
                if ((xA < plotL && xB < plotL) || (xA > plotR && xB > plotR)) continue;
                DrawLine(pixels, w, h,
                    xA, YOf(samples[i - 1]),
                    xB, YOf(samples[i]),
                    lineColor, plotL, plotR, plotB, plotT);
            }

            float xLast = XOf(n - 1);
            if (xLast >= plotL && xLast <= plotR)
            {
                int mx = Mathf.Clamp(Mathf.RoundToInt(xLast), plotL + 2, plotR - 2);
                int my = Mathf.Clamp(Mathf.RoundToInt(YOf(samples[n - 1])), plotB + 2, plotT - 2);
                Color mk = Color.Lerp(lineColor, Color.white, 0.35f);
                for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    if (dx * dx + dy * dy <= 4)
                        pixels[(my + dy) * w + (mx + dx)] = mk;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false);

        UpdateYTickTexts(lo, hi);
        LayoutXTicks();

        if (lblTitle)
            lblTitle.text = string.IsNullOrEmpty(unit) ? title : title + "  (" + unit + ")";

        if (lblCur)
        {
            if (n > 0)
            {
                string u = string.IsNullOrEmpty(unit) ? "" : " " + unit;
                lblCur.text = Fmt(samples[n - 1]) + u;
            }
            else lblCur.text = "-";
            lblCur.color = lineColor;
        }

        ApplyLabelStyle();
        if (lblCur) lblCur.color = lineColor;
        LayoutYTicks();
    }

    void UpdateYTickTexts(float lo, float hi)
    {
        if (lblYTicks == null) return;
        for (int i = 0; i < YTickCapacity; i++)
        {
            var t = lblYTicks[i];
            if (t == null) continue;
            bool on = interactiveView || i == 0 || i == YTickCapacity / 2 || i == YTickCapacity - 1;
            if (!on) continue;
            float frac = i / (float)(YTickCapacity - 1);
            float v = Mathf.Lerp(lo, hi, frac);
            t.text = Fmt(v);
        }
    }

    static void NiceBounds(ref float lo, ref float hi, out float step)
    {
        float range = hi - lo;
        if (range <= 1e-8f) { step = 1f; return; }
        float mag = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(range)));
        if (mag < 1e-8f) mag = 1e-8f;
        step = mag;
        if (range / step > 5f) step = mag * 2f;
        if (range / step > 5f) step = mag * 5f;
        lo = Mathf.Floor(lo / step) * step;
        hi = Mathf.Ceil(hi / step) * step;
        if (Mathf.Approximately(lo, hi)) hi = lo + step;
    }

    static void DrawLine(Color[] px, int w, int h, float x0, float y0, float x1, float y1, Color c,
        int clipL, int clipR, int clipB, int clipT)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1)) * 1.6f));
        for (int s = 0; s <= steps; s++)
        {
            float t = s / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
            if (x < clipL || x > clipR || y < clipB || y > clipT) continue;
            px[y * w + x] = c;
            if (y + 1 <= clipT) px[(y + 1) * w + x] = Color.Lerp(px[(y + 1) * w + x], c, 0.5f);
            if (y - 1 >= clipB) px[(y - 1) * w + x] = Color.Lerp(px[(y - 1) * w + x], c, 0.35f);
        }
    }
}
