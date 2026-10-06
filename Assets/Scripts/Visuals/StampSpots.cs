using System;

/// <summary>
/// Where a desk stamp's mark lands on a placed form (the travel documents
/// spec, TD4: the APPROVED and DENIED stamps, track B's to press): marks are
/// MarkHeight H tall at the mark's own aspect; the next mark of a paper goes
/// into its largest stamp area (a passport's visa page, a form's footer box),
/// in rows from its top left, each mark beside the last with a gap, so a second
/// stamp never hides the first (the rows start over when the area is full); a
/// mark pressed on the paper (a desk stamp let go over the passport: Papers,
/// Please's stamps, Saleh 2026-10-06, "the stamp mark must land exactly where
/// the stamp is pressed") is centred on the pressed point, wherever it is on
/// the page (over the boxes too), and only moved to stay whole on the page
/// (AtPoint); the ENTRY VISA box is a guide, no longer where a pressed mark is
/// put. A form with no stamp area takes its marks at the page's bottom right.
/// Pure, so it is tested headless; the desk paper prints the mark
/// (DeskDocument.Stamp).
/// </summary>
public static class StampSpots
{
    /// <summary>A mark's height, in H (the print unit).</summary>
    public const float MarkHeight = 0.075f;

    /// <summary>The share of the page's width and height at its bottom right a form without a stamp area takes its marks in.</summary>
    public const float FallbackShare = 0.3f;

    /// <summary>The gap between two marks and round them inside the area, as a share of the mark's height.</summary>
    public const float Gap = 0.3f;

    /// <summary>The largest stamp area of <paramref name="form"/> (by area; the first of equals), else the page's bottom right corner region; an empty rectangle without a form.</summary>
    public static FaceRect Area(PlacedForm form)
    {
        if (form == null)
            return new FaceRect(0f, 0f, 0f, 0f);
        FaceRect best = new FaceRect(form.Width * (1f - FallbackShare), form.PageHeight * (1f - FallbackShare), form.Width, form.PageHeight);
        float most = -1f;
        foreach (FormItem item in form.Items)
            if (item.Kind == FormItemKind.StampArea && item.Rect.Width * item.Rect.Height > most)
            {
                most = item.Rect.Width * item.Rect.Height;
                best = item.Rect;
            }
        return best;
    }

    /// <summary>A mark's size on <paramref name="form"/> at <paramref name="aspect"/> (its width over its height): MarkHeight H tall, shrunk to fit <paramref name="area"/>.</summary>
    public static (float width, float height) MarkSize(PlacedForm form, FaceRect area, float aspect)
    {
        aspect = aspect > 0f ? aspect : 1f;
        float h = MarkHeight * (form != null ? form.Unit : 0f);
        float w = h * aspect;
        float fit = Math.Min(1f, Math.Min(area.Width > 0f ? area.Width / Math.Max(w, 1e-6f) : 1f, area.Height > 0f ? area.Height / Math.Max(h, 1e-6f) : 1f));
        return (w * fit, h * fit);
    }

    /// <summary>
    /// Where mark number <paramref name="index"/> (0 the first) of
    /// <paramref name="aspect"/> lands on <paramref name="form"/>: in its stamp
    /// area's rows from the top left, a Gap round each mark (as many to a row
    /// and as many rows as fit, at least one; the rows start over when the
    /// area is full).
    /// </summary>
    public static FaceRect Next(PlacedForm form, int index, float aspect)
    {
        FaceRect area = Area(form);
        (float w, float h) = MarkSize(form, area, aspect);
        float gap = h * Gap;
        int across = Math.Max(1, (int)((area.Width - gap) / (w + gap)));
        int down = Math.Max(1, (int)((area.Height - gap) / (h + gap)));
        int k = Math.Max(0, index) % (across * down);
        float x = Math.Min(area.XMin + gap + (k % across) * (w + gap), area.XMax - w);
        float y = Math.Min(area.YMin + gap + (k / across) * (h + gap), area.YMax - h);
        return FaceRect.FromTop(Math.Max(area.XMin, x), Math.Max(area.YMin, y), w, h);
    }

    /// <summary>
    /// A mark of <paramref name="aspect"/> pressed at (<paramref name="x"/>,
    /// <paramref name="y"/>) in form space: the size the form's marks have
    /// (MarkSize in its largest stamp area), centred on the pressed point,
    /// moved only as far as it takes to lie whole on the page (a press by the
    /// paper's edge); anywhere else it may cover the boxes, as a stamp does.
    /// </summary>
    public static FaceRect AtPoint(PlacedForm form, float x, float y, float aspect)
    {
        if (form == null)
            return new FaceRect(0f, 0f, 0f, 0f);
        (float w, float h) = MarkSize(form, Area(form), aspect);
        float left = Math.Max(0f, Math.Min(form.Width - w, x - w / 2f));
        float top = Math.Max(0f, Math.Min(form.PageHeight - h, y - h / 2f));
        return FaceRect.FromTop(left, top, w, h);
    }
}
