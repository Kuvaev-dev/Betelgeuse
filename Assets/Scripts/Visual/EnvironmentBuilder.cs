using System.Collections;
using UnityEngine;

/// <summary>
/// Середовище посадки: диск Місяця, LZ pad, зірки, сонце, підхідні маркери.
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
        SetupSkyAndFog();
        yield return null;

        var existing = GameObject.Find("EnvironmentRoot");
        if (existing != null)
            Object.Destroy(existing);

        var root = new GameObject("EnvironmentRoot");

        yield return BuildLunarSurfaceRoutine(root.transform);
        BuildLandingPad(root.transform);
        yield return null;
        var starPs = BuildStarField(root.transform);
        BuildSunDisc(root.transform);
        BuildApproachLights(root.transform);

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
        // Balanced mesh: smooth enough rims, fast cold start
        int res = QualitySettings.GetQualityLevel() <= 1 ? 160 : 224;
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
        int nRocks = QualitySettings.GetQualityLevel() <= 1 ? 10 : 16;
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

    static float SampleApproxHeight(float x, float z) => SampleTerrainSurfaceY(x, z);

    /// <summary>
    /// Physics ground / deck top. ONE pad slab only — no stacked coplanar lids
    /// (those caused camera shimmer). Terrain under pad is sunk in LunarTerrainMesh.
    /// </summary>
    public const float PadSurfaceY = 0.05f;

    static void BuildLandingPad(Transform parent)
    {
        var old = GameObject.Find("LandingPad");
        if (old != null) Object.Destroy(old);

        var pad = new GameObject("LandingPad");
        pad.transform.SetParent(parent, false);

        // One deck slab + low collar + 4 slim beacons. No stacked coplanar lids.
        var deckMat = MakePadDeckMaterial("PadDeckSkin");
        var sideMat = VisualMaterials.Lit(new Color(0.26f, 0.27f, 0.29f), 0.02f, 0.05f);
        var collarMat = MakePadCollarMaterial("PadCollarSkin");
        GetBeaconMaterials(out var baseMat, out var poleMat, out var lampMat);

        const float top = PadSurfaceY;
        const float bottom = -1.35f;
        float half = (top - bottom) * 0.5f;
        float centerY = (top + bottom) * 0.5f;

        var deck = SmoothMesh.MakeCylinder("Deck", pad.transform,
            new Vector3(0f, centerY, 0f), 90f, half, deckMat);
        SetShadow(deck, true, true);

        // Collar sits fully below deck top; wider footprint blends into pit wall
        const float collarTop = top - 0.42f;
        const float collarBot = -1.1f;
        float cHalf = (collarTop - collarBot) * 0.5f;
        float cY = (collarTop + collarBot) * 0.5f;
        var collar = SmoothMesh.MakeCylinder("Collar", pad.transform,
            new Vector3(0f, cY, 0f), 112f, cHalf, collarMat);
        SetShadow(collar, true, true);

        // Thin steel lip under deck edge (side only feel — lower than top)
        var lip = SmoothMesh.MakeCylinder("DeckLip", pad.transform,
            new Vector3(0f, top - 0.12f, 0f), 91.2f, 0.08f, sideMat);
        SetShadow(lip, true, true);

        // Four corner beacons — same kit as approach markers
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Sin(a) * 36f, 0f, Mathf.Cos(a) * 36f);
            PlaceBeacon(pad.transform, $"PadBeacon_{i}", p, top, baseMat, poleMat, lampMat);
        }
    }

    /// <summary>Shared beacon kit: base + pole + soft white lamp (pad & approach identical).</summary>
    static void GetBeaconMaterials(out Material baseMat, out Material poleMat, out Material lampMat)
    {
        baseMat = VisualMaterials.Lit(new Color(0.3f, 0.31f, 0.33f), 0.06f, 0.1f);
        poleMat = VisualMaterials.Lit(new Color(0.42f, 0.43f, 0.46f), 0.1f, 0.18f);
        lampMat = VisualMaterials.Unlit(new Color(0.9f, 0.92f, 0.95f), new Color(0.7f, 0.75f, 0.85f));
    }

    /// <summary>
    /// One standard beacon. <paramref name="groundY"/> = surface under the base (pad top or terrain).
    /// </summary>
    static void PlaceBeacon(Transform parent, string id, Vector3 xz, float groundY,
        Material baseMat, Material poleMat, Material lampMat)
    {
        // Geometry matches pad corner beacons exactly
        var bBase = SmoothMesh.MakeCylinder($"{id}_Base", parent,
            xz + Vector3.up * (groundY + 0.15f), 0.55f, 0.14f, baseMat);
        var bPole = SmoothMesh.MakeCylinder($"{id}_Pole", parent,
            xz + Vector3.up * (groundY + 1.85f), 0.18f, 1.65f, poleMat);
        SetShadow(bBase, true, true);
        SetShadow(bPole, true, true);
        var lamp = SmoothMesh.MakeSphere($"{id}_Lamp", parent,
            xz + Vector3.up * (groundY + 3.7f), new Vector3(0.42f, 0.32f, 0.42f), lampMat);
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
    /// Dark-medium gray matte LZ (must read gray under harsh lunar sun ~2.8).
    /// Line art: distance rings, cross, diagonals, rim ticks, bullseye.
    /// Deck Ø ≈ 92 m → r=1 ≈ 46 m.
    /// </summary>
    static Material MakePadDeckMaterial(string name)
    {
        const int n = 768;
        // linear:false = sRGB albedo (correct for color textures in URP)
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, true, false);
        tex.name = name;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.anisoLevel = 8;
        var cols = new Color[n * n];

        // Slightly thicker lines so they survive mipmaps + distance
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

            // ── Base deck: darker even gray under harsh lunar sun ──
            float g0 = 0.14f
                     + PadHash(u * 9f, v * 9f) * 0.015f
                     + PadHash(u * 24f, v * 24f) * 0.008f;

            float bay = Mathf.Abs(r * 3f - Mathf.Round(r * 3f));
            g0 -= (1f - Mathf.SmoothStep(0f, 0.06f, bay)) * 0.03f;
            float a01 = ang / (Mathf.PI * 2f) + 0.5f;
            float spoke = Mathf.Abs(a01 * 8f - Mathf.Round(a01 * 8f));
            g0 -= (1f - Mathf.SmoothStep(0f, 0.03f, spoke)) * 0.02f;

            g0 -= Mathf.SmoothStep(0.88f, 1f, r) * 0.03f;
            g0 = Mathf.Clamp(g0, 0.08f, 0.22f);

            // Cool dark gray
            float rr = g0 * 0.97f, gg = g0 * 0.99f, bb = g0 * 1.03f;

            // ── Rich LZ line art ──
            float line = 0f;
            float[] rings = { 0.97f, 0.88f, 0.78f, 0.66f, 0.54f, 0.42f, 0.30f, 0.20f, 0.12f, 0.06f };
            for (int ri = 0; ri < rings.Length; ri++)
            {
                float w = (ri == 0 || ri == 4) ? wRing * 1.45f : wRing;
                line = Mathf.Max(line, RingLine(r, rings[ri], w));
            }

            // Primary + secondary cross
            if (r < 0.96f)
            {
                line = Mathf.Max(line, AxisLine(u, wCross * 1.15f));
                line = Mathf.Max(line, AxisLine(v, wCross * 1.15f));
            }
            // Full diagonals (not only stubs)
            if (r < 0.9f)
            {
                float d1 = Mathf.Abs(u - v) * 0.7071f;
                float d2 = Mathf.Abs(u + v) * 0.7071f;
                line = Mathf.Max(line, 1f - Mathf.SmoothStep(wCross * 0.3f, wCross * 0.95f, d1));
                line = Mathf.Max(line, 1f - Mathf.SmoothStep(wCross * 0.3f, wCross * 0.95f, d2));
            }

            // 16 azimuth ticks on outer + mid rings
            float angDeg = ang * Mathf.Rad2Deg;
            for (int t = 0; t < 16; t++)
            {
                float target = -180f + t * 22.5f;
                float da = Mathf.Abs(Mathf.DeltaAngle(angDeg, target));
                bool major = (t % 2) == 0;
                bool cardinal = (t % 4) == 0;
                float angW = cardinal ? 1.7f : (major ? 1.2f : 0.85f);
                // Outer ticks
                float r0 = cardinal ? 0.84f : (major ? 0.88f : 0.91f);
                if (da < angW && r > r0 && r < 0.985f)
                {
                    float aFade = 1f - da / angW;
                    float rFade = Mathf.SmoothStep(r0, r0 + 0.02f, r)
                                * (1f - Mathf.SmoothStep(0.96f, 0.985f, r));
                    line = Mathf.Max(line, aFade * rFade);
                }
                // Mid-ring ticks (TDZ)
                if (cardinal && da < 1.4f && r > 0.48f && r < 0.58f)
                    line = Mathf.Max(line, (1f - da / 1.4f) * 0.9f);
            }

            // Inner chevrons at 45° on TDZ (short arcs)
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

            // Soft radial hatch near center (same tone family, no filled blot)
            if (r > 0.08f && r < 0.22f)
            {
                float spokeH = Mathf.Abs(a01 * 16f - Mathf.Round(a01 * 16f));
                if (spokeH < 0.04f)
                    line = Mathf.Max(line, 1f - spokeH / 0.04f);
            }

            if (line > 0.02f)
            {
                // Medium-light paint (readable on darker deck)
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
        // Slight gray multiply so deck stays dark even if sun is harsh
        var tint = new Color(0.75f, 0.76f, 0.78f, 1f);
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
    /// Approach cues only — no paved strip (it sank into terrain and broke the lunar look).
    /// Short posts sit on terrain with a safety lift; palette matches regolith + pad gray.
    /// </summary>
    static void BuildApproachLights(Transform parent)
    {
        var old = GameObject.Find("ApproachCorridor");
        if (old != null) Object.Destroy(old);
        var old2 = GameObject.Find("ApproachMarkers");
        if (old2 != null) Object.Destroy(old2);

        var root = new GameObject("ApproachMarkers");
        root.transform.SetParent(parent, false);

        // Same materials + geometry as pad corner beacons
        GetBeaconMaterials(out var baseMat, out var poleMat, out var lampMat);

        const float xOff = 22f;
        for (int i = 0; i < 9; i++)
        {
            float z = LunarTerrainMesh.PadClearRadius + 8f + i * 38f;
            foreach (float x in new[] { -xOff, xOff })
            {
                float ground = SampleTerrainSurfaceY(x, z);
                // Small lift so base never clips terrain undulation
                float y = ground + 0.25f;
                PlaceBeacon(root.transform, $"AppBeacon_{i}_{x}",
                    new Vector3(x, 0f, z), y, baseMat, poleMat, lampMat);
            }
        }
    }

    /// <summary>
    /// Same height model as LunarTerrainMesh.SampleHeight (no craters) so props sit on mesh.
    /// PadHash ≡ terrain Noise2 (range −1…1).
    /// </summary>
    static float SampleTerrainSurfaceY(float x, float z)
    {
        float dist = Mathf.Sqrt(x * x + z * z);
        float clear = LunarTerrainMesh.PadClearRadius;

        if (dist <= clear)
        {
            float t = dist / Mathf.Max(1f, clear);
            return Mathf.Lerp(-1.6f, -0.15f, t * t);
        }

        float h = 0f;
        h += PadHash(x * 0.0016f, z * 0.0016f) * 2.8f;
        h += PadHash(x * 0.0048f + 11f, z * 0.0048f - 7f) * 1.15f;
        h += PadHash(x * 0.012f, z * 0.012f) * 0.35f;
        float edgeN = dist / Mathf.Max(1f, LunarTerrainMesh.TerrainRadius);
        h += edgeN * edgeN * 1.1f;

        float blend = Mathf.SmoothStep(0f, 1f, (dist - clear) / 32f);
        return h * blend;
    }

    static Material MakePadCollarMaterial(string name)
    {
        // Darker gray ring under deck — matches pit wall / regolith
        var mat = VisualMaterials.Lit(new Color(0.16f, 0.165f, 0.18f), 0.02f, 0.04f);
        mat.name = name;
        return mat;
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
        // Lunar sun: slightly softer so pad gray isn't crushed to white
        sun.color = new Color(1f, 0.98f, 0.94f);
        sun.intensity = 2.35f;
        // Hard shadows — soft filter grows in world space when zoomed out and looks blurry
        sun.shadows = LightShadows.Hard;
        sun.shadowStrength = 0.9f;
        // Bias tuned for large pad + thin legs (less acne, less peter-panning)
        sun.shadowBias = 0.03f;
        sun.shadowNormalBias = 0.35f;
        sun.shadowNearPlane = 0.15f;
        sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;
        // ~28° elevation — longer readable shadows of rocket on pad
        sun.transform.rotation = Quaternion.Euler(28f, -42f, 0f);

        // Soft fill + rim — vacuum still dark, white booster reads cleanly
        EnsureDir("FillLight", new Color(0.5f, 0.52f, 0.58f), 0.18f, Quaternion.Euler(200f, 55f, 0f));
        EnsureDir("RimLight", new Color(0.4f, 0.44f, 0.52f), 0.12f, Quaternion.Euler(-8f, 145f, 0f));

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.07f, 0.072f, 0.082f);
        RenderSettings.reflectionIntensity = 0.03f;

        // Baseline; CameraFollow.FitShadows keeps cascades sharp when orbit-zooming
        FitShadowsToFocusDepth(120f);
    }

    /// <summary>
    /// Keep directional shadow cascades dense around the orbit focus so zooming out
    /// does not push the rocket/pad into a low-res far cascade (blurry silhouettes).
    /// </summary>
    public static void FitShadowsToFocusDepth(float focusDepth)
    {
        bool low = (SystemInfo.graphicsMemorySize > 0 && SystemInfo.graphicsMemorySize < 3000)
                   || QualitySettings.GetQualityLevel() <= 1;

        focusDepth = Mathf.Max(8f, focusDepth);
        // Shadow volume just past the subject — not a fixed 1400 m that dilutes texels when close
        float shadowDist = Mathf.Clamp(focusDepth * 1.55f + 90f, low ? 180f : 220f, low ? 900f : 2200f);

        // Put cascade boundaries so the focus depth sits near the end of cascade 2/3
        // (highest useful density on the rocket + pad, not only near the camera lens).
        float f = Mathf.Clamp(focusDepth / shadowDist, 0.2f, 0.88f);
        var split4 = new Vector3(
            Mathf.Clamp(f * 0.28f, 0.04f, 0.18f),
            Mathf.Clamp(f * 0.58f, 0.12f, 0.42f),
            Mathf.Clamp(f * 0.92f, 0.28f, 0.78f));

        QualitySettings.shadowDistance = shadowDist;
        QualitySettings.shadowCascades = 4;
        QualitySettings.shadowCascade4Split = split4;
        QualitySettings.shadowResolution = low ? ShadowResolution.High : ShadowResolution.VeryHigh;
        // Disable soft shadow filter (Hard) — Soft blurs more as cascade texels grow
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
        // Minimal cascade blend — large border looks like soft blur when zooming
        TrySetProp(pipe, t, "cascadeBorder", 0.02f);
        TrySetProp(pipe, t, "shadowDepthBias", 0.5f);
        TrySetProp(pipe, t, "shadowNormalBias", 0.4f);
        TrySetProp(pipe, t, "mainLightShadowmapResolution", low ? 2048 : 4096);
        TrySetProp(pipe, t, "additionalLightsShadowmapResolution", low ? 1024 : 2048);
        TrySetProp(pipe, t, "supportsMainLightShadows", true);
        // Soft filter off at pipeline level when possible
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
