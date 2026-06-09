using UnityEngine;

/// <summary>
/// Основний компонент фізики та керування ракетою.
/// Реалізує:
/// - Інтеграцію руху методом Рунге-Кутта 4-го порядку (RK4)
/// - Три режими керування: PID, Fuzzy Logic, Neural Network
/// - Моделювання аеродинаміки, гравітації та витрати палива
/// - Збір метрик та логування
/// </summary>
[RequireComponent(typeof(DataLogger))]
public class RocketPhysics : MonoBehaviour
{
    [Header("Основні параметри")]
    public SimulationParameters parameters;
    public enum ControlMode { PID, Fuzzy, Neural }

    [Header("Режим керування")]
    public ControlMode controlMode = ControlMode.Fuzzy;
    public RocketState state = new RocketState();
    private DataLogger logger;

    private PIDController pitchPID = new PIDController();
    private PIDController yawPID = new PIDController();

    private PIDController thrustPID = new PIDController() { Kp = 2.8f, Ki = 0.4f, Kd = 1.5f };

    private ParticleSystem engineFlame;
    private ParticleSystem engineSmoke;

    public FuzzyLandingController fuzzyController;
    public NeuralController neuralController;
    public LandingMetrics metrics = new LandingMetrics();
    private float maxHeightRecorded = 0f;
    private float currentTime = 0f;

    void Start()
    {
        logger = GetComponent<DataLogger>();
        logger.Initialize();

        engineFlame = transform.Find("EngineFlame")?.GetComponent<ParticleSystem>();
        engineSmoke = transform.Find("EngineSmoke")?.GetComponent<ParticleSystem>();

        if (fuzzyController == null) fuzzyController = GetComponent<FuzzyLandingController>();
        if (neuralController == null) neuralController = GetComponent<NeuralController>();
        if (neuralController != null) neuralController.LoadBestWeights();

        InitializeSimulation();
    }

    private void InitializeSimulation()
    {
        if (parameters == null) return;
        state.position = parameters.startPosition;
        state.velocity = parameters.startVelocity;
        state.rotation = Quaternion.Euler(parameters.startEulerAngles);
        state.angularVelocity = Vector3.zero;

        state.dryMass = parameters.dryMass;
        state.currentFuelMass = parameters.fuelMass;
        state.maxThrust = parameters.maxThrust;

        SyncTransformWithState();
    }

    void FixedUpdate()
    {
        if (state.isLanded || state.simulationFinished) return;
        if (state.position.y > maxHeightRecorded) maxHeightRecorded = state.position.y;

        currentTime += parameters.fixedTimeStep;
        state.time = currentTime;

        UpdateControl();
        RungeKutta4Step(parameters.fixedTimeStep);
        SyncTransformWithState(); // Використовуємо уніфіковану синхронізацію

        logger.Log(state);
        if (state.position.y <= 0.05f)
            FinishLanding();
    }

    private void UpdateControl()
    {
        if (parameters == null) return;
        Vector3 up = state.rotation * Vector3.up;
        float pitchError = Vector3.SignedAngle(up, Vector3.up, Vector3.right);
        float yawError = Vector3.SignedAngle(up, Vector3.up, Vector3.forward);

        if (controlMode == ControlMode.Fuzzy && fuzzyController != null && fuzzyController.isActive)
        {
            state.currentThrust = fuzzyController.CalculateThrust(state.position.y, state.velocity.y, state.TotalMass);
            Vector3 g = fuzzyController.CalculateGimbal(pitchError, yawError);
            state.thrustDirection = Quaternion.Euler(g) * Vector3.up;
        }
        else if (controlMode == ControlMode.Neural && neuralController != null && neuralController.isActive)
        {
            state.currentThrust = neuralController.CalculateThrust(state.position.y, state.velocity.y, state.TotalMass, state.currentThrust, pitchError);
            Vector3 g = neuralController.CalculateGimbal(pitchError, yawError);
            state.thrustDirection = Quaternion.Euler(g) * Vector3.up;
        }
        else
        {
            float pitchCorrection = pitchPID.Calculate(0, pitchError, parameters.fixedTimeStep);
            float yawCorrection = yawPID.Calculate(0, yawError, parameters.fixedTimeStep);

            Quaternion targetGimbal = Quaternion.Euler(pitchCorrection * 0.8f, 0, yawCorrection * 0.8f);
            state.thrustDirection = targetGimbal * Vector3.up;
            state.currentThrust = CalculateThrustPID();
        }

        state.currentThrust = Mathf.Clamp(state.currentThrust, 0f, state.maxThrust);
        bool engineOn = state.currentThrust > 1000f;
        if (engineFlame != null) { var em = engineFlame.emission; em.enabled = engineOn; }
        if (engineSmoke != null) { var em = engineSmoke.emission; em.enabled = engineOn; }
    }

