using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Real-time strip chart. Labels are siblings on <see cref="labelRoot"/> (not children of RawImage)
/// so they always draw on top and stay readable.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class TelemetryGraph : MonoBehaviour
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

    /// <summary>Parent for TMP labels (usually graph frame root). If null, uses this.transform.</summary>
    public RectTransform labelRoot;

    RawImage image;
    Texture2D tex;
    readonly List<float> samples = new();
    int w = 300, h = 90;
    bool dirty = true;

    TMP_Text lblTitle, lblCur, lblMax, lblMid, lblMin;

    public float LastValue => samples.Count > 0 ? samples[samples.Count - 1] : 0f;
    public float DisplayMin { get; private set; }
    public float DisplayMax { get; private set; }

    void Awake()
    {
        image = GetComponent<RawImage>();
        if (labelRoot == null)
            labelRoot = transform as RectTransform;
        ApplyThemeColors();
        EnsureLabels();
        RebuildTexture();
    }

    public void BindLabelRoot(RectTransform root)
    {
        labelRoot = root != null ? root : transform as RectTransform;
        // Recreate labels under the correct parent
        DestroyLabels();
        EnsureLabels();
        dirty = true;
    }

    void DestroyLabels()
    {
        void Kill(TMP_Text t)
        {
            if (t != null) Destroy(t.gameObject);
        }
        Kill(lblTitle); Kill(lblCur); Kill(lblMax); Kill(lblMid); Kill(lblMin);
        lblTitle = lblCur = lblMax = lblMid = lblMin = null;
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
    }

    void EnsureLabels()
    {
        if (lblTitle != null) return;
        Transform p = labelRoot != null ? labelRoot : transform;

        lblTitle = MakeLabel(p, "GTitle", 11f, labelColor, TextAlignmentOptions.MidlineLeft);
        Stretch(lblTitle.rectTransform, 8f, -2f, 140f, 18f, 0f, 1f, 0f, 1f);

        lblCur = MakeLabel(p, "GCur", 12f, lineColor, TextAlignmentOptions.MidlineRight);
        lblCur.fontStyle = FontStyles.Bold;
        Stretch(lblCur.rectTransform, -8f, -2f, 130f, 18f, 1f, 1f, 1f, 1f);

        // Y scale: max top, mid center, min bottom — left side
        lblMax = MakeLabel(p, "GMax", 10f, labelColor, TextAlignmentOptions.MidlineLeft);
        Stretch(lblMax.rectTransform, 6f, -20f, 70f, 14f, 0f, 1f, 0f, 1f);

        lblMid = MakeLabel(p, "GMid", 10f, labelColor, TextAlignmentOptions.MidlineLeft);
        Stretch(lblMid.rectTransform, 6f, 0f, 70f, 14f, 0f, 0.5f, 0f, 0.5f);

        lblMin = MakeLabel(p, "GMin", 10f, labelColor, TextAlignmentOptions.MidlineLeft);
        Stretch(lblMin.rectTransform, 6f, 4f, 70f, 14f, 0f, 0f, 0f, 0f);

        // Always draw above plot texture
        if (labelRoot != null)
        {
            if (lblTitle) lblTitle.transform.SetAsLastSibling();
            if (lblCur) lblCur.transform.SetAsLastSibling();
            if (lblMax) lblMax.transform.SetAsLastSibling();
            if (lblMid) lblMid.transform.SetAsLastSibling();
            if (lblMin) lblMin.transform.SetAsLastSibling();
        }

        ApplyLabelStyle();
        // Immediate placeholder so user always sees something
        if (lblTitle) lblTitle.text = title;
        if (lblCur) lblCur.text = "—";
        if (lblMax) lblMax.text = "—";
        if (lblMid) lblMid.text = "—";
        if (lblMin) lblMin.text = "—";
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
        if (lblMax) { lblMax.color = muted; lblMax.fontSize = 10f; }
        if (lblMid) { lblMid.color = muted; lblMid.fontSize = 10f; }
        if (lblMin) { lblMin.color = muted; lblMin.fontSize = 10f; }
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
        dirty = true;
        EnsureLabels();
        if (lblCur) lblCur.text = "—";
        if (lblMax) lblMax.text = "—";
        if (lblMid) lblMid.text = "—";
        if (lblMin) lblMin.text = "—";
    }

    public float[] GetSamples()
    {
        return samples.Count == 0 ? System.Array.Empty<float>() : samples.ToArray();
    }

    public void RestoreSamples(float[] data)
    {
        samples.Clear();
        if (data != null && data.Length > 0)
        {
            samples.AddRange(data);
            while (samples.Count > maxSamples) samples.RemoveAt(0);
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
        samples.Add(value);
        while (samples.Count > maxSamples) samples.RemoveAt(0);
        dirty = true;
    }

    void LateUpdate()
    {
        EnsureLabels();
        if (image == null) return;

        // Keep labels on top every frame (scroll rebuilds can reorder)
        if (lblMin) lblMin.transform.SetAsLastSibling();
        if (lblMid) lblMid.transform.SetAsLastSibling();
        if (lblMax) lblMax.transform.SetAsLastSibling();
        if (lblTitle) lblTitle.transform.SetAsLastSibling();
        if (lblCur) lblCur.transform.SetAsLastSibling();

        if (!dirty && tex != null) return;

        if (tex == null
            || (image.rectTransform.rect.width > 8f
                && (Mathf.Abs(image.rectTransform.rect.width - w) > 6f
                    || Mathf.Abs(image.rectTransform.rect.height - h) > 6f)))
            RebuildTexture();

        Draw();
        dirty = false;
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

        // Always show a scale even with few samples
        float lo = 0f, hi = 1f;
        if (samples.Count > 0)
        {
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

        DisplayMin = lo;
        DisplayMax = hi;

        // Left margin for Y labels (plot starts later)
        int plotL = 48;
        int plotR = w - 3;
        int plotB = 3;
        int plotT = h - 3;
        int plotH = Mathf.Max(1, plotT - plotB);
        int plotW = Mathf.Max(1, plotR - plotL);

        // Soft left gutter
        Color gut = Color.Lerp(bgColor, borderColor, 0.12f);
        for (int y = 1; y < h - 1; y++)
        for (int x = 1; x < plotL; x++)
            pixels[y * w + x] = gut;
        for (int y = 1; y < h - 1; y++)
            pixels[y * w + plotL] = Color.Lerp(borderColor, axisColor, 0.4f);

        // Grid 4 bands
        for (int i = 1; i <= 3; i++)
        {
            int gy = plotB + plotH * i / 4;
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
            float XOf(int i) => plotL + (i + shift) / (float)Mathf.Max(1, maxSamples - 1) * plotW;
            float YOf(float v) => plotB + Mathf.InverseLerp(lo, hi, v) * plotH;

            if (showFill)
            {
                int zeroY = (showZeroLine && lo < 0f && hi > 0f)
                    ? Mathf.RoundToInt(YOf(0f)) : plotB;
                zeroY = Mathf.Clamp(zeroY, plotB, plotT);
                for (int i = 0; i < n; i++)
                {
                    int x = Mathf.Clamp(Mathf.RoundToInt(XOf(i)), plotL + 1, plotR - 1);
                    int y = Mathf.Clamp(Mathf.RoundToInt(YOf(samples[i])), plotB, plotT);
                    int y0 = Mathf.Min(y, zeroY);
                    int y1 = Mathf.Max(y, zeroY);
                    for (int yy = y0; yy <= y1; yy++)
                        pixels[yy * w + x] = Color.Lerp(pixels[yy * w + x], fillColor, 0.85f);
                }
            }

            for (int i = 1; i < n; i++)
                DrawLine(pixels, w, h,
                    XOf(i - 1), YOf(samples[i - 1]),
                    XOf(i), YOf(samples[i]),
                    lineColor, plotL, plotR, plotB, plotT);

            int mx = Mathf.Clamp(Mathf.RoundToInt(XOf(n - 1)), plotL + 2, plotR - 2);
            int my = Mathf.Clamp(Mathf.RoundToInt(YOf(samples[n - 1])), plotB + 2, plotT - 2);
            Color mk = Color.Lerp(lineColor, Color.white, 0.35f);
            for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
                if (dx * dx + dy * dy <= 4)
                    pixels[(my + dy) * w + (mx + dx)] = mk;
        }

        tex.SetPixels(pixels);
        tex.Apply(false);

        // Labels — always filled
        float mid = (lo + hi) * 0.5f;
        if (lblMax) lblMax.text = Fmt(hi);
        if (lblMid) lblMid.text = Fmt(mid);
        if (lblMin) lblMin.text = Fmt(lo);

        if (lblTitle)
            lblTitle.text = string.IsNullOrEmpty(unit) ? title : title + "  (" + unit + ")";

        if (lblCur)
        {
            if (n > 0)
            {
                string u = string.IsNullOrEmpty(unit) ? "" : " " + unit;
                lblCur.text = Fmt(samples[n - 1]) + u;
            }
            else lblCur.text = "—";
            lblCur.color = lineColor;
        }

        ApplyLabelStyle();
        if (lblCur) lblCur.color = lineColor;
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
