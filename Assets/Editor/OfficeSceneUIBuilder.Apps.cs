using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's desktop apps (redesign phase 25; the PC spec's ML1,
/// AC1, NT1, SG1, §2.12-2.15): the Mail window (the inbox and the memo form)
/// with its feed on the desktop canvas, the Citizen Account window (the
/// clerk's Record Extract and Statement), the Notes window (days, clippings,
/// the typed notes), and the Settings window in sections (Language, Motion,
/// Desktop, Keyboard) with the shortcut card. The memo, the account's extract
/// and its statement are forms (phase 5: Form_Memo, Form_RecordExtract and
/// Form_Statement on FormViews, OfficeSceneUIBuilder.PcForms), diegetic and
/// never themed. The three app windows are rebuilt fresh on each run
/// (like Records); Settings keeps its objects. Part of
/// <see cref="OfficeSceneUIBuilder"/>; the desktop shell builds each app and
/// registers it in DesktopApps (phase 17: BuildDesktopShell), before the
/// window manager (which wires every window's chrome).
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The paper tone behind Mail's memo page.</summary>
    private static readonly Color FormPaper = new Color(0.965f, 0.95f, 0.9f, 1f);

    /// <summary>The Mail memo's page kind (TC-950).</summary>
    private const string MemoFormPath = "Assets/Data/Forms/Form_Memo.asset";

    /// <summary>The Citizen Account's page kinds: the Record Extract (TC-901) and the Statement (TC-960).</summary>
    private const string RecordExtractFormPath = "Assets/Data/Forms/Form_RecordExtract.asset", StatementFormPath = "Assets/Data/Forms/Form_Statement.asset";

    /// <summary>The gap between the Citizen Account's two pages (desktop units).</summary>
    private const float AccountPageGap = 16f;

    /// <summary>A list row's height (the inbox, the day list).</summary>
    private const float AppRowHeight = 52f;

    /// <summary>Wires a single persistent call with a string argument on a UnityEvent (as WirePersistentVoid, String mode).</summary>
    private static void WirePersistentString(Object host, string eventProp, Object target, string method, string argument)
    {
        var so = new SerializedObject(host);
        SerializedProperty calls = ClearPersistentCalls(so, eventProp);
        if (calls == null)
            return;

        calls.InsertArrayElementAtIndex(0);
        SerializedProperty call = calls.GetArrayElementAtIndex(0);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = target.GetType().AssemblyQualifiedName;
        call.FindPropertyRelative("m_MethodName").stringValue = method;
        call.FindPropertyRelative("m_Mode").enumValueIndex = 5; // String
        call.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue = argument;
        call.FindPropertyRelative("m_Arguments.m_ObjectArgumentAssemblyTypeName").stringValue = typeof(Object).AssemblyQualifiedName;
        call.FindPropertyRelative("m_CallState").enumValueIndex = 2; // RuntimeOnly
        so.ApplyModifiedProperties();
    }

    // -----------------------------
    // Mail (ML1, §2.12)
    // -----------------------------

    /// <summary>The Mail window: INBOX (a scrolling list of message rows) on the left; on the right the message's link over the memo, a Form_Memo page (TC-950) on a FormView in a scroll; the directive memo's link opens <paramref name="investigation"/> on its Rules tab. Rebuilt fresh.</summary>
    private static DesktopWindow BuildMailWindow(Transform windowLayer, DesktopConfigSO config, MailFeed feed, DesktopApps apps, BrowserWindow browser,
                                                 InvestigationApp investigation, out TMP_Text title)
    {
        DestroyChildIfPresent(windowLayer, "MailWindow");
        DesktopWindow chrome = BuildOSWindow(windowLayer, "MailWindow", null, null, null, config.mailWindowSize);
        Transform win = chrome.transform;
        Object.DestroyImmediate(win.Find("Body").gameObject);
        title = win.Find("Header/TitleText").GetComponent<TMP_Text>();
        title.text = UiText.Get("window.mail");

        Text(win, "InboxLabel", null, 16, TextAlignmentOptions.BottomLeft, new Vector2(0.02f, 0.885f), new Vector2(0.36f, 0.935f), Ink,
             ThemeRoleId.WindowBody, "mail.inbox", FontStyles.Bold);
        RectTransform list = BuildScrollList(win, "Inbox", new Vector2(0.02f, 0.02f), new Vector2(0.36f, 0.88f), 4f);
        Button row = BuildListRow(list, "MailRowTemplate");
        TMP_Text empty = Text(win, "EmptyText", null, 16, TextAlignmentOptions.Center, new Vector2(0.03f, 0.7f), new Vector2(0.35f, 0.8f), Ink,
                              ThemeRoleId.InputField, "mail.none", FontStyles.Italic);
        empty.raycastTarget = false;

        Button link = MakeButton(win, "LinkButton", "", new Vector2(0.52f, 0.885f), new Vector2(0.98f, 0.935f), new Color(0.15f, 0.3f, 0.5f, 1f), ThemeRoleId.SearchButton);
        FitLabel(link, 17f);
        Transform page = Panel(win, "MemoPage", new Vector2(0.38f, 0.02f), new Vector2(0.98f, 0.875f), Vector2.zero, Vector2.zero, FormPaper, ThemeRoleId.DiegeticPaper);
        TMP_Text select = Text(page, "SelectText", null, 18, TextAlignmentOptions.Center, new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.6f), Ink,
                               ThemeRoleId.DiegeticRow, "mail.select", FontStyles.Italic);
        float memoWidth = config.mailWindowSize.x * 0.6f - DocMargin - DocGap - DocScrollbar;
        ScrollRect memo = BuildFormScroll(page, "Memo", memoWidth, out FormView memoView);

        MailWindow component = win.gameObject.AddComponent<MailWindow>();
        var so = new SerializedObject(component);
        SetRef(so, "feed", feed);
        SetRef(so, "apps", apps);
        SetRef(so, "browser", browser);
        Wire(so, "investigation", investigation);
        SetRef(so, "listRoot", list);
        SetRef(so, "rowTemplate", row);
        SetRef(so, "emptyText", empty);
        Wire(so, "memoScroll", memo);
        Wire(so, "memo", memoView);
        Wire(so, "memoForm", AssetDatabase.LoadAssetAtPath<FormSpecSO>(MemoFormPath));
        SetRef(so, "selectText", select);
        SetRef(so, "linkButton", link);
        so.ApplyModifiedProperties();

        memo.gameObject.SetActive(false);
        win.gameObject.SetActive(false);
        return chrome;
    }

    // -----------------------------
    // Citizen Account (AC1, §2.13)
    // -----------------------------

    /// <summary>
    /// The Citizen Account window (phase 5): one scroll whose content stacks
    /// the Record Extract (a FormView at the PC page width, Form_RecordExtract)
    /// and the Statement (a FormView across the scroll, Form_Statement's
    /// landscape page, so its eight columns keep their type sizes), both
    /// centred. AccountWindow fills and stacks them. Rebuilt fresh.
    /// </summary>
    private static DesktopWindow BuildAccountWindow(Transform windowLayer, DesktopConfigSO config)
    {
        DestroyChildIfPresent(windowLayer, "AccountWindow");
        DesktopWindow chrome = BuildOSWindow(windowLayer, "AccountWindow", "window.account", null, null, config.accountWindowSize);
        Transform win = chrome.transform;
        Object.DestroyImmediate(win.Find("Body").gameObject);

        ScrollRect scroll = BuildScrollArea(win, "Sheet", out RectTransform viewport);
        PlaceRect(scroll.transform, Vector2.zero, Vector2.one, new Vector2(DocMargin, DocMargin), new Vector2(-DocMargin, -(config.titleBarHeight + DocGap)));
        var content = (RectTransform)Panel(viewport, "Content", new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero, null);
        content.pivot = new Vector2(0.5f, 1f);
        scroll.content = content;
        float statementWidth = config.accountWindowSize.x - 2f * DocMargin - DocGap - DocScrollbar;
        FormView extract = BuildFormView(content, "Extract", PcPageWidth);
        FormView statement = BuildFormView(content, "Statement", statementWidth);

        AccountWindow component = win.gameObject.AddComponent<AccountWindow>();
        var so = new SerializedObject(component);
        Wire(so, "scroll", scroll);
        Wire(so, "extract", extract);
        Wire(so, "extractForm", AssetDatabase.LoadAssetAtPath<FormSpecSO>(RecordExtractFormPath));
        Wire(so, "statement", statement);
        Wire(so, "statementForm", AssetDatabase.LoadAssetAtPath<FormSpecSO>(StatementFormPath));
        so.FindProperty("gap").floatValue = AccountPageGap;
        so.ApplyModifiedProperties();

        win.gameObject.SetActive(false);
        return chrome;
    }

    // -----------------------------
    // Notes (NT1, §2.14)
    // -----------------------------

    /// <summary>
    /// The Notes window: DAYS (a scrolling list) on the left; on the right the
    /// page's heading, CLIPPINGS with Paste clipping and the cards' list (the
    /// empty page's hint over it), and NOTES with its counter and the typed
    /// notes' field. Rebuilt fresh.
    /// </summary>
    private static DesktopWindow BuildNotesWindow(Transform windowLayer, DesktopConfigSO config)
    {
        DestroyChildIfPresent(windowLayer, "NotesWindow");
        DesktopWindow chrome = BuildOSWindow(windowLayer, "NotesWindow", "window.notes", null, null, config.notesWindowSize);
        Transform win = chrome.transform;
        Object.DestroyImmediate(win.Find("Body").gameObject);

        Text(win, "DaysLabel", null, 16, TextAlignmentOptions.BottomLeft, new Vector2(0.02f, 0.885f), new Vector2(0.22f, 0.935f), Ink,
             ThemeRoleId.WindowBody, "notes.days", FontStyles.Bold);
        RectTransform days = BuildScrollList(win, "Days", new Vector2(0.02f, 0.02f), new Vector2(0.22f, 0.88f), 4f);
        Button day = BuildListRow(days, "DayTemplate");
        SetLayoutHeight(day, 40f);

        TMP_Text title = Text(win, "PageTitle", "", 20, TextAlignmentOptions.BottomLeft, new Vector2(0.25f, 0.885f), new Vector2(0.97f, 0.935f), Ink,
                              ThemeRoleId.WindowBody, style: FontStyles.Bold);
        Text(win, "ClippingsLabel", null, 15, TextAlignmentOptions.BottomLeft, new Vector2(0.25f, 0.82f), new Vector2(0.6f, 0.865f), Ink,
             ThemeRoleId.WindowBody, "notes.clippings", FontStyles.Bold);
        Button paste = MakeButton(win, "PasteButton", null, new Vector2(0.68f, 0.82f), new Vector2(0.97f, 0.875f), null, ThemeRoleId.Button, "notes.paste");
        RectTransform clips = BuildScrollList(win, "Clippings", new Vector2(0.25f, 0.47f), new Vector2(0.97f, 0.81f), 4f);
        TMP_Text group = LayoutText(clips, "GroupTemplate", 15, FontStyles.Bold, ThemeRoleId.InputField);
        RectTransform card = BuildClipCard(clips, "ClipTemplate");
        TMP_Text hint = Text(win, "HintText", null, 16, TextAlignmentOptions.Center, new Vector2(0.28f, 0.55f), new Vector2(0.94f, 0.73f), Ink,
                             ThemeRoleId.InputField, "notes.hint", FontStyles.Italic);
        hint.textWrappingMode = TextWrappingModes.Normal;
        hint.raycastTarget = false;

        Text(win, "NotesLabel", null, 15, TextAlignmentOptions.BottomLeft, new Vector2(0.25f, 0.41f), new Vector2(0.6f, 0.455f), Ink,
             ThemeRoleId.WindowBody, "notes.notes", FontStyles.Bold);
        TMP_Text counter = Text(win, "CounterText", "", 14, TextAlignmentOptions.BottomRight, new Vector2(0.6f, 0.41f), new Vector2(0.97f, 0.455f), Ink,
                                ThemeRoleId.WindowBody);
        TMP_InputField field = BuildInputField(win, "NotesField", "notes.placeholder", new Vector2(0.25f, 0.03f), new Vector2(0.97f, 0.4f));
        field.textComponent.alignment = TextAlignmentOptions.TopLeft;
        field.textComponent.textWrappingMode = TextWrappingModes.Normal;
        ((TMP_Text)field.placeholder).alignment = TextAlignmentOptions.TopLeft;
        field.lineType = TMP_InputField.LineType.MultiLineNewline;
        field.characterLimit = config.notesMaxChars;

        NotesWindow component = win.gameObject.AddComponent<NotesWindow>();
        var so = new SerializedObject(component);
        SetRef(so, "config", config);
        SetRef(so, "dayListRoot", days);
        SetRef(so, "dayTemplate", day);
        SetRef(so, "pageTitle", title);
        SetRef(so, "clipRoot", clips);
        SetRef(so, "groupTemplate", group);
        SetRef(so, "clipTemplate", card);
        SetRef(so, "hintText", hint);
        SetRef(so, "pasteButton", paste);
        SetRef(so, "notesField", field);
        SetRef(so, "counterText", counter);
        so.ApplyModifiedProperties();

        group.gameObject.SetActive(false);
        card.gameObject.SetActive(false);
        win.gameObject.SetActive(false);
        return chrome;
    }

    // -----------------------------
    // Settings (SG1, §2.15)
    // -----------------------------

    /// <summary>
    /// The Settings window (piece 6 U12, piece 9 R17, redesign phase 25 SG1),
    /// 640 × 720, in titled sections: Language (Follow history / Always
    /// English), Motion (Full / Reduced), Desktop (open icons with Double
    /// click / Single click, Reset icon positions: phase 17; its icons wired
    /// by WireIconSettings), Investigation (Text size: a button per zoom
    /// level, redesign phase 20; phases 18 and 21 add their rows), Keyboard
    /// (Show shortcuts, which opens the F1 card: BuildShortcutCard), then the
    /// note. Existing objects are kept; every row's anchors are re-applied on
    /// each build.
    /// </summary>
    private static DesktopWindow BuildSettingsWindow(Transform windowLayer)
    {
        DesktopWindow chrome = BuildOSWindow(windowLayer, "SettingsWindow", "window.settings", "settings.language", null, EnsureDesktopConfig().settingsWindowSize);
        Transform win = chrome.transform;
        Heading(win.Find("Body").GetComponent<TMP_Text>(), "settings.language", new Vector2(0.05f, 0.875f), new Vector2(0.95f, 0.93f));
        Button follow = MakeButton(win, "FollowHistoryButton", null, new Vector2(0.05f, 0.795f), new Vector2(0.48f, 0.865f), null, ThemeRoleId.Button, "settings.followHistory");
        SetAnchors(follow.transform, new Vector2(0.05f, 0.795f), new Vector2(0.48f, 0.865f));
        Button english = MakeButton(win, "AlwaysEnglishButton", null, new Vector2(0.52f, 0.795f), new Vector2(0.95f, 0.865f), null, ThemeRoleId.Button, "settings.alwaysEnglish");
        SetAnchors(english.transform, new Vector2(0.52f, 0.795f), new Vector2(0.95f, 0.865f));

        TMP_Text motion = Text(win, "MotionLabel", null, 20, TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.725f), new Vector2(0.95f, 0.78f), Ink,
                               ThemeRoleId.WindowBody, "settings.motion");
        Heading(motion, "settings.motion", new Vector2(0.05f, 0.725f), new Vector2(0.95f, 0.78f));
        Button full = MakeButton(win, "FullMotionButton", null, new Vector2(0.05f, 0.645f), new Vector2(0.48f, 0.715f), null, ThemeRoleId.Button, "settings.motionFull");
        SetAnchors(full.transform, new Vector2(0.05f, 0.645f), new Vector2(0.48f, 0.715f));
        Button reduced = MakeButton(win, "ReducedMotionButton", null, new Vector2(0.52f, 0.645f), new Vector2(0.95f, 0.715f), null, ThemeRoleId.Button, "settings.motionReduced");
        SetAnchors(reduced.transform, new Vector2(0.52f, 0.645f), new Vector2(0.95f, 0.715f));

        TMP_Text desktop = Text(win, "DesktopLabel", null, 20, TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.575f), new Vector2(0.95f, 0.63f), Ink,
                                ThemeRoleId.WindowBody, "settings.desktop");
        Heading(desktop, "settings.desktop", new Vector2(0.05f, 0.575f), new Vector2(0.95f, 0.63f));
        Button iconDouble = MakeButton(win, "IconDoubleClickButton", null, new Vector2(0.05f, 0.495f), new Vector2(0.48f, 0.565f), null, ThemeRoleId.Button, "settings.iconDouble");
        SetAnchors(iconDouble.transform, new Vector2(0.05f, 0.495f), new Vector2(0.48f, 0.565f));
        Button iconSingle = MakeButton(win, "IconSingleClickButton", null, new Vector2(0.52f, 0.495f), new Vector2(0.95f, 0.565f), null, ThemeRoleId.Button, "settings.iconSingle");
        SetAnchors(iconSingle.transform, new Vector2(0.52f, 0.495f), new Vector2(0.95f, 0.565f));
        Button resetIcons = MakeButton(win, "ResetIconsButton", null, new Vector2(0.05f, 0.415f), new Vector2(0.48f, 0.485f), null, ThemeRoleId.Button, "settings.resetIcons");
        SetAnchors(resetIcons.transform, new Vector2(0.05f, 0.415f), new Vector2(0.48f, 0.485f));

        DesktopConfigSO config = EnsureDesktopConfig();
        TMP_Text investigation = Text(win, "InvestigationLabel", null, 20, TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.345f), new Vector2(0.95f, 0.4f), Ink,
                                      ThemeRoleId.WindowBody, "settings.investigation");
        Heading(investigation, "settings.investigation", new Vector2(0.05f, 0.345f), new Vector2(0.95f, 0.4f));
        var textSizes = new List<Object>();
        int levels = config.zoomLevels.Length;
        for (int i = 0; i < levels; i++)
        {
            float from = 0.05f + i * 0.9f / levels;
            var aMin = new Vector2(from + (i > 0 ? 0.01f : 0f), 0.265f);
            var aMax = new Vector2(from + 0.9f / levels - (i < levels - 1 ? 0.01f : 0f), 0.335f);
            Button size = MakeButton(win, "TextSizeButton_" + config.zoomLevels[i], null, aMin, aMax, null, ThemeRoleId.Button);
            SetAnchors(size.transform, aMin, aMax);
            TMP_Text sizeLabel = size.transform.Find("Label").GetComponent<TMP_Text>();
            sizeLabel.text = UiText.Format("settings.textSize", config.zoomLevels[i]);
            Tag(sizeLabel, ThemeRoleId.Button, ThemePart.Ink, null, FontStyles.Normal, ThemeTextKind.Button);
            textSizes.Add(size);
        }

        TMP_Text keyboard = Text(win, "KeyboardLabel", null, 20, TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.195f), new Vector2(0.95f, 0.25f), Ink,
                                 ThemeRoleId.WindowBody, "settings.keyboard");
        Heading(keyboard, "settings.keyboard", new Vector2(0.05f, 0.195f), new Vector2(0.95f, 0.25f));
        Button shortcuts = MakeButton(win, "ShowShortcutsButton", null, new Vector2(0.05f, 0.115f), new Vector2(0.48f, 0.185f), null, ThemeRoleId.Button, "settings.showShortcuts");
        SetAnchors(shortcuts.transform, new Vector2(0.05f, 0.115f), new Vector2(0.48f, 0.185f));

        TMP_Text note = Text(win, "NoteText", null, 15, TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.105f), Ink,
                             ThemeRoleId.WindowBody, "settings.note");
        SetAnchors(note.transform, new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.105f));
        note.fontSize = 15f;
        note.text = UiText.Get("settings.note");
        note.textWrappingMode = TextWrappingModes.Normal;

        // The shortcut card (F1; OfficeSceneUIBuilder.Keys), rebuilt fresh, so it keeps its place after the rebuilt windows.
        DesktopWindow card = BuildShortcutCard(windowLayer);

        SettingsWindowController controller = GetOrAdd<SettingsWindowController>(win.gameObject);
        var so = new SerializedObject(controller);
        SetRef(so, "followHistoryButton", follow);
        SetRef(so, "alwaysEnglishButton", english);
        SetRef(so, "fullMotionButton", full);
        SetRef(so, "reducedMotionButton", reduced);
        SetRef(so, "iconDoubleClickButton", iconDouble);
        SetRef(so, "iconSingleClickButton", iconSingle);
        SetRef(so, "resetIconsButton", resetIcons);
        SerializedArrays.Set(so, "textSizeButtons", textSizes);
        SetRef(so, "config", config);
        SetRef(so, "showShortcutsButton", shortcuts);
        SetRef(so, "shortcutsWindow", card);
        so.ApplyModifiedProperties();
        return chrome;
    }

    /// <summary>A section heading: bold, 20 u, its anchors re-applied (an existing text keeps its object).</summary>
    private static void Heading(TMP_Text text, string key, Vector2 aMin, Vector2 aMax)
    {
        SetAnchors(text.transform, aMin, aMax);
        text.fontSize = 20f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.BottomLeft;
        Tag(text, ThemeRoleId.WindowBody, ThemePart.Ink, key, FontStyles.Bold, ThemeTextKind.Heading);
    }

    // -----------------------------
    // Parts
    // -----------------------------

    /// <summary>
    /// A scrolling vertical list: a box (an input-field white by default, the
    /// scroll's raycast target) with a masked viewport and a content that
    /// grows with its children (a vertical layout, fitted to its preferred
    /// height). Returns the content.
    /// </summary>
    private static RectTransform BuildScrollList(Transform parent, string name, Vector2 aMin, Vector2 aMax, float spacing, Color? fill = null,
                                                 ThemeRoleId role = ThemeRoleId.InputField)
    {
        Transform box = Panel(parent, name, aMin, aMax, Vector2.zero, Vector2.zero, fill ?? Color.white, role);
        Transform viewport = Panel(box, "Viewport", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-8f, -8f), null);
        GetOrAdd<RectMask2D>(viewport.gameObject);
        var content = (RectTransform)Panel(viewport, "Content", new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero, null);
        content.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(content.gameObject);
        layout.spacing = spacing;
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fit = GetOrAdd<ContentSizeFitter>(content.gameObject);
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect scroll = GetOrAdd<ScrollRect>(box.gameObject);
        scroll.content = content;
        scroll.viewport = (RectTransform)viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        return content;
    }

    /// <summary>A list row: a button of <see cref="AppRowHeight"/> whose left-aligned label holds two lines (rich text), with a "Selected" bar at its left edge (hidden; the window shows it on the open row).</summary>
    private static Button BuildListRow(Transform list, string name)
    {
        Button row = MakeButton(list, name, "", Vector2.zero, Vector2.one, null, ThemeRoleId.Button);
        SetLayoutHeight(row, AppRowHeight);
        Transform bar = Panel(row.transform, "Selected", Vector2.zero, new Vector2(0f, 1f), new Vector2(3f, 0f), new Vector2(6f, -8f), new Color(0.15f, 0.35f, 0.85f, 1f),
                              ThemeRoleId.SelectionHighlight);
        bar.GetComponent<Image>().raycastTarget = false;
        bar.gameObject.SetActive(false);
        TMP_Text label = row.transform.Find("Label").GetComponent<TMP_Text>();
        label.fontSize = 16f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.margin = new Vector4(10f, 2f, 8f, 2f);
        label.richText = true;
        return row;
    }

    /// <summary>A clipping's card: its text (wrapping) over where it came from, and an X at the top right.</summary>
    private static RectTransform BuildClipCard(Transform list, string name)
    {
        var card = (RectTransform)Panel(list, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 0.97f, 0.8f, 1f), ThemeRoleId.StickyNote);
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(card.gameObject);
        layout.padding = new RectOffset(10, 44, 6, 6);
        layout.spacing = 2f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text text = LayoutText(card, "Text", 16, FontStyles.Normal, ThemeRoleId.StickyNote);
        text.color = Ink;
        TMP_Text label = LayoutText(card, "Label", 13, FontStyles.Italic, ThemeRoleId.StickyNote);
        label.color = Ink;

        Button remove = MakeButton(card, "RemoveButton", null, new Vector2(1f, 1f), new Vector2(1f, 1f), XpRed, ThemeRoleId.CloseButton, "notes.remove");
        var rt = (RectTransform)remove.transform;
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(30f, 30f);
        rt.anchoredPosition = new Vector2(-6f, -4f);
        GetOrAdd<LayoutElement>(remove.gameObject).ignoreLayout = true;
        return card;
    }

    /// <summary>A wrapping text laid out by its parent (its height is its text's), in the ink of <paramref name="role"/>'s builder colour.</summary>
    private static TMP_Text LayoutText(Transform parent, string name, int size, FontStyles style, ThemeRoleId role)
    {
        TMP_Text text = Text(parent, name, "", size, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Ink, role, style: style);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.richText = true;
        return text;
    }

    /// <summary>A button's label shrinks to fit from <paramref name="size"/>.</summary>
    private static void FitLabel(Button button, float size)
    {
        TMP_Text label = button.transform.Find("Label").GetComponent<TMP_Text>();
        label.enableAutoSizing = true;
        label.fontSizeMax = size;
        label.fontSizeMin = 12f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
    }
}
