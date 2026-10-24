/// <summary>
/// Стратегія GNC посадки (патерн Strategy). Реалізації: PID, Fuzzy, Neural, Hybrid.
/// Стратегія відповідає за модуль тяги та кути TVC; бічне наведення і safety — спільні
/// (<see cref="LandingSimulation"/>), тож порівняння A–D чесне.
/// </summary>
public interface ILandingController
{
    ControlMode Mode { get; }
    string DisplayName { get; }
    /// <summary>Скинути внутрішній стан перед новою посадкою (інтегратори тощо).</summary>
    void ResetSession();
    ControlCommand Evaluate(in ControlContext context);
}
