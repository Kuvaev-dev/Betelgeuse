using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

/// <summary>
/// Покроковий лог симуляції (~20 Гц) для експорту та аналізу.
/// Колонки: кінематика, маса, тяга, T/W, gimbal, кути, критерії.
/// </summary>
public class DataLogger : MonoBehaviour
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    readonly List<string> data = new();
    readonly List<Sample> samples = new();
    string filePath;
    int logStride;
    int sampleCounter;
    RocketPhysics rocket;

    public string LastFilePath => filePath;
    public IReadOnlyList<string> Rows => data;
    public IReadOnlyList<Sample> Samples => samples;
    public int SampleCount => samples.Count;

    public struct Sample
    {
        public float time;
        public float posX, posY, posZ;
        public float velX, velY, velZ;
        public float speed, horizSpeed;
        public float thrust, thrustKn, mass, fuel, twr;
        public float tiltDeg, pitchRate, yawRate;
        public float gimbalX, gimbalZ;
        public float miss;
        public string controlMode;
    }

    public void Initialize()
    {
        rocket = GetComponent<RocketPhysics>();
        filePath = Path.Combine(ResearchExporter.LogsDirectory,
            $"Landing_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");
        data.Clear();
        samples.Clear();
        data.Add(
            "step,time_s,posX_m,posY_m,posZ_m,velX_mps,velY_mps,velZ_mps," +
            "speed_mps,horizSpeed_mps,thrust_N,thrust_kN,mass_kg,fuel_kg,twr," +
            "tilt_deg,pitchRate_dps,yawRate_dps,gimbalX_deg,gimbalZ_deg,miss_m,controlMode");
        sampleCounter = 0;
        logStride = 10; // ~20 Hz @ dt=0.005
    }

    public void Log(RocketState state)
    {
        if (state == null) return;
        sampleCounter++;
        if (sampleCounter % logStride != 0 && state.position.y > 1f) return;

        float tilt = Vector3.Angle(state.rotation * Vector3.up, Vector3.up);
        float miss = new Vector2(state.position.x, state.position.z).magnitude;
        float hSpd = new Vector2(state.velocity.x, state.velocity.z).magnitude;
        float spd = state.velocity.magnitude;
        float g = AtmosphereModel.GetGravity(Mathf.Max(0f, state.position.y));
        float twr = state.currentThrust / Mathf.Max(1f, state.TotalMass * g);

        // Gimbal: thrustDirection relative to body up
        Vector3 td = state.thrustDirection.normalized;
        float gimbX = Mathf.Atan2(-td.z, td.y) * Mathf.Rad2Deg;
        float gimbZ = Mathf.Atan2(td.x, td.y) * Mathf.Rad2Deg;
        float pRate = state.angularVelocity.x * Mathf.Rad2Deg;
        float yRate = state.angularVelocity.z * Mathf.Rad2Deg;

        string mode = rocket != null ? rocket.controlMode.ToString() : "Unknown";

        var s = new Sample
        {
            time = state.time,
            posX = state.position.x,
            posY = state.position.y,
            posZ = state.position.z,
            velX = state.velocity.x,
            velY = state.velocity.y,
            velZ = state.velocity.z,
            speed = spd,
            horizSpeed = hSpd,
            thrust = state.currentThrust,
            thrustKn = state.currentThrust / 1000f,
            mass = state.TotalMass,
            fuel = state.currentFuelMass,
            twr = twr,
            tiltDeg = tilt,
            pitchRate = pRate,
            yawRate = yRate,
            gimbalX = gimbX,
            gimbalZ = gimbZ,
            miss = miss,
            controlMode = mode
        };
        samples.Add(s);

        int step = samples.Count;
        data.Add(string.Format(Inv,
            "{0},{1:F4},{2:F3},{3:F3},{4:F3},{5:F3},{6:F3},{7:F3}," +
            "{8:F3},{9:F3},{10:F1},{11:F2},{12:F2},{13:F2},{14:F3}," +
            "{15:F3},{16:F3},{17:F3},{18:F3},{19:F3},{20:F3},{21}",
            step, s.time, s.posX, s.posY, s.posZ, s.velX, s.velY, s.velZ,
            s.speed, s.horizSpeed, s.thrust, s.thrustKn, s.mass, s.fuel, s.twr,
            s.tiltDeg, s.pitchRate, s.yawRate, s.gimbalX, s.gimbalZ, s.miss, s.controlMode));
    }

    public void Save()
    {
        if (string.IsNullOrEmpty(filePath) || data.Count <= 1) return;
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ResearchExporter.LogsDirectory);
        File.WriteAllLines(filePath, data, Encoding.UTF8);
        Debug.Log($"Лог траєкторії: {filePath}");
    }

    public List<string> CloneRows() => new(data);

    public List<Sample> CloneSamples() => new(samples);
}
