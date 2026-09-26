using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>An icon's top-left in desktop units, x right and y down from the icon area's top-left corner.</summary>
public readonly struct IconPlace : IEquatable<IconPlace>
{
    /// <summary>The icon's app id (DesktopAppIds).</summary>
    public readonly string Id;

    /// <summary>The cell's left edge from the icon area's left.</summary>
    public readonly float X;

    /// <summary>The cell's top edge from the icon area's top.</summary>
    public readonly float Y;

    /// <summary>Creates a place.</summary>
    public IconPlace(string id, float x, float y)
    {
        Id = id;
        X = x;
        Y = y;
    }

    /// <summary>Same id and position.</summary>
    public bool Equals(IconPlace other) => Id == other.Id && X.Equals(other.X) && Y.Equals(other.Y);

    /// <summary>Same id and position.</summary>
    public override bool Equals(object obj) => obj is IconPlace other && Equals(other);

    /// <summary>A hash of the id and position.</summary>
    public override int GetHashCode() => ((Id != null ? Id.GetHashCode() : 0) * 397 ^ X.GetHashCode()) * 397 ^ Y.GetHashCode();

    /// <summary>"id (x, y)", for test messages.</summary>
    public override string ToString() => $"{Id} ({X.ToString(CultureInfo.InvariantCulture)}, {Y.ToString(CultureInfo.InvariantCulture)})";
}

/// <summary>The icon area and the arrange grid, in desktop units (from DesktopConfigSO and the icon layer's size).</summary>
public sealed class IconGrid
{
    /// <summary>The icon area's width (the desktop's).</summary>
    public readonly float AreaWidth;

    /// <summary>The icon area's height (the desktop above the compare dock and the taskbar).</summary>
    public readonly float AreaHeight;

    /// <summary>An icon cell's width.</summary>
    public readonly float CellWidth;

    /// <summary>An icon cell's height.</summary>
    public readonly float CellHeight;

    /// <summary>The first arrange spot's left edge.</summary>
    public readonly float OriginX;

    /// <summary>The first arrange spot's top edge.</summary>
    public readonly float OriginY;

    /// <summary>From one arrange column to the next.</summary>
    public readonly float ColumnStep;

    /// <summary>From one arrange row to the next.</summary>
    public readonly float RowStep;

    /// <summary>Creates a grid; the steps must be positive.</summary>
    public IconGrid(float areaWidth, float areaHeight, float cellWidth, float cellHeight, float originX, float originY, float columnStep, float rowStep)
    {
        if (columnStep <= 0f || rowStep <= 0f)
            throw new ArgumentException("An icon grid needs positive steps.");
        AreaWidth = areaWidth;
        AreaHeight = areaHeight;
        CellWidth = cellWidth;
        CellHeight = cellHeight;
        OriginX = originX;
        OriginY = originY;
        ColumnStep = columnStep;
        RowStep = rowStep;
    }

    /// <summary>The arrange spots in a column (at least one).</summary>
    public int Rows => Math.Max(1, (int)Math.Floor((AreaHeight - OriginY - CellHeight) / RowStep) + 1);

    /// <summary>The arrange columns in the area (at least one).</summary>
    public int Columns => Math.Max(1, (int)Math.Floor((AreaWidth - OriginX - CellWidth) / ColumnStep) + 1);

    /// <summary>The n-th arrange spot's top-left, column-first from the origin.</summary>
    public (float x, float y) Spot(int index) => (OriginX + index / Rows * ColumnStep, OriginY + index % Rows * RowStep);
}

/// <summary>
/// Where the desktop's icons sit (the PC redesign DK3-DK6, section 4.1):
/// Arrange lays them out column-first from the origin in the default order;
/// a drop is clamped into the icon area and, when it covers more than a
/// share of another icon's cell, moves to the nearest free arrange spot
/// (else it stays exactly where it was dropped); the player's layout is
/// saved as "id:x,y;..." in invariant numbers and restored with unknown ids
/// dropped, new ones in the first free spots and every place clamped; the
/// arrow keys move the selection to the nearest icon that way. A free spot
/// overlaps no other icon at all. Pure.
/// </summary>
public static class DesktopLayout
{
    /// <summary>The icons of <paramref name="order"/> in the arrange spots, in that order.</summary>
    public static IReadOnlyList<IconPlace> Arrange(IReadOnlyList<string> order, IconGrid grid)
    {
        var places = new List<IconPlace>(order.Count);
        for (int i = 0; i < order.Count; i++)
        {
            (float x, float y) = grid.Spot(i);
            places.Add(Clamp(new IconPlace(order[i], x, y), grid));
        }
        return places;
    }

