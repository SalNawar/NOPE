using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Day One spine validation (implementation plan Tasks 4-5): generates the
/// six-slot day across many seeds and asserts the designed shape holds —
/// anchors exact, pools inside their constraints, V6's citizen record real,
/// mercy impact sized to win the ranked board. Fails loudly per the manifesto.
/// </summary>
public static class DayOneValidation
{
    private const int SeedCount = 25;
    private static readonly List<string> Failures = new();

    [MenuItem("Tools/TimeDesk/Validate Day One Spine")]
    public static void Run()
    {
        Failures.Clear();

        var runConfig = Resources.Load<RunConfigSO>("RunConfig");
        if (runConfig == null || runConfig.contentLibrary == null)
        {
            Debug.LogError("[DayOneValidation] RunConfig or its ContentLibrary is missing — cannot validate.");
            return;
        }

        ContentLibrarySO lib = runConfig.contentLibrary;
        GameConfigSO config = runConfig.gameConfig;

        // Day 1 must resolve to the investigation plan after the renumbering fix.
        DayPlanSO plan = lib.GetDayPlan(1);
        if (plan == null || plan.name != "DayPlan_Inv_Day1")
            Fail($"GetDayPlan(1) returned '{(plan != null ? plan.name : "null")}' — expected DayPlan_Inv_Day1 (duplicate day-number shadowing?).");
        if (plan != null && plan.VisitorsCount != 6)
            Fail($"DayPlan_Inv_Day1 has {plan.VisitorsCount} visitors — expected 6.");

        int poolBForged = 0;
        int poolBHonest = 0;

        for (int seed = 1; seed <= SeedCount; seed++)
        {
            Random.InitState(seed * 7919);
            var world = new WorldState { day = 1 };
            var factory = new CaseFactory(lib);
            List<CaseInstance> cases = factory.GenerateDayCases(plan, world);

            if (cases.Count != 6)
            {
                Fail($"Seed {seed}: {cases.Count} cases generated — expected 6.");
                continue;
            }

            // --- V1: honest Greek anchor ---
            CaseInstance v1 = cases[0];
            Check("V1 name", "Dimitra Kanellos", v1.visitorGivenName, seed);
            Check("V1 era", "Greece", v1.trueEra != null ? v1.trueEra.id : "null", seed);
            if (v1.isForged) Fail($"Seed {seed}: V1 is forged — the safe case must be clean.");
            if (!v1.ShouldAccept) Fail($"Seed {seed}: V1 ShouldAccept is false — the safe case must be acceptable.");
            if (v1.nation == null || v1.nation.id != "greece") Fail($"Seed {seed}: V1 nation is '{(v1.nation != null ? v1.nation.id : "null")}' — expected greece.");
            if (v1.archetype == null || v1.archetype.id != "artist") Fail($"Seed {seed}: V1 archetype is '{(v1.archetype != null ? v1.archetype.id : "null")}' — expected artist.");

            // --- V3: the first liar ---
            CaseInstance v3 = cases[2];
            Check("V3 name", "Klaus Reinhardt", v3.visitorGivenName, seed);
            Check("V3 era", "NGermany", v3.trueEra != null ? v3.trueEra.id : "null", seed);
            if (!v3.isForged) Fail($"Seed {seed}: V3 is not forged — the currency lie is missing.");
            if (v3.ShouldAccept) Fail($"Seed {seed}: V3 ShouldAccept is true — a forger must be deniable.");
            Check("V3 forged value", "Drachma", FindFieldValue(v3, ClueCategory.Currency), seed);
            if (!IsFieldAnachronism(v3, ClueCategory.Currency)) Fail($"Seed {seed}: V3 currency field is not flagged isAnachronism.");
            // Provable both ways: mismatch vs Germany's Goldmark, foreign-origin vs Greece's Drachma.
            ReferenceBookSO currencyBook = lib.GetReferenceBook(ClueCategory.Currency);
            if (currencyBook == null || currencyBook.GetValue(v3.claimedNation, v3.claimedEra) != "Goldmark")
                Fail($"Seed {seed}: Currency reference book lacks the Germany/Imperial truth — V3 not provable by mismatch.");

            // --- V6: the sibling ---
            CaseInstance v6 = cases[5];
            Check("V6 name", "Emi Asakura", v6.visitorGivenName, seed);
            if (v6.archetype == null || v6.archetype.id != "guardian") Fail($"Seed {seed}: V6 archetype is '{(v6.archetype != null ? v6.archetype.id : "null")}' — expected guardian (Ward).");
            Check("V6 era", "Japan", v6.trueEra != null ? v6.trueEra.id : "null", seed);
            if (!v6.isForged) Fail($"Seed {seed}: V6 is not forged — the DOB lie is missing.");
            Check("V6 forged DOB", "3 Mar 1899", FindFieldValue(v6, ClueCategory.BirthDate), seed);
            Check("V6 true DOB", "3 Mar 1908", v6.trueBirthDate, seed);
            if (!v6.claimAllowedByRules) Fail($"Seed {seed}: V6 destination violates the travel rule — Japan must stay legal (Egypt is the forbidden pair).");
            if (!v6.ShouldAccept) { /* expected: forged */ } else Fail($"Seed {seed}: V6 ShouldAccept is true — deny must be the correct verdict.");
            bool guardianshipSeen = false;
            foreach (ClueSO clue in v6.usedClues)
                if (clue != null && clue.name == "Clue_GuardianshipForm") guardianshipSeen = true;
            if (!guardianshipSeen) Fail($"Seed {seed}: V6 does not carry the guardianship form clue — the paper story is missing.");

            // Hard requirement: Emi's citizen record exists and carries the true DOB.
            CitizenRegistry registry = CaseFactory.BuildRegistry(cases);
            CitizenRecord emi = registry.Find("Emi Asakura");
            if (emi == null) Fail($"Seed {seed}: Emi Asakura has no citizen record — record proof silently degrades (the known risk).");
            else if (emi.birthDate != "3 Mar 1908") Fail($"Seed {seed}: Emi's record DOB is '{emi.birthDate}' — expected 3 Mar 1908.");
            CitizenRecord dimitra = registry.Find("Dimitra Kanellos");
            if (dimitra == null) Fail($"Seed {seed}: Dimitra Kanellos has no citizen record — V1 identity check impossible.");

            // --- Slot 2: Pool A (quiet morning) ---
            CaseInstance v2 = cases[1];
            if (v2.isForged) Fail($"Seed {seed}: Pool A forged a case — the practice slot must be 100% honest.");
            if (!v2.ShouldAccept) Fail($"Seed {seed}: Pool A ShouldAccept is false — honest + permitted must be acceptable.");
            CheckEraIn(v2, "Pool A", new[] { "Greece", "Japan", "China" }, seed);

            // --- Slot 4: Pool B (eyes vs machine) ---
            CaseInstance v4 = cases[3];
            if (v4.isForged) poolBForged++; else poolBHonest++;
            CheckEraIn(v4, "Pool B", new[] { "Greece", "NGermany", "Japan", "China" }, seed);

            // --- Slot 5: Pool C (the directive) ---
            CaseInstance v5 = cases[4];
            if (v5.isForged) Fail($"Seed {seed}: Pool C forged a case — the pressure is the rule, not the lie.");
            if (v5.claimAllowedByRules) Fail($"Seed {seed}: Pool C claim is allowed — destination must hit the forbidden Egypt pair.");
            if (v5.ShouldAccept) Fail($"Seed {seed}: Pool C ShouldAccept is true — directive denial is the correct verdict.");
            if (v5.nation == null || v5.nation.id != "egypt") Fail($"Seed {seed}: Pool C nation is '{(v5.nation != null ? v5.nation.id : "null")}' — expected egypt.");

            // Every case has a claim line + populated fields.
            for (int i = 0; i < cases.Count; i++)
            {
                if (string.IsNullOrEmpty(cases[i].claimLine)) Fail($"Seed {seed}: case {i + 1} has no claim line.");
                if (!HasAnyField(cases[i])) Fail($"Seed {seed}: case {i + 1} has no document fields.");
            }
        }

        // Pool B ratio sanity: 30% forged over the sample, accept 10%-60% band.
        float forgedRatio = poolBForged / (float)(poolBForged + poolBHonest);
        if (forgedRatio < 0.10f || forgedRatio > 0.60f)
            Fail($"Pool B forged ratio is {forgedRatio:P0} ({poolBForged}/{poolBForged + poolBHonest}) — expected ~30% inside the 10-60% band.");

        // --- Mercy impact sizing (the wall trace) ---
        if (config != null)
        {
            Random.InitState(42);
            var world = new WorldState { day = 1 };
            var factory = new CaseFactory(lib);
            List<CaseInstance> cases = factory.GenerateDayCases(plan, world);

            // Typical day: accept V1, deny V3, V5, V6 (correct), accept V2.
            TimelineService.ApplyVerdictImpacts(cases[0], cases[0].claimedEra, true, world, lib);
            TimelineService.ApplyVerdictImpacts(cases[1], cases[1].claimedEra, true, world, lib);

            float kinshipBefore = world.timeline.GetScore(TimelineKeys.GlobalAttr(lib != null ? FindAttr(lib, "kinship") : null));
            float artScore = world.timeline.GetScore(TimelineKeys.GlobalAttr(FindAttr(lib, "art")));
            if (Mathf.Approximately(artScore, 0f)) Fail("Clean-day check: honest accepts did not move Art — V1's +2 is not landing.");

            // Now the mercy: accept V6 (wrong verdict).
            TimelineService.ApplyVerdictImpacts(cases[5], cases[5].claimedEra, false, world, lib);
            AttributeSO kinship = FindAttr(lib, "kinship");
            float kinshipAfter = world.timeline.GetScore(TimelineKeys.GlobalAttr(kinship));
            if (!Mathf.Approximately(kinshipAfter - kinshipBefore, 8f))
                Fail($"Mercy impact moved Kinship by {kinshipAfter - kinshipBefore} — expected exactly +8.");

            // Kinship must win the ranked attribute board after the mercy day.
            if (ScoreRanking.TryGetTop(ToPairs(world.timeline.scores), RankCategory.TopAttribute, config.rankedLayerMinScore, out RankedScore top))
            {
                if (top.id != "kinship")
                    Fail($"Top attribute after a mercy day is '{top.id}' ({top.value:0.##}) — kinship must win the Day-1 board.");
                else
                    Debug.Log($"[DayOneValidation] Mercy wall-trace OK: kinship {top.value:0.##} tops the attribute board.");
            }
            else
            {
                Fail("Top attribute ranking found no winner after a mercy day — the wall trace cannot render.");
            }

            // Clean day (deny V6): kinship must NOT appear above the floor.
            var clean = new WorldState { day = 1 };
            TimelineService.ApplyVerdictImpacts(cases[0], cases[0].claimedEra, true, clean, lib);
            TimelineService.ApplyVerdictImpacts(cases[1], cases[1].claimedEra, true, clean, lib);
            float cleanKinship = clean.timeline.GetScore(TimelineKeys.GlobalAttr(kinship));
            if (!Mathf.Approximately(cleanKinship, 0f))
                Fail($"Clean day (deny V6) left Kinship at {cleanKinship} — the absence half of the beat is broken.");
        }

        // --- Chrono Converter smoke check (era calendars + span rules) ---
        EraSO greece = FindEra(lib, "Greece");
        EraSO japan = FindEra(lib, "Japan");
        IsTrue(greece != null && greece.HasCalendar, "Greece era carries calendar data");
        IsTrue(japan != null && japan.HasCalendar, "Japan era carries calendar data");

        if (greece != null && japan != null)
        {
            // V1's pinned date parses as an Attic month and sits inside the span.
            IsTrue(CalendarConverter.TryParse("21 Elaphebolion 437", greece.CalendarMonths, out CalendarConverter.ParsedDate g),
                "V1 date parses against the Attic calendar");
            IsTrue(g.nativeMonth, "Elaphebolion is a native Greek month");
            IsTrue(CalendarConverter.WithinSpan(CalendarConverter.ToModernYear(g.year, greece.CalendarYearsAreBCE), greece.CalendarSpanStart, greece.CalendarSpanEnd),
                "V1's 437 BCE falls inside the Classical Greece span");

            // V6's two dates parse in Japan's reckoning and differ by exactly 9 years.
            IsTrue(CalendarConverter.TryParse("3 Mar 1908", japan.CalendarMonths, out CalendarConverter.ParsedDate trueD), "V6 true DOB parses");
            IsTrue(CalendarConverter.TryParse("3 Mar 1899", japan.CalendarMonths, out CalendarConverter.ParsedDate forgedD), "V6 forged DOB parses");
            IsTrue(CalendarConverter.ToModernYear(trueD.year, false) - CalendarConverter.ToModernYear(forgedD.year, false) == 9,
                "the forged DOB ages the passport by exactly 9 years (modern reckoning)");

            // A modern year is not classical — the span separates eras.
            IsTrue(!CalendarConverter.WithinSpan(1908, greece.CalendarSpanStart, greece.CalendarSpanEnd),
                "a 1908 CE date does not belong to Classical Greece");
        }

        if (Failures.Count == 0)
        {
            Debug.Log($"[DayOneValidation] PASS — Day 1 spine holds across {SeedCount} seeds. Pool B forged ratio {forgedRatio:P0}.");
        }
        else
        {
            foreach (string failure in Failures)
                Debug.LogError($"[DayOneValidation] FAIL — {failure}");
            Debug.LogError($"[DayOneValidation] {Failures.Count} failure(s). Day 1 is NOT shippable in this state.");
        }
    }

