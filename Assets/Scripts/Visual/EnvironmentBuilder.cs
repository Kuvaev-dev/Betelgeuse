using UnityEngine;

/// <summary>
/// Середовище посадки: crater-диск Місяця, industrial pad, підхідні маркери, небо.
/// Рельєф — один heightmap-меш (LunarTerrainMesh); pad — smooth cylinders/rings.
/// </summary>
public static class EnvironmentBuilder
{
    public static void Build()
    {
        SetupLighting(out Light sun);
        SetupSkyAndFog();

        var existing = GameObject.Find("EnvironmentRoot");
        if (existing != null)
            Object.Destroy(existing);

        var root = new GameObject("EnvironmentRoot");

        BuildLunarSurface(root.transform);
        BuildLandingPad(root.transform);
        var starPs = BuildStarField(root.transform);
        BuildSunDisc(root.transform);
        BuildApproachLights(root.transform);

        var amb = SpaceAmbience.Ensure();
        amb.Bind(root.transform, starPs, sun);
    }

    static void BuildLunarSurface(Transform parent)
    {
        var surface = new GameObject("LunarSurface");
        surface.transform.SetParent(parent, false);

        // Базовий mat (albedo + normal підставить LunarTerrainMesh)
        var regolith = VisualMaterials.Lit(
            new Color(0.58f, 0.585f, 0.595f),
            metallic: 0.0f,
            smooth: 0.03f);

        // Cratered disk — єдина видима поверхня (без сірого «обідка» зовні)
        float R = LunarTerrainMesh.TerrainRadius;
        LunarTerrainMesh.Create(surface.transform, regolith,
            resolution: 448, radius: R);

        // HorizonDisk = 2R (не виступає і не лишає сірого кільця)
        var farMat = VisualMaterials.Lit(new Color(0.34f, 0.345f, 0.355f), 0.0f, 0.02f);
        float underDiameter = R * 2f;
        var far = SmoothMesh.MakeCylinder("HorizonDisk", surface.transform,
            new Vector3(0f, -3.2f, 0f), underDiameter, 2.6f, farMat);
        var fr = far.GetComponent<MeshRenderer>();
        if (fr != null)
        {
            fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fr.receiveShadows = true;
        }

        // Валуни (smooth spheres) — cool-gray, частково «вкопані» в реголіт
        var rng = new System.Random(11);
        var rockA = new Color(0.36f, 0.365f, 0.375f);
        var rockB = new Color(0.50f, 0.505f, 0.515f);
        var rockC = new Color(0.44f, 0.445f, 0.455f);
        float clear = LunarTerrainMesh.PadClearRadius + 4f;
        var rockMatA = VisualMaterials.Lit(rockA, 0.02f, 0.06f);
        var rockMatB = VisualMaterials.Lit(rockB, 0.02f, 0.07f);
        var rockMatC = VisualMaterials.Lit(rockC, 0.03f, 0.05f);
        for (int i = 0; i < 110; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float dist = clear + 10f + (float)rng.NextDouble() * (R * 0.90f - clear);
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            float s = 1.0f + (float)rng.NextDouble() * 5.2f;
            float h = SampleApproxHeight(x, z);
            double pick = rng.NextDouble();
            var mat = pick < 0.34 ? rockMatA : (pick < 0.67 ? rockMatB : rockMatC);

            // Slightly flattened ellipsoids, buried ~25–40% so they don't float
            float bury = 0.28f + (float)rng.NextDouble() * 0.14f;
            var rock = SmoothMesh.MakeSphere($"Boulder_{i}", surface.transform,
                new Vector3(x, h + s * (0.5f - bury), z),
                new Vector3(
                    s * (0.72f + (float)rng.NextDouble() * 0.38f),
                    s * (0.40f + (float)rng.NextDouble() * 0.32f),
                    s * (0.72f + (float)rng.NextDouble() * 0.38f)),
                mat);
            rock.transform.localRotation = Quaternion.Euler(
                (float)rng.NextDouble() * 28f,
                (float)rng.NextDouble() * 360f,
                (float)rng.NextDouble() * 28f);
        }
    }

