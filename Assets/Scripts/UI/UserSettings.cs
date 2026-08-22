using UnityEngine;

/// <summary>
/// Local PlayerPrefs for HUD / experiment setup. Theme & language have their own keys.
/// </summary>
public static class UserSettings
{
    const string P = "Betelgeuse.Set.";

    public static float Wind
    {
        get => PlayerPrefs.GetFloat(P + "Wind", 14f);
        set => PlayerPrefs.SetFloat(P + "Wind", Mathf.Clamp(value, 0f, 25f));
    }

    public static int Tests
    {
        get => PlayerPrefs.GetInt(P + "Tests", 15);
        set => PlayerPrefs.SetInt(P + "Tests", Mathf.Clamp(value, 5, 40));
    }

    public static float TimeScale
    {
        get => PlayerPrefs.GetFloat(P + "TimeScale", 8f);
        set => PlayerPrefs.SetFloat(P + "TimeScale", Mathf.Clamp(value, 1f, 40f));
    }

    public static bool Noise
    {
        get => PlayerPrefs.GetInt(P + "Noise", 1) != 0;
        set => PlayerPrefs.SetInt(P + "Noise", value ? 1 : 0);
    }

    public static bool Train
    {
        // Default off: stable presentation; enable for ES research demos
        get => PlayerPrefs.GetInt(P + "Train", 0) != 0;
        set => PlayerPrefs.SetInt(P + "Train", value ? 1 : 0);
    }

    public static bool TrajectoryVisible
    {
        get => PlayerPrefs.GetInt(P + "Traj", 1) != 0;
        set => PlayerPrefs.SetInt(P + "Traj", value ? 1 : 0);
    }

    public static bool PanelsHidden
    {
        get => PlayerPrefs.GetInt(P + "HideUI", 0) != 0;
        set => PlayerPrefs.SetInt(P + "HideUI", value ? 1 : 0);
    }

    /// <summary>0=PID … 3=Hybrid</summary>
    public static int ControlMode
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(P + "Mode", 3), 0, 3);
        set => PlayerPrefs.SetInt(P + "Mode", Mathf.Clamp(value, 0, 3));
    }

    public static void Save() => PlayerPrefs.Save();
}
