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

        // Базовий mat (albedo підставить LunarTerrainMesh)
        var regolith = VisualMaterials.Lit(
            new Color(0.62f, 0.62f, 0.63f),
            metallic: 0.0f,
            smooth: 0.05f);

        // Cratered disk — єдина видима поверхня (без сірого «обідка» зовні)
        float R = LunarTerrainMesh.TerrainRadius;
        LunarTerrainMesh.Create(surface.transform, regolith,
            resolution: 400, radius: R);

        // Підкладка трохи менша за crater-диск (world radius ≈ 0.99·R).
        // MakeCylinder: scale.x = diameter, mesh r=0.5 → worldR = diameter/2.
        var farMat = VisualMaterials.Lit(new Color(0.38f, 0.38f, 0.39f), 0.0f, 0.02f);
        float underDiameter = R * 2f * 0.99f; // ≈ 2R, не виступає за край
        var far = SmoothMesh.MakeCylinder("HorizonDisk", surface.transform,
            new Vector3(0f, -2.8f, 0f), underDiameter, 2.2f, farMat);
        var fr = far.GetComponent<MeshRenderer>();
        if (fr != null)
        {
            fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fr.receiveShadows = true;
        }

        // Валуни (smooth spheres) по всьому диску
        var rng = new System.Random(11);
        var rockA = new Color(0.38f, 0.38f, 0.39f);
        var rockB = new Color(0.55f, 0.55f, 0.56f);
        float clear = LunarTerrainMesh.PadClearRadius + 4f;
        var rockMatA = VisualMaterials.Lit(rockA, 0.05f, 0.1f);
        var rockMatB = VisualMaterials.Lit(rockB, 0.05f, 0.1f);
        for (int i = 0; i < 80; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float dist = clear + 10f + (float)rng.NextDouble() * (R * 0.92f - clear);
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            float s = 1.3f + (float)rng.NextDouble() * 4.5f;
            float h = SampleApproxHeight(x, z);
            var mat = (float)rng.NextDouble() > 0.5f ? rockMatA : rockMatB;

            var rock = SmoothMesh.MakeSphere($"Boulder_{i}", surface.transform,
                new Vector3(x, h + s * 0.22f, z),
                new Vector3(
                    s * (0.75f + (float)rng.NextDouble() * 0.4f),
                    s * (0.45f + (float)rng.NextDouble() * 0.35f),
                    s * (0.75f + (float)rng.NextDouble() * 0.4f)),
                mat);
            rock.transform.localRotation = Quaternion.Euler(
                (float)rng.NextDouble() * 35f,
                (float)rng.NextDouble() * 360f,
                (float)rng.NextDouble() * 35f);
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

        Color deck = new Color(0.78f, 0.79f, 0.82f);
        Color white = new Color(0.98f, 0.985f, 1f);
        Color mark = new Color(0.95f, 0.96f, 0.98f);
        Color amber = new Color(1f, 0.72f, 0.28f);
        Color baseCol = new Color(0.48f, 0.49f, 0.52f);
        Color metal = new Color(0.62f, 0.63f, 0.66f);
        Color soot = new Color(0.22f, 0.21f, 0.2f);
        Color regolith = new Color(0.56f, 0.55f, 0.54f);

        // ── Реголітна подушка під падом (багатошарова, без «плоского диска») ──
        var bedOuter = VisualMaterials.Lit(new Color(0.5f, 0.5f, 0.5f), 0f, 0.04f);
        var bedMid = VisualMaterials.Lit(regolith, 0f, 0.06f);
        var bedInner = VisualMaterials.Lit(new Color(0.62f, 0.61f, 0.6f), 0f, 0.08f);
        var scorched = VisualMaterials.Lit(soot, 0.15f, 0.12f);

        // Зовнішній насип (м'яко зливається з clear-зоною)
        var berm = SmoothMesh.MakeCylinder("PadBerm", pad.transform,
            new Vector3(0f, -0.18f, 0f), 128f, 0.28f, bedOuter);
        berm.GetComponent<MeshRenderer>().receiveShadows = true;
        SmoothMesh.MakeCylinder("PadBermRing", pad.transform,
            new Vector3(0f, 0.02f, 0f), 124f, 0.06f, bedMid);

        var bed = SmoothMesh.MakeCylinder("PadBed", pad.transform,
            new Vector3(0f, -0.02f, 0f), 112f, 0.14f, bedInner);
        bed.GetComponent<MeshRenderer>().receiveShadows = true;

        // Кільце випаленого реголіту (exhaust plume footprint)
        SmoothMesh.MakeRing("ScorchRing", pad.transform,
            new Vector3(0f, 0.08f, 0f), 96f, 0.72f, scorched);
        SmoothMesh.MakeDisc("ScorchCore", pad.transform,
            new Vector3(0f, 0.09f, 0f), 28f, 0.02f, scorched);

        var baseMat = VisualMaterials.Lit(
            new Color(baseCol.r + 0.08f, baseCol.g + 0.08f, baseCol.b + 0.08f),
            0.22f, 0.42f, baseCol * 0.12f);
        var deckMat = VisualMaterials.Lit(
            new Color(deck.r + 0.05f, deck.g + 0.05f, deck.b + 0.05f),
            0.2f, 0.4f, deck * 0.12f);

        // База + палуба
        var baseGo = SmoothMesh.MakeCylinder("PadBase", pad.transform,
            new Vector3(0f, 0.22f, 0f), 116f, 0.18f, baseMat);
        var br = baseGo.GetComponent<MeshRenderer>();
        br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        br.receiveShadows = true;

        // Фаска / shoulder
        SmoothMesh.MakeCylinder("PadShoulder", pad.transform,
            new Vector3(0f, 0.38f, 0f), 110f, 0.05f,
            VisualMaterials.Lit(new Color(0.55f, 0.56f, 0.59f), 0.35f, 0.5f));

        var deckGo = SmoothMesh.MakeCylinder("PadDeck", pad.transform,
            new Vector3(0f, 0.52f, 0f), 100f, 0.12f, deckMat);
        var dr = deckGo.GetComponent<MeshRenderer>();
        dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        dr.receiveShadows = true;

        // Тонкі сталеві кільця-шви + радіальні сектори
        var seamMat = VisualMaterials.Lit(new Color(0.68f, 0.69f, 0.72f), 0.55f, 0.55f);
        float[] seams = { 90f, 72f, 52f, 34f };
        for (int i = 0; i < seams.Length; i++)
            SmoothMesh.MakeRing($"Seam_{i}", pad.transform,
                new Vector3(0f, 0.65f + i * 0.004f, 0f), seams[i], 0.988f, seamMat);

        // Радіальні шви (8 секторів)
        for (int i = 0; i < 8; i++)
        {
            float a = i * 22.5f;
            var seam = MakeBox(pad.transform, $"RadSeam_{i}",
                new Vector3(0f, 0.66f, 0f), new Vector3(0.35f, 0.03f, 96f), seamMat);
            seam.transform.localRotation = Quaternion.Euler(0f, a, 0f);
        }

        var markMat = VisualMaterials.Unlit(
            new Color(
                Mathf.Clamp01(mark.r * 1.1f + 0.07f),
                Mathf.Clamp01(mark.g * 1.1f + 0.07f),
                Mathf.Clamp01(mark.b * 1.1f + 0.07f)),
            white * 0.85f);

        float[] outer = { 93f, 70f, 48f, 28f, 16f };
        float[] innerRatio = { 0.955f, 0.945f, 0.935f, 0.9f, 0.82f };
        for (int i = 0; i < outer.Length; i++)
        {
            SmoothMesh.MakeRing($"MarkRing_{i}", pad.transform,
                new Vector3(0f, 0.69f + i * 0.008f, 0f), outer[i], innerRatio[i], markMat);
        }

        var whiteMat = VisualMaterials.Unlit(white, white);
        MakeBox(pad.transform, "CrossX", new Vector3(0f, 0.77f, 0f), new Vector3(86f, 0.04f, 1.7f), whiteMat);
        MakeBox(pad.transform, "CrossZ", new Vector3(0f, 0.77f, 0f), new Vector3(1.7f, 0.04f, 86f), whiteMat);

        // Chevron approach markers (4 axes)
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            for (int k = 0; k < 3; k++)
            {
                float d = 48f + k * 8f;
                Vector3 p = new Vector3(Mathf.Sin(a) * d, 0.78f, Mathf.Cos(a) * d);
                var ch = MakeBox(pad.transform, $"Chevron_{i}_{k}", p,
                    new Vector3(4.2f - k * 0.5f, 0.035f, 1.4f), whiteMat);
                ch.transform.localRotation = Quaternion.Euler(0f, i * 90f, 0f);
            }
        }

        // Кутові «T»-маркери
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            float d = 40f;
            Vector3 p = new Vector3(Mathf.Sin(a) * d, 0.78f, Mathf.Cos(a) * d);
            var t1 = MakeBox(pad.transform, $"TMarkA_{i}", p, new Vector3(6f, 0.035f, 1.15f), whiteMat);
            t1.transform.localRotation = Quaternion.Euler(0f, i * 90f + 45f, 0f);
            var t2 = MakeBox(pad.transform, $"TMarkB_{i}",
                p + new Vector3(Mathf.Sin(a) * 2.5f, 0f, Mathf.Cos(a) * 2.5f),
                new Vector3(1.15f, 0.035f, 3.2f), whiteMat);
            t2.transform.localRotation = Quaternion.Euler(0f, i * 90f + 45f, 0f);
        }

        var amberMat = VisualMaterials.Unlit(amber, amber);
        SmoothMesh.MakeDisc("Bullseye", pad.transform, new Vector3(0f, 0.81f, 0f), 10f, 0.03f, amberMat);
        SmoothMesh.MakeRing("BullseyeRing", pad.transform,
            new Vector3(0f, 0.83f, 0f), 12.2f, 0.86f, amberMat);
        SmoothMesh.MakeRing("BullseyeOuter", pad.transform,
            new Vector3(0f, 0.84f, 0f), 14.5f, 0.94f, whiteMat);
        SmoothMesh.MakeDisc("CenterDot", pad.transform, new Vector3(0f, 0.86f, 0f), 3.0f, 0.03f, whiteMat);
        SmoothMesh.MakeRing("PadEdgeRing", pad.transform,
            new Vector3(0f, 0.64f, 0f), 103f, 0.972f, markMat);

        // LED strip ring under edge
        var ledMat = VisualMaterials.Unlit(new Color(0.4f, 0.85f, 1f), new Color(0.3f, 0.7f, 1f));
        SmoothMesh.MakeRing("LedRing", pad.transform,
            new Vector3(0f, 0.58f, 0f), 105f, 0.985f, ledMat);

        // Периметрні панелі + bolt accents
        var panelMat = VisualMaterials.Lit(new Color(0.4f, 0.41f, 0.44f), 0.45f, 0.38f);
        var boltMat = VisualMaterials.Lit(new Color(0.7f, 0.72f, 0.75f), 0.85f, 0.7f);
        for (int i = 0; i < 24; i++)
        {
            float a = i * 15f * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Sin(a) * 56.5f, 0.36f, Mathf.Cos(a) * 56.5f);
            var panel = SmoothMesh.MakeCylinder($"PadPanel_{i}", pad.transform,
                p, 2.6f, 0.2f, panelMat);
            panel.transform.localRotation = Quaternion.Euler(0f, i * 15f, 0f);
            if (i % 3 == 0)
                SmoothMesh.MakeSphere($"Bolt_{i}", pad.transform,
                    p + Vector3.up * 0.35f, Vector3.one * 0.35f, boltMat);
        }

        var poleMat = VisualMaterials.Lit(metal, 0.78f, 0.58f);
        var lampMatW = VisualMaterials.Unlit(white, white);
        var lampMatA = VisualMaterials.Unlit(amber, amber);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Sin(a) * 60f, 0f, Mathf.Cos(a) * 60f);

            SmoothMesh.MakeCylinder($"BeaconPole_{i}", pad.transform,
                p + Vector3.up * 2.2f, 0.7f, 2.2f, poleMat);
            SmoothMesh.MakeCylinder($"BeaconBase_{i}", pad.transform,
                p + Vector3.up * 0.15f, 1.6f, 0.15f, poleMat);

            var lampMat = i % 2 == 0 ? lampMatW : lampMatA;
            SmoothMesh.MakeSphere($"BeaconLamp_{i}", pad.transform,
                p + Vector3.up * 4.7f, Vector3.one * 1.3f, lampMat);
        }

        var fill = new GameObject("PadFill");
        fill.transform.SetParent(pad.transform, false);
        fill.transform.position = new Vector3(0f, 16f, 0f);
        var fl = fill.AddComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(0.96f, 0.96f, 1f);
        fl.intensity = 38f;
        fl.range = 130f;
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
        // Низьке місячне сонце — глибокі контрастні тіні в кратерах
        sun.color = new Color(1f, 0.98f, 0.94f);
        sun.intensity = 3.4f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 1f;
        sun.shadowBias = 0.02f;
        sun.shadowNormalBias = 0.15f;
        sun.shadowNearPlane = 0.5f;
        // ~18° elevation → довгі різкі тіні
        sun.transform.rotation = Quaternion.Euler(18f, -48f, 0f);

        // Майже без fill (вакуум) — тіні не «вимиваються»
        EnsureDir("FillLight", new Color(0.4f, 0.4f, 0.42f), 0.06f, Quaternion.Euler(195f, 55f, 0f));
        EnsureDir("RimLight", new Color(0.35f, 0.36f, 0.4f), 0.05f, Quaternion.Euler(-8f, 155f, 0f));

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.055f);
        RenderSettings.reflectionIntensity = 0.04f;
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
