using System.Collections;
using UnityEngine;

/// <summary>
/// ÐŸÑ€Ð¾Ñ†ÐµÐ´ÑƒÑ€Ð½Ð° Ð¼Ð¾Ð´ÐµÐ»ÑŒ Ð»Ð¸ÑˆÐµ 1-Ð³Ð¾ ÑÑ‚ÑƒÐ¿ÐµÐ½Ñ ~28 Ð¼: ÐºÐ¾Ñ€Ð¿ÑƒÑ, fins, Ð½Ð¾Ð³Ð¸, ÑÐ¾Ð¿Ð»Ð°, FX.
/// </summary>
public static class RocketVisualBuilder
{
    public const float Height = 28f;   // compact visual proxy of F9 first stage ~42.6 m; ÐºÐ¾Ð¼Ð¿Ð°ÐºÑ‚Ð½Ð¸Ð¹ 1-Ð¹ ÑÑ‚ÑƒÐ¿Ñ–Ð½ÑŒ (Ð½Ðµ Ð²ÐµÑÑŒ Ð½Ð¾ÑÑ–Ð¹)
    public const float Radius = 1.83f; // Ã˜3.66 Ð¼
    public const string UpperStackName = "UpperStack";

    public static void Build(RocketPhysics rocket)
    {
        LunarTerrainMesh.Drain(BuildRoutine(rocket));
    }

    /// <summary>No-op: Ð²ÐµÑ€Ñ…Ð½Ñ– ÑÑ‚ÑƒÐ¿ÐµÐ½Ñ– Ð½Ðµ Ð¼Ð¾Ð´ÐµÐ»ÑŽÑŽÑ‚ÑŒÑÑ (Ð»Ð¸ÑˆÐµ 1-Ð¹).</summary>
    public static void SetUpperStackVisible(Transform rocketRoot, bool visible, bool animateAway = false)
    {
        if (rocketRoot == null) return;
        var t = rocketRoot.Find(UpperStackName);
        if (t != null) Object.Destroy(t.gameObject);
        var orphan = GameObject.Find(UpperStackName);
        if (orphan != null) Object.Destroy(orphan);
    }

    /// <summary>ÐŸÐ¾ÐºÑ€Ð¾ÐºÐ¾Ð²Ð° Ð·Ð±Ñ–Ñ€ÐºÐ° â€” yield Ð¼Ñ–Ð¶ Ð²Ð°Ð¶ÐºÐ¸Ð¼Ð¸ skins, Ñ‰Ð¾Ð± splash-ÑÐ¿Ñ–Ð½ÐµÑ€ ÐºÑ€ÑƒÑ‚Ð¸Ð²ÑÑ.</summary>
    public static IEnumerator BuildRoutine(RocketPhysics rocket)
    {
        if (rocket == null) yield break;
        Transform root = rocket.transform;
        root.localScale = Vector3.one;

        var mr = root.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
        var mf = root.GetComponent<MeshFilter>();
        if (mf != null) mf.sharedMesh = null;

        var rb = root.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
        }

