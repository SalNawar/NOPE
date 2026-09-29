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

        // --- Title panel ---
        Transform title = FindOrCreatePanel(uiRoot, "TitlePanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(640f, 420f), withBackground: true, bgColor: new Color(0.08f, 0.09f, 0.14f, 0.97f));

        TMP_Text titleText = FindOrCreateText(title, "TitleText", "Time Sorter", 44,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.7f), new Vector2(0.95f, 0.95f));
        Button continueButton = FindOrCreateButton(title, "ContinueButton", "Continue",
            new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.55f));
        Button newRunButton = FindOrCreateButton(title, "NewRunButton", "New Run",
            new Vector2(0.2f, 0.18f), new Vector2(0.8f, 0.33f));

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
