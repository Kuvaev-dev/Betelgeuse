using NUnit.Framework;
using UnityEngine;

/// <summary>Бортова навігація: pipeline існує, zero-noise трекає істину, шум не розвалюється.</summary>
public class NavigationEstimatorTests
{
    RocketState MakeTruth()
    {
        return new RocketState
        {
            position = new Vector3(12f, 800f, -7f),
            velocity = new Vector3(3f, -40f, 1.5f),
            rotation = Quaternion.Euler(2f, 0f, -1.5f),
            angularVelocity = new Vector3(0.02f, 0f, -0.01f),
            dryMass = Stage1Vehicle.DryMassKg,
            currentFuelMass = Stage1Vehicle.LandingFuelKg,
            currentThrust = 400000f,
            maxThrust = Stage1Vehicle.LandingThrustN,
            thrustDirection = Vector3.up
        };
    }

    [Test]
    public void ZeroNoise_TracksTruthWithinTightBand()
    {
        var truth = MakeTruth();
        var nav = new NavigationEstimator();
        nav.Reset(truth, seed: 42);
        Vector3 acc = new Vector3(0.2f, -6f, -0.1f);
        const float dt = 0.005f;
        for (int i = 0; i < 200; i++)
        {
            // проста кінематика істини
            truth.velocity += acc * dt;
            truth.position += truth.velocity * dt;
            nav.Step(truth, acc, dt, noiseScale: 0f);
        }

        Assert.IsTrue(nav.Current.valid);
        Assert.Less(nav.Current.heightResid, 0.25f, "zero-noise height residual");
        Assert.Less(nav.Current.horizResid, 0.35f, "zero-noise horiz residual");
        var ctx = nav.ToContext(truth, dt);
        Assert.AreEqual(truth.TotalMass, ctx.Mass, 0.01f);
        Assert.IsFalse(float.IsNaN(ctx.Height));
        Assert.Greater(ctx.Height, 100f);
    }

    [Test]
    public void NoisySensors_StayBounded()
    {
        var truth = MakeTruth();
        var nav = new NavigationEstimator();
        nav.Reset(truth, seed: 7);
        Vector3 acc = new Vector3(0.1f, -5.5f, 0.05f);
        const float dt = 0.005f;
        for (int i = 0; i < 400; i++)
        {
            truth.velocity += acc * dt;
            truth.position += truth.velocity * dt;
            nav.Step(truth, acc, dt, noiseScale: 1f);
        }

        Assert.IsTrue(nav.Current.valid);
        Assert.Less(nav.Current.heightResid, 8f);
        Assert.Less(nav.Current.horizResid, 12f);
        var ctx = nav.ToContext(truth, dt);
        Assert.IsFalse(float.IsNaN(ctx.VerticalVelocity));
        Assert.IsFalse(float.IsNaN(ctx.PitchErrorDeg));
        Assert.Less(Mathf.Abs(ctx.TiltDeg), 35f);
    }

    [Test]
    public void SameSeed_SameNoiseSequence()
    {
        var a = new NavigationEstimator();
        var b = new NavigationEstimator();
        var t = MakeTruth();
        a.Reset(t, 99);
        b.Reset(t, 99);
        Vector3 acc = Vector3.down * 6f;
        a.Step(t, acc, 0.005f, 1f);
        b.Step(t, acc, 0.005f, 1f);
        Assert.AreEqual(a.Current.position.x, b.Current.position.x, 1e-6f);
        Assert.AreEqual(a.Current.position.y, b.Current.position.y, 1e-6f);
    }

    [Test]
    public void Context_CarriesLateralState()
    {
        var t = MakeTruth();
        var nav = new NavigationEstimator();
        nav.Reset(t, 1);
        var ctx = nav.ToContext(t, 0.005f);
        Assert.AreEqual(t.position.x, ctx.PositionX, 0.01f);
        Assert.AreEqual(t.position.z, ctx.PositionZ, 0.01f);
        Assert.AreEqual(t.velocity.x, ctx.VelX, 0.01f);
        Assert.AreEqual(t.velocity.z, ctx.VelZ, 0.01f);
    }
}
