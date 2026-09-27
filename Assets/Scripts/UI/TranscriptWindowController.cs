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
/// while its evidence stays the canonical value. Every sentence is written in
/// the font TextFlip picks from the row template's own (the script's only
/// while foreign cells show). Rows are named Line_{id}; each is marked with
/// its line key for the keys, the copy and the pins (AppRow: "Nikias · line
/// 7"; an untranslated line keeps its tongue, so its copy is a foreign clip),
/// and a jump shows a line's page (ShowLine).
/// </summary>
public sealed class TranscriptWindowController : PagedRowsWindow
{
    private IReadOnlyList<DialogLine> _lines = System.Array.Empty<DialogLine>();
    private string _deskName = string.Empty;
    private string _travellerName = string.Empty;
    private CompareController _compare;
    private CaseTranslation _translation = CaseTranslation.None;

    /// <summary>The sentence text's own font and material (the row template's), read once.</summary>
    private TMP_FontAsset _ownFont;
    private Material _ownMaterial;
    private bool _ownRead;

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

    /// <summary>Shows the page line <paramref name="index"/> is on; false when there is no such line.</summary>
    public bool ShowLine(int index)
    {
        if (index < 0 || index >= _lines.Count)
            return false;
        ShowRowAt(index);
        return true;
    }

    /// <inheritdoc />
    protected override int RowCount => _lines.Count;

    /// <summary>Speaker and sentence; an answer row puts its fact into the compare bar, other rows are inert.</summary>
    protected override void FillRow(int index, GameObject row, TMP_Text[] texts, Image background, Button button)
    {
        DialogLine line = _lines[index];
        row.name = $"Line_{line.Id}";

        string speaker = line.Speaker == DialogSpeaker.Desk ? _deskName : _travellerName;
        if (texts.Length > 0 && texts[0] != null)
            texts[0].text = speaker;
        if (texts.Length > 1)
        {
            ReadOwnFont();
            TextFlip.Write(texts[1], _ownFont, _ownMaterial, line.Text, _translation.Line(line), _translation);
        }
        AppRow marked = AppRow.Mark(row, AppTab.Transcript, PickKeys.Line(index), UiText.Format("app.row.line", speaker, index + 1),
                                    texts.Length > 0 ? texts[0] : null, texts.Length > 1 ? texts[1] : null, line.IsAnswer ? button : null);
        if (_translation.Untranslated(line))
            marked.MarkUntranslated(_translation.TongueId, _translation.TongueName, line.Text);

        if (button == null)
            return;

        // A disabled button is neither clickable nor hover-highlighted.
        button.enabled = line.IsAnswer;
        if (!line.IsAnswer || _compare == null)
            return;

        ComparePick pick = EvidencePicks.ForAnswer(index, line, _translation);
        button.onClick.AddListener(() => _compare.Select(pick, new ImageHighlight(background)));
    }

    /// <summary>Reads the sentence text's own font and material from the row template, once (a row's own font, whatever a foreign line set before).</summary>
    private void ReadOwnFont()
    {
        if (_ownRead || RowTemplate == null)
            return;
        TMP_Text[] template = RowTemplate.GetComponentsInChildren<TMP_Text>(true);
        if (template.Length > 1)
        {
            _ownFont = template[1].font;
            _ownMaterial = template[1].fontSharedMaterial;
        }
        _ownRead = true;
    }
}
