using UnityEngine;

/// <summary>
/// Ядро фізики та GNC ракетоносія.
/// — Трансляція: RK4; орієнтація: semi-implicit Euler + демпфінг;
/// — Режими A–D: PID / Fuzzy Sugeno / Neural ES / Hybrid (різні закони керування);
/// — Спільне: TVC-PD, lateral guidance, термінал soft-landing (h&lt;25 м);
/// — Старт лише після simulationArmed (UI / Ideal presets не автозапуск).
/// </summary>
[RequireComponent(typeof(DataLogger))]
public class RocketPhysics : MonoBehaviour
{
    [Header("Основні параметри")]
    public SimulationParameters parameters;
    /// <summary>Алгоритм керування (A–D у UI).</summary>
    public enum ControlMode { PID, Fuzzy, Neural, Hybrid }

    [Header("Режим керування")]
    public ControlMode controlMode = ControlMode.Hybrid;
    /// <summary>Поточний фізичний стан (єдине джерело правди для камери/UI).</summary>
    public RocketState state = new RocketState();

    [Header("Запуск")]
    [Tooltip("false = ракета чекає кнопки «Запустити посадку»")]
    public bool simulationArmed = false;

    [Header("Зовнішні збурення")]
    public Vector3 windVelocity = Vector3.zero;
    public bool applyContinuousWind = true;

    private DataLogger logger;
    // Класичний PID: слабший integral — менше windup, але гірше за Hybrid під збуреннями
    private PIDController pitchPID = new PIDController() { Kp = 0.55f, Ki = 0.04f, Kd = 0.48f };
    private PIDController yawPID = new PIDController() { Kp = 0.55f, Ki = 0.04f, Kd = 0.48f };
    private PIDController thrustPID = new PIDController() { Kp = 2.8f, Ki = 0.25f, Kd = 1.4f };

    /// <summary>Налаштування PID з IdealLandingPresets / UI.</summary>
    public void SetPidGains(float thrustKp, float thrustKi, float thrustKd,
        float attKp, float attKi, float attKd)
    {
        thrustPID.Kp = thrustKp; thrustPID.Ki = thrustKi; thrustPID.Kd = thrustKd;
        pitchPID.Kp = attKp; pitchPID.Ki = attKi; pitchPID.Kd = attKd;
        yawPID.Kp = attKp; yawPID.Ki = attKi; yawPID.Kd = attKd;
    }

    public FuzzyLandingController fuzzyController;
    public NeuralController neuralController;
    public HybridController hybridController;
    public LandingMetrics metrics = new LandingMetrics();

    private float maxHeightRecorded;
    private float currentTime;
    private TrajectoryVisualizer cachedVisualizer;

    const float LeverArm = 11f;
    const float AngularDamping = 980000f; // сильніше — без mid-flight PIO
    const float InertiaFactor = 55f;
    const float MaxOmega = 0.85f; // rad/s
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

