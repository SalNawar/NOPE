using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static SceneUiKit;

/// <summary>
/// One-click builder for the Home scene UI added in Alpha Phase 4: HUD
/// (day/money/stability), expenses + family condition panel, the House
/// (Home's upgrade tree in the old shop panel; the Home upgrades spec §6),
/// slot machine, and the sleep prompt. Unlike OfficeSceneUIBuilder, this
/// script creates the Canvas, EventSystem, HomeManager and HomeUIController
/// objects from scratch if they don't already exist (HomeScene starts as an
/// empty scene). Safe to re-run: skips/finds pieces that already exist (by name,
/// through the shared SceneUiKit),
/// except the HUD's backing strip, which it keeps as it builds it (the
/// readability fix: the HUD's white texts read over the room's art on it),
/// and the expenses and House panels' layout, which it re-applies on every
/// build (the House grew into a tree; the evening's lines outgrew the old
/// expenses body); it then checks every text's contrast on what it is drawn
/// on (UiContrastCheck).
/// </summary>
public static class HomeSceneBuilder
{
    /// <summary>Builds and wires the Home UI in the open scene.</summary>
    [MenuItem("Tools/TimeDesk/Build Home UI (Panels + Wiring)")]
    public static void Build()
    {
        // --- Canvas + EventSystem ---
        Canvas canvas = SceneUiKit.EnsureCanvasAndEventSystem();
        EnsureScaler(canvas);
        Transform root = canvas.transform;

        // --- HomeManager (logic) + HomeUIController (UI) ---
        HomeManager homeManager = SceneUiKit.FindOrCreateObject<HomeManager>("HomeManager");
        HomeUIController homeUI = SceneUiKit.FindOrCreateStretched<HomeUIController>(root, "HomeUI");

        Transform uiRoot = homeUI.transform;

        // --- HUD bar (top) ---
        Transform hud = FindOrCreatePanel(uiRoot, "HUD", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -50f), new Vector2(0f, 0f), withBackground: false);

        TMP_Text dayText = FindOrCreateText(hud, "DayText", "Day 1", 28, TextAlignmentOptions.Left,
            new Vector2(0f, 0f), new Vector2(0.2f, 1f));
        TMP_Text moneyText = FindOrCreateText(hud, "MoneyText", "Credits: 0", 28, TextAlignmentOptions.Center,
            new Vector2(0.4f, 0f), new Vector2(0.6f, 1f));
        TMP_Text stabilityText = FindOrCreateText(hud, "StabilityText", "Stability: 100%", 28, TextAlignmentOptions.Right,
            new Vector2(0.8f, 0f), new Vector2(1f, 1f));
        // The wallet and stability read the leading culture's labels (UiText): themed like the office tray's, so the
        // culture's font draws them (an Arabic label in LiberationSans drew boxes); the Tray ink is white on this dark strip.
        Tag(moneyText, ThemeRoleId.Tray, ThemePart.Ink, fit: true);
        Tag(stabilityText, ThemeRoleId.Tray, ThemePart.Ink, fit: true);
        EnsureHudBacking(hud);

