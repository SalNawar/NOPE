using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Citizen Account app: the clerk's own account (the PC spec's AC1,
/// §2.13; the traveller-types spec's D1-D3), two forms in one scroll (phase
/// 5, FO9). On top the Record Extract (Form_RecordExtract, TC-901, a portrait
/// page): the account holder's line, then the rows of Account.ExtractRows in
/// their groups (Records, Forms on file, Travel) and the row with no group as
/// the note line ("NOTE  No remarks on file."); no row is pickable, the
/// clerk is nobody's case. Below it the Statement (Form_Statement, TC-960, a
/// landscape page across the window, so its eight columns keep their type
/// sizes): one row per day of WorldState.accountDays (DAY, WAGES, FINES, DEBT
/// RELIEF, HOUSEHOLD, PURCHASES, BALANCE, OWED; "–" where no source gives a
/// value yet), "No entries yet." while there is none, the amounts' unit, the
/// fine print and the stamp area. Redrawn each time the window shows. There
/// is no lookup field: a traveller's record is the Investigation app's.
/// </summary>
public sealed class AccountWindow : MonoBehaviour
{
    /// <summary>The scroll the two pages stack in (its content holds both).</summary>
    [SerializeField] private ScrollRect scroll;

    /// <summary>The Record Extract's page.</summary>
    [SerializeField] private FormView extract;

    /// <summary>The Record Extract's page kind (Form_RecordExtract).</summary>
    [SerializeField] private FormSpecSO extractForm;

    /// <summary>The Statement's page.</summary>
    [SerializeField] private FormView statement;

    /// <summary>The Statement's page kind (Form_Statement, landscape).</summary>
    [SerializeField] private FormSpecSO statementForm;

    /// <summary>The gap between the two pages (desktop units).</summary>
    [SerializeField] private float gap = 16f;

    private void OnEnable() => Draw();

    /// <summary>Draws the extract and the statement from the run's account, the statement under the extract.</summary>
    private void Draw()
    {
        WorldState world = RunManager.HasInstance ? RunManager.Instance.World : null;
        ContentLibrarySO library = RunManager.HasInstance ? RunManager.Instance.Library : null;
        AgencyContent agency = library != null ? library.Agency : null;
        var source = new ClerkAccountSource(world, library);
        float bottom = 0f;

        if (extract != null && extractForm != null)
        {
            FormData page = extractForm.Page(agency);
            List<AccountRow> rows = Account.ExtractRows(source, UiText.Get, Amount);
            page.Text = new Dictionary<string, string>
            {
                { "query", UiText.Format("account.query", source.Profile.name, source.Profile.citizenId) },
                { "note", Note(rows) }
            };
            page.Groups = Groups(rows);
            extract.Show(extractForm.form, page, _ => false);
            bottom = Stack(extract, bottom);
        }

        if (statement != null && statementForm != null)
        {
            List<AccountDay> days = world != null ? world.accountDays : new List<AccountDay>();
            FormData page = statementForm.Page(agency);
            page.Rows = new Dictionary<string, IReadOnlyList<string[]>> { { "rows", days.ConvertAll(d => Account.StatementCells(d, UiText.Get)) } };
            page.Text = new Dictionary<string, string>
            {
                { "none", days.Count == 0 ? UiText.Get("account.statement.none") : string.Empty },
                { "unit", UiText.Format("account.statement.unit", UiText.Currency(UiText.WalletForm.Short)) }
            };
            statement.Show(statementForm.form, page, _ => false);
            bottom = Stack(statement, bottom > 0f ? bottom + gap : 0f);
        }

        if (scroll != null && scroll.content != null)
        {
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bottom);
            scroll.verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>Puts a page's top at <paramref name="top"/> in the scroll's content; returns its bottom.</summary>
    private static float Stack(FormView page, float top)
    {
        var rt = (RectTransform)page.transform;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -top);
        return top + rt.rect.height;
    }

    /// <summary>The extract's grouped rows as the form's groups, in order (a row joins the group before it while its group is the same); a row with no group is the note, not a group.</summary>
    private static List<FormGroup> Groups(IReadOnlyList<AccountRow> rows)
    {
        var groups = new List<FormGroup>();
        string title = null;
        List<(string, string)> current = null;
        foreach (AccountRow row in rows)
        {
            if (string.IsNullOrEmpty(row.Group))
                continue;
            if (current == null || row.Group != title)
            {
                current = new List<(string, string)>();
                title = row.Group;
                groups.Add(new FormGroup(title, current));
            }
            current.Add((row.Label, row.Value));
        }
        return groups;
    }

    /// <summary>The note line: each row with no group as its label in capitals and its value ("NOTE  No remarks on file.").</summary>
    private static string Note(IReadOnlyList<AccountRow> rows)
    {
        var lines = new List<string>();
        foreach (AccountRow row in rows)
            if (string.IsNullOrEmpty(row.Group))
                lines.Add((row.Label ?? string.Empty).ToUpperInvariant() + "  " + row.Value);
        return string.Join("\n", lines);
    }

    /// <summary>An amount of credits in the wallet's short word ("1,250 cr"); the Debt Relief ending's papers word theirs the same way.</summary>
    internal static string Amount(int value) =>
        value.ToString("N0", CultureInfo.InvariantCulture) + " " + UiText.Currency(UiText.WalletForm.Short);
}
