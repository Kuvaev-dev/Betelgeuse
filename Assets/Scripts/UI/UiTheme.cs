using UnityEngine;

/// <summary>
/// Кольорові теми HUD. PlayerPrefs. MissionControlUI читає UiTheme.Current.
/// </summary>
public static class UiTheme
{
    public enum Id
    {
        Dark = 0,
        Cyan = 1,
        Amber = 2,
        Light = 3,
        Green = 4,
        Violet = 5,
        Red = 6,
        Ice = 7
    }

    const string PrefKey = "Betelgeuse.UiTheme";
    static Id current = Id.Dark;
    static bool loaded;

    public static event System.Action OnThemeChanged;

    public static Id CurrentId
    {
        get { Ensure(); return current; }
        set
        {
            Ensure();
            if (current == value) return;
            current = value;
            PlayerPrefs.SetInt(PrefKey, (int)value);
            PlayerPrefs.Save();
            OnThemeChanged?.Invoke();
        }
    }

    public static Palette Current
    {
        get
        {
            Ensure();
            int i = (int)current;
            if (i < 0 || i >= Palettes.Length) i = 0;
            return Palettes[i];
        }
    }

    public static void Cycle()
    {
        Ensure();
        int n = ((int)current + 1) % Palettes.Length;
        CurrentId = (Id)n;
    }

    public static string ButtonLabel => CurrentId switch
    {
        Id.Cyan => "CYAN",
        Id.Amber => "AMBER",
        Id.Light => "LIGHT",
        Id.Green => "GREEN",
        Id.Violet => "VIOLET",
        Id.Red => "RED",
        Id.Ice => "ICE",
        _ => "DARK"
    };

    public static string ButtonLabelUk => CurrentId switch
    {
        Id.Cyan => "ЦІАН",
        Id.Amber => "БУРШТИН",
        Id.Light => "СВІТЛА",
        Id.Green => "ЗЕЛЕНА",
        Id.Violet => "ФІОЛЕТОВА",
        Id.Red => "ЧЕРВОНА",
        Id.Ice => "ЛІД",
        _ => "ТЕМНА"
    };

    /// <summary>true — світлий фон панелей (підкладка тексту інша).</summary>
    public static bool IsLightBackground
    {
        get
        {
            Ensure();
            return current == Id.Light || current == Id.Ice;
        }
    }

    /// <summary>Світлий текст на темному фоні.</summary>
    public static Color TextOnDark => new(0.98f, 0.98f, 1f, 1f);

    /// <summary>
    /// Chrome top-menu / progress: у світлих темах — світла смуга (не чорна),
    /// у темних — panelSoft.
    /// </summary>
    public static Color DarkChrome => IsLightBackground
        ? new Color(0.96f, 0.97f, 0.985f, 0.98f)
        : Current.PanelSoft;

    /// <summary>Текст на chrome-смузі (адаптивний).</summary>
    public static Color ChromeText => IsLightBackground
        ? Current.Text
        : TextOnDark;

    /// <summary>Кнопка на chrome / action: світла тема → м'який tint + білий текст на accent-кнопках.</summary>
    public static Color ActionBtnBg(Color darkTint)
    {
        if (!IsLightBackground) return darkTint;
        // Освітлити dark tint для light UI
        return Color.Lerp(darkTint, new Color(0.88f, 0.92f, 0.96f, 1f), 0.72f);
    }

    public static Color ActionBtnText(Color bg)
    {
        float luma = 0.2126f * bg.r + 0.7152f * bg.g + 0.0722f * bg.b;
        // На достатньо темному — білий; на світлому — ink
        return luma < 0.55f ? TextOnDark : Current.Text;
    }

    /// <summary>Картка результату / overlay.</summary>
    public static Color ModalCard => IsLightBackground
        ? new Color(0.97f, 0.97f, 0.98f, 0.99f)
        : new Color(0.08f, 0.08f, 0.1f, 0.98f);

    public static Color ModalScrim => IsLightBackground
        ? new Color(0.12f, 0.14f, 0.18f, 0.52f)
        : new Color(0.01f, 0.012f, 0.018f, 0.82f);

    /// <summary>Текст для фону bg (автоконтраст).</summary>
    public static Color ContrastOn(Color bg)
    {
        float luma = 0.2126f * bg.r + 0.7152f * bg.g + 0.0722f * bg.b;
        return luma > 0.55f ? Current.Text : TextOnDark;
    }

    static void Ensure()
    {
        if (loaded) return;
        loaded = true;
        int v = PlayerPrefs.GetInt(PrefKey, 0);
        if (v < 0 || v >= Palettes.Length) v = 0;
        current = (Id)v;
    }

