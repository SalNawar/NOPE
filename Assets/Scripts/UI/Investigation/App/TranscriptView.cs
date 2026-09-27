/// <summary>
/// The Investigation app's Transcript tab (the PC redesign AP5, §2.7): the
/// current traveller's interview, drawn today by the transcript component on
/// the same root (TranscriptWindowController: the newest page, answer rows
/// pickable); phase 5's FormView takes its place (the Interview Record,
/// TC-920). A case source: between travellers the pane shows the no-case
/// state. A new line badges the tab; nothing opens it. It has no items; a
/// jump shows a line's page (IAppItems).
/// </summary>
public sealed class TranscriptView : AppView, IAppItems
{
    private TranscriptWindowController _transcript;

    /// <inheritdoc />
    public override AppTab Tab => AppTab.Transcript;

    /// <inheritdoc />
    public string ItemKey => null;

    /// <inheritdoc />
    public string ItemTitle => null;

    /// <inheritdoc />
    public bool Reveal(string key)
    {
        if (_transcript == null)
            _transcript = GetComponent<TranscriptWindowController>();
        return _transcript != null && EntryKeys.TryLine(key, out int line) && _transcript.ShowLine(line);
    }
}
