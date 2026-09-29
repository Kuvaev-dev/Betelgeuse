using NUnit.Framework;
using UnityEngine;

public class NeuralEsTrainingTests
{
    GameObject go;
    NeuralController nn;

    [SetUp]
    public void SetUp()
    {
        IdealLandingPresets.ClearActive();
        go = new GameObject("EsTrain");
        nn = go.AddComponent<NeuralController>();
        nn.enableTraining = true;
        nn.persistTrainedWeights = false;
        nn.InstallIdealWeights(persist: false);
        nn.bestCost = 100f;
    }

    [TearDown]
    public void TearDown()
    {
        IdealLandingPresets.ClearActive();
        if (go != null) Object.DestroyImmediate(go);
    }

    [Test]
    public void Train_Disabled_DoesNotAdvanceGeneration()
    {
        nn.enableTraining = false;
        int g = nn.generation;
        nn.Train(1f, 1f, 8000f, 5f, false);
        Assert.AreEqual(g, nn.generation);
        Assert.IsFalse(nn.LastStep.Applied);
    }

    [Test]
    public void Train_BetterCost_SnapshotsAndIncrements()
    {
        int g = nn.generation;
        nn.Train(1f, 1f, 8000f, 2f, false);
        Assert.AreEqual(g + 1, nn.generation);
        Assert.IsTrue(nn.LastStep.Applied);
        Assert.IsTrue(nn.LastStep.Improved);
        Assert.Less(nn.bestCost, 100f);
    }

    [Test]
    public void Train_WorseCost_KeepsEliteCost()
    {
        nn.Train(1f, 1f, 8000f, 2f, false);
        float elite = nn.bestCost;
        nn.BeginEpisode();
        nn.Train(80f, 40f, 100f, 200f, true);
        Assert.IsTrue(nn.LastStep.Applied);
        Assert.IsFalse(nn.LastStep.Improved);
        Assert.AreEqual(elite, nn.bestCost, 1e-4f);
    }

    [Test]
    public void BeginEpisode_WithTrainingOff_IsNoOp()
    {
        nn.enableTraining = false;
        int g = nn.generation;
        nn.BeginEpisode();
        Assert.AreEqual(g, nn.generation);
    }

    [Test]
    public void StatusLine_MentionsTrainingState()
    {
        nn.enableTraining = false;
        StringAssert.Contains("OFF", nn.StatusLine(false));
        nn.enableTraining = true;
        StringAssert.Contains("ES", nn.StatusLine(false));
        StringAssert.Contains("ES", nn.StatusLine(true));
    }

    [Test]
    public void LiveWind_ScalesExistingVector()
    {
        var rpGo = new GameObject("RpWind");
        try
        {
            var rp = rpGo.AddComponent<RocketPhysics>();
            rp.windVelocity = new Vector3(4f, 0f, 0f);
            rp.ApplyLiveWind(10f);
            Assert.AreEqual(10f, rp.windVelocity.magnitude, 0.05f);
            Assert.Greater(rp.windVelocity.x, 0f);
            rp.ApplyLiveWind(0f);
            Assert.AreEqual(Vector3.zero, rp.windVelocity);
            Assert.IsFalse(rp.applyContinuousWind);
        }
        finally
        {
            Object.DestroyImmediate(rpGo);
        }
    }
}
