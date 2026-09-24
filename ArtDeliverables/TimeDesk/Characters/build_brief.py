"""Builds docs/CHARACTER_ART_BRIEF.md from the verified costume research."""
import json, shutil
from pathlib import Path

SCRATCH = Path(__file__).parent
REPO = Path(r"E:\unity\NOPE")
data = json.loads((SCRATCH / "costumes.json").read_text(encoding="utf-8"))
E = {(e["country"], e["era"]): e for e in data["entries"]}
MOTIFS = {m["country"]: m["motifs"] for m in data["futureMotifs"]}

COUNTRIES = ["egypt", "iraq", "greece", "italy", "china", "japan", "britain", "germany"]
CNAME = dict(egypt="Egypt", iraq="Iraq / Mesopotamia", greece="Greece", italy="Italy", china="China",
             japan="Japan", britain="Britain", germany="Germany")
ERAS = [("ancient", "Ancient", "before 500 CE"), ("medieval", "Medieval", "500 to 1450"),
        ("earlymodern", "Early modern", "1450 to 1750"), ("industrial", "Industrial", "1750 to 1900"),
        ("modern", "Modern", "1900 to 2000")]

def none(t): return t.strip().lower().startswith("none")
def year(y): return f"{-y} BCE" if y < 0 else f"{y} CE" if y < 1000 else str(y)

def pair_files(e):
    c, era = e["country"], e["era"]
    files = []
    for g, key in (("m", "male"), ("f", "female")):
        files.append(f"outfit_{g}_{c}_{era}.png")
        files.append(f"hair_{g}_{c}_{era}.png")
        if g == "m" and not none(e[key]["facialHair"]):
            files.append(f"facialhair_m_{c}_{era}.png")
        if not none(e[key]["headwear"]):
            files.append(f"headwear_{g}_{c}_{era}.png")
        files.append(f"accessory_{g}_{c}_{era}.png")
    return files

def pair_block(e):
    m, f = e["male"], e["female"]
    lines = [f"PAIR: {e['country']}_{e['era']} ({e['displayName']}, {year(e['year'])})",
             f"Place and time: {e['moment']}",
             "",
             "MAN",
             f"- Outfit: {m['outfit']}",
             f"- Hair: {m['hair']}",
             f"- Facial hair: {m['facialHair']}",
             f"- Headwear: {m['headwear']}",
             f"- Accessory: {m['accessory']}",
             "",
             "WOMAN",
             f"- Outfit: {f['outfit']}",
             f"- Hair: {f['hair']}",
             f"- Headwear: {f['headwear']}",
             f"- Accessory: {f['accessory']}",
             "",
             f"MUST READ AT A GLANCE: {e['signature']}",
             f"DO NOT DRAW: {'; '.join(e['avoid'])}"]
    return "\n".join(lines)

counts = {}
out = []
w = out.append

