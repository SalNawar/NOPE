using System;
using System.Collections.Generic;

/// <summary>
/// The knobs of a desk paper's face (piece 10 X16), held by DeskConfigSO.face:
/// fractions of the paper's width W (sideMargin, photoGap) or height H (the
/// rest), measured from its top-left. Build Office UI checks that every
/// document template's rows fit (Capacity); re-run it after changing these.
/// </summary>
[Serializable]
public sealed class PaperFaceTuning
{
    /// <summary>The margin left and right of the title and the rows, in W.</summary>
    public float sideMargin = 0.07f;

    /// <summary>The title band's top, in H.</summary>
    public float titleTop = 0.03f;

    /// <summary>The title band's height, in H.</summary>
    public float titleHeight = 0.08f;

    /// <summary>The first row's top, in H.</summary>
    public float rowsTop = 0.13f;

    /// <summary>From one row's top to the next's, in H.</summary>
    public float rowPitch = 0.125f;

    /// <summary>The label's share of a row's pitch (the label sits over the value).</summary>
    public float labelShare = 0.36f;

    /// <summary>The value's share of a row's pitch (the rest of the pitch is the gap to the next row).</summary>
    public float valueShare = 0.52f;

    /// <summary>The margin under the last row, in H.</summary>
    public float bottomMargin = 0.04f;

    /// <summary>The photo's top, in H (at the top right, inside the side margin).</summary>
    public float photoTop = 0.13f;

    /// <summary>The photo's height, in H (its width follows LookCanvas.PhotoAspect).</summary>
    public float photoHeight = 0.25f;

    /// <summary>The gap between the rows beside the photo and the photo, in W.</summary>
    public float photoGap = 0.03f;
}

/// <summary>A rectangle on the paper's face, in the sheet's local space (the paper's height is 1; centre origin, y up).</summary>
public readonly struct FaceRect
{
    /// <summary>A rectangle from its edges.</summary>
    public FaceRect(float xMin, float yMin, float xMax, float yMax)
    {
        XMin = xMin;
        YMin = yMin;
        XMax = xMax;
        YMax = yMax;
    }

    /// <summary>The left edge.</summary>
    public float XMin { get; }

    /// <summary>The bottom edge.</summary>
    public float YMin { get; }

    /// <summary>The right edge.</summary>
    public float XMax { get; }

    /// <summary>The top edge.</summary>
    public float YMax { get; }

    /// <summary>The width.</summary>
    public float Width => XMax - XMin;

    /// <summary>The height.</summary>
    public float Height => YMax - YMin;

    /// <summary>The centre's x.</summary>
    public float CentreX => (XMin + XMax) / 2f;

    /// <summary>The centre's y.</summary>
    public float CentreY => (YMin + YMax) / 2f;

    /// <summary>True when the point is inside (edges included).</summary>
    public bool Contains(float x, float y) => x >= XMin && x <= XMax && y >= YMin && y <= YMax;
}

/// <summary>One row of a paper's face: its label over its value, and the rectangle a click or a hover picks it in (the label and the value, not the gap under them).</summary>
public readonly struct FaceRow
{
    /// <summary>A row from its rectangles.</summary>
    public FaceRow(FaceRect label, FaceRect value, FaceRect hit)
    {
        Label = label;
        Value = value;
        Hit = hit;
    }

    /// <summary>The label's box.</summary>
    public FaceRect Label { get; }

    /// <summary>The value's box.</summary>
    public FaceRect Value { get; }

    /// <summary>The row's hit area (the label's top to the value's bottom).</summary>
    public FaceRect Hit { get; }
}

/// <summary>A paper's laid-out face (PaperFace.Layout).</summary>
public sealed class FaceLayout
{
    /// <summary>A layout from its parts.</summary>
    public FaceLayout(FaceRect title, bool hasPhoto, FaceRect photo, IReadOnlyList<FaceRow> rows)
    {
        Title = title;
        HasPhoto = hasPhoto;
        Photo = photo;
        Rows = rows;
    }

