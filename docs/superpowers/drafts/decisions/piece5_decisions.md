# Piece 5 — History changes facts: decisions (made by Claude under Saleh's "go with all pieces dont stop", 2026-09-24)

Source analysis: SCRATCH/piece5_map.json (`synthesis.decisions` in order = H1..H20; `critique.extraDecisions` = Y1..Y11; handle or explicitly defer every `critique.missing` item). Stacked on pieces 2-4 (branch feat/history-facts); use their FINAL implemented names (read the code; the code wins over their specs).

Saleh: "changing the timeline should change info too for example if china becomes too dominant in the past maybe the currency for the future becomes chinese. an invention can change era and country based on your choices".

- H1 = A. One persisted `WorldState.history` block (Future leader + latched fact-edit list), written only at night by: the leader step, authored rules fired as triggers, and liar carries. Facts are resolved from night-latched state only (never live scores), so an end-of-shift save + Continue replay sees the same facts.
- H2 = B (+ D later). "Too dominant" = past influence: a nation's summed stored attribute deltas over its non-Future places, ranked across the 8 nations; a leader needs a score above a floor knob. Per-attribute category leaders (D) are a documented later extension.
- H3 = C. The Future: one Future place per day = the leader's `{country}_future` profile. Author 8 Future places in world_source.json (each with 5 facts, names, year, birth years; plus outfit motif data if piece 4 defined a slot for it). Before any leader exists, a neutral default (define it: e.g. no Future travellers, or a default present) — decide and document.
- H4 = C. The Future leader is reversible with hysteresis (changes only when a challenger leads by a margin knob; incumbent keeps ties — Y8), and every change is announced. Authored rules and carries latch permanently.
- H5 = A. Carry: an accepted LIAR brings the TRUE home's Technology into the CLAIMED place (recorded at accept; per (true, claimed) pair count reaching a threshold knob, default 1, latches a fact edit at night). Plus authored invention rules (H6). Violators (honest) never carry (Y7). Honest-papers "risky cargo" is deferred (Y1), named in the spec.
- H6 = A. Authored history rules live in a "history" section of world_source.json; the generator emits TimelineTriggerSO (conditions, newsLineOnFire, oneShot) + EffectSO with a new instant op SetFact (EffectOpType appended). SetFact may appear only in history effects for now (Y4 = A). Use piece 3's Domain condition evaluator; append condition types (e.g. NationIsLeader, GlobalAttrAtLeast/AtMost) — enums append-only.
- H7 = A. Author days 4-6 in world_source days[] (day 4 adds Industrial, day 5 Modern, day 6 Future) with tell/rule/channel knobs; days after the last authored plan reuse the last plan (replace the inspector-fallback replay).
- H8: The cross-nation ranking is implemented ONCE in Domain (a tested ScoreRanking helper; PR #3's ScoreKey/ScoreRanking design may be followed and credited, but PR #3 is not merged, so write it here). RecomputeDominance's per-place tier ranking uses the same helper (Y6). Note for Saleh: tell Marwan that PR #3's resubmission should build on this helper.
- H9 = A. Evidence currency changes in piece 5 (the Future place's Currency fact on papers, books, tells); the wallet label changes in piece 6 from the same resolved leader.
- H10 = A. The validator simulates day plans under authored rule combinations to keep piece 2's book-value uniqueness (R13) true; runtime also guards (skip + warn) as a second line.
- H11 = A. Fix the Continue replay: an additive WorldState phase field so Continue after the end-of-shift save resumes at Home (not replaying the office day). Coordinate with whatever piece 3 did for dialog effects (don't double-fix).
- H12 = A. History may change any of the 5 categories; origin labels never change.
- H13 = A. Conflicts on the same (place, category): newest latched edit wins; same-night ties by order leader → triggers (library order) → carries (record order).
- H14 = A. Announce each change as a news line in the tomorrow package (authored newsLineOnFire; templated lines for leader changes and carries), capped per night by a knob. Book-row "changed" markers → piece 6.
- H15 = B. Dominance news: announce only flips in places on tomorrow's plan; drop the unreachable "rising" branch (or set supportingPerProfile to 1).
- H16 = A. Append GlobalAttrAtLeast/AtMost condition types; retune the 3 dead triggers and 3 endings to reachable thresholds; endings fire at the day boundary (Y2 = B), not mid-shift.
- H17 = A. History resolution is fully deterministic (no random draws).
- H18: One seam — BuildFactTable resolves history before Add; papers, books, tells, answers and (piece 4) outfits all follow.
- H19: Impacts keep landing on the CLAIMED place, liar or not.
- H20: Domain rules with decision-table tests; knobs in SOs (GameConfigSO/DayPlanSO); content in world_source.json via the generator; save stays version 2 with an additive history block (old v2 saves load with an empty history).
- Y3 = B: Future places do not join per-place dominance tiers/news (one ranking, no flood).
- Y5 = A: travel rules keep working on the selected Future place like any place.
- Y9 = A: influence counts only traveller impacts (state that effect ops like AddNationScore are not a lever).
- Y10 = B: verify with a dev/automation path that forces multi-day history (e.g. seeding scores) + Domain tests; automation stays temporary.
- Piece 6 contract: expose the resolved present culture = the Future leader country id (plus the ordered ranking) in the saved history block, emitted to the UI via the existing cue pipeline (TimelineCueReceiver, EffectChannel.UI) — piece 6 consumes it. The debug panel gets a "force leader" cheat for testing.

## AMENDMENT A1 (Claude, 2026-09-24)
Pieces 5+ are built after the art sync (SCRATCH/artsync_decisions.md): the office is Codex's hybrid 3D scene and the builder is existing-wins. Piece 5 has no booth changes, but any builder change must follow existing-wins.
