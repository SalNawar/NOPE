using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// One pane of the Investigation app (the PC redesign AP2, AP5, AP8, AP9;
/// the PC UX redesign IA6, IA7, C4): its header, which names what it shows
/// in full words ("Papers  ›  Leisure Departure Visa": the source, then the
/// item it shows when the view has items) beside the header's Pin and Open
/// beside or Close (the app wires those), and its content (the active
/// source's view; between travellers a case source shows the no-case state,
/// "Waiting for the next traveller", instead). The views are IAppView
/// components, each drawing its page as a form (FormPage). The pane has no
/// tabs of its own: the app's navigator (AppNav) chooses its source and its
/// item, for the active pane. Each pane has its own views and its own
/// back/forward history (NavHistory of LinkTargets: every source switch,
/// item switch, lookup and link is recorded; Back and Forward walk it
/// without recording). A link followed from one of its rows (FollowLink)
/// goes to the app, which sends it to the other pane (Ctrl held: this one).
/// A source is shown only by the player or by the app's own rules (a new
/// case shows Papers in the left pane, AP8); a view never switches it. The
/// active pane's header wears its accent underline while the app is split
/// (SetFrame). Changed tells the app to redraw the navigator.
/// </summary>
public sealed partial class AppPane : MonoBehaviour
{
    /// <summary>The views, one per source (their Tab says which).</summary>
    [SerializeField] private AppView[] views = new AppView[0];

    /// <summary>The header's title: the source and the item it shows.</summary>
    [SerializeField] private TMP_Text titleText;

    /// <summary>The no-case state over the content (a case source between travellers).</summary>
    [SerializeField] private GameObject noCase;

    /// <summary>The active pane's accent underline under its header (shown while the app is split).</summary>
    [SerializeField] private GameObject activeFrame;

    /// <summary>The source the pane shows first.</summary>
    [SerializeField] private AppTab startTab = AppTab.Documents;

    /// <summary>The desktop's knobs: the history's length.</summary>
    [SerializeField] private DesktopConfigSO config;

    private readonly Dictionary<AppTab, IAppView> _views = new Dictionary<AppTab, IAppView>();
    private NavHistory<LinkTarget> _history;
    private AppTab _active;
    private bool _caseOn;
    private bool _ready;

    /// <summary>Raised when a source is shown (the app clears its badge).</summary>
    public event Action<AppPane, AppTab> Shown;

    /// <summary>Raised when what the pane shows changes (a source, an item, the items themselves, the no-case state): the navigator redraws.</summary>
    public event Action<AppPane> Changed;

    /// <summary>Raised when the history changes (the toolbar's Back and Forward follow the active pane's).</summary>
    public event Action<AppPane> HistoryChanged;

    /// <summary>Raised when a row's link is followed here: the target, and true to stay in this pane (Ctrl held).</summary>
    public event Action<AppPane, LinkTarget, bool> LinkFollowed;

    /// <summary>The source the pane shows.</summary>
    public AppTab ActiveTab
    {
        get
        {
            Init();
            return _active;
        }
    }

    /// <summary>True while the pane shows its no-case state (a case source between travellers): it lists no items.</summary>
    public bool Blocked => !_caseOn && TabOrder.IsCaseSource(ActiveTab);

    /// <summary>True when Back has somewhere to go.</summary>
    public bool CanBack => _history != null && _history.CanBack;

    /// <summary>True when Forward has somewhere to go.</summary>
    public bool CanForward => _history != null && _history.CanForward;

    /// <summary>The source's view, or null when the pane has none.</summary>
    public IAppView View(AppTab tab)
    {
        Init();
        return _views.TryGetValue(tab, out IAppView view) ? view : null;
    }

    /// <summary>True when the pane hosts a view for the source (read from the wired views, before the pane first shows).</summary>
    public bool Hosts(AppTab tab)
    {
        foreach (AppView view in views)
            if (view != null && view.Tab == tab)
                return true;
        return false;
    }

    /// <summary>The player shows the source (the navigator): its view with the item it shows, recorded in the history.</summary>
    public void Show(AppTab tab) => Navigate(LinkTarget.ToTab(tab), true);

    /// <summary>The player shows item <paramref name="index"/> of the source the pane shows (the navigator's item), recorded in the history.</summary>
    public void ShowItem(int index) => Navigate(LinkTarget.ToTab(ActiveTab, index), true);

    /// <summary>Goes to <paramref name="target"/> (a link, a dock side, the toast's paper, a pin's or a recent item's jump): its source, item and row, recorded in the history. False when the target is gone (None, or its view says so); the source shows all the same.</summary>
    public bool Go(LinkTarget target) => !target.IsNone && Navigate(target, true);

    /// <summary>Back through the history (not recorded again).</summary>
    public void Back()
    {
        if (_history != null && _history.Back(out LinkTarget at))
            Navigate(at, false);
        HistoryChanged?.Invoke(this);
    }

