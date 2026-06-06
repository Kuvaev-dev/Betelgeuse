using UnityEngine;
using System.Collections.Generic;

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
            points.Add(rocketPhysics.state.position);
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
        }
    }

    public void Clear()
    {
        points.Clear();
        lineRenderer.positionCount = 0;
    }
}