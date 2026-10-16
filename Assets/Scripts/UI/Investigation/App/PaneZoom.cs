using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One pane's zoom (the PC redesign KB5; Settings' Text size): on the pane's
/// content, which is the viewport, a zoom root holds the views, laid out at
/// the viewport's size over the scale and scaled about its top-left corner,
/// so at 125 or 150 % the words grow and wrap to the pane's width, as a
/// browser's zoom does, and each view scrolls down in its own scroll (QA2
/// 2026-10-16: scaled at the viewport's own size, the case board's chips and
/// the memo's right edge ran past the pane, cut off). The pane itself never
/// scrolls.
/// </summary>
public sealed class PaneZoom : MonoBehaviour
{
    /// <summary>The viewport's scroll (kept still: the content always fits it).</summary>
    [SerializeField] private ScrollRect scroll;

    /// <summary>The views' parent, stretched to the viewport, pivoted at its top-left.</summary>
    [SerializeField] private RectTransform zoomRoot;

    /// <summary>The scale shown (1 = 100 %).</summary>
    public float Scale { get; private set; } = 1f;

    /// <summary>Shows the content at <paramref name="scale"/>, from its top-left, fitting the viewport.</summary>
    public void Apply(float scale)
    {
        Scale = Mathf.Max(0.1f, scale);
        if (scroll != null)
        {
            scroll.horizontal = false;
            scroll.vertical = false;
            scroll.StopMovement();
        }
        Fit();
    }

    /// <summary>The pane was resized (the window, the split, the findings column): the content fits it again.</summary>
    private void OnRectTransformDimensionsChange() => Fit();

    /// <summary>Lays the views out at the viewport's size over the scale, so scaled they cover it exactly.</summary>
    private void Fit()
    {
        if (zoomRoot == null)
            return;
        Vector2 view = ((RectTransform)transform).rect.size;
        zoomRoot.localScale = new Vector3(Scale, Scale, 1f);
        zoomRoot.sizeDelta = view / Scale - view;
        zoomRoot.anchoredPosition = Vector2.zero;
    }
}