    /// <summary>
    /// Where an icon dropped at (x, y) lands: clamped into the area; when it
    /// then covers more than <paramref name="overlapShare"/> of another icon's
    /// cell, the nearest free arrange spot (nearest to the drop), if any.
    /// </summary>
    public static IconPlace Drop(string id, float x, float y, IReadOnlyList<IconPlace> others, IconGrid grid, float overlapShare)
    {
        IconPlace dropped = Clamp(new IconPlace(id, x, y), grid);
        bool covers = false;
        foreach (IconPlace other in others)
            if (other.Id != id && OverlapShare(dropped.X, dropped.Y, other.X, other.Y, grid) > overlapShare)
                covers = true;
        if (!covers)
            return dropped;

        IconPlace? best = null;
        float bestDistance = float.MaxValue;
        int spots = grid.Rows * grid.Columns;
        for (int i = 0; i < spots; i++)
        {
            (float sx, float sy) = grid.Spot(i);
            if (!Free(id, sx, sy, others, grid))
                continue;
            float dx = sx - dropped.X, dy = sy - dropped.Y;
            float distance = dx * dx + dy * dy;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = new IconPlace(id, sx, sy);
            }
        }
        return best ?? dropped;
    }

    /// <summary>The share of the cell at (otherX, otherY) that a cell at (x, y) covers (0 to 1).</summary>
    public static float OverlapShare(float x, float y, float otherX, float otherY, IconGrid grid)
    {
        float w = Math.Max(0f, Math.Min(x, otherX) + grid.CellWidth - Math.Max(x, otherX));
        float h = Math.Max(0f, Math.Min(y, otherY) + grid.CellHeight - Math.Max(y, otherY));
        float cell = grid.CellWidth * grid.CellHeight;
        return cell > 0f ? w * h / cell : 0f;
    }

    /// <summary>
    /// The saved layout for the icons of <paramref name="order"/>: each saved
    /// place of a known id (the first one per id, clamped), and every id
    /// without one in the first free arrange spots; unknown ids and malformed
    /// entries are dropped. Nothing saved is the arrangement. In
    /// <paramref name="order"/>'s order.
    /// </summary>
    public static IReadOnlyList<IconPlace> Restore(string saved, IReadOnlyList<string> order, IconGrid grid)
    {
        if (string.IsNullOrEmpty(saved))
            return Arrange(order, grid);

        var known = new HashSet<string>(order, StringComparer.Ordinal);
        var placed = new Dictionary<string, IconPlace>(StringComparer.Ordinal);
        foreach (string entry in saved.Split(';'))
        {
            int colon = entry.IndexOf(':');
            if (colon <= 0)
                continue;
            string id = entry.Substring(0, colon);
            string[] xy = entry.Substring(colon + 1).Split(',');
            if (!known.Contains(id) || placed.ContainsKey(id) || xy.Length != 2 ||
                !float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
                !float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                continue;
            placed.Add(id, Clamp(new IconPlace(id, x, y), grid));
        }

        var taken = new List<IconPlace>(placed.Values);
        int spot = 0, spots = grid.Rows * grid.Columns;
        foreach (string id in order)
        {
            if (placed.ContainsKey(id))
                continue;
            IconPlace place = Clamp(new IconPlace(id, grid.OriginX, grid.OriginY), grid);
            for (; spot < spots; spot++)
            {
                (float sx, float sy) = grid.Spot(spot);
                if (Free(id, sx, sy, taken, grid))
                {
                    place = new IconPlace(id, sx, sy);
                    spot++;
                    break;
                }
            }
            placed.Add(id, place);
            taken.Add(place);
        }

        var result = new List<IconPlace>(order.Count);
        foreach (string id in order)
            result.Add(placed[id]);
        return result;
    }

    /// <summary>The layout as "id:x,y;..." (at most two decimals, invariant).</summary>
    public static string Save(IReadOnlyList<IconPlace> places)
    {
        var sb = new StringBuilder();
        foreach (IconPlace p in places)
        {
            if (sb.Length > 0)
                sb.Append(';');
            sb.Append(p.Id).Append(':')
              .Append(p.X.ToString("0.##", CultureInfo.InvariantCulture)).Append(',')
              .Append(p.Y.ToString("0.##", CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    /// <summary>
    /// The icon the arrow keys select from <paramref name="fromId"/> in the
    /// direction (dx, dy) (one of them -1 or 1, y down): the nearest one whose
    /// cell centre lies that way (within 45 degrees of it); null when there is
    /// none or <paramref name="fromId"/> is not placed.
    /// </summary>
    public static string Nearest(string fromId, int dx, int dy, IReadOnlyList<IconPlace> places)
    {
        IconPlace? from = null;
        foreach (IconPlace p in places)
            if (p.Id == fromId)
                from = p;
        if (from == null)
            return null;

        string best = null;
        float bestDistance = float.MaxValue;
        foreach (IconPlace p in places)
        {
            if (p.Id == fromId)
                continue;
            float ox = p.X - from.Value.X, oy = p.Y - from.Value.Y;
            float along = ox * dx + oy * dy;
            float across = Math.Abs(dx != 0 ? oy : ox);
            if (along <= 0f || across > along)
                continue;
            float distance = ox * ox + oy * oy;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = p.Id;
            }
        }
        return best;
    }

    /// <summary>The place moved inside the area (a cell larger than the area keeps its top-left at 0).</summary>
    public static IconPlace Clamp(IconPlace place, IconGrid grid) =>
        new IconPlace(place.Id,
                      Math.Max(0f, Math.Min(place.X, grid.AreaWidth - grid.CellWidth)),
                      Math.Max(0f, Math.Min(place.Y, grid.AreaHeight - grid.CellHeight)));

    /// <summary>True when a cell at (x, y) overlaps no icon but <paramref name="id"/> itself.</summary>
    private static bool Free(string id, float x, float y, IReadOnlyList<IconPlace> others, IconGrid grid)
    {
        foreach (IconPlace other in others)
            if (other.Id != id && OverlapShare(x, y, other.X, other.Y, grid) > 0f)
                return false;
        return true;
    }
}

/// <summary>What an icon's badge shows (the PC redesign DK2): a count, a dot, or nothing. Pure.</summary>
public static class IconBadge
{
    /// <summary>The count that shows a dot (something arrived, no number).</summary>
    public const int Dot = -1;

    /// <summary>The largest count written out; more reads "99+".</summary>
    public const int MaxShown = 99;

    /// <summary>The badge's text: null for no badge (a count of 0 or less, but the dot), "" for the dot, else the count.</summary>
    public static string Label(int count)
    {
        if (count == Dot)
            return string.Empty;
        if (count <= 0)
            return null;
        return count > MaxShown ? MaxShown.ToString(CultureInfo.InvariantCulture) + "+" : count.ToString(CultureInfo.InvariantCulture);
    }
}
