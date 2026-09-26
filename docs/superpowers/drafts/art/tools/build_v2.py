"""Builds art/CHARACTER_ART_BRIEF_v2.md and art/coverage.json from art/tools/wardrobe_v2.json.
Run data_v2.py first. Key grammar = piece-4 spec LookKeys (section 2.3) + piece-5 R18 (artNation).
v2.1 (2026-09-25): revised for the 3D office (the desk-view sizes below are measured in the art office at main d5844d0).
v2.2 (2026-09-25): the ReStory style (cute 2D anime-style faces, flat cel colours) for both kinds of character, the
layered travellers and the premades; the head size and body proportions stay the guide's (LookCanvas)."""
import json, sys
from pathlib import Path

sys.stdout.reconfigure(encoding="utf-8")
TOOLS = Path(__file__).resolve().parent
ART = TOOLS.parent
W = json.loads((TOOLS / "wardrobe_v2.json").read_text(encoding="utf-8"))
P = {p["id"]: p for p in W["places"]}
FUT = {f["country"]: f for f in W["future"]}
# v2.3 (2026-09-26): the present's clothes (the neutral Future outfits become game files) and the 2150 accessory kit.
PRESENT_CLOTHES = W["presentClothes"]
KIT = W["presentKit"]
# v2.1 counts for the change section: MUST READ lines rewritten, generated pairs under the two new women's headwear items.
N_MUST_V21 = sum(1 for e in W["editLog"] if "MUST READ names only" in e["why"])
N_HID_V21 = sum(1 for x in W["confusableHidden"] if x["gender"] == "f" and x["a"] in ("japan_earlymodern", "china_modern"))

COUNTRIES = ["egypt", "iraq", "greece", "italy", "china", "japan", "britain", "germany"]
CNAME = dict(egypt="Egypt", iraq="Iraq / Mesopotamia", greece="Greece", italy="Italy", china="China",
             japan="Japan", britain="Britain", germany="Germany")
GENDERS = ("m", "f")
GWORD = {"m": "man", "f": "woman"}
COLOURS = ["black", "brown", "blond", "red", "grey"]
SKINS = [1, 2, 3, 4, 5]
FACES = ["a", "b", "c", "d"]
EXPRESSIONS = ["neutral", "happy", "angry", "worried"]
# v2.2: how faces a-d differ in the anime style, which tends to give everyone one face. They keep face a's head
# outline and feature positions (the beards, caps and glasses are fitted to them: Batch 2), so they differ inside it.
FACE_AGE = {"a": "a young adult in their 20s", "b": "a different young adult in their 20s",
            "c": "middle-aged, 40s to 50s", "d": "elderly, 60s and up"}
FACE_LOOK = {"a": "soft, round cheeks and chin; large, round eyes; gently curved brows; a small, short nose",
             "b": "slimmer cheeks and a more defined chin; narrower, longer eyes; straight, thicker brows; a longer, "
                  "straighter nose",
             "c": "fuller cheeks and a firmer, squarer chin; steady eyes with a small line at each outer corner; heavier, "
                  "lower brows; a broader nose; a few lines on the forehead and beside the mouth",
             "d": "softer, slightly hollow cheeks and a softer jawline; smaller eyes under heavier lids, with wrinkles at "
                  "the corners; thin, lighter eyebrows; a longer nose; wrinkles on the forehead and cheeks"}
def face_drawn(v): return f"{FACE_AGE[v]}: {FACE_LOOK[v]}"
SLOTWORD = {"Outfit": "outfit", "Hair": "hair", "FacialHair": "facial hair", "Headwear": "headwear", "Accessory": "accessory"}
# Skin swatches (review 2 finding 41): ChatGPT gets them in prompt 9.2, Claude recolours bodies 2-5 to them.
SKIN_WORD = {1: "very light", 2: "light", 3: "medium olive", 4: "brown", 5: "deep brown"}
SKIN_HEX = {1: "#F1D3C0", 2: "#E0B394", 3: "#C39A6B", 4: "#94653F", 5: "#5C3A24"}
OUTLINE_HEX = "#3B2A20"
SIZE_LINE = "Portrait, 1024 x 1536, the same framing as the attached image."
STYLE_ATTACH = "style_card.png (STYLE REFERENCE ONLY: match its line weight and shading; do not copy any clothing)"
OFFICE_REF = "office_style_reference.png"
OFFICE_ATTACH = (f"{OFFICE_REF} (STYLE REFERENCE ONLY: the game's office; match its colour range and contrast, never draw "
                 "the room)")

# The desk view (v2.1), measured in the art office (main d5844d0): the office camera at (0, 2.16, -2.62), pitched 10 degrees
# down, vertical field of view 55 degrees; the traveller's feet at the default Anchor_Traveller (0, 0, 1.6), 1.8 m from
# the soles to the top of the head (LookCanvas y 1490 to 260). Projected landmarks match p9_day2_bubble_hieroglyphs.png
# within a pixel (hat top y 407, chin 505, sleeve ends 613). The vertical field of view is fixed, so sizes scale with the
# screen height only.
DESK = {
    1080: dict(head=58, face=44, head_to_shoulders=85, shoulders=121, px=0.36, photo_desk="48 x 25", photo_pc="60 x 75",
               photo_held="95 x 119", photo_beside="134 x 167"),
    720: dict(head=39, face=30, head_to_shoulders=57, shoulders=80, px=0.24, photo_desk="32 x 17", photo_pc="40 x 50",
              photo_held="63 x 79", photo_beside="89 x 111"),
}
DESK_VISIBLE_TO = 760   # canvas y where the NEXT sign's top cuts the figure (the waist)
READ_ZONE_TO = 630      # canvas y of mid-chest: held papers (piece 10) cover the figure's sides below about here

# Shared by Block A (layers) and Block P (premades), so the two never drift apart. v2.2 (2026-09-25): the ReStory style,
# the same for both kinds of character; the head size and body proportions stay the guide's (LookCanvas).
STYLE_BULLETS = f"""- Cute, soft 2D anime-style characters, like the customers of a cozy shop-counter game: expressive anime eyes (larger than realistic, with one simple highlight), a small, simple nose and mouth, clean rounded face shapes, and hair drawn in clean stylised shapes and locks. Every character is an adult who looks their age: never chibi, never childlike.
- The head size and body proportions are fixed by the attached guide, mannequin or base figure (adult proportions, about seven and a half heads tall; see CANVAS). The style changes only the face and the rendering: never enlarge the head, and never shorten or lengthen the body.
- Faces stay individual: give each face the face shape, eye shape, brows and nose its prompt describes, never one anime face for everyone. Paint every skin tone exactly as given, never lighter. Every culture is drawn in this same style: never a caricature, never an exoticised version of a people.
- One cast: the game's layered travellers and its named historical characters are drawn in two separate Projects but must look like one cast, with the same eyes, line, shading and colour treatment, so neither kind ever stands out from the other at the desk.
- Everything reads small: in the game the head is only about {DESK[1080]['head']} px tall on screen, so keep the eyes, brows, mouth, hair shapes and headwear simple, bold and clear.
- Flat cel colours: each colour area has ONE hard-edged shade tone (the same hue, about 20% darker, with a crisp edge and no soft blending) and at most one small highlight. No fabric grain, brush or paper texture, noise, gradient or photographic detail. Draw patterns (stripes, checks, borders, embroidery) as clean, bold, flat shapes.
- A clean, even dark-brown outline ({OUTLINE_HEX}, 2-3 px) around every piece and its main folds.
- Neutral, even lighting: plain white light from the front. Shade only to show form (under the chin, inside folds, under a brim), the same on both sides. No light direction, no rim light, no glow, no cast shadow, no ground shadow, no warm or cool tint.
- Gently muted, natural period colours. Nothing neon.
- No manga symbols: no sweat drops, anger marks, blush lines, sparkles, tears or speed lines. A face shows feeling only through its eyes, brows and mouth.
- Front view: standing straight, facing the viewer, arms relaxed slightly away from the body, hands open and empty, neutral expression, eyes looking at the viewer.
- When I attach a STYLE REFERENCE image, match its line weight, shading and pattern scale, and never copy its clothing."""
CANVAS_BULLETS = """- Portrait, 1024 x 1536 px. The figure stays exactly where the attached guide, mannequin or base figure puts it: same top of head, chin, shoulders, waist, hips, knees and feet, same centre line. Never move, resize, turn or re-pose it.
- Background: one flat pure green #00FF00 everywhere outside the figure. No gradient, texture, floor, shadow, vignette or frame. Never draw a checkerboard or "transparent" pattern.
- Leave green on all four sides: nothing touches an edge. Tall hats and hair may rise into the space above the head.
- Never use bright green, lime, magenta, pink, purple or violet anywhere in the clothing, hair or items, not even muted. Where a look asks for purple, paint deep wine red. Where it asks for pink or rose, paint dusty coral or salmon. Where it asks for green, paint dark olive, moss or bottle green. Paint jade as a dark, dull grey-green stone with a strong dark outline. Natural skin and lip colours on faces and bodies are fine.
- Paint every fabric fully opaque, even veils, gauze, muslin and stockings, and never cut holes through it. Magenta, green or skin must never show through anything. Where I say "light" or "fine" fabric, paint a thin, light, solid fabric.
- No text, letters, numbers, logos, watermarks or signatures anywhere. Bands called "tiraz" or "script" are abstract embroidered patterns, never real or fake letters. Coins are plain discs with no face, letters or marks."""
# Shared by Block A and Block P (v2.1): the figures stand in the 3D office.
GAME_BULLETS = f"""- The characters are flat 2D figures that stand behind the clerk's desk in the 3D office, facing the player; the game places them in the room and tones them to its light, so draw them flat and evenly lit.
- The desk hides every figure below the waist, and the head shows small on screen. So everything that tells where a character comes from (headwear, hair, face, beard, collar, necklace, shoulders and upper chest) must be bold and clear. Still draw the whole figure, down to the feet.
- When I attach {OFFICE_REF}, it is a STYLE REFERENCE ONLY: a screenshot of the game's office. Match its colour range and contrast so the character sits in that room. Never draw the room, the desk, any furniture or the room's lighting: the background stays flat green."""
RULE_BULLETS = """- Respectful and historically grounded. No caricature or stereotype of any people or culture{likeness}.
- Civilian clothing only. No military uniforms or armour, weapons, flags, insignia, badges, royal regalia, political, regime or hate symbols, and no religious vestments or holy symbols."""

def yr(y): return f"{-y} BCE" if y < 0 else (f"{y} CE" if y < 1000 else str(y))

# ------------------------------------------------------------------ keys (LookKeys grammar)
def body_key(g, s): return f"body_{g}_skin{s}"
def head_key(g, s, v): return f"head_{g}_skin{s}_face{v}"
def premade_key(pid, ex): return f"premade_{pid}_{ex}"

def garment_keys(place_id, g):
    """Processed keys for one place and gender (LookKeys.Required, one gender)."""
    p = P[place_id]; w = p["wardrobe"][g]; nation, era = p["country"], p["era"]
    out = [f"outfit_{g}_{nation}_{era}"]
    cols = [None] if w["wig"] else COLOURS
    for c in cols:
        out.append(f"hair_{g}_{nation}_{era}" + (f"_{c}" if c else ""))
    if w["back"]:
        for c in cols:
            out.append(f"hairback_{g}_{nation}_{era}" + (f"_{c}" if c else ""))
    if w["present"]["facialHair"]:
        out += [f"facialhair_{g}_{nation}_{era}_{c}" for c in COLOURS]
    if w["present"]["headwear"]:
        out.append(f"headwear_{g}_{nation}_{era}")
    if w["present"]["accessory"]:
        out.append(f"accessory_{g}_{nation}_{era}")
    return out

def covers_hair(place_id, g):
    return "Hair" in P[place_id]["wardrobe"][g]["covers"]

def raw_files(place_id, g):
    """What ChatGPT draws (raw, green + magenta) for one place and gender, with the keys each makes."""
    p = P[place_id]; w = p["wardrobe"][g]; nation, era = p["country"], p["era"]
    base = f"{nation}_{era}"
    cols = [None] if w["wig"] else COLOURS
    res = [(f"outfit_{g}_{base}.png", [f"outfit_{g}_{base}"])]
    hk = [f"hair_{g}_{base}" + (f"_{c}" if c else "") for c in cols]
    if w["back"]:
        hk += [f"hairback_{g}_{base}" + (f"_{c}" if c else "") for c in cols]
    res.append((f"hair_{g}_{base}.png", hk))
    if w["present"]["facialHair"]:
        res.append((f"facialhair_{g}_{base}.png", [f"facialhair_{g}_{base}_{c}" for c in COLOURS]))
    if w["present"]["headwear"]:
        res.append((f"headwear_{g}_{base}.png", [f"headwear_{g}_{base}"]))
    if w["present"]["accessory"]:
        res.append((f"accessory_{g}_{base}.png", [f"accessory_{g}_{base}"]))
    return res

def future_keys(country, g):
    out = [f"outfit_{g}_{country}_future"]
    return out

NEUTRAL_FUTURE_KEYS = [f"hair_{g}_neutral_future_{c}" for g in GENDERS for c in COLOURS] + \
                      [f"facialhair_m_neutral_future_{c}" for c in COLOURS]
PRESENT_OUTFIT_KEYS = [f"outfit_{g}_neutral_future" for g in GENDERS]
KIT_KEYS = [f"accessory_{g}_neutral_future_{k['variant']}" for g in GENDERS for k in KIT]

