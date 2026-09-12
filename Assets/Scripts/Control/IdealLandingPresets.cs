using UnityEngine;

/// <summary>
/// Ідеальні умови посадки 1-го ступеня (Earth LZ) + per-algorithm GNC-тюнінг.
/// Лише ділянка після відділення (skipStackPhase) — номінал без вітру/шуму.
/// </summary>
public static class IdealLandingPresets
{
    /// <summary>true після [I] Ideal, доки не змінено режим/IC вручну.</summary>
    public static bool Active { get; private set; }

    public static void ClearActive() => Active = false;

    // Номінал ділянки посадки Stage-1 (Earth)
    public const float StartHeight = 1400f;
    public const float StartVy = -48f;
    /// <summary>Додатний модуль StartVy (для слайдерів UI).</summary>
    public const float StartDescentSpeed = 48f;
    /// <summary>0° — без початкового lean (інакше lateral розганяє вбік на півдорозі).</summary>
    public const float StartTiltDeg = 0f;
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
            Stage1Vehicle.ApplyTo(rocket.parameters);
            rocket.parameters.dryMass = DryMass;
            rocket.parameters.fuelMass = FuelMass;
            rocket.parameters.maxThrust = MaxThrust;
            rocket.parameters.isp = Stage1Vehicle.IspLandingS;
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
            // Чистий номінал: без вітру/шуму/jitter (MC має свої baseline-значення окремо)
            sim.enableNoise = false;
            sim.windStrength = 0f;
            sim.massVariationPercent = 0f;
            sim.angleVariationDegrees = 0f;
            sim.continuousWind = false;
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
        // PrepareMode обнуляв velocity — повернути landing IC для прев’ю
        if (rocket.parameters != null)
        {
            rocket.state.velocity = rocket.parameters.startVelocity;
            rocket.SyncTransformWithState();
        }
        Active = true;

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
        rocket?.SetPidGains(2.8f, 0.25f, 1.4f, 0.55f, 0f, 0.55f); // att Ki=0: no I-windup rock

        if (fuzzy != null)
        {
            fuzzy.isActive = true;
            fuzzy.heightScale = 2800f;
            fuzzy.velocityScale = 110f;
            fuzzy.maxGimbalDeg = 12f;
            // Профіль домінує по тязі; fuzzy — м’яка корекція
            fuzzy.fuzzyThrustWeight = 0.3f;
            fuzzy.maxDevFrac = 0.3f;
            fuzzy.gimbalBlend = 0.18f;
        }

        if (neural != null)
        {
            neural.isActive = true;
            neural.residualWeight = 0.28f;
            neural.maxDevFrac = 0.3f;
            // Майже 0 gimbal bias — інакше clean-run miss~26 м без вітру
            neural.gimbalBiasScale = 0.04f;
        }

        if (hybrid != null)
        {
            // Не форсувати residual: UI / MC ablation лишає свій вибір
            bool keepResidual = hybrid.useNeuralResidual;
            hybrid.isActive = true;
            hybrid.useNeuralResidual = keepResidual;
            // Thrust residual; gimbal residual майже OFF (анти drift)
            hybrid.neuralThrustBlend = 0.12f;
            hybrid.neuralGimbalBlend = 0.03f;
            hybrid.maxResidualMult = 0.16f;
            hybrid.smartWeight = 0.34f;
            hybrid.maxDevFrac = 0.26f;
            hybrid.fuzzy = fuzzy;
            hybrid.neural = neural;
        }
    }

    /// <summary>Повторно накласти ideal GNC для поточного режиму (перед START).</summary>
    public static void ReapplyModeIdeal(RocketPhysics rocket)
    {
        if (rocket == null) return;
        var fuzzy = rocket.fuzzyController ?? rocket.GetComponent<FuzzyLandingController>();
        var neural = rocket.neuralController ?? rocket.GetComponent<NeuralController>();
        var hybrid = rocket.hybridController ?? rocket.GetComponent<HybridController>();
        ApplyDefaultControllerTuning(rocket, fuzzy, neural, hybrid);
        ApplyModeIdeal(rocket.controlMode, rocket, fuzzy, neural, hybrid);
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
                rocket.SetPidGains(3.2f, 0.10f, 1.85f, 0.58f, 0f, 0.75f); // Ideal PID: Ki=0, more rate damp
                break;

            case RocketPhysics.ControlMode.Fuzzy:
                if (fuzzy != null)
                {
                    fuzzy.heightScale = 2400f;
                    fuzzy.velocityScale = 95f;
                    fuzzy.fuzzyThrustWeight = 0.32f; // ближче до профілю
                    fuzzy.maxDevFrac = 0.3f;
                    fuzzy.gimbalBlend = 0.35f;
                    fuzzy.maxGimbalDeg = 10f;
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
                    fuzzy.fuzzyThrustWeight = 0.32f;
                    fuzzy.maxDevFrac = 0.28f;
                    fuzzy.gimbalBlend = 0.3f;
                    fuzzy.maxGimbalDeg = 9f;
                }
                if (neural != null)
                {
                    neural.enableTraining = false;
                    neural.residualWeight = 0.15f;
                    neural.maxDevFrac = 0.22f;
                    neural.gimbalBiasScale = 0.05f;
                    neural.InstallIdealWeights();
                }
                if (hybrid != null)
                {
                    hybrid.useNeuralResidual = true;
                    hybrid.neuralThrustBlend = 0.08f;
                    hybrid.neuralGimbalBlend = 0.05f; // майже без NN-gimbal (анти-drift)
                    hybrid.maxResidualMult = 0.15f;
                    hybrid.smartWeight = 0.35f;
                    hybrid.maxDevFrac = 0.25f;
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
