using UnityEngine;

/// <summary>
/// Спільна фабрика URP/Standard матеріалів для процедурних ассетів.
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
        SetColor(mat, color);
        if (emission.HasValue) SetEmission(mat, emission.Value);
        return mat;
    }

    public static Material Particle(Color tint)
    {
        var mat = new Material(ParticleShader);
        SetColor(mat, tint);
        // Prefer additive-ish look when possible
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // transparent
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        return mat;
    }

    public static void Apply(GameObject go, Material mat)
    {
        var r = go.GetComponent<MeshRenderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    public static void Apply(GameObject go, Color color, float metallic = 0.3f, float smooth = 0.5f, Color? emission = null)
        => Apply(go, Lit(color, metallic, smooth, emission));

    static void SetColor(Material mat, Color color)
    {
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
