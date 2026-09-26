using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's steps checklist (redesign phase 21; the PC spec's
/// ST1-ST4, SG1, §2.2, §2.15): the Investigation app's sidebar section
/// (StepsPanel: the "STEPS" heading, the line shown instead of the list, and
/// a scrolling list with an inactive row, StepRowView: a tick box over its
/// tick and the label that jumps), the toolbar's Steps toggle wired to
/// StepsPanel.Toggle, the panel's references into the app, the façade and the
/// PC's screen, and Settings' Investigation section (Steps shown / Steps
/// hidden). The section is part of the app's window, which the App partial
/// rebuilds fresh on each run; the Settings rows keep their objects and have
/// their anchors re-applied (the Apps partial's policy). Part of
/// <see cref="OfficeSceneUIBuilder"/>.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The share of the sidebar's height the Steps section takes (at its top; Pinned and Recent share the rest).</summary>
    private const float StepsSectionShare = 0.62f;

    /// <summary>The section's heading band (units, at its top).</summary>
    private const float StepsHeadingHeight = 44f;

    /// <summary>A step row's height (two lines of its label).</summary>
    private const float StepRowHeight = 46f;

    /// <summary>
    /// The Steps section in the top <paramref name="top"/>..<paramref name="bottom"/>
    /// of <paramref name="side"/>: the heading, the line shown instead of the
    /// list (hidden steps, or no traveller), the list and its inactive row.
    /// Returns its StepsPanel (the app's parts are wired later: WireStepsPanel).
    /// </summary>
    private static StepsPanel BuildStepsSection(Transform side, float top, float bottom)
    {
        Transform section = Panel(side, "StepsSection", new Vector2(0f, bottom), new Vector2(1f, top), Vector2.zero, Vector2.zero, null);

        TMP_Text heading = Text(section, "StepsHeading", null, 18, TextAlignmentOptions.BottomLeft, new Vector2(0.06f, 1f), new Vector2(0.94f, 1f), Ink,
                                ThemeRoleId.Sidebar, "app.sidebar.steps", FontStyles.Bold, ThemeTextKind.Heading, true);
        PlaceRect(heading.transform, new Vector2(0.06f, 1f), new Vector2(0.94f, 1f), new Vector2(0f, -StepsHeadingHeight + 4f), new Vector2(0f, -6f));

        TMP_Text state = Text(section, "StepsState", null, 16, TextAlignmentOptions.TopLeft, new Vector2(0.06f, 1f), new Vector2(0.94f, 1f), Ink,
                              ThemeRoleId.Sidebar, null, FontStyles.Italic, ThemeTextKind.Body);
        PlaceRect(state.transform, new Vector2(0.06f, 1f), new Vector2(0.94f, 1f), new Vector2(0f, -StepsHeadingHeight - 72f), new Vector2(0f, -StepsHeadingHeight - 4f));
        state.text = UiText.Get("steps.none");
        state.textWrappingMode = TextWrappingModes.Normal;

        RectTransform rows = BuildScrollList(section, "StepsList", Vector2.zero, Vector2.one, 4f);
        Transform list = rows.parent.parent;
        PlaceRect(list, new Vector2(0.03f, 0f), new Vector2(0.97f, 1f), new Vector2(0f, 6f), new Vector2(0f, -StepsHeadingHeight));
        StepRowView row = BuildStepRow(rows);

        StepsPanel steps = section.gameObject.AddComponent<StepsPanel>();
        var so = new SerializedObject(steps);
        Wire(so, "list", list.gameObject);
        Wire(so, "rowsRoot", rows);
        Wire(so, "rowTemplate", row);
        Wire(so, "stateText", state);
        so.ApplyModifiedProperties();
        list.gameObject.SetActive(false);
        return steps;
    }

    /// <summary>
    /// The inactive step row, a plate in the button colour (like the Reference
    /// tab's "Claimed place only"): the tick box (a white box, its green tick
    /// hidden) at its left, the label button over the rest in the plate's
    /// colour, its text wrapping onto two lines.
    /// </summary>
    private static StepRowView BuildStepRow(Transform rows)
    {
        Transform row = Panel(rows, "StepRowTemplate", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, XpFace, ThemeRoleId.Button);
        row.GetComponent<Image>().raycastTarget = false;
        SetLayoutHeight(row, StepRowHeight);

        Transform boxRect = Panel(row, "Box", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(17f, 0f), new Vector2(24f, 24f), Color.white, ThemeRoleId.InputField);
        Button box = GetOrAdd<Button>(boxRect.gameObject);
        box.targetGraphic = boxRect.GetComponent<Image>();
        Transform check = Panel(boxRect, "Check", Center, Center, Vector2.zero, new Vector2(14f, 14f), XpGreen, ThemeRoleId.Badge);
        check.GetComponent<Image>().raycastTarget = false;
        check.gameObject.SetActive(false);

        Button label = MakeButton(row, "Label", null, Vector2.zero, Vector2.one, null, ThemeRoleId.Button);
        PlaceRect(label.transform, Vector2.zero, Vector2.one, new Vector2(33f, 0f), Vector2.zero);
        TMP_Text text = label.transform.Find("Label").GetComponent<TMP_Text>();
        text.text = UiText.Get("steps.identity");
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.enableAutoSizing = true;
        text.fontSizeMin = 13f;
        text.fontSizeMax = 16f;
        text.margin = new Vector4(4f, 2f, 4f, 2f);

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

    /// <summary>The steps' references into the app (its tabs, the toast, the knobs) and the toolbar's Steps toggle calling StepsPanel.Toggle.</summary>
    private static void WireStepsPanel(StepsPanel steps, AppParts parts, Button toggle, AppToast toast, DesktopConfigSO config)
    {
        var so = new SerializedObject(steps);
        Wire(so, "app", parts.App);
        Wire(so, "documents", parts.Documents);
        Wire(so, "records", parts.Records);
        Wire(so, "reference", parts.Reference);
        Wire(so, "toast", toast);
        Wire(so, "config", config);
        so.ApplyModifiedProperties();
        WirePersistentVoid(toggle, "m_OnClick", steps, nameof(StepsPanel.Toggle));
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

    /// <summary>
    /// Settings' Investigation section (SG1, §2.15) under Desktop: its heading
    /// and the Steps shown / Steps hidden pair, wired into the window's
    /// controller (its checklist: WireStepsSettings). Existing objects are
    /// kept; the anchors are re-applied.
    /// </summary>
    private static void BuildSettingsInvestigation(Transform win)
    {
        TMP_Text heading = Text(win, "InvestigationLabel", null, 20, TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.345f), new Vector2(0.95f, 0.4f), Ink,
                                ThemeRoleId.WindowBody, "settings.investigation");
        Heading(heading, "settings.investigation", new Vector2(0.05f, 0.345f), new Vector2(0.95f, 0.4f));
        Button shown = MakeButton(win, "StepsShownButton", null, new Vector2(0.05f, 0.265f), new Vector2(0.48f, 0.335f), null, ThemeRoleId.Button, "settings.stepsShown");
        SetAnchors(shown.transform, new Vector2(0.05f, 0.265f), new Vector2(0.48f, 0.335f));
        Button hidden = MakeButton(win, "StepsHiddenButton", null, new Vector2(0.52f, 0.265f), new Vector2(0.95f, 0.335f), null, ThemeRoleId.Button, "settings.stepsHidden");
        SetAnchors(hidden.transform, new Vector2(0.52f, 0.265f), new Vector2(0.95f, 0.335f));

        var so = new SerializedObject(GetOrAdd<SettingsWindowController>(win.gameObject));
        SetRef(so, "stepsShownButton", shown);
        SetRef(so, "stepsHiddenButton", hidden);
        so.ApplyModifiedProperties();
    }

    /// <summary>Gives the Settings window the app's steps checklist (its pair shows or hides it).</summary>
    private static void WireStepsSettings(DesktopWindow settings, StepsPanel steps)
    {
        var so = new SerializedObject(settings.GetComponent<SettingsWindowController>());
        SetRef(so, "steps", steps);
        so.ApplyModifiedProperties();
    }
}
