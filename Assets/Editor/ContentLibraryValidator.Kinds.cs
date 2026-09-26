using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Validate Content Library's traveller-kinds part (the redesign's phase 3):
/// the agency forms each blueprint lists and the claim line of every kind a
/// blueprint makes.
/// </summary>
public static partial class ContentLibraryValidator
{
    /// <summary>An agency form number: "TC-" and three digits (traveller types F1).</summary>
    private static readonly Regex FormNumber = new Regex(@"^TC-\d{3}$");

    /// <summary>
    /// Reports, over every blueprint a traveller can come from: a form whose
    /// number is not "TC-nnn" or is shared with another form; a form handed
    /// over on request that its blueprint's kind may not be asked for
    /// (DocumentTemplateSO.askableBy); and a kind with no claim line, or a
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
                if (form.handOver == DocumentHandOver.OnRequest && !form.IsAskableBy(blueprint.Kind))
                    Error($"Blueprint '{blueprint.name}' ({blueprint.Kind}) hands '{form.name}' over on request, but the form's askableBy does not list {blueprint.Kind}.", form);

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

        return issues;
    }
}