    private void RungeKutta4Step(float dt)
    {
        Vector3 k1v = state.velocity;
        Vector3 k1a = CalculateAccelerationAt(state.position, state.velocity);

        Vector3 k2v = state.velocity + k1a * (dt * 0.5f);
        Vector3 k2a = CalculateAccelerationAt(state.position + k1v * (dt * 0.5f), k2v);
        Vector3 k3v = state.velocity + k2a * (dt * 0.5f);
        Vector3 k3a = CalculateAccelerationAt(state.position + k2v * (dt * 0.5f), k3v);

        Vector3 k4v = state.velocity + k3a * dt;
        Vector3 k4a = CalculateAccelerationAt(state.position + k3v * dt, k4v);

        state.velocity += (k1a + 2 * k2a + 2 * k3a + k4a) * (dt / 6f);
        state.position += (k1v + 2 * k2v + 2 * k3v + k4v) * (dt / 6f);

        if (state.currentFuelMass > 0 && state.currentThrust > 0)
        {
            float massFlow = state.currentThrust / (parameters.isp * 9.80665f);
            state.currentFuelMass = Mathf.Max(0f, state.currentFuelMass - massFlow * dt);
        }

        float leverArm = 16f;
        Vector3 localTorque = new Vector3(-state.thrustDirection.z, 0f, state.thrustDirection.x) * state.currentThrust * leverArm;
        localTorque -= state.angularVelocity * 40000f;
        float momentOfInertia = state.TotalMass * 25f;
        Vector3 angularAcceleration = localTorque / momentOfInertia;

        // Симплектичний метод Ейлера для кутової стабілізації (висока кутова стійкість)
        state.angularVelocity += angularAcceleration * dt;
        state.rotation *= Quaternion.Euler(state.angularVelocity * dt * Mathf.Rad2Deg);
    }

    private Vector3 CalculateAccelerationAt(Vector3 pos, Vector3 vel)
    {
        Vector3 acc = Vector3.zero;
        acc.y -= AtmosphereModel.GetGravity(pos.y);

        Vector3 thrustWorld = state.rotation * state.thrustDirection * state.currentThrust;
        acc += thrustWorld / state.TotalMass;

        float density = AtmosphereModel.GetDensity(pos.y);
        float drag = 0.5f * density * vel.sqrMagnitude * 0.85f * 8.5f;
        if (vel.sqrMagnitude > 0.01f)
            acc -= vel.normalized * (drag / state.TotalMass);
        return acc;
    }

    private float CalculateThrustPID()
    {
        float targetVelocity = Mathf.Clamp(-Mathf.Sqrt(2f * 1.6f * state.position.y), -75f, -2.0f);
        if (state.position.y < 6f) targetVelocity = -1.5f; // Плавний дотик на фінальних метрах

        float pidOutput = thrustPID.Calculate(targetVelocity, state.velocity.y, parameters.fixedTimeStep);
        float gravityCompensation = state.TotalMass * AtmosphereModel.GetGravity(state.position.y);

        return gravityCompensation + pidOutput * 12000f; // Масштабування виходу під динаміку носія
    }

    private void FinishLanding()
    {
        state.position.y = 0f;
        state.isLanded = true;
        state.simulationFinished = true;

        metrics.touchdownVelocity = state.velocity.magnitude;
        metrics.landingAngleError = Vector3.Angle(state.rotation * Vector3.up, Vector3.up);
        metrics.fuelRemaining = state.currentFuelMass;
        metrics.maxAltitude = maxHeightRecorded;
        metrics.totalFlightTime = state.time;
        metrics.isSuccessfulLanding = (metrics.touchdownVelocity < 3.5f) && (metrics.landingAngleError < 7.0f);
        state.velocity = Vector3.zero;
        state.angularVelocity = Vector3.zero;

        logger.Save();

        string algorithm = controlMode switch
        {
            ControlMode.Fuzzy => "Fuzzy Logic (Sugeno)",
            ControlMode.Neural => "Neural Network (Evolutionary)",
            _ => "PID"
        };
        metrics.PrintResults(algorithm);

        FindObjectOfType<TrajectoryVisualizer>()?.OnSimulationFinished(metrics.isSuccessfulLanding);

        if (controlMode == ControlMode.Neural && neuralController != null)
        {
            neuralController.Train(metrics.touchdownVelocity, metrics.landingAngleError, metrics.fuelRemaining);
        }
    }

    public void ResetSimulation()
    {
        state.isLanded = false;
        state.simulationFinished = false;
        currentTime = 0f;
        maxHeightRecorded = 0f;
        metrics = new LandingMetrics();

        pitchPID.Reset();
        yawPID.Reset();
        thrustPID.Reset(); // Скидання ПІД вертикального каналу

        InitializeSimulation();
        logger.Initialize();
    }

    public void SyncTransformWithState()
    {
        transform.position = state.position;
        transform.rotation = state.rotation;
    }
}
