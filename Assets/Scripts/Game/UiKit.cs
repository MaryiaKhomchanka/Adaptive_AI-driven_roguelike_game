using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Builds the plain uGUI used by menus from code, so no scene or prefab needs editing.</summary>
public static class UiKit
{
    static readonly Color ButtonColor = new Color(0.16f, 0.16f, 0.22f, 0.95f);

    public static (GameObject root, Transform content) Page(Transform parent, string name, Color background)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        var rt = (RectTransform)root.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = background;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(root.transform, false);
        var contentRt = (RectTransform)content.transform;
        contentRt.anchorMin = contentRt.anchorMax = contentRt.pivot = new Vector2(0.5f, 0.5f);
        contentRt.sizeDelta = new Vector2(560, 0);

        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 14;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        root.SetActive(false);
        return (root, content.transform);
    }

    public static TextMeshProUGUI Label(Transform parent, string text, float size, bool bold = false)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        if (bold) label.fontStyle = FontStyles.Bold;
        return label;
    }

    public static Button Button(Transform parent, string text, UnityAction onClick, float height = 56)
    {
        var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = height;

        var image = go.GetComponent<Image>();
        image.color = ButtonColor;
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.7f, 1.7f, 2f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        var label = Label(go.transform, text, 26);
        var labelRt = (RectTransform)label.transform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
        return button;
    }

    public static void SetButtonText(Button button, string text) => button.GetComponentInChildren<TMP_Text>().text = text;

    public static Slider Slider(Transform parent, float value, UnityAction<float> onChange)
    {
        var root = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
        root.transform.SetParent(parent, false);
        root.GetComponent<LayoutElement>().preferredHeight = 32;

        var background = Rect(root.transform, "Background", typeof(Image));
        background.anchorMin = new Vector2(0, 0.25f);
        background.anchorMax = new Vector2(1, 0.75f);
        background.offsetMin = background.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color(1, 1, 1, 0.2f);

        var fillArea = Rect(root.transform, "Fill Area");
        fillArea.anchorMin = new Vector2(0, 0.25f);
        fillArea.anchorMax = new Vector2(1, 0.75f);
        fillArea.offsetMin = new Vector2(5, 0);
        fillArea.offsetMax = new Vector2(-15, 0);
        var fill = Rect(fillArea, "Fill", typeof(Image));
        fill.sizeDelta = new Vector2(10, 0);
        fill.GetComponent<Image>().color = new Color(0.45f, 0.75f, 1f);

        var handleArea = Rect(root.transform, "Handle Slide Area");
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(10, 0);
        handleArea.offsetMax = new Vector2(-10, 0);
        var handle = Rect(handleArea, "Handle", typeof(Image));
        handle.sizeDelta = new Vector2(20, 0);

        var slider = root.GetComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(onChange);
        return slider;
    }

    static RectTransform Rect(Transform parent, string name, params System.Type[] components)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        foreach (var type in components) go.AddComponent(type);
        return (RectTransform)go.transform;
    }
}
