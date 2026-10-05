# Famous travellers: whole-portrait art requests (Track E2, 2026-10-05)

Saleh (2026-10-05): "they should be famous like Tesla", then "add places for them" and "drop the filter". The premade cast is thirty-three of the biggest household names of the eight nations, each in their own place's moment; fifteen new places (second moments of an era: Pella, Alexandria, Qin Xianyang, Galileo's Padua...) were added so the biggest names fit. Until a portrait lands, each stands at the desk as a generated stand-in in their place's dress, through the character art's fallback table (a second moment's dress borrows its main era's art until its own lands), so **no art is required to play**.

## What to deliver

- Four whole images per character, in the **"Time Sorter Premades"** Project with Block P and prompts 9.10 / 9.11 of `CHARACTER_ART_BRIEF_v2.md`: `premade_{id}_neutral.png`, `premade_{id}_happy.png`, `premade_{id}_angry.png`, `premade_{id}_worried.png` (1024 x 1536, flat #00FF00 background, the base figure of their skin tone).
- The game uses a portrait as soon as its neutral image exists; a missing expression falls back to the neutral one.
- Dress: their place's period dress (the Costume Guide's item, so the dress rule never flags them). Identity sits above mid-chest and reads with the head 58 px tall.
- The fifteen new places also need their garment layers (the brief's place batches) under their era ids: `ancient2`, `ancient3`, `medieval2`, `earlymodern2`, `industrial2`, `industrial3`; their labels are in `world_source.json` `places[].wardrobe`.

## Block P amendments (needed before these chats)

1. "none was a ruler" and "no royal regalia": rulers are in the cast now (Ramesses II, Cleopatra, Saladin, Alexander, Caesar, Qin Shi Huang, Genghis Khan, Nobunaga, Elizabeth I). Draw them in court or travelling dress without regalia: no crowns, sceptres or uraeus.
2. "no photograph of any of them exists": six lived in the age of photography (Umm Kulthum, Kurosawa, Turing, Marx, Einstein; Darwin late in life). A stylised anime portrait from the features below, never a likeness copied from a photograph.
3. "no weapons": the soldiers (Alexander, Caesar, Saladin, Sun Tzu, Genghis Khan, Nobunaga) are drawn unarmed, hands empty, as everyone.

## The cast, in the order the days meet them

| id | Name | Place (moment) | Sex, age at the desk | Bold identifying features |
|---|---|---|---|---|
| caesar | Julius Caesar | Republican Rome (Ancient), 50 BCE | M, 50 | receding hair combed forward, clean-shaven, laurel wreath, toga with a purple border |
| ramesses | Ramesses II | Ramesside Egypt (Ancient), 1250 BCE | M, 53 | blue khepresh war crown shape as a cloth cap (no cobra), broad gold collar, kohl eyes, proud long jaw |
| kurosawa | Akira Kurosawa | Showa Tokyo (Modern), 1980 | M, 70 | tinted glasses, a fisherman's bucket hat, tall and lean |
| saladin | Saladin | Ayyubid Cairo (Medieval), 1187 | M, 50 | sharbush cap wound with a plain turban cloth, full dark beard, calm eyes, plain mail-free coat |
| cleopatra | Cleopatra | Ptolemaic Alexandria (Ancient), 45 BCE | F, 24 | diadem band (no crown), braided melon bun, pearl earrings, Greek chiton with pinned shoulder |
| beethoven | Ludwig van Beethoven | Electoral Bonn (Industrial), 1790 | M, 20 | nineteen, wild black hair, broad face, high-collared coat, a cravat tied in a hurry |
| nobunaga | Oda Nobunaga | Sengoku Azuchi (Early modern), 1576 | M, 42 | shaved pate with a topknot, thin moustache, a red European-style cape over the kamishimo |
| alexander | Alexander the Great | Macedonian Pella (Ancient), 336 BCE | M, 20 | young, clean-shaven, lion-mane hair swept up from the brow, tilted head, chlamys pinned at the shoulder |
| darwin | Charles Darwin | Victorian Britain (Industrial), 1843 | M, 34 | young, clean-shaven with side whiskers, big brow, a beetle on the lapel |
| suntzu | Sun Tzu | Spring and Autumn Lu (Ancient), 500 BCE | M, 44 | lean face, thin moustache, plain dark robe, a bamboo-slip book at the sash |
| einstein | Albert Einstein | Weimar Berlin (Modern), 1926 | M, 47 | wild grey-black hair, bushy moustache, baggy cardigan |
| marx | Karl Marx | Revolutionary Cologne (Industrial), 1848 | M, 30 | full bushy black beard, swept-back hair, frock coat, a folded newspaper at the pocket |
| luther | Martin Luther | Renaissance Nuremberg (Early modern), 1525 | M, 42 | broad face, monk's tonsure grown out, black academic gown, a printed Bible at the chest |
| newton | Isaac Newton | Restoration London (Early modern), 1687 | M, 45 | long grey periwig, sharp nose, plain dark coat with a cravat, a prism at the chest pocket |
| michelangelo | Michelangelo Buonarroti | Sforza Milan (Early modern), 1495 | M, 20 | broken flattened nose, short dark curls, marble dust on the shoulders, plain work tunic |
| turing | Alan Turing | Post-war Britain (Modern), 1950 | M, 38 | (brief section 11 already) |
| confucius | Confucius | Spring and Autumn Lu (Ancient), 500 BCE | M, 51 | long thin grey beard, black guan cap, deep robe, hands folded in a bow (drawn open and empty) |
| aristotle | Aristotle | Macedonian Pella (Ancient), 336 BCE | M, 48 | short trimmed beard, receding hair, thoughtful frown, himation, a scroll tucked at the chest |
| hokusai | Katsushika Hokusai | Late Edo (Industrial), 1830 | M, 70 | seventy, bald with a little grey topknot, thin face, plain kosode, ink on the fingers |
| genghis | Genghis Khan | Mongol Zhongdu (Medieval), 1215 | M, 53 | weathered face, long drooping moustache, fur-brimmed toortsog hat, deel with a wide sash |
| qinshihuang | Qin Shi Huang | Qin Xianyang (Ancient), 215 BCE | M, 44 | black robe, Qin official's cap with a flat board (no imperial tassels), stern eyebrows |
| kulthum | Umm Kulthum | Nasser's Egypt (Modern), 1962 | F, 58 | dark glasses, hair up in a low bun, a folded silk scarf draped at the neck, pearl earrings |
| leonardo | Leonardo da Vinci | Sforza Milan (Early modern), 1495 | M, 43 | (brief section 11 already) |
| elizabeth | Elizabeth I | Elizabethan England (Early modern), 1600 | F, 67 | pale face, red curled hair, pearl-strung hair, a wide lace ruff (no crown) |
| khwarizmi | Muhammad al-Khwarizmi | Abbasid Baghdad (Medieval), 830 | M, 50 | white turban, trimmed grey-black beard, scholar's robe, a pen case at the sash |
| shakespeare | William Shakespeare | Elizabethan England (Early modern), 1600 | M, 36 | high bald forehead, moustache and small beard, gold ring earring, white falling collar |
| marcopolo | Marco Polo | Venice of the Doges (Medieval), 1295 | M, 41 | trimmed beard, Venetian capuccio hood-hat, a Mongol gold tablet on a cord at the neck |
| galileo | Galileo Galilei | Galileo's Padua (Early modern), 1610 | M, 46 | broad red-brown beard, receding hair, black scholar's gown, a brass spyglass at the belt |
| archimedes | Archimedes | Hellenistic Syracuse (Ancient), 230 BCE | M, 57 | full grey beard, wild curls, sun-browned, short tunic, a compass hanging at the belt |
| hammurabi | Hammurabi | Babylonia (Ancient), 1760 BCE | M, 50 | long squared beard in rows of curls, round brimmed cap, fringed shawl over one shoulder |
| socrates | Socrates | Periclean Athens (Ancient), 430 BCE | M, 40 | (brief section 11 already) |
| ada | Ada Lovelace | Victorian Britain (Industrial), 1843 | F, 28 | (brief section 11 already) |
| gutenberg | Johannes Gutenberg | Holy Roman Empire (Rhineland) (Medieval), 1440 | M, 40 | (brief section 11 already) |

## Left the cast: do not draw

Senenmut, Aspasia, Ban Zhao, Arib al-Ma'muniyya, Aemilia Lanyer, Cecilia Gallerani, Rifa'a al-Tahtawi, Nazik al-Mala'ika, Badr Shakir al-Sayyab, Odysseas Elytis, Federico Fellini, He Zehui, Sakichi Toyoda, Lise Meitner, Hatshepsut, Omar Sharif, Harun al-Rashid, Hippocrates, Maria Callas, Cosimo de' Medici, Guglielmo Marconi, Cai Lun, Su Dongpo, Miyamoto Musashi, Charles Dickens, Albrecht Durer, Karl Benz. Drop any of their files already made.