w("""# Time Sorter: Character Art Brief for ChatGPT

Everything ChatGPT needs to draw every character in the game. Work through it batch by batch. Each batch ends with **send to Claude**: Claude cuts the images out, checks that they line up, and tests them in Unity before you start the next batch.

Files that go with this brief:

- `ArtDeliverables/TimeDesk/Characters/character_guide_1024x1536.png`: the figure guide. Attach it in Batch 1.
- `mannequin_m.png` and `mannequin_f.png`: Claude makes these from your approved Batch 1 figures. Attach one in every later request.
- `ArtDeliverables/TimeDesk/Characters/costume_research.json`: the full research behind every look (sources, dates, notes). You don't need it for ChatGPT; it feeds the game's world data.

## 1. How characters work in the game

- **Two kinds of character.** *Generated* travellers are built from layers, so the game can make thousands of different people. *Premade* characters (famous or story characters) are drawn whole.
- **Seven layers**, stacked in this order: body, outfit, head, facial hair, hair, headwear, accessory.
- **Every layer is a full 1024 x 1536 image with the figure in the same spot**, so the game stacks them with no adjusting. The passport photo is a crop of the same stack, so it always matches the traveller at the desk.
- **Every country and era has its own look.** Honest travellers wear only their own. A liar's disguise leaks one item from their real home, like a British top hat on someone claiming to be from Edo Japan. That's why every look below has a "must read at a glance" item.
- **Skin and hair colour never give anyone away.** The game picks them to fit where the traveller *claims* to be from. Only culture (clothes, hairstyle, headwear, accessories) can be a clue.

## 2. Why green and magenta

ChatGPT can't line up separate transparent images reliably. So every piece is drawn **on top of a flat magenta mannequin, on a flat green background**. Claude's script then removes the green and magenta, leaving just the piece, already in the right place. That's why characters must never use pure bright green or pure magenta.

## 3. Setup in ChatGPT (once)

1. Create a ChatGPT **Project** called "Time Sorter Characters".
2. Paste **Block A** (below) into the Project's instructions.
3. Upload the figure guide to the Project files. After Batch 1, also upload the two mannequins and your approved pilot images, so ChatGPT keeps the same style.
4. Always ask for the **tall portrait size (1024 x 1536)**.
5. Start a new chat for each era. Long chats drift in style.

### Block A: paste into the Project instructions

```
You are drawing characters for "Time Sorter", a 2D game in the style of Papers, Please. The player is a clerk at a time-travel border desk, and travellers from many countries and eras come to the desk. Every character is built from separate layers (body, head, hair, facial hair, outfit, headwear, accessory) that the game stacks on top of each other, so every layer must line up exactly.

STYLE
- 2D painted illustration with a slightly muted palette and soft painterly shading.
- A clean dark outline (2-3 px) around every piece.
- Light comes from the top-left. Shade inside each piece only: no cast shadows, no ground shadow, no glow.
- Realistic adult proportions. Not chibi, not anime, not cartoon.
- Front view: standing straight, facing the viewer, arms relaxed slightly away from the body, hands open, neutral expression, eyes looking at the viewer.
- Respectful and historically grounded. No caricature of any people or culture, and no likeness of any real person.
- Civilian clothing only. No military uniforms, weapons, flags, insignia, political or hate symbols, and no religious vestments.

CANVAS (every image)
- Portrait, 1024 x 1536 px.
- Background: flat pure green #00FF00 everywhere outside the figure. No gradient, texture, floor, shadow or vignette.
- Match the attached guide or mannequin exactly: same top of head, chin, shoulders, waist, hips, knees and feet, and the same centre line. Never move, resize or re-pose the figure.
- Keep everything inside the guide's blue safe-area box. Tall hats and hair may rise into the headroom at the top.
- Never use pure bright green (#00FF00) or pure magenta (#FF00FF) in the character. Use muted greens and purples instead.
- No text, letters, numbers, logos, watermarks or frames anywhere.

MANNEQUIN RULE
- When I attach a magenta mannequin, keep it exactly as it is: flat magenta, same shape, same position, same darker-magenta face marks. Draw ONLY the item I ask for, on top of it, fitted to it. Do not draw skin, a face, hair or anything else I did not ask for.

HAIR COLOUR
- Draw all natural hair and facial hair in medium brown; the game recolours it. Wigs are costume: draw a wig in the colour I describe.

One image per reply unless I ask for more.
```

## 4. The layers

| Layer | What it contains | What it must NOT contain | How many |
|---|---|---|---|
| Body | Figure without the head: neck up to the chin line, arms, open hands, legs, bare feet, plain light-grey undergarment | Head, hair, real clothes | 2 genders x 5 skin tones |
| Head | Bald head with ears and face, in the body's skin tone | Hair, beard, anything below the chin | 2 genders x 5 skin tones x 4 faces |
| Outfit | All clothing and footwear for one country and era | Face, hair, headwear, the accessory | 1 per gender per country per era |
| Facial hair | Beard, moustache or whiskers only | Face, hair | Men only, when the look has one |
| Hair | Hairstyle only, fitted to the bald head (shaved parts stay magenta) | Face, headwear | 1 per gender per country per era |
| Headwear | Hat, veil, headdress or cap, sitting over the hair; eyes, nose and mouth stay visible | Hair, face | When the look has one |
| Accessory | One **worn** item: jewellery, collar, belt pouch, satchel, sash, shawl, glasses, pin | Anything held in the hands | 1 per gender per country per era |

**Skin tones:** 1 very light, 2 light, 3 medium olive, 4 brown, 5 deep brown.
**Faces:** a = young adult (20s), b = a different young adult, c = middle-aged (40s to 50s), d = elderly (60s and up).
Grey hair for older faces, plus black, blond and other colours, are made by Claude's script from your medium-brown hair. You never draw colour variants.

## 5. File names and where to save them

Save every image with **exactly** the listed name into `ArtDeliverables/TimeDesk/Characters/Raw/<batch folder>/`, as the green-and-magenta original. Claude makes the cut-out versions.

- Countries: `egypt iraq greece italy china japan britain germany`
- Eras: `ancient medieval earlymodern industrial modern future`
- Gender: `m` or `f`
- Examples: `base_f_skin1_facea.png`, `head_m_skin4_facec.png`, `outfit_f_china_medieval.png`, `hair_m_japan_earlymodern.png`, `facialhair_m_iraq_ancient.png`, `headwear_f_britain_industrial.png`, `accessory_m_egypt_ancient.png`

## 6. The prompts

Replace the parts in [square brackets]. For each country and era, paste its **PAIR block** first (they're in the era sections below), then use prompts 6.4 to 6.8 for each image in its file list.

### 6.1 Base figure (Batch 1 and Batch 2)

```
Attached: the Time Sorter figure guide.
Draw a BASE FIGURE: a [man / woman] in their 20s, skin tone [1 very light], bald (no hair at all, smooth scalp), ears visible, neutral expression, looking at the viewer.
Follow the guide exactly: same pose, proportions and landmark heights. The head runs from the top of the head at y=260 to the chin at y=424.
Clothing: only [plain light-grey knee-length shorts / a plain light-grey sleeveless top and plain light-grey knee-length shorts]. Bare feet.
Remove all guide lines, labels and the grey silhouette. Flat pure green #00FF00 background.
```

### 6.2 Skin tone variant (Batch 2)

```
Attached: my approved base figure.
Keep everything exactly the same (pose, face, proportions, position, clothing, background) and change ONLY the skin tone to [2 light / 3 medium olive / 4 brown / 5 deep brown].
```

### 6.3 Extra face (Batch 2)

```
Attached: mannequin_[m / f].png and my approved base figure with skin tone [N].
Keep the magenta mannequin exactly as it is. Draw ONLY a HEAD filling the mannequin's head shape (top of head y=260, chin y=424), with eyes, nose and mouth on the darker magenta face marks.
Same skin tone as the attached base figure, bald, ears visible, neutral expression, looking at the viewer.
Face: [b: a different young adult in their 20s / c: middle-aged, 40s to 50s, a few lines / d: elderly, 60s and up, wrinkles, lighter eyebrows].
Draw nothing below the chin.
```

### 6.4 Outfit

```
Attached: mannequin_[m / f].png.
From the PAIR block, draw ONLY the [man's / woman's] OUTFIT (all clothing and footwear) on the magenta mannequin, fitted to its body.
Do not draw the face, hair, facial hair, headwear or accessory. The head stays magenta and uncovered, and any part of the body the outfit does not cover stays magenta.
```

### 6.5 Hair

```
Attached: mannequin_[m / f].png.
From the PAIR block, draw ONLY the [man's / woman's] HAIR on the mannequin's bald head, in medium brown (unless the PAIR block says it is a wig). Leave any shaved parts of the scalp magenta.
Hair may fall over the shoulders and back but must never cover the eyes, nose or mouth. Draw nothing else.
```

### 6.6 Facial hair

```
Attached: mannequin_m.png.
From the PAIR block, draw ONLY the man's FACIAL HAIR on the mannequin's face, using the darker magenta face marks to place it, in medium brown. Leave the rest of the face magenta. Draw nothing else.
```

### 6.7 Headwear

```
Attached: mannequin_[m / f].png.
From the PAIR block, draw ONLY the [man's / woman's] HEADWEAR, worn on the mannequin's head the way it would sit over hair. The eyes, nose and mouth stay fully visible. Draw nothing else.
```

### 6.8 Accessory

```
Attached: mannequin_[m / f].png.
From the PAIR block, draw ONLY the [man's / woman's] ACCESSORY in its natural worn position on the body. It is worn, never held. Draw nothing else.
```
""")

