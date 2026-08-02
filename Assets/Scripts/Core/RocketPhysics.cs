using UnityEngine;

/// <summary>
/// Ядро фізики та GNC ракетоносія.
/// — Трансляція: інтегратор Рунге–Кутти 4-го порядку (RK4);
/// — Орієнтація: semi-implicit Euler + демпфінг;
/// — Режими: PID / Fuzzy Sugeno / Neural ES / Hybrid Neuro-Fuzzy.
/// Симуляція стартує лише після simulationArmed = true (кнопка UI).
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
    private PIDController pitchPID = new PIDController() { Kp = 0.65f, Ki = 0.08f, Kd = 0.4f };
    private PIDController yawPID = new PIDController() { Kp = 0.65f, Ki = 0.08f, Kd = 0.4f };
    private PIDController thrustPID = new PIDController() { Kp = 3.2f, Ki = 0.45f, Kd = 1.8f };

    public FuzzyLandingController fuzzyController;
    public NeuralController neuralController;
    public HybridController hybridController;
    public LandingMetrics metrics = new LandingMetrics();

    private float maxHeightRecorded;
    private float currentTime;
    private TrajectoryVisualizer cachedVisualizer;

    const float LeverArm = 11f;
    const float AngularDamping = 320000f;
    const float InertiaFactor = 45f;
    const float MaxOmega = 1.2f; // rad/s
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

        // Базова стабілізація (cross-product) — завжди
        Vector3 baseGimbal = SoftLandingGuidance.AttitudeGimbal(
            state.rotation, state.angularVelocity, maxDeg: 20f);
        Vector3 gCmd = baseGimbal;
        float thrustCmd = SoftLandingGuidance.ProfileThrust(h, state.velocity.y, state.TotalMass);

        switch (controlMode)
        {
            case ControlMode.Fuzzy when fuzzyController != null && fuzzyController.isActive:
            {
                thrustCmd = fuzzyController.CalculateThrust(h, state.velocity.y, state.TotalMass);
                Vector3 fg = fuzzyController.CalculateGimbal(pitchError, yawError, pitchRate, yawRate);
                gCmd = Vector3.Lerp(baseGimbal, fg, 0.4f);
                break;
            }
            case ControlMode.Neural when neuralController != null && neuralController.isActive:
            {
                neuralController.CalculateControl(
                    h, state.velocity.y, state.TotalMass, state.currentThrust,
                    pitchError, yawError, horizSpeed,
                    out thrustCmd, out Vector3 ng);
                gCmd = Vector3.Lerp(baseGimbal, ng, 0.35f);
                break;
            }
            case ControlMode.Hybrid when hybridController != null && hybridController.isActive:
            {
                hybridController.CalculateControl(
                    h, state.velocity.y, state.TotalMass, state.currentThrust,
                    pitchError, yawError, pitchRate, yawRate, horizSpeed,
                    out thrustCmd, out Vector3 hg);
                gCmd = Vector3.Lerp(baseGimbal, hg, 0.4f);
                break;
            }
            default:
            {
                float pc = pitchPID.Calculate(0, pitchError, parameters.fixedTimeStep);
                float yc = yawPID.Calculate(0, yawError, parameters.fixedTimeStep);
                gCmd = new Vector3(
                    Mathf.Clamp(baseGimbal.x + pc * 0.25f, -20f, 20f),
                    0f,
                    Mathf.Clamp(baseGimbal.z + yc * 0.25f, -20f, 20f));
                thrustCmd = CalculateThrustPID();
                break;
            }
        }

        // При великому нахилі — тільки вирівнювання, мінімальна тяга
        float upright = SoftLandingGuidance.UprightThrustScale(tilt);
        thrustCmd *= upright;
        if (tilt > 25f)
        {
            gCmd = baseGimbal; // чистий stabilizer
            thrustCmd = Mathf.Min(thrustCmd, state.TotalMass * AtmosphereModel.GetGravity(h) * 0.85f);
        }

        gCmd.x = Mathf.Clamp(gCmd.x, -20f, 20f);
        gCmd.y = 0f;
        gCmd.z = Mathf.Clamp(gCmd.z, -20f, 20f);
        state.thrustDirection = (Quaternion.Euler(gCmd) * Vector3.up).normalized;

        if (tilt < 20f)
            ApplyLateralGuidance();

        state.currentThrust = Mathf.Clamp(thrustCmd, 0f, state.maxThrust);
        if (state.currentFuelMass <= 0f) state.currentThrust = 0f;
    }

    /// <summary>
    /// Guidance до центру pad: при майже вертикальному корпусі
    /// gx ≈ +k·z + c·vz, gz ≈ −k·x − c·vx (див. DOCS / модель сил).
    /// </summary>
    void ApplyLateralGuidance()
    {
        float h = Mathf.Max(0f, state.position.y);
        if (h > 1200f) return;

        float tilt = Vector3.Angle(state.rotation * Vector3.up, Vector3.up);
        if (tilt > 35f) return; // спочатку вирівняти корпус

        float fade = Mathf.Clamp01(1f - h / 1200f);
        float kPos = 0.028f * fade;
        float kVel = 0.08f * fade;
        if (h < 80f)
        {
            kPos *= 1.2f;
            kVel *= 1.15f;
        }

        float gx = Mathf.Clamp(kPos * state.position.z + kVel * state.velocity.z, -8f, 8f);
        float gz = Mathf.Clamp(-(kPos * state.position.x + kVel * state.velocity.x), -8f, 8f);

        Vector3 td = state.thrustDirection.normalized;
        float curX = Mathf.Atan2(-td.z, Mathf.Max(1e-4f, td.y)) * Mathf.Rad2Deg;
        float curZ = Mathf.Atan2(td.x, Mathf.Max(1e-4f, td.y)) * Mathf.Rad2Deg;
        float nx = Mathf.Clamp(curX + gx, -28f, 28f);
        float nz = Mathf.Clamp(curZ + gz, -28f, 28f);
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

    float CalculateThrustPID()
    {
        float h = Mathf.Max(0f, state.position.y);
        float profile = SoftLandingGuidance.ProfileThrust(h, state.velocity.y, state.TotalMass);
        float targetVelocity = SoftLandingGuidance.TargetDescentRate(h);
        float pidOutput = thrustPID.Calculate(targetVelocity, state.velocity.y, parameters.fixedTimeStep);
        // PID fine-tune поверх профілю
        return profile + pidOutput * 10000f;
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
        cachedVisualizer?.OnSimulationFinished(metrics.isSuccessfulLanding);

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

    /// <summary>Повне перезавантаження та старт спуску.</summary>
    public void ResetSimulation()
    {
        state.isLanded = false;
        state.simulationFinished = false;
        currentTime = 0f;
        maxHeightRecorded = 0f;
        metrics = new LandingMetrics();
        windVelocity = Vector3.zero;
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
