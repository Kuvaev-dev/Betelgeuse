using NUnit.Framework;
using UnityEngine;

/// <summary>MLP 5→8→2: нейтральність, обмеженість виходів, вектор параметрів і JSON.</summary>
public class MlpPolicyTests
{
    static ControlContext Ctx(float h = 600f, float vy = -35f) => ControlContext.Create(
        new Vector3(5f, h, -3f), new Vector3(1f, vy, 0.5f), Quaternion.Euler(2f, 0f, 1f), Vector3.zero,
        34000f, 380000f, Stage1Vehicle.LandingThrustN, Vector3.up, 0.005f);

    [Test]
    public void NeutralPolicy_ReproducesBaseLaw()
    {
        var ctx = Ctx();
        var nn = new NeuralController();
        var baseCmd = NeuralController.BaseCommand(ctx);
        var cmd = nn.Evaluate(ctx);
        Assert.AreEqual(baseCmd.Thrust, cmd.Thrust, 1e-2f);
        Assert.AreEqual(1f, cmd.LateralScale, 1e-6f);
    }

    [Test]
    public void Outputs_AreBounded_ForExtremeWeights()
    {
        var p = MlpPolicy.CreateNeutral("profile");
        var w = p.GetParameters();
        for (int i = 0; i < w.Length; i++) w[i] = (i % 2 == 0 ? 50f : -50f);
        p.SetParameters(w);
        p.Infer(Ctx(), out float frac, out float lat);
        Assert.LessOrEqual(Mathf.Abs(frac), MlpPolicy.ResidualFrac + 1e-5f);
        Assert.LessOrEqual(lat, Mathf.Exp(MlpPolicy.LateralRange) + 1e-4f);
        Assert.GreaterOrEqual(lat, Mathf.Exp(-MlpPolicy.LateralRange) - 1e-4f);
    }

    [Test]
    public void Residual_FadesOutNearGround()
    {
        var p = MlpPolicy.CreateNeutral("profile");
        var w = p.GetParameters();
        w[w.Length - 2] = 3f; // bO[0] ⇒ ΔT ≈ +0.3·mg
        p.SetParameters(w);
        var ctx = Ctx(h: 2f, vy: -1f);
        var baseCmd = NeuralController.BaseCommand(ctx);
        Assert.AreEqual(baseCmd.Thrust, p.ApplyResidual(ctx, baseCmd).Thrust, 1e-2f);
        var high = Ctx(h: 500f);
        var bh = NeuralController.BaseCommand(high);
        Assert.Greater(p.ApplyResidual(high, bh).Thrust, bh.Thrust);
    }

    [Test]
    public void Parameters_AndJson_RoundTrip()
    {
        var p = MlpPolicy.CreateNeutral("fuzzy");
        var w = p.GetParameters();
        for (int i = 0; i < w.Length; i++) w[i] = Mathf.Sin(i * 1.3f);
        p.SetParameters(w);
        p.trainedBy = "ES(1+12)";
        p.validationCost = 0.25f;
        Assert.AreEqual(MlpPolicy.ParameterCount, w.Length);
        Assert.IsTrue(MlpPolicy.TryFromJson(p.ToJson(), out var q));
        CollectionAssert.AreEqual(w, q.GetParameters());
        Assert.AreEqual("fuzzy", q.baseLaw);
        Assert.IsTrue(q.IsTrained);
        Assert.AreEqual(0.25f, q.validationCost, 1e-5f);
    }

    [Test]
    public void LegacyWeightsFormat_IsRejected()
    {
        Assert.IsFalse(MlpPolicy.TryFromJson("{\"inputSize\":5,\"wIH\":[0.1]}", out _));
    }
}