# ------------------------------------------------------------------ premades (AMENDMENT A1 cast)
PREMADES = [
    dict(id="senenmut", name="Senenmut", g="m", place="egypt_ancient", born="14 Mar 1505 BCE", age=35, face="c", skin=4,
         role="artist", sched="forced: day 1, slot 3 (story)", batch="3", true_place="",
         note="Steward of the Pharaoh's household and architect of her temple.",
         look="Senenmut, steward of the royal household and architect of the queen's temple at Deir el-Bahari. A lean man of about 35 with a calm, clever face, clean-shaven. A shoulder-length black bobbed wig with a short straight fringe. A calf-length wrap-around kilt of fine white linen, knotted at the waist, with a finely pleated front panel; bare chest; a broad wesekh collar of blue-leaning turquoise, lapis-blue, carnelian-red and gold beads with teardrop pendants, covering the upper chest and shoulders; plain gold bands on the upper arms; papyrus sandals.",
         avoid="nemes headcloth, cobra (uraeus), any crown, false beard, crook and flail; a long pleated over-robe; anything held in the hands"),
    dict(id="socrates", name="Socrates", g="m", place="greece_ancient", born="6 Jun 470 BCE", age=40, face="c", skin=3,
         role="wanderer", sched="forced: day 2, slot 6 (story); pooled day 3", batch="3", true_place="italy_ancient (an impostor)",
         note="Philosopher. Known to question officials at length.",
         look="Socrates of Athens as his friends described him, about 40: stocky, with a snub nose, wide-set prominent eyes, full lips, a high balding forehead with curly dark-brown hair at the sides and back, and a full, bushy dark-brown beard. He wears only one plain, well-worn himation of coarse undyed brown-grey wool (a tribon) wrapped round the body and over the left shoulder, leaving the right shoulder and arm bare, with no tunic under it. On his head, a petasos: a flat, wide-brimmed traveller's hat of undyed tan felt with a low crown, tilted back with the brim turned up at the front so the face is fully clear. Barefoot. Warm, alert and dignified, never a caricature.",
         avoid="a Roman toga or any Roman item (in the game he is an impostor, but his art shows the Socrates he claims to be); a short chlamys cloak; philosopher props, scrolls, anything held"),
    dict(id="aspasia", name="Aspasia", g="f", place="greece_ancient", born="12 Sep 470 BCE", age=40, face="c", skin=3,
         role="diplomat", sched="pooled days 2-3", batch="6", true_place="",
         note="Teacher of rhetoric from Miletus. Keeps the company of Pericles.",
         look="Aspasia of Miletus, a learned woman of Periclean Athens, about 40, with an intelligent, composed face. Centre-parted wavy dark hair gathered into a patterned cloth sakkos snood that covers the crown and back of the head, a few curls at the temples. An ankle-length Ionic chiton of fine crinkled linen with short sleeves pinned along the upper arms, girdled and bloused at the waist; over it a saffron himation with a woven meander border wrapped diagonally over the left shoulder. Large gold disc earrings and a simple gold necklace. Plain strapped sandals.",
         avoid="seductive or 'courtesan' styling, goddess styling, a Roman stola"),
    dict(id="banzhao", name="Ban Zhao", g="f", place="china_ancient", born="3 Feb 45", age=60, face="d", skin=2,
         role="scientist", sched="pooled days 2-3", batch="6", true_place="",
         note="Historian of the Han court. Completed the Book of Han.",
         look="Ban Zhao, historian and teacher at the Eastern Han court, about 60: a dignified elderly scholar with a kind, firm face and grey hair drawn smoothly back into a low looped bun at the nape, held with one plain silver hairpin. A floor-length quju wrap robe closed with the wearer's left panel over the right (a 'y' at the throat), layered collars with the innermost white, in sober deep brown or dark red silk with broad black cloud-scroll borders; huge sleeves that bag below the arm and narrow at the wrist; a dark cloth sash. A jade bi-disc pendant: a flat round disc of dark, dull grey-green jade with a hole in the centre, as wide as the palm, hanging high on the chest (its centre about a hand's width below the collarbones) on its own red silk cord round the neck.",
         # v2.1: raised from mid-chest with the china_ancient women's leak item (data_v2.py section 2c).
         avoid="court rank insignia, phoenix crowns, books or brushes in the hands"),
    dict(id="arib", name="Arib al-Ma'muniyya", g="f", place="iraq_medieval", born="20 May 797", age=33, face="b", skin=3,
         role="artist", sched="pooled days 2-3", batch="6", true_place="",
         note="Singer, poet and composer of the caliph's court.",
         look="Arib al-Ma'muniyya, celebrated singer, poet and composer of Abbasid Baghdad, early 30s, confident and elegant. A long-sleeved, ankle-length saffron silk qamis with gold-embroidered tiraz bands (abstract pattern, no letters) at the upper arms, full sirwal trousers gathered at the ankle, and a light dusty-coral mantle over the shoulders only. An opaque light khimar veil over the black hair, bound at the brow by an 'isaba headband of small abstract gold patterns (no letters), with two glossy black S-shaped side-curls on the cheeks. Gold earrings and a choker of large pearls.",
         avoid="a face veil, real or fake Arabic letters, a musical instrument in the hands",
         review="Orientalist or 'harem' styling"),
    dict(id="khwarizmi", name="Muhammad al-Khwarizmi", g="m", place="iraq_medieval", born="3 Apr 780", age=50, face="c", skin=3,
         role="scientist", sched="pooled days 2-3", batch="6", true_place="",
         note="Scholar of the House of Wisdom. Writes on calculation.",
         look="Muhammad ibn Musa al-Khwarizmi, scholar of the House of Wisdom in Baghdad, about 50, with a thoughtful face and a full, neatly rounded dark beard and moustache. A tall, stiff black qalansuwa cap wound at its base with a white turban; a front-opening, calf-length black durra'a robe over a pale linen qamis, with broad gold tiraz bands (abstract strokes and knots, no letters) around the upper sleeves; a dark taylasan scholar's shawl over both shoulders with its ends falling down the front; soft leather boots.",
         avoid="astrolabes, books or scrolls in the hands, real or fake Arabic letters"),
    dict(id="gutenberg", name="Johannes Gutenberg", g="m", place="germany_medieval", born="24 Jun 1400", age=40, face="c", skin=2,
         role="scientist", sched="pooled day 3", batch="8", true_place="",
         note="Goldsmith of Mainz. Business in Strasbourg undisclosed.",
         look="Johannes Gutenberg, goldsmith and inventor of printing with movable metal type, about 40. No true likeness survives (the forked beard of his traditional image was drawn a century after his death), so draw him as a clean-shaven Rhineland burgher of the 1440s with a shrewd, patient face: a Gugel hood of deep brown wool enclosing the head and neck with the face fully clear, its short shoulder cape cut into scalloped leaf-shaped points; a calf-length Tappert gown of dark blue wool with regular organ-pipe pleats, a high standing collar and wide fur-trimmed sleeves, belted low on the hips with a leather belt and a leather purse; dark hose; moderately pointed shoes.",
         avoid="a beard, a fur hat, a printing press, type or books in the hands"),
    dict(id="leonardo", name="Leonardo da Vinci", g="m", place="italy_earlymodern", born="15 Apr 1452", age=43, face="c", skin=2,
         role="artist", sched="pooled day 3", batch="8", true_place="",
         note="Painter and engineer in the service of the Duke of Milan.",
         look="Leonardo da Vinci, painter and engineer at the Sforza court of Milan, about 43, handsome and well groomed as his contemporaries described him, and clean-shaven like the Milanese men of the 1490s: shoulder-length, softly curled, well-kept brown hair. A small, soft, round red berretta cap worn straight on the head, with no brim and no feather. A knee-length, belted dusty-coral wool tunic (pitocco), short when long gowns were the fashion, over a white linen shirt gathered at the neck; close-fitting dark hose; soft round-toed shoes.",
         avoid="any beard (the long white beard belongs to his old age), paintings, notebooks or machines in the hands"),
    dict(id="lanyer", name="Aemilia Lanyer", g="f", place="britain_earlymodern", born="27 Jan 1569", age=31, face="b", skin=2,
         role="artist", sched="pooled day 3", batch="8", true_place="",
         note="Poet of London. Her book is not yet printed.",
         look="Aemilia Lanyer (born Bassano), London poet, about 31, bright and self-possessed. A tall-crowned black felt hat with a narrow brim, worn over a close white linen coif whose lace edge frames the face (the London citizen's wife's hat), with dark hair drawn up under the coif. A gentlewoman's black or russet silk gown with a long, stiff, pointed bodice and a full skirt over a modest hip roll; a wide, starched, white lace-edged ruff and white lace cuffs; one long strand of pearls.",
         avoid="royal regalia, a court wheel farthingale, an Elizabeth I look (no crown, no jewelled wig)"),
    dict(id="gallerani", name="Cecilia Gallerani", g="f", place="italy_earlymodern", born="3 Mar 1473", age=22, face="b", skin=2,
         role="artist", sched="pooled day 3", batch="8", true_place="",
         note="Poet and letter writer of the Sforza court in Milan.",
         look="Cecilia Gallerani, poet and learned lady of the Sforza court of Milan, about 22, with a composed, intelligent face. Dark hair centre-parted and smoothed flat over the ears, the rest bound into one long braid down the back (hidden from the front). A lenza: a finger-wide dark velvet band worn straight across the forehead, with a large dark-red stone set in gold, as big as the whole eye, at the centre of the brow. A square-necked deep-crimson gamurra gown with contrasting deep-blue sleeves laced on at the shoulder and elbow, so puffs of the white linen camicia show through the gaps; a deep-blue sbernia mantle draped over the left shoulder; a long necklace of black jet beads looped twice round the neck.",
         avoid="an animal in her arms, a see-through veil, any hat, ducal jewels or regalia"),
]
PRE = {x["id"]: x for x in PREMADES}

# ------------------------------------------------------------------ batch plan (by game day)
PILOT_PLACE = "greece_ancient"
STRESS = [("egypt_ancient", "hair_f_egypt_ancient.png"),
          ("egypt_medieval", "outfit_f_egypt_medieval.png"),
          ("egypt_medieval", "hair_f_egypt_medieval.png"),
          ("egypt_medieval", "headwear_f_egypt_medieval.png"),
          ("japan_ancient", "accessory_m_japan_ancient.png")]
STRESS_FILES = tuple(f for _, f in STRESS)  # ordered: the brief lists skipped files in this order on every run
DAY1 = ["egypt_ancient", "iraq_ancient", "italy_ancient"]
DAY2 = ["china_ancient", "britain_ancient", "egypt_medieval", "iraq_medieval", "greece_medieval",
        "italy_medieval", "china_medieval", "britain_medieval"]
DAY3 = ["japan_ancient", "germany_ancient", "japan_medieval", "germany_medieval"] + \
       [f"{c}_earlymodern" for c in COUNTRIES]
DAY4 = [f"{c}_industrial" for c in COUNTRIES]
DAY5 = [f"{c}_modern" for c in COUNTRIES]
assert len(set(DAY1 + DAY2 + DAY3 + DAY4 + DAY5 + [PILOT_PLACE])) == 40

def place_raws(pid, skip=()):
    out = []
    for g in GENDERS:
        for fname, keys in raw_files(pid, g):
            if fname not in skip:
                out.append((fname, keys))
    return out

# ------------------------------------------------------------------ PAIR blocks
def leak_line(p):
    parts = []
    for g in GENDERS:
        s = p["wardrobe"][g]["signature"]
        parts.append(f"{GWORD[g]}'s {SLOTWORD[s['slot']]} ({s['label']})")
    return "; ".join(parts)

COVER_NOTE = (" (This headwear hides all the hair, so no hair is drawn under it: bring its front edge right down to the "
              "hairline so no bald scalp shows.)")

def pair_block(pid):
    p = P[pid]; m, f = p["male"], p["female"]
    def hw(g, text):
        return f"- Headwear: {text}" + (COVER_NOTE if covers_hair(pid, g) else "")
    L = [f"PAIR: {pid} ({p['displayName']}, {yr(p['year'])})",
         f"Place and time: {p['moment']}", "",
         "MAN",
         f"- Outfit: {m['outfit']}",
         f"- Hair: {m['hair']}" + (" (this is a WIG: draw it in the colour described)" if p["wardrobe"]["m"]["wig"] else ""),
         f"- Facial hair: {m['facialHair']}",
         hw("m", m["headwear"]),
         f"- Accessory: {m['accessory']}", "",
         "WOMAN",
         f"- Outfit: {f['outfit']}",
         f"- Hair: {f['hair']}" + (" (this is a WIG: draw it in the colour described)" if p["wardrobe"]["f"]["wig"] else ""),
         hw("f", f["headwear"]),
         f"- Accessory: {f['accessory']}", "",
         f"MUST READ AT A GLANCE: {p['mustRead']}",
         f"LEAK ITEMS (draw these extra clear and true to the text): {leak_line(p)}",
         f"DO NOT DRAW: {'; '.join(p['avoidChatGPT'])}"]
    return "\n".join(L)

def place_section(pid, skip=(), heading_level="###", send_line=True):
    p = P[pid]
    raws = place_raws(pid, skip)
    files = ", ".join(f"`{fn}`" for fn, _ in raws)
    skipped = [s for s in skip if s.endswith(f"{p['country']}_{p['era']}.png")]
    extra = f" (already made in Batch 1: {', '.join(f'`{s}`' for s in skipped)})" if skipped else ""
    flags = []
    for g in GENDERS:
        w = p["wardrobe"][g]
        if w["wig"]: flags.append(f"the {GWORD[g]}'s hair is a wig (one colour, no variants)")
        if w["back"]: flags.append(f"the {GWORD[g]}'s hair hangs behind the shoulders (Claude splits off a hair-back layer)")
        if w["ornamentsToMask"]: flags.append(f"mask the {GWORD[g]}'s hair ornament before recolouring: {w['ornamentsToMask']}")
        if covers_hair(pid, g): flags.append(f"the {GWORD[g]}'s headwear covers the hair (check on the bald head that no scalp shows under it)")
    fl = ("\n\nNotes for Claude's processing: " + "; ".join(flags) + ".") if flags else ""
    rv = p["avoidReview"]
    review = ("\n\nReview only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A "
              "already bans caricature, hate symbols and likenesses in general terms): " + "; ".join(rv) + ".") if rv else ""
    send = "\n\n**Send this place's files to Claude** before you start the next place." if send_line else ""
    return (f"{heading_level} {CNAME[p['country']]}: {p['displayName']}\n\n"
            f"Costume Guide entry (the Culture value): **{p['cultureValue']}**\n\n"
            f"```\n{pair_block(pid)}\n```\n\n"
            f"Files ({len(raws)}): {files}{extra}{fl}{review}{send}\n")

# ------------------------------------------------------------------ counting
counts = []  # (batch label, raw images, processed keys)
def keys_of(raws): return [k for _, ks in raws for k in ks]

# ------------------------------------------------------------------ text
out = []
w = out.append

