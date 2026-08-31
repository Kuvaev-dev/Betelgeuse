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
            float flicker = on ? 0.88f + 0.12f * Mathf.PerlinNoise(Time.time * 32f, 0.4f) : 1f;
            engineLight.intensity = smoothThrust * maxLightIntensity * flicker;
            engineLight.range = lightRange * (0.55f + 0.45f * smoothThrust);
            engineLight.color = Color.Lerp(
                new Color(1f, 0.38f, 0.08f),
                new Color(1f, 0.78f, 0.32f),
                0.25f + 0.75f * smoothThrust);
        }

        DriveJets(on);

        // Лише startSpeed / startSize (single-axis) — ніколи не змішувати curve modes на осях VOL
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
            float f0 = 70f + smoothThrust * 45f, f1 = 115f + smoothThrust * 50f;
            float fs0 = 0.28f + smoothThrust * 0.18f, fs1 = 0.62f + smoothThrust * 0.28f;
            float c0 = 100f + smoothThrust * 50f, c1 = 155f + smoothThrust * 55f;
            float cs0 = 0.09f + smoothThrust * 0.07f, cs1 = 0.22f + smoothThrust * 0.10f;
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
                float g0 = 1.2f + smoothThrust * 2f, g1 = 4f + smoothThrust * 5f;
                float gs0 = 0.5f + smoothThrust * 0.25f, gs1 = 1.05f + smoothThrust * 0.45f;
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


    void DriveJets(bool on)
    {
        float flick = on
            ? 0.86f + 0.14f * Mathf.PerlinNoise(Time.time * 19f, 0.3f)
              * (0.55f + 0.45f * Mathf.PerlinNoise(Time.time * 47f, 1.8f))
            : 0f;
        float lenK = on ? Mathf.Lerp(0.18f, 1f, Mathf.Pow(smoothThrust, 0.48f)) : 0.04f;
        float radK = on ? Mathf.Lerp(0.58f, 1f, smoothThrust) : 0.2f;
        float scroll = Time.time;

        if (jetRoots != null)
        {
            for (int i = 0; i < jetRoots.Length; i++)
            {
                if (jetRoots[i] == null) continue;
                float wLen = 1f + 0.08f * Mathf.Sin(scroll * 19f + i * 1.7f);
                float wRad = 1f + 0.10f * Mathf.Sin(scroll * 27f + i * 2.3f);
                jetRoots[i].localScale = new Vector3(radK * wRad, lenK * wLen, radK * (2f - wRad));
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
                float baseI = (i & 1) == 0 ? 2.7f : 5.1f;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat("_Intensity", on ? baseI * smoothThrust * flick : 0f);
                mpb.SetFloat("_Flicker", flick);
                mpb.SetFloat("_Scroll", scroll * (1.15f + 0.12f * (i & 1)));
                mpb.SetFloat("_NoiseAmt", 0.42f);
                r.SetPropertyBlock(mpb);
                if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Scroll"))
                    r.sharedMaterial.SetFloat("_Scroll", scroll * (1.1f + 0.15f * (i & 1)));
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
            float gk = on ? (0.42f + 0.70f * smoothThrust) * (0.9f + 0.1f * flick) : 0.01f;
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
