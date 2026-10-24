using UnityEngine;

/// <summary>
/// «Ідеальні» (номінальні) умови демонстрації [I]: чисті ПУ без вітру, шуму і розкиду.
/// Змінює лише умови експерименту; закони керування і фізика — ті самі, що в Monte-Carlo.
/// </summary>
public static class IdealLandingPresets
{
    /// <summary>true після [I], доки користувач не змінив умови вручну.</summary>
    public static bool Active { get; private set; }

    public static void ClearActive() => Active = false;

    public const float StartHeight = 1400f;
    public const float StartVy = -48f;
    /// <summary>Модуль StartVy (для слайдерів UI).</summary>
    public const float StartDescentSpeed = 48f;
    public const float StartTiltDeg = 0f;

    public static void Apply(RocketPhysics rocket, SimulationManager sim, out string summaryUk, out string summaryEn)
    {
        if (rocket == null)
        {
            summaryUk = "RocketPhysics відсутній.";
            summaryEn = "RocketPhysics missing.";
            return;
        }

        if (rocket.parameters != null)
        {
            rocket.parameters.ApplyStage1Vehicle();
            rocket.parameters.SetInitialConditions(StartHeight, StartDescentSpeed, StartTiltDeg);
        }

        if (sim != null)
        {
            sim.enableNoise = false;
            sim.windStrength = 0f;
            sim.massVariationPercent = 0f;
            sim.angleVariationDegrees = 0f;
            sim.positionJitterMeters = 0f;
            sim.continuousWind = false;
            sim.startHeight = StartHeight;
            sim.startDescentSpeed = StartDescentSpeed;
            sim.startTiltDeg = StartTiltDeg;
        }

        rocket.PrepareMode(rocket.controlMode);
        Active = true;

        string mode = rocket.GetModeDisplayName();
        summaryUk =
            $"Номінал Stage-1 (Earth LZ) «{mode}»: h₀={StartHeight:F0} м, Vᵧ={StartVy:F0} м/с, нахил {StartTiltDeg:F1}°.\n" +
            "Вітер/шум/розкид ВИМК · закони керування без змін.\nЗАПУСТИТИ — очікуваний soft-landing.";
        summaryEn =
            $"Nominal Stage-1 (Earth LZ) “{mode}”: h₀={StartHeight:F0} m, Vᵧ={StartVy:F0} m/s, tilt {StartTiltDeg:F1}°.\n" +
            "Wind/noise/dispersion OFF · control laws unchanged.\nSTART — expected soft-landing.";
    }

    /// <summary>Чи забезпечує профіль soft-landing у 1D (для тестів).</summary>
    public static bool ProfileGuaranteesSoftLanding(out float touchdownVy)
    {
        touchdownVy = SoftLandingGuidance.SimulateVerticalLanding(
            StartHeight, StartVy, Stage1Vehicle.LandingMassKg, Stage1Vehicle.LandingThrustN);
        return touchdownVy < LandingCriteria.DefaultMaxTouchdownVelocity;
    }
}
