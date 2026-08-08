using UnityEngine;

/// <summary>
/// Ідеальні умови + per-algorithm GNC-тюнінг для гарантованої м’якої посадки.
/// Не змінює «характер» алгоритмів назавжди — лише виставляє номінал без збурень
/// і коефіцієнти, за яких кожен A/B/C/D стабільно сідає.
/// Ручні/Monte-Carlo умови лишаються складнішими (див. LandingParams default).
/// </summary>
public static class IdealLandingPresets
{
    // Спокійний номінал (не default сцени)
    public const float StartHeight = 1400f;
    public const float StartVy = -48f;
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
            rocket.parameters.maxTouchdownVelocity = 3.5f;
            rocket.parameters.maxLandingAngle = 7f;
            rocket.parameters.maxHorizontalMiss = 25f;
            rocket.parameters.maxHorizontalSpeed = 5f;
        }

        rocket.windVelocity = Vector3.zero;
        rocket.applyContinuousWind = false;
        rocket.SyncFixedTimestep();

        if (sim != null)
        {
            sim.enableNoise = false;
            sim.windStrength = 0f;
            // Не обнуляємо mass/angle variation назавжди — інакше після Ideal
            // toggle «Шум» не впливав би на одиночний старт.
            sim.massVariationPercent = 6f;
            sim.angleVariationDegrees = 7f;
            sim.continuousWind = true;
        }
        // UI-слайдери вітру/шуму скидає MissionControlUI після Apply

        var fuzzy = rocket.fuzzyController ?? rocket.GetComponent<FuzzyLandingController>();
        var neural = rocket.neuralController ?? rocket.GetComponent<NeuralController>();
        var hybrid = rocket.hybridController ?? rocket.GetComponent<HybridController>();

        // Скинути «агресивні» default-и, потім per-mode ідеал
        ApplyDefaultControllerTuning(rocket, fuzzy, neural, hybrid);
        ApplyModeIdeal(rocket.controlMode, rocket, fuzzy, neural, hybrid);

        rocket.PrepareMode(rocket.controlMode);

        string mode = rocket.GetModeDisplayName();
        summaryUk =
            $"Ідеал для «{mode}»: h₀={StartHeight:F0} м, Vᵧ={StartVy:F0} м/с, крен {StartTiltDeg:F1}°.\n" +
            "Вітер/шум ВИМК · GNC-тюнінг цього алгоритму.\n" +
            "ЗАПУСТИТИ ПОСАДКУ — очікуваний успіх. Ручні умови лишаються складнішими.";
        summaryEn =
            $"Ideal for “{mode}”: h₀={StartHeight:F0} m, Vᵧ={StartVy:F0} m/s, tilt {StartTiltDeg:F1}°.\n" +
            "Wind/noise OFF · per-algorithm GNC tune.\n" +
            "START LANDING — expected success. Manual conditions stay harder.";
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
            fuzzy.maxGimbalDeg = 18f;
            fuzzy.fuzzyThrustWeight = 0.55f;
            fuzzy.maxDevFrac = 0.55f;
            fuzzy.gimbalBlend = 0.5f;
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
            hybrid.neuralThrustBlend = 0.25f;
            hybrid.neuralGimbalBlend = 0.2f;
            hybrid.maxResidualMult = 0.3f;
            hybrid.smartWeight = 0.5f;
            hybrid.maxDevFrac = 0.4f;
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
