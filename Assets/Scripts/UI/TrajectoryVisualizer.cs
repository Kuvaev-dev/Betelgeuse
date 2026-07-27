using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Траєкторія польоту (LineRenderer) з кольоровим статусом посадки.
/// </summary>
public class TrajectoryVisualizer : MonoBehaviour
{
    [Header("Посилання")]
    public RocketPhysics rocketPhysics;

    [Header("Лінія")]
    public LineRenderer lineRenderer;
    public int maxPoints = 1200;
    public float lineWidth = 4f;
    public float minPointDistance = 5f;

    [Header("Кольори Mission Control")]
    public Color goodColor = new(0.24f, 1f, 0.60f, 0.95f);
    public Color badColor = new(1f, 0.30f, 0.42f, 0.95f);
    public Color normalColor = new(0.24f, 0.88f, 1f, 0.85f);

    readonly List<Vector3> points = new();
    Vector3 lastPoint;
    bool hasLast;

    void Start()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        if (rocketPhysics == null)
            rocketPhysics = FindFirstObjectByType<RocketPhysics>();

        ConfigureLine();
        Clear();
    }

    void ConfigureLine()
    {
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth * 0.35f;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        lineRenderer.numCapVertices = 2;
        lineRenderer.numCornerVertices = 2;
        if (lineRenderer.sharedMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
                lineRenderer.material = new Material(shader);
        }
        lineRenderer.startColor = normalColor;
        lineRenderer.endColor = normalColor;
    }

    void FixedUpdate()
    {
        if (rocketPhysics == null || rocketPhysics.state.simulationFinished) return;
        if (points.Count >= maxPoints) return;

        Vector3 p = rocketPhysics.state.position;
        if (hasLast && (p - lastPoint).sqrMagnitude < minPointDistance * minPointDistance)
            return;

        points.Add(p);
        lastPoint = p;
        hasLast = true;
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPosition(points.Count - 1, p);
    }

    public void OnSimulationFinished(bool successful)
    {
        Color c = successful ? goodColor : badColor;
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;
    }

    public void Clear()
    {
        points.Clear();
        hasLast = false;
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.startColor = normalColor;
            lineRenderer.endColor = normalColor;
        }
    }
}
