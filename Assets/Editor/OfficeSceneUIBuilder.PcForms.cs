using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's PC forms (redesign phase 5, PC spec FO1, §2.4, §6.5):
/// a FormView (the uGUI form: its paper, seal, fill and line strokes, the slot
/// button every pickable box clones, the photo cell and the text template in
/// the paper's text material, every part tagged DiegeticForm so no theme
/// touches it) and the scanned-document window built on it: the scanner's
/// dark backing, the title bar, the scan strip and the form in a scroll, a
/// document page at the PC width (542 u, so H = 708 u) showing whole. Part of
/// <see cref="OfficeSceneUIBuilder"/>.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>A form page's width on the PC: the 580 u pane less its padding and scrollbar (PC spec §6.3).</summary>
    private const float PcPageWidth = 542f;

    /// <summary>The document window's margin, the gap beside the scrollbar, the scrollbar's width and the scan strip's height and text size (u).</summary>
    private const float DocMargin = 10f, DocGap = 4f, DocScrollbar = 14f, DocStrip = 22f, DocStripText = 18f;

    /// <summary>The seal placeholder's sprite (FormStyle's seal art until the agency seal lands; the desk paper's ring).</summary>
    private const string FormSealSprite = "form_seal";

    /// <summary>
    /// A FormView named <paramref name="name"/> under <paramref name="parent"/>,
    /// <paramref name="width"/> wide at the parent's top centre (a document
    /// page tall until it draws): the page in the style's paper, the faint
    /// seal, the fills, the slot button template (a clear image and a button),
    /// the lines, the photo cell (a portrait), and the text template in the
    /// paper's text material (the desk paper's, so both print one ink). Every
    /// part is DiegeticForm (the photo's layers DiegeticPhoto); the templates,
    /// the seal and the photo start inactive. Rebuilt fresh.
    /// </summary>
    private static FormView BuildFormView(Transform parent, string name, float width)
    {
        FormStyleSO style = EnsureFormStyle();
        DestroyChildIfPresent(parent, name);
        var view = (RectTransform)Panel(parent, name, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero,
                                        new Vector2(width, width / style.metrics.aspect), style.paper, ThemeRoleId.DiegeticForm);
        view.pivot = new Vector2(0.5f, 1f);

        Image seal = Panel(view, "Seal", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, style.seal, ThemeRoleId.DiegeticForm).GetComponent<Image>();
        seal.sprite = EnsureOfficeShape(FormSealSprite, SealPixels, SealPixels, new Vector2(0.5f, 0.5f), SealRing);
        seal.raycastTarget = false;
        seal.gameObject.SetActive(false);

        FormStrokes fills = Strokes(view, "Fills");

        Transform slots = Panel(view, "Slots", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Image slotImage = Panel(slots, "SlotTemplate", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear, ThemeRoleId.DiegeticForm).GetComponent<Image>();
        Button slot = slotImage.gameObject.AddComponent<Button>();
        slot.transition = Selectable.Transition.None;
        slot.targetGraphic = slotImage;
        slotImage.gameObject.SetActive(false);

        FormStrokes lines = Strokes(view, "Lines");

        Transform photoFrame = Panel(view, "Photo", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        TravellerPortraitView portrait = BuildPortrait(photoFrame);
        photoFrame.gameObject.SetActive(false);

        Transform texts = Panel(view, "Texts", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        TMP_Text text = Text(texts, "TextTemplate", string.Empty, Mathf.RoundToInt(style.metrics.valueSize * width / style.metrics.aspect), TextAlignmentOptions.TopLeft,
                             Vector2.zero, Vector2.one, style.ink, ThemeRoleId.DiegeticForm);
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        Material ink = PaperTextMaterial(text.font, style.inkWeight);
        if (ink != null)
            text.fontSharedMaterial = ink;
        text.gameObject.SetActive(false);

        FormView form = view.gameObject.AddComponent<FormView>();
        var so = new SerializedObject(form);
        SetRef(so, "style", style);
        SetRef(so, "paper", view.GetComponent<Image>());
        SetRef(so, "seal", seal);
        SetRef(so, "fills", fills);
        SetRef(so, "slotsRoot", slots);
        SetRef(so, "slotTemplate", slot);
        SetRef(so, "lines", lines);
        SetRef(so, "photoFrame", photoFrame);
        SetRef(so, "photo", portrait);
        SetRef(so, "textsRoot", texts);
        SetRef(so, "textTemplate", text);
        so.ApplyModifiedProperties();
        return form;
    }

    /// <summary>One printed layer of a form: a FormStrokes graphic spanning the form, never a raycast target.</summary>
    private static FormStrokes Strokes(Transform view, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(view, false);
        SetAnchors(go.transform, Vector2.zero, Vector2.one);
        FormStrokes strokes = go.AddComponent<FormStrokes>();
        strokes.raycastTarget = false;
        Tag(strokes, ThemeRoleId.DiegeticForm, ThemePart.Fill);
        return strokes;
    }

    /// <summary>
    /// The scanned-document window, rebuilt fresh each run (a paper's scanned
    /// copy, PC spec §2.4): on the scanner's dark backing (the form style's,
    /// DiegeticBacking), the title bar, the scan strip in the backing's ink,
    /// and a scroll whose content is the paper's FormView at the PC page
    /// width, with an auto-hiding scrollbar. The window fits a one-page form
    /// whole; a maximised window keeps the page at its width, centred.
    /// </summary>
    private static DocumentWindowController BuildDocumentWindow(Transform layer)
    {
        FormStyleSO style = EnsureFormStyle();
        DestroyChildIfPresent(layer, "DocumentWindowTemplate");
        float titleBar = TitleBarSize.y;
        float pageHeight = PcPageWidth / style.metrics.aspect;
        var size = new Vector2(DocMargin + PcPageWidth + DocGap + DocScrollbar + DocMargin, titleBar + DocGap + DocStrip + DocGap + pageHeight + DocMargin);
        Transform win = Panel(layer, "DocumentWindowTemplate", Center, Center, Vector2.zero, size, style.backing, ThemeRoleId.DiegeticBacking);
        TMP_Text title = BuildWindowHeader(win, null, UiText.Get("document.untitled"));

        TMP_Text strip = Text(win, "ScanStrip", string.Format(style.scanStrip, "--:--"), Mathf.RoundToInt(DocStripText), TextAlignmentOptions.MidlineLeft,
                              new Vector2(0f, 1f), Vector2.one, style.backingInk, ThemeRoleId.DiegeticBacking);
        PlaceRect(strip.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(DocMargin, -(titleBar + DocGap + DocStrip)), new Vector2(-DocMargin, -(titleBar + DocGap)));
        strip.raycastTarget = false;
        strip.textWrappingMode = TextWrappingModes.NoWrap;
        strip.overflowMode = TextOverflowModes.Ellipsis;

        Transform area = Panel(win, "Scroll", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(area, Vector2.zero, Vector2.one, new Vector2(DocMargin, DocMargin), new Vector2(-DocMargin, -(titleBar + DocGap + DocStrip + DocGap)));
        Transform viewport = Panel(area, "Viewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-(DocScrollbar + DocGap), 0f));
        viewport.gameObject.AddComponent<RectMask2D>();
        FormView form = BuildFormView(viewport, "Form", PcPageWidth);

        Transform track = Panel(area, "Scrollbar", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, style.backing, ThemeRoleId.DiegeticBacking);
        PlaceRect(track, new Vector2(1f, 0f), Vector2.one, new Vector2(-DocScrollbar, 0f), Vector2.zero);
        Transform slide = Panel(track, "SlidingArea", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        SetAnchors(slide, Vector2.zero, Vector2.one);
        Transform handle = Panel(slide, "Handle", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, style.backingInk, ThemeRoleId.DiegeticBacking);
        SetAnchors(handle, Vector2.zero, Vector2.one);
        // Through the serialized fields, as the browser's scrollbar: a setter would drive the handle now and save it zeroed.
        Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
        var soBar = new SerializedObject(scrollbar);
        soBar.FindProperty("m_Direction").enumValueIndex = (int)Scrollbar.Direction.BottomToTop;
        soBar.FindProperty("m_HandleRect").objectReferenceValue = handle;
        soBar.FindProperty("m_TargetGraphic").objectReferenceValue = handle.GetComponent<Image>();
        soBar.FindProperty("m_Size").floatValue = 1f;
        soBar.FindProperty("m_Value").floatValue = 0f;
        soBar.ApplyModifiedProperties();

        ScrollRect scroll = area.gameObject.AddComponent<ScrollRect>();
        scroll.content = (RectTransform)form.transform;
        scroll.viewport = (RectTransform)viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        DocumentWindowController c = win.gameObject.AddComponent<DocumentWindowController>();
        var so = new SerializedObject(c);
        SetRef(so, "titleText", title);
        SetRef(so, "scanStrip", strip);
        SetRef(so, "scroll", scroll);
        SetRef(so, "form", form);
        so.ApplyModifiedProperties();
        win.gameObject.SetActive(false);
        return c;
    }

    /// <summary>The document window reads the shift clock for its scan strip (the clock is on the GameManager, built after the window).</summary>
    private static void WireDocumentClock(DocumentWindowController window, ShiftClockDriver clock)
    {
        var so = new SerializedObject(window);
        SetRef(so, "clock", clock);
        so.ApplyModifiedProperties();
    }
}
