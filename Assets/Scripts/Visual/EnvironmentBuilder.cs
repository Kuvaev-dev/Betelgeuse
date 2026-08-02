using UnityEngine;

/// <summary>
/// Посадка на Місяць: реголіт + кратери + чистий industrial pad
/// (без темних блоків на білій поверхні).
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
        BuildSubtleHorizon(root.transform);
        BuildApproachCorridor(root.transform);

        var amb = SpaceAmbience.Ensure();
        amb.Bind(root.transform, starPs, sun);
    }

    /// <summary>
    /// Круглий місячний диск (Cylinder, не Plane/квадрат):
    /// концентричні кільця реголіту → м’який край, кратери, каміння.
    /// </summary>
    static void BuildLunarSurface(Transform parent)
    {
        var surface = new GameObject("LunarSurface");
        surface.transform.SetParent(parent, false);

        Color regolith = new Color(0.48f, 0.46f, 0.43f);
        Color regolithMid = new Color(0.38f, 0.36f, 0.34f);
        Color regolithDark = new Color(0.26f, 0.25f, 0.24f);
        Color regolithLight = new Color(0.58f, 0.56f, 0.52f);
        Color regolithEdge = new Color(0.18f, 0.17f, 0.16f);

        // ── Концентричні КРУГЛІ диски (scale.x/z = діаметр) ──
        // Зовнішній «горизонт» місяця
        var moonFar = Prim(PrimitiveType.Cylinder, "MoonDisk_Far", surface.transform,
            new Vector3(0f, -0.8f, 0f), new Vector3(4200f, 0.35f, 4200f));
        VisualMaterials.Apply(moonFar, regolithEdge, 0.03f, 0.06f);
        NoShadow(moonFar);

        // Середнє кільце
        var moonMid = Prim(PrimitiveType.Cylinder, "MoonDisk_Mid", surface.transform,
            new Vector3(0f, -0.35f, 0f), new Vector3(2400f, 0.4f, 2400f));
        VisualMaterials.Apply(moonMid, regolithDark, 0.04f, 0.08f);
        NoShadow(moonMid);

        // Основна поверхня навколо pad
        var moonMain = Prim(PrimitiveType.Cylinder, "MoonDisk_Main", surface.transform,
            new Vector3(0f, -0.05f, 0f), new Vector3(1400f, 0.45f, 1400f));
        VisualMaterials.Apply(moonMain, regolithMid, 0.05f, 0.12f);

        // Ближнє поле (трохи світліше — пил біля pad)
        var moonNear = Prim(PrimitiveType.Cylinder, "MoonDisk_Near", surface.transform,
            new Vector3(0f, 0.02f, 0f), new Vector3(420f, 0.22f, 420f));
        VisualMaterials.Apply(moonNear, regolith, 0.06f, 0.14f);

        // М’який обід ближнього поля (перехід)
        var moonHalo = Prim(PrimitiveType.Cylinder, "MoonDisk_Halo", surface.transform,
            new Vector3(0f, 0.01f, 0f), new Vector3(520f, 0.12f, 520f));
        VisualMaterials.Apply(moonHalo, Color.Lerp(regolith, regolithMid, 0.5f), 0.05f, 0.1f);
        NoShadow(moonHalo);

        var rng = new System.Random(42);

        // Кратери — лише на круглому диску (dist < 1050)
        for (int i = 0; i < 28; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float dist = 160f + (float)rng.NextDouble() * 900f;
            if (dist > 1050f) continue;
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            float r = 10f + (float)rng.NextDouble() * 42f;

            var floor = Prim(PrimitiveType.Cylinder, $"CraterFloor_{i}", surface.transform,
                new Vector3(x, -0.12f, z), new Vector3(r * 1.55f, 0.14f, r * 1.55f));
            VisualMaterials.Apply(floor, Color.Lerp(regolithDark, regolithMid, 0.3f), 0.04f, 0.1f);
            NoShadow(floor);

            var rim = Prim(PrimitiveType.Cylinder, $"CraterRim_{i}", surface.transform,
                new Vector3(x, 0.1f, z), new Vector3(r * 2.0f, 0.2f, r * 2.0f));
            VisualMaterials.Apply(rim, Color.Lerp(regolith, regolithLight, 0.45f), 0.06f, 0.16f);
            NoShadow(rim);

            // Іноді яскравий внутрішній вал
            if (i % 3 == 0)
            {
                var inner = Prim(PrimitiveType.Cylinder, $"CraterInner_{i}", surface.transform,
                    new Vector3(x, 0.02f, z), new Vector3(r * 1.15f, 0.08f, r * 1.15f));
                VisualMaterials.Apply(inner, Color.Lerp(regolithMid, regolithLight, 0.2f), 0.05f, 0.12f);
                NoShadow(inner);
            }
        }

        // Каміння
        for (int i = 0; i < 70; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float dist = 120f + (float)rng.NextDouble() * 850f;
            if (dist > 1100f) continue;
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            float s = 1.0f + (float)rng.NextDouble() * 5.0f;
            float h = s * (0.35f + (float)rng.NextDouble() * 0.55f);

            var rock = Prim(PrimitiveType.Sphere, $"Rock_{i}", surface.transform,
                new Vector3(x, h * 0.32f, z),
                new Vector3(s, h, s * (0.65f + (float)rng.NextDouble() * 0.55f)));
            Color rc = Color.Lerp(regolithDark, regolithLight, (float)rng.NextDouble());
            VisualMaterials.Apply(rock, rc, 0.1f, 0.2f);
            NoShadow(rock);
        }

        // Низькі круглі пагорби реголіту
        for (int i = 0; i < 24; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float dist = 200f + (float)rng.NextDouble() * 750f;
            if (dist > 1000f) continue;
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            float d = 35f + (float)rng.NextDouble() * 70f;

            var hill = Prim(PrimitiveType.Cylinder, $"Hill_{i}", surface.transform,
                new Vector3(x, 0.05f, z),
                new Vector3(d, 0.4f + (float)rng.NextDouble() * 0.7f, d));
            VisualMaterials.Apply(hill, Color.Lerp(regolith, regolithLight, 0.3f), 0.05f, 0.14f);
            NoShadow(hill);
        }

        // Кільцеві «хвилі» пилу навколо pad (естетика)
        float[] dustR = { 160f, 220f, 300f, 380f };
        for (int i = 0; i < dustR.Length; i++)
        {
            var dust = Prim(PrimitiveType.Cylinder, $"DustRing_{i}", surface.transform,
                new Vector3(0f, 0.04f + i * 0.015f, 0f),
                new Vector3(dustR[i] * 2f, 0.08f, dustR[i] * 2f));
            Color dc = Color.Lerp(regolith, regolithLight, 0.15f + i * 0.08f);
            VisualMaterials.Apply(dust, dc, 0.04f, 0.1f);
            NoShadow(dust);
        }
    }

    /// <summary>
    /// Чистий landing pad: світла палуба + білий хрест + тонкі кільця.
    /// Жодних темних кубів/блоків на білій поверхні.
    /// </summary>
    static void BuildLandingPad(Transform parent)
    {
        var old = GameObject.Find("LandingPad");
        if (old != null) Object.Destroy(old);

        var pad = new GameObject("LandingPad");
        pad.transform.SetParent(parent, false);

        Color deckCol = new Color(0.72f, 0.73f, 0.76f);   // світлий метал/бетон
        Color white = new Color(0.97f, 0.97f, 0.99f);
        Color mark = new Color(0.92f, 0.93f, 0.95f);
        Color amber = new Color(1f, 0.72f, 0.28f);
        Color edge = new Color(0.5f, 0.52f, 0.56f);

        // Підкладка — злегка піднята над реголітом
        var basePlate = Prim(PrimitiveType.Cylinder, "PadBase", pad.transform,
            new Vector3(0f, 0.25f, 0f), new Vector3(130f, 0.28f, 130f));
        VisualMaterials.ApplyBright(basePlate, edge);

        // Головна палуба (єдиний світлий диск — без «дір»)
        var deck = Prim(PrimitiveType.Cylinder, "PadDeck", pad.transform,
            new Vector3(0f, 0.55f, 0f), new Vector3(110f, 0.22f, 110f));
        VisualMaterials.ApplyBright(deck, deckCol);

        // Тонкі білі кільця-маркери (підняті над палубою, solid light)
        // Лише обід: використовуємо дуже тонкий по висоті диск світліший за deck
        float[] ringD = { 100f, 72f, 44f, 22f };
        for (int i = 0; i < ringD.Length; i++)
        {
            var ring = Prim(PrimitiveType.Cylinder, $"Ring_{i}", pad.transform,
                new Vector3(0f, 0.72f + i * 0.03f, 0f),
                new Vector3(ringD[i], 0.06f, ringD[i]));
            // Світліше за deck — виглядає як намальоване кільце/зона
            VisualMaterials.ApplyUnlit(ring, mark, white * 0.85f);
        }

        // Білий прицільний хрест (тонкий, чистий)
        var cx = Prim(PrimitiveType.Cube, "CrossX", pad.transform,
            new Vector3(0f, 0.85f, 0f), new Vector3(96f, 0.1f, 3.5f));
        VisualMaterials.ApplyUnlit(cx, white, white);

        var cz = Prim(PrimitiveType.Cube, "CrossZ", pad.transform,
            new Vector3(0f, 0.85f, 0f), new Vector3(3.5f, 0.1f, 96f));
        VisualMaterials.ApplyUnlit(cz, white, white);

        // Центр — amber + біла точка (без зайвих шарів)
        var bull = Prim(PrimitiveType.Cylinder, "Bullseye", pad.transform,
            new Vector3(0f, 0.92f, 0f), new Vector3(10f, 0.08f, 10f));
        VisualMaterials.ApplyUnlit(bull, amber, amber);

        var center = Prim(PrimitiveType.Cylinder, "CenterDot", pad.transform,
            new Vector3(0f, 0.98f, 0f), new Vector3(3.5f, 0.06f, 3.5f));
        VisualMaterials.ApplyUnlit(center, white, white);

        // Маяки ПОЗА палубою (не на білому колі)
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            // 72 м — за краєм палуби 55 м radius
            Vector3 p = new Vector3(Mathf.Sin(a) * 72f, 0f, Mathf.Cos(a) * 72f);

            var pole = Prim(PrimitiveType.Cylinder, $"BeaconPole_{i}", pad.transform,
                p + Vector3.up * 3f, new Vector3(1.2f, 3f, 1.2f));
            VisualMaterials.ApplyBright(pole, new Color(0.45f, 0.46f, 0.5f));

            var lamp = Prim(PrimitiveType.Sphere, $"BeaconLamp_{i}", pad.transform,
                p + Vector3.up * 6.2f, Vector3.one * 2.2f);
            VisualMaterials.ApplyUnlit(lamp, white, white);

            var spotGo = new GameObject($"PadSpot_{i}");
            spotGo.transform.SetParent(pad.transform, false);
            spotGo.transform.position = p + Vector3.up * 6.5f;
            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(0.95f, 0.96f, 1f);
            spot.intensity = 100f;
            spot.range = 160f;
            spot.spotAngle = 70f;
            spot.shadows = LightShadows.None;
            spotGo.transform.rotation = Quaternion.LookRotation(
                (Vector3.zero - p + Vector3.down * 8f).normalized);
        }

        // М'яке заповнення світлом над pad
        var fill = new GameObject("PadFill");
        fill.transform.SetParent(pad.transform, false);
        fill.transform.position = new Vector3(0f, 25f, 0f);
        var fl = fill.AddComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(0.95f, 0.95f, 1f);
        fl.intensity = 50f;
        fl.range = 160f;
    }

    static void BuildApproachCorridor(Transform parent)
    {
        // Низькі вогні на реголіті — підхід до pad
        for (int i = 1; i <= 14; i++)
        {
            float z = 70f + i * 40f;
            foreach (float x in new[] { -24f, 24f })
            {
                var lamp = Prim(PrimitiveType.Sphere, "AppLight", parent,
                    new Vector3(x, 0.6f, z), Vector3.one * 1.3f);
                Color c = i % 4 == 0
                    ? new Color(1f, 0.55f, 0.22f)
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
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 2.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.87f, 0.95f, 0.9f), Color.white);
        main.maxParticles = 3000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 2700) });

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

    static void BuildSubtleHorizon(Transform parent)
    {
        var sky = new GameObject("SkyMinimal");
        sky.transform.SetParent(parent, false);

        // Сонце (різке, як на Місяці — без атмосфери)
        Vector3 sunPos = new Vector3(-2400f, 1600f, -1800f);
        var sunDisc = Prim(PrimitiveType.Sphere, "SunDisc", sky.transform,
            sunPos, Vector3.one * 90f);
        VisualMaterials.ApplyUnlit(sunDisc, new Color(1f, 0.98f, 0.94f),
            new Color(1f, 0.95f, 0.85f));
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
        // Жорстке місячне світло
        sun.color = new Color(1f, 0.98f, 0.95f);
        sun.intensity = 1.85f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.9f;
        sun.transform.rotation = Quaternion.Euler(38f, -28f, 0f);

        // Слабкий fill (відбиття від реголіту)
        EnsureDir("FillLight", new Color(0.45f, 0.48f, 0.55f), 0.28f, Quaternion.Euler(195f, 50f, 0f));
        EnsureDir("RimLight", new Color(0.5f, 0.5f, 0.55f), 0.18f, Quaternion.Euler(-8f, 150f, 0f));

        // Місячний ambient — сірий, не чорний
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.18f, 0.18f, 0.2f);
        RenderSettings.reflectionIntensity = 0.2f;
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
        // На Місяці туману майже немає
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.025f);
        RenderSettings.fogDensity = 0.000012f;

        var cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.012f);
            cam.farClipPlane = 16000f;
            cam.nearClipPlane = 0.3f;
        }
    }

    static GameObject Prim(PrimitiveType t, string n, Transform p, Vector3 pos, Vector3 sc)
    {
        var go = GameObject.CreatePrimitive(t);
        go.name = n;
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localScale = sc;
        var c = go.GetComponent<Collider>();
        if (c != null) Object.Destroy(c);
        return go;
    }

    static void NoShadow(GameObject go)
    {
        var r = go.GetComponent<MeshRenderer>();
        if (r == null) return;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }
}
