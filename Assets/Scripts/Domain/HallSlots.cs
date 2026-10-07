using System;
using System.Collections.Generic;

/// <summary>Where the debt crisis stands, read from the day's desk hours (night shifts: the standard day, the extended hours, the night shifts; HallStates.PhaseOf).</summary>
public enum HallPhase
{
    /// <summary>A day that closes at the standard hour or earlier (days 1-7, 09:00-17:00): the ordinary week.</summary>
    Normal,

    /// <summary>A day that closes after the standard hour but before midnight (days 8-11, 13:00-21:00): the Bureau extends the hours.</summary>
    Extended,

    /// <summary>A day that closes at midnight (days 12-15, 16:00-24:00): night shifts.</summary>
    Nights
}

/// <summary>Today's special, read from the day plan's rules (HallStates.EventOf).</summary>
public enum HallEvent
{
    /// <summary>An ordinary day.</summary>
    None,

    /// <summary>A transponder recall starts today (a TransponderRecall rule today that yesterday's plan lacked).</summary>
    Recall,

    /// <summary>A border is closed today (a NationForbidden or NationEraForbidden rule).</summary>
    Ban,

    /// <summary>The displaced return home (a ReturnHome rule).</summary>
    Return
}

/// <summary>How many famous travellers of one nation the clerk let through.</summary>
public readonly struct HallExhibit
{
    /// <summary>The nation's id ("egypt").</summary>
    public readonly string Nation;

    /// <summary>How many of its famous travellers were accepted (their latest verdict).</summary>
    public readonly int Count;

    /// <summary>One nation's count.</summary>
    public HallExhibit(string nation, int count)
    {
        Nation = nation;
        Count = count;
    }
}

/// <summary>
/// The hall's variables (the hall slots spec, docs/superpowers/specs/2026-10-07-hall-slots-design.md):
/// what the swappable art of the anime hall reacts to. Read from the run
/// each morning and live where it changes during a shift (the tier). Never
/// shown to the player as a word or a number: only the art changes.
/// </summary>
public sealed class HallState
{
    /// <summary>The leading culture: a nation id, or null when no culture leads (neutral).</summary>
    public string Culture;

    /// <summary>The Helix River's state (HelixRiver.Tier).</summary>
    public StabilityTier Tier;

    /// <summary>The debt crisis's phase (HallStates.PhaseOf).</summary>
    public HallPhase Phase;

    /// <summary>Today's special (HallStates.EventOf).</summary>
    public HallEvent Event;

    /// <summary>The famous travellers let through, by nation, strongest first (HallStates.ExhibitsOf); never null.</summary>
    public IReadOnlyList<HallExhibit> Exhibits = Array.Empty<HallExhibit>();

    /// <summary>The nation of the famous traveller let through most recently, or null.</summary>
    public string RecentExhibit;

    /// <summary>The nation with the most famous travellers let through (the most recent among a tie), or null.</summary>
    public string StrongestExhibit;

    /// <summary>True when <paramref name="other"/> picks the same art: every variable equal.</summary>
    public bool SameAs(HallState other)
    {
        if (other == null || Culture != other.Culture || Tier != other.Tier || Phase != other.Phase || Event != other.Event
            || RecentExhibit != other.RecentExhibit || StrongestExhibit != other.StrongestExhibit || Exhibits.Count != other.Exhibits.Count)
            return false;
        for (int i = 0; i < Exhibits.Count; i++)
            if (Exhibits[i].Nation != other.Exhibits[i].Nation || Exhibits[i].Count != other.Exhibits[i].Count)
                return false;
        return true;
    }

    /// <summary>The state as one line for the logs and the cheat menu ("culture=egypt tier=strained phase=extended event=none exhibit=greece recent=egypt"; "none" without an exhibit).</summary>
    public override string ToString() =>
        $"culture={HallStates.CultureKey(Culture)} tier={HallStates.Key(Tier)} phase={HallStates.Key(Phase)} event={HallStates.Key(Event)} exhibit={StrongestExhibit ?? HallStates.NoExhibit} recent={RecentExhibit ?? HallStates.NoExhibit}";
}

/// <summary>The rules that read the hall's variables from the run (pure, tested: HallSlotsTests).</summary>
public static class HallStates
{
    /// <summary>The condition value of "no culture leads" and "no exhibit".</summary>
    public const string Neutral = "neutral";

