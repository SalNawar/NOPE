/// <summary>
/// The Investigation app's Records tab (the PC redesign AP5, §2.5): Citizen
/// Records' lookup by name or number and the record's rows, drawn today by the
/// Records component on the same root (CitizenRecordsWindowController); phase
/// 16's second part moves it onto FormView (the Record Extract, TC-901). A day
/// source: it works between travellers. Its place in the pane's history is its
/// lookup (Spot); a link runs a lookup and marks the found record's row of its
/// category (Reveal), and a lookup the player runs is a move the pane records.
/// Each pane has one.
/// </summary>
public sealed class RecordsView : AppView
{
    /// <summary>The Records component on the same root.</summary>
    [UnityEngine.SerializeField] private CitizenRecordsWindowController records;

    /// <inheritdoc />
    public override AppTab Tab => AppTab.Records;

    /// <inheritdoc />
    public override LinkTarget Spot => LinkTarget.ToRecords(records != null ? records.Query : null);

    /// <summary>The player's lookups are moves the pane records.</summary>
    private void Awake()
    {
        if (records != null)
            records.Searched += RaiseMoved;
    }

    private void OnDestroy()
    {
        if (records != null)
            records.Searched -= RaiseMoved;
    }

    /// <inheritdoc />
    public override void Reveal(LinkTarget target)
    {
        if (records != null)
            records.Reveal(target.Query, target.RecordRow);
    }
}
