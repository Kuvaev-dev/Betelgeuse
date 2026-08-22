using System.Collections;
using UnityEngine;

/// <summary>
/// Середовище посадки: crater-диск Місяця, industrial pad, підхідні маркери, небо.
/// Рельєф — один heightmap-меш (LunarTerrainMesh); pad — smooth cylinders/rings.
/// </summary>
public static class EnvironmentBuilder
{
    public static void Build()
    {
        LunarTerrainMesh.Drain(BuildRoutine());
    }

    /// <summary>Stepped build — yields so splash spinner keeps spinning.</summary>
    public static IEnumerator BuildRoutine()
    {
        SetupLighting(out Light sun);
        yield return null;
        SetupSkyAndFog();
        yield return null;

        var existing = GameObject.Find("EnvironmentRoot");
        if (existing != null)
            Object.Destroy(existing);

        var root = new GameObject("EnvironmentRoot");

        yield return BuildLunarSurfaceRoutine(root.transform);
        yield return null;
        BuildLandingPad(root.transform);
        yield return null;
        var starPs = BuildStarField(root.transform);
        yield return null;
        BuildSunDisc(root.transform);
        yield return null;
        BuildApproachLights(root.transform);
        yield return null;

        var amb = SpaceAmbience.Ensure();
        amb.Bind(root.transform, starPs, sun);
    }

    static IEnumerator BuildLunarSurfaceRoutine(Transform parent)
    {
        var surface = new GameObject("LunarSurface");
        surface.transform.SetParent(parent, false);

        var regolith = VisualMaterials.Lit(
            new Color(0.40f, 0.405f, 0.42f),
            metallic: 0.0f,
            smooth: 0.028f);

        float R = LunarTerrainMesh.TerrainRadius;
        // Higher mesh res → smooth circular crater rims (low res looked ragged)
        int res = QualitySettings.GetQualityLevel() <= 1 ? 320 : 420;
        yield return LunarTerrainMesh.CreateRoutine(surface.transform, regolith, null, res, R);

        // Horizon ring uses the same NASA LROC albedo (darker, no normal — cheap far field)
        var farMat = MakeHorizonMaterial();
        var far = SmoothMesh.MakeCylinder("HorizonDisk", surface.transform,
            new Vector3(0f, -2.8f, 0f), R * 2f, 2.2f, farMat);
        var fr = far.GetComponent<MeshRenderer>();
        if (fr != null)
        {
            fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fr.receiveShadows = true;
        }
        yield return null;

        var rng = new System.Random(17);
        float clear = LunarTerrainMesh.PadClearRadius + 25f;
        var rockMat = MakeRockMaterial();
        int nRocks = QualitySettings.GetQualityLevel() <= 1 ? 14 : 22;
        for (int i = 0; i < nRocks; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float dist = clear + 40f + (float)rng.NextDouble() * (R * 0.75f - clear);
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            float s = 2.2f + (float)rng.NextDouble() * 5.5f;
            float h = SampleApproxHeight(x, z);
            SmoothMesh.MakeSphere($"Boulder_{i}", surface.transform,
                new Vector3(x, h + s * 0.12f, z),
                new Vector3(s * 1.0f, s * 0.42f, s * 0.95f),
                rockMat);
            if ((i & 3) == 0) yield return null;
        }
    }

    static float SampleApproxHeight(float x, float z)
    {
        float dist = Mathf.Sqrt(x * x + z * z);
        if (dist < LunarTerrainMesh.PadClearRadius) return 0f;
        float n = Mathf.PerlinNoise(x * 0.0018f + 10f, z * 0.0018f + 3f);
        float blend = Mathf.Clamp01((dist - LunarTerrainMesh.PadClearRadius) / 50f);
        return ((n - 0.5f) * 2.4f) * blend * blend;
    }

