using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// What the player does that the guide listens for (the FTUE's steps and the
/// new rule's practice; Saleh 2026-10-06: "each step completes when the
/// player does the action"). Serialized in the content library: append only.
/// </summary>
public enum GuideAction
{
    /// <summary>Nothing (a page without a practice).</summary>
    None = 0,

    /// <summary>A traveller is called to the desk (the AVAILABLE sign).</summary>
    Call = 1,

    /// <summary>A paper lands on the desk, full size, to be read (dragged there or sent by a click); a step may name the form.</summary>
    OnDesk = 2,

    /// <summary>Inspect mode is turned on (SPACE or the magnifier).</summary>
    Inspect = 3,

    /// <summary>A comparison is logged in the findings; a step may name the details it must be about (the finding's category).</summary>
    Compare = 4,

    /// <summary>The stamp bar slides out (TAB or the grey tab).</summary>
    StampsOut = 5,

    /// <summary>The passport takes its verdict stamp.</summary>
    Stamp = 6,

    /// <summary>The stamped passport is handed back on the counter (the case is decided).</summary>
    HandBack = 7,

    /// <summary>A scan finishes (the paper's copy reaches the PC).</summary>
    Scan = 8
}

/// <summary>When the guide speaks on a day (the FTUE on day 1, the new rule's moment on the guided days, then only the badge).</summary>
public enum GuideMoment
{
    /// <summary>Nothing new today.</summary>
    None,

    /// <summary>Day 1: the step-by-step first shift.</summary>
    Ftue,

    /// <summary>A guided day (2 to GuideContent.guidedThroughDay) with a new page: the rulebook opens itself on it, and a practice prompt waits for the first traveller who carries it.</summary>
    NewRule,

    /// <summary>A later day with a new page: only the "NEW" badge on the rulebook's GUIDE tab (the player has learned to check).</summary>
    Badge
}

/// <summary>One thing the player did, as the guide hears it: the action, the detail a logged comparison was about, the form a paper landing on the desk prints.</summary>
public readonly struct GuideEvent
{
    /// <summary>An event of <paramref name="action"/>, about <paramref name="category"/> (a comparison) or <paramref name="form"/> (a paper on the desk).</summary>
    public GuideEvent(GuideAction action, ClueCategory? category = null, string form = null)
    {
        Action = action;
        Category = category;
        Form = form ?? string.Empty;
    }

    /// <summary>What was done.</summary>
    public GuideAction Action { get; }

    /// <summary>The detail a logged comparison was about (Finding.Category), or null.</summary>
    public ClueCategory? Category { get; }

    /// <summary>The form number of the paper that landed on the desk, or empty.</summary>
    public string Form { get; }
}

/// <summary>
/// One prompt that waits for an action (an FTUE step, or a page's practice):
/// its line, where its arrow points (GuideTargets) and what completes it.
/// Content (world_source.json guide.ftue, guide.pages[].practice).
/// </summary>
[Serializable]
public sealed class GuideStep
{
    /// <summary>The step's id (an FTUE step's progress key).</summary>
    public string id = string.Empty;

    /// <summary>The action that completes it (None: no step).</summary>
    public GuideAction action;

    /// <summary>For Compare: the details the comparison must be about, any of them (empty: any logged comparison).</summary>
    public List<ClueCategory> categories = new List<ClueCategory>();

    /// <summary>For OnDesk: the form the paper must print (empty: any paper).</summary>
    public string form = string.Empty;

    /// <summary>Where the arrow points (GuideTargets).</summary>
    public string target = string.Empty;

    /// <summary>The one line the prompt shows ({inspect}, {stamps}, {pc}, {back}: the keys, GuideText.Keys).</summary>
    public string text = string.Empty;

    /// <summary>True when the step waits for something.</summary>
    public bool IsSet => action != GuideAction.None;

    /// <summary>True when <paramref name="e"/> completes this step: the same action, about one of its details (when it names any) and of its form (when it names one).</summary>
    public bool Matches(GuideEvent e) =>
        IsSet && e.Action == action &&
        (categories == null || categories.Count == 0 || (e.Category.HasValue && categories.Contains(e.Category.Value))) &&
        (string.IsNullOrEmpty(form) || string.Equals(e.Form, form, StringComparison.Ordinal));
}

