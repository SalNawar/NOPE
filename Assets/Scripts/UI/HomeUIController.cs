using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the Home phase panels (Phase 4): daily expenses + family condition,
/// the House (Home's upgrade tree: the office's upgrades are the PC's Orders
/// app's), slot machine, and the sleep prompt that hands off to the
/// next day. All references are optional; unwired panels are skipped so the
/// flow degrades gracefully (HomeManager just calls straight through).
/// Dynamic rows (family members) are spawned at runtime in their panel's own
/// ink (its body text's colour, as the scene draws the panel), so they read
/// on whatever the panel is; the House (the old shop panel) draws Home's
/// upgrade tree as cards on opaque plates with connectors and a detail strip
/// (the Home upgrades spec §6). A row or card shows its art when the file
/// exists (redesign phase 27): a family member's portrait for their
/// condition, an upgrade's icon.
/// </summary>
public sealed class HomeUIController : MonoBehaviour
{
    [Header("HUD (optional — null-safe)")]
    /// <summary>Shows current money.</summary>
    [SerializeField] private TMP_Text moneyText;

    /// <summary>Shows timeline stability.</summary>
    [SerializeField] private TMP_Text stabilityText;

    /// <summary>Shows the current day number.</summary>
    [SerializeField] private TMP_Text dayText;

    [Header("Expenses Panel")]
    /// <summary>Root of the expenses panel.</summary>
    [SerializeField] private GameObject expensesPanel;

    /// <summary>Expenses title ("Day 3 — Home").</summary>
    [SerializeField] private TMP_Text expensesTitleText;

    /// <summary>Expenses breakdown (rent, family upkeep, medical drain).</summary>
    [SerializeField] private TMP_Text expensesBodyText;

    /// <summary>Container for one row per family member (Treat buttons).</summary>
    [SerializeField] private Transform familyRowsRoot;

    /// <summary>Continues to the shop.</summary>
    [SerializeField] private Button expensesContinueButton;

    [Header("House Panel (the old shop panel)")]
    /// <summary>Root of the House panel (the scene's ShopPanel, its art panel_shop.png).</summary>
    [SerializeField] private GameObject shopPanel;

    /// <summary>The House panel's title.</summary>
    [SerializeField] private TMP_Text shopTitleText;

    /// <summary>The House panel's wallet line.</summary>
    [SerializeField] private TMP_Text shopBodyText;

    /// <summary>The tree's area: the category heads, connectors and cards are spawned into it (from its top-left).</summary>
    [SerializeField] private RectTransform houseTreeRoot;

    /// <summary>The detail strip: the selected upgrade's name, price, upkeep, blurb, effects and state.</summary>
    [SerializeField] private TMP_Text houseDetailText;

    /// <summary>Buys the selected upgrade (interactable only while it is buyable).</summary>
    [SerializeField] private Button houseBuyButton;

    /// <summary>Continues to the slot machine.</summary>
    [SerializeField] private Button shopContinueButton;

    [Header("Slot Panel")]
    /// <summary>Root of the slot machine panel.</summary>
    [SerializeField] private GameObject slotPanel;

    /// <summary>Slot title.</summary>
    [SerializeField] private TMP_Text slotTitleText;

    /// <summary>Slot result / status text.</summary>
    [SerializeField] private TMP_Text slotBodyText;

    /// <summary>Spins the slot machine.</summary>
    [SerializeField] private Button slotSpinButton;

    /// <summary>Continues to the sleep prompt.</summary>
    [SerializeField] private Button slotContinueButton;

    [Header("Sleep Panel")]
    /// <summary>Root of the sleep panel.</summary>
    [SerializeField] private GameObject sleepPanel;

    /// <summary>Sleep title.</summary>
    [SerializeField] private TMP_Text sleepTitleText;

    /// <summary>Sleep summary text.</summary>
    [SerializeField] private TMP_Text sleepBodyText;

    /// <summary>Ends the day and advances to tomorrow.</summary>
    [SerializeField] private Button sleepButton;

    /// <summary>Spawned family member rows (cleared/rebuilt on refresh).</summary>
    private readonly List<GameObject> _familyRows = new();

    /// <summary>Everything the House spawned (heads, connectors, cards; cleared and rebuilt on refresh).</summary>
    private readonly List<GameObject> _houseItems = new();

    /// <summary>The selected card's upgrade id: cleared when the House opens, kept when a purchase re-shows it.</summary>
    private string _selectedId;

