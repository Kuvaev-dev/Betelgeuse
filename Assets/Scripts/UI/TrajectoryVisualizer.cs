using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Повна траєкторія польоту (LineRenderer). Не стирається після посадки —
/// лише при новому старті. Підтримує огляд усього шляху (bounds).
/// </summary>
public class TrajectoryVisualizer : MonoBehaviour
{
    public RocketPhysics rocketPhysics;
    public LineRenderer lineRenderer;
    public int maxPoints = 4000;
    public float lineWidth = 4f;
    public float minPointDistance = 2f;

    public Color goodColor = new(0.35f, 0.95f, 0.55f, 0.95f);
    public Color badColor = new(1f, 0.35f, 0.4f, 0.95f);
    public Color normalColor = new(0.45f, 0.9f, 1f, 0.95f);

    readonly List<Vector3> points = new();
    Vector3 lastPoint;
    bool hasLast;
    bool finished;

    public int PointCount => points.Count;
    public IReadOnlyList<Vector3> Points => points;

    void Start()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        if (rocketPhysics == null)
            rocketPhysics = FindFirstObjectByType<RocketPhysics>();

        ConfigureLine();
    }

    void ConfigureLine()
    {
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth * 0.55f;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.sortingOrder = 10;

        var shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Particles/Standard Unlit");
        if (shader != null)
            lineRenderer.material = new Material(shader) { color = Color.white };

        ApplyColor(normalColor);
    }

    void LateUpdate()
    {
        if (rocketPhysics == null)
        {
            rocketPhysics = FindFirstObjectByType<RocketPhysics>();
            if (rocketPhysics == null) return;
        }

        // Record while armed and not finished
        if (!rocketPhysics.simulationArmed || finished) return;
        if (rocketPhysics.state.simulationFinished)
        {
            // Ensure last point (touchdown) is on the line
            AddPoint(rocketPhysics.state.position, force: true);
            return;
        }

        AddPoint(rocketPhysics.state.position, force: false);
    }

    void AddPoint(Vector3 p, bool force)
    {
        if (points.Count >= maxPoints) return;
        if (!force && hasLast && (p - lastPoint).sqrMagnitude < minPointDistance * minPointDistance)
            return;

        // Lift slightly so line is not z-fighting with ground
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

    /// <summary>Центр і радіус усієї траєкторії (+ pad) для оглядової камери.</summary>
    public bool TryGetOverview(out Vector3 center, out float radius)
    {
        center = Vector3.zero;
        radius = 100f;
        if (points.Count < 2)
        {
            // fallback: rocket to pad
            if (rocketPhysics != null)
            {
                Vector3 a = rocketPhysics.state.position;
                center = (a + Vector3.zero) * 0.5f;
                radius = Mathf.Max(80f, a.magnitude * 0.6f + a.y * 0.5f);
                return true;
            }
            return false;
        }

        Vector3 min = points[0];
        Vector3 max = points[0];
        foreach (var p in points)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        min = Vector3.Min(min, Vector3.zero);
        max = Vector3.Max(max, Vector3.zero);
        center = (min + max) * 0.5f;
        radius = Mathf.Max(60f, (max - min).magnitude * 0.55f);
        return true;
    }

    void ApplyColor(Color c)
    {
        if (lineRenderer == null) return;
        lineRenderer.startColor = c;
        lineRenderer.endColor = new Color(c.r, c.g, c.b, 0.55f);
        if (lineRenderer.material != null)
            lineRenderer.material.color = c;
    }
}
