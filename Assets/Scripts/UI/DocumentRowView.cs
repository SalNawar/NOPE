using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Writes one document row (DocumentRows) into its label and value texts, for
/// the scanned page on the PC and the desk paper alike (piece 10 E2): the
/// label as it is; the value through piece 9's DisplayText (plain,
/// untranslated, or flipping on the document's RevealClock, staggered by the
/// row's place among the rows in the tongue). A value in the tongue in a row
/// with a horizontal layout (the scanned page) takes the row's width after its
/// label, so wide glyphs shrink into it instead of squeezing the label; a desk
/// row's boxes are fixed by PaperFace and its value auto-sizes.
/// </summary>
public static class DocumentRowView
{
    /// <summary>The value's share of the row's spare width against the label's (the row's layout expands every child by at least 1).</summary>
    private const float RestWeight = 1000f;

    /// <summary>
    /// Writes <paramref name="row"/> at <paramref name="now"/> (Time.unscaledTime)
    /// with the document's translation and reveal clock; a flip still running
    /// is added to <paramref name="running"/> (tick it with the clock's elapsed
    /// time). Null texts are skipped.
    /// </summary>
    public static void Write(TMP_Text label, TMP_Text value, DocumentRow row, CaseTranslation tr, RevealClock clock, float now, List<TextFlip> running)
    {
        tr = tr ?? CaseTranslation.None;
        DocumentField f = row.Field;
        if (label != null)
            label.text = f.label;
        if (value == null)
            return;

        if (tr.Foreign && Translation.InTongue(f.category) && value.transform.parent != null &&
            value.transform.parent.GetComponent<HorizontalLayoutGroup>() != null)
            FillRest(value);

        Reveal reveal = tr.Field(f.category, clock != null ? clock.Elapsed(now) : float.NaN, row.TongueRow);
        if (reveal.Kind == RevealKind.Flipping)
        {
            var flip = new TextFlip();
            flip.Show(value, f.value, reveal, tr);
            if (flip.Running && running != null)
                running.Add(flip);
        }
        else
        {
            TextFlip.Write(value, f.value, reveal, tr);
        }
    }

    /// <summary>The value takes whatever width the row's layout has left after its label (its glyphs' own width never counts) and may wrap onto a second line there.</summary>
    private static void FillRest(TMP_Text value)
    {
        if (!value.TryGetComponent(out LayoutElement element))
            element = value.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = 0f;
        element.flexibleWidth = RestWeight;
        value.textWrappingMode = TextWrappingModes.Normal;
    }
}

/// <summary>When a document's written reveal starts (piece 10 X18, X25): at a sighting, for a foreign traveller whose Papers translator is owned, when the document has a value in the tongue.</summary>
public static class DocumentReveal
{
    /// <summary>Starts <paramref name="clock"/> at <paramref name="now"/> when the rule holds and it has not started; true when this call started it (the caller then redraws both surfaces).</summary>
    public static bool Begin(RevealClock clock, CaseTranslation tr, IReadOnlyList<DocumentField> fields, float now)
    {
        if (clock == null || clock.Started || tr == null || !tr.Foreign || !tr.PapersTranslated || !DocumentRows.HasTongue(fields))
            return false;

        clock.Start(now);
        return true;
    }
}
