using UnityEngine;

/// <summary>
/// Визуал огня двигателей: core/outer plume, дым, искры, ground dust, light.
/// Интенсивность от currentThrust / maxThrust (сглаженно).
/// Не трогать velocityOverLifetime (режим X/Y/Z → Unity error).
/// </summary>
public class RocketEngineFX : MonoBehaviour
{
    public ParticleSystem flame;
    public ParticleSystem flameCore;
    public ParticleSystem[] plumes;
    public ParticleSystem[] cores;
    public ParticleSystem[] glows;
    public Transform[] jetRoots;
    public MeshRenderer[] jetRenderers;
    public Transform[] glowBalls;
    public ParticleSystem smoke;
    public ParticleSystem sparks;
    public ParticleSystem dust;
    public Light engineLight;

    public float maxFlameRate = 72f;
    public float maxCoreRate = 58f;
    public float maxGlowRate = 90f;
    public float maxSmokeRate = 40f;
    public float maxSparkRate = 14f;
    public float maxDustRate = 75f;
    public float maxLightIntensity = 165f;
    public float lightRange = 160f;
    public float smokeBoostBelowAltitude = 180f;
    public float dustBelowAltitude = 95f;

    RocketPhysics rocket;
    float smoothThrust;
    MaterialPropertyBlock mpb;
    Vector3[] glowBaseScale;

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

        float flameRate = on ? smoothThrust * maxFlameRate : 0f;
        float coreRate = on ? smoothThrust * maxCoreRate : 0f;
        float glowRate = on ? smoothThrust * maxGlowRate : 0f;
        if (plumes != null && plumes.Length > 0)
        {
            for (int i = 0; i < plumes.Length; i++)
                SetEmission(plumes[i], flameRate, on);
        }
        else
            SetEmission(flame, flameRate, on);
        if (cores != null && cores.Length > 0)
        {
            for (int i = 0; i < cores.Length; i++)
                SetEmission(cores[i], coreRate, on);
        }
        else
            SetEmission(flameCore, coreRate, on);
        if (glows != null)
        {
            for (int i = 0; i < glows.Length; i++)
                SetEmission(glows[i], glowRate, on);
        }

        float smokeMul = 0.25f + 0.75f * Mathf.Clamp01(1f - h / smokeBoostBelowAltitude);
        float smokeRate = on ? smoothThrust * maxSmokeRate * smokeMul : 0f;
        SetEmission(smoke, smokeRate, on && smokeRate > 0.4f);

        SetEmission(sparks, on ? smoothThrust * maxSparkRate * 0.5f : 0f, on);

        float dustFade = Mathf.Clamp01(1f - h / dustBelowAltitude);
        float dustRate = on ? smoothThrust * maxDustRate * dustFade * dustFade : 0f;
        SetEmission(dust, dustRate, on && dustRate > 1f);

        if (engineLight != null)
        {
            float flicker = on ? EngineFlicker(0.4f) : 1f;
            engineLight.intensity = smoothThrust * maxLightIntensity * flicker;
            engineLight.range = lightRange * (0.55f + 0.45f * smoothThrust);
            // Hotter / bluer at high throttle, warmer orange at low.
            engineLight.color = Color.Lerp(
                new Color(1f, 0.42f, 0.10f),
                new Color(0.85f, 0.92f, 1.00f),
                0.15f + 0.55f * smoothThrust);
        }

        DriveJets(on);