        var existing = root.Find("Visual");
        if (existing != null) Object.Destroy(existing.gameObject);
        DestroyChild(root, UpperStackName);
        DestroyChild(root, "EngineFlame");
        DestroyChild(root, "EngineSmoke");
        DestroyChild(root, "EngineLight");

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root, false);

        // â”€â”€ ÐŸÐ°Ð»Ñ–Ñ‚Ñ€Ð° recoverable 1st-stage analogue (no logos) â”€â”€
        var white = MakeTankSkin("TankWhite", sootAmount: 0.10f, panelContrast: 0.62f, seed: 11);
        yield return null;
        var whiteLower = MakeTankSkin("TankLower", sootAmount: 0.78f, panelContrast: 0.45f, seed: 29);
        yield return null;
        var black = VisualMaterials.Lit(new Color(0.035f, 0.038f, 0.042f), 0.28f, 0.32f);
        var metal = VisualMaterials.Lit(new Color(0.78f, 0.8f, 0.84f), 0.9f, 0.68f);
        var titanium = VisualMaterials.Lit(new Color(0.62f, 0.64f, 0.68f), 0.85f, 0.5f);
        var carbon = VisualMaterials.Lit(new Color(0.055f, 0.057f, 0.062f), 0.18f, 0.28f);
        var silver = VisualMaterials.Lit(new Color(0.88f, 0.89f, 0.92f), 0.92f, 0.75f);
        var heat = MakeNozzleSkin("NozzleHeat", seed: 7);
        yield return null;
        var copper = VisualMaterials.Lit(new Color(0.72f, 0.42f, 0.24f), 0.92f, 0.38f);
        var darkMetal = VisualMaterials.Lit(new Color(0.14f, 0.15f, 0.17f), 0.82f, 0.4f);
        var hydra = VisualMaterials.Lit(new Color(0.90f, 0.91f, 0.93f), 0.55f, 0.62f);
        var interstageMat = MakeInterstageSkin("InterstageCFRP", seed: 41);
        yield return null;
        var legWhite = VisualMaterials.Lit(new Color(0.93f, 0.935f, 0.94f), 0.08f, 0.55f);

        // â”€â”€ Aft: clustered 9-engine octaweb (not toy cubes) â”€â”€
        BuildOctaweb(visual.transform, black, darkMetal, titanium, carbon);
        yield return null;

        float dBody = Radius * 2.0f;
        float yBot = 2.1f;

        // Tall sooty lower tank â€” reads from afar
        SmoothCyl("SootLo", visual.transform, yBot + 2.55f, dBody * 1.004f, 2.55f, whiteLower, 96);
        float bodyBot = yBot;

        const float bodyH = 20.5f;
        SmoothCyl("Stage1Body", visual.transform, yBot + bodyH * 0.5f, dBody, bodyH * 0.5f, white, 96);
        float bodyTop = yBot + bodyH;
        yBot = bodyTop;

        // Thin barrel / weld hoops â€” no vertical raceway
        const int nRings = 6;
        float ringLo = bodyBot + 1.15f;
        float ringHi = bodyTop - 0.85f;
        for (int i = 0; i < nRings; i++)
        {
            float y = Mathf.Lerp(ringLo, ringHi, i / (float)(nRings - 1));
            bool major = i == 0 || i == nRings - 1 || i == nRings / 2;
            float halfH = major ? 0.016f : 0.008f;
            float dia = major ? dBody * 1.009f : dBody * 1.0055f;
            SmoothCyl($"Hoop_{i}", visual.transform, y, dia, halfH, silver, 96);
        }
        yield return null;

        BuildDockingInterface(visual.transform, bodyTop, dBody,
            interstageMat, metal, titanium, carbon, darkMetal, silver);

        yield return null;
        BuildWings(visual.transform, carbon, black, darkMetal, bodyTop - 1.35f);
        yield return null;
        BuildLegs(visual.transform, black, metal, titanium, carbon, darkMetal, hydra, legWhite);
        yield return null;
        BuildNozzles(visual.transform, heat, metal, copper, titanium, darkMetal);
        yield return null;
        BuildEngineFX(visual.transform);

        var cap = root.GetComponent<CapsuleCollider>();
        if (cap != null)
        {
            cap.direction = 1;
            cap.height = Height;
            cap.radius = Radius * 1.1f;
            cap.center = new Vector3(0f, Height * 0.5f, 0f);
            cap.enabled = false;
        }
    }

    /// <summary>
    /// Ð†Ð´ÐµÐ°Ð»ÑŒÐ½Ð° Ð·Ð¾Ð½Ð° ÑÑ‚Ð¸ÐºÐ¾Ð²ÐºÐ¸: Ð¿Ñ€Ð¾Ð¿Ð¾Ñ€Ñ†Ñ–Ð¹Ð½Ð¸Ð¹ interstage + Ñ‡Ð¸ÑÑ‚Ð¸Ð¹ sep flange.
    /// </summary>
    static void BuildDockingInterface(Transform visual, float bodyTop, float dBody,
        Material interstageMat, Material metal, Material titanium, Material carbon,
        Material darkMetal, Material silver)
    {
        // CFRP interstage + one sep plane â€” not a stack of random cylinders
        float interH = 1.75f;
        float interBot = bodyTop - 0.02f;
        float interCenter = interBot + interH * 0.5f;
        float topY = interBot + interH;

        SmoothCyl("Interstage", visual, interCenter, dBody * 1.004f, interH * 0.5f, interstageMat, 96);
        SmoothCyl("JoinRing", visual, interBot + 0.045f, dBody * 1.016f, 0.045f, metal, 96);
        SmoothCyl("SepFlange", visual, topY - 0.048f, dBody * 1.028f, 0.048f, titanium, 96);
        SmoothCyl("SepLip", visual, topY - 0.010f, dBody * 1.034f, 0.014f, silver, 96);
        SmoothCyl("SepWell", visual, topY - 0.42f, dBody * 0.90f, 0.36f, carbon, 72);
        SmoothCyl("SepFloor", visual, topY - 0.82f, dBody * 0.84f, 0.045f, darkMetal, 72);

        float rPush = dBody * 0.38f;
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            SmoothCylAt($"SepPush_{i}", visual,
                new Vector3(Mathf.Sin(a) * rPush, topY - 0.10f, Mathf.Cos(a) * rPush),
                0.20f, 0.035f, titanium, 32);
        }
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // ÐŸÑ€Ð¾Ñ†ÐµÐ´ÑƒÑ€Ð½Ñ– Ñ†Ð¸Ð»Ñ–Ð½Ð´Ñ€Ð¸Ñ‡Ð½Ñ– skins â€” Ð³Ð»Ð°Ð´ÐºÐ¸Ð¹ Ð±Ñ–Ð»Ð¸Ð¹ (Ð±ÐµÐ· Ð¿Ð°Ð½ÐµÐ»ÐµÐ¹)
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    static Material MakeTankSkin(string name, float sootAmount, float panelContrast, int seed)
    {
        const int tw = 512;
        const int th = 1024;
        var tex = new Texture2D(tw, th, TextureFormat.RGB24, true, false);
        tex.name = name + "_Albedo";
        tex.wrapModeU = TextureWrapMode.Repeat;
        tex.wrapModeV = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 8;

        var cols = new Color[tw * th];
        var rng = new System.Random(seed);
        float ox = (float)rng.NextDouble() * 40f;
        float oy = (float)rng.NextDouble() * 40f;

        for (int y = 0; y < th; y++)
        {
            float v = y / (float)(th - 1);
            for (int x = 0; x < tw; x++)
            {
                float u = x / (float)tw;
                int idx = y * tw + x;

                // White LOX/RP-1 tank with weld rings + stringers
                float g = 0.985f;
                g += HashNoise(u * 14f + ox, v * 28f + oy) * 0.004f;

                float line = 0f;

                // Horizontal weld / barrel rings
                const int nTexRings = 7;
                const float v0 = 0.06f;
                const float v1 = 0.94f;
                float step = (v1 - v0) / (nTexRings - 1);
                for (int ri = 0; ri < nTexRings; ri++)
                {
                    float rv = v0 + ri * step;
                    float d = Mathf.Abs(v - rv);
                    float halfW = (ri % 3 == 0) ? 0.0048f : 0.0024f;
                    line = Mathf.Max(line, 1f - Mathf.SmoothStep(0.0005f, halfW, d));
                }

                // No vertical stringers â€” they read as a black raceway from the camera.

                // Rivets on two major rings
                float rivetV = v0 + 3 * step;
                if (Mathf.Abs(v - rivetV) < 0.010f || Mathf.Abs(v - (v0 + 6 * step)) < 0.010f)
                {
                    float dots = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 40f));
                    dots = 1f - Mathf.SmoothStep(0.10f, 0.38f, dots);
                    line = Mathf.Max(line, dots * 0.50f);
                }

                // Bright weld highlight rather than a dirty groove
                g += line * (0.035f + 0.02f * panelContrast);
                g -= line * 0.012f;


                if (sootAmount > 0.01f)
                {
                    float sootV = Mathf.Clamp01(1f - v * 1.05f);
                    sootV = sootV * sootV;
                    float blotch = 0.65f + 0.35f * HashNoise(u * 5.5f, v * 8f + oy);
                    float streak = Mathf.Pow(Mathf.Clamp01(0.5f + 0.5f * HashNoise(u * 22f + ox, v * 2.4f)), 2.2f);
                    g -= sootAmount * sootV * (0.55f * blotch + 0.22f * streak);
                }

                g = Mathf.Clamp(g, 0.16f, 0.999f);
                cols[idx] = new Color(
                    Mathf.Clamp01(g * 0.997f),
                    Mathf.Clamp01(g * 1.0f),
                    Mathf.Clamp01(g * 1.008f), 1f);
            }
        }

        tex.SetPixels(cols);
        tex.Apply(true, true);

        var mat = new Material(VisualMaterials.LitShader);
        mat.name = name;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.02f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", sootAmount > 0.2f ? 0.38f : 0.86f);
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.EnableKeyword("_BASEMAP");
        }
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        mat.mainTexture = tex;
        // Ð‘ÐµÐ· normal map Ð¿Ð°Ð½ÐµÐ»ÐµÐ¹ â€” Ñ–Ð´ÐµÐ°Ð»ÑŒÐ½Ð¾ Ð³Ð»Ð°Ð´ÐºÐ¸Ð¹ ÐºÐ¾Ñ€Ð¿ÑƒÑ
        if (mat.HasProperty("_BumpMap"))
        {
            mat.SetTexture("_BumpMap", null);
            mat.DisableKeyword("_NORMALMAP");
        }
        return mat;
    }

    static float SampleGray(float u, float v, int nVert, int nHoriz, float sootAmount, float panelContrast, float ox, float oy)
    {
        u = u - Mathf.Floor(u);
        v = Mathf.Clamp01(v);
        float g = 0.985f;
        g += HashNoise(u * 28f + ox, v * 50f + oy) * 0.003f;
        if (sootAmount > 0.01f)
        {
            float sootV = Mathf.Clamp01(1f - v * 1.65f);
            sootV *= sootV;
            g -= sootAmount * sootV * 0.25f;
        }
        return g;
    }

    static Material MakeInterstageSkin(string name, int seed)
    {
        const int tw = 512;
        const int th = 512;
        var tex = new Texture2D(tw, th, TextureFormat.RGB24, true, false);
        tex.name = name;
        tex.wrapModeU = TextureWrapMode.Repeat;
        tex.wrapModeV = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 6;

        var cols = new Color[tw * th];
        var rng = new System.Random(seed);
        float ox = (float)rng.NextDouble() * 20f;

        for (int y = 0; y < th; y++)
        {
            float v = y / (float)(th - 1);
            for (int x = 0; x < tw; x++)
            {
                float u = x / (float)tw;
                // Ð“Ð»Ð¸Ð±Ð¾ÐºÐµ Ð¿Ð»ÐµÑ‚Ð¸Ð²Ð¾ CFRP Ð· Ð»ÐµÐ³ÐºÐ¸Ð¼Ð¸ ÑÐ¼ÑƒÐ³Ð°Ð¼Ð¸ Ð±Ð»Ð¸ÑÐºÑƒ
                float g = 0.055f;
                float weaveU = Mathf.Abs((u * 40f) - Mathf.Round(u * 40f));
                float weaveV = Mathf.Abs((v * 22f) - Mathf.Round(v * 22f));
                g += (1f - Mathf.SmoothStep(0f, 0.12f, weaveU)) * 0.028f;
                g += (1f - Mathf.SmoothStep(0f, 0.12f, weaveV)) * 0.022f;
                g += HashNoise(u * 48f + ox, v * 48f) * 0.012f;
                float band = Mathf.Abs((v * 5f) - Mathf.Round(v * 5f));
                g += (1f - Mathf.SmoothStep(0f, 0.07f, band)) * 0.045f;
                // Ð›ÐµÐ³ÐºÐ¸Ð¹ Ð²ÐµÑ€Ñ‚Ð¸ÐºÐ°Ð»ÑŒÐ½Ð¸Ð¹ Ð³Ñ€Ð°Ð´Ñ–Ñ”Ð½Ñ‚ Ð±Ð»Ð¸ÑÐºÑƒ
                g += (0.5f - Mathf.Abs(v - 0.5f)) * 0.02f;
                g = Mathf.Clamp01(g);
                cols[y * tw + x] = new Color(g * 0.92f, g * 0.95f, g * 1.08f, 1f);
            }
        }
        tex.SetPixels(cols);
        tex.Apply(true, true);

        var mat = new Material(VisualMaterials.LitShader);
        mat.name = name;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.42f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.52f);
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.EnableKeyword("_BASEMAP");
        }
        mat.mainTexture = tex;
        return mat;
    }

    static Material MakeFairingSkin(string name, int seed)
    {
        const int tw = 512;
        const int th = 512;
        var tex = new Texture2D(tw, th, TextureFormat.RGB24, true, false);
        tex.name = name;
        tex.wrapModeU = TextureWrapMode.Repeat;
        tex.wrapModeV = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 8;

        var nrm = new Texture2D(tw, th, TextureFormat.RGBA32, true, true);
        nrm.name = name + "_N";
        nrm.wrapModeU = TextureWrapMode.Repeat;
        nrm.wrapModeV = TextureWrapMode.Clamp;
        nrm.filterMode = FilterMode.Trilinear;

        var cols = new Color[tw * th];
        var nrmCols = new Color[tw * th];
        var rng = new System.Random(seed);
        float ox = (float)rng.NextDouble() * 30f;

        for (int y = 0; y < th; y++)
        {
            float v = y / (float)(th - 1);
            for (int x = 0; x < tw; x++)
            {
                float u = x / (float)tw;
                // Ð§Ð¸ÑÑ‚Ð¸Ð¹ Ð¾Ð±Ñ‚Ñ–Ñ‡Ð½Ð¸Ðº: Ð»ÐµÐ³ÐºÐ¸Ð¹ micro-noise + Ð»Ð¸ÑˆÐµ 2 Ð¿Ð¾Ð·Ð´Ð¾Ð²Ð¶Ð½Ñ– half-seams
                float g = 0.94f;
                g += HashNoise(u * 28f + ox, v * 36f) * 0.008f;
                float petal = u * 2f; // Ð´Ð²Ñ– Ð¿Ð¾Ð»Ð¾Ð²Ð¸Ð½Ð¸ Ð¾Ð±Ñ‚Ñ–Ñ‡Ð½Ð¸ÐºÐ°
                float seam = Mathf.Abs(petal - Mathf.Round(petal));
                float seamW = 1f - Mathf.SmoothStep(0f, 0.012f, seam);
                g -= seamW * 0.06f;
                // Ð»Ð¸ÑˆÐµ Ð¾Ð´Ð½Ðµ access-ÐºÑ–Ð»ÑŒÑ†Ðµ Ð±Ñ–Ð»Ñ Ð¾ÑÐ½Ð¾Ð²Ð¸
                float h = Mathf.Abs(v - 0.12f);
                g -= (1f - Mathf.SmoothStep(0f, 0.025f, h)) * 0.03f;
                // tip Ñ‚Ñ€Ð¾Ñ…Ð¸ ÑÐ²Ñ–Ñ‚Ð»Ñ–ÑˆÐ¸Ð¹ (Ð³Ð»Ð°Ð´ÐºÐ° Ñ„Ð°Ñ€Ð±Ð°)
                g *= Mathf.Lerp(0.97f, 1.0f, v);
                g = Mathf.Clamp01(g);
                cols[y * tw + x] = new Color(g * 0.995f, g, g * 1.01f, 1f);

                float du = seamW * 0.45f * Mathf.Sign(petal - Mathf.Round(petal) + 1e-4f);
                Vector3 tn = new Vector3(-du, 0f, 1f).normalized;
                nrmCols[y * tw + x] = new Color(
                    tn.x * 0.5f + 0.5f, tn.y * 0.5f + 0.5f, tn.z * 0.5f + 0.5f, 1f);
            }
        }
        tex.SetPixels(cols);
        tex.Apply(true, true);
        nrm.SetPixels(nrmCols);
        nrm.Apply(true, true);

        var mat = new Material(VisualMaterials.LitShader);
        mat.name = name;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.05f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.68f);
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
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 0.45f);
        }
        return mat;
    }

    static Material MakeNozzleSkin(string name, int seed)
    {
        const int tw = 512;
        const int th = 512;
        var tex = new Texture2D(tw, th, TextureFormat.RGB24, true, false);
        tex.name = name;
        tex.wrapModeU = TextureWrapMode.Repeat;
        tex.wrapModeV = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 4;

        var cols = new Color[tw * th];
        var rng = new System.Random(seed);
        float ox = (float)rng.NextDouble() * 10f;

        for (int y = 0; y < th; y++)
        {
            float v = y / (float)(th - 1);
            for (int x = 0; x < tw; x++)
            {
                float u = x / (float)tw;
                // Regen-cooled niobium bell: sooty exit â†’ heat-stained copper mid â†’ steel throat
                float mid = Mathf.Sin(v * Mathf.PI);
                float body = Mathf.Lerp(0.22f, 0.58f, mid);
                float ring = Mathf.Abs((v * 28f) - Mathf.Round(v * 28f));
                body += (1f - Mathf.SmoothStep(0f, 0.09f, ring)) * 0.10f;
                float ch = Mathf.Abs((u * 64f) - Mathf.Round(u * 64f));
                body += (1f - Mathf.SmoothStep(0f, 0.06f, ch)) * 0.055f;
                body += HashNoise(u * 18f + ox, v * 28f) * 0.03f;
                body = Mathf.Clamp01(body);

                float rC = body * (0.78f + 0.55f * mid);
                float gC = body * (0.40f + 0.22f * mid);
                float bC = body * (0.20f + 0.08f * mid);
                rC = Mathf.Min(rC, 0.92f);
                gC = Mathf.Min(gC, 0.62f);
                bC = Mathf.Min(bC, 0.38f);
                // Sooty exit lip
                if (v < 0.16f)
                {
                    float t = 1f - v / 0.16f;
                    rC = Mathf.Lerp(rC, 0.10f, t * 0.82f);
                    gC = Mathf.Lerp(gC, 0.09f, t * 0.82f);
                    bC = Mathf.Lerp(bC, 0.09f, t * 0.82f);
                }
                // Steel / inconel throat
                if (v > 0.74f)
                {
                    float t = (v - 0.74f) / 0.26f;
                    rC = Mathf.Lerp(rC, 0.58f, t);
                    gC = Mathf.Lerp(gC, 0.54f, t);
                    bC = Mathf.Lerp(bC, 0.50f, t);
                }
                cols[y * tw + x] = new Color(rC, gC, bC, 1f);
            }
        }
        tex.SetPixels(cols);
        tex.Apply(true, true);

        var mat = new Material(VisualMaterials.LitShader);
        mat.name = name;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.86f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.42f);
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.EnableKeyword("_BASEMAP");
        }
        mat.mainTexture = tex;
        return mat;
    }

    static float HashNoise(float x, float y)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        float fx = x - x0;
        float fy = y - y0;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        float v00 = Hash(x0, y0);
        float v10 = Hash(x0 + 1, y0);
        float v01 = Hash(x0, y0 + 1);
        float v11 = Hash(x0 + 1, y0 + 1);
        return Mathf.Lerp(Mathf.Lerp(v00, v10, fx), Mathf.Lerp(v01, v11, fx), fy) * 2f - 1f;
    }

    static float Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7fffffff) / (float)0x7fffffff;
        }
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // ÐŸÑ–Ð´Ð²ÑƒÐ·Ð»Ð¸
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    static void BuildWings(Transform visual, Material skin, Material fair, Material edge, float mountY)
    {
        // Four solid carbon canards, deep-cut into the forward tank. No blobs, no cards.
        const float span = 1.38f;
        const float rootChord = 2.15f;
        const float tipChord = 0.98f;
        const float rootThick = 0.34f;
        const float tipThick = 0.12f;
        const float embed = 0.10f;
        _ = fair;
        _ = edge;

        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            Vector3 radial = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            SmoothMesh.MakeFin($"Wing_{i}", visual,
                radial * Radius + Vector3.up * mountY,
                Quaternion.LookRotation(radial, Vector3.up),
                skin, span, rootChord, tipChord, rootThick, tipThick, embed, Radius);
        }
    }

    static void BuildLegs(Transform visual, Material black, Material metal, Material titanium,
        Material carbon, Material darkMetal, Material hydra, Material legWhite)
    {
        // Deployed landing pose: carbon boom, white hydraulic, crush core, circular footpad
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            var legRoot = new GameObject($"LegAsm_{i}");
            legRoot.transform.SetParent(visual, false);

            Vector3 radial = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            Vector3 hipPos = radial * Radius + Vector3.up * 6.35f;
            Vector3 hinge = radial * (Radius + 0.08f) + Vector3.up * 6.35f;
            Vector3 foot = radial * (Radius + 5.6f) + Vector3.up * 0.06f;

            var hip = new GameObject("Hip");
            hip.transform.SetParent(legRoot.transform, false);
            hip.transform.localPosition = hipPos;
            hip.transform.localRotation = Quaternion.LookRotation(radial, Vector3.up);
            SmoothSphere("Fair", hip.transform, new Vector3(0f, 0.04f, 0.05f), new Vector3(0.88f, 1.12f, 0.72f), carbon);
            SmoothCylAt("Collar", hip.transform, new Vector3(0f, 0f, 0.10f), 0.72f, 0.18f, black, 40);
            SmoothCylAt("CollarLip", hip.transform, new Vector3(0f, 0f, 0.22f), 0.58f, 0.06f, darkMetal, 40);

            SmoothSphere("Hinge", legRoot.transform, hinge, Vector3.one * 0.56f, titanium);
            SmoothCylAt("HingeCap", legRoot.transform, hinge + Vector3.up * 0.16f, 0.50f, 0.07f, darkMetal, 40);

            // Flattened carbon boom + white inner face (coherent F9-like black/white)
            Beam(legRoot.transform, "Boom", hinge, foot, 0.62f, 0.12f, black);
            Beam(legRoot.transform, "BoomInner",
                hinge - radial * 0.04f, foot - radial * 0.035f, 0.48f, 0.03f, legWhite);
            Strut(legRoot.transform, "BoomEdgeHi",
                hinge + Vector3.up * 0.12f,
                foot + Vector3.up * 0.10f, 0.08f, titanium);
            Strut(legRoot.transform, "BoomEdgeLo",
                hinge - Vector3.up * 0.08f,
                foot + Vector3.up * 0.02f, 0.07f, darkMetal);

            Vector3 bodyAnchor = radial * Radius + Vector3.up * 3.85f;
            Vector3 boomMid = Vector3.Lerp(hinge, foot, 0.42f);
            Vector3 boomKnee = Vector3.Lerp(hinge, foot, 0.70f);

            // White hydraulic ram + dark piston rod
            Vector3 hydEnd = Vector3.Lerp(bodyAnchor, boomMid, 0.62f);
            Strut(legRoot.transform, "HydBarrel", bodyAnchor, hydEnd, 0.16f, hydra);
            Strut(legRoot.transform, "HydRod", hydEnd, boomMid, 0.07f, metal);
            Strut(legRoot.transform, "LockLink", bodyAnchor + Vector3.up * 0.85f, boomKnee, 0.075f, titanium);
            SmoothSphere("HydJoint", legRoot.transform, bodyAnchor, Vector3.one * 0.28f, metal);
            SmoothSphere("HydKnee", legRoot.transform, boomMid, Vector3.one * 0.18f, titanium);
            SmoothSphere("HydGland", legRoot.transform, hydEnd, Vector3.one * 0.20f, darkMetal);

            // Stacked crush core + circular footpad
            for (int k = 0; k < 5; k++)
            {
                float yk = 0.18f + k * 0.11f;
                float dk = 0.92f - k * 0.06f;
                SmoothCylAt($"Crush_{k}", legRoot.transform, foot + Vector3.up * yk, dk, 0.045f, carbon, 32);
            }
            SmoothCylAt("CrushLip", legRoot.transform, foot + Vector3.up * 0.16f, 1.05f, 0.04f, darkMetal, 40);
            SmoothCylAt("Foot", legRoot.transform, foot + Vector3.up * 0.10f, 1.85f, 0.06f, metal, 48);
            SmoothCylAt("FootPad", legRoot.transform, foot, 2.25f, 0.032f, black, 48);
            SmoothCylAt("FootRing", legRoot.transform, foot + Vector3.up * 0.045f, 2.05f, 0.018f, titanium, 48);
            SmoothCylAt("FootGrip", legRoot.transform, foot + Vector3.up * 0.018f, 1.45f, 0.014f, darkMetal, 40);
        }
    }

    static void BuildOctaweb(Transform visual, Material black, Material darkMetal, Material titanium, Material carbon)
    {
        // Clustered 9-engine plate: rim + core + I-beams between bells (not 8 toy cubes)
        SmoothCyl("OctawebPlate", visual, 0.36f, Radius * 2.24f, 0.11f, darkMetal, 96);
        SmoothCyl("OctawebCore", visual, 0.50f, Radius * 1.05f, 0.14f, carbon, 64);
        SmoothCyl("OctRim", visual, 0.28f, Radius * 2.28f, 0.055f, black, 96);
        SmoothCyl("AftSkirt", visual, 1.32f, Radius * 2.04f, 0.52f, black, 96);
        SmoothCyl("AftJoin", visual, 1.95f, Radius * 2.01f, 0.065f, black, 96);
        SmoothCyl("AftLip", visual, 0.78f, Radius * 2.12f, 0.055f, black, 96);
        SmoothCyl("OctRing", visual, 0.40f, Radius * 1.62f, 0.04f, titanium, 64);

        for (int i = 0; i < 8; i++)
        {
            float a = (i * 45f + 22.5f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            Vector3 from = dir * 0.42f + Vector3.up * 0.40f;
            Vector3 to = dir * (Radius * 0.95f) + Vector3.up * 0.40f;
            Beam(visual, $"OctWeb_{i}", from, to, 0.10f, 0.16f, black);
            Strut(visual, $"OctFlange_{i}",
                from + Vector3.up * 0.07f, to + Vector3.up * 0.07f, 0.045f, darkMetal);
        }

        // Circumferential hoop at outer engine ring
        for (int i = 0; i < 8; i++)
        {
            float a0 = i * 45f * Mathf.Deg2Rad;
            float a1 = (i + 1) * 45f * Mathf.Deg2Rad;
            Vector3 p0 = new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)) * 1.30f + Vector3.up * 0.40f;
            Vector3 p1 = new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1)) * 1.30f + Vector3.up * 0.40f;
            Strut(visual, $"OctHoop_{i}", p0, p1, 0.055f, titanium);
        }
    }

    static void Beam(Transform parent, string name, Vector3 from, Vector3 to, float width, float thickness, Material mat)
    {
        Vector3 delta = to - from;
        float len = delta.magnitude;
        if (len < 1e-4f) return;
        var go = Prim(PrimitiveType.Cube, name, parent, (from + to) * 0.5f, Vector3.one, mat);
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        go.transform.localScale = new Vector3(width, len, thickness);
    }

    static void Strut(Transform parent, string name, Vector3 from, Vector3 to, float thickness, Material mat)
    {
        Vector3 delta = to - from;
        float len = delta.magnitude;
        if (len < 1e-4f) return;

        var go = SmoothMesh.MakeCylinder(name, parent, (from + to) * 0.5f, thickness, len * 0.5f, mat, 32);
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        var r = go.GetComponent<MeshRenderer>();
        if (r != null)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
        }
    }

    static void BuildNozzles(Transform visual, Material heat, Material metal, Material copper,
        Material titanium, Material darkMetal)
    {
        // 9 Merlin-class bells: heat-stained niobium/copper, center slightly larger
        Nozzle(visual, Vector3.zero, heat, metal, copper, titanium, darkMetal, 1.00f, true, 0);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Nozzle(visual,
                new Vector3(Mathf.Sin(a) * 1.32f, 0f, Mathf.Cos(a) * 1.32f),
                heat, metal, copper, titanium, darkMetal, 0.84f, false, i + 1);
        }
    }

    static void Nozzle(Transform parent, Vector3 xz, Material heat, Material metal, Material copper,
        Material titanium, Material darkMetal, float s, bool center, int index)
    {
        var eng = new GameObject(center ? "MerlinCenter" : $"Merlin_{index}");
        eng.transform.SetParent(parent, false);
        var t = eng.transform;

        SmoothMesh.MakeBell("Bell", t,
            new Vector3(xz.x, 0.52f * s, xz.z),
            1.34f * s, 0.78f * s, heat);
        SmoothCylAt("Exit", t,
            new Vector3(xz.x, 0.02f * s, xz.z), 1.42f * s, 0.030f * s, darkMetal, 48);
        SmoothCylAt("CuBand", t,
            new Vector3(xz.x, 0.58f * s, xz.z), 1.16f * s, 0.038f * s, copper, 48);
        SmoothCylAt("Throat", t,
            new Vector3(xz.x, 1.28f * s, xz.z), 0.38f * s, 0.085f * s, copper, 40);
        SmoothCylAt("Gimbal", t,
            new Vector3(xz.x, 1.44f * s, xz.z), 0.50f * s, 0.055f * s, titanium, 40);
        SmoothCylAt("Mount", t,
            new Vector3(xz.x, 1.58f * s, xz.z), 0.44f * s, 0.07f * s, darkMetal, 40);
        SmoothCylAt("Collar", t,
            new Vector3(xz.x, 1.72f * s, xz.z), 0.64f * s, 0.065f * s, darkMetal, 40);

        if (center)
        {
            SmoothCylAt("Turbopump", t,
                new Vector3(xz.x, 1.92f * s, xz.z), 0.50f * s, 0.14f * s, titanium, 40);
            SmoothCylAt("TpBelt", t,
                new Vector3(xz.x, 2.08f * s, xz.z), 0.36f * s, 0.05f * s, metal, 32);
        }
        else
        {
            Vector3 radial = new Vector3(xz.x, 0f, xz.z).normalized;
            Vector3 act0 = new Vector3(xz.x, 1.50f * s, xz.z) + radial * (0.18f * s);
            Vector3 act1 = act0 + Vector3.up * (0.22f * s);
            Strut(t, "Tvc", act0, act1, 0.055f * s, metal);
        }
    }

    static void BuildEngineFX(Transform visual)
    {
        // ÐšÑ€Ð¾Ð¼ÐºÐ° exit ÑÐ¾Ð¿Ð»Ð° Ð±Ñ–Ð»Ñ yâ‰ˆ0. Ð”ÐµÐ²'ÑÑ‚ÑŒ ÑÑ‚Ñ€ÑƒÐ¼ÐµÐ½Ñ–Ð² Merlin (RP-1): glow + core + sheath.
        const float exitY = -0.08f;
        var down = Quaternion.Euler(90f, 0f, 0f);

        var flames = new ParticleSystem[9];
        var cores = new ParticleSystem[9];
        var glows = new ParticleSystem[9];
        var jetRoots = new Transform[9];
        var jetRenderers = new MeshRenderer[18];
        var glowBalls = new Transform[9];
        // Core white-blue -> gold/orange sheath -> translucent tip smoke.
        var sheathMat = VisualMaterials.Plume(
            new Color(0.75f, 0.92f, 1.00f),
            new Color(1.00f, 0.62f, 0.18f),
            new Color(0.95f, 0.22f, 0.04f), 3.1f);
        var coreMat = VisualMaterials.Plume(
            new Color(0.82f, 0.95f, 1.00f),
            new Color(1.00f, 0.98f, 0.88f),
            new Color(1.00f, 0.55f, 0.14f), 5.6f);
        var glowMat = VisualMaterials.Lit(
            new Color(1f, 0.85f, 0.45f), 0.05f, 0.9f,
            new Color(2.4f, 1.4f, 0.35f));

        for (int i = 0; i < 9; i++)
        {
            Vector3 xz;
            float s;
            if (i == 0)
            {
                xz = Vector3.zero;
                s = 1f;
            }
            else
            {
                float a = (i - 1) * 45f * Mathf.Deg2Rad;
                xz = new Vector3(Mathf.Sin(a) * 1.32f, 0f, Mathf.Cos(a) * 1.32f);
                s = 0.84f;
            }

            var jet = new GameObject(i == 0 ? "Jet" : $"Jet_{i}");
            jet.transform.SetParent(visual, false);
            jet.transform.localPosition = new Vector3(xz.x, exitY, xz.z);
            jet.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            jetRoots[i] = jet.transform;
            float diam = 0.88f * s;
            float len = 15.5f * s;
            var sheathGo = SmoothMesh.MakePlume("Sheath", jet.transform, Vector3.zero, Quaternion.identity,
                diam, len, sheathMat);
            var coreMesh = SmoothMesh.MakePlume("Core", jet.transform, Vector3.zero, Quaternion.identity,
                diam * 0.36f, len * 1.18f, coreMat);
            jetRenderers[i * 2] = sheathGo.GetComponent<MeshRenderer>();
            jetRenderers[i * 2 + 1] = coreMesh.GetComponent<MeshRenderer>();

            var glowBall = SmoothSphere($"BellGlow_{i}", visual,
                new Vector3(xz.x, exitY - 0.08f * s, xz.z),
                Vector3.one * (0.70f * s), glowMat);
            glowBalls[i] = glowBall.transform;

            var glowGo = new GameObject(i == 0 ? "EngineGlow" : $"EngineGlow_{i}");
            glowGo.transform.SetParent(visual, false);
            glowGo.transform.localPosition = new Vector3(xz.x, exitY + 0.04f * s, xz.z);
            glowGo.transform.localRotation = down;
            glows[i] = glowGo.AddComponent<ParticleSystem>();
            ConfigureFlameGlow(glows[i], s);

            var flameGo = new GameObject(i == 0 ? "EngineFlame" : $"EngineFlame_{i}");
            flameGo.transform.SetParent(visual, false);
            flameGo.transform.localPosition = new Vector3(xz.x, exitY, xz.z);
            flameGo.transform.localRotation = down;
            flames[i] = flameGo.AddComponent<ParticleSystem>();
            ConfigureFlameOuter(flames[i], s);

            var coreGo = new GameObject(i == 0 ? "EngineFlameCore" : $"EngineFlameCore_{i}");
            coreGo.transform.SetParent(visual, false);
            coreGo.transform.localPosition = new Vector3(xz.x, exitY - 0.02f * s, xz.z);
            coreGo.transform.localRotation = down;
            cores[i] = coreGo.AddComponent<ParticleSystem>();
            ConfigureFlameCore(cores[i], s);
        }

        var smokeGo = new GameObject("EngineSmoke");
        smokeGo.transform.SetParent(visual, false);
        smokeGo.transform.localPosition = new Vector3(0f, exitY - 1.8f, 0f);
        smokeGo.transform.localRotation = down;
        var smoke = smokeGo.AddComponent<ParticleSystem>();
        ConfigureSmoke(smoke);

        var sparkGo = new GameObject("EngineSparks");
        sparkGo.transform.SetParent(visual, false);
        sparkGo.transform.localPosition = new Vector3(0f, exitY, 0f);
        sparkGo.transform.localRotation = down;
        var sparks = sparkGo.AddComponent<ParticleSystem>();
        ConfigureSparks(sparks);

        var dustGo = new GameObject("EngineDust");
        dustGo.transform.SetParent(visual, false);
        dustGo.transform.localPosition = new Vector3(0f, exitY - 0.5f, 0f);
        dustGo.transform.localRotation = down;
        var dust = dustGo.AddComponent<ParticleSystem>();
        ConfigureDust(dust);

        var lightGo = new GameObject("EngineLight");
        lightGo.transform.SetParent(visual, false);
        lightGo.transform.localPosition = new Vector3(0f, exitY - 1.6f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.48f, 0.14f);
        light.intensity = 0f;
        light.range = 160f;
        light.shadows = LightShadows.None;

        var fx = visual.gameObject.AddComponent<RocketEngineFX>();
        fx.flame = flames[0];
        fx.flameCore = cores[0];
        fx.plumes = flames;
        fx.cores = cores;
        fx.glows = glows;
        fx.jetRoots = jetRoots;
        fx.jetRenderers = jetRenderers;
        fx.glowBalls = glowBalls;
        fx.smoke = smoke;
        fx.sparks = sparks;
        fx.dust = dust;
        fx.engineLight = light;
        fx.maxFlameRate = 64f;
        fx.maxCoreRate = 52f;
        fx.maxGlowRate = 78f;
        fx.maxLightIntensity = 210f;
        fx.lightRange = 185f;
    }

    /// <summary>Unity Ð²Ð¸Ð¼Ð°Ð³Ð°Ñ”, Ñ‰Ð¾Ð± Ð£Ð¡Ð† Ð¾ÑÑ– velocityOverLifetime Ð±ÑƒÐ»Ð¸ Ð² Ð¾Ð´Ð½Ð¾Ð¼Ñƒ Ñ€ÐµÐ¶Ð¸Ð¼Ñ– MinMaxCurve.</summary>
    static void DisableVelocityOverLifetime(ParticleSystem ps)
    {
        var vel = ps.velocityOverLifetime;
        vel.enabled = false;
        vel.x = new ParticleSystem.MinMaxCurve(0f);
        vel.y = new ParticleSystem.MinMaxCurve(0f);
        vel.z = new ParticleSystem.MinMaxCurve(0f);
        vel.speedModifier = new ParticleSystem.MinMaxCurve(1f);
    }

    static void StyleFlameRenderer(ParticleSystem ps, ParticleSystemRenderMode mode,
        Color tint, float lengthScale, float velocityScale, float fudge)
    {
        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = mode;
        rend.lengthScale = lengthScale;
        rend.velocityScale = velocityScale;
        rend.cameraVelocityScale = 0f;
        rend.sharedMaterial = VisualMaterials.ParticleAdditive(tint);
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        rend.sortingFudge = fudge;
        rend.minParticleSize = 0f;
        rend.maxParticleSize = 8f;
        rend.allowRoll = false;
    }

        static void ConfigureFlameGlow(ParticleSystem ps, float s = 1f)
    {
        // Soft luminous plug in the bell — hot throat bloom, not a particle spray.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f * s, 5.5f * s);
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(0.48f * s, 0.95f * s);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.95f, 1f, 0.9f),
            new Color(1f, 0.78f, 0.35f, 0.75f));
        main.maxParticles = 64;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.20f * s;
        shape.radiusThickness = 1f;

        DisableVelocityOverLifetime(ps);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.90f, 0.97f, 1.00f), 0f),
                new GradientColorKey(new Color(1.00f, 0.88f, 0.55f), 0.35f),
                new GradientColorKey(new Color(1.00f, 0.48f, 0.12f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.25f, 0f),
                new GradientAlphaKey(0.85f, 0.16f),
                new GradientAlphaKey(0.35f, 0.62f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.separateAxes = false;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f, 0.55f),
                new Keyframe(0.28f, 1.20f),
                new Keyframe(1f, 0.35f)));

        StyleFlameRenderer(ps, ParticleSystemRenderMode.Billboard,
            new Color(1f, 0.86f, 0.48f, 1f), 1f, 0f, -8f);
    }
    static void ConfigureFlameOuter(ParticleSystem ps, float s = 1f)
    {
        // Soft stretched sheath: pale throat, orange body, translucent smoke tip.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.13f, 0.26f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(82f * s, 138f * s);
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f * s, 0.48f * s);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.92f, 0.96f, 1.00f, 0.70f),
            new Color(1.00f, 0.52f, 0.12f, 0.55f));
        main.maxParticles = 360;
        main.gravityModifier = 0.012f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 2.8f;
        shape.radius = 0.30f * s;
        shape.radiusThickness = 0.55f;
        shape.arc = 360f;
        shape.alignToDirection = false;
        shape.randomDirectionAmount = 0.055f;

        DisableVelocityOverLifetime(ps);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.88f, 0.95f, 1.00f), 0f),
                new GradientColorKey(new Color(1.00f, 0.90f, 0.55f), 0.12f),
                new GradientColorKey(new Color(1.00f, 0.55f, 0.14f), 0.38f),
                new GradientColorKey(new Color(0.88f, 0.20f, 0.04f), 0.70f),
                new GradientColorKey(new Color(0.20f, 0.10f, 0.06f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.08f, 0f),
                new GradientAlphaKey(0.72f, 0.07f),
                new GradientAlphaKey(0.48f, 0.30f),
                new GradientAlphaKey(0.18f, 0.68f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.separateAxes = false;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f, 0.38f),
                new Keyframe(0.10f, 1.05f),
                new Keyframe(0.42f, 1.15f),
                new Keyframe(0.75f, 0.78f),
                new Keyframe(1f, 0.12f)));

        var noise = ps.noise;
        noise.enabled = true;
        noise.separateAxes = false;
        noise.strength = new ParticleSystem.MinMaxCurve(0.28f);
        noise.frequency = 1.05f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(2.4f);
        noise.damping = true;
        noise.octaveCount = 2;
        noise.quality = ParticleSystemNoiseQuality.High;

        StyleFlameRenderer(ps, ParticleSystemRenderMode.Stretch,
            new Color(1f, 0.58f, 0.18f, 0.9f), 5.6f, 0.020f, -2f);
    }
    static void ConfigureFlameCore(ParticleSystem ps, float s = 1f)
    {
        // Tight white-blue needle with soft shock-diamond pulses.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.09f, 0.18f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(118f * s, 185f * s);
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f * s, 0.16f * s);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.78f, 0.93f, 1.00f, 1f),
            new Color(1.00f, 0.96f, 0.78f, 0.9f));
        main.maxParticles = 240;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0.95f;
        shape.radius = 0.11f * s;
        shape.radiusThickness = 0.30f;
        shape.alignToDirection = false;

        DisableVelocityOverLifetime(ps);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.80f, 0.94f, 1.00f), 0f),
                new GradientColorKey(new Color(1.00f, 1.00f, 0.95f), 0.18f),
                new GradientColorKey(new Color(1.00f, 0.82f, 0.40f), 0.48f),
                new GradientColorKey(new Color(1.00f, 0.45f, 0.10f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.35f, 0f),
                new GradientAlphaKey(0.95f, 0.07f),
                new GradientAlphaKey(0.70f, 0.35f),
                new GradientAlphaKey(0.22f, 0.78f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.separateAxes = false;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0.00f, 0.45f),
                new Keyframe(0.08f, 1.00f),
                new Keyframe(0.20f, 0.72f),
                new Keyframe(0.32f, 1.10f),
                new Keyframe(0.48f, 0.68f),
                new Keyframe(0.62f, 0.95f),
                new Keyframe(1.00f, 0.14f)));

        var noise = ps.noise;
        noise.enabled = true;
        noise.separateAxes = false;
        noise.strength = new ParticleSystem.MinMaxCurve(0.14f);
        noise.frequency = 0.85f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(2.0f);
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.High;

        StyleFlameRenderer(ps, ParticleSystemRenderMode.Stretch,
            new Color(0.90f, 0.96f, 1.00f, 1f), 6.4f, 0.028f, -7f);
    }
