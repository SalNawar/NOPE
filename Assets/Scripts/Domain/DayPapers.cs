using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The papers in circulation on a day (Papers Please lesson D7, "documents
/// arrive one day at a time"; world_source.json days[].papers): the form
/// numbers the agency has issued by that day. A traveller carries only the
/// issued forms of their kind's blueprint, the papers menu offers only them,
/// and a paper-set procedure never asks for a form not issued yet
/// (CaseFacts.Issued). An empty list issues every form, so a day that does
/// not stage its papers keeps the whole set. Pure; tested headless.
/// </summary>
public static class DayPapers
{
    /// <summary>True when <paramref name="formNumber"/> is issued on a day whose papers are <paramref name="papers"/> (null or empty: every form).</summary>
    public static bool Issued(IReadOnlyCollection<string> papers, string formNumber) =>
        papers == null || papers.Count == 0 || (formNumber != null && papers.Contains(formNumber));

    /// <summary>The forms of <paramref name="forms"/> issued on a day whose papers are <paramref name="papers"/>, in their order (null entries skipped).</summary>
    public static List<string> Carried(IEnumerable<string> forms, IReadOnlyCollection<string> papers) =>
        (forms ?? Enumerable.Empty<string>()).Where(f => f != null && Issued(papers, f)).ToList();

    /// <summary>
    /// One message per problem in a day's papers list (Generate World and
    /// Validate Content Library, in the same words): a form number no
    /// template has, a form listed twice, and a form a later day no longer
    /// issues (<paramref name="earlier"/>: the forms issued on the days
    /// before; a staged day never withdraws a paper). An empty list is none.
    /// </summary>
    public static List<string> Problems(string asset, IReadOnlyList<string> papers, ICollection<string> knownForms, IReadOnlyCollection<string> earlier)
    {
        var problems = new List<string>();
        if (papers == null || papers.Count == 0)
            return problems;

        var seen = new HashSet<string>();
        foreach (string form in papers)
        {
            if (string.IsNullOrWhiteSpace(form) || knownForms == null || !knownForms.Contains(form))
                problems.Add($"Day '{asset}' issues the paper '{form}', which no document template has (a form number such as TC-101).");
            else if (!seen.Add(form))
                problems.Add($"Day '{asset}' lists the paper '{form}' twice.");
        }

        foreach (string form in earlier ?? new string[0])
            if (!papers.Contains(form))
                problems.Add($"Day '{asset}' no longer issues '{form}', which an earlier day issued: papers arrive one day at a time and are never withdrawn (list it, or leave the day's papers empty for every form).");
        return problems;
    }
}
