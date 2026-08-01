using NUnit.Framework;
using UnityEngine;

public class PIDControllerTests
{
    [Test]
    public void Calculate_ZeroError_ReturnsNearZeroAfterSettle()
    {
        var pid = new PIDController { Kp = 1f, Ki = 0f, Kd = 0f };
        float u = pid.Calculate(10f, 10f, 0.01f);
        Assert.AreEqual(0f, u, 1e-5f);
    }

    [Test]
    public void Calculate_ProportionalOnly_ScalesWithError()
    {
        var pid = new PIDController { Kp = 2f, Ki = 0f, Kd = 0f };
        float u = pid.Calculate(10f, 6f, 0.01f);
        Assert.AreEqual(8f, u, 1e-4f);
    }

    [Test]
    public void Calculate_InvalidDt_ReturnsZero()
    {
        var pid = new PIDController { Kp = 1f, Ki = 1f, Kd = 1f };
        Assert.AreEqual(0f, pid.Calculate(1f, 0f, 0f));
        Assert.AreEqual(0f, pid.Calculate(1f, 0f, -0.1f));
    }

    [Test]
    public void Integral_IsClamped_AntiWindup()
    {
        var pid = new PIDController { Kp = 0f, Ki = 100f, Kd = 0f };
        for (int i = 0; i < 100; i++)
            pid.Calculate(100f, 0f, 0.1f);
        // integral clamped to ±15 → output = Ki * integral ∈ [-1500, 1500]
        float u = pid.Calculate(100f, 0f, 0.1f);
        Assert.LessOrEqual(Mathf.Abs(u), 1500f + 1e-2f);
    }

    [Test]
    public void Reset_ClearsInternalState()
    {
        var pid = new PIDController { Kp = 0f, Ki = 1f, Kd = 0f };
        pid.Calculate(5f, 0f, 0.1f);
        pid.Reset();
        float u = pid.Calculate(0f, 0f, 0.1f);
        Assert.AreEqual(0f, u, 1e-5f);
    }

    [Test]
    public void Derivative_RespondsToErrorChange()
    {
        var pid = new PIDController { Kp = 0f, Ki = 0f, Kd = 1f };
        pid.Calculate(0f, 0f, 0.1f);
        float u = pid.Calculate(0f, -1f, 0.1f); // error goes 0 → 1, dError/dt = 10
        Assert.AreEqual(10f, u, 1e-3f);
    }
}
