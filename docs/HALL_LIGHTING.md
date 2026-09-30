# The anime hall's lights, shadows and dust

Saleh (2026-09-30): "there are no lights or shadows placed around the scene I
also want you to create a subtle particles vfx one to create dust particles",
and "scan the scene art analyse all light sources and place 2d lights and
manage them dynamically in the scene have a full day night cycle".

What was built: every light source painted in the hall's registered layers (and
the preserved 3D desk's lamp and PC) got a light, the painted layers are lit
through the URP 2D Renderer by those Light2Ds, the lights follow a full
day-night cycle driven by the shift clock, the sun shafts cast the piers'
shadows, and slow dust motes drift in the light. Everything is placed by
`Tools > TimeDesk > Add Anime Hall Hooks (art office)` (its lighting step,
`AnimeHallLightingHooks`), reproducible and idempotent, and every knob is in
`Assets/Data/Config/HallLighting_Default.asset` (`HallLightingSO`) or on each
light's `HallLight` component.

## The renderer (the decision)

The hall's player camera (`Anime hall player preview`) renders with the
Universal renderer `OfficeForwardRenderer` (the pipeline's renderer 1; its
default, renderer 0, is `Renderer2D`, which the PC's desktop cameras use).
Light2D only works under a 2D Renderer, and switching the hall camera to one was
ruled out: the preserved 3D desk's 155 materials use the art's `NOPE/Desk Anime`
shader, whose only colour pass is `UniversalForwardOnly`; the 2D Renderer draws
only `Universal2D` and `SRPDefaultUnlit` passes, so the desk would not draw at
all (and the desk's real lighting and cast shadows, from the art's directional
daylight, would go with it). URP cannot stack a 2D camera with a forward one
(the stack must share a renderer type), and a URP base camera always clears, so
two cameras cannot simply draw over each other.

So the hall is drawn in two passes (`HallBackdrop`):

1. A camera made at runtime (never saved) with its own 2D Renderer
   (`Assets/Settings/HallRenderer2D.asset`, a copy of the pipeline's
   `Renderer2D`, listed in `UniversalRP`'s renderers by the hooks tool) draws
   only the `HallBackdrop` layer (the 58 painted layers, the portal rings'
   effects, the dust and the Light2Ds) into a texture of the screen's size. It
   copies the office camera's pose and lens every frame before any camera
   renders (`RenderPipelineManager.beginContextRendering`), so the desk view and
   any art pan stay in step.
2. The office camera (unchanged: forward, the 3D desk lit by the art's daylight
   with its shadows, the papers, the traveller, every text) first draws that
   texture as one full-screen triangle (`TimeDesk/HallBackdrop`, the Background
   queue, no depth test or write), then everything else over it. It never draws
   the `HallBackdrop` layer itself.

The painted layers use URP's `Sprite-Lit-Default` material (the Light2Ds light
them in the 2D pass; its forward pass draws them unlit). With the lighting off
(`HallLightingSO.lightingOn`) or without a 2D Renderer in the pipeline, the
office camera draws the layer itself, so the hall shows as painted, unlit;
nothing turns black. The PC frame and clone cameras are untouched (they draw only
`PCDesktop`, and the full-screen draw shows only in the office camera). The
texts (the readouts, the NEXT sign, the board's rows, the tooltips, the UI) stay
unlit: they are drawn by the office camera over the lit hall. The traveller is
drawn by the office camera too (over the hall, behind the desk), so a lit sprite
material would draw unlit there; instead its tint is multiplied by the cycle's
`travellerShade` (white by day, a moonlit blue-grey at night, kept light enough
that the face reads), through `AnimeHallShiftLink` and `TravellerView.Tint`.

The layers are mutually exclusive masks (`layers.json`: "Mutually exclusive
visible-pixel masks"), so the exterior layer (`03`, the sky and the city through
the windows) moved to its own sorting layer `HallSky` (listed before Default)
draws exactly where it did; only the sky light reaches it, so the interior's
fixtures never light the sky.

Cost (measured in play, 1920×1080, the editor; the audit profile job and the
track's play job): the lights' components allocate 0 B a frame (the rig, the
backdrop, the two links, measured call by call). The backdrop camera has its
own 2D Renderer because a renderer's per-frame light and shadow texture tables
are rebuilt whenever cameras with different layer batches share it: sharing the
default `Renderer2D` with the PC's desktop cameras cost 1188 B a frame. What is
left is URP's: its 2D Renderer keeps the layer batches in one static table for
every 2D camera, and the backdrop camera (two blend styles) and the PC's clone
camera (none) resize its small index arrays as they alternate, 36 B + 32 B a
frame (the office frame: 436 B, was 368 B). The 2D pass costs about 0.6 to 1.2 ms
of main-thread time a frame in the editor (a second camera: culling, the light
textures at half size, the 58 layers; frame time median 4.9 ms, was 4.3 ms), and
the first office load of a session about 250 ms more (1.34 s, was 1.08 s: the 2D
Renderer's first use and the heavier scene; the day's later loads are within
10 %).

## The inventory: every painted light source

Positions on the hall's canvas (layers.json, 2172 × 724 px, y down) and on a
1920 × 1080 screen from the office camera (screen x = canvas x × 1.0459 − 361,
y = canvas y × 1.0438 + 4; the canvas is wider than the screen, so its left
edge is off screen). Found from the layers' opaque pixels (bright, near-neutral
connected regions; `SCRATCH/halllights/sources2.py`) and by eye on the composite.

| Source (layer) | Canvas px | 1080p screen px | Light (under `HallLighting/Plane`) |
|---|---|---|---|
| Ceiling fixture, front left (01) | (922, 35) | (603, 40) | `Fixtures/Fixture 1 - front left` |
| Ceiling fixture, front right, by the board's hanger (01; 15) | (1068, 35) | (756, 40) | `Fixtures/Fixture 2 - front right` |
| Ceiling fixture, middle left (01) | (828, 117) | (505, 126) | `Fixtures/Fixture 3 - mid left` |
| Ceiling fixture, middle right (01) | (1208, 117) | (902, 126) | `Fixtures/Fixture 4 - mid right` |
| Ceiling strip, rear left (02) | (970, 190) | (653, 202) | `Fixtures/Fixture 5 - rear left` |
| Ceiling strip, rear right (02) | (1059, 190) | (746, 202) | `Fixtures/Fixture 6 - rear right` |
| Left curtain wall, pane 1 (03 through 02) | 95–260 at y 40 | off screen, (−262, 46)–(−89, 46) | `Windows/Shaft - left pane 1` |
| Left curtain wall, pane 2 | 310–405 at y 40 | (−37, 46)–(62, 46) | `Windows/Shaft - left pane 2` |
| Left curtain wall, pane 3 | 440–505 at y 40 | (99, 46)–(167, 46) | `Windows/Shaft - left pane 3` |
| Rear windows, left band (03 through 02) | 730–900 at y 205 | (402, 218)–(580, 218) | `Windows/Shaft - rear windows left` |
| Rear windows, right band | 1060–1240 at y 205 | (748, 218)–(936, 218) | `Windows/Shaft - rear windows right` |
| The sky through every window (03) | x 0–1322, y 0–571 | (−361, 4)–(1022, 600) | `Sky light` (global, `HallSky`) |
| Departure Board display (16) | [853, 74, 1142, 173] | (682, 132) | `Screens/Departure Board display` |
| Bridge display, left (08) | [855, 278, 902, 299] | (557, 306) | `Screens/Bridge display left` |
| Bridge display, right (08) | [1142, 278, 1190, 299] | (858, 306) | `Screens/Bridge display right` |
| Vending machine's lit front (31) | [1720, 476, 1764, 582] | (1461, 556) | `Screens/Vending machine` |
| Kiosk / wall terminal, upper right (04) | [2100, 142, 2140, 234] | (1856, 200) | `Screens/Kiosk` |
| Detention door sign (20) | [1392, 358, 1452, 408] | (1126, 404) | `Signs/Detention sign` |
| Medbay door sign (21) | [1484, 382, 1556, 450] | (1229, 438) | `Signs/Medbay sign` |
| Executive suites door sign (22) | [1598, 410, 1680, 496] | (1353, 477) | `Signs/Executive suites sign` |
| Portal 01 ring, front (39) | (1027, 495) | (713, 521) | `Portals/Portal 01 ring` |
| Portal 02 ring, rear left (41) | (904, 367) | (584, 387) | `Portals/Portal 02 ring` |
| Portal 03 ring, rear right: the Return Gate (43) | (1139, 367) | (830, 387) | `Portals/Portal 03 ring` |
| Portal 04 ring, upper left (45) | (1700, 208) | (1417, 221) | `Portals/Portal 04 ring` |
| Portal 05 ring, upper right (47) | (1949, 203) | (1677, 216) | `Portals/Portal 05 ring` |
| The whole hall | — | — | `Global light` (global, Default) |

The preserved 3D desk (world metres; the office camera at (0, 2.16, −2.62)):

| Source | Where | 1080p screen px | Light (under `HallLighting/Desk`, 3D) |
|---|---|---|---|
| The green desk lamp (`Clean_Lamp__DeskClean_Green`, its paper inner shade) | under the shade, (0.46, 1.47, 0.59) | about (1110, 665) | `Desk lamp` (a spot pointing down, warm) |
| The PC's screen (`CRT2_Glass`) | 0.22 m in front of the glass towards the camera | about (245, 765) | `PC screen glow` (a point, cool) |

Not lights, on purpose: the NEXT sign's glass, the clock's and the stability
monitor's glasses and the till's display are dark glass with light digits (the
digits are unlit texts, readable at every hour); the fire cabinets (52) are
extinguishers, not lamps; the painted "fixture diffusers" layer (51) holds
ceiling hardware fragments, not lit panels (the lit panels are in layers 01 and
02). The hall's own `Movable warm maintenance light` (a 3D point light on the
art's layer) lit the painted layers through their old shader; it no longer
reaches them (they are drawn by the 2D pass) and is left as the art placed it.

## The day-night cycle

`HallDayCycle` (pure, `Assets/Scripts/Visuals`, tested: `HallDayCycleTests`)
turns an hour into: the daylight (1 by day, 0 at night, eased through a dawn
and a dusk of `twilightHours` centred on `sunriseHour` and `sunsetHour`); the
solar position (0 solar midnight, 0.25 sunrise, 0.5 solar noon, 0.75 sunset;
every colour gradient and intensity curve of the knobs is read there, so moving
sunrise or sunset moves them all); the sun's arc (0 at sunrise, 1 at sunset);
each fixture's switch (it reads the daylight its order × `fixtureStaggerMinutes`
earlier and comes on as that falls below `fixturesOnBelow` through
`fixtureFadeBand`, so the fixtures come on one after another at dusk and go off
in the same order at dawn); and the strike flicker (0.6 s of blinks, none with
reduced motion).

The hour: the shift clock's minute (`IShiftProgress.MinuteOfDay`, handed to the
rig by `AnimeHallShiftLink` each frame); `previewHourOn` + `previewHour` override
it (the way to see night and dawn, which a shift never reaches: it runs
09:00–17:00); without a clock (the hall open on its own, edit mode)
`editModeHour`. Defaults: sunrise 07:00, sunset 16:30, 1.5 h twilights, so a
shift opens in a morning light, is full day at noon, turns warm in the late
afternoon and ends in the dusk (the fixtures strike from about 16:24, 4 in-game
minutes apart); night and dawn are the preview hour's (and Home's evening is a
separate scene: the hall is not seen then). The art presentation's own
`SetTime` (its daylight on the 3D desk) takes the cycle's evening (1 −
daylight), and the calendar's ink switches on it as before.

