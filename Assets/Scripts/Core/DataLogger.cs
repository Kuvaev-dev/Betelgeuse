using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Логер даних симуляції посадки.
/// Записує траєкторію, швидкість, тягу та інші параметри у CSV-файл для подальшого аналізу.
/// </summary>
public class DataLogger : MonoBehaviour
{
    private List<string> data = new List<string>();
    private string filePath;

    /// <summary>
    /// Ініціалізує логер: створює папку та заголовок CSV-файлу.
    /// </summary>
    public void Initialize()
    {
        filePath = Path.Combine(Application.dataPath, "..", "SimulationLogs",
            $"Landing_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        data.Clear();
        data.Add("time,posY,velY,thrust,mass,angleError");
    }

    /// <summary>
    /// Записує поточний стан ракети у буфер.
    /// </summary>
    public void Log(RocketState state)
    {
        float angleError = Vector3.Angle(state.rotation * Vector3.up, Vector3.up);
        string line = $"{state.time:F3},{state.position.y:F2},{state.velocity.y:F2}," +
                      $"{state.currentThrust:F1},{state.TotalMass:F2},{angleError:F2}";
        data.Add(line);
    }

    /// <summary>
    /// Зберігає всі накопичені дані у CSV-файл на диск.
    /// </summary>
    public void Save()
    {
        File.WriteAllLines(filePath, data);
        Debug.Log($"Лог траєкторії збережено!");
    }
}
