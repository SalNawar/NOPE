using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// The create-only UI kit the Home and Title builders share (audit R6-010: it
/// was copied into both), and the theme-tag stamp the office builder shares: the canvas and event-system bootstrap, the scene's
/// logic and UI objects, and panels, texts and buttons found by name under a
/// parent or created with the given layout. An object that already exists is
/// returned as it is, never re-laid out (the two builders' create-only policy).
/// The art slots (redesign phase 27, ArtSlotImage) are the one exception: a
/// slot's configuration is re-applied on every build.
/// </summary>
internal static class SceneUiKit
{
    /// <summary>
    /// Stamps (or re-stamps) a graphic's theme tag (piece 6): its role and part,
    /// and for a text its label key, built style, kind and shrink-to-fit. The
    /// office builder stamps every graphic it creates; the Home builder its
    /// HUD's wallet and stability, so a leading culture's labels are drawn in
    /// the culture's font (CultureThemeService) as the office's are.
    /// </summary>
    public static void Tag(Component graphic, ThemeRoleId role, ThemePart part, string labelKey = null, FontStyles style = FontStyles.Normal,
                           ThemeTextKind kind = ThemeTextKind.Body, bool fit = false)
    {
        if (graphic == null)
            return;
        ThemeTag tag = graphic.GetComponent<ThemeTag>();
        if (tag == null)
            tag = graphic.gameObject.AddComponent<ThemeTag>();
        tag.Configure(role, part, labelKey, style, kind, fit);
    }

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
    /// Finds a child text field by name or creates one filling the given
    /// relative anchors: a white box, a masked text area with a grey italic
    /// placeholder and a dark text of <paramref name="fontSize"/>, and its
    /// TMP_InputField (single line).
    /// </summary>
    public static TMP_InputField FindOrCreateInputField(Transform parent, string name, int fontSize, Vector2 anchorMin, Vector2 anchorMax)
    {
        Transform existing = parent.Find(name);

        if (existing != null)
            return existing.GetComponent<TMP_InputField>();

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform, anchorMin, anchorMax);
        Image box = go.AddComponent<Image>();
        box.color = Color.white;

        var area = new GameObject("TextArea", typeof(RectTransform));
        area.transform.SetParent(go.transform, false);
        var areaRect = (RectTransform)area.transform;
        Stretch(areaRect, Vector2.zero, Vector2.one);
        areaRect.offsetMin = new Vector2(14f, 4f);
        areaRect.offsetMax = new Vector2(-14f, -4f);
        area.AddComponent<RectMask2D>();

