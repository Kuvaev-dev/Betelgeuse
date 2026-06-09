using UnityEngine;
using System.Collections.Generic;

public class TrajectoryVisualizer : MonoBehaviour
{
    [Header("Посилання")]
    public RocketPhysics rocketPhysics;

    [Header("Налаштування лінії")]
    public LineRenderer lineRenderer;
    public int maxPoints = 600;
    public float lineWidth = 0.7f;

    [Header("Кольори")]
    public Color goodColor = Color.green;
    public Color badColor = Color.red;
    public Color normalColor = Color.cyan;

    private List<Vector3> points = new List<Vector3>();
    private bool isGoodLanding = false;

    void Start()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
    }

    void FixedUpdate()
    {
        if (rocketPhysics == null || rocketPhysics.state.simulationFinished) return;

        if (points.Count < maxPoints)
        {
            points.Add(rocketPhysics.state.position);
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
        }
    }

    public void OnSimulationFinished(bool successful)
    {
        isGoodLanding = successful;
        Color color = successful ? goodColor : badColor;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    public void Clear()
    {
        points.Clear();
        lineRenderer.positionCount = 0;
        lineRenderer.startColor = normalColor;
        lineRenderer.endColor = normalColor;
    }
}