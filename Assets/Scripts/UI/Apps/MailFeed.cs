using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The inbox's sources in the office (the PC spec's ML1-ML2), on the desktop
/// canvas so the badge counts while the Mail window is closed: each day's
/// directive memo (the day plan's travel rules, worded as the Directives
/// window words them), each day's Times issue (the day's paper from the News
/// site's archive, WorldState.newsArchive; today's from the morning paper
/// until the briefing archives it; an issue the archive no longer keeps is
/// only its link),
/// a notice for each citation slip once it is acknowledged this shift, and
/// the authored mail. It rebuilds the list (Mailbox.ForDays) when the day
/// changes and when a slip is acknowledged; opening a message marks it read
/// in WorldState.mailRead, the only saved state. The unread count shows on
/// the Start menu's Mail entry and in the Mail window's title (the taskbar
/// button shows the title); phase 17's icon badge reads <see cref="Unread"/>.
/// </summary>
public sealed class MailFeed : MonoBehaviour
{
    /// <summary>The office's game manager (the acknowledged citation slips).</summary>
    [SerializeField] private GameManager game;

    /// <summary>The Start menu's Mail entry label ("Mail (2)").</summary>
    [SerializeField] private TMP_Text startEntryLabel;

    /// <summary>The Mail window's title ("Mail (2 unread)").</summary>
    [SerializeField] private TMP_Text windowTitle;

    private readonly List<CitationCopy> _citations = new List<CitationCopy>();
    private List<MailItem> _items = new List<MailItem>();
    private int _day = -1;

    /// <summary>Raised when the list or a read flag changes.</summary>
    public event Action Changed;

    /// <summary>The inbox, newest first.</summary>
    public IReadOnlyList<MailItem> Items => _items;

    /// <summary>How many messages are not read.</summary>
    public int Unread => Mailbox.Unread(_items, World?.mailRead);

    /// <summary>The run's state (null outside a run).</summary>
    private static WorldState World => RunManager.HasInstance ? RunManager.Instance.World : null;

    private void Start()
    {
        if (game != null)
            game.CitationAcknowledged += HandleCitation;
    }

    private void OnDestroy()
    {
        if (game != null)
            game.CitationAcknowledged -= HandleCitation;
    }

    /// <summary>Rebuilds the inbox when the run's day changes (the run starts after this component).</summary>
    private void Update()
    {
        WorldState world = World;
        if (world != null && world.day != _day)
            Refresh();
    }

    /// <summary>True when the message is read.</summary>
    public bool IsRead(string id) => World != null && World.mailRead.Contains(id);

    /// <summary>Marks a message read (opening it); saved with the run.</summary>
    public void MarkRead(string id)
    {
        WorldState world = World;
        if (world != null && Mailbox.MarkRead(world.mailRead, id))
            Announce();
    }

    /// <summary>A citation slip was acknowledged: its notice arrives.</summary>
    private void HandleCitation(CaseVerdict verdict)
    {
        WorldState world = World;
        if (world == null || verdict == null || !verdict.citationIssued)
            return;
        _citations.Add(new CitationCopy(world.day, verdict.caseIndex1Based, verdict.citationText));
        Refresh();
    }

    /// <summary>Rebuilds the inbox from its sources.</summary>
    private void Refresh()
    {
        WorldState world = World;
        if (world == null)
            return;

        if (world.day != _day)
        {
            _citations.RemoveAll(c => c.Day != world.day);
            _day = world.day;
        }

        ContentLibrarySO library = RunManager.Instance.Library;
        _items = Mailbox.ForDays(new MailSources
        {
            Today = world.day,
            Rules = day => RulesOf(library, day),
            News = day => Headlines(world, day),
            Citations = _citations,
            Authored = library != null ? library.Mail : Array.Empty<AuthoredMail>(),
            HasFlag = world.HasFlag
        });
        Announce();
    }

    /// <summary>A day's directive lines (its day plan's travel rules).</summary>
    private static IReadOnlyList<string> RulesOf(ContentLibrarySO library, int day)
    {
        var lines = new List<string>();
        DayPlanSO plan = library != null ? library.GetDayPlan(day) : null;
        if (plan != null)
            foreach (TravelRuleSO rule in plan.ActiveTravelRules)
                if (rule != null)
                    lines.Add(rule.Summary());
        return lines;
    }

    /// <summary>A day's paper, its notices then its news: the archived issue, else today's morning paper; null when neither is on hand.</summary>
    private static IReadOnlyList<string> Headlines(WorldState world, int day)
    {
        NewsIssue issue = NewsArchive.Find(world.newsArchive, day);
        if (issue == null && day != world.day)
            return null;
        var lines = new List<string>(issue != null ? issue.briefing : world.tomorrow.briefingLines);
        lines.AddRange(issue != null ? issue.news : world.tomorrow.newsLines);
        return lines;
    }

    /// <summary>Shows the unread count and tells the Mail window.</summary>
    private void Announce()
    {
        int unread = Unread;
        if (startEntryLabel != null)
            startEntryLabel.text = unread > 0 ? UiText.Format("startmenu.mailUnread", unread) : UiText.Get("startmenu.mail");
        if (windowTitle != null)
            windowTitle.text = unread > 0 ? UiText.Format("window.mailUnread", unread) : UiText.Get("window.mail");
        Changed?.Invoke();
    }
}