    /// <summary>The run the House shows.</summary>
    private WorldState _houseWorld;

    /// <summary>The content library (prices and discounts).</summary>
    private ContentLibrarySO _houseLib;

    /// <summary>Home's upgrades the House shows.</summary>
    private IReadOnlyList<UpgradeSO> _houseUpgrades = Array.Empty<UpgradeSO>();

    /// <summary>The purchase callback for Buy.</summary>
    private Action<UpgradeSO> _onBuy;

    /// <summary>Pending callback for the expenses continue button.</summary>
    private Action _onExpensesContinue;

    /// <summary>Pending callback for the shop continue button.</summary>
    private Action _onShopContinue;

    /// <summary>Pending callback for the slot continue button.</summary>
    private Action _onSlotContinue;

    /// <summary>Pending callback for the sleep button.</summary>
    private Action _onSleep;

    /// <summary>True if the expenses panel and its continue button are wired, so it can be shown and left (audit R4-014: a panel without its button would strand the flow).</summary>
    public bool HasExpensesPanel => expensesPanel != null && expensesContinueButton != null;

    /// <summary>True if the shop panel and its continue button are wired, so it can be shown and left.</summary>
    public bool HasShopPanel => shopPanel != null && shopContinueButton != null;

    /// <summary>True if the slot panel and its continue button are wired, so it can be shown and left.</summary>
    public bool HasSlotPanel => slotPanel != null && slotContinueButton != null;

    /// <summary>True if the sleep panel is wired and can be shown.</summary>
    public bool HasSleepPanel => sleepPanel != null && sleepButton != null;

    /// <summary>Hides all panels on scene start and wires static buttons.</summary>
    private void Awake()
    {
        // A long Future currency name shrinks the money line instead of wrapping it (piece 6 R21).
        UiText.FitLabel(moneyText);

        if (expensesPanel != null) expensesPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (slotPanel != null) slotPanel.SetActive(false);
        if (sleepPanel != null) sleepPanel.SetActive(false);

        if (expensesContinueButton != null)
            expensesContinueButton.onClick.AddListener(HandleExpensesContinueClicked);

        if (shopContinueButton != null)
            shopContinueButton.onClick.AddListener(HandleShopContinueClicked);

        if (houseBuyButton != null)
            houseBuyButton.onClick.AddListener(HandleBuyClicked);

        if (slotContinueButton != null)
            slotContinueButton.onClick.AddListener(HandleSlotContinueClicked);

        if (sleepButton != null)
            sleepButton.onClick.AddListener(HandleSleepClicked);
    }

    /// <summary>Refreshes the money/stability/day HUD from world state.</summary>
    public void UpdateHud(WorldState world)
    {
        if (world == null)
            return;

        if (moneyText != null)
            moneyText.text = $"{UiText.Currency(UiText.WalletForm.Label)}: {world.money}";

        if (stabilityText != null)
            stabilityText.text = UiText.Format("tray.stability", StabilityRules.Format(world.timelineStability));

        if (dayText != null)
            dayText.text = $"Day {world.day}";
    }

    // =========================================================
    // Expenses panel
    // =========================================================

    /// <summary>
    /// Shows tonight's evening: a break-in first when there was one, the
    /// expense breakdown (rent and utilities, family upkeep, medical drain,
    /// the house's upkeep), the household's mood with its recovery chance,
    /// who got worse or better overnight, and one row per family member with
    /// a Treat button at <paramref name="careCost"/> (calls onTreat with the
    /// member's index). Invokes onContinue when the player moves on to the
    /// House, or the slot machine when <paramref name="shopNext"/> is false (or immediately if unwired); the continue button names the next step.
    /// </summary>
    public void ShowExpenses(WorldState world, HomeEconomy.Evening evening, GameConfigSO config, int careCost, Action<int> onTreat, Action onContinue, bool shopNext)
    {
        if (!HasExpensesPanel || world == null)
        {
            onContinue?.Invoke();
            return;
        }

        _onExpensesContinue = onContinue;
        TMP_Text continueLabel = expensesContinueButton.GetComponentInChildren<TMP_Text>(true);
        if (continueLabel != null)
            continueLabel.text = shopNext ? "Continue to the House" : "Continue to Slots";

        if (expensesTitleText != null)
            expensesTitleText.text = $"Day {world.day} — Home";

        if (expensesBodyText != null)
            expensesBodyText.text = ExpensesText(world, evening ?? new HomeEconomy.Evening());

        BuildFamilyRows(world, config, careCost, onTreat);

        expensesPanel.SetActive(true);
    }

