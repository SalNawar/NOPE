using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// The narrative workbook (Saleh 2026-09-30: "track all cases on all days, if they are
/// generated or not, the lines they use, if a case has a narrative I can write the lines,
/// when do they trigger"): a writer's view of the content spreadsheet's own tables
/// (<see cref="ContentSheets.Export"/> of world_source.json with <see cref="ContentSheetMap"/>),
/// never a second content format. Its sheets: README; Days (each day's queue, authored
/// and generated slots, rules, pools and what can fire); Cases (the reference run's
/// travellers and every line they would say, from the editor's export); Narrative (one
/// block per story character, dialog and story beat: its triggers, every line, choice,
/// effect, condition and pull, each an editable cell bound to one cell of a content
/// table); Lines (every personality and default voice line by slot, editable, with new
/// rows and removals); Triggers (when each narrative event fires and what it depends
/// on); Lists (hidden: the drop-downs' values). Every editable cell carries its binding:
/// "ref" names the content sheet and row ("dialogLines#57", the content workbook's own
/// row number; ":text" names the column in Narrative) and the hidden "was" holds the
/// row as it was exported, so <see cref="NarrativeImport.Apply"/> writes back only what
/// the author changed and refuses what changed in the source meanwhile. Pure.
/// </summary>
public static class NarrativeWorkbook
{
    /// <summary>The guide sheet.</summary>
    public const string ReadmeSheet = "README";

    /// <summary>The days overview (read-only).</summary>
    public const string DaysSheet = "Days";

    /// <summary>The reference run's travellers (read-only).</summary>
    public const string CasesSheet = "Cases";

    /// <summary>The authored narratives (editable text).</summary>
    public const string NarrativeSheet = "Narrative";

    /// <summary>The voice lines by slot (editable, new rows allowed).</summary>
    public const string LinesSheet = "Lines";

    /// <summary>When each narrative event fires (read-only).</summary>
    public const string TriggersSheet = "Triggers";

    /// <summary>The drop-downs' values (hidden).</summary>
    public const string ListsSheet = "Lists";

    /// <summary>The published fault canon's view (the document design spec, D9): every fault a document can carry, read from the agencyFaults table.</summary>
    public const string FaultsSheet = "Faults";

    /// <summary>The Faults sheet's columns.</summary>
    public static readonly string[] FaultHeaders = { "document", "field", "fault", "variant", "proved against", "what the forger did", "id" };

    /// <summary>The ref of a Narrative row shown read-only (a text authored outside world_source.json): the import skips it.</summary>
    public const string ReadOnlyRef = "(read-only)";

    /// <summary>The value of the Lines sheet's remove column that removes a line.</summary>
    public const string RemoveMark = "remove";

    /// <summary>The Narrative sheet's columns; only "text" is read back.</summary>
    public static readonly string[] NarrativeHeaders = { "narrative", "who", "when", "part", "field", "speaker", "text", "notes", "ref", "was" };

    /// <summary>The Lines sheet's columns: the slot, the voice, the content columns a voice row may have (read back where the slot's sheet has them), remove, then the pool and the binding.</summary>
    public static readonly string[] LinesHeaders = { "slot", "voice", "personality", "premade", "kind", "kinds", "era", "request", "variant", "question", "verdict", "intent", "reason", "lie", "reply", "outcome", "text", "then", "remove", "pool", "ref", "was" };

    /// <summary>The Lines sheet's columns that name content columns (read back).</summary>
    public static readonly string[] LineContentHeaders = { "personality", "premade", "kind", "kinds", "era", "request", "variant", "question", "verdict", "intent", "reason", "lie", "reply", "outcome", "text", "then" };

    /// <summary>The voice slots: the content sheets the Lines sheet shows, the personalities' and premades' first, then the defaults.</summary>
    public static readonly string[] LineSlots = VoiceSlots().Concat(new[] { "claims", "kindSmallTalk", "missingFormReplies", "interviewReactions", "interviewSlips", "waiverPadReplies", "confrontReplies" }).ToArray();

    /// <summary>The voice slots a personality or a premade speaks in (interview.voices; the rest of <see cref="LineSlots"/> are the defaults).</summary>
    private static string[] VoiceSlots() => new[] { "voiceClaims", "voiceHandOver", "voiceMissingForms", "voiceSpoken", "voiceAnswers", "voiceSmallTalk", "voiceReactions", "voiceSlips", "voiceWaiverPad", "voiceConfront" };

    /// <summary>The Cases sheet's columns.</summary>
    public static readonly string[] CaseHeaders =
    {
        "seed", "day", "slot", "source", "appearance", "premade", "name", "kind", "personality", "claims to be from", "really from", "lie", "fault",
        "correct call", "intro", "claim", "small talk", "slip", "answers", "papers", "spoken", "if accepted", "if denied", "dialogs"
    };

    /// <summary>The Days sheet's columns.</summary>
    public static readonly string[] DayHeaders =
    {
        "day", "queue", "authored slots", "generated slots", "premade pool", "kinds", "eras", "countries", "lies", "rules and closures", "portals",
        "questions from today", "dialogs from today", "story beats from tonight", "mail", "reference run"
    };

    /// <summary>The Triggers sheet's columns.</summary>
    public static readonly string[] TriggerHeaders = { "kind", "id", "narrative", "day", "conditions", "depends on", "does", "reference run" };

    /// <summary>The condition columns shown per condition (a column absent from a sheet is skipped).</summary>
    private static readonly string[] ConditionFields = { "type", "key", "threshold", "place", "attribute", "nation" };

    /// <summary>How many blank rows below the Lines data keep the drop-downs (new lines are typed there).</summary>
    private const int NewLineRows = 500;

    /// <summary>
    /// The workbook for today's content tables (<paramref name="content"/>: every table of
    /// <see cref="ContentSheets.Export"/> for <paramref name="map"/>) and the editor's
    /// reference run and effects (<paramref name="context"/>; null: none).
    /// </summary>
    public static List<RowTable> Build(SheetSpec map, IReadOnlyList<RowTable> content, NarrativeContext context)
    {
        context = context ?? new NarrativeContext();
        var book = new Book(map, content, context);
        var lists = new Lists();
        RowTable narrative = BuildNarrative(book, lists, context);
        RowTable lines = BuildLines(book, lists);
        RowTable days = BuildDays(book, context);
        RowTable cases = BuildCases(context);
        RowTable triggers = BuildTriggers(book, context);
        RowTable faults = BuildFaults(book);
        RowTable readme = BuildReadme(context, new[] { days, cases, narrative, lines, triggers, faults });
        return new List<RowTable> { readme, days, cases, narrative, lines, triggers, faults, lists.Table() };
    }

    // =====================================================================
    // Narrative
    // =====================================================================

    private static RowTable BuildNarrative(Book b, Lists lists, NarrativeContext context)
    {
        var sheet = new SheetWriter(NarrativeSheet, NarrativeHeaders, lists);
        var premades = b.Rows("premades").Select(r => b.Get("premades", r, "id")).Where(id => id.Length > 0).ToList();
        var forced = b.Rows("dayForced").ToList();

        // Story characters by first appearance, then the rest in the table's order.
        List<string> order = premades
            .Select((id, i) => (id, i, first: forced.Where(r => b.Get("dayForced", r, "premade") == id).Select(r => Int(b.Get("dayForced", r, "day")) * 100 + Int(b.Get("dayForced", r, "slot"))).DefaultIfEmpty(int.MaxValue).Min()))
            .OrderBy(x => x.first).ThenBy(x => x.i).Select(x => x.id).ToList();

        var placedDialogs = new HashSet<string>();
        foreach (string id in order)
            AddPremade(b, sheet, context, id, placedDialogs);

        foreach (int r in b.Rows("dialogs"))
        {
            string d = b.Get("dialogs", r, "id");
            if (placedDialogs.Contains(d) || b.DialogOwner(d) != null)
                continue;
            sheet.Section(d, b.Get("dialogs", r, "label"), "dialog · " + b.Gate("dialogConditions", "dialog", d, "offered to every traveller"), "a dialog no story character owns");
            AddDialog(b, sheet, context, d, b.Get("dialogs", r, "label"), placedDialogs);
        }

        List<int> unowned = forced.Where(r => b.Get("dayForced", r, "premade").Length == 0).ToList();
        if (unowned.Count > 0)
        {
            sheet.Section("(forced slots)", "slots without a premade", "authored slots of a blueprint", "a blueprint stands in the slot");
            foreach (int r in unowned)
                AddAppearance(b, sheet, "(forced slots)", "slots without a premade", r);
        }

        AddStrandings(b, sheet, context);
        AddMail(b, sheet);
        AddPaper(b, sheet);

        foreach (int r in b.Rows("historyRules"))
        {
            string rule = b.Get("historyRules", r, "id");
            if (b.RuleOwner(rule) != null)
                continue;
            sheet.Section(rule, b.Get("historyRules", r, "name"), "story beat · " + b.Night("historyConditions", "rule", rule),
                b.Rows("historyEdits").Any(e => b.Get("historyEdits", e, "rule") == rule) ? "a history rule: it rewrites a place's fact" : "a story beat: its news line");
            AddBeat(b, sheet, rule, b.Get("historyRules", r, "name"), r);
        }

        RowTable table = sheet.Table();
        table.Look.Columns = NarrativeHeaders.Select(h => h == "text" ? CellLook.Editable : h == "ref" || h == "was" ? CellLook.Binding : CellLook.Locked).ToArray();
        table.Look.Widths = new[] { 14, 18, 34, 26, 16, 12, 70, 34, 18, 0 };
        table.Look.HiddenColumns = NarrativeHeaders.Select(h => h == "was").ToArray();
        table.Look.FreezeColumns = 2;
        table.Look.TabColor = "FFFFC000";
        return table;
    }