RULES_A = RULE_BULLETS.format(likeness=", and no likeness of any real person")
D1080, D720 = DESK[1080], DESK[720]
w(f"""# Time Sorter: Character Art Brief v2.3 (for ChatGPT)

*2026-09-24, revised the same day after the review of v2 (Appendix E), on 2026-09-25 for the 3D office (v2.1: see "What changed in v2.1"), again on 2026-09-25 for the ReStory style (v2.2: see "What changed in v2.2"), and on 2026-09-26 for the 2150 clothes and accessory kit (v2.3). Replaces `docs/CHARACTER_ART_BRIEF.md` (v1). Follows the piece-4 characters design (layers, file names, canvas), its Amendment A1 (premade cast about half women), piece 5 (Future outfits), the office move (the game in the art side's 3D office) and Saleh's style direction: like ReStory's cute 2D anime-style customers, for both kinds of character, with flat cel colours, simple textures and neutral even lighting.*

**The contract this brief follows.** This brief implements `docs/CHARACTER_ART_CONTRACT.md`, the tracked character-art contract of the piece-4 design (its W1 and R26). The older character contracts are retired: `ART_ASSET_LIST.md` section D (the 240 x 440 visitor trios and legendary pairs), its Tier-1 `traveller.png`, and the character direction in `PRODUCTION_PLAN.md`. No character art is delivered to them, and none goes to `Assets/Art/Office/Placeholder/traveller.png`: the game no longer uses that file. It stays on disk only because the art scene's leftover 2D booth (`OfficeRoot`, switched off when the office loads) and the art side's recovery scenes still reference it, and it goes when the art side deletes those leftovers (`docs/SCENE_CONTRACT_GAMEPLAY.md`).

Everything ChatGPT needs to draw every character in the game. "Time Sorter" is set in a 3D office; the travellers are flat 2D figures, built from layers, standing behind its desk. Work through the brief batch by batch, and **send each place's files to Claude as soon as that place is done**: Claude cuts the images out, lines them up, bakes the hair colours, names them for the game and tests them in Unity, in the game's office. A mistake that repeats (square images, a drifting mannequin, a colour the cut-out eats) then costs a few images instead of a whole batch. The game already runs on coloured placeholder shapes, and every finished file replaces its placeholder the moment it lands, so each place makes the game look better straight away.

Files that go with this brief (all in this folder, except the office screenshot):

- `{OFFICE_REF}`: a screenshot of the game's office that Saleh takes once (Unity's Game view at 1920 x 1080, the office with no traveller at the desk, saved as a PNG). It is a **style reference only**: ChatGPT matches its colour range and contrast so the figures sit in that room, and never draws the room. Attach it where a prompt's "Attached:" line names it.
- `character_guide_v2_1024x1536.png`: the figure guide. Attach it in Batch 1.
- `mannequin_m.png` and `mannequin_f.png`: Claude makes these from your approved Batch 1 figures.
- `style_card.png`: Claude makes it from the approved pilot (Batch 1, Step 4). Every garment prompt and the premade prompt (9.10) attach it from then on as a style reference.
- `premadebase_m_skin[N].png` and `premadebase_f_skin[N].png`: Claude makes these after Batch 2 (the approved body and head of each skin tone, lined up). The premade prompts attach them.
- `CHATGPT_MESSAGE.md`: the short message that starts the work.
- `UI_ART_RULES.md`: the rules for the desktop wallpapers, the 2D layers over the office (the PC frame, the speech bubble, the paper faces) and other UI art (a separate track).
- `coverage.json`: every file name the game needs, so the delivery can be checked by a script.

Attach whatever each prompt's "Attached:" line names.

## What changed in v2.3 (2026-09-26): 2150 clothes and the accessory kit

Every traveller must be dressed for the time they are going to, or they would cause a panic there (Saleh). A citizen of 2150 travelling into the past can get it wrong: another place's item, their own 2150 clothes, or one 2150 accessory over an otherwise right costume. Two things change for the art:

1. **The neutral Future outfits become game files** (`outfit_m_neutral_future`, `outfit_f_neutral_future`): they are the present's clothes, which the game draws on a 2150 citizen who forgot their costume. They were working references only; draw them exactly as Batch 11 says.
2. **New: the 2150 accessory kit** (Batch 12, section 24): four small items per gender ({", ".join(k["label"] for k in KIT)}), each readable at a glance above the desk and worn over any period costume. They are filed under the art nation `neutral` with their own name at the end (`accessory_m_neutral_future_{KIT[0]["variant"]}`).

Nothing else changes.

## What changed in v2.2 (2026-09-25): the ReStory style

v2.1 named the 2D customers of *ReStory: Chill Electronics Repairs* as its reference, then steered away from them: realistic proportions, a ban on large heads and eyes, and a ban on anime styling in Showa Tokyo's DO NOT DRAW. ReStory's customers are cute 2D anime-style characters with a soft, rounded, illustrative look, standing in a detailed 3D shop. That is this game's own set-up (flat 2D travellers in a 3D office), and the art side now cel-shades the office's desk props (`NOPE/Desk Anime`). Saleh asked for that style for both kinds of character: the generated travellers and the premades. Every change:

1. **The style (section 2, Block A, Block P).** The shared STYLE text of both blocks is rewritten: cute, soft 2D anime-style characters (expressive anime eyes, larger than realistic, with one simple highlight; a small, simple nose and mouth; clean rounded face shapes; hair in clean stylised shapes and locks), flat cel colours with one hard-edged shade tone, the same dark-brown outline and the same neutral, even front lighting. Adults who look their age, never chibi. No manga symbols (sweat drops, anger marks, blush lines); section 10 says so too.
2. **One cast.** The style applies the same way to both kinds of character, and section 2 and both blocks say so: the same eyes, line, shading and colour treatment, so a premade never stands out from a generated traveller at the desk. The premade prompt (9.10) now attaches the style card too, and Claude compares each premade with the pilot's Athens travellers (section 11).
3. **The proportions do not change.** The head size and body proportions stay the figure guide's and `LookCanvas`'s (the mannequins, every layer and the passport crop depend on them): the style changes the face and the rendering only. Batch 1, Step 1 now rejects a base figure whose head is too big for its body.
4. **Faces stay distinct.** Section 7 and prompts 9.1 and 9.3 say how faces a to d differ (cheeks and chin, eye shape, brows, nose, the lines of age) inside face a's head outline, which Batch 2 still requires. The skin tones stay exactly their swatches (the style never lightens a darker skin), and every culture gets the same style.
5. **Premade expressions** (prompt 9.11, Block P): anime-expressive but dignified, because they are real people; each premade keeps the features its description gives (Socrates' snub nose and full lips, for example).
6. **Showa Tokyo.** Its DO NOT DRAW bans "cosplay, idol-costume or anime-costume cliches" in place of the old anime ban, which would now ban the style itself, and adds "a school-uniform look": in this style its office outfit (a blouse with a bow at the collar and a pleated skirt) could drift into one. Every other DO NOT DRAW and review line was checked against the new style; none contradicts it.
7. **`CHATGPT_MESSAGE.md` and `README.md`** carry the new style rules; the first image is still the bald base man, now in this style.

Nothing else changes: the layers, canvas and landmarks, the green background and colour rules, the leak items and MUST READ lines, section 1's desk view and sizes, the file names, the batch order and files, and every key in `coverage.json`.

## What changed in v2.1 (2026-09-25): revised for the 3D office

The v2 brief was written for the old flat 2D booth. The game now runs in the art side's Blender-made 3D office, with the travellers as flat 2D layered figures standing behind its desk. Every change, decided by Claude under Saleh's "go with all pieces, don't stop" (open to his review):

1. **The framing (D1).** Block A, Block P, `CHATGPT_MESSAGE.md` and the `UI_ART_RULES.md` set-up say it: "Time Sorter" is set in a 3D office and the travellers are flat 2D figures standing at the desk. A screenshot of the office, `{OFFICE_REF}`, is attached as a **style reference only** (its colour range and contrast, so the figures sit in the room; the room is never drawn). The flat style, the outlines, the neutral even lighting and the green background are unchanged. "Unity adds time-of-day light" is replaced by what the game does: it stands the flat, unlit figure in the lit room and tones it to the room's light.
2. **Where the player sees a traveller (D2).** Section 1's "shown three ways" and the Visitor window (270 x 406 px) are replaced by a table of the three places a traveller appears: the office view (head to waist, measured sizes), the passport photo, and the Look menu (names only). Nothing shows the whole figure. The layers stay whole figures on the same 1024 x 1536 canvas (the contract is unchanged; a walk-in or a moved anchor may show more later), but nothing that identifies a look relies on the lower body, and every identifying item must read at the measured head size. Section 3's canvas table, section 6's accessory row, prompts 9.4 and 9.8 and the checklist say so.
3. **Leak items the desk can see (D3).** Every leak item now sits on the head, face, neck, shoulders or upper chest. Five sat below the desk; each was replaced or moved (details and sources in Appendix F):
   - Ottoman Ioannina, woman: the **pafti buckle** (waist) becomes **silver chest chains** (the pafti stays drawn, closing the outfit's belt);
   - Tokugawa Edo, woman: the **Nagoya-obi** (hips) becomes the **kazuki veil**, a kosode worn over the head (the Nagoya-obi stays drawn, in the outfit);
   - Beijing 1972, woman: the **khaki satchel** (hip) becomes the **navy cap**, the same cap as the men's (the satchel stays drawn, not leakable);
   - Metapolitefsi Athens, man and woman: the **tagari bag** stays the leak item but is drawn high against the side of the chest on a short strap;
   - Eastern Han Luoyang, woman: the **bi-disc pendant** (mid-chest, borderline) moves up to the upper chest.
   Every MUST READ line now names only what shows above the desk ({N_MUST_V21} rewritten; the lower body stays in the outfit lines, because it is still drawn), and `tools/data_v2.py` fails on a leak accessory or a MUST READ line that names a lower-body feature.
4. **The game matches (D4).** The same leak-item change is in `Assets/Data/World/world_source.json` (the piece-4 wardrobe that `worldSourceWardrobe` mirrors): the three changed signatures, their labels and `leakable` flags, the Costume Guide rows (Culture values) they give, one new hand-authored confusable pair and {N_HID_V21} generated ones (Appendix D).
5. **UI art (D5).** `UI_ART_RULES.md`: the 3D-office framing; wallpapers become 4:3 (1440 x 1080, from ChatGPT's 1536 x 1024); posters leave the ChatGPT prompts and the delivery list (the office has no poster); a new section on the 2D layers over the office (the PC frame, rendered from the Blender CRT by preference, the speech bubble, the paper faces); the 3D props the Blender side makes (the scanner).
6. **The 2D booth is gone from the text (D6).** Pixels-per-unit and world-unit instructions, "the booth rework", the `traveller.png` sentence, "Claude checks the booth", the Visitor window and Appendix E's poster question are removed or replaced. `coverage.json` `officeArt` lists the 4:3 wallpapers and marks the posters deferred; its canvas gains the measured desk view.

**Open questions for Saleh (v2.1)**

1. **The desk hides less than thought.** Measured at the default traveller anchor, the NEXT sign's top edge cuts the figure at the **waist** (canvas y {DESK_VISIBLE_TO}), not mid-chest: the whole chest shows. The brief still keeps identity above mid-chest (y {READ_ZONE_TO}), because papers held up to read (piece 10) cover the figure's sides from there down and the Look menu's "< Back" button sits over the neck while it is open. Keep that margin?
2. **The tagari.** Worn high on the chest it is less typical than at the hip, where the research and v2 put it. Accept, or pick another Greek 1975 item (none passed the checks: Appendix F)?
3. **The kazuki and the women's navy cap** add headwear to two looks that had none (both historically attested: Appendix F). Accept?

## What changed since v1

- **Style.** Cute, soft 2D anime-style characters (since v2.2) with flat cel colours and very little texture, the same for travellers and premades, and **neutral, even lighting** (the game stands the flat figure in its 3D office and tones it to the room's light). v1 asked for painterly shading lit from the top-left.
- **File names follow the game's key grammar** (section 8). Hair and facial hair come out in five baked colours (Claude makes them from your medium-brown drawing). The Future uses the same pattern as every other era (`outfit_m_china_future`), and all Future travellers share one neutral hairstyle.
- **A hair-back layer.** Hair that shows beside the neck behind the shoulders is split off by Claude and drawn behind the body, so it never covers a collar or shawl.
- **All 68 data fixes and all 22 practicality fixes from the v1 review are in** (Appendices A to C say how, and the few that were adapted and why).
- **Leak items.** Every look now names, per gender, the one item a liar from that place can "leak" onto a disguise. Draw those extra clearly.
- **Batches follow the game's days**, so the places that appear first get art first. The pilot is Periclean Athens (day 1) instead of Victorian London.
- **Premade characters are named**: ten real, long-dead people who are not rulers, five women and five men (section 11), made in their own ChatGPT Project.
- **After the v2 review** (Appendix E): every leak item stands on its own; purses, fans and cases that hung from an outfit's belt are now part of that outfit; every prompt states the size; a style card keeps the look steady; each DO NOT DRAW line holds only likely confusions (caricature and hate-symbol checks moved to a review line outside the block); the premades wear their place's Costume Guide item.

## 1. How characters work in the game

- **Two kinds of character.** *Generated* travellers are built from layers, so the game can make thousands of different people. *Premade* characters (named historical people) are drawn whole, with four expressions. Both kinds share one style (section 2), so they look like one cast.
- **The layers, bottom first:** hair back, body, outfit, head, facial hair, hair, headwear, accessory. You draw seven kinds of image; Claude makes the hair-back layer from your hair drawing.
- **Every layer is a full 1024 x 1536 image with the figure in exactly the same spot**, so the game stacks them with no adjusting.
- **Every country and era has its own look.** Honest travellers wear only their claimed place's look. A liar's disguise leaks exactly **one** item from their real home, like a Victorian top hat on someone claiming to be from Edo Japan. The player picks that garment by name in the traveller wheel's Look menu and compares it with a reference book on the office PC, the **Costume Guide**, which lists one "leak item" per gender for each place. That is why every look has LEAK ITEMS: they must be recognisable at the desk's size (below), different from every other place's, and make sense on their own over any other place's clothes.
- **A worn item can hide another.** A turban, hood or veil that hides all the hair (its PAIR line says so) means the game draws no hair under it, so its edge must come down to the hairline.
- **Skin and hair colour never give anyone away.** The game picks them from the place the traveller *claims* to be from (and turns hair grey from age 60). Only culture (clothes, hairstyle, headwear, accessories) can be a clue.
- **The traveller's job never changes the look.** Soldiers, scientists and merchants all wear civilian dress.

### Where the player sees a traveller

The game is set in the art side's 3D office. A traveller is the stack of flat 2D layers, standing behind the desk as a billboard 1.8 m tall (soles to the top of the head), turned to face the camera and toned to the room's light. Sizes are measured at the default traveller anchor, at 1920 x 1080 (and 1280 x 720; they scale with the screen height only).

| Where | What the player sees | Size at 1920 x 1080 (1280 x 720) |
|---|---|---|
| **The office view**, from the moment the traveller is called until the decision | The figure from the headroom (tall hats) down to the **waist**: the NEXT sign and the desk hide everything below (canvas y {DESK_VISIBLE_TO}). Papers held up to read (from piece 10) cover the figure's sides from about mid-chest (canvas y {READ_ZONE_TO}) down, and while the wheel's Look menu (or another sub-menu) is open, its "< Back" button sits over the neck and collar. | The head (top of the head to the chin) about **{D1080['head']} px** tall and {D1080['face']} px wide ({D720['head']} x {D720['face']}); top of the head to the shoulders {D1080['head_to_shoulders']} px ({D720['head_to_shoulders']}); the shoulders {D1080['shoulders']} px wide ({D720['shoulders']}). One canvas pixel is about {D1080['px']} screen pixels ({D720['px']}): the 2-3 px outline shows as about 1 px, and a detail thinner than a finger (about 12 canvas px) is lost. |
| **The passport photo**: on the passport paper on the desk, on its scanned copy on the PC, and (from piece 10) on the paper held up close to read | The head-and-shoulders crop (362, 215) to (662, 590) of the same stack: head, neck, shoulders and upper chest. | On the desk paper about {D1080['photo_desk']} px, lying flat (not for reading); on the scanned copy in the open PC frame about {D1080['photo_pc']} px ({D720['photo_pc']}); on a held paper about {D1080['photo_held']} px ({D720['photo_held']}), and up to {D1080['photo_beside']} px beside the PC frame. |
| **The Look menu** on the traveller wheel | No picture: every worn garment is listed by its label ("sakkos snood", "petasos hat"); choosing one puts it into the PC's compare bar, to compare with the Costume Guide. | – |

**Nothing shows the whole figure.** The Visitor window of the first design was dropped (the piece-4 amendments, K1). Still draw every layer as a whole figure, down to the feet, on the same 1024 x 1536 canvas: the art contract is unchanged, and a walk-in or a moved traveller anchor may show more later. But **nothing that identifies a look may rely on the lower body**: every LEAK ITEM and everything a MUST READ line names sits on the head, face, neck, shoulders or upper chest, in bold shapes that read when the head is only {D1080['head']} px tall (a leak item is at least a palm across).

## 2. The look

The reference is the 2D customers who walk up to the counter in *ReStory: Chill Electronics Repairs*: cute, soft 2D anime-style characters with a rounded, illustrative look, standing in a detailed 3D shop. That is this game's set-up too (flat 2D travellers in a 3D office), and the art side already shades the office's desk props with an anime cel shader (`NOPE/Desk Anime`: `ArtDeliverables/TimeDesk/ImportedOffice/DeskClean/ANIME_SHADER.md`). So the characters follow ReStory's customers: anime faces, flat cel colours, almost no texture. Never name any game in a prompt; Block A describes the look in words, and a named game pulls ChatGPT towards that game's look.

- **One cast, two kinds.** The style is the same for both kinds of character: the generated travellers, built from layers (Block A), and the premades, drawn whole (Block P, section 11). Both blocks carry the same STYLE text, written once by `tools/build_v2.py`, and both say the two kinds must look like one cast: the same eyes, line, shading and colour treatment, so a premade never stands out from a generated traveller at the desk.
- **Faces and hair:** cute, soft and anime-style: expressive anime eyes (larger than realistic, with one simple highlight), a small, simple nose and mouth, clean rounded face shapes, and hair in clean stylised shapes and locks. Adults who look their age, never chibi or childlike.
- **Proportions: the guide's, exactly.** The head size and body proportions are fixed by the figure guide and the game's `LookCanvas` (section 3; the figure is about seven and a half heads tall): the mannequins, every layer, the premades and the passport crop depend on them. The style changes the face and the rendering, never the head size or the body.
- **Reads small:** every look stays recognisable when the head is {D1080['head']} px tall (section 1). The anime face helps: big, clear eyes and simple, bold shapes survive at that size, where fine realistic features blur.
- **Respect:** never a caricature of any people. The five skin tones stay exactly their swatches (the style never lightens a darker skin tone), and the four faces a to d stay clearly different in cheeks and chin, eye shape, brows and nose (section 7), because anime styling tends to give everyone the same face. Every culture gets this same style: no exoticised styling, and no cosplay or anime-costume cliches.
- **Textures:** flat cel colours: each colour area has one hard-edged shade tone (the same hue, about 20% darker, with a crisp edge, never blended) and at most one small highlight. No fabric grain, brush or paper texture, noise or photo detail. Patterns (stripes, checks, borders, embroidery) are clean, bold, flat shapes that still read when the head is {D1080['head']} px tall (section 1).
- **Outline:** a clean, even dark-brown line ({OUTLINE_HEX}, 2 to 3 px) around every piece and its main folds.
- **Lighting: neutral and even.** Plain white light from the front. Shading only shows form (under the chin, inside folds, under a brim), the same on both sides. No light direction, rim light, glow, cast or ground shadow, and no warm or cool tint. The game stands the flat, unlit figure in its lit 3D office and tones it to the room's light (a warm grey tint today), so any light baked into the drawing would fight the room's.
- **The room:** `{OFFICE_REF}` shows the office the figures stand in. ChatGPT matches its colour range and contrast (so a figure never looks pasted in), never its lighting, and never draws the room.
- **Colour:** gently muted, natural period dyes. Nothing neon.
- **Pose and expression:** front view, standing straight, facing the viewer, arms relaxed slightly away from the body, hands open and empty, neutral expression, eyes looking at the viewer. The premades' other three expressions (prompt 9.11) are anime-expressive but dignified, because they are real historical people. No manga symbols on any face (sweat drops, anger marks, blush lines, sparkles, tears, speed lines).
- **One style across about 40 chats.** After the pilot is approved, Claude makes `style_card.png`: the approved Athens outfits in greyscale on the mannequin, plus a small swatch of folds showing their one hard-edged shade tone, a pattern band and the outline weight. Every garment prompt attaches it as a style reference only, and so does the premade prompt (9.10), so line weight, shading depth and pattern scale stay the same in every chat and in both Projects.

## 3. Canvas, pivot and the figure guide

Every layer (and every premade image) uses the same canvas and the same figure position. These numbers are the game's (`LookCanvas` in the code), so they never change.

| What | Value |
|---|---|
| Canvas | 1024 x 1536 px, portrait (ChatGPT's tall size) |
| Centre line | x = 512 |
| Landmarks (from the top) | top of head 260, chin 424, shoulders 500, waist 760, hips 900, knees 1170, soles of the feet 1490 |
| Safe area | x 120 to 904; nothing may cross it. Tall hats and hair may rise into the headroom above y = 260 |
| Pivot | the feet: x = 512, y = 1490 (the game's pivot is 3% up from the bottom edge) |
| Passport photo | crop (362, 215) to (662, 590), 4:5, head and shoulders; a tall hat is cut by the frame, which is fine |
| Office view (section 1) | the figure from the headroom to the waist (y {DESK_VISIBLE_TO}); the head about {D1080['head']} px tall at 1920 x 1080 ({D720['head']} px at 1280 x 720), one canvas pixel about {D1080['px']} screen pixels |
| Read zone | everything that identifies a look sits above mid-chest (y {READ_ZONE_TO}): the head, face, neck, shoulders and upper chest |
| Delivered file | RGBA PNG, 1024 x 1536, **untrimmed** (Claude never crops layers; the stack depends on it) |

ChatGPT can't measure pixels, so the prompts refer to the guide's lines instead. Claude re-centres and rescales every image onto these landmarks before cutting it out, so a figure that comes out slightly big or off-centre is not a reason to reject it.

## 4. Why green and magenta

ChatGPT can't line up separate transparent images reliably. So every piece is drawn **on top of a flat magenta mannequin, on a flat green background**. Claude's script then lines each image up with the mannequin (ChatGPT always redraws the whole picture, so everything shifts a little), removes the green and magenta, and leaves just the piece in the right place. That's why characters must never use bright green, magenta, pink or purple, and why nothing may be shaded onto the magenta: a shadow painted on the mannequin turns into purple-grey pixels that either vanish (the hat looks pasted on) or stay as a dark smear that travels with every leaked hat. Claude adds any contact shade in processing.

## 5. Setup in ChatGPT (once)

1. Create a ChatGPT **Project** called "Time Sorter Characters".
2. Paste **Block A** (below) into the Project's instructions.
3. Take `{OFFICE_REF}` once: in Unity, open the game's office, set the Game view to 1920 x 1080, and save a screenshot of the office with no traveller at the desk. It is a style reference only (section 2).
4. Keep it, the figure guide, and later the two mannequins and the style card, in a folder on your PC. In every message, use the paperclip to attach exactly the file(s) named on the prompt's "Attached:" line, even if they are also in the Project files. ChatGPT's drawing tool only reliably uses images attached to the message. Don't add finished pieces to the Project files, because ChatGPT copies details from them into other looks.
5. Every prompt below ends with a size line ("{SIZE_LINE}"). Keep it: without it ChatGPT often answers with a square image, which the importer rejects and which costs a regeneration.
6. Start a new chat for each PAIR block (each country and era). Long chats drift in style and mix up looks.
7. Never ask ChatGPT to fix or tweak an image it made. If something is wrong, press Regenerate, or send the same prompt again with the original mannequin attached. Every edit redraws the whole picture, and the figure drifts further each time.
8. Save every image with ChatGPT's own download button on a computer, so the file is a 1024 x 1536 PNG. Never screenshot, and never save from the phone app: JPG files blur the green and magenta edges Claude removes.
9. **Send each place to Claude as soon as it is done** (its 5 to 9 files), even in the middle of a batch, and wait for Claude's go-ahead on the first place of every batch. The batches are only the planning unit.
10. Premade characters are real people, so they are made in a **second Project**, "Time Sorter Premades", with its own instructions, Block P (section 11).

### Block A: paste into the Project instructions

```
You are drawing characters for "Time Sorter", a game set in a 3D office in which the player is a clerk at a time-travel border desk, checking the papers of travellers from many countries and eras. The travellers are flat 2D figures standing at the desk. Every character is built from separate layers (body, head, outfit, facial hair, hair, headwear, accessory) that the game stacks on top of each other, so every layer must line up with the same figure.

IN THE GAME
{GAME_BULLETS}

STYLE
{STYLE_BULLETS}
{RULES_A}

CANVAS (every image)
{CANVAS_BULLETS}

MANNEQUIN RULE
- When I attach a magenta mannequin, keep it as it is: flat magenta, same shape, same position, same darker-magenta face marks. Draw ONLY the item I ask for, on top of it, fitted to it. Do not draw skin, a face, hair or anything else I did not ask for.
- Never shade, darken or cast a shadow onto the magenta mannequin. Shade only inside the item you draw.

HAIR COLOUR
- Draw all natural hair and facial hair in one medium brown; the other colours are made later. A wig is costume: draw a wig in the colour I describe.
- Anything in the hair that is not hair (ties, cords, ribbons, pins, combs, nets) must be clearly a different colour from brown: white, silver or blue, or black where the look says so. Never red. Draw gold ornaments bright yellow with a dark outline.

When I send a PAIR block, reply only READY and draw nothing. After that, one image per reply.
```

## 6. The layers

| Layer | What it contains | What it must NOT contain | How many |
|---|---|---|---|
| Body | The figure without the head: neck up to the chin line, arms, open hands, legs, bare feet, plain light-grey undergarment (a strapless bandeau for women and mid-thigh shorts) | Head, hair, real clothes | 2 genders x 5 skin tones (tones 2 to 5 recoloured from tone 1 by Claude) |
| Head | Bald head with ears and face, in the body's skin tone, no makeup | Hair, beard, anything below the chin | 2 genders x 5 skin tones x 4 faces |
| Outfit | All clothing and footwear for one place, with the fastenings and the purses, cases, fans and cords that hang from its own belt or sash | Face, hair, headwear, the accessory, jewellery, badges (plain buttons, clasps and pins are fine); nothing above the chin line | 1 per gender per place |
| Facial hair | Beard, moustache or whiskers only | Face, hair | Men only, when the look has one |
| Hair | The hairstyle only, as seen from the front with any hat or veil taken off, fitted to the bald head (shaved parts stay magenta) | Face, headwear; a plait, braid or tail that hangs down the back (the body hides it) | 1 per gender per place |
| Hair back | Made by Claude: the part of the hair that shows beside the neck behind the shoulders, moved behind the body | (you never draw it separately) | Only for loose long hair, a wide wig or a spread curtain of plaits |
| Headwear | Hat, cap, veil, hood or headdress, sized to sit over a full head of hair; eyes, nose and mouth stay visible. One that hides all the hair (its PAIR line says so) comes down to the hairline, because the game draws no hair under it | Hair, face | When the look has one |
| Accessory | One worn item that stands on its own on any outfit and hairstyle and reads at the desk's head size (section 1): a necklace, collar or pendant on its own chain or cord, a belt, sash or girdle and what hangs from it, a strap bag, a shawl or scarf, glasses, headphones, large earrings | Anything held in the hands; anything that hangs from, is tucked into, pins, closes or sits under another layer's item (a sash, belt, cloak, collar, pocket, hairstyle); rings, single or small earrings, small pins or badges | When the look has one |

A liar's leaked item is drawn over the claimed look, so every item must make sense on its own, on anyone. An item is never an absence ("clean-shaven", "bareheaded"): only real items can leak. For a leak item this is a hard rule: a brooch that pins "the cloak" or a pendant hanging from "the sash" would float in the air over another place's clothes. A leak item also sits where the desk never hides it, on the head, face, neck, shoulders or upper chest (section 1): a belt, a sash round the waist or a bag at the hip can be an ordinary accessory, never a leak item.

## 7. Bodies, heads, skin tones, faces and hair colours

**Skin tones** (5): """ + ", ".join(f"{n} {SKIN_WORD[n]} `{SKIN_HEX[n]}`" for n in SKINS) + f""". Every culture uses several tones; the game picks them by weights per place, so a skin tone never points to a country. ChatGPT gets the swatches in prompt 9.2, and Claude recolours bodies 2 to 5 from the approved tone-1 body to the same swatches, so heads and bodies agree.

**Faces** (4), chosen by the traveller's age. In the anime style (section 2) faces easily come out alike, so each face has its own cheeks and chin, eye shape, brows and nose. All four keep face a's head outline and feature positions (within a few pixels: Batch 2), because the beards, caps and glasses are fitted to them, so they differ inside that outline:

| Face | Age | Drawn as |
|---|---|---|
| a | 18 to 34 | {face_drawn('a')} |
| b | 18 to 34 | {face_drawn('b')} |
| c | 35 to 59 | {face_drawn('c')} |
| d | 60 and over | {face_drawn('d')} (the game also turns the hair grey) |

**Hair colours** (5, baked by Claude, never drawn by ChatGPT): black, brown, blond, red, grey. You draw every natural hairstyle and beard once, in medium brown; Claude masks any ornaments and bakes the five colours into five files. Wigs are costume and keep the colour you draw (one file, no variants).

For Saleh only, never for ChatGPT (the game's starting weights, to review; they are never a clue): Egypt and Iraq mostly tones 3 to 4 and black hair; Greece and Italy tones 2 to 3, black or brown hair; China and Japan tones 2 to 3, black hair; Britain and Germany tones 1 to 2, brown or blond hair, some red.

## 8. File names, delivery and where files go

**You save** each image with exactly the listed name into `ArtDeliverables/TimeDesk/Characters/Raw/<batch folder>/`, as the green-and-magenta original. **Claude makes** the cut-out, game-ready files from it at `Assets/Art/Characters/Resources/Characters/<key>.png`, and the game loads them by name. A finished file replaces its placeholder with no code change.

| You draw (raw file) | Claude delivers (game keys) |
|---|---|
| `base_[m/f]_skin1_facea.png` (whole figure) | `body_[m/f]_skin1` to `body_[m/f]_skin5` (skin 1 cut from it; skins 2 to 5 recoloured from it to the section-7 swatches) and `head_[m/f]_skin1_facea` |
| `base_[m/f]_skin[2-5]_facea.png` (whole figure, new skin tone) | `head_[m/f]_skin[N]_facea` (Claude keeps only the head) |
| `head_[m/f]_skin[1-5]_face[b/c/d].png` (whole figure, new face) | `head_[m/f]_skin[N]_face[b/c/d]` |
| `outfit_[m/f]_[country]_[era].png` | `outfit_[m/f]_[country]_[era]` |
| `hair_[m/f]_[country]_[era].png` | `hair_[m/f]_[country]_[era]_[black/brown/blond/red/grey]` (5 files; a wig: 1 file with no colour), plus `hairback_...` in the same colours when the hair shows behind the shoulders |
| `facialhair_m_[country]_[era].png` | `facialhair_m_[country]_[era]_[colour]` (5 files) |
| `headwear_[m/f]_[country]_[era].png` | `headwear_[m/f]_[country]_[era]` |
| `accessory_[m/f]_[country]_[era].png` | `accessory_[m/f]_[country]_[era]` |
| `outfit_[m/f]_[country]_future.png` | `outfit_[m/f]_[country]_future` |
| `hair_[m/f]_neutral_future.png`, `facialhair_m_neutral_future.png` | `hair_[m/f]_neutral_future_[colour]`, `facialhair_m_neutral_future_[colour]` (shared by every Future place) |
| `outfit_[m/f]_neutral_future.png` | `outfit_[m/f]_neutral_future` (the present's clothes, since v2.3) |
| `accessory_[m/f]_neutral_future_[item].png` | `accessory_[m/f]_neutral_future_[item]` (the 2150 accessory kit; items: `{" ".join(k["variant"] for k in KIT)}`) |
| `premade_[id]_[expression].png` | `premade_[id]_[neutral/happy/angry/worried]` |

- Countries: `egypt iraq greece italy china japan britain germany`. Eras: `ancient medieval earlymodern industrial modern future`. Gender: `m` or `f`. Lower case, no spaces, underscores only.
- Only what the look has: no file for "none" (no facial hair, no headwear, no accessory).
- `outfit_[m/f]_neutral_future.png` is the base of the Future batch and, since v2.3, a game file: the present's clothes. Claude's working files are not game keys: `mannequin_[m/f].png`, `style_card.png`, `premadebase_[m/f]_skin[N].png`.
- The full list of game keys, with the raw file each comes from, is in `coverage.json`.

## 9. The prompts

Replace the parts in [square brackets]. For each place, start a new chat, send its PAIR block first (they're in the batch sections), then use prompts 9.4 to 9.8 for each file in its list, one message per file. Every prompt ends with the size line; keep it.

### 9.0 Starting a place

```
Attached: {OFFICE_ATTACH}.
[paste the PAIR block]
Read this and reply only READY. Do not draw anything yet.
```

Attach the office screenshot here once per chat; ChatGPT keeps it in mind for the rest of that place's images.

### 9.1 Base figure (Batch 1)

```
Attached: character_guide_v2_1024x1536.png (the Time Sorter figure guide) and {OFFICE_ATTACH}.
Draw a BASE FIGURE: a [man / woman] in their 20s, skin tone [1 very light], bald (no hair at all, smooth scalp), ears visible, no makeup, neutral expression, looking at the viewer.
Face: {FACE_LOOK['a']}.
Follow the guide's pose, proportions and landmark lines: the top of the head on the TOP OF HEAD line, the chin on the CHIN line, the soles of the feet on the bottom line, centred on the dashed centre line.
Clothing: only [plain light-grey fitted shorts ending mid-thigh / a plain light-grey strapless bandeau covering only the bust, and plain light-grey fitted shorts ending mid-thigh]. Bare feet.
Remove all guide lines, labels and the grey silhouette. Flat pure green #00FF00 background.
Portrait, 1024 x 1536, the same framing as the attached guide.
```

### 9.2 Skin tone variant (Batch 2)

```
Attached: my approved base figure base_[m / f]_skin1_facea.png.
Keep everything exactly the same (pose, face, proportions, position, clothing, background) and change ONLY the skin tone to [""" + " / ".join(f"{n} {SKIN_WORD[n]}, {SKIN_HEX[n]}" for n in SKINS[1:]) + f"""].
{SIZE_LINE}
```

Claude keeps only the head from these; the bodies for tones 2 to 5 are recoloured from your approved tone-1 body to the same swatches, so they match the mannequin exactly.

### 9.3 Extra face (Batch 2)

```
Attached: my approved base figure base_[m / f]_skin[N]_facea.png.
Keep everything exactly the same (pose, body, skin tone, grey clothing, position, size, background) and change ONLY the face to: [""" + " / ".join(f"{v}: {face_drawn(v)}" for v in FACES[1:]) + f"""]. Still bald, ears visible, no makeup, neutral expression, looking at the viewer. Do not move, turn or resize the head, and keep its outline: only the cheeks and chin may be a little rounder or more defined, as described.
{SIZE_LINE}
```

Each image shows the whole figure; Claude cuts the head out, lines it up on the face-a marks and matches its colour to the body (Batch 2 says how).

### 9.4 Outfit

```
Attached: mannequin_[m / f].png (draw on this) and {STYLE_ATTACH}.
From the PAIR block, draw ONLY the [man's / woman's] OUTFIT (all clothing and footwear) on the magenta mannequin, fitted to its body.
Do not draw the face, hair, facial hair, headwear, accessory or any jewellery. The head stays magenta and uncovered: keep every collar and garment below the chin line, because anything drawn over the head is hidden by the head layer. Any part of the body the outfit does not cover stays magenta.
Draw the whole outfit down to the shoes. Make the collar, shoulders and chest especially clear: in the game the desk hides the figure below the waist.
{SIZE_LINE}
```

### 9.5 Hair

```
Attached: mannequin_[m / f].png (draw on this) and {STYLE_ATTACH}.
From the PAIR block, draw ONLY the [man's / woman's] HAIR on the mannequin's bald head, in medium brown (unless the PAIR block says it is a wig). Leave any shaved parts of the scalp magenta.
Draw the hair as it looks from the front with any hat or veil taken off. If the PAIR block says the hair is hidden under headwear or pinned up, draw it short or pinned up close to the head (a neat low bun, or braids pinned up), never hanging below the jaw, and do not draw the headwear. A plait, braid or tail that hangs down the back is hidden by the body: leave it out.
Hair must never cover the eyes, nose or mouth. Draw nothing else.
{SIZE_LINE}
```

### 9.6 Facial hair

```
Attached: mannequin_m.png (draw on this) and {STYLE_ATTACH}.
From the PAIR block, draw ONLY the man's FACIAL HAIR on the mannequin's face, using the darker magenta face marks to place it, in medium brown. Leave the rest of the face magenta. Draw nothing else.
{SIZE_LINE}
```

### 9.7 Headwear

```
Attached: mannequin_[m / f].png (draw on this) and {STYLE_ATTACH}.
From the PAIR block, draw ONLY the [man's / woman's] HEADWEAR on the mannequin's head, sized to sit over a full head of hair: a little larger all round than the bald head, never tight to the scalp. If the PAIR block says it hides all the hair, bring its front edge right down to the hairline so no bald scalp would show. Headwear that covers the head (a hood, veil or head-shawl) continues down the sides of the neck onto the shoulders. The eyes, nose and mouth stay fully visible. Draw only what is seen in front of the body, and leave out anything that would hang down the back. Draw nothing else.
{SIZE_LINE}
```

### 9.8 Accessory

```
Attached: mannequin_[m / f].png (draw on this) and {STYLE_ATTACH}.
From the PAIR block, draw ONLY the [man's / woman's] ACCESSORY in its natural worn position on the body. It is worn, never held, and it stands on its own: do not draw a belt, sash, cloak or collar for it to hang from unless the accessory line names one as part of it. Make it big and bold enough to recognise when the head is shown only about 40 to 60 px tall: a clear shape at least a palm across, with no detail that matters thinner than a finger. Draw only what is seen in front of the body, and leave out anything that would hang down the back. Draw nothing else.
{SIZE_LINE}
```

### 9.9 Future culture outfit (Batch 11)

```
Attached: my approved outfit_[m / f]_neutral_future.png.
Keep the magenta mannequin, the green background and the outfit's cut, fit and outline exactly. Restyle ONLY the [man's / woman's] outfit's fabric, colours, seams, trims and patterns with the motifs in the PAIR block. Where a motif describes a shape (sleeves, drapes, capes, wraps, collars), suggest it with panels, seams and trim lines on the same cut. Clothing and shoes only: no hat, cap, headband, hood, glasses, visor, jewellery, neck ring or bag, nothing above the chin, nothing that glows, nothing see-through, no holes cut through the fabric, no letters.
{SIZE_LINE}
```

### 9.10 Premade character (Batches 3, 6 and 8)

```
Attached: premadebase_[m / f]_skin[N].png (the lined-up base figure), {STYLE_ATTACH} and {OFFICE_ATTACH}.
Dress this exact figure as a complete PREMADE CHARACTER, keeping its pose, size and position, with a new face, hair, clothing, headwear and accessories: [the character's description from its batch section]. Hands stay open and empty. Neutral expression, looking at the viewer. Flat pure green #00FF00 background.
Do not draw: [the character's "do not draw" line].
{SIZE_LINE}
```

### 9.11 Premade expression

```
Attached: my approved premade_[id]_neutral.png.
Keep everything exactly the same (pose, clothing, position, background) and change ONLY the facial expression to [happy: a warm, open smile with softly curved, smiling eyes / angry: lowered, frowning brows, narrowed eyes and pressed lips / worried: raised inner brows, wide, uncertain eyes and a small, tight mouth]. Make it clear and expressive in the anime style, but dignified: no comic distortion of the face and no manga symbols (sweat drops, anger marks, blush lines, tears).
{SIZE_LINE}
```

Claude takes only the face from each expression image and pastes it onto your approved neutral image. Don't reject an expression because its clothes changed; reject it only if the head moved or changed shape.

## 10. What NOT to draw

These hold for every image, on top of each PAIR block's own DO NOT DRAW list.

- **No text of any kind:** no letters, numbers, logos, brand names, signatures, watermarks, labels, and no fake writing. "Tiraz" and "script" bands are abstract patterns. Coins are plain discs.
- **No insignia or regime symbols:** no flags, national or party emblems, stars on caps, eagles, rising-sun rays, the imperial chrysanthemum, five-claw dragons, rank badges, medals, company badges, crests that read as a warrior's retainer, and never any hate symbol.
- **No royal regalia or religious vestments:** no crowns, cobras, false beards, sceptres; no priest's, monk's or nun's garments, no holy symbols or deity motifs.
- **No uniforms, armour or weapons.** Every traveller is a civilian.
- **No skin-colour tells:** never tie a skin tone or hair colour to a culture, never tint or shade skin inside an outfit, hair or headwear layer, never draw skin anywhere except on the base figures and heads, and never draw makeup on the heads.
- **No held props:** hands stay open and empty.
- **No caricature:** no stereotyped features, no "Hollywood" versions of a culture, no ragged or comic poverty.
- **No manga symbols:** no sweat drops, anger marks, blush lines, sparkles, tears or speed lines on any face.
- **No colour the cut-out eats:** no bright green, lime, magenta, pink, purple or violet; nothing see-through; no holes cut through fabric (lattice, perforation and laser-cut motifs are printed or stitched on solid cloth); no checkerboard.
- **No lighting effects:** no glow, rim light, cast or ground shadow, or coloured mood light, and no shade painted onto the magenta mannequin.
- **No room:** nothing from the office screenshot (no walls, desk, furniture, floor, window or its lighting); the background is always flat green.
- **Nothing hanging behind the body** on hair, headwear and accessory layers (it can't be seen from the front and it lands in front of the outfit).
""")

