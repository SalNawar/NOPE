using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A magnifying glass drawn in code (Saleh 2026-10-06: "the inspect mode
/// should be a magnify glass icon"): a lens ring in the upper left of this
/// graphic's rect, a faint glass inside it with a small glint, and a thick
/// handle running from the ring to the bottom right corner, all in the
/// graphic's colour (the glass and the glint fainter). The red inspect
/// button's icon; Build Office UI places it over the button's key label.
/// Never a raycast target (the button takes the click).
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MagnifierGlyph : MaskableGraphic
{
    /// <summary>The ring's thickness, as a share of the lens's radius.</summary>
    [SerializeField, Range(0.05f, 0.6f)] private float ringShare = 0.24f;

    /// <summary>The handle's thickness, as a share of the lens's radius.</summary>
    [SerializeField, Range(0.1f, 1f)] private float handleShare = 0.42f;

    /// <summary>The glass's opacity inside the ring (a share of the colour's alpha).</summary>
    [SerializeField, Range(0f, 1f)] private float glassAlpha = 0.18f;

    /// <summary>The segments a circle is drawn with.</summary>
    private const int Segments = 40;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    /// <summary>The lens (a ring over a faint disc, a glint) and the handle at 45 degrees down to the right.</summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        float side = Mathf.Min(r.width, r.height);
        float radius = side * 0.34f;
        Vector2 centre = new Vector2(r.center.x - side * 0.12f, r.center.y + side * 0.12f);
        Color ink = color;
        Color glass = new Color(ink.r, ink.g, ink.b, ink.a * glassAlpha);
        float ring = radius * ringShare;

        Disc(vh, centre, radius - ring, glass);
        Ring(vh, centre, radius - ring, radius, ink);
        Arc(vh, centre, (radius - ring) * 0.55f, (radius - ring) * 0.72f, 110f, 170f, new Color(ink.r, ink.g, ink.b, ink.a * 0.7f));

        Vector2 direction = new Vector2(1f, -1f).normalized;
        Vector2 from = centre + direction * radius * 0.92f;
        Vector2 to = new Vector2(r.center.x + side * 0.44f, r.center.y - side * 0.44f);
        Vector2 across = new Vector2(-direction.y, direction.x) * radius * handleShare / 2f;
        Quad(vh, from - across, from + across, to + across, to - across, ink);
        Disc(vh, to, radius * handleShare / 2f, ink);
    }

    /// <summary>A filled circle.</summary>
    private static void Disc(VertexHelper vh, Vector2 centre, float radius, Color colour)
    {
        int start = vh.currentVertCount;
        vh.AddVert(centre, colour, Vector4.zero);
        for (int i = 0; i <= Segments; i++)
        {
            float a = i * Mathf.PI * 2f / Segments;
            vh.AddVert(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, colour, Vector4.zero);
        }
        for (int i = 0; i < Segments; i++)
            vh.AddTriangle(start, start + 1 + i, start + 2 + i);
    }

    /// <summary>A whole ring between two radii.</summary>
    private static void Ring(VertexHelper vh, Vector2 centre, float inner, float outer, Color colour) => Arc(vh, centre, inner, outer, 0f, 360f, colour);

    /// <summary>A band between two radii from one angle to another (degrees, anticlockwise from the right).</summary>
    private static void Arc(VertexHelper vh, Vector2 centre, float inner, float outer, float fromDegrees, float toDegrees, Color colour)
    {
        int steps = Mathf.Max(2, Mathf.CeilToInt(Segments * Mathf.Abs(toDegrees - fromDegrees) / 360f));
        for (int i = 0; i < steps; i++)
        {
            float a0 = Mathf.Lerp(fromDegrees, toDegrees, i / (float)steps) * Mathf.Deg2Rad;
            float a1 = Mathf.Lerp(fromDegrees, toDegrees, (i + 1) / (float)steps) * Mathf.Deg2Rad;
            Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)), d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            Quad(vh, centre + d0 * inner, centre + d0 * outer, centre + d1 * outer, centre + d1 * inner, colour);
        }
    }

    /// <summary>A quad from four corners in order.</summary>
    private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color colour)
    {
        int start = vh.currentVertCount;
        vh.AddVert(a, colour, Vector4.zero);
        vh.AddVert(b, colour, Vector4.zero);
        vh.AddVert(c, colour, Vector4.zero);
        vh.AddVert(d, colour, Vector4.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }
}
