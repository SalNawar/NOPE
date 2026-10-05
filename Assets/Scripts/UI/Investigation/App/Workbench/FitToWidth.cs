using UnityEngine;

/// <summary>
/// Scales a page drawn at a fixed width (a paper's scanned copy) to its
/// parent's width, from the top (wave 5 A3: a document fits its pane, larger
/// in a wide pane and smaller in a narrow one, and scrolls down only when it
/// is taller than the pane). The page keeps its own layout (its fields never
/// reflow: the document design spec D2); only its scale changes, kept within
/// <see cref="scaleRange"/>, and its height is the parent's at that scale, so
/// its scroll shows the rest. Followed every frame the parent's size changes
/// (a window maximised, a pane split, the findings rail opened).
/// </summary>
public sealed class FitToWidth : MonoBehaviour
{
    /// <summary>The page's own width (desktop units), shown at scale 1.</summary>
    [SerializeField, Min(1f)] private float designWidth = 558f;

    /// <summary>The least and the most the page is scaled.</summary>
    [SerializeField] private Vector2 scaleRange = new Vector2(0.75f, 1.5f);

    private Vector2 _fitted = new Vector2(-1f, -1f);

    /// <summary>The scale shown.</summary>
    public float Scale { get; private set; } = 1f;

    private void OnEnable()
    {
        _fitted = new Vector2(-1f, -1f);
        Fit();
    }

    private void LateUpdate() => Fit();

    /// <summary>The page at its parent's width, pinned to its top centre, as tall as the parent at that scale.</summary>
    private void Fit()
    {
        if (!(transform.parent is RectTransform parent) || !(transform is RectTransform page))
            return;
        Vector2 size = parent.rect.size;
        if (size == _fitted || size.x <= 0f || size.y <= 0f)
            return;
        _fitted = size;
        Scale = Mathf.Clamp(size.x / designWidth, scaleRange.x, scaleRange.y);
        page.anchorMin = page.anchorMax = page.pivot = new Vector2(0.5f, 1f);
        page.localScale = new Vector3(Scale, Scale, 1f);
        page.sizeDelta = new Vector2(designWidth, size.y / Scale);
        page.anchoredPosition = Vector2.zero;
    }
}