    /// <summary>The expenses panel's body: the break-in, the bill's lines and total, the mood, and tonight's changes in the family.</summary>
    private static string ExpensesText(WorldState world, HomeEconomy.Evening evening)
    {
        HomeEconomy.ExpenseReport report = evening.bill;
        var sb = new StringBuilder();
        if (report.breakInLoss > 0)
            sb.AppendLine($"Someone broke in while you were at work: -{report.breakInLoss}");

        sb.AppendLine("Tonight's expenses:");
        sb.AppendLine($"  Rent & utilities: -{report.baseAmount}");

        if (report.memberCount > 0)
            sb.AppendLine($"  Family upkeep ({report.memberCount}): -{report.memberAmount}");

        if (report.conditionAmount > 0)
            sb.AppendLine($"  Medical drain: -{report.conditionAmount}");

        if (report.upkeepAmount > 0)
            sb.AppendLine($"  House upkeep: -{report.upkeepAmount}");

        sb.AppendLine($"Total: -{report.total} {UiText.Currency(UiText.WalletForm.Inline)}   (Balance: {world.money})");

        if (evening.mood > 0f)
            sb.AppendLine($"Household mood: {evening.mood:0.#} (a sick member recovers {evening.recoveryChance * 100f:0}% of nights)");

        if (evening.worse.Count > 0)
            sb.AppendLine($"Worse tonight: {string.Join(", ", evening.worse)}.");

        if (evening.better.Count > 0)
            sb.AppendLine($"Feeling better: {string.Join(", ", evening.better)}.");

        if (world.money < 0)
            sb.AppendLine("You are in debt. Find a way to make ends meet.");

        return sb.ToString();
    }

    /// <summary>Rebuilds the family member rows (the member's portrait for their condition when its art exists, ArtSlots.FamilyPortrait; name, condition, Treat button at <paramref name="careCost"/>).</summary>
    private void BuildFamilyRows(WorldState world, GameConfigSO config, int careCost, Action<int> onTreat)
    {
        if (familyRowsRoot == null)
            return;

        ClearRows(_familyRows);

        for (int i = 0; i < world.family.members.Count; i++)
        {
            FamilyMemberData member = world.family.members[i];

            if (member == null)
                continue;

            int capturedIndex = i;
            string label = $"{member.name} — condition {member.condition}";
            string buttonLabel = $"Treat (-{careCost})";
            bool interactable = member.condition > 0 && world.money >= careCost && onTreat != null;

            Sprite portrait = SlotArt.Sprite(ArtSlots.FamilyPortrait(member.name, member.condition, config != null ? config.maxFamilyCondition : 0));
            GameObject row = CreateRow(familyRowsRoot, label, buttonLabel, interactable,
                () => onTreat?.Invoke(capturedIndex), PanelInk(expensesBodyText), portrait);

            _familyRows.Add(row);
        }

        if (world.family.members.Count == 0)
            _familyRows.Add(CreateLabelRow(familyRowsRoot, "No family members on record.", PanelInk(expensesBodyText)));
    }

    /// <summary>Expenses continue clicked: close and move to the shop.</summary>
    private void HandleExpensesContinueClicked()
    {
        if (expensesPanel != null)
            expensesPanel.SetActive(false);

        OneShot.Fire(ref _onExpensesContinue);
    }

    // =========================================================
    // House panel (the old shop panel; the Home upgrades spec §6)
    // =========================================================

    /// <summary>
    /// Shows the House: <paramref name="upgrades"/> (Home's) as a tree, one
    /// column per category (UpgradeTree.Layout for the Home venue, each band
    /// on its side: a slot is a sub-column, a tier a row), a card per upgrade
    /// in its state (OrderBook.StateOf: Owned, Buyable, Too dear, Locked with
    /// "Needs ..."), connectors from each prerequisite down to its
    /// dependants, and a detail strip for the selected card (price, upkeep,
    /// blurb, effects in words, Buy). Invokes onBuy(upgrade) when Buy is
    /// clicked, onContinue when the player moves on to the slot machine (or
    /// immediately if unwired). The selection is kept while the panel is
    /// shown again after a purchase.
    /// </summary>
    public void ShowShop(WorldState world, ContentLibrarySO lib, IReadOnlyList<UpgradeSO> upgrades, Action<UpgradeSO> onBuy, Action onContinue)
    {
        if (!HasShopPanel || world == null)
        {
            onContinue?.Invoke();
            return;
        }

        _onShopContinue = onContinue;
        _houseWorld = world;
        _houseLib = lib;
        _houseUpgrades = upgrades ?? Array.Empty<UpgradeSO>();
        _onBuy = onBuy;
        if (!shopPanel.activeSelf)
            _selectedId = null;

        if (shopTitleText != null)
            shopTitleText.text = "House";

        if (shopBodyText != null)
            shopBodyText.text = $"{UiText.Currency(UiText.WalletForm.Label)}: {world.money}   ·   Bought tonight, in force from tomorrow night.";

        BuildHouse();
        shopPanel.SetActive(true);
    }

