using UnityEngine;

/// <summary>
/// Преміальна процедурна модель 1-го ступеня ~42 м (Falcon-class).
/// Білий корпус / чорний heat-shield / срібло / без cyan-«іграшковості».
/// Pivot у площині сопел; +Y вгору корпусу.
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

        // ── Палітра: clean aerospace white / carbon / titanium ──
        var white = VisualMaterials.Lit(new Color(0.96f, 0.97f, 0.98f), 0.08f, 0.86f);
        var whiteMatte = VisualMaterials.Lit(new Color(0.9f, 0.91f, 0.93f), 0.05f, 0.45f);
        var black = VisualMaterials.Lit(new Color(0.06f, 0.065f, 0.07f), 0.55f, 0.4f);
        var soot = VisualMaterials.Lit(new Color(0.12f, 0.11f, 0.1f), 0.35f, 0.28f);
        var metal = VisualMaterials.Lit(new Color(0.68f, 0.7f, 0.74f), 0.92f, 0.82f);
        var titanium = VisualMaterials.Lit(new Color(0.55f, 0.57f, 0.6f), 0.88f, 0.7f);
        var carbon = VisualMaterials.Lit(new Color(0.09f, 0.095f, 0.1f), 0.45f, 0.48f);
        var silver = VisualMaterials.Lit(new Color(0.84f, 0.86f, 0.9f), 0.9f, 0.78f,
            new Color(0.25f, 0.28f, 0.32f) * 0.2f);
        var heat = VisualMaterials.Lit(new Color(0.14f, 0.12f, 0.11f), 0.8f, 0.28f);
        var copper = VisualMaterials.Lit(new Color(0.48f, 0.38f, 0.3f), 0.9f, 0.55f);
        var darkMetal = VisualMaterials.Lit(new Color(0.2f, 0.21f, 0.23f), 0.88f, 0.52f);
        var stripe = VisualMaterials.Lit(new Color(0.08f, 0.08f, 0.09f), 0.4f, 0.35f);

        // ── Octaweb / aft ──
        Cyl("Octaweb", visual.transform, 1.35f, Radius * 2.32f, 1.25f, black);
        Cyl("AftSkirt", visual.transform, 3.4f, Radius * 2.14f, 0.95f, carbon);
        for (int i = 0; i < 5; i++)
            Cyl($"AftRing_{i}", visual.transform, 2.2f + i * 0.45f, Radius * 2.22f, 0.045f, titanium);

        // ── LOX / RP tanks (білий корпус) ──
        Cyl("LowerTank", visual.transform, 9.0f, Radius * 2.0f, 4.9f, white);
        Cyl("CommonDome", visual.transform, 14.35f, Radius * 2.04f, 0.32f, silver);
        Cyl("Stripe1", visual.transform, 14.85f, Radius * 2.08f, 0.18f, stripe);
        Cyl("MidTank", visual.transform, 22.0f, Radius * 2.0f, 6.85f, white);
        Cyl("Stripe2", visual.transform, 29.3f, Radius * 2.08f, 0.18f, stripe);
        Cyl("UpperTank", visual.transform, 33.6f, Radius * 1.98f, 3.9f, whiteMatte);
        Cyl("Interstage", visual.transform, 37.85f, Radius * 1.78f, 0.75f, carbon);

        // Structural rings
        for (int i = 0; i < 8; i++)
        {
            float hy = 6.5f + i * 3.85f;
            Cyl($"Stringer_{i}", visual.transform, hy, Radius * 2.05f, 0.055f, silver);
        }

        // ── Nose / tip ──
        Prim(PrimitiveType.Sphere, "Nose", visual.transform,
            new Vector3(0f, 39.55f, 0f), new Vector3(Radius * 1.88f, 3.2f, Radius * 1.88f), white);
        Prim(PrimitiveType.Sphere, "Tip", visual.transform,
            new Vector3(0f, 41.35f, 0f), new Vector3(Radius * 0.7f, 0.95f, Radius * 0.7f), metal);

        // Heat-stained lower band (реалізм after reentry)
        Cyl("HeatStain", visual.transform, 5.8f, Radius * 2.06f, 0.55f, soot);

        // COPV spheres (subtle)
        for (int i = 0; i < 6; i++)
        {
            float a = (i * 60f + 15f) * Mathf.Deg2Rad;
            float h = 12f + (i % 3) * 4.2f;
            Prim(PrimitiveType.Sphere, $"COPV_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * (Radius + 0.04f), h, Mathf.Cos(a) * (Radius + 0.04f)),
                Vector3.one * (0.28f + (i % 2) * 0.06f), metal);
        }

        // Mission plate (monochrome BETELGEUSE bars)
        Prim(PrimitiveType.Cube, "Decal", visual.transform,
            new Vector3(0f, 25.2f, Radius + 0.06f), new Vector3(2.4f, 3.6f, 0.07f), black);
        Prim(PrimitiveType.Cube, "DecalLine", visual.transform,
            new Vector3(0f, 26.5f, Radius + 0.09f), new Vector3(1.8f, 0.12f, 0.05f), silver);
        for (int i = 0; i < 9; i++)
        {
            Prim(PrimitiveType.Cube, $"Glyph_{i}", visual.transform,
                new Vector3(-1.0f + i * 0.25f, 25.15f, Radius + 0.1f),
                new Vector3(0.1f, 0.75f + (i % 3) * 0.12f, 0.04f), white);
        }

        // Raceway
        Prim(PrimitiveType.Cube, "Raceway", visual.transform,
            new Vector3(Radius + 0.12f, 21.2f, 0f), new Vector3(0.22f, 28.5f, 0.3f), carbon);
        for (int i = 0; i < 6; i++)
        {
            Prim(PrimitiveType.Cube, $"Clamp_{i}", visual.transform,
                new Vector3(Radius + 0.18f, 8f + i * 5f, 0f),
                new Vector3(0.28f, 0.14f, 0.4f), darkMetal);
        }

        // RCS pods
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 22f) * Mathf.Deg2Rad;
            float r = Radius + 0.12f;
            Prim(PrimitiveType.Cube, $"RCS_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * r, 36.0f, Mathf.Cos(a) * r),
                new Vector3(0.42f, 0.55f, 0.42f), darkMetal);
        }

        // Sensor / radar
        Prim(PrimitiveType.Cube, "SensorBay", visual.transform,
            new Vector3(0f, 4.0f, Radius + 0.18f), new Vector3(1.1f, 0.7f, 0.3f), black);
        Prim(PrimitiveType.Sphere, "Radar", visual.transform,
            new Vector3(0f, 4.0f, Radius + 0.4f), Vector3.one * 0.35f, silver);

        Prim(PrimitiveType.Cylinder, "Antenna", visual.transform,
            new Vector3(-Radius - 0.18f, 33.2f, 0f), new Vector3(0.1f, 1.1f, 0.1f), metal);

        BuildGridFins(visual.transform, titanium, silver, darkMetal);
        BuildLegs(visual.transform, black, metal, titanium);
        BuildNozzles(visual.transform, heat, metal, copper);
        BuildEngineFX(visual.transform);

        // Soft body lights (neutral white — match lunar scene)
        AddPointLight(visual.transform, "BodyKey", new Vector3(10f, 22f, -9f),
            new Color(0.95f, 0.96f, 1f), 8f, 55f);
        AddPointLight(visual.transform, "BodyFill", new Vector3(-9f, 26f, 6f),
            new Color(0.75f, 0.78f, 0.85f), 4.5f, 45f);
        AddPointLight(visual.transform, "BodyAft", new Vector3(0f, 6f, -8f),
            new Color(1f, 0.9f, 0.8f), 3f, 35f);

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

    static void BuildGridFins(Transform visual, Material frame, Material lattice, Material hub)
    {
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            float r = Radius + 1.5f;
            var fin = new GameObject($"GridFin_{i}");
            fin.transform.SetParent(visual, false);
            fin.transform.localPosition = new Vector3(Mathf.Sin(a) * r, 34.2f, Mathf.Cos(a) * r);
            fin.transform.localRotation = Quaternion.Euler(0f, i * 90f, 10f);

            Prim(PrimitiveType.Cube, "Plate", fin.transform, Vector3.zero,
                new Vector3(0.1f, 2.5f, 3.4f), frame);
            Prim(PrimitiveType.Cube, "Hub", fin.transform, new Vector3(-0.22f, 0f, 0f),
                new Vector3(0.5f, 0.55f, 0.55f), hub);
            Prim(PrimitiveType.Cube, "Act", fin.transform, new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.3f, 0.35f, 0.35f), lattice);

            for (int g = 0; g < 5; g++)
                Prim(PrimitiveType.Cube, $"H_{g}", fin.transform,
                    new Vector3(0.08f, -1.05f + g * 0.52f, 0f),
                    new Vector3(0.04f, 0.05f, 3.05f), lattice);
            for (int g = 0; g < 6; g++)
                Prim(PrimitiveType.Cube, $"V_{g}", fin.transform,
                    new Vector3(0.08f, 0f, -1.35f + g * 0.54f),
                    new Vector3(0.04f, 2.25f, 0.05f), lattice);
        }
    }

    static void BuildLegs(Transform visual, Material black, Material metal, Material titanium)
    {
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            float r = Radius + 2.45f;
            var legRoot = new GameObject($"LegAsm_{i}");
            legRoot.transform.SetParent(visual, false);

            // Main boom
            var leg = Prim(PrimitiveType.Cube, "Boom", legRoot.transform,
                new Vector3(Mathf.Sin(a) * r, 5.6f, Mathf.Cos(a) * r),
                new Vector3(0.38f, 8.4f, 0.38f), black);
            leg.transform.localRotation = Quaternion.Euler(
                26f * Mathf.Cos(a), 0f, -26f * Mathf.Sin(a));

            // Deploy hinge
            Prim(PrimitiveType.Cylinder, "Hinge", legRoot.transform,
                new Vector3(Mathf.Sin(a) * (Radius + 0.35f), 9.2f, Mathf.Cos(a) * (Radius + 0.35f)),
                new Vector3(0.35f, 0.28f, 0.35f), titanium);

            // Hydraulic strut
            Prim(PrimitiveType.Cylinder, "Strut", legRoot.transform,
                new Vector3(Mathf.Sin(a) * (r * 0.62f), 7.2f, Mathf.Cos(a) * (r * 0.62f)),
                new Vector3(0.2f, 2.2f, 0.2f), metal);

            // Foot
            float fx = Mathf.Sin(a) * (r + 1.75f);
            float fz = Mathf.Cos(a) * (r + 1.75f);
            Prim(PrimitiveType.Cylinder, "Foot", legRoot.transform,
                new Vector3(fx, 0.32f, fz), new Vector3(1.9f, 0.18f, 1.9f), metal);
            Prim(PrimitiveType.Cylinder, "FootPad", legRoot.transform,
                new Vector3(fx, 0.14f, fz), new Vector3(2.35f, 0.07f, 2.35f), black);
        }
    }

    static void BuildNozzles(Transform visual, Material heat, Material metal, Material copper)
    {
        Nozzle(visual, Vector3.zero, heat, metal, copper, 1.22f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Nozzle(visual, new Vector3(Mathf.Sin(a) * 1.4f, 0f, Mathf.Cos(a) * 1.4f),
                heat, metal, copper, 0.72f);
        }
        Cyl("OctawebRing", visual, 1.85f, Radius * 2.38f, 0.1f, metal);
    }

    static void Nozzle(Transform parent, Vector3 xz, Material heat, Material metal, Material copper, float s)
    {
        Prim(PrimitiveType.Cylinder, "Bell", parent,
            new Vector3(xz.x, 0.7f * s, xz.z),
            new Vector3(0.92f * s, 0.95f * s, 0.92f * s), heat);
        Prim(PrimitiveType.Cylinder, "Exit", parent,
            new Vector3(xz.x, 0.05f * s, xz.z),
            new Vector3(1.25f * s, 0.12f * s, 1.25f * s), metal);
        Prim(PrimitiveType.Cylinder, "Throat", parent,
            new Vector3(xz.x, 1.35f * s, xz.z),
            new Vector3(0.4f * s, 0.2f * s, 0.4f * s), copper);
        Prim(PrimitiveType.Cylinder, "Gimbal", parent,
            new Vector3(xz.x, 1.58f * s, xz.z),
            new Vector3(0.52f * s, 0.1f * s, 0.52f * s), metal);
    }

    static void BuildEngineFX(Transform visual)
    {
        var flameGo = new GameObject("EngineFlame");
        flameGo.transform.SetParent(visual, false);
        flameGo.transform.localPosition = new Vector3(0f, -1.45f, 0f);
        flameGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var flame = flameGo.AddComponent<ParticleSystem>();
        ConfigureFlame(flame);

        var smokeGo = new GameObject("EngineSmoke");
        smokeGo.transform.SetParent(visual, false);
        smokeGo.transform.localPosition = new Vector3(0f, -4.2f, 0f);
        smokeGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var smoke = smokeGo.AddComponent<ParticleSystem>();
        ConfigureSmoke(smoke);

        var sparkGo = new GameObject("EngineSparks");
        sparkGo.transform.SetParent(visual, false);
        sparkGo.transform.localPosition = new Vector3(0f, -1.05f, 0f);
        sparkGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var sparks = sparkGo.AddComponent<ParticleSystem>();
        ConfigureSparks(sparks);

        var lightGo = new GameObject("EngineLight");
        lightGo.transform.SetParent(visual, false);
        lightGo.transform.localPosition = new Vector3(0f, -2.3f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.7f, 0.85f, 1f);
        light.intensity = 0f;
        light.range = 130f;
        light.shadows = LightShadows.None;

        var fx = visual.gameObject.AddComponent<RocketEngineFX>();
        fx.flame = flame;
        fx.smoke = smoke;
        fx.sparks = sparks;
        fx.engineLight = light;
    }

    static void ConfigureFlame(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(48f, 100f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 5.0f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.9f, 0.96f, 1f, 1f),
            new Color(1f, 0.5f, 0.15f, 0.9f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 800;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 7.5f;
        shape.radius = 1.65f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.95f, 0.98f, 1f), 0f),
                new GradientColorKey(new Color(0.6f, 0.82f, 1f), 0.2f),
                new GradientColorKey(new Color(1f, 0.55f, 0.18f), 0.5f),
                new GradientColorKey(new Color(0.5f, 0.1f, 0.04f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.25f),
                new GradientAlphaKey(0.4f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.4f, 1f, 1.7f));

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.Particle(new Color(0.75f, 0.88f, 1f, 1f));
    }

    static void ConfigureSmoke(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(16f, 38f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.1f, 3.0f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.55f, 0.55f, 0.58f, 0.12f),
            new Color(0.28f, 0.28f, 0.3f, 0.05f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 70;
        main.gravityModifier = 0.06f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 11f;
        shape.radius = 0.95f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(0.55f, 0.55f, 0.58f), 0f), new GradientColorKey(new Color(0.2f, 0.2f, 0.22f), 1f) },
            new[] { new GradientAlphaKey(0.1f, 0f), new GradientAlphaKey(0.04f, 0.4f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.35f));

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.Particle(new Color(0.4f, 0.4f, 0.42f, 0.1f));
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static void ConfigureSparks(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(50f, 120f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.38f);
        main.startColor = new Color(1f, 0.88f, 0.5f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 220;
        main.gravityModifier = 0.2f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 14f;
        shape.radius = 1.2f;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Stretch;
        rend.lengthScale = 2.6f;
        rend.velocityScale = 0.08f;
        rend.sharedMaterial = VisualMaterials.Particle(new Color(1f, 0.85f, 0.35f, 1f));
    }

    static void Cyl(string name, Transform parent, float y, float diameter, float halfHeight, Material mat)
    {
        Prim(PrimitiveType.Cylinder, name, parent,
            new Vector3(0f, y, 0f),
            new Vector3(diameter, halfHeight, diameter), mat);
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
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
            bool thin = scale.y < 0.2f || Mathf.Min(scale.x, scale.z) < 0.45f;
            if (thin || name.Contains("Ring") || name.Contains("Stripe") || name.Contains("Glyph") || name.Contains("Stringer"))
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
