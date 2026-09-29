using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's Orders app (Saleh 2026-09-29: the upgrade tree moves
/// to the PC; the portals spec v3 OR3, OR9, §6.2): the Orders window, opening
/// maximised, with the wallet line at the top, the tree's canvas on the left
/// (a scroll both ways over a sidebar-toned plate, holding the inactive
/// templates OrdersWindow clones: a band head with its glyph and name, a node
/// card in the input-field role with its glyph, name, state line, state badge
/// and selection frame, and a link segment dim and lit), and on the right the
/// detail card: the selected node as Form_Requisition (TC-980) on a FormView
/// in a scroll (diegetic, never themed), the hint while nothing is selected,
/// and the one action button under it. Rebuilt fresh. Part of
/// <see cref="OfficeSceneUIBuilder"/>; BuildDesktopShell registers it under
/// DesktopAppIds.Orders.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The Orders detail card's page kind (TC-980).</summary>
    private const string RequisitionFormPath = "Assets/Data/Forms/Form_Requisition.asset";

    /// <summary>The wallet line's height and the action button's height (desktop units).</summary>
    private const float OrdersHeader = 48f, OrdersActionHeight = 52f;

    /// <summary>The Orders window (see the class summary).</summary>
    private static DesktopWindow BuildOrdersWindow(Transform windowLayer, DesktopConfigSO config)
    {
        DestroyChildIfPresent(windowLayer, "OrdersWindow");
        DesktopWindow chrome = BuildOSWindow(windowLayer, "OrdersWindow", "window.orders", null, null, config.ordersWindowSize);
        Transform win = chrome.transform;
        Object.DestroyImmediate(win.Find("Body").gameObject);
        float top = config.titleBarHeight + DocGap;

        TMP_Text wallet = Text(win, "WalletText", "", 20, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, Ink, ThemeRoleId.WindowBody, style: FontStyles.Bold);
        PlaceRect(wallet.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(DocMargin, -(top + OrdersHeader)), new Vector2(-DocMargin, -top));
        wallet.textWrappingMode = TextWrappingModes.Normal;
        wallet.enableAutoSizing = true;
        wallet.fontSizeMax = 20f;
        wallet.fontSizeMin = 14f;

        // The tree: a scroll both ways over a plate.
        float below = top + OrdersHeader + DocGap;
        Transform area = Panel(win, "Tree", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.93f, 0.94f, 0.96f, 1f), ThemeRoleId.Sidebar);
        PlaceRect(area, Vector2.zero, Vector2.one, new Vector2(DocMargin, DocMargin), new Vector2(-(config.ordersDetailWidth + 2f * DocMargin), -below));
        Transform viewport = Panel(area, "Viewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(viewport, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
        GetOrAdd<RectMask2D>(viewport.gameObject);
        var content = (RectTransform)Panel(viewport, "Content", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(100f, 100f), null);
        content.pivot = new Vector2(0f, 1f);
        ScrollRect scroll = GetOrAdd<ScrollRect>(area.gameObject);
        scroll.content = content;
        scroll.viewport = (RectTransform)viewport;
        scroll.horizontal = true;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        RectTransform band = BuildOrdersBand(content, config);
        Button node = BuildOrdersNode(content, config);
        Image link = Panel(content, "LinkTemplate", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(10f, 3f), new Color(0.6f, 0.62f, 0.66f, 1f), ThemeRoleId.Tab)
            .GetComponent<Image>();
        link.raycastTarget = false;
        Image lit = Panel(content, "LinkLitTemplate", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(10f, 3f), new Color(0.2f, 0.6f, 0.25f, 1f), ThemeRoleId.AcceptButton)
            .GetComponent<Image>();
        lit.raycastTarget = false;

        // The detail card: the requisition form on its paper, the hint, and the action under it.
        Transform page = Panel(win, "DetailPage", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FormPaper, ThemeRoleId.DiegeticPaper);
        PlaceRect(page, new Vector2(1f, 0f), Vector2.one, new Vector2(-(config.ordersDetailWidth + DocMargin), DocMargin + OrdersActionHeight + DocGap),
                  new Vector2(-DocMargin, -below));
        TMP_Text select = Text(page, "SelectText", null, 20, TextAlignmentOptions.Center, new Vector2(0.06f, 0.4f), new Vector2(0.94f, 0.6f), Ink,
                               ThemeRoleId.DiegeticRow, "app.orders.select", FontStyles.Italic);
        select.textWrappingMode = TextWrappingModes.Normal;
        float formWidth = config.ordersDetailWidth - 2f * DocMargin - DocGap - DocScrollbar;
        ScrollRect detail = BuildFormScroll(page, "Detail", formWidth, out FormView detailView);

        Button action = MakeButton(win, "ActionButton", null, Vector2.zero, Vector2.zero, null, ThemeRoleId.Button, "app.orders.order");
        PlaceRect(action.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-(config.ordersDetailWidth + DocMargin), DocMargin),
                  new Vector2(-DocMargin, DocMargin + OrdersActionHeight));
        FitLabel(action, 22f);

        OrdersWindow component = win.gameObject.AddComponent<OrdersWindow>();
        var so = new SerializedObject(component);
        Wire(so, "config", config);
        Wire(so, "window", chrome);
        Wire(so, "walletText", wallet);
        Wire(so, "treeContent", content);
        Wire(so, "bandTemplate", band);
        Wire(so, "nodeTemplate", node);
        Wire(so, "linkTemplate", link);
        Wire(so, "linkLitTemplate", lit);
        Wire(so, "detailScroll", detail);
        Wire(so, "detail", detailView);
        Wire(so, "detailForm", AssetDatabase.LoadAssetAtPath<FormSpecSO>(RequisitionFormPath));
        Wire(so, "selectText", select);
        Wire(so, "actionButton", action);
        so.ApplyModifiedProperties();

        band.gameObject.SetActive(false);
        node.gameObject.SetActive(false);
        link.gameObject.SetActive(false);
        lit.gameObject.SetActive(false);
        detail.gameObject.SetActive(false);
        action.gameObject.SetActive(false);
        win.gameObject.SetActive(false);
        return chrome;
    }

    /// <summary>A band head's template: a plate in the sidebar's role with the band's glyph (the role's ink) and its bold name.</summary>
    private static RectTransform BuildOrdersBand(Transform content, DesktopConfigSO config)
    {
        float height = config.ordersBandHead - 6f;
        var band = (RectTransform)Panel(content, "BandTemplate", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(400f, height),
                                        new Color(0.85f, 0.87f, 0.9f, 1f), ThemeRoleId.Sidebar);
        band.GetComponent<Image>().raycastTarget = false;
        Image glyph = Panel(band, "Glyph", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Ink).GetComponent<Image>();
        PlaceRect(glyph.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, -height / 2f + 2f), new Vector2(height, height / 2f - 2f));
        glyph.preserveAspect = true;
        glyph.raycastTarget = false;
        Tag(glyph, ThemeRoleId.Sidebar, ThemePart.Ink);
        TMP_Text label = Text(band, "Label", "", 22, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, Ink, ThemeRoleId.Sidebar, style: FontStyles.Bold,
                              kind: ThemeTextKind.Heading);
        PlaceRect(label.transform, Vector2.zero, Vector2.one, new Vector2(height + 12f, 0f), new Vector2(-8f, 0f));
        label.raycastTarget = false;
        return band;
    }

    /// <summary>A node's template: a card button in the input-field role with the upgrade's glyph, its name (two lines at most), its state line, the state badge at the top right (the padlock, clock or tick, hidden until drawn), the selection frame (hidden) and a CanvasGroup (a locked card is greyed).</summary>
    private static Button BuildOrdersNode(Transform content, DesktopConfigSO config)
    {
        Button node = MakeButton(content, "NodeTemplate", "", new Vector2(0f, 1f), new Vector2(0f, 1f), Color.white, ThemeRoleId.InputField);
        Object.DestroyImmediate(node.transform.Find("Label").gameObject);
        var rt = (RectTransform)node.transform;
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = config.ordersNodeSize;
        GetOrAdd<CanvasGroup>(node.gameObject);

        Image glyph = Panel(node.transform, "Glyph", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Ink).GetComponent<Image>();
        PlaceRect(glyph.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, -24f), new Vector2(58f, 24f));
        glyph.preserveAspect = true;
        glyph.raycastTarget = false;
        Tag(glyph, ThemeRoleId.InputField, ThemePart.Ink);

        TMP_Text name = Text(node.transform, "Name", "", 22, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Ink, ThemeRoleId.InputField, style: FontStyles.Bold);
        PlaceRect(name.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(66f, -54f), new Vector2(-34f, -6f));
        name.textWrappingMode = TextWrappingModes.Normal;
        name.overflowMode = TextOverflowModes.Ellipsis;
        name.maxVisibleLines = 2;
        name.enableAutoSizing = true;
        name.fontSizeMax = 22f;
        name.fontSizeMin = 17f;
        name.raycastTarget = false;

        TMP_Text state = Text(node.transform, "State", "", 19, TextAlignmentOptions.BottomLeft, Vector2.zero, Vector2.one, Ink, ThemeRoleId.InputField);
        PlaceRect(state.transform, Vector2.zero, new Vector2(1f, 0f), new Vector2(66f, 6f), new Vector2(-8f, 32f));
        state.textWrappingMode = TextWrappingModes.NoWrap;
        state.overflowMode = TextOverflowModes.Ellipsis;
        state.enableAutoSizing = true;
        state.fontSizeMax = 19f;
        state.fontSizeMin = 14f;
        state.raycastTarget = false;

        Image badge = Panel(node.transform, "Badge", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Ink).GetComponent<Image>();
        PlaceRect(badge.transform, Vector2.one, Vector2.one, new Vector2(-32f, -32f), new Vector2(-6f, -6f));
        badge.preserveAspect = true;
        badge.raycastTarget = false;
        Tag(badge, ThemeRoleId.InputField, ThemePart.Ink);
        badge.gameObject.SetActive(false);

        Transform selected = BuildFrame(node.transform, "Selected", 3f, AccentInk, ThemeRoleId.FocusRing);
        selected.gameObject.SetActive(false);
        return node;
    }
}
