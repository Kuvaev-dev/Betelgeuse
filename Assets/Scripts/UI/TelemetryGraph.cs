using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Real-time strip chart на RawImage (Texture2D).
/// </summary>
[RequireComponent(typeof(RawImage))]
public class TelemetryGraph : MonoBehaviour
{
    public string title = "GRAPH";
    public Color lineColor = new(0.24f, 0.88f, 1f, 1f);
    public Color gridColor = new(1f, 1f, 1f, 0.08f);
    public Color bgColor = new(0.015f, 0.02f, 0.045f, 1f);
    public int maxSamples = 300;
    public float minY = 0f;
    public float maxY = 1f;
    public bool autoScale = true;

    RawImage image;
    Texture2D tex;
    readonly List<float> samples = new();
    int w = 320, h = 90;
    bool dirty = true;

    void Awake()
    {
        image = GetComponent<RawImage>();
        RebuildTexture();
    }

    void RebuildTexture()
    {
        if (tex != null) Destroy(tex);
        var rt = image.rectTransform.rect;
        w = Mathf.Max(64, Mathf.RoundToInt(rt.width > 10 ? rt.width : 320));
        h = Mathf.Max(40, Mathf.RoundToInt(rt.height > 10 ? rt.height : 90));
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
    }

    public void Push(float value)
    {
        samples.Add(value);
        while (samples.Count > maxSamples) samples.RemoveAt(0);
        dirty = true;
    }

    void LateUpdate()
    {
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

        // Grid
        for (int gx = 0; gx < w; gx += w / 6)
            for (int y = 0; y < h; y++)
                pixels[y * w + gx] = gridColor;
        for (int gy = 0; gy < h; gy += h / 4)
            for (int x = 0; x < w; x++)
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
            if (Mathf.Approximately(lo, hi)) { lo -= 1f; hi += 1f; }
            float pad = (hi - lo) * 0.08f;
            lo -= pad; hi += pad;
        }

        int n = samples.Count;
        for (int i = 1; i < n; i++)
        {
            float x0 = (i - 1) / (float)(maxSamples - 1) * (w - 1);
            float x1 = i / (float)(maxSamples - 1) * (w - 1);
            // right-align live data
            float shift = (maxSamples - n);
            x0 = (i - 1 + shift) / (float)(maxSamples - 1) * (w - 1);
            x1 = (i + shift) / (float)(maxSamples - 1) * (w - 1);

            float y0 = Mathf.InverseLerp(lo, hi, samples[i - 1]) * (h - 1);
            float y1 = Mathf.InverseLerp(lo, hi, samples[i]) * (h - 1);
            DrawLine(pixels, w, h, x0, y0, x1, y1, lineColor);
        }

        // Zero line if in range
        if (lo < 0f && hi > 0f)
        {
            int zy = Mathf.RoundToInt(Mathf.InverseLerp(lo, hi, 0f) * (h - 1));
            Color zc = new(1f, 0.7f, 0.2f, 0.35f);
            for (int x = 0; x < w; x++) pixels[zy * w + x] = zc;
        }

        tex.SetPixels(pixels);
        tex.Apply(false);
    }

    static void DrawLine(Color[] px, int w, int h, float x0, float y0, float x1, float y1, Color c)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1))));
        for (int s = 0; s <= steps; s++)
        {
            float t = s / (float)steps;
            int x = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), 0, w - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), 0, h - 1);
            px[y * w + x] = c;
            // thickness
            if (y + 1 < h) px[(y + 1) * w + x] = Color.Lerp(px[(y + 1) * w + x], c, 0.6f);
            if (x + 1 < w) px[y * w + x + 1] = Color.Lerp(px[y * w + x + 1], c, 0.5f);
        }
    }
}
