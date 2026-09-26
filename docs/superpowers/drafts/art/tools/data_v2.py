"""Builds the corrected wardrobe data for brief v2 (read-only inputs, output under art/tools).

1. Loads costume_research.json and brief_review.json from this checkout's ArtDeliverables/TimeDesk/Characters
   (read only).
2. Applies the 68 data fixes from brief_review.json.
3. Applies the text edits of the 22 practicality issues (and the conflict rules of
   the piece-4 spec R23: a later practicality issue wins over an earlier data fix).
4. Applies the review-2 edits (the review of brief v2): self-contained signature items, accessories that depended on
   an outfit moved into it, the one hair-back test, colour and wording fixes.
5. Applies the v2.1 edits (2026-09-25, the 3D office): the desk and its NEXT sign hide the traveller below the waist,
   so every leak item sits on the head, face, neck, shoulders or upper chest, and every MUST READ line names only
   what shows above the desk. v2.2 (2026-09-25, the ReStory style) changes one DO NOT DRAW entry (Showa Tokyo).
6. Adds the piece-4 wardrobe proposals: a short label for every item, the signature slot per gender, leakable, wig,
   back and covers flags, ornaments to mask before recolouring, confusable pairs (hand-authored and generated), the
   DO NOT DRAW split, and the Future wardrobes.
7. Adds the 2150 accessory kit (v2.3, 2026-09-26; traveller types C2-C4): the present's accessories, one of which a 2150
   citizen can slip onto an otherwise right costume (a costume error), and the present's clothes (the neutral Future).
Checks the full Looks.LabelProblems rule, the accessory rule and the desk-view rule (v2.1). Writes
art/tools/wardrobe_v2.json and art/tools/confusable_candidates.txt and prints an audit.
"""
import json, re, sys
from pathlib import Path

sys.stdout.reconfigure(encoding="utf-8")
REPO = Path(__file__).resolve().parents[5]
SRC = REPO / "ArtDeliverables" / "TimeDesk" / "Characters"
OUT = Path(__file__).parent

data = json.loads((SRC / "costume_research.json").read_text(encoding="utf-8"))
review = json.loads((SRC / "brief_review.json").read_text(encoding="utf-8"))
E = {f"{e['country']}_{e['era']}": e for e in data["entries"]}
G = {"m": "male", "f": "female"}

# ---------------------------------------------------------------- 1. the 68 fixes
fix_log = []
for i, f in enumerate(review["fixes"], 1):
    e = E[f"{f['country']}_{f['era']}"]
    fld = f["field"]
    if fld == "avoid":
        e["avoid"] = [s.strip() for s in f["newValue"].split(" ; ") if s.strip()]
    elif fld == "signature":
        e["signature"] = f["newValue"]
    else:
        g, k = fld.split(".")
        assert k in e[g], fld
        e[g][k] = f["newValue"]
    fix_log.append({"n": i, "place": f"{f['country']}_{f['era']}", "field": fld,
                    "status": "applied", "note": ""})

def fixnote(n, status, note):
    fix_log[n - 1]["status"] = status
    fix_log[n - 1]["note"] = note

# ---------------------------------------------------------------- 2. issue edits
edit_log = []

def setv(place, field, value, why):
    e = E[place]
    if field in ("signature",):
        e[field] = value
    else:
        g, k = field.split(".")
        e[G.get(g, g)][k] = value
    edit_log.append((place, field, "set", why))

def rep(place, field, old, new, why):
    e = E[place]
    if field == "signature":
        cur = e["signature"]
    else:
        g, k = field.split(".")
        cur = e[G.get(g, g)][k]
    assert old in cur, (place, field, old, cur)
    cur = cur.replace(old, new)
    if field == "signature":
        e["signature"] = cur
    else:
        e[G.get(g, g)][k] = cur
    edit_log.append((place, field, f"'{old}' -> '{new}'", why))

# Issue 4: colours that the green/magenta key would eat.
rep("italy_ancient", "m.outfit", "reddish-purple", "deep wine-red", "issue 4")
rep("italy_ancient", "f.outfit", "Ochre, rose, sea-green or blue wool, not purple.",
    "Ochre, dusty coral, slate blue or blue wool.", "issue 4 (+ rose -> dusty coral by the same rule)")
rep("italy_ancient", "signature", "narrow purple clavus", "narrow deep wine-red clavus", "issue 4")
rep("germany_ancient", "f.outfit", "red-purple", "deep wine-red", "issue 4")
rep("germany_ancient", "signature", "red-purple", "deep wine-red", "issue 4")
rep("japan_industrial", "f.outfit", "purple-and-white", "deep wine-red-and-white", "issue 4")
rep("italy_earlymodern", "m.outfit", "rose-pink", "dusty-coral", "issue 4")
setv("japan_ancient", "m.accessory",
     "Necklace of dark, dull grey-green jade magatama (comma-shaped beads) strung with cylindrical kudatama beads", "issue 4")
setv("japan_ancient", "f.accessory",
     "Multi-strand necklace of dark grey-green magatama and round blue glass beads", "issue 4")
rep("japan_ancient", "signature", "necklace of green comma-shaped", "necklace of dark grey-green comma-shaped",
    "issue 4 (on fix 9's wording)")
setv("china_industrial", "f.accessory", "Thick pale white-jade (mutton-fat jade) bangle", "issue 4")
rep("egypt_medieval", "f.outfit", "saffron, crimson or emerald", "saffron, crimson or deep bottle green", "issue 4")
rep("iraq_medieval", "f.outfit", "(rose, saffron or pistachio)", "(dusty coral, saffron or olive)", "issue 4")
rep("britain_ancient", "m.outfit", "red, mustard yellow and green", "red, mustard yellow and dark olive green", "issue 4")
rep("britain_ancient", "f.outfit", "red, yellow and green", "red, yellow and dark olive green", "issue 4")
rep("italy_modern", "f.outfit", "(magenta, turquoise, lime)", "(burnt orange, ochre, teal-blue and black)", "issue 4")
# Issue 4 rule extended to the remaining occurrences (same reason, not listed by line in the issue).
rep("china_medieval", "m.outfit", "muted green or blue-grey", "muted dark olive green or blue-grey", "issue 4 rule, extended")
rep("china_medieval", "f.outfit", "celadon green, ivory, pale pink or light blue",
    "dull grey-green celadon, ivory, pale coral or light blue", "issue 4 rule, extended")
rep("iraq_industrial", "f.outfit", "(gold on crimson or green)", "(gold on crimson or bottle green)", "issue 4 rule, extended")
rep("germany_medieval", "m.outfit", "deep blue or green wool", "deep blue or bottle-green wool", "issue 4 rule, extended")
rep("germany_medieval", "f.outfit", "deep blue or green", "deep blue or bottle green", "issue 4 rule, extended")
rep("egypt_medieval", "m.outfit", "deep green, brown", "bottle green, brown", "issue 4 rule, extended")
rep("greece_medieval", "m.outfit", "deep green, wine or blue", "bottle green, wine or blue", "issue 4 rule, extended")
rep("italy_medieval", "f.outfit", "crimson or green silk velvet", "crimson or bottle-green silk velvet", "issue 4 rule, extended")
rep("china_medieval", "m.outfit", "not the purple or vermilion of high ranks", "not the bright colours of high ranks", "issue 4 rule, extended (never name purple)")
rep("britain_medieval", "f.outfit", "soft red, blue or green wool", "soft red, blue or dark olive-green wool", "issue 4 rule, extended")
rep("egypt_earlymodern", "m.outfit", "saffron or rose silk", "saffron or dusty-coral silk", "issue 4 rule, extended")
rep("egypt_earlymodern", "m.outfit", "forest green", "bottle green", "issue 4 rule, extended")
rep("egypt_earlymodern", "f.outfit", "gold on deep rose", "gold on deep coral", "issue 4 rule, extended")
rep("egypt_earlymodern", "f.outfit", "a sheer gauze chemise", "a thin, opaque white chemise", "issue 5 rule, extended")
rep("iraq_earlymodern", "f.outfit", "(indigo and rose)", "(indigo and dusty coral)", "issue 4 rule, extended")
rep("egypt_industrial", "f.outfit", "cream with rose and gold stripes", "cream with dusty-coral and gold stripes", "issue 4 rule, extended")
rep("greece_industrial", "f.outfit", "pale ivory or rose silk", "pale ivory or dusty-coral silk", "issue 4 rule, extended")
rep("china_industrial", "f.outfit", "lotus-pink or pale blue", "pale coral or pale blue", "issue 4 rule, extended")
rep("germany_industrial", "f.outfit", "sage-green, cream or dove-grey", "moss-green, cream or dove-grey", "issue 4 rule, extended")
rep("egypt_modern", "f.outfit", "saturated red, green or blue ground", "saturated red, bottle-green or blue ground", "issue 4 rule, extended")
rep("egypt_ancient", "f.outfit", " No overgarment: the sheer pleated over-robes came one or two generations later.", " No overgarment.",
    "issue 5 rule, extended (never name sheer fabric; the reason stays in DO NOT DRAW)")

# Issue 5: everything opaque, nothing sheer.
rep("italy_earlymodern", "f.hair", " and a small sheer net (trinzale) over the back of the head", "",
    "issue 5 (wins over fix 31: a sheer net keys badly and is a cap in the hair layer)")
fixnote(31, "applied, then amended", "issue 5 deletes the sheer trinzale net that fix 31 kept")
setv("egypt_earlymodern", "f.headwear",
     "Tartur: a tall headdress that is wider at the top than at the base, wrapped in black or brown cloth woven with gold stripes. The face is fully uncovered.",
     "issues 5, 8, 12 (the gauze veil hanging down the back was sheer and invisible from the front)")

# Issue 6: nothing that covers the head in the outfit; the head part goes to headwear.
setv("egypt_medieval", "f.headwear",
     "The top of the white izar drawn over the head as a soft hood that frames the fully visible face and falls down both sides of the neck onto the shoulders, where it meets the izar. No face veil.",
     "issue 6 (same content as fix 14, wording of the later issue)")
fixnote(14, "applied, then reworded", "issue 6's wording (down both sides of the neck) replaces it; same intent")
setv("iraq_industrial", "f.outfit",
     "Ankle-length, long-sleeved dress of brocaded silk (gold on crimson or bottle green) with a silver-plaque belt. Over everything is a black silk 'abaya, hanging from the shoulders to the ankles and falling open at the front to show the dress.",
     "issue 6 (spec R23: abaya body in the outfit)")
setv("iraq_industrial", "f.headwear",
     "The top of the black silk 'abaya drawn over a black head-kerchief (futa), from the crown down both sides of the head and neck to the shoulders. The face is uncovered.",
     "issue 6 (spec R23: head part in headwear)")
fixnote(44, "superseded", "spec R23 / issue 6: the abaya body stays in the outfit, from the shoulders")
fixnote(46, "superseded", "spec R23 / issue 6: headwear holds only the head part of the abaya")
rep("britain_industrial", "m.outfit", "tall collar points", "collar points kept below the chin", "issue 6 (collars below the chin)")

# Issue 8: nothing that hangs behind the body on the hair/headwear/accessory layers.
rep("egypt_medieval", "m.headwear", "hangs behind one shoulder", "hangs down in front of one shoulder", "issue 8")
setv("iraq_medieval", "m.accessory",
     "Taylasan: a dark shawl draped over both shoulders, its ends falling down the front of the chest, the mark of a scholar. It stays on top of the shoulders, clear of the tiraz armbands on the upper sleeves.",
     "issue 8 merged with fix 17")
fixnote(17, "merged", "issue 8 moves the ends to the front; fix 17's 'clear of the tiraz armbands' is kept")
setv("egypt_industrial", "f.headwear",
     "Tarha: a long white muslin head veil with its ends embroidered in coloured silk and gold thread. It rests on the head and falls over both shoulders and down the sides of the body to below the knee, and the face is fully uncovered.",
     "issue 8 (spec R23: tarha over the shoulders and down the sides)")
setv("egypt_industrial", "signature",
     "Men: the stiff, upright crimson tarboush with a black tassel, over a black stambouline buttoned to the collar. Women: the long white embroidered tarha veil falling from the head over both shoulders, over a striped yelek robe.",
     "issue 8 (spec R23)")
