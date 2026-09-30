using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's steps checklist (redesign phase 21; the PC spec's
/// ST1-ST4, SG1; the PC UX redesign IA8, C6): the navigator's last section
/// (StepsPanel: its heading row, a button wired to StepsPanel.Toggle with a
/// chevron, "Checklist" and the count of steps done; the line shown instead of the
/// list; the list, laid out in the sidebar's own scrolling list, with an
/// inactive row, StepRowView: a tick box beside the label that jumps and
/// wraps), the panel's references into the app (and the panel into the app,
/// for Ctrl+Shift+S), the façade and the PC's screen, and Settings'
/// Investigation choice (Checklist shown / Checklist hidden). The section is
/// part of the app's window, which the App partial rebuilds fresh on each
/// run, and so is Settings. Part of <see cref="OfficeSceneUIBuilder"/>.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The section's heading row.</summary>
    private const float StepsHeadingHeight = 48f;

    /// <summary>A step row's least height (a one-line label; a longer one wraps and the row grows).</summary>
    private const float StepRowHeight = 44f;

    /// <summary>A step's tick box.</summary>
    private const float StepBoxSize = 28f;

    /// <summary>The heading's chevron room at its left.</summary>
    private const float StepsChevronRoom = 32f;

    /// <summary>
    /// The checklist section at the end of the sidebar's <paramref name="list"/>:
    /// a gap, the heading row, the line shown instead of the list (hidden
    /// steps, or no traveller), the list and its inactive row. Returns its
    /// StepsPanel (the app's parts are wired later: WireStepsPanel).
    /// </summary>
    private static StepsPanel BuildStepsSection(RectTransform list)
    {
        Transform gap = Panel(list, "ChecklistGap", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        SetLayoutHeight(gap, PcSize.L);

        // The heading is the fold's button (the Sidebar role's plate, a hover tint): the chevron, "Checklist", and the count at its right end.
        Button heading = MakeButton(list, "ChecklistHeading", null, Vector2.zero, Vector2.one, XpFace, ThemeRoleId.Sidebar, "app.sidebar.steps");
        SetLayoutHeight(heading, StepsHeadingHeight);
        TMP_Text title = heading.transform.Find("Label").GetComponent<TMP_Text>();
        PlaceRect(title.transform, Vector2.zero, Vector2.one, new Vector2(PcSize.S + StepsChevronRoom, 0f), new Vector2(-PcSize.S, 0f));
        Chrome(title, PcType.Body);
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.raycastTarget = false;
        SceneUiKit.Tag(title, ThemeRoleId.Sidebar, ThemePart.Ink, "app.sidebar.steps", FontStyles.Bold, ThemeTextKind.Heading, true);
        Transform chevron = Panel(heading.transform, "Chevron", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(PcSize.S + StepsChevronRoom / 2f, 0f), new Vector2(20f, 20f), null);
        ChevronStroke(chevron, "Left", new Vector2(-4f, 2f), 45f);
        ChevronStroke(chevron, "Right", new Vector2(4f, 2f), -45f);
        TMP_Text count = Text(heading.transform, "Count", string.Empty, PcType.Caption, TextAlignmentOptions.MidlineRight, Vector2.zero, Vector2.one, Ink,
                              ThemeRoleId.Sidebar, kind: ThemeTextKind.Body);
        PlaceRect(count.transform, Vector2.zero, Vector2.one, new Vector2(PcSize.S + StepsChevronRoom, 0f), new Vector2(-PcSize.S, 0f));
        Chrome(count, PcType.Caption);
        count.raycastTarget = false;

        TMP_Text state = Text(list, "ChecklistState", UiText.Get("steps.none"), PcType.Caption, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Ink,
                              ThemeRoleId.Sidebar, null, FontStyles.Italic, ThemeTextKind.Body);
        Chrome(state, PcType.Caption, true);
        state.margin = new Vector4(PcSize.S, 0f, PcSize.S, 0f);
        state.raycastTarget = false;

        var rows = (RectTransform)Panel(list, "ChecklistList", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(rows.gameObject);
        layout.spacing = 2f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        StepRowView row = BuildStepRow(rows);

        StepsPanel steps = heading.gameObject.AddComponent<StepsPanel>(); // on the heading, always active: it follows what the player sees while the list is hidden
        var so = new SerializedObject(steps);
        Wire(so, "list", rows.gameObject);
        Wire(so, "rowsRoot", rows);
        Wire(so, "rowTemplate", row);
        Wire(so, "stateText", state);
        Wire(so, "toggleGlyph", chevron);
        Wire(so, "countText", count);
        so.ApplyModifiedProperties();
        WirePersistentVoid(heading, "m_OnClick", steps, nameof(StepsPanel.Toggle));
        return steps;
    }

    /// <summary>One stroke of the checklist heading's chevron (a "v" in the sidebar's ink; StepsPanel turns it to point right while the list is hidden).</summary>
    private static void ChevronStroke(Transform chevron, string name, Vector2 centre, float angle)
    {
        Transform bar = Panel(chevron, name, Center, Center, centre, new Vector2(2.5f, 11f), Ink);
        bar.localRotation = Quaternion.Euler(0f, 0f, angle);
        Image image = bar.GetComponent<Image>();
        image.raycastTarget = false;
        SceneUiKit.Tag(image, ThemeRoleId.Sidebar, ThemePart.Ink);
    }

    /// <summary>
    /// The inactive step row (C6): the tick box (a white box, its tick hidden)
    /// at its top left, and the label button over the rest in the sidebar's
    /// colour, its text at Body size wrapping onto more lines as it needs (the
    /// row grows with it: a vertical layout sizes both).
    /// </summary>
    private static StepRowView BuildStepRow(Transform rows)
    {
        Transform row = Panel(rows, "StepRowTemplate", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        VerticalLayoutGroup rowLayout = GetOrAdd<VerticalLayoutGroup>(row.gameObject);
        rowLayout.padding = new RectOffset((int)(PcSize.S + StepBoxSize + PcSize.S), 0, 0, 0);
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;
        GetOrAdd<LayoutElement>(row.gameObject).minHeight = StepRowHeight;

        Transform boxRect = Panel(row, "Box", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(PcSize.S + StepBoxSize / 2f, -StepRowHeight / 2f),
                                  new Vector2(StepBoxSize, StepBoxSize), Color.white, ThemeRoleId.InputField);
        GetOrAdd<LayoutElement>(boxRect.gameObject).ignoreLayout = true;
        Button box = GetOrAdd<Button>(boxRect.gameObject);
        box.targetGraphic = boxRect.GetComponent<Image>();
        Transform check = Panel(boxRect, "Check", Center, Center, Vector2.zero, new Vector2(16f, 16f), XpGreen, ThemeRoleId.Badge);
        check.GetComponent<Image>().raycastTarget = false;
        check.gameObject.SetActive(false);

        Button label = MakeButton(row, "Label", null, Vector2.zero, Vector2.one, XpFace, ThemeRoleId.Sidebar);
        VerticalLayoutGroup labelLayout = GetOrAdd<VerticalLayoutGroup>(label.gameObject);
        labelLayout.padding = new RectOffset(4, 4, 8, 8);
        labelLayout.childControlWidth = true;
        labelLayout.childControlHeight = true;
        labelLayout.childForceExpandWidth = true;
        labelLayout.childForceExpandHeight = false;
        TMP_Text text = label.transform.Find("Label").GetComponent<TMP_Text>();
        text.text = UiText.Get("steps.identity");
        Chrome(text, PcType.Body, true);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;

        StepRowView view = row.gameObject.AddComponent<StepRowView>();
        var so = new SerializedObject(view);
        Wire(so, "box", box);
        Wire(so, "check", check.gameObject);
        Wire(so, "label", label);
        Wire(so, "text", text);
        so.ApplyModifiedProperties();
        row.gameObject.SetActive(false);
        return view;
    }

    /// <summary>The steps' references (the app: what the player sees and where a jump goes; the toast; the knobs) and the steps for the app's key (Ctrl+Shift+S, ShortcutMap's ToggleSteps).</summary>
    private static void WireStepsPanel(StepsPanel steps, AppParts parts, AppToast toast, DesktopConfigSO config)
    {
        var so = new SerializedObject(steps);
        Wire(so, "app", parts.App);
        Wire(so, "toast", toast);
        Wire(so, "config", config);
        so.ApplyModifiedProperties();
        var soApp = new SerializedObject(parts.App);
        Wire(soApp, "steps", steps);
        soApp.ApplyModifiedProperties();
    }

    /// <summary>The PC's screen (whether the player looks at the desktop) for the steps, and the steps for the façade.</summary>
    private static void WireStepsToOffice(StepsPanel steps, MonitorScreen screen, InvestigationUIController invest)
    {
        var so = new SerializedObject(steps);
        Wire(so, "screen", screen);
        so.ApplyModifiedProperties();
        var soInvest = new SerializedObject(invest);
        Wire(soInvest, "stepsPanel", steps);
        soInvest.ApplyModifiedProperties();
    }

    /// <summary>Gives the Settings window the app's steps checklist (its pair shows or hides it).</summary>
    private static void WireStepsSettings(DesktopWindow settings, StepsPanel steps)
    {
        var so = new SerializedObject(settings.GetComponent<SettingsWindowController>());
        SetRef(so, "steps", steps);
        so.ApplyModifiedProperties();
    }
}
