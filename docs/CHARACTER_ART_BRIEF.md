# Time Sorter: Character Art Brief for ChatGPT

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

## 7. Batch 1: pilot (11 images)

This small batch proves the pieces stack before anyone makes hundreds of images. Folder: `Raw/batch1-pilot/`.

**Step 1.** Use prompt 6.1 twice, for skin tone 1, face a:

- `base_m_skin1_facea.png`
- `base_f_skin1_facea.png`

**Send to Claude.** Claude splits each figure into a body and a head, and sends back `mannequin_m.png` and `mannequin_f.png`. Upload both to the Project files.

**Step 2.** Paste this PAIR block, then use prompts 6.4 to 6.8 for each file below it:

```
PAIR: britain_industrial (Victorian Britain, 1843)
Place and time: London in 1843, when Ada Lovelace published her notes on Babbage's Analytical Engine, as the Cooke-Wheatstone electric telegraph spread and just before Railway Mania (1845-47)

MAN
- Outfit: Knee-length black or bottle-green wool frock coat with a nipped waist and full skirt, over a patterned silk waistcoat and narrow fawn or checked trousers. A white shirt with tall collar points, wrapped with a wide black silk neck-stock or cravat.
- Hair: Side-parted hair, full and curled over the ears
- Facial hair: Mutton-chop side-whiskers running down the jaw, with the upper lip and chin clean-shaven
- Headwear: Tall black silk top hat (stovepipe) with a slightly curled brim
- Accessory: Gold pocket-watch chain looped across the waistcoat

WOMAN
- Outfit: Day dress with a tight bodice that comes to a point at the waist, low sloping shoulders, narrow long sleeves and a full bell-shaped skirt over layered petticoats. It is made in dark plaid silk or printed cotton, with a small white lace collar.
- Hair: Early-Victorian centre parting: hair smoothed flat over the crown, with clusters of ringlets over each ear and a bun at the back
- Headwear: Deep straw poke ('coal-scuttle') bonnet that frames the face, tied under the chin with a wide ribbon. The face stays fully visible.
- Accessory: Large Paisley shawl, woven in Paisley, Scotland, with the teardrop 'boteh' pattern, draped around the shoulders

MUST READ AT A GLANCE: Men: a tall black silk top hat with mutton-chop whiskers and a clean upper lip. Women: a coal-scuttle bonnet with a Paisley shawl.
DO NOT DRAW: redcoat, police, guards or any uniform; Union Jack or royal regalia; steampunk goggles, gears or other anachronistic gadgets; Homburg hat or upturned waxed moustache (Germany-industrial); loose uncorseted Reformkleid or Jugendstil ornament (Germany-industrial); plain black fringed shawl with a bare chignon and coral beads (Italy-industrial); bustle silhouette (1870s-80s, the wrong decade); Dickensian ragged-urchin or chimney-sweep caricature; monocle-and-top-hat 'toff' caricature
```

Files: `outfit_m_britain_industrial.png`, `hair_m_britain_industrial.png`, `facialhair_m_britain_industrial.png`, `headwear_m_britain_industrial.png`, `accessory_m_britain_industrial.png`, `outfit_f_britain_industrial.png`, `hair_f_britain_industrial.png`, `headwear_f_britain_industrial.png`, `accessory_f_britain_industrial.png`

**Send to Claude.** Claude stacks everything in Unity. If the pieces line up, go on to Batch 2. If not, we fix the prompts before making more.

## 8. Batch 2: skin tones and faces (38 images)

Folder: `Raw/batch2-base/`.

- **8 base figures** with prompt 6.2, from your approved skin-1 figures: `base_[m/f]_skin[2-5]_facea.png`
- **30 heads** with prompt 6.3, faces b, c and d for every skin tone and both genders: `head_[m/f]_skin[1-5]_face[b/c/d].png`

**Send to Claude.**

## 9. Batch 3: Ancient era, before 500 CE (59 images)

Folder: `Raw/ancient/`. Start a new chat for this era. For each country, paste its PAIR block, then make each file in its list.

### Egypt: New Kingdom Egypt

```
PAIR: egypt_ancient (New Kingdom Egypt, 1470 BCE)
Place and time: Thebes during the reign of Hatshepsut, c. 1470 BCE (her architect Senenmut; 'scribes of the fields' re-measuring land with knotted ropes after each Nile flood)

MAN
- Outfit: Calf-length wrap-around kilt of fine bleached white linen, knotted at the waist, with the overlapping front edge falling in a slanting, finely pleated panel. Bare torso; plain papyrus or leather sandals. Everything is white or ecru linen, so the bright beadwork at the neck stands out.
- Hair: Official's bobbed wig: straight black wig cut bluntly at shoulder length with a short straight fringe, falling over the ears or tucked behind them (head shaved underneath)
- Facial hair: none (clean-shaven; the false beard was royal and must not be used)
- Headwear: none
- Accessory: Wesekh broad collar: a flat semicircular collar of many rows of faience beads (turquoise, lapis blue, carnelian red, gold) with a row of teardrop pendants on the outer edge, covering the upper chest and shoulders

WOMAN
- Outfit: Ankle-length, close-fitting sheath dress of fine white linen that covers the bust and is held up by two broad shoulder straps; papyrus sandals or barefoot. No overgarment: the sheer pleated over-robes came one or two generations later.
- Hair: Tripartite wig: a long, heavy, straight black wig parted in the centre, with two thick lappets falling in front of the shoulders to the chest and the rest hanging down the back
- Headwear: Thin floral fillet: a narrow band of blue-lotus petals tied round the wig, with a single lotus bud over the forehead
- Accessory: Wesekh broad collar of rows of turquoise, lapis, carnelian and gold faience beads with teardrop pendants

MUST READ AT A GLANCE: The broad beaded wesekh collar worn over crisp white linen, with a heavy black wig (kohl-winged eyes on the face layer)
DO NOT DRAW: nemes striped headcloth, uraeus cobra, vulture crown, blue khepresh crown (all royal or divine); false beard, crook and flail; Hollywood 'Cleopatra' styling or gold lame; sheer pleated over-robes, knotted shawls and layered 'duplex' wigs (later 18th-dynasty and Amarna fashion); perfume/wax cones on the head (banquet-only, and they read as a hat); Greek draped chiton/himation and fibula brooches (reads as Greece-ancient; Ptolemaic Alexandria was largely Greek-dressed); coloured or fringed wool wraps and long beards (read as Mesopotamia); ankh or god emblems as ornaments; mummy wrappings, pyramids or sphinx props
```

Files: `outfit_m_egypt_ancient.png`, `hair_m_egypt_ancient.png`, `accessory_m_egypt_ancient.png`, `outfit_f_egypt_ancient.png`, `hair_f_egypt_ancient.png`, `headwear_f_egypt_ancient.png`, `accessory_f_egypt_ancient.png`

### Iraq / Mesopotamia: Babylonia

```
PAIR: iraq_ancient (Babylonia, 1760 BCE)
Place and time: Babylon under Hammurabi, c. 1760 BCE (the law code, clay contracts sealed with cylinder seals)

MAN
- Outfit: Ankle-length wrap of heavy dyed wool (madder red, blue or yellow) wound round the body and thrown over the left shoulder, leaving the right shoulder and arm bare. Every edge has a thick rolled fringe or tufted border, and a wide cloth belt holds it at the waist. Plain leather sandals.
- Hair: Long hair drawn back into a low, rounded chignon (bun) at the nape
- Facial hair: Full, long beard reaching the upper chest, squared at the bottom, combed into long wavy strands that end in neat rows of small curls, with a moustache
- Headwear: Narrow twisted-wool headband (fillet) circling the head and holding the hair
- Accessory: Lapis-lazuli cylinder seal: a finger-length carved stone cylinder hung on a cord around the neck. It was the owner's signature, rolled onto clay contracts.

WOMAN
- Outfit: Ankle-length shawl-dress of dyed wool (saffron-yellow, rust or blue), wound round the body with the end thrown over the left shoulder so the right shoulder is bare. A thick contrasting fringe runs along its edges and spirals down the body in one or two bands. It is a fringed wrap, not a tiered flounced robe.
- Hair: Centre-parted hair braided into one thick plait wound round the head like a crown
- Headwear: A wide gold-thread ribbon wound over the coiled braid, hung with small gold ring pendants
- Accessory: A pair of long bronze toggle pins (tudittum) fastening the shawl at the chest, linked by a looping string of carnelian and lapis beads

MUST READ AT A GLANCE: The heavily fringed, coloured wool wrap draped over the left shoulder with the right shoulder bare; men also have the long chest-length curled beard and the hair bun
DO NOT DRAW: horned crowns (divine) and Hammurabi's royal brimmed cap; tiered flounced robes (in Old Babylonian art these are goddess dress, e.g. the Lama goddess); Assyrian winged bulls (lamassu), Ishtar Gate dragons and other religious motifs; Sumerian sheepskin kaunakes skirts (an earlier period); plain, unfringed Greek-style himation with short hair and a short beard (reads as Greece-ancient); white linen, broad bead collars, wigs or clean-shaven faces (read as Egypt); maces, weapons, or clay tablets held in the hand
```

Files: `outfit_m_iraq_ancient.png`, `hair_m_iraq_ancient.png`, `facialhair_m_iraq_ancient.png`, `headwear_m_iraq_ancient.png`, `accessory_m_iraq_ancient.png`, `outfit_f_iraq_ancient.png`, `hair_f_iraq_ancient.png`, `headwear_f_iraq_ancient.png`, `accessory_f_iraq_ancient.png`

### Greece: Periclean Athens

```
PAIR: greece_ancient (Periclean Athens, 430 BCE)
Place and time: Athens in the age of Pericles and Socrates, c. 430 BCE

MAN
- Outfit: Short, knee-length wool chiton in undyed cream or saffron, under a large rectangular himation (wool mantle) in a colour such as madder red, blue-grey or saffron. The himation wraps round the body and is thrown over the left shoulder, leaving the right arm and shoulder free (the chiton covers the chest). Its hem has a woven meander (Greek key) border in dark red or black. Simple strapped leather sandals or bare feet.
- Hair: Classical Athenian crop: short-to-medium thick curls brushed forward, half covering the ears
- Facial hair: Full, rounded, neatly trimmed beard with moustache (the norm for an adult Athenian citizen)
- Headwear: Petasos: a flat, wide-brimmed felt traveller's hat with a low crown, worn tilted back on the head with the brim turned up at the front so the face is fully clear, tied under the chin with a thin cord
- Accessory: Pera: a small soft leather traveller's pouch on a thin strap slung diagonally across the body from the left shoulder, over the himation

WOMAN
- Outfit: Ankle-length Ionic chiton of fine crinkled linen, with short sleeves formed by a row of small pins along the upper arms. A himation in saffron, madder red or indigo is wrapped diagonally over one shoulder, with a meander or wave-pattern band at its hem.
- Hair: Centre-parted wavy hair drawn back to a bun at the nape, with a few curls escaping at the temples
- Headwear: Sakkos: a patterned cloth snood that wraps the bun and the back of the head, leaving the forehead and face fully visible
- Accessory: Narrow woven girdle tied at the natural waist, with the chiton bloused over it (kolpos)

MUST READ AT A GLANCE: The coloured himation with a woven meander (Greek key) border, draped over one shoulder with no curved toga edge; men add a beard and the flat petasos traveller's hat, and women add the patterned sakkos snood
DO NOT DRAW: Roman toga with a curved, rounded edge or a U-shaped front fold, or a tunic with purple stripes (reads as Rome); clean-shaven adult men (reads as Roman); laurel or olive victory wreaths; Corinthian helmet from Pericles' busts, hoplite armour, Spartan red cloak (military); short chlamys cloak pinned at the shoulder with petasos (the ephebe/cadet and hunting costume); all-white marble-statue clothing (real Greek dress was coloured); 'toga party' bedsheet look; girdle high under the bust (a Hellenistic fashion, a century later); Greek national flag colours or modern symbols; priestess or temple ritual dress (sacred)
```

Files: `outfit_m_greece_ancient.png`, `hair_m_greece_ancient.png`, `facialhair_m_greece_ancient.png`, `headwear_m_greece_ancient.png`, `accessory_m_greece_ancient.png`, `outfit_f_greece_ancient.png`, `hair_f_greece_ancient.png`, `headwear_f_greece_ancient.png`, `accessory_f_greece_ancient.png`

### Italy: Republican Rome

```
PAIR: italy_ancient (Republican Rome, 50 BCE)
Place and time: Rome in the last years of the Republic, under Julius Caesar, c. 50 BCE

MAN
- Outfit: Knee-length white wool tunica with two narrow reddish-purple vertical stripes (angusticlavus, associated with the equestrian business class), under a long, ample, plain off-white wool toga. The toga is draped the late-Republican way: over the left shoulder, round the back, under the right arm and back up across the front to the left shoulder, forming a firm diagonal band across the chest. Its distinctive rounded (curved) lower edge hangs to the ankles and dips lower at the sides. The right-hand clavus stripe shows at the right side of the chest. Closed black leather calcei ankle boots with laced straps, not sandals.
- Hair: Late-Republican short crop, cut close and combed forward onto the brow (Caesar style)
- Facial hair: none (clean-shaven, the Roman norm of the period)
- Headwear: none
- Accessory: Gold signet ring (anulus aureus, the equestrian gold ring) on the left hand

WOMAN
- Outfit: A long-sleeved tunica with a sleeveless, floor-length stola over it on narrow shoulder straps (the dress of a respectable married woman). A large plain rectangular palla (mantle) with no patterned border is draped over the left shoulder and around the body. Ochre, rose, sea-green or blue wool, not purple.
- Hair: Nodus hairstyle: a raised roll of hair pushed up and pinned above the forehead, the sides waved back, the rest braided into a small bun at the nape
- Headwear: none
- Accessory: Gold earrings with pearl drops (crotalia)

MUST READ AT A GLANCE: Men: a plain white toga with a curved, rounded edge over a tunic with a narrow purple clavus stripe, worn clean-shaven with calcei boots. Women: the nodus roll of hair above the forehead, with the strapped stola.
DO NOT DRAW: deep knee-length sinus fold and chest pouch (umbo): these are Augustan (umbo from c. 10 BCE), a generation too late; laurel wreath (triumphal or imperial); toga praetexta or all-purple toga (magistrate or emperor); broad senatorial stripe (latus clavus); legionary armour, red military cloak, gladiator gear; beards on men (reads as Greek or later imperial); Greek-key or other patterned borders (keep those for Greece); sandals worn with the toga; Vestal or priestly veiling, toga drawn over the head (sacred); arm held in a toga sling (a statue pose that fights the layered body rig)
```

