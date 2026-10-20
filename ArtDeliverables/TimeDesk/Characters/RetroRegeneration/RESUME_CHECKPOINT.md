# Resume checkpoint: 20 October 2026

User resumed generation. 447 / 1153 raw sources saved; 706 remain. This supersedes the count in NEXT_WEEK_HANDOVER.md (which preserves the 7 October pause).

24 new sources: Alexandria female outfit and three poses, hair and band; Cairo male/female outfits and three poses each, male hair/beard/sharbush and female braids/scarf; Babylon male wrap and three poses and hair.

Next: facialhair_m_iraq_ancient. Read queue minus manifest with tools/characters/next_retro.py. Continue all remaining wardrobe and 18 body-pose sources.

Fetched origin/main 34e87b1 on resume. Read new contract section 10 and process_retro.py registration requirements; main was not merged into this art branch. Claude's main now has processing/pose switching. Our raw sources have not been validated with it or Unity.

Checks: 447 PNG dimensions/hashes/prompt records verified. New Alexandria head bounds checked against guide (within 2 px), not a full registration test. Cairo male explaining pose was regenerated twice for hand drift; pose-first, costume-second reference order produced the closest match and is now preferred. Do not claim pixel-perfect full alignment.

Review: qina scarf overlaps shoulder/upper arm; test all arm poses for scarf occlusion. Babylon neutral guide includes chest lines; extraction must remove them. Hair/headwear guide clothing lines are discard-only. No old art removed.

Continue using built-in image_gen and save_retro.py. Do not replay or commit pending-job/batch/fixes JSON. Work only in this art worktree; unrelated E:/unity/NOPE checkout remains untouched.