    /// <summary>The condition value of "no famous traveller let through".</summary>
    public const string NoExhibit = "none";

    /// <summary>
    /// The phase of a day whose desk hours are <paramref name="today"/>, from
    /// how late it sends the clerk home (ShiftHours.Lateness against
    /// <paramref name="standard"/>, GameConfigSO's standard day): Normal at the
    /// standard closing or earlier, Nights at midnight, Extended between (the
    /// shipped ramp: days 1-7 09:00-17:00, 8-11 13:00-21:00, 12-15 16:00-24:00;
    /// world_source.json days[].shiftStart / shiftEnd).
    /// </summary>
    public static HallPhase PhaseOf(ShiftHours today, ShiftHours standard)
    {
        float lateness = ShiftHours.Lateness(today.EndMinute, standard);
        if (lateness >= 1f)
            return HallPhase.Nights;
        return lateness > 0f ? HallPhase.Extended : HallPhase.Normal;
    }

    /// <summary>
    /// Today's special from the day plans' rule types: Return when a
    /// ReturnHome rule is in force today, else Ban when a border closure
    /// (NationForbidden, NationEraForbidden) is, else Recall when a
    /// TransponderRecall rule starts today (in force today, not in
    /// <paramref name="yesterday"/>), else None. Null lists read as empty.
    /// </summary>
    public static HallEvent EventOf(IEnumerable<TravelRuleType> today, IEnumerable<TravelRuleType> yesterday)
    {
        bool returns = false, ban = false, recall = false;
        if (today != null)
            foreach (TravelRuleType t in today)
            {
                returns |= t == TravelRuleType.ReturnHome;
                ban |= t == TravelRuleType.NationForbidden || t == TravelRuleType.NationEraForbidden;
                recall |= t == TravelRuleType.TransponderRecall;
            }
        if (returns)
            return HallEvent.Return;
        if (ban)
            return HallEvent.Ban;
        if (!recall)
            return HallEvent.None;
        if (yesterday != null)
            foreach (TravelRuleType t in yesterday)
                if (t == TravelRuleType.TransponderRecall)
                    return HallEvent.None;
        return HallEvent.Recall;
    }

    /// <summary>
    /// The famous travellers let through, by nation, from the run's flags in
    /// their order (FlagKeys.PremadeVerdict "premade:{id}:accepted": the
    /// latest verdict wins, so a later deny clears an accept): each accepted
    /// premade counts for its nation (<paramref name="nationOf"/>: its id to
    /// its nation's id; null or blank skips it). Strongest first, a tie in
    /// the order of their most recent acceptance (the latest first).
    /// <paramref name="recent"/> is the last accepted premade's nation,
    /// <paramref name="strongest"/> the first of the result (null without any).
    /// </summary>
    public static List<HallExhibit> ExhibitsOf(IReadOnlyList<string> flags, Func<string, string> nationOf, out string recent, out string strongest)
    {
        recent = null;
        strongest = null;
        var counts = new Dictionary<string, int>();
        var latest = new Dictionary<string, int>();
        if (flags != null && nationOf != null)
            for (int i = 0; i < flags.Count; i++)
            {
                if (!FlagKeys.TryParsePremade(flags[i], out string premade, out PremadeFlag flag) || flag != PremadeFlag.Accepted)
                    continue;
                string nation = nationOf(premade);
                if (string.IsNullOrWhiteSpace(nation))
                    continue;
                counts.TryGetValue(nation, out int n);
                counts[nation] = n + 1;
                latest[nation] = i;
                recent = nation;
            }

        var result = new List<HallExhibit>();
        foreach (KeyValuePair<string, int> c in counts)
            result.Add(new HallExhibit(c.Key, c.Value));
        result.Sort((a, b) => a.Count != b.Count ? b.Count.CompareTo(a.Count) : latest[b.Nation].CompareTo(latest[a.Nation]));
        if (result.Count > 0)
            strongest = result[0].Nation;
        return result;
    }

    /// <summary>A culture or exhibit as a condition value: the nation's id, or "neutral" for none.</summary>
    public static string CultureKey(string nation) => string.IsNullOrWhiteSpace(nation) ? Neutral : nation;

