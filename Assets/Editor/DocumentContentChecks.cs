using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The document design spec's content checks (D1, D4, D8, D9), one rule for
/// Generate World and the content validator: the issuing offices
/// (Seals.Problems: every traveller form issued by one office, every
/// office's seal told apart), the published fault canon (FaultCanon.Problems
/// against the forms the agency hands out and the fields they print; a
/// forged seal's variants are SealForgery's; the photo's rows name photo
/// forms), and the canon's reach: every field a record lie's maker or a
/// place lie's tell could print on a form is a canon row, so the case
/// factory, which draws only from the canon, never loses a fault it had.
/// </summary>
public static class DocumentContentChecks
{
    /// <summary>Every problem of <paramref name="agency"/>'s offices and canon against the traveller forms <paramref name="carried"/> (each kind's templates), one message each.</summary>
    public static List<string> Problems(AgencyContent agency, IEnumerable<(TravellerKind kind, DocumentTemplateSO template)> carried)
    {
        var problems = new List<string>();
        List<(TravellerKind kind, DocumentTemplateSO template)> pairs = (carried ?? Array.Empty<(TravellerKind, DocumentTemplateSO)>()).Where(p => p.template != null).ToList();
        List<DocumentTemplateSO> forms = pairs.Select(p => p.template).Distinct().ToList();
        var fields = new Dictionary<string, IReadOnlyList<ClueCategory>>();
        foreach (DocumentTemplateSO t in forms)
            fields[t.formNumber ?? string.Empty] = (t.fieldSpecs ?? new DocumentFieldSpec[0]).Where(s => s != null).Select(s => s.category).ToList();

        problems.AddRange(Seals.Problems(agency.offices, fields.Keys));
        problems.AddRange(FaultCanon.Problems(agency.faults, fields));

        foreach (FaultVariant v in FaultCanon.Variants(agency.faults, LieKind.ForgedSeal))
            if (!Enum.TryParse(v.Name, true, out SealForgery _))
                problems.Add($"agency.faults: ForgedSeal's variant '{v.Name}' is not a seal forgery ({string.Join(", ", Enum.GetNames(typeof(SealForgery)))}).");
        foreach (FaultEntry e in agency.faults.Where(e => e != null && e.TryLie(out LieKind l) && l == LieKind.SwappedPhoto))
            if (!forms.Any(t => t.formNumber == e.form && t.showsPhoto))
                problems.Add($"agency.faults '{e.id}': {e.form} shows no photo.");

        // The canon's reach: a place lie may print a tell on the forms of the kinds it fits (LieKinds.AppliesTo): smuggling on
        // any currency or device field, a false origin and the fake displaced on any place fact or birth date; a directive's
        // dates on every Valid Until and departure.
        foreach ((TravellerKind kind, DocumentTemplateSO t) in pairs)
            foreach (DocumentFieldSpec s in t.fieldSpecs ?? new DocumentFieldSpec[0])
            {
                if (s == null)
                    continue;
                foreach (LieKind lie in new[] { LieKind.Smuggling, LieKind.FalseOrigin, LieKind.FakeDisplaced })
                {
                    bool tell = lie == LieKind.Smuggling
                        ? Lies.SmuggledCategories.Contains(s.category)
                        : s.category == ClueCategory.BirthDate || s.category == ClueCategory.Currency || s.category == ClueCategory.Language || s.category == ClueCategory.Technology;
                    if (tell && LieKinds.AppliesTo(lie, kind) && !FaultCanon.Allows(agency.faults, FaultCanon.Of(lie), t.formNumber, s.category))
                        problems.Add($"agency.faults: a {kind}'s {t.formNumber} prints {s.category}, where a {lie} tell can show, but no {lie} row names it.");
                }
                if (s.category == ClueCategory.Expiry && !FaultCanon.Allows(agency.faults, FaultCanon.Of(DirectiveFault.ExpiredPaper), t.formNumber, s.category))
                    problems.Add($"agency.faults: {t.formNumber} prints a Valid Until but no ExpiredPaper row names it.");
                if (s.category == ClueCategory.DepartureDate && !FaultCanon.Allows(agency.faults, FaultCanon.Of(DirectiveFault.WrongDepartureDate), t.formNumber, s.category))
                    problems.Add($"agency.faults: {t.formNumber} prints a departure but no WrongDepartureDate row names it.");
            }
        return problems.Distinct().ToList();
    }
}
