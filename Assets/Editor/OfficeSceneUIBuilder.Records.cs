using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's Citizen Records view (redesign phases 2 and 16): the
/// Investigation app's Records tab, a paged list (PagedRowsWindow's rows cloned
/// from a template, Prev/Next) with, over the rows, the agency's line, the
/// lookup (a name or a number, SEARCH) and the status line. Its rows are the
/// transcript's label-and-value layout (a fixed label column, the value
/// wrapping), so a record's long note fits; a row's label keeps the record
/// label's ink (DiegeticLabel); a row lights by its key and carries the found
/// mark (phase 18). Built with each pane of the app (fresh on each run).
/// Part of <see cref="OfficeSceneUIBuilder"/>; BuildInvestigationApp calls it.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>A record row's label ink (DiegeticLabel's built colour).</summary>
    private static readonly Color RecordLabelInk = new Color(0.35f, 0.32f, 0.25f, 1f);

    /// <summary>Builds the Records tab's view in the pane's <paramref name="content"/>; its evidence rows pick into <paramref name="compare"/>.</summary>
    private static CitizenRecordsWindowController BuildRecordsView(Transform content, CompareController compare, out AppView view)
    {
        Transform root = ViewRoot(content, "RecordsView", Paper, ThemeRoleId.WindowBody);
        TMP_Text agency = Text(root, "AgencyText", "", 15, TextAlignmentOptions.Left, new Vector2(0.03f, 0.915f), new Vector2(0.97f, 0.985f), Ink,
                               ThemeRoleId.WindowBody, style: FontStyles.Bold);
        TMP_InputField input = BuildInputField(root, "SearchInput", "records.placeholder", new Vector2(0.03f, 0.81f), new Vector2(0.7f, 0.9f));
        Button search = MakeButton(root, "SearchButton", null, new Vector2(0.72f, 0.81f), new Vector2(0.97f, 0.9f), new Color(0.15f, 0.3f, 0.5f, 1f),
                                   ThemeRoleId.SearchButton, "records.search");
        TMP_Text status = Text(root, "StatusText", UiText.Get("records.idle"), 16, TextAlignmentOptions.Left, new Vector2(0.03f, 0.73f), new Vector2(0.97f, 0.8f), Ink,
                               ThemeRoleId.WindowBody);
        status.textWrappingMode = TextWrappingModes.NoWrap;
        status.overflowMode = TextOverflowModes.Ellipsis;

        PagedBody paged = BuildPagedBody(root, new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.72f), ThemeRoleId.WindowBody, ThemeRoleId.DiegeticRow, false);
        ApplyTranscriptRowLayout(paged.rowTemplate);
        TMP_Text rowLabel = paged.rowTemplate.transform.Find("Label").GetComponent<TMP_Text>();
        rowLabel.color = RecordLabelInk;
        Tag(rowLabel, ThemeRoleId.DiegeticLabel, ThemePart.Ink);
        DecorateAppRow(paged.rowTemplate, false);

        CitizenRecordsWindowController records = root.gameObject.AddComponent<CitizenRecordsWindowController>();
        var so = new SerializedObject(records);
        WirePaging(so, paged, AppRecordRowsPerPage);
        Wire(so, "searchInput", input);
        Wire(so, "searchButton", search);
        Wire(so, "statusText", status);
        Wire(so, "agencyText", agency);
        Wire(so, "compareController", compare);
        so.ApplyModifiedProperties();

        RecordsView recordsView = root.gameObject.AddComponent<RecordsView>();
        var soView = new SerializedObject(recordsView);
        Wire(soView, "records", records);
        soView.ApplyModifiedProperties();
        view = recordsView;
        return records;
    }
}
