using UnityEngine;

/// <summary>
/// ScriptableObject, що містить усі початкові умови та характеристики ракети-носія.
/// Дозволяє легко змінювати параметри без редагування коду.
/// </summary>
[CreateAssetMenu(fileName = "LandingParams", menuName = "Betelgeuse/Simulation Parameters")]
public class SimulationParameters : ScriptableObject
{
    [Header("Початкові умови для посадки")]
    public Vector3 startPosition = new Vector3(0, 2500f, 0);
    public Vector3 startVelocity = new Vector3(0, -100f, 0);
    public Vector3 startEulerAngles = new Vector3(0, 0, 5f);

    [Header("Характеристики ракети")]
    public float dryMass = 25600f;
    public float fuelMass = 14000f;
    public float maxThrust = 845000f;
    public float isp = 311f;

    [Header("Симуляція")]
    public float fixedTimeStep = 0.005f;
    public float maxSimulationTime = 400f;
}
