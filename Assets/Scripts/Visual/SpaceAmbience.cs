using UnityEngine;

/// <summary>
/// Легка «жива» атмосфера космосу:
/// — мерехтіння сонячної інтенсивності (Perlin);
/// — повільне обертання та пульсація туманностей.
/// Не впливає на фізику — лише візуальний шар.
/// </summary>
public class SpaceAmbience : MonoBehaviour
{
    Light sun;
    float sunBase = 1.4f;
    readonly System.Collections.Generic.List<Transform> nebulae = new();
    readonly System.Collections.Generic.List<Vector3> nebulaBaseScale = new();
    readonly System.Collections.Generic.List<Light> padBeacons = new();
    float beaconPhase;

    /// <summary>Створює або повертає єдиний екземпляр SpaceAmbience.</summary>
    public static SpaceAmbience Ensure()
    {
        var existing = FindFirstObjectByType<SpaceAmbience>();
        if (existing != null) return existing;
        var go = new GameObject("SpaceAmbience");
        return go.AddComponent<SpaceAmbience>();
    }

    /// <summary>
    /// Прив'язка до збудованого EnvironmentRoot після EnvironmentBuilder.Build().
    /// </summary>
    public void Bind(Transform environmentRoot, ParticleSystem starPs, Light directionalSun)
    {
        _ = starPs; // bind for future star twinkle
        sun = directionalSun;
        if (sun != null) sunBase = sun.intensity;

        nebulae.Clear();
        nebulaBaseScale.Clear();
        padBeacons.Clear();
        if (environmentRoot == null) return;

        foreach (var t in environmentRoot.GetComponentsInChildren<Transform>())
        {
            if (t == null) continue;
            if (t.name.StartsWith("Nebula_"))
            {
                nebulae.Add(t);
                nebulaBaseScale.Add(t.localScale);
            }
        }

        foreach (var l in environmentRoot.GetComponentsInChildren<Light>())
        {
            if (l != null && l.name.Contains("Beacon"))
                padBeacons.Add(l);
        }
    }

    void LateUpdate()
    {
        // М'яке мерехтіння «сонця»
        if (sun != null)
        {
            float n = Mathf.PerlinNoise(Time.time * 0.12f, 1.7f);
            sun.intensity = sunBase * (0.94f + 0.12f * n);
        }

        // Drift туманностей
        float t = Time.time;
        for (int i = 0; i < nebulae.Count; i++)
        {
            var n = nebulae[i];
            if (n == null) continue;
            n.Rotate(Vector3.up, (0.35f + i * 0.04f) * Time.deltaTime, Space.World);
            float pulse = 1f + 0.03f * Mathf.Sin(t * 0.14f + i * 0.7f);
            n.localScale = nebulaBaseScale[i] * pulse;
        }

        // Пульс маяків pad
        beaconPhase += Time.deltaTime;
        for (int i = 0; i < padBeacons.Count; i++)
        {
            var b = padBeacons[i];
            if (b == null) continue;
            float baseI = i == 0 ? 12f : 7f;
            b.intensity = baseI * (0.75f + 0.25f * Mathf.Sin(beaconPhase * 2.2f + i));
        }
    }
}
