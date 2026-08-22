using System.IO;
using UnityEngine;

/// <summary>
/// NASA CGI Moon Kit surface tiles (Resources/Moon).
/// Loaded readable at runtime so LunarTerrainMesh can bake them into the disk maps.
/// Source: https://svs.gsfc.nasa.gov/4720/ (public domain / NASA).
/// </summary>
public static class MoonSurfaceAssets
{
    const string RelDir = "Resources/Moon";

    static Texture2D albedo;
    static Texture2D normal;
    static Texture2D height;
    static Texture2D globeColor;
    static bool tried;

    public static bool Available
    {
        get
        {
            EnsureLoaded();
            return albedo != null;
        }
    }

    public static Texture2D Albedo
    {
        get { EnsureLoaded(); return albedo; }
    }

    public static Texture2D Normal
    {
        get { EnsureLoaded(); return normal; }
    }

    public static Texture2D Height
    {
        get { EnsureLoaded(); return height; }
    }

    /// <summary>Full-disk LROC color (equirectangular) for a sky Moon sphere.</summary>
    public static Texture2D GlobeColor
    {
        get { EnsureLoaded(); return globeColor; }
    }

    public static void EnsureLoaded()
    {
        if (tried) return;
        tried = true;

        albedo = LoadReadable("LunarSurfaceAlbedo.png", "Moon/LunarSurfaceAlbedo", srgb: true);
        normal = LoadReadable("LunarSurfaceNormal.png", "Moon/LunarSurfaceNormal", srgb: false);
        height = LoadReadable("LunarSurfaceHeight.png", "Moon/LunarSurfaceHeight", srgb: false);
        globeColor = LoadReadable("LrocColor2k.png", "Moon/LrocColor2k", srgb: true)
                  ?? LoadReadable("LrocColor4k.png", "Moon/LrocColor4k", srgb: true);

        if (albedo != null)
        {
            albedo.wrapMode = TextureWrapMode.Repeat;
            albedo.filterMode = FilterMode.Trilinear;
            albedo.anisoLevel = 8;
        }
        if (normal != null)
        {
            normal.wrapMode = TextureWrapMode.Repeat;
            normal.filterMode = FilterMode.Trilinear;
            normal.anisoLevel = 8;
        }
        if (height != null)
        {
            height.wrapMode = TextureWrapMode.Repeat;
            height.filterMode = FilterMode.Bilinear;
        }
        if (globeColor != null)
        {
            globeColor.wrapMode = TextureWrapMode.Clamp;
            globeColor.filterMode = FilterMode.Trilinear;
        }

        if (albedo != null)
            Debug.Log($"[MoonSurface] gray regolith ready albedo={albedo.width}x{albedo.height}"
                      + (normal != null ? $" normal={normal.width}" : "")
                      + (globeColor != null ? $" globe={globeColor.width}" : ""));
        else
            Debug.LogWarning("[MoonSurface] tiles missing under Assets/Resources/Moon/");
    }

    /// <summary>
    /// World-XZ → tiled UV. tileMeters = how many meters one texture repeat covers.
    /// </summary>
    public static Vector2 WorldToUv(float wx, float wz, float tileMeters = 1400f)
    {
        // Axis-aligned UV — no skew (skew looked crooked when tiled)
        float inv = 1f / Mathf.Max(1f, tileMeters);
        return new Vector2(wx * inv + 0.5f, wz * inv + 0.5f);
    }

    public static Color SampleAlbedo(float wx, float wz, float tileMeters = 1400f)
    {
        if (albedo == null) return new Color(0.50f, 0.505f, 0.52f, 1f);
        Vector2 uv = WorldToUv(wx, wz, tileMeters);
        return albedo.GetPixelBilinear(uv.x, uv.y);
    }

    public static float SampleHeight01(float wx, float wz, float tileMeters = 1400f)
    {
        if (height == null) return 0.5f;
        Vector2 uv = WorldToUv(wx, wz, tileMeters);
        return height.GetPixelBilinear(uv.x, uv.y).r;
    }

    public static Vector3 SampleNormalTS(float wx, float wz, float tileMeters = 1400f)
    {
        if (normal == null) return new Vector3(0f, 0f, 1f);
        Vector2 uv = WorldToUv(wx, wz, tileMeters);
        Color c = normal.GetPixelBilinear(uv.x, uv.y);
        return new Vector3(c.r * 2f - 1f, c.g * 2f - 1f, c.b * 2f - 1f);
    }

    static Texture2D LoadReadable(string fileName, string resourcesPath, bool srgb)
    {
        // Prefer raw PNG bytes so we always get a readable Texture2D (no import flag dependency).
        string path = Path.Combine(Application.dataPath, RelDir, fileName);
        if (File.Exists(path))
        {
            try
            {
                byte[] data = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, srgb ? TextureFormat.RGB24 : TextureFormat.RGBA32, true, !srgb);
                if (tex.LoadImage(data, markNonReadable: false))
                {
                    tex.name = Path.GetFileNameWithoutExtension(fileName);
                    return tex;
                }
                Object.Destroy(tex);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MoonSurface] LoadImage failed {fileName}: {e.Message}");
            }
        }

        var res = Resources.Load<Texture2D>(resourcesPath);
        if (res == null) return null;
        // Duplicate into a readable copy when possible
        try
        {
            var copy = new Texture2D(res.width, res.height, TextureFormat.RGBA32, true, !srgb);
            var tmp = RenderTexture.GetTemporary(res.width, res.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(res, tmp);
            var prev = RenderTexture.active;
            RenderTexture.active = tmp;
            copy.ReadPixels(new Rect(0, 0, res.width, res.height), 0, 0);
            copy.Apply(true, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(tmp);
            copy.name = res.name + "_Readable";
            copy.wrapMode = res.wrapMode;
            return copy;
        }
        catch
        {
            return res;
        }
    }
}
