using UnityEngine;

/// <summary>
/// Легка «жива» атмосфера космосу: мерехтіння сонця, повільний drift nebula.
/// </summary>
public class SpaceAmbience : MonoBehaviour
{
    ParticleSystem stars;
    Light sun;
    float sunBase = 1.4f;
    readonly System.Collections.Generic.List<Transform> nebulae = new();
    readonly System.Collections.Generic.List<Vector3> nebulaBaseScale = new();

    public static SpaceAmbience Ensure()
    {
        var existing = FindFirstObjectByType<SpaceAmbience>();
        if (existing != null) return existing;
        var go = new GameObject("SpaceAmbience");
        return go.AddComponent<SpaceAmbience>();
    }

    public void Bind(Transform environmentRoot, ParticleSystem starPs, Light directionalSun)
    {
        stars = starPs;
        sun = directionalSun;
        if (sun != null) sunBase = sun.intensity;

        nebulae.Clear();
        nebulaBaseScale.Clear();
        if (environmentRoot == null) return;

        foreach (var t in environmentRoot.GetComponentsInChildren<Transform>())
        {
            if (t != null && t.name.StartsWith("Nebula_"))
            {
                nebulae.Add(t);
                nebulaBaseScale.Add(t.localScale);
            }
        }
    }

    void LateUpdate()
    {
        if (sun != null)
        {
            float n = Mathf.PerlinNoise(Time.time * 0.12f, 1.7f);
            sun.intensity = sunBase * (0.95f + 0.1f * n);
        }

        float t = Time.time;
        for (int i = 0; i < nebulae.Count; i++)
        {
            var n = nebulae[i];
            if (n == null) continue;
            n.Rotate(Vector3.up, (0.4f + i * 0.05f) * Time.deltaTime, Space.World);
            float pulse = 1f + 0.025f * Mathf.Sin(t * 0.15f + i);
            n.localScale = nebulaBaseScale[i] * pulse;
        }
    }
}
