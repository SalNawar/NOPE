# Hall lamp mounting and scheduled emission

Three gallery lamps use a new authored unlit gray industrial housing with screws, wall plates, support conduit and beam clamp. They are separate registered drawings, so the background PNG stays unchanged and their diffuser emission remains controllable. Each drawing is a child of its original light; no light transform, camera or gameplay object was moved.

`HallMountedFixture` now uses the same FixtureLevel, fixtureOffShare, lightingOn and strike-flicker preferences as the existing rig. The live warm-stone hall uses Hall Deep Layout (not the older four-state bake): HallBakedLighting supplies nine registered fixture centers and their live levels. The existing protected ceiling-diffuser mask now takes its nearest fixture's level, rather than an evening/night approximation. Screens, signs, portals and desk-light rules are unchanged. No fixture-pool rebake is required.

## Verified

- Seven .NET regression checks link the changed production component and actual HallDayCycle code: noon, night, lighting disabled, changed sunset, strike flicker, flicker disabled and missing fixture. Unity-shaped test dependencies are limited substitutes, not game-scene QA.
- Native Unity6000.4.11f1/URP17.4.0 isolated project compiled the changed production components; sprite imported at724x2172, PPU2205.9375, custom pivot(0.5,0.36), GUID72d39e17c1c9465789398d82b1142706/fileID21300000. Native importer caught and corrected default downscaling/pivot behavior.
- Native shader import passed for mounted fixture and live Hall Deep Layout. Four-state/waiting shaders were checked but unchanged in this final branch.
- Native component checks passed for noon, night, lighting disabled and changed sunset. Native HallBakedLighting checks passed for registered centers and live off/night/noon levels. Test rig/art dependencies were isolated substitutes; this was not the full game scene.
- Scene structure: three new child housings and three drawing links; original light transforms and HallWarmStone PNG unchanged. Nine serialized fixture lights fit the16-slot schedule arrays.
- Source-space placement proof was rendered with unchanged background and authored fixture inputs. Approximate source UVs are clearly documented; not a Unity capture.

## Remaining acceptance

Claude must pull this branch, import the live scene and check full pan, mount-to-beam contact/occlusion, lamp glass emissive boundaries, off/day/dusk/night, custom sunset and reduced-motion flicker. Capture the result and verify the actual game-scene binding before merging to main. Until that evidence exists, this is an implementation milestone, not final gameplay acceptance.

Source PNG SHA256:11F42AB5B0F3208B7341EFE9CB869D6187E60BE521F6D501E4C05057F1F435C6.

Preview Library:libfile_c85c792997c48191824f471b63f68262 / file_00000000fd18823084081e4314cb0d2d, lamp-placement-proof.png. Library save succeeded; Windows xattr helper unsupported, so no local xattr persistence claimed.

## Merged (Claude, 2026-10-07)

Merged on feat/hall-art-1007 and accepted in the game scene (the hall's full pan, off/day/dusk/night, custom sunset, reduced motion). One change at merge: the lit share is no longer recomputed beside the rig. `HallLightingRig` writes each fixture's share to `HallLight.Lit` (0 with the lighting off), and `HallMountedFixture` and `HallBakedLighting` read it, so there is one source for a fixture's level. The isolated .NET and native-project harnesses (`tests/`, `NativeValidation/`) were retired with it: they stubbed the removed `EmissionLevel` and duplicated Unity types; the game scene is the acceptance.

The built-in image tool authored the PNG; no source raster extraction or repaint of the hall was performed. Prompt record accompanies it.
