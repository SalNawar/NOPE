using System;
using System.Collections.Generic;

/// <summary>Where the speech bubble goes (BubbleLayout.Place): its box, its tail from the box's bottom edge toward the mouth, and whether it is clear of every obstacle.</summary>
public readonly struct BubblePlacement
{
    /// <summary>The bubble's box (y up).</summary>
    public readonly FaceRect Box;

    /// <summary>Where the tail leaves the box (the edge facing the mouth).</summary>
    public readonly float TailBaseX, TailBaseY;

    /// <summary>The tail's tip (TailLength from its base, toward the mouth).</summary>
    public readonly float TailTipX, TailTipY;

    /// <summary>True when the box and its tail lie on the screen and overlap no obstacle; false for the fallback (the ideal place, kept on the screen).</summary>
    public readonly bool Clear;

    /// <summary>A placement from its parts.</summary>
    public BubblePlacement(FaceRect box, float baseX, float baseY, float tipX, float tipY, bool clear)
    {
        Box = box;
        TailBaseX = baseX;
        TailBaseY = baseY;
        TailTipX = tipX;
        TailTipY = tipY;
        Clear = clear;
    }

    /// <summary>The tail's turn from pointing straight down, in degrees counter-clockwise (a tail pointing down and to the right turns positive).</summary>
    public float TailDegrees => (float)(Math.Atan2(TailTipX - TailBaseX, -(TailTipY - TailBaseY)) * 180.0 / Math.PI);
}

/// <summary>
/// The traveller's speech bubble belongs to the traveller (Saleh 2026-10-07:
/// it sat high over the hall's bridge and covered the wheel's top pill):
/// ideally beside and above the head, its bottom just above the head's top
/// and its near edge over the mouth, the tail pointing down at the mouth;
/// from there it moves the least it must (sideways first, then up, either
/// side, in steps of <see cref="Step"/>; down past the mouth only when the
/// screen has no room above, as in the desk view, which raises the head to
/// the top) until the box and its tail lie on the screen, the box clear of
/// the face, and both clear of every obstacle (the open wheel's pills) by the gap; the tail leaves the edge
/// that faces the mouth. Canvas units, y up; resolution independent
/// (the overlay scales from 1920 x 1080). Pure, so the rule is tested.
/// </summary>
public static class BubbleLayout
{
    /// <summary>The search's step (canvas units).</summary>
    public const float Step = 12f;

    /// <summary>The most a tail leaving the box's top or bottom turns from straight, in degrees: toward the mouth, never flatter.</summary>
    public const float MaxTailDegrees = 40f;

    /// <summary>
    /// Places a <paramref name="width"/> x <paramref name="height"/> bubble
    /// whose tail is <paramref name="tailLength"/> long for a head whose top is
    /// at (<paramref name="headX"/>, <paramref name="headTopY"/>) and whose
    /// mouth is at (<paramref name="mouthX"/>, <paramref name="mouthY"/>),
    /// inside <paramref name="screen"/>, at least <paramref name="gap"/> from
    /// every rectangle of <paramref name="obstacles"/> (null or empty: the
    /// wheel is closed).
    /// </summary>
    public static BubblePlacement Place(float headX, float headTopY, float mouthX, float mouthY, float width, float height, float tailLength,
                                        FaceRect screen, IReadOnlyList<FaceRect> obstacles, float gap)
    {
        float inset = Math.Min(width * 0.2f, 60f); // how far in from the box's corner the tail leaves it
        float idealBottom = Math.Max(headTopY, mouthY + tailLength * 0.5f) + tailLength * 0.25f;
        float lowestAbove = mouthY + tailLength * 0.5f; // under this, the bubble no longer sits above the mouth: it costs more
        int up = Math.Max(0, (int)Math.Ceiling((screen.YMax - height - idealBottom) / Step));
        int down = Math.Max(0, (int)Math.Floor((idealBottom - screen.YMin) / Step));
        int across = (int)Math.Ceiling(screen.Width / Step);

        // The face itself: the box never covers it (its tail may point at it).
        float headHeight = Math.Max(1f, headTopY - mouthY);
        var face = new FaceRect(headX - headHeight * 0.9f, mouthY - headHeight * 0.7f, headX + headHeight * 0.9f, headTopY);

        float bestCost = float.MaxValue;
        BubblePlacement best = default;
        bool found = false;
        for (int k = -down; k <= up; k++)
        {
            float bottom = idealBottom + k * Step;
            float verticalCost = Math.Abs(k) * Step * (bottom < lowestAbove ? 4f : 1.5f);
            if (verticalCost >= bestCost)
                continue;
            for (int side = 1; side >= -1; side -= 2)
            {
                float idealCentre = mouthX + side * (width / 2f - inset);
                for (int j = 0; j <= across; j++)
                {
                    float cost = verticalCost + j * Step + (side < 0 ? 1f : 0f); // the right first, at equal cost
                    if (cost >= bestCost)
                        break;
                    float centre = idealCentre + side * j * Step;
                    var box = new FaceRect(centre - width / 2f, bottom, centre + width / 2f, bottom + height);
                    if (box.XMin < screen.XMin || box.XMax > screen.XMax)
                        break; // further out stays off the screen
                    BubblePlacement candidate = WithTail(box, mouthX, mouthY, inset, tailLength, true);
                    if (box.YMax > screen.YMax || !OnScreen(candidate, tailLength, screen) || Overlap(box, face, gap) || Hits(candidate, tailLength, obstacles, gap))
                        continue;
                    bestCost = cost;
                    best = candidate;
                    found = true;
                }
            }
        }
        if (found)
            return best;

        // Nothing clears: the ideal place, kept on the screen.
        float x = mouthX + (width / 2f - inset) - width / 2f, y = idealBottom;
        x += RectClamp.Shift(x, x + width, screen.XMin, screen.XMax, false);
        y += RectClamp.Shift(y, y + height, screen.YMin, screen.YMax, true);
        return WithTail(new FaceRect(x, y, x + width, y + height), mouthX, mouthY, inset, tailLength, false);
    }

