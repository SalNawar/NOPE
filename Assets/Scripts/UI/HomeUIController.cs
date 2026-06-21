using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the Home phase panels (Phase 4): daily expenses + family condition,
/// upgrade shop, slot machine, and the sleep prompt that hands off to the
/// next day. All references are optional; unwired panels are skipped so the
/// flow degrades gracefully (HomeManager just calls straight through).
/// Dynamic rows (family members, shop items) are spawned at runtime.
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

    [Header("Shop Panel")]
    /// <summary>Root of the shop panel.</summary>
    [SerializeField] private GameObject shopPanel;

    /// <summary>Shop title.</summary>
    [SerializeField] private TMP_Text shopTitleText;

    /// <summary>Shop intro / status text.</summary>
    [SerializeField] private TMP_Text shopBodyText;

    /// <summary>Container for one row per upgrade (Buy buttons).</summary>
    [SerializeField] private Transform shopRowsRoot;

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

    /// <summary>Spawned shop item rows (cleared/rebuilt on refresh).</summary>
    private readonly List<GameObject> _shopRows = new();

    /// <summary>Pending callback for the expenses continue button.</summary>
    private Action _onExpensesContinue;

    /// <summary>Pending callback for the shop continue button.</summary>
    private Action _onShopContinue;

    /// <summary>Pending callback for the slot continue button.</summary>
    private Action _onSlotContinue;

    /// <summary>Pending callback for the sleep button.</summary>
    private Action _onSleep;

    /// <summary>True if the expenses panel is wired and can be shown.</summary>
    public bool HasExpensesPanel => expensesPanel != null;

    /// <summary>True if the shop panel is wired and can be shown.</summary>
    public bool HasShopPanel => shopPanel != null;

    /// <summary>True if the slot panel is wired and can be shown.</summary>
    public bool HasSlotPanel => slotPanel != null;

    /// <summary>True if the sleep panel is wired and can be shown.</summary>
    public bool HasSleepPanel => sleepPanel != null && sleepButton != null;

    /// <summary>Hides all panels on scene start and wires static buttons.</summary>
    private void Awake()
    {
        if (expensesPanel != null) expensesPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (slotPanel != null) slotPanel.SetActive(false);
        if (sleepPanel != null) sleepPanel.SetActive(false);

        if (expensesContinueButton != null)
            expensesContinueButton.onClick.AddListener(HandleExpensesContinueClicked);

        if (shopContinueButton != null)
            shopContinueButton.onClick.AddListener(HandleShopContinueClicked);

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
            moneyText.text = $"Credits: {world.money}";

        if (stabilityText != null)
            stabilityText.text = $"Stability: {world.timelineStability:0}%";

        if (dayText != null)
            dayText.text = $"Day {world.day}";
    }

    // =========================================================
    // Expenses panel
    // =========================================================

    /// <summary>
    /// Shows today's expense breakdown and one row per family member with a
    /// Treat button (calls onTreat with the member's index). Invokes
    /// onContinue when the player moves on to the shop (or immediately if unwired).
    /// </summary>
    public void ShowExpenses(WorldState world, HomeEconomy.ExpenseReport report, GameConfigSO config, Action<int> onTreat, Action onContinue)
    {
        if (!HasExpensesPanel || world == null)
        {
            onContinue?.Invoke();
            return;
        }

        _onExpensesContinue = onContinue;

        if (expensesTitleText != null)
            expensesTitleText.text = $"Day {world.day} — Home";

        if (expensesBodyText != null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Today's expenses:");
            sb.AppendLine($"  Rent & utilities: -{report.baseAmount}");

            if (report.memberCount > 0)
                sb.AppendLine($"  Family upkeep ({report.memberCount}): -{report.memberAmount}");

            if (report.conditionAmount > 0)
                sb.AppendLine($"  Medical drain: -{report.conditionAmount}");

            sb.AppendLine($"Total: -{report.total} credits   (Balance: {world.money})");

            if (world.money < 0)
                sb.AppendLine("\nYou are in debt. Find a way to make ends meet.");

            expensesBodyText.text = sb.ToString();
        }

        BuildFamilyRows(world, config, onTreat);

        expensesPanel.SetActive(true);
    }

    /// <summary>Rebuilds the family member rows (name, condition, Treat button).</summary>
    private void BuildFamilyRows(WorldState world, GameConfigSO config, Action<int> onTreat)
    {
        if (familyRowsRoot == null)
            return;

        ClearRows(_familyRows);

        int careCost = HomeEconomy.GetCareCost(config);

        for (int i = 0; i < world.family.members.Count; i++)
        {
            FamilyMemberData member = world.family.members[i];

            if (member == null)
                continue;

            int capturedIndex = i;
            string label = $"{member.name} — condition {member.condition}";
            string buttonLabel = $"Treat (-{careCost})";
            bool interactable = member.condition > 0 && world.money >= careCost && onTreat != null;

            GameObject row = CreateRow(familyRowsRoot, label, buttonLabel, interactable,
                () => onTreat?.Invoke(capturedIndex));

            _familyRows.Add(row);
        }

        if (world.family.members.Count == 0)
            _familyRows.Add(CreateLabelRow(familyRowsRoot, "No family members on record."));
    }

    /// <summary>Expenses continue clicked: close and move to the shop.</summary>
    private void HandleExpensesContinueClicked()
    {
        if (expensesPanel != null)
            expensesPanel.SetActive(false);

        Action cb = _onExpensesContinue;
        _onExpensesContinue = null;
        cb?.Invoke();
    }

    // =========================================================
    // Shop panel
    // =========================================================

    /// <summary>
    /// Shows the upgrade shop: one row per upgrade with its (discounted) cost
    /// and a Buy button. Invokes onBuy(upgrade) when purchased, onContinue
    /// when the player moves on to the slot machine (or immediately if unwired).
    /// </summary>
    public void ShowShop(WorldState world, ContentLibrarySO lib, Action<UpgradeSO> onBuy, Action onContinue)
    {
        if (!HasShopPanel || world == null)
        {
            onContinue?.Invoke();
            return;
        }

        _onShopContinue = onContinue;

        if (shopTitleText != null)
            shopTitleText.text = "Upgrade Shop";

        if (shopBodyText != null)
        {
            shopBodyText.text = $"Balance: {world.money} credits";
        }

        BuildShopRows(world, lib, onBuy);

        shopPanel.SetActive(true);
    }

    /// <summary>Rebuilds the shop item rows (name + cost, Buy/Owned button).</summary>
    private void BuildShopRows(WorldState world, ContentLibrarySO lib, Action<UpgradeSO> onBuy)
    {
        if (shopRowsRoot == null)
            return;

        ClearRows(_shopRows);

        IReadOnlyList<UpgradeSO> upgrades = lib != null ? lib.Upgrades : System.Array.Empty<UpgradeSO>();

        if (upgrades.Count == 0)
        {
            _shopRows.Add(CreateLabelRow(shopRowsRoot, "No upgrades stocked yet."));
            return;
        }

        foreach (UpgradeSO upgrade in upgrades)
        {
            if (upgrade == null)
                continue;

            bool owned = world.HasUpgrade(upgrade.id);
            float discountPercent = lib != null
                ? TimelineEffects.GetShopDiscountPercent(world, lib, upgrade.id)
                : 0f;
            int cost = Mathf.RoundToInt(upgrade.cost * (1f - discountPercent / 100f));

            string label = owned
                ? $"{upgrade.displayName} (owned)"
                : discountPercent > 0f
                    ? $"{upgrade.displayName} — {cost} cr ({discountPercent:0}% off)"
                    : $"{upgrade.displayName} — {cost} cr";

            string buttonLabel = owned ? "Owned" : "Buy";
            bool interactable = !owned && world.money >= cost && onBuy != null;

            UpgradeSO capturedUpgrade = upgrade;
            GameObject row = CreateRow(shopRowsRoot, label, buttonLabel, interactable,
                () => onBuy?.Invoke(capturedUpgrade));

            _shopRows.Add(row);
        }
    }

    /// <summary>Shop continue clicked: close and move to the slot machine.</summary>
    private void HandleShopContinueClicked()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);

        Action cb = _onShopContinue;
        _onShopContinue = null;
        cb?.Invoke();
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
            slotBodyText.text = $"Spin for {spinCost} credits. Try your luck for tomorrow's shift.";

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

        Action cb = _onSlotContinue;
        _onSlotContinue = null;
        cb?.Invoke();
    }

    // =========================================================
    // Sleep panel
    // =========================================================

    /// <summary>
    /// Shows the sleep prompt. Invokes onSleep when the player clicks Sleep
    /// (or immediately if unwired) — HomeManager then advances to the next day.
    /// </summary>
    public void ShowSleep(WorldState world, Action onSleep)
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
            sleepBodyText.text = "Get some rest. Tomorrow's briefing will reflect tonight's choices.";

        sleepPanel.SetActive(true);
    }

    /// <summary>Sleep clicked: close and advance to the next day.</summary>
    private void HandleSleepClicked()
    {
        if (sleepPanel != null)
            sleepPanel.SetActive(false);

        Action cb = _onSleep;
        _onSleep = null;
        cb?.Invoke();
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

    /// <summary>Creates a label-only row (no button) — used for empty-state messages.</summary>
    private static GameObject CreateLabelRow(Transform parent, string label)
    {
        var row = new GameObject("Row_Label", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        var layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = 36f;
        layout.flexibleWidth = 1f;

        var text = row.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 22;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;

        return row;
    }

    /// <summary>Creates a row with a label on the left and a button on the right.</summary>
    private static GameObject CreateRow(Transform parent, string label, string buttonLabel, bool buttonInteractable, Action onClick)
    {
        var row = new GameObject("Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        var rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 44f;
        rowLayout.flexibleWidth = 1f;

        var hLayout = row.AddComponent<HorizontalLayoutGroup>();
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = true;
        hLayout.spacing = 12f;
        hLayout.childAlignment = TextAnchor.MiddleLeft;

        // Label.
        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(row.transform, false);

        var labelLayout = labelGo.AddComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;

        var labelText = labelGo.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 22;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;

        // Button.
        var buttonGo = new GameObject("Button", typeof(RectTransform));
        buttonGo.transform.SetParent(row.transform, false);

        var buttonLayout = buttonGo.AddComponent<LayoutElement>();
        buttonLayout.preferredWidth = 160f;
        buttonLayout.preferredHeight = 40f;

        Image img = buttonGo.AddComponent<Image>();
        img.color = new Color(0.95f, 0.95f, 0.95f, 1f);

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
        btnLabelText.fontSize = 20;
        btnLabelText.alignment = TextAlignmentOptions.Center;
        btnLabelText.color = Color.black;

        if (onClick != null)
            btn.onClick.AddListener(() => onClick());

        return row;
    }
}
