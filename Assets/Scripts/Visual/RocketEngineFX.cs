using UnityEngine;

/// <summary>
/// Візуальні ефекти двигуна: core/outer plume, дим, іскри, ground dust, light.
/// Інтенсивність ∝ currentThrust / maxThrust (згладжено).
/// Не чіпає velocityOverLifetime (різні mode X/Y/Z → Unity error).
/// </summary>
public class RocketEngineFX : MonoBehaviour
{
    public ParticleSystem flame;
    public ParticleSystem flameCore;
    public ParticleSystem smoke;
    public ParticleSystem sparks;
    public ParticleSystem dust;
    public Light engineLight;

    public float maxFlameRate = 240f;
    public float maxCoreRate = 140f;
    public float maxSmokeRate = 40f;
    public float maxSparkRate = 55f;
    public float maxDustRate = 75f;
    public float maxLightIntensity = 90f;
    public float lightRange = 120f;
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

        SetEmission(sparks, on ? smoothThrust * maxSparkRate * 0.5f : 0f, on);

        float dustFade = Mathf.Clamp01(1f - h / dustBelowAltitude);
        float dustRate = on ? smoothThrust * maxDustRate * dustFade * dustFade : 0f;
        SetEmission(dust, dustRate, on && dustRate > 1f);

        if (engineLight != null)
        {
            float flicker = on ? 0.88f + 0.12f * Mathf.PerlinNoise(Time.time * 32f, 0.4f) : 1f;
            engineLight.intensity = smoothThrust * maxLightIntensity * flicker;
            engineLight.range = lightRange * (0.55f + 0.45f * smoothThrust);
            engineLight.color = Color.Lerp(
                new Color(0.55f, 0.8f, 1f),
                new Color(1f, 0.72f, 0.35f),
                smoothThrust * 0.85f);
        }

        // Only startSpeed / startSize (single-axis) — never mix curve modes on VOL axes
        if (flame != null && on)
        {
            var main = flame.main;
            main.startSize3D = false;
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                28f + smoothThrust * 32f,
                55f + smoothThrust * 45f);
            main.startSize = new ParticleSystem.MinMaxCurve(
                0.75f + smoothThrust * 0.9f,
                1.8f + smoothThrust * 1.6f);
        }

        if (flameCore != null && on)
        {
            var main = flameCore.main;
            main.startSize3D = false;
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                48f + smoothThrust * 40f,
                85f + smoothThrust * 55f);
            main.startSize = new ParticleSystem.MinMaxCurve(
                0.3f + smoothThrust * 0.35f,
                0.75f + smoothThrust * 0.55f);
        }

        if (smoke != null && on)
        {
            var main = smoke.main;
            main.startSize3D = false;
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                10f + smoothThrust * 16f,
                20f + smoothThrust * 30f);
            main.startSize = new ParticleSystem.MinMaxCurve(
                1.4f + smoothThrust * 1.1f,
                3.0f + smoothThrust * 3.0f);
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
