using UnityEngine;

/// <summary>
/// Ядро симуляції посадки 1-го ступеня (plant + GNC), без залежностей від сцени Unity.
///
/// Такт (крок dt):
/// 1. Навігація: <see cref="NavigationEstimator"/> оцінює стан з IMU/GPS/висотоміра.
/// 2. Наведення (спільне для A–D): <see cref="LateralGuidance"/> → бажана вісь корпусу.
/// 3. Стратегія A–D (<see cref="ILandingController"/>) → тяга і кути TVC.
/// 4. Спільний safety-шар: аварійне вирівнювання при нахилі &gt; 28°, обмеження TVC,
///    інерційність приводів (швидкість TVC і дроселювання).
/// 5. Інтегрування RK4 поступального руху, обертання корпусу від моменту TVC.
/// 6. Торкання → <see cref="LandingMetrics"/> за <see cref="LandingCriteria"/> на істинному стані.
/// </summary>
public sealed class LandingSimulation
{
    // ---- Середовище ----
    /// <summary>Висота поверхні pad, м (узгоджено з EnvironmentBuilder.PadSurfaceY).</summary>
    public const float PadSurfaceY = 0.05f;
    /// <summary>Радіус робочої зони LZ, м.</summary>
    public const float AreaRadius = 2000f * 0.92f;

    // ---- Модель апарата (Falcon 9-class, спрощено) ----
    const float LeverArm = 11f;               // плече TVC від центру мас, м
    const float InertiaPerKg = 55f;           // I = m·55, кг·м²
    const float AngularDamping = 980000f;     // зведене аеродинамічне демпфування обертання, Н·м·с
    const float MaxOmega = 0.85f;             // рад/с
    const float MaxOmegaLow = 0.32f;          // рад/с нижче 40 м
    const float MaxOmegaTouchdown = 0.22f;    // рад/с нижче 15 м
    const float Cd = 1.05f;
    const float RefArea = 10.5f;              // π·(3.66/2)², м²
    const float G0 = 9.80665f;
    const float ThrottleSlewFracPerSec = 0.65f;
    const float GimbalRateDegPerSec = 12f;
    const float MaxGimbalDeg = 14f;
    const float MaxGimbalTerminalDeg = 8f;
    const float RecoveryTiltDeg = 28f;

    public readonly RocketState State = new RocketState();
    public readonly NavigationEstimator Navigation = new NavigationEstimator();
    public readonly LateralGuidance Guidance = new LateralGuidance();
    public LandingMetrics Metrics { get; private set; } = new LandingMetrics();

    public LandingSetup Setup { get; private set; } = LandingSetup.Default;
    public LandingScenario Scenario { get; private set; } = LandingScenario.Nominal();
    public ILandingController Controller;

    public Vector3 Wind;
    public bool ContinuousWind;
    public float NavNoiseScale;

    /// <summary>Остання команда стратегії (для телеметрії/UI).</summary>
    public ControlCommand LastCommand { get; private set; }
    /// <summary>Остання бажана вісь корпусу від наведення.</summary>
    public Vector3 LastDesiredUp { get; private set; } = Vector3.up;

    public bool Finished => State.simulationFinished;
    public float MaxAltitude { get; private set; }

    Vector3 lastAccel;
    float lateralScale = 1f;

    public LandingSimulation() { Reset(LandingSetup.Default, LandingScenario.Nominal()); }