    /// <summary>Lays the tree out anew (heads, connectors, cards), keeps or picks the selection (the first buyable card, else the first) and fills the detail strip.</summary>
    private void BuildHouse()
    {
        ClearRows(_houseItems);
        if (houseTreeRoot == null)
            return;

        var byId = new Dictionary<string, UpgradeSO>(StringComparer.Ordinal);
        var nodes = new List<TreeNode>();
        foreach (UpgradeSO u in _houseUpgrades)
            if (u != null && !string.IsNullOrEmpty(u.id) && !byId.ContainsKey(u.id))
            {
                byId.Add(u.id, u);
                nodes.Add(u.Node);
            }

        TreeLayout layout = UpgradeTree.Layout(nodes, UpgradeVenue.Home);
        Color ink = PanelInk(shopBodyText);

        int slots = 0;
        foreach (TreeBand band in layout.Bands)
            slots += band.Slots;
        float width = houseTreeRoot.rect.width;
        float gaps = Mathf.Max(0, layout.Bands.Count - 1) * HouseBandGap + Mathf.Max(0, slots - layout.Bands.Count) * HouseSlotGap;
        float cardWidth = Mathf.Min(HouseCardMaxWidth, (width - gaps) / Mathf.Max(1, slots));
        float used = slots * cardWidth + gaps;

        var bandX = new float[layout.Bands.Count];
        float x = (width - used) / 2f;
        for (int b = 0; b < layout.Bands.Count; b++)
        {
            bandX[b] = x;
            float bandWidth = layout.Bands[b].Slots * cardWidth + (layout.Bands[b].Slots - 1) * HouseSlotGap;
            _houseItems.Add(HouseText(houseTreeRoot, "Head", layout.Bands[b].Branch.ToString().ToUpperInvariant(), HouseHeadFontSize, FontStyles.Bold, ink,
                                      TextAlignmentOptions.Center, new Vector2(x, 0f), new Vector2(bandWidth, HouseHeadHeight)));
            x += bandWidth + HouseBandGap;
        }

        var at = new Dictionary<string, Vector2>(StringComparer.Ordinal);
        foreach (TreeCell cell in layout.Cells)
            at[cell.Id] = new Vector2(bandX[cell.Band] + cell.Slot * (cardWidth + HouseSlotGap), HouseHeadHeight + cell.Tier * (HouseCardHeight + HouseRowGap));

        foreach (TreeLink link in layout.Links)
            AddConnector(at[link.From], at[link.To], cardWidth, _houseWorld.HasUpgrade(link.From));

        string firstBuyable = null;
        foreach (TreeCell cell in layout.Cells)
            if (firstBuyable == null && OrderBook.StateOf(_houseWorld, _houseLib, byId[cell.Id]) == OrderState.Orderable)
                firstBuyable = cell.Id;
        if (_selectedId == null || !byId.ContainsKey(_selectedId))
            _selectedId = firstBuyable ?? (layout.Cells.Count > 0 ? layout.Cells[0].Id : null);

        foreach (TreeCell cell in layout.Cells)
            AddCard(byId[cell.Id], at[cell.Id], cardWidth, byId);

        UpdateDetail(_selectedId != null && byId.TryGetValue(_selectedId, out UpgradeSO chosen) ? chosen : null, byId);
    }

