using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generate World's day-pacing part (Papers Please lessons 4 and D7, wave 5
/// track C): each day's papers in circulation (days[].papers, DayPapers: known
/// form numbers, none withdrawn later) and what each day brings for the first
/// time, a paper or a directive, at most one of each and named in the day's
/// bulletin (days[].bulletin, DayPacing), checked before anything is written.
/// The validator runs the same Domain rules over the day plans.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>The form numbers of <paramref name="blueprint"/>'s templates issued on day <paramref name="d"/> (DayPapers.Carried over days[].papers).</summary>
    private static List<string> DayForms(DayData d, CaseBlueprintSO blueprint) => DayPapers.Carried(FormNumbers(blueprint), d?.papers);

    /// <summary>
    /// Checks every day's papers (DayPapers.Problems: known forms, listed
    /// once, none an earlier day issued left out) and pacing
    /// (DayPacing.Problems over DayPacing.NewByDay: the papers the day's
    /// travellers carry, its kinds', pooled premades' and forced entries',
    /// and its directives, each against every earlier day).
    /// </summary>
    private static void CheckPacing(WorldSource src, Authored authored, List<string> errors)
    {
        var known = new HashSet<string>(DocumentTemplates(authored).Select(t => t.formNumber).Where(f => !string.IsNullOrEmpty(f)));
        var rules = (src.rules ?? Array.Empty<RuleData>()).Where(r => r != null && r.asset != null).GroupBy(r => r.asset).ToDictionary(g => g.Key, g => g.First());
        var premades = (src.premades ?? Array.Empty<PremadeData>()).Where(m => m != null && m.id != null).GroupBy(m => m.id).ToDictionary(g => g.Key, g => g.First());
        DayData[] days = (src.days ?? Array.Empty<DayData>()).Where(d => d != null).OrderBy(d => d.day).ToArray();

        var issuedBefore = new HashSet<string>();
        var papers = new List<IEnumerable<string>>();
        var directives = new List<IEnumerable<string>>();
        foreach (DayData d in days)
        {
            errors.AddRange(DayPapers.Problems(d.asset, d.papers ?? Array.Empty<string>(), known, issuedBefore.ToList()));
            issuedBefore.UnionWith(known.Where(f => DayPapers.Issued(d.papers, f)));

            var blueprints = new List<CaseBlueprintSO>();
            foreach (KindWeightData k in d.kinds ?? Array.Empty<KindWeightData>())
                if (k != null && k.weight > 0f && ParseEnum(k.kind, out TravellerKind kind) && authored.blueprints.TryGetValue(kind, out CaseBlueprintSO b))
                    blueprints.Add(b);
            foreach (string id in (d.premades ?? Array.Empty<string>()).Concat((d.forced ?? Array.Empty<ForcedData>()).Select(f => f?.premade)))
                if (!string.IsNullOrEmpty(id) && premades.TryGetValue(id, out PremadeData m) && authored.blueprints.TryGetValue(PremadeKind(m), out CaseBlueprintSO b))
                    blueprints.Add(b);
            foreach (ForcedData f in d.forced ?? Array.Empty<ForcedData>())
                if (f != null && !string.IsNullOrEmpty(f.blueprint) && authored.forcedBlueprints.TryGetValue(f.blueprint, out CaseBlueprintSO b))
                    blueprints.Add(b);
            papers.Add(blueprints.Where(b => b != null).SelectMany(b => b.DocumentTemplates ?? Array.Empty<DocumentTemplateSO>())
                                 .Where(t => t != null && DayPapers.Issued(d.papers, t.formNumber))
                                 .Select(t => DayPacing.PaperKey(t.askGroup, t.formNumber)).ToList());

            directives.Add((d.rules ?? Array.Empty<string>())
                .Where(name => name != null && rules.ContainsKey(name) && ParseEnum(rules[name].type, out TravelRuleType _))
                .Select(name => DayPacing.RuleKey(name, Directives.IsClosure((TravelRuleType)Enum.Parse(typeof(TravelRuleType), rules[name].type)), (rules[name].kinds ?? Array.Empty<string>()).Length > 0))
                .ToList());
        }

        List<List<string>> newPapers = DayPacing.NewByDay(papers), newRules = DayPacing.NewByDay(directives);
        for (int i = 0; i < days.Length; i++)
        {
            errors.AddRange(DayPacing.Problems(days[i].asset, newPapers[i], newRules[i], days[i].bulletin));
            if (!string.IsNullOrEmpty(days[i].bulletin) && !IsAscii(days[i].bulletin))
                errors.Add($"Day '{days[i].asset}' has a non-ASCII \"bulletin\" (the briefing's fonts print ASCII).");
        }
    }
}
