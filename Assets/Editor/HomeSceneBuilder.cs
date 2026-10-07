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
/// Home upgrades spec §6), the Night Slots machine (its cabinet, marquee and
/// bulbs, three reels behind glass, deck, tray and lever: SlotMachineView) and
/// the sleep prompt. Every panel is drawn in the cel UI kit
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

        // --- The Night Slots machine (SlotMachineView) ---
        Transform slot = FindOrCreatePanel(uiRoot, "SlotPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, withBackground: true);
        Button slotContinue = FindOrCreateButton(slot, "ContinueButton", "Continue",
            new Vector2(0.55f, 0.2f), new Vector2(0.9f, 0.34f));
        SlotMachineView slotMachine = LayOutSlot(slot, kit, slotContinue);

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
        soUi.FindProperty("backdrop").objectReferenceValue = Backdrop(root);
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
        soUi.FindProperty("slotMachine").objectReferenceValue = slotMachine;
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
        pet.gameObject.SetActive(false);
        shop.gameObject.SetActive(false);
        slot.gameObject.SetActive(false);
        sleep.gameObject.SetActive(false);

        HelixRiverAuthoring.Wire(homeUI.gameObject.scene, null);
        UiContrastCheck.Check(canvas, canvas.GetComponent<CanvasScaler>() is CanvasScaler s && s.referenceResolution.y > 0f ? 1080f / s.referenceResolution.y : 1f, null, null);
        EditorSceneManager.MarkSceneDirty(homeUI.gameObject.scene);
        Debug.Log("[TimeDesk] Home UI built and wired. Save the scene.");
    }

    /// <summary>
    /// The flat's painted backdrop, the art's "ArtBackground" image on the
    /// canvas (placed with the art, never created here), which Home tints
    /// toward deep night after a late shift (HomeUIController.SetLateness;
    /// night shifts); null when the scene has none.
    /// </summary>
    private static Image Backdrop(Transform canvas)
    {
        Transform art = canvas.Find("ArtBackground");
        return art != null ? art.GetComponent<Image>() : null;
    }

    /// <summary>The bills panel's size (reference px): the title, the evening's lines (the break-in, the fixed costs, the pet's needs, the bills' total), the five bills' rows and Pay.</summary>
    private static readonly Vector2 ExpensesPanelSize = new Vector2(1000f, 900f);

    /// <summary>The pet's corner's size (reference px): the pet on the left, its needs, reaction and the toys on the right, Pet and Continue at the foot.</summary>
    private static readonly Vector2 PetPanelSize = new Vector2(1180f, 800f);

    /// <summary>The House panel's size (reference px): five category columns of cards, four rows deep, beside the detail card.</summary>
    private static readonly Vector2 HousePanelSize = new Vector2(1840f, 920f);

    /// <summary>The sleep prompt's size (reference px).</summary>
    private static readonly Vector2 SleepPanelSize = new Vector2(800f, 360f);

    /// <summary>Every panel's offset (reference px): lowered so its top clears the HUD's readouts.</summary>
    private static readonly Vector2 PanelOffset = new Vector2(0f, -30f);

    /// <summary>The inset of every panel's content from its edges (reference px).</summary>
    private const float Inset = 36f;

    /// <summary>A panel's heading: its box's height and size range.</summary>
    private const float HeadingHeight = 56f;

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
        KitScreens.Label(title, kit, kit.inkOnLight, KitText.PanelHeading);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        KitScreens.Across(body.rectTransform, Inset, Inset, 92f, 290f);
        KitScreens.Body(body, kit, kit.inkOnLight, KitText.Body);
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
        KitScreens.Label(title, kit, kit.inkOnLight, KitText.PanelHeading);
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
        KitScreens.Body(body, kit, kit.inkOnLight, KitText.Body);
        KitScreens.Across(reaction.rectTransform, right, Inset, 286f, 76f);
        KitScreens.Body(reaction, kit, kit.inkOnLight, KitText.Body);
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
        KitScreens.Label(title, kit, kit.inkOnLight, KitText.PanelHeading);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        KitScreens.Place(body.rectTransform, Inset + 440f, 34f, new Vector2(HousePanelSize.x - 2f * Inset - 440f - DetailCardWidth - 24f, 40f));
        KitScreens.Body(body, kit, kit.inkOnLight, KitText.Body, TextAlignmentOptions.MidlineLeft);
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
        KitScreens.Body(detail, kit, kit.inkOnLight, KitText.Body);
        KitScreens.PlaceBottomLeft((RectTransform)buy.transform, 28f, 28f + PlateHeight + 14f, new Vector2(DetailCardWidth - 56f, PlateHeight));
        KitScreens.Plate(buy, kit, "plate_ox");
        KitScreens.PlaceBottomLeft((RectTransform)next.transform, 28f, 28f, new Vector2(DetailCardWidth - 56f, PlateHeight));
        KitScreens.Plate(next, kit, "plate_slate");

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

    /// <summary>The machine's size (reference px, as seen) and its centre's offset from the screen's: the cabinet from the crown's arch to its foot.</summary>
    private static readonly Vector2 MachineSize = new Vector2(640f, 960f), MachineOffset = new Vector2(0f, 4f);

    /// <summary>The veil over the flat behind the machine (the kit's ink, translucent).</summary>
    private static readonly Color SlotVeil = new Color(0.118f, 0.078f, 0.11f, 0.47f);

    /// <summary>The reels' left edges inside the machine, their top, and a reel's size (two symbol pitches tall: the payline's symbol whole, its neighbours cut by the window).</summary>
    private static readonly float[] ReelLefts = { 62f, 240f, 418f };
    private const float ReelTop = 236f;
    private static readonly Vector2 ReelSize = new Vector2(160f, 244f);

    /// <summary>A reel symbol's size, and its win glow's (reference px, as seen).</summary>
    private static readonly Vector2 SymbolSize = new Vector2(100f, 100f), GlowSize = new Vector2(170f, 170f);

    /// <summary>The marquee's bulbs: the ring round its plate (left, right, top, bottom centres), the bulbs along the top and bottom, the bulbs between the corners on each side, and a bulb's size.</summary>
    private const float BulbLeft = 44f, BulbRight = 596f, BulbTop = 40f, BulbBottom = 186f, BulbSize = 20f;
    private const int BulbsAcross = 18, BulbsBetweenDown = 3;

    /// <summary>The lever's box inside the machine (the ball up top to the ball pulled down), the hub's pivot in it, the rod's and the ball's size.</summary>
    private static readonly Vector2 LeverAt = new Vector2(598f, 70f), LeverBox = new Vector2(140f, 560f), LeverPivot = new Vector2(70f, 280f);
    private static readonly Vector2 ArmSize = new Vector2(28f, 230f), BallSize = new Vector2(74f, 74f);

    /// <summary>
    /// Re-applies the Night Slots machine on every build (Saleh 2026-10-07:
    /// "make it a real 80s/90s slot machine"; mockup in the run's SL folder):
    /// the panel is the whole screen under an ink veil; in its middle the
    /// machine, every piece the kit's (slot_*): the oxblood cabinet under the
    /// arched crown, the lit marquee with the live title ringed by bulbs, the
    /// three reels (each a masked strip of five symbol cells and its win glow,
    /// the cylinder's shade over it) in the dark well, the payline and its
    /// arrows, the glass and the brass bezel; the LCD strip holding the line;
    /// the deck with the coin slot, its price plate, the credits readout, the
    /// red SPIN plate and its SPACE keycap; the lower panel's grilles, chute
    /// and payout tray (the coins fall between its inside and its lip); and
    /// on the right side the lever: the hub, the rod pivoting there, the
    /// ball, the chain and padlock (hidden), and its grip. Continue sits on
    /// the bone plate at the screen's foot right. The older panel's reels,
    /// lever and dome are removed. Returns the machine's view, wired.
    /// </summary>
    private static SlotMachineView LayOutSlot(Transform panel, UiKitSO kit, Button next)
    {
        var panelRect = (RectTransform)panel;
        Undo.RecordObject(panelRect, "Lay out the slots");
        Stretch(panelRect, Vector2.zero, Vector2.one);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        KitScreens.Remove(panel, UiKitSO.FaceName);
        foreach (string old in new[] { "Reel1", "Reel2", "Reel3", "KitLever" })
            KitScreens.Remove(panel, old);
        Image veil = panel.GetComponent<Image>();
        Undo.RecordObject(veil, "Lay out the slots");
        veil.sprite = null;
        veil.color = SlotVeil;
        veil.raycastTarget = true;

        RectTransform machine = FindOrCreateArea(panel, "SlotMachine");
        Undo.RecordObject(machine, "Lay out the slots");
        machine.anchorMin = machine.anchorMax = machine.pivot = new Vector2(0.5f, 0.5f);
        machine.anchoredPosition = MachineOffset;
        machine.sizeDelta = MachineSize;
        machine.SetAsFirstSibling();

        Piece(machine, "Cabinet", kit, "slot_cabinet", 0f, 180f, new Vector2(640f, 780f));
        Piece(machine, "Crown", kit, "slot_crown", 0f, 0f, new Vector2(640f, 200f));
        Image marquee = Piece(machine, "Marquee", kit, "slot_marquee", 64f, 60f, new Vector2(512f, 106f));
        TMP_Text title = ReparentedText(panel, Host(marquee), "TitleText");
        Fill(title, 12f, 4f);
        title.text = "Night Slots";
        SceneUiKit.SkinText(title, kit, KitText.Marquee, 106f, kit.signalRed);
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;

        RectTransform bulbRing = FindOrCreateArea(machine, "Bulbs");
        KitScreens.Place(bulbRing, 0f, 0f, new Vector2(640f, 200f));
        var bulbs = new System.Collections.Generic.List<Image>();
        foreach (Vector2 at in BulbRing())
            bulbs.Add(Piece(bulbRing, "Bulb" + bulbs.Count.ToString("00"), kit, "slot_bulb_off", at.x - BulbSize / 2f, at.y - BulbSize / 2f, new Vector2(BulbSize, BulbSize)));

        Piece(machine, "ReelBed", kit, "slot_reelbed", ReelLefts[0], ReelTop, new Vector2(ReelLefts[2] + ReelSize.x - ReelLefts[0], ReelSize.y));
        var strips = new RectTransform[SlotReels.Count];
        var glows = new RectTransform[SlotReels.Count];
        var cells = new RectTransform[SlotReels.Count * SlotMachineView.CellsPerReel];
        var cellFaces = new Image[cells.Length];
        for (int r = 0; r < SlotReels.Count; r++)
        {
            Transform reel = Host(Piece(machine, "Reel" + (r + 1), kit, "slot_reel", ReelLefts[r], ReelTop, ReelSize));
            RectTransform strip = FindOrCreateArea(reel, "Strip");
            Undo.RecordObject(strip, "Lay out the slots");
            Stretch(strip, Vector2.zero, Vector2.one);
            strip.offsetMin = strip.offsetMax = Vector2.zero;
            strip.SetSiblingIndex(1);
            if (strip.GetComponent<RectMask2D>() == null)
                Undo.AddComponent<RectMask2D>(strip.gameObject);
            strips[r] = strip;
            glows[r] = (RectTransform)Host(Centred(strip, "Glow", kit, "slot_glow", GlowSize));
            glows[r].gameObject.SetActive(false);
            for (int k = 0; k < SlotMachineView.CellsPerReel; k++)
            {
                Image face = Centred(strip, "Cell" + k, kit, "slot_sym_default", SymbolSize);
                cells[r * SlotMachineView.CellsPerReel + k] = (RectTransform)Host(face);
                cellFaces[r * SlotMachineView.CellsPerReel + k] = face;
            }
            Piece(reel, "Shade", kit, "slot_reel_shade", 0f, 0f, ReelSize);
            reel.Find("Shade").SetAsLastSibling();
        }
        Piece(machine, "Payline", kit, "slot_payline", 52f, ReelTop + ReelSize.y / 2f - 3f, new Vector2(536f, 6f));
        Piece(machine, "Glass", kit, "slot_glass", ReelLefts[0], ReelTop, new Vector2(ReelLefts[2] + ReelSize.x - ReelLefts[0], ReelSize.y));
        Piece(machine, "Window", kit, "slot_window", 40f, 214f, new Vector2(560f, 288f));
        Piece(machine, "ArrowLeft", kit, "slot_payline_arrow", 42f, ReelTop + ReelSize.y / 2f - 13f, new Vector2(20f, 26f));
        Mirror(Host(Piece(machine, "ArrowRight", kit, "slot_payline_arrow", 578f, ReelTop + ReelSize.y / 2f - 13f, new Vector2(20f, 26f))));

        Image lcd = Piece(machine, "Lcd", kit, "lcd_glass", 52f, 520f, new Vector2(536f, 74f));
        TMP_Text line = ReparentedText(panel, Host(lcd), "BodyText");
        Fill(line, 16f, 6f);
        SceneUiKit.SkinText(line, kit, KitText.Readout, 40f, kit.phosphorInk);
        line.textWrappingMode = TextWrappingModes.Normal;
        line.alignment = TextAlignmentOptions.Center;
        line.raycastTarget = false;

        Piece(machine, "Deck", kit, "slot_deck", 20f, 612f, new Vector2(600f, 150f));
        Piece(machine, "CoinSlot", kit, "slot_coinslot", 56f, 626f, new Vector2(68f, 92f));
        Image price = Piece(machine, "PricePlate", kit, "slot_priceplate", 22f, 724f, new Vector2(156f, 34f));
        TMP_Text priceText = FindOrCreateText(Host(price), "PriceText", "10 cr", 20, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
        Fill(priceText, 8f, 2f);
        SceneUiKit.SkinText(priceText, kit, KitText.Pill, 34f, kit.inkOnLight);
        priceText.alignment = TextAlignmentOptions.Center;
        priceText.raycastTarget = false;
        TMP_Text credits = Find<TMP_Text>(machine, "Credits/CreditsText")
                           ?? FindOrCreateText(machine, "CreditsText", "0 cr", 26, TextAlignmentOptions.Right, Vector2.zero, Vector2.one);
        KitScreens.Readout(machine, "Credits", kit, credits, 166f, 650f, new Vector2(196f, 56f));
        Button spin = ReparentedButton(panel, machine, "SpinButton", "Spin");
        KitScreens.Place((RectTransform)spin.transform, 392f, 636f, new Vector2(200f, 64f));
        KitScreens.Plate(spin, kit, "plate_red");
        Image key = Piece(machine, "SpinKey", kit, "keycap_bone_rest", 452f, 712f, new Vector2(80f, 30f));
        TMP_Text keyText = FindOrCreateText(Host(key), "KeyText", "Space", 14, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
        Fill(keyText, 4f, 0f);
        keyText.text = "Space";
        SceneUiKit.SkinText(keyText, kit, KitText.Keycap, 30f, kit.inkOnLight);
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.raycastTarget = false;

        Piece(machine, "Lower", kit, "slot_lower", 40f, 782f, new Vector2(560f, 158f));
        Piece(machine, "GrilleLeft", kit, "slot_grille", 66f, 804f, new Vector2(96f, 114f));
        Piece(machine, "GrilleRight", kit, "slot_grille", 478f, 804f, new Vector2(96f, 114f));
        Piece(machine, "Chute", kit, "slot_chute", 278f, 796f, new Vector2(84f, 26f));
        Piece(machine, "TrayBack", kit, "slot_tray_back", 186f, 832f, new Vector2(268f, 66f));
        RectTransform coins = FindOrCreateArea(machine, "Coins");
        KitScreens.Place(coins, 186f, 806f, new Vector2(268f, 92f));
        var coin = (RectTransform)Host(Centred(coins, "CoinTemplate", kit, "slot_coin", new Vector2(30f, 30f)));
        coin.gameObject.SetActive(false);
        Piece(machine, "TrayLip", kit, "slot_tray_lip", 176f, 870f, new Vector2(288f, 54f));

        RectTransform lever = FindOrCreateArea(machine, "Lever");
        KitScreens.Place(lever, LeverAt.x, LeverAt.y, LeverBox);
        lever.SetAsLastSibling();
        Piece(lever, "Hub", kit, "slot_lever_hub", LeverPivot.x - 36f, LeverPivot.y - 54f, new Vector2(72f, 108f));
        var arm = (RectTransform)Host(Piece(lever, "Arm", kit, "slot_lever_arm", 0f, 0f, ArmSize));
        arm.pivot = new Vector2(0.5f, 0f);
        arm.anchoredPosition = new Vector2(LeverPivot.x, -LeverPivot.y);
        var ball = (RectTransform)Host(Piece(lever, "Ball", kit, "slot_lever_ball", 0f, 0f, BallSize));
        ball.pivot = new Vector2(0.5f, 0.5f);
        ball.anchoredPosition = new Vector2(LeverPivot.x, -LeverPivot.y + ArmSize.y);
        RectTransform locked = FindOrCreateArea(lever, "Lock");
        KitScreens.Place(locked, 0f, 0f, LeverBox);
        Piece(locked, "Chain", kit, "slot_chain", LeverPivot.x - 84f, LeverPivot.y - 114f, new Vector2(128f, 82f));
        Piece(locked, "Padlock", kit, "slot_padlock", LeverPivot.x - 46f, LeverPivot.y - 58f, new Vector2(44f, 54f));
        locked.gameObject.SetActive(false);
        Image grip = FindOrCreateImage(lever, "Grip", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        KitScreens.Place(grip.rectTransform, 0f, 0f, new Vector2(LeverBox.x, LeverPivot.y + 60f));
        Undo.RecordObject(grip, "Lay out the slots");
        grip.sprite = null;
        grip.color = Color.clear;
        grip.raycastTarget = true;
        grip.transform.SetAsLastSibling();
        SlotLeverHandle handle = grip.GetComponent<SlotLeverHandle>();
        if (handle == null)
            handle = Undo.AddComponent<SlotLeverHandle>(grip.gameObject);

        KitScreens.PlaceBottomRight((RectTransform)next.transform, 64f, 44f, new Vector2(300f, PlateHeight));
        KitScreens.Plate(next, kit, "plate_bone");

        SlotMachineView view = panel.GetComponent<SlotMachineView>();
        if (view == null)
            view = Undo.AddComponent<SlotMachineView>(panel.gameObject);
        var so = new SerializedObject(view);
        so.FindProperty("kit").objectReferenceValue = kit;
        so.FindProperty("marqueeFace").objectReferenceValue = marquee;
        so.FindProperty("titleText").objectReferenceValue = title;
        SerializedArrays.Set(so, "bulbs", bulbs.ToArray());
        SerializedArrays.Set(so, "reels", strips);
        SerializedArrays.Set(so, "cells", cells);
        SerializedArrays.Set(so, "cellFaces", cellFaces);
        SerializedArrays.Set(so, "glows", glows);
        so.FindProperty("lcdText").objectReferenceValue = line;
        so.FindProperty("creditsText").objectReferenceValue = credits;
        so.FindProperty("priceFace").objectReferenceValue = price;
        so.FindProperty("priceText").objectReferenceValue = priceText;
        so.FindProperty("spinButton").objectReferenceValue = spin;
        so.FindProperty("keyFace").objectReferenceValue = key;
        so.FindProperty("lever").objectReferenceValue = lever;
        so.FindProperty("leverArm").objectReferenceValue = arm;
        so.FindProperty("leverBall").objectReferenceValue = ball;
        so.FindProperty("leverHandle").objectReferenceValue = handle;
        so.FindProperty("leverLock").objectReferenceValue = locked.gameObject;
        so.FindProperty("coinsRoot").objectReferenceValue = coins;
        so.FindProperty("coinTemplate").objectReferenceValue = coin;
        so.ApplyModifiedProperties();
        return view;
    }

    /// <summary>The marquee bulbs' centres (machine px), in chase order: along the top, down the right, back along the bottom, up the left.</summary>
    private static System.Collections.Generic.IEnumerable<Vector2> BulbRing()
    {
        for (int k = 0; k < BulbsAcross; k++)
            yield return new Vector2(Mathf.Lerp(BulbLeft, BulbRight, k / (BulbsAcross - 1f)), BulbTop);
        for (int k = 1; k <= BulbsBetweenDown; k++)
            yield return new Vector2(BulbRight, Mathf.Lerp(BulbTop, BulbBottom, k / (BulbsBetweenDown + 1f)));
        for (int k = 0; k < BulbsAcross; k++)
            yield return new Vector2(Mathf.Lerp(BulbRight, BulbLeft, k / (BulbsAcross - 1f)), BulbBottom);
        for (int k = 1; k <= BulbsBetweenDown; k++)
            yield return new Vector2(BulbLeft, Mathf.Lerp(BulbBottom, BulbTop, k / (BulbsBetweenDown + 1f)));
    }

    /// <summary>A kit piece <paramref name="sprite"/> as a part called <paramref name="name"/> under <paramref name="parent"/> (found or created), its visible face on the rect at <paramref name="left"/>, <paramref name="top"/>, <paramref name="size"/> (stretched as drawn, never kept to its aspect); no raycasts. Returns the face.</summary>
    private static Image Piece(Transform parent, string name, UiKitSO kit, string sprite, float left, float top, Vector2 size)
    {
        Image host = FindOrCreateImage(parent, name, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        KitScreens.Place(host.rectTransform, left, top, size);
        Undo.RecordObject(host, "Lay out the slots");
        host.raycastTarget = false;
        host.preserveAspect = false;
        return SceneUiKit.Skin(host, kit, sprite, kit.overlayScale);
    }

    /// <summary>A kit piece centred in <paramref name="parent"/> (a reel's cell or glow, the coin): the view moves it from the centre.</summary>
    private static Image Centred(Transform parent, string name, UiKitSO kit, string sprite, Vector2 size)
    {
        Image face = Piece(parent, name, kit, sprite, 0f, 0f, size);
        var rt = (RectTransform)Host(face);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        return face;
    }

    /// <summary>The part a kit face belongs to (Skin draws the face as the part's first child).</summary>
    private static Transform Host(Image face) => face != null ? face.transform.parent : null;

    /// <summary>Mirrors a placed part left to right about its own middle (the right payline arrow).</summary>
    private static void Mirror(Transform part)
    {
        var rt = (RectTransform)part;
        Undo.RecordObject(rt, "Lay out the slots");
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition += new Vector2(rt.sizeDelta.x / 2f, -rt.sizeDelta.y / 2f);
        rt.localScale = new Vector3(-1f, 1f, 1f);
    }

    /// <summary>Stretches <paramref name="text"/> over its parent, <paramref name="x"/> in from the sides and <paramref name="y"/> from the top and foot.</summary>
    private static void Fill(TMP_Text text, float x, float y)
    {
        RectTransform rt = text.rectTransform;
        Undo.RecordObject(rt, "Lay out the slots");
        Stretch(rt, Vector2.zero, Vector2.one);
        rt.offsetMin = new Vector2(x, y);
        rt.offsetMax = new Vector2(-x, -y);
    }

    /// <summary>The component <typeparamref name="T"/> on the child at <paramref name="path"/>, or null.</summary>
    private static T Find<T>(Transform parent, string path) where T : Component
    {
        Transform child = parent.Find(path);
        return child != null ? child.GetComponent<T>() : null;
    }

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
        KitScreens.Label(title, kit, kit.inkOnDark, KitText.PanelHeading);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        KitScreens.Across(body.rectTransform, left, Inset, 96f, 140f);
        KitScreens.Body(body, kit, kit.inkOnDark, KitText.Body);
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