# ---------------------------------------------------------------- premade set-up (section 11)
RULES_P = RULE_BULLETS.format(likeness="")
def wears(x):
    s = P[x["place"]]["wardrobe"][x["g"]]["signature"]
    return f"{s['label']} ({SLOTWORD[s['slot']]})"
w(f"""## 11. Premade characters (set-up and cast)

Premade characters are drawn **whole**: one finished image per expression (neutral, happy, angry, worried), on the lined-up base figure `premadebase_[m/f]_skin[N].png` that Claude makes after Batch 2 (the approved recoloured body with the approved head, aligned to the landmarks). Claude registers every premade image to the same landmarks as the layers before delivering it, so its place behind the desk and the passport crop match the generated travellers. The game swaps the four images as they talk.

**A second Project.** Block A forbids any likeness of a real person, draws every hair in medium brown and works on a magenta mannequin; premades are real people, drawn whole, in their real hair colour. So they get their own Project:

1. Create a second ChatGPT Project called **"Time Sorter Premades"**.
2. Paste **Block P** (below) into its instructions.
3. Start one new chat per character inside it. Every chat inherits Block P, so nothing is pasted by hand.

All ten lived and died centuries ago, none was a ruler, and no photograph of any of them exists.

### Block P: paste into the premades Project's instructions

```
You are drawing the named historical characters of "Time Sorter", a game set in a 3D office in which the player is a clerk at a time-travel border desk. The travellers are flat 2D figures standing at the desk. Each character is ONE whole image: a respectful portrait based on period descriptions, not a photo likeness. Every one of them lived and died centuries ago, and none was a ruler.

IN THE GAME
{GAME_BULLETS}

STYLE
{STYLE_BULLETS}
{RULES_P}

CANVAS (every image)
{CANVAS_BULLETS}

CHARACTERS
- Draw each character on the attached base figure, keeping its pose, size and position, with a new face, hair, clothing, headwear and accessories.
- Draw hair, beards and wigs in the colour the description gives (grey for an elderly character).
- Dress each character exactly as described: the civilian dress of their own place and time.
- Draw every face in the shared style, but keep the features its description gives (such as a snub nose, full lips, a firm jaw or the lines of age): they make each person recognisable.
- Each character's first image has a neutral expression. When I ask for happy, angry or worried, change only the expression: anime-expressive but dignified, because these are real people. Show feeling through the eyes, brows and mouth only; no comic distortion and no manga symbols.
```

**Rules for every premade:** the same style as the generated travellers (section 2: one cast, with the same eyes, line, shading and colour treatment; Claude compares each premade with the pilot's Athens travellers at the desk, and one that stands out is redrawn), and the same canvas, neutral lighting, green background and "do not draw" rules as everyone else; period-accurate civilian dress of their claimed place and moment, **including that place's Costume Guide item for their gender** (the Look menu lists a premade's whole picture as one garment, "Period dress", valued with the claim's Costume Guide entry, and from day 3 the player compares dress with the Guide, so an honest premade must look like its row, and the item must show above the desk); anime-expressive but dignified expressions (prompt 9.11); the age shown below; hands open and empty. Every description was checked against its place's ChatGPT DO NOT DRAW line and contradicts nothing in it; paste the premade's own "Do not draw" line with prompt 9.10.

**The cast** (Amendment A1: about half women, at most ten). The drawing descriptions are in Batches 3, 6 and 8 (sections 15, 18 and 20).

| id | Name | Gender | Claims | Wears (Costume Guide item) | Age | Born (for the game) | In the game |
|---|---|---|---|---|---|---|---|
""" + "\n".join(f"| `{x['id']}` | {x['name']} | {'woman' if x['g'] == 'f' else 'man'} | {P[x['place']]['displayName']} | {wears(x)} | {x['age']} | {x['born']} | {x['sched']}" + (f"; impostor from {x['true_place'].split(' ')[0]}" if x['true_place'] else '') + " |" for x in PREMADES) + """

Alternates if Saleh wants to swap someone: **Alessandra Macinghi Strozzi** (Florentine Republic, born 1407), who was in the cast until the v2 review: in 1439 she was a widow, and a widow's veil and dress contradict both Florence's Costume Guide item (the ghirlanda) and its DO NOT DRAW line (no women's veils), so her art would look disguised. She can come back if that mismatch is accepted knowingly (Appendix E). Also Cai Lun (Eastern Han Luoyang, born c. 62) and Zeami Motokiyo (Muromachi Kyoto, born 1363; the only way to give Japan a premade before day 4). Caritas Pirckheimer (Renaissance Nuremberg) was left out: she was an abbess, and the rules forbid religious vestments.
""")

