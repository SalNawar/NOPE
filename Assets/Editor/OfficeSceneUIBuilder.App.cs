using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's Investigation app (redesign phases 16, 18 and 21;
/// the PC spec's AP1-AP9, ST1, WN4, WN5; the PC UX redesign's IA1-IA12, §3,
/// C2-C6): one desktop window on the window layer ("Investigation"; the
/// restored size from DesktopConfigSO, maximised on its first open) with its
/// toolbar row (Back and Forward as drawn chevrons, the search field, the
/// PC's Accept and Deny with their fixed glyphs; the search's palette is
/// OfficeSceneUIBuilder.Search's), its sidebar (one vertical list that
/// scrolls: the case summary, the navigator's six sources in full words and
/// the template of an item under its source (AppNav), then the checklist,
/// OfficeSceneUIBuilder.Steps) and two panes side by side (AppPane, built by
/// OfficeSceneUIBuilder.Panes: a header naming what it shows with Pin and
/// Open beside or Close, the content with a view per source; the left pane
/// starts on Papers, the right one on Reference books and is hidden until
/// the app splits). Each pane's views are forms (the scanned copy, and
/// OfficeSceneUIBuilder.AppViews' Record Extract, registers, Interview
/// Record, Deviation Report and Directive Memo, each on a FormPage), each
/// behind IAppView; the scan toast goes on the investigation host above the
/// window layer. Rebuilt fresh on each run (the one convergence policy of
/// this partial, audit R6-008); every reference it wires is checked (Wire,
/// audit R6-004). Part of <see cref="OfficeSceneUIBuilder"/>; Build() calls
/// it in its order.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The gap between the sidebar and the panes.</summary>
    private const float AppDividerWidth = 8f;

    /// <summary>The Accept and Deny buttons' widths at the toolbar's right end.</summary>
    private const float AcceptWidth = 184f, DenyWidth = 168f;

    /// <summary>A navigator badge dot's size (at the entry's right end).</summary>
    private const float NavBadgeSize = 14f;

    /// <summary>The scan toast's size, and its gap above the compare dock.</summary>
    private static readonly Vector2 AppToastSize = new Vector2(720f, 64f);

    /// <summary>The sources' label keys, in TabOrder.Default.</summary>
    private static readonly Dictionary<AppTab, string> AppTabKeys = new Dictionary<AppTab, string>
    {
        { AppTab.Documents, "app.tab.documents" },
        { AppTab.Records, "app.tab.records" },
        { AppTab.Reference, "app.tab.reference" },
        { AppTab.Transcript, "app.tab.transcript" },
        { AppTab.Report, "app.tab.report" },
        { AppTab.Rules, "app.tab.rules" },
    };

    /// <summary>The app's parts the rest of Build wires (the façade, the desktop's registry, Mail, the dock): each source's views, one per pane, the left pane's first.</summary>
    private struct AppParts
    {
        public InvestigationApp App;
        public DesktopWindow Window;
        public Button Accept;
        public Button Deny;
        public DocumentsView[] Documents;
        public RecordsView[] Records;
        public ReferenceView[] Reference;
        public TranscriptView[] Transcript;
        public ReportView[] Report;
        public RulesView[] Rules;
        public StepsPanel Steps;
    }

    /// <summary>Sets an object reference, logging an error when the property does not exist (audit R6-004: a renamed field is never skipped silently).</summary>
    private static void Wire(SerializedObject so, string prop, Object value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null)
        {
            Debug.LogError($"[TimeDesk] {so.targetObject.GetType().Name} has no serialized field '{prop}' to wire; fix OfficeSceneUIBuilder.");
            return;
        }
        p.objectReferenceValue = value;
    }

    /// <summary>
    /// Builds the Investigation app on <paramref name="windowLayer"/> and its
    /// scan toast on <paramref name="investHost"/> (above the layer); its rows
    /// pick into <paramref name="compare"/>. The retired per-source windows
    /// (and phase 17's case-tile window) go.
    /// </summary>
    private static AppParts BuildInvestigationApp(Transform windowLayer, Transform investHost, CompareController compare)
    {
        foreach (string retired in new[] { "InvestigationWindow", "DirectivesWindow", "IconScannerWindow", "RecordsWindow", "TranscriptWindow",
                                           "DocumentWindowTemplate", "BookWindowTemplate", "InvestigationApp" })
            DestroyChildIfPresent(windowLayer, retired);

        DesktopConfigSO config = EnsureDesktopConfig();
        DesktopWindow window = BuildOSWindow(windowLayer, "InvestigationApp", null, null, string.Empty, config.investigationWindowSize);
        Transform win = window.transform;
        DestroyChildIfPresent(win, "Body");
        TMP_Text title = win.Find("Header/TitleText").GetComponent<TMP_Text>();
        title.text = UiText.Get("app.title");
        float top = config.titleBarHeight;

        var parts = new AppParts { Window = window };
        AppToolbar toolbar = BuildAppToolbar(win, top, out parts.Accept, out parts.Deny);

        Transform body = Panel(win, "AppBody", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(body, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -(top + PcSize.Toolbar)));
        Transform sidebar = BuildAppSidebar(body, out RectTransform list, out TMP_Text counters);
        Transform panes = Panel(body, "Panes", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(panes, Vector2.zero, Vector2.one, new Vector2(PcSize.Nav + AppDividerWidth, 0f), Vector2.zero);
        AppPane left = BuildAppPane(panes, "PaneLeft", AppTab.Documents, compare, config, true, out PaneViews leftViews, out Button beside);
        AppPane right = BuildAppPane(panes, "PaneRight", AppTab.Reference, compare, config, false, out PaneViews rightViews, out Button closeBeside);
        PlaceRect(right.transform, new Vector2(0.5f, 0f), Vector2.one, new Vector2(config.paneGap / 2f, 0f), Vector2.zero);
        right.gameObject.SetActive(false);
        parts.Documents = new[] { leftViews.Documents, rightViews.Documents };
        parts.Records = new[] { leftViews.Records, rightViews.Records };
        parts.Reference = new[] { leftViews.Reference, rightViews.Reference };
        parts.Transcript = new[] { leftViews.Transcript, rightViews.Transcript };
        parts.Report = new[] { leftViews.Report, rightViews.Report };
        parts.Rules = new[] { leftViews.Rules, rightViews.Rules };

        AppNav nav = BuildAppNav(list);
        parts.Steps = BuildStepsSection(list);
        TMP_Text splitHint = beside.transform.Find("Hint/Label").GetComponent<TMP_Text>();

        AppToast toast = BuildAppToast(investHost, config);

        parts.App = win.gameObject.AddComponent<InvestigationApp>();
        var so = new SerializedObject(parts.App);
        Wire(so, "window", window);
        Wire(so, "leftPane", left);
        Wire(so, "rightPane", right);
        Wire(so, "body", body);
        Wire(so, "sidebar", sidebar);
        Wire(so, "nav", nav);
        Wire(so, "countersText", counters);
        Wire(so, "backButton", toolbar.Back);
        Wire(so, "forwardButton", toolbar.Forward);
        Wire(so, "splitButton", beside);
        Wire(so, "splitHint", splitHint);
        Wire(so, "closeSplitButton", closeBeside);
        Wire(so, "toast", toast);
        Wire(so, "config", config);
        so.ApplyModifiedProperties();
        WireStepsPanel(parts.Steps, parts, toast, config);
        BuildAppSearch(parts.App, win, top + PcSize.Toolbar, config);
        return parts;
    }

    /// <summary>The toolbar's controls the app wires.</summary>
    private struct AppToolbar
    {
        public Button Back;
        public Button Forward;
    }

    /// <summary>
    /// The toolbar row (IA5, §3): on the Sidebar role's surface under the title
    /// bar, Back and Forward (drawn chevrons, hover hints naming them), the
    /// search field (flexible; search's: BuildAppSearch, the keys': BuildAppKeys),
    /// and the PC's Accept and Deny at its right end with their fixed glyphs
    /// (piece 6 R8).
    /// </summary>
    private static AppToolbar BuildAppToolbar(Transform win, float top, out Button accept, out Button deny)
    {
        Transform bar = Panel(win, "Toolbar", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -(top + PcSize.Toolbar / 2f)),
                              new Vector2(0f, PcSize.Toolbar), XpFace, ThemeRoleId.Sidebar);
        float pad = (PcSize.Toolbar - PcSize.Control) / 2f;
        var toolbar = new AppToolbar
        {
            Back = ChevronButton(bar, "BackButton", "browser.back", PcSize.M, true),
            Forward = ChevronButton(bar, "ForwardButton", "browser.forward", PcSize.M + PcSize.Control + 4f, false),
        };

        float right = PcSize.M + DenyWidth + PcSize.S + AcceptWidth + PcSize.L;
        TMP_InputField search = BuildInputField(bar, "SearchField", "app.search", Vector2.zero, Vector2.one);
        PlaceRect(search.transform, Vector2.zero, Vector2.one, new Vector2(PcSize.M + 2f * PcSize.Control + 4f + PcSize.L, pad), new Vector2(-right, -pad));

        accept = MakeButton(bar, "AcceptButton", null, new Vector2(1f, 0f), Vector2.one, new Color(0.2f, 0.5f, 0.24f, 1f), ThemeRoleId.AcceptButton, "accept");
        PlaceRect(accept.transform, new Vector2(1f, 0f), Vector2.one, new Vector2(-(PcSize.M + DenyWidth + PcSize.S + AcceptWidth), pad), new Vector2(-(PcSize.M + DenyWidth + PcSize.S), -pad));
        deny = MakeButton(bar, "DenyButton", null, new Vector2(1f, 0f), Vector2.one, new Color(0.72f, 0.2f, 0.18f, 1f), ThemeRoleId.DenyButton, "deny");
        PlaceRect(deny.transform, new Vector2(1f, 0f), Vector2.one, new Vector2(-(PcSize.M + DenyWidth), pad), new Vector2(-PcSize.M, -pad));
        foreach (Button decision in new[] { accept, deny })
        {
            TMP_Text label = ButtonLabel(decision, PcType.Body, TextAlignmentOptions.Center, 0f);
            label.fontStyle = FontStyles.Bold;
        }
        BuildDecisionGlyph(accept, ThemeRoleId.AcceptButton, true);
        BuildDecisionGlyph(deny, ThemeRoleId.DenyButton, false);
        return toolbar;
    }

    /// <summary>A toolbar button of PcSize.Control square at <paramref name="x"/> from the bar's left, its label gone and a chevron drawn in the Button role's ink (pointing left for Back), with a hover hint (<paramref name="hintKey"/>).</summary>
    private static Button ChevronButton(Transform bar, string name, string hintKey, float x, bool back)
    {
        float pad = (PcSize.Toolbar - PcSize.Control) / 2f;
        Button button = MakeButton(bar, name, null, Vector2.zero, new Vector2(0f, 1f), null, ThemeRoleId.Button);
        PlaceRect(button.transform, Vector2.zero, new Vector2(0f, 1f), new Vector2(x, pad), new Vector2(x + PcSize.Control, -pad));
        DestroyChildIfPresent(button.transform, "Label");
        float s = back ? 1f : -1f;
        ButtonStroke(button.transform, "Upper", new Vector2(-1.5f * s, 4.5f), new Vector2(2.5f, 13f), -45f * s);
        ButtonStroke(button.transform, "Lower", new Vector2(-1.5f * s, -4.5f), new Vector2(2.5f, 13f), 45f * s);
        BuildHoverHint(button, hintKey, null, new Vector2(0.5f, 0f), new Vector2(0f, 1f));
        return button;
    }

    /// <summary>One stroke of a drawn glyph on a Button-role control, in its ink (no raycast).</summary>
    private static void ButtonStroke(Transform parent, string name, Vector2 centre, Vector2 size, float angle)
    {
        Transform bar = Panel(parent, name, Center, Center, centre, size, Ink);
        bar.localRotation = Quaternion.Euler(0f, 0f, angle);
        Image image = bar.GetComponent<Image>();
        image.raycastTarget = false;
        SceneUiKit.Tag(image, ThemeRoleId.Button, ThemePart.Ink);
    }

    /// <summary>
    /// The sidebar (IA1, IA4, IA8, §3): a Sidebar-role column of PcSize.Nav
    /// whose one vertical list scrolls when it outgrows the window: the case
    /// summary (the counters, or the idle line) at its top; the navigator
    /// (BuildAppNav) and the checklist (BuildStepsSection) are added to the
    /// list after it. Returns the sidebar, the list in <paramref name="list"/>
    /// and the summary's text in <paramref name="counters"/>.
    /// </summary>
    private static Transform BuildAppSidebar(Transform body, out RectTransform list, out TMP_Text counters)
    {
        Transform side = Panel(body, "Sidebar", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero, XpFace, ThemeRoleId.Sidebar);
        PlaceRect(side, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(PcSize.Nav, 0f));

        Transform viewport = Panel(side, "Viewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        GetOrAdd<RectMask2D>(viewport.gameObject);
        list = (RectTransform)Panel(viewport, "List", new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero, null);
        list.pivot = new Vector2(0.5f, 1f);
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(list.gameObject);
        layout.padding = new RectOffset(12, 12, 12, 16);
        layout.spacing = 2f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fit = GetOrAdd<ContentSizeFitter>(list.gameObject);
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        ScrollRect scroll = GetOrAdd<ScrollRect>(side.gameObject);
        scroll.viewport = (RectTransform)viewport;
        scroll.content = list;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;
        scroll.scrollSensitivity = 30f;

        counters = Text(list, "CaseSummary", UiText.Get("idle.waiting"), PcType.Caption, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Ink,
                        ThemeRoleId.Sidebar, kind: ThemeTextKind.Body);
        Chrome(counters, PcType.Caption, true);
        counters.margin = new Vector4(PcSize.S, 4f, PcSize.S, PcSize.M);
        counters.raycastTarget = false;
        LayoutElement summary = GetOrAdd<LayoutElement>(counters.gameObject);
        summary.minHeight = PcSize.NavCase;
        return side;
    }

    /// <summary>
    /// The navigator (IA1-IA3, IA11, C2, C3) in the sidebar's list: an entry
    /// per source in TabOrder.Default (BuildNavEntry: its name, its selected
    /// look, its beside outline, its badge dot, its drag handle), then the
    /// inactive item template (a row indented under its source whose label
    /// wraps and whose height follows it; its Chosen plate marks the shown
    /// item), which AppNav clones after the selected source.
    /// </summary>
    private static AppNav BuildAppNav(RectTransform list)
    {
        AppNav nav = GetOrAdd<AppNav>(list.gameObject);
        var entries = new List<Object>();
        var selected = new List<Object>();
        var beside = new List<Object>();
        var badges = new List<Object>();
        foreach (AppTab tab in TabOrder.Default)
        {
            Button entry = BuildNavEntry(list, tab, nav, out GameObject chosen, out GameObject outline, out GameObject dot);
            entries.Add(entry);
            selected.Add(chosen);
            beside.Add(outline);
            badges.Add(dot);
        }

        Button item = MakeButton(list, "ItemTemplate", "Item", Vector2.zero, Vector2.one, XpFace, ThemeRoleId.Sidebar);
        VerticalLayoutGroup itemLayout = GetOrAdd<VerticalLayoutGroup>(item.gameObject);
        itemLayout.padding = new RectOffset((int)(PcSize.NavIndent + PcSize.M), (int)PcSize.M, 8, 8);
        itemLayout.childControlWidth = true;
        itemLayout.childControlHeight = true;
        itemLayout.childForceExpandWidth = true;
        itemLayout.childForceExpandHeight = false;
        GetOrAdd<LayoutElement>(item.gameObject).minHeight = PcSize.NavItem;
        Transform chosenPlate = Panel(item.transform, "Chosen", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Paper, ThemeRoleId.TabActive);
        PlaceRect(chosenPlate, Vector2.zero, Vector2.one, new Vector2(PcSize.NavIndent - 4f, 2f), new Vector2(0f, -2f));
        chosenPlate.GetComponent<Image>().raycastTarget = false;
        GetOrAdd<LayoutElement>(chosenPlate.gameObject).ignoreLayout = true;
        chosenPlate.SetAsFirstSibling();
        chosenPlate.gameObject.SetActive(false);
        TMP_Text label = item.transform.Find("Label").GetComponent<TMP_Text>();
        Chrome(label, PcType.Body, true);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        SceneUiKit.Tag(label, ThemeRoleId.Sidebar, ThemePart.Ink, null, FontStyles.Normal, ThemeTextKind.Body, false);
        item.gameObject.SetActive(false);

        var so = new SerializedObject(nav);
        SerializedArrays.Set(so, "entries", entries);
        SerializedArrays.Set(so, "selected", selected);
        SerializedArrays.Set(so, "beside", beside);
        SerializedArrays.Set(so, "badges", badges);
        Wire(so, "itemTemplate", item);
        so.ApplyModifiedProperties();
        return nav;
    }

    /// <summary>
    /// One source's entry (C2): a Sidebar-role row of PcSize.NavEntry with its
    /// full name at Body size; its selected look (an accent plate, the Badge
    /// role, with the name again in its ink, bold) over it; the beside outline
    /// (2 u, the FocusRing role); the badge dot at its right end; a tooltip-free
    /// handle that drags it along the list or opens its menu (AppTabHandle).
    /// </summary>
    private static Button BuildNavEntry(Transform list, AppTab tab, AppNav nav, out GameObject selected, out GameObject beside, out GameObject badge)
    {
        Button entry = MakeButton(list, "Source_" + tab, null, Vector2.zero, Vector2.one, XpFace, ThemeRoleId.Sidebar, AppTabKeys[tab]);
        SetLayoutHeight(entry, PcSize.NavEntry);
        TMP_Text label = ButtonLabel(entry, PcType.Body, TextAlignmentOptions.MidlineLeft, PcSize.M);
        label.margin = new Vector4(PcSize.M, 0f, PcSize.L + NavBadgeSize, 0f);

        Transform plate = Panel(entry.transform, "Selected", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, XpGreen, ThemeRoleId.Badge);
        plate.GetComponent<Image>().raycastTarget = false;
        TMP_Text chosen = Text(plate, "Label", null, PcType.Body, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, Color.white,
                               ThemeRoleId.Badge, AppTabKeys[tab], FontStyles.Bold, ThemeTextKind.Button, true);
        Chrome(chosen, PcType.Body);
        chosen.margin = new Vector4(PcSize.M, 0f, PcSize.L + NavBadgeSize, 0f);
        chosen.raycastTarget = false;
        plate.gameObject.SetActive(false);

        Transform outline = BuildFrame(entry.transform, "Beside", 2f, new Color(0.95f, 0.55f, 0.1f, 1f), ThemeRoleId.FocusRing);
        outline.gameObject.SetActive(false);

        Transform dot = Panel(entry.transform, "Badge", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-(PcSize.M + NavBadgeSize / 2f), 0f),
                              new Vector2(NavBadgeSize, NavBadgeSize), XpGreen, ThemeRoleId.Badge);
        Image dotImage = dot.GetComponent<Image>();
        dotImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        dotImage.raycastTarget = false;
        dot.gameObject.SetActive(false);

        AppTabHandle handle = GetOrAdd<AppTabHandle>(entry.gameObject);
        var so = new SerializedObject(handle);
        Wire(so, "nav", nav);
        so.FindProperty("tab").enumValueIndex = (int)tab;
        so.ApplyModifiedProperties();

        selected = plate.gameObject;
        beside = outline.gameObject;
        badge = dot.gameObject;
        return entry;
    }

    /// <summary>A view's root: the whole content, hidden until its source shows.</summary>
    private static Transform ViewRoot(Transform content, string name, Color? fill, ThemeRoleId? role)
    {
        Transform view = Panel(content, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fill, role);
        view.gameObject.SetActive(false);
        return view;
    }

    /// <summary>
    /// The Papers view (§2.4): the scanner's dark backing (the form style's),
    /// the hint shown instead of a copy, and the scanned-copy page template (the
    /// paper's form in a scroll, OfficeSceneUIBuilder.PcForms), cloned per
    /// paper by DocumentsView.
    /// </summary>
    private static DocumentsView BuildDocumentsView(Transform content, out AppView view)
    {
        FormStyleSO style = EnsureFormStyle();
        Transform root = ViewRoot(content, "DocumentsView", style.backing, ThemeRoleId.DiegeticBacking);
        TMP_Text hint = Text(root, "Hint", UiText.Get("app.doc.none"), PcType.Body, TextAlignmentOptions.Center, new Vector2(0.08f, 0.4f), new Vector2(0.92f, 0.6f),
                             style.backingInk, ThemeRoleId.DiegeticBacking);
        hint.textWrappingMode = TextWrappingModes.Normal;

        DocumentWindowController copy = BuildDocumentPage(root);

        DocumentsView documents = root.gameObject.AddComponent<DocumentsView>();
        var soView = new SerializedObject(documents);
        Wire(soView, "pageTemplate", copy);
        Wire(soView, "hintText", hint);
        soView.ApplyModifiedProperties();
        view = documents;
        return documents;
    }

    /// <summary>A labelled checkbox (a Toggle on a Button-role plate: the box, its check, the keyed label at Body size), on by default.</summary>
    private static Toggle BuildToggle(Transform parent, string name, string labelKey, Vector2 aMin, Vector2 aMax)
    {
        Transform plate = Panel(parent, name, aMin, aMax, Vector2.zero, Vector2.zero, XpFace, ThemeRoleId.Button);
        SetAnchors(plate, aMin, aMax);
        Transform box = Panel(plate, "Box", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(PcSize.M + 14f, 0f), new Vector2(28f, 28f), Color.white, ThemeRoleId.InputField);
        Transform check = Panel(box, "Check", Center, Center, Vector2.zero, new Vector2(16f, 16f), XpGreen, ThemeRoleId.Badge);
        check.GetComponent<Image>().raycastTarget = false;
        TMP_Text label = Text(plate, "Label", null, PcType.Body, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, Ink, ThemeRoleId.Button, labelKey,
                              FontStyles.Normal, ThemeTextKind.Button, true);
        ((RectTransform)label.transform).offsetMin = new Vector2(PcSize.M + 28f + PcSize.M, 0f);
        label.raycastTarget = false;
        Toggle toggle = GetOrAdd<Toggle>(plate.gameObject);
        toggle.targetGraphic = box.GetComponent<Image>();
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = true;
        return toggle;
    }

    /// <summary>The scan toast (WN5, C10) on the investigation host, above the window layer: centred right above the compare dock, its line at Body size and Open, hidden.</summary>
    private static AppToast BuildAppToast(Transform investHost, DesktopConfigSO config)
    {
        DestroyChildIfPresent(investHost, "AppToast");
        Transform strip = Panel(investHost, "AppToast", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, config.MaximisedBottom + PcSize.M + AppToastSize.y / 2f),
                                AppToastSize, PanelNavy, ThemeRoleId.Toast);
        TMP_Text line = Text(strip, "Text", string.Empty, PcType.Body, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0f), new Vector2(0.76f, 1f), Color.white,
                             ThemeRoleId.Toast, fit: true);
        ((RectTransform)line.transform).offsetMin = new Vector2(PcSize.L + 4f, 0f);
        Button open = MakeButton(strip, "OpenButton", null, new Vector2(0.78f, 0.18f), new Vector2(0.97f, 0.82f), null, ThemeRoleId.Button, "app.toast.open");
        ButtonLabel(open, PcType.Body);
        AppToast toast = strip.gameObject.AddComponent<AppToast>();
        var so = new SerializedObject(toast);
        Wire(so, "text", line);
        Wire(so, "openButton", open);
        so.ApplyModifiedProperties();
        strip.gameObject.SetActive(false);
        return toast;
    }
}