Files: `outfit_m_italy_ancient.png`, `hair_m_italy_ancient.png`, `accessory_m_italy_ancient.png`, `outfit_f_italy_ancient.png`, `hair_f_italy_ancient.png`, `accessory_f_italy_ancient.png`

### China: Eastern Han Luoyang

```
PAIR: china_ancient (Eastern Han Luoyang, 105 CE)
Place and time: Luoyang, the Eastern Han capital, c. 105 CE, when Cai Lun presented his improved paper to the court

MAN
- Outfit: Ankle-length, straight-hemmed wrap robe (zhiju paofu) closed with the wearer's left panel over the right, making a 'y' at the throat, with two or three layered collars (the innermost white). The sleeves are huge, bagging out below the arm and narrowing at the wrist. The ground is black, deep red or brown silk with broad contrasting patterned borders at collar, cuffs and hem, tied at the waist with a cloth sash on a small bronze belt hook. Black cloth shoes with upturned square toes peek from the hem.
- Hair: Han topknot (fa ji): all hair combed smoothly up from forehead and nape into a single knot on the crown, no fringe
- Facial hair: Thin moustache with slightly upturned tips and a short pointed chin beard
- Headwear: Jieze: a close-fitting black cloth cap with a stiff band across the forehead, a raised roof-shaped ridge on top covering the topknot, and two short upright flaps ('ears') standing at the back. No chin ties.
- Accessory: Zanbi: a slim bamboo writing brush tucked upright into the side of the cap, as Han scribes kept brushes in the hair or cap

WOMAN
- Outfit: Floor-length quju shenyi: a wrap robe whose long curved front panel spirals round the body, making layered diagonal bands on the skirt. Wide drooping sleeves narrow at the wrist, and two or three cross-collars layer at the neck, the wearer's left over right. Deep red, black or ochre silk with checked or cloud-scroll borders, cinched with a sash. The silhouette is narrow and trails at the hem.
- Hair: Chuishao ji: centre-parted hair drawn smoothly back into a low looped bun at the nape, with a short tail of hair hanging below it
- Headwear: none
- Accessory: Jade bi-disc pendant hanging from the sash on a red silk cord with a tassel

MUST READ AT A GLANCE: A floor-length cross-collar wrap robe with huge bag-shaped 'ox-dewlap' sleeves and broad dark borders; men add the roof-ridged black jieze cap
DO NOT DRAW: Emperor's mianguan crown with bead curtains, dragon robes or imperial yellow; jinxian guan ridged official hat or coloured seal-ribbons (shou), which mark rank; armour, swords, crossbows; Qing items (queue braid, skullcap, horse-hoof cuffs) or Tang/Ming items; Japanese elements: mizura side loops, magatama beads, short jacket with trousers tied at the knee; conical 'coolie' straw hat, Fu Manchu moustache, any slanted-eye caricature; robe closed with the right panel over the left (reversed); chin ties on the jieze (ties belong to the formal guan hat worn on top of it)
```

Files: `outfit_m_china_ancient.png`, `hair_m_china_ancient.png`, `facialhair_m_china_ancient.png`, `headwear_m_china_ancient.png`, `accessory_m_china_ancient.png`, `outfit_f_china_ancient.png`, `hair_f_china_ancient.png`, `accessory_f_china_ancient.png`

### Japan: Kofun Yamato

```
PAIR: japan_ancient (Kofun Yamato, 450 CE)
Place and time: Yamato (the Nara–Osaka plain) in the Kofun period, c. 450 CE, the age of the great keyhole tombs and haniwa clay figures

MAN
- Outfit: Kinu: a hip-length jacket of undyed white hemp with narrow tube sleeves, fastened at the chest with two small cloth bows and belted at the waist. Loose white hakama trousers are gathered and tied just below each knee with cords (ashi-yui), so they balloon above. Bare feet or simple straw sandals; optional simple red triangle or dot trim.
- Hair: Mizura: hair parted in the centre, with each side looped into a figure-eight bundle tied beside the ears and hanging to the jaw
- Facial hair: none
- Headwear: none
- Accessory: Necklace of green jade magatama (comma-shaped beads) strung with cylindrical kudatama beads

WOMAN
- Outfit: White hemp kinu jacket with narrow sleeves, fastened with small cloth bows and belted at the waist, over a mo: a long wrapped skirt to the ankle, sometimes pleated or striped in red and white.
- Hair: Flat 'board' chignon: hair gathered on top of the head into a long, flat bun lying front to back, tied at its middle, with a small comb at the front
- Headwear: none
- Accessory: Multi-strand necklace of green magatama and round blue glass beads

MUST READ AT A GLANCE: A short belted jacket over knee-tied trousers or a long skirt, with a big green comma-shaped magatama bead necklace; men also wear mizura hair loops beside the ears (the clearest silhouette cue), and women the flat board chignon
DO NOT DRAW: keiko armour, swords, helmets (the famous haniwa warrior); shrine-maiden ritual sash (osuhi), bronze mirrors, anything liturgical; a single oversized jewel styled as the imperial regalia; red face paint seen on haniwa (reads as war paint or caricature); later kimono with a wide obi, or Heian court robes; Chinese long robes with huge sleeves, or topknot caps
```

Files: `outfit_m_japan_ancient.png`, `hair_m_japan_ancient.png`, `accessory_m_japan_ancient.png`, `outfit_f_japan_ancient.png`, `hair_f_japan_ancient.png`, `accessory_f_japan_ancient.png`

### Britain: Iron Age Britain

```
PAIR: britain_ancient (Iron Age Britain, 20 CE)
Place and time: Camulodunum (Colchester), the trading capital of the Catuvellauni under Cunobelinus, c. 20 CE: shortly before the Roman conquest, while wine and luxury goods were arriving from Roman Gaul

MAN
- Outfit: Knee-length belted wool tunic over close-fitting trousers (bracae), both woven in bold multicoloured checks of red, mustard yellow and green. A thick, short rectangular cloak in a contrasting check is pinned at the right shoulder. Soft leather ankle shoes.
- Hair: Long swept-back mane: shoulder-length hair brushed straight back off the forehead, thick and full
- Facial hair: Long drooping moustache that hangs past the corners of the mouth; chin and cheeks clean-shaven
- Headwear: none
- Accessory: Twisted gold torc: a thick, rope-twisted open neck ring with rounded knob ends meeting at the throat

WOMAN
- Outfit: Ankle-length, long-sleeved tunic-dress of bright checked wool in red, yellow and green, cinched with a woven belt. Over it hangs a thick, plain dark-red or ochre wool mantle, fastened at the chest with a bronze brooch enamelled in red with swirling La Tene curves.
- Hair: Very long centre-parted hair in two thick braids falling to the waist
- Headwear: none
- Accessory: Twisted gold or bronze torc at the neck

MUST READ AT A GLANCE: The rope-twisted gold torc at the throat, worn with bold multicoloured checked wool
DO NOT DRAW: blue woad body paint or tattoos (skin must never carry the signal); horned or winged helmets; Roman toga, tunic-with-toga or Roman armour; swords, shields, spears, chariot-warrior gear; Scottish kilts or clan tartans (anachronistic by 1,500+ years); Suebian side-knot hairstyle or full beard (that is Germany-ancient); plain undyed or single-colour cloth with amber beads (reads as Germany-ancient); fur-loincloth 'savage barbarian' caricature; druid robes or any religious costume
```

Files: `outfit_m_britain_ancient.png`, `hair_m_britain_ancient.png`, `facialhair_m_britain_ancient.png`, `accessory_m_britain_ancient.png`, `outfit_f_britain_ancient.png`, `hair_f_britain_ancient.png`, `accessory_f_britain_ancient.png`

### Germany: Germania

```
PAIR: germany_ancient (Germania, 98 CE)
Place and time: Free Germania east of the Rhine and north of the Danube, c. 98 CE, as described in Tacitus' Germania

MAN
- Outfit: Tight-fitting, long-sleeved, knee-length wool tunic and close-fitting long trousers, as in the Thorsberg bog finds, in undyed brown and deep woad blue with a diamond-twill texture. A short, dark rectangular wool cloak with a fringed, tablet-woven border is pinned at the right shoulder. Leather wrap shoes.
- Hair: Suebian knot: hair combed back and to the side and twisted into a tight knot on the right side of the head, above the temple
- Facial hair: Full short beard
- Headwear: none
- Accessory: Bronze bow brooch (eye-fibula) pinning the cloak at the right shoulder

WOMAN
- Outfit: Loose, sleeveless, ankle-length linen tube dress (peplos type) pinned at each shoulder with a small bronze brooch, leaving the arms bare, with red-purple bands at the neck and hem as Tacitus notes. A plain undyed wool shawl drapes round the shoulders, and the waist is tied with a woven band. Draw it modestly: fuller and draped, not a fitted sheath.
- Hair: Long hair gathered into a low knot at the nape, bound with a narrow woven wool hair-band wound round it
- Headwear: none
- Accessory: Necklace of chunky orange Baltic amber and glass beads

MUST READ AT A GLANCE: For men, the Suebian side-knot hairstyle with a full beard. For women, a chunky Baltic amber necklace over a sleeveless, shoulder-pinned linen dress with red-purple trim.
DO NOT DRAW: horned or winged helmets; Viking or Wagnerian-opera stereotypes; fur trims, pelts or fur-loincloth barbarian caricature; spears, shields or any weapons; Roman armour; gold torc or bright multicoloured checks (that is Britain-ancient); fitted white strapped sheath dress or broad bead collar (reads as Egypt-ancient); snood or hairnet over the bun (too close to the Greek sakkos); Viking-age oval brooches with bead strings between them (the wrong period); Roman toga or Roman-style jewellery
```

Files: `outfit_m_germany_ancient.png`, `hair_m_germany_ancient.png`, `facialhair_m_germany_ancient.png`, `accessory_m_germany_ancient.png`, `outfit_f_germany_ancient.png`, `hair_f_germany_ancient.png`, `accessory_f_germany_ancient.png`

**Send to Claude.**

## 10. Batch 4: Medieval era, 500 to 1450 (69 images)

Folder: `Raw/medieval/`. Start a new chat for this era. For each country, paste its PAIR block, then make each file in its list.

### Egypt: Mamluk Egypt

```
PAIR: egypt_medieval (Mamluk Egypt, 1340)
Place and time: Mamluk Cairo under al-Nasir Muhammad, c. 1340 CE (the al-Khalij al-Nasiri canal was dug in 1325 and the Nile flood was read at the Roda Nilometer)

MAN
- Outfit: Ankle-length pale linen qamis under an open, front-opening farajiyya, the long outer robe of Mamluk scholars, judges and scribes. The farajiyya is deep green, brown or dark blue wool (never black), with very wide, long sleeves that fall well past the fingertips because ample sleeves marked civilian status. No embroidered armbands. Soft leather boots.
- Hair: Short-cropped, hidden under the turban
- Facial hair: Full, rounded, neatly trimmed beard with a moustache
- Headwear: Large, broad, rounded white muslin turban ('imama) wound low and full around a small hidden cap, with no cap showing above it. One loose end (the 'adhaba tail) hangs behind one shoulder.
- Accessory: Broad woven-silk girdle wound at the waist, with long knotted fringe ends

WOMAN
- Outfit: Ankle-length silk qamis in saffron, crimson or emerald with small woven motifs. It has very wide sleeves that hang well below the hands and is worn over full trousers (sirwal) gathered at the ankle. Over it, a large plain WHITE linen izar is draped from the head down to the ankles and hangs open at the front, so a strip of the bright qamis and its big sleeves shows down the middle.
- Hair: Long hair braided and hidden under the izar, with only a thin edge of hair at the brow
- Headwear: The top of the white izar drawn over the head as a soft hood that frames the fully visible face, with no face veil
- Accessory: Gold filigree collar necklace with small hanging pendants

MUST READ AT A GLANCE: Men: a big, low, round white turban with a hanging tail, worn with very long, wide sleeves. Women: a full-length white izar hooded over the head and hanging open over a brightly coloured gown.
DO NOT DRAW: Mamluk military dress: kallawta cap, heraldic blazons or roundels, metal-plaque belts, swords, bows; Abbasid tall black qalansuwa cap, black robes, tiraz script armbands, or taylasan/tarha shawl over the shoulders (read as Iraq-medieval); Blue, yellow or red izars, or any colour-coded turban: these colours were imposed on Christian, Jewish and Samaritan subjects; face-covering veils (burqu'); Ottoman tall kavuk turbans; the tall tartur/taqiyya women's caps (15th century and later, and banned in 1426/7); harem or belly-dance cliches; Crusader imagery
```

Files: `outfit_m_egypt_medieval.png`, `hair_m_egypt_medieval.png`, `facialhair_m_egypt_medieval.png`, `headwear_m_egypt_medieval.png`, `accessory_m_egypt_medieval.png`, `outfit_f_egypt_medieval.png`, `hair_f_egypt_medieval.png`, `headwear_f_egypt_medieval.png`, `accessory_f_egypt_medieval.png`

### Iraq / Mesopotamia: Abbasid Baghdad

