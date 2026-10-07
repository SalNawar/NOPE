using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Title and Home builders' kit layout (the cel UI kit, docs/UI_KIT.md;
/// sheet 04): places a part by its top-left inside its panel in reference
/// pixels (the rect is the kit face as seen; SceneUiKit.Skin draws the
/// sprite's shadow room outside it), skins plates, panels, readouts and tiles
/// from the kit (SceneUiKit.Skin) and sets their labels (SceneUiKit.SkinText):
/// headings and plate labels in the kit's condensed label face, upper case;
/// body lines in the project's body font. Everything here is re-applied on
/// every build, so the screens follow the kit and these numbers.
/// </summary>
internal static class KitScreens
{
    /// <summary>Places <paramref name="rt"/> inside its parent at <paramref name="left"/>, <paramref name="top"/> from the parent's top-left (y down) with <paramref name="size"/>.</summary>
    public static void Place(RectTransform rt, float left, float top, Vector2 size)
    {
        Undo.RecordObject(rt, "Kit layout");
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(left, -top);
        rt.sizeDelta = size;
    }

    /// <summary>Places <paramref name="rt"/> <paramref name="right"/> in from its parent's right edge and <paramref name="bottom"/> up from its bottom, with <paramref name="size"/>.</summary>
    public static void PlaceBottomRight(RectTransform rt, float right, float bottom, Vector2 size)
    {
        Undo.RecordObject(rt, "Kit layout");
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-right, bottom);
        rt.sizeDelta = size;
    }

    /// <summary>Places <paramref name="rt"/> <paramref name="left"/> in from its parent's left edge and <paramref name="bottom"/> up from its bottom, with <paramref name="size"/>.</summary>
    public static void PlaceBottomLeft(RectTransform rt, float left, float bottom, Vector2 size)
    {
        Undo.RecordObject(rt, "Kit layout");
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(left, bottom);
        rt.sizeDelta = size;
    }

    /// <summary>Stretches <paramref name="rt"/> across its parent between <paramref name="left"/> and <paramref name="right"/> insets, <paramref name="top"/> down from its top and <paramref name="height"/> tall.</summary>
    public static void Across(RectTransform rt, float left, float right, float top, float height)
    {
        Undo.RecordObject(rt, "Kit layout");
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(left, -top - height);
        rt.offsetMax = new Vector2(-right, -top);
    }

    /// <summary>Sets a panel's size (reference px) and its offset from the screen's centre.</summary>
    public static void Size(Transform panel, Vector2 size, Vector2 offset)
    {
        var rt = (RectTransform)panel;
        Undo.RecordObject(rt, "Kit layout");
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;
    }

    /// <summary>A panel drawn as the kit's <paramref name="piece"/> (panel_bone, panel_manila, panel_night...), its own image cleared.</summary>
    public static void Panel(Transform panel, UiKitSO kit, string piece)
    {
        Image host = panel.GetComponent<Image>();
        if (host == null)
            host = Undo.AddComponent<Image>(panel.gameObject);
        SceneUiKit.Skin(host, kit, piece, kit.overlayScale);
    }

    /// <summary>
    /// A button drawn as the kit's plate <paramref name="piece"/> (plate_ox,
    /// plate_slate, plate_bone, miniplate_ox, a choice card, the dome: its
    /// states swapped), its art slot (an older hand-drawn face) removed, and
    /// its label on, by the type scale (SceneUiKit.SkinText): the plate's role
    /// sized from the plate's height, in the ink for its face (or
    /// <paramref name="ink"/>), the plate widening when the word does not fit.
    /// </summary>
    public static void Plate(Button button, UiKitSO kit, string piece, Color? ink = null)
    {
        if (button == null)
            return;
        ArtSlotImage art = button.GetComponent<ArtSlotImage>();
        if (art != null)
            Undo.DestroyObjectImmediate(art);
        Image host = button.GetComponent<Image>();
        SceneUiKit.Skin(host, kit, piece, kit.overlayScale);
        Transform labelTransform = button.transform.Find("Label");
        TMP_Text label = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
        if (label == null)
            return;
        Undo.RecordObject(label, "Kit plate label");
        label.enabled = true;
        label.alignment = TextAlignmentOptions.Center;
        label.margin = new Vector4(16f, 0f, 16f, 0f);
        label.raycastTarget = false;
        var plate = (RectTransform)button.transform;
        KitText kind = piece.StartsWith("miniplate", System.StringComparison.Ordinal) ? KitText.MiniPlateLabel
            : piece.StartsWith("choicecard", System.StringComparison.Ordinal) ? KitText.CardTitle : KitText.PlateLabel;
        SceneUiKit.SkinText(label, kit, kind, plate.rect.height, ink ?? kit.InkOn(piece), 0f, plate.anchorMin.x == plate.anchorMax.x ? plate : null);
        EditorUtility.SetDirty(label);
    }

    /// <summary>A label or heading in the kit's label face by the type scale: <paramref name="kind"/> sized from the text's own box, in <paramref name="ink"/>.</summary>
    public static void Label(TMP_Text text, UiKitSO kit, Color ink, KitText kind)
    {
        if (text == null)
            return;
        Undo.RecordObject(text, "Kit label");
        SceneUiKit.SkinText(text, kit, kind, text.rectTransform.rect.height, ink);
        EditorUtility.SetDirty(text);
    }

    /// <summary>A reading line on a kit surface by the type scale (a body role: <paramref name="kind"/>): <paramref name="ink"/>, wrapping, shrinking no lower than its floor, in the kit's body face.</summary>
    public static void Body(TMP_Text text, UiKitSO kit, Color ink, KitText kind, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
    {
        if (text == null)
            return;
        Undo.RecordObject(text, "Kit body");
        SceneUiKit.SkinText(text, kit, kind, text.rectTransform.rect.height, ink);
        text.alignment = alignment;
        text.raycastTarget = false;
        EditorUtility.SetDirty(text);
    }

    /// <summary>A kit picture (a tile, the logo, the lever: <paramref name="sprite"/> by its name) under <paramref name="parent"/>, found by name or created, placed at <paramref name="left"/>, <paramref name="top"/> with <paramref name="size"/> as seen (its shadow room drawn outside); no raycasts.</summary>
    public static Image Picture(Transform parent, string name, UiKitSO kit, string sprite, float left, float top, Vector2 size)
    {
        Image host = SceneUiKit.FindOrCreateImage(parent, name, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        Place(host.rectTransform, left, top, size);
        Undo.RecordObject(host, "Kit picture");
        host.raycastTarget = false;
        host.preserveAspect = false;
        Image face = SceneUiKit.Skin(host, kit, sprite, kit.overlayScale);
        if (face != null)
            face.preserveAspect = true;
        return face;
    }

    /// <summary>A phosphor readout under <paramref name="parent"/> (the kit's lcd_glass, found by name or created) at <paramref name="left"/>, <paramref name="top"/>, holding <paramref name="text"/> (moved into it) in the kit's readout face and phosphor ink, right-aligned.</summary>
    public static void Readout(Transform parent, string name, UiKitSO kit, TMP_Text text, float left, float top, Vector2 size)
    {
        Image host = SceneUiKit.FindOrCreateImage(parent, name, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        Place(host.rectTransform, left, top, size);
        SceneUiKit.Skin(host, kit, "lcd_glass", kit.overlayScale);
        if (text == null)
            return;
        if (text.transform.parent != host.transform)
            Undo.SetTransformParent(text.transform, host.transform, "Kit readout");
        var rt = text.rectTransform;
        Undo.RecordObject(rt, "Kit readout");
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(14f, 4f);
        rt.offsetMax = new Vector2(-14f, -4f);
        Undo.RecordObject(text, "Kit readout");
        SceneUiKit.SkinText(text, kit, KitText.Readout, size.y, kit.phosphorInk);
        text.alignment = TextAlignmentOptions.MidlineRight;
        text.raycastTarget = false;
        EditorUtility.SetDirty(text);
    }

    /// <summary>Destroys <paramref name="parent"/>'s child called <paramref name="name"/> when it exists (a part the kit replaced).</summary>
    public static void Remove(Transform parent, string name)
    {
        Transform old = parent != null ? parent.Find(name) : null;
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);
    }
}
