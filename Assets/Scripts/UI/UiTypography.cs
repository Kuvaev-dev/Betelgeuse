using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using System.Collections.Generic;

/// <summary>
/// Чіткий HUD-текст. Один динамічний SDF (Segoe UI) для латиниці + кирилиці —
/// без dual-material fallback, який розмиває дрібний UI.
/// </summary>
public static class UiTypography
{
    static TMP_FontAsset cachedFont;
    static bool triedBuild;

    public static TMP_FontAsset FontAsset
    {
        get
        {
            if (cachedFont != null) return cachedFont;
            if (!triedBuild)
            {
                triedBuild = true;
                cachedFont = BuildFont();
            }
            if (cachedFont == null && TMP_Settings.defaultFontAsset != null)
                cachedFont = TMP_Settings.defaultFontAsset;
            return cachedFont;
        }
    }

    public static TMP_FontAsset Font => FontAsset;

    public static Color Text => UiTheme.Current.Text;
    public static Color Muted => UiTheme.Current.Muted;
    public static Color Accent => UiTheme.Current.Accent;
    public static Color Amber => UiTheme.Current.Amber;
    public static Color Ok => UiTheme.Current.Ok;
    public static Color Alert => UiTheme.Current.Alert;
    public static Color Panel => UiTheme.Current.Panel;
    public static Color PanelSoft => UiTheme.Current.PanelSoft;
    public static Color Btn => UiTheme.Current.Btn;
    public static Color BtnActive => UiTheme.Current.BtnActive;
    public static Color Edge => UiTheme.Current.Edge;

    const int Sampling = 48; // TMP UI sweet-spot (large sampling blurs small HUD sizes)
    const int Padding = 5;

    static TMP_FontAsset BuildFont()
    {
        try
        {
            var fa = BuildDynamicSdf();
            if (fa != null)
            {
                Prefill(fa);
                TuneMaterial(fa.material);
                // LiberationSans as last-resort for rare glyphs only
                var lib = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (lib != null)
                {
                    if (fa.fallbackFontAssetTable == null)
                        fa.fallbackFontAssetTable = new List<TMP_FontAsset>();
                    fa.fallbackFontAssetTable.Clear();
                    fa.fallbackFontAssetTable.Add(lib);
                }
                Debug.Log("[UiTypography] Dynamic SDF ready (sampling=" + Sampling + ")");
                return fa;
            }

            var fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (fallback != null)
            {
                var copy = Object.Instantiate(fallback);
                TuneMaterial(copy.material);
                return copy;
            }
            return TMP_Settings.defaultFontAsset;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[UiTypography] " + e.Message);
            return null;
        }
    }

    static TMP_FontAsset BuildDynamicSdf()
    {
        UnityEngine.Font source = Resources.Load<UnityEngine.Font>("Fonts/SegoeUI");
        if (source == null)
        {
            source = UnityEngine.Font.CreateDynamicFontFromOSFont(
                new[]
                {
                    "Segoe UI", "Segoe UI Variable Text",
                    "Arial", "Tahoma", "Calibri", "Microsoft Sans Serif"
                },
                Sampling);
        }
        if (source == null) return null;

        TMP_FontAsset fa;
        try
        {
            fa = TMP_FontAsset.CreateFontAsset(
                source,
                Sampling,
                Padding,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);
        }
        catch
        {
            fa = TMP_FontAsset.CreateFontAsset(source);
        }
        if (fa == null) return null;

        fa.name = "Betelgeuse_UI_SDF";
        fa.isMultiAtlasTexturesEnabled = true;

        if (fa.atlasTextures != null)
        {
            foreach (var tex in fa.atlasTextures)
            {
                if (tex == null) continue;
                tex.filterMode = FilterMode.Bilinear;
                tex.anisoLevel = 0;
            }
        }

        TuneMaterial(fa.material);
        return fa;
    }