    /// <summary>A tier as a condition value ("strained").</summary>
    public static string Key(StabilityTier tier) => tier.ToString().ToLowerInvariant();

    /// <summary>A phase as a condition value ("nights").</summary>
    public static string Key(HallPhase phase) => phase.ToString().ToLowerInvariant();

    /// <summary>A special as a condition value ("ban").</summary>
    public static string Key(HallEvent e) => e.ToString().ToLowerInvariant();
}

/// <summary>
/// One variant of a hall slot: the art file it shows
/// (<c>&lt;slot&gt;/&lt;id&gt;.png</c>, or <c>&lt;id&gt;_1..N.png</c> when it has
/// alternates) and when (HallConditions). Edited in HallSlotsSO's Inspector.
/// </summary>
[Serializable]
public sealed class HallVariantDef
{
    /// <summary>The variant's id and its file's name ("egypt", "breaching").</summary>
    public string id;

    /// <summary>When it shows (HallConditions: "culture=egypt", "tier>=breaching", "phase=nights &amp; culture=japan"); empty: always.</summary>
    public string when;

    /// <summary>Among the variants whose condition holds, the highest priority shows (the first listed among equals).</summary>
    public int priority;

    /// <summary>How many interchangeable paintings it has (1: <c>id.png</c>; N: <c>id_1.png</c>..<c>id_N.png</c>, one picked per run by Seeds.ForHallSlot).</summary>
    public int alternates = 1;

    /// <summary>What to paint (the art request's line; the Export Templates manifest lists it).</summary>
    public string describes;
}

/// <summary>
/// A swappable part of the anime hall (HallSlotsSO lists them): a region of
/// the hall painting's 2172 x 724 source canvas, drawn over the painting at
/// <see cref="order"/> on its sorting layer, whose art follows the hall's
/// variables. A slot that varies a registered layer names it
/// (<see cref="layer"/>: a missing variant file keeps the painting as it is);
/// a new overlay names none (a missing file draws the editor's stand-in).
/// </summary>
[Serializable]
public sealed class HallSlotDef
{
    /// <summary>The slot's id and its art folder (a registered layer's file stem, "13-flag-left-cloth", or "new-…" for a new overlay).</summary>
    public string id;

    /// <summary>The registered layer it varies ("13 Flag left cloth", AnimeHallPresentation's id), or empty for a new overlay.</summary>
    public string layer;

    /// <summary>What it shows in the hall today (the art request's line).</summary>
    public string shows;

    /// <summary>Its region on the source canvas (pixels, top-left origin): the paintable area, the template's crop and the stand-in.</summary>
    public int x, y, width, height;

    /// <summary>Its sorting order on the painting's sorting layer (the painting draws at 58, the portal glows at 59, the crowds at 61-62, the gallery fixtures at 99).</summary>
    public int order = 60;

    /// <summary>Its variants.</summary>
    public List<HallVariantDef> variants = new List<HallVariantDef>();

    /// <summary>The variant that shows when none's condition holds (an id of <see cref="variants"/>); empty: the painting as it is (nothing drawn).</summary>
    public string fallback;

    /// <summary>True for a new overlay (no registered layer): a missing file draws the stand-in in the editor and development builds.</summary>
    public bool IsNewOverlay => string.IsNullOrEmpty(layer);
}

/// <summary>What a slot shows: a variant's file, or nothing (the painting as it is).</summary>
public readonly struct HallPick
{
    /// <summary>The variant picked, or null (nothing: the painting as it is).</summary>
    public readonly string Variant;

    /// <summary>The art file's name without its extension (the variant's id, or "id_k" for its k-th alternate), or null.</summary>
    public readonly string File;

    /// <summary>A pick.</summary>
    public HallPick(string variant, string file)
    {
        Variant = variant;
        File = file;
    }

    /// <summary>True when a variant shows.</summary>
    public bool Shows => File != null;
}

