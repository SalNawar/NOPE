using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static SceneUiKit;

/// <summary>
/// One-click builder for the Home scene UI added in Alpha Phase 4: the HUD's
/// readouts (day and wallet, and the place the Helix River takes: the
/// timeline's stability is never a number), the bills panel (the fixed costs,
/// the pet's needs, one row per night's bill with its Paying / Skip pair; the
/// Home pet spec), the pet's corner (the pet drawn by PetStandIn, Pet, the
/// toys' rows), the House (Home's upgrade tree beside its detail card; the
/// Home upgrades spec §6), the night slots (three reels, the SPIN dome and the
/// lever) and the sleep prompt. Every panel is drawn in the cel UI kit
/// (docs/UI_KIT.md, sheet 04; KitScreens): kit panels, plates and readouts
/// with live TMP labels, laid out and skinned again on every build. Unlike
/// OfficeSceneUIBuilder, this script creates the Canvas, EventSystem,
/// HomeManager and HomeUIController objects from scratch if they don't
/// already exist (HomeScene starts as an empty scene); the rest is found by
/// name or created (through the shared SceneUiKit); it then checks every
/// text's contrast on what it is drawn on (UiContrastCheck).
/// </summary>
public static class HomeSceneBuilder
{
    /// <summary>Builds and wires the Home UI in the open scene.</summary>
    [MenuItem("Tools/TimeDesk/Build Home UI (Panels + Wiring)")]
    public static void Build()
    {
        UiKitSO kit = UiKitAssets.Ensure();
        if (kit == null)
            return;

        // --- Canvas + EventSystem ---
        Canvas canvas = SceneUiKit.EnsureCanvasAndEventSystem();
        EnsureScaler(canvas);
        Transform root = canvas.transform;

        // --- HomeManager (logic) + HomeUIController (UI) ---
        HomeManager homeManager = SceneUiKit.FindOrCreateObject<HomeManager>("HomeManager");
        HomeUIController homeUI = SceneUiKit.FindOrCreateStretched<HomeUIController>(root, "HomeUI");

        Transform uiRoot = homeUI.transform;

        // --- HUD: the day and wallet readouts, and the Helix River's place ---
        Transform hud = FindOrCreatePanel(uiRoot, "HUD", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -50f), new Vector2(0f, 0f), withBackground: false);
        TMP_Text dayText = FindOrCreateText(Within(hud, "DayReadout"), "DayText", "Day 1", 28, TextAlignmentOptions.Left,
            new Vector2(0f, 0f), new Vector2(0.2f, 1f));
        TMP_Text moneyText = FindOrCreateText(Within(hud, "WalletReadout"), "MoneyText", "0", 28, TextAlignmentOptions.Center,
            new Vector2(0.4f, 0f), new Vector2(0.6f, 1f));
        // The wallet reads the leading culture's currency (UiText): themed like the office tray's, so a culture's own script finds its font.
        Tag(moneyText, ThemeRoleId.Tray, ThemePart.Ink, fit: true);
        LayOutHud(hud, kit, dayText, moneyText);
        // The Helix River fills the HUD's StabilitySlot (stability is never a number).
        HelixRiverAuthoring.UiRiver(hud.Find("StabilitySlot"), "River", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, HomeRiverZoom, 0.18f, false);