```
PAIR: iraq_medieval (Abbasid Baghdad, 830 CE)
Place and time: Baghdad under the Abbasids, c. 830 CE (al-Khwarizmi at the House of Wisdom; translation and algebra)

MAN
- Outfit: Ankle-length pale linen qamis and sirwal under a front-opening, calf-length durra'a robe of black wool or silk. Bands of gold-embroidered decorative Arabic-style pseudo-script (tiraz) circle both upper sleeves. The sleeves are moderately wide and end at the wrist. Soft leather khuff boots.
- Hair: Hair to the nape, mostly hidden under the cap and turban
- Facial hair: Full beard, neatly trimmed and rounded, with a moustache
- Headwear: Qalansuwa tawila: a tall, stiff, smooth black cap (a cone or high cylinder about one and a half times the height of the head) with a white turban wound round its base. The tall black top rises clearly above the turban and stays upright, not soft or crumpled.
- Accessory: Taylasan: a dark shawl draped over the shoulders and hanging down the back like a thrown-back hood, the mark of a scholar

WOMAN
- Outfit: Long-sleeved, ankle-length qamis in a rich colour (rose, saffron or pistachio) with tiraz script bands at the upper arms, over full sirwal gathered at the ankle. A light mantle (rida') lies over the shoulders only, not over the head.
- Hair: Long hair mostly under the veil, with two glossy S-shaped side-curls (sudgh) combed forward onto the cheeks at the temples
- Headwear: Light, sheer khimar veil over the hair, bound at the brow by an 'isaba: a narrow jewelled and embroidered headband carrying a line of decorative pseudo-script
- Accessory: Choker of large pearls

MUST READ AT A GLANCE: Men: the tall, stiff black qalansuwa cap rising out of a white turban, with tiraz armbands. Women: the jewelled 'isaba band across the brow, with S-shaped side-curls on the cheeks.
DO NOT DRAW: 'Arabian Nights' or Aladdin cliches: curled-toe slippers, genie trousers, onion domes; harem stereotypes and face-covering veils; readable Qur'anic text on clothing (use decorative pseudo-script); keffiyeh with agal (a later style); Ottoman bulbous turbans; Mamluk very long hanging sleeves, a big round all-white turban, or a full-length white izar wrap (read as Egypt-medieval); soft, crumpled or forward-leaning cap (reads as the Japanese eboshi); Sasanian royal crowns, black battle banners, weapons
```

Files: `outfit_m_iraq_medieval.png`, `hair_m_iraq_medieval.png`, `facialhair_m_iraq_medieval.png`, `headwear_m_iraq_medieval.png`, `accessory_m_iraq_medieval.png`, `outfit_f_iraq_medieval.png`, `hair_f_iraq_medieval.png`, `headwear_f_iraq_medieval.png`, `accessory_f_iraq_medieval.png`

### Greece: Byzantine Mystras

```
PAIR: greece_medieval (Byzantine Mystras, 1430)
Place and time: Mystras, capital of the Byzantine Despotate of the Morea, in the circle of the philosopher Gemistos Plethon, c. 1430

MAN
- Outfit: Ankle-length kabbadion: a caftan-like robe closed down the centre front with a long row of small round buttons, with long narrow sleeves, in deep green, wine or blue silk with a small woven pattern. No overcoat, so the button row stays fully visible. Soft dark boots.
- Hair: Long hair, centre-parted, falling to the shoulders
- Facial hair: Long full beard with moustache (the Byzantine lay norm, noted by Italian observers in 1438)
- Headwear: Tall felt hat with a high rounded crown and a brim turned sharply up all round against it, with a light crown and a darker brim, as Pisanello sketched among the Byzantine delegation. It must not be the emperor's pointed-visor version, an official's skaranikon with a portrait, or a black clerical kalimavkion.
- Accessory: Belt with small gilt metal mounts, worn at the waist over the kabbadion

WOMAN
- Outfit: A fitted, sleeveless over-dress of patterned silk damask (for example wine red with a small floral pattern), shaped close at the bust and flaring from just below it to the floor, with braided gold trim at the neckline and armholes. Under it is a long T-shaped under-tunic of lighter patterned silk whose long, fitted sleeves show full-length on the arms.
- Hair: Hair fully covered by the headdress; only a smooth edge shows at the forehead
- Headwear: Turban-style headdress (fakiolion): a long strip of cream silk woven with bands of gold and red, wound high and rounded around the head so it covers all the hair. The neck, ears and face stay open, with no cloth wrapped under the chin.
- Accessory: Large gold crescent-shaped (lunate) earrings edged with pearls, hanging clearly below the headdress

MUST READ AT A GLANCE: Men: a tall felt hat with a high rounded crown and turned-up brim, a long beard and a button-front robe. Women: a compact, patterned, wound turban-headdress with big crescent earrings, over a fitted sleeveless damask over-dress.
DO NOT DRAW: imperial regalia: loros, crowns, purple silk, double-headed eagle; emperor's pointed-visor hat (royalty) and the skaranikon official cap bearing the emperor's portrait (regime insignia); Orthodox clerical or monastic robes, black kalimavkion, crosses or icons (sacred); Virgin Mary (Theotokos) style dark star-marked maphorion veil (sacred); plain white headcloth wrapped round the neck and chin (reads as the Britain headrail) or a full-length white wrap (reads Egypt); wide bell-sleeved overgown (reads Britain); the Byzantine 'head donut' padded ring (unattested, and reads as Italy's ghirlanda); Ottoman white turban on men (anachronistic here; reads Egypt or Iraq); Italian cappuccio hood-hat, or clean-shaven men (reads Italy); armour or weapons, including the sabre in Pisanello's sketches
```

Files: `outfit_m_greece_medieval.png`, `hair_m_greece_medieval.png`, `facialhair_m_greece_medieval.png`, `headwear_m_greece_medieval.png`, `accessory_m_greece_medieval.png`, `outfit_f_greece_medieval.png`, `hair_f_greece_medieval.png`, `headwear_f_greece_medieval.png`, `accessory_f_greece_medieval.png`

### Italy: Florentine Republic

```
PAIR: italy_medieval (Florentine Republic, 1439)
Place and time: Florence of the Medici bank, during the Council of Florence, 1439

MAN
- Outfit: Ankle-length lucco, the Florentine citizen's gown, in deep red (rosato or cremisi) wool. It is closed high at the neck with a small standing collar, falls in straight organ-pipe pleats, is fur-lined, has wide open sleeves and is belted with a narrow leather belt. A dark doublet and hose are worn beneath, with soft black shoes.
- Hair: Early-15th-century Florentine crop: short, rounded, cut above the ears
- Facial hair: none (clean-shaven, the Florentine norm)
- Headwear: Cappuccio a mazzocchio: a padded ring sitting on top of the head like a hat, with draped cloth (foggia) falling to one side and a long band (becchetto) hanging over one shoulder, in red or black. Hair shows below the ring, and it does not enclose the neck like a hood.
- Accessory: Scarsella: a leather merchant's purse hanging from the belt at the hip

WOMAN
- Outfit: Fitted gamurra gown in plain silk with long, close sleeves, under a giornea: an open-sided, sleeveless overgown of crimson or green silk velvet with the Italian pomegranate pattern, falling from the shoulders to the floor with a short train, edged with fur or brocade at the neck. No trailing sleeves.
- Hair: Fashionably plucked high forehead; hair drawn back smoothly and gathered into a coil at the back of the head, partly visible
- Headwear: Ghirlanda: a thick padded roll, like a fat wreath, covered in crimson silk and studded with pearls, set around the crown of the head. No veil, so the drawn-back hair and the high bare forehead show.
- Accessory: Gold brooch (fermaglio) with a pearl cluster, pinned at the neckline

MUST READ AT A GLANCE: Men: the cappuccio a mazzocchio (a padded ring-hat with a draped side and a long hanging becchetto) with an ankle-length red lucco. Women: the pearl-studded padded ghirlanda roll over bare, drawn-back hair and a plucked high forehead.
DO NOT DRAW: beards on men (reads Byzantine Greek); tall brimmed Byzantine hat; women's veils, wimples or wound headcloths (read Byzantine, Egypt, Britain or Germany); floor-trailing hanging sleeves (reads Egypt-medieval); a hood enclosing the head and neck with a scalloped shoulder cape (reads Germany's Gugel); cardinal's red galero hat, clerical or papal robes (sacred); laurel wreath and red-cap 'Dante' costume cliches; the later balzo headdress (1450s-80s, Ferrara and Milan); condottiere armour or weapons; slashed Renaissance sleeves (too late; reserve for early modern); Medici family crest or balls emblem
```

Files: `outfit_m_italy_medieval.png`, `hair_m_italy_medieval.png`, `headwear_m_italy_medieval.png`, `accessory_m_italy_medieval.png`, `outfit_f_italy_medieval.png`, `hair_f_italy_medieval.png`, `headwear_f_italy_medieval.png`, `accessory_f_italy_medieval.png`

### China: Northern Song Kaifeng

```
PAIR: china_medieval (Northern Song Kaifeng, 1075)
Place and time: Kaifeng, the Northern Song capital, c. 1075. This is the era of Shen Kuo, scholar-official and envoy to the Liao, and of the magnetic compass and movable type.

MAN
- Outfit: Ankle-length round-collared robe (yuanling pao) fastened at the right shoulder, with a white inner collar edge at the throat, moderately wide sleeves and a horizontal seam band (lan) across the knees. Plain, unpatterned silk in muted green or blue-grey, the colours of middle-ranking civil officials, not the purple or vermilion of high ranks. Black cloth boots; a long, straight, sober silhouette.
- Hair: Topknot on the crown, fully hidden under the cap; neat hairline
- Facial hair: Long thin moustache and a tapered chin beard, the groomed Song scholar look
- Headwear: Zhijiao futou: a black lacquered-gauze cap with two stiff, straight, flat wings sticking out horizontally from the back. Keep each wing about shoulder-width so the silhouette reads as a 'T'.
- Accessory: Black leather belt worn loose at the hips, set with plain square plaques of black horn or silver

WOMAN
- Outfit: Beizi: a long, straight, open-front coat to mid-calf. Its parallel front edges do not overlap and are edged with a narrow contrasting band; it has high side slits and narrow sleeves. It is worn over a moxiong chest wrap and a long pleated skirt, in pale, understated Song colours: celadon green, ivory, pale pink or light blue.
- Hair: Gaoji: a tall bun piled high on the crown in one or two upright loops, with a few small flower pins
- Headwear: Guanshu: a large arched crescent comb of ivory or lacquer, set upright across the front of the high bun
- Accessory: Pearl drop earrings

MUST READ AT A GLANCE: Men: the black futou cap with long, stiff, horizontal wings. Women: the tall bun crowned by a big upright crescent comb.
DO NOT DRAW: Ming or Qing items: queue, skullcap, standing collars with metal buttons; official fish pouch (yudai), purple or vermilion high-rank robes, and jade rank belts; emperor's yellow robe, dragons; armour or weapons; Mongol/Yuan-style dress; Japanese eboshi or hitatare; coolie hat or any facial caricature; collar closed right-over-left (zuoren); futou wings so long they break the sprite frame
```

Files: `outfit_m_china_medieval.png`, `hair_m_china_medieval.png`, `facialhair_m_china_medieval.png`, `headwear_m_china_medieval.png`, `accessory_m_china_medieval.png`, `outfit_f_china_medieval.png`, `hair_f_china_medieval.png`, `headwear_f_china_medieval.png`, `accessory_f_china_medieval.png`

### Japan: Muromachi Kyoto

```
PAIR: japan_medieval (Muromachi Kyoto, 1400)
Place and time: Kyoto under the Ashikaga shogunate, c. 1400: Kitayama culture, Noh theatre, and Japanese folding fans exported to Ming China

MAN
- Outfit: Hitatare of hemp: a cross-collared jacket, closed left-over-right, with very wide square sleeves whose cuffs are gathered by a threaded cord. It is tucked into matching ankle-length pleated hakama, with small decorative cord tufts (kikutoji) on the seams and chest ties. Persimmon brown, indigo or olive, with no family crests.
- Hair: Motodori topknot at the crown under a full head of hair (no shaved pate), hidden under the cap
- Facial hair: Short moustache and a small chin tuft
- Headwear: Momi-eboshi: a tall, soft cap of black, lightly lacquered gauze or paper, crumpled and leaning slightly forward
- Accessory: Folding fan (sensu) with black lacquered ribs, closed and tucked into the waist ties at the front

WOMAN
- Outfit: Tsubo-shozoku travel dress: an ankle-length patterned kosode, closed left-over-right, under an outer robe hitched up at the hips so its hem ends at mid-calf, tied with a narrow cloth sash. Muted tie-dyed or small-motif patterns in indigo, rust and cream.
- Hair: Long centre-parted hair worn down, tied once loosely at the nape and falling to the waist
- Headwear: Ichime-gasa: a wide-brimmed lacquered sedge hat with a tall knob crown, worn WITHOUT its veil so the face is fully visible
- Accessory: Small tie-dyed drawstring purse (kinchaku) hanging from the sash

MUST READ AT A GLANCE: Men: a tall, soft, crumpled black eboshi cap with a wide-sleeved hitatare and hakama. Women: the wide, knob-crowned ichime-gasa travel hat.
DO NOT DRAW: armour, katana, samurai helmets, ninja imagery; Heian junihitoe or court robes (aristocratic and the wrong period); Buddhist monk kesa or yamabushi robes (liturgical); amulet pendants (kake-mamori), which are religious; family crests (kamon) on the suo, which can read as a warrior retainer; wide Edo obi or geisha look (anachronistic); Chinese futou with horizontal wings; a stiff, smooth, upright cap with a turban at its base (reads Iraq's qalansuwa); collar closed right-over-left (the burial style); the hanging veil (mushi-no-tareginu) that hides the face
```

Files: `outfit_m_japan_medieval.png`, `hair_m_japan_medieval.png`, `facialhair_m_japan_medieval.png`, `headwear_m_japan_medieval.png`, `accessory_m_japan_medieval.png`, `outfit_f_japan_medieval.png`, `hair_f_japan_medieval.png`, `headwear_f_japan_medieval.png`, `accessory_f_japan_medieval.png`

### Britain: Anglo-Saxon England

