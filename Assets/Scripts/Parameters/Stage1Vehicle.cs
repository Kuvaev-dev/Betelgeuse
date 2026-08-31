using UnityEngine;

/// <summary>
/// Ідентичність об'єкта керування: перший ступінь ракети-носія класу Falcon 9 Block 5
/// (документований аналог, не сертифікована копія SpaceX).
/// Візуал компактний (~28 м проти ~42.6 м натури) при повномасштабних масі / Ø / тязі посадки.
/// </summary>
public static class Stage1Vehicle
{
    /// <summary>Аналог: Falcon 9 FT / Block 5 first stage.</summary>
    public const string AnalogueName = "Falcon 9 Block 5 first stage (documented analogue)";

    /// <summary>Повна натурна довжина 1-го ступеня, м (візуал компактніший — див. RocketVisualBuilder.Height).</summary>
    public const float FullScaleHeightM = 42.6f;

    /// <summary>Діаметр бака, м.</summary>
    public const float DiameterM = 3.66f;

    /// <summary>Кількість основних РРД (Merlin-class); посадка — центральний.</summary>
    public const int EngineCount = 9;
    public const int LandingEngineCount = 1;

    /// <summary>Суха маса 1-го ступеня, кг (F9 ~25.6 т).</summary>
    public const float DryMassKg = 25600f;

    /// <summary>Залишок палива на ділянку посадки, кг (не повний бак підйому).</summary>
    public const float LandingFuelKg = 14000f;

    /// <summary>Тяга одного Merlin-class SL, Н — типова 1-engine landing burn.</summary>
    public const float LandingThrustN = 845000f;

    /// <summary>Isp Merlin-class (vac ~311 с; посадка біля рівня моря ближче до 282 с).</summary>
    public const float IspVacS = 311f;

    public const int GridFinCount = 4;
    public const int LandingLegCount = 4;

    /// <summary>Макс. відхилення TVC, град.</summary>
    public const float MaxTvcDeg = 16f;

    public static float LandingMassKg => DryMassKg + LandingFuelKg;

    public static void ApplyTo(SimulationParameters p)
    {
        if (p == null) return;
        p.dryMass = DryMassKg;
        p.fuelMass = LandingFuelKg;
        p.maxThrust = LandingThrustN;
        p.isp = IspVacS;
    }
}
