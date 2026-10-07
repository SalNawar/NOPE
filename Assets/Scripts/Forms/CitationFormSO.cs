using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// The Citation's form (TC-900, Saleh 2026-10-07: "it's a citation; think of
/// a traffic violation"): Saleh's red carbon ticket art drawn on its face
/// (Forms/paper_tc900) with the values printed at its places (FormSpec.look's
/// FormArt, one place per CitationTickets value: the date, the desk, the
/// clerk, the total, the three ticks, the stub, then each of the four rows'
/// number, violation, rule, reference and penalty). DeskController prints a
/// sheet of it per CitationTickets.Sheets sheet and flies it onto the desk.
/// </summary>
[CreateAssetMenu(fileName = "CitationForm_", menuName = "TimeDesk/Citation Form", order = 13)]
public sealed class CitationFormSO : ScriptableObject
{
    /// <summary>The printed form number (the face is found by it: ArtSlots.PaperFaces).</summary>
    public string formNumber = "TC-900";

    /// <summary>The form's name.</summary>
    public string title = "Citation";

    /// <summary>The form: its look's art places every value (no blocks).</summary>
    public FormSpec form = new FormSpec();

    /// <summary>One sheet's form showing <paramref name="values"/> (CitationTickets.Sheets' order; its labels the values' names).</summary>
    public DocumentForm Sheet(string[] values) => new DocumentForm(form, new FormData
    {
        FormNumber = formNumber,
        Title = title,
        FieldLabels = Labels(),
        FieldValues = values ?? Array.Empty<string>()
    });

    /// <summary>The values' names in CitationTickets order ("Date", ..., "Row 1 Violation", ...).</summary>
    private static string[] Labels() =>
        Enum.GetNames(typeof(CitationTickets.Field))
            .Concat(Enumerable.Range(1, CitationTickets.RowsPerSheet).SelectMany(r => Enum.GetNames(typeof(CitationTickets.RowField)).Select(f => $"Row {r} {f}")))
            .ToArray();
}