fixnote(41, "superseded", "spec R23 / issue 8: the tarha falls over both shoulders and down the sides (the kerchief cap stays dropped)")
fixnote(42, "superseded", "spec R23 / issue 8's signature wording")

rep("germany_medieval", "m.headwear", " and ends in a long tail (Zipfel) that hangs down the back.", ".",
    "issue 8 rule, extended (fix 27 already dropped the tail from the MUST READ: it hangs behind the body)")
# Issue 10: Future motifs (applied in the Future section of build_v2.py).
# Issue 11: accessories that depend on another layer or are too small.
rep("china_ancient", "m.headwear", "No chin ties.", "No chin ties. A slim bamboo writing brush (zanbi) is tucked upright into its side.", "issue 11")
setv("china_ancient", "m.accessory", "A hand-sized embroidered silk pouch hanging from the sash on a red cord",
     "issue 11 ('small' raised to 'hand-sized' so it meets the issue's own new accessory rule)")
setv("egypt_industrial", "m.accessory", "Gold watch-chain hanging in a clear loop across the front of the chest", "issue 11")
setv("italy_earlymodern", "f.headwear",
     "Lenza: a dark cord worn straight across the forehead, with a clearly visible jewel (a pearl or dark-red stone about the size of an eye) at the centre of the brow",
     "issue 11")
setv("italy_earlymodern", "signature",
     "Lenza: a dark cord with a large brow jewel worn straight across the forehead over smooth, hatless hair (men: a small soft red berretta over a shoulder-length bob, with a knee-length dusty-coral pitocco)",
     "issues 11 and 4 on fix 32's wording")
fixnote(32, "applied, then amended", "issue 11 (large brow jewel) and issue 4 (pink -> dusty coral)")
rep("japan_industrial", "f.hair", "(Western-inspired, promoted from 1885)",
    "(Western-inspired, promoted from 1885), with a large deep-red silk ribbon bow tied high at the back, its loops standing out on both sides of the head in front view",
    "issue 11")
setv("japan_industrial", "f.accessory", "none", "issue 11 (the bow moved into the hair layer)")
fixnote(54, "superseded", "issue 11 moves the ribbon bow into the hair layer (a bow tied to a hairstyle cannot leak onto another hairstyle); accessory is none")
setv("italy_ancient", "m.accessory", "none",
     "issue 11: the signet ring is too small; the research names no larger worn item for a togate man, so none (fix 5 precedent)")
setv("italy_ancient", "f.accessory", "Monile: a short necklace of large pearls on gold links, worn high on the chest",
     "issue 11: pearl earrings too small; the research notes pearls as the Roman status craze")
setv("china_medieval", "f.accessory", "none", "issue 11: pearl earrings too small; no larger researched item")
setv("china_earlymodern", "f.accessory", "none",
     "issue 11: earrings too small; the research's jinbu pendant was rejected there as confusable with Japan's tasselled Nagoya-obi")
setv("britain_earlymodern", "m.accessory", "none",
     "issue 11: single earring too small; a merchant's gold chain would collide with Germany early-modern men")
setv("china_modern", "m.accessory",
     "Plain faded-khaki canvas satchel on a cross-body strap, with no star, slogan, print or stripes",
     "issue 11: pen in a pocket depends on the outfit and is too small; the research's satchel ('the ubiquitous plain canvas bag of the time')")
# Issue 13: tiraz and script as abstract pattern.
rep("iraq_medieval", "m.outfit", "Bands of gold-embroidered decorative Arabic-style pseudo-script (tiraz) circle both upper sleeves.",
    "Broad bands of gold embroidery (tiraz) circle both upper sleeves, patterned with abstract strokes and knots, not letters.", "issue 13")
rep("iraq_medieval", "f.outfit", "with tiraz script bands at the upper arms", "with gold-embroidered tiraz bands (abstract pattern, no letters) at the upper arms", "issue 13")
rep("iraq_medieval", "f.headwear", "carrying a line of decorative pseudo-script", "embroidered with a line of small abstract gold patterns (no letters)", "issue 13")
# Issue 21: no jewellery or badges in outfits.
rep("britain_ancient", "f.outfit", "fastened at the chest.", "fastened at the chest with a plain bronze pin.", "issue 21 (on fix 10)")
rep("japan_modern", "m.outfit", ", plus a small round enamel company badge (shasho) with a generic abstract mark on the left lapel", "", "issue 21")
# Issue 5 (continued): sheer stockings.
rep("japan_modern", "f.outfit", "sheer stockings", "bare legs (no stockings drawn: leave the legs magenta)", "issue 5")

# Issue 7 consistency: hair lines must not name the headwear (same reason as fixes 38, 43, 45, 55, 56, 59).
HAIR7 = {
    ("egypt_medieval", "m"): "Short-cropped",
    ("egypt_medieval", "f"): "Long hair braided and pinned up close to the head, with a thin smooth edge of hair at the brow",
    ("iraq_medieval", "m"): "Hair to the nape, combed smooth and close to the head",
    ("iraq_medieval", "f"): "Long hair pinned up close to the head, with two glossy S-shaped side-curls (sudgh) combed forward onto the cheeks at the temples",
    ("greece_medieval", "f"): "Hair pinned up close to the head, with a smooth edge at the forehead",
    ("china_medieval", "m"): "Topknot on the crown; neat hairline",
    ("japan_medieval", "m"): "Motodori topknot at the crown over a full head of hair (no shaved pate)",
    ("britain_medieval", "f"): "Long hair coiled low and close at the nape",
    ("germany_medieval", "m"): "Chin-length straight hair with a full fringe",
    ("germany_medieval", "f"): "Hair plaited and pinned up close to the head",
    ("egypt_earlymodern", "m"): "Short-cropped",
    ("iraq_earlymodern", "m"): "Short-cropped",
    ("iraq_earlymodern", "f"): "Long hair in two braids pinned up close to the head",
    ("greece_earlymodern", "m"): "Cropped short",
    ("china_earlymodern", "m"): "Topknot on the crown, with a black horsehair net headband (wangjin) across the forehead",
}
for (p, g), v in HAIR7.items():
    setv(p, f"{g}.hair", v, "issue 7 consistency (hair described with the headwear off, never naming it)")
# Iraq early-modern men: one turban colour, so the leak item is one thing.
rep("iraq_earlymodern", "m.headwear", "(for example cream with fine red-brown checks, or dark indigo with small motifs)",
    "(cream with fine red-brown checks)", "one colourway, so the leak label names one item")

# ---------------------------------------------------------------- 2b. review 2 (brief v2 review, 2026-09-24)
R2 = "review 2"
def moment(place, old, new, why):
    assert old in E[place]["moment"], (place, old)
    E[place]["moment"] = E[place]["moment"].replace(old, new)
    edit_log.append((place, "moment", f"'{old}' -> '{new}'", why))

def avoid_rep(place, old, new, why):
    lst = E[place]["avoid"]
    hits = [i for i, a in enumerate(lst) if old in a]
    assert len(hits) == 1, (place, old, hits)
    lst[hits[0]] = lst[hits[0]].replace(old, new)
    edit_log.append((place, "avoid", f"'{old}' -> '{new}'", why))

# Signature (leak) items must stand on their own and read at thumbnail size (findings 3, 15, 16).
setv("china_ancient", "f.accessory",
     "Jade bi-disc pendant: a flat round disc of dark, dull grey-green jade with a hole in the centre, as wide as the palm, "
     "hanging at mid-chest on its own long red silk cord round the neck, with a short red tassel below it",
     f"{R2} finding 3/15: on its own cord, not hanging from the outfit's sash")
setv("britain_medieval", "m.accessory",
     "Large round silver disc brooch with interlace and niello decoration, about as wide as the face, worn high on the "
     "right shoulder. Draw the brooch alone, with no cloak behind it",
     f"{R2} findings 3/16: large and self-contained (a small pin is banned by the accessory rule)")
rep("britain_medieval", "m.outfit", "is draped over the left shoulder and pinned at the right.",
    "is draped over the left shoulder and fastened at the right shoulder with a small plain pin.",
    f"{R2} finding 3: the cloak fastens itself; the brooch is only the accessory")
setv("britain_medieval", "signature",
     "Men: the only knee-length tunic of the era, with leg bindings, a big round silver disc brooch high on the right "
     "shoulder, and the long English moustache. Women: a coloured headrail veil wrapped over the head and round the "
     "throat, above a bell-sleeved overgown.", f"{R2} finding 3 (the brooch no longer pins the cloak)")
setv("japan_modern", "m.accessory",
     "Lightweight silver-and-black headband headphones with round bright-orange foam ear pads, resting around the neck "
     "with the ear pads on the collarbones either side of the throat; no cord, no player, no brand logo",
     f"{R2} findings 3/15: no cord to a player on a belt the disguise may not have")
setv("japan_modern", "f.accessory",
     "The same silver-and-black headband headphones with round bright-orange foam ear pads, resting around the neck; "
     "no cord, no player, no brand logo", f"{R2} finding 3 (same item as the man's)")
setv("japan_modern", "signature",
     "Orange-foam headphones resting around the neck (men with a dark salaryman suit, women with a fitted office "
     "waistcoat and pleated skirt)", f"{R2} finding 3")
setv("germany_industrial", "f.accessory",
     "Jugendstil pendant: a large silver plaque with dark-blue enamel and a curving 'whiplash' leaf motif in the "
     "Pforzheim style, about as wide as the face, hanging at the base of the throat on its own short silver chain",
     f"{R2} findings 3/16: a brooch is a small pin on the collar; a pendant on its own chain stands alone")
setv("italy_earlymodern", "f.headwear",
     "Lenza: a finger-wide dark velvet band worn straight across the forehead, with a large dark-red stone set in gold, "
     "as big as the whole eye, at the centre of the brow",
     f"{R2} finding 3: a thin cord and an eye-sized jewel are about 3 px in the Visitor window")
setv("italy_earlymodern", "signature",
     "Lenza: a finger-wide dark brow band with a large dark-red jewel at the centre, worn straight across the forehead "
     "over smooth, hatless hair (men: a small soft red berretta over a shoulder-length bob, with a knee-length "
     "dusty-coral pitocco)", f"{R2} finding 3")
setv("iraq_modern", "f.accessory",
     "Gold coin pendant: a single large plain gold disc with a raised rim (a gold lira, drawn with no face, letters or "
     "marks), about as wide as three fingers, on its own short gold chain at the base of the throat",
     f"{R2} findings 15/34: its own chain (not 'where the abaya falls open'); coins drawn as plain discs")

# Non-signature accessories that hang from, tuck into, pin or close an outfit item move into that outfit (finding 15).
rep("china_ancient", "m.outfit", "tied at the waist with a cloth sash on a small bronze belt hook.",
    "tied at the waist with a cloth sash on a bronze belt hook, with a hand-sized embroidered silk pouch hanging from "
    "the sash on a red cord.", f"{R2} finding 15: the pouch hangs from the outfit's sash")
setv("china_ancient", "m.accessory", "none", f"{R2} finding 15 (pouch moved into the outfit)")
rep("iraq_ancient", "f.outfit", "so the right shoulder is bare.",
    "so the right shoulder is bare, and it is fastened at the chest with a pair of long bronze toggle pins.",
    f"{R2} finding 15: the toggle pins fasten the shawl, so they are part of it (plain pins are allowed in an outfit)")
setv("iraq_ancient", "f.accessory", "none", f"{R2} finding 15 (the bead string between the pins is jewellery; dropped)")
rep("italy_medieval", "m.outfit", "belted with a narrow leather belt.",
    "belted with a narrow leather belt, with a leather merchant's purse (scarsella) hanging from it at the hip.",
    f"{R2} finding 15")
setv("italy_medieval", "m.accessory", "none", f"{R2} finding 15 (purse moved into the outfit)")
setv("italy_medieval", "f.accessory",
     "A large gold pendant jewel (fermaglio) with a pearl cluster, as wide as the palm, hanging at the base of the "
     "throat on its own short gold chain", f"{R2} finding 15: a brooch pinned to the neckline depends on the gown")
