# Applied office colours and textures

2026-09-26. Branch: art. Scene: Assets/Scenes/OfficeScene.unity.

The latest user request authorized colours/textures, a clear teleporter approach, and resumed character generation. The PC remains unchanged. Desk-layout requests remain in the earlier screen-04 preview only.

## Installed result

- Individual prop finishes: burgundy plain pad, coral phone, aubergine till, lavender tray and selected keys, mustard cup, charcoal lamp/NEXT, pale mouse and stapler, warm wood sorter.
- Generated QuietWalnut texture: broad smooth boards with restrained grain, using the existing desk UVs and anime shader. Exact generation prompt is adjacent.
- Warm plaster structure, separately coloured city facades, neutral transparent window glass, coherent aluminium portal casing and blue portal energy.
- Nineteen existing crowd groups moved laterally and oriented to the camera. Existing dimensions, meshes, contact shadows and morning/evening wiring retained. Neutral pale morning and dark violet evening materials.
- Four existing 2D flag sprites retained.
- Four dedicated additive-gameplay scanner materials saved with aluminium body, dark glass, warm timber trim and apricot indicator. Geometry and behaviour unchanged.

![Actual Unity Play-mode capture](runtime.png)

This is a real 1920×1080 GameView capture. Screen-space gameplay overlays were temporarily hidden for the capture and restored before leaving Play mode. Existing lighting and atmosphere affect the palette; generated paint-overs are not a pixel-exact target.

## Validation

before.json and after.json record material assignments, transforms and protected hashes. validation.json reports:
- protected PC and original floor unchanged;
- gameplay component data unchanged;
- all desk renderer transforms unchanged;
- 19 crowd groups, zero projected overlap with the portal;
- 238 renderer material slots assigned to persistent art variants.

The scene was visually reviewed in Play mode, then returned to Edit mode. Unity reported zero console errors. The scanner persistence correction was verified from its saved material files.

## Authoring and limits

Assets/Editor/OfficeArt/OfficeDebtReliefArt*.cs contains editor-only authoring, audit and capture tools. No runtime component was added. Existing shared originals were preserved by creating material variants; the dedicated crowd and scanner materials were recoloured directly.

Menu: Tools > Office Art > Debt Relief. Capture Before preserves the first baseline; Apply Colours Textures And Clear Portal uses original material references from that baseline; Validate Colours checks protected content and crowd clearance. Save Scanner Finishes persists the separately loaded prop.

Do not run broad office/desk/crowd rebuilders: they can restore earlier generated layouts or assignments. No layout edits, debt posters, new PC interface or lore text were installed. The original PC appearance is intentionally retained under the user's explicit exception.
