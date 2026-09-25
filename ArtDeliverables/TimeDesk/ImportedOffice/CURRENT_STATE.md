# Current state: pre-pack room, desktop props, one CRT study

The user rejected the pack-built room and the ceiling repair. OfficeScene was restored from commit `004eb02`, then only desktop props were replaced. The room, original desk, boards, original floor, original ceiling, lighting, portal and exterior remain at that checkpoint. `room_preservation_check.json` records a serialized scene comparison.

The user then requested a ReStory-inspired style guide and Blender application, narrowing the scope to **one object with before/after evidence**. `RESTORY_STYLE_GUIDE.md` and `BlenderCRT/CRT_StyleStudy.blend` implement the first CRT study. Stop at one object for review. Do not expand to other objects without further user direction.

The actual current engine capture is `../DeskFinish/Iterations/53_crt_final_study.png`. Before/after sheets are in `BlenderCRT`. The original vendor CRT is retained inactive under the same desk pivot, beside the Blender variant. Bounds and screen centre match. CRT, Back, NEXT, Back event tests passed; Unity console and URP Lit shader reported no errors. These are event-path tests, not a physical pointer simulation.

The editor menu `Build Desk Props` intentionally affects only the desk. It rebuilds the vendor prop arrangement; `Apply Blender CRT Study` reapplies the reviewed one-object variant afterward. No room builder, generated ceiling repair or global shader/lighting adjustment is part of the current scene.

Push approval was rejected earlier and the user has not answered the destination/payload permission question. Do not assume these local checkpoints were pushed.