/// <summary>
/// One page of the rulebook's GUIDE (Papers, Please's rulebook: what to check,
/// against what, an example of a fault), added the day its feature is
/// introduced (Introductions.FirstDay(feature)). Content (world_source.json guide.pages).
/// </summary>
[Serializable]
public sealed class GuidePage
{
    /// <summary>The page's id (the read pages' key).</summary>
    public string id = string.Empty;

    /// <summary>The introduction key that adds the page (Feature: "paper:TC-230", "tool:board", "rule:Rule_DebtStanding").</summary>
    public string feature = string.Empty;

    /// <summary>The page's heading.</summary>
    public string title = string.Empty;

    /// <summary>What to check.</summary>
    public string check = string.Empty;

    /// <summary>What to check it against.</summary>
    public string against = string.Empty;

    /// <summary>An example of a fault.</summary>
    public string fault = string.Empty;

    /// <summary>Where the new rule's moment points (GuideTargets): the new paper's field, the board, the PC (the books), the scanner.</summary>
    public string point = string.Empty;

    /// <summary>The one-step practice on the first traveller who carries the page's feature (its action None: no practice).</summary>
    public GuideStep practice = new GuideStep();
}

/// <summary>
/// The guide's words and steps (world_source.json "guide", Generate World into
/// the content library): the BASICS page, the pages by feature, the day-1
/// steps and the last guided day. All wording is here, so the content sheets
/// and the narrative workbook edit it.
/// </summary>
[Serializable]
public sealed class GuideContent
{
    /// <summary>The last day the new rule's moment runs (Saleh: "at least for the first week"); later days show only the badge.</summary>
    public int guidedThroughDay = 7;

    /// <summary>The BASICS page's heading.</summary>
    public string basicsTitle = string.Empty;

    /// <summary>The BASICS page's lines: the controls ({inspect}, {stamps}, {pc}, {back}: the keys).</summary>
    public List<string> basics = new List<string>();

    /// <summary>The pages, in authored order (a day's pages keep it).</summary>
    public List<GuidePage> pages = new List<GuidePage>();

    /// <summary>Day 1's steps, in order.</summary>
    public List<GuideStep> ftue = new List<GuideStep>();

    /// <summary>The line shown once the last step is done.</summary>
    public string ftueDone = string.Empty;

    /// <summary>
    /// What is wrong with the guide against the ramp (<paramref name="known"/>,
    /// days 1 to <paramref name="lastDay"/>): no guided days, no BASICS, a
    /// blank or repeated id, a page whose feature no day introduces or with a
    /// blank word, an unknown target, a step that waits for nothing or says
    /// nothing, no FTUE step, and a day after the first with no page of its own
    /// (every day's new thing has its page). Empty when sound.
    /// </summary>
    public List<string> Problems(Introductions known, int lastDay)
    {
        var problems = new List<string>();
        known ??= Introductions.None;
        if (guidedThroughDay < 1)
            problems.Add("guide.guidedThroughDay must be 1 or more.");
        if (string.IsNullOrWhiteSpace(basicsTitle) || basics == null || basics.Count == 0 || basics.Any(string.IsNullOrWhiteSpace))
            problems.Add("guide.basicsTitle and guide.basics (non-blank lines) make the BASICS page.");

        var pageIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (GuidePage page in pages ?? new List<GuidePage>())
        {
            if (page == null || string.IsNullOrWhiteSpace(page.id) || !pageIds.Add(page.id))
            {
                problems.Add($"guide.pages: '{page?.id}' is a blank or repeated id.");
                continue;
            }
            if (known.FirstDay(page.feature) == 0)
                problems.Add($"guide.pages '{page.id}': no day introduces its feature '{page.feature}' (days[].papers, rules, lies, introduces).");
            if (new[] { page.title, page.check, page.against, page.fault }.Any(string.IsNullOrWhiteSpace))
                problems.Add($"guide.pages '{page.id}': title, check, against and fault are all needed.");
            if (!GuideTargets.IsKnown(page.point))
                problems.Add($"guide.pages '{page.id}': point '{page.point}' is no target ({GuideTargets.Describe}).");
            if (page.practice != null && page.practice.IsSet)
                StepProblems($"guide.pages '{page.id}' practice", page.practice, problems);
        }

        var stepIds = new HashSet<string>(StringComparer.Ordinal);
        if (ftue == null || ftue.Count == 0)
            problems.Add("guide.ftue has no step.");
        foreach (GuideStep step in ftue ?? new List<GuideStep>())
        {
            if (step == null || string.IsNullOrWhiteSpace(step.id) || !stepIds.Add(step.id))
                problems.Add($"guide.ftue: '{step?.id}' is a blank or repeated id.");
            else if (!step.IsSet)
                problems.Add($"guide.ftue '{step.id}' waits for no action.");
            else
                StepProblems($"guide.ftue '{step.id}'", step, problems);
        }
        if (string.IsNullOrWhiteSpace(ftueDone))
            problems.Add("guide.ftueDone (the line after the last step) is blank.");

        for (int day = 2; day <= lastDay; day++)
            if (!(pages ?? new List<GuidePage>()).Any(p => p != null && known.FirstDay(p.feature) == day))
                problems.Add($"Day {day} has no guide page (guide.pages: a page whose feature the day introduces).");
        return problems;
    }

