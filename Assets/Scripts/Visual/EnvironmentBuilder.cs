using UnityEngine;

/// <summary>
/// Космічне середовище: глибокий космос, зорі, м'який горизонт, нічний landing pad.
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
        BuildDistanceGrid(root.transform);
        var starPs = BuildStarField(root.transform);
        BuildHorizonGlow(root.transform);
        BuildPadBeacons(root.transform);
        BuildLandingLights(root.transform);

        var amb = SpaceAmbience.Ensure();
        amb.Bind(root.transform, starPs, sun);
    }

    static void BuildGround(Transform parent)
    {
        var ground = Prim(PrimitiveType.Plane, "Ground", parent,
            Vector3.zero, new Vector3(600f, 1f, 600f));
        VisualMaterials.Apply(ground, new Color(0.035f, 0.04f, 0.055f), 0.12f, 0.1f);

        // Subtle terrain mottling patches
        var rng = new System.Random(11);
        for (int i = 0; i < 40; i++)
        {
            float x = ((float)rng.NextDouble() - 0.5f) * 1800f;
            float z = ((float)rng.NextDouble() - 0.5f) * 1800f;
            if (x * x + z * z < 80f * 80f) continue;
            var patch = Prim(PrimitiveType.Cylinder, "TerrainPatch", parent,
                new Vector3(x, -0.2f, z),
                new Vector3(40f + (float)rng.NextDouble() * 80f, 0.15f, 40f + (float)rng.NextDouble() * 80f));
            float g = 0.03f + (float)rng.NextDouble() * 0.04f;
            VisualMaterials.Apply(patch, new Color(g, g * 1.05f, g * 1.15f), 0.08f, 0.08f);
        }

        var outer = Prim(PrimitiveType.Cylinder, "VoidDisc", parent,
            new Vector3(0f, -6f, 0f), new Vector3(14000f, 2f, 14000f));
        VisualMaterials.Apply(outer, new Color(0.01f, 0.012f, 0.03f), 0.02f, 0.05f);
    }

    static void BuildLandingPad(Transform parent)
    {
        var old = GameObject.Find("LandingPad");
        if (old != null) Object.Destroy(old);

        var padRoot = new GameObject("LandingPad");
        padRoot.transform.SetParent(parent, false);

        // Outer apron
        var apron = Prim(PrimitiveType.Cylinder, "PadApron", padRoot.transform,
            new Vector3(0f, 0.08f, 0f), new Vector3(90f, 0.12f, 90f));
        VisualMaterials.Apply(apron, new Color(0.08f, 0.09f, 0.12f), 0.35f, 0.3f);

        // Main deck
        var deck = Prim(PrimitiveType.Cylinder, "PadDeck", padRoot.transform,
            new Vector3(0f, 0.22f, 0f), new Vector3(58f, 0.16f, 58f));
        VisualMaterials.Apply(deck, new Color(0.14f, 0.15f, 0.18f), 0.5f, 0.4f);

        // Chequered inner zone (alternating wedges via cubes ring)
        var target = Prim(PrimitiveType.Cylinder, "PadTarget", padRoot.transform,
            new Vector3(0f, 0.35f, 0f), new Vector3(24f, 0.05f, 24f));
        VisualMaterials.Apply(target, new Color(0.2f, 0.22f, 0.26f), 0.4f, 0.45f);

        // Neon rings
        float[] rs = { 0.95f, 0.62f, 0.32f, 0.12f };
        Color[] cols =
        {
            new Color(0.2f, 0.75f, 1f),
            new Color(0.25f, 0.85f, 1f),
            new Color(0.4f, 0.9f, 1f),
            new Color(1f, 0.8f, 0.3f)
        };
        for (int i = 0; i < rs.Length; i++)
        {
            var ring = Prim(PrimitiveType.Cylinder, $"PadRing_{i}", padRoot.transform,
                new Vector3(0f, 0.4f + i * 0.015f, 0f),
                new Vector3(58f * rs[i], 0.035f, 58f * rs[i]));
            VisualMaterials.Apply(ring, cols[i] * 0.5f, 0.15f, 0.7f, cols[i] * 0.55f);
        }

        // Cross
        foreach (var xAxis in new[] { true, false })
        {
            var bar = Prim(PrimitiveType.Cube, xAxis ? "CrossX" : "CrossZ", padRoot.transform,
                new Vector3(0f, 0.42f, 0f),
                xAxis ? new Vector3(50f, 0.06f, 1.1f) : new Vector3(1.1f, 0.06f, 50f));
            VisualMaterials.Apply(bar, new Color(1f, 0.82f, 0.25f), 0.2f, 0.55f,
                new Color(0.55f, 0.35f, 0.05f));
        }

        // Raised rim
        var rim = Prim(PrimitiveType.Cylinder, "PadRim", padRoot.transform,
            new Vector3(0f, 0.55f, 0f), new Vector3(59f, 0.35f, 59f));
        // Hollow-ish look: dark thin wall
        VisualMaterials.Apply(rim, new Color(0.25f, 0.28f, 0.32f), 0.55f, 0.35f);

        // Corner towers + spots
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            float r = 32f;
            Vector3 basePos = new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r);

            var pole = Prim(PrimitiveType.Cylinder, $"Tower_{i}", padRoot.transform,
                basePos + Vector3.up * 7f, new Vector3(0.7f, 7f, 0.7f));
            VisualMaterials.Apply(pole, new Color(0.3f, 0.32f, 0.36f), 0.6f, 0.35f);

            var head = Prim(PrimitiveType.Sphere, $"TowerHead_{i}", padRoot.transform,
                basePos + Vector3.up * 14.2f, Vector3.one * 1.4f);
            VisualMaterials.Apply(head, new Color(0.9f, 0.95f, 1f), 0.2f, 0.8f,
                new Color(0.4f, 0.7f, 1f) * 0.8f);

            var lightGo = new GameObject($"Spot_{i}");
            lightGo.transform.SetParent(padRoot.transform, false);
            lightGo.transform.position = basePos + Vector3.up * 14f;
            var spot = lightGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(0.85f, 0.92f, 1f);
            spot.intensity = 55f;
            spot.range = 100f;
            spot.spotAngle = 65f;
            spot.innerSpotAngle = 35f;
            spot.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.LookRotation(
                (-basePos + Vector3.up * -8f).normalized);
        }

        // Center fill
        var cGo = new GameObject("PadFill");
        cGo.transform.SetParent(padRoot.transform, false);
        cGo.transform.position = new Vector3(0f, 10f, 0f);
        var cl = cGo.AddComponent<Light>();
        cl.type = LightType.Point;
        cl.color = new Color(0.45f, 0.7f, 1f);
        cl.intensity = 18f;
        cl.range = 80f;
    }

    static void BuildLandingLights(Transform parent)
    {
        // Approach runway lights along Z
        for (int i = 1; i <= 12; i++)
        {
            float z = i * 35f;
            foreach (float x in new[] { -18f, 18f })
            {
                var lamp = Prim(PrimitiveType.Sphere, "ApproachLight", parent,
                    new Vector3(x, 0.6f, z), Vector3.one * 0.9f);
                Color c = i % 3 == 0
                    ? new Color(1f, 0.35f, 0.2f)
                    : new Color(0.3f, 0.9f, 1f);
                VisualMaterials.Apply(lamp, c * 0.6f, 0.1f, 0.7f, c * 0.9f);
            }
        }
    }

    static void BuildDistanceGrid(Transform parent)
    {
        var grid = new GameObject("DistanceGrid");
        grid.transform.SetParent(parent, false);

        float[] radii = { 75f, 150f, 300f, 500f };
        foreach (float radius in radii)
        {
            var ring = Prim(PrimitiveType.Cylinder, $"Ring_{radius}", grid.transform,
                new Vector3(0f, 0.04f, 0f),
                new Vector3(radius * 2f, 0.025f, radius * 2f));
            VisualMaterials.Apply(ring, new Color(0.06f, 0.12f, 0.18f), 0.1f, 0.2f,
                new Color(0.03f, 0.08f, 0.14f));
        }

        for (int d = 1; d <= 6; d++)
        {
            foreach (var dir in new[] {
                Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
            {
                var m = Prim(PrimitiveType.Cube, "Mark", grid.transform,
                    dir * (d * 100f) + Vector3.up * 0.35f,
                    new Vector3(2.5f, 0.7f, 2.5f));
                VisualMaterials.Apply(m, new Color(0.18f, 0.28f, 0.35f), 0.25f, 0.3f,
                    d == 1 ? new Color(0.05f, 0.15f, 0.2f) : null);
            }
        }
    }

    static ParticleSystem BuildStarField(Transform parent)
    {
        var starsRoot = new GameObject("StarField");
        starsRoot.transform.SetParent(parent, false);

        // Dense star particles
        var go = new GameObject("Stars");
        go.transform.SetParent(starsRoot.transform, false);
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.duration = 0.5f;
        main.startLifetime = 99999f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 5.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.65f, 0.75f, 1f, 0.85f),
            new Color(1f, 0.95f, 0.85f, 1f));
        main.maxParticles = 3500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3200) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 5000f;
        shape.radiusThickness = 0.4f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = VisualMaterials.Particle(Color.white);

        // Bright accent stars (mesh spheres with emission)
        var rng = new System.Random(3);
        for (int i = 0; i < 80; i++)
        {
            Vector3 dir = RandomOnSphere(rng);
            if (dir.y < -0.15f) dir.y = Mathf.Abs(dir.y) * 0.3f; // prefer upper hemisphere
            dir.Normalize();
            float dist = 2800f + (float)rng.NextDouble() * 1800f;
            float s = 2f + (float)rng.NextDouble() * 6f;
            var star = Prim(PrimitiveType.Sphere, "BrightStar", starsRoot.transform,
                dir * dist, Vector3.one * s);
            Color c = Color.Lerp(
                new Color(0.7f, 0.85f, 1f),
                new Color(1f, 0.9f, 0.7f),
                (float)rng.NextDouble());
            VisualMaterials.Apply(star, c, 0f, 0f, c * (1.2f + (float)rng.NextDouble()));
        }

        // Nebula clouds
        for (int i = 0; i < 8; i++)
        {
            Vector3 dir = RandomOnSphere(rng);
            dir.y = Mathf.Abs(dir.y) * 0.55f + 0.1f;
            dir.Normalize();
            float dist = 3200f + (float)rng.NextDouble() * 1200f;
            float s = 350f + (float)rng.NextDouble() * 700f;
            var neb = Prim(PrimitiveType.Sphere, $"Nebula_{i}", starsRoot.transform,
                dir * dist, Vector3.one * s);
            Color nc = i % 2 == 0
                ? new Color(0.18f, 0.08f, 0.4f)
                : new Color(0.06f, 0.14f, 0.38f);
            float b = 0.12f + (float)rng.NextDouble() * 0.12f;
            VisualMaterials.Apply(neb, nc * 0.35f, 0f, 0f, nc * b);
        }

        // Milky-way band (elongated)
        var band = Prim(PrimitiveType.Sphere, "MilkyWay", starsRoot.transform,
            new Vector3(0f, 800f, 4200f), new Vector3(5500f, 400f, 900f));
        VisualMaterials.Apply(band, new Color(0.12f, 0.12f, 0.2f) * 0.4f, 0f, 0f,
            new Color(0.15f, 0.16f, 0.28f) * 0.2f);

        return ps;
    }

    static void BuildHorizonGlow(Transform parent)
    {
        // Atmospheric limb
        var limb = Prim(PrimitiveType.Cylinder, "AtmoLimb", parent,
            new Vector3(0f, 18f, 0f), new Vector3(10000f, 28f, 10000f));
        VisualMaterials.Apply(limb, new Color(0.05f, 0.1f, 0.22f), 0f, 0f,
            new Color(0.08f, 0.15f, 0.35f) * 0.25f);

        // Soft sky dome (inside-facing look via large sphere)
        var dome = Prim(PrimitiveType.Sphere, "SkyDome", parent,
            Vector3.zero, Vector3.one * 11000f);
        VisualMaterials.Apply(dome, new Color(0.01f, 0.015f, 0.04f), 0f, 0f);
        // Flip normals not trivial on primitive — keep dark outer, stars outside anyway

        var rng = new System.Random(42);
        for (int i = 0; i < 20; i++)
        {
            float a = i / 20f * Mathf.PI * 2f;
            float d = 2000f + (float)rng.NextDouble() * 1000f;
            float s = 120f + (float)rng.NextDouble() * 260f;
            var hill = Prim(PrimitiveType.Sphere, $"Hill_{i}", parent,
                new Vector3(Mathf.Cos(a) * d, s * 0.18f, Mathf.Sin(a) * d),
                new Vector3(s * 2.4f, s * 0.45f, s * 2.4f));
            VisualMaterials.Apply(hill, new Color(0.025f, 0.03f, 0.045f), 0.05f, 0.08f);
        }
    }

    static void BuildPadBeacons(Transform parent)
    {
        var tower = Prim(PrimitiveType.Cylinder, "RefTower", parent,
            new Vector3(95f, 45f, 95f), new Vector3(1.2f, 90f, 1.2f));
        VisualMaterials.Apply(tower, new Color(0.4f, 0.42f, 0.48f), 0.55f, 0.4f);

        float[] marks = { 15f, 35f, 55f, 75f, 90f };
        foreach (float h in marks)
        {
            var mark = Prim(PrimitiveType.Cube, $"TMark_{h}", parent,
                new Vector3(95f, h, 95f), new Vector3(3.5f, 0.4f, 3.5f));
            VisualMaterials.Apply(mark, new Color(1f, 0.75f, 0.25f), 0.2f, 0.55f,
                new Color(0.4f, 0.2f, 0.04f));
        }

        var beacon = new GameObject("TowerBeacon");
        beacon.transform.SetParent(parent, false);
        beacon.transform.position = new Vector3(95f, 92f, 95f);
        var bl = beacon.AddComponent<Light>();
        bl.type = LightType.Point;
        bl.color = new Color(1f, 0.7f, 0.25f);
        bl.intensity = 8f;
        bl.range = 60f;
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
        sun.color = new Color(1f, 0.97f, 0.92f);
        sun.intensity = 1.45f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.85f;
        sun.transform.rotation = Quaternion.Euler(32f, -48f, 0f);

        EnsureDirLight("FillLight", new Color(0.3f, 0.4f, 0.75f), 0.32f, Quaternion.Euler(195f, 55f, 0f));
        EnsureDirLight("RimLight", new Color(0.45f, 0.6f, 1f), 0.28f, Quaternion.Euler(-20f, 155f, 0f));
        EnsureDirLight("GroundBounce", new Color(0.15f, 0.2f, 0.3f), 0.12f, Quaternion.Euler(90f, 0f, 0f));

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.05f, 0.06f, 0.12f);
        RenderSettings.ambientEquatorColor = new Color(0.03f, 0.04f, 0.07f);
        RenderSettings.ambientGroundColor = new Color(0.015f, 0.018f, 0.03f);
        RenderSettings.reflectionIntensity = 0.3f;
        RenderSettings.subtractiveShadowColor = new Color(0.02f, 0.03f, 0.06f);
    }

    static void EnsureDirLight(string name, Color color, float intensity, Quaternion rot)
    {
        var go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            go.AddComponent<Light>();
        }
        var l = go.GetComponent<Light>();
        l.type = LightType.Directional;
        l.color = color;
        l.intensity = intensity;
        l.shadows = LightShadows.None;
        go.transform.rotation = rot;
    }

    static void SetupSkyAndFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.015f, 0.02f, 0.05f);
        RenderSettings.fogDensity = 0.00006f;

        var cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.012f, 0.035f);
            cam.farClipPlane = 16000f;
            cam.nearClipPlane = 0.25f;
            cam.fieldOfView = 46f;
            try { if (!cam.CompareTag("MainCamera")) cam.tag = "MainCamera"; } catch { /* ignore */ }
        }
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        var col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        return go;
    }

    static Vector3 RandomOnSphere(System.Random rng)
    {
        float u = (float)rng.NextDouble();
        float v = (float)rng.NextDouble();
        float theta = 2f * Mathf.PI * u;
        float phi = Mathf.Acos(2f * v - 1f);
        return new Vector3(
            Mathf.Sin(phi) * Mathf.Cos(theta),
            Mathf.Cos(phi),
            Mathf.Sin(phi) * Mathf.Sin(theta));
    }
}
