using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Максимально чіткий HUD-текст (кирилиця + латиниця).
/// Високий SDF sampling, dilate, underlay/outline за темою.
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
                cachedFont = BuildCyrillicFont();
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

    static TMP_FontAsset BuildCyrillicFont()
    {
        try
        {
            UnityEngine.Font source = Resources.Load<UnityEngine.Font>("Fonts/SegoeUI");
            if (source == null)
            {
                // Великий point size OS-шрифту → чіткіші гліфи в атласі
                source = UnityEngine.Font.CreateDynamicFontFromOSFont(
                    new[]
                    {
                        "Segoe UI Semibold", "Segoe UI", "Arial",
                        "Tahoma", "Calibri", "Microsoft Sans Serif"
                    },
                    256);
            }
            if (source == null) return null;

            TMP_FontAsset fa;
            try
            {
                // samplingPointSize 256 + padding 20 = дуже різкий SDF
                fa = TMP_FontAsset.CreateFontAsset(
                    source,
                    256,
                    20,
                    GlyphRenderMode.SDFAA,
                    4096,
                    4096,
                    AtlasPopulationMode.Dynamic,
                    true);
            }
            catch
            {
                fa = TMP_FontAsset.CreateFontAsset(source);
            }
            if (fa == null) return null;
            fa.name = "Betelgeuse_UI_SDF_HD";
            TuneMaterial(fa.material);
            Prefill(fa);
            Debug.Log("[UiTypography] HD font ready: " + source.name);
            return fa;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[UiTypography] " + e.Message);
            return null;
        }
    }

    static void TuneMaterial(Material mat)
    {
        if (mat == null) return;

        // Максимальна різкість SDF-гліфів (кирилиця + латиниця)
        if (mat.HasProperty(ShaderUtilities.ID_GradientScale))
            mat.SetFloat(ShaderUtilities.ID_GradientScale, 22f);
        if (mat.HasProperty(ShaderUtilities.ID_WeightNormal))
            mat.SetFloat(ShaderUtilities.ID_WeightNormal, 0.12f);
        if (mat.HasProperty(ShaderUtilities.ID_WeightBold))
            mat.SetFloat(ShaderUtilities.ID_WeightBold, 0.5f);
        if (mat.HasProperty(ShaderUtilities.ID_FaceDilate))
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.18f);
        if (mat.HasProperty(ShaderUtilities.ID_Sharpness))
            mat.SetFloat(ShaderUtilities.ID_Sharpness, 1f);

        bool light = UiTheme.IsLightBackground;

        // Light: dark soft shadow (paper print). Dark: black underlay.
        if (mat.HasProperty(ShaderUtilities.ID_UnderlayColor))
        {
            mat.EnableKeyword("UNDERLAY_ON");
            if (light)
            {
                mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.05f, 0.07f, 0.1f, 0.28f));
                if (mat.HasProperty(ShaderUtilities.ID_UnderlayOffsetX))
                    mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.4f);
                if (mat.HasProperty(ShaderUtilities.ID_UnderlayOffsetY))
                    mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.5f);
                if (mat.HasProperty(ShaderUtilities.ID_UnderlayDilate))
                    mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.12f);
                if (mat.HasProperty(ShaderUtilities.ID_UnderlaySoftness))
                    mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.35f);
            }
            else
            {
                mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.85f));
                if (mat.HasProperty(ShaderUtilities.ID_UnderlayOffsetX))
                    mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.75f);
                if (mat.HasProperty(ShaderUtilities.ID_UnderlayOffsetY))
                    mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.75f);
                if (mat.HasProperty(ShaderUtilities.ID_UnderlayDilate))
                    mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.35f);
                if (mat.HasProperty(ShaderUtilities.ID_UnderlaySoftness))
                    mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.12f);
            }
        }

        // Light: thin dark hairline outline (not white glow — that washed text out)
        if (mat.HasProperty(ShaderUtilities.ID_OutlineColor))
        {
            mat.EnableKeyword("OUTLINE_ON");
            if (light)
                mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.08f, 0.1f, 0.14f, 0.35f));
            else
                mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.75f));
            if (mat.HasProperty(ShaderUtilities.ID_OutlineWidth))
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, light ? 0.06f : 0.15f);
            if (mat.HasProperty(ShaderUtilities.ID_OutlineSoftness))
                mat.SetFloat(ShaderUtilities.ID_OutlineSoftness, light ? 0.05f : 0.0f);
        }

        // Light: ще тонший штрих на білих панелях
        if (light && mat.HasProperty(ShaderUtilities.ID_FaceDilate))
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.08f);

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
            " м/с кг кН % с т /100 ОК";
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
            {
                // Завжди свіжий material під поточну тему
                tmp.fontMaterial = new Material(f.material);
                TuneMaterial(tmp.fontMaterial);
            }
        }

        // +1.5 pt, мін. 13 — чіткість на 1080p/1440p
        float s = size + 1.5f;
        if (style == FontStyles.Bold) s += 0.5f;
        tmp.fontSize = Mathf.Clamp(s, 13f, 48f);

        color.a = 1f;
        // НЕ перефарбовуємо навмисно білий/світлий текст (кнопки primary на light theme).
        // Раніше Luma>0.5 → Text (темний) ламало білий текст на зелених/синіх кнопках.
        // На темному — підтягуємо надто тьмяний muted
        if (!UiTheme.IsLightBackground && Luma(color) < 0.45f && Luma(color) > 0.05f)
            color = Color.Lerp(color, Color.white, 0.35f);

        tmp.color = color;
        tmp.fontStyle = style;
        tmp.characterSpacing = 0.5f;
        tmp.wordSpacing = 2f;
        tmp.lineSpacing = 4f;
        tmp.enableKerning = true;
        tmp.extraPadding = true;
        tmp.richText = false;
        tmp.raycastTarget = false;
        tmp.isOrthographic = true;
        if (UiTheme.IsLightBackground)
        {
            tmp.outlineWidth = 0.05f;
            tmp.outlineColor = new Color32(20, 24, 32, 90);
        }
        else
        {
            tmp.outlineWidth = 0.14f;
            tmp.outlineColor = new Color32(0, 0, 0, 200);
        }
        tmp.enableVertexGradient = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.enableWordWrapping = false;
        tmp.enableAutoSizing = false;
    }

    static float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
}
