using UnityEngine;

/// <summary>
/// Початкові умови та характеристики. Об'єкт керування після відділення — 1-й ступінь (Earth LZ).
/// До відділення — спрощений підйом повного пакета (Stack); A–D лише на Stage1.
/// </summary>
[CreateAssetMenu(fileName = "LandingParams", menuName = "Betelgeuse/Simulation Parameters")]
public class SimulationParameters : ScriptableObject
{
    [Header("Посадка 1-го ступеня (після відділення)")]
    [Tooltip("IC ділянки посадки Stage-1 (Ideal / MC / після sep)")]
    public Vector3 startPosition = new Vector3(0, 1600f, 0);
    public Vector3 startVelocity = new Vector3(0, -60f, 0);
    public Vector3 startEulerAngles = new Vector3(0, 0, 2f);

    [Header("1-й ступінь (об'єкт GNC A–D)")]
    public float dryMass = 25600f;
    public float fuelMass = 14000f;
    public float maxThrust = 845000f;
    public float isp = 311f;

    [Header("Пакет Stack (до відділення, спрощений підйом)")]
    [Tooltip("Старт пакета біля pad / низький підйом")]
    public Vector3 stackStartPosition = new Vector3(0f, 45f, 0f);
    public Vector3 stackStartVelocity = new Vector3(0f, 35f, 0f);
    public Vector3 stackStartEulerAngles = Vector3.zero;
    [Tooltip("Суха маса всього 3-ступеневого пакета (спрощено)")]
    public float stackDryMass = 110000f;
    [Tooltip("Паливо на підйом (спрощено; не модель орбіти)")]
    public float stackFuelMass = 280000f;
    public float stackMaxThrust = 7600000f;
    [Tooltip("Висота або час відділення (що настане раніше)")]
    public float separationAltitude = 2400f;
    public float separationTime = 18f;
    [Tooltip("Після sep підставити IC посадки (start*) замість поточної балістики")]
    public bool snapToLandingIcOnSeparation = true;

    [Header("Симуляція")]
    public float fixedTimeStep = 0.005f;
    public float maxSimulationTime = 400f;

    [Header("Критерії успішної посадки")]
    public float maxTouchdownVelocity = 3.5f;
    public float maxLandingAngle = 7f;
    public float maxHorizontalMiss = 40f;
    public float maxHorizontalSpeed = 6.5f;

    /// <summary>
    /// Підстрахування для старих .asset без нових полів Stack (Unity серіалізує 0).
    /// </summary>
    public void EnsureStackDefaults()
    {
        if (stackDryMass < 1000f) stackDryMass = 110000f;
        if (stackFuelMass < 1000f) stackFuelMass = 280000f;
        if (stackMaxThrust < 1000f) stackMaxThrust = 7600000f;
        if (stackStartPosition.y < 1f)
            stackStartPosition = new Vector3(0f, 45f, 0f);
        if (stackStartVelocity.sqrMagnitude < 0.01f)
            stackStartVelocity = new Vector3(0f, 35f, 0f);
        if (separationAltitude < 100f) separationAltitude = 2400f;
        if (separationTime < 1f) separationTime = 18f;
        if (dryMass < 1000f) dryMass = 25600f;
        if (fuelMass < 100f) fuelMass = 14000f;
        if (maxThrust < 1000f) maxThrust = 845000f;
        if (startPosition.y < 100f) startPosition = new Vector3(0f, 1600f, 0f);
        if (startVelocity.y > -1f) startVelocity = new Vector3(0f, -60f, 0f);
    }
}
