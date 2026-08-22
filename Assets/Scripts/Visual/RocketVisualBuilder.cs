using System.Collections;
using UnityEngine;

/// <summary>
/// Процедурна модель 1-го ступеня ~42 м (Falcon-class).
/// Чистий силует: UV-скіни несуть деталь; меші — тільки ключові форми.
/// </summary>
public static class RocketVisualBuilder
{
    public const float Height = 39.2f; // 1st-stage stack (interstage dome, no tall fairing)
    public const float Radius = 1.85f;

    public static void Build(RocketPhysics rocket)
    {
        LunarTerrainMesh.Drain(BuildRoutine(rocket));
    }

    /// <summary>Stepped build — yields between heavy skins so splash spinner keeps spinning.</summary>
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
        DestroyChild(root, "EngineFlame");
        DestroyChild(root, "EngineSmoke");
        DestroyChild(root, "EngineLight");

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root, false);

        // ── Palette — clean Falcon-class presentation ──
        var white = MakeTankSkin("TankWhite", sootAmount: 0.0f, panelContrast: 0.042f, seed: 11);
        yield return null;
        var whiteLower = MakeTankSkin("TankLower", sootAmount: 0.22f, panelContrast: 0.048f, seed: 29);
        yield return null;
        var black = VisualMaterials.Lit(new Color(0.035f, 0.037f, 0.042f), 0.62f, 0.38f);
        var metal = VisualMaterials.Lit(new Color(0.78f, 0.80f, 0.84f), 0.95f, 0.86f);
        var titanium = VisualMaterials.Lit(new Color(0.66f, 0.68f, 0.72f), 0.92f, 0.74f);
        var carbon = VisualMaterials.Lit(new Color(0.055f, 0.058f, 0.062f), 0.48f, 0.48f);
        var silver = VisualMaterials.Lit(new Color(0.90f, 0.92f, 0.95f), 0.94f, 0.84f);
        var heat = MakeNozzleSkin("NozzleHeat", seed: 7);
        yield return null;
        var copper = VisualMaterials.Lit(new Color(0.62f, 0.44f, 0.32f), 0.93f, 0.58f);
        var darkMetal = VisualMaterials.Lit(new Color(0.14f, 0.15f, 0.17f), 0.90f, 0.52f);
        var stripe = VisualMaterials.Lit(new Color(0.04f, 0.04f, 0.045f), 0.45f, 0.32f);
        var accent = VisualMaterials.Lit(new Color(0.12f, 0.42f, 0.78f), 0.35f, 0.55f); // quiet brand blue
        var hydra = VisualMaterials.Lit(new Color(0.86f, 0.88f, 0.92f), 0.88f, 0.62f);
        var whitePaint = VisualMaterials.Lit(new Color(0.96f, 0.97f, 0.985f), 0.08f, 0.78f);
        var interstageMat = MakeInterstageSkin("InterstageCFRP", seed: 41);
        yield return null;

        // ── Aft (octaweb + TPS skirt) ──
        SmoothCyl("Octaweb", visual.transform, 0.82f, Radius * 2.40f, 0.88f, black);
        SmoothCyl("OctawebLip", visual.transform, 0.26f, Radius * 2.48f, 0.035f, titanium);
        SmoothCyl("AftSkirt", visual.transform, 2.65f, Radius * 2.14f, 0.78f, carbon);
        SmoothCyl("AftSkirtRim", visual.transform, 3.48f, Radius * 2.16f, 0.04f, darkMetal);
        SmoothCyl("AftJoin", visual.transform, 3.62f, Radius * 2.05f, 0.07f, silver);

        // ── Body stack ──
        SmoothCyl("LowerTank", visual.transform, 8.40f, Radius * 2.0f, 4.70f, whiteLower);
        SmoothCyl("CommonDome", visual.transform, 13.25f, Radius * 2.05f, 0.14f, silver);
        SmoothCyl("Stripe1", visual.transform, 13.50f, Radius * 2.08f, 0.09f, stripe);
        SmoothCyl("MidTank", visual.transform, 21.20f, Radius * 2.0f, 7.60f, white);
        SmoothCyl("Stripe2", visual.transform, 28.95f, Radius * 2.08f, 0.12f, stripe);
        SmoothCyl("UpperTank", visual.transform, 32.80f, Radius * 2.0f, 3.70f, white);

        // Thin bright weld rings — catch light on the white stack
        float[] ringYs = { 5.9f, 10.2f, 17.2f, 24.2f, 31.2f, 35.6f };
        for (int i = 0; i < ringYs.Length; i++)
            SmoothCyl($"Ring_{i}", visual.transform, ringYs[i], Radius * 2.035f, 0.010f, silver);

        // Soft residual soot only at very bottom of white stack
        SmoothCyl("SootBand", visual.transform, 4.95f, Radius * 2.015f, 0.55f,
            VisualMaterials.Lit(new Color(0.18f, 0.175f, 0.17f), 0.42f, 0.28f));

        // ── Head: black CFRP interstage + closed booster nose ──
        float top = 36.50f;

        SmoothCyl("UpperCrown", visual.transform, top + 0.05f, Radius * 2.02f, 0.05f, silver);
        top += 0.10f;

        float interH = 1.55f;
        SmoothCyl("Interstage", visual.transform, top + interH * 0.5f, Radius * 2.0f, interH * 0.5f, interstageMat);
        // Thin bright lip at base of interstage
        SmoothCyl("InterLip", visual.transform, top + 0.04f, Radius * 2.04f, 0.04f, titanium);
        float interstageMidY = top + interH * 0.5f;
        top += interH;

        SmoothCyl("SepRing", visual.transform, top + 0.025f, Radius * 2.05f, 0.025f, silver);
        top += 0.05f;
        SmoothCyl("Bulkhead", visual.transform, top + 0.10f, Radius * 2.0f, 0.10f, darkMetal);
        top += 0.20f;
        SmoothMesh.MakeFrustum("NoseShoulder", visual.transform,
            new Vector3(0f, top + 0.30f, 0f),
            Radius * 2.0f, 0.30f, topRatio: 0.52f, titanium);
        top += 0.60f;
        SmoothMesh.MakeOgive("NoseTip", visual.transform,
            new Vector3(0f, top + 0.40f, 0f),
            Radius * 1.05f, 0.40f, metal, tipBlunt: 0.18f);

        // Raceway + thin conduit accent
        SmoothCylAt("Raceway", visual.transform,
            new Vector3(Radius + 0.11f, 20.5f, 0f), 0.18f, 14.2f, carbon);
        SmoothCylAt("RacewayEdge", visual.transform,
            new Vector3(Radius + 0.20f, 20.5f, 0f), 0.05f, 14.0f, darkMetal);

        // Brand panel: black plate + white field + blue accent bar
        Prim(PrimitiveType.Cube, "LogoBack", visual.transform,
            new Vector3(0f, 24.2f, Radius + 0.025f), new Vector3(1.85f, 2.70f, 0.022f), black);
        Prim(PrimitiveType.Cube, "LogoField", visual.transform,
            new Vector3(0f, 24.25f, Radius + 0.040f), new Vector3(1.45f, 1.85f, 0.018f), whitePaint);
        Prim(PrimitiveType.Cube, "LogoBar", visual.transform,
            new Vector3(0f, 23.25f, Radius + 0.048f), new Vector3(1.20f, 0.12f, 0.016f), accent);

        yield return null;
        BuildGridFins(visual.transform, titanium, silver, darkMetal, carbon, interstageMidY);
        yield return null;
        BuildLegs(visual.transform, black, metal, titanium, carbon, darkMetal, hydra);
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

    // ─────────────────────────────────────────────────────────────
    // Procedural cylindrical skins (U=angle, V=height) — no warping
    // ─────────────────────────────────────────────────────────────

    static Material MakeTankSkin(string name, float sootAmount, float panelContrast, int seed)
    {
        const int tw = 1024;
        const int th = 2048;
        var tex = new Texture2D(tw, th, TextureFormat.RGB24, true, false);
        tex.name = name + "_Albedo";
        tex.wrapModeU = TextureWrapMode.Repeat;
        tex.wrapModeV = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 8;

        var nrm = new Texture2D(tw, th, TextureFormat.RGBA32, true, true);
        nrm.name = name + "_Nrm";
        nrm.wrapModeU = TextureWrapMode.Repeat;
        nrm.wrapModeV = TextureWrapMode.Clamp;
        nrm.filterMode = FilterMode.Trilinear;
        nrm.anisoLevel = 8;

        var cols = new Color[tw * th];
        var nrmCols = new Color[tw * th];
        var rng = new System.Random(seed);
        // Pre-hash offsets
        float ox = (float)rng.NextDouble() * 40f;
        float oy = (float)rng.NextDouble() * 40f;

        // Panel grid in UV space
        int nVert = 12;   // vertical stringer count around
        int nHoriz = 28;  // horizontal bay count along height

        for (int y = 0; y < th; y++)
        {
            float v = y / (float)(th - 1);
            for (int x = 0; x < tw; x++)
            {
                float u = x / (float)tw;
                int idx = y * tw + x;

                // Clean presentation white — minimal noise, crisp panels
                float g = 0.965f;
                g += HashNoise(u * 36f + ox, v * 72f + oy) * 0.006f;
                g += HashNoise(u * 90f - ox, v * 140f + oy) * 0.003f;

                // Horizontal bay seams
                float hCell = v * nHoriz;
                float hEdge = Mathf.Abs(hCell - Mathf.Round(hCell));
                float hSeam = 1f - Mathf.SmoothStep(0f, 0.038f, hEdge);
                g -= hSeam * panelContrast * 0.70f;

                // Vertical stringers (narrow)
                float vCell = u * nVert;
                float vEdge = Mathf.Abs(vCell - Mathf.Round(vCell));
                float vSeam = 1f - Mathf.SmoothStep(0f, 0.014f, vEdge);
                g -= vSeam * panelContrast * 0.42f;

                // Sparse rivets — only on seams, tiny
                if (hSeam > 0.45f || vSeam > 0.45f)
                {
                    float rivU = u * nVert * 6f;
                    float rivV = v * nHoriz * 3f;
                    float rd = Mathf.Min(
                        Mathf.Abs(rivU - Mathf.Round(rivU)),
                        Mathf.Abs(rivV - Mathf.Round(rivV)));
                    if (rd < 0.06f)
                        g -= (1f - rd / 0.06f) * 0.025f;
                }

                // Soft soot wash (lower tanks only) — cool charcoal, not brown
                if (sootAmount > 0.01f)
                {
                    float sootV = Mathf.Clamp01(1f - v * 1.55f);
                    sootV = sootV * sootV * sootV;
                    float blot = 0.60f + 0.40f * HashNoise(u * 5f + 3f, v * 8f - 2f);
                    float side = 0.70f + 0.30f * Mathf.Sin(u * Mathf.PI * 2f + 0.7f);
                    g -= sootAmount * sootV * blot * side * 0.48f;
                }

                // Very light micro grain
                g += HashNoise(u * 280f, v * 520f) * 0.004f;
                g = Mathf.Clamp(g, 0.55f, 0.995f);

                // Cool pure white (slight blue, premium paint)
                float rC = Mathf.Clamp01(g * 0.992f);
                float gC = Mathf.Clamp01(g * 0.998f);
                float bC = Mathf.Clamp01(g * 1.012f);
                cols[idx] = new Color(rC, gC, bC, 1f);

                // Soft panel normals
                float du = (SampleGray(u + 1f / tw, v, nVert, nHoriz, sootAmount, panelContrast, ox, oy)
                          - SampleGray(u - 1f / tw, v, nVert, nHoriz, sootAmount, panelContrast, ox, oy)) * 4.2f;
                float dv = (SampleGray(u, v + 1f / th, nVert, nHoriz, sootAmount, panelContrast, ox, oy)
                          - SampleGray(u, v - 1f / th, nVert, nHoriz, sootAmount, panelContrast, ox, oy)) * 4.2f;
                Vector3 tn = new Vector3(-du, -dv, 1f).normalized;
                nrmCols[idx] = new Color(tn.x * 0.5f + 0.5f, tn.y * 0.5f + 0.5f, tn.z * 0.5f + 0.5f, 1f);
            }
        }

        tex.SetPixels(cols);
        tex.Apply(true, true);
        nrm.SetPixels(nrmCols);
        nrm.Apply(true, true);

        var mat = new Material(VisualMaterials.LitShader);
        mat.name = name;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.04f);
        // Glossy aerospace paint on clean tanks; matte where sooted
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", sootAmount > 0.15f ? 0.48f : 0.82f);
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.EnableKeyword("_BASEMAP");
        }
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        mat.mainTexture = tex;
        if (mat.HasProperty("_BumpMap"))
        {
            mat.SetTexture("_BumpMap", nrm);
            mat.EnableKeyword("_NORMALMAP");
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 0.38f);
        }
        return mat;
    }

    static float SampleGray(float u, float v, int nVert, int nHoriz, float sootAmount, float panelContrast, float ox, float oy)
    {
        u = u - Mathf.Floor(u);
        v = Mathf.Clamp01(v);
        float g = 0.965f;
        g += HashNoise(u * 36f + ox, v * 72f + oy) * 0.006f;
        float hCell = v * nHoriz;
        float hEdge = Mathf.Abs(hCell - Mathf.Round(hCell));
        g -= (1f - Mathf.SmoothStep(0f, 0.038f, hEdge)) * panelContrast * 0.70f;
        float vCell = u * nVert;
        float vEdge = Mathf.Abs(vCell - Mathf.Round(vCell));
        g -= (1f - Mathf.SmoothStep(0f, 0.014f, vEdge)) * panelContrast * 0.42f;
        if (sootAmount > 0.01f)
        {
            float sootV = Mathf.Clamp01(1f - v * 1.55f);
            sootV = sootV * sootV * sootV;
            g -= sootAmount * sootV * 0.32f;
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
                // Deep CFRP weave with subtle sheen bands
                float g = 0.055f;
                float weaveU = Mathf.Abs((u * 40f) - Mathf.Round(u * 40f));
                float weaveV = Mathf.Abs((v * 22f) - Mathf.Round(v * 22f));
                g += (1f - Mathf.SmoothStep(0f, 0.12f, weaveU)) * 0.028f;
                g += (1f - Mathf.SmoothStep(0f, 0.12f, weaveV)) * 0.022f;
                g += HashNoise(u * 48f + ox, v * 48f) * 0.012f;
                float band = Mathf.Abs((v * 5f) - Mathf.Round(v * 5f));
                g += (1f - Mathf.SmoothStep(0f, 0.07f, band)) * 0.045f;
                // Slight vertical gloss gradient
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
        const int tw = 1024;
        const int th = 1024;
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
                // Clean fairing: subtle micro-noise + 2 longitudinal half-seams only
                float g = 0.94f;
                g += HashNoise(u * 28f + ox, v * 36f) * 0.008f;
                float petal = u * 2f; // two fairing halves
                float seam = Mathf.Abs(petal - Mathf.Round(petal));
                float seamW = 1f - Mathf.SmoothStep(0f, 0.012f, seam);
                g -= seamW * 0.06f;
                // one access ring near base only
                float h = Mathf.Abs(v - 0.12f);
                g -= (1f - Mathf.SmoothStep(0f, 0.025f, h)) * 0.03f;
                // tip slightly brighter (smooth paint)
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
                // Regen-cooled niobium bell: dark exit → bronze mid → steel throat
                float mid = Mathf.Sin(v * Mathf.PI);
                float body = Mathf.Lerp(0.12f, 0.28f, mid);
                float ring = Mathf.Abs((v * 26f) - Mathf.Round(v * 26f));
                body += (1f - Mathf.SmoothStep(0f, 0.10f, ring)) * 0.08f;
                float ch = Mathf.Abs((u * 56f) - Mathf.Round(u * 56f));
                body += (1f - Mathf.SmoothStep(0f, 0.07f, ch)) * 0.04f;
                body += HashNoise(u * 18f + ox, v * 28f) * 0.025f;
                body = Mathf.Clamp01(body);

                float rC = body * (0.48f + 0.42f * mid);
                float gC = body * (0.34f + 0.22f * mid);
                float bC = body * (0.28f + 0.10f * mid);
                // Sooted exit lip
                if (v < 0.12f)
                {
                    float t = 1f - v / 0.12f;
                    rC = Mathf.Lerp(rC, 0.08f, t * 0.7f);
                    gC = Mathf.Lerp(gC, 0.08f, t * 0.7f);
                    bC = Mathf.Lerp(bC, 0.09f, t * 0.7f);
                }
                // Bright metallic throat
                if (v > 0.80f)
                {
                    float t = (v - 0.80f) / 0.20f;
                    rC = Mathf.Lerp(rC, 0.52f, t);
                    gC = Mathf.Lerp(gC, 0.50f, t);
                    bC = Mathf.Lerp(bC, 0.48f, t);
                }
                cols[y * tw + x] = new Color(rC, gC, bC, 1f);
            }
        }
        tex.SetPixels(cols);
        tex.Apply(true, true);

        var mat = new Material(VisualMaterials.LitShader);
        mat.name = name;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.88f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.34f);
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

    // ─────────────────────────────────────────────────────────────
    // Subassemblies
    // ─────────────────────────────────────────────────────────────

    static void BuildGridFins(Transform visual, Material frame, Material lattice, Material hub, Material carbon,
        float mountY = 37.2f)
    {
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            float r = Radius + 1.42f;
            var fin = new GameObject($"GridFin_{i}");
            fin.transform.SetParent(visual, false);
            fin.transform.localPosition = new Vector3(Mathf.Sin(a) * r, mountY, Mathf.Cos(a) * r);
            fin.transform.localRotation = Quaternion.Euler(0f, i * 90f, 2f);

            // Titanium frame + denser titanium lattice (readable from camera)
            Prim(PrimitiveType.Cube, "Plate", fin.transform, Vector3.zero,
                new Vector3(0.035f, 2.40f, 3.20f), carbon);
            Prim(PrimitiveType.Cube, "RimT", fin.transform, new Vector3(0.04f, 1.18f, 0f),
                new Vector3(0.09f, 0.055f, 3.15f), lattice);
            Prim(PrimitiveType.Cube, "RimB", fin.transform, new Vector3(0.04f, -1.18f, 0f),
                new Vector3(0.09f, 0.055f, 3.15f), frame);
            Prim(PrimitiveType.Cube, "RimL", fin.transform, new Vector3(0.04f, 0f, 1.55f),
                new Vector3(0.09f, 2.28f, 0.055f), frame);
            Prim(PrimitiveType.Cube, "RimR", fin.transform, new Vector3(0.04f, 0f, -1.55f),
                new Vector3(0.09f, 2.28f, 0.055f), frame);

            for (int g = 0; g < 5; g++)
                Prim(PrimitiveType.Cube, $"H_{g}", fin.transform,
                    new Vector3(0.055f, -0.90f + g * 0.45f, 0f),
                    new Vector3(0.014f, 0.018f, 3.00f), lattice);
            for (int g = 0; g < 6; g++)
                Prim(PrimitiveType.Cube, $"V_{g}", fin.transform,
                    new Vector3(0.055f, 0f, -1.30f + g * 0.52f),
                    new Vector3(0.014f, 2.20f, 0.018f), lattice);

            SmoothSphere("Hub", fin.transform, new Vector3(-0.28f, 0f, 0f), Vector3.one * 0.44f, hub);
            SmoothCylAt("Actuator", fin.transform, new Vector3(-0.52f, 0f, 0f), 0.15f, 0.24f, carbon);
            SmoothCylAt("Mount", fin.transform, new Vector3(-0.72f, 0f, 0f), 0.22f, 0.10f, frame);
        }
    }

    static void BuildLegs(Transform visual, Material black, Material metal, Material titanium,
        Material carbon, Material darkMetal, Material hydra)
    {
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            var legRoot = new GameObject($"LegAsm_{i}");
            legRoot.transform.SetParent(visual, false);

            Vector3 hinge = new Vector3(
                Mathf.Sin(a) * (Radius + 0.30f), 9.20f, Mathf.Cos(a) * (Radius + 0.30f));
            Vector3 foot = new Vector3(
                Mathf.Sin(a) * (Radius + 6.2f), 0.08f, Mathf.Cos(a) * (Radius + 6.2f));

            // Hinge fairing + primary boom
            SmoothSphere("Hinge", legRoot.transform, hinge, Vector3.one * 0.52f, titanium);
            SmoothCylAt("HingeCap", legRoot.transform, hinge + Vector3.up * 0.15f, 0.55f, 0.08f, darkMetal);
            Strut(legRoot.transform, "Boom", hinge, foot, 0.34f, black);
            // Light metal edge strip along boom (reads structure)
            Strut(legRoot.transform, "BoomEdge",
                hinge + Vector3.up * 0.12f,
                foot + Vector3.up * 0.12f, 0.10f, titanium);

            Vector3 bodyAnchor = new Vector3(
                Mathf.Sin(a) * (Radius + 0.08f), 5.85f, Mathf.Cos(a) * (Radius + 0.08f));
            Vector3 boomMid = Vector3.Lerp(hinge, foot, 0.42f);
            Strut(legRoot.transform, "Hydraulics", bodyAnchor, boomMid, 0.12f, hydra);
            SmoothSphere("HydJoint", legRoot.transform, bodyAnchor, Vector3.one * 0.24f, metal);
            SmoothSphere("HydKnee", legRoot.transform, boomMid, Vector3.one * 0.18f, titanium);

            // Landing foot stack
            SmoothCylAt("Crush", legRoot.transform, foot + Vector3.up * 0.38f, 0.72f, 0.26f, carbon);
            SmoothCylAt("Foot", legRoot.transform, foot + Vector3.up * 0.12f, 1.85f, 0.07f, metal);
            SmoothCylAt("FootPad", legRoot.transform, foot, 2.25f, 0.032f, black);
            SmoothCylAt("FootRing", legRoot.transform, foot + Vector3.up * 0.05f, 2.05f, 0.02f, titanium);
        }
    }

    static void Strut(Transform parent, string name, Vector3 from, Vector3 to, float thickness, Material mat)
    {
        Vector3 delta = to - from;
        float len = delta.magnitude;
        if (len < 1e-4f) return;

        var go = SmoothMesh.MakeCylinder(name, parent, (from + to) * 0.5f, thickness, len * 0.5f, mat);
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
        Nozzle(visual, Vector3.zero, heat, metal, copper, titanium, darkMetal, 1.20f, true);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Nozzle(visual,
                new Vector3(Mathf.Sin(a) * 1.38f, 0f, Mathf.Cos(a) * 1.38f),
                heat, metal, copper, titanium, darkMetal, 0.68f, false);
        }
        SmoothCyl("OctawebRing", visual.transform, 1.70f, Radius * 2.32f, 0.05f, metal);
    }

    static void Nozzle(Transform parent, Vector3 xz, Material heat, Material metal, Material copper,
        Material titanium, Material darkMetal, float s, bool center)
    {
        SmoothMesh.MakeBell("Bell", parent,
            new Vector3(xz.x, 0.52f * s, xz.z),
            1.34f * s, 0.78f * s, heat);
        SmoothCylAt("Exit", parent,
            new Vector3(xz.x, 0.02f * s, xz.z), 1.40f * s, 0.04f * s, metal);
        SmoothCylAt("ExitInner", parent,
            new Vector3(xz.x, 0.06f * s, xz.z), 1.22f * s, 0.02f * s, darkMetal);
        SmoothCylAt("Throat", parent,
            new Vector3(xz.x, 1.42f * s, xz.z), 0.36f * s, 0.11f * s, copper);
        SmoothSphere("Gimbal", parent,
            new Vector3(xz.x, 1.62f * s, xz.z), Vector3.one * ((center ? 0.38f : 0.28f) * s), metal);
        if (center)
        {
            SmoothCylAt("Turbopump", parent,
                new Vector3(xz.x, 1.86f * s, xz.z), 0.64f * s, 0.15f * s, titanium);
            SmoothCylAt("PumpLip", parent,
                new Vector3(xz.x, 2.05f * s, xz.z), 0.70f * s, 0.04f * s, metal);
        }
    }

    static void BuildEngineFX(Transform visual)
    {
        var flameGo = new GameObject("EngineFlame");
        flameGo.transform.SetParent(visual, false);
        flameGo.transform.localPosition = new Vector3(0f, -1.2f, 0f);
        flameGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var flame = flameGo.AddComponent<ParticleSystem>();
        ConfigureFlameOuter(flame);

        var coreGo = new GameObject("EngineFlameCore");
        coreGo.transform.SetParent(visual, false);
        coreGo.transform.localPosition = new Vector3(0f, -0.95f, 0f);
        coreGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var core = coreGo.AddComponent<ParticleSystem>();
        ConfigureFlameCore(core);

        var smokeGo = new GameObject("EngineSmoke");
        smokeGo.transform.SetParent(visual, false);
        smokeGo.transform.localPosition = new Vector3(0f, -5.5f, 0f);
        smokeGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var smoke = smokeGo.AddComponent<ParticleSystem>();
        ConfigureSmoke(smoke);

        var sparkGo = new GameObject("EngineSparks");
        sparkGo.transform.SetParent(visual, false);
        sparkGo.transform.localPosition = new Vector3(0f, -1.0f, 0f);
        sparkGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var sparks = sparkGo.AddComponent<ParticleSystem>();
        ConfigureSparks(sparks);

        var dustGo = new GameObject("EngineDust");
        dustGo.transform.SetParent(visual, false);
        dustGo.transform.localPosition = new Vector3(0f, -8f, 0f);
        dustGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var dust = dustGo.AddComponent<ParticleSystem>();
        ConfigureDust(dust);

        var lightGo = new GameObject("EngineLight");
        lightGo.transform.SetParent(visual, false);
        lightGo.transform.localPosition = new Vector3(0f, -2.5f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.65f, 0.85f, 1f);
        light.intensity = 0f;
        light.range = 150f;
        light.shadows = LightShadows.None;

        var fx = visual.gameObject.AddComponent<RocketEngineFX>();
        fx.flame = flame;
        fx.flameCore = core;
        fx.smoke = smoke;
        fx.sparks = sparks;
        fx.dust = dust;
        fx.engineLight = light;
    }

    static void ConfigureFlameOuter(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.32f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(55f, 120f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.4f, 4.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.92f, 0.75f, 0.95f),
            new Color(1f, 0.45f, 0.12f, 0.85f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1200;
        main.gravityModifier = 0f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 6.5f;
        shape.radius = 1.75f;
        shape.arc = 360f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.98f, 0.95f), 0f),
                new GradientColorKey(new Color(0.75f, 0.88f, 1f), 0.12f),
                new GradientColorKey(new Color(1f, 0.62f, 0.22f), 0.42f),
                new GradientColorKey(new Color(0.85f, 0.22f, 0.05f), 0.75f),
                new GradientColorKey(new Color(0.25f, 0.06f, 0.02f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.9f, 0.15f),
                new GradientAlphaKey(0.55f, 0.5f),
                new GradientAlphaKey(0.2f, 0.78f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.25f, 0.85f),
                new Keyframe(0.7f, 1.35f),
                new Keyframe(1f, 1.85f)));

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.z = new ParticleSystem.MinMaxCurve(0f, AnimationCurve.Linear(0f, 0f, 1f, 8f));

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 1.2f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.Particle(new Color(1f, 0.75f, 0.4f, 1f));
        rend.sortingFudge = -2f;
    }

    static void ConfigureFlameCore(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(80f, 160f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.6f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.95f, 1f, 1f),
            new Color(0.55f, 0.82f, 1f, 0.95f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 600;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 3.2f;
        shape.radius = 0.85f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 1f, 1f), 0f),
                new GradientColorKey(new Color(0.7f, 0.9f, 1f), 0.35f),
                new GradientColorKey(new Color(0.4f, 0.7f, 1f), 0.7f),
                new GradientColorKey(new Color(0.3f, 0.4f, 0.8f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.4f),
                new GradientAlphaKey(0.25f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.4f));

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.Particle(new Color(0.7f, 0.9f, 1f, 1f));
        rend.sortingFudge = -5f;
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
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f, 0.4f),
                new Keyframe(0.35f, 1.0f),
                new Keyframe(1f, 2.4f)));

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.85f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.4f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);

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
        shape.radius = 1.3f;

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
        rend.lengthScale = 3.2f;
        rend.velocityScale = 0.1f;
        rend.sharedMaterial = VisualMaterials.Particle(new Color(1f, 0.85f, 0.4f, 1f));
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

    static void SmoothCyl(string name, Transform parent, float y, float diameter, float halfHeight, Material mat)
        => SmoothCylAt(name, parent, new Vector3(0f, y, 0f), diameter, halfHeight, mat);

    static GameObject SmoothCylAt(string name, Transform parent, Vector3 pos, float diameter, float halfHeight, Material mat)
    {
        var go = SmoothMesh.MakeCylinder(name, parent, pos, diameter, halfHeight, mat);
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
