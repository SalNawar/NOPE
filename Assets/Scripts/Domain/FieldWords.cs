using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Which of the content's words each field of a document prints, for the
/// validator's word check (ArtLayout.CutWords: no name or origin may be cut
/// with a full stop on an art document; DOC-LOOK review 7, the re-verify of
/// ce69276 point 2). A Name field and a Signature field print a traveller's
/// given name (a holder signs with it: CaseFactory's Signature value), drawn
/// from every place's name lists or a premade's own name (CaseFactory's two
/// sources; a returning traveller keeps the name drawn from them); a
/// Destination field prints a place's origin label. Every other field prints
/// none of these lists: FormLayout.Check's probe stands in for its words
/// (docs/pending/DOC-LOOK.md, "The words"). Pure: the validator
/// (ContentLibraryValidator.Forms) hands it the content library's lists.
/// </summary>
public static class FieldWords
{
    /// <summary>
    /// The words each of <paramref name="fields"/> prints, in their order
    /// (null for a field, or a null field, that prints none of the content's
    /// lists): a Name or a Signature field's are <paramref name="longest"/> of
    /// <paramref name="givenNames"/> and <paramref name="premadeNames"/>
    /// together, a Destination field's <paramref name="longest"/> of
    /// <paramref name="origins"/>. Each list is made once and shared by the
    /// fields that print it.
    /// </summary>
    public static List<IReadOnlyList<string>> Of(IEnumerable<DocumentFieldSpec> fields, IEnumerable<string> givenNames, IEnumerable<string> premadeNames,
                                                  IEnumerable<string> origins, Func<IEnumerable<string>, IReadOnlyList<string>> longest)
    {
        IReadOnlyList<string> names = null, places = null;
        var words = new List<IReadOnlyList<string>>();
        foreach (DocumentFieldSpec field in fields ?? Enumerable.Empty<DocumentFieldSpec>())
        {
            ClueCategory? category = field?.category;
            if (category == ClueCategory.Name || category == ClueCategory.Signature)
                words.Add(names ??= longest((givenNames ?? Enumerable.Empty<string>()).Concat(premadeNames ?? Enumerable.Empty<string>())));
            else if (category == ClueCategory.Destination)
                words.Add(places ??= longest(origins ?? Enumerable.Empty<string>()));
            else
                words.Add(null);
        }
        return words;
    }
}