    /// <summary>A card at <paramref name="topLeft"/> (tree space, y down): its plate in its state's colour (outlined when selected; a click selects it), its icon when the art exists (ArtSlots.UpgradeIcon), its name and its state line.</summary>
    private void AddCard(UpgradeSO upgrade, Vector2 topLeft, float width, Dictionary<string, UpgradeSO> byId)
    {
        OrderState state = OrderBook.StateOf(_houseWorld, _houseLib, upgrade);
        var card = new GameObject("Card_" + upgrade.id, typeof(RectTransform));
        card.transform.SetParent(houseTreeRoot, false);
        Place((RectTransform)card.transform, topLeft, new Vector2(width, HouseCardHeight));

        Image plate = card.AddComponent<Image>();
        plate.color = state == OrderState.Owned ? HouseOwnedFill : state == OrderState.Locked ? HouseLockedFill : state == OrderState.TooDear ? HouseTooDearFill : HouseBuyableFill;
        if (upgrade.id == _selectedId)
        {
            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = HouseSelectedOutline;
            outline.effectDistance = new Vector2(3f, -3f);
        }
        Button button = card.AddComponent<Button>();
        button.targetGraphic = plate;
        string id = upgrade.id;
        button.onClick.AddListener(() =>
        {
            _selectedId = id;
            BuildHouse();
        });

        float textLeft = HouseCardPadding;
        Sprite icon = SlotArt.Sprite(ArtSlots.UpgradeIcon(upgrade.id));
        if (icon != null)
        {
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(card.transform, false);
            Place((RectTransform)iconGo.transform, new Vector2(HouseCardPadding, (HouseCardHeight - HouseIconSize) / 2f), new Vector2(HouseIconSize, HouseIconSize));
            Image image = iconGo.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            textLeft += HouseIconSize + HouseCardPadding;
        }

        Color ink = state == OrderState.Locked ? HouseLockedInk : HouseCardInk;
        float textWidth = width - textLeft - HouseCardPadding;
        HouseText(card.transform, "Name", upgrade.displayName, HouseNameFontSize, FontStyles.Bold, ink, TextAlignmentOptions.BottomLeft,
                  new Vector2(textLeft, 4f), new Vector2(textWidth, HouseCardHeight / 2f));
        HouseText(card.transform, "State", StateLine(upgrade, state, byId), HouseStateFontSize, FontStyles.Normal, state == OrderState.TooDear ? HouseAlertInk : ink,
                  TextAlignmentOptions.TopLeft, new Vector2(textLeft, HouseCardHeight / 2f + 2f), new Vector2(textWidth, HouseCardHeight / 2f - 6f));
        _houseItems.Add(card);
    }

    /// <summary>A card's state line: "Owned", its price (and its upkeep a night), its price and "not enough", or "Needs ..." with the names of the prerequisites not owned yet.</summary>
    private string StateLine(UpgradeSO upgrade, OrderState state, Dictionary<string, UpgradeSO> byId)
    {
        string cr = UiText.Currency(UiText.WalletForm.Short);
        switch (state)
        {
            case OrderState.Owned:
                return "Owned";
            case OrderState.Locked:
                return "Needs " + string.Join(", ", UpgradeTree.Missing(upgrade.Node, _houseWorld.HasUpgrade).ConvertAll(need => byId.TryGetValue(need, out UpgradeSO u) ? u.displayName : need));
            case OrderState.TooDear:
                return $"{OrderBook.Price(_houseWorld, _houseLib, upgrade)} {cr} · not enough";
            default:
                float upkeep = Sum(upgrade, EffectOpType.Upkeep);
                return $"{OrderBook.Price(_houseWorld, _houseLib, upgrade)} {cr}" + (upkeep > 0f ? $" + {upkeep:0.##} a night" : string.Empty);
        }
    }

    /// <summary>A connector from a prerequisite's card at <paramref name="from"/> down to its dependant's at <paramref name="to"/> (tree space, y down): straight down in one sub-column, else down, across and down, bending in the row gap; dark once the prerequisite is owned, pale before.</summary>
    private void AddConnector(Vector2 from, Vector2 to, float cardWidth, bool lit)
    {
        Color colour = lit ? HouseLinkLit : HouseLinkDim;
        float ax = from.x + cardWidth / 2f, bx = to.x + cardWidth / 2f;
        float top = from.y + HouseCardHeight, bottom = to.y;
        float bend = bottom - HouseRowGap / 2f;
        if (Mathf.Abs(ax - bx) < 0.5f)
        {
            Segment(new Vector2(ax - HouseLinkWidth / 2f, top), new Vector2(HouseLinkWidth, bottom - top), colour);
            return;
        }
        Segment(new Vector2(ax - HouseLinkWidth / 2f, top), new Vector2(HouseLinkWidth, bend - top), colour);
        Segment(new Vector2(Mathf.Min(ax, bx) - HouseLinkWidth / 2f, bend - HouseLinkWidth / 2f), new Vector2(Mathf.Abs(bx - ax) + HouseLinkWidth, HouseLinkWidth), colour);
        Segment(new Vector2(bx - HouseLinkWidth / 2f, bend), new Vector2(HouseLinkWidth, bottom - bend), colour);
    }

