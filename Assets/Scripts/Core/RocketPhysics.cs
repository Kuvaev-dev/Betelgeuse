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
    [Tooltip("ScriptableObject з початковими умовами та характеристиками ракети")]
    public SimulationParameters parameters;

    /// <summary>
    /// Доступні режими керування посадкою.
    /// </summary>
    public enum ControlMode { PID, Fuzzy, Neural }

    [Header("Режим керування")]
    public ControlMode controlMode = ControlMode.Fuzzy;

    [Header("Поточний стан ракети (для відладки)")]
    public RocketState state = new RocketState();

    // Приватні поля для внутрішнього використання
    private DataLogger logger;

    private PIDController pitchPID = new PIDController();
    private PIDController yawPID = new PIDController();

    private ParticleSystem engineFlame;
    private ParticleSystem engineSmoke;

    public FuzzyLandingController fuzzyController;
    public NeuralController neuralController;

    // Метрики поточної симуляції
    public LandingMetrics metrics = new LandingMetrics();
    private float maxHeightRecorded = 0f;
    private float currentTime = 0f;

    void Start()
    {
        logger = GetComponent<DataLogger>();
        logger.Initialize();

        // Пошук систем частинок для візуалізації двигуна
        engineFlame = transform.Find("EngineFlame")?.GetComponent<ParticleSystem>();
        engineSmoke = transform.Find("EngineSmoke")?.GetComponent<ParticleSystem>();

        // Автоматичне отримання посилань на контролери
        if (fuzzyController == null) fuzzyController = GetComponent<FuzzyLandingController>();
        if (neuralController == null) neuralController = GetComponent<NeuralController>();

        if (neuralController != null)
            neuralController.LoadBestWeights();

        InitializeSimulation();
    }

    /// <summary>
    /// Ініціалізує початковий стан ракети з параметрів SimulationParameters.
    /// </summary>
    private void InitializeSimulation()
    {
        if (parameters == null)
            return;

        state.position = parameters.startPosition;
        state.velocity = parameters.startVelocity;
        state.rotation = Quaternion.Euler(parameters.startEulerAngles);
        state.angularVelocity = Vector3.zero;

        state.dryMass = parameters.dryMass;
        state.currentFuelMass = parameters.fuelMass;
        state.maxThrust = parameters.maxThrust;

        transform.position = state.position;
        transform.rotation = state.rotation;
    }

    void FixedUpdate()
    {
        if (state.isLanded || state.simulationFinished)
            return;

        // Запис максимальної висоти
        if (state.position.y > maxHeightRecorded)
            maxHeightRecorded = state.position.y;

        currentTime += parameters.fixedTimeStep;
        state.time = currentTime;

        // 1. Обчислення керування (тяга + вектор тяги)
        UpdateControl();

        // 2. Інтеграція фізики методом RK4
        RungeKutta4Step(parameters.fixedTimeStep);

        // 3. Оновлення Transform Unity
        ApplyToTransform();

        // 4. Логування
        logger.Log(state);

        // 5. Перевірка приземлення
        if (state.position.y <= 0.05f)
            FinishLanding();
    }

    /// <summary>
    /// Обчислює керуючі сигнали (тяга та кут вектора тяги) залежно від обраного режиму.
    /// </summary>
    private void UpdateControl()
    {
        if (parameters == null)
            return;

        // Обчислення помилок орієнтації
        Vector3 up = state.rotation * Vector3.up;
        float pitchError = Vector3.SignedAngle(up, Vector3.up, Vector3.right);
        float yawError = Vector3.SignedAngle(up, Vector3.up, Vector3.forward);

        if (controlMode == ControlMode.Fuzzy && fuzzyController != null && fuzzyController.isActive)
        {
            // Нечітке керування
            state.currentThrust = fuzzyController.CalculateThrust(state.position.y, state.velocity.y, state.TotalMass);
            Vector3 g = fuzzyController.CalculateGimbal(pitchError, yawError);
            state.thrustDirection = Quaternion.Euler(g) * Vector3.up;
        }
        else if (controlMode == ControlMode.Neural && neuralController != null && neuralController.isActive)
        {
            // Нейромережеве керування
            state.currentThrust = neuralController.CalculateThrust(
                state.position.y, state.velocity.y, state.TotalMass, state.currentThrust, pitchError);

            Vector3 g = neuralController.CalculateGimbal(pitchError, yawError);
            state.thrustDirection = Quaternion.Euler(g) * Vector3.up;
        }
        else
        {
            // Класичний PID
            float pitchCorrection = pitchPID.Calculate(0, pitchError, parameters.fixedTimeStep);
            float yawCorrection = yawPID.Calculate(0, yawError, parameters.fixedTimeStep);

            Quaternion targetGimbal = Quaternion.Euler(pitchCorrection * 0.8f, 0, yawCorrection * 0.8f);
            state.thrustDirection = targetGimbal * Vector3.up;

            state.currentThrust = CalculateThrustPID();
        }

        // Обмеження тяги фізичними можливостями двигуна
        state.currentThrust = Mathf.Clamp(state.currentThrust, 0f, state.maxThrust);

        // Керування системами частинок (візуалізація полум'я)
        bool engineOn = state.currentThrust > 1000f;
        if (engineFlame != null) { var em = engineFlame.emission; em.enabled = engineOn; }
        if (engineSmoke != null) { var em = engineSmoke.emission; em.enabled = engineOn; }
    }

    /// <summary>
    /// Один крок інтеграції руху методом Рунге-Кутта 4-го порядку (RK4).
    /// Забезпечує високу точність моделювання траєкторії.
    /// </summary>
    private void RungeKutta4Step(float dt)
    {
        // k1
        Vector3 k1v = state.velocity;
        Vector3 k1a = CalculateAccelerationAt(state.position, state.velocity);

        // k2
        Vector3 k2v = state.velocity + k1a * (dt * 0.5f);
        Vector3 k2a = CalculateAccelerationAt(state.position + k1v * (dt * 0.5f), k2v);

        // k3
        Vector3 k3v = state.velocity + k2a * (dt * 0.5f);
        Vector3 k3a = CalculateAccelerationAt(state.position + k2v * (dt * 0.5f), k3v);

        // k4
        Vector3 k4v = state.velocity + k3a * dt;
        Vector3 k4a = CalculateAccelerationAt(state.position + k3v * dt, k4v);

        // Оновлення стану (зважене середнє)
        state.velocity += (k1a + 2 * k2a + 2 * k3a + k4a) * (dt / 6f);
        state.position += (k1v + 2 * k2v + 2 * k3v + k4v) * (dt / 6f);

        // Витрата палива
        if (state.currentFuelMass > 0 && state.currentThrust > 0)
        {
            float massFlow = state.currentThrust / (parameters.isp * 9.80665f);
            state.currentFuelMass = Mathf.Max(0f, state.currentFuelMass - massFlow * dt);
        }

        // Моделювання кутового руху (Torque)

        float leverArm = 16f; // Відстань від центру мас до сопла (м)

        // Момент сил від відхиленого вектора тяги
        Vector3 localTorque = new Vector3(-state.thrustDirection.z, 0f, state.thrustDirection.x) *
                              state.currentThrust * leverArm;

        // Аеродинамічний демпфуючий момент (опір обертанню)
        localTorque -= state.angularVelocity * 40000f;

        // Момент інерції (спрощена модель циліндра)
        float momentOfInertia = state.TotalMass * 25f;
        Vector3 angularAcceleration = localTorque / momentOfInertia;

        // Оновлення кутових параметрів
        state.angularVelocity += angularAcceleration * dt;
        state.rotation *= Quaternion.Euler(state.angularVelocity * dt * Mathf.Rad2Deg);
    }

    /// <summary>
    /// Обчислює прискорення у заданій точці простору (для RK4).
    /// Враховує гравітацію, тягу та аеродинамічний опір.
    /// </summary>
    private Vector3 CalculateAccelerationAt(Vector3 pos, Vector3 vel)
    {
        Vector3 acc = Vector3.zero;

        // Гравітація (залежить від висоти)
        acc.y -= AtmosphereModel.GetGravity(pos.y);

        // Тяга двигуна (у світових координатах)
        Vector3 thrustWorld = state.rotation * state.thrustDirection * state.currentThrust;
        acc += thrustWorld / state.TotalMass;

        // Аеродинамічний лобовий опір
        float density = AtmosphereModel.GetDensity(pos.y);
        float drag = 0.5f * density * vel.sqrMagnitude * 0.85f * 8.5f;

        if (vel.sqrMagnitude > 0.01f)
            acc -= vel.normalized * (drag / state.TotalMass);

        return acc;
    }

    /// <summary>
    /// Проста евристика тяги для PID-режиму (базовий контролер висоти).
    /// </summary>
    private float CalculateThrustPID()
    {
        if (state.position.y < 1200f)
            return state.TotalMass * 9.81f * 1.85f; // Сильне гальмування біля землі
        return state.TotalMass * 9.81f * 1.05f; // Підтримка на великій висоті
    }

    /// <summary>
    /// Завершує симуляцію посадки, фіксує метрики та зберігає лог.
    /// </summary>
    private void FinishLanding()
    {
        state.position.y = 0f;
        state.isLanded = true;
        state.simulationFinished = true;

        // Фіксація метрик
        metrics.touchdownVelocity = state.velocity.magnitude;
        metrics.landingAngleError = Vector3.Angle(state.rotation * Vector3.up, Vector3.up);
        metrics.fuelRemaining = state.currentFuelMass;
        metrics.maxAltitude = maxHeightRecorded;
        metrics.totalFlightTime = state.time;

        // Критерії успішної посадки
        metrics.isSuccessfulLanding = (metrics.touchdownVelocity < 3.5f) &&
                                      (metrics.landingAngleError < 7.0f);

        state.velocity = Vector3.zero;
        state.angularVelocity = Vector3.zero;

        logger.Save();

        // Визначення назви алгоритму для логування
        string algorithm = controlMode switch
        {
            ControlMode.Fuzzy => "Fuzzy Logic (Sugeno)",
            ControlMode.Neural => "Neural Network (Evolutionary)",
            _ => "PID"
        };

        metrics.PrintResults(algorithm);

        // Онлайн-навчання нейромережі (якщо активовано)
        if (controlMode == ControlMode.Neural && neuralController != null)
        {
            neuralController.Train(metrics.touchdownVelocity,
                                   metrics.landingAngleError,
                                   metrics.fuelRemaining);
        }
    }

    /// <summary>
    /// Синхронізує Transform Unity з внутрішнім станом ракети.
    /// </summary>
    private void ApplyToTransform()
    {
        transform.position = state.position;
        transform.rotation = state.rotation;
    }

    /// <summary>
    /// Повністю скидає симуляцію до початкового стану.
    /// </summary>
    public void ResetSimulation()
    {
        state.isLanded = false;
        state.simulationFinished = false;
        currentTime = 0f;
        maxHeightRecorded = 0f;
        metrics = new LandingMetrics();

        pitchPID.Reset();
        yawPID.Reset();

        InitializeSimulation();
        logger.Initialize();
    }

    /// <summary>
    /// Синхронізує Transform з поточним станом (використовується після додавання шуму).
    /// </summary>
    public void SyncTransformWithState()
    {
        transform.position = state.position;
        transform.rotation = state.rotation;
    }
}