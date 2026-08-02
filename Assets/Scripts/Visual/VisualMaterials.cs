using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Матеріали для процедурної геометрії. Pad-маркування — через Unlit opaque
/// з яскравим BaseColor (emission у URP Unlit часто «не світить» без bloom).
/// </summary>
public static class VisualMaterials
{
    static Shader lit;
    static Shader unlit;
    static Shader particles;

    public static Shader LitShader =>
        lit ??= Shader.Find("Universal Render Pipeline/Lit")
             ?? Shader.Find("Standard")
             ?? Shader.Find("Sprites/Default");

    public static Shader UnlitShader =>
        unlit ??= Shader.Find("Universal Render Pipeline/Unlit")
               ?? Shader.Find("Unlit/Color")
               ?? Shader.Find("Sprites/Default");

    public static Shader ParticleShader =>
        particles ??= Shader.Find("Universal Render Pipeline/Particles/Unlit")
                   ?? Shader.Find("Particles/Standard Unlit")
                   ?? Shader.Find("Sprites/Default");

    public static Material Lit(Color color, float metallic = 0.3f, float smooth = 0.5f, Color? emission = null)
    {
        var mat = new Material(LitShader);
        SetColor(mat, color);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smooth);
        if (emission.HasValue) SetEmission(mat, emission.Value);
        return mat;
    }

    public static Material Unlit(Color color, Color? emission = null)
    {
        var mat = new Material(UnlitShader);
        // Opaque solid — гарантовано видно
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
        SetColor(mat, color);
        // Дублюємо в emission якщо є (для bloom), але base color уже яскравий
        if (emission.HasValue && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
        }
        return mat;
    }

    public static Material Particle(Color tint)
    {
        var mat = new Material(ParticleShader);
        SetColor(mat, tint);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        return mat;
    }

    public static void Apply(GameObject go, Material mat)
    {
        var r = go.GetComponent<MeshRenderer>();
        if (r == null) return;
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    public static void Apply(GameObject go, Color color, float metallic = 0.3f, float smooth = 0.5f, Color? emission = null)
        => Apply(go, Lit(color, metallic, smooth, emission));

    /// <summary>Яскраве маркування pad — solid unlit, видно з 2 км.</summary>
    public static void ApplyUnlit(GameObject go, Color color, Color? emission = null)
    {
        // Base color = max(color, emission) щоб не було «чорного unlit»
        Color c = color;
        if (emission.HasValue)
            c = Color.Lerp(color, emission.Value, 0.55f);
        c.a = 1f;
        // Підсилення яскравості
        c = new Color(
            Mathf.Clamp01(c.r * 1.15f + 0.08f),
            Mathf.Clamp01(c.g * 1.15f + 0.08f),
            Mathf.Clamp01(c.b * 1.15f + 0.08f), 1f);
        Apply(go, Unlit(c, emission ?? c));
    }

    /// <summary>Яскравий Lit бетон (реагує на сонце + ambient).</summary>
    public static void ApplyBright(GameObject go, Color color)
    {
        Color c = new Color(
            Mathf.Clamp01(color.r + 0.12f),
            Mathf.Clamp01(color.g + 0.12f),
            Mathf.Clamp01(color.b + 0.12f), 1f);
        Apply(go, Lit(c, 0.15f, 0.35f, c * 0.25f));
        var r = go.GetComponent<MeshRenderer>();
        if (r != null)
        {
            r.shadowCastingMode = ShadowCastingMode.On;
            r.receiveShadows = true;
        }
    }

    static void SetColor(Material mat, Color color)
    {
        color.a = 1f;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
    }

    static void SetEmission(Material mat, Color emission)
    {
        if (!mat.HasProperty("_EmissionColor")) return;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emission);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    }
}