    /// <summary>Скинути стан у ПУ з урахуванням конкретних збурень сценарію.</summary>
    public void Reset(in LandingSetup setup, in LandingScenario scenario)
    {
        Setup = setup;
        Scenario = scenario;

        var s = State;
        s.dryMass = setup.DryMass;
        s.currentFuelMass = Mathf.Max(500f, setup.FuelMass * (scenario.FuelScale > 0f ? scenario.FuelScale : 1f));
        s.maxThrust = setup.MaxThrust;
        s.position = setup.StartPosition + new Vector3(scenario.OffsetX, 0f, scenario.OffsetZ);
        s.velocity = setup.StartVelocity;
        s.rotation = Quaternion.Normalize(Quaternion.Euler(setup.StartEulerDeg)
                                          * Quaternion.Euler(scenario.TiltXDeg, 0f, scenario.TiltZDeg));
        s.angularVelocity = Vector3.zero;
        s.currentThrust = 0f;
        s.thrustDirection = Vector3.up;
        s.time = 0f;
        s.isLanded = false;
        s.simulationFinished = false;

        Wind = scenario.Wind;
        ContinuousWind = scenario.ContinuousWind && Wind.sqrMagnitude > 1e-4f;
        // Вхід у зону посадки з частково захопленою вітром горизонтальною швидкістю
        s.velocity += Wind * 0.32f;
        NavNoiseScale = scenario.NavNoiseScale;

        Metrics = new LandingMetrics();
        MaxAltitude = s.position.y;
        lateralScale = 1f;
        LastDesiredUp = Vector3.up;
        LastCommand = default;
        Guidance.Reset();
        Controller?.ResetSession();
        AlignNavigationToTruth();
    }

    /// <summary>Перевиставити оцінювач навігації на істинний стан (після ручної зміни стану).</summary>
    public void AlignNavigationToTruth()
    {
        Navigation.Reset(State, (uint)Mathf.Max(1, Scenario.Seed));
        lastAccel = AccelerationAt(State.position, State.velocity);
    }

    /// <summary>Живе оновлення вітру (слайдер під час польоту). Напрям зберігається.</summary>
    public void SetWindStrength(float strength, float directionRad)
    {
        strength = Mathf.Max(0f, strength);
        if (strength < 0.05f) { Wind = Vector3.zero; ContinuousWind = false; return; }
        Vector3 dir = Wind.sqrMagnitude > 1e-4f
            ? Wind.normalized
            : new Vector3(Mathf.Cos(directionRad), 0f, Mathf.Sin(directionRad));
        Wind = dir * strength;
        ContinuousWind = true;
    }

    /// <summary>Один такт GNC + інтегрування.</summary>
    public void Step()
    {
        if (State.simulationFinished) return;
        float dt = Setup.Dt;
        State.time += dt;

        if (State.time >= Setup.MaxTime)
        {
            Finish(timeout: true);
            return;
        }

        Navigation.Step(State, lastAccel, dt, NavNoiseScale);
        UpdateControl(dt);
        Integrate(dt);
        lastAccel = AccelerationAt(State.position, State.velocity);

        if (State.position.y < PadSurfaceY) State.position.y = PadSurfaceY;
        ClampToArea();
        if (State.position.y > MaxAltitude) MaxAltitude = State.position.y;

        if (State.position.y <= PadSurfaceY + 0.04f)
            Finish(timeout: false);
    }

    /// <summary>Прогнати до завершення (Monte-Carlo / навчання).</summary>
    public LandingMetrics RunToEnd()
    {
        while (!State.simulationFinished) Step();
        return Metrics;
    }

    /// <summary>Примусово завершити з оцінкою (СТОП з метриками / timeout).</summary>
    public void ForceFinish(bool asTimeout)
    {
        if (!State.simulationFinished) Finish(asTimeout);
    }

