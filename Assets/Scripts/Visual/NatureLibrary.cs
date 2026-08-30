using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Готові 3D-моделі природи (Kenney Nature Kit, CC0) з Resources/Nature.
/// </summary>
public static class NatureLibrary
{
    static bool _loaded;
    static readonly List<GameObject> Trees = new();
    static readonly List<GameObject> Pines = new();
    static readonly List<GameObject> Bushes = new();
    static readonly List<GameObject> Grass = new();
    static readonly List<GameObject> Flowers = new();
    static readonly List<GameObject> Rocks = new();
    static readonly List<GameObject> Stumps = new();
    static readonly List<GameObject> Mushrooms = new();

    public static bool Ready
    {
        get
        {
            EnsureLoaded();
            return Trees.Count + Bushes.Count + Rocks.Count > 0;
        }
    }

    public static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        var all = Resources.LoadAll<GameObject>("Nature");
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning("[NatureLibrary] Resources/Nature empty — open Unity once to import FBX, or check path.");
            return;
        }

        foreach (var go in all)
        {
            if (go == null) continue;
            string n = go.name.ToLowerInvariant();
            if (n.StartsWith("tree_pine") || n.Contains("cone"))
                Pines.Add(go);
            else if (n.StartsWith("tree_"))
                Trees.Add(go);
            else if (n.StartsWith("plant_bush") || n.Contains("bush"))
                Bushes.Add(go);
            else if (n.StartsWith("grass"))
                Grass.Add(go);
            else if (n.StartsWith("flower_"))
                Flowers.Add(go);
            else if (n.StartsWith("rock_"))
                Rocks.Add(go);
            else if (n.StartsWith("stump_") || n.StartsWith("log"))
                Stumps.Add(go);
            else if (n.StartsWith("mushroom_"))
                Mushrooms.Add(go);
        }

        Debug.Log($"[NatureLibrary] trees={Trees.Count} pines={Pines.Count} bushes={Bushes.Count} " +
                  $"grass={Grass.Count} flowers={Flowers.Count} rocks={Rocks.Count} stumps={Stumps.Count}");
    }

    /// <param name="bury">Додаткове заглиблення низу mesh у землю (м).</param>
    public static GameObject Spawn(Transform parent, string name, List<GameObject> pool, System.Random rng,
        float x, float z, float groundY, float uniformScale, float yRotDeg = 0f, float bury = 0.35f)
    {
        if (pool == null || pool.Count == 0) return null;
        var prefab = pool[rng.Next(pool.Count)];
        var go = Object.Instantiate(prefab, parent, false);
        go.name = name;

        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(0f, yRotDeg, 0f);
        go.transform.localScale = Vector3.one * Mathf.Max(0.01f, uniformScale);

        StripColliders(go);
        PrepareRenderers(go);

        // Свіжа висота mesh (raycast) + plant по local mesh bounds
        float gy = LunarTerrainMesh.SampleSurfaceY(x, z);
        PlantOnGround(go, x, z, gy, bury, uniformScale);
        return go;
    }

    /// <summary>
    /// Ставить root так, щоб мінімальний Y геометрії = groundY − bury.
    /// Рахує world-corners усіх MeshFilter (надійніше за Renderer.bounds у spawn-кадрі).
    /// </summary>
    public static void PlantOnGround(GameObject go, float x, float z, float groundY, float bury, float scale)
    {
        if (go == null) return;

        // Спочатку на точку (x, groundY, z)
        go.transform.position = new Vector3(x, groundY, z);

        float minY = float.PositiveInfinity;
        bool any = false;
        var filters = go.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            var mf = filters[i];
            if (mf == null || mf.sharedMesh == null) continue;
            var lb = mf.sharedMesh.bounds;
            // 8 кутів local bounds → world
            Vector3 c = lb.center;
            Vector3 e = lb.extents;
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 local = c + new Vector3(e.x * sx, e.y * sy, e.z * sz);
                float wy = mf.transform.TransformPoint(local).y;
                if (wy < minY) minY = wy;
                any = true;
            }
        }

        if (!any)
        {
            // fallback renderer bounds
            var rends = go.GetComponentsInChildren<Renderer>(true);
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++)
                    if (rends[i] != null) b.Encapsulate(rends[i].bounds);
                minY = b.min.y;
                any = true;
            }
        }

        if (!any) return;

        // bury масштабується з розміром моделі
        float sink = bury + scale * 0.04f;
        float dy = (groundY - sink) - minY;
        go.transform.position += new Vector3(0f, dy, 0f);
    }

    public static GameObject SpawnTree(Transform p, string name, System.Random rng, float x, float z, float gy, float scale, float yaw)
        => Spawn(p, name, Trees.Count > 0 ? Trees : Pines, rng, x, z, gy, scale, yaw, 0.55f);

    public static GameObject SpawnPine(Transform p, string name, System.Random rng, float x, float z, float gy, float scale, float yaw)
        => Spawn(p, name, Pines.Count > 0 ? Pines : Trees, rng, x, z, gy, scale, yaw, 0.6f);

    public static GameObject SpawnBush(Transform p, string name, System.Random rng, float x, float z, float gy, float scale, float yaw)
        => Spawn(p, name, Bushes, rng, x, z, gy, scale, yaw, 0.35f);

    public static GameObject SpawnGrass(Transform p, string name, System.Random rng, float x, float z, float gy, float scale, float yaw)
        => Spawn(p, name, Grass, rng, x, z, gy, scale, yaw, 0.15f);

    public static GameObject SpawnFlower(Transform p, string name, System.Random rng, float x, float z, float gy, float scale, float yaw)
        => Spawn(p, name, Flowers, rng, x, z, gy, scale, yaw, 0.1f);

    public static GameObject SpawnRock(Transform p, string name, System.Random rng, float x, float z, float gy, float scale, float yaw)
        => Spawn(p, name, Rocks, rng, x, z, gy, scale, yaw, 0.7f);

    public static GameObject SpawnStump(Transform p, string name, System.Random rng, float x, float z, float gy, float scale, float yaw)
        => Spawn(p, name, Stumps, rng, x, z, gy, scale, yaw, 0.45f);

    public static GameObject SpawnMushroom(Transform p, string name, System.Random rng, float x, float z, float gy, float scale, float yaw)
        => Spawn(p, name, Mushrooms, rng, x, z, gy, scale, yaw, 0.12f);

    static void StripColliders(GameObject root)
    {
        foreach (var c in root.GetComponentsInChildren<Collider>(true))
            Object.Destroy(c);
    }

    static void PrepareRenderers(GameObject root)
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Universal Render Pipeline/Simple Lit");

        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            // Обов’язково тіні від directional sun
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
            r.allowOcclusionWhenDynamic = true;
            if (lit == null) continue;

            var src = r.sharedMaterials;
            if (src == null || src.Length == 0) continue;
            var dst = new Material[src.Length];
            for (int i = 0; i < src.Length; i++)
                dst[i] = ToUrpLitInstance(src[i], lit);
            r.sharedMaterials = dst;
        }
    }

    static Material ToUrpLitInstance(Material src, Shader lit)
    {
        if (src == null) return VisualMaterials.Lit(EnvironmentTextures.FoliageGreen, 0f, 0.12f);

        Color col = Color.white;
        if (src.HasProperty("_Color")) col = src.GetColor("_Color");
        else if (src.HasProperty("_BaseColor")) col = src.GetColor("_BaseColor");
        else if (src.HasProperty("_MainColor")) col = src.GetColor("_MainColor");

        // Kenney leafsGreen ~ (0.16, 0.79, 0.67) — занадто яскравий teal; підганяємо під meadow
        col = HarmonizeNatureColor(col, src != null ? src.name : "");

        Texture main = null;
        if (src != null)
        {
            if (src.HasProperty("_MainTex")) main = src.GetTexture("_MainTex");
            else if (src.HasProperty("_BaseMap")) main = src.GetTexture("_BaseMap");
        }

        var m = new Material(lit);
        m.name = (src != null ? src.name : "Nature") + "_URP";
        // Opaque — transparent не кидає нормальні тіні в URP
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 0f);
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
        if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0f);
        if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 1f);
        m.SetOverrideTag("RenderType", "Opaque");
        m.renderQueue = -1;
        m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(col.r, col.g, col.b, 1f));
        if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(col.r, col.g, col.b, 1f));
        if (main != null)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", main);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", main);
        }
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        return m;
    }

    /// <summary>Приглушити neon Kenney greens / wood до палітри meadow ground.</summary>
    static Color HarmonizeNatureColor(Color c, string matName)
    {
        string n = (matName ?? "").ToLowerInvariant();
        float lum = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        float maxC = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        float minC = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
        float sat = maxC > 1e-4f ? (maxC - minC) / maxC : 0f;

        bool foliage = n.Contains("leaf") || n.Contains("foliage") || n.Contains("grass")
                    || n.Contains("bush") || n.Contains("plant") || n.Contains("pine")
                    || (c.g > c.r + 0.08f && c.g > c.b + 0.05f && sat > 0.25f);
        bool bark = n.Contains("wood") || n.Contains("bark") || n.Contains("trunk") || n.Contains("stump")
                    || n.Contains("log");
        bool rock = n.Contains("rock") || n.Contains("stone");
        bool flower = n.Contains("flower") || n.Contains("petal");
        bool dirt = n.Contains("dirt") || n.Contains("soil") || n.Contains("mud");

        if (foliage)
        {
            // Ціль: темно-оливкова / forest green як baked earth
            Color target = Color.Lerp(EnvironmentTextures.FoliageGreen, EnvironmentTextures.FoliageDark, 0.35f + lum * 0.25f);
            // Зберігаємо легкий відтінок, але гасимо brightness + teal
            return Color.Lerp(c * 0.45f, target, 0.82f);
        }
        if (bark)
        {
            Color target = EnvironmentTextures.BarkBrown;
            return Color.Lerp(c * 0.55f, target, 0.75f);
        }
        if (rock)
        {
            Color target = EnvironmentTextures.RockGrey;
            return Color.Lerp(c * 0.6f, target, 0.7f);
        }
        if (flower)
        {
            // Приглушені квіти, не neon
            return new Color(
                Mathf.Clamp01(c.r * 0.75f + 0.08f),
                Mathf.Clamp01(c.g * 0.7f + 0.05f),
                Mathf.Clamp01(c.b * 0.7f + 0.05f), 1f);
        }
        if (dirt)
            return Color.Lerp(c, EnvironmentTextures.SoilBrown, 0.65f);

        // Загальне: якщо дуже насичений green — pull to foliage
        if (c.g > 0.5f && c.g > c.r * 1.3f)
            return Color.Lerp(c * 0.5f, EnvironmentTextures.FoliageGreen, 0.8f);

        return new Color(c.r * 0.85f, c.g * 0.85f, c.b * 0.85f, 1f);
    }
}
