using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItemSpawner;

internal static class UiFactory
{
    internal static TMP_FontAsset FindGameFont()
    {
        TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
        foreach (TextMeshProUGUI text in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
        {
            if (text == null || text.font == null)
            {
                continue;
            }
            fallback ??= text.font;
            string value = text.text;
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if (IsCjkCharacter(value[i]))
                    {
                        return text.font;
                    }
                }
            }
        }
        return fallback;
    }

    private static bool IsCjkCharacter(char value)
    {
        return (value >= '\u3400' && value <= '\u9fff') ||
            (value >= '\uf900' && value <= '\ufaff');
    }

    internal static RectTransform Rect(string name, Transform parent)
    {
        GameObject gameObject = new(name, Il2CppType.Of<RectTransform>());
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    internal static TextMeshProUGUI Text(
        RectTransform parent,
        TMP_FontAsset font,
        string value,
        float size,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = Rect("Text", parent);
        TextMeshProUGUI text = ((Component)rect).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10f, 4f);
        rect.offsetMax = new Vector2(-10f, -4f);
        return text;
    }

    internal static Button Button(RectTransform parent, TMP_FontAsset font, string label)
    {
        Image image = ((Component)parent).gameObject.AddComponent<Image>();
        image.color = new Color(0.22f, 0.25f, 0.29f, 1f);
        Button button = ((Component)parent).gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Text(parent, font, label, 24f, TextAlignmentOptions.Center);
        return button;
    }

    internal static TMP_InputField Input(RectTransform parent, TMP_FontAsset font, string placeholderText)
    {
        Image image = ((Component)parent).gameObject.AddComponent<Image>();
        image.color = new Color(0.10f, 0.12f, 0.15f, 1f);
        RectTransform viewport = Rect("Viewport", parent);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(12f, 4f);
        viewport.offsetMax = new Vector2(-12f, -4f);
        ((Component)viewport).gameObject.AddComponent<RectMask2D>();
        TextMeshProUGUI value = Text(viewport, font, string.Empty, 24f, TextAlignmentOptions.MidlineLeft);
        TextMeshProUGUI placeholder = Text(viewport, font, placeholderText, 24f, TextAlignmentOptions.MidlineLeft);
        placeholder.color = new Color(1f, 1f, 1f, 0.45f);
        TMP_InputField input = ((Component)parent).gameObject.AddComponent<TMP_InputField>();
        input.textViewport = viewport;
        input.textComponent = value;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        return input;
    }
}
