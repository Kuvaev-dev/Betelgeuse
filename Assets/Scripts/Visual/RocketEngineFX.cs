using UnityEngine;

/// <summary>
/// Полум'я / дим / іскри / світло двигуна — масштаб від тяги.
/// </summary>
public class RocketEngineFX : MonoBehaviour
{
    public ParticleSystem flame;
    public ParticleSystem smoke;
    public ParticleSystem sparks;
    public Light engineLight;

    public float maxFlameRate = 220f;
    public float maxSmokeRate = 70f;
    public float maxSparkRate = 90f;
    public float maxLightIntensity = 70f;
    public float lightRange = 110f;

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

        smoothThrust = Mathf.Lerp(smoothThrust, target, 1f - Mathf.Exp(-12f * Time.deltaTime));
        bool on = smoothThrust > 0.015f;

        SetEmission(flame, on ? smoothThrust * maxFlameRate : 0f, on);
        SetEmission(smoke, on ? smoothThrust * maxSmokeRate : 0f, on);
        SetEmission(sparks, on ? smoothThrust * maxSparkRate * 0.6f : 0f, on);

        if (engineLight != null)
        {
            float flicker = on ? 0.88f + 0.12f * Mathf.PerlinNoise(Time.time * 32f, 0.4f) : 1f;
            engineLight.intensity = smoothThrust * maxLightIntensity * flicker;
            engineLight.range = lightRange * (0.7f + 0.3f * smoothThrust);
            engineLight.color = Color.Lerp(
                new Color(1f, 0.3f, 0.06f),
                new Color(1f, 0.8f, 0.4f),
                smoothThrust);
        }

        if (flame != null && on)
        {
            var main = flame.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                30f + smoothThrust * 55f,
                55f + smoothThrust * 90f);
            main.startSize = new ParticleSystem.MinMaxCurve(
                1.3f + smoothThrust * 2.2f,
                3.2f + smoothThrust * 4.5f);
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
