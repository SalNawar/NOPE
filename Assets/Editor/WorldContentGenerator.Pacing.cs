using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Generate World's day-pacing part (Papers Please lessons 4 and D7, wave 5
/// track C): each day's papers in circulation (days[].papers, DayPapers: known
/// form numbers, none withdrawn later) and what each day brings for the first
/// time, a paper or a directive, at most one of each and named in the day's
/// bulletin (days[].bulletin, DayPacing), and each day's desk hours
/// (days[].shiftStart / shiftEnd, ShiftHours; night shifts), checked before
/// anything is written. The validator runs the same Domain rules over the day plans.
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

        CheckShifts(days, errors);

        var issuedBefore = new HashSet<string>();
        var papers = new List<IEnumerable<string>>();
        var directives = new List<IEnumerable<string>>();
        foreach (DayData d in days)
        {
            errors.AddRange(Introductions.Problems(d.asset, d.introduces ?? Array.Empty<string>()));
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
                .Select(name => DayPacing.RuleKey(name, Directives.IsClosure((TravelRuleType)Enum.Parse(typeof(TravelRuleType), rules[name].type)), (rules[name].kinds ?? Array.Empty<string>()).Length > 0,
                                                  rules[name].type == nameof(TravelRuleType.OpenDestinations)))
                .ToList());
        }

        List<List<string>> newPapers = DayPacing.NewByDay(papers), newRules = DayPacing.NewByDay(directives);
        for (int i = 0; i < days.Length; i++)
        {
            errors.AddRange(DayPacing.Problems(days[i].asset, newPapers[i], newRules[i], days[i].bulletin, i == 0));
            if (!string.IsNullOrEmpty(days[i].bulletin) && !IsAscii(days[i].bulletin))
                errors.Add($"Day '{days[i].asset}' has a non-ASCII \"bulletin\" (the briefing's fonts print ASCII).");
        }
    }

    /// <summary>
    /// Checks each day's desk hours (night shifts): each time "HH:MM" up to
    /// "24:00" (ShiftHours.TryParse), then ShiftHours.Problems (both or
    /// neither, opening before closing, 24:00 at the latest, a length within
    /// the run's GameConfigSO shiftMinHours to shiftMaxHours).
    /// </summary>
    private static void CheckShifts(IEnumerable<DayData> days, List<string> errors)
    {
        GameConfigSO config = RunGameConfig();
        foreach (DayData d in days)
        {
            string owner = $"Day '{d.asset}'";
            int start = ShiftMinute(d.shiftStart), end = ShiftMinute(d.shiftEnd);
            if (start == UnreadableTime)
                errors.Add($"{owner} has \"shiftStart\" '{d.shiftStart}'; write a 24-hour time, \"13:00\" (blank: the standard day).");
            if (end == UnreadableTime)
                errors.Add($"{owner} has \"shiftEnd\" '{d.shiftEnd}'; write a 24-hour time up to \"24:00\" (blank: the standard day).");
            if (start != UnreadableTime && end != UnreadableTime)
                errors.AddRange(ShiftHours.Problems(owner, start, end, config.shiftMinHours, config.shiftMaxHours));
        }
    }

    /// <summary>What <see cref="ShiftMinute"/> returns for a time it cannot read.</summary>
    private const int UnreadableTime = -2;

    /// <summary>An authored desk time as a minute of the day (ShiftHours.TryParse); -1 for blank (the standard day), <see cref="UnreadableTime"/> for anything unreadable.</summary>
    private static int ShiftMinute(string text) =>
        string.IsNullOrWhiteSpace(text) ? -1 : ShiftHours.TryParse(text, out int minute) ? minute : UnreadableTime;

    /// <summary>The run's GameConfigSO (Resources/RunConfig, as the game reads it), else a fresh one holding the defaults: the shift length knobs Generate World and the validator check the day plans' hours against.</summary>
    internal static GameConfigSO RunGameConfig()
    {
        RunConfigSO run = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        return run != null && run.gameConfig != null ? run.gameConfig : ScriptableObject.CreateInstance<GameConfigSO>();
    }
}
