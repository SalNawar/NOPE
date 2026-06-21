using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// One-click builder for the Home scene UI added in Alpha Phase 4: HUD
/// (day/money/stability), expenses + family condition panel, upgrade shop,
/// slot machine, and the sleep prompt. Unlike OfficeSceneUIBuilder, this
/// script creates the Canvas, EventSystem, HomeManager and HomeUIController
/// objects from scratch if they don't already exist (HomeScene starts as an
/// empty scene). Safe to re-run: skips/finds pieces that already exist (by name).
/// </summary>
public static class HomeSceneBuilder
{
    /// <summary>Builds and wires the Home UI in the open scene.</summary>
    [MenuItem("Tools/TimeDesk/Build Home UI (Panels + Wiring)")]
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

        // --- HomeManager (logic) + HomeUIController (UI) ---
        HomeManager homeManager = Object.FindFirstObjectByType<HomeManager>();

        if (homeManager == null)
        {
            var go = new GameObject("HomeManager");
            homeManager = go.AddComponent<HomeManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create HomeManager");
        }

        HomeUIController homeUI = Object.FindFirstObjectByType<HomeUIController>();

        if (homeUI == null)
        {
            var go = new GameObject("HomeUI", typeof(RectTransform));
            go.transform.SetParent(root, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            homeUI = go.AddComponent<HomeUIController>();
            Undo.RegisterCreatedObjectUndo(go, "Create HomeUI");
        }

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

        // --- Expenses panel ---
        Transform expenses = FindOrCreatePanel(uiRoot, "ExpensesPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(760f, 600f), withBackground: true, bgColor: new Color(0.1f, 0.12f, 0.2f, 0.97f));

        TMP_Text expensesTitle = FindOrCreateText(expenses, "TitleText", "Day 1 — Home", 34,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f));
        TMP_Text expensesBody = FindOrCreateText(expenses, "BodyText", "...", 22,
            TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.55f), new Vector2(0.94f, 0.86f));
        Transform familyRows = FindOrCreateRowsContainer(expenses, "FamilyRows",
            new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.53f));
        Button expensesContinue = FindOrCreateButton(expenses, "ContinueButton", "Continue to Shop",
            new Vector2(0.35f, 0.04f), new Vector2(0.65f, 0.13f));

        // --- Shop panel ---
        Transform shop = FindOrCreatePanel(uiRoot, "ShopPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(760f, 600f), withBackground: true, bgColor: new Color(0.12f, 0.16f, 0.1f, 0.97f));

        TMP_Text shopTitle = FindOrCreateText(shop, "TitleText", "Upgrade Shop", 34,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f));
        TMP_Text shopBody = FindOrCreateText(shop, "BodyText", "...", 22,
            TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.86f));
        Transform shopRows = FindOrCreateRowsContainer(shop, "ShopRows",
            new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.76f));
        Button shopContinue = FindOrCreateButton(shop, "ContinueButton", "Continue to Slots",
            new Vector2(0.35f, 0.04f), new Vector2(0.65f, 0.13f));

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
        soUi.FindProperty("shopRowsRoot").objectReferenceValue = shopRows;
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

        EditorSceneManager.MarkSceneDirty(homeUI.gameObject.scene);
        Debug.Log("[TimeDesk] Home UI built and wired. Save the scene.");
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

    /// <summary>
    /// Finds a child rows container by name or creates one with a
    /// VerticalLayoutGroup, ready for runtime-spawned rows
    /// (HomeUIController.CreateRow/CreateLabelRow).
    /// </summary>
    private static Transform FindOrCreateRowsContainer(
        Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        Transform existing = parent.Find(name);

        if (existing != null)
            return existing;

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

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
