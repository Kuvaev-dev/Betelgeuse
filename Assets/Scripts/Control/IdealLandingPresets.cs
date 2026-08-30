using UnityEngine;

/// <summary>
/// Ідеальні умови посадки 1-го ступеня (Earth LZ) + per-algorithm GNC-тюнінг.
/// Лише ділянка після відділення (skipStackPhase) — номінал без вітру/шуму.
/// </summary>
public static class IdealLandingPresets
{
    // Номінал ділянки посадки Stage-1 (Earth)
    public const float StartHeight = 1400f;
    public const float StartVy = -48f;
    /// <summary>Додатний модуль StartVy (для слайдерів UI).</summary>
    public const float StartDescentSpeed = 48f;
    public const float StartTiltDeg = 0.4f;
    public const float DryMass = 25600f;
    public const float FuelMass = 14000f;
    public const float MaxThrust = 845000f;

    public static void Apply(
        RocketPhysics rocket,
        SimulationManager sim,
        out string summaryUk,
        out string summaryEn)
    {
        if (rocket == null)
        {
            summaryUk = "RocketPhysics відсутній.";
            summaryEn = "RocketPhysics missing.";
            return;
        }

        if (rocket.parameters != null)
        {
            rocket.parameters.startPosition = new Vector3(0f, StartHeight, 0f);
            rocket.parameters.startVelocity = new Vector3(0f, StartVy, 0f);
            rocket.parameters.startEulerAngles = new Vector3(0f, 0f, StartTiltDeg);
            rocket.parameters.dryMass = DryMass;
            rocket.parameters.fuelMass = FuelMass;
            rocket.parameters.maxThrust = MaxThrust;
            rocket.parameters.isp = 311f;
            rocket.parameters.fixedTimeStep = 0.005f;
            rocket.parameters.maxSimulationTime = 400f;
            rocket.parameters.maxTouchdownVelocity = LandingCriteria.DefaultMaxTouchdownVelocity;
            rocket.parameters.maxLandingAngle = LandingCriteria.DefaultMaxLandingAngle;
            rocket.parameters.maxHorizontalMiss = LandingCriteria.DefaultMaxHorizontalMiss;
            rocket.parameters.maxHorizontalSpeed = LandingCriteria.DefaultMaxHorizontalSpeed;
        }

        rocket.windVelocity = Vector3.zero;
        rocket.applyContinuousWind = false;
        // Ideal = лише ділянка посадки 1-го ступеня (без Stack-підйому)
        rocket.skipStackPhase = true;
        rocket.SyncFixedTimestep();

        if (sim != null)
        {
            sim.enableNoise = false;
            sim.windStrength = 0f;
            sim.massVariationPercent = 6f;
            sim.angleVariationDegrees = 7f;
            sim.continuousWind = true;
            sim.startHeight = StartHeight;
            sim.startDescentSpeed = StartDescentSpeed;
            sim.startTiltDeg = StartTiltDeg;
        }

        var fuzzy = rocket.fuzzyController ?? rocket.GetComponent<FuzzyLandingController>();
        var neural = rocket.neuralController ?? rocket.GetComponent<NeuralController>();
        var hybrid = rocket.hybridController ?? rocket.GetComponent<HybridController>();

        ApplyDefaultControllerTuning(rocket, fuzzy, neural, hybrid);
        ApplyModeIdeal(rocket.controlMode, rocket, fuzzy, neural, hybrid);

        rocket.PrepareMode(rocket.controlMode);
        rocket.skipStackPhase = true;

        string mode = rocket.GetModeDisplayName();
        summaryUk =
            $"Ідеал Stage-1 (Earth LZ) «{mode}»: h₀={StartHeight:F0} м, Vᵧ={StartVy:F0} м/с, крен {StartTiltDeg:F1}°.\n" +
            "Після відділення · вітер/шум ВИМК · маса 1-го ступеня.\n" +
            "ЗАПУСТИТИ — очікуваний soft-landing.";
        summaryEn =
            $"Ideal Stage-1 (Earth LZ) “{mode}”: h₀={StartHeight:F0} m, Vᵧ={StartVy:F0} m/s, tilt {StartTiltDeg:F1}°.\n" +
            "After separation · wind/noise OFF · first-stage mass.\n" +
            "START — expected soft-landing.";
    }

