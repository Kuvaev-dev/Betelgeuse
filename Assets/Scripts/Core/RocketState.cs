using UnityEngine;

/// <summary>
/// Повний фізичний стан ракети в довільний момент симуляції.
/// Трансляція (position/velocity), орієнтація (rotation/ω),
/// паливо/маса та параметри двигуна (тяга, напрям gimbal).
/// </summary>
[System.Serializable]
public class RocketState
{
    /// <summary>Позиція в світі, м (Y — висота над pad).</summary>
    public Vector3 position;
    /// <summary>Лінійна швидкість, м/с.</summary>
    public Vector3 velocity;
    /// <summary>Орієнтація корпусу (локальний +Y = «вгору» корпусу).</summary>
    public Quaternion rotation;
    /// <summary>Кутова швидкість, рад/с.</summary>
    public Vector3 angularVelocity;

    /// <summary>Суха маса без палива, кг.</summary>
    public float dryMass;
    /// <summary>Поточна маса палива, кг.</summary>
    public float currentFuelMass;
    /// <summary>Повна маса = суха + паливо.</summary>
    public float TotalMass => dryMass + currentFuelMass;

    /// <summary>Поточна тяга, Н.</summary>
    public float currentThrust;
    /// <summary>Максимальна тяга, Н.</summary>
    public float maxThrust;
    /// <summary>Напрям тяги в тілі ракети (після gimbal), одиничний вектор.</summary>
    public Vector3 thrustDirection = Vector3.up;

    /// <summary>Час симуляції від старту, с.</summary>
    public float time = 0f;
    /// <summary>true після touchdown або STOP.</summary>
    public bool isLanded = false;
    /// <summary>true коли цикл FixedUpdate більше не інтегрує.</summary>
    public bool simulationFinished = false;
}