        if (neuralController != null) neuralController.LoadBestWeights();
        cachedVisualizer = FindFirstObjectByType<TrajectoryVisualizer>();

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
        state.position = parameters.startPosition;
        state.velocity = parameters.startVelocity;
        state.rotation = Quaternion.Euler(parameters.startEulerAngles);
        state.angularVelocity = Vector3.zero;
        state.dryMass = parameters.dryMass;
        state.currentFuelMass = parameters.fuelMass;
        state.maxThrust = parameters.maxThrust;
        state.currentThrust = 0f;
        state.thrustDirection = Vector3.up;
        state.time = 0f;
        SyncTransformWithState();
    }

    void FixedUpdate()
    {
        if (!simulationArmed) return;
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

        UpdateControl();
        RungeKutta4Step(dt);

        // Не дозволяємо «провалитись» під pad
        if (state.position.y < 0f)
            state.position.y = 0f;

        SyncTransformWithState();
        if (logger != null) logger.Log(state);

        if (state.position.y <= 0.05f)
            FinishLanding(timeout: false);
    }

    void UpdateControl()
    {
        Vector3 up = state.rotation * Vector3.up;
        float pitchError = Vector3.SignedAngle(up, Vector3.up, Vector3.right);
        float yawError = Vector3.SignedAngle(up, Vector3.up, Vector3.forward);
        float pitchRate = state.angularVelocity.x * Mathf.Rad2Deg;
        float yawRate = state.angularVelocity.z * Mathf.Rad2Deg;
        float horizSpeed = new Vector2(state.velocity.x, state.velocity.z).magnitude;
        float tilt = Vector3.Angle(up, Vector3.up);
        float h = state.position.y;
        float mass = state.TotalMass;

        // Базовий PD (TVC) — safety net для всіх режимів
        Vector3 baseGimbal = SoftLandingGuidance.AttitudeGimbal(
            state.rotation, state.angularVelocity, maxDeg: 16f, kp: 0.7f, kd: 0.92f);
        Vector3 gCmd = baseGimbal;
        float thrustCmd = SoftLandingGuidance.ProfileThrust(h, state.velocity.y, mass);

        switch (controlMode)
        {
            case ControlMode.Fuzzy when fuzzyController != null && fuzzyController.isActive:
            {
                // B: Sugeno thrust + fuzzy gimbal (помітна різниця від PID)
                thrustCmd = fuzzyController.CalculateThrust(h, state.velocity.y, mass);
                Vector3 fg = fuzzyController.CalculateGimbal(pitchError, yawError, pitchRate, yawRate);
                gCmd = Vector3.Lerp(baseGimbal, fg, 0.55f);
                break;
            }
            case ControlMode.Neural when neuralController != null && neuralController.isActive:
            {
                // C: MLP residual thrust + limited gimbal bias
                neuralController.CalculateControl(
                    h, state.velocity.y, mass, state.currentThrust,
                    pitchError, yawError, horizSpeed,
                    out thrustCmd, out Vector3 ng);
                gCmd = Vector3.Lerp(baseGimbal, ng, 0.4f);
                break;
            }
            case ControlMode.Hybrid when hybridController != null && hybridController.isActive:
            {
                // D: Neuro-Fuzzy — fuzzy base + NN residual
                hybridController.CalculateControl(
                    h, state.velocity.y, mass, state.currentThrust,
                    pitchError, yawError, pitchRate, yawRate, horizSpeed,
                    out thrustCmd, out Vector3 hg);
                gCmd = Vector3.Lerp(baseGimbal, hg, 0.5f);
                break;
            }
            default:
            {
                // A: класичний PID (тяга + attitude) — еталон, гірший під збуреннями
                thrustCmd = CalculateThrustPID();
                float pc = pitchPID.Calculate(0f, pitchError, parameters.fixedTimeStep);
                float yc = yawPID.Calculate(0f, yawError, parameters.fixedTimeStep);
                gCmd = new Vector3(
                    Mathf.Clamp(baseGimbal.x + pc * 0.35f, -16f, 16f),
                    0f,
                    Mathf.Clamp(baseGimbal.z + yc * 0.35f, -16f, 16f));
                break;
            }
        }

        // Великий нахил — пріоритет вирівнювання
        float upright = SoftLandingGuidance.UprightThrustScale(tilt);
        thrustCmd *= upright;
        if (tilt > 20f)
        {
            gCmd = baseGimbal;
            float hover = mass * AtmosphereModel.GetGravity(h);
            thrustCmd = Mathf.Clamp(thrustCmd, hover * 0.65f, hover * 1.3f);
        }

        gCmd.x = Mathf.Clamp(gCmd.x, -16f, 16f);
        gCmd.y = 0f;
        gCmd.z = Mathf.Clamp(gCmd.z, -16f, 16f);
        state.thrustDirection = (Quaternion.Euler(gCmd) * Vector3.up).normalized;

        // Бічне наведення: Hybrid/Fuzzy сильніші за PID (реалістична різниця)
        if (tilt < 12f && h < 1000f)
        {
            float latScale = controlMode switch
            {
                ControlMode.Hybrid => 1.15f,
                ControlMode.Fuzzy => 1.0f,
                ControlMode.Neural => 0.85f,
                _ => 0.55f // PID — слабке бічне
            };
            ApplyLateralGuidance(latScale);
        }

        state.currentThrust = Mathf.Clamp(thrustCmd, 0f, state.maxThrust);
        if (state.currentFuelMass <= 0f) state.currentThrust = 0f;
    }

    /// <summary>
    /// Бічне наведення до pad. Знаки TVC:
    /// td = R(gx,0,gz)·up ⇒ td.x≈−sin(gz), td.z≈sin(gx).
    /// Щоб тягнути до −x (коли x&gt;0): td.x&lt;0 ⇒ gz&gt;0.
    /// Щоб тягнути до −z (коли z&gt;0): td.z&lt;0 ⇒ gx&lt;0.
    /// </summary>
    void ApplyLateralGuidance(float gainScale = 1f)
    {
        float h = Mathf.Max(0f, state.position.y);
        if (h > 900f || h < 8f) return;

        float tilt = Vector3.Angle(state.rotation * Vector3.up, Vector3.up);
        if (tilt > 12f) return;

        float fade = Mathf.SmoothStep(0f, 1f, 1f - h / 900f) * Mathf.Clamp(gainScale, 0.2f, 1.4f);
        float kPos = 0.014f * fade;
        float kVel = 0.06f * fade;
        if (h < 120f)
        {
            kPos *= 0.85f;
            kVel *= 1.1f;
        }

        float lim = 5f * Mathf.Clamp(gainScale, 0.4f, 1.3f);
        float gx = Mathf.Clamp(-(kPos * state.position.z + kVel * state.velocity.z), -lim, lim);
        float gz = Mathf.Clamp(+(kPos * state.position.x + kVel * state.velocity.x), -lim, lim);

        Vector3 td = state.thrustDirection.normalized;
        float curX = Mathf.Atan2(td.z, Mathf.Max(1e-4f, td.y)) * Mathf.Rad2Deg;
        float curZ = Mathf.Atan2(-td.x, Mathf.Max(1e-4f, td.y)) * Mathf.Rad2Deg;
        float nx = Mathf.Clamp(curX + gx, -14f, 14f);
        float nz = Mathf.Clamp(curZ + gz, -14f, 14f);
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

        // torque in body-ish frame from thrust vector offset
        Vector3 localTorque = new Vector3(-state.thrustDirection.z, 0f, state.thrustDirection.x)
                              * state.currentThrust * LeverArm;
        localTorque -= state.angularVelocity * AngularDamping;
        float I = Mathf.Max(1f, state.TotalMass * InertiaFactor);
        Vector3 angularAcceleration = localTorque / I;

        state.angularVelocity += angularAcceleration * dt;
        // Обмеження ω — анти-перекид
        if (state.angularVelocity.sqrMagnitude > MaxOmega * MaxOmega)
            state.angularVelocity = state.angularVelocity.normalized * MaxOmega;

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

        // drag relative to air (incl. wind)
        Vector3 airRel = vel - (applyContinuousWind ? windVelocity : Vector3.zero);
        float density = AtmosphereModel.GetDensity(pos.y);
        float dragMag = 0.5f * density * airRel.sqrMagnitude * Cd * RefArea;
        if (airRel.sqrMagnitude > 0.01f)
            acc -= airRel.normalized * (dragMag / Mathf.Max(1f, state.TotalMass));

        return acc;
    }

    /// <summary>
    /// Класичний вертикальний PID: hover FF + PID на v_target.
    /// Слабший термінал ніж у Fuzzy/Hybrid — реалістично гірший під вітром.
    /// </summary>
    float CalculateThrustPID()
    {
        float h = Mathf.Max(0f, state.position.y);
        float mass = state.TotalMass;
        float g = AtmosphereModel.GetGravity(h);
        float hover = mass * g;
        float target = SoftLandingGuidance.TargetDescentRate(h);
        float pid = thrustPID.Calculate(target, state.velocity.y, parameters.fixedTimeStep);
        float thrust = hover + pid * 16000f;
        thrust = Mathf.Clamp(thrust, hover * 0.15f, state.maxThrust);

        // Термінал м’якший і пізніший — PID частіше «промахує» soft contact під збуреннями
        if (h < 12f)
        {
            float profile = SoftLandingGuidance.ProfileThrust(h, state.velocity.y, mass);
            float t = 1f - Mathf.Clamp01(h / 12f);
            thrust = Mathf.Lerp(thrust, profile, t * 0.7f);
        }
        return thrust;
    }

    void FinishLanding(bool timeout)
    {
        if (!timeout)
            state.position.y = 0f;

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

        // Захист: якщо asset не серіалізував критерії (0), беремо номінал
        float maxV = parameters != null && parameters.maxTouchdownVelocity > 0.1f
            ? parameters.maxTouchdownVelocity : 3.5f;
        float maxA = parameters != null && parameters.maxLandingAngle > 0.1f
            ? parameters.maxLandingAngle : 7f;
        float maxM = parameters != null && parameters.maxHorizontalMiss > 0.1f
            ? parameters.maxHorizontalMiss : 25f;
        float maxH = parameters != null && parameters.maxHorizontalSpeed > 0.1f
            ? parameters.maxHorizontalSpeed : 5f;
        metrics.isSuccessfulLanding = !timeout
            && metrics.touchdownVelocity < maxV
            && metrics.landingAngleError < maxA
            && metrics.horizontalMiss < maxM
            && metrics.horizontalSpeed < maxH;

        state.velocity = Vector3.zero;
        state.angularVelocity = Vector3.zero;

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
            cachedVisualizer = FindFirstObjectByType<TrajectoryVisualizer>();
        // Лінія траєкторії лишається видимою після посадки (Clear лише на новий старт)
        cachedVisualizer?.OnSimulationFinished(metrics.isSuccessfulLanding);
        if (cachedVisualizer != null)
            cachedVisualizer.SetVisible(true);

        // Don't train during batch Monte-Carlo (SimulationManager sets timeScale high)
        bool batch = FindFirstObjectByType<SimulationManager>() is { IsExperimentRunning: true };
        if (!batch && (controlMode == ControlMode.Neural || controlMode == ControlMode.Hybrid)
            && neuralController != null)
        {
            neuralController.Train(
                metrics.touchdownVelocity,
                metrics.landingAngleError,
                metrics.fuelRemaining,
                metrics.horizontalMiss);
        }

        // Notify UI (single flights only — batch suppresses popup)
        if (!batch)
        {
            MissionControlUI.Instance?.ShowLandingResult(metrics);
        }
    }

    /// <summary>Останній шлях CSV-логу (якщо є).</summary>
    public string GetLastTrajectoryPath()
    {
        if (logger == null) logger = GetComponent<DataLogger>();
        return logger != null ? logger.LastFilePath : null;
    }

    /// <summary>Повне перезавантаження та старт спуску (без збурень — див. ApplyFlightDisturbances).</summary>
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

        pitchPID.Reset();
        yawPID.Reset();
        thrustPID.Reset();

        SyncFixedTimestep();
        InitializeSimulation();
        if (logger != null) logger.Initialize();
        if (cachedVisualizer == null)
            cachedVisualizer = FindFirstObjectByType<TrajectoryVisualizer>();
        cachedVisualizer?.Clear();
        MissionControlUI.Instance?.HideLandingResult();
        SnapCamera();
    }

    /// <summary>
    /// Збурення для ОДИНОЧНОЇ посадки з UI (вітер / шум).
    /// Раніше працювало лише в Monte-Carlo — тому ручні слайдери «нічого не міняли».
    /// Ideal presets: windStrength=0, randomize=false.
    /// </summary>
    public void ApplyFlightDisturbances(
        float windStrength,
        bool randomize,
        float massVariationPercent = 6f,
        float angleVariationDegrees = 7f,
        float positionJitterMeters = 18f)
    {
        windStrength = Mathf.Max(0f, windStrength);
        applyContinuousWind = windStrength > 0.05f;

        if (windStrength > 0.05f)
        {
            // Постійний вітер + початковий kick
            Vector3 kick = new Vector3(
                Random.Range(-windStrength, windStrength),
                0f,
                Random.Range(-windStrength * 0.55f, windStrength * 0.55f));
            windVelocity = kick * 0.45f;
            state.velocity += kick * 0.75f;
        }
        else
        {
            windVelocity = Vector3.zero;
        }

        if (randomize)
        {
            float massNoise = 1f + Random.Range(-massVariationPercent, massVariationPercent) / 100f;
            state.currentFuelMass = Mathf.Max(500f, state.currentFuelMass * massNoise);

            float ax = Random.Range(-angleVariationDegrees, angleVariationDegrees);
            float az = Random.Range(-angleVariationDegrees, angleVariationDegrees);
            state.rotation = Quaternion.Normalize(state.rotation * Quaternion.Euler(ax, 0f, az));

            if (positionJitterMeters > 0.1f)
            {
                state.position.x += Random.Range(-positionJitterMeters, positionJitterMeters);
                state.position.z += Random.Range(-positionJitterMeters, positionJitterMeters);
            }
        }

        SyncTransformWithState();
    }

    /// <summary>Лише вибір режиму — без старту.</summary>
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

        pitchPID.Reset();
        yawPID.Reset();
        thrustPID.Reset();

        SyncFixedTimestep();
        InitializeSimulation();
        if (logger != null) logger.Initialize();
        if (cachedVisualizer == null)
            cachedVisualizer = FindFirstObjectByType<TrajectoryVisualizer>();
        cachedVisualizer?.Clear();
        MissionControlUI.Instance?.HideLandingResult();
        SnapCamera();
    }

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
        state.simulationFinished = true;
        state.isLanded = true;
        state.currentThrust = 0f;
        state.velocity = Vector3.zero;
        state.angularVelocity = Vector3.zero;

        if (!keepPosition)
        {
            // Return to start pad altitude for next run
            InitializeSimulation();
            state.simulationFinished = false;
            state.isLanded = false;
        }
        else
        {
            SyncTransformWithState();
        }

        Time.timeScale = 1f;
        SnapCamera();
    }

    static void SnapCamera()
    {
        var cam = FindFirstObjectByType<CameraFollow>();
        cam?.SnapNow();
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
