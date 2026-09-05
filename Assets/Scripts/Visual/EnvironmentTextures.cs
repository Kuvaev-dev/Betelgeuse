using UnityEngine;

/// <summary>
/// Poly Haven / procedural textures for Earth LZ composition (CC0 aerial grass + mud + rock).
/// </summary>
public static class EnvironmentTextures
{
    static bool _loaded;
    public static Texture2D GrassDiff { get; private set; }
    public static Texture2D GrassNor { get; private set; }
    public static Texture2D GrassRough { get; private set; }
    public static Texture2D MudDiff { get; private set; }
    public static Texture2D RockDiff { get; private set; }
    public static Texture2D CloudSoft { get; private set; }
    public static Texture2D[] CloudSprites { get; private set; }
    public static Texture2D SunGlow { get; private set; }

    // Unified day palette — earth + foliage share the same meadow family
    public static readonly Color SkyZenith = new Color(0.08f, 0.16f, 0.30f);
    public static readonly Color SkyHorizon = new Color(0.52f, 0.60f, 0.66f);
    public static readonly Color FogColor = new Color(0.48f, 0.56f, 0.60f);
    public static readonly Color SunYellow = new Color(1f, 0.86f, 0.28f);
    public static readonly Color CloudLit = new Color(0.88f, 0.90f, 0.94f);
    public static readonly Color CloudShade = new Color(0.62f, 0.66f, 0.72f);
    // Ground / nature shared (dark olive meadow — не neon Kenney)
    public static readonly Color MeadowTint = new Color(0.42f, 0.48f, 0.32f);
    public static readonly Color FoliageGreen = new Color(0.22f, 0.34f, 0.16f);
    public static readonly Color FoliageDark = new Color(0.14f, 0.24f, 0.11f);
    public static readonly Color BarkBrown = new Color(0.32f, 0.22f, 0.14f);
    public static readonly Color RockGrey = new Color(0.36f, 0.34f, 0.30f);
    public static readonly Color SoilBrown = new Color(0.34f, 0.28f, 0.18f);

    public static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        GrassDiff = MakeReadable(Resources.Load<Texture2D>("Textures/grass_diff"));
        GrassNor = Resources.Load<Texture2D>("Textures/grass_nor");
        GrassRough = Resources.Load<Texture2D>("Textures/grass_rough");
        MudDiff = MakeReadable(Resources.Load<Texture2D>("Textures/mud_diff"));
        RockDiff = Resources.Load<Texture2D>("Textures/rock_diff");

        // Poly Haven tonemapped sky → ready cloud sprites (multiple crops)
        var skyPhoto = MakeReadableScaled(Resources.Load<Texture2D>("Textures/sky_cloud"), 1280);
        if (skyPhoto != null)
        {
            CloudSprites = BuildCloudSpriteSet(skyPhoto, 6, 384);
            CloudSoft = CloudSprites.Length > 0 ? CloudSprites[0] : BuildCloudSoftTex(256);
        }
        else
        {
            CloudSoft = BuildCloudSoftTex(256);
            CloudSprites = new[] { CloudSoft, BuildCloudSoftTex(256), BuildCloudSoftTex(192) };
        }
        SunGlow = BuildBeautifulSunTex(512);

