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
/// where it is edited, the endings, the money, the world the runs leave
/// (the outcomes under END OF DEMO, a distribution, never a target), the queue's faults by day and kind, and the
/// authored travellers (forced slots, premades, dialogs) apart from the random draws;
/// the pet (every run pays its bills as a careful carer, PetPolicy: what it
/// costs, how often it is sick, the Welfare Office's notices and the pets
/// taken; the Home pet spec PS9); and the House's shoppers (the
/// cheapest-first buyer and the climber, each refusing and taking the
/// bribes; Saleh's Q6 of the Home upgrades spec).
/// </summary>
public static class BalanceSimulation
{
    /// <summary>Runs per play style.</summary>
    public const int Runs = 50;

    /// <summary>Days per run: the run's last day (the "world you made" ending is checked at its night).</summary>
    public const int Days = 15;

    /// <summary>The run seed whose every decision is dumped per style, and whose runs are played twice to prove the simulation deterministic.</summary>
    public const int ExampleSeed = 12345;

    /// <summary>
    /// The pace when BalanceSimSettings.asset is missing: the travellers a
    /// careful clerk gets through in a shift before the clock closes (8 real
    /// minutes at about 45 s a traveller). The knob itself is
    /// BalanceSimSettingsSO.travellersPerShift (days 7-15 X3, Q11), which Saleh
    /// edits in the Inspector.
    /// </summary>
    public const int ShiftPace = 10;

    /// <summary>The pace this run plays at (BalanceSimSettingsSO.travellersPerShift, read at Run; <see cref="ShiftPace"/> without the asset).</summary>
    private static int _pace = ShiftPace;

    /// <summary>The House buyer's and the pet's TV reserve, and the price from which a house upgrade is top tier (BalanceSimSettingsSO, read at Run; the asset's defaults without it).</summary>
    private static int _houseReserve = 60, _topTierPrice = 150;

    /// <summary>Who shops at Home in a run: nobody (the plain runs), the cheapest-first buyer (HousePolicy.Purchase) or the climber (HousePolicy.Climb).</summary>
    private enum Shopper
    {
        None,
        Buyer,
        Climber
    }

    /// <summary>The House's variants, each played per style and pace next to the plain runs: its label, its shopper and whether the clerk takes the bribes offered (BribePolicy).</summary>
    private static readonly (string label, Shopper shopper, bool bribes)[] Variants =
    {
        ("buyer", Shopper.Buyer, false),
        ("buyer, takes bribes", Shopper.Buyer, true),
        ("climber", Shopper.Climber, false),
        ("climber, takes bribes", Shopper.Climber, true),
    };

    /// <summary>The folder the summary and the example dumps go to (project-relative; Logs/ is not in git).</summary>
    public const string ReportFolder = "Logs/Balance";

    /// <summary>The summary's file name.</summary>
    public const string SummaryFile = "balance_summary.txt";

    private const int SeedStep = 7919;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly PlayStyle[] Styles = { PlayStyle.Perfect, PlayStyle.Imperfect, PlayStyle.Careless };
    private static int[] Paces => _pace > 0 ? new[] { 0, _pace } : new[] { 0 };

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
        if (!Load(out RunConfigSO run, out ContentLibrarySO lib, out GameConfigSO config))
            return null;

        var settings = AssetDatabase.LoadAssetAtPath<BalanceSimSettingsSO>(BalanceSimSettingsSO.AssetPath);
        _pace = settings != null ? settings.travellersPerShift : ShiftPace;
        _houseReserve = settings != null ? settings.houseReserve : 60;
        _topTierPrice = settings != null ? settings.topTierPrice : 150;

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
            var shoppers = new Dictionary<(PlayStyle, int, int), List<RunResult>>();
            int step = 0, steps = Styles.Length * ((1 + Variants.Length) * Paces.Length * Runs + 2);
            foreach (PlayStyle style in Styles)
            {
                foreach (int pace in Paces)
                {
                    var runs = new List<RunResult>();
                    for (int i = 1; i <= Runs; i++)
                    {
                        EditorUtility.DisplayProgressBar("Balance simulation", $"{style} play, {PaceLabel(pace)}, run {i} of {Runs}", (float)step++ / steps);
                        runs.Add(Play(run, lib, config, i * SeedStep, style, pace, Shopper.None, false));
                    }
                    results[(style, pace)] = runs;

                    for (int v = 0; v < Variants.Length; v++)
                    {
                        var shopped = new List<RunResult>();
                        for (int i = 1; i <= Runs; i++)
                        {
                            EditorUtility.DisplayProgressBar("Balance simulation", $"{style} play, the House's {Variants[v].label}, {PaceLabel(pace)}, run {i} of {Runs}", (float)step++ / steps);
                            shopped.Add(Play(run, lib, config, i * SeedStep, style, pace, Variants[v].shopper, Variants[v].bribes));
                        }
                        shoppers[(style, pace, v)] = shopped;
                    }
                }

                EditorUtility.DisplayProgressBar("Balance simulation", $"{style} play, seed {ExampleSeed} twice", (float)step / steps);
                RunResult a = Play(run, lib, config, ExampleSeed, style, 0, Shopper.None, false);
                RunResult b = Play(run, lib, config, ExampleSeed, style, 0, Shopper.None, false);
                step += 2;
                File.WriteAllText(Path.Combine(dir, $"balance_seed{ExampleSeed}_{style.ToString().ToLowerInvariant()}.txt"), a.Dump.ToString());
                if (a.Fingerprint != b.Fingerprint || a.Dump.ToString() != b.Dump.ToString())
                    errors.Add($"{style} play: seed {ExampleSeed} played twice gave two different runs (the simulation is not deterministic).");
            }

            Write(summary, run, lib, config, results, shoppers, errors);
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

