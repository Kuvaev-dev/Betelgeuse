using UnityEngine;

/// <summary>
/// Ядро фізики та GNC (Earth LZ): RK4, TVC, soft-landing 1-го ступеня.
/// Фази: <see cref="FlightPhase.Stack"/> (спрощений підйом пакета) →
/// відділення → <see cref="FlightPhase.Stage1"/> (A–D лише тут).
/// </summary>
[RequireComponent(typeof(DataLogger))]
public class RocketPhysics : MonoBehaviour
{
    [Header("Основні параметри")]
    public SimulationParameters parameters;
    /// <summary>Алгоритм керування (A–D у UI) — активний лише у фазі Stage1.</summary>
    public enum ControlMode { PID, Fuzzy, Neural, Hybrid }

    /// <summary>Фаза місії: пакет до sep / 1-й ступінь на посадці.</summary>
    public enum FlightPhase { Idle, Stack, Stage1 }

    [Header("Режим керування")]
    public ControlMode controlMode = ControlMode.Hybrid;
    /// <summary>Поточний фізичний стан (єдине джерело правди для камери/UI).</summary>
    public RocketState state = new RocketState();

    [Header("Фаза польоту")]
    public FlightPhase phase = FlightPhase.Idle;
    /// <summary>
    /// true = одразу Stage1 (єдиний режим UI: лише 1-й ступінь).
    /// </summary>
    public bool skipStackPhase = true;
    /// <summary>Чи відбулось відділення в поточному прогоні.</summary>
    public bool hasSeparated { get; private set; }

    [Header("Запуск")]
    [Tooltip("false = ракета чекає кнопки «Запустити посадку»")]
    public bool simulationArmed = false;
    /// <summary>Пауза польоту (кнопка ПАУЗА) — тик фізики пропускається, стан зберігається.</summary>
    public bool simulationPaused = false;

    [Header("Зовнішні збурення")]
    public Vector3 windVelocity = Vector3.zero;
    public bool applyContinuousWind = true;
    /// <summary>Відкладені збурення UI/MC — застосовуються після відділення (не до пакета).</summary>
    float pendingWindStrength;
    bool pendingRandomize;
    float pendingMassVar = 6f;
    float pendingAngleVar = 7f;
    float pendingJitter = 18f;
    bool hasPendingDisturbances;

    private DataLogger logger;

    /// <summary>Стратегія PID (режим A) — чистий клас через патерн Strategy.</summary>
    readonly PidLandingStrategy pidStrategy = new PidLandingStrategy();

    /// <summary>Резолвить ILandingController за ControlMode (DIP).</summary>
    LandingControllerResolver controllerResolver;

    /// <summary>Налаштування PID з IdealLandingPresets / UI.</summary>
    public void SetPidGains(float thrustKp, float thrustKi, float thrustKd,
        float attKp, float attKi, float attKd)
        => pidStrategy.SetGains(thrustKp, thrustKi, thrustKd, attKp, attKi, attKd);

    public FuzzyLandingController fuzzyController;
    public NeuralController neuralController;
    public HybridController hybridController;
    public LandingMetrics metrics = new LandingMetrics();
    public readonly NavigationEstimator navigation = new NavigationEstimator();
    public float NavNoiseScale;
    public uint NavSeed = 1;
    Vector3 lastAccel;

    private float maxHeightRecorded;
    private float currentTime;
    private TrajectoryVisualizer cachedVisualizer;

    const float LeverArm = 11f;
    const float AngularDamping = 980000f; // сильніше — без mid-flight PIO
    const float InertiaFactor = 55f;
    const float MaxOmega = 0.85f; // rad/s
    const float MaxOmegaTerminal = 0.35f; // rad/s — анти-раскачка біля pad
    // Lumped Cd*S = booster body + 4 grid fins (F9-class analogue). Product unchanged.
    const float Cd = 0.85f;
    const float RefArea = 8.5f;
    const float G0 = 9.80665f;