    private static void AddPremade(Book b, SheetWriter sheet, NarrativeContext context, string id, HashSet<string> placedDialogs)
    {
        int p = b.Rows("premades").First(r => b.Get("premades", r, "id") == id);
        string who = b.Get("premades", p, "name");
        List<int> appearances = b.Rows("dayForced").Where(r => b.Get("dayForced", r, "premade") == id)
            .OrderBy(r => Int(b.Get("dayForced", r, "day"))).ThenBy(r => Int(b.Get("dayForced", r, "slot"))).ToList();
        List<int> poolDays = b.Rows("days").Where(r => Items(b.Get("days", r, "premades")).Contains(id)).Select(r => Int(b.Get("days", r, "day"))).ToList();

        var when = new List<string>();
        if (appearances.Count > 0)
            when.Add("appears " + string.Join(", ", appearances.Select(r => $"day {b.Get("dayForced", r, "day")} slot {b.Get("dayForced", r, "slot")}" + (b.ForcedGate(r).Length > 0 ? " if " + b.ForcedGate(r) : string.Empty))));
        if (poolDays.Count > 0)
            when.Add("in the premade pool on days " + Spans(poolDays));
        string kind = b.Get("premades", p, "kind");
        string notes = $"{(kind.Length > 0 ? kind + " (2150 story character)" : "famous (displaced)")} · {b.Get("premades", p, "place")}" +
                       (b.Get("premades", p, "truePlace").Length > 0 ? $" · really {b.Get("premades", p, "truePlace")}" : string.Empty) +
                       (b.Get("premades", p, "repeatable") == "true" ? " · repeatable" : " · once per run");
        sheet.Section(id, who, when.Count > 0 ? string.Join("; ", when) : "never scheduled (no pool, no forced slot)", notes);

        foreach ((string field, string note) in new[] { ("name", "the name on their papers"), ("intro", "the desk's opener for them (blank: the interview's)"), ("recordNote", "the note on their record"), ("dialog", "the dialog offered while they are at the desk (blank: none)") })
            sheet.Field(b, id, who, "at the desk", "premade", "premades", p, field, string.Empty, note);

        foreach (int r in appearances)
            AddAppearance(b, sheet, id, who, r);

        var dialogs = new List<string> { b.Get("premades", p, "dialog") };
        dialogs.AddRange(appearances.Select(r => b.Get("dayForced", r, "dialog")));
        dialogs.AddRange(b.Rows("dialogs").Select(r => b.Get("dialogs", r, "id")).Where(d => b.DialogOwner(d) == id));
        foreach (string d in dialogs.Where(d => d.Length > 0).Distinct())
            AddDialog(b, sheet, context, d, who, placedDialogs);

        foreach (string slot in VoiceSlots())
            foreach (int r in b.Rows(slot).Where(r => b.Get(slot, r, "premade") == id))
            {
                string part = "voice: " + SlotLabel(b, slot, r);
                sheet.Field(b, id, who, SlotWhen(b, slot, r), part, slot, r, "text", string.Empty, VoiceNote(b, slot, r));
                if (b.Has(slot, "then"))
                    sheet.Field(b, id, who, SlotWhen(b, slot, r), part, slot, r, "then", string.Empty, "an optional second line");
            }

        foreach (int r in b.Rows("historyRules").Where(r => b.RuleOwner(b.Get("historyRules", r, "id")) == id))
            AddBeat(b, sheet, id, who, r);

        int pull = 0;
        foreach (int r in b.Rows("premadePulls").Where(r => b.Get("premadePulls", r, "premade") == id))
        {
            pull++;
            foreach (string field in new[] { "factor", "outcome", "amount" })
                sheet.Field(b, id, who, "when stamped Accepted", "world pull", "premadePulls", r, field, string.Empty, field == "amount" ? "the pull, instead of the role's" : string.Empty, $"pull {pull} · {field}");
        }
    }

    /// <summary>
    /// The strandings (the endings and strandings spec): the desk's waiver pad, each
    /// fate's weights, report line and paper lines, the agency's failure report in Mail,
    /// the paper's fallback line, and the texts authored outside world_source.json (the
    /// waiver form's fine print) as read-only rows naming where they are edited.
    /// </summary>
    private static void AddStrandings(Book b, SheetWriter sheet, NarrativeContext context)
    {
        const string id = "strandings", who = "Strandings";
        if (!b.Has("agencyStrandingFates", "id") && !b.Has("waiverPad", "label"))
            return;
        string chance = b.Rows("agency").Select(r => b.Get("agency", r, "strandChance")).FirstOrDefault() ?? string.Empty;
        sheet.Section(id, who, "an accepted traveller on an Economy transponder may be stranded at the shift's end" + (chance.Length > 0 ? $" ({Percent(chance)})" : string.Empty),
            "the waiver pad, the fates, the failure report; the pad answers of the personalities are in Lines (voiceWaiverPad, waiverPadReplies)");
        foreach (int r in b.Rows("waiverPad"))
        {
            sheet.Field(b, id, who, "a day whose papers menu holds the Stranding Waiver", "waiver pad", "waiverPad", r, "label", "Desk", "the entry every traveller of the day is offered");
            sheet.Field(b, id, who, "the clerk picks the pad", "waiver pad", "waiverPad", r, "prompt", "Desk", "the desk's words as a blank slides across");
        }
        foreach (NarrativeNote note in context.Notes.Where(n => n.Narrative == id))
            sheet.Note(id, who, note);
        foreach (int r in b.Rows("agencyStrandingFates"))
        {
            string fate = b.Get("agencyStrandingFates", r, "id");
            string part = $"fate {fate} ({b.Get("agencyStrandingFates", r, "fate")})";
            const string when = "a stranding: one fate, drawn by the waiver's weights";
            sheet.Field(b, id, who, when, part, "agencyStrandingFates", r, "weightWaivered", string.Empty, "its weight with a valid signed waiver (never shown)");
            sheet.Field(b, id, who, when, part, "agencyStrandingFates", r, "weightUnwaivered", string.Empty, "its weight without one");
            if (b.Get("agencyStrandingFates", r, "stability").Length > 0)
                sheet.Field(b, id, who, when, part, "agencyStrandingFates", r, "stability", string.Empty, "a tremor's stability loss, a percent");
            sheet.Field(b, id, who, when, part, "agencyStrandingFates", r, "status", string.Empty, "the failure report's last line");
            foreach (int l in b.Rows("strandingFateLines").Where(l => b.Get("strandingFateLines", l, "strandingFate") == fate))
                sheet.Field(b, id, who, "the next morning's paper" + (b.Get("strandingFateLines", l, "era").Length > 0 ? $" (era {b.Get("strandingFateLines", l, "era")})" : string.Empty),
                    part, "strandingFateLines", l, "text", string.Empty, "{place} required, {name} optional");
        }
        foreach (int r in b.Rows("agencyStrandingReport"))
            foreach ((string field, string note) in new[] { ("unit", "{unit}, {place}"), ("traveller", "{name}, {id}"), ("waivered", "a valid signed waiver on file: {waiver}, {debt}"), ("unwaivered", "no valid signed waiver on file"), ("fine", "the stranding fine: {fine}") })
                sheet.Field(b, id, who, "Mail, the morning after every stranding", "failure report", "agencyStrandingReport", r, field, string.Empty, note);
        foreach (int r in b.Rows("news"))
            sheet.Field(b, id, who, "the morning paper, per stranding without a fate line", "paper", "news", r, "stranded", string.Empty, "{name} and {place}");
    }

    /// <summary>Mail's authored messages: each one's sender, subject and paragraphs.</summary>
    private static void AddMail(Book b, SheetWriter sheet)
    {
        const string id = "mail", who = "Mail";
        List<int> mail = b.Rows("pcMail").ToList();
        if (mail.Count == 0)
            return;
        sheet.Section(id, who, "the PC's Mail: each message arrives on its day (and after its flag)", "the stranding failure report is under Strandings");
        foreach (int r in mail)
        {
            string m = b.Get("pcMail", r, "id"), flag = b.Get("pcMail", r, "flag"), until = b.Get("pcMail", r, "untilDay");
            string when = $"day {b.Get("pcMail", r, "fromDay")}" + (flag.Length > 0 ? " if " + b.Flag(flag) : string.Empty) + (until.Length > 0 && until != "0" ? $", until day {until}" : string.Empty);
            string part = "mail " + m;
            sheet.Field(b, id, who, when, part, "pcMail", r, "from", string.Empty, string.Empty);
            sheet.Field(b, id, who, when, part, "pcMail", r, "subject", string.Empty, string.Empty);
            int n = 0;
            foreach (int body in b.Rows("pcMailBody").Where(x => b.Get("pcMailBody", x, "mail") == m))
                sheet.Field(b, id, who, when, part, "pcMailBody", body, "text", string.Empty, string.Empty, $"paragraph {++n}");
        }
    }

    /// <summary>The morning paper's authored lines (the history templates and the debt lines) and the radio at home.</summary>
    private static void AddPaper(Book b, SheetWriter sheet)
    {
        const string id = "paper", who = "The paper and the radio";
        sheet.Section(id, who, "every morning's paper, and the radio at home each night", "the story beats' own news lines are in their blocks");
        foreach (int r in b.Rows("history"))
            foreach ((string field, string note) in new[] { ("lines.leaderGained", "a nation takes the lead"), ("lines.leaderLost", "a nation loses the lead"), ("lines.carry", "a carry changed a place"), ("lines.dominant", "{attribute} and {place}"), ("lines.panic", "{place} and {value}") })
                sheet.Field(b, id, who, "the morning after", "history lines", "history", r, field, string.Empty, note, field.Substring("lines.".Length));
        foreach (int r in b.Rows("news"))
            sheet.Field(b, id, who, "the morning after a shift with Debt Relief departures", "debt lines", "news", r, "debtReliefCount", string.Empty, "{count}");
        int n = 0;
        foreach (int r in b.Rows("newsDebt"))
            sheet.Field(b, id, who, "one a morning, in a shuffled order per run", "debt lines", "newsDebt", r, "text", string.Empty, string.Empty, $"debt line {++n}");
        n = 0;
        foreach (int r in b.Rows("homeRadio"))
            sheet.Field(b, id, who, "one a night at home, in order, once the radio is owned", "radio", "homeRadio", r, "text", string.Empty, string.Empty, $"night {++n}");
    }

