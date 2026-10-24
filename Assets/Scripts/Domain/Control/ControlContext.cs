using UnityEngine;

/// <summary>
/// Вхід стратегії керування на один такт GNC. Заповнюється з бортової оцінки
/// <see cref="NavigationEstimator"/> (або з істинного стану, якщо шум сенсорів вимкнено).
///
/// Конвенція кутів (єдина для всіх алгоритмів A–D):
/// помилка орієнтації — це кут між віссю корпусу та <b>бажаною</b> віссю, яку задає
/// бічне наведення <see cref="LateralGuidance"/>, розкладений по осях корпусу X («pitch») і Z («yaw»).
/// Додатна помилка + додатна кутова швидкість ⇒ додатний кут TVC є відновлювальним
/// (див. <see cref="SoftLandingGuidance.AttitudeGimbal"/>).
/// </summary>
public readonly struct ControlContext
{
    public readonly float Height;
    public readonly float VerticalVelocity;
    public readonly float Mass;
    public readonly float Gravity;
    public readonly float CurrentThrust;
    public readonly float MaxThrust;
    /// <summary>Помилка орієнтації навколо осі X корпусу, град.</summary>
    public readonly float PitchErrorDeg;
    /// <summary>Помилка орієнтації навколо осі Z корпусу, град.</summary>
    public readonly float YawErrorDeg;
    /// <summary>Кутова швидкість навколо осі X корпусу, град/с.</summary>
    public readonly float PitchRateDeg;
    /// <summary>Кутова швидкість навколо осі Z корпусу, град/с.</summary>
    public readonly float YawRateDeg;
    /// <summary>Нахил корпусу від вертикалі, град (для safety-логіки та входу MLP).</summary>
    public readonly float TiltDeg;
    /// <summary>Бажаний нахил від наведення, град.</summary>
    public readonly float DesiredTiltDeg;
    public readonly float HorizSpeed;
    public readonly float PositionX;
    public readonly float PositionZ;
    public readonly float VelX;
    public readonly float VelZ;
    public readonly float Dt;

    ControlContext(float height, float vy, float mass, float gravity, float thrust, float maxThrust,
        float pitchErr, float yawErr, float pitchRate, float yawRate, float tilt, float desiredTilt,
        float horizSpeed, float px, float pz, float vx, float vz, float dt)
    {
        Height = height; VerticalVelocity = vy; Mass = mass; Gravity = gravity;
        CurrentThrust = thrust; MaxThrust = maxThrust;
        PitchErrorDeg = pitchErr; YawErrorDeg = yawErr;
        PitchRateDeg = pitchRate; YawRateDeg = yawRate;
        TiltDeg = tilt; DesiredTiltDeg = desiredTilt; HorizSpeed = horizSpeed;
        PositionX = px; PositionZ = pz; VelX = vx; VelZ = vz; Dt = dt;
    }

    /// <summary>
    /// Єдина точка побудови контексту (DRY): оцінка стану + бажана вісь корпусу.
    /// </summary>
    public static ControlContext Create(
        Vector3 position, Vector3 velocity, Quaternion rotation, Vector3 angularVelocityBody,
        float mass, float currentThrust, float maxThrust, Vector3 desiredUp, float dt)
    {
        Vector3 bodyUp = rotation * Vector3.up;
        if (desiredUp.sqrMagnitude < 1e-6f) desiredUp = Vector3.up;
        // Вісь, навколо якої треба повернути корпус від бажаної осі до поточної (у тілі)
        Vector3 axisBody = Quaternion.Inverse(rotation) * Vector3.Cross(desiredUp.normalized, bodyUp);
        float errX = Mathf.Asin(Mathf.Clamp(axisBody.x, -1f, 1f)) * Mathf.Rad2Deg;
        float errZ = Mathf.Asin(Mathf.Clamp(axisBody.z, -1f, 1f)) * Mathf.Rad2Deg;
        float h = position.y;
        return new ControlContext(
            h, velocity.y, mass, AtmosphereModel.GetGravity(Mathf.Max(0f, h)),
            currentThrust, maxThrust,
            errX, errZ,
            angularVelocityBody.x * Mathf.Rad2Deg, angularVelocityBody.z * Mathf.Rad2Deg,
            Vector3.Angle(bodyUp, Vector3.up), Vector3.Angle(desiredUp, Vector3.up),
            new Vector2(velocity.x, velocity.z).magnitude,
            position.x, position.z, velocity.x, velocity.z, dt);
    }

    /// <summary>Контекст з істинного стану з вертикаллю як бажаною віссю (тести).</summary>
    public static ControlContext FromState(RocketState s, float dt)
        => Create(s.position, s.velocity, s.rotation, s.angularVelocity,
            s.TotalMass, s.currentThrust, s.maxThrust, Vector3.up, dt);
}
