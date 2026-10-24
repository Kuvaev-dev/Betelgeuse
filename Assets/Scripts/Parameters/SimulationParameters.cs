using UnityEngine;

/// <summary>
/// Ассет параметрів ділянки посадки 1-го ступеня (Earth LZ): ПУ, апарат, крок, критерії.
/// Перетворюється на чисту <see cref="LandingSetup"/> для ядра симуляції.
/// </summary>
[CreateAssetMenu(fileName = "LandingParams", menuName = "Betelgeuse/Simulation Parameters")]
public class SimulationParameters : ScriptableObject
{
    [Header("Початкові умови (після відділення)")]
    public Vector3 startPosition = new Vector3(0, 1600f, 0);
    public Vector3 startVelocity = new Vector3(0, -60f, 0);
    public Vector3 startEulerAngles = new Vector3(0, 0, 2f);

    [Header("1-й ступінь")]
    public float dryMass = Stage1Vehicle.DryMassKg;
    public float fuelMass = Stage1Vehicle.LandingFuelKg;
    public float maxThrust = Stage1Vehicle.LandingThrustN;
    public float isp = Stage1Vehicle.IspLandingS;

    [Header("Симуляція")]
    public float fixedTimeStep = 0.005f;
    public float maxSimulationTime = 400f;

    [Header("Критерії успішної посадки")]
    public float maxTouchdownVelocity = LandingCriteria.DefaultMaxTouchdownVelocity;
    public float maxLandingAngle = LandingCriteria.DefaultMaxLandingAngle;
    public float maxHorizontalMiss = LandingCriteria.DefaultMaxHorizontalMiss;
    public float maxHorizontalSpeed = LandingCriteria.DefaultMaxHorizontalSpeed;

    /// <summary>Задати ПУ ділянки посадки (висота, швидкість зниження, нахил).</summary>
    public void SetInitialConditions(float height, float descentSpeed, float tiltDeg)
    {
        startPosition = new Vector3(0f, height, 0f);
        startVelocity = new Vector3(0f, -Mathf.Abs(descentSpeed), 0f);
        startEulerAngles = new Vector3(0f, 0f, tiltDeg);
    }

    /// <summary>Номінальні характеристики Falcon 9-class (<see cref="Stage1Vehicle"/>) і стандартні критерії.</summary>
    public void ApplyStage1Vehicle()
    {
        dryMass = Stage1Vehicle.DryMassKg;
        fuelMass = Stage1Vehicle.LandingFuelKg;
        maxThrust = Stage1Vehicle.LandingThrustN;
        isp = Stage1Vehicle.IspLandingS;
        fixedTimeStep = 0.005f;
        maxSimulationTime = 400f;
        maxTouchdownVelocity = LandingCriteria.DefaultMaxTouchdownVelocity;
        maxLandingAngle = LandingCriteria.DefaultMaxLandingAngle;
        maxHorizontalMiss = LandingCriteria.DefaultMaxHorizontalMiss;
        maxHorizontalSpeed = LandingCriteria.DefaultMaxHorizontalSpeed;
    }

    public LandingSetup ToSetup() => new LandingSetup
    {
        DryMass = dryMass > 1000f ? dryMass : Stage1Vehicle.DryMassKg,
        FuelMass = fuelMass > 100f ? fuelMass : Stage1Vehicle.LandingFuelKg,
        MaxThrust = maxThrust > 1000f ? maxThrust : Stage1Vehicle.LandingThrustN,
        Isp = isp > 10f ? isp : Stage1Vehicle.IspLandingS,
        Dt = Mathf.Clamp(fixedTimeStep, 0.001f, 0.02f),
        MaxTime = maxSimulationTime > 10f ? maxSimulationTime : 400f,
        StartPosition = startPosition.y > 10f ? startPosition : new Vector3(0f, 1600f, 0f),
        StartVelocity = startVelocity,
        StartEulerDeg = startEulerAngles,
        Limits = new LandingCriteria.Limits(maxTouchdownVelocity, maxLandingAngle, maxHorizontalMiss, maxHorizontalSpeed)
    };
}