# ---- Batch 1: pilot
pilot = E[("britain", "industrial")]
pilot_files = pair_files(pilot)
counts["Batch 1: pilot"] = 2 + len(pilot_files)
w(f"""## 7. Batch 1: pilot ({2 + len(pilot_files)} images)

This small batch proves the pieces stack before anyone makes hundreds of images. Folder: `Raw/batch1-pilot/`.

**Step 1.** Use prompt 6.1 twice, for skin tone 1, face a:

- `base_m_skin1_facea.png`
- `base_f_skin1_facea.png`

**Send to Claude.** Claude splits each figure into a body and a head, and sends back `mannequin_m.png` and `mannequin_f.png`. Upload both to the Project files.

**Step 2.** Paste this PAIR block, then use prompts 6.4 to 6.8 for each file below it:

```
{pair_block(pilot)}
```

Files: {", ".join(f"`{f}`" for f in pilot_files)}

**Send to Claude.** Claude stacks everything in Unity. If the pieces line up, go on to Batch 2. If not, we fix the prompts before making more.
""")

# ---- Batch 2: base
counts["Batch 2: skin tones and faces"] = 8 + 30
w("""## 8. Batch 2: skin tones and faces (38 images)

Folder: `Raw/batch2-base/`.

- **8 base figures** with prompt 6.2, from your approved skin-1 figures: `base_[m/f]_skin[2-5]_facea.png`
- **30 heads** with prompt 6.3, faces b, c and d for every skin tone and both genders: `head_[m/f]_skin[1-5]_face[b/c/d].png`

**Send to Claude.**
""")