    private static void AddAppearance(Book b, SheetWriter sheet, string narrative, string who, int r)
    {
        string day = b.Get("dayForced", r, "day"), slot = b.Get("dayForced", r, "slot"), id = b.Get("dayForced", r, "id");
        string gate = b.ForcedGate(r);
        string when = $"day {day}, slot {slot}" + (gate.Length > 0 ? " · if " + gate : string.Empty);
        string part = "appearance " + (id.Length > 0 ? id : $"day {day} slot {slot}");
        sheet.Field(b, narrative, who, when, part, "dayForced", r, "intro", string.Empty, "the desk's opener (blank: the premade's, else the interview's)");
        sheet.Field(b, narrative, who, when, part, "dayForced", r, "dialog", string.Empty, "replaces the premade's dialog (blank: the premade's)");
        sheet.Field(b, narrative, who, when, part, "dayForced", r, "lie", string.Empty, "an authored lie (blank: none)");
        sheet.Field(b, narrative, who, when, part, "dayForced", r, "directive", string.Empty, "an authored directive fault (blank: none)");
        string forcedKey = slot + id;
        AddConditions(b, sheet, narrative, who, when, part, "dayForcedConditions", b.Rows("dayForcedConditions").Where(c => b.Get("dayForcedConditions", c, "day") == day && b.Get("dayForcedConditions", c, "forced") == forcedKey));
    }

    private static void AddDialog(Book b, SheetWriter sheet, NarrativeContext context, string d, string who, HashSet<string> placedDialogs)
    {
        int row = Find(b.Rows("dialogs"), r => b.Get("dialogs", r, "id") == d);
        if (row < 0)
            return;
        placedDialogs.Add(d);
        string owner = b.DialogOwner(d);
        string narrative = owner ?? d;
        string label = b.Get("dialogs", row, "label");
        string offered = (owner != null ? "offered while they are at the desk" : "offered to every traveller") + (b.Get("dialogs", row, "repeatable") == "true" ? string.Empty : ", once");
        string gate = b.Gate("dialogConditions", "dialog", d, string.Empty);
        string whenDialog = offered + (gate.Length > 0 ? " · if " + gate : string.Empty);
        string part = "dialog " + d;
        sheet.Field(b, narrative, who, whenDialog, part, "dialogs", row, "label", string.Empty, "the wheel entry");
        AddConditions(b, sheet, narrative, who, whenDialog, part, "dialogConditions", b.Rows("dialogConditions").Where(c => b.Get("dialogConditions", c, "dialog") == d));

        List<int> nodes = b.Rows("dialogNodes").Where(n => b.Get("dialogNodes", n, "dialog") == d).ToList();
        for (int i = 0; i < nodes.Count; i++)
        {
            string node = b.Get("dialogNodes", nodes[i], "id");
            List<string> incoming = b.Rows("dialogChoices").Where(c => b.Get("dialogChoices", c, "dialog") == d && b.Get("dialogChoices", c, "next") == node)
                .Select(c => $"'{b.Get("dialogChoices", c, "label")}'").ToList();
            string when = i == 0 ? $"the clerk picks '{label}'" : incoming.Count > 0 ? "after " + string.Join(" or ", incoming) : "no choice leads here";
            string nodePart = $"{d} / {node}";
            foreach (int l in b.Rows("dialogLines").Where(l => b.Get("dialogLines", l, "dialog") == d && b.Get("dialogLines", l, "node") == node))
                sheet.Field(b, narrative, who, when, nodePart, "dialogLines", l, "text", Speaker(b, "dialogLines", l), string.Empty);

            foreach (int c in b.Rows("dialogChoices").Where(c => b.Get("dialogChoices", c, "dialog") == d && b.Get("dialogChoices", c, "node") == node))
            {
                string choice = b.Get("dialogChoices", c, "id");
                string choicePart = $"{nodePart} / {choice}";
                string choiceWhen = $"a choice at '{node}'";
                sheet.Field(b, narrative, who, choiceWhen, choicePart, "dialogChoices", c, "label", "Desk", "the clerk's choice, said aloud");
                foreach (int l in b.Rows("choiceLines").Where(l => b.Get("choiceLines", l, "dialog") == d && b.Get("choiceLines", l, "node") == node && b.Get("choiceLines", l, "choice") == choice))
                    sheet.Field(b, narrative, who, $"after '{b.Get("dialogChoices", c, "label")}'", choicePart, "choiceLines", l, "text", Speaker(b, "choiceLines", l), string.Empty);
                sheet.Field(b, narrative, who, choiceWhen, choicePart, "dialogChoices", c, "next", string.Empty, "the node it leads to (blank: the dialog ends)");
                string effect = b.Get("dialogChoices", c, "effect");
                sheet.Field(b, narrative, who, choiceWhen, choicePart, "dialogChoices", c, "effect", string.Empty,
                    effect.Length == 0 ? "applied at the shift's end when the dialog ends here (blank: none)"
                    : context.Effects.TryGetValue(effect, out string does) ? does : "an effect asset the generated library does not list");
            }
        }
    }

    private static void AddBeat(Book b, SheetWriter sheet, string narrative, string who, int r)
    {
        string rule = b.Get("historyRules", r, "id");
        string when = "the night " + b.Night("historyConditions", "rule", rule);
        string part = "story beat " + rule;
        sheet.Field(b, narrative, who, when, part, "historyRules", r, "name", string.Empty, "its name (for authors and the report)");
        sheet.Field(b, narrative, who, when, part, "historyRules", r, "news", string.Empty, "the morning paper's line");
        sheet.Field(b, narrative, who, when, part, "historyRules", r, "section", string.Empty, "News (blank), Desk, or Return: held until the character returns");
        sheet.Field(b, narrative, who, when, part, "historyRules", r, "stability", string.Empty, "the stability change that night, a percent (-3 takes 3%; blank: none)");
        AddConditions(b, sheet, narrative, who, when, part, "historyConditions", b.Rows("historyConditions").Where(c => b.Get("historyConditions", c, "rule") == rule));
        foreach (int e in b.Rows("historyEdits").Where(e => b.Get("historyEdits", e, "rule") == rule))
            sheet.Field(b, narrative, who, when, part, "historyEdits", e, "value", string.Empty, "the place's fact becomes this", $"edit · {b.Get("historyEdits", e, "place")} {b.Get("historyEdits", e, "category")}");
        int pull = 0;
        foreach (int pr in b.Rows("historyPulls").Where(x => b.Get("historyPulls", x, "rule") == rule))
        {
            pull++;
            foreach (string field in new[] { "factor", "outcome", "amount" })
                sheet.Field(b, narrative, who, when, part, "historyPulls", pr, field, string.Empty, field == "amount" ? "the pull the night it fires" : string.Empty, $"pull {pull} · {field}");
        }
    }

    /// <summary>One row per condition field that matters (the type always; a key or threshold where the type reads it or it is set).</summary>
    private static void AddConditions(Book b, SheetWriter sheet, string narrative, string who, string when, string part, string conditions, IEnumerable<int> rows)
    {
        int n = 0;
        foreach (int r in rows)
        {
            n++;
            string type = b.Get(conditions, r, "type");
            foreach (string field in ConditionFields.Where(f => b.Has(conditions, f)))
            {
                string value = b.Get(conditions, r, field);
                bool shown = field == "type"
                             || (field == "key" && (value.Length > 0 || UsesKey(type)))
                             || (field == "threshold" && (UsesThreshold(type) || (value.Length > 0 && value != "0")))
                             || (field != "key" && field != "threshold" && value.Length > 0);
                if (shown)
                    sheet.Field(b, narrative, who, when, part, conditions, r, field, string.Empty, field == "type" ? "all conditions must pass" : string.Empty, $"if {n} · {field}");
            }
        }
    }

    // =====================================================================
    // Lines
    // =====================================================================

