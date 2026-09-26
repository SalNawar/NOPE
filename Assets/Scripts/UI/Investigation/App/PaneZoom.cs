using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One pane's zoom (the PC redesign KB5): on the pane's content, which is the
/// viewport, a zoom root holds the views stretched to the viewport and scaled
/// about its top-left corner, so at 125 or 150 % the content grows right and
/// down and scrolls both ways (the ScrollRect: the mouse wheel, a drag, the
/// focus ring's row brought into view); at 100 % it fits and does not scroll.
/// </summary>
public sealed class PaneZoom : MonoBehaviour
{
    /// <summary>The viewport's scroll (horizontal and vertical, clamped).</summary>
    [SerializeField] private ScrollRect scroll;

    /// <summary>The views' parent, stretched to the viewport, pivoted at its top-left.</summary>
    [SerializeField] private RectTransform zoomRoot;

    private readonly Vector3[] _corners = new Vector3[4];

    /// <summary>The scale shown (1 = 100 %).</summary>
    public float Scale { get; private set; } = 1f;

    /// <summary>Shows the content at <paramref name="scale"/>, from its top-left.</summary>
    public void Apply(float scale)
    {
        Scale = Mathf.Max(0.1f, scale);
        if (zoomRoot == null || scroll == null)
            return;
        zoomRoot.localScale = new Vector3(Scale, Scale, 1f);
        zoomRoot.anchoredPosition = Vector2.zero;
        bool zoomed = Scale > 1.001f;
        scroll.horizontal = zoomed;
        scroll.vertical = zoomed;
        scroll.StopMovement();
    }

    /// <summary>Scrolls just enough to show <paramref name="target"/> (a focused row) inside the viewport.</summary>
    public void Reveal(RectTransform target)
    {
        if (target == null || zoomRoot == null || Scale <= 1.001f)
            return;
        var viewport = (RectTransform)transform;
        target.GetWorldCorners(_corners);
        Vector2 min = viewport.InverseTransformPoint(_corners[0]);
        Vector2 max = viewport.InverseTransformPoint(_corners[2]);
        Rect view = viewport.rect;
        Vector2 shift = Vector2.zero;
        if (max.x > view.xMax)
            shift.x = view.xMax - max.x;
        if (min.x + shift.x < view.xMin)
            shift.x = view.xMin - min.x;
        if (min.y < view.yMin)
            shift.y = view.yMin - min.y;
        if (max.y + shift.y > view.yMax)
            shift.y = view.yMax - max.y;
        zoomRoot.anchoredPosition += shift;
        Clamp(view);
    }

    /// <summary>Keeps the scaled content covering the viewport (no gap at its right or bottom).</summary>
    private void Clamp(Rect view)
    {
        Vector2 size = view.size * Scale;
        Vector2 at = zoomRoot.anchoredPosition;
        at.x = Mathf.Clamp(at.x, view.width - size.x, 0f);
        at.y = Mathf.Clamp(at.y, 0f, size.y - view.height);
        zoomRoot.anchoredPosition = at;
    }
}
