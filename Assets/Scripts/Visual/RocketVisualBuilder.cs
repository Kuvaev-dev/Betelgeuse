using UnityEngine;

/// <summary>
/// Процедурна модель ракетоносія ~42 м (first stage) + engine FX.
/// Pivot у соплах; корпус уздовж +Y.
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

        // Materials
        var white = VisualMaterials.Lit(new Color(0.96f, 0.97f, 0.99f), 0.22f, 0.72f);
        var whiteMatte = VisualMaterials.Lit(new Color(0.9f, 0.91f, 0.93f), 0.15f, 0.45f);
        var black = VisualMaterials.Lit(new Color(0.05f, 0.055f, 0.07f), 0.55f, 0.28f);
        var metal = VisualMaterials.Lit(new Color(0.58f, 0.6f, 0.64f), 0.9f, 0.62f);
        var carbon = VisualMaterials.Lit(new Color(0.1f, 0.1f, 0.12f), 0.35f, 0.4f);
        var orange = VisualMaterials.Lit(new Color(0.95f, 0.42f, 0.1f), 0.25f, 0.55f,
            new Color(0.45f, 0.12f, 0.02f));
        var heat = VisualMaterials.Lit(new Color(0.2f, 0.14f, 0.1f), 0.6f, 0.22f);
        var gold = VisualMaterials.Lit(new Color(0.78f, 0.64f, 0.35f), 0.75f, 0.55f);
        var cyanTrim = VisualMaterials.Lit(new Color(0.2f, 0.7f, 0.95f), 0.3f, 0.6f,
            new Color(0.05f, 0.25f, 0.4f));

        // ── Stack bottom → top ──
        Cyl("Octaweb", visual.transform, 1.6f, Radius * 2.2f, 1.5f, black);
        Cyl("Skirt", visual.transform, 3.8f, Radius * 2.08f, 1.1f, carbon);
        Cyl("LowerTank", visual.transform, 9.5f, Radius * 2f, 5.2f, white);
        Cyl("Stripe1", visual.transform, 15.0f, Radius * 2.05f, 0.28f, black);
        Cyl("MidTank", visual.transform, 22.5f, Radius * 2f, 7.2f, white);
        Cyl("Stripe2", visual.transform, 30.0f, Radius * 2.05f, 0.28f, black);
        Cyl("Accent", visual.transform, 32.2f, Radius * 2.08f, 0.5f, orange);
        Cyl("Upper", visual.transform, 35.5f, Radius * 1.9f, 2.8f, whiteMatte);
        Cyl("Raceway", visual.transform, 24f, Radius * 2.12f, 0.35f, carbon); // external cable raceway ring

        // Nose
        var nose = Prim(PrimitiveType.Sphere, "Nose", visual.transform,
            new Vector3(0f, 39.8f, 0f), new Vector3(Radius * 1.9f, 3.8f, Radius * 1.9f), white);
        Prim(PrimitiveType.Sphere, "Tip", visual.transform,
            new Vector3(0f, 41.7f, 0f), new Vector3(Radius * 0.85f, 1.1f, Radius * 0.85f), metal);

        // LOX vent / COPV detail bumps
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            Prim(PrimitiveType.Sphere, $"TankDetail_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * (Radius + 0.05f), 20f + i * 1.5f, Mathf.Cos(a) * (Radius + 0.05f)),
                Vector3.one * 0.45f, metal);
        }

        // Mission decal plate
        Prim(PrimitiveType.Cube, "Decal", visual.transform,
            new Vector3(0f, 26f, Radius + 0.06f), new Vector3(2.4f, 4f, 0.1f), orange);
        Prim(PrimitiveType.Cube, "DecalTrim", visual.transform,
            new Vector3(0f, 26f, Radius + 0.08f), new Vector3(1.6f, 0.35f, 0.08f), cyanTrim);

        // Vertical raceway bar
        Prim(PrimitiveType.Cube, "RacewayBar", visual.transform,
            new Vector3(Radius + 0.12f, 22f, 0f), new Vector3(0.25f, 28f, 0.35f), carbon);

        BuildGridFins(visual.transform, metal, gold);
        BuildLegs(visual.transform, black, metal);
        BuildNozzles(visual.transform, heat, metal);
        BuildEngineFX(visual.transform);

        // Soft fill light on body so white paint reads in dark space
        var bodyLight = new GameObject("BodyFill");
        bodyLight.transform.SetParent(visual.transform, false);
        bodyLight.transform.localPosition = new Vector3(8f, 22f, -6f);
        var bl = bodyLight.AddComponent<Light>();
        bl.type = LightType.Point;
        bl.color = new Color(0.75f, 0.85f, 1f);
        bl.intensity = 6f;
        bl.range = 50f;
        bl.shadows = LightShadows.None;

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

    static void BuildGridFins(Transform visual, Material metal, Material gold)
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
                new Vector3(0.1f, 2.5f, 3.4f), metal);
            Prim(PrimitiveType.Cube, "Hub", fin.transform, new Vector3(-0.2f, 0f, 0f),
                new Vector3(0.5f, 0.6f, 0.6f), metal);

            for (int g = 0; g < 4; g++)
            {
                Prim(PrimitiveType.Cube, $"LatH_{g}", fin.transform,
                    new Vector3(0.08f, -1f + g * 0.65f, 0f),
                    new Vector3(0.05f, 0.07f, 3.0f), gold);
            }
            for (int g = 0; g < 5; g++)
            {
                Prim(PrimitiveType.Cube, $"LatV_{g}", fin.transform,
                    new Vector3(0.08f, 0f, -1.3f + g * 0.65f),
                    new Vector3(0.05f, 2.2f, 0.07f), gold);
            }
        }
    }

    static void BuildLegs(Transform visual, Material black, Material metal)
    {
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            float r = Radius + 2.6f;
            var leg = Prim(PrimitiveType.Cube, $"Leg_{i}", visual,
                new Vector3(Mathf.Sin(a) * r, 5.8f, Mathf.Cos(a) * r),
                new Vector3(0.38f, 8.5f, 0.38f), black);
            leg.transform.localRotation = Quaternion.Euler(
                30f * Mathf.Cos(a), 0f, -30f * Mathf.Sin(a));

            Prim(PrimitiveType.Cylinder, $"Foot_{i}", visual,
                new Vector3(Mathf.Sin(a) * (r + 1.8f), 0.3f, Mathf.Cos(a) * (r + 1.8f)),
                new Vector3(1.8f, 0.22f, 1.8f), metal);

            // Hydraulic strut
            Prim(PrimitiveType.Cube, $"Strut_{i}", visual,
                new Vector3(Mathf.Sin(a) * (r * 0.55f), 8f, Mathf.Cos(a) * (r * 0.55f)),
                new Vector3(0.2f, 0.2f, 3.5f), metal);
        }
    }

    static void BuildNozzles(Transform visual, Material heat, Material metal)
    {
        // Center Merlin + 8 outer
        Nozzle(visual, Vector3.zero, heat, metal, 1.2f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Nozzle(visual, new Vector3(Mathf.Sin(a) * 1.4f, 0f, Mathf.Cos(a) * 1.4f), heat, metal, 0.72f);
        }
    }

    static void Nozzle(Transform parent, Vector3 xz, Material heat, Material metal, float s)
    {
        Prim(PrimitiveType.Cylinder, "Bell", parent,
            new Vector3(xz.x, 0.7f * s, xz.z),
            new Vector3(0.9f * s, 0.95f * s, 0.9f * s), heat);
        Prim(PrimitiveType.Cylinder, "Exit", parent,
            new Vector3(xz.x, 0.08f * s, xz.z),
            new Vector3(1.2f * s, 0.15f * s, 1.2f * s), metal);
        Prim(PrimitiveType.Cylinder, "Throat", parent,
            new Vector3(xz.x, 1.35f * s, xz.z),
            new Vector3(0.45f * s, 0.25f * s, 0.45f * s), metal);
    }

    static void BuildEngineFX(Transform visual)
    {
        var flameGo = new GameObject("EngineFlame");
        flameGo.transform.SetParent(visual, false);
        flameGo.transform.localPosition = new Vector3(0f, -1.4f, 0f);
        flameGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var flame = flameGo.AddComponent<ParticleSystem>();
        ConfigureFlame(flame);

        var smokeGo = new GameObject("EngineSmoke");
        smokeGo.transform.SetParent(visual, false);
        smokeGo.transform.localPosition = new Vector3(0f, -2.8f, 0f);
        smokeGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var smoke = smokeGo.AddComponent<ParticleSystem>();
        ConfigureSmoke(smoke);

        // Secondary sparklets
        var sparkGo = new GameObject("EngineSparks");
        sparkGo.transform.SetParent(visual, false);
        sparkGo.transform.localPosition = new Vector3(0f, -1.0f, 0f);
        sparkGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var sparks = sparkGo.AddComponent<ParticleSystem>();
        ConfigureSparks(sparks);

        var lightGo = new GameObject("EngineLight");
        lightGo.transform.SetParent(visual, false);
        lightGo.transform.localPosition = new Vector3(0f, -2.2f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.55f, 0.2f);
        light.intensity = 0f;
        light.range = 100f;
        light.shadows = LightShadows.None;

        var fx = visual.gameObject.AddComponent<RocketEngineFX>();
        fx.flame = flame;
        fx.smoke = smoke;
        fx.sparks = sparks;
        fx.engineLight = light;
    }

    static void ConfigureFlame(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(40f, 85f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 4.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.95f, 0.75f, 1f),
            new Color(1f, 0.4f, 0.08f, 0.9f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 500;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 1.5f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.98f, 0.85f), 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.12f), 0.35f),
                new GradientColorKey(new Color(0.7f, 0.12f, 0.04f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.75f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.5f));

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.Particle(new Color(1f, 0.6f, 0.2f, 1f));
    }

    static void ConfigureSmoke(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 3.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 28f);
        main.startSize = new ParticleSystem.MinMaxCurve(3.5f, 11f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.55f, 0.55f, 0.58f, 0.4f),
            new Color(0.25f, 0.25f, 0.28f, 0.2f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        main.gravityModifier = -0.015f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 24f;
        shape.radius = 2f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.6f, 0.6f, 0.62f), 0f),
                new GradientColorKey(new Color(0.2f, 0.2f, 0.24f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.35f, 0f),
                new GradientAlphaKey(0.12f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 2.4f));

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.Particle(new Color(0.5f, 0.5f, 0.52f, 0.35f));
    }

    static void ConfigureSparks(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(50f, 120f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.startColor = new Color(1f, 0.85f, 0.4f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        main.gravityModifier = 0.2f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 1.2f;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Stretch;
        rend.lengthScale = 2.5f;
        rend.velocityScale = 0.08f;
        rend.sharedMaterial = VisualMaterials.Particle(new Color(1f, 0.8f, 0.3f, 1f));
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
        if (r != null) r.sharedMaterial = mat;
        return go;
    }

    static void DestroyChild(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) Object.Destroy(t.gameObject);
    }
}