    /// <summary>The run config, its content library and its game config; false (logged) when one is missing.</summary>
    private static bool Load(out RunConfigSO run, out ContentLibrarySO lib, out GameConfigSO config)
    {
        run = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        lib = run != null ? run.contentLibrary : null;
        config = run != null ? run.gameConfig : null;
        if (lib != null && config != null)
            return true;
        Debug.LogError("[Balance] Resources/RunConfig.asset, its content library or its game config is missing.");
        return false;
    }

    /// <summary>
    /// What a caller watches while a run plays (the narrative workbook's reference run,
    /// NarrativeWorkbookMenu): each day's queue as generated, each traveller as they come
    /// to the desk (before the decision), each night after the day turned, and the
    /// ending. Null members are skipped.
    /// </summary>
    public sealed class RunObserver
    {
        /// <summary>A day's queue: the day, its interview (the day's lines, questions and dialogs) and its cases.</summary>
        public Action<int, InterviewDay, IReadOnlyList<CaseInstance>> DayStarted;

        /// <summary>A traveller comes to the desk: the day, the 1-based slot, the case and the day's interview.</summary>
        public Action<int, int, CaseInstance, InterviewDay> Presented;

        /// <summary>A night passed: the day just played and the world after the night's resolve.</summary>
        public Action<int, WorldState> NightTurned;

        /// <summary>The run ended: the day and the ending's id.</summary>
        public Action<int, string> Ended;
    }

    /// <summary>
    /// Plays one run from <paramref name="seed"/> under <paramref name="style"/> over the
    /// whole queue (no shopper, no bribes), through the simulation's own steps, telling
    /// <paramref name="observer"/>; false when the run config is missing. Messages below
    /// errors are muted while it plays, as in <see cref="Run"/>.
    /// </summary>
    public static bool Observe(int seed, PlayStyle style, RunObserver observer)
    {
        if (!Load(out RunConfigSO run, out ContentLibrarySO lib, out GameConfigSO config))
            return false;
        LogType filter = Debug.unityLogger.filterLogType;
        Debug.unityLogger.filterLogType = LogType.Error;
        try
        {
            Play(run, lib, config, seed, style, 0, Shopper.None, false, observer);
        }
        finally
        {
            Debug.unityLogger.filterLogType = filter;
            DevToolsState.ResetAll();
        }
        return true;
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

        /// <summary>The stranding fines the failure reports charged (the endings and strandings spec §7; Saleh's Q10 = D), in cr.</summary>
        public int StrandingFines;

        /// <summary>Home's figures (the Home upgrades spec §9): the house's upkeep paid, the break-ins and what they took, the pet's bills paid and the house upgrades' prices paid.</summary>
        public int Upkeep, BreakIns, BreakInLoss, PetBills, HousePurchases;

        /// <summary>The pet's figures (the Home pet spec PS9): nights it ended sick, nights it went unfed, and nights it ended under the Welfare Office's notice.</summary>
        public int SickNights, UnfedNights, WelfareNights;

        /// <summary>The house upgrades the buyer bought, with the day of each.</summary>
        public readonly List<(int day, string id)> Bought = new List<(int, string)>();

        /// <summary>The bribes the clerk took (BribePolicy) and what they paid, in cr.</summary>
        public int BribesTaken, BribeMoney;

        /// <summary>The top-tier house upgrades bought (priced at the top-tier price or more), with the day of each.</summary>
        public readonly List<(int day, string id)> TopTier = new List<(int, string)>();

        /// <summary>The strandings and the carries of each day played (days 7-15 X5).</summary>
        public readonly List<int> StrandedByDay = new List<int>(), CarriesByDay = new List<int>();

        /// <summary>The past places whose Technology a carry rewrote by the run's end (an accepted liar's or smuggler's, a stranded citizen's; history rules apart; days 7-15 X5).</summary>
        public int TechnologyChanged;
        public readonly List<CaseRecord> Records = new List<CaseRecord>();
        public readonly List<string> DialogsOffered = new List<string>();
        public readonly StringBuilder Dump = new StringBuilder();
        public string Fingerprint = "";
    }

    /// <summary>One traveller of a run: where they came from (a random draw, a forced slot, a pooled premade) and how they were judged.</summary>
    private sealed class CaseRecord
    {
        public int Day, Slot;
        public string Kind, Source, Premade, Reason, Entry, Closure;
        public bool Faulty, Deviation, Accepted, Correct, Economy, Famous;
        public float StabilityDelta;
    }

    /// <summary>A run of <see cref="Days"/> days from <paramref name="seed"/> under <paramref name="style"/>, through the game's own steps, <paramref name="pace"/> travellers a shift (0: the whole queue); with a <paramref name="shopper"/>, each night at Home it treats and buys (<see cref="ShopperNight"/>); with <paramref name="bribes"/>, the clerk takes every bribe a traveller at the desk offers (BribePolicy, applied at the shift's close as the game applies a dialog's effect); an <paramref name="observer"/> is told each day, traveller, night and the ending.</summary>
    private static RunResult Play(RunConfigSO run, ContentLibrarySO lib, GameConfigSO config, int seed, PlayStyle style, int pace, Shopper shopper, bool bribes, RunObserver observer = null)
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
            TodaysWorld today = lib.BuildToday(plan, world);
            List<CaseInstance> cases = new CaseFactory(lib, today).GenerateDayCases(plan, world, Seeds.Day(seed, day), interview, true);
            r.DialogsOffered.Add($"day {day}: [{string.Join(", ", interview.OfferedDialogs(null).Select(d => d.id))}]");
            observer?.DayStarted?.Invoke(day, interview, cases);
            policy.StartDay(day);
            int carriesBefore = r.Carries;

