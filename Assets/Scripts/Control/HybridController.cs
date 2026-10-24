/// <summary>
/// Режим D — гібрид Neuro-Fuzzy: повний нечіткий закон режиму B (тяга + орієнтація)
/// плюс окремо навчений MLP-residual (поправка тяги та масштаб бічного наведення).
/// Ваги — BestWeights_Hybrid.json (навчені ES(1+λ) саме поверх нечіткого закону).
/// Ablation: <see cref="useNeuralResidual"/> = false ⇒ D ≡ B.
/// </summary>
[System.Serializable]
public sealed class HybridController : ILandingController
{
    public ControlMode Mode => ControlMode.Hybrid;
    public string DisplayName => "Hybrid Neuro-Fuzzy";

    public const string WeightsFileName = "BestWeights_Hybrid.json";
    public const string BaseLaw = "fuzzy";

    public bool useNeuralResidual = true;
    public MlpPolicy policy = MlpPolicy.CreateNeutral(BaseLaw);
    /// <summary>Нечіткий закон (спільний екземпляр із режимом B, без стану).</summary>
    [System.NonSerialized] public FuzzyLandingController fuzzy;

    public HybridController(FuzzyLandingController fuzzy) { this.fuzzy = fuzzy; }

    public void ResetSession() => fuzzy?.ResetSession();

    public ControlCommand Evaluate(in ControlContext ctx)
    {
        var baseCmd = fuzzy.Evaluate(in ctx);
        return useNeuralResidual ? policy.ApplyResidual(in ctx, in baseCmd) : baseCmd;
    }

    public HybridController Clone(FuzzyLandingController fuzzyClone)
        => new HybridController(fuzzyClone) { useNeuralResidual = useNeuralResidual, policy = policy.Clone() };
}
