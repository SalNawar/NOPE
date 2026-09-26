using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's Citizen Records app (redesign phase 2): a paged window
/// (PagedRowsWindow's shell: the title bar, the rows cloned from a template,
/// Prev/Next) with, over the rows, the agency's line, the lookup (a name or a
/// number, SEARCH) and the status line. Its rows are the transcript's
/// label-and-value layout (a fixed label column, the value wrapping), so a
/// record's long note fits; a row's label keeps the record label's ink
/// (DiegeticLabel). Rebuilt fresh on each run, like the transcript.
/// Part of <see cref="OfficeSceneUIBuilder"/>; Build() calls it in its order.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The Records window's size in desktop units.</summary>
    private static readonly Vector2 RecordsWindowSize = new Vector2(560f, 470f);

    /// <summary>Record lines (a group's heading or a row) per page.</summary>
    private const int RecordsRowsPerPage = 6;

    /// <summary>A record row's label ink (DiegeticLabel's built colour).</summary>
    private static readonly Color RecordLabelInk = new Color(0.35f, 0.32f, 0.25f, 1f);

    /// <summary>Builds the Records window and its desktop icon in <paramref name="iconGrid"/>; its evidence rows pick into <paramref name="compare"/>.</summary>
    private static CitizenRecordsWindowController BuildRecordsWindow(Transform windowLayer, Transform iconGrid, CompareController compare)
    {
        DestroyChildIfPresent(windowLayer, "RecordsWindow");
        Transform win = Panel(windowLayer, "RecordsWindow", Center, Center, Vector2.zero, RecordsWindowSize, Paper, ThemeRoleId.WindowBody);
        WindowShell shell = BuildWindowShell(win, "records.title", null, ThemeRoleId.WindowBody, ThemeRoleId.DiegeticRow, false);
        SetAnchors(shell.rowsRoot, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.63f));
        ApplyTranscriptRowLayout(shell.rowTemplate);
        TMP_Text rowLabel = shell.rowTemplate.transform.Find("Label").GetComponent<TMP_Text>();
        rowLabel.color = RecordLabelInk;
        Tag(rowLabel, ThemeRoleId.DiegeticLabel, ThemePart.Ink);

        TMP_Text agency = Text(win, "AgencyText", "", 15, TextAlignmentOptions.Left, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.91f), Ink,
                               ThemeRoleId.WindowBody, style: FontStyles.Bold);
        TMP_InputField input = BuildInputField(win, "SearchInput", "records.placeholder", new Vector2(0.05f, 0.72f), new Vector2(0.68f, 0.82f));
        Button search = MakeButton(win, "SearchButton", null, new Vector2(0.71f, 0.72f), new Vector2(0.95f, 0.82f), new Color(0.15f, 0.3f, 0.5f, 1f),
                                   ThemeRoleId.SearchButton, "records.search");
        TMP_Text status = Text(win, "StatusText", UiText.Get("records.idle"), 16, TextAlignmentOptions.Left, new Vector2(0.05f, 0.64f), new Vector2(0.95f, 0.71f), Ink,
                               ThemeRoleId.WindowBody);
        status.textWrappingMode = TextWrappingModes.NoWrap;
        status.overflowMode = TextOverflowModes.Ellipsis;

        CitizenRecordsWindowController records = win.gameObject.AddComponent<CitizenRecordsWindowController>();
        var so = new SerializedObject(records);
        SetRef(so, "titleText", shell.title);
        SetRef(so, "pageText", shell.page);
        SetRef(so, "prevButton", shell.prev);
        SetRef(so, "nextButton", shell.next);
        SetRef(so, "entryRowsRoot", shell.rowsRoot);
        SetRef(so, "entryRowTemplate", shell.rowTemplate);
        so.FindProperty("entriesPerPage").intValue = RecordsRowsPerPage;
        SetRef(so, "searchInput", input);
        SetRef(so, "searchButton", search);
        SetRef(so, "statusText", status);
        SetRef(so, "agencyText", agency);
        SetRef(so, "compareController", compare);
        so.ApplyModifiedProperties();

        DesktopWindow chrome = win.GetComponent<DesktopWindow>();
        win.gameObject.SetActive(false); // opened by its icon
        BuildDesktopIcon(iconGrid, "IconRecords", "icon.records", chrome, "");
        return records;
    }
}