rep("britain_medieval", "f.outfit", "A plain mantle hangs from the shoulders.",
    "A plain mantle hangs from the shoulders, fastened at the upper chest with a plain round gilt clasp.",
    f"{R2} findings 15/16: the clasp closes the mantle, so it is part of it; 'small' dropped")
setv("britain_medieval", "f.accessory", "none", f"{R2} finding 15 (clasp moved into the outfit)")
rep("germany_ancient", "m.outfit", "is pinned at the right shoulder.",
    "is pinned at the right shoulder with a bronze bow brooch (fibula).", f"{R2} finding 15")
setv("germany_ancient", "m.accessory", "none", f"{R2} finding 15 (the fibula pins the cloak: moved into the outfit)")
rep("japan_medieval", "m.outfit", "with no family crests.",
    "with no family crests. A folding fan (sensu) with black lacquered ribs is tucked, closed, into the waist ties at the front.",
    f"{R2} finding 15")
setv("japan_medieval", "m.accessory", "none", f"{R2} finding 15 (fan moved into the outfit)")
rep("japan_medieval", "f.outfit", "tied with a narrow cloth sash.",
    "tied with a narrow cloth sash, with a small tie-dyed drawstring purse (kinchaku) hanging from it.", f"{R2} finding 15")
setv("japan_medieval", "f.accessory", "none", f"{R2} finding 15 (purse moved into the outfit)")
rep("germany_medieval", "m.outfit", "belted low on the hips with a leather belt, worn over tight hose.",
    "belted low on the hips with a leather belt with a leather purse hanging from it, worn over tight hose.", f"{R2} finding 15")
setv("germany_medieval", "m.accessory", "none", f"{R2} finding 15 (purse moved into the outfit)")
rep("germany_medieval", "f.outfit", "closed at the chest by a plain cord (not held in the hands).",
    "closed at the chest by a plain cord (not held in the hands). The neckline is closed with a round gold clasp.",
    f"{R2} finding 15: the Fuerspan clasp closes the neckline")
setv("germany_medieval", "f.accessory", "none", f"{R2} finding 15 (clasp moved into the outfit)")
rep("iraq_earlymodern", "m.outfit", "wrapped across the chest and belted.",
    "wrapped across the chest and belted, with a long brass pen case (qalamdan) tucked upright into the belt.", f"{R2} finding 15")
setv("iraq_earlymodern", "m.accessory", "none", f"{R2} finding 15 (pen case moved into the outfit)")
rep("italy_earlymodern", "m.outfit", "Close-fitting dark hose and soft round-toed shoes.",
    "A small leather-bound notebook hangs from the belt on a cord. Close-fitting dark hose and soft round-toed shoes.",
    f"{R2} finding 15")
setv("italy_earlymodern", "m.accessory", "none", f"{R2} finding 15 (notebook moved into the outfit)")
rep("japan_earlymodern", "m.outfit", "No crests.",
    "No crests. An inro (a small stacked lacquered case) hangs at the hip on a silk cord from the hakama waist ties, held by a carved toggle.",
    f"{R2} finding 15")
setv("japan_earlymodern", "m.accessory", "none", f"{R2} finding 15 (inro moved into the outfit)")
setv("britain_earlymodern", "f.accessory",
     "A slim silver-link girdle worn at the waist, with a silver pomander ball as big as a fist hanging from it on a chain to the knee",
     f"{R2} finding 15: the pomander brings its own girdle (a belt and what hangs from it is an allowed accessory)")
rep("iraq_industrial", "m.outfit", "girdled with a folded patterned shawl.",
    "girdled with a folded patterned shawl, with a slim brass scribe's pen case (dawat) tucked upright into the front of it.",
    f"{R2} finding 15")
setv("iraq_industrial", "m.accessory", "none", f"{R2} finding 15 (pen case moved into the outfit)")
rep("china_industrial", "m.outfit", "Black cloth shoes with white soles.",
    "A small embroidered silk purse (hebao) on a knotted cord with a tassel hangs at the right hip below the magua. Black cloth shoes with white soles.",
    f"{R2} finding 15")
setv("china_industrial", "m.accessory", "none", f"{R2} finding 15 (purse moved into the outfit)")
rep("japan_industrial", "m.outfit", "knee-length black haori with small white generic family crests (never the chrysanthemum)",
    "knee-length black haori with five small plain white circles (blank crest roundels with no design inside), closed at "
    "the chest by a thick white braided silk cord (haori-himo) tied in a flat knot with two tassels",
    f"{R2} findings 15/35: the himo closes the haori; crests become blank roundels")
setv("japan_industrial", "m.accessory", "none", f"{R2} finding 15 (himo moved into the outfit)")
rep("japan_industrial", "signature", "worn with a crested haori, white himo cord", "worn with a black haori, white himo cord",
    f"{R2} finding 35")
setv("britain_industrial", "m.accessory", "Gold pocket-watch chain hanging in a clear loop across the front of the chest",
     f"{R2} finding 15: no longer names the waistcoat")
setv("greece_ancient", "m.accessory",
     "Pera: a hand-sized soft leather traveller's pouch on a thin strap slung diagonally across the body from the left shoulder",
     f"{R2} finding 15 check ('small' and 'over the himation' dropped)")
setv("greece_medieval", "m.accessory", "Belt with gilt metal mounts, worn at the waist", f"{R2} finding 15 check ('small' dropped)")
setv("egypt_medieval", "f.accessory", "Broad gold filigree collar necklace with a row of hanging pendants", f"{R2} finding 15 check ('small' dropped)")
# Coins drawn as plain discs, no crescent row (findings 34, 36).
setv("egypt_earlymodern", "f.accessory",
     "Long gold earrings, each ending in a cluster of plain gold discs with raised rims (no face, letters or marks)", f"{R2} finding 34")
setv("iraq_earlymodern", "f.accessory",
     "Gold qilada necklace hung with a row of plain gold discs with raised rims (no face, letters or marks) and teardrop pendants",
     f"{R2} findings 34/15 check")
setv("egypt_industrial", "f.accessory", "Kirdan: a gold choker-collar necklace hung with a row of teardrop and disc pendants", f"{R2} finding 36")
setv("greece_industrial", "f.accessory",
     "Necklace of plain gold discs with raised rims on a chain (the dowry flouria, drawn with no face, letters or marks)", f"{R2} finding 34")

# Hair-back rule (finding 8): back only when hair shows beside the neck behind the shoulders in front view.
setv("greece_earlymodern", "f.hair", "Long hair plaited into two braids that fall forward over the shoulders onto the chest",
     f"{R2} finding 8: the headwear line says the braids stay visible, so they hang in front")
rep("britain_ancient", "f.hair", "in two thick braids falling to the waist",
    "in two thick braids falling forward over the shoulders to the waist", f"{R2} finding 8")
setv("japan_medieval", "f.hair",
     "Long centre-parted hair worn down, drawn smoothly back behind the ears and tied once loosely at the nape; the long "
     "tail falls straight down the back, hidden from the front", f"{R2} finding 8 (a tail down the back is hidden by the body)")
setv("egypt_earlymodern", "f.hair",
     "Long hair in many thin plaits falling behind the shoulders, spread wide so they show on both sides of the neck",
     f"{R2} finding 8 (a spread curtain of plaits shows beside the neck: back)")
setv("egypt_industrial", "f.hair",
     "Centre-parted, with many long thin plaits falling behind the shoulders, spread wide so they show on both sides of "
     "the neck, each plait ending in small gold safa ornaments", f"{R2} finding 8")
setv("iraq_industrial", "f.hair", "Long hair in two braids falling down the back, hidden from the front", f"{R2} finding 8")
rep("egypt_modern", "f.hair", "in one thick plait hanging down the back", "in one thick plait hanging down the back, hidden from the front",
    f"{R2} finding 8")
rep("italy_earlymodern", "f.hair", "bound into one very long ribbon-wrapped braid down the back (coazzone). No hat.",
    "bound into one very long braid down the back (coazzone), hidden from the front. No hat.",
    f"{R2} finding 8 (the braid and its ribbon are hidden from the front; nothing to mask)")
rep("japan_earlymodern", "f.hair", "with the ends hanging down the back", "with the ends hanging down the back (hidden from the front)",
    f"{R2} finding 8")
rep("china_industrial", "m.hair", "braided into one long, neat plait down the back.",
    "braided into one long, neat plait down the back (hidden from the front).", f"{R2} finding 8")
# Hair ornaments never red (finding 31).
rep("japan_industrial", "f.hair", "a large deep-red silk ribbon bow", "a large white silk ribbon bow",
    f"{R2} finding 31: red is too close to brown hair for the ornament mask")
# Colour and wording (findings 33, 37, 38, 39, 43, 19, 25).
rep("iraq_modern", "f.outfit", "in a jewel colour", "in ruby red, sapphire blue or amber", f"{R2} finding 33")
rep("egypt_modern", "f.headwear", "cotton or chiffon headscarf", "cotton headscarf", f"{R2} finding 33 (chiffon is sheer)")
rep("egypt_ancient", "m.accessory", "(turquoise, lapis blue", "(blue-leaning turquoise, lapis blue", f"{R2} finding 33 (turquoise near the green key)")
rep("egypt_ancient", "f.accessory", "rows of turquoise, lapis", "rows of blue-leaning turquoise, lapis", f"{R2} finding 33")
rep("japan_industrial", "f.outfit", "deep wine-red-and-white yagasuri", "indigo-and-white yagasuri",
    f"{R2} finding 37: contrast with the maroon hakama")
rep("china_medieval", "m.headwear", "Keep each wing about shoulder-width so the silhouette reads as a 'T'.",
    "From wing tip to wing tip the cap is about one and a half times the shoulder width, inside the safe area, so the silhouette reads as a 'T'.",
    f"{R2} finding 38")
rep("japan_modern", "f.outfit", "Office lady (OL) company uniform: a fitted", "Office outfit: a fitted", f"{R2} finding 39 (no uniforms)")
moment("italy_ancient", "under Julius Caesar, c. 50 BCE", "as Caesar rose to power, c. 50 BCE", f"{R2} finding 43")
avoid_rep("greece_earlymodern", "(Muslim-coded and forbidden to Christians;", "(reserved for Muslims under Ottoman sumptuary law;",
          f"{R2} finding 43")
avoid_rep("iraq_medieval", "readable Qur'anic text on clothing (use decorative pseudo-script)",
          "any Arabic letters or writing, real or fake, on clothing", f"{R2} finding 19 (leftover of issue 13)")
setv("germany_modern", "m.facialHair",
     "Very bushy walrus moustache that covers the whole upper lip and droops past the corners of the mouth, wider than the mouth",
     f"{R2} finding 25: extreme enough to tell from the thick moustaches of other places")
rep("iraq_earlymodern", "m.headwear", "(cream with fine red-brown checks)", "(cream with bold red-brown checks)",
    f"{R2} finding 4: fine checks read as plain white next to the Cairo turbans")
rep("greece_ancient", "m.headwear", "a flat, wide-brimmed felt traveller's hat", "a flat, wide-brimmed traveller's hat of undyed tan felt",
    f"{R2} finding 4: a colour keeps it apart from the black wide-brimmed hats of Bologna and Weimar")
rep("china_industrial", "f.outfit", "worn over a black pleated mamian skirt.", "worn over a black pleated mamian skirt. Flat black cloth shoes.",
    f"{R2} finding 20: an explicit shoe replaces the 'bound feet' warning, which moves to the review list")

