using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A form in a scroll on the PC (redesign phase 16, the PC spec AP7, FO9):
/// the Investigation app's page kinds (the Record Extract, a book's Register,
/// the Interview Record, the Deviation Report, the Directive Memo) and a
/// paper's scanned copy each draw their FormView here. A page kind takes the
/// scroll's width (<see cref="fitsViewport"/>: it is drawn again when the
/// pane's width changes, a maximise or a split, and Redrawn tells its view
/// to mark its rows again); a document copy keeps its fixed width. Lists
/// scroll, pages do not flip: a link, Back or a search hit reveals a slot
/// (its box outlined and scrolled to the middle: AppPanes.ScrollToMiddle),
/// the keys' PgUp and PgDn move by a viewport (PageBy), and the transcript
/// follows its newest line while the view is at the bottom (AtBottom,
/// ScrollToBottom). The builder wires it (OfficeSceneUIBuilder.PcForms).
/// </summary>
public sealed class FormPage : MonoBehaviour
{
    /// <summary>The scroll the form is the content of.</summary>
    [SerializeField] private ScrollRect scroll;

    /// <summary>The form drawn in the scroll.</summary>
    [SerializeField] private FormView form;

    /// <summary>True for a page kind: the form is drawn at the viewport's width, and again when it changes; false for a document copy, drawn at its own width.</summary>
    [SerializeField] private bool fitsViewport = true;

    private FormSpec _spec;
    private FormData _data;
    private Func<FormSlot, bool> _pickable;
    private Func<FormSlot, string> _linkHint;
    private Func<FormSlot, int, string> _cellLinkHint;
    private float _drawnWidth = -1f;

    /// <summary>Raised after the form was drawn again for a new width (its slots' buttons are new: a view marks its rows again).</summary>
    public event Action Redrawn;

    /// <summary>Raised when the scroll moves (the transcript's "New line" pill goes when the player reaches the bottom).</summary>
    public event Action Scrolled;

    /// <summary>The scroll's moves are announced.</summary>
    private void Awake()
    {
        if (scroll != null)
            scroll.onValueChanged.AddListener(_ => Scrolled?.Invoke());
    }

    /// <summary>The form (its picks, links, texts and marks).</summary>
    public FormView Form => form;

    /// <summary>The form as last placed (null before the first Show).</summary>
    public PlacedForm Placed => form != null ? form.Placed : null;

    /// <summary>True while the scroll shows the content's bottom (or all of it): the transcript keeps following its newest line.</summary>
    public bool AtBottom => scroll == null || scroll.content == null || Viewport == null || scroll.content.rect.height <= Viewport.rect.height + 0.5f || scroll.verticalNormalizedPosition <= 0.001f;

    /// <summary>The scroll's viewport (the scroll's own rect without one).</summary>
    private RectTransform Viewport => scroll != null ? (scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform) : null;

    /// <summary>
    /// Draws <paramref name="spec"/> with <paramref name="data"/> (FormView.Show:
    /// the slots <paramref name="pickable"/> accepts get a button, the slots
    /// <paramref name="linkHint"/> hints a ↗, a table row's cells
    /// <paramref name="cellLinkHint"/> hints a ↗ each), scrolled to the top.
    /// Returns the placed form (null without a form).
    /// </summary>
    public PlacedForm Show(FormSpec spec, FormData data, Func<FormSlot, bool> pickable = null, Func<FormSlot, string> linkHint = null, Func<FormSlot, int, string> cellLinkHint = null)
    {
        _spec = spec;
        _data = data;
        _pickable = pickable;
        _linkHint = linkHint;
        _cellLinkHint = cellLinkHint;
        PlacedForm placed = Draw();
        ScrollToTop();
        return placed;
    }

    /// <summary>Outlines slot <paramref name="slot"/>'s box (the one a link went to) and scrolls it to the middle; -1 only clears the outline.</summary>
    public void Reveal(int slot)
    {
        if (form == null)
            return;
        form.MarkFound(slot);
        ScrollToSlot(slot);
    }

    /// <summary>Scrolls slot <paramref name="slot"/>'s box to the middle of the viewport (nothing for -1 or a slot the form has not).</summary>
    public void ScrollToSlot(int slot)
    {
        PlacedForm placed = Placed;
        if (scroll == null || placed == null || slot < 0 || slot >= placed.Slots.Count || Viewport == null)
            return;
        FaceRect box = placed.Slots[slot].Hit;
        scroll.verticalNormalizedPosition = AppPanes.ScrollToMiddle(placed.Height, Viewport.rect.height, box.YMin, box.Height);
    }

    /// <summary>The top of the page.</summary>
    public void ScrollToTop()
    {
        if (scroll != null)
            scroll.verticalNormalizedPosition = 1f;
    }

    /// <summary>The bottom of the page (the newest transcript line).</summary>
    public void ScrollToBottom()
    {
        if (scroll != null)
            scroll.verticalNormalizedPosition = 0f;
    }

    /// <summary>PgDn (<paramref name="direction"/> 1) or PgUp (-1): the page moves by a viewport's height; false when it is already at that end (or nothing scrolls).</summary>
    public bool PageBy(int direction)
    {
        if (scroll == null || scroll.content == null || Viewport == null)
            return false;
        float travel = scroll.content.rect.height - Viewport.rect.height;
        if (travel <= 0.5f)
            return false;
        float at = scroll.verticalNormalizedPosition;
        float to = Mathf.Clamp01(at - direction * Viewport.rect.height / travel);
        if (Mathf.Abs(to - at) < 0.0005f)
            return false;
        scroll.verticalNormalizedPosition = to;
        return true;
    }

    /// <summary>A page kind drawn for a width the viewport no longer has (the pane resized, the window restored) is drawn again.</summary>
    private void OnRectTransformDimensionsChange() => RedrawForWidth();

    /// <summary>A page kind shown again (its tab, its pane) at a new width is drawn again.</summary>
    private void OnEnable() => RedrawForWidth();

    private void RedrawForWidth()
    {
        if (_spec != null && fitsViewport && Mathf.Abs(Width - _drawnWidth) > 0.5f)
            Draw();
    }

    /// <summary>The width the form is drawn at: the viewport's for a page kind (once it has one), else the form's own.</summary>
    private float Width
    {
        get
        {
            float viewport = fitsViewport && Viewport != null ? Viewport.rect.width : 0f;
            return viewport > 1f ? viewport : 0f;
        }
    }

    /// <summary>Draws the last shown form at the current width and tells the view.</summary>
    private PlacedForm Draw()
    {
        if (form == null)
            return null;
        float width = Width;
        PlacedForm placed = form.Show(_spec, _data, _pickable, width, _linkHint, _cellLinkHint);
        _drawnWidth = width;
        Redrawn?.Invoke();
        return placed;
    }
}
