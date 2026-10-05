# Four-state 2D hall lighting trial

Installed in AnimeHall on codex/hall-art-completion. Existing 58 registered sprites and their gameplay tint controls remain separate.

Four source-aligned native Unity art bakes: morning 08:00, noon 12:00, evening 16:30, night from 19:30. Four illumination maps plus four emission maps, 2172x724 linear RGBA, bilinear clamp. The original painting is multiplied by illumination and receives controlled additive emission. This is an authored 2D light/occlusion bake, not a 3D lightmapper bake.

HallBakedCycle blends adjacent states smoothly. Night holds across midnight until 05:30; dawn reaches morning by 08:00. Existing HallLightingRig.Hour / AnimeHallShiftLink remain the clock source. The same weights set traveller tint and the desk's main light. Desk lamp is warm and PC glow is blue. Preview hours are temporary and restored after captures.

Receiver masks: concourse, foreground platform, gallery and exterior. 38 registered ground shadow shapes are baked at long morning/evening lengths and short noon lengths, with no direct sun casts at night. Existing close contact shadows remain. The old Ground cast shadows object is disabled to prevent double application; its mesh remains the bake source.

17 fixture/screen/sign positions and colours supply localized pools. Fixture emission is restricted to bright pixels in the approved painting near each light; the extracted diffuser layer is not used as an emission mask because it contains unrelated pipe/wall fragments. A subtle moving cloud noise modulates only the baked daylight receiver channel, and freezes with reduced motion.

Limitations: authored receiver/shadow geometry approximates the painted space. It does not reconstruct physical wall occlusion, bounced light for the painted background. Fixture floor footprints are fitted art approximations. Portal states, live display text and dust stay on their existing runtime hooks.

Validation: HallBakedCycleTests 7/7 passed, covering key states, held night, continuous normalized cyclic weights and invalid clock fallback. Unity reports zero errors. 45 native captures in Transitions cover four times, both pans, five desk transition positions, plus an additional left pan and four lighting-off endpoints. The transition filename hour-17 represents the 16:30 evening key (rounded filename); live filenames retain exact decimal hours. Live captures include four key states, two intermediate hours and midnight.

Rebake in edit mode: Tools > Terminal Art > Lighting > Bake Four States.
Live capture: Tools > Terminal Art > Lighting > Capture Four States.
The Game view is open in the managed worktree at C:/Users/Saleh/.codex/worktrees/hall-art-completion/NOPE.

Time-slider preview: Tools > Terminal Art > Lighting > Time Slider. Drag 00:00–24:00 or use Morning/Noon/Evening/Night presets. The window displays the four blend weights. Use game clock releases the lighting override; closing the window restores the previous preview setting. It changes lighting only, not shift progress. The midpoint checks verify 10:00 morning/noon 50/50, 18:00 evening/night 50/50, and restoration.


Lighting correction pass, 05 October: removed the invented detention-door pier cast and three unanchored contact patches. Contact shadows now multiply the receiver instead of blending grey, so they cannot brighten a night floor. Added three visible metal-housed warm gallery fixtures, with floor pools explicitly registered to the upper gallery rather than the concourse. The real desk directional light now changes azimuth and elevation with the same four-state weights. Native morning/noon/evening captures show changing desk-object shadow positions and lengths; the desk lamp remains fixed to its physical source.