            string ended = null;
            for (int i = 0; i < cases.Count && PlayPolicy.Reaches(i + 1, pace) && ended == null; i++)
            {
                CaseInstance inst = cases[i];
                // The traveller comes to the desk: a once-per-run premade is met (days 7-15 X1, the game's own step).
                DayCycle.Present(world, inst);
                observer?.Presented?.Invoke(day, i + 1, inst, interview);
                if (bribes)
                    foreach ((string dialogId, string effect) in BribePolicy.Take(interview.OfferedDialogs(inst.premadeDialogId), e => Pays(lib, e)))
                        if (interview.Complete(dialogId, effect))
                        {
                            r.BribesTaken++;
                            r.BribeMoney += Mathf.RoundToInt(Pays(lib, effect));
                        }
                PlayDecision decision = policy.Decide(inst.ShouldAccept, inst.HasDeviationFault);
                int carries = world.history.pendingCarries.Count;
                CaseVerdict verdict = DayCycle.Decide(inst, decision.Accept, i + 1, decision.Documented ? 1 : 0, world, today, ledger, lib, config);
                r.Carries += world.history.pendingCarries.Count - carries;
                r.Records.Add(Record(plan, day, i + 1, inst, decision.Accept, verdict));
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
            r.StrandingFines += ledger.strandingFines;
            r.StrandedByDay.Add(ledger.strandedCount);
            r.CarriesByDay.Add(r.Carries - carriesBefore);
            r.MoneyAfterShift.Add(world.money);
            r.StabilityAfterShift.Add(world.timelineStability);
            r.MinMoney = Math.Min(r.MinMoney, world.money);
            r.Dump.AppendLine($"shift {day}: pay {ledger.TotalPay} penalties {ledger.TotalPenalties} stranded {ledger.strandedCount} stranding fines {ledger.strandingFines} instalment {ledger.debtInstalment} money {world.money}{(ended != null ? " ENDING " + ended : "")}");
            if (ended != null)
            {
                End(r, ended, day);
                observer?.Ended?.Invoke(day, ended);
                break;
            }

            // Home, as HomeManager plays it: the break-in and the fixed bill; the pet's bills as a careful carer (PetPolicy,
            // every run); the shopper buys (the plain runs buy nothing); the pet's night at Sleep. No run spins the slot
            // machine, orders at the PC (so no toy) or pets the pet.
            int nightSeed = Seeds.Day(seed, day);
            HomeEconomy.ExpenseReport bill = DayCycle.OpenHome(world, lib, config, nightSeed);
            PetCare care = PetPolicy.Care(world.pet.Needs, world.money, b => HomeEconomy.BillPrice(world, lib, b), _houseReserve, HomeEconomy.OwnedToys(world, lib).Count > 0);
            int bills = Math.Max(0, HomeEconomy.PayBills(world, lib, care));
            int bought = shopper != Shopper.None ? ShopperNight(world, lib, r, day, shopper) : 0;
            HomeEconomy.PetNight(world, lib, config, nightSeed, care);
            ClerkAccountSource.RecordHome(world, bill.total + bills, bought, lib, config);
            r.Household += bill.total + bills;
            r.Upkeep += bill.upkeepAmount;
            r.BreakInLoss += bill.breakInLoss;
            r.BreakIns += bill.breakInLoss > 0 ? 1 : 0;
            r.PetBills += bills;
            r.SickNights += world.pet.sickness > 0 ? 1 : 0;
            r.UnfedNights += care.Food ? 0 : 1;
            r.WelfareNights += world.pet.welfareNights > 0 ? 1 : 0;
            r.HousePurchases += bought;
            r.MoneyAtNight.Add(world.money);
            r.MinMoney = Math.Min(r.MinMoney, world.money);

            EndingSO sleep = EndingService.Evaluate(world, lib, config, EndingMoment.DayBoundary);
            if (sleep != null)
            {
                DayCycle.EndRun(world, sleep, lib, config);
                r.Dump.AppendLine($"night {day}: household {bill.total} + pet's bills {bills} money {world.money} pet {Pet(world.pet)} ENDING {sleep.id}");
                End(r, sleep.id, day);
                observer?.NightTurned?.Invoke(day, world);
                observer?.Ended?.Invoke(day, sleep.id);
                break;
            }
            DayCycle.AdvanceNight(world, lib, config);
            observer?.NightTurned?.Invoke(day, world);
            r.Dump.AppendLine($"night {day}: household {bill.total} + pet's bills {bills} money {world.money} pet {Pet(world.pet)} leader '{world.history.leaderId}'");
        }