# ---- Era batches
batch_no = 3
for era_id, era_name, span in ERAS:
    era_entries = [E[(c, era_id)] for c in COUNTRIES]
    files = [f for e in era_entries for f in pair_files(e)]
    if era_id == "industrial":
        files = [f for f in files if f not in pilot_files]
    label = f"Batch {batch_no}: {era_name}"
    counts[label] = len(files)
    note = "; Britain was done in the pilot" if era_id == "industrial" else ""
    w(f"""## {batch_no + 6}. Batch {batch_no}: {era_name} era, {span} ({len(files)} images{note})

Folder: `Raw/{era_id}/`. Start a new chat for this era. For each country, paste its PAIR block, then make each file in its list.
""")
    for e in era_entries:
        if era_id == "industrial" and e["country"] == "britain":
            w(f"### {CNAME[e['country']]}: {e['displayName']}\n\nDone in the pilot (Batch 1).\n")
            continue
        fl = pair_files(e)
        w(f"""### {CNAME[e['country']]}: {e['displayName']}

```
{pair_block(e)}
```

Files: {", ".join(f"`{f}`" for f in fl)}
""")
    w("**Send to Claude.**\n")
    batch_no += 1

# ---- Future
future_files = ["outfit_m_future_neutral.png", "outfit_f_future_neutral.png"] + \
    [f"outfit_{g}_future_{c}.png" for c in COUNTRIES for g in ("m", "f")] + \
    ["hair_m_future_neutral.png", "hair_f_future_neutral.png"]
