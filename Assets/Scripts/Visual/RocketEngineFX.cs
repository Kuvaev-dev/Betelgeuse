using UnityEngine;

/// <summary>
/// Візуальні ефекти двигуна: core/outer plume, дим, іскри, ground dust, light.
/// Інтенсивність ∝ currentThrust / maxThrust (згладжено).
/// </summary>
public class RocketEngineFX : MonoBehaviour
{
    public ParticleSystem flame;
    public ParticleSystem flameCore;
    public ParticleSystem smoke;
    public ParticleSystem sparks;
    public ParticleSystem dust;
    public Light engineLight;

    public float maxFlameRate = 320f;
    public float maxCoreRate = 180f;
    public float maxSmokeRate = 48f;
    public float maxSparkRate = 110f;
    public float maxDustRate = 90f;
    public float maxLightIntensity = 110f;
    public float lightRange = 150f;
    public float smokeBoostBelowAltitude = 180f;
    public float dustBelowAltitude = 95f;

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

        smoothThrust = Mathf.Lerp(smoothThrust, target, 1f - Mathf.Exp(-16f * Time.deltaTime));
        bool on = smoothThrust > 0.02f;
        float h = rocket.state.position.y;

        SetEmission(flame, on ? smoothThrust * maxFlameRate : 0f, on);
        SetEmission(flameCore, on ? smoothThrust * maxCoreRate : 0f, on);

        float smokeMul = 0.25f + 0.75f * Mathf.Clamp01(1f - h / smokeBoostBelowAltitude);
        float smokeRate = on ? smoothThrust * maxSmokeRate * smokeMul : 0f;
        SetEmission(smoke, smokeRate, on && smokeRate > 0.4f);

        SetEmission(sparks, on ? smoothThrust * maxSparkRate * 0.55f : 0f, on);

        float dustFade = Mathf.Clamp01(1f - h / dustBelowAltitude);
        float dustRate = on ? smoothThrust * maxDustRate * dustFade * dustFade : 0f;
        SetEmission(dust, dustRate, on && dustRate > 1f);

        if (engineLight != null)
        {
            float flicker = on ? 0.86f + 0.14f * Mathf.PerlinNoise(Time.time * 38f, 0.4f) : 1f;
            engineLight.intensity = smoothThrust * maxLightIntensity * flicker;
            engineLight.range = lightRange * (0.55f + 0.45f * smoothThrust);
            // Methane/LOX Raptor-like: cool cyan core → warm amber at high throttle
            engineLight.color = Color.Lerp(
                new Color(0.45f, 0.78f, 1f),
                new Color(1f, 0.78f, 0.42f),
                smoothThrust * 0.9f);
        }

        if (flame != null && on)
        {
            var main = flame.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                38f + smoothThrust * 70f,
                70f + smoothThrust * 110f);
            main.startSize = new ParticleSystem.MinMaxCurve(
                1.1f + smoothThrust * 1.8f,
                2.6f + smoothThrust * 3.8f);
        }

        if (flameCore != null && on)
        {
            var main = flameCore.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                55f + smoothThrust * 90f,
                95f + smoothThrust * 140f);
            main.startSize = new ParticleSystem.MinMaxCurve(
                0.45f + smoothThrust * 0.7f,
                1.1f + smoothThrust * 1.4f);
        }

        if (smoke != null && on)
        {
            var main = smoke.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                10f + smoothThrust * 18f,
                22f + smoothThrust * 36f);
            main.startSize = new ParticleSystem.MinMaxCurve(
                1.4f + smoothThrust * 1.2f,
                3.2f + smoothThrust * 3.5f);
        }
    }

    void StopAll()
    {
        StopPs(flame);
        StopPs(flameCore);
        StopPs(smoke);
        StopPs(sparks);
        StopPs(dust);
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
