using NUnit.Framework;
using UnityEngine;

public class RocketStateTests
{
    [Test]
    public void TotalMass_SumsDryAndFuel()
    {
        var s = new RocketState { dryMass = 25600f, currentFuelMass = 14000f };
        Assert.AreEqual(39600f, s.TotalMass, 1e-3f);
    }

    [Test]
    public void TotalMass_ZeroFuel_EqualsDry()
    {
        var s = new RocketState { dryMass = 1000f, currentFuelMass = 0f };
        Assert.AreEqual(1000f, s.TotalMass, 1e-3f);
    }

    [Test]
    public void DefaultFlags_NotLanded()
    {
        var s = new RocketState();
        Assert.IsFalse(s.isLanded);
        Assert.IsFalse(s.simulationFinished);
        Assert.AreEqual(0f, s.time);
    }
}
