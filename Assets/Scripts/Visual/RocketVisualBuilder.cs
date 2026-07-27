using UnityEngine;

/// <summary>
/// Будує читабельну 3D-модель ракетоносія з примітивів (F9-class scale ~42 м).
/// Ховає дефолтний маленький циліндр сцени.
/// </summary>
public static class RocketVisualBuilder
{
    const float Height = 42f;
    const float Radius = 1.85f;

    public static void Build(RocketPhysics rocket)
    {
        if (rocket == null) return;
        Transform root = rocket.transform;

        // Scene rocket was a tiny stretched cylinder (scale 1.5×6×1.5) — reset
        root.localScale = Vector3.one;

        // Disable default mesh on root (tiny cylinder)
        var mr = root.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
        var mf = root.GetComponent<MeshFilter>();
        if (mf != null) mf.sharedMesh = null;

        // Kinematic RB — physics is custom RK4
        var rb = root.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
        }

        var existing = root.Find("Visual");
        if (existing != null) Object.Destroy(existing.gameObject);

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        var white = MakeMat(new Color(0.92f, 0.93f, 0.95f), 0.35f, 0.55f);
        var black = MakeMat(new Color(0.08f, 0.09f, 0.11f), 0.45f, 0.25f);
        var metal = MakeMat(new Color(0.55f, 0.58f, 0.62f), 0.7f, 0.45f);
        var accent = MakeMat(new Color(0.15f, 0.55f, 0.85f), 0.4f, 0.6f, new Color(0.05f, 0.25f, 0.4f));
        var heat = MakeMat(new Color(0.25f, 0.18f, 0.12f), 0.5f, 0.3f);
        var glow = MakeMat(new Color(1f, 0.55f, 0.15f), 0.2f, 0.1f, new Color(2f, 0.8f, 0.1f));

        // Main body
        var body = Prim(PrimitiveType.Cylinder, "Body", visual.transform,
            new Vector3(0f, Height * 0.42f, 0f),
            new Vector3(Radius * 2f, Height * 0.38f, Radius * 2f), white);

        // Interstage band
        Prim(PrimitiveType.Cylinder, "Band", visual.transform,
            new Vector3(0f, Height * 0.62f, 0f),
            new Vector3(Radius * 2.05f, Height * 0.02f, Radius * 2.05f), accent);

        // Nose / upper
        Prim(PrimitiveType.Cylinder, "Upper", visual.transform,
            new Vector3(0f, Height * 0.78f, 0f),
            new Vector3(Radius * 1.7f, Height * 0.12f, Radius * 1.7f), white);

        var nose = Prim(PrimitiveType.Sphere, "Nose", visual.transform,
            new Vector3(0f, Height * 0.92f, 0f),
            new Vector3(Radius * 1.7f, Height * 0.14f, Radius * 1.7f), white);

        // Grid fins (4)
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            var fin = Prim(PrimitiveType.Cube, $"GridFin_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * (Radius + 1.2f), Height * 0.72f, Mathf.Cos(a) * (Radius + 1.2f)),
                new Vector3(0.15f, 2.2f, 2.8f), metal);
            fin.transform.localRotation = Quaternion.Euler(0f, i * 90f, 15f);
        }

        // Landing legs (4)
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            var leg = Prim(PrimitiveType.Cube, $"Leg_{i}", visual.transform,
                new Vector3(Mathf.Sin(a) * (Radius + 2.5f), Height * 0.08f, Mathf.Cos(a) * (Radius + 2.5f)),
                new Vector3(0.35f, Height * 0.16f, 0.35f), black);
            leg.transform.localRotation = Quaternion.Euler(25f * Mathf.Cos(a), 0f, -25f * Mathf.Sin(a));
        }

        // Engine section
        Prim(PrimitiveType.Cylinder, "EngineBay", visual.transform,
            new Vector3(0f, Height * 0.06f, 0f),
            new Vector3(Radius * 2.1f, Height * 0.05f, Radius * 2.1f), black);

        // Nozzles (center + ring of 8)
        CreateNozzle(visual.transform, Vector3.zero, glow, heat, 1.1f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            CreateNozzle(visual.transform,
                new Vector3(Mathf.Sin(a) * 1.3f, 0f, Mathf.Cos(a) * 1.3f),
                glow, heat, 0.7f);
        }

        // Stripe markings
        Prim(PrimitiveType.Cylinder, "Stripe", visual.transform,
            new Vector3(0f, Height * 0.35f, 0f),
            new Vector3(Radius * 2.02f, 0.4f, Radius * 2.02f), black);

        // Reposition engine particles under nozzles
        RebindParticles(root, visual.transform);

        // Collider match
        var cap = root.GetComponent<CapsuleCollider>();
        if (cap != null)
        {
            cap.direction = 1;
            cap.height = Height;
            cap.radius = Radius * 1.1f;
            cap.center = new Vector3(0f, Height * 0.45f, 0f);
            cap.enabled = false; // custom physics
        }
    }

    static void CreateNozzle(Transform parent, Vector3 offset, Material glow, Material heat, float scale)
    {
        var n = Prim(PrimitiveType.Cylinder, "Nozzle", parent,
            new Vector3(offset.x, 0.4f * scale, offset.z),
            new Vector3(1.1f * scale, 1.4f * scale, 1.1f * scale), heat);
        n.transform.localScale = new Vector3(1.1f * scale, 0.9f * scale, 1.1f * scale);

        Prim(PrimitiveType.Sphere, "PlumeHint", parent,
            new Vector3(offset.x, -0.3f * scale, offset.z),
            Vector3.one * (0.9f * scale), glow);
    }

    static void RebindParticles(Transform rocket, Transform visual)
    {
        var flame = rocket.Find("EngineFlame");
        var smoke = rocket.Find("EngineSmoke");
        if (flame != null)
        {
            flame.SetParent(visual, true);
            flame.localPosition = new Vector3(0f, -1.5f, 0f);
            flame.localScale = Vector3.one * 3f;
            var ps = flame.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startSizeMultiplier = 3.5f;
                main.startSpeedMultiplier = 1.5f;
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.75f, 0.25f, 1f));
                var em = ps.emission;
                em.rateOverTimeMultiplier = 2f;
            }
        }
        if (smoke != null)
        {
            smoke.SetParent(visual, true);
            smoke.localPosition = new Vector3(0f, -2.5f, 0f);
            smoke.localScale = Vector3.one * 4f;
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
        if (r != null) r.sharedMaterial = mat;
        return go;
    }

    static Material MakeMat(Color color, float metallic, float smooth, Color? emission = null)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard")
                     ?? Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smooth);
        if (emission.HasValue)
        {
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
            }
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        return mat;
    }
}
