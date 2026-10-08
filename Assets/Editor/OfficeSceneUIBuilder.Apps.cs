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
/// never themed. Every app window is rebuilt fresh on each run. Part of
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

    /// <summary>The Mail list's width and a message row's height (the subject over the day and sender).</summary>
    private const float MailListWidth = 360f, MailRowHeight = 72f;

    /// <summary>The Notes day list's width.</summary>
    private const float NotesDaysWidth = 220f;

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

    /// <summary>
    /// The Mail window (the PC UX redesign's list and detail): "Inbox" over a
    /// scrolling list of message rows (the subject at Body size, the day and
    /// sender under it) on the left; on the right the message's link over the
    /// memo, a Form_Memo page (TC-950) on a FormView in a scroll; the directive
    /// memo's link opens <paramref name="investigation"/> on today's rules.
    /// Rebuilt fresh.
    /// </summary>
    private static DesktopWindow BuildMailWindow(Transform windowLayer, DesktopConfigSO config, MailFeed feed, DesktopApps apps, BrowserWindow browser,
                                                 InvestigationApp investigation)
    {
        DestroyChildIfPresent(windowLayer, "MailWindow");
        DesktopWindow chrome = BuildOSWindow(windowLayer, "MailWindow", null, null, null, config.mailWindowSize);
        Transform win = chrome.transform;
        Object.DestroyImmediate(win.Find("Body").gameObject);
        win.Find("Header/TitleText").GetComponent<TMP_Text>().text = UiText.Get("window.mail");
        float top = config.titleBarHeight + PcSize.M;

        SectionHeading(win, "InboxLabel", "mail.inbox", PcSize.L, top, MailListWidth);
        RectTransform list = BuildScrollList(win, "Inbox", Vector2.zero, new Vector2(0f, 1f), 4f);
        PlaceRect(list.parent.parent, Vector2.zero, new Vector2(0f, 1f), new Vector2(PcSize.L, PcSize.L), new Vector2(PcSize.L + MailListWidth, -(top + 48f)));
        Button row = BuildListRow(list, "MailRowTemplate");
        GetOrAdd<LayoutElement>(row.gameObject).minHeight = MailRowHeight;
        TMP_Text empty = Text(list.parent.parent, "EmptyText", null, PcType.Body, TextAlignmentOptions.Top, Vector2.zero, Vector2.one, Ink,
                              ThemeRoleId.InputField, "mail.none", FontStyles.Italic);
        PlaceRect(empty.transform, Vector2.zero, Vector2.one, new Vector2(PcSize.M, 0f), new Vector2(-PcSize.M, -PcSize.L));
        empty.raycastTarget = false;

        float detail = PcSize.L + MailListWidth + PcSize.L;
        Button link = MakeButton(win, "LinkButton", "", Vector2.zero, Vector2.one, new Color(0.15f, 0.3f, 0.5f, 1f), ThemeRoleId.SearchButton);
        PlaceRect(link.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(detail, -(top + PcSize.Control)), new Vector2(-PcSize.L, -top));
        ButtonLabel(link, PcType.Body, TextAlignmentOptions.MidlineLeft, PcSize.L);
        Transform page = Panel(win, "MemoPage", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FormPaper, ThemeRoleId.DiegeticPaper);
        PlaceRect(page, Vector2.zero, Vector2.one, new Vector2(detail, PcSize.L), new Vector2(-PcSize.L, -(top + PcSize.Control + PcSize.M)));
        TMP_Text select = Text(page, "SelectText", null, PcType.Body, TextAlignmentOptions.Center, new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.6f), Ink,
                               ThemeRoleId.DiegeticRow, "mail.select", FontStyles.Italic);
        float memoWidth = config.mailWindowSize.x - detail - PcSize.L - DocMargin - DocGap - DocScrollbar;
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

    /// <summary>A window's section heading: Title size, bold, <paramref name="width"/> wide at <paramref name="x"/>, its top <paramref name="top"/> under the window's top.</summary>
    private static TMP_Text SectionHeading(Transform win, string name, string key, float x, float top, float width)
    {
        TMP_Text heading = Text(win, name, null, PcType.Title, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, Ink, ThemeRoleId.WindowBody, key,
                                FontStyles.Bold, ThemeTextKind.Heading, true);
        PlaceRect(heading.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -(top + 40f)), new Vector2(x + width, -top));
        heading.raycastTarget = false;
        return heading;
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
    /// The Notes window: "Days" (a scrolling list) on the left; on the right
    /// the page's heading, "Clippings" with Paste clipping and the cards' list
    /// (the empty page's hint over it), and "Notes" with its counter and the
    /// typed notes' field; every label at the PC's scale (the PC UX redesign
    /// §4). Rebuilt fresh.
    /// </summary>
    private static DesktopWindow BuildNotesWindow(Transform windowLayer, DesktopConfigSO config)
    {
        DestroyChildIfPresent(windowLayer, "NotesWindow");
        DesktopWindow chrome = BuildOSWindow(windowLayer, "NotesWindow", "window.notes", null, null, config.notesWindowSize);
        Transform win = chrome.transform;
        Object.DestroyImmediate(win.Find("Body").gameObject);
        float top = config.titleBarHeight + PcSize.M;
        float right = PcSize.L + NotesDaysWidth + PcSize.L;

        SectionHeading(win, "DaysLabel", "notes.days", PcSize.L, top, NotesDaysWidth);
        RectTransform days = BuildScrollList(win, "Days", Vector2.zero, new Vector2(0f, 1f), 4f);
        PlaceRect(days.parent.parent, Vector2.zero, new Vector2(0f, 1f), new Vector2(PcSize.L, PcSize.L), new Vector2(PcSize.L + NotesDaysWidth, -(top + 48f)));
        Button day = BuildListRow(days, "DayTemplate");

        TMP_Text title = Text(win, "PageTitle", "", PcType.Title, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, Ink,
                              ThemeRoleId.WindowBody, style: FontStyles.Bold);
        PlaceRect(title.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(right, -(top + 40f)), new Vector2(-PcSize.L, -top));
        Chrome(title, PcType.Title);
        TMP_Text clipsLabel = Text(win, "ClippingsLabel", null, PcType.Caption, TextAlignmentOptions.BottomLeft, new Vector2(0f, 0.82f), new Vector2(0.6f, 0.87f), Ink,
                                   ThemeRoleId.WindowBody, "notes.clippings", FontStyles.Bold);
        PlaceRect(clipsLabel.transform, new Vector2(0f, 0.82f), new Vector2(0.6f, 0.87f), new Vector2(right, 0f), Vector2.zero);
        Button paste = MakeButton(win, "PasteButton", null, new Vector2(1f, 0.82f), new Vector2(1f, 0.875f), null, ThemeRoleId.Button, "notes.paste");
        PlaceRect(paste.transform, new Vector2(1f, 0.82f), new Vector2(1f, 0.875f), new Vector2(-(PcSize.L + 220f), 0f), new Vector2(-PcSize.L, 0f));
        ButtonLabel(paste, PcType.Body);
        RectTransform clips = BuildScrollList(win, "Clippings", new Vector2(0f, 0.47f), new Vector2(1f, 0.81f), 4f);
        PlaceRect(clips.parent.parent, new Vector2(0f, 0.47f), new Vector2(1f, 0.81f), new Vector2(right, 0f), new Vector2(-PcSize.L, 0f));
        TMP_Text group = LayoutText(clips, "GroupTemplate", PcType.Caption, FontStyles.Bold, ThemeRoleId.InputField);
        RectTransform card = BuildClipCard(clips, "ClipTemplate");
        TMP_Text hint = Text(win, "HintText", null, PcType.Caption, TextAlignmentOptions.Center, new Vector2(0f, 0.55f), new Vector2(1f, 0.73f), Ink,
                             ThemeRoleId.InputField, "notes.hint", FontStyles.Italic);
        PlaceRect(hint.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.78f), new Vector2(right + PcSize.L, 0f), new Vector2(-(PcSize.L * 2f), 0f));
        Chrome(hint, PcType.Caption, true);
        hint.raycastTarget = false;

        TMP_Text notesLabel = Text(win, "NotesLabel", null, PcType.Caption, TextAlignmentOptions.BottomLeft, new Vector2(0f, 0.41f), new Vector2(0.6f, 0.455f), Ink,
                                   ThemeRoleId.WindowBody, "notes.notes", FontStyles.Bold);
        PlaceRect(notesLabel.transform, new Vector2(0f, 0.405f), new Vector2(0.6f, 0.455f), new Vector2(right, 0f), Vector2.zero);
        TMP_Text counter = Text(win, "CounterText", "", PcType.Caption, TextAlignmentOptions.BottomRight, new Vector2(0.6f, 0.41f), new Vector2(1f, 0.455f), Ink,
                                ThemeRoleId.WindowBody);
        PlaceRect(counter.transform, new Vector2(0.6f, 0.405f), new Vector2(1f, 0.455f), Vector2.zero, new Vector2(-PcSize.L, 0f));
        Chrome(counter, PcType.Caption);
        TMP_InputField field = BuildInputField(win, "NotesField", "notes.placeholder", new Vector2(0f, 0f), new Vector2(1f, 0.4f));
        PlaceRect(field.transform, Vector2.zero, new Vector2(1f, 0.4f), new Vector2(right, PcSize.L), new Vector2(-PcSize.L, 0f));
        field.textComponent.alignment = TextAlignmentOptions.TopLeft;
        field.textComponent.textWrappingMode = TextWrappingModes.Normal;
        ((TMP_Text)field.placeholder).alignment = TextAlignmentOptions.TopLeft;
        ((TMP_Text)field.placeholder).textWrappingMode = TextWrappingModes.Normal;
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
    /// The Settings window (piece 6 U12, piece 9 R17, redesign phase 25 SG1;
    /// the PC UX redesign §7: System Settings' grouped rows), rebuilt fresh: a
    /// column of titled groups, each heading at Title size over a row of
    /// choices that share the row (the chosen one in the accent colours:
    /// SettingsWindowController): Language (Follow history / Always English,
    /// and the lock's line under them, hidden until the Translation Lens's day),
    /// Motion (Full / Reduced, and the Motion intensity slider under them,
    /// 0-100 %: the kit's sunk track and bone thumb), Desktop icons open with (Double click /
    /// Single click) and Reset icon positions (its icons wired by
    /// WireIconSettings), Investigation's Text size (a choice per zoom level,
    /// redesign phase 20; the step hints' pair is gone with the hints),
    /// Keyboard (Show shortcuts, which opens the F1 card: BuildShortcutCard),
    /// then the note at Caption size.
    /// </summary>
    private static DesktopWindow BuildSettingsWindow(Transform windowLayer)
    {
        DesktopConfigSO config = EnsureDesktopConfig();
        DestroyChildIfPresent(windowLayer, "SettingsWindow");
        DesktopWindow chrome = BuildOSWindow(windowLayer, "SettingsWindow", "window.settings", null, null, config.settingsWindowSize);
        Transform win = chrome.transform;
        Object.DestroyImmediate(win.Find("Body").gameObject);

        Transform column = Panel(win, "Column", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(column, Vector2.zero, Vector2.one, new Vector2(PcSize.L + 8f, PcSize.L), new Vector2(-(PcSize.L + 8f), -(config.titleBarHeight + PcSize.M)));
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(column.gameObject);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        SettingsHeading(column, "LanguageLabel", "settings.language");
        Transform language = SettingsRow(column, "LanguageRow");
        Button follow = SettingsChoice(language, "FollowHistoryButton", "settings.followHistory");
        Button english = SettingsChoice(language, "AlwaysEnglishButton", "settings.alwaysEnglish");
        TMP_Text languageLock = Text(column, "LanguageLockText", null, PcType.Caption, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Ink,
                                     ThemeRoleId.WindowBody, null);
        Chrome(languageLock, PcType.Caption, true);
        languageLock.raycastTarget = false;
        languageLock.gameObject.SetActive(false); // shown from the Translation Lens's day (SettingsWindowController)

        SettingsHeading(column, "MotionLabel", "settings.motion");
        Transform motion = SettingsRow(column, "MotionRow");
        Button full = SettingsChoice(motion, "FullMotionButton", "settings.motionFull");
        Button reduced = SettingsChoice(motion, "ReducedMotionButton", "settings.motionReduced");
        Transform intensityRow = SettingsRow(column, "MotionIntensityRow");
        TMP_Text intensityLabel = Text(intensityRow, "MotionIntensityLabel", null, PcType.Body, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, Ink,
                                       ThemeRoleId.WindowBody, "settings.motionIntensity");
        Chrome(intensityLabel, PcType.Body);
        intensityLabel.raycastTarget = false;
        GetOrAdd<LayoutElement>(intensityLabel.gameObject).flexibleWidth = 1f;
        Slider intensity = SettingsSlider(intensityRow, "MotionIntensitySlider");
        TMP_Text intensityValue = Text(intensityRow, "MotionIntensityValue", null, PcType.Body, TextAlignmentOptions.MidlineRight, Vector2.zero, Vector2.one, Ink,
                                       ThemeRoleId.WindowBody);
        Chrome(intensityValue, PcType.Body);
        intensityValue.raycastTarget = false;
        GetOrAdd<LayoutElement>(intensityValue.gameObject).flexibleWidth = 0.5f;
        // Camera sway (Saleh 2026-10-07: off by default, it caused motion sickness): its word, then the Off / On pair.
        Transform swayRow = SettingsRow(column, "CameraSwayRow");
        TMP_Text swayLabel = Text(swayRow, "CameraSwayLabel", null, PcType.Body, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, Ink,
                                  ThemeRoleId.WindowBody, "settings.cameraSway");
        Chrome(swayLabel, PcType.Body);
        swayLabel.raycastTarget = false;
        GetOrAdd<LayoutElement>(swayLabel.gameObject).flexibleWidth = 1f;
        Button swayOff = SettingsChoice(swayRow, "CameraSwayOffButton", "settings.cameraSwayOff");
        Button swayOn = SettingsChoice(swayRow, "CameraSwayOnButton", "settings.cameraSwayOn");

        SettingsHeading(column, "DesktopLabel", "settings.desktop");
        Transform icons = SettingsRow(column, "IconOpenRow");
        Button iconDouble = SettingsChoice(icons, "IconDoubleClickButton", "settings.iconDouble");
        Button iconSingle = SettingsChoice(icons, "IconSingleClickButton", "settings.iconSingle");
        Button resetIcons = SettingsChoice(SettingsRow(column, "ResetIconsRow"), "ResetIconsButton", "settings.resetIcons");

        SettingsHeading(column, "InvestigationLabel", "settings.investigation");
        Transform sizes = SettingsRow(column, "TextSizeRow");
        var textSizes = new List<Object>();
        foreach (int level in config.zoomLevels)
        {
            Button size = SettingsChoice(sizes, "TextSizeButton_" + level, null);
            TMP_Text sizeLabel = size.transform.Find("Label").GetComponent<TMP_Text>();
            sizeLabel.text = UiText.Format("settings.textSize", level);
            SceneUiKit.Tag(sizeLabel, ThemeRoleId.Button, ThemePart.Ink, null, FontStyles.Normal, ThemeTextKind.Button);
            textSizes.Add(size);
        }

        SettingsHeading(column, "KeyboardLabel", "settings.keyboard");
        Transform shortcutsRow = SettingsRow(column, "ShortcutsRow");
        Button shortcuts = SettingsChoice(shortcutsRow, "ShowShortcutsButton", "settings.showShortcuts");
        SettingsChoice(shortcutsRow, GuideReplayButton, "settings.replayTutorial"); // wired to the desk's guide by BuildGuide

        TMP_Text note = Text(column, "NoteText", null, PcType.Caption, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Ink,
                             ThemeRoleId.WindowBody, "settings.note");
        Chrome(note, PcType.Caption, true);
        note.margin = new Vector4(0f, PcSize.M, 0f, 0f);
        note.raycastTarget = false;

        // The shortcut card (F1; OfficeSceneUIBuilder.Keys), rebuilt fresh, so it keeps its place after the rebuilt windows.
        DesktopWindow card = BuildShortcutCard(windowLayer);

        SettingsWindowController controller = GetOrAdd<SettingsWindowController>(win.gameObject);
        var so = new SerializedObject(controller);
        SetRef(so, "followHistoryButton", follow);
        SetRef(so, "alwaysEnglishButton", english);
        SetRef(so, "languageLockText", languageLock);
        SetRef(so, "fullMotionButton", full);
        SetRef(so, "reducedMotionButton", reduced);
        SetRef(so, "motionIntensitySlider", intensity);
        SetRef(so, "motionIntensityText", intensityValue);
        SetRef(so, "cameraSwayOffButton", swayOff);
        SetRef(so, "cameraSwayOnButton", swayOn);
        SetRef(so, "iconDoubleClickButton", iconDouble);
        SetRef(so, "iconSingleClickButton", iconSingle);
        SetRef(so, "resetIconsButton", resetIcons);
        SetRef(so, "kit", _kit);
        SerializedArrays.Set(so, "textSizeButtons", textSizes);
        SetRef(so, "config", config);
        SetRef(so, "showShortcutsButton", shortcuts);
        SetRef(so, "shortcutsWindow", card);
        so.ApplyModifiedProperties();
        win.gameObject.SetActive(false);
        return chrome;
    }

    /// <summary>A Settings heading's height (reference px): 40 since the Camera sway row joined the Motion group, so the window still holds every row and the note.</summary>
    private const float SettingsHeadingHeight = 40f;

    /// <summary>A Settings group's heading: Title size, bold, with room above it.</summary>
    private static void SettingsHeading(Transform column, string name, string key)
    {
        TMP_Text heading = Text(column, name, null, PcType.Title, TextAlignmentOptions.BottomLeft, Vector2.zero, Vector2.one, Ink, ThemeRoleId.WindowBody, key,
                                FontStyles.Bold, ThemeTextKind.Heading, true);
        heading.raycastTarget = false;
        SetLayoutHeight(heading, SettingsHeadingHeight);
    }

    /// <summary>A Settings row: its choices share its width, PcSize.Row tall.</summary>
    private static Transform SettingsRow(Transform column, string name)
    {
        Transform row = Panel(column, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        SetLayoutHeight(row, PcSize.Row);
        HorizontalLayoutGroup line = GetOrAdd<HorizontalLayoutGroup>(row.gameObject);
        line.spacing = PcSize.S;
        line.childControlWidth = true;
        line.childControlHeight = true;
        line.childForceExpandWidth = true;
        line.childForceExpandHeight = true;
        return row;
    }

    /// <summary>
    /// A slider in a Settings row (0 to 100, whole numbers), twice a label's
    /// share of the row: a track with a thumb that slides along it, skinned
    /// as the kit's sunk scroll track and its bone thumb (sheet 02; the role
    /// pass leaves them), the thumb its graphic. Its value is set through the
    /// serialized field (a setter would drive the thumb's anchors at build).
    /// </summary>
    private static Slider SettingsSlider(Transform row, string name)
    {
        Transform track = Panel(row, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, XpFace, ThemeRoleId.WindowBody);
        GetOrAdd<LayoutElement>(track.gameObject).flexibleWidth = 2f;
        Transform area = Panel(track, "HandleSlideArea", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(area, Vector2.zero, Vector2.one, new Vector2(PcSize.M, 0f), new Vector2(-PcSize.M, 0f));
        Transform thumb = Panel(area, "Handle", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, XpBlue, ThemeRoleId.TitleBar);
        PlaceRect(thumb, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-PcSize.M, 0f), new Vector2(PcSize.M, 0f));
        Slider slider = GetOrAdd<Slider>(track.gameObject);
        var so = new SerializedObject(slider);
        so.FindProperty("m_HandleRect").objectReferenceValue = thumb;
        so.FindProperty("m_TargetGraphic").objectReferenceValue = thumb.GetComponent<Image>();
        so.FindProperty("m_MinValue").floatValue = 0f;
        so.FindProperty("m_MaxValue").floatValue = 100f;
        so.FindProperty("m_WholeNumbers").boolValue = true;
        so.FindProperty("m_Value").floatValue = 100f;
        so.ApplyModifiedProperties();
        if (_kit != null)
        {
            KitSkin(track, "scroll_track", _kit.desktopScale);
            slider.targetGraphic = KitSkin(thumb, "scroll_thumb", _kit.desktopScale);
            slider.transition = Selectable.Transition.None;
        }
        return slider;
    }

    /// <summary>A choice in a Settings row: a Button-role plate with its keyed label at Body size.</summary>
    private static Button SettingsChoice(Transform row, string name, string key)
    {
        Button choice = MakeButton(row, name, null, Vector2.zero, Vector2.one, null, ThemeRoleId.Button, key);
        GetOrAdd<LayoutElement>(choice.gameObject).flexibleWidth = 1f;
        ButtonLabel(choice, PcType.Body, TextAlignmentOptions.Center, 8f);
        return choice;
    }

    // -----------------------------
    // Parts
    // -----------------------------

    /// <summary>
    /// A scrolling vertical list: a box (the sidebar's surface by default, so
    /// its rows' white plates stand out; the scroll's raycast target) with a masked viewport and a content that
    /// grows with its children (a vertical layout, fitted to its preferred
    /// height). Returns the content.
    /// </summary>
    private static RectTransform BuildScrollList(Transform parent, string name, Vector2 aMin, Vector2 aMax, float spacing, Color? fill = null,
                                                 ThemeRoleId role = ThemeRoleId.Sidebar)
    {
        Transform box = Panel(parent, name, aMin, aMax, Vector2.zero, Vector2.zero, fill ?? XpFace, role);
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

    /// <summary>
    /// A vertical scrollbar at the right of <paramref name="area"/>,
    /// <paramref name="width"/> wide (callers may place it): a track in
    /// <paramref name="track"/> (<paramref name="trackRole"/>) whose handle is
    /// <paramref name="handle"/> (<paramref name="handleRole"/>), reading bottom
    /// to top. Its handle, direction, size and value are set through the
    /// serialized fields, not the setters: a setter drives the handle's anchors
    /// at once (even under a closed window), and a driven RectTransform is saved
    /// zeroed, so the saved scene would differ from the built one. The scrollbar
    /// drives the handle itself once it shows; until then the handle fills the
    /// track (a list that fits).
    /// </summary>
    private static Scrollbar BuildScrollbar(Transform area, float width, Color track, ThemeRoleId trackRole, Color handle, ThemeRoleId handleRole)
    {
        Transform bar = Panel(area, "Scrollbar", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, track, trackRole);
        PlaceRect(bar, new Vector2(1f, 0f), Vector2.one, new Vector2(-width, 0f), Vector2.zero);
        Transform slide = Panel(bar, "SlidingArea", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        SetAnchors(slide, Vector2.zero, Vector2.one);
        Transform grip = Panel(slide, "Handle", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, handle, handleRole);
        SetAnchors(grip, Vector2.zero, Vector2.one);
        Scrollbar scrollbar = bar.gameObject.AddComponent<Scrollbar>();
        var so = new SerializedObject(scrollbar);
        so.FindProperty("m_Direction").enumValueIndex = (int)Scrollbar.Direction.BottomToTop;
        so.FindProperty("m_HandleRect").objectReferenceValue = grip;
        so.FindProperty("m_TargetGraphic").objectReferenceValue = grip.GetComponent<Image>();
        so.FindProperty("m_Size").floatValue = 1f;
        so.FindProperty("m_Value").floatValue = 0f;
        so.ApplyModifiedProperties();
        if (_kit != null)
        {
            // Sheet 02: the kit's sunk track and its bone thumb; the scrollbar keeps the thumb as its graphic.
            KitSkin(bar, "scroll_track", _kit.desktopScale);
            Image thumb = KitSkin(grip, "scroll_thumb", _kit.desktopScale);
            scrollbar.targetGraphic = thumb;
            scrollbar.transition = Selectable.Transition.None;
        }
        return scrollbar;
    }

    /// <summary>A list row: a button of PcSize.Row whose left-aligned label (Body size, rich text: a second line may follow at Caption size) wraps, over a "Selected" plate across the whole row (hidden; the window shows it on the open row: a tint, never a side stripe).</summary>
    private static Button BuildListRow(Transform list, string name)
    {
        Button row = MakeButton(list, name, "", Vector2.zero, Vector2.one, null, ThemeRoleId.Button);
        GetOrAdd<LayoutElement>(row.gameObject).minHeight = PcSize.Row;
        VerticalLayoutGroup grow = GetOrAdd<VerticalLayoutGroup>(row.gameObject);
        grow.padding = new RectOffset(0, 0, 6, 6);
        grow.childControlWidth = true;
        grow.childControlHeight = true;
        grow.childForceExpandWidth = true;
        grow.childForceExpandHeight = false;
        Transform plate = Panel(row.transform, "Selected", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 0.92f, 0.35f, 0.7f),
                                ThemeRoleId.SelectionHighlight);
        plate.GetComponent<Image>().raycastTarget = false;
        GetOrAdd<LayoutElement>(plate.gameObject).ignoreLayout = true;
        plate.SetAsFirstSibling();
        plate.gameObject.SetActive(false);
        TMP_Text label = row.transform.Find("Label").GetComponent<TMP_Text>();
        Chrome(label, PcType.Body, true);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.margin = new Vector4(PcSize.M, 2f, PcSize.S, 2f);
        label.lineSpacing = -6f;
        label.richText = true;
        if (_kit != null)
        {
            // Sheet 02: a list row is the kit's card row; the open one its selected row.
            KitSkin(row, "listrow", _kit.desktopScale);
            KitSkin(plate, "listrow_selected", _kit.desktopScale);
            KitType(label, KitText.ListTitle, row, _kit.inkOnLight);
        }
        return row;
    }

    /// <summary>A clipping's card: its text (wrapping) over where it came from, and an X at the top right.</summary>
    private static RectTransform BuildClipCard(Transform list, string name)
    {
        var card = (RectTransform)Panel(list, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 0.97f, 0.8f, 1f), ThemeRoleId.StickyNote);
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(card.gameObject);
        layout.padding = new RectOffset(12, 128, 8, 8);
        layout.spacing = 2f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text text = LayoutText(card, "Text", PcType.Body, FontStyles.Normal, ThemeRoleId.StickyNote);
        text.color = Ink;
        TMP_Text label = LayoutText(card, "Label", PcType.Caption, FontStyles.Italic, ThemeRoleId.StickyNote);
        label.color = Ink;

        Button remove = MakeButton(card, "RemoveButton", null, new Vector2(1f, 1f), new Vector2(1f, 1f), null, ThemeRoleId.Button, "notes.remove");
        var rt = (RectTransform)remove.transform;
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(112f, 36f);
        rt.anchoredPosition = new Vector2(-6f, -6f);
        ButtonLabel(remove, PcType.Caption, TextAlignmentOptions.Center, 4f);
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

}
