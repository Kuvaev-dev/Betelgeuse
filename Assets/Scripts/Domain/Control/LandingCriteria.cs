/// <summary>
/// Критерії soft-landing 1-го ступеня — єдине джерело правди для симуляції, UI та звітів.
/// </summary>
public static class LandingCriteria
{
    public const float DefaultMaxTouchdownVelocity = 3.5f;
    public const float DefaultMaxLandingAngle = 7f;
    public const float DefaultMaxHorizontalMiss = 40f;
    public const float DefaultMaxHorizontalSpeed = 6.5f;

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

        public static Limits Default => new Limits(
            DefaultMaxTouchdownVelocity, DefaultMaxLandingAngle,
            DefaultMaxHorizontalMiss, DefaultMaxHorizontalSpeed);
    }

    public static bool IsSuccessful(LandingMetrics m, in Limits lim)
    {
        if (m == null || m.timedOut) return false;
        return m.touchdownVelocity < lim.MaxV
            && m.landingAngleError < lim.MaxAngle
            && m.horizontalMiss < lim.MaxMiss
            && m.horizontalSpeed < lim.MaxHSpeed;
    }
}
