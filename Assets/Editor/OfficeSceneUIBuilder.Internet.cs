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

    /// <summary>The browser's toolbar height (the Investigation app's toolbar row: the PC UX redesign §3).</summary>
    private const float BrowserToolbarHeight = PcSize.Toolbar;

    /// <summary>The status line's height under the page.</summary>
    private const float BrowserStatusHeight = 36f;

    /// <summary>The Home and Go buttons' widths, and the site search box's (its field and Search).</summary>
    private const float BrowserHomeWidth = 108f, BrowserGoWidth = 84f, BrowserSearchWidth = 440f;

    /// <summary>The page's scrollbar width.</summary>
    private const float BrowserScrollbarWidth = 16f;

    /// <summary>The gap between the browser's parts.</summary>
    private const float BrowserGap = 4f;

    /// <summary>The page texts' template size (SiteRenderer sets each block's).</summary>
    private const int BrowserTextSize = PcType.Body;

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
        Transform bar = Panel(win, "Toolbar", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, XpFace, ThemeRoleId.Sidebar);
        PlaceRect(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -(top + BrowserToolbarHeight)), new Vector2(0f, -top));
        Button back = ChevronButton(bar, "BackButton", "browser.back", PcSize.M, true);
        Button forward = ChevronButton(bar, "ForwardButton", "browser.forward", PcSize.M + PcSize.Control + 4f, false);
        float x = PcSize.M + 2f * PcSize.Control + 4f + PcSize.S;
        Button home = BrowserButton(bar, "HomeButton", "browser.home", x, BrowserHomeWidth);
        x += BrowserHomeWidth + PcSize.S;
        float right = PcSize.M + BrowserSearchWidth + PcSize.M;
        TMP_InputField address = BuildInputField(bar, "AddressField", "browser.address", Vector2.zero, Vector2.one);
        PlaceRect(address.transform, Vector2.zero, Vector2.one, new Vector2(x, 10f), new Vector2(-(right + BrowserGoWidth + PcSize.S), -10f));
        Button go = BrowserButton(bar, "GoButton", "browser.go", -(right + BrowserGoWidth), BrowserGoWidth);
        Transform searchBox = Panel(bar, "SearchBox", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(searchBox, new Vector2(1f, 0f), Vector2.one, new Vector2(-(PcSize.M + BrowserSearchWidth), 0f), new Vector2(-PcSize.M, 0f));
        TMP_InputField search = BuildInputField(searchBox, "SearchField", "browser.search", Vector2.zero, Vector2.one);
        PlaceRect(search.transform, Vector2.zero, Vector2.one, new Vector2(0f, 10f), new Vector2(-(128f + PcSize.S), -10f));
        Button searchButton = BrowserButton(searchBox, "SearchButton", "browser.searchButton", -128f, 128f);
        searchBox.gameObject.SetActive(false);

        // --- The status line (the window's body text) ---
        TMP_Text status = win.Find("Body").GetComponent<TMP_Text>();
        PlaceRect(status.transform, Vector2.zero, new Vector2(1f, 0f), new Vector2(PcSize.L, BrowserGap), new Vector2(-PcSize.L, BrowserGap + BrowserStatusHeight));
        status.fontSize = PcType.Caption;
        status.alignment = TextAlignmentOptions.MidlineLeft;
        status.textWrappingMode = TextWrappingModes.NoWrap;
        status.overflowMode = TextOverflowModes.Ellipsis;

        // --- The page: a scroll view whose viewport is the site's paper ---
        Transform area = Panel(win, "PageArea", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(area, Vector2.zero, Vector2.one, new Vector2(BrowserGap, 2f * BrowserGap + BrowserStatusHeight),
              new Vector2(-BrowserGap, -(top + BrowserGap + BrowserToolbarHeight)));
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
        Scrollbar scrollbar = BuildScrollbar(area, BrowserScrollbarWidth, XpFace, ThemeRoleId.WindowBody, XpBlue, ThemeRoleId.TitleBar);

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

    /// <summary>A toolbar button <paramref name="width"/> wide at <paramref name="x"/> from the bar's left (a negative <paramref name="x"/>: from its right), its keyed label at Body size.</summary>
    private static Button BrowserButton(Transform bar, string name, string labelKey, float x, float width)
    {
        Button b = MakeButton(bar, name, null, Vector2.zero, Vector2.one, null, ThemeRoleId.Button, labelKey);
        float pad = (BrowserToolbarHeight - PcSize.Control) / 2f;
        Vector2 anchor = x < 0f ? new Vector2(1f, 0f) : Vector2.zero;
        PlaceRect(b.transform, anchor, new Vector2(anchor.x, 1f), new Vector2(x, pad), new Vector2(x + width, -pad));
        ButtonLabel(b, PcType.Body, TextAlignmentOptions.Center, 4f);
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
