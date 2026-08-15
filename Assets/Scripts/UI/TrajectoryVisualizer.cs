using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Smooth landing trajectory: dense samples → Catmull-Rom + Chaikin → LineRenderer.
/// </summary>
public class TrajectoryVisualizer : MonoBehaviour
{
    public RocketPhysics rocketPhysics;
    public LineRenderer lineRenderer;
    public int maxRawPoints = 5000;
    public float baseLineWidth = 3.5f;
    public float minPointDistance = 0.85f;
    public float groundY = 0.5f;
    public int splineSamplesPerSeg = 8;
    public int chaikinPasses = 2;

    public Color goodColor = new(0.35f, 0.95f, 0.65f, 1f);
    public Color badColor = new(1f, 0.4f, 0.42f, 1f);
    public Color normalColor = new(0.45f, 0.9f, 1f, 1f);

    readonly List<Vector3> raw = new();
    readonly List<Vector3> display = new();
    Vector3 lastPoint;
    bool hasLast;
    bool finished;
    bool visible = true;

    public int PointCount => raw.Count;
    public IReadOnlyList<Vector3> Points => raw;
    public bool IsVisible => visible;

    void Awake()
    {
        EnsureLine();
        if (rocketPhysics == null)
            rocketPhysics = FindAnyObjectByType<RocketPhysics>();
    }

    void Start()
    {
        EnsureLine();
        ApplyVisibility();
    }

    void EnsureLine()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.numCapVertices = 12;
        lineRenderer.numCornerVertices = 12;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.allowOcclusionWhenDynamic = false;
        lineRenderer.sortingOrder = 100;
        lineRenderer.widthMultiplier = 1f;
        lineRenderer.generateLightingData = false;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;

        Shader shader = Shader.Find("Sprites/Default")
                        ?? Shader.Find("Unlit/Color")
                        ?? Shader.Find("Universal Render Pipeline/Unlit")
                        ?? Shader.Find("Hidden/Internal-Colored");
        if (shader != null)
        {
            var mat = new Material(shader);
            mat.color = Color.white;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = 3000;
            lineRenderer.sharedMaterial = mat;
        }

