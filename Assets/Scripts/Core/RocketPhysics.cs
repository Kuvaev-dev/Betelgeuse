using UnityEngine;

[RequireComponent(typeof(DataLogger))]
public class RocketPhysics : MonoBehaviour
{
    [Header("Основні параметри")]
    public SimulationParameters parameters;

    public RocketState state = new RocketState();
    private DataLogger logger;

    private PIDController pitchPID = new PIDController();
    private PIDController yawPID = new PIDController();

    private ParticleSystem engineFlame;
    private ParticleSystem engineSmoke;

    private float currentTime = 0f;

    void Start()
    {
        logger = GetComponent<DataLogger>();
        logger.Initialize();

        engineFlame = transform.Find("EngineFlame")?.GetComponent<ParticleSystem>();
        engineSmoke = transform.Find("EngineSmoke")?.GetComponent<ParticleSystem>();

        InitializeSimulation();
    }

    private void InitializeSimulation()
    {
        if (parameters == null)
        {
            Debug.LogError("SimulationParameters не призначено на RocketPhysics!");
            return;
        }

        state.position = parameters.startPosition;
        state.velocity = parameters.startVelocity;
        state.rotation = Quaternion.Euler(parameters.startEulerAngles);

        state.dryMass = parameters.dryMass;
        state.currentFuelMass = parameters.fuelMass;
        state.maxThrust = parameters.maxThrust;

        transform.position = state.position;
        transform.rotation = state.rotation;

        Debug.Log($"Ракета ініціалізована. Висота: {state.position.y} м");
    }

    void FixedUpdate()
    {
        if (state.isLanded || state.simulationFinished) return;

        currentTime += parameters.fixedTimeStep;
        state.time = currentTime;

        RungeKutta4Step(parameters.fixedTimeStep);
        ApplyToTransform();

        logger.Log(state);

        UpdateControl();

        if (state.position.y <= 0.1f)
            FinishLanding();
    }

    private void UpdateControl()
    {
        // Стабілізація
        Vector3 up = state.rotation * Vector3.up;
        float pitchError = Vector3.SignedAngle(up, Vector3.up, Vector3.right);
        float yawError = Vector3.SignedAngle(up, Vector3.up, Vector3.forward);

        float pitchCorrection = pitchPID.Calculate(0, pitchError, parameters.fixedTimeStep);
        float yawCorrection = yawPID.Calculate(0, yawError, parameters.fixedTimeStep);

        Quaternion targetGimbal = Quaternion.Euler(pitchCorrection * 0.8f, 0, yawCorrection * 0.8f);
        state.thrustDirection = targetGimbal * Vector3.up;

        state.currentThrust = CalculateThrust();

        // Візуалізація двигуна
        bool engineOn = state.currentThrust > 10000f;
        if (engineFlame != null)
        {
            var em = engineFlame.emission;
            em.enabled = engineOn;
        }
        if (engineSmoke != null)
        {
            var em = engineSmoke.emission;
            em.enabled = engineOn;
        }
    }

    private void RungeKutta4Step(float dt)
    {
        Vector3 k1v = state.velocity;
        Vector3 k1a = CalculateAcceleration();

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
            float massFlow = state.currentThrust / (parameters.isp * 9.81f);
            state.currentFuelMass -= massFlow * dt;
        }
    }

    private Vector3 CalculateAcceleration()
    {
        return CalculateAccelerationAt(state.position, state.velocity);
    }

    private Vector3 CalculateAccelerationAt(Vector3 pos, Vector3 vel)
    {
        Vector3 acc = Vector3.zero;
        acc.y -= AtmosphereModel.GetGravity(pos.y);

        Vector3 thrustWorld = state.rotation * state.thrustDirection * state.currentThrust;
        acc += thrustWorld / state.TotalMass;

        float density = AtmosphereModel.GetDensity(pos.y);
        float drag = 0.5f * density * vel.sqrMagnitude * 8.5f;
        if (vel.sqrMagnitude > 0.1f)
            acc -= vel.normalized * (drag / state.TotalMass);

        return acc;
    }

    private float CalculateThrust()
    {
        if (state.position.y < 1200f)
            return state.TotalMass * 9.81f * 1.85f;
        return state.TotalMass * 9.81f * 1.05f;
    }

    private void FinishLanding()
    {
        state.position.y = 0;
        state.velocity = Vector3.zero;
        state.isLanded = true;
        state.simulationFinished = true;
        logger.Save();
    }

    private void ApplyToTransform()
    {
        transform.position = state.position;
        transform.rotation = state.rotation;
    }
}