    void Start()
    {
        logger = GetComponent<DataLogger>();
        if (logger == null) logger = gameObject.AddComponent<DataLogger>();
        logger.Initialize();

        if (fuzzyController == null) fuzzyController = GetComponent<FuzzyLandingController>();
        if (fuzzyController == null) fuzzyController = gameObject.AddComponent<FuzzyLandingController>();
        if (neuralController == null) neuralController = GetComponent<NeuralController>();
        if (neuralController == null) neuralController = gameObject.AddComponent<NeuralController>();
        if (hybridController == null) hybridController = GetComponent<HybridController>();
        if (hybridController == null)
            hybridController = gameObject.AddComponent<HybridController>();
        hybridController.fuzzy = fuzzyController;
        hybridController.neural = neuralController;

        if (neuralController != null)
        {
            neuralController.LoadBestWeights();
            // Гарантія демо: якщо файлу ваг немає — встановити фізично обґрунтовані
            if (neuralController.generation <= 0 && neuralController.bestCost >= float.MaxValue * 0.5f)
                neuralController.InstallIdealWeights();
        }
        cachedVisualizer = FindAnyObjectByType<TrajectoryVisualizer>();

        // Composition root: зареєструвати стратегії один раз
        controllerResolver = LandingControllerResolver.CreateDefault(this, pidStrategy);

        SyncFixedTimestep();
        InitializeSimulation();
    }

    /// <summary>
    /// Узгоджує Unity FixedUpdate з кроком інтегратора RK4 (інакше sim ≠ real-time).
    /// </summary>
    public void SyncFixedTimestep()
    {
        if (parameters == null) return;
        float step = Mathf.Clamp(parameters.fixedTimeStep, 0.001f, 0.05f);
        parameters.fixedTimeStep = step;
        Time.fixedDeltaTime = step;
    }

    void InitializeSimulation()
    {
        if (parameters == null) return;
        parameters.EnsureStackDefaults();
        hasSeparated = false;
        hasPendingDisturbances = false;

        // Завжди лише 1-й ступінь (візуал і GNC)
        skipStackPhase = true;
        EnterStage1(fromSeparation: false, applyLandingIc: true);
        hasSeparated = true;

        state.currentThrust = 0f;
        state.thrustDirection = Vector3.up;
        state.time = 0f;
        state.isLanded = false;
        state.simulationFinished = false;
        SyncTransformWithState();
        RocketVisualBuilder.SetUpperStackVisible(transform, false);
        AlignNavigationToTruth();
    }

    void EnterStack()
    {
        phase = FlightPhase.Stack;
        hasSeparated = false;
        state.position = parameters.stackStartPosition;
        state.velocity = parameters.stackStartVelocity;
        state.rotation = Quaternion.Euler(parameters.stackStartEulerAngles);
        state.angularVelocity = Vector3.zero;
        state.dryMass = parameters.stackDryMass;
        state.currentFuelMass = parameters.stackFuelMass;
        state.maxThrust = parameters.stackMaxThrust;
        // Під час Stack вітер не застосовуємо
        windVelocity = Vector3.zero;
        applyContinuousWind = false;
        RocketVisualBuilder.SetUpperStackVisible(transform, true);
    }

    void EnterStage1(bool fromSeparation, bool applyLandingIc)
    {
        phase = FlightPhase.Stage1;
        if (fromSeparation) hasSeparated = true;

        state.dryMass = parameters.dryMass;
        state.currentFuelMass = parameters.fuelMass;
        state.maxThrust = parameters.maxThrust;

        if (applyLandingIc || (fromSeparation && parameters.snapToLandingIcOnSeparation))
        {
            state.position = parameters.startPosition;
            state.velocity = parameters.startVelocity;
            state.rotation = Quaternion.Euler(parameters.startEulerAngles);
            state.angularVelocity = Vector3.zero;
            if (fromSeparation)
            {
                currentTime = 0f;
                state.time = 0f;
                maxHeightRecorded = state.position.y;
            }
        }

        // 1) sync stage1 у фінальну позу (після можливого snap)
        // 2) лише тоді cinematic sep — upper прив’язується до stage1 у кадрі камери
        SyncTransformWithState();
        if (fromSeparation)
        {
            RocketVisualBuilder.SetUpperStackVisible(transform, false, animateAway: true);
            ApplyPendingDisturbancesIfAny();
        }
        else
        {
            RocketVisualBuilder.SetUpperStackVisible(transform, false, animateAway: false);
        }
        AlignNavigationToTruth();
    }

    void CheckSeparation()
    {
        if (phase != FlightPhase.Stack || parameters == null) return;
        float h = state.position.y;
        float t = state.time;
        bool byAlt = h >= parameters.separationAltitude;
        bool byTime = t >= parameters.separationTime;
        if (!byAlt && !byTime) return;
        EnterStage1(fromSeparation: true, applyLandingIc: parameters.snapToLandingIcOnSeparation);
        controllerResolver?.ResetAll();
        pidStrategy.ResetSession();
    }

