using UnityEngine;

/// <summary>
/// Середовище посадки: земля, сітка, pad, освітлення, туман, небо.
/// </summary>
public static class EnvironmentBuilder
{
    public static void Build()
    {
        SetupLighting();
        SetupFog();
        if (GameObject.Find("EnvironmentRoot") != null) return;

        var root = new GameObject("EnvironmentRoot");

        // Vast ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(root.transform, false);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(400f, 1f, 400f); // 4000x4000 m
        Object.Destroy(ground.GetComponent<Collider>());
        ApplyMat(ground, new Color(0.12f, 0.14f, 0.11f), 0.1f, 0.15f);

        // Ocean-ish ring (darker outer)
        var outer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        outer.name = "TerrainRing";
        outer.transform.SetParent(root.transform, false);
        outer.transform.position = new Vector3(0f, -2f, 0f);
        outer.transform.localScale = new Vector3(8000f, 1f, 8000f);
        Object.Destroy(outer.GetComponent<Collider>());
        ApplyMat(outer, new Color(0.05f, 0.08f, 0.12f), 0.05f, 0.2f);

        BuildLandingPad(root.transform);
        BuildGridMarkers(root.transform);
        BuildAltitudeBeacons(root.transform);
        BuildHorizonHills(root.transform);
    }

    static void BuildLandingPad(Transform parent)
    {
        var old = GameObject.Find("LandingPad");
        if (old != null) Object.Destroy(old);

        var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "LandingPad";
        pad.transform.SetParent(parent, false);
        pad.transform.position = new Vector3(0f, 0.15f, 0f);
        pad.transform.localScale = new Vector3(55f, 0.2f, 55f);
        Object.Destroy(pad.GetComponent<Collider>());
        ApplyMat(pad, new Color(0.18f, 0.2f, 0.24f), 0.3f, 0.4f);

        // Concentric rings
        float[] rings = { 0.9f, 0.65f, 0.4f, 0.18f };
        foreach (float r in rings)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "PadRing";
            ring.transform.SetParent(pad.transform, false);
            ring.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            ring.transform.localScale = new Vector3(r, 0.08f, r);
            Object.Destroy(ring.GetComponent<Collider>());
            ApplyMat(ring, new Color(0.2f, 0.85f, 1f), 0.2f, 0.5f, new Color(0.1f, 0.4f, 0.5f) * (1.2f - r));
        }

