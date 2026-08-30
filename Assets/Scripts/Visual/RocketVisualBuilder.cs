using System.Collections;
using UnityEngine;

/// <summary>
/// Процедурна модель лише 1-го ступеня ~28 м: корпус, fins, ноги, сопла, FX.
/// </summary>
public static class RocketVisualBuilder
{
    public const float Height = 28f;   // компактний 1-й ступінь (не весь носій)
    public const float Radius = 1.83f; // Ø3.66 м
    public const string UpperStackName = "UpperStack";

    public static void Build(RocketPhysics rocket)
    {
        LunarTerrainMesh.Drain(BuildRoutine(rocket));
    }

    /// <summary>No-op: верхні ступені не моделюються (лише 1-й).</summary>
    public static void SetUpperStackVisible(Transform rocketRoot, bool visible, bool animateAway = false)
    {
        if (rocketRoot == null) return;
        var t = rocketRoot.Find(UpperStackName);
        if (t != null) Object.Destroy(t.gameObject);
        var orphan = GameObject.Find(UpperStackName);
        if (orphan != null) Object.Destroy(orphan);
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
        DestroyChild(root, UpperStackName);
        DestroyChild(root, "EngineFlame");
        DestroyChild(root, "EngineSmoke");
        DestroyChild(root, "EngineLight");

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root, false);

        // ── Палітра Falcon-class 1st stage booster ──
        // Преміум гладкий білий + м’яка кіптява знизу
        var white = MakeTankSkin("TankWhite", sootAmount: 0.0f, panelContrast: 0f, seed: 11);
        var whiteLower = MakeTankSkin("TankLower", sootAmount: 0.18f, panelContrast: 0f, seed: 29);
        yield return null;
        var black = VisualMaterials.Lit(new Color(0.035f, 0.038f, 0.042f), 0.28f, 0.32f);
        var metal = VisualMaterials.Lit(new Color(0.78f, 0.8f, 0.84f), 0.9f, 0.68f);
        var titanium = VisualMaterials.Lit(new Color(0.62f, 0.64f, 0.68f), 0.85f, 0.5f);
        var carbon = VisualMaterials.Lit(new Color(0.06f, 0.062f, 0.07f), 0.22f, 0.32f);
        var silver = VisualMaterials.Lit(new Color(0.88f, 0.89f, 0.92f), 0.92f, 0.75f);
        var heat = MakeNozzleSkin("NozzleHeat", seed: 7);
        var copper = VisualMaterials.Lit(new Color(0.55f, 0.38f, 0.28f), 0.92f, 0.42f);
        var darkMetal = VisualMaterials.Lit(new Color(0.14f, 0.15f, 0.17f), 0.82f, 0.4f);
        var hydra = VisualMaterials.Lit(new Color(0.86f, 0.88f, 0.91f), 0.72f, 0.55f);
        var interstageMat = MakeInterstageSkin("InterstageCFRP", seed: 41);
        var raceway = VisualMaterials.Lit(new Color(0.09f, 0.1f, 0.12f), 0.1f, 0.2f);
        var goldFoil = VisualMaterials.Lit(new Color(0.72f, 0.58f, 0.28f), 0.85f, 0.45f);
        yield return null;

