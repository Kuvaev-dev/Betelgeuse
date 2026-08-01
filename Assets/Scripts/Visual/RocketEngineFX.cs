using UnityEngine;

/// <summary>
/// Візуальні ефекти двигуна: полум'я, дим, іскри та point light.
/// Інтенсивність пропорційна currentThrust / maxThrust (згладжено).
/// Компонент вішається на Visual під час RocketVisualBuilder.Build.
/// </summary>
public class RocketEngineFX : MonoBehaviour
{
    public ParticleSystem flame;
    public ParticleSystem smoke;
    public ParticleSystem sparks;
    public Light engineLight;

    public float maxFlameRate = 200f;
    /// <summary>Дим — лише легкий шлейф, щоб не закривав корпус.</summary>
    public float maxSmokeRate = 18f;
    public float maxSparkRate = 70f;
    public float maxLightIntensity = 65f;
    public float lightRange = 100f;
    /// <summary>Нижче цієї висоти дим трохи сильніший (landing burn).</summary>
    public float smokeBoostBelowAltitude = 120f;

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
        bool on = smoothThrust > 0.02f;

        SetEmission(flame, on ? smoothThrust * maxFlameRate : 0f, on);

        // Дим: слабкий на висоті, трохи сильніший біля pad; ніколи не «стіна»
        float h = rocket.state.position.y;
        float smokeMul = 0.35f + 0.65f * Mathf.Clamp01(1f - h / smokeBoostBelowAltitude);
        float smokeRate = on ? smoothThrust * maxSmokeRate * smokeMul : 0f;
        SetEmission(smoke, smokeRate, on && smokeRate > 0.5f);

        SetEmission(sparks, on ? smoothThrust * maxSparkRate * 0.45f : 0f, on);

        if (engineLight != null)
        {
            float flicker = on ? 0.9f + 0.1f * Mathf.PerlinNoise(Time.time * 28f, 0.4f) : 1f;
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
                28f + smoothThrust * 50f,
                50f + smoothThrust * 80f);
            main.startSize = new ParticleSystem.MinMaxCurve(
                1.2f + smoothThrust * 1.8f,
                2.8f + smoothThrust * 3.5f);
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
