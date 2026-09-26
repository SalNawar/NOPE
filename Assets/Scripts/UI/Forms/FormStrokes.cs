using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One printed layer of a form on the PC (redesign phase 5): FormPaint's
/// quads of one layer (the fills under the slots' tints, or the lines over
/// them) as a single uGUI mesh, the uGUI twin of the desk paper's meshes.
/// The quads are in form space (the page's top-left, y down), laid over this
/// graphic's rect, which spans the form. Never a raycast target; tagged
/// DiegeticForm by the builder, so the theme leaves it alone.
/// </summary>
public sealed class FormStrokes : MaskableGraphic
{
    private readonly List<FormQuad> _quads = new List<FormQuad>();

    /// <summary>Replaces the quads with those of <paramref name="layer"/> in <paramref name="quads"/> (null: none) and redraws.</summary>
    public void Set(IReadOnlyList<FormQuad> quads, FormPaintLayer layer)
    {
        _quads.Clear();
        if (quads != null)
            foreach (FormQuad q in quads)
                if (q.Layer == layer)
                    _quads.Add(q);
        SetVerticesDirty();
    }

    /// <summary>Each quad as two triangles in its own colour, from the form's top-left.</summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        foreach (FormQuad q in _quads)
        {
            var colour = (Color32)new Color(q.Colour.R, q.Colour.G, q.Colour.B, q.Colour.A);
            float x0 = r.xMin + q.Rect.XMin, x1 = r.xMin + q.Rect.XMax;
            float y0 = r.yMax - q.Rect.YMax, y1 = r.yMax - q.Rect.YMin;
            int v = vh.currentVertCount;
            vh.AddVert(new Vector3(x0, y0), colour, Vector2.zero);
            vh.AddVert(new Vector3(x0, y1), colour, Vector2.zero);
            vh.AddVert(new Vector3(x1, y1), colour, Vector2.zero);
            vh.AddVert(new Vector3(x1, y0), colour, Vector2.zero);
            vh.AddTriangle(v, v + 1, v + 2);
            vh.AddTriangle(v, v + 2, v + 3);
        }
    }
}
