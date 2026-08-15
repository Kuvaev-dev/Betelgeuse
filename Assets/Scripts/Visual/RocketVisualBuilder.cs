using UnityEngine;

/// <summary>
/// Процедурна модель 1-го ступеня ~42 м (Falcon-class).
/// Циліндричні UV-скіни (без викривлення), smooth meshes, octaweb, multi-layer FX.
/// </summary>
public static class RocketVisualBuilder
{
    public const float Height = 42.4f;
    public const float Radius = 1.85f;

    public static void Build(RocketPhysics rocket)
    {
        if (rocket == null) return;
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

        // ── Materials ──
        var white = MakeTankSkin("TankWhite", sootAmount: 0.0f, panelContrast: 0.07f, seed: 11);
        var whiteLower = MakeTankSkin("TankLower", sootAmount: 0.38f, panelContrast: 0.08f, seed: 29);
        var whiteMatte = VisualMaterials.Lit(new Color(0.90f, 0.91f, 0.93f), 0.04f, 0.38f);
        var black = VisualMaterials.Lit(new Color(0.045f, 0.048f, 0.055f), 0.55f, 0.32f);
        var soot = VisualMaterials.Lit(new Color(0.10f, 0.095f, 0.09f), 0.35f, 0.18f);
        var metal = VisualMaterials.Lit(new Color(0.74f, 0.76f, 0.80f), 0.94f, 0.82f);
        var titanium = VisualMaterials.Lit(new Color(0.60f, 0.62f, 0.66f), 0.90f, 0.70f);
        var carbon = VisualMaterials.Lit(new Color(0.07f, 0.075f, 0.08f), 0.42f, 0.42f);
        var silver = VisualMaterials.Lit(new Color(0.88f, 0.90f, 0.93f), 0.93f, 0.78f);
        var heat = MakeNozzleSkin("NozzleHeat", seed: 7);
        var copper = VisualMaterials.Lit(new Color(0.58f, 0.42f, 0.30f), 0.92f, 0.55f);
        var darkMetal = VisualMaterials.Lit(new Color(0.16f, 0.17f, 0.19f), 0.88f, 0.48f);
        var stripe = VisualMaterials.Lit(new Color(0.055f, 0.055f, 0.06f), 0.40f, 0.28f);
        var gold = VisualMaterials.Lit(new Color(0.74f, 0.60f, 0.28f), 0.88f, 0.62f);
        var accent = VisualMaterials.Lit(new Color(0.16f, 0.70f, 0.90f), 0.25f, 0.65f,
            new Color(0.06f, 0.38f, 0.58f) * 0.4f);
        var tpz = VisualMaterials.Lit(new Color(0.09f, 0.09f, 0.10f), 0.25f, 0.22f); // thermal paint zone
        var hydra = VisualMaterials.Lit(new Color(0.82f, 0.84f, 0.88f), 0.85f, 0.55f);

        // ── Aft / octaweb ──
        SmoothCyl("Octaweb", visual.transform, 0.88f, Radius * 2.42f, 0.95f, black);
        SmoothCyl("OctawebLip", visual.transform, 0.32f, Radius * 2.50f, 0.045f, titanium);
        SmoothCyl("AftSkirt", visual.transform, 2.95f, Radius * 2.18f, 0.88f, carbon);
        SmoothCyl("AftSkirtFlare", visual.transform, 2.05f, Radius * 2.30f, 0.12f, darkMetal);

        for (int i = 0; i < 6; i++)
            SmoothCyl($"AftRing_{i}", visual.transform, 1.55f + i * 0.32f, Radius * 2.26f, 0.018f, titanium);

        // Octaweb radial ribs (smooth cylinders, not cubes)
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Vector3 p0 = new Vector3(Mathf.Sin(a) * (Radius * 0.35f), 0.95f, Mathf.Cos(a) * (Radius * 0.35f));
            Vector3 p1 = new Vector3(Mathf.Sin(a) * (Radius * 1.05f), 1.05f, Mathf.Cos(a) * (Radius * 1.05f));
            Strut(visual.transform, $"OctRib_{i}", p0, p1, 0.09f, darkMetal);
        }