        Debug.Log($"[EnvTex] grass={(GrassDiff != null)} clouds={CloudSprites?.Length ?? 0} mud={(MudDiff != null)}");
    }

    static Texture2D MakeReadable(Texture2D src) => MakeReadableScaled(src, 0);

    static Texture2D MakeReadableScaled(Texture2D src, int maxSize)
    {
        if (src == null) return null;
        int w = src.width, h = src.height;
        if (maxSize > 0 && (w > maxSize || h > maxSize))
        {
            float s = maxSize / (float)Mathf.Max(w, h);
            w = Mathf.Max(1, Mathf.RoundToInt(w * s));
            h = Mathf.Max(1, Mathf.RoundToInt(h * s));
        }

        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var prev = RenderTexture.active;
        Graphics.Blit(src, rt);
        RenderTexture.active = rt;
        var copy = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
        copy.name = src.name + "_readable";
        copy.wrapMode = TextureWrapMode.Repeat;
        copy.filterMode = FilterMode.Bilinear;
        copy.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        copy.Apply(false, false);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return copy;
    }

    public static Material MakeGroundMaterial()
    {
        EnsureLoaded();
        var groundShader = Shader.Find("Betelgeuse/LandingRangeGround");
        Material mat;
        if (groundShader != null)
        {
            mat = new Material(groundShader);
            mat.name = "EarthGround_LandingRange";
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_TileMeters")) mat.SetFloat("_TileMeters", 16f);
            if (mat.HasProperty("_MacroStrength")) mat.SetFloat("_MacroStrength", 0.45f);
            if (mat.HasProperty("_MacroBright")) mat.SetFloat("_MacroBright", 1.35f);
            if (mat.HasProperty("_TerrainRadius")) mat.SetFloat("_TerrainRadius", LunarTerrainMesh.TerrainRadius);
            if (mat.HasProperty("_RimFadeWidth")) mat.SetFloat("_RimFadeWidth", 80f);
            if (mat.HasProperty("_RimFogColor")) mat.SetColor("_RimFogColor", FogColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.16f);
        }
        else
        {
            mat = VisualMaterials.Lit(MeadowTint, 0f, 0.18f);
            mat.name = "EarthGround_PH";
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.22f);
        }

        if (GrassDiff != null && mat.HasProperty("_BaseMap"))
        {
            // Shader samples world-XZ with an explicit Repeat sampler; keep CPU wrap consistent.
            GrassDiff.wrapMode = TextureWrapMode.Repeat;
            GrassDiff.filterMode = FilterMode.Bilinear;
            mat.SetTexture("_BaseMap", GrassDiff);
            mat.EnableKeyword("_BASEMAP");
            if (mat.HasProperty("_BaseMap_ST"))
                mat.SetVector("_BaseMap_ST", new Vector4(1f, 1f, 0f, 0f));
            mat.mainTextureScale = new Vector2(1f, 1f);
        }
        if (GrassNor != null && mat.HasProperty("_BumpMap"))
        {
            GrassNor.wrapMode = TextureWrapMode.Repeat;
            GrassNor.filterMode = FilterMode.Bilinear;
            mat.SetTexture("_BumpMap", GrassNor);
            mat.EnableKeyword("_NORMALMAP");
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 0.55f);
        }
        if (GrassRough != null && mat.HasProperty("_MetallicGlossMap"))
        {
            mat.SetTexture("_MetallicGlossMap", GrassRough);
        }
        return mat;
    }

    public static Material MakeCloudMaterial(bool shaded) => MakeCloudMaterial(shaded, -1);

    public static Material MakeCloudMaterial(bool shaded, int spriteIndex)
    {
        EnsureLoaded();
        Color c = shaded ? CloudShade : CloudLit;
        var mat = new Material(VisualMaterials.UnlitShader);
        mat.name = shaded ? "CloudShade" : "CloudLit";
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
        mat.renderQueue = 3000;

        Texture2D tex = CloudSoft;
        if (CloudSprites != null && CloudSprites.Length > 0)
        {
            int i = spriteIndex < 0 ? 0 : spriteIndex % CloudSprites.Length;
            tex = CloudSprites[i];
        }
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) { mat.SetTexture("_BaseMap", tex); mat.EnableKeyword("_BASEMAP"); }
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            mat.mainTexture = tex;
        }
        float a = shaded ? 0.7f : 0.88f;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(c.r, c.g, c.b, a));
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(c.r, c.g, c.b, a));
        return mat;
    }

    /// <summary>Красиве жовте сонце (одне коло, soft corona в текстурі).</summary>
    public static Material MakeBeautifulSunMaterial()
    {
        EnsureLoaded();
        var mat = new Material(VisualMaterials.UnlitShader);
        mat.name = "SunBeautiful";
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
        mat.renderQueue = 3100;
        if (SunGlow != null)
        {
            if (mat.HasProperty("_BaseMap")) { mat.SetTexture("_BaseMap", SunGlow); mat.EnableKeyword("_BASEMAP"); }
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", SunGlow);
            mat.mainTexture = SunGlow;
        }
        var col = new Color(1f, 0.95f, 0.55f, 1f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1.4f, 1.1f, 0.35f));
        }
        return mat;
    }

    // legacy alias
    public static Material MakeSimpleSunMaterial() => MakeBeautifulSunMaterial();

    // Кеш 128²: 1M×GetPixelBilinear під час albedo bake блокував splash
    static Color[] _grassLut;
    static Color[] _mudLut;
    static int _lutN;
    static bool _lutReady;

    static void EnsureWorldLuts()
    {
        if (_lutReady) return;
        EnsureLoaded();
        const int N = 128;
        _lutN = N;
        _grassLut = new Color[N * N];
        _mudLut = new Color[N * N];
        for (int y = 0; y < N; y++)
        {
            float v = (y + 0.5f) / N;
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N;
                int i = y * N + x;
                if (GrassDiff != null && GrassDiff.isReadable)
                {
                    Color g = GrassDiff.GetPixelBilinear(u, v);
                    g = Color.Lerp(g * 0.55f, FoliageGreen, 0.45f);
                    g = Color.Lerp(g, SoilBrown, 0.06f);
                    _grassLut[i] = g;
                }
                else
                    _grassLut[i] = FoliageGreen * 0.55f;

                if (MudDiff != null && MudDiff.isReadable)
                    _mudLut[i] = MudDiff.GetPixelBilinear(u, v);
                else
                    _mudLut[i] = SoilBrown;
            }
        }
        _lutReady = true;
    }

    public static Color SampleGrassWorld(float wx, float wz, float fallbackR, float fallbackG, float fallbackB)
    {
        EnsureWorldLuts();
        Color baseCol = new Color(
            fallbackR > 0.01f ? fallbackR : FoliageGreen.r,
            fallbackG > 0.01f ? fallbackG : FoliageGreen.g,
            fallbackB > 0.01f ? fallbackB : FoliageGreen.b, 1f);

        float u = Mathf.Repeat(Mathf.Abs(wx) * 0.0125f, 1f);
        float v = Mathf.Repeat(Mathf.Abs(wz) * 0.0125f, 1f);
        int N = _lutN;
        int ix = Mathf.Clamp((int)(u * N), 0, N - 1);
        int iy = Mathf.Clamp((int)(v * N), 0, N - 1);
        int i = iy * N + ix;

        Color g = _grassLut[i];
        Color mud = _mudLut[i];
        g = Color.Lerp(g, mud * 0.85f, 0.12f);
        g = Color.Lerp(baseCol, g, 0.85f);
        return g;
    }

    /// <summary>
    /// Набір готових cloud-спрайтів з фото неба (різні crop + soft alpha silhouette).
    /// </summary>
    static Texture2D[] BuildCloudSpriteSet(Texture2D sky, int count, int size)
    {
        var list = new Texture2D[count];
        // Різні ділянки фото неба (хмари)
        var crops = new[]
        {
            new Vector4(0.05f, 0.40f, 0.45f, 0.85f),
            new Vector4(0.35f, 0.35f, 0.80f, 0.80f),
            new Vector4(0.55f, 0.45f, 0.98f, 0.90f),
            new Vector4(0.10f, 0.55f, 0.55f, 0.95f),
            new Vector4(0.40f, 0.25f, 0.90f, 0.70f),
            new Vector4(0.00f, 0.30f, 0.50f, 0.75f),
        };
        for (int i = 0; i < count; i++)
        {
            var c = crops[i % crops.Length];
            float seed = i * 17.13f + 2.7f;
            list[i] = BuildOneCloudSprite(sky, size, c.x, c.y, c.z, c.w, seed);
            list[i].name = $"CloudSprite_{i}_PH";
        }
        return list;
    }

    static Texture2D BuildOneCloudSprite(Texture2D sky, int size,
        float u0, float v0, float u1, float v1, float seed)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float cx = (size - 1) * 0.5f;
        int srcW = sky.width, srcH = sky.height;
        var cols = new Color[size * size];

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px = x / (float)(size - 1);
            float py = y / (float)(size - 1);
            float nx = (x - cx) / cx;
            float ny = (y - cx) / cx;

            // М’яка «хмароподібна» маска (не коло): кілька blob’ів
            float mask = 0f;
            mask = Mathf.Max(mask, Blob(nx, ny, -0.15f, 0.05f, 0.85f, 0.55f));
            mask = Mathf.Max(mask, Blob(nx, ny, 0.35f, -0.1f, 0.65f, 0.45f));
            mask = Mathf.Max(mask, Blob(nx, ny, -0.4f, -0.15f, 0.55f, 0.4f));
            mask = Mathf.Max(mask, Blob(nx, ny, 0.1f, 0.25f, 0.5f, 0.35f));
            float edge = 1f - Mathf.SmoothStep(0.55f, 1.15f, Mathf.Sqrt(nx * nx * 0.7f + ny * ny));
            mask *= edge;

            float n = Mathf.PerlinNoise(x * 0.028f + seed, y * 0.028f + seed * 0.7f);
            float n2 = Mathf.PerlinNoise(x * 0.07f + seed * 2f, y * 0.07f);
            mask *= 0.55f + 0.45f * n;
            mask *= 0.7f + 0.3f * n2;

            float u = Mathf.Lerp(u0, u1, px);
            float v = Mathf.Lerp(v0, v1, py);
            Color s = sky.GetPixelBilinear(u, v);
            float lum = 0.299f * s.r + 0.587f * s.g + 0.114f * s.b;
            float dens = Mathf.SmoothStep(0.35f, 0.92f, lum);

            float a = Mathf.Clamp01(mask * dens * 1.55f);
            // Білі / cool-grey хмари
            Color rgb = Color.Lerp(new Color(0.78f, 0.82f, 0.88f), new Color(0.98f, 0.99f, 1f), dens);
            rgb = Color.Lerp(rgb, s, 0.15f); // трохи real sky color
            cols[y * size + x] = new Color(rgb.r, rgb.g, rgb.b, a);
        }
        tex.SetPixels(cols);
        tex.Apply(false, true);
        return tex;
    }

    static float Blob(float x, float y, float cx, float cy, float rx, float ry)
    {
        float dx = (x - cx) / Mathf.Max(0.01f, rx);
        float dy = (y - cy) / Mathf.Max(0.01f, ry);
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        return 1f - Mathf.SmoothStep(0.35f, 1f, d);
    }

    static Texture2D BuildCloudSoftTex(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        tex.name = "CloudSoft";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float cx = (size - 1) * 0.5f;
        var cols = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float nx = (x - cx) / cx;
            float ny = (y - cx) / cx;
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            float a = 1f - Mathf.SmoothStep(0.15f, 1.0f, r);
            float n = Mathf.PerlinNoise(x * 0.035f + 2.1f, y * 0.035f + 1.3f);
            a *= 0.55f + 0.45f * n;
            a = Mathf.Clamp01(a * a * 1.15f);
            cols[y * size + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels(cols);
        tex.Apply(false, true);
        return tex;
    }

    static Texture2D BuildBeautifulSunTex(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        tex.name = "SunBeautiful";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float cx = (size - 1) * 0.5f;
        var cols = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float nx = (x - cx) / cx;
            float ny = (y - cx) / cx;
            float r = Mathf.Sqrt(nx * nx + ny * ny);

            // Яскраве жовте ядро
            float core = 1f - Mathf.SmoothStep(0f, 0.42f, r);
            // М’яка corona / glow
            float corona = 1f - Mathf.SmoothStep(0.25f, 0.92f, r);
            corona = Mathf.Pow(Mathf.Max(0f, corona), 1.6f);
            // Тонкий hot rim
            float rim = 1f - Mathf.SmoothStep(0.38f, 0.58f, Mathf.Abs(r - 0.48f));

            float a = Mathf.Clamp01(core * 1.15f + corona * 0.75f + rim * 0.2f);
            a = Mathf.Clamp01(a);

            Color hot = new Color(1f, 0.98f, 0.72f);
            Color mid = new Color(1f, 0.85f, 0.28f);
            Color outer = new Color(1f, 0.55f, 0.08f);
            Color rgb = Color.Lerp(hot, mid, Mathf.Clamp01(r * 1.4f));
            rgb = Color.Lerp(rgb, outer, Mathf.Clamp01((r - 0.35f) * 1.8f));
            // Легкий «полум’яний» noise на краю
            float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
            if (r > 0.35f && r < 0.85f)
                a *= 0.85f + 0.15f * n;

            cols[y * size + x] = new Color(rgb.r, rgb.g, rgb.b, a);
        }
        tex.SetPixels(cols);
        tex.Apply(false, true);
        return tex;
    }
}
