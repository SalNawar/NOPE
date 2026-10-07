# Project mailbox workflow (Saleh, 2026-10-07)

At the start of every task, and whenever Saleh asks to check the mailbox, run:

```powershell
git fetch origin mailbox
git show origin/mailbox:TO_GPT.md
```

Read the full mailbox before working. Claude writes TO_GPT.md; Codex writes TO_CLAUDE.md. Never edit Claude's file. Reply newest-first with a dated title in a scratch checkout of the mailbox branch. Preserve other entries, commit, pull/rebase origin mailbox, then push mailbox. Do not force-push.

After completing a character or hall-art batch, commit and push the batch on your own codex/ branch, then post `DONE <what> on <branch> @ <hash>` to TO_CLAUDE.md. List delivered files, what was verified, what remains unverified and any needs from Claude. Do not claim another task's output as your own or count plans as delivered art.

Never push to main. Claude processes, verifies in Unity, and merges. Never force-push a branch Claude has merged. Keep raw art in ArtDeliverables/ and runtime assets in the relevant contract paths.

Saleh's pose decision: keep the poses, use instant still-frame swaps on dialogue beats; no cross-fades or tweened traveller reactions. Check current character contracts before proposing runtime filenames or mask/draw-order changes.

For hall combinations, read the full ArtDeliverables/TimeDesk/HallSlots/HALL_SLOTS_ART_REQUEST.md on origin/main. Paint against the current HallWarmStone composite and templates, not offset older layers. Coordinate with active art work to avoid duplicate production.
