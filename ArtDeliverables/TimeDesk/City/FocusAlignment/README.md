# Traveller-centred composition — installed 2026-10-06

Saleh's yellow cross denotes the player's natural focus/traveller position, not a calculated vanishing point. This change aligns the composition around that guest position.

Applied to AnimeHall:
- Existing traveller anchor stays at (0,0,1.6), with the existing configured height. Camera and desk structure are preserved.
- All AVAILABLE sign mesh parts and both sign labels moved left0.13m; glass/caption now lie on viewport x0.5. Runtime click target binds at world x0.
- Work mat and edging moved left0.13m, and the hand-over marker is centred on the same axis.
- Hall normal-view framing shifted horizontally by3.23m, bringing the painted main portal from viewport x0.41 to0.5. The complete detailed HallDeepMorning source remains intact.
- Corkboard meshes, ephemera and readouts grouped into two preserved assemblies at80% scale, placed at viewport x0.07/0.93. This reveals the stairs and three right-hand doors.
- Left pan bounded to the original canvas with1% overscan; endpoint now local x2.98, normal view x-0.12. No exposed blank source edge in the native pan capture.
- Departure-board marker fitted to source centre(1060,126), size260x90, so gameplay text sits within the painted sign.

Verification: after-empty.png and after-traveller.png are native1920x1080 captures. after-left-pan.png checks canvas coverage. live-traveller.png shows the actual runtime guest called by invoking the real Office/ReadySign Clickable event after starting a shift; AVAILABLE caption, click target, work mat and guest share the centre column. Departure text, clock/calendar/stability readouts and portal glow retain their live bindings. Unity console reported zero errors.

This fixes horizontal composition and framing. It does not reconstruct the hall's painted floor/ceiling geometry or install the earlier unfinished horizon/ceiling extension. Original architecture/detail remain preserved; geometric perspective correction remains a separate issue. Initial before captures failed to initialise lighting and are excluded from this checkpoint.

Authoring: Assets/Editor/TerminalArt/HallFocusAlignmentAuthoring.cs. Menus under Tools > Terminal Art > Composition. Apply Focus Alignment centres sign/work mat/hall; Finish Focus Framing positions the boards, limits pan and fits the display marker. Both save the scene, and the transformations are idempotent for the current drawing.
