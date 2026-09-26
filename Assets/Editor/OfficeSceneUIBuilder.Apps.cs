using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's desktop apps (redesign phase 25; the PC spec's ML1,
/// AC1, NT1, SG1, §2.12-2.15): the Mail window (the inbox and the memo form)
/// with its feed on the desktop canvas, the Citizen Account window (the
/// clerk's Record Extract and Statement), the Notes window (days, clippings,
/// the typed notes; the Notes desktop tile now opens it and the placeholder
/// window goes), the Settings window in sections (Language, Motion,
/// Keyboard) with the shortcut card, the desktop's app registry
/// (DesktopApps: its OpenApp is what phase 17's icons call), and the Start
/// menu's app entries (Mail, Citizen Account, Notes above Settings). The
/// forms are drawn with today's widgets on diegetic paper (fixed colours,
/// never themed), each in its own window component, so phase 5's forms
/// engine replaces one drawing method per form. The three app windows are
/// rebuilt fresh on each run (like Records); Settings keeps its objects.
/// Part of <see cref="OfficeSceneUIBuilder"/>; Build() calls BuildApps after
/// the desktop shell and before the window manager (which wires every
/// window's chrome).
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The forms' paper (the memo, the extract and the statement).</summary>
    private static readonly Color FormPaper = new Color(0.965f, 0.95f, 0.9f, 1f);

    /// <summary>A form box's fill, a shade lighter than the paper.</summary>
    private static readonly Color FormBox = new Color(1f, 0.995f, 0.97f, 1f);

    /// <summary>A form box's outline and the forms' rules.</summary>
    private static readonly Color FormRule = new Color(0.45f, 0.42f, 0.35f, 1f);

    /// <summary>The stamp's red ink.</summary>
    private static readonly Color StampInk = new Color(0.66f, 0.12f, 0.1f, 1f);

    /// <summary>A list row's height (the inbox, the day list).</summary>
    private const float AppRowHeight = 52f;

    /// <summary>A Start menu entry's height (as BuildDesktopShell's).</summary>
    private const float StartEntryHeight = 46f;

    /// <summary>The gap between Start menu entries (as BuildDesktopShell's).</summary>
    private const float StartEntryGap = 4f;

    /// <summary>
    /// Builds the three apps, wires Settings' Keyboard section, lists every
    /// app in DesktopApps and puts the apps at the top of the Start menu.
    /// <paramref name="rulesWindow"/> is what Mail's Rules link opens.
    /// </summary>
    private static void BuildApps(Canvas canvas, Transform windowLayer, Transform iconGrid, GameManager game, DesktopWindow rulesWindow)
    {
        Transform root = canvas.transform;
        DesktopConfigSO config = EnsureDesktopConfig();
        DesktopApps apps = GetOrAdd<DesktopApps>(root.gameObject);
        MailFeed feed = GetOrAdd<MailFeed>(root.gameObject);

        DesktopWindow mail = BuildMailWindow(windowLayer, config, feed, apps, rulesWindow, out TMP_Text mailTitle);
        DesktopWindow account = BuildAccountWindow(windowLayer, config);
        DesktopWindow notes = BuildNotesWindow(windowLayer, config);

        // The Notes tile opens the real app; its placeholder window goes (its body.notes string retired with it).
        DestroyChildIfPresent(windowLayer, "IconNotesWindow");
        BuildDesktopIcon(iconGrid, "IconNotes", "icon.notes", notes, "");

        Transform settingsWin = windowLayer.Find("SettingsWindow");
        Transform internetWin = windowLayer.Find("IconInternetWindow");
        var soApps = new SerializedObject(apps);
        SetRef(soApps, "shell", root.GetComponent<DesktopShell>());
        SetApps(soApps, ("mail", mail), ("citizen_account", account), ("notes", notes),
                ("settings", settingsWin != null ? settingsWin.GetComponent<DesktopWindow>() : null),
                ("internet", internetWin != null ? internetWin.GetComponent<DesktopWindow>() : null));
        soApps.ApplyModifiedProperties();

        TMP_Text mailEntry = BuildStartMenuApps(root, apps);

        var soFeed = new SerializedObject(feed);
        SetRef(soFeed, "game", game);
        SetRef(soFeed, "startEntryLabel", mailEntry);
        SetRef(soFeed, "windowTitle", mailTitle);
        soFeed.ApplyModifiedProperties();
    }

    /// <summary>Lists the apps that have a window (an id whose window is missing is left out).</summary>
    private static void SetApps(SerializedObject so, params (string id, DesktopWindow window)[] apps)
    {
        SerializedProperty list = so.FindProperty("apps");
        list.arraySize = 0;
        foreach ((string id, DesktopWindow window) in apps)
        {
            if (window == null)
                continue;
            list.arraySize++;
            SerializedProperty app = list.GetArrayElementAtIndex(list.arraySize - 1);
            app.FindPropertyRelative("id").stringValue = id;
            app.FindPropertyRelative("window").objectReferenceValue = window;
        }
    }

    /// <summary>
    /// Mail, Citizen Account and Notes at the top of the Start menu (DK8:
    /// the apps, then Settings, Turn off screen and Quit game), each opening
    /// its app through DesktopApps.OpenApp; the menu grows to hold them.
    /// Returns the Mail entry's label (the feed writes the unread count into it).
    /// </summary>
    private static TMP_Text BuildStartMenuApps(Transform root, DesktopApps apps)
    {
        Transform menu = root.Find("StartMenu");
        var entries = new (string name, string id, string key)[]
        {
            ("MailEntry", "mail", null), ("AccountEntry", "citizen_account", "startmenu.account"), ("NotesEntry", "notes", "startmenu.notes")
        };
        TMP_Text mailLabel = null;
        for (int i = 0; i < entries.Length; i++)
        {
            Button entry = MakeButton(menu, entries[i].name, entries[i].key == null ? UiText.Get("startmenu.mail") : null, Vector2.zero, Vector2.one,
                                      new Color(0.2f, 0.25f, 0.35f, 1f), ThemeRoleId.MenuEntry, entries[i].key);
            SetLayoutHeight(entry, StartEntryHeight);
            entry.transform.SetSiblingIndex(i);
            WirePersistentString(entry, "m_OnClick", apps, nameof(DesktopApps.OpenApp), entries[i].id);
            if (entries[i].key == null)
                mailLabel = entry.transform.Find("Label").GetComponent<TMP_Text>();
        }

        int count = 0;
        foreach (Transform child in menu)
            if (child.GetComponent<Button>() != null)
                count++;
        float height = count * StartEntryHeight + (count - 1) * StartEntryGap + 2 * VLayoutPadding;
        var rt = (RectTransform)menu;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, StartMenuCentre(height));
        return mailLabel;
    }

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

    /// <summary>The Mail window: INBOX (a scrolling list of message rows) on the left, the memo form on the right. Rebuilt fresh.</summary>
    private static DesktopWindow BuildMailWindow(Transform windowLayer, DesktopConfigSO config, MailFeed feed, DesktopApps apps, DesktopWindow rulesWindow, out TMP_Text title)
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

        Transform page = Panel(win, "MemoPage", new Vector2(0.38f, 0.02f), new Vector2(0.98f, 0.935f), Vector2.zero, Vector2.zero, FormPaper, ThemeRoleId.DiegeticPaper);
        TMP_Text select = Text(page, "SelectText", null, 18, TextAlignmentOptions.Center, new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.6f), Ink,
                               ThemeRoleId.DiegeticRow, "mail.select", FontStyles.Italic);
        Transform memo = Panel(page, "Memo", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        FormTitle(memo, "mail.form.title", "TC-950", new Vector2(0.03f, 0.915f), new Vector2(0.97f, 0.985f));
        TMP_Text to = FormField(memo, "To", "mail.form.to", new Vector2(0.03f, 0.8f), new Vector2(0.6f, 0.9f));
        TMP_Text date = FormField(memo, "Date", "mail.form.date", new Vector2(0.62f, 0.8f), new Vector2(0.97f, 0.9f));
        TMP_Text from = FormField(memo, "From", "mail.form.from", new Vector2(0.03f, 0.69f), new Vector2(0.6f, 0.79f));
        TMP_Text reference = FormField(memo, "Ref", "mail.form.ref", new Vector2(0.62f, 0.69f), new Vector2(0.97f, 0.79f));
        TMP_Text subject = FormField(memo, "Subject", "mail.form.subject", new Vector2(0.03f, 0.58f), new Vector2(0.97f, 0.68f));

        TMP_Text body = Text(memo, "BodyText", "", 17, TextAlignmentOptions.TopLeft, new Vector2(0.04f, 0.23f), new Vector2(0.96f, 0.56f), Ink, ThemeRoleId.DiegeticRow);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Ellipsis;
        body.enableAutoSizing = true;
        body.fontSizeMin = 13f;
        body.fontSizeMax = 17f;

        Button link = MakeButton(memo, "LinkButton", "", new Vector2(0.04f, 0.145f), new Vector2(0.62f, 0.215f), new Color(0.15f, 0.3f, 0.5f, 1f), ThemeRoleId.SearchButton);
        FitLabel(link, 17f);

        Panel(memo, "SignatureRule", new Vector2(0.04f, 0.11f), new Vector2(0.58f, 0.113f), Vector2.zero, Vector2.zero, FormRule, ThemeRoleId.DiegeticPaper);
        TMP_Text signature = Text(memo, "SignatureText", "", 16, TextAlignmentOptions.TopLeft, new Vector2(0.04f, 0.03f), new Vector2(0.62f, 0.105f), Ink,
                                  ThemeRoleId.DiegeticRow, style: FontStyles.Italic);
        signature.raycastTarget = false;
        BuildStamp(memo, "Stamp", "mail.stamp", new Vector2(0.68f, 0.03f), new Vector2(0.95f, 0.15f));

        MailWindow component = win.gameObject.AddComponent<MailWindow>();
        var so = new SerializedObject(component);
        SetRef(so, "feed", feed);
        SetRef(so, "apps", apps);
        SetRef(so, "rulesWindow", rulesWindow);
        SetRef(so, "listRoot", list);
        SetRef(so, "rowTemplate", row);
        SetRef(so, "emptyText", empty);
        SetRef(so, "memoRoot", memo.gameObject);
        SetRef(so, "selectText", select);
        SetRef(so, "toText", to);
        SetRef(so, "fromText", from);
        SetRef(so, "dateText", date);
        SetRef(so, "refText", reference);
        SetRef(so, "subjectText", subject);
        SetRef(so, "bodyText", body);
        SetRef(so, "signatureText", signature);
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
    /// The Citizen Account window: one scrolling sheet of paper holding the
    /// Record Extract (TC-901: title, the holder's line, the grouped rows) and
    /// the Statement (TC-960: title, the column heads, the day rows, the unit,
    /// the fine print, an empty stamp box). Rebuilt fresh.
    /// </summary>
    private static DesktopWindow BuildAccountWindow(Transform windowLayer, DesktopConfigSO config)
    {
        DestroyChildIfPresent(windowLayer, "AccountWindow");
        DesktopWindow chrome = BuildOSWindow(windowLayer, "AccountWindow", "window.account", null, null, config.accountWindowSize);
        Transform win = chrome.transform;
        Object.DestroyImmediate(win.Find("Body").gameObject);

        RectTransform sheet = BuildScrollList(win, "Sheet", new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.935f), 6f, FormPaper, ThemeRoleId.DiegeticPaper);
        VerticalLayoutGroup layout = sheet.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 12, 16);

        FormTitleRow(sheet, "ExtractTitle", "account.form.title", "TC-901");
        TMP_Text query = LayoutText(sheet, "QueryText", 16, FontStyles.Italic, ThemeRoleId.DiegeticRow);
        RectTransform extract = LayoutBlock(sheet, "Extract", 4f);
        TMP_Text group = LayoutText(extract, "GroupTemplate", 15, FontStyles.Bold, ThemeRoleId.DiegeticLabel);
        group.color = RecordLabelInk;
        group.margin = new Vector4(0f, 8f, 0f, 0f);
        RectTransform row = BuildLabelledRow(extract, "RowTemplate");

        Panel(sheet, "StatementRule", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FormRule, ThemeRoleId.DiegeticPaper);
        SetLayoutHeight(sheet.Find("StatementRule"), 2f);
        FormTitleRow(sheet, "StatementTitle", "account.statement.title", "TC-960");
        TMP_Text head = LayoutText(sheet, "StatementHead", 13, FontStyles.Bold, ThemeRoleId.DiegeticLabel);
        head.color = RecordLabelInk;
        RectTransform statement = LayoutBlock(sheet, "Statement", 2f);
        TMP_Text line = LayoutText(statement, "StatementRowTemplate", 15, FontStyles.Normal, ThemeRoleId.DiegeticRow);
        TMP_Text none = LayoutText(statement, "StatementEmpty", 15, FontStyles.Italic, ThemeRoleId.DiegeticRow);
        none.text = UiText.Get("account.statement.none");
        TMP_Text unit = LayoutText(sheet, "UnitText", 13, FontStyles.Normal, ThemeRoleId.DiegeticRow);
        TMP_Text fine = LayoutText(sheet, "FinePrint", 13, FontStyles.Italic, ThemeRoleId.DiegeticRow);
        fine.text = UiText.Get("account.finePrint");
        Transform stampRow = Panel(sheet, "StampRow", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        SetLayoutHeight(stampRow, 76f);
        Transform stampBox = Panel(stampRow, "StampBox", new Vector2(0.72f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero, FormBox, ThemeRoleId.DiegeticPaper);
        DrawOutline(stampBox);

        AccountWindow component = win.gameObject.AddComponent<AccountWindow>();
        var so = new SerializedObject(component);
        SetRef(so, "queryText", query);
        SetRef(so, "extractRoot", extract);
        SetRef(so, "groupTemplate", group);
        SetRef(so, "rowTemplate", row);
        SetRef(so, "statementHead", head);
        SetRef(so, "statementRoot", statement);
        SetRef(so, "statementRowTemplate", line);
        SetRef(so, "statementEmpty", none);
        SetRef(so, "unitText", unit);
        so.ApplyModifiedProperties();

        group.gameObject.SetActive(false);
        row.gameObject.SetActive(false);
        line.gameObject.SetActive(false);
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
    /// English), Motion (Full / Reduced), Keyboard (Show shortcuts, which
    /// opens the shortcut card), then the note. The Desktop and Investigation
    /// sections come with the features they set (phases 17, 18, 20 and 21).
    /// Existing objects are kept; every row's anchors are re-applied on each build.
    /// </summary>
    private static DesktopWindow BuildSettingsWindow(Transform windowLayer)
    {
        DesktopWindow chrome = BuildOSWindow(windowLayer, "SettingsWindow", "window.settings", "settings.language", null, EnsureDesktopConfig().settingsWindowSize);
        Transform win = chrome.transform;
        Heading(win.Find("Body").GetComponent<TMP_Text>(), "settings.language", new Vector2(0.05f, 0.855f), new Vector2(0.95f, 0.915f));
        Button follow = MakeButton(win, "FollowHistoryButton", null, new Vector2(0.05f, 0.76f), new Vector2(0.48f, 0.845f), null, ThemeRoleId.Button, "settings.followHistory");
        SetAnchors(follow.transform, new Vector2(0.05f, 0.76f), new Vector2(0.48f, 0.845f));
        Button english = MakeButton(win, "AlwaysEnglishButton", null, new Vector2(0.52f, 0.76f), new Vector2(0.95f, 0.845f), null, ThemeRoleId.Button, "settings.alwaysEnglish");
        SetAnchors(english.transform, new Vector2(0.52f, 0.76f), new Vector2(0.95f, 0.845f));

        TMP_Text motion = Text(win, "MotionLabel", null, 20, TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.665f), new Vector2(0.95f, 0.725f), Ink,
                               ThemeRoleId.WindowBody, "settings.motion");
        Heading(motion, "settings.motion", new Vector2(0.05f, 0.665f), new Vector2(0.95f, 0.725f));
        Button full = MakeButton(win, "FullMotionButton", null, new Vector2(0.05f, 0.57f), new Vector2(0.48f, 0.655f), null, ThemeRoleId.Button, "settings.motionFull");
        SetAnchors(full.transform, new Vector2(0.05f, 0.57f), new Vector2(0.48f, 0.655f));
        Button reduced = MakeButton(win, "ReducedMotionButton", null, new Vector2(0.52f, 0.57f), new Vector2(0.95f, 0.655f), null, ThemeRoleId.Button, "settings.motionReduced");
        SetAnchors(reduced.transform, new Vector2(0.52f, 0.57f), new Vector2(0.95f, 0.655f));

        TMP_Text keyboard = Text(win, "KeyboardLabel", null, 20, TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.475f), new Vector2(0.95f, 0.535f), Ink,
                                 ThemeRoleId.WindowBody, "settings.keyboard");
        Heading(keyboard, "settings.keyboard", new Vector2(0.05f, 0.475f), new Vector2(0.95f, 0.535f));
        Button shortcuts = MakeButton(win, "ShowShortcutsButton", null, new Vector2(0.05f, 0.38f), new Vector2(0.48f, 0.465f), null, ThemeRoleId.Button, "settings.showShortcuts");
        SetAnchors(shortcuts.transform, new Vector2(0.05f, 0.38f), new Vector2(0.48f, 0.465f));

        TMP_Text note = Text(win, "NoteText", null, 17, TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.34f), Ink,
                             ThemeRoleId.WindowBody, "settings.note");
        SetAnchors(note.transform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.34f));
        note.text = UiText.Get("settings.note");
        note.textWrappingMode = TextWrappingModes.Normal;

        // The shortcut card: the desktop's keys today (phase 20's F1 card replaces it). Rebuilt fresh, so it keeps its place after the rebuilt windows.
        DestroyChildIfPresent(windowLayer, "ShortcutsWindow");
        DesktopWindow card = BuildOSWindow(windowLayer, "ShortcutsWindow", "window.shortcuts", "keys.card", null, new Vector2(620f, 300f));
        SetAnchors(card.transform.Find("Body"), new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.84f));

        SettingsWindowController controller = GetOrAdd<SettingsWindowController>(win.gameObject);
        var so = new SerializedObject(controller);
        SetRef(so, "followHistoryButton", follow);
        SetRef(so, "alwaysEnglishButton", english);
        SetRef(so, "fullMotionButton", full);
        SetRef(so, "reducedMotionButton", reduced);
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

    /// <summary>A labelled row of the Record Extract: a box with the label (a fixed column) and the value (wrapping).</summary>
    private static RectTransform BuildLabelledRow(Transform parent, string name)
    {
        var box = (RectTransform)Panel(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FormBox, ThemeRoleId.DiegeticPaper);
        DrawOutline(box);
        HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(box.gameObject);
        layout.padding = new RectOffset(10, 10, 6, 6);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TMP_Text label = LayoutText(box, "Label", 14, FontStyles.Bold, ThemeRoleId.DiegeticLabel);
        label.color = RecordLabelInk;
        LayoutElement labelSize = GetOrAdd<LayoutElement>(label.gameObject);
        labelSize.minWidth = 170f;
        labelSize.preferredWidth = 170f;
        labelSize.flexibleWidth = 0f;
        TMP_Text value = LayoutText(box, "Value", 17, FontStyles.Normal, ThemeRoleId.DiegeticRow);
        GetOrAdd<LayoutElement>(value.gameObject).flexibleWidth = 1f;
        return box;
    }

    /// <summary>A block of a layout (its own vertical layout; its height follows its children).</summary>
    private static RectTransform LayoutBlock(Transform parent, string name, float spacing)
    {
        var block = (RectTransform)Panel(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(block.gameObject);
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return block;
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

    /// <summary>A form's title row in a layout: the title (bold) at the left and the form number at the right.</summary>
    private static void FormTitleRow(Transform parent, string name, string titleKey, string formNumber)
    {
        Transform row = Panel(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        SetLayoutHeight(row, 34f);
        FormTitle(row, titleKey, formNumber, Vector2.zero, Vector2.one);
    }

    /// <summary>A form's title (bold, left) and its number (right) over the given area, with a rule under them.</summary>
    private static void FormTitle(Transform parent, string titleKey, string formNumber, Vector2 aMin, Vector2 aMax)
    {
        Transform area = Panel(parent, "FormTitle", aMin, aMax, Vector2.zero, Vector2.zero, null);
        TMP_Text title = Text(area, "Title", null, 20, TextAlignmentOptions.MidlineLeft, Vector2.zero, new Vector2(0.78f, 1f), Ink,
                              ThemeRoleId.DiegeticRow, titleKey, FontStyles.Bold);
        title.raycastTarget = false;
        TMP_Text number = Text(area, "FormNumber", formNumber, 14, TextAlignmentOptions.MidlineRight, new Vector2(0.78f, 0f), Vector2.one, RecordLabelInk,
                               ThemeRoleId.DiegeticLabel);
        number.raycastTarget = false;
        Panel(area, "Rule", Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(0f, 2f), FormRule, ThemeRoleId.DiegeticPaper);
    }

    /// <summary>A form's labelled box: the label (small, bold) over the value. Returns the value's text.</summary>
    private static TMP_Text FormField(Transform parent, string name, string labelKey, Vector2 aMin, Vector2 aMax)
    {
        Transform box = Panel(parent, name, aMin, aMax, Vector2.zero, Vector2.zero, FormBox, ThemeRoleId.DiegeticPaper);
        DrawOutline(box);
        TMP_Text label = Text(box, "Label", null, 12, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.6f), new Vector2(0.97f, 0.96f), RecordLabelInk,
                              ThemeRoleId.DiegeticLabel, labelKey, FontStyles.Bold);
        label.raycastTarget = false;
        TMP_Text value = Text(box, "Value", "", 18, TextAlignmentOptions.MidlineLeft, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.62f), Ink, ThemeRoleId.DiegeticRow);
        value.textWrappingMode = TextWrappingModes.NoWrap;
        value.overflowMode = TextOverflowModes.Ellipsis;
        value.raycastTarget = false;
        return value;
    }

    /// <summary>A stamp: a red-outlined box, turned a little, with its keyed word.</summary>
    private static void BuildStamp(Transform parent, string name, string key, Vector2 aMin, Vector2 aMax)
    {
        Transform stamp = Panel(parent, name, aMin, aMax, Vector2.zero, Vector2.zero, FormPaper, ThemeRoleId.DiegeticPaper);
        stamp.localRotation = Quaternion.Euler(0f, 0f, 6f);
        DrawOutline(stamp, StampInk, 2f);
        TMP_Text word = Text(stamp, "Word", null, 20, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, StampInk, ThemeRoleId.DiegeticRow, key, FontStyles.Bold);
        word.raycastTarget = false;
    }

    /// <summary>A thin outline round a box (the forms' ruled boxes).</summary>
    private static void DrawOutline(Transform box, Color? colour = null, float width = 1f)
    {
        Outline outline = GetOrAdd<Outline>(box.gameObject);
        outline.effectColor = colour ?? FormRule;
        outline.effectDistance = new Vector2(width, -width);
        outline.useGraphicAlpha = true;
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
