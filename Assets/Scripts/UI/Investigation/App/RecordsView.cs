/// <summary>
/// The Investigation app's Records tab (the PC redesign AP5, §2.5): Citizen
/// Records' lookup by name or number and the record's rows, drawn today by the
/// Records component on the same root (CitizenRecordsWindowController); phase
/// 5's FormView takes its place (the Record Extract, TC-901). A day source: it
/// works between travellers.
/// </summary>
public sealed class RecordsView : AppView
{
    /// <inheritdoc />
    public override AppTab Tab => AppTab.Records;
}
