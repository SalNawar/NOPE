using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The visual lies (the document design spec, D4, D8): a traveller who is who
/// they say, whose every printed value is true, but one of whose papers
/// carries a forged seal, or whose photo shows someone else. Each draws only
/// from the published canon (agency.faults; FaultCanon) on the traveller's
/// lie stream, after their look is composed, and prints record tells (the
/// traveller is a forger). Pure, so the draws and their order are tested
/// headless.
/// </summary>
public static class VisualLies
{
    /// <summary>
    /// A forged seal: the variant among the canon's ForgedSeal variants that
    /// name a SealForgery and can show (a legend needs a wrong legend on
    /// every paper's office), one Range draw when two or more can; the paper
    /// among <paramref name="forms"/> a row names that prints a Seal field
    /// and whose office (<paramref name="offices"/>) has a valid seal, one
    /// Range draw when two or more; then the forgery's own draw
    /// (Seals.Forge). Empty, with no draw, when nothing can show.
    /// </summary>
    public static List<RecordTell> PlanSeal(IReadOnlyList<FaultEntry> canon, IReadOnlyList<RecordForm> forms, IReadOnlyList<AgencyOffice> offices, IRandomSource rng)
    {
        var tells = new List<RecordTell>();
        if (rng == null)
            return tells;

        var papers = new List<(int document, AgencyOffice office)>();
        for (int d = 0; d < (forms?.Count ?? 0); d++)
        {
            AgencyOffice office = Seals.OfficeOf(offices, forms[d].FormNumber);
            if (office != null && office.TryGetSeal(out _) && Prints(forms[d], ClueCategory.Seal)
                && FaultCanon.Allows(canon, FaultCanon.Of(LieKind.ForgedSeal), forms[d].FormNumber, ClueCategory.Seal))
                papers.Add((d, office));
        }
        List<SealForgery> variants = FaultCanon.Variants(canon, LieKind.ForgedSeal)
            .Select(v => Enum.TryParse(v.Name, true, out SealForgery f) && Enum.IsDefined(typeof(SealForgery), f) ? f : (SealForgery?)null)
            .Where(f => f != null && (f != SealForgery.Legend || papers.All(p => !string.IsNullOrWhiteSpace(p.office.forgedLegend))))
            .Select(f => f.Value)
            .ToList();
        if (papers.Count == 0 || variants.Count == 0)
            return tells;

        SealForgery forgery = variants.Count == 1 ? variants[0] : variants[rng.Range(0, variants.Count)];
        (int document, AgencyOffice office) paper = papers.Count == 1 ? papers[0] : papers[rng.Range(0, papers.Count)];
        tells.Add(new RecordTell(paper.document, ClueCategory.Seal, Seals.Describe(Seals.Forge(paper.office, forgery, rng))));
        return tells;
    }

    /// <summary>
    /// Someone else's photo: a stranger's look (Looks.Stranger: two draws) on
    /// every one of <paramref name="forms"/> the canon's SwappedPhoto rows name
    /// that prints a Photo field; <paramref name="stranger"/> is the look
    /// printed. Empty, with no draw and no stranger, when no carried form can
    /// show it or <paramref name="person"/> is a premade's whole picture.
    /// </summary>
    public static List<RecordTell> PlanPhoto(IReadOnlyList<FaultEntry> canon, IReadOnlyList<RecordForm> forms, TravellerLook person, IRandomSource rng, out TravellerLook stranger)
    {
        stranger = null;
        var tells = new List<RecordTell>();
        var photos = new List<int>();
        for (int d = 0; d < (forms?.Count ?? 0); d++)
            if (Prints(forms[d], ClueCategory.Photo) && FaultCanon.Allows(canon, FaultCanon.Of(LieKind.SwappedPhoto), forms[d].FormNumber, ClueCategory.Photo))
                photos.Add(d);
        if (photos.Count == 0 || person == null || person.PremadeId != null)
            return tells;

        stranger = Looks.Stranger(person, rng);
        if (stranger == null)
            return tells;
        string who = Looks.IdentityKey(stranger);
        foreach (int d in photos)
            tells.Add(new RecordTell(d, ClueCategory.Photo, who));
        return tells;
    }

    /// <summary>True when the paper prints a field of <paramref name="category"/>.</summary>
    private static bool Prints(RecordForm form, ClueCategory category) =>
        form.Fields != null && form.Fields.Any(f => f != null && f.category == category);
}
