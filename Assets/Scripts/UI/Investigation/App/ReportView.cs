/// <summary>
/// The Investigation app's Report tab (the PC redesign AP5, §2.8, CM5): the
/// Deviation Report's text (EvidencePresenter writes it); phase 5's FormView
/// takes its place (the Deviation Report form, TC-930). A case source. A
/// logged deviation badges the tab; nothing opens it.
/// </summary>
public sealed class ReportView : AppView
{
    /// <inheritdoc />
    public override AppTab Tab => AppTab.Report;
}
