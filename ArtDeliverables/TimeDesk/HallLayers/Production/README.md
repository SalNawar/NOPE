# Furnished terminal — high fidelity 2.5D Unity composition

Open **Assets/Art/Office/TerminalProduction/FurnishedTerminal.unity**, then the Game tab at 16:9. The editor menu **Tools > Terminal Art > Open Furnished Terminal** does both. This replaces the rejected blockout as the visual delivery; it does not overwrite OfficeScene.

`unity-forward.png` is a real 1920 × 1080 Unity camera capture. It is not the generated mockup presented as a game render.

## Composition and rendering

- The architectural sprite was edited from the approved mockup, preserving its central gate, second rear gate, upper right platforms, bridge height, left stair/window/city and right destination passage. Its foreground desk and noticeboards were removed; the foreground visible in Unity is the actual existing 3D desk art.
- Architecture carries painted/baked daylight, lantern lighting, portal glow and soft floor reflections. It is intentionally a 2.5D painted backdrop, not a fully navigable or dynamically relit building.
- Four separately placed furnishing sprites supply the station lockers/bench, one neglected framed painting, stone bust and ceramic display. Their atlas has real alpha. Cropping happens through Unity Sprite rectangles; the generated PNG is retained intact.
- Eight independently placed anonymous crowd groups reuse the approved group meshes/alpha atlas with a new production-only atmospheric tint. The foreground group avoids the lamp and central boarding lane.
- Real directional window light and a task-lamp spotlight light the preserved 3D desk. Existing desk meshes, materials and PC are referenced without modifying their source assets. Static preview labels reproduce the reference's NEXT, clock, calendar and stability displays. They are not gameplay UI.
- The composition camera keeps the original position and 55° vertical FOV; preview pitch is 4° to align the desktop's screen height with the mockup. No live game camera was changed.

## Assets

- `FurnishedTerminalArt.prefab`: hall layers only; no desk, gameplay, camera or lights.
- `FurnishedTerminal.unity`: visual review scene with the original desk art, hall layers, camera and foreground lights.
- `Textures/Architecture.png`: registered architectural plate.
- `Textures/Furnishings.png`: transparent furnishing atlas.
- `TerminalPaintedSprite.shader`: forward-renderer sprite shader with alpha blending and normal scene depth testing, independent of all protected shaders.
- `Assets/Editor/TerminalArt/TerminalProduction.cs`: reproducible authoring and capture tool.

The original PC screen remains connected only in the live gameplay scene; this art-only preview does not run its desktop or character systems. The preview therefore does not show the live PC desktop. Original OfficeScene, OfficeGameplay and floor assets are preserved.

## Integration boundary

This delivery is the requested high-fidelity forward-view art composition. Continuous left panning needs an adjoining painted view and an agreed transition; arbitrary walking would require further geometry. Traveller routing, live UI, camera controls and installation into the running game remain Claude-owned. Do not reuse the previous blockout's physical gate coordinates as destinations for these camera-composed sprite layers.

Regenerate with `TerminalProduction.Build` through the editor menu or Unity `-batchmode -executeMethod TerminalProduction.Build -force-d3d11` (no `-quit`; the builder waits for rendering, captures and exits itself). The exact generation prompts are stored beside this document. No removed art rebuild menu is used.
