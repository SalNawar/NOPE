using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// The Citizen Account app: the clerk's own account (the PC spec's AC1,
/// §2.13; the traveller-types spec's D1-D3). On top the Record Extract (form
/// TC-901): the account holder's line, then the rows of Account.ExtractRows
/// under their group headings (Records, Forms on file, Travel) and the note;
/// no row is pickable, the clerk is nobody's case. Below it the Statement
/// (form TC-960): one row per day of WorldState.accountDays (DAY, WAGES,
/// FINES, DEBT RELIEF, HOUSEHOLD, PURCHASES, BALANCE, OWED; "–" where no
/// source gives a value yet), the amounts' unit, the fine print and an empty
/// stamp box. Redrawn each time the window shows. The rows are drawn with
/// today's widgets (<see cref="Draw"/>), the one place phase 5's forms engine
/// replaces (Form_RecordExtract, Form_Statement). There is no lookup field:
/// a traveller's record is the Investigation app's.
/// </summary>
public sealed class AccountWindow : MonoBehaviour
{
    /// <summary>The account holder's line under the form's title.</summary>
    [SerializeField] private TMP_Text queryText;

    /// <summary>Where the extract's headings and rows are cloned (a vertical layout).</summary>
    [SerializeField] private RectTransform extractRoot;

    /// <summary>A group heading template (inactive).</summary>
    [SerializeField] private TMP_Text groupTemplate;

    /// <summary>A row template (inactive): a box with "Label" and "Value" texts.</summary>
    [SerializeField] private RectTransform rowTemplate;

    /// <summary>The statement's column heads.</summary>
    [SerializeField] private TMP_Text statementHead;

    /// <summary>Where the statement's day rows are cloned (a vertical layout).</summary>
    [SerializeField] private RectTransform statementRoot;

    /// <summary>A statement row template (inactive).</summary>
    [SerializeField] private TMP_Text statementRowTemplate;

    /// <summary>"No entries yet" (shown while the statement has no row).</summary>
    [SerializeField] private TMP_Text statementEmpty;

    /// <summary>The amounts' unit line ("Amounts in cr.").</summary>
    [SerializeField] private TMP_Text unitText;

    /// <summary>Each column's start, in percent of the row's width.</summary>
    private static readonly int[] ColumnStarts = { 0, 8, 20, 31, 46, 60, 74, 88 };

    /// <summary>The statement's column heads' UI keys, in column order.</summary>
    private static readonly string[] ColumnKeys =
    {
        "account.col.day", "account.col.wages", "account.col.fines", "account.col.debtRelief",
        "account.col.household", "account.col.purchases", "account.col.balance", "account.col.owed"
    };

    private readonly List<GameObject> _drawn = new List<GameObject>();

    private void Awake()
    {
        if (groupTemplate != null)
            groupTemplate.gameObject.SetActive(false);
        if (rowTemplate != null)
            rowTemplate.gameObject.SetActive(false);
        if (statementRowTemplate != null)
            statementRowTemplate.gameObject.SetActive(false);
    }

    private void OnEnable() => Draw();

    /// <summary>Draws the extract and the statement from the run's account.</summary>
    private void Draw()
    {
        foreach (GameObject go in _drawn)
            if (go != null)
                Destroy(go);
        _drawn.Clear();

        WorldState world = RunManager.HasInstance ? RunManager.Instance.World : null;
        ContentLibrarySO library = RunManager.HasInstance ? RunManager.Instance.Library : null;
        var source = new ClerkAccountSource(world, library);

        if (queryText != null)
            queryText.text = UiText.Format("account.query", source.Profile.name, source.Profile.citizenId);

        string group = null;
        foreach (AccountRow row in Account.ExtractRows(source, UiText.Get, Amount))
        {
            if (row.Group != group && !string.IsNullOrEmpty(row.Group) && groupTemplate != null && extractRoot != null)
            {
                TMP_Text heading = Instantiate(groupTemplate, extractRoot);
                heading.gameObject.SetActive(true);
                heading.text = row.Group;
                _drawn.Add(heading.gameObject);
            }
            group = row.Group;

            if (rowTemplate == null || extractRoot == null)
                continue;
            RectTransform box = Instantiate(rowTemplate, extractRoot);
            box.gameObject.SetActive(true);
            SetChild(box, "Label", row.Label);
            SetChild(box, "Value", row.Value);
            _drawn.Add(box.gameObject);
        }

        if (statementHead != null)
            statementHead.text = Columns(System.Array.ConvertAll(ColumnKeys, key => UiText.Get(key)));
        List<AccountDay> days = world != null ? world.accountDays : new List<AccountDay>();
        if (statementRowTemplate != null && statementRoot != null)
            foreach (AccountDay d in days)
            {
                TMP_Text line = Instantiate(statementRowTemplate, statementRoot);
                line.gameObject.SetActive(true);
                line.text = Columns(Account.StatementCells(d, UiText.Get));
                _drawn.Add(line.gameObject);
            }
        if (statementEmpty != null)
        {
            statementEmpty.gameObject.SetActive(days.Count == 0);
            statementEmpty.transform.SetAsLastSibling();
        }
        if (unitText != null)
            unitText.text = UiText.Format("account.statement.unit", UiText.Currency(UiText.WalletForm.Short));
    }

    /// <summary>An amount of credits in the wallet's short word ("1,250 cr").</summary>
    private static string Amount(int value) =>
        value.ToString("N0", CultureInfo.InvariantCulture) + " " + UiText.Currency(UiText.WalletForm.Short);

    /// <summary>Cells placed at the statement's column starts (TMP position tags).</summary>
    private static string Columns(IReadOnlyList<string> cells)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < cells.Count && i < ColumnStarts.Length; i++)
            sb.Append("<pos=").Append(ColumnStarts[i].ToString(CultureInfo.InvariantCulture)).Append("%>").Append(cells[i]);
        return sb.ToString();
    }

    private static void SetChild(Transform parent, string name, string text)
    {
        Transform child = parent.Find(name);
        if (child != null && child.TryGetComponent(out TMP_Text t))
            t.text = text;
    }
}