        // Cross
        foreach (var axis in new[] { true, false })
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = axis ? "PadCrossX" : "PadCrossZ";
            bar.transform.SetParent(pad.transform, false);
            bar.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            bar.transform.localScale = axis
                ? new Vector3(0.85f, 0.05f, 0.04f)
                : new Vector3(0.04f, 0.05f, 0.85f);
            Object.Destroy(bar.GetComponent<Collider>());
            ApplyMat(bar, new Color(1f, 0.75f, 0.2f), 0.2f, 0.5f, new Color(0.5f, 0.3f, 0.05f));
        }

        // Corner lights
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            var lightGo = new GameObject($"PadLight_{i}");
            lightGo.transform.SetParent(pad.transform, false);
            lightGo.transform.localPosition = new Vector3(Mathf.Sin(a) * 0.42f, 3f, Mathf.Cos(a) * 0.42f);
            var pl = lightGo.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(0.3f, 0.9f, 1f);
            pl.intensity = 8f;
            pl.range = 80f;
        }
    }

    static void BuildGridMarkers(Transform parent)
    {
        var gridRoot = new GameObject("DistanceGrid");
        gridRoot.transform.SetParent(parent, false);
        for (int x = -5; x <= 5; x++)
        {
            for (int z = -5; z <= 5; z++)
            {
                if (x == 0 && z == 0) continue;
                if (Mathf.Abs(x) + Mathf.Abs(z) > 7) continue;
                var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
                m.name = $"Grid_{x}_{z}";
                m.transform.SetParent(gridRoot.transform, false);
                m.transform.position = new Vector3(x * 100f, 0.5f, z * 100f);
                m.transform.localScale = new Vector3(4f, 1f, 4f);
                Object.Destroy(m.GetComponent<Collider>());
                bool major = x % 2 == 0 && z % 2 == 0;
                ApplyMat(m, major
                    ? new Color(0.25f, 0.35f, 0.4f)
                    : new Color(0.15f, 0.2f, 0.22f), 0.1f, 0.2f);
            }
        }
    }

    static void BuildAltitudeBeacons(Transform parent)
    {
        // Vertical reference tower near pad (helps depth perception)
        var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tower.name = "RefTower";
        tower.transform.SetParent(parent, false);
        tower.transform.position = new Vector3(80f, 50f, 80f);
        tower.transform.localScale = new Vector3(2f, 100f, 2f);
        Object.Destroy(tower.GetComponent<Collider>());
        ApplyMat(tower, new Color(0.3f, 0.32f, 0.35f), 0.4f, 0.3f);

        float[] marks = { 100f, 250f, 500f, 1000f, 1500f, 2000f, 2500f };
        foreach (float h in marks)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = $"AltMark_{h}";
            ring.transform.SetParent(parent, false);
            ring.transform.position = new Vector3(0f, h, 0f);
            ring.transform.localScale = new Vector3(120f, 0.4f, 120f);
            Object.Destroy(ring.GetComponent<Collider>());
            float t = h / 2500f;
            ApplyMat(ring, Color.Lerp(new Color(0.2f, 0.8f, 0.5f), new Color(0.2f, 0.5f, 1f), t),
                0.1f, 0.3f, Color.Lerp(new Color(0.05f, 0.2f, 0.1f), new Color(0.05f, 0.1f, 0.3f), t));
        }
    }

    static void BuildHorizonHills(Transform parent)
    {
        var rng = new System.Random(42);
        for (int i = 0; i < 24; i++)
        {
            float a = i / 24f * Mathf.PI * 2f;
            float d = 1500f + (float)rng.NextDouble() * 800f;
            var hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hill.name = $"Hill_{i}";
            hill.transform.SetParent(parent, false);
            float s = 80f + (float)rng.NextDouble() * 200f;
            hill.transform.position = new Vector3(Mathf.Cos(a) * d, s * 0.25f, Mathf.Sin(a) * d);
            hill.transform.localScale = new Vector3(s * 2f, s * 0.6f, s * 2f);
            Object.Destroy(hill.GetComponent<Collider>());
            ApplyMat(hill, new Color(0.1f, 0.12f, 0.1f), 0.05f, 0.1f);
        }
    }

    static void SetupLighting()
    {
        // Main directional
        var sun = Object.FindFirstObjectByType<Light>();
        if (sun == null || sun.type != LightType.Directional)
        {
            var go = new GameObject("Sun");
            sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.color = new Color(1f, 0.95f, 0.88f);
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(38f, -35f, 0f);

        // Fill
        if (GameObject.Find("FillLight") == null)
        {
            var fill = new GameObject("FillLight");
            var fl = fill.AddComponent<Light>();
            fl.type = LightType.Directional;
            fl.color = new Color(0.4f, 0.55f, 0.75f);
            fl.intensity = 0.35f;
            fl.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(15f, 140f, 0f);
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.15f, 0.22f, 0.35f);
        RenderSettings.ambientEquatorColor = new Color(0.12f, 0.14f, 0.16f);
        RenderSettings.ambientGroundColor = new Color(0.05f, 0.05f, 0.06f);
        RenderSettings.reflectionIntensity = 0.4f;
    }

    static void SetupFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.35f, 0.48f, 0.62f);
        RenderSettings.fogDensity = 0.00018f;

        var cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.62f, 0.82f);
            cam.farClipPlane = 12000f;
            cam.nearClipPlane = 0.5f;
            cam.fieldOfView = 50f;
            if (!cam.CompareTag("MainCamera"))
            {
                try { cam.tag = "MainCamera"; } catch { /* tag may not exist in build */ }
            }
        }
    }

    static void ApplyMat(GameObject go, Color color, float metallic, float smooth, Color? emission = null)
    {
        var r = go.GetComponent<MeshRenderer>();
        if (r == null) return;
        var shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard");
        if (shader == null) return;
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
        if (emission.HasValue && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
        }
        r.sharedMaterial = mat;
    }
}
