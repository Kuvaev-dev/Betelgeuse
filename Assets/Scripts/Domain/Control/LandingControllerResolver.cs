using System.Collections.Generic;

/// <summary>
/// Реєстр стратегій посадки: <see cref="LandingSimulation"/> отримує стратегію за режимом без switch по типах.
/// </summary>
public sealed class LandingControllerResolver
{
    readonly Dictionary<ControlMode, ILandingController> map = new Dictionary<ControlMode, ILandingController>();

    public void Register(ILandingController controller)
    {
        if (controller != null) map[controller.Mode] = controller;
    }

    /// <summary>Стратегія режиму або null (тоді симуляція застосує <see cref="ControlCommand.ProfileFallback"/>).</summary>
    public ILandingController Resolve(ControlMode mode)
        => map.TryGetValue(mode, out var c) ? c : null;

    public void ResetAll()
    {
        foreach (var c in map.Values) c?.ResetSession();
    }

    public IEnumerable<ILandingController> All => map.Values;
}