```
PAIR: britain_medieval (Anglo-Saxon England, 1050)
Place and time: London under Edward the Confessor, c. 1050: a North Sea port trading wool and silver, where laws of the time mention merchants from Flanders, Normandy and the German Empire

MAN
- Outfit: Knee-length wool tunic (cyrtel) in madder red or woad blue, loosely bloused over a belt, with contrasting embroidered or tablet-woven bands at the neckline, cuffs and hem. Loose trousers are wrapped from ankle to knee with linen leg bindings (winingas). A plain rectangular wool cloak is draped over the left shoulder and pinned at the right.
- Hair: Neck-length straight hair, centre-parted and cut evenly at the collar
- Facial hair: Long full moustache with a clean-shaven chin (the English look on the Bayeux Tapestry)
- Headwear: none
- Accessory: Round silver disc brooch with interlace and niello decoration, pinning the cloak at the right shoulder

WOMAN
- Outfit: Ankle-length undergown with tight long sleeves, under a slightly shorter overgown with wide bell sleeves, in soft red, blue or green wool with embroidered cuff bands. A plain mantle hangs from the shoulders.
- Hair: Long hair coiled low at the nape, hidden under the headrail
- Headwear: Headrail: a long rectangular veil of COLOURED wool or linen (blue, brown-red or ochre, contrasting with the gown) laid over the head, wrapped once around the throat and draped down over the shoulders and back. It frames the face but never covers it.
- Accessory: Small round gilt disc brooch closing the mantle at the throat

MUST READ AT A GLANCE: Men: bareheaded, in the only knee-length tunic of the era, with leg bindings, a cloak pinned at the right shoulder by a round silver disc brooch, and the long English moustache. Women: a coloured headrail veil wrapped over the head and round the throat, above a bell-sleeved overgown.
DO NOT DRAW: horned or winged helmets and Viking-raider stereotypes; chainmail, Norman kite shields, any armour or weapons; crowns or royal regalia; monks' habits, nuns' wimples or any liturgical vestment; white headrail (reads Egypt's white izar and Germany's Kruseler at thumbnail size); a compact wound turban-headdress (reads Greece); hood with a long tail (Gugel) or long pointed shoes (Germany-medieval); frilled Kruseler veil (Germany-medieval); tall conical hennin or later Gothic headdresses; Robin Hood-style feathered cap and Lincoln green
```

Files: `outfit_m_britain_medieval.png`, `hair_m_britain_medieval.png`, `facialhair_m_britain_medieval.png`, `accessory_m_britain_medieval.png`, `outfit_f_britain_medieval.png`, `hair_f_britain_medieval.png`, `headwear_f_britain_medieval.png`, `accessory_f_britain_medieval.png`

### Germany: Holy Roman Empire (Rhineland)

```
PAIR: germany_medieval (Holy Roman Empire (Rhineland), 1440)
Place and time: The Rhine cities of Strasbourg and Mainz, c. 1440, when Johannes Gutenberg was running his first experiments with movable metal type

MAN
- Outfit: Calf-length belted gown (Tappert) in deep blue or green wool with regular organ-pipe pleats, a high standing collar and wide, fur-trimmed sleeves, worn over tight hose. Moderately long pointed shoes (Schnabelschuhe).
- Hair: Chin-length straight hair with a full fringe, mostly inside the hood
- Facial hair: none (clean-shaven, the 15th-century norm)
- Headwear: Gugel: a close hood enclosing the head and neck, with the face opening fully clear. It has a short shoulder cape cut into scalloped leaf-shaped points (dags) and ends in a long tail (Zipfel) that hangs down the back.
- Accessory: Low-slung leather belt with a hanging leather purse (Guerteltasche)

WOMAN
- Outfit: Ankle-length, high-waisted wool gown in deep blue or green with long sleeves fitted tight to the wrist, belted just under the bust with a wide girdle, the skirt trailing on the ground. A dark mantle is held at the chest.
- Hair: Hair plaited and pinned up entirely beneath the headdress
- Headwear: Kruseler: a white linen veil edged with several rows of tightly ruffled frills that frame the forehead and cheeks like a halo, with no chin band
- Accessory: Round gold brooch-clasp (Fuerspan / Heftel) at the neckline

MUST READ AT A GLANCE: Men: the Gugel hood with its scalloped shoulder cape and long tail, plus pointed Schnabelschuhe. Women: the Kruseler veil with its many-layered frilled edge.
DO NOT DRAW: armour, crowns or noble regalia; clerical or monastic robes; the pointed 'Judenhut' or any discriminatory marker or badge; Anglo-Saxon disc-brooch cloak, long moustache or headrail veil (Britain-medieval); tall conical hennin (Burgundian); Italian padded ring-hat (mazzocchio) or the women's ghirlanda roll (Italy-medieval); an ankle-length red citizen's gown (reads as the Florentine lucco); Crusader tabards or crosses
```

Files: `outfit_m_germany_medieval.png`, `hair_m_germany_medieval.png`, `headwear_m_germany_medieval.png`, `accessory_m_germany_medieval.png`, `outfit_f_germany_medieval.png`, `hair_f_germany_medieval.png`, `headwear_f_germany_medieval.png`, `accessory_f_germany_medieval.png`

**Send to Claude.**

## 11. Batch 5: Early modern era, 1450 to 1750 (69 images)

Folder: `Raw/earlymodern/`. Start a new chat for this era. For each country, paste its PAIR block, then make each file in its list.

### Egypt: Ottoman Egypt

```
PAIR: egypt_earlymodern (Ottoman Egypt, 1700)
Place and time: Ottoman Cairo, c. 1700 (Cairo as hub of the Red Sea coffee trade; the yearly cutting of the Khalij canal dam at the Nile flood)

MAN
- Outfit: Ankle-length quftan (kaftan) of saffron or rose silk with small woven stripes or motifs, crossed over the chest. Over it a long, wide-sleeved gibba coat of plain dark broadcloth (forest green or deep blue), open at the front. Wide trousers and pointed red leather slippers.
- Hair: Short-cropped or shaved, hidden under the turban
- Facial hair: Full beard, trimmed short and rounded, with a moustache
- Headwear: Crisp white muslin turban wound in smooth, rounded folds around a red felt cap, with a disc of the red crown left showing on top. The turban cloth is pure white.
- Accessory: Patterned Kashmir or Indian wool shawl wound as a broad sash (hizam) at the waist

WOMAN
- Outfit: Floor-length entari robe of brocaded silk (for example gold on deep rose). Its long sleeves are slit from the elbow to hang open. It is worn over a sheer gauze chemise and wide shintiyan trousers and belted at the hips with a jewelled belt.
- Hair: Long hair in many thin plaits hanging down the back
- Headwear: Tartur: a tall headdress that is wider at the top than at the base, wrapped in black or brown cloth woven with gold stripes. A light gauze veil hangs down the back from it, and the face is fully uncovered.
- Accessory: Long gold earrings with coin drops

MUST READ AT A GLANCE: A pure-white muslin turban wound around a red cap, with the red crown showing (women: the tall, gold-striped tartur headdress)
DO NOT DRAW: Istanbul court kavuks and giant ceremonial turbans; janissary bork caps or any Ottoman military dress; Mamluk-bey armour and weapons; fez worn alone without a turban (a 19th-century look); coloured or checked turban cloth, striped camel-hair 'aba cloaks, keffiyeh/agal (read as Iraq); a small red cap wrapped in a patterned kerchief for the woman (reads as Greece-earlymodern); face veils that hide the face; odalisque or harem imagery; Mamluk floor-length sleeves (read as Egypt-medieval)
```

Files: `outfit_m_egypt_earlymodern.png`, `hair_m_egypt_earlymodern.png`, `facialhair_m_egypt_earlymodern.png`, `headwear_m_egypt_earlymodern.png`, `accessory_m_egypt_earlymodern.png`, `outfit_f_egypt_earlymodern.png`, `hair_f_egypt_earlymodern.png`, `headwear_f_egypt_earlymodern.png`, `accessory_f_egypt_earlymodern.png`

### Iraq / Mesopotamia: Ottoman Baghdad

```
PAIR: iraq_earlymodern (Ottoman Baghdad, 1720)
Place and time: Baghdad under the governor Hasan Pasha (1704-1723), whose household grew into the Mamluk regime, c. 1720 (notaries recording contracts for the qadi court registers)

MAN
- Outfit: Ankle-length zaboun robe of striped cotton-silk (cream with narrow crimson stripes), wrapped across the chest and belted. Over it is an 'aba: a wide, square-shouldered cloak of camel-hair wool with arm slits, woven in broad vertical stripes of dark brown and cream, falling open at the front to mid-calf.
- Hair: Short-cropped, hidden under the turban
- Facial hair: Full beard with a moustache
- Headwear: Loosely wound turban of clearly coloured, patterned cloth (for example cream with fine red-brown checks, or dark indigo with small motifs), with a fringed end tucked in at the side. It is never plain white and never shows a red crown.
- Accessory: Brass qalamdan: a long pen case with an inkwell at one end, tucked upright into the belt

WOMAN
- Outfit: Ankle-length, long-sleeved dress of striped silk-cotton (indigo and rose) held by a silver-link belt, over trousers. A dark, sleeveless 'aba cloak is worn open over the shoulders.
- Hair: Long hair in two braids, hidden beneath the head-shawl
- Headwear: Black crepe head-shawl (futa) wrapped over the hair and round the neck, bound at the brow by a narrow black 'asaba band. The face is uncovered.
- Accessory: Gold qilada necklace of small hanging coins and teardrop pendants

MUST READ AT A GLANCE: The square-shouldered camel-hair 'aba cloak with broad brown-and-cream vertical stripes
DO NOT DRAW: Cairo's pure-white turban over a red cap with a kaftan and gibba (reads as Egypt-earlymodern); fez or tarboush; the Safavid Qizilbash red-staved taj (a sectarian and military emblem); janissary or other Ottoman military dress; face veils that hide the face; khanjar daggers or any weapons; Persian royal court dress; crescent-and-star motifs in jewellery (can read as a later Ottoman state emblem)
```

Files: `outfit_m_iraq_earlymodern.png`, `hair_m_iraq_earlymodern.png`, `facialhair_m_iraq_earlymodern.png`, `headwear_m_iraq_earlymodern.png`, `accessory_m_iraq_earlymodern.png`, `outfit_f_iraq_earlymodern.png`, `hair_f_iraq_earlymodern.png`, `headwear_f_iraq_earlymodern.png`, `accessory_f_iraq_earlymodern.png`

### Greece: Ottoman Ioannina

```
PAIR: greece_earlymodern (Ottoman Ioannina, 1700)
Place and time: Greek Orthodox merchant community of Ioannina (Epirus) under Ottoman rule, c. 1700

MAN
- Outfit: Ankle-length anteri (caftan) of striped silk-cotton (alaca) wrapped across the chest. Over it a long dark-blue or black wool overcoat (kavadi/gouna) with loose sleeves, lined and edged with fox or lamb fur at the collar and all down the front. Black leather shoes, the colour prescribed for Christians.
- Hair: Cropped short, mostly hidden under the cap
- Facial hair: Full beard with a moustache
- Headwear: Tall black lambskin kalpak: a brimless, clearly fur-textured cap with a softly rounded top, worn instead of a turban as the typical headgear of Greek Christian merchants
- Accessory: Wide striped silk-cotton sash (zonari) wound several times around the waist

WOMAN
- Outfit: Long open-fronted anteri of striped silk (alatzas) over a white chemise with wide sleeves and a full skirt. Over it a short, sleeveless, dark velvet waistcoat (zipouni) embroidered with gold cord.
- Hair: Long hair plaited into two braids hanging down the back
- Headwear: Small red cap wrapped with a patterned silk headscarf (tsemberi) tied at the back. The braids and face stay visible.
- Accessory: Pafti: a large, ornate double-plate silver-filigree belt buckle at the waist (Ioannina silverwork)

MUST READ AT A GLANCE: Tall, rounded, black lambskin kalpak worn with a long, dark, fur-edged coat (women: the large silver pafti buckle)
DO NOT DRAW: white turban or green garments (Muslim-coded and forbidden to Christians; also reads as Ottoman Egypt or Iraq); red kalpak (forbidden to Christians by a 1662 regulation) and sable fur (restricted for non-Muslims); mitre-shaped or two-pointed kalpaks (a mitre shape reads as clergy); a square, stiff-gauze tall cap or a pale wide-sleeved robe (reads as Ming China); fustanella kilt or tasselled fez (19th century; reserve for industrial); Janissary or Ottoman military uniforms, yataghan or pistols in the sash; Orthodox clergy robes, priest's hat, crosses (sacred); Venetian or Italian Renaissance dress (would read as Venetian Crete or Italy); sultan or court dress (royalty)
```

Files: `outfit_m_greece_earlymodern.png`, `hair_m_greece_earlymodern.png`, `facialhair_m_greece_earlymodern.png`, `headwear_m_greece_earlymodern.png`, `accessory_m_greece_earlymodern.png`, `outfit_f_greece_earlymodern.png`, `hair_f_greece_earlymodern.png`, `headwear_f_greece_earlymodern.png`, `accessory_f_greece_earlymodern.png`

### Italy: Sforza Milan

```
PAIR: italy_earlymodern (Sforza Milan, 1495)
Place and time: Milan under Ludovico Sforza, while Leonardo da Vinci painted the Last Supper and designed machines, c. 1495

MAN
- Outfit: Knee-length rose-pink wool pitocco, a short, belted overgown with a full pleated skirt, worn when long gowns were the norm. At the neck the doublet collar shows a finely gathered white linen shirt. Close-fitting dark hose and soft round-toed shoes.
- Hair: Zazzera: straight, chin-to-shoulder-length bob with a blunt fringe across the brow
- Facial hair: none (clean-shaven, the Italian norm c. 1495)
- Headwear: Small, soft, round red berretta cap worn straight on the head, with no brim and no feather
- Accessory: Small leather-bound notebook hanging from the belt on a cord

WOMAN
- Outfit: Square-necked gown (gamurra) in deep blue or crimson. Its contrasting sleeves are laced on at the shoulder and elbow, so puffs of the white camicia show through the gaps. A sbernia mantle is draped over one shoulder.
- Hair: Centre-parted, smooth and flat over the ears, with the rest bound into one very long ribbon-wrapped braid down the back (coazzone) and a sheer little cap (trinzale) at the back. No hat.
- Headwear: Lenza: a fine dark cord worn straight across the forehead, with a small jewel at the centre of the brow
- Accessory: Long necklace of dark jet beads looped twice around the neck

MUST READ AT A GLANCE: Lenza: a fine cord with a small brow jewel worn straight across the forehead over smooth, hatless hair (men: a small soft red berretta over a shoulder-length bob, clean-shaven, with a knee-length pink pitocco)
DO NOT DRAW: Leonardo's old long white 'wizard' beard (a later image; also blurs the Greek beard tell); any beard on the man, a wide flat tilted Barett, a fur-collared Schaube or gold chains (read as Germany-earlymodern); any hat on the woman (the German woman wears the Barett); Galileo-era black Spanish dress with ruff or falling collar (reads northern Europe); Tudor-style flat cap with brim and feather (reads Britain); a cloak slung over one shoulder on the man (duplicates Britain); Landsknecht slashing and puffing, armour, weapons; Venetian carnival masks; clerical or cardinal robes (sacred); Sforza or ducal regalia (royalty)
```

