using UnityEngine;

/// <summary>
/// Початкові умови та характеристики ракетоносія (ScriptableObject).
/// Значення наближені до класу F9 1st stage (спрощена модель).
/// </summary>
[CreateAssetMenu(fileName = "LandingParams", menuName = "Betelgeuse/Simulation Parameters")]
public class SimulationParameters : ScriptableObject
{
    [Header("Початкові умови (номінал — складніший за «Ідеал»)")]
    public Vector3 startPosition = new Vector3(0, 1800f, 0);
    public Vector3 startVelocity = new Vector3(0, -72f, 0);
    public Vector3 startEulerAngles = new Vector3(0, 0, 3.5f);

    [Header("Характеристики ракети")]
    public float dryMass = 25600f;
    public float fuelMass = 14000f;
    public float maxThrust = 845000f;
    public float isp = 311f;

    [Header("Симуляція")]
    public float fixedTimeStep = 0.005f;
    public float maxSimulationTime = 400f;

    [Header("Критерії успішної посадки")]
    public float maxTouchdownVelocity = 3.5f;
    public float maxLandingAngle = 7f;
    public float maxHorizontalMiss = 25f;
    public float maxHorizontalSpeed = 5f;
}
