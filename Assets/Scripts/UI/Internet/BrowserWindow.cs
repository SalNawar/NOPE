using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Internet app (P spec IN1): a browser over the in-game sites. Its
/// chrome (themed) has Back, Forward and Home (the start page's tiles), an
/// address field (Enter or Go opens a typed site name, id, domain or
/// chronet:// address: Sites.Resolve) and, on a site that has one, the site's
/// search field (the Lineage Archive's name search, keeping its filters).
/// Links in a page navigate; every visit goes on a back/forward stack
/// (NavHistory, capped by DesktopConfigSO.browserHistory). It opens
/// maximised the first time (a desktop window of the window stack, opened by
/// the Internet icon). Each time the window opens it gathers the run's
/// Internet anew (SiteWorldBuilder) and redraws the page it shows, so the
/// day's news and history are always current.
/// </summary>
public sealed class BrowserWindow : MonoBehaviour
{
    /// <summary>The window's chrome (maximised on the first open).</summary>
    [SerializeField] private DesktopWindow window;

    /// <summary>The desktop's knobs (the history cap).</summary>
    [SerializeField] private DesktopConfigSO config;

    [Header("Chrome")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button forwardButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private TMP_InputField addressField;
    [SerializeField] private Button goButton;

    /// <summary>The site's own search (shown on a site that has one).</summary>
    [SerializeField] private GameObject searchBox;
    [SerializeField] private TMP_InputField searchField;
    [SerializeField] private Button searchButton;

    /// <summary>The status line under the page: the page's title.</summary>
    [SerializeField] private TMP_Text statusText;

    [Header("Page")]
    [SerializeField] private SiteRenderer page;

    /// <summary>The visits (created on the first open, sized by DesktopConfigSO.browserHistory).</summary>
    private NavHistory<string> _history;

    /// <summary>The run's Internet as it stood when the window opened.</summary>
    private SiteWorld _world;

    /// <summary>The page shown (null before the window first opens).</summary>
    private SitePage _shown;

    private void Awake()
    {
        if (backButton != null)
            backButton.onClick.AddListener(Back);
        if (forwardButton != null)
            forwardButton.onClick.AddListener(Forward);
        if (homeButton != null)
            homeButton.onClick.AddListener(() => Go(Sites.PortalAddress));
        if (addressField != null)
            addressField.onSubmit.AddListener(_ => GoTyped());
        if (goButton != null)
            goButton.onClick.AddListener(GoTyped);
        if (searchField != null)
            searchField.onSubmit.AddListener(_ => Search());
        if (searchButton != null)
            searchButton.onClick.AddListener(Search);
        if (page != null)
            page.LinkClicked += Go;
    }

    /// <summary>The first open: the browser fills the desktop (P spec IN1).</summary>
    private void Start()
    {
        if (window != null && !window.IsMaximised)
            window.ToggleMaximise();
    }

    private void OnDestroy()
    {
        if (page != null)
            page.LinkClicked -= Go;
    }

    /// <summary>Opening the window (its icon shows it): gathers the run's Internet anew and redraws the page it shows, else the start page.</summary>
    private void OnEnable()
    {
        RunManager run = RunManager.HasInstance ? RunManager.Instance : null;
        _world = SiteWorldBuilder.Build(run != null ? run.Library : null, run != null ? run.World : null);
        if (_history == null)
            _history = new NavHistory<string>(config != null ? config.browserHistory : 1);

        if (_history.HasCurrent)
            Show(Sites.Page(_world, _history.Current));
        else
            Go(Sites.PortalAddress);
    }

    /// <summary>Opens an address (a link, Home, the address field): the page there, added to the visits.</summary>
    public void Go(string address)
    {
        if (_world == null)
            return;
        SitePage next = Sites.Page(_world, address);
        _history.Go(next.Address);
        Show(next);
    }

    /// <summary>Back to the page before.</summary>
    private void Back()
    {
        if (_history != null && _history.Back(out string address))
            Show(Sites.Page(_world, address));
    }

    /// <summary>Forward to the page after.</summary>
    private void Forward()
    {
        if (_history != null && _history.Forward(out string address))
            Show(Sites.Page(_world, address));
    }

    /// <summary>Opens what the address field holds (Sites.Resolve over today's listed sites).</summary>
    private void GoTyped()
    {
        if (_world != null && addressField != null)
            Go(Sites.Resolve(addressField.text, Sites.ListedOn(_world.Sites, _world.Day)));
    }

    /// <summary>The site's search: the Lineage Archive's name search with the shown page's country and era filters.</summary>
    private void Search()
    {
        SiteSpec site = SiteOfShown();
        if (site == null || site.kind != SiteKind.Ancestry || searchField == null)
            return;
        SiteAddress at = SiteAddress.Parse(_shown.Address);
        Go(AncestryPages.SearchAddress(site, searchField.text, at.Get("country"), at.Get("era")));
    }

    /// <summary>Draws a page in its site's style and updates the chrome.</summary>
    private void Show(SitePage shown)
    {
        _shown = shown;
        SiteSpec site = SiteOfShown();
        if (page != null)
            page.Render(shown, SiteStyles.For(site != null ? site.kind.ToString() : null));
        if (addressField != null)
            addressField.SetTextWithoutNotify(shown.Address);
        if (statusText != null)
            statusText.text = shown.Title;
        if (backButton != null)
            backButton.interactable = _history.CanBack;
        if (forwardButton != null)
            forwardButton.interactable = _history.CanForward;

        bool searchable = site != null && site.kind == SiteKind.Ancestry;
        if (searchBox != null)
            searchBox.SetActive(searchable);
        if (searchable && searchField != null)
            searchField.SetTextWithoutNotify(SiteAddress.Parse(shown.Address).Get("name"));
    }

    /// <summary>The shown page's site, or null (the start page, a missing page).</summary>
    private SiteSpec SiteOfShown() =>
        _shown == null || _shown.SiteId == null || _world == null ? null : System.Linq.Enumerable.FirstOrDefault(_world.Sites, s => s != null && s.id == _shown.SiteId);
}
