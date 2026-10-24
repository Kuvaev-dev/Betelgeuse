using UnityEngine;

/// <summary>
/// Режим C — нейромережевий регулятор: базовий закон (профіль soft-landing + PD орієнтації)
/// плюс навчений MLP-residual <see cref="MlpPolicy"/> (поправка тяги та адаптивний масштаб
/// бічного наведення). Ваги — BestWeights_Neural.json, навчені ES(1+λ) офлайн.
/// </summary>
[System.Serializable]
public sealed class NeuralController : ILandingController
{
    public ControlMode Mode => ControlMode.Neural;
    public string DisplayName => "Neural MLP (ES)";

    public const string WeightsFileName = "BestWeights_Neural.json";
    public const string BaseLaw = "profile";

    public MlpPolicy policy = MlpPolicy.CreateNeutral(BaseLaw);

    public void ResetSession() { /* ваги заморожені; внутрішнього стану немає */ }

    public ControlCommand Evaluate(in ControlContext ctx)
        => policy.ApplyResidual(in ctx, BaseCommand(in ctx));

    /// <summary>Базовий (не навчений) закон режиму C.</summary>
    public static ControlCommand BaseCommand(in ControlContext ctx)
        => new ControlCommand(
            SoftLandingGuidance.ProfileThrust(ctx.Height, ctx.VerticalVelocity, ctx.Mass),
            SoftLandingGuidance.AttitudeGimbal(in ctx));

    public NeuralController Clone() => new NeuralController { policy = policy.Clone() };
}
