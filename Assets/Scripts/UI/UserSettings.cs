using UnityEngine;

/// <summary>
/// Локальні PlayerPrefs для HUD / налаштування експерименту. Тема й мова мають власні ключі.
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

    /// <summary>Множник burst Monte-Carlo (Порівняти).</summary>
    public static float TimeScale
    {
        get => PlayerPrefs.GetFloat(P + "TimeScale", 12f);
        set => PlayerPrefs.SetFloat(P + "TimeScale", Mathf.Clamp(value, 1f, 40f));
    }

    /// <summary>Time.timeScale живого польоту (одиночний Start).</summary>
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

    /// <summary>Початкова висота h₀, м (одиночний політ + Monte-Carlo).</summary>
    public static float StartHeight
    {
        get => PlayerPrefs.GetFloat(P + "H0", 1800f);
        set => PlayerPrefs.SetFloat(P + "H0", Mathf.Clamp(value, 800f, 3000f));
    }

    /// <summary>Модуль початкової швидкості зниження |Vy|, м/с (як −Vy).</summary>
    public static float StartDescentSpeed
    {
        get => PlayerPrefs.GetFloat(P + "Vy0", 72f);
        set => PlayerPrefs.SetFloat(P + "Vy0", Mathf.Clamp(value, 30f, 120f));
    }

    /// <summary>Початковий нахил навколо Z, градуси.</summary>
    public static float StartTilt
    {
        get => PlayerPrefs.GetFloat(P + "Tilt0", 3.5f);
        set => PlayerPrefs.SetFloat(P + "Tilt0", Mathf.Clamp(value, 0f, 12f));
    }

    /// <summary>Шум маси Monte-Carlo ±%.</summary>
    public static float MassNoise
    {
        get => PlayerPrefs.GetFloat(P + "MassN", 8f);
        set => PlayerPrefs.SetFloat(P + "MassN", Mathf.Clamp(value, 0f, 15f));
    }

    /// <summary>Шум кута Monte-Carlo / одиночного запуску ±град.</summary>
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
        // За замовчуванням вимкнено: стабільна презентація; увімкнути для ES research-демо
        get => PlayerPrefs.GetInt(P + "Train", 0) != 0;
        set => PlayerPrefs.SetInt(P + "Train", value ? 1 : 0);
    }

    /// <summary>Hybrid MLP residual увімк (false = лише Fuzzy; за замовч. вимк).</summary>
    public static bool HybridResidual
    {
        // Ключ v2: старий HybRes за замовч. був 1 і лишав residual завжди увімкненим
        get => PlayerPrefs.GetInt(P + "HybRes2", 0) != 0;
        set => PlayerPrefs.SetInt(P + "HybRes2", value ? 1 : 0);
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
