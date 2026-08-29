# Review — PR #3 "feat: office booth reacts to the winning nation, attribute, and pairing"

- **Author:** MarwanXiv · **Branch:** `feat/timeline-ranked-office-layers` (160afde) · **Base:** `main` (current)
- **Reviewed:** 2026-08-29 against `docs/reviews/MERGE_CRITERIA.md`, in two passes: mechanical gates/bars, then an **intent audit** against the pre-existing timeline systems.
- **Verdict:** ❌ **REJECT — revise & resubmit.** All six gates pass and the craft is high (initial pass scored it accept-with-conditions), but the intent audit found the feature **forks the timeline architecture instead of extending it**: it builds a second ranking system and a second sprite-reaction mechanism parallel to ones that already exist. Under the manifesto's reuse law ("if two systems half-duplicate each other, one of them is a bug that hasn't happened yet") Modularity/Reuse drops to 2 → rejection per the criteria. The feature itself is wanted; the resubmission path is concrete and mostly *deletes* code.

## Gate results (all PASS — kept for the record)

G1 builds/boots (static) ✓ · G2 tests are genuine decision tables ✓ · G3 feature-complete ✓ · G4 FEATURES.md updated (one overclaim nit) ✓ · G5 clean merge ✓ · G6 metas/LFS/no settings churn ✓. Scene churn is legit builder regeneration (267→269 GameObjects, all key objects survive).

## Intent audit — why this is rejected anyway

**What already existed on main (d3a9a49), built by Saleh:**

1. **A nightly ranking system.** `TimelineService.RecomputeDominance` already sorts scores and classifies dominant/supporting tiers, sized by `GameConfigSO.dominantPerProfile`/`supportingPerProfile`, persisted in `world.timeline.dominantKeys`/`supportingKeys`, with news lines on tier changes.
2. **A cue pipeline designed exactly for this.** `EffectSO` Cue ops → `TimelineEffects.GetCues` → `TimelineCueReceiver` (channel `Visuals`) → `TimelineReactiveSprite` (inspector `cueId → Sprite`). Dominance is already wired to effect activation (`RebuildTierEffects` activates each baseline's `dominantEffect`/`supportingEffect` while the tier holds), and **30 authored `Effect_Dom_*`/`Effect_Sup_*` assets already exist** — they just have no Cue ops authored yet.
3. **A no-code path for the spirit of this feature:** "office reacts to the dominant culture" = edit the 30 existing effect assets (channel `Visuals`, add a Cue op) + map cues on the existing booth `TimelineReactiveSprite`. Pure inspector work. The commit message's premise ("nothing in the office showed them") described a *content* gap, not a *code* gap.

**The genuine gap** (credited): nothing computed a cross-entity argmax — top *nation*, top *global attribute*, top *pairing* — and `TimelineTriggerSO` only expresses absolute thresholds, not "currently leads all others". The literal spec'd behaviour did require code. But the right-sized fix was **~30 lines inside `NightlyResolve`**, next to `RecomputeDominance` where sorting already happens, emitting the winners as cues — after which the *unmodified* `TimelineReactiveSprite` drives all three layers.

**What the PR built instead:**

- `TimelineRankedSprite` — a structural near-clone of `TimelineReactiveSprite` (same target/defaultSprite/mapping-list/first-match/Reset/Start→Refresh shape) that bypasses the `TimelineCueReceiver` channel architecture the office design doc locks in. Its doc-comment justification ("no content authoring required") is oversold: id→sprite mappings still have to be authored per layer — the same burden as cue→sprite mappings.
- `ScoreRanking` — a **second, unreconciled notion of "winning"** alongside `RecomputeDominance`, and they can visibly disagree:
  - it ranks raw deltas only, ignoring authored baselines (a high-baseline attribute shows `[dominant]` in the debug panel yet is invisible to the ranked layers until its first delta);
  - it applies `rankedLayerMinScore`; dominance has no floor (a −1 attribute can be `[dominant]`);
  - it reads live scores at scene `Start`; dominance snapshots nightly — mid-day the wall and the `dominantKeys` diverge.
  Nothing reconciles or even documents this divergence.
- Scope creep: `TimelineScoreDisplay` rewrote the debug-panel score rendering (an improvement, but out of scope, and its FEATURES line overclaims test coverage — only key parsing is tested).

## What survives into the resubmission (genuinely good work)

- `ScoreKey` + its 12-case parse test suite (a tested twin of `TimelineKeys`).
- The min-score / "no clear leader" neutral-state semantics and its decision-table tests.
- The generated-art pipeline (`RankedArtNaming`, hash-pinned tests, LFS-clean PNGs, drop-in replacement path).
- The `rankedLayerMinScore` knob with its documented rationale.

## Required rework to resubmit

1. **Move the ranking into the existing system:** compute top nation / top global attribute / top pairing inside `NightlyResolve` (or a helper it calls), **next to and reconciled with `RecomputeDominance`** — one written decision on baselines (count them or don't, for both systems), one on the floor, one on timing (nightly snapshot, matching dominance). Keep `ScoreKey`/`ScoreRanking` as the pure Domain helpers for it; delete nothing from the tests that still applies.
2. **Broadcast winners as cues** (e.g. `top_nation:{id}`, `top_attr:{id}`, `top_pair:{nation}:{attr}`) through the existing `TimelineEffects` cue path (synthesized tier-style effect or direct cue injection — designer-visible either way).
3. **Delete `TimelineRankedSprite`; drive the three booth layers with the existing `TimelineReactiveSprite`** mapped on the new cue ids. If a capability is missing there (e.g. default-sprite fallback), extend that one class — both the booth poster and the ranked layers then share one mechanism.
4. **Split the debug-panel display rewrite** (`TimelineScoreDisplay` + `DebugPanelController` change) into its own small PR, with either a test for `FormatGrouped` or an honest FEATURES line.
5. Keep: generated art, min-score knob, FEATURES entries (rewritten to match the cue-based mechanism).

This lands the same player-visible feature with one ranking truth, one
reaction mechanism, and less code than the current branch carries.