    void UpdateControl(float dt)
    {
        var s = State;
        bool cleanNav = NavNoiseScale < 1e-5f || !Navigation.Current.valid;
        var nav = Navigation.Current;
        Vector3 pos = cleanNav ? s.position : nav.position;
        Vector3 vel = cleanNav ? s.velocity : nav.velocity;
        Quaternion rot = cleanNav ? s.rotation : nav.rotation;
        Vector3 omega = cleanNav ? s.angularVelocity : nav.angularVelocity;
        float mass = s.TotalMass;
        float h = pos.y;

        // 1) Спільне бічне наведення → бажана вісь корпусу
        float accelUp = s.currentThrust / Mathf.Max(1f, mass);
        Vector3 desiredUp = Guidance.DesiredUp(h, pos, vel, lateralScale, accelUp, dt);
        LastDesiredUp = desiredUp;

        // 2) Стратегія A–D
        var ctx = ControlContext.Create(pos, vel, rot, omega, mass, s.currentThrust, s.maxThrust, desiredUp, dt);
        ControlCommand cmd = Controller != null ? Controller.Evaluate(in ctx) : ControlCommand.ProfileFallback(in ctx);
        LastCommand = cmd;
        lateralScale = Mathf.Clamp(cmd.LateralScale, 0.2f, 3f);

        // 3) Спільний safety-шар
        float tilt = ctx.TiltDeg;
        float hover = mass * ctx.Gravity;
        // Вертикальна складова тяги ∝ cos(нахилу)
        float thrustCmd = cmd.Thrust / Mathf.Max(0.85f, Mathf.Cos(tilt * Mathf.Deg2Rad));
        thrustCmd *= SoftLandingGuidance.UprightThrustScale(tilt);
        Vector3 gimbal = cmd.GimbalEuler;
        if (tilt > RecoveryTiltDeg)
        {
            // Аварійне вирівнювання до вертикалі: спільний PD незалежно від алгоритму
            var upCtx = ControlContext.Create(pos, vel, rot, omega, mass, s.currentThrust, s.maxThrust, Vector3.up, dt);
            gimbal = SoftLandingGuidance.AttitudeGimbal(in upCtx);
            thrustCmd = Mathf.Clamp(Mathf.Max(thrustCmd, hover * 1.05f), hover * 0.2f, hover * 2.2f);
        }

        float gLim = h < 60f ? MaxGimbalTerminalDeg : MaxGimbalDeg;
        gimbal.x = Mathf.Clamp(gimbal.x, -gLim, gLim);
        gimbal.z = Mathf.Clamp(gimbal.z, -gLim, gLim);
        Vector3 desiredDir = Quaternion.Euler(gimbal.x, 0f, gimbal.z) * Vector3.up;

        // 4) Інерційність приводів
        Vector3 fromDir = s.thrustDirection.sqrMagnitude > 0.01f ? s.thrustDirection.normalized : Vector3.up;
        s.thrustDirection = Vector3.RotateTowards(fromDir, desiredDir,
            GimbalRateDegPerSec * Mathf.Deg2Rad * dt, 0f).normalized;

        float thrustTarget = s.currentFuelMass > 0f ? Mathf.Clamp(thrustCmd, 0f, s.maxThrust) : 0f;
        float slew = Mathf.Max(1f, s.maxThrust) * ThrottleSlewFracPerSec * dt;
        s.currentThrust = Mathf.MoveTowards(s.currentThrust, thrustTarget, slew);
    }

    void Integrate(float dt)
    {
        var s = State;
        Vector3 k1v = s.velocity;
        Vector3 k1a = AccelerationAt(s.position, s.velocity);
        Vector3 k2v = s.velocity + k1a * (dt * 0.5f);
        Vector3 k2a = AccelerationAt(s.position + k1v * (dt * 0.5f), k2v);
        Vector3 k3v = s.velocity + k2a * (dt * 0.5f);
        Vector3 k3a = AccelerationAt(s.position + k2v * (dt * 0.5f), k3v);
        Vector3 k4v = s.velocity + k3a * dt;
        Vector3 k4a = AccelerationAt(s.position + k3v * dt, k4v);

        s.velocity += (k1a + 2f * k2a + 2f * k3a + k4a) * (dt / 6f);
        s.position += (k1v + 2f * k2v + 2f * k3v + k4v) * (dt / 6f);

        if (s.currentFuelMass > 0f && s.currentThrust > 0f)
        {
            float massFlow = s.currentThrust / (Setup.Isp * G0);
            s.currentFuelMass = Mathf.Max(0f, s.currentFuelMass - massFlow * dt);
        }

        // Обертання: момент від відхилення вектора тяги (тіло), демпфування, обмеження ω
        Vector3 torque = new Vector3(-s.thrustDirection.z, 0f, s.thrustDirection.x) * (s.currentThrust * LeverArm);
        torque -= s.angularVelocity * AngularDamping;
        float inertia = Mathf.Max(1f, s.TotalMass * InertiaPerKg);
        s.angularVelocity += torque / inertia * dt;

        float wMax = s.position.y < 15f ? MaxOmegaTouchdown : (s.position.y < 40f ? MaxOmegaLow : MaxOmega);
        if (s.angularVelocity.sqrMagnitude > wMax * wMax)
            s.angularVelocity = s.angularVelocity.normalized * wMax;

        float wMag = s.angularVelocity.magnitude;
        if (wMag > 1e-8f)
            s.rotation = Quaternion.Normalize(
                s.rotation * Quaternion.AngleAxis(wMag * dt * Mathf.Rad2Deg, s.angularVelocity / wMag));
    }

