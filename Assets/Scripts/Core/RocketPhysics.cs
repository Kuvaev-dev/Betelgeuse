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
    private PIDController pitchPID = new PIDController();
    private PIDController yawPID = new PIDController();
    private PIDController thrustPID = new PIDController() { Kp = 2.8f, Ki = 0.4f, Kd = 1.5f };

    public FuzzyLandingController fuzzyController;
    public NeuralController neuralController;
    public HybridController hybridController;
    public LandingMetrics metrics = new LandingMetrics();

    private float maxHeightRecorded;
    private float currentTime;
    private TrajectoryVisualizer cachedVisualizer;

    const float LeverArm = 16f;
    const float AngularDamping = 40000f;
    const float InertiaFactor = 25f;
    const float Cd = 0.85f;
    const float RefArea = 8.5f;
    const float G0 = 9.80665f;

    void Start()
    {
        logger = GetComponent<DataLogger>();
        logger.Initialize();

        if (fuzzyController == null) fuzzyController = GetComponent<FuzzyLandingController>();
        if (neuralController == null) neuralController = GetComponent<NeuralController>();
        if (hybridController == null) hybridController = GetComponent<HybridController>();
        if (hybridController == null)
        {
            hybridController = gameObject.AddComponent<HybridController>();
            hybridController.fuzzy = fuzzyController;
            hybridController.neural = neuralController;
        }

        if (neuralController != null) neuralController.LoadBestWeights();
        cachedVisualizer = FindFirstObjectByType<TrajectoryVisualizer>();

        InitializeSimulation();
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
        SyncTransformWithState();
        logger.Log(state);

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

        switch (controlMode)
        {
            case ControlMode.Fuzzy when fuzzyController != null && fuzzyController.isActive:
            {
                state.currentThrust = fuzzyController.CalculateThrust(
                    state.position.y, state.velocity.y, state.TotalMass);
                Vector3 g = fuzzyController.CalculateGimbal(pitchError, yawError, pitchRate, yawRate);
                state.thrustDirection = Quaternion.Euler(g) * Vector3.up;
                break;
            }
            case ControlMode.Neural when neuralController != null && neuralController.isActive:
            {
                neuralController.CalculateControl(
                    state.position.y, state.velocity.y, state.TotalMass, state.currentThrust,
                    pitchError, yawError, horizSpeed,
                    out state.currentThrust, out Vector3 g);
                state.thrustDirection = Quaternion.Euler(g) * Vector3.up;
                break;
            }
            case ControlMode.Hybrid when hybridController != null && hybridController.isActive:
            {
                hybridController.CalculateControl(
                    state.position.y, state.velocity.y, state.TotalMass, state.currentThrust,
                    pitchError, yawError, pitchRate, yawRate, horizSpeed,
                    out state.currentThrust, out Vector3 g);
                state.thrustDirection = Quaternion.Euler(g) * Vector3.up;
                break;
            }
            default:
            {
                float pitchCorrection = pitchPID.Calculate(0, pitchError, parameters.fixedTimeStep);
                float yawCorrection = yawPID.Calculate(0, yawError, parameters.fixedTimeStep);
                Quaternion targetGimbal = Quaternion.Euler(
                    Mathf.Clamp(pitchCorrection * 0.8f, -28f, 28f),
                    0f,
                    Mathf.Clamp(yawCorrection * 0.8f, -28f, 28f));
                state.thrustDirection = targetGimbal * Vector3.up;
                state.currentThrust = CalculateThrustPID();
                break;
            }
        }

        state.currentThrust = Mathf.Clamp(state.currentThrust, 0f, state.maxThrust);
        if (state.currentFuelMass <= 0f) state.currentThrust = 0f;
        // Engine VFX: RocketEngineFX (on Visual) drives flame/smoke from thrust
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
        state.rotation *= Quaternion.Euler(state.angularVelocity * dt * Mathf.Rad2Deg);
        state.rotation = Quaternion.Normalize(state.rotation);
    }

    Vector3 CalculateAccelerationAt(Vector3 pos, Vector3 vel)
    {
        Vector3 acc = Vector3.zero;
        acc.y -= AtmosphereModel.GetGravity(pos.y);

        Vector3 thrustWorld = state.rotation * state.thrustDirection * state.currentThrust;
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
        float targetVelocity = Mathf.Clamp(-Mathf.Sqrt(2f * 1.6f * h), -75f, -2f);
        if (h < 6f) targetVelocity = -1.5f;

        float pidOutput = thrustPID.Calculate(targetVelocity, state.velocity.y, parameters.fixedTimeStep);
        float gravityCompensation = state.TotalMass * AtmosphereModel.GetGravity(h);
        return gravityCompensation + pidOutput * 12000f;
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

        float maxV = parameters != null ? parameters.maxTouchdownVelocity : 3.5f;
        float maxA = parameters != null ? parameters.maxLandingAngle : 7f;
        float maxM = parameters != null ? parameters.maxHorizontalMiss : 25f;
        float maxH = parameters != null ? parameters.maxHorizontalSpeed : 5f;
        metrics.isSuccessfulLanding = !timeout
            && metrics.touchdownVelocity < maxV
            && metrics.landingAngleError < maxA
            && metrics.horizontalMiss < maxM
            && metrics.horizontalSpeed < maxH;

        state.velocity = Vector3.zero;
        state.angularVelocity = Vector3.zero;

        logger.Save();

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

        InitializeSimulation();
        if (logger != null) logger.Initialize();
        if (cachedVisualizer == null)
            cachedVisualizer = FindFirstObjectByType<TrajectoryVisualizer>();
        cachedVisualizer?.Clear();
        MissionControlUI.Instance?.HideLandingResult();
        SnapCamera();
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