    /// <summary>The tail for <paramref name="box"/>: from the edge that faces the mouth (its bottom when the mouth is below it, its top when above, else its nearer side), at the mouth's height or across (kept <paramref name="inset"/> in from the corners), toward the mouth (at most MaxTailDegrees from straight off the top or the bottom).</summary>
    private static BubblePlacement WithTail(FaceRect box, float mouthX, float mouthY, float inset, float tailLength, bool clear)
    {
        float baseX, baseY;
        if (mouthY < box.YMin || mouthY > box.YMax)
        {
            baseX = Math.Min(Math.Max(mouthX, box.XMin + inset), box.XMax - inset);
            baseY = mouthY < box.YMin ? box.YMin : box.YMax;
        }
        else
        {
            float sideInset = Math.Min(inset, box.Height / 3f);
            baseX = mouthX < box.CentreX ? box.XMin : box.XMax;
            baseY = Math.Min(Math.Max(mouthY, box.YMin + sideInset), box.YMax - sideInset);
        }
        float dx = mouthX - baseX, dy = mouthY - baseY;
        // A tail leaving the top or the bottom turns at most MaxTailDegrees from straight (the kit's tail reads as a tail, not a sliver along the edge).
        if (dy != 0f && (mouthY < box.YMin || mouthY > box.YMax))
        {
            float most = Math.Abs(dy) * (float)Math.Tan(MaxTailDegrees * Math.PI / 180.0);
            dx = Math.Max(-most, Math.Min(most, dx));
        }
        float length = (float)Math.Sqrt(dx * dx + dy * dy);
        if (length < 1e-3f)
        {
            dx = 0f;
            dy = -1f;
            length = 1f;
        }
        return new BubblePlacement(box, baseX, baseY, baseX + dx / length * tailLength, baseY + dy / length * tailLength, clear);
    }

    /// <summary>The tail's box: from its base to its tip, as wide as it is long (the kit's tail is about square).</summary>
    public static FaceRect TailBox(BubblePlacement p, float tailLength)
    {
        float half = tailLength / 2f;
        return new FaceRect(Math.Min(p.TailBaseX, p.TailTipX) - half, Math.Min(p.TailBaseY, p.TailTipY), Math.Max(p.TailBaseX, p.TailTipX) + half,
                            Math.Max(p.TailBaseY, p.TailTipY));
    }

    /// <summary>True when the two rectangles come closer than <paramref name="gap"/>.</summary>
    public static bool Overlap(FaceRect a, FaceRect b, float gap) =>
        a.XMin < b.XMax + gap && b.XMin < a.XMax + gap && a.YMin < b.YMax + gap && b.YMin < a.YMax + gap;

    private static bool OnScreen(BubblePlacement p, float tailLength, FaceRect screen)
    {
        FaceRect tail = TailBox(p, tailLength);
        return p.Box.YMin >= screen.YMin && tail.YMin >= screen.YMin && tail.XMin >= screen.XMin && tail.XMax <= screen.XMax;
    }

    private static bool Hits(BubblePlacement p, float tailLength, IReadOnlyList<FaceRect> obstacles, float gap)
    {
        if (obstacles == null)
            return false;
        FaceRect tail = TailBox(p, tailLength);
        for (int i = 0; i < obstacles.Count; i++)
            if (Overlap(p.Box, obstacles[i], gap) || Overlap(tail, obstacles[i], gap))
                return true;
        return false;
    }
}