        ApplyColor(normalColor);
        SetWidth(baseLineWidth);
        lineRenderer.enabled = visible;
    }

    void SetWidth(float w)
    {
        if (lineRenderer == null) return;
        float ww = Mathf.Clamp(w, 1.2f, 14f);
        lineRenderer.startWidth = ww;
        lineRenderer.endWidth = ww * 0.65f;
    }

    public void SetVisible(bool on)
    {
        visible = on;
        ApplyVisibility();
    }

    public void ToggleVisible() => SetVisible(!visible);

    void ApplyVisibility()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = visible;
    }

    void FixedUpdate() => SampleFlight(false);

    void LateUpdate()
    {
        if (rocketPhysics == null)
            rocketPhysics = FindAnyObjectByType<RocketPhysics>();

        if (InFlight())
        {
            SampleFlight(false);
            RebuildSmooth(liveTip: rocketPhysics.state.position);
        }
        else if (display.Count >= 2 && lineRenderer != null
                 && lineRenderer.positionCount != display.Count)
        {
            PushDisplay();
        }

        if (visible && lineRenderer != null && Camera.main != null && display.Count > 0)
        {
            Vector3 mid = display[display.Count / 2];
            float d = Vector3.Distance(Camera.main.transform.position, mid);
            SetWidth(baseLineWidth + d * 0.007f);
        }
    }

    bool InFlight()
    {
        return !finished
               && rocketPhysics != null
               && rocketPhysics.simulationArmed
               && !rocketPhysics.state.simulationFinished
               && !rocketPhysics.state.isLanded;
    }

    public void SampleFlight(bool force = false)
    {
        if (finished) return;
        if (rocketPhysics == null)
        {
            rocketPhysics = FindAnyObjectByType<RocketPhysics>();
            if (rocketPhysics == null) return;
        }
        if (!rocketPhysics.simulationArmed) return;
        if (rocketPhysics.state.simulationFinished || rocketPhysics.state.isLanded) return;
        AddRaw(rocketPhysics.state.position, force);
    }

    void AddRaw(Vector3 p, bool force)
    {
        if (p.y < groundY) p.y = groundY;

        float minDist = minPointDistance;
        if (p.y < 200f) minDist = 0.7f;
        if (p.y < 60f) minDist = 0.4f;
        if (p.y < 15f) minDist = 0.2f;

        if (!force && hasLast && (p - lastPoint).sqrMagnitude < minDist * minDist)
            return;

        // Light EMA to kill RK4 jitter before spline
        if (hasLast && !force)
            p = Vector3.Lerp(lastPoint, p, 0.78f);

        if (raw.Count >= maxRawPoints)
            raw.RemoveAt(0);

        raw.Add(p);
        lastPoint = p;
        hasLast = true;
    }

    void RebuildSmooth(Vector3? liveTip)
    {
        if (raw.Count == 0 && !liveTip.HasValue)
        {
            display.Clear();
            PushDisplay();
            return;
        }

        // 1) Control points (decimated for stable spline)
        var ctrl = Decimate(raw, 0.55f);
        if (liveTip.HasValue)
        {
            Vector3 tip = liveTip.Value;
            if (tip.y < groundY) tip.y = groundY;
            if (ctrl.Count == 0 || (ctrl[ctrl.Count - 1] - tip).sqrMagnitude > 0.01f)
                ctrl.Add(tip);
            else
                ctrl[ctrl.Count - 1] = tip;
        }

        if (ctrl.Count == 1)
        {
            display.Clear();
            display.Add(ctrl[0]);
            display.Add(ctrl[0] + Vector3.up * 0.4f);
            PushDisplay();
            return;
        }

        // 2) Catmull-Rom densify
        var spline = CatmullRom(ctrl, splineSamplesPerSeg);

        // 3) Chaikin corner-cutting → smooth ribbon
        display.Clear();
        display.AddRange(Chaikin(spline, chaikinPasses));

        for (int i = 0; i < display.Count; i++)
        {
            var p = display[i];
            if (p.y < groundY + 0.12f) p.y = groundY + 0.12f;
            display[i] = p;
        }

        PushDisplay();
    }

    void PushDisplay()
    {
        EnsureLine();
        if (lineRenderer == null) return;
        int n = display.Count;
        lineRenderer.positionCount = n;
        for (int i = 0; i < n; i++)
            lineRenderer.SetPosition(i, display[i]);
        lineRenderer.enabled = visible && n >= 2;
    }

    static List<Vector3> Decimate(List<Vector3> src, float minSeg)
    {
        if (src.Count <= 2) return new List<Vector3>(src);
        float minSq = minSeg * minSeg;
        var dst = new List<Vector3>(src.Count) { src[0] };
        Vector3 last = src[0];
        for (int i = 1; i < src.Count - 1; i++)
        {
            if ((src[i] - last).sqrMagnitude >= minSq)
            {
                dst.Add(src[i]);
                last = src[i];
            }
        }
        dst.Add(src[src.Count - 1]);
        return dst;
    }

    static List<Vector3> CatmullRom(List<Vector3> pts, int samplesPerSeg)
    {
        if (pts.Count < 2) return new List<Vector3>(pts);
        if (pts.Count == 2 || samplesPerSeg <= 1)
            return new List<Vector3>(pts);

        samplesPerSeg = Mathf.Clamp(samplesPerSeg, 2, 16);
        var dst = new List<Vector3>(pts.Count * samplesPerSeg);

        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector3 p0 = pts[Mathf.Max(i - 1, 0)];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = pts[Mathf.Min(i + 2, pts.Count - 1)];

            for (int s = 0; s < samplesPerSeg; s++)
            {
                float t = s / (float)samplesPerSeg;
                dst.Add(CatmullPoint(p0, p1, p2, p3, t));
            }
        }
        dst.Add(pts[pts.Count - 1]);
        return dst;
    }

    static Vector3 CatmullPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    static List<Vector3> Chaikin(List<Vector3> src, int passes)
    {
        if (src.Count < 3 || passes <= 0) return new List<Vector3>(src);
        var cur = src;
        for (int p = 0; p < passes; p++)
        {
            var next = new List<Vector3>(cur.Count * 2);
            next.Add(cur[0]);
            for (int i = 0; i < cur.Count - 1; i++)
            {
                Vector3 a = cur[i];
                Vector3 b = cur[i + 1];
                next.Add(Vector3.Lerp(a, b, 0.25f));
                next.Add(Vector3.Lerp(a, b, 0.75f));
            }
            next.Add(cur[cur.Count - 1]);
            // Cap density
            cur = next.Count > 12000 ? Decimate(next, 0.35f) : next;
        }
        return cur;
    }

    public void OnSimulationFinished(bool successful)
    {
        finished = true;
        if (rocketPhysics != null)
        {
            Vector3 touch = rocketPhysics.state.position;
            touch.y = groundY + 0.2f;
            // Smooth descent to pad if last sample was still high
            if (hasLast && lastPoint.y > groundY + 2f)
            {
                int steps = Mathf.Clamp(Mathf.CeilToInt((lastPoint.y - groundY) / 5f), 4, 16);
                Vector3 from = lastPoint;
                for (int i = 1; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    t = t * t * (3f - 2f * t);
                    Vector3 p = Vector3.Lerp(from, touch, t);
                    raw.Add(p);
                }
                lastPoint = touch;
                hasLast = true;
            }
            else
                AddRaw(touch, force: true);
        }
        RebuildSmooth(null);
        ApplyColor(successful ? goodColor : badColor);
        if (lineRenderer != null)
            lineRenderer.enabled = visible;
    }

    public void Clear()
    {
        raw.Clear();
        display.Clear();
        hasLast = false;
        finished = false;
        EnsureLine();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = visible;
            ApplyColor(normalColor);
        }
    }

    public bool TryGetOverview(out Vector3 center, out float radius)
    {
        center = Vector3.zero;
        radius = 100f;
        if (raw.Count == 0 && rocketPhysics == null) return false;

        Vector3 min = Vector3.zero, max = Vector3.zero;
        bool any = false;
        void Enc(Vector3 p)
        {
            if (!any) { min = max = p; any = true; }
            else { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
        }
        Enc(Vector3.zero);
        if (rocketPhysics != null)
        {
            Enc(rocketPhysics.state.position);
            if (rocketPhysics.parameters != null)
                Enc(rocketPhysics.parameters.startPosition);
        }
        foreach (var p in raw) Enc(p);
        if (!any) return false;
        center = (min + max) * 0.5f;
        radius = Mathf.Min(1500f, Mathf.Max(80f, (max - min).magnitude * 0.5f, max.y * 0.45f + 50f));
        return true;
    }

    void ApplyColor(Color c)
    {
        if (lineRenderer == null) return;
        lineRenderer.startColor = c;
        lineRenderer.endColor = new Color(c.r, c.g, c.b, 0.95f);
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(c, 0f),
                new GradientColorKey(Color.Lerp(c, Color.white, 0.12f), 0.5f),
                new GradientColorKey(c, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.65f, 0f),
                new GradientAlphaKey(1f, 0.25f),
                new GradientAlphaKey(0.92f, 1f)
            });
        lineRenderer.colorGradient = g;
        if (lineRenderer.sharedMaterial != null)
        {
            var mat = lineRenderer.sharedMaterial;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            mat.color = c;
        }
    }
}
