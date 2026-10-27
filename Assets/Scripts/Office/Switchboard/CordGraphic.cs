using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One switchboard cord drawn as a cel strip along its hanging curve
/// (SwitchCords): a soft drop shadow, the ink outline, the rubber in its
/// colour and a thin highlight along its upper side, built as one mesh in
/// that order. The view sets its points (in this graphic's own space) each
/// time the cord moves; it takes no clicks.
/// </summary>
public sealed class CordGraphic : MaskableGraphic
{
    private static readonly Color Ink = new Color(0.102f, 0.055f, 0.031f, 1f);
    private static readonly Color Shadow = new Color(0f, 0f, 0f, 0.28f);
    private static readonly Vector2 ShadowOffset = new Vector2(4f, -6f);

    private readonly List<Vector2> _points = new List<Vector2>();
    private float _width = 7f;

    /// <summary>Takes no clicks.</summary>
    public override bool raycastTarget { get => false; set { } }

    /// <summary>Redraws the cord along <paramref name="points"/> (local space), <paramref name="width"/> wide; an empty list hides it.</summary>
    public void SetPoints(List<Vector2> points, float width)
    {
        if ((points == null || points.Count == 0) && _points.Count == 0)
            return;
        _points.Clear();
        if (points != null)
            _points.AddRange(points);
        _width = Mathf.Max(1f, width);
        SetVerticesDirty();
    }

    /// <summary>The strips: the shadow, the outline, the rubber, the highlight.</summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_points.Count < 2)
            return;
        Strip(vh, ShadowOffset, _width + 3f, Shadow);
        Strip(vh, Vector2.zero, _width + 3.5f, Ink);
        Strip(vh, Vector2.zero, _width, color);
        Color light = Color.Lerp(color, Color.white, 0.45f);
        light.a = color.a * 0.8f;
        Strip(vh, Vector2.zero, _width * 0.32f, light, _width * 0.22f);
    }

    /// <summary>One strip of <paramref name="width"/> along the points, moved by <paramref name="offset"/> and by <paramref name="lift"/> toward the upper side.</summary>
    private void Strip(VertexHelper vh, Vector2 offset, float width, Color c, float lift = 0f)
    {
        int start = vh.currentVertCount;
        float half = width / 2f;
        // One side for the whole cord (its upper side, or its left for a cord hanging straight down), so the strip never twists.
        Vector2 overall = _points[_points.Count - 1] - _points[0];
        var across = new Vector2(-overall.y, overall.x);
        float side = across.y < 0f || (Mathf.Approximately(across.y, 0f) && across.x > 0f) ? -1f : 1f;
        for (int i = 0; i < _points.Count; i++)
        {
            Vector2 prev = _points[Mathf.Max(0, i - 1)], next = _points[Mathf.Min(_points.Count - 1, i + 1)];
            Vector2 along = next - prev;
            if (along.sqrMagnitude < 1e-6f)
                along = Vector2.right;
            along.Normalize();
            Vector2 normal = new Vector2(-along.y, along.x) * side;
            Vector2 p = _points[i] + offset + normal * lift;
            vh.AddVert(p + normal * half, c, Vector2.zero);
            vh.AddVert(p - normal * half, c, Vector2.zero);
        }
        for (int i = 0; i < _points.Count - 1; i++)
        {
            int a = start + 2 * i;
            vh.AddTriangle(a, a + 1, a + 3);
            vh.AddTriangle(a, a + 3, a + 2);
        }
    }
}
