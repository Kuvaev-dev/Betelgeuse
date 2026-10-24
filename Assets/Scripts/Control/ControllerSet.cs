using System.IO;

/// <summary>
/// Набір стратегій A–D з однаковими налаштуваннями (composition root для симуляції,
/// Monte-Carlo та навчання). Для паралельних прогонів кожен потік бере свій <see cref="Clone"/>.
/// </summary>
[System.Serializable]
public sealed class ControllerSet
{
    public PidLandingStrategy pid = new PidLandingStrategy();
    public FuzzyLandingController fuzzy = new FuzzyLandingController();
    public NeuralController neural = new NeuralController();
    public HybridController hybrid;

    [System.NonSerialized] LandingControllerResolver resolver;

    public ControllerSet()
    {
        hybrid = new HybridController(fuzzy);
    }

    LandingControllerResolver Resolver
    {
        get
        {
            if (resolver != null) return resolver;
            if (hybrid == null) hybrid = new HybridController(fuzzy);
            hybrid.fuzzy = fuzzy;
            resolver = new LandingControllerResolver();
            resolver.Register(pid);
            resolver.Register(fuzzy);
            resolver.Register(neural);
            resolver.Register(hybrid);
            return resolver;
        }
    }

    public ILandingController Get(ControlMode mode) => Resolver.Resolve(mode);

    public void ResetAll() => Resolver.ResetAll();

    /// <summary>Глибока копія (окремі інтегратори PID і буфери MLP) — для багатопотоковості.</summary>
    public ControllerSet Clone()
    {
        var c = new ControllerSet
        {
            pid = new PidLandingStrategy(),
            fuzzy = fuzzy.Clone(),
            neural = neural.Clone()
        };
        c.pid.Thrust = Copy(pid.Thrust);
        c.pid.Pitch = Copy(pid.Pitch);
        c.pid.Yaw = Copy(pid.Yaw);
        c.pid.thrustGainN = pid.thrustGainN;
        c.pid.maxGimbalDeg = pid.maxGimbalDeg;
        c.hybrid = hybrid.Clone(c.fuzzy);
        return c;
    }

    static PIDController Copy(PIDController p)
        => new PIDController { Kp = p.Kp, Ki = p.Ki, Kd = p.Kd, IntegralLimit = p.IntegralLimit, DerivativeFilterTau = p.DerivativeFilterTau };

    /// <summary>
    /// Завантажити навчені ваги MLP з каталогу проєкту. Повертає опис для журналу/UI.
    /// Якщо файлу немає або формат застарілий — лишається нейтральна мережа (ΔT=0, k=1).
    /// </summary>
    public string LoadWeights(string directory)
    {
        var n = MlpPolicy.LoadOrNull(Path.Combine(directory, NeuralController.WeightsFileName));
        var h = MlpPolicy.LoadOrNull(Path.Combine(directory, HybridController.WeightsFileName));
        if (n != null) neural.policy = n;
        if (h != null) hybrid.policy = h;
        return $"Neural: {Describe(neural.policy)}; Hybrid: {Describe(hybrid.policy)}";
    }

    static string Describe(MlpPolicy p)
        => p.IsTrained
            ? $"{p.trainedBy}, gen {p.generations}, val cost {p.validationCost:F3}, val success {p.validationSuccess:P0}"
            : "untrained (neutral)";
}
