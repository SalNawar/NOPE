# CITY-R17: the r17 city as the game's city backdrop

Branch `feat/city-r17`, cut from `origin/main` 4290bd6 (the six Phase A tracks had not landed). Source of the look: the r17 look-dev, `E:\unity\NOPE-tools\city\live\r17.html` (its README's r16 and r17 sections; the target video `r17\city_r17.mp4`).

**Saleh's words this builds:**
- 2026-10-21, on r16: "Fir the city the clouds are dead, no cars still, we are missing a train, the ads too slow not alive and boring just text" (r17 answers each one).
- Tonight: "give me a polished demo with the new city flying cars and new desk and switchboard and all other fixes".
- Earlier rules that still hold: "dont have flying cars or lights or smoke or fire baked in the picture even the billboards all of this we will animate"; "you should be able to see the FULL picture"; "for now, one daytime look. Do not ask about time of day again."; camera sway made him motion-sick (sway stays off).

## What the player gets

- **The city view** (A, the left arrow or the CITY tab) shows the r17 city, alive, with nothing baked in:
  - **The 11 planes** move with the page's parallax as the pointer moves (rate-capped, BUG-69).
  - **8 soft clouds** sit at three depths behind every building. They drift continuously, morph between their painted drawings, and wrap off frame or behind a building.
  - **About 1,000 3D flying vehicles** (taxis, coupes, buses, trucks), toon-shaded with an ink outline. They fly the page's lanes with its car-following, lane changes, banking, thrust pitch and hover bob, and spawn and despawn hidden.
  - **The double-deck train** crosses the viaduct twice every 48 s.
  - **22 ad screens** run the 28 animated ads with channel-flip, wipe and CRT changes, and no ad is ever on two screens at once.
  - **The furnace fire, both smoke stacks, the noon lamps and the laundry sway** on the tenements run as the page's loops.
- **The hall's windows** look out on the same city:
  - The hall's turn moves its camera a little.
  - The old painted flyers are hidden.
  - The hall's hour tints the windows only (the city view has no hour grade).
- **Reduced Motion:** no parallax, and the smoke and fire hold one drawing. The traffic, train and clouds keep moving (CITY-LIFE spec P5).
- **Off switch:** `Assets/Resources/CityR17/CityR17Config.asset` `live`. Off, or with data that fails to parse, the city view and the windows are exactly as before. The fallback is a data switch, never a revert.

## How it works

| Part | Where | Notes |
|---|---|---|
| Data | `Assets/Data/City/R17/city_r17_{scene,traffic,vehicles,art}.json` | `tools/art/city_r17/export_r17.js` reads them from the running page (headless Chrome, frame loop held): the lanes after the page's load-time edits, the slot rules, the four fleet meshes, every loop's tiles and schedule, the noon lamps, and the page's own draw order (`order`, from `App.drawers`). `import_r17_art.py` writes the art map. |
| Art | `Assets/Art/City/R17/{Planes,Clouds,Ads,Fire,Smoke,Train,Hive}` (40 textures, ~109 MB at BC7) | `tools/art/city_r17/import_r17_art.py`. Pixels are never resampled. The planes and the viaduct paint are cropped to their painted pixels (placed by the art map), and everything is padded to a multiple of 4. `CityR17Import` enforces the import settings: no sRGB decode, alpha is transparency, no mips, BC7, max 4096. |
| Pure logic (tested) | `Assets/Scripts/Domain/City/R17/` | `CityR17Traffic` is a line-for-line port of `js/r17traffic.js` (slot clocks, IDM, ramps, lane changes, attitude), pooled, with an allocation-free frame path. `CityR17Loops` holds the clouds, train passes, ad tiles and schedule check, flipbooks, spill gain, lamp pulses and laundry sway. `CityR17Look` is the camera spring with the BUG-69 cap. `CityR17Framing` is the page's overscan, letterbox or fill. `CityR17Order` is the page's draw order. `CityR17Rng` is mulberry32. Also the parsers. |
| Runtime | `Assets/Scripts/City/R17/` | `CityR17` (the facade: clock, look, budget, hall hookup). `CityR17Stage` (one command buffer a frame into a render texture). `CityR17TrafficDraw` (instancing). `CityR17Windows` (the hall's windows). `CityR17ConfigSO`. |
| Shaders | `Assets/Shaders/City/R17/` | `Sprite` (alpha, add, depth mask and morph passes), `Vehicle` (instanced toon body and ink hull), `Lamp`, `Window`, `Present`. |
| Hook | `CityView.cs` (+33 lines) | `Bind` attaches `CityR17`. The panorama shows its texture letterboxed on the matte. `Set`/`SetLive` accept the r17 city as live. `Draw` tells it when the view shows. |
| Editor | `Assets/Editor/City/R17/CityR17Import.cs` | Import settings, plus `Tools > TimeDesk > City R17 > Check` and `Rebuild Config Art`. |

- **Draw order:** the page's own:
  - P00, clouds, traffic; P01, lamps, traffic; P02, lamps, traffic; P03, traffic; P04, lamps, traffic;
  - P05, traffic, ads; P06, traffic; P07, train, lamps, traffic; P08, laundry, traffic, smoke (at P09's parallax);
  - P09, fire, smoke mouths, traffic; P10, traffic.
  - Every drawer the page runs is ported. A drawer a later cut adds and the port does not know is named by `CityR17Order.Unported` (and by `Check`), never silently lost.
- **Occlusion:** the page's slot rule.
  - A vehicle draws right after the nearest plane still farther than it, with LANE_SLOT, LANE_CAP and SLOT_FIX applied.
  - Each slot's vehicles draw in two groups (the K skyway first, then the rest), each on a cleared depth buffer, as the page's per-group WebGL passes.
  - Slots 1-4 are first cut by the P01 and P02 towers (written to depth from their alpha), so no far speck sits on a megatower face.
- **Traffic rendering:**
  - The four meshes are drawn with `CommandBuffer.DrawMeshInstanced` and one shader. Each instance's matrix is its model-to-camera frame; per-instance livery, haze, ink, lamps and parallax go through property block arrays, in chunks of 250 or fewer.
  - The vertex shader projects through the page's pinhole with the instance's own parallax.
  - The model matrix is mirrored, so both passes draw both faces. The ink hull is pushed 0.8 m back in depth so it only shows round the silhouette.
  - Ink-free (far, under 7 px) vehicles skip the hull pass. The pods are dropped at load (`dropPods`), as the page.
- **No camera, no layer:** the stage records one command buffer a frame (`Graphics.ExecuteCommandBuffer` in LateUpdate). That replaces the notes' "stage camera with RenderParams.camera". The forward renderer carries the office's SSAO feature, which would have run on a stage camera, and the 2D renderer would sort by its own rules. MSAA 4x on the draw target, resolved into a mipmapped output (the hall's windows sample it small).
- **Colour:** the city composites in the look-dev's gamma space, so every blend matches the canvas. The textures are not sRGB-decoded and the target is linear-format. The two readers decode it once: the city view's `Present` UI shader and the hall's `Window` shader.
- **Time:** the city's own clock on unscaled time, each frame capped at 50 ms as the page. The loops run on the page's 24 fps frames. The clouds and the train move continuously, and the held train drawings snap to the output's pixel grid. The traffic substeps at 1/120 s. Hidden (no view, no window on screen), nothing steps or draws.

## Checks

| Check | Result |
|---|---|
| `compile_check_any.py E:\unity\NOPE-gpt` | 0 errors; no warnings in the new files |
| Offline runner | passed 3763, failed 0 (24 new: `Assets/Tests/EditMode/City/R17`) |
| Traffic against the page | The C# sim replays the page's golden record exactly: every vehicle's id, lane, type, s and v after the prefill and after 48 frames, and every drawn vehicle's x, y, Z, slot, roll and pitch (within 2e-5 for s and v, 2e-3 px for x and y). |
| Loops against the page | The clouds' x, drawing and morph weight at 11 frames, and the train's pass on all 1,152 frames of its cycle, match the page's own functions. No ad is on two screens at once (the ads17 schedule's own rule). The lamps never step more than 0.2 a frame. |
| SO dead-field scan (QA) | All 27 `CityR17ConfigSO` fields have readers |
| Sim cost (.NET, offline) | parse 51 ms + prefill 82 ms at build. 0.50 ms a frame at 60 fps (sim plus attitude, ~1,000 vehicles); the hinted lane lookups and shared spring decays halved it from 0.86 ms. |
| Unity 6000.4.11f1, batchmode (`-executeMethod`, an untracked helper, no GUI) | The 40 textures import, the five shaders compile and are supported, and `Check` reports nothing. The stage rendered at 1920x823; its frames match the look-dev's `r17/frames/f096.png` side by side: the planes, clouds, ads, train with glows, smoke, fire, laundry and traffic, upright, in order, with no vehicle on a megatower face. The hall windows' shader over the hall canvas matches the mapping, and the present shader writes back the city's exact colours (0 difference in an sRGB target). |
| Unity CPU (Mono, batchmode) | parse 36 ms + build 88 ms. The sim is 0.46 ms a frame at 60 fps, and recording is 0.33 ms (46 draws of instanced traffic plus about 70 sprite draws). Batchmode has no render thread, so its submit (1.1 ms) runs inline there. |

**Still for the Unity play check** (the recipes):
1. Play OfficeGameplay and press A:
   - the city view shows the r17 city letterboxed;
   - compare it side by side with `city\live\r17\city_r17.mp4`: the plane order, traffic in front of and behind the right planes, the ads over the J-canyon traffic, the smoke over the D lanes, the train between its mattes, no vehicle on a megatower face.
2. Move the pointer: the planes part, never faster than 1 px per 30 fps frame.
3. Reduced Motion: no parallax, the smoke and fire still.
4. The hall's windows (drawn by HallBackdrop's 2D camera through the `Universal2D` pass) show the city. The flyers are gone, and turning toward the window wall moves the city slightly.
5. Profiler, city view at 1920x1080: CityR17.LateUpdate under 2 ms CPU, 0 B GC a frame. GPU: 11 planes, ~50 instanced draws.

## Decisions (ours; flagged for Saleh)

- **Letterbox by default** (the whole 21:9 frame, as the look-dev's video and "you should be able to see the FULL picture"). `fillScreen` fills the height and pans with the pointer instead (BUG-42's "no bars").
- **Continuous train motion** at the display's rate, snapped to whole output pixels, instead of the page's 24 fps steps of 5 px, which judder at 60 fps.
- **The hall's windows:**
  - the left wall's windows show plate (300, 20, 1500, 1086), and the front clerestory (1280, 60, 1000, 480): config knobs;
  - they redraw at 30 fps (halving under budget pressure);
  - the hall's hour tints them (`windowHourTint`); the city view keeps one daytime look.
- **Left out on purpose:** the depth-of-field blur stays off (CITY-LIFE spec P1: "buildings should be clear"); the page's half-pixel blur on the far planes would only soften them.

## Follow-ups

- **HALL-FX (integrate/six):** CityR17Windows finds the windows by the "NOPE/Hall Living City" shader on `HallCityExterior.window`, `panelRenderers` and `lanes[].vehicle`. All three are still on HALL-FX's `HallCityExterior`, so it should hold after six lands. The boxes assume the 2172 x 724 hall canvas.
- **CITY-LIFE (`feat/city-life`):** this replaces it. Its look-along and flash budget were for the r6 sprite city.
- **Sound:** `amb_city` and the fly-by cues are still FEEDBACK's (CITY-LIFE's SOUND_CUES fragment applies).
- **Memory:** the 4 ad sheets are 63 MB of the 108 MB. Lowering their max size to 2048 in the importer quarters them, at a visible cost above 1080p.