# ---------------------------------------------------------------- 2c. v2.1: the 3D office (2026-09-25)
# The game stands the flat figure behind the desk of the art side's 3D office. At the traveller anchor the NEXT sign
# and the desk hide everything below the waist (canvas y ~760), papers held up to read (piece 10) cover the figure's
# sides from about mid-chest down, and nothing shows the whole figure any more (the piece-4 Visitor window was dropped).
# So a leak item sits on the head, face, neck, shoulders or upper chest, and a MUST READ line names only what shows
# above the desk; the lower body stays in the outfit lines (it is still drawn). Brief section "What changed in v2.1".
V21 = "v2.1 (3D office)"
# Ottoman Ioannina women: the pafti buckle sits at the waist, behind the NEXT sign. The leak item becomes Epirote chest
# silverwork (Greek women's chest ornaments of silver chains; Ioannina's filigree workshops), back-projected to c. 1700
# like the pafti itself (the research's own note); the pafti stays drawn, as the buckle that closes the outfit's belt.
setv("greece_earlymodern", "f.accessory",
     "Silver chest chains: four or five rows of fine silver chains hanging in festoons across the upper chest from two "
     "round silver-filigree rosettes on the collarbones, the rosettes joined by a silver chain round the back of the "
     "neck; the chains are strung with round silver filigree beads (Ioannina silverwork)",
     f"{V21}: the pafti buckle at the waist is hidden by the desk; chest silverwork is the visible Ioannina item")
rep("greece_earlymodern", "f.outfit", "embroidered with gold cord.",
    "embroidered with gold cord. A belt at the waist is closed with a pafti: a large, ornate double-plate "
    "silver-filigree buckle (Ioannina silverwork).",
    f"{V21}: the pafti closes the outfit's belt, so it is part of the outfit (the accessory rule)")
# Tokugawa Edo women: the Nagoya-obi is wound round the hips. The leak item becomes the kazuki, the kosode worn over the
# head as a veil by women going out in the Momoyama and early Edo periods; the Nagoya-obi stays drawn, in the outfit.
setv("japan_earlymodern", "f.headwear",
     "Kazuki: a second kosode worn over the head as a veil. Its collar edge lies across the top of the forehead, it "
     "frames the fully visible face and the hair at the sides, and the robe falls over both shoulders to the upper "
     "arms. Dark ground (black, deep red or brown) with dense small motifs in divided zones, in colours different from "
     "the kosode worn on the body. The face is fully uncovered.",
     f"{V21}: a visible leak item on the head (the Nagoya-obi at the hips is hidden by the desk)")
setv("japan_earlymodern", "f.accessory", "none", f"{V21}: the Nagoya-obi moves into the outfit")
rep("japan_earlymodern", "f.outfit", "slim silhouette with no wide obi.",
    "slim silhouette with no wide obi. Round the hips, a Nagoya-obi: a braided silk cord belt wound several times and "
    "tied in front, with long tassels hanging to the knee.",
    f"{V21}: the Nagoya-obi stays drawn, as part of the outfit")
# Beijing 1972 women: the khaki satchel hangs at the hip. The leak item becomes the plain cloth peaked cap worn with the
# Zhongshan suit by men and women alike; the satchel stays drawn, as the women's (non-leakable) accessory.
setv("china_modern", "f.headwear",
     "The same soft navy cotton cap as the men's: a tall, rounded, slightly stiffened crown, a cloth band and a short "
     "stiff peak, sitting high on the head over the bob, unlike a low flat cap. Plain, with no badge or star.",
     f"{V21}: a visible leak item on the head (the khaki satchel at the hip is hidden by the desk)")
# Metapolitefsi Athens: the tagari rested at the front of the hip. No head or neck item of Athens 1975 passes the checks
# (the moustache and the 1970s cut look like other places' items; the fisherman's cap is Beijing's and London's cap), so
# the tagari stays the leak item and is drawn higher: a shorter strap brings the bag up against the side of the chest.
TAGARI = ("Tagari: a hand-woven wool shoulder bag in bold horizontal stripes with a fringed bottom, about as tall as the "
          "head, fringe included. It is worn crossbody on a short, broad strap woven in the same stripes: the strap runs "
          "from the right shoulder across the chest, and the bag rides high against the left side of the chest, its "
          "top level with the armpit")
setv("greece_modern", "m.accessory", TAGARI, f"{V21}: the bag rode at the hip, behind the desk; drawn high on the chest")
setv("greece_modern", "f.accessory", TAGARI, f"{V21}: the bag rode at the hip, behind the desk; drawn high on the chest")
# Eastern Han women: the bi-disc at mid-chest shows above the desk, but papers held up to read reach mid-chest, so it
# moves up to the upper chest (it is on its own cord round the neck since the v2 review).
rep("china_ancient", "f.accessory", "hanging at mid-chest on its own long red silk cord round the neck",
    "hanging high on the chest (its centre about a hand's width below the collarbones) on its own red silk cord round "
    "the neck", f"{V21}: raised from mid-chest to the upper chest, clear of papers held up to read")

# MUST READ lines name only what shows above the desk (the lower body stays in the outfit lines).
MUST_V21 = {
    "italy_ancient": "Men: a plain white toga with a curved, rounded edge draped over the left shoulder, over a tunic "
                     "with a narrow deep wine-red clavus stripe running down from the shoulder. Women: the nodus roll of "
                     "hair above the forehead, with the stola's straps on the shoulders.",
    "china_ancient": "The layered cross-collars (a 'y' at the throat) of a long wrap robe with broad dark borders and huge "
                     "bag-shaped 'ox-dewlap' sleeves; men add the roof-ridged black jieze cap, women the palm-wide dark "
                     "jade bi-disc pendant high on the chest",
    "japan_ancient": "Men: mizura hair loops beside the ears (the clearest silhouette cue) and a necklace of dark "
                     "grey-green comma-shaped magatama beads over a short belted jacket. Women: the flat board chignon "
                     "and a necklace of magatama beads over the jacket.",
    "italy_medieval": "Men: the cappuccio a mazzocchio (a padded ring-hat with a draped side and a long hanging "
                      "becchetto) over a red lucco gown. Women: the pearl-studded padded ghirlanda roll over bare, "
                      "drawn-back hair and a plucked high forehead.",
    "japan_medieval": "Men: a tall, soft, crumpled black eboshi cap with a wide-sleeved hitatare. Women: the wide, "
                      "knob-crowned ichime-gasa travel hat.",
    "britain_medieval": "Men: a big round silver disc brooch high on the right shoulder and the long English moustache, "
                        "with a cloak over a tunic. Women: a coloured headrail veil wrapped over the head and round the "
                        "throat, above a bell-sleeved overgown.",
    "germany_medieval": "Men: the Gugel hood with its scalloped shoulder cape, the face fully clear. Women: the Kruseler "
                        "veil with its many-layered frilled edge.",
    "greece_earlymodern": "Men: the tall, rounded, black lambskin kalpak with a full beard, over a long dark coat edged "
                          "with fur at the collar and down the front. Women: rows of fine silver chains festooned across "
                          "the upper chest over a dark gold-corded velvet waistcoat, with a red cap wrapped in a "
                          "patterned headscarf.",
    "italy_earlymodern": "Lenza: a finger-wide dark brow band with a large dark-red jewel at the centre, worn straight "
                         "across the forehead over smooth, hatless hair (men: a small soft red berretta over a "
                         "shoulder-length bob, with a dusty-coral pitocco tunic)",
    "china_earlymodern": "Tall, square, black gauze scholar's cap (women: the broad black satin baotou band worn low "
                         "across the brow, over a jacket with a standing collar and gold buttons)",
    "japan_earlymodern": "Men: the bare, shaved-pate tea-whisk topknot (chasen-mage) with a sleeveless kataginu vest "
                         "with flat, squared shoulders over a kosode. Women: the kazuki, a densely patterned kosode worn "
                         "over the head and shoulders as a veil, framing the face.",
    "iraq_industrial": "Men: the white chfiyya headcloth held by a black 'igal cord, worn with a gold-trimmed camel "
                       "'aba. Women: the black silk 'abaya drawn over the head and falling open over a brocade dress.",
    "greece_industrial": "Men: the small red fesi with a long tassel, over a full-sleeved white shirt and a dark-crimson "
                         "gold-braided jacket. Women: the gold-embroidered velvet kontogouni bolero with a small tilted "
                         "red fesi and a gold tassel.",
    "japan_industrial": "Men: a black bowler hat worn with a black haori closed by a white himo cord, a Western hat over "
                        "Japanese dress. Women: the sokuhatsu hairstyle with a large white ribbon bow, over a yagasuri "
                        "kimono.",
    "germany_industrial": "Men: a Homburg hat with pince-nez and an upturned moustache. Women: a large Jugendstil "
                          "pendant at the throat, over the embroidered yoke of a loose Reformkleid.",
    "egypt_modern": "Men: a long, dark, fringed shal round the neck, its ends down the chest, over a tailored Western "
                    "jacket worn on a galabiya. Women: a flower-edged mandil tied at the nape, with a bright printed "
                    "galabiya.",
    "greece_modern": "The tagari: a hand-woven wool bag in bold stripes with a fringed bottom, worn high on the left side "
                     "of the chest on a broad striped strap that crosses the chest (men with a thick moustache and a "
                     "shaggy 1970s cut; women with a cross-stitched folk blouse and long loose hair)",
    "china_modern": "The soft navy cap with a tall, rounded crown and a short peak, over a blue-grey Zhongshan (Mao) "
                    "jacket with a turn-down collar and buttoned patch pockets, worn by men and women alike",
    "japan_modern": "Orange-foam headphones resting around the neck (men with a dark salaryman suit, women with a "
                    "fitted office waistcoat over a blouse with a bow at the collar)",
    "britain_modern": "Men: a flat cap, with a herringbone tweed jacket over a Fair Isle pullover. Women: an "
                      "under-the-chin headscarf, with a twinset and pearls.",
    "germany_modern": "For women, the close felt cloche over a Bubikopf bob, with Bauhaus colour-block geometry on the "
                      "dress. For men, a broad-brimmed soft felt hat, a walrus moustache and round wire spectacles.",
}
for _place, _text in MUST_V21.items():
    setv(_place, "signature", _text, f"{V21}: MUST READ names only what shows above the desk")

# ---------------------------------------------------------------- 3. piece-4 proposals
# Signature (leak) item per gender: slot + short label. Culture value = "m / f" (<= 28).
SIG = {
    "egypt_ancient": (("Accessory", "wesekh collar"), ("Accessory", "wesekh collar")),
    "iraq_ancient": (("FacialHair", "curled beard"), ("Headwear", "gold fillet")),
    "greece_ancient": (("Headwear", "petasos hat"), ("Headwear", "sakkos snood")),
    "italy_ancient": (("Hair", "Caesar crop"), ("Hair", "nodus roll")),
    "china_ancient": (("Headwear", "jieze cap"), ("Accessory", "bi-disc pendant")),
    "japan_ancient": (("Hair", "mizura"), ("Accessory", "magatama beads")),
    "britain_ancient": (("Accessory", "torc"), ("Accessory", "torc")),
    "germany_ancient": (("Hair", "Suebian knot"), ("Accessory", "amber beads")),
    "egypt_medieval": (("Headwear", "imama turban"), ("Headwear", "izar hood")),
    "iraq_medieval": (("Headwear", "qalansuwa"), ("Headwear", "veil with isaba")),
    "greece_medieval": (("Headwear", "Palaiologan hat"), ("Headwear", "fakiolion")),
    "italy_medieval": (("Headwear", "mazzocchio hat"), ("Headwear", "ghirlanda")),
    "china_medieval": (("Headwear", "winged futou"), ("Headwear", "crescent comb")),
    "japan_medieval": (("Headwear", "eboshi cap"), ("Headwear", "ichime-gasa")),
    "britain_medieval": (("Accessory", "disc brooch"), ("Headwear", "headrail")),
    "germany_medieval": (("Headwear", "Gugel hood"), ("Headwear", "Kruseler veil")),
    "egypt_earlymodern": (("Headwear", "red-crown turban"), ("Headwear", "tartur")),
    "iraq_earlymodern": (("Headwear", "checked turban"), ("Headwear", "futa shawl")),
    "greece_earlymodern": (("Headwear", "kalpak"), ("Accessory", "pafti buckle")),
    "italy_earlymodern": (("Headwear", "red berretta"), ("Headwear", "lenza")),
    "china_earlymodern": (("Headwear", "square gauze cap"), ("Headwear", "baotou")),
    "japan_earlymodern": (("Hair", "chasen-mage"), ("Accessory", "Nagoya-obi")),
    "britain_earlymodern": (("Headwear", "capotain hat"), ("Headwear", "hat over coif")),
    "germany_earlymodern": (("Headwear", "Barett"), ("Headwear", "Barett")),
    "egypt_industrial": (("Headwear", "tarboush"), ("Headwear", "tarha veil")),
    "iraq_industrial": (("Headwear", "chfiyya"), ("Headwear", "abaya veil")),
    "greece_industrial": (("Headwear", "fesi"), ("Headwear", "fesi")),
    "italy_industrial": (("Accessory", "silk scarf"), ("Accessory", "coral beads")),
    "china_industrial": (("Headwear", "guapimao cap"), ("Headwear", "meile band")),
    "japan_industrial": (("Headwear", "bowler hat"), ("Hair", "sokuhatsu")),
    "britain_industrial": (("Headwear", "top hat"), ("Headwear", "poke bonnet")),
    "germany_industrial": (("Headwear", "Homburg"), ("Accessory", "Jugendstil pendant")),
    "egypt_modern": (("Accessory", "fringed shal"), ("Headwear", "mandil")),
    "iraq_modern": (("Headwear", "sidara"), ("Accessory", "gold coin pendant")),
    "greece_modern": (("Accessory", "tagari bag"), ("Accessory", "tagari bag")),
    "italy_modern": (("Accessory", "dark shades"), ("Accessory", "cat-eye shades")),
    "china_modern": (("Headwear", "navy cap"), ("Accessory", "khaki satchel")),
    "japan_modern": (("Accessory", "orange headphones"), ("Accessory", "orange headphones")),
    "britain_modern": (("Headwear", "flat cap"), ("Headwear", "headscarf")),
    "germany_modern": (("FacialHair", "walrus moustache"), ("Headwear", "cloche")),
}
SLOTKEY = {"Outfit": "outfit", "Hair": "hair", "FacialHair": "facialHair", "Headwear": "headwear", "Accessory": "accessory"}

