using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/// <summary>
/// Generate World's present (the redesign's phase 6; traveller types H1, K4):
/// world_source.json "present" (the neutral present: its name, year, facts
/// and wardrobe) is checked and written into the content library as
/// PresentContent, its Culture fact derived from the wardrobe as a place's is
/// and its birth years from its year as a place's are. The 2150 citizens'
/// names (the Future places' lists together) are checked here too.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>The neutral present as authored ("present").</summary>
    [Serializable] private sealed class PresentData
    {
        /// <summary>Its name ("Temporal Customs Zone").</summary>
        public string displayName;

        /// <summary>Its year (2150).</summary>
        public int year;

        /// <summary>Its facts (Culture is derived from the wardrobe).</summary>
        public FactData[] facts;

        /// <summary>What its people wear (its signature items make the Costume Guide's row).</summary>
        public WardrobeData wardrobe;
    }

    /// <summary>
    /// Checks the present before anything is written: an ASCII name, a year,
    /// one fact for every category the places author (never Culture), each
    /// within the book width and shared with no place (the present's row must
    /// name one place); a wardrobe for both genders whose derived Culture fits
    /// the width and is shared with no place, and whose labels clash with no
    /// place's (Looks.LabelProblems); and the 2150 citizens' names
    /// (CitizenNames.Problems over the Future places' lists).
    /// </summary>
    private static void CheckPresent(WorldSource src, List<string> errors)
    {
        var futureEras = new HashSet<string>(src.eras.Where(e => e.future).Select(e => e.id));
        var names = new CitizenNames(src.places.Where(p => futureEras.Contains(p.era))
                                               .Select(p => new NameList(PlaceId(p), p.maleNames, p.femaleNames)));
        errors.AddRange(names.Problems());

        PresentData present = src.present;
        if (present == null)
        {
            errors.Add($"'{SourcePath}' has no \"present\" section (the neutral present: displayName, year, facts, wardrobe).");
            return;
        }

        if (string.IsNullOrWhiteSpace(present.displayName) || !IsAscii(present.displayName))
            errors.Add("present.displayName must be ASCII and non-blank.");
        if (present.year <= 0)
            errors.Add($"present.year is {present.year}: the present's year (2150).");

        var authoredCategories = new HashSet<string>(src.places.SelectMany(p => p.facts ?? Array.Empty<FactData>()).Select(f => f.category));
        var listed = new HashSet<string>();
        foreach (FactData f in present.facts ?? Array.Empty<FactData>())
        {
            if (!ParseEnum(f.category, out ClueCategory category))
            {
                errors.Add($"present.facts: '{f.category}' is not a category.");
                continue;
            }
            if (category == Looks.EvidenceCategory)
                errors.Add("present.facts authors a Culture fact; Culture is derived from the wardrobe's signature items.");
            if (!listed.Add(f.category))
                errors.Add($"present.facts lists {f.category} twice.");
            CheckPresentValue(src, f.category, f.value, errors);
        }
        foreach (string missing in authoredCategories.Where(c => !listed.Contains(c)))
            errors.Add($"present.facts has no {missing} fact; every book lists the present's row.");

        if (present.wardrobe == null || present.wardrobe.m == null || present.wardrobe.f == null)
        {
            errors.Add("present needs a wardrobe with \"m\" and \"f\".");
            return;
        }

        CheckGenderLook(present.wardrobe.m, "The present m", errors);
        CheckGenderLook(present.wardrobe.f, "The present f", errors);
        PlaceWardrobe wardrobe = ToWardrobe(present.wardrobe);
        string culture = Looks.CultureValue(wardrobe);
        if (culture != null)
            CheckPresentValue(src, Looks.EvidenceCategory.ToString(), culture, errors);

        var wardrobes = src.places.Where(p => p.wardrobe != null && p.wardrobe.m != null && p.wardrobe.f != null)
                                  .Select(p => (PlaceId(p), ToWardrobe(p.wardrobe)))
                                  .ToList();
        wardrobes.Add((Present.NeutralNationId, wardrobe));
        foreach (string problem in Looks.LabelProblems(wardrobes).Where(p => p.Contains(Present.NeutralNationId)))
            errors.Add($"The present's wardrobe labels: {problem}");
    }

    /// <summary>A present fact's value: non-blank, within the book width, and shared with no place's fact of its category (a place's derived Culture included).</summary>
    private static void CheckPresentValue(WorldSource src, string category, string value, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"present.facts {category} is blank.");
            return;
        }
        if (value.Length > FactTable.MaxValueLength)
            errors.Add($"The present's {category} '{value}' is {value.Length} characters; a book row holds {FactTable.MaxValueLength}.");

        foreach (PlaceData p in src.places)
        {
            IEnumerable<string> theirs = category == Looks.EvidenceCategory.ToString()
                ? new[] { p.wardrobe != null ? Looks.CultureValue(ToWardrobe(p.wardrobe)) : null }
                : (p.facts ?? Array.Empty<FactData>()).Where(f => f.category == category).Select(f => f.value);
            if (theirs.Any(v => v != null && DiscrepancyLog.ValuesMatch(v, value)))
                errors.Add($"The present's {category} '{value}' is also '{PlaceId(p)}''s; the present's row must name one place.");
        }
    }

    /// <summary>The neutral present as the library holds it: its name, year, birth years (as a place's) and facts, the Culture fact derived from the wardrobe last.</summary>
    private static PresentContent BuildPresent(WorldSource src)
    {
        PresentData p = src.present;
        var facts = (p.facts ?? Array.Empty<FactData>())
            .Select(f => new ProfileFact { category = (ClueCategory)Enum.Parse(typeof(ClueCategory), f.category), value = f.value })
            .ToList();
        facts.Add(new ProfileFact { category = Looks.EvidenceCategory, value = Looks.CultureValue(ToWardrobe(p.wardrobe)) });
        return new PresentContent
        {
            displayName = p.displayName,
            year = p.year,
            birthYearMin = p.year - src.travellerAgeMax,
            birthYearMax = p.year - src.travellerAgeMin,
            facts = facts
        };
    }

    /// <summary>Writes the neutral present into the content library.</summary>
    private static void WirePresent(ContentLibrarySO lib, WorldSource src)
    {
        var so = new SerializedObject(lib);
        so.FindProperty("present").boxedValue = BuildPresent(src);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lib);
    }
}