        // ── Корма: суцільний чорний ──
        SmoothCyl("Octaweb", visual.transform, 0.42f, Radius * 2.18f, 0.42f, black);
        SmoothCyl("AftSkirt", visual.transform, 1.35f, Radius * 2.04f, 0.55f, black);
        SmoothCyl("AftJoin", visual.transform, 2.0f, Radius * 2.0f, 0.07f, black);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            var spoke = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spoke.name = $"OctSpoke_{i}";
            spoke.transform.SetParent(visual.transform, false);
            spoke.transform.localPosition = new Vector3(Mathf.Sin(a) * 0.5f, 0.42f, Mathf.Cos(a) * 0.5f);
            spoke.transform.localScale = new Vector3(0.07f, 0.45f, 0.8f);
            spoke.transform.localRotation = Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f);
            Object.Destroy(spoke.GetComponent<Collider>());
            var sr = spoke.GetComponent<MeshRenderer>();
            if (sr != null) sr.sharedMaterial = black;
        }

        float dBody = Radius * 2.0f;
        float yBot = 2.1f;

        // Плавний перехід кіптява → білий
        SmoothCyl("SootLo", visual.transform, yBot + 1.1f, dBody * 1.003f, 1.1f, whiteLower);
        float bodyBot = yBot;

        // Білий бак — компактніший 1-й ступінь
        const float bodyH = 20.5f;
        SmoothCyl("Stage1Body", visual.transform, yBot + bodyH * 0.5f, dBody, bodyH * 0.5f, white);
        float bodyTop = yBot + bodyH;
        yBot = bodyTop;

        // Рівномірні декоративні кільця (однаковий крок)
        const int nRings = 7;
        float ringMarginLo = 2.0f;
        float ringMarginHi = 1.6f;
        float ringSpan = (bodyTop - ringMarginHi) - (bodyBot + ringMarginLo);
        float ringStep = ringSpan / (nRings - 1);
        for (int i = 0; i < nRings; i++)
        {
            float y = bodyBot + ringMarginLo + i * ringStep;
            float halfH = (i % 3 == 0) ? 0.04f : 0.025f;
            float dia = (i % 3 == 0) ? dBody * 1.01f : dBody * 1.007f;
            SmoothCyl($"AccentRing_{i}", visual.transform, y, dia, halfH, silver);
        }

        // ── Чиста зона стиковки 1↔2 ──
        BuildDockingInterface(visual.transform, bodyTop, dBody,
            interstageMat, metal, titanium, carbon, darkMetal, goldFoil, silver);

        yield return null;
        BuildGridFins(visual.transform, titanium, silver, darkMetal, carbon, bodyTop - 0.25f);
        yield return null;
        BuildLegs(visual.transform, black, metal, titanium, carbon, darkMetal, hydra);
        yield return null;
        // Сопла: лише чорні (без білих кілець/gimbal)
        BuildNozzles(visual.transform, black, black, black, black, black);
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
    /// Ідеальна зона стиковки: пропорційний interstage + чистий sep flange.
    /// </summary>
    static void BuildDockingInterface(Transform visual, float bodyTop, float dBody,
        Material interstageMat, Material metal, Material titanium, Material carbon,
        Material darkMetal, Material goldFoil, Material silver)
    {
        float interH = 1.9f;
        float interBot = bodyTop - 0.02f;
        float interCenter = interBot + interH * 0.5f;
        float topY = interBot + interH;

        // Гладкий CFRP
        SmoothCyl("Interstage", visual, interCenter, dBody, interH * 0.5f, interstageMat);

        // Нижній join (білий бак → interstage)
        SmoothCyl("JoinRing", visual, interBot + 0.05f, dBody * 1.018f, 0.055f, metal);
        SmoothCyl("JoinRingFine", visual, interBot + 0.14f, dBody * 1.012f, 0.02f, silver);

        // MLI стрічка (одна, тонка)
        SmoothCyl("InterGold", visual, interBot + 0.5f, dBody * 1.01f, 0.08f, goldFoil);

        // Верхній sep flange — ідеальна площина
        SmoothCyl("SepFlange", visual, topY - 0.055f, dBody * 1.03f, 0.055f, metal);
        SmoothCyl("SepLip", visual, topY - 0.012f, dBody * 1.038f, 0.022f, silver);
        // Внутрішній shoulder
        SmoothCyl("SepInner", visual, topY - 0.12f, dBody * 0.96f, 0.04f, titanium);

        // Відкритий колодязь
        SmoothCyl("SepWell", visual, topY - 0.5f, dBody * 0.88f, 0.4f, carbon);
        SmoothCyl("SepFloor", visual, topY - 0.95f, dBody * 0.82f, 0.055f, darkMetal);
        SmoothMesh.MakeFrustum("SepChamfer", visual,
            new Vector3(0f, topY - 0.22f, 0f),
            dBody * 0.97f, 0.11f, topRatio: 0.9f, titanium);
    }

    // ─────────────────────────────────────────────────────────────
    // Процедурні циліндричні skins — гладкий білий (без панелей)
    // ─────────────────────────────────────────────────────────────

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

                // Premium white
                float g = 0.992f;
                g += HashNoise(u * 14f + ox, v * 28f + oy) * 0.002f;

                float line = 0f;

                // 1) Рівномірні горизонтальні кільця (однаковий крок)
                const int nTexRings = 8;
                const float v0 = 0.08f;
                const float v1 = 0.92f;
                float step = (v1 - v0) / (nTexRings - 1);
                for (int ri = 0; ri < nTexRings; ri++)
                {
                    float rv = v0 + ri * step;
                    float d = Mathf.Abs(v - rv);
                    // major кожні 3-тє
                    float halfW = (ri % 3 == 0) ? 0.0045f : 0.0028f;
                    line = Mathf.Max(line, 1f - Mathf.SmoothStep(0.0006f, halfW, d));
                }

                // 2) Елегантний шевронний пояс (одна зона)
                if (v > 0.38f && v < 0.50f)
                {
                    float local = (v - 0.38f) / 0.12f;
                    float wave = Mathf.Abs(Mathf.Sin((u * 7f + local * 1.2f) * Mathf.PI));
                    float chev = (1f - Mathf.SmoothStep(0.05f, 0.16f, wave)) * Mathf.Sin(local * Mathf.PI);
                    line = Mathf.Max(line, chev * 0.7f);
                }

                // 3) Тонкий emblem-овал (один, симетричний)
                {
                    float lu = (u - 0.5f) * 10f;
                    float lv = (v - 0.22f) * 28f;
                    float ell = lu * lu * 1.8f + lv * lv;
                    float outline = 1f - Mathf.SmoothStep(0.03f, 0.1f, Mathf.Abs(ell - 1f));
                    float band = Mathf.SmoothStep(0.16f, 0.19f, v) * (1f - Mathf.SmoothStep(0.25f, 0.28f, v));
                    line = Mathf.Max(line, outline * band * 0.85f);
                }

                // 4) Rivet dots на одному major-кільці
                float rivetV = v0 + 6 * step; // 7-ме кільце
                if (Mathf.Abs(v - rivetV) < 0.012f)
                {
                    float dots = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 36f));
                    dots = 1f - Mathf.SmoothStep(0.12f, 0.4f, dots);
                    line = Mathf.Max(line, dots * 0.45f);
                }

                g -= line * 0.075f;

                if (sootAmount > 0.01f)
                {
                    float sootV = Mathf.Clamp01(1f - v * 1.7f);
                    sootV = sootV * sootV * (0.85f + 0.15f * HashNoise(u * 3f, v * 5f));
                    g -= sootAmount * sootV * 0.26f;
                }

                g = Mathf.Clamp(g, 0.86f, 0.999f);
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
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", sootAmount > 0.1f ? 0.72f : 0.94f);
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.EnableKeyword("_BASEMAP");
        }
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        mat.mainTexture = tex;
        // Без normal map панелей — ідеально гладкий корпус
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

            // Шарнір під компактний 1-й ступінь
            Vector3 hinge = new Vector3(
                Mathf.Sin(a) * (Radius + 0.28f), 6.2f, Mathf.Cos(a) * (Radius + 0.28f));
            Vector3 foot = new Vector3(
                Mathf.Sin(a) * (Radius + 5.8f), 0.05f, Mathf.Cos(a) * (Radius + 5.8f));

            SmoothSphere("Hinge", legRoot.transform, hinge, Vector3.one * 0.48f, titanium);
            SmoothCylAt("HingeCap", legRoot.transform, hinge + Vector3.up * 0.12f, 0.5f, 0.07f, darkMetal);
            SmoothCylAt("HingeFair", legRoot.transform, hinge + Vector3.up * 0.28f, 0.36f, 0.16f, carbon);
            Strut(legRoot.transform, "Boom", hinge, foot, 0.3f, black);
            Strut(legRoot.transform, "BoomEdge",
                hinge + Vector3.up * 0.1f,
                foot + Vector3.up * 0.1f, 0.09f, titanium);
            Strut(legRoot.transform, "BoomEdgeLo",
                hinge - Vector3.up * 0.07f,
                foot - Vector3.up * 0.02f, 0.07f, darkMetal);

            Vector3 bodyAnchor = new Vector3(
                Mathf.Sin(a) * (Radius + 0.06f), 4.0f, Mathf.Cos(a) * (Radius + 0.06f));
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
        // без додаткового білого кільця під соплами
    }

    static void Nozzle(Transform parent, Vector3 xz, Material heat, Material metal, Material copper,
        Material titanium, Material darkMetal, float s, bool center)
    {
        // Усе чорне — без білих круглих виступів
        SmoothMesh.MakeBell("Bell", parent,
            new Vector3(xz.x, 0.52f * s, xz.z),
            1.34f * s, 0.78f * s, heat);
        SmoothCylAt("Exit", parent,
            new Vector3(xz.x, 0.02f * s, xz.z), 1.40f * s, 0.035f * s, heat);
        SmoothCylAt("Throat", parent,
            new Vector3(xz.x, 1.35f * s, xz.z), 0.34f * s, 0.1f * s, heat);
        if (center)
        {
            SmoothCylAt("Turbopump", parent,
                new Vector3(xz.x, 1.75f * s, xz.z), 0.55f * s, 0.14f * s, heat);
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
        // Довгий теплий plume Merlin-class
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(55f, 110f);
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 3.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.92f, 0.6f, 0.95f),
            new Color(1f, 0.45f, 0.1f, 0.75f));
        main.maxParticles = 900;
        main.gravityModifier = 0.01f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 9f;
        shape.radius = 1.35f;
        shape.radiusThickness = 0.6f;
        shape.arc = 360f;
        shape.alignToDirection = false;
        shape.randomDirectionAmount = 0.08f;

        DisableVelocityOverLifetime(ps);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.98f, 0.85f), 0f),
                new GradientColorKey(new Color(1f, 0.78f, 0.3f), 0.18f),
                new GradientColorKey(new Color(1f, 0.42f, 0.08f), 0.45f),
                new GradientColorKey(new Color(0.55f, 0.15f, 0.04f), 0.75f),
                new GradientColorKey(new Color(0.15f, 0.05f, 0.02f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.85f, 0.15f),
                new GradientAlphaKey(0.5f, 0.45f),
                new GradientAlphaKey(0.2f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.separateAxes = false;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f, 0.45f),
                new Keyframe(0.2f, 1.0f),
                new Keyframe(0.55f, 1.55f),
                new Keyframe(1f, 2.4f)));

        var noise = ps.noise;
        noise.enabled = true;
        noise.separateAxes = false;
        noise.strength = new ParticleSystem.MinMaxCurve(0.65f);
        noise.frequency = 0.55f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(1.4f);
        noise.damping = true;
        noise.octaveCount = 2;
        noise.quality = ParticleSystemNoiseQuality.High;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.ParticleAdditive(new Color(1f, 0.6f, 0.2f, 1f));
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
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(90f, 160f);
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 1.15f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 1f),
            new Color(0.55f, 0.85f, 1f, 0.98f));
        main.maxParticles = 450;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 2.5f;
        shape.radius = 0.5f;
        shape.radiusThickness = 0.35f;
        shape.alignToDirection = false;

        DisableVelocityOverLifetime(ps);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 1f, 1f), 0f),
                new GradientColorKey(new Color(0.85f, 0.95f, 1f), 0.25f),
                new GradientColorKey(new Color(0.5f, 0.78f, 1f), 0.6f),
                new GradientColorKey(new Color(0.25f, 0.4f, 0.9f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.9f, 0.3f),
                new GradientAlphaKey(0.35f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.separateAxes = false;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f, 0.55f),
                new Keyframe(0.3f, 1.05f),
                new Keyframe(0.7f, 0.85f),
                new Keyframe(1f, 0.25f)));

        var noise = ps.noise;
        noise.enabled = true;
        noise.separateAxes = false;
        noise.strength = new ParticleSystem.MinMaxCurve(0.22f);
        noise.frequency = 0.9f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(2.2f);
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.High;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.ParticleAdditive(new Color(0.75f, 0.92f, 1f, 1f));
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
