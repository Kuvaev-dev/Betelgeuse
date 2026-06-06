using UnityEngine;

/// <summary>
/// Структура, що описує повний фізичний стан ракети в будь-який момент часу.
/// Містить як лінійні, так і кутові параметри руху, а також параметри двигуна та палива.
/// </summary>
[System.Serializable]
public class RocketState
{
    [Header("Лінійний рух")]
    public Vector3 position; // Позиція у світових координатах (м)
    public Vector3 velocity; // Лінійна швидкість (м/с)

    [Header("Кутовий рух")]
    public Quaternion rotation; // Орієнтація ракети
    public Vector3 angularVelocity; // Кутова швидкість (рад/с)

    [Header("Маса та двигун")]
    public float dryMass; // Маса порожньої ракети (кг)
    public float currentFuelMass; // Поточна маса палива (кг)
    public float currentThrust; // Поточна тяга двигуна (Н)
    public float maxThrust; // Максимальна тяга двигуна (Н)
    public Vector3 thrustDirection; // Напрямок вектора тяги (локальні координати)

    [Header("Час та статус")]
    public float time; // Час симуляції (с)
    public bool isLanded; // Чи приземлилася ракета
    public bool simulationFinished; // Чи завершена симуляція

    /// <summary>
    /// Повна поточна маса ракети (суха маса + паливо).
    /// </summary>
    public float TotalMass => dryMass + currentFuelMass;
}