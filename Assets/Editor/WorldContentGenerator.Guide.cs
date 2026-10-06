using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/// <summary>
/// Generate World's guide part (the FTUE and the daily guide, Saleh
/// 2026-10-06): world_source.json "guide" (the BASICS page, the pages by
/// feature, the day-1 steps, the last guided day) is checked against the
/// source's own ramp (GuideContent.Problems over the days' introductions: a
/// page whose feature no day introduces, a day with no page, an unknown
/// target, action or detail; GuideContent.BulletinProblems: a day's bulletin
/// that does not name its new pages) and written verbatim into the library's guide
/// content, which the rulebook's GUIDE and the guide director read.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>world_source.json "guide".</summary>
    [Serializable] private sealed class GuideData
    {
        public int guidedThroughDay;
        public string basicsTitle;
        public string[] basics;
        public GuidePageData[] pages;
        public GuideStepData[] ftue;
        public string ftueDone;
    }

    /// <summary>One guide page (guide.pages): its feature, words, where the moment points and its practice.</summary>
    [Serializable] private sealed class GuidePageData
    {
        public string id;
        public string feature;
        public string title;
        public string named;
        public string check;
        public string against;
        public string fault;
        public string point;
        public GuideStepData practice;
    }

    /// <summary>One step (guide.ftue, a page's practice): its action (a GuideAction name; blank: none), details (ClueCategory names), form, target and line.</summary>
    [Serializable] private sealed class GuideStepData
    {
        public string id;
        public string action;
        public string[] categories;
        public string form;
        public string target;
        public string text;
    }

    /// <summary>Checks the guide: every action and detail named, then GuideContent.Problems over the source's ramp (days 1 to the last day).</summary>
    private static void CheckGuide(WorldSource src, List<string> errors)
    {
        GuideData g = src.guide;
        if (g == null)
        {
            errors.Add("world_source.json has no \"guide\" (the FTUE and the rulebook's GUIDE pages).");
            return;
        }
        foreach (GuideStepData step in (g.ftue ?? Array.Empty<GuideStepData>()).Concat((g.pages ?? Array.Empty<GuidePageData>()).Select(p => p?.practice)))
        {
            if (step == null)
                continue;
            if (!string.IsNullOrEmpty(step.action) && !ParseEnum(step.action, out GuideAction _))
                errors.Add($"guide: step '{step.id}' action '{step.action}' is none of {string.Join(", ", Enum.GetNames(typeof(GuideAction)))}.");
            foreach (string c in step.categories ?? Array.Empty<string>())
                if (!ParseEnum(c, out ClueCategory _))
                    errors.Add($"guide: step '{step.id}' detail '{c}' is no ClueCategory.");
        }
        DayData[] days = src.days ?? Array.Empty<DayData>();
        var known = new Introductions(days.Select(d => (d.day, Introductions.DayKeys(d.papers, d.rules,
            (d.lies ?? Array.Empty<string>()).Select(l => ParseEnum(l, out LieKind lie) ? (LieKind?)lie : null).Where(l => l.HasValue).Select(l => l.Value),
            null, d.introduces))));
        GuideContent guide = BuildGuide(g);
        errors.AddRange(guide.Problems(known, days.Length == 0 ? 0 : days.Max(d => d.day)));
        errors.AddRange(guide.BulletinProblems(known, days.Select(d => (d.day, d.bulletin))));
    }

    /// <summary>The library's guide content from the source, verbatim (an unknown action reads as None and a bad detail is dropped; CheckGuide refuses both).</summary>
    private static GuideContent BuildGuide(GuideData g) => new GuideContent
    {
        guidedThroughDay = g?.guidedThroughDay ?? 0,
        basicsTitle = g?.basicsTitle ?? string.Empty,
        basics = (g?.basics ?? Array.Empty<string>()).ToList(),
        pages = (g?.pages ?? Array.Empty<GuidePageData>()).Where(p => p != null).Select(p => new GuidePage
        {
            id = p.id ?? string.Empty,
            feature = p.feature ?? string.Empty,
            title = p.title ?? string.Empty,
            named = p.named ?? string.Empty,
            check = p.check ?? string.Empty,
            against = p.against ?? string.Empty,
            fault = p.fault ?? string.Empty,
            point = p.point ?? string.Empty,
            practice = BuildGuideStep(p.practice)
        }).ToList(),
        ftue = (g?.ftue ?? Array.Empty<GuideStepData>()).Where(s => s != null).Select(BuildGuideStep).ToList(),
        ftueDone = g?.ftueDone ?? string.Empty
    };

    private static GuideStep BuildGuideStep(GuideStepData s) => s == null ? new GuideStep() : new GuideStep
    {
        id = s.id ?? string.Empty,
        action = ParseEnum(s.action, out GuideAction action) ? action : GuideAction.None,
        categories = (s.categories ?? Array.Empty<string>()).Select(c => ParseEnum(c, out ClueCategory cat) ? (ClueCategory?)cat : null)
            .Where(c => c.HasValue).Select(c => c.Value).ToList(),
        form = s.form ?? string.Empty,
        target = s.target ?? string.Empty,
        text = s.text ?? string.Empty
    };

    /// <summary>Writes the guide into the library (authoritative).</summary>
    private static void WireGuide(ContentLibrarySO lib, GuideData guide)
    {
        var so = new SerializedObject(lib);
        so.FindProperty("guide").boxedValue = BuildGuide(guide);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lib);
    }
}