    /// <summary>One connector segment: an untargeted plain image at <paramref name="topLeft"/> (tree space) drawn under the cards.</summary>
    private void Segment(Vector2 topLeft, Vector2 size, Color colour)
    {
        var go = new GameObject("Link", typeof(RectTransform));
        go.transform.SetParent(houseTreeRoot, false);
        Place((RectTransform)go.transform, topLeft, size);
        Image image = go.AddComponent<Image>();
        image.color = colour;
        image.raycastTarget = false;
        _houseItems.Add(go);
    }

    /// <summary>The detail strip for <paramref name="upgrade"/> (none: an empty strip): its name, price and upkeep, blurb, effects in words (HouseEffects.Line) and state; Buy only while it is buyable.</summary>
    private void UpdateDetail(UpgradeSO upgrade, Dictionary<string, UpgradeSO> byId)
    {
        OrderState state = upgrade != null ? OrderBook.StateOf(_houseWorld, _houseLib, upgrade) : OrderState.Locked;
        if (houseDetailText != null)
        {
            if (upgrade == null)
                houseDetailText.text = string.Empty;
            else
            {
                var ops = new List<(EffectOpType, float)>();
                if (upgrade.unlockEffect != null)
                    foreach (EffectOp op in upgrade.unlockEffect.ops)
                        if (op != null)
                            ops.Add((op.type, op.floatParam));
                houseDetailText.text = $"<b>{upgrade.displayName}</b>   {StateLine(upgrade, state, byId)}\n{upgrade.description}\n{HouseEffects.Line(ops)}";
            }
        }

        if (houseBuyButton != null)
            houseBuyButton.interactable = upgrade != null && state == OrderState.Orderable && _onBuy != null;
    }

    /// <summary>Buy clicked: hands the selected upgrade to the purchase callback (HomeManager re-shows the House after it).</summary>
    private void HandleBuyClicked()
    {
        foreach (UpgradeSO u in _houseUpgrades)
            if (u != null && u.id == _selectedId)
            {
                _onBuy?.Invoke(u);
                return;
            }
    }

    /// <summary>The summed <paramref name="op"/> of an upgrade's unlock effect (its upkeep a night, for the card).</summary>
    private static float Sum(UpgradeSO upgrade, EffectOpType op)
    {
        float sum = 0f;
        if (upgrade != null && upgrade.unlockEffect != null)
            foreach (EffectOp o in upgrade.unlockEffect.ops)
                if (o != null && o.type == op)
                    sum += o.floatParam;
        return sum;
    }

    /// <summary>Puts <paramref name="rt"/> at <paramref name="topLeft"/> of its parent (y down from the parent's top-left) with <paramref name="size"/>.</summary>
    private static void Place(RectTransform rt, Vector2 topLeft, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
        rt.sizeDelta = size;
    }