    static float SampleApproxHeight(float x, float z)
    {
        float dist = Mathf.Sqrt(x * x + z * z);
        if (dist < LunarTerrainMesh.PadClearRadius) return 0f;
        float n = Mathf.PerlinNoise(x * 0.004f + 10f, z * 0.004f + 3f);
        float blend = Mathf.Clamp01((dist - LunarTerrainMesh.PadClearRadius) / 50f);
        return ((n - 0.5f) * 3f) * blend * blend;
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
        var regDeep = VisualMaterials.Lit(new Color(0.42f, 0.425f, 0.435f), 0f, 0.04f);
        var regMid = VisualMaterials.Lit(new Color(0.50f, 0.505f, 0.515f), 0f, 0.05f);
        var regLite = VisualMaterials.Lit(new Color(0.56f, 0.565f, 0.575f), 0f, 0.06f);
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

        // Center exhaust grate (recessed dark disc + thin rings — no box clutter)
        SmoothMesh.MakeDisc("Grate", pad.transform,
            new Vector3(0f, deckY + 0.015f, 0f), 16f, 0.02f, grate);
        SmoothMesh.MakeRing("GrateRim", pad.transform,
            new Vector3(0f, deckY + 0.03f, 0f), 17.2f, 0.90f, dark);
        SmoothMesh.MakeRing("GrateInner", pad.transform,
            new Vector3(0f, deckY + 0.032f, 0f), 10f, 0.94f, steel);
        // Subtle radial grate lines (6 only)
        for (int i = 0; i < 6; i++)
        {
            var g = MakeBox(pad.transform, $"GrateLine_{i}",
                new Vector3(0f, deckY + 0.04f, 0f), new Vector3(0.14f, 0.03f, 15.2f), steel);
            g.transform.localRotation = Quaternion.Euler(0f, i * 30f, 0f);
        }

        // Sparse steel seams (texture carries most detail)
        var seam = VisualMaterials.Lit(new Color(0.72f, 0.74f, 0.78f), 0.72f, 0.52f);
        float[] seamD = { 88f, 64f, 40f };
        for (int i = 0; i < seamD.Length; i++)
            SmoothMesh.MakeRing($"Seam_{i}", pad.transform,
                new Vector3(0f, deckY + 0.04f + i * 0.003f, 0f), seamD[i], 0.991f, seam);
        for (int i = 0; i < 8; i++)
        {
            var s = MakeBox(pad.transform, $"Rad_{i}",
                new Vector3(0f, deckY + 0.045f, 0f), new Vector3(0.18f, 0.02f, 96f), seam);
            s.transform.localRotation = Quaternion.Euler(0f, i * 22.5f, 0f);
        }

        // ── Paint markings: cross + rings + bullseye only (no edge chevrons) ──
        float my = deckY + 0.07f;

        // Thin outer rim paint (single elegant edge, not triple chevrons)
        SmoothMesh.MakeRing("EdgeStripe", pad.transform,
            new Vector3(0f, my, 0f), 98.6f, 0.978f, markMat);

        // Concentric TDZ rings — evenly spaced, calm rhythm
        float[] rings = { 86f, 62f, 40f };
        float[] ratios = { 0.972f, 0.962f, 0.945f };
        for (int i = 0; i < rings.Length; i++)
            SmoothMesh.MakeRing($"TDZ_{i}", pad.transform,
                new Vector3(0f, my + 0.006f * i, 0f), rings[i], ratios[i], markMat);

        // Main cross — stops before outer rim (no competing edge bars)
        MakeBox(pad.transform, "CrossX", new Vector3(0f, my + 0.032f, 0f),
            new Vector3(72f, 0.028f, 1.25f), whiteMat);
        MakeBox(pad.transform, "CrossZ", new Vector3(0f, my + 0.032f, 0f),
            new Vector3(1.25f, 0.028f, 72f), whiteMat);
        // Soft end caps on cross arms (rounded look via short discs)
        float arm = 36f;
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Sin(a) * arm, my + 0.036f, Mathf.Cos(a) * arm);
            SmoothMesh.MakeDisc($"CrossCap_{i}", pad.transform, p, 1.35f, 0.012f, whiteMat);
        }

        // Leg targets at 45° (F9 feet)
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Sin(a) * 26f, my + 0.045f, Mathf.Cos(a) * 26f);
            SmoothMesh.MakeDisc($"Leg_{i}", pad.transform, p, 5.0f, 0.018f, dark);
            SmoothMesh.MakeRing($"LegR_{i}", pad.transform, p + Vector3.up * 0.014f, 5.4f, 0.87f, amberMat);
            SmoothMesh.MakeDisc($"LegD_{i}", pad.transform, p + Vector3.up * 0.022f, 1.15f, 0.012f, whiteMat);
        }

        // Center bullseye — clear aim hierarchy
        SmoothMesh.MakeDisc("Bull", pad.transform, new Vector3(0f, my + 0.05f, 0f), 8.0f, 0.018f, amberMat);
        SmoothMesh.MakeRing("BullR", pad.transform, new Vector3(0f, my + 0.062f, 0f), 10.0f, 0.84f, amberMat);
        SmoothMesh.MakeRing("BullW", pad.transform, new Vector3(0f, my + 0.068f, 0f), 11.8f, 0.93f, whiteMat);
        SmoothMesh.MakeDisc("BullDot", pad.transform, new Vector3(0f, my + 0.078f, 0f), 2.0f, 0.012f, whiteMat);

        // Single cyan LED under curb (less noise than dual rings)
        SmoothMesh.MakeRing("LedOuter", pad.transform,
            new Vector3(0f, 0.50f, 0f), 104.0f, 0.986f, ledMat);

        // ── Perimeter: restrained (12 panels, 8 beacons, no clutter) ──
        var panelMat = VisualMaterials.Lit(new Color(0.32f, 0.33f, 0.36f), 0.58f, 0.34f);
        var boltMat = VisualMaterials.Lit(new Color(0.75f, 0.77f, 0.80f), 0.90f, 0.65f);
        for (int i = 0; i < 16; i++)
        {
            float a = i * (360f / 16f) * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Sin(a) * 55.5f, 0.42f, Mathf.Cos(a) * 55.5f);
            var panel = SmoothMesh.MakeCylinder($"Panel_{i}", pad.transform, p, 2.8f, 0.18f, panelMat);
            panel.transform.localRotation = Quaternion.Euler(0f, i * (360f / 16f), 0f);
            if (i % 2 == 0)
                SmoothMesh.MakeSphere($"Bolt_{i}", pad.transform,
                    p + Vector3.up * 0.28f, new Vector3(0.38f, 0.22f, 0.38f), boltMat);
        }

        // Beacons — 8 poles with soft lamp heads
        var lampW = VisualMaterials.Unlit(white, white);
        var lampA = VisualMaterials.Unlit(amber, amber);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Sin(a) * 59f, 0f, Mathf.Cos(a) * 59f);
            Vector3 outward = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));

            SmoothMesh.MakeCylinder($"BBase_{i}", pad.transform, p + Vector3.up * 0.18f, 1.6f, 0.18f, poleMat);
            SmoothMesh.MakeCylinder($"BPole_{i}", pad.transform, p + Vector3.up * 2.5f, 0.42f, 2.3f, poleMat);
            SmoothMesh.MakeSphere($"BLamp_{i}", pad.transform,
                p + Vector3.up * 5.0f + outward * 0.15f,
                new Vector3(1.05f, 0.72f, 1.05f),
                i % 2 == 0 ? lampW : lampA);

            var lg = new GameObject($"BLight_{i}");
            lg.transform.SetParent(pad.transform, false);
            lg.transform.localPosition = p + Vector3.up * 4.6f;
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = i % 2 == 0 ? new Color(0.92f, 0.94f, 1f) : new Color(1f, 0.78f, 0.45f);
            l.intensity = 5.5f;
            l.range = 26f;
            l.shadows = LightShadows.None;
        }

        // Soft fill over pad
        var fill = new GameObject("PadFill");
        fill.transform.SetParent(pad.transform, false);
        fill.transform.position = new Vector3(0f, 20f, 0f);
        var fl = fill.AddComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(0.93f, 0.95f, 1f);
        fl.intensity = 36f;
        fl.range = 135f;
    }

    /// <summary>Premium planar deck: cool steel plates, soft wear, crisp seams.</summary>
    static Material MakePadDeckMaterial(string name)
    {
        const int n = 1024;
        var tex = new Texture2D(n, n, TextureFormat.RGB24, true, false);
        tex.name = name;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 8;
        var nrm = new Texture2D(n, n, TextureFormat.RGBA32, true, true);
        nrm.name = name + "_N";
        nrm.wrapMode = TextureWrapMode.Clamp;
        nrm.filterMode = FilterMode.Trilinear;

        var cols = new Color[n * n];
        var nrmCols = new Color[n * n];
        float cx = (n - 1) * 0.5f;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x - cx) / cx;
            float v = (y - cx) / cx;
            float r = Mathf.Sqrt(u * u + v * v);
            int idx = y * n + x;

            if (r > 1.002f)
            {
                cols[idx] = new Color(0.12f, 0.12f, 0.13f, 1f);
                nrmCols[idx] = new Color(0.5f, 0.5f, 1f, 1f);
                continue;
            }

            // Brushed steel base (cool)
            float g = 0.56f;
            g += PadHash(u * 14f, v * 14f) * 0.018f;
            g += PadHash(u * 42f + 2f, v * 42f - 1f) * 0.010f;
            // Fine brush along circumference
            float ang = Mathf.Atan2(v, u);
            g += Mathf.Sin(ang * 48f + r * 20f) * 0.006f;

            // Concentric plates (6 — cleaner than 9)
            float ring = Mathf.Abs((r * 6.5f) - Mathf.Round(r * 6.5f));
            float ringSeam = 1f - Mathf.SmoothStep(0f, 0.05f, ring);
            g -= ringSeam * 0.055f;

            // 8 radial sectors
            float a01 = ang / (Mathf.PI * 2f) + 0.5f;
            float sec = Mathf.Abs((a01 * 8f) - Mathf.Round(a01 * 8f));
            float radSeam = 1f - Mathf.SmoothStep(0f, 0.022f, sec);
            g -= radSeam * 0.04f;

            // Soft center heat darkening
            float center = 1f - Mathf.SmoothStep(0.08f, 0.42f, r);
            g -= center * 0.10f;
            // Outer rim slightly lighter
            g += Mathf.SmoothStep(0.82f, 1f, r) * 0.035f;

            // Micro grit
            g += PadHash(u * 160f, v * 160f) * 0.012f;
            g = Mathf.Clamp01(g);

            cols[idx] = new Color(
                Mathf.Clamp01(g * 0.97f),
                Mathf.Clamp01(g * 0.995f),
                Mathf.Clamp01(g * 1.04f), 1f);

            float bump = (ringSeam * 0.55f + radSeam * 0.45f);
            float du = -bump * 0.55f * Mathf.Sign(u + 1e-4f);
            float dv = -bump * 0.55f * Mathf.Sign(v + 1e-4f);
            Vector3 tn = new Vector3(du, dv, 1f).normalized;
            nrmCols[idx] = new Color(tn.x * 0.5f + 0.5f, tn.y * 0.5f + 0.5f, tn.z * 0.5f + 0.5f, 1f);
        }

        tex.SetPixels(cols);
        tex.Apply(true, true);
        nrm.SetPixels(nrmCols);
        nrm.Apply(true, true);

        var mat = new Material(VisualMaterials.LitShader);
        mat.name = name;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.42f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.36f);
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.EnableKeyword("_BASEMAP");
        }
        mat.mainTexture = tex;
        if (mat.HasProperty("_BumpMap"))
        {
            mat.SetTexture("_BumpMap", nrm);
            mat.EnableKeyword("_NORMALMAP");
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 0.5f);
        }
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

        var markMat = VisualMaterials.Unlit(
            new Color(0.95f, 0.95f, 0.97f),
            new Color(0.9f, 0.9f, 0.92f));

        for (int i = 1; i <= 14; i++)
        {
            float z = 78f + i * 38f;
            foreach (float x in new[] { -18f, 18f })
            {
                float y0 = Mathf.Max(0f, SampleApproxHeight(x, z)) + 0.2f;
                SmoothMesh.MakeCylinder($"AppPole_{i}", root.transform,
                    new Vector3(x, y0 + 1.6f, z), 0.4f, 1.6f, poleMat);

                Color c = i % 3 == 0
                    ? new Color(1f, 0.58f, 0.22f)
                    : new Color(0.93f, 0.94f, 0.97f);
                var lampMat = VisualMaterials.Unlit(c, c);
                SmoothMesh.MakeSphere($"AppLamp_{i}", root.transform,
                    new Vector3(x, y0 + 3.45f, z), Vector3.one * 0.9f, lampMat);
            }

            if (i % 2 == 0)
            {
                float h = Mathf.Max(0f, SampleApproxHeight(0f, z)) + 0.4f;
                SmoothMesh.MakeDisc($"AppMark_{i}", root.transform,
                    new Vector3(0f, h, z), 2.4f, 0.04f, markMat);
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

    static void SetupLighting(out Light sun)
    {
        sun = Object.FindFirstObjectByType<Light>();
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

        // Tiny fill + rim (vacuum still dark, but contact shadows readable)
        EnsureDir("FillLight", new Color(0.42f, 0.43f, 0.48f), 0.08f, Quaternion.Euler(200f, 50f, 0f));
        EnsureDir("RimLight", new Color(0.38f, 0.40f, 0.48f), 0.06f, Quaternion.Euler(-6f, 150f, 0f));

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.045f, 0.045f, 0.052f);
        RenderSettings.reflectionIntensity = 0.03f;

        // Terrain R=2000 m — default URP shadowDistance 50 m kills pad/rocket shadows mid-descent
        QualitySettings.shadowDistance = 1200f;
        QualitySettings.shadowCascades = 4;
        QualitySettings.shadowCascade4Split = new Vector3(0.05f, 0.15f, 0.38f);
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
        QualitySettings.shadows = ShadowQuality.All;

        // URP shadow distance via reflection (no hard ref to Universal assembly)
        ApplyUrpShadowDistance(1200f);
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

        var cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
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
