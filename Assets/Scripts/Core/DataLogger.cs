using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class DataLogger : MonoBehaviour
{
    private List<string> data = new List<string>();
    private string filePath;

    public void Initialize()
    {
        filePath = Path.Combine(Application.dataPath, "..", "SimulationLogs",
            $"Landing_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");

        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        data.Add("time,posY,velY,thrust,mass,angleError");
    }

    public void Log(RocketState state)
    {
        float angleError = Vector3.Angle(state.rotation * Vector3.up, Vector3.up);

        string line = $"{state.time:F3},{state.position.y:F2},{state.velocity.y:F2}," +
                      $"{state.currentThrust:F1},{state.TotalMass:F2},{angleError:F2}";
        data.Add(line);
    }

    public void Save()
    {
        File.WriteAllLines(filePath, data);
        Debug.Log($"Лог траєкторії збережено!");
    }
}