Files: `outfit_m_italy_earlymodern.png`, `hair_m_italy_earlymodern.png`, `headwear_m_italy_earlymodern.png`, `accessory_m_italy_earlymodern.png`, `outfit_f_italy_earlymodern.png`, `hair_f_italy_earlymodern.png`, `headwear_f_italy_earlymodern.png`, `accessory_f_italy_earlymodern.png`

### China: Late Ming Suzhou

```
PAIR: china_earlymodern (Late Ming Suzhou, 1635)
Place and time: Suzhou and Jiangnan in the late Ming, c. 1635, during the printing and publishing boom that produced Song Yingxing's illustrated technology encyclopedia Tiangong Kaiwu (1637)

MAN
- Outfit: Daopao: ankle-length cross-collared robe closed left panel over right, with very wide sleeves and a broad white protective strip (huling) along the collar edge. Plain pale blue or light grey silk or cotton; black cloth shoes. A lay scholar's robe, not a priest's vestment.
- Hair: Topknot under the cap, with a black horsehair net headband (wangjin) visible across the forehead below the cap
- Facial hair: Neatly trimmed moustache and a short-to-medium, neat chin beard, as in Ming gentry portraits
- Headwear: Sifang pingding jin: a tall, square, box-like, flat-topped cap of stiff black gauze with visible corners, the Ming scholar's and commoner's 'four-cornered' cap
- Accessory: Sitao: a blue or black silk-cord sash tied at the waist, with long tasselled ends hanging to the knee

WOMAN
- Outfit: Long ao jacket reaching the knee, with a standing collar and a centre-front opening fastened at the collar by gold interlocking zimu buttons. Its pipa-shaped sleeves narrow at the cuff. It is worn over a mamianqun, a pleated 'horse-face' skirt with flat front and back panels. Pale blue or apricot jacket; the skirt is plain white or very pale, the Chongzhen-era fashion, with a narrow woven band near the hem.
- Hair: Rounded bun set high and slightly flattened on the crown, held with a few gold hairpins
- Headwear: Baotou (tougu): a broad black satin band worn low across the brow, with a small jade or pearl ornament at the centre
- Accessory: Gold gourd-shaped (hulu) earrings with small pearls

MUST READ AT A GLANCE: Tall, square, black gauze scholar's cap (women: a knee-length jacket with a standing collar and gold buttons over a pleated horse-face skirt)
DO NOT DRAW: Qing queue, skullcap or horse-hoof cuffs (wrong dynasty); Mandarin rank squares (buzi), python or dragon robes, the official winged wusha cap; Daoist priest vestments. Despite its name the daopao here is a lay robe, so no bagua, taiji or cranes.; long drooping 'Fu Manchu' moustaches or any caricatured features; white robes for the man (read as mourning); rounded fur-textured caps or dark fur-edged coats (read as Greece-earlymodern); jinbu waist pendants hanging to the knee (too close to Japan's tasselled cord belt); Japanese chonmage topknot or kamishimo; Coolie hat; Collar closed right-over-left; Qipao (a 20th-century garment)
```

Files: `outfit_m_china_earlymodern.png`, `hair_m_china_earlymodern.png`, `facialhair_m_china_earlymodern.png`, `headwear_m_china_earlymodern.png`, `accessory_m_china_earlymodern.png`, `outfit_f_china_earlymodern.png`, `hair_f_china_earlymodern.png`, `headwear_f_china_earlymodern.png`, `accessory_f_china_earlymodern.png`

### Japan: Tokugawa Edo

```
PAIR: japan_earlymodern (Tokugawa Edo, 1610)
Place and time: Edo and Sunpu at the founding of the Tokugawa shogunate, c. 1610, in Tokugawa Ieyasu's time. He received a Spanish mechanical clock in 1611.

MAN
- Outfit: Kataginu and hakama over a kosode. The kataginu is a sleeveless over-vest with straight, squared shoulders that extend a little past the wearer's own. They lie flat, not yet the stiff, flaring wings of later Edo. The matching pleated hakama are in indigo, grey or brown hemp. Under them is a kosode with small sleeve openings and a fine repeating pattern, collar crossing left over right. No crests.
- Hair: Chasen-mage: shaved pate (sakayaki), with the remaining hair gathered at the crown into a short upright topknot bound with white cord like a tea whisk
- Facial hair: Thin moustache and a small chin beard
- Headwear: none
- Accessory: Inro: a small stacked lacquered case hanging from the sash on a silk cord, held by a carved netsuke toggle

WOMAN
- Outfit: Keicho-style kosode: an ankle-length straight robe with small wrist openings and rounded sleeve bottoms. Dense small motifs in divided zones (tie-dye, embroidery, gold leaf) sit on a dark ground of black, deep red or brown. Collar left over right; slim silhouette with no wide obi.
- Hair: Long centre-parted hair drawn to the nape and looped once into a soft tamamusubi fold, tied with white paper cord, with the ends hanging down the back
- Headwear: none
- Accessory: Nagoya-obi: a braided silk cord belt wound several times around the hips and tied in front, with long tassels hanging to the knee

MUST READ AT A GLANCE: Sleeveless kataginu vest over a kosode and pleated hakama, worn bareheaded with the shaved-pate tea-whisk topknot (women: the tasselled Nagoya-obi cord belt on a dark, densely patterned kosode)
DO NOT DRAW: stiff, whalebone-stiffened winged kataginu shoulders (a mid-Edo, Genroku-era development); Katana, wakizashi or any sword (samurai wore two; omit them); Armour or kabuto helmets; Tokugawa hollyhock (aoi) crest or any clan crest; Wide obi with a large back bow, or geisha/oiran white makeup and hairpins (later Edo); Chinese square cap or cross-collar daopao with huge sleeves; Collar closed right-over-left; Late-Edo folded-forward chonmage (use the upright chasen-mage)
```

Files: `outfit_m_japan_earlymodern.png`, `hair_m_japan_earlymodern.png`, `facialhair_m_japan_earlymodern.png`, `accessory_m_japan_earlymodern.png`, `outfit_f_japan_earlymodern.png`, `hair_f_japan_earlymodern.png`, `accessory_f_japan_earlymodern.png`

### Britain: Elizabethan England

```
PAIR: britain_earlymodern (Elizabethan England, 1600)
Place and time: The Royal Exchange in London, c. 1600, the year the East India Company received its charter

MAN
- Outfit: Close-fitting black doublet with small shoulder wings, a short tabbed skirt and a long row of small buttons. It is worn with padded knee breeches (Venetians), dark stockings and flat shoes, and a short black cloak slung over one shoulder. At the neck is a large starched white cartwheel ruff.
- Hair: Short hair brushed up and back from the forehead
- Facial hair: Pointed 'pick-a-devant' beard with an upturned moustache
- Headwear: Tall-crowned black felt capotain hat with a narrow brim and a plain hatband
- Accessory: A single pearl-drop earring in one ear

WOMAN
- Outfit: Black or russet gown with a stiff, long, pointed bodice and a full skirt over a modest hip roll, not a court wheel farthingale. A wide starched white lace-edged ruff sits at the neck, with matching white linen cuffs.
- Hair: Hair drawn up and back under a close white linen coif, with only a smooth front edge showing
- Headwear: Tall-crowned black felt hat with a narrow brim, worn over the white coif (the London citizen's wife's hat)
- Accessory: Silver pomander ball hanging on a chain from the waist girdle

MUST READ AT A GLANCE: A large starched white cartwheel ruff under a tall-crowned, narrow-brimmed black capotain hat
DO NOT DRAW: royal or court regalia: crowns, jewel-encrusted Elizabeth I gowns, huge wheel farthingales; Henry VIII-style flat cap and wide fur-collared gown (reads as Germany c. 1525); slashed-and-puffed Landsknecht sleeves, a wide flat tilted Barett or heavy gold chains (Germany earlymodern); rapiers, swords, armour; buckle-hat 'Puritan' caricature (buckled hats are a later myth); Tudor rose badges or royal livery
```

Files: `outfit_m_britain_earlymodern.png`, `hair_m_britain_earlymodern.png`, `facialhair_m_britain_earlymodern.png`, `headwear_m_britain_earlymodern.png`, `accessory_m_britain_earlymodern.png`, `outfit_f_britain_earlymodern.png`, `hair_f_britain_earlymodern.png`, `headwear_f_britain_earlymodern.png`, `accessory_f_britain_earlymodern.png`

### Germany: Renaissance Nuremberg

```
PAIR: germany_earlymodern (Renaissance Nuremberg, 1525)
Place and time: Nuremberg c. 1525: Duerer's city and Europe's hub of printing and precision instruments (Henlein's early portable spring-driven clocks, Behaim's globe), the year the city council adopted the Reformation as Luther's printed pamphlets spread

MAN
- Outfit: Knee-length Schaube: a wide, open, pleated overcoat with a broad fur or velvet shawl collar, worn over a doublet whose sleeves are slashed and puffed to show a contrasting lining. Pleated white shirt with an embroidered stand-up neckband. Hose, and wide square-toed 'cow-mouth' shoes (Kuhmaulschuhe).
- Hair: Shortened Kolbe of the 1520s: hair cut short, level with the ear-lobes, with a short straight fringe
- Facial hair: Full, rounded beard with a moustache, trimmed fairly short (the new German fashion of the 1520s)
- Headwear: Wide, flat black velvet Barett with a slashed brim, worn tilted to one side
- Accessory: Heavy gold link chain draped across the shoulders and chest

WOMAN
- Outfit: Red or black wool gown with a laced bodice, a full pleated skirt and sleeves banded with horizontal puffs and slashes. It is worn over a high-necked pleated white linen shirt with a gold-embroidered collar band, and a short black velvet shoulder cape (Goller) covers the shoulders.
- Hair: Hair plaited and gathered into a gold-thread hairnet cap (Haarhaube) at the back of the head
- Headwear: Wide, flat Barett tilted to one side over the hairnet, trimmed with a small white feather
- Accessory: Several layered gold chains across the chest

MUST READ AT A GLANCE: The wide, flat, slashed Barett worn tilted (men: with a broad fur-collared Schaube and a full beard; women: with the black Goller shoulder cape)
DO NOT DRAW: a clean-shaven man with a shoulder-length bob under a small round cap, or a pink knee-length gown (reads as Italy-earlymodern); Landsknecht mercenary gear: armour, halberds, two-handed swords, exaggerated codpieces, huge feather plumes; Luther's black preaching gown or any clerical vestment; starched cartwheel ruffs or tall capotain hats (Britain earlymodern); princely or court regalia; printing tools, globes or other held props (the theme lives in the notes, not in the hands)
```

Files: `outfit_m_germany_earlymodern.png`, `hair_m_germany_earlymodern.png`, `facialhair_m_germany_earlymodern.png`, `headwear_m_germany_earlymodern.png`, `accessory_m_germany_earlymodern.png`, `outfit_f_germany_earlymodern.png`, `hair_f_germany_earlymodern.png`, `headwear_f_germany_earlymodern.png`, `accessory_f_germany_earlymodern.png`

**Send to Claude.**

## 12. Batch 6: Industrial era, 1750 to 1900 (59 images; Britain was done in the pilot)

Folder: `Raw/industrial/`. Start a new chat for this era. For each country, paste its PAIR block, then make each file in its list.

### Egypt: Khedivate of Egypt

```
PAIR: egypt_industrial (Khedivate of Egypt, 1869)
Place and time: Cairo in 1869, the year Khedive Isma'il opened the Suez Canal, during the building of the new irrigation canal network

MAN
- Outfit: Stambouline (istanbuli): a black or dark-navy knee-length frock coat, single-breasted and buttoned right up to a small standing collar, with matching narrow trousers and polished black shoes. A white shirt collar just shows at the neck.
- Hair: Short and neatly trimmed, mostly hidden under the tarboush
- Facial hair: Full, neatly groomed moustache with a clean-shaven chin
- Headwear: Tarboush: a stiff, upright, flat-topped cap of deep-crimson felt, shaped like a truncated cone and set straight and level on the head. A short black silk tassel falls from the centre of the crown toward the back. It is never floppy or tilted.
- Accessory: Gold watch-chain hanging from a coat button to the breast pocket

WOMAN
- Outfit: Yelek: an ankle-length, fitted, long-sleeved robe of striped silk (cream with rose and gold stripes), buttoned from bust to hips and slit open at the sides below. It is worn over a sheer chemise and full shintiyan trousers gathered at the ankle, and girdled at the hips with a folded Kashmir shawl.
- Hair: Centre-parted, with long thin plaits falling down the back under the veil, each plait ending in small gold safa ornaments
- Headwear: Tarha: a long white muslin head veil with its ends embroidered in coloured silk and gold thread. It rests on the head and hangs down the back almost to the ground, and the face is fully uncovered. Beneath it is a small cap wound with a printed kerchief (faroodiyeh), which shows only as a coloured band across the top of the forehead.
- Accessory: Kirdan: a gold choker-collar necklace hung with a row of small crescent pendants

MUST READ AT A GLANCE: Men: the stiff, upright crimson tarboush with a black tassel, over a black stambouline buttoned to the collar. Women: the long white embroidered tarha veil trailing down the back over a striped yelek robe.
DO NOT DRAW: Ottoman or Egyptian military uniforms, epaulettes, medals, the Khedive's orders and sashes; European top hats; a soft, drooping, tilted fez with a very long tassel (reads as Greece-industrial); a small tilted red cap with a gold tassel on a woman (reads as Greece-industrial); keffiyeh with 'igal, 'aba cloaks, or a black 'abaya over the head (read as Iraq-industrial); the black habara wrap with burqu' face veil (hides the face); pyramid or sphinx props; Orientalist harem imagery
```

