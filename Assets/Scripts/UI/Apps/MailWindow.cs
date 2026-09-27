using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Mail app (the PC spec's ML1, §2.12): the inbox on the left (unread in
/// bold with a bullet; the day and the sender under the subject) and the open
/// message on the right, drawn as an internal memorandum: its page kind,
/// Form_Memo (TC-950: TO, DATE, FROM and REF boxes, the subject, the body,
/// the sender's sign-off and the stamp area), on a FormView (phase 5), with
/// the message's link over it. Opening a message marks it read (MailFeed).
/// </summary>
public sealed class MailWindow : MonoBehaviour
{
    /// <summary>The inbox's sources and read flags.</summary>
    [SerializeField] private MailFeed feed;

    /// <summary>The desktop's apps (the News link opens "internet").</summary>
    [SerializeField] private DesktopApps apps;

    /// <summary>The Internet app's browser (the News link goes to the message's issue on the News site).</summary>
    [SerializeField] private BrowserWindow browser;

    /// <summary>The Investigation app: the Rules link opens it on its Rules tab.</summary>
    [SerializeField] private InvestigationApp investigation;

    /// <summary>The inbox list's content (rows are cloned into it).</summary>
    [SerializeField] private RectTransform listRoot;

    /// <summary>The inbox row template (inactive).</summary>
    [SerializeField] private Button rowTemplate;

    /// <summary>The list's "No mail." line.</summary>
    [SerializeField] private TMP_Text emptyText;

    /// <summary>The memo's scroll (hidden until a message is open).</summary>
    [SerializeField] private ScrollRect memoScroll;

    /// <summary>The memo: the open message drawn as its form.</summary>
    [SerializeField] private FormView memo;

    /// <summary>The memo's page kind (Form_Memo, TC-950).</summary>
    [SerializeField] private FormSpecSO memoForm;

    /// <summary>"Select a message to read it." (shown instead of the memo).</summary>
    [SerializeField] private TMP_Text selectText;

    /// <summary>The message's link, over the memo (hidden when the message has none).</summary>
    [SerializeField] private Button linkButton;

    private readonly List<Button> _rows = new List<Button>();
    private string _openId;

    private void Awake()
    {
        if (rowTemplate != null)
            rowTemplate.gameObject.SetActive(false);
        if (linkButton != null)
            linkButton.onClick.AddListener(FollowLink);
    }

    private void OnEnable()
    {
        if (feed != null)
            feed.Changed += Redraw;
        Redraw();
    }

    private void OnDisable()
    {
        if (feed != null)
            feed.Changed -= Redraw;
    }

    /// <summary>Opens a message: shows it as a memo and marks it read.</summary>
    public void Open(string id)
    {
        _openId = id;
        if (feed != null)
            feed.MarkRead(id); // raises Changed, which redraws
        Redraw();
    }

    /// <summary>Redraws the inbox and the open message.</summary>
    private void Redraw()
    {
        IReadOnlyList<MailItem> items = feed != null ? feed.Items : new List<MailItem>();
        MailItem open = null;
        foreach (MailItem m in items)
            if (m.Id == _openId)
                open = m;

        DrawRows(items, open);
        if (emptyText != null)
            emptyText.gameObject.SetActive(items.Count == 0);
        ShowMemo(open);
    }

    /// <summary>One row per message, pooled: the subject (bold with a bullet while unread) over the day and the sender.</summary>
    private void DrawRows(IReadOnlyList<MailItem> items, MailItem open)
    {
        if (listRoot == null || rowTemplate == null)
            return;

        while (_rows.Count < items.Count)
        {
            Button row = Instantiate(rowTemplate, listRoot);
            row.gameObject.name = "MailRow";
            int index = _rows.Count;
            row.onClick.AddListener(() => OpenRow(index));
            _rows.Add(row);
        }

        for (int i = 0; i < _rows.Count; i++)
        {
            bool used = i < items.Count;
            _rows[i].gameObject.SetActive(used);
            if (!used)
                continue;

            MailItem m = items[i];
            bool read = feed != null && feed.IsRead(m.Id);
            string subject = read ? MailText.Subject(m) : "<b>" + UiText.Format("mail.unreadRow", MailText.Subject(m)) + "</b>";
            TMP_Text label = _rows[i].GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = subject + "\n<size=80%>" + UiText.Format("mail.rowMeta", m.Day, MailText.From(m)) + "</size>";
            AppRows.MarkSelected(_rows[i], m == open);
        }
    }

    /// <summary>A row was clicked: opens its message.</summary>
    private void OpenRow(int index)
    {
        IReadOnlyList<MailItem> items = feed != null ? feed.Items : null;
        if (items != null && index >= 0 && index < items.Count)
            Open(items[index].Id);
    }

    /// <summary>Draws a message as its memo, Form_Memo (TO, DATE, FROM, REF, SUBJECT, the body, the sender's sign-off and the stamp area), with its link over it; nothing open: the "select a message" line.</summary>
    private void ShowMemo(MailItem m)
    {
        if (memoScroll != null)
            memoScroll.gameObject.SetActive(m != null);
        if (selectText != null)
            selectText.gameObject.SetActive(m == null);
        if (linkButton != null)
            linkButton.gameObject.SetActive(m != null && m.Link != MailLink.None);
        if (m == null)
            return;

        if (memo != null && memoForm != null)
        {
            ContentLibrarySO library = RunManager.HasInstance ? RunManager.Instance.Library : null;
            FormData page = memoForm.Page(library != null ? library.Agency : null);
            page.Text = new Dictionary<string, string>
            {
                { "to", UiText.Get("mail.to") },
                { "date", MailText.Date(m.Day) },
                { "from", MailText.From(m) },
                { "ref", MailText.Ref(m) },
                { "subject", MailText.Subject(m) },
                { "body", MailText.Body(m) },
                { "signature", MailText.From(m) }
            };
            memo.Show(memoForm.form, page, _ => false);
            if (memoScroll != null)
                memoScroll.verticalNormalizedPosition = 1f;
        }
        if (linkButton != null)
        {
            TMP_Text label = linkButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = UiText.Get(m.Link == MailLink.Rules ? "mail.link.rules" : "mail.link.news");
        }
    }

    /// <summary>Follows the open message's link: the Rules (the Investigation app's Rules tab), or the News site's issue of the message's day (the Internet app).</summary>
    private void FollowLink()
    {
        MailItem open = null;
        if (feed != null)
            foreach (MailItem m in feed.Items)
                if (m.Id == _openId)
                    open = m;
        if (open == null)
            return;

        if (open.Link == MailLink.Rules && investigation != null)
            investigation.ShowTab(AppTab.Rules);
        else if (open.Link == MailLink.News && apps != null)
        {
            apps.OpenApp("internet");
            SiteSpec news = NewsSite();
            if (browser != null && news != null)
                browser.Go(NewsPages.IssueAddress(news, open.Day));
        }
    }

    /// <summary>The News site (the first site of the News kind), or null.</summary>
    private static SiteSpec NewsSite()
    {
        ContentLibrarySO library = RunManager.HasInstance ? RunManager.Instance.Library : null;
        if (library != null)
            foreach (SiteSpec site in library.Pc.sites)
                if (site != null && site.kind == SiteKind.News)
                    return site;
        return null;
    }

}

/// <summary>The app windows' list rows (the inbox, the day list).</summary>
public static class AppRows
{
    /// <summary>Shows or hides a row's "Selected" bar (the open message, the shown day).</summary>
    public static void MarkSelected(Button row, bool selected)
    {
        Transform bar = row != null ? row.transform.Find("Selected") : null;
        if (bar != null && bar.gameObject.activeSelf != selected)
            bar.gameObject.SetActive(selected);
    }
}

/// <summary>The Mail app's wording of a message by kind (UI strings): sender, subject, date, reference and body.</summary>
public static class MailText
{
    /// <summary>The sender: the Customs Directorate (memo), the Times, the Timeline Integrity Office (citation) or the authored sender.</summary>
    public static string From(MailItem m) =>
        m.Kind == MailKind.Authored ? m.From
        : UiText.Get(m.Kind == MailKind.TimesIssue ? "mail.from.times" : m.Kind == MailKind.CitationNotice ? "mail.from.integrity" : "mail.from.directorate");

    /// <summary>The subject line.</summary>
    public static string Subject(MailItem m) =>
        m.Kind switch
        {
            MailKind.DirectiveMemo => UiText.Get("mail.subject.memo"),
            MailKind.TimesIssue => UiText.Format("mail.subject.times", m.Day),
            MailKind.CitationNotice => UiText.Format("mail.subject.citation", m.Slot),
            _ => m.Subject
        };

    /// <summary>The day's date in the agency's calendar ("14 Mar 2150"), else "Day n".</summary>
    public static string Date(int day)
    {
        ContentLibrarySO library = RunManager.HasInstance ? RunManager.Instance.Library : null;
        string date = library != null ? AgencyCalendar.Today(library.Agency.firstDate, day) : null;
        return date ?? UiText.Format("notes.day", day);
    }

    /// <summary>The memo's reference: D-3 (directives), TT-3 (the Times), C-3-2 (a citation), M-welcome (authored).</summary>
    public static string Ref(MailItem m) =>
        m.Kind switch
        {
            MailKind.DirectiveMemo => "D-" + m.Day,
            MailKind.TimesIssue => "TT-" + m.Day,
            MailKind.CitationNotice => "C-" + m.Day + "-" + m.Slot,
            _ => "M-" + m.Id.Substring(Mailbox.AuthoredPrefix.Length)
        };

    /// <summary>The body: the directives as a list, the paper's lines and where the edition is (today's on the News site, an earlier one in its archive), the slip's copy, or the authored paragraphs.</summary>
    public static string Body(MailItem m)
    {
        var sb = new StringBuilder();
        switch (m.Kind)
        {
            case MailKind.DirectiveMemo:
                if (m.Body.Count == 0)
                    sb.Append(UiText.Get("mail.body.noDirectives"));
                foreach (string line in m.Body)
                    sb.AppendLine(UiText.Format("list.bullet", line));
                break;
            case MailKind.TimesIssue:
                foreach (string line in m.Body)
                    sb.AppendLine(UiText.Format("list.bullet", line));
                if (m.Body.Count > 0)
                    sb.AppendLine();
                bool today = m.IssueOnHand && RunManager.HasInstance && RunManager.Instance.World.day == m.Day;
                sb.Append(UiText.Get(today ? "mail.body.times" : "mail.body.timesArchived"));
                break;
            case MailKind.CitationNotice:
                sb.AppendLine(UiText.Get("mail.body.citation"));
                sb.AppendLine();
                foreach (string line in m.Body)
                    sb.AppendLine(line);
                break;
            default:
                for (int i = 0; i < m.Body.Count; i++)
                {
                    if (i > 0)
                        sb.AppendLine().AppendLine();
                    sb.Append(m.Body[i]);
                }
                break;
        }
        return sb.ToString().TrimEnd();
    }
}
