using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

/// <summary>
/// The hover highlight on desktop UI. A dedicated Outline subclass, so the
/// HoverHighlighter never toggles an Outline a designer added for styling.
/// On themed UI it draws two rings: the inner one in effectColor at the
/// effect distance and, beneath it, the outer one in outerColor at twice the
/// distance; the theme's two colours are far enough apart that one of them
/// always contrasts with what is behind the control (piece 6 R7). On a
/// see-through graphic (a form's value box) the rings are frames outside
/// the box and the box gets a light wash, so the value stays readable.
/// </summary>
public sealed class HoverUIOutline : Outline
{
    /// <summary>Draw the outer ring too (themed UI).</summary>
    public bool twoRings;

    /// <summary>The outer ring's colour.</summary>
    public Color outerColor;

    /// <summary>A graphic under this alpha is see-through: a form's value box, clear, with its hover tint (6 %) or with the pick tint (70 %).</summary>
    private const float SeeThrough = 0.75f;

    /// <summary>The wash under a see-through graphic, as a share of the inner ring's alpha (Saleh, 2026-11-13: "Yes, 35 %").</summary>
    private const float SeeThroughWash = 0.35f;

    /// <summary>The outer ring (when on) beneath the inner ring, beneath the graphic.</summary>
    public override void ModifyMesh(VertexHelper vh)
    {
        if (IsActive() && graphic != null && graphic.color.a < SeeThrough)
        {
            Frames(vh);
            return;
        }
        if (!twoRings)
        {
            base.ModifyMesh(vh);
            return;
        }
        if (!IsActive())
            return;

        List<UIVertex> verts = ListPool<UIVertex>.Get();
        vh.GetUIVertexStream(verts);
        int start = 0, end = verts.Count;
        Ring(verts, ref start, ref end, outerColor, effectDistance * 2f);
        Ring(verts, ref start, ref end, effectColor, effectDistance);
        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);
        ListPool<UIVertex>.Release(verts);
    }

    /// <summary>
    /// A see-through graphic (a form's value box: its fill is clear or a light tint). Outline's offset copies
    /// would show through it as a solid block over the value. So the rings are frames outside the box
    /// (the inner one next to it, the outer one around that) and the box gets a light wash in the inner colour.
    /// </summary>
    private void Frames(VertexHelper vh)
    {
        List<UIVertex> own = ListPool<UIVertex>.Get();
        vh.GetUIVertexStream(own);
        List<UIVertex> all = ListPool<UIVertex>.Get();
        Vector2 uv = own.Count > 0 ? (Vector2)own[0].uv0 : Vector2.zero;
        Rect box = graphic.rectTransform.rect;
        var d = new Vector2(Mathf.Abs(effectDistance.x), Mathf.Abs(effectDistance.y));
        Rect inner = Rect.MinMaxRect(box.xMin - d.x, box.yMin - d.y, box.xMax + d.x, box.yMax + d.y);
        Color wash = effectColor;
        wash.a *= SeeThroughWash;
        Quad(all, box, wash, uv);
        if (twoRings)
            Frame(all, inner, Rect.MinMaxRect(inner.xMin - d.x, inner.yMin - d.y, inner.xMax + d.x, inner.yMax + d.y), outerColor, uv);
        Frame(all, box, inner, effectColor, uv);
        all.AddRange(own);
        vh.Clear();
        vh.AddUIVertexTriangleStream(all);
        ListPool<UIVertex>.Release(own);
        ListPool<UIVertex>.Release(all);
    }

    /// <summary>The band between <paramref name="hole"/> and <paramref name="outer"/>, as four quads.</summary>
    private static void Frame(List<UIVertex> verts, Rect hole, Rect outer, Color color, Vector2 uv)
    {
        Quad(verts, Rect.MinMaxRect(outer.xMin, hole.yMax, outer.xMax, outer.yMax), color, uv);
        Quad(verts, Rect.MinMaxRect(outer.xMin, outer.yMin, outer.xMax, hole.yMin), color, uv);
        Quad(verts, Rect.MinMaxRect(outer.xMin, hole.yMin, hole.xMin, hole.yMax), color, uv);
        Quad(verts, Rect.MinMaxRect(hole.xMax, hole.yMin, outer.xMax, hole.yMax), color, uv);
    }

    /// <summary>A rectangle as two triangles.</summary>
    private static void Quad(List<UIVertex> verts, Rect r, Color color, Vector2 uv)
    {
        UIVertex v = UIVertex.simpleVert;
        v.color = color;
        v.uv0 = uv;
        foreach (Vector2 p in new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax),
                                      new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin), new Vector2(r.xMin, r.yMin) })
        {
            v.position = p;
            verts.Add(v);
        }
    }

    /// <summary>Four offset copies (as Outline draws them) of the vertices in [start, end), which move to the end.</summary>
    private void Ring(List<UIVertex> verts, ref int start, ref int end, Color color, Vector2 d)
    {
        foreach (Vector2 offset in new[] { new Vector2(d.x, d.y), new Vector2(d.x, -d.y), new Vector2(-d.x, d.y), new Vector2(-d.x, -d.y) })
        {
            ApplyShadowZeroAlloc(verts, color, start, verts.Count, offset.x, offset.y);
            start = end;
            end = verts.Count;
        }
    }
}