# ---------------------------------------------------------------- batch order table
POOL2 = ["aspasia", "banzhao", "arib", "khwarizmi"]
POOL3_NEW = ["gutenberg", "leonardo", "lanyer", "gallerani"]
BATCHES = [
    ("1", "Pilot: base figures + Periclean Athens + stress test", "day 1", "proves the stack, the key and the style before anything else"),
    ("2", "Skin tones and faces", "every day", "every traveller has a real body and face"),
    ("3", "Story premades: Senenmut, Socrates", "days 1-2", "the two scripted story visitors"),
    ("4", "Day 1 places (Ancient Egypt, Iraq, Italy)", "day 1", "day 1 fully drawn"),
    ("5", "Day 2 places (Ancient China and Britain; Medieval Egypt, Iraq, Greece, Italy, China, Britain)", "day 2", "day 2 fully drawn"),
    ("6", "Day 2-3 premades: " + ", ".join(PRE[i]["name"] for i in POOL2), "days 2-3", "the random premade pool of day 2"),
    ("7", "Day 3 places (Ancient and Medieval Japan and Germany; all Early modern)", "day 3", "day 3, the first day with dress tells"),
    ("8", "Day 3 premades: " + ", ".join(PRE[i]["name"] for i in POOL3_NEW), "day 3", "the rest of the premade pool"),
    ("9", "Industrial era", "day 4", ""),
    ("10", "Modern era", "day 5", ""),
    ("11", "The Future", "day 6", "only appears once a country leads history"),
    ("12", "The 2150 accessory kit", "day 2", "a 2150 citizen's costume error: one kit item over a right costume (the neutral Future outfits of Batch 11 are the other, 2150 clothes)"),
]
w("""## 12. Batch order

The game runs on placeholder shapes until art arrives, and every file replaces its placeholder by name. So the order below puts first what the player sees first: the pilot proves the method, bases give every traveller a real body, then each game day in turn. Dress tells start on day 3, so the leak items of days 1 to 3 matter most.

| Batch | What | Game day | Why now |
|---|---|---|---|
""" + "\n".join(f"| {b} | {w_} | {d} | {y} |" for b, w_, d, y in BATCHES) + """

Batch 1 has to come first and Batch 2 second (premades and everything else need the approved base figures). After that the order is a recommendation: skipping ahead never breaks anything. Within a batch, send each place to Claude as soon as it is done. The UI art in `UI_ART_RULES.md` is a separate track in its own ChatGPT Project; start it any time after Batch 1.
""")

# ---------------------------------------------------------------- Batch 1
pilot_raws = place_raws(PILOT_PLACE)
stress_raws = []
for pid, fn in STRESS:
    for g in GENDERS:
        for f_, ks in raw_files(pid, g):
            if f_ == fn:
                stress_raws.append((f_, ks))
# Bodies 2-5 are recoloured from the approved skin-1 body (review 2 finding 9), so skin 1's raw file produces them.
b1_raws = [(f"base_{g}_skin1_facea.png", [body_key(g, s) for s in SKINS] + [head_key(g, 1, "a")]) for g in GENDERS] \
          + pilot_raws + stress_raws
counts.append(("Batch 1: pilot", len(b1_raws), len(keys_of(b1_raws))))
w(f"""## 13. Batch 1: pilot ({len(b1_raws)} images)

This small batch proves the pieces stack, the cut-out works and the style is right before anyone makes hundreds of images. Folder: `Raw/batch01-pilot/`.

**Step 1.** Use prompt 9.1 twice, for skin tone 1, face a:

- `base_m_skin1_facea.png`
- `base_f_skin1_facea.png`

**Send to Claude.** Claude resizes and re-centres both figures onto the guide's landmarks (top of head y=260, chin y=424, soles y=1490) before making the mannequins, so don't reject a figure just because it is a little big or off-centre. Do reject one whose head is too big for its body (easy in this style: with the top of the head and the soles on their lines, the chin sits clearly below the CHIN line), because the head size is fixed. Claude splits each figure into a body and a head, recolours the approved body to the four other skin swatches (section 7), and sends back `mannequin_m.png` and `mannequin_f.png`. Keep them in your art folder.

**Step 2.** In a new chat, send this PAIR block with prompt 9.0, then use prompts 9.4 to 9.8 for each file below it. The style card does not exist yet, so for this pilot only, attach the mannequin alone and leave the style card out of each prompt's "Attached:" line.

{place_section(PILOT_PLACE, heading_level="####", send_line=False)}
**Step 3 (stress test).** These try the risky cases (a hood over the head that hides all the hair, hair under a veil, a large dark-green area, a wig hanging behind the shoulders). In a new chat for each PAIR block (they are in Batches 4, 5 and 7): paste the Egypt medieval PAIR block and make `outfit_f_egypt_medieval.png` (add to the prompt: "Make the qamis deep bottle green.", so the green case is really tested), `hair_f_egypt_medieval.png`, `headwear_f_egypt_medieval.png`; paste the Japan ancient PAIR block and make `accessory_m_japan_ancient.png`; paste the Egypt ancient PAIR block and make `hair_f_egypt_ancient.png`. They count toward their own batches, which skip them.

**Send to Claude.** Claude stacks everything in Unity: the figure behind the desk in the game's office, the passport photo, and the whole canvas. If the pieces line up and the style is right, go on. If not, we fix the prompts before making more.

**Step 4 (style card).** Once the pilot is approved, Claude makes `style_card.png`: the approved Athens outfits in greyscale on the mannequin, with a small swatch of folds showing their one hard-edged shade tone, a pattern band and the outline weight, all on the green background. From now on every garment prompt (9.4 to 9.8) attaches it next to the mannequin, and every premade prompt (9.10) next to the base figure, as a style reference only.
""")

# ---------------------------------------------------------------- Batch 2
b2_raws = [(f"base_{g}_skin{s}_facea.png", [head_key(g, s, "a")]) for g in GENDERS for s in SKINS[1:]] + \
          [(f"head_{g}_skin{s}_face{v}.png", [head_key(g, s, v)]) for g in GENDERS for s in SKINS for v in FACES[1:]]
counts.append(("Batch 2: skin tones and faces", len(b2_raws), len(keys_of(b2_raws))))
w(f"""## 14. Batch 2: skin tones and faces ({len(b2_raws)} images)

Folder: `Raw/batch02-base/`. One chat is fine here (no costumes to mix up).

- **8 base figures** with prompt 9.2: `base_[m/f]_skin[2-5]_facea.png`. Claude keeps only the head from these; the bodies for skins 2 to 5 were already recoloured from your approved skin-1 body to the same swatches, so they match the mannequin exactly.
- **30 heads** with prompt 9.3, faces b, c and d for every skin tone and both genders: `head_[m/f]_skin[1-5]_face[b/c/d].png`. Each image shows the whole figure.

**Send to Claude** after the first skin tone's faces (so a drifting head is caught early), then the rest.

How Claude processes and accepts the heads (every garment is fitted to the face-a marks, so a head that drifts would misalign beards, glasses and close caps):

1. Claude cuts each head out and warps it so its eyes, nose, mouth, ears and skull outline land on the face-a mannequin's marks, then matches its colour to the body.
2. A head whose skull outline or jaw is more than a few pixels off the face-a outline after that is rejected: press Regenerate for it.
3. **Stacking check** before Batch 2 is done: Claude stacks the pilot's beard and petasos and the stress test's hood on every new head, with face d as the hardest case. It repeats the check when the first glasses (Batch 10: Iraq and Germany Modern) and the first close caps (the guapimao in Batch 9, the cloche in Batch 10) arrive.
4. Claude then makes the premade base figures `premadebase_[m/f]_skin[1-5].png` (the recoloured body with the approved face-a head, aligned to the landmarks) for Batches 3, 6 and 8.

From here on every traveller in the game has a real body and face.
""")

# ---------------------------------------------------------------- premade helpers
def premade_block(pid):
    x = PRE[pid]; p = P[x["place"]]
    rv = f"\n\nReview only (Saleh and Claude; never paste into ChatGPT): {x['review']}." if x.get("review") else ""
    return (f"#### {x['name']} (`{x['id']}`)\n\n"
            f"- Claims: {p['displayName']} ({x['place']}), {yr(p['year'])}. Age at the desk: about {x['age']} (face band {x['face']}). "
            f"Base figure to attach: `premadebase_{x['g']}_skin{x['skin']}.png`.\n"
            f"- Wears the claimed place's Costume Guide item: {wears(x)}.\n"
            f"- In the game: {x['sched']}" + (f"; an authored liar, really from {x['true_place']}" if x['true_place'] else "") + ".\n\n"
            f"```\n{x['look']}\n```\n\n"
            f"Do not draw: {x['avoid']}.{rv}\n\n"
            f"Files: " + ", ".join(f"`{premade_key(pid, e)}.png`" for e in EXPRESSIONS) + "\n")

def premade_raws(ids):
    return [(f"{premade_key(i, e)}.png", [premade_key(i, e)]) for i in ids for e in EXPRESSIONS]

def premade_batch(num, secno, ids, why):
    raws = premade_raws(ids)
    counts.append((f"Batch {num}: premades ({', '.join(PRE[i]['name'] for i in ids)})", len(raws), len(raws)))
    w(f"""## {secno}. Batch {num}: premade characters: {', '.join(PRE[i]['name'] for i in ids)} ({len(raws)} images)

{why} Make them in the **Time Sorter Premades** Project (section 11), one new chat per character. Use prompt 9.10 for the neutral image, then 9.11 three times. Folder: `Raw/batch{int(num):02d}-premades/`. Send each character's four images to Claude as soon as they are done.

""" + "\n".join(premade_block(i) for i in ids) + "\n**Send to Claude.**\n")

def places_batch(num, secno, title, ids, intro, skip=()):
    raws = [r for pid in ids for r in place_raws(pid, skip)]
    counts.append((f"Batch {num}: {title}", len(raws), len(keys_of(raws))))
    w(f"""## {secno}. Batch {num}: {title} ({len(raws)} images)

{intro} Folder: `Raw/batch{int(num):02d}-{title.split(' (')[0].lower().replace(' ', '-')}/`. Start a new chat for each place, send its PAIR block with prompt 9.0, then make each file in its list. **Send each place's files to Claude as soon as that place is done**, and wait for the go-ahead on the first place of the batch.

""" + "\n".join(place_section(pid, skip) for pid in ids) + "\nWhen the last place is in, Claude runs the batch check in Unity.\n")

# ---------------------------------------------------------------- Batches 3..10
premade_batch("3", 15, ["senenmut", "socrates"],
              "The two story visitors: Senenmut is always the third traveller of day 1, and \"Socrates\" the sixth of day 2.")
places_batch("4", 16, "Day 1 places", DAY1,
             "Day 1 sends travellers from Ancient Egypt, Iraq, Greece and Italy. Greece was the pilot.", STRESS_FILES)
places_batch("5", 17, "Day 2 places", DAY2,
             "Day 2 adds the Medieval era and China and Britain.", STRESS_FILES)
premade_batch("6", 18, POOL2,
              "The random premade pool of days 2 and 3 (each ordinary slot has a small chance of one of them).")
places_batch("7", 19, "Day 3 places", DAY3,
             "Day 3 adds the Early modern era, Japan and Germany, and it is the first day on which a liar's dress can leak. (Tokugawa Edo is closed by a rule on day 3, but rule-breakers still claim it, so it is needed.)", STRESS_FILES)
premade_batch("8", 20, POOL3_NEW, "The rest of the day-3 premade pool.")
places_batch("9", 21, "Industrial era", DAY4, "Day 4 adds the Industrial era (1750 to 1900).")
places_batch("10", 22, "Modern era", DAY5, "Day 5 adds the Modern era (1900 to 2000).")

# ---------------------------------------------------------------- Future (Batch 11) + premade setup (section 22)
fut_raws = [("outfit_m_neutral_future.png", ["outfit_m_neutral_future"]), ("outfit_f_neutral_future.png", ["outfit_f_neutral_future"]),
            ("hair_m_neutral_future.png", [f"hair_m_neutral_future_{c}" for c in COLOURS]),
            ("hair_f_neutral_future.png", [f"hair_f_neutral_future_{c}" for c in COLOURS]),
            ("facialhair_m_neutral_future.png", [f"facialhair_m_neutral_future_{c}" for c in COLOURS])] + \
           [(f"outfit_{g}_{c}_future.png", [f"outfit_{g}_{c}_future"]) for c in COUNTRIES for g in GENDERS]
counts.append(("Batch 11: the Future", len(fut_raws), len(keys_of(fut_raws))))
fblocks = []
for c in COUNTRIES:
    f = FUT[c]
    fblocks.append(f"""### Future shaped by {CNAME[c]}: {f['displayName']}

Costume Guide entry (proposed): **{f['cultureValue']}** (the whole outfit; a Future look never leaks)

```
PAIR: {c}_future ({f['displayName']}, 2150: the Future after {CNAME[c]} came to dominate history)
Restyle the approved neutral Future outfits (same cut, same fit) for both the man and the woman with these motifs:
{f['motifs']}
Keep it wearable civilian clothing, clearly of 2150 yet clearly {CNAME[c].split(' /')[0]}-inspired. No flags, emblems, insignia, letters or readable text; nothing that glows; nothing see-through; no holes cut through the fabric (lattice, perforation and laser-cut motifs are printed or stitched on solid cloth).
```

Files: `outfit_m_{c}_future.png`, `outfit_f_{c}_future.png` (prompt 9.9). Hair and facial hair: the shared neutral Future items. Import labels (Appendix D): outfit "{f['cultureValue']}", hair "short textured crop" / "sleek low bun", facial hair "short trimmed beard", all with `artNation: "neutral"` on the hair and beard.
""")
w(f"""## 23. Batch 11: the Future ({len(fut_raws)} images)

The Future (2150) is the player's own time. Its look changes with history: whichever country dominates the past, its Future place becomes a destination, and its travellers dress in that culture's Future style. There is no Future traveller before a country leads, so this batch comes last. Folder: `Raw/batch11-future/`.

Make the neutral Future first with prompts 9.4 to 9.6 (it sets the cut and the shared hairstyle and beard). Send the neutral Future to Claude and wait for the go-ahead. Then make each culture Future from the approved neutral outfit with prompt 9.9, and send each one to Claude as soon as it is done. Culture Futures are outfits only: they reuse the neutral hair and beard, and have no headwear or accessory. A motif that describes a shape (cape-sleeves, one-shoulder drapes, wraps) is suggested with panels, seams and trims on the neutral cut, never a new silhouette (issue 10).

### Neutral Future (the shared base)

```
PAIR: neutral_future (the Future, the player's own time, 2150)

MAN
- Outfit: clean, practical civilian clothes in soft technical fabrics: a hip-length jacket with a soft stand collar kept below the chin and simple geometric seams, over a plain fitted top, slim straight trousers, and flat low-profile shoes. Muted slate grey, sand and slate teal-blue.
- Hair: short, neat, textured crop.
- Facial hair: a short, neatly trimmed beard and moustache.

WOMAN
- Outfit: the same practical future style: a long panelled coat-dress with a soft stand collar kept below the chin and simple geometric seams, over slim trousers, with flat low-profile shoes. Muted slate grey, sand and slate teal-blue.
- Hair: sleek, smooth hair drawn back into a low bun at the nape.

MUST READ AT A GLANCE: the soft stand collar and clean geometric seams in slate grey, sand and slate teal-blue.
DO NOT DRAW: armour, helmets, visors, glowing parts, robot parts, weapons, logos, letters or readable text.
```

Files: `outfit_m_neutral_future.png`, `outfit_f_neutral_future.png` (the base of every culture Future and, since v2.3, the present's clothes: import labels "{PRESENT_CLOTHES['m']['outfit']}" / "{PRESENT_CLOTHES['f']['outfit']}"), `hair_m_neutral_future.png`, `hair_f_neutral_future.png`, `facialhair_m_neutral_future.png`

""" + "\n".join(fblocks) + "\n**Send to Claude.**\n")

