using NUnit.Framework;
using UnityEngine;

public class AtmosphereModelTests
{
    [Test]
    public void Density_AtSeaLevel_IsStandard()
    {
        Assert.AreEqual(1.225f, AtmosphereModel.GetDensity(0f), 1e-4f);
    }

    [Test]
    public void Density_NegativeAltitude_ClampedToSeaLevel()
    {
        Assert.AreEqual(1.225f, AtmosphereModel.GetDensity(-100f), 1e-4f);
    }

    [Test]
    public void Density_Above85km_IsZero()
    {
        Assert.AreEqual(0f, AtmosphereModel.GetDensity(90000f));
    }

    [Test]
    public void Density_DecreasesWithAltitude()
    {
        float d0 = AtmosphereModel.GetDensity(0f);
        float d1 = AtmosphereModel.GetDensity(5000f);
        float d2 = AtmosphereModel.GetDensity(20000f);
        Assert.Greater(d0, d1);
        Assert.Greater(d1, d2);
        Assert.Greater(d2, 0f);
    }

    [Test]
    public void Gravity_AtSurface_Near980665()
    {
        Assert.AreEqual(9.80665f, AtmosphereModel.GetGravity(0f), 1e-4f);
    }

    [Test]
    public void Gravity_DecreasesWithAltitude()
    {
        float g0 = AtmosphereModel.GetGravity(0f);
        float gH = AtmosphereModel.GetGravity(2500f);
        Assert.Less(gH, g0);
        Assert.Greater(gH, 9.79f);
    }
}