What each kind of light does (`HallLightingRig`, every frame, 0 B):

| Kind | Driven by |
|---|---|
| Global | `globalColour` × `globalIntensity` at the solar position (multiplied over the art: 0.88 at noon, 0.4 blue at night) |
| Sky | `skyColour` × `skyIntensity` (only the `HallSky` layer: bright by day, orange at sunset, deep blue at night) |
| Window (the shafts, additive) | `windowColour` × `windowIntensity` (strongest with a low sun, none at night); each turns about its pane's top from `shaftAngleAtSunrise` to `shaftAngleAtSunset` along the sun's arc and lengthens with a low sun (`shaftLowSunLength`); they cast the piers' shadows while `shaftShadows` |
| Fixture | `fixtureColour` × its level (`fixtureOffShare` while off) × the strike flicker (`fixtureFlicker`, never with reduced motion) |
| Screen, Sign | always on, their light's own colour, `screenDayShare` / `signDayShare` of their strength by day, full at night |
| Portal | on while its ring shows a glow or the Return Gate's spiral (`AnimeHallPortalLink` → `SetPortal`), off while it is closed (CLOSED or under maintenance); `portalOpenLight` or `returnGateLight`; fades over `portalFadeSeconds` (at once with reduced motion) |
| DeskLamp, DeskScreen (3D) | always on, `deskLampDayShare` / `deskScreenDayShare` of their strength by day |

