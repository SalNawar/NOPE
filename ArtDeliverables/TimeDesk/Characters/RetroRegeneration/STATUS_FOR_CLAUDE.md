# Character art status — 25 October 2026

## Current delivery
1165 of 1171 planned raw source images are saved (99.49% of source generation, NOT runtime completion). Built-in image_gen was used. Each source has its prompt and manifest hash. All 1165 passed dimension (1024 x 1536), hash, uniqueness and prompt-file checks.

The 33 famous travellers have their eight planned sources each. Ordinary wardrobe sets, neutral bodies and gesture sources are present except the six items below. No old runtime art has been deleted. Alignment, chroma extraction, assembled occlusion and Unity integration are not verified.

## Six outstanding source items
| Asset | Reason |
|---|---|
| accessory_f_britain_earlymodern | Tool moderation block; no accepted output. Do not bypass the block. |
| outfit_f_britain_modern__thinking_a | Tool moderation block; no output. Do not bypass the block. |
| base_m_skin1_facea__thinking_a | Tool output moderation block on 25 October (sexual category); no output. Do not bypass the block. |
| outfit_m_germany_industrial3__explaining_a | Repeated edits introduce decorative swirls/brocade into plain wool. Not saved as accepted source. |
| outfit_m_germany_industrial3__thinking_b | Same fabric drift; cuff also extends too low. Not saved as accepted source. |
| outfit_m_germany_industrial3__objecting_c | Same fabric drift; extended hand about25px right of guide. Not saved as accepted source. |

## Verification still required
- Run the main-branch processing/registration contract against the raw set. Claude's runtime pose switching lives on main; it has not been tested with these new sources.
- Treat the per-source review field “registration pending” literally. A successful file audit is not evidence of visual alignment or pose-switch compatibility.
- Check hand-over-face overlays, shoulder/hair/headwear occlusion, necklaces under hair and arms, accessory pose variants, skin recolours and passport crops.
- Apply contract rejection thresholds rather than stretching or individually nudging layers to fit.
- Review the detailed accumulated issues in RESUME_CHECKPOINT.md. New examples: Germany future female soles about10px low and objecting_c extended hand about14px left; female body explaining_b right hand about15px wide/right; male objecting_a fingertips about7px high. Skin/body pigment texture also needs checking at desk scale.
- Do not remove old art until the replacement families are processed and visually verified in game.
- Continue the three Cologne quality holds with a coordinated plain-wool costume set; do not count the generated patterned drafts as complete.

## Worktree and branch
Work only in C:/Users/Saleh/.codex/worktrees/famous-travellers/NOPE on codex/textured-travellers-all. The E:/unity/NOPE checkout contains unrelated work.
The user has authorized pushing this art work to origin. Local pending-job/batch/fixes JSON are scratch files and must not be committed.
