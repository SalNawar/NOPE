# History changes facts: design (piece 5)

*2026-09-24 · decisions made by Claude under Saleh's instruction "go with all pieces, don't stop" (2026-09-24), open to his review · promoted 2026-09-25 · branch `feat/history-facts` from `main` at `1a3e624` (pieces 0–3 and 7; piece 4 is built in parallel on its own branch)*

Saleh: "changing the timeline should change info too for example if china becomes too dominant in the past maybe the currency for the future becomes chinese. an invention can change era and country based on your choices".

The player's sends now rewrite the world. Every night the nations are ranked by the influence the player's accepted travellers left in their past, and the nation that clearly leads becomes the **present culture**: from the next day the Future is that nation's Future place, with its own currency, language, device, capital and ruler on every paper and in every book (a Chinese lead means the Future pays in digital yuan). Inventions move too: a liar the player lets through carries their true home's device into the place they claimed, and authored history rules rewrite a place's facts when history reaches a condition (China leads, so Florence prints with Chinese movable type). Every change is permanent except the leader, which can be overtaken, and every change is announced in the morning paper. Days 4–6 open the Industrial, Modern and Future eras so the changes can be seen, and Continue after the end of a shift now resumes at Home instead of replaying the day.

Line numbers refer to `efe385d` on `feat/identity-lies`, where the draft was written; they are indicative only. The anchors are the named members, re-checked on 2026-09-25 against `main` at `1a3e624` (pieces 0–3 and the physical desk, piece 7, as implemented); the drift found is listed in §9. **Piece 4 (characters) is not in this code**: it is being built in parallel, so every part of this spec that needs piece 4 (Future wardrobes, `artNation`, the premade rules, the Appearance channel on days 4–6, the Culture-width move) is marked "after piece 4 merges" and collected in §8, to be applied when the two branches meet. This spec builds on:
- piece 1: `FactTable`, `OriginLabels`, `ContentLibrarySO.TodaysProfiles`, the generator and validator, `Seeds`;
- piece 2 as implemented: `CaseInstance.trueHome`/`trueHomeLabel`/`IsLiar`/`HomeLabel`, `CaseFactory.Disguise`/`PlaceLabel`, `Lies.Plan`/`LiePlan`, `Forgery.IsProvableTell` (R13 uniqueness), `DiscrepancyLog.ValuesMatch` (internal);
- piece 3 as implemented (its spec's names hold, except where §9 says otherwise): Domain `Gates.cs` (`TriggerConditionType` moved there with `UpgradeOwned` appended, `GateCondition`, `GateSnapshot`, `Gates.Passes`/`AllPass`/`UnlockNight`, and the run-flag grammar `FlagKeys.TriggerFired`/`FlagKeys.DialogDone`), Domain `EffectOps.cs` (`EffectOpType` moved there unchanged, `EffectOps.ActsWhileActive`, R24 there), `TimelineService.ToGate`/`ToGates`/`Snapshot` (its V3 retired `ConditionsPass`), `InterviewContent.LineText`, `Interview.Fill`/`ValueToken`/`PlaceToken`/`WorstCaseLength`, the generator's ASCII check for authored text (R18 there), `ConditionData`, `WorldContentGenerator` owning `Interview` and merging triggers by folder, `GameManager.ApplyDialogOutcomes` (X5: dialog effects at the end of the shift, before the save), answers computed from the `FactTable` (X7), `days[].channels`, `RefBook_Capital`/`RefBook_Ruler`, and world_source.json hand-maintained (X10);
- piece 4 as its draft spec defines it: the per-place `wardrobe` block in world_source.json, the Culture fact **derived** from the wardrobe's signature items by the generator (never authored, R1 there), `LookData`/`Looks`/`LookKeys` (Domain), the Appearance tell channel from day 3 (C3), premades, and its contract for piece 5 (R27 and §2.20 there) **[after piece 4 merges, §8]**.

Where the implemented pieces differ from their specs, the implemented names win (§9); the contracts in §2.16 stay. Amendments to the binding decisions: **A1** (piece 5 has no booth change; any builder change follows existing-wins) is superseded by **A2**: the live `OfficeScene.unity` is builder-built, piece 5 changes no builder and rebuilds no scene, and `OfficeScene_HybridArt.unity` is never modified.

## 0. Decisions

Claude made these under Saleh's instruction "go with all pieces, don't stop" (2026-09-24), from the piece-5 code analysis (session scratchpad `piece5_map.json`: its decisions H1–H20, its completeness check's extra decisions Y1–Y11 and its list of missing items) and `piece5_decisions.md`. They bind this piece and are open to Saleh's review. The R rows under the table are the details this spec settles where a decision left them open, including refinements of a decision (R1, R2, R5, R6). R18–R22 were added by the independent review (Review notes).

| Id | Decision | Rationale |
|---|---|---|
| H1 | **One persisted history block**, `WorldState.history` (Domain type `HistoryState`): the Future leader, the ranking behind it, the latched fact edits and the pending carries. Facts are resolved only from night-latched state (the leader and the latched edits), never from live scores. Fact edits are latched only at night, by authored rules fired as triggers and by carry promotion; the leader is latched only at night. Pending carries are recorded at accept (like scores) and read only at night. | One store and one resolver keep the fact build the only fact source. Night-latched facts are the same for every scene load of a day. `WorldState` is the only persisted state and uses lists only (JsonUtility), so an empty block is "no history yet" for an old save. |
| H2 | **"Too dominant" = past influence**: the sum of the stored attribute deltas (`attr:{profile}:{attr}` and ad-hoc `attr:{nation}@{era}:{attr}` keys) over a nation's non-Future places, ranked across the 8 nations. A leader needs influence strictly above a floor knob. Per-attribute category leaders are a documented later extension. | It reuses the keys every accept already writes, so it adds no mid-shift write. Deltas only: the authored facts already are the baseline world, and country baselines tie (China = Germany, Science 5.0), so baselines would crown a leader before the player acts. Excluding the Future stops the leader's own Future travellers from entrenching it. |
| H3 | **The Future is one place per day: the leader's `{country}_future`.** Eight Future places are authored in world_source.json (5 facts, 8+8 names, year, birth years, and, after piece 4 merges, a piece-4 `wardrobe` block from which piece 4's generator derives their Culture fact, §8). Future travellers are citizens of the present going home; liars may claim the Future or come from it. **Default (R1): with no leader, nobody travels from the Future.** | One Future row per category per day, so no book value collides (piece-2 R13), and Saleh's example lands exactly: a Chinese lead gives a Chinese Future currency. |
| H4 | **The leader is reversible with hysteresis** (against challengers by a margin, at the floor by a lower keep floor, R2); the incumbent keeps ties (Y8); every change is announced. **Authored rules and carries latch permanently.** | The Future follows whoever dominates, and hysteresis stops one ±2 send from flipping it. One-shot rules and permanent edits are announceable and cheap to validate. |
| H5 | **Carry:** an accepted **liar** brings their **true** home's Technology into the **claimed** place. It is recorded at accept; when the (true home, claim) pair's records reach the carry threshold (knob, default 1) it latches as a permanent edit that night. Violators never carry (Y7). Honest travellers with risky cargo are deferred (Y1). | A liar is exactly a foreign body sent into another era or country (piece-2 D5 allows cross-era homes), so Saleh's "an invention can change era and country based on your choices" gets a systemic carrier with no per-pair authoring, and a mistake becomes a visible, lasting consequence. |
| H6 | **Authored history rules** live in a `history.rules` section of world_source.json. The generator emits a one-shot `TimelineTriggerSO` (conditions, `newsLineOnFire`) per rule, whose outcome `EffectSO` carries the new instant op **`SetFact`** (appended to `EffectOpType` in piece 3's Domain `EffectOps.cs`). `SetFact` may appear only in history-rule effects (Y4, `EffectOps.HistoryOnly`). Conditions go through piece 3's `Gates`; `TriggerConditionType` gains `NationIsLeader`, `GlobalAttrAtLeast`, `GlobalAttrAtMost` (appended). | The trigger engine already has all-conditions-must-pass, one-shot fired flags, news lines and outcome effects; only the fact write is missing (one op and one field). A separate rule engine would repeat what got PR #3 rejected. |
| H7 | **Days 4–6 are authored** (day 4 adds Industrial, day 5 Modern, day 6 the Future) with queue, tell, channel and rule knobs. A day without its own plan reuses the latest earlier plan (replacing the inspector-fallback replay of day 1). | Causality runs forward: day-1 choices show up in later-era facts on later days. Today days 4–15 replay Day 1's Ancient × 4 countries (`GameManager.cs:92-97`), so Industrial, Modern and Future facts are never seen. |
| H8 | **The ranking is implemented once, in Domain:** `ScoreRanking` (a stable ranking; the PR #3 design is followed and credited) is used by the per-place tiers and by the nation leader (Y6). `ScoreKey` (parser ported from PR #3 commit `160afde`, credited) becomes the single home of the key grammar. Note for Saleh: tell Marwan that PR #3's resubmission should build on these helpers and the latched leader. | PR #3's review requires one reconciled ranking inside `NightlyResolve` and the key grammar in one Domain home (`docs/reviews/2026-08-29-pr3-timeline-ranked-office-layers.md`, rework item 1, addendum item 1). PR #3 is not merged, so the code is written here. |
| H9 | **Evidence currency changes in piece 5**: the Future place's Currency fact on papers, books, tells and answers. The wallet label ("credits") changes in piece 6, read from the same resolved facts (§2.16). | The locked decision names papers, books and the forgery pool. The wallet is about 11 hard-coded UI strings, piece 6's UI-language work. |
| H10 | **Book-value uniqueness (piece-2 R13) is proven for authored content** before it ships, by the generator and the validator (R5). At runtime a rule edit that shares a value with another place is logged as a warning (R5). | Authored rules can be proven clean; a carry copies a value by construction, and R13 already degrades safely for it. |
| H11 | **Continue after the end-of-shift save resumes at Home** (additive `WorldState.phase`), not by replaying the Office day. Piece 3's X5 (dialog effects applied at the end of the shift, before the save) stays as the moment effects apply; it is no longer a replay mitigation (Y11). | The replay re-applied scores, counters, pay and would re-apply carries; it also lost the slot modifiers that shaped the original shift (`GameManager.cs:228-233`, `CaseFactory.cs:330-334`). History would turn this double counting into visible world change. One mitigation, owned here. |
| H12 | **History may change any of the five fact categories** (Currency, Language, Technology, Geography, Politics). Origin labels never change. Piece 4's Culture fact is not editable (R12). | Piece 3's capital and ruler books and questions make Geography and Politics edits visible. Labels identify a place for claims, records and the origin proof (piece-2 R14). |
| H13 | **The newest latched edit of a (place, category) wins.** Same-night order: rule edits in library trigger order, then carries in first-record order. The leader step writes no edit (R6). | Deterministic and testable as a decision table; "history keeps moving" reads naturally. |
| H14 | **Each change is announced** in the tomorrow package: the rule's authored `newsLineOnFire`, and templated lines (content, stable ids) for leader changes and carries, capped per night by a knob (R8). Book-row "changed" markers belong to piece 6. | The tomorrow package is the only news surface (`DayFlowUIController.cs:69-109`). The player must learn new values to judge papers fairly. |
| H15 | **Dominance news only for places on tomorrow's plan**, and only for a newly DOMINANT attribute; the "is rising" line is dropped. | All 40 places are ranked nightly and single sends flip rank 1; with history lines added, the paper would flood. The supporting tier stays for triggers. |
| H16 | **Append `GlobalAttrAtLeast`/`AtMost`; retune the three dead triggers and the three score endings** to reachable thresholds; score endings fire at the day boundary, not mid-shift (Y2). | The triggers' descriptions already say "global total", and the validator then checks condition fields so the dead-trigger bug cannot recur. |
| H17 | **History draws no random number.** | The authored rules and the accepts fully determine every change, so no stream shifts. |
| H18 | **One seam:** the fact build resolves history before `FactTable.Add` (`ContentLibrarySO.FillFacts`, used by `BuildToday` and `BuildWorldFacts`, R4). Papers, books, tells, answers and piece 4's outfits all follow. | Locked by piece 1 (`BuildFactTable` was declared the seam piece 5 extends, world-model spec line 39). |
| H19 | **Impacts keep landing on the claimed place**, liar or not. | Locked by piece 2 (identity-lies spec line 102). What an accepted liar does beyond that is the carry (H5). |
| H20 | **Domain rules with decision-table tests; knobs in SOs; content in world_source.json via the generator; save version 2** with an additive block (an old v2 save loads with empty history). | House rules, manifesto Laws 1–3. The change is additive for JsonUtility (missing fields keep their initialisers). |
| Y1 | Honest travellers with risky cargo (an invention outside their era, scored as its own verdict dimension) are **deferred** (§4). | It needs a new document field or category and a scoring rule: piece-2/3 surface. |
| Y2 | **Endings (B):** failure endings (fired, bankrupt) stay immediate; the Retirement milestone and the attribute endings are checked only at the day boundary (sleep), where an attribute ending is an **epilogue** that replaces Retirement when its total is reached (R10). | With reachable thresholds, today's check after every verdict (`GameManager.cs:439, 521`) would end the run mid-shift ahead of Retirement. |
| Y3 | **Future places stay out of the per-place tiers and their news** (they get no attribute baselines, R3). | One ranking, no flood: on an old save the first night would otherwise announce 8 "DOMINANT" and 16 "rising" lines. |
| Y4 | **`SetFact` only in history-rule effects**, as the tested Domain rule `EffectOps.HistoryOnly` (true for `SetFact` only). Piece 3's generator dialog-effect check rejects a dialog choice naming an effect with such an op, and the validator rejects one in slot-outcome, upgrade, tier, leader and dialog effects and in a trigger that is not one-shot. | `ActivateEffect(applyInstantOps: true)` also runs at Home (`HomeManager.cs:137, 214`) and for piece-3 dialog effects; a fact write there would not be night-latched. `SetFact` is instant, so piece 3's `ActsWhileActive` passes it: without the second rule a dialog could name a generated, library-listed `Effect_History_*` asset. |
| Y5 | **Travel rules keep working on the selected Future place like any place**: era and nation rules apply to whichever Future place is in the world; a nation+era rule may not name the Future (validator). | A rule naming an unselected Future place would forbid nothing and silently go untested (`CaseFactory.cs:121-126`). |
| Y6 | **One Domain ranking module** for the per-place tiers and the cross-nation leader; `TimelineService` becomes thin glue. | World-model spec line 97 hands this to piece 5; PR #3 review item 1 demands one reconciled ranking. |
| Y7 | **Accepted violators have no history consequence** beyond `deltaOnWrong`. | A carry needs a true home that differs from the claim; a violator is honest. |
| Y8 | **Ties never make or unmake a leader**; the incumbent keeps ties (R2). | Explicit and testable; matches "changes only when a challenger leads by the margin". |
| Y9 | **Influence counts only attribute keys**: traveller impacts, and `AddAttributeScore` effects because they write the same keys. `AddNationScore` writes `nation:{id}` and is **not** a lever. | Keeps the lever list consistent with the measure (the analysis's first risk contradicted it). |
| Y10 | **Verification:** Domain decision-table tests, a temporary headless multi-day automation that plays whole runs through the real glue, a play-mode smoke, and a debug "force leader" cheat (§6). | Leader flips, carries and rules take several days of play. |
| Y11 | **Piece 5 owns the Continue fix**; piece 3's end-of-shift timing stays (H11). | Never two replay mitigations. |
| P6 | **Piece 6 contract:** the present culture is `WorldState.history.leaderId` (empty = neutral) with `history.ranking` saved for blends, broadcast as the UI-channel cue `culture:{id}` (grammar: Domain `CultureCue`, R20) by the leader's generated effect (R9); the Future currency for the wallet comes from `BuildWorldFacts` (§2.16). | Piece 6 consumes the saved snapshot through the existing cue pipeline (`TimelineCueReceiver`, `EffectChannel.UI`), as its U1/U2 decisions ask. |

### Refinements settled by this spec

| Id | Refinement | Rationale |
|---|---|---|
| R1 | **No leader, no Future travellers** (refines H3's open default). `TodaysProfiles` includes a Future-era place only for the leader's nation (the tested Domain predicate `History.InWorld`), and `CaseFactory.PickEraFromPlan` gives an era weight only to eras that have a place today, so a Future-weighted day without a leader simply draws no Future traveller. When the leader falls to the floor the Future closes again. | A default country's Future would show one country's facts under piece 4's neutral Future outfit and piece 6's neutral look, and would change facts silently when that country later leads. "The Future is unsettled until someone dominates it" is coherent everywhere. On days 1–5 every weighted era has a place, so the era draw is unchanged (`WeightedRandom.Pick` makes one draw and skips zero weights). |
| R2 | **Leader rule** (`NationLeader.Decide`, ranking highest first, ties in library nation order): with no incumbent (or an incumbent at or below the **keep floor**, or unknown), the top nation leads if its influence is strictly above the floor and strictly above the runner-up's; otherwise nobody leads. An incumbent above the keep floor stays unless the best other nation's influence exceeds the incumbent's by **more than** the margin **and** that challenger is unique (strictly above the next non-incumbent). The keep floor (`GameConfigSO.leaderKeepFloor`, 2) is below the floor (4); a keep floor above the floor counts as the floor. | Hysteresis for changes and at the floor: a first leader at about 4.5 would otherwise lose the lead to one honest Soldier (−1.5) and regain it with the next Artist, each flip announcing, opening or closing the Future and firing that nation's one-shot rule for good. A unique top for a first leader, and a unique challenger for a change, so ties never make or change a leader, even with margin 0 (every delta is a multiple of 0.5, so ties are common). |
| R3 | **Future places have no attribute baselines** (the generator writes none), so `RecomputeDominance` skips them (its existing `baselines.Count == 0` guard, `TimelineService.cs:189`). All eight Future places share the year 2150 (the office's own time; born 2080–2132). | Y3 with no new code path. The year is a narrative call left open for Saleh (§7). |
| R4 | **One "today's world" object.** `ContentLibrarySO.BuildToday(plan, history)` returns a `TodaysWorld` (today's places and their `FactTable`); `CaseFactory` takes it instead of computing `TodaysProfiles` again. `BuildFactTable(plan)` is replaced by `BuildToday`, plus `BuildWorldFacts(history)` (every place, history applied) for the night steps and piece 6. Both go through one private `FillFacts`, the only reader of place facts. They take no day: every latched edit applies from the next day, and every caller builds for that day or later (review F19). | The fact table, the case place pick and the true-home candidates must agree on the Future place; today three callers compute the list independently (`ContentLibrarySO.cs:111`, `CaseFactory.cs:79`, `ContentLibraryValidator.cs:160`). |
| R5 | **H10 as a pairwise proof** (refines "simulate rule combinations"). `HistoryChecks.Problems` rejects an authored value that equals any other place's base value or any other rule's value for another place, in the same category. Pairwise uniqueness holds under every subset and order of rules, so no combination has to be enumerated. Base-content shared values (Iraq Language Early modern = Industrial; "Sultan Mustafa II") are not history's and stay allowed (piece-2 R13 degrades them safely). **The runtime second line warns but does not skip** (refines "skip + warn"): skipping after the trigger fired would leave its news line and fired flag describing an edit that never happened. | Correct by construction, cheaper than simulation, and the critique showed a strict all-duplicates check would fail on existing content. |
| R6 | **The leader step writes no fact edit** (refines H13's "leader → triggers → carries"): it selects the Future place, whose facts are authored. The same-night edit order is rules, then carries. | Nothing else would ever sit in the "leader" slot; per-attribute leaders (D) would reuse it later. |
| R7 | **Carry details.** The carried category is a knob (`GameConfigSO.carryCategory`, Technology; only the five editable categories carry). The value is the true home's value in **today's** fact table (history applied) at accept, stored with its category. A carry whose value already equals the claim's is not recorded; at night a due pair whose value equals the target's current value (after the edits latched earlier in the same promotion) is consumed without an edit. Carries never warn about shared values. `GameConfigSO.OnValidate` warns when `carryCategory` is not editable (a non-editable category would silently disable carries). | A carry copies a value by design, so a shared-value warning would fire for every carry. Two due pairs aimed at the same place and category with the same value latch once and print one line. |
| R8 | **News cap scope.** `GameConfigSO.maxHistoryNewsPerNight` (3) counts the templated lines: the leader line (always first, at most one a night) then carry lines; overflow carry lines are dropped with a log. How many carry lines fit is the tested Domain rule `History.NewsSlots`. A rule's own `newsLineOnFire` is not capped (one-shot, bounded by content). | The leader change matters most; authored lines are few by construction. |
| R9 | **The leader cue comes from a generated effect per nation** (`NationSO.leaderEffect`: channel UI, permanent, `Cue culture:{id}`). The leader step re-activates it every night like `RebuildTierEffects` (source prefix `history:leader`), so the cue always matches `history.leaderId`. | "Tier-style effect" is the path PR #3's review names; `TimelineEffects.GetCues` and `TimelineCueReceiver` need no change, and piece 6 reads the cue on scene load. |
| R10 | **Endings in Domain.** `EndingConditionType` moves to Domain `EndingRules.cs` unchanged (ints kept). `EndingRules.KindOf` maps Fired/Bankrupt → Failure, DayAtLeast → Milestone, AttrTotalAtLeast → Epilogue; `EndingRules.Select(candidates, moment)` picks. The day-boundary check moves from `HomeManager.HandleSleep` into `RunManager.Sleep()`, which the no-Home path of `GoHomeOrAdvance` also calls. Retirement (`DayAtLeast 15`) therefore ends the run after day 15 is played, not after its first verdict. | Y2 needs one tested selection rule, and without the move a build with no Home scene would never reach Retirement. |
| R11 | **Resume detail.** `WorldState.phase` (`RunPhase.Office` / `Home`, serialized as int) is set to Home by the end-of-shift save and back to Office by `AdvanceToNextDay`. `RunManager.ResumeRun()` loads Home (`GoHomeOrAdvance`) or the Office. A save without the field resumes in the Office once (the old replay). Playing OfficeScene directly on a Home-phase save logs a warning (dev only). | Smallest fix; `HomeManager` is replay-safe because Home writes are saved only at sleep and its expenses are day-seeded. |
| R12 | **Value conventions enforced.** The generator rejects any place fact longer than `FactTable.MaxValueLength` (28; all 200 current values pass, the longest is 28) and any history text or value that is blank or non-ASCII (piece 3's R18 check, called, not copied). History-rule values also feed piece 3's transcript line-length check (R10 there): the longest `{value}` of a category covers the rule values of that category. Piece 4's `Culture` fact is not editable: the wardrobe art cannot follow a text edit. | The book row width and the TMP atlas rule (piece-3 R18) become checks instead of conventions. A 28-character Technology rule value is longer than any Technology place value today (27), so without the feed it could overflow a transcript row. |
| R13 | **Conditions by id.** One generator helper resolves `ConditionData` `place`, `attribute` and `nation` ids to `TriggerCondition` references for history rules, questions and dialogs alike (lifting piece-3 R6's rejection). `GateSnapshot` gains `LeaderId`. | Piece 3 deferred exactly this to piece 5; one resolver for every JSON condition. |
| R14 | **First-cut numbers, pending playtest:** leader floor 4, keep floor 2, margin 2, carry threshold 1, history news cap 3; triggers Art ≥ 20, Science ≥ 15, Democracy ≤ −6 from day 3. **Epilogue thresholds are set from the §6 automation's 50-run report before the ending assets are committed**: each is the 65th percentile of perfect play's day-15 total for its attribute, rounded to a whole number (Art 45, Science 32, Democracy 15 are only the values the first report runs with). | Estimates from the archetype deltas (+1.1 influence per honest accept; per accept Art +0.6, Science +0.4, Democracy +0.1 on average) and about 4–5 accepted honest travellers a day. With about 67 accepts, perfect play ends near Art 40 ± 6.5, Science 27 ± 6.5 and Democracy 7 ± 9, so the first-cut epilogues would be reached in only about one run in five, by queue luck (§7). |
| R15 | **Lookups.** `ContentLibrarySO` gains `GetNationById`, `GetProfileById` (in `EnsureLookups`, like `GetEraById`) and `FutureEra`. | PR #3 addendum item 3 forbids ad-hoc scans; the leader, the news and the influence map need them. |
| R16 | **Legacy era-pick path** (`HandlePlayerChoseEra`, Test_DayLoop only): its sends move influence (they write the same score keys) but never carry (that path has no accept). | States the critique's open question. |
| R17 | **Days 7–15 reuse day 6's plan** (the Future stays open once a leader exists). Which plan a day uses (its own, else the latest earlier one, else none) is the tested Domain rule `DayPlans.Pick`. | H7's fallback, made explicit and tested. |
| R18 | **Future art sharing (piece-4 R27, delivered).** **[after piece 4 merges, §8]** Piece 4's `LookItem` gains `artNation`: a key token that replaces the place's nation in the item's file names, so places of one era share a drawing (`hair_{g}_neutral_future_{colour}` for `artNation: "neutral"`). `Looks.Compose` and `LookKeys.Required` use it; `Looks.CanLeak` gains a row that refuses a leak whose item draws the same art as the claim's item in that slot. The eight Future places' hair and facial-hair items carry `"artNation": "neutral"`; their signatures are unshared items (the outfit), so their derived Culture values stay unique. | Piece 4 keeps a neutral Future hair to cut the art list (one hairstyle in five colours per gender instead of eight), and assigns the override and its leak guard to piece 5. The guard keeps a shared drawing from becoming an invisible Appearance tell. A nation token (not a place token) keeps piece 4's `{layer}_{g}_{nation}_{era}` grammar intact. |
| R19 | **The force-leader cheat is sticky for the session.** `DevToolsState.ForcedLeaderId` (session-only, cleared by `ResetAll`): while set, the nightly leader step still ranks and saves `history.ranking` but keeps the forced leader instead of calling `NationLeader.Decide`, and logs that it did. "No leader" clears the override and the leader. Skip Day goes through `RunManager.Sleep()`. | Without it, the next night's `Decide` drops a forced nation at or below the keep floor (always the case on a fresh or test run) and announces "no longer dominates", so the Future never opens and piece 6's force-culture smoke (its U15) cannot work. Skip Day calling `AdvanceToNextDay` directly would skip the day-boundary endings that R10 moves into `Sleep`. |
| R20 | **One home per rule** (MERGE_CRITERIA "zero re-implementation"). The fact width is `FactTable.MaxValueLength` (piece 4's `Looks.MaxCultureLength` is removed and its readers use it, after piece 4 merges, §8). "Another place of this category has this value" is `FactTable.TryFindOtherPlaceWith`, which `Forgery.IsProvableTell` also calls (its loop goes). The place token is piece 3's `Interview.PlaceToken`; the ASCII check is piece 3's R18 helper. The culture cue grammar is Domain `CultureCue` (`Format`/`TryParse`; piece 6 adds `Pick` there). Removing effects by source prefix is `TimelineService.RemoveEffectsFrom`, shared by the tier and leader rebuilds. | PR #3 was rejected for second sources of truth; each of these would otherwise exist twice (in two pieces or two methods). |
| R21 | **History news is English text in v1**, filled at night into `WorldState.tomorrow.newsLines` (piece 6's R2 keeps news English; its F2 would add keyed news). The line ids are content ids, not a localization contract. The leader lines promise no travellers: the leader can change on nights 2–5, before any plan includes the Future. | A structured news save field would have no reader in v1 (dead code); a line saying "From today the Future is …" on day 3 would mislead the player. |
| R22 | **Premades and the Future.** **[after piece 4 merges, §8]** No premade may claim or come from a Future place (generator and validator errors). Days 4–6 carry day 3's premade pool (`premades` = its seven ids, `forced: []`, `premadeChance` 0.05), so premades not met by day 3 can still appear; new Industrial and Modern premades are deferred (§4). | A Future place is in the world only while its nation leads, so a Future premade would silently stay honest (piece-4 R17 fallback) or print placeholder papers; piece 4's static plan check cannot see this. Piece 4's contract asks days 4–6 to author their premade fields. |

## 1. Behaviour

### 1.1 Influence and the leader

- **Influence** of a nation = the sum of every attribute delta stored for its places outside the Future: the deltas accepted travellers left there (`ApplyVerdictImpacts`), plus any `AddAttributeScore` effect. Baselines are not counted. A nation's own `nation:{id}` score is not counted, so `AddNationScore` effects do not move it.
  - An accepted honest Artist, Diplomat or Scientist adds +2 to the claimed place's nation, a Merchant +1, a Soldier −1.5; an accepted liar or violator applies the role's wrong-accept delta instead (−1, −0.5 for a Merchant, +0.5 for a Soldier). Denied travellers change nothing.
- **Every night** (before triggers) the eight nations are ranked by influence, highest first, ties in library nation order (Egypt, Iraq, Greece, Italy, China, Japan, Britain, Germany). The ranking is saved.
- **Who leads** (R2):
  - With no leader, the top nation leads when its influence is above the floor (4) and strictly above the runner-up's.
  - A leader keeps leading until one other nation's influence exceeds its own by more than the margin (2) and is strictly above every other challenger, or until its own influence falls to the keep floor (2) or below (then the no-leader rule applies again that night).
  - A tie never makes or changes a leader: two challengers level with each other never replace the incumbent.
- A new leader, a changed leader or a lost leader makes the morning paper (the lines promise no travellers, R21):
  - "HISTORY: China now dominates the timeline. The Future belongs to Shanghai Megacity (Future)."
  - "HISTORY: China no longer dominates the timeline. The Future is unsettled until a new power rises."
- While a nation leads, its leader effect broadcasts the UI cue `culture:{nation}` (for piece 6; nothing receives it yet).

### 1.2 The Future

- There are 8 Future places, one per country (§2.14), all in 2150, the office's own time. At most one is in the world on any day: **the leader's**, from the day after it starts leading.
- On a day whose plan includes the Future era (day 6 on), the leader's Future place is one of today's places: Future travellers claim it ("I request passage home to Shanghai Megacity (Future)."), its facts fill their papers, and the books list its row. They are citizens of the present going home.
- **No leader:** the Future era draws nobody and the books list no Future row. With a leader, the Future's facts show on every Future traveller's papers, including Currency on both passport and permit (so "Digital yuan (e-CNY)" appears twice, like any currency).
- Liars may claim the Future (their true home is a past place of today) or come from it (their papers can leak, say, "Maglev commuter pod" as a Victorian device, or a 21st-century birth year).
- Rules: "no travel to Modern Japan" never touches the Future; an era rule "no travel to the Future" or a nation rule "no travel to China" would forbid the Future place of that day like any place (none is authored).
- Looks: **[after piece 4 merges, §8]** a Future traveller wears their Future place's culture-shaped outfit (piece 4) and the shared neutral Future hair (R18). No premade claims or comes from the Future (R22).

### 1.3 Carries

- When the player **accepts a liar**, their true home's Technology is recorded as heading for the claimed place: an Ancient Egyptian passing as Periclean Athens carries "Papyrus & reed brush" to Athens. Honest travellers and violators never carry.
- That night, a (true home, claim) pair that has been accepted the carry threshold (1) times latches: from the next day the claimed place's Technology **is** the carried value, everywhere: its book row, honest travellers' papers, answers and tells. The paper says "HISTORY: travellers brought Papyrus & reed brush to Periclean Athens (Ancient)."
- A later carry or rule can overwrite it (the newest edit wins). The true home keeps its own value, so the two places now share it, and neither can leak a Technology tell while both are in the world (piece-2 R13 removes that category for them).

### 1.4 Authored history rules

- Each rule has conditions, one or more fact edits and a news line. At night, after the leader step, a rule whose conditions all pass fires once per run: its edits latch from the next day, and its news line appears.
- Piece-5 content (§2.14): one rule per possible leader, plus three attribute rules. Examples:
  - China leads → Florentine Republic Technology "Chinese movable type press" ("HISTORY: Florentine printers set type the Chinese way, years before Gutenberg.");
  - Science total ≥ 22 → Victorian Britain Technology "Babbage analytical engine";
  - Science dominant in Periclean Athens → its Technology "Aeolipile steam engine".
- Rules fire once even if their condition later stops holding (a lost leader does not undo its rule).

### 1.5 How facts resolve

- Each morning the day's facts are built once: every fact of today's places, with each (place, category) replaced by its newest latched edit. Books, papers, tells (piece 2), answers (piece 3) and outfits' Culture (piece 4, never edited) read that one table.
- Place names and origin labels never change. Names, birth years and the claim line of a place never change.
- Nothing about history is random.

### 1.6 The morning paper

- "— TIMELINE NEWS —" (`DayFlowUIController.ShowBriefing`) lists, in order:
  1. newly DOMINANT attributes of **tomorrow's** places only (Future places are never ranked); the "is rising" line is gone;
  2. the leader line (if any);
  3. timeline trigger lines, including history-rule lines, in library order;
  4. carry lines, until the history cap (3 templated lines a night, the leader's first) is reached; the rest are logged, not printed;
  5. effect news lines (unchanged).

### 1.7 Days 4–6 and after

| Day | Eras | Countries | Queue | Rules |
|---|---|---|---|---|
| 1–3 | unchanged | unchanged | 8 / 10 / 12 | unchanged |
| 4 | + Industrial | all 8 | 12 | no travel to Victorian Britain |
| 5 | + Modern | all 8 | 13 | no travel to Weimar Berlin; no travel to New Kingdom Egypt |
| 6 | + Future (only while a nation leads) | all 8 | 14 | no travel to Showa Tokyo |
| 7–15 | as day 6 | | 14 | as day 6 |

Each era has weight 1; tell count 1; the tell channels copy day 3's (Papers + Answer; piece 4's Appearance is added after piece 4 merges, §8); the premade pool and chance copy day 3's, with no forced premade (R22, **[after piece 4 merges, §8]**).

### 1.8 Triggers and endings

- Art Renaissance fires once when the global Art total reaches 20; Science Boom when Science reaches 15; Democratic Collapse when Democracy falls to −6 or below, from the night of day 3. Their effects are unchanged.
- **Endings:**
  - Fired and Bankrupt are checked after every verdict, at the end of a shift that applied dialog consequences (piece 3), and at sleep.
  - Retirement (day 15) is checked only at sleep: the run ends after day 15 is played.
  - At that same sleep, Artistic Golden Age, Age of Science or Democracy Triumphant replaces Retirement when its attribute total is reached (highest priority, ties in library order: Democracy, Science, Art). They never end a run early. Their thresholds come from the §6 run report (R14; first values Art 45, Science 32, Democracy 15).
  - The present culture and the ending can differ (an Art epilogue under a Chinese Future).

### 1.9 Saves, Continue and determinism

- **Continue** after the end-of-shift save opens **Home** (expenses, shop, slot, sleep), never the finished office day again. Continue at any other time opens the Office for the current day.
- **Determinism:** same run + same day + same history (leader and latched edits) = same travellers. With an empty history (every day until the first latch), days 1–3 are identical to piece 4.
- **Saves** stay version 2. A save from before piece 5 loads with no history: the first night ranks the existing scores, may announce a leader, and never floods the paper (Future places are not ranked). Such a save made at the end of a shift replays that shift once more on Continue (it has no phase). A save on day 4 or later switches from replaying Day 1 to the new day plans, so its next office day differs from what that save would have shown before.

### 1.10 Dev tools

- The Timeline Inspector shows the leader (and since which day), tonight's ranking, every latched edit (place, category, value, day, cause) and the pending carries; the state dump prints the same.
- Cheats: "Force leader" (one button per nation) sets the leader and its cue now and keeps it for the session: each night still ranks the nations, but the leader stays the forced one (R19). The Future changes at the next office day (Skip Day or Sleep). "No leader" clears the override and the leader; the next night decides from influence again. The override is not saved: after Continue, the next night decides from influence.
- "Skip Day" runs the same path as Sleep: day-boundary endings (Retirement after day 15, the epilogues, failures), then the nightly resolve and the next day.

## 2. Design

### 2.1 Where things live

- **TimeDesk.Domain** (`Assets/Scripts/Domain`, pure, EditMode-tested):
  - new: `ScoreKey.cs`, `ScoreRanking.cs`, `Influence.cs`, `NationLeader.cs`, `History.cs`, `Carries.cs`, `HistoryChecks.cs`, `EndingRules.cs`, `CultureCue.cs`, `DayPlans.cs`;
  - changed: `Gates.cs` and `EffectOps.cs` (piece 3), `LookData.cs`, `Looks.cs` and `LookKeys.cs` (piece 4, **[after piece 4 merges, §8]**), `FactTable.cs` (`MaxValueLength`, `TryFindOtherPlaceWith`, doc), `Forgery.cs` (its uniqueness loop calls `TryFindOtherPlaceWith`).
- **Assembly-CSharp:**
  - new: `TodaysWorld.cs`, `Timeline/HistoryService.cs`;
  - changed: `ContentLibrarySO.cs`, `CaseFactory.cs`, `GameManager.cs`, `WorldState.cs`, `EraSO.cs`, `Timeline/NationSO.cs`, `EffectSO.cs` (`EffectOp.category` and docs only; the enum lives in Domain after piece 3), `Timeline/TimelineTriggerSO.cs`, `Timeline/TimelineService.cs`, `Core/GameConfigSO.cs`, `Core/RunManager.cs`, `Home/HomeManager.cs`, `TitleSceneController.cs`, `Endings/EndingService.cs`, `Endings/EndingSO.cs`, `DevTools/DebugPanelController.cs`, `DevTools/DevToolsState.cs`; docs only: `Investigation/ReferenceBookSO.cs`, `Timeline/NationEraProfileSO.cs`.
- **Assembly-CSharp-Editor:** `WorldContentGenerator.cs` (including piece 3's dialog-effect and line-length checks and, after piece 4 merges, piece 4's wardrobe item data, §8), `ContentLibraryValidator.cs`. `OfficeSceneUIBuilder.cs` is unchanged (its `GetDayPlan(1)` keeps its result), so the scene is not rebuilt.
- **Content:** `world_source.json`; generated eras, nations, places, rules, day plans, `Assets/Data/World/History/*` and `ContentLibrary_Main.asset`; hand-edited `GameConfig_Default.asset`, the three `Trigger_*.asset` and three `Ending_*.asset` score assets.
- **Tests:** `Assets/Tests/EditMode` (references only `TimeDesk.Domain` and `TimeDesk.Visuals`): every new test targets Domain.

Line endings: `core.autocrlf=true` checks every text file out as CRLF in the worktree (the draft's per-file LF list was for an older worktree), and git stores LF; edits preserve each file's endings (`SCRATCH/subs.py`).
- `world_source.json` is LF (kept LF, 2-space indent, by the scripts that edit it).
- Generated assets are written by Unity.
- **New files:** LF, each with a `.meta` (`SCRATCH/make_meta.py`).

### 2.2 Domain: score keys and ranking

**`ScoreKey.cs`** (new; parser ported from PR #3 commit `160afde`, credited in the file's class doc):
- `public enum ScoreKeyKind { ProfileAttr, AdHocAttr, GlobalAttr, Nation }` and `public struct ParsedScoreKey { public ScoreKeyKind kind; public string profileId, nationId, eraId, attributeId; }` (as PR #3).
- Builders, the one home of the grammar (PR #3 addendum item 1):
  - `ProfileAttr(string profileId, string attrId) => $"attr:{profileId}:{attrId}"`;
  - `AdHocAttr(string nationId, string eraId, string attrId) => $"attr:{nationId}@{eraId}:{attrId}"`;
  - `GlobalAttr(string attrId) => $"attrTotal:{attrId}"`;
  - `Nation(string nationId) => $"nation:{nationId}"`;
  - `Dominance(string profileId, string attrId) => $"{profileId}:{attrId}"`.
- `public static bool TryParse(string key, out ParsedScoreKey parsed)` exactly as PR #3 (`attrTotal:` checked before `attr:`; `attr:{scope}:{attr}` splits on the last colon; a scope with `@` is ad-hoc). The prefixes are private constants used by both the builders and the parser. PR #3's `DominanceKey(parsed)` is not ported (no caller).

**`ScoreRanking.cs`** (new):
- `[Serializable] public struct RankedScore { public string id; public float score; public RankedScore(string id, float score); }`: public fields so `HistoryState.ranking` serializes with JsonUtility.
- `public static class ScoreRanking { public static List<RankedScore> Rank(IEnumerable<RankedScore> entries); }`: a new list, highest score first; **equal scores keep their input order** (a stable sort, `Enumerable.OrderByDescending`); null gives an empty list; the input is not modified. Class doc: "The one ranking of the timeline: per-place attribute tiers and the nation leader both rank through it (design credited to PR #3)."
- `public enum DominanceTier { None, Supporting, Dominant }`.
- `public static class DominanceTiers { public static DominanceTier[] Classify(IReadOnlyList<RankedScore> scores, int dominantCount, int supportingCount); }`: ranks `scores` with `ScoreRanking.Rank`; the first `dominantCount` are Dominant, the next `supportingCount` Supporting, the rest None. The result is aligned with the **input** order (index i is the tier of `scores[i]`); ids are unique within a place (attribute ids). Negative counts count as 0; null gives an empty array.

### 2.3 Domain: influence and the leader

**`Influence.cs`** (new):
- `public readonly struct PlaceRef { public readonly string NationId, EraId; public PlaceRef(string nationId, string eraId); }`.
- `public static List<RankedScore> ByNation(IReadOnlyList<string> nationIds, IEnumerable<KeyValuePair<string, float>> scores, Func<string, PlaceRef?> placeOfProfile, string excludedEraId)`:
  - for each score key, `ScoreKey.TryParse`; a `ProfileAttr` key belongs to `placeOfProfile(profileId)` (an unknown profile is skipped); an `AdHocAttr` key belongs to its parsed nation and era; every other kind is skipped;
  - a key whose era equals `excludedEraId` (the Future) is skipped, as is a nation not in `nationIds`;
  - returns one entry per `nationIds` entry, **in that order**, with 0 for a nation with no key (the caller ranks it).
- Class doc: "A nation's influence on the past: the attribute deltas stored for its places outside the Future. Baselines, nation scores and global totals are not influence (AddNationScore is not a lever)."

**`NationLeader.cs`** (new):
- `public static string Decide(string incumbentId, IReadOnlyList<RankedScore> ranked, float floor, float keepFloor, float margin)`, `ranked` highest first (`ScoreRanking.Rank`); returns the leader's id or `""` (R2):
  1. find the incumbent's entry by id; if the incumbent is blank, not in `ranked`, or its score ≤ `keepFloor`, go to step 3;
  2. `best` = the first entry that is not the incumbent, `next` = the second such entry; return `best.id` when `best.score > incumbent.score + margin` and (`next` does not exist or `best.score > next.score`), else the incumbent;
  3. with `ranked[0]` = top: return `top.id` when `top.score > floor` and (`ranked.Count == 1` or `top.score > ranked[1].score`), else `""`.
- A negative margin counts as 0; a `keepFloor` above `floor` counts as `floor`. Class doc: "Who leads the timeline, with hysteresis: a unique top above the floor starts leading; a leader stays until one unique challenger beats it by more than the margin, or until it falls to the keep floor; ties never make or change a leader."

### 2.4 Domain: history state, resolution, carries, checks

**`History.cs`** (new), all `[Serializable]` classes with public fields and a public parameterless constructor (JsonUtility):

```csharp
/// Why a fact changed; serialized as ints in saves: append only.
public enum EditCause { Rule, Carry }

/// One latched change of a place's fact ("from day 5, Florentine Republic's Technology is ...").
[Serializable] public sealed class FactEdit
{
    public string nationId, eraId;       // the place
    public ClueCategory category;
    public string value;
    public int sinceDay;                 // first day the edit applies (display: inspector, logs)
    public EditCause cause;
    public string source;                // trigger label for a rule, the origin place's label for a carry
    public FactEdit();
    public FactEdit(string nationId, string eraId, ClueCategory category, string value, int sinceDay, EditCause cause, string source);
}

/// An accepted liar's carry, recorded at accept and promoted at night.
[Serializable] public sealed class CarryRecord
{
    public string fromNationId, fromEraId, toNationId, toEraId;
    public ClueCategory category;
    public string value;
    public int day;
}

/// The saved history of the run (WorldState.history).
[Serializable] public sealed class HistoryState
{
    public string leaderId = string.Empty;           // the present culture; "" = none
    public int leaderSinceDay;                        // first day under this leader (0 = none)
    public List<RankedScore> ranking = new();         // last night's influence ranking, highest first
    public List<FactEdit> factEdits = new();          // oldest first
    public List<CarryRecord> pendingCarries = new();  // in record order
}

/// Wording of the templated history news (content, English in v1, R21).
[Serializable] public sealed class HistoryLines
{
    public LineText leaderGained = new();   // tokens {nation}, {place}
    public LineText leaderLost = new();     // token {nation}
    public LineText carry = new();          // tokens {value}, {place}
}
```

`LineText` is piece 3's (`InterviewContent.cs`). `public static class History`:
- `public static readonly ClueCategory[] EditableCategories = { Currency, Language, Technology, Geography, Politics };` (H12, R12).
- `public const string NationToken = "nation";` (the only new token: `{place}` is piece 3's `Interview.PlaceToken`, `{value}` its `Interview.ValueToken`, R20).
- `public static bool IsEditable(ClueCategory c)`.
- `public static string FutureNation(HistoryState h)`: the leader's id, or null when there is none (R1). Doc: "Whose Future place is in the world: the leader's; none without a leader."
- `public static bool InWorld(bool isFutureEra, string nationId, string futureNationId)`: true for a place outside the Future; a Future place only when `futureNationId` is non-null and equals `nationId` (ordinal). Doc: "Whether a place the day plan allows is in today's world: at most one Future place, the given nation's (R1)." Callers: `ContentLibrarySO.TodaysProfiles` (and through it the validator's per-leader checks).
- `public static string Resolve(HistoryState h, string nationId, string eraId, ClueCategory category, string baseValue)`: scans `h.factEdits` from newest to oldest and returns the first edit's value with the same place (ordinal ids), category and a non-blank value; otherwise `baseValue`. A null state gives `baseValue`. No day filter: every edit is latched at night for the next day, and facts are only ever built for that day or later.
- `public static bool Latch(HistoryState h, FactEdit edit)`: appends and returns true, unless `h` or `edit` is null, the value is blank, or the category is not editable.
- `public static int NewsSlots(int linesSoFar, int cap, int count)`: how many of `count` further templated lines fit tonight: `max(0, min(count, cap − linesSoFar))`; negative inputs count as 0. Doc: "The history news cap (R8): the leader line comes first, then carry lines while they fit." Caller: `HistoryService.PromoteCarries`.

**`FactTable.cs`** (changed, R20):
- `public const int MaxValueLength = 28;` "The widest fact value a book row shows; every place fact, derived Culture value and history value fits (checked by the generator)." Readers: the generator's fact-length and Culture-length checks, `HistoryChecks`, the validator. Piece 4's `Looks.MaxCultureLength` is removed and its readers use this.
- `public bool TryFindOtherPlaceWith(ClueCategory category, string nationId, string eraId, string value, out FactRow row)`: the first row of `Rows(category)` whose place is not (`nationId`, `eraId`) and whose value `DiscrepancyLog.ValuesMatch` `value`; false (and a default row) when none or `value` is blank. Doc: "Another place of this category with this value (the uniqueness question behind provable tells and history checks)."
- `Forgery.IsProvableTell` (`Forgery.cs:40-45`) replaces its loop with `if (facts.TryFindOtherPlaceWith(category, home.NationId, home.EraId, homeValue, out _)) return false;`; its results are unchanged (the existing `ForgeryTests` pin them).
- Class doc (40-47): "history-dependent facts (a later feature) change what goes in" becomes "history changes what goes in (`ContentLibrarySO.FillFacts`), not how it is read".

**`CultureCue.cs`** (new, R20):

```csharp
/// The UI-channel cue that carries the present culture (piece 5 emits it, piece 6 reads it).
public static class CultureCue
{
    public const string Prefix = "culture:";
    public static string Format(string nationId);                    // "culture:" + id
    public static bool TryParse(string cue, out string nationId);    // exact, case-sensitive prefix; blank id → false
}
```

Callers: the generator's `MakeLeaderEffect` (`Format`) and the validator's `CheckFuture` (`TryParse`). This is the shape piece 6's draft §2.3 defines; piece 6 adds its `Pick` here instead of creating the file.

**`DayPlans.cs`** (new, R17): `public static int Pick(IReadOnlyList<int> dayNumbers, int day)`: the index of the first entry equal to `day`; otherwise the index of the largest entry below `day` (the first such entry on a repeat); otherwise −1 (also for a null or empty list). Doc: "Which day plan a day uses: its own, else the latest earlier one." Caller: `ContentLibrarySO.GetDayPlan`.

**`Carries.cs`** (new), `public static class Carries`:
- `public static CarryRecord Make(string fromNationId, string fromEraId, string toNationId, string toEraId, ClueCategory category, FactTable today, int day)`: null when an id is blank, from equals to, the category is not editable, `today` is null, the home has no value in `today`, or its value `ValuesMatch` the claim's; otherwise a record with the home's value.
- `public static List<FactEdit> Promote(HistoryState h, int threshold, int sinceDay, FactTable world)`:
  - a threshold below 1 counts as 1;
  - pairs (from, to, category) are taken in order of their first pending record; a pair with at least `threshold` records is due;
  - for a due pair: the value is its **latest** record's; the target's current value is the value of the last edit this call has already returned for (to, category), else `world.Get(to, category)`; if it `ValuesMatch` the value nothing latches; else `History.Latch` a `FactEdit(to, category, value, sinceDay, EditCause.Carry, source: world.OriginLabel(from) ?? "{from}_{fromEra}")` and add it to the result; either way the pair's records are removed;
  - pairs below the threshold keep their records.

**`HistoryChecks.cs`** (new), `public static List<string> Problems(IReadOnlyList<FactEdit> edits, FactTable baseWorld)`. `baseWorld` holds every place's authored facts (no history). One message per problem, naming `edit.source`, the place and the category:
- the value is blank or longer than `FactTable.MaxValueLength` (ASCII is the generator's job, through piece 3's R18 check, R20);
- the category is not editable;
- the place is not in `baseWorld` (`OriginLabel` null);
- the value `ValuesMatch` the place's own base value (the edit would change nothing);
- the value `ValuesMatch` another place's base value in that category (`baseWorld.TryFindOtherPlaceWith`);
- the value `ValuesMatch` another edit's value in that category for a **different** place (two edits for the same place may share a value).

Used by the generator (on the JSON, before writing) and the validator (on the assets), so the rules exist once.

### 2.5 Domain: gates, effect ops and looks (pieces 3 and 4)

- `TriggerConditionType` gains, appended after piece 3's `UpgradeOwned` (10):
  - `NationIsLeader` (11): "The nation currently leads the timeline (WorldState.history.leaderId). key = nation id.";
  - `GlobalAttrAtLeast` (12) / `GlobalAttrAtMost` (13): "The global attribute total (attrTotal:{id}, deltas only; a missing total reads 0, so an AtMost threshold ≥ 0 passes before any send) ≥ / ≤ threshold. key = the attrTotal key."
  Piece 4 appends none (its draft touches neither enum); if the implemented pieces appended values first, these follow them and the retuned trigger assets (§2.14) and the test pins use the actual ints.
- `GateSnapshot` gains `public string LeaderId { get; }`, set by a new trailing constructor parameter `string leaderId = null`.
- `Gates.Passes` rows: `NationIsLeader` → `Key != null && s.LeaderId != null && s.LeaderId == Key` (ordinal); `GlobalAttrAtLeast`/`AtMost` → `Key != null && s.Score(Key) >= / <= Threshold`.
- **`EffectOps.cs`** (LF, created by piece 3):
  - `EffectOpType` gains, appended after `NewsLine` (16): `SetFact` (17), `/// <summary>Instant: profile = the target place, category = the fact, stringParam = the new value. History rules only (EffectOps.HistoryOnly).</summary>`.
  - `EffectOps.ActsWhileActive(SetFact)` is **false** (it acts once, when its trigger fires at night); its doc adds SetFact to the instant ops and points to `HistoryOnly`.
  - New `public static bool HistoryOnly(EffectOpType type)`: true for `SetFact` only. Doc: "Ops only a history rule's effect may hold: a night-latched fact write (Y4). Rejected in dialog, slot-outcome, upgrade, tier and leader effects." Callers: piece 3's generator dialog-effect check (`CheckInterview`: "Dialog '{id}' choice '{choice}' names effect '{name}', whose {op} op may only run in a history rule at night") and the validator's `CheckEffects`/`CheckTriggers` (§2.13).
- **Looks** (piece 4's LF files; R18 delivers its R27): **[after piece 4 merges, §8]**
  - `LookData.cs`, `LookItem` gains `public string artNation;` "Key token that replaces the place's nation in this item's art file names, so places of one era share a drawing (`neutral` gives `hair_{g}_neutral_future_{colour}`); blank = the item's own place." and `public string ArtNation(string placeNationId) => string.IsNullOrWhiteSpace(artNation) ? placeNationId : artNation;` ("the nation token this item's art is filed under"). The one home of the override rule; callers below.
  - `LookKeys.Required(nationId, eraId, w)`: each garment key uses `item.ArtNation(nationId)`; per-place counts are unchanged, and places that share an item yield the same names (the validator's art report lists distinct names, so the eight Future places list one neutral hair set: 5 colours per gender instead of 40, likewise facial hair and hair back where present).
  - `Looks.Compose` step 5: garment keys use `item.ArtNation(source.NationId)` with the source's era; colours and wigs as before.
  - `Looks.CanLeak` gains a row after "hidden by the disguise" (piece 4's row 5): **6.** the claim's item for g in the signature slot is present, and `LookKeys.Garment(layer, g, homeItem.ArtNation(home.NationId), home.EraId, null).Name` equals the same name for the claim's item (claim nation and era), with both items wigs or neither → false ("the leak would draw the same art as the disguise, an invisible tell"). The confusable row becomes 7 and "otherwise true" 8.
  - `Looks.MaxCultureLength` is removed; its readers use `FactTable.MaxValueLength` (R20). `Looks.MaxLabelLength` stays.

### 2.6 Domain: endings (`EndingRules.cs`, new)

- `public enum EndingConditionType { Fired, Bankrupt, AttrTotalAtLeast, DayAtLeast }`: moved from `EndingSO.cs:8-21` with its docs, values and order unchanged (ending assets store ints; global namespace, so no reference changes).
- `public enum EndingKind { Failure, Milestone, Epilogue }` and `public enum EndingMoment { Immediate, DayBoundary }` (Immediate: after a verdict or at the end of a shift; DayBoundary: at sleep).
- `public readonly struct EndingCandidate { public readonly EndingKind Kind; public readonly int Priority; public readonly bool Met; ctor }`.
- `EndingRules.KindOf(EndingConditionType t)`: Fired, Bankrupt → Failure; DayAtLeast → Milestone; AttrTotalAtLeast → Epilogue; any other value → Failure.
- `EndingRules.Select(IReadOnlyList<EndingCandidate> candidates, EndingMoment moment)`: returns the index of the winner or −1:
  - only met candidates count;
  - Immediate: only Failures count;
  - DayBoundary: Failures and Milestones count; an Epilogue counts only when some Milestone is met;
  - highest `Priority` wins; ties go to the lowest index (library order).

### 2.7 Assembly-CSharp: content types and state

- **`EraSO`**: `public bool isFuture;` "The office's own time: at most one era; its places come and go with history (one a day: the leader's). Written by Generate World from eras[].future."
- **`NationSO`**: `public EffectSO leaderEffect;` "Effect active while this nation leads the timeline (its UI cue culture:{id} is the present culture). Written by Generate World."
- **`EffectSO`** (`EffectSO.cs`; after piece 3 the enum is in Domain `EffectOps.cs`, §2.5, so this file keeps only the class and `EffectOp`):
  - `EffectOp` gains `public ClueCategory category;` "Fact category (SetFact)"; the `profile` doc adds "SetFact target place".
  - Class doc: "Activated by dominance tiers, timeline triggers (including history rules), the timeline leader, slot outcomes …".
- **`TimelineTriggerSO`**: `TriggerCondition.attribute` doc "(attribute and global-attribute conditions)"; `nation` doc "(NationScoreAtLeast, NationIsLeader)". The class doc mentions generated history-rule triggers.
- **`GameConfigSO`**, new block after "Timeline dominance" (file lines 71-78):

  ```csharp
  [Header("History")]
  /// Influence a nation needs, strictly above this, to lead the timeline (and open its Future).
  public float leaderFloor = 4f;
  /// A challenger replaces the leader only when its influence exceeds the leader's by more than this.
  [Min(0f)] public float leaderMargin = 2f;
  /// A leader loses the lead when its influence falls to this or below (kept below leaderFloor, so a one-send dip does not flip the Future).
  public float leaderKeepFloor = 2f;
  /// How often the same (true home, claimed place) pair must be accepted before the carry latches.
  [Min(1)] public int carryThreshold = 1;
  /// The fact an accepted liar carries from their true home into the claimed place (one of the five editable categories).
  public ClueCategory carryCategory = ClueCategory.Technology;
  /// Most templated history news lines a night (the leader's first, then carries).
  [Min(1)] public int maxHistoryNewsPerNight = 3;
  ```

  New `private void OnValidate()` (the `CaseBlueprintSO.cs:73` precedent) logs a warning when `!History.IsEditable(carryCategory)` ("carries would silently never record") or `leaderKeepFloor > leaderFloor` ("counts as the floor: no hysteresis at the floor").

- **`WorldState`** (subsystem blocks, 78-89):
  - `/// History: the timeline leader, latched fact edits and pending carries (piece 5). public HistoryState history = new();`
  - `/// Where Continue resumes: Office until the day's shift ends, Home after the end-of-shift save. public RunPhase phase = RunPhase.Office;`
  - new `public enum RunPhase { Office, Home }` in the same file ("serialized as ints: append only").
- **`TodaysWorld.cs`** (new):

  ```csharp
  /// Today's world, built once per day by ContentLibrarySO.BuildToday: the places travellers
  /// come from and their facts (history applied), so case generation and the books share one
  /// list and one table.
  public sealed class TodaysWorld
  {
      public TodaysWorld(IReadOnlyList<NationEraProfileSO> places, FactTable facts);
      public IReadOnlyList<NationEraProfileSO> Places { get; }
      public FactTable Facts { get; }
  }
  ```

- **`ContentLibrarySO`**:
  - `TodaysProfiles(DayPlanSO plan)` (63-99) becomes `TodaysProfiles(DayPlanSO plan, string futureNationId)`: after the plan filter, a place is kept only when `History.InWorld(p.era.isFuture, p.nation.id, futureNationId)`. Doc: "… and at most one Future place: the given nation's (History.FutureNation, History.InWorld)."
  - `BuildFactTable(DayPlanSO)` (101-122) is replaced by:
    - `public TodaysWorld BuildToday(DayPlanSO plan, HistoryState history)`: `places = TodaysProfiles(plan, History.FutureNation(history))`, a new table filled by `FillFacts(table, places, history)`;
    - `public FactTable BuildWorldFacts(HistoryState history)`: every profile with nation and era ids, in library order, through `FillFacts`;
    - `private static void FillFacts(FactTable table, IEnumerable<NationEraProfileSO> places, HistoryState history)`: for each fact, `table.Add(nation.id, era.id, p.OriginLabel, f.category, History.Resolve(history, nation.id, era.id, f.category, f.value))`. Doc: "The only code that turns place facts into table rows (H18): history is resolved before Add, so papers, books, tells, answers and outfits all follow it. It is also the only code that sees a fact's authored and resolved values together (piece 6 marks revised rows here)."
  - `[Header("History")]` `[SerializeField] private HistoryLines historyLines = new();` + `public HistoryLines HistoryLines => historyLines ?? new HistoryLines();` (written by Generate World).
  - `public EraSO FutureEra`: the first non-null era with `isFuture`, or null.
  - `GetNationById(string id)` and `GetProfileById(string id)`: cached, case-insensitive like `GetEraById` (`_nationById`, `_profileById` filled in `EnsureLookups`, cleared in `OnEnable`).
  - `GetDayPlan(int dayNumber)` (271-287): collects the non-null plans' `DayNumber`s in list order and returns the plan at `DayPlans.Pick(numbers, dayNumber)`, or null for −1 (the plan with that day number, else the latest earlier one, else none). Doc: "… days after the last authored plan reuse it (the Future stays open once a leader exists)."
  - Class docs of `ReferenceBooks`/`ReferenceBookCategories` unchanged.

### 2.8 Assembly-CSharp: timeline and history glue

**`TimelineKeys`** (`TimelineService.cs:8-27`): every builder becomes a one-line wrapper, e.g. `ProfileAttr(profile, attr) => ScoreKey.ProfileAttr(profile.id, attr.id)`; `AdHocAttr`, `GlobalAttr`, `Nation`, `Dominance` likewise. Flag keys are not score keys: piece 3 left `TimelineKeys` unchanged and put the run-flag grammar in Domain `FlagKeys` (`TriggerFired`, `DialogDone`), which piece 5 does not touch. (Piece 4's draft adds `PremadeMetFlag` to `TimelineKeys`, naming piece 3's pre-`FlagKeys` members; that is piece 4's to reconcile, and piece 5 leaves whatever piece 4 implements.)

**`TimelineService`** (CRLF):
- `ApplyVerdictImpacts` (45-107): code unchanged; doc adds "the attribute keys it writes are what history counts as influence (HistoryService)".
- `NightlyResolve` (127-151), new order (doc updated):

  ```csharp
  int tomorrow = world.day + 1;
  var news = new List<string>();
  RecomputeDominance(world, lib, config, news, TomorrowPlaces(world, lib));
  RebuildTierEffects(world, lib, tomorrow);
  int historyLines = HistoryService.LatchLeader(world, lib, config, tomorrow, news);
  EvaluateTriggers(world, lib, tomorrow, news);           // history rules fire here (SetFact)
  HistoryService.PromoteCarries(world, lib, config, tomorrow, news, historyLines);
  ExpireEffects(world, tomorrow);
  BuildTomorrowPackage(world, lib, news);
  ```

- New `private static HashSet<NationEraProfileSO> TomorrowPlaces(WorldState world, ContentLibrarySO lib)`: `lib.TodaysProfiles(lib.GetDayPlan(world.day + 1), null)` (Future places are never ranked, so no leader is needed); empty when there is no plan.
- `SeedDominance` (153-164) calls `RecomputeDominance(world, lib, config, null, null)`.
- New `internal static int RemoveEffectsFrom(WorldState world, string sourcePrefix)`: removes the active effects whose `sourceLabel` starts with the prefix and returns how many (R20). Doc: "One idempotent-rebuild step: an effect family removes its own entries before re-activating." Callers: `RebuildTierEffects` (its inline `RemoveAll`, 243-244, goes) and `HistoryService.RebuildLeaderEffect`.
- `RecomputeDominance(world, lib, config, news, tomorrowPlaces)` (166-232):
  - per profile with baselines: `scores` = `new RankedScore(b.attribute.id, GetProfileAttributeScore(...))` in baseline order; `tiers = DominanceTiers.Classify(scores, dominantCount, supportingCount)`; Dominant/Supporting keys as before;
  - news only when `announce && tomorrowPlaces != null && tomorrowPlaces.Contains(profile)` and the key was not dominant before: "{Attr} is now DOMINANT in {place}.";
  - the "is rising" branch (218-224) is removed; the `ranked.Sort` (203) is removed (the Domain rank replaces it);
  - doc: "Tiers rank baseline + delta (they describe a place); influence ranks deltas only (it describes the player's deviation). Both are night snapshots through ScoreRanking."
- `ActivateEffect` (369-418) gains:

  ```csharp
  case EffectOpType.SetFact:
      if (op.profile != null && op.profile.nation != null && op.profile.era != null)
          History.Latch(world.history, new FactEdit(op.profile.nation.id, op.profile.era.id,
              op.category, op.stringParam, startDay, EditCause.Rule, sourceLabel));
      break;
  ```

- Piece 3's `ToGate` gains: `NationIsLeader` → `c.nation != null ? c.nation.id : null`; `GlobalAttrAtLeast`/`AtMost` → `c.attribute != null ? TimelineKeys.GlobalAttr(c.attribute) : null`. Piece 3's `Snapshot` fills `world.timeline.GetScore(key)` for the global-attribute keys the conditions read, and passes `world.history.leaderId` as `leaderId`.

**`HistoryService`** (new `Assets/Scripts/Timeline/HistoryService.cs`, static). Class doc: "History glue: records liar carries at accept, and at night latches the timeline leader and promotes carries. Every decision is a Domain call (Influence, ScoreRanking, NationLeader, Carries, History)."
- `private const string LeaderSourcePrefix = "history:leader";`
- `public static void RecordCarry(WorldState world, CaseInstance inst, FactTable today, GameConfigSO config)`: returns unless `inst.IsLiar` and `config != null`; `Carries.Make(home ids, claim ids, config.carryCategory, today, world.day)`; a non-null record is appended to `world.history.pendingCarries` and logged ("[HistoryService] Carry recorded: '{value}' from {home} to {claim}.").
- `public static int LatchLeader(WorldState world, ContentLibrarySO lib, GameConfigSO config, int tomorrow, List<string> news)`: returns the number of lines added (0 or 1). With a null config it logs one warning and returns 0 (history needs the knobs).
  1. `influence = Influence.ByNation(nation ids in library order, world.timeline.scores as pairs, id => place of lib.GetProfileById(id), lib.FutureEra?.id)`;
  2. `ranked = ScoreRanking.Rank(influence)`; `world.history.ranking = ranked`;
  3. `next` = `DevToolsState.ForcedLeaderId` when it is set (R19; logged "[HistoryService] Leader kept by the debug override: '{id}' (influence decision skipped)."), else `NationLeader.Decide(world.history.leaderId, ranked, config.leaderFloor, config.leaderKeepFloor, config.leaderMargin)`;
  4. on a change: set `leaderId`, `leaderSinceDay = next == "" ? 0 : tomorrow`, add the leaderGained line (`{nation}` = display name, `{place}` (`Interview.PlaceToken`) = the leader's Future place label via `lib.GetProfile(nation, lib.FutureEra)`, or the nation's name when the content has no Future era) or the leaderLost line (old leader), filled with `Interview.Fill`;
  5. `RebuildLeaderEffect(world, lib, tomorrow)`.
- `public static void PromoteCarries(WorldState world, ContentLibrarySO lib, GameConfigSO config, int tomorrow, List<string> news, int historyLinesSoFar)`:
  1. `worldFacts = lib.BuildWorldFacts(world.history)`;
  2. for each edit latched tonight by a rule (`sinceDay == tomorrow && cause == Rule`) for which `worldFacts.TryFindOtherPlaceWith(edit.category, edit.nationId, edit.eraId, edit.value, out FactRow other)` holds: a warning "[HistoryService] History rule '{source}' gives {place} the {category} value '{value}', which {other.OriginLabel} already has, so neither can leak a {category} tell while both are in the world. Give the rule a distinct value (Tools > TimeDesk > Validate Content Library).";
  3. `edits = Carries.Promote(world.history, config.carryThreshold, tomorrow, worldFacts)`;
  4. `slots = History.NewsSlots(historyLinesSoFar, config.maxHistoryNewsPerNight, edits.Count)`: one carry line for each of the first `slots` edits (`{value}`, `{place}` = the target's label); the rest are logged ("[HistoryService] {n} carry line(s) over tonight's news cap: …").
  With a null config it returns without promoting.
- `public static void ForceLeader(WorldState world, ContentLibrarySO lib, string nationId)` (dev cheat, R19): a blank id clears `DevToolsState.ForcedLeaderId`, sets `leaderId = ""` and `leaderSinceDay = 0`; otherwise it sets `DevToolsState.ForcedLeaderId = nationId`, `leaderId = nationId` and `leaderSinceDay = world.day + 1`. Then `RebuildLeaderEffect(world, lib, world.day)` (the cue is active at once for a scene reload, e.g. piece 6's "apply culture now"); no news. Doc: "Sets the leader for the rest of the session: the nightly leader step keeps it until 'No leader' or a new run/Continue (DevToolsState.ResetAll). The Future place follows at the next office day."
- `private static void RebuildLeaderEffect(WorldState world, ContentLibrarySO lib, int startDay)`: `TimelineService.RemoveEffectsFrom(world, LeaderSourcePrefix)`; when a leader exists and `lib.GetNationById(leaderId)?.leaderEffect` is set, `TimelineService.ActivateEffect(world, fx, $"{LeaderSourcePrefix} {displayName}", startDay, -1, applyInstantOps: false)`; a missing effect logs a warning naming Generate World. It is the idempotent emission step piece 6 calls on Continue (its C3); piece 6 makes it public when it adds that caller.

**`DevToolsState`** (`DevTools/DevToolsState.cs`, CRLF): `public static string ForcedLeaderId;` "Session-only override of the timeline leader (debug panel 'Force leader'); while set, the nightly leader step keeps it instead of deciding from influence. Null = no override." `ResetAll` also clears it (with a log line when it was set); the class doc's cheat list adds it.

### 2.9 Case generation (`CaseFactory`, CRLF)

- Constructor (50-55): `CaseFactory(ContentLibrarySO lib, TodaysWorld today)`: `_facts = today?.Facts ?? new FactTable()`, `_todays = today != null ? new List<NationEraProfileSO>(today.Places) : new List<NationEraProfileSO>()`. Doc: "over a content library and today's world (ContentLibrarySO.BuildToday for the same day plan)".
- `GenerateDayCases` (63-101): the `_todays = _lib.TodaysProfiles(plan)` line (79) is removed; the "no places" warning stays.
- `PickEraFromPlan` (477-494): the weight selector becomes `ew => ew.era != null && _todays.Any(p => p.era == ew.era) ? ew.weight : 0f` (R1). Doc: "An era with no place today (the Future without a leader) is never drawn." The no-weights branch is unchanged.
- Class summary: today's places come from `TodaysWorld`, facts carry history.
- Unchanged: `PickPlace` (the legendary `GetProfile` path keeps its "not in today's world" warning; a premade naming a Future place is a generator and validator error, R22), `Disguise`, `PlanViolators`, `ResolveFieldValue`, `BuildRegistry`. Piece 2's true-home candidates and piece 3's answers read `_todays`/`_facts`, so they follow history with no change.

### 2.10 Run flow

**`GameManager`** (CRLF):
- field `private TodaysWorld _today;` ("today's places and facts, history applied; one snapshot for the factory, the books and carries").
- `Start`:
  - the comment at 92-93 says "the library's plan for today, or its latest earlier plan";
  - after resolving the world: when `_worldState.phase == RunPhase.Home`, log a warning "[GameManager] The saved run already finished day {day}'s shift; replaying it because the Office scene was opened directly. Continue from the Title resumes at Home." (dev only; the day proceeds);
  - 132-140: `_today = contentLibrary.BuildToday(dayPlan, _worldState.history); _caseFactory = new CaseFactory(contentLibrary, _today);` and `investigationUI.SetFacts(_today.Facts)` (148);
  - the day-start log (163) adds `places={_today.Places.Count}, leader='{_worldState.history.leaderId}'`.
- `HandleDayCompleted` (228-233): `_worldState.phase = RunPhase.Home;` before `ResetTomorrowModifiers()`/`SaveNow()` (after piece 3's `ApplyDialogOutcomes`, whose `EndingService.Evaluate` passes `EndingMoment.Immediate`).
- `HandleDecision` (512-514): inside `if (accepted)`, after `ApplyVerdictImpacts`, `HistoryService.RecordCarry(_worldState, inst, _today.Facts, _gameConfig);`.
- `HandlePlayerChoseEra` (439) and `HandleDecision` (521): `EndingService.Evaluate(..., EndingMoment.Immediate)`. The legacy path records no carry (R16).

**`RunManager`** (CRLF):
- `GetCurrentDayPlan` (164-169): doc "… or the latest earlier plan (ContentLibrarySO.GetDayPlan)".
- `AdvanceToNextDay` (171-190): `World.phase = RunPhase.Office;` before `SaveNow()`; doc mentions the history steps of the nightly resolve.
- New `public void ResumeRun()`: "Opens the scene the saved run resumes in: Home after the end-of-shift save (GoHomeOrAdvance), otherwise the Office." Logs the phase.
- New `public void Sleep()`: "End of the Home phase: day-boundary endings (Retirement and the attribute epilogues, plus the failures) first; otherwise the nightly resolve and the next day." Body = today's `HomeManager.HandleSleep` logic (`HomeManager.cs:262-279`) with `EndingService.Evaluate(World, Library, Config.gameConfig, EndingMoment.DayBoundary)`. Callers: `HomeManager.HandleSleep`, the no-Home branch of `GoHomeOrAdvance` and the debug panel's Skip Day (§2.11); `AdvanceToNextDay`'s only remaining caller is `Sleep`.
- `GoHomeOrAdvance` (231-252): the no-Home branch calls `Sleep()` instead of `AdvanceToNextDay()` (log text says "sleeping").

**`HomeManager`**: `HandleSleep` (246-280) becomes the RunManager null check plus `RunManager.Instance.Sleep()`; its doc and the class doc (4-10) point there.

**`TitleSceneController`**: `HandleContinue` (58-72) and the no-UI fallback (53-55) call `RunManager.Instance.ResumeRun()` / `run.ResumeRun()`; class doc "Continue resumes where the save was made". `HandleNewRun` still loads the Office.

**`EndingService`**: `Evaluate(WorldState world, ContentLibrarySO lib, GameConfigSO config, EndingMoment moment)`: builds one `EndingCandidate(EndingRules.KindOf(e.conditionType), e.priority, Matches(e, world, config))` per non-null library ending, calls `EndingRules.Select`, logs the moment and the winner. `Matches` is unchanged. Class doc: "failures after every verdict; milestones and epilogues at the day boundary".

**`EndingSO`**: the enum moves out (R10); the class doc says an attribute ending is an epilogue chosen at the day boundary when a milestone (Retirement) is reached.

### 2.11 Dev tools (`DebugPanelController`, CRLF)

- `DrawCheatsTab` (92-207):
  - "Skip Day" (97-101) calls `run.Sleep()` instead of `run.AdvanceToNextDay()` (label "Skip Day (sleep: endings + nightly resolve + advance)"; log "Cheat: Skip Day requested (through Sleep)"), so day-boundary endings are never skipped (R19);
  - after "Day flow": a "History" label, a "No leader" button and one button per `lib.Nations` entry → `HistoryService.ForceLeader(world, lib, id)` with a log line; a caption "Kept every night this session; the Future follows at the next office day."; the method doc lists the history cheats.
- `DrawInspectorTab` (209-247), after the active effects: "History: leader '{id}' (since day {n}){ (forced)}", "Ranking: {id} {score:0.#}, …", "Fact edits ({n}):" with `{nationId}_{eraId} {category} = '{value}' (from day {sinceDay}, {cause}: {source})`, "Pending carries ({n}):" with `{from} -> {to}: '{value}' ({category}, day {day})`.
- `DumpStateToConsole` (273-304) prints the same block.

### 2.12 Content pipeline

**`world_source.json`** (LF, hand-maintained since piece 3's X10):
- `eras[]`: `"future": true` on the `future` era (others omit it).
- `places[]`: eight entries with `"era": "future"` (§2.14), in country order, after the Modern places; each has `moment` (research context, not generated), `year` 2150, the five editable facts (no Culture: piece 4's generator derives it from the wardrobe and rejects a Culture entry in `facts[]`, R1 there), 8+8 names, and, after piece 4 merges (§8), a `wardrobe` block in piece 4's format and, optionally, `looks` (the country default applies otherwise).
- piece 4's wardrobe items gain an optional `"artNation"` (R18). **[after piece 4 merges, §8]**
- `rules[]`: `Rule_NoIndustrialBritain`, `Rule_NoModernGermany`, `Rule_NoModernJapan` (§2.14).
- `days[]`: days 4–6 (§2.14), with piece 4's `premades`/`forced`/`premadeChance` (R22) **[after piece 4 merges, §8]**.
- New `history` object: `{ "lines": { "leaderGained", "leaderLost", "carry" }, "rules": [ { "id", "name", "news", "conditions": [ConditionData], "edits": [ { "place", "category", "value" } ] } ] }`.
- Piece 3's `ConditionData` gains `attribute`, `place` and `nation` (ids).

**`WorldContentGenerator`** (LF):
- `OwnedFolders` (32) gains `"History"` (after piece 3's `"Interview"`).
- Source classes (411-475): `EraData.future` (bool); `WorldSource.history` (`HistoryData`); new `HistoryData { HistoryLinesData lines; HistoryRuleData[] rules; }`, `HistoryLinesData { string leaderGained, leaderLost, carry; }`, `HistoryRuleData { string id, name, news; ConditionData[] conditions; EditData[] edits; }`, `EditData { string place, category, value; }`; `ConditionData` gains `attribute`, `place`, `nation`; piece 4's `ItemData` gains `artNation` **[after piece 4 merges, §8]**.
- **Checks** (a new `CheckHistory(src, authored, errors)` called from `Generate` with `CheckReferences`, before anything is written):
  - at most one era has `future`; if one does, every country has exactly one place in it;
  - every place fact value is at most `FactTable.MaxValueLength` characters (R12);
  - `history.lines`: each non-blank and ASCII (piece 3's R18 helper, called); leaderGained holds `{nation}` and `{place}`, leaderLost `{nation}`, carry `{value}` and `{place}` (piece 3's `Interview.Placeholder` with `History.NationToken`, `Interview.PlaceToken`, `Interview.ValueToken`);
  - rules: ids unique, non-blank, lower-case `[a-z0-9_]`; `name`, `news` and every edit value non-blank and ASCII (the same helper); at least one condition and one edit;
  - conditions (history, questions and dialogs alike, R13): the type parses; the fields it needs resolve: AttributeScore*/AttributeIs* need `place` and `attribute`, NationScoreAtLeast and NationIsLeader need `nation`, GlobalAttr* need `attribute`, counter/flag/upgrade types need `key`; piece 3's question-only rule (no `DayAtLeast` in a question) stays; piece 3's "needs piece 5" rejection is removed;
  - edits: `place` is a known place id (`{country}_{era}`), `category` parses;
  - `HistoryChecks.Problems(edits as FactEdits with source = rule id, a FactTable built from the JSON places)` is empty;
  - **premades** (R22): **[after piece 4 merges, §8]** no premade's `place` or `truePlace` is in the Future era ("the Future place is in the world only while its nation leads");
  - **wardrobe items** (R18): **[after piece 4 merges, §8]** a non-blank `artNation` passes `LookKeys.IsToken`.
- **Piece-3 checks extended:**
  - the dialog-effect check (`CheckInterview`, R24 there) also rejects an effect with an op for which `EffectOps.HistoryOnly` holds (Y4);
  - the line-length check (R10 there) fills `{value}` with the longest value of the question's category across all places **and all history-rule edit values of that category** (R12).
- **Builders:**
  - `MakeEra` writes `isFuture`;
  - new `MakeLeaderEffect(CountryData c)` → `History/Effect_Leader_{id}.asset`: `displayName = "Leader: {displayName}"`, `channel = UI`, `defaultDurationDays = -1`, ops `[Cue CultureCue.Format(id)]`; `MakeNation` then writes `leaderEffect`;
  - `MakePlace` (221-247): no baselines for a Future place (R3), with a comment; birth years from `year` as today; piece 4's wardrobe writer copies `artNation` onto each `LookItem` **[after piece 4 merges, §8]**;
  - new `MakeCondition(ConditionData, places, attributes, nations)` shared by piece 3's questions/dialogs and the history rules (replaces piece 3's condition builder);
  - new `MakeHistoryRule(HistoryRuleData r, …)` → `History/Effect_History_{id}.asset` (`displayName = name`, channel General, `defaultDurationDays = 1`, one `SetFact` op per edit: `profile`, `category`, `stringParam`) and `History/Trigger_History_{id}.asset` (`id = "history_{id}"`, `displayName = name`, `description = "Generated from world_source.json history.rules."`, `oneShot = true`, `newsLineOnFire = news`, conditions, one outcome with `durationDaysOverride = 0`);
  - `MakeDay` unchanged (days 4–6 are ordinary entries in `dayPlanFolder`, `DayPlan_Inv_Day4..6.asset`).
- **`WireLibrary`** (292-310):
  - `historyLines` through `SerializedProperty.boxedValue` (ids `history.leaderGained`, `history.leaderLost`, `history.carry`);
  - `timelineTriggers` = the existing non-null entries outside the owned `Interview` and `History` folders, in order, then piece 3's unlock triggers, then the history triggers in JSON order;
  - `effects` = the existing non-null entries outside `History`, then the history-rule effects in JSON order, then the leader effects in country order;
  - hand-authored triggers and effects survive (FEATURES promise).
- `Generate` order: eras; leader effects; nations; places; rules; history rules; blueprint; days; books; wire; prune. The final log adds "{n} Future places, {r} history rules, {n} leader effects".
- Class doc: owns `{Eras, Nations, Places, Rules, Interview, History}`; merges generated triggers and effects after the hand-authored ones.

### 2.13 Validator (`ContentLibraryValidator`, CRLF)

`ValidateLibrary` (50-93) gains these checks (errors unless noted):
- **`CheckFuture`:** at most one era `isFuture`; if one exists, every nation has a place in it; every nation has a `leaderEffect` with channel UI and a `Cue` op for which `CultureCue.TryParse` gives that nation's id (warning); a Future place with baselines (warning: it would join the tiers); after piece 4 merges (§8): a library premade (`LegendarySO`) whose claimed era (`trueEra`) or `truePlace` era is the Future (error, R22), and a wardrobe item whose `artNation` fails `LookKeys.IsToken` (error, R18).
- **`CheckDayPlanPlaces`** (146-200) runs its checks once per possible Future: for a plan that includes the Future era, once with no leader and once per nation with a Future place (`TodaysProfiles(plan, id)`); otherwise once with null. With no leader the Future era is exempt from "every weighted era has a place". New: a plan that weights the Future must allow every nation that has a Future place; a NationEraForbidden rule naming the Future era is an error (Y5).
- **`CheckTriggers`:** for every trigger, each condition's required reference for its type (profile + attribute, attribute, nation, key); a missing one is an error "can never pass" (this is what hid the three dead triggers). A trigger whose outcome effect holds an op for which `EffectOps.HistoryOnly` holds must be `oneShot` (error: a repeatable one would latch an edit and print its line every night, and `History.Latch` does not deduplicate).
- **`CheckEffects`:** duplicate effect asset names; `AddAttributeScore` needs profile and attribute; `AddNationScore` needs a nation; `SetFact` needs a profile, an editable category and a value. **Y4:** an effect with an op for which `EffectOps.HistoryOnly` holds must be referenced by at least one trigger outcome and by no slot outcome, upgrade `unlockEffect`, attribute baseline tier effect, nation `leaderEffect` or piece-3 dialog choice.
- **`CheckHistoryValues`:** `HistoryChecks.Problems` over every `SetFact` op (as a `FactEdit` named by its effect) against `lib.BuildWorldFacts(null)` (authored facts, no history).
- **`CheckEndings`:** AttrTotalAtLeast needs an attribute and a threshold above 0; DayAtLeast a threshold of at least 1. A warning when an AttrTotalAtLeast ending exists but no DayAtLeast ending does (`EndingRules.Select` lets an epilogue win only when a milestone is met, so it could never fire).
- `RequiredFacts` (95-97) doc: "papers + books + questions (piece 4 adds Culture)".

### 2.14 Content

**Future places** (`places[]`, era `future`, year 2150; values ASCII, at most 28 characters, none equal to another place's value in its category). These are placeholder writing for Saleh's review:

| id | displayName | Currency | Language | Technology | Geography | Politics |
|---|---|---|---|---|---|---|
| egypt_future | Nile Arcology | Nile credit (digital) | Cairene Arabic-English | Solar desalination tower | New Administrative Capital | Speaker Amira Nasser |
| iraq_future | Baghdad Garden City | Mesopotamian dinar token | Unified Iraqi Arabic | Vertical date-palm farm | Baghdad (Garden City) | President Layla Kareem |
| greece_future | Aegean Commonwealth | Aegean euro-drachma | Pan-Hellenic Greek | Wave-power island grid | Athens (Commonwealth seat) | Archon Eleni Pappas |
| italy_future | Mediterranean Union Rome | Mediterranean lira (eLira) | Standard Italian (voice) | 3D-printed stone vaults | Rome (Union capital) | Consul Marco Ferri |
| china_future | Shanghai Megacity | Digital yuan (e-CNY) | Mandarin (pinyin-first) | Maglev commuter pod | Shanghai (Pudong capital) | Premier Chen Yuming |
| japan_future | Neo-Tokyo Bay | Crypto-yen (J-coin) | Tokyo Japanese (kana-first) | Companion robot | Neo-Tokyo (Bay Capital) | Prime Minister Hana Sato |
| britain_future | Thames Barrier London | Sterling token (GBT) | Global English | Fusion reactor (Thames) | London (Thames Barrier) | First Minister Ada Clarke |
| germany_future | Rhine-Ruhr Metropole | Euro-mark (digital) | Standard German (Denglisch) | Hydrogen hyperloop | Cologne (Rhine-Ruhr seat) | Chancellor Lena Vogt |

Names (8 male / 8 female each):
- egypt: Omar, Karim, Youssef, Adam, Ziad, Hamza, Seif, Malik / Nour, Farida, Salma, Malak, Jana, Laila, Hana, Rania;
- iraq: Ali, Mustafa, Haider, Yusuf, Mahdi, Sajjad, Zaid, Ammar / Zahra, Maryam, Ruqaya, Sara, Dania, Tabarak, Hawraa, Aya;
- greece: Nikos, Giorgos, Kostas, Dimitris, Alexandros, Petros, Stavros, Yannis / Eleni, Maria, Sofia, Katerina, Despina, Ioanna, Anastasia, Chrysa;
- italy: Leonardo, Francesco, Tommaso, Lorenzo, Mattia, Riccardo, Gabriele, Edoardo / Aurora, Giulia, Ginevra, Beatrice, Alice, Vittoria, Chiara, Martina;
- china: Haoyu, Zihan, Yichen, Junjie, Tianyi, Minghao, Zhiyuan, Bowen / Ruoxi, Yutong, Xinyi, Shiyu, Jiaxin, Mengqi, Wanting, Yuxi;
- japan: Haruto, Sota, Ren, Minato, Yuto, Riku, Kaito, Asahi / Himari, Yui, Mei, Tsumugi, Rin, Sakura, Koharu, Mio;
- britain: Oliver, Noah, Arlo, Theo, Leo, Oscar, Idris, Rohan / Olivia, Amelia, Isla, Ava, Freya, Ivy, Aisha, Priya;
- germany: Emil, Finn, Paul, Elias, Mats, Ben, Leon, Jonas / Emilia, Mia, Lina, Clara, Ella, Frieda, Leni, Yasmin.

**After piece 4 merges (§8):** Each Future place also gets a `wardrobe` block in piece 4's final format, written from `SCRATCH/costumes.json` `futureMotifs` for that country: the outfit per gender is the culture-shaped Future outfit and is the signature slot, so piece 4's generator derives a Culture value unique across all 48 places (checked there); the hair and facial-hair items carry `"artNation": "neutral"` (R18), sharing `hair_{g}_neutral_future_{colour}` and `facialhair_m_neutral_future_{colour}`. `facts[]` has no Culture entry.

**`history.lines`:**

| id | text |
|---|---|
| history.leaderGained | HISTORY: {nation} now dominates the timeline. The Future belongs to {place}. |
| history.leaderLost | HISTORY: {nation} no longer dominates the timeline. The Future is unsettled until a new power rises. |
| history.carry | HISTORY: travellers brought {value} to {place}. |

**`history.rules`** (11; every value checked by `HistoryChecks`):

| id | name | conditions | edit | news |
|---|---|---|---|---|
| movable_type_florence | Movable type reaches Florence | NationIsLeader china | italy_medieval Technology "Chinese movable type press" | HISTORY: Florentine printers set type the Chinese way, years before Gutenberg. |
| yen_reichsmark | The yen backs the Reichsmark | NationIsLeader japan | germany_modern Currency "Yen-backed Reichsmark" | HISTORY: Weimar Berlin's banks peg the Reichsmark to the yen. |
| roman_aljabr | Al-jabr reaches Rome | NationIsLeader iraq | italy_ancient Technology "Al-jabr reckoning tables" | HISTORY: Roman engineers reckon with al-jabr tables from Baghdad. |
| sterling_cairo | Sterling on the Nile | NationIsLeader britain | egypt_industrial Currency "British pound sterling" | HISTORY: Khedivate Cairo now trades in British sterling. |
| greek_thebes | Greek scribes in Thebes | NationIsLeader greece | egypt_ancient Language "Greek-Egyptian (Koine)" | HISTORY: New Kingdom scribes now write in Greek. |
| mark_britain | The mark crosses the Channel | NationIsLeader germany | britain_modern Currency "Deutsche Mark & shilling" | HISTORY: Post-war Britain pays in Deutsche Marks. |
| florin_mystras | Florins in the Morea | NationIsLeader italy | greece_medieval Currency "Florentine florin (Morea)" | HISTORY: Mystras now trades in Florentine florins. |
| hieroglyphs_babylon | Hieroglyphs in Babylon | NationIsLeader egypt | iraq_ancient Language "Hieroglyphic Akkadian" | HISTORY: Babylon's scribes carve hieroglyphs beside their cuneiform. |
| babbage_engine | The analytical engine runs | GlobalAttrAtLeast science 22 | britain_industrial Technology "Babbage analytical engine" | HISTORY: Babbage's analytical engine hums in Victorian London. |
| athenian_steam | Steam in Athens | AttributeIsDominant greece_ancient science | greece_ancient Technology "Aeolipile steam engine" | HISTORY: Athenian engineers put the steam engine to work. |
| elected_pharaoh | An elected pharaoh | GlobalAttrAtLeast democracy 12 | egypt_ancient Politics "Elected Pharaoh Hatshepsut" | HISTORY: Thebes votes: Hatshepsut now rules as an elected pharaoh. |

**`rules[]`** (new): `Rule_NoIndustrialBritain` (NationEraForbidden britain industrial, "Cholera quarantine: no travel to Victorian Britain today."), `Rule_NoModernGermany` (germany modern, "Border closed: no travel to Weimar Berlin today."), `Rule_NoModernJapan` (japan modern, "Typhoon warning: no travel to Showa Tokyo today.").

**`days[]`** (new, each era weight 1, `countries: []`, `tells: 1`, `channels` = day 3's, and, after piece 4 merges (§8), piece 4's premade fields: `premades` = day 3's pool of seven, `forced: []`, `premadeChance` 0.05, R22):
- `DayPlan_Inv_Day4`: day 4, queue 12, eras ancient…industrial, rules `[Rule_NoIndustrialBritain]`;
- `DayPlan_Inv_Day5`: day 5, queue 13, + modern, rules `[Rule_NoModernGermany, Rule_NoAncientEgypt]`;
- `DayPlan_Inv_Day6`: day 6, queue 14, + future, rules `[Rule_NoModernJapan]`.

**Hand-edited assets** (authored content, not generated):
- `Trigger_ArtRenaissance.asset`: condition `type: 12` (GlobalAttrAtLeast), attribute Art, threshold 20.
- `Trigger_ScienceBoom.asset`: `type: 12`, attribute Science, threshold 15.
- `Trigger_DemocracyCollapse.asset`: conditions `type: 13` (GlobalAttrAtMost), attribute Democracy, threshold −6; and `type: 8` (DayAtLeast), threshold 3. (Without the negative threshold and the day guard it would fire on night 1: a missing total reads 0.)
- `Ending_Artistic_golden_age.asset`, `Ending_Scientific_age.asset`, `Ending_Democracy_triumphant.asset`: thresholds from the §6 step 3.9 report (the 65th percentile of perfect play's day-15 total, R14), first run with 45, 32 and 15 (priorities unchanged, 50).
- `GameConfig_Default.asset`: the six history knobs written explicitly (`leaderFloor: 4`, `leaderKeepFloor: 2`, `leaderMargin: 2`, `carryThreshold: 1`, `carryCategory: 3`, `maxHistoryNewsPerNight: 3`), so they never silently fall back to code defaults. The pre-existing unserialized knobs (evidence gate, shift clock, dominance, Home) are left as they are (§4).

**Generated** (Generate World): `Era_future` (`isFuture`), `Nation_*` (`leaderEffect`), `Place_{country}_future` ×8, three rules, `DayPlan_Inv_Day4..6`, `History/` (11 `Trigger_History_*`, 11 `Effect_History_*`, 8 `Effect_Leader_*`), `ContentLibrary_Main` arrays and `historyLines`.

### 2.15 Knobs

| Knob | Where | Value | Authored in |
|---|---|---|---|
| Leader floor / keep floor / margin | `GameConfigSO.leaderFloor` / `leaderKeepFloor` / `leaderMargin` | 4 / 2 / 2 | `GameConfig_Default.asset` |
| Carry threshold / category | `GameConfigSO.carryThreshold` / `carryCategory` | 1 / Technology | same |
| History news cap | `GameConfigSO.maxHistoryNewsPerNight` | 3 | same |
| Future places and their facts | `NationEraProfileSO` (era `future`) | §2.14 | `world_source.json` places |
| History rules | generated triggers + effects | §2.14 | `world_source.json` history.rules |
| History news wording | `ContentLibrarySO.historyLines` | §2.14 | `world_source.json` history.lines |
| Days 4–6 (eras, queue, rules, channels, tells, premade pool) | `DayPlanSO` | §1.7 | `world_source.json` days |
| Future art sharing **[after piece 4 merges, §8]** | piece 4's `LookItem.artNation` | `neutral` on Future hair and facial hair | `world_source.json` wardrobes |
| Trigger and ending thresholds | `Trigger_*`, `Ending_*` assets | §2.14 (epilogues from the §6 report) | inspector |
| Dominance tier sizes | `GameConfigSO.dominantPerProfile` / `supportingPerProfile` | 1 / 2 (unchanged) | — |

### 2.16 Contracts for pieces 4 and 6 (and PR #3)

**Piece 4 (characters)**, matching its R1, R27 and §2.20 (all applied after piece 4 merges, §8):
- The eight Future places are ordinary places with a `wardrobe` block (and optional `looks`) in piece 4's format, which piece 5 authors (it lands after piece 4) from `costumes.json` `futureMotifs`. Their `facts[]` hold the five editable categories only; piece 4's generator derives their Culture fact from the wardrobe's signature items (unique across all 48 places, at most `FactTable.MaxValueLength`).
- Piece 5 delivers piece 4's R27 as R18: `LookItem.artNation` (the nation token in the art file names; `neutral` gives `hair_{g}_neutral_future_{colour}`, the name piece 4 asked for), used by `Looks.Compose` and `LookKeys.Required`, and the `Looks.CanLeak` row that refuses a leak drawing the same art as the claim's item in that slot. The Future signature is the outfit, so a Future home never leaks dress (piece 4's row 3).
- A Future traveller exists only while a nation leads, and their place's country **is** the leader, so the culture-shaped Future outfit is the dominant culture's.
- History never edits Culture (R12); outfits follow the place, facts follow history.
- The fact width has one home, `FactTable.MaxValueLength`; piece 4's `Looks.MaxCultureLength` is removed (R20).
- No premade claims or comes from a Future place: generator and validator errors (R22).
- Days 4–6 carry `channels` with Appearance and day 3's premade pool (R22).
- Deferred with owners (§4): Industrial and Modern premades, premade multi-day arcs; history-driven dress never happens (R12).

**Piece 6 (UI reacts)**, matching its §2.2 C1–C7:
- **Present culture (C1):** `WorldState.history.leaderId` ("" = neutral); `history.ranking` (saved, highest first) for blends.
- **Cue (C2):** while a nation leads, `TimelineEffects.GetCues(world, lib, EffectChannel.UI)` contains `CultureCue.Format(id)` (the leader effect, rebuilt every night and by the force-leader cheat). A `TimelineCueReceiver` on the UI channel sees it at scene start; piece 6 applies it on `sceneLoaded` (its Z2). The grammar is Domain `CultureCue` (`Prefix`, `Format`, `TryParse`); piece 6 adds `Pick` there.
- **Emission step (C3):** `HistoryService.RebuildLeaderEffect(world, lib, startDay)`, idempotent (removes its own entries through `TimelineService.RemoveEffectsFrom`, then activates); private in piece 5, made public by piece 6 for its Continue call.
- **Force leader (C4):** `HistoryService.ForceLeader` sets the leader and its cue at once and keeps it through every night of the session (R19), so piece 6's force-culture smoke (its U15) holds across Skip Day or Sleep; piece 6's "apply culture now" reloads the scene for the same day.
- **Future currency for the wallet (C6):** `lib.BuildWorldFacts(world.history).Get(leaderId, lib.FutureEra.id, ClueCategory.Currency)` when a leader exists; "credits" otherwise.
- **Revised-row markers (C7, its R14):** `ContentLibrarySO.FillFacts` is the only code that sees a fact's authored value and its resolved value together; piece 6 adds its `table.MarkChanged(...)` call there when `!DiscrepancyLog.ValuesMatch(resolved, authored)`. Piece 5 adds no marker API.
- **History news text (C5, its R2):** filled at night with `Interview.Fill` and saved as finished English text in `WorldState.tomorrow.newsLines` (R21). The `history.*` ids are content ids; keyed news (line id plus arguments in the package) is piece 6's deferred F2.

**PR #3 (Marwan):** its resubmission should build on `ScoreKey`, `ScoreRanking` and the latched leader (`history.ranking`, `leaderId`) instead of a second ranking; booth layers that need a Visuals-channel cue add a second generated effect per nation in the same generator step (not in piece 5).

### 2.17 Every file that changes

Found with `grep` over `Assets` for `BuildFactTable`, `TodaysProfiles`, `GetDayPlan`, `TimelineKeys`, `EndingConditionType`, `EndingService.Evaluate`, `HandleSleep`, `AdvanceToNextDay` (including the debug panel's Skip Day, `DebugPanelController.cs:100`), `GoHomeOrAdvance`, `LoadOfficeScene`, `NightlyResolve`, `ActivateEffect(`, `RecomputeDominance`, "is rising", `new CaseFactory(`, `sourceLabel.StartsWith`, `ValuesMatch(row.Value`, `DevToolsState`; piece 3's and piece 4's new files are named from their specs (`EffectOpType`, `MaxCultureLength`, `LookItem`, `CanLeak`, `Required(`):

| File | Assembly | Change |
|---|---|---|
| `Assets/Scripts/Domain/ScoreKey.cs` (+meta) | Domain | new: builders + ported parser |
| `Assets/Scripts/Domain/ScoreRanking.cs` (+meta) | Domain | new: `RankedScore`, `ScoreRanking`, `DominanceTier`, `DominanceTiers` |
| `Assets/Scripts/Domain/Influence.cs` (+meta) | Domain | new: `PlaceRef`, `Influence` |
| `Assets/Scripts/Domain/NationLeader.cs` (+meta) | Domain | new |
| `Assets/Scripts/Domain/History.cs` (+meta) | Domain | new: `EditCause`, `FactEdit`, `CarryRecord`, `HistoryState`, `HistoryLines`, `History` (`EditableCategories`, `NationToken`, `IsEditable`, `FutureNation`, `InWorld`, `Resolve`, `Latch`, `NewsSlots`) |
| `Assets/Scripts/Domain/Carries.cs` (+meta) | Domain | new |
| `Assets/Scripts/Domain/HistoryChecks.cs` (+meta) | Domain | new |
| `Assets/Scripts/Domain/EndingRules.cs` (+meta) | Domain | new: `EndingConditionType` (moved), `EndingKind`, `EndingMoment`, `EndingCandidate`, `EndingRules` |
| `Assets/Scripts/Domain/CultureCue.cs` (+meta) | Domain | new: `Prefix`, `Format`, `TryParse` (R20) |
| `Assets/Scripts/Domain/DayPlans.cs` (+meta) | Domain | new: `Pick` (R17) |
| `Assets/Scripts/Domain/Gates.cs` (piece 3, LF) | Domain | three condition types, `GateSnapshot.LeaderId`, `Passes` rows (§2.5) |
| `Assets/Scripts/Domain/EffectOps.cs` (piece 3, LF) | Domain | `SetFact` appended with its doc; `HistoryOnly`; `ActsWhileActive` doc (§2.5) |
| `Assets/Scripts/Domain/LookData.cs`, `Looks.cs`, `LookKeys.cs` (piece 4, LF) | Domain | **[after piece 4 merges, §8]** `LookItem.artNation`/`ArtNation`; `Compose` and `Required` use it; `CanLeak` row 6; `Looks.MaxCultureLength` removed (§2.5, R18, R20) |
| `Assets/Scripts/Domain/FactTable.cs` | Domain | `MaxValueLength`, `TryFindOtherPlaceWith`, class doc 40-47 (§2.4) |
| `Assets/Scripts/Domain/Forgery.cs` | Domain | `IsProvableTell`'s uniqueness loop (40-45) → `TryFindOtherPlaceWith` |
| `Assets/Scripts/TodaysWorld.cs` (+meta) | Assembly-CSharp | new |
| `Assets/Scripts/Timeline/HistoryService.cs` (+meta) | Assembly-CSharp | new (§2.8) |
| `Assets/Scripts/ContentLibrarySO.cs` | Assembly-CSharp | `TodaysProfiles(plan, future)`, `BuildToday`, `BuildWorldFacts`, `FillFacts`; − `BuildFactTable`; `historyLines`, `FutureEra`, `GetNationById`, `GetProfileById`, `GetDayPlan` fallback |
| `Assets/Scripts/CaseFactory.cs` | Assembly-CSharp | §2.9 |
| `Assets/Scripts/GameManager.cs` | Assembly-CSharp | §2.10 |
| `Assets/Scripts/WorldState.cs` | Assembly-CSharp | `history`, `phase`, `RunPhase` |
| `Assets/Scripts/EraSO.cs` | Assembly-CSharp | `isFuture` |
| `Assets/Scripts/Timeline/NationSO.cs` | Assembly-CSharp | `leaderEffect` |
| `Assets/Scripts/EffectSO.cs` | Assembly-CSharp | `EffectOp.category`, docs (the enum is in `EffectOps.cs` after piece 3) |
| `Assets/Scripts/Timeline/TimelineTriggerSO.cs` | Assembly-CSharp | `TriggerCondition` docs, class doc |
| `Assets/Scripts/Timeline/TimelineService.cs` | Assembly-CSharp | `TimelineKeys` wrappers; `NightlyResolve`, `TomorrowPlaces`, `RecomputeDominance`, `SeedDominance`, `RemoveEffectsFrom`, `RebuildTierEffects` (uses it), `ActivateEffect`, piece 3's `ToGate`/`Snapshot`; docs |
| `Assets/Scripts/Core/GameConfigSO.cs` | Assembly-CSharp | History knobs, `OnValidate` |
| `Assets/Scripts/Core/RunManager.cs` | Assembly-CSharp | `ResumeRun`, `Sleep`, phase in `AdvanceToNextDay`, `GoHomeOrAdvance`, docs |
| `Assets/Scripts/Home/HomeManager.cs` | Assembly-CSharp | `HandleSleep` → `RunManager.Sleep`; docs |
| `Assets/Scripts/TitleSceneController.cs` | Assembly-CSharp | `ResumeRun`; doc |
| `Assets/Scripts/Endings/EndingService.cs` | Assembly-CSharp | `EndingMoment`, `EndingRules.Select` |
| `Assets/Scripts/Endings/EndingSO.cs` | Assembly-CSharp | enum moved out; doc |
| `Assets/Scripts/DevTools/DebugPanelController.cs` | Assembly-CSharp | §2.11 (Skip Day → `Sleep`, history cheats and inspector) |
| `Assets/Scripts/DevTools/DevToolsState.cs` | Assembly-CSharp | `ForcedLeaderId`, `ResetAll`, class doc (§2.8) |
| `Assets/Scripts/Investigation/ReferenceBookSO.cs`, `Assets/Scripts/Timeline/NationEraProfileSO.cs` | Assembly-CSharp | docs: `BuildFactTable` → `ContentLibrarySO.BuildToday` (lines 6 and 9) |
| `Assets/Editor/WorldContentGenerator.cs` | Editor | §2.12 (including piece 3's dialog-effect and line-length checks, and, after piece 4 merges, piece 4's `ItemData.artNation` and Culture-length constant) |
| `Assets/Editor/ContentLibraryValidator.cs` | Editor | §2.13 (and, after piece 4 merges, piece 4's Culture-length constant) |
| `Assets/Data/World/world_source.json` | content | §2.12, §2.14 |
| `Assets/Data/World/Eras/Era_future.asset`, `Nations/Nation_*.asset`, `Places/Place_*_future.asset` (+metas), `Rules/Rule_No{IndustrialBritain,ModernGermany,ModernJapan}.asset` (+metas), `History/` (+folder meta, 30 assets + metas) | generated | Generate World |
| `Assets/Data/Investigation/DayPlan_Inv_Day4..6.asset` (+metas) | generated | new (Generate World) |
| `Assets/Data/Content Library/ContentLibrary_Main.asset` | generated | arrays, `historyLines` |
| `Assets/Data/Triggers/Trigger_{ArtRenaissance,ScienceBoom,DemocracyCollapse}.asset` | authored | retuned conditions |
| `Assets/Data/Endings/Ending_{Artistic_golden_age,Scientific_age,Democracy_triumphant}.asset` | authored | thresholds |
| `Assets/Data/Config/GameConfig_Default.asset` | authored | History knobs |
| `Assets/Tests/EditMode/{ScoreKey,ScoreRanking,DominanceTiers,Influence,NationLeader,History,Carries,HistoryChecks,EndingRules,CultureCue,DayPlans}Tests.cs` (+metas) | tests | new, §5 |
| `Assets/Tests/EditMode/GatesTests.cs`, `EffectOpsTests.cs` (piece 3, LF) | tests | §5 |
| `Assets/Tests/EditMode/LooksTests.cs`, `LookKeysTests.cs` (piece 4, LF) | tests | §5 **[after piece 4 merges, §8]** |
| `Assets/Tests/EditMode/FactTableTests.cs` (LF) | tests | §5 (`TryFindOtherPlaceWith`); `ForgeryTests.cs` unchanged and green (pins the loop extraction) |
| `docs/FEATURES.md` | docs | §3.3 |
| `docs/superpowers/specs/2026-09-24-history-facts-design.md` | docs | this spec, committed first |

Checked and unchanged: `SaveSystem.cs` (version 2), `DayFlowUIController.cs` (it already lists the news), `TimelineEffects.cs`, `TimelineCueReceiver.cs`, `TimelineReactiveSprite.cs`, `DayPlanSO.cs`, `CaseInstance.cs`, `Lies.cs`, `DiscrepancyLog.cs`, `ForgeryTests.cs`, `InvestigationUIController.cs`, `ShiftScoring.cs`, `OfficeSceneUIBuilder.cs` (no scene rebuild), `HomeUIController.cs`, `SeedsTests.cs` (no new stream).

### 2.18 Why some logic stays outside Domain

The glue is `HistoryService`, `TimelineService` (night order, tier keys, `RemoveEffectsFrom`, `ActivateEffect`), `ContentLibrarySO` (the SO iteration of `TodaysProfiles`, `BuildToday`, `FillFacts`, `GetDayPlan`, lookups), `CaseFactory.PickEraFromPlan`, the run flow (`GameManager`, `RunManager.ResumeRun`/`Sleep`, `TitleSceneController`, `HomeManager`), `EndingService.Matches`, `GameConfigSO.OnValidate`, `DevToolsState`, the debug panel, the generator and the validator. It reads ScriptableObjects, loads scenes, writes `WorldState`, formats news and logs, or runs in the editor. Every decision it makes is a call into tested Domain code: `ScoreKey`, `ScoreRanking`, `DominanceTiers`, `Influence`, `NationLeader`, `History` (`FutureNation`, `InWorld`, `Resolve`, `Latch`, `NewsSlots`), `Carries`, `HistoryChecks`, `FactTable.TryFindOtherPlaceWith`, `DayPlans.Pick`, `CultureCue`, `EffectOps.HistoryOnly`, `EndingRules`, `Gates`, `Looks`/`LookKeys`, `WeightedRandom`. Three small choices stay in glue, each with a reason:
- the resume choice (Home or Office by phase) is two branches of scene loading;
- `PickEraFromPlan`'s "an era with no place today weighs 0" is one identity test over SO references (`_todays.Any(p => p.era == ew.era)`); the draw itself is `WeightedRandom.Pick` (tested, zero weights skipped), and §6 step 3.3 proves no Future traveller without a leader;
- the force-leader override (`DevToolsState.ForcedLeaderId ?? NationLeader.Decide(...)`) is a dev-only cheat, not a game rule; §6 step 5 checks it.
The tomorrow-places filter is one set lookup. The night order, the SetFact write, the JSON round trip of `HistoryState`, the generator and the validator are verified in Unity (§6).

### 2.19 Intent audit: the existing mechanism tried for each new one

| New | Existing mechanism reused or tried (redo/override check) | Content-only path? | Why new code was still needed |
|---|---|---|---|
| Fact edits | `FactTable` + the `BuildFactTable` seam (reused as `FillFacts`); `ProfileFact` assets (mutating them was rejected: breaks the generator and saves) | no | nothing stored a changed fact |
| Rule vehicle | `TimelineTriggerSO` + `EffectSO` + fired flags + `newsLineOnFire` (reused whole) | yes, once `SetFact` exists | one op and one field: no op touched facts |
| Leader | `RecomputeDominance`'s ranking (moved to Domain and shared), PR #3's `ScoreRanking` design | no | nothing ranked nations against each other (`TriggerConditionType` had only absolute conditions) |
| Leader cue | tier effects + `Cue` op + `TimelineEffects.GetCues` + `TimelineCueReceiver` (reused; the leader effect is a generated asset) | yes (the effects are content) | only the nightly re-activation is new; it shares `RemoveEffectsFrom` with `RebuildTierEffects` and the cue grammar with piece 6 (`CultureCue`) |
| Future selection | `TodaysProfiles` plan filter (one more condition, `History.InWorld`) and `WeightedRandom`'s zero weights | no | the Future place depends on history |
| Future art sharing | piece 4's `LookKeys` grammar and `CanLeak` table (extended, not duplicated) | yes, once the field exists (`artNation` is content) | piece 4 assigned the field and the leak row to piece 5 (its R27) |
| Force-leader cheat | `DevToolsState` (session cheats, cleared by `ResetAll`) and the Skip Day button (rerouted through `Sleep`) | no | the nightly decision would undo a forced leader |
| Uniqueness lookups and the fact width | `Forgery.IsProvableTell`'s loop and piece 4's `Looks.MaxCultureLength` (moved to `FactTable`, not copied) | — | history checks and warnings ask the same question |
| Carries | `ApplyVerdictImpacts` at accept (same moment), `WorldState` lists, `Lies`' true home | no | nothing remembered an accepted liar past the shift (the ledger is not saved) |
| History news | the tomorrow package and `DayFlowUIController` (reused unchanged) | yes (wording is content) | templated lines need a fill helper (piece 3's `Interview.Fill`, reused) |
| Condition types | piece 3's `Gates` + `TriggerCondition` (three appended values) | — | "leads all others" and global totals had no condition |
| Endings timing | `EndingService` + `EndingSO` priorities (reused; selection moved to Domain) | no | the check had no notion of moment |
| Resume | `SaveSystem` + `WorldState` (one field), `GoHomeOrAdvance` (reused as the Home path) | no | Continue always loaded the Office |
| Day plans 4–6 | `MakeDay` and `days[]` (reused) | yes | only the fallback rule is code (`DayPlans.Pick`) |
| Validator proof | `ContentLibraryValidator`, `DiscrepancyLog.ValuesMatch`, `DialogChecks`' shared-rules pattern (piece 3) | — | no check covered history values or condition fields |

## 3. Retired or superseded

### 3.1 Code removed or replaced

- `ContentLibrarySO.BuildFactTable(DayPlanSO)` (101-122): replaced by `BuildToday`/`BuildWorldFacts`/`FillFacts`.
- `CaseFactory.GenerateDayCases`' own `TodaysProfiles` call (79).
- `RecomputeDominance`'s `ranked.Sort` (203) and the "is rising" branch (218-224).
- `TimelineKeys`' string formats (11-26): now in `ScoreKey`.
- `EndingConditionType` in `EndingSO.cs` (8-21): moved to Domain.
- `HomeManager.HandleSleep`'s ending check and advance (262-279): moved to `RunManager.Sleep`.
- `ContentLibrarySO.GetDayPlan`'s exact-match loop (271-287): `DayPlans.Pick`.
- `RebuildTierEffects`' inline `RemoveAll` (243-244): `TimelineService.RemoveEffectsFrom`.
- `Forgery.IsProvableTell`'s uniqueness loop (40-45): `FactTable.TryFindOtherPlaceWith`.
- The debug panel's direct `run.AdvanceToNextDay()` (100): `run.Sleep()`.
- Piece 4's `Looks.MaxCultureLength`: `FactTable.MaxValueLength`.
- Piece 3's generator rejection of profile/attribute/nation condition types (its R6).

### 3.2 Earlier spec lines superseded (kept as approved records, not edited)

**`docs/superpowers/specs/2026-09-24-world-model-design.md` (piece 1):**
- §1 line 9: "The Future era exists as the office's own time, but nobody travels from it yet (piece 5)." Eight Future places exist; the leader's is in the world on Future days (§1.2).
- §1 lines 10-16 (three-day ramp): extended by days 4–6 and the latest-plan fallback (§1.7).
- §1 line 21: "Same run + same day = same visitors." Now same run + same day + same history (§1.9).
- §1 line 23 (morning paper reports tier changes): only newly DOMINANT attributes of tomorrow's places, plus history lines (§1.6).
- §2 line 39: "`ContentLibrarySO.BuildFactTable(DayPlanSO)` is the only code that reads profile facts … the seam piece 5 extends": the seam is `FillFacts` behind `BuildToday` and `BuildWorldFacts` (R4).
- §2 line 51 (the generator creates 40 profiles and 3 day plans): 48 places, 6 day plans, the History folder and leader effects.
- §4 line 66: "Days 4+ (they keep replaying the inspector fallback plan)." Delivered (H7).
- §4 line 68: "History-dependent facts (piece 5)." Delivered.
- §4 line 70: "Saving the day's generation inputs, so Continue after the end-of-shift save regenerates the day." Continue no longer regenerates a finished day (H11); nothing else is saved.
- §6 line 94 (generator owns `{Eras,Nations,Places,Rules}`, only drops missing entries elsewhere): it also owns `Interview` (piece 3) and `History`, and merges its generated triggers and effects after the hand-authored ones, which still survive.
- §6 line 97: "The timeline ranking … moving them is out of scope here (piece 5 reworks the timeline)." Delivered: `ScoreRanking`, `DominanceTiers`, `NationLeader` in Domain (Y6).

**`docs/superpowers/specs/2026-09-24-identity-lies-design.md` (piece 2):**
- §1.4 lines 108-109: "Same run + same day = same travellers … For any seed, claims, names, … are identical to piece 1": on days whose places or facts follow history (a Future day, or any day after an edit latched), they are identical only for the same history. With an empty history nothing changes.
- §2.5 line 354: "Timeline: no code change." Piece 5 changes the timeline (history steps, SetFact, ranking); the impacts' landing place (line 102) is unchanged.
- §4 line 508 (handed to piece 5): history-dependent facts, Future travellers and "consequences of accepted liars" are delivered (the consequence is the carry into the **claimed** place; the true home is unchanged); "embargo evader" motives stay deferred (§4).
- R13 (line 49, "piece 5 history edits can add more") and §7 line 619: authored edits are proven unique (R5); carries share values by design and R13 degrades them.

**Piece 3 (`docs/superpowers/specs/2026-09-24-dialog-questions-design.md`, committed on `feat/dialog-questions` at `fbf4905`):**
- R10 and §2.12 line length: `{value}`'s longest value per category also covers history-rule values (R12).
- R24 and §2.12 dialog effects: besides `ActsWhileActive`, an op for which `EffectOps.HistoryOnly` holds is rejected (Y4); §2.2 `EffectOps` gains `SetFact` and `HistoryOnly`.
- §1.8 "Continue replays the day" and its bullets, §7 "Replay after the end-of-shift save", §4 "saving the day's generation inputs (the Continue replay gap)": Continue resumes at Home (H11). X5's end-of-shift application stays as the moment dialog effects apply; the one-shot rule for dialogs with a consequence stays as a content rule (piece 4's repeatable consequence dialogs no longer need a replay-safe design beyond this).
- R6 (the generator rejects profile/attribute/nation conditions until piece 5): lifted (R13).
- R22 and §4 ("piece 5 owns the fix" of the three dead triggers; history-dependent facts; score-gated questions; Future travellers): delivered; score-gated questions are now possible, none authored. Timeline-reactive Chatter talk stays deferred (§4).
- §2.2 `GateSnapshot` constructor: gains `leaderId` (§2.5).
- §3.3 FEATURES :12 wording: rewritten again (§3.3 below).

**Piece 4 (`specs/2026-09-24-characters-design.md` in the session scratchpad, draft):**
- R27 and §2.20 "a per-item art override (a place token such as `neutral_future`)": delivered as a nation token, `LookItem.artNation` (`neutral`), which produces the same file names and keeps the `{layer}_{g}_{nation}_{era}` grammar (R18).
- §2.4 `CanLeak`: a row is inserted after row 5 (same art as the claim's item → false); rows 6–7 become 7–8.
- §2.4 `Looks.MaxCultureLength` and R1's "at most `Looks.MaxCultureLength` (28)": `FactTable.MaxValueLength` (R20).
- §2.20 "H10's uniqueness simulation should include Culture": Culture is not editable (R12), so history cannot break piece 4's Culture uniqueness; R5's pairwise check covers the editable categories.
- §4 "Piece 5: … premade arcs, Industrial and Modern premades, history-driven dress": Future wardrobes are delivered; the rest is deferred or never (§4 below).

### 3.3 `docs/FEATURES.md` (same commits as the behaviour)

Line numbers are from `efe385d`; at `1a3e624` the same bullets are :9, :10, :11, :12, :52 (World: 40 places), :56 (liars), :84 (queue), :104 (impacts), :105 (dominance news), :106 (tiers), :112 (Generate World), :113 (validator), :115 (travel rules), :118 (debug panel); the bullets are matched by their text, and the wording that needs piece 4 (the Future outfits and hair) is added after piece 4 merges (§8).

- **:9:** "… Continue resumes where the save was made: at Home after the end-of-shift save, otherwise in the Office (saves without a phase resume in the Office)".
- **:10:** "Day plans per day number from `ContentLibrary_Main`; a day without its own plan reuses the latest earlier plan (days 7+ replay day 6's; the inspector plan only when the library has none) (plan choice tested: `DayPlansTests`). Ramp: … day 4 + Industrial, no Victorian Britain (12); day 5 + Modern, no Weimar Berlin / New Kingdom Egypt (13); day 6 + the Future (while a nation leads), no Showa Tokyo (14)".
- **:11:** "Deterministic case generation: same run + same day + same history (leader and latched fact edits) = same travellers; … history draws no random number".
- **:12:** "Endings: fired and bankrupt are checked after every verdict, at the end of a shift that applied dialog consequences, and at sleep (the debug Skip Day sleeps too); Retirement (day 15) and the attribute epilogues only at sleep, where an epilogue whose total is reached (thresholds from the run report, R14) replaces Retirement (selection tested: `EndingRulesTests`; the checks' timing is checked in Unity)".
- **:40:** "… = 40 places, plus eight Future places (the office's own time, 2150), of which only the leading nation's is in the world (see History)".
- **:44:** add "Future travellers claim the leading nation's Future place (none while no nation leads); a liar may claim the Future or come from it".
- **:68:** "(8 / 10 / 12 / 12 / 13 / 14 on days 1–6; later days as day 6)".
- **:88:** "Timeline impacts apply only on ACCEPT and land on the claimed place, liar or not; sends tracked per era; an accepted liar also carries their true home's Technology there (see History)".
- **:89:** "A new run ranks every place's attributes (Future places have none) from the baselines silently; each night's news reports a newly DOMINANT attribute only for places on tomorrow's plan ("Science is now DOMINANT in …") (tiers tested: `DominanceTiersTests`, `ScoreRankingTests`; the tomorrow filter is checked in Unity)".
- **:90:** "Dominance tiers carry no gameplay effects (the Phase 7 tier effects were retired); history follows national influence instead (see History)".
- **New "## History" section** before "Content & tooling":
  - "Each night every nation's influence = the attribute deltas stored for its non-Future places (accepted travellers; `AddAttributeScore` effects; nation scores are not influence). The top nation leads when above the floor (GameConfigSO "History", 4) and strictly ahead; a leader stays until one challenger alone beats it by more than the margin (2) or it falls to the keep floor (2); ties change nothing; every change is in the next morning paper (influence, ranking and the leader rule tested: `InfluenceTests`, `ScoreRankingTests`, `NationLeaderTests`; the nightly wiring is checked in Unity)".
  - "The Future is the leader's: from the next day, Future travellers claim its Future place and its facts fill the papers and books (Saleh's example: a Chinese lead makes the Future pay in digital yuan); with no leader nobody travels from the Future (which places are in the world tested: `HistoryTests`; the era draw is checked in Unity). (after piece 4 merges, §8: Future travellers wear their country's Future outfit and a shared neutral Future hair; a dress tell never uses a garment drawn from the same art as the disguise (tested: `LooksTests`, `LookKeysTests`))".
  - "An accepted liar carries their true home's Technology (GameConfigSO carry category) into the claimed place: once a (home, claim) pair reaches the carry threshold (1), that place's Technology is the carried value from the next day, announced in the paper (carry recording and promotion tested: `CarriesTests`; that books, papers, tells and answers follow is checked in Unity)".
  - "Authored history rules (world_source.json `history.rules`, generated as one-shot triggers whose effects carry SetFact) rewrite a place's fact for good when their conditions pass at night, with a news line; eleven rules ship (one per possible leader, three on attribute totals or tiers); SetFact is allowed only in history-rule effects (conditions tested: `GatesTests`; content checks tested: `HistoryChecksTests`; the op rule tested: `EffectOpsTests`)".
  - "The newest latched edit of a (place, category) wins (tested: `HistoryTests`); facts are built in one place from night-latched history only, and place names and origin labels never change (checked in Unity)".
  - "History news: the leader line and carry lines are capped per night (GameConfigSO, 3), the leader's first (tested: `HistoryTests`); rule lines are authored one-shots; news is English text".
  - "While a nation leads, its leader effect broadcasts the UI cue `culture:{nation}` (the present culture, for piece 6; cue grammar tested: `CultureCueTests`); nothing receives it yet".
- **New bullet under "Scoring & consequences":** "Timeline triggers fire once per run at night: Art Renaissance (Art total ≥ 20), Science Boom (Science ≥ 15), Democratic Collapse (Democracy ≤ −6, from day 3) (condition types tested: `GatesTests`)".
- **:95:** add "the eight Future places (after piece 4 merges: their wardrobes share a neutral Future hair), the history rules and lines, one leader effect per country, days 4–6; also owns `Assets/Data/World/History`; generated triggers and effects are merged after the hand-authored ones; rejects fact values over 28 characters, non-ASCII history text, history values that would overflow a transcript line, a dialog effect holding SetFact and (after piece 4 merges) a premade claiming or coming from the Future".
- **:96:** add "the Future (one place per country, leader effects, checks per possible leader, Future days allow every country, no nation+era rule on the Future, after piece 4 merges no Future premade), history values unique (tested: `HistoryChecksTests`), SetFact only in one-shot history-rule triggers, trigger/effect/ending field checks (an epilogue needs a milestone ending)".
- **:98:** "Travel rules: era / nation / nation+era forbidden, shown in briefing + directives; era and nation rules also apply to the day's Future place; a nation+era rule may not name the Future".
- **:101:** "Debug panel (dev tools): … the Timeline Inspector shows the history (leader, ranking, edits, pending carries); Force-leader cheat (kept every night of the session); Skip Day sleeps (day-boundary endings, then the nightly resolve)".

## 4. Out of scope

- **Deferred to a later piece:**
  - honest travellers with risky cargo (Y1): a document field or category and a scoring rule;
  - per-attribute category leaders (H2 option D): the R6 slot is where they would latch;
  - rules that target "the current Future" or the leader (embargoes on the leader, Y5 options B/C);
  - the "embargo evader" motive (piece 2's handoff): weighting true homes toward forbidden places would change piece 2's R11 draw contract; dialog flavour for pieces 3/6;
  - timeline-reactive small talk through `EffectChannel.Chatter` (piece 3 R16): no receiver exists;
  - score- or leader-gated questions and dialogs: supported by the generator now, none authored;
  - **Industrial and Modern premades** (piece 4 hands them over; its C14 rules apply, e.g. an Ada Lovelace for `britain_industrial`): a later content pass for Saleh's review; days 4–6 reuse day 3's pool meanwhile (R22);
  - **premade multi-day arcs** (a premade scheduled by flags or conditions): a later piece; the path is premade `conditions` evaluated by piece 3's `Gates` at day start;
  - premades set in the Future: not possible while a Future place exists only under a leader (R22);
  - a Visuals-channel leader cue for the booth (PR #3's resubmission);
  - archetype retune (the Soldier's −1.5 lowers its nation's influence on a correct accept, §7);
  - serializing the pre-existing unserialized `GameConfigSO` knobs (evidence gate, shift clock, dominance, Home) in `GameConfig_Default.asset`.
- **Piece 6:** the wallet label, UI language, fonts, colours and wallpaper from the present culture; book-row "revised" markers (in `FillFacts`, §2.16); keyed, translatable news (its F2; news stays English in v1, R21); ending and Retirement text that names the Future the player made; per-traveller report lines; the redirect outcome.
- **Never:** renaming places or changing origin labels (H12); editing Culture (R12), so **history-driven dress** (piece 4's handoff) never happens: outfits follow the place, facts follow history; random history (H17).

## 5. Tests (EditMode, `Assets/Tests/EditMode`)

- **`ScoreKeyTests`** (new; the parse cases ported from PR #3's `ScoreKeyTests`, with arbitrary ids instead of the retired made-up ones): ProfileAttr, AdHocAttr (nation, era, attribute), GlobalAttr, Nation parse; `attrTotal:` wins over the `attr:` look-alike; the eight unrecognised shapes (null, "", "garbage", "attr:", "attr:noattribute", "attr:trailing:", "attrTotal:", "nation:") give false; **round trip:** every builder's output parses back to the same kind and parts; `Dominance("p", "a") == "p:a"`.
- **`ScoreRankingTests`** (new): highest first; equal scores keep input order (a three-way tie, and a tie between the first and last entries); negatives rank below zero; null and empty give an empty list; the input list is not modified.
- **`DominanceTiersTests`** (new): `Classify` with 1/2 gives one Dominant and two Supporting aligned with the input order; with 1/1 the third is None; a tie keeps baseline order (the first listed is Dominant); counts 0/0 give all None; counts larger than the list are fine; negative counts count as 0; null gives an empty array.
- **`InfluenceTests`** (new): two attributes of two places of one nation sum; an ad-hoc key counts for its nation and era; Future-era profile and ad-hoc keys are excluded; `nation:`, `attrTotal:` and unparsable keys are ignored (AddNationScore is not a lever); an unknown profile and an unlisted nation are ignored; the output follows `nationIds` order with 0 for nations without keys; baselines never appear (deltas only).
- **`NationLeaderTests`** (new; `[TestCase]` table, floor 4, keep floor 2, margin 2 unless stated):
  - no incumbent: [a 5, b 3] → a; [a 4, b 1] → none (not above the floor); [a 5, b 5] → none (tie at the top); [a 5] → a; [] → none; all negative → none;
  - incumbent a: [b 6, a 5] → a; [b 7, a 5] → a (exactly the margin); [b 7.5, a 5] → b; [a 5, b 5] → a;
  - **tied challengers:** incumbent a, [b 8, c 8, a 5] → a; [b 8, c 7, a 5] → b;
  - **keep floor:** incumbent a between the keep floor and the floor, [a 3, b 1] → a; [b 4.5, a 4] → a (b does not beat a by more than the margin); incumbent a at the keep floor, [b 4.5, a 2] → b; incumbent a at the keep floor with nobody above the floor, [a 2, b 1] → none; a keep floor above the floor counts as the floor (keep 6: [a 5, b 5] with incumbent a → a; unclamped, a would fall to step 3 and the tie would give none);
  - an incumbent id not in the ranking behaves like none;
  - margin 0: [b 5, a 5] → a; [b 5.1, a 5] → b; a negative margin counts as 0.
- **`HistoryTests`** (new):
  - `FutureNation`: null state, blank leader → null; leader "china" → "china";
  - `InWorld`: a non-Future place → true with any future nation (including null); a Future place → true only for the same nation id, false for another, for null and for blank;
  - `Resolve`: no edits → base; one edit → its value; two edits for one key → the newest; an edit for another place or category leaves the value alone; a blank edit value is ignored; a null state gives the base;
  - `Latch`: appends; refuses null, blank value and non-editable categories (Culture, Name, BirthDate, Material);
  - `NewsSlots`: (0, 3, 5) → 3; (1, 3, 5) → 2 (after the leader line); (3, 3, 1) → 0; (0, 3, 1) → 1; (4, 3, 2) → 0; negative inputs count as 0;
  - `IsEditable` for every `ClueCategory` value.
- **`FactTableTests`** (extended, LF): `TryFindOtherPlaceWith`: another place with the same value in different case and with leading/trailing spaces → true and its row; the given place's own row never counts; a value that differs only in internal spacing → false (`ValuesMatch` only trims and ignores case); another category's equal value → false; a blank value → false. `MaxValueLength == 28`.
- **`CultureCueTests`** (new): `Format("china") == "culture:china"`; `TryParse` round trip; "Culture:china" (case), "culture:" (blank id), null and another cue ("booth:x") → false.
- **`DayPlansTests`** (new): [1, 2, 3] day 2 → 1; day 5 → 2 (latest earlier); day 0 → −1; null and empty → −1; [1, 3, 2] day 7 → 1 (the largest below, not the last); [1, 2, 2] day 2 → 1 (first on a repeat).
- **`CarriesTests`** (new):
  - `Make`: an honest pair (from = to) → null; the home without a value today → null; the home's value equal to the claim's → null; a non-editable category → null; otherwise the home's value, category and day;
  - `Promote` threshold 1: two pairs latch in first-record order, their records are removed, `sinceDay` and `EditCause.Carry` are set and `source` is the home's label;
  - threshold 2: a single record stays pending and nothing latches; a second record for the pair latches one edit with the latest record's value;
  - a due pair whose value already equals the target's current value is consumed with no edit;
  - **two due pairs from different homes to the same target with the same value** → one edit (the second pair is consumed against the first's edit);
  - a threshold of 0 counts as 1.
- **`HistoryChecksTests`** (new): one case per problem (blank, 29 characters, non-editable category, unknown place, equal to its own base, equal to another place's base, equal to another edit's value for another place) and a clean set with no problems; two edits for the same place may share a value.
- **`EffectOpsTests`** (piece 3's, extended): `SetFact == 17` (after `NewsLine == 16`); `ActsWhileActive(SetFact)` is false; `HistoryOnly`, one `[TestCase]` per `EffectOpType` value: true for `SetFact` only.
- **`LooksTests`** (piece 4's, extended): **[after piece 4 merges, §8]** `Compose` with an `artNation` item names the part `hair_m_neutral_future_{colour}` and keeps the traveller's colour; `CanLeak` row 6: a home signature item drawing the same art as the claim's item in that slot → false; the same `artNation` in another era → true; one wig and one non-wig → true; the claim with no item in that slot → true.
- **`LookKeysTests`** (piece 4's, extended): **[after piece 4 merges, §8]** `Required` with an `artNation` item uses that token; two places sharing it yield the same hair names; `Required`'s per-place counts are unchanged.
- **`EndingRulesTests`** (new): `KindOf` for the four types and an unknown value; `Select`: Immediate ignores a met milestone and epilogue and picks the highest-priority failure; DayBoundary picks a milestone alone; an epilogue without a met milestone is ignored; an epilogue with a met milestone beats it (priority 50 > 10); a failure beats an epilogue; equal priorities pick the lowest index; nothing met or an empty list → −1.
- **`GatesTests`** (piece 3's, extended): `NationIsLeader` passes for the leader, fails for another nation, with no leader, and with a null key; `GlobalAttrAtLeast`/`AtMost` pass and fail at the threshold; a missing total reads 0 (`AtMost 0` passes, `AtMost -6` fails: the Democratic Collapse trap); a null key fails; the int pins extend piece 3's: `NationIsLeader == 11`, `GlobalAttrAtLeast == 12`, `GlobalAttrAtMost == 13` (the retuned trigger assets store 12 and 13).
- **Unchanged and green:** `SeedsTests`, `LiesTests`, `ForgeryTests` (pins the `TryFindOtherPlaceWith` extraction), `DiscrepancyLogTests`, `WeightedRandomTests` and the rest.

## 6. Verification plan

**Offline, after every change:** `compile_check.py` reports 0 errors in every project, and the reflection runner reports every Domain test passing.

**In the branch's own Unity 6000.4.11f1**, through temporary `_TimeDesk*` scripts (patterns: the piece-2 plan's Task 13 and `SCRATCH/prev__TimeDesk*.cs.txt`; never committed; reports go to the scratchpad):

0. **Baseline, before any piece-5 code** (at `1a3e624`, the branch point): dump seeds 12345 and 999, days 1–3: per slot the claim, name, registered date, role, `claimAllowed`, violator slot, liar, home, tells, answers and every paper value; and the dominance news of one scripted night.
1. **Content:**
   - Generate World twice; the second run changes no file;
   - `History/` holds 11 rule triggers, 11 rule effects and 8 leader effects; the library lists 48 places, 6 day plans, the hand-authored triggers and effects before the generated ones, and `historyLines`;
   - a deliberately broken JSON copy (a duplicate rule value, a 29-character fact, a rule naming an unknown place, a non-ASCII rule value, a dialog choice naming an `Effect_History_*` effect; after piece 4 merges also a premade whose `truePlace` is a Future place) aborts with those errors and writes nothing;
   - Validate Content Library reports no issues (after piece 4 merges, §8: its character-art report lists one neutral Future hair set, `hair_{g}_neutral_future_{colour}`, not one per country).
2. **Empty history changes nothing:** with a fresh `WorldState`, `BuildToday` + `CaseFactory` for days 1–3 reproduce the step-0 dump line for line (facts, liars, tells, answers).
3. **Headless run simulation** (the core check; `-executeMethod`): for 50 seeds, play days 1–15 through the real glue without scenes: `BuildToday`, `GenerateDayCases`, then for each case in queue order (up to the clock's typical 10 a day) accept iff `ShouldAccept` ("perfect play"); a second policy also accepts the first liar of each day. Accepts call `ApplyVerdictImpacts` and `HistoryService.RecordCarry`; each night `EndingService.Evaluate(DayBoundary)` is recorded, then `NightlyResolve` and `day++`. The script proves:
   1. **Determinism:** the same seed and policy give byte-identical `HistoryState` JSON and per-day dumps.
   2. **Leader:** every night's `history.ranking` equals an independent re-sum of the score list; every leader change satisfies R2 against the previous night (a unique challenger beat the incumbent by more than the margin, or the incumbent fell to the keep floor); no change on a tie; the number of leader flips per run is reported.
   3. **Future:** on days 6+, today's places hold exactly one Future place, the leader's, and none without a leader; no traveller claims or comes from the Future without a leader.
   4. **Carries:** each accepted liar whose home Technology differs from the claim's makes one pending record; from the next day `BuildWorldFacts` gives the claim that value, the book row shows it, and an honest traveller of that place prints it; the paper has the carry line while under the cap.
   5. **Rules:** each fired history trigger's edits resolve from the next day, its news line is in the package, its fired flag is set, and it fires once.
   6. **News:** no dominance line names a place outside tomorrow's plan or a Future place; no "is rising" line; at most 3 templated history lines a night, the leader's first.
   7. **Triggers and endings:** Democratic Collapse never fires before the night of day 3; Immediate evaluation never returns Retirement or an epilogue; at the day-15 boundary the result is Retirement or an epilogue.
   8. **Tell pool:** the `NoPossibleLie` warning count per day is reported (expected 0 on days 1–6 with perfect play).
   9. **Report for tuning (R14):** the day the first leader appears, leader identities and change counts, carries and rules fired, trigger nights, and the day-15 attribute totals (mean, standard deviation, 50th/65th/80th percentiles per policy) and ending distribution. **Before the ending assets are committed**, the three epilogue thresholds are set to the 65th percentile of perfect play's day-15 totals (rounded) and the run is repeated to record the resulting ending distribution in the verification record.
4. **Saves:** a `WorldState` with history and phase Home round-trips through `JsonUtility` (the `SaveSystem` payload) unchanged; a version-2 JSON from before piece 5 (no `history`, no `phase`) loads with an empty history and phase Office, and its first `NightlyResolve` adds no dominance line for a Future place.
5. **Force leader across a night** (a fresh run, so China's influence is 0, below the keep floor): set `world.day = 5`, `HistoryService.ForceLeader(world, lib, "china")` → `GetCues(UI)` contains `culture:china` at once; then the Skip Day path (`RunManager.Sleep()`, or in the headless script `EndingService.Evaluate(DayBoundary)` + `NightlyResolve` + `day++`): the log shows the override line, `history.leaderId` is still "china", `history.ranking` was saved, no leaderLost line is in the package, and on day 6 `BuildToday(GetDayPlan(6), history)` holds `china_future`, the Currency rows include "Digital yuan (e-CNY)" and `GetCues(UI)` holds `culture:china`. Then "No leader" (`ForceLeader(world, lib, "")`) clears `DevToolsState.ForcedLeaderId`, removes the cue and the Future place; `DevToolsState.ResetAll()` also clears a set override. Skip Day on day 15 ends the run with Retirement or an epilogue instead of advancing.
6. **EditMode suite** through `TestRunnerApi`: everything passes except the known third-party `UnitySkills.Tests.Core.PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly` (reported, ignored).
7. **Play-mode smoke** on the committed OfficeScene (`[InitializeOnLoad]` + SessionState steps):
   - new run with a fixed seed; day 1; force closing time; the results panel → the save holds `phase: 1`; `RunManager.ResumeRun()` loads **Home**, not the Office; Sleep advances to day 2 (phase back to 0);
   - seed China's influence above the floor, `RunManager.Sleep()`: the briefing shows the leaderGained line;
   - set day 6 with China leading: the Currency book lists "Shanghai Megacity (Future)" with "Digital yuan (e-CNY)", and a Future traveller's passport prints it;
   - day 15 at Home: Sleep ends the run with Retirement (or the epilogue the totals reach), and the Title shows it;
   - no warnings or errors.
8. **Hygiene:** the builder is unchanged, so the scene is not rebuilt; revert Unity-touched unrelated files (`*.csproj`, `ProjectSettings/*`, the TMP fallback atlas, OfficeScene); delete the `_TimeDesk*` files and metas; commit the generated content; append the verification record to this spec.

## 7. Risks

- **Player agency (inference).** Correct verdicts are fully determined, and the day plans choose which nations travel, so the first leader is mostly an emergent function of the queue: by day 3 the Mediterranean nations have about 5 travellers each, China and Britain about 3, Japan and Germany about 1.5, and the day-2/3 guaranteed violators (Egypt Ancient, China Medieval, Japan Early modern) are denied by correct play. The levers are real but costly: accepting a liar who claims a rival (−0.6 and a citation) or denying a rival's honest traveller (a citation). The §6 report gives Saleh the leader distribution; the day plans or the floor may need to favour choice.
- **Epilogues are mostly chance (estimate).** With about 67 accepts over 15 days and per-accept deltas of Art 0.6 (sd 0.8), Science 0.4 (sd 0.8) and Democracy 0.1 (sd about 1.1), perfect play ends near Art 40 ± 6.5, Science 27 ± 6.5 and Democracy 7 ± 9. Correct verdicts are fixed and wrong ones mostly lower these totals, so whether an epilogue is reached depends on the queue more than on the player. Setting thresholds at the 65th percentile (R14) makes each epilogue reachable in about a third of perfect runs; it does not make them earned. A player-driven lever (for example a dialog or upgrade that moves an attribute) is a later design question for Saleh.
- **Leader flicker.** The keep floor (R2) stops one-send flips at the floor; the §6 report counts flips per run, and the keep floor is the knob if they remain frequent.
- **The Soldier lowers its own nation.** Its correct accept is −1.5 Democracy, so an honest Chinese soldier reduces China's influence. This is counterintuitive for "dominance" and may need an archetype retune (deferred).
- **Tell-pool erosion.** Every carry shares a Technology value between two places, so R13 removes that category for both whenever both are in the world; heavy liar-accepting play pushes days toward the `NoPossibleLie` fallback. The validator can prove only authored content. The §6 report counts it; the carry threshold is the knob.
- **Late visibility.** Most leaders appear around days 4–5, so Future facts show from about day 6; a run with no clear leader never opens the Future. That is intended (R1) but may read as "nothing happened"; the leaderGained line is the only announcement.
- **Repetition.** Days 7–15 all use day 6's plan and directives.
- **Anachronism readability.** Carried or authored values that embed years or people ("Aspirin powder (1899)" in Periclean Athens) read oddly. That is the point, but it can confuse; the news line explains each one.
- **Stacking and enum order.** Piece 3 is implemented (its enums end at `UpgradeOwned` = 10 and `NewsLine` = 16, as this spec assumed); piece 4 is built in parallel. The appended enum values (`TriggerConditionType` in `Gates.cs`, `EffectOpType` in `EffectOps.cs`) must follow whatever they appended; inserting a value would silently remap assets, which the `GatesTests`/`EffectOpsTests` pins catch. The plan re-reads `Gates.cs`, `EffectOps.cs`, `EffectSO.cs`, the generator's `ConditionData`, piece 3's ASCII and line-length helpers, and piece 4's `LookData`/`Looks`/`LookKeys` and wardrobe format before anchoring edits.
- **Cross-piece edits.** R18 and R20 edit files pieces 3 and 4 create (`EffectOps.cs`, `Looks.cs`, `LookKeys.cs`, `LookData.cs`, their tests, and their generator checks). If those pieces are implemented with different names, the plan adapts the anchors, keeps the rules in one home, and records the mapping in the plan's first task.
- **Generator ownership.** Merging generated triggers and effects into hand-authored arrays could drop hand-authored entries if the folder filter is wrong; step 1 of §6 checks the order and counts. Renaming a rule id breaks its latched fired flag (it would fire again); rule ids are stable content.
- **Old saves.** A pre-piece-5 end-of-shift save replays once more; a day-4+ save changes plans; both are one-time.
- **Retirement moves.** It now ends the run after day 15 is played instead of after its first verdict, a small behaviour change recorded in FEATURES.
- **Future narrative calls for Saleh:** the year (2150), the Future places' names and facts, the "nobody travels from an unsettled Future" default, the ending thresholds, and days 4–6 reusing day 3's premade pool.
- **The force-leader override is session-only.** After Continue (`ResetAll`) the next night decides from influence again, so a forced nation without influence is dropped then; dev testing across a Continue has to force again.
- **PR #3 conflict.** If Marwan resubmits a separate ranking, the repo gets the two truths the review rejected. The note in H8 and §2.16 must reach him.

## Review notes

The piece-5 analysis's completeness check (`piece5_map.json` `critique.missing`) listed 20 items; each is handled or deferred here:

| Critique item | Where |
|---|---|
| Continue replay loses slot modifiers and double-applies (end-of-shift save before `ResetTomorrowModifiers`/second save) | H11, R11, §1.9, §2.10 |
| SetFact reachable at Home and from dialog effects | Y4, §2.13 (`CheckEffects`) |
| Only three categories visible; Currency printed twice | H12, §1.2 (piece 3's books make Geography/Politics visible) |
| Future places get baselines → first-night flood on old saves | Y3, R3, §1.9, §6 step 4 |
| Travel rules static; a rule naming an unselected Future tests nothing | Y5, §2.13 |
| Endings after every verdict would end runs mid-shift | Y2, R10, §1.8 |
| Democratic Collapse retune fires on night 1 (missing key reads 0) | §2.14 (−6 and DayAtLeast 3), `GatesTests` |
| Validator Future handling per leader | §2.13 (`CheckDayPlanPlaces` per selection) |
| World-model line 97: move the ranking to Domain | Y6, H8, §2.2, §3.2 |
| More spec lines to supersede (world-model 94, 70; identity-lies 108-109) | §3.2 |
| More FEATURES lines (9, 10, 11, 12, 44, 67, 94, 95, 97) | §3.3 (at `efe385d` numbering: 9, 10, 11, 12, 44, 68, 95, 96, 98) |
| No `GetNationById`/`GetProfileById` | R15, §2.7 |
| Influence needs a profile→nation map; ad-hoc keys | §2.3 (`placeOfProfile`, ad-hoc parse) |
| Dev surfaces (inspector, cheats, automation) | §1.10, §2.11, §6 |
| Test gaps (Future choice and carries as Domain; Unity-only glue named; Domain FactEdit shape) | §2.4, §2.18, §5, §6 |
| Legacy era-pick path | R16 |
| Accepted violators | Y7 |
| Piece-3 X5 vs the resume fix | H11, Y11, §3.2 |
| Reachability estimate | §7, §6 step 3.9 |
| `GameConfig_Default` serializes few knobs | §2.14 (history knobs explicit), §4 (the rest) |
| The day-plan fallback changes existing saves | §1.9 |

Claims the critique marked wrong, as corrected here:
- "Only ACCEPT moves the timeline": the legacy path and `AddAttributeScore`/`AddNationScore` effects move scores too (R16, Y9).
- "The validator does not check triggers": it checks null entries and duplicate ids, not fields; §2.13 adds the field checks.
- "Only three night-time writers": `SetFact` would be reachable from any `ActivateEffect`; Y4 adds the validator guard, and H1 now distinguishes night-latched facts from shift-time pending carries.
- "Port ScoreKey/ScoreRanking": only the parser is ported; the stable ranking, aggregation and hysteresis are new code (§2.2–2.3).
- "Ties must resolve deterministically because List.Sort is unstable": an unstable sort is deterministic; the tie rules are now explicit and tested (R2, `ScoreRankingTests`).
- "Only the TodaysProfiles filter changes": the validator, travel rules, era draw and legendary picks are handled (R1, R4, Y5, §2.9, §2.13).
- "Error on every duplicate book value": existing content already shares values; only history-introduced duplicates are errors (R5).
- "AddNationScore is a lever": it is not (Y9).

### Independent review (2026-09-24)

Every finding was checked against the code at `fbf4905` (code equal to `efe385d`), piece 3's committed spec, and the piece-4 and piece-6 drafts before it was applied. F1–F26 follow the review's order; several overlap (F2/F12, F3/F13, F1/F14, F4/F15, F5/F24).

| # | Finding (short) | Verdict | Where |
|---|---|---|---|
| F1 | `SetFact` was appended in `EffectSO.cs`, but piece 3 moves `EffectOpType` to Domain `EffectOps.cs` (`ActsWhileActive`, `EffectOpsTests` pins); GatesTests should pin 11–13 | applied | §2.5 (`SetFact` 17 in `EffectOps.cs`, `ActsWhileActive(SetFact)` false, docs), §2.7 `EffectSO`, §2.1, §2.17, line endings, §5 `EffectOpsTests`/`GatesTests`, §7 |
| F2 | `ForceLeader` does not survive the night: `LatchLeader` drops a forced nation at or below the floor | applied, option (a) | R19, §1.10, §2.8 (`DevToolsState.ForcedLeaderId`, `ForceLeader`, `LatchLeader` step 3), §2.11, §2.16 C4, §6 step 5 |
| F3 | Contradictions with piece 4: Culture is derived (R1 there), R27's art override and `CanLeak` row not delivered, Industrial/Modern premades and arcs not handled, history-driven dress | applied: R27 delivered, not rejected | R18, §2.5 Looks, §2.12, §2.14, §2.16 piece 4, §3.2 piece 4, §4, §5 `LooksTests`/`LookKeysTests` |
| F4 | Second homes: `History.PlaceToken`, `History.MaxValueLength`, a new ASCII check | applied | R20: `Interview.PlaceToken`; `FactTable.MaxValueLength` (piece 4's `Looks.MaxCultureLength` removed); piece 3's R18 helper, `HistoryChecks` has no ASCII rule |
| F5 | Piece-6 contract: the revised marker belongs where base and resolved values meet; "translates the templates before filling" has no hook | applied | §2.7 `FillFacts` doc, §2.16 C7 and C5, R21 |
| F6 | Wrong piece-3 names (`TimelineKeys.FiredFlag`/`DialogDoneFlag`, `ConditionsPass`) | applied | intro, §2.8 `TimelineKeys` |
| F7 | `GetDayPlan` fallback and the news cap sat in untested glue | applied | R17/`DayPlans.Pick`, R8/`History.NewsSlots`, §2.18, §5 |
| F8 | FEATURES queue line is :68, not :67 (verified with `grep -n` at `efe385d`: :67 is the clock) | applied | §3.3, Review notes table above |
| F9 | History values can exceed piece 3's per-category `{value}` bound in the transcript check | applied (first option) | R12, §2.12 "Piece-3 checks extended", §3.2 piece 3 |
| F10 | Skip Day bypasses the day-boundary endings | applied | R19, §2.10 `Sleep` callers, §2.11, §2.17, §3.1, §6 step 5 |
| F11 | FEATURES "origin labels never change (tested: HistoryTests)" untested; `SharedWith` "spacing" case would fail (`ValuesMatch` only trims and ignores case) | applied | §3.3 (claims scoped); `SharedWith` replaced by `FactTable.TryFindOtherPlaceWith`, whose test says "leading/trailing spaces" and adds an internal-spacing false row |
| F12 | = F2, plus §6 step 5 never ran a night | applied | as F2; §6 step 5 now forces, skips a night and checks day 6 |
| F13 | = F3 (R27, Culture in `facts[]`) | applied | as F3 |
| F14 | = F1, plus `EffectOps.HistoryOnly` so piece 3's dialog check rejects a history effect | applied | Y4, §2.5, §2.12, §2.13, §5 |
| F15 | Re-implementations: `SharedWith` loop, `PlaceToken`, fact width, ASCII, cue grammar literal, remove-by-prefix | applied | R20: `FactTable.TryFindOtherPlaceWith` (also used by `Forgery.IsProvableTell`), `CultureCue`, `TimelineService.RemoveEffectsFrom`, and F4's items |
| F16 | Tied challengers could change the leader by library order | applied | R2, §1.1, §2.3 step 2, §5 rows [b 8, c 8, a 5] → a and [b 8, c 7, a 5] → b |
| F17 | No hysteresis at the floor: a first leader flickers | applied (keep floor, not floor − margin) | R2, `GameConfigSO.leaderKeepFloor` (2), §2.3, §2.14, §2.15, §5, §7 |
| F18 | Leader lines promise Future travellers on days 2–5 | applied (reworded, no second line) | R21, §1.1, §2.14 `history.lines` |
| F19 | `Resolve`'s day filter and the threaded `day` parameter are unreachable | applied | R4, §2.4 `Resolve`, §2.7, §2.8, §2.10, §2.13, §2.16; `sinceDay` kept for display |
| F20 | `Carries.Promote` ignores edits latched earlier in the same call | applied | R7, §2.4, §5 `CarriesTests` |
| F21 | Repeatable SetFact triggers, epilogues without a milestone ending, a non-editable `carryCategory` pass silently | applied | §2.13 `CheckTriggers`/`CheckEndings`, §2.7 `GameConfigSO.OnValidate`, R7 |
| F22 | Premades in the Future pass piece 4's static check; Industrial/Modern premades and arcs not deferred | applied | R22, §2.12, §2.13, §4 |
| F23 | FEATURES "(tested: X)" claims broader than the tests | applied | §3.3 History bullets scoped; the Future filter moved to `History.InWorld` (tested) |
| F24 | History lines saved as text lose their ids; structured news or a stated English-only rule | applied as "English text in v1"; structured `NewsItem` storage **rejected** | R21, §2.16. Rejected part: a save field of line ids and arguments would have no reader in v1 (dead code); piece 6's R2 keeps news English and its F2 owns keyed news |
| F25 | Epilogue thresholds are chance-driven | applied | R14 (thresholds from the §6 report, 65th percentile), §2.14, §6 step 3.9, §7 |
| F26 | `TodaysProfiles` filter, `PickEraFromPlan` zero weight and the news-cap order in untested glue | applied for the filter (`History.InWorld`) and the cap (`History.NewsSlots`); `PickEraFromPlan` **kept in glue** | §2.18 gives the reason: one identity test over SO references; the draw is `WeightedRandom` (tested) and §6 step 3.3 proves no Future traveller without a leader |

Also corrected while verifying: the header's piece-2 state (Task 13 is done), piece 3's spec location (committed at `fbf4905`), `DevToolsState.cs` and `DebugPanelController.cs` line endings (CRLF), and `Forgery.cs`, `FactTableTests.cs`, `ForgeryTests.cs` (LF).

The plan's review should append its findings here.

## 8. After piece 4 merges (apply at merge time)

Piece 4 (characters) is built in parallel on its own branch and is not in this branch's code. Everything below needs piece 4's types or content and is applied when the two branches are merged, by whoever merges them; until then piece 5 ships without it and nothing in piece 5 depends on it:

1. **Future wardrobes (H3, §2.14):** each of the eight Future places gets a `wardrobe` block in piece 4's final format, written from `SCRATCH/costumes.json` `futureMotifs` (the outfit is the signature slot; piece 4's generator derives their Culture fact, unique across all 48 places). Their `facts[]` keep the five editable categories only (piece 4 rejects a Culture entry there).
2. **`artNation` (R18, §2.5 Looks, §2.12, §2.15):** `LookItem.artNation`/`ArtNation`, `LookKeys.Required` and `Looks.Compose` use it, `Looks.CanLeak` row 6 (same art as the claim's item → no leak), the generator's `ItemData.artNation` (checked with `LookKeys.IsToken`), the validator's check; the eight Future places' hair and facial-hair items carry `"artNation": "neutral"`; `LooksTests`/`LookKeysTests` rows (§5); the validator's art report lists one neutral Future hair set (§6 step 1).
3. **Culture width (R20):** piece 4's `Looks.MaxCultureLength` is removed; its readers use `FactTable.MaxValueLength` (added here, 28).
4. **Premades and the Future (R22):** generator and validator errors for a premade whose `place` or `truePlace` is in the Future era; days 4–6 get piece 4's `premades` (day 3's pool of seven), `forced: []` and `premadeChance` 0.05.
5. **Appearance channel (§1.7):** days 4–6 copy day 3's channels, so they gain `Appearance` with day 3.
6. **FEATURES wording (§3.3):** the Future outfit/hair sentence and the generator/validator premade and wardrobe clauses.
7. **Piece 4's contract for piece 5 (its R27, §2.20):** delivered by items 1–4; piece 4's `PremadeMetFlag`/`TimelineKeys` reconciliation stays piece 4's.

The piece-6 contract (§2.16: `history.leaderId`, `history.ranking`, the `culture:{id}` UI cue from the leader effect, `BuildWorldFacts` for the wallet, `FillFacts` for revised-row markers) does not depend on piece 4 and ships with piece 5.

## 9. Alignment with the code at `1a3e624` (2026-09-25)

The draft was written against `efe385d` and the drafts of pieces 3 and 4. Re-checked against the implemented pieces 0–3 and 7:
- **Piece 3 as implemented** matches the names this spec uses: `Gates.cs` (`TriggerConditionType` ending at `UpgradeOwned` = 10, `GateCondition`, `GateSnapshot`, `Gates.Passes`/`AllPass`/`UnlockNight`, `FlagKeys`), `EffectOps.cs` (`EffectOpType` ending at `NewsLine` = 16, `EffectOps.ActsWhileActive`), `TimelineService.ToGate`/`ToGates`/`Snapshot` (private), `Interview.Fill`/`Placeholder`/`HoldsToken`/`PlaceToken`/`ValueToken`/`WorstCaseLength`, `LineText`, `ConditionData` (`type`, `key`, `threshold`), the generator's `CheckConditions` rejecting reference condition types "until piece 5", `GameManager.ApplyDialogOutcomes`, `days[].channels`. So `NationIsLeader` = 11, `GlobalAttrAtLeast` = 12, `GlobalAttrAtMost` = 13 and `SetFact` = 17, as §2.5 says.
- **"Piece 3's R18 ASCII helper"** is a local function inside `WorldContentGenerator.CheckInterview`. It is lifted to a private static helper of the generator (one home) that the interview checks and the history checks both call.
- **Piece 3's dialog-effect error** text lives in `ContentLibraryValidator.DialogEffectOpError` (shared by the generator and the validator); the history-only rule (Y4) gets its own shared message beside it.
- **Piece 7 (physical desk)** moved `GameManager` lines (the booth, the traveller view, the READY gate) but not the members this spec touches: `Start` (the fact build and the factory), `HandleDayCompleted` (the end-of-shift save), `HandleDecision` (the accept), the three `EndingService.Evaluate` calls. `OfficeSceneUIBuilder` still reads only `GetDayPlan(1)`, so no builder change and no scene rebuild (A2).
- **The validator** also calls `TodaysProfiles` in `CheckSmallTalk`; it passes a null Future nation there, so it never sees a Future place; the Future era gets its own small talk in world_source.json instead (the Future places have none), so a Future traveller always has something to say.
- **Line endings:** see §2.1 (all CRLF in the worktree).
- **§6 step 7** (play-mode smoke) becomes the scripted play-through the task asks for: a day-4–6 office day with a Future traveller, its papers and books captured in a screenshot; the Continue-resumes-at-Home check runs through `RunManager.ResumeRun` after the end-of-shift save.