    private static void StepProblems(string where, GuideStep step, List<string> problems)
    {
        if (string.IsNullOrWhiteSpace(step.text))
            problems.Add($"{where}: its text is blank.");
        if (!GuideTargets.IsKnown(step.target))
            problems.Add($"{where}: target '{step.target}' is no target ({GuideTargets.Describe}).");
    }
}

/// <summary>
/// Where a guide arrow may point: the AVAILABLE sign, the red inspect button,
/// the grey stamp tab, the PC's grey tab, the rulebook, the calendar, the
/// Departure Board, the scanner, the counter, the traveller, a paper by its
/// form ("paper:TC-230") or one field of it ("field:TC-230/CitizenId"). A
/// paper not on the desk yet points at the rulebook (its PAPERS tab lists the
/// papers to ask for). Pure.
/// </summary>
public static class GuideTargets
{
    /// <summary>The named targets.</summary>
    public static readonly IReadOnlyList<string> Named = new[] { "sign", "inspect", "stamps", "pc", "rulebook", "calendar", "board", "scanner", "counter", "traveller" };

    /// <summary>The targets in words, for a problem's message.</summary>
    public static string Describe => string.Join(", ", Named) + ", paper:<form>, field:<form>/<ClueCategory>";

    /// <summary>True for a named target, "paper:" and a form, or "field:", a form, "/" and a ClueCategory's name.</summary>
    public static bool IsKnown(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return false;
        if (Named.Contains(target))
            return true;
        if (target.StartsWith("paper:", StringComparison.Ordinal))
            return target.Length > 6;
        return TryField(target, out _, out _);
    }

    /// <summary>The form of a "paper:" or "field:" target; false for a named one.</summary>
    public static bool TryForm(string target, out string form)
    {
        form = null;
        if (target != null && target.StartsWith("paper:", StringComparison.Ordinal) && target.Length > 6)
            form = target.Substring(6);
        else if (TryField(target, out string f, out _))
            form = f;
        return form != null;
    }

    /// <summary>The form and the detail of a "field:form/Category" target.</summary>
    public static bool TryField(string target, out string form, out ClueCategory category)
    {
        form = null;
        category = default;
        if (target == null || !target.StartsWith("field:", StringComparison.Ordinal))
            return false;
        string rest = target.Substring(6);
        int slash = rest.IndexOf('/');
        if (slash <= 0 || slash == rest.Length - 1)
            return false;
        string name = rest.Substring(slash + 1);
        if (int.TryParse(name, out _) || !Enum.TryParse(name, false, out category))
            return false;
        form = rest.Substring(0, slash);
        return true;
    }
}