# ---------------------------------------------------------------- the 2150 accessory kit (Batch 12, v2.3)
kit_raws = [(f"accessory_{g}_neutral_future_{k['variant']}.png", [f"accessory_{g}_neutral_future_{k['variant']}"]) for g in GENDERS for k in KIT]
counts.append(("Batch 12: the 2150 accessory kit", len(kit_raws), len(keys_of(kit_raws))))
w(f"""## 24. Batch 12: the 2150 accessory kit ({len(kit_raws)} images)

The travellers of 2150 must dress for the time they are going to. One who slips wears a single item of their own time over an otherwise right costume, and the player catches it by comparing it with the Costume Guide. These are those items: one small set, drawn for the man and for the woman. Folder: `Raw/batch12-2150-kit/`.

```
PAIR: neutral_future_kit (the 2150 accessory kit, the player's own time)
Both: small, clean, practical 2150 gadgets in slate grey, sand and slate teal-blue, like the neutral Future outfits. Each stands on its own, over any period costume, and reads at a glance above the desk.
""" + "\n".join(f"- {k['label'].upper()}: {k['look']}." for k in KIT) + """
MUST READ AT A GLANCE: each item's clean geometric shape in slate grey and teal-blue, unlike any period accessory.
DO NOT DRAW: glowing parts, screens with pictures, letters, numbers or readable text, logos, cables, anything held in the hand.
```

For each item, send the PAIR block once, then prompt 9.8 (Accessory) with the item's name, once for the man and once for the woman.

Files: """ + ", ".join(f"`{fn}`" for fn, _ in kit_raws) + """. Import labels (world_source.json `present.kit`): """ + "; ".join(f'"{k["label"]}" (`{k["variant"]}`)' for k in KIT) + """.

**Send to Claude.**
""")

# ---------------------------------------------------------------- checklist + totals
total_raw = sum(c[1] for c in counts)
total_keys_batches = sum(c[2] for c in counts)
w("""## Checklist before you send a place to Claude

- Every file is a 1024 x 1536 portrait PNG (not square) saved with the download button, named exactly as listed.
- The background is one flat green (no checkerboard, floor or shadow), and the magenta figure is still there at the same size and in the same place as in the mannequin file. Open both and flip between them: nothing should jump.
- Nothing touches or runs off any edge of the image: there is green on all four sides, including above the tallest hat.
- No pink or green tint inside white or pale fabric; nothing see-through; no holes through the fabric.
- No shade or shadow painted onto the magenta mannequin.
- Headwear that hides all the hair comes down to the hairline (no bald scalp showing under it).
- Each accessory stands on its own (no belt, sash or cloak drawn for it to hang from).
- Lighting is flat and even: no bright side, no glow, no shadow on the ground.
- No text, logos, flags, badges or insignia anywhere; coins are plain discs.
- The LEAK ITEMS are clearly visible, match the description and sit on the head, face, neck, shoulders or upper chest; the collar, shoulders and chest read clearly (the desk hides the rest in the game).

## Totals

| Batch | Images you make | Game files Claude makes from them |
|---|---|---|
""" + "\n".join(f"| {k} | {r} | {n} |" for k, r, n in counts) + f"""
| **Total** | **{total_raw}** | **{total_keys_batches}** |

The game files include the five baked hair colours, the hair-back layers and the four premade expressions. `coverage.json` lists every game file.
""")

# ---------------------------------------------------------------- appendices
fixlog = W["fixLog"]
from collections import Counter
st = Counter(x["status"] for x in fixlog)
w("""## Appendix A. The 68 data fixes (brief_review.json `fixes`)

All 68 are in the PAIR blocks above. "Applied" means the reviewer's text is still used exactly as written (`tools/data_v2.py` compares every field with the reviewer's text). "Applied, then amended" means the fix went in and a later rule changed its text; the note names that rule: a v1 practicality issue (the piece-4 spec's rule R23: where a data fix and a later practicality issue conflict, the issue wins) or a finding of the v2 review (Appendix E). The other statuses say where an issue replaced or merged the fix. Totals: """ + ", ".join(f"{n} {k}" for k, n in sorted(st.items(), key=lambda kv: -kv[1])) + """.

| # | Place | Field | Status | Note |
|---|---|---|---|---|
""" + "\n".join(f"| {x['n']} | {x['place']} | {x['field']} | {x['status']} | {x['note']} |" for x in fixlog) + "\n")

ISSUES = [
    (1, "ChatGPT redraws the whole image; pixel landmarks", "Applied: section 4 text, setup step 6 (never tweak, Regenerate), Batch 1 Step 1 note (Claude re-centres), checklist line 2 (flip between files). Prompt 9.1 refers to the guide's lines, not pixel values."),
    (2, "Images in Project files are not used; finished pieces get copied", "Applied: setup step 3 and the files list (attach what each prompt's Attached line names)."),
    (3, "PAIR block alone makes ChatGPT draw; one chat per era drifts", "Applied: setup step 5 (new chat per PAIR), prompt 9.0 (reply only READY), Block A's last line, every batch intro."),
    (4, "Purple, pink and green get eaten by the key", "Applied: Block A colour rule and every listed line; the same rule was also applied to every other pink, rose, purple and light-green word in the looks (Appendix C). The v2 review added turquoise and teal (Appendix E, finding 33)."),
    (5, "See-through fabric keys badly", "Applied: Block A opacity rule; iraq_medieval veil (fix 18), sheer chemise (fix 39), trinzale deleted, sheer stockings to bare legs, Future China and Britain edits; the Ottoman Egypt tartur's gauze veil was also removed (Appendix C); checklist line 4. The v2 review removed 'chiffon' and the Future motifs that cut holes through fabric (findings 29 and 33)."),
    (6, "Garments over the head placed in the outfit", "Applied: Mamluk izar (fixes 13-14, issue wording), Ottoman Iraq 'abaya split into outfit (from the shoulders) and headwear (head part) per R23, Kingdom of Iraq (fixes 57-58), prompt 9.4 (collars below the chin), 1843 collar points."),
    (7, "Hair 'hidden under' headwear", "Applied: prompt 9.5 as written. Every remaining hair line that named a headwear item was rewritten without it (Appendix C), the same fix the reviewer made in fixes 38, 43, 45, 55, 56 and 59. Headwear that hides all the hair now carries `covers` (Appendix D)."),
    (8, "Things that hang behind the body land in front of the outfit", "Applied: the hair-back layer (section 1, section 6; Claude splits it), prompts 9.7 and 9.8, the 'adhaba tail in front, the taylasan ends down the front (merged with fix 17), the tarha over both shoulders (R23), the new Ottoman Egypt women's MUST READ. The v2 review set one test for the hair-back flag (section 6, Appendix E finding 8)."),
    (9, "Skin variants redrawn; heads pasted across images", "Applied: prompts 9.2 and 9.3, Batch 2 text, section 6 body row. The v2 review added skin swatches (section 7) and the head alignment and stacking check (Batch 2)."),
    (10, "Future motifs break the layer rules", "Applied: prompt 9.9, the Batch 11 intro, and every motif edit (plus: Britain's 'racing green' to bottle green, China's celadon to dull grey-green, Iraq's cuneiform texture made abstract). Future hair is shared (piece 5 R18)."),
    (11, "Accessories tied to another layer or too small", "Applied to every line the issue listed, and after the v2 review to every other accessory line: about 19 more hung from, tucked into, pinned or closed an outfit item, two of them leak items (Appendix E, findings 3, 15 and 16). Purses, cases, fans and cords that hang from an outfit's own belt are now part of that outfit, and `tools/data_v2.py` checks every accessory line. Where the research has no larger authentic item, the accessory is none (Appendix C)."),
    (12, "Headwear drawn tight to the bald scalp", "Applied: prompt 9.7 as written."),
    (13, "Tiraz 'pseudo-script' vs the no-text rule", "Applied: Block A text rule and the three tiraz/'isaba lines. The Abbasid DO NOT DRAW still asked for pseudo-script until the v2 review fixed it (Appendix E, finding 19)."),
    (14, "Kohl on the face layer", "Applied: fix 2's MUST READ; heads have no makeup (section 6, prompt 9.1, section 10)."),
    (15, "Grey undershirt shows under low necklines", "Applied: prompt 9.1 (bandeau and mid-thigh shorts), section 6 body row."),
    (16, "Gold ornaments recoloured with the hair", "Applied: Block A hair-ornament rule (after the v2 review: never red); the ornaments to mask are listed per place ('Notes for Claude's processing') and in coverage.json."),
    (17, "The pilot tests none of the risky cases", "Applied: Batch 1 Step 3 (the same five files, with the qamis forced to bottle green). The pilot pair itself moved from Victorian Britain to Periclean Athens so it counts toward day 1; v1's batch counts no longer apply."),
    (18, "Premades vs 'no likeness'; expressions drift", "Applied: premades have their own Project with Block P (section 11, after the v2 review), prompts 9.10 and 9.11, the note after 9.11."),
    (19, "File names for Future, bases, heads, premades", "Adapted: the game's key grammar is now the single naming rule (section 8). Future files follow the same order as every era (`outfit_m_egypt_future`), not v1's flipped order (Appendix C)."),
    (20, "Unusable safe-box check; checkerboard", "Applied: checklist line 3, Block A (no checkerboard)."),
    (21, "Jewellery and badges in outfits", "Applied: section 6 outfit row, the La Tene brooch now a plain bronze pin, the shasho badge deleted."),
    (22, "JPG and screenshots", "Applied: setup step 7, checklist line 1."),
]
w("""## Appendix B. The 22 practicality issues (brief_review.json `issues`)

| # | Problem | How v2 handles it |
|---|---|---|
""" + "\n".join(f"| {n} | {p} | {h} |" for n, p, h in ISSUES) + "\n")

w("""## Appendix C. Rejected or adapted, with reasons (v1 review)

Nothing was rejected outright. These parts were adapted:

1. **Issue 19's Future file order** (`outfit_m_future_egypt`) is not used. The game's key grammar is `{layer}_{gender}_{country}_{era}` for every place, the Future included (piece-4 R27), and the shared Future hair uses the country token `neutral` (piece-5 R18): `hair_m_neutral_future_brown`. One rule is simpler than two.
2. **Issue 17's pilot pair.** The stress test is kept as written, but the pilot pair is Periclean Athens (day 1) instead of Victorian Britain (day 4), so the pilot's images count toward the first day the player plays. Athens still tests a hat over hair, a beard, a snood and a strap bag.
3. **Issue 11, accessories with no larger researched item.** The reviewer asked to replace small or dependent accessories "with a larger item from costume_research.json". The research offers one for three of them and none for four, so, as fix 5 did for the Athenian woman, no item was invented:
   - italy_ancient man: the signet ring is now **none**;
   - italy_ancient woman: pearl earrings became a **short necklace of large pearls** (the research notes pearls as the Roman status craze);
   - china_medieval woman: pearl earrings are now **none**;
   - china_earlymodern woman: earrings are now **none** (the research's jinbu pendant was dropped there because it looks like Japan's tasselled Nagoya-obi);
   - britain_earlymodern man: the single earring is now **none** (a merchant's gold chain would look like the German early-modern men's chain);
   - china_modern man: the pen in a pocket became the **plain khaki satchel** the research calls "the ubiquitous plain canvas bag of the time";
   - china_ancient man: the issue's embroidered silk pouch hangs from the outfit's sash, so after the v2 review it is part of the outfit and the accessory is **none** (Appendix E, finding 15).
4. **Fix 54 vs issue 11** (Meiji women's ribbon bow): issue 11 wins (R23 rule). The bow is now part of the hairstyle, masked before hair recolouring; the accessory is none. A bow tied to one hairstyle could never leak onto another. After the v2 review the bow is white, not deep red (red is too close to brown hair for the mask).
5. **Fixes 44 and 46 vs issue 6** (Ottoman Iraq 'abaya) and **fixes 41 and 42 vs issue 8** (Khedivate tarha): the issues win, as the piece-4 spec R23 already decided.
6. **Fix 17 vs issue 8** (taylasan): merged. The ends fall down the front (issue 8); it stays clear of the tiraz armbands (fix 17).
7. **Fix 31 vs issue 5** (trinzale net): issue 5 wins; the sheer net is deleted.
8. **Fix 14 vs issue 6** (Mamluk izar hood): same intent; the issue's wording (down both sides of the neck) is used.
9. **Extensions of the reviewer's own rules** (same reasons, lines the review did not list): every remaining pink, rose, purple or light-green colour word became dusty coral, deep wine red or a dark green (""" + str(sum(1 for e in W["editLog"] if "issue 4 rule, extended" in e["why"])) + """ edits in """ + str(len({(e["place"], e["field"]) for e in W["editLog"] if "issue 4 rule, extended" in e["why"]})) + """ item texts; "not the purple of high ranks" became "not the bright colours of high ranks"); the Ottoman Egypt chemise and the Ottoman Egypt tartur's hanging gauze veil (sheer, and hanging down the back); the Gugel's long tail down the back (fix 27 had already dropped it from the MUST READ); the New Kingdom sheath's note about sheer over-robes (it named what must not be drawn); """ + str(sum(1 for e in W["editLog"] if e["why"].startswith("issue 7 consistency"))) + """ hair lines that still named the headwear on top of them; the Ottoman Baghdad men's turban now has one colourway so its leak label names one item.
10. **v1's "the game recolours hair"** is replaced by baked colour variants made by Claude (piece-4 C13; art contract item 9).
""")

# ---------------------------------------------------------------- appendix D: import data
def cell(ww, k):
    lab = ww["labels"].get(k, "")
    if not lab:
        return ""
    sig = SLOTKEY_REV[k] == ww["signature"]["slot"]
    extra = []
    if k == "hair" and ww["wig"]: extra.append("wig")
    if k == "hair" and ww["back"]: extra.append("back")
    if k == "headwear" and ww["covers"]: extra.append("covers Hair")
    if k == "headwear" and ww["headwearHides"] == "top": extra.append("hides top")
    t = f"**{lab}** (leakable)" if sig else lab
    return t + (f" [{', '.join(extra)}]" if extra else "")
SLOTKEY_REV = {"outfit": "Outfit", "hair": "Hair", "facialHair": "FacialHair", "headwear": "Headwear", "accessory": "Accessory"}
rows = []
for p in W["places"]:
    for g in GENDERS:
        ww = p["wardrobe"][g]
        rows.append(f"| {p['id']} | {g} | {ww['signature']['slot']} | " + " | ".join(cell(ww, k) for k in ("outfit", "hair", "facialHair", "headwear", "accessory")) + f" | {p['cultureValue'] if g == 'm' else ''} |")
CONF = W["confusable"]
HID = W["confusableHidden"]
from collections import defaultdict
hid_by = defaultdict(list)
for x in HID:
    hid_by[(x["b"], x["gender"])].append(x["a"])