counts[f"Batch {batch_no}: the Future"] = len(future_files)
future_blocks = []
for c in COUNTRIES:
    future_blocks.append(f"""### Future shaped by {CNAME[c]}

```
PAIR: future_{c} (the Future, after {CNAME[c]} came to dominate history)
Take the neutral Future outfits (same practical cut, same fit) and rework them with these motifs, for both the man and the woman:
{MOTIFS[c]}
Keep it wearable civilian clothing. No flags, emblems, insignia or readable text.
```

Files: `outfit_m_future_{c}.png`, `outfit_f_future_{c}.png`
""")
w(f"""## {batch_no + 6}. Batch {batch_no}: the Future ({len(future_files)} images)

The Future is the player's own time. Its look changes with history: if one culture comes to dominate the past, the Future dresses in its style. Folder: `Raw/future/`. Use prompts 6.4 and 6.5.

### Neutral Future (before any culture dominates)

```
PAIR: future_neutral (the Future, the player's own time)

MAN
- Outfit: clean, practical civilian clothes in soft technical fabrics: a hip-length jacket with a high soft stand collar and simple geometric seams over a plain fitted top, slim straight trousers, and flat low-profile shoes. Muted slate grey, sand and teal.
- Hair: short, neat, textured crop.

WOMAN
- Outfit: the same practical future style: a long panelled coat-dress with a high soft stand collar and simple geometric seams, over slim trousers, with flat low-profile shoes. Muted slate grey, sand and teal.
- Hair: sleek low bun at the nape.

MUST READ AT A GLANCE: the high soft stand collar and clean geometric seams in slate, sand and teal.
DO NOT DRAW: glowing armour, helmets, visors over the face, weapons, robot parts, logos or readable text.
```

Files: `outfit_m_future_neutral.png`, `outfit_f_future_neutral.png`, `hair_m_future_neutral.png`, `hair_f_future_neutral.png`

""" + "\n".join(future_blocks) + "\n**Send to Claude.**\n")
batch_no += 1

# ---- Premade
w(f"""## {batch_no + 6}. Batch {batch_no}: premade characters

Premade characters are drawn **whole**, not in layers: one finished image per expression, on the same figure guide and green background, so they stand at the desk exactly where generated travellers do.

- Four expressions each: `neutral`, `happy`, `angry`, `worried`.
- Name: `premade_[character id]_[expression].png`. Folder: `Raw/premade/`.

```
Attached: the Time Sorter figure guide.
Draw a complete PREMADE CHARACTER, following the guide exactly (pose, proportions, landmark heights), with hair, clothing, headwear and accessories all included: [description of the character, their country and era, and their look].
Neutral expression. Flat pure green #00FF00 background. Remove all guide lines and labels.
```

Then, for each other expression:

```
Attached: my approved neutral image of this character.
Keep everything exactly the same (pose, clothing, position, background) and change ONLY the facial expression to [happy / angry / worried].
```

**Who they are is waiting on one decision:** whether premade characters are the real famous people from the country table (Hatshepsut, Archimedes, Ada Lovelace and so on) or original characters inspired by them. The character list gets added here once that's decided.
""")
batch_no += 1

# ---- Checklist + totals
total = sum(counts.values())
w("""## Checklist before you send a batch to Claude

- Every file is 1024 x 1536 and named exactly as listed.
- The background is flat green, and the magenta mannequin is untouched wherever the new piece doesn't cover it.
- Nothing crosses the guide's blue safe-area box.
- No text, logos, flags or insignia anywhere.
- The "must read at a glance" item is clearly visible.

## Totals

| Batch | Images |
|---|---|
""" + "\n".join(f"| {k} | {v} |" for k, v in counts.items()) + f"""
| Premade characters | 4 per character |
| **Total, not counting premade** | **{total}** |

Batch 1 is the only one that has to come first. After Batch 2, the era batches can be done in any order.
""")

(REPO / "docs" / "CHARACTER_ART_BRIEF.md").write_text("\n".join(out), encoding="utf-8")
shutil.copy(SCRATCH / "costumes.json", REPO / "ArtDeliverables/TimeDesk/Characters/costume_research.json")
print("written; counts:", counts, "total", total)