        // Только startSpeed / startSize (single-axis) — избегать кривых VOL.
        void Tune(ParticleSystem ps, float spd0, float spd1, float sz0, float sz1)
        {
            if (ps == null) return;
            var main = ps.main;
            main.startSize3D = false;
            main.startSpeed = new ParticleSystem.MinMaxCurve(spd0, spd1);
            main.startSize = new ParticleSystem.MinMaxCurve(sz0, sz1);
        }
        if (on)
        {
            // Length via speed; width via size — both track throttle with soft floor.
            float thrustCurve = Mathf.Pow(smoothThrust, 0.72f);
            float f0 = 74f + thrustCurve * 58f, f1 = 120f + thrustCurve * 70f;
            float fs0 = 0.16f + thrustCurve * 0.14f, fs1 = 0.38f + thrustCurve * 0.22f;
            float c0 = 108f + thrustCurve * 62f, c1 = 165f + thrustCurve * 72f;
            float cs0 = 0.055f + thrustCurve * 0.055f, cs1 = 0.14f + thrustCurve * 0.08f;
            if (plumes != null)
                for (int i = 0; i < plumes.Length; i++) Tune(plumes[i], f0, f1, fs0, fs1);
            else
                Tune(flame, f0, f1, fs0, fs1);
            if (cores != null)
                for (int i = 0; i < cores.Length; i++) Tune(cores[i], c0, c1, cs0, cs1);
            else
                Tune(flameCore, c0, c1, cs0, cs1);
            if (glows != null)
            {
                float g0 = 1.1f + smoothThrust * 2.2f, g1 = 3.8f + smoothThrust * 5.5f;
                float gs0 = 0.42f + smoothThrust * 0.22f, gs1 = 0.95f + smoothThrust * 0.40f;
                for (int i = 0; i < glows.Length; i++) Tune(glows[i], g0, g1, gs0, gs1);
            }
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

    static float EngineFlicker(float seed)
    {
        float t = Time.time;
        float a = Mathf.PerlinNoise(t * 23f, seed);
        float b = Mathf.PerlinNoise(t * 51f, seed + 2.7f);
        float c = Mathf.PerlinNoise(t * 9f, seed + 5.1f);
        return 0.82f + 0.12f * a + 0.06f * b * c;
    }

    void DriveJets(bool on)
    {
        float flick = on ? EngineFlicker(0.3f) : 0f;
        // Length grows faster than width with throttle (real plume behavior).
        float lenK = on ? Mathf.Lerp(0.16f, 1.08f, Mathf.Pow(smoothThrust, 0.42f)) * flick : 0.04f;
        float radK = on ? Mathf.Lerp(0.52f, 1.02f, Mathf.Pow(smoothThrust, 0.65f)) : 0.2f;
        float scroll = Time.time;
        float noiseAmt = on ? Mathf.Lerp(0.28f, 0.52f, smoothThrust) : 0.2f;
        float tipSmoke = on ? Mathf.Lerp(0.72f, 0.42f, smoothThrust) : 0.6f;
        float softness = on ? Mathf.Lerp(1.55f, 1.2f, smoothThrust) : 1.4f;

        if (jetRoots != null)
        {
            for (int i = 0; i < jetRoots.Length; i++)
            {
                if (jetRoots[i] == null) continue;
                float wLen = 1f + 0.10f * Mathf.Sin(scroll * 19f + i * 1.7f)
                    + 0.05f * Mathf.Sin(scroll * 43f + i * 0.9f);
                float wRad = 1f + 0.12f * Mathf.Sin(scroll * 27f + i * 2.3f)
                    + 0.04f * Mathf.PerlinNoise(scroll * 7f, i * 0.37f);
                jetRoots[i].localScale = new Vector3(radK * wRad, lenK * wLen, radK * (2f - wRad));
                // Tiny lateral shimmer (heat haze proxy without post FX).
                float shimX = on ? 0.6f * Mathf.Sin(scroll * 31f + i * 1.1f) * smoothThrust : 0f;
                float shimZ = on ? 0.6f * Mathf.Sin(scroll * 37f + i * 1.9f) * smoothThrust : 0f;
                jetRoots[i].localRotation = Quaternion.Euler(180f + shimX, shimZ * 8f, shimX * 6f);
                jetRoots[i].gameObject.SetActive(on);
            }
        }

        if (jetRenderers != null)
        {
            mpb ??= new MaterialPropertyBlock();
            for (int i = 0; i < jetRenderers.Length; i++)
            {
                var r = jetRenderers[i];
                if (r == null) continue;
                bool isCore = (i & 1) == 1;
                float baseI = isCore ? 6.0f : 3.2f;
                float localFlick = flick * (0.94f + 0.06f * Mathf.PerlinNoise(scroll * 29f, i * 0.17f));
                r.GetPropertyBlock(mpb);
                mpb.SetFloat("_Intensity", on ? baseI * smoothThrust * localFlick : 0f);
                mpb.SetFloat("_Flicker", localFlick);
                mpb.SetFloat("_Scroll", scroll * (1.15f + 0.12f * (i & 1)));
                mpb.SetFloat("_NoiseAmt", noiseAmt * (isCore ? 0.85f : 1.05f));
                mpb.SetFloat("_Softness", softness);
                mpb.SetFloat("_EdgePower", 1.75f);
                mpb.SetFloat("_TipSmoke", tipSmoke * (isCore ? 0.55f : 1f));
                r.SetPropertyBlock(mpb);
                r.enabled = on;
            }
        }

        if (glowBalls != null)
        {
            if (glowBaseScale == null || glowBaseScale.Length != glowBalls.Length)
            {
                glowBaseScale = new Vector3[glowBalls.Length];
                for (int i = 0; i < glowBalls.Length; i++)
                    glowBaseScale[i] = glowBalls[i] != null ? glowBalls[i].localScale : Vector3.one;
            }
            float gk = on ? (0.40f + 0.75f * smoothThrust) * (0.88f + 0.12f * flick) : 0.01f;
            for (int i = 0; i < glowBalls.Length; i++)
            {
                if (glowBalls[i] == null) continue;
                glowBalls[i].localScale = glowBaseScale[i] * gk;
                glowBalls[i].gameObject.SetActive(on);
            }
        }
    }

    void StopAll()
    {
        StopPs(flame);
        StopPs(flameCore);
        if (plumes != null) for (int i = 0; i < plumes.Length; i++) StopPs(plumes[i]);
        if (cores != null) for (int i = 0; i < cores.Length; i++) StopPs(cores[i]);
        if (glows != null) for (int i = 0; i < glows.Length; i++) StopPs(glows[i]);
        StopPs(smoke);
        StopPs(sparks);
        StopPs(dust);
        DriveJets(false);
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
