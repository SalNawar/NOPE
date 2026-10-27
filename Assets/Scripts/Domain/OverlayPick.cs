using System.Collections.Generic;

/// <summary>
/// The case board's overlay choice kept by what was chosen, not where it sat
/// (CODE-157, BUG-58a: "after a new scan, the case board's overlay can show a
/// different pair than the one the player chose"): a new scan adds its paper
/// among the sources and shifts the record, the face and the Seal Register
/// down, so a chosen source is kept as its title and which of that title it
/// is (two ID cards stay first and second), and found again by them in the
/// new list. Pure; CaseBoardView keeps its pair with it.
/// </summary>
public static class OverlayPick
{
    /// <summary>The key of the source at <paramref name="index"/>: its title and how many sources before it share that title; (null, 0) for none.</summary>
    public static (string title, int nth) Key(IReadOnlyList<OverlaySource> sources, int index)
    {
        if (sources == null || index < 0 || index >= sources.Count || sources[index] == null)
            return (null, 0);
        string title = sources[index].Title;
        int nth = 0;
        for (int i = 0; i < index; i++)
            if (sources[i] != null && sources[i].Title == title)
                nth++;
        return (title, nth);
    }

    /// <summary>The index in <paramref name="sources"/> of the source with <paramref name="key"/>, or -1 when it is gone (or the key is none).</summary>
    public static int Find(IReadOnlyList<OverlaySource> sources, (string title, int nth) key)
    {
        if (sources == null || key.title == null)
            return -1;
        int seen = 0;
        for (int i = 0; i < sources.Count; i++)
            if (sources[i] != null && sources[i].Title == key.title && seen++ == key.nth)
                return i;
        return -1;
    }
}
