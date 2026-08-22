using UnityEngine;

/// <summary>
/// Знімок стану апарата для одного кроку GNC (<see cref="ILandingController"/>).
/// </summary>
public readonly struct ControlContext
{
    public readonly float Height;
    public readonly float VerticalVelocity;
    public readonly float Mass;
    public readonly float CurrentThrust;
    public readonly float MaxThrust;
    public readonly float PitchErrorDeg;
    public readonly float YawErrorDeg;
    public readonly float PitchRateDeg;
    public readonly float YawRateDeg;
    public readonly float HorizSpeed;
    public readonly float TiltDeg;
    public readonly float Dt;
    public readonly Quaternion Rotation;
    public readonly Vector3 AngularVelocity;

    public ControlContext(
        float height, float verticalVelocity, float mass, float currentThrust, float maxThrust,
        float pitchErrorDeg, float yawErrorDeg, float pitchRateDeg, float yawRateDeg,
        float horizSpeed, float tiltDeg, float dt,
        Quaternion rotation, Vector3 angularVelocity)
    {
        Height = height;
        VerticalVelocity = verticalVelocity;
        Mass = mass;
        CurrentThrust = currentThrust;
        MaxThrust = maxThrust;
        PitchErrorDeg = pitchErrorDeg;
        YawErrorDeg = yawErrorDeg;
        PitchRateDeg = pitchRateDeg;
        YawRateDeg = yawRateDeg;
        HorizSpeed = horizSpeed;
        TiltDeg = tiltDeg;
        Dt = dt;
        Rotation = rotation;
        AngularVelocity = angularVelocity;
    }

    public static ControlContext FromState(RocketState state, float dt)
    {
        Vector3 up = state.rotation * Vector3.up;
        float pitchError = Vector3.SignedAngle(up, Vector3.up, Vector3.right);
        float yawError = Vector3.SignedAngle(up, Vector3.up, Vector3.forward);
        float pitchRate = state.angularVelocity.x * Mathf.Rad2Deg;
        float yawRate = state.angularVelocity.z * Mathf.Rad2Deg;
        float horizSpeed = new Vector2(state.velocity.x, state.velocity.z).magnitude;
        float tilt = Vector3.Angle(up, Vector3.up);
        return new ControlContext(
            state.position.y, state.velocity.y, state.TotalMass, state.currentThrust, state.maxThrust,
            pitchError, yawError, pitchRate, yawRate, horizSpeed, tilt, dt,
            state.rotation, state.angularVelocity);
    }
}
