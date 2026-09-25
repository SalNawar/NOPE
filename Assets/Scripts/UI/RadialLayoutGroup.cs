using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lays its active children out on an ellipse (RadialLayout.Point: item 0 at
/// the top, then clockwise), each at <see cref="ItemSize"/>, centred on this
/// rect's pivot. A child with LayoutElement.ignoreLayout (the traveller wheel's
/// centre slot) is left alone. Reports no preferred size.
/// </summary>
public sealed class RadialLayoutGroup : LayoutGroup
{
    /// <summary>The ellipse's horizontal and vertical radii (reference px).</summary>
    [SerializeField] private Vector2 radii = new Vector2(300f, 200f);

    /// <summary>Every item's size (reference px).</summary>
    [SerializeField] private Vector2 itemSize = new Vector2(240f, 44f);

    /// <summary>The ellipse's radii; setting it re-lays the ring.</summary>
    public Vector2 Radii
    {
        get => radii;
        set
        {
            radii = value;
            SetDirty();
        }
    }

    /// <summary>Every item's size; setting it re-lays the ring.</summary>
    public Vector2 ItemSize
    {
        get => itemSize;
        set
        {
            itemSize = value;
            SetDirty();
        }
    }

    /// <summary>Collects the laid-out children (the base class) and reports no preferred width.</summary>
    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        SetLayoutInputForAxis(0f, 0f, -1f, 0);
    }

    /// <summary>Reports no preferred height.</summary>
    public override void CalculateLayoutInputVertical() => SetLayoutInputForAxis(0f, 0f, -1f, 1);

    /// <summary>Places the items (both axes at once).</summary>
    public override void SetLayoutHorizontal() => Place();

    /// <summary>Places the items (both axes at once).</summary>
    public override void SetLayoutVertical() => Place();

    /// <summary>Puts child i of n at RadialLayout.Point(i, n) around the pivot, at the item size.</summary>
    private void Place()
    {
        int n = rectChildren.Count;
        var middle = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < n; i++)
        {
            RectTransform child = rectChildren[i];
            m_Tracker.Add(this, child, DrivenTransformProperties.Anchors | DrivenTransformProperties.Pivot |
                                       DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.SizeDelta);
            (float x, float y) = RadialLayout.Point(i, n, radii.x, radii.y);
            child.anchorMin = middle;
            child.anchorMax = middle;
            child.pivot = middle;
            child.sizeDelta = itemSize;
            child.anchoredPosition = new Vector2(x, y);
        }
    }
}
