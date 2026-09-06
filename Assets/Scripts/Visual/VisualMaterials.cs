using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Materials for procedural geometry. Pad markings use Unlit opaque with bright BaseColor.
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
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
        SetColor(mat, color);
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
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f); // alpha
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
        mat.renderQueue = 3000;
        return mat;
    }

    /// <summary>ParticleAdditive</summary>
    public static Material ParticleAdditive(Color tint)
    {
        var mat = new Material(ParticleShader);
        SetColor(mat, tint);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 1f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", tint * 1.6f);
        }
        mat.renderQueue = 3000;
        return mat;
    }

    public static Material Plume(Color hot, Color mid, Color cool, float intensity)
    {
        var sh = Shader.Find("Betelgeuse/EnginePlume");
        if (sh == null)
            return ParticleAdditive(mid);

        // Higher-res ramp: hot core -> mid body -> cool tip -> translucent smoke.
        var tex = new Texture2D(256, 4, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.name = "PlumeRamp";
        for (int i = 0; i < 256; i++)
        {
            float u = i / 255f;
            Color c;
            if (u < 0.12f)
                c = Color.Lerp(hot, mid, Smooth01(u / 0.12f));
            else if (u < 0.38f)
                c = Color.Lerp(mid, cool, Smooth01((u - 0.12f) / 0.26f));
            else if (u < 0.70f)
            {
                var deep = new Color(
                    cool.r * 0.55f + 0.08f,
                    cool.g * 0.28f + 0.02f,
                    cool.b * 0.12f,
                    0.65f);
                c = Color.Lerp(cool, deep, Smooth01((u - 0.38f) / 0.32f));
            }
            else
            {
                var deep = new Color(
                    cool.r * 0.55f + 0.08f,
                    cool.g * 0.28f + 0.02f,
                    cool.b * 0.12f,
                    0.65f);
                var smoke = new Color(0.22f, 0.16f, 0.10f, 0.0f);
                c = Color.Lerp(deep, smoke, Smooth01((u - 0.70f) / 0.30f));
            }
            for (int y = 0; y < 4; y++) tex.SetPixel(i, y, c);
        }
        tex.Apply(false, false);

        var mat = new Material(sh);
        mat.SetTexture("_MainTex", tex);
        mat.SetFloat("_Intensity", intensity);
        mat.SetFloat("_Flicker", 1f);
        mat.SetFloat("_NoiseAmt", 0.42f);
        if (mat.HasProperty("_Softness")) mat.SetFloat("_Softness", 1.35f);
        if (mat.HasProperty("_EdgePower")) mat.SetFloat("_EdgePower", 1.75f);
        if (mat.HasProperty("_TipSmoke")) mat.SetFloat("_TipSmoke", 0.58f);
        mat.renderQueue = 3100;
        return mat;
    }

    static float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
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

    /// <summary>ApplyUnlit</summary>
    public static void ApplyUnlit(GameObject go, Color color, Color? emission = null)
    {
        Color c = color;
        if (emission.HasValue)
            c = Color.Lerp(color, emission.Value, 0.55f);
        c.a = 1f;
        c = new Color(
            Mathf.Clamp01(c.r * 1.15f + 0.08f),
            Mathf.Clamp01(c.g * 1.15f + 0.08f),
            Mathf.Clamp01(c.b * 1.15f + 0.08f), 1f);
        Apply(go, Unlit(c, emission ?? c));
    }

    /// <summary>ApplyBright</summary>
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