/// <summary>
/// The hall slots' conditions: clauses joined by "&amp;", each true for the
/// state to match (an empty condition always matches). A clause is
/// <c>culture=egypt</c> (or <c>neutral</c>; <c>!=</c> negates),
/// <c>tier=breaching</c> / <c>tier&gt;=strained</c> / <c>tier&lt;=strained</c>
/// (steady &lt; strained &lt; breaching &lt; collapsing),
/// <c>phase=nights</c> / <c>phase&gt;=extended</c> (normal &lt; extended &lt; nights),
/// <c>event=ban</c> (none, recall, ban, return), <c>exhibit=egypt</c> (the
/// strongest famous nation; <c>none</c> without any), <c>recent=egypt</c>
/// (the latest one) and <c>exhibit:egypt</c> (at least one of that nation let
/// through). Pure, tested: HallSlotsTests.
/// </summary>
public static class HallConditions
{
    private static readonly string[] Tiers = { "steady", "strained", "breaching", "collapsing" };
    private static readonly string[] Phases = { "normal", "extended", "nights" };
    private static readonly string[] Events = { "none", "recall", "ban", "return" };

    /// <summary>True when every clause of <paramref name="condition"/> holds for <paramref name="state"/>; false for a clause that does not parse.</summary>
    public static bool Matches(string condition, HallState state)
    {
        if (state == null)
            return false;
        if (string.IsNullOrWhiteSpace(condition))
            return true;
        foreach (string raw in condition.Split('&'))
        {
            string clause = raw.Trim();
            if (clause.Length == 0)
                continue;
            if (!Clause(clause, state, out bool holds, out _) || !holds)
                return false;
        }
        return true;
    }

    /// <summary>The problems of <paramref name="condition"/> (an unknown key, operator or value; a nation not in <paramref name="nations"/> when given), each a line; none for a good one.</summary>
    public static List<string> Problems(string condition, ICollection<string> nations)
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(condition))
            return problems;
        var probe = new HallState();
        foreach (string raw in condition.Split('&'))
        {
            string clause = raw.Trim();
            if (clause.Length == 0)
                continue;
            if (!Clause(clause, probe, out _, out string nation))
                problems.Add($"'{clause}' does not parse (culture=, tier=, tier>=, phase=, event=, exhibit=, recent=, exhibit:)");
            else if (nation != null && nations != null && nation != HallStates.Neutral && nation != HallStates.NoExhibit && !nations.Contains(nation))
                problems.Add($"'{clause}' names no nation of the content");
        }
        return problems;
    }

    /// <summary>Reads one clause: false when it does not parse; <paramref name="nation"/> is the nation it names, if any.</summary>
    private static bool Clause(string clause, HallState state, out bool holds, out string nation)
    {
        holds = false;
        nation = null;
        int colon = clause.IndexOf(':');
        if (colon > 0 && clause.IndexOf('=') < 0)
        {
            if (clause.Substring(0, colon).Trim() != "exhibit")
                return false;
            nation = clause.Substring(colon + 1).Trim();
            if (nation.Length == 0)
                return false;
            foreach (HallExhibit e in state.Exhibits)
                if (e.Nation == nation && e.Count > 0)
                    holds = true;
            return true;
        }

        string op = clause.Contains(">=") ? ">=" : clause.Contains("<=") ? "<=" : clause.Contains("!=") ? "!=" : clause.Contains("=") ? "=" : null;
        if (op == null)
            return false;
        int at = clause.IndexOf(op, StringComparison.Ordinal);
        string key = clause.Substring(0, at).Trim();
        string value = clause.Substring(at + op.Length).Trim();
        if (value.Length == 0)
            return false;

        switch (key)
        {
            case "tier":
                return Ordered(Tiers, (int)state.Tier, op, value, out holds);
            case "phase":
                return Ordered(Phases, (int)state.Phase, op, value, out holds);
            case "event":
                return Ordered(Events, (int)state.Event, op == "=" || op == "!=" ? op : null, value, out holds);
            case "culture":
            case "exhibit":
            case "recent":
                if (op != "=" && op != "!=")
                    return false;
                nation = value;
                string actual = key == "culture" ? HallStates.CultureKey(state.Culture)
                    : key == "exhibit" ? (state.StrongestExhibit ?? HallStates.NoExhibit)
                    : (state.RecentExhibit ?? HallStates.NoExhibit);
                if (key != "culture" && value == HallStates.Neutral)
                    value = HallStates.NoExhibit;
                holds = (actual == value) == (op == "=");
                return true;
            default:
                return false;
        }
    }

    /// <summary>A clause on an ordered scale: false when the value or the operator is not one of it.</summary>
    private static bool Ordered(string[] scale, int actual, string op, string value, out bool holds)
    {
        holds = false;
        int wanted = Array.IndexOf(scale, value);
        if (wanted < 0 || op == null)
            return false;
        switch (op)
        {
            case "=": holds = actual == wanted; break;
            case "!=": holds = actual != wanted; break;
            case ">=": holds = actual >= wanted; break;
            case "<=": holds = actual <= wanted; break;
        }
        return true;
    }
}