# Wigs (drawn in their own colour, no colour variants) and hair with a back part (Claude splits it
# into the hairback layer). Proposals: the wardrobe's `wig`/`back` flags in world_source.json decide.
WIG = {("egypt_ancient", "m"), ("egypt_ancient", "f")}
# Review 2 finding 8, one test: back = true only when, in front view, the hair hangs behind the shoulders and shows
# beside the neck (loose long hair, a wide wig, a spread curtain of plaits). A single plait, a pair of braids or a tail
# down the back is hidden by the body: back = false, and the hair line says it is hidden from the front.
BACK = {("egypt_ancient", "f"), ("britain_ancient", "m"), ("greece_medieval", "m"),
        ("egypt_earlymodern", "f"), ("egypt_industrial", "f"), ("greece_modern", "f")}
# Hair ornaments Claude masks before baking hair colours (issue 16, mapped from v1 line numbers).
ORNAMENTS = {
    ("japan_ancient", "f"): "small comb", ("germany_ancient", "f"): "woven wool hair-band",
    ("china_earlymodern", "m"): "black horsehair net headband (wangjin)", ("china_earlymodern", "f"): "gold hairpins",
    ("japan_earlymodern", "m"): "white cord", ("japan_earlymodern", "f"): "white paper cord",
    ("germany_earlymodern", "f"): "gold-thread hairnet", ("egypt_industrial", "f"): "gold safa ornaments",
    ("china_industrial", "f"): "silver hairpin", ("china_modern", "f"): "black hair clip",
    ("japan_industrial", "f"): "white ribbon bow",
}

def none(t):
    return t.strip().lower().startswith("none")

# Short labels for EVERY present item (review 2 finding 2): the compare bar shows them, and Looks.LabelProblems checks
# them. Order: m = (outfit, hair, facialHair, headwear, accessory), f = (outfit, hair, headwear, accessory); None = absent.
LABELS = {
    "egypt_ancient": {"m": ("pleated linen kilt", "bobbed wig", None, None, "wesekh collar"),
                      "f": ("linen sheath dress", "tripartite wig", "lotus fillet", "wesekh collar")},
    "iraq_ancient": {"m": ("fringed wool wrap", "low chignon", "curled beard", "wool fillet", "cylinder seal"),
                     "f": ("fringed shawl-dress", "braid crown", "gold fillet", None)},
    "greece_ancient": {"m": ("chiton and himation", "forward curls", "rounded beard", "petasos hat", "pera pouch"),
                       "f": ("Ionic chiton", "wavy low bun", "sakkos snood", None)},
    "italy_ancient": {"m": ("toga and tunica", "Caesar crop", None, None, None),
                      "f": ("stola and palla", "nodus roll", None, "pearl monile")},
    "china_ancient": {"m": ("zhiju wrap robe", "Han topknot", "moustache and goatee", "jieze cap", None),
                      "f": ("quju wrap robe", "low looped bun", None, "bi-disc pendant")},
    "japan_ancient": {"m": ("kinu and tied hakama", "mizura", None, None, "magatama necklace"),
                      "f": ("kinu and mo skirt", "board chignon", None, "magatama beads")},
    "britain_ancient": {"m": ("checked tunic and bracae", "swept-back mane", "long drooping moustache", None, "torc"),
                        "f": ("checked wool dress", "two thick braids", None, "torc")},
    "germany_ancient": {"m": ("twill tunic and trousers", "Suebian knot", "full short beard", None, None),
                        "f": ("linen peplos dress", "low bound knot", None, "amber beads")},
    "egypt_medieval": {"m": ("qamis and farajiyya", "short crop", "rounded beard", "imama turban", "fringed silk girdle"),
                       "f": ("qamis and white izar", "pinned braids", "izar hood", "filigree collar")},
    "iraq_medieval": {"m": ("black durra'a robe", "smooth nape-length hair", "rounded beard", "qalansuwa", "taylasan shawl"),
                      "f": ("qamis with tiraz bands", "S-curl side locks", "veil with isaba", "pearl choker")},
    "greece_medieval": {"m": ("buttoned kabbadion", "long centre-parted hair", "long full beard", "Palaiologan hat", "gilt-mounted belt"),
                        "f": ("damask over-dress", "pinned-up hair", "fakiolion", "lunate earrings")},
    "italy_medieval": {"m": ("red lucco gown", "rounded short crop", None, "mazzocchio hat", None),
                       "f": ("gamurra and giornea", "drawn-back coil", "ghirlanda", "pearl pendant jewel")},
    "china_medieval": {"m": ("round-collared robe", "crown topknot", "thin moustache and beard", "winged futou", "plaque belt"),
                       "f": ("beizi coat and skirt", "tall gaoji bun", "crescent comb", None)},
    "japan_medieval": {"m": ("hitatare and hakama", "motodori topknot", "moustache and chin tuft", "eboshi cap", None),
                       "f": ("tsubo-shozoku robes", "long tied-back hair", "ichime-gasa", None)},
    "britain_medieval": {"m": ("cyrtel and leg bindings", "collar-length hair", "long English moustache", None, "disc brooch"),
                         "f": ("bell-sleeved overgown", "low nape coil", "headrail", None)},
    "germany_medieval": {"m": ("pleated Tappert gown", "fringed chin-length hair", None, "Gugel hood", None),
                         "f": ("high-waisted gown", "pinned plaits", "Kruseler veil", None)},
    "egypt_earlymodern": {"m": ("quftan and gibba", "short crop", "short rounded beard", "red-crown turban", "shawl sash"),
                          "f": ("brocade entari", "curtain of thin plaits", "tartur", "disc-drop earrings")},
    "iraq_earlymodern": {"m": ("zaboun and striped aba", "short crop", "full beard", "checked turban", None),
                         "f": ("striped dress and aba", "pinned braids", "futa shawl", "qilada necklace")},
    "greece_earlymodern": {"m": ("anteri and fur coat", "short crop", "full beard", "kalpak", "striped sash"),
                           "f": ("anteri and zipouni", "two front braids", "cap and tsemberi", "pafti buckle")},
    "italy_earlymodern": {"m": ("coral pitocco", "zazzera bob", None, "red berretta", None),
                          "f": ("laced-sleeve gamurra", "smooth centre parting", "lenza", "jet bead necklace")},
    "china_earlymodern": {"m": ("pale daopao robe", "topknot with wangjin", "neat moustache and beard", "square gauze cap", "tasselled sitao sash"),
                          "f": ("ao and horse-face skirt", "high flat bun", "baotou", None)},
    "japan_earlymodern": {"m": ("kataginu and hakama", "chasen-mage", "thin moustache and beard", None, None),
                          "f": ("Keicho kosode", "tamamusubi loop", None, "Nagoya-obi")},
    "britain_earlymodern": {"m": ("doublet and ruff", "brushed-up short hair", "pick-a-devant beard", "capotain hat", None),
                            "f": ("gown and lace ruff", "pinned-up hair", "hat over coif", "pomander girdle")},
    "germany_earlymodern": {"m": ("fur-collared Schaube", "short Kolbe", "short rounded beard", "Barett", "gold chain"),
                            "f": ("slashed gown and Goller", "plaits in gold net", "Barett", "layered gold chains")},
    "egypt_industrial": {"m": ("black stambouline", "short trimmed hair", "groomed moustache", "tarboush", "gold watch chain"),
                         "f": ("striped yelek robe", "curtain of thin plaits", "tarha veil", "kirdan choker")},
    "iraq_industrial": {"m": ("zaboun and camel aba", "close crop", "moustache, short beard", "chfiyya", None),
                        "f": ("brocade dress and abaya", "long braids", "abaya veil", "filigree bangles")},
    "greece_industrial": {"m": ("fustanella", "short tapered hair", "upturned moustache", "fesi", "red silk sash"),
                          "f": ("Amalia dress", "braids round the crown", "fesi", "gold disc necklace")},
    "italy_industrial": {"m": ("suit and tabarro", "pomaded side parting", "Umbertine moustache", "wide black felt hat", "silk scarf"),
                         "f": ("dress and black scialle", "high curled chignon", None, "coral beads")},
    "china_industrial": {"m": ("changshan and magua", "queue", None, "guapimao cap", None),
                         "f": ("trimmed ao and skirt", "low flat bun", "meile band", "white jade bangle")},
    "japan_industrial": {"m": ("haori and hakama", "zangiri crop", "trimmed moustache", "bowler hat", None),
                         "f": ("yagasuri and hakama", "sokuhatsu", None, None)},
    "britain_industrial": {"m": ("frock coat and stock", "curled side parting", "mutton-chop whiskers", "top hat", "watch chain"),
                           "f": ("plaid day dress", "ringlets and bun", "poke bonnet", "Paisley shawl")},
    "germany_industrial": {"m": ("Gehrock and Stehkragen", "brush cut", "turned-up moustache", "Homburg", "pince-nez"),
                           "f": ("Reformkleid", "soft low chignon", None, "Jugendstil pendant")},
    "egypt_modern": {"m": ("galabiya and jacket", "combed-back hair", "thick moustache", None, "fringed shal"),
                     "f": ("printed galabiya", "smooth plaited hair", "mandil", "gold bangles")},
    "iraq_modern": {"m": ("light wool suit", "side-parted short hair", "trimmed moustache", "sidara", "horn-rimmed glasses"),
                    "f": ("1950s dress and abaya", "permed curls", "abaya over the head", "gold coin pendant")},
    "greece_modern": {"m": ("corduroy and flares", "shaggy 1970s cut", "thick full moustache", None, "tagari bag"),
                      "f": ("embroidered blouse", "long loose hair", None, "tagari bag")},
    "italy_modern": {"m": ("slim mohair suit", "slicked-back hair", None, None, "dark shades"),
                     "f": ("print sheath dress", "bouffant", None, "cat-eye shades")},
    "china_modern": {"m": ("Zhongshan suit", "side-parted crop", None, "navy cap", "khaki satchel"),
                     "f": ("Zhongshan jacket", "clipped bob", None, "khaki satchel")},
    "japan_modern": {"m": ("salaryman suit", "combed side parting", None, None, "orange headphones"),
                     "f": ("office waistcoat set", "Seiko-chan cut", None, "orange headphones")},
    "britain_modern": {"m": ("tweed and Fair Isle", "short back and sides", None, "flat cap", "college scarf"),
                       "f": ("twinset and tweed skirt", "pin-curl set", "headscarf", "pearl strand")},
    "germany_modern": {"m": ("rumpled suit and coat", "brushed-back hair", "walrus moustache", "broad soft felt hat", "round spectacles"),
                       "f": ("Bauhaus drop-waist dress", "Bubikopf bob", "cloche", "Bauhaus necklace")},
}
SLOTS_M = ("outfit", "hair", "facialHair", "headwear", "accessory")
SLOTS_F = ("outfit", "hair", "headwear", "accessory")