    /// <summary>The title band.</summary>
    public FaceRect Title { get; }

    /// <summary>True on a photo document.</summary>
    public bool HasPhoto { get; }

    /// <summary>The photo's box (meaningful when HasPhoto).</summary>
    public FaceRect Photo { get; }

    /// <summary>The rows, top first (at most Capacity).</summary>
    public IReadOnlyList<FaceRow> Rows { get; }
}

/// <summary>
/// A desk paper's face (piece 10 X16), the same page as the scanned copy on
/// the PC: a title band; rows of a label over a value, a pitch apart; on a
/// photo document the photo at the top right, the rows beside it ending left
/// of it (the scanned page's photo-inset rule). Laid out in the sheet's local
/// space with the paper's height as 1 (the caller scales by the paper's
/// height). RowAt is the paper's hit test. Engine-free and tested;
/// DeskDocument places its texts and hit boxes from it.
/// </summary>
public static class PaperFace
{
    /// <summary>Two rectangles closer than this count as touching, not overlapping.</summary>
    private const float Touch = 1e-5f;

    /// <summary>
    /// The face of a paper of <paramref name="paperAspect"/> (width / height)
    /// with <paramref name="rows"/> rows (at most Capacity are laid out; a
    /// negative count gives none), with the photo when <paramref name="photo"/>.
    /// </summary>
    public static FaceLayout Layout(int rows, bool photo, float paperAspect, PaperFaceTuning t)
    {
        float w = paperAspect;
        float left = -w / 2f + t.sideMargin * w;
        float right = w / 2f - t.sideMargin * w;

        FaceRect title = FromTop(left, right, t.titleTop, t.titleHeight);
        float photoWidth = t.photoHeight * LookCanvas.PhotoAspect;
        FaceRect photoRect = FromTop(right - photoWidth, right, t.photoTop, t.photoHeight);
        float besidePhoto = photoRect.XMin - t.photoGap * w;

        int count = Math.Max(0, Math.Min(rows, Capacity(photo, t)));
        var laid = new List<FaceRow>(count);
        for (int i = 0; i < count; i++)
        {
            float top = t.rowsTop + i * t.rowPitch;
            float used = (t.labelShare + t.valueShare) * t.rowPitch;
            bool narrowed = photo && top < t.photoTop + t.photoHeight - Touch && top + used > t.photoTop + Touch;
            float rowRight = narrowed ? besidePhoto : right;
            FaceRect label = FromTop(left, rowRight, top, t.labelShare * t.rowPitch);
            FaceRect value = FromTop(left, rowRight, top + t.labelShare * t.rowPitch, t.valueShare * t.rowPitch);
            laid.Add(new FaceRow(label, value, FromTop(left, rowRight, top, used)));
        }

        return new FaceLayout(title, photo, photoRect, laid);
    }

    /// <summary>The row whose hit area holds the point, or -1 (the title, the photo, a margin, a gap, off the paper, or no layout).</summary>
    public static int RowAt(FaceLayout layout, float x, float y)
    {
        if (layout == null)
            return -1;
        for (int i = 0; i < layout.Rows.Count; i++)
            if (layout.Rows[i].Hit.Contains(x, y))
                return i;
        return -1;
    }

    /// <summary>How many rows fit above the bottom margin (the photo only narrows rows, so it does not change the count).</summary>
    public static int Capacity(bool photo, PaperFaceTuning t)
    {
        if (!(t.rowPitch > 0f))
            return 0;
        float used = (t.labelShare + t.valueShare) * t.rowPitch;
        float room = 1f - t.bottomMargin - t.rowsTop - used;
        return room < -Touch ? 0 : (int)Math.Floor((room + Touch) / t.rowPitch) + 1;
    }

    /// <summary>A rectangle between two x edges, <paramref name="top"/> down from the paper's top and <paramref name="height"/> tall (the paper's height is 1).</summary>
    private static FaceRect FromTop(float xMin, float xMax, float top, float height) =>
        new FaceRect(xMin, 0.5f - top - height, xMax, 0.5f - top);
}
