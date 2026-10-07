using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Orders app (Saleh 2026-09-29: "the upgrade tree moves to a new PC
/// desktop icon, a real tree with prerequisites, drawn visually"; the portals
/// spec v3 OR3, OR9, §6.2): the wallet line over the tree, drawn from
/// UpgradeTree.Layout through OrderBook: one band per branch (its glyph and
/// name), each node a card with its icon (the upgrade's art slot, else its
/// band's code-drawn glyph), its name and its state line, a badge for the
/// state (a padlock when locked, a clock in transit, a tick when owned) and
/// a greyed card while locked, and elbow links from each prerequisite to its
/// dependants, dim until the prerequisite is owned, lit after. Beside it the
/// detail card: the selected node as the TC-980 requisition form (item,
/// section, price, status, requires, blurb) with its one action button:
/// Order (paid now, arrives tomorrow), Cancel order (today's, refunded),
/// Install tomorrow / Cancel install / Keep installed for an owned scanner
/// (one installed at a time). The arrows walk the tree, scrolling the
/// selected node into view, and Enter acts (DesktopKeyboard, UpgradeTree.Step).
/// The tree zooms, pans and scrolls (Saleh: "zoom drag and scroll";
/// TreeScrollRect): Ctrl+wheel about the pointer, the − and + buttons over
/// its corner (with the level between them) and Ctrl+=, Ctrl+- and Ctrl+0,
/// through the readable levels only (TreeZoom.ReadableLevels: never so far
/// out that a node's smallest text reads under DesktopConfigSO.ordersTextFloor).
/// It opens maximised and redraws when the day, the wallet, the order log, the
/// owned upgrades or the installs change; each node's parts are found once,
/// when the tree is laid out, so a redraw looks nothing up.
/// </summary>
public sealed class OrdersWindow : MonoBehaviour
{
    /// <summary>The tree's sizes (cell, node, band head, link width) and its zoom levels and text floor.</summary>
    [SerializeField] private DesktopConfigSO config;

    /// <summary>The app's window (it opens maximised; the keyboard routes to it while it has the focus).</summary>
    [SerializeField] private DesktopWindow window;

    /// <summary>The wallet line over the tree.</summary>
    [SerializeField] private TMP_Text walletText;

    /// <summary>The tree's view: scrolls, pans and zooms the content.</summary>
    [SerializeField] private TreeScrollRect view;

    /// <summary>The tree's content (top-left pivot; bands, links and nodes are cloned into it).</summary>
    [SerializeField] private RectTransform treeContent;

    /// <summary>A band's head template (inactive): its "Glyph" image and its "Label" text.</summary>
    [SerializeField] private RectTransform bandTemplate;

    /// <summary>A node's template (inactive): the card button with its "Glyph", "Name", "State", "Badge" and "Selected" children and a CanvasGroup.</summary>
    [SerializeField] private Button nodeTemplate;

    /// <summary>A link segment before its prerequisite is owned (inactive).</summary>
    [SerializeField] private Image linkTemplate;

    /// <summary>A link segment once its prerequisite is owned (inactive).</summary>
    [SerializeField] private Image linkLitTemplate;

    /// <summary>The zoom's − button (a level out; off at the lowest readable level).</summary>
    [SerializeField] private Button zoomOutButton;

    /// <summary>The zoom's + button (a level in; off at the highest level).</summary>
    [SerializeField] private Button zoomInButton;

    /// <summary>The zoom level between the buttons ("125 %").</summary>
    [SerializeField] private TMP_Text zoomText;

    /// <summary>The detail card's scroll (hidden until a node is selected).</summary>
    [SerializeField] private ScrollRect detailScroll;

    /// <summary>The detail card: the selected node as its form.</summary>
    [SerializeField] private FormView detail;

    /// <summary>The detail card's page kind (Form_Requisition, TC-980).</summary>
    [SerializeField] private FormSpecSO detailForm;

    /// <summary>"Select an item in the tree to see it here." (instead of the card).</summary>
    [SerializeField] private TMP_Text selectText;

    /// <summary>The selected node's action: Order, Cancel order, Install tomorrow, Cancel install or Keep installed (hidden when there is none).</summary>
    [SerializeField] private Button actionButton;

    /// <summary>The UI kit (run 7): a node's card and state badge come from it (UiKitNames.UpgradeCard and UpgradeBadge, one card for Orders and the House); without it the template's card stays and the badges are code-drawn.</summary>
    [SerializeField] private UiKitSO kit;

    /// <summary>A locked node's card alpha.</summary>
    [SerializeField, Range(0.2f, 1f)] private float lockedAlpha = 0.55f;

    /// <summary>The detail card's slots are never picked (the requisition form is read, not cited).</summary>
    private static readonly Func<FormSlot, bool> NothingPicks = _ => false;

    /// <summary>A node's card and the parts a redraw writes, found once when the tree is laid out.</summary>
    private sealed class NodeView
    {
        public RectTransform Card;
        public Image Face;
        public Image Glyph;
        public UpgradeBranch Branch;
        public TMP_Text State;
        public Image Badge;
        public CanvasGroup Group;
        public GameObject Selected;
    }

    private readonly List<GameObject> _built = new List<GameObject>();
    private readonly Dictionary<string, NodeView> _nodes = new Dictionary<string, NodeView>();
    private readonly List<(string from, Image dim, Image lit)> _links = new List<(string, Image, Image)>();
    private readonly Dictionary<string, Sprite> _glyphs = new Dictionary<string, Sprite>();
    private readonly List<Texture2D> _glyphTextures = new List<Texture2D>();
    private readonly Dictionary<string, string> _detailText = new Dictionary<string, string>();
    private TMP_Text _actionLabel;
    private TreeLayout _layout;
    private string _selected;
    private int _signature;

    /// <summary>The app's window.</summary>
    public DesktopWindow Window => window;

    private static WorldState World => RunManager.HasInstance ? RunManager.Instance.World : null;

    private static ContentLibrarySO Library => RunManager.HasInstance ? RunManager.Instance.Library : null;

    private void Awake()
    {
        foreach (Component template in new Component[] { bandTemplate, nodeTemplate, linkTemplate, linkLitTemplate })
            if (template != null)
                template.gameObject.SetActive(false);
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(Act);
            _actionLabel = actionButton.GetComponentInChildren<TMP_Text>(true);
        }
        if (zoomOutButton != null)
            zoomOutButton.onClick.AddListener(() => Zoom(-1));
        if (zoomInButton != null)
            zoomInButton.onClick.AddListener(() => Zoom(1));
        if (view != null)
        {
            view.LevelChanged += ShowZoom;
            if (config != null)
                view.SetLevels(TreeZoom.ReadableLevels(config.ordersZoomLevels, SmallestText(), config.ordersTextFloor));
        }
        ShowZoom();
    }

    /// <summary>The first open: the tree fills the desktop.</summary>
    private void Start()
    {
        if (window != null && !window.IsMaximised)
            window.ToggleMaximise();
    }

    private void OnEnable()
    {
        Build();
        Redraw();
    }

    /// <summary>Redraws when the run changes under the open window (a decision's pay, the day's turn).</summary>
    private void Update()
    {
        if (Signature() != _signature)
            Redraw();
    }

    private void OnDestroy()
    {
        if (view != null)
            view.LevelChanged -= ShowZoom;
        foreach (Sprite s in _glyphs.Values)
            Destroy(s);
        foreach (Texture2D t in _glyphTextures)
            Destroy(t);
    }

    /// <summary>The arrows (DesktopKeyboard): moves the selection along the links or within a tier (UpgradeTree.Step) and scrolls it into view; nothing that way keeps it.</summary>
    public void Step(int dx, int dy)
    {
        string next = UpgradeTree.Step(_layout, _selected, dx, dy);
        if (next == null)
            return;
        Select(next);
        if (view != null && _nodes.TryGetValue(next, out NodeView node))
            view.Reveal(node.Card);
    }

    /// <summary>The zoom (the buttons, Ctrl+=, Ctrl+- and Ctrl+0): a level in (1), out (-1) or back to 100 % (0), about the view's centre.</summary>
    public void Zoom(int direction)
    {
        if (view != null)
            view.Step(direction);
    }

    /// <summary>Enter (DesktopKeyboard) and the action button: the selected node's action.</summary>
    public void Act()
    {
        WorldState world = World;
        ContentLibrarySO lib = Library;
        UpgradeSO upgrade = lib != null && _selected != null ? lib.GetUpgradeById(_selected) : null;
        if (world == null || upgrade == null)
            return;

        switch (ActionOf(world, lib, upgrade, out _))
        {
            case NodeAction.Order:
                OrderBook.Order(world, lib, upgrade);
                break;
            case NodeAction.Cancel:
                OrderBook.Cancel(world, upgrade);
                break;
            case NodeAction.Install:
            case NodeAction.Keep:
                OrderBook.Install(world, lib, upgrade);
                break;
            case NodeAction.CancelInstall:
                UpgradeSO installed = lib.GetUpgradeById(Installs.InstalledIn(world.installs, upgrade.installSlot, world.unlockedUpgradeIds, SlotOf));
                OrderBook.Install(world, lib, installed);
                break;
        }
        Redraw();
    }

    /// <summary>Selects a node (a click on its card, or the arrows): the detail card shows it.</summary>
    public void Select(string upgradeId)
    {
        _selected = upgradeId;
        Redraw();
    }

    /// <summary>What a node's button does in its state.</summary>
    private enum NodeAction { None, Order, Cancel, Install, CancelInstall, Keep }

    /// <summary>The node's action and whether it can run now (an Order the wallet cannot cover, or a locked node, shows disabled).</summary>
    private static NodeAction ActionOf(WorldState world, ContentLibrarySO lib, UpgradeSO upgrade, out bool enabled)
    {
        enabled = true;
        switch (OrderBook.StateOf(world, lib, upgrade))
        {
            case OrderState.Orderable:
                return NodeAction.Order;
            case OrderState.TooDear:
            case OrderState.Locked:
                enabled = false;
                return NodeAction.Order;
            case OrderState.InTransit:
                enabled = Orders.CanCancel(world.orders, upgrade.id, world.day);
                return NodeAction.Cancel;
            default:
                switch (OrderBook.InstallStateOf(world, lib, upgrade))
                {
                    case InstallState.Stored: return NodeAction.Install;
                    case InstallState.InstallsTomorrow: return NodeAction.CancelInstall;
                    case InstallState.LeavesTomorrow: return NodeAction.Keep;
                    default: return NodeAction.None;
                }
        }
    }

    /// <summary>The action's plate in the UI kit (sheet 03 D3): Order oxblood, Install slate, Keep installed green, a cancel bone.</summary>
    private static string ActionPlate(NodeAction action) =>
        action switch
        {
            NodeAction.Order => "miniplate_ox",
            NodeAction.Install => "miniplate_slate",
            NodeAction.Keep => "plate_green",
            _ => "miniplate_bone"
        };

    /// <summary>The action button's label key.</summary>
    private static string ActionKey(NodeAction action) =>
        action switch
        {
            NodeAction.Order => "app.orders.order",
            NodeAction.Cancel => "app.orders.cancel",
            NodeAction.Install => "app.orders.install",
            NodeAction.CancelInstall => "app.orders.cancelInstall",
            NodeAction.Keep => "app.orders.keep",
            _ => null
        };

    /// <summary>Lays the tree out anew: a head per band, a card per node at its cell (its parts found here, once), three segments per link (dim and lit), the badges' glyphs, and the content's size; the view goes back to the tree's corner.</summary>
    private void Build()
    {
        foreach (GameObject go in _built)
            Destroy(go);
        _built.Clear();
        _nodes.Clear();
        _links.Clear();

        ContentLibrarySO lib = Library;
        if (lib == null || config == null || treeContent == null)
            return;

        _layout = OrderBook.Layout(lib);
        Vector2 cell = config.ordersCellSize, node = config.ordersNodeSize;
        float margin = (cell.x - node.x) / 2f, head = config.ordersBandHead;
        var bandTop = new float[_layout.Bands.Count];
        float y = margin;
        for (int b = 0; b < _layout.Bands.Count; b++)
        {
            TreeBand band = _layout.Bands[b];
            if (bandTemplate != null)
            {
                RectTransform h = Instantiate(bandTemplate, treeContent);
                h.name = "Band_" + band.Branch;
                Place(h, new Vector2(margin, y), new Vector2(Mathf.Max(1, _layout.Tiers) * cell.x - 2f * margin, head - 6f));
                SetText(h, "Label", OrderLines.Branch(band.Branch));
                SetImage(h, "Glyph", SlotArt.Sprite(ArtSlots.OrderBranch(band.Branch)) ?? Glyph("branch_" + ArtSlots.Key(band.Branch.ToString())));
                h.gameObject.SetActive(true);
                _built.Add(h.gameObject);
            }
            bandTop[b] = y + head;
            y += head + band.Slots * cell.y;
        }
        treeContent.sizeDelta = new Vector2(Mathf.Max(1, _layout.Tiers) * cell.x, y + margin);
        if (view != null)
            view.ShowCorner();

        var at = new Dictionary<string, Vector2>();
        foreach (TreeCell c in _layout.Cells)
            at[c.Id] = new Vector2(margin + c.Tier * cell.x, bandTop[c.Band] + c.Slot * cell.y + (cell.y - node.y) / 2f);

        foreach (TreeLink link in _layout.Links)
            AddLink(link.From, at[link.From], at[link.To], node, cell);

        foreach (TreeCell c in _layout.Cells)
        {
            UpgradeSO upgrade = lib.GetUpgradeById(c.Id);
            if (upgrade == null || nodeTemplate == null)
                continue;
            Button card = Instantiate(nodeTemplate, treeContent);
            card.name = "Node_" + c.Id;
            Place((RectTransform)card.transform, at[c.Id], node);
            SetText(card.transform, "Name", upgrade.displayName);
            Sprite art = SlotArt.Sprite(ArtSlots.OrderIcon(c.Id));
            SetImage(card.transform, "Glyph", art ?? Glyph("branch_" + ArtSlots.Key(c.Branch.ToString())));
            if (art != null)
                ShowUntinted(card.transform.Find("Glyph"));
            string id = c.Id;
            card.onClick.AddListener(() => Select(id));
            card.gameObject.SetActive(true);
            Transform selected = card.transform.Find("Selected");
            _nodes[id] = new NodeView
            {
                Card = (RectTransform)card.transform,
                Face = Child<Image>(card.transform, UiKitSO.FaceName),
                Glyph = Child<Image>(card.transform, "Glyph"),
                Branch = c.Branch,
                State = Child<TMP_Text>(card.transform, "State"),
                Badge = Child<Image>(card.transform, "Badge"),
                Group = card.GetComponent<CanvasGroup>(),
                Selected = selected != null ? selected.gameObject : null
            };
            _built.Add(card.gameObject);
        }
    }

    /// <summary>A link from a prerequisite's right edge to its dependant's left edge: out, across (bending halfway into the gap) and in, each segment twice (dim and lit).</summary>
    private void AddLink(string from, Vector2 a, Vector2 b, Vector2 node, Vector2 cell)
    {
        float w = config.ordersLinkWidth;
        float y1 = a.y + node.y / 2f, y2 = b.y + node.y / 2f;
        float x1 = a.x + node.x, x2 = b.x, bend = x2 - (cell.x - node.x) / 2f;
        var segments = new List<(Vector2 pos, Vector2 size)>
        {
            (new Vector2(x1, y1 - w / 2f), new Vector2(bend - x1 + w / 2f, w)),
            (new Vector2(bend - w / 2f, Mathf.Min(y1, y2) - w / 2f), new Vector2(w, Mathf.Abs(y2 - y1) + w)),
            (new Vector2(bend - w / 2f, y2 - w / 2f), new Vector2(x2 - bend + w / 2f, w)),
        };
        foreach ((Vector2 pos, Vector2 size) in segments)
        {
            if (size.x <= w && size.y <= w)
                continue;
            Image dim = Segment(linkTemplate, pos, size);
            Image lit = Segment(linkLitTemplate, pos, size);
            _links.Add((from, dim, lit));
        }
    }

    /// <summary>One cloned segment (null without its template).</summary>
    private Image Segment(Image template, Vector2 pos, Vector2 size)
    {
        if (template == null)
            return null;
        Image segment = Instantiate(template, treeContent);
        segment.name = "Link";
        Place(segment.rectTransform, pos, size);
        segment.transform.SetAsFirstSibling();
        _built.Add(segment.gameObject);
        return segment;
    }

    /// <summary>Redraws every node's state, the links, the wallet line and the detail card.</summary>
    private void Redraw()
    {
        _signature = Signature();
        WorldState world = World;
        ContentLibrarySO lib = Library;
        if (world == null || lib == null)
            return;

        if (walletText != null)
            walletText.text = OrderLines.Wallet(world);

        foreach (KeyValuePair<string, NodeView> pair in _nodes)
        {
            UpgradeSO upgrade = lib.GetUpgradeById(pair.Key);
            OrderState state = OrderBook.StateOf(world, lib, upgrade);
            NodeView node = pair.Value;
            if (node.State != null)
                node.State.text = OrderLines.State(world, lib, upgrade, state, false);
            if (kit != null)
            {
                kit.Show(node.Face, UiKitNames.UpgradeCard(state));
                // Sheet 03: the card's pictogram tile is its band's, the locked tile while locked.
                Sprite tile = kit.Get(UiKitNames.UpgradeTile(node.Branch, state));
                if (node.Glyph != null && tile != null)
                {
                    node.Glyph.sprite = tile;
                    node.Glyph.color = Color.white;
                }
            }
            string badgeName = UiKitNames.UpgradeBadge(state);
            Sprite badge = badgeName == null ? null : kit != null && kit.Get(badgeName) != null ? kit.Get(badgeName) : Glyph(badgeName.Substring(BadgePrefix.Length));
            if (node.Badge != null)
            {
                node.Badge.sprite = badge;
                node.Badge.gameObject.SetActive(badge != null);
            }
            if (node.Group != null)
                node.Group.alpha = state == OrderState.Locked ? lockedAlpha : 1f;
            if (node.Selected != null)
                node.Selected.SetActive(pair.Key == _selected);
        }

        foreach ((string from, Image dim, Image lit) in _links)
        {
            bool owned = world.HasUpgrade(from);
            if (dim != null)
                dim.gameObject.SetActive(!owned);
            if (lit != null)
                lit.gameObject.SetActive(owned);
        }

        DrawDetail(world, lib);
    }

    /// <summary>The detail card: the selected node's requisition form and its action; nothing selected, the hint.</summary>
    private void DrawDetail(WorldState world, ContentLibrarySO lib)
    {
        UpgradeSO upgrade = _selected != null ? lib.GetUpgradeById(_selected) : null;
        if (detailScroll != null)
            detailScroll.gameObject.SetActive(upgrade != null);
        if (selectText != null)
            selectText.gameObject.SetActive(upgrade == null);

        bool enabled = false;
        NodeAction action = upgrade != null ? ActionOf(world, lib, upgrade, out enabled) : NodeAction.None;
        if (actionButton != null)
        {
            actionButton.gameObject.SetActive(action != NodeAction.None);
            actionButton.interactable = enabled;
            if (_actionLabel != null && action != NodeAction.None)
                _actionLabel.text = UiText.Get(ActionKey(action));
            if (kit != null && action != NodeAction.None && actionButton.targetGraphic is Image face)
            {
                string plate = ActionPlate(action);
                kit.Show(face, plate, actionButton);
                if (_actionLabel != null)
                    _actionLabel.color = kit.InkOn(plate);
            }
        }
        if (upgrade == null || detail == null || detailForm == null)
            return;

        OrderState state = OrderBook.StateOf(world, lib, upgrade);
        FormData page = detailForm.Page(lib.Agency);
        _detailText.Clear();
        _detailText["item"] = upgrade.displayName;
        _detailText["section"] = OrderLines.Branch(upgrade.branch);
        _detailText["price"] = OrderLines.Money(OrderBook.Price(world, lib, upgrade));
        _detailText["status"] = OrderLines.State(world, lib, upgrade, state, true);
        _detailText["requires"] = OrderLines.Requires(world, lib, upgrade);
        _detailText["blurb"] = upgrade.description ?? string.Empty;
        page.Text = _detailText;
        detail.Show(detailForm.form, page, NothingPicks);
    }

    /// <summary>The zoom's line and buttons follow the view's level (the − button off at the lowest readable level, + at the highest).</summary>
    private void ShowZoom()
    {
        int level = view != null ? view.Level : AppZoom.Normal;
        if (zoomText != null)
            zoomText.text = UiText.Format("app.orders.zoom", level);
        if (zoomOutButton != null)
            zoomOutButton.interactable = view != null && view.CanStep(-1);
        if (zoomInButton != null)
            zoomInButton.interactable = view != null && view.CanStep(1);
    }

    /// <summary>The smallest a node's or a band's text may be drawn at 100 % (an auto-sized text's floor), for the readable zoom levels.</summary>
    private float SmallestText()
    {
        float smallest = float.MaxValue;
        foreach (Component template in new Component[] { nodeTemplate, bandTemplate })
            if (template != null)
                foreach (TMP_Text text in template.GetComponentsInChildren<TMP_Text>(true))
                    smallest = Mathf.Min(smallest, text.enableAutoSizing ? Mathf.Min(text.fontSizeMin, text.fontSizeMax) : text.fontSize);
        return smallest == float.MaxValue ? 0f : smallest;
    }

    /// <summary>An upgrade's install slot by id.</summary>
    private static string SlotOf(string id)
    {
        UpgradeSO upgrade = Library != null ? Library.GetUpgradeById(id) : null;
        return upgrade != null ? upgrade.installSlot ?? string.Empty : string.Empty;
    }

    /// <summary>What the drawing depends on, folded into one number (no allocation): the day, the wallet, the log, the owned upgrades, the installs.</summary>
    private static int Signature()
    {
        WorldState world = World;
        if (world == null)
            return 0;
        int h = world.day * 31 + world.money;
        h = h * 31 + world.orders.Count;
        foreach (OrderEntry e in world.orders)
            h = h * 31 + (e != null ? e.deliveredDay : 0);
        h = h * 31 + world.unlockedUpgradeIds.Count;
        foreach (InstallEntry e in world.installs)
            h = h * 31 + (e != null ? (e.installed ?? string.Empty).GetHashCode() * 7 + (e.next ?? string.Empty).GetHashCode() : 0);
        return h;
    }

    /// <summary>The kit's state badges' prefix: without the kit a badge is the code-drawn glyph of the rest of its name (tick, clock, padlock).</summary>
    private const string BadgePrefix = "roundbadge_";

    /// <summary>A code-drawn glyph (DesktopIconPlaceholder) as a sprite, made once per key.</summary>
    private Sprite Glyph(string key)
    {
        if (_glyphs.TryGetValue(key, out Sprite sprite))
            return sprite;
        byte[] rgba = DesktopIconPlaceholder.Render(key);
        if (rgba == null)
            return null;
        var texture = new Texture2D(DesktopIconPlaceholder.Size, DesktopIconPlaceholder.Size, TextureFormat.RGBA32, false)
        {
            name = "orders_" + key,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.LoadRawTextureData(rgba);
        texture.Apply(false, true);
        _glyphTextures.Add(texture);
        sprite = Sprite.Create(texture, new Rect(0f, 0f, DesktopIconPlaceholder.Size, DesktopIconPlaceholder.Size), new Vector2(0.5f, 0.5f));
        sprite.name = texture.name;
        _glyphs[key] = sprite;
        return sprite;
    }

    /// <summary>An upgrade's own icon shows in its colours (as Home's rows showed them): the clone's glyph drops its theme tint.</summary>
    private static void ShowUntinted(Transform glyph)
    {
        if (glyph == null)
            return;
        ThemeTag tag = glyph.GetComponent<ThemeTag>();
        if (tag != null)
            Destroy(tag);
        Image image = glyph.GetComponent<Image>();
        if (image != null)
            image.color = Color.white;
    }

    /// <summary>Puts a rect at a top-left position in the tree's content (y down) with a size.</summary>
    private static void Place(RectTransform rt, Vector2 topLeft, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
        rt.sizeDelta = size;
    }

    /// <summary>A named child's component (null without either).</summary>
    private static T Child<T>(Transform parent, string child) where T : Component
    {
        Transform t = parent.Find(child);
        return t != null ? t.GetComponent<T>() : null;
    }

    /// <summary>Writes a child text.</summary>
    private static void SetText(Transform parent, string child, string text)
    {
        TMP_Text label = Child<TMP_Text>(parent, child);
        if (label != null)
            label.text = text;
    }

    /// <summary>Sets a child image's sprite.</summary>
    private static void SetImage(Transform parent, string child, Sprite sprite)
    {
        Image image = Child<Image>(parent, child);
        if (image != null)
            image.sprite = sprite;
    }
}
