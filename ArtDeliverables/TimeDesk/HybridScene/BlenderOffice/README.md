# Blender office object pass — 24 September 2026

Installed in `Assets/Scenes/OfficeScene.unity`. The user reviews the result in game; this is an installed art revision, not final visual approval.

43 FBX models and 43 material-bound prefabs live in `Assets/Art/Office/Hybrid/BlenderOffice`, alongside the three BlenderPC assets.

| Editable library | Models | Contents |
| --- | ---: | --- |
| Booth_Hardware.blend | 12 | Till, Next, clock, stability device, calendar, intercom, tray, stamp, partition, board, blotter, newspaper |
| Hall_Collection.blend | 16 | Hall shell, floor, banner, departures, bench, portal, cabinet, plinth, stand, six framed paintings, paperwork |
| Sculptures.blend | 3 | Thinker, Discobolus, Nefertiti; approximately 7,000 triangles each |
| Details_Collection.blend | 9 | Press, Wright Flyer, Rocket, Voyager cover, bracket, pin, three vehicle bodies |
| City_Collection.blend | 3 | Civic, twin and terraced megablocks |

Each source library has one collection per exported asset, with individual parts retained. Collections share an origin for reusable exports; isolate a collection while editing. Offline `author_*.py` files and `artlib.py` stay outside Unity's Assets folder. Manifest JSON records renderer nodes, palette and triangle counts.

## Scene integration

- PC and keyboard share an inward yaw of approximately 31.61 degrees. The keyboard sits in front of the chassis. Neutral grey/ivory casings use orange only on controls. Monitor click proxy and zoom camera follow the screen.
- Till and panel devices face inward. Devices/pictures mount against the boards using their physical back faces. `panel_mounts.json` records final overrides.
- Original `CreditsNumber`, `NextLabel`, `ShiftClockDisplay`, `StabilityPercent` and `DayNumber` objects retain their identities and gameplay references. This art pass does not add a shift-time system; clock text keeps its pre-existing setup.
- Paintings use dimensional frames and canvas-only UVs; Mondrian has a detached corner. Three statue cutouts are replaced by actual sculpture geometry. Both Voyager keepsakes have physical disc geometry.
- Four ceiling banners, three window walls, established exhibition positions and the open portal approach remain. Benches face inward. No characters, queues, roads or generic collectibles were added.
- Twelve original `OfficeTrafficVehicle` roots retain their animation components and lane settings; only child meshes changed.
- The new industrial portal ring surrounds the existing separate animated opening. The existing shader now uses broad drifting shapes instead of concentric rings.
- Sky and distant-city images remain separate layers. Five nearby buildings use the three new FBX variants.
- Lighting is realtime, with neutral warm daylight and cool fill. No lightmaps or time-of-day shadows were baked into new materials.

Superseded visible meshes were removed after replacement. No C# installers, new gameplay scripts or editor auto-run hooks were added. Earlier source assets remain for recovery and historical-machine authoring input. Placement JSON and `before_*.json` record current/earlier transforms.

## Review status

No Play mode, automated tests or Unity Game-view capture was run. A Blender sculpture turntable selected front orientations; `Sculpture_authoring_angles.png` shows the source angles before the final correction, not an in-game result. See `SOURCES.md` for provenance and redistribution credits.
