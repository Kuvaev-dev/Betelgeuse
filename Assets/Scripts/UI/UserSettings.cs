using UnityEngine;

/// <summary>
/// Local PlayerPrefs for HUD / experiment setup. Theme & language have their own keys.
/// </summary>
public static class UserSettings
{
    const string P = "Betelgeuse.Set.";

    public static float Wind
    {
        get => PlayerPrefs.GetFloat(P + "Wind", 10f);
        set => PlayerPrefs.SetFloat(P + "Wind", Mathf.Clamp(value, 0f, 25f));
    }

    public static int Tests
    {
        get => PlayerPrefs.GetInt(P + "Tests", 15);
        set => PlayerPrefs.SetInt(P + "Tests", Mathf.Clamp(value, 5, 40));
    }

    /// <summary>Monte-Carlo burst multiplier (Compare).</summary>
    public static float TimeScale
    {
        get => PlayerPrefs.GetFloat(P + "TimeScale", 12f);
        set => PlayerPrefs.SetFloat(P + "TimeScale", Mathf.Clamp(value, 1f, 40f));
    }

    /// <summary>Live flight Time.timeScale (single Start).</summary>
    public static float LiveTimeScale
    {
        get => PlayerPrefs.GetFloat(P + "LiveTS", 1f);
        set => PlayerPrefs.SetFloat(P + "LiveTS", Mathf.Clamp(value, 0.25f, 8f));
    }

    public static int ExperimentSeed
    {
        get => PlayerPrefs.GetInt(P + "Seed", 42);
        set => PlayerPrefs.SetInt(P + "Seed", value);
    }

    /// <summary>Initial altitude h₀, m (single flight + Monte-Carlo).</summary>
    public static float StartHeight
    {
        get => PlayerPrefs.GetFloat(P + "H0", 1800f);
        set => PlayerPrefs.SetFloat(P + "H0", Mathf.Clamp(value, 800f, 3000f));
    }

    /// <summary>Initial descent speed magnitude |Vy|, m/s (applied as −Vy).</summary>
    public static float StartDescentSpeed
    {
        get => PlayerPrefs.GetFloat(P + "Vy0", 72f);
        set => PlayerPrefs.SetFloat(P + "Vy0", Mathf.Clamp(value, 30f, 120f));
    }

    /// <summary>Initial tilt about Z, degrees.</summary>
    public static float StartTilt
    {
        get => PlayerPrefs.GetFloat(P + "Tilt0", 3.5f);
        set => PlayerPrefs.SetFloat(P + "Tilt0", Mathf.Clamp(value, 0f, 12f));
    }

    /// <summary>Monte-Carlo mass noise ±%.</summary>
    public static float MassNoise
    {
        get => PlayerPrefs.GetFloat(P + "MassN", 8f);
        set => PlayerPrefs.SetFloat(P + "MassN", Mathf.Clamp(value, 0f, 15f));
    }

    /// <summary>Monte-Carlo / single-run angle noise ±deg.</summary>
    public static float AngleNoise
    {
        get => PlayerPrefs.GetFloat(P + "AngN", 8f);
        set => PlayerPrefs.SetFloat(P + "AngN", Mathf.Clamp(value, 0f, 15f));
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

    /// <summary>Hybrid MLP residual on (false = Fuzzy-only ablation).</summary>
    public static bool HybridResidual
    {
        get => PlayerPrefs.GetInt(P + "HybRes", 1) != 0;
        set => PlayerPrefs.SetInt(P + "HybRes", value ? 1 : 0);
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
