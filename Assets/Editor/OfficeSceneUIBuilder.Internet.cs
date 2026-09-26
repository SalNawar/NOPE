using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's Internet app (P spec IN1, IN6): a desktop window
/// (BuildOSWindow, run by the window stack) with the browser's chrome
/// (Back, Forward, Home, the address field and Go, the site's search field
/// and Search; themed), a scrolling page (its viewport takes the site's paper
/// colour) with the page renderer's three templates (a text, a link, a filled
/// panel: diegetic roles, so the theme never touches page content) and a
/// status line (the page's title); the BrowserWindow drives it and the
/// Internet desktop icon opens it. Rebuilt from scratch each run; the old
/// placeholder window goes. Part of <see cref="OfficeSceneUIBuilder"/>.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The browser's restored size (it opens maximised: BrowserWindow).</summary>
    private static readonly Vector2 BrowserRestoredSize = new Vector2(1200f, 820f);

    /// <summary>The browser's toolbar height.</summary>
    private const float BrowserToolbarHeight = 44f;

    /// <summary>The status line's height under the page.</summary>
    private const float BrowserStatusHeight = 26f;

    /// <summary>The page's scrollbar width.</summary>
    private const float BrowserScrollbarWidth = 16f;

    /// <summary>The gap between the browser's parts.</summary>
    private const float BrowserGap = 4f;

    /// <summary>The page texts' template size (SiteRenderer sets each block's).</summary>
    private const int BrowserTextSize = 21;

    /// <summary>
    /// Builds the Internet window and its desktop icon, and checks the sites'
    /// fixed page styles once (P spec IN6: every text colour on its page and
    /// boxes at the library's body-text minimum; an error per failing pair).
    /// Returns the window.
    /// </summary>
    private static DesktopWindow BuildInternetWindow(Transform windowLayer, ContentLibrarySO library)
    {
        ContrastRules rules = library != null && library.CultureUi.contrast != null ? library.CultureUi.contrast : new ContrastRules();
        foreach ((string styleName, SiteStyle siteStyle) in SiteStyles.All)
            foreach (string problem in Contrast.Problems(SiteStyles.Pairs(styleName, siteStyle), new Rgba(0f, 0f, 0f), new Rgba(1f, 1f, 1f), rules))
                Debug.LogError($"[TimeDesk] Site page style: {problem} Fix SiteStyles.");

        DestroyChildIfPresent(windowLayer, "IconInternetWindow");
        DestroyChildIfPresent(windowLayer, "InternetWindow");
        DesktopWindow chrome = BuildOSWindow(windowLayer, "InternetWindow", "window.internet", null, "", BrowserRestoredSize);
        Transform win = chrome.transform;
        float top = EnsureDesktopConfig().titleBarHeight;
        SiteStyle style = SiteStyles.For(null);

        // --- The toolbar ---
        Transform bar = Panel(win, "Toolbar", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, XpFace, ThemeRoleId.WindowBody);
        PlaceRect(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(BrowserGap, -(top + BrowserGap + BrowserToolbarHeight)), new Vector2(-BrowserGap, -(top + BrowserGap)));
        Button back = BrowserButton(bar, "BackButton", "browser.back", 0.004f, 0.04f);
        Button forward = BrowserButton(bar, "ForwardButton", "browser.forward", 0.044f, 0.08f);
        Button home = BrowserButton(bar, "HomeButton", "browser.home", 0.084f, 0.14f);
        TMP_InputField address = BuildInputField(bar, "AddressField", "browser.address", new Vector2(0.146f, 0.12f), new Vector2(0.6f, 0.88f));
        Button go = BrowserButton(bar, "GoButton", "browser.go", 0.604f, 0.65f);
        Transform searchBox = Panel(bar, "SearchBox", new Vector2(0.66f, 0f), new Vector2(0.996f, 1f), Vector2.zero, Vector2.zero, null);
        SetAnchors(searchBox, new Vector2(0.66f, 0f), new Vector2(0.996f, 1f));
        TMP_InputField search = BuildInputField(searchBox, "SearchField", "browser.search", new Vector2(0f, 0.12f), new Vector2(0.72f, 0.88f));
        Button searchButton = BrowserButton(searchBox, "SearchButton", "browser.searchButton", 0.74f, 1f);
        searchBox.gameObject.SetActive(false);

        // --- The status line (the window's body text) ---
        TMP_Text status = win.Find("Body").GetComponent<TMP_Text>();
        PlaceRect(status.transform, Vector2.zero, new Vector2(1f, 0f), new Vector2(10f, BrowserGap), new Vector2(-10f, BrowserGap + BrowserStatusHeight));
        status.fontSize = 16;
        status.alignment = TextAlignmentOptions.MidlineLeft;
        status.textWrappingMode = TextWrappingModes.NoWrap;
        status.overflowMode = TextOverflowModes.Ellipsis;

        // --- The page: a scroll view whose viewport is the site's paper ---
        Transform area = Panel(win, "PageArea", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(area, Vector2.zero, Vector2.one, new Vector2(BrowserGap, 2f * BrowserGap + BrowserStatusHeight),
              new Vector2(-BrowserGap, -(top + 2f * BrowserGap + BrowserToolbarHeight)));
        Transform viewport = Panel(area, "Viewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, SiteColor(style.Paper), ThemeRoleId.DiegeticPaper);
        PlaceRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-(BrowserScrollbarWidth + BrowserGap), 0f));
        viewport.gameObject.AddComponent<RectMask2D>();

        Transform content = Panel(viewport, "Content", new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, null);
        ((RectTransform)content).pivot = new Vector2(0.5f, 1f);
        var column = content.gameObject.AddComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(28, 28, 20, 28);
        column.spacing = 10f;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // The renderer's templates, drawn on the paper (inactive; cloned per block).
        Transform templates = Panel(viewport, "Templates", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        TMP_Text textTemplate = Text(templates, "TextTemplate", "Text", BrowserTextSize, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one,
                                     SiteColor(style.Ink), ThemeRoleId.DiegeticRow);
        textTemplate.textWrappingMode = TextWrappingModes.Normal;
        Button linkTemplate = MakeButton(templates, "LinkTemplate", "Link", Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f), ThemeRoleId.DiegeticRow);
        TMP_Text linkLabel = linkTemplate.transform.Find("Label").GetComponent<TMP_Text>();
        linkLabel.fontSize = BrowserTextSize;
        linkLabel.color = SiteColor(style.Link);
        linkLabel.alignment = TextAlignmentOptions.TopLeft;
        linkLabel.fontStyle = FontStyles.Underline;
        linkLabel.textWrappingMode = TextWrappingModes.Normal;
        var linkColumn = linkTemplate.gameObject.AddComponent<VerticalLayoutGroup>();
        linkColumn.padding = new RectOffset(0, 0, 2, 2);
        linkColumn.childControlWidth = true;
        linkColumn.childControlHeight = true;
        linkColumn.childForceExpandWidth = true;
        linkColumn.childForceExpandHeight = false;
        Image panelTemplate = Panel(templates, "PanelTemplate", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, SiteColor(style.Box), ThemeRoleId.DiegeticRow)
            .GetComponent<Image>();
        textTemplate.gameObject.SetActive(false);
        linkTemplate.gameObject.SetActive(false);
        panelTemplate.gameObject.SetActive(false);

        // The scrollbar beside the page.
        Transform track = Panel(area, "Scrollbar", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, XpFace, ThemeRoleId.WindowBody);
        PlaceRect(track, new Vector2(1f, 0f), Vector2.one, new Vector2(-BrowserScrollbarWidth, 0f), Vector2.zero);
        Transform slide = Panel(track, "SlidingArea", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        SetAnchors(slide, Vector2.zero, Vector2.one);
        Transform handle = Panel(slide, "Handle", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, XpBlue, ThemeRoleId.TitleBar);
        SetAnchors(handle, Vector2.zero, Vector2.one);
        Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = (RectTransform)handle;
        scrollbar.targetGraphic = handle.GetComponent<Image>();
        scrollbar.size = 1f; // a page that fits: the handle fills the track (the anchors the scrollbar itself keeps, so rebuilds compare equal)
        scrollbar.value = 0f;

        ScrollRect scroll = area.gameObject.AddComponent<ScrollRect>();
        scroll.content = (RectTransform)content;
        scroll.viewport = (RectTransform)viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        SiteRenderer renderer = area.gameObject.AddComponent<SiteRenderer>();
        var soRenderer = new SerializedObject(renderer);
        SetRef(soRenderer, "scroll", scroll);
        SetRef(soRenderer, "content", content);
        SetRef(soRenderer, "paper", viewport.GetComponent<Image>());
        SetRef(soRenderer, "textTemplate", textTemplate);
        SetRef(soRenderer, "linkTemplate", linkTemplate);
        SetRef(soRenderer, "panelTemplate", panelTemplate);
        soRenderer.ApplyModifiedProperties();

        BrowserWindow browser = win.gameObject.AddComponent<BrowserWindow>();
        var so = new SerializedObject(browser);
        SetRef(so, "window", chrome);
        SetRef(so, "config", EnsureDesktopConfig());
        SetRef(so, "backButton", back);
        SetRef(so, "forwardButton", forward);
        SetRef(so, "homeButton", home);
        SetRef(so, "addressField", address);
        SetRef(so, "goButton", go);
        SetRef(so, "searchBox", searchBox.gameObject);
        SetRef(so, "searchField", search);
        SetRef(so, "searchButton", searchButton);
        SetRef(so, "statusText", status);
        SetRef(so, "page", renderer);
        so.ApplyModifiedProperties();
        return chrome;
    }

    /// <summary>A toolbar button between two horizontal anchors (its label keyed).</summary>
    private static Button BrowserButton(Transform bar, string name, string labelKey, float from, float to)
    {
        Button b = MakeButton(bar, name, null, new Vector2(from, 0.12f), new Vector2(to, 0.88f), null, ThemeRoleId.Button, labelKey);
        SetAnchors(b.transform, new Vector2(from, 0.12f), new Vector2(to, 0.88f));
        return b;
    }

    /// <summary>Sets a RectTransform's anchors and offsets.</summary>
    private static void PlaceRect(Transform t, Vector2 aMin, Vector2 aMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt = (RectTransform)t;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    /// <summary>A page colour as a Unity colour.</summary>
    private static Color SiteColor(Rgba c) => new Color(c.R, c.G, c.B, c.A);
}