# What each headwear item hides (review 2 finding 2). "all": it hides all the hair, so the game data gets
# covers: [Hair] (Looks.Compose drops the hair and CanLeak row 5 refuses a hair leak under it; the drawing must then
# come down to the hairline). "top": it hides the top of the head but hair shows around it (no covers, or the honest
# hair would vanish). "band": a band, comb or small cap that hides nothing.
HIDES = {
    ("egypt_ancient", "f"): "band", ("iraq_ancient", "m"): "band", ("iraq_ancient", "f"): "band",
    ("greece_ancient", "m"): "top", ("greece_ancient", "f"): "top", ("china_ancient", "m"): "top",
    ("egypt_medieval", "m"): "all", ("egypt_medieval", "f"): "all", ("iraq_medieval", "m"): "top", ("iraq_medieval", "f"): "top",
    ("greece_medieval", "m"): "top", ("greece_medieval", "f"): "all", ("italy_medieval", "m"): "top", ("italy_medieval", "f"): "top",
    ("china_medieval", "m"): "top", ("china_medieval", "f"): "band", ("japan_medieval", "m"): "top", ("japan_medieval", "f"): "top",
    ("britain_medieval", "f"): "all", ("germany_medieval", "m"): "all", ("germany_medieval", "f"): "all",
    ("egypt_earlymodern", "m"): "all", ("egypt_earlymodern", "f"): "top", ("iraq_earlymodern", "m"): "all", ("iraq_earlymodern", "f"): "all",
    ("greece_earlymodern", "m"): "top", ("greece_earlymodern", "f"): "top", ("italy_earlymodern", "m"): "top", ("italy_earlymodern", "f"): "band",
    ("china_earlymodern", "m"): "top", ("china_earlymodern", "f"): "band", ("britain_earlymodern", "m"): "top", ("britain_earlymodern", "f"): "all",
    ("germany_earlymodern", "m"): "top", ("germany_earlymodern", "f"): "top",
    ("egypt_industrial", "m"): "top", ("egypt_industrial", "f"): "top", ("iraq_industrial", "m"): "all", ("iraq_industrial", "f"): "all",
    ("greece_industrial", "m"): "top", ("greece_industrial", "f"): "band", ("italy_industrial", "m"): "top",
    ("china_industrial", "m"): "top", ("china_industrial", "f"): "band", ("japan_industrial", "m"): "top",
    ("britain_industrial", "m"): "top", ("britain_industrial", "f"): "top", ("germany_industrial", "m"): "top",
    ("egypt_modern", "f"): "top", ("iraq_modern", "m"): "top", ("iraq_modern", "f"): "top",
    ("china_modern", "m"): "top", ("britain_modern", "m"): "top", ("britain_modern", "f"): "top",
    ("germany_modern", "m"): "top", ("germany_modern", "f"): "top",
}
# Hair signatures whose telling part sits on the top or upper side of the head: under any "top" headwear they are
# hidden, so a leak onto such a claim would be invisible. Mizura (loops at ear level) stays visible under a hat.
HAIR_TOP_READ = {("italy_ancient", "m"), ("germany_ancient", "m"), ("japan_earlymodern", "m"),
                 ("italy_ancient", "f"), ("japan_industrial", "f")}

# v2.1 (the 3D office): the leak items the desk can see (section 2c). The v2 proposals above stay as the record.
SIG["greece_earlymodern"] = (SIG["greece_earlymodern"][0], ("Accessory", "silver chest chains"))
SIG["japan_earlymodern"] = (SIG["japan_earlymodern"][0], ("Headwear", "kazuki veil"))
SIG["china_modern"] = (SIG["china_modern"][0], ("Headwear", "navy cap"))
LABELS["greece_earlymodern"]["f"] = ("anteri and zipouni", "two front braids", "cap and tsemberi", "silver chest chains")
LABELS["japan_earlymodern"]["f"] = ("Keicho kosode", "tamamusubi loop", "kazuki veil", None)
LABELS["china_modern"]["f"] = ("Zhongshan jacket", "clipped bob", "navy cap", "khaki satchel")
HIDES[("japan_earlymodern", "f")] = "top"  # the kazuki frames the face and the side hair
HIDES[("china_modern", "f")] = "top"

places = []
problems = []
for key, e in E.items():
    rec = {"id": key, "country": e["country"], "era": e["era"], "displayName": e["displayName"],
           "year": e["year"], "moment": e["moment"], "male": e["male"], "female": e["female"],
           "mustRead": e["signature"], "avoid": e["avoid"], "wardrobe": {}, "worldSourceWardrobe": {}}
    for gi, g in enumerate(("m", "f")):
        look = e[G[g]]
        present = {"outfit": True, "hair": True,
                   "facialHair": g == "m" and not none(look["facialHair"]),
                   "headwear": not none(look["headwear"]),
                   "accessory": not none(look["accessory"])}
        slot, label = SIG[key][gi]
        if not present[SLOTKEY[slot]]:
            problems.append(f"{key} {g}: signature slot {slot} has no item")
        slots = SLOTS_M if g == "m" else SLOTS_F
        labs = dict(zip(slots, LABELS[key][g]))
        labs.setdefault("facialHair", None)
        for k in SLOTS_M:
            if bool(labs[k]) != present[k]:
                problems.append(f"{key} {g}: label/presence mismatch on {k} (label {labs[k]!r}, present {present[k]})")
        if labs[SLOTKEY[slot]] != label:
            problems.append(f"{key} {g}: signature label {label!r} != item label {labs[SLOTKEY[slot]]!r}")
        hides = HIDES.get((key, g), "") if present["headwear"] else ""
        if present["headwear"] and not hides:
            problems.append(f"{key} {g}: headwear has no HIDES entry")
        items = {}
        for k in SLOTS_M:
            if not labs[k]:
                continue
            it = {"label": labs[k]}
            if SLOTKEY[slot] == k:
                it["leakable"] = True
            if k == "hair" and (key, g) in WIG:
                it["wig"] = True
            if k == "hair" and (key, g) in BACK:
                it["back"] = True
            if k == "headwear" and hides == "all":
                it["covers"] = ["Hair"]
            items[k] = it
        rec["worldSourceWardrobe"][g] = {"signature": slot, **items}
        rec["wardrobe"][g] = {"present": present, "wig": (key, g) in WIG, "back": (key, g) in BACK,
                              "signature": {"slot": slot, "label": label, "leakable": True},
                              "labels": {k: labs[k] for k in SLOTS_M if labs[k]},
                              "headwearHides": hides,
                              "covers": ["Hair"] if hides == "all" else [],
                              "ornamentsToMask": ORNAMENTS.get((key, g), "")}
    m, f = rec["wardrobe"]["m"]["signature"]["label"], rec["wardrobe"]["f"]["signature"]["label"]
    rec["cultureValue"] = m if m.lower() == f.lower() else f"{m} / {f}"
    places.append(rec)
P2 = {p["id"]: p for p in places}

# Checks mirroring piece 4's R1 and the full Looks.LabelProblems (every item label, per gender).
def vm(a, b):  # DiscrepancyLog.ValuesMatch: trimmed, case-insensitive
    return (a or "").strip().lower() == (b or "").strip().lower()
for p in places:
    if len(p["cultureValue"]) > 28:
        problems.append(f"{p['id']}: Culture value '{p['cultureValue']}' is {len(p['cultureValue'])} > 28")
    for g in ("m", "f"):
        for k, lab in p["wardrobe"][g]["labels"].items():
            if len(lab) > 24 or not lab.isascii() or "/" in lab or not lab.strip():
                problems.append(f"{p['id']} {g} {k}: bad label '{lab}' ({len(lab)} chars)")
for g in ("m", "f"):
    for a in places:
        sa = a["wardrobe"][g]["signature"]["label"]
        for b in places:
            if b is a:
                continue
            if vm(sa, b["wardrobe"][g]["signature"]["label"]):
                problems.append(f"LabelProblems: signature label '{sa}' ({g}) used by {a['id']} and {b['id']}")
            for k, lab in b["wardrobe"][g]["labels"].items():
                if vm(lab, sa):
                    problems.append(f"LabelProblems: {b['id']} {g} {k} '{lab}' carries {a['id']}'s signature label")
cv = {}
for p in places:
    v = p["cultureValue"].lower()
    if v in cv:
        problems.append(f"Culture value '{v}' shared by {cv[v]} and {p['id']}")
    cv[v] = p["id"]

# Accessory rule (spec C7, art contract item 6, brief section 6): an accessory never hangs from, tucks into, pins or
# closes another layer's item, and is never small (review 2 finding 15).
DEPEND = re.compile(r"tucked|hanging from the (sash|belt)|pinning|closing|from just under|\bsmall |at the collar|where the", re.I)
for p in places:
    for g, G_ in (("m", "male"), ("f", "female")):
        t = p[G_]["accessory"]
        if not none(t) and DEPEND.search(t):
            problems.append(f"{p['id']} {g} accessory depends on another layer or is small: ...{DEPEND.search(t).group(0)}...")

# Desk-view rule (v2.1): the desk and its NEXT sign hide the figure below the waist, so no leak accessory may sit there
# (hair, facial hair and headwear start on the head, and a long veil still reads there) and no MUST READ line may name
# a lower-body feature (the outfit lines keep the lower body: it is still drawn).
LOWER = re.compile(r"\b(waist|hips?|thighs?|knees?|ankles?|legs?|feet|foot|shoes?|boots?|sandals?|Schnabelschuhe|calcei|"
                   r"trousers|bracae|hakama|skirts?|kilt|fustanella|flares|jeans|obi|hose|stockings)\b", re.I)
for p in places:
    for g, G_ in (("m", "male"), ("f", "female")):
        sig = p["wardrobe"][g]["signature"]
        if sig["slot"] == "Accessory" and LOWER.search(p[G_][SLOTKEY[sig["slot"]]]):
            problems.append(f"{p['id']} {g}: the leak item ({sig['label']}) sits below the desk: "
                            f"...{LOWER.search(p[G_][SLOTKEY[sig['slot']]]).group(0)}...")
    if LOWER.search(p["mustRead"]):
        problems.append(f"{p['id']}: MUST READ names a feature below the desk: ...{LOWER.search(p['mustRead']).group(0)}...")

