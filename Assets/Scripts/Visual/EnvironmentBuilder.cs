using System.Collections;
using UnityEngine;

/// <summary>
/// ÃƒÂÃ‚Â¡ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂµÃƒÂÃ‚Â´ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â°ÃƒÂÃ‚Âµ ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â¾Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â°ÃƒÂÃ‚Â´ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¸: ÃƒÂÃ‚Â·ÃƒÂÃ‚ÂµÃƒÂÃ‚Â¼ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ ÃƒÂÃ‚Â°ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â´Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¼ / ÃƒÂÃ‚Â±ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° LZ, ÃƒÂÃ‚Â³ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â·ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Å¡, ÃƒÂÃ‚Â½ÃƒÂÃ‚ÂµÃƒÂÃ‚Â±ÃƒÂÃ‚Â¾, pad, ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂºÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸.
/// </summary>
public static class EnvironmentBuilder
{
    /// <summary>ÃƒÂÃ‚Â¡ÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²ÃƒÂÃ‚Â° ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â·ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã‚Â ÃƒÂÃ‚Â´ÃƒÂÃ‚Â¸Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂºÃƒÂÃ‚Â° ÃƒÂÃ‚Â¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€˜Ã‚Â (Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¿Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° ÃƒÂÃ‚Â´ÃƒÂÃ‚Â»Ãƒâ€˜Ã‚Â Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â»ÃƒÂÃ‚Â° ÃƒÂÃ‚Â¹ ÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â·Ãƒâ€˜Ã†â€™ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã†â€™).</summary>
    public static readonly Vector3 SunWorldPosition = new Vector3(-3600f, 3400f, -2800f);

    public static void Build()
    {
        LunarTerrainMesh.EarthSurface = true;
        LunarTerrainMesh.Drain(BuildRoutine());
    }

    /// <summary>ÃƒÂÃ…Â¸ÃƒÂÃ‚Â¾ÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²ÃƒÂÃ‚Â° ÃƒÂÃ‚Â·ÃƒÂÃ‚Â±Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂºÃƒÂÃ‚Â° ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â yield + splash progress (0.50ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦0.78).</summary>
    public static IEnumerator BuildRoutine()
    {
        LunarTerrainMesh.EarthSurface = true;
        Splash(0.52f, "ÃƒÂÃ…Â¾Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â»ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½ÃƒÂÃ‚Â½Ãƒâ€˜Ã‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦", "LightingÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦");
        SetupLighting(out Light sun);
        SetupSkyAndFog();
        yield return null;

        var existing = GameObject.Find("EnvironmentRoot");
        if (existing != null)
            Object.Destroy(existing);

        var root = new GameObject("EnvironmentRoot");

        Splash(0.56f, "ÃƒÂÃ‚Â ÃƒÂÃ‚ÂµÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™Ãƒâ€˜Ã¢â‚¬ÂÃƒâ€˜Ã¢â‚¬Å¾ Earth LZÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦", "Earth LZ terrainÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦");
        yield return null;
        yield return BuildEarthSurfaceRoutine(root.transform);

        Splash(0.72f, "ÃƒÂÃ…Â¸ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â‚¬Â°ÃƒÂÃ‚Â°ÃƒÂÃ‚Â´ÃƒÂÃ‚ÂºÃƒÂÃ‚Â° / ÃƒÂÃ‚Â½ÃƒÂÃ‚ÂµÃƒÂÃ‚Â±ÃƒÂÃ‚Â¾ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦", "Pad / skyÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦");
        BuildLandingPad(root.transform);
        yield return null;
        BuildSkyGradientDome(root.transform);
        yield return null;
        Splash(0.75f, "ÃƒÂÃ‚Â¥ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦", "CloudsÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦");
        BuildSoftClouds(root.transform);
        BuildSunDisc(root.transform);
        BuildApproachLights(root.transform);
        yield return null;
        var amb = SpaceAmbience.Ensure();
        amb.Bind(root.transform, null, sun);
        Splash(0.78f, "ÃƒÂÃ‚Â¡ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂµÃƒÂÃ‚Â´ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â°ÃƒÂÃ‚Âµ ÃƒÂ¢Ã…â€œÃ¢â‚¬Å“", "Environment ÃƒÂ¢Ã…â€œÃ¢â‚¬Å“");
        yield return null;
    }

    static void Splash(float t, string uk, string en)
    {
        var s = SplashScreenUI.Instance;
        if (s != null)
            s.SetProgress(t, UILocale.IsUK ? uk : en);
    }

    static IEnumerator BuildEarthSurfaceRoutine(Transform parent)
    {
        var surface = new GameObject("EarthSurface");
        surface.transform.SetParent(parent, false);

        Splash(0.58f, "ÃƒÂÃ‚Â¢ÃƒÂÃ‚ÂµÃƒÂÃ‚ÂºÃƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã†â€™Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ ÃƒÂÃ‚Â·ÃƒÂÃ‚ÂµÃƒÂÃ‚Â¼ÃƒÂÃ‚Â»Ãƒâ€˜Ã¢â‚¬â€œÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦", "Ground texturesÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦");
        yield return null;
        EnvironmentTextures.EnsureLoaded();
        var ground = EnvironmentTextures.MakeGroundMaterial();

        float R = LunarTerrainMesh.TerrainRadius;
        int res = QualitySettings.GetQualityLevel() <= 1 ? 220 : 320;
        Splash(0.60f, "ÃƒÂÃ…â€œÃƒÂÃ‚ÂµÃƒâ€˜Ã‹â€  Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂµÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™Ãƒâ€˜Ã¢â‚¬ÂÃƒâ€˜Ã¢â‚¬Å¾Ãƒâ€˜Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦", "Terrain meshÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦");
        yield return null;
        yield return LunarTerrainMesh.CreateRoutine(surface.transform, ground, null, res, R);

        Physics.SyncTransforms();
        yield return null;

        Splash(0.66f, "ÃƒÂÃ…Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â´ÃƒÂÃ‚Â°ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦", "Nature propsÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦");
        yield return null;
        yield return BuildNatureProps(surface.transform);

        Splash(0.71f, "ÃƒÂÃ…Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â´ÃƒÂÃ‚Â° ÃƒÂ¢Ã…â€œÃ¢â‚¬Å“", "Nature ÃƒÂ¢Ã…â€œÃ¢â‚¬Å“");
        yield return null;
        FitShadowsToFocusDepth(420f);
    }

    /// <summary>
    /// ÃƒÂÃ…Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â´ÃƒÂÃ‚Â° ÃƒÂÃ‚Â· Kenney Nature Kit (FBX Ãƒâ€˜Ã†â€™ Resources/Nature).
    /// ÃƒÂÃ…Â¸ÃƒÂÃ‚Â¾Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â°ÃƒÂÃ‚Â´ÃƒÂÃ‚ÂºÃƒÂÃ‚Â° ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° smoothed terrain height.
    /// </summary>
    static IEnumerator BuildNatureProps(Transform parent)
    {
        NatureLibrary.EnsureLoaded();
        if (!NatureLibrary.Ready)
        {
            Debug.LogWarning("[Nature] FBX not imported yet ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â skip props this run. Re-enter Play after Unity imports Resources/Nature.");
            yield break;
        }

        var rng = new System.Random(47);
        float clear = LunarTerrainMesh.PadClearRadius + 22f;
        float R = LunarTerrainMesh.TerrainRadius * 0.88f;
        bool low = QualitySettings.GetQualityLevel() <= 1;

        const float treeScale = 8.5f;
        const float pineScale = 10.5f;
        const float bushScale = 5.0f;
        const float grassScale = 3.6f;
        const float flowerScale = 2.6f;
        const float rockScale = 4.6f;
        const float stumpScale = 4.2f;

        float Yaw() => (float)rng.NextDouble() * 360f;
        float Gy(float x, float z) => LunarTerrainMesh.SampleSurfaceY(x, z);

        // ÃƒÂÃ…Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â³Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂµÃƒâ€˜Ã‚Â 0.66ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦0.71 ÃƒÂÃ‚Â¿Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´ Ãƒâ€˜Ã¢â‚¬Â¡ÃƒÂÃ‚Â°Ãƒâ€˜Ã‚Â ÃƒÂÃ‚Â¿Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â´ÃƒÂÃ‚Â¸ (ÃƒÂÃ‚Â±ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â€šÂ¬ ÃƒÂÃ‚Â½ÃƒÂÃ‚Âµ Ãƒâ€šÃ‚Â«ÃƒÂÃ‚Â·ÃƒÂÃ‚Â°Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â³ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â‚¬Â/ÃƒÂÃ‚Â·ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚ÂºÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â‚¬ÂÃƒâ€šÃ‚Â»)
        float natureT = 0.66f;
        void NatureProg(string uk, string en)
        {
            natureT = Mathf.Min(0.705f, natureT + 0.004f);
            Splash(natureT, uk, en);
        }

        // ÃƒÂÃ…Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â´ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â·ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¸ÃƒÂÃ‚Â´ ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â¾ ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â‚¬Â°Ãƒâ€˜Ã¢â‚¬â€œ (sqrt) + jitter
        void NaturalPoint(float d0, float d1, out float x, out float z)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float t = (float)rng.NextDouble();
            float dist = Mathf.Sqrt(Mathf.Lerp(d0 * d0, d1 * d1, t));
            float cx = Mathf.Cos(ang) * dist;
            float cz = Mathf.Sin(ang) * dist;
            float u1 = Mathf.Max(1e-4f, (float)rng.NextDouble());
            float u2 = (float)rng.NextDouble();
            float mag = Mathf.Sqrt(-2f * Mathf.Log(u1)) * (8f + (float)rng.NextDouble() * 18f);
            float phi = u2 * Mathf.PI * 2f;
            x = cx + Mathf.Cos(phi) * mag;
            z = cz + Mathf.Sin(phi) * mag;
            float d = Mathf.Sqrt(x * x + z * z);
            if (d < clear + 2f)
            {
                float k = (clear + 4f + (float)rng.NextDouble() * 10f) / Mathf.Max(0.01f, d);
                x *= k;
                z *= k;
            }
        }

        // ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ ÃƒÂÃ¢â‚¬Å“Ãƒâ€˜Ã†â€™Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€˜Ã‚Â: ÃƒÂÃ‚Â½ÃƒÂÃ‚ÂµÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€šÃ‚Â«ÃƒÂÃ‚Â¿Ãƒâ€˜Ã†â€™Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã‚ÂÃƒâ€šÃ‚Â» ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬
        // Approach path
        for (int i = 0; i < (low ? 40 : 70); i++)
        {
            float z = clear + 15f + (float)rng.NextDouble() * 380f;
            float side = (i % 2 == 0) ? 1f : -1f;
            float x = side * (26f + (float)rng.NextDouble() * 50f);
            NatureLibrary.SpawnBush(parent, $"PathBush_{i}", rng, x, z, Gy(x, z),
                bushScale * (0.65f + (float)rng.NextDouble() * 0.55f), Yaw());
            float gx = x + side * (2f + (float)rng.NextDouble() * 8f);
            float gz = z + ((float)rng.NextDouble() - 0.5f) * 10f;
            NatureLibrary.SpawnGrass(parent, $"PathGrass_{i}", rng, gx, gz, Gy(gx, gz),
                grassScale * (0.8f + (float)rng.NextDouble() * 0.5f), Yaw());
            if (i % 4 == 0)
            {
                float fx = x + side * 3f, fz = z - 4f;
                NatureLibrary.SpawnFlower(parent, $"PathFlower_{i}", rng, fx, fz, Gy(fx, fz),
                    flowerScale * 0.9f, Yaw());
            }
            if ((i & 3) == 0) { NatureProg("ÃƒÂÃ…Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â´ÃƒÂÃ‚Â°: Ãƒâ€˜Ã‹â€ ÃƒÂÃ‚Â»Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Â¦ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦", "Nature: pathÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦"); yield return null; }
        }

        // ÃƒÂÃ¢â‚¬ËœÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¶ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ‚ÂºÃƒâ€˜Ã†â€™Ãƒâ€˜Ã¢â‚¬Â°Ãƒâ€˜Ã¢â‚¬â€œ (natural area scatter)
        int nBushNear = low ? 100 : 180;
        for (int i = 0; i < nBushNear; i++)
        {
            NaturalPoint(clear + 4f, clear + 130f, out float x, out float z);
            NatureLibrary.SpawnBush(parent, $"BushNear_{i}", rng, x, z, Gy(x, z),
                bushScale * (0.55f + (float)rng.NextDouble() * 0.7f), Yaw());
            if ((i & 7) == 0) { NatureProg("ÃƒÂÃ…Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â´ÃƒÂÃ‚Â°: ÃƒÂÃ‚ÂºÃƒâ€˜Ã†â€™Ãƒâ€˜Ã¢â‚¬Â°Ãƒâ€˜Ã¢â‚¬â€œÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦", "Nature: bushesÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦"); yield return null; }
        }

        // ÃƒÂÃ‚Â¢Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â°ÃƒÂÃ‚Â²ÃƒÂÃ‚Â° ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ÃƒÂÃ‚Â³Ãƒâ€˜Ã†â€™Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾, Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â²ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¼Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¾ ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â¾ ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â‚¬Â°Ãƒâ€˜Ã¢â‚¬â€œ
        int nGrass = low ? 280 : 480;
        for (int i = 0; i < nGrass; i++)
        {
            NaturalPoint(clear + 2f, clear + 650f, out float x, out float z);
            NatureLibrary.SpawnGrass(parent, $"Grass_{i}", rng, x, z, Gy(x, z),
                grassScale * (0.45f + (float)rng.NextDouble() * 0.85f), Yaw());
            if ((i & 15) == 0) { NatureProg("ÃƒÂÃ…Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â´ÃƒÂÃ‚Â°: Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â°ÃƒÂÃ‚Â²ÃƒÂÃ‚Â°ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦", "Nature: grassÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦"); yield return null; }
        }

        int nFlower = low ? 90 : 160;
        for (int i = 0; i < nFlower; i++)
        {
            NaturalPoint(clear + 5f, clear + 340f, out float x, out float z);
            NatureLibrary.SpawnFlower(parent, $"Flower_{i}", rng, x, z, Gy(x, z),
                flowerScale * (0.55f + (float)rng.NextDouble() * 0.65f), Yaw());
            if ((i & 7) == 0) yield return null;
        }

        int nBushMid = low ? 90 : 160;
        for (int i = 0; i < nBushMid; i++)
        {
            NaturalPoint(clear + 70f, R * 0.65f, out float x, out float z);
            NatureLibrary.SpawnBush(parent, $"BushMid_{i}", rng, x, z, Gy(x, z),
                bushScale * (0.65f + (float)rng.NextDouble() * 0.7f), Yaw());
            if ((i & 7) == 0) yield return null;
        }

        int nTrees = low ? 95 : 150;
        for (int i = 0; i < nTrees; i++)
        {
            NaturalPoint(clear + 40f, R - 80f, out float x, out float z);
            NatureLibrary.SpawnTree(parent, $"Tree_{i}", rng, x, z, Gy(x, z),
                treeScale * (0.7f + (float)rng.NextDouble() * 0.55f), Yaw());
            if ((i & 3) == 0) { NatureProg("ÃƒÂÃ…Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â´ÃƒÂÃ‚Â°: ÃƒÂÃ‚Â´ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂµÃƒÂÃ‚Â²ÃƒÂÃ‚Â°ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦", "Nature: treesÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦"); yield return null; }
        }

        int nPine = low ? 50 : 85;
        for (int i = 0; i < nPine; i++)
        {
            NaturalPoint(clear + 80f, R - 70f, out float x, out float z);
            NatureLibrary.SpawnPine(parent, $"Pine_{i}", rng, x, z, Gy(x, z),
                pineScale * (0.7f + (float)rng.NextDouble() * 0.55f), Yaw());
            if ((i & 2) == 0) { NatureProg("ÃƒÂÃ…Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â´ÃƒÂÃ‚Â°: Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¾Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦", "Nature: pinesÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦"); yield return null; }
        }

        int nRock = low ? 85 : 140;
        for (int i = 0; i < nRock; i++)
        {
            NaturalPoint(clear + 12f, R * 0.75f, out float x, out float z);
            NatureLibrary.SpawnRock(parent, $"Rock_{i}", rng, x, z, Gy(x, z),
                rockScale * (0.4f + (float)rng.NextDouble() * 1.1f), Yaw());
            if ((i & 3) == 0) yield return null;
        }

        int nStump = low ? 24 : 42;
        for (int i = 0; i < nStump; i++)
        {
            NaturalPoint(clear + 25f, clear + 420f, out float x, out float z);
            NatureLibrary.SpawnStump(parent, $"Stump_{i}", rng, x, z, Gy(x, z),
                stumpScale * (0.6f + (float)rng.NextDouble() * 0.6f), Yaw());
            if ((i & 3) == 0) yield return null;
        }

        int nMush = low ? 30 : 55;
        for (int i = 0; i < nMush; i++)
        {
            NaturalPoint(clear + 20f, clear + 360f, out float x, out float z);
            NatureLibrary.SpawnMushroom(parent, $"Mush_{i}", rng, x, z, Gy(x, z),
                2.8f * (0.6f + (float)rng.NextDouble() * 0.8f), Yaw());
            if ((i & 3) == 0) yield return null;
        }

        // ÃƒÂÃ¢â‚¬â€œÃƒÂÃ‚Â¸ÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â³ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¶Ãƒâ€˜Ã¢â‚¬â€œ
        int nHedge = low ? 8 : 12;
        for (int h = 0; h < nHedge; h++)
        {
            float baseAng = h * (Mathf.PI * 2f / nHedge) + 0.1f;
            float arc = 0.5f + (float)rng.NextDouble() * 0.35f;
            float dist = clear + 8f + (float)rng.NextDouble() * 36f;
            int segs = low ? 10 : 14;
            for (int s = 0; s < segs; s++)
            {
                float a = baseAng + (s / (float)(segs - 1) - 0.5f) * arc;
                float x = Mathf.Cos(a) * dist, z = Mathf.Sin(a) * dist;
                NatureLibrary.SpawnBush(parent, $"Hedge_{h}_{s}", rng, x, z, Gy(x, z),
                    bushScale * (0.5f + (float)rng.NextDouble() * 0.45f), a * Mathf.Rad2Deg);
            }
            yield return null;
        }

        // ÃƒÂÃ¢â‚¬â€ÃƒÂÃ‚Â°ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²ÃƒÂÃ‚Â½Ãƒâ€˜Ã…Â½ÃƒÂÃ‚Â²ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â‚¬Â¡Ãƒâ€˜Ã¢â‚¬â€œ mid-field (ÃƒÂÃ‚Â·ÃƒÂÃ‚Â¼Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã‹â€ ÃƒÂÃ‚Â°ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ‚ÂºÃƒÂÃ‚Â»ÃƒÂÃ‚Â°Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸)
        int nFill = low ? 60 : 100;
        for (int i = 0; i < nFill; i++)
        {
            NaturalPoint(clear + 100f, clear + 600f, out float x, out float z);
            float gy = Gy(x, z);
            int kind = i % 5;
            if (kind == 0)
                NatureLibrary.SpawnTree(parent, $"FillTree_{i}", rng, x, z, gy, treeScale * 0.85f, Yaw());
            else if (kind == 1)
                NatureLibrary.SpawnPine(parent, $"FillPine_{i}", rng, x, z, gy, pineScale * 0.85f, Yaw());
            else if (kind == 2)
                NatureLibrary.SpawnBush(parent, $"FillBush_{i}", rng, x, z, gy, bushScale * 1.1f, Yaw());
            else if (kind == 3)
                NatureLibrary.SpawnRock(parent, $"FillRock_{i}", rng, x, z, gy, rockScale * 0.9f, Yaw());
            else
            {
                NatureLibrary.SpawnGrass(parent, $"FillGrass_{i}a", rng, x, z, gy, grassScale, Yaw());
                float x2 = x + ((float)rng.NextDouble() - 0.5f) * 5f;
                float z2 = z + ((float)rng.NextDouble() - 0.5f) * 5f;
                NatureLibrary.SpawnGrass(parent, $"FillGrass_{i}b", rng, x2, z2, Gy(x2, z2), grassScale * 0.8f, Yaw());
            }
            if ((i & 3) == 0) yield return null;
        }

