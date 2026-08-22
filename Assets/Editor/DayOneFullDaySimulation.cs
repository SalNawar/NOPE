using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Full-day integration simulation for Day One (plan Task 8's headless full-day
/// test): drives the REAL runtime systems — CaseFactory generation, the
/// DiscrepancyLog evidence gate, ShiftScoring verdicts, TimelineService
/// impacts + nightly resolve, HomeEconomy expenses, slot outcomes, and the
/// EndingService day gate — across four player archetypes. No UI, no mocks:
/// if this passes, the day plays as designed on the numbers.
/// </summary>
public static class DayOneFullDaySimulation
{
    private static readonly List<string> Failures = new();
    private static int _checks;

    [MenuItem("Tools/TimeDesk/Simulate Full Day One")]
    public static void Run()
    {
        Failures.Clear();
        _checks = 0;

        var runConfig = Resources.Load<RunConfigSO>("RunConfig");
        if (runConfig == null || runConfig.contentLibrary == null || runConfig.gameConfig == null)
        {
            Debug.LogError("[DayOneSim] RunConfig/library/config missing — cannot simulate.");
            return;
        }

        ContentLibrarySO lib = runConfig.contentLibrary;
        GameConfigSO config = runConfig.gameConfig;
        DayPlanSO plan = lib.GetDayPlan(1);

        // ---------------------------------------------------------------
        // Evidence-gate unit behaviour first (the V3 lesson).
        // ---------------------------------------------------------------
        Random.InitState(1234);
        var gateWorld = FreshWorld();
        List<CaseInstance> gateCases = new CaseFactory(lib).GenerateDayCases(plan, gateWorld);
        CaseInstance v3 = gateCases[2];

        // Proven denial: register the currency mismatch, then deny with 1 evidence.
        var log = new DiscrepancyLog();
        Discrepancy found = log.TryRegister(
            CompareEvidence.ForReferenceEntry(ClueCategory.Currency, "Goldmark", v3.claimedNation.id, v3.claimedEra.id, "Imperial Germany"),
            CompareEvidence.FromDocumentField(FieldOf(v3, ClueCategory.Currency)),
            v3.claimedNation.id, v3.claimedEra.id);
        IsTrue(found != null, "V3 currency compare registers a ClaimMismatch discrepancy");
        IsTrue(found != null && found.provedBy == DiscrepancyProof.ClaimMismatch, "V3 proof style is ClaimMismatch");

        CaseVerdict provenDeny = ShiftScoring.ResolveDecision(v3, false, 3, gateWorld, config, lib, log.Count);
        IsTrue(provenDeny.correct, "V3 deny WITH evidence is correct");
        IsTrue(provenDeny.payAwarded == config.basePayPerCorrect, $"V3 proven deny pays base ({provenDeny.payAwarded})");

        // Unproven denial: same lie, empty log.
        var gutWorld = FreshWorld();
        CaseVerdict gutDeny = ShiftScoring.ResolveDecision(v3, false, 3, gutWorld, config, lib, 0);
        IsTrue(gutDenialWrong(gutDeny), "V3 deny WITHOUT evidence is the unproven-denial lesson");
        IsTrue(gutDeny.unprovenDenial, "V3 gut-deny flagged unprovenDenial");
        IsTrue(gutDeny.wasFreeWarning && gutDeny.moneyPenalty == 0, "First unproven denial is a free warning (no pay loss)");

        // Record proof (the V6 lesson) against the real registry.
        CitizenRegistry registry = CaseFactory.BuildRegistry(gateCases);
        CitizenRecord emi = registry.Find("Emi Asakura");
        IsTrue(emi != null, "Emi Asakura is in the citizen registry");
        var recordLog = new DiscrepancyLog();
        Discrepancy recordHit = recordLog.TryRegister(
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, emi.birthDate),
            CompareEvidence.FromDocumentField(FieldOf(gateCases[5], ClueCategory.BirthDate)),
            gateCases[5].claimedNation.id, gateCases[5].claimedEra.id);
        IsTrue(recordHit != null && recordHit.provedBy == DiscrepancyProof.RecordMismatch, "V6 DOB vs citizen record registers a RecordMismatch");

        // ---------------------------------------------------------------
        // Scenario A: perfect day (all six correct, evidence used).
        // ---------------------------------------------------------------
        WorldState a = PlayDay(lib, config, plan, seed: 101, mercy: false, mistakes: 0);
        IsTrue(a.money == 60, $"Perfect day shift pay = 60 (got {a.money})");
        IsTrue(a.citationsToday == 0, "Perfect day has no citations");
        IsTrue(Mathf.Approximately(a.timelineStability, 100f), $"Perfect day stability 100 (got {a.timelineStability})");
        IsTrue(EndingService.Evaluate(a, lib, config) == null, "Day 1 never ends the run (gate)");
        var report = HomeEconomy.ApplyDailyExpenses(a, config);
        IsTrue(report.total == 30, $"Evening expenses total 30 (got {report.total}: base {report.baseAmount} + {report.memberAmount} members + {report.conditionAmount} conditions)");
        IsTrue(a.money == 30, $"After bills: 30 (got {a.money}) — Lexicon(12) yes, Wall Material(35) no");
        UpgradeSO lexicon = FindUpgrade(lib, "lexicon");
        UpgradeSO material = FindUpgrade(lib, "material");
        IsTrue(lexicon != null && lexicon.cost == 12 && a.money >= lexicon.cost, "Lexicon affordable after a clean day");
        IsTrue(material != null && material.cost == 35 && a.money < material.cost, "Wall Material out of reach even on a perfect day");

        // ---------------------------------------------------------------
        // Scenario B: mercy day (all correct EXCEPT accepting the sibling).
        // ---------------------------------------------------------------
        WorldState b = PlayDay(lib, config, plan, seed: 202, mercy: true, mistakes: 0);
        IsTrue(b.money == 50, $"Mercy day pay = 50 (got {b.money})");
        IsTrue(b.citationsToday == 1, "Mercy is one wrong verdict (free warning)");
        IsTrue(Mathf.Approximately(b.timelineStability, 95f), $"Mercy stability -5 (got {b.timelineStability})");
        HomeEconomy.ApplyDailyExpenses(b, config);
        IsTrue(b.money == 20, $"Mercy evening: 20 left (got {b.money})");
        float bKinship = b.timeline.GetScore(TimelineKeys.GlobalAttr(FindAttr(lib, "kinship")));
        IsTrue(Mathf.Approximately(bKinship, 8f), $"Mercy puts Kinship at +8 (got {bKinship})");

        // Scenario B2: mercy + one earlier mistake = the design's 'mercy costs the Lexicon' column.
        WorldState b2 = PlayDay(lib, config, plan, seed: 203, mercy: true, mistakes: 1);
        HomeEconomy.ApplyDailyExpenses(b2, config);
        IsTrue(b2.money == 10, $"Mercy + one mistake: 10 left, Lexicon out of reach (got {b2.money})");

        // ---------------------------------------------------------------
        // Scenario C: disaster day (every verdict wrong).
        // ---------------------------------------------------------------
        WorldState c = PlayDay(lib, config, plan, seed: 303, mercy: true, mistakes: 6);
        // 6 wrong: 2 free + 4 citations (10+15+20+20) = -65; stability -30.
        IsTrue(c.citationsToday == 6, $"Disaster day cites all six (got {c.citationsToday})");
        IsTrue(c.money == -65, $"Disaster day money -65 (got {c.money})");
        IsTrue(Mathf.Approximately(c.timelineStability, 70f), $"Disaster stability 70 (got {c.timelineStability})");
        IsTrue(EndingService.Evaluate(c, lib, config) == null, "Even a disaster Day 1 cannot end the run");
        HomeEconomy.ApplyDailyExpenses(c, config);
        IsTrue(c.money == -95, $"Disaster after bills: -95 debt, above bankruptcy floor (got {c.money})");

        // ---------------------------------------------------------------
        // Sleep: nightly resolve, day advance, and the Day-2 wall.
        // ---------------------------------------------------------------
        WorldState sleep = PlayDay(lib, config, plan, seed: 404, mercy: true, mistakes: 0);
        HomeEconomy.ApplyDailyExpenses(sleep, config);
        HomeEconomy.AdvanceFamilyConditions(sleep, config, seed: 7);
        TimelineService.NightlyResolve(sleep, lib, config);
        sleep.day++;
        sleep.citationsToday = 0;
        IsTrue(sleep.day == 2, "Run advances to day 2");
        IsTrue(ScoreRanking.TryGetTop(Pairs(sleep.timeline.scores), RankCategory.TopAttribute, config.rankedLayerMinScore, out RankedScore mercyTop)
               && mercyTop.id == "kinship",
               $"Day-2 wall after mercy shows kinship (got '{(mercyTop.id != null ? mercyTop.id : "none")}' @{mercyTop.value:0.#})");

        WorldState clean = PlayDay(lib, config, plan, seed: 405, mercy: false, mistakes: 0);
        HomeEconomy.ApplyDailyExpenses(clean, config);
        TimelineService.NightlyResolve(clean, lib, config);
        clean.day++;
        float cleanKinship = clean.timeline.GetScore(TimelineKeys.GlobalAttr(FindAttr(lib, "kinship")));
        IsTrue(Mathf.Approximately(cleanKinship, 0f), $"Day-2 wall after clean deny has NO kinship trace (got {cleanKinship})");

        // Day 2: the ending gate opens (config controls it).
        WorldState day2 = sleep;
        EndingSO gateCheck = EndingService.Evaluate(day2, lib, config);
        IsTrue(gateCheck == null || gateCheck.id != "bankrupt", "Day-2 gate open but -95 is above bankruptcy (-100) — run continues");

        // ---------------------------------------------------------------
        // Slot machine: cost + statistical weights.
        // ---------------------------------------------------------------
        IsTrue(config.slotSpinCost == 3, $"Spin costs 3 (got {config.slotSpinCost})");
        Dictionary<string, int> counts = new();
        int spins = 2000;
        for (int i = 0; i < spins; i++)
        {
            SlotOutcomeSO outcome = WeightedRandom.Pick(lib.SlotOutcomes, o => o != null ? o.weight : 0f);
            if (outcome != null)
            {
                counts.TryGetValue(outcome.id, out int n);
                counts[outcome.id] = n + 1;
            }
        }
        IsTrue(counts.TryGetValue("neutral_spin", out int neutral) && neutral >= spins * 0.45f, $"Neutral outcome dominates (~55%, got {neutral}/{spins})");
        IsTrue(!counts.ContainsKey("jackpot_cash"), "Jackpot is parked (weight 0) for Day 1");
        int overtime = counts.TryGetValue("overtime_pay", out int ot) ? ot : 0;
        IsTrue(overtime <= spins * 0.09f, $"Overtime rare (~5%, got {overtime}/{spins})");

        // ---------------------------------------------------------------
        if (Failures.Count == 0)
        {
            Debug.Log($"[DayOneSim] PASS — all {_checks} checks green across evidence gate, 4 day scenarios, sleep/wall, evening economy, and slots.");
        }
        else
        {
            foreach (string failure in Failures)
                Debug.LogError($"[DayOneSim] FAIL — {failure}");
            Debug.LogError($"[DayOneSim] {Failures.Count}/{_checks} checks failed.");
        }
    }

    /// <summary>Plays Day 1 with the given posture and returns the post-shift world.</summary>
    private static WorldState PlayDay(ContentLibrarySO lib, GameConfigSO config, DayPlanSO plan, int seed, bool mercy, int mistakes)
    {
        Random.InitState(seed);
        var world = FreshWorld();
        var factory = new CaseFactory(lib);
        List<CaseInstance> cases = factory.GenerateDayCases(plan, world);

        int wrongBudget = mistakes;
        int moneyBefore = world.money;

        for (int i = 0; i < cases.Count; i++)
        {
            CaseInstance inst = cases[i];
            int slot = i + 1;
            bool isV6 = slot == 6;
            bool shouldAccept = inst.ShouldAccept;

            // Mercy: accept V6 regardless. Mistakes: invert verdicts while budget lasts.
            bool accepted;
            if (isV6 && mercy)
                accepted = true;
            else if (wrongBudget > 0)
            {
                accepted = !shouldAccept;
                wrongBudget--;
            }
            else
                accepted = shouldAccept;

            // Evidence: correct denials of forgers carry proof; wrong denials don't.
            int evidence = 0;
            if (!accepted && inst.isForged && accepted == shouldAccept)
            {
                var log = new DiscrepancyLog();
                DocumentField lie = FirstAnachronism(inst);
                if (lie != null)
                {
                    if (lie.category == ClueCategory.BirthDate)
                    {
                        CitizenRecord rec = CaseFactory.BuildRegistry(cases).Find(inst.visitorGivenName);
                        log.TryRegister(CompareEvidence.ForRecordField(ClueCategory.BirthDate, rec.birthDate),
                            CompareEvidence.FromDocumentField(lie), inst.claimedNation.id, inst.claimedEra.id);
                    }
                    else
                    {
                        ReferenceBookSO book = lib.GetReferenceBook(lie.category);
                        log.TryRegister(CompareEvidence.ForReferenceEntry(lie.category, book.GetValue(inst.claimedNation, inst.claimedEra), inst.claimedNation.id, inst.claimedEra.id, inst.claimedEra.displayName),
                            CompareEvidence.FromDocumentField(lie), inst.claimedNation.id, inst.claimedEra.id);
                    }
                }
                evidence = log.Count;
            }

            CaseVerdict verdict = ShiftScoring.ResolveDecision(inst, accepted, slot, world, config, lib, evidence);
            if (accepted)
                TimelineService.ApplyVerdictImpacts(inst, inst.claimedEra, verdict.correct, world, lib);
        }

        return world;
    }

    private static WorldState FreshWorld() => new WorldState
    {
        day = 1,
        money = 0,
        timelineStability = 100f,
        runSeed = 99,
        family = { members = { new FamilyMemberData { name = "Mother" }, new FamilyMemberData { name = "Brother" } } }
    };

    private static bool gutDenialWrong(CaseVerdict v) => !v.correct;

    private static DocumentField FieldOf(CaseInstance inst, ClueCategory category)
    {
        foreach (DocumentInstance doc in inst.documents)
            foreach (DocumentField f in doc.fields)
                if (f != null && f.category == category)
                    return f;
        return null;
    }

    private static DocumentField FirstAnachronism(CaseInstance inst)
    {
        foreach (DocumentInstance doc in inst.documents)
            foreach (DocumentField f in doc.fields)
                if (f != null && f.isAnachronism)
                    return f;
        return null;
    }

    private static UpgradeSO FindUpgrade(ContentLibrarySO lib, string id)
    {
        foreach (UpgradeSO u in lib.Upgrades)
            if (u != null && u.id == id)
                return u;
        return null;
    }

    private static AttributeSO FindAttr(ContentLibrarySO lib, string id)
    {
        foreach (AttributeSO attr in lib.Attributes)
            if (attr != null && attr.id == id)
                return attr;
        return null;
    }

    private static IEnumerable<KeyValuePair<string, float>> Pairs(List<ScoreEntry> scores)
    {
        foreach (ScoreEntry e in scores)
            if (e != null)
                yield return new KeyValuePair<string, float>(e.key, e.value);
    }

    private static void IsTrue(bool condition, string label)
    {
        _checks++;
        if (condition)
            Debug.Log($"[DayOneSim] ok — {label}");
        else
            Failures.Add(label);
    }
}
