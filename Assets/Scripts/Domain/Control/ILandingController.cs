/// <summary>
/// Стратегія GNC посадки (Strategy). Реалізації: PID, Fuzzy, Neural, Hybrid.
/// </summary>
public interface ILandingController
{
    RocketPhysics.ControlMode Mode { get; }
    string DisplayName { get; }
    bool IsAvailable { get; }
    void ResetSession();
    ControlCommand Evaluate(in ControlContext context);
}
