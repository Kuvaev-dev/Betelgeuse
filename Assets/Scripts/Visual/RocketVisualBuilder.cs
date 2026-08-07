using UnityEngine;

/// <summary>
/// Процедурна модель 1-го ступеня ~42 м (Falcon-class).
/// Чистий корпус без «болтів»; ноги — від шарніра до стопи.
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

        var white = VisualMaterials.Lit(new Color(0.96f, 0.97f, 0.98f), 0.06f, 0.86f);
        var whiteMatte = VisualMaterials.Lit(new Color(0.9f, 0.91f, 0.93f), 0.04f, 0.42f);
        var black = VisualMaterials.Lit(new Color(0.06f, 0.065f, 0.07f), 0.55f, 0.38f);
        var soot = VisualMaterials.Lit(new Color(0.12f, 0.11f, 0.1f), 0.35f, 0.26f);
        var metal = VisualMaterials.Lit(new Color(0.7f, 0.72f, 0.76f), 0.92f, 0.82f);
        var titanium = VisualMaterials.Lit(new Color(0.56f, 0.58f, 0.62f), 0.88f, 0.7f);
        var carbon = VisualMaterials.Lit(new Color(0.09f, 0.095f, 0.1f), 0.45f, 0.48f);
        var silver = VisualMaterials.Lit(new Color(0.84f, 0.86f, 0.9f), 0.9f, 0.78f);
        var heat = VisualMaterials.Lit(new Color(0.14f, 0.12f, 0.11f), 0.8f, 0.26f);
        var copper = VisualMaterials.Lit(new Color(0.5f, 0.38f, 0.3f), 0.9f, 0.55f);
        var darkMetal = VisualMaterials.Lit(new Color(0.2f, 0.21f, 0.23f), 0.88f, 0.5f);
        var stripe = VisualMaterials.Lit(new Color(0.08f, 0.08f, 0.09f), 0.4f, 0.32f);
        var accent = VisualMaterials.Lit(new Color(0.2f, 0.7f, 0.9f), 0.25f, 0.65f,
            new Color(0.1f, 0.45f, 0.65f) * 0.4f);

        // ── Корпус (smooth cylinders 48 seg) ──
        SmoothCyl("Octaweb", visual.transform, 1.3f, Radius * 2.3f, 1.15f, black);
        SmoothCyl("AftSkirt", visual.transform, 3.4f, Radius * 2.14f, 0.95f, carbon);
        for (int i = 0; i < 4; i++)
            SmoothCyl($"AftRing_{i}", visual.transform, 2.2f + i * 0.42f, Radius * 2.22f, 0.035f, titanium);

        SmoothCyl("LowerTank", visual.transform, 9.0f, Radius * 2.0f, 4.9f, white);
        SmoothCyl("CommonDome", visual.transform, 14.35f, Radius * 2.05f, 0.3f, silver);
        SmoothCyl("Stripe1", visual.transform, 14.9f, Radius * 2.08f, 0.12f, stripe);
        SmoothCyl("MidTank", visual.transform, 22.0f, Radius * 2.0f, 6.85f, white);
        SmoothCyl("Stripe2", visual.transform, 29.3f, Radius * 2.08f, 0.12f, stripe);
        SmoothCyl("UpperTank", visual.transform, 33.6f, Radius * 1.98f, 3.9f, whiteMatte);
        SmoothCyl("Interstage", visual.transform, 37.85f, Radius * 1.78f, 0.7f, carbon);
        SmoothCyl("InterstageRing", visual.transform, 38.5f, Radius * 1.82f, 0.08f, titanium);

        for (int i = 0; i < 7; i++)
            SmoothCyl($"Ring_{i}", visual.transform, 6.5f + i * 4.6f, Radius * 2.05f, 0.035f, silver);

        SmoothCyl("HeatStain", visual.transform, 5.7f, Radius * 2.06f, 0.5f, soot);
        SmoothCyl("HeatStain2", visual.transform, 6.5f, Radius * 2.04f, 0.18f,
            VisualMaterials.Lit(new Color(0.18f, 0.14f, 0.12f), 0.5f, 0.28f));

        Prim(PrimitiveType.Sphere, "Nose", visual.transform,
            new Vector3(0f, 39.55f, 0f), new Vector3(Radius * 1.88f, 3.15f, Radius * 1.88f), white);
        Prim(PrimitiveType.Sphere, "Tip", visual.transform,
            new Vector3(0f, 41.35f, 0f), new Vector3(Radius * 0.62f, 0.88f, Radius * 0.62f), metal);

        // Маркування BETELGEUSE
        Prim(PrimitiveType.Cube, "Decal", visual.transform,
            new Vector3(0f, 25.2f, Radius + 0.06f), new Vector3(2.4f, 3.5f, 0.07f), black);
        Prim(PrimitiveType.Cube, "DecalLine", visual.transform,
            new Vector3(0f, 26.55f, Radius + 0.1f), new Vector3(1.85f, 0.1f, 0.04f), accent);
        Prim(PrimitiveType.Cube, "DecalLine2", visual.transform,
            new Vector3(0f, 23.9f, Radius + 0.1f), new Vector3(1.4f, 0.06f, 0.04f), silver);

        Prim(PrimitiveType.Cube, "Raceway", visual.transform,
            new Vector3(Radius + 0.12f, 21f, 0f), new Vector3(0.22f, 28f, 0.3f), carbon);
        Prim(PrimitiveType.Cube, "RacewayEdge", visual.transform,
            new Vector3(Radius + 0.2f, 21f, 0f), new Vector3(0.05f, 28f, 0.08f), titanium);

        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 22f) * Mathf.Deg2Rad;
            float r = Radius + 0.14f;
            Prim(PrimitiveType.Cube, $"RCS_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * r, 36.1f, Mathf.Cos(a) * r),
                new Vector3(0.42f, 0.52f, 0.42f), darkMetal);
        }
        // Nav lights
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            var led = VisualMaterials.Lit(
                i % 2 == 0 ? new Color(0.2f, 0.9f, 1f) : new Color(1f, 0.3f, 0.15f),
                0.1f, 0.8f,
                (i % 2 == 0 ? new Color(0.2f, 0.9f, 1f) : new Color(1f, 0.3f, 0.15f)) * 0.7f);
            Prim(PrimitiveType.Sphere, $"Nav_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * (Radius + 0.06f), 30.2f, Mathf.Cos(a) * (Radius + 0.06f)),
                Vector3.one * 0.14f, led);
        }

        BuildGridFins(visual.transform, titanium, silver, darkMetal);
        BuildLegs(visual.transform, black, metal, titanium, carbon);
        BuildNozzles(visual.transform, heat, metal, copper);
        BuildEngineFX(visual.transform);

        AddPointLight(visual.transform, "BodyKey", new Vector3(9f, 22f, -8f),
            new Color(0.95f, 0.96f, 1f), 6f, 50f);
        AddPointLight(visual.transform, "BodyFill", new Vector3(-8f, 26f, 5f),
            new Color(0.7f, 0.75f, 0.85f), 3.5f, 40f);

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
            float r = Radius + 1.45f;
            var fin = new GameObject($"GridFin_{i}");
            fin.transform.SetParent(visual, false);
            fin.transform.localPosition = new Vector3(Mathf.Sin(a) * r, 34.2f, Mathf.Cos(a) * r);
            fin.transform.localRotation = Quaternion.Euler(0f, i * 90f, 10f);

            Prim(PrimitiveType.Cube, "Plate", fin.transform, Vector3.zero,
                new Vector3(0.08f, 2.4f, 3.3f), frame);
            Prim(PrimitiveType.Cube, "Hub", fin.transform, new Vector3(-0.22f, 0f, 0f),
                new Vector3(0.48f, 0.5f, 0.5f), hub);

            for (int g = 0; g < 5; g++)
                Prim(PrimitiveType.Cube, $"H_{g}", fin.transform,
                    new Vector3(0.07f, -1.0f + g * 0.5f, 0f),
                    new Vector3(0.035f, 0.04f, 3.0f), lattice);
            for (int g = 0; g < 6; g++)
                Prim(PrimitiveType.Cube, $"V_{g}", fin.transform,
                    new Vector3(0.07f, 0f, -1.3f + g * 0.52f),
                    new Vector3(0.035f, 2.2f, 0.04f), lattice);
        }
    }

    /// <summary>
    /// Ноги Falcon-style: шарнір на корпусі → балка до стопи на y≈0.
    /// Балка орієнтована вздовж вектора hinge→foot (transform.up).
    /// </summary>
    static void BuildLegs(Transform visual, Material black, Material metal, Material titanium, Material carbon)
    {
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            var legRoot = new GameObject($"LegAsm_{i}");
            legRoot.transform.SetParent(visual, false);

            // Шарнір на корпусі
            Vector3 hinge = new Vector3(
                Mathf.Sin(a) * (Radius + 0.25f),
                9.2f,
                Mathf.Cos(a) * (Radius + 0.25f));

            // Стопа на поверхні, відведена назовні
            Vector3 foot = new Vector3(
                Mathf.Sin(a) * (Radius + 5.2f),
                0.18f,
                Mathf.Cos(a) * (Radius + 5.2f));

            Prim(PrimitiveType.Cylinder, "Hinge", legRoot.transform,
                hinge, new Vector3(0.42f, 0.28f, 0.42f), titanium);

            // Головна балка hinge → foot
            Strut(legRoot.transform, "Boom", hinge, foot, 0.32f, black);

            // Гідравліка: від точки нижче шарніра на корпусі до середини балки
            Vector3 bodyAnchor = new Vector3(
                Mathf.Sin(a) * (Radius + 0.15f),
                6.5f,
                Mathf.Cos(a) * (Radius + 0.15f));
            Vector3 boomMid = Vector3.Lerp(hinge, foot, 0.45f);
            Strut(legRoot.transform, "Hydraulics", bodyAnchor, boomMid, 0.14f, metal);

            // Стопа
            Prim(PrimitiveType.Cylinder, "Foot", legRoot.transform,
                foot + Vector3.up * 0.12f, new Vector3(1.7f, 0.14f, 1.7f), metal);
            Prim(PrimitiveType.Cylinder, "FootPad", legRoot.transform,
                foot, new Vector3(2.2f, 0.06f, 2.2f), black);
        }
    }

    /// <summary>Циліндр-стійка між двома точками (локальні координати visual).</summary>
    static void Strut(Transform parent, string name, Vector3 from, Vector3 to, float thickness, Material mat)
    {
        Vector3 delta = to - from;
        float len = delta.magnitude;
        if (len < 1e-4f) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = (from + to) * 0.5f;
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        // Unity cylinder height = 2 * scale.y
        go.transform.localScale = new Vector3(thickness, len * 0.5f, thickness);

        var col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        var r = go.GetComponent<MeshRenderer>();
        if (r != null)
        {
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
        }
    }

    static void BuildNozzles(Transform visual, Material heat, Material metal, Material copper)
    {
        Nozzle(visual, Vector3.zero, heat, metal, copper, 1.2f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Nozzle(visual, new Vector3(Mathf.Sin(a) * 1.38f, 0f, Mathf.Cos(a) * 1.38f),
                heat, metal, copper, 0.7f);
        }
        Cyl("OctawebRing", visual, 1.85f, Radius * 2.36f, 0.08f, metal);
    }

    static void Nozzle(Transform parent, Vector3 xz, Material heat, Material metal, Material copper, float s)
    {
        Prim(PrimitiveType.Cylinder, "Bell", parent,
            new Vector3(xz.x, 0.7f * s, xz.z),
            new Vector3(0.95f * s, 0.9f * s, 0.95f * s), heat);
        Prim(PrimitiveType.Cylinder, "Exit", parent,
            new Vector3(xz.x, 0.05f * s, xz.z),
            new Vector3(1.28f * s, 0.1f * s, 1.28f * s), metal);
        Prim(PrimitiveType.Cylinder, "Throat", parent,
            new Vector3(xz.x, 1.35f * s, xz.z),
            new Vector3(0.38f * s, 0.18f * s, 0.38f * s), copper);
        Prim(PrimitiveType.Cylinder, "Gimbal", parent,
            new Vector3(xz.x, 1.55f * s, xz.z),
            new Vector3(0.5f * s, 0.08f * s, 0.5f * s), metal);
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
        light.range = 120f;
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
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(14f, 34f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.0f, 2.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.55f, 0.55f, 0.58f, 0.1f),
            new Color(0.28f, 0.28f, 0.3f, 0.04f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 70;
        main.gravityModifier = 0.05f;

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
            new[] { new GradientAlphaKey(0.08f, 0f), new GradientAlphaKey(0.03f, 0.4f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.35f));

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.Particle(new Color(0.4f, 0.4f, 0.42f, 0.08f));
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static void ConfigureSparks(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.32f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(50f, 115f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
        main.startColor = new Color(1f, 0.88f, 0.5f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        main.gravityModifier = 0.18f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 13f;
        shape.radius = 1.2f;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Stretch;
        rend.lengthScale = 2.5f;
        rend.velocityScale = 0.08f;
        rend.sharedMaterial = VisualMaterials.Particle(new Color(1f, 0.85f, 0.35f, 1f));
    }

    static void Cyl(string name, Transform parent, float y, float diameter, float halfHeight, Material mat)
    {
        Prim(PrimitiveType.Cylinder, name, parent,
            new Vector3(0f, y, 0f),
            new Vector3(diameter, halfHeight, diameter), mat);
    }

    static void SmoothCyl(string name, Transform parent, float y, float diameter, float halfHeight, Material mat)
    {
        var go = SmoothMesh.MakeCylinder(name, parent, new Vector3(0f, y, 0f), diameter, halfHeight, mat);
        var r = go.GetComponent<MeshRenderer>();
        if (r != null)
        {
            bool thin = halfHeight < 0.2f;
            r.shadowCastingMode = thin
                ? UnityEngine.Rendering.ShadowCastingMode.Off
                : UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = !thin;
        }
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
