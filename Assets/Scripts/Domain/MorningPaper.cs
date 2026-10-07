using System;
using System.Collections.Generic;

/// <summary>
/// The morning paper's front page from the day's data (run 7, the UI kit's
/// "The Temporal Times", docs/ui-kit/newspaper.png): one key story and one
/// titled small story, the rest of the page dummy type. The key story is the
/// day's bulletin (Papers, Please lesson 4: the one new paper or check comes
/// first): its first sentence the headline, the rest its deck. On a day with
/// no bulletin the first timeline headline leads and the morning's notes are
/// its deck. The small story carries every other line (the news, the desk's
/// own stories, the notes when a bulletin leads) under the news header, or
/// the desk header when only the desk's lines are left. Pure; tested.
/// </summary>
public sealed class MorningPaper
{
    /// <summary>The key story's headline; empty on a day with nothing to print (the caller prints its "no directives" line).</summary>
    public string Headline { get; private set; } = string.Empty;

    /// <summary>The key story's deck (empty when the headline says it all).</summary>
    public string Deck { get; private set; } = string.Empty;

    /// <summary>True when the key story is the day's bulletin (the kicker "BULLETIN · NEW TODAY" prints over it).</summary>
    public bool LeadIsBulletin { get; private set; }

    /// <summary>The small story's title as a UI string key ("briefing.newsHeader" or "briefing.deskHeader"), or null with no small story.</summary>
    public string StoryTitleKey { get; private set; }

    /// <summary>The small story's lines, in order.</summary>
    public IReadOnlyList<string> StoryLines => _story;

    private readonly List<string> _story = new List<string>();

    /// <summary>The page for a day's <paramref name="bulletin"/> (empty or null: none) and its tomorrow package's lines (null lists count as empty).</summary>
    public static MorningPaper Compose(string bulletin, IReadOnlyList<string> briefingLines, IReadOnlyList<string> newsLines, IReadOnlyList<string> deskLines)
    {
        var page = new MorningPaper();
        var notes = Clean(briefingLines);
        var news = Clean(newsLines);
        var desk = Clean(deskLines);

        if (!string.IsNullOrWhiteSpace(bulletin))
        {
            page.LeadIsBulletin = true;
            Split(bulletin.Trim(), out string head, out string deck);
            page.Headline = head;
            page.Deck = deck;
        }
        else if (news.Count > 0)
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

    /// <summary>A bulletin's first sentence (the headline, its full stop dropped) and the rest (the deck); the whole bulletin is the headline when it is one sentence.</summary>
    private static void Split(string text, out string head, out string deck)
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