/// <summary>
/// Which art a hall slot shows (pure, tested: HallSlotsTests): the variant of
/// the highest priority whose condition holds (the first listed among equal
/// priorities), else the slot's fallback, else nothing; a variant with N
/// alternates shows one of them, picked once per run by Seeds.ForHallSlot (a
/// replay of the run shows the same hall).
/// </summary>
public static class HallSlotPick
{
    /// <summary>What <paramref name="slot"/> shows for <paramref name="state"/> in run <paramref name="runSeed"/>.</summary>
    public static HallPick Pick(HallSlotDef slot, HallState state, int runSeed)
    {
        if (slot == null || slot.variants == null)
            return default;
        HallVariantDef best = null;
        foreach (HallVariantDef v in slot.variants)
            if (v != null && !string.IsNullOrWhiteSpace(v.id) && (best == null || v.priority > best.priority) && HallConditions.Matches(v.when, state))
                best = v;
        if (best == null && !string.IsNullOrWhiteSpace(slot.fallback))
            best = slot.variants.Find(v => v != null && v.id == slot.fallback);
        if (best == null)
            return default;
        if (best.alternates <= 1)
            return new HallPick(best.id, best.id);
        int k = 1 + new SeededRandom(Seeds.ForHallSlot(runSeed, slot.id + "/" + best.id)).Range(0, best.alternates);
        return new HallPick(best.id, best.id + "_" + k);
    }

    /// <summary>
    /// The problems of a slot list on a <paramref name="canvasWidth"/> x
    /// <paramref name="canvasHeight"/> canvas, each a line naming the slot:
    /// a blank or repeated id, a region off the canvas or empty, a blank or
    /// repeated variant id, alternates under 1, a bad condition
    /// (HallConditions.Problems), a fallback that names no variant.
    /// </summary>
    public static List<string> Problems(IReadOnlyList<HallSlotDef> slots, int canvasWidth, int canvasHeight, ICollection<string> nations)
    {
        var problems = new List<string>();
        if (slots == null)
            return problems;
        var ids = new HashSet<string>();
        for (int i = 0; i < slots.Count; i++)
        {
            HallSlotDef s = slots[i];
            if (s == null)
            {
                problems.Add($"slot #{i}: empty");
                continue;
            }
            string name = string.IsNullOrWhiteSpace(s.id) ? $"slot #{i}" : s.id;
            if (string.IsNullOrWhiteSpace(s.id))
                problems.Add($"{name}: no id");
            else if (!ids.Add(s.id))
                problems.Add($"{name}: the id is used twice");
            if (s.width <= 0 || s.height <= 0 || s.x < 0 || s.y < 0 || s.x + s.width > canvasWidth || s.y + s.height > canvasHeight)
                problems.Add($"{name}: region {s.x},{s.y} {s.width}x{s.height} is not inside the {canvasWidth}x{canvasHeight} canvas");
            var variantIds = new HashSet<string>();
            foreach (HallVariantDef v in s.variants ?? new List<HallVariantDef>())
            {
                if (v == null || string.IsNullOrWhiteSpace(v.id))
                {
                    problems.Add($"{name}: a variant has no id");
                    continue;
                }
                if (!variantIds.Add(v.id))
                    problems.Add($"{name}/{v.id}: the variant id is used twice");
                if (v.alternates < 1)
                    problems.Add($"{name}/{v.id}: alternates must be 1 or more");
                foreach (string p in HallConditions.Problems(v.when, nations))
                    problems.Add($"{name}/{v.id}: {p}");
            }
            if (!string.IsNullOrWhiteSpace(s.fallback) && !variantIds.Contains(s.fallback))
                problems.Add($"{name}: the fallback '{s.fallback}' is not one of its variants");
        }
        return problems;
    }
}