    /// <summary>Прискорення центру мас: гравітація + тяга + опір (відносно повітря) + grid fins.</summary>
    Vector3 AccelerationAt(Vector3 pos, Vector3 vel)
    {
        var s = State;
        Vector3 acc = new Vector3(0f, -AtmosphereModel.GetGravity(pos.y), 0f);

        Vector3 td = s.thrustDirection.sqrMagnitude > 1e-6f ? s.thrustDirection.normalized : Vector3.up;
        float mass = Mathf.Max(1f, s.TotalMass);
        acc += s.rotation * td * (s.currentThrust / mass);

        Vector3 airRel = vel - EffectiveWind();
        float density = AtmosphereModel.GetDensity(pos.y);
        if (airRel.sqrMagnitude > 0.01f)
            acc -= airRel.normalized * (0.5f * density * airRel.sqrMagnitude * Cd * RefArea / mass);

        // Grid fins: бічна сила по швидкості відносно повітря; складені нижче ~60 м
        float hAgl = pos.y - PadSurfaceY;
        if (hAgl > 60f && hAgl < 2200f)
        {
            float deploy = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((hAgl - 60f) / 80f));
            float kFin = (0.09f + 0.08f * density / 1.225f) * deploy;
            acc -= new Vector3(airRel.x, 0f, airRel.z) * kFin;
        }
        return acc;
    }

    /// <summary>Вітер з поривами (дві гармоніки, 30 % амплітуди).</summary>
    public Vector3 EffectiveWind()
    {
        if (!ContinuousWind || Wind.sqrMagnitude < 1e-4f) return Vector3.zero;
        float t = State.time;
        float a = 0.30f * Wind.magnitude;
        Vector3 along = Wind.normalized;
        Vector3 side = Vector3.Cross(along, Vector3.up).normalized;
        return Wind + along * (a * Mathf.Sin(t * 0.85f)) + side * (a * 0.75f * Mathf.Sin(t * 1.37f + 0.6f));
    }

    void ClampToArea()
    {
        var s = State;
        float r2 = s.position.x * s.position.x + s.position.z * s.position.z;
        if (r2 <= AreaRadius * AreaRadius) return;
        float k = AreaRadius / Mathf.Sqrt(r2);
        s.position.x *= k;
        s.position.z *= k;
        Vector3 radial = new Vector3(s.position.x, 0f, s.position.z).normalized;
        float vOut = Vector3.Dot(s.velocity, radial);
        if (vOut > 0f) s.velocity -= radial * vOut;
    }

    void Finish(bool timeout)
    {
        var s = State;
        if (!timeout) s.position.y = PadSurfaceY;

        var m = new LandingMetrics
        {
            touchdownVelocity = Mathf.Abs(s.velocity.y),
            horizontalMiss = new Vector2(s.position.x, s.position.z).magnitude,
            horizontalSpeed = new Vector2(s.velocity.x, s.velocity.z).magnitude,
            landingAngleError = Vector3.Angle(s.rotation * Vector3.up, Vector3.up),
            fuelRemaining = s.currentFuelMass,
            maxAltitude = MaxAltitude,
            totalFlightTime = s.time,
            timedOut = timeout
        };
        m.isSuccessfulLanding = LandingCriteria.IsSuccessful(m, Setup.Limits);
        Metrics = m;

        s.isLanded = true;
        s.simulationFinished = true;
        s.velocity = Vector3.zero;
        s.angularVelocity = Vector3.zero;
        s.currentThrust = 0f;
    }
}