        // --- Expenses panel ---
        Transform expenses = FindOrCreatePanel(uiRoot, "ExpensesPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(760f, 600f), withBackground: true, bgColor: new Color(0.1f, 0.12f, 0.2f, 0.97f));

        TMP_Text expensesTitle = FindOrCreateText(expenses, "TitleText", "Day 1 — Home", 34,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.875f), new Vector2(0.95f, 0.955f));
        TMP_Text expensesBody = FindOrCreateText(expenses, "BodyText", "...", 22,
            TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.44f), new Vector2(0.94f, 0.86f));
        Transform familyRows = FindOrCreateRowsContainer(expenses, "FamilyRows",
            new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.42f));
        Button expensesContinue = FindOrCreateButton(expenses, "ContinueButton", "Continue to the House",
            new Vector2(0.33f, 0.03f), new Vector2(0.67f, 0.11f));
        LayOutExpenses(expenses, expensesTitle, expensesBody, familyRows, expensesContinue);

        // --- House panel (the old shop panel, its hand-wired art kept) ---
        Transform shop = FindOrCreatePanel(uiRoot, "ShopPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, HousePanelSize, withBackground: true, bgColor: new Color(0.12f, 0.16f, 0.1f, 0.97f));

        TMP_Text shopTitle = FindOrCreateText(shop, "TitleText", "House", 34,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.895f), new Vector2(0.95f, 0.955f));
        TMP_Text shopBody = FindOrCreateText(shop, "BodyText", "...", 22,
            TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.85f), new Vector2(0.97f, 0.89f));
        RectTransform houseTree = FindOrCreateArea(shop, "HouseTree");
        TMP_Text houseDetail = FindOrCreateText(shop, "DetailText", "...", 24,
            TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.115f), new Vector2(0.8f, 0.285f));
        Button houseBuy = FindOrCreateButton(shop, "BuyButton", "Buy",
            new Vector2(0.83f, 0.15f), new Vector2(0.97f, 0.25f));
        Button shopContinue = FindOrCreateButton(shop, "ContinueButton", "Continue to Slots",
            new Vector2(0.4f, 0.02f), new Vector2(0.6f, 0.085f));
        LayOutHouse(shop, shopTitle, shopBody, houseTree, houseDetail, houseBuy, shopContinue);

        // --- Slot panel ---
        Transform slot = FindOrCreatePanel(uiRoot, "SlotPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(560f, 360f), withBackground: true, bgColor: new Color(0.2f, 0.16f, 0.05f, 0.97f));

        TMP_Text slotTitle = FindOrCreateText(slot, "TitleText", "Slot Machine", 34,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.98f));
        TMP_Text slotBody = FindOrCreateText(slot, "BodyText", "...", 24,
            TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.4f), new Vector2(0.94f, 0.82f));
        Button slotSpin = FindOrCreateButton(slot, "SpinButton", "Spin",
            new Vector2(0.1f, 0.2f), new Vector2(0.45f, 0.34f));
        Button slotContinue = FindOrCreateButton(slot, "ContinueButton", "Continue",
            new Vector2(0.55f, 0.2f), new Vector2(0.9f, 0.34f));
        BuildSlotMachineArt(slot);

        // --- Sleep panel ---
        Transform sleep = FindOrCreatePanel(uiRoot, "SleepPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(560f, 320f), withBackground: true, bgColor: new Color(0.08f, 0.1f, 0.18f, 0.97f));

        TMP_Text sleepTitle = FindOrCreateText(sleep, "TitleText", "Day 1 — Turn In", 34,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.98f));
        TMP_Text sleepBody = FindOrCreateText(sleep, "BodyText", "...", 24,
            TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.3f), new Vector2(0.94f, 0.82f));
        Button sleepButton = FindOrCreateButton(sleep, "SleepButton", "Sleep",
            new Vector2(0.3f, 0.06f), new Vector2(0.7f, 0.24f));

        // --- Wire HomeUIController ---
        var soUi = new SerializedObject(homeUI);
        soUi.FindProperty("moneyText").objectReferenceValue = moneyText;
        soUi.FindProperty("stabilityText").objectReferenceValue = stabilityText;
        soUi.FindProperty("dayText").objectReferenceValue = dayText;

        soUi.FindProperty("expensesPanel").objectReferenceValue = expenses.gameObject;
        soUi.FindProperty("expensesTitleText").objectReferenceValue = expensesTitle;
        soUi.FindProperty("expensesBodyText").objectReferenceValue = expensesBody;
        soUi.FindProperty("familyRowsRoot").objectReferenceValue = familyRows;
        soUi.FindProperty("expensesContinueButton").objectReferenceValue = expensesContinue;

        soUi.FindProperty("shopPanel").objectReferenceValue = shop.gameObject;
        soUi.FindProperty("shopTitleText").objectReferenceValue = shopTitle;
        soUi.FindProperty("shopBodyText").objectReferenceValue = shopBody;
        soUi.FindProperty("houseTreeRoot").objectReferenceValue = houseTree;
        soUi.FindProperty("houseDetailText").objectReferenceValue = houseDetail;
        soUi.FindProperty("houseBuyButton").objectReferenceValue = houseBuy;
        soUi.FindProperty("shopContinueButton").objectReferenceValue = shopContinue;

        soUi.FindProperty("slotPanel").objectReferenceValue = slot.gameObject;
        soUi.FindProperty("slotTitleText").objectReferenceValue = slotTitle;
        soUi.FindProperty("slotBodyText").objectReferenceValue = slotBody;
        soUi.FindProperty("slotSpinButton").objectReferenceValue = slotSpin;
        soUi.FindProperty("slotContinueButton").objectReferenceValue = slotContinue;

        soUi.FindProperty("sleepPanel").objectReferenceValue = sleep.gameObject;
        soUi.FindProperty("sleepTitleText").objectReferenceValue = sleepTitle;
        soUi.FindProperty("sleepBodyText").objectReferenceValue = sleepBody;
        soUi.FindProperty("sleepButton").objectReferenceValue = sleepButton;
        soUi.ApplyModifiedProperties();

        // --- Wire HomeManager ---
        var soMgr = new SerializedObject(homeManager);
        SerializedProperty homeUiProp = soMgr.FindProperty("homeUI");

        if (homeUiProp != null)
        {
            homeUiProp.objectReferenceValue = homeUI;
            soMgr.ApplyModifiedProperties();
        }

        // Panels start hidden (HomeUIController.Awake also enforces this).
        expenses.gameObject.SetActive(false);
        shop.gameObject.SetActive(false);
        slot.gameObject.SetActive(false);
        sleep.gameObject.SetActive(false);

        UiContrastCheck.Check(canvas, canvas.GetComponent<CanvasScaler>() is CanvasScaler s && s.referenceResolution.y > 0f ? 1080f / s.referenceResolution.y : 1f, null, null);
        EditorSceneManager.MarkSceneDirty(homeUI.gameObject.scene);
        Debug.Log("[TimeDesk] Home UI built and wired. Save the scene.");
    }

    /// <summary>The expenses panel's size (reference px): tall enough for the evening's lines (the break-in, the bill, the mood, tonight's changes) over the family rows.</summary>
    private static readonly Vector2 ExpensesPanelSize = new Vector2(900f, 780f);

    /// <summary>The House panel's size (reference px): five category columns of cards, four rows deep, over the detail strip.</summary>
    private static readonly Vector2 HousePanelSize = new Vector2(1840f, 920f);

    /// <summary>The House panel's offset (reference px): lowered so its top clears the HUD strip.</summary>
    private static readonly Vector2 HousePanelOffset = new Vector2(0f, -30f);

    /// <summary>
    /// Re-applies the canvas's scaling on every build: Scale With Screen Size
    /// from 1920 x 1080, as SceneUiKit makes a new canvas and as the Title's
    /// is, so the Home panels (the House is 1840 x 920) shrink with a 720p
    /// screen instead of spilling off it (the scene's hand-made canvas kept a
    /// constant pixel size from its first 800 x 600 layout).
    /// </summary>
    private static void EnsureScaler(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
    }

    /// <summary>Re-applies the expenses panel's layout (its size and its parts' anchors) on every build.</summary>
    private static void LayOutExpenses(Transform panel, TMP_Text title, TMP_Text body, Transform rows, Button next)
    {
        ((RectTransform)panel).sizeDelta = ExpensesPanelSize;
        Stretch(title.rectTransform, new Vector2(0.05f, 0.875f), new Vector2(0.95f, 0.955f));
        Stretch(body.rectTransform, new Vector2(0.06f, 0.44f), new Vector2(0.94f, 0.86f));
        Stretch((RectTransform)rows, new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.42f));
        Stretch((RectTransform)next.transform, new Vector2(0.33f, 0.03f), new Vector2(0.67f, 0.11f));
    }

    /// <summary>
    /// Re-applies the House panel's layout on every build: its size, the
    /// title, the wallet line, the tree's area, the detail strip (in the
    /// panel's own ink: the wallet line's colour, dark on the art's cream
    /// panel), Buy and Continue; and removes the old shop's rows container,
    /// which the tree replaced.
    /// </summary>
    private static void LayOutHouse(Transform panel, TMP_Text title, TMP_Text body, RectTransform tree, TMP_Text detail, Button buy, Button next)
    {
        ((RectTransform)panel).sizeDelta = HousePanelSize;
        ((RectTransform)panel).anchoredPosition = HousePanelOffset;
        Stretch(title.rectTransform, new Vector2(0.05f, 0.895f), new Vector2(0.95f, 0.955f));
        title.text = "House";
        Stretch(body.rectTransform, new Vector2(0.03f, 0.85f), new Vector2(0.97f, 0.89f));
        Stretch(tree, new Vector2(0.025f, 0.3f), new Vector2(0.975f, 0.845f));
        Stretch(detail.rectTransform, new Vector2(0.03f, 0.115f), new Vector2(0.8f, 0.285f));
        detail.color = body.color;
        detail.fontSize = 24;
        detail.textWrappingMode = TextWrappingModes.Normal;
        Stretch((RectTransform)buy.transform, new Vector2(0.83f, 0.15f), new Vector2(0.97f, 0.25f));
        Stretch((RectTransform)next.transform, new Vector2(0.4f, 0.02f), new Vector2(0.6f, 0.085f));

        Transform oldRows = panel.Find("ShopRows");
        if (oldRows != null)
            Undo.DestroyObjectImmediate(oldRows.gameObject);
    }

    /// <summary>Finds a child area by name or creates an empty one (the House's tree: the cards and connectors are spawned into it at runtime).</summary>
    private static RectTransform FindOrCreateArea(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return (RectTransform)existing;

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return (RectTransform)go.transform;
    }

    /// <summary>The slot machine's box (reference px) above the slot panel, at the landscape art's aspect (1000 x 640), clear of the HUD.</summary>
    private static readonly Vector2 SlotMachineSize = new Vector2(375f, 240f);

    /// <summary>The slot machine's lever (reference px), standing at the machine's right edge.</summary>
    private static readonly Vector2 SlotLeverSize = new Vector2(80f, 240f);

    /// <summary>The gap (reference px) between the slot panel's top and the machine and lever above it.</summary>
    private const float SlotArtGap = 8f;

    /// <summary>
    /// The slot panel's art slots (redesign phase 27): the landscape machine
    /// standing on the panel's top edge (above it, so the panel's white texts
    /// stay on the dark panel) and the lever at the machine's right edge; both
    /// full colour and hidden until their art exists.
    /// </summary>
    private static void BuildSlotMachineArt(Transform slot)
    {
        Image machine = FindOrCreateImage(slot, "Machine", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                                          new Vector2(0f, SlotArtGap), SlotMachineSize);
        EnsureArtSlot(machine, ArtSlots.SlotMachine, null, true, true);
        Image lever = FindOrCreateImage(slot, "Lever", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f),
                                        new Vector2(SlotMachineSize.x / 2f + SlotArtGap / 2f, SlotArtGap), SlotLeverSize);
        EnsureArtSlot(lever, ArtSlots.SlotLever, null, true, true);
    }

    /// <summary>The HUD's height on its dark strip (reference px), centred on the HUD's texts.</summary>
    private const float HudBackingHeight = 64f;

    /// <summary>The HUD's strip: dark and mostly opaque, so the white day, wallet and stability read over any part of the room's art.</summary>
    private static readonly Color HudBackingColour = new Color(0.06f, 0.07f, 0.1f, 0.72f);

    /// <summary>The HUD's own height (reference px), re-applied: its texts stretch over it, so the wallet line, which shrinks to fit (UiText.FitLabel), keeps its 28 px instead of shrinking to its floor in a HUD of no height.</summary>
    private const float HudHeight = 50f;

    /// <summary>
    /// The HUD's backing strip, drawn first under the HUD (full width,
    /// <see cref="HudBackingHeight"/> tall, centred on its texts; no raycasts):
    /// created once, its place and colour re-applied on every build, with the
    /// HUD's own height (<see cref="HudHeight"/>).
    /// </summary>
    private static void EnsureHudBacking(Transform hud)
    {
        Transform existing = hud.Find("Backing");
        GameObject go = existing != null ? existing.gameObject : new GameObject("Backing", typeof(RectTransform));
        if (existing == null)
        {
            go.transform.SetParent(hud, false);
            Undo.RegisterCreatedObjectUndo(go, "Create HUD Backing");
        }
        go.transform.SetAsFirstSibling();
        ((RectTransform)hud).sizeDelta = new Vector2(0f, HudHeight);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, HudBackingHeight);

        Image img = go.GetComponent<Image>();
        if (img == null)
            img = go.AddComponent<Image>();
        img.color = HudBackingColour;
        img.raycastTarget = false;
    }

    /// <summary>
    /// Finds a child rows container by name or creates one with a
    /// VerticalLayoutGroup, ready for runtime-spawned rows
    /// (HomeUIController.CreateRow/CreateLabelRow: the family's).
    /// </summary>
    private static Transform FindOrCreateRowsContainer(
        Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        Transform existing = parent.Find(name);

        if (existing != null)
            return existing;

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Stretch((RectTransform)go.transform, anchorMin, anchorMax);

        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = false;
        layout.childControlWidth = true;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go.transform;
    }
}
