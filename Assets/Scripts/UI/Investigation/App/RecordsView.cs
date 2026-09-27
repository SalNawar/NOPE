/// <summary>
/// The Investigation app's Records tab (the PC redesign AP5, §2.5): Citizen
/// Records' lookup by name or number and the record's rows, drawn today by the
/// Records component on the same root (CitizenRecordsWindowController); phase
/// 5's FormView takes its place (the Record Extract, TC-901). A day source: it
/// works between travellers. Its item is the record looked up ("rec:{id}"; a
/// lookup tells the pane, as a chosen chip does); a jump looks a record up and
/// shows a row's page (IAppItems).
/// </summary>
public sealed class RecordsView : AppView, IAppItems
{
    private CitizenRecordsWindowController _records;

    /// <summary>The Records component on the same root.</summary>
    private CitizenRecordsWindowController Records => _records != null ? _records : _records = GetComponent<CitizenRecordsWindowController>();

    /// <inheritdoc />
    public override AppTab Tab => AppTab.Records;

    /// <inheritdoc />
    public string ItemKey => Records != null && Records.Current != null ? EntryKeys.RecordCard(Records.Current.Id) : null;

    /// <inheritdoc />
    public string ItemTitle => Records != null && Records.Current != null ? UiText.Format("app.item.record", Records.Current.FullName) : null;

    private void OnEnable()
    {
        if (Records != null)
            Records.Looked += RaiseChipsChanged;
    }

    private void OnDisable()
    {
        if (Records != null)
            Records.Looked -= RaiseChipsChanged;
    }

    /// <inheritdoc />
    public bool Reveal(string key)
    {
        if (Records == null)
            return false;
        if (EntryKeys.TryRecordCard(key, out string id))
            Records.Show(id, null);
        else if (EntryKeys.TryRecordRow(key, out id, out _))
            Records.Show(id, key);
        else
            return false;
        return Records.Current != null;
    }
}
