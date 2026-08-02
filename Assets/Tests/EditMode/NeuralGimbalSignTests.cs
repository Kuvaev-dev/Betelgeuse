using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Перевірка узгодженості знаку gimbal NN/Fuzzy (негативний зворотний зв'язок).
/// </summary>
public class NeuralGimbalSignTests
{
    GameObject go;
    NeuralController nn;
    FuzzyLandingController fuzzy;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("NNTest");
        nn = go.AddComponent<NeuralController>();
        nn.enableTraining = false;
        fuzzy = go.AddComponent<FuzzyLandingController>();
    }

    [TearDown]
    public void TearDown()
    {
        if (go != null) Object.DestroyImmediate(go);
    }

    [Test]
    public void NeuralGimbal_OpposesPositivePitchError()
    {
        nn.CalculateControl(500f, -40f, 35000f, 200000f,
            12f, 0f, 2f, out _, out Vector3 g);
        Assert.Less(g.x, 0f, "NN gimbal must counteract +pitch error");
    }

    [Test]
    public void NeuralGimbal_OpposesNegativePitchError()
    {
        nn.CalculateControl(500f, -40f, 35000f, 200000f,
            -12f, 0f, 2f, out _, out Vector3 g);
        Assert.Greater(g.x, 0f, "NN gimbal must counteract -pitch error");
    }

    [Test]
    public void NeuralAndFuzzy_SameSignOnPitch()
    {
        float pitchErr = 18f;
        nn.CalculateControl(400f, -35f, 32000f, 180000f,
            pitchErr, 5f, 3f, out _, out Vector3 nnG);
        Vector3 fzG = fuzzy.CalculateGimbal(pitchErr, 5f, 2f, 1f);
        Assert.AreEqual(Mathf.Sign(nnG.x), Mathf.Sign(fzG.x),
            "NN and Fuzzy must share negative-feedback sign convention");
    }

    [Test]
    public void NeuralThrust_InPhysicalRange()
    {
        float mass = 35000f;
        float h = 800f;
        nn.CalculateControl(h, -55f, mass, 250000f,
            3f, -2f, 4f, out float thrust, out _);
        float g = AtmosphereModel.GetGravity(h);
        Assert.IsFalse(float.IsNaN(thrust));
        Assert.Greater(thrust, mass * g * 0.5f);
        Assert.Less(thrust, mass * g * 3.2f);
    }

    [Test]
    public void LegacyCalculateGimbal_NegativeFeedback()
    {
        Vector3 g = nn.CalculateGimbal(10f, -8f);
        Assert.Less(g.x, 0f);
        Assert.Greater(g.z, 0f);
    }
}
