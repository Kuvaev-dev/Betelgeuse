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

        // ── Palette (restrained) ──
        var white = MakeTankSkin("TankWhite", sootAmount: 0.0f, panelContrast: 0.055f, seed: 11);
        var whiteLower = MakeTankSkin("TankLower", sootAmount: 0.32f, panelContrast: 0.06f, seed: 29);
        var black = VisualMaterials.Lit(new Color(0.045f, 0.048f, 0.055f), 0.55f, 0.32f);
        var metal = VisualMaterials.Lit(new Color(0.74f, 0.76f, 0.80f), 0.94f, 0.82f);
        var titanium = VisualMaterials.Lit(new Color(0.60f, 0.62f, 0.66f), 0.90f, 0.70f);
        var carbon = VisualMaterials.Lit(new Color(0.07f, 0.075f, 0.08f), 0.42f, 0.42f);
        var silver = VisualMaterials.Lit(new Color(0.88f, 0.90f, 0.93f), 0.93f, 0.78f);
        var heat = MakeNozzleSkin("NozzleHeat", seed: 7);
        var copper = VisualMaterials.Lit(new Color(0.58f, 0.42f, 0.30f), 0.92f, 0.55f);
        var darkMetal = VisualMaterials.Lit(new Color(0.16f, 0.17f, 0.19f), 0.88f, 0.48f);
        var stripe = VisualMaterials.Lit(new Color(0.055f, 0.055f, 0.06f), 0.40f, 0.28f);
        var hydra = VisualMaterials.Lit(new Color(0.82f, 0.84f, 0.88f), 0.85f, 0.55f);
        var interstageMat = MakeInterstageSkin("InterstageCFRP", seed: 41);

        // ── Aft (octaweb + short skirt — no stringers / ribs clutter) ──
        SmoothCyl("Octaweb", visual.transform, 0.85f, Radius * 2.38f, 0.90f, black);
        SmoothCyl("OctawebLip", visual.transform, 0.28f, Radius * 2.44f, 0.04f, titanium);
        SmoothCyl("AftSkirt", visual.transform, 2.70f, Radius * 2.12f, 0.72f, carbon);
        SmoothCyl("AftJoin", visual.transform, 3.55f, Radius * 2.04f, 0.08f, darkMetal);

        // ── Body stack (halfHeight = half of full height; pieces butt-join) ──
        // LowerTank top = 8.40+4.70 = 13.10
        SmoothCyl("LowerTank", visual.transform, 8.40f, Radius * 2.0f, 4.70f, whiteLower);
        SmoothCyl("CommonDome", visual.transform, 13.25f, Radius * 2.04f, 0.15f, silver); // 13.10..13.40
        SmoothCyl("Stripe1", visual.transform, 13.50f, Radius * 2.07f, 0.10f, stripe);   // 13.40..13.60
        // Mid 13.60..28.80 → center 21.20, half 7.60
        SmoothCyl("MidTank", visual.transform, 21.20f, Radius * 2.0f, 7.60f, white);
        SmoothCyl("Stripe2", visual.transform, 28.95f, Radius * 2.07f, 0.15f, stripe);   // 28.80..29.10
        // Upper 29.10..36.50 → center 32.80, half 3.70
        SmoothCyl("UpperTank", visual.transform, 32.80f, Radius * 2.0f, 3.70f, white);

        float[] ringYs = { 6.2f, 10.5f, 17.5f, 24.5f, 31.5f };
        for (int i = 0; i < ringYs.Length; i++)
            SmoothCyl($"Ring_{i}", visual.transform, ringYs[i], Radius * 2.03f, 0.012f, silver);

        SmoothCyl("SootBand", visual.transform, 5.15f, Radius * 2.02f, 0.70f,
            VisualMaterials.Lit(new Color(0.12f, 0.11f, 0.105f), 0.38f, 0.20f));

        // ── Head: Falcon-class 1st stage — black CFRP interstage + rounded metal cap ──
        // (no tall payload fairing — landing booster look)
        float top = 36.50f;

        float crownH = 0.10f;
        SmoothCyl("UpperCrown", visual.transform, top + crownH * 0.5f, Radius * 2.01f, crownH * 0.5f, silver);
        top += crownH;

        float interH = 1.55f;
        SmoothCyl("Interstage", visual.transform, top + interH * 0.5f, Radius * 2.0f, interH * 0.5f, interstageMat);
        float interstageMidY = top + interH * 0.5f;
        top += interH;

        // Closed nose: full-diameter plug + short blunt ogive (no cavity / hole)
        SmoothCyl("SepRing", visual.transform, top + 0.025f, Radius * 2.04f, 0.025f, titanium);
        top += 0.05f;
        // Solid bulkhead disc fills the tube completely
        SmoothCyl("Bulkhead", visual.transform, top + 0.10f, Radius * 2.0f, 0.10f, darkMetal);
        top += 0.20f;
        // Short frustum taper then blunt ogive tip — continuous solid silhouette
        SmoothMesh.MakeFrustum("NoseShoulder", visual.transform,
            new Vector3(0f, top + 0.28f, 0f),
            Radius * 2.0f, 0.28f, topRatio: 0.55f, titanium);
        top += 0.56f;
        SmoothMesh.MakeOgive("NoseTip", visual.transform,
            new Vector3(0f, top + 0.42f, 0f),
            Radius * 1.12f, 0.42f, metal, tipBlunt: 0.22f);

        // Single thin raceway (no clips / conduits / COPV clutter)
        SmoothCylAt("Raceway", visual.transform,
            new Vector3(Radius + 0.10f, 20.5f, 0f), 0.20f, 14.0f, carbon);

        // Quiet black logo panel (no gold dots / cyan lines)
        Prim(PrimitiveType.Cube, "LogoPanel", visual.transform,
            new Vector3(0f, 24.2f, Radius + 0.03f), new Vector3(1.6f, 2.4f, 0.028f), black);

        BuildGridFins(visual.transform, titanium, silver, darkMetal, carbon, interstageMidY);
        BuildLegs(visual.transform, black, metal, titanium, carbon, darkMetal, hydra);
        BuildNozzles(visual.transform, heat, metal, copper, titanium, darkMetal);
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

    static void BuildGridFins(Transform visual, Material frame, Material lattice, Material hub, Material carbon,
        float mountY = 37.2f)
    {
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            float r = Radius + 1.35f;
            var fin = new GameObject($"GridFin_{i}");
            fin.transform.SetParent(visual, false);
            fin.transform.localPosition = new Vector3(Mathf.Sin(a) * r, mountY, Mathf.Cos(a) * r);
            fin.transform.localRotation = Quaternion.Euler(0f, i * 90f, 3f);

            // Outer frame only + sparse grid (readable, not busy)
            Prim(PrimitiveType.Cube, "Plate", fin.transform, Vector3.zero,
                new Vector3(0.04f, 2.30f, 3.10f), frame);
            Prim(PrimitiveType.Cube, "RimT", fin.transform, new Vector3(0.03f, 1.12f, 0f),
                new Vector3(0.08f, 0.05f, 3.05f), lattice);
            Prim(PrimitiveType.Cube, "RimB", fin.transform, new Vector3(0.03f, -1.12f, 0f),
                new Vector3(0.08f, 0.05f, 3.05f), frame);
            Prim(PrimitiveType.Cube, "RimL", fin.transform, new Vector3(0.03f, 0f, 1.50f),
                new Vector3(0.08f, 2.15f, 0.05f), frame);
            Prim(PrimitiveType.Cube, "RimR", fin.transform, new Vector3(0.03f, 0f, -1.50f),
                new Vector3(0.08f, 2.15f, 0.05f), frame);

            for (int g = 0; g < 4; g++)
                Prim(PrimitiveType.Cube, $"H_{g}", fin.transform,
                    new Vector3(0.05f, -0.75f + g * 0.50f, 0f),
                    new Vector3(0.016f, 0.02f, 2.90f), lattice);
            for (int g = 0; g < 5; g++)
                Prim(PrimitiveType.Cube, $"V_{g}", fin.transform,
                    new Vector3(0.05f, 0f, -1.20f + g * 0.60f),
                    new Vector3(0.016f, 2.10f, 0.02f), lattice);

            SmoothSphere("Hub", fin.transform, new Vector3(-0.26f, 0f, 0f), Vector3.one * 0.42f, hub);
            SmoothCylAt("Actuator", fin.transform, new Vector3(-0.48f, 0f, 0f), 0.16f, 0.22f, carbon);
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
                Mathf.Sin(a) * (Radius + 0.28f), 9.10f, Mathf.Cos(a) * (Radius + 0.28f));
            Vector3 foot = new Vector3(
                Mathf.Sin(a) * (Radius + 6.0f), 0.10f, Mathf.Cos(a) * (Radius + 6.0f));

            SmoothSphere("Hinge", legRoot.transform, hinge, Vector3.one * 0.48f, titanium);
            Strut(legRoot.transform, "Boom", hinge, foot, 0.36f, black);

            Vector3 bodyAnchor = new Vector3(
                Mathf.Sin(a) * (Radius + 0.08f), 5.90f, Mathf.Cos(a) * (Radius + 0.08f));
            Vector3 boomMid = Vector3.Lerp(hinge, foot, 0.40f);
            Strut(legRoot.transform, "Hydraulics", bodyAnchor, boomMid, 0.13f, hydra);
            SmoothSphere("HydJoint", legRoot.transform, bodyAnchor, Vector3.one * 0.22f, metal);

            SmoothCylAt("Crush", legRoot.transform, foot + Vector3.up * 0.32f, 0.70f, 0.24f, carbon);
            SmoothCylAt("Foot", legRoot.transform, foot + Vector3.up * 0.10f, 1.70f, 0.08f, metal);
            SmoothCylAt("FootPad", legRoot.transform, foot, 2.15f, 0.035f, black);
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
        // Bell + exit + throat only (cooling rings live in heat texture)
        SmoothMesh.MakeBell("Bell", parent,
            new Vector3(xz.x, 0.50f * s, xz.z),
            1.30f * s, 0.76f * s, heat);
        SmoothCylAt("Exit", parent,
            new Vector3(xz.x, 0.02f * s, xz.z), 1.36f * s, 0.045f * s, metal);
        SmoothCylAt("Throat", parent,
            new Vector3(xz.x, 1.38f * s, xz.z), 0.34f * s, 0.10f * s, copper);
        SmoothSphere("Gimbal", parent,
            new Vector3(xz.x, 1.58f * s, xz.z), Vector3.one * ((center ? 0.36f : 0.26f) * s), metal);
        if (center)
            SmoothCylAt("Turbopump", parent,
                new Vector3(xz.x, 1.80f * s, xz.z), 0.62f * s, 0.14f * s, titanium);
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
