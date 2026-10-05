# Famous travellers: whole-portrait art requests (Track E, 2026-10-05)

Saleh (2026-10-05): "the legendary characters, many of them are not famous enough ... they should be famous like Tesla." The premade cast is now twenty-seven household names of the game's eight nations, each from their place's moment (`Assets/Data/World/world_source.json`, `premades`). Until a portrait lands, every one of them stands at the desk as a generated stand-in: their place's layered dress through the character art's fallback table (`CharacterArtFallbackSO`), so **no art is required to play**. These requests replace the stand-ins with whole portraits.

## What to deliver

- Four whole images per character, made in the **"Time Sorter Premades"** Project with Block P and prompts 9.10 / 9.11 of `CHARACTER_ART_BRIEF_v2.md`: `premade_{id}_neutral.png`, `premade_{id}_happy.png`, `premade_{id}_angry.png`, `premade_{id}_worried.png` (1024 x 1536, flat #00FF00 background, the base figure `premadebase_[m/f]_skin[N].png` of their skin tone).
- The game picks up a portrait the moment its neutral image exists (`CharacterArt.HasFinalArt(premade_{id}_neutral)`); a missing expression falls back to the neutral one (`LookArtFallbackStep.NeutralExpression`).
- Dress: the place's own period dress (its Costume Guide item, so the dress rule never flags a famous traveller). Identity sits above mid-chest and reads with the head 58 px tall.

## Block P amendments (needed before these chats)

Block P was written for ten long-dead commoners. The new cast breaks three of its lines; amend them in the Project's instructions:

1. "none was a ruler" and "no royal regalia": four are rulers (Hatshepsut, Hammurabi, Cleopatra, Harun al-Rashid). Draw them in **court dress without regalia**: no crowns, sceptres or uraeus; a headcloth, a diadem band or a turban is dress, not regalia.
2. "no photograph of any of them exists": eight lived in the age of photography (Umm Kulthum, Omar Sharif, Maria Callas, Marconi, Kurosawa, Turing, Benz, Einstein). Keep the rule's intent: **a stylised anime portrait built from the features listed below, never a likeness copied from a photograph**.
3. "no weapons": Miyamoto Musashi is drawn **without swords** (his wooden practice oar may hang behind his shoulder only if the review allows it; otherwise hands empty, as everyone).

## The cast

| id | Name | Place (moment) | Sex, age at the desk | Bold identifying features | Expressions note |
|---|---|---|---|---|---|
| hatshepsut | Hatshepsut | New Kingdom Egypt, c. 1470 BCE | F, 37 | striped royal headcloth (nemes-like, no cobra), the ceremonial false beard on a strap, broad bead collar, kohl eyes | happy: regal half-smile; angry: one brow up, chin high |
| kulthum | Umm Kulthum | Nasser's Egypt, 1962 | F, 58 | dark glasses, hair up in a low bun, a folded silk scarf draped at the neck, pearl earrings | worried: hand to chest |
| sharif | Omar Sharif | Nasser's Egypt, 1962 | M, 30 | thick dark moustache, heavy brows, wavy black hair, open white shirt collar | happy: wide film-star smile |
| hammurabi | Hammurabi | Babylonia, c. 1760 BCE | M, 50 | long squared beard in rows of curls, round brimmed cap (civilian), fringed shawl over one shoulder | angry: the lawgiver's frown |
| khwarizmi | Muhammad al-Khwarizmi | Abbasid Baghdad, c. 830 | M, 50 | white turban, trimmed grey-black beard, scholar's robe, a pen case at the sash | happy: absorbed, pleased |
| harun | Harun al-Rashid | Abbasid Baghdad, c. 830 | M, 64 | tall turban with a jewel-less band, long grey beard, rich layered caftan (a plain cloak over it: he walks in disguise) | worried: a conspiratorial glance |
| socrates | Socrates | Periclean Athens, c. 430 BCE | M, 40 | snub nose, full lips, bald crown, bushy beard, plain himation | (brief section 11 already) |
| hippocrates | Hippocrates | Periclean Athens, c. 430 BCE | M, 30 | short curly beard, receding hair, himation, a small herb pouch at the chest | worried: the physician's concern |
| callas | Maria Callas | Metapolitefsi Athens, 1975 | F, 52 | winged black eyeliner, dark hair swept up, large statement earrings, a stole | angry: diva glare |
| cleopatra | Cleopatra | Republican Rome, c. 50 BCE | F, 19 | a diadem band (no crown), braided "melon" hairstyle with a bun, pearl earrings, Greek chiton with a pinned shoulder | happy: confident smile |
| cosimo | Cosimo de' Medici | Florentine Republic, 1439 | M, 50 | red cappuccio hood-hat, fur-trimmed dark gown, clean-shaven, long nose | happy: a banker's knowing smile |
| leonardo | Leonardo da Vinci | Sforza Milan, c. 1495 | M, 43 | (brief section 11 already) | |
| michelangelo | Michelangelo Buonarroti | Sforza Milan, c. 1495 | M, 20 | broken flattened nose, short dark curls, marble dust on the shoulders, plain work tunic | angry: stubborn |
| marconi | Guglielmo Marconi | Bologna, 1895 | M, 21 | slicked side parting, high stiff collar, earphone headset around the neck | happy: eager |
| cailun | Cai Lun | Eastern Han Luoyang, c. 105 | M, 43 | court official's black cap (civilian), long wide sleeves, a roll of paper tucked in the sash | worried: fussing over the paper |
| sudongpo | Su Dongpo | Northern Song Kaifeng, c. 1075 | M, 38 | the tall square "Dongpo hat", long beard, scholar's robe | happy: a poet's smile |
| musashi | Miyamoto Musashi | Tokugawa Edo, c. 1610 | M, 26 | unkempt topknot, stubble, plain dark kimono with no crest, NO swords | angry: a duellist's stare |
| kurosawa | Akira Kurosawa | Showa Tokyo, c. 1980 | M, 70 | tinted glasses, a fisherman's hat (bucket hat), tall and lean | angry: "Cut!" |
| shakespeare | William Shakespeare | Elizabethan England, c. 1600 | M, 36 | high bald forehead, moustache and small beard, gold ring earring, a white falling collar | happy: mischievous |
| ada | Ada Lovelace | Victorian Britain, 1843 | F, 28 | (brief section 11 already) | |
| darwin | Charles Darwin | Victorian Britain, 1843 | M, 34 | young, clean-shaven with side whiskers, big brow, a beetle on the lapel | happy: curious |
| dickens | Charles Dickens | Victorian Britain, 1843 | M, 31 | long wavy hair, flamboyant velvet waistcoat and a big cravat, a small beard just starting | angry: theatrical |
| turing | Alan Turing | Post-war Britain, c. 1950 | M, 38 | (brief section 11 already) | |
| gutenberg | Johannes Gutenberg | Rhineland, c. 1440 | M, 40 | (brief section 11 already) | |
| durer | Albrecht Durer | Renaissance Nuremberg, c. 1525 | M, 54 | long ringleted hair to the shoulders, forked beard, fur collar | worried: an artist's squint |
| benz | Karl Benz | Wilhelmine Germany, 1899 | M, 55 | full white-grey beard, round spectacles, oily work apron over a waistcoat | happy: proud inventor |
| einstein | Albert Einstein | Weimar Berlin, c. 1926 | M, 47 | the wild grey-black hair, bushy moustache, baggy cardigan, no socks (not shown) | happy: twinkling; worried: puzzled |

Skin tone: the one that fits the real person, on the matching base figure (`premadebase_[m/f]_skin[N].png`, the brief's section 5).

## Already requested (brief section 11)

Socrates, Leonardo, Gutenberg, Ada Lovelace and Alan Turing were in the first ten and keep their entries. The rest of the earlier cast (Senenmut, Aspasia, Ban Zhao, Arib al-Ma'muniyya, Aemilia Lanyer, Cecilia Gallerani, and the later Rifa'a al-Tahtawi, Nazik al-Mala'ika, Badr Shakir al-Sayyab, Odysseas Elytis, Federico Fellini, He Zehui, Sakichi Toyoda, Lise Meitner) left the game: **do not draw them**, and drop any of their files already made.

## Order

The days meet them in this order (two or three a day from day 6), so draw them in it: Cleopatra, Albert Einstein, William Shakespeare, Leonardo, Charles Darwin, Michelangelo, Hippocrates, Hammurabi, Charles Dickens, Umm Kulthum, Alan Turing, Guglielmo Marconi, al-Khwarizmi, Hatshepsut, Maria Callas, Miyamoto Musashi, Akira Kurosawa, Karl Benz, Cosimo de' Medici, Harun al-Rashid, Albrecht Durer, Cai Lun, Su Dongpo, Omar Sharif; the story beats Gutenberg (day 9), "Ada Lovelace" and "Socrates" (day 12).
