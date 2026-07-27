using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime Mission-Control skin: темні панелі, cyan/amber акценти, mono-телеметрія.
/// Не ламає scene-wired посилання — лише візуальний шар.
/// </summary>
[DefaultExecutionOrder(-100)]
public class MissionControlTheme : MonoBehaviour
{
    public static readonly Color Void = new(0.027f, 0.043f, 0.078f, 0.92f);
    public static readonly Color Panel = new(0.051f, 0.082f, 0.149f, 0.88f);
    public static readonly Color Edge = new(0.102f, 0.153f, 0.267f, 1f);
    public static readonly Color Cyan = new(0.239f, 0.878f, 1f, 1f);
    public static readonly Color Amber = new(1f, 0.690f, 0.125f, 1f);
    public static readonly Color Ok = new(0.239f, 1f, 0.604f, 1f);
    public static readonly Color Alert = new(1f, 0.302f, 0.416f, 1f);
    public static readonly Color Text = new(0.910f, 0.941f, 1f, 1f);
    public static readonly Color Muted = new(0.478f, 0.545f, 0.659f, 1f);

    [Header("Авто-стиль при старті")]
    public bool styleOnAwake = true;
    public bool dimMainCameraBackground = true;

    void Awake()
    {
        // Disabled by default when MissionControlUI rebuilds the HUD.
        // Avoid painting the old broken canvas black over the 3D view.
        if (styleOnAwake && FindFirstObjectByType<MissionControlUI>() == null)
            Apply();
    }

    [ContextMenu("Apply Mission Control Theme")]
    public void Apply()
    {
        if (dimMainCameraBackground && Camera.main != null)
            Camera.main.backgroundColor = new Color(0.02f, 0.035f, 0.06f);

        foreach (var img in FindObjectsByType<Image>(FindObjectsSortMode.None))
        {
            if (img == null) continue;
            string n = img.gameObject.name.ToLowerInvariant();

            if (img.GetComponent<Button>() != null)
            {
                StyleButton(img);
                continue;
            }

            if (n.Contains("panel") || n.Contains("background") || n.Contains("bg")
                || n.Contains("dashboard") || n.Contains("hud") || n.Contains("frame"))
            {
                img.color = Panel;
            }
            else if (img.GetComponentInParent<Slider>() != null
                     || img.GetComponentInParent<Toggle>() != null
                     || img.GetComponentInParent<TMP_InputField>() != null)
            {
                // keep interactive chrome slightly brighter
                if (n.Contains("fill")) img.color = Cyan * 0.85f;
                else if (n.Contains("handle")) img.color = Amber;
                else img.color = Edge;
            }
        }

        foreach (var tmp in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
        {
            if (tmp == null) continue;
            string n = tmp.gameObject.name.ToLowerInvariant();
            tmp.color = Text;
            tmp.fontStyle = FontStyles.Normal;
            if (n.Contains("title") || n.Contains("header") || n.Contains("winner"))
            {
                tmp.color = Cyan;
                tmp.fontStyle = FontStyles.Bold;
            }
            else if (n.Contains("success") || n.Contains("stats") || n.Contains("pid")
                     || n.Contains("fuzzy") || n.Contains("neural"))
            {
                tmp.color = Text;
            }
            // Prefer monospace if available
            TrySetMono(tmp);
        }

        foreach (var btn in FindObjectsByType<Button>(FindObjectsSortMode.None))
            StyleButton(btn.targetGraphic as Image);

        // Do NOT paint Canvas root black — that covers the entire 3D view.
    }

    static void StyleButton(Image img)
    {
        if (img == null) return;
        img.color = new Color(0.08f, 0.14f, 0.24f, 0.95f);
        var btn = img.GetComponent<Button>();
        if (btn == null) return;
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.75f, 0.95f, 1f, 1f);
        cb.pressedColor = Cyan;
        cb.selectedColor = new Color(0.85f, 0.95f, 1f, 1f);
        cb.disabledColor = new Color(0.4f, 0.4f, 0.45f, 0.5f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
    }

    static void TrySetMono(TMP_Text tmp)
    {
        // Use default TMP font; letter-spacing for instrument feel
        tmp.characterSpacing = 2f;
        if (tmp.fontSize > 0 && tmp.fontSize < 18f)
            tmp.enableAutoSizing = false;
    }
}
