using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Validate Content Library's traveller-kinds part (the redesign's phases 3
/// and 6): the agency forms each blueprint lists, the claim line of every
/// kind a blueprint makes, each day's traveller mix, the present and the
/// 2150 citizens' names.
/// </summary>
public static partial class ContentLibraryValidator
{
    /// <summary>An agency form number: "TC-" and three digits (traveller types F1).</summary>
    private static readonly Regex FormNumber = new Regex(@"^TC-\d{3}$");

    /// <summary>
    /// Reports, over every blueprint a traveller can come from: a form whose
    /// number is not "TC-nnn" or is shared with another form; and a kind with
    /// no claim line, or a
    /// broken claim line (Interview.ClaimProblems, the rule Generate World
    /// also checks). Returns the issue count.
    /// </summary>
    private static int CheckKinds(ContentLibrarySO lib)
    {
        int issues = 0;
        void Error(string message, Object context)
        {
            Debug.LogError($"[ContentLibraryValidator] {message} ('{lib.name}')", context);
            issues++;
        }

        List<CaseBlueprintSO> blueprints = TravellerBlueprints(lib).Where(b => b != null).Distinct().ToList();
        var numbers = new Dictionary<string, DocumentTemplateSO>();
        foreach (CaseBlueprintSO blueprint in blueprints)
        {
            foreach (DocumentTemplateSO form in (blueprint.DocumentTemplates ?? new DocumentTemplateSO[0]).Where(t => t != null))
            {
                if (numbers.TryGetValue(form.formNumber ?? string.Empty, out DocumentTemplateSO first))
                {
                    if (first != form)
                        Error($"Forms '{first.name}' and '{form.name}' share the form number '{form.formNumber}'.", form);
                    continue;
                }

                if (!FormNumber.IsMatch(form.formNumber ?? string.Empty))
                    Error($"Form '{form.name}' has the form number '{form.formNumber}'; an agency form number is \"TC-\" and three digits.", form);
                numbers[form.formNumber ?? string.Empty] = form;
            }
        }

        foreach (string problem in Interview.ClaimProblems((lib.Interview ?? new InterviewLines()).claims, blueprints.Select(b => b.Kind).Distinct()))
            Error($"{problem} (run Tools > TimeDesk > Generate World)", lib);

        foreach (DayPlanSO plan in lib.DayPlans.Where(p => p != null))
        {
            if (!plan.Kinds.Any(k => k != null && k.blueprint != null && k.weight > 0f))
                Error($"Day plan '{plan.name}' has no traveller kind with a blueprint and a positive weight (days[].kinds).", plan);
            foreach (KindWeight k in plan.Kinds.Where(k => k != null && k.blueprint == null))
                Error($"Day plan '{plan.name}' lists a kind with no blueprint (run Tools > TimeDesk > Generate World).", plan);

            // A premade stands as its kind (TravellerKinds.PickWeight: the slot's one draw weighs only it): the pool's famous as the displaced, a forced premade as its own.
            var premadeKinds = new HashSet<TravellerKind>((plan.AvailableLegendaries ?? System.Array.Empty<LegendarySO>()).Where(l => l != null).Select(l => l.kind)
                .Concat(plan.ForcedCases.Where(f => f != null && f.legendary != null).Select(f => f.legendary.kind)));
            foreach (TravellerKind kind in premadeKinds.Where(kind => !plan.Kinds.Any(k => k != null && k.blueprint != null && k.weight > 0f && k.blueprint.Kind == kind)))
                Error($"Day plan '{plan.name}' has a {kind} premade but no {kind} kind with a positive weight; a premade stands only as its own kind.", plan);
        }

        // The present (traveller types H1): every book lists its row, so it needs a fact per book category.
        PresentPlace present = lib.BuildPresent(null);
        if (present == null)
            Error("The library has no present (world_source.json \"present\"; run Tools > TimeDesk > Generate World).", lib);
        else
            foreach (ClueCategory category in lib.ReferenceBookCategories().Where(c => present.Fact(c) == null))
                Error($"The present '{present.Label}' has no {category} fact, so its row is missing from that book (run Tools > TimeDesk > Generate World).", lib);

        // The 2150 citizens' names (K4): the Future places' lists together.
        foreach (string problem in lib.CitizenNames().Problems())
            Error(problem, lib);

        return issues;
    }
}