        // Circumferential stringers around aft
        for (int i = 0; i < 24; i++)
        {
            float a = i * 15f * Mathf.Deg2Rad;
            float r = Radius + 0.05f;
            SmoothCylAt($"AftStringer_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * r, 3.05f, Mathf.Cos(a) * r),
                0.055f, 1.15f, darkMetal);
        }

        // ── Body stack (cylindrical UV skins — no stretch) ──
        SmoothCyl("LowerTank", visual.transform, 8.55f, Radius * 2.0f, 4.55f, whiteLower);
        SmoothCyl("CommonDome", visual.transform, 13.55f, Radius * 2.06f, 0.26f, silver);
        SmoothCyl("Stripe1", visual.transform, 14.05f, Radius * 2.09f, 0.075f, stripe);
        SmoothCyl("MidTank", visual.transform, 21.55f, Radius * 2.0f, 7.15f, white);
        SmoothCyl("Stripe2", visual.transform, 29.05f, Radius * 2.09f, 0.075f, stripe);
        // Upper tank ends cleanly under interstage (no floating white balls)
        SmoothCyl("UpperTank", visual.transform, 32.85f, Radius * 2.0f, 3.55f, white);

        // Weld / stiffener rings (thin, metallic)
        float[] ringYs =
        {
            4.6f, 6.4f, 8.2f, 10.0f, 11.9f, 14.55f, 16.4f, 18.6f, 20.8f,
            23.0f, 25.2f, 27.4f, 29.55f, 31.4f, 33.9f
        };
        for (int i = 0; i < ringYs.Length; i++)
            SmoothCyl($"Ring_{i}", visual.transform, ringYs[i], Radius * 2.035f, 0.014f, silver);

        // Soft heat / soot banding (cylindrical skins, not cube streaks)
        SmoothCyl("HeatBand1", visual.transform, 4.95f, Radius * 2.045f, 0.55f, soot);
        SmoothCyl("HeatBand2", visual.transform, 5.85f, Radius * 2.02f, 0.22f,
            VisualMaterials.Lit(new Color(0.14f, 0.12f, 0.11f), 0.42f, 0.20f));
        SmoothCyl("TpzBand", visual.transform, 7.15f, Radius * 2.01f, 0.35f, tpz);

        // ── Top stack: tank → interstage → frustum shoulder → tangent ogive ──
        // (replaces ugly stacked spheres)
        var interstageMat = MakeInterstageSkin("InterstageCFRP", seed: 41);
        var fairingMat = MakeFairingSkin("FairingSkin", seed: 17);

        // Tank→interstage joint
        SmoothCyl("UpperCrown", visual.transform, 36.55f, Radius * 2.02f, 0.12f, silver);
        SmoothCyl("InterstageLip", visual.transform, 36.78f, Radius * 2.05f, 0.06f, titanium);

        // Black CFRP interstage (grid fins mount here)
        SmoothCyl("Interstage", visual.transform, 37.85f, Radius * 1.98f, 1.05f, interstageMat);
        SmoothCyl("InterstageBand", visual.transform, 37.85f, Radius * 2.02f, 0.035f, darkMetal);
        SmoothCyl("InterstageTopRing", visual.transform, 38.95f, Radius * 2.04f, 0.05f, titanium);

