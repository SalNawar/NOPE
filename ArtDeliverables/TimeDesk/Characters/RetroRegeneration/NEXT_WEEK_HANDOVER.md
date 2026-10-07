# Resume character remake next week
Saved 7 October 2026. User asked to push current work and leave the remaining work for next week. Stop generation after this handover; resume when requested.

## Branch and workspace
- Branch: codex/textured-travellers-all
- Workspace: C:/Users/Saleh/.codex/worktrees/famous-travellers/NOPE
- Base: origin/main 92481b2, fetched 7 October.
- Do not work in the unrelated dirty checkout E:/unity/NOPE.
- Latest pre-handover checkpoint: 3ef5c99. This handover and all 423 sources are committed after it.
- Fetch/check main on resumption; preserve this branch and its existing art.

## Verified inventory
423 / 1153 planned raw source images saved (36.7%); 730 remain.
- 40 body/head base sources.
- 20 pose mannequins (female objecting_c missing earlier, now generated).
- 91 historical wardrobe sources.
- 8 civilian passport wardrobe/hair sources.
- All 33 famous travellers have 8 sources each: neutral/happy/angry/worried, three varied gesture poses, one civilian passport photo (264 sources).
These are RAW SOURCES, NOT completed runtime assets. No new Unity integration, no old runtime art removed. File audit checks PNG dimensions, hashes, unique names and prompt records; it does not check registration or game rendering.

## Next generation work
1. 712 remaining historical wardrobe sources (costumes, their poses, hair, headwear, facial hair, accessories).
2. 18 body-pose sources.
Start with outfit_f_egypt_ancient3: Ptolemaic Alexandria, chiton and mantle. Then its explaining_b / thinking_c / objecting_a poses, melon-rib bun and diadem band; then Ayyubid Egypt.
Use tools/characters/next_retro.py --family wardrobe --limit 2 to find the next exact jobs.
queue.json retains original pending labels: subtract manifest.json names to establish actual progress. REMAINING_NEXT_WEEK.json is the snapshot of all 730 unsaved jobs.
Do not blindly rerun plan_retro.py: it overwrites repaired descriptions and passport additions.
Added era entries sometimes have empty details. Their authoritative wardrobe labels and moment are in Assets/Data/World/world_source.json, places[].wardrobe; consult this before inventing details.

## Art requirements
- Textured late-1980s/early-1990s anime: hand ink, restrained cel shadows, visible subtle pigment/fabric texture. Avoid glossy vector finish.
- Ordinary people, varied age/build/facial structure; not everyone beautiful or model-shaped.
- 1024 x 1536 full canvas, flat green background. Modular garments drawn on magenta fitting figures.
- Historical costume accuracy. Rulers without crowns/sceptres, soldiers unarmed, photography-era likeness stylized rather than photo copies.
- User later approved roster modern/future clothing; no invented contemporary outfits outside the approved briefs. Passport civilian 2150 addendum is explicit.
- Pose families: neutral + explaining/thinking/objecting, each with a/b/c variations across the cast. Current plan assigns each outfit and unique one variant of each gesture category. No detached animated hands.
- Keep costume identity and texture consistent across poses. Use neutral costume as first reference, pose mannequin as second; second controls arms only.
- Hair front/back, heads, clothing and accessories need appropriate layer order. Hands crossing face require foreground occlusion layers.
- Claude handles runtime pose switching and processing/recolours. Supply reliable sources and honest verification status.

## Remaining processing / verification
- Register sources to character contract landmarks. Many sources drift; do NOT claim drop-in compatibility.
- Extract transparent layers / remove green and magenta guide regions and their dark guide lines.
- Recolour skin/hair as required by Claude's contract.
- Review accessories attached to moving wrists/waist/torso for pose compatibility.
- Test composites with hair, necklaces, headwear, sleeves and hands: no overlap mistakes.
- Check every expression and pose at actual desk scale, varied ordinary faces/builds and texture consistency.
- Integrate only after validation with Claude's switching implementation; verify Unity.
- Replace/remove old art only after its replacement is validated. Prior user authorized removal, but none has happened yet.

## Known review issues
- Some young base faces a/b are too similar; ages c/d more varied.
- Some figures remain slimmer/idealized despite the brief; verify diversity before claiming complete.
- Guide face/clothes/anatomical lines must not survive extraction.
- Passport high collars were revised, but exact chin clearance and crop need verification.
- Some thinking_b frames are mirrored whole-character pose variations; inspect metadata/runtime mapping.
- Caesar neutral includes laurel: recheck against no-crowns rule.
- Hokusai's three poses were corrected to retain ink-stained fingers.
- Ramesside blue cap was corrected to remove an unintended hanging tail.
- REVIEW_NOTES.md includes older review notes; a prior appended section may contain literal backslash-n text and should be cleaned when editing.
- Runtime shadows/hall/city work is outside this character branch's current generation task; do not claim those fixed here.

## Files and safe workflow
- README.md: direction and scope.
- GPT_NOTES_character_remake.md: copied Claude addendum.
- manifest.json / CHECKLIST.md: saved inventory.
- queue.json / pose-families.json / unique-roster.json: production plan.
- Raw/*.png and matching *.prompt.md: sources.
- Production/FAMOUS_PREMADES_REQUEST.md and docs/CHARACTER_ART_CONTRACT.md: authoritative roster and technical contract.
- tools/characters/save_retro.py and save_retro_batch.py: save PNG, prompt, hash, manifest and checklist; refuse accidental overwrite.
- tools/characters/audit_retro.py: file-level verification.
- pending-*.json files are temporary and can be stale. Never blindly replay them or commit them.
- Use built-in image generation, visually inspect each result, then save. Do not report generated-but-unsaved images as saved.
- Push checkpoints after audit, and keep user progress concise. Saved raw count is separate from runtime completion.
- No active automation or subagent is continuing work.

