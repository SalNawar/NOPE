using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static SceneUiKit;

/// <summary>
/// One-click builder for the Title scene added in Alpha Phase 5: a title
/// panel (Continue/New Run) and an ending panel (display name + body for the
/// reached EndingSO, New Run). Like HomeSceneBuilder, this creates the
/// Canvas, EventSystem, TitleSceneController and TitleUIController objects
/// from scratch if they don't already exist; the rest is found by name or
/// created, through the shared SceneUiKit. Every panel is drawn in the cel UI
/// kit (docs/UI_KIT.md, sheet 04; KitScreens), laid out and skinned again on
/// every build: the title block (the kit's logo over Continue on the oxblood
/// plate and New Run on the slate one, on the navy panel over the painting's
/// open floor, from the knobs in <see cref="TitleBlock"/>); the ending panel
/// (New Run, and "The world you leave behind" after a failure) with the
/// ending's picture full screen behind it (EndingSO.picture) and, for the
/// Debt Relief ending, the clerk's papers as printed cards; the world panel
/// (the endings spec E0: the world's outcomes over the END OF DEMO card and
/// New Run); and the adoption panel (the Home pet spec PS1, shown after New
/// Run): the dog's and the cat's choice cards, a name field, the refusal's
/// line, Back and Adopt.
/// </summary>
public static class TitleSceneBuilder
{
    /// <summary>Builds and wires the Title UI in the open scene.</summary>
    [MenuItem("Tools/TimeDesk/Build Title UI (Panels + Wiring)")]
    public static void Build()
    {
        UiKitSO kit = UiKitAssets.Ensure();
        if (kit == null)
            return;

        // --- Canvas + EventSystem ---
        Canvas canvas = SceneUiKit.EnsureCanvasAndEventSystem();
        Transform root = canvas.transform;

        // --- TitleSceneController (logic) + TitleUIController (UI) ---
        TitleSceneController titleController = SceneUiKit.FindOrCreateObject<TitleSceneController>("TitleSceneController");
        TitleUIController titleUI = SceneUiKit.FindOrCreateStretched<TitleUIController>(root, "TitleUI");

        Transform uiRoot = titleUI.transform;

        // --- Title panel: the title block (the logo over the menu; laid out on every build by LayOutTitleBlock) ---
        Transform title = FindOrCreatePanel(uiRoot, "TitlePanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(640f, 420f), withBackground: true);
        Button continueButton = FindOrCreateButton(title, "ContinueButton", "Continue",
            new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.55f));
        Button newRunButton = FindOrCreateButton(title, "NewRunButton", "New Run",
            new Vector2(0.2f, 0.18f), new Vector2(0.8f, 0.33f));
        LayOutTitleBlock(title, kit, continueButton, newRunButton);

        // --- Ending panel ---
        Transform ending = FindOrCreatePanel(uiRoot, "EndingPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, EndingPanelSize, withBackground: true);
        TMP_Text endingTitleText = FindOrCreateText(ending, "EndingTitleText", "The End", 40,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.97f));
        TMP_Text endingBodyText = FindOrCreateText(ending, "EndingBodyText", "...", 24,
            TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.3f), new Vector2(0.94f, 0.8f));
        Button endingNewRunButton = FindOrCreateButton(ending, "NewRunButton", "New Run",
            new Vector2(0.3f, 0.06f), new Vector2(0.7f, 0.2f));
        Button endingWorldButton = FindOrCreateButton(ending, "WorldButton", "The world you leave behind",
            new Vector2(0.2f, 0.215f), new Vector2(0.8f, 0.3f));
        LayOutEnding(ending, kit, endingTitleText, endingBodyText, endingWorldButton, endingNewRunButton);

