# Desk notes and worn surfaces — applied 2026-09-26

This correction applies the five desk notes that were mistakenly left in the concept preview, plus the missing worn surfaces. It supersedes the earlier material-pass-only scope.

## Applied in OfficeScene

1. Telephone moved to the rear right, with its coiled handset cord retained and clearance from the form sorter and spare paper.
2. NEXT moved beside the back edge of the office mat: a 6.55 cm gap.
3. Banker's lamp moved behind NEXT, offset slightly right so the label remains readable.
4. Calculator turned toward the player's seat and shifted slightly back to keep it visible above the gameplay scanner.
5. Mouse wire renderer disabled; the mouse and pad stay in place.

Moved the existing prop roots, so the runtime scene-contract anchors resolve their new positions. No gameplay scripts or component data changed.

## Worn materials

Eleven renderers now use persistent texture/material assignments: repaired/chipped plaster on the booth partitions and hall structure; aged cork on both noticeboards; restrained handling scuffs on the plain burgundy blotter, phone, till and lamp. The PC, original floor and smooth desk wood remain protected. Glass, crowd scale, flag sprites and the clear teleporter approach are retained.

The three textures were generated with the built-in image tool. Exact prompts and sources are adjacent. Selected broad surfaces use new mesh copies with planar UVs so the texture reads on the actual surface; original FBX geometry and UVs are untouched. No photo grain or new procedural grain was added.

![Actual Unity Game view](runtime.png)

## Verified

validation.json records all five notes satisfied, 11 textured renderers, unchanged PC/floor and gameplay data, 19 crowd groups, zero portal overlap, and no tested prop intersections. The result was inspected in Play mode with no Unity console errors. Capture overlays were restored and the editor returned to Edit mode.

Authoring menus: Tools > Office Art > Debt Relief > Apply Desk Notes And Wear / Validate Desk Notes And Wear. The palette-application menu preserves this approved follow-up once its baseline exists. The old before/after and runtime files one directory above record the earlier material-only pass; use this directory for the current scene evidence.
