/// <summary>
/// The Investigation app's Transcript tab (the PC redesign AP5, §2.7): the
/// current traveller's interview, drawn today by the transcript component on
/// the same root (TranscriptWindowController: the newest page, answer rows
/// pickable and linked); phase 16's second part moves it onto FormView (the
/// Interview Record, TC-920). A case source: between travellers the pane shows
/// the no-case state. A new line badges the tab; nothing opens it. A dock side
/// reveals its line (the page turned, the line marked). Each pane has one.
/// </summary>
public sealed class TranscriptView : AppView
{
    /// <summary>The transcript component on the same root.</summary>
    [UnityEngine.SerializeField] private TranscriptWindowController transcript;

    /// <inheritdoc />
    public override AppTab Tab => AppTab.Transcript;

    /// <inheritdoc />
    public override void Reveal(LinkTarget target)
    {
        if (transcript != null)
            transcript.Reveal(target.Key);
    }
}
