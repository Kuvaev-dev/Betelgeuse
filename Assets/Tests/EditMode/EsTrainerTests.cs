using NUnit.Framework;

/// <summary>ES(1+λ): метадані, відтворюваність і відсутність погіршення на валідації.</summary>
public class EsTrainerTests
{
    static EsTrainer.Options Tiny(ControlMode mode)
    {
        var o = EsTrainer.Options.Default(mode);
        o.Generations = 3;
        o.Lambda = 3;
        o.BatchSize = 3;
        o.ValidationSize = 4;
        o.ValidateEvery = 1;
        o.Seed = 11;
        return o;
    }

    [Test]
    public void Train_ProducesTrainedPolicy_WithMetadata()
    {
        var t = new EsTrainer();
        var p = t.Train(new ControllerSet(), Tiny(ControlMode.Neural));
        Assert.IsTrue(p.IsTrained);
        Assert.AreEqual("profile", p.baseLaw);
        Assert.AreEqual(3, p.generations);
        Assert.AreEqual(3, t.Log.Count);
        StringAssert.Contains("ES(1+3)", p.trainedBy);
    }

    [Test]
    public void BestByValidation_NeverWorseThanBaseline()
    {
        var p = new EsTrainer().Train(new ControllerSet(), Tiny(ControlMode.Hybrid));
        Assert.AreEqual("fuzzy", p.baseLaw);
        Assert.LessOrEqual(p.validationCost, p.baselineValidationCost + 1e-6f);
    }

    [Test]
    public void SameSeed_SameWeights()
    {
        var a = new EsTrainer().Train(new ControllerSet(), Tiny(ControlMode.Neural));
        var b = new EsTrainer().Train(new ControllerSet(), Tiny(ControlMode.Neural));
        CollectionAssert.AreEqual(a.GetParameters(), b.GetParameters());
    }

    [Test]
    public void OnlyNeuralOrHybrid_CanBeTrained()
    {
        Assert.Throws<System.ArgumentException>(() =>
            new EsTrainer().Train(new ControllerSet(), Tiny(ControlMode.PID)));
    }

    [Test]
    public void EpisodeCost_PenalizesFailureAndTimeout()
    {
        var lim = LandingCriteria.Limits.Default;
        var ok = new LandingMetrics { touchdownVelocity = 1f, landingAngleError = 1f, horizontalMiss = 5f,
            horizontalSpeed = 1f, fuelRemaining = 6000f, isSuccessfulLanding = true };
        var bad = new LandingMetrics { touchdownVelocity = 6f, landingAngleError = 1f, horizontalMiss = 5f,
            horizontalSpeed = 1f, fuelRemaining = 6000f, isSuccessfulLanding = false };
        var to = new LandingMetrics { timedOut = true };
        Assert.Less(LandingExperiment.EpisodeCost(ok, lim, 14000f), LandingExperiment.EpisodeCost(bad, lim, 14000f));
        Assert.Less(LandingExperiment.EpisodeCost(bad, lim, 14000f), LandingExperiment.EpisodeCost(to, lim, 14000f));
    }
}
