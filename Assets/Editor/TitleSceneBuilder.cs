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
/// from scratch if they don't already exist. Safe to re-run: finds and skips
/// pieces that already exist (by name), through the shared SceneUiKit. The
/// art slots (redesign phase 27): the three buttons take the text-free Title
/// face with their labels on when it exists (else their hand-wired sprites,
/// labels off), and the ending's picture shows full screen behind the ending
/// panel (EndingSO.picture). The world panel (the endings spec E0) lists the
/// world's outcomes over the END OF DEMO card and New Run; the ending panel's
/// world button opens it after a failure ("The world you leave behind").
/// The title panel is the title block (Saleh 2026-09-30), laid out on every
/// build from the knobs in <see cref="TitleBlock"/>: the printed name, a gold
/// rule and the menu on a navy plate over the painting's open floor.
/// </summary>
public static class TitleSceneBuilder
{
    /// <summary>Builds and wires the Title UI in the open scene.</summary>
    [MenuItem("Tools/TimeDesk/Build Title UI (Panels + Wiring)")]
    public static void Build()
    {
        // --- Canvas + EventSystem ---
        Canvas canvas = SceneUiKit.EnsureCanvasAndEventSystem();
        Transform root = canvas.transform;

        // --- TitleSceneController (logic) + TitleUIController (UI) ---
        TitleSceneController titleController = SceneUiKit.FindOrCreateObject<TitleSceneController>("TitleSceneController");
        TitleUIController titleUI = SceneUiKit.FindOrCreateStretched<TitleUIController>(root, "TitleUI");

        Transform uiRoot = titleUI.transform;

        // --- Title panel: the title block (the game's name over the menu; laid out on every build by LayOutTitleBlock) ---
        Transform title = FindOrCreatePanel(uiRoot, "TitlePanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(640f, 420f), withBackground: true, bgColor: TitleBlock.PlateColour);

        TMP_Text titleText = FindOrCreateText(title, "TitleText", "Time Sorter", 44,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.7f), new Vector2(0.95f, 0.95f));
        Button continueButton = FindOrCreateButton(title, "ContinueButton", "Continue",
            new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.55f));
        Button newRunButton = FindOrCreateButton(title, "NewRunButton", "New Run",
            new Vector2(0.2f, 0.18f), new Vector2(0.8f, 0.33f));
        LayOutTitleBlock(title, titleText, continueButton, newRunButton);

