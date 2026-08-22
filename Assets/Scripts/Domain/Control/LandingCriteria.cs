using UnityEngine;

/// <summary>
/// Критерії soft-landing (єдине джерело правди для finish path і GATE UI).
/// </summary>
public static class LandingCriteria
{
    public const float DefaultMaxTouchdownVelocity = 3.5f;
    public const float DefaultMaxLandingAngle = 7f;
    public const float DefaultMaxHorizontalMiss = 25f;
    public const float DefaultMaxHorizontalSpeed = 5f;

    public readonly struct Limits
    {
        public readonly float MaxV;
        public readonly float MaxAngle;
        public readonly float MaxMiss;
        public readonly float MaxHSpeed;

        public Limits(float maxV, float maxAngle, float maxMiss, float maxHSpeed)
        {
            MaxV = maxV > 0.1f ? maxV : DefaultMaxTouchdownVelocity;
            MaxAngle = maxAngle > 0.1f ? maxAngle : DefaultMaxLandingAngle;
            MaxMiss = maxMiss > 0.1f ? maxMiss : DefaultMaxHorizontalMiss;
            MaxHSpeed = maxHSpeed > 0.1f ? maxHSpeed : DefaultMaxHorizontalSpeed;
        }

        public static Limits FromParameters(SimulationParameters p)
        {
            if (p == null)
                return new Limits(DefaultMaxTouchdownVelocity, DefaultMaxLandingAngle,
                    DefaultMaxHorizontalMiss, DefaultMaxHorizontalSpeed);
            return new Limits(p.maxTouchdownVelocity, p.maxLandingAngle,
                p.maxHorizontalMiss, p.maxHorizontalSpeed);
        }
    }

    public static bool IsSuccessful(LandingMetrics m, in Limits lim)
    {
        if (m == null || m.timedOut) return false;
        return m.touchdownVelocity < lim.MaxV
            && m.landingAngleError < lim.MaxAngle
            && m.horizontalMiss < lim.MaxMiss
            && m.horizontalSpeed < lim.MaxHSpeed;
    }

    public static void ApplySuccessFlag(LandingMetrics m, SimulationParameters p)
    {
        if (m == null) return;
        m.isSuccessfulLanding = IsSuccessful(m, Limits.FromParameters(p));
    }
}
