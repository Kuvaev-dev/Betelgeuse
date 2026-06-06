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

    [Tooltip("Максимальна кількість точок траєкторії")]
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
        if (rocketPhysics == null || rocketPhysics.state.simulationFinished)
            return;

        if (points.Count < maxPoints)
        {
            points.Add(rocketPhysics.state.position);
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
        }
    }

    /// <summary>
    /// Очищає поточну траєкторію (викликається перед новим тестом).
    /// </summary>
    public void Clear()
    {
        points.Clear();
        lineRenderer.positionCount = 0;
    }
}