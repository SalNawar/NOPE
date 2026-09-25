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
/// always contrasts with what is behind the control (piece 6 R7).
/// </summary>
public sealed class HoverUIOutline : Outline
{
    /// <summary>Draw the outer ring too (themed UI).</summary>
    public bool twoRings;

    /// <summary>The outer ring's colour.</summary>
    public Color outerColor;

    /// <summary>The outer ring (when on) beneath the inner ring, beneath the graphic.</summary>
    public override void ModifyMesh(VertexHelper vh)
    {
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