        // --- The world panel (the endings spec E0): the world's outcomes, the END OF DEMO card, New Run ---
        Transform world = FindOrCreatePanel(uiRoot, "WorldPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, WorldPageSize, withBackground: true);
        TMP_Text worldTitleText = FindOrCreateText(world, "WorldTitleText", "The World You Made", 40,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f));
        TMP_Text worldOutcomesText = FindOrCreateText(world, "WorldOutcomesText", "...", 28,
            TextAlignmentOptions.Top, new Vector2(0.08f, 0.33f), new Vector2(0.92f, 0.84f));
        TMP_Text worldCardText = FindOrCreateText(world, "WorldCardText", "END OF DEMO", 28,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.17f), new Vector2(0.95f, 0.31f));
        Button worldNewRunButton = FindOrCreateButton(world, "NewRunButton", "New Run",
            new Vector2(0.3f, 0.04f), new Vector2(0.7f, 0.14f));
        LayOutWorldPage(world, kit, worldTitleText, worldOutcomesText, worldCardText, worldNewRunButton);

        // --- The adoption panel (the Home pet spec PS1): a dog or a cat, and its name ---
        Transform adopt = FindOrCreatePanel(uiRoot, "AdoptPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, AdoptPanelSize, withBackground: true);
        TMP_Text adoptTitle = FindOrCreateText(adopt, "TitleText", "Adopt a companion", 40,
            TextAlignmentOptions.Left, new Vector2(0.05f, 0.87f), new Vector2(0.95f, 0.97f));
        TMP_Text adoptBody = FindOrCreateText(adopt, "BodyText", "...", 24,
            TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.73f), new Vector2(0.94f, 0.86f));
        Button dog = FindOrCreateButton(adopt, "DogButton", "Dog", new Vector2(0.52f, 0.6f), new Vector2(0.72f, 0.69f));
        Button cat = FindOrCreateButton(adopt, "CatButton", "Cat", new Vector2(0.74f, 0.6f), new Vector2(0.94f, 0.69f));
        TMP_InputField nameInput = FindOrCreateInputField(adopt, "NameInput", 28, new Vector2(0.52f, 0.46f), new Vector2(0.94f, 0.55f));
        TMP_Text problem = FindOrCreateText(adopt, "ProblemText", "", 22,
            TextAlignmentOptions.TopLeft, new Vector2(0.52f, 0.35f), new Vector2(0.94f, 0.45f));
        Button adoptGo = FindOrCreateButton(adopt, "AdoptButton", "Adopt and start", new Vector2(0.52f, 0.2f), new Vector2(0.94f, 0.31f));
        Button adoptBack = FindOrCreateButton(adopt, "BackButton", "Back", new Vector2(0.05f, 0.04f), new Vector2(0.27f, 0.12f));
        LayOutAdopt(adopt, kit, adoptTitle, adoptBody, dog, cat, nameInput, problem, adoptGo, adoptBack);

        // --- The Debt Relief ending's papers (redesign phase 13): the clerk's Labour Contract left of the panel, the account right ---
        Transform papers = FindOrCreatePanel(ending, "ClerkPapers", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1880f, 520f), withBackground: false);
        Transform contractCard = FindOrCreatePanel(papers, "ContractCard", new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(270f, 0f), new Vector2(540f, 0f), withBackground: true);
        TMP_Text contractText = FindOrCreateText(contractCard, "ContractText", "...", 18,
            TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f));
        Transform accountCard = FindOrCreatePanel(papers, "AccountCard", new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(-270f, 0f), new Vector2(540f, 0f), withBackground: true);
        TMP_Text accountText = FindOrCreateText(accountCard, "AccountText", "...", 16,
            TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f));
        LayOutPapers(kit, contractCard, contractText, accountCard, accountText);

        // --- Wire TitleUIController ---
        var soUi = new SerializedObject(titleUI);

        soUi.FindProperty("kit").objectReferenceValue = kit;
        soUi.FindProperty("titlePanel").objectReferenceValue = title.gameObject;
        soUi.FindProperty("continueButton").objectReferenceValue = continueButton;
        soUi.FindProperty("newRunButton").objectReferenceValue = newRunButton;

        soUi.FindProperty("endingPanel").objectReferenceValue = ending.gameObject;
        soUi.FindProperty("endingTitleText").objectReferenceValue = endingTitleText;
        soUi.FindProperty("endingBodyText").objectReferenceValue = endingBodyText;
        soUi.FindProperty("endingNewRunButton").objectReferenceValue = endingNewRunButton;
        soUi.FindProperty("endingWorldButton").objectReferenceValue = endingWorldButton;
        soUi.FindProperty("worldPanel").objectReferenceValue = world.gameObject;
        soUi.FindProperty("worldTitleText").objectReferenceValue = worldTitleText;
        soUi.FindProperty("worldOutcomesText").objectReferenceValue = worldOutcomesText;
        soUi.FindProperty("worldCardText").objectReferenceValue = worldCardText;
        soUi.FindProperty("worldNewRunButton").objectReferenceValue = worldNewRunButton;
        soUi.FindProperty("clerkPapers").objectReferenceValue = papers.gameObject;
        soUi.FindProperty("clerkContractText").objectReferenceValue = contractText;
        soUi.FindProperty("clerkAccountText").objectReferenceValue = accountText;
        soUi.FindProperty("adoptPanel").objectReferenceValue = adopt.gameObject;
        soUi.FindProperty("adoptTitleText").objectReferenceValue = adoptTitle;
        soUi.FindProperty("adoptBodyText").objectReferenceValue = adoptBody;
        soUi.FindProperty("adoptDogButton").objectReferenceValue = dog;
        soUi.FindProperty("adoptCatButton").objectReferenceValue = cat;
        soUi.FindProperty("adoptNameInput").objectReferenceValue = nameInput;
        soUi.FindProperty("adoptProblemText").objectReferenceValue = problem;
        soUi.FindProperty("adoptButton").objectReferenceValue = adoptGo;
        soUi.FindProperty("adoptBackButton").objectReferenceValue = adoptBack;
        soUi.ApplyModifiedProperties();

        BuildEndingPicture(titleUI, ending);

        // --- Wire TitleSceneController ---
        var soController = new SerializedObject(titleController);
        SerializedProperty titleUiProp = soController.FindProperty("titleUI");

        if (titleUiProp != null)
        {
            titleUiProp.objectReferenceValue = titleUI;
            soController.ApplyModifiedProperties();
        }

        // Panels start hidden (TitleUIController.Awake also enforces this; ShowClerkPapers shows the papers).
        title.gameObject.SetActive(false);
        ending.gameObject.SetActive(false);
        world.gameObject.SetActive(false);
        adopt.gameObject.SetActive(false);
        papers.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(titleUI.gameObject.scene);
        Debug.Log("[TimeDesk] Title UI built and wired. Save the scene.");
    }

    /// <summary>
    /// The title block's layout knobs (Saleh 2026-09-30: the name large and
    /// legible, the menu grouped under it, clear of the desk's props; the UI
    /// kit 2026-10-07: the kit's logo, plates and navy panel), in units of the
    /// 1920×1080 canvas. The block stands over the title painting's open
    /// floor, between the window's potted plant (its right edge near x 282)
    /// and the desk's front corner (near x 790).
    /// </summary>
    private static class TitleBlock
    {
        /// <summary>The block's left edge and vertical centre, from the screen's left middle.</summary>
        public static readonly Vector2 Position = new Vector2(290f, -20f);

        /// <summary>The block's width; its height follows what it shows (Continue only with a save).</summary>
        public const float Width = 480f;

        /// <summary>Space inside the panel: left, right, top, bottom.</summary>
        public const int PadSides = 40, PadTop = 30, PadBottom = 44;

        /// <summary>Space between the logo and each plate.</summary>
        public const float Spacing = 22f;

        /// <summary>The logo (the kit's logo_time_sorter, its words baked in: TIME SORTER over タイムソーター), as seen.</summary>
        public static readonly Vector2 LogoSize = new Vector2(420f, 230f);

        /// <summary>Each plate (Continue on oxblood, New Run on slate), as seen.</summary>
        public static readonly Vector2 ButtonSize = new Vector2(360f, 76f);
    }

    /// <summary>
    /// Lays the title panel out as the title block, re-applied on every build:
    /// the kit's navy panel at the left of the screen, stacking its children
    /// top down (a VerticalLayoutGroup, the height fitted, so a hidden
    /// Continue closes up) in the order the logo, Continue, New Run. The
    /// printed name, its gold rule and the interim logo picture that came
    /// before the kit are removed.
    /// </summary>
    private static void LayOutTitleBlock(Transform block, UiKitSO kit, Button continueButton, Button newRunButton)
    {
        var plate = (RectTransform)block;
        Undo.RecordObject(plate, "Lay out the title block");
        plate.anchorMin = plate.anchorMax = new Vector2(0f, 0.5f);
        plate.pivot = new Vector2(0f, 0.5f);
        plate.anchoredPosition = TitleBlock.Position;
        KitScreens.Panel(block, kit, "panel_night");
        Transform face = block.Find(UiKitSO.FaceName);
        if (face != null)
            Ensure<LayoutElement>(face).ignoreLayout = true;
        KitScreens.Remove(block, "TitleText");
        KitScreens.Remove(block, "TitleRule");
        KitScreens.Remove(block, "ArtLogo");

        var group = Ensure<VerticalLayoutGroup>(block);
        group.padding = new RectOffset(TitleBlock.PadSides, TitleBlock.PadSides, TitleBlock.PadTop, TitleBlock.PadBottom);
        group.spacing = TitleBlock.Spacing;
        group.childAlignment = TextAnchor.UpperCenter;
        group.childControlWidth = group.childControlHeight = false;
        group.childForceExpandWidth = group.childForceExpandHeight = false;
        group.childScaleWidth = group.childScaleHeight = false;
        EditorUtility.SetDirty(group);
        var fitter = Ensure<ContentSizeFitter>(block);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        EditorUtility.SetDirty(fitter);

        Image logo = FindOrCreateImage(block, "KitLogo", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        Undo.RecordObject(logo, "Lay out the title block");
        logo.raycastTarget = false;
        Image logoFace = SceneUiKit.Skin(logo, kit, "logo_time_sorter", kit.overlayScale);
        if (logoFace != null)
            logoFace.preserveAspect = true;
        KitScreens.Plate(continueButton, kit, "plate_ox");
        KitScreens.Plate(newRunButton, kit, "plate_slate");

        var stack = new (RectTransform rect, Vector2 size)[]
        {
            (logo.rectTransform, TitleBlock.LogoSize),
            ((RectTransform)continueButton.transform, TitleBlock.ButtonSize),
            ((RectTransform)newRunButton.transform, TitleBlock.ButtonSize)
        };
        float y = TitleBlock.PadTop;
        for (int i = 0; i < stack.Length; i++)
        {
            RectTransform rt = stack[i].rect;
            Undo.RecordObject(rt, "Lay out the title block");
            rt.SetSiblingIndex(i + 1);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = stack[i].size;
            rt.anchoredPosition = new Vector2(0f, -y);
            y += stack[i].size.y + TitleBlock.Spacing;
        }
        plate.sizeDelta = new Vector2(TitleBlock.Width, y - TitleBlock.Spacing + TitleBlock.PadBottom);
    }

    /// <summary>The ending panel's size (reference px): its heading, its body and the two plates under it.</summary>
    private static readonly Vector2 EndingPanelSize = new Vector2(820f, 600f);

    /// <summary>The ending panel's and the world page's plates (reference px, as seen).</summary>
    private static readonly Vector2 EndingPlateSize = new Vector2(440f, 70f);

    /// <summary>The inset of every panel's content from its edges (reference px).</summary>
    private const float Inset = 36f;

    /// <summary>A panel's heading: its box's height and size range.</summary>
    private const float HeadingHeight = 60f, HeadingMax = 44f, HeadingMin = 26f;

    /// <summary>
    /// The ending panel, re-applied on every build (sheet 04's endings): the
    /// kit's navy panel, its heading and body in bone, and at its foot New Run
    /// on the oxblood plate with "The world you leave behind" on the slate one
    /// just above it, the same size.
    /// </summary>
    private static void LayOutEnding(Transform panel, UiKitSO kit, TMP_Text title, TMP_Text body, Button world, Button newRun)
    {
        KitScreens.Size(panel, EndingPanelSize, Vector2.zero);
        KitScreens.Panel(panel, kit, "panel_night");
        KitScreens.Across(title.rectTransform, Inset, Inset, 28f, HeadingHeight);
        KitScreens.Label(title, kit, kit.inkOnDark, HeadingMax, HeadingMin);
        title.alignment = TextAlignmentOptions.Center;
        float plates = 30f + 2f * EndingPlateSize.y + 16f;
        KitScreens.Across(body.rectTransform, Inset + 8f, Inset + 8f, 100f, EndingPanelSize.y - 100f - plates - 20f);
        KitScreens.Body(body, kit.inkOnDark, 24f, 17f);
        float left = (EndingPanelSize.x - EndingPlateSize.x) / 2f;
        KitScreens.PlaceBottomLeft((RectTransform)newRun.transform, left, 30f, EndingPlateSize);
        KitScreens.Plate(newRun, kit, "plate_ox");
        KitScreens.PlaceBottomLeft((RectTransform)world.transform, left, 30f + EndingPlateSize.y + 16f, EndingPlateSize);
        KitScreens.Plate(world, kit, "plate_slate", 26f);
    }

    /// <summary>The world page's size on the 1920×1080 canvas (wide enough for a split answer on one line at 25 px or more).</summary>
    private static readonly Vector2 WorldPageSize = new Vector2(1160f, 800f);

    /// <summary>The END OF DEMO card's ink (the kit's brass).</summary>
    private static readonly Color CardInk = new Color(0.831f, 0.627f, 0.333f, 1f);

    /// <summary>
    /// The world page, re-applied on every build: the kit's navy panel, the
    /// heading in bone, the world's outcomes across 90% of its width in bone
    /// (shrinking from 28 to 20 rather than spilling onto the END OF DEMO
    /// card when the answers are long, such as "The Cybernetic Age and The
    /// Nuclear Age, one on each bank of the river"), the card in the kit's
    /// label face in brass, and New Run on the oxblood plate at the foot.
    /// </summary>
    private static void LayOutWorldPage(Transform world, UiKitSO kit, TMP_Text title, TMP_Text outcomes, TMP_Text card, Button newRun)
    {
        KitScreens.Size(world, WorldPageSize, Vector2.zero);
        KitScreens.Panel(world, kit, "panel_night");
        KitScreens.Across(title.rectTransform, Inset, Inset, 28f, HeadingHeight);
        KitScreens.Label(title, kit, kit.inkOnDark, HeadingMax, HeadingMin);
        title.alignment = TextAlignmentOptions.Center;
        KitScreens.Across(outcomes.rectTransform, WorldPageSize.x * 0.05f, WorldPageSize.x * 0.05f, 104f, 420f);
        KitScreens.Body(outcomes, kit.inkOnDark, 28f, 20f, TextAlignmentOptions.Top);
        KitScreens.Across(card.rectTransform, Inset, Inset, 540f, 80f);
        KitScreens.Label(card, kit, CardInk, 40f, 22f);
        card.alignment = TextAlignmentOptions.Center;
        KitScreens.PlaceBottomLeft((RectTransform)newRun.transform, (WorldPageSize.x - EndingPlateSize.x) / 2f, 34f, EndingPlateSize);
        KitScreens.Plate(newRun, kit, "plate_ox");
    }

    /// <summary>The Debt Relief ending's two papers as the kit's printed cards (the card panel, the papers' text in ink).</summary>
    private static void LayOutPapers(UiKitSO kit, Transform contract, TMP_Text contractText, Transform account, TMP_Text accountText)
    {
        KitScreens.Panel(contract, kit, "panel_card");
        KitScreens.Panel(account, kit, "panel_card");
        KitScreens.Body(contractText, kit.inkOnLight, 18f, 13f);
        KitScreens.Body(accountText, kit.inkOnLight, 16f, 12f);
    }

    /// <summary>The adoption panel's size (reference px).</summary>
    private static readonly Vector2 AdoptPanelSize = new Vector2(1180f, 620f);

    /// <summary>A choice card (reference px, as seen), the tile on it and the gap between the two cards.</summary>
    private static readonly Vector2 ChoiceCardSize = new Vector2(220f, 260f);

    /// <summary>The pet's tile on its choice card (reference px, as seen).</summary>
    private const float ChoiceTileSize = 128f;

    /// <summary>
    /// The adoption panel, re-applied on every build (sheet 04's ADOPT A
    /// COMPANION): the kit's bone panel; its heading and line; the dog's and
    /// the cat's choice cards at the left (the kit's card with the kind's tile
    /// over its name; under the pointer its hover face; the chosen card takes
    /// no clicks and shows the selected face, framed in oxblood); the name
    /// field (the kit's field: its focus face while typing; TitleUIController
    /// shows the error face on a refusal) with the refusal's line under it in
    /// signal red; Back on the slate plate and Adopt on the oxblood one at the
    /// foot right. The drawn preview pet that came before the kit's cards is
    /// removed.
    /// </summary>
    private static void LayOutAdopt(Transform panel, UiKitSO kit, TMP_Text title, TMP_Text body, Button dog, Button cat, TMP_InputField name,
                                    TMP_Text problem, Button go, Button back)
    {
        KitScreens.Size(panel, AdoptPanelSize, Vector2.zero);
        KitScreens.Panel(panel, kit, "panel_bone");
        KitScreens.Remove(panel, "Preview");
        KitScreens.Across(title.rectTransform, Inset, Inset, 26f, HeadingHeight);
        KitScreens.Label(title, kit, kit.inkOnLight, HeadingMax, HeadingMin);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        KitScreens.Across(body.rectTransform, Inset, Inset, 92f, 70f);
        KitScreens.Body(body, kit.inkOnLight, 22f, 17f);

        ChoiceCard(dog, kit, "tile_dog_rest", Inset, 186f);
        ChoiceCard(cat, kit, "tile_cat_rest", Inset + ChoiceCardSize.x + 24f, 186f);

        float fieldLeft = Inset + 2f * ChoiceCardSize.x + 24f + 56f;
        float fieldWidth = AdoptPanelSize.x - fieldLeft - Inset;
        KitScreens.Place((RectTransform)name.transform, fieldLeft, 206f, new Vector2(fieldWidth, 64f));
        Image field = name.GetComponent<Image>();
        Image fieldFace = SceneUiKit.Skin(field, kit, "field_rest", kit.overlayScale);
        Undo.RecordObject(name, "Lay out the adoption");
        if (fieldFace != null)
            name.targetGraphic = fieldFace;
        name.transition = Selectable.Transition.SpriteSwap;
        name.spriteState = new SpriteState { selectedSprite = kit.Get("field_focus") };
        foreach (TMP_Text t in name.GetComponentsInChildren<TMP_Text>(true))
        {
            Undo.RecordObject(t, "Lay out the adoption");
            t.color = t.name == "Placeholder" ? PlaceholderInk : kit.inkOnLight;
            EditorUtility.SetDirty(t);
        }
        EditorUtility.SetDirty(name);

        KitScreens.Place(problem.rectTransform, fieldLeft + 4f, 282f, new Vector2(fieldWidth - 8f, 60f));
        KitScreens.Body(problem, kit.signalRed, 20f, 16f);
        problem.fontStyle = FontStyles.Bold;

        KitScreens.PlaceBottomRight((RectTransform)go.transform, Inset, 32f, new Vector2(380f, 72f));
        KitScreens.Plate(go, kit, "plate_ox");
        KitScreens.PlaceBottomRight((RectTransform)back.transform, Inset + 380f + 22f, 32f, new Vector2(230f, 72f));
        KitScreens.Plate(back, kit, "plate_slate");
    }

    /// <summary>The name field's placeholder ink (muted, on the kit's field).</summary>
    private static readonly Color PlaceholderInk = new Color(0.45f, 0.4f, 0.37f, 1f);

    /// <summary>A pet's choice card at <paramref name="left"/>, <paramref name="top"/>: the kit's choicecard (rest and hover swapped; its selected face shown while it is the chosen card, which takes no clicks), the kind's tile and its name in the kit's label face under it.</summary>
    private static void ChoiceCard(Button card, UiKitSO kit, string tile, float left, float top)
    {
        KitScreens.Place((RectTransform)card.transform, left, top, ChoiceCardSize);
        KitScreens.Plate(card, kit, "choicecard", 36f);
        Undo.RecordObject(card, "Lay out the adoption");
        SpriteState states = card.spriteState;
        states.pressedSprite = kit.Get("choicecard_hover");
        states.disabledSprite = kit.Get("choicecard_selected");
        card.spriteState = states;
        card.transition = Selectable.Transition.SpriteSwap;
        ColorBlock colours = card.colors;
        colours.disabledColor = Color.white;
        card.colors = colours;
        EditorUtility.SetDirty(card);

        KitScreens.Picture(card.transform, "Tile", kit, tile, (ChoiceCardSize.x - ChoiceTileSize) / 2f, 26f, new Vector2(ChoiceTileSize, ChoiceTileSize));
        Transform label = card.transform.Find("Label");
        if (label != null)
        {
            KitScreens.Across((RectTransform)label, 12f, 12f, 26f + ChoiceTileSize + 16f, 60f);
            label.SetAsLastSibling();
        }
    }

    /// <summary>The <typeparamref name="T"/> on <paramref name="host"/>, added (with undo) when it has none.</summary>
    private static T Ensure<T>(Transform host) where T : Component
    {
        T component = host.GetComponent<T>();
        if (component == null)
            component = Undo.AddComponent<T>(host.gameObject);
        else
            Undo.RecordObject(component, "Lay out the title block");
        return component;
    }

    /// <summary>
    /// The ending's picture: a full-screen image just under the ending panel,
    /// hidden until an ending with a picture shows
    /// (TitleUIController.endingPicture).
    /// </summary>
    private static void BuildEndingPicture(TitleUIController titleUI, Transform ending)
    {
        Image picture = FindOrCreateImage(ending.parent, "EndingPicture", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        picture.preserveAspect = false;
        if (picture.transform.GetSiblingIndex() > ending.GetSiblingIndex())
            picture.transform.SetSiblingIndex(ending.GetSiblingIndex());
        picture.gameObject.SetActive(false);

        var so = new SerializedObject(titleUI);
        so.FindProperty("endingPicture").objectReferenceValue = picture;
        so.ApplyModifiedProperties();
    }
}
