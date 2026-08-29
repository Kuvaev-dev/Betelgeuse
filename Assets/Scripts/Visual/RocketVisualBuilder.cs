using System.Collections;
using UnityEngine;

/// <summary>
/// Процедурна модель 1-го ступеня ~42 м (Falcon-class): корпус, fins, ноги, сопла, FX.
/// </summary>
public static class RocketVisualBuilder
{
    public const float Height = 39.2f; // стек 1-го ступеня (купол interstage, без високого обтічника)
    public const float Radius = 1.85f;

    public static void Build(RocketPhysics rocket)
    {
        LunarTerrainMesh.Drain(BuildRoutine(rocket));
    }

    /// <summary>Покрокова збірка — yield між важкими skins, щоб splash-спінер крутився.</summary>
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

        // ── Палітра — вигляд flight-hardware: холодніший білий, різкіший метал, глибший CFRP ──
        // Чистіший білий стек — м’якші шви панелей, світліша кіптява
        var white = MakeTankSkin("TankWhite", sootAmount: 0.0f, panelContrast: 0.032f, seed: 11);
        var whiteLower = MakeTankSkin("TankLower", sootAmount: 0.14f, panelContrast: 0.038f, seed: 29);
        yield return null;
        var black = VisualMaterials.Lit(new Color(0.028f, 0.03f, 0.034f), 0.58f, 0.42f);
        var metal = VisualMaterials.Lit(new Color(0.86f, 0.88f, 0.92f), 0.96f, 0.9f);
        var titanium = VisualMaterials.Lit(new Color(0.74f, 0.76f, 0.8f), 0.94f, 0.8f);
        var carbon = VisualMaterials.Lit(new Color(0.05f, 0.052f, 0.056f), 0.4f, 0.5f);
        var silver = VisualMaterials.Lit(new Color(0.94f, 0.95f, 0.98f), 0.97f, 0.92f);
        var heat = MakeNozzleSkin("NozzleHeat", seed: 7);
        var copper = VisualMaterials.Lit(new Color(0.68f, 0.48f, 0.34f), 0.94f, 0.62f);
        var darkMetal = VisualMaterials.Lit(new Color(0.14f, 0.15f, 0.17f), 0.9f, 0.55f);
        var hydra = VisualMaterials.Lit(new Color(0.9f, 0.92f, 0.95f), 0.88f, 0.7f);
        var goldFoil = VisualMaterials.Lit(new Color(0.8f, 0.66f, 0.32f), 0.88f, 0.58f);
        var interstageMat = MakeInterstageSkin("InterstageCFRP", seed: 41);
        yield return null;

        // ── Корма (octaweb + TPS skirt) ──
        SmoothCyl("Octaweb", visual.transform, 0.82f, Radius * 2.42f, 0.88f, black);
        SmoothCyl("OctawebLip", visual.transform, 0.22f, Radius * 2.52f, 0.04f, titanium);
        SmoothCyl("OctawebRing", visual.transform, 1.55f, Radius * 2.36f, 0.035f, metal);
        SmoothCyl("AftSkirt", visual.transform, 2.65f, Radius * 2.16f, 0.78f, carbon);
        SmoothCyl("AftSkirtBand", visual.transform, 2.9f, Radius * 2.18f, 0.05f, darkMetal);
        SmoothCyl("AftSkirtRim", visual.transform, 3.48f, Radius * 2.18f, 0.04f, titanium);
        SmoothCyl("AftJoin", visual.transform, 3.62f, Radius * 2.06f, 0.07f, silver);

        // ── Стек корпусу: неперервні стиковані циліндри (без «висячих» щілин) ──
        // Одиниця MakeCylinder — висота 2 → повна висота = 2 * halfH; легкий overlap закриває шви.
        float dBody = Radius * 2.0f;
        float dBand = Radius * 2.02f;
        const float seam = 0.06f; // overlap між сусідами, щоб ступені ніколи не «висіли» окремо

        // Курсор = низ наступної секції (world Y)
        float yBot = 3.70f; // стоїть на верху AftJoin (~3.62+0.07)