covers_list = [f"{p['id']} {g} ({p['wardrobe'][g]['labels']['headwear']})" for p in W["places"] for g in GENDERS if p["wardrobe"][g]["covers"]]
w("""## Appendix D. For the piece-4 content import (not for ChatGPT)

The piece-4 plan authors each place's `wardrobe` block in `world_source.json` by hand from the corrected research (its R23). These are the proposals this brief is drawn to, for Saleh's review. The full data is `tools/wardrobe_v2.json`: the corrected text of every item, and each place's `worldSourceWardrobe` already in the spec's format (section 2.13: `{ signature, outfit: {label}, hair: {label, wig?, back?}, facialHair, headwear: {label, covers?}, accessory }`, with `leakable: true` on each signature item). Items marked "none" in a PAIR block are absent (no key).

Checked by `tools/data_v2.py`: every item label is at most 24 characters, ASCII and without "/"; every Culture value is at most 28 characters and unique; the full `Looks.LabelProblems` rule per gender (no two places share a signature label, and no item of any place, in any slot, carries another place's signature label); every accessory stands on its own (no "tucked", "hanging from the sash/belt", "pinning", "closing", "small").

**Every item, per place and gender** (bold = the signature item, which is `leakable`; every signature passes spec C7: an item, readable at the desk's head size (section 1), standing on its own; since v2.1 every leak item sits on the head, face, neck, shoulders or upper chest; flags in brackets):

| Place | g | Signature slot | Outfit | Hair | Facial hair | Headwear | Accessory | Culture value |
|---|---|---|---|---|---|---|---|---|
""" + "\n".join(rows) + """

**`covers`.** Headwear that hides all the hair carries `covers: ["Hair"]`, so `Looks.Compose` draws no hair under it and `CanLeak` row 5 refuses a hair leak onto that claim: """ + "; ".join(covers_list) + """. The PAIR block tells ChatGPT to bring such headwear down to the hairline. Headwear that hides only the top of the head ("hides top": most hats and caps, and the veils and scarves that show the front hair, such as Abbasid Baghdad's veil with its side-curls and post-war Britain's headscarf) has no `covers`, or the honest hair around it would vanish.

**Confusable pairs to author** (`looks.confusable`; a leak between these places in this slot would be hard to see, so `Looks.CanLeak` refuses it). Every signature item was checked against every other place's same-gender item in its slot; `tools/confusable_candidates.txt` prints that whole grid for Saleh's review.

| Place A | Place B | Slot | Gender | Why |
|---|---|---|---|---|
""" + "\n".join(f"| {x['a']} | {x['b']} | {x['slot']} | {x['gender']} | {x['why']} |" for x in CONF) + """

**Generated pairs: a hair signature hidden under a hat** (""" + str(len(HID)) + """ entries in `wardrobe_v2.json` `confusableHidden`, slot `Hair`). The telling part of these hairstyles sits on the top or upper side of the head, so under a claim's hat or cap ("hides top") the leak would be invisible although the proof would land. Mizura (loops at ear level) stays visible under a hat and needs none.

""" + "\n".join(f"- {P[b]['wardrobe'][g]['signature']['label']} ({b}, {GWORD[g]}) is refused under the headwear of: {', '.join(sorted(v))}." for (b, g), v in sorted(hid_by.items())) + """

These generated pairs work with the spec as written. A simpler alternative for Saleh (open question 2 in Appendix E): one extra `CanLeak` row, "a Hair signature is refused when the claim wears headwear that hides the top of the head", with a `hidesTop` flag on those headwear items instead of """ + str(len(HID)) + """ pairs.

Not authored, with the reason: the spec's section 2.14 asks for "greece/italy/germany Industrial men's facial hair (#19) at least". No Industrial man's signature is facial hair (theirs are the fesi, the silk scarf and the Homburg), so a facial-hair leak can never happen between those three places; the pairs would be unreachable. The Modern walrus moustache, which can leak onto them, has its own pairs above.

**Future places** (piece 5, section 2.14 and R18): signature slot `Outfit` for both genders (a Future home never leaks), hair and facial hair with `artNation: "neutral"`, no headwear or accessory. Each is in `wardrobe_v2.json` `future[].worldSourceWardrobe` and in `coverage.json` `wardrobeFlags`.

| Place | Culture value = outfit label (both genders) | Hair (m / f) | Facial hair (m) |
|---|---|---|---|
""" + "\n".join(f"| {c}_future | {FUT[c]['cultureValue']} | short textured crop / sleek low bun (`artNation: neutral`) | short trimmed beard (`artNation: neutral`) |" for c in COUNTRIES) + """

**Premades** (the spec's section 2.14 cast table, rebalanced per Amendment A1; birth dates are inside each place's birth years, invented where history gives only a year; record notes are ASCII and under 120 characters):

| id | name | gender | place | truePlace | birthDate | archetype | recordNote | schedule |
|---|---|---|---|---|---|---|---|---|
""" + "\n".join(f"| {x['id']} | {x['name']} | {x['g']} | {x['place']} | {x['true_place'].split(' ')[0] if x['true_place'] else ''} | {x['born']} | {x['role']} | {x['note']} | {x['sched']} |" for x in PREMADES) + f"""

Day pools this implies: day 1 forced `senenmut` (slot 3); day 2 forced `socrates` (slot 6), pool `{", ".join(POOL2)}`; day 3 pool `{", ".join(["socrates"] + POOL2 + POOL3_NEW)}` ({1 + len(POOL2) + len(POOL3_NEW)} ids). Every claim and true place is in its day's world, and no day-3 rule forbids a claim (none claims Northern Song Kaifeng or Tokugawa Edo). Cecilia Gallerani's birth year, 1473, lies in Sforza Milan's range (1425 to 1477, Amendment A1). The Senenmut dialog and the Socrates intro stay as the spec has them.

**Knock-on edits in the other documents** (they still describe the old cast; to make when the spec is amended):

1. Piece-4 spec section 1.4 ("Who"): replace the list of eight men with the ten above.
2. Piece-4 spec section 2.14: the premade table (all ten, with genders; Cai Lun, Zeami and Shakespeare leave the cast), the days (day 2 pool `{", ".join(POOL2)}`; day 3 pool as above), the notes (ten premades; longest name still "Muhammad al-Khwarizmi", 21; every note under 120 characters) and the remark that the set "is all male", which goes. Its confusable sentence gets the reason above for not authoring pair #19.
3. Piece-4 spec C14 and R19 ("a starter set of eight", "Eight premades"): ten.
4. Piece-4 spec section 2.17: "8 `Premade_*.asset`" becomes 10.
5. Piece-5 spec R22, its section 1.7 note and its section 2.14 `days[]` line: days 4 to 6 carry day 3's pool of {1 + len(POOL2) + len(POOL3_NEW)} ids, not seven.
6. `docs/CHARACTER_ART_CONTRACT.md` item 7 (premades, when it is written): premades are made in their own Project with Block P and drawn on Claude's `premadebase` figures, not "in a separate chat without the no-likeness clause".
""")

# ---------------------------------------------------------------- appendix E: the v2 review
N_COVERS = sum(1 for p in W["places"] for g in GENDERS if p["wardrobe"][g]["covers"])
N_BACK = sum(1 for p in W["places"] for g in GENDERS if p["wardrobe"][g]["back"])
N_HAND_NEW = len(W["confusable"]) - W["confusableAddedV21"] - 9  # v2 had 9 pairs; v2.1 pairs are not the review's
N_HID = len(W["confusableHidden"])
N_HID_V2 = N_HID - N_HID_V21  # the review's generated pairs (v2.1 added pairs under two new headwear items)
REVIEW2 = [
    (1, "high", "Poster delivered at 1024 x 1408 with the placeholder's PPU 100 renders about 13 times too big", "Applied, then superseded (v2.1)", "UI_ART_RULES Posters and Delivery, coverage.json officeArt: the poster was scaled to 320 x 440 with its .meta at 400 pixels per unit (0.8 x 1.1 world units, the old booth's posters); the wallpaper's target was 1920 x 1080. v2.1: the 3D office has no poster (deferred) and the desktop wallpaper is 4:3, 1440 x 1080 (UI_ART_RULES)."),
    (2, "medium", "No `leakable` or `covers`; non-signature items have no labels", "Applied, adapted", f"Every item has a label and every signature is `leakable` (Appendix D); `data_v2.py` runs the full per-gender LabelProblems rule. `covers: [Hair]` on the {N_COVERS} headwear items that hide all the hair. Adapted: post-war Britain's headscarf and Abbasid Baghdad's veil show the front hair and side-curls, so they get no `covers` (the honest hair would vanish); instead, hair signatures read on the top of the head get generated confusable pairs under every such headwear."),
    (3, "medium", "Five signature items hang off another layer or are tiny", "Applied", "Bi-disc on its own cord round the neck, palm-wide; the Anglo-Saxon brooch face-wide at the shoulder with the cloak fastening itself; headphones round the neck with no cord or player; the Jugendstil brooch became a face-wide pendant on its own chain (label `Jugendstil pendant`); the lenza a finger-wide band with an eye-sized stone. The non-signature lines: finding 15."),
    (4, "medium", "Confusable list incomplete", "Applied", f"Every signature checked against every same-gender item in its slot (`tools/confusable_candidates.txt`); {N_HAND_NEW} hand pairs added, including all seven the finding lists (the Caesar-crop ones through `covers` and the generated 'hidden under a hat' set, because every one of those claims wears a turban or a hat), plus {N_HID_V2} generated pairs. Pair #19 is unreachable and not authored (reason in Appendix D). Two items were made clearer instead of paired: the petasos is undyed tan felt (apart from the black wide-brimmed hats of Bologna and Weimar) and the Ottoman Baghdad turban has bold checks."),
    (5, "medium", "Premade descriptions break their place's DO NOT DRAW", "Applied", "Leonardo clean-shaven (the Milanese fashion of the 1490s; his look at 43 is unknown), Gutenberg clean-shaven in the Gugel (no likeness survives), Senenmut's pleated over-kilt removed; Strozzi replaced (finding 6). Section 11 keeps 'the place's list applies' and says every description was checked against it."),
    (6, "medium", "Honest premades don't wear their place's Costume Guide item", "Applied", "Leonardo red berretta, Gutenberg Gugel, Lanyer hat over coif, Ban Zhao bi-disc, Socrates petasos worn on the head (slung would hang behind him, unseen). Strozzi is swapped for Cecilia Gallerani (Sforza Milan, born 1473, an A1 candidate), whose portrait shows exactly the lenza; Strozzi is listed as an alternate. The cast table names each premade's item."),
    (7, "low", "The premade chat inherits Block A's hair colour and mannequin rules", "Applied", "Block P (section 11) has no mannequin, medium-brown or READY rule, and says to draw hair in the colour described."),
    (8, "low", "Hair-back flag set inconsistently", "Applied", f"One test (section 6): back only for loose long hair, a wide wig or a spread curtain of plaits. {N_BACK} hair items are back; single plaits, pairs of braids and tails are hidden from the front and say so; the Ioannina and Iron Age braids now fall in front of the shoulders (the Ioannina headwear line already said the braids stay visible)."),
    (9, "low", "coverage.json does not match the brief", "Applied", "Future wardrobe entries added; one ornament key (`ornamentsToMask`); bodies 2 to 5 are produced by `base_[m/f]_skin1_facea` (recoloured to the section-7 swatches); the Costume Guide cover has its path and size (`Assets/Art/UI/Investigation/refbook_cover_culture.png`, 400 x 560)."),
    (10, "low", "Other documents still describe the old cast", "Applied", "Appendix D lists the knock-on edits (piece-4 sections 1.4, 2.14, C14, R19, 2.17; piece-5 R22: nine ids)."),
    (11, "low", "The brief never names docs/CHARACTER_ART_CONTRACT.md", "Applied", "Precedence paragraph at the top: the brief implements the contract; ART_ASSET_LIST.md section D, PRODUCTION_PLAN's character direction and `traveller.png` are retired."),
    (12, "low", "The guide invites crowns", "Applied", "The headroom label reads 'tall hats, headdresses and hair buns may extend up here' (finding 40 offered a shorter wording; this one keeps headdresses such as the tartur)."),
    (13, "info", "Check of the brief_review.json fixes", "No action", "Confirmed; the remaining gaps are the other findings."),
    (14, "high", "Block A and the first message name Papers, Please", "Applied", "Both describe the gameplay only; section 2 says never to name a game."),
    (15, "high", "About 19 accessory lines still hang from, tuck into, pin or close an outfit item", "Applied", "Purses, pen cases, fan, inro, notebook, hebao, toggle pins, fibula, clasps and the haori-himo moved into their outfits (the accessory is none); the pomander brings its own girdle; the Florentine brooch became a pendant on its own chain; the two signature items as in finding 3. `data_v2.py` fails on any accessory text matching the finding's pattern (plus 'at the collar' and 'where the')."),
    (16, "high", "Two signature brooches with no size", "Applied", "Finding 3; the women's Anglo-Saxon 'small' brooch is now a plain clasp in the outfit."),
    (17, "medium", "Block A for premades pasted by hand ten times", "Applied", "Block P lives in a second Project, 'Time Sorter Premades'; each character's chat inherits it."),
    (18, "medium", "Premades contradict the DO NOT DRAW lists; Leonardo's beard", "Applied", "Finding 5."),
    (19, "medium", "Abbasid pseudo-script and China Future's movable type", "Applied", "'any Arabic letters or writing, real or fake, on clothing'; 'a fine movable-type grid texture (blank squares, no characters)'."),
    (20, "medium", "Long DO NOT DRAW lists name slurs and hate symbols", "Applied, adapted", "Each list is split: ChatGPT gets the confusions and anachronisms; caricature, slur, hate-symbol and likeness items sit on a review line outside the PAIR block. 'Coolie hat' became 'conical straw hat (douli)'. Adapted: a few neutral guard items that ChatGPT is likely to add stay in its list in plain words (a badge on the jacket, a star on the cap, a monocle)."),
    (21, "medium", "Heads b to d drift; beards, glasses and caps misalign", "Applied", "Batch 2: Claude warps each head onto the face-a marks, rejects heads that stay a few pixels off, and runs a stacking check (face d hardest), repeated when the first glasses and close caps arrive."),
    (22, "medium", "No style anchor after Batch 1; unmeasurable style words", "Applied", "`style_card.png` after the pilot (greyscale Athens outfits plus a swatch), attached as 'STYLE REFERENCE ONLY' to every garment prompt; Block A: outline #3B2A20, shade the same hue about 20% darker, at most one small highlight."),
    (23, "medium", "Shade under brims lands on the magenta", "Applied", "MANNEQUIN RULE: never shade onto the mannequin; section 4 says why; Claude adds contact shade."),
    (24, "medium", "Errors surface only after a whole batch", "Applied", "Send each place (and each premade) as soon as it is done; wait for the go-ahead on the first place of every batch."),
    (25, "medium", "Walrus and curled-beard look-alikes", "Applied", "Eight walrus pairs and two curled-beard pairs (Byzantine beard; the Gugel hides most of a chest-length beard), and the walrus text made extreme."),
    (26, "medium", "Culture palettes invite flags", "Applied", "UI_ART_RULES avoid lists: Italy no green, white and red side by side; Germany no black-red-gold (and still no black-white-red); Japan no red sun or red disc."),
    (27, "medium", "Poster PPU (same as finding 1)", "Applied, then superseded (v2.1)", "Finding 1."),
    (28, "medium", "'Travel poster' primes lettering", "Applied", "'an illustrated wall print in a flat vintage-poster style, with no title area, no banner and no lettering of any kind'; 'no title band' in the checklist."),
    (29, "medium", "Future motifs cut holes through fabric", "Applied", "Printed or embroidered lattice and punch-card patterns, 'laser-cut-look stitched' line work on solid fabric; section 10 and prompt 9.9 ban holes."),
    (30, "medium", "Premades attach drifting skin 2 to 5 figures", "Applied", "Claude makes `premadebase_[m/f]_skin[N].png` after Batch 2; prompt 9.10 attaches it; Claude registers every premade image to the landmarks."),
    (31, "low", "Red hair ornaments defeat the mask", "Applied", "Ornaments are white, silver or blue (black where the look says so), never red; the Meiji bow is white."),
    (32, "low", "The first message bans all green", "Applied", "'never bright green or lime (dark bottle or olive green is fine)'."),
    (33, "low", "Colour words that invite banned colours", "Applied", "'ruby red, sapphire blue or amber'; 'cotton'; 'blue-leaning turquoise'; 'slate teal-blue'; the stress test forces the qamis to deep bottle green."),
    (34, "low", "Coins come back with faces or lettering", "Applied", "Every coin is a plain gold disc with a raised rim and no face, letters or marks; Block A and section 10 say so too."),
    (35, "low", "Meiji 'generic family crests'", "Applied", "'five small plain white circles (blank crest roundels with no design inside)'."),
    (36, "low", "Crescent pendants", "Applied", "'a row of teardrop and disc pendants'."),
    (37, "low", "Two near-identical reds in the Meiji girl's dress", "Applied", "Indigo-and-white yagasuri."),
    (38, "low", "Futou wing width ambiguous", "Applied", "'from wing tip to wing tip about one and a half times the shoulder width, inside the safe area'."),
    (39, "low", "'Office lady company uniform'", "Applied", "'Office outfit'."),
    (40, "low", "Guide: no feet; 'crowns' in the headroom label", "Applied", "Simple front-view feet end on y = 1490; label as in finding 12."),
    (41, "low", "Skin tones only in words; 'never pink' may grey the lips", "Applied", "Five hex swatches (section 7, prompt 9.2, coverage.json); Block A allows natural skin and lip colours on faces and bodies."),
    (42, "low", "Appendix A misreports statuses", "Applied", "`data_v2.py` compares every fix with the reviewer's text; changed rows read 'applied, then amended' with the rule that changed them."),
    (43, "low", "Wording and accuracy details", "Applied", "'(reserved for Muslims under Ottoman sumptuary law)'; 'as Caesar rose to power, c. 50 BCE'; Iraq Future 'roll-print bands of abstract geometric pattern (no figures, no writing)'; Germany Future keeps white away from red-and-black areas."),
]
w("""## Appendix E. The review of v2: applied, adapted and rejected

The v2 brief was reviewed on 2026-09-24 (43 findings). Nothing was rejected. Two findings were applied in an adapted form, and each says why. Findings are numbered in the review's order.

| # | Severity | Finding | Status | What changed |
|---|---|---|---|---|
""" + "\n".join(f"| {n} | {s} | {f} | {st} | {h} |" for n, s, f, st, h in REVIEW2) + """

Also changed while applying them (same reasons): the Florentine women's brooch became a pendant on its own chain (finding 15); the Khedivate and Ottoman Egypt plait curtains are described as showing beside the neck, so their hair-back flag passes the one test (finding 8); the Qing woman's outfit names flat cloth shoes, so the 'bound feet' warning can leave ChatGPT's list (finding 20); the watch chains no longer name a waistcoat (finding 15).

**Open questions for Saleh**

1. **Cast swap.** Strozzi out, Cecilia Gallerani in (finding 6). Keep, or bring Strozzi back and accept that her widow's veil looks unlike Florence's Costume Guide row?
2. **Hair under hats.** """ + str(N_HID) + """ generated confusable pairs keep the hair signatures (Caesar crop, Suebian knot, chasen-mage, nodus roll, sokuhatsu) from leaking invisibly under a claim's hat. Keep them as data, or add one `CanLeak` row and a `hidesTop` headwear flag to the piece-4 spec instead?
3. **Headphones round the neck.** Showa Tokyo's men wore them on the head with a Walkman on the belt; the leak item now rests round the neck for both genders so it stands alone. Acceptable?
4. **Leonardo and Gutenberg without beards.** Chosen to fit their places' rules and the historical record; their famous later images have beards, so they are harder to recognise. Acceptable, given that the name appears on their papers?
5. **Poster size.** Closed in v2.1: the 3D office has no poster. When the art side adds a poster frame to the office, the poster becomes a texture in it, sized by that frame (UI_ART_RULES, "Posters: later").
""")

