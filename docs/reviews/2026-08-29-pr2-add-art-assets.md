# Review — PR #2 "Add placeholder art assets"

- **Author:** MarwanXiv · **Branch:** `add-art-assets` (7d16861 → 9858274) · **Base:** `main`
- **Reviewed:** 2026-08-29 against `docs/reviews/MERGE_CRITERIA.md`
- **Verdict:** ❌ **REJECT** (gates G1, G5, G6 fail; Code Quality bar = 1)

## What the PR does

Adds 71 placeholder PNGs, all correctly stored as git-LFS pointers: document
kits for the 6 nations (`Assets/Art/Documents/Nations/<Nation>_{border,seal,stock}.png`),
3 stamps, 8 booth props (`Assets/Art/Props/`), and 7-layer visitor sprite
stacks for all 6 nations (`Assets/Art/Visitors/<Nation>/`). Nation names match
the game's content generator. The second commit also carries substantial
non-art churn (see G6).

## Gates

| Gate | Result | Evidence |
|---|---|---|
| G1 Builds & boots | **FAIL** | All 71 new PNGs are missing their `.meta` files (only 5 folder metas committed). Unity will mint fresh GUIDs on first open → 71 files of untracked churn and unstable sprite references. `NOPE.sln` deleted while main still maintains it. |
| G2 Tests pass | N/A | No script/rule changes; test suite untouched. |
| G3 Feature complete vs spec | **FAIL** | The approved swappable-sprite convention (spec `docs/superpowers/specs/2026-06-21-office-scene-two-states-design.md`) swaps art **in place** under `Assets/Art/Office/Placeholder/` (same filenames/GUIDs). The PR creates parallel folders instead — `Art/Props/03_calendar.png`, `02_credits_till.png`, `01_stability_gauge.png` duplicate main's existing `Office/Placeholder/{calendar,till,stabilitymonitor}.png` — and nothing in code or scenes references the new folders. The art is delivered but wired to nothing. |
| G4 FEATURES.md | N/A | No gameplay behaviour changed. (But see G6: a project setting was flipped silently.) |
| G5 Mergeable & current | **FAIL** | Merge-base e886dc5 (2026-06-21) is 10 commits behind main. `git merge-tree` conflicts: `Assembly-CSharp-Editor.csproj` (content), `NOPE.sln` (modify/delete). |
| G6 Asset hygiene | **FAIL** | Committed IDE junk (`.vs/**` incl. binary `.suo`/`.vsidx`, `.vsconfig`); regenerated csprojs; `NOPE.sln`→`NOPE.slnx`; wholesale CRLF→LF rewrites of `ProjectSettings/GraphicsSettings.asset` (zero real change under `git diff -w`) and `DefaultVolumeProfile.asset` (1,593-line diff, 3 real lines); **silent semantic regressions**: `UnityConnectSettings.m_Enabled: 0 → 1` (re-enables telemetry main deliberately disables), URP shader-prefiltering flags flipped, ProjectSettings preloadedAssets/static-batching changes — none explained. Stray throwaway `_sprite_import/Stamps.ps1`. ✅ LFS itself is correct: all sampled PNGs are valid LFS pointers. |

## Quality bars

- Configurability / Modularity: N/A (no code)
- Scalability: **3** — the per-nation layered naming (`_01_backdrop`…`_06_acc` + `_COMPOSITE`) is sensible and matches the six nations, but it is unregistered and duplicates existing props instead of extending the established convention.
- Optimization (diff proportionality): **2** — the art is ~230 pointer lines; the other ~3,500 diff lines are line-ending and IDE churn.
- Code quality: **1** — commit 9858274 is titled "Add placeholder art assets…" yet contains **zero PNGs**; it is exactly the commit carrying all the junk. `Stamps.ps1` has a hardcoded `C:\Users\m_bos\Downloads\...` path and writes `FromBase64String("PLACEHOLDER")` — it can never have produced the committed art.

## Intent audit — does it redo/override existing work?

Both failure patterns the intent lens looks for are present:

- **Overrides deliberate decisions without necessity or explanation:** re-enables Unity Connect telemetry that main deliberately disables, flips URP shader-prefiltering flags, alters ProjectSettings, and deletes `NOPE.sln` — none of it needed for art, none of it declared. This is overriding the owner's configuration as a side effect of a stale environment, not an argued improvement.
- **Redoes what the existing design already does without code:** the swappable-sprite convention was *designed* so final art lands with zero code/scene changes — replace the placeholder file, keep the GUID. Instead the PR creates parallel `Art/Props|Visitors|Documents` folders that duplicate three existing `Office/Placeholder` sprites and are referenced by nothing, which means a *future* integration task the convention was built to avoid.

## Required fixes to resubmit

1. **Rebase onto current `main` (d3a9a49).** Drop the branch-side csproj/sln changes entirely (take main's versions). → G5
2. **Remove `.vs/**`, `.vsconfig`, `NOPE.slnx`** from the PR. → G6
3. **Revert `ProjectSettings/UnityConnectSettings.asset`** — `m_Enabled` must stay `0`. → G6
4. **Revert `GraphicsSettings.asset`** (pure line-ending churn) and revert `ProjectSettings.asset`, `UniversalRP.asset`, `DefaultVolumeProfile.asset`, `UniversalRenderPipelineGlobalSettings.asset` unless each change is intentional and explained in the PR body. → G6
5. **Commit a `.png.meta` for every sprite** (correct Sprite import settings), in the same commit as the art. Open the project once after cleanup to generate them. → G1 + G6
6. **Delete `_sprite_import/Stamps.ps1`**; if a generator is wanted, write an idempotent editor tool under `Assets/Editor/` with repo-relative paths. → G6
7. **Resolve the prop duplication:** either swap the new art into the existing `Assets/Art/Office/Placeholder/{calendar,till,stabilitymonitor}.png` files (preserving GUIDs per the swappable-sprite convention), or land an approved spec for the new `Art/Props|Visitors|Documents` layout **plus the code that consumes it**. → G3
8. **Split into honest, focused commits** (e.g. "art: nation document kits + stamps (LFS, with metas)" / "art: visitor layer stacks"). → Code quality

The art content itself is good and wanted — this is a hygiene/integration
rejection, not a content rejection. A cleaned-up resubmission is expected to
pass quickly.

*Follow-up noted for main: `.gitignore` covers `.vscode/` but not `.vs/` — add it to prevent recurrence.*
