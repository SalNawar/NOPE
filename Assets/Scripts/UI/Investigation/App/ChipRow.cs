using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A pane header's chip row (Saleh 2026-09-29, "Scroll the row"): the chips
/// keep their size (a chip is as wide as its label at the chip size, never
/// squeezed) in a row that scrolls sideways when they do not fit, like a
/// browser's tabs: a small arrow then shows at each end (greyed while there
/// is nothing more that way) and brings the next chip cut or hidden on its
/// side whole into view (AppPanes.NextHidden, AppPanes.RevealOffset); the
/// mouse wheel and a drag scroll it too (its ScrollRect, sideways only). The
/// chosen chip is scrolled into view when the chips are drawn (AppPane), and
/// so is the chip the keys' focus ring moves to (InvestigationApp).
/// </summary>
public sealed class ChipRow : MonoBehaviour
{
    /// <summary>Half a unit: a row this much wider than its room still fits.</summary>
    private const float Slack = 0.5f;

    /// <summary>The row's sideways scroll (its viewport and content are the two below).</summary>
    [SerializeField] private ScrollRect scroll;

    /// <summary>The window the chips show through (it cuts them and the focus ring).</summary>
    [SerializeField] private RectTransform viewport;

    /// <summary>The chips' parent: as wide as its chips, pivoted at its left.</summary>
    [SerializeField] private RectTransform content;

    /// <summary>The arrow at the row's left end.</summary>
    [SerializeField] private Button left;

    /// <summary>The arrow at the row's right end.</summary>
    [SerializeField] private Button right;

    /// <summary>Each arrow's width: the window gives it room while the arrows show.</summary>
    [SerializeField] private float arrowWidth = 24f;

    private readonly List<float> _starts = new List<float>();
    private readonly List<float> _ends = new List<float>();
    private bool _wired;
    private bool _dirty;

    /// <summary>The window the chips show through.</summary>
    public RectTransform Viewport => viewport;

    private void Awake() => Wire();

    /// <summary>The row shows again: it is measured again.</summary>
    private void OnEnable() => _dirty = true;

    /// <summary>The row's width changed (a split, the window restored): it is measured again at the frame's end.</summary>
    private void OnRectTransformDimensionsChange() => _dirty = true;

    private void LateUpdate()
    {
        if (_dirty)
            Refresh(null);
    }

    /// <summary>
    /// The chips were drawn or the row resized: the arrows show when the chips
    /// are wider than the row (the window then leaves them room), the scroll
    /// stays within the row, and <paramref name="chosen"/> (a chip; null: none)
    /// is scrolled whole into view.
    /// </summary>
    public void Refresh(RectTransform chosen)
    {
        Wire();
        _dirty = false;
        if (scroll == null || viewport == null || content == null || !isActiveAndEnabled)
            return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        bool overflow = content.rect.width > ((RectTransform)transform).rect.width + Slack;
        float inset = overflow ? arrowWidth : 0f;
        if (viewport.offsetMin.x != inset)
        {
            viewport.offsetMin = new Vector2(inset, viewport.offsetMin.y);
            viewport.offsetMax = new Vector2(-inset, viewport.offsetMax.y);
        }
        Show(left, overflow);
        Show(right, overflow);
        ScrollTo(Offset);
        if (chosen != null)
            Reveal(chosen);
    }

    /// <summary>Scrolls <paramref name="chip"/> (one of the row's chips) whole into view, moving as little as it can.</summary>
    public void Reveal(RectTransform chip)
    {
        if (content == null || viewport == null || chip == null || !chip.IsChildOf(content))
            return;
        (float start, float end) = Span(chip);
        ScrollTo(AppPanes.RevealOffset(Offset, viewport.rect.width, content.rect.width, start, end));
    }

    /// <summary>An arrow: the next chip cut or hidden on its side (<paramref name="direction"/> -1 left, 1 right) comes whole into view.</summary>
    private void Step(int direction)
    {
        Measure();
        int i = AppPanes.NextHidden(_starts, _ends, Offset, viewport.rect.width, direction);
        if (i >= 0)
            ScrollTo(AppPanes.RevealOffset(Offset, viewport.rect.width, content.rect.width, _starts[i], _ends[i]));
    }

    /// <summary>How far the row is scrolled (its width out of view to the left).</summary>
    private float Offset => content != null ? -content.anchoredPosition.x : 0f;

    /// <summary>Scrolls to <paramref name="offset"/>, clamped to the row's ends; the arrows follow.</summary>
    private void ScrollTo(float offset)
    {
        float max = Mathf.Max(0f, content.rect.width - viewport.rect.width);
        scroll.StopMovement();
        content.anchoredPosition = new Vector2(-Mathf.Clamp(offset, 0f, max), content.anchoredPosition.y);
        UpdateArrows();
    }

    /// <summary>Each arrow greys while there is nothing more on its side.</summary>
    private void UpdateArrows()
    {
        Measure();
        float window = viewport.rect.width;
        if (left != null)
            left.interactable = AppPanes.NextHidden(_starts, _ends, Offset, window, -1) >= 0;
        if (right != null)
            right.interactable = AppPanes.NextHidden(_starts, _ends, Offset, window, 1) >= 0;
    }

    /// <summary>The shown chips' spans from the row's start, in order.</summary>
    private void Measure()
    {
        _starts.Clear();
        _ends.Clear();
        foreach (RectTransform chip in content)
        {
            if (!chip.gameObject.activeSelf || chip.GetComponent<Button>() == null)
                continue;
            (float start, float end) = Span(chip);
            _starts.Add(start);
            _ends.Add(end);
        }
    }

    /// <summary>A chip's span from the row's start (the content is pivoted at its left).</summary>
    private static (float start, float end) Span(RectTransform chip)
    {
        float start = chip.localPosition.x - chip.pivot.x * chip.rect.width;
        return (start, start + chip.rect.width);
    }

    /// <summary>Wires the arrows and the scroll (once).</summary>
    private void Wire()
    {
        if (_wired)
            return;
        _wired = true;
        if (left != null)
            left.onClick.AddListener(() => Step(-1));
        if (right != null)
            right.onClick.AddListener(() => Step(1));
        if (scroll != null)
            scroll.onValueChanged.AddListener(_ => UpdateArrows());
    }

    private static void Show(Button arrow, bool on)
    {
        if (arrow != null && arrow.gameObject.activeSelf != on)
            arrow.gameObject.SetActive(on);
    }
}
