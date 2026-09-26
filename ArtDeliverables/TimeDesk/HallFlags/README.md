# Hall flags: actual 2D sprites

Installed all four flags in OfficeScene as SpriteRenderers using the authored ochre PNG's original alpha. The previous cloth MeshRenderer children remain preserved but inactive; metal hanging supports are retained. Sprite placement matches the original cloth's 1.664 m × 4.499 m visible bounds. No billboarding or 3D cloth simulation is added.

Validation: four assigned active sprites, four disabled old cloth objects, four retained supports; report passed. Play-mode screenshot confirms the sprites render in the game. Unity console had zero errors after the play check. Returned to Edit mode. The morning briefing covers part of the runtime screenshot, but all four flags are visible above it.

Run `Tools/Office Art/Install Hall Flag Sprites` to reinstall idempotently, or `Tools/Office Art/Validate Hall Flag Sprites` to check. Source bitmap is unmodified; Unity's Sprite rect ignores transparent margins. The built-in URP Sprite-Unlit shader supplies flat 2D rendering.

The game-wide palette and table material have not been changed by this operation. The latest ReStory-based colour image is a separate paint-over proposal in `PaletteExploration/ScreenStudies/Restory`.