        // ÃƒÂÃ¢â‚¬ÂÃƒÂÃ‚Â°ÃƒÂÃ‚Â»ÃƒÂÃ‚ÂµÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â‚¬â€œ Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â‚¬Â°Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã†â€™Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ ÃƒÂÃ‚Â»Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã‚ÂÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ ÃƒÂÃ‚Â³ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â·ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Å¡
        int nGrove = low ? 14 : 20;
        for (int g = 0; g < nGrove; g++)
        {
            float gang = g * (Mathf.PI * 2f / nGrove) + 0.12f;
            float gdist = 240f + (float)rng.NextDouble() * 520f;
            float gx = Mathf.Cos(gang) * gdist;
            float gz = Mathf.Sin(gang) * gdist;
            int count = 10 + rng.Next(10);
            for (int j = 0; j < count; j++)
            {
                float ox = gx + ((float)rng.NextDouble() - 0.5f) * 80f;
                float oz = gz + ((float)rng.NextDouble() - 0.5f) * 80f;
                float gy = Gy(ox, oz);
                float s = treeScale * (0.8f + (float)rng.NextDouble() * 0.5f);
                if (j % 3 == 0)
                    NatureLibrary.SpawnPine(parent, $"GrovePine_{g}_{j}", rng, ox, oz, gy, s * 1.15f, Yaw());
                else
                    NatureLibrary.SpawnTree(parent, $"GroveTree_{g}_{j}", rng, ox, oz, gy, s, Yaw());
                if (j % 4 == 0)
                    NatureLibrary.SpawnBush(parent, $"GroveBush_{g}_{j}", rng,
                        ox + 3f, oz - 2f, Gy(ox + 3f, oz - 2f), bushScale * 0.9f, Yaw());
            }
            yield return null;
        }
    }

    /// <summary>ÃƒÂÃ…Â¡Ãƒâ€˜Ã†â€™ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â» ÃƒÂÃ‚Â· Ãƒâ€˜Ã¢â‚¬Â¡ÃƒÂÃ‚Â¸Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¼ ÃƒÂÃ‚Â²ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸ÃƒÂÃ‚ÂºÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¼ ÃƒÂÃ‚Â³Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â°ÃƒÂÃ‚Â´Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬ÂÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¼ (ÃƒÂÃ‚Â±ÃƒÂÃ‚ÂµÃƒÂÃ‚Â· ÃƒÂÃ‚Â³ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â·ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â³ÃƒÂÃ‚Â¾ Ãƒâ€˜Ã‹â€ ÃƒÂÃ‚Â²ÃƒÂÃ‚Â°).</summary>
    static void BuildSkyGradientDome(Transform parent)
    {
        var old = GameObject.Find("SkyDome");
        if (old != null) Object.Destroy(old);

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "SkyDome";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        // ÃƒÂÃ‚Â Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â²ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¼Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ ÃƒÂÃ‚ÂºÃƒâ€˜Ã†â€™ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â»; flip X Ãƒâ€˜Ã¢â‚¬Â°ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â± normals ÃƒÂÃ‚Â²Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂµÃƒÂÃ‚Â´ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â½Ãƒâ€˜Ã†â€™ (ÃƒÂÃ‚Â±ÃƒÂÃ‚ÂµÃƒÂÃ‚Â· ÃƒÂÃ‚Â°Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¼ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬â€ Y)
        go.transform.localScale = new Vector3(-14000f, 14000f, 14000f); // flip X only: inward normals, V=1 zenith on top
        Object.Destroy(go.GetComponent<Collider>());

        var tex = MakeSkyGradientTex(512);
        var mat = new Material(VisualMaterials.UnlitShader);
        mat.name = "SkyDomeGrad";
        mat.renderQueue = 850;
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.EnableKeyword("_BASEMAP");
        }
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        mat.mainTexture = tex;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        // ÃƒÂÃ‚ÂÃƒÂÃ‚ÂµÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â‚¬Â tiling/offset ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Ãƒâ€˜Ã¢â‚¬Â¡ÃƒÂÃ‚Â¸Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ V-ÃƒÂÃ‚Â³Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â°ÃƒÂÃ‚Â´Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬ÂÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Å¡
        if (mat.HasProperty("_BaseMap_ST")) mat.SetVector("_BaseMap_ST", new Vector4(1f, 1f, 0f, 0f));

        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    static Texture2D MakeSkyGradientTex(int h)
    {
        // ÃƒÂÃ‚Â¨ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° 4: Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â¡ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œ Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²ÃƒÂÃ‚Â¿Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ UV-Ãƒâ€˜Ã‹â€ ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â² Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¾ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ ÃƒÂÃ‚Â½ÃƒÂÃ‚ÂµÃƒÂÃ‚Â²ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â´ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹
        const int w = 4;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false, false);
        tex.name = "SkyGrad";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        // ÃƒÂÃ…Â¸ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â° Ãƒâ€˜Ã†â€™ÃƒÂÃ‚Â·ÃƒÂÃ‚Â³ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â´ÃƒÂÃ‚Â¶ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½ÃƒÂÃ‚Â° ÃƒÂÃ‚Â· EnvironmentTextures (steel + warm horizon near sun side conceptually)
        // Below-horizon (V~0) = fog-dark void so low camera tilts past the disk rim
        // do not flash a bright gray slab; horizon band stays near the equator.
        Color below = Color.Lerp(EnvironmentTextures.FogColor, EnvironmentTextures.SkyZenith, 0.62f);
        Color fogH = EnvironmentTextures.FogColor;
        Color horizon = EnvironmentTextures.SkyHorizon;
        Color low = Color.Lerp(EnvironmentTextures.SkyHorizon, new Color(0.32f, 0.44f, 0.56f), 0.55f);
        Color mid = new Color(0.22f, 0.36f, 0.5f);
        Color upper = new Color(0.12f, 0.24f, 0.4f);
        Color zenith = EnvironmentTextures.SkyZenith;

        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1);
            Color c;
            if (t < 0.12f)
                c = Color.Lerp(below, fogH, Smooth01(t / 0.12f));
            else if (t < 0.22f)
                c = Color.Lerp(fogH, horizon, Smooth01((t - 0.12f) / 0.10f));
            else if (t < 0.40f)
                c = Color.Lerp(horizon, low, Smooth01((t - 0.22f) / 0.18f));
            else if (t < 0.62f)
                c = Color.Lerp(low, mid, Smooth01((t - 0.40f) / 0.22f));
            else if (t < 0.82f)
                c = Color.Lerp(mid, upper, Smooth01((t - 0.62f) / 0.20f));
            else
                c = Color.Lerp(upper, zenith, Smooth01((t - 0.82f) / 0.18f));

            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, c);
        }
        tex.Apply(false, true);
        return tex;
    }

    static float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    /// <summary>ÃƒÂÃ¢â‚¬Å“ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œ cloud-Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¿Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â°ÃƒÂÃ‚Â¹Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸ (Poly Haven) ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° Quad; ÃƒÂÃ‚Â±Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™Ãƒâ€˜Ã‹â€ ÃƒÂÃ‚Âµ Ãƒâ€˜Ã‹â€ ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â², ÃƒÂÃ‚Â´ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»ÃƒÂÃ‚ÂµÃƒÂÃ‚ÂºÃƒÂÃ‚Â¾ ÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´ pad.</summary>
    static void BuildSoftClouds(Transform parent)
    {
        EnvironmentTextures.EnsureLoaded();
        var root = new GameObject("Clouds");
        root.transform.SetParent(parent, false);

        // ÃƒÂÃ¢â‚¬ËœÃƒÂÃ‚Â°ÃƒÂÃ‚Â³ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂµÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â¡ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â¦ Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂºÃƒâ€˜Ã†â€™ÃƒÂÃ‚Â¿Ãƒâ€˜Ã¢â‚¬Â¡ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½Ãƒâ€˜Ã…â€™ (ÃƒÂÃ‚Â´ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»ÃƒÂÃ‚ÂµÃƒÂÃ‚ÂºÃƒÂÃ‚Â¾/ÃƒÂÃ‚Â²ÃƒÂÃ‚Â¸Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¾ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¾ ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â pad ÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹)
        var specs = new[]
        {
            new Vector4( 3000f, 1450f, -2600f, 560f),
            new Vector4(-3300f, 1550f,  2100f, 520f),
            new Vector4( 2100f, 1650f,  3400f, 540f),
            new Vector4(-2600f, 1380f, -3500f, 480f),
            new Vector4( 3600f, 1580f,   800f, 500f),
            new Vector4(-1400f, 1720f,  3700f, 440f),
            new Vector4(  800f, 1800f, -3800f, 460f),
            new Vector4(-3800f, 1480f,  -400f, 490f),
            new Vector4( 4200f, 1700f, -1200f, 430f),
            new Vector4(-2000f, 1900f,  4200f, 450f),
            new Vector4( 1500f, 1350f, -4200f, 410f),
            new Vector4(-4200f, 1600f,  1500f, 470f),
            new Vector4( 4800f, 1850f,  2200f, 400f),
            new Vector4(-4800f, 1750f, -2200f, 420f),
            new Vector4( 2500f, 2000f,  4800f, 380f),
            new Vector4(-900f,  2100f, -4800f, 390f),
            new Vector4(  200f, 1550f,  5000f, 360f),
            new Vector4( 5000f, 1650f,  -800f, 410f),
        };

        for (int i = 0; i < specs.Length; i++)
        {
            var s = specs[i];
            bool shade = (i % 2) == 1;
            var matMain = EnvironmentTextures.MakeCloudMaterial(shade, i);
            var matSoft = EnvironmentTextures.MakeCloudMaterial(!shade, i + 2);
            var matFar = EnvironmentTextures.MakeCloudMaterial(shade, i + 4);

            // ÃƒÂÃ…Â¾Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬ÂÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã‚Â ÃƒÂÃ‚ÂºÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Âº Ãƒâ€šÃ‚Â«ÃƒÂÃ‚Â´ÃƒÂÃ‚Â¾ Ãƒâ€˜Ã¢â‚¬Â ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã†â€™ Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Â ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸Ãƒâ€šÃ‚Â» (ÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â‚¬Â°ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¸ÃƒÂÃ‚Â»Ãƒâ€˜Ã†â€™ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â‚¬Å¡)
            float yaw = Mathf.Atan2(s.x, s.z) * Mathf.Rad2Deg + 180f;

            MakeCloudQuad(root.transform, $"C{i}_a", new Vector3(s.x, s.y, s.z),
                s.w * 1.85f, s.w * 0.62f, matMain, yaw);
            MakeCloudQuad(root.transform, $"C{i}_b",
                new Vector3(s.x + s.w * 0.4f, s.y + 40f, s.z - s.w * 0.15f),
                s.w * 1.25f, s.w * 0.45f, matSoft, yaw + 18f);
            MakeCloudQuad(root.transform, $"C{i}_c",
                new Vector3(s.x - s.w * 0.35f, s.y + 22f, s.z + s.w * 0.12f),
                s.w * 1.0f, s.w * 0.36f, matMain, yaw - 22f);
            MakeCloudQuad(root.transform, $"C{i}_d",
                new Vector3(s.x + s.w * 0.12f, s.y + 58f, s.z + s.w * 0.22f),
                s.w * 0.8f, s.w * 0.3f, matFar, yaw + 35f);
            MakeCloudQuad(root.transform, $"C{i}_e",
                new Vector3(s.x - s.w * 0.2f, s.y + 8f, s.z - s.w * 0.25f),
                s.w * 0.65f, s.w * 0.26f, matSoft, yaw - 40f);
        }

        // ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ ÃƒÂÃ‚Â¥ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ ÃƒÂÃ¢â‚¬â€ÃƒÂÃ¢â‚¬â„¢ÃƒÂÃ¢â‚¬Â¢ÃƒÂÃ‚Â ÃƒÂÃ‚Â¥ÃƒÂÃ‚Â£ (zenith / ÃƒÂÃ‚Â½ÃƒÂÃ‚Â°ÃƒÂÃ‚Â´ pad) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ÃƒÂÃ‚Â³ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â·ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ‚ÂºÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¸ ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬
        var overhead = new[]
        {
            new Vector4(  400f, 2800f,  -300f, 700f),
            new Vector4( -500f, 3000f,   450f, 650f),
            new Vector4(  200f, 3200f,   600f, 580f),
            new Vector4( -350f, 2700f,  -550f, 620f),
            new Vector4(  700f, 3100f,   150f, 540f),
            new Vector4( -700f, 2900f,  -100f, 560f),
            new Vector4(  100f, 3400f,  -700f, 500f),
            new Vector4( -150f, 2600f,   800f, 520f),
        };
        for (int i = 0; i < overhead.Length; i++)
        {
            var s = overhead[i];
            var mat = EnvironmentTextures.MakeCloudMaterial(i % 2 == 0, i + 1);
            var mat2 = EnvironmentTextures.MakeCloudMaterial(i % 2 == 1, i + 3);
            // ÃƒÂÃ¢â‚¬Å“ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â·ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¾ (ÃƒÂÃ‚Â´ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â²ÃƒÂÃ‚Â»Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã…â€™Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã‚Â ÃƒÂÃ‚Â²ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â·) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ÃƒÂÃ‚Â²ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â´ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¾ ÃƒÂÃ‚Â¿Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â³ÃƒÂÃ‚Â»Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â´Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ‚Â²ÃƒÂÃ‚Â³ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã†â€™
            MakeCloudQuadFlat(root.transform, $"Top_{i}_a",
                new Vector3(s.x, s.y, s.z), s.w * 1.4f, s.w * 1.1f, mat, i * 20f);
            MakeCloudQuadFlat(root.transform, $"Top_{i}_b",
                new Vector3(s.x + s.w * 0.25f, s.y + 80f, s.z - s.w * 0.2f),
                s.w * 0.9f, s.w * 0.75f, mat2, i * 20f + 35f);
        }
    }

    static void MakeCloudQuad(Transform parent, string name, Vector3 pos, float width, float height, Material mat, float yawDeg)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Quad);
        c.name = name;
        c.transform.SetParent(parent, false);
        c.transform.position = pos;
        c.transform.rotation = Quaternion.Euler(12f, yawDeg, 0f);
        c.transform.localScale = new Vector3(width, height, 1f);
        Object.Destroy(c.GetComponent<Collider>());
        var mr = c.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    /// <summary>ÃƒÂÃ‚Â¥ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â° ÃƒÂÃ‚Â·ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã†â€™: quad ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°ÃƒÂÃ‚Â¹ÃƒÂÃ‚Â¶ÃƒÂÃ‚Âµ ÃƒÂÃ‚Â³ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â·ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¾ (ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ ÃƒÂÃ‚Â²ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â·).</summary>
    static void MakeCloudQuadFlat(Transform parent, string name, Vector3 pos, float width, float depth, Material mat, float yawDeg)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Quad);
        c.name = name;
        c.transform.SetParent(parent, false);
        c.transform.position = pos;
        // 90Ãƒâ€šÃ‚Â° pitch = face down; yaw ÃƒÂÃ‚Â´ÃƒÂÃ‚Â»Ãƒâ€˜Ã‚Â Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â·ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¾Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â‚¬â€œ
        c.transform.rotation = Quaternion.Euler(90f, yawDeg, 0f);
        c.transform.localScale = new Vector3(width, depth, 1f);
        Object.Destroy(c.GetComponent<Collider>());
        var mr = c.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }


    /// <summary>
    /// ÃƒÂÃ‚Â¤Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â·ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â¡ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° ÃƒÂÃ‚Â·ÃƒÂÃ‚ÂµÃƒÂÃ‚Â¼ÃƒÂÃ‚Â»Ãƒâ€˜Ã‚Â / ÃƒÂÃ‚Â²ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬Â¦ ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã†â€™ÃƒÂÃ‚Â±ÃƒÂÃ‚Â¸. ÃƒÂÃ¢â‚¬ÂºÃƒÂÃ‹Å“ÃƒÂÃ‚Â¨ÃƒÂÃ¢â‚¬Â¢ ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â´ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â° pad ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ÃƒÂÃ‚Â±ÃƒÂÃ‚ÂµÃƒÂÃ‚Â· ÃƒÂÃ‚Â½ÃƒÂÃ‚Â°ÃƒÂÃ‚ÂºÃƒÂÃ‚Â»ÃƒÂÃ‚Â°ÃƒÂÃ‚Â´ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â¦ ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â»ÃƒÂÃ‚Â°ÃƒÂÃ‚Â½ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â¦ ÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸Ãƒâ€˜Ã‹â€ ÃƒÂÃ‚Â¾ÃƒÂÃ‚Âº
    /// (ÃƒÂÃ‚Â²ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ ÃƒÂÃ‚Â´ÃƒÂÃ‚Â°ÃƒÂÃ‚Â²ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸ ÃƒÂÃ‚Â¼ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â‚¬Â¦Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â½ÃƒÂÃ‚Â½Ãƒâ€˜Ã‚Â ÃƒÂÃ‚ÂºÃƒÂÃ‚Â°ÃƒÂÃ‚Â¼ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸). ÃƒÂÃ‚Â ÃƒÂÃ‚ÂµÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™Ãƒâ€˜Ã¢â‚¬ÂÃƒâ€˜Ã¢â‚¬Å¾ ÃƒÂÃ‚Â¿Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´ pad ÃƒÂÃ‚Â·ÃƒÂÃ‚Â°ÃƒÂÃ‚Â½Ãƒâ€˜Ã†â€™Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½ÃƒÂÃ‚Â¾ ÃƒÂÃ‚Â² LunarTerrainMesh.
    /// </summary>
    public const float PadSurfaceY = 0.05f;

    static void BuildLandingPad(Transform parent)
    {
        var old = GameObject.Find("LandingPad");
        if (old != null) Object.Destroy(old);

        var pad = new GameObject("LandingPad");
        pad.transform.SetParent(parent, false);

        // ÃƒÂÃ…Â¾ÃƒÂÃ‚Â´ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â° ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã†â€™ÃƒÂÃ‚Â±ÃƒÂÃ‚Â¸ + ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â·Ãƒâ€˜Ã…â€™ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¼Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â€šÂ¬ + 4 Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½ÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂºÃƒÂÃ‚Â¸. ÃƒÂÃ¢â‚¬ËœÃƒÂÃ‚ÂµÃƒÂÃ‚Â· ÃƒÂÃ‚Â½ÃƒÂÃ‚Â°ÃƒÂÃ‚ÂºÃƒÂÃ‚Â»ÃƒÂÃ‚Â°ÃƒÂÃ‚Â´ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â¦ ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â»ÃƒÂÃ‚Â°ÃƒÂÃ‚Â½ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â¦ ÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸Ãƒâ€˜Ã‹â€ ÃƒÂÃ‚Â¾ÃƒÂÃ‚Âº.
        var deckMat = MakePadDeckMaterial("PadDeckSkin");
        var sideMat = VisualMaterials.Lit(new Color(0.26f, 0.27f, 0.29f), 0.02f, 0.05f);
        var collarMat = MakePadCollarMaterial("PadCollarSkin");
        GetBeaconMaterials(out var baseMat, out var poleMat, out var lampMat);

        const float top = PadSurfaceY;
        const float bottom = -1.35f;
        float half = (top - bottom) * 0.5f;
        float centerY = (top + bottom) * 0.5f;

        // 128 Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂµÃƒÂÃ‚Â³ÃƒÂÃ‚Â¼ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â² ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¸ÃƒÂÃ‚Â»Ãƒâ€˜Ã†â€™ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â‚¬Å¡ pad ÃƒÂ¢Ã¢â‚¬Â°Ã‹â€  Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´ÃƒÂÃ‚ÂµÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½ÃƒÂÃ‚Âµ ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¾ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¾ (ÃƒÂÃ‚Â½ÃƒÂÃ‚Âµ 20-gon Unity primitive)
        const int padSeg = 128;
        var deck = SmoothMesh.MakeCylinder("Deck", pad.transform,
            new Vector3(0f, centerY, 0f), 90f, half, deckMat, padSeg);
        SetShadow(deck, true, true);

        // ÃƒÂÃ…Â¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¼Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â€šÂ¬ ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã…Â½ ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¶Ãƒâ€˜Ã¢â‚¬Â¡ÃƒÂÃ‚Âµ ÃƒÂÃ‚Â²ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬Â¦Ãƒâ€˜Ã†â€™ ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã†â€™ÃƒÂÃ‚Â±ÃƒÂÃ‚Â¸; Ãƒâ€˜Ã‹â€ ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã‹â€ ÃƒÂÃ‚Â° ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â»Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¼ÃƒÂÃ‚Â° ÃƒÂÃ‚Â·ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â²ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â‚¬ÂÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã…â€™Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã‚Â ÃƒÂÃ‚Â·Ãƒâ€˜Ã¢â‚¬â€œ Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â½ÃƒÂÃ‚Â¾Ãƒâ€˜Ã…Â½ Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¼ÃƒÂÃ‚Â¸
        const float collarTop = top - 0.42f;
        const float collarBot = -1.1f;
        float cHalf = (collarTop - collarBot) * 0.5f;
        float cY = (collarTop + collarBot) * 0.5f;
        var collar = SmoothMesh.MakeCylinder("Collar", pad.transform,
            new Vector3(0f, cY, 0f), 112f, cHalf, collarMat, padSeg);
        SetShadow(collar, true, true);

        // ÃƒÂÃ‚Â¢ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½ÃƒÂÃ‚ÂºÃƒÂÃ‚Â° Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»ÃƒÂÃ‚ÂµÃƒÂÃ‚Â²ÃƒÂÃ‚Â° ÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¼ÃƒÂÃ‚ÂºÃƒÂÃ‚Â° ÃƒÂÃ‚Â¿Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´ ÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â‚¬ÂÃƒÂÃ‚Â¼ ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã†â€™ÃƒÂÃ‚Â±ÃƒÂÃ‚Â¸ (ÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´Ãƒâ€˜Ã¢â‚¬Â¡Ãƒâ€˜Ã†â€™Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã‚Â ÃƒÂÃ‚Â±ÃƒÂÃ‚Â¾ÃƒÂÃ‚ÂºÃƒâ€˜Ã†â€™ ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¶Ãƒâ€˜Ã¢â‚¬Â¡ÃƒÂÃ‚Âµ ÃƒÂÃ‚Â·ÃƒÂÃ‚Â° top)
        var lip = SmoothMesh.MakeCylinder("DeckLip", pad.transform,
            new Vector3(0f, top - 0.12f, 0f), 91.2f, 0.08f, sideMat, padSeg);
        SetShadow(lip, true, true);

        // ÃƒÂÃ‚Â§ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ ÃƒÂÃ‚ÂºÃƒâ€˜Ã†â€™Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂºÃƒÂÃ‚Â¸ ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¹ Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â°ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ ÃƒÂÃ‚Â½ÃƒÂÃ‚Â°ÃƒÂÃ‚Â±Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â€šÂ¬, Ãƒâ€˜Ã¢â‚¬Â°ÃƒÂÃ‚Â¾ ÃƒÂÃ‚Â¿Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´Ãƒâ€˜Ã¢â‚¬Â¦Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂºÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Sin(a) * 36f, 0f, Mathf.Cos(a) * 36f);
            PlaceBeacon(pad.transform, $"PadBeacon_{i}", p, top, baseMat, poleMat, lampMat);
        }
    }

    /// <summary>ÃƒÂÃ‚Â¡ÃƒÂÃ‚Â¿Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ ÃƒÂÃ‚Â½ÃƒÂÃ‚Â°ÃƒÂÃ‚Â±Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â€šÂ¬ ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂºÃƒÂÃ‚Â°: ÃƒÂÃ‚Â±ÃƒÂÃ‚Â°ÃƒÂÃ‚Â·ÃƒÂÃ‚Â° + Ãƒâ€˜Ã¢â‚¬Â°ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â³ÃƒÂÃ‚Â»ÃƒÂÃ‚Â° + ÃƒÂÃ‚Â¼ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€žÂ¢Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂºÃƒÂÃ‚Â° ÃƒÂÃ‚Â±Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»ÃƒÂÃ‚Â° ÃƒÂÃ‚Â»ÃƒÂÃ‚Â°ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â° (pad Ãƒâ€˜Ã¢â‚¬â€œ approach Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â¡ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œ).</summary>
    static void GetBeaconMaterials(out Material baseMat, out Material poleMat, out Material lampMat)
    {
        baseMat = VisualMaterials.Lit(new Color(0.3f, 0.31f, 0.33f), 0.06f, 0.1f);
        poleMat = VisualMaterials.Lit(new Color(0.42f, 0.43f, 0.46f), 0.1f, 0.18f);
        lampMat = VisualMaterials.Unlit(new Color(0.9f, 0.92f, 0.95f), new Color(0.7f, 0.75f, 0.85f));
    }

    /// <summary>
    /// ÃƒÂÃ…â€œÃƒÂÃ‚Â°Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Âº ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° ÃƒÂÃ‚Â·ÃƒÂÃ‚ÂµÃƒÂÃ‚Â¼ÃƒÂÃ‚Â»Ãƒâ€˜Ã¢â‚¬â€œ: ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â· ÃƒÂÃ‚Â±ÃƒÂÃ‚Â°ÃƒÂÃ‚Â·ÃƒÂÃ‚Â¸ = groundY (ÃƒÂÃ‚Â±ÃƒÂÃ‚ÂµÃƒÂÃ‚Â· Ãƒâ€šÃ‚Â«ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â½ÃƒÂÃ‚Â½Ãƒâ€˜Ã‚ÂÃƒâ€šÃ‚Â»).
    /// MakeCylinder: center Y, halfHeight ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â· = center ÃƒÂ¢Ã‹â€ Ã¢â‚¬â„¢ halfH.
    /// </summary>
    static void PlaceBeacon(Transform parent, string id, Vector3 xz, float groundY,
        Material baseMat, Material poleMat, Material lampMat)
    {
        const float baseHalf = 0.12f;
        const float poleHalf = 1.55f;
        // ÃƒÂÃ¢â‚¬ËœÃƒÂÃ‚Â°ÃƒÂÃ‚Â·ÃƒÂÃ‚Â° Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¸ÃƒÂÃ‚Â´ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã…â€™ ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° groundY (Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â‚¬Â¦ÃƒÂÃ‚Â¸ ÃƒÂÃ‚Â·ÃƒÂÃ‚Â°ÃƒÂÃ‚Â³ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â±ÃƒÂÃ‚Â»ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½ÃƒÂÃ‚Â°)
        float baseY = groundY + baseHalf - 0.06f;
        float poleY = baseY + baseHalf + poleHalf - 0.02f;
        float lampY = poleY + poleHalf + 0.2f;

        var bBase = SmoothMesh.MakeCylinder($"{id}_Base", parent,
            new Vector3(xz.x, baseY, xz.z), 0.55f, baseHalf, baseMat);
        var bPole = SmoothMesh.MakeCylinder($"{id}_Pole", parent,
            new Vector3(xz.x, poleY, xz.z), 0.16f, poleHalf, poleMat);
        SetShadow(bBase, true, true);
        SetShadow(bPole, true, true);
        var lamp = SmoothMesh.MakeSphere($"{id}_Lamp", parent,
            new Vector3(xz.x, lampY, xz.z), new Vector3(0.4f, 0.3f, 0.4f), lampMat);
        SetShadow(lamp, false, false);
    }

    static void SetShadow(GameObject go, bool cast, bool receive)
    {
        var mr = go != null ? go.GetComponent<MeshRenderer>() : null;
        if (mr == null) return;
        mr.shadowCastingMode = cast
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = receive;
    }

    /// <summary>
    /// ÃƒÂÃ¢â‚¬ËœÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° LZ ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã†â€™ÃƒÂÃ‚Â±ÃƒÂÃ‚Â° (Earth airfield): ÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€˜Ã‚Â, Ãƒâ€˜Ã¢â‚¬Â¦Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂµÃƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡, ÃƒÂÃ‚Â´Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â°ÃƒÂÃ‚Â³ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã¢â‚¬â€œ.
    /// ÃƒÂÃ…Â¸ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã†â€™ÃƒÂÃ‚Â±ÃƒÂÃ‚Â° ÃƒÆ’Ã‹Å“ ÃƒÂ¢Ã¢â‚¬Â°Ã‹â€  92 ÃƒÂÃ‚Â¼ ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ r=1 ÃƒÂ¢Ã¢â‚¬Â°Ã‹â€  46 ÃƒÂÃ‚Â¼.
    /// </summary>
    static Material MakePadDeckMaterial(string name)
    {
        const int n = 768;
        // linear:false = sRGB albedo (ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂµÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¾ ÃƒÂÃ‚Â´ÃƒÂÃ‚Â»Ãƒâ€˜Ã‚Â color textures ÃƒÂÃ‚Â² URP)
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, true, false);
        tex.name = name;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.anisoLevel = 8;
        var cols = new Color[n * n];

        // ÃƒÂÃ‚Â¢Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â‚¬Â¦ÃƒÂÃ‚Â¸ Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã‹â€ Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ‚Â»Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬â€, Ãƒâ€˜Ã¢â‚¬Â°ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â± ÃƒÂÃ‚Â²ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¶ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â²ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸ mipmaps + ÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â°ÃƒÂÃ‚Â½Ãƒâ€˜Ã…â€™
        const float wRing = 0.010f;
        const float wCross = 0.0075f;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x + 0.5f) / n * 2f - 1f;
            float v = (y + 0.5f) / n * 2f - 1f;
            float r = Mathf.Sqrt(u * u + v * v);
            float ang = Mathf.Atan2(v, u);
            int idx = y * n + x;

            if (r > 1.002f)
            {
                cols[idx] = new Color(0.22f, 0.225f, 0.235f, 1f);
                continue;
            }

            // ÃƒÂÃ‚Â§ÃƒÂÃ‚Â¸Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ ÃƒÂÃ‚Â±ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½ LZ
            float g0 = 0.52f
                     + PadHash(u * 9f, v * 9f) * 0.02f
                     + PadHash(u * 24f, v * 24f) * 0.01f;

            float bay = Mathf.Abs(r * 3f - Mathf.Round(r * 3f));
            g0 -= (1f - Mathf.SmoothStep(0f, 0.06f, bay)) * 0.03f;
            float a01 = ang / (Mathf.PI * 2f) + 0.5f;
            float spoke = Mathf.Abs(a01 * 8f - Mathf.Round(a01 * 8f));
            g0 -= (1f - Mathf.SmoothStep(0f, 0.03f, spoke)) * 0.02f;

            g0 -= Mathf.SmoothStep(0.88f, 1f, r) * 0.05f;
            g0 = Mathf.Clamp(g0, 0.32f, 0.62f);

            float rr = g0 * 1.06f, gg = g0 * 1.03f, bb = g0 * 0.96f;

            // ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ ÃƒÂÃ‚ÂÃƒÂÃ‚Â°Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â¡ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ line art LZ ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬ÃƒÂ¢Ã¢â‚¬ÂÃ¢â€šÂ¬
            float line = 0f;
            float[] rings = { 0.97f, 0.88f, 0.78f, 0.66f, 0.54f, 0.42f, 0.30f, 0.20f, 0.12f, 0.06f };
            for (int ri = 0; ri < rings.Length; ri++)
            {
                float w = (ri == 0 || ri == 4) ? wRing * 1.45f : wRing;
                line = Mathf.Max(line, RingLine(r, rings[ri], w));
            }

            // ÃƒÂÃ…Â¾Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â½ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ + ÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â½ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ Ãƒâ€˜Ã¢â‚¬Â¦Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂµÃƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡
            if (r < 0.96f)
            {
                line = Mathf.Max(line, AxisLine(u, wCross * 1.15f));
                line = Mathf.Max(line, AxisLine(v, wCross * 1.15f));
            }
            // ÃƒÂÃ…Â¸ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ‚Â´Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â°ÃƒÂÃ‚Â³ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã¢â‚¬â€œ (ÃƒÂÃ‚Â½ÃƒÂÃ‚Âµ ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸Ãƒâ€˜Ã‹â€ ÃƒÂÃ‚Âµ ÃƒÂÃ‚Â·ÃƒÂÃ‚Â°ÃƒÂÃ‚Â³ÃƒÂÃ‚Â»Ãƒâ€˜Ã†â€™Ãƒâ€˜Ã‹â€ ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¸)
            if (r < 0.9f)
            {
                float d1 = Mathf.Abs(u - v) * 0.7071f;
                float d2 = Mathf.Abs(u + v) * 0.7071f;
                line = Mathf.Max(line, 1f - Mathf.SmoothStep(wCross * 0.3f, wCross * 0.95f, d1));
                line = Mathf.Max(line, 1f - Mathf.SmoothStep(wCross * 0.3f, wCross * 0.95f, d2));
            }

            // 16 ÃƒÂÃ‚Â°ÃƒÂÃ‚Â·ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¼Ãƒâ€˜Ã†â€™Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â¦ Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¾ÃƒÂÃ‚Âº ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° ÃƒÂÃ‚Â·ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã‹â€ ÃƒÂÃ‚Â½Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¼Ãƒâ€˜Ã†â€™ + Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂµÃƒÂÃ‚Â´ÃƒÂÃ‚Â½Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¼Ãƒâ€˜Ã†â€™ ÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Â¦
            float angDeg = ang * Mathf.Rad2Deg;
            for (int t = 0; t < 16; t++)
            {
                float target = -180f + t * 22.5f;
                float da = Mathf.Abs(Mathf.DeltaAngle(angDeg, target));
                bool major = (t % 2) == 0;
                bool cardinal = (t % 4) == 0;
                float angW = cardinal ? 1.7f : (major ? 1.2f : 0.85f);
                // ÃƒÂÃ¢â‚¬â€ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã‹â€ ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œ Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂºÃƒÂÃ‚Â¸
                float r0 = cardinal ? 0.84f : (major ? 0.88f : 0.91f);
                if (da < angW && r > r0 && r < 0.985f)
                {
                    float aFade = 1f - da / angW;
                    float rFade = Mathf.SmoothStep(r0, r0 + 0.02f, r)
                                * (1f - Mathf.SmoothStep(0.96f, 0.985f, r));
                    line = Mathf.Max(line, aFade * rFade);
                }
                // ÃƒÂÃ‚Â ÃƒÂÃ‚Â¸Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂºÃƒÂÃ‚Â¸ Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂµÃƒÂÃ‚Â´ÃƒÂÃ‚Â½Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â³ÃƒÂÃ‚Â¾ ÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€˜Ã‚Â (TDZ)
                if (cardinal && da < 1.4f && r > 0.48f && r < 0.58f)
                    line = Mathf.Max(line, (1f - da / 1.4f) * 0.9f);
            }

            // ÃƒÂÃ¢â‚¬â„¢ÃƒÂÃ‚Â½Ãƒâ€˜Ã†â€™Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã‹â€ ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œ Ãƒâ€˜Ã‹â€ ÃƒÂÃ‚ÂµÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ ÃƒÂÃ‚Â¿Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´ 45Ãƒâ€šÃ‚Â° ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° TDZ (ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ‚Â´Ãƒâ€˜Ã†â€™ÃƒÂÃ‚Â³ÃƒÂÃ‚Â¸)
            for (int c = 0; c < 4; c++)
            {
                float cAng = -135f + c * 90f;
                float da = Mathf.Abs(Mathf.DeltaAngle(angDeg, cAng));
                if (da < 12f && r > 0.50f && r < 0.58f)
                {
                    float arc = 1f - da / 12f;
                    float band = RingLine(r, 0.54f, wRing * 1.8f);
                    line = Mathf.Max(line, arc * band);
                }
            }

            // ÃƒÂÃ…â€œÃƒÂ¢Ã¢â€šÂ¬Ã¢â€žÂ¢Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂºÃƒÂÃ‚Â° Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â°ÃƒÂÃ‚Â´Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° Ãƒâ€˜Ã‹â€ Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â¦ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²ÃƒÂÃ‚ÂºÃƒÂÃ‚Â° ÃƒÂÃ‚Â±Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»Ãƒâ€˜Ã‚Â Ãƒâ€˜Ã¢â‚¬Â ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã†â€™ (Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â° Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â°ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â° Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â´ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â², ÃƒÂÃ‚Â±ÃƒÂÃ‚ÂµÃƒÂÃ‚Â· ÃƒÂÃ‚Â·ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â¦ ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â»Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¼)
            if (r > 0.08f && r < 0.22f)
            {
                float spokeH = Mathf.Abs(a01 * 16f - Mathf.Round(a01 * 16f));
                if (spokeH < 0.04f)
                    line = Mathf.Max(line, 1f - spokeH / 0.04f);
            }

            if (line > 0.02f)
            {
                // ÃƒÂÃ‚Â¡ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂµÃƒÂÃ‚Â´ÃƒÂÃ‚Â½Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â¾-Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â»ÃƒÂÃ‚Â° Ãƒâ€˜Ã¢â‚¬Å¾ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â±ÃƒÂÃ‚Â° (Ãƒâ€˜Ã¢â‚¬Â¡ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â°ÃƒÂÃ‚Â±ÃƒÂÃ‚ÂµÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚ÂµÃƒÂÃ‚Â¼ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã‹â€ Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â¹ ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã†â€™ÃƒÂÃ‚Â±Ãƒâ€˜Ã¢â‚¬â€œ)
                rr = Mathf.Lerp(rr, 0.72f, line);
                gg = Mathf.Lerp(gg, 0.74f, line);
                bb = Mathf.Lerp(bb, 0.78f, line);
            }

            cols[idx] = new Color(Mathf.Clamp01(rr), Mathf.Clamp01(gg), Mathf.Clamp01(bb), 1f);
        }

        tex.SetPixels(cols);
        tex.Apply(true, true);

        var mat = new Material(VisualMaterials.LitShader);
        mat.name = name;
        var tint = new Color(0.95f, 0.94f, 0.90f, 1f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.04f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.04f);
        if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", 0f);
        if (mat.HasProperty("_EnvironmentReflections")) mat.SetFloat("_EnvironmentReflections", 0f);
        mat.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
        mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", Vector2.one);
            mat.SetTextureOffset("_BaseMap", Vector2.zero);
            mat.EnableKeyword("_BASEMAP");
            mat.EnableKeyword("_BASE_MAP");
        }
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetTexture("_MainTex", tex);
            mat.SetTextureScale("_MainTex", Vector2.one);
        }
        mat.mainTexture = tex;
        return mat;
    }

    static float RingLine(float r, float center, float halfW)
    {
        float d = Mathf.Abs(r - center);
        return 1f - Mathf.SmoothStep(halfW * 0.35f, halfW, d);
    }

    static float AxisLine(float coord, float halfW)
    {
        float d = Mathf.Abs(coord);
        return 1f - Mathf.SmoothStep(halfW * 0.35f, halfW, d);
    }

    static float PadHash(float x, float y)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        float fx = x - x0;
        float fy = y - y0;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        unchecked
        {
            float H(int ix, int iy)
            {
                int h = ix * 374761393 + iy * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)0x7fffffff;
            }
            float v00 = H(x0, y0), v10 = H(x0 + 1, y0);
            float v01 = H(x0, y0 + 1), v11 = H(x0 + 1, y0 + 1);
            return Mathf.Lerp(Mathf.Lerp(v00, v10, fx), Mathf.Lerp(v01, v11, fx), fy) * 2f - 1f;
        }
    }

    /// <summary>
    /// ÃƒÂÃ…Â¸Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´Ãƒâ€˜Ã¢â‚¬Â¦Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂºÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ Earth LZ (ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂºÃƒÂÃ‚Â¸ ÃƒÂÃ‚Â²ÃƒÂÃ‚Â·ÃƒÂÃ‚Â´ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²ÃƒÂÃ‚Â¶ corridor).
    /// </summary>
    static void BuildApproachLights(Transform parent)
    {
        var old = GameObject.Find("ApproachCorridor");
        if (old != null) Object.Destroy(old);
        var old2 = GameObject.Find("ApproachMarkers");
        if (old2 != null) Object.Destroy(old2);

        var root = new GameObject("ApproachMarkers");
        root.transform.SetParent(parent, false);

        // ÃƒÂÃ‚Â¢Ãƒâ€˜Ã¢â‚¬â€œ Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â°ÃƒÂÃ‚Â¼Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â°ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸ + ÃƒÂÃ‚Â³ÃƒÂÃ‚ÂµÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¼ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã‚Â, Ãƒâ€˜Ã¢â‚¬Â°ÃƒÂÃ‚Â¾ ÃƒÂÃ‚ÂºÃƒâ€˜Ã†â€™Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂºÃƒÂÃ‚Â¸ pad
        GetBeaconMaterials(out var baseMat, out var poleMat, out var lampMat);

        Physics.SyncTransforms();
        const float xOff = 22f;
        for (int i = 0; i < 9; i++)
        {
            float z = LunarTerrainMesh.PadClearRadius + 8f + i * 38f;
            foreach (float x in new[] { -xOff, xOff })
            {
                // ÃƒÂÃ‚Â¢ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â‚¬Â¡ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° ÃƒÂÃ‚Â²ÃƒÂÃ‚Â¸Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â° mesh ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ÃƒÂÃ‚Â±ÃƒÂÃ‚ÂµÃƒÂÃ‚Â· +lift (Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â°ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã‹â€ ÃƒÂÃ‚Âµ Ãƒâ€šÃ‚Â«ÃƒÂÃ‚Â»Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸Ãƒâ€šÃ‚Â»)
                float ground = LunarTerrainMesh.SampleSurfaceY(x, z);
                PlaceBeacon(root.transform, $"AppBeacon_{i}_{x}",
                    new Vector3(x, 0f, z), ground, baseMat, poleMat, lampMat);
            }
        }
    }

    static Material MakePadCollarMaterial(string name)
    {
        var mat = VisualMaterials.Lit(new Color(0.28f, 0.29f, 0.30f), 0.02f, 0.06f);
        mat.name = name;
        return mat;
    }

    static void BuildSunDisc(Transform parent)
    {
        EnvironmentTextures.EnsureLoaded();
        var sky = new GameObject("SkyBodies");
        sky.transform.SetParent(parent, false);

        // ÃƒÂÃ¢â‚¬ÂÃƒÂÃ‚Â¸Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Âº Ãƒâ€˜Ã†â€™ Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â¹ Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â°ÃƒÂÃ‚Â¼Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â¹ ÃƒÂÃ‚Â¿ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â·ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬â€, ÃƒÂÃ‚Â·ÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¸ Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã…â€™ directional light
        var sunMat = EnvironmentTextures.MakeBeautifulSunMaterial();
        var sun = SmoothMesh.MakeSphere("Sun", sky.transform, SunWorldPosition, Vector3.one * 280f, sunMat);
        var r = sun.GetComponent<MeshRenderer>();
        if (r != null)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    static void SetupLighting(out Light sun)
    {
        EnvironmentTextures.EnsureLoaded();

        // ÃƒÂÃ¢â‚¬ÂºÃƒÂÃ‚Â¸Ãƒâ€˜Ã‹â€ ÃƒÂÃ‚Âµ ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â´ÃƒÂÃ‚Â½ÃƒÂÃ‚Âµ directional-Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¾ = ÃƒÂÃ‚Â¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Â ÃƒÂÃ‚Âµ (ÃƒÂÃ‚Â¿Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â±Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸ Fill/Rim/SkyBounce Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â‚¬Â°ÃƒÂÃ‚Â¾)
        DestroyExtraDirectionalLights();

        sun = FindMainSunLight();
        if (sun == null)
        {
            var go = new GameObject("Sun");
            sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.name = "Sun";
        sun.enabled = true;
        sun.color = new Color(1f, 0.95f, 0.88f);
        sun.intensity = 1.35f;
        sun.shadows = LightShadows.Hard;
        sun.shadowStrength = 0.75f;
        sun.shadowBias = 0.02f;
        sun.shadowNormalBias = 0.25f;
        sun.shadowNearPlane = 0.1f;
        sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;

        // ÃƒÂÃ…Â¸Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¼ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ¢â‚¬â„¢ÃƒÂÃ¢â‚¬Â ÃƒÂÃ¢â‚¬Â ÃƒÂÃ‚Â´ÃƒÂÃ‚Â¸Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂºÃƒÂÃ‚Â° ÃƒÂÃ‚Â¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€˜Ã‚Â
        Vector3 toScene = -SunWorldPosition.normalized;
        sun.transform.rotation = Quaternion.LookRotation(toScene, Vector3.up);

        // ÃƒÂÃ…â€œÃƒÂ¢Ã¢â€šÂ¬Ã¢â€žÂ¢Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂºÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ ambient ÃƒÂÃ‚Â·ÃƒÂÃ‚Â°ÃƒÂÃ‚Â¼Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã…â€™ ÃƒÂÃ‚Â´Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã†â€™ÃƒÂÃ‚Â³ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â³ÃƒÂÃ‚Â¾ Ãƒâ€šÃ‚Â«Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¾ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€˜Ã‚Â ÃƒÂÃ‚Â·ÃƒÂÃ‚Â²ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬Â¦Ãƒâ€˜Ã†â€™Ãƒâ€šÃ‚Â»
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.32f, 0.42f, 0.55f);
        RenderSettings.ambientEquatorColor = new Color(0.42f, 0.48f, 0.4f);
        RenderSettings.ambientGroundColor = new Color(0.18f, 0.24f, 0.14f);
        RenderSettings.ambientIntensity = 1.15f;
        RenderSettings.reflectionIntensity = 0.3f;

        FitShadowsToFocusDepth(420f);
    }

    static Light FindMainSunLight()
    {
        var named = GameObject.Find("Sun");
        if (named != null)
        {
            var l = named.GetComponent<Light>();
            if (l != null && l.type == LightType.Directional) return l;
        }
        foreach (var l in Object.FindObjectsByType<Light>())
        {
            if (l != null && l.type == LightType.Directional && l.enabled)
                return l;
        }
        return null;
    }

    static void DestroyExtraDirectionalLights()
    {
        string[] kill = { "FillLight", "RimLight", "SkyBounce", "Fill", "Rim" };
        foreach (var n in kill)
        {
            var go = GameObject.Find(n);
            if (go != null) Object.Destroy(go);
        }
        // ÃƒÂÃ¢â‚¬ËœÃƒâ€˜Ã†â€™ÃƒÂÃ‚Â´Ãƒâ€˜Ã…â€™-Ãƒâ€˜Ã‚ÂÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â‚¬â€œ Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â½Ãƒâ€˜Ã‹â€ Ãƒâ€˜Ã¢â‚¬â€œ directional, ÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â¼ "Sun"
        foreach (var l in Object.FindObjectsByType<Light>())
        {
            if (l == null || l.type != LightType.Directional) continue;
            if (l.gameObject.name == "Sun") continue;
            Object.Destroy(l.gameObject);
        }
    }

    /// <summary>
    /// ÃƒÂÃ‚Â¢Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸ directional shadow cascades Ãƒâ€˜Ã¢â‚¬Â°Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â¸ ÃƒÂÃ‚Â½ÃƒÂÃ‚Â°ÃƒÂÃ‚Â²ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¾ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¾ orbit focus, Ãƒâ€˜Ã¢â‚¬Â°ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â± ÃƒÂÃ‚Â¿Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ ÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´ÃƒÂÃ‚Â´ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œ
    /// ÃƒÂÃ‚Â½ÃƒÂÃ‚Âµ Ãƒâ€˜Ã‹â€ Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²Ãƒâ€˜Ã¢â‚¬Â¦ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â°ÃƒÂÃ‚ÂºÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã†â€™/pad Ãƒâ€˜Ã†â€™ low-res far cascade (Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â·ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â‚¬â€œ Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¸ÃƒÂÃ‚Â»Ãƒâ€˜Ã†â€™ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸).
    /// </summary>
    public static void FitShadowsToFocusDepth(float focusDepth)
    {
        bool low = (SystemInfo.graphicsMemorySize > 0 && SystemInfo.graphicsMemorySize < 3000)
                   || QualitySettings.GetQualityLevel() <= 1;

        focusDepth = Mathf.Max(8f, focusDepth);
        // ÃƒÂÃ…Â¾ÃƒÂÃ‚Â±ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€žÂ¢Ãƒâ€˜Ã¢â‚¬ÂÃƒÂÃ‚Â¼ Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œ Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â‚¬Â¦ÃƒÂÃ‚Â¸ ÃƒÂÃ‚Â·ÃƒÂÃ‚Â° ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â±ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€žÂ¢Ãƒâ€˜Ã¢â‚¬ÂÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¼ ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ÃƒÂÃ‚Â½ÃƒÂÃ‚Âµ Ãƒâ€˜Ã¢â‚¬Å¾Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚ÂºÃƒâ€˜Ã‚ÂÃƒÂÃ‚Â¾ÃƒÂÃ‚Â²ÃƒÂÃ‚Â°ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œ 1400 ÃƒÂÃ‚Â¼, Ãƒâ€˜Ã¢â‚¬Â°ÃƒÂÃ‚Â¾ Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â·Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â´ÃƒÂÃ‚Â¶Ãƒâ€˜Ã†â€™Ãƒâ€˜Ã…Â½Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã…â€™ texels ÃƒÂÃ‚Â·ÃƒÂÃ‚Â±ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â·Ãƒâ€˜Ã…â€™ÃƒÂÃ‚ÂºÃƒÂÃ‚Â°
        // ÃƒÂÃ‚Â¨ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã‹â€ ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ shadow distance ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ÃƒÂÃ‚Â´ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚ÂµÃƒÂÃ‚Â²ÃƒÂÃ‚Â°/ÃƒÂÃ‚ÂºÃƒâ€˜Ã†â€™Ãƒâ€˜Ã¢â‚¬Â°Ãƒâ€˜Ã¢â‚¬â€œ ÃƒÂÃ‚Â±Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»Ãƒâ€˜Ã‚Â pad Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚ÂµÃƒÂÃ‚Â¶ ÃƒÂÃ‚Â² cascade
        float shadowDist = Mathf.Clamp(focusDepth * 1.85f + 140f, low ? 280f : 380f, low ? 1200f : 2800f);

        // ÃƒÂÃ‚Â ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â·Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â°ÃƒÂÃ‚Â²ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸ ÃƒÂÃ‚Â¼ÃƒÂÃ‚ÂµÃƒÂÃ‚Â¶Ãƒâ€˜Ã¢â‚¬â€œ cascade Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â°ÃƒÂÃ‚Âº, Ãƒâ€˜Ã¢â‚¬Â°ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â± ÃƒÂÃ‚Â³ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â±ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° focus ÃƒÂÃ‚Â±Ãƒâ€˜Ã†â€™ÃƒÂÃ‚Â»ÃƒÂÃ‚Â° ÃƒÂÃ‚Â±Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»Ãƒâ€˜Ã‚Â ÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€˜Ã‚Â cascade 2/3
        // (ÃƒÂÃ‚Â½ÃƒÂÃ‚Â°ÃƒÂÃ‚Â¹ÃƒÂÃ‚Â²ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Â°ÃƒÂÃ‚Â° ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¾Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â½ÃƒÂÃ‚Â° Ãƒâ€˜Ã¢â‚¬Â°Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã…â€™ ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â°ÃƒÂÃ‚ÂºÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã¢â‚¬â€œ + pad, ÃƒÂÃ‚Â½ÃƒÂÃ‚Âµ ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸Ãƒâ€˜Ã‹â€ ÃƒÂÃ‚Âµ ÃƒÂÃ‚Â±Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»Ãƒâ€˜Ã‚Â ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â±ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€žÂ¢Ãƒâ€˜Ã¢â‚¬ÂÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â²ÃƒÂÃ‚Â° ÃƒÂÃ‚ÂºÃƒÂÃ‚Â°ÃƒÂÃ‚Â¼ÃƒÂÃ‚ÂµÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸).
        float f = Mathf.Clamp(focusDepth / shadowDist, 0.2f, 0.88f);
        var split4 = new Vector3(
            Mathf.Clamp(f * 0.28f, 0.04f, 0.18f),
            Mathf.Clamp(f * 0.58f, 0.12f, 0.42f),
            Mathf.Clamp(f * 0.92f, 0.28f, 0.78f));

        QualitySettings.shadowDistance = shadowDist;
        QualitySettings.shadowCascades = 4;
        QualitySettings.shadowCascade4Split = split4;
        QualitySettings.shadowResolution = low ? ShadowResolution.High : ShadowResolution.VeryHigh;
        // ÃƒÂÃ¢â‚¬â„¢ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¼ÃƒÂÃ‚ÂºÃƒÂÃ‚Â½Ãƒâ€˜Ã†â€™Ãƒâ€˜Ã¢â‚¬Å¡ÃƒÂÃ‚Â¸ soft shadow filter (Hard) ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Soft Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â¸ÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œÃƒâ€˜Ã‹â€ ÃƒÂÃ‚Âµ ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã…â€™, ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¾ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸ texels cascade Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã†â€™Ãƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã…â€™
        QualitySettings.shadows = ShadowQuality.HardOnly;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        QualitySettings.shadowNearPlaneOffset = 2f;
        ApplyUrpShadowSettings(shadowDist, split4, low);
    }

    static void ApplyUrpShadowSettings(float distance, Vector3 cascade4Split, bool low)
    {
        var pipe = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        if (pipe == null) return;
        var t = pipe.GetType();
        TrySetProp(pipe, t, "shadowDistance", distance);
        TrySetProp(pipe, t, "shadowCascadeCount", 4);
        TrySetProp(pipe, t, "cascade4Split", cascade4Split);
        // ÃƒÂÃ…â€œÃƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â¼ÃƒÂÃ‚Â°ÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™ÃƒÂÃ‚Â½ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ cascade blend ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â ÃƒÂÃ‚Â²ÃƒÂÃ‚ÂµÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¹ border ÃƒÂÃ‚Â²ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â³ÃƒÂÃ‚Â»Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Â´ÃƒÂÃ‚Â°Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€˜Ã‚ÂÃƒÂÃ‚Âº soft blur ÃƒÂÃ‚Â¿Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¸ zoom
        TrySetProp(pipe, t, "cascadeBorder", 0.02f);
        TrySetProp(pipe, t, "shadowDepthBias", 0.5f);
        TrySetProp(pipe, t, "shadowNormalBias", 0.4f);
        TrySetProp(pipe, t, "mainLightShadowmapResolution", low ? 2048 : 4096);
        TrySetProp(pipe, t, "additionalLightsShadowmapResolution", low ? 1024 : 2048);
        TrySetProp(pipe, t, "supportsMainLightShadows", true);
        // Soft filter ÃƒÂÃ‚Â²ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â¼ÃƒÂÃ‚ÂºÃƒÂÃ‚Â½ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½ÃƒÂÃ‚Â¾ ÃƒÂÃ‚Â½ÃƒÂÃ‚Â° Ãƒâ€˜Ã¢â€šÂ¬Ãƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â²ÃƒÂÃ‚Â½Ãƒâ€˜Ã¢â‚¬â€œ pipeline, ÃƒÂÃ‚ÂºÃƒÂÃ‚Â¾ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸ ÃƒÂÃ‚Â¼ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â¶ÃƒÂÃ‚Â»ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â²ÃƒÂÃ‚Â¾
        TrySetProp(pipe, t, "supportsSoftShadows", false);
        TrySetProp(pipe, t, "softShadowQuality", 0);
    }

    static void TrySetProp(object obj, System.Type t, string name, object value)
    {
        var p = t.GetProperty(name,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        if (p == null || !p.CanWrite) return;
        try { p.SetValue(obj, value, null); }
        catch { /* ignore type mismatch */ }
    }

    static void SetupSkyAndFog()
    {
        // Fog = atmospheric perspective: ÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â°ÃƒÂÃ‚Â¹ ÃƒÂÃ‚Â·ÃƒÂÃ‚ÂµÃƒÂÃ‚Â¼ÃƒÂÃ‚Â»Ãƒâ€˜Ã¢â‚¬â€œ Ãƒâ€˜Ã¢â€šÂ¬ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â·Ãƒâ€˜Ã¢â‚¬Â¡ÃƒÂÃ‚Â¸ÃƒÂÃ‚Â½Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã¢â‚¬ÂÃƒâ€˜Ã¢â‚¬Å¡Ãƒâ€˜Ã…â€™Ãƒâ€˜Ã‚ÂÃƒâ€˜Ã‚Â ÃƒÂÃ‚Â² ÃƒÂÃ‚Â½ÃƒÂÃ‚ÂµÃƒÂÃ‚Â±Ãƒâ€˜Ã¢â‚¬â€œ (ÃƒÂÃ‚Â±ÃƒÂÃ‚ÂµÃƒÂÃ‚Â· ÃƒÂÃ‚Â·ÃƒÂÃ‚ÂµÃƒÂÃ‚Â»ÃƒÂÃ‚ÂµÃƒÂÃ‚Â½ÃƒÂÃ‚Â¾ÃƒÂÃ‚Â³ÃƒÂÃ‚Â¾ ÃƒÂÃ‚ÂºÃƒâ€˜Ã¢â‚¬â€œÃƒÂÃ‚Â»Ãƒâ€˜Ã…â€™Ãƒâ€˜Ã¢â‚¬Â Ãƒâ€˜Ã‚Â)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        EnvironmentTextures.EnsureLoaded();
        RenderSettings.fogColor = EnvironmentTextures.FogColor;
        RenderSettings.fogDensity = 0.00011f; // stronger horizon haze softens disk rim

        var cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.Lerp(EnvironmentTextures.FogColor, EnvironmentTextures.SkyZenith, 0.42f);
            cam.farClipPlane = 18000f;
            cam.nearClipPlane = 0.25f;
        }
    }

}