    /// <summary>Forward through the history (not recorded again).</summary>
    public void Forward()
    {
        if (_history != null && _history.Forward(out LinkTarget at))
            Navigate(at, false);
        HistoryChanged?.Invoke(this);
    }

    /// <summary>A row of this pane follows its link (LK2): the app sends it to the other pane, or keeps it here while Ctrl is held.</summary>
    public void FollowLink(LinkTarget target)
    {
        Keyboard keys = Keyboard.current;
        if (!target.IsNone)
            LinkFollowed?.Invoke(this, target, keys != null && keys.ctrlKey.isPressed);
    }

    /// <summary>A traveller is at the desk (a case source's view shows) or not (it shows the no-case state).</summary>
    public void SetCase(bool on)
    {
        Init();
        _caseOn = on;
        Apply();
    }

    /// <summary>
    /// A new case: the history forgets the last traveller's papers, lines and
    /// deviations (the case sources' places; the day sources' stay) and
    /// records where the pane is now.
    /// </summary>
    public void DropCaseHistory()
    {
        Init();
        _history.RemoveAll(t => TabOrder.IsCaseSource(t.Tab));
        _history.Go(Spot());
        HistoryChanged?.Invoke(this);
    }

    /// <summary>Shows or hides the active pane's accent underline.</summary>
    public void SetFrame(bool on)
    {
        if (activeFrame != null && activeFrame.activeSelf != on)
            activeFrame.SetActive(on);
    }

    /// <summary>The pane shows (the app split, the window opened): it is set up and shows its source.</summary>
    private void OnEnable() => Init();

    /// <summary>Maps the views, follows their items and moves, and shows the first source (once; the pane may be driven while its window is still closed).</summary>
    private void Init()
    {
        if (_ready)
            return;
        _ready = true;
        _history = new NavHistory<LinkTarget>(config != null ? config.paneHistory : 30);
        _active = startTab;

        foreach (AppView view in views)
            if (view != null)
            {
                _views[view.Tab] = view;
                view.ChipsChanged += () => ItemsChanged(view);
                view.Moved += () => ViewMoved(view);
            }
        Navigate(LinkTarget.ToTab(startTab), true);
    }

    /// <summary>Shows the target's source and sends its view there (true when the view finds the target); recorded (<paramref name="record"/>) as the target when it names a row or a lookup, else as where the view is.</summary>
    private bool Navigate(LinkTarget target, bool record)
    {
        Init();
        _active = target.Tab;
        Apply();
        IAppView view = View(_active);
        bool there = view == null || view.Reveal(target);
        if (record)
        {
            _history.Go(target.Key != null || target.Query != null ? target : Spot());
            HistoryChanged?.Invoke(this);
        }
        Title();
        Shown?.Invoke(this, _active);
        Changed?.Invoke(this);
        return there;
    }

    /// <summary>Where the pane is: the active view's spot.</summary>
    private LinkTarget Spot()
    {
        IAppView view = _views.TryGetValue(_active, out IAppView v) ? v : null;
        return view != null ? view.Spot : LinkTarget.ToTab(_active);
    }

    /// <summary>A view's items changed (a paper scanned, another item shown): the title and, for the active one, the navigator follow.</summary>
    private void ItemsChanged(IAppView view)
    {
        if (view.Tab != _active)
            return;
        Title();
        Changed?.Invoke(this);
    }

    /// <summary>The player moved the active view (a lookup): recorded.</summary>
    private void ViewMoved(IAppView view)
    {
        if (view.Tab != _active)
            return;
        _history.Go(view.Spot);
        HistoryChanged?.Invoke(this);
        Title();
    }

    /// <summary>Which view shows and the no-case state.</summary>
    private void Apply()
    {
        bool blocked = Blocked;
        foreach (KeyValuePair<AppTab, IAppView> pair in _views)
            pair.Value.SetVisible(pair.Key == _active && !blocked);
        if (noCase != null && noCase.activeSelf != blocked)
            noCase.SetActive(blocked);
        Title();
        Changed?.Invoke(this);
    }

    /// <summary>The header's title: the source's full name, then the item it shows in bold (a view with items, while a case shows); the source alone in bold otherwise.</summary>
    private void Title()
    {
        if (titleText == null)
            return;
        string source = UiText.Get("app.tab." + _active.ToString().ToLowerInvariant());
        string item = !Blocked && _views.TryGetValue(_active, out IAppView view) && view is IAppItems items ? items.ItemTitle : null;
        if (!string.IsNullOrEmpty(item) && item.StartsWith(source, StringComparison.Ordinal))
            item = item.Substring(source.Length).TrimStart(' ', '\u00B7'); // "Citizen records · Mio" (a pin's title) under Citizen records: "Mio"
        titleText.text = string.IsNullOrEmpty(item) ? "<b>" + source + "</b>" : UiText.Format("app.pane.path", source, item);
    }
}
