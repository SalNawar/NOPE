# Registered anime hall installation — preparation checkpoint

Current approved-direction source: `../Master/TwoPanel/hall-anime-platform-v8.png` (2172 × 724). The updated painting is complete; layer extraction and scene installation are NOT yet complete.

The user requested splitting the finished painting into layers and installing them in Unity. An asynchronous question is pending: permission to use exact pixel masks/scripted extraction rather than independently regenerating transparent cutouts. No scripted image extraction has been performed pending that answer.

## Prepared

- `clean-underlay-v1.png`: built-in imagegen reconstruction of surfaces behind props and railings. This is a hidden-surface aid, not a replacement for the visible master. Full prompt in `clean-underlay-prompt.txt`.
- `Assets/Art/Office/AnimeHallLayers/AnimeHallLighting.shader`: URP forward sprite lighting; independent per-layer material tint and approximate surface normal; realtime main/additional lights. Not a Light2D shader.
- `AnimeHallPresentation.cs`: art-only external time/pan inputs, named layer access, material tint and lighting preview. No gameplay state, authoritative clock or input routing.
- `Assets/Editor/TerminalArt/AnimeHallLayerInstaller.cs`: imports full-canvas registered sprites from a manifest, preserves the approved desk from the saved art scene, creates a separate scene/prefab, and captures lighting/pan checks. Refuses installation while the layer manifest is absent.
- `AnimeHallAuthoringBridge.cs`: explicit one-shot local authoring connection/status requests only. Does not change server permission settings.

Unity 6000.4.11f1 project NOPE was connected. Shader check reported no errors; scripts finished compiling. Active source scene is `Assets/Art/Office/TerminalLayered/LayeredTerminal.unity`, clean. No existing Unity scene or gameplay source has been saved or rebuilt in this checkpoint.

## Extraction contract

Every sprite retains the 2172 × 724 canvas, centered pivot and 100 pixels per unit. No independent cropping, resizing, perspective shifts or re-layout. The composited visible layers must reproduce the approved master. Exterior remains ONE placeholder layer; no city generation now.

Separate exterior, window structure, ceiling/service structure, walls/piers, concourse floor, upper walking surface, bridge fascia, left elevator/stair architecture, foreground platform/rail, other rails, five portal assemblies, transparent security panes, two flags, departure-board frame/display, three doors and their pictograms, lamps/diffusers, lockers, rear scanner, rear seating/dispenser, rear maintenance equipment, artifacts, vending/seating and bins. Use additional splits where actual outlines require them. Text/flag marks remain replaceable presentation overlays.

Manifest expected at `Assets/Art/Office/AnimeHallLayers/layers.json`:

```json
{"width":2172,"height":724,"source":"hall-anime-platform-v8.png","layers":[{"id":"00 Exterior placeholder","file":"00-exterior.png","order":0,"normal":[0,0,-1]}]}
```

Texture files belong in that folder's `Textures` subdirectory. The installer menu is `Tools/Terminal Art/Install Registered Anime Hall`. Intended outputs are `AnimeHall.unity` and `AnimeHallArt.prefab` in the new art folder. These files do not exist yet.

The current installer frames the source at its native 3:1 aspect above the unchanged 3D desk and exposes a limited leftward translation across the available master. A wider matching city-window extension is still separate future work. Surface normals are approximate planar art normals; no normal-map or physical glass/refraction claim is made.
