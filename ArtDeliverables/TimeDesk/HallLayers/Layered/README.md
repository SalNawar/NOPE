# Modular illustrated terminal — art preview

Open `Assets/Art/Office/TerminalLayered/LayeredTerminal.unity`. The original gameplay scene, desk source and PC are unchanged. TerminalRelit remains preserved.

## Layers

City.png is an independent exterior panorama. HallShell.png and LeftShell.png contain architecture; Floor.png and LeftFloor.png are separate floor layers. Architecture now has patched stone, cracks and service panels. ModularFixtures.png supplies separate portal, sconce, station storage and flag sprites. Railing steel/brass, floor, board trim, portal energy, flags and fixture categories have independent materials. Flag emblems are replaceable sprites; the diamond is a provisional mark.

Departure and route text use editable TextMesh components. Destinations A–D are sample data. Antique luggage and irregular hanging lamps are removed. Station storage sits on the floor with a contact shadow; four repeated sconces form paired architectural bays. Crowds, displayed art and desk remain separate.

## Claude presentation hooks

HallDisplayView on `Layered terminal art` is presentation-only:

- SetGate(index, gateLabel, destination, open): board row and portal energy visibility.
- SetStatusText(index, text): override/localise status.
- SetWayfinding(index, text): route signage.
- heading.text: board heading.
- SetFlag(colour, logoSprite): cloth tint and replaceable emblems; null hides logos.

Claude owns game state, destination rules, input, localisation and orchestration. No gameplay integration has been performed. Do not rebuild OfficeScene to adopt this art; agree an integration hook first.

LayeredHallRig exposes Evening (0–1), Sun Direction (-1–1), Look Left (0–27), Animate Portal and SetTime(float). This uses a constrained illustrated pan.

## Verification and limits

morning.png, evening.png, light-direction-test.png and left-window.png are Unity renders. The builder checks shader errors, no baked Unity lightmaps, display bindings and portal status switching. This does not test gameplay.

Lighting is simulated: approximate normal response, moving window bands, illuminated sconce glass and animated portal colour. Crevice shading and wear remain painted. Fixtures are sprites, not physically relightable metal geometry. The independently generated left and forward floor paintings are not a mathematically continuous mesh. This remains an art review scene.

Build with Tools > Terminal Art > Build Layered Lit Hall. The restricted local preview bridge accepts refresh then layered.

## Generation record

Built-in image generation used the preserved HallNeutral and HallLeftExtension as references. Prompts requested exact composition; architecture-only transparent layers with floor/signage/fixtures removed; separate worn ivory/burgundy/gold floor without cast shadows; and transparent cutouts of a portal frame, repeated sconce, modern station lockers and neutral logo-free flag. Selected outputs are saved in Assets/Art/Office/TerminalLayered/Textures. Earlier plates remain as source references.
