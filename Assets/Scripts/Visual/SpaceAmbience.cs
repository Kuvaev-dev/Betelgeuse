using UnityEngine;

/// <summary>
/// Легка атмосфера: мерехтіння сонця, twinkle зірок. Без drift зайвих об’єктів.
/// </summary>
public class SpaceAmbience : MonoBehaviour
{
    Light sun;
    float sunBase = 1.25f;
    readonly System.Collections.Generic.List<MeshRenderer> brightStars = new();
    readonly System.Collections.Generic.List<Color> starBaseEmit = new();
    readonly System.Collections.Generic.List<Light> padLights = new();
    MaterialPropertyBlock mpb;
    int emissionId = -1;
    float phase;

    public static SpaceAmbience Ensure()
    {
        var existing = FindAnyObjectByType<SpaceAmbience>();
        if (existing != null) return existing;
        var go = new GameObject("SpaceAmbience");
        return go.AddComponent<SpaceAmbience>();
    }

    void Awake() => EnsureMpb();

    void EnsureMpb()
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
        if (emissionId < 0) emissionId = Shader.PropertyToID("_EmissionColor");
    }

    public void Bind(Transform environmentRoot, ParticleSystem starPs, Light directionalSun)
    {
        _ = starPs;
        EnsureMpb();
        sun = directionalSun;
        if (sun != null) sunBase = sun.intensity;

        brightStars.Clear();
        starBaseEmit.Clear();
        padLights.Clear();
        if (environmentRoot == null) return;

        foreach (var t in environmentRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t == null) continue;
            if (t.name == "BStar" || t.name.StartsWith("BStar"))
            {
                var r = t.GetComponent<MeshRenderer>();
                if (r == null || r.sharedMaterial == null) continue;
                if (!r.sharedMaterial.HasProperty(emissionId)) continue;
                brightStars.Add(r);
                starBaseEmit.Add(r.sharedMaterial.GetColor(emissionId));
            }
        }

        foreach (var l in environmentRoot.GetComponentsInChildren<Light>(true))
        {
            if (l != null && l.type == LightType.Point && l.name.Contains("Pad"))
                padLights.Add(l);
        }
    }

    void LateUpdate()
    {
        EnsureMpb();
        float t = Time.time;

        if (sun != null)
            sun.intensity = sunBase * (0.94f + 0.1f * Mathf.PerlinNoise(t * 0.08f, 1.2f));

        phase += Time.deltaTime;
        for (int i = 0; i < padLights.Count; i++)
        {
            var b = padLights[i];
            if (b == null) continue;
            b.intensity = 20f * (0.9f + 0.1f * Mathf.Sin(phase * 1.5f + i));
        }

        int n = Mathf.Min(brightStars.Count, starBaseEmit.Count);
        for (int i = 0; i < n; i++)
        {
            var r = brightStars[i];
            if (r == null) continue;
            float tw = 0.75f + 0.3f * Mathf.PerlinNoise(t * 0.55f + i * 0.4f, i * 0.11f);
            mpb.Clear();
            mpb.SetColor(emissionId, starBaseEmit[i] * tw);
            r.SetPropertyBlock(mpb);
        }
    }
}
