using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A layout group that places its children left to right at their preferred
/// sizes and wraps onto a new row when the next one would pass the right
/// edge (the search drawer's source chips: the PC workbench spec IA9). Each
/// row is as tall as its tallest child, children centred in it; its
/// preferred height is the rows' (it grows rather than scrolling sideways).
/// </summary>
public sealed class FlowLayoutGroup : LayoutGroup
{
    /// <summary>The gap between two children of a row.</summary>
    [SerializeField, Min(0f)] private float spacingX = 8f;

    /// <summary>The gap between two rows.</summary>
    [SerializeField, Min(0f)] private float spacingY = 8f;

    private readonly List<float> _rowHeights = new List<float>();

    /// <inheritdoc />
    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        float widest = 0f;
        foreach (RectTransform child in rectChildren)
            widest = Mathf.Max(widest, LayoutUtility.GetPreferredWidth(child));
        // The least width is the padding alone: a child wider than the row is clamped to it (its text wraps), never pushing the row past its parent.
        SetLayoutInputForAxis(padding.horizontal, padding.horizontal + widest, -1f, 0);
    }

    /// <inheritdoc />
    public override void CalculateLayoutInputVertical()
    {
        float height = Arrange(false);
        SetLayoutInputForAxis(height, height, -1f, 1);
    }

    /// <inheritdoc />
    public override void SetLayoutHorizontal() => Arrange(true);

    /// <inheritdoc />
    public override void SetLayoutVertical() => Arrange(true);

    /// <summary>Child <paramref name="i"/>'s width, clamped to the row's.</summary>
    private float Width(int i, float row) => Mathf.Min(LayoutUtility.GetPreferredWidth(rectChildren[i]), row);

    /// <summary>True when child <paramref name="i"/> starts a new row at <paramref name="x"/>: it would pass the right edge.</summary>
    private bool Wraps(int i, float x, float row) => x > 0f && x + Width(i, row) > row;

    /// <summary>Walks the children into rows (placing them when <paramref name="place"/>) and returns the height they take with the padding.</summary>
    private float Arrange(bool place)
    {
        float row = rectTransform.rect.width - padding.horizontal;
        _rowHeights.Clear();
        float x = 0f, tallest = 0f;
        for (int i = 0; i < rectChildren.Count; i++)
        {
            if (Wraps(i, x, row))
            {
                _rowHeights.Add(tallest);
                x = 0f;
                tallest = 0f;
            }
            tallest = Mathf.Max(tallest, LayoutUtility.GetPreferredHeight(rectChildren[i]));
            x += Width(i, row) + spacingX;
        }
        if (rectChildren.Count > 0)
            _rowHeights.Add(tallest);

        float total = padding.vertical;
        for (int i = 0; i < _rowHeights.Count; i++)
            total += _rowHeights[i] + (i > 0 ? spacingY : 0f);
        if (!place)
            return total;

        int r = 0;
        x = 0f;
        float y = padding.top;
        for (int i = 0; i < rectChildren.Count; i++)
        {
            RectTransform child = rectChildren[i];
            float w = Width(i, row), h = LayoutUtility.GetPreferredHeight(child);
            if (Wraps(i, x, row))
            {
                y += _rowHeights[r] + spacingY;
                r++;
                x = 0f;
            }
            float rowHeight = r < _rowHeights.Count ? _rowHeights[r] : h;
            SetChildAlongAxis(child, 0, padding.left + x, w);
            SetChildAlongAxis(child, 1, y + (rowHeight - h) / 2f, h);
            x += w + spacingX;
        }
        return total;
    }
}
