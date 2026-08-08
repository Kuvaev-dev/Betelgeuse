using UnityEngine;

/// <summary>
/// Середовище посадки: суцільний heightmap-Місяць + industrial pad.
/// Рельєф/кратери — один меш (LunarTerrainMesh), не стопка циліндрів.
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
            new Color(0.65f, 0.65f, 0.65f),
            metallic: 0.0f,
            smooth: 0.04f);

        // Cratered disk — єдина видима поверхня (без сірого «обідка» зовні)
        float R = LunarTerrainMesh.TerrainRadius;
        LunarTerrainMesh.Create(surface.transform, regolith,
            resolution: 300, radius: R);

        // Підкладка трохи менша за crater-диск (world radius ≈ 0.99·R).
        // MakeCylinder: scale.x = diameter, mesh r=0.5 → worldR = diameter/2.
        var farMat = VisualMaterials.Lit(new Color(0.36f, 0.36f, 0.37f), 0.0f, 0.02f);
        float underDiameter = R * 2f * 0.99f; // ≈ 2R, не виступає за край
        var far = SmoothMesh.MakeCylinder("HorizonDisk", surface.transform,
            new Vector3(0f, -2.8f, 0f), underDiameter, 2.2f, farMat);
        var fr = far.GetComponent<MeshRenderer>();
        if (fr != null)
        {
            fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fr.receiveShadows = true;
        }

        // Валуни по всьому диску (включно з краєм)
        var rng = new System.Random(11);
        var rockA = new Color(0.38f, 0.38f, 0.39f);
        var rockB = new Color(0.55f, 0.55f, 0.56f);
        float clear = LunarTerrainMesh.PadClearRadius + 4f;
        for (int i = 0; i < 70; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float dist = clear + 10f + (float)rng.NextDouble() * (R * 0.92f - clear);
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            float s = 1.3f + (float)rng.NextDouble() * 4.5f;
            float h = SampleApproxHeight(x, z);

            var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = $"Boulder_{i}";
            rock.transform.SetParent(surface.transform, false);
            rock.transform.localPosition = new Vector3(x, h + s * 0.22f, z);
            rock.transform.localScale = new Vector3(
                s * (0.75f + (float)rng.NextDouble() * 0.4f),
                s * (0.45f + (float)rng.NextDouble() * 0.35f),
                s * (0.75f + (float)rng.NextDouble() * 0.4f));
            rock.transform.localRotation = Quaternion.Euler(
                (float)rng.NextDouble() * 35f,
                (float)rng.NextDouble() * 360f,
                (float)rng.NextDouble() * 35f);
            Object.Destroy(rock.GetComponent<Collider>());
            VisualMaterials.Apply(rock, Color.Lerp(rockA, rockB, (float)rng.NextDouble()), 0.05f, 0.1f);
            var rr = rock.GetComponent<MeshRenderer>();
            if (rr != null)
            {
                rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                rr.receiveShadows = true;
            }
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

        Color deck = new Color(0.74f, 0.75f, 0.78f);
        Color white = new Color(0.98f, 0.98f, 1f);
        Color mark = new Color(0.94f, 0.95f, 0.97f);
        Color amber = new Color(1f, 0.7f, 0.22f);
        Color baseCol = new Color(0.52f, 0.53f, 0.56f);
        Color metal = new Color(0.58f, 0.59f, 0.62f);

        // Сіра подушка реголіту під палубою (на рівній clear-зоні, y≈0)
        var bedMat = VisualMaterials.Lit(new Color(0.58f, 0.58f, 0.58f), 0.0f, 0.08f);
        var bed = SmoothMesh.MakeCylinder("PadBed", pad.transform,
            new Vector3(0f, -0.06f, 0f), 108f, 0.12f, bedMat);
        bed.GetComponent<MeshRenderer>().receiveShadows = true;

        var baseMat = VisualMaterials.Lit(
            new Color(baseCol.r + 0.06f, baseCol.g + 0.06f, baseCol.b + 0.06f),
            0.2f, 0.4f, baseCol * 0.15f);
        var deckMat = VisualMaterials.Lit(
            new Color(deck.r + 0.06f, deck.g + 0.06f, deck.b + 0.06f),
            0.18f, 0.38f, deck * 0.15f);

        var baseGo = SmoothMesh.MakeCylinder("PadBase", pad.transform,
            new Vector3(0f, 0.22f, 0f), 118f, 0.2f, baseMat);
        var br = baseGo.GetComponent<MeshRenderer>();
        br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        br.receiveShadows = true;

        var deckGo = SmoothMesh.MakeCylinder("PadDeck", pad.transform,
            new Vector3(0f, 0.5f, 0f), 102f, 0.14f, deckMat);
        var dr = deckGo.GetComponent<MeshRenderer>();
        dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        dr.receiveShadows = true;

        var markMat = VisualMaterials.Unlit(
            new Color(
                Mathf.Clamp01(mark.r * 1.1f + 0.08f),
                Mathf.Clamp01(mark.g * 1.1f + 0.08f),
                Mathf.Clamp01(mark.b * 1.1f + 0.08f)),
            white * 0.85f);

        float[] outer = { 94f, 68f, 44f, 22f };
        float[] innerRatio = { 0.945f, 0.935f, 0.925f, 0.88f };
        for (int i = 0; i < outer.Length; i++)
        {
            SmoothMesh.MakeRing($"MarkRing_{i}", pad.transform,
                new Vector3(0f, 0.66f + i * 0.01f, 0f), outer[i], innerRatio[i], markMat);
        }

        var whiteMat = VisualMaterials.Unlit(white, white);
        MakeBox(pad.transform, "CrossX", new Vector3(0f, 0.74f, 0f), new Vector3(86f, 0.05f, 2.1f), whiteMat);
        MakeBox(pad.transform, "CrossZ", new Vector3(0f, 0.74f, 0f), new Vector3(2.1f, 0.05f, 86f), whiteMat);

        var amberMat = VisualMaterials.Unlit(amber, amber);
        SmoothMesh.MakeDisc("Bullseye", pad.transform, new Vector3(0f, 0.78f, 0f), 10f, 0.03f, amberMat);
        SmoothMesh.MakeDisc("CenterDot", pad.transform, new Vector3(0f, 0.81f, 0f), 3.4f, 0.03f, whiteMat);
        SmoothMesh.MakeRing("PadEdgeRing", pad.transform,
            new Vector3(0f, 0.63f, 0f), 104f, 0.968f, markMat);

        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Sin(a) * 60f, 0f, Mathf.Cos(a) * 60f);

            var poleMat = VisualMaterials.Lit(metal, 0.75f, 0.55f);
            SmoothMesh.MakeCylinder($"BeaconPole_{i}", pad.transform,
                p + Vector3.up * 2.2f, 0.75f, 2.2f, poleMat);

            Color lc = i % 2 == 0 ? white : amber;
            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = $"BeaconLamp_{i}";
            lamp.transform.SetParent(pad.transform, false);
            lamp.transform.localPosition = p + Vector3.up * 4.7f;
            lamp.transform.localScale = Vector3.one * 1.35f;
            Object.Destroy(lamp.GetComponent<Collider>());
            VisualMaterials.ApplyUnlit(lamp, lc, lc);
        }

        var fill = new GameObject("PadFill");
        fill.transform.SetParent(pad.transform, false);
        fill.transform.position = new Vector3(0f, 18f, 0f);
        var fl = fill.AddComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(0.95f, 0.95f, 1f);
        fl.intensity = 42f;
        fl.range = 140f;
    }

    static void BuildApproachLights(Transform parent)
    {
        for (int i = 1; i <= 12; i++)
        {
            float z = 90f + i * 42f;
            foreach (float x in new[] { -22f, 22f })
            {
                var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lamp.name = "AppLight";
                lamp.transform.SetParent(parent, false);
                // Трохи над рельєфом
                lamp.transform.localPosition = new Vector3(x, 0.8f, z);
                lamp.transform.localScale = Vector3.one * 1.1f;
                Object.Destroy(lamp.GetComponent<Collider>());
                Color c = i % 3 == 0
                    ? new Color(1f, 0.55f, 0.2f)
                    : new Color(0.92f, 0.93f, 0.96f);
                VisualMaterials.ApplyUnlit(lamp, c, c);
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
        var sunDisc = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sunDisc.name = "SunDisc";
        sunDisc.transform.SetParent(sky.transform, false);
        sunDisc.transform.localPosition = new Vector3(-2600f, 1700f, -1900f);
        sunDisc.transform.localScale = Vector3.one * 100f;
        Object.Destroy(sunDisc.GetComponent<Collider>());
        VisualMaterials.ApplyUnlit(sunDisc, new Color(1f, 0.98f, 0.94f), new Color(1f, 0.95f, 0.85f));
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

    static void MakeBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
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
    }
}
