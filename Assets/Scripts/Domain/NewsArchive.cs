using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>One day's morning paper as it was printed: its notices (briefing lines) and news lines (P spec IN3). Saved in WorldState.newsArchive.</summary>
[Serializable]
public sealed class NewsIssue
{
    /// <summary>The day it was printed for.</summary>
    public int day;

    /// <summary>The notices (the morning briefing's lines).</summary>
    public List<string> briefing = new List<string>();

    /// <summary>The news lines (dominance, history, trigger and effect news, in the paper's order).</summary>
    public List<string> news = new List<string>();
}

/// <summary>
/// The back issues (P spec IN3): each morning's paper is recorded when the
/// briefing shows, because the night rebuilds the lines and they cannot be
/// recomputed later. Issues stay in day order; a day recorded again
/// replaces its issue (the day was replayed), and past the cap the oldest go.
/// </summary>
public static class NewsArchive
{
    /// <summary>
    /// Records <paramref name="day"/>'s issue (copies of the lines; blank lines
    /// dropped): replaces that day's issue if there is one, keeps the archive
    /// in day order and drops the oldest issues past <paramref name="cap"/>
    /// (below 1 counts as 1). A null archive does nothing.
    /// </summary>
    public static void Record(List<NewsIssue> archive, int day, IReadOnlyList<string> briefing, IReadOnlyList<string> news, int cap)
    {
        if (archive == null)
            return;

        archive.RemoveAll(i => i == null || i.day == day);
        archive.Add(new NewsIssue { day = day, briefing = Lines(briefing), news = Lines(news) });
        archive.Sort((a, b) => a.day.CompareTo(b.day));

        int keep = Math.Max(1, cap);
        if (archive.Count > keep)
            archive.RemoveRange(0, archive.Count - keep);
    }

    /// <summary>The issue of <paramref name="day"/>, or null when none is on file.</summary>
    public static NewsIssue Find(IReadOnlyList<NewsIssue> archive, int day) =>
        archive?.FirstOrDefault(i => i != null && i.day == day);

    /// <summary>The non-blank lines, copied.</summary>
    private static List<string> Lines(IReadOnlyList<string> lines) =>
        (lines ?? Array.Empty<string>()).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
}