        // --- Ending panel ---
        Transform ending = FindOrCreatePanel(uiRoot, "EndingPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(760f, 520f), withBackground: true, bgColor: new Color(0.14f, 0.08f, 0.09f, 0.97f));

        TMP_Text endingTitleText = FindOrCreateText(ending, "EndingTitleText", "The End", 40,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.97f));
        TMP_Text endingBodyText = FindOrCreateText(ending, "EndingBodyText", "...", 24,
            TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.3f), new Vector2(0.94f, 0.8f));
        Button endingNewRunButton = FindOrCreateButton(ending, "NewRunButton", "New Run",
            new Vector2(0.3f, 0.06f), new Vector2(0.7f, 0.2f));
        Button endingWorldButton = FindOrCreateButton(ending, "WorldButton", "The world you leave behind",
            new Vector2(0.2f, 0.215f), new Vector2(0.8f, 0.3f));

        // --- The world panel (the endings spec E0): the world's outcomes, the END OF DEMO card, New Run ---
        Transform world = FindOrCreatePanel(uiRoot, "WorldPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(900f, 760f), withBackground: true, bgColor: new Color(0.08f, 0.09f, 0.14f, 0.97f));
        TMP_Text worldTitleText = FindOrCreateText(world, "WorldTitleText", "The World You Made", 40,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f));
        TMP_Text worldOutcomesText = FindOrCreateText(world, "WorldOutcomesText", "...", 28,
            TextAlignmentOptions.Top, new Vector2(0.08f, 0.33f), new Vector2(0.92f, 0.84f));
        TMP_Text worldCardText = FindOrCreateText(world, "WorldCardText", "END OF DEMO", 28,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.17f), new Vector2(0.95f, 0.31f));
        Button worldNewRunButton = FindOrCreateButton(world, "NewRunButton", "New Run",
            new Vector2(0.3f, 0.04f), new Vector2(0.7f, 0.14f));
        worldOutcomesText.textWrappingMode = TextWrappingModes.Normal;
        MatchFace(worldNewRunButton, endingNewRunButton);
        LayOutEndingButtons(endingBodyText, endingWorldButton, endingNewRunButton);

        // --- The Debt Relief ending's papers (redesign phase 13): the clerk's Labour Contract left of the panel, the account right ---
        Transform papers = FindOrCreatePanel(ending, "ClerkPapers", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1880f, 520f), withBackground: false);
        Transform contractCard = FindOrCreatePanel(papers, "ContractCard", new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(270f, 0f), new Vector2(540f, 0f), withBackground: true, bgColor: new Color(0.1f, 0.1f, 0.12f, 0.97f));
        TMP_Text contractText = FindOrCreateText(contractCard, "ContractText", "...", 18,
            TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f));
        Transform accountCard = FindOrCreatePanel(papers, "AccountCard", new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(-270f, 0f), new Vector2(540f, 0f), withBackground: true, bgColor: new Color(0.1f, 0.1f, 0.12f, 0.97f));
        TMP_Text accountText = FindOrCreateText(accountCard, "AccountText", "...", 16,
            TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f));
        contractText.textWrappingMode = TextWrappingModes.Normal;
        accountText.textWrappingMode = TextWrappingModes.Normal;

        // --- Wire TitleUIController ---
        var soUi = new SerializedObject(titleUI);

        soUi.FindProperty("titlePanel").objectReferenceValue = title.gameObject;
        soUi.FindProperty("titleText").objectReferenceValue = titleText;
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
        soUi.ApplyModifiedProperties();

        BuildArtSlots(titleUI, ending, continueButton, newRunButton, endingNewRunButton, endingWorldButton, worldNewRunButton);

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
        papers.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(titleUI.gameObject.scene);
        Debug.Log("[TimeDesk] Title UI built and wired. Save the scene.");
    }

    /// <summary>
    /// The title block's layout knobs (Saleh 2026-09-30: the name large and
    /// legible, the menu grouped under it, clear of the desk's props), in
    /// units of the 1920×1080 canvas. The block stands over the title
    /// painting's open floor, between the window's potted plant (its right
    /// edge near x 282) and the desk's front corner (near x 790).
    /// </summary>
    private static class TitleBlock
    {
        /// <summary>The block's left edge and vertical centre, from the screen's left middle.</summary>
        public static readonly Vector2 Position = new Vector2(290f, -20f);

        /// <summary>The block's width; its height follows what it shows (Continue only with a save).</summary>
        public const float Width = 480f;

        /// <summary>Space inside the plate: left, right, top, bottom.</summary>
        public const int PadSides = 40, PadTop = 40, PadBottom = 48;

        /// <summary>Space between the name, the rule and each button.</summary>
        public const float Spacing = 22f;

        /// <summary>The plate: the Title's own panel navy, nearly opaque (it hides the painting's highlights under the name; measured on screen, linear colour space).</summary>
        public static readonly Color PlateColour = new Color(0.08f, 0.09f, 0.14f, 0.94f);

        /// <summary>The name's box: two lines ("TIME" over "SORTER") at the largest size that fits.</summary>
        public static readonly Vector2 NameBox = new Vector2(400f, 232f);

        /// <summary>The name's size range (auto-sized to its box).</summary>
        public const float NameMaxSize = 108f, NameMinSize = 64f;

        /// <summary>The name's ink: the buttons' cream face colour.</summary>
        public static readonly Color NameColour = new Color(0.957f, 0.929f, 0.859f, 1f);

        /// <summary>The rule under the name, the logo's gold line.</summary>
        public static readonly Vector2 RuleSize = new Vector2(360f, 4f);

        /// <summary>The rule's colour.</summary>
        public static readonly Color RuleColour = new Color(0.8f, 0.6f, 0.31f, 1f);

        /// <summary>Each menu button (the faces are 400×100 pictures: kept at their 4:1 shape).</summary>
        public static readonly Vector2 ButtonSize = new Vector2(360f, 90f);
    }

    /// <summary>
    /// Lays the title panel out as the title block, re-applied on every build
    /// (the one part of the Title that is not create-only): the plate at the
    /// left of the screen, stacking its children top down (a
    /// VerticalLayoutGroup, the height fitted, so a hidden Continue closes up)
    /// in the order the name, the gold rule, Continue, New Run; the printed
    /// name on (TitleUIController prints UiText's "title.name") and the
    /// interim logo picture off (its dark ink was drawn for a light ground).
    /// Each child's own place is set too, so the scene reads right in the
    /// editor before the layout runs.
    /// </summary>
    private static void LayOutTitleBlock(Transform block, TMP_Text name, Button continueButton, Button newRunButton)
    {
        var plate = (RectTransform)block;
        Undo.RecordObject(plate, "Lay out the title block");
        plate.anchorMin = plate.anchorMax = new Vector2(0f, 0.5f);
        plate.pivot = new Vector2(0f, 0.5f);
        plate.anchoredPosition = TitleBlock.Position;
        Image plateImage = block.GetComponent<Image>();
        if (plateImage != null)
        {
            Undo.RecordObject(plateImage, "Lay out the title block");
            plateImage.color = TitleBlock.PlateColour;
            EditorUtility.SetDirty(plateImage);
        }

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

        Undo.RecordObject(name, "Lay out the title block");
        name.enabled = true;
        name.text = UiText.Get(TitleUIController.NameKey);
        name.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        name.color = TitleBlock.NameColour;
        name.alignment = TextAlignmentOptions.Center;
        name.textWrappingMode = TextWrappingModes.Normal;
        name.enableAutoSizing = true;
        name.fontSizeMin = TitleBlock.NameMinSize;
        name.fontSizeMax = TitleBlock.NameMaxSize;
        name.lineSpacing = -8f;
        name.characterSpacing = 2f;
        name.raycastTarget = false;
        EditorUtility.SetDirty(name);

        Image rule = FindOrCreateImage(block, "TitleRule", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        Undo.RecordObject(rule, "Lay out the title block");
        rule.preserveAspect = false;
        rule.color = TitleBlock.RuleColour;
        EditorUtility.SetDirty(rule);

        Transform logo = block.Find("ArtLogo");
        if (logo != null && logo.gameObject.activeSelf)
        {
            Undo.RecordObject(logo.gameObject, "Lay out the title block");
            logo.gameObject.SetActive(false);
        }

        var stack = new (RectTransform rect, Vector2 size)[]
        {
            (name.rectTransform, TitleBlock.NameBox),
            ((RectTransform)rule.transform, TitleBlock.RuleSize),
            ((RectTransform)continueButton.transform, TitleBlock.ButtonSize),
            ((RectTransform)newRunButton.transform, TitleBlock.ButtonSize)
        };
        float y = TitleBlock.PadTop;
        for (int i = 0; i < stack.Length; i++)
        {
            RectTransform rt = stack[i].rect;
            Undo.RecordObject(rt, "Lay out the title block");
            rt.SetSiblingIndex(i);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = stack[i].size;
            rt.anchoredPosition = new Vector2(0f, -y);
            y += stack[i].size.y + TitleBlock.Spacing;
        }
        plate.sizeDelta = new Vector2(TitleBlock.Width, y - TitleBlock.Spacing + TitleBlock.PadBottom);
    }

    /// <summary>
    /// The ending panel's two buttons as one column, re-applied on every
    /// build: New Run at the panel's foot, as wide as its label needs and at
    /// its face picture's 4:1 shape, and the world button ("The world you
    /// leave behind") the same size just above it, on the buttons' cream face
    /// colour with the face's teal ink, so the pair reads as one menu; the
    /// body text ends above them.
    /// </summary>
    private static void LayOutEndingButtons(TMP_Text body, Button world, Button newRun)
    {
        var panel = (RectTransform)newRun.transform.parent;
        var newRunRect = (RectTransform)newRun.transform;
        float height = EndingButtonWidth * panel.sizeDelta.x / 4f / panel.sizeDelta.y;
        Undo.RecordObject(newRunRect, "Lay out the ending buttons");
        newRunRect.anchorMin = new Vector2(0.5f - EndingButtonWidth / 2f, EndingButtonGap + 0.02f);
        newRunRect.anchorMax = new Vector2(0.5f + EndingButtonWidth / 2f, newRunRect.anchorMin.y + height);
        newRunRect.offsetMin = newRunRect.offsetMax = Vector2.zero;
        var worldRect = (RectTransform)world.transform;
        Undo.RecordObject(worldRect, "Lay out the ending buttons");
        worldRect.anchorMin = new Vector2(newRunRect.anchorMin.x, newRunRect.anchorMax.y + EndingButtonGap);
        worldRect.anchorMax = new Vector2(newRunRect.anchorMax.x, worldRect.anchorMin.y + height);
        worldRect.offsetMin = worldRect.offsetMax = Vector2.zero;

        Undo.RecordObject(body.rectTransform, "Lay out the ending buttons");
        body.rectTransform.anchorMin = new Vector2(body.rectTransform.anchorMin.x, worldRect.anchorMax.y + EndingButtonGap);

        Image face = world.GetComponent<Image>();
        Undo.RecordObject(face, "Lay out the ending buttons");
        face.color = TitleBlock.NameColour;
        EditorUtility.SetDirty(face);
        Transform label = world.transform.Find("Label");
        TMP_Text text = label != null ? label.GetComponent<TMP_Text>() : null;
        if (text != null)
        {
            Undo.RecordObject(text, "Lay out the ending buttons");
            text.color = EndingButtonInk;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = 22f;
            text.fontSizeMax = 26f;
            text.margin = new Vector4(12f, 0f, 12f, 0f);
            EditorUtility.SetDirty(text);
        }
    }

    /// <summary>The ending panel's buttons' width, in panel widths (the world button's label at 24 px or more).</summary>
    private const float EndingButtonWidth = 0.5f;

    /// <summary>The gap between the ending panel's buttons, and above them, in panel heights.</summary>
    private const float EndingButtonGap = 0.03f;

    /// <summary>The ending panel's world button's ink: the Title button faces' printed teal.</summary>
    private static readonly Color EndingButtonInk = new Color(0.18f, 0.29f, 0.33f, 1f);

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
    /// Gives <paramref name="button"/> <paramref name="model"/>'s hand-wired
    /// face (its sprite, colour, transition and hover sprites, and its label
    /// off when the face prints it), re-applied on every build, so the world
    /// page's New Run looks like the ending panel's. Nothing when the model
    /// has no sprite.
    /// </summary>
    private static void MatchFace(Button button, Button model)
    {
        Image image = button.GetComponent<Image>(), face = model.GetComponent<Image>();
        if (image == null || face == null || face.sprite == null)
            return;
        Undo.RecordObject(image, "Match the New Run face");
        Undo.RecordObject(button, "Match the New Run face");
        image.sprite = face.sprite;
        image.type = face.type;
        image.color = face.color;
        button.transition = model.transition;
        button.spriteState = model.spriteState;
        button.colors = model.colors;
        Transform label = button.transform.Find("Label"), modelLabel = model.transform.Find("Label");
        TMP_Text text = label != null ? label.GetComponent<TMP_Text>() : null, modelText = modelLabel != null ? modelLabel.GetComponent<TMP_Text>() : null;
        if (text != null && modelText != null)
        {
            Undo.RecordObject(text, "Match the New Run face");
            text.enabled = modelText.enabled;
        }
        EditorUtility.SetDirty(image);
        EditorUtility.SetDirty(button);
    }

    /// <summary>
    /// The Title's art slots: each button's text-free face (ArtSlots.TitleButton,
    /// its hover face swapped in, its label turned on) and the ending's
    /// picture, a full-screen image just under the ending panel, hidden until
    /// an ending with a picture shows (TitleUIController.endingPicture).
    /// </summary>
    private static void BuildArtSlots(TitleUIController titleUI, Transform ending, params Button[] buttons)
    {
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;
            Transform label = button.transform.Find("Label");
            EnsureArtSlot(button.GetComponent<Image>(), ArtSlots.TitleButton, ArtSlots.TitleButtonHover, true, false,
                          label != null ? label.GetComponent<TMP_Text>() : null);
        }

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
