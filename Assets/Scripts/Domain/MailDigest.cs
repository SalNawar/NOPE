using System.Collections.Generic;

/// <summary>
/// The inbox without its automatic copies (BUG-50a, UI-PC; UI-24: "day 5
/// holds 10-11 unread, all automatic copies of the paper and directives. The
/// paper is shown twice"): the newspaper is never mailed (it is on the desk
/// each morning, and the Internet carries the news, D19), and of the
/// directive memos only today's stays (the Rules tab has the day's rules;
/// yesterday's memo is stale). Every other message (citation notices,
/// deliveries, failure reports, authored mail) is kept, in its order. Pure;
/// MailFeed filters Mailbox.ForDays with it.
/// </summary>
public static class MailDigest
{
    /// <summary>The messages of <paramref name="items"/> worth a place in the inbox on day <paramref name="today"/>, in their order.</summary>
    public static List<MailItem> Of(IReadOnlyList<MailItem> items, int today)
    {
        var kept = new List<MailItem>();
        if (items == null)
            return kept;
        foreach (MailItem item in items)
            if (item != null && Keeps(item, today))
                kept.Add(item);
        return kept;
    }

    /// <summary>True when <paramref name="item"/> stays in the inbox on day <paramref name="today"/>.</summary>
    private static bool Keeps(MailItem item, int today)
    {
        switch (item.Kind)
        {
            case MailKind.TimesIssue:
                return false;
            case MailKind.DirectiveMemo:
                return item.Day == today && item.Body != null && item.Body.Count > 0;
            default:
                return true;
        }
    }
}
