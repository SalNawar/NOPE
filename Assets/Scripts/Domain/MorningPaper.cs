using System;
using System.Collections.Generic;

/// <summary>
/// The morning paper's front page from the day's data (run 7, the UI kit's
/// "The Temporal Times", docs/ui-kit/newspaper.png): one key story and one
/// titled small story, the rest of the page dummy type. The paper is the
/// world's (Saleh 2026-10-07: "the newspaper shouldn't mention my work stuff
/// like it's a new desk"): it reports the world and the timeline, never the
/// clerk's instructions, which come on the Bureau memo clipped to it
/// (BureauMemo). The first timeline headline leads and the morning's notes
/// are its deck; on a day with no news the first note leads, and on a day
/// with neither the quiet day's story (a line of the debt economy's own news,
/// DebtNews.Line) does. The small story carries every other line (the news,
/// the desk's own stories) under the news header, or the desk header when
/// only the desk's lines are left. Pure; tested.
/// </summary>
public sealed class MorningPaper
{
    /// <summary>The key story's headline; empty on a day with nothing to print (the caller prints its "no news" line).</summary>
    public string Headline { get; private set; } = string.Empty;

    /// <summary>The key story's deck (empty when the headline says it all).</summary>
    public string Deck { get; private set; } = string.Empty;

    /// <summary>The small story's title as a UI string key ("briefing.newsHeader" or "briefing.deskHeader"), or null with no small story.</summary>
    public string StoryTitleKey { get; private set; }

    /// <summary>The small story's lines, in order.</summary>
    public IReadOnlyList<string> StoryLines => _story;

    private readonly List<string> _story = new List<string>();

    /// <summary>The page for a day's tomorrow package lines (null lists count as empty), with <paramref name="quietDayStory"/> (empty or null: none) leading on a day with no news and no notes.</summary>
    public static MorningPaper Compose(IReadOnlyList<string> briefingLines, IReadOnlyList<string> newsLines, IReadOnlyList<string> deskLines, string quietDayStory)
    {
        var page = new MorningPaper();
        var notes = Clean(briefingLines);
        var news = Clean(newsLines);
        var desk = Clean(deskLines);

        if (news.Count > 0)
        {
            page.Headline = AsHeadline(news[0]);
            news.RemoveAt(0);
            page.Deck = string.Join(" ", notes);
            notes.Clear();
        }
        else if (notes.Count > 0)
        {
            page.Headline = AsHeadline(notes[0]);
            notes.RemoveAt(0);
        }
        else if (!string.IsNullOrWhiteSpace(quietDayStory))
            page.Headline = AsHeadline(quietDayStory.Trim());

        page._story.AddRange(news);
        page._story.AddRange(desk);
        page._story.AddRange(notes);
        if (page._story.Count > 0)
            page.StoryTitleKey = news.Count > 0 || desk.Count == 0 ? "briefing.newsHeader" : "briefing.deskHeader";
        return page;
    }

    /// <summary>The trimmed, non-empty lines of a list.</summary>
    private static List<string> Clean(IReadOnlyList<string> lines)
    {
        var list = new List<string>();
        if (lines == null)
            return list;
        foreach (string line in lines)
            if (!string.IsNullOrWhiteSpace(line))
                list.Add(line.Trim());
        return list;
    }

    /// <summary>A text as a headline: its closing full stop dropped (a headline keeps an exclamation or a question mark).</summary>
    private static string AsHeadline(string text) => text.EndsWith(".", StringComparison.Ordinal) ? text.Substring(0, text.Length - 1).TrimEnd() : text;

    /// <summary>A text's first sentence (its full stop dropped) and the rest; the whole text is the first part when it is one sentence (the Bureau memo's title and body).</summary>
    internal static void Split(string text, out string head, out string deck)
    {
        for (int i = 1; i < text.Length - 1; i++)
        {
            char c = text[i];
            if ((c == '.' || c == '!' || c == '?') && char.IsWhiteSpace(text[i + 1]))
            {
                head = AsHeadline(text.Substring(0, i + 1));
                deck = text.Substring(i + 1).Trim();
                return;
            }
        }
        head = AsHeadline(text);
        deck = string.Empty;
    }
}

/// <summary>
/// The Bureau memo clipped to the morning paper's corner (Saleh 2026-10-07:
/// the clerk's work news is the Bureau's, not the paper's): the day's
/// bulletin (DayPlanSO.Bulletin, with the new desk hours on the days they
/// change), its first sentence the memo's title and the rest its body. Empty
/// on a day without a bulletin (no memo). Pure; tested.
/// </summary>
public readonly struct BureauMemo
{
    /// <summary>The memo's title (the bulletin's first sentence, its full stop dropped).</summary>
    public readonly string Title;

    /// <summary>The memo's body (the rest of the bulletin; empty when it is one sentence).</summary>
    public readonly string Body;

    private BureauMemo(string title, string body)
    {
        Title = title;
        Body = body;
    }

    /// <summary>True when there is no memo today.</summary>
    public bool IsEmpty => string.IsNullOrEmpty(Title);

    /// <summary>The memo for <paramref name="bulletin"/> (empty or null: none).</summary>
    public static BureauMemo Of(string bulletin)
    {
        if (string.IsNullOrWhiteSpace(bulletin))
            return new BureauMemo(string.Empty, string.Empty);
        MorningPaper.Split(bulletin.Trim(), out string title, out string body);
        return new BureauMemo(title, body);
    }
}
