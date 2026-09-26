using System;

/// <summary>
/// Showing a list a page at a time: the paged windows (reference books, the
/// transcript) and the Home shop share this one rule. A per-page count below
/// 1 counts as 1; an empty list has one (empty) page.
/// </summary>
public static class Paging
{
    /// <summary>Pages needed for <paramref name="count"/> items (at least 1).</summary>
    public static int PageCount(int count, int perPage)
    {
        int per = Math.Max(1, perPage);
        return count <= 0 ? 1 : (count + per - 1) / per;
    }

    /// <summary>The page clamped to 0 .. PageCount - 1.</summary>
    public static int Clamp(int page, int count, int perPage) =>
        Math.Min(Math.Max(0, page), PageCount(count, perPage) - 1);

    /// <summary>The index of the first item on the (clamped) page.</summary>
    public static int First(int page, int count, int perPage) =>
        Clamp(page, count, perPage) * Math.Max(1, perPage);

    /// <summary>The page item <paramref name="index"/> is on (a negative index: the first page): a link turns there.</summary>
    public static int PageOf(int index, int perPage) => Math.Max(0, index) / Math.Max(1, perPage);

    /// <summary>One past the index of the last item on the (clamped) page.</summary>
    public static int End(int page, int count, int perPage) =>
        Math.Min(Math.Max(0, count), First(page, count, perPage) + Math.Max(1, perPage));
}
