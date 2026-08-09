using UnityEngine;

/// <summary>
/// Процедурна модель 1-го ступеня ~42 м (Falcon-class).
/// Корпус з деталями, ноги, octaweb, multi-layer engine FX.
/// </summary>
public static class RocketVisualBuilder
{
    public const float Height = 42f;
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

        var white = VisualMaterials.Lit(new Color(0.97f, 0.975f, 0.985f), 0.08f, 0.88f);
        var whiteMatte = VisualMaterials.Lit(new Color(0.91f, 0.92f, 0.94f), 0.05f, 0.45f);
        var black = VisualMaterials.Lit(new Color(0.05f, 0.055f, 0.06f), 0.58f, 0.36f);
        var soot = VisualMaterials.Lit(new Color(0.11f, 0.10f, 0.09f), 0.38f, 0.22f);
        var metal = VisualMaterials.Lit(new Color(0.72f, 0.74f, 0.78f), 0.94f, 0.84f);
        var titanium = VisualMaterials.Lit(new Color(0.58f, 0.60f, 0.64f), 0.90f, 0.72f);
        var carbon = VisualMaterials.Lit(new Color(0.08f, 0.085f, 0.09f), 0.48f, 0.50f);
        var silver = VisualMaterials.Lit(new Color(0.86f, 0.88f, 0.92f), 0.92f, 0.80f);
        var heat = VisualMaterials.Lit(new Color(0.13f, 0.11f, 0.10f), 0.82f, 0.24f);
        var copper = VisualMaterials.Lit(new Color(0.55f, 0.40f, 0.30f), 0.92f, 0.58f);
        var darkMetal = VisualMaterials.Lit(new Color(0.18f, 0.19f, 0.21f), 0.90f, 0.52f);
        var stripe = VisualMaterials.Lit(new Color(0.07f, 0.07f, 0.08f), 0.42f, 0.30f);
        var gold = VisualMaterials.Lit(new Color(0.72f, 0.58f, 0.28f), 0.85f, 0.65f);
        var accent = VisualMaterials.Lit(new Color(0.18f, 0.72f, 0.92f), 0.28f, 0.68f,
            new Color(0.08f, 0.42f, 0.62f) * 0.45f);