/// <summary>The keys in the guide's words: {inspect}, {stamps}, {pc} and {back} become ControlRules' keys, so a line can never name another key than the tabs print. Pure.</summary>
public static class GuideText
{
    /// <summary><paramref name="line"/> with its key tokens replaced.</summary>
    public static string Keys(string line) => (line ?? string.Empty)
        .Replace("{inspect}", ControlRules.InspectKey)
        .Replace("{stamps}", ControlRules.StampsKey)
        .Replace("{pc}", ControlRules.PcKey)
        .Replace("{back}", ControlRules.BackKeys);
}

/// <summary>
/// The guide's progress in a run (WorldState.guide; additive: an older save
/// loads it empty, so its first day shows the FTUE again only on day 1):
/// the FTUE's done steps and whether it was finished or skipped, the pages
/// opened (the badge) and the pages practised.
/// </summary>
[Serializable]
public sealed class GuideState
{
    /// <summary>True once the FTUE is over: every step done, or a shift ended with it open.</summary>
    public bool ftueDone;

    /// <summary>True when the player skipped the FTUE.</summary>
    public bool ftueSkipped;

    /// <summary>The FTUE steps done (GuideStep.id), in the order they were done.</summary>
    public List<string> ftueSteps = new List<string>();

    /// <summary>The guide pages the player has opened (GuidePage.id).</summary>
    public List<string> pagesRead = new List<string>();

    /// <summary>The pages whose practice was done (GuidePage.id).</summary>
    public List<string> practiced = new List<string>();
}

/// <summary>
/// The guide over the ramp (Saleh 2026-10-06: "an FTUE and a help guide that
/// gets expanded every day like Papers, Please that explains the new rule"):
/// which pages the rulebook's GUIDE holds on a day (a page from the day its
/// feature is introduced, ordered by that day, then as authored), which are
/// new, what the day's moment is, the day's practice, and the FTUE's
/// progress (the current step, an action completing steps, skip, replay, the
/// shift's end). Built from the content and the one introduction registry,
/// so it always matches the ramp. Pure; tested headless.
/// </summary>
public sealed class Guide
{
    private readonly Introductions _known;

    /// <summary>The guide of <paramref name="content"/> over <paramref name="known"/> (null: empty).</summary>
    public Guide(GuideContent content, Introductions known)
    {
        Content = content ?? new GuideContent();
        _known = known ?? Introductions.None;
    }

    /// <summary>The words and steps.</summary>
    public GuideContent Content { get; }

    /// <summary>The day page <paramref name="page"/> is added (its feature's first day; 0: never).</summary>
    public int DayOf(GuidePage page) => page != null ? _known.FirstDay(page.feature) : 0;

    /// <summary>The pages the rulebook's GUIDE holds on <paramref name="day"/> after BASICS: every page introduced on or before it, by its day, then as authored.</summary>
    public List<GuidePage> PagesOn(int day) => (Content.pages ?? new List<GuidePage>())
        .Select((p, i) => (p, i, d: DayOf(p)))
        .Where(x => x.p != null && x.d > 0 && x.d <= day)
        .OrderBy(x => x.d).ThenBy(x => x.i)
        .Select(x => x.p)
        .ToList();

    /// <summary>The pages added on <paramref name="day"/>, as authored.</summary>
    public List<GuidePage> NewOn(int day) => PagesOn(day).Where(p => DayOf(p) == day).ToList();

    /// <summary>True when <paramref name="page"/> is added on <paramref name="day"/>.</summary>
    public bool IsNew(int day, GuidePage page) => page != null && DayOf(page) == day && day > 0;

    /// <summary>The day's moment: the FTUE on day 1; the new rule's moment on a guided day with a new page; the badge after them with one; else none.</summary>
    public GuideMoment Moment(int day)
    {
        if (day == 1)
            return GuideMoment.Ftue;
        if (day < 1 || NewOn(day).Count == 0)
            return GuideMoment.None;
        return day <= Content.guidedThroughDay ? GuideMoment.NewRule : GuideMoment.Badge;
    }

    /// <summary>The page the new rule's moment opens on <paramref name="day"/> (its first new page), or null when the day has no moment.</summary>
    public GuidePage MomentPage(int day) => Moment(day) == GuideMoment.NewRule ? NewOn(day)[0] : null;