    /// <summary>Спрощений open-loop підйом пакета (не A–D).</summary>
    void UpdateStackControl()
    {
        float h = state.position.y;
        float vy = state.velocity.y;
        float sepH = parameters != null ? parameters.separationAltitude : 2400f;
        // Профіль: повна тяга на розгін, throttle down біля sep
        float throttle = 1f;
        if (h > sepH * 0.55f) throttle = 0.72f;
        if (h > sepH * 0.82f) throttle = 0.35f;
        if (vy > 120f) throttle = Mathf.Min(throttle, 0.25f);
        if (h > sepH * 0.95f) throttle = 0.05f;

        state.thrustDirection = Vector3.up;
        state.currentThrust = state.maxThrust * throttle;
        if (state.currentFuelMass <= 0f) state.currentThrust = 0f;

        // М'яке вирівнювання корпусу
        state.rotation = Quaternion.Slerp(state.rotation, Quaternion.identity, 0.08f);
        state.angularVelocity *= 0.9f;
    }

    /// <summary>True, поки Monte-Carlo ганяє ticks вручну (пропустити double-step FixedUpdate).</summary>
    public bool batchDrivenTicks;

    void FixedUpdate()
    {
        if (batchDrivenTicks) return;
        SimulationTick();
    }

    /// <summary>
    /// Один крок GNC+RK4. FixedUpdate у грі; Monte-Carlo викликає burst
    /// (high timeScale інакше впирається в ліміт FixedUpdate/frame → усі timeout 0%).
    /// </summary>
    public void SimulationTick()
    {
        if (!simulationArmed) return;
        if (simulationPaused) return;
        if (state.isLanded || state.simulationFinished) return;
        if (parameters == null) return;

        float dt = parameters.fixedTimeStep;
        if (state.position.y > maxHeightRecorded) maxHeightRecorded = state.position.y;

        currentTime += dt;
        state.time = currentTime;

        if (currentTime >= parameters.maxSimulationTime)
        {
            FinishLanding(timeout: true);
            return;
        }

        if (phase == FlightPhase.Stack)
        {
            UpdateStackControl();
            RungeKutta4Step(dt);
            CheckSeparation();
        }
        else
        {
            // A–D лише після відділення (Stage1)
            navigation.Step(state, lastAccel, dt, NavNoiseScale);
            UpdateControl();
            RungeKutta4Step(dt);
            lastAccel = CalculateAccelerationAt(state.position, state.velocity);
        }

        // Тримати physics origin на/над поверхнею pad (верх палуби ≈ PadSurfaceY)
        float ground = EnvironmentBuilder.PadSurfaceY;
        if (state.position.y < ground)
            state.position.y = ground;

        ClampToTerrainDisk();
        SyncTransformWithState();
        // Batch Monte-Carlo: пропускати logger/trajectory — вони домінують у CPU і раніше тягнули trial у timeout → 0%
        if (!batchDrivenTicks)
        {
            if (logger != null) logger.Log(state);
            if (cachedVisualizer == null)
                cachedVisualizer = FindAnyObjectByType<TrajectoryVisualizer>();
            cachedVisualizer?.SampleFlight(force: false);
        }

        // Touchdown лише у фазі посадки 1-го ступеня
        if (phase == FlightPhase.Stage1 && state.position.y <= ground + 0.04f)
            FinishLanding(timeout: false);
    }