        // ── Aft / octaweb (чистіший «двигунний» блок) ──
        SmoothCyl("Octaweb", visual.transform, 0.95f, Radius * 2.45f, 1.05f, black);
        SmoothCyl("OctawebLip", visual.transform, 0.35f, Radius * 2.52f, 0.05f, titanium);
        SmoothCyl("AftSkirt", visual.transform, 3.15f, Radius * 2.2f, 0.95f, carbon);
        for (int i = 0; i < 5; i++)
            SmoothCyl($"AftRing_{i}", visual.transform, 1.85f + i * 0.38f, Radius * 2.28f, 0.025f, titanium);

        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Prim(PrimitiveType.Cube, $"OctRib_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * (Radius * 0.98f), 1.0f, Mathf.Cos(a) * (Radius * 0.98f)),
                new Vector3(0.07f, 0.9f, 0.5f), darkMetal);
        }

        for (int i = 0; i < 18; i++)
        {
            float a = i * 20f * Mathf.Deg2Rad;
            float r = Radius + 0.06f;
            Prim(PrimitiveType.Cube, $"Stringer_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * r, 3.2f, Mathf.Cos(a) * r),
                new Vector3(0.04f, 1.45f, 0.08f), darkMetal);
        }

        // ── Тіло: суцільний білий стек + чіткі шви ──
        SmoothCyl("LowerTank", visual.transform, 8.9f, Radius * 2.0f, 4.85f, white);
        SmoothCyl("CommonDome", visual.transform, 14.15f, Radius * 2.07f, 0.28f, silver);
        SmoothCyl("Stripe1", visual.transform, 14.65f, Radius * 2.1f, 0.09f, stripe);
        SmoothCyl("MidTank", visual.transform, 21.85f, Radius * 2.0f, 7.05f, white);
        SmoothCyl("Stripe2", visual.transform, 29.25f, Radius * 2.1f, 0.09f, stripe);
        SmoothCyl("UpperTank", visual.transform, 33.55f, Radius * 1.99f, 4.0f, white);
        SmoothCyl("Interstage", visual.transform, 37.85f, Radius * 1.82f, 0.65f, carbon);
        SmoothCyl("InterstageRing", visual.transform, 38.45f, Radius * 1.9f, 0.055f, titanium);

        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            float r = Radius * 0.93f;
            SmoothCylAt($"Vent_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * r, 37.55f, Mathf.Cos(a) * r), 0.2f, 0.1f, darkMetal);
        }

        for (int i = 0; i < 11; i++)
            SmoothCyl($"Ring_{i}", visual.transform, 5.2f + i * 3.2f, Radius * 2.05f, 0.02f, silver);

        // Теплові плями (м'якші, асиметричні)
        SmoothCyl("HeatStain", visual.transform, 5.35f, Radius * 2.08f, 0.5f, soot);
        SmoothCyl("HeatStain2", visual.transform, 6.2f, Radius * 2.04f, 0.18f,
            VisualMaterials.Lit(new Color(0.16f, 0.13f, 0.11f), 0.45f, 0.24f));
        Prim(PrimitiveType.Cube, "BurnStreak", visual.transform,
            new Vector3(0.2f, 7.8f, Radius + 0.015f), new Vector3(0.95f, 2.4f, 0.04f),
            VisualMaterials.Lit(new Color(0.18f, 0.13f, 0.11f), 0.32f, 0.18f));

        // Ніс — плавний конус
        SmoothSphere("Nose", visual.transform,
            new Vector3(0f, 39.55f, 0f), new Vector3(Radius * 1.95f, 3.55f, Radius * 1.95f), white);
        SmoothSphere("NoseMid", visual.transform,
            new Vector3(0f, 40.85f, 0f), new Vector3(Radius * 1.05f, 1.05f, Radius * 1.05f), whiteMatte);
        SmoothSphere("Tip", visual.transform,
            new Vector3(0f, 41.5f, 0f), new Vector3(Radius * 0.42f, 0.62f, Radius * 0.42f), metal);
        SmoothCyl("TipSpike", visual.transform, 41.95f, 0.09f, 0.18f, titanium);

        // Декалі / raceway
        Prim(PrimitiveType.Cube, "Decal", visual.transform,
            new Vector3(0f, 24.8f, Radius + 0.04f), new Vector3(2.4f, 3.6f, 0.045f), black);
        Prim(PrimitiveType.Cube, "DecalLine", visual.transform,
            new Vector3(0f, 26.2f, Radius + 0.08f), new Vector3(1.85f, 0.07f, 0.03f), accent);
        Prim(PrimitiveType.Cube, "DecalLine2", visual.transform,
            new Vector3(0f, 23.5f, Radius + 0.08f), new Vector3(1.35f, 0.045f, 0.03f), silver);
        SmoothSphere("DecalDot", visual.transform,
            new Vector3(0f, 24.8f, Radius + 0.12f), Vector3.one * 0.28f, gold);

        Prim(PrimitiveType.Cube, "Raceway", visual.transform,
            new Vector3(Radius + 0.11f, 20.5f, 0f), new Vector3(0.2f, 27.5f, 0.28f), carbon);
        Prim(PrimitiveType.Cube, "RacewayEdge", visual.transform,
            new Vector3(Radius + 0.18f, 20.5f, 0f), new Vector3(0.035f, 27.5f, 0.06f), titanium);
        for (int i = 0; i < 7; i++)
        {
            Prim(PrimitiveType.Cube, $"RacewayClip_{i}", visual.transform,
                new Vector3(Radius + 0.16f, 7f + i * 4.5f, 0f),
                new Vector3(0.09f, 0.14f, 0.34f), metal);
        }

        for (int i = 0; i < 3; i++)
        {
            float a = (205f + i * 18f) * Mathf.Deg2Rad;
            SmoothCapsule($"COPV_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * (Radius + 0.36f), 31.2f + i * 0.12f, Mathf.Cos(a) * (Radius + 0.36f)),
                new Vector3(0.48f, 0.78f, 0.48f), whiteMatte);
        }

        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 20f) * Mathf.Deg2Rad;
            float r = Radius + 0.15f;
            SmoothSphere($"RCS_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * r, 36.15f, Mathf.Cos(a) * r),
                new Vector3(0.4f, 0.46f, 0.4f), darkMetal);
            SmoothCylAt($"RCSNoz_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * (r + 0.2f), 36.15f, Mathf.Cos(a) * (r + 0.2f)),
                0.11f, 0.09f, heat);
        }

        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            var led = VisualMaterials.Lit(
                i % 2 == 0 ? new Color(0.2f, 0.95f, 1f) : new Color(1f, 0.3f, 0.15f),
                0.1f, 0.9f,
                (i % 2 == 0 ? new Color(0.2f, 0.95f, 1f) : new Color(1f, 0.3f, 0.15f)) * 0.85f);
            SmoothSphere($"Nav_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * (Radius + 0.05f), 29.8f, Mathf.Cos(a) * (Radius + 0.05f)),
                Vector3.one * 0.14f, led);
        }

        BuildGridFins(visual.transform, titanium, silver, darkMetal, carbon);
        BuildLegs(visual.transform, black, metal, titanium, carbon, darkMetal);
        BuildNozzles(visual.transform, heat, metal, copper, titanium);
        BuildEngineFX(visual.transform);

        // М'яке підсвічування корпусу (не «плями»)
        AddPointLight(visual.transform, "BodyKey", new Vector3(10f, 22f, -9f),
            new Color(0.95f, 0.96f, 1f), 5.5f, 60f);
        AddPointLight(visual.transform, "BodyFill", new Vector3(-9f, 24f, 6f),
            new Color(0.7f, 0.75f, 0.85f), 3.2f, 48f);

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
            float r = Radius + 1.35f;
            var fin = new GameObject($"GridFin_{i}");
            fin.transform.SetParent(visual, false);
            // Трохи нижче interstage — класична F9 посадка
            fin.transform.localPosition = new Vector3(Mathf.Sin(a) * r, 35.2f, Mathf.Cos(a) * r);
            fin.transform.localRotation = Quaternion.Euler(0f, i * 90f, 6f);

            Prim(PrimitiveType.Cube, "Plate", fin.transform, Vector3.zero,
                new Vector3(0.055f, 2.35f, 3.2f), frame);
            Prim(PrimitiveType.Cube, "FrameTop", fin.transform, new Vector3(0.02f, 1.12f, 0f),
                new Vector3(0.09f, 0.06f, 3.15f), titaniumLike(frame));
            Prim(PrimitiveType.Cube, "FrameBot", fin.transform, new Vector3(0.02f, -1.12f, 0f),
                new Vector3(0.09f, 0.06f, 3.15f), frame);
            Prim(PrimitiveType.Cube, "FrameL", fin.transform, new Vector3(0.02f, 0f, 1.52f),
                new Vector3(0.09f, 2.2f, 0.06f), frame);
            Prim(PrimitiveType.Cube, "FrameR", fin.transform, new Vector3(0.02f, 0f, -1.52f),
                new Vector3(0.09f, 2.2f, 0.06f), frame);
            SmoothSphere("Hub", fin.transform, new Vector3(-0.25f, 0f, 0f), Vector3.one * 0.48f, hub);
            SmoothCylAt("Actuator", fin.transform, new Vector3(-0.48f, 0f, 0f), 0.18f, 0.28f, carbon);

            for (int g = 0; g < 6; g++)
                Prim(PrimitiveType.Cube, $"H_{g}", fin.transform,
                    new Vector3(0.06f, -1.0f + g * 0.4f, 0f),
                    new Vector3(0.022f, 0.028f, 2.9f), lattice);
            for (int g = 0; g < 7; g++)
                Prim(PrimitiveType.Cube, $"V_{g}", fin.transform,
                    new Vector3(0.06f, 0f, -1.3f + g * 0.43f),
                    new Vector3(0.022f, 2.15f, 0.028f), lattice);
        }
    }

    static Material titaniumLike(Material _) =>
        VisualMaterials.Lit(new Color(0.6f, 0.62f, 0.66f), 0.9f, 0.7f);

    static void BuildLegs(Transform visual, Material black, Material metal, Material titanium, Material carbon, Material darkMetal)
    {
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            var legRoot = new GameObject($"LegAsm_{i}");
            legRoot.transform.SetParent(visual, false);

            Vector3 hinge = new Vector3(
                Mathf.Sin(a) * (Radius + 0.32f),
                9.1f,
                Mathf.Cos(a) * (Radius + 0.32f));

            Vector3 foot = new Vector3(
                Mathf.Sin(a) * (Radius + 5.8f),
                0.12f,
                Mathf.Cos(a) * (Radius + 5.8f));

            SmoothCylAt("Hinge", legRoot.transform, hinge, 0.52f, 0.28f, titanium);
            SmoothSphere("HingeBall", legRoot.transform, hinge, Vector3.one * 0.5f, darkMetal);

            // Основна нога + A-frame
            Strut(legRoot.transform, "Boom", hinge, foot, 0.36f, black);
            Vector3 hinge2 = hinge + new Vector3(
                Mathf.Sin(a + 0.2f) * 0.4f, -0.35f, Mathf.Cos(a + 0.2f) * 0.4f);
            Vector3 footInner = Vector3.Lerp(hinge, foot, 0.9f) + Vector3.up * 0.2f;
            Strut(legRoot.transform, "Boom2", hinge2, footInner, 0.16f, carbon);

            Vector3 bodyAnchor = new Vector3(
                Mathf.Sin(a) * (Radius + 0.12f),
                6.0f,
                Mathf.Cos(a) * (Radius + 0.12f));
            Vector3 boomMid = Vector3.Lerp(hinge, foot, 0.4f);
            Strut(legRoot.transform, "Hydraulics", bodyAnchor, boomMid, 0.14f, metal);
            Strut(legRoot.transform, "Hydraulics2",
                bodyAnchor + Vector3.up * 1.1f,
                Vector3.Lerp(hinge, foot, 0.26f), 0.09f, titanium);

            // Стопа
            SmoothCylAt("CrushCore", legRoot.transform, foot + Vector3.up * 0.32f, 0.75f, 0.24f, carbon);
            SmoothCylAt("Foot", legRoot.transform, foot + Vector3.up * 0.1f, 1.7f, 0.1f, metal);
            SmoothCylAt("FootPad", legRoot.transform, foot, 2.2f, 0.045f, black);
            SmoothCylAt("FootRing", legRoot.transform, foot + Vector3.up * 0.04f, 1.9f, 0.025f, titanium);
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

    static void BuildNozzles(Transform visual, Material heat, Material metal, Material copper, Material titanium)
    {
        Nozzle(visual, Vector3.zero, heat, metal, copper, titanium, 1.25f, true);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Nozzle(visual, new Vector3(Mathf.Sin(a) * 1.42f, 0f, Mathf.Cos(a) * 1.42f),
                heat, metal, copper, titanium, 0.72f, false);
        }
        SmoothCyl("OctawebRing", visual.transform, 1.9f, Radius * 2.4f, 0.07f, metal);
        SmoothCyl("OctawebRing2", visual.transform, 1.55f, Radius * 2.28f, 0.04f, titanium);
    }

    static void Nozzle(Transform parent, Vector3 xz, Material heat, Material metal, Material copper, Material titanium, float s, bool center)
    {
        // Справжній bell-конус + кільця
        SmoothMesh.MakeBell("Bell", parent,
            new Vector3(xz.x, 0.55f * s, xz.z),
            1.28f * s, 0.72f * s, heat);
        SmoothCylAt("Exit", parent,
            new Vector3(xz.x, 0.02f * s, xz.z), 1.38f * s, 0.07f * s, metal);
        SmoothCylAt("Throat", parent,
            new Vector3(xz.x, 1.38f * s, xz.z), 0.38f * s, 0.14f * s, copper);
        SmoothCylAt("Gimbal", parent,
            new Vector3(xz.x, 1.58f * s, xz.z), 0.55f * s, 0.07f * s, metal);
        if (center)
        {
            SmoothCylAt("Turbopump", parent,
                new Vector3(xz.x, 1.82f * s, xz.z), 0.72f * s, 0.18f * s, titanium);
            SmoothSphere("GimbalBall", parent,
                new Vector3(xz.x, 1.68f * s, xz.z), Vector3.one * (0.42f * s), metal);
        }
    }

    static void BuildEngineFX(Transform visual)
    {
        // Outer plume (warm amber/orange)
        var flameGo = new GameObject("EngineFlame");
        flameGo.transform.SetParent(visual, false);
        flameGo.transform.localPosition = new Vector3(0f, -1.2f, 0f);
        flameGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var flame = flameGo.AddComponent<ParticleSystem>();
        ConfigureFlameOuter(flame);

        // Core plume (bright cyan-white, tighter)
        var coreGo = new GameObject("EngineFlameCore");
        coreGo.transform.SetParent(visual, false);
        coreGo.transform.localPosition = new Vector3(0f, -0.95f, 0f);
        coreGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var core = coreGo.AddComponent<ParticleSystem>();
        ConfigureFlameCore(core);

        // Exhaust smoke / vapor
        var smokeGo = new GameObject("EngineSmoke");
        smokeGo.transform.SetParent(visual, false);
        smokeGo.transform.localPosition = new Vector3(0f, -5.5f, 0f);
        smokeGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var smoke = smokeGo.AddComponent<ParticleSystem>();
        ConfigureSmoke(smoke);

        // Sparks / soot streaks
        var sparkGo = new GameObject("EngineSparks");
        sparkGo.transform.SetParent(visual, false);
        sparkGo.transform.localPosition = new Vector3(0f, -1.0f, 0f);
        sparkGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var sparks = sparkGo.AddComponent<ParticleSystem>();
        ConfigureSparks(sparks);

        // Ground dust plume (activated near surface by RocketEngineFX)
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
            new Color(0.7f, 0.68f, 0.62f, 0.28f),
            new Color(0.45f, 0.43f, 0.4f, 0.12f));
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
                new GradientColorKey(new Color(0.75f, 0.72f, 0.65f), 0f),
                new GradientColorKey(new Color(0.5f, 0.48f, 0.44f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.0f, 0f),
                new GradientAlphaKey(0.22f, 0.15f),
                new GradientAlphaKey(0.1f, 0.55f),
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
        rend.sharedMaterial = VisualMaterials.Particle(new Color(0.6f, 0.58f, 0.52f, 0.2f));
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
        // Кутисті Sphere/Cylinder/Capsule → smooth meshes
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
            if (thin || name.Contains("Ring") || name.Contains("Stripe"))
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
