# Applied office colours and textures

2026-09-26. Branch: art. Scene: Assets/Scenes/OfficeScene.unity.

The initial pass applied colours/textures and crowd clearance. The user then corrected the omitted desk layout and wear: **all five notes and worn surfaces are now installed**. See [current follow-up report and Game view](LayoutWear/README.md). The PC remains unchanged.

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

Menu: Tools > Office Art > Debt Relief. Capture Before preserves the first baseline; Apply Colours Textures And Clear Portal uses original material references from that baseline; Validate Colours checks protected content and crowd clearance. Save Scanner Finishes persists the separately loaded prop. Those scanner materials (`Assets/Art/Office/Gameplay/Materials/Placeholder_Scanner*.mat`) belong to the gameplay layer (Build Office UI makes them), so this crosses the ownership line (triage B9); the fix is an art scanner marked `Anchor_Scanner`. Refresh Baseline Hashes (added 2026-09-26) re-stamps the protected and gameplay hashes of both baselines after an intentional change outside these passes, keeping the renderer records. Run it once after the 2026-09-26 cleanup, which changed what the hashes cover: `GameplayAnchors` replaces the legacy `OfficeRoot/CRTMonitor` among the protected roots, the crowd palette and window glass changed fields, and the `*_Painted.mat` name URP Lit.

The broad office/desk/crowd rebuilders were removed on 2026-09-26: they restored earlier generated layouts or assignments. The follow-up in LayoutWear applies the layout and worn materials. No debt posters, new PC interface or lore text were installed. The original PC appearance is intentionally retained under the user's explicit exception.
