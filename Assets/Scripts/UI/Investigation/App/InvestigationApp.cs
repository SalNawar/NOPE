using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Investigation app (the PC redesign AP1, AP2, AP8, WN5, DK2): one
/// desktop window, "Investigation" (· the traveller's name while one is at
/// the desk), that fills the desktop the first time it opens. Its case header
/// holds the claim, the counters ("Papers 2 of 3 received · 1 scanned ·
/// Deviations 1") and the PC's Accept and Deny (the façade wires them); its
/// toolbar holds Back, Forward, the search field, Steps, Split and Keys, shown
/// but not live until their phases (18-21); the sidebar's Steps, Pinned and
/// Recent are placeholders until then; one pane holds the six tabs.
/// Nothing steals the view: something new for a tab badges it unless the
/// player sees it (AppBadges), and dots the desktop's Investigation icon
/// while the app is closed or minimised; a scan (ScanArrival) opens the app
/// only when it is closed, shows the paper only in a Documents view showing
/// none, and toasts ("… scanned", Open shows it). A new case shows Documents
/// and clears the badges; at the decision the case sources show the no-case
/// state. The window is opened by the desktop's icon, the Start menu, Mail's
/// directive memo (the Rules tab) and the toast. InvestigationUIController
/// drives it.
/// </summary>
public sealed class InvestigationApp : MonoBehaviour
{
    /// <summary>The app's window (maximised on the first open).</summary>
    [SerializeField] private DesktopWindow window;

    /// <summary>The pane with the six tabs.</summary>
    [SerializeField] private AppPane pane;

    [Header("Case header")]
    /// <summary>The claim (the claim banner's text, which the office claim tag also shows).</summary>
    [SerializeField] private TMP_Text claimText;

    /// <summary>The counters: papers received and scanned, deviations logged.</summary>
    [SerializeField] private TMP_Text countersText;

    [Header("Toolbar")]
    /// <summary>Back, Forward, the search field, Steps, Split and Keys: shown, not live until their phases (18-21).</summary>
    [SerializeField] private Selectable[] notYetLive = new Selectable[0];

    [Header("Desktop")]
    /// <summary>The scan toast (on the desktop, above the windows: it shows while the app is down too).</summary>
    [SerializeField] private AppToast toast;

    /// <summary>The desktop's knobs (the toast's time).</summary>
    [SerializeField] private DesktopConfigSO config;

    /// <summary>The desktop's icons (the Investigation icon's dot).</summary>
    [SerializeField] private DesktopIcons icons;

    private readonly AppBadges _badges = new AppBadges();
    private bool _ready;

    /// <summary>True while the app shows (open and not minimised).</summary>
    public bool IsShowing => window != null && window.gameObject.activeSelf;

    /// <summary>True when the pane hosts a view for the tab.</summary>
    public bool Hosts(AppTab tab) => pane != null && pane.Hosts(tab);

    /// <summary>The first open: the app fills the desktop (P spec WN4).</summary>
    private void Start()
    {
        if (window != null && !window.IsMaximised)
            window.ToggleMaximise();
    }

    /// <summary>The app shows (opened or restored): the active tab is seen, and the icon's dot goes.</summary>
    private void OnEnable()
    {
        if (pane == null)
            return;
        Init();
        Seen(pane.ActiveTab);
        if (icons != null)
            icons.SetBadge(DesktopAppIds.Investigation, 0);
    }

    /// <summary>A traveller is presented: the title, the claim, the pane on Documents, the badges and the icon's dot cleared, the toast gone.</summary>
    public void BeginCase(string claim, string travellerName)
    {
        Init();
        if (window != null)
            window.SetTitle(UiText.Format("app.titleCase", travellerName));
        if (claimText != null)
            claimText.text = claim;
        _badges.Clear();
        foreach (AppTab tab in TabOrder.Default)
            pane.SetBadge(tab, false);
        if (icons != null)
            icons.SetBadge(DesktopAppIds.Investigation, 0);
        pane.SetCase(true);
        pane.Show(AppTab.Documents);
        if (toast != null)
            toast.Hide();
    }

    /// <summary>The decision: the case sources show the no-case state; the header waits for the next traveller.</summary>
    public void EndCase()
    {
        Init();
        if (window != null)
            window.SetTitle(UiText.Get("app.title"));
        if (claimText != null)
            claimText.text = UiText.Get("idle.waiting");
        if (countersText != null)
            countersText.text = string.Empty;
        pane.SetCase(false);
        if (toast != null)
            toast.Hide();
    }

    /// <summary>Writes the counters from the case's papers and the logged deviations.</summary>
    public void SetCounters(CasePapers papers, int deviations)
    {
        if (countersText != null && papers != null)
            countersText.text = UiText.Format("app.counters", papers.Received, papers.Count, papers.Scanned, deviations);
    }

    /// <summary>Something new for the tab (a transcript line, a logged deviation): badged unless the player sees it; the icon dotted while the app is down.</summary>
    public void Arrived(AppTab tab)
    {
        Init();
        if (_badges.Arrived(tab, IsShowing ? pane.ActiveTab : (AppTab?)null))
            pane.SetBadge(tab, true);
        if (!IsShowing && icons != null)
            icons.SetBadge(DesktopAppIds.Investigation, IconBadge.Dot);
    }

    /// <summary>Paper <paramref name="paper"/> was scanned (ScanArrival): the app opens when closed, the paper shows when the Documents view shows none, Documents is badged unless seen, a toast names it.</summary>
    public void Scanned(int paper, string paperName)
    {
        Init();
        IAppView documents = pane.View(AppTab.Documents);
        ScanArrival arrival = ScanArrival.Decide(window != null && window.IsOpen, window != null && window.IsMinimised, pane.ActiveTab,
                                                 documents != null && documents.Selected >= 0);
        if (arrival.OpenApp && window != null)
            window.Open();
        if (arrival.ShowPaper && documents != null)
            documents.Select(paper);
        if (arrival.BadgeDocuments)
            Arrived(AppTab.Documents);
        if (arrival.Toast && toast != null)
            toast.Show(UiText.Format("app.toast.scanned", paperName), config != null ? config.toastSeconds : 4f, () => ShowPaper(paper));
    }

    /// <summary>Opens the app (a minimised one restores) on the tab: Mail's directive memo shows the Rules.</summary>
    public void ShowTab(AppTab tab)
    {
        Init();
        if (window != null)
            window.Open();
        pane.Show(tab);
    }

    /// <summary>Wires the pane's shown tabs and turns the toolbar's not-yet-live controls off (once; the app is driven while its window is closed).</summary>
    private void Init()
    {
        if (_ready)
            return;
        _ready = true;
        if (pane != null)
            pane.Shown += Seen;
        foreach (Selectable control in notYetLive)
            if (control != null)
                control.interactable = false;
    }

    /// <summary>The player sees the tab: its badge goes.</summary>
    private void Seen(AppTab tab)
    {
        _badges.Seen(tab);
        pane.SetBadge(tab, false);
    }

    /// <summary>The toast's Open: the app on Documents, showing the scanned paper.</summary>
    private void ShowPaper(int paper)
    {
        ShowTab(AppTab.Documents);
        IAppView documents = pane.View(AppTab.Documents);
        if (documents != null)
            documents.Select(paper);
    }
}
