# Resume checkpoint: 20 October 2026

User resumed generation. 521 / 1156 raw sources saved; 635 remain. This supersedes the count in NEXT_WEEK_HANDOVER.md (which preserves the 7 October pause).

24 new sources: Alexandria female outfit and three poses, hair and band; Cairo male/female outfits and three poses each, male hair/beard/sharbush and female braids/scarf; Babylon male wrap and three poses and hair.

Next: hair_m_greece_ancient. Read queue minus manifest with tools/characters/next_retro.py. Continue all remaining wardrobe and 18 body-pose sources.

Fetched origin/main 34e87b1 on resume. Read new contract section 10 and process_retro.py registration requirements; main was not merged into this art branch. Claude's main now has processing/pose switching. Our raw sources have not been validated with it or Unity.

Checks: 496 PNG dimensions/hashes/prompt records verified. New Alexandria head bounds checked against guide (within 2 px), not a full registration test. Cairo male explaining pose was regenerated twice for hand drift; pose-first, costume-second reference order produced the closest match and is now preferred. Do not claim pixel-perfect full alignment.

Review: qina scarf overlaps shoulder/upper arm; test all arm poses for scarf occlusion. Babylon neutral guide includes chest lines; extraction must remove them. Hair/headwear guide clothing lines are discard-only. No old art removed.

Continue using built-in image_gen and save_retro.py. Do not replay or commit pending-job/batch/fixes JSON. Work only in this art worktree; unrelated E:/unity/NOPE checkout remains untouched.

Additional 24 sources saved: Babylon male beard, fillet and cylinder seal; Babylon female outfit and three poses, hair and band; Abbasid Baghdad male outfit and three poses, hair, beard, qalansuwa and taylasan; Abbasid female outfit and three poses, hair, veil and pearl choker.

Review additions: Baghdad female pinned hair crown extends above the veil silhouette; fit/mask or regenerate compact hair before accepting assembled art. Veil behind-arm portions require occlusion checking. Pearl choker bead scale and Babylon cylinder seal size need desk-scale review. These are raw sources, not approved runtime layers.

25 further sources saved: complete 1720 Baghdad male/female wardrobe source sets (14); 1870 Baghdad male outfit and three poses, hair/beard/headcloth (7), female outfit and three poses (4). Female 1720 objecting_b regenerated to restore closed-toe shoes. 496 source dimensions/hashes/prompts audited.

Occlusion review: 1720 female futa overlaps necklace region and thinking hand; assemble in correct order or mask. 1870 male headcloth shoulder edges need pose checks. No runtime alignment claimed. Previous checkpoint 2070048 pushed after explicit user confirmation; current additions remain local until next push.

25 more sources since 4f98fa5: remaining 1870 Baghdad female hair, veil, bangles and three matching wrist-pose variants; complete 1956 Baghdad male/female sets; Athens male outfit and three gestures. Queue expanded by three wrist accessory variants (1156 total). Athens explaining pose regenerated twice to restore feet; final visually checked but registration remains pending. Latest audit verified first 520 source dimensions, hashes and prompts. Baghdad shawl shoulder/hand occlusion and necklace collar layering need assembled checks. Current checkpoint remains local.