    private static void Check(string what, string expected, string actual, int seed)
    {
        if (!string.Equals(expected, actual))
            Fail($"Seed {seed}: {what} is '{actual}' — expected '{expected}'.");
    }

    private static void CheckEraIn(CaseInstance inst, string pool, string[] eraIds, int seed)
    {
        string id = inst.trueEra != null ? inst.trueEra.id : "null";
        if (System.Array.IndexOf(eraIds, id) < 0)
            Fail($"Seed {seed}: {pool} era is '{id}' — outside its candidate set [{string.Join(", ", eraIds)}].");
    }

    private static string FindFieldValue(CaseInstance inst, ClueCategory category)
    {
        foreach (DocumentInstance doc in inst.documents)
            foreach (DocumentField field in doc.fields)
                if (field != null && field.category == category)
                    return field.value;
        return "<missing>";
    }

    private static bool IsFieldAnachronism(CaseInstance inst, ClueCategory category)
    {
        foreach (DocumentInstance doc in inst.documents)
            foreach (DocumentField field in doc.fields)
                if (field != null && field.category == category)
                    return field.isAnachronism;
        return false;
    }

    private static bool HasAnyField(CaseInstance inst)
    {
        foreach (DocumentInstance doc in inst.documents)
            if (doc.fields.Count > 0)
                return true;
        return false;
    }

    private static EraSO FindEra(ContentLibrarySO lib, string id)
    {
        foreach (EraSO era in lib.Eras)
            if (era != null && era.id == id)
                return era;
        return null;
    }

    private static AttributeSO FindAttr(ContentLibrarySO lib, string id)
    {
        foreach (AttributeSO attr in lib.Attributes)
            if (attr != null && attr.id == id)
                return attr;
        return null;
    }

    private static IEnumerable<KeyValuePair<string, float>> ToPairs(List<ScoreEntry> scores)
    {
        foreach (ScoreEntry entry in scores)
            if (entry != null)
                yield return new KeyValuePair<string, float>(entry.key, entry.value);
    }

    private static void IsTrue(bool condition, string label)
    {
        if (!condition)
            Failures.Add(label);
    }

    private static void Fail(string message) => Failures.Add(message);
}