    void UpdateControl()
    {
        if (phase != FlightPhase.Stage1) return;

        float dt = parameters != null ? parameters.fixedTimeStep : Time.fixedDeltaTime;
        // Без nav-шуму Ideal/clean: GNC на істинному plant (complementary filter
        // інакше лагає → lateral «жене» фантом і ступінь іде вбік на півдорозі).
        bool cleanNav = NavNoiseScale < 1e-5f;
        var ctx = (!cleanNav && navigation.Current.valid)
            ? navigation.ToContext(state, dt)
            : ControlContext.FromState(state, dt);
        float h = ctx.Height;
        float mass = ctx.Mass;
        float tilt = ctx.TiltDeg;

        // Термінал: сильніший rate-damp upright (анти-раскачка перед touchdown)
        bool terminal = h < 45f;
        bool idealTerm = terminal && IdealLandingPresets.Active;
        float attKp = idealTerm ? 0.48f : (terminal ? 0.55f : 0.7f);
        float attKd = idealTerm ? 1.55f : (terminal ? 1.35f : 0.92f);
        float attMax = idealTerm ? 8f : (terminal ? 10f : 16f);
        Vector3 baseGimbal = SoftLandingGuidance.AttitudeGimbal(
            ctx.Rotation, ctx.AngularVelocity, maxDeg: attMax, kp: attKp, kd: attKd);

        if (controllerResolver == null)
            controllerResolver = LandingControllerResolver.CreateDefault(this, pidStrategy);

        ILandingController strategy = controllerResolver.Resolve(controlMode);
        ControlCommand cmd = strategy != null
            ? strategy.Evaluate(in ctx)
            : ControlCommand.ProfileFallback(in ctx);

        float thrustCmd = cmd.Thrust;
        float gBlend = cmd.GimbalBlend;
        // Terminal: hand attitude to upright PD (strategy gimbal rocked the stage)
        bool idealLike = IdealLandingPresets.Active; // MC uses cleanNav≠Ideal; only [I] softens terminal
        float fadeH = idealLike ? 80f : 55f;
        if (h < fadeH) gBlend *= Mathf.Clamp01(h / fadeH);
        if (h < (idealLike ? 22f : 15f)) gBlend = 0f;
        Vector3 gCmd = Vector3.Lerp(baseGimbal, cmd.GimbalEuler, gBlend);

        // Великий нахил — пріоритет вирівнювання, але не душимо посадковий burn
        float upright = SoftLandingGuidance.UprightThrustScale(tilt);
        thrustCmd *= upright;
        if (tilt > 34f || (IdealLandingPresets.Active && terminal && tilt > 12f))
        {
            gCmd = baseGimbal;
            float hover = mass * AtmosphereModel.GetGravity(h);
            // Allow braking (1.05–2.1 × hover) instead of hover-only
            thrustCmd = Mathf.Clamp(thrustCmd, hover * 1.05f, hover * 2.1f);
        }

        float gLim = terminal ? 8f : 16f;
        gCmd.x = Mathf.Clamp(gCmd.x, -gLim, gLim);
        gCmd.y = 0f;
        gCmd.z = Mathf.Clamp(gCmd.z, -gLim, gLim);
        state.thrustDirection = (Quaternion.Euler(gCmd) * Vector3.up).normalized;

        // Бічне: Ideal/clean м’якше; нижче ~12 м — лише гасіння Vh / off
        if (tilt < 30f && h > 1.2f)
        {
            float lat = cmd.LateralScale;
            // Ideal softens; Monte-Carlo keeps lateral authority (jitter/wind recoverable)
            if (IdealLandingPresets.Active)
            {
                lat = Mathf.Min(lat, 0.85f);
                if (terminal) lat *= Mathf.Lerp(0.12f, 0.75f, Mathf.Clamp01(h / 45f));
                if (h < 12f) lat *= 0.35f;
            }
            else if (terminal)
            {
                lat *= Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(h / 45f));
            }
            ApplyLateralGuidance(lat, in ctx);
        }