    private static RowTable BuildLines(Book b, Lists lists)
    {
        var sheet = new SheetWriter(LinesSheet, LinesHeaders, lists);
        foreach (string slot in LineSlots)
        {
            List<int> rows = b.Rows(slot).Where(r => b.Get(slot, r, "premade").Length == 0).ToList();
            var pools = rows.GroupBy(r => PoolKey(b, slot, r)).ToDictionary(g => g.Key, g => g.ToList());
            foreach (int r in rows)
            {
                List<int> pool = pools[PoolKey(b, slot, r)];
                string personality = b.Get(slot, r, "personality");
                string voice = personality.Length > 0 ? b.PersonalityName(personality) : "default";
                var cells = new string[LinesHeaders.Length];
                cells[0] = slot;
                cells[1] = voice;
                for (int c = 2; c < LinesHeaders.Length; c++)
                    cells[c] = string.Empty;
                foreach (string h in LineContentHeaders)
                    cells[Array.IndexOf(LinesHeaders, h)] = b.Get(slot, r, h);
                cells[Array.IndexOf(LinesHeaders, "pool")] = $"{pool.IndexOf(r) + 1} of {pool.Count}";
                cells[Array.IndexOf(LinesHeaders, "ref")] = Ref(slot, r);
                cells[Array.IndexOf(LinesHeaders, "was")] = b.Json(slot, r);
                int at = sheet.Add(cells);
                sheet.Look.Cells[(at, 0)] = CellLook.Locked;
                foreach (string h in LineContentHeaders.Where(h => !b.Has(slot, h)))
                    sheet.Look.Cells[(at, Array.IndexOf(LinesHeaders, h))] = CellLook.NotApplicable;
            }
        }

        RowTable table = sheet.Table();
        int last = table.Rows.Count + NewLineRows;
        void Column(string header, string source)
        {
            if (source != null)
                sheet.Rule(source).AddColumn(Array.IndexOf(LinesHeaders, header), 0, last);
        }
        Column("slot", lists.Source("slot", LineSlots));
        Column("personality", lists.Source("personalities", b.Keys("personalities")));
        Column("premade", lists.Source("premades", b.Keys("premades")));
        Column("kind", lists.Source("kind", Enum.GetNames(typeof(TravellerKind))));
        Column("era", lists.Source("eras", b.Keys("eras")));
        Column("variant", lists.Source("variant", Enum.GetNames(typeof(MissingFormVariant))));
        Column("question", lists.Source("questions", b.Keys("questions")));
        Column("verdict", lists.Source("verdict", Enum.GetNames(typeof(ReactionVerdict))));
        Column("intent", lists.Source("intent", Enum.GetNames(typeof(ReactionIntent))));
        Column("reason", lists.Source("reason", Faults.Reasons));
        Column("lie", lists.Source("lie", Enum.GetNames(typeof(LieKind))));
        Column("reply", lists.Source("reply", Enum.GetNames(typeof(WaiverPadReply))));
        Column("outcome", lists.Source("outcome", Enum.GetNames(typeof(ConfrontOutcome))));
        Column("remove", lists.Source("remove", new[] { RemoveMark }));

        table.Look.Columns = LinesHeaders.Select(h => h == "slot" || h == "remove" || LineContentHeaders.Contains(h) ? CellLook.Editable : h == "ref" || h == "was" ? CellLook.Binding : CellLook.Locked).ToArray();
        table.Look.Widths = new[] { 18, 12, 12, 10, 12, 14, 10, 12, 9, 12, 9, 8, 10, 14, 12, 11, 70, 40, 9, 8, 18, 0 };
        table.Look.HiddenColumns = LinesHeaders.Select(h => h == "was").ToArray();
        table.Look.FreezeColumns = 2;
        table.Look.TabColor = "FFFFC000";
        return table;
    }

    /// <summary>The rows one line is picked among: the same slot, voice and keys.</summary>
    private static string PoolKey(Book b, string slot, int r) =>
        slot + "\u001f" + string.Join("\u001f", new[] { "personality", "kind", "request", "variant", "question", "verdict", "intent", "reason", "lie", "reply", "outcome" }.Select(h => b.Get(slot, r, h)));

    // =====================================================================
    // Days, Cases, Triggers
    // =====================================================================

    private static RowTable BuildDays(Book b, NarrativeContext context)
    {
        var t = new RowTable(DaysSheet, DayHeaders) { Kinds = DayHeaders.Select(h => h == "day" || h == "queue" || h == "generated slots" ? CellKind.Number : CellKind.Text).ToArray() };
        foreach (int r in b.Rows("days"))
        {
            string day = b.Get("days", r, "day");
            int d = Int(day);
            List<int> forced = b.Rows("dayForced").Where(f => b.Get("dayForced", f, "day") == day).ToList();
            var slots = forced.GroupBy(f => Int(b.Get("dayForced", f, "slot"))).OrderBy(g => g.Key).ToList();
            string authored = string.Join("\n", slots.Select(g => $"{g.Key}: " + string.Join(" / else ", g.Select(f => Appearance(b, f)))));
            int queue = Int(b.Get("days", r, "queue"));
            List<string> pool = Items(b.Get("days", r, "premades"));
            string chance = Percent(b.Get("days", r, "premadeChance"));
            t.Add(new[]
            {
                day,
                b.Get("days", r, "queue"),
                authored.Length > 0 ? authored : "none",
                Math.Max(0, queue - slots.Count).ToString(CultureInfo.InvariantCulture),
                pool.Count > 0 ? $"{chance} per generated slot: " + string.Join(", ", pool.Select(b.PremadeName)) : "none",
                string.Join(", ", b.Rows("dayKinds").Where(k => b.Get("dayKinds", k, "day") == day).Select(k => $"{b.Get("dayKinds", k, "kind")} {b.Get("dayKinds", k, "weight")}{(b.Get("dayKinds", k, "honest") == "true" ? " (honest)" : string.Empty)}")),
                string.Join(", ", b.Rows("dayEras").Where(k => b.Get("dayEras", k, "day") == day).Select(k => $"{b.Get("dayEras", k, "era")} {b.Get("dayEras", k, "weight")}")),
                string.Join(", ", Items(b.Get("days", r, "countries"))),
                string.Join(", ", Items(b.Get("days", r, "lies"))),
                string.Join("\n", Items(b.Get("days", r, "rules")).Select(a => b.RuleText(a))),
                string.Join(", ", b.Rows("dayPortals").Where(k => b.Get("dayPortals", k, "day") == day).Select(k => $"{b.Get("dayPortals", k, "portal")}: {b.Get("dayPortals", k, "country")} {b.Get("dayPortals", k, "era")}")),
                string.Join("\n", b.Rows("questions").Where(q => Int(b.Get("questions", q, "fromDay")) == d).Select(q => b.Get("questions", q, "label"))),
                string.Join("\n", b.Rows("dialogs").Where(x => b.DayGate("dialogConditions", "dialog", b.Get("dialogs", x, "id")) == d).Select(x => b.Get("dialogs", x, "id"))),
                string.Join("\n", b.Rows("historyRules").Where(x => BeatDay(b, x, d)).Select(x => b.Get("historyRules", x, "id") + Iff(b.Gate("historyConditions", "rule", b.Get("historyRules", x, "id"), string.Empty, skipDay: true)))),
                string.Join("\n", b.Rows("pcMail").Where(m => Int(b.Get("pcMail", m, "fromDay")) == d).Select(m => b.Get("pcMail", m, "subject") + (b.Get("pcMail", m, "flag").Length > 0 ? $" (if {b.Flag(b.Get("pcMail", m, "flag"))})" : string.Empty))),
                ReferenceDay(b, context, d)
            });
        }
        t.Look = ReadOnlyLook(new[] { 5, 6, 44, 9, 40, 30, 22, 22, 26, 50, 30, 24, 20, 36, 30, 60 }, 1, "FF8EA9DB");
        return t;
    }

    /// <summary>A story beat is new on day <paramref name="d"/>'s night when its day gate is that day, or on day 1 when it has none.</summary>
    private static bool BeatDay(Book b, int rule, int d)
    {
        int gate = b.DayGate("historyConditions", "rule", b.Get("historyRules", rule, "id"));
        return gate == d || (gate == 0 && d == 1);
    }

    private static string ReferenceDay(Book b, NarrativeContext context, int day)
    {
        var parts = new List<string>();
        foreach (IGrouping<int, NarrativeCase> seed in context.Cases.Where(c => c.Day == day).GroupBy(c => c.Seed))
        {
            List<NarrativeCase> cs = seed.ToList();
            string forced = string.Join(", ", cs.Where(c => c.Source == NarrativeCaseSource.Forced).Select(c => $"{c.Slot} {Who(c)}"));
            string pool = string.Join(", ", cs.Where(c => c.Source == NarrativeCaseSource.Pool).Select(c => $"{c.Slot} {Who(c)}"));
            string beats = string.Join(", ", context.Events.Where(e => e.Seed == seed.Key && e.Day == day && e.Kind == NarrativeEvent.BeatFired).Select(e => e.Id));
            parts.Add($"seed {seed.Key}: {cs.Count} travellers, {cs.Count(c => c.Correct == "Deny")} to deny" +
                      (forced.Length > 0 ? $"; forced {forced}" : string.Empty) + (pool.Length > 0 ? $"; pool {pool}" : string.Empty) +
                      (beats.Length > 0 ? $"; that night: {beats}" : string.Empty));
        }
        return string.Join("\n", parts);
    }

    private static string Who(NarrativeCase c) => c.Name.Length > 0 ? c.Name : c.Premade;

    private static RowTable BuildCases(NarrativeContext context)
    {
        var t = new RowTable(CasesSheet, CaseHeaders) { Kinds = CaseHeaders.Select(h => h == "seed" || h == "day" || h == "slot" ? CellKind.Number : CellKind.Text).ToArray() };
        foreach (NarrativeCase c in context.Cases)
            t.Add(new[]
            {
                c.Seed.ToString(CultureInfo.InvariantCulture), c.Day.ToString(CultureInfo.InvariantCulture), c.Slot.ToString(CultureInfo.InvariantCulture),
                c.Source == NarrativeCaseSource.Generated ? "generated" : c.Source == NarrativeCaseSource.Forced ? "authored (forced slot)" : "authored (premade pool)",
                c.Appearance, c.Premade, c.Name, c.Kind, c.Personality, c.Claimed, c.TrueHome, c.Lie, c.Fault, c.Correct,
                c.Intro, c.Claim, c.SmallTalk, c.Slip, c.Answers, c.Papers, c.Spoken, c.IfAccepted, c.IfDenied, c.Dialogs
            });
        t.Look = ReadOnlyLook(new[] { 7, 5, 5, 14, 12, 10, 20, 12, 11, 24, 24, 16, 18, 8, 30, 40, 40, 30, 50, 50, 40, 40, 40, 16 }, 3, "FF8EA9DB");
        return t;
    }

