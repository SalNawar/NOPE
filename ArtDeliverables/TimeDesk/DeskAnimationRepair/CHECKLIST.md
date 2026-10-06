# Desk animation repair — 2026-10-07

- [x] Inspect actual runtime bindings and every desk prop mesh.
- [x] Identify single-material-part targets (cashier 1 of 7 meshes).
- [x] Record original cashier rest/motion/return captures and binding report.
- [x] Restore nine whole-object anchors without changing art world poses.
- [x] Include cashier, clock, calendar and stability readouts in their assemblies.
- [x] Verify all six animated props move every part and return in live Play.
- [x] Inspect cashier rest/motion/return native camera captures.
- [x] Final review: all nine prop expectations pass; zero Unity errors.
- [x] Final saved-scene diff review; only prop grouping/readout parenting and regenerated empty shadow-mesh IDs.

Cause: the original desk-copy art flattens mesh renderers into separate material objects. The scene contract fallbacks bind one material part instead of a complete assembly. This was already present in the original AnimeHall commit c75e1fe; recent crowd commits did not change the reaction implementation. Static batching is absent. Explicit Anchor_* roots now resolve whole props.

Motion evidence: cashier 7 mesh parts + money display, phone 4, stamp 3, calculator 8, pen pot 6, stapler 4. Every part moves and returns to its original pose. Clock/calendar/stability reaction assets use ReactionKind.None (tooltip only); their expected result is no movement. The first review incorrectly treated those as motion failures; the final review uses the actual authored expectation.

Scope: nine assembly roots and preserved-world-pose parenting. Original mesh, material, reaction kind, duration and amplitude retained. Camera and light-plane serialized records remain identical to HEAD. Two Unity regenerated shadow-mesh identifiers changed without changing geometry or bounds.