# ---------------------------------------------------------------- appendix F: v2.1, the 3D office
def cv(pid): return P[pid]["cultureValue"]
w(f"""## Appendix F. v2.1: the desk view and the leak items it can see

**How the desk view was measured.** In the art office on `main` (d5844d0: the art scene of art 23aa6e1 with the gameplay layer), the office camera sits at (0, 2.16, -2.62), pitched 10 degrees down, with a vertical field of view of 55 degrees; the traveller stands at the default `Anchor_Traveller` (0, 0, 1.6), 1.8 m from the soles to the top of the head (`DeskConfigSO.travellerHeight`, `TravellerView.Stand`), a yaw-only billboard tinted (0.9, 0.88, 0.84). Projecting the `LookCanvas` landmarks through that camera gives the placeholder's hat top at y 407, the chin at 505 and the sleeve ends at 613 on a 1920 x 1080 screen; the layered placeholder in `p9_day2_bubble_hieroglyphs.png` (a screenshot of piece 9's play-through) shows them at 406.5, 505 and 609 to 613, so the projection holds. The head (the top of the head, y 260, to the chin, y 424) is {D1080['head']} px tall; the "about 85 px" read off that screenshot earlier was the top of the head to the shoulders ({D1080['head_to_shoulders']} px). The NEXT sign's top edge meets the figure at y 622 to 628 on screen, the waist line (canvas y {DESK_VISIBLE_TO} projects to 622), and the sign is as wide as the figure's hands. The field of view is vertical, so every size scales with the screen height ({D720['head']} px at 720 lines).

**The rule.** A leak item sits on the head, face, neck, shoulders or upper chest (above y {READ_ZONE_TO}), is leakable over any other place's look, differs from every other place's signature (the candidate grid in `tools/confusable_candidates.txt`; `Looks.LabelProblems` in `tools/data_v2.py`), is respectful, and breaks no line of its place's DO NOT DRAW list. An item the look already has (its headwear or hair) was preferred when it passed those checks.

| Place | Gender | v2 leak item (where) | v2.1 leak item | Why | Source |
|---|---|---|---|---|---|
| greece_earlymodern (Ottoman Ioannina) | woman | pafti buckle (Accessory, the waist: behind the NEXT sign) | **silver chest chains** (Accessory) | The look's headwear (cap and tsemberi) is already confusable with three other women's headwear (the sakkos, the mandil, the fesi), and its two front braids look like Iron Age Britain's; Epirote silverwork keeps Ioannina's identity where the desk shows it. The pafti stays drawn, as the buckle that closes the outfit's belt. A confusable pair with Renaissance Nuremberg's layered gold chains (the one similar item) keeps a hard-to-see leak out. | Greek women's chest ornaments of silver chains (for example the Benaki Museum's chest ornament of chains; the kioustekia of Greek costume) and Ioannina's filigree workshops ("Wearing Silver", The Athenian, 1990); back-projected to c. 1700 like the pafti itself, as the research's own note says. Drawn without crosses, saints' plaques or double-headed eagles (the place's DO NOT DRAW list bans crosses; Block A bans eagles). |
| japan_earlymodern (Tokugawa Edo) | woman | Nagoya-obi (Accessory, the hips) | **kazuki veil** (Headwear, hides the top) | The look had no headwear, and its tamamusubi hair reads like Muromachi Kyoto's long tied-back hair from the front (its loop sits at the nape, out of view). Japanese women of 1610 wore no necklaces or earrings, and hairpins belong to later Edo (the DO NOT DRAW list). The kazuki, a patterned kosode draped over the head, is a big, dark, densely patterned shape unlike every other veil (all plain white, black or one colour). The Nagoya-obi stays drawn, in the outfit. | Women going out in the Momoyama and early Edo periods wore a kosode over the head as a veil (kazuki), as the genre screens the research already cites show; the Edo kazuki was worn with its collar pulled forward over the forehead (for example the Kyoto Prefectural Library and Archives, "The Costume of Edo-Period Japanese Women"). |
| china_modern (Beijing 1972) | woman | khaki satchel (Accessory, the hip) | **navy cap** (Headwear, hides the top) | The women's look had no headwear, and its clipped bob reads like Weimar Berlin's Bubikopf; the plain cloth peaked cap of the men's look was worn by men and women alike with the Zhongshan suit. Among the women's headwear it has no look-alike (the Weimar cloche is a low bell hat without a peak). The satchel stays drawn as the women's accessory (not leakable). The Culture value becomes one item for both genders: "{cv('china_modern')}". | The research's own sources (1970s documentary photographs of Beijing; the suit "worn by men and women alike") and "Dress in Communist China" (Fashion, Costume, and Culture: men and women wore the same garments, with cloth peaked caps). Plain, with no star or badge (DO NOT DRAW). |
| greece_modern (Metapolitefsi Athens) | man and woman | tagari bag (Accessory, the front of the hip) | **tagari bag**, drawn high: a short, broad striped strap across the chest and the bag against the left side of the chest, its top level with the armpit | No head or neck item of Athens 1975 passes the checks: the thick moustache and the shaggy cut look like other places' moustaches and cuts, the fisherman's cap is Beijing's and London's cap (the research dropped it for that), and worry beads are held. The tagari is the research's single Greek item for both genders, so it stays and moves up. | The research: the tagari was so emblematic of 1970s Athenian students that it named them. Worn higher than it usually was (open question 2 of v2.1). |
| china_ancient (Eastern Han Luoyang) | woman | bi-disc pendant (Accessory, mid-chest: borderline) | **bi-disc pendant**, raised to the upper chest (its centre about a hand below the collarbones) | It shows above the desk at mid-chest, but papers held up to read (piece 10) reach mid-chest, so it moves up to clear them. The label and the Culture value are unchanged ("{cv('china_ancient')}"), so the game data does not change; Ban Zhao's description moves it up too. | Unchanged (it hangs on its own cord round the neck since the v2 review, finding 3). |

**The Culture values (Costume Guide rows) that change:** Ottoman Ioannina "{cv('greece_earlymodern')}"; Tokugawa Edo "{cv('japan_earlymodern')}"; Beijing, People's Republic "{cv('china_modern')}". Metapolitefsi Athens and Eastern Han Luoyang keep theirs.

**MUST READ lines.** {N_MUST_V21} lines named or led with a feature below the desk (such as calcei boots, knee-tied trousers, hakama, the fustanella, Schnabelschuhe, leg bindings, knee- and ankle-length garments, skirts, the pafti and the Nagoya-obi, or a long garment's silhouette), or did not name the leak item that replaced one; each now names only what shows above the desk. The lower-body description stays in the outfit lines, because every layer is still drawn whole.
""")

brief = "\n".join(out)
(ART / "CHARACTER_ART_BRIEF_v2.md").write_text(brief, encoding="utf-8")

# ================================================================= coverage.json
bases = [body_key(g, s) for g in GENDERS for s in SKINS] + [head_key(g, s, v) for g in GENDERS for s in SKINS for v in FACES]
garments = {pid: {g: garment_keys(pid, g) for g in GENDERS} for pid in P}
future = {f"{c}_future": {g: future_keys(c, g) for g in GENDERS} for c in COUNTRIES}
premades = {x["id"]: [premade_key(x["id"], e) for e in EXPRESSIONS] for x in PREMADES}
flat = list(bases)
for pid in P:
    for g in GENDERS:
        flat += garments[pid][g]
for c in future.values():
    for g in GENDERS:
        flat += c[g]
flat += NEUTRAL_FUTURE_KEYS
flat += PRESENT_OUTFIT_KEYS + KIT_KEYS
for v in premades.values():
    flat += v
assert len(flat) == len(set(flat)), "duplicate key"

raw = []
def add_raw(batch, folder, lst):
    for fn, ks in lst:
        r = {"batch": batch, "raw": f"ArtDeliverables/TimeDesk/Characters/Raw/{folder}/{fn}", "produces": ks}
        if fn.startswith("base_") and "_skin1_" in fn:
            r["note"] = "body skin 1 is cut from this figure; bodies 2-5 are recoloured from that body to skinSwatches"
        elif fn.startswith("base_"):
            r["note"] = "only the head is kept (the body comes from skin 1); the file is also the skin-colour reference for the head"
        raw.append(r)
add_raw(1, "batch01-pilot", b1_raws)
add_raw(2, "batch02-base", b2_raws)
add_raw(3, "batch03-premades", premade_raws(["senenmut", "socrates"]))
add_raw(4, "batch04-day-1-places", [r for pid in DAY1 for r in place_raws(pid, STRESS_FILES)])
add_raw(5, "batch05-day-2-places", [r for pid in DAY2 for r in place_raws(pid, STRESS_FILES)])
add_raw(6, "batch06-premades", premade_raws(POOL2))
add_raw(7, "batch07-day-3-places", [r for pid in DAY3 for r in place_raws(pid, STRESS_FILES)])
add_raw(8, "batch08-premades", premade_raws(POOL3_NEW))
add_raw(9, "batch09-industrial-era", [r for pid in DAY4 for r in place_raws(pid)])
add_raw(10, "batch10-modern-era", [r for pid in DAY5 for r in place_raws(pid)])
add_raw(11, "batch11-future", fut_raws)
add_raw(12, "batch12-2150-kit", kit_raws)
produced = [k for r in raw for k in r["produces"]]
assert sorted(produced) == sorted(flat), (set(flat) - set(produced), set(produced) - set(flat))

cov = {
    "schema": "timesorter.character-art-coverage/1",
    "generated": "2026-09-26",
    "sources": {
        "grammar": "piece-4 spec section 2.3 (LookKeys) + piece-5 R18 (artNation 'neutral' for Future hair and facial hair)",
        "wardrobe": "costume_research.json + brief_review.json (68 fixes, 22 issues) -> tools/wardrobe_v2.json",
        "premades": "piece4_decisions.md Amendment A1 (cast after the review of brief v2: Cecilia Gallerani replaces Alessandra Strozzi)",
        "review": "brief v2 review (43 findings), applied in data_v2.py and build_v2.py; see the brief's Appendix E",
        "revision": "v2.1 (2026-09-25): revised for the 3D office (leak items the desk can see, the desk view, 4:3 wallpapers, posters deferred); see the brief's 'What changed in v2.1' and Appendix F; v2.2 (2026-09-25): the ReStory style for both kinds of character (style text only: no key, file, canvas or size change); see the brief's 'What changed in v2.2'; v2.3 (2026-09-26): the present's clothes (the neutral Future outfits become keys) and the 2150 accessory kit (Batch 12); see the brief's 'What changed in v2.3'",
    },
    "assetFolder": "Assets/Art/Characters/Resources/Characters",
    "extension": ".png",
    "canvas": {"width": 1024, "height": 1536, "format": "RGBA PNG, untrimmed", "pivot": {"x": 512, "yFromTop": 1490},
               "landmarksFromTop": {"headTop": 260, "chin": 424, "shoulders": 500, "waist": 760, "hips": 900, "knees": 1170, "feet": 1490},
               "safeX": [120, 904], "photoCrop": [362, 215, 662, 590],
               "officeView": {"visibleFromTopTo": DESK_VISIBLE_TO, "readZoneTo": READ_ZONE_TO,
                              "headPx": {"1080": DESK[1080]["head"], "720": DESK[720]["head"]},
                              "screenPxPerCanvasPx": {"1080": DESK[1080]["px"], "720": DESK[720]["px"]},
                              "note": "the figure behind the desk at the default Anchor_Traveller: the NEXT sign cuts it at the waist; every leak item sits above readZoneTo (the brief's section 1)"}},
    "skinSwatches": {str(n): {"name": SKIN_WORD[n], "hex": SKIN_HEX[n]} for n in SKINS},
    "outlineHex": OUTLINE_HEX,
    "grammar": {
        "body": "body_{g}_skin{N}",
        "head": "head_{g}_skin{N}_face{v}",
        "garment": "{layer}_{g}_{nation}_{era}[_{variant}][_{colour}]  (variant on the 2150 accessory kit's items; colour on hair and hairback unless the hair is a wig; always on facialhair)",
        "premade": "premade_{id}_{expression}",
        "tokens": {"g": list(GENDERS), "N": SKINS, "v": FACES, "colour": COLOURS,
                   "layer": ["hairback", "outfit", "facialhair", "hair", "headwear", "accessory"],
                   "nation": COUNTRIES + ["neutral"], "era": ["ancient", "medieval", "earlymodern", "industrial", "modern", "future"],
                   "expression": EXPRESSIONS, "variant": [k["variant"] for k in KIT]},
        "reserved": "an outfit variant suffix _v{N} (not used yet)",
    },
    "counts": {"bases": len(bases), "garments": sum(len(v[g]) for v in garments.values() for g in GENDERS),
               "future": sum(len(v[g]) for v in future.values() for g in GENDERS) + len(NEUTRAL_FUTURE_KEYS),
               "present": len(PRESENT_OUTFIT_KEYS) + len(KIT_KEYS),
               "premades": sum(len(v) for v in premades.values()), "total": len(flat),
               "rawImages": len(raw)},
    "required": {"bases": bases, "garments": garments, "future": future, "futureShared": NEUTRAL_FUTURE_KEYS,
                 "present": {"clothes": PRESENT_OUTFIT_KEYS, "kit": KIT_KEYS}, "premades": premades},
    "presentKit": {"labels": {k["variant"]: k["label"] for k in KIT}, "slot": "Accessory", "leakable": True, "artNation": "neutral",
                   "note": "one item slips onto an otherwise right costume (a costume error); world_source.json present.kit lists them per gender"},
    "requiredFlat": sorted(flat),
    "wardrobeFlags": {**{p["id"]: {g: {"present": p["wardrobe"][g]["present"], "labels": p["wardrobe"][g]["labels"],
                                        "signature": p["wardrobe"][g]["signature"],
                                        "wig": p["wardrobe"][g]["wig"], "back": p["wardrobe"][g]["back"],
                                        "covers": p["wardrobe"][g]["covers"], "headwearHides": p["wardrobe"][g]["headwearHides"],
                                        "ornamentsToMask": p["wardrobe"][g]["ornamentsToMask"]} for g in GENDERS}
                          for p in W["places"]},
                       **{fp["id"]: {g: {"present": {"outfit": True, "hair": True, "facialHair": g == "m", "headwear": False, "accessory": False},
                                         "labels": {k: v["label"] for k, v in fp["worldSourceWardrobe"][g].items() if k != "signature"},
                                         "signature": {"slot": "Outfit", "label": fp["cultureValue"], "leakable": False},
                                         "artNation": {"hair": "neutral", **({"facialHair": "neutral"} if g == "m" else {})},
                                         "wig": False, "back": False, "covers": [], "headwearHides": "",
                                         "ornamentsToMask": ""} for g in GENDERS}
                          for fp in W["future"]}},
    "rawDeliverables": raw,
    "notes": [
        "The game's own authority is LookKeys.Required/Bases/PremadeSet over the generated content; this file is the brief's proposal and must match once world_source.json carries the wardrobes (wig/back flags and item presence).",
        "hairback keys exist only for hair items flagged back=true (proposed here; one test: hair that shows beside the neck behind the shoulders in front view); change the flag and the list changes.",
        "wardrobeFlags: 'labels' holds every present item's short label; the signature item is leakable; 'covers' is the headwear's covers list (['Hair'] when it hides all the hair); Future places share hair and facial hair through artNation 'neutral' and never leak (signature Outfit).",
        "Confusable pairs (hand-authored and generated) are in tools/wardrobe_v2.json 'confusable' and 'confusableHidden'.",
        "outfit_{g}_neutral_future is the present's clothes (since v2.3): a 2150 citizen who forgot their costume wears them whole, with the neutral Future hair and beard.",
        "UI art (the culture wallpapers; posters are deferred) is listed in officeArt, not in requiredFlat: it follows the office path convention (piece-6 R11). officeArt also names the 2D layers over the office and the art side's scanner, which replace placeholders in place.",
    ],
    "officeArt": {
        "convention": "fixed path, replaced in place (piece-6 R11); raw ChatGPT masters go to ArtDeliverables/TimeDesk/Culture/Raw/",
        "files": [f"Assets/Art/Culture/{c}/wallpaper.png" for c in COUNTRIES],
        "wallpaper": {"raw": "1536 x 1024 from ChatGPT; crop the middle 1365 x 1024 (4:3; about 85 px off each side)", "final": [1440, 1080],
                      "aspect": "4:3, the desktop canvas (OfficeSceneUIBuilder DesktopSize 1440 x 1080); the wallpaper Image envelopes it (AspectRatioFitter EnvelopeParent, overflow clipped), so a 16:9 image loses its sides",
                      "quietShares": {"left": 0.2, "top": 0.13, "bottom": 0.04},
                      "quietWhy": "the desktop icons in two columns at the left (0.5% to 17% of the width), the claim banner over the top 13% while a traveller is at the desk, the taskbar along the bottom (36 of 1080 units); the desktop is also cloned small on the office CRT (1024 x 768, letterboxed)",
                      "import": "keep the placeholder's .meta (Sprite, max size 2048, mipmaps on); the fitter takes the sprite's own aspect (CultureThemeService), so any aspect and pixels per unit work",
                      "placeholder": [960, 540],
                      "placeholderNote": "the 16:9 text-free placeholders Generate World wrote; it never writes over an existing file, so they are not regenerated; until the art lands they lose an eighth of their width on each side"},
        "posters": {"status": "deferred",
                    "note": "the 3D office has no poster frame: piece 6 deferred the culture posters to the office move (its S2 and F13), and the move added none; no poster.png exists. When the art side adds a poster frame (an anchor), the poster becomes a texture in it, sized by that frame"},
        "overlay2d": [{"path": "Assets/Art/Office/Placeholder/pc_frame.png", "referenceSize": [1240, 1060], "suggested": [1860, 1590],
                       "glassFromTopLeft": [60, 60, 1180, 900], "placeholder": [620, 530],
                       "note": "the PC close-up bezel on the office overlay (PcFrame); a transparent 4:3 glass hole; render it from the Blender CRT by preference (UI_ART_RULES)"},
                      {"path": "Assets/Art/Office/Placeholder/pc_close.png", "referenceSize": [64, 64], "placeholder": [48, 48], "note": "the frame's close X"},
                      {"path": "Assets/Art/Office/Placeholder/crt_power.png", "referenceSize": [64, 64], "placeholder": [28, 28], "note": "the frame's power button (the LED stays a code-tinted white disc)"}],
        "props3d": [{"anchor": "Anchor_Scanner", "owner": "art side (Blender)",
                     "note": "the scanner model; until it exists the gameplay layer shows a stand-in flatbed (0.40 x 0.32 m) at the default pose (UI_ART_RULES)"}],
        "optional": [{"path": "Assets/Art/Generated/xp_bliss.png", "size": [1920, 1080], "note": "neutral wallpaper, exists; 16:9, so the 4:3 desktop shows its middle 1440 x 1080"},
                     {"path": "Assets/Art/UI/Investigation/refbook_cover_culture.png", "size": [400, 560],
                      "note": "Costume Guide cover, text-free, like refbook_cover_currency.png; new file (+ .meta like the other covers)"}],
    },
}
(ART / "coverage.json").write_text(json.dumps(cov, ensure_ascii=False, indent=1), encoding="utf-8")
print("brief chars", len(brief), "keys", len(flat), "raw", len(raw))
for c in counts: print(c)
print(cov["counts"])
