using UnityEngine;

/// <summary>
/// Візуальні ефекти двигуна: полум'я (core + outer), дим, іскри, light.
/// Інтенсивність ∝ currentThrust / maxThrust (згладжено).
/// </summary>
public class RocketEngineFX : MonoBehaviour
{
    public ParticleSystem flame;
    public ParticleSystem smoke;
    public ParticleSystem sparks;
    public Light engineLight;

    public float maxFlameRate = 240f;
    public float maxSmokeRate = 22f;
    public float maxSparkRate = 80f;
    public float maxLightIntensity = 80f;
    public float lightRange = 120f;
    public float smokeBoostBelowAltitude = 140f;

    RocketPhysics rocket;
    float smoothThrust;

    void Start()
    {
        rocket = GetComponentInParent<RocketPhysics>();
        StopAll();
        if (engineLight != null)
        {
            engineLight.intensity = 0f;
            engineLight.range = lightRange;
        }
    }

    void LateUpdate()
    {
        if (rocket == null)
        {
            rocket = GetComponentInParent<RocketPhysics>();
            if (rocket == null) return;
        }

        float maxT = Mathf.Max(1f, rocket.state.maxThrust);
        bool dead = rocket.state.simulationFinished || rocket.state.isLanded || !rocket.simulationArmed;
        float target = dead ? 0f : Mathf.Clamp01(rocket.state.currentThrust / maxT);

        smoothThrust = Mathf.Lerp(smoothThrust, target, 1f - Mathf.Exp(-14f * Time.deltaTime));
        bool on = smoothThrust > 0.02f;

        SetEmission(flame, on ? smoothThrust * maxFlameRate : 0f, on);

        float h = rocket.state.position.y;
        float smokeMul = 0.3f + 0.7f * Mathf.Clamp01(1f - h / smokeBoostBelowAltitude);
        float smokeRate = on ? smoothThrust * maxSmokeRate * smokeMul : 0f;
        SetEmission(smoke, smokeRate, on && smokeRate > 0.5f);

        SetEmission(sparks, on ? smoothThrust * maxSparkRate * 0.5f : 0f, on);

        if (engineLight != null)
        {
            float flicker = on ? 0.88f + 0.12f * Mathf.PerlinNoise(Time.time * 32f, 0.4f) : 1f;
            engineLight.intensity = smoothThrust * maxLightIntensity * flicker;
            engineLight.range = lightRange * (0.65f + 0.35f * smoothThrust);
            // Raptor-like: cool core → warm outer
            engineLight.color = Color.Lerp(
                new Color(0.35f, 0.65f, 1f),
                new Color(1f, 0.72f, 0.35f),
                smoothThrust * 0.85f);
        }

        if (flame != null && on)
        {
            var main = flame.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                32f + smoothThrust * 55f,
                55f + smoothThrust * 90f);
            main.startSize = new ParticleSystem.MinMaxCurve(
                1.3f + smoothThrust * 2.0f,
                3.0f + smoothThrust * 4.0f);
        }
    }

    void StopAll()
    {
        StopPs(flame);
        StopPs(smoke);
        StopPs(sparks);
    }

    static void StopPs(ParticleSystem ps)
    {
        if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    static void SetEmission(ParticleSystem ps, float rate, bool play)
    {
        if (ps == null) return;
        var em = ps.emission;
        em.rateOverTime = rate;
        if (play && !ps.isPlaying) ps.Play();
        if (!play && ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}
