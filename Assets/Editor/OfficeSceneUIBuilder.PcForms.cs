using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's PC forms (redesign phase 5, PC spec FO1, §2.4, §6.5):
/// a FormView (the uGUI form: its paper, seal, fill and line strokes, the slot
/// button every pickable box clones, the photo cell and the text template in
/// the paper's text material, every part tagged DiegeticForm so no theme
/// touches it) and the scanned-copy page of the Investigation app's
/// Documents tab built on it: the scanner's dark backing, the document's
/// name, the scan strip and the form in a scroll, a document page at the PC
/// width (542 u, so H = 708 u). Part of <see cref="OfficeSceneUIBuilder"/>.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>A form page's width on the PC: the 580 u pane less its padding and scrollbar (PC spec §6.3).</summary>
    private const float PcPageWidth = 542f;

    /// <summary>The scanned-copy page's margin, its gaps, the scrollbar's width, the name's and the scan strip's heights and text sizes (u).</summary>
    private const float DocMargin = 10f, DocGap = 4f, DocScrollbar = 14f, DocTitle = 28f, DocTitleText = 20f, DocStrip = 22f, DocStripText = 18f;

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
    /// the seal, the photo and its frame's art start inactive. Rebuilt fresh.
    /// The view prints at the width Show is given, else this one.
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
        Image frameArt = Panel(photoFrame, "FrameArt", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white, ThemeRoleId.DiegeticForm).GetComponent<Image>();
        frameArt.raycastTarget = false;
        frameArt.gameObject.SetActive(false);
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
        Wire(so, "style", style);
        Wire(so, "paper", view.GetComponent<Image>());
        Wire(so, "seal", seal);
        Wire(so, "fills", fills);
        Wire(so, "slotsRoot", slots);
        Wire(so, "slotTemplate", slot);
        Wire(so, "lines", lines);
        Wire(so, "photoFrame", photoFrame);
        Wire(so, "photo", portrait);
        Wire(so, "photoFrameArt", frameArt);
        Wire(so, "textsRoot", texts);
        Wire(so, "textTemplate", text);
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
    /// The scanned-copy page template of the Investigation app's Documents tab
    /// (PC spec §2.4), cloned per paper by DocumentsView: on the scanner's dark
    /// backing (the form style's, DiegeticBacking), the document's name, the
    /// scan strip in the backing's ink, and a scroll (the pane's height) whose
    /// content is the paper's FormView at the PC page width, with an
    /// auto-hiding scrollbar. Inactive; its parent rebuilds it fresh.
    /// </summary>
    private static DocumentWindowController BuildDocumentPage(Transform parent)
    {
        FormStyleSO style = EnsureFormStyle();
        DestroyChildIfPresent(parent, "PageTemplate");
        float width = DocMargin + PcPageWidth + DocGap + DocScrollbar + DocMargin;
        Transform page = Panel(parent, "PageTemplate", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(width, 0f), style.backing, ThemeRoleId.DiegeticBacking);

        TMP_Text title = Text(page, "TitleText", UiText.Get("document.untitled"), Mathf.RoundToInt(DocTitleText), TextAlignmentOptions.MidlineLeft,
                              new Vector2(0f, 1f), Vector2.one, style.backingInk, ThemeRoleId.DiegeticBacking, style: FontStyles.Bold);
        PlaceRect(title.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(DocMargin, -(DocGap + DocTitle)), new Vector2(-DocMargin, -DocGap));
        title.raycastTarget = false;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.overflowMode = TextOverflowModes.Ellipsis;

        float stripTop = DocGap + DocTitle;
        TMP_Text strip = Text(page, "ScanStrip", string.Format(style.scanStrip, "--:--"), Mathf.RoundToInt(DocStripText), TextAlignmentOptions.MidlineLeft,
                              new Vector2(0f, 1f), Vector2.one, style.backingInk, ThemeRoleId.DiegeticBacking);
        PlaceRect(strip.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(DocMargin, -(stripTop + DocStrip)), new Vector2(-DocMargin, -stripTop));
        strip.raycastTarget = false;
        strip.textWrappingMode = TextWrappingModes.NoWrap;
        strip.overflowMode = TextOverflowModes.Ellipsis;

        ScrollRect scroll = BuildFormScroll(page, "Scroll", PcPageWidth, out FormView form);
        PlaceRect(scroll.transform, Vector2.zero, Vector2.one, new Vector2(DocMargin, DocMargin), new Vector2(-DocMargin, -(stripTop + DocStrip + DocGap)));

        DocumentWindowController c = page.gameObject.AddComponent<DocumentWindowController>();
        var so = new SerializedObject(c);
        Wire(so, "titleText", title);
        Wire(so, "scanStrip", strip);
        Wire(so, "scroll", scroll);
        Wire(so, "form", form);
        so.ApplyModifiedProperties();
        page.gameObject.SetActive(false);
        return c;
    }

    /// <summary>
    /// A form in a scroll under <paramref name="parent"/> (filling it inside
    /// the margin; callers may place it): a FormView <paramref name="formWidth"/>
    /// wide at the top centre of the scroll's viewport, as its content.
    /// </summary>
    private static ScrollRect BuildFormScroll(Transform parent, string name, float formWidth, out FormView form)
    {
        ScrollRect scroll = BuildScrollArea(parent, name, out RectTransform viewport);
        form = BuildFormView(viewport, "Form", formWidth);
        scroll.content = (RectTransform)form.transform;
        return scroll;
    }

    /// <summary>
    /// A vertical scroll under <paramref name="parent"/> (filling it inside the
    /// margin; callers may place it): a masked <paramref name="viewport"/> for
    /// the caller's content and an auto-hiding scrollbar at the right in the
    /// backing's colours. The caller sets the scroll's content.
    /// </summary>
    private static ScrollRect BuildScrollArea(Transform parent, string name, out RectTransform viewport)
    {
        FormStyleSO style = EnsureFormStyle();
        Transform area = Panel(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(area, Vector2.zero, Vector2.one, new Vector2(DocMargin, DocMargin), new Vector2(-DocMargin, -DocMargin));
        viewport = (RectTransform)Panel(area, "Viewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-(DocScrollbar + DocGap), 0f));
        viewport.gameObject.AddComponent<RectMask2D>();

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
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        return scroll;
    }

    /// <summary>The Documents tab's page template reads the shift clock for its scan strip (the clock is on the GameManager, built after the app).</summary>
    private static void WireDocumentClock(DocumentsView documents, ShiftClockDriver clock)
    {
        var view = new SerializedObject(documents);
        Object page = view.FindProperty("pageTemplate").objectReferenceValue;
        if (page == null)
        {
            Debug.LogError("[TimeDesk] The Documents tab has no page template to wire the shift clock to; fix OfficeSceneUIBuilder.");
            return;
        }
        var so = new SerializedObject(page);
        Wire(so, "clock", clock);
        so.ApplyModifiedProperties();
    }
}
