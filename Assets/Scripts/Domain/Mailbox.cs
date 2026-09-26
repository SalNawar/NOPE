using System;
using System.Collections.Generic;

/// <summary>What a message is, which decides its sender, subject and link (the PC spec's ML1).</summary>
public enum MailKind
{
    /// <summary>The day's directives, as the Rules show them (a link to the Rules).</summary>
    DirectiveMemo,

    /// <summary>"The Temporal Times, day N" (a link to the News site).</summary>
    TimesIssue,

    /// <summary>The copy of a citation slip, delivered once the slip is acknowledged.</summary>
    CitationNotice,

    /// <summary>A message written in world_source.json "mail".</summary>
    Authored
}

/// <summary>Where a message's link leads (the view decides how it opens).</summary>
public enum MailLink
{
    /// <summary>No link.</summary>
    None,

    /// <summary>The day's directives (today the Directives window; the Investigation app's Rules tab from phase 16).</summary>
    Rules,

    /// <summary>The News site's issue of the message's day (today the Internet window; phase 24's browser).</summary>
    News
}

/// <summary>
/// One authored message (world_source.json "mail", written into the content
/// library by Generate World): it arrives on <see cref="fromDay"/>, stays in
/// the inbox until <see cref="untilDay"/> (0 = for the run), and only while
/// <see cref="flag"/> is set when it names one.
/// </summary>
[Serializable]
public sealed class AuthoredMail
{
    /// <summary>Unique id (the message's id is "mail:" + this).</summary>
    public string id = string.Empty;

    /// <summary>The day the message arrives (its date).</summary>
    public int fromDay = 1;

    /// <summary>The last day it is in the inbox; 0 keeps it for the run.</summary>
    public int untilDay;

    /// <summary>A story flag the message waits for (empty = none).</summary>
    public string flag = string.Empty;

    /// <summary>The sender, as printed in the FROM box.</summary>
    public string from = string.Empty;

    /// <summary>The subject line.</summary>
    public string subject = string.Empty;

    /// <summary>The body's paragraphs.</summary>
    public string[] body = Array.Empty<string>();
}

/// <summary>A citation slip the clerk acknowledged this shift: its day, the traveller's slot, and the slip's text.</summary>
public readonly struct CitationCopy
{
    /// <summary>The shift day.</summary>
    public readonly int Day;

    /// <summary>The traveller's 1-based slot.</summary>
    public readonly int Slot;

    /// <summary>The slip's text (CaseVerdict.citationText).</summary>
    public readonly string Text;

    /// <summary>A copy of the slip for traveller <paramref name="slot"/> on <paramref name="day"/>.</summary>
    public CitationCopy(int day, int slot, string text)
    {
        Day = day;
        Slot = slot;
        Text = text ?? string.Empty;
    }
}

/// <summary>
/// One message in the inbox: a view of data the game already has (ML1). The
/// wording of the generated kinds (sender, subject, empty bodies) is the
/// view's (UI strings by kind); <see cref="From"/> and <see cref="Subject"/>
/// are set only for authored mail.
/// </summary>
public sealed class MailItem
{
    /// <summary>Stable id, the key of its read flag ("memo:3", "times:3", "cite:3:2", "mail:welcome").</summary>
    public string Id;

    /// <summary>The day the message is dated.</summary>
    public int Day;

    /// <summary>What the message is.</summary>
    public MailKind Kind;

    /// <summary>The citation's traveller slot (citation notices only).</summary>
    public int Slot;

    /// <summary>An authored message's sender (empty for the generated kinds).</summary>
    public string From = string.Empty;

    /// <summary>An authored message's subject (empty for the generated kinds).</summary>
    public string Subject = string.Empty;

    /// <summary>The body's lines: the directives, the day's headlines, the slip's text or the authored paragraphs (may be empty).</summary>
    public IReadOnlyList<string> Body = Array.Empty<string>();

    /// <summary>Where the message links to.</summary>
    public MailLink Link;

    /// <summary>A Times issue only: true when its day's paper is on hand (today's, or an archived one), even with no headlines; false when it is only the link.</summary>
    public bool IssueOnHand;
}

/// <summary>
/// What the inbox is built from (ML1-ML2); the engine fills it from the day
/// plans, the morning paper, the shift's acknowledged citations and the
/// content library. Nothing here is saved: only the read flags are.
/// </summary>
public sealed class MailSources
{
    /// <summary>Today's shift day (messages are listed for days 1 to today).</summary>
    public int Today = 1;

    /// <summary>A day's directive lines (empty = no directives that day).</summary>
    public Func<int, IReadOnlyList<string>> Rules;

    /// <summary>A day's Times headlines, or null when that issue is not archived (its message is then only the link; phase 24's News archive fills past days).</summary>
    public Func<int, IReadOnlyList<string>> News;

    /// <summary>The citation slips acknowledged (the shift's ledger keeps today's).</summary>
    public IReadOnlyList<CitationCopy> Citations = Array.Empty<CitationCopy>();

    /// <summary>The authored messages.</summary>
    public IReadOnlyList<AuthoredMail> Authored = Array.Empty<AuthoredMail>();

