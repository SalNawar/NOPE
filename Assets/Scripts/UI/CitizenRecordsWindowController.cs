using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Citizen Records desktop app: type a name, get the agency's record for
/// that person. The Name and Born rows are compare-clickable, so a record can
/// disprove a liar's birth-date tell (RecordMismatch evidence).
/// Registry is injected per day by GameManager via InvestigationUIController.
/// </summary>
public sealed class CitizenRecordsWindowController : MonoBehaviour
{
    [Header("Search")]
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Button searchButton;

    [Header("Result")]
    /// <summary>Status / instructions line ("Type a name...", "NO RECORD...").</summary>
    [SerializeField] private TMP_Text statusText;

    [SerializeField] private GameObject nameRow;
    [SerializeField] private TMP_Text nameValueText;
    [SerializeField] private GameObject bornRow;
    [SerializeField] private TMP_Text bornValueText;
    [SerializeField] private TMP_Text originText;
    [SerializeField] private TMP_Text noteText;

    [Header("Compare")]
    [SerializeField] private CompareController compareController;

    private CitizenRegistry _registry;
    private CitizenRecord _current;

    private void Awake()
    {
        if (searchButton != null)
            searchButton.onClick.AddListener(Search);

        if (searchInput != null)
            searchInput.onSubmit.AddListener(_ => Search());

        WireRow(nameRow, () => _current != null ? _current.fullName : null, ClueCategory.Name, UiText.Get("records.compare.name"));
        WireRow(bornRow, () => _current != null ? _current.birthDate : null, ClueCategory.BirthDate, UiText.Get("records.compare.born"));

        ShowIdle();
    }

    /// <summary>Sets the day's registry and resets the view.</summary>
    public void SetRegistry(CitizenRegistry registry)
    {
        _registry = registry;
        _current = null;
        ShowIdle();
    }

    /// <summary>Looks up the typed name and renders the record (or a miss).</summary>
    public void Search()
    {
        string query = searchInput != null ? searchInput.text : null;
        _current = _registry != null ? _registry.Find(query) : null;

        if (_current == null)
        {
            SetResultVisible(false);
            if (statusText != null)
                statusText.text = string.IsNullOrWhiteSpace(query)
                    ? UiText.Get("records.idle")
                    : UiText.Format("records.noRecord", query.Trim());
            return;
        }

        SetResultVisible(true);

        if (statusText != null)
            statusText.text = UiText.Get("records.onFile");
        if (nameValueText != null)
            nameValueText.text = _current.fullName;
        if (bornValueText != null)
            bornValueText.text = _current.birthDate;
        if (originText != null)
            originText.text = UiText.Format("records.origin", _current.origin);
        if (noteText != null)
            noteText.text = _current.note;
    }

    private void ShowIdle()
    {
        SetResultVisible(false);
        if (statusText != null)
            statusText.text = UiText.Get("records.idle");
        if (searchInput != null)
            searchInput.text = string.Empty;
    }

    private void SetResultVisible(bool visible)
    {
        if (nameRow != null) nameRow.SetActive(visible);
        if (bornRow != null) bornRow.SetActive(visible);
        if (originText != null) originText.gameObject.SetActive(visible);
        if (noteText != null) noteText.gameObject.SetActive(visible);
    }

    /// <summary>Makes a result row register itself with the compare system.</summary>
    private void WireRow(GameObject row, System.Func<string> currentValue, ClueCategory category, string label)
    {
        if (row == null)
            return;

        Button btn = row.GetComponent<Button>();
        Image bg = row.GetComponent<Image>();
        if (btn == null)
            return;

        btn.onClick.AddListener(() =>
        {
            string value = currentValue();
            if (string.IsNullOrEmpty(value) || compareController == null)
                return;

            compareController.Select(label, value, bg, CompareEvidence.ForRecordField(category, value));
        });
    }
}