    static void BuildLandingPad(Transform parent)
    {
        var old = GameObject.Find("LandingPad");
        if (old != null) Object.Destroy(old);

        var pad = new GameObject("LandingPad");
        pad.transform.SetParent(parent, false);

        // Premium LZ palette — cool steel, crisp paint, restrained glow
        Color white = new Color(0.98f, 0.985f, 1f);
        Color amber = new Color(1f, 0.72f, 0.28f);
        Color cyan = new Color(0.45f, 0.88f, 1f);

        var deckMat = MakePadDeckMaterial("PadDeckSkin");
        var scorchMat = MakePadScorchMaterial("PadScorchSkin");
        var concrete = VisualMaterials.Lit(new Color(0.48f, 0.49f, 0.52f), 0.06f, 0.22f);
        var steel = VisualMaterials.Lit(new Color(0.66f, 0.68f, 0.72f), 0.82f, 0.48f);
        var dark = VisualMaterials.Lit(new Color(0.22f, 0.23f, 0.26f), 0.75f, 0.32f);
        var curb = VisualMaterials.Lit(new Color(0.38f, 0.39f, 0.42f), 0.60f, 0.38f);
        var regDeep = VisualMaterials.Lit(new Color(0.34f, 0.345f, 0.36f), 0f, 0.04f);
        var regMid = VisualMaterials.Lit(new Color(0.40f, 0.405f, 0.42f), 0f, 0.05f);
        var regLite = VisualMaterials.Lit(new Color(0.46f, 0.465f, 0.48f), 0f, 0.06f);
        var grate = VisualMaterials.Lit(new Color(0.14f, 0.145f, 0.16f), 0.88f, 0.28f);
        var whiteMat = VisualMaterials.Unlit(white, white * 0.85f);
        var amberMat = VisualMaterials.Unlit(amber, amber);
        var markMat = VisualMaterials.Unlit(new Color(0.94f, 0.95f, 0.98f), white * 0.7f);
        var ledMat = VisualMaterials.Unlit(cyan, cyan * 0.9f);
        var poleMat = VisualMaterials.Lit(new Color(0.55f, 0.57f, 0.61f), 0.85f, 0.55f);

        // ── Soft regolith apron (3 rings, no hard steps) ──
        var berm = SmoothMesh.MakeCylinder("Berm", pad.transform,
            new Vector3(0f, -0.28f, 0f), 136f, 0.36f, regDeep);
        berm.GetComponent<MeshRenderer>().receiveShadows = true;
        SmoothMesh.MakeCylinder("BermMid", pad.transform,
            new Vector3(0f, -0.06f, 0f), 126f, 0.16f, regMid);
        var bed = SmoothMesh.MakeCylinder("Bed", pad.transform,
            new Vector3(0f, 0.08f, 0f), 116f, 0.12f, regLite);
        bed.GetComponent<MeshRenderer>().receiveShadows = true;

        // Soft scorch halo (only outside deck)
        SmoothMesh.MakeRing("Scorch", pad.transform,
            new Vector3(0f, 0.18f, 0f), 112f, 0.82f, scorchMat);

        // ── Foundation stack ──
        var foundation = SmoothMesh.MakeCylinder("Foundation", pad.transform,
            new Vector3(0f, 0.24f, 0f), 110f, 0.14f, concrete);
        foundation.GetComponent<MeshRenderer>().shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.On;
        foundation.GetComponent<MeshRenderer>().receiveShadows = true;

        SmoothMesh.MakeCylinder("Subframe", pad.transform,
            new Vector3(0f, 0.40f, 0f), 106f, 0.07f, dark);

        // Elegant curb lip (single clean profile)
        SmoothMesh.MakeCylinder("Curb", pad.transform,
            new Vector3(0f, 0.54f, 0f), 103.5f, 0.12f, curb);
        SmoothMesh.MakeRing("CurbCap", pad.transform,
            new Vector3(0f, 0.68f, 0f), 104.2f, 0.952f, steel);
        SmoothMesh.MakeRing("CurbInner", pad.transform,
            new Vector3(0f, 0.66f, 0f), 101.2f, 0.975f, dark);

        // ── Deck surface ──
        const float deckY = 0.64f;
        var deck = SmoothMesh.MakeDisc("Deck", pad.transform,
            new Vector3(0f, deckY, 0f), 100f, 0.035f, deckMat);
        var dr = deck.GetComponent<MeshRenderer>();
        dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        dr.receiveShadows = true;
        SmoothMesh.MakeCylinder("DeckRim", pad.transform,
            new Vector3(0f, deckY - 0.05f, 0f), 100.15f, 0.05f, steel);

        // Center grate (simple)
        SmoothMesh.MakeDisc("Grate", pad.transform,
            new Vector3(0f, deckY + 0.015f, 0f), 14f, 0.02f, grate);
        SmoothMesh.MakeRing("GrateRim", pad.transform,
            new Vector3(0f, deckY + 0.03f, 0f), 15.5f, 0.90f, dark);

        // Markings: one rim, one TDZ, cross, bullseye, 4 leg pads
        float my = deckY + 0.07f;
        SmoothMesh.MakeRing("EdgeStripe", pad.transform,
            new Vector3(0f, my, 0f), 98.6f, 0.978f, markMat);
        SmoothMesh.MakeRing("TDZ", pad.transform,
            new Vector3(0f, my + 0.008f, 0f), 58f, 0.96f, markMat);
        MakeBox(pad.transform, "CrossX", new Vector3(0f, my + 0.03f, 0f),
            new Vector3(64f, 0.028f, 1.2f), whiteMat);
        MakeBox(pad.transform, "CrossZ", new Vector3(0f, my + 0.03f, 0f),
            new Vector3(1.2f, 0.028f, 64f), whiteMat);
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Sin(a) * 26f, my + 0.04f, Mathf.Cos(a) * 26f);
            SmoothMesh.MakeDisc($"Leg_{i}", pad.transform, p, 4.6f, 0.016f, dark);
            SmoothMesh.MakeRing($"LegR_{i}", pad.transform, p + Vector3.up * 0.012f, 5.0f, 0.88f, amberMat);
        }
        SmoothMesh.MakeDisc("Bull", pad.transform, new Vector3(0f, my + 0.05f, 0f), 7.5f, 0.016f, amberMat);
        SmoothMesh.MakeRing("BullR", pad.transform, new Vector3(0f, my + 0.06f, 0f), 9.5f, 0.85f, whiteMat);
        SmoothMesh.MakeRing("LedOuter", pad.transform,
            new Vector3(0f, 0.50f, 0f), 104.0f, 0.986f, ledMat);

        // 4 corner beacons only (no 16-panel ring clutter)
        var lampW = VisualMaterials.Unlit(white, white);
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Sin(a) * 58f, 0f, Mathf.Cos(a) * 58f);
            SmoothMesh.MakeCylinder($"BPole_{i}", pad.transform, p + Vector3.up * 2.2f, 0.4f, 2.1f, poleMat);
            SmoothMesh.MakeSphere($"BLamp_{i}", pad.transform,
                p + Vector3.up * 4.5f, new Vector3(0.95f, 0.65f, 0.95f), lampW);
        }
    }

    /// <summary>Premium planar deck: cool steel plates, soft wear, crisp seams.</summary>
    static Material MakePadDeckMaterial(string name)
    {
        // Compact 512 atlas — enough detail, faster load on weak PCs
        const int n = 512;
        var tex = new Texture2D(n, n, TextureFormat.RGB24, true, false);
        tex.name = name;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var cols = new Color[n * n];
        float cx = (n - 1) * 0.5f;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x - cx) / cx;
            float v = (y - cx) / cx;
            float r = Mathf.Sqrt(u * u + v * v);
            int idx = y * n + x;
            if (r > 1.002f) { cols[idx] = Color.black; continue; }

            float g = 0.56f + PadHash(u * 18f, v * 18f) * 0.02f;
            float ring = Mathf.Abs((r * 5f) - Mathf.Round(r * 5f));
            g -= (1f - Mathf.SmoothStep(0f, 0.06f, ring)) * 0.05f;
            float ang = Mathf.Atan2(v, u) / (Mathf.PI * 2f) + 0.5f;
            float sec = Mathf.Abs((ang * 8f) - Mathf.Round(ang * 8f));
            g -= (1f - Mathf.SmoothStep(0f, 0.03f, sec)) * 0.035f;
            g -= (1f - Mathf.SmoothStep(0.1f, 0.4f, r)) * 0.08f;
            g = Mathf.Clamp01(g);
            cols[idx] = new Color(g * 0.97f, g, g * 1.03f, 1f);
        }

        tex.SetPixels(cols);
        tex.Apply(true, true);

        var mat = new Material(VisualMaterials.LitShader);
        mat.name = name;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.4f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.34f);
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.EnableKeyword("_BASEMAP");
        }
        mat.mainTexture = tex;
        return mat;
    }

    static Material MakePadScorchMaterial(string name)
    {
        const int n = 512;
        var tex = new Texture2D(n, n, TextureFormat.RGB24, true, false);
        tex.name = name;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        var cols = new Color[n * n];
        float cx = (n - 1) * 0.5f;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x - cx) / cx;
            float v = (y - cx) / cx;
            float r = Mathf.Sqrt(u * u + v * v);
            float ang = Mathf.Atan2(v, u);
            // Soft radial scorch with angular blotches
            float band = Mathf.SmoothStep(0.55f, 0.85f, r) * (1f - Mathf.SmoothStep(0.9f, 1.05f, r));
            float blot = 0.55f + 0.45f * PadHash(Mathf.Cos(ang) * 3f + r * 4f, Mathf.Sin(ang) * 3f);
            float g = Mathf.Lerp(0.42f, 0.10f, band * blot);
            g += PadHash(u * 20f, v * 20f) * 0.03f;
            g = Mathf.Clamp01(g);
            cols[y * n + x] = new Color(g * 0.95f, g * 0.93f, g * 0.92f, 1f);
        }
        tex.SetPixels(cols);
        tex.Apply(true, true);

        var mat = new Material(VisualMaterials.LitShader);
        mat.name = name;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.12f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.14f);
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.EnableKeyword("_BASEMAP");
        }
        mat.mainTexture = tex;
        return mat;
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

    static void BuildApproachLights(Transform parent)
    {
        // Лише наземні маркери підходу (без «дороги під рельєфом»)
        var root = new GameObject("ApproachMarkers");
        root.transform.SetParent(parent, false);
        var poleMat = VisualMaterials.Lit(new Color(0.55f, 0.56f, 0.58f), 0.7f, 0.5f);

        // Sparse approach markers (6 pairs) — less clutter, cheaper
        for (int i = 1; i <= 6; i++)
        {
            float z = 90f + i * 55f;
            foreach (float x in new[] { -16f, 16f })
            {
                float y0 = Mathf.Max(0f, SampleApproxHeight(x, z)) + 0.2f;
                SmoothMesh.MakeCylinder($"AppPole_{i}_{x}", root.transform,
                    new Vector3(x, y0 + 1.4f, z), 0.35f, 1.4f, poleMat);
                Color c = i % 2 == 0 ? new Color(1f, 0.6f, 0.25f) : new Color(0.93f, 0.94f, 0.97f);
                SmoothMesh.MakeSphere($"AppLamp_{i}_{x}", root.transform,
                    new Vector3(x, y0 + 3.0f, z), Vector3.one * 0.75f, VisualMaterials.Unlit(c, c));
            }
        }
    }

    static ParticleSystem BuildStarField(Transform parent)
    {
        var root = new GameObject("StarField");
        root.transform.SetParent(parent, false);
        var go = new GameObject("Stars");
        go.transform.SetParent(root.transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.5f;
        main.startLifetime = 99999f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 2.0f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.87f, 0.95f, 0.85f), Color.white);
        main.maxParticles = 2800;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 2500) });

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 5200f;
        sh.radiusThickness = 0.5f;

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.Particle(Color.white);
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
        return ps;
    }

    static void BuildSunDisc(Transform parent)
    {
        var sky = new GameObject("SkyBodies");
        sky.transform.SetParent(parent, false);
        var sunMat = VisualMaterials.Unlit(new Color(1f, 0.98f, 0.94f), new Color(1f, 0.95f, 0.85f));
        SmoothMesh.MakeSphere("SunDisc", sky.transform,
            new Vector3(-2600f, 1700f, -1900f), Vector3.one * 100f, sunMat);
    }

    static Material MakeHorizonMaterial()
    {
        // Slightly deeper than the main disk — depth without looking black
        var mat = VisualMaterials.Lit(new Color(0.32f, 0.325f, 0.34f), 0f, 0.02f);
        mat.name = "HorizonDisk_SolidGray";
        return mat;
    }

    static Material MakeRockMaterial()
    {
        var mat = VisualMaterials.Lit(new Color(0.36f, 0.365f, 0.38f), 0.03f, 0.045f);
        mat.name = "Boulder_SolidGray";
        return mat;
    }

    static void SetupLighting(out Light sun)
    {
        sun = Object.FindAnyObjectByType<Light>();
        if (sun == null || sun.type != LightType.Directional)
        {
            var go = new GameObject("Sun");
            sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.name = "Sun";
        // Lunar sun: cool-white, soft penumbra, long shadows
        sun.color = new Color(1f, 0.985f, 0.95f);
        sun.intensity = 2.85f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.92f;
        // Lower bias → less “floating” shadow acne; soft looks more natural
        sun.shadowBias = 0.04f;
        sun.shadowNormalBias = 0.35f;
        sun.shadowNearPlane = 0.2f;
        // ~22° elevation — readable relief without harsh black voids
        sun.transform.rotation = Quaternion.Euler(22f, -48f, 0f);

        // Soft fill + rim — vacuum still dark, white booster reads cleanly
        EnsureDir("FillLight", new Color(0.48f, 0.50f, 0.56f), 0.14f, Quaternion.Euler(200f, 50f, 0f));
        EnsureDir("RimLight", new Color(0.42f, 0.46f, 0.55f), 0.10f, Quaternion.Euler(-6f, 150f, 0f));

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.065f, 0.068f, 0.078f);
        RenderSettings.reflectionIntensity = 0.04f;

        // Adaptive shadows — long range on strong GPUs, lighter on weak
        bool low = SystemInfo.graphicsMemorySize > 0 && SystemInfo.graphicsMemorySize < 3000
                   || QualitySettings.GetQualityLevel() <= 1;
        float shadowDist = low ? 500f : 1000f;
        QualitySettings.shadowDistance = shadowDist;
        QualitySettings.shadowCascades = low ? 2 : 4;
        QualitySettings.shadowCascade4Split = new Vector3(0.05f, 0.15f, 0.38f);
        QualitySettings.shadowResolution = low ? ShadowResolution.Medium : ShadowResolution.High;
        QualitySettings.shadows = ShadowQuality.All;
        ApplyUrpShadowDistance(shadowDist);
    }

    static void ApplyUrpShadowDistance(float distance)
    {
        var pipe = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        if (pipe == null) return;
        var t = pipe.GetType();
        // UniversalRenderPipelineAsset.shadowDistance / cascade4Split / biases
        TrySetProp(pipe, t, "shadowDistance", distance);
        TrySetProp(pipe, t, "shadowCascadeCount", 4);
        TrySetProp(pipe, t, "cascade4Split", new Vector3(0.05f, 0.15f, 0.38f));
        TrySetProp(pipe, t, "shadowDepthBias", 0.8f);
        TrySetProp(pipe, t, "shadowNormalBias", 0.6f);
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

    static void EnsureDir(string name, Color c, float i, Quaternion r)
    {
        var go = GameObject.Find(name);
        if (go == null) { go = new GameObject(name); go.AddComponent<Light>(); }
        var l = go.GetComponent<Light>();
        l.type = LightType.Directional;
        l.color = c;
        l.intensity = i;
        l.shadows = LightShadows.None;
        go.transform.rotation = r;
    }

    static void SetupSkyAndFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.012f, 0.012f, 0.014f);
        RenderSettings.fogDensity = 0.00001f;

        var cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.006f, 0.006f, 0.008f);
            cam.farClipPlane = 18000f;
            cam.nearClipPlane = 0.3f;
        }
    }

    static GameObject MakeBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        Object.Destroy(go.GetComponent<Collider>());
        var r = go.GetComponent<MeshRenderer>();
        if (r != null)
        {
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        return go;
    }
}
