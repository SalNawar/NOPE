/// <summary>
/// The Investigation app's Transcript tab (the PC redesign AP5, §2.7): the
/// current traveller's interview, drawn today by the transcript component on
/// the same root (TranscriptWindowController: the newest page, answer rows
/// pickable); phase 5's FormView takes its place (the Interview Record,
/// TC-920). A case source: between travellers the pane shows the no-case
/// state. A new line badges the tab; nothing opens it.
/// </summary>
public sealed class TranscriptView : AppView
{
    /// <inheritdoc />
    public override AppTab Tab => AppTab.Transcript;
}