        TextMeshProUGUI MakeText(string childName, Color colour, FontStyles style)
        {
            var t = new GameObject(childName, typeof(RectTransform));
            t.transform.SetParent(area.transform, false);
            Stretch((RectTransform)t.transform, Vector2.zero, Vector2.one);
            var text = t.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = colour;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        TextMeshProUGUI placeholder = MakeText("Placeholder", new Color(0.4f, 0.4f, 0.42f, 1f), FontStyles.Italic);
        TextMeshProUGUI value = MakeText("Text", new Color(0.1f, 0.1f, 0.12f, 1f), FontStyles.Normal);

        TMP_InputField input = go.AddComponent<TMP_InputField>();
        input.textViewport = areaRect;
        input.textComponent = value;
        input.placeholder = placeholder;
        input.targetGraphic = box;
        input.lineType = TMP_InputField.LineType.SingleLine;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return input;
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

    /// <summary>
    /// Skins <paramref name="host"/> with the UI kit's <paramref name="piece"/>
    /// (docs/UI_KIT.md; re-applied on every build): the host keeps its rect and
    /// its clicks but draws nothing (clear, its theme tag the kit's), and its
    /// first child KitFace draws the piece's sprite untinted, sliced at
    /// <paramref name="scale"/> sprite pixels per unit, grown past the host by
    /// the sprite's empty pad so the drawn plate fills the host's rect; a
    /// control on the host (a Button, a Toggle, an input field) swaps the
    /// piece's sprites (rest, hover, pressed, locked) on its face. A stateful
    /// piece is named without its state ("plate_ox"), any other by its sprite
    /// ("panel_bone"). Returns the face, or null (with an error) when the kit
    /// has no such piece.
    /// </summary>
    public static Image Skin(Image host, UiKitSO kit, string piece, float scale)
    {
        if (host == null || kit == null)
            return null;
        Sprite rest = kit.Get(piece, KitState.Rest) ?? kit.Get(piece);
        if (rest == null)
        {
            Debug.LogError($"[TimeDesk] The UI kit has no piece '{piece}' for '{host.name}'; it keeps its flat look. Check the name against Assets/Art/UI/Kit/kit_manifest.json.", host);
            return null;
        }

        host.sprite = null;
        host.color = Color.clear;
        ThemeTag hostTag = host.GetComponent<ThemeTag>();
        ThemeRoleId role = hostTag != null ? hostTag.Role : ThemeRoleId.ClickCatcher;
        Rekit(hostTag, FontStyles.Normal, null);

        Transform faceTransform = host.transform.Find(UiKitSO.FaceName);
        if (faceTransform == null)
        {
            faceTransform = new GameObject(UiKitSO.FaceName, typeof(RectTransform)).transform;
            faceTransform.SetParent(host.transform, false);
        }
        faceTransform.SetAsFirstSibling();
        faceTransform.gameObject.layer = host.gameObject.layer;
        Image face = faceTransform.GetComponent<Image>();
        if (face == null)
            face = faceTransform.gameObject.AddComponent<Image>();
        face.sprite = rest;
        face.color = Color.white;
        face.raycastTarget = false;
        face.preserveAspect = false;
        face.fillCenter = true;
        bool sliced = rest.border != Vector4.zero;
        face.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        face.pixelsPerUnitMultiplier = scale;
        Tag(face, role, ThemePart.Kit);
        LayoutElement free = faceTransform.GetComponent<LayoutElement>();
        if (free == null)
            free = faceTransform.gameObject.AddComponent<LayoutElement>();
        free.ignoreLayout = true; // a host's layout group lays out its content, never its face

        var rt = (RectTransform)faceTransform;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
        if (sliced)
        {
            float pad = kit.spritePad / (rest.pixelsPerUnit / 100f * scale);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-pad, -pad);
            rt.offsetMax = new Vector2(pad, pad);
        }
        else
        {
            // A simple sprite stretches whole: grow it by its pad's share of the drawn part, whatever the host's size.
            Vector2 size = rest.rect.size;
            var grow = new Vector2(kit.spritePad / Mathf.Max(1f, size.x - 2f * kit.spritePad), kit.spritePad / Mathf.Max(1f, size.y - 2f * kit.spritePad));
            rt.anchorMin = -grow;
            rt.anchorMax = Vector2.one + grow;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        if (host.TryGetComponent(out Selectable control))
        {
            control.targetGraphic = face;
            bool states = kit.Get(piece, KitState.Hover) != null;
            control.transition = states ? Selectable.Transition.SpriteSwap : Selectable.Transition.None;
            kit.Show(face, piece, states ? control : null);
        }
        EditorUtility.SetDirty(host);
        return face;
    }

    /// <summary>
    /// A text on the kit (re-applied on every build), styled by the type scale
    /// (UiKitSO.typeScale, the one place its sizes live): its role's face (kept
    /// by the theme unless the labels are in a culture's script) and its size
    /// for a component <paramref name="height"/> units tall (never under
    /// <paramref name="floor"/>, a canvas's reading floor), in
    /// <paramref name="ink"/>; its theme tag the kit's (the theme no longer
    /// recolours it). A label role is one line in tracked capitals at its size,
    /// fitted to its room (<see cref="FitLabel"/>: <paramref name="grow"/>, the
    /// plate to widen when it does not fit, or none); a reading role wraps and
    /// may shrink to its floor. Returns the size.
    /// </summary>
    public static float SkinText(TMP_Text text, UiKitSO kit, KitText kind, float height, Color ink, float floor = 0f, RectTransform grow = null)
    {
        if (text == null || kit == null)
            return 0f;
        TMP_FontAsset face = kit.Face(kind);
        float size = kit.TextSize(kind, height, floor);
        bool label = KitTypeScale.IsLabel(kind);
        text.color = ink;
        if (face != null)
            text.font = face;
        text.fontSize = size;
        text.overflowMode = TextOverflowModes.Overflow;
        if (label)
        {
            text.fontStyle = (text.fontStyle & ~FontStyles.Italic) | FontStyles.UpperCase;
            text.characterSpacing = KitTypeScale.IsTracked(kind) ? kit.labelTracking : 0f;
        }
        else
        {
            text.fontStyle &= ~FontStyles.UpperCase;
            text.characterSpacing = 0f;
        }
        // Every role sizes itself at run time between its scale size and its floor (never smaller): a word set
        // at run time (a culture's, an order's name) fits its room as the build's own words do.
        text.enableAutoSizing = true;
        text.fontSizeMax = size;
        text.fontSizeMin = Mathf.Min(size, Mathf.Max(kit.Style(kind).min, floor));
        // A text whose plate widens to fit it (a content-sized pill) never needs to shrink: it keeps its scale size.
        if (text.transform.parent != null && text.transform.parent.TryGetComponent(out ContentSizeFitter sized) && sized.horizontalFit == ContentSizeFitter.FitMode.PreferredSize)
            text.fontSizeMin = size;
        text.textWrappingMode = KitTypeScale.Wraps(kind) ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        Rekit(text.GetComponent<ThemeTag>(), label && kind != KitText.Masthead ? FontStyles.UpperCase : FontStyles.Normal, face);
        if (!KitTypeScale.Wraps(kind))
            FitLabel(text, kit, kind, floor, grow);
        return text.fontSizeMax;
    }

    /// <summary>Recolours a text that keeps its builder's size and font (a window's own content lines) in the kit's <paramref name="ink"/>, its theme tag the kit's.</summary>
    public static void Ink(TMP_Text text, Color ink)
    {
        if (text == null)
            return;
        text.color = ink;
        Rekit(text.GetComponent<ThemeTag>(), FontStyles.Normal, null);
    }

    /// <summary>
    /// Fits a one-line label to its room at its scale size (KitTypeScale.Fit):
    /// too wide, it shrinks no lower than its role's floor, and the plate
    /// <paramref name="grow"/> (sized by its own width) widens for the rest;
    /// a label still too wide is logged (no shrink-to-tiny). A label written at
    /// run time (empty at build) is left at its scale size.
    /// </summary>
    public static void FitLabel(TMP_Text text, UiKitSO kit, KitText kind, float floor, RectTransform grow)
    {
        string word = text.text;
        ThemeTag tag = text.GetComponent<ThemeTag>();
        if (string.IsNullOrEmpty(word) && tag != null && !string.IsNullOrEmpty(tag.LabelKey))
            word = UiText.Get(tag.LabelKey); // a keyed label is written when the scene loads: fit the reading language's word
        if (string.IsNullOrEmpty(word))
            return;
        float room = text.rectTransform.rect.width - text.margin.x - text.margin.z;
        if (room <= 0f)
            return;
        float width = text.GetPreferredValues(word, float.PositiveInfinity, float.PositiveInfinity).x;
        (float size, float more) = KitTypeScale.Fit(text.fontSize, width, room, Mathf.Max(kit.Style(kind).min, floor));
        text.fontSize = size;
        text.fontSizeMax = size;
        if (more <= 0.5f)
            return;
        if (grow != null && grow.anchorMin.x == grow.anchorMax.x)
            grow.sizeDelta += new Vector2(Mathf.Ceil(more), 0f);
        else
            Debug.LogWarning($"[TimeDesk] The {kind} '{word}' ({text.name}) needs {more:0} more units than its room at its floor size; widen its plate in the builder.", text);
    }

    /// <summary>Turns a theme tag into the kit's (part Kit, its role, label key, kind and fit kept), adding <paramref name="style"/> to its base style and setting its face.</summary>
    private static void Rekit(ThemeTag tag, FontStyles style, TMP_FontAsset face)
    {
        if (tag == null)
            return;
        tag.Configure(tag.Role, ThemePart.Kit, tag.LabelKey, tag.BaseStyle | style, tag.TextKind, tag.ShrinkToFit);
        tag.SetFace(face);
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
