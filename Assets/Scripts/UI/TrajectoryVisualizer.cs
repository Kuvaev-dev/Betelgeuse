using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Візуалізація траєкторії польоту (LineRenderer).
/// Товщина масштабується відстанню до камери. Можна увімкнути/вимкнути.
/// </summary>
public class TrajectoryVisualizer : MonoBehaviour
{
    public RocketPhysics rocketPhysics;
    public LineRenderer lineRenderer;
    public int maxPoints = 4000;
    public float baseLineWidth = 6f;
    /// <summary>Alias для сумісності зі старим API / сценами.</summary>
    public float lineWidth
    {
        get => baseLineWidth;
        set => baseLineWidth = value;
    }
    public float minPointDistance = 2.5f;
    public float minWidth = 4f;
    public float maxWidth = 55f;
    public float widthScalePerMeter = 0.012f;

    // Cyan mission / success green / fail coral
    public Color goodColor = new(0.35f, 0.95f, 0.65f, 1f);
    public Color badColor = new(1f, 0.4f, 0.42f, 1f);
    public Color normalColor = new(0.45f, 0.85f, 1f, 1f);

    readonly List<Vector3> points = new();
    Vector3 lastPoint;
    bool hasLast;
    bool finished;
    bool visible = true;
    Color currentColor;

    public int PointCount => points.Count;
    public IReadOnlyList<Vector3> Points => points;
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
        currentColor = normalColor;
        ApplyVisibility();
    }

    void ConfigureLine()
    {
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        lineRenderer.numCapVertices = 6;
        lineRenderer.numCornerVertices = 6;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.allowOcclusionWhenDynamic = false;
        lineRenderer.sortingOrder = 50;
        lineRenderer.widthMultiplier = 1f;

        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Sprites/Default");
        if (shader != null)
        {
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", normalColor * 1.2f);
            }
            lineRenderer.material = mat;
        }

        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;
        ApplyColor(normalColor);
        ApplyDistanceWidth();
    }

    /// <summary>Увімкнути / вимкнути відображення лінії траєкторії.</summary>
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

    void LateUpdate()
    {
        if (rocketPhysics == null)
        {
            rocketPhysics = FindFirstObjectByType<RocketPhysics>();
            if (rocketPhysics == null) return;
        }

        if (rocketPhysics.simulationArmed && !finished)
        {
            if (rocketPhysics.state.simulationFinished)
                AddPoint(rocketPhysics.state.position, force: true);
            else
                AddPoint(rocketPhysics.state.position, force: false);
        }

        if (visible)
            ApplyDistanceWidth();
    }

    void ApplyDistanceWidth()
    {
        if (lineRenderer == null || !visible) return;

        float dist = 200f;
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 refPoint = points.Count > 0
                ? points[points.Count / 2]
                : (rocketPhysics != null ? rocketPhysics.state.position : Vector3.zero);
            dist = Vector3.Distance(cam.transform.position, refPoint);
        }

        float w = Mathf.Clamp(baseLineWidth + dist * widthScalePerMeter, minWidth, maxWidth);
        lineRenderer.startWidth = w;
        lineRenderer.endWidth = w * 0.65f;
    }

    void AddPoint(Vector3 p, bool force)
    {
        if (lineRenderer == null) return;
        if (points.Count >= maxPoints) return;
        if (!force && hasLast && (p - lastPoint).sqrMagnitude < minPointDistance * minPointDistance)
            return;

        if (p.y < 0.5f) p.y = 0.5f;

        points.Add(p);
        lastPoint = p;
        hasLast = true;
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPosition(points.Count - 1, p);
    }

    public void OnSimulationFinished(bool successful)
    {
        finished = true;
        if (rocketPhysics != null)
            AddPoint(rocketPhysics.state.position, force: true);
        ApplyColor(successful ? goodColor : badColor);
        ApplyDistanceWidth();
    }

    public void Clear()
    {
        points.Clear();
        hasLast = false;
        finished = false;
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
        if (points.Count > 0)
            foreach (var p in points) Enc(p);

        if (!any) return false;
        center = (min + max) * 0.5f;
        radius = Mathf.Min(1500f, Mathf.Max(80f, (max - min).magnitude * 0.5f, max.y * 0.45f + 50f));
        return true;
    }

    void ApplyColor(Color c)
    {
        currentColor = c;
        if (lineRenderer == null) return;
        lineRenderer.startColor = c;
        lineRenderer.endColor = new Color(c.r, c.g, c.b, 0.85f);
        if (lineRenderer.material != null)
        {
            if (lineRenderer.material.HasProperty("_BaseColor"))
                lineRenderer.material.SetColor("_BaseColor", c);
            if (lineRenderer.material.HasProperty("_Color"))
                lineRenderer.material.SetColor("_Color", c);
            if (lineRenderer.material.HasProperty("_EmissionColor"))
            {
                lineRenderer.material.EnableKeyword("_EMISSION");
                lineRenderer.material.SetColor("_EmissionColor", c * 1.3f);
            }
            lineRenderer.material.color = c;
        }
    }
}