        // --- Expenses panel (the bills) ---
        Transform expenses = FindOrCreatePanel(uiRoot, "ExpensesPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, ExpensesPanelSize, withBackground: true);
        TMP_Text expensesTitle = FindOrCreateText(expenses, "TitleText", "Day 1 — Home", 34,
            TextAlignmentOptions.Left, new Vector2(0.05f, 0.875f), new Vector2(0.95f, 0.955f));
        TMP_Text expensesBody = FindOrCreateText(expenses, "BodyText", "...", 22,
            TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.44f), new Vector2(0.94f, 0.86f));
        RenameChild(expenses, "FamilyRows", "BillRows");
        Transform billRows = FindOrCreateRowsContainer(expenses, "BillRows",
            new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.44f));
        Button expensesContinue = FindOrCreateButton(expenses, "ContinueButton", "Pay and see your pet",
            new Vector2(0.33f, 0.03f), new Vector2(0.67f, 0.1f));
        LayOutExpenses(expenses, kit, expensesTitle, expensesBody, billRows, expensesContinue);

        // --- The pet's corner (the Home pet spec PS7) ---
        Transform pet = FindOrCreatePanel(uiRoot, "PetPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, PetPanelSize, withBackground: true);
        TMP_Text petTitle = FindOrCreateText(pet, "TitleText", "Your pet's corner", 34,
            TextAlignmentOptions.Left, new Vector2(0.05f, 0.89f), new Vector2(0.95f, 0.97f));
        RectTransform petViewArea = FindOrCreateArea(Within(pet, "PetFrame"), "PetView");
        PetStandIn petView = petViewArea.GetComponent<PetStandIn>();
        if (petView == null)
            petView = Undo.AddComponent<PetStandIn>(petViewArea.gameObject);
        TMP_Text petBody = FindOrCreateText(pet, "BodyText", "...", 24,
            TextAlignmentOptions.TopLeft, new Vector2(0.55f, 0.6f), new Vector2(0.96f, 0.86f));
        TMP_Text petReaction = FindOrCreateText(pet, "ReactionText", "", 22,
            TextAlignmentOptions.TopLeft, new Vector2(0.55f, 0.47f), new Vector2(0.96f, 0.59f));
        Button petPat = FindOrCreateButton(pet, "PatButton", "Pet",
            new Vector2(0.55f, 0.38f), new Vector2(0.8f, 0.46f));
        Transform toyRows = FindOrCreateRowsContainer(pet, "ToyRows",
            new Vector2(0.55f, 0.13f), new Vector2(0.96f, 0.36f));
        Button petContinue = FindOrCreateButton(pet, "ContinueButton", "Continue to the House",
            new Vector2(0.35f, 0.03f), new Vector2(0.65f, 0.1f));
        LayOutPet(pet, kit, petTitle, petViewArea, petView, petBody, petReaction, petPat, toyRows, petContinue);

        // --- House panel (the old shop panel) ---
        Transform shop = FindOrCreatePanel(uiRoot, "ShopPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, HousePanelSize, withBackground: true);
        TMP_Text shopTitle = FindOrCreateText(shop, "TitleText", "The House", 34,
            TextAlignmentOptions.Left, new Vector2(0.05f, 0.895f), new Vector2(0.95f, 0.955f));
        TMP_Text shopBody = FindOrCreateText(shop, "BodyText", "...", 22,
            TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.85f), new Vector2(0.97f, 0.89f));
        RectTransform houseTree = FindOrCreateArea(shop, "HouseTree");
        Transform detailCard = FindOrCreatePanel(shop, "DetailCard", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, withBackground: true);
        Image detailIcon = FindOrCreateImage(detailCard, "DetailIcon", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        TMP_Text houseDetail = ReparentedText(shop, detailCard, "DetailText");
        Button houseBuy = ReparentedButton(shop, detailCard, "BuyButton", "Buy");
        Button shopContinue = ReparentedButton(shop, detailCard, "ContinueButton", "Continue to Slots");
        LayOutHouse(shop, kit, shopTitle, shopBody, houseTree, detailCard, detailIcon, houseDetail, houseBuy, shopContinue);

        // --- Slot panel ---
        Transform slot = FindOrCreatePanel(uiRoot, "SlotPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, SlotPanelSize, withBackground: true);
        TMP_Text slotTitle = FindOrCreateText(slot, "TitleText", "Night Slots", 34,
            TextAlignmentOptions.Left, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.98f));
        TMP_Text slotBody = FindOrCreateText(slot, "BodyText", "...", 24,
            TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.4f), new Vector2(0.94f, 0.82f));
        Button slotSpin = FindOrCreateButton(slot, "SpinButton", "Spin",
            new Vector2(0.1f, 0.2f), new Vector2(0.45f, 0.34f));
        Button slotContinue = FindOrCreateButton(slot, "ContinueButton", "Continue",
            new Vector2(0.55f, 0.2f), new Vector2(0.9f, 0.34f));
        Image[] reelFaces = LayOutSlot(slot, kit, slotTitle, slotBody, slotSpin, slotContinue);

        // --- Sleep panel ---
        Transform sleep = FindOrCreatePanel(uiRoot, "SleepPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, SleepPanelSize, withBackground: true);
        TMP_Text sleepTitle = FindOrCreateText(sleep, "TitleText", "Day 1 — Turn In", 34,
            TextAlignmentOptions.Left, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.98f));
        TMP_Text sleepBody = FindOrCreateText(sleep, "BodyText", "...", 24,
            TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.3f), new Vector2(0.94f, 0.82f));
        Button sleepButton = FindOrCreateButton(sleep, "SleepButton", "Sleep",
            new Vector2(0.3f, 0.06f), new Vector2(0.7f, 0.24f));
        LayOutSleep(sleep, kit, sleepTitle, sleepBody, sleepButton);

        // --- Wire HomeUIController ---
        var soUi = new SerializedObject(homeUI);
        soUi.FindProperty("kit").objectReferenceValue = kit;
        soUi.FindProperty("moneyText").objectReferenceValue = moneyText;
        soUi.FindProperty("dayText").objectReferenceValue = dayText;

        soUi.FindProperty("expensesPanel").objectReferenceValue = expenses.gameObject;
        soUi.FindProperty("expensesTitleText").objectReferenceValue = expensesTitle;
        soUi.FindProperty("expensesBodyText").objectReferenceValue = expensesBody;
        soUi.FindProperty("billRowsRoot").objectReferenceValue = billRows;
        soUi.FindProperty("expensesContinueButton").objectReferenceValue = expensesContinue;

        soUi.FindProperty("petPanel").objectReferenceValue = pet.gameObject;
        soUi.FindProperty("petTitleText").objectReferenceValue = petTitle;
        soUi.FindProperty("petBodyText").objectReferenceValue = petBody;
        soUi.FindProperty("petView").objectReferenceValue = petView;
        soUi.FindProperty("petReactionText").objectReferenceValue = petReaction;
        soUi.FindProperty("petPatButton").objectReferenceValue = petPat;
        soUi.FindProperty("toyRowsRoot").objectReferenceValue = toyRows;
        soUi.FindProperty("petContinueButton").objectReferenceValue = petContinue;

        soUi.FindProperty("shopPanel").objectReferenceValue = shop.gameObject;
        soUi.FindProperty("shopTitleText").objectReferenceValue = shopTitle;
        soUi.FindProperty("shopBodyText").objectReferenceValue = shopBody;
        soUi.FindProperty("houseTreeRoot").objectReferenceValue = houseTree;
        soUi.FindProperty("houseDetailIcon").objectReferenceValue = detailIcon;
        soUi.FindProperty("houseDetailText").objectReferenceValue = houseDetail;
        soUi.FindProperty("houseBuyButton").objectReferenceValue = houseBuy;
        soUi.FindProperty("shopContinueButton").objectReferenceValue = shopContinue;

        soUi.FindProperty("slotPanel").objectReferenceValue = slot.gameObject;
        soUi.FindProperty("slotTitleText").objectReferenceValue = slotTitle;
        soUi.FindProperty("slotBodyText").objectReferenceValue = slotBody;
        soUi.FindProperty("slotSpinButton").objectReferenceValue = slotSpin;
        soUi.FindProperty("slotContinueButton").objectReferenceValue = slotContinue;
        SerializedArrays.Set(soUi, "slotReelFaces", reelFaces);

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
        pet.gameObject.SetActive(false);
        shop.gameObject.SetActive(false);
        slot.gameObject.SetActive(false);
        sleep.gameObject.SetActive(false);

        HelixRiverAuthoring.Wire(homeUI.gameObject.scene, null);
        UiContrastCheck.Check(canvas, canvas.GetComponent<CanvasScaler>() is CanvasScaler s && s.referenceResolution.y > 0f ? 1080f / s.referenceResolution.y : 1f, null, null);
        EditorSceneManager.MarkSceneDirty(homeUI.gameObject.scene);
        Debug.Log("[TimeDesk] Home UI built and wired. Save the scene.");
    }

    /// <summary>The bills panel's size (reference px): the title, the evening's lines (the break-in, the fixed costs, the pet's needs, the bills' total), the five bills' rows and Pay.</summary>
    private static readonly Vector2 ExpensesPanelSize = new Vector2(1000f, 900f);

    /// <summary>The pet's corner's size (reference px): the pet on the left, its needs, reaction and the toys on the right, Pet and Continue at the foot.</summary>
    private static readonly Vector2 PetPanelSize = new Vector2(1180f, 800f);

    /// <summary>The House panel's size (reference px): five category columns of cards, four rows deep, beside the detail card.</summary>
    private static readonly Vector2 HousePanelSize = new Vector2(1840f, 920f);

    /// <summary>The night slots' size (reference px): the three reels, the dome and the lever over the result line and Continue.</summary>
    private static readonly Vector2 SlotPanelSize = new Vector2(860f, 500f);

    /// <summary>The sleep prompt's size (reference px).</summary>
    private static readonly Vector2 SleepPanelSize = new Vector2(800f, 360f);

    /// <summary>Every panel's offset (reference px): lowered so its top clears the HUD's readouts.</summary>
    private static readonly Vector2 PanelOffset = new Vector2(0f, -30f);

    /// <summary>The inset of every panel's content from its edges (reference px).</summary>
    private const float Inset = 36f;

    /// <summary>A panel's heading: its box's height and size range.</summary>
    private const float HeadingHeight = 56f, HeadingMax = 42f, HeadingMin = 26f;

    /// <summary>A primary plate's height (reference px, as seen).</summary>
    private const float PlateHeight = 66f;

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

    /// <summary>How much of the Helix River's glass the HUD's slot shows: a thin strip, so closer.</summary>
    private const float HomeRiverZoom = 2f;

    /// <summary>The HUD's height (reference px), along the top of the screen.</summary>
    private const float HudHeight = 50f;

    /// <summary>The HUD's readouts' size (reference px, as seen): the day's, the wallet's, and the place the Helix River takes.</summary>
    private static readonly Vector2 DayReadoutSize = new Vector2(150f, 46f), WalletReadoutSize = new Vector2(220f, 46f), RiverSize = new Vector2(280f, 46f);

    /// <summary>
    /// Re-applies the HUD on every build: along the top of the screen, the
    /// day's and the wallet's phosphor readouts (the kit's lcd_glass, their
    /// texts moved into them) at the top left, and at the top right the
    /// StabilitySlot, where Build fills the Helix River (the timeline's
    /// stability is never shown as a number). The old
    /// dark strip and the stability line are removed.
    /// </summary>
    private static void LayOutHud(Transform hud, UiKitSO kit, TMP_Text day, TMP_Text money)
    {
        var rt = (RectTransform)hud;
        Undo.RecordObject(rt, "Lay out the HUD");
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -12f);
        rt.sizeDelta = new Vector2(0f, HudHeight);
        KitScreens.Remove(hud, "Backing");
        KitScreens.Remove(hud, "StabilityText");
        KitScreens.Remove(hud, "TimelineStrip");

        KitScreens.Readout(hud, "DayReadout", kit, day, 24f, 2f, DayReadoutSize);
        KitScreens.Readout(hud, "WalletReadout", kit, money, 24f + DayReadoutSize.x + 16f, 2f, WalletReadoutSize);

        Transform river = hud.Find("StabilitySlot");
        if (river == null)
        {
            var go = new GameObject("StabilitySlot", typeof(RectTransform));
            go.transform.SetParent(hud, false);
            Undo.RegisterCreatedObjectUndo(go, "Create the stability slot");
            river = go.transform;
        }
        var riverRect = (RectTransform)river;
        Undo.RecordObject(riverRect, "Lay out the HUD");
        riverRect.anchorMin = riverRect.anchorMax = riverRect.pivot = new Vector2(1f, 1f);
        riverRect.anchoredPosition = new Vector2(-24f, -2f);
        riverRect.sizeDelta = RiverSize;
    }

    /// <summary>
    /// Re-applies the bills panel on every build (sheet 04's DAY 8 — HOME): the
    /// kit's manila panel; its heading at the top left; the evening's lines
    /// (shrinking from 21 to 16 reference px); the bills' rows (HomeUIController
    /// spawns them: tile, name, price line, Paying / Skip); Pay on the oxblood
    /// primary plate at the foot.
    /// </summary>
    private static void LayOutExpenses(Transform panel, UiKitSO kit, TMP_Text title, TMP_Text body, Transform rows, Button next)
    {
        KitScreens.Size(panel, ExpensesPanelSize, PanelOffset);
        KitScreens.Panel(panel, kit, "panel_manila");
        KitScreens.Across(title.rectTransform, Inset, Inset, 26f, HeadingHeight);
        KitScreens.Label(title, kit, kit.inkOnLight, HeadingMax, HeadingMin);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        KitScreens.Across(body.rectTransform, Inset, Inset, 92f, 290f);
        KitScreens.Body(body, kit.inkOnLight, 21f, 16f);
        KitScreens.Across((RectTransform)rows, Inset, Inset, 396f, 5f * 62f + 4f * RowSpacing);
        var layout = rows.GetComponent<VerticalLayoutGroup>();
        Undo.RecordObject(layout, "Lay out the bills");
        layout.spacing = RowSpacing;
        KitScreens.PlaceBottomLeft((RectTransform)next.transform, (ExpensesPanelSize.x - 480f) / 2f, 30f, new Vector2(480f, PlateHeight));
        KitScreens.Plate(next, kit, "plate_ox");
    }

    /// <summary>The space between the bills' and the toys' rows (reference px).</summary>
    private const float RowSpacing = 10f;

    /// <summary>The pet's frame (reference px, as seen): the kit's content well around the pet, at the left of the corner.</summary>
    private static readonly Vector2 PetFrameSize = new Vector2(520f, 560f);

    /// <summary>The manila the pet stands on when the corner has no backdrop art (the kit's manila).</summary>
    private static readonly Color PetPlate = new Color(0.886f, 0.788f, 0.58f, 1f);

    /// <summary>
    /// Re-applies the pet's corner on every build (sheet 04's pet corner): the
    /// kit's bone panel; its heading; the pet in the kit's content well at the
    /// left (on the kit's manila when the corner has no backdrop); at the right
    /// its needs, its reaction (italic) and the toys' rows; Pet (slate) and
    /// Continue (oxblood) at the foot.
    /// </summary>
    private static void LayOutPet(Transform panel, UiKitSO kit, TMP_Text title, RectTransform view, PetStandIn standIn, TMP_Text body, TMP_Text reaction,
                                  Button pat, Transform toys, Button next)
    {
        KitScreens.Size(panel, PetPanelSize, PanelOffset);
        KitScreens.Panel(panel, kit, "panel_bone");
        KitScreens.Across(title.rectTransform, Inset, Inset, 26f, HeadingHeight);
        KitScreens.Label(title, kit, kit.inkOnLight, HeadingMax, HeadingMin);
        title.alignment = TextAlignmentOptions.MidlineLeft;

        Transform frame = FindOrCreatePanel(panel, "PetFrame", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, withBackground: true);
        KitScreens.Place((RectTransform)frame, Inset, 98f, PetFrameSize);
        KitScreens.Panel(frame, kit, "content_inset");
        if (view.parent != frame)
            Undo.SetTransformParent(view, frame, "Lay out the pet");
        Undo.RecordObject(view, "Lay out the pet");
        Stretch(view, Vector2.zero, Vector2.one);
        view.offsetMin = new Vector2(12f, 12f);
        view.offsetMax = new Vector2(-12f, -12f);
        var so = new SerializedObject(standIn);
        so.FindProperty("plateColour").colorValue = PetPlate;
        so.ApplyModifiedProperties();

        float right = Inset + PetFrameSize.x + 32f;
        KitScreens.Across(body.rectTransform, right, Inset, 98f, 180f);
        KitScreens.Body(body, kit.inkOnLight, 24f, 18f);
        KitScreens.Across(reaction.rectTransform, right, Inset, 286f, 76f);
        KitScreens.Body(reaction, kit.inkOnLight, 22f, 17f);
        reaction.fontStyle = FontStyles.Italic;
        KitScreens.Across((RectTransform)toys, right, Inset, 376f, 4f * 62f + 3f * RowSpacing);
        var layout = toys.GetComponent<VerticalLayoutGroup>();
        Undo.RecordObject(layout, "Lay out the toys");
        layout.spacing = RowSpacing;

        KitScreens.PlaceBottomLeft((RectTransform)pat.transform, Inset, 30f, new Vector2(PetFrameSize.x, PlateHeight));
        KitScreens.Plate(pat, kit, "plate_slate");
        KitScreens.PlaceBottomRight((RectTransform)next.transform, Inset, 30f, new Vector2(PetPanelSize.x - right - Inset, PlateHeight));
        KitScreens.Plate(next, kit, "plate_ox");
    }

    /// <summary>The child <paramref name="name"/> of <paramref name="parent"/> when it exists (a part the kit layout moved a text or the pet into), else <paramref name="parent"/> itself.</summary>
    private static Transform Within(Transform parent, string name)
    {
        Transform inner = parent.Find(name);
        return inner != null ? inner : parent;
    }

    /// <summary>Renames <paramref name="from"/> under <paramref name="parent"/> to <paramref name="to"/> when only the old name exists (a scene built before the rename keeps its object).</summary>
    private static void RenameChild(Transform parent, string from, string to)
    {
        Transform old = parent.Find(from);
        if (old == null || parent.Find(to) != null)
            return;
        Undo.RecordObject(old.gameObject, $"Rename {from}");
        old.name = to;
    }

    /// <summary>The House's detail card's width (reference px, as seen), at the panel's right.</summary>
    private const float DetailCardWidth = 400f;

    /// <summary>The detail card's tile (reference px, as seen).</summary>
    private const float DetailIconSize = 76f;

    /// <summary>
    /// Re-applies the House panel on every build (sheet 03's THE HOUSE): the
    /// kit's manila panel, its heading and the wallet line beside it, the tree
    /// (HomeUIController spawns the slate heads, links and kit cards into it)
    /// at the left, and at the right the detail card (the kit's card panel:
    /// the selected upgrade's tile and its lines, Buy on the oxblood plate and
    /// Continue on the slate one); the old shop's rows container, which the
    /// tree replaced, is removed.
    /// </summary>
    private static void LayOutHouse(Transform panel, UiKitSO kit, TMP_Text title, TMP_Text body, RectTransform tree, Transform card, Image icon, TMP_Text detail,
                                    Button buy, Button next)
    {
        KitScreens.Size(panel, HousePanelSize, PanelOffset);
        KitScreens.Panel(panel, kit, "panel_manila");
        KitScreens.Place(title.rectTransform, Inset, 24f, new Vector2(420f, HeadingHeight));
        KitScreens.Label(title, kit, kit.inkOnLight, HeadingMax, HeadingMin);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        KitScreens.Place(body.rectTransform, Inset + 440f, 34f, new Vector2(HousePanelSize.x - 2f * Inset - 440f - DetailCardWidth - 24f, 40f));
        KitScreens.Body(body, kit.inkOnLight, 22f, 16f, TextAlignmentOptions.MidlineLeft);
        body.textWrappingMode = TextWrappingModes.NoWrap;

        float treeWidth = HousePanelSize.x - 2f * Inset - DetailCardWidth - 30f;
        KitScreens.Place(tree, Inset, 100f, new Vector2(treeWidth, HousePanelSize.y - 100f - Inset));

        float cardHeight = HousePanelSize.y - 100f - Inset;
        KitScreens.Place((RectTransform)card, HousePanelSize.x - Inset - DetailCardWidth, 100f, new Vector2(DetailCardWidth, cardHeight));
        KitScreens.Panel(card, kit, "panel_card");
        KitScreens.Place(icon.rectTransform, 28f, 28f, new Vector2(DetailIconSize, DetailIconSize));
        Undo.RecordObject(icon, "Lay out the House");
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        KitScreens.Across(detail.rectTransform, 28f, 28f, 28f + DetailIconSize + 18f, cardHeight - (28f + DetailIconSize + 18f) - 2f * (PlateHeight + 14f) - 28f);
        KitScreens.Body(detail, kit.inkOnLight, 22f, 16f);
        KitScreens.PlaceBottomLeft((RectTransform)buy.transform, 28f, 28f + PlateHeight + 14f, new Vector2(DetailCardWidth - 56f, PlateHeight));
        KitScreens.Plate(buy, kit, "plate_ox");
        KitScreens.PlaceBottomLeft((RectTransform)next.transform, 28f, 28f, new Vector2(DetailCardWidth - 56f, PlateHeight));
        KitScreens.Plate(next, kit, "plate_slate", 26f);

        KitScreens.Remove(panel, "ShopRows");
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

    /// <summary>The House's text <paramref name="name"/>, found under the detail card or moved there from the panel (a scene built before the card), or created in the card.</summary>
    private static TMP_Text ReparentedText(Transform panel, Transform card, string name)
    {
        Transform old = panel.Find(name);
        if (old != null && card.Find(name) == null)
            Undo.SetTransformParent(old, card, "Move into the detail card");
        return FindOrCreateText(card, name, "...", 22, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one);
    }

    /// <summary>The House's button <paramref name="name"/>, found under the detail card or moved there from the panel (a scene built before the card), or created in the card.</summary>
    private static Button ReparentedButton(Transform panel, Transform card, string name, string label)
    {
        Transform old = panel.Find(name);
        if (old != null && card.Find(name) == null)
            Undo.SetTransformParent(old, card, "Move into the detail card");
        return FindOrCreateButton(card, name, label, Vector2.zero, Vector2.one);
    }

    /// <summary>A reel window's size (reference px, as seen) and the symbol tile inside it.</summary>
    private static readonly Vector2 ReelSize = new Vector2(132f, 168f);

    /// <summary>The symbol tile inside a reel (reference px, as seen).</summary>
    private const float ReelSymbolSize = 92f;

    /// <summary>The SPIN dome's size (reference px, as seen) and the lever's.</summary>
    private static readonly Vector2 DomeSize = new Vector2(150f, 150f), LeverSize = new Vector2(54f, 170f);

    /// <summary>
    /// Re-applies the night slots on every build (sheet 04's NIGHT SLOTS): the
    /// kit's dark plum panel; its heading; three reel windows each holding a
    /// symbol tile (HomeUIController spins and lands them); the SPIN dome (its
    /// pressed face swapped in) and the lever beside it; the result line under
    /// them; Continue on the bone plate at the foot. The slot machine's older
    /// art slots (the landscape machine and its lever above the panel) are
    /// removed: the kit's reels, dome and lever replace them. Returns the
    /// reels' symbol images.
    /// </summary>
    private static Image[] LayOutSlot(Transform panel, UiKitSO kit, TMP_Text title, TMP_Text body, Button spin, Button next)
    {
        KitScreens.Size(panel, SlotPanelSize, PanelOffset);
        KitScreens.Panel(panel, kit, "panel_dark");
        KitScreens.Remove(panel, "Machine");
        KitScreens.Remove(panel, "Lever");
        KitScreens.Across(title.rectTransform, Inset, Inset, 24f, HeadingHeight);
        KitScreens.Label(title, kit, kit.inkOnDark, HeadingMax, HeadingMin);
        title.alignment = TextAlignmentOptions.MidlineLeft;

        var faces = new Image[SlotReels.Count];
        for (int i = 0; i < SlotReels.Count; i++)
        {
            Transform reel = FindOrCreatePanel(panel, "Reel" + (i + 1), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, withBackground: true);
            KitScreens.Place((RectTransform)reel, Inset + 10f + i * (ReelSize.x + 18f), 100f, ReelSize);
            KitScreens.Panel(reel, kit, "slot_reel");
            Image host = KitScreens.Picture(reel, "Symbol", kit, ReelSymbols[i], (ReelSize.x - ReelSymbolSize) / 2f, (ReelSize.y - ReelSymbolSize) / 2f,
                                            new Vector2(ReelSymbolSize, ReelSymbolSize));
            faces[i] = host;
        }

        float domeLeft = Inset + 10f + SlotReels.Count * (ReelSize.x + 18f) + 22f;
        KitScreens.Place((RectTransform)spin.transform, domeLeft, 108f, DomeSize);
        KitScreens.Plate(spin, kit, "dome_red", 34f, kit.inkOnDark);
        Image dome = spin.transform.Find(SceneUiKit.KitFaceName)?.GetComponent<Image>();
        Undo.RecordObject(spin, "Lay out the slots");
        spin.transition = Selectable.Transition.SpriteSwap;
        spin.spriteState = new SpriteState { pressedSprite = kit.Get("dome_red_pressed") };
        if (dome != null)
            spin.targetGraphic = dome;
        KitScreens.Picture(panel, "KitLever", kit, "slot_lever", domeLeft + DomeSize.x + 20f, 80f, LeverSize);

        KitScreens.Across(body.rectTransform, Inset, Inset, 296f, 90f);
        KitScreens.Body(body, kit.inkOnDark, 24f, 17f);
        KitScreens.PlaceBottomRight((RectTransform)next.transform, Inset, 28f, new Vector2(280f, 60f));
        KitScreens.Plate(next, kit, "plate_bone", 26f);
        return faces;
    }

    /// <summary>The reels' symbols as built (HomeUIController's first faces; sheet 04).</summary>
    private static readonly string[] ReelSymbols = { "tile_crate_rest", "tile_star_rest", "tile_bolt_rest" };

    /// <summary>The moon tile on the sleep prompt (reference px, as seen).</summary>
    private const float MoonSize = 96f;

    /// <summary>
    /// Re-applies the sleep prompt on every build (sheet 04's DAY 8 — TURN IN):
    /// the kit's slate panel, the moon tile at the left, the heading and the
    /// night's lines beside it in bone, and Sleep on the oxblood plate at the
    /// foot right.
    /// </summary>
    private static void LayOutSleep(Transform panel, UiKitSO kit, TMP_Text title, TMP_Text body, Button sleep)
    {
        KitScreens.Size(panel, SleepPanelSize, PanelOffset);
        KitScreens.Panel(panel, kit, "panel_slate");
        KitScreens.Picture(panel, "Moon", kit, "tile_moon_rest", Inset, 34f, new Vector2(MoonSize, MoonSize));
        float left = Inset + MoonSize + 28f;
        KitScreens.Across(title.rectTransform, left, Inset, 30f, HeadingHeight);
        KitScreens.Label(title, kit, kit.inkOnDark, HeadingMax, HeadingMin);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        KitScreens.Across(body.rectTransform, left, Inset, 96f, 140f);
        KitScreens.Body(body, kit.inkOnDark, 22f, 16f);
        KitScreens.PlaceBottomRight((RectTransform)sleep.transform, Inset, 30f, new Vector2(300f, PlateHeight));
        KitScreens.Plate(sleep, kit, "plate_ox");
    }

    /// <summary>
    /// Finds a child rows container by name or creates one with a
    /// VerticalLayoutGroup, ready for runtime-spawned rows
    /// (HomeUIController's bills' and toys' rows).
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
        layout.spacing = RowSpacing;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = false;
        layout.childControlWidth = true;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go.transform;
    }
}
