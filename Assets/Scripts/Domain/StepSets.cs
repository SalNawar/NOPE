using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>world_source.json "pc.steps" as Generate World reads it (JsonUtility; StepSets.Parse turns it into the library's StepSetData).</summary>
[Serializable]
public sealed class StepsSource
{
    /// <summary>The sets.</summary>
    public StepSetSource[] sets;
}

/// <summary>One set as written: its type, the set it inherits, its data-only mark and its steps.</summary>
[Serializable]
public sealed class StepSetSource
{
    /// <summary>A TravellerKind name or "default".</summary>
    public string type;

    /// <summary>The type of the set it starts from (blank: none).</summary>
    public string inherit;

    /// <summary>Authored before its kind is in play.</summary>
    public bool dataOnly;

    /// <summary>Its steps.</summary>
    public StepSource[] steps;
}

/// <summary>One step as written: its enum values by name, its jump (a tab's name in source, or a link's name) or its hint.</summary>
[Serializable]
public sealed class StepSource
{
    /// <summary>Its id.</summary>
    public string id;

    /// <summary>A StepWhen name.</summary>
    public string when;

    /// <summary>ClueCategory names.</summary>
    public string[] categories;

    /// <summary>A StatementKind name (blank: Any).</summary>
    public string statement;

    /// <summary>A TruthKind name (blank: Any).</summary>
    public string truth;

    /// <summary>Form numbers.</summary>
    public string[] forms;

    /// <summary>Where a click goes (absent for a hint).</summary>
    public StepJumpSource jump;

    /// <summary>A hint's UI string key.</summary>
    public string hint;

    /// <summary>Its first day.</summary>
    public int fromDay = 1;
}

/// <summary>A step's jump as written: an AppTab name in source, or a StepLink name in link (one of the two).</summary>
[Serializable]
public sealed class StepJumpSource
{
    /// <summary>An AppTab name ("Rules").</summary>
    public string source;

    /// <summary>A StepLink name other than None and Tab ("PrimaryName").</summary>
    public string link;
}

/// <summary>
/// The steps checklist's content (the PC redesign ST3, §4.8): world_source.json
/// pc.steps read into the content library's StepSetData (Parse) and the rules
/// Generate World and the content validator share (Problems): one set per type
/// (a TravellerKind name or "default", which must exist), known parents and no
/// inherit cycle, unique step ids, a label for each, categories only where a
/// step counts them, forms only on paper steps, a jump or a hint, days from 1.
/// A data-only set (a kind not in play yet) may name forms and categories later
/// phases add. Pure.
/// </summary>
public static class StepSets
{
    /// <summary>The UI string keys the steps panel writes with (its progress, its no-case line, its hidden line).</summary>
    public static readonly IReadOnlyList<string> PanelKeys = new[] { "steps.progress", "steps.none", "steps.hidden" };

    /// <summary>A step's label key: "steps.{id}".</summary>
    public static string LabelKey(string id) => "steps." + id;

    /// <summary>True with the ClueCategory named <paramref name="name"/> (exactly; false for a name no category has yet).</summary>
    public static bool TryCategory(string name, out ClueCategory category) =>
        Enum.TryParse(name, false, out category) && Enum.IsDefined(typeof(ClueCategory), category) && !int.TryParse(name, out _);

    /// <summary>The source read into the library's shape; each unknown name, and a jump that is both or neither kind, is added to <paramref name="errors"/>.</summary>
    public static StepSetData Parse(StepsSource source, List<string> errors)
    {
        var data = new StepSetData();
        foreach (StepSetSource s in source?.sets ?? Array.Empty<StepSetSource>())
        {
            if (s == null)
                continue;
            var set = new StepSet { type = (s.type ?? string.Empty).Trim(), inherit = (s.inherit ?? string.Empty).Trim(), dataOnly = s.dataOnly };
            foreach (StepSource x in s.steps ?? Array.Empty<StepSource>())
                if (x != null)
                    set.steps.Add(ParseStep(set.type, x, errors));
            data.sets.Add(set);
        }
        return data;
    }

    /// <summary>
    /// The content's problems (see the class summary), with the UI string keys
    /// and the form numbers the game has (the blueprints' forms).
    /// </summary>
    public static List<string> Problems(StepSetData data, ICollection<string> stringKeys, ICollection<string> formNumbers)
    {
        var problems = new List<string>();
        List<StepSet> sets = (data?.sets ?? new List<StepSet>()).Where(s => s != null).ToList();
        if (!sets.Any(s => s.type == CaseSteps.DefaultType))
            problems.Add($"pc.steps has no '{CaseSteps.DefaultType}' set (the steps of a traveller kind without a set of its own).");

        var types = new HashSet<string>(StringComparer.Ordinal);
        foreach (StepSet set in sets)
        {
            string where = $"pc.steps set '{set.type}'";
            if (set.type != CaseSteps.DefaultType && !Enum.GetNames(typeof(TravellerKind)).Contains(set.type))
                problems.Add($"{where}: the type is not '{CaseSteps.DefaultType}' or one of {string.Join(", ", Enum.GetNames(typeof(TravellerKind)))}.");
            if (!types.Add(set.type))
                problems.Add($"{where} is written twice.");
            if (set.dataOnly && set.type == CaseSteps.DefaultType)
                problems.Add($"{where} is data-only, but the default set is the fallback of every kind in play.");
            if (!string.IsNullOrEmpty(set.inherit) && !sets.Any(s => s.type == set.inherit && s != set))
                problems.Add($"{where} inherits '{set.inherit}', which is no other set.");
            if (InheritCycle(sets, set))
                problems.Add($"{where}'s inherit chain is a cycle.");
            StepProblems(set, stringKeys, formNumbers, problems);
        }

        foreach (string key in PanelKeys.Where(k => stringKeys == null || !stringKeys.Contains(k)))
            problems.Add($"the UI strings have no '{key}', which the steps panel writes with");
        return problems;
    }

    /// <summary>The types of the data-only sets whose kind is in play (<paramref name="kindsInPlay"/>: the kinds a blueprint makes): their mark should go (the validator warns).</summary>
    public static List<string> DataOnlyInPlay(StepSetData data, IEnumerable<TravellerKind> kindsInPlay)
    {
        var inPlay = new HashSet<string>((kindsInPlay ?? Array.Empty<TravellerKind>()).Select(k => k.ToString()));
        return (data?.sets ?? new List<StepSet>()).Where(s => s != null && s.dataOnly && inPlay.Contains(s.type)).Select(s => s.type).ToList();
    }

    private static StepSpec ParseStep(string type, StepSource x, List<string> errors)
    {
        string where = $"pc.steps set '{type}' step '{x.id}'";
        var step = new StepSpec
        {
            id = (x.id ?? string.Empty).Trim(),
            categories = (x.categories ?? Array.Empty<string>()).Select(c => (c ?? string.Empty).Trim()).ToList(),
            forms = (x.forms ?? Array.Empty<string>()).Select(f => (f ?? string.Empty).Trim()).ToList(),
            hint = (x.hint ?? string.Empty).Trim(),
            fromDay = x.fromDay
        };
        step.when = Name(x.when, StepWhen.PapersReceived, false, $"{where}: when", errors);
        step.statement = Name(x.statement, StatementKind.Any, true, $"{where}: statement", errors);
        step.truth = Name(x.truth, TruthKind.Any, true, $"{where}: truth", errors);

        string source = (x.jump?.source ?? string.Empty).Trim(), link = (x.jump?.link ?? string.Empty).Trim();
        if (source.Length > 0 && link.Length > 0)
        {
            errors.Add($"{where} has both jump.source and jump.link: name a tab or a link.");
        }
        else if (source.Length > 0)
        {
            step.link = StepLink.Tab;
            step.tab = Name(source, AppTab.Documents, false, $"{where}: jump.source", errors);
        }
        else if (link.Length > 0)
        {
            if (Enum.TryParse(link, false, out StepLink parsed) && Enum.IsDefined(typeof(StepLink), parsed) && parsed != StepLink.None && parsed != StepLink.Tab && !int.TryParse(link, out _))
                step.link = parsed;
            else
                errors.Add($"{where}: jump.link '{link}' is not one of {string.Join(", ", Enum.GetNames(typeof(StepLink)).Where(n => n != nameof(StepLink.None) && n != nameof(StepLink.Tab)))} (a tab is named by jump.source).");
        }
        return step;
    }

    /// <summary>The enum value named <paramref name="text"/> (blank: <paramref name="blank"/> when allowed); an unknown name is an error.</summary>
    private static T Name<T>(string text, T blank, bool blankAllowed, string what, List<string> errors) where T : struct
    {
        string name = (text ?? string.Empty).Trim();
        if (name.Length == 0 && blankAllowed)
            return blank;
        if (Enum.TryParse(name, false, out T value) && Enum.IsDefined(typeof(T), value) && !int.TryParse(name, out _))
            return value;
        errors.Add($"{what} '{name}' is not one of {string.Join(", ", Enum.GetNames(typeof(T)))}.");
        return blank;
    }

    private static void StepProblems(StepSet set, ICollection<string> keys, ICollection<string> forms, List<string> problems)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (StepSpec step in set.steps ?? new List<StepSpec>())
        {
            if (step == null)
                continue;
            string where = $"pc.steps set '{set.type}' step '{step.id}'";
            if (string.IsNullOrWhiteSpace(step.id))
            {
                problems.Add($"pc.steps set '{set.type}' has a step with a blank id.");
                continue;
            }
            if (!ids.Add(step.id))
                problems.Add($"{where} is written twice in its set.");
            if (keys == null || !keys.Contains(LabelKey(step.id)))
                problems.Add($"{where}: the UI strings have no '{LabelKey(step.id)}', its label.");

            bool counts = step.when == StepWhen.Compared || step.when == StepWhen.Asked;
            if (!counts && step.categories.Count > 0)
                problems.Add($"{where}: only a Compared or Asked step counts categories.");
            if (step.when != StepWhen.Compared && (step.statement != StatementKind.Any || step.truth != TruthKind.Any))
                problems.Add($"{where}: only a Compared step has a statement or truth.");
            bool paper = step.when == StepWhen.PapersReceived || step.when == StepWhen.PaperRead || step.when == StepWhen.Requested;
            if (!paper && step.forms.Count > 0)
                problems.Add($"{where}: only a PapersReceived, PaperRead or Requested step counts forms.");
            if (!set.dataOnly)
            {
                foreach (string c in step.categories.Where(c => !TryCategory(c, out _)))
                    problems.Add($"{where}: the category '{c}' is not one of {string.Join(", ", Enum.GetNames(typeof(ClueCategory)))}.");
                foreach (string f in step.forms.Where(f => forms == null || !forms.Contains(f)))
                    problems.Add($"{where}: the form '{f}' is on no traveller's papers.");
            }
            if (step.when == StepWhen.Compared && step.categories.Count == 0 && step.truth != TruthKind.Reference && step.statement != StatementKind.Answer)
                problems.Add($"{where}: a Compared step names its categories unless it checks against the reference or checks answers (those derive theirs).");

            bool hint = !string.IsNullOrWhiteSpace(step.hint);
            if (step.link == StepLink.None && !hint)
                problems.Add($"{where} has no jump and no hint.");
            if (step.link != StepLink.None && hint)
                problems.Add($"{where} has both a jump and a hint.");
            if (hint && (keys == null || !keys.Contains(step.hint)))
                problems.Add($"{where}: the UI strings have no '{step.hint}', its hint.");
            if (step.fromDay < 1)
                problems.Add($"{where}: its first day is {step.fromDay}; days start at 1.");
        }
    }

    private static bool InheritCycle(List<StepSet> sets, StepSet from)
    {
        var seen = new HashSet<StepSet>();
        for (StepSet s = from; s != null; s = string.IsNullOrEmpty(s.inherit) ? null : sets.FirstOrDefault(x => x.type == s.inherit && x != s))
            if (!seen.Add(s))
                return true;
        return false;
    }
}
