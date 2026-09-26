using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Case Notes: Interview — the current traveller's transcript: one row per
/// line (speaker, sentence), paged like the reference books and always
/// showing the newest page. Only answer rows are clickable: a click puts the
/// answer's canonical fact value into the compare bar as the traveller's
/// statement (EvidencePicks.ForAnswer, keyed by the line's index, the same
/// pick as the bubble's answer). Sentences are shown through DisplayText: from translation's
/// first day the traveller's lines are in their claimed place's tongue and
/// show in English only with the region's Speech translator (settled, never
/// animated); an untranslated answer shows in the bar as the placeholder,
/// while its evidence stays the canonical value. Rows are named Line_{id}.
/// </summary>
public sealed class TranscriptWindowController : PagedRowsWindow
{
    private IReadOnlyList<DialogLine> _lines = System.Array.Empty<DialogLine>();
    private string _deskName = string.Empty;
    private string _travellerName = string.Empty;
    private CompareController _compare;
    private CaseTranslation _translation = CaseTranslation.None;

    /// <summary>Shows a traveller's transcript (the runner's live, append-only list) on its newest page, in their translation.</summary>
    public void Bind(IReadOnlyList<DialogLine> transcript, string deskName, string travellerName, CompareController compare, CaseTranslation translation)
    {
        _lines = transcript ?? System.Array.Empty<DialogLine>();
        _deskName = deskName ?? string.Empty;
        _travellerName = travellerName ?? string.Empty;
        _compare = compare;
        _translation = translation ?? CaseTranslation.None;
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
        if (texts.Length > 1)
            TextFlip.Write(texts[1], line.Text, _translation.Line(line.Speaker), _translation);

        if (button == null)
            return;

        // A disabled button is neither clickable nor hover-highlighted.
        button.enabled = line.IsAnswer;
        if (!line.IsAnswer || _compare == null)
            return;

        ComparePick pick = EvidencePicks.ForAnswer(index, line, _translation);
        button.onClick.AddListener(() => _compare.Select(pick, new ImageHighlight(background)));
    }
}