Files: `outfit_m_egypt_industrial.png`, `hair_m_egypt_industrial.png`, `facialhair_m_egypt_industrial.png`, `headwear_m_egypt_industrial.png`, `accessory_m_egypt_industrial.png`, `outfit_f_egypt_industrial.png`, `hair_f_egypt_industrial.png`, `headwear_f_egypt_industrial.png`, `accessory_f_egypt_industrial.png`

### Iraq / Mesopotamia: Ottoman Iraq (Baghdad Vilayet)

```
PAIR: iraq_industrial (Ottoman Iraq (Baghdad Vilayet), 1870)
Place and time: Baghdad under the reforming governor Midhat Pasha, c. 1870 (Iraq's first printing press and first newspaper, al-Zawra, 1869)

MAN
- Outfit: Ankle-length zaboun robe of white cotton with thin blue-black stripes, crossed over at the chest and girdled with a folded patterned shawl. Over it is an 'aba: a wide, sleeveless, open-fronted cloak of fine wool in dark camel-brown, with a band of gold-thread embroidery round the neck and down the front edges.
- Hair: Hidden under the headcloth
- Facial hair: Heavy moustache with a short trimmed beard
- Headwear: Chfiyya (Iraqi keffiyeh): a square headcloth of plain white cotton, folded into a triangle and draped over the head to the shoulders, held in place by a doubled black camel-hair 'igal cord ring. The face is fully visible.
- Accessory: Dawat: a slim brass scribe's pen-case with an attached inkwell, tucked upright into the front of the shawl girdle (the mark of a literate merchant or clerk)

WOMAN
- Outfit: Ankle-length, long-sleeved dress of brocaded silk (gold on crimson or green) with a silver-plaque belt. Over everything is a black silk 'abaya, draped from the crown of the head to the ankles and falling open at the front to show the dress.
- Hair: Long hair in braids, hidden beneath the kerchief
- Headwear: Black silk head-kerchief (futa) tied over the hair, with the top edge of the black 'abaya resting on the crown of the head over it. The face is uncovered.
- Accessory: Heavy gold filigree bangles stacked on both wrists

MUST READ AT A GLANCE: Men: the white chfiyya headcloth held by a black 'igal cord, worn with a gold-trimmed camel 'aba. Women: the black silk 'abaya worn from the crown of the head to the ankles, open over a brocade dress.
DO NOT DRAW: a stiff crimson fez or tarboush with a stambouline frock coat (reads as Egypt-industrial, even though Ottoman officials in Baghdad also wore it); a white veil trailing down the back (reads as Egypt women); black-and-white or other chequered keffiyeh patterns tied to modern political movements, and any slogans; Ottoman military uniforms; the black horsehair face veil (pushi) or anything else that hides the face; daggers, rifles or other weapons; Gulf-style all-white thobe and ghutra (a modern look)
```

Files: `outfit_m_iraq_industrial.png`, `hair_m_iraq_industrial.png`, `facialhair_m_iraq_industrial.png`, `headwear_m_iraq_industrial.png`, `accessory_m_iraq_industrial.png`, `outfit_f_iraq_industrial.png`, `hair_f_iraq_industrial.png`, `headwear_f_iraq_industrial.png`, `accessory_f_iraq_industrial.png`

### Greece: Athens, Kingdom of Greece

```
PAIR: greece_industrial (Athens, Kingdom of Greece, 1860)
Place and time: Athens, new capital of the independent Greek kingdom, c. 1860

MAN
- Outfit: Civilian fustanella: a white, many-pleated linen kilt ending just above the knee, a full-sleeved white shirt, and a dark-crimson sleeveless jacket (fermeli) trimmed with gold braid and worn open over an embroidered waistcoat. Below are knitted white stockings with tasselled garters and plain black leather shoes.
- Hair: Short on top, slightly longer at the nape
- Facial hair: Thick moustache with slightly upturned ends
- Headwear: Fesi: a soft Greek fez of red felt whose limp crown droops to one side, worn tilted, with a very long dark-blue silk tassel falling to the chest. It is clearly floppy, unlike Egypt's stiff, upright tarboush.
- Accessory: Wide red silk sash (zonari) wound around the waist

WOMAN
- Outfit: Amalia dress: a floor-length full skirt of pale ivory or rose silk, a white long-sleeved blouse with a lace chemisette front, and a short, fitted, open bolero jacket (kontogouni) of deep-burgundy velvet heavily embroidered with gold braid.
- Hair: Long hair plaited into two braids and wound around the base of the fesi
- Headwear: Small red fesi pinned tilted on the crown, with a long tassel of braided gold thread falling to the shoulder. The face and hairline are fully visible.
- Accessory: Necklace of small gold coins on a chain (dowry flouria)

MUST READ AT A GLANCE: Men: the white, many-pleated fustanella kilt. Women: the gold-embroidered velvet kontogouni bolero with a small tilted red fesi and a gold tassel.
DO NOT DRAW: Evzone guard look: pom-pom tsarouchia shoes, guard ranks, ceremonial drill pose (military); silahlik weapon belt, pistols, yataghan, rifles, cartridge belts; stiff, tall, upright Egyptian or Ottoman tarboosh (reads as Egypt); a long white veil trailing down the back (reads as Egypt women); Queen Amalia's crown or court jewels (royalty); a bright-blue-and-white colour scheme (the Greek flag); Orthodox clergy dress (sacred); Italian tabarro cloak or broad-brimmed felt hat (reads as Italy)
```

Files: `outfit_m_greece_industrial.png`, `hair_m_greece_industrial.png`, `facialhair_m_greece_industrial.png`, `headwear_m_greece_industrial.png`, `accessory_m_greece_industrial.png`, `outfit_f_greece_industrial.png`, `hair_f_greece_industrial.png`, `headwear_f_greece_industrial.png`, `accessory_f_greece_industrial.png`

### Italy: Bologna, Kingdom of Italy

```
PAIR: italy_industrial (Bologna, Kingdom of Italy, 1895)
Place and time: Bologna and Pontecchio, where young Guglielmo Marconi ran his first radio experiments, 1895

MAN
- Outfit: Dark wool three-piece suit with a white shirt, stiff standing collar and dark cravat. Over it is a full, calf-length circular tabarro cloak in black or midnight-blue wool, with a velvet collar and a metal chain clasp at the throat. One side is flung back over the opposite shoulder, showing the suit.
- Hair: Short, side-parted and pomaded
- Facial hair: Full, thick moustache brushed outward in the 'Umbertine' fashion, not waxed to points
- Headwear: Wide-brimmed soft black felt hat with a low rounded crown and a broad, slightly drooping brim (the hat worn with the tabarro in the Po Valley)
- Accessory: Long white silk scarf hanging loose around the neck under the open cloak, in the style of Verdi

WOMAN
- Outfit: Mid-1890s dress in dark wool: a fitted bodice with a high collar, puffed leg-of-mutton sleeves and a smooth, bell-shaped, floor-length skirt. A large black silk scialle (shawl) with a long knotted fringe is draped over the shoulders and crossed at the chest.
- Hair: Swept up into a high chignon with a soft curled fringe
- Headwear: none
- Accessory: Multi-strand red coral bead necklace worn over the high collar

MUST READ AT A GLANCE: Men: the tabarro, a full dark circular cloak with a velvet collar and one side flung over the opposite shoulder, worn with a broad-brimmed black felt hat. Women: the black fringed silk scialle crossed over the chest, with a red coral necklace.
DO NOT DRAW: Garibaldi red shirt (volunteer military uniform); Bersaglieri feathered hat, carabinieri, any uniform; King Umberto I style regalia (royalty); mafia, gangster or cloaked-villain caricature; organ-grinder or peasant caricature; Greek fustanella or fez (reads as Greece); Homburg or dented-crown hat with a narrow curled brim, or pince-nez (reads as Germany-industrial); top hat and frock coat without the cloak, or a poke bonnet with a Paisley shawl (reads as Britain-industrial)
```

Files: `outfit_m_italy_industrial.png`, `hair_m_italy_industrial.png`, `facialhair_m_italy_industrial.png`, `headwear_m_italy_industrial.png`, `accessory_m_italy_industrial.png`, `outfit_f_italy_industrial.png`, `hair_f_italy_industrial.png`, `accessory_f_italy_industrial.png`

### China: Qing Shanghai

```
PAIR: china_industrial (Qing Shanghai, 1880)
Place and time: Shanghai in the late Qing, c. 1880: the Shenbao newspaper, the Jiangnan Arsenal translation bureau and the first telegraph lines

MAN
- Outfit: Changshan: an ankle-length gown in dark-blue or grey silk with a small standing collar and a curved diagonal (dajin) opening fastened by cloth knot buttons down the right side. Over it is a magua: a short, hip-length black jacket with a straight centre opening, knot buttons and roomy sleeves. Black cloth shoes with white soles.
- Hair: Queue: the front of the scalp shaved back to the crown and the rest braided into one long, neat plait down the back. Draw it tidily and with dignity, never as a caricature.
- Facial hair: none (clean-shaven; by Qing custom moustaches were usually grown only in middle age)
- Headwear: Guapimao: a rounded black satin skullcap of six segments with a red knotted-silk button on top and no official finial
- Accessory: Hebao: a small embroidered silk purse on a knotted silk cord with a tassel, hanging from the waist belt at the right hip just below the hem of the magua

WOMAN
- Outfit: Ao: a loose, knee-length jacket with wide flared sleeves and a curved right-side opening with knot buttons. Its collar edge, front curve, cuffs and hem are covered in several broad bands of contrasting embroidered trim (xianggun). The jacket is lotus-pink or pale blue with black trim, worn over a black pleated mamian skirt.
- Hair: Sleek and centre-parted, combed smoothly back into a flat low bun at the nape and pinned with a silver hairpin
- Headwear: Meile (brow band): a narrow band of black satin worn across the forehead just above the brows and tied at the back. It is embroidered with small flowers and set with a small pearl or jade ornament at centre front, and leaves the face fully visible.
- Accessory: Green jade bangle bracelet

MUST READ AT A GLANCE: Men: the black six-segment guapimao skullcap with a red knot button, worn with the queue. Women: the broad-trimmed ao jacket with a black embroidered meile band across the forehead.
DO NOT DRAW: 19th-century Western 'pigtail' cartoons, Fu Manchu or long drooping moustache, slanted-eye features; Opium pipes or any opium imagery; Qing official hat with rank finial or peacock feather, mandarin squares, court beads (chaozhu); Boxer, Taiping or military imagery; Manchu court headdress (liangbatou) or platform shoes on a Han civilian woman; Any emphasis on bound feet; Coolie straw hat; 1920s qipao (anachronistic); bowler hat, haori or hakama (reads as Japan-industrial)
```

Files: `outfit_m_china_industrial.png`, `hair_m_china_industrial.png`, `headwear_m_china_industrial.png`, `accessory_m_china_industrial.png`, `outfit_f_china_industrial.png`, `hair_f_china_industrial.png`, `headwear_f_china_industrial.png`, `accessory_f_china_industrial.png`

### Japan: Meiji Nagoya

```
PAIR: japan_industrial (Meiji Nagoya, 1899)
Place and time: Nagoya in the Meiji period, c. 1899, the time of Sakichi Toyoda's steam-powered wooden loom (1896) and Japan's rapid industrialisation

MAN
- Outfit: Dark kimono under a knee-length black haori with small white generic family crests (never the chrysanthemum), over grey-and-black striped pleated hakama. White tabi socks and zori sandals. The kimono collar crosses left over right.
- Hair: Zangiri: a short, Western-style, side-parted crop (topknots were abandoned after 1871)
- Facial hair: Neat, trimmed moustache, straight and not waxed to points
- Headwear: Black Western bowler hat (yamataka-bo) with a rigid round dome and a narrow brim
- Accessory: Haori-himo: a thick white braided silk cord tied in a flat knot with two tassels at the centre of the chest, closing the haori

WOMAN
- Outfit: Jogakusei (girl student) look: a small-patterned kimono, e.g. with purple-and-white yagasuri arrow-feather stripes, under a maroon (ebicha) pleated hakama tied high above the waist. Tabi with zori or low boots. The kimono collar crosses left over right.
- Hair: Sokuhatsu: hair swept up and back into a puffed pompadour roll with a bun at the back of the head (Western-inspired, promoted from 1885)
- Headwear: none
- Accessory: Large silk ribbon bow tied at the back of the sokuhatsu

MUST READ AT A GLANCE: Men: a black bowler hat worn with a crested haori, white himo cord and striped hakama, a Western hat over Japanese dress. Women: the maroon hakama tied high over a yagasuri kimono, with sokuhatsu hair and a ribbon bow.
DO NOT DRAW: Imperial Army or Navy uniforms, the Rising Sun flag, the sixteen-petal imperial chrysanthemum; Swords (banned in 1876); Samurai topknot (anachronistic by now); Geisha makeup or kimono; Chinese queue, skullcap or changshan; Collar closed right-over-left (the dressing used for the dead); a bowler worn with a Western frock coat or suit (loses the Japanese tell; reads as Germany or Britain)
```

Files: `outfit_m_japan_industrial.png`, `hair_m_japan_industrial.png`, `facialhair_m_japan_industrial.png`, `headwear_m_japan_industrial.png`, `accessory_m_japan_industrial.png`, `outfit_f_japan_industrial.png`, `hair_f_japan_industrial.png`, `accessory_f_japan_industrial.png`

### Britain: Victorian Britain

Done in the pilot (Batch 1).

### Germany: Wilhelmine Germany