# Confusable pairs (looks.confusable). Hand-authored: signature items that read alike in their slot (review 2
# findings 4 and 25; spec 2.14). Unordered; CanLeak refuses a leak between the two places in that slot and gender.
CONFUSABLE = [
    ("greece_ancient", "italy_ancient", "Hair", "m", "short forward-brushed crops look alike (spec 2.14)"),
    ("egypt_industrial", "greece_industrial", "Headwear", "m", "tarboush vs fesi: both red fez-type caps (research note 15)"),
    ("iraq_medieval", "japan_medieval", "Headwear", "m", "two tall black caps (research note 6)"),
    ("china_earlymodern", "greece_earlymodern", "Headwear", "m", "tall black caps; check at thumbnail (research note 11)"),
    ("china_modern", "britain_modern", "Headwear", "m", "soft peaked caps (research note 20)"),
    ("britain_earlymodern", "britain_industrial", "Headwear", "m", "capotain vs top hat: two tall black narrow-brimmed hats"),
    ("egypt_medieval", "egypt_earlymodern", "Headwear", "m", "two big white turbans; only the red crown tells them apart"),
    ("germany_industrial", "germany_modern", "Headwear", "m", "Homburg vs the pinched soft felt hat: two dark dented felt hats"),
    ("iraq_ancient", "greece_medieval", "FacialHair", "m", "chest-length curled beard vs the Byzantine long full beard"),
    ("iraq_ancient", "germany_medieval", "FacialHair", "m", "the Gugel's neck and cape hide most of a chest-length beard"),
    ("germany_modern", "greece_modern", "FacialHair", "m", "walrus vs thick full moustache (same day-5 world)"),
    ("germany_modern", "greece_industrial", "FacialHair", "m", "walrus vs thick upturned moustache"),
    ("germany_modern", "italy_industrial", "FacialHair", "m", "walrus vs full, thick Umbertine moustache (research note 19)"),
    ("germany_modern", "egypt_modern", "FacialHair", "m", "walrus vs thick, neat moustache"),
    ("germany_modern", "egypt_industrial", "FacialHair", "m", "walrus vs full, groomed moustache"),
    ("germany_modern", "iraq_industrial", "FacialHair", "m", "walrus vs heavy moustache (with a short beard)"),
    ("germany_modern", "britain_ancient", "FacialHair", "m", "walrus vs long drooping moustache past the mouth corners"),
    ("germany_modern", "britain_medieval", "FacialHair", "m", "walrus vs long full English moustache"),
    ("egypt_modern", "iraq_medieval", "Accessory", "m", "long dark fringed shal vs dark taylasan, both ends down the chest"),
    ("iraq_industrial", "iraq_modern", "Headwear", "f", "both are a black 'abaya over the head"),
    ("iraq_earlymodern", "iraq_industrial", "Headwear", "f", "black futa head-shawl vs black 'abaya over a black futa"),
    ("iraq_earlymodern", "iraq_modern", "Headwear", "f", "black futa head-shawl vs black 'abaya over the head"),
    ("egypt_industrial", "egypt_medieval", "Headwear", "f", "white tarha veil over both shoulders vs white izar hood"),
    ("iraq_medieval", "egypt_medieval", "Headwear", "f", "light khimar veil vs white izar hood (only the 'isaba band differs)"),
    ("iraq_medieval", "egypt_industrial", "Headwear", "f", "light khimar veil vs white tarha veil (only the 'isaba band differs)"),
    ("china_earlymodern", "china_industrial", "Headwear", "f", "two black brow bands"),
    ("italy_earlymodern", "china_earlymodern", "Headwear", "f", "dark brow bands with a central jewel (research note 14)"),
    ("italy_earlymodern", "china_industrial", "Headwear", "f", "dark brow bands with a central jewel"),
    ("egypt_modern", "britain_modern", "Headwear", "f", "two triangular headscarves (research note 22)"),
    ("egypt_modern", "greece_earlymodern", "Headwear", "f", "coloured headscarves tied at the back"),
    ("greece_ancient", "egypt_modern", "Headwear", "f", "patterned sakkos vs mandil: cloth covering the back of the head"),
    ("greece_ancient", "greece_earlymodern", "Headwear", "f", "patterned sakkos vs patterned tsemberi"),
    ("greece_industrial", "greece_earlymodern", "Headwear", "f", "two small red caps on the crown"),
    ("germany_ancient", "italy_industrial", "Accessory", "f", "chunky orange amber vs red coral beads"),
    ("iraq_modern", "greece_industrial", "Accessory", "f", "plain gold discs at the neck (coin pendant vs disc necklace)"),
    ("iraq_modern", "iraq_earlymodern", "Accessory", "f", "plain gold discs at the neck (coin pendant vs qilada)"),
]
# v2.1: the new leak items, checked against the candidate grid (kept apart so the brief can tell v2's pairs from them).
CONFUSABLE_V21 = [
    ("greece_earlymodern", "germany_earlymodern", "Accessory", "f", "festoons of chains on the chest (silver vs layered gold)"),
]
CONFUSABLE += CONFUSABLE_V21
# Generated: a hair signature read on the top of the head is hidden under a claim's "top" headwear (review 2 finding 2).
CONFUSABLE_HIDDEN = []
hand = {(frozenset((a, b)), sl, g) for a, b, sl, g, _ in CONFUSABLE}
for home, g in sorted(HAIR_TOP_READ):
    sig = P2[home]["wardrobe"][g]["signature"]
    assert sig["slot"] == "Hair", home
    for p in places:
        if p["id"] != home and p["wardrobe"][g]["headwearHides"] == "top" and (frozenset((p["id"], home)), "Hair", g) not in hand:
            CONFUSABLE_HIDDEN.append((p["id"], home, "Hair", g,
                                      f"{sig['label']} hidden under the {p['wardrobe'][g]['labels']['headwear']}"))
seen_pairs = set()
for a, b, sl, g, _ in CONFUSABLE + CONFUSABLE_HIDDEN:
    k = (frozenset((a, b)), sl, g)
    if k in seen_pairs:
        problems.append(f"duplicate confusable {a}/{b} {sl} {g}")
    seen_pairs.add(k)
    for pid in (a, b):
        if pid not in P2:
            problems.append(f"confusable names unknown place {pid}")
    if not any(P2[x]["wardrobe"][g]["signature"]["slot"] == sl for x in (a, b)):
        problems.append(f"confusable {a}/{b} {sl} {g}: neither place's signature is in that slot (unreachable)")
# Spec 2.14's Industrial facial-hair trio (#19): no Industrial man's signature is facial hair, so those pairs are
# unreachable; the walrus pairs above cover the Modern moustache that CAN leak onto them.
assert not any(P2[f"{c}_industrial"]["wardrobe"]["m"]["signature"]["slot"] == "FacialHair" for c in ("greece", "italy", "germany"))

# Candidate list for Saleh's review: every signature against every other place's same-gender item in its slot.
cand = ["Signature items against every other place's item in the same slot and gender (review list for confusable pairs).",
        "PAIR = already a confusable pair; COVERS = the claim's headwear hides all hair; HIDDEN = generated 'top' pair.", ""]
pairset = {(frozenset((a, b)), sl, g) for a, b, sl, g, y in CONFUSABLE}
hidset = {(frozenset((a, b)), sl, g) for a, b, sl, g, _ in CONFUSABLE_HIDDEN}
for g in ("m", "f"):
    for home in places:
        sig = home["wardrobe"][g]["signature"]
        k = SLOTKEY[sig["slot"]]
        cand.append(f"[{g}] {home['id']} {sig['slot']} '{sig['label']}':")
        for claim in places:
            if claim is home:
                continue
            lab = claim["wardrobe"][g]["labels"].get(k, "-")
            key3 = (frozenset((home["id"], claim["id"])), sig["slot"], g)
            tag = "PAIR" if key3 in pairset else ("HIDDEN" if key3 in hidset else "")
            if sig["slot"] == "Hair" and claim["wardrobe"][g]["covers"]:
                tag = (tag + " COVERS").strip()
            cand.append(f"    {claim['id']:<20} {lab:<26} {tag}")
(OUT / "confusable_candidates.txt").write_text("\n".join(cand) + "\n", encoding="utf-8")

# DO NOT DRAW split (review 2 finding 20): ChatGPT gets the likely confusions and anachronisms; caricature, slur,
# hate-symbol and likeness entries go to a review list for Saleh and Claude (Block A already bans them in general terms).
# (place, index) -> (text for ChatGPT or None, text for the review list or None)
AVOID_SPLIT = {
    ("japan_ancient", 3): ("red face paint (seen on haniwa figures)", None),
    ("germany_ancient", 1): ("Viking-age or opera-costume looks", "Viking or Wagnerian-opera stereotypes"),
    ("britain_medieval", 0): ("horned or winged helmets, a Viking-raider look", None),
    ("china_ancient", 5): ("conical straw hat (douli)", "'coolie' hat stereotype, Fu Manchu moustache, slanted-eye caricature"),
    ("britain_ancient", 7): ("fur loincloths or pelts", "'savage barbarian' caricature"),
    ("germany_ancient", 2): ("fur trims, pelts or fur loincloths", "barbarian caricature"),
    ("egypt_medieval", 6): ("belly-dance costume", "harem cliches"),
    ("iraq_medieval", 0): ("curled-toe slippers, balloon 'genie' trousers, onion domes", "'Arabian Nights' or Aladdin cliches"),
    ("iraq_medieval", 1): ("face-covering veils", "harem stereotypes"),
    ("china_medieval", 6): ("conical straw hat (douli)", "'coolie' stereotype or any facial caricature"),
    ("germany_medieval", 2): ("a pointed or funnel-shaped hat, or any marker or badge on the clothing", "the 'Judenhut' or any discriminatory marker"),
    ("egypt_earlymodern", 7): (None, "odalisque or harem imagery"),
    ("china_earlymodern", 3): ("long drooping moustaches", "Fu Manchu caricature or caricatured features"),
    ("china_earlymodern", 8): ("conical straw hat (douli)", "'coolie' stereotype"),
    ("britain_earlymodern", 4): ("a buckle on the hatband (buckled hats are a later myth)", "'Puritan' caricature"),
    ("egypt_industrial", 7): (None, "Orientalist harem imagery"),
    ("italy_industrial", 3): ("a sinister, cloaked-villain look", "mafia or gangster caricature"),
    ("italy_industrial", 4): (None, "organ-grinder or peasant caricature"),
    ("china_industrial", 0): ("long drooping moustache", "'pigtail' cartoons, Fu Manchu, slanted-eye features"),
    ("china_industrial", 1): (None, "opium pipes or opium imagery"),
    ("china_industrial", 5): (None, "any emphasis on bound feet (the outfit names flat cloth shoes)"),
    ("china_industrial", 6): ("conical straw hat (douli)", "'coolie' stereotype"),
    ("britain_industrial", 7): (None, "Dickensian ragged-urchin or chimney-sweep caricature"),
    ("britain_industrial", 8): ("a monocle", "'toff' caricature"),
    ("germany_industrial", 3): ("a monocle", "Prussian-officer caricature"),
    ("germany_industrial", 4): ("a moustache waxed into long needle points reaching the cheekbones", "Kaiser caricature"),
    ("egypt_modern", 3): (None, "a likeness of Nasser or Umm Kulthum"),
    ("egypt_modern", 7): ("belly-dance costume or coin hip-scarves", "'harem' caricature"),
    ("iraq_modern", 1): ("flags or eagle emblems", "Ba'ath-era imagery"),
    ("iraq_modern", 7): (None, "a likeness of King Faisal, Zaha Hadid or any real person"),
    ("greece_modern", 0): ("any uniform, insignia or phoenix emblem", "junta-era (1967-74) symbols"),
    ("greece_modern", 5): (None, "'Zorba' or taverna-dancer caricature"),
    ("italy_modern", 0): ("a black shirt worn as a uniform, or any insignia", "Fascist-era imagery (black shirts, fasces)"),
    ("italy_modern", 2): ("pinstripes, a fedora or a violin case", "mafia or gangster caricature"),
    ("italy_modern", 3): ("a striped gondolier shirt or a straw boater", "pizza-chef or gondolier stereotypes"),
    ("china_modern", 0): ("any badge or pin on the jacket", "Mao badges or portrait pins"),
    ("china_modern", 1): ("a star or badge on the cap, a green army-style uniform, armbands", "PLA or Red Guard imagery"),
    ("china_modern", 2): ("a book in the hands, slogans or any text", "Little Red Book or propaganda"),
    ("china_modern", 5): ("conical straw hat (douli)", "'coolie' stereotype or any caricature"),
    # v2.2 (the ReStory style): the brief draws everyone in a cute anime style, so a ban on anime styling would ban the
    # style itself; the research's worry (anime caricature) becomes costume cliches, plus the school uniform the office
    # outfit (blouse with a bow, pleated skirt) could drift into in this style.
    ("japan_modern", 3): ("cosplay, idol-costume or anime-costume cliches; a school-uniform look; samurai or geisha looks", None),
    ("britain_modern", 7): (None, "a likeness of Alan Turing"),
    ("germany_modern", 0): ("any uniform, armband or insignia", "swastika or any other regime symbol"),
    ("germany_modern", 1): ("brown or black shirts, jackboots, leather trench coats", "secret-police stereotype"),
    ("germany_modern", 5): ("lederhosen or dirndl", "beer-hall caricature"),
    ("germany_modern", 6): ("fishnets or lingerie", "'Cabaret' decadence caricature"),
    ("germany_modern", 7): ("wild white hair or a famous-portrait pose", "a likeness of Einstein"),
}
SENSITIVE = re.compile(r"coolie|slant|Fu Manchu|swastika|Judenhut|harem|savage|caricatur|stereotyp|likeness|Nazi|fascis|junta|Ba.ath|\bMao\b|opium|bound feet", re.I)
for p in places:
    chat, rev = [], []
    for i, a in enumerate(p["avoid"]):
        if (p["id"], i) in AVOID_SPLIT:
            c, r = AVOID_SPLIT[(p["id"], i)]
            if c: chat.append(c)
            if r: rev.append(r)
        else:
            chat.append(a)
    for c in chat:
        if SENSITIVE.search(c):
            problems.append(f"{p['id']}: sensitive term left in the ChatGPT DO NOT DRAW: {c}")
    p["avoidChatGPT"], p["avoidReview"] = chat, rev
