using System;

/// <summary>
/// Where a desk stamp's mark lands on a placed form (the travel documents
/// spec, TD4: the APPROVED and DENIED stamps, track B's to press): marks are
/// MarkHeight H tall at the mark's own aspect; the next mark of a paper goes
/// into its largest stamp area (a passport's visa page, a form's footer box),
/// the first in its middle and each later one stepped across and down it so a
/// second stamp never hides the first; a mark pressed at a point is centred
/// there and kept whole on the page. A form with no stamp area takes its marks
/// at the page's bottom right. Pure, so it is tested headless; the desk paper
/// prints the mark (DeskDocument.Stamp).
/// </summary>
public static class StampSpots
{
    /// <summary>A mark's height, in H (the print unit).</summary>
    public const float MarkHeight = 0.075f;

    /// <summary>The share of the page's width and height at its bottom right a form without a stamp area takes its marks in.</summary>
    public const float FallbackShare = 0.3f;

    /// <summary>How far each later mark steps from the one before, as a share of the mark's own size.</summary>
    public const float Step = 0.55f;

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
    /// <paramref name="aspect"/> lands on <paramref name="form"/>: the first in
    /// the middle of its stamp area, each later one Step of the mark across
    /// and down from the one before, wrapping inside the area.
    /// </summary>
    public static FaceRect Next(PlacedForm form, int index, float aspect)
    {
        FaceRect area = Area(form);
        (float w, float h) = MarkSize(form, area, aspect);
        float roomX = Math.Max(0f, area.Width - w), roomY = Math.Max(0f, area.Height - h);
        float x = area.XMin + roomX / 2f, y = area.YMin + roomY / 2f;
        for (int i = 0; i < Math.Max(0, index); i++)
        {
            x += w * Step;
            y += h * Step;
            if (x > area.XMin + roomX + 1e-6f)
                x = area.XMin + (x - area.XMin) % Math.Max(roomX, 1e-6f);
            if (y > area.YMin + roomY + 1e-6f)
                y = area.YMin + (y - area.YMin) % Math.Max(roomY, 1e-6f);
        }
        return FaceRect.FromTop(x, y, w, h);
    }

    /// <summary>A mark of <paramref name="aspect"/> pressed at (<paramref name="x"/>, <paramref name="y"/>) in form space: centred there, moved just enough to lie whole on the page.</summary>
    public static FaceRect At(PlacedForm form, float x, float y, float aspect)
    {
        if (form == null)
            return new FaceRect(0f, 0f, 0f, 0f);
        (float w, float h) = MarkSize(form, new FaceRect(0f, 0f, form.Width, form.PageHeight), aspect);
        float left = Math.Max(0f, Math.Min(form.Width - w, x - w / 2f));
        float top = Math.Max(0f, Math.Min(form.PageHeight - h, y - h / 2f));
        return FaceRect.FromTop(left, top, w, h);
    }
}
