using System;

/// <summary>
/// How tall a paper's printed capitals are on the screen (BUG-9's legibility
/// floor, DOC-LOOK): from the text's size (an em, in the placed form's units),
/// the page's height in those units and in metres on the desk, the font's
/// cap height per em, and the reading view's pixels per metre at 1080 lines.
/// The cap-height gate measures every form, role and culture with it at the
/// modelled reading pose (DocLookChecks, static) and on the real camera (the
/// rig's capHeight op). Pure.
/// </summary>
public static class CapHeight
{
    /// <summary>The data floor at A1 (1920x1080) and A2 (1280x720), in pixels of cap height (FIX_PLAN DOC-LOOK: 12 and 9 are the targets where the layout allows).</summary>
    public const float DataFloorA1 = 10f, DataFloorA2 = 7f;

    /// <summary>The label floor at A2 (pixels of cap height).</summary>
    public const float LabelFloorA2 = 6f;

    /// <summary>The screen heights of A1 and A2 (lines).</summary>
    public const int A1Lines = 1080, A2Lines = 720;

    /// <summary>The value face's cap height per em (Geist Mono Bold: cap line 40 of a 56-unit em).</summary>
    public const float ValueCapPerEm = 40f / 56f;

    /// <summary>The label face's cap height per em (Big Shoulders Bold: cap line 45 of a 56-unit em).</summary>
    public const float LabelCapPerEm = 45f / 56f;

    /// <summary>
    /// The pixels a text's capitals stand on a screen <paramref name="lines"/>
    /// tall: its <paramref name="size"/> (an em) on a page
    /// <paramref name="pageHeight"/> tall in the same units and
    /// <paramref name="pageMetres"/> tall on the desk, its face's
    /// <paramref name="capPerEm"/>, seen at <paramref name="pixelsPerMetre"/>
    /// at 1080 lines (the reading view's scale at the paper).
    /// </summary>
    public static float Pixels(float size, float pageHeight, float pageMetres, float capPerEm, float pixelsPerMetre, int lines)
    {
        if (pageHeight <= 0f || lines <= 0)
            return 0f;
        return size / pageHeight * pageMetres * capPerEm * pixelsPerMetre * lines / A1Lines;
    }

    /// <summary>The smallest size of <paramref name="form"/>'s data (a value or a hand in a field's slot; <paramref name="labels"/>: its labels instead), in the form's units; 0 when it prints none.</summary>
    public static float Smallest(PlacedForm form, bool labels)
    {
        float least = 0f;
        if (form == null)
            return least;
        foreach (FormItem item in form.Items)
        {
            if (item.Kind != FormItemKind.Text || item.Size <= 0f)
                continue;
            bool counts = labels ? item.Role == FormTextRole.Label : item.Slot >= 0 && (item.Role == FormTextRole.Value || item.Role == FormTextRole.Hand);
            if (counts)
                least = least <= 0f ? item.Size : Math.Min(least, item.Size);
        }
        return least;
    }

    /// <summary>True when data at <paramref name="a1"/> and <paramref name="a2"/> pixels of cap height meets both floors.</summary>
    public static bool DataReads(float a1, float a2) => a1 + 1e-3f >= DataFloorA1 && a2 + 1e-3f >= DataFloorA2;

    /// <summary>How many times larger data at <paramref name="a1"/> and <paramref name="a2"/> pixels of cap height must print to meet both floors (1 when it reads; the cap-height gate's report, DocLookChecks).</summary>
    public static float Needed(float a1, float a2)
    {
        if (a1 <= 0f || a2 <= 0f)
            return float.PositiveInfinity;
        return Math.Max(1f, Math.Max(DataFloorA1 / a1, DataFloorA2 / a2));
    }
}
