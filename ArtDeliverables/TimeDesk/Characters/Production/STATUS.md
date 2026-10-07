# Character art production status — 2026-10-04

Current branch: `codex/hall-art-completion`, based on latest main `5c7c281` with Claude's pilot and scene integration. Production is complete: 470 source deliverables (455 new, 15 pilot) and all 944 current required game keys are installed, including 26 named characters with four expressions each. Native Unity import and runtime Resources validation pass for all 944 sprites with zero errors.

The processing stage now includes green/magenta extraction, registered transparent layers, skin/hair palettes and variants, rear hair, expression alignment, full accessories and visually reviewed game stacks. The 42 approved pilot keys and metadata are retained. Eight interim recoloured heads were replaced with generated faces, with originals archived and GUIDs retained. All raw sources remain unchanged. See `../Completion/README.md`, validation reports and `ProcessedQA` for evidence and `tools/characters/README.md` for the maintained pipeline. The two neutral future outfit raw sheets are reference-only in coverage.

## Historical pilot status — 2026-09-26

Branch: art. Production follows the v2.2 brief snapshot. Coverage requires 406 raw images producing 880 processed game keys; these counts are different.

The user explicitly resumed character generation after the office colour/texture pass and teleporter clearance. The pilot now contains **15 selected raw source sheets**: two base figures, all eight Athens pieces, and the five required layering stress cases. Twelve selected sources were added on 2026-09-26.

Files: ../Raw/batch01-pilot/. Exact prompts, generation_manifest_2026-09-26.json, raw_file_check.json and REVIEW_2026-09-26.md are saved alongside them. Rejected candidates remain under revisions.

All 15 selected PNGs are 1024×1536 and opaque. This checks file format, not alignment or game readiness. Female hair/headwear sources use head/shoulder registration forms on the full canvas and need head-landmark alignment rather than full-body registration. See the review for additional processing notes.

**No new game-ready character layers are installed.** Next: register and cut out this pilot, check hair/hat/beard/hood combinations and the dark-green mask case, bake required variants, then validate the stack at the desk and in passport crops before producing the remaining places. The style card is still pending a validated pilot. No message was sent to Claude.

Processing is Claude's (Saleh, 2026-09-26): the art side delivers and reviews the raw sheets; Claude registers, cuts out, bakes the variants, keys, imports and validates them. See the rules in ../../ImportedOffice/CURRENT_STATE.md.
