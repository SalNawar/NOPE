# Art request: the pet (for the ChatGPT art side)

> **Delivered (run 7, 2026-10-07):** files 1-8 arrived as coats instead: `Home/pet_<kind>_<coat>_<look>.png`, five coats per kind (the Home pet spec PS11), made in Canva and recoloured by `tools/art/pets/make_pets.py`. `ArtSlots.PetSprite` still tries `Home/pet_<kind>_<look>` after the coat's file, so a coat-less file 1-8 would show for a coat without its own picture. Files 9-18 are still open.

*2026-10-05 · the Home pet spec (`docs/superpowers/specs/2026-10-05-home-pet-design.md`) · until these arrive the game draws a stand-in in code (`PetStandIn`), so each file switches on by itself the moment it is dropped in place (the art slots: `ArtSlots.PetSprite`, `PetCorner`, `PetToy`, `OrderIcon`, `OrderBranch`).*

The run starts with the player adopting a **dog or a cat** and naming it. Each night at Home the player pays (or skips) its food, heating, electricity, TV and medicine; the pet's corner then shows it as tonight's care leaves it, the player pets it and plays with toys bought at the PC's Orders app. If it is left hungry, cold or very sick two nights running, the Animal Welfare Office rehomes it (the run's failure ending).

## Style

The game's cel style (see `docs/ART_ASSET_LIST.md`, "Style for all 2D UI and illustration art"): clean 2D anime line art, flat colour with one shade step, warm lamp light from the left, matching the Home background (`Assets/Art/Home/home_bg.png`). The world is a debt dystopia in 2150, gently absurd: the pet is the one soft thing in it. Nothing graphic: a sick pet looks poorly (droopy, under a blanket, a thermometer), never injured.

## Files

Put the sprites in `Assets/Art/UI/Resources/` under the path shown (PNG, transparent background unless noted). The importer sets them up as single sprites.

| # | File | Size | What |
|---|---|---|---|
| 1-4 | `Home/pet_dog_idle.png`, `pet_dog_happy.png`, `pet_dog_sad.png`, `pet_dog_sick.png` | 1024 × 1024 | A scruffy medium mongrel (sandy brown, one floppy ear up, one down), lying or sitting in three-quarter view facing left, the same pose and scale in all four so swaps do not jump. **Idle:** relaxed, eyes open. **Happy:** tail up mid-wag, eyes closed in a smile, tongue out. **Sad:** ears flat, head on paws, eyes up at the viewer. **Sick:** curled under a small blue blanket, ears down, a thermometer in its mouth or a hot-water bottle beside it. |
| 5-8 | `Home/pet_cat_idle.png`, `pet_cat_happy.png`, `pet_cat_sad.png`, `pet_cat_sick.png` | 1024 × 1024 | A slate-grey short-haired cat with a white chest and one white paw, loaf or sitting pose, three-quarter view facing left, same pose and scale in all four. **Idle:** loaf, half-lidded. **Happy:** kneading or upright with tail curled, eyes closed, purring lines. **Sad:** hunched small, ears sideways, big eyes. **Sick:** curled under the same blue blanket, ears down. |
| 9 | `Home/pet_corner.png` | 1100 × 1100, opaque | The corner of the clerk's flat where the pet sleeps: a worn cushion or basket on a stained floor, a radiator with a coin meter, a window with a sliver of the 2150 city, a bowl. Leave the middle third clear and fairly plain: the pet is drawn over it. |
| 10-13 | `Home/toy_toy_ball.png`, `toy_toy_rope.png`, `toy_toy_feather.png`, `toy_toy_squeaky.png` | 256 × 256 | The four toys as objects: a **Pre-owned Tennis Ball** (slightly bald), a **Rope of Regulation Length** (knotted, with a tiny official tag), a **Feather Wand**, a **Squeaky Auditor** (a rubber toy shaped like a stern little auditor with a clipboard). |
| 14-17 | `Orders/upgrade_toy_ball.png`, `upgrade_toy_rope.png`, `upgrade_toy_feather.png`, `upgrade_toy_squeaky.png` | 256 × 256 | The same four toys as the Orders app's node icons: simpler, white on transparent, like the other Orders icons. |
| 18 | `Orders/branch_toys.png` | 128 × 128 | The Orders tree's Toys band glyph: a ball (or a bone and a ball), white on transparent, matching `branch_desk`, `branch_interview`, `branch_portals`, `branch_contacts`. |

## Nice to have (not wired yet)

- An adoption-panel backdrop for the Title (the shelter's counter), 1000 × 720.
- A Welfare Office ending picture for `Ending_PetTaken` (an inspector's clipboard on the empty cushion, the pet's bowl left behind), 1920 × 1080 opaque, in the style of the other ending pictures.

## Checks before delivery

- Same pose and scale across a kind's four states (the game swaps them in place).
- No text in any picture (the game prints the pet's name and every label).
- Transparent backgrounds for 1-8 and 10-18.