    /// <summary>Робочі (не ідеальні) коефіцієнти — відмінності A/B/C/D помітні.</summary>
    public static void ApplyDefaultControllerTuning(
        RocketPhysics rocket,
        FuzzyLandingController fuzzy,
        NeuralController neural,
        HybridController hybrid)
    {
        rocket?.SetPidGains(2.8f, 0.25f, 1.4f, 0.55f, 0.04f, 0.48f);

        if (fuzzy != null)
        {
            fuzzy.isActive = true;
            fuzzy.heightScale = 2800f;
            fuzzy.velocityScale = 110f;
            fuzzy.maxGimbalDeg = 14f;
            fuzzy.fuzzyThrustWeight = 0.42f;
            fuzzy.maxDevFrac = 0.4f;
            fuzzy.gimbalBlend = 0.35f;
        }

        if (neural != null)
        {
            neural.isActive = true;
            neural.residualWeight = 0.48f;
            neural.maxDevFrac = 0.6f;
            neural.gimbalBiasScale = 0.22f;
            // training лишається як виставлено UI
        }

        if (hybrid != null)
        {
            hybrid.isActive = true;
            hybrid.neuralThrustBlend = 0.2f;
            hybrid.neuralGimbalBlend = 0.15f;
            hybrid.maxResidualMult = 0.25f;
            hybrid.smartWeight = 0.42f;
            hybrid.maxDevFrac = 0.32f;
            hybrid.fuzzy = fuzzy;
            hybrid.neural = neural;
        }
    }

    static void ApplyModeIdeal(
        RocketPhysics.ControlMode mode,
        RocketPhysics rocket,
        FuzzyLandingController fuzzy,
        NeuralController neural,
        HybridController hybrid)
    {
        switch (mode)
        {
            case RocketPhysics.ControlMode.PID:
                // Добре відтюнований класичний PID + сильніший термінал через gains
                rocket.SetPidGains(3.4f, 0.12f, 1.9f, 0.62f, 0.02f, 0.62f);
                break;

            case RocketPhysics.ControlMode.Fuzzy:
                if (fuzzy != null)
                {
                    fuzzy.heightScale = 2400f;
                    fuzzy.velocityScale = 95f;
                    fuzzy.fuzzyThrustWeight = 0.32f; // ближче до профілю
                    fuzzy.maxDevFrac = 0.3f;
                    fuzzy.gimbalBlend = 0.35f;
                    fuzzy.maxGimbalDeg = 14f;
                }
                break;

            case RocketPhysics.ControlMode.Neural:
                if (neural != null)
                {
                    neural.enableTraining = false;
                    neural.residualWeight = 0.22f;
                    neural.maxDevFrac = 0.28f;
                    neural.gimbalBiasScale = 0.1f;
                    neural.mutationSigma = 0.03f;
                    neural.InstallIdealWeights();
                }
                break;

            case RocketPhysics.ControlMode.Hybrid:
                if (fuzzy != null)
                {
                    fuzzy.fuzzyThrustWeight = 0.35f;
                    fuzzy.maxDevFrac = 0.32f;
                    fuzzy.gimbalBlend = 0.4f;
                    fuzzy.maxGimbalDeg = 14f;
                }
                if (neural != null)
                {
                    neural.enableTraining = false;
                    neural.residualWeight = 0.2f;
                    neural.maxDevFrac = 0.25f;
                    neural.InstallIdealWeights();
                }
                if (hybrid != null)
                {
                    hybrid.neuralThrustBlend = 0.12f;
                    hybrid.neuralGimbalBlend = 0.1f;
                    hybrid.maxResidualMult = 0.18f;
                    hybrid.smartWeight = 0.4f;
                    hybrid.maxDevFrac = 0.28f;
                }
                break;
        }
    }

    public static bool ProfileGuaranteesSoftLanding(out float touchdownVy)
    {
        float mass = DryMass + FuelMass;
        touchdownVy = SoftLandingGuidance.SimulateVerticalLanding(
            StartHeight, StartVy, mass, MaxThrust);
        return touchdownVy < 3.5f;
    }
}
