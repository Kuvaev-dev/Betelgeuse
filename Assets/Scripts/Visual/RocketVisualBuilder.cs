using UnityEngine;

/// <summary>
/// Процедурна 3D-модель першого ступеня ~42 м (клас Falcon 9 / New Glenn scale).
/// Pivot у площині сопел; вісь корпусу — локальний +Y.
/// Будується в runtime без зовнішніх prefab-ассетів (відтворюваність демо).
/// </summary>
public static class RocketVisualBuilder
{
    public const float Height = 42f;
    public const float Radius = 1.85f;

    /// <summary>Збирає повну візуальну ієрархію під RocketPhysics.</summary>
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

        // Палітра: cold-white body + carbon + cyan mission accent (сучасний GNC-демо look)
        Color cyan = new Color(0.25f, 0.78f, 1f);
        var white = VisualMaterials.Lit(new Color(0.97f, 0.975f, 0.985f), 0.1f, 0.88f);
        var whiteMatte = VisualMaterials.Lit(new Color(0.88f, 0.89f, 0.91f), 0.06f, 0.42f);
        var black = VisualMaterials.Lit(new Color(0.05f, 0.055f, 0.065f), 0.6f, 0.38f);
        var metal = VisualMaterials.Lit(new Color(0.62f, 0.64f, 0.68f), 0.92f, 0.78f);
        var carbon = VisualMaterials.Lit(new Color(0.1f, 0.11f, 0.125f), 0.4f, 0.42f);
        var silver = VisualMaterials.Lit(new Color(0.82f, 0.84f, 0.88f), 0.9f, 0.72f,
            new Color(0.2f, 0.25f, 0.3f) * 0.25f);
        var heat = VisualMaterials.Lit(new Color(0.16f, 0.14f, 0.13f), 0.75f, 0.32f);
        var gold = VisualMaterials.Lit(new Color(0.72f, 0.74f, 0.78f), 0.8f, 0.6f);
        var darkMetal = VisualMaterials.Lit(new Color(0.22f, 0.23f, 0.26f), 0.9f, 0.5f);
        var copper = VisualMaterials.Lit(new Color(0.5f, 0.42f, 0.35f), 0.92f, 0.52f);
        var accent = VisualMaterials.Lit(new Color(0.12f, 0.16f, 0.2f), 0.35f, 0.6f, cyan * 0.35f);
        var led = VisualMaterials.Lit(cyan * 0.4f, 0.15f, 0.75f, cyan * 1.1f);
        var noseGlow = VisualMaterials.Lit(new Color(0.9f, 0.92f, 0.95f), 0.2f, 0.7f,
            cyan * 0.15f);

        // ── Корпус (пропорції first stage ~42 м) ──
        Cyl("Octaweb", visual.transform, 1.5f, Radius * 2.28f, 1.4f, black);
        Cyl("Skirt", visual.transform, 3.55f, Radius * 2.12f, 1.0f, carbon);
        for (int i = 0; i < 4; i++)
            Cyl($"SkirtRing_{i}", visual.transform, 2.4f + i * 0.55f, Radius * 2.2f, 0.06f, darkMetal);

        // Баки + structural stringers
        Cyl("LowerTank", visual.transform, 9.2f, Radius * 2f, 5.0f, white);
        Cyl("CommonDome", visual.transform, 14.6f, Radius * 2.02f, 0.35f, silver);
        Cyl("Stripe1", visual.transform, 15.1f, Radius * 2.06f, 0.22f, black);
        Cyl("MidTank", visual.transform, 22.2f, Radius * 2f, 6.9f, white);
        Cyl("Stripe2", visual.transform, 29.5f, Radius * 2.06f, 0.22f, black);
        Cyl("Accent", visual.transform, 31.6f, Radius * 2.08f, 0.38f, accent);
        Cyl("Upper", visual.transform, 35.0f, Radius * 1.94f, 2.6f, whiteMatte);
        Cyl("Interstage", visual.transform, 37.9f, Radius * 1.72f, 0.85f, carbon);
        // Тонкі кільця жорсткості вздовж корпусу
        for (int i = 0; i < 6; i++)
        {
            float hy = 7f + i * 4.5f;
            Cyl($"StringerRing_{i}", visual.transform, hy, Radius * 2.04f, 0.07f, silver);
        }

        // Ніс
        Prim(PrimitiveType.Sphere, "Nose", visual.transform,
            new Vector3(0f, 39.6f, 0f), new Vector3(Radius * 1.9f, 3.5f, Radius * 1.9f), noseGlow);
        Prim(PrimitiveType.Sphere, "Tip", visual.transform,
            new Vector3(0f, 41.5f, 0f), new Vector3(Radius * 0.75f, 1.0f, Radius * 0.75f), metal);

