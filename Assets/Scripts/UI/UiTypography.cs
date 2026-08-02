using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Максимально чітка кирилиця для HUD.
/// Segoe UI / Arial → SDF high-res atlas, Dynamic population, без outline.
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

    static TMP_FontAsset BuildCyrillicFont()
    {
        try
        {
            // Arial часто чіткіший для UI-кирилиці на Windows
            UnityEngine.Font source = Resources.Load<UnityEngine.Font>("Fonts/SegoeUI");
            if (source == null)
            {
                source = UnityEngine.Font.CreateDynamicFontFromOSFont(
                    new[] { "Segoe UI", "Arial", "Tahoma", "Calibri" },
                    180);
            }

            if (source == null)
            {
                Debug.LogWarning("[UiTypography] Font not found.");
                return null;
            }

            // Максимальна якість SDF
            TMP_FontAsset fa = null;
            try
            {
                fa = TMP_FontAsset.CreateFontAsset(
                    source,
                    180, // sampling — головний фактор різкості
                    16,  // padding
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
            fa.name = "Betelgeuse_UI_SDF";

            TuneMaterial(fa.material);
            Prefill(fa);
            Debug.Log("[UiTypography] Ready: " + source.name);
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
        // Різкі краї гліфів
        if (mat.HasProperty(ShaderUtilities.ID_GradientScale))
            mat.SetFloat(ShaderUtilities.ID_GradientScale, 16f);
        if (mat.HasProperty(ShaderUtilities.ID_WeightNormal))
            mat.SetFloat(ShaderUtilities.ID_WeightNormal, 0.0f);
        if (mat.HasProperty(ShaderUtilities.ID_WeightBold))
            mat.SetFloat(ShaderUtilities.ID_WeightBold, 0.35f);
        // Ледь товстіші штрихи — читабельність UA на темному
        if (mat.HasProperty(ShaderUtilities.ID_FaceDilate))
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.12f);
        if (mat.HasProperty(ShaderUtilities.ID_Sharpness))
            mat.SetFloat(ShaderUtilities.ID_Sharpness, 1f);
        // Тонка темна підкладка — контраст без «пікселів» outline
        if (mat.HasProperty(ShaderUtilities.ID_UnderlayColor))
        {
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.55f));
            if (mat.HasProperty(ShaderUtilities.ID_UnderlayOffsetX))
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.5f);
            if (mat.HasProperty(ShaderUtilities.ID_UnderlayOffsetY))
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.5f);
            if (mat.HasProperty(ShaderUtilities.ID_UnderlayDilate))
                mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.15f);
            if (mat.HasProperty(ShaderUtilities.ID_UnderlaySoftness))
                mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.2f);
        }
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
            "0123456789" +
            ".,;:!?%/+-*=()[]{}<>|@#_'\"°·•✓✗→←↑↓★" +
            "—–…«»№ м/с кг кН % с т ";
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

        // Без штучного +2 — розмір як передано (мін. 11 для компактної правої панелі)
        tmp.fontSize = Mathf.Clamp(size, 11f, 42f);
        color.a = 1f;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.characterSpacing = 0.25f;
        tmp.wordSpacing = 0f;
        tmp.lineSpacing = 6f;
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
    }

    public static readonly Color Text = new(0.98f, 0.98f, 1f, 1f);
    public static readonly Color Muted = new(0.7f, 0.7f, 0.74f, 1f);
    public static readonly Color Accent = new(0.92f, 0.92f, 0.96f, 1f);
    public static readonly Color Amber = new(0.92f, 0.8f, 0.48f, 1f);
    public static readonly Color Ok = new(0.5f, 0.9f, 0.62f, 1f);
    public static readonly Color Alert = new(0.95f, 0.5f, 0.52f, 1f);
    public static readonly Color Panel = new(0.04f, 0.04f, 0.05f, 0.96f);
    public static readonly Color PanelSoft = new(0.07f, 0.07f, 0.08f, 0.94f);
    public static readonly Color Btn = new(0.13f, 0.13f, 0.15f, 1f);
    public static readonly Color BtnActive = new(0.26f, 0.26f, 0.3f, 1f);
    public static readonly Color Edge = new(0.42f, 0.42f, 0.48f, 0.55f);
}