    private static RowTable BuildTriggers(Book b, NarrativeContext context)
    {
        var t = new RowTable(TriggersSheet, TriggerHeaders) { Kinds = TriggerHeaders.Select(h => h == "day" ? CellKind.Number : CellKind.Text).ToArray() };
        string Day(int d) => d > 0 ? d.ToString(CultureInfo.InvariantCulture) : string.Empty;

        foreach (int r in b.Rows("dayForced"))
        {
            string day = b.Get("dayForced", r, "day"), slot = b.Get("dayForced", r, "slot"), id = b.Get("dayForced", r, "id"), premade = b.Get("dayForced", r, "premade");
            string key = slot + id;
            List<int> conditions = b.Rows("dayForcedConditions").Where(c => b.Get("dayForcedConditions", c, "day") == day && b.Get("dayForcedConditions", c, "forced") == key).ToList();
            string seen = string.Join("; ", context.Cases.Where(c => c.Day == Int(day) && c.Slot == Int(slot) && c.Source == NarrativeCaseSource.Forced && (id.Length == 0 || c.Appearance == id))
                .Select(c => $"seed {c.Seed}: {Who(c)}"));
            t.Add(new[]
            {
                "appearance", id.Length > 0 ? id : $"day {day} slot {slot}", premade, day,
                conditions.Count > 0 ? string.Join(" and ", conditions.Select(c => b.Condition("dayForcedConditions", c))) : "always (unless the premade was already met)",
                b.DependsOn("dayForcedConditions", conditions), $"{Appearance(b, r)} stands in slot {slot}", seen
            });
        }

        foreach (string premade in b.Keys("premades"))
        {
            List<int> days = b.Rows("days").Where(r => Items(b.Get("days", r, "premades")).Contains(premade)).Select(r => Int(b.Get("days", r, "day"))).ToList();
            if (days.Count == 0)
                continue;
            int p = b.Rows("premades").First(r => b.Get("premades", r, "id") == premade);
            string seen = string.Join("; ", context.Cases.Where(c => c.Premade == premade && c.Source == NarrativeCaseSource.Pool).Select(c => $"seed {c.Seed}: day {c.Day} slot {c.Slot}"));
            t.Add(new[]
            {
                "premade pool", premade, premade, Day(days.Min()), $"days {Spans(days)}: the day's premade chance per generated slot",
                b.Get("premades", p, "repeatable") == "true" ? "repeatable" : "once per run: never after they came to the desk",
                $"{b.PremadeName(premade)} may be drawn instead of a generated traveller", seen
            });
        }

        foreach (int r in b.Rows("dialogs"))
        {
            string d = b.Get("dialogs", r, "id");
            List<int> conditions = b.Rows("dialogConditions").Where(c => b.Get("dialogConditions", c, "dialog") == d).ToList();
            string owner = b.DialogOwner(d);
            List<string> effects = b.Rows("dialogChoices").Where(c => b.Get("dialogChoices", c, "dialog") == d && b.Get("dialogChoices", c, "effect").Length > 0)
                .Select(c => $"'{b.Get("dialogChoices", c, "label")}' applies {b.Get("dialogChoices", c, "effect")}").ToList();
            var offered = context.Events.Where(e => e.Kind == NarrativeEvent.DialogOffered && e.Id == d).GroupBy(e => e.Seed)
                .Select(g => $"seed {g.Key}: day{(g.Select(e => e.Day).Distinct().Count() > 1 ? "s" : string.Empty)} {Spans(g.Select(e => e.Day).ToList())}");
            t.Add(new[]
            {
                "dialog", d, owner ?? string.Empty, Day(b.DayGate("dialogConditions", "dialog", d)),
                conditions.Count > 0 ? string.Join(" and ", conditions.Select(c => b.Condition("dialogConditions", c))) : "always",
                b.DependsOn("dialogConditions", conditions),
                (owner != null ? $"offered while {b.PremadeName(owner)} is at the desk" : "offered to every traveller") + (b.Get("dialogs", r, "repeatable") == "true" ? string.Empty : $"; once (sets {FlagKeys.DialogDone(d)})") +
                (effects.Count > 0 ? "; " + string.Join("; ", effects) : string.Empty),
                string.Join("; ", offered)
            });
        }

        foreach (int r in b.Rows("historyRules"))
        {
            string rule = b.Get("historyRules", r, "id");
            List<int> conditions = b.Rows("historyConditions").Where(c => b.Get("historyConditions", c, "rule") == rule).ToList();
            var does = new List<string>();
            string section = b.Get("historyRules", r, "section");
            if (b.Get("historyRules", r, "news").Length > 0)
                does.Add((section.Length > 0 ? section : "News") + ": " + b.Get("historyRules", r, "news"));
            if (b.Get("historyRules", r, "stability").Length > 0)
                does.Add($"stability {b.Get("historyRules", r, "stability")}%");
            does.AddRange(b.Rows("historyEdits").Where(e => b.Get("historyEdits", e, "rule") == rule).Select(e => $"{b.Get("historyEdits", e, "place")} {b.Get("historyEdits", e, "category")} becomes {b.Get("historyEdits", e, "value")}"));
            does.AddRange(b.Rows("historyPulls").Where(e => b.Get("historyPulls", e, "rule") == rule).Select(e => $"pulls {b.Get("historyPulls", e, "factor")} toward {b.Get("historyPulls", e, "outcome")} by {b.Get("historyPulls", e, "amount")}"));
            var fired = context.Events.Where(e => e.Kind == NarrativeEvent.BeatFired && e.Id == rule).Select(e => $"seed {e.Seed}: night {e.Day}");
            t.Add(new[]
            {
                "story beat", rule, b.RuleOwner(rule) ?? string.Empty, Day(b.DayGate("historyConditions", "rule", rule)),
                (conditions.Count > 0 ? string.Join(" and ", conditions.Select(c => b.Condition("historyConditions", c))) : "always") + " (checked each night; fires once)",
                b.DependsOn("historyConditions", conditions), string.Join("\n", does), string.Join("; ", fired)
            });
        }

        foreach (int r in b.Rows("questions"))
        {
            string q = b.Get("questions", r, "id");
            List<int> conditions = b.Rows("questionConditions").Where(c => b.Get("questionConditions", c, "question") == q).ToList();
            t.Add(new[]
            {
                "question", q, string.Empty, Day(Int(b.Get("questions", r, "fromDay"))),
                conditions.Count > 0 ? string.Join(" and ", conditions.Select(c => b.Condition("questionConditions", c))) : "from its day",
                b.DependsOn("questionConditions", conditions), $"the desk can ask '{b.Get("questions", r, "label")}'", string.Empty
            });
        }

        foreach (int r in b.Rows("pcMail"))
        {
            string flag = b.Get("pcMail", r, "flag"), until = b.Get("pcMail", r, "untilDay");
            t.Add(new[]
            {
                "mail", b.Get("pcMail", r, "id"), string.Empty, b.Get("pcMail", r, "fromDay"),
                (flag.Length > 0 ? "if " + b.Flag(flag) : "always") + (until.Length > 0 && until != "0" ? $"; until day {until}" : string.Empty),
                flag.Length > 0 ? b.Setter(flag) : string.Empty, $"Mail from {b.Get("pcMail", r, "from")}: {b.Get("pcMail", r, "subject")}", string.Empty
            });
        }

        t.Look = ReadOnlyLook(new[] { 12, 22, 12, 5, 50, 50, 60, 40 }, 2, "FF8EA9DB");
        return t;
    }

    // =====================================================================
    // README
    // =====================================================================

    private static RowTable BuildReadme(NarrativeContext context, IEnumerable<RowTable> sheets)
    {
        var t = new RowTable(ReadmeSheet, new[] { "topic", "how it works" });
        void Add(string topic, string text) => t.Add(new[] { topic, text });
        Add("What this is", "The narrative workbook: every day, every traveller of a reference run, every authored narrative and every voice line of Time Sorter, from Assets/Data/World/world_source.json (the content spreadsheet's own tables, the same source Generate World reads). Time Sorter > Narrative Workbook > Export writes it; Import writes your edits back.");
        Add("Colours", "Yellow cells (amber headers) are yours to edit; the import reads only them. Grey cells are read-only views; blue bands start a narrative; shaded cells do not apply to their row. Locked cells refuse typing (Review > Unprotect Sheet lifts the lock, no password; the import still reads only the yellow columns).");
        foreach (RowTable s in sheets)
            Add(s.Name, SheetGuide(s.Name) + $" ({s.Rows.Count} rows)");
        Add("Edit a line", "Narrative: change the text cell (a line, a choice's label, an effect, a condition's key or threshold, a news line, a pull). Lines: change any yellow cell. Keep the tokens: {place} (the claimed place), {value} (an answer's value, required in answers), {document} (the paper asked for).");
        Add("Add a line", "In Lines, type a new row at the bottom: slot (the voice sheet), personality or premade (exactly one; both blank for a default slot), the slot's keys (request, variant, question, verdict, intent, reason, lie; kind for claims and missingFormReplies), kinds and era (blank: any), text (and then for a reaction). Example: voiceClaims | chatty | | | | | | | | | | | | Off to {place}, and not a moment too soon!");
        Add("Remove a line", "In Lines, choose 'remove' in the remove column. Deleting a row from this workbook deletes nothing: the import ignores rows that are not there.");
        Add("New structure", "New premades, dialogs, nodes, choices, forced slots or beats go into the content spreadsheet (Tools > TimeDesk > Export Content Spreadsheet, the same world_source.json and the same import). Then export this workbook again.");
        Add("Import", "Save the workbook, then Time Sorter > Narrative Workbook > Import. It reads ContentSheets/NarrativeWorkbook.xlsx, writes only the changed yellow cells into world_source.json, checks the result with the content spreadsheet's rules, runs Generate World and the validator, and writes ContentSheets/NarrativeWorkbook_import.txt (every change, every problem). Any problem stops it before anything is written.");
        Add("Conflicts", "Each editable row remembers the content row it came from (ref, and a hidden copy). If that row changed in world_source.json since the export (another change landed), an edit to it is refused as a conflict: export again and redo the edit. Rows you did not edit are skipped quietly.");
        Add("Reference run", context.RunNote.Length > 0 ? context.RunNote : "none (the Cases sheet is empty: export from Unity to fill it)");
        t.Look = ReadOnlyLook(new[] { 18, 140 }, 0, "FF548235");
        return t;
    }

