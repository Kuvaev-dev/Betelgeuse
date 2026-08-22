using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Реєстр стратегій посадки. <see cref="RocketPhysics"/> резолвить режим без switch по типах.
/// </summary>
public sealed class LandingControllerResolver
{
    readonly Dictionary<RocketPhysics.ControlMode, ILandingController> map = new();
    ILandingController fallback;

    public void Register(ILandingController controller)
    {
        if (controller == null) return;
        map[controller.Mode] = controller;
        if (controller.Mode == RocketPhysics.ControlMode.PID)
            fallback = controller;
    }

    public void SetFallback(ILandingController controller) => fallback = controller;

    public ILandingController Resolve(RocketPhysics.ControlMode mode)
    {
        if (map.TryGetValue(mode, out var c) && c != null && c.IsAvailable)
            return c;
        if (fallback != null) return fallback;
        foreach (var kv in map)
            if (kv.Value != null && kv.Value.IsAvailable)
                return kv.Value;
        return null;
    }

    public void ResetAll()
    {
        foreach (var c in map.Values)
            c?.ResetSession();
    }

    public IEnumerable<ILandingController> All => map.Values;

    /// <summary>Composition root: PID + Fuzzy/Neural/Hybrid з компонента ракети.</summary>
    public static LandingControllerResolver CreateDefault(RocketPhysics rocket, PidLandingStrategy pid)
    {
        var resolver = new LandingControllerResolver();
        if (pid != null)
        {
            resolver.Register(pid);
            resolver.SetFallback(pid);
        }

        var fuzzy = rocket.fuzzyController ?? rocket.GetComponent<FuzzyLandingController>();
        var neural = rocket.neuralController ?? rocket.GetComponent<NeuralController>();
        var hybrid = rocket.hybridController ?? rocket.GetComponent<HybridController>();

        if (fuzzy != null) resolver.Register(fuzzy);
        if (neural != null) resolver.Register(neural);
        if (hybrid != null)
        {
            hybrid.fuzzy = fuzzy;
            hybrid.neural = neural;
            resolver.Register(hybrid);
        }

        return resolver;
    }
}
