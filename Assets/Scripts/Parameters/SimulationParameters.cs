using UnityEngine;

/// <summary>
/// ScriptableObject, що містить усі початкові умови та характеристики ракети-носія.
/// Дозволяє легко змінювати параметри без редагування коду.
/// </summary>
[CreateAssetMenu(fileName = "LandingParams", menuName = "Betelgeuse/Simulation Parameters")]
public class SimulationParameters : ScriptableObject
{
    [Header("Початкові умови для посадки")]
    [Tooltip("Початкова позиція ракети (м)")]
    public Vector3 startPosition = new Vector3(0, 2500f, 0);

    [Tooltip("Початкова швидкість (м/с)")]
    public Vector3 startVelocity = new Vector3(0, -100f, 0);

    [Tooltip("Початкові кути Ейлера (градуси)")]
    public Vector3 startEulerAngles = new Vector3(0, 0, 5f);

    [Header("Характеристики ракети")]
    [Tooltip("Маса порожньої ракети (кг)")]
    public float dryMass = 25600f;

    [Tooltip("Маса палива (кг)")]
    public float fuelMass = 14000f;

    [Tooltip("Максимальна тяга двигуна (Н)")]
    public float maxThrust = 845000f;

    [Tooltip("Питомий імпульс двигуна (с)")]
    public float isp = 311f;

    [Header("Симуляція")]
    [Tooltip("Крок інтеграції (с) — менший = точніше, але повільніше")]
    public float fixedTimeStep = 0.005f;

    [Tooltip("Максимальний час симуляції (с)")]
    public float maxSimulationTime = 400f;
}