using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Validate Content Library's day-pacing part (Papers Please lessons 4 and
/// D7, wave 5 track C): each day plan's papers in circulation (DayPapers:
/// known forms, none an earlier day issued left out) and what each day
/// brings for the first time, a paper or a directive, at most one of each and
/// named in its bulletin (DayPacing), and each day's desk hours (ShiftHours;
/// night shifts), in the words Generate World checks its source with.
/// </summary>
public static partial class ContentLibraryValidator
{
    /// <summary>Reports the guide's problems over the library's ramp (GuideContent.Problems: a page whose feature no day introduces, a day after the first with no page, an unknown target, a step that waits for nothing; GuideContent.BulletinProblems: a day's bulletin that does not name its new pages), in the words Generate World checks its source with.</summary>
    private static int CheckGuide(ContentLibrarySO lib)
    {
        int lastDay = lib.DayPlans.Where(p => p != null).Select(p => p.DayNumber).DefaultIfEmpty(0).Max();
        List<string> problems = lib.Guide.Content.Problems(lib.Introductions, lastDay);
        problems.AddRange(lib.Guide.Content.BulletinProblems(lib.Introductions, lib.DayPlans.Where(p => p != null).Select(p => (p.DayNumber, p.Bulletin))));
        foreach (string problem in problems)
            Debug.LogError($"[ContentLibraryValidator] {problem} (world_source.json guide; run Tools > TimeDesk > Generate World)", lib);
        return problems.Count;
    }

    /// <summary>Reports every day plan's introductions, papers and pacing problems (Introductions.Problems, DayPapers.Problems, DayPacing.Problems over DayPacing.NewByDay), in day order.</summary>
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
            foreach (string problem in Introductions.Problems(plan.name, plan.Introduces))
            {
                Debug.LogError($"[ContentLibraryValidator] {problem} (run Tools > TimeDesk > Generate World)", plan);
                issues++;
            }
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
            directives.Add(plan.ActiveTravelRules.Where(r => r != null).Select(r => DayPacing.RuleKey(r.name, r.IsClosure, r.kinds != null && r.kinds.Length > 0, r.type == TravelRuleType.OpenDestinations)).ToList());
        }

        List<List<string>> newPapers = DayPacing.NewByDay(papers), newRules = DayPacing.NewByDay(directives);
        for (int i = 0; i < plans.Count; i++)
            foreach (string problem in DayPacing.Problems(plans[i].name, newPapers[i], newRules[i], plans[i].Bulletin, i == 0))
            {
                Debug.LogError($"[ContentLibraryValidator] {problem} (run Tools > TimeDesk > Generate World)", plans[i]);
                issues++;
            }

        // Each day's desk hours (night shifts), against the run's shift length knobs.
        GameConfigSO config = WorldContentGenerator.RunGameConfig();
        foreach (DayPlanSO plan in plans)
            foreach (string problem in ShiftHours.Problems($"Day '{plan.name}'", plan.ShiftStartMinute, plan.ShiftEndMinute, config.shiftMinHours, config.shiftMaxHours))
            {
                Debug.LogError($"[ContentLibraryValidator] {problem} (world_source.json days[].shiftStart / shiftEnd; run Tools > TimeDesk > Generate World)", plan);
                issues++;
            }
        return issues;
    }
}