        state.currentThrust = Mathf.Clamp(thrustCmd, 0f, state.maxThrust);
        if (state.currentFuelMass <= 0f) state.currentThrust = 0f;
    }

    /// <summary>
    /// Бічне наведення до pad. Tail TVC: lean TOWARD pad
    /// (x&gt;0 ⇒ gz&lt;0 ⇒ td.x&gt;0 ⇒ τz&gt;0 ⇒ lean to −X).
    /// td = R(gx,0,gz)·up ⇒ td.x≈−sin(gz), td.z≈sin(gx).
    /// gainScale raises kVel (Hybrid damps Vh), not lean angle.
    /// Shared lean envelope lim = 8.5° (terminal 2.8° at h&lt;20).
    /// </summary>
    void ApplyLateralGuidance(float gainScale, in ControlContext ctx)
    {
        float h = Mathf.Max(0f, ctx.Height);
        if (h > 2800f || h < 1.2f) return;

        float tilt = Vector3.Angle(state.rotation * Vector3.up, Vector3.up);
        if (tilt > 30f) return;

        float scale = Mathf.Clamp(gainScale, 0.35f, 1.6f);
        float shape = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Clamp01(h / 2200f));
        float fade = Mathf.Lerp(0.55f, 1.05f, shape);

        float px = ctx.PositionX;
        float pz = ctx.PositionZ;
        float vx = ctx.VelX;
        float vz = ctx.VelZ;
        float miss = Mathf.Sqrt(px * px + pz * pz);
        float vh = Mathf.Sqrt(vx * vx + vz * vz);

        // PD: mid-course позиція; термінал — майже лише гасіння Vh (анти-раскачка)
        // Ideal: freeze position lean early (anti-rock). MC: keep kPos until near pad
        // (freezing at 30 m with DefenseBaseline jitter/wind caused universal 0% success).
        bool ideal = IdealLandingPresets.Active;
        float kPos = (ideal ? 0.16f : 0.22f) * fade;
        float kVel = (ideal ? 0.85f : 0.95f) * fade * scale;
        if (h < 700f) { kPos *= ideal ? 1.15f : 1.3f; kVel *= ideal ? 1.25f : 1.4f; }
        if (h < 250f) { kPos *= ideal ? 1.15f : 1.25f; kVel *= ideal ? 1.35f : 1.5f; }
        if (h < 80f)  { kPos *= ideal ? 0.7f : 0.9f;  kVel *= ideal ? 1.5f : 2.0f; }
        if (ideal)
        {
            if (h < 40f)  { kPos *= 0.35f; kVel *= 1.25f; }
            if (h < 30f)  { kPos = 0f;     kVel *= 0.95f; }
            if (h < 14f)  { kPos = 0f;     kVel *= 0.7f; }
            if (h < 6f)   { kPos = 0f;     kVel *= 0.35f; }
        }
        else
        {
            if (h < 40f)  { kPos *= 0.55f; kVel *= 1.6f; }
            if (h < 25f)  { kPos *= 0.5f;  kVel *= 2.0f; }
            if (h < 12f)  { kPos *= 0.35f; kVel *= 1.8f; }
            if (h < 5f)   { kPos = 0f;     kVel *= 0.5f; }
        }

        if (miss > 40f && h > (ideal ? 40f : 20f)) kPos *= ideal ? 1.2f : 1.35f;
        if (vh > 8f && (!ideal || h > 25f)) kVel *= ideal ? 1.15f : 1.25f;

        float lim = ideal ? 6.0f : 8.5f;
        if (ideal)
        {
            if (h < 40f) lim = 3.2f;
            if (h < 18f) lim = 1.6f;
            if (h < 8f)  lim = 0.8f;
            if (tilt > 10f) lim *= Mathf.Lerp(1f, 0.45f, (tilt - 10f) / 18f);
            lim = Mathf.Min(lim, h < 40f ? 2.2f : 4.5f);
        }
        else
        {
            if (h < 20f) lim = 2.8f;
            if (tilt > 12f) lim *= Mathf.Lerp(1f, 0.5f, (tilt - 12f) / 16f);
        }

        // Tail TVC lean TOWARD pad (x>0 ⇒ gz<0 ⇒ td.x>0 ⇒ τz>0 ⇒ lean to −X)
        float gx = Mathf.Clamp(+(kPos * pz + kVel * vz), -lim, lim);
        float gz = Mathf.Clamp(-(kPos * px + kVel * vx), -lim, lim);

        Vector3 td = state.thrustDirection.normalized;
        float curX = Mathf.Atan2(td.z, Mathf.Max(1e-4f, td.y)) * Mathf.Rad2Deg;
        float curZ = Mathf.Atan2(-td.x, Mathf.Max(1e-4f, td.y)) * Mathf.Rad2Deg;
        // Термінал: більше upright PD, менше lateral lean
        float uprightKeep;
        float cmdCap;
        if (ideal)
        {
            uprightKeep = h < 40f ? Mathf.Lerp(0.85f, 0.45f, Mathf.Clamp01(h / 40f))
                                 : (h < 80f ? 0.28f : 0.12f);
            cmdCap = h < 18f ? 6f : 14f;
        }
        else
        {
            // MC: leave room for lateral TVC (HEAD-like); Ideal keeps stronger upright
            uprightKeep = h < 30f ? 0.35f : 0.12f;
            cmdCap = 18f;
        }
        float nx = Mathf.Clamp(curX * uprightKeep + gx, -cmdCap, cmdCap);
        float nz = Mathf.Clamp(curZ * uprightKeep + gz, -cmdCap, cmdCap);
        state.thrustDirection = (Quaternion.Euler(nx, 0f, nz) * Vector3.up).normalized;
    }

    void RungeKutta4Step(float dt)
    {
        Vector3 k1v = state.velocity;
        Vector3 k1a = CalculateAccelerationAt(state.position, state.velocity);

        Vector3 k2v = state.velocity + k1a * (dt * 0.5f);
        Vector3 k2a = CalculateAccelerationAt(state.position + k1v * (dt * 0.5f), k2v);

        Vector3 k3v = state.velocity + k2a * (dt * 0.5f);
        Vector3 k3a = CalculateAccelerationAt(state.position + k2v * (dt * 0.5f), k3v);

        Vector3 k4v = state.velocity + k3a * dt;
        Vector3 k4a = CalculateAccelerationAt(state.position + k3v * dt, k4v);

        state.velocity += (k1a + 2f * k2a + 2f * k3a + k4a) * (dt / 6f);
        state.position += (k1v + 2f * k2v + 2f * k3v + k4v) * (dt / 6f);

        if (state.currentFuelMass > 0f && state.currentThrust > 0f)
        {
            float massFlow = state.currentThrust / (parameters.isp * G0);
            state.currentFuelMass = Mathf.Max(0f, state.currentFuelMass - massFlow * dt);
        }

        // момент у body-ish frame від offset вектора тяги
        Vector3 localTorque = new Vector3(-state.thrustDirection.z, 0f, state.thrustDirection.x)
                              * state.currentThrust * LeverArm;
        localTorque -= state.angularVelocity * AngularDamping;
        float I = Mathf.Max(1f, state.TotalMass * InertiaFactor);
        Vector3 angularAcceleration = localTorque / I;

        state.angularVelocity += angularAcceleration * dt;
        // Обмеження ω — анти-перекид; біля землі жорсткіше (анти-раскачка)
        float wMax = state.position.y < 40f ? MaxOmegaTerminal : MaxOmega;
        if (state.position.y < 15f) wMax = 0.22f;
        if (state.angularVelocity.sqrMagnitude > wMax * wMax)
            state.angularVelocity = state.angularVelocity.normalized * wMax;

        float wMag = state.angularVelocity.magnitude;
        if (wMag > 1e-8f)
        {
            float deg = wMag * dt * Mathf.Rad2Deg;
            state.rotation = Quaternion.Normalize(
                state.rotation * Quaternion.AngleAxis(deg, state.angularVelocity / wMag));
        }
    }

    Vector3 CalculateAccelerationAt(Vector3 pos, Vector3 vel)
    {
        Vector3 acc = Vector3.zero;
        acc.y -= AtmosphereModel.GetGravity(pos.y);

        Vector3 td = state.thrustDirection.sqrMagnitude > 1e-6f
            ? state.thrustDirection.normalized : Vector3.up;
        Vector3 thrustWorld = state.rotation * td * state.currentThrust;
        acc += thrustWorld / Mathf.Max(1f, state.TotalMass);

        // опір відносно повітря (вкл. вітер)
        Vector3 airRel = vel - (applyContinuousWind ? windVelocity : Vector3.zero);
        float density = AtmosphereModel.GetDensity(pos.y);
        float dragMag = 0.5f * density * airRel.sqrMagnitude * Cd * RefArea;
        if (airRel.sqrMagnitude > 0.01f)
            acc -= airRel.normalized * (dragMag / Mathf.Max(1f, state.TotalMass));

        return acc;
    }

    void FinishLanding(bool timeout)
    {
        if (!timeout)
            // Сісти на палубу pad — ноги ~0.06 м над origin, поверхня на PadSurfaceY
            state.position.y = EnvironmentBuilder.PadSurfaceY;

        state.isLanded = true;
        state.simulationFinished = true;

        metrics.touchdownVelocity = Mathf.Abs(state.velocity.y);
        metrics.horizontalMiss = new Vector2(state.position.x, state.position.z).magnitude;
        metrics.horizontalSpeed = new Vector2(state.velocity.x, state.velocity.z).magnitude;
        metrics.landingAngleError = Vector3.Angle(state.rotation * Vector3.up, Vector3.up);
        metrics.fuelRemaining = state.currentFuelMass;
        metrics.maxAltitude = maxHeightRecorded;
        metrics.totalFlightTime = state.time;
        metrics.timedOut = timeout;

        // Єдине джерело істини для soft-landing gate (Domain/LandingCriteria)
        LandingCriteria.ApplySuccessFlag(metrics, parameters);

        state.velocity = Vector3.zero;
        state.angularVelocity = Vector3.zero;

        bool batch = batchDrivenTicks
            || FindAnyObjectByType<SimulationManager>() is { IsExperimentRunning: true };

        if (!batch)
        {
            if (logger != null) logger.Save();

            string algorithm = controlMode switch
            {
                ControlMode.Fuzzy => "Fuzzy Logic (Sugeno-0)",
                ControlMode.Neural => "Neural Network (ES 1+λ)",
                ControlMode.Hybrid => "Hybrid Neuro-Fuzzy",
                _ => "PID"
            };
            metrics.PrintResults(algorithm);

            if (cachedVisualizer == null)
                cachedVisualizer = FindAnyObjectByType<TrajectoryVisualizer>();
            // Фіналізувати лінію; видимість НЕ форсувати — лишається як виставив UI (Start не вмикає сам)
            cachedVisualizer?.OnSimulationFinished(metrics.isSuccessfulLanding);

            if ((controlMode == ControlMode.Neural || controlMode == ControlMode.Hybrid)
                && neuralController != null)
            {
                neuralController.Train(
                    metrics.touchdownVelocity,
                    metrics.landingAngleError,
                    metrics.fuelRemaining,
                    metrics.horizontalMiss);
            }

            MissionControlUI.Instance?.ShowLandingResult(metrics);
        }
    }

    /// <summary>Останній шлях CSV-логу (якщо є).</summary>
    public string GetLastTrajectoryPath()
    {
        if (logger == null) logger = GetComponent<DataLogger>();
        return logger != null ? logger.LastFilePath : null;
    }

    /// <summary>Повне перезавантаження та старт (Stack→sep→Stage1 або одразу Stage1).</summary>
    public void ResetSimulation()
    {
        state.isLanded = false;
        state.simulationFinished = false;
        currentTime = 0f;
        maxHeightRecorded = 0f;
        metrics = new LandingMetrics();
        windVelocity = Vector3.zero;
        applyContinuousWind = true;
        simulationArmed = true;
        simulationPaused = false;
        // MC завжди лише ділянка посадки
        if (batchDrivenTicks) skipStackPhase = true;

        controllerResolver?.ResetAll();
        pidStrategy.ResetSession();

        SyncFixedTimestep();
        InitializeSimulation();
        if (!batchDrivenTicks)
        {
            if (logger != null) logger.Initialize();
            if (cachedVisualizer == null)
                cachedVisualizer = FindAnyObjectByType<TrajectoryVisualizer>();
            cachedVisualizer?.Clear();
            MissionControlUI.Instance?.HideLandingResult();
            SnapCamera();
        }
        else
        {
            if (logger != null) logger.Initialize();
        }
    }

    /// <summary>
    /// Збурення для посадки 1-го ступеня (вітер / шум).
    /// Якщо ще фаза Stack — відкладаються до відділення (не діють на пакет).
    /// Ideal: windStrength=0, randomize=false.
    /// </summary>
    public void ApplyFlightDisturbances(
        float windStrength,
        bool randomize,
        float massVariationPercent = 6f,
        float angleVariationDegrees = 7f,
        float positionJitterMeters = -1f)
    {
        pendingWindStrength = Mathf.Max(0f, windStrength);
        pendingRandomize = randomize;
        pendingMassVar = massVariationPercent;
        pendingAngleVar = angleVariationDegrees;
        // Jitter лише при randomize; default 18 м ламав Ideal, якщо викликали з noise=on випадково
        pendingJitter = positionJitterMeters >= 0f
            ? positionJitterMeters
            : (randomize ? 18f : 0f);
        hasPendingDisturbances = true;

        if (phase == FlightPhase.Stage1)
            ApplyPendingDisturbancesIfAny();
        else
        {
            // Під час Stack збурення вимкнені
            windVelocity = Vector3.zero;
            applyContinuousWind = false;
        }
    }

    void ApplyPendingDisturbancesIfAny()
    {
        if (!hasPendingDisturbances) return;
        hasPendingDisturbances = false;

        float windStrength = pendingWindStrength;
        applyContinuousWind = windStrength > 0.05f;

        if (windStrength > 0.05f)
        {
            Vector3 kick = new Vector3(
                SimRng.Range(-windStrength, windStrength),
                0f,
                SimRng.Range(-windStrength * 0.55f, windStrength * 0.55f));
            windVelocity = kick * 0.1f;
            state.velocity += kick * 0.45f;
        }
        else
        {
            windVelocity = Vector3.zero;
        }

        if (pendingRandomize)
        {
            float massNoise = 1f + SimRng.Range(-pendingMassVar, pendingMassVar) / 100f;
            state.currentFuelMass = Mathf.Max(500f, state.currentFuelMass * massNoise);

            float ax = SimRng.Range(-pendingAngleVar, pendingAngleVar);
            float az = SimRng.Range(-pendingAngleVar, pendingAngleVar);
            state.rotation = Quaternion.Normalize(state.rotation * Quaternion.Euler(ax, 0f, az));

            if (pendingJitter > 0.1f)
            {
                state.position.x += SimRng.Range(-pendingJitter, pendingJitter);
                state.position.z += SimRng.Range(-pendingJitter, pendingJitter);
            }
        }

        SyncTransformWithState();
        AlignNavigationToTruth();
    }

    /// <summary>Лише вибір режиму — без старту. Показує пакет на pad (idle Stack visual).</summary>
    public void PrepareMode(ControlMode mode)
    {
        controlMode = mode;
        state.isLanded = false;
        state.simulationFinished = false;
        currentTime = 0f;
        maxHeightRecorded = 0f;
        metrics = new LandingMetrics();
        windVelocity = Vector3.zero;
        simulationArmed = false;
        simulationPaused = false;
        // skipStackPhase зберігається (Ideal/MC можуть лишити landing-only)
        hasSeparated = false;
        hasPendingDisturbances = false;
        phase = FlightPhase.Idle;

        controllerResolver?.ResetAll();
        pidStrategy.ResetSession();

        SyncFixedTimestep();
        if (parameters != null)
        {
            parameters.EnsureStackDefaults();
            state.position = parameters.startPosition;
            // Прев’ю IC: швидкість старту (не zero — Ideal/landing IC мають Vу)
            state.velocity = parameters.startVelocity;
            state.rotation = Quaternion.Euler(parameters.startEulerAngles);
            state.angularVelocity = Vector3.zero;
            state.dryMass = parameters.dryMass;
            state.currentFuelMass = parameters.fuelMass;
            state.maxThrust = parameters.maxThrust;
            state.currentThrust = 0f;
            state.thrustDirection = Vector3.up;
            state.time = 0f;
            RocketVisualBuilder.SetUpperStackVisible(transform, false);
            SyncTransformWithState();
            AlignNavigationToTruth();
        }
        if (logger != null) logger.Initialize();
        if (cachedVisualizer == null)
            cachedVisualizer = FindAnyObjectByType<TrajectoryVisualizer>();
        cachedVisualizer?.Clear();
        MissionControlUI.Instance?.HideLandingResult();
        SnapCamera();
    }

    /// <summary>Підпис середовища для UI/експорту.</summary>
    public static string EnvironmentLabel => "Earth LZ · Stage-1 · after separation";

    /// <summary>
    /// Примусове завершення з метриками (для Monte-Carlo timeout / STOP з оцінкою).
    /// </summary>
    public void ForceFinish(bool asTimeout)
    {
        if (state.simulationFinished) return;
        FinishLanding(timeout: asTimeout);
    }

    /// <summary>
    /// Зупинити політ (кнопка СТОП). Ракета замирає; режим не змінюється.
    /// </summary>
    public void StopSimulation(bool keepPosition = true)
    {
        simulationArmed = false;
        simulationPaused = false;
        state.simulationFinished = true;
        state.isLanded = true;
        state.currentThrust = 0f;
        state.velocity = Vector3.zero;
        state.angularVelocity = Vector3.zero;

        if (!keepPosition)
        {
            // Повернутись на стартову висоту pad для наступного запуску
            InitializeSimulation();
            state.simulationFinished = false;
            state.isLanded = false;
        }
        else
        {
            SyncTransformWithState();
        }

        Time.timeScale = 1f;
        // keepPosition (UI STOP): stay on stage — do not SnapNow while Overview
        // (ComputeFraming lookAt is pad-biased and yanks the camera to the LZ).
        if (keepPosition)
            StayOnRocketCamera();
        else
            SnapCamera();
    }

    static void StayOnRocketCamera()
    {
        var cam = FindAnyObjectByType<CameraFollow>();
        cam?.StayOnRocketAfterAbort();
    }

    static void SnapCamera()
    {
        var cam = FindAnyObjectByType<CameraFollow>();
        cam?.SnapNow();
    }

    /// <summary>
    /// Тримає горизонтальну позицію всередині земного диска LZ (м'який відскік).
    /// </summary>
    void ClampToTerrainDisk()
    {
        float maxR = LunarTerrainMesh.TerrainRadius * 0.92f;
        float hx = state.position.x;
        float hz = state.position.z;
        float r2 = hx * hx + hz * hz;
        float maxR2 = maxR * maxR;
        if (r2 <= maxR2) return;

        float r = Mathf.Sqrt(r2);
        float s = maxR / r;
        state.position.x = hx * s;
        state.position.z = hz * s;

        // Гасимо радіальну складову швидкості назовні
        Vector3 radial = new Vector3(state.position.x, 0f, state.position.z).normalized;
        float vOut = Vector3.Dot(state.velocity, radial);
        if (vOut > 0f)
            state.velocity -= radial * vOut;
    }

    public void AlignNavigationToTruth()
    {
        if (state == null) return;
        navigation.Reset(state, NavSeed);
        lastAccel = CalculateAccelerationAt(state.position, state.velocity);
    }

    public void SyncTransformWithState()
    {
        transform.position = state.position;
        transform.rotation = state.rotation;
    }

    public string GetModeDisplayName()
    {
        return controlMode switch
        {
            ControlMode.Fuzzy => "Нечітка логіка (Sugeno)",
            ControlMode.Neural => "Нейромережа (ES)",
            ControlMode.Hybrid => "Гібрид Neuro-Fuzzy",
            _ => "Класичний PID"
        };
    }
}
