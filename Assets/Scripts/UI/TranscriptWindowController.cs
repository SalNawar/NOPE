using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Case Notes: Interview — the current traveller's transcript: one row per
/// line (speaker, sentence), paged like the reference books and always
/// showing the newest page. Only answer rows are clickable: a click puts the
/// answer's canonical fact value into the compare bar as the traveller's
/// statement. Sentences are shown through DisplayText (the spoken reveal
/// point); the compared value stays canonical. Rows are named Line_{id}.
/// </summary>
public sealed class TranscriptWindowController : PagedRowsWindow
{
    private IReadOnlyList<DialogLine> _lines = System.Array.Empty<DialogLine>();
    private string _deskName = string.Empty;
    private string _travellerName = string.Empty;
    private CompareController _compare;

    /// <summary>Shows a traveller's transcript (the runner's live, append-only list) on its newest page.</summary>
    public void Bind(IReadOnlyList<DialogLine> transcript, string deskName, string travellerName, CompareController compare)
    {
        _lines = transcript ?? System.Array.Empty<DialogLine>();
        _deskName = deskName ?? string.Empty;
        _travellerName = travellerName ?? string.Empty;
        _compare = compare;
        ShowLastPage();
    }

    /// <summary>Shows the newest page (call after lines were appended).</summary>
    public void Refresh() => ShowLastPage();

    /// <inheritdoc />
    protected override int RowCount => _lines.Count;

    /// <summary>Speaker and sentence; an answer row puts its fact into the compare bar, other rows are inert.</summary>
    protected override void FillRow(int index, GameObject row, TMP_Text[] texts, Image background, Button button)
    {
        DialogLine line = _lines[index];
        row.name = $"Line_{line.Id}";

        if (texts.Length > 0 && texts[0] != null)
            texts[0].text = line.Speaker == DialogSpeaker.Desk ? _deskName : _travellerName;
        if (texts.Length > 1 && texts[1] != null)
            texts[1].text = DisplayText.For(line.Text, TextMedium.Spoken);

        if (button == null)
            return;

        // A disabled button is neither clickable nor hover-highlighted.
        button.enabled = line.IsAnswer;
        if (!line.IsAnswer || _compare == null)
            return;

        string label = $"Traveller · {ClueLabels.Report(line.Category)}";
        string value = line.Value;
        CompareEvidence evidence = CompareEvidence.ForAnswer(line.Category, line.Value, line.IsTell);
        button.onClick.AddListener(() => _compare.Select(label, value, background, evidence));
    }
}