        // Separation plane / push-rod belt
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            float r = Radius * 0.99f;
            SmoothCylAt($"SepBolt_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * r, 36.85f, Mathf.Cos(a) * r),
                0.11f, 0.07f, metal);
        }

        // Vent ports on interstage
        for (int i = 0; i < 8; i++)
        {
            float a = (i * 45f + 12f) * Mathf.Deg2Rad;
            float r = Radius * 0.99f;
            SmoothCylAt($"Vent_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * r, 37.55f, Mathf.Cos(a) * r),
                0.18f, 0.07f, darkMetal);
            SmoothCylAt($"VentLip_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * (r + 0.02f), 37.55f, Mathf.Cos(a) * (r + 0.02f)),
                0.22f, 0.02f, titanium);
        }

        // Shoulder frustum: interstage → fairing (smooth diameter step)
        SmoothMesh.MakeFrustum("FairingShoulder", visual.transform,
            new Vector3(0f, 39.40f, 0f),
            Radius * 2.00f, 0.38f, topRatio: 0.94f, fairingMat);

        SmoothCyl("FairingBaseRing", visual.transform, 39.82f, Radius * 1.90f, 0.035f, titanium);
        SmoothCyl("FairingSeam", visual.transform, 39.90f, Radius * 1.86f, 0.012f, darkMetal);

        // Longer elegant ogive (blunter tip, cleaner silhouette)
        SmoothMesh.MakeOgive("FairingOgive", visual.transform,
            new Vector3(0f, 40.95f, 0f),
            Radius * 1.88f, 1.05f, fairingMat, tipBlunt: 0.16f);

        // Apex stack
        BuildFairingTip(visual.transform, whiteMatte, metal, titanium, darkMetal, silver);

        // ── Markings / raceway (flat panels OK; solid mats — no warped maps) ──
        Prim(PrimitiveType.Cube, "Decal", visual.transform,
            new Vector3(0f, 24.5f, Radius + 0.035f), new Vector3(2.15f, 3.2f, 0.035f), black);
        Prim(PrimitiveType.Cube, "DecalLine", visual.transform,
            new Vector3(0f, 25.7f, Radius + 0.07f), new Vector3(1.65f, 0.055f, 0.025f), accent);
        Prim(PrimitiveType.Cube, "DecalLine2", visual.transform,
            new Vector3(0f, 23.4f, Radius + 0.07f), new Vector3(1.2f, 0.04f, 0.025f), silver);
        SmoothSphere("DecalDot", visual.transform,
            new Vector3(0f, 24.5f, Radius + 0.11f), Vector3.one * 0.24f, gold);

        // Raceway as rounded tube stack (less "boxy cable")
        SmoothCylAt("Raceway", visual.transform,
            new Vector3(Radius + 0.12f, 20.2f, 0f), 0.26f, 13.6f, carbon);
        SmoothCylAt("RacewayEdge", visual.transform,
            new Vector3(Radius + 0.20f, 20.2f, 0f), 0.07f, 13.55f, titanium);
        for (int i = 0; i < 9; i++)
        {
            SmoothCylAt($"RacewayClip_{i}", visual.transform,
                new Vector3(Radius + 0.17f, 6.2f + i * 3.55f, 0f),
                0.34f, 0.07f, metal);
        }

        // COPVs (composite overwrapped pressure vessels)
        for (int i = 0; i < 3; i++)
        {
            float a = (208f + i * 16f) * Mathf.Deg2Rad;
            float rr = Radius + 0.38f;
            SmoothCapsule($"COPV_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * rr, 31.0f + i * 0.1f, Mathf.Cos(a) * rr),
                new Vector3(0.44f, 0.72f, 0.44f), whiteMatte);
            SmoothCylAt($"COPVBand_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * rr, 31.0f + i * 0.1f, Mathf.Cos(a) * rr),
                0.48f, 0.04f, darkMetal);
        }

        // RCS thruster quads (on upper tank, below interstage)
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 18f) * Mathf.Deg2Rad;
            float r = Radius + 0.14f;
            Vector3 p = new Vector3(Mathf.Sin(a) * r, 35.55f, Mathf.Cos(a) * r);
            SmoothSphere($"RCS_{i}", visual.transform, p, new Vector3(0.36f, 0.40f, 0.36f), darkMetal);
            Vector3 n = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            SmoothCylAt($"RCSNoz_{i}", visual.transform, p + n * 0.22f, 0.10f, 0.08f, heat);
            SmoothCylAt($"RCSNozB_{i}", visual.transform, p + n * 0.18f + Vector3.up * 0.12f, 0.08f, 0.06f, heat);
        }

        // Nav LEDs
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            var led = VisualMaterials.Lit(
                i % 2 == 0 ? new Color(0.18f, 0.95f, 1f) : new Color(1f, 0.28f, 0.14f),
                0.08f, 0.9f,
                (i % 2 == 0 ? new Color(0.18f, 0.95f, 1f) : new Color(1f, 0.28f, 0.14f)) * 0.9f);
            SmoothSphere($"Nav_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * (Radius + 0.04f), 29.55f, Mathf.Cos(a) * (Radius + 0.04f)),
                Vector3.one * 0.12f, led);
        }

        // Cable conduits (thin, opposite raceway)
        for (int i = 0; i < 2; i++)
        {
            float a = (160f + i * 25f) * Mathf.Deg2Rad;
            float r = Radius + 0.08f;
            SmoothCylAt($"Conduit_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * r, 18f, Mathf.Cos(a) * r),
                0.09f, 10f, darkMetal);
        }

        BuildGridFins(visual.transform, titanium, silver, darkMetal, carbon);
        BuildLegs(visual.transform, black, metal, titanium, carbon, darkMetal, hydra);
        BuildNozzles(visual.transform, heat, metal, copper, titanium, darkMetal);
        BuildEngineFX(visual.transform);

        // Soft body fill (not hot spots)
        AddPointLight(visual.transform, "BodyKey", new Vector3(9f, 22f, -8f),
            new Color(0.96f, 0.97f, 1f), 4.2f, 55f);
        AddPointLight(visual.transform, "BodyFill", new Vector3(-8f, 24f, 5f),
            new Color(0.65f, 0.70f, 0.82f), 2.4f, 45f);

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

                // Base cool white / light gray
                float g = 0.94f;
                g += HashNoise(u * 48f + ox, v * 96f + oy) * 0.012f;
                g += HashNoise(u * 120f - ox, v * 200f + oy) * 0.006f;

                // Horizontal panel seams (thin, sharp, cylindrical-correct)
                float hCell = v * nHoriz;
                float hEdge = Mathf.Abs(hCell - Mathf.Round(hCell));
                float hSeam = 1f - Mathf.SmoothStep(0f, 0.045f, hEdge);
                g -= hSeam * panelContrast * 0.85f;

                // Vertical stringers
                float vCell = u * nVert;
                float vEdge = Mathf.Abs(vCell - Mathf.Round(vCell));
                float vSeam = 1f - Mathf.SmoothStep(0f, 0.018f, vEdge);
                g -= vSeam * panelContrast * 0.55f;

                // Rivet dots along seams
                if (hSeam > 0.35f || vSeam > 0.35f)
                {
                    float rivU = u * nVert * 8f;
                    float rivV = v * nHoriz * 4f;
                    float rd = Mathf.Min(
                        Mathf.Abs(rivU - Mathf.Round(rivU)),
                        Mathf.Abs(rivV - Mathf.Round(rivV)));
                    if (rd < 0.08f)
                        g -= (1f - rd / 0.08f) * 0.04f;
                }

                // Soft soot / heat wash from bottom (only lower tanks)
                if (sootAmount > 0.01f)
                {
                    float sootV = Mathf.Clamp01(1f - v * 1.35f);
                    sootV = sootV * sootV;
                    float blot = 0.55f + 0.45f * HashNoise(u * 6f + 3f, v * 10f - 2f);
                    // asymmetric circumferential wash
                    float side = 0.65f + 0.35f * Mathf.Sin(u * Mathf.PI * 2f * 1.0f + 0.7f);
                    g -= sootAmount * sootV * blot * side * 0.55f;
                    // slight warm-to-cool keep neutral-dark (avoid brown body)
                    // (g only — applied as gray)
                }

                // Micro orange-peel
                g += HashNoise(u * 400f, v * 800f) * 0.008f;
                g = Mathf.Clamp01(g);

                // Cool white (slight blue, not cream)
                float rC = Mathf.Clamp01(g * 0.995f);
                float gC = Mathf.Clamp01(g);
                float bC = Mathf.Clamp01(g * 1.01f);
                cols[idx] = new Color(rC, gC, bC, 1f);

                // Tangent-space normal from panel grooves (U→tangent, V→bitangent)
                float du = (SampleGray(u + 1f / tw, v, nVert, nHoriz, sootAmount, panelContrast, ox, oy)
                          - SampleGray(u - 1f / tw, v, nVert, nHoriz, sootAmount, panelContrast, ox, oy)) * 6f;
                float dv = (SampleGray(u, v + 1f / th, nVert, nHoriz, sootAmount, panelContrast, ox, oy)
                          - SampleGray(u, v - 1f / th, nVert, nHoriz, sootAmount, panelContrast, ox, oy)) * 6f;
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
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.06f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", sootAmount > 0.2f ? 0.42f : 0.72f);
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
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 0.55f);
        }
        return mat;
    }

    static float SampleGray(float u, float v, int nVert, int nHoriz, float sootAmount, float panelContrast, float ox, float oy)
    {
        u = u - Mathf.Floor(u);
        v = Mathf.Clamp01(v);
        float g = 0.94f;
        g += HashNoise(u * 48f + ox, v * 96f + oy) * 0.012f;
        float hCell = v * nHoriz;
        float hEdge = Mathf.Abs(hCell - Mathf.Round(hCell));
        g -= (1f - Mathf.SmoothStep(0f, 0.045f, hEdge)) * panelContrast * 0.85f;
        float vCell = u * nVert;
        float vEdge = Mathf.Abs(vCell - Mathf.Round(vCell));
        g -= (1f - Mathf.SmoothStep(0f, 0.018f, vEdge)) * panelContrast * 0.55f;
        if (sootAmount > 0.01f)
        {
            float sootV = Mathf.Clamp01(1f - v * 1.35f);
            sootV *= sootV;
            g -= sootAmount * sootV * 0.35f;
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
                // Carbon fiber weave (dark)
                float g = 0.07f;
                float weaveU = Mathf.Abs((u * 32f) - Mathf.Round(u * 32f));
                float weaveV = Mathf.Abs((v * 18f) - Mathf.Round(v * 18f));
                g += (1f - Mathf.SmoothStep(0f, 0.15f, weaveU)) * 0.025f;
                g += (1f - Mathf.SmoothStep(0f, 0.15f, weaveV)) * 0.02f;
                g += HashNoise(u * 40f + ox, v * 40f) * 0.015f;
                // Horizontal stiffener bands
                float band = Mathf.Abs((v * 6f) - Mathf.Round(v * 6f));
                g += (1f - Mathf.SmoothStep(0f, 0.08f, band)) * 0.04f;
                g = Mathf.Clamp01(g);
                cols[y * tw + x] = new Color(g * 0.95f, g * 0.97f, g * 1.05f, 1f);
            }
        }
        tex.SetPixels(cols);
        tex.Apply(true, true);

        var mat = new Material(VisualMaterials.LitShader);
        mat.name = name;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.35f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.38f);
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
                float g = 0.93f;
                g += HashNoise(u * 36f + ox, v * 48f) * 0.01f;
                // 4 long fairing petals (longitudinal seams)
                float petal = u * 4f;
                float seam = Mathf.Abs(petal - Mathf.Round(petal));
                float seamW = 1f - Mathf.SmoothStep(0f, 0.02f, seam);
                g -= seamW * 0.10f;
                // sparse horizontal access rings
                float h = Mathf.Abs((v * 5f) - Mathf.Round(v * 5f));
                g -= (1f - Mathf.SmoothStep(0f, 0.035f, h)) * 0.04f;
                // soft tip darkening
                g *= Mathf.Lerp(1f, 0.88f, v * v * 0.5f);
                g = Mathf.Clamp01(g);
                cols[y * tw + x] = new Color(g * 0.995f, g, g * 1.01f, 1f);

                float du = seamW * 0.8f * Mathf.Sign(petal - Mathf.Round(petal) + 1e-4f);
                float dv = (1f - Mathf.SmoothStep(0f, 0.035f, h)) * 0.5f;
                Vector3 tn = new Vector3(-du, -dv, 1f).normalized;
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
                // Niobium / carbon-carbon heat gradient: dark throat → glowing mid → dark exit soot
                float body = Mathf.Lerp(0.10f, 0.22f, Mathf.Sin(v * Mathf.PI));
                // Cooling tube rings (circumferential) — crisp on cylindrical UV
                float ring = Mathf.Abs((v * 22f) - Mathf.Round(v * 22f));
                float ringLine = 1f - Mathf.SmoothStep(0f, 0.12f, ring);
                body += ringLine * 0.07f;
                // Longitudinal regen channels
                float ch = Mathf.Abs((u * 48f) - Mathf.Round(u * 48f));
                body += (1f - Mathf.SmoothStep(0f, 0.08f, ch)) * 0.035f;
                // Heat iridescence-ish noise (subtle)
                body += HashNoise(u * 20f + ox, v * 30f) * 0.03f;
                body = Mathf.Clamp01(body);

                // Slight copper/bronze at mid-bell, charcoal at ends
                float mid = Mathf.Sin(v * Mathf.PI);
                float rC = body * (0.55f + 0.35f * mid);
                float gC = body * (0.38f + 0.15f * mid);
                float bC = body * (0.30f + 0.05f * mid);
                // throat brighter metallic
                if (v > 0.82f)
                {
                    float t = (v - 0.82f) / 0.18f;
                    rC = Mathf.Lerp(rC, 0.45f, t);
                    gC = Mathf.Lerp(gC, 0.42f, t);
                    bC = Mathf.Lerp(bC, 0.40f, t);
                }
                cols[y * tw + x] = new Color(rC, gC, bC, 1f);
            }
        }
        tex.SetPixels(cols);
        tex.Apply(true, true);

        var mat = new Material(VisualMaterials.LitShader);
        mat.name = name;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.82f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.28f);
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

    /// <summary>
    /// Верхівка: seamless ogive close-out — titanium band + soft silver dome (no spike/mast).
    /// </summary>
    static void BuildFairingTip(Transform visual, Material whiteMatte, Material metal,
        Material titanium, Material darkMetal, Material silver)
    {
        // Close-out ring where ogive ends
        SmoothCyl("TipBand", visual.transform, 41.95f, 0.72f, 0.04f, titanium);
        SmoothCyl("TipBandInner", visual.transform, 42.00f, 0.55f, 0.025f, darkMetal);

        // Soft metal dome (primary apex shape)
        SmoothSphere("TipDome", visual.transform,
            new Vector3(0f, 42.18f, 0f),
            new Vector3(0.52f, 0.38f, 0.52f), silver);

        // White thermal nose plug
        SmoothSphere("TipPlug", visual.transform,
            new Vector3(0f, 42.36f, 0f),
            new Vector3(0.26f, 0.18f, 0.26f), whiteMatte);

        // Micro apex highlight (tiny, flush)
        SmoothSphere("TipHighlight", visual.transform,
            new Vector3(0f, 42.44f, 0f),
            new Vector3(0.10f, 0.07f, 0.10f), metal);
    }

    static void AddPointLight(Transform parent, string name, Vector3 pos, Color c, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
    }

    static void BuildGridFins(Transform visual, Material frame, Material lattice, Material hub, Material carbon)
    {
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            float r = Radius + 1.42f;
            var fin = new GameObject($"GridFin_{i}");
            fin.transform.SetParent(visual, false);
            // Mount on black interstage (classic F9 landing look)
            fin.transform.localPosition = new Vector3(Mathf.Sin(a) * r, 37.75f, Mathf.Cos(a) * r);
            fin.transform.localRotation = Quaternion.Euler(0f, i * 90f, 4f);

            // Main plate + outer frame
            Prim(PrimitiveType.Cube, "Plate", fin.transform, Vector3.zero,
                new Vector3(0.045f, 2.45f, 3.35f), frame);
            Prim(PrimitiveType.Cube, "FrameTop", fin.transform, new Vector3(0.03f, 1.18f, 0f),
                new Vector3(0.10f, 0.055f, 3.30f), titaniumLike());
            Prim(PrimitiveType.Cube, "FrameBot", fin.transform, new Vector3(0.03f, -1.18f, 0f),
                new Vector3(0.10f, 0.055f, 3.30f), frame);
            Prim(PrimitiveType.Cube, "FrameL", fin.transform, new Vector3(0.03f, 0f, 1.60f),
                new Vector3(0.10f, 2.30f, 0.055f), frame);
            Prim(PrimitiveType.Cube, "FrameR", fin.transform, new Vector3(0.03f, 0f, -1.60f),
                new Vector3(0.10f, 2.30f, 0.055f), frame);

            // Mid stiffeners
            Prim(PrimitiveType.Cube, "MidH", fin.transform, new Vector3(0.04f, 0f, 0f),
                new Vector3(0.04f, 0.04f, 3.1f), lattice);
            Prim(PrimitiveType.Cube, "MidV", fin.transform, new Vector3(0.04f, 0f, 0f),
                new Vector3(0.04f, 2.2f, 0.04f), lattice);

            SmoothSphere("Hub", fin.transform, new Vector3(-0.28f, 0f, 0f), Vector3.one * 0.52f, hub);
            SmoothCylAt("Actuator", fin.transform, new Vector3(-0.52f, 0f, 0f), 0.20f, 0.30f, carbon);
            SmoothCylAt("ActuatorPin", fin.transform, new Vector3(-0.70f, 0f, 0f), 0.10f, 0.12f, frame);

            // Denser lattice
            for (int g = 0; g < 8; g++)
                Prim(PrimitiveType.Cube, $"H_{g}", fin.transform,
                    new Vector3(0.055f, -1.05f + g * 0.30f, 0f),
                    new Vector3(0.018f, 0.022f, 3.05f), lattice);
            for (int g = 0; g < 9; g++)
                Prim(PrimitiveType.Cube, $"V_{g}", fin.transform,
                    new Vector3(0.055f, 0f, -1.40f + g * 0.35f),
                    new Vector3(0.018f, 2.25f, 0.022f), lattice);
        }
    }

    static Material titaniumLike() =>
        VisualMaterials.Lit(new Color(0.62f, 0.64f, 0.68f), 0.9f, 0.72f);

    static void BuildLegs(Transform visual, Material black, Material metal, Material titanium,
        Material carbon, Material darkMetal, Material hydra)
    {
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            var legRoot = new GameObject($"LegAsm_{i}");
            legRoot.transform.SetParent(visual, false);

            Vector3 hinge = new Vector3(
                Mathf.Sin(a) * (Radius + 0.30f),
                9.25f,
                Mathf.Cos(a) * (Radius + 0.30f));

            Vector3 foot = new Vector3(
                Mathf.Sin(a) * (Radius + 6.1f),
                0.10f,
                Mathf.Cos(a) * (Radius + 6.1f));

            // Hinge fairing
            SmoothCylAt("Hinge", legRoot.transform, hinge, 0.58f, 0.32f, titanium);
            SmoothSphere("HingeBall", legRoot.transform, hinge, Vector3.one * 0.55f, darkMetal);
            SmoothCylAt("HingeFairing", legRoot.transform,
                hinge + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 0.15f,
                0.72f, 0.18f, black);

            // Primary boom (black composite)
            Strut(legRoot.transform, "Boom", hinge, foot, 0.38f, black);

            // Secondary A-frame members
            Vector3 hingeL = hinge + new Vector3(Mathf.Sin(a + 0.28f), 0f, Mathf.Cos(a + 0.28f)) * 0.45f
                             + Vector3.down * 0.25f;
            Vector3 hingeR = hinge + new Vector3(Mathf.Sin(a - 0.28f), 0f, Mathf.Cos(a - 0.28f)) * 0.45f
                             + Vector3.down * 0.25f;
            Vector3 footJoin = Vector3.Lerp(hinge, foot, 0.88f) + Vector3.up * 0.15f;
            Strut(legRoot.transform, "AFrameL", hingeL, footJoin, 0.14f, carbon);
            Strut(legRoot.transform, "AFrameR", hingeR, footJoin, 0.14f, carbon);

            // Hydraulic actuators
            Vector3 bodyAnchor = new Vector3(
                Mathf.Sin(a) * (Radius + 0.10f),
                5.85f,
                Mathf.Cos(a) * (Radius + 0.10f));
            Vector3 boomMid = Vector3.Lerp(hinge, foot, 0.42f);
            Strut(legRoot.transform, "Hydraulics", bodyAnchor, boomMid, 0.15f, hydra);
            Strut(legRoot.transform, "Hydraulics2",
                bodyAnchor + Vector3.up * 1.15f,
                Vector3.Lerp(hinge, foot, 0.24f), 0.10f, titanium);

            // Actuator housings
            SmoothSphere("HydJoint", legRoot.transform, bodyAnchor, Vector3.one * 0.28f, metal);
            SmoothSphere("HydJoint2", legRoot.transform, boomMid, Vector3.one * 0.22f, metal);

            // Crush core + footpad
            SmoothCylAt("CrushCore", legRoot.transform, foot + Vector3.up * 0.36f, 0.82f, 0.28f, carbon);
            SmoothCylAt("Foot", legRoot.transform, foot + Vector3.up * 0.12f, 1.85f, 0.09f, metal);
            SmoothCylAt("FootPad", legRoot.transform, foot, 2.35f, 0.04f, black);
            SmoothCylAt("FootRing", legRoot.transform, foot + Vector3.up * 0.045f, 2.05f, 0.022f, titanium);
            // Foot ribs
            for (int k = 0; k < 4; k++)
            {
                float fa = (k * 45f + i * 10f) * Mathf.Deg2Rad;
                Vector3 fr = foot + new Vector3(Mathf.Sin(fa), 0f, Mathf.Cos(fa)) * 0.7f + Vector3.up * 0.08f;
                SmoothCylAt($"FootRib_{k}", legRoot.transform, fr, 0.12f, 0.05f, darkMetal);
            }
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
        Nozzle(visual, Vector3.zero, heat, metal, copper, titanium, darkMetal, 1.22f, true);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Nozzle(visual,
                new Vector3(Mathf.Sin(a) * 1.40f, 0f, Mathf.Cos(a) * 1.40f),
                heat, metal, copper, titanium, darkMetal, 0.70f, false);
        }

        // Octaweb structure rings
        SmoothCyl("OctawebRing", visual.transform, 1.85f, Radius * 2.38f, 0.06f, metal);
        SmoothCyl("OctawebRing2", visual.transform, 1.48f, Radius * 2.26f, 0.035f, titanium);
        SmoothCyl("OctawebRing3", visual.transform, 1.15f, Radius * 2.20f, 0.025f, darkMetal);

        // Engine bay cross braces
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 22.5f) * Mathf.Deg2Rad;
            Vector3 p0 = new Vector3(Mathf.Sin(a) * 0.4f, 1.55f, Mathf.Cos(a) * 0.4f);
            Vector3 p1 = new Vector3(Mathf.Sin(a) * 1.55f, 1.55f, Mathf.Cos(a) * 1.55f);
            Strut(visual.transform, $"BayBrace_{i}", p0, p1, 0.07f, darkMetal);
        }
    }

    static void Nozzle(Transform parent, Vector3 xz, Material heat, Material metal, Material copper,
        Material titanium, Material darkMetal, float s, bool center)
    {
        // Curved bell + cooling detail
        SmoothMesh.MakeBell("Bell", parent,
            new Vector3(xz.x, 0.52f * s, xz.z),
            1.32f * s, 0.78f * s, heat);

        // Exit ring / stiffener
        SmoothCylAt("Exit", parent,
            new Vector3(xz.x, 0.015f * s, xz.z), 1.40f * s, 0.055f * s, metal);
        SmoothCylAt("ExitInner", parent,
            new Vector3(xz.x, 0.06f * s, xz.z), 1.22f * s, 0.03f * s, darkMetal);

        // Cooling jacket rings along bell (geometry, reinforces texture)
        for (int k = 0; k < 5; k++)
        {
            float t = (k + 1) / 6f;
            float y = Mathf.Lerp(0.12f, 1.15f, t) * s;
            float d = Mathf.Lerp(1.28f, 0.55f, t * t) * s;
            SmoothCylAt($"CoolRing_{k}", parent,
                new Vector3(xz.x, y, xz.z), d, 0.012f * s, darkMetal);
        }

        SmoothCylAt("Throat", parent,
            new Vector3(xz.x, 1.40f * s, xz.z), 0.36f * s, 0.12f * s, copper);
        SmoothCylAt("Gimbal", parent,
            new Vector3(xz.x, 1.60f * s, xz.z), 0.52f * s, 0.065f * s, metal);

        if (center)
        {
            SmoothCylAt("Turbopump", parent,
                new Vector3(xz.x, 1.85f * s, xz.z), 0.70f * s, 0.17f * s, titanium);
            SmoothSphere("GimbalBall", parent,
                new Vector3(xz.x, 1.68f * s, xz.z), Vector3.one * (0.40f * s), metal);
            SmoothCylAt("GasGen", parent,
                new Vector3(xz.x + 0.35f * s, 1.95f * s, xz.z), 0.28f * s, 0.12f * s, darkMetal);
        }
        else
        {
            SmoothSphere("GimbalBall", parent,
                new Vector3(xz.x, 1.62f * s, xz.z), Vector3.one * (0.28f * s), metal);
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