        r.TechnologyChanged = world.history.factEdits.Where(e => e != null && e.cause == EditCause.Carry && e.category == ClueCategory.Technology && e.eraId != "future")
            .Select(e => e.nationId + "/" + e.eraId).Distinct().Count();
        r.Fingerprint = JsonUtility.ToJson(world.history) + JsonUtility.ToJson(world.timeline) + world.money.ToString(Inv) + r.Ending;
        return r;
    }

    /// <summary>The pet's needs for the dumps (the simulation's own numbers; the player only ever reads words).</summary>
    private static string Pet(PetState pet) =>
        pet == null ? "none" : $"hunger {pet.hunger} cold {pet.cold} boredom {pet.boredom} sickness {pet.sickness} welfare {pet.welfareNights}{(pet.taken ? " TAKEN" : "")}";

    /// <summary>What a dialog effect pays the clerk: its AddMoney ops summed (0 for an unknown effect).</summary>
    private static float Pays(ContentLibrarySO lib, string effect)
    {
        EffectSO so = lib.GetEffectByAssetName(effect);
        return so != null ? so.ops.Where(o => o != null && o.type == EffectOpType.AddMoney).Sum(o => o.floatParam) : 0f;
    }

    /// <summary>
    /// A shopper's night (Domain HousePolicy through the game's own purchase
    /// path), after the pet's bills: the buyer buys at most one house
    /// upgrade, the cheapest buyable one that keeps the reserve, and the
    /// climber buys every step of the cheapest top-tier path once the wallet
    /// covers it all and keeps the reserve (HomeEconomy.BuyHouseUpgrade).
    /// Returns the purchases paid.
    /// </summary>
    private static int ShopperNight(WorldState world, ContentLibrarySO lib, RunResult r, int day, Shopper shopper)
    {
        List<UpgradeSO> house = lib.Upgrades.Where(u => u != null && u.venue == UpgradeVenue.Home).ToList();
        int spent = 0;
        while (true)
        {
            List<HouseOffer> offers = house.Select(u => new HouseOffer(u.id, OrderBook.Price(world, lib, u), world.HasUpgrade(u.id), UpgradeTree.Unlocked(u.Node, world.HasUpgrade), u.Node.Requires)).ToList();
            string pick = shopper == Shopper.Climber
                ? HousePolicy.Climb(offers, world.money, _houseReserve, _topTierPrice)
                : HousePolicy.Purchase(offers, world.money, _houseReserve);
            UpgradeSO upgrade = pick != null ? house.First(u => u.id == pick) : null;
            int paid = upgrade != null ? HomeEconomy.BuyHouseUpgrade(world, lib, upgrade) : -1;
            if (paid < 0)
                break;
            spent += paid;
            r.Bought.Add((day, pick));
            if (upgrade.cost >= _topTierPrice)
                r.TopTier.Add((day, pick));
            if (shopper == Shopper.Buyer)
                break;
        }
        return spent;
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

    private static CaseRecord Record(DayPlanSO plan, int day, int slot, CaseInstance inst, bool accepted, CaseVerdict verdict) => new CaseRecord
    {
        Entry = inst.forcedAppearance != null ? (string.IsNullOrEmpty(inst.forcedAppearance.id) ? "(unnamed)" : inst.forcedAppearance.id) : "",
        Closure = inst.directiveFault == DirectiveFault.ClosedDestination ? ClosureOf(plan, inst) : "",
        Famous = inst.IsFamous,
        StabilityDelta = verdict.stabilityDelta,
        Day = day,
        Slot = slot,
        Kind = inst.kind.ToString(),
        Source = Source(plan, slot, inst),
        Premade = inst.legendarySource != null ? inst.legendarySource.id : "",
        Reason = inst.FaultReason ?? "",
        Faulty = !inst.ShouldAccept,
        Deviation = inst.HasDeviationFault,
        Accepted = accepted,
        Correct = verdict.correct,
        Economy = inst.account != null && inst.account.TransponderClass == TransponderClass.Economy
    };

    /// <summary>The type of the day's closure that bars the traveller (nation-era, nation, era; an era closure listing kinds is the range limit), days 7-15 X4.</summary>
    private static string ClosureOf(DayPlanSO plan, CaseInstance inst)
    {
        TravelRuleSO rule = plan.ActiveTravelRules.FirstOrDefault(t => t != null && t.IsClosure && t.AppliesTo(inst.kind) && !t.Allows(inst.claimedNation, inst.claimedEra));
        if (rule == null)
            return "?";
        return rule.type == TravelRuleType.EraForbidden && rule.kinds != null && rule.kinds.Length > 0 ? "range limit" : rule.type.ToString();
    }

    // ------------------------------------------------------------------
    // The summary
    // ------------------------------------------------------------------

    private static void Write(StringBuilder sb, RunConfigSO run, ContentLibrarySO lib, GameConfigSO config, Dictionary<(PlayStyle, int), List<RunResult>> results,
                              Dictionary<(PlayStyle, int, int), List<RunResult>> shoppers, List<string> errors)
    {
        sb.AppendLine($"BALANCE SIMULATION  {DateTime.Now:yyyy-MM-dd HH:mm}  ({Runs} runs x {Days} days per play style and pace; Tools > TimeDesk > Balance > Run 50-Run Simulation)");
        sb.AppendLine($"Each run plays each day through the game's own steps (DayCycle), with no scene: no orders, no slot machine, no dialog choices (but the bribes in the House's bribe-taking variants), the pet's bills paid as a careful carer every night (PetPolicy: food, the heating with its electricity, medicine when unwell, the TV when bored and the reserve holds), and no house upgrade except in the House's variants (their own section below); once on the whole queue and once at the shift clock's pace ({_pace} travellers a shift, {BalanceSimSettingsSO.AssetPath} travellersPerShift: the rest go home when the clock closes, PlayPolicy.Reaches).");
        sb.AppendLine("Perfect: every call right. Imperfect: one wrong call a day (odd days the first faulty traveller let through, even days the first deviation denial left unproven). Careless: both every day.");
        sb.AppendLine();
        Knobs(sb, run, lib, config);

        foreach (int pace in Paces)
            foreach (PlayStyle style in Styles)
                Style(sb, style, pace, results[(style, pace)], lib, config);

        Queue(sb, results[(PlayStyle.Perfect, 0)]);
        Authored(sb, results[(PlayStyle.Perfect, 0)]);
        Beats(sb, results);
        Contamination(sb, results);
        Bribe(sb, lib);
        House(sb, lib, config, results, shoppers);

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
                      $"baseDailyExpense {config.baseDailyExpense}, expensePerConditionPoint {config.expensePerConditionPoint}");
        sb.AppendLine($"  {AssetDatabase.GetAssetPath(run)}: startingMoney {run.startingMoney}, startingStability {F(run.startingStability)}, startingPetKind {run.startingPetKind}");
        sb.AppendLine($"  {BalanceSimSettingsSO.AssetPath}: travellersPerShift {_pace} (the simulation's cap, days 7-15 X3), houseReserve {_houseReserve} (the House's shoppers, and the pet's TV), topTierPrice {_topTierPrice} (the climber's target and the top tier counted below)");
        sb.AppendLine($"  {AssetDatabase.GetAssetPath(config)} (Home, the pet): petNeedMax {config.petNeedMax}, conditionWorsenChance {F(config.conditionWorsenChance)}, sicknessPerNeed {F(config.sicknessPerNeed)}, welfareNights {config.welfareNights}, recoveryPerMood {F(config.recoveryPerMood)}, maxRecoveryChance {F(config.maxRecoveryChance)}, sicknessPerMood {F(config.sicknessPerMood)}, maxMoodSicknessCut {F(config.maxMoodSicknessCut)}, " +
                      $"breakInFromDay {config.breakInFromDay}, breakInChance {F(config.breakInChance)}, breakInShare {F(config.breakInShare)}, breakInMaxLoss {config.breakInMaxLoss}");
        sb.AppendLine($"  world_source.json home.bills (the content spreadsheet): {string.Join(", ", lib.Home.bills.Where(b => b != null).Select(b => $"{b.bill} {b.price}"))} cr a night");
        sb.AppendLine($"  {AssetDatabase.GetAssetPath(config)} (strandings): strandingFine {config.strandingFine} (Saleh's Q10 = D), strandingFateTilt {F(config.strandingFateTilt)}, waiverSignMinutes {F(config.waiverSignMinutes)} (the simulation never uses the pad)");
        EffectSO bribe = lib.GetEffectByAssetName(BribeEffect);
        if (bribe != null)
            sb.AppendLine($"  {AssetDatabase.GetAssetPath(bribe)}: AddMoney {F(bribe.ops.Where(o => o != null && o.type == EffectOpType.AddMoney).Sum(o => o.floatParam))} (the bribe's amount, days 7-15 X6)");
        int lastDay = Math.Max(6, lib.LastDay);
        List<DayPlanSO> plans = Enumerable.Range(1, lastDay).Select(lib.GetDayPlan).Where(p => p != null).Distinct().ToList();
        foreach (CaseBlueprintSO bp in plans.SelectMany(p => p.PossibleBlueprints.Concat(p.ForcedBlueprints)).Where(b => b != null).Distinct().OrderBy(b => b.Kind))
            sb.AppendLine($"  {AssetDatabase.GetAssetPath(bp)}: contradictionChance {F(bp.ContradictionChance)} (the {bp.Kind} liar chance)");
        foreach (EndingSO e in lib.Endings.Where(e => e != null).OrderBy(e => e.conditionType).ThenBy(e => e.priority))
            sb.AppendLine($"  {AssetDatabase.GetAssetPath(e)}: {e.conditionType} threshold {F(e.threshold)}, priority {e.priority}");
        sb.AppendLine($"  the day plans' guaranteeRuleViolators (the one day-plan field Generate World leaves): {string.Join(", ", plans.Select(p => $"day {p.DayNumber} {p.GuaranteeRuleViolators}"))}");

        AgencyContent agency = lib.Agency;
        sb.AppendLine("In the content spreadsheet (Tools > TimeDesk > Export Content Spreadsheet, edit, then Import Content Spreadsheet, which runs Generate World):");
        sb.AppendLine($"  agency.strandingFates (waivered/unwaivered weights): {string.Join(", ", agency.strandingFates.Where(f => f != null).Select(f => $"{f.fate} {F(f.weightWaivered)}/{F(f.weightUnwaivered)}{(f.stability > 0f ? $" stability {F(f.stability)}%" : "")}"))}; " +
                      $"personalities (waiverRefusal, strandingFate): {string.Join(", ", lib.Personalities.Where(p => p != null).Select(p => $"{p.id} {F(p.waiverRefusal)}{(string.IsNullOrEmpty(p.strandingFate) ? "" : " " + p.strandingFate)}"))}");
        sb.AppendLine($"  agency: strandChance {F(agency.strandChance)}; clerk.garnishShare {F(agency.clerk != null ? agency.clerk.garnishShare : 0f)}, clerk.startDebt {(agency.clerk != null ? agency.clerk.startDebt : 0)}; " +
                      $"proofs [{string.Join(", ", agency.proofs.Select(p => $"{p.form} {F(p.weight)}"))}]; transponders [{string.Join(", ", agency.transponders.Select(t => $"{t.id} {t.transponderClass} {F(t.weight)}"))}]");
        foreach (DayPlanSO p in plans)
            sb.AppendLine($"  days/dayKinds, day {p.DayNumber}: queue {p.VisitorsCount}, kinds [{string.Join(", ", p.Kinds.Select(k => $"{(k.blueprint != null ? k.blueprint.Kind.ToString() : "?")} {F(k.weight)}{(k.honest ? " honest" : "")}"))}], " +
                          $"lies [{string.Join(", ", p.EnabledLies)}], violationChance {F(p.ViolationChance)}, costumeErrorChance {F(p.CostumeErrorChance)}, premadeChance {F(p.LegendaryBaseChance)}");
        foreach (TravelRuleSO recall in plans.SelectMany(p => p.ActiveTravelRules).Where(t => t != null && t.type == TravelRuleType.TransponderRecall).Distinct())
            sb.AppendLine($"  rules, {recall.name}: transponder {recall.transponder}, kinds [{string.Join(", ", recall.kinds ?? new TravellerKind[0])}], first day {lib.FirstDayOf(recall)}");
        sb.AppendLine($"  violationChance by day (its ramp): {string.Join(" ", plans.Select(p => $"d{p.DayNumber}:{F(p.ViolationChance)}"))}");
        sb.AppendLine($"  (a day past {lastDay} replays day {lastDay}'s plan)");
    }

    private static string PaceLabel(int pace) => pace > 0 ? $"{pace} a shift" : "whole queue";

    private static void Style(StringBuilder sb, PlayStyle style, int pace, List<RunResult> runs, ContentLibrarySO lib, GameConfigSO config)
    {
        sb.AppendLine();
        sb.AppendLine($"== {style} play, {PaceLabel(pace)}, {runs.Count} runs ==");
        sb.AppendLine($"endings: {string.Join(", ", runs.GroupBy(r => r.Ending).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} x{g.Count()}"))}");
        List<RunResult> early = runs.Where(r => r.EndingDay < Days).ToList();
        sb.AppendLine($"runs ending before day {Days}'s night: {early.Count}{(early.Count > 0 ? $" ({string.Join(", ", early.GroupBy(r => r.Ending).Select(g => $"{g.Key} on days {string.Join(",", g.Select(r => r.EndingDay).OrderBy(d => d))}"))})" : "")}");
        sb.AppendLine($"per run: travellers {M(runs, r => r.Cases)}, faulty {M(runs, r => r.Faulty)}, accepted {M(runs, r => r.Accepted)}, wrong calls {M(runs, r => r.Wrong)} (unproven denials {M(runs, r => r.Unproven)}), strandings {M(runs, r => r.Stranded)}, carries {M(runs, r => r.Carries)}");
        sb.AppendLine($"per run, cr: pay {M(runs, r => r.Pay)}, wrong-decision penalties {M(runs, r => r.Penalties)}, stranding fines {M(runs, r => r.StrandingFines)}, Debt Relief instalments {M(runs, r => r.Instalments)}, household {M(runs, r => r.Household)} (the pet's bills {M(runs, r => r.PetBills)})");
        sb.AppendLine($"the pet (a careful carer, PetPolicy): nights it ended sick {M(runs, r => r.SickNights)}, nights unfed {M(runs, r => r.UnfedNights)}, nights under the Welfare Office's notice {M(runs, r => r.WelfareNights)}; taken in {runs.Count(r => r.World.pet != null && r.World.pet.taken)} of {runs.Count} runs");
        Fates(sb, runs);
        sb.AppendLine($"wallet: lowest in a run mean {F(BalanceStats.Mean(runs.Select(r => (float)r.MinMoney)))}, min {runs.Min(r => r.MinMoney)}; runs ever below 0: {runs.Count(r => r.MinMoney < 0)}; at or below the bankruptcy line ({config.bankruptcyMoneyThreshold}): {runs.Count(r => r.MinMoney <= config.bankruptcyMoneyThreshold)}");
        sb.AppendLine("wallet after each shift, mean/min over the runs still going: " + Curve(runs, r => r.MoneyAfterShift, "d"));
        sb.AppendLine("wallet after each night's bills, mean/min: " + Curve(runs, r => r.MoneyAtNight, "n"));
        sb.AppendLine($"stability at the end (firing line {StabilityRules.Format(config.firedAtStability)}): mean {StabilityRules.Format(BalanceStats.Mean(runs.Select(r => r.World.timelineStability)))}, min {StabilityRules.Format(runs.Min(r => r.World.timelineStability))}, max {StabilityRules.Format(runs.Max(r => r.World.timelineStability))}");
        sb.AppendLine("stability after each shift, mean/min over the runs still going: " + string.Join(" ", Enumerable.Range(0, Days)
            .Select(n => runs.Where(r => r.StabilityAfterShift.Count > n).Select(r => r.StabilityAfterShift[n]).ToList())
            .TakeWhile(l => l.Count > 0)
            .Select((l, n) => $"d{n + 1}:{BalanceStats.Mean(l).ToString("0.00", Inv)}/{l.Min().ToString("0.00", Inv)}")));

        World(sb, runs, lib, config);
    }

    /// <summary>
    /// The strandings' fates over the runs (the endings and strandings spec
    /// §6, §10: never shown to the player): how many met each fate, how many
    /// had a valid signed waiver on file, and the fines the unwaivered cost,
    /// from each run's stranding log.
    /// </summary>
    private static void Fates(StringBuilder sb, List<RunResult> runs)
    {
        List<StrandingRecord> all = runs.SelectMany(r => r.World.history.strandingLog ?? new List<StrandingRecord>()).Where(s => s != null).ToList();
        sb.AppendLine($"strandings over the runs: {all.Count} ({all.Count(s => s.waivered)} with a valid signed waiver on file, {all.Count(s => !s.waivered)} without); by fate: " +
                      string.Join(", ", ((StrandingFate[])Enum.GetValues(typeof(StrandingFate))).Select(f => $"{f} {all.Count(s => s.fate == f)} ({all.Count(s => s.fate == f && s.waivered)} waivered)")) +
                      $"; stranding fines {all.Sum(s => s.fine)} cr over {all.Count(s => s.fine > 0)} stranding(s), per run {M(runs, r => r.StrandingFines)} cr");
    }

    /// <summary>
    /// The world each run leaves (the endings spec E0: the outcomes listed
    /// under END OF DEMO, on the last day and after a failure alike;
    /// ContentLibrarySO.WorldOutcomes, the Title's own reading), as a
    /// distribution per factor, never against a target (Saleh 2026-09-29:
    /// "we dont make judgements").
    /// </summary>
    private static void World(StringBuilder sb, List<RunResult> runs, ContentLibrarySO lib, GameConfigSO config)
    {
        if (runs.Count == 0)
            return;
        List<List<OutcomeLine>> worlds = runs.Select(r => lib.WorldOutcomes(r.World, config)).ToList();
        sb.AppendLine($"the world the runs leave ({runs.Count} runs, the outcomes under END OF DEMO; a distribution, not a target):");
        foreach (OutcomeLine factor in worlds[0])
        {
            IEnumerable<string> answers = worlds.Select(w => w.FirstOrDefault(l => l.FactorId == factor.FactorId).Answer ?? "none");
            sb.AppendLine($"  {factor.Question} " + string.Join(", ", answers.GroupBy(a => a).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).Select(g => $"{g.Key} x{g.Count()}")));
        }

        // The endings spec §10's watch lines (variety, not a target): which outcomes no run reached, how often the world
        // stayed as the run found it, how many splits; tuned in the leanings and the Inspector, never by making one outcome "harder".
        List<List<FactorLead>> leads = runs.Select(r => lib.WorldAnswersNow(r.World, config)).ToList();
        foreach (WorldFactor f in lib.World.factors.Where(f => f != null && f.answer == FactorAnswer.Pulls))
        {
            List<FactorLead> of = leads.Select(a => a.FirstOrDefault(l => l.factor == f.id)).Where(l => l != null).ToList();
            IEnumerable<string> reached = of.SelectMany(l => l.IsSplit ? new[] { l.outcome, l.split } : new[] { l.outcome });
            List<string> never = lib.World.OutcomesOf(f.id).Select(o => o.id).Except(reached).ToList();
            sb.AppendLine($"  watch, {f.id}: as found ({f.statusQuo}) in {of.Count(l => !l.IsSplit && l.outcome == f.statusQuo)} of {of.Count}; splits {of.Count(l => l.IsSplit)}; " +
                          $"outcomes no run reached: {(never.Count > 0 ? string.Join(", ", never) : "none")}");
        }
        foreach (PullFactor f in lib.World.PullFactors())
            sb.AppendLine($"  watch, {f.Id}'s pull per run at the end (mean; \"as found\" starts {F(config.worldStatusQuoWeight)} ahead): " + string.Join(", ", f.Outcomes
                .Select(o => (o, mean: BalanceStats.Mean(runs.Select(r => WorldPulls.Weight(r.World.pulls, f.Id, o))))).OrderByDescending(x => x.mean).Select(x => $"{x.o} {F(x.mean)}")));
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

    /// <summary>The dialog effect the bribe's choice names (dlg_rook, days 7-15 §4.3).</summary>
    private const string BribeEffect = "Effect_Dialog_BribeTaken";

    /// <summary>
    /// Days 7-15 X2 and X8: each beat (a forced entry of a day plan) per style at
    /// the shift clock's pace: how often it stood, which entry, why not (a met
    /// premade or no entry's conditions), its fault, the right calls, the
    /// famous beats' stability cost; then the story's branches: how many runs
    /// fired each story rule.
    /// </summary>
    private static void Beats(StringBuilder sb, Dictionary<(PlayStyle, int), List<RunResult>> results)
    {
        sb.AppendLine();
        sb.AppendLine($"== The beats of days 7-15 (forced entries; per style at {PaceLabel(_pace)}) ==");
        int pace = Paces.Last();
        foreach (PlayStyle style in Styles)
        {
            List<RunResult> runs = results[(style, pace)];
            sb.AppendLine($"{style}:");
            foreach (IGrouping<(int, int), CaseRecord> slot in runs.SelectMany(r => r.Records).Where(c => c.Day >= 7 && c.Source == "forced").GroupBy(c => (c.Day, c.Slot)).OrderBy(g => g.Key))
            {
                List<CaseRecord> all = slot.ToList();
                string entries = string.Join(", ", all.GroupBy(c => c.Entry.Length > 0 ? $"{c.Entry} ({c.Premade})" : "none standing (met, or no entry's conditions)").Select(g => $"{g.Key} {g.Count()}"));
                List<CaseRecord> famous = all.Where(c => c.Famous && c.Entry.Length > 0).ToList();
                string cost = famous.Count > 0 ? $"; the famous beat's stability change per run {F(famous.Sum(c => c.StabilityDelta) / runs.Count)} (wrong {famous.Count(c => !c.Correct)})" : "";
                sb.AppendLine($"  day {slot.Key.Item1} slot {slot.Key.Item2}: reached in {all.Count} of {runs.Count} runs: {entries}; {Judged(all.Where(c => c.Entry.Length > 0).ToList())}{cost}");
            }
            var fired = runs.SelectMany(r => r.World.flags.Where(f => f.StartsWith("trig:history_") && f.EndsWith(":fired")).Distinct())
                .GroupBy(f => f.Substring("trig:history_".Length, f.Length - "trig:history_".Length - ":fired".Length)).OrderBy(g => g.Key);
            sb.AppendLine($"  the story's branches (runs that fired each rule): {string.Join(", ", fired.Select(g => $"{g.Key} {g.Count()}"))}");
            sb.AppendLine($"  verdicts remembered at the end: {string.Join(", ", runs.SelectMany(r => r.World.flags.Where(f => f.StartsWith("premade:") && !f.EndsWith(":met"))).GroupBy(f => f).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}"))}");
        }
    }

    /// <summary>Days 7-15 X4 and X5: the closures by type and the 2150 contamination (strandings, carries, past places whose Technology changed) by day.</summary>
    private static void Contamination(StringBuilder sb, Dictionary<(PlayStyle, int), List<RunResult>> results)
    {
        sb.AppendLine();
        sb.AppendLine("== Closures by type and the 2150 contamination ==");
        foreach (int pace in Paces)
            foreach (PlayStyle style in Styles)
            {
                List<RunResult> runs = results[(style, pace)];
                string closures = string.Join(", ", runs.SelectMany(r => r.Records).Where(c => c.Closure.Length > 0).GroupBy(c => c.Closure).OrderBy(g => g.Key).Select(g => $"{g.Key} {F((float)g.Count() / runs.Count)}"));
                string Daily(Func<RunResult, List<int>> of) => string.Join(" ", Enumerable.Range(0, Days)
                    .Select(n => runs.Where(r => of(r).Count > n).Select(r => of(r)[n]).ToList()).TakeWhile(l => l.Count > 0)
                    .Select((l, n) => $"d{n + 1}:{l.Average().ToString("0.##", Inv)}"));
                List<float> changed = runs.Select(r => (float)r.TechnologyChanged).ToList();
                sb.AppendLine($"{style}, {PaceLabel(pace)}: closure faults per run by type [{closures}]; recalled units per run {F((float)runs.SelectMany(r => r.Records).Count(c => c.Reason == Faults.Recalled) / runs.Count)}");
                sb.AppendLine($"  strandings per day, mean: {Daily(r => r.StrandedByDay)}");
                sb.AppendLine($"  carries per day, mean: {Daily(r => r.CarriesByDay)}");
                sb.AppendLine($"  past places whose Technology a carry rewrote by the run's end: mean {F(BalanceStats.Mean(changed))}, median {F(BalanceStats.Quantile(changed, 0.5f))}, max {F(changed.Max())} (watch line: more than three in the median run lowers agency.strandChance)");
            }
    }

    /// <summary>
    /// The House (the Home upgrades spec §9, §12): per pace and style, the
    /// plain runs (no purchase) against the House's variants (the
    /// cheapest-first buyer and the climber, each refusing and taking the
    /// bribes): the endings, the wallet, the household, upkeep, break-ins,
    /// the pet's bills, the bribes, what the house upgrades cost and which were bought
    /// when, and the top tier reached; then Saleh's Q6 in one table (the top
    /// tier by day 15, careful against bribe-taking); and the tree's prices
    /// from the content spreadsheet.
    /// </summary>
    private static void House(StringBuilder sb, ContentLibrarySO lib, GameConfigSO config, Dictionary<(PlayStyle, int), List<RunResult>> results, Dictionary<(PlayStyle, int, int), List<RunResult>> shoppers)
    {
        List<UpgradeSO> house = lib.Upgrades.Where(u => u != null && u.venue == UpgradeVenue.Home).ToList();
        sb.AppendLine();
        sb.AppendLine($"== The House: the plain runs against its shoppers (reserve {_houseReserve} cr; the top tier from {_topTierPrice} cr) ==");
        sb.AppendLine($"the tree (content spreadsheet, homeUpgrades): {house.Count} upgrades, {house.Sum(u => u.cost)} cr in all: {string.Join(", ", house.OrderBy(u => u.cost).Select(u => $"{u.id} {u.cost}"))}");
        sb.AppendLine($"the top tier: {string.Join(", ", house.Where(u => u.cost >= _topTierPrice).OrderBy(u => u.cost).Select(u => $"{u.id} {u.cost}"))}");
        sb.AppendLine("the buyer buys the cheapest upgrade it may each night; the climber buys nothing but the cheapest top-tier path, all of it on the night the wallet covers it and keeps the reserve (HousePolicy); a bribe-taking clerk takes every bribe offered at the desk (BribePolicy).");
        foreach (int pace in Paces)
            foreach (PlayStyle style in Styles)
            {
                sb.AppendLine($"{style}, {PaceLabel(pace)}:");
                var rows = new List<(string label, List<RunResult> runs)> { ("plain", results[(style, pace)]) };
                rows.AddRange(Variants.Select((v, i) => (v.label, shoppers[(style, pace, i)])));
                foreach ((string label, List<RunResult> runs) in rows)
                {
                    sb.AppendLine($"  {label}: endings {string.Join(", ", runs.GroupBy(r => r.Ending).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} x{g.Count()}"))}; " +
                                  $"wallet lowest mean {F(BalanceStats.Mean(runs.Select(r => (float)r.MinMoney)))} min {runs.Min(r => r.MinMoney)}, at the end mean {F(BalanceStats.Mean(runs.Select(r => (float)r.World.money)))} min {runs.Min(r => r.World.money)} max {runs.Max(r => r.World.money)}; " +
                                  $"per run cr: household {M(runs, r => r.Household)} (upkeep {M(runs, r => r.Upkeep)}, break-ins {M(runs, r => r.BreakIns)} taking {M(runs, r => r.BreakInLoss)}, the pet's bills {M(runs, r => r.PetBills)}), bribes {M(runs, r => r.BribeMoney)} ({M(runs, r => r.BribesTaken)} taken), " +
                                  $"house upgrades {M(runs, r => r.HousePurchases)} ({M(runs, r => r.Bought.Count)} bought); {TopTier(runs)}");
                    if (label != "plain")
                        sb.AppendLine("    bought (runs, mean day): " + string.Join(", ", runs.SelectMany(r => r.Bought).GroupBy(b => b.id).OrderBy(g => g.Average(b => b.day))
                                          .Select(g => $"{g.Key} {g.Count()} d{g.Average(b => b.day).ToString("0.#", Inv)}")));
                }
            }

        sb.AppendLine();
        sb.AppendLine($"Saleh's Q6 (\"balance so you can buy one or two if you take bribes\"): top-tier upgrades owned by day {Days}'s night, careful against bribe-taking, per style at each pace:");
        sb.AppendLine("  style, pace | buyer: careful / takes bribes | climber: careful / takes bribes");
        foreach (int pace in Paces)
            foreach (PlayStyle style in Styles)
            {
                string Cell(int v) => $"{M(shoppers[(style, pace, v)], r => r.TopTier.Count)} ({shoppers[(style, pace, v)].Count(r => r.TopTier.Count > 0)}/{Runs} runs)";
                sb.AppendLine($"  {style}, {PaceLabel(pace)} | {Cell(0)} / {Cell(1)} | {Cell(2)} / {Cell(3)}");
            }
    }

    /// <summary>The runs' top tier: the mean bought, the runs with one and with two or more, and the mean day of the first.</summary>
    private static string TopTier(List<RunResult> runs)
    {
        List<RunResult> reached = runs.Where(r => r.TopTier.Count > 0).ToList();
        string first = reached.Count > 0 ? $", the first on day {reached.Average(r => r.TopTier[0].day).ToString("0.#", Inv)}" : "";
        return $"top tier {M(runs, r => r.TopTier.Count)} a run (one in {runs.Count(r => r.TopTier.Count == 1)} runs, two or more in {runs.Count(r => r.TopTier.Count >= 2)}{first})";
    }

    /// <summary>Days 7-15 X6: the bribe is a dialog choice, which only the House's bribe-taking variants make; its amount is reported here.</summary>
    private static void Bribe(StringBuilder sb, ContentLibrarySO lib)
    {
        EffectSO bribe = lib.GetEffectByAssetName(BribeEffect);
        float amount = bribe != null ? bribe.ops.Where(o => o != null && o.type == EffectOpType.AddMoney).Sum(o => o.floatParam) : 0f;
        sb.AppendLine();
        sb.AppendLine($"== The bribe (day 11, dlg_rook) ==");
        sb.AppendLine($"not in the plain runs' numbers: only the House's bribe-taking variants take it (below). Taken, it adds {F(amount)} cr once at day 11's close ({BribeEffect}); its consequences are a story rule's 3 % of stability (rook_complaint if Rook is denied, audit_rook on day 14's night if he is approved).");
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
