using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Візуалізація траєкторії посадки.
/// Семпл у FixedUpdate → Catmull-Rom spline + Chaikin → плавна крива без «зубів».
/// Rebuild кожен кадр під час польоту; після touchdown лінія лишається до Clear().
/// </summary>
public class TrajectoryVisualizer : MonoBehaviour
{
    public RocketPhysics rocketPhysics;
    public LineRenderer lineRenderer;
    public int maxRawPoints = 4000;
    public float baseLineWidth = 5.5f;
    public float lineWidth
    {
        get => baseLineWidth;
        set => baseLineWidth = value;
    }
    public float minPointDistance = 2.2f;
    public float minWidth = 3.2f;
    public float maxWidth = 48f;
    public float widthScalePerMeter = 0.011f;
    public float groundY = 0.7f;
    public int smoothIterations = 2;
    public int splineSamplesPerSeg = 6;

    public Color goodColor = new(0.35f, 0.95f, 0.65f, 1f);
    public Color badColor = new(1f, 0.4f, 0.42f, 1f);
    public Color normalColor = new(0.45f, 0.85f, 1f, 1f);

    readonly List<Vector3> raw = new();
    readonly List<Vector3> display = new();
    Vector3 lastPoint;
    Vector3 smoothPos;
    bool hasLast;
    bool finished;
    bool visible = true;
    bool displayDirty = true;
    int lastPushedCount = -1;

    public int PointCount => raw.Count;
    public IReadOnlyList<Vector3> Points => raw;
    public bool IsVisible => visible;

    void Start()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        if (rocketPhysics == null)
            rocketPhysics = FindFirstObjectByType<RocketPhysics>();

