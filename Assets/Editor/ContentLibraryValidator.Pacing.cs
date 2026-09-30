using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Validate Content Library's day-pacing part (Papers Please lessons 4 and
/// D7, wave 5 track C): each day plan's papers in circulation (DayPapers:
/// known forms, none an earlier day issued left out) and what each day
/// brings for the first time, a paper or a directive, at most one of each and
/// named in its bulletin (DayPacing), in the words Generate World checks its
/// source with.
/// </summary>
public static partial class ContentLibraryValidator
{
    /// <summary>Reports every day plan's papers and pacing problems (DayPapers.Problems, DayPacing.Problems over DayPacing.NewByDay), in day order.</summary>
    private static int CheckPacing(ContentLibrarySO lib)
    {
        int issues = 0;
        List<DayPlanSO> plans = lib.DayPlans.Where(p => p != null).OrderBy(p => p.DayNumber).ToList();
        var known = new HashSet<string>(TravellerBlueprints(lib).Where(b => b != null).SelectMany(b => b.DocumentTemplates ?? new DocumentTemplateSO[0])
                                                                  .Where(t => t != null && !string.IsNullOrEmpty(t.formNumber)).Select(t => t.formNumber));
        var issuedBefore = new HashSet<string>();
        var papers = new List<IEnumerable<string>>();
        var directives = new List<IEnumerable<string>>();
        foreach (DayPlanSO plan in plans)
        {
            foreach (string problem in DayPapers.Problems(plan.name, plan.Papers, known, issuedBefore.ToList()))
            {
                Debug.LogError($"[ContentLibraryValidator] {problem} (run Tools > TimeDesk > Generate World)", plan);
                issues++;
            }
            issuedBefore.UnionWith(known.Where(plan.Issues));

            IEnumerable<CaseBlueprintSO> blueprints = plan.PossibleBlueprints.Concat(plan.ForcedBlueprints)
                .Concat((plan.AvailableLegendaries ?? new LegendarySO[0]).Concat(plan.ForcedCases.Where(f => f != null).Select(f => f.legendary))
                        .Where(l => l != null).Select(l => plan.Kinds.Where(k => k != null && k.blueprint != null && k.blueprint.Kind == l.kind).Select(k => k.blueprint).FirstOrDefault()));
            papers.Add(blueprints.Where(b => b != null).SelectMany(plan.TemplatesOf).Select(t => DayPacing.PaperKey(t.askGroup, t.formNumber)).ToList());
            directives.Add(plan.ActiveTravelRules.Where(r => r != null).Select(r => DayPacing.RuleKey(r.name, r.IsClosure, r.kinds != null && r.kinds.Length > 0)).ToList());
        }

        List<List<string>> newPapers = DayPacing.NewByDay(papers), newRules = DayPacing.NewByDay(directives);
        for (int i = 0; i < plans.Count; i++)
            foreach (string problem in DayPacing.Problems(plans[i].name, newPapers[i], newRules[i], plans[i].Bulletin))
            {
                Debug.LogError($"[ContentLibraryValidator] {problem} (run Tools > TimeDesk > Generate World)", plans[i]);
                issues++;
            }
        return issues;
    }
}