    /// <summary>The day's practice: the first new page of a guided day (2 on) that has one and was not practised; null otherwise.</summary>
    public GuidePage Practice(int day, GuideState state) => Moment(day) != GuideMoment.NewRule
        ? null
        : NewOn(day).FirstOrDefault(p => p.practice != null && p.practice.IsSet && !(state?.practiced?.Contains(p.id) ?? false));

    /// <summary>True when a traveller carrying <paramref name="forms"/> carries <paramref name="page"/>'s feature: its paper for a paper's page; every traveller for a tool's, a rule's or a book's.</summary>
    public static bool Carries(GuidePage page, IEnumerable<string> forms)
    {
        if (page == null)
            return false;
        if (!page.feature.StartsWith("paper:", StringComparison.Ordinal))
            return true;
        string form = page.feature.Substring(6);
        return (forms ?? Enumerable.Empty<string>()).Contains(form);
    }

    /// <summary>True when a page added on <paramref name="day"/> has not been opened yet: the GUIDE tab wears its "NEW" badge.</summary>
    public bool Badge(int day, GuideState state) => NewOn(day).Any(p => !(state?.pagesRead?.Contains(p.id) ?? false));

    /// <summary>Records <paramref name="pageId"/> as opened (once).</summary>
    public static void Read(GuideState state, string pageId)
    {
        if (state != null && !string.IsNullOrEmpty(pageId) && !state.pagesRead.Contains(pageId))
            state.pagesRead.Add(pageId);
    }

    /// <summary>Records <paramref name="e"/> against the day's practice <paramref name="page"/>: true (and the page practised) when it completes it.</summary>
    public static bool Practise(GuideState state, GuidePage page, GuideEvent e)
    {
        if (state == null || page?.practice == null || !page.practice.Matches(e) || state.practiced.Contains(page.id))
            return false;
        state.practiced.Add(page.id);
        return true;
    }

    // ---- The FTUE ----

    /// <summary>True while the FTUE runs: not finished, not skipped (day 1, or replayed).</summary>
    public static bool FtueOpen(GuideState state) => state != null && !state.ftueDone && !state.ftueSkipped;

    /// <summary>The step the FTUE shows: the first not done; null when it is over.</summary>
    public GuideStep CurrentStep(GuideState state) => !FtueOpen(state)
        ? null
        : (Content.ftue ?? new List<GuideStep>()).FirstOrDefault(s => s != null && s.IsSet && !state.ftueSteps.Contains(s.id));

    /// <summary>The current step's number (1-based) of the steps' count, for the prompt ("2 / 8"); 0 when over.</summary>
    public int StepNumber(GuideState state)
    {
        GuideStep step = CurrentStep(state);
        return step == null ? 0 : Content.ftue.IndexOf(step) + 1;
    }

    /// <summary>
    /// Records <paramref name="e"/>: every open step it completes is done (a
    /// later step done early stays done; the prompt still asks the first
    /// open one), and when none is left the FTUE is over. True when a step
    /// was done.
    /// </summary>
    public bool Record(GuideState state, GuideEvent e)
    {
        if (!FtueOpen(state))
            return false;
        bool any = false;
        foreach (GuideStep step in Content.ftue ?? new List<GuideStep>())
            if (step != null && !state.ftueSteps.Contains(step.id) && step.Matches(e))
            {
                state.ftueSteps.Add(step.id);
                any = true;
            }
        if (any && CurrentStep(state) == null)
            state.ftueDone = true;
        return any;
    }

    /// <summary>The player skips the FTUE: it is over (Settings and the F1 card replay it).</summary>
    public static void Skip(GuideState state)
    {
        if (state != null)
            state.ftueSkipped = true;
    }

    /// <summary>The FTUE from its first step again (Settings, the F1 card), on any day.</summary>
    public static void Replay(GuideState state)
    {
        if (state == null)
            return;
        state.ftueDone = false;
        state.ftueSkipped = false;
        state.ftueSteps.Clear();
    }

    /// <summary>The shift ends: an FTUE still open is over (it never runs into another day by itself).</summary>
    public static void CloseShift(GuideState state)
    {
        if (FtueOpen(state))
            state.ftueDone = true;
    }
}