    public readonly struct Palette
    {
        public readonly Color Text, Muted, Accent, Amber, Ok, Alert;
        public readonly Color Panel, PanelSoft, Btn, BtnActive, Edge, BtnHover;
        public readonly Color GraphA, GraphB, GraphC;

        public Palette(
            Color text, Color muted, Color accent, Color amber, Color ok, Color alert,
            Color panel, Color panelSoft, Color btn, Color btnActive, Color edge, Color btnHover,
            Color gA, Color gB, Color gC)
        {
            Text = text; Muted = muted; Accent = accent; Amber = amber; Ok = ok; Alert = alert;
            Panel = panel; PanelSoft = panelSoft; Btn = btn; BtnActive = btnActive; Edge = edge;
            BtnHover = btnHover; GraphA = gA; GraphB = gB; GraphC = gC;
        }
    }

    static readonly Palette[] Palettes =
    {
        // 0 Dark
        new(
            text: new Color(0.99f, 0.99f, 1f),
            muted: new Color(0.78f, 0.78f, 0.82f),
            accent: new Color(0.94f, 0.94f, 0.98f),
            amber: new Color(1f, 0.84f, 0.42f),
            ok: new Color(0.45f, 0.95f, 0.6f),
            alert: new Color(1f, 0.48f, 0.5f),
            panel: new Color(0.035f, 0.035f, 0.045f, 0.97f),
            panelSoft: new Color(0.06f, 0.06f, 0.08f, 0.95f),
            btn: new Color(0.14f, 0.14f, 0.17f, 1f),
            btnActive: new Color(0.28f, 0.28f, 0.34f, 1f),
            edge: new Color(0.5f, 0.52f, 0.6f, 0.7f),
            btnHover: new Color(0.18f, 0.22f, 0.32f, 1f),
            gA: new Color(0.9f, 0.9f, 0.95f),
            gB: new Color(0.85f, 0.78f, 0.5f),
            gC: new Color(0.55f, 0.85f, 0.65f)),

        // 1 Cyan
        new(
            text: new Color(0.95f, 0.99f, 1f),
            muted: new Color(0.6f, 0.78f, 0.88f),
            accent: new Color(0.35f, 0.92f, 1f),
            amber: new Color(1f, 0.78f, 0.35f),
            ok: new Color(0.4f, 0.95f, 0.7f),
            alert: new Color(1f, 0.45f, 0.5f),
            panel: new Color(0.02f, 0.04f, 0.08f, 0.97f),
            panelSoft: new Color(0.04f, 0.07f, 0.12f, 0.95f),
            btn: new Color(0.06f, 0.12f, 0.2f, 1f),
            btnActive: new Color(0.1f, 0.28f, 0.4f, 1f),
            edge: new Color(0.3f, 0.65f, 0.85f, 0.9f),
            btnHover: new Color(0.1f, 0.22f, 0.35f, 1f),
            gA: new Color(0.4f, 0.9f, 1f),
            gB: new Color(1f, 0.75f, 0.35f),
            gC: new Color(0.4f, 0.95f, 0.7f)),

        // 2 Amber
        new(
            text: new Color(1f, 0.98f, 0.92f),
            muted: new Color(0.82f, 0.74f, 0.58f),
            accent: new Color(1f, 0.82f, 0.4f),
            amber: new Color(1f, 0.7f, 0.25f),
            ok: new Color(0.55f, 0.9f, 0.5f),
            alert: new Color(1f, 0.4f, 0.35f),
            panel: new Color(0.06f, 0.04f, 0.03f, 0.97f),
            panelSoft: new Color(0.1f, 0.07f, 0.04f, 0.95f),
            btn: new Color(0.18f, 0.12f, 0.08f, 1f),
            btnActive: new Color(0.35f, 0.22f, 0.1f, 1f),
            edge: new Color(0.85f, 0.6f, 0.3f, 0.9f),
            btnHover: new Color(0.28f, 0.18f, 0.1f, 1f),
            gA: new Color(1f, 0.85f, 0.5f),
            gB: new Color(0.95f, 0.6f, 0.3f),
            gC: new Color(0.6f, 0.9f, 0.5f)),

        // 3 Light — paper UI, soft hairline edges (no black frames)
        new(
            text: new Color(0.08f, 0.1f, 0.14f, 1f),
            muted: new Color(0.38f, 0.4f, 0.46f, 1f),
            accent: new Color(0.08f, 0.38f, 0.68f, 1f),
            amber: new Color(0.78f, 0.42f, 0.02f, 1f),
            ok: new Color(0.04f, 0.5f, 0.28f, 1f),
            alert: new Color(0.78f, 0.1f, 0.14f, 1f),
            panel: new Color(0.995f, 0.995f, 1f, 0.96f),
            panelSoft: new Color(0.95f, 0.96f, 0.98f, 0.97f),
            btn: new Color(0.92f, 0.93f, 0.96f, 1f),
            btnActive: new Color(0.78f, 0.86f, 0.96f, 1f),
            edge: new Color(0.62f, 0.68f, 0.76f, 0.55f),
            btnHover: new Color(0.88f, 0.91f, 0.96f, 1f),
            gA: new Color(0.12f, 0.38f, 0.72f),
            gB: new Color(0.78f, 0.42f, 0.05f),
            gC: new Color(0.05f, 0.52f, 0.3f)),

        // 4 Green
        new(
            text: new Color(0.92f, 1f, 0.94f),
            muted: new Color(0.55f, 0.75f, 0.6f),
            accent: new Color(0.35f, 0.95f, 0.55f),
            amber: new Color(0.95f, 0.85f, 0.35f),
            ok: new Color(0.4f, 1f, 0.55f),
            alert: new Color(1f, 0.45f, 0.45f),
            panel: new Color(0.02f, 0.06f, 0.04f, 0.97f),
            panelSoft: new Color(0.04f, 0.1f, 0.06f, 0.95f),
            btn: new Color(0.06f, 0.14f, 0.09f, 1f),
            btnActive: new Color(0.1f, 0.3f, 0.16f, 1f),
            edge: new Color(0.3f, 0.7f, 0.4f, 0.9f),
            btnHover: new Color(0.1f, 0.22f, 0.14f, 1f),
            gA: new Color(0.4f, 0.95f, 0.55f),
            gB: new Color(0.95f, 0.85f, 0.35f),
            gC: new Color(0.5f, 0.85f, 1f)),

        // 5 Violet
        new(
            text: new Color(0.96f, 0.94f, 1f),
            muted: new Color(0.7f, 0.65f, 0.85f),
            accent: new Color(0.75f, 0.55f, 1f),
            amber: new Color(1f, 0.75f, 0.4f),
            ok: new Color(0.5f, 0.9f, 0.7f),
            alert: new Color(1f, 0.45f, 0.55f),
            panel: new Color(0.05f, 0.03f, 0.08f, 0.97f),
            panelSoft: new Color(0.09f, 0.05f, 0.14f, 0.95f),
            btn: new Color(0.12f, 0.08f, 0.18f, 1f),
            btnActive: new Color(0.28f, 0.16f, 0.4f, 1f),
            edge: new Color(0.6f, 0.4f, 0.85f, 0.9f),
            btnHover: new Color(0.2f, 0.12f, 0.3f, 1f),
            gA: new Color(0.75f, 0.55f, 1f),
            gB: new Color(1f, 0.75f, 0.4f),
            gC: new Color(0.5f, 0.9f, 0.75f)),

        // 6 Red / mission alert
        new(
            text: new Color(1f, 0.95f, 0.95f),
            muted: new Color(0.85f, 0.65f, 0.65f),
            accent: new Color(1f, 0.4f, 0.4f),
            amber: new Color(1f, 0.7f, 0.3f),
            ok: new Color(0.5f, 0.9f, 0.55f),
            alert: new Color(1f, 0.35f, 0.35f),
            panel: new Color(0.07f, 0.03f, 0.03f, 0.97f),
            panelSoft: new Color(0.12f, 0.05f, 0.05f, 0.95f),
            btn: new Color(0.18f, 0.08f, 0.08f, 1f),
            btnActive: new Color(0.4f, 0.14f, 0.14f, 1f),
            edge: new Color(0.9f, 0.35f, 0.35f, 0.9f),
            btnHover: new Color(0.28f, 0.12f, 0.12f, 1f),
            gA: new Color(1f, 0.45f, 0.45f),
            gB: new Color(1f, 0.75f, 0.35f),
            gC: new Color(0.55f, 0.9f, 0.6f)),

        // 7 Ice — cold paper, soft cyan hairlines
        new(
            text: new Color(0.07f, 0.12f, 0.18f, 1f),
            muted: new Color(0.34f, 0.44f, 0.52f, 1f),
            accent: new Color(0.05f, 0.45f, 0.62f, 1f),
            amber: new Color(0.62f, 0.38f, 0.04f, 1f),
            ok: new Color(0.03f, 0.48f, 0.38f, 1f),
            alert: new Color(0.72f, 0.08f, 0.14f, 1f),
            panel: new Color(0.97f, 0.985f, 1f, 0.96f),
            panelSoft: new Color(0.92f, 0.95f, 0.98f, 0.97f),
            btn: new Color(0.88f, 0.93f, 0.96f, 1f),
            btnActive: new Color(0.7f, 0.86f, 0.94f, 1f),
            edge: new Color(0.55f, 0.7f, 0.8f, 0.5f),
            btnHover: new Color(0.82f, 0.9f, 0.95f, 1f),
            gA: new Color(0.08f, 0.45f, 0.65f),
            gB: new Color(0.65f, 0.4f, 0.08f),
            gC: new Color(0.05f, 0.5f, 0.4f)),
    };
}
