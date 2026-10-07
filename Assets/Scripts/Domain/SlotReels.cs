/// <summary>
/// The slot panel's three reels (the UI kit, sheet 04): which symbol each reel
/// lands on after a spin, so the reels say what the result line says. A spin
/// that wins money lands the same symbol on every reel (which one follows the
/// outcome, so each win has its own look); any other spin lands three
/// different symbols. The same outcome always lands the same faces; the draw
/// itself stays the slot's own seeded stream (Seeds.ForSlot). Pure.
/// </summary>
public static class SlotReels
{
    /// <summary>The number of reels.</summary>
    public const int Count = 3;

    /// <summary>
    /// The symbol (an index into a set of <paramref name="symbols"/>) each reel
    /// lands on for the outcome at <paramref name="outcomeIndex"/> in the
    /// library's list: one symbol on every reel when <paramref name="win"/>,
    /// else three different ones (as different as the set allows); all 0 for
    /// an empty set.
    /// </summary>
    public static int[] Faces(int outcomeIndex, bool win, int symbols)
    {
        var faces = new int[Count];
        if (symbols <= 0)
            return faces;
        int first = ((outcomeIndex % symbols) + symbols) % symbols;
        for (int i = 0; i < Count; i++)
            faces[i] = win ? first : (first + i) % symbols;
        return faces;
    }
}
