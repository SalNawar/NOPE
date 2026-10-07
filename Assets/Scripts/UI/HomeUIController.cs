using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Owns the Home phase panels (Phase 4; the Home pet spec): the bills (the
/// fixed costs paid, the pet's needs in words, and the night's optional
/// bills, each a row with its Paying / Skip pair), the pet's corner (the pet
/// drawn by PetStandIn, petted, played with a toy), the House (Home's upgrade
/// tree: the office's upgrades are the PC's Orders app's), slot machine (three
/// reels and the SPIN dome), and the sleep prompt that hands off to the next
/// day. All references are optional; unwired panels are skipped so the flow
/// degrades gracefully (HomeManager just calls straight through). Everything
/// it spawns at runtime (the bills' and toys' rows, the House's heads, links
/// and cards, the reels' faces) is drawn in the cel UI kit (docs/UI_KIT.md;
/// <see cref="kit"/>): kit plates with live TMP labels in the kit's fonts, so
/// the language settings keep working; without the kit they stay plain. A
/// card shows its art when the file exists (redesign phase 27): an upgrade's
/// icon; the pet its picture (ArtSlots.PetSprite); a toy its picture
/// (ArtSlots.PetToy), else the kit's tile.
/// </summary>
public sealed class HomeUIController : MonoBehaviour
{
    [Header("The UI kit")]
    /// <summary>The cel UI kit (Assets/Data/UI/UiKit_Default.asset): the sprites and fonts the spawned rows, cards and reels are drawn with.</summary>
    [SerializeField] private UiKitSO kit;

    [Header("HUD (optional — null-safe)")]
    /// <summary>Shows current money (on the HUD's phosphor readout).</summary>
    [SerializeField] private TMP_Text moneyText;

    /// <summary>Shows the current day number (on the HUD's phosphor readout).</summary>
    [SerializeField] private TMP_Text dayText;

    [Header("Expenses Panel")]
    /// <summary>Root of the expenses panel.</summary>
    [SerializeField] private GameObject expensesPanel;

    /// <summary>Expenses title ("Day 3 — Home").</summary>
    [SerializeField] private TMP_Text expensesTitleText;

    /// <summary>The fixed costs paid, the pet's needs in words, and the night's bills' total.</summary>
    [SerializeField] private TMP_Text expensesBodyText;

    /// <summary>Container for one row per night's bill (its Paying / Skip pair).</summary>
    [FormerlySerializedAs("familyRowsRoot")]
    [SerializeField] private Transform billRowsRoot;

    /// <summary>Pays the chosen bills and goes on to the pet's corner.</summary>
    [SerializeField] private Button expensesContinueButton;

    [Header("Pet corner (the Home pet spec PS7)")]
    /// <summary>Root of the pet's corner.</summary>
    [SerializeField] private GameObject petPanel;

    /// <summary>The corner's title ("Biscuit's corner").</summary>
    [SerializeField] private TMP_Text petTitleText;

    /// <summary>The pet's needs after tonight's care, in words.</summary>
    [SerializeField] private TMP_Text petBodyText;

    /// <summary>The pet itself (its art, else the code-drawn stand-in).</summary>
    [SerializeField] private PetStandIn petView;

    /// <summary>What the pet just did (a pat's or a toy's line).</summary>
    [SerializeField] private TMP_Text petReactionText;

    /// <summary>Pets the pet (a reaction, as often as the player likes).</summary>
    [SerializeField] private Button petPatButton;

    /// <summary>Container for one row per owned toy (Play, once a night).</summary>
    [SerializeField] private Transform toyRowsRoot;

    /// <summary>Continues to the House (or the slot machine).</summary>
    [SerializeField] private Button petContinueButton;

    [Header("House Panel (the old shop panel)")]
    /// <summary>Root of the House panel (the scene's ShopPanel).</summary>
    [SerializeField] private GameObject shopPanel;

    /// <summary>The House panel's title.</summary>
    [SerializeField] private TMP_Text shopTitleText;

    /// <summary>The House panel's wallet line.</summary>
    [SerializeField] private TMP_Text shopBodyText;

    /// <summary>The tree's area: the category heads, connectors and cards are spawned into it (from its top-left).</summary>
    [SerializeField] private RectTransform houseTreeRoot;

    /// <summary>The detail card's tile: the selected upgrade's icon (its art, else its category's kit tile).</summary>
    [SerializeField] private Image houseDetailIcon;

    /// <summary>The detail card: the selected upgrade's name, price, upkeep, blurb, effects and state.</summary>
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

    /// <summary>The three reels' symbols (a kit tile each, in its reel window): spun and landed on the outcome's faces (SlotReels).</summary>
    [SerializeField] private Image[] slotReelFaces = Array.Empty<Image>();

    [Header("Sleep Panel")]
    /// <summary>Root of the sleep panel.</summary>
    [SerializeField] private GameObject sleepPanel;

    /// <summary>Sleep title.</summary>
    [SerializeField] private TMP_Text sleepTitleText;

    /// <summary>Sleep summary text.</summary>
    [SerializeField] private TMP_Text sleepBodyText;

    /// <summary>Ends the day and advances to tomorrow.</summary>
    [SerializeField] private Button sleepButton;

    /// <summary>Spawned bill rows (cleared/rebuilt on refresh).</summary>
    private readonly List<GameObject> _billRows = new();

    /// <summary>Each bill's Paying side as last shown (a toggle's re-show slides the pair's pill across from the side it left: UiPill).</summary>
    private readonly Dictionary<HomeBill, bool> _billPaying = new();

    /// <summary>Spawned toy rows (cleared/rebuilt on refresh).</summary>
    private readonly List<GameObject> _toyRows = new();

    /// <summary>Pending callback for the pet corner's continue button.</summary>
    private Action _onPetContinue;

    /// <summary>The pat callback (returns the reaction's line).</summary>
    private Func<string> _onPat;

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

    /// <summary>The reels' spin while it runs (a new spin stops it first).</summary>
    private Coroutine _reelSpin;

    /// <summary>True if the expenses panel and its continue button are wired, so it can be shown and left (audit R4-014: a panel without its button would strand the flow).</summary>
    public bool HasExpensesPanel => expensesPanel != null && expensesContinueButton != null;

    /// <summary>True if the pet's corner and its continue button are wired, so it can be shown and left.</summary>
    public bool HasPetPanel => petPanel != null && petContinueButton != null;

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
        if (petPanel != null) petPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (slotPanel != null) slotPanel.SetActive(false);
        if (sleepPanel != null) sleepPanel.SetActive(false);

        if (expensesContinueButton != null)
            expensesContinueButton.onClick.AddListener(HandleExpensesContinueClicked);

        if (petContinueButton != null)
            petContinueButton.onClick.AddListener(HandlePetContinueClicked);

        if (petPatButton != null)
            petPatButton.onClick.AddListener(HandlePatClicked);

        if (shopContinueButton != null)
            shopContinueButton.onClick.AddListener(HandleShopContinueClicked);

        if (houseBuyButton != null)
            houseBuyButton.onClick.AddListener(HandleBuyClicked);

        if (slotContinueButton != null)
            slotContinueButton.onClick.AddListener(HandleSlotContinueClicked);

        if (sleepButton != null)
            sleepButton.onClick.AddListener(HandleSleepClicked);
    }

    /// <summary>Refreshes the money and day readouts from world state (the timeline's stability is never a number: the Helix River shows it).</summary>
    public void UpdateHud(WorldState world)
    {
        if (world == null)
            return;

        if (moneyText != null)
            moneyText.text = $"{world.money} {UiText.Currency(UiText.WalletForm.Short)}";

        if (dayText != null)
            dayText.text = $"Day {world.day}";
    }

    // =========================================================
    // Expenses panel
    // =========================================================

    /// <summary>One of the night's bills as its row shows it: the bill, its name, its price and line, whether tonight's care pays it, and whether its pair takes clicks.</summary>
    public readonly struct BillView
    {
        /// <summary>The bill.</summary>
        public readonly HomeBill Bill;

        /// <summary>The bill's name.</summary>
        public readonly string Name;

        /// <summary>The line under the name: its price and note.</summary>
        public readonly string Detail;

        /// <summary>Whether tonight's care pays it (the pair's Paying side is on).</summary>
        public readonly bool Paying;

        /// <summary>Whether the pair takes clicks.</summary>
        public readonly bool Enabled;

        /// <summary>A row.</summary>
        public BillView(HomeBill bill, string name, string detail, bool paying, bool enabled)
        {
            Bill = bill;
            Name = name;
            Detail = detail;
            Paying = paying;
            Enabled = enabled;
        }
    }

    /// <summary>
    /// Shows the bills step (the Home pet spec PS2): <paramref name="title"/>,
    /// <paramref name="body"/> (the break-in, the fixed costs paid, the pet's
    /// needs in words, the night's bills' total), one row per bill in
    /// <paramref name="rows"/> with its <paramref name="payingLabel"/> /
    /// <paramref name="skipLabel"/> pair (the side that is off calls
    /// <paramref name="onToggle"/> with the bill), and the continue button,
    /// labelled <paramref name="payLabel"/>, taking clicks only while
    /// <paramref name="canPay"/>, calling <paramref name="onPay"/> (or
    /// immediately if unwired). HomeManager shows it again after each toggle.
    /// </summary>
    public void ShowExpenses(string title, string body, IReadOnlyList<BillView> rows, string payingLabel, string skipLabel, string payLabel, bool canPay,
                             Action<HomeBill> onToggle, Action onPay)
    {
        if (!HasExpensesPanel)
        {
            onPay?.Invoke();
            return;
        }

        _onExpensesContinue = onPay;
        TMP_Text continueLabel = expensesContinueButton.GetComponentInChildren<TMP_Text>(true);
        if (continueLabel != null)
            continueLabel.text = payLabel;
        expensesContinueButton.interactable = canPay;

        if (expensesTitleText != null)
            expensesTitleText.text = title;

        if (expensesBodyText != null)
            expensesBodyText.text = body;

        if (billRowsRoot != null)
        {
            ClearRows(_billRows);
            foreach (BillView row in rows ?? Array.Empty<BillView>())
            {
                HomeBill bill = row.Bill;
                _billRows.Add(CreateBillRow(row, payingLabel, skipLabel, onToggle != null ? () => onToggle(bill) : null));
            }
        }

        expensesPanel.SetActive(true);
    }

    /// <summary>Expenses continue clicked: close and move on (HomeManager pays the bills first).</summary>
    private void HandleExpensesContinueClicked()
    {
        if (expensesPanel != null)
            expensesPanel.SetActive(false);

        OneShot.Fire(ref _onExpensesContinue);
    }

    // =========================================================
    // Pet corner (the Home pet spec PS7)
    // =========================================================

    /// <summary>One owned toy as its row shows it: its id (its picture), its label, its button's label, whether it takes clicks, and what playing does.</summary>
    public readonly struct ToyView
    {
        /// <summary>The toy's upgrade id (its picture, ArtSlots.PetToy, else the kit's tile named in it).</summary>
        public readonly string Id;

        /// <summary>The row's label (the toy's name).</summary>
        public readonly string Label;

        /// <summary>The button's label ("Play" or "Played tonight").</summary>
        public readonly string Button;

        /// <summary>Whether the button takes clicks (once a night).</summary>
        public readonly bool Enabled;

        /// <summary>Plays with it (HomeManager shows the corner again with the pet's line).</summary>
        public readonly Action OnPlay;

        /// <summary>A row.</summary>
        public ToyView(string id, string label, string button, bool enabled, Action onPlay)
        {
            Id = id;
            Label = label;
            Button = button;
            Enabled = enabled;
            OnPlay = onPlay;
        }
    }

    /// <summary>
    /// Shows the pet's corner: <paramref name="title"/>, the pet's needs after
    /// tonight's care in words (<paramref name="body"/>), the pet
    /// (<paramref name="kind"/> looking <paramref name="look"/>, the room
    /// dark unless <paramref name="lit"/>), <paramref name="reaction"/> (what
    /// it just did), the pat button (<paramref name="patLabel"/>: a hop and
    /// a heart, and <paramref name="onPat"/>'s line), one row per owned toy
    /// (<paramref name="toys"/>; <paramref name="noToys"/> without one), and
    /// the continue button, labelled <paramref name="continueLabel"/>, calling
    /// <paramref name="onContinue"/> (or immediately if unwired). Shown again
    /// after a toy (<paramref name="played"/>), the pet spins.
    /// </summary>
    public void ShowPet(string title, string body, PetKind kind, PetLook look, bool lit, string reaction, string patLabel, Func<string> onPat,
                        IReadOnlyList<ToyView> toys, string noToys, string continueLabel, Action onContinue, bool played = false)
    {
        if (!HasPetPanel)
        {
            onContinue?.Invoke();
            return;
        }

        _onPetContinue = onContinue;
        _onPat = onPat;
        TMP_Text next = petContinueButton.GetComponentInChildren<TMP_Text>(true);
        if (next != null)
            next.text = continueLabel;
        if (petTitleText != null)
            petTitleText.text = title;
        if (petBodyText != null)
            petBodyText.text = body;
        if (petReactionText != null)
            petReactionText.text = reaction ?? string.Empty;
        if (petPatButton != null)
        {
            TMP_Text pat = petPatButton.GetComponentInChildren<TMP_Text>(true);
            if (pat != null)
                pat.text = patLabel;
        }

        if (toyRowsRoot != null)
        {
            ClearRows(_toyRows);
            if (toys == null || toys.Count == 0)
                _toyRows.Add(CreateLabelRow(toyRowsRoot, noToys, PanelInk(petBodyText)));
            else
                foreach (ToyView toy in toys)
                    _toyRows.Add(CreateToyRow(toy));
        }

        bool opening = !petPanel.activeSelf;
        petPanel.SetActive(true);
        if (petView != null)
        {
            if (opening || played)
                petView.Show(kind, look, lit);
            if (played)
                petView.Play();
        }
    }

    /// <summary>Pat clicked: the pet hops with a heart and its reaction line shows.</summary>
    private void HandlePatClicked()
    {
        if (petView != null)
            petView.Pat();
        string line = _onPat != null ? _onPat.Invoke() : null;
        if (petReactionText != null && !string.IsNullOrEmpty(line))
            petReactionText.text = line;
    }

    /// <summary>The pet corner's continue clicked: close and move on.</summary>
    private void HandlePetContinueClicked()
    {
        if (petPanel != null)
            petPanel.SetActive(false);

        OneShot.Fire(ref _onPetContinue);
    }

    // =========================================================
    // House panel (the old shop panel; the Home upgrades spec §6)
    // =========================================================

    /// <summary>
    /// Shows the House: <paramref name="upgrades"/> (Home's) as a tree, one
    /// column per category under its slate head (UpgradeTree.Layout for the
    /// Home venue, each band on its side: a slot is a sub-column, a tier a
    /// row), a kit upgrade card per upgrade in its state (OrderBook.StateOf:
    /// owned with the tick, buyable, too dear with the clock, locked with the
    /// padlock and "Needs ..."), links from each prerequisite down to its
    /// dependants (brass once it is owned, dashed before), and the detail card
    /// for the selected card (price, upkeep, blurb, effects in words, Buy).
    /// Invokes onBuy(upgrade) when Buy is clicked, onContinue when the player
    /// moves on to the slot machine (or immediately if unwired). The selection
    /// is kept while the panel is shown again after a purchase.
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
            shopTitleText.text = "The House";

        if (shopBodyText != null)
            shopBodyText.text = $"{UiText.Currency(UiText.WalletForm.Label)}: {world.money}   ·   Bought tonight, in force from tomorrow night.";

        shopPanel.SetActive(true);
        BuildHouse();
    }

    /// <summary>Lays the tree out anew (heads, connectors, cards), keeps or picks the selection (the first buyable card, else the first) and fills the detail card.</summary>
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
            _houseItems.Add(BandHead(layout.Bands[b].Branch, new Vector2(x, 0f), bandWidth));
            x += bandWidth + HouseBandGap;
        }

        var at = new Dictionary<string, Vector2>(StringComparer.Ordinal);
        foreach (TreeCell cell in layout.Cells)
            at[cell.Id] = new Vector2(bandX[cell.Band] + cell.Slot * (cardWidth + HouseSlotGap), HouseHeadHeight + HouseHeadGap + cell.Tier * (HouseCardHeight + HouseRowGap));

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

    /// <summary>A category's head over its column at <paramref name="topLeft"/> (tree space, y down): the kit's slate title bar with the category's tile and its name.</summary>
    private GameObject BandHead(UpgradeBranch branch, Vector2 topLeft, float width)
    {
        var head = new GameObject("Head_" + branch, typeof(RectTransform));
        head.transform.SetParent(houseTreeRoot, false);
        Place((RectTransform)head.transform, topLeft, new Vector2(width, HouseHeadHeight));
        Image face = Face(head.transform, "titlebar_slate");
        if (face == null)
            face = head.AddComponent<Image>();
        face.raycastTarget = false;

        float left = HouseHeadPad;
        Sprite tile = Kit("tile_" + BranchTile(branch) + "_rest");
        if (tile != null)
        {
            Tile(head.transform, tile, new Vector2(left, (HouseHeadHeight - HouseHeadTile) / 2f), HouseHeadTile);
            left += HouseHeadTile + HouseHeadPad;
        }
        Text(head.transform, "Name", branch.ToString(), HouseHeadFontSize, LabelInk(true), TextAlignmentOptions.MidlineLeft, new Vector2(left, 0f),
             new Vector2(width - left - HouseHeadPad, HouseHeadHeight), true);
        return head;
    }

    /// <summary>
    /// A card at <paramref name="topLeft"/> (tree space, y down): the kit's
    /// upgrade card in its state (a click selects it; the selected card wears
    /// a brass outline), its icon tile (the upgrade's art when it exists,
    /// ArtSlots.UpgradeIcon, else its category's kit tile), its name, its
    /// state line and its state's round badge.
    /// </summary>
    private void AddCard(UpgradeSO upgrade, Vector2 topLeft, float width, Dictionary<string, UpgradeSO> byId)
    {
        OrderState state = OrderBook.StateOf(_houseWorld, _houseLib, upgrade);
        var card = new GameObject("Card_" + upgrade.id, typeof(RectTransform));
        card.transform.SetParent(houseTreeRoot, false);
        Place((RectTransform)card.transform, topLeft, new Vector2(width, HouseCardHeight));

        Image plate = Face(card.transform, UiKitNames.UpgradeCard(state));
        if (plate == null)
        {
            plate = card.AddComponent<Image>();
            plate.color = state == OrderState.Locked ? HouseLockedFill : HouseBuyableFill;
        }
        if (upgrade.id == _selectedId)
        {
            Outline outline = plate.gameObject.AddComponent<Outline>();
            outline.effectColor = HouseSelectedOutline;
            outline.effectDistance = new Vector2(3f, -3f);
        }
        Button button = card.AddComponent<Button>();
        button.targetGraphic = plate;
        button.transition = Selectable.Transition.ColorTint;
        string id = upgrade.id;
        button.onClick.AddListener(() =>
        {
            _selectedId = id;
            BuildHouse();
        });

        float textLeft = HouseCardPadding;
        Sprite icon = UpgradeTile(upgrade, state == OrderState.Locked);
        if (icon != null)
        {
            Tile(card.transform, icon, new Vector2(HouseCardPadding, (HouseCardHeight - HouseIconSize) / 2f), HouseIconSize);
            textLeft += HouseIconSize + HouseCardPadding;
        }

        Color ink = state == OrderState.Locked ? HouseLockedInk : HouseCardInk;
        float textWidth = width - textLeft - HouseCardPadding - HouseBadgeRoom;
        TMP_Text name = Text(card.transform, "Name", upgrade.displayName, HouseNameFontSize, ink, TextAlignmentOptions.BottomLeft,
                             new Vector2(textLeft, 6f), new Vector2(textWidth, HouseNameHeight), true, true);
        name.characterSpacing = HouseNameSpacing;
        name.lineSpacing = -12f;
        Text(card.transform, "State", StateLine(upgrade, state, byId, false), HouseStateFontSize, StateInk(state),
             TextAlignmentOptions.TopLeft, new Vector2(textLeft, 8f + HouseNameHeight), new Vector2(width - textLeft - HouseCardPadding, HouseCardHeight - HouseNameHeight - 14f), false);

        string badge = UiKitNames.UpgradeBadge(state);
        Sprite badgeSprite = badge != null ? Kit(badge) : null;
        if (badgeSprite != null)
        {
            Image b = Tile(card.transform, badgeSprite, new Vector2(width - HouseBadgeSize * 0.6f, -HouseBadgeSize * 0.4f), HouseBadgeSize);
            b.name = "Badge";
        }
        _houseItems.Add(card);
    }

    /// <summary>A card's state line: "Installed", its price (and in the detail card, <paramref name="upkeep"/>, its upkeep a night), its price and "too dear", or "Needs ..." with the names of the prerequisites not owned yet.</summary>
    private string StateLine(UpgradeSO upgrade, OrderState state, Dictionary<string, UpgradeSO> byId, bool upkeep = true)
    {
        string cr = UiText.Currency(UiText.WalletForm.Short);
        switch (state)
        {
            case OrderState.Owned:
                return "Installed";
            case OrderState.Locked:
                return "Needs " + string.Join(", ", UpgradeTree.Missing(upgrade.Node, _houseWorld.HasUpgrade).ConvertAll(need => byId.TryGetValue(need, out UpgradeSO u) ? u.displayName : need));
            case OrderState.TooDear:
                return $"{OrderBook.Price(_houseWorld, _houseLib, upgrade)} {cr} · too dear";
            default:
                float nightly = upkeep ? Sum(upgrade, EffectOpType.Upkeep) : 0f;
                return $"{OrderBook.Price(_houseWorld, _houseLib, upgrade)} {cr}" + (nightly > 0f ? $" + {nightly:0.##} a night" : string.Empty);
        }
    }

    /// <summary>A state line's ink: green when installed, oxblood with a price, signal red when too dear, grey when locked (the kit's upgrade card, sheet 03).</summary>
    private static Color StateInk(OrderState state) =>
        state == OrderState.Owned ? HouseOwnedInk : state == OrderState.TooDear ? HouseAlertInk : state == OrderState.Locked ? HouseLockedInk : HousePriceInk;

    /// <summary>An upgrade's tile: its own art when the file exists (ArtSlots.UpgradeIcon), else its category's kit tile (locked: the screentone tile).</summary>
    private Sprite UpgradeTile(UpgradeSO upgrade, bool locked)
    {
        Sprite art = SlotArt.Sprite(ArtSlots.UpgradeIcon(upgrade.id));
        return art != null ? art : Kit("tile_" + BranchTile(upgrade.Node.Branch) + (locked ? "_locked" : "_rest"));
    }

    /// <summary>The kit tile that stands for a Home category (the House's heads and cards without art of their own).</summary>
    private static string BranchTile(UpgradeBranch branch)
    {
        switch (branch)
        {
            case UpgradeBranch.Food:
                return "food";
            case UpgradeBranch.Housing:
                return "house";
            case UpgradeBranch.Security:
                return "shield";
            case UpgradeBranch.Health:
                return "medicine";
            case UpgradeBranch.Comfort:
                return "sofa";
            default:
                return "star";
        }
    }

    /// <summary>A connector from a prerequisite's card at <paramref name="from"/> down to its dependant's at <paramref name="to"/> (tree space, y down): straight down in one sub-column, else down, across and down, bending in the row gap; brass on an ink keyline once the prerequisite is owned, a grey dashed line before.</summary>
    private void AddConnector(Vector2 from, Vector2 to, float cardWidth, bool lit)
    {
        float ax = from.x + cardWidth / 2f, bx = to.x + cardWidth / 2f;
        float top = from.y + HouseCardHeight, bottom = to.y;
        float bend = bottom - HouseRowGap / 2f;
        var points = Mathf.Abs(ax - bx) < 0.5f
            ? new[] { new Vector2(ax, top), new Vector2(ax, bottom) }
            : new[] { new Vector2(ax, top), new Vector2(ax, bend), new Vector2(bx, bend), new Vector2(bx, bottom) };
        for (int i = 0; i + 1 < points.Length; i++)
        {
            if (lit)
            {
                Line(points[i], points[i + 1], HouseLinkKeyline, HouseLinkInk);
                Line(points[i], points[i + 1], HouseLinkWidth, HouseLinkLit);
            }
            else
                Dashes(points[i], points[i + 1]);
        }
    }

    /// <summary>A straight axis-aligned line of <paramref name="thickness"/> from <paramref name="a"/> to <paramref name="b"/> (tree space), drawn under the cards.</summary>
    private void Line(Vector2 a, Vector2 b, float thickness, Color colour)
    {
        Vector2 min = Vector2.Min(a, b), max = Vector2.Max(a, b);
        Segment(new Vector2(min.x - thickness / 2f, min.y - thickness / 2f), new Vector2(max.x - min.x + thickness, max.y - min.y + thickness), colour);
    }

    /// <summary>A dashed axis-aligned line from <paramref name="a"/> to <paramref name="b"/> (a link whose prerequisite is not owned yet).</summary>
    private void Dashes(Vector2 a, Vector2 b)
    {
        float length = Vector2.Distance(a, b);
        if (length <= 0f)
            return;
        Vector2 step = (b - a) / length;
        for (float d = 0f; d < length; d += HouseDashPeriod)
            Line(a + step * d, a + step * Mathf.Min(length, d + HouseDashPeriod * 0.55f), HouseDashWidth, HouseLinkDim);
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

    /// <summary>The detail card for <paramref name="upgrade"/> (none: an empty card): its tile, its name, price and upkeep, blurb, effects in words (HouseEffects.Line) and state; Buy only while it is buyable.</summary>
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
                houseDetailText.text = $"<size=130%><b>{upgrade.displayName}</b></size>\n{StateLine(upgrade, state, byId)}\n\n{upgrade.description}\n\n{HouseEffects.Line(ops)}";
            }
        }

        if (houseDetailIcon != null)
        {
            Sprite icon = upgrade != null ? UpgradeTile(upgrade, state == OrderState.Locked) : null;
            houseDetailIcon.sprite = icon;
            houseDetailIcon.enabled = icon != null;
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

    /// <summary>What a spin shows: the line, and for a spin that happened the outcome's place in the library and whether it won money (the reels' faces, SlotReels).</summary>
    public readonly struct SpinView
    {
        /// <summary>The line shown under the reels.</summary>
        public readonly string Line;

        /// <summary>The outcome's index in the library's list; -1 when nothing spun (the reels stay as they are).</summary>
        public readonly int Outcome;

        /// <summary>Whether the spin won money (one symbol on every reel).</summary>
        public readonly bool Win;

        /// <summary>A refused or empty spin: a line only.</summary>
        public SpinView(string line) : this(line, -1, false)
        {
        }

        /// <summary>A spin.</summary>
        public SpinView(string line, int outcome, bool win)
        {
            Line = line;
            Outcome = outcome;
            Win = win;
        }
    }

    /// <summary>
    /// Shows the slot machine. onSpin is invoked when the player clicks Spin and
    /// returns what to show (its line, and for a spin that happened the reels'
    /// result: they spin briefly and land on SlotReels' faces). onContinue is
    /// invoked when the player moves on to sleep (or immediately if unwired).
    /// </summary>
    public void ShowSlot(WorldState world, GameConfigSO config, Func<SpinView> onSpin, Action onContinue)
    {
        if (!HasSlotPanel || world == null)
        {
            onContinue?.Invoke();
            return;
        }

        _onSlotContinue = onContinue;

        if (slotTitleText != null)
            slotTitleText.text = "Night Slots";

        int spinCost = config != null ? config.slotSpinCost : 0;

        if (slotBodyText != null)
            slotBodyText.text = $"Spin for {spinCost} {UiText.Currency(UiText.WalletForm.Inline)}. Try your luck for tomorrow's shift.";

        if (slotSpinButton != null)
        {
            slotSpinButton.onClick.RemoveAllListeners();
            slotSpinButton.onClick.AddListener(() =>
            {
                if (onSpin == null)
                    return;
                SpinView spin = onSpin.Invoke();
                if (slotBodyText != null && !string.IsNullOrEmpty(spin.Line))
                    slotBodyText.text = spin.Line;
                if (spin.Outcome >= 0)
                    SpinReels(SlotReels.Faces(spin.Outcome, spin.Win, ReelSymbols.Length));
            });
        }

        slotPanel.SetActive(true);
        ShowReels(SlotReels.Faces(0, false, ReelSymbols.Length));
    }

    /// <summary>The reels' symbols: kit tiles (SlotReels indexes into this list).</summary>
    private static readonly string[] ReelSymbols = { "tile_crate_rest", "tile_star_rest", "tile_bolt_rest", "tile_paw_rest", "tile_moon_rest" };

    /// <summary>Spins the reels (each cycles through the symbols, stopping one after another) and lands them on <paramref name="faces"/>.</summary>
    private void SpinReels(int[] faces)
    {
        if (_reelSpin != null)
            StopCoroutine(_reelSpin);
        _reelSpin = isActiveAndEnabled ? StartCoroutine(Spin(faces)) : null;
        if (_reelSpin == null)
            ShowReels(faces);
    }

    /// <summary>The reels' spin: every reel cycles, the first stops after <see cref="ReelSpinSeconds"/>, each next one a beat later.</summary>
    private IEnumerator Spin(int[] faces)
    {
        float start = Time.unscaledTime;
        var shown = new int[faces.Length];
        int turn = 0;
        while (true)
        {
            float t = Time.unscaledTime - start;
            bool done = true;
            for (int i = 0; i < shown.Length; i++)
            {
                bool stopped = t >= ReelSpinSeconds + i * ReelStopGap;
                shown[i] = stopped ? faces[i] : (turn + i * 2) % ReelSymbols.Length;
                done &= stopped;
            }
            ShowReels(shown);
            if (done)
                break;
            turn++;
            yield return new WaitForSecondsRealtime(ReelFrameSeconds);
        }
        _reelSpin = null;
    }

    /// <summary>Shows <paramref name="faces"/> on the reels (a reel without a face keeps its sprite).</summary>
    private void ShowReels(int[] faces)
    {
        for (int i = 0; i < slotReelFaces.Length && i < faces.Length; i++)
        {
            Sprite symbol = Kit(ReelSymbols[faces[i]]);
            if (slotReelFaces[i] != null && symbol != null)
                slotReelFaces[i].sprite = symbol;
        }
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
    // Runtime row helpers (the kit's pieces)
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

    // The runtime rows' layout (reference px; the kit's visible sizes, its sprites' shadow room outside them).
    private const float LabelRowHeight = 36f;
    private const float RowHeight = 62f;
    private const float RowPad = 12f;
    private const float RowTileSize = 44f;
    private const float RowNameFontSize = 26f;
    private const float RowDetailFontSize = 19f;
    private const float RowFontSize = 22f;
    private const float SegmentWidth = 124f;
    private const float SegmentHeight = 40f;
    private const float SegmentFontSize = 19f;
    private const float RowButtonWidth = 210f;
    private const float RowButtonHeight = 42f;
    private const float RowButtonFontSize = 19f;
    private static readonly Color RowDetailInk = new Color(0.42f, 0.36f, 0.33f, 1f);
    private static readonly Color RowButtonFill = new Color(0.95f, 0.95f, 0.95f, 1f);

    // The reels' spin (seconds).
    private const float ReelSpinSeconds = 0.6f;
    private const float ReelStopGap = 0.25f;
    private const float ReelFrameSeconds = 0.06f;

    // The House's layout (reference px) and colours: the kit's cards under dark ink (sheet 03).
    private const float HouseHeadHeight = 44f;
    private const float HouseHeadGap = 18f;
    private const float HouseHeadPad = 10f;
    private const float HouseHeadTile = 30f;
    private const float HouseHeadFontSize = 24f;
    private const float HouseCardHeight = 100f;
    private const float HouseNameHeight = 54f;
    private const float HouseNameSpacing = 1f;
    private const float HouseCardMaxWidth = 300f;
    private const float HouseCardPadding = 10f;
    private const float HouseIconSize = 48f;
    private const float HouseBadgeSize = 32f;
    private const float HouseBadgeRoom = 14f;
    private const float HouseNameFontSize = 22f;
    private const float HouseStateFontSize = 20f;
    private const float HouseMinFontSize = 20f;
    private const float HouseRowGap = 28f;
    private const float HouseSlotGap = 16f;
    private const float HouseBandGap = 34f;
    private const float HouseLinkWidth = 5f;
    private const float HouseLinkKeyline = 9f;
    private const float HouseDashWidth = 3f;
    private const float HouseDashPeriod = 12f;
    private static readonly Color HouseBuyableFill = new Color(0.98f, 0.95f, 0.88f, 1f);
    private static readonly Color HouseLockedFill = new Color(0.87f, 0.84f, 0.78f, 1f);
    private static readonly Color HouseCardInk = new Color(0.169f, 0.11f, 0.141f, 1f);
    private static readonly Color HouseLockedInk = new Color(0.36f, 0.33f, 0.3f, 1f);
    private static readonly Color HouseOwnedInk = new Color(0.26f, 0.42f, 0.22f, 1f);
    private static readonly Color HousePriceInk = new Color(0.541f, 0.184f, 0.231f, 1f);
    private static readonly Color HouseAlertInk = new Color(0.62f, 0.13f, 0.1f, 1f);
    private static readonly Color HouseSelectedOutline = new Color(0.831f, 0.627f, 0.333f, 1f);
    private static readonly Color HouseLinkLit = new Color(0.831f, 0.627f, 0.333f, 1f);
    private static readonly Color HouseLinkInk = new Color(0.169f, 0.11f, 0.141f, 1f);
    private static readonly Color HouseLinkDim = new Color(0.55f, 0.5f, 0.46f, 1f);

    /// <summary>The kit's sprite called <paramref name="name"/>, or null without the kit or the sprite.</summary>
    private Sprite Kit(string name) => kit != null ? kit.Get(name) : null;

    /// <summary>A label's ink on a dark kit face (bone) or a light one (ink); white / near-black without the kit.</summary>
    private Color LabelInk(bool dark) => kit != null ? (dark ? kit.inkOnDark : kit.inkOnLight) : dark ? Color.white : HouseCardInk;

    /// <summary>
    /// The kit's face for <paramref name="host"/>: a child image of the kit
    /// sprite <paramref name="sprite"/>, sliced at the kit's scale and reaching
    /// past the host by the sprite's shadow room, so the host's rect is the
    /// face as seen; drawn first under the host's other children. Null (and
    /// nothing made) without the kit or the sprite.
    /// </summary>
    private Image Face(Transform host, string sprite)
    {
        Sprite s = Kit(sprite);
        if (s == null)
            return null;
        var go = new GameObject("Face", typeof(RectTransform));
        go.transform.SetParent(host, false);
        go.transform.SetAsFirstSibling();
        var rt = (RectTransform)go.transform;
        float pad = kit.spritePad / kit.overlayScale;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-pad, -pad);
        rt.offsetMax = new Vector2(pad, pad);
        Image image = go.AddComponent<Image>();
        image.sprite = s;
        image.type = s.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = kit.overlayScale;
        return image;
    }

    /// <summary>A kit tile (or any square picture) at <paramref name="topLeft"/> of <paramref name="parent"/> (y down) whose visible square is <paramref name="size"/>: the image grows by the kit's shadow room; no raycasts.</summary>
    private Image Tile(Transform parent, Sprite sprite, Vector2 topLeft, float size)
    {
        var go = new GameObject("Tile", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        float grow = KitSprite(sprite) ? size * SpritePadShare(sprite) : 0f;
        Place((RectTransform)go.transform, topLeft - new Vector2(grow, grow), new Vector2(size + 2f * grow, size + 2f * grow));
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    /// <summary>True when <paramref name="sprite"/> is one of the kit's (it carries the kit's shadow room round it).</summary>
    private bool KitSprite(Sprite sprite)
    {
        if (kit == null || sprite == null)
            return false;
        foreach (UiKitSO.KitSprite s in kit.sprites)
            if (s != null && s.sprite == sprite)
                return true;
        return false;
    }

    /// <summary>A kit sprite's shadow room on each side as a share of its visible square.</summary>
    private float SpritePadShare(Sprite sprite)
    {
        float inner = sprite.rect.height - 2f * kit.spritePad;
        return inner > 0f ? kit.spritePad / inner : 0f;
    }

    /// <summary>An untargeted text at <paramref name="topLeft"/> of <paramref name="parent"/> (y down), shrinking rather than overflowing (UiText.FitLabel), never below the House's floor (20 reference px: 13 px at 720p; past it an ellipsis); a <paramref name="label"/> in the kit's label face, upper case; wrapping onto more lines when <paramref name="wrap"/>.</summary>
    private TMP_Text Text(Transform parent, string name, string text, float size, Color ink, TextAlignmentOptions alignment, Vector2 topLeft, Vector2 box, bool label, bool wrap = false)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, topLeft, box);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        if (label && kit != null && kit.labelFont != null)
            t.font = kit.labelFont;
        t.fontSize = size;
        t.fontStyle = label ? FontStyles.UpperCase : FontStyles.Normal;
        t.characterSpacing = label ? 3f : 0f;
        t.color = ink;
        t.alignment = alignment;
        t.raycastTarget = false;
        UiText.FitLabel(t, wrap);
        t.fontSizeMin = Mathf.Max(t.fontSizeMin, Mathf.Min(size, HouseMinFontSize));
        t.overflowMode = TextOverflowModes.Ellipsis;
        return t;
    }

    /// <summary>Puts <paramref name="rt"/> at <paramref name="topLeft"/> of its parent (y down from the parent's top-left) with <paramref name="size"/>.</summary>
    private static void Place(RectTransform rt, Vector2 topLeft, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
        rt.sizeDelta = size;
    }

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
        text.textWrappingMode = TextWrappingModes.Normal;

        return row;
    }

    /// <summary>A kit row in <paramref name="parent"/>: the list row plate (its rect the plate as seen) and a tile on the left when given; returns the row and its text's left edge (the text stretches from there to the room kept on the right).</summary>
    private GameObject Row(Transform parent, string name, Sprite tile, out float textLeft)
    {
        var row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = RowHeight;
        layout.flexibleWidth = 1f;
        ((RectTransform)row.transform).sizeDelta = new Vector2(0f, RowHeight);

        Image plate = Face(row.transform, "listrow_rest");
        if (plate != null)
            plate.raycastTarget = false;

        textLeft = RowPad;
        if (tile != null)
        {
            Tile(row.transform, tile, new Vector2(RowPad, (RowHeight - RowTileSize) / 2f), RowTileSize);
            textLeft += RowTileSize + RowPad;
        }
        return row;
    }

    /// <summary>Stretches a row's text across the row from <paramref name="left"/> to <paramref name="right"/> short of its right edge, <paramref name="top"/> down from its top and <paramref name="height"/> tall.</summary>
    private static void Across(TMP_Text text, float left, float right, float top, float height)
    {
        RectTransform rt = text.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.offsetMin = new Vector2(left, -top - height);
        rt.offsetMax = new Vector2(-right, -top);
    }

    /// <summary>Puts <paramref name="rt"/> <paramref name="right"/> in from its parent's right edge and <paramref name="top"/> down, with <paramref name="size"/>.</summary>
    private static void PlaceRight(RectTransform rt, float right, float top, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-right, -top);
        rt.sizeDelta = size;
    }

    /// <summary>
    /// A bill's row: its kit tile (food, heating, electricity, TV, medicine;
    /// locked when the bill is not offered), its name over its price and line,
    /// and its Paying / Skip pair on the right (the kit's segmented track, the
    /// side that holds oxblood and pressed, the other bone; clicking the side
    /// that is off calls <paramref name="onToggle"/>; when the choice changed
    /// since the last show, the oxblood face slides across from the side it
    /// left, the reel's segmented pill: UiPill).
    /// </summary>
    private GameObject CreateBillRow(BillView view, string payingLabel, string skipLabel, Action onToggle)
    {
        Sprite tile = Kit("tile_" + BillTile(view.Bill) + (view.Enabled ? "_rest" : "_locked"));
        float pair = 2f * SegmentWidth + 8f;
        GameObject row = Row(billRowsRoot, "Row_" + view.Bill, tile, out float left);
        float right = pair + 2f * RowPad;
        Across(Text(row.transform, "Name", view.Name, RowNameFontSize, LabelInk(false), TextAlignmentOptions.BottomLeft, Vector2.zero, Vector2.zero, true),
               left, right, 4f, RowHeight / 2f + 2f);
        Across(Text(row.transform, "Detail", view.Detail, RowDetailFontSize, RowDetailInk, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.zero, false),
               left, right, RowHeight / 2f + 6f, RowHeight / 2f - 8f);

        var track = new GameObject("Pair", typeof(RectTransform));
        track.transform.SetParent(row.transform, false);
        PlaceRight((RectTransform)track.transform, RowPad, (RowHeight - SegmentHeight - 8f) / 2f, new Vector2(pair, SegmentHeight + 8f));
        Image trackFace = Face(track.transform, "segmented_track");
        if (trackFace != null)
            trackFace.raycastTarget = false;
        Image paying = PairSide(track.transform, "Paying", payingLabel, 4f, view.Paying, view.Enabled, onToggle);
        Image skip = PairSide(track.transform, "Skip", skipLabel, 4f + SegmentWidth, !view.Paying, view.Enabled, onToggle);
        if (_billPaying.TryGetValue(view.Bill, out bool was) && was != view.Paying && paying != null && skip != null)
        {
            Image on = view.Paying ? paying : skip, off = view.Paying ? skip : paying;
            UiPill.Slide(on.rectTransform, off.transform.parent.TransformPoint(on.rectTransform.localPosition));
        }
        _billPaying[view.Bill] = view.Paying;
        return row;
    }

    /// <summary>One side of a choice pair at <paramref name="x"/> in <paramref name="track"/>: on (the kit's oxblood segment, cream label) or off (bone, ink label; a click calls <paramref name="onPick"/>); locked when not <paramref name="enabled"/>; with the kit controls' game feel (UiJuice; the on side is the chosen one). Returns its face.</summary>
    private Image PairSide(Transform track, string name, string label, float x, bool on, bool enabled, Action onPick)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(track, false);
        Place((RectTransform)go.transform, new Vector2(x, 4f), new Vector2(SegmentWidth, SegmentHeight));
        Image face = Face(go.transform, on ? "segment_on" : "segment_off");
        if (face == null)
        {
            face = go.AddComponent<Image>();
            face.color = on ? HousePriceInk : RowButtonFill;
        }
        Button button = go.AddComponent<Button>();
        button.targetGraphic = face;
        button.interactable = enabled && !on && onPick != null;
        ColorBlock colours = button.colors;
        colours.disabledColor = on ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f);
        colours.highlightedColor = new Color(1f, 0.96f, 0.86f, 1f);
        button.colors = colours;
        if (onPick != null)
            button.onClick.AddListener(() => onPick());
        UiJuice.On(button);
        if (enabled)
            UiJuice.Choose(button, on, false); // the pill slides instead of a bounce
        TMP_Text text = Text(go.transform, "Label", label, SegmentFontSize, on ? LabelInk(true) : LabelInk(false), TextAlignmentOptions.Center, Vector2.zero,
                             new Vector2(SegmentWidth, SegmentHeight), true);
        if (!enabled)
            text.alpha = 0.55f;
        return face;
    }

    /// <summary>The kit tile that stands for a bill (sheet 04's bills).</summary>
    private static string BillTile(HomeBill bill)
    {
        switch (bill)
        {
            case HomeBill.Food:
                return "food";
            case HomeBill.Heating:
                return "flame";
            case HomeBill.Electricity:
                return "bolt";
            case HomeBill.Tv:
                return "tv";
            default:
                return "medicine";
        }
    }

    /// <summary>A toy's row: its picture (ArtSlots.PetToy, else the kit tile its id names, else the paw), its name, and its oxblood mini plate (Play; Played tonight is the locked plate).</summary>
    private GameObject CreateToyRow(ToyView toy)
    {
        Sprite tile = SlotArt.Sprite(ArtSlots.PetToy(toy.Id)) ?? ToyTile(toy.Id);
        GameObject row = Row(toyRowsRoot, "Row_" + toy.Id, tile, out float left);
        Across(Text(row.transform, "Name", toy.Label, RowNameFontSize, LabelInk(false), TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.zero, true),
               left, RowButtonWidth + 2f * RowPad, 0f, RowHeight);

        var go = new GameObject("Button", typeof(RectTransform));
        go.transform.SetParent(row.transform, false);
        PlaceRight((RectTransform)go.transform, RowPad, (RowHeight - RowButtonHeight) / 2f, new Vector2(RowButtonWidth, RowButtonHeight));
        Image face = Face(go.transform, "miniplate_ox_rest");
        if (face == null)
        {
            face = go.AddComponent<Image>();
            face.color = RowButtonFill;
        }
        Button button = go.AddComponent<Button>();
        button.targetGraphic = face;
        if (kit != null)
            kit.Show(face, "miniplate_ox", button);
        button.transition = kit != null ? Selectable.Transition.SpriteSwap : Selectable.Transition.ColorTint;
        button.interactable = toy.Enabled && toy.OnPlay != null;
        if (toy.OnPlay != null)
            button.onClick.AddListener(() => toy.OnPlay());
        Text(go.transform, "Label", toy.Button, RowButtonFontSize, LabelInk(kit != null), TextAlignmentOptions.Center, Vector2.zero,
             new Vector2(RowButtonWidth, RowButtonHeight), true);
        return row;
    }

    /// <summary>The kit tile a toy's id names (toy_ball: the ball), else the paw.</summary>
    private Sprite ToyTile(string id)
    {
        foreach (string word in (id ?? string.Empty).Split('_'))
        {
            Sprite tile = Kit("tile_" + word + "_rest");
            if (tile != null)
                return tile;
        }
        return Kit("tile_paw_rest");
    }
}