        ConfigureLine();
        ApplyVisibility();
    }

    void ConfigureLine()
    {
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        lineRenderer.numCapVertices = 12;
        lineRenderer.numCornerVertices = 12;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.allowOcclusionWhenDynamic = false;
        lineRenderer.sortingOrder = 80;
        lineRenderer.widthMultiplier = 1f;
        lineRenderer.generateLightingData = false;

        var shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color");
        if (shader != null)
        {
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = 3100;
            lineRenderer.material = mat;
        }

        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.startColor = normalColor;
        lineRenderer.endColor = normalColor;
        ApplyColor(normalColor);
        ApplyDistanceWidth();
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

    void FixedUpdate()
    {
        if (finished) return;
        if (rocketPhysics == null)
        {
            rocketPhysics = FindFirstObjectByType<RocketPhysics>();
            if (rocketPhysics == null) return;
        }
        if (!rocketPhysics.simulationArmed) return;
        if (rocketPhysics.state.simulationFinished || rocketPhysics.state.isLanded) return;

        AddRaw(rocketPhysics.state.position, force: false);
    }

    void LateUpdate()
    {
        if (rocketPhysics == null)
            rocketPhysics = FindFirstObjectByType<RocketPhysics>();

        // Під час польоту — live tip + повний rebuild кожен кадр (плавне подовження)
        if (!finished && rocketPhysics != null && rocketPhysics.simulationArmed
            && !rocketPhysics.state.simulationFinished && !rocketPhysics.state.isLanded)
        {
            Vector3 tip = rocketPhysics.state.position;
            if (tip.y < groundY) tip.y = groundY;
            if (hasLast)
                smoothPos = Vector3.Lerp(smoothPos, tip, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            else
                smoothPos = tip;

            // Додаємо tip у display без очікування minDist (візуальний «хвіст»)
            if (raw.Count >= 1)
            {
                RebuildDisplay(liveTip: smoothPos);
            }
        }
        else if (displayDirty && raw.Count >= 2)
        {
            RebuildDisplay(liveTip: null);
            displayDirty = false;
        }

        if (visible)
        {
            ApplyDistanceWidth();
            if (finished && lineRenderer != null)
            {
                if (!lineRenderer.enabled && visible)
                    lineRenderer.enabled = true;
                if (lineRenderer.positionCount == 0 && display.Count > 0)
                    PushDisplayToRenderer();
            }
        }
    }

    void ApplyDistanceWidth()
    {
        if (lineRenderer == null || !visible) return;

        float dist = 200f;
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 refPoint = display.Count > 0
                ? display[display.Count / 2]
                : (rocketPhysics != null ? rocketPhysics.state.position : Vector3.zero);
            dist = Vector3.Distance(cam.transform.position, refPoint);
        }

        float w = Mathf.Clamp(baseLineWidth + dist * widthScalePerMeter, minWidth, maxWidth);
        lineRenderer.startWidth = w;
        lineRenderer.endWidth = w * 0.72f;
    }

    void AddRaw(Vector3 p, bool force)
    {
        if (p.y < groundY) p.y = groundY;

        float minDist = minPointDistance;
        if (p.y < 200f) minDist = 1.4f;
        if (p.y < 60f) minDist = 0.7f;
        if (p.y < 15f) minDist = 0.35f;

        if (!force && hasLast && (p - lastPoint).sqrMagnitude < minDist * minDist)
            return;

        // Легке EMA — менше шуму RK4, без «стрибків»
        if (hasLast && !force)
            p = Vector3.Lerp(lastPoint, p, 0.72f);

        if (raw.Count >= maxRawPoints)
            raw.RemoveAt(0);

        raw.Add(p);
        lastPoint = p;
        smoothPos = p;
        hasLast = true;
        displayDirty = true;
    }

    void RebuildDisplay(Vector3? liveTip)
    {
        if (raw.Count < 2 && liveTip == null)
        {
            display.Clear();
            display.AddRange(raw);
            PushDisplayToRenderer();
            return;
        }

        // 1) Sparse control points
        var sparse = Decimate(raw, 0.9f);

        // 2) Live tip as final control
        if (liveTip.HasValue)
        {
            Vector3 tip = liveTip.Value;
            if (sparse.Count == 0 || (sparse[sparse.Count - 1] - tip).sqrMagnitude > 0.04f)
                sparse.Add(tip);
            else
                sparse[sparse.Count - 1] = tip;
        }

        // 3) Catmull-Rom densify → C1 curve
        var spline = CatmullRom(sparse, splineSamplesPerSeg);

        // 4) Light Chaikin polish
        display.Clear();
        display.AddRange(Chaikin(spline, smoothIterations));

        for (int i = 0; i < display.Count; i++)
        {
            var p = display[i];
            if (p.y < groundY + 0.15f) p.y = groundY + 0.15f;
            display[i] = p;
        }
        PushDisplayToRenderer();
    }

    void PushDisplayToRenderer()
    {
        if (lineRenderer == null) return;
        int n = display.Count;
        if (n != lastPushedCount)
        {
            lineRenderer.positionCount = n;
            lastPushedCount = n;
        }
        for (int i = 0; i < n; i++)
            lineRenderer.SetPosition(i, display[i]);
        lineRenderer.enabled = visible;
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

            int steps = (i == pts.Count - 2) ? samplesPerSeg : samplesPerSeg;
            for (int s = 0; s < steps; s++)
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
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    static List<Vector3> Chaikin(List<Vector3> pts, int iterations)
    {
        if (pts.Count < 3 || iterations <= 0) return new List<Vector3>(pts);
        var cur = pts;
        for (int it = 0; it < iterations; it++)
        {
            var next = new List<Vector3>(cur.Count * 2);
            next.Add(cur[0]);
            for (int i = 0; i < cur.Count - 1; i++)
            {
                Vector3 p0 = cur[i];
                Vector3 p1 = cur[i + 1];
                next.Add(Vector3.Lerp(p0, p1, 0.25f));
                next.Add(Vector3.Lerp(p0, p1, 0.75f));
            }
            next.Add(cur[cur.Count - 1]);
            if (next.Count > 10000)
                cur = Decimate(next, 0.6f);
            else
                cur = next;
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
            if (hasLast && lastPoint.y > groundY + 1.5f)
            {
                int steps = Mathf.Clamp(Mathf.CeilToInt((lastPoint.y - groundY) / 6f), 3, 12);
                Vector3 from = lastPoint;
                for (int i = 1; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    t = t * t * (3f - 2f * t);
                    Vector3 p = Vector3.Lerp(from, touch, t);
                    p.y = Mathf.Lerp(from.y, touch.y, t);
                    raw.Add(p);
                }
            }
            else
            {
                raw.Add(touch);
            }
            lastPoint = touch;
            smoothPos = touch;
            hasLast = true;
        }

        RebuildDisplay(liveTip: null);
        displayDirty = false;
        ApplyColor(successful ? goodColor : badColor);
        ApplyDistanceWidth();
        if (lineRenderer != null)
        {
            lineRenderer.enabled = visible;
            if (lineRenderer.positionCount == 0 && display.Count > 0)
                PushDisplayToRenderer();
        }
    }

    public void Clear()
    {
        raw.Clear();
        display.Clear();
        hasLast = false;
        finished = false;
        displayDirty = true;
        lastPushedCount = -1;
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            ApplyColor(normalColor);
        }
    }

    public bool TryGetOverview(out Vector3 center, out float radius)
    {
        center = Vector3.zero;
        radius = 100f;

        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;
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
        lineRenderer.endColor = new Color(c.r, c.g, c.b, 0.92f);
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(c, 0f),
                new GradientColorKey(Color.Lerp(c, Color.white, 0.15f), 0.55f),
                new GradientColorKey(c, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.55f, 0f),
                new GradientAlphaKey(0.95f, 0.35f),
                new GradientAlphaKey(0.9f, 1f)
            });
        lineRenderer.colorGradient = g;

        if (lineRenderer.material != null)
        {
            if (lineRenderer.material.HasProperty("_BaseColor"))
                lineRenderer.material.SetColor("_BaseColor", c);
            if (lineRenderer.material.HasProperty("_Color"))
                lineRenderer.material.SetColor("_Color", c);
            lineRenderer.material.color = c;
        }
    }
}
