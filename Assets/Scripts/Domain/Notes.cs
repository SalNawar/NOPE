using System;
using System.Collections.Generic;

/// <summary>
/// A pasted clip on a Notes page (the PC spec's NT1): the text as copied,
/// where it came from, and whose case it belongs to. Phase 20's clipboard
/// fills the source fields; a plain paste carries only the text.
/// </summary>
[Serializable]
public sealed class Clipping
{
    /// <summary>The text as shown where it was copied.</summary>
    public string text = string.Empty;

    /// <summary>Where it came from, as the card prints it ("Visa · Visa Class").</summary>
    public string label = string.Empty;

    /// <summary>The source's entry key (phase 18's links follow it; empty for a plain paste).</summary>
    public string sourceKey = string.Empty;

    /// <summary>The traveller whose case it came from (the card's group; empty = not a case's).</summary>
    public string traveller = string.Empty;

    /// <summary>True for an untranslated line (its text is shown as copied).</summary>
    public bool foreign;
}

/// <summary>One day's Notes page: the typed notes and the clippings, saved in WorldState.notes (additive; an old save loads none).</summary>
[Serializable]
public sealed class NotePage
{
    /// <summary>The shift day.</summary>
    public int day;

    /// <summary>The typed notes (at most the notes limit).</summary>
    public string text = string.Empty;

    /// <summary>The pasted clippings, in the order pasted.</summary>
    public List<Clipping> clippings = new();
}

/// <summary>A traveller's clippings on a page (the cards grouped under a name).</summary>
public readonly struct ClippingGroup
{
    /// <summary>The traveller's name (empty for clippings from no case).</summary>
    public readonly string Traveller;

    /// <summary>The indexes of the group's clippings on the page, in paste order.</summary>
    public readonly IReadOnlyList<int> Indexes;

    /// <summary>A group of clippings.</summary>
    public ClippingGroup(string traveller, IReadOnlyList<int> indexes)
    {
        Traveller = traveller;
        Indexes = indexes;
    }
}

/// <summary>
/// The Notes app's rules (NT1, §4.7): one page per day, the oldest dropped
/// past the day cap; clippings (at most a cap per page, a blank one refused)
/// grouped under each traveller's name in the order they first appear, the
/// clips from no case last; the typed notes cut at the character limit. Pure.
/// </summary>
public static class Notes
{
    /// <summary>The page for <paramref name="day"/>, created in day order when missing; the oldest pages past <paramref name="maxDays"/> are dropped (never the requested one). Null for a day below 1.</summary>
    public static NotePage Page(List<NotePage> pages, int day, int maxDays)
    {
        if (pages == null || day < 1)
            return null;

        pages.RemoveAll(p => p == null);
        NotePage page = pages.Find(p => p.day == day);
        if (page == null)
        {
            page = new NotePage { day = day };
            int at = pages.FindIndex(p => p.day > day);
            if (at < 0)
                pages.Add(page);
            else
                pages.Insert(at, page);
        }
        while (maxDays > 0 && pages.Count > maxDays && pages[0] != page)
            pages.RemoveAt(0);
        return page;
    }

    /// <summary>Adds a clipping to the page; false when the page is full or the clip has no text.</summary>
    public static bool Clip(NotePage page, Clipping clip, int maxClippings)
    {
        if (page == null || clip == null || string.IsNullOrWhiteSpace(clip.text))
            return false;
        page.clippings ??= new List<Clipping>();
        if (maxClippings > 0 && page.clippings.Count >= maxClippings)
            return false;
        page.clippings.Add(clip);
        return true;
    }

    /// <summary>Removes the page's clipping at <paramref name="index"/>; false when there is none.</summary>
    public static bool Unclip(NotePage page, int index)
    {
        if (page?.clippings == null || index < 0 || index >= page.clippings.Count)
            return false;
        page.clippings.RemoveAt(index);
        return true;
    }

    /// <summary>The text cut to <paramref name="maxChars"/> characters (never null).</summary>
    public static string Trim(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        return maxChars >= 0 && text.Length > maxChars ? text.Substring(0, maxChars) : text;
    }

    /// <summary>The page's clippings grouped by traveller, in the order each name first appears; clippings from no case come last.</summary>
    public static List<ClippingGroup> Groups(NotePage page)
    {
        var groups = new List<ClippingGroup>();
        if (page?.clippings == null)
            return groups;

        var order = new List<string>();
        var byName = new Dictionary<string, List<int>>();
        for (int i = 0; i < page.clippings.Count; i++)
        {
            Clipping c = page.clippings[i];
            if (c == null)
                continue;
            string name = c.traveller ?? string.Empty;
            if (!byName.TryGetValue(name, out List<int> list))
            {
                list = new List<int>();
                byName.Add(name, list);
                if (name.Length > 0)
                    order.Add(name);
            }
            list.Add(i);
        }
        foreach (string name in order)
            groups.Add(new ClippingGroup(name, byName[name]));
        if (byName.TryGetValue(string.Empty, out List<int> loose))
            groups.Add(new ClippingGroup(string.Empty, loose));
        return groups;
    }

    /// <summary>True when the page has no typed notes and no clippings (the view then shows the hint).</summary>
    public static bool IsEmpty(NotePage page) =>
        page == null || (string.IsNullOrEmpty(page.text) && (page.clippings == null || page.clippings.Count == 0));
}