```
PAIR: germany_industrial (Wilhelmine Germany, 1899)
Place and time: Berlin c. 1899, during the boom in German electrical engineering (Siemens, AEG) and chemistry (Bayer's aspirin, synthetic dyes), when Berlin's permanent dress-reform exhibition opened

MAN
- Outfit: Dark charcoal wool frock coat (Gehrock) buttoned high over a matching waistcoat, with narrow dark striped trousers. A very tall, stiff white stand-up collar (Stehkragen) with a narrow dark tie.
- Hair: Short brush cut (Buerstenschnitt): hair cropped close and standing upright
- Facial hair: Moustache with the ends twisted up in the Wilhelmine 'Es ist erreicht' fashion, kept moderate rather than pointed up to the cheekbones
- Headwear: Dark felt Homburg with a single dent down the centre of the crown and a stiff, curled-up, ribbon-bound brim (named after Bad Homburg in Hesse)
- Accessory: Pince-nez (Kneifer) clipped to the nose, on a thin black cord

WOMAN
- Outfit: Reformkleid: a loose, uncorseted, floor-length dress hanging straight from a fitted shoulder yoke, with soft full sleeves, in sage-green, cream or dove-grey wool. The yoke and hem are embroidered with flowing Jugendstil plant curves.
- Hair: Soft, loose chignon: hair pinned up low and full at the back, with natural waves at the temples
- Headwear: none
- Accessory: Jugendstil silver-and-enamel brooch at the collar in the Pforzheim style, with a curving 'whiplash' leaf motif

MUST READ AT A GLANCE: Men: a Homburg hat with pince-nez and an upturned moustache. Women: a loose, uncorseted Reformkleid with Jugendstil embroidery on the yoke.
DO NOT DRAW: Pickelhaube or any military or police uniform; Iron Cross, eagle emblems, black-white-red imperial colours; student-fraternity caps, sashes or duelling scars; monocle (Prussian-officer caricature); exaggerated needle-point Kaiser moustache (caricature); lederhosen, dirndl or Tyrolean hats; top hat with mutton-chop whiskers, or a poke bonnet with Paisley shawl (Britain-industrial); broad-brimmed soft felt hat with a full circular cloak (Italy-industrial); a round-domed bowler (the Japan-industrial hat); tight wasp-waist corset with a bustle
```

Files: `outfit_m_germany_industrial.png`, `hair_m_germany_industrial.png`, `facialhair_m_germany_industrial.png`, `headwear_m_germany_industrial.png`, `accessory_m_germany_industrial.png`, `outfit_f_germany_industrial.png`, `hair_f_germany_industrial.png`, `accessory_f_germany_industrial.png`

**Send to Claude.**

## 13. Batch 7: Modern era, 1900 to 2000 (60 images)

Folder: `Raw/modern/`. Start a new chat for this era. For each country, paste its PAIR block, then make each file in its list.

### Egypt: Nasser's Egypt

```
PAIR: egypt_modern (Nasser's Egypt, 1962)
Place and time: Egypt under Gamal Abdel Nasser, Cairo and Aswan, c. 1962 (Aswan High Dam under construction 1960-70; Suez Canal nationalised 1956)

MAN
- Outfit: Ankle-length galabiya of fine pale-grey or finely striped cotton (loose and collarless, with a deep slit neckline and wide sleeves), worn with a tailored dark Western suit jacket open over it; leather shoes.
- Hair: Short hair combed straight back from a high forehead
- Facial hair: Thick, neat moustache, otherwise clean-shaven
- Headwear: none (bareheaded; the tarboush was abandoned after the 1952 revolution)
- Accessory: Shal: a long, plain dark wool scarf (brown, charcoal or camel) with a knotted fringe, draped round the neck so both ends hang straight down the chest outside the jacket. No stripes.

WOMAN
- Outfit: Village or provincial galabiya: an ankle-length, loose, long-sleeved dress in brightly printed cotton (small flowers on a saturated red, green or blue ground), with a round neck and a gathered yoke, often finished with a gathered flounce at the hem. Flat slippers.
- Hair: Long hair, centre-parted, in one thick plait hanging down the back below the scarf. Only the parting and a little front hair show.
- Headwear: Mandil abu oya: a coloured triangular cotton or chiffon headscarf tied at the back of the head (knot at the nape, point covering the back of the head), its front edge trimmed with a fringe of tiny crocheted flowers or beads that frames the forehead. Face, chin and neck fully visible.
- Accessory: Ghawayesh: a stack of thick gold bangles on each wrist

MUST READ AT A GLANCE: The galabiya. Men wear a tailored Western jacket over it. Women wear a bright printed galabiya with a flower-edged mandil tied at the nape.
DO NOT DRAW: tarboush or fez (reads as Egypt-industrial); military uniforms or Free Officers' khaki; revolutionary or United Arab Republic emblems, the eagle, flags; a likeness of Nasser or Umm Kulthum; keffiyeh with agal, or the sidara cap (read as Iraq); black melaya laff wrap over the head (reads as Iraq's 'abaya); face veils or the burqu'; belly-dance costume, coin hip-scarves or any 'harem' caricature; bouffant hair, sheath dress and big sunglasses (read as 1960 Rome); striped scarf (reads as Britain's college scarf); pharaonic costume
```

Files: `outfit_m_egypt_modern.png`, `hair_m_egypt_modern.png`, `facialhair_m_egypt_modern.png`, `accessory_m_egypt_modern.png`, `outfit_f_egypt_modern.png`, `hair_f_egypt_modern.png`, `headwear_f_egypt_modern.png`, `accessory_f_egypt_modern.png`

### Iraq / Mesopotamia: Kingdom of Iraq

```
PAIR: iraq_modern (Kingdom of Iraq, 1956)
Place and time: Baghdad c. 1956, the Development Board's modernist building boom (commissions for Gropius, Le Corbusier and Wright) in the city where Zaha Hadid grew up

MAN
- Outfit: Light-grey or tan wool two-piece suit with wide lapels, white shirt, dark tie and polished shoes.
- Hair: Short, neatly combed and side-parted, mostly under the cap
- Facial hair: Neatly trimmed moustache
- Headwear: Sidara (al-Faisaliyya): a soft black felt or velvet civilian cap that folds flat; it is boat-shaped, with a raised point at front and back and a lengthwise crease along the crown, worn straight on the head with no badge
- Accessory: Thick black horn-rimmed glasses with clear lenses

WOMAN
- Outfit: Tailored 1950s dress (fitted bodice, belted waist, full mid-calf skirt) in a jewel colour, with a black silk 'abaya drawn over the head and falling open at the front to the ankles so the dress shows.
- Hair: Short, softly permed curls, side-parted, visible at the front beneath the edge of the 'abaya
- Headwear: The top edge of the black 'abaya resting on the crown of the head, leaving the face and front hair visible
- Accessory: Gold coin pendant: a single large gold coin (lira) on a short gold chain, visible at the neckline where the 'abaya falls open

MUST READ AT A GLANCE: The sidara, Iraq's folding boat-shaped national cap, on men. The black 'abaya over the head, worn open over a bright 1950s dress, on women.
DO NOT DRAW: military berets, garrison-cap insignia or any badge on the sidara; Ba'ath-era imagery, flags, national eagles; fez or tarboush (Ottoman and Egypt); keffiyeh with agal (reserved for Iraq-industrial); Gulf-style white thobe and ghutra; face veils; pearl strand (Britain modern's 'twinset and pearls'); a likeness of King Faisal, Zaha Hadid or any real person
```

Files: `outfit_m_iraq_modern.png`, `hair_m_iraq_modern.png`, `facialhair_m_iraq_modern.png`, `headwear_m_iraq_modern.png`, `accessory_m_iraq_modern.png`, `outfit_f_iraq_modern.png`, `hair_f_iraq_modern.png`, `headwear_f_iraq_modern.png`, `accessory_f_iraq_modern.png`

### Greece: Metapolitefsi Athens

```
PAIR: greece_modern (Metapolitefsi Athens, 1975)
Place and time: Athens after the fall of the junta and the restoration of democracy, c. 1975

MAN
- Outfit: Brown corduroy jacket over a thick hand-knit wool sweater or an open-collared patterned shirt, with flared jeans and suede desert boots.
- Hair: Collar-length, slightly shaggy 1970s cut
- Facial hair: Thick full moustache
- Headwear: none
- Accessory: Tagari: hand-woven wool shoulder bag in bold horizontal stripes, with a fringed bottom, worn diagonally across the body

WOMAN
- Outfit: White cotton folk-revival blouse with red and black cross-stitch embroidery at the neckline and cuffs, under a loose chunky knit cardigan. Below are a long midi skirt or flared jeans and flat handmade leather strap sandals.
- Hair: Long, straight, centre-parted and loose
- Headwear: none
- Accessory: Tagari: hand-woven wool shoulder bag in bold horizontal stripes, with a fringed bottom, worn diagonally across the body

MUST READ AT A GLANCE: Tagari: a hand-woven striped wool shoulder bag with a fringed bottom, worn crossbody
DO NOT DRAW: junta-era (1967-74) uniforms, insignia or the phoenix emblem (regime symbols); any military or police uniform; Greek flag as clothing; ancient chiton or Evzone costume on a modern traveller; Orthodox clergy dress (sacred); 'Zorba' or taverna-dancer caricature; komboloi worry beads (held prop, not worn); fisherman's or other soft peaked cap (collides with Beijing 1972's cap); plain khaki canvas satchel (reads as Beijing 1972); slim dark suit and dark sunglasses (reads 1960 Italy)
```

Files: `outfit_m_greece_modern.png`, `hair_m_greece_modern.png`, `facialhair_m_greece_modern.png`, `accessory_m_greece_modern.png`, `outfit_f_greece_modern.png`, `hair_f_greece_modern.png`, `accessory_f_greece_modern.png`

### Italy: Dolce Vita Rome

```
PAIR: italy_modern (Dolce Vita Rome, 1960)
Place and time: Rome during the economic miracle, the year of 'La Dolce Vita', 1960

MAN
- Outfit: Slim, sharply tailored single-breasted suit in charcoal or midnight mohair-wool, with a short jacket, narrow lapels and narrow cropped trousers. Worn with a crisp white shirt and thin dark tie (or a fine-knit black turtleneck, the 'dolcevita') and polished slip-on loafers.
- Hair: Short, neatly slicked back with pomade
- Facial hair: none
- Headwear: none
- Accessory: Dark sunglasses with solid black lenses and heavy black frames

WOMAN
- Outfit: Fitted knee-length sleeveless sheath dress with a narrow waist in a bold, swirling kaleidoscopic silk print (magenta, turquoise, lime) in the 1960s Florentine style, with pointed low-heeled pumps.
- Hair: Short, voluminous backcombed bouffant, left uncovered
- Headwear: none
- Accessory: Cat-eye sunglasses with dark lenses

MUST READ AT A GLANCE: Dark sunglasses: men wear them with a razor-slim tailored suit; women wear cat-eye sunglasses with a swirling kaleidoscopic silk-print sheath and an uncovered bouffant
DO NOT DRAW: Fascist-era black shirt, fasces or any 1920s-40s regime insignia; military or carabinieri uniforms; mafia or gangster caricature (pinstripes, fedora, violin case); pizza-chef, gondolier striped shirt and straw boater stereotypes; Vespa or other held or ridden props; priest's collar or clerical dress (sacred); headscarf knotted under the chin (now Britain modern's tell); woven crossbody bag or fisherman's cap (reads 1970s Greece); orange-padded headphones or lapel badge (reads Japan 1980)
```

Files: `outfit_m_italy_modern.png`, `hair_m_italy_modern.png`, `accessory_m_italy_modern.png`, `outfit_f_italy_modern.png`, `hair_f_italy_modern.png`, `accessory_f_italy_modern.png`

### China: Beijing, People's Republic

```
PAIR: china_modern (Beijing, People's Republic, 1972)
Place and time: Beijing, c. 1972, when Tu Youyou's Project 523 team isolated artemisinin at the Academy of Traditional Chinese Medicine

MAN
- Outfit: Zhongshan suit: loose, boxy grey or navy-blue cotton-wool jacket with a closed turn-down collar, five front buttons and four patch pockets with buttoned flaps, worn with matching straight, roomy trousers and black cloth slip-on shoes.
- Hair: Short, neat side-parted crop
- Facial hair: none
- Headwear: Soft navy cotton cap with a tall, rounded, slightly stiffened crown, a cloth band and a short stiff peak. It sits high on the head, unlike a low flat cap. Plain, with no badge or star.
- Accessory: Fountain pen clipped in the upper-left chest pocket, the 1970s mark of an educated professional

WOMAN
- Outfit: Women's version of the same jacket in pale blue or grey cotton, with turn-down collar and patch pockets and a white shirt collar folded out over it. Straight dark trousers and black cloth strap shoes; practical and unornamented.
- Hair: Short, straight, chin-length bob with a side part, held back on one side by a plain black hair clip
- Headwear: none
- Accessory: Plain faded-khaki canvas satchel on a cross-body strap, with no star, slogan, print or stripes

MUST READ AT A GLANCE: Blue-grey Zhongshan (Mao) suit with four patch pockets, worn by men and women alike
DO NOT DRAW: Mao badges or portrait pins; Red-star cap badge, green PLA uniform, Red Guard armbands; Little Red Book, slogans, any propaganda text; 1930s qipao or Shanghai glamour (wrong decade); White lab coat as the main garment (not culturally distinctive); Coolie hat or any caricature; Japanese salaryman suit and tie; low flat tweed cap (Britain modern); striped or fringed woven bag (Greece modern)
```

Files: `outfit_m_china_modern.png`, `hair_m_china_modern.png`, `headwear_m_china_modern.png`, `accessory_m_china_modern.png`, `outfit_f_china_modern.png`, `hair_f_china_modern.png`, `accessory_f_china_modern.png`

### Japan: Showa Tokyo

```
PAIR: japan_modern (Showa Tokyo, 1980)
Place and time: Tokyo during the Walkman boom, c. 1980, as Akio Morita's Sony took Japanese electronics global

MAN
- Outfit: Salaryman suit: dark navy or charcoal single-breasted two-piece with a white shirt and a narrow dark tie, plus a small round enamel company badge (shasho) with a generic abstract mark on the left lapel; black leather shoes.
- Hair: Short hair neatly combed back with a side part
- Facial hair: none
- Headwear: none
- Accessory: Lightweight silver-and-black headband headphones with round bright-orange foam ear pads worn on the head, a thin cord running to a compact blue-and-silver cassette player in a small tan leather case hooked on the belt; no brand logo

WOMAN
- Outfit: Office lady (OL) company uniform: a fitted navy or burgundy waistcoat over a white blouse with a soft ribbon bow at the collar, a matching knee-length pleated skirt, sheer stockings and low pumps.
- Hair: Seiko-chan cut: layered, shoulder-length feathered bob with soft inward flicks at the sides and wispy bangs (the c. 1980 pop-idol trend)
- Headwear: none
- Accessory: The same orange-foam headphones resting around the neck

MUST READ AT A GLANCE: Orange-foam headphones worn with a portable cassette player
DO NOT DRAW: Rising Sun flag or rays, or a hachimaki headband with the hinomaru; WWII military uniforms or imagery; Sony or any real brand logo; Anime or manga caricature, samurai or geisha cliches; Kimono (not everyday wear at this moment); Chinese Zhongshan suit; dark sunglasses (reads as 1960 Rome, whose slim suit is close to the salaryman's)
```