    private static string SheetGuide(string sheet)
    {
        switch (sheet)
        {
            case DaysSheet: return "Read-only. Each day: the queue, the authored slots (forced appearances, their conditions and faults), how many slots are generated, the premade pool, the mix, the rules and closures, what unlocks, the story beats that can fire that night, the mail, and what the reference run met.";
            case CasesSheet: return "Read-only. Every traveller of the reference run, day by day and slot by slot: generated or authored, who they are, their lie or fault, and every line they would say (claim, small talk, slip, answers, papers, spoken requests, the reaction to either stamp), resolved by the game's own code.";
            case NarrativeSheet: return "Editable (the text column). One block per story character, dialog and story beat: its triggers, then every line, choice, effect, condition and pull.";
            case LinesSheet: return "Editable. Every personality line and every default line, by voice slot (the premades' own lines are in Narrative); pool says which of the lines one pick chooses among.";
            case TriggersSheet: return "Read-only. Every appearance, premade pool, dialog, story beat, question and mail: when it fires, the player choices it depends on, what it does, and when the reference run saw it.";
            case FaultsSheet: return "Read-only. The published fault canon (docs/DOCUMENT_FAULTS.md): every fault a document can carry, what proves it, and the lie or directive that puts it there; the case generator draws only from it. Edit it in the content spreadsheet (agencyFaults).";
            default: return string.Empty;
        }
    }

    /// <summary>The Faults sheet (the document design spec, D9): the canon's rows as the content spreadsheet holds them (agencyFaults), a document per row, a whole paper named "(the paper)".</summary>
    private static RowTable BuildFaults(Book b)
    {
        var t = new RowTable(FaultsSheet, FaultHeaders);
        foreach (int r in b.Rows("agencyFaults"))
        {
            string lie = b.Get("agencyFaults", r, "lie"), directive = b.Get("agencyFaults", r, "directive");
            string form = b.Get("agencyFaults", r, "form"), field = b.Get("agencyFaults", r, "field");
            t.Add(new[]
            {
                form == "*" ? "any paper" : form,
                field.Length > 0 ? field : "(the paper)",
                lie.Length > 0 ? lie : directive,
                b.Get("agencyFaults", r, "variant"),
                b.Get("agencyFaults", r, "against"),
                b.Get("agencyFaults", r, "note"),
                b.Get("agencyFaults", r, "id")
            });
        }
        t.Look = ReadOnlyLook(new[] { 12, 16, 22, 14, 34, 60, 22 }, 1, "FF7F7F7F");
        return t;
    }

    // =====================================================================
    // Shared
    // =====================================================================

    /// <summary>A read-only sheet's look: locked, filtered, protected.</summary>
    private static SheetLook ReadOnlyLook(int[] widths, int freeze, string tab) =>
        new SheetLook { Widths = widths, FreezeColumns = freeze, Filter = true, Protect = true, TabColor = tab };

    /// <summary>The binding of a content row: "sheet#row" with the content workbook's row number (data row 0 is row 2).</summary>
    public static string Ref(string sheet, int row) => $"{sheet}#{(row + 2).ToString(CultureInfo.InvariantCulture)}";

    private static string Appearance(Book b, int f)
    {
        string premade = b.Get("dayForced", f, "premade"), blueprint = b.Get("dayForced", f, "blueprint"), id = b.Get("dayForced", f, "id");
        var details = new[] { id, b.Get("dayForced", f, "lie"), b.Get("dayForced", f, "directive"), b.Get("dayForced", f, "dialog") }.Where(x => x.Length > 0).ToList();
        string gate = b.ForcedGate(f);
        return (premade.Length > 0 ? b.PremadeName(premade) : blueprint.Length > 0 ? blueprint : "(empty)") +
               (details.Count > 0 ? $" ({string.Join(" · ", details)})" : string.Empty) + (gate.Length > 0 ? " if " + gate : string.Empty);
    }

    private static string Speaker(Book b, string sheet, int r)
    {
        string expression = b.Get(sheet, r, "expression");
        return b.Get(sheet, r, "speaker") + (expression.Length > 0 ? $" ({expression})" : string.Empty);
    }

    private static string SlotLabel(Book b, string slot, int r)
    {
        string keys = string.Join(" · ", new[] { "request", "variant", "question", "verdict", "intent", "reason", "lie", "reply", "outcome" }.Select(h => b.Get(slot, r, h)).Where(v => v.Length > 0));
        string name;
        switch (slot)
        {
            case "voiceClaims": name = "claim"; break;
            case "voiceHandOver": name = "hand-over"; break;
            case "voiceMissingForms": name = "missing form"; break;
            case "voiceSpoken": name = "spoken request"; break;
            case "voiceAnswers": name = "answer"; break;
            case "voiceSmallTalk": name = "small talk"; break;
            case "voiceReactions": name = "reaction"; break;
            case "voiceSlips": name = "slip"; break;
            case "voiceWaiverPad": name = "waiver pad"; break;
            case "voiceConfront": name = "difference"; break;
            default: name = slot; break;
        }
        return keys.Length > 0 ? $"{name} ({keys})" : name;
    }

    private static string SlotWhen(Book b, string slot, int r)
    {
        switch (slot)
        {
            case "voiceClaims": return "as they step up";
            case "voiceHandOver": return b.Get(slot, r, "request").Length > 0 ? $"handing over {b.Get(slot, r, "request")}" : "handing over any paper";
            case "voiceMissingForms": return $"asked for {b.Get(slot, r, "request")}, which they lack ({b.Get(slot, r, "variant")})";
            case "voiceSpoken": return $"after the request '{b.Get(slot, r, "request")}'";
            case "voiceAnswers": return $"asked the question '{b.Get(slot, r, "question")}'";
            case "voiceSmallTalk": return "small talk";
            case "voiceReactions": return $"stamped {b.Get(slot, r, "verdict")} ({b.Get(slot, r, "intent")}{(b.Get(slot, r, "reason").Length > 0 ? ", " + b.Get(slot, r, "reason") : string.Empty)})";
            case "voiceSlips": return "after small talk, when lying" + (b.Get(slot, r, "lie").Length > 0 ? $" ({b.Get(slot, r, "lie")})" : string.Empty);
            case "voiceWaiverPad": return $"handed the waiver pad: {b.Get(slot, r, "reply")}";
            case "voiceConfront": return $"asked about a difference the clerk logged: {b.Get(slot, r, "outcome")}" + (b.Get(slot, r, "reason").Length > 0 ? $" ({b.Get(slot, r, "reason")})" : string.Empty);
            default: return string.Empty;
        }
    }

    private static string VoiceNote(Book b, string slot, int r)
    {
        string kinds = b.Get(slot, r, "kinds"), era = b.Get(slot, r, "era");
        string tokens = slot == "voiceClaims" ? "{place} required" : slot == "voiceAnswers" ? "{value} required; {place}" : slot == "voiceHandOver" || slot == "voiceMissingForms" ? "{document}, {place}"
                      : slot == "voiceConfront" ? "{value}, {other}, {place}" : "{place}";
        return tokens + (kinds.Length > 0 ? $" · kinds {kinds}" : string.Empty) + (era.Length > 0 ? $" · era {era}" : string.Empty);
    }

    private static bool UsesKey(string type) => Enum.TryParse(type, out TriggerConditionType t) && t != TriggerConditionType.DayAtLeast && t != TriggerConditionType.StabilityAtMost;

    private static bool UsesThreshold(string type) =>
        Enum.TryParse(type, out TriggerConditionType t) && t != TriggerConditionType.FlagSet && t != TriggerConditionType.FlagNotSet && t != TriggerConditionType.UpgradeOwned
        && t != TriggerConditionType.NationIsLeader && t != TriggerConditionType.AttributeIsDominant && t != TriggerConditionType.AttributeIsSupporting;

    private static string Iff(string gate) => gate.Length > 0 ? " (if " + gate + ")" : string.Empty;

    private static string Percent(string chance) =>
        double.TryParse(chance, NumberStyles.Float, CultureInfo.InvariantCulture, out double c) ? (c * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%" : chance;

    /// <summary>The first row of <paramref name="rows"/> that <paramref name="match"/> accepts; -1 for none.</summary>
    private static int Find(IEnumerable<int> rows, Func<int, bool> match)
    {
        foreach (int r in rows)
            if (match(r))
                return r;
        return -1;
    }

    private static int Int(string s) => int.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int v) ? v : 0;

    private static List<string> Items(string cell) => cell.Split('|').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

    /// <summary>Day numbers as runs: "6-9, 11, 13-15".</summary>
    private static string Spans(IReadOnlyCollection<int> days)
    {
        List<int> sorted = days.Distinct().OrderBy(d => d).ToList();
        var parts = new List<string>();
        for (int i = 0; i < sorted.Count; i++)
        {
            int start = sorted[i];
            while (i + 1 < sorted.Count && sorted[i + 1] == sorted[i] + 1)
                i++;
            parts.Add(start == sorted[i] ? start.ToString(CultureInfo.InvariantCulture) : $"{start}-{sorted[i]}");
        }
        return string.Join(", ", parts);
    }

    // =====================================================================
    // The content tables, read by sheet and header
    // =====================================================================