        // COPV / венти
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            float h = 11f + (i % 4) * 3.4f;
            Prim(PrimitiveType.Sphere, $"TankDetail_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * (Radius + 0.05f), h, Mathf.Cos(a) * (Radius + 0.05f)),
                Vector3.one * (0.32f + (i % 3) * 0.07f), metal);
        }

        // Місійна пластина BETELGEUSE + cyan accent strip
        Prim(PrimitiveType.Cube, "Decal", visual.transform,
            new Vector3(0f, 25.5f, Radius + 0.07f), new Vector3(2.6f, 4.0f, 0.09f), black);
        Prim(PrimitiveType.Cube, "DecalTrim", visual.transform,
            new Vector3(0f, 26.8f, Radius + 0.1f), new Vector3(1.9f, 0.22f, 0.07f), led);
        Prim(PrimitiveType.Cube, "DecalTrim2", visual.transform,
            new Vector3(0f, 24.2f, Radius + 0.1f), new Vector3(1.9f, 0.14f, 0.07f), darkMetal);
        for (int i = 0; i < 9; i++)
        {
            Prim(PrimitiveType.Cube, $"Letter_{i}", visual.transform,
                new Vector3(-1.05f + i * 0.26f, 25.5f, Radius + 0.12f),
                new Vector3(0.12f, 0.9f + (i % 2) * 0.18f, 0.05f), white);
        }

        // Вертикальні LED-смуги (mission aesthetic)
        for (int s = 0; s < 2; s++)
        {
            float side = s == 0 ? 1f : -1f;
            Prim(PrimitiveType.Cube, $"LedStrip_{s}", visual.transform,
                new Vector3(side * (Radius + 0.02f) * 0.7f, 22f, Radius + 0.04f),
                new Vector3(0.08f, 18f, 0.06f), led);
        }

        // Raceway
        Prim(PrimitiveType.Cube, "RacewayBar", visual.transform,
            new Vector3(Radius + 0.13f, 21.5f, 0f), new Vector3(0.26f, 29f, 0.34f), carbon);
        for (int i = 0; i < 7; i++)
        {
            Prim(PrimitiveType.Cube, $"RacewayClamp_{i}", visual.transform,
                new Vector3(Radius + 0.2f, 7.5f + i * 4.5f, 0f),
                new Vector3(0.32f, 0.18f, 0.46f), darkMetal);
        }

        // RCS
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 20f) * Mathf.Deg2Rad;
            float r = Radius + 0.14f;
            Prim(PrimitiveType.Cube, $"RCS_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * r, 36.2f, Mathf.Cos(a) * r),
                new Vector3(0.5f, 0.65f, 0.5f), darkMetal);
        }

        // Landing radar / sensor bay
        Prim(PrimitiveType.Cube, "SensorBay", visual.transform,
            new Vector3(0f, 4.2f, Radius + 0.2f), new Vector3(1.2f, 0.8f, 0.35f), black);
        Prim(PrimitiveType.Sphere, "Radar", visual.transform,
            new Vector3(0f, 4.2f, Radius + 0.45f), Vector3.one * 0.4f, silver);

        // Антени / датчики
        Prim(PrimitiveType.Cylinder, "Antenna", visual.transform,
            new Vector3(-Radius - 0.2f, 33f, 0f), new Vector3(0.12f, 1.2f, 0.12f), metal);
        Prim(PrimitiveType.Sphere, "Sensor", visual.transform,
            new Vector3(0f, 40.5f, Radius * 0.6f), Vector3.one * 0.35f, silver);

        BuildGridFins(visual.transform, metal, gold, darkMetal);
        BuildLegs(visual.transform, black, metal, darkMetal);
        BuildNozzles(visual.transform, heat, metal, copper);
        BuildEngineFX(visual.transform);

        // Кінематографічне підсвічування корпусу
        var bodyLight = new GameObject("BodyFill");
        bodyLight.transform.SetParent(visual.transform, false);
        bodyLight.transform.localPosition = new Vector3(11f, 24f, -9f);
        var bl = bodyLight.AddComponent<Light>();
        bl.type = LightType.Point;
        bl.color = new Color(0.9f, 0.93f, 1f);
        bl.intensity = 9f;
        bl.range = 60f;
        bl.shadows = LightShadows.None;

        var rimLight = new GameObject("BodyRim");
        rimLight.transform.SetParent(visual.transform, false);
        rimLight.transform.localPosition = new Vector3(-12f, 28f, 7f);
        var rl = rimLight.AddComponent<Light>();
        rl.type = LightType.Point;
        rl.color = new Color(0.45f, 0.7f, 1f);
        rl.intensity = 5.5f;
        rl.range = 50f;

        var bellyLight = new GameObject("BodyBelly");
        bellyLight.transform.SetParent(visual.transform, false);
        bellyLight.transform.localPosition = new Vector3(0f, 8f, -10f);
        var bel = bellyLight.AddComponent<Light>();
        bel.type = LightType.Point;
        bel.color = new Color(1f, 0.85f, 0.7f);
        bel.intensity = 3.5f;
        bel.range = 40f;

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

    static void BuildGridFins(Transform visual, Material metal, Material gold, Material darkMetal)
    {
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            float r = Radius + 1.55f;
            var fin = new GameObject($"GridFin_{i}");
            fin.transform.SetParent(visual, false);
            fin.transform.localPosition = new Vector3(Mathf.Sin(a) * r, 34.3f, Mathf.Cos(a) * r);
            fin.transform.localRotation = Quaternion.Euler(0f, i * 90f, 12f);

            Prim(PrimitiveType.Cube, "Plate", fin.transform, Vector3.zero,
                new Vector3(0.12f, 2.6f, 3.5f), metal);
            Prim(PrimitiveType.Cube, "Hub", fin.transform, new Vector3(-0.25f, 0f, 0f),
                new Vector3(0.55f, 0.65f, 0.65f), darkMetal);
            Prim(PrimitiveType.Cube, "Actuator", fin.transform, new Vector3(-0.55f, 0f, 0f),
                new Vector3(0.35f, 0.4f, 0.4f), gold);

            for (int g = 0; g < 5; g++)
            {
                Prim(PrimitiveType.Cube, $"LatH_{g}", fin.transform,
                    new Vector3(0.1f, -1.1f + g * 0.55f, 0f),
                    new Vector3(0.05f, 0.06f, 3.15f), gold);
            }
            for (int g = 0; g < 6; g++)
            {
                Prim(PrimitiveType.Cube, $"LatV_{g}", fin.transform,
                    new Vector3(0.1f, 0f, -1.4f + g * 0.56f),
                    new Vector3(0.05f, 2.35f, 0.06f), gold);
            }
        }
    }

    static void BuildLegs(Transform visual, Material black, Material metal, Material darkMetal)
    {
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            float r = Radius + 2.55f;
            var legRoot = new GameObject($"LegAsm_{i}");
            legRoot.transform.SetParent(visual, false);

            var leg = Prim(PrimitiveType.Cube, $"Leg_{i}", legRoot.transform,
                new Vector3(Mathf.Sin(a) * r, 5.8f, Mathf.Cos(a) * r),
                new Vector3(0.4f, 8.6f, 0.4f), black);
            leg.transform.localRotation = Quaternion.Euler(
                28f * Mathf.Cos(a), 0f, -28f * Mathf.Sin(a));

            // Гідравліка
            Prim(PrimitiveType.Cube, $"Strut_{i}", legRoot.transform,
                new Vector3(Mathf.Sin(a) * (r * 0.55f), 8.2f, Mathf.Cos(a) * (r * 0.55f)),
                new Vector3(0.18f, 0.18f, 3.6f), metal);
            Prim(PrimitiveType.Cylinder, $"Piston_{i}", legRoot.transform,
                new Vector3(Mathf.Sin(a) * (r * 0.72f), 6.5f, Mathf.Cos(a) * (r * 0.72f)),
                new Vector3(0.25f, 1.8f, 0.25f), darkMetal);

            // Стопа
            Prim(PrimitiveType.Cylinder, $"Foot_{i}", legRoot.transform,
                new Vector3(Mathf.Sin(a) * (r + 1.85f), 0.28f, Mathf.Cos(a) * (r + 1.85f)),
                new Vector3(2.0f, 0.2f, 2.0f), metal);
            Prim(PrimitiveType.Cylinder, $"FootPad_{i}", legRoot.transform,
                new Vector3(Mathf.Sin(a) * (r + 1.85f), 0.12f, Mathf.Cos(a) * (r + 1.85f)),
                new Vector3(2.4f, 0.08f, 2.4f), black);
        }
    }

    static void BuildNozzles(Transform visual, Material heat, Material metal, Material copper)
    {
        // Центральний + 8 зовнішніх (октавеб)
        Nozzle(visual, Vector3.zero, heat, metal, copper, 1.25f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Nozzle(visual, new Vector3(Mathf.Sin(a) * 1.42f, 0f, Mathf.Cos(a) * 1.42f), heat, metal, copper, 0.74f);
        }
        // Кільце октавеба
        Cyl("OctawebRing", visual, 1.9f, Radius * 2.4f, 0.12f, metal);
    }

    static void Nozzle(Transform parent, Vector3 xz, Material heat, Material metal, Material copper, float s)
    {
        Prim(PrimitiveType.Cylinder, "Bell", parent,
            new Vector3(xz.x, 0.72f * s, xz.z),
            new Vector3(0.95f * s, 1.0f * s, 0.95f * s), heat);
        Prim(PrimitiveType.Cylinder, "Exit", parent,
            new Vector3(xz.x, 0.06f * s, xz.z),
            new Vector3(1.28f * s, 0.14f * s, 1.28f * s), metal);
        Prim(PrimitiveType.Cylinder, "Throat", parent,
            new Vector3(xz.x, 1.4f * s, xz.z),
            new Vector3(0.42f * s, 0.22f * s, 0.42f * s), copper);
        Prim(PrimitiveType.Cylinder, "Gimbal", parent,
            new Vector3(xz.x, 1.65f * s, xz.z),
            new Vector3(0.55f * s, 0.12f * s, 0.55f * s), metal);
    }

    static void BuildEngineFX(Transform visual)
    {
        var flameGo = new GameObject("EngineFlame");
        flameGo.transform.SetParent(visual, false);
        flameGo.transform.localPosition = new Vector3(0f, -1.5f, 0f);
        flameGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var flame = flameGo.AddComponent<ParticleSystem>();
        ConfigureFlame(flame);

        // Дим нижче сопел, щоб не обволікав корпус
        var smokeGo = new GameObject("EngineSmoke");
        smokeGo.transform.SetParent(visual, false);
        smokeGo.transform.localPosition = new Vector3(0f, -4.5f, 0f);
        smokeGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var smoke = smokeGo.AddComponent<ParticleSystem>();
        ConfigureSmoke(smoke);

        var sparkGo = new GameObject("EngineSparks");
        sparkGo.transform.SetParent(visual, false);
        sparkGo.transform.localPosition = new Vector3(0f, -1.1f, 0f);
        sparkGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var sparks = sparkGo.AddComponent<ParticleSystem>();
        ConfigureSparks(sparks);

        var lightGo = new GameObject("EngineLight");
        lightGo.transform.SetParent(visual, false);
        lightGo.transform.localPosition = new Vector3(0f, -2.4f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.6f, 0.8f, 1f);
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
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(45f, 95f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.6f, 5.2f);
        // Raptor-inspired: cool white-blue core → warm amber outer
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.95f, 1f, 1f),
            new Color(1f, 0.45f, 0.12f, 0.92f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 750;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = 1.7f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.9f, 0.97f, 1f), 0f),
                new GradientColorKey(new Color(0.55f, 0.8f, 1f), 0.18f),
                new GradientColorKey(new Color(1f, 0.55f, 0.15f), 0.45f),
                new GradientColorKey(new Color(0.55f, 0.12f, 0.04f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.25f),
                new GradientAlphaKey(0.45f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.4f, 1f, 1.75f));

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.Particle(new Color(0.7f, 0.85f, 1f, 1f));
    }

    /// <summary>
    /// Легкий шлейф диму вниз від сопел — не закриває корпус.
    /// Мало частинок, низька opacity, швидкий розсіювач.
    /// </summary>
    static void ConfigureSmoke(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.95f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(18f, 40f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 3.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.55f, 0.55f, 0.58f, 0.14f),
            new Color(0.3f, 0.3f, 0.34f, 0.06f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;
        main.gravityModifier = 0.05f; // падає вниз, не обволікає ракету

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;   // вузький конус вниз
        shape.radius = 1.0f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.6f, 0.6f, 0.62f), 0f),
                new GradientColorKey(new Color(0.25f, 0.25f, 0.28f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.12f, 0f),
                new GradientAlphaKey(0.05f, 0.35f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = g;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.4f));

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = VisualMaterials.Particle(new Color(0.45f, 0.45f, 0.48f, 0.12f));
        // Не затінює ракету
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
    }

    static void ConfigureSparks(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(55f, 130f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.42f);
        main.startColor = new Color(1f, 0.88f, 0.45f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 250;
        main.gravityModifier = 0.22f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 16f;
        shape.radius = 1.25f;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Stretch;
        rend.lengthScale = 2.8f;
        rend.velocityScale = 0.09f;
        rend.sharedMaterial = VisualMaterials.Particle(new Color(1f, 0.82f, 0.28f, 1f));
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
            // Тонкі кільця/декорі без тіней — менше «рваних» ліній
            bool thin = scale.y < 0.2f || Mathf.Min(scale.x, scale.z) < 0.5f;
            if (thin || name.Contains("Ring") || name.Contains("Stripe") || name.Contains("Letter"))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
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