for (pid, i) in AVOID_SPLIT:
    assert i < len(P2[pid]["avoid"]), (pid, i)

# Appendix A status (review 2 finding 42): "applied" only when the reviewer's text is still used exactly.
def fix_value(place, field):
    e = E[place]
    if field == "avoid":
        return " ; ".join(e["avoid"])
    if field == "signature":
        return e["signature"]
    g, k = field.split(".")
    return e[g][k]
GINV = {"male": "m", "female": "f"}
for x, f in zip(fix_log, review["fixes"]):
    cur = fix_value(x["place"], x["field"])
    want = f["newValue"] if x["field"] != "avoid" else " ; ".join(s.strip() for s in f["newValue"].split(" ; ") if s.strip())
    if x["status"] == "applied" and cur != want:
        if x["field"] in ("signature", "avoid"):
            fk = x["field"]
        else:
            g, k = x["field"].split(".")
            fk = f"{GINV[g]}.{k}"
        whys = sorted({w for (pl, fl, _, w) in edit_log if pl == x["place"] and fl == fk})
        x["status"] = "applied, then amended"
        x["note"] = "; ".join(whys) if whys else "amended"

# Future motifs (issue 10 edits).
MOT = {m["country"]: m["motifs"] for m in data["futureMotifs"]}
def mrep(c, old, new):
    assert old in MOT[c], (c, old)
    MOT[c] = MOT[c].replace(old, new)
mrep("egypt", " the tarboush's truncated cone as a sleek crimson cap with a single tassel;", "")
mrep("iraq", "the Abbasid tall-cap silhouette and taylasan shoulder shawl as a high hood-collar", "the taylasan shoulder shawl as a draped shoulder panel")
mrep("iraq", "; the sidara's folded boat-shaped crown", "")
mrep("iraq", "tiraz armbands in pseudo-script", "tiraz armbands of abstract embroidered bands (no letters)")
mrep("iraq", "embossed cuneiform-wedge texture and cylinder-seal roll-print bands (decorative, not readable)", "embossed wedge-shaped texture and cylinder-seal roll-print bands of abstract figures (no writing)")
mrep("greece", "as glowing trim lines", "as fine metallic trim lines")
mrep("greece", "Striped hand-woven crossbody bags echoing the tagari, and tall brimmed crown shapes echoing the Palaiologan hat.", "Striped hand-woven panels echoing the tagari.")
mrep("italy", "iridescent", "sheen")
mrep("italy", " with dark visor lenses", "")
mrep("italy", "A thin lenza forehead band reimagined as a light strip, with Vitruvian", "Vitruvian")
mrep("china", "glowing circuit-trace trim", "fine metallic circuit-trace trim")
mrep("china", "sheer layered panels", "layered opaque panels")
mrep("china", "translucent layers like xuan paper pages", "layered pale panels like xuan paper pages")
mrep("china", "Jade-like translucent fastenings", "Dark jade-like stone fastenings")
mrep("china", "celadon", "dull grey-green celadon")
mrep("japan", "laser-cut or LED line work", "laser-cut or fine stitched line work")
mrep("japan", "Visible karakuri-style mechanical joints", "Karakuri-inspired hinged seam details (fabric, not machinery)")
mrep("japan", "soft sakura-pink accents", "pale coral accents")
mrep("britain", "An open neck ring inspired by the torc.", "A torc-inspired twisted collar edge sewn into the neckline.")
mrep("britain", "Pleated translucent collar rings", "Pleated opaque white collar rings, kept below the chin,")
mrep("britain", "racing green", "dark bottle green")
mrep("germany", "round lens-like visor or spectacle elements", "round lens-like buttons and fastenings")
mrep("germany", " A wide, flat, soft Barett-style cap.", "")
mrep("germany", "collars framing the face", "collars kept below the chin")
mrep("germany", "Movable-type grid texture (letterforms used as pattern, never as slogans)", "A fine movable-type grid texture (no letters)")
# Review 2: nothing cut through the fabric (finding 29), no figures or characters (19, 43), no green-key colours (33),
# no black-white-red trio (43).
mrep("egypt", "mashrabiya lattice perforation", "a printed or embroidered mashrabiya lattice pattern (not cut-through holes)")
mrep("egypt", "Nile turquoise", "blue-leaning Nile turquoise")
mrep("iraq", "cylinder-seal roll-print bands of abstract figures (no writing)", "roll-print bands of abstract geometric pattern (no figures, no writing)")
mrep("china", "Movable-type grids", "A fine movable-type grid texture (blank squares, no characters)")
mrep("japan", "rendered as laser-cut or fine stitched line work", "rendered as laser-cut-look stitched line work (the fabric stays solid)")
mrep("britain", "punch-card perforation grids", "printed punch-card dot grids (not cut-through holes)")
mrep("germany", "as embroidered or laser-cut trim", "as embroidered or laser-cut-look stitched trim (the fabric stays solid)")
mrep("germany", "Baltic amber and linen-white accents.", "Baltic amber and linen-white accents, with the white kept away from red-and-black areas.")

FUTURE_NAMES = {"egypt": "Nile Arcology", "iraq": "Baghdad Garden City", "greece": "Aegean Commonwealth",
                "italy": "Mediterranean Union Rome", "china": "Shanghai Megacity", "japan": "Neo-Tokyo Bay",
                "britain": "Thames Barrier London", "germany": "Rhine-Ruhr Metropole"}
FUTURE_LABEL = {"egypt": "bead-row yoke linen", "iraq": "fringed lapis coat", "greece": "meander-trim drape",
                "italy": "clavus-stripe wrap", "china": "pankou cross-collar coat", "japan": "kasane-collar layers",
                "britain": "tweed-grid frock coat", "germany": "Bauhaus colour panels"}
future = [{"id": f"{c}_future", "country": c, "displayName": FUTURE_NAMES[c], "year": 2150,
           "motifs": MOT[c], "cultureValue": FUTURE_LABEL[c]} for c in MOT]
# Future wardrobes (piece 5 section 2.14 / R18): the outfit is the signature (a Future home never leaks), hair and
# facial hair are shared through artNation "neutral", no headwear or accessory (review 2 finding 9).
FUTURE_HAIR = {"m": "short textured crop", "f": "sleek low bun"}
FUTURE_BEARD = "short trimmed beard"
for fp in future:
    fp["worldSourceWardrobe"] = {
        "m": {"signature": "Outfit", "outfit": {"label": fp["cultureValue"]},
              "hair": {"label": FUTURE_HAIR["m"], "artNation": "neutral"},
              "facialHair": {"label": FUTURE_BEARD, "artNation": "neutral"}},
        "f": {"signature": "Outfit", "outfit": {"label": fp["cultureValue"]},
              "hair": {"label": FUTURE_HAIR["f"], "artNation": "neutral"}}}
    if len(fp["cultureValue"]) > 24 or fp["cultureValue"].lower() in cv:
        problems.append(f"future {fp['id']} label problem")
    for g in ("m", "f"):
        for k, it in fp["worldSourceWardrobe"][g].items():
            if k == "signature":
                continue
            for pl in places:
                if vm(it["label"], pl["wardrobe"][g]["signature"]["label"]):
                    problems.append(f"LabelProblems: {fp['id']} {g} {k} carries {pl['id']}'s signature label")

# The present (2150, traveller types H1) and its accessory kit (C2-C4, v2.3). The present's clothes are the neutral
# Future outfits and hair (their labels as world_source.json "present" authors them). The kit: one small set per gender,
# the same four items, each on the head, face, neck, shoulders or upper chest (v2.1's desk-view rule), standing on its
# own, filed under the art nation "neutral" with its own variant token (accessory_{g}_neutral_future_{variant}).
PRESENT_CLOTHES = {"m": {"outfit": "tech jacket", "hair": "short textured crop"},
                   "f": {"outfit": "coat-dress", "hair": "sleek low bun"}}
PRESENT_KIT = [
    {"variant": "lenses", "label": "smart lenses",
     "look": "a pair of slim rimless smart lenses: two small rounded-rectangle lenses of flat, opaque pale blue-grey joined by a thin slate-grey bridge, sitting on the nose in front of the eyes"},
    {"variant": "earpiece", "label": "comm earpiece",
     "look": "a sleek slate teal-blue comm earpiece hooked over the ear on the viewer's left, with a short slim boom reaching forward along the cheek"},
    {"variant": "badge", "label": "transit badge",
     "look": "a large rounded-rectangle transit badge in sand with slate teal-blue geometric panels, clipped to the upper chest on the viewer's right (no text, numbers or symbols)"},
    {"variant": "display", "label": "shoulder display",
     "look": "a curved slate-grey display panel strapped over the shoulder on the viewer's left, its face flat matte teal-blue (not lit, no text)"},
]
for k in PRESENT_KIT:
    if len(k["label"]) > 24:
        problems.append(f"present kit {k['variant']} label over 24 characters")
    for pl in places:
        for g in ("m", "f"):
            if vm(k["label"], pl["wardrobe"][g]["signature"]["label"]):
                problems.append(f"LabelProblems: present kit {k['variant']} carries {pl['id']}'s {g} signature label")

# Colour audit: risky words left anywhere a ChatGPT prompt will read.
RISK = re.compile(r"\b(pink|purple|violet|magenta|lime|mauve|lilac|rose|green|emerald|jade|sheer|translucent|glow\w*)\b", re.I)
audit = []
for p in places:
    for g in ("male", "female"):
        for k, v in p[g].items():
            for mm in RISK.finditer(v):
                audit.append(f"{p['id']} {g}.{k}: {mm.group(0)} :: ...{v[max(0, mm.start()-40):mm.end()+30]}...")
    for mm in RISK.finditer(p["mustRead"]):
        audit.append(f"{p['id']} mustRead: {mm.group(0)}")
for fp in future:
    for mm in RISK.finditer(fp["motifs"]):
        audit.append(f"{fp['id']} motifs: {mm.group(0)}")
for k in PRESENT_KIT:
    for mm in RISK.finditer(k["look"]):
        audit.append(f"present kit {k['variant']}: {mm.group(0)}")

out = {"places": places, "future": future, "presentClothes": PRESENT_CLOTHES, "presentKit": PRESENT_KIT, "fixLog": fix_log,
       "confusable": [{"a": a, "b": b, "slot": sl, "gender": g, "why": y} for a, b, sl, g, y in CONFUSABLE],
       "confusableHidden": [{"a": a, "b": b, "slot": sl, "gender": g, "why": y} for a, b, sl, g, y in CONFUSABLE_HIDDEN],
       "confusableAddedV21": len(CONFUSABLE_V21),
       "editLog": [{"place": a, "field": b, "change": c, "why": d} for a, b, c, d in edit_log],
       "problems": problems}
(OUT / "wardrobe_v2.json").write_text(json.dumps(out, ensure_ascii=False, indent=1), encoding="utf-8")
print("places", len(places), "future", len(future), "edits", len(edit_log),
      "confusable", len(CONFUSABLE), "hidden", len(CONFUSABLE_HIDDEN))
print("PROBLEMS:", *problems, sep="\n  ")
print("AUDIT:", *audit, sep="\n  ")