    static void TuneMaterial(Material mat)
    {
        if (mat == null) return;

        // Must equal atlas padding + 1 for crisp SDF edges
        if (mat.HasProperty(ShaderUtilities.ID_GradientScale))
            mat.SetFloat(ShaderUtilities.ID_GradientScale, Padding + 1f);

        if (mat.HasProperty(ShaderUtilities.ID_FaceDilate))
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_WeightNormal))
            mat.SetFloat(ShaderUtilities.ID_WeightNormal, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_WeightBold))
            mat.SetFloat(ShaderUtilities.ID_WeightBold, 0.3f);
        // Slight positive sharpness helps Overlay canvas readability
        if (mat.HasProperty(ShaderUtilities.ID_Sharpness))
            mat.SetFloat(ShaderUtilities.ID_Sharpness, 0.35f);

        mat.DisableKeyword("UNDERLAY_ON");
        mat.DisableKeyword("UNDERLAY_INNER");
        mat.DisableKeyword("OUTLINE_ON");
        if (mat.HasProperty(ShaderUtilities.ID_UnderlayColor))
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, Color.clear);
        if (mat.HasProperty(ShaderUtilities.ID_UnderlaySoftness))
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_UnderlayOffsetX))
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_UnderlayOffsetY))
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_OutlineColor))
            mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.clear);
        if (mat.HasProperty(ShaderUtilities.ID_OutlineWidth))
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_OutlineSoftness))
            mat.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_FaceColor))
            mat.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
    }

    static void Prefill(TMP_FontAsset fa)
    {
        if (fa == null) return;
        const string sample =
            " АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯ" +
            "абвгґдеєжзиіїйклмнопрстуфхцчшщьюя" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" +
            "0123456789.,;:!?%/+-*=()[]{}<>|@#_'\" " +
            "м/с кг кН % с т /100 " +
            "ГОТОВООЧІК.СТАРТСПУСКУСПІХЗБІЙСТОПТЕСТСХОВАТИПОКАЗАТИПАУЗАДАЛІ" +
            "READYWAITSTARTDOWNOKFAILSTOPTESTHIDEUISHOWUIPAUSERESUMEPAUSED" +
            "КритеріїКлавішінахилпромахПідказкаАлгоритм";
        fa.TryAddCharacters(sample, out _);
    }

    public static void Apply(TMP_Text tmp, float size, Color color, FontStyles style = FontStyles.Normal)
    {
        if (tmp == null) return;
        var f = FontAsset;
        if (f != null)
        {
            tmp.font = f;
            if (f.material != null)
                tmp.fontSharedMaterial = f.material;
        }

        // Integer sizes only
        float s = Mathf.Max(12f, Mathf.Round(size));
        tmp.fontSize = Mathf.Clamp(s, 12f, 32f);

        color.a = 1f;
        if (!UiTheme.IsLightBackground && Luma(color) < 0.42f && Luma(color) > 0.04f)
            color = Color.Lerp(color, Color.white, 0.45f);

        tmp.color = color;
        // Synthetic Bold softens SDF — prefer Normal + slightly larger size for "bold" look
        if (style == FontStyles.Bold)
        {
            tmp.fontStyle = FontStyles.Normal;
            tmp.fontSize = Mathf.Min(32f, tmp.fontSize + 1f);
            if (tmp.fontSharedMaterial != null &&
                tmp.fontSharedMaterial.HasProperty(ShaderUtilities.ID_FaceDilate))
            {
                // keep shared mat clean; weight via size only
            }
        }
        else
        {
            tmp.fontStyle = FontStyles.Normal;
        }

        tmp.characterSpacing = 0f;
        tmp.wordSpacing = 0f;
        tmp.lineSpacing = 0f;
        tmp.paragraphSpacing = 0f;
        tmp.extraPadding = true;
        tmp.richText = false;
        tmp.raycastTarget = false;
        tmp.isOrthographic = true;
        tmp.outlineWidth = 0f;
        tmp.outlineColor = new Color32(0, 0, 0, 0);
        tmp.enableVertexGradient = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.enableAutoSizing = false;
        tmp.geometrySortingOrder = VertexSortingOrder.Normal;
    }

    public static void ConfigureCanvas(Canvas canvas)
    {
        if (canvas == null) return;
        canvas.pixelPerfect = false;
        canvas.additionalShaderChannels =
            AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;
    }

    static float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
}