        void StackCyl(string name, float height, float diameter, Material mat)
        {
            float half = height * 0.5f;
            float center = yBot + half;
            SmoothCyl(name, visual.transform, center, diameter, half, mat);
            yBot += height - seam; // наступна секція стартує з overlap
        }

        // М’яка кіптява біля основи білого стека
        SmoothCyl("SootFade", visual.transform, yBot + 0.55f, Radius * 2.012f, 0.55f,
            VisualMaterials.Lit(new Color(0.78f, 0.77f, 0.76f), 0.16f, 0.45f));

        float lowerBot = yBot;
        StackCyl("LowerTank", 8.6f, dBody, whiteLower);
        float lowerTop = yBot + seam;

        float yBandLo = yBot + 0.36f;
        StackCyl("StationLo", 0.72f, dBand, interstageMat);
        SmoothCyl("StationLoGold", visual.transform, yBandLo, Radius * 2.005f, 0.045f, goldFoil);

        float midBot = yBot;
        StackCyl("MidTank", 13.6f, dBody, white);
        float midTop = yBot + seam;

        float yBandHi = yBot + 0.36f;
        StackCyl("StationHi", 0.72f, dBand, interstageMat);
        SmoothCyl("StationHiGold", visual.transform, yBandHi, Radius * 2.005f, 0.045f, goldFoil);

        float upperBot = yBot;
        StackCyl("UpperTank", 6.8f, dBody, white);
        float upperTop = yBot + seam;

        // Волосинні срібні зварні кільця лише всередині білих ділянок
        void RingsIn(float a, float b, int n, int id0)
        {
            if (b <= a + 0.2f) return;
            for (int i = 0; i < n; i++)
            {
                float t = (i + 1f) / (n + 1f);
                float y = Mathf.Lerp(a + 0.15f, b - 0.15f, t);
                SmoothCyl($"Ring_{id0 + i}", visual.transform, y, Radius * 2.022f, 0.008f, silver);
            }
        }
        RingsIn(lowerBot, lowerTop, 4, 0);
        RingsIn(midBot, midTop, 6, 10);
        RingsIn(upperBot, upperTop, 3, 20);

        // ── Голова: чорний «капелюх» впритул на верхньому баку ──
        float interH = 1.65f;
        float interCenter = yBot + interH * 0.5f;
        SmoothCyl("Interstage", visual.transform, interCenter, dBody, interH * 0.5f, interstageMat);
        float interstageMidY = interCenter;
        yBot += interH - seam;

        SmoothCyl("Bulkhead", visual.transform, yBot + 0.07f, dBody, 0.07f, carbon);
        yBot += 0.14f;
        SmoothMesh.MakeFrustum("NoseShoulder", visual.transform,
            new Vector3(0f, yBot + 0.34f, 0f),
            Radius * 2.0f, 0.34f, topRatio: 0.44f, carbon);
        yBot += 0.68f;
        SmoothMesh.MakeOgive("NoseTip", visual.transform,
            new Vector3(0f, yBot + 0.44f, 0f),
            Radius * 0.92f, 0.44f, black, tipBlunt: 0.1f);

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
    // Процедурні циліндричні skins (U=кут, V=висота) — без warping
    // ─────────────────────────────────────────────────────────────

