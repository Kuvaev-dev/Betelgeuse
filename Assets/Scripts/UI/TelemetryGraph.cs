using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Real-time strip chart: сітка, вісь 0, підписи min/max/current, поріг.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class TelemetryGraph : MonoBehaviour
{
    public string title = "GRAPH";
    public string unit = "";
    public Color lineColor = new(0.9f, 0.9f, 0.92f, 1f);
    public Color fillColor = new(0.75f, 0.75f, 0.78f, 0.12f);
    public Color gridColor = new(1f, 1f, 1f, 0.08f);
    public Color bgColor = new(0.06f, 0.06f, 0.07f, 1f);
    public Color axisColor = new(0.7f, 0.7f, 0.72f, 0.5f);
    public Color thresholdColor = new(0.85f, 0.55f, 0.55f, 0.4f);
    public int maxSamples = 280;
    public float minY = 0f;
    public float maxY = 1f;
    public bool autoScale = true;
    public bool showFill = true;
    public bool showZeroLine = true;
    public float? thresholdY = null;
    public string valueFormat = "F1";

    RawImage image;
    Texture2D tex;
    readonly List<float> samples = new();
    int w = 320, h = 100;
    bool dirty = true;

    TMP_Text lblMin, lblMax, lblCur, lblTitle;

    public float LastValue => samples.Count > 0 ? samples[samples.Count - 1] : 0f;
    public float DisplayMin { get; private set; }
    public float DisplayMax { get; private set; }

    void Awake()
    {
        image = GetComponent<RawImage>();
        EnsureLabels();
        RebuildTexture();
    }

    void EnsureLabels()
    {
        if (lblTitle != null) return;
        var parent = transform;

        lblTitle = MakeLabel(parent, "GTitle", 11, new Color(0.55f, 0.62f, 0.75f), TextAlignmentOptions.Left);
        PinLabel(lblTitle.rectTransform, 0f, 1f, 0f, 1f, 4, 2, 180, 16);

        lblMax = MakeLabel(parent, "GMax", 10, new Color(0.7f, 0.78f, 0.9f), TextAlignmentOptions.Right);
        PinLabel(lblMax.rectTransform, 1f, 1f, 1f, 1f, -4, 2, 90, 14);

        lblMin = MakeLabel(parent, "GMin", 10, new Color(0.7f, 0.78f, 0.9f), TextAlignmentOptions.Right);
        PinLabel(lblMin.rectTransform, 1f, 0f, 1f, 0f, -4, 2, 90, 14);

        lblCur = MakeLabel(parent, "GCur", 12, lineColor, TextAlignmentOptions.Left);
        lblCur.fontStyle = FontStyles.Bold;
        PinLabel(lblCur.rectTransform, 0f, 0f, 0f, 0f, 4, 2, 140, 16);
    }

    static TMP_Text MakeLabel(Transform parent, string name, float size, Color c, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.color = c;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        // Slight shadow for readability on graph
        var outline = go.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.75f);
        outline.effectDistance = new Vector2(1f, -1f);
        return tmp;
    }

    static void PinLabel(RectTransform rt, float ax0, float ay0, float ax1, float ay1,
        float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(ax0, ay0);
        rt.anchorMax = new Vector2(ax1, ay1);
        rt.pivot = new Vector2(ax0 < 0.5f ? 0f : 1f, ay0 < 0.5f ? 0f : 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    void RebuildTexture()
    {
        if (tex != null) Destroy(tex);
        var rt = image.rectTransform.rect;
        w = Mathf.Max(64, Mathf.RoundToInt(rt.width > 10 ? rt.width : 320));
        h = Mathf.Max(48, Mathf.RoundToInt(rt.height > 10 ? rt.height : 100));
        tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        image.texture = tex;
        dirty = true;
    }

    public void Clear()
    {
        samples.Clear();
        dirty = true;
        if (lblCur) lblCur.text = "—";
        if (lblMin) lblMin.text = "";
        if (lblMax) lblMax.text = "";
    }

    public void Configure(string graphTitle, string graphUnit, Color color, float? threshold = null)
    {
        title = graphTitle;
        unit = graphUnit ?? "";
        lineColor = color;
        fillColor = new Color(color.r, color.g, color.b, 0.14f);
        thresholdY = threshold;
        if (lblTitle) lblTitle.text = string.IsNullOrEmpty(unit) ? title : $"{title} ({unit})";
        if (lblCur) lblCur.color = color;
    }

    public void Push(float value)
    {
        samples.Add(value);
        while (samples.Count > maxSamples) samples.RemoveAt(0);
        dirty = true;
    }

    void LateUpdate()
    {
        if (lblTitle != null && string.IsNullOrEmpty(lblTitle.text))
            lblTitle.text = string.IsNullOrEmpty(unit) ? title : $"{title} ({unit})";

        if (!dirty || tex == null) return;
        if (image.rectTransform.rect.width > 10 &&
            (Mathf.Abs(image.rectTransform.rect.width - w) > 8f
             || Mathf.Abs(image.rectTransform.rect.height - h) > 8f))
            RebuildTexture();

        Draw();
        dirty = false;
    }

    void Draw()
    {
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = bgColor;

        // Subtle border
        Color border = new(0.2f, 0.4f, 0.65f, 0.35f);
        for (int x = 0; x < w; x++)
        {
            pixels[x] = border;
            pixels[(h - 1) * w + x] = border;
        }
        for (int y = 0; y < h; y++)
        {
            pixels[y * w] = border;
            pixels[y * w + (w - 1)] = border;
        }

        // Grid
        int gxStep = Mathf.Max(1, w / 6);
        int gyStep = Mathf.Max(1, h / 4);
        for (int gx = gxStep; gx < w - 1; gx += gxStep)
            for (int y = 1; y < h - 1; y++)
                pixels[y * w + gx] = gridColor;
        for (int gy = gyStep; gy < h - 1; gy += gyStep)
            for (int x = 1; x < w - 1; x++)
                pixels[gy * w + x] = gridColor;

        if (samples.Count < 2)
        {
            tex.SetPixels(pixels);
            tex.Apply(false);
            return;
        }

        float lo = minY, hi = maxY;
        if (autoScale)
        {
            lo = float.MaxValue;
            hi = float.MinValue;
            foreach (float s in samples)
            {
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
            float pad = (hi - lo) * 0.10f;
            if (pad < 0.5f) pad = 0.5f;
            lo -= pad;
            hi += pad;
        }

        // Nice round bounds for readability
        NiceBounds(ref lo, ref hi);
        DisplayMin = lo;
        DisplayMax = hi;

        // Threshold band
        if (thresholdY.HasValue && thresholdY.Value >= lo && thresholdY.Value <= hi)
        {
            int ty = Mathf.RoundToInt(Mathf.InverseLerp(lo, hi, thresholdY.Value) * (h - 1));
            for (int x = 1; x < w - 1; x++)
            {
                pixels[ty * w + x] = thresholdColor;
                if (ty + 1 < h - 1) pixels[(ty + 1) * w + x] = Color.Lerp(pixels[(ty + 1) * w + x], thresholdColor, 0.35f);
            }
        }

        // Zero line
        if (showZeroLine && lo < 0f && hi > 0f)
        {
            int zy = Mathf.RoundToInt(Mathf.InverseLerp(lo, hi, 0f) * (h - 1));
            for (int x = 1; x < w - 1; x++)
                pixels[zy * w + x] = axisColor;
        }

        int n = samples.Count;
        float shift = maxSamples - n;

        // Fill under curve
        if (showFill)
        {
            int zeroY = showZeroLine && lo < 0f && hi > 0f
                ? Mathf.RoundToInt(Mathf.InverseLerp(lo, hi, 0f) * (h - 1))
                : 0;
            for (int i = 0; i < n; i++)
            {
                float xf = (i + shift) / (float)(maxSamples - 1) * (w - 1);
                int x = Mathf.Clamp(Mathf.RoundToInt(xf), 1, w - 2);
                int y = Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(lo, hi, samples[i]) * (h - 1)), 1, h - 2);
                int y0 = Mathf.Min(y, zeroY);
                int y1 = Mathf.Max(y, zeroY);
                for (int yy = y0; yy <= y1; yy++)
                    pixels[yy * w + x] = Color.Lerp(pixels[yy * w + x], fillColor, 0.85f);
            }
        }

        // Line
        for (int i = 1; i < n; i++)
        {
            float x0 = (i - 1 + shift) / (float)(maxSamples - 1) * (w - 1);
            float x1 = (i + shift) / (float)(maxSamples - 1) * (w - 1);
            float y0 = Mathf.InverseLerp(lo, hi, samples[i - 1]) * (h - 1);
            float y1 = Mathf.InverseLerp(lo, hi, samples[i]) * (h - 1);
            DrawLine(pixels, w, h, x0, y0, x1, y1, lineColor);
        }

        // Current value marker
        {
            float xf = (n - 1 + shift) / (float)(maxSamples - 1) * (w - 1);
            int x = Mathf.Clamp(Mathf.RoundToInt(xf), 2, w - 3);
            int y = Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(lo, hi, samples[n - 1]) * (h - 1)), 2, h - 3);
            for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
            {
                if (dx * dx + dy * dy <= 5)
                    pixels[(y + dy) * w + (x + dx)] = Color.white;
            }
            pixels[y * w + x] = lineColor;
        }

        tex.SetPixels(pixels);
        tex.Apply(false);

        // Labels
        string u = string.IsNullOrEmpty(unit) ? "" : " " + unit;
        if (lblMax) lblMax.text = hi.ToString(valueFormat) + u;
        if (lblMin) lblMin.text = lo.ToString(valueFormat) + u;
        if (lblCur)
        {
            float cur = samples[n - 1];
            lblCur.text = $"● {cur.ToString(valueFormat)}{u}";
            lblCur.color = lineColor;
        }
        if (lblTitle)
            lblTitle.text = string.IsNullOrEmpty(unit) ? title : $"{title}";
    }

    static void NiceBounds(ref float lo, ref float hi)
    {
        float range = hi - lo;
        if (range <= 1e-8f) return;
        float mag = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(range)));
        if (mag < 1e-8f) mag = 1e-8f;
        float step = mag;
        if (range / step > 8f) step = mag * 2f;
        if (range / step > 8f) step = mag * 5f;
        lo = Mathf.Floor(lo / step) * step;
        hi = Mathf.Ceil(hi / step) * step;
        if (Mathf.Approximately(lo, hi)) hi = lo + step;
    }

    static void DrawLine(Color[] px, int w, int h, float x0, float y0, float x1, float y1, Color c)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1)) * 1.5f));
        for (int s = 0; s <= steps; s++)
        {
            float t = s / (float)steps;
            int x = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), 0, w - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), 0, h - 1);
            px[y * w + x] = c;
            if (y + 1 < h) px[(y + 1) * w + x] = Color.Lerp(px[(y + 1) * w + x], c, 0.65f);
            if (y > 0) px[(y - 1) * w + x] = Color.Lerp(px[(y - 1) * w + x], c, 0.4f);
            if (x + 1 < w) px[y * w + x + 1] = Color.Lerp(px[y * w + x + 1], c, 0.45f);
        }
    }
}