    /// <summary>The content tables by name, with the map's columns (for rules and drop-downs) and the questions the views ask of them.</summary>
    private sealed class Book
    {
        private readonly Dictionary<string, RowTable> _tables = new Dictionary<string, RowTable>();
        private readonly Dictionary<string, SheetSpec> _specs = new Dictionary<string, SheetSpec>();
        private readonly HashSet<string> _premades;

        private readonly NarrativeContext _context;

        public Book(SheetSpec map, IEnumerable<RowTable> tables, NarrativeContext context)
        {
            _context = context;
            foreach (RowTable t in tables ?? Enumerable.Empty<RowTable>())
                if (t != null && !_tables.ContainsKey(t.Name))
                    _tables[t.Name] = t;
            if (map != null)
                Walk(map);
            _premades = new HashSet<string>(Keys("premades"));
        }

        private void Walk(SheetSpec s)
        {
            _specs[s.Name] = s;
            foreach (SheetSpec child in s.Children)
                Walk(child);
        }

        public RowTable Table(string sheet) => _tables.TryGetValue(sheet, out RowTable t) ? t : null;

        public IEnumerable<int> Rows(string sheet) => Enumerable.Range(0, Table(sheet)?.Rows.Count ?? 0);

        public bool Has(string sheet, string header) => Table(sheet)?.Headers.Contains(header) ?? false;

        public string Get(string sheet, int row, string header)
        {
            RowTable t = Table(sheet);
            int c = t?.Headers.IndexOf(header) ?? -1;
            return c < 0 || row < 0 || row >= t.Rows.Count || c >= t.Rows[row].Length ? string.Empty : t.Rows[row][c] ?? string.Empty;
        }

        /// <summary>The row as it was exported: a one-line JSON object of its cells by header (the "was" binding; ContentJson escapes each text).</summary>
        public string Json(string sheet, int row)
        {
            RowTable t = Table(sheet);
            string Quote(string s) => ContentJson.Write(ContentNode.FromString(s ?? string.Empty)).TrimEnd('\n');
            return "{" + string.Join(", ", t.Headers.Select((h, c) => Quote(h) + ": " + Quote(c < t.Rows[row].Length ? t.Rows[row][c] : string.Empty))) + "}";
        }

        public ColumnSpec Column(string sheet, string header)
        {
            if (!_specs.TryGetValue(sheet, out SheetSpec s))
                return null;
            return s.Columns.FirstOrDefault(c => c.Header == header) ?? (s.KeyColumn != null && s.KeyColumn.Header == header ? s.KeyColumn : null);
        }

        /// <summary>The row names of a sheet (its key column, or its key template), as other sheets refer to them.</summary>
        public IEnumerable<string> Keys(string sheet)
        {
            if (!_specs.TryGetValue(sheet, out SheetSpec s))
                return Enumerable.Empty<string>();
            if (s.Shape == SheetShape.Keyed && s.KeyColumn != null)
                return Rows(sheet).Select(r => Get(sheet, r, s.KeyColumn.Header)).Where(k => k.Length > 0).Distinct().ToList();
            if (s.RowKey != null)
                return Rows(sheet).Select(r => s.RowKey.Of(h => Get(sheet, r, h))).Where(k => k.Length > 0).Distinct().ToList();
            return Enumerable.Empty<string>();
        }

        public string PremadeName(string id)
        {
            int r = Find(Rows("premades"), x => Get("premades", x, "id") == id);
            string name = r >= 0 ? Get("premades", r, "name") : string.Empty;
            return name.Length > 0 ? name : id;
        }

        public string PersonalityName(string id)
        {
            int r = Find(Rows("personalities"), x => Get("personalities", x, "id") == id);
            string name = r >= 0 ? Get("personalities", r, "name") : string.Empty;
            return name.Length > 0 ? name : id;
        }

        public string RuleText(string asset)
        {
            int r = Find(Rows("rules"), x => Get("rules", x, "asset") == asset);
            return r < 0 ? asset : $"{asset} ({Get("rules", r, "type")}): {Get("rules", r, "description")}";
        }

        /// <summary>The story character a flag key names: a premade's verdict or meeting, a dialog they own finishing, a story beat they own firing, or any ':'-part that is a premade's id.</summary>
        private string OwnerOfKey(string key, int depth)
        {
            if (string.IsNullOrEmpty(key) || depth > 4)
                return null;
            if (FlagKeys.TryParsePremade(key, out string premade, out _) && _premades.Contains(premade))
                return premade;
            if (key.StartsWith("dlg:", StringComparison.Ordinal) && key.EndsWith(":done", StringComparison.Ordinal))
                return DialogOwner(key.Substring(4, key.Length - 9), depth + 1);
            string beatPrefix = FlagKeys.TriggerFired(FlagKeys.HistoryRuleTriggerId(string.Empty));
            beatPrefix = beatPrefix.Substring(0, beatPrefix.Length - ":fired".Length);
            if (key.StartsWith(beatPrefix, StringComparison.Ordinal) && key.EndsWith(":fired", StringComparison.Ordinal))
                return RuleOwner(key.Substring(beatPrefix.Length, key.Length - beatPrefix.Length - 6), depth + 1);
            return key.Split(':').FirstOrDefault(_premades.Contains);
        }

        /// <summary>The premade a dialog belongs to: named by the premade or one of its appearances, else by its conditions; null for a dialog anyone may be offered.</summary>
        public string DialogOwner(string dialog, int depth = 0)
        {
            if (string.IsNullOrEmpty(dialog))
                return null;
            int p = Find(Rows("premades"), r => Get("premades", r, "dialog") == dialog);
            if (p >= 0)
                return Get("premades", p, "id");
            int f = Find(Rows("dayForced"), r => Get("dayForced", r, "dialog") == dialog && Get("dayForced", r, "premade").Length > 0);
            if (f >= 0)
                return Get("dayForced", f, "premade");
            return Rows("dialogConditions").Where(r => Get("dialogConditions", r, "dialog") == dialog).Select(r => OwnerOfKey(Get("dialogConditions", r, "key"), depth)).FirstOrDefault(o => o != null);
        }

        /// <summary>The premade a story beat belongs to (its conditions name them); null for a world beat.</summary>
        public string RuleOwner(string rule, int depth = 0) =>
            Rows("historyConditions").Where(r => Get("historyConditions", r, "rule") == rule).Select(r => OwnerOfKey(Get("historyConditions", r, "key"), depth)).FirstOrDefault(o => o != null);

        /// <summary>A flag in words.</summary>
        public string Flag(string key)
        {
            if (FlagKeys.TryParsePremade(key, out string premade, out PremadeFlag flag))
                return $"{PremadeName(premade)} {(flag == PremadeFlag.Met ? "came to the desk" : flag == PremadeFlag.Accepted ? "was accepted" : "was denied")}";
            if (key.StartsWith("dlg:", StringComparison.Ordinal) && key.EndsWith(":done", StringComparison.Ordinal))
                return $"dialog {key.Substring(4, key.Length - 9)} was finished";
            if (key.StartsWith("trig:history_", StringComparison.Ordinal) && key.EndsWith(":fired", StringComparison.Ordinal))
                return $"beat {key.Substring(13, key.Length - 19)} fired";
            return $"flag {key}";
        }

        /// <summary>What sets a flag: the clerk's verdict, a dialog ending, a beat, or the dialog choices whose effect sets it.</summary>
        public string Setter(string key)
        {
            if (FlagKeys.TryParsePremade(key, out string premade, out PremadeFlag flag))
                return flag == PremadeFlag.Met ? $"{PremadeName(premade)} coming to the desk" : $"the clerk's stamp on {PremadeName(premade)}";
            if (key.StartsWith("dlg:", StringComparison.Ordinal) && key.EndsWith(":done", StringComparison.Ordinal))
                return $"finishing dialog {key.Substring(4, key.Length - 9)}";
            if (key.StartsWith("trig:history_", StringComparison.Ordinal) && key.EndsWith(":fired", StringComparison.Ordinal))
                return $"story beat {key.Substring(13, key.Length - 19)} (its own conditions)";
            List<string> choices = Rows("dialogChoices").Where(c => EffectSets(Get("dialogChoices", c, "effect"), key))
                .Select(c => $"choosing '{Get("dialogChoices", c, "label")}' in {Get("dialogChoices", c, "dialog")}").ToList();
            return choices.Count > 0 ? string.Join(" or ", choices) : $"{key} (set outside world_source.json)";
        }

        /// <summary>Whether an effect sets a flag (the editor's <see cref="NarrativeContext.EffectFlags"/>).</summary>
        private bool EffectSets(string effect, string flag) =>
            !string.IsNullOrEmpty(effect) && _context.EffectFlags.TryGetValue(effect, out List<string> flags) && flags.Contains(flag);

        /// <summary>The effects a dialog choice may name (the editor's <see cref="NarrativeContext.Effects"/>).</summary>
        public IEnumerable<string> EffectNames => _context.Effects.Keys.OrderBy(k => k, StringComparer.Ordinal);

        public string DependsOn(string conditions, IEnumerable<int> rows)
        {
            var parts = new List<string>();
            foreach (int r in rows)
            {
                string type = Get(conditions, r, "type"), key = Get(conditions, r, "key");
                if (type == nameof(TriggerConditionType.FlagSet))
                    parts.Add(Setter(key));
                else if (type == nameof(TriggerConditionType.FlagNotSet))
                    parts.Add("not " + Setter(key));
                else if (type == nameof(TriggerConditionType.UpgradeOwned))
                    parts.Add($"buying {key}");
            }
            return string.Join("; ", parts.Distinct());
        }