Files: `outfit_m_japan_modern.png`, `hair_m_japan_modern.png`, `accessory_m_japan_modern.png`, `outfit_f_japan_modern.png`, `hair_f_japan_modern.png`, `accessory_f_japan_modern.png`

### Britain: Post-war Britain

```
PAIR: britain_modern (Post-war Britain, 1950)
Place and time: Manchester, c. 1950, where Alan Turing was working on the university's pioneering stored-program computer (the Manchester Mark 1, built commercially by Ferranti)

MAN
- Outfit: Brown herringbone tweed sports jacket with leather elbow patches over a Fair Isle-patterned sleeveless knitted pullover, a white shirt and a knitted wool tie. Baggy grey flannel trousers and brown brogues.
- Hair: Short back and sides: neatly cut, side-parted and slicked with hair cream
- Facial hair: none
- Headwear: Tweed flat cap: low, flat crown sloping forward onto a short peak
- Accessory: Long college scarf in broad block stripes of two contrasting colours, wound once round the neck with the ends hanging loose

WOMAN
- Outfit: Soft wool twinset (short-sleeved jumper with a matching cardigan) in a pastel or heather tone, tucked into a below-the-knee A-line tweed skirt. Seamed stockings and low-heeled court shoes.
- Hair: Early-1950s 'set': collar-length hair in soft rolled pin-curls framing the face
- Headwear: Printed silk square headscarf folded into a triangle and knotted under the chin, covering the ears, with a roll of curls showing at the forehead; face fully visible
- Accessory: A single strand of pearls (the 'twinset and pearls')

MUST READ AT A GLANCE: Tweed and knitwear. Men wear an elbow-patched herringbone tweed jacket over a Fair Isle pullover with a flat cap. Women wear a twinset and pearls with a tweed skirt and an under-the-chin headscarf.
DO NOT DRAW: military, Home Guard, ARP or any uniform; Union Jack, royal regalia, guards' bearskins, police helmets; 1960s Mod or 1970s punk looks (the wrong moment); cloche hat, Bubikopf bob or Bauhaus colour-block prints (Germany modern); broad-brimmed soft felt hat with round wire spectacles and a walrus moustache (Germany modern); tall-crowned navy peaked cap (China modern); kerchief knotted at the back of the head with a flower-trimmed edge (Egypt modern); a likeness of Alan Turing; Sherlock-style deerstalker cliche
```

Files: `outfit_m_britain_modern.png`, `hair_m_britain_modern.png`, `headwear_m_britain_modern.png`, `accessory_m_britain_modern.png`, `outfit_f_britain_modern.png`, `hair_f_britain_modern.png`, `headwear_f_britain_modern.png`, `accessory_f_britain_modern.png`

### Germany: Weimar Berlin

```
PAIR: germany_modern (Weimar Berlin, 1926)
Place and time: Berlin c. 1926, the Weimar Republic's golden age of science and design: Einstein directs the Kaiser Wilhelm Institute for Physics, and the Bauhaus has just moved to Dessau

MAN
- Outfit: Loose, slightly rumpled charcoal three-piece wool suit with wide trousers, a white shirt with a soft turn-down collar and a dark knitted tie. A long dark wool overcoat is worn open.
- Hair: Collar-length hair brushed back from the forehead, a little untidy (not a wild halo)
- Facial hair: Bushy walrus moustache
- Headwear: Broad-brimmed soft black felt hat with a pinched crown
- Accessory: Round wire-rimmed spectacles

WOMAN
- Outfit: Straight, loose, knee-length drop-waist dress with a Bauhaus-style geometric pattern of colour blocks (red, ochre, blue and black squares, bars and circles), worn with flat bar-strap shoes.
- Hair: Bubikopf: a sleek chin-length bob with a blunt fringe
- Headwear: Close-fitting felt cloche hat pulled down to the eyebrows, with a small brim so the face stays clear
- Accessory: Geometric chrome-and-brass necklace of circles and bars in the Bauhaus metal-workshop style

MUST READ AT A GLANCE: For women, Bauhaus colour-block geometry with a Bubikopf bob and cloche. For men, a broad-brimmed soft felt hat, walrus moustache and round wire spectacles.
DO NOT DRAW: ANY uniform, armband, swastika or other regime or military insignia; brown shirts, black shirts, jackboots, leather trench coats (secret-police stereotype); Iron Cross, eagles, black-white-red imperial colours; toothbrush moustache; monocle or Pickelhaube; lederhosen, dirndl or beer-hall caricature; 'Cabaret' decadence caricature (fishnets, lingerie); a likeness of Einstein (no wild white hair, no famous-portrait pose); tweed flat cap, Fair Isle knitwear or twinset and pearls (Britain modern)
```

Files: `outfit_m_germany_modern.png`, `hair_m_germany_modern.png`, `facialhair_m_germany_modern.png`, `headwear_m_germany_modern.png`, `accessory_m_germany_modern.png`, `outfit_f_germany_modern.png`, `hair_f_germany_modern.png`, `headwear_f_germany_modern.png`, `accessory_f_germany_modern.png`

**Send to Claude.**

## 14. Batch 8: the Future (20 images)

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

### Future shaped by Egypt

```
PAIR: future_egypt (the Future, after Egypt came to dominate history)
Take the neutral Future outfits (same practical cut, same fit) and rework them with these motifs, for both the man and the woman:
Crisp white and ecru knife-pleated linen; concentric broad-collar necklines reworked as layered circular yokes in bead-row colours (Nile turquoise, lapis blue, carnelian red, gold); kohl-wing graphic lines as piping along seams; vertical fluting taken from lotus and papyrus columns; Mamluk oversized hanging sleeves as dramatic cape-sleeves; the tarboush's truncated cone as a sleek crimson cap with a single tassel; mashrabiya lattice perforation and irrigation-canal grid quilting. Avoid god emblems, royal crowns and cobra motifs.
Keep it wearable civilian clothing. No flags, emblems, insignia or readable text.
```

Files: `outfit_m_future_egypt.png`, `outfit_f_future_egypt.png`

### Future shaped by Iraq / Mesopotamia

```
PAIR: future_iraq (the Future, after Iraq / Mesopotamia came to dominate history)
Take the neutral Future outfits (same practical cut, same fit) and rework them with these motifs, for both the man and the woman:
Tiered Babylonian fringe as kinetic hem and shoulder fringing; one-shoulder spiral draping; embossed cuneiform-wedge texture and cylinder-seal roll-print bands (decorative, not readable); glazed-brick lapis blue with ochre, date-palm brown, cream and gold; the Abbasid tall-cap silhouette and taylasan shoulder shawl as a high hood-collar; tiraz armbands in pseudo-script; square-shouldered open 'aba cloaks with gold-thread yoke edging; the concentric circles of Baghdad's Round City and muqarnas honeycomb geometry; flowing parametric curves recalling Iraqi-born Zaha Hadid's architecture; the sidara's folded boat-shaped crown. Avoid religious text and deity motifs.
Keep it wearable civilian clothing. No flags, emblems, insignia or readable text.
```

Files: `outfit_m_future_iraq.png`, `outfit_f_future_iraq.png`

### Future shaped by Greece

```
PAIR: future_greece (the Future, after Greece came to dominate history)
Take the neutral Future outfits (same practical cut, same fit) and rework them with these motifs, for both the man and the woman:
Meander (Greek key) and wave-scroll borders as glowing trim lines on hems and cuffs. Asymmetric one-shoulder drapes from the himation and chiton, and crisp accordion pleating from the fustanella. Round fibula-style shoulder clasps, and Byzantine gold roundel (segmenta) inserts as circular panels. Palette of chalk white, Aegean blue, terracotta and black from red-figure pottery, with fine olive-leaf line embroidery. Striped hand-woven crossbody bags echoing the tagari, and tall brimmed crown shapes echoing the Palaiologan hat.
Keep it wearable civilian clothing. No flags, emblems, insignia or readable text.
```

Files: `outfit_m_future_greece.png`, `outfit_f_future_greece.png`

### Future shaped by Italy

```
PAIR: future_italy (the Future, after Italy came to dominate history)
Take the neutral Future outfits (same practical cut, same fit) and rework them with these motifs, for both the man and the woman:
Sweeping curved toga-like wraps with slim vertical clavi-stripe accents. Renaissance slashed-and-puffed sleeves with contrasting linings, and pomegranate damask/velvet patterns rendered as cut or iridescent textures. Palette of Florentine crimson, ochre, terracotta, travertine and Carrara-marble veining. Circular tabarro capes with velvet collars, razor-slim 1960s Roman tailoring with dark visor lenses, and kaleidoscopic swirl prints. A thin lenza forehead band reimagined as a light strip, with Vitruvian circle-and-square geometry, arcade arches and Marconi-style concentric radio-wave rings as graphic motifs.
Keep it wearable civilian clothing. No flags, emblems, insignia or readable text.
```

Files: `outfit_m_future_italy.png`, `outfit_f_future_italy.png`

### Future shaped by China

```
PAIR: future_china (the Future, after China came to dominate history)
Take the neutral Future outfits (same practical cut, same fit) and rework them with these motifs, for both the man and the woman:
Diagonal cross-collar (jiaoling) wrap closures and high standing collars fastened with sculpted pankou knot buttons. Deep, flowing sleeves cut as sheer layered panels. Horse-face (mamian) pleated side panels on long coats and skirts. Cloud-scroll (xiangyun) and key-fret (huiwen) borders reworked as glowing circuit-trace trim. Paper-fold pleating and translucent layers like xuan paper pages. Movable-type grids and star-chart or compass-rose line engraving for the navigation and knowledge theme. Jade-like translucent fastenings and lacquer finishes. Palette: ink black, cinnabar lacquer red, celadon, indigo, gold-thread accents. No imperial five-claw dragons, rank squares, flags or political badges.
Keep it wearable civilian clothing. No flags, emblems, insignia or readable text.
```

Files: `outfit_m_future_china.png`, `outfit_f_future_china.png`

### Future shaped by Japan

```
PAIR: future_japan (the Future, after Japan came to dominate history)
Take the neutral Future outfits (same practical cut, same fit) and rework them with these motifs, for both the man and the woman:
Kimono collar layering (kasane colour stacking) as a signature neckline. Wide, structured obi-style waist cinchers with modular clasps and obijime cord detail. Clean architectural shoulder lines inspired by the haori and kataginu. Hakama pleat geometry in wide trousers and skirts. Precise geometric patterns (asanoha hemp leaf, seigaiha waves, shippo, kikko, yagasuri) rendered as laser-cut or LED line work. Sashiko grid stitching and origami-fold panels. Visible karakuri-style mechanical joints and split-toe tabi footwear. Palette: aizome indigo, sumi black, washi white, vermilion lacquer, Hokusai Prussian blue, soft sakura-pink accents. No rising-sun rays, imperial chrysanthemum, flags or military insignia.
Keep it wearable civilian clothing. No flags, emblems, insignia or readable text.
```

Files: `outfit_m_future_japan.png`, `outfit_f_future_japan.png`

### Future shaped by Britain

```
PAIR: future_britain (the Future, after Britain came to dominate history)
Take the neutral Future outfits (same practical cut, same fit) and rework them with these motifs, for both the man and the woman:
Sharp Savile Row-style tailoring stretched into long frock-coat lines with high stand collars. Herringbone tweed, Fair Isle knit bands and bold Iron Age checks re-rendered as fine woven or circuit-trace micro-patterns. An open neck ring inspired by the torc. Pleated translucent collar rings that echo the Elizabethan ruff. Paisley teardrop scrollwork etched into panels. Brass rivets, railway-line pinstripes and punch-card perforation grids that recall steam, the telegraph and the computing of Lovelace and Turing. Palette: racing green, oxblood, slate grey, heather and navy, with brass and copper accents.
Keep it wearable civilian clothing. No flags, emblems, insignia or readable text.
```

Files: `outfit_m_future_britain.png`, `outfit_f_future_britain.png`

### Future shaped by Germany

```
PAIR: future_germany (the Future, after Germany came to dominate history)
Take the neutral Future outfits (same practical cut, same fit) and rework them with these motifs, for both the man and the woman:
Bauhaus geometry (circle, square, triangle) in red, yellow, blue and black on graphite. Clean, precision-engineered seams and round lens-like visor or spectacle elements that recall Jena optics. Jugendstil 'whiplash' curves as embroidered or laser-cut trim. Renaissance slashing and puffing reinterpreted as vented panels with contrasting linings. A wide, flat, soft Barett-style cap. Frilled, Kruseler-like pleated collars framing the face. Movable-type grid texture (letterforms used as pattern, never as slogans). Baltic amber and linen-white accents. Avoid black-white-red combinations, eagles, crosses and blackletter lettering.
Keep it wearable civilian clothing. No flags, emblems, insignia or readable text.
```

Files: `outfit_m_future_germany.png`, `outfit_f_future_germany.png`

**Send to Claude.**

## 15. Batch 9: premade characters

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

## Checklist before you send a batch to Claude

- Every file is 1024 x 1536 and named exactly as listed.
- The background is flat green, and the magenta mannequin is untouched wherever the new piece doesn't cover it.
- Nothing crosses the guide's blue safe-area box.
- No text, logos, flags or insignia anywhere.
- The "must read at a glance" item is clearly visible.

## Totals

| Batch | Images |
|---|---|
| Batch 1: pilot | 11 |
| Batch 2: skin tones and faces | 38 |
| Batch 3: Ancient | 59 |
| Batch 4: Medieval | 69 |
| Batch 5: Early modern | 69 |
| Batch 6: Industrial | 59 |
| Batch 7: Modern | 60 |
| Batch 8: the Future | 20 |
| Premade characters | 4 per character |
| **Total, not counting premade** | **385** |

Batch 1 is the only one that has to come first. After Batch 2, the era batches can be done in any order.
