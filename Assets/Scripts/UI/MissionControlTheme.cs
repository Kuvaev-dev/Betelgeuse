using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Космічна палітра Mission Control (legacy scene UI).
/// Основний HUD будує MissionControlUI — цей клас лише для старих елементів.
/// </summary>
[DefaultExecutionOrder(-100)]
public class MissionControlTheme : MonoBehaviour
{
    public static readonly Color Void = new(0.02f, 0.03f, 0.07f, 0.92f);
    public static readonly Color Panel = new(0.03f, 0.04f, 0.09f, 0.9f);
    public static readonly Color Edge = new(0.2f, 0.45f, 0.75f, 0.55f);
    public static readonly Color Cyan = new(0.35f, 0.85f, 1f, 1f);
    public static readonly Color Amber = new(1f, 0.72f, 0.25f, 1f);
    public static readonly Color Ok = new(0.35f, 0.95f, 0.55f, 1f);
    public static readonly Color Alert = new(1f, 0.38f, 0.42f, 1f);
    public static readonly Color Text = new(0.92f, 0.95f, 1f, 1f);
    public static readonly Color Muted = new(0.55f, 0.62f, 0.75f, 1f);

    [Header("Авто-стиль при старті")]
    public bool styleOnAwake = true;
    public bool dimMainCameraBackground = true;

    void Awake()
    {
        if (styleOnAwake && FindFirstObjectByType<MissionControlUI>() == null)
            Apply();
    }

    [ContextMenu("Apply Mission Control Theme")]
    public void Apply()
    {
        if (dimMainCameraBackground && Camera.main != null)
            Camera.main.backgroundColor = new Color(0.008f, 0.01f, 0.03f);

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
            tmp.characterSpacing = 1.5f;
        }

        foreach (var btn in FindObjectsByType<Button>(FindObjectsSortMode.None))
            StyleButton(btn.targetGraphic as Image);
    }

    static void StyleButton(Image img)
    {
        if (img == null) return;
        img.color = new Color(0.07f, 0.12f, 0.22f, 0.95f);
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
}
