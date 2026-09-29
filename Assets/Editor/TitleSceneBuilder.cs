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
/// panel (EndingSO.picture). The world panel (2026-09-29) is the run's last
/// day: the neutral "world you made" ending's title, its text and the world
/// summary in a scroll, the END OF DEMO card set apart under it, and New Run.
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

        // --- The world panel (2026-09-29): the run's last day, the neutral "world you made" ending ---
        Transform world = FindOrCreatePanel(uiRoot, "WorldPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1280f, 960f), withBackground: true, bgColor: new Color(0.08f, 0.09f, 0.14f, 0.97f));
        TMP_Text worldTitleText = FindOrCreateText(world, "WorldTitleText", "The World You Made", 40,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.91f), new Vector2(0.95f, 0.98f));
        ScrollRect worldScroll = BuildWorldScroll(world, out TMP_Text worldSummaryText);
        TMP_Text worldCardText = FindOrCreateText(world, "WorldCardText", "END OF DEMO", 26,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.23f));
        Button worldNewRunButton = FindOrCreateButton(world, "NewRunButton", "New Run",
            new Vector2(0.35f, 0.03f), new Vector2(0.65f, 0.11f));

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
        soUi.FindProperty("worldPanel").objectReferenceValue = world.gameObject;
        soUi.FindProperty("worldTitleText").objectReferenceValue = worldTitleText;
        soUi.FindProperty("worldSummaryText").objectReferenceValue = worldSummaryText;
        soUi.FindProperty("worldCardText").objectReferenceValue = worldCardText;
        soUi.FindProperty("worldScroll").objectReferenceValue = worldScroll;
        soUi.FindProperty("worldNewRunButton").objectReferenceValue = worldNewRunButton;
        soUi.FindProperty("clerkPapers").objectReferenceValue = papers.gameObject;
        soUi.FindProperty("clerkContractText").objectReferenceValue = contractText;
        soUi.FindProperty("clerkAccountText").objectReferenceValue = accountText;
        soUi.ApplyModifiedProperties();

        BuildArtSlots(titleUI, ending, continueButton, newRunButton, endingNewRunButton, worldNewRunButton);

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
    /// The world panel's scroll (create-only, like every piece here): a dark
    /// area between the title and the card, its masked viewport, the summary
    /// text growing downwards from the top (a ContentSizeFitter) as the scroll's
    /// content, and a vertical scrollbar on the right; the wheel and a drag
    /// scroll it too.
    /// </summary>
    private static ScrollRect BuildWorldScroll(Transform world, out TMP_Text text)
    {
        Transform area = FindOrCreatePanel(world, "WorldScroll", new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.9f),
            Vector2.zero, Vector2.zero, withBackground: true, bgColor: new Color(0.05f, 0.06f, 0.09f, 1f));
        Transform viewport = FindOrCreatePanel(area, "Viewport", Vector2.zero, Vector2.one,
            new Vector2(-12f, 0f), new Vector2(-24f, 0f), withBackground: false);
        Ensure<RectMask2D>(viewport.gameObject);

        text = FindOrCreateText(viewport, "WorldSummaryText", "...", 22,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(1f, 1f));
        text.textWrappingMode = TextWrappingModes.Normal;
        text.margin = new Vector4(20f, 16f, 20f, 16f);
        var content = (RectTransform)text.transform;
        content.pivot = new Vector2(0.5f, 1f);
        ContentSizeFitter fitter = Ensure<ContentSizeFitter>(text.gameObject);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Transform bar = FindOrCreatePanel(area, "Scrollbar", new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(-12f, 0f), new Vector2(24f, 0f), withBackground: true, bgColor: new Color(0.14f, 0.15f, 0.2f, 1f));
        Transform handle = FindOrCreatePanel(bar, "Handle", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, withBackground: true, bgColor: new Color(0.62f, 0.65f, 0.72f, 1f));
        Scrollbar scrollbar = Ensure<Scrollbar>(bar.gameObject);
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = (RectTransform)handle;
        scrollbar.targetGraphic = handle.GetComponent<Image>();

        ScrollRect scroll = Ensure<ScrollRect>(area.gameObject);
        scroll.viewport = (RectTransform)viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        return scroll;
    }

    /// <summary>The object's <typeparamref name="T"/>, added (with undo) when it has none.</summary>
    private static T Ensure<T>(GameObject go) where T : Component
    {
        T existing = go.GetComponent<T>();
        return existing != null ? existing : Undo.AddComponent<T>(go);
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