    /// <summary>True when a story flag is set (an authored message may wait for one).</summary>
    public Func<string, bool> HasFlag;
}

/// <summary>
/// The Mail app's rules (the PC spec's ML1-ML2): the inbox is rebuilt from
/// its sources by id, newest day first; within a day the citation notices
/// (latest traveller first), then the directive memo, the Times issue and the
/// authored messages. Opening a message marks it read; the read flags are the
/// only saved state. Pure, so the inbox and its badge are tested headless.
/// </summary>
public static class Mailbox
{
    /// <summary>The id prefix of an authored message.</summary>
    public const string AuthoredPrefix = "mail:";

    /// <summary>The inbox for days 1 to <see cref="MailSources.Today"/>, newest first.</summary>
    public static List<MailItem> ForDays(MailSources s)
    {
        var items = new List<MailItem>();
        if (s == null)
            return items;

        for (int day = s.Today; day >= 1; day--)
        {
            var citations = new List<CitationCopy>();
            foreach (CitationCopy c in s.Citations ?? Array.Empty<CitationCopy>())
                if (c.Day == day)
                    citations.Add(c);
            citations.Sort((a, b) => b.Slot.CompareTo(a.Slot));
            foreach (CitationCopy c in citations)
                items.Add(new MailItem
                {
                    Id = $"cite:{day}:{c.Slot}", Day = day, Kind = MailKind.CitationNotice, Slot = c.Slot,
                    Body = string.IsNullOrEmpty(c.Text) ? Array.Empty<string>() : new[] { c.Text }
                });

            items.Add(new MailItem
            {
                Id = $"memo:{day}", Day = day, Kind = MailKind.DirectiveMemo, Link = MailLink.Rules,
                Body = s.Rules?.Invoke(day) ?? Array.Empty<string>()
            });
            IReadOnlyList<string> news = s.News?.Invoke(day);
            items.Add(new MailItem
            {
                Id = $"times:{day}", Day = day, Kind = MailKind.TimesIssue, Link = MailLink.News,
                Body = news ?? Array.Empty<string>(), IssueOnHand = news != null
            });

            foreach (AuthoredMail a in s.Authored ?? Array.Empty<AuthoredMail>())
                if (a != null && a.fromDay == day && Delivered(a, s))
                    items.Add(new MailItem
                    {
                        Id = AuthoredPrefix + a.id, Day = day, Kind = MailKind.Authored,
                        From = a.from ?? string.Empty, Subject = a.subject ?? string.Empty,
                        Body = a.body ?? Array.Empty<string>()
                    });
        }
        return items;
    }

    /// <summary>True while an authored message is in the inbox: arrived, not past its last day, its flag (if any) set.</summary>
    private static bool Delivered(AuthoredMail a, MailSources s) =>
        a.fromDay <= s.Today &&
        (a.untilDay <= 0 || s.Today <= a.untilDay) &&
        (string.IsNullOrEmpty(a.flag) || (s.HasFlag != null && s.HasFlag(a.flag)));

    /// <summary>How many of the messages are not read (the badge).</summary>
    public static int Unread(IReadOnlyList<MailItem> items, ICollection<string> read)
    {
        int n = 0;
        if (items == null)
            return n;
        foreach (MailItem m in items)
            if (m != null && (read == null || !read.Contains(m.Id)))
                n++;
        return n;
    }

    /// <summary>Marks a message read (no duplicates); true when it was unread.</summary>
    public static bool MarkRead(List<string> read, string id)
    {
        if (read == null || string.IsNullOrEmpty(id) || read.Contains(id))
            return false;
        read.Add(id);
        return true;
    }

    /// <summary>What Generate World and the validator refuse in the authored mail: a blank or repeated id, a first day below 1, a last day before it, a blank sender, subject or body. Empty when sound.</summary>
    public static List<string> AuthoredProblems(IReadOnlyList<AuthoredMail> mail)
    {
        var problems = new List<string>();
        var ids = new HashSet<string>();
        if (mail == null)
            return problems;

        for (int i = 0; i < mail.Count; i++)
        {
            AuthoredMail m = mail[i];
            if (m == null)
            {
                problems.Add($"mail[{i}] is empty.");
                continue;
            }
            string name = string.IsNullOrWhiteSpace(m.id) ? $"mail[{i}]" : $"mail '{m.id}'";
            if (string.IsNullOrWhiteSpace(m.id))
                problems.Add($"{name} has no id.");
            else if (!ids.Add(m.id))
                problems.Add($"{name} is listed twice.");
            if (m.fromDay < 1)
                problems.Add($"{name}: fromDay {m.fromDay} is before day 1.");
            if (m.untilDay != 0 && m.untilDay < m.fromDay)
                problems.Add($"{name}: untilDay {m.untilDay} is before fromDay {m.fromDay} (0 keeps it for the run).");
            if (string.IsNullOrWhiteSpace(m.from))
                problems.Add($"{name} has no sender (from).");
            if (string.IsNullOrWhiteSpace(m.subject))
                problems.Add($"{name} has no subject.");
            if (m.body == null || m.body.Length == 0 || Array.TrueForAll(m.body, string.IsNullOrWhiteSpace))
                problems.Add($"{name} has no body.");
        }
        return problems;
    }
}