        /// <summary>One condition in words.</summary>
        public string Condition(string conditions, int r)
        {
            string type = Get(conditions, r, "type"), key = Get(conditions, r, "key"), threshold = Get(conditions, r, "threshold");
            string subject = new[] { Get(conditions, r, "attribute"), Get(conditions, r, "nation"), key }.FirstOrDefault(x => x.Length > 0) ?? string.Empty;
            string place = Get(conditions, r, "place");
            string where = place.Length > 0 ? " in " + place : string.Empty;
            switch (type)
            {
                case nameof(TriggerConditionType.FlagSet): return Flag(key);
                case nameof(TriggerConditionType.FlagNotSet): return "not: " + Flag(key);
                case nameof(TriggerConditionType.DayAtLeast): return $"day ≥ {threshold}";
                case nameof(TriggerConditionType.StabilityAtMost): return $"stability ≤ {threshold}";
                case nameof(TriggerConditionType.UpgradeOwned): return $"owns {key}";
                case nameof(TriggerConditionType.NationIsLeader): return $"{subject} leads";
                case nameof(TriggerConditionType.AttributeIsDominant): return $"{subject} dominant{where}";
                case nameof(TriggerConditionType.AttributeIsSupporting): return $"{subject} supporting{where}";
                case nameof(TriggerConditionType.AttributeScoreAtMost):
                case nameof(TriggerConditionType.GlobalAttrAtMost): return $"{subject}{where} ≤ {threshold}";
                default: return $"{subject}{where} ≥ {threshold}";
            }
        }

        /// <summary>A parent row's conditions in words ("" when it has none), <paramref name="none"/> when empty; <paramref name="skipDay"/> leaves the day gate out.</summary>
        public string Gate(string conditions, string parentHeader, string parent, string none, bool skipDay = false)
        {
            List<string> parts = Rows(conditions).Where(r => Get(conditions, r, parentHeader) == parent)
                .Where(r => !skipDay || Get(conditions, r, "type") != nameof(TriggerConditionType.DayAtLeast))
                .Select(r => Condition(conditions, r)).ToList();
            return parts.Count > 0 ? string.Join(" and ", parts) : none;
        }

        /// <summary>A story beat's night in words: "of day 6 or later, if ...", "any night, if ...".</summary>
        public string Night(string conditions, string parentHeader, string parent)
        {
            int day = DayGate(conditions, parentHeader, parent);
            string rest = Gate(conditions, parentHeader, parent, string.Empty, skipDay: true);
            return (day > 0 ? $"of day {day} or later" : "of any day") + (rest.Length > 0 ? ", if " + rest : string.Empty);
        }

        /// <summary>The largest DayAtLeast threshold among a parent row's conditions (0: none).</summary>
        public int DayGate(string conditions, string parentHeader, string parent) =>
            Rows(conditions).Where(r => Get(conditions, r, parentHeader) == parent && Get(conditions, r, "type") == nameof(TriggerConditionType.DayAtLeast))
                .Select(r => (int)Math.Ceiling(double.TryParse(Get(conditions, r, "threshold"), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0))
                .DefaultIfEmpty(0).Max();

        /// <summary>A forced appearance's conditions in words ("" for none).</summary>
        public string ForcedGate(int forced)
        {
            string day = Get("dayForced", forced, "day"), key = Get("dayForced", forced, "slot") + Get("dayForced", forced, "id");
            List<string> parts = Rows("dayForcedConditions").Where(r => Get("dayForcedConditions", r, "day") == day && Get("dayForcedConditions", r, "forced") == key)
                .Select(r => Condition("dayForcedConditions", r)).ToList();
            return string.Join(" and ", parts);
        }
    }

    /// <summary>The hidden Lists sheet: one column per drop-down's values, shared by every rule with the same values.</summary>
    private sealed class Lists
    {
        private readonly List<(string name, List<string> values)> _columns = new List<(string, List<string>)>();
        private readonly Dictionary<string, string> _bySignature = new Dictionary<string, string>();

        /// <summary>The range holding <paramref name="values"/> (added under <paramref name="name"/> the first time); null for no values.</summary>
        public string Source(string name, IEnumerable<string> values)
        {
            List<string> list = (values ?? Enumerable.Empty<string>()).Where(v => !string.IsNullOrEmpty(v)).Distinct().ToList();
            if (list.Count == 0)
                return null;
            string signature = string.Join("\u001f", list);
            if (_bySignature.TryGetValue(signature, out string source))
                return source;
            string letter = RowTable.ColumnLetter(_columns.Count);
            _columns.Add((name, list));
            source = $"{ListsSheet}!${letter}$2:${letter}${list.Count + 1}";
            _bySignature[signature] = source;
            return source;
        }

        public RowTable Table()
        {
            var t = new RowTable(ListsSheet, _columns.Select(c => c.name));
            int rows = _columns.Count == 0 ? 0 : _columns.Max(c => c.values.Count);
            for (int r = 0; r < rows; r++)
                t.Add(_columns.Select(c => r < c.values.Count ? c.values[r] : string.Empty).ToArray());
            t.Look = new SheetLook { Hidden = true, Protect = true };
            return t;
        }
    }

    /// <summary>An editable sheet under construction: its rows, its look and its list rules.</summary>
    private sealed class SheetWriter
    {
        private readonly string _name;
        private readonly string[] _headers;
        private readonly Lists _lists;
        private readonly List<string[]> _rows = new List<string[]>();
        private readonly Dictionary<string, ListRule> _rules = new Dictionary<string, ListRule>();

        public SheetWriter(string name, string[] headers, Lists lists)
        {
            _name = name;
            _headers = headers;
            _lists = lists;
        }

        public SheetLook Look { get; } = new SheetLook { Protect = true, Filter = true };

        public int Add(string[] cells)
        {
            _rows.Add(cells);
            return _rows.Count - 1;
        }

        public ListRule Rule(string source)
        {
            if (!_rules.TryGetValue(source, out ListRule rule))
            {
                rule = new ListRule(source);
                _rules[source] = rule;
                Look.Lists.Add(rule);
            }
            return rule;
        }

        /// <summary>A narrative's banner row (nothing to edit).</summary>
        public void Section(string narrative, string who, string when, string notes)
        {
            int at = Add(Row(narrative, who, when, string.Empty, string.Empty, string.Empty, string.Empty, notes, string.Empty, string.Empty));
            Look.SectionRows.Add(at);
        }

        /// <summary>A read-only Narrative row: a text authored outside world_source.json, with where it is edited (the import never reads it).</summary>
        public void Note(string narrative, string who, NarrativeNote note)
        {
            int at = Add(Row(narrative, who, note.When, note.Part, note.Field, string.Empty, note.Text, "read-only here: edit it in " + note.Where, ReadOnlyRef, string.Empty));
            Look.Cells[(at, Array.IndexOf(NarrativeHeaders, "text"))] = CellLook.NotApplicable;
        }

        /// <summary>A Narrative row bound to one cell of a content table (its text editable), with the column's drop-down when it has one.</summary>
        public void Field(Book b, string narrative, string who, string when, string part, string sheet, int row, string header, string speaker, string notes, string field = null)
        {
            if (!b.Has(sheet, header))
                return;
            string rules = Rules(b, sheet, header);
            string note = string.Join(" · ", new[] { notes, rules }.Where(x => !string.IsNullOrEmpty(x)));
            int at = Add(Row(narrative, who, when, part, field ?? header, speaker, b.Get(sheet, row, header), note, Ref(sheet, row) + ":" + header, b.Json(sheet, row)));
            string source = SourceFor(b, sheet, header, row);
            if (source != null)
                Rule(source).Add(at, Array.IndexOf(NarrativeHeaders, "text"));
        }

        private static string[] Row(params string[] cells) => cells;

        public RowTable Table()
        {
            var t = new RowTable(_name, _headers);
            foreach (string[] r in _rows)
                t.Add(r);
            t.Look = Look;
            return t;
        }

        /// <summary>The column's rules in words, from the map (allowed values are left to the drop-down).</summary>
        private static string Rules(Book b, string sheet, string header)
        {
            ColumnSpec col = b.Column(sheet, header);
            if (col == null)
                return string.Empty;
            var parts = new List<string>();
            if (col.IsRequired)
                parts.Add("required");
            if (col.RefSheet != null)
                parts.Add($"names a row of {col.RefSheet}");
            if (col.Type == CellType.Int || col.Type == CellType.Float || col.Type == CellType.Number)
                parts.Add("a number");
            return string.Join(", ", parts);
        }

        /// <summary>The drop-down of a content cell: the map's allowed values or referred sheet's names, else the game's enums for the headers that hold them; a dialog choice's next lists its dialog's nodes.</summary>
        private string SourceFor(Book b, string sheet, string header, int row)
        {
            if (sheet == "dialogChoices" && header == "next")
            {
                string dialog = b.Get(sheet, row, "dialog");
                return _lists.Source("nodes of " + dialog, b.Rows("dialogNodes").Where(n => b.Get("dialogNodes", n, "dialog") == dialog).Select(n => b.Get("dialogNodes", n, "id")));
            }
            ColumnSpec col = b.Column(sheet, header);
            if (col != null && (col.Type == CellType.TextList || col.Type == CellType.NumberList))
                return null;
            if (col?.Allowed != null)
                return _lists.Source(header, col.Allowed);
            if (col?.RefSheet != null)
                return _lists.Source(col.RefSheet, b.Keys(col.RefSheet));
            switch (header)
            {
                case "speaker": return _lists.Source("speaker", Enum.GetNames(typeof(DialogSpeaker)));
                case "type" when sheet.EndsWith("Conditions", StringComparison.Ordinal): return _lists.Source("condition type", Enum.GetNames(typeof(TriggerConditionType)));
                case "lie": return _lists.Source("lie", Enum.GetNames(typeof(LieKind)));
                case "reason": return _lists.Source("reason", Faults.Reasons);
                case "effect": return _lists.Source("effect", b.EffectNames);
                case "outcome": return _lists.Source("outcome", b.Rows("worldOutcomes").Select(r => b.Get("worldOutcomes", r, "id")));
                default: return null;
            }
        }
    }
}
