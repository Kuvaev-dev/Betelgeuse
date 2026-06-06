using UnityEngine;

/// <summary>
/// Структура, що описує повний фізичний стан ракети в будь-який момент часу.
/// Містить як лінійні, так і кутові параметри руху, а також параметри двигуна та палива.
/// </summary>
[System.Serializable]
public class RocketState
{
    public Vector3 position;
    public Vector3 velocity;
    public Quaternion rotation;
    public Vector3 angularVelocity;

    public float dryMass;
    public float currentFuelMass;
    public float TotalMass => dryMass + currentFuelMass;

    public float currentThrust;
    public float maxThrust;
    public Vector3 thrustDirection = Vector3.up;

    public float time = 0f;
    public bool isLanded = false;
    public bool simulationFinished = false;
}