    /// <summary>An untargeted text at <paramref name="topLeft"/> of <paramref name="parent"/> (y down), shrinking rather than overflowing (UiText.FitLabel), never below the House's floor (20 reference px: 13 px at 720p; past it an ellipsis).</summary>
    private static GameObject HouseText(Transform parent, string name, string text, float size, FontStyles style, Color ink, TextAlignmentOptions alignment, Vector2 topLeft, Vector2 box)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, topLeft, box);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = ink;
        label.alignment = alignment;
        label.raycastTarget = false;
        UiText.FitLabel(label);
        label.fontSizeMin = Mathf.Max(label.fontSizeMin, HouseMinFontSize);
        label.overflowMode = TextOverflowModes.Ellipsis;
        return go;
    }

    /// <summary>Shop continue clicked: close and move to the slot machine.</summary>
    private void HandleShopContinueClicked()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);

        OneShot.Fire(ref _onShopContinue);
    }

    // =========================================================
    // Slot panel
    // =========================================================

    /// <summary>
    /// Shows the slot machine. onSpin is invoked when the player clicks Spin and
    /// should return the result line to display (or null/empty if the spin was
    /// rejected, e.g. insufficient credits). onContinue is invoked when the
    /// player moves on to sleep (or immediately if unwired).
    /// </summary>
    public void ShowSlot(WorldState world, GameConfigSO config, Func<string> onSpin, Action onContinue)
    {
        if (!HasSlotPanel || world == null)
        {
            onContinue?.Invoke();
            return;
        }

        _onSlotContinue = onContinue;

        if (slotTitleText != null)
            slotTitleText.text = "Slot Machine";

        int spinCost = config != null ? config.slotSpinCost : 0;

        if (slotBodyText != null)
            slotBodyText.text = $"Spin for {spinCost} {UiText.Currency(UiText.WalletForm.Inline)}. Try your luck for tomorrow's shift.";

        if (slotSpinButton != null)
        {
            slotSpinButton.onClick.RemoveAllListeners();
            slotSpinButton.onClick.AddListener(() =>
            {
                string result = onSpin != null ? onSpin.Invoke() : null;

                if (slotBodyText != null && !string.IsNullOrEmpty(result))
                    slotBodyText.text = result;
            });
        }

        slotPanel.SetActive(true);
    }

    /// <summary>Slot continue clicked: close and move to the sleep prompt.</summary>
    private void HandleSlotContinueClicked()
    {
        if (slotPanel != null)
            slotPanel.SetActive(false);

        OneShot.Fire(ref _onSlotContinue);
    }

    // =========================================================
    // Sleep panel
    // =========================================================

    /// <summary>
    /// Shows the sleep prompt, with <paramref name="radioLine"/> ("On the
    /// radio: ...") when the house owns the radio. Invokes onSleep when the
    /// player clicks Sleep (or immediately if unwired) — HomeManager then
    /// advances to the next day.
    /// </summary>
    public void ShowSleep(WorldState world, string radioLine, Action onSleep)
    {
        if (!HasSleepPanel || world == null)
        {
            onSleep?.Invoke();
            return;
        }

        _onSleep = onSleep;

        if (sleepTitleText != null)
            sleepTitleText.text = $"Day {world.day} — Turn In";

        if (sleepBodyText != null)
            sleepBodyText.text = "Get some rest. Tomorrow's briefing will reflect tonight's choices."
                                 + (string.IsNullOrEmpty(radioLine) ? string.Empty : $"\n\nOn the radio: \"{radioLine}\"");

        sleepPanel.SetActive(true);
    }

    /// <summary>Sleep clicked: close and advance to the next day.</summary>
    private void HandleSleepClicked()
    {
        if (sleepPanel != null)
            sleepPanel.SetActive(false);

        OneShot.Fire(ref _onSleep);
    }

    // =========================================================
    // Runtime row helpers
    // =========================================================

    /// <summary>Destroys previously spawned rows and clears the tracking list.</summary>
    private static void ClearRows(List<GameObject> rows)
    {
        for (int i = 0; i < rows.Count; i++)
            if (rows[i] != null)
                Destroy(rows[i]);

        rows.Clear();
    }

    /// <summary>A panel's own ink: its body text's colour (the scene's choice for that panel), white without a body text.</summary>
    private static Color PanelInk(TMP_Text body) => body != null ? body.color : Color.white;

    // The runtime rows' layout (the family, shop and empty-state rows; audit R4-015).
    private const float LabelRowHeight = 36f;
    private const float RowHeight = 44f;
    private const float RowSpacing = 12f;
    private const float RowFontSize = 22f;
    private const float RowButtonWidth = 160f;
    private const float RowButtonHeight = 40f;
    private const float RowButtonFontSize = 20f;
    private static readonly Color RowButtonFill = new Color(0.95f, 0.95f, 0.95f, 1f);
    private static readonly Color RowButtonInk = Color.black;

    // The House's layout (reference px) and colours: opaque plates under dark ink.
    private const float HouseHeadHeight = 36f;
    private const float HouseHeadFontSize = 24f;
    private const float HouseCardHeight = 96f;
    private const float HouseCardMaxWidth = 300f;
    private const float HouseCardPadding = 10f;
    private const float HouseIconSize = 64f;
    private const float HouseNameFontSize = 26f;
    private const float HouseStateFontSize = 24f;
    private const float HouseMinFontSize = 20f;
    private const float HouseRowGap = 26f;
    private const float HouseSlotGap = 14f;
    private const float HouseBandGap = 36f;
    private const float HouseLinkWidth = 4f;
    private static readonly Color HouseBuyableFill = new Color(1f, 0.98f, 0.93f, 1f);
    private static readonly Color HouseOwnedFill = new Color(0.78f, 0.89f, 0.76f, 1f);
    private static readonly Color HouseTooDearFill = new Color(0.97f, 0.9f, 0.86f, 1f);
    private static readonly Color HouseLockedFill = new Color(0.82f, 0.82f, 0.82f, 1f);
    private static readonly Color HouseCardInk = new Color(0.12f, 0.12f, 0.15f, 1f);
    private static readonly Color HouseLockedInk = new Color(0.25f, 0.25f, 0.28f, 1f);
    private static readonly Color HouseAlertInk = new Color(0.62f, 0.08f, 0.08f, 1f);
    private static readonly Color HouseSelectedOutline = new Color(0.13f, 0.3f, 0.5f, 1f);
    private static readonly Color HouseLinkLit = new Color(0.2f, 0.32f, 0.45f, 1f);
    private static readonly Color HouseLinkDim = new Color(0.6f, 0.6f, 0.62f, 1f);

    /// <summary>Creates a label-only row (no button) in <paramref name="ink"/> — used for empty-state messages.</summary>
    private static GameObject CreateLabelRow(Transform parent, string label, Color ink)
    {
        var row = new GameObject("Row_Label", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        var layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = LabelRowHeight;
        layout.flexibleWidth = 1f;

        // The rows containers do not control their children's heights: the row takes its own.
        ((RectTransform)row.transform).sizeDelta = new Vector2(0f, layout.preferredHeight);

        var text = row.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = RowFontSize;
        text.color = ink;
        text.alignment = TextAlignmentOptions.MidlineLeft;

        return row;
    }

    /// <summary>Creates a row with <paramref name="icon"/> (when given, a square the row's height) and a label in <paramref name="ink"/> on the left and a button on the right.</summary>
    private static GameObject CreateRow(Transform parent, string label, string buttonLabel, bool buttonInteractable, Action onClick, Color ink, Sprite icon = null)
    {
        var row = new GameObject("Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        var rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = RowHeight;
        rowLayout.flexibleWidth = 1f;

        // The rows containers do not control their children's heights: the row takes its own.
        ((RectTransform)row.transform).sizeDelta = new Vector2(0f, rowLayout.preferredHeight);

        var hLayout = row.AddComponent<HorizontalLayoutGroup>();
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = true;
        hLayout.spacing = RowSpacing;
        hLayout.childAlignment = TextAnchor.MiddleLeft;

        // Icon (the row's art slot; none without art).
        if (icon != null)
        {
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(row.transform, false);
            var iconLayout = iconGo.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = rowLayout.preferredHeight;
            iconLayout.flexibleWidth = 0f;
            Image iconImage = iconGo.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        // Label.
        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(row.transform, false);

        var labelLayout = labelGo.AddComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;

        var labelText = labelGo.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = RowFontSize;
        labelText.color = ink;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        UiText.FitLabel(labelText); // a long currency name shrinks the price instead of wrapping

        // Button.
        var buttonGo = new GameObject("Button", typeof(RectTransform));
        buttonGo.transform.SetParent(row.transform, false);

        var buttonLayout = buttonGo.AddComponent<LayoutElement>();
        buttonLayout.preferredWidth = RowButtonWidth;
        buttonLayout.preferredHeight = RowButtonHeight;

        Image img = buttonGo.AddComponent<Image>();
        img.color = RowButtonFill;

        Button btn = buttonGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.interactable = buttonInteractable;

        var btnLabelGo = new GameObject("Label", typeof(RectTransform));
        btnLabelGo.transform.SetParent(buttonGo.transform, false);

        var btnLabelRt = (RectTransform)btnLabelGo.transform;
        btnLabelRt.anchorMin = Vector2.zero;
        btnLabelRt.anchorMax = Vector2.one;
        btnLabelRt.offsetMin = Vector2.zero;
        btnLabelRt.offsetMax = Vector2.zero;

        var btnLabelText = btnLabelGo.AddComponent<TextMeshProUGUI>();
        btnLabelText.text = buttonLabel;
        btnLabelText.fontSize = RowButtonFontSize;
        btnLabelText.alignment = TextAlignmentOptions.Center;
        btnLabelText.color = RowButtonInk;

        if (onClick != null)
            btn.onClick.AddListener(() => onClick());

        return row;
    }
}
