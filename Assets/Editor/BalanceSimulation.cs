using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The balance simulation (redesign phase 23; Tools > TimeDesk > Balance > Run
/// 50-Run Simulation): 50 runs of 15 days under each <see cref="PlayStyle"/>,
/// played headless through the game's own steps (<see cref="DayCycle"/>: the
/// run's world, CaseFactory's queue, each decision, the shift's close, Home's
/// bills, the ending checks at the game's moments, the night), with no scene
/// and nothing saved. It reads the knobs where Saleh edits them (the run
/// config, the game config, the blueprints and endings in the Inspector; the
/// day mixes and the agency's chances through the content spreadsheet) and
/// writes a summary (Logs/Balance/balance_summary.txt): each knob's value and
/// where it is edited, the endings, the money, the epilogue thresholds the
/// 50 perfect runs propose, the queue's faults by day and kind, and the
/// authored travellers (forced slots, premades, dialogs) apart from the random draws.
/// </summary>
public static class BalanceSimulation
{
    /// <summary>Runs per play style.</summary>
    public const int Runs = 50;

    /// <summary>Days per run: the run's last day (Retirement and the epilogues are checked at its night).</summary>
    public const int Days = 15;

    /// <summary>The run seed whose every decision is dumped per style, and whose runs are played twice to prove the simulation deterministic.</summary>
    public const int ExampleSeed = 12345;

    /// <summary>
    /// The travellers a careful clerk gets through in a shift before the clock
    /// closes (an estimate: 8 real minutes at about 45 s a traveller). Every
    /// style is played twice, on the whole queue and at this pace (the rest of
    /// the queue goes home, as when the shift clock closes); the epilogue
    /// thresholds are read from perfect play at this pace.
    /// </summary>
    public const int ShiftPace = 10;

    /// <summary>The folder the summary and the example dumps go to (project-relative; Logs/ is not in git).</summary>
    public const string ReportFolder = "Logs/Balance";

    /// <summary>The summary's file name.</summary>
    public const string SummaryFile = "balance_summary.txt";

    private const int SeedStep = 7919;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly PlayStyle[] Styles = { PlayStyle.Perfect, PlayStyle.Imperfect, PlayStyle.Careless };
    private static readonly int[] Paces = { 0, ShiftPace };

    /// <summary>Runs the simulation with today's knobs and shows the summary's file (refused in play mode).</summary>
    [MenuItem("Tools/TimeDesk/Balance/Run 50-Run Simulation")]
    public static void RunMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Balance] Leave play mode first: the simulation plays whole runs of its own.");
            return;
        }

        string summary = Run(ReportFolder);
        if (summary != null)
            EditorUtility.RevealInFinder(summary);
    }

    /// <summary>
    /// Runs the simulation and writes its summary and the example seed's
    /// dumps into <paramref name="folder"/> (project-relative or absolute);
    /// returns the summary's path, or null when the run config, its content
    /// library or its game config is missing. The dev tools' cheats are
    /// cleared before each run and after the last, as a new run clears them.
    /// </summary>
    public static string Run(string folder)
    {
        RunConfigSO run = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        ContentLibrarySO lib = run != null ? run.contentLibrary : null;
        GameConfigSO config = run != null ? run.gameConfig : null;
        if (lib == null || config == null)
        {
            Debug.LogError("[Balance] Resources/RunConfig.asset, its content library or its game config is missing.");
            return null;
        }

        string dir = Path.IsPathRooted(folder) ? folder : Path.Combine(Directory.GetCurrentDirectory(), folder);
        Directory.CreateDirectory(dir);

        var errors = new List<string>();
        void Capture(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                errors.Add(message);
        }

        LogType filter = Debug.unityLogger.filterLogType;
        var summary = new StringBuilder();
        Application.logMessageReceived += Capture;
        Debug.unityLogger.filterLogType = LogType.Error;
        try
        {
            var results = new Dictionary<(PlayStyle, int), List<RunResult>>();
            int step = 0, steps = Styles.Length * (Paces.Length * Runs + 2);
            foreach (PlayStyle style in Styles)
            {
                foreach (int pace in Paces)
                {
                    var runs = new List<RunResult>();
                    for (int i = 1; i <= Runs; i++)
                    {
                        EditorUtility.DisplayProgressBar("Balance simulation", $"{style} play, {PaceLabel(pace)}, run {i} of {Runs}", (float)step++ / steps);
                        runs.Add(Play(run, lib, config, i * SeedStep, style, pace));
                    }
                    results[(style, pace)] = runs;
                }

                EditorUtility.DisplayProgressBar("Balance simulation", $"{style} play, seed {ExampleSeed} twice", (float)step / steps);
                RunResult a = Play(run, lib, config, ExampleSeed, style, 0);
                RunResult b = Play(run, lib, config, ExampleSeed, style, 0);
                step += 2;
                File.WriteAllText(Path.Combine(dir, $"balance_seed{ExampleSeed}_{style.ToString().ToLowerInvariant()}.txt"), a.Dump.ToString());
                if (a.Fingerprint != b.Fingerprint || a.Dump.ToString() != b.Dump.ToString())
                    errors.Add($"{style} play: seed {ExampleSeed} played twice gave two different runs (the simulation is not deterministic).");
            }

            Write(summary, run, lib, config, results, errors);
        }
        finally
        {
            Debug.unityLogger.filterLogType = filter;
            Application.logMessageReceived -= Capture;
            DevToolsState.ResetAll();
            EditorUtility.ClearProgressBar();
        }

        string path = Path.Combine(dir, SummaryFile);
        File.WriteAllText(path, summary.ToString());
        Debug.Log($"[Balance] {Styles.Length} x {Paces.Length} x {Runs} runs of {Days} days: {path}{(errors.Count > 0 ? $" ({errors.Count} error(s): see its last section)" : "")}.");
        return path;
    }

    /// <summary>One run's numbers, its final world and its decisions.</summary>
    private sealed class RunResult
    {
        public int Seed;
        public string Ending = "none";
        public int EndingDay;
        public WorldState World;
        public readonly List<int> MoneyAfterShift = new List<int>();
        public readonly List<int> MoneyAtNight = new List<int>();
        public readonly List<float> StabilityAfterShift = new List<float>();
        public int MinMoney = int.MaxValue;
        public int Cases, Faulty, Accepted, Wrong, Unproven, Pay, Penalties, Instalments, Household, Stranded, Carries;
        public readonly List<CaseRecord> Records = new List<CaseRecord>();
        public readonly List<string> DialogsOffered = new List<string>();
        public readonly StringBuilder Dump = new StringBuilder();
        public string Fingerprint = "";
    }

    /// <summary>One traveller of a run: where they came from (a random draw, a forced slot, a pooled premade) and how they were judged.</summary>
    private sealed class CaseRecord
    {
        public int Day, Slot;
        public string Kind, Source, Premade, Reason;
        public bool Faulty, Deviation, Accepted, Correct, Economy;
    }

    /// <summary>A run of <see cref="Days"/> days from <paramref name="seed"/> under <paramref name="style"/>, through the game's own steps, <paramref name="pace"/> travellers a shift (0: the whole queue).</summary>
    private static RunResult Play(RunConfigSO run, ContentLibrarySO lib, GameConfigSO config, int seed, PlayStyle style, int pace)
    {
        DevToolsState.ResetAll();
        var r = new RunResult { Seed = seed };
        var policy = new PlayPolicy(style);
        WorldState world = DayCycle.NewWorld(run, lib, seed);
        r.World = world;

        while (world.day <= Days)
        {
            int day = world.day;
            DayPlanSO plan = lib.GetDayPlan(day);
            var ledger = new ShiftLedger();
            InterviewDay interview = TimelineService.BuildInterviewDay(lib, world, ledger);
            TodaysWorld today = lib.BuildToday(plan, world.history);
            List<CaseInstance> cases = new CaseFactory(lib, today).GenerateDayCases(plan, world, Seeds.Day(seed, day), interview, true);
            r.DialogsOffered.Add($"day {day}: [{string.Join(", ", interview.OfferedDialogs(null).Select(d => d.id))}]");
            policy.StartDay(day);

            string ended = null;
            int seen = pace > 0 ? Math.Min(pace, cases.Count) : cases.Count;
            for (int i = 0; i < seen && ended == null; i++)
            {
                CaseInstance inst = cases[i];
                PlayDecision decision = policy.Decide(inst.ShouldAccept, inst.HasDeviationFault);
                int carries = world.history.pendingCarries.Count;
                CaseVerdict verdict = DayCycle.Decide(inst, decision.Accept, i + 1, decision.Documented ? 1 : 0, world, today, ledger, lib, config);
                r.Carries += world.history.pendingCarries.Count - carries;
                r.Records.Add(Record(plan, day, i + 1, inst, decision.Accept, verdict.correct));
                Count(r, inst, decision.Accept, verdict);
                r.Dump.AppendLine($"d{day} #{i + 1} {inst.kind} {Source(plan, i + 1, inst)} '{inst.originLabel}' fault='{inst.FaultReason}' accept={decision.Accept} documented={decision.Documented} correct={verdict.correct} pay={verdict.payAwarded} penalty={verdict.moneyPenalty} money={world.money} stability={StabilityRules.Format(world.timelineStability)}");
                EndingSO now = EndingService.Evaluate(world, lib, config, EndingMoment.Immediate);
                if (now != null)
                    ended = now.id;
            }

            if (ended == null && DayCycle.CloseShift(world, ledger, cases, today, lib, config))
                ended = EndingService.Evaluate(world, lib, config, EndingMoment.Immediate)?.id;
            ClerkAccountSource.RecordShift(world, ledger, lib, config);
            r.Pay += ledger.TotalPay;
            r.Penalties += ledger.TotalPenalties;
            r.Instalments += ledger.debtInstalment;
            r.Stranded += ledger.strandedCount;
            r.MoneyAfterShift.Add(world.money);
            r.StabilityAfterShift.Add(world.timelineStability);
            r.MinMoney = Math.Min(r.MinMoney, world.money);
            r.Dump.AppendLine($"shift {day}: pay {ledger.TotalPay} penalties {ledger.TotalPenalties} stranded {ledger.strandedCount} instalment {ledger.debtInstalment} money {world.money}{(ended != null ? " ENDING " + ended : "")}");
            if (ended != null)
            {
                End(r, ended, day);
                break;
            }

            // Home: the bill and the family's drift; the simulation buys nothing, treats no one and spins nothing.
            HomeEconomy.ExpenseReport bill = DayCycle.OpenHome(world, config, Seeds.Day(seed, day));
            ClerkAccountSource.RecordHome(world, bill.total, 0, lib, config);
            r.Household += bill.total;
            r.MoneyAtNight.Add(world.money);
            r.MinMoney = Math.Min(r.MinMoney, world.money);

            EndingSO sleep = EndingService.Evaluate(world, lib, config, EndingMoment.DayBoundary);
            if (sleep != null)
            {
                r.Dump.AppendLine($"night {day}: household {bill.total} money {world.money} ENDING {sleep.id}");
                End(r, sleep.id, day);
                break;
            }
            DayCycle.AdvanceNight(world, lib, config);
            r.Dump.AppendLine($"night {day}: household {bill.total} money {world.money} leader '{world.history.leaderId}'");
        }

        r.Fingerprint = JsonUtility.ToJson(world.history) + JsonUtility.ToJson(world.timeline) + world.money.ToString(Inv) + r.Ending;
        return r;
    }

    private static void End(RunResult r, string ending, int day)
    {
        r.Ending = ending;
        r.EndingDay = day;
    }

    private static void Count(RunResult r, CaseInstance inst, bool accepted, CaseVerdict verdict)
    {
        r.Cases++;
        if (!inst.ShouldAccept)
            r.Faulty++;
        if (accepted)
            r.Accepted++;
        if (!verdict.correct)
            r.Wrong++;
        if (verdict.unprovenDenial)
            r.Unproven++;
    }

    /// <summary>Where a traveller came from: a forced slot of the day plan, a premade drawn from its pool, or a random draw.</summary>
    private static string Source(DayPlanSO plan, int slot, CaseInstance inst)
    {
        if (plan.ForcedCases.Any(f => f != null && f.caseIndex1Based == slot))
            return "forced";
        return inst.isLegendary ? "premade" : "random";
    }

    private static CaseRecord Record(DayPlanSO plan, int day, int slot, CaseInstance inst, bool accepted, bool correct) => new CaseRecord
    {
        Day = day,
        Slot = slot,
        Kind = inst.kind.ToString(),
        Source = Source(plan, slot, inst),
        Premade = inst.legendarySource != null ? inst.legendarySource.id : "",
        Reason = inst.FaultReason ?? "",
        Faulty = !inst.ShouldAccept,
        Deviation = inst.HasDeviationFault,
        Accepted = accepted,
        Correct = correct,
        Economy = inst.account != null && inst.account.TransponderClass == TransponderClass.Economy
    };

    // ------------------------------------------------------------------
    // The summary
    // ------------------------------------------------------------------

    private static void Write(StringBuilder sb, RunConfigSO run, ContentLibrarySO lib, GameConfigSO config, Dictionary<(PlayStyle, int), List<RunResult>> results, List<string> errors)
    {
        sb.AppendLine($"BALANCE SIMULATION  {DateTime.Now:yyyy-MM-dd HH:mm}  ({Runs} runs x {Days} days per play style and pace; Tools > TimeDesk > Balance > Run 50-Run Simulation)");
        sb.AppendLine($"Each run plays each day through the game's own steps (DayCycle), with no scene: no shop, no care, no slot machine, no dialog choices; once on the whole queue and once at the shift clock's pace ({ShiftPace} travellers a shift, BalanceSimulation.ShiftPace: the rest go home when the clock closes).");
        sb.AppendLine("Perfect: every call right. Imperfect: one wrong call a day (odd days the first faulty traveller let through, even days the first deviation denial left unproven). Careless: both every day.");
        sb.AppendLine();
        Knobs(sb, run, lib, config);

        List<EndingSO> epilogues = lib.Endings.Where(e => e != null && e.conditionType == EndingConditionType.AttrTotalAtLeast && e.attribute != null).ToList();
        foreach (int pace in Paces)
            foreach (PlayStyle style in Styles)
                Style(sb, style, pace, results[(style, pace)], lib, config, epilogues);

        Queue(sb, results[(PlayStyle.Perfect, 0)]);
        Authored(sb, results[(PlayStyle.Perfect, 0)]);

        sb.AppendLine();
        sb.AppendLine("== Checks ==");
        sb.AppendLine($"determinism: seed {ExampleSeed} played twice per style gives the same run: {(errors.Any(e => e.Contains("not deterministic")) ? "FAIL" : "PASS")}");
        sb.AppendLine($"errors logged during the runs: {errors.Count}");
        foreach (string e in errors.Distinct().Take(20))
            sb.AppendLine("  " + e);
    }

    /// <summary>Every knob the balance reads, its value and where Saleh edits it.</summary>
    private static void Knobs(StringBuilder sb, RunConfigSO run, ContentLibrarySO lib, GameConfigSO config)
    {
        sb.AppendLine("== The knobs and where to edit them ==");
        sb.AppendLine("In the Inspector (assets Generate World never writes):");
        sb.AppendLine($"  {AssetDatabase.GetAssetPath(config)}: basePayPerCorrect {config.basePayPerCorrect}, legendaryBonusPay {config.legendaryBonusPay}, wrongDecisionPenalty {config.wrongDecisionPenalty}, freeWarningsPerDay {config.freeWarningsPerDay}, " +
                      $"stabilityChangeRate {config.stabilityChangeRate.ToString("0.####", Inv)}, stabilityLossPerWrong {F(config.stabilityLossPerWrong)}, extraStabilityLossLegendary {F(config.extraStabilityLossLegendary)}, stabilityGainPerCorrect {F(config.stabilityGainPerCorrect)}, firedAtStability {F(config.firedAtStability)}, stabilityWarningMargin {F(config.stabilityWarningMargin)}, stabilityCriticalMargin {F(config.stabilityCriticalMargin)}, bankruptcyMoneyThreshold {config.bankruptcyMoneyThreshold}, " +
                      $"baseDailyExpense {config.baseDailyExpense}, expensePerFamilyMember {config.expensePerFamilyMember}, expensePerConditionPoint {config.expensePerConditionPoint}, conditionWorsenChance {F(config.conditionWorsenChance)}");
        sb.AppendLine($"  {AssetDatabase.GetAssetPath(run)}: startingMoney {run.startingMoney}, startingStability {F(run.startingStability)}, startingFamilyMembers {run.startingFamilyMembers?.Count ?? 0}");
        List<DayPlanSO> plans = Enumerable.Range(1, 6).Select(lib.GetDayPlan).Where(p => p != null).Distinct().ToList();
        foreach (CaseBlueprintSO bp in plans.SelectMany(p => p.PossibleBlueprints.Concat(p.ForcedBlueprints)).Where(b => b != null).Distinct().OrderBy(b => b.Kind))
            sb.AppendLine($"  {AssetDatabase.GetAssetPath(bp)}: contradictionChance {F(bp.ContradictionChance)} (the {bp.Kind} liar chance)");
        foreach (EndingSO e in lib.Endings.Where(e => e != null).OrderBy(e => e.conditionType).ThenBy(e => e.priority))
            sb.AppendLine($"  {AssetDatabase.GetAssetPath(e)}: {e.conditionType} threshold {F(e.threshold)}, priority {e.priority}");
        sb.AppendLine($"  the day plans' guaranteeRuleViolators (the one day-plan field Generate World leaves): {string.Join(", ", plans.Select(p => $"day {p.DayNumber} {p.GuaranteeRuleViolators}"))}");

        AgencyContent agency = lib.Agency;
        sb.AppendLine("In the content spreadsheet (Tools > TimeDesk > Export Content Spreadsheet, edit, then Import Content Spreadsheet, which runs Generate World):");
        sb.AppendLine($"  agency: strandChance {F(agency.strandChance)}; clerk.garnishShare {F(agency.clerk != null ? agency.clerk.garnishShare : 0f)}, clerk.startDebt {(agency.clerk != null ? agency.clerk.startDebt : 0)}; " +
                      $"proofs [{string.Join(", ", agency.proofs.Select(p => $"{p.form} {F(p.weight)}"))}]; transponders [{string.Join(", ", agency.transponders.Select(t => $"{t.id} {t.transponderClass} {F(t.weight)}"))}]");
        foreach (DayPlanSO p in plans)
            sb.AppendLine($"  days/dayKinds, day {p.DayNumber}: queue {p.VisitorsCount}, kinds [{string.Join(", ", p.Kinds.Select(k => $"{(k.blueprint != null ? k.blueprint.Kind.ToString() : "?")} {F(k.weight)}{(k.honest ? " honest" : "")}"))}], " +
                          $"lies [{string.Join(", ", p.EnabledLies)}], violationChance {F(p.ViolationChance)}, costumeErrorChance {F(p.CostumeErrorChance)}, premadeChance {F(p.LegendaryBaseChance)}");
        sb.AppendLine("  (days 7 and on replay day 6's plan)");
    }

    private static string PaceLabel(int pace) => pace > 0 ? $"{pace} a shift" : "whole queue";

    private static void Style(StringBuilder sb, PlayStyle style, int pace, List<RunResult> runs, ContentLibrarySO lib, GameConfigSO config, List<EndingSO> epilogues)
    {
        sb.AppendLine();
        sb.AppendLine($"== {style} play, {PaceLabel(pace)}, {runs.Count} runs ==");
        sb.AppendLine($"endings: {string.Join(", ", runs.GroupBy(r => r.Ending).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} x{g.Count()}"))}");
        List<RunResult> early = runs.Where(r => r.EndingDay < Days).ToList();
        sb.AppendLine($"runs ending before day {Days}'s night: {early.Count}{(early.Count > 0 ? $" ({string.Join(", ", early.GroupBy(r => r.Ending).Select(g => $"{g.Key} on days {string.Join(",", g.Select(r => r.EndingDay).OrderBy(d => d))}"))})" : "")}");
        sb.AppendLine($"per run: travellers {M(runs, r => r.Cases)}, faulty {M(runs, r => r.Faulty)}, accepted {M(runs, r => r.Accepted)}, wrong calls {M(runs, r => r.Wrong)} (unproven denials {M(runs, r => r.Unproven)}), strandings {M(runs, r => r.Stranded)}, carries {M(runs, r => r.Carries)}");
        sb.AppendLine($"per run, cr: pay {M(runs, r => r.Pay)}, wrong-decision penalties {M(runs, r => r.Penalties)}, Debt Relief instalments {M(runs, r => r.Instalments)}, household {M(runs, r => r.Household)}");
        sb.AppendLine($"wallet: lowest in a run mean {F(BalanceStats.Mean(runs.Select(r => (float)r.MinMoney)))}, min {runs.Min(r => r.MinMoney)}; runs ever below 0: {runs.Count(r => r.MinMoney < 0)}; at or below the bankruptcy line ({config.bankruptcyMoneyThreshold}): {runs.Count(r => r.MinMoney <= config.bankruptcyMoneyThreshold)}");
        sb.AppendLine("wallet after each shift, mean/min over the runs still going: " + Curve(runs, r => r.MoneyAfterShift, "d"));
        sb.AppendLine("wallet after each night's bills, mean/min: " + Curve(runs, r => r.MoneyAtNight, "n"));
        sb.AppendLine($"stability at the end (firing line {StabilityRules.Format(config.firedAtStability)}): mean {StabilityRules.Format(BalanceStats.Mean(runs.Select(r => r.World.timelineStability)))}, min {StabilityRules.Format(runs.Min(r => r.World.timelineStability))}, max {StabilityRules.Format(runs.Max(r => r.World.timelineStability))}");
        sb.AppendLine("stability after each shift, mean/min over the runs still going: " + string.Join(" ", Enumerable.Range(0, Days)
            .Select(n => runs.Where(r => r.StabilityAfterShift.Count > n).Select(r => r.StabilityAfterShift[n]).ToList())
            .TakeWhile(l => l.Count > 0)
            .Select((l, n) => $"d{n + 1}:{BalanceStats.Mean(l).ToString("0.00", Inv)}/{l.Min().ToString("0.00", Inv)}")));

        List<RunResult> full = runs.Where(r => r.EndingDay >= Days).ToList();
        if (full.Count == 0 || epilogues.Count == 0)
            return;
        var proposed = new Dictionary<EndingSO, float>();
        foreach (EndingSO e in epilogues)
        {
            List<float> totals = full.Select(r => Total(r.World, e)).ToList();
            proposed[e] = BalanceStats.EpilogueThreshold(totals);
            sb.AppendLine($"day-{Days} {e.attribute.name} total ({e.id}, threshold now {F(e.threshold)}): mean {F(BalanceStats.Mean(totals))} sd {F(BalanceStats.StandardDeviation(totals))} p50 {F(BalanceStats.Quantile(totals, 0.5f))} p65 {F(BalanceStats.Quantile(totals, BalanceStats.EpilogueQuantile))} p80 {F(BalanceStats.Quantile(totals, 0.8f))}");
        }
        if (style != PlayStyle.Perfect)
            return;
        sb.AppendLine($"{(pace == ShiftPace ? "proposed epilogue thresholds" : "for comparison, whole-queue thresholds")} (the {F(BalanceStats.EpilogueQuantile * 100f)}th percentile of perfect play{(pace == ShiftPace ? " at the shift clock's pace" : "")}, rounded): {string.Join(", ", proposed.Select(kv => $"{kv.Key.id} {F(kv.Value)}"))}");
        sb.AppendLine($"endings with those thresholds: {string.Join(", ", full.GroupBy(r => Hypothetical(r.World, lib, config, proposed)).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} x{g.Count()}"))}");
    }

    /// <summary>The run's attribute total an epilogue reads.</summary>
    private static float Total(WorldState world, EndingSO epilogue) => world.timeline.GetScore(TimelineKeys.GlobalAttr(epilogue.attribute));

    /// <summary>The ending a finished run would reach with <paramref name="proposed"/> thresholds (EndingRules.Met and Select, the game's own rule).</summary>
    private static string Hypothetical(WorldState world, ContentLibrarySO lib, GameConfigSO config, Dictionary<EndingSO, float> proposed)
    {
        var now = new EndingCheck(world.timelineStability, world.money, world.day, config.firedAtStability, config.bankruptcyMoneyThreshold);
        List<EndingSO> endings = lib.Endings.Where(e => e != null).ToList();
        var candidates = endings.Select(e => new EndingCandidate(EndingRules.KindOf(e.conditionType), e.priority,
            EndingRules.Met(e.conditionType, proposed.TryGetValue(e, out float t) ? t : e.threshold,
                e.conditionType == EndingConditionType.AttrTotalAtLeast && e.attribute != null ? Total(world, e) : (float?)null, now))).ToList();
        int winner = EndingRules.Select(candidates, EndingMoment.DayBoundary);
        return winner >= 0 ? endings[winner].id : "none";
    }

    /// <summary>The random draws of the perfect runs by day: how many, how many faulty and why, by kind.</summary>
    private static void Queue(StringBuilder sb, List<RunResult> runs)
    {
        sb.AppendLine();
        sb.AppendLine($"== The random draws by day (perfect play, whole queue, {runs.Count} runs; a day counts while its run lasted; authored travellers apart, below) ==");
        foreach (IGrouping<int, CaseRecord> day in runs.SelectMany(r => r.Records).Where(c => c.Source == "random").GroupBy(c => c.Day).OrderBy(g => g.Key))
        {
            List<CaseRecord> all = day.ToList();
            string Share(int n) => (100f * n / all.Count).ToString("0", Inv) + "%";
            string kinds = string.Join(", ", all.GroupBy(c => c.Kind).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()} ({Share2(g.Count(c => c.Faulty), g.Count())} faulty)"));
            string reasons = string.Join(", ", all.Where(c => c.Faulty).GroupBy(c => c.Reason).OrderByDescending(g => g.Count()).Select(g => $"'{g.Key}' {g.Count()}"));
            sb.AppendLine($"day {day.Key}: {all.Count} travellers, faulty {Share(all.Count(c => c.Faulty))} (deviation {Share(all.Count(c => c.Faulty && c.Deviation))}, directive {Share(all.Count(c => c.Faulty && !c.Deviation))}), Economy units {Share(all.Count(c => c.Economy))} | {kinds} | {reasons}");
        }
    }

    /// <summary>The authored travellers of the perfect runs (forced slots, pooled premades) and the dialogs offered, reported apart from the random draws.</summary>
    private static void Authored(StringBuilder sb, List<RunResult> runs)
    {
        sb.AppendLine();
        sb.AppendLine($"== Authored travellers (not random draws; perfect play, whole queue, {runs.Count} runs) ==");
        List<CaseRecord> authored = runs.SelectMany(r => r.Records).Where(c => c.Source != "random").ToList();
        if (authored.Count == 0)
            sb.AppendLine("none stood in these runs");
        foreach (IGrouping<string, CaseRecord> g in authored.Where(c => c.Source == "forced")
                     .GroupBy(c => $"forced {(c.Premade.Length > 0 ? $"premade '{c.Premade}'" : c.Kind)} in slot {c.Slot}").OrderBy(g => g.Key))
            sb.AppendLine($"{g.Key}, days {DayRange(g)}: {Judged(g.ToList())}");
        List<CaseRecord> pooled = authored.Where(c => c.Source == "premade").ToList();
        if (pooled.Count > 0)
            sb.AppendLine($"premades drawn from the days' pools, days {DayRange(pooled)} ({string.Join(", ", pooled.GroupBy(c => c.Premade).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}"))}): {Judged(pooled)}");
        RunResult example = runs.FirstOrDefault();
        if (example != null)
            sb.AppendLine($"dialogs offered to drawn travellers (run seed {example.Seed}; the simulation makes no dialog choice, so a dialog's effect is not in these numbers): {string.Join("; ", example.DialogsOffered)}");
    }

    /// <summary>The days the records stood on, as a range when they run on ("6-15") or a list.</summary>
    private static string DayRange(IEnumerable<CaseRecord> records)
    {
        List<int> days = records.Select(c => c.Day).Distinct().OrderBy(d => d).ToList();
        bool contiguous = days.Count > 1 && days.Last() - days.First() == days.Count - 1;
        return contiguous ? $"{days.First()}-{days.Last()}" : string.Join(",", days);
    }

    /// <summary>How often the records stood, were faulty (and why), were accepted and were judged right.</summary>
    private static string Judged(List<CaseRecord> all)
    {
        string reasons = string.Join(", ", all.Where(c => c.Faulty).GroupBy(c => c.Reason).Select(r => $"'{r.Key}' {r.Count()}"));
        return $"stood {all.Count} times, faulty {all.Count(c => c.Faulty)}{(reasons.Length > 0 ? $" ({reasons})" : "")}, accepted {all.Count(c => c.Accepted)}, the right call {all.Count(c => c.Correct)}";
    }

    private static string Curve(List<RunResult> runs, Func<RunResult, List<int>> values, string prefix) =>
        string.Join(" ", Enumerable.Range(0, Days)
            .Select(n => runs.Where(r => values(r).Count > n).Select(r => values(r)[n]).ToList())
            .TakeWhile(l => l.Count > 0)
            .Select((l, n) => $"{prefix}{n + 1}:{l.Average().ToString("0", Inv)}/{l.Min()}"));

    private static string M(List<RunResult> runs, Func<RunResult, int> value) => F(BalanceStats.Mean(runs.Select(r => (float)value(r))));

    private static string Share2(int n, int of) => of > 0 ? (100f * n / of).ToString("0", Inv) + "%" : "-";

    private static string F(float value) => value.ToString("0.##", Inv);
}
