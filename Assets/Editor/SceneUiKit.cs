using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// The create-only UI kit the Home and Title builders share (audit R6-010: it
/// was copied into both): the canvas and event-system bootstrap, the scene's
/// logic and UI objects, and panels, texts and buttons found by name under a
/// parent or created with the given layout. An object that already exists is
/// returned as it is, never re-laid out (the two builders' create-only policy).
/// The art slots (redesign phase 27, ArtSlotImage) are the one exception: a
/// slot's configuration is re-applied on every build.
/// </summary>
internal static class SceneUiKit
{
    /// <summary>
    /// The scene's canvas, or a new Screen Space Overlay canvas that scales
    /// from 1920×1080; the scene also gets an EventSystem with the Input
    /// System's UI module if it has none.
    /// </summary>
    public static Canvas EnsureCanvasAndEventSystem()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasGo.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
        }

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
        }

        return canvas;
    }

    /// <summary>The scene's <typeparamref name="T"/>, or a new scene-root object named <paramref name="name"/> carrying one (a scene's logic controller).</summary>
    public static T FindOrCreateObject<T>(string name) where T : Component
    {
        T existing = Object.FindAnyObjectByType<T>();

        if (existing != null)
            return existing;

        var go = new GameObject(name);
        T component = go.AddComponent<T>();
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return component;
    }

    /// <summary>The scene's <typeparamref name="T"/>, or a new UI object named <paramref name="name"/> under <paramref name="parent"/>, stretched over it, carrying one (a scene's UI controller).</summary>
    public static T FindOrCreateStretched<T>(Transform parent, string name) where T : Component
    {
        T existing = Object.FindAnyObjectByType<T>();

        if (existing != null)
            return existing;

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform, Vector2.zero, Vector2.one);

        T component = go.AddComponent<T>();
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return component;
    }

    /// <summary>Finds a child panel by name or creates it with the given anchors, position and size, on a <paramref name="bgColor"/> background (translucent black by default) when <paramref name="withBackground"/>.</summary>
    public static Transform FindOrCreatePanel(
        Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 size, bool withBackground, Color bgColor = default)
    {
        Transform existing = parent.Find(name);

        if (existing != null)
            return existing;

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        if (withBackground)
        {
            Image img = go.AddComponent<Image>();
            img.color = bgColor == default ? new Color(0f, 0f, 0f, 0.85f) : bgColor;
        }

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go.transform;
    }

    /// <summary>Finds a child TMP text by name or creates a white one filling the given relative anchors.</summary>
    public static TMP_Text FindOrCreateText(
        Transform parent, string name, string content, int fontSize,
        TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax)
    {
        Transform existing = parent.Find(name);

        if (existing != null)
            return existing.GetComponent<TMP_Text>();

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform, anchorMin, anchorMax);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return text;
    }

    /// <summary>Finds a child button by name or creates one filling the given relative anchors (a light Image, the Button and a black centred TMP label).</summary>
    public static Button FindOrCreateButton(
        Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        Transform existing = parent.Find(name);

        if (existing != null)
            return existing.GetComponent<Button>();

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform, anchorMin, anchorMax);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        Stretch((RectTransform)labelGo.transform, Vector2.zero, Vector2.one);

        var labelText = labelGo.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 26;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.black;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return btn;
    }

    /// <summary>
    /// Finds a child image by name or creates a white one (no raycasts) at the
    /// given anchors, pivot, position and size, keeping its aspect: the host
    /// of an art slot that has no image of its own today.
    /// </summary>
    public static Image FindOrCreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                                          Vector2 anchoredPos, Vector2 size)
    {
        Transform existing = parent.Find(name);

        if (existing != null)
            return existing.GetComponent<Image>();

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return img;
    }

    /// <summary>
    /// Gives an image its art slot (ArtSlotImage.Configure, re-applied on every
    /// build): the slot's file shows when it exists, else the image keeps its
    /// look (or hides, <paramref name="hideWithoutArt"/>); a full-colour
    /// <paramref name="picture"/> shows untinted; <paramref name="companions"/>
    /// come on with the art.
    /// </summary>
    public static void EnsureArtSlot(Image image, string slot, string hoverSlot, bool picture, bool hideWithoutArt, params Behaviour[] companions)
    {
        if (image == null)
            return;
        ArtSlotImage art = image.GetComponent<ArtSlotImage>();
        if (art == null)
            art = Undo.AddComponent<ArtSlotImage>(image.gameObject);
        art.Configure(slot, hoverSlot, picture, hideWithoutArt, companions);
        EditorUtility.SetDirty(art);
    }

    /// <summary>Anchors a rect to the given relative corners with no offsets, so it fills them.</summary>
    public static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