    static Material MakeTankSkin(string name, float sootAmount, float panelContrast, int seed)
    {
        // Половина попереднього atlas — у 4× менше texels, досі чітко на корпусі ~42 м
        const int tw = 512;
        const int th = 1024;
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
        // Попередні hash-зсуви
        float ox = (float)rng.NextDouble() * 40f;
        float oy = (float)rng.NextDouble() * 40f;

        // Сітка панелей у UV-просторі
        int nVert = 12;   // кількість вертикальних stringers по колу
        int nHoriz = 28;  // кількість горизонтальних відсіків уздовж висоти

        for (int y = 0; y < th; y++)
        {
            float v = y / (float)(th - 1);
            for (int x = 0; x < tw; x++)
            {
                float u = x / (float)tw;
                int idx = y * tw + x;

                // Premium flight-white — м’які лінії відсіків, без темної «смуги» stringer
                float g = 0.978f;
                g += HashNoise(u * 36f + ox, v * 72f + oy) * 0.004f;
                g += HashNoise(u * 90f - ox, v * 140f + oy) * 0.002f;

                // Горизонтальні шви відсіків (стримані)
                float hCell = v * nHoriz;
                float hEdge = Mathf.Abs(hCell - Mathf.Round(hCell));
                float hSeam = 1f - Mathf.SmoothStep(0f, 0.045f, hEdge);
                g -= hSeam * panelContrast * 0.48f;

                // Вертикальні stringers — дуже м’які, щоб ніколи не читались як чорна лінія
                float vCell = u * nVert;
                float vEdge = Mathf.Abs(vCell - Mathf.Round(vCell));
                float vSeam = 1f - Mathf.SmoothStep(0f, 0.022f, vEdge);
                g -= vSeam * panelContrast * 0.22f;

                // М’який wash кіптяви (лише нижні баки)
                if (sootAmount > 0.01f)
                {
                    float sootV = Mathf.Clamp01(1f - v * 1.7f);
                    sootV = sootV * sootV;
                    float blot = 0.72f + 0.28f * HashNoise(u * 5f + 3f, v * 8f - 2f);
                    g -= sootAmount * sootV * blot * 0.28f;
                }

                g += HashNoise(u * 280f, v * 520f) * 0.003f;
                g = Mathf.Clamp(g, 0.72f, 0.995f);

                // Холодний чистий білий (легка синява, premium-фарба)
                float rC = Mathf.Clamp01(g * 0.994f);
                float gC = Mathf.Clamp01(g * 0.999f);
                float bC = Mathf.Clamp01(g * 1.01f);
                cols[idx] = new Color(rC, gC, bC, 1f);

                // М’які normals панелей
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
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.03f);
        // Глянцева аерокосмічна фарба на чистих баках; трохи м’якше на закіптюжених
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", sootAmount > 0.1f ? 0.62f : 0.88f);
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
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 0.22f);
        }
        return mat;
    }

    static float SampleGray(float u, float v, int nVert, int nHoriz, float sootAmount, float panelContrast, float ox, float oy)
    {
        u = u - Mathf.Floor(u);
        v = Mathf.Clamp01(v);
        float g = 0.978f;
        g += HashNoise(u * 36f + ox, v * 72f + oy) * 0.004f;
        float hCell = v * nHoriz;
        float hEdge = Mathf.Abs(hCell - Mathf.Round(hCell));
        g -= (1f - Mathf.SmoothStep(0f, 0.045f, hEdge)) * panelContrast * 0.48f;
        float vCell = u * nVert;
        float vEdge = Mathf.Abs(vCell - Mathf.Round(vCell));
        g -= (1f - Mathf.SmoothStep(0f, 0.022f, vEdge)) * panelContrast * 0.22f;
        if (sootAmount > 0.01f)
        {
            float sootV = Mathf.Clamp01(1f - v * 1.7f);
            sootV = sootV * sootV;
            g -= sootAmount * sootV * 0.22f;
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
                // Глибоке плетиво CFRP з легкими смугами блиску
                float g = 0.055f;
                float weaveU = Mathf.Abs((u * 40f) - Mathf.Round(u * 40f));
                float weaveV = Mathf.Abs((v * 22f) - Mathf.Round(v * 22f));
                g += (1f - Mathf.SmoothStep(0f, 0.12f, weaveU)) * 0.028f;
                g += (1f - Mathf.SmoothStep(0f, 0.12f, weaveV)) * 0.022f;
                g += HashNoise(u * 48f + ox, v * 48f) * 0.012f;
                float band = Mathf.Abs((v * 5f) - Mathf.Round(v * 5f));
                g += (1f - Mathf.SmoothStep(0f, 0.07f, band)) * 0.045f;
                // Легкий вертикальний градієнт блиску
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
                // Чистий обтічник: легкий micro-noise + лише 2 поздовжні half-seams
                float g = 0.94f;
                g += HashNoise(u * 28f + ox, v * 36f) * 0.008f;
                float petal = u * 2f; // дві половини обтічника
                float seam = Mathf.Abs(petal - Mathf.Round(petal));
                float seamW = 1f - Mathf.SmoothStep(0f, 0.012f, seam);
                g -= seamW * 0.06f;
                // лише одне access-кільце біля основи
                float h = Mathf.Abs(v - 0.12f);
                g -= (1f - Mathf.SmoothStep(0f, 0.025f, h)) * 0.03f;
                // tip трохи світліший (гладка фарба)
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
                // Регенеративно охолоджене ніобієве сопло: темний exit → бронза mid → сталевий throat
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
                // Закіптюжена кромка exit
                if (v < 0.12f)
                {
                    float t = 1f - v / 0.12f;
                    rC = Mathf.Lerp(rC, 0.08f, t * 0.7f);
                    gC = Mathf.Lerp(gC, 0.08f, t * 0.7f);
                    bC = Mathf.Lerp(bC, 0.09f, t * 0.7f);
                }
                // Яскраве металеве горло сопла
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
    // Підвузли
    // ─────────────────────────────────────────────────────────────

    static void BuildGridFins(Transform visual, Material frame, Material lattice, Material hub, Material carbon,
        float mountY = 37.2f)
    {
        // Титанові grid fins у стилі Falcon: щільна решітка, подвійна рама, чиста штанга привода.
        // Локально: +Z outboard, +Y up, +X tangent. Пластина повністю outboard від корпусу.
        const float armLen = 1.05f;
        const float plateT = 0.055f;
        const float plateH = 2.55f;
        const float plateW = 3.25f;
        const float frameW = 0.07f;
        float hullR = Radius + 0.06f;

        // Трохи холодніші металеві акценти для решітки
        var cellMat = lattice;
        var edgeMat = frame;
        var dark = carbon;

        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            Vector3 radial = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));

            var fin = new GameObject($"GridFin_{i}");
            fin.transform.SetParent(visual, false);
            fin.transform.localPosition = radial * hullR + Vector3.up * mountY;
            // Кут 8° — вигляд у розкритому стані, ловить світло
            fin.transform.localRotation = Quaternion.LookRotation(radial, Vector3.up)
                * Quaternion.Euler(0f, 0f, i % 2 == 0 ? 6f : -6f);

            // ── Стек кріплення до корпусу ──
            SmoothSphere("Hub", fin.transform, new Vector3(0f, 0f, 0.05f), Vector3.one * 0.38f, hub);
            var basePad = SmoothCylAt("BasePad", fin.transform, new Vector3(0f, 0f, 0.07f), 0.55f, 0.055f, edgeMat);
            basePad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var baseRing = SmoothCylAt("BaseRing", fin.transform, new Vector3(0f, 0f, 0.11f), 0.42f, 0.03f, hub);
            baseRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Корпус привода + конусна штанга
            float armEnd = armLen - 0.1f;
            SmoothCylAt("Actuator", fin.transform, new Vector3(0f, 0f, 0.28f), 0.28f, 0.16f, dark);
            var act = fin.transform.Find("Actuator");
            if (act != null) act.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Strut(fin.transform, "Arm", new Vector3(0f, 0f, 0.22f), new Vector3(0f, 0f, armEnd),
                0.16f, dark);
            Strut(fin.transform, "ArmCore", new Vector3(0f, 0f, 0.24f), new Vector3(0f, 0f, armEnd - 0.02f),
                0.08f, edgeMat);
            // Дві тонкі напрямні вздовж штанги
            Strut(fin.transform, "RailA", new Vector3(0.07f, 0.06f, 0.26f), new Vector3(0.07f, 0.06f, armEnd),
                0.035f, hub);
            Strut(fin.transform, "RailB", new Vector3(-0.07f, -0.06f, 0.26f), new Vector3(-0.07f, -0.06f, armEnd),
                0.035f, hub);

            SmoothSphere("Wrist", fin.transform, new Vector3(0f, 0f, armEnd + 0.03f),
                Vector3.one * 0.3f, hub);
            var wristRing = SmoothCylAt("WristRing", fin.transform,
                new Vector3(0f, 0f, armEnd + 0.06f), 0.36f, 0.025f, edgeMat);
            wristRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // ── Решітчаста пластина (outboard) ──
            float z = armLen + plateT * 0.5f;
            // Підкладка — темний лист CFRP позаду решітки
            Prim(PrimitiveType.Cube, "Backing", fin.transform, new Vector3(0f, 0f, z - plateT * 0.15f),
                new Vector3(plateW - 0.12f, plateH - 0.12f, plateT * 0.35f), dark);

            // Зовнішня титанова рама (товща)
            float hw = plateW * 0.5f;
            float hh = plateH * 0.5f;
            float zF = z + plateT * 0.15f;
            float ft = plateT + 0.03f;
            Prim(PrimitiveType.Cube, "RimT", fin.transform, new Vector3(0f, hh - frameW * 0.5f, zF),
                new Vector3(plateW, frameW, ft), edgeMat);
            Prim(PrimitiveType.Cube, "RimB", fin.transform, new Vector3(0f, -(hh - frameW * 0.5f), zF),
                new Vector3(plateW, frameW, ft), edgeMat);
            Prim(PrimitiveType.Cube, "RimL", fin.transform, new Vector3(-(hw - frameW * 0.5f), 0f, zF),
                new Vector3(frameW, plateH - frameW * 2f, ft), edgeMat);
            Prim(PrimitiveType.Cube, "RimR", fin.transform, new Vector3(hw - frameW * 0.5f, 0f, zF),
                new Vector3(frameW, plateH - frameW * 2f, ft), edgeMat);

            // Внутрішня кромка (трохи врізана, світліша)
            float inset = frameW + 0.02f;
            float ft2 = plateT + 0.01f;
            Prim(PrimitiveType.Cube, "LipT", fin.transform, new Vector3(0f, hh - inset - 0.015f, zF + 0.01f),
                new Vector3(plateW - inset * 2f, 0.03f, ft2), cellMat);
            Prim(PrimitiveType.Cube, "LipB", fin.transform, new Vector3(0f, -(hh - inset - 0.015f), zF + 0.01f),
                new Vector3(plateW - inset * 2f, 0.03f, ft2), cellMat);

            // Щільна стільникова решітка (8 × 10 комірок)
            const int nH = 8;
            const int nV = 10;
            float innerW = plateW - frameW * 2.4f;
            float innerH = plateH - frameW * 2.4f;
            float bar = 0.028f;
            float zL = z + plateT * 0.05f;

            for (int gy = 0; gy <= nH; gy++)
            {
                float yy = -innerH * 0.5f + gy * (innerH / nH);
                Prim(PrimitiveType.Cube, $"H_{gy}", fin.transform,
                    new Vector3(0f, yy, zL),
                    new Vector3(innerW, bar, plateT * 0.7f), cellMat);
            }
            for (int gx = 0; gx <= nV; gx++)
            {
                float xx = -innerW * 0.5f + gx * (innerW / nV);
                Prim(PrimitiveType.Cube, $"V_{gx}", fin.transform,
                    new Vector3(xx, 0f, zL),
                    new Vector3(bar, innerH, plateT * 0.7f), cellMat);
            }

            // Кутові накладки (як механічно оброблені титанові стики)
            float c = 0.11f;
            float cz = zF + 0.01f;
            Prim(PrimitiveType.Cube, "C_TL", fin.transform, new Vector3(-(hw - c * 0.55f), hh - c * 0.55f, cz),
                new Vector3(c, c, ft + 0.01f), hub);
            Prim(PrimitiveType.Cube, "C_TR", fin.transform, new Vector3(hw - c * 0.55f, hh - c * 0.55f, cz),
                new Vector3(c, c, ft + 0.01f), hub);
            Prim(PrimitiveType.Cube, "C_BL", fin.transform, new Vector3(-(hw - c * 0.55f), -(hh - c * 0.55f), cz),
                new Vector3(c, c, ft + 0.01f), hub);
            Prim(PrimitiveType.Cube, "C_BR", fin.transform, new Vector3(hw - c * 0.55f, -(hh - c * 0.55f), cz),
                new Vector3(c, c, ft + 0.01f), hub);
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
                Mathf.Sin(a) * (Radius + 0.32f), 9.35f, Mathf.Cos(a) * (Radius + 0.32f));
            Vector3 foot = new Vector3(
                Mathf.Sin(a) * (Radius + 6.45f), 0.06f, Mathf.Cos(a) * (Radius + 6.45f));

            // Обтічник шарніра + основна балка + вторинна A-frame
            SmoothSphere("Hinge", legRoot.transform, hinge, Vector3.one * 0.56f, titanium);
            SmoothCylAt("HingeCap", legRoot.transform, hinge + Vector3.up * 0.16f, 0.58f, 0.09f, darkMetal);
            SmoothCylAt("HingeFair", legRoot.transform, hinge + Vector3.up * 0.35f, 0.42f, 0.2f, carbon);
            Strut(legRoot.transform, "Boom", hinge, foot, 0.36f, black);
            Strut(legRoot.transform, "BoomEdge",
                hinge + Vector3.up * 0.14f,
                foot + Vector3.up * 0.14f, 0.11f, titanium);
            Strut(legRoot.transform, "BoomEdgeLo",
                hinge - Vector3.up * 0.1f,
                foot - Vector3.up * 0.02f, 0.08f, darkMetal);

            Vector3 bodyAnchor = new Vector3(
                Mathf.Sin(a) * (Radius + 0.08f), 5.9f, Mathf.Cos(a) * (Radius + 0.08f));
            Vector3 boomMid = Vector3.Lerp(hinge, foot, 0.4f);
            Vector3 boomKnee = Vector3.Lerp(hinge, foot, 0.68f);
            Strut(legRoot.transform, "Hydraulics", bodyAnchor, boomMid, 0.13f, hydra);
            Strut(legRoot.transform, "LockLink", bodyAnchor + Vector3.up * 0.8f, boomKnee, 0.08f, metal);
            SmoothSphere("HydJoint", legRoot.transform, bodyAnchor, Vector3.one * 0.26f, metal);
            SmoothSphere("HydKnee", legRoot.transform, boomMid, Vector3.one * 0.2f, titanium);

            // Стек посадкової ноги — crush core + широка підошва + traction ring
            SmoothCylAt("Crush", legRoot.transform, foot + Vector3.up * 0.42f, 0.78f, 0.28f, carbon);
            SmoothCylAt("CrushLip", legRoot.transform, foot + Vector3.up * 0.22f, 0.95f, 0.05f, darkMetal);
            SmoothCylAt("Foot", legRoot.transform, foot + Vector3.up * 0.12f, 1.95f, 0.08f, metal);
            SmoothCylAt("FootPad", legRoot.transform, foot, 2.4f, 0.035f, black);
            SmoothCylAt("FootRing", legRoot.transform, foot + Vector3.up * 0.05f, 2.15f, 0.022f, titanium);
            SmoothCylAt("FootGrip", legRoot.transform, foot + Vector3.up * 0.02f, 1.6f, 0.018f, darkMetal);
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
        // Кромка exit сопла біля y≈0 (center bell s=1.2). Емісія одразу під exit, не в бак.
        const float exitY = -0.08f;
        // За замовчуванням PS летить уздовж +Z; pitch 90° → вихлоп униз (−Y).
        var down = Quaternion.Euler(90f, 0f, 0f);

        var flameGo = new GameObject("EngineFlame");
        flameGo.transform.SetParent(visual, false);
        flameGo.transform.localPosition = new Vector3(0f, exitY, 0f);
        flameGo.transform.localRotation = down;
        var flame = flameGo.AddComponent<ParticleSystem>();
        ConfigureFlameOuter(flame);

        var coreGo = new GameObject("EngineFlameCore");
        coreGo.transform.SetParent(visual, false);
        coreGo.transform.localPosition = new Vector3(0f, exitY - 0.05f, 0f);
        coreGo.transform.localRotation = down;
        var core = coreGo.AddComponent<ParticleSystem>();
        ConfigureFlameCore(core);

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
        lightGo.transform.localPosition = new Vector3(0f, exitY - 1.2f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.72f, 0.38f);
        light.intensity = 0f;
        light.range = 140f;
        light.shadows = LightShadows.None;

        var fx = visual.gameObject.AddComponent<RocketEngineFX>();
        fx.flame = flame;
        fx.flameCore = core;
        fx.smoke = smoke;
        fx.sparks = sparks;
        fx.dust = dust;
        fx.engineLight = light;
    }

    /// <summary>Unity вимагає, щоб УСІ осі velocityOverLifetime були в одному режимі MinMaxCurve.</summary>
    static void DisableVelocityOverLifetime(ParticleSystem ps)
    {
        var vel = ps.velocityOverLifetime;
        vel.enabled = false;
        // Форсувати однаковий Constant mode на кожній осі (запобігає spam runtime-помилок)
        vel.x = new ParticleSystem.MinMaxCurve(0f);
        vel.y = new ParticleSystem.MinMaxCurve(0f);
        vel.z = new ParticleSystem.MinMaxCurve(0f);
        vel.speedModifier = new ParticleSystem.MinMaxCurve(1f);
    }

    static void ConfigureFlameOuter(ParticleSystem ps)
    {
        // Тепла оболонка з exit сопла — billboard, world-space, без зламаних VOL curves
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(35f, 70f);
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(0.9f, 2.4f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.9f, 0.55f, 0.92f),
            new Color(1f, 0.4f, 0.08f, 0.7f));
        main.maxParticles = 500;
        main.gravityModifier = 0.02f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 7f;
        shape.radius = 1.15f; // ≈ радіус exit центрального сопла
        shape.radiusThickness = 0.55f;
        shape.arc = 360f;
        shape.alignToDirection = false;
        shape.randomDirectionAmount = 0.05f;

        DisableVelocityOverLifetime(ps);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.75f), 0f),
                new GradientColorKey(new Color(1f, 0.7f, 0.25f), 0.25f),
                new GradientColorKey(new Color(1f, 0.38f, 0.08f), 0.55f),
                new GradientColorKey(new Color(0.35f, 0.1f, 0.04f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.85f, 0f),
                new GradientAlphaKey(0.75f, 0.2f),
                new GradientAlphaKey(0.35f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.separateAxes = false;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f, 0.55f),
                new Keyframe(0.35f, 1.1f),
                new Keyframe(1f, 1.65f)));

        var noise = ps.noise;
        noise.enabled = true;
        noise.separateAxes = false;
        noise.strength = new ParticleSystem.MinMaxCurve(0.4f);
        noise.frequency = 0.7f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(1.1f);
        noise.damping = true;
        noise.octaveCount = 1;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.ParticleAdditive(new Color(1f, 0.65f, 0.25f, 1f));
        rend.sortingFudge = -2f;
    }

    static void ConfigureFlameCore(ParticleSystem ps)
    {
        // Яскравий струмінь ядра з центру throat/exit
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(55f, 100f);
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.95f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 1f),
            new Color(0.6f, 0.88f, 1f, 0.95f));
        main.maxParticles = 280;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 3f;
        shape.radius = 0.45f;
        shape.radiusThickness = 0.4f;
        shape.alignToDirection = false;

        DisableVelocityOverLifetime(ps);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 1f, 1f), 0f),
                new GradientColorKey(new Color(0.75f, 0.92f, 1f), 0.35f),
                new GradientColorKey(new Color(0.45f, 0.7f, 1f), 0.75f),
                new GradientColorKey(new Color(0.3f, 0.4f, 0.85f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.4f),
                new GradientAlphaKey(0.2f, 0.85f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.separateAxes = false;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f, 0.7f),
                new Keyframe(0.4f, 1f),
                new Keyframe(1f, 0.4f)));

        var noise = ps.noise;
        noise.enabled = true;
        noise.separateAxes = false;
        noise.strength = new ParticleSystem.MinMaxCurve(0.15f);
        noise.frequency = 1.1f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(1.8f);
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.ParticleAdditive(new Color(0.7f, 0.9f, 1f, 1f));
        rend.sortingFudge = -6f;
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
        // Тримати x/y у тому ж mode, що z, коли separateAxes=false — Unity використовує лише z

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
