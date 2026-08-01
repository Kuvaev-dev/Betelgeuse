using UnityEngine;

/// <summary>
/// Космічне середовище: глибоке небо, зорі, туманності, обрій, landing pad.
/// Акуратна композиція без «артефактних» фігур біля pad.
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

        BuildGround(root.transform);
        BuildLandingPad(root.transform);
        BuildDistanceRings(root.transform);
        var starPs = BuildStarField(root.transform);
        BuildSkyScenery(root.transform);
        BuildPadInfrastructure(root.transform);
        BuildApproachLights(root.transform);

        var amb = SpaceAmbience.Ensure();
        amb.Bind(root.transform, starPs, sun);
    }

    static void BuildGround(Transform parent)
    {
        var ground = Prim(PrimitiveType.Plane, "Ground", parent,
            Vector3.zero, new Vector3(500f, 1f, 500f));
        VisualMaterials.Apply(ground, new Color(0.035f, 0.036f, 0.04f), 0.06f, 0.1f);
        NoShadow(ground);

        // Далека площина (не «квадрат-диск» біля pad)
        var far = Prim(PrimitiveType.Plane, "FarPlane", parent,
            new Vector3(0f, -1f, 0f), new Vector3(4000f, 1f, 4000f));
        VisualMaterials.Apply(far, new Color(0.018f, 0.018f, 0.022f), 0.02f, 0.04f);
        NoShadow(far);

        // Легкий рельєф далеко від pad (не біля центру)
        var rng = new System.Random(19);
        for (int i = 0; i < 28; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float d = 180f + (float)rng.NextDouble() * 900f;
            float x = Mathf.Cos(ang) * d;
            float z = Mathf.Sin(ang) * d;
            float s = 25f + (float)rng.NextDouble() * 55f;
            var patch = Prim(PrimitiveType.Cylinder, "Terrain", parent,
                new Vector3(x, -0.35f, z), new Vector3(s, 0.25f, s * (0.7f + (float)rng.NextDouble() * 0.5f)));
            float g = 0.03f + (float)rng.NextDouble() * 0.035f;
            VisualMaterials.Apply(patch, new Color(g, g, g * 1.05f), 0.05f, 0.08f);
            NoShadow(patch);
        }
    }

    static void BuildLandingPad(Transform parent)
    {
        var old = GameObject.Find("LandingPad");
        if (old != null) Object.Destroy(old);

        var pad = new GameObject("LandingPad");
        pad.transform.SetParent(parent, false);

        var apron = Prim(PrimitiveType.Cylinder, "PadApron", pad.transform,
            new Vector3(0f, 0.05f, 0f), new Vector3(88f, 0.1f, 88f));
        VisualMaterials.Apply(apron, new Color(0.08f, 0.08f, 0.09f), 0.3f, 0.32f);

        var deck = Prim(PrimitiveType.Cylinder, "PadDeck", pad.transform,
            new Vector3(0f, 0.2f, 0f), new Vector3(52f, 0.14f, 52f));
        VisualMaterials.Apply(deck, new Color(0.15f, 0.15f, 0.16f), 0.45f, 0.42f);

        var target = Prim(PrimitiveType.Cylinder, "PadTarget", pad.transform,
            new Vector3(0f, 0.32f, 0f), new Vector3(22f, 0.05f, 22f));
        VisualMaterials.Apply(target, new Color(0.2f, 0.2f, 0.22f), 0.35f, 0.48f);
        NoShadow(target);

        float[] rs = { 0.9f, 0.58f, 0.3f, 0.12f };
        for (int i = 0; i < rs.Length; i++)
        {
            float g = 0.5f + i * 0.1f;
            var ring = Prim(PrimitiveType.Cylinder, $"PadRing_{i}", pad.transform,
                new Vector3(0f, 0.38f + i * 0.015f, 0f),
                new Vector3(52f * rs[i], 0.05f, 52f * rs[i]));
            VisualMaterials.Apply(ring, new Color(g, g, g + 0.02f), 0.15f, 0.6f,
                new Color(0.15f, 0.15f, 0.18f) * (0.4f + i * 0.1f));
            NoShadow(ring);
        }

        foreach (var x in new[] { true, false })
        {
            var bar = Prim(PrimitiveType.Cube, x ? "CX" : "CZ", pad.transform,
                new Vector3(0f, 0.4f, 0f),
                x ? new Vector3(44f, 0.05f, 1.05f) : new Vector3(1.05f, 0.05f, 44f));
            VisualMaterials.Apply(bar, new Color(0.88f, 0.88f, 0.9f), 0.15f, 0.55f,
                new Color(0.22f, 0.22f, 0.25f));
            NoShadow(bar);
        }

        var bull = Prim(PrimitiveType.Cylinder, "Bull", pad.transform,
            new Vector3(0f, 0.42f, 0f), new Vector3(3f, 0.04f, 3f));
        VisualMaterials.Apply(bull, new Color(0.92f, 0.92f, 0.94f), 0.2f, 0.55f,
            new Color(0.3f, 0.3f, 0.32f));
        NoShadow(bull);

        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            Vector3 bp = new Vector3(Mathf.Sin(a) * 30f, 0f, Mathf.Cos(a) * 30f);
            var pole = Prim(PrimitiveType.Cylinder, $"Tow_{i}", pad.transform,
                bp + Vector3.up * 6.5f, new Vector3(0.6f, 6.5f, 0.6f));
            VisualMaterials.Apply(pole, new Color(0.32f, 0.32f, 0.34f), 0.55f, 0.4f);
            var head = Prim(PrimitiveType.Sphere, $"TH_{i}", pad.transform,
                bp + Vector3.up * 13.2f, Vector3.one * 1.15f);
            VisualMaterials.Apply(head, new Color(0.92f, 0.93f, 0.95f), 0.12f, 0.75f,
                new Color(0.4f, 0.42f, 0.5f));

            var lg = new GameObject($"Spot_{i}");
            lg.transform.SetParent(pad.transform, false);
            lg.transform.position = bp + Vector3.up * 13f;
            var spot = lg.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(0.95f, 0.96f, 1f);
            spot.intensity = 48f;
            spot.range = 90f;
            spot.spotAngle = 62f;
            spot.shadows = LightShadows.Soft;
            lg.transform.rotation = Quaternion.LookRotation((-bp + Vector3.up * -7f).normalized);
        }

        var fill = new GameObject("PadFill");
        fill.transform.SetParent(pad.transform, false);
        fill.transform.position = new Vector3(0f, 11f, 0f);
        var fl = fill.AddComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(0.8f, 0.85f, 1f);
        fl.intensity = 14f;
        fl.range = 75f;
    }

    static void BuildDistanceRings(Transform parent)
    {
        float[] r = { 120f, 280f, 520f };
        foreach (float radius in r)
        {
            var ring = Prim(PrimitiveType.Cylinder, $"DRing_{radius}", parent,
                new Vector3(0f, 0.04f, 0f), new Vector3(radius * 2f, 0.045f, radius * 2f));
            VisualMaterials.Apply(ring, new Color(0.1f, 0.11f, 0.13f), 0.08f, 0.2f,
                new Color(0.05f, 0.06f, 0.08f));
            NoShadow(ring);
        }
    }

    static ParticleSystem BuildStarField(Transform parent)
    {
        var root = new GameObject("StarField");
        root.transform.SetParent(parent, false);

        var go = new GameObject("Stars");
        go.transform.SetParent(root.transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.duration = 0.5f;
        main.startLifetime = 99999f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 4.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.7f, 0.75f, 0.9f, 0.9f),
            new Color(1f, 0.97f, 0.92f, 1f));
        main.maxParticles = 4000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 3600) });

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 5000f;
        sh.radiusThickness = 0.45f;

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.Particle(Color.white);
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var rng = new System.Random(5);
        for (int i = 0; i < 70; i++)
        {
            Vector3 dir = RandSphere(rng);
            if (dir.y < 0.08f) dir.y = Mathf.Abs(dir.y) + 0.2f;
            dir.Normalize();
            float dist = 3000f + (float)rng.NextDouble() * 1600f;
            float s = 1.8f + (float)rng.NextDouble() * 5f;
            var star = Prim(PrimitiveType.Sphere, "BStar", root.transform, dir * dist, Vector3.one * s);
            Color c = Color.Lerp(new Color(0.75f, 0.82f, 1f), new Color(1f, 0.92f, 0.8f), (float)rng.NextDouble());
            if (i % 19 == 0) c = new Color(1f, 0.55f, 0.4f);
            VisualMaterials.Apply(star, c, 0f, 0f, c * (1.1f + (float)rng.NextDouble() * 0.5f));
            NoShadow(star);
        }

        return ps;
    }

    /// <summary>Туманності, Чумацький Шлях, атмосферний обрій — далеко від pad.</summary>
    static void BuildSkyScenery(Transform parent)
    {
        var sky = new GameObject("SkyScenery");
        sky.transform.SetParent(parent, false);
        var rng = new System.Random(21);

        // Туманності (далеко, м'які)
        Color[] nebCols =
        {
            new Color(0.2f, 0.08f, 0.35f),
            new Color(0.06f, 0.12f, 0.32f),
            new Color(0.15f, 0.05f, 0.22f),
            new Color(0.08f, 0.16f, 0.28f),
            new Color(0.18f, 0.1f, 0.3f),
            new Color(0.05f, 0.1f, 0.25f)
        };
        for (int i = 0; i < nebCols.Length; i++)
        {
            Vector3 dir = RandSphere(rng);
            dir.y = Mathf.Abs(dir.y) * 0.55f + 0.2f;
            dir.Normalize();
            float dist = 3400f + (float)rng.NextDouble() * 1000f;
            float sx = 400f + (float)rng.NextDouble() * 500f;
            float sy = 280f + (float)rng.NextDouble() * 350f;
            var neb = Prim(PrimitiveType.Sphere, $"Nebula_{i}", sky.transform,
                dir * dist, new Vector3(sx, sy, sx * 0.85f));
            float b = 0.1f + (float)rng.NextDouble() * 0.1f;
            VisualMaterials.Apply(neb, nebCols[i] * 0.3f, 0f, 0f, nebCols[i] * b);
            NoShadow(neb);
        }

        // Смуга Чумацького Шляху
        var mw = Prim(PrimitiveType.Sphere, "MilkyWay", sky.transform,
            new Vector3(400f, 700f, 3800f), new Vector3(5200f, 320f, 900f));
        VisualMaterials.Apply(mw, new Color(0.12f, 0.12f, 0.18f) * 0.35f, 0f, 0f,
            new Color(0.16f, 0.17f, 0.28f) * 0.22f);
        NoShadow(mw);

        var mw2 = Prim(PrimitiveType.Sphere, "MilkyWay2", sky.transform,
            new Vector3(-600f, 500f, -3600f), new Vector3(4200f, 260f, 750f));
        VisualMaterials.Apply(mw2, new Color(0.1f, 0.09f, 0.16f) * 0.3f, 0f, 0f,
            new Color(0.18f, 0.12f, 0.28f) * 0.15f);
        NoShadow(mw2);

        // Атмосферний обрій — дуже тонкий і низький, не «стіна»
        var limb = Prim(PrimitiveType.Cylinder, "AtmoLimb", sky.transform,
            new Vector3(0f, 8f, 0f), new Vector3(9000f, 12f, 9000f));
        VisualMaterials.Apply(limb, new Color(0.04f, 0.06f, 0.12f), 0f, 0f,
            new Color(0.1f, 0.18f, 0.4f) * 0.2f);
        NoShadow(limb);

        // Далекий «сонце-диск»
        var sunDisc = Prim(PrimitiveType.Sphere, "SunDisc", sky.transform,
            new Vector3(-3200f, 1800f, -2500f), Vector3.one * 140f);
        VisualMaterials.Apply(sunDisc, new Color(1f, 0.96f, 0.88f), 0f, 0f,
            new Color(1f, 0.9f, 0.65f) * 1.8f);
        NoShadow(sunDisc);
    }

    static void BuildPadInfrastructure(Transform parent)
    {
        var tower = Prim(PrimitiveType.Cylinder, "RefTower", parent,
            new Vector3(85f, 40f, 85f), new Vector3(1.0f, 80f, 1.0f));
        VisualMaterials.Apply(tower, new Color(0.36f, 0.36f, 0.39f), 0.5f, 0.4f);

        float[] marks = { 15f, 35f, 55f, 75f };
        foreach (float h in marks)
        {
            var m = Prim(PrimitiveType.Cube, "TMark", parent,
                new Vector3(85f, h, 85f), new Vector3(3.2f, 0.4f, 3.2f));
            VisualMaterials.Apply(m, new Color(0.85f, 0.75f, 0.45f), 0.2f, 0.5f,
                new Color(0.3f, 0.2f, 0.05f));
            NoShadow(m);
        }

        var b = new GameObject("TowerBeacon");
        b.transform.SetParent(parent, false);
        b.transform.position = new Vector3(85f, 82f, 85f);
        var bl = b.AddComponent<Light>();
        bl.type = LightType.Point;
        bl.color = new Color(1f, 0.75f, 0.35f);
        bl.intensity = 8f;
        bl.range = 55f;
    }

    static void BuildApproachLights(Transform parent)
    {
        for (int i = 1; i <= 12; i++)
        {
            float z = i * 38f;
            foreach (float x in new[] { -17f, 17f })
            {
                var lamp = Prim(PrimitiveType.Sphere, "AppLight", parent,
                    new Vector3(x, 0.5f, z), Vector3.one * 0.6f);
                Color c = i % 4 == 0 ? new Color(0.95f, 0.45f, 0.3f) : new Color(0.8f, 0.85f, 0.95f);
                VisualMaterials.Apply(lamp, c * 0.5f, 0.1f, 0.65f, c * 0.7f);
                NoShadow(lamp);
            }
        }
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
        sun.color = new Color(1f, 0.97f, 0.93f);
        sun.intensity = 1.4f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.8f;
        sun.transform.rotation = Quaternion.Euler(32f, -45f, 0f);

        EnsureDir("FillLight", new Color(0.4f, 0.45f, 0.6f), 0.3f, Quaternion.Euler(195f, 55f, 0f));
        EnsureDir("RimLight", new Color(0.55f, 0.6f, 0.75f), 0.25f, Quaternion.Euler(-18f, 145f, 0f));

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.07f, 0.07f, 0.1f);
        RenderSettings.ambientEquatorColor = new Color(0.045f, 0.045f, 0.055f);
        RenderSettings.ambientGroundColor = new Color(0.025f, 0.025f, 0.03f);
        RenderSettings.reflectionIntensity = 0.28f;
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
        RenderSettings.fogColor = new Color(0.04f, 0.04f, 0.055f);
        RenderSettings.fogDensity = 0.000045f;

        var cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.03f, 0.04f);
            cam.farClipPlane = 14000f;
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

    static Vector3 RandSphere(System.Random rng)
    {
        float u = (float)rng.NextDouble();
        float v = (float)rng.NextDouble();
        float th = 2f * Mathf.PI * u;
        float ph = Mathf.Acos(2f * v - 1f);
        return new Vector3(Mathf.Sin(ph) * Mathf.Cos(th), Mathf.Cos(ph), Mathf.Sin(ph) * Mathf.Sin(th));
    }
}
