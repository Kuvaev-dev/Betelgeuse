using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using System.Collections.Generic;

/// <summary>
/// Чіткий HUD-текст (кирилиця + латиниця).
/// Base = LiberationSans SDF (латиниця/цифри), fallback = динамічний Segoe UI (кирилиця).
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
                cachedFont = BuildFontChain();
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

    static TMP_FontAsset BuildFontChain()
    {
        try
        {
            // 1) Готовий якісний SDF (латиниця/цифри) з пакету TMP
            TMP_FontAsset primary = null;
            var tmpDefault = TMP_Settings.defaultFontAsset;
            if (tmpDefault != null)
                primary = Object.Instantiate(tmpDefault);
            if (primary == null)
                primary = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            // 2) Динамічний OS-шрифт з кирилицею (висока якість атласу)
            TMP_FontAsset cyr = BuildCyrillicDynamic();

            if (primary != null && cyr != null)
            {
                primary.name = "Betelgeuse_UI_Primary";
                if (primary.fallbackFontAssetTable == null)
                    primary.fallbackFontAssetTable = new List<TMP_FontAsset>();
                // прибрати старі fallback і додати наш
                primary.fallbackFontAssetTable.Clear();
                primary.fallbackFontAssetTable.Add(cyr);
                TuneMaterial(primary.material);
                TuneMaterial(cyr.material);
                Prefill(cyr);
                Debug.Log("[UiTypography] Primary+Cyrillic fallback ready");
                return primary;
            }

            if (cyr != null)
            {
                Prefill(cyr);
                TuneMaterial(cyr.material);
                return cyr;
            }

            return primary;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[UiTypography] " + e.Message);
            return null;
        }
    }

    static TMP_FontAsset BuildCyrillicDynamic()
    {
        // sampling 72 + padding 8 — стандарт TMP для чіткого UI (не oversized 256)
        const int sampling = 72;
        const int padding = 8;

        UnityEngine.Font source = Resources.Load<UnityEngine.Font>("Fonts/SegoeUI");
        if (source == null)
        {
            source = UnityEngine.Font.CreateDynamicFontFromOSFont(
                new[]
                {
                    "Segoe UI", "Segoe UI Variable Text",
                    "Arial", "Tahoma", "Calibri", "Microsoft Sans Serif"
                },
                sampling);
        }
        if (source == null) return null;

        TMP_FontAsset fa;
        try
        {
            fa = TMP_FontAsset.CreateFontAsset(
                source,
                sampling,
                padding,
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

        fa.name = "Betelgeuse_UI_Cyrillic";
        fa.isMultiAtlasTexturesEnabled = true;

        if (fa.atlasTextures != null)
        {
            foreach (var tex in fa.atlasTextures)
            {
                if (tex == null) continue;
                tex.filterMode = FilterMode.Bilinear;
                tex.anisoLevel = 1;
            }
        }

        TuneMaterial(fa.material);
        return fa;
    }

    static void TuneMaterial(Material mat)
    {
        if (mat == null) return;

        // GradientScale ≈ atlasPadding + 1 (критично для різких країв SDF)
        float grad = 9f;
        if (mat.HasProperty(ShaderUtilities.ID_GradientScale))
        {
            float g = mat.GetFloat(ShaderUtilities.ID_GradientScale);
            if (g > 1f) grad = g;
            else mat.SetFloat(ShaderUtilities.ID_GradientScale, grad);
        }

        // Легкий dilate для читабельності, без «мильності»
        if (mat.HasProperty(ShaderUtilities.ID_FaceDilate))
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.0f);
        if (mat.HasProperty(ShaderUtilities.ID_WeightNormal))
            mat.SetFloat(ShaderUtilities.ID_WeightNormal, 0.0f);
        if (mat.HasProperty(ShaderUtilities.ID_WeightBold))
            mat.SetFloat(ShaderUtilities.ID_WeightBold, 0.4f);
        if (mat.HasProperty(ShaderUtilities.ID_Sharpness))
            mat.SetFloat(ShaderUtilities.ID_Sharpness, 0.75f);

        // Без underlay / outline — вони розмивають дрібний UI
        mat.DisableKeyword("UNDERLAY_ON");
        mat.DisableKeyword("UNDERLAY_INNER");
        mat.DisableKeyword("OUTLINE_ON");
        if (mat.HasProperty(ShaderUtilities.ID_UnderlayColor))
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, Color.clear);
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
            "0123456789.,;:!?%/+-*=()[]{}<>|@#_'\"°·•→←↑↓—–…«»№" +
            " м/с кг кН % с т /100 Δ≈" +
            "ГОТОВООЧІК.СТАРТСПУСКУСПІХЗБІЙСТОПТЕСТСХОВАТИПОКАЗАТИ" +
            "READYWAITSTARTDOWNOKFAILSTOPTESTHIDEUISHOWUI" +
            "КритеріїКлавішінахилпромахм/с";
        fa.TryAddCharacters(sample, out _);
    }

    public static void Apply(TMP_Text tmp, float size, Color color, FontStyles style = FontStyles.Normal)
    {
        if (tmp == null) return;
        var f = FontAsset;
        if (f != null)
        {
            tmp.font = f;
            // Shared material — не плодити копії (зберігає SDF keywords/quality)
            if (f.material != null)
                tmp.fontSharedMaterial = f.material;
        }

        float s = Mathf.Max(12f, Mathf.Round(size));
        if (style == FontStyles.Bold) s += 1f;
        tmp.fontSize = Mathf.Clamp(s, 12f, 40f);

        color.a = 1f;
        if (!UiTheme.IsLightBackground && Luma(color) < 0.42f && Luma(color) > 0.04f)
            color = Color.Lerp(color, Color.white, 0.45f);

        tmp.color = color;
        tmp.fontStyle = style;
        tmp.characterSpacing = -0.5f;
        tmp.wordSpacing = 0f;
        tmp.lineSpacing = 0f;
        tmp.paragraphSpacing = 0f;
        tmp.enableKerning = true;
        tmp.extraPadding = true;
        tmp.richText = false;
        tmp.raycastTarget = false;
        tmp.isOrthographic = true;
        tmp.outlineWidth = 0f;
        tmp.outlineColor = new Color32(0, 0, 0, 0);
        tmp.enableVertexGradient = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.enableWordWrapping = false;
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
