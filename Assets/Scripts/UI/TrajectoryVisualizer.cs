using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Візуалізатор траєкторії польоту ракети за допомогою LineRenderer.
/// Автоматично малює лінію шляху ракети під час симуляції.
/// </summary>
public class TrajectoryVisualizer : MonoBehaviour
{
    public RocketPhysics rocketPhysics;
    public LineRenderer lineRenderer;
    public int maxPoints = 500;

    private List<Vector3> points = new List<Vector3>();

    void Start()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = 0;
    }

    void FixedUpdate()
    {
        if (rocketPhysics == null || rocketPhysics.state.simulationFinished) return;

        if (points.Count < maxPoints)
        {
            Vector3 currentPos = rocketPhysics.state.position;
            points.Add(currentPos);
            lineRenderer.positionCount = points.Count;

            lineRenderer.SetPosition(points.Count - 1, currentPos);
        }
    }

    public void Clear()
    {
        points.Clear();
        lineRenderer.positionCount = 0;
    }
}
