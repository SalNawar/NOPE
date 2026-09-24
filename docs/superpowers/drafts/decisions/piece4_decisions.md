# Piece 4 — Characters (premade + generated layered travellers): decisions (made by Claude under Saleh's "go with all pieces dont stop", 2026-09-24)

Source analysis: SCRATCH/piece4_map.json (`synthesis.decisions` in order = C1..C19; `critique.extraDecisions` = W1..W12; handle or explicitly defer every `critique.missing` item). Stacked on pieces 2-3 (branch feat/characters); use their FINAL implemented names and rules (read the code; code wins over specs). Never touch Codex's files in E:\unity\NOPE (its untracked ART_ASSET_LIST.md / PRODUCTION_PLAN / hybrid 3D office are Codex's; read-only reference at most).

Saleh: "ill give you a base body, hair, outfits from each era, and accessories male and female for each and when you generate a random case you pull from these sprites making sure it makes sense unless the character is lying about something"; ChatGPT makes "premade and generated" characters; style direction: simpler, like Restory, neutral lighting.

- C1 (locked): generated travellers = layers (body, head, hair (+ hair-back slot per review #8), facial hair, outfit per country x era x gender, headwear, accessory); premades are drawn whole.
- C2 (locked): 1024x1536 canvas, all layers share it; placeholder-first; skin and hair colour come from the CLAIM and are never tells.
- C3 = A. Clothing tell = a provable third tell channel "Appearance" (ClueCategory.Culture), sharing piece 2's tellCount budget and piece 3's one-tell-per-category rule; enabled per day by the channel knob (Papers day 1, +Answer day 2, +Appearance day 3 and later).
- C4 = B. A desktop "Visitor" window shows the whole figure (auto-opened on case start like the Scanner/transcript), with clickable garments feeding the compare bar; the passport photo is a crop of the same stack; the booth figure is look-only (no Clickable → no HoverHighlighter change).
- C5 = A + W4 = B. Reuse ClueCategory.Culture: one Culture fact per place (the place's signature item(s)); the Costume Guide book (RefBook_Culture) lists them; respect the ~28-char value cap by keeping values short (designer picks the exact storage per W4-B and documents it).
- C6 = A. Append EvidenceKind.Appearance; TryRegister's "presented" side generalised together with piece 3's Answer; Summary says "traveller wears ...".
- C7 = A. Only the true home's signature item for the traveller's gender may leak; it must be a real item (never an absence), marked leakable; pure Domain Looks.CanLeak with a decision table. W5 = B: never the whole outfit.
- C8 = A. Layered rendering: booth = SortingGroup root with one SpriteRenderer per slot; desktop = a uGUI Image stack component reused for the Visitor window and the passport crop.
- C9 = A. Processed file-name keys in a Domain LookKeys grammar (e.g. body_{g}_skin{N}, head_{g}_skin{N}_face{a-d}, {slot}_{g}_{country}_{era}, baked hair colour variants); accessory, headwear and facial hair optional; lazy loading by key (no Addressables).
- C10 = A + W9 = B. An editor tool generates colour-coded placeholder layers (country hue, era shade, slot shape from the make_guide silhouettes) for every missing required key; reuse the builder's texture helpers by extracting them into a shared editor utility (no copies). Placeholders are NOT committed to LFS (generated on demand / into an ignored or clearly generated folder) and a runtime fallback keeps a fresh clone running — designer defines the exact policy.
- C11 = A. Wardrobe per place, look weights, confusable exclusions → world_source.json (imported once from the costume research by a hand-checked script committed as tooling if it's needed again); generator + validator.
- C12 = A. Seeds.ForLooks per traveller, fixed draw order (skin tone, face variant within the age band, hair colour); age band edges + grey-at-oldest rule are knobs (SO or JSON).
- C13 = A + W10: baked hair/facial-hair colour variants picked by key; skin/hair weights authored PER PLACE with a per-country default (Modern places differ), reviewed later by Saleh; never a tell.
- C14 = C (hybrid). Premades: real, long-dead, non-ruler historical figures named in their place's moment (fit its birth range) for pre-20th-century places; original characters inspired by real people for 20th-century (Modern) places. Content only — code is the same. Author a small starter set (e.g. one per country, placeholder art) — note that Saleh should review the list.
- C15 = A. Extend LegendarySO (keep type/GUIDs): id, recordName, birthDate, gender, claimed place, optional true place, intro, recordNote, expression keys, dialogId (piece 3), oncePerRun; authored in a world_source.json "premades" section, generated into Assets/Data/World/Premades (owned, pruned).
- C16 = A. Move the premade roll to Seeds.ForLegendary; a legendary field on ForcedCaseSlot for story beats; skip the random roll on planned violator slots and exclude forced premades; fix the violator-slot loss noted by piece 2.
- C17 = B. Premades may be authored liars (honest by default) through Papers and Answer tells only (Appearance excluded for whole-drawn premades); supersede piece-2 E2 explicitly.
- C18 = A. Premade art = 4 whole aligned expression images (neutral, happy, angry, worried); the presenter swaps them using piece 3's per-line expression field if it exists (else add an optional field); in the Visitor window a premade is one clickable region carrying the claim's Culture value.
- C19 → W2 = A: no flow change — the booth figure appears at ShowActiveCase and clears at HandleDecision; the presenter keeps Enter/Leave hooks for the future booth rework (walk-in, speech bubbles, idle animation deferred — W12 = A).
- C20 = A. Passport photo: Passport only (DocumentTemplateSO.showsPhoto), a 4:5 non-clickable crop of the same stack; W11 = B: photo placement defined so final document art drops in.
- W1: character art contract = Saleh's latest direction (simpler, Restory-like, neutral lighting); write a TRACKED art contract doc in the worktree (docs/CHARACTER_ART_CONTRACT.md) that the final ChatGPT brief will follow; don't edit Codex's untracked docs.
- W3 = B/C (designer picks): a clicked garment shows a label (and icon if cheap) matched against the Costume Guide rows.
- W6 = A: tell frequency shares the budget (no extra always-on visual leak).
- W7 = A: the role (Soldier, Scientist...) does not change the look (civilian dress); note the banner mismatch.
- W8 = B/C (designer picks): a premade is "met" when presented; a premade cut off by closing time is not consumed; the DayOrchestrator unreached-content warning learns about premades.

## AMENDMENT A1 (Claude, after the spec review, 2026-09-24) — apply to the spec before planning
The drafted starter cast (8 premades) is all men because it only used figures the place "moments" name. Relax C14's sourcing to: real, long-dead, non-ruler people who lived in the place and era (born inside the place's birth-year range), and rebalance the starter cast to about half women, max 10 premades. Verified candidates on days 1-3 places (check each birth year against the generated place range and that no day-3 rule forbids the claim):
- Aspasia (Periclean Athens, born c. 470 BCE; range -500..-448)
- Ban Zhao (Eastern Han Luoyang, historian, born 45 CE; range 35..87)
- Arib al-Ma'muniyya (Abbasid Baghdad, singer-poet, born 797; range 760..812)
- Alessandra Macinghi Strozzi (Florentine Republic, letter writer, born 1407; range 1369..1421)
- Aemilia Lanyer (Elizabethan England, poet, born 1569; range 1530..1582)
- Cecilia Gallerani (Sforza Milan, born 1473; range 1425..1477) or Caritas Pirckheimer (Renaissance Nuremberg, abbess, born 1467; range 1455..1507)
Keep Senenmut (day-1 story slot) and the Socrates impostor (day-2 story slot); keep at most three more men (e.g. al-Khwarizmi, Cai Lun, Gutenberg or Leonardo). Avoid Izumo no Okuni / anyone claiming Tokugawa Edo on day 3 (forbidden by Rule_NoEarlyModernJapan) unless deliberately authored as a violator. Update the spec's cast table, day pools, notes/intros (ASCII, length caps), and the "all male" remark.

## AMENDMENT A2 (Claude, 2026-09-24) — the office is now Codex's hybrid 3D scene
Before piece 4 is planned, an "art sync" step merges Codex's art (origin/art) and makes the builder existing-wins (SCRATCH/artsync_decisions.md). Piece 4 must follow artsync D6: the booth traveller is a billboard figure parented to an anchor HybridOffice/Booth/TravellerAnchor (world-height knob ~1.7 m in an SO, unlit, SortingGroup), not the old 2D Traveller at (0,-3.3,2); the builder creates the anchor/figure only if missing (existing-wins). The Visitor window and passport photo are desktop UI and unaffected, but must not destroy/recolour Codex's UI skins (artsync D4).
