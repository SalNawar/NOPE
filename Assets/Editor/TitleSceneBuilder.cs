using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// One-click builder for the Title scene added in Alpha Phase 5: a title
/// panel (Continue/New Run) and an ending panel (display name + body for the
/// reached EndingSO, New Run). Like HomeSceneBuilder, this creates the
/// Canvas, EventSystem, TitleSceneController and TitleUIController objects
/// from scratch if they don't already exist. Safe to re-run: finds and skips
/// pieces that already exist (by name).
/// </summary>
public static class TitleSceneBuilder
{
    /// <summary>Builds and wires the Title UI in the open scene.</summary>
    [MenuItem("Tools/TimeDesk/Build Title UI (Panels + Wiring)")]
    public static void Build()
    {
        // --- Canvas + EventSystem ---
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasGo.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
        }

        Transform root = canvas.transform;

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
        }

        // --- TitleSceneController (logic) + TitleUIController (UI) ---
        TitleSceneController titleController = Object.FindFirstObjectByType<TitleSceneController>();

        if (titleController == null)
        {
            var go = new GameObject("TitleSceneController");
            titleController = go.AddComponent<TitleSceneController>();
            Undo.RegisterCreatedObjectUndo(go, "Create TitleSceneController");
        }

        TitleUIController titleUI = Object.FindFirstObjectByType<TitleUIController>();

        if (titleUI == null)
        {
            var go = new GameObject("TitleUI", typeof(RectTransform));
            go.transform.SetParent(root, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            titleUI = go.AddComponent<TitleUIController>();
            Undo.RegisterCreatedObjectUndo(go, "Create TitleUI");
        }

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
        soUi.ApplyModifiedProperties();

        // --- Wire TitleSceneController ---
        var soController = new SerializedObject(titleController);
        SerializedProperty titleUiProp = soController.FindProperty("titleUI");

        if (titleUiProp != null)
        {
            titleUiProp.objectReferenceValue = titleUI;
            soController.ApplyModifiedProperties();
        }

        // Panels start hidden (TitleUIController.Awake also enforces this).
        title.gameObject.SetActive(false);
        ending.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(titleUI.gameObject.scene);
        Debug.Log("[TimeDesk] Title UI built and wired. Save the scene.");
    }

    /// <summary>Finds a child panel by name or creates it with the given anchors.</summary>
    private static Transform FindOrCreatePanel(
        Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 size, bool withBackground, Color bgColor = default)
    {
        Transform existing = parent.Find(name);

        if (existing != null)
            return existing;

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        if (withBackground)
        {
            Image img = go.AddComponent<Image>();
            img.color = bgColor == default ? new Color(0f, 0f, 0f, 0.85f) : bgColor;
        }

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go.transform;
    }

    /// <summary>Finds a child TMP text by name or creates one with relative anchors.</summary>
    private static TMP_Text FindOrCreateText(
        Transform parent, string name, string content, int fontSize,
        TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax)
    {
        Transform existing = parent.Find(name);

        if (existing != null)
            return existing.GetComponent<TMP_Text>();

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return text;
    }

    /// <summary>Finds a child button by name or creates one (Image + Button + TMP label).</summary>
    private static Button FindOrCreateButton(
        Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        Transform existing = parent.Find(name);

        if (existing != null)
            return existing.GetComponent<Button>();

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);

        var labelRt = (RectTransform)labelGo.transform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        var labelText = labelGo.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 26;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.black;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return btn;
    }
}