static void ConfigureSmoke(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(12f, 38f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.6f, 4.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.72f, 0.72f, 0.75f, 0.22f),
            new Color(0.35f, 0.35f, 0.38f, 0.10f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 280;
        main.gravityModifier = -0.02f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 14f;
        shape.radius = 1.4f;

        DisableVelocityOverLifetime(ps);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.7f, 0.7f, 0.73f), 0f),
                new GradientColorKey(new Color(0.45f, 0.45f, 0.48f), 0.45f),
                new GradientColorKey(new Color(0.22f, 0.22f, 0.24f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.18f, 0f),
                new GradientAlphaKey(0.12f, 0.25f),
                new GradientAlphaKey(0.05f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.separateAxes = false;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f, 0.4f),
                new Keyframe(0.35f, 1.0f),
                new Keyframe(1f, 2.4f)));

        var noise = ps.noise;
        noise.enabled = true;
        noise.separateAxes = false;
        noise.strength = new ParticleSystem.MinMaxCurve(0.85f);
        noise.frequency = 0.35f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.4f);
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.separateAxes = false;
        rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
        // Ð¢Ñ€Ð¸Ð¼Ð°Ñ‚Ð¸ x/y Ñƒ Ñ‚Ð¾Ð¼Ñƒ Ð¶ mode, Ñ‰Ð¾ z, ÐºÐ¾Ð»Ð¸ separateAxes=false â€” Unity Ð²Ð¸ÐºÐ¾Ñ€Ð¸ÑÑ‚Ð¾Ð²ÑƒÑ” Ð»Ð¸ÑˆÐµ z

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.Particle(new Color(0.5f, 0.5f, 0.52f, 0.15f));
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.sortingFudge = 5f;
    }

    static void ConfigureSparks(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(60f, 140f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.32f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.95f, 0.7f, 1f),
            new Color(1f, 0.55f, 0.2f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 280;
        main.gravityModifier = 0.25f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 11f;
        shape.radius = 1.1f;

        DisableVelocityOverLifetime(ps);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 1f, 0.9f), 0f),
                new GradientColorKey(new Color(1f, 0.6f, 0.2f), 0.6f),
                new GradientColorKey(new Color(0.4f, 0.1f, 0.05f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.7f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Stretch;
        rend.lengthScale = 2.4f;
        rend.velocityScale = 0.08f;
        rend.sharedMaterial = VisualMaterials.ParticleAdditive(new Color(1f, 0.85f, 0.4f, 1f));
    }

    static void ConfigureDust(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 28f);
        main.startSize = new ParticleSystem.MinMaxCurve(2.5f, 8f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.62f, 0.62f, 0.64f, 0.26f),
            new Color(0.42f, 0.42f, 0.44f, 0.10f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        main.gravityModifier = 0.08f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 55f;
        shape.radius = 2.5f;

        DisableVelocityOverLifetime(ps);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.65f, 0.65f, 0.67f), 0f),
                new GradientColorKey(new Color(0.45f, 0.45f, 0.47f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.0f, 0f),
                new GradientAlphaKey(0.20f, 0.15f),
                new GradientAlphaKey(0.09f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            AnimationCurve.Linear(0f, 0.5f, 1f, 2.8f));

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 1.1f;
        noise.frequency = 0.25f;
        noise.scrollSpeed = 0.3f;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.Particle(new Color(0.55f, 0.55f, 0.57f, 0.18f));
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static void SmoothCyl(string name, Transform parent, float y, float diameter, float halfHeight, Material mat, int segments = 96)
        => SmoothCylAt(name, parent, new Vector3(0f, y, 0f), diameter, halfHeight, mat, segments);

    static GameObject SmoothCylAt(string name, Transform parent, Vector3 pos, float diameter, float halfHeight, Material mat, int segments = 96)
    {
        var go = SmoothMesh.MakeCylinder(name, parent, pos, diameter, halfHeight, mat, segments);
        var r = go.GetComponent<MeshRenderer>();
        if (r != null)
        {
            bool thin = halfHeight < 0.2f;
            r.shadowCastingMode = thin
                ? UnityEngine.Rendering.ShadowCastingMode.Off
                : UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = !thin;
        }
        return go;
    }

    static GameObject SmoothSphere(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = SmoothMesh.MakeSphere(name, parent, pos, scale, mat);
        var r = go.GetComponent<MeshRenderer>();
        if (r != null)
        {
            bool small = scale.x < 0.5f;
            r.shadowCastingMode = small
                ? UnityEngine.Rendering.ShadowCastingMode.Off
                : UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = !small;
        }
        return go;
    }

    static GameObject SmoothCapsule(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        => SmoothMesh.MakeCapsule(name, parent, pos, scale, mat);

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        if (type == PrimitiveType.Sphere)
            return SmoothSphere(name, parent, localPos, scale, mat);
        if (type == PrimitiveType.Cylinder)
            return SmoothCylAt(name, parent, localPos, Mathf.Max(scale.x, scale.z), scale.y, mat);
        if (type == PrimitiveType.Capsule)
            return SmoothCapsule(name, parent, localPos, scale, mat);

        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        var col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        var r = go.GetComponent<MeshRenderer>();
        if (r != null)
        {
            r.sharedMaterial = mat;
            bool thin = scale.y < 0.2f || Mathf.Min(scale.x, scale.z) < 0.4f;
            if (thin || name.Contains("Ring") || name.Contains("Stripe") || name.StartsWith("H_") || name.StartsWith("V_"))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            else
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }
        }
        return go;
    }

    static void DestroyChild(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) Object.Destroy(t.gameObject);
    }
}

