using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// CSV-лог траєкторії посадки для аналізу в Excel/Python.
/// </summary>
public class DataLogger : MonoBehaviour
{
    List<string> data = new();
    string filePath;
    int logStride;
    int sampleCounter;

    public void Initialize()
    {
        filePath = Path.Combine(Application.dataPath, "..", "SimulationLogs",
            $"Landing_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        data.Clear();
        data.Add("time,posX,posY,posZ,velX,velY,velZ,thrust,mass,angleError,fuel");
        sampleCounter = 0;
        // ~20 Hz при dt=0.005 → кожен 10-й крок
        logStride = 10;
    }

    public void Log(RocketState state)
    {
        sampleCounter++;
        if (sampleCounter % logStride != 0 && state.position.y > 1f) return;

        float angleError = Vector3.Angle(state.rotation * Vector3.up, Vector3.up);
        data.Add(
            $"{state.time:F3},{state.position.x:F2},{state.position.y:F2},{state.position.z:F2}," +
            $"{state.velocity.x:F2},{state.velocity.y:F2},{state.velocity.z:F2}," +
            $"{state.currentThrust:F1},{state.TotalMass:F2},{angleError:F2},{state.currentFuelMass:F1}");
    }

    public void Save()
    {
        if (string.IsNullOrEmpty(filePath) || data.Count <= 1) return;
        File.WriteAllLines(filePath, data);
        Debug.Log($"Лог траєкторії: {filePath}");
    }
}
