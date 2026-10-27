using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scales a page drawn at a fixed width (a paper's scanned copy) to its
/// pane's width, from the top (wave 5 A3: a document fits its pane, larger
/// in a wide pane and smaller in a narrow one, and scrolls down only when it
/// is taller than the pane), under the pane's text zoom (CODE-111: PaneZoom
/// lays its views out at the pane's width over the zoom, so fitting to the
/// parent's width undid the zoom; PageFit fits the pane's own width, so
/// Ctrl+= and Settings' Text size grow a scanned page as they grow every
/// other text). The page keeps its own layout (its fields never reflow: the
/// document design spec D2); only its scale changes, kept within
/// <see cref="scaleRange"/>. Its box is its parent's size at that scale,
/// never wider, so the pane's mask never cuts an edge: a page wider than its
/// box (zoomed in, or a narrow split pane under the floor) scrolls sideways
/// in its own scroll, from its left edge. Followed every frame the parent's
/// size or the zoom changes (a window maximised, a pane split, the findings
/// rail opened, a zoom step).
/// </summary>
public sealed class FitToWidth : MonoBehaviour
{
    /// <summary>The page's own width (desktop units), shown at scale 1.</summary>
    [SerializeField, Min(1f)] private float designWidth = 558f;

    /// <summary>The least and the most the page is scaled to fit its pane (the text zoom multiplies it on screen).</summary>
    [SerializeField] private Vector2 scaleRange = new Vector2(0.75f, 1.5f);

    /// <summary>The parent's size and the zoom last fitted (x, y, zoom; negative: none yet).</summary>
    private Vector3 _fitted = new Vector3(-1f, -1f, -1f);

    /// <summary>The pane's text zoom above the page (found when shown; none: 100 %).</summary>
    private PaneZoom _zoom;

    /// <summary>The page's own scroll (the form in it), which moves sideways while the page is wider than its box.</summary>
    private ScrollRect _scroll;

    /// <summary>The scale shown (the page's own; the pane's zoom multiplies it on screen).</summary>
    public float Scale { get; private set; } = 1f;

    private void OnEnable()
    {
        _zoom = GetComponentInParent<PaneZoom>(true);
        _scroll = GetComponentInChildren<ScrollRect>(true);
        _fitted = new Vector3(-1f, -1f, -1f);
        Fit();
    }

    private void LateUpdate() => Fit();

    /// <summary>The page at its pane's width under the zoom (PageFit), pinned to its top centre, its box the parent's size at that scale; sideways scrolling while it is wider than the box.</summary>
    private void Fit()
    {
        if (!(transform.parent is RectTransform parent) || !(transform is RectTransform page))
            return;
        Vector2 size = parent.rect.size;
        float zoom = _zoom != null ? _zoom.Scale : 1f;
        var now = new Vector3(size.x, size.y, zoom);
        if (now == _fitted || size.x <= 0f || size.y <= 0f)
            return;
        _fitted = now;
        PageFitResult fit = PageFit.Of(size.x, size.y, zoom, designWidth, scaleRange.x, scaleRange.y);
        Scale = fit.Scale;
        page.anchorMin = page.anchorMax = page.pivot = new Vector2(0.5f, 1f);
        page.localScale = new Vector3(Scale, Scale, 1f);
        page.sizeDelta = new Vector2(fit.Width, fit.Height);
        page.anchoredPosition = Vector2.zero;
        Sideways(fit.Sideways);
    }

    /// <summary>The page's scroll moves sideways (from the page's left edge) while it is wider than its box, and is centred again when it fits.</summary>
    private void Sideways(bool on)
    {
        if (_scroll == null || _scroll.content == null)
            return;
        bool was = _scroll.horizontal;
        _scroll.horizontal = on;
        if (on && !was)
            _scroll.horizontalNormalizedPosition = 0f;
        else if (!on && was)
            _scroll.content.anchoredPosition = new Vector2(0f, _scroll.content.anchoredPosition.y);
    }
}
