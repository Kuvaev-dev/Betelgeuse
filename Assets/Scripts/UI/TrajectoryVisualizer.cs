using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Візуалізація траєкторії посадки.
/// Семпл у FixedUpdate → decimate → Chaikin corner-cutting → рівна крива без «зубів».
/// Після touchdown лінія лишається видимою до Clear() (лише новий старт / PrepareMode).
/// </summary>
public class TrajectoryVisualizer : MonoBehaviour
{
    public RocketPhysics rocketPhysics;
    public LineRenderer lineRenderer;
    public int maxRawPoints = 2500;
    public float baseLineWidth = 5.5f;
    public float lineWidth
    {
        get => baseLineWidth;
        set => baseLineWidth = value;
    }
    public float minPointDistance = 6f;
    public float minWidth = 3.5f;
    public float maxWidth = 48f;
    public float widthScalePerMeter = 0.011f;
    public float groundY = 0.7f;
    public int smoothIterations = 3;

    public Color goodColor = new(0.35f, 0.95f, 0.65f, 1f);
    public Color badColor = new(1f, 0.4f, 0.42f, 1f);
    public Color normalColor = new(0.45f, 0.85f, 1f, 1f);

    readonly List<Vector3> raw = new();
    readonly List<Vector3> display = new();
    Vector3 lastPoint;
    bool hasLast;
    bool finished;
    bool visible = true;
    float rebuildTimer;

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
        lineRenderer.numCapVertices = 8;
        lineRenderer.numCornerVertices = 8;
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
        // Vertex colors drive the look (Sprites/Default)
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

        // Періодичний rebuild згладженої кривої під час польоту
        if (!finished && raw.Count >= 2)
        {
            rebuildTimer += Time.unscaledDeltaTime;
            if (rebuildTimer >= 0.08f)
            {
                rebuildTimer = 0f;
                RebuildDisplay();
            }
        }

        if (visible)
        {
            ApplyDistanceWidth();
            // Гарантія: лінія не «зникає» після посадки
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
        lineRenderer.endWidth = w * 0.75f;
    }

    void AddRaw(Vector3 p, bool force)
    {
        if (p.y < groundY) p.y = groundY;

        float minDist = minPointDistance;
        if (p.y < 200f) minDist = 3.5f;
        if (p.y < 60f) minDist = 1.8f;
        if (p.y < 15f) minDist = 0.8f;

        if (!force && hasLast && (p - lastPoint).sqrMagnitude < minDist * minDist)
            return;

        // Легке згладжування позиції (EMA) — прибирає «зуби» RK4/шуму
        if (hasLast && !force)
            p = Vector3.Lerp(lastPoint, p, 0.55f);

        if (raw.Count >= maxRawPoints)
            raw.RemoveAt(0);

        raw.Add(p);
        lastPoint = p;
        hasLast = true;
    }

    void RebuildDisplay()
    {
        if (raw.Count < 2)
        {
            display.Clear();
            display.AddRange(raw);
            PushDisplayToRenderer();
            return;
        }

        // 1) Decimate for smoothness (keep endpoints)
        var sparse = Decimate(raw, 1.5f);
        // 2) Chaikin corner-cutting → C1-like curve
        display.Clear();
        display.AddRange(Chaikin(sparse, smoothIterations));
        // 3) Lift slightly above terrain to avoid z-fight
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
        lineRenderer.positionCount = n;
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

    static List<Vector3> Chaikin(List<Vector3> pts, int iterations)
    {
        if (pts.Count < 3 || iterations <= 0) return new List<Vector3>(pts);
        var cur = pts;
        for (int it = 0; it < iterations; it++)
        {
            var next = new List<Vector3>(cur.Count * 2);
            next.Add(cur[0]); // keep start
            for (int i = 0; i < cur.Count - 1; i++)
            {
                Vector3 p0 = cur[i];
                Vector3 p1 = cur[i + 1];
                next.Add(Vector3.Lerp(p0, p1, 0.25f));
                next.Add(Vector3.Lerp(p0, p1, 0.75f));
            }
            next.Add(cur[cur.Count - 1]); // keep end
            // Cap growth
            if (next.Count > 8000)
            {
                // thin again
                cur = Decimate(next, 0.8f);
            }
            else cur = next;
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
            // Дотягти до pad плавно
            if (hasLast && lastPoint.y > groundY + 1.5f)
            {
                int steps = Mathf.Clamp(Mathf.CeilToInt((lastPoint.y - groundY) / 8f), 2, 8);
                Vector3 from = lastPoint;
                for (int i = 1; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    // smoothstep
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
            hasLast = true;
        }

        RebuildDisplay();
        ApplyColor(successful ? goodColor : badColor);
        ApplyDistanceWidth();
        // Жорстко показати
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
        rebuildTimer = 0f;
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
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 1f) });
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
