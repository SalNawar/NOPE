# The Helix River: stability without a number (2026-10-07)

Saleh: "for the stability meter i really dont like it being a number ... I want something animated like a
dna helix ... that you see degrade and change with time but you dont really see a value for, otherwise the
player will only care for the number being high". He picked "The Helix River" (concept 1, the Chrono-Helix,
merged with concept 2, the River of Time). Reference: `scratchpad/buttons/helix-river-states.png` (STEADY /
STRAINED / BREACHING / COLLAPSING), `helix-river.gif` and the generator `helixriver.py` (+ `timeline.py`,
`timeline2.py` for the glass finish).

## What the player sees

History as a double helix (amber front strand, blue back strand, rungs in the eight nations' colours)
flowing left to right between two green river banks on dark CRT glass, over a faint water band with current
streaks; bright motes ride the strands downstream. It is always moving. No number, no state word.

As stability falls (read from the firing line, collapsing, to 100, steady):

| Damage | Grows with | Reference px (380 px tall glass) |
| --- | --- | --- |
| the centre line meanders | (1 - calm)^1.1 | 6 → 118 |
| the helix widens | 1 - calm | radius 26 → 40 |
| it pushes past the banks: those bank segments turn red, blue flood blooms leak out | geometry | banks at ±86 |
| rungs snap (two stubs) | 0.5 (1 - calm)^1.4 | share of rungs |
| snapped rungs mutate red | 0.5 (1 - calm) | share of the snapped |
| oxbows (loops of helix) pinch off beyond the banks and drift downstream, fading | calm under 0.75 | up to 7 |
| the leading ("now", right) end unzips | 70 (1 - calm)^1.2, from 0.62 + 0.35 calm of the width | px apart |
| the leading end frays | calm under 0.5 | up to 24 fibres |
| the amber reddens | 0.5 (1 - calm) | |
| the CRT glitches (band shifts, chroma split) | from the warning line to the firing line | 0 → 1 |
| the screen flickers | the critical band | on / off |

A loss today (the shift ledger's stability change) agitates the river on top of the damage: the motes race
(up to +40 %) and the glass twitches (up to 0.25 glitch). A citation sends a red pulse running downstream
along the centre line (1.2 s).

## Decisions taken on Saleh's behalf (overnight run, no questions)

1. **Calm is linear from the firing line (60) to 100.** A single wrong call (100 → 97.50) is already visible
   (calm 0.94); the firing line is full collapse. Knob: `steadyAt`.
2. **The glitch starts at the warning line, the flicker in the critical band**: the existing
   `GameConfigSO.stabilityWarningMargin` / `stabilityCriticalMargin` (they tinted the old readout) now drive
   the CRT, so the river's alarm comes exactly where the old amber and red did. The reference glitched from
   calm 0.5 (stability 80); the warning line (70) keeps the middle of the range readable.
3. **The pulse fires when the citation slip is dismissed** (`GameManager.CitationAcknowledged`), not when it
   is issued: the slip covers the office while it is open, so a pulse at issue time would run unseen.
4. **Rungs and oxbows move downstream with the twist** (the reference moved the rungs against it, which read
   as noise in motion).
5. **No state word, no number anywhere a player looks.** The monitor's tooltip says "The timeline. Keep it
   between its banks." (`tooltip.stability`), the tray strip's hint says "Stability" (`tray.stability`, its
   seven translations kept as the bare word, so the Translation Lens glossary still matches), the shift
   report's panel is captioned "The timeline tonight" (`results.timeline`). The verdict line is "WRONG" or
   "WRONG  (-fine)"; the citation slip lost its stability line (`citation.stability` removed); the ledger
   lost "Timeline stability: x (y today)" (`results.stability` removed). The cheat menu and the logs keep
   their numbers (`StabilityRules.Format`).
6. **Reduced Motion** runs the river at a tenth of its speed (0 would freeze it; knob
   `reducedMotionSpeed`), keeps every damage, drops the flicker and shows a citation as a red glow along the
   whole river that fades over the pulse's time instead of a running pulse.
7. **Zoom per display.** Every display draws the same 380 px reference glass; small ones show less of it so
   the helix stays legible: the desk monitor (about 90 x 40 px at 1080p) 1.5, the tray strip 1.6, Home's
   HUD 2, the shift report's wide panel 1.8. Lines drawn under a screen pixel keep their light (thinner is
   dimmer), as the reference reads scaled down; the banks keep most of a pixel's light at any size.

## Architecture

- **Domain** (`Assets/Scripts/Domain/HelixRiver.cs`, pure, `HelixRiverTests`): `HelixRiverKnobs` (every
  number above), `HelixRiverInput` (stability, firing line, warning and critical margins, today's change,
  Reduced Motion), `HelixRiverFrame` (what the shader draws), `HelixRiver.Step` (the shown stability and
  agitation follow their targets with a time constant, `settleSeconds`, so a change never pops; the first
  step starts where stability stands; the clock and the pulse advance) and `HelixRiver.Shape` (the damage at
  a calm). Tests: monotonic degradation, clamping (above 100, under the firing line, NaN), the warning and
  critical lines, smoothing, today's agitation, the pulse's timing and restart, Reduced Motion.
- **Knobs**: `HelixRiverSO` (`Assets/Data/Config/HelixRiver_Default.asset`, created by the builder when
  missing, never written by Generate World).
- **Shader**: `TimeDesk/HelixRiver` (`Assets/Shaders/HelixRiver.shader`): procedural, distances to the
  strands, rungs, banks, motes, oxbows, fibres and pulse in the reference's pixels, glow as the reference's two
  gaussians, the CRT finish (glass gradient, soft reflection, screen-space scanlines, vignette, grain, glitch,
  flicker), composited in gamma space as the reference and converted to linear. One pass without a
  LightMode, uGUI-compatible (stencil, RectMask2D), so it draws on a world quad and in a RawImage.
- **Runtime**: `HelixRiverMonitor` on each display: steps the river by unscaled time, reads the run's
  stability, the shift ledger's change and `GameConfigSO`'s lines, sets the material's floats (its own
  material, made once; no per-frame allocation, checked in Unity), pulses on `CitationAcknowledged`, caches
  `MotionPreference`. `Cover(TMP_Text)` lays the quad over the art's stability text and switches the text off.
- **Builders**: `OfficeSceneUIBuilder.HelixRiver.cs` (the tray strip, the report panel, the fallback HUD
  cell, the desk quad, the wiring) and `HelixRiverAuthoring` (shared with `HomeSceneBuilder`). The art scenes
  are untouched: the binder places the gameplay layer's quad at load (docs/SCENE_CONTRACT_GAMEPLAY.md).

## Rules unchanged

The firing threshold, every loss and gain, the ending checks and the save are exactly as before; only the
display changed.

## Open questions for Saleh

- Should the tray strip keep its hover word ("Stability"), or go fully wordless (Home's strip has none)?
- The desk monitor's glass is small in the hall's framing (about 90 x 40 px at 1080p); a larger monitor prop
  in the art would let the river's detail read from the desk.