Each light's strength at full share is its `HallLight.intensity` (the rig writes
the light's own intensity from it: tune that one); a fixture's switching order
is its `HallLight.order`, a portal light's portal number too. Duplicating a light
under `HallLighting` makes another one (the rig gathers its lights at play).

## Shadows

The two piers between the windows and the hall (layers 09 and 10) carry
`ShadowCaster2D` rectangles (`Plane/Shadows`); the sun shafts cast their
shadows (shadow intensity 0.55, soft). The desk's props cast real 3D shadows
from the art's daylight, as before (the desk is the forward pass). The traveller
is drawn by the office camera, not the 2D pass, so no 2D shadow falls from it.

## Dust

Three particle emitters (`Plane/Dust`): in the left window's shafts, in the rear
hall under its windows, and in the right wing's air, each well under the
Departure Board so no mote drifts over its rows. Low count (`dustMaxParticles`
40 each, `dustRate` 3 a second, 9–16 s lives), soft (a 32 px radial mote,
`HallDustMote.png`), slow (`dustDrift` 0.08 m/s with a gentle noise), drawn by
the 2D pass on the art's sorting layer at order 58 (over the painted hall, under
the desk and the traveller), in `TimeDesk/HallDust`, which is lit by the hall's
Light2Ds: a mote shows bright in a sun shaft or under a fixture and fades in the
dark, so the time of day tints it. With reduced motion the dust is off
(`dustReducedMotionShare` 0; a higher share thins it instead). No allocation per
frame (the particle system is native).

## Readability (measured on screen)

The play job measures each text against what is under it on the 1080p
screenshots (the lightest tenth of the rect's pixels for light ink, the darkest
for dark ink, against the median of the other half; `SCRATCH/halllights/contrast.py`)
at opening, midday, late afternoon, dusk, night and dawn: the stability, credits
and clock digits and the NEXT sign read 12:1 or better at every hour (they are
light digits on the dark desk glasses, lit by the forward pass as before), the
board's rows 4.5:1 or better, and the calendar's date as before (its ink switch
is on the same evening blend). The report's figures are in the track's report.

## Reinstalling the hall's art

`AnimeHallLayerInstaller` rebuilds the scene without the hooks: run Add Anime
Hall Hooks again (its lighting step recreates the lights where the inventory
put them; a renamed or renumbered painted layer is an edit of
`AnimeHallLightingHooks`' layer ids and pixel positions).
