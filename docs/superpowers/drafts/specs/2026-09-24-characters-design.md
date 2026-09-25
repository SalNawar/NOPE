# Characters: design (piece 4)

*2026-09-24 · decisions made by Claude under Saleh's instruction "go with all pieces, don't stop" (2026-09-24), open to his review · branch `feat/characters` (stacked on `feat/dialog-questions`, which is stacked on `feat/identity-lies`)*

Travellers get a body. A generated traveller is drawn from stacked layers (body, head, hair, facial hair, outfit, headwear, accessory) chosen from the place they **claim**; skin and hair colour follow the claim and are never tells. A liar's disguise can now leak through their dress: one garment, the signature item of their true home, worn over the claimed look. The player sees the traveller in a new desktop **Visitor** window, clicks a garment and compares it with a new reference book, the **Costume Guide**, exactly like a paper field or a spoken answer. The passport gains a photo cropped from the same figure. Premade characters (named, whole-drawn, with four expressions) join the queue on story slots or by chance; one of them is an authored liar. Everything runs on generated placeholder art until the ChatGPT art lands, and final art drops in by file name with no code change.

## Amendments from piece 7 (2026-09-25), read first

Piece 7 (the physical desk and the basic traveller wheel, `docs/superpowers/specs/2026-09-25-physical-desk-design.md`) lands before this piece and removes premises this draft relies on. Piece 4 re-decides the rows below before its plan; this draft's own lines are otherwise not edited.

- **The booth figure is clickable; garments are inspected through the traveller wheel** (C4 :27, §1.2 :100-106, R12 :67, R14 :69, C8 :31 (its Visitor-window use), C18 :41 ("In the Visitor window a premade is one clickable region"), §2.9 `TravellerPortraitView` as the Visitor window :427-437, §2.11 the Visitor fields and `ShowRich` opening :469-481, §2.12 the Visitor window block :500-509, §3.3 the FEATURES :35/:827 lines :819 and :827, §6 :926-927, §7 UI crowding :946):
  - C4's premise ("the desktop canvas and the booth are never visible together") is false: the desktop is drawn live on the CRT in the booth view, and clicking the booth figure opens the traveller wheel (piece-7 K20).
  - Garments are inspected through the wheel ("Look >", piece-8 content once this piece exists): each visible garment is a choice, and its observation becomes a compare-clickable transcript row carrying `EvidenceKind.Appearance` (the item's label as text, the Culture value as evidence).
  - The Visitor window, its auto-open (R12) and `VisitorReachable` (R14) are dropped; Appearance tells are gated by `InterviewReachable`.
  - `TravellerPortraitView` stays only for the passport photo.
- **`TravellerView` exists** (C19 :42, §2.9 `TravellerView` :421-425, §2.10 :441 and :457-460): piece 7 created `Assets/Scripts/Characters/TravellerView.cs` with `Show()`/`Clear()`, `figure` and `Anchor`; this piece extends it (`Show(look, art)`, the layer renderers) instead of creating it. `GameManager.travellerView` exists; its `Show`/`Clear` calls sit in `GameManager.SetTravellerAtDesk`.
- **The builder keeps the anchor and the hit zone** (R11 :66, §2.12 `BuildTraveller` :493-499): when it replaces the placeholder with layer children it must keep `Traveller/Anchor` and `Traveller/TravellerHitZone`, and refit the hit zone to the figure (a child with its own hidden renderer: a collider on the `SortingGroup` root would report order 0).
- **Speech bubbles** (W12 :50, §4 :845): delivered, minimal, by piece 7 (one `OverlayCallout`, shown by `TravellerWheel.Say`); polish goes to piece 8; walk-in and walk-out stay with the booth rework.
- **"Clickable booth figure"** (§4 :849, not scheduled): delivered by piece 7.
- **The passport photo** (C20 and R13, :43 and :68) also fills the physical paper's reserved `DeskDocument.photoSlot`.
- **Both pieces edit `DocumentTemplateSO`** (`handOver` in piece 7, `showsPhoto` here), and **`ContentLibraryValidator.TravellerBlueprints` is now public** (piece 7), which R15 (`blueprintOverride` removed) edits: the plan re-reads both.
- **Line numbers and paths:** the builder anchors this draft cites are stale again (piece 7 adds `Assets/Editor/OfficeSceneUIBuilder.Desk.cs`), as are the `DocumentWindowController` photo layout (the window stays) and the menu-capacity wording ("intercom" is now "the traveller wheel").

Line numbers refer to `efe385d` on `feat/identity-lies` (piece 2, Tasks 1–12 committed). Piece 3 (`docs/superpowers/specs/2026-09-24-dialog-questions-design.md`, committed on `feat/dialog-questions` at `fbf4905`, under review, not yet implemented) lands between that commit and this piece; where this spec names piece-3 code it uses that spec's names:
- `TellChannel { Papers, Answer }`, `Lies.Plan(..., papers, answerTellCategories, channels, facts, bookCategories, rng)`, `LiePlan.ChannelOf`/`TellValue`;
- `EvidenceKind.Answer`, `CompareEvidence.ForAnswer`, `Discrepancy.source`, `DiscrepancyLog.Prove`/`Add`, `ClueLabels.Report`;
- `DayPlanSO.tellChannels`/`TellChannels`, `world_source.json` `days[].channels`;
- `ScriptLine`, `ScriptChoice`, `AuthoredDialog`, `DialogLine`, `DialogRunner`, `InterviewScript.Build`, `DialogChecks.MenuProblems`, `InterviewDay` (`AskableCategories`, `AnswerTellCategories`, `OfferedDialogs`);
- `Interview.Opener`/`WorstCaseLength` (Domain `Interview.cs`), `InterviewLines.maxLineChars`/`menuCapacity`, the generator's ASCII check for authored text (piece-3 R18);
- `CaseFactory.GenerateDayCases(plan, state, daySeed, askable, answerTellCategories)`, `GameManager.BuildInterviewDay`;
- `InvestigationUIController.StartInterview`/`Choose`/`InterviewReachable`, `PagedRowsWindow`, the run-flag grammar `FlagKeys.TriggerFired`/`DialogDone` (Domain `Gates.cs`; `TimelineKeys` stays as it is), `Seeds.ForDialog`.

The plan re-reads every anchored file after piece 3 is implemented; the implemented code wins over both specs. Every statement below about existing code was checked against the worktree at `efe385d` (the scripts are unchanged at `fbf4905`; `15302e1` only regenerated the day plans and re-saved the blueprint).

## 0. Decisions

Claude made these under Saleh's instruction "go with all pieces, don't stop" (2026-09-24), from the piece-4 code analysis (session scratchpad `piece4_map.json`: its synthesis decisions in order = C1–C19 plus C20, its completeness check's extra decisions = W1–W12, and its list of missing items). They bind this piece and are open to Saleh's review. The R rows under the table are the details this spec settles where a decision left them open, the refinements it makes to a decision (marked "refines"), and one deviation (R9, marked "deviates", for Saleh's review).

| Id | Decision | Rationale |
|---|---|---|
| C1 | **Generated travellers are layered; premades are drawn whole.** Garment slots: outfit, hair (with an optional hair-back part, review #8), facial hair, headwear, accessory, per country × era × gender, over a shared body and head. | Saleh: "a base body, hair, outfits from each era, and accessories male and female ... pull from these sprites making sure it makes sense unless the character is lying". The brief (`CHARACTER_ART_BRIEF.md:13-15`). |
| C2 | **1024×1536 canvas for every layer; placeholder-first; skin and hair colour come from the claim and are never tells.** | Locked in the brief (`:15-17`). Art is the last step. |
| C3 | **A dress tell is a third tell channel, `Appearance`, on `ClueCategory.Culture`.** It shares piece 2's `tellCount` budget and piece 3's one-channel-per-category rule; it is enabled per day by `days[].channels` (day 1 Papers; day 2 + Answer; day 3 and later + Appearance). | Saleh: a liar "could dress like they are from japan". One budget, one option pool (piece 3 X1), so no second lie system. |
| C4 | **A desktop Visitor window shows the whole figure; its garments are compare-clickable.** It opens when the traveller is presented. The passport photo is a crop of the same stack. The booth figure is look-only (no `Clickable`, so `HoverHighlighter` is unchanged). | The desktop canvas and the booth are never visible together (`OfficeViewController.cs:88-93`), and `CompareController.Select` takes a uGUI `Image` (`CompareController.cs:62`). |
| C5 + W4 | **`ClueCategory.Culture` (value 6, unused today) carries dress.** One Culture fact per place names its signature item(s); the Costume Guide book (`RefBook_Culture`) lists them. Storage: R1. | No generator, `FactTable` or proof-logic change; ClaimMismatch and ForeignOrigin work as for any book category (`DiscrepancyLog.cs:198-246`). |
| C6 | **Append `EvidenceKind.Appearance`.** The statement side of piece 3's `Prove` accepts it; the Summary says "traveller wears". | Reusing `DocumentField` would print "papers show" and collide with `LiePlan.ApplyTo`. |
| C7 + W5 | **Only the true home's signature item for the traveller's gender may leak.** It must be an item (never an absence), marked leakable, visible over the disguise, not an outfit, not labelled like the disguise's own item in that slot, and not on an authored confusable list. The rule is pure Domain `Looks.CanLeak` with a decision table. | brief_review fixes 3, 7, 16, 22, 27, 32, 33, 51, 65 and issue 11; a leaked full outfit reads as "not disguised". |
| C8 | **Layered rendering.** Booth: a `SortingGroup` root with one `SpriteRenderer` per layer. Desktop: one uGUI Image-stack component, used by the Visitor window and by the passport photo. | Cheap swaps, per-garment click targets, and the photo is a crop (anchors) of the same stack. |
| C9 | **Processed file names are a Domain grammar, `LookKeys`**; facial hair, headwear and accessory are optional; hair colours are baked variants picked by key; sprites load lazily by key (`Resources`, no Addressables). | Loading every layer through a catalog SO would be about 578 MiB (analysis). |
| C10 | **Placeholders are colour-coded (country hue, era shade, slot shape) and never committed.** R9 **deviates** from this decision's editor tool (for Saleh's review): placeholders are drawn at runtime only, and no editor tool writes them. | Clothing tells must be playable before art exists; git LFS must not fill with generated PNGs. |
| C11 | **Wardrobe data lives in `world_source.json`**, generator-written and validated; imported once from the costume research by a hand-checked script (R23). | `world_source.json` is the hand-maintained source from piece 3 on (piece 3 X10). |
| C12 | **A per-traveller look stream, `Seeds.ForLooks`, with a fixed draw order** (R7); age bands and the grey-hair age are knobs in `world_source.json`. | New concern, new salted stream (HOUSE_RULES). |
| C13 + W10 | **Baked hair and facial-hair colour variants; skin and hair weights per place, with a per-country default.** Starter values are for Saleh's review. | No shader; Modern places differ from their lineage. |
| C14 | **Premades: real, long-dead, non-ruler figures of their place's moment** (fitting its birth years) for pre-20th-century places; originals inspired by real people for 20th-century places. Content only. A starter set of eight (R19), listed for Saleh's review. | The moments already name such figures; rulers are Politics facts and conflict with the civilian dress rules. |
| C15 | **Extend `LegendarySO` (type and script GUID kept)**; premades are authored in a `world_source.json` `premades` section and generated into an owned `Assets/Data/World/Premades` folder. Refined by R15. | `CaseFactory`, `ShiftScoring` and the validator already route `LegendarySO`. |
| C16 | **The premade roll moves to `Seeds.ForLegendary`; `ForcedCaseSlot` gains a premade (its forced blueprint stays, now authored in `world_source.json`, R20); the random roll is skipped on planned violator slots; forced premades are excluded from the random roll.** This fixes the violator loss handed over by piece 2 (`CaseFactory.cs:156-159`). | No premade is authored yet, so moving the roll changes no current seed. |
| C17 | **Premades may be authored liars (honest by default), through Papers and Answer tells only.** Supersedes piece-2 E2. | Reuses `Lies.Plan` with one extra input; a whole image cannot carry a generated leak. |
| C18 | **Premade art is four aligned whole images (neutral, happy, angry, worried).** Piece 3's dialog lines gain an optional expression that swaps them. In the Visitor window a premade is one clickable region carrying the claim's Culture value. | Piece 3 has no expression field, so this piece adds it (R18). |
| C19 + W2 | **No flow change:** the booth figure appears at `ShowActiveCase` and clears at the decision; `READY` is unchanged. `TravellerView.Show`/`Clear` are the enter/leave seam for the booth rework. | Matches "empty before READY" (`OFFICE_RESET_BRIEF.md:14`, `SCENE_CONTRACT.md:10`) and FEATURES :23. |
| C20 + W11 | **Passport photo: Passport only (`DocumentTemplateSO.showsPhoto`), a 4:5 non-clickable crop of the same stack, placed top-left like the incoming passport stock art.** | Today both documents show a grey ~103×97 box (`OfficeSceneUIBuilder.cs:342`). The stock art's photo window is 4:5 at the top left. |
| W1 | **A tracked art contract, `docs/CHARACTER_ART_CONTRACT.md`**, written in the worktree; it follows Saleh's latest style direction (simpler, Restory-like, neutral lighting) and supersedes the older character contracts (R26). | Codex's `ART_ASSET_LIST.md`/`PRODUCTION_PLAN.md` still describe 240×440 anime trios. |
| W3 | **B: a clicked garment shows its own item name; proof is by pairing it with a Costume Guide row** (R2). Icons are not cheap (book rows have no image slot and a full-canvas item is unreadable at row size): deferred. | Avoids solving the dress tell by reading text alone. |
| W6 | **A: dress tells share the budget**; no extra always-on visual leak. | One lie system. |
| W7 | **A: the role (Soldier, Scientist ...) never changes the look.** | Civilian dress only (brief Block A). The "(Soldier)" banner beside civilian dress is noted in §7. |
| W8 | **B: a premade is "met" when presented; one cut off by closing time is not consumed**; the unreached-content warning names forced premades. | Rescheduling (C) is not needed with random pools. |
| W9 | **B, taken further by R9:** placeholders are never written to disk; a runtime fallback draws any key without final art. | Fresh clones run; LFS holds only final art. |
| W12 | **A: static figures.** Walk-in/out, idle frames and speech bubbles belong to the booth rework. | The booth is being rebuilt as a hybrid 3D office in the main checkout. |

### Refinements settled by this spec

| Id | Refinement | Rationale |
|---|---|---|
| R1 | **The Culture fact is derived, never authored.** Each place's wardrobe marks one signature slot per gender; the generator writes the place's Culture fact as `Looks.CultureValue(wardrobe)`: `"{men's signature label} / {women's signature label}"`, or the one label when both match (`DiscrepancyLog.ValuesMatch`). It must be at most `FactTable.MaxValueLength` (28, new: the one fact-width cap, which piece 5 extends to every fact) characters, ASCII, and unique across all places. `world_source.json` `facts[]` may not contain Culture. **Labels are unambiguous per gender** (`Looks.LabelProblems`): a place's signature label for a gender is used by no other place's signature for that gender, and no item of another place (any slot, same gender) carries it. No label contains "/". | One source of truth: the evidence string and the art's signature slot cannot drift apart (the analysis's "content drift" risk). 28 is the existing fact cap (every fact value today is ≤ 28 characters). Uniqueness across all 40 places keeps piece-2 R13 true on every possible day. The player reads one gender's item label, not the whole Culture value: a shared label would make an honest garment look foreign, or a leaked one look native while the proof says otherwise. |
| R2 | **What a clicked garment shows (W3).** The compare bar shows `Visitor · {slot}` and the item's own label ("top hat"); the typed evidence carries the source place's Culture value ("top hat / poke bonnet"). MATCH/MISMATCH compares each side's `CompareEvidence.MatchValue(shown)` (Domain, tested: the evidence value when the side carries typed evidence, else the shown text) through `DiscrepancyLog.ValuesMatch` (made public), which replaces `CompareController`'s private copy (`CompareController.cs:140-144`). | A garment and a book row then match exactly when the garment belongs to that place. Every existing row's displayed value equals its evidence value, so nothing else changes. The rule sits in Domain, so the compare bar keeps no untested decision. The private copy was a zero-re-implementation defect on a line this piece edits. |
| R3 | **Every visible garment is clickable, outfit included**; body and head never are. The garment layers are every `LookLayer` except `Body` and `Head` (`Looks.IsGarmentLayer`), so the hair back and a premade's `Whole` layer are clickable. An honest garment carries its source's Culture value, so it MATCHes its own place's row. | The number of clickable garments never reveals a liar. |
| R4 | **Coverage.** A garment named in another worn item's `covers` list is not drawn and not clickable (a turban hides the hair). `CanLeak` refuses a leak that a remaining disguise item would cover. | brief_review issue 7 (hair "hidden under" headwear); a hidden leak is unprovable by eye. |
| R5 | **The hair-back part belongs to the hair item** (`back` flag). It renders under the body, uses the hair's colour, and clicks as the hair garment. **Colour follows the item**: hair and hair back are coloured unless the hair item is a wig; facial hair is always coloured (the facial-hair item can never be a wig), whatever the hair item is. | brief_review issue 8, without a sixth garment slot. A wig (claimed or leaked) must not strip the colour from the traveller's own beard, and every key `Compose` emits must be one `LookKeys.Required` lists. |
| R6 | **The Appearance option.** For each home, after piece 3's Papers and Answer options, there is at most one Appearance option: Culture, when the day allows Appearance, `Forgery.IsProvableTell(Culture, ...)` holds (a Culture book, differing values, a home value unique today) and the candidate's new `HomeCandidate.AppearanceLeakable` is true. The leaked slot follows from data (the home's signature slot for the gender), so no draw is added. | Keeps piece 3's draw shape (roll, home, one per tell, birth year). Days without Appearance draw exactly as piece 3. |
| R7 | **Look-stream draw order:** a gender draw only when the gender is Unknown (one `Value`), then skin tone (one weighted pick), face (one `Range` over the age band's faces, even when the band has one face), hair colour (one weighted pick). At or above `greyFromAge` the colour becomes grey after the draw. Premades and travellers with no wardrobe draw nothing. | A fixed count per traveller keeps the stream contract simple to test. |
| R8 | **Age** is the claimed place's `year` minus the **cover** birth year, with no year 0 (`BirthDates.TryAgeAt`, new). `NationEraProfileSO.year` returns (the piece-1 review dropped it because nothing read it). An unreadable date uses the youngest band. | The face follows the registered identity, so the photo agrees with the records. |
| R9 (**deviates from C10**, takes W9 further; for Saleh's review) | **Placeholders are runtime-only.** When a key has no final art, `CharacterArt` draws it with the pure `LayerPlaceholder` (TimeDesk.Visuals, tested) and keeps it in memory. Nothing is written to disk, nothing reaches LFS, and there is **no editor placeholder tool**, although C10 asked for one (C10 left only the storage policy to this spec). The only builder helper this needs is the polygon test, so `InPolygon` moves from the builder to `TimeDesk.Visuals` `PixelShapes` and the builder calls it. If Saleh wants the tool back, it is a thin menu that calls `LayerPlaceholder` for every missing key the validator's art report lists and writes into an ignored folder; nothing else changes. | One mechanism serves the editor, a fresh clone and a build; an editor tool would be a second path to the same pixels with no player-facing use (the art team works from the art contract's silhouettes and the validator's list of missing names). The builder's PNG helpers (`WritePlaceholderPng`, `EnsureOfficeShape`) are not needed, so there is nothing to extract or copy. |
| R10 | **Final art import.** Files live at `Assets/Art/Characters/Resources/Characters/{key}.png`. An `AssetPostprocessor` makes every texture under `Assets/Art/Characters/` a Single, FullRect sprite whose PPU is its own pixel height (the canvas is 1 world unit tall at any resolution), pivot at the feet (`LookCanvas.FeetPivotY`), readable (alpha hit tests), no mipmaps, no crunch, max size 2048. `CharacterArt` loads by key with `Resources.Load` and `Retain` keeps only the current traveller's textures. | Placeholders (256×384) and final art (1024×1536) are then interchangeable. About 8 textures stay loaded. |
| R11 | **Booth placement.** The `Traveller` object becomes a `SortingGroup` (order −20, between the partitions at −50 and the desk at −10) with one child renderer per layer, feet at (0, −3.3, 2), uniform scale 5.4 (the canvas height in world units): feet hidden behind the desk strip (−4.05..−3.15), head top at about +1.0. These are builder layout constants, redone in the booth rework. | The placeholder booth is small (back wall 4×2.4 units); the hybrid office will replace it. |
| R12 | **The Visitor window opens when the traveller is presented,** right after "every new case closes all open windows". It is a stated exception to FEATURES :35; a desktop icon reopens it. It sits at anchored (−480, 60), 300×470, clear of the book shelf, the claim strip, the compare bar and piece 3's transcript. | The traveller is otherwise invisible while the player works (analysis). The critique's "like the Scanner" was wrong: the Scanner opens on the first find, so this is an exception, not a reuse. |
| R13 | **Photo layout.** The PhotoBox is a 4:5 box anchored at the scan page's top left (anchors (0.03, 0.57)–(0.03, 0.97), width from an `AspectRatioFitter`, pivot (0, 1)) with a `RectMask2D`; inside it the portrait stack is anchored with `LookCanvas.PhotoAnchorMin`/`Max`, so the crop (362, 215)–(662, 590) fills the box with no runtime maths. Rows on a photo document's first page get 120 px more left padding than the builder's (6 px), which is kept on every other page. | Matches the incoming `*_passport_stock.png` window (4:5, top left) so final document art drops in. |
| R14 | **Visitor wiring gates the Appearance channel** (piece-3 R12 pattern). `InvestigationUIController.VisitorReachable` is true in the text fallback and, in the rich desk, only when the Visitor window's view and its chrome are both wired (piece-3 F10: `ShowRich` opens the chrome). When it is false no dress tell is generated that day, and the rich desk logs one warning naming the builder. | A scene never rebuilt would otherwise generate liars whose only tell cannot be seen. |
| R15 (refines C15) | **Premade fields.** `displayName` stays and is the record name (Citizen Records key, banner, papers); no separate `recordName`. Expression art keys derive from `id` (`premade_{id}_{expression}`) and are not stored. `blueprintOverride` is removed (the generator would always leave it null, so its branch would be dead); its readers are `CaseFactory.cs:170` and piece 3's two menu checks (generator and validator), which then count only the day plans' blueprints (§2.13). `minDay`/`maxDay` are removed: the generator-owned day pools (R20) already say on which days a premade can roll, and the validator had to reconcile the two (`ContentLibraryValidator.cs:291-316, 355-357`). New: `id`, `birthDate`, `gender`, `truePlace`, `introLine`, `recordNote`, `dialogId`, `oncePerRun` (authored as `repeatable`, R29). | One name, one key grammar and one day list; no dead fields. |
| R16 | **Premade scheduling rules live in Domain `Premades`** (decision tables): a forced, unmet premade takes its slot; a met forced premade leaves an ordinary traveller; a planned violator slot never rolls; otherwise the slot rolls among rollable premades (in the day's pool, not met, name free). Forced premades' names are reserved before slot 1, which is also what keeps them out of the day's roll (their names are taken); a premade name may not appear in any place's name list, so the reservation itself never shifts a generated name (a premade slot still leaves one pool name unused, §1.5). A premade is forced at most once per day. Forced-premade slots are excluded from violator placement (`ViolatorSlots.Pick` gains an exclusion set; with none it is identical to today). The met flag, Domain `FlagKeys.PremadeMet(id)` = `premade:{id}:met` (piece 3's one home for run-flag names, `Gates.cs`), is set when the premade is presented. | Critique items "name roster", "met guard timing", "forced premade slot plumbing" and "testability". |
| R17 | **A lying premade** calls `Lies.Plan` with liar chance 1 and a one-entry candidate list, its `truePlace`, and the same `answerTellCategories` as every other traveller, so the existing rules pick the tells (Papers and Answer only; Appearance is removed from its channels). The true place must be in today's world, or the premade stays honest with a warning. `Lies.MayLie`'s first parameter becomes "an honest premade". | No second lie path; `FactTable` holds only today's places (`ContentLibrarySO.cs:106-120`, `BuildFactTable` over `TodaysProfiles`). |
| R18 | **Premade dialog and expressions.** `LegendarySO.dialogId` names a piece-3 dialog that is offered only while that premade is at the desk. Piece-3 `ScriptLine` gains `expression`, carried to `DialogLine.Expression` for node lines and choice lines alike; the last traveller line with an expression swaps the premade's whole image in the Visitor window and, through one event, in the booth. Generated travellers ignore expressions (one face each). Premade-bound dialogs count as **one** hub entry in piece 3's menu-capacity check (at most one premade is at the desk), and unbound dialogs count as before. | The piece-3 handoff (conversations, portraits, expressions) is served with piece 3's runner and no second dialog path. Piece-3 R25 left the hub count for piece 4 to revisit; counting every premade dialog at once would reject valid content as premades gain conversations. |
| R19 | **Starter content.** Eight premades (§2.14): Senenmut forced on day 1 slot 3 (the first half of day 1's queue, which has no rules, so no violator is planned); Socrates, an authored impostor from Republican Rome, forced on day 2 slot 6 (the second half of the queue, so violator placement is untouched); the rest in day 2–3 random pools at the existing 5% chance. One premade dialog (Senenmut) exercises expressions. | Exercises every premade path on days 1–3; every non-premade slot keeps piece 3's claim, date, role and violator status, and on days 1–2 its answers and lies (names and day 3: §1.5). |
| R20 | **Generate World owns premades and forced slots**: `Assets/Data/World/Premades` (owned, pruned), the library's `legendaries` array, each day plan's `availableLegendaries`, `forcedCases` and `legendaryBaseChance`. Each `days[].forced` entry names a premade, a case blueprint (an asset path, like `content.blueprint`) or both, so the existing forced-blueprint feature (`ForcedCaseSlot.caseBlueprint`, `TryGetForcedCase`, FEATURES :70) keeps a content path; the generator writes both fields. | Supersedes FEATURES :95 "never removes hand-authored legendaries": one content path. Writing `forcedCases` from premades alone would wipe every forced blueprint on each run and leave `TryGetForcedCase`, `CaseFactory.cs:151` and the orchestrator's forced-case warning unreachable. |
| R21 | **Weights.** `countries[].looks` is the default and `places[].looks` overrides it list by list (a non-empty `skin` or `hair` replaces the country's, R29); the generator writes the effective table onto the place. Skin weights are 5 numbers (tones 1–5); hair weights name black, brown, blond or red. Grey appears only through age. | One value on the asset; a sensitive table authored in one reviewed place. |
| R22 | **`LookRules`** (face bands, grey age, the whole-figure label, confusable pairs) is a Domain type on `ContentLibrarySO`, written by the generator from a top-level `looks` section (the piece-3 `InterviewLines` precedent). | Knobs in data, readable by Domain code without projection. |
| R23 | **Wardrobe import** is a one-off scratchpad script, not committed: it reads `costume_research.json` and `brief_review.json` from the main checkout (read only), applies the 68 fixes, resolves the two conflicts in favour of the later practicality issues (Iraq Industrial women: abaya body in the outfit, head part in headwear, issue 6; Egypt Industrial tarha over the shoulders and down the sides, issue 8), and emits a skeleton that is completed by hand (short labels, signature slots, flags). | `world_source.json` is hand-maintained afterwards, so a re-import path would be a second source. |
| R24 | **Text fallback** prints a "TRAVELLER'S DRESS" block. | The fallback stays informative; its evidence gate is off anyway (`GameManager.cs:505-507`). |
| R25 | **Copy**: citation, scanner idle hint and the `NoPossibleLie` warning mention dress; `ClueLabels.Report(Culture)` is "DRESS" (§2.16). | Player-facing text must stay true. |
| R26 | **Art contract and old placeholder.** `docs/CHARACTER_ART_CONTRACT.md` supersedes `ART_ASSET_LIST.md` §D (visitor trios, legendary pairs), its Tier-1 `traveller.png`, and `PRODUCTION_PLAN.md`'s character direction. The builder stops using `Assets/Art/Office/Placeholder/traveller.png`, but the file (+ meta) **stays** until the booth rework: the tracked `Assets/_Recovery/0.unity` and Codex's uncommitted `OfficeScene.unity` in the main checkout both reference its GUID (`04b51760ee3e85041bb372382ecf3eff`). The art contract marks it retired. | Prevents art delivered to a dead contract, without leaving a dangling reference in a committed scene or in Codex's scene when the branches meet. |
| R27 | **Future contract (piece 5).** A Future place is an ordinary place with a standard `wardrobe` block and its own keys (`{slot}_{g}_{country}_future`); no per-item art override and no extra `CanLeak` row are needed, because at most one Future place is in a day's world (piece-5 R1) and distinct places never share a key. If the art team wants one drawing for several Future places, the processing step writes it under each place's names. A Future signature may be the outfit, so a Future home never leaks dress; piece 4's "signature is the outfit" validator warning is unconditional, and piece 5 exempts Future places with its `isFuture` flag. | Defines only what piece 4 owns; a same-art leak row would be unreachable. Piece 4 has no Future marker to exempt by. |
| R28 | **`SerializedArrays`**: the generator's private `SetArray`/`DropMissing` move to a shared editor helper that the builder also uses (it wires arrays of layer images for the first time). Its error messages use a neutral prefix: "[SerializedArrays] '{object}' has no serialized field '{prop}'." | Zero re-implementation; the old "[WorldContentGenerator]" prefix would name the wrong tool when the builder calls it. |
| R29 | **Source defaults never decide behaviour** (piece-3 R27). Every new `world_source.json` key reads safely when missing (0, false or empty): a premade's run memory is `repeatable` (missing = false = once per run; the generator writes `LegendarySO.oncePerRun = !repeatable`); an impact's `skipNationScore` (missing = false = the delta also moves the nation's score; the generator writes `alsoAffectsNationScore = !skipNationScore`) and an impact with both deltas 0 is rejected; `premadeChance` must be above 0 on a day whose `premades` pool is non-empty (a missing value reads 0 and is rejected, the `days[].tells` precedent); a forced entry's `slot` must be at least 1; `looks.greyFromAge`, `wholeFigureLabel`, every face band's faces, every signature, a premade's gender, place, birth date and archetype are required. A place's `looks` override applies per list (`skin`, `hair`) only when that list is non-empty. Every starter entry authors its keys explicitly. | Whether `JsonUtility` runs field initialisers on elements of nested arrays then never matters: a missing key never repeats a premade, never silences the random roll and never inverts an impact. |

## 1. Behaviour

### 1.1 What a traveller looks like

- **Every generated traveller is a stack of layers** on the same 1024×1536 canvas, bottom first: hair back, body, outfit, head, facial hair, hair, headwear, accessory.
- **Everything comes from the claimed place**: its outfit, hairstyle, facial hair, headwear and accessory for the traveller's gender (from the name list, piece 2), skin tone and hair colour from the place's weights, a face chosen by age:
  - ages 18–34: face a or b; 35–59: face c; 60 and over: face d, and the hair turns grey;
  - age = the claimed place's year minus the registered (cover) birth year.
- **A worn item can hide another** (a turban hides the hair): the hidden garment is not drawn.
- **Honest travellers and liars without a dress tell** wear the claimed look completely.
- **A liar with a dress tell** wears the claimed look except for one garment: the signature item of their true home for their gender (for example a Victorian top hat over Edo Japanese dress). Skin and hair colour stay the claim's, even when the leaked item is a hairstyle.
- **Premades** are one whole picture with four expressions; the picture changes with what they say in a conversation.
- **Unknown gender** ("Subject #n", a content gap) draws a gender on the look stream and logs a warning; such travellers never carry a dress tell.

### 1.2 Where the player sees the traveller

- **Visitor window** (desktop, new): when the player taps READY and lands on the monitor, the Visitor window is already open, showing the whole figure. It is draggable, can be maximised, and reopens from its desktop icon. It closes when the traveller leaves, like every window.
  - Hovering a garment outlines it (the existing UI hover); clicking selects it (a yellow overlay on that garment) and puts `Visitor · Headwear:  top hat` in the compare bar.
  - A premade is one clickable region, "Period dress".
- **Passport photo**: the Travel Passport's first page shows a 4:5 photo, the head-and-shoulders crop of the same figure. Tall headwear is cut by the frame. It is not clickable. The Transit Permit has no photo.
- **Booth**: after READY the figure stands behind the desk, and it leaves when the decision is made. It is not clickable. The booth is out of view while the player works on the monitor, so the Visitor window is the working view.

### 1.3 Dress tells and the Costume Guide

- **The Costume Guide** is a new reference book on the desktop, like the others. It lists today's places, each with its signature dress: "New Kingdom Egypt (Ancient) — wesekh collar", "Victorian London (Industrial) — top hat / poke bonnet" (men's item / women's item, or one item for both).
- **Proving a dress tell**: click the leaked garment, then a Costume Guide row.
  - Against the claimed place's row: `DRESS INCORRECT — traveller wears: "top hat / poke bonnet"  /  expected: "chonmage / shimada"`.
  - Against the true home's row: `DRESS INCORRECT — traveller wears "top hat / poke bonnet", which belongs to Victorian London (Industrial)`. This names the true home.
  - Any other row, or an honest garment against any row, logs nothing. An honest garment against its own place's row shows MATCH.
- **One discrepancy per category from any source** (piece 3): a second dress proof shows "ALREADY DOCUMENTED".
- **When**: from day 3 (the day plan's tell channels: day 1 Papers; day 2 Papers, Answer; day 3 Papers, Answer, Appearance). The tell count stays 1, so a dress tell replaces a paper or spoken one; on day 3 roughly one liar in ten carries it (measured in §6).
- **Which item**: only the true home's signature item for the liar's gender, and only when it is an item (never "clean-shaven" or "bareheaded"), marked leakable (readable at thumbnail size, not hanging off another layer), not the whole outfit, not covered by the disguise, and not named or drawn like the claimed look's item in that slot (the same label, or an authored confusable pair). Item names are unambiguous: no place wears an item named like another place's signature item for the same gender.
- **Skin and hair colour are never tells.** The role (Soldier, Scientist ...) never changes the dress.

### 1.4 Premade characters

- **Who**: named people from a place's moment, drawn whole: Senenmut, Socrates, Muhammad al-Khwarizmi, Cai Lun, Leonardo da Vinci, Zeami Motokiyo, William Shakespeare, Johannes Gutenberg (§2.14; Saleh reviews the list).
- **When**:
  - story slots: Senenmut is the third traveller of day 1; Socrates is the sixth of day 2;
  - by chance: each other slot of days 2–3 has a 5% chance of a premade from the day's pool;
  - never on a slot planned for a rule violator (the violator always keeps their slot now);
  - once per run: a premade presented to the player never returns. A premade cut off by closing time is not used up, and the log names forced premades the day never reached.
- **What they carry**: their own name, birth date and gender; papers from their claimed place; a Citizen Record with their own note ("Philosopher. Known to question officials at length."); their own opener line when authored ("Priority arrival: Socrates of Athens. He asks more questions than you do."), else "Priority arrival: <name>." (piece 3).
- **Conversations**: a premade can have a conversation in the intercom hub, offered only while they are at the desk. Senenmut talks about his temple, happily and then worriedly, and his picture changes with each line.
- **Lying premades**: premades are honest unless authored otherwise. "Socrates" is an impostor from Republican Rome: his papers or his answers leak one Roman tell (never his dress). Deny him with a logged deviation.
- **Scoring** is unchanged: a correct decision on a premade pays the legendary bonus, a wrong one costs extra stability.

### 1.5 Determinism

- Same run + same day + same unlocked questions + same met-premade flags (and the same Visitor and interview wiring) = same travellers, same looks, same premades.
- **Met flags are a generation input.** Which premades were met on earlier days depends on how far the player got before closing time. A met premade no longer rolls, and a met forced premade leaves an ordinary traveller in its slot. So the met flags can change which slots hold premades, then (through the day's name roster) later names and genders, then day-3 dress eligibility and so day-3 lies. This supersedes piece 3's "purchases and story flags never change the lie draws" for these flags (§3.2).
- **Stream contract**, compared with piece 3 for every seed, with no premade met:
  - every slot that holds no premade keeps its claim, date, role and violator status;
  - its name (and so its gender and opener) can differ only when an earlier slot of the same day holds a premade: the premade uses no name from a place's list, so the day's name roster hands a later traveller from that place a different unused name. Before the first premade slot of a day, names are identical;
  - on days 1 and 2 it also keeps its lies and its answers (a lie never reads the name, and no Appearance option exists yet);
  - on day 3, lies can differ for liars whose home could leak dress (the option pool grows, and dress eligibility reads the gender), and piece-3 answers come from the lie plan (`Interview.Answer` reads `ChannelOf`/`TellValue`), so a changed liar can answer differently. A traveller honest in both runs keeps its answers;
  - a premade slot differs (its identity is authored);
  - violator placement is unchanged: day 1's forced slot 3 is in the first half of its queue, but day 1 has no rules, so no violator is planned; day 2's forced slot 6 is in the second half.
- Look draws have their own stream, so look tuning never changes who travellers are or who lies.

### 1.6 Art

- Until final art exists, every layer is a generated placeholder: a flat shape per layer, garments in the country's hue and the era's shade (so a foreign garment shows as a foreign colour), hair in its colour with a band of the country's hue, skin in five tones, premades as a striped figure with a mouth mark per expression.
- Final art replaces a placeholder by file name (`Assets/Art/Characters/Resources/Characters/{key}.png`) with no code change; the art contract lists every name. The content validator reports how many keys have final art.

### 1.7 Text fallback

The fallback prints, after the agency record and before the interview (piece 3), "— TRAVELLER'S DRESS —" with one line per garment, `    Headwear: top hat (top hat / poke bonnet)`. The "REFERENCE (claimed place)" block gains the Costume Guide line.

### 1.8 Saves

`WorldState` keeps its shape: met premades are flags (`premade:{id}:met`, built only by `FlagKeys.PremadeMet`), so `SaveSystem.SaveVersion` stays 2. Continue after the end-of-shift save replays the day (known gap, fixed by piece 5 H11): a premade met that day is then already flagged, so its slot holds an ordinary traveller in the replay.

## 2. Design

### 2.1 Where things live

- **TimeDesk.Domain** (`Assets/Scripts/Domain`, pure, EditMode-tested):
  - new: `LookData.cs`, `Looks.cs`, `LookKeys.cs`, `Premades.cs`;
  - changed: `Seeds.cs`, `Lies.cs`, `DiscrepancyLog.cs`, `FactTable.cs`, `BirthDates.cs`, `ViolatorSlots.cs`, and piece 3's `ClueLabels.cs`, `Gates.cs` (`FlagKeys`), `Interview.cs` (`Opener`), `InterviewContent.cs`, `Dialog.cs`, `InterviewScript.cs` (`Build`, `DialogChecks.MenuProblems`), `InterviewDay.cs`; docs only: `ClueCategory.cs`, `TravellerGender.cs`.
- **TimeDesk.Visuals** (`Assets/Scripts/Visuals`, engine-free, EditMode-tested): new `LookCanvas.cs`, `PixelShapes.cs`, `PlaceholderPalette.cs`, `LayerPlaceholder.cs`.
- **Assembly-CSharp**:
  - new folder `Assets/Scripts/Characters` (+ meta): `CharacterArt.cs`, `TravellerView.cs`, `TravellerPortraitView.cs`;
  - changed: `CaseInstance.cs`, `CaseFactory.cs`, `LegendarySO.cs`, `DayPlanSO.cs`, `ContentLibrarySO.cs`, `DocumentTemplateSO.cs`, `Timeline/NationEraProfileSO.cs`, `GameManager.cs`, `DayOrchestrator.cs`, `UI/InvestigationUIController.cs`, `UI/DocumentWindowController.cs`, `UI/CompareController.cs`, `Shift/ShiftScoring.cs`; docs only: `DevTools/DevToolsState.cs`.
- **Assembly-CSharp-Editor** (`Assets/Editor`, no asmdef): new `CharacterArtImporter.cs`, `SerializedArrays.cs`; changed `WorldContentGenerator.cs`, `ContentLibraryValidator.cs`, `OfficeSceneUIBuilder.cs`.
- **Content**: `world_source.json`; new `RefBook_Culture.asset`; `DocTemplate_Passport.asset` (`showsPhoto`); generated places, premades and day plans; `ContentLibrary_Main.asset`; `OfficeScene.unity` rebuilt; `traveller.png` kept (R26).
- **Tests**: `Assets/Tests/EditMode` (references TimeDesk.Domain and TimeDesk.Visuals only), so every new test targets those two assemblies.
- **Docs**: this spec, `docs/FEATURES.md`, new `docs/CHARACTER_ART_CONTRACT.md`.

Line endings to preserve (the working-tree column of `git ls-files --eol`; `core.autocrlf` is true, so the index holds LF for most of these):
- **CRLF**: `DiscrepancyLog.cs`, `ClueCategory.cs`, `CaseInstance.cs`, `CaseFactory.cs`, `LegendarySO.cs`, `DayPlanSO.cs`, `ContentLibrarySO.cs`, `DocumentTemplateSO.cs`, `NationEraProfileSO.cs`, `GameManager.cs`, `DayOrchestrator.cs`, `InvestigationUIController.cs`, `DocumentWindowController.cs`, `CompareController.cs`, `ShiftScoring.cs`, `DevToolsState.cs`, `ContentLibraryValidator.cs`, `DocTemplate_Passport.asset`, `DiscrepancyLogTests.cs`, `docs/FEATURES.md`.
- **LF**: `Seeds.cs`, `Lies.cs`, `FactTable.cs`, `BirthDates.cs`, `ViolatorSlots.cs`, `TravellerGender.cs`, `WorldContentGenerator.cs`, `OfficeSceneUIBuilder.cs`, `world_source.json`, the day plans, `ContentLibrary_Main.asset`, `SeedsTests.cs`, `LiesTests.cs`, `BirthDatesTests.cs`, `ViolatorSlotsTests.cs`, piece 3's new files and tests (written LF), and every new file.

### 2.2 Domain: look data (`LookData.cs`, new)

Serializable content types. `NationEraProfileSO` and `ContentLibrarySO` hold them directly (the piece-3 `InterviewLines` precedent), so no projection code exists. Every nested object field is initialised (`= new()`); Unity serialises an absent item as a default object, so **an empty `label` means "no item"**.

| Type | Fields | Notes |
|---|---|---|
| `enum LookSlot` | `Outfit, Hair, FacialHair, Headwear, Accessory` | Garment slots. Serialized: append only. |
| `LookItem` | `string label; bool leakable; bool wig; bool back; List<LookSlot> covers = new();` | `IsPresent => !string.IsNullOrWhiteSpace(label)`. `wig`: drawn in its own colour, no colour variants. `back`: the hair item has a part behind the body (hair only). `covers`: garment slots this item hides. |
| `GenderLook` | `LookSlot signature; LookItem outfit, hair, facialHair, headwear, accessory` (each `= new()`) | `LookItem Item(LookSlot slot)`. |
| `PlaceWardrobe` | `GenderLook male = new(), female = new();` | `GenderLook For(TravellerGender g)`: Male → male, Female → female, Unknown → null. |
| `HairColourWeight` | `string colour; float weight;` | colour ∈ `LookKeys.HairColours` minus grey. |
| `LookWeights` | `float[] skin = new float[LookKeys.SkinTones]; List<HairColourWeight> hair = new();` | The place's effective table (R21). |
| `FaceBand` | `int minAge; List<string> faces = new();` | faces are one-letter tokens. |
| `ConfusablePair` | `string placeA, placeB; LookSlot slot; string gender;` | place ids (`NationEraProfileSO.id`); gender `"m"`, `"f"` or empty for both. |
| `LookRules` | `List<FaceBand> faceBands = new(); int greyFromAge; string wholeFigureLabel; List<ConfusablePair> confusable = new();` | `bool IsConfusable(string placeA, string placeB, LookSlot slot, TravellerGender g)` (unordered pair). |

### 2.3 Domain: the key grammar (`LookKeys.cs`, new)

`public static class LookKeys` is the only place that writes a character file name.

- Tokens: `Male = "m"`, `Female = "f"`; `SkinTones = 5`; `HairColours = { "black", "brown", "blond", "red", "grey" }`, `Grey = "grey"`; `Expressions = { "neutral", "happy", "angry", "worried" }`, `NeutralExpression = "neutral"`.
- `string GenderToken(TravellerGender g)` (Unknown throws `ArgumentException`: callers resolve gender first).
- `bool IsToken(string s)`: non-empty `[a-z0-9]`; ids used in keys (nation, era, premade) must pass, because `_` separates tokens.
- Layer tokens (private, one table): HairBack `hairback`, Outfit `outfit`, FacialHair `facialhair`, Hair `hair`, Headwear `headwear`, Accessory `accessory`.
- Factories, returning a `LookKey` (§2.4) whose `Name` is:
  - `Body(g, skin)`: `body_{g}_skin{N}`;
  - `Head(g, skin, face)`: `head_{g}_skin{N}_face{v}`;
  - `Garment(layer, g, nationId, eraId, colour)`: `{layer}_{g}_{nation}_{era}`, plus `_{colour}` when `colour` is not null (hair and hair back unless the hair item is a wig; facial hair always, R5);
  - `Premade(id, expression, claimNationId, claimEraId)`: `premade_{id}_{expression}` (the claim ids only colour the placeholder).
- Enumerations (the validator's art report and tests):
  - `IEnumerable<string> Required(string nationId, string eraId, PlaceWardrobe w)`: every garment key of both genders; hair and hair back in **every** colour unless the hair item is a wig (then one uncoloured key each), facial hair always in every colour (a leaked hairstyle keeps the traveller's colour, so any colour can meet any hairstyle);
  - `IEnumerable<string> Bases(LookRules rules)`: bodies (2 × 5) and heads (2 × 5 × every face in the bands);
  - `IEnumerable<string> PremadeSet(string premadeId)`: the four expressions.
- Reserved (documented in the art contract, no code): an outfit variant suffix `_v{N}`.

### 2.4 Domain: composing a look (`Looks.cs`, new)

**Types:**
- `enum LookLayer { HairBack, Body, Outfit, Head, FacialHair, Hair, Headwear, Accessory, Whole }`: render order, bottom first. The views' serialized arrays are indexed by it, so a change needs a scene rebuild (doc says so). Garment layers: `Looks.IsGarmentLayer`.
- `readonly struct LookKey`: `Name`, `Layer`, `NationId`, `EraId`, `SkinTone`, `Face`, `HairColour`, `PremadeId`, `Expression`. Built only by `LookKeys`; `CharacterArt` loads by `Name` and colours placeholders from the rest.
- `readonly struct LookPart { LookLayer Layer; LookKey Key; int GarmentIndex; }`: `GarmentIndex` is −1 for body and head.
- `sealed class Garment { LookSlot Slot; string Label; string Value; bool IsTell; }`: `Value` is the source place's Culture fact.
- `sealed class TravellerLook`:
  - `IReadOnlyList<LookPart> Parts` (stack order), `IReadOnlyList<Garment> Garments`, `string PremadeId` (null for generated);
  - `LookPart? PartOn(LookLayer layer)`; `IEnumerable<LookKey> Keys`;
  - `LookKey WholeKey(string expression)`: the premade image for an expression (blank or unknown → neutral); only for premades;
  - `string Describe()`: for the case log, "m skin3 face-b brown; dress tell: Headwear 'top hat'" or "premade socrates".
- `sealed class LookSource { string NationId, EraId, PlaceId; PlaceWardrobe Wardrobe; string CultureValue; }`: one place as the composer sees it.

**`public static class Looks`:**
- `const ClueCategory EvidenceCategory = ClueCategory.Culture`: the one home of "dress evidence is Culture" (read by `Lies`, `CaseFactory`, the views, and the generator's and validator's "Culture is never asked" check).
- `const int MaxLabelLength = 24` (read by the generator and the validator; the Culture value's cap is `FactTable.MaxValueLength`).
- `string SlotLabel(LookSlot s)`: "Outfit", "Hair", "Facial hair", "Headwear", "Accessory".
- `bool IsGarmentLayer(LookLayer layer)`: every layer except `Body` and `Head` (so `HairBack` and `Whole` are garment layers, R3). Callers: the builder's `BuildPortraitStack` (buttons, highlights, raycast) and `TravellerPortraitView.Show`.
- `string CultureValue(PlaceWardrobe w)`: R1. Null when either gender's signature item is missing.
- `List<string> LabelProblems(IReadOnlyList<(string placeId, PlaceWardrobe wardrobe)> places)`: R1's per-gender label rule, one message per clash, each naming both places, the gender and the label (compared with `DiscrepancyLog.ValuesMatch`): two places whose signature labels for a gender match; an item of one place (any slot) whose label matches another place's signature label for the same gender; a label containing "/". Callers: the generator (on the wardrobes it is about to write) and the validator.
- `bool CanLeak(LookSource claim, LookSource home, TravellerGender g, LookRules rules)`, the C7 decision table, in order:
  1. g is Unknown, or either wardrobe is null → false;
  2. the home's signature item for g is missing (an absence) → false;
  3. the signature slot is `Outfit` → false;
  4. the item is not `leakable` → false;
  5. a claim item for g in another slot is present and lists the signature slot in `covers` → false (hidden by the disguise);
  6. the claim's item for g in the signature slot is present and its label matches the leaked item's label (`DiscrepancyLog.ValuesMatch`) → false (the garment would read as the disguise's own; `LabelProblems` keeps generated content from reaching this row, which keeps `CanLeak` right for any data it is given);
  7. `rules.IsConfusable(claim.PlaceId, home.PlaceId, slot, g)` → false;
  8. otherwise true.
- `TravellerLook Compose(LookSource claim, LookSource leakFrom, TravellerGender gender, string coverBirthDate, int claimYear, LookWeights weights, LookRules rules, IRandomSource rng)`:
  1. `claim.Wardrobe == null` → a minimal look (body and head, skin 3, the first face of the first band, gender Male when Unknown), no garments, **no draws**.
  2. **Draws** (R7): gender when Unknown (`rng.Value() < 0.5` → Male); skin `1 + WeightedRandom.Pick(tones 0..4, weights.skin)` (tone 3 and no draw when every weight is 0); `BirthDates.TryAgeAt(coverBirthDate, claimYear, out age)`; band = the last band with `minAge <= age` (the first band when the age is unreadable); face = `band.faces[rng.Range(0, band.faces.Count)]`; colour = `WeightedRandom.Pick(weights.hair)` (brown and no draw when empty); colour = grey when the age is readable and `age >= rules.greyFromAge`.
  3. **Items**: every slot's item comes from `claim.Wardrobe.For(g)`; when `leakFrom` is not null the home's signature slot takes `leakFrom.Wardrobe.For(g)`'s signature item instead (its source becomes `leakFrom`).
  4. **Coverage** (R4): an item whose slot is listed in another present item's `covers` is dropped.
  5. **Parts**, bottom first: hair back (when the hair item has `back`), body, outfit, head, facial hair, hair, headwear, accessory. Garment keys use the item's **source** place; hair and hair back take the colour unless the hair item is a wig (a wig's hair back has no colour either); facial hair always takes the colour, whatever the hair item is (R5).
  6. **Garments**, in `LookSlot` order, one per drawn item: `Label = item.label`, `Value = source.CultureValue`, `IsTell = source == leakFrom`. The hair-back part points at the hair garment.
- `TravellerLook Whole(string premadeId, LookSource claim, LookRules rules)`: one part (`Whole`, `LookKeys.Premade(id, neutral, ...)`), one garment (`Outfit`, `rules.wholeFigureLabel`, `claim.CultureValue`, not a tell), no draws.

### 2.5 Domain: tells, evidence, seeds, dates, violators, premades, dialog

**`Lies.cs`** (LF, piece-3 shape):
- `TellChannel` gains `Appearance`, appended ("the liar's dress; a dress tell is the true home's signature garment").
- `HomeCandidate` gains `public readonly bool AppearanceLeakable;` ("this place's signature garment can leak onto the traveller's claimed look (`Looks.CanLeak`)") as a fifth constructor parameter with default `false`, so every existing call and test compiles unchanged.
- `Lies.Plan` step 2 (piece-3 R1): after the Answer options, when `channels` contains `Appearance` and `place.AppearanceLeakable` and `Forgery.IsProvableTell(Looks.EvidenceCategory, ...)` holds, add the option (Culture, Appearance). Nothing else changes: the tell pick, the one-channel-per-category removal and the values (`facts.Get(home, Culture)`) are piece 3's. `ApplyTo` still rewrites Papers tells only.
- `Lies.MayLie(bool honestPremade, bool claimAllowed, papers)`: the first parameter is renamed from `isLegendary`; the result and the tests' rows are unchanged; the doc says premades are honest unless authored as liars (supersedes piece-2 E2).
- Class docs: "... on the papers, in speech or in dress".

**`DiscrepancyLog.cs`** (CRLF, piece-3 shape):
- `EvidenceKind.Appearance`, appended after `Answer`: "A garment the traveller wears (the Visitor window)".
- `public static CompareEvidence ForAppearance(ClueCategory category, string value, bool isTell)`: `kind = Appearance`, `isAnachronism = isTell`.
- `Prove`: the statement side is `DocumentField`, `Answer` **or `Appearance`**; everything else is piece 3's.
- `Summary`: for source `Appearance` the statement reads `traveller wears: "{v}"` (ClaimMismatch and RecordMismatch) and `traveller wears "{v}", which belongs to {o}` (ForeignOrigin).
- `ValuesMatch` becomes `public` (R2); its doc adds `CompareEvidence.MatchValue`'s caller `CompareController`, `Looks.CultureValue`, `Looks.CanLeak` and `Looks.LabelProblems` as sharers.
- `CompareEvidence` gains `public string MatchValue(string shown) => kind != EvidenceKind.None ? value : shown;`: "The value a comparison matches on: this side's typed evidence value when it carries evidence (a garment shows its item name but matches on its place's Culture value, R2), else the text shown." Caller: `CompareController.Refresh`.
- Class doc: "a statement (document field, answer or worn garment)".

**`FactTable.cs`** (LF): `public const int MaxValueLength = 28;`: "The widest fact value a book row shows. The derived Culture value is checked against it (generator and validator); piece 5 extends the check to every fact value." It replaces the Culture-only cap an earlier draft put in `Looks`, so the book-row width has one home.

**`ClueLabels.cs`** (piece 3): `Report(Culture)` = "DRESS".

**`Gates.cs`** (piece 3), `FlagKeys` gains `public static string PremadeMet(string premadeId) => $"premade:{premadeId}:met";`: "Set when a once-per-run premade is presented; a met premade never rolls again and a forced slot for them holds an ordinary traveller." Callers: `CaseFactory.ResolvePremade`, `GameManager.ShowActiveCase`, `DayOrchestrator.WarnAboutUnreachedContent`.

**`Interview.cs`** (piece 3): `Opener(InterviewLines lines, TravellerGender gender, string legendaryName, string authoredIntro)`: a non-blank `authoredIntro` is returned as it is (a premade's own opener, R15); otherwise piece 3's rules (the legendary template for a non-blank `legendaryName`, else the honorific opener). Caller: `CaseFactory` (piece 3's call gains the fourth argument).

**`InterviewScript.cs`** (piece 3), `DialogChecks.MenuProblems(int questions, bool smallTalk, int maxDocuments, int dialogs, int premadeDialogs, int maxChoices)`: `dialogs` now counts dialogs bound to no premade, and the hub is `maxDocuments` + 1 (ask) + `dialogs` + (1 when `premadeDialogs > 0`), because `InterviewDay.OfferedDialogs` offers at most one premade-bound dialog at a time (R18). The ask-menu count is piece 3's.

**`ClueCategory.cs`** (doc only): `Culture` gets "A place's signature dress: the Costume Guide rows and worn garments (derived from the place's wardrobe)."

**`Seeds.cs`** (LF):
- `LookSalt = 0x4C4F4F4B` ("LOOK"), `ForLooks(int caseSeed) => Mix(caseSeed, LookSalt)`: "one traveller's look draws (gender when unknown, skin, face, hair colour), apart from the case and lie streams so look tuning never changes who travellers are or who lies".
- `LegendarySalt = 0x4C474E44` ("LGND"), `ForLegendary(int caseSeed) => Mix(caseSeed, LegendarySalt)`: "one slot's premade roll and pick, apart from the case stream so authoring a premade never reshuffles a day's travellers".

**`BirthDates.cs`** (LF): `public static bool TryAgeAt(string date, int atYear, out int age)`: false when the date is unreadable or `atYear` is 0; otherwise `age = atYear − year − (year < 0 && atYear > 0 ? 1 : 0)` (no year 0). Class doc adds "ages".

**`ViolatorSlots.cs`** (LF):
- `public static int Window(int queueSize) => queueSize <= 0 ? 0 : (queueSize + 1) / 2;`: "How many leading slots can hold a guaranteed violator (the first half, rounded up)." `Pick` uses it in place of its inline `(queueSize + 1) / 2`; the generator's and validator's forced-slot warning (§2.13) calls it.
- `Pick(int queueSize, int violators, IRandomSource rng, ICollection<int> excludedSlots = null)`. The pool is the first `Window(queueSize)` slots minus the excluded ones, in ascending order; the count is capped by the pool. With no exclusions the pool, the draws and the result are identical to today.

**`Premades.cs`** (new):
- `enum PremadeSlot { Forced, Roll, None }`.
- `PremadeSlot SlotSource(bool forcedHere, bool forcedMet, bool violatorSlot)`: forced and unmet → Forced; forced and met → None; violator slot → None; otherwise Roll.
- `bool IsRollable(bool met, bool nameTaken)`: true when the pooled premade is not met this run and their name is free today. Doc: forced premades' names are reserved before slot 1, which is what keeps them out of the day's roll (no separate "forced today" input).
- `int Roll(float chance, int candidates, bool forcedByCheat, IRandomSource rng)`: −1 with no draw when `candidates <= 0`; unless `forcedByCheat`, one `Value` and −1 when it is not below `chance`; then one `Range(0, candidates)`. This is `CaseFactory.TryRollLegendary`'s rule (`CaseFactory.cs:530-548`), moved.
- `const int MaxNoteLength = 120`: the longest Citizen Records note a premade may carry (the note box, `OfficeSceneUIBuilder.cs:184`, holds about three lines of 60 characters at 15 pt; 120 leaves room). Read by the generator and the validator.

**Dialog (piece-3 files, LF):**
- `ScriptLine` gains `public string expression;` ("optional: a `LookKeys.Expressions` token; a premade's picture changes to it").
- `DialogLine` gains `Expression` and an optional last constructor parameter; `DialogLine.Answer` leaves it null.
- `InterviewScript.Build` copies `ScriptLine.expression` into every authored line it builds: node lines and each `ScriptChoice`'s lines (the desk's label line has none).
- `InterviewDay`: the constructor gains `ICollection<string> premadeDialogIds`; `OfferedDialogs()` becomes `OfferedDialogs(string premadeDialogId)`: today's dialogs minus those completed this shift, minus every premade-bound dialog except `premadeDialogId`.

**`TravellerGender.cs`** (doc only): Unknown is "'Subject #n', or a name on both lists or neither"; premades carry an authored gender.

### 2.6 Visuals (engine-free)

**`LookCanvas`** (static, the art contract's geometry, pinned to `make_guide.py:5-9`):
- `Width = 1024`, `Height = 1536`, `CenterX = 512`; top-left landmarks `HeadTop 260, Chin 424, Shoulders 500, Waist 760, Hips 900, Knees 1170, Feet 1490`; `SafeXMin 120`, `SafeXMax 904`; photo crop `(362, 215)–(662, 590)`.
- Derived: `FeetPivotY = (Height − Feet) / Height` (≈ 0.02995); `PhotoAnchorMin = (−362/300, −946/375)` ≈ (−1.2067, −2.5227) and `PhotoAnchorMax = (662/300, 590/375)` ≈ (2.2067, 1.5733), which place the whole canvas so that the crop fills a unit box; `PhotoAspect = 300/375 = 0.8`.

**`PixelShapes`**: `bool InPolygon((float x, float y)[] polygon, float px, float py)` (moved unchanged from `OfficeSceneUIBuilder.cs:1236-1246`, even-odd rule) and `bool InEllipse(float cx, float cy, float rx, float ry, float px, float py)`.

**`PlaceholderPalette`**: `(byte r, byte g, byte b) FromHsv(float h, float s, float v)`: `h` wraps (its fractional part), `s` and `v` are clamped to 0..1, channels round to the nearest byte. Caller: `CharacterArt` (nation hue, era shade, skin ramp).

**`LayerPlaceholder`**:
- `Width = 256`, `Height = 384` (the canvas at quarter size; not a gameplay knob).
- `enum PlaceholderRegion { HairBehind, Body, Clothes, Head, Beard, HairCap, Hat, Collar, WholeFigure }`: drawing regions from the landmarks (hair behind the shoulders; mannequin torso, arms and legs; clothes from shoulders to knees; head oval; beard below the mouth; hair cap over the head top; hat band above the head top into the headroom; a collar band at the shoulders; a whole figure).
- `enum PlaceholderMark { None, Neutral, Happy, Angry, Worried }` (a mouth line on the head).
- `byte[] Render(PlaceholderRegion region, (byte, byte, byte) fill, (byte, byte, byte) accent, PlaceholderMark mark)`: RGBA32, row 0 = bottom (like `OutlineMask`), transparent outside the region, a 3-px accent border inside it (for `WholeFigure`, accent stripes), the mark drawn in the accent.

### 2.7 Assembly-CSharp: content types

**`NationEraProfileSO`** (CRLF):
- `public int year;`: "The place's moment year (negative = BCE); travellers' ages (their face) are measured against it." (R8)
- `public PlaceWardrobe wardrobe = new();`: "What people of this place wear, per gender, and each gender's signature item (the Costume Guide entry and the only item a disguise can leak)."
- `public LookWeights looks = new();`: "Skin-tone and hair-colour weights of travellers claiming this place (never a tell)."
- Class doc gains "... and of how its travellers look".

**`LegendarySO`** (CRLF), class doc: "A premade character: a named traveller drawn whole, generated from `world_source.json` `premades` by Generate World (`Assets/Data/World/Premades`). The claim is `nation` + `trueEra`." The `CreateAssetMenu` attribute is removed (generated only).
- kept: `displayName` (doc: "The name on papers, in Citizen Records and on the banner"), `trueEra` (doc: "the era the premade claims"), `archetype`, `nation` (doc: "the nation the premade claims"), `authoredImpacts`;
- removed: `blueprintOverride` (R15) and its branch at `CaseFactory.cs:170`; `minDay`/`maxDay` (R15: the day pools decide the days) and their readers (`CaseFactory.cs:519`, the validator's range warning and inverted-range error);
- added: `public string id;` (a `LookKeys` token), `public string birthDate;` (the registered date), `public TravellerGender gender;`, `public NationEraProfileSO truePlace;` ("null = honest; else an authored liar from this place"), `public string introLine;` ("the desk's opener; blank = the interview's legendary opener"), `public string recordNote;` ("Citizen Records note; blank = the sealed-records note"), `public string dialogId;` ("a dialog offered only while this premade is at the desk"), `public bool oncePerRun = true;` ("met once, never again this run; written by Generate World as `!repeatable`").

**`DayPlanSO`** (CRLF):
- `ForcedCaseSlot` gains `public LegendarySO legendary;` ("a premade who stands in this slot; the slot is never a rule violator's"); the class doc covers both fields (a slot names a blueprint, a premade or both, all written by Generate World from `days[].forced`).
- `public IReadOnlyList<ForcedCaseSlot> ForcedCases => forcedCases;`
- `public bool TryGetForcedPremade(int caseIndex1Based, out LegendarySO premade)`, like `TryGetForcedCase` (`DayPlanSO.cs:143-159`), which is unchanged and stays reachable through `days[].forced[].blueprint` (R20).
- Docs of `legendaryBaseChance`/`availableLegendaries`/`forcedCases`: "premade", "written by Generate World".

**`ContentLibrarySO`** (CRLF): `[Header("Characters")] [SerializeField] private LookRules lookRules = new();` and `public LookRules LookRules => lookRules;`. The `legendaries` doc says "premade characters (generated)".

**`DocumentTemplateSO`** (CRLF): `public bool showsPhoto;`: "The first page carries the traveller's photo (a 4:5 crop of how they look). Only the Travel Passport does."

**`CaseInstance`** (CRLF):
- `public TravellerLook look;`: "How the traveller looks (layers and clickable garments; a premade: one whole picture), composed at generation from the claim; a liar's dress tell is one garment from the true home."
- Docs: `isLegendary`/`legendarySource` say "premade"; `gender` "from the claimed place's name list, or the premade's".

`TimelineKeys` (`TimelineService.cs:8-27`) is unchanged: the met flag's name lives in Domain `FlagKeys` (§2.5), piece 3's one home for run-flag names.

### 2.8 Case generation (`CaseFactory.cs`, CRLF)

**Fields:**
- `_looksRng` ("the current traveller's look stream, Seeds.ForLooks") and `_legendaryRng` ("the current slot's premade roll, Seeds.ForLegendary");
- `_channels` ("today's tell channels: the plan's, minus Appearance when the Visitor window cannot show dress").

**`GenerateDayCases(DayPlanSO plan, WorldState state, int daySeed, IReadOnlyList<ClueCategory> askable, IReadOnlyList<ClueCategory> answerTellCategories, bool appearanceReachable)`** (piece 3's five parameters plus one; `askable` and `answerTellCategories` keep piece 3's meaning and handling):
- `_channels` = `plan.TellChannels` without `Appearance` when `!appearanceReachable`;
- each premade in `plan.ForcedCases` has its `displayName` reserved in the fresh roster before slot 1 (this reservation is also what keeps forced premades out of the day's roll, R16);
- the loop adds `_looksRng = new SeededRandom(Seeds.ForLooks(caseSeed))` and `_legendaryRng = new SeededRandom(Seeds.ForLegendary(caseSeed))`.

**`PlanViolators`**: passes the forced-premade slots to `ViolatorSlots.Pick` as exclusions.

**`GenerateSingleCase`:**
- **Step 2 becomes `LegendarySO legendary = ResolvePremade(plan, state, caseIndex1Based)`**, before the violator lookup:
  - `Premades.SlotSource(forced here, forced premade met, _violators has the slot)`, with "met" = `state.HasFlag(FlagKeys.PremadeMet(id))`;
  - Forced → the forced premade; a met forced premade logs "[CaseFactory] Case {n}: forced premade '{name}' was already met this run; the slot holds an ordinary traveller.";
  - Roll → `TryRollLegendary`, rewritten over `Premades.IsRollable` (met from `state.HasFlag(FlagKeys.PremadeMet(id))`, name from `_roster.IsTaken`; the day-range filter at 519 goes with `minDay`/`maxDay`) and `Premades.Roll(chance, count, DevToolsState.ForceLegendaryNextCase, _legendaryRng)`; the dev cheat is consumed as today;
  - the comment at 156 ("a legendary keeps its own place") goes: a premade never meets a violator slot now.
- **Blueprint**: the `legendary.blueprintOverride` branch goes (R15); the forced blueprint (step 1, `TryGetForcedCase`) is unchanged.
- **Name**: `ResolveGivenName` skips `Reserve` for a forced premade (already reserved).
- **Gender**: a premade's `gender`; otherwise piece 2's derivation.
- **Birth date**: a premade's `birthDate`; otherwise `GenerateBirthDate(place)`.
- **Opener** (piece 3's call, one more argument): `Interview.Opener(_lib.Interview, gender, legendary != null ? legendary.displayName : null, legendary != null ? legendary.introLine : null)`; the precedence (a premade's own opener first) is `Interview.Opener`'s, tested in `InterviewTests`.
- **After `Disguise` and piece 3's `AddAnswers`**: `inst.look = ComposeLook(inst, place, lie)`.
- **The null-blueprint early return** (205-209) leaves `inst.look` null; `Retain`, both views and the case log accept a null look (§2.9, §2.10).
- **Case log**: adds `look={inst.look.Describe()}`, with an explicit null check.

**`Disguise`** (piece 2 + 3):
- `Lies.MayLie(inst.isLegendary && legendary.truePlace == null, inst.claimAllowedByRules, fields)`.
- Generated traveller: the `_todays` projection passes `AppearanceLeakable = Looks.CanLeak(claimSource, SourceOf(p), inst.gender, _lib.LookRules)`, computed with `Culture` values from `_facts`.
- Premade liar (R17): when `truePlace` is not in `_todays`, warn "[CaseFactory] Case {n}: premade '{name}' is authored as a liar from '{truePlace.OriginLabel}', which is not in today's world, so they stay honest. List them only on days that include their true place." and stay honest. Otherwise `Lies.Plan(1f, plan.TellCount, ..., todays: [truePlace], papers, answerTellCategories: _answerTellCategories, channels: _channels without Appearance, ...)`; `trueHome = truePlace` for a `Liar` outcome. Both paths pass piece 3's `_answerTellCategories` (never `_askable`), so a purchase never changes a premade's lie either.
- The piece-3 `NoPossibleLie` warning text becomes §2.16's.

**New `private TravellerLook ComposeLook(CaseInstance inst, NationEraProfileSO place, LiePlan lie)`:**
- premade → `Looks.Whole(id, claim, _lib.LookRules)`;
- no place → `Looks.Compose` with a null wardrobe (minimal look) and a warning naming Generate World;
- Unknown gender → a warning ("[CaseFactory] Case {n}: '{name}' has no known gender (not on the place's name lists); the look draws one. Check the place's names.");
- otherwise `Looks.Compose(claim, leak, inst.gender, inst.trueBirthDate, place.year, place.looks, _lib.LookRules, _looksRng)`:
  - `claim` = `LookSource(place ids, place.wardrobe, ResolveFieldValue(Culture, inst))` (the existing placeholder grammar covers a missing fact);
  - `leak` = the true home's source, with `lie.TellValue(Culture)`, only when `lie.ChannelOf(Culture) == TellChannel.Appearance`.

**`BuildRegistry`**: a premade's note is its `recordNote`, or the existing sealed text when blank.

**Streams after piece 4:**

| Stream | Seed | Draws | Change |
|---|---|---|---|
| Case (`_rng`) | `ForCase` | era, blueprint, archetype, place, name, birth date | the premade roll leaves it (today it never draws: every day's list is empty) |
| Premade (`_legendaryRng`) | `ForLegendary` (new) | roll unless forced or the cheat is on, then the pick; nothing on forced or violator slots or with no rollable premade | new |
| Clue, Dialog | `ForClues`, `ForDialog` | unchanged | none |
| Violators | `ForViolators` | slots and places | forced-premade slots excluded (day 1's slot 3 is in the first half, but day 1 has no rules; day 2's slot 6 is in the second half) |
| Lies (`_lieRng`) | `ForLies` | piece 3's; the option pool gains Appearance options on days that allow them; an authored liar premade draws roll, home (one candidate) and tells | days 1–2 unchanged on non-premade slots; met flags can change day-3 lies (§1.5) |
| Looks (`_looksRng`) | `ForLooks` (new) | [gender], skin, face, hair colour; nothing for premades or a missing wardrobe | new |

### 2.9 Rendering (`Assets/Scripts/Characters`, new, LF)

**`CharacterArt`** (`sealed class`, `IDisposable`):
- `public const string ResourcesFolder = "Characters";` and `public const string AssetFolder = "Assets/Art/Characters/Resources/" + ResourcesFolder;`.
- `CharacterArt(ContentLibrarySO library)`: the library supplies placeholder colours (a nation's index in `Nations` sets the hue; an era's `order` sets the shade).
- `Sprite Get(LookKey key)`: cached by `key.Name`; `Resources.Load<Sprite>($"{ResourcesFolder}/{key.Name}")`, else a placeholder:
  - region by layer: HairBack → HairBehind, Body → Body, Outfit → Clothes, Head → Head, FacialHair → Beard, Hair → HairCap, Headwear → Hat, Accessory → Collar, Whole → WholeFigure;
  - fill: garments the nation hue at the era shade; hair, hair back and facial hair their colour (a small table keyed by the `LookKeys` colour constants) with the nation hue as accent; body and head a five-step skin ramp; premades the nation hue with stripes and the expression mark; a nation id the library does not list (a content gap) grey. Hue and shade values come from `PlaceholderPalette.FromHsv` (tested);
  - `Texture2D(256, 384, RGBA32, false)` + `LoadRawTextureData` + `Apply(false, false)` (stays readable), then `Sprite.Create(tex, full rect, (0.5, LookCanvas.FeetPivotY), pixelsPerUnit: 384, 0, SpriteMeshType.FullRect)`;
  - the first placeholder of a session logs once: "[CharacterArt] No final art for '{key}' (and maybe others); drawing a placeholder. Final art goes to {AssetFolder}/<key>.png (docs/CHARACTER_ART_CONTRACT.md)."
- `void Retain(IEnumerable<LookKey> keys)`: releases every cached key not in the set (`Resources.UnloadAsset` on a loaded sprite's texture; `Destroy` for a placeholder's texture and sprite).
- `Dispose()`: releases everything.

**`TravellerView`** (booth, `MonoBehaviour` on `Traveller`):
- `[SerializeField] private SpriteRenderer[] layers;` (index = `LookLayer`, wired by the builder); `Awake` warns once when the length is not `LookLayer`'s count ("rebuild the scene").
- `Show(TravellerLook look, CharacterArt art)`: a null `look` (a case whose blueprint was missing, `CaseFactory.cs:205-209`) or a null `art` calls `Clear()` and returns; otherwise sets each renderer's sprite from `look.PartOn(layer)`, null elsewhere.
- `SetExpression(string expression)`: for a premade, the `Whole` sprite becomes `art.Get(look.WholeKey(expression))`; nothing when the current look is null or not a premade.
- `Clear()`: every sprite null; the current look is forgotten.

**`TravellerPortraitView`** (uGUI):
- `[SerializeField] private Image[] layers;`, `[SerializeField] private Image[] highlights;`, `[SerializeField] private Button[] buttons;` (index = `LookLayer`; highlight and button entries exist only where `Looks.IsGarmentLayer` holds, `Whole` included; both arrays are empty on the photo), `[SerializeField, Range(0f, 1f)] private float alphaHitThreshold = 0.5f;`.
- `Show(TravellerLook look, CharacterArt art, CompareController compare)`:
  - a null `look` or `art` calls `Clear()` and returns (every layer, highlight and button object inactive, every listener removed);
  - each layer object is active only when the look has a part there;
  - its sprite, and its highlight's sprite, come from `art.Get`;
  - `alphaHitTestMinimumThreshold` = the knob. A garment texture that is not readable cannot be alpha-tested: every layer is a full-canvas rectangle, so the topmost garment would take every click and a leaked lower garment could not be clicked. The view then logs an **error** once ("[TravellerPortraitView] '{key}' is not readable, so garments cannot be told apart by clicks; the character art importer should make it readable. Re-import Assets/Art/Characters.") and sets 0 so the figure still shows. The importer (R10) makes every texture under `Assets/Art/Characters/` readable and placeholders are created readable, so this state means a broken import (§7);
  - when `compare` is not null and the part has a garment, the button calls `compare.Select($"Visitor · {Looks.SlotLabel(g.Slot)}", g.Label, highlight, CompareEvidence.ForAppearance(Looks.EvidenceCategory, g.Value, g.IsTell))`; previous listeners are removed.
- `SetExpression(string expression)`, `Clear()` as in `TravellerView` (`Clear` also deactivates the highlights and buttons and removes their listeners).
- Highlight overlays are separate Images with the garment's sprite, colour (1, 1, 1, 0) and no raycast, so `CompareController.Fill` (`CompareController.cs:99-110`) tints the overlay, never the garment. Buttons use `Transition.None`, with the garment Image as `targetGraphic`, so the existing `HoverUIOutline` hover applies unchanged.

### 2.10 `GameManager` and `DayOrchestrator` (CRLF)

**`GameManager`:**
- `[SerializeField] private TravellerView travellerView;`: "Optional: the booth figure (from presentation until the decision)."
- Private `CharacterArt _characterArt`:
  - `Start`: `_characterArt = new CharacterArt(contentLibrary)`; `investigationUI.SetCharacterArt(_characterArt)` in the injection block; subscribe `investigationUI.TravellerExpressionChanged += HandleTravellerExpression`;
  - the `GenerateDayCases` call is piece 3's with one more argument:

    ```csharp
    _dayCases = _caseFactory.GenerateDayCases(dayPlan, _worldState, seed,
        spoken ? interview.AskableCategories : Array.Empty<ClueCategory>(),
        spoken ? interview.AnswerTellCategories : Array.Empty<ClueCategory>(),
        investigationUI != null && investigationUI.VisitorReachable);
    ```

  - `OnDestroy`: the new lines go **before** the existing `if (orchestrator == null) return;` (`GameManager.cs:188-189`), so they also run in scenes where `Start` bailed out: `if (investigationUI != null) investigationUI.TravellerExpressionChanged -= HandleTravellerExpression;` then `_characterArt?.Dispose();` (a plain class, so `?.` is safe there).
- `ShowActiveCase` (`GameManager.cs:372-380`), before presenting:
  - `_characterArt.Retain(inst.look.Keys)` (an empty set when the look is null);
  - `travellerView.Show(inst.look, _characterArt)` when wired (a null look clears it);
  - for a premade with `oncePerRun`, `_worldState.SetFlag(FlagKeys.PremadeMet(id))` (W8).
- `HandleDecision` and the legacy `HandlePlayerChoseEra`: `travellerView.Clear()` when wired.
- `HandleDayCompleted`: `travellerView.Clear()` and `_characterArt.Retain(none)`.
- The `HandleShiftClosed` abandon path needs nothing: the figure appears only at presentation.
- `HandleTravellerExpression(string e)`: `travellerView.SetExpression(e)` when wired.
- Piece-3 `BuildInterviewDay`: passes every library premade's non-blank `dialogId` as `premadeDialogIds`.

**`DayOrchestrator.WarnAboutUnreachedContent`** (`DayOrchestrator.cs:231-232`): also reports `forced premade {name} (slot {s})` through `TryGetForcedPremade`, but only when the slot really held that premade: `Premades.SlotSource(true, _worldState.HasFlag(FlagKeys.PremadeMet(id)), false) == PremadeSlot.Forced` (a forced slot is never a violator's). A premade met on an earlier day left an ordinary traveller there, so it is not named. The forced-blueprint branch is unchanged.

### 2.11 UI

**`InvestigationUIController`** (CRLF):
- Fields `[Header("Visitor")] [SerializeField] private TravellerPortraitView visitorView; [SerializeField] private OSWindowChrome visitorChrome;` and a private `CharacterArt _art`.
- `public bool VisitorReachable => !RichMode || (visitorView != null && visitorChrome != null);` (R14). Both serialized references are tested with `!= null`, never `?.` (piece 3's pattern: an unassigned field is Unity's fake null).
- `public event Action<string> TravellerExpressionChanged;`
- `public void SetCharacterArt(CharacterArt art)`.
- `Awake`: after piece 3's interview warning, when `RichMode && !VisitorReachable`, log once §2.16's wiring warning, which names both fields.
- `ShowRich`, after `CloseAllWindows()` and the document clones:
  - `visitorView.Show(inst.look, _art, compareController)` and `visitorChrome.Open()` (R12);
  - document clones call `SetDocument(doc, compareController, inst.look, _art)`.
- Piece 3's `StartInterview` passes the premade's `dialogId` (explicit null checks, not `?.` on a ScriptableObject) to `_day.OfferedDialogs(...)`.
- Piece 3's `Choose`: after the transcript refresh, the last new traveller line with an `Expression` calls `visitorView.SetExpression(e)` and raises `TravellerExpressionChanged(e)`.
- `RefreshScannerText`: §2.16 idle hint.
- `BuildFallbackBody`: the dress block (R24) after the agency record.
- Class summary mentions the Visitor window.

**`DocumentWindowController`** (CRLF):
- `[SerializeField] private GameObject photoBox; [SerializeField] private TravellerPortraitView photo; [SerializeField] private float photoInset = 120f;`
- `SetDocument(DocumentInstance doc, CompareController compare, TravellerLook look, CharacterArt art)`: `photo.Show(look, art, null)` when `doc.template.showsPhoto` and `look` is not null.
- `Awake` reads the rows' `VerticalLayoutGroup` (on `fieldRowsRoot`) once and keeps its authored padding (the builder's `AddVLayout` sets (6, 6, 6, 6), `OfficeSceneUIBuilder.cs:504`).
- `ShowPage`: the box is active only on page 0 of a photo document; there the layout gets `padding = new RectOffset(base.left + (int)photoInset, base.right, base.top, base.bottom)`, and on every other page the authored padding back (assigning the property marks the layout dirty; editing `padding.left` in place would not).

**`CompareController`** (CRLF, R2): `Refresh` computes `DiscrepancyLog.ValuesMatch(_a.evidence.MatchValue(_a.value), _b.evidence.MatchValue(_b.value))`; the private `ValuesMatch` (140-144) is deleted.

### 2.12 Builder (`OfficeSceneUIBuilder.cs`, LF)

- **`BuildBooth`** (875-944): the `Traveller` `EnsureSprite` call (885) becomes `BuildTraveller(root.transform)`:
  1. get or create `Traveller`; destroy a leftover `SpriteRenderer` on it;
  2. `localPosition` (0, −3.3, 2), `localScale` 5.4 (R11), active;
  3. get or add `UnityEngine.Rendering.SortingGroup`, `sortingOrder = −20`;
  4. one child per `LookLayer` through `EnsureSprite(traveller, layer.ToString(), null, Vector3.zero, (int)layer)`;
  5. get or add `TravellerView` and set `layers` with `SerializedArrays.Set`.
  The `EnsureOfficeSprite("traveller", ...)` call goes. `Assets/Art/Office/Placeholder/traveller.png` and its meta stay (R26: the tracked `Assets/_Recovery/0.unity` and Codex's scene reference them).
- **Visitor window**, a new block after Citizen Records (piece 3 adds its transcript block there too):
  1. `DestroyChildIfPresent(windowLayer, "VisitorWindow")`;
  2. `BuildOSWindow(windowLayer, "VisitorWindow", "Visitor", "Click a garment, then a Costume Guide entry, to compare.", new Vector2(300f, 470f))`, anchored at (−480, 60); its `Body` text moves to anchors (0.04, 0.01)–(0.96, 0.07), 12 pt;
  3. a `Figure` panel at anchors (0.03, 0.08)–(0.97, 0.93) with an `AspectRatioFitter` (FitInParent, 1024/1536);
  4. `BuildPortraitStack(figure, clickable: true)`: one full-stretch Image per `LookLayer` (raycast only where `Looks.IsGarmentLayer` holds: every layer but `Body` and `Head`, so `HairBack` and a premade's `Whole` are clickable), and for those layers a `Button` (`Transition.None`, target the layer Image) plus a child `Highlight` Image; wire `TravellerPortraitView` with `SerializedArrays.Set`;
  5. `BuildDesktopIcon(bookShelf, "IconVisitor", "Visitor", visitorChrome, "")`.
- **`BuildDocumentWindow`** (331-359): the PhotoBox becomes R13's box (grey frame colour kept as the empty background, the "PHOTO" label removed), holding a `RectMask2D` and a `Portrait` child anchored by `LookCanvas.PhotoAnchorMin`/`Max` with `BuildPortraitStack(portrait, clickable: false)`; `DocumentWindowController` gets `photoBox`, `photo`, `photoInset = 120`.
- **Wiring**: `soInvest` sets `visitorView` and `visitorChrome`; `soGm` sets `travellerView`.
- **`InPolygon`** (1236-1246) is deleted; `CursorPixel` calls `PixelShapes.InPolygon`, and `ArrowCursorShape`/`HandCursorShape` become `(float x, float y)[]` (same points).
- The final log line mentions the traveller figure and the Visitor window.
- After the change the scene is rebuilt in the worktree's Unity and `OfficeScene.unity` is committed.

**`SerializedArrays`** (new, `internal static class`): `Set(SerializedObject so, string prop, IReadOnlyList<Object> values)` and `DropMissing(SerializedObject so, string prop)`, moved from `WorldContentGenerator.cs:355-386`; the generator and the builder call them. Their error (today "[WorldContentGenerator] '{object}' has no serialized field '{prop}'.", 360 and 374) gets a neutral prefix: "[SerializedArrays] '{object}' has no serialized field '{prop}'." (the object's name tells which tool's target it was).

### 2.13 Content pipeline

**`world_source.json`** (LF, hand-maintained):
- `content.books` gains `Assets/Data/Investigation/RefBook_Culture.asset`.
- New top-level `looks`: `{ faceBands: [{minAge, faces[]}], greyFromAge, wholeFigureLabel, confusable: [{a, b, slot, gender}] }`.
- `countries[].looks`: `{ skin: [5 numbers], hair: [{colour, weight}] }`; `places[].looks` (optional override, per list: a non-empty `skin` replaces the country's skin weights, a non-empty `hair` its hair weights; an empty or missing list inherits).
- `places[].wardrobe`: `{ m: GenderLookData, f: GenderLookData }`, where `GenderLookData` is `{ signature, outfit, hair, facialHair, headwear, accessory }` and each item is `{ label, leakable, wig, back, covers[] }` (a missing or empty item = none; the flags read false when missing).
- New `premades[]`: `{ id, name, gender, place, truePlace, birthDate, archetype, intro, recordNote, dialog, repeatable, impacts: [{attribute, onCorrect, onWrong, skipNationScore}] }`. Every key reads safely when missing (R29): `truePlace`, `intro`, `recordNote`, `dialog` empty = none; `repeatable` false = once per run; `skipNationScore` false = the impact also moves the nation's score; the required keys are checked.
- `days[]` gains `premades[]` (ids for the random pool; missing = none), `forced: [{slot, premade, blueprint}]` (`premade` an id, `blueprint` an asset path like `content.blueprint`; an entry names at least one) and `premadeChance` (required above 0 when the pool is non-empty); day 3's `channels` become `["Papers", "Answer", "Appearance"]`.
- `dialogs[].nodes[].lines[]` and `dialogs[].nodes[].choices[].lines[]` gain optional `expression`.

Example wardrobe (the format; the plan authors all 40 places):

```json
"wardrobe": {
  "m": { "signature": "Headwear",
         "outfit":   { "label": "frock coat" },
         "hair":     { "label": "side-parted curls" },
         "facialHair": { "label": "mutton-chop whiskers", "leakable": true },
         "headwear": { "label": "top hat", "leakable": true },
         "accessory": { "label": "watch chain" } },
  "f": { "signature": "Headwear",
         "outfit":   { "label": "bell-skirted day dress" },
         "hair":     { "label": "ringlets and bun", "back": true },
         "headwear": { "label": "poke bonnet", "leakable": true },
         "accessory": { "label": "Paisley shawl", "leakable": true } }
}
```

Culture values this gives (R1), for scale: britain_industrial "top hat / poke bonnet" (21); egypt_ancient "wesekh collar" (both genders' accessory, 13); greece_ancient "petasos hat / sakkos snood" (26); italy_ancient "Caesar crop / nodus roll" (hair, 24).

**`WorldContentGenerator`** (LF):
- `OwnedFolders` gains `"Premades"`.
- Source classes: `WorldSource.looks`, `premades`; `CountryData.looks`, `PlaceData.looks`/`wardrobe`; `DayData.premades`/`forced`/`premadeChance`; new `LookRulesData`, `FaceBandData`, `ConfusableData`, `LooksData`, `HairWeightData`, `WardrobeData`, `GenderLookData`, `ItemData`, `PremadeData`, `ImpactData`, `ForcedData`; piece 3's `LineData` gains `expression`.
- `LoadAuthored`: archetypes are also indexed by `ArchetypeSO.id` for premades, and every non-empty `days[].forced[].blueprint` path is loaded like `content.blueprint` (a missing or wrong-typed asset is an error before anything is written).
- **Checks** (a new `CheckCharacters`, called before anything is written; every message names the place, premade or day). ASCII is checked with piece 3's helper for authored text (R18 there) and line lengths with piece 3's `Interview.WorstCaseLength`, both called, never copied:
  - **wardrobe**: every place has both genders; outfit and hair present; the signature parses as a `LookSlot` and its item is present; labels ≤ `Looks.MaxLabelLength` and ASCII; `covers` entries parse and never name the item's own slot; `back` only on hair; `wig` only on hair; `Looks.LabelProblems` over every place's wardrobe is empty (R1: per-gender signature labels unique, no item carrying another place's signature label for the same gender, no "/");
  - **Culture**: `facts[]` has no Culture entry ("Culture is derived from the wardrobe's signature items"); `Looks.CultureValue` is ≤ `FactTable.MaxValueLength` characters and unique across all places under `ValuesMatch`;
  - **questions** (piece 3's list): no question has category Culture (`Looks.EvidenceCategory`): "dress is seen in the Visitor window, never asked". With the Costume Guide present, piece 3's `Forgery.IsProvableCategory` would otherwise accept one and let dress leak as a spoken Answer tell;
  - **ids**: every country, era and premade id passes `LookKeys.IsToken`;
  - **looks**: 5 non-negative skin weights with a positive sum; hair colours are `LookKeys.HairColours` other than grey, with a positive sum (after the country default is applied); face bands ascend by `minAge`, the first is ≤ `travellerAgeMin`, **every band has at least one face** (`Compose` indexes into it), faces are single lowercase letters; `greyFromAge > 0`; `wholeFigureLabel` non-blank, ASCII and ≤ `Looks.MaxLabelLength` (the compare bar shows it); confusable places exist and slots parse; gender is "m", "f" or empty;
  - **premades**: unique ids and names; the name is ASCII and in no place's name lists; gender is Male or Female; `place` and `truePlace` exist and differ; `birthDate` parses and its year lies in the place's birth years (year − ageMax .. year − ageMin); `archetype` resolves; `dialog` is empty or a dialog id; `intro` is empty or ASCII (it becomes the transcript's first line, piece-3 R8); `recordNote` is empty or ASCII and ≤ `Premades.MaxNoteLength`; every impact's attribute resolves, and an impact with both deltas 0 is rejected (a missing delta reads 0, R29);
  - **days**: pool and forced ids exist; every forced entry has `slot` ≥ 1 and within the queue, and names a premade, a blueprint or both; forced slots are unique; a premade appears at most once in a day's `forced` list and is not both forced and pooled on one day; a forced `blueprint` loads as a `CaseBlueprintSO` (checked like `content.blueprint`); a forced or pooled premade's claimed and true places are in the day's world; `premadeChance` is in 0..1, and above 0 when the pool is non-empty; a warning when a forced premade's slot is within `ViolatorSlots.Window(queue)` on a day with rules ("it takes a slot a guaranteed violator could need; with every first-half slot taken a violator is dropped");
  - **dialog lines**: `expression` is empty or in `LookKeys.Expressions`, and only on Traveller lines (node lines and choice lines alike).
- **Piece 3's checks, changed:**
  - **menus** (`CheckInterview`): `DialogChecks.MenuProblems(questions.Length, smallTalk, the most document templates of the wired blueprint and of every days[].forced[].blueprint, the dialogs no premade names, the dialogs premades name, menuCapacity)`. The "every listed legendary's blueprint override" term goes with `blueprintOverride` (premades use the day's blueprints);
  - **line length**: `{name}` is filled with the longest `premades[].name` of this run's source (the premades are generated in the same run, so the library's list could be stale or empty); every non-empty premade `intro` joins the checked lines (no token: its own length ≤ `maxLineChars`).
- **Builders**:
  - `MakePlace` writes `year`, `wardrobe`, `looks` (the country default with the place's non-empty lists over it) and appends the derived Culture `ProfileFact`;
  - new `MakePremade` writes `Premades/Premade_{id}.asset` (all fields; `oncePerRun = !repeatable`; `authoredImpacts` from `impacts` with `alsoAffectsNationScore = !skipNationScore`);
  - `MakeDay` writes `availableLegendaries` (`SerializedArrays.Set`, replacing `DropMissing` at 273), `forcedCases` (slot, premade and blueprint of every `forced` entry; authoritative) and `legendaryBaseChance`; its doc (260-263) says it writes the premade pool, the forced slots and the chance;
  - `WireLibrary` sets `legendaries` (authoritative, replacing `DropMissing` at 305) and `lookRules` through `boxedValue`.
- The class doc covers characters, premades and forced slots, and drops "legendaries" from the hand-authored content that survives a re-run (18-19).

**`ContentLibraryValidator`** (CRLF):
- `RequiredFacts` gains `Culture` (doc: "papers + books + questions + dress").
- `CheckPlaces`: the Culture fact equals `Looks.CultureValue(place.wardrobe)` (else "run Generate World") and is ≤ `FactTable.MaxValueLength` characters; both genders have outfit and hair; the signature item exists; `year` is non-zero; the weights have positive sums. A place whose signature is `Outfit` for a gender gets a warning ("it can never leak"), with no exception in piece 4 (piece 5 exempts Future places through `isFuture`, R27).
- New `CheckCultureUnique`: no two places share a Culture value under `ValuesMatch` (error; piece-2 R13's premise for dress), and `Looks.LabelProblems` over the library's places is empty (error).
- `CheckLookRules`: every face band has at least one face; `greyFromAge > 0`; `wholeFigureLabel` non-blank.
- `CheckDuplicateIds` over `Ids(lib.Legendaries, l => l.id)`.
- `CheckLegendaryReferences` adds: blank name; unreadable or out-of-range `birthDate`; `truePlace` equal to the claim; `introLine` longer than `lib.Interview.maxLineChars`; `recordNote` longer than `Premades.MaxNoteLength`. Its inverted-day-range error (355-357) goes with `minDay`/`maxDay`.
- `CheckDayPlanLegendaryRanges` (290-323) keeps its null-entry error and loses its range warning (314-318); it is renamed `CheckDayPlanLegendaries` (call at 82), and its summary (290-293) and `CheckLegendaryReferences`' (325) drop the day range.
- `CheckDayPlanPlaces` adds: forced premades (like listed ones) whose claim or `truePlace` is outside the day's world; forced slots beyond the queue; a premade forced twice on one day (error); a forced premade in the first `ViolatorSlots.Window(VisitorsCount)` slots of a day with `GuaranteeRuleViolators` and active rules (warning, the generator's text); a day allowing Appearance without a Culture book (warning).
- Piece 3's checks, changed: the `DialogChecks.MenuProblems` call counts the day plans' blueprints (possible and forced) only, and passes the dialogs no library premade names and those premades name (their `dialogId`s) separately; a question whose category is Culture is an error (the generator's text).
- New `ReportCharacterArt` (a log line, never counted as an issue): how many of `LookKeys.Bases`, `Required` (every place) and `PremadeSet` (every premade) exist at `CharacterArt.AssetFolder/{key}.png`, and the first 20 missing names.

**`CharacterArtImporter`** (new `AssetPostprocessor`): `OnPreprocessTexture` for paths under `Assets/Art/Characters/` applies R10's settings every import (authoritative), with PPU from `GetSourceTextureWidthAndHeight`; a source that is not 2:3 logs "[CharacterArtImporter] '{path}' is {w}x{h}; character art must be 1024x1536 (2:3). See docs/CHARACTER_ART_CONTRACT.md."

**`RefBook_Culture.asset`** (new, + meta, LF like the other books): `displayName: Costume Guide`, `category: 6`.

**`DocTemplate_Passport.asset`** (CRLF): `showsPhoto: 1`. The Permit keeps the default false (no edit).

**`docs/CHARACTER_ART_CONTRACT.md`** (new, tracked), sections:
1. precedence (R26) and style: simpler, Restory-like textures, neutral even lighting, front view, civilian dress, no text, insignia, regalia or religious vestments;
2. canvas: `LookCanvas` values, safe area, feet pivot, the photo crop and what it cuts;
3. layers and stack order; hair back; optional facial hair, headwear and accessory; wigs;
4. processed file names (`LookKeys`), colour variants (hair and hair back unless a wig; facial hair always), Future places under the ordinary grammar with era `future` (R27; one drawing shared by several places is written under each place's names), the reserved `_v{N}` suffix, premade names;
5. delivery: RGBA PNG, 1024×1536, untrimmed, at `CharacterArt.AssetFolder`; import is automatic;
6. drawing rules for tells: an item never an absence; front-visible; readable in the Visitor window (figure about 270×406 px); accessories worn and not tied to another layer (issue 11); headwear sized over full hair (12); nothing in the outfit above the chin (6); outfits complete on their own; no makeup on heads (14); bandeau-and-shorts base (15); skins 2–5 recoloured from skin 1 (9); ornaments masked before recolouring (16);
7. premades: the list, four aligned expressions drawn on the approved base figure with faces pasted on the neutral image (18), in a separate chat without the no-likeness clause for real people;
8. other art: `refbook_cover_culture.png`; the existing `photo_frame.png` (240×300, 4:5) fits the photo box; `Assets/Art/Office/Placeholder/traveller.png` is retired (no new art goes there; it is deleted in the booth rework, R26);
9. pipeline repairs before art resumes: `apply_fixes.py` expects a `result` wrapper that `brief_review.json` lacks (and its ArtDeliverables copy reads `costumes.json`); `build_brief.py` writes into the main checkout; the abaya and tarha conflicts are resolved as in R23; Block A's "the game recolours it" is replaced by baked variants.

### 2.14 Content (starter values, for Saleh's review)

**`looks`:** face bands 18 → a, b; 35 → c; 60 → d; `greyFromAge` 60; `wholeFigureLabel` "Period dress"; confusable pairs authored from `costume_research.json` `confusable[]`, one entry wherever a pair's signature slot items read alike (at least greece/italy/germany Industrial men's facial hair, confusable #19, and greece_ancient/italy_ancient men's hair).

**Skin weights (tones 1–5) and hair weights per country** (never tells; broad on purpose):

| Country | Skin 1..5 | Hair |
|---|---|---|
| egypt | 0, 1, 3, 4, 2 | black 8, brown 2 |
| iraq | 0, 1, 4, 3, 1 | black 8, brown 2 |
| greece | 1, 3, 5, 1, 0 | black 5, brown 4, blond 1 |
| italy | 1, 4, 4, 1, 0 | black 4, brown 5, blond 1 |
| china | 1, 5, 3, 1, 0 | black 10 |
| japan | 1, 5, 3, 1, 0 | black 10 |
| britain | 5, 4, 1, 0, 0 | black 1, brown 5, blond 3, red 1 |
| germany | 4, 5, 1, 0, 0 | black 1, brown 5, blond 4, red 1 |

Place overrides: britain_modern skin 4, 3, 1, 1, 1 and hair black 2, brown 5, blond 3, red 1; germany_modern skin 4, 4, 1, 1, 0.

**Premades** (all Male, archetype by `ArchetypeSO.id`):

| id | name | claim | true place | born | role | schedule | intro / record note |
|---|---|---|---|---|---|---|---|
| senenmut | Senenmut | egypt_ancient | — | 14 Mar 1505 BCE | artist | forced day 1 slot 3; dialog `dlg_senenmut` | — / "Steward of the Pharaoh's household and architect of her temple." |
| socrates | Socrates | greece_ancient | italy_ancient (an impostor) | 6 Jun 470 BCE | wanderer | forced day 2 slot 6; pooled day 3 | "Priority arrival: Socrates of Athens. He asks more questions than you do." / "Philosopher. Known to question officials at length." |
| khwarizmi | Muhammad al-Khwarizmi | iraq_medieval | — | 3 Apr 780 | scientist | pooled days 2–3 | — / "Scholar of the House of Wisdom. Writes on calculation." |
| cailun | Cai Lun | china_ancient | — | 12 Aug 62 | scientist | pooled days 2–3 | — / "Court official, supervisor of instruments and weapons." |
| leonardo | Leonardo da Vinci | italy_earlymodern | — | 15 Apr 1452 | artist | pooled day 3 | — / "Painter and engineer in the service of the Duke of Milan." |
| zeami | Zeami Motokiyo | japan_medieval | — | 8 Nov 1363 | artist | pooled day 3 | — / "Actor and playwright of the sarugaku (Noh) theatre." |
| shakespeare | William Shakespeare | britain_earlymodern | — | 23 Apr 1564 | artist | pooled day 3 | — / "Player and poet of the Lord Chamberlain's Men." |
| gutenberg | Johannes Gutenberg | germany_medieval | — | 24 Jun 1400 | scientist | pooled day 3 | — / "Goldsmith of Mainz. Business in Strasbourg undisclosed." |

Every entry authors `repeatable: false` and `impacts: []` explicitly (R29); the intro and notes are ASCII, Socrates' intro is 73 characters (≤ `maxLineChars` 100), the longest name 21 ("Muhammad al-Khwarizmi", so the legendary opener fills to 40) and the longest note 63 (≤ `Premades.MaxNoteLength` 120). Every birth year lies in its claimed place's birth years (for example −1505 in −1540..−1488; 1564 in 1530..1582). Ancient and medieval dates are invented inside the plausible range; Leonardo's and Shakespeare's are the traditional dates. No name is in any place's name list (checked). The moments name Senenmut, Socrates, al-Khwarizmi, Cai Lun, Leonardo and Gutenberg; Zeami and Shakespeare fit their moments (Noh in Muromachi Kyoto; London c. 1600) without being named, and rulers (Hatshepsut, Tokugawa Ieyasu, Nasser) are excluded by C14. The set is all male because the non-ruler figures the moments name are men; women (for example an Ada Lovelace for britain_industrial, or an original inspired by Tu Youyou for china_modern) belong with the Industrial and Modern days of piece 5. Day 3's rules (no Medieval China, no Early modern Japan) forbid none of the day-3 premades' claims.

**Dialog** `dlg_senenmut` (one-shot, no conditions, bound to Senenmut): label "Ask about the temple >". Node `start`: Traveller (`dlg_senenmut.start.1`, expression happy) "My temple at Deir el-Bahari rises in three terraces. The Pharaoh will be pleased." Choices `more` "And if she is not?" → `worried`; `bye` "Safe travels." (ends). Node `worried`: Traveller (`dlg_senenmut.worried.1`, expression worried) "Then my name will be chiselled off every wall. Let us hope your stamp is kind." Choice `done` "Next!" (ends).

**Days:** day 1 forced `[{slot 3, premade senenmut}]`, pool `[]`; day 2 forced `[{slot 6, premade socrates}]`, pool `[khwarizmi, cailun]`; day 3 forced `[]`, pool `[socrates, khwarizmi, cailun, leonardo, zeami, shakespeare, gutenberg]`; `premadeChance` 0.05 authored on all three (today's value). No forced blueprint is authored (none is today). Day 1's slot 3 lies in the first half of its queue (`ViolatorSlots.Window(8)` = 4), which draws no warning because day 1 has no rules.

### 2.15 Knobs

| Knob | Where | Value | Authored in |
|---|---|---|---|
| Tell channels per day | `DayPlanSO.tellChannels` | day 3 adds Appearance | `days[].channels` |
| Face bands, grey age, whole-figure label, confusable pairs | `ContentLibrarySO.LookRules` | §2.14 | `looks` |
| Skin and hair weights | `NationEraProfileSO.looks` | §2.14 | `countries[].looks`, `places[].looks` |
| Wardrobe, signature slots, leakable/covers/wig/back | `NationEraProfileSO.wardrobe` | per place | `places[].wardrobe` |
| Premade roster, schedule, chance; forced blueprints | `LegendarySO`, `DayPlanSO` | §2.14 | `premades`, `days[]` |
| Garment hit threshold | `TravellerPortraitView.alphaHitThreshold` | 0.5 | builder |
| Booth placement | `Traveller` transform | (0, −3.3, 2), scale 5.4 | builder |
| Visitor window, photo box, row inset | builder / `DocumentWindowController.photoInset` | R12, R13, 120 | builder |
| Label, fact-value and record-note caps | `Looks.MaxLabelLength`, `FactTable.MaxValueLength`, `Premades.MaxNoteLength` | 24, 28, 120 | code (content-format limits of fixed UI boxes, shared by generator and validator; `FactTable.MaxValueLength` is the one fact-width cap piece 5 reuses) |
| Placeholder size | `LayerPlaceholder.Width`/`Height` | 256×384 | code (not gameplay) |

### 2.16 Copy

| Where | Text |
|---|---|
| `ShiftScoring.cs:142` | "Deviation denied without documented evidence. Log a deviation from the papers, the traveller's answers or their dress before denying." |
| Scanner idle hint | "Compare a document field, a traveller's answer or a garment they wear against the claimed place's reference entry, the entry it really belongs to, or the Citizen Record to log evidence." |
| `Discrepancy.Summary` | §2.5 |
| `NoPossibleLie` warning | "[CaseFactory] Case {n}: rolled a liar, but no other place today differs from '{originLabel}' in a fact its papers print, today's questions ask or its dress could leak (with a reference book), or in birth year, so the traveller stays honest. Widen the day's eras or countries, add a reference book, or allow more tell channels." |
| Visitor wiring warning | "[InvestigationUIController] Visitor window not wired (visitorView or visitorChrome): the traveller cannot be seen on the desk and no dress tell is generated today. Run Tools > TimeDesk > Build Office UI." |
| Visitor window | title "Visitor"; hint "Click a garment, then a Costume Guide entry, to compare."; compare label "Visitor · {slot}" |
| Fallback | "— TRAVELLER'S DRESS —", lines "    {slot}: {label} ({value})" |
| Book | "Costume Guide" |
| Case factory, art, importer, orchestrator, generator, validator messages | §2.8–§2.13 |

### 2.17 Every file that changes

| File | Assembly | Change |
|---|---|---|
| `Assets/Scripts/Domain/LookData.cs`, `Looks.cs`, `LookKeys.cs`, `Premades.cs` (+ metas) | Domain | new, §2.2–2.5 |
| `Assets/Scripts/Domain/Lies.cs` | Domain | `TellChannel.Appearance`, `HomeCandidate.AppearanceLeakable`, Appearance option, `MayLie` parameter, docs |
| `Assets/Scripts/Domain/DiscrepancyLog.cs` | Domain | `EvidenceKind.Appearance`, `ForAppearance`, `CompareEvidence.MatchValue`, statement side, Summary, public `ValuesMatch`, docs |
| `Assets/Scripts/Domain/FactTable.cs` | Domain | `MaxValueLength` |
| `Assets/Scripts/Domain/ClueLabels.cs` | Domain | Culture → DRESS |
| `Assets/Scripts/Domain/Gates.cs` (piece 3) | Domain | `FlagKeys.PremadeMet` |
| `Assets/Scripts/Domain/Interview.cs` (piece 3) | Domain | `Opener` takes the authored intro |
| `Assets/Scripts/Domain/Seeds.cs` | Domain | `LookSalt`/`ForLooks`, `LegendarySalt`/`ForLegendary` |
| `Assets/Scripts/Domain/BirthDates.cs` | Domain | `TryAgeAt` |
| `Assets/Scripts/Domain/ViolatorSlots.cs` | Domain | `Window`, exclusions |
| `Assets/Scripts/Domain/InterviewContent.cs`, `Dialog.cs`, `InterviewScript.cs`, `InterviewDay.cs` (piece 3) | Domain | expression (node and choice lines); premade-bound dialogs; `DialogChecks.MenuProblems` hub count |
| `Assets/Scripts/Domain/ClueCategory.cs`, `TravellerGender.cs` | Domain | docs |
| `Assets/Scripts/Visuals/LookCanvas.cs`, `PixelShapes.cs`, `PlaceholderPalette.cs`, `LayerPlaceholder.cs` (+ metas) | Visuals | new, §2.6 |
| `Assets/Scripts/Characters.meta`, `Characters/CharacterArt.cs`, `TravellerView.cs`, `TravellerPortraitView.cs` (+ metas) | Assembly-CSharp | new, §2.9 |
| `Assets/Scripts/CaseInstance.cs` | Assembly-CSharp | `look`; docs |
| `Assets/Scripts/CaseFactory.cs` | Assembly-CSharp | §2.8 |
| `Assets/Scripts/LegendarySO.cs` | Assembly-CSharp | premade fields; − `blueprintOverride`, − `minDay`/`maxDay`, − `CreateAssetMenu` |
| `Assets/Scripts/DayPlanSO.cs` | Assembly-CSharp | `ForcedCaseSlot.legendary`, `ForcedCases`, `TryGetForcedPremade`, docs |
| `Assets/Scripts/ContentLibrarySO.cs` | Assembly-CSharp | `lookRules`/`LookRules`, docs |
| `Assets/Scripts/DocumentTemplateSO.cs` | Assembly-CSharp | `showsPhoto` |
| `Assets/Scripts/Timeline/NationEraProfileSO.cs` | Assembly-CSharp | `year`, `wardrobe`, `looks` |
| `Assets/Scripts/GameManager.cs` | Assembly-CSharp | §2.10 |
| `Assets/Scripts/DayOrchestrator.cs` | Assembly-CSharp | forced premades that really held their slot in the unreached warning |
| `Assets/Scripts/UI/InvestigationUIController.cs` | Assembly-CSharp | §2.11 |
| `Assets/Scripts/UI/DocumentWindowController.cs` | Assembly-CSharp | photo |
| `Assets/Scripts/UI/CompareController.cs` | Assembly-CSharp | typed-evidence compare; − private `ValuesMatch` |
| `Assets/Scripts/Shift/ShiftScoring.cs` | Assembly-CSharp | citation text |
| `Assets/Scripts/DevTools/DevToolsState.cs` | Assembly-CSharp | doc ("premade") |
| `Assets/Editor/CharacterArtImporter.cs`, `SerializedArrays.cs` (+ metas) | Editor | new |
| `Assets/Editor/WorldContentGenerator.cs` | Editor | §2.13, including piece 3's `CheckInterview` menu and line-length checks |
| `Assets/Editor/ContentLibraryValidator.cs` | Editor | §2.13, including piece 3's `MenuProblems` call; − the legendary day-range checks |
| `Assets/Editor/OfficeSceneUIBuilder.cs` | Editor | §2.12 |
| `Assets/Data/World/world_source.json` | content | §2.13–2.14 |
| `Assets/Data/Investigation/RefBook_Culture.asset` (+ meta) | content | new |
| `Assets/Data/Investigation/DocTemplate_Passport.asset` | content | `showsPhoto: 1` |
| `Assets/Data/World/Places/Place_*.asset` (40) | generated | `year`, `wardrobe`, `looks`, Culture fact |
| `Assets/Data/World/Premades/` (folder + 8 `Premade_*.asset`, metas) | generated | new |
| `Assets/Data/Investigation/DayPlan_Inv_Day1..3.asset` | generated | premade lists, forced slots, chance; day 3 channels |
| `Assets/Data/World/Interview/Dialog_dlg_senenmut.asset` (+ meta) | generated (piece 3's folder) | new |
| `Assets/Data/Content Library/ContentLibrary_Main.asset` | generated | books, legendaries, `lookRules`, dialogs |
| `Assets/Scenes/OfficeScene.unity` | scene | rebuilt |
| `Assets/Tests/EditMode/{Looks,LookKeys,Premades,LookCanvas,PixelShapes,PlaceholderPalette,LayerPlaceholder}Tests.cs` (+ metas) | tests | new, §5 |
| `Assets/Tests/EditMode/{Seeds,Lies,DiscrepancyLog,BirthDates,ViolatorSlots}Tests.cs` and piece 3's `{Gates,Interview,InterviewDay,InterviewScript}Tests.cs` | tests | §5 |
| `docs/FEATURES.md` | docs | §3.3 |
| `docs/CHARACTER_ART_CONTRACT.md` | docs | new |
| `docs/superpowers/specs/2026-09-24-characters-design.md` | docs | this spec, committed first |

Checked and unchanged: `HoverHighlighter`, `HoverUIOutline`, `SpriteOutlineBuilder`, `Clickable`, `OfficeViewController`, `ReferenceBookWindowController` (piece 3's `PagedRowsWindow` renders the new book), `TimelineService.cs` (`TimelineKeys`: the met flag is `FlagKeys`'), `Forgery`, `OutlineMask`, `CitizenRecordsWindowController`, `OfficeUIController` (reads `introLine`, now possibly a premade's), `SaveSystem`, `WorldState`, `TimelineReactiveSprite`, `Test_DayLoop.unity` (no `TravellerView`: optional wiring), `Assets/Art/Office/Placeholder/traveller.png` and the tracked `Assets/_Recovery/0.unity` that references it (R26).

### 2.18 Why some logic stays outside Domain

The glue is:
- `CaseFactory` (building `LookSource`s and `HomeCandidate`s, resolving premades, logging);
- `CharacterArt` (Resources, textures, sprites), `TravellerView`, `TravellerPortraitView`;
- `GameManager`, `InvestigationUIController`, `DocumentWindowController`;
- the builder, the generator, the validator and the importer.

It reads ScriptableObjects, touches Unity objects or runs in the editor. Every decision it makes is a call into tested code: `Looks.Compose`/`Whole`/`CanLeak`/`CultureValue`/`LabelProblems`/`IsGarmentLayer`, `LookKeys`, `Premades`, `FlagKeys.PremadeMet`, `Interview.Opener`, `DialogChecks.MenuProblems`, `Lies.Plan`, `DiscrepancyLog.Prove`, `CompareEvidence.MatchValue`, `BirthDates.TryAgeAt`, `ViolatorSlots.Pick`/`Window` (Domain) and `LookCanvas`, `PixelShapes`, `LayerPlaceholder`, `PlaceholderPalette` (Visuals). The whole-day behaviour of the glue is proven by the Unity checks in §6.

### 2.19 Intent audit: the existing mechanism tried for each new one

| New | Existing mechanism reused or tried | Why something new was still needed |
|---|---|---|
| Dress evidence | `ClueCategory.Culture`, `FactTable`, book covers, `Prove`, `CompareController`, the deny gate | One `EvidenceKind` value and one statement-side check |
| Costume Guide | `ReferenceBookSO` + `FactTable.Rows` + the book window | none (one asset) |
| Culture facts | `ProfileFact` + `BuildFactTable` | derived by the generator (R1) instead of authored, to avoid a second source |
| Dress tell selection | piece 3's option pool in `Lies.Plan`, `Forgery.IsProvableTell` | one option kind and one candidate flag |
| Look composition | `WeightedRandom`, `BirthDates`, `Seeds` | no look model existed |
| Visitor window and photo | `BuildOSWindow`, `OSWindowChrome`, `BuildDesktopIcon`, `DocumentWindowTemplate`, `HoverUIOutline` | a layered Image stack is new |
| Booth figure | the builder's `EnsureSprite`, the `Traveller` object | a sorting group of layers replaces one sprite |
| Placeholders | the builder's PNG placeholders and cursor pattern (tried) | runtime drawing instead (R9); `InPolygon` moved, not copied |
| Art loading | `Resources.Load` (as for `RunConfig`); a catalog SO and Addressables (rejected, C9) | lazy per-key loading with release |
| Premades | `LegendarySO`, `TryRollLegendary`, `ForcedCaseSlot`, flags | fields and a Domain rule helper; no new type |
| Met guard | `WorldState.flags` + piece 3's `FlagKeys` grammar | one builder beside `TriggerFired` and `DialogDone` |
| Premade conversations and expressions | piece 3's dialog runner and content pipeline | one field per line and one filter |
| Array wiring in the builder | the generator's `SetArray` | moved to a shared helper |
| Per-gender label rule | `DiscrepancyLog.ValuesMatch`, the whole-value uniqueness check | the whole Culture value cannot see one gender's label; one Domain helper (`Looks.LabelProblems`) serves generator and validator |
| Garment compare value | `CompareController`'s private `ValuesMatch` (tried) | the "evidence value when present" rule moved to Domain (`CompareEvidence.MatchValue`) so it is tested |
| Forced blueprints | `ForcedCaseSlot.caseBlueprint`, `TryGetForcedCase` | none: `days[].forced` gives them the content path the generator's ownership would otherwise remove |
| Fact-width cap | the 28-character convention; piece 5's planned `FactTable.MaxValueLength` | defined once here, in `FactTable` |

### 2.20 Contracts for pieces 5 and 6

**Piece 5 (history changes facts):**
- A Future place is an ordinary `world_source.json` place (country, era `future`, year, birth years, facts, names) with a `wardrobe` block and its own keys: its outfit per gender is the culture-shaped Future outfit (`outfit_{g}_{country}_future`), its hair `hair_{g}_{country}_future_{colour}` (R27). No per-item art override or extra `CanLeak` row is needed: at most one Future place is in a day's world (piece-5 R1), and distinct places never share a key. A hairstyle shared by the eight Future places is an art-pipeline choice (the processing step writes one drawing under each place's names). The motif prose stays in the art brief; no other data slot is needed. A Future signature may be the outfit (a Future home then never leaks dress); piece 4's "signature is the outfit" validator warning is unconditional, and piece 5 exempts Future places with its `isFuture` flag.
- Culture facts are derived from wardrobes. History edits (`SetFact`) must not target Culture; H10's uniqueness simulation should include Culture, which holds unless wardrobes change. Future wardrobes pass `Looks.LabelProblems` with every other place.
- The fact-width cap is `FactTable.MaxValueLength` (28), defined here for the Culture value; piece 5 reuses it for every fact and history value (no second cap).
- Days 4–6 need `channels` including Appearance to keep dress tells, and their own `premades`/`forced`/`premadeChance` (required above 0 when the pool is non-empty, R29). Industrial and Modern premades follow C14 (originals inspired by real people for Modern places).
- Met-premade flags are a generation input (§1.5); piece 5's determinism statements should name them beside the history block.
- Premade multi-day arcs (a premade scheduled by flags or conditions) are piece 5's or later: the path is premade `conditions` evaluated by piece 3's `Gates` at day start. Premade impacts already reach the timeline through archetypes and `impacts`.
- H11 (Continue resumes at Home) removes the §1.8 replay caveat for met premades.

**Piece 6 (PC/UI reacts):**
- The Visitor window is PC chrome (themed). The passport photo and the Costume Guide are diegetic (Z4: skipped by theming).
- String keys: "Visitor", its hint, the slot labels (`Looks.SlotLabel`), "DRESS" (`ClueLabels`) and `wholeFigureLabel` are display text. **Garment item labels are evidence text**, like fact values: the Culture value the Costume Guide prints is built from them (R1) and the player finds the clicked label inside a book row, so they stay canonical and are never localised (U3), exactly as Culture values are.
- `DiscrepancyLog.ValuesMatch` is already public after this piece (R2); piece 6 reuses it.
- Per-traveller report lines may name a dress tell (`Garment.IsTell`, `Label`).

## 3. Retired or superseded

### 3.1 Code removed or replaced

- `OfficeSceneUIBuilder.InPolygon` (1236-1246), moved to `PixelShapes`; the `Traveller` placeholder call (885). `traveller.png` itself stays until the booth rework (R26).
- `CompareController.ValuesMatch` (140-144); its compare rule moves to `CompareEvidence.MatchValue`.
- `WorldContentGenerator.SetArray`/`DropMissing` (355-386), moved to `SerializedArrays` (with a neutral error prefix).
- `LegendarySO.blueprintOverride`, its `CreateAssetMenu`, and `CaseFactory`'s override branch (170); piece 3's two readers of the override (the generator's and validator's menu checks) lose that term.
- `LegendarySO.minDay`/`maxDay`, `TryRollLegendary`'s day-range filter (519), the validator's day-range warning (314-318) and inverted-range error (355-357).
- `CaseFactory.TryRollLegendary`'s inline chance and pick (530-548), moved to `Premades.Roll` on the premade stream.
- `ViolatorSlots.Pick`'s inline window (`(queueSize + 1) / 2`), now `ViolatorSlots.Window`.
- The builder's "PHOTO" label (343).

### 3.2 Earlier spec lines superseded (kept as approved records, not edited)

**`2026-09-24-identity-lies-design.md` (piece 2):**
- **E2 (line 25)** "Legendaries never lie": premades may be authored liars (C17, R17). **§1.1 line 71** "legendaries (none are authored)" and **R6 (42)**: `MayLie`'s exemption is now "an honest premade".
- **D10 (26)** and **§1.5 (114)** "Legendaries ... are unknown": premades carry an authored gender.
- **§4 (503-507)**: the piece-4 list is delivered here (clothing tells, sprite and photo, premade liars, the violator loss).
- **§7 line 620** ("Cross-piece gate trap"): dress tells are registrable (`EvidenceKind.Appearance`) and switched off where the Visitor window is missing (R14).

**Piece 3 (`2026-09-24-dialog-questions-design.md`, as committed at `fbf4905`):**
- **X1** and **R1**: the channels are Papers, Answer and Appearance; Appearance options follow Answer options.
- **§1.5** (tells by day) and **§2.14** `days[].channels`: day 3 adds Appearance.
- **R12**: a second reachability gate, `VisitorReachable`, for the Appearance channel.
- **R13**: `ClueLabels.Report(Culture)` is DRESS.
- **§1.1** legendary opener: a premade's own `introLine` wins; **§2.4** `Interview.Opener` gains the `authoredIntro` parameter.
- **§2.5** `InterviewDay` constructor and `OfferedDialogs`; **§2.4** `ScriptLine`/`DialogLine` gain an expression (node and choice lines).
- **§2.2** `FlagKeys` gains `PremadeMet`.
- **§2.9** `GenerateDayCases` gains `appearanceReachable` after `answerTellCategories`; **§2.10** the `GameManager` call passes it.
- **§1.8** "same run + same day ... = same travellers" and the stream-contract bullets "claims, names ... identical to piece 2" and "purchases and story flags never change the lie draws ... depend only on the run seed and the day", and **R23**'s "a purchase or a story flag never changes the lie draws": met-premade flags are a generation input (§1.5 here). Purchases still never change the lie draws.
- **R25**, **§2.4** `DialogChecks.MenuProblems` and **§2.13** (the generator's menu check, "every listed legendary's blueprint override", and the validator's "the legendaries' overrides"): the override term goes with `blueprintOverride`; premade-bound dialogs count as one hub entry (R18); the forced blueprints are those of `days[].forced`.
- **§2.13** line-length check: `{name}` takes the longest `premades[].name`, and premade intros are checked lines.
- **§1.7 and §2.16**: the citation and idle-hint texts are replaced by §2.16 here.
- **§4** piece-4 list: conversations (R18), portraits and expressions (the Visitor window, R18) and lying legendaries (C17) are delivered; booth speech bubbles go to the booth rework (W12); repeatable dialogs with consequences stay unscheduled.

**`2026-09-24-world-model-design.md` (piece 1):**
- **§6 line 86** "`moment` and `year` stay in `world_source.json` ... not copied to the asset": `year` is copied again (R8).
- **§6 line 94** "never wipes hand-authored legendaries ... leaves legendary chances to their authors": the generator owns premades, pools, forced slots and chances (R20), and `Premades` joins its owned folders.
- **§3 line 59** and **§4 line 67**: premade characters and clothing are delivered here.

**`2026-07-03-scanner-evidence-design.md`:** rule 1's statement side (already widened to answers by piece 3) includes a worn garment; the report wording names it ("traveller wears").

**`2026-06-21-office-scene-two-states-design.md`:** lines 42, 56-58, 68 and 154-158 (booth elements as prefabs under `Assets/Prefabs/Office`, a `Queue/TravellerSprite`, an animated traveller): the traveller is a builder-built layered figure; no prefab, queue or animation (W12).

### 3.3 `docs/FEATURES.md` (same commits as the behaviour)

Numbers are at `efe385d`; piece 3's edits land first, so the plan anchors by text.

- **:11** (streams): "same run + same day + the premades already met = same travellers"; add "a per-traveller look stream (`Seeds.ForLooks`) and a per-slot premade stream (`Seeds.ForLegendary`)" (seeding tested: `SeedsTests`).
- **:17** (booth): "... traveller (a layered figure shown from READY until the decision; not clickable) ...".
- **:23** (READY): unchanged.
- **:30/:33** (icons, apps): add the Visitor icon.
- **:35**: "Every new case closes all open windows, then opens the Visitor window for the new traveller (pin system planned to override)".
- **:36** (Records): "... a premade's record carries their own note".
- **:41** (places): "Each place has a moment and year, five authored facts (currency, language, technology, capital, ruler), a derived Culture (dress) fact naming its signature items, a wardrobe, and 8 male + 8 female period names".
- **:43** (roles): "... roles never change a traveller's dress".
- **:44** (liar bullet, piece-3 text): the tells sentence adds "or in dress (one garment, the true home's signature item for their gender, worn over the claimed look), from day 3"; "rule violators and legendaries never lie" becomes "rule violators and honest premades never lie (a premade authored as a liar leaks through papers or answers, never dress)"; the test claim adds `LooksTests`.
- **:45** (gender): "... premades carry an authored gender; 'Subject #n' is unknown (their look draws a gender)".
- **New "Characters" group after "World"**:
  - "Generated travellers are layered figures (hair back, body, outfit, head, facial hair, hair, headwear, accessory) from the claimed place's wardrobe; skin tone and hair colour follow the claimed place's weights and are never tells; the face follows age (grey from 60); a worn item can hide another (tested: `LooksTests`, `LookKeysTests`)";
  - "Visitor window: the whole figure, opened when the traveller is presented; garments are hoverable and compare-clickable (their item name in the compare bar); a premade is one region";
  - "Passport photo: a 4:5 head-and-shoulders crop of the same figure on the passport's first page; the permit has none";
  - "Character art loads by file name from `Assets/Art/Characters/Resources/Characters`; missing art is drawn as colour-coded placeholders at runtime (placeholder drawing and canvas geometry tested: `LayerPlaceholderTests`, `PixelShapesTests`, `PlaceholderPaletteTests`, `LookCanvasTests`; loading and the fallback are checked in Unity, not by the EditMode suite); the art contract is `docs/CHARACTER_ART_CONTRACT.md`";
  - "Premade characters: eight named figures drawn whole with four expressions; story slots (day 1 slot 3, day 2 slot 6) and a 5% chance per slot from the day's pool; once per run when presented; never on a rule violator's slot; honest unless authored as liars (papers or answers only); their own opener and record note; a premade conversation changes their expression (scheduling tested: `PremadesTests`, `ViolatorSlotsTests`; the opener tested: `InterviewTests`)".
- **:50/:51** (intercom, documents): the planned "photo capture" is superseded by the Visitor window; "white page + photo placeholder" becomes "the passport's first page carries the traveller's photo".
- **:53** (books): add the Costume Guide.
- **:54** (compare): "... a garment shows its item name and matches on its place's Culture value (the compare rule tested: `DiscrepancyLogTests`)".
- **:55-61** (Scanner): statements include a worn garment; proofs "traveller wears ..."; the DRESS label (tested: `DiscrepancyLogTests`).
- **:63** (fallback): "... the traveller's dress ...".
- **:70**: "Scheduled events, forced cases (blueprints) and forced premades that stood in slots never reached ..." (a premade met on an earlier day is not named).
- **:72** (names): "... a premade's name is reserved for their story slot and never used by a generated traveller".
- **:86** (provable tells): add "a dress tell only when a Costume Guide exists, the true home's signature item can be seen over the disguise and is not confusable, and its Culture value differs from the claim's and is unique (tested: `LooksTests`, `LiesTests`)".
- **:95** (generator): "... wardrobes, look weights and rules, the Culture facts derived from wardrobes, premades and each day's premade pool, forced slots (premades and blueprints) and chance; owns `Assets/Data/World/Premades` too" (replacing "never removes hand-authored legendaries").
- **:96** (validator): "Content validator checks every place (five facts plus the derived Culture fact, which must match the wardrobe and be unique; names; birth years; year) and every day plan (today has places; every weighted era has one; every rule can be broken; premades, listed and forced, claim and come from today's places); wardrobes, labels, weights and premades (ids, dates, notes, story slots); a character-art report".
- **:100** (legendaries): replaced by the premade bullet above.

## 4. Out of scope

- **Booth rework** (after Codex's hybrid 3D office lands): walk-in and walk-out, idle frames (the old `idle2`), speech bubbles, final booth scale and camera, `Light2D`.
- **Art production**: applying the brief fixes, the ChatGPT batches, the processing script (alignment, keying, body/head split, hair back, masked colour variants). Final art replaces placeholders with no code change.
- **Piece 5**: Future travellers and wardrobes (contract in §2.20), premade arcs, Industrial and Modern premades, history-driven dress.
- **Piece 6**: UI language for the new text; report lines naming a dress tell.
- **Not scheduled**: Costume Guide pictures (W3); outfit variants per place (`_v{N}` reserved); role-specific accessories (W7); a per-channel tell weight; a separate photo-vs-person mismatch tell; clickable booth figure; dress in the legacy era-pick path beyond drawing the figure.

## 5. Tests (EditMode, `Assets/Tests/EditMode`)

- **`LooksTests`** (new):
  - **Compose draw order (R7)** with `ScriptedRandom`: known gender `[Value skin, Range face, Value hair]` is `Done` after 3 draws; Unknown gender `[Value gender, ...]` after 4; swapping kinds or adding a draw fails;
  - **honest look**: every part's key uses the claim's ids; garments in `LookSlot` order; every garment's `Value` is the claim's Culture value; no tell;
  - **leak**: with `leakFrom`, exactly the home's signature slot (for the traveller's gender) uses the home's ids and value with `IsTell`; every other garment is the claim's; a leaked hairstyle keeps the traveller's colour; a leaked wig has no colour;
  - **wig and beard (R5)**: a wig hair item (claimed or leaked) with a facial-hair item gives an uncoloured hair key and a **coloured** facial-hair key, and that key is in `LookKeys.Required`;
  - **coverage**: a covered item is neither a part nor a garment; a leaked item that covers the claim's hair hides it;
  - **hair back**: present only with `back`, under the body, pointing at the hair garment; absent when the hair is covered;
  - **faces**: ages 18, 34, 35, 59, 60 and 70 pick bands a/b, a/b, c, c, d, d; an unreadable date uses the first band; BCE→CE ages have no year 0;
  - **grey**: at `greyFromAge` the hair, hair back and facial hair are grey, below it the drawn colour;
  - **weights**: a zero skin weight is never drawn; all-zero skin gives tone 3 with no draw; no hair weights gives brown with no draw;
  - **minimal look**: a null wardrobe gives body and head only, no garments, no draws;
  - **`Whole`**: one part, one garment with the claim's value, no draws; `WholeKey` for each expression and neutral for blank or unknown;
  - **`CanLeak`**: one row per §2.4 rule (Unknown gender, absent signature, outfit signature, not leakable, hidden by a claim item, the claim's same-slot item with the same label (case-insensitively), confusable in either order and for the gender or both, the true row);
  - **`CultureValue`**: "m / f", one label when both match (case-insensitively), null when a signature is missing;
  - **`LabelProblems`**: distinct labels → empty; two places with the same men's signature label → one message naming both; a place's accessory labelled like another place's women's signature for women → a message, for men → none (other gender); a label with "/" → a message;
  - **`IsGarmentLayer`**: false for `Body` and `Head`, true for every other layer including `HairBack` and `Whole`;
  - every key `Compose` emits is in `LookKeys.Required(...)` ∪ `Bases(...)`.
- **`LookKeysTests`** (new): one `[TestCase]` per name form (body, head, each garment layer with and without colour, premade); `IsToken` table (lowercase letters and digits only); `GenderToken(Unknown)` throws; `Required` counts for a fixture wardrobe (all colours for non-wig hair and hair back; one uncoloured key each for a wig; facial hair in every colour either way); `Bases` counts (10 bodies, 40 heads for faces a–d); `PremadeSet` gives four names.
- **`PremadesTests`** (new): `SlotSource` decision table (all 8 rows); `IsRollable` (the 4 rows of met × name taken); `Roll`: no candidates → −1 with no draw; `[Value ≥ chance]` → −1 after one draw; `[Value < chance, Range k]` → k after two; the cheat → `[Range k]` only.
- **`LookCanvasTests`** (new): the constants equal `make_guide.py`; `FeetPivotY` = 46/1536; the photo anchors map canvas points (362, 946) and (662, 1321) to box (0, 0) and (1, 1); `PhotoAspect` 0.8.
- **`PixelShapesTests`** (new): the moved `InPolygon` table (inside, outside, on the even-odd edge; the arrow cursor's points) and `InEllipse` rows.
- **`PlaceholderPaletteTests`** (new): `FromHsv` rows: the primaries and secondaries at s = v = 1 (0 → red, 1/3 → green, 2/3 → blue, 1/6 → yellow), s = 0 gives grey at v, v = 0 gives black, h = 1 wraps to red, out-of-range inputs are clamped.
- **`LayerPlaceholderTests`** (new): output length = 256 × 384 × 4; every region has opaque pixels inside the safe area and none outside it; pixels outside the region are transparent; the fill and accent colours appear; each mark changes the head area; deterministic.
- **`LiesTests`** (changed): the fixture gains a Culture book and Culture values (Egypt, twin, Iraq, Italy distinct); the 4-argument `HomeCandidate`s stay:
  - **golden order, dress**: channels [Papers, Answer, Appearance], Iraq with `AppearanceLeakable`; its options end with A:Culture; the script picking the last option gives `Tells == [Culture]`, `ChannelOf(Culture) == Appearance`, `TellValue(Culture)` = Iraq's Culture, `ApplyTo` changes no field, 3 draws;
  - Appearance allowed but `AppearanceLeakable` false → no Culture option;
  - a home eligible only through dress is a candidate with Appearance and not without;
  - **no Appearance channel reproduces piece 3**: for seeds 0..300, channels [Papers, Answer] give identical plans with and without leakable flags and Culture facts;
  - a shared Culture value (R13) removes the option;
  - **premade liar**: a one-entry candidate list at chance 1 with channels [Papers, Answer] and a non-empty `answerTellCategories` gives `Liar`, `HomeIndex 0`, and its options include the Answer ones;
  - `MayLie` table: an honest premade → false; an authored liar premade → true (other rows unchanged);
  - round trip: the Culture tell proves (`DiscrepancyLog.Prove`) as `ForAppearance` ClaimMismatch against the claim row and ForeignOrigin against the home row, nothing against a third row.
- **`DiscrepancyLogTests`** (changed, CRLF, through piece 3's `Prove`/`Add` helper): an Appearance tell vs the claim row → ClaimMismatch, source Appearance, Summary contains `traveller wears: "`; vs a foreign row → ForeignOrigin with `traveller wears "` and "which belongs to"; an honest garment never registers; garment vs a document field or an answer → nothing; `ClueLabels.Report(Culture)` is "DRESS"; `MatchValue`: a side with no evidence returns the shown text, a garment side returns its evidence value ("top hat" shown, "top hat / poke bonnet" returned), a document-field side returns its value.
- **`SeedsTests`** (changed): `LookAndLegendaryStreams_AreDistinct`: for cases 1..20, `ForLooks` and `ForLegendary` of a case are not case seeds and differ from `ForClues`, `ForLies`, `ForDialog` of the same case, from each other and from `ForViolators(d)`; both deterministic.
- **`BirthDatesTests`** (changed): `TryAgeAt` rows: CE (1452 at 1495 → 43), BCE (−1505 at −1470 → 35), across year 0 (−10 at 5 → 14), unreadable → false, `atYear` 0 → false.
- **`ViolatorSlotsTests`** (changed): `Window` rows (0 → 0, 1 → 1, 8 → 4, 10 → 5, 12 → 6); with exclusions, no excluded slot is returned and the count is capped by the remaining pool; with null or empty exclusions, results equal today's golden values.
- **`GatesTests`** (piece 3, changed): `FlagKeys.PremadeMet("x") == "premade:x:met"` (the format saves hold), beside piece 3's `TriggerFired`/`DialogDone` rows.
- **`InterviewTests`** (piece 3, changed): `Opener` with a non-blank authored intro returns it unchanged (with and without a legendary name, any gender); a blank or null authored intro gives piece 3's results.
- **`InterviewDayTests`** (piece 3, changed): a premade-bound dialog is offered only for its premade id; unbound dialogs are offered for any id.
- **`InterviewScriptTests`** (piece 3, changed): an authored node line's expression and a choice line's expression both reach `DialogLine.Expression`; the desk's choice-label line, answer and request lines have none; `DialogChecks.MenuProblems`: with 2 documents, 4 unbound dialogs and 3 premade-bound dialogs the hub is 8 and passes at capacity 8 (the premade dialogs count once); one more unbound dialog fails; 0 premade dialogs add nothing.
- **Unchanged and green**: `ForgeryTests`, `FactTableTests`, `VerdictRulesTests`, `TravellerGendersTests`, `NameRosterTests`, `OutlineMaskTests`, `CursorHotspotTests` and the rest.

## 6. Verification plan

**Offline, after every change:** `compile_check.py` reports 0 errors in every project; the reflection runner reports every Domain and Visuals test passing.

**In the branch's own Unity 6000.4.11f1**, through temporary `_TimeDesk*` `-executeMethod` scripts (patterns from `SCRATCH/prev__TimeDesk*.cs.txt` and the piece-2/3 automation), never committed, reports to the scratchpad:

0. **Baseline, before any piece-4 code** (at the piece-3 head): dump seeds 12345 and 999, days 1–3, with and without Interview Protocols, one line per slot: claim, name, gender, date, role, violator slot, liar, home, tells with channels, paper values and answers.
1. **Content**:
   - Generate World twice; the second run changes no file;
   - 40 places carry `year`, both wardrobes, weights and a Culture fact equal to `Looks.CultureValue`; 40 distinct Culture values, each ≤ `FactTable.MaxValueLength`; `Looks.LabelProblems` over the 40 places is empty;
   - `Assets/Data/World/Premades` holds 8 premades (`oncePerRun` true on all); the library lists them, 6 books and `lookRules`; the day plans hold the §2.14 pools, story slots (premade set, `caseBlueprint` null) and chance, and day 3's channels;
   - a scratch copy of `world_source.json` with one missing key per R29 row (no `premadeChance` on a pooled day, a forced entry without `slot`, an impact without deltas, a face band without faces, a Culture question, two places sharing a men's signature label) is rejected by the generator with the named message, and nothing is written;
   - Validate Content Library reports no issues; its character-art report is logged.
2. **World check** on seeds 12345 and 999 × days 1–3 (with and without Interview Protocols) plus a 200-seed sweep, from `CaseFactory.GenerateDayCases(plan, state, Seeds.Day(seed, d), askable, answerTellCategories, true)`, with both lists from piece 3's `InterviewDay` as in its step 3:
   1. **Determinism**: identical dumps (including looks) on a rerun with the same state; a different seed differs. **With met flags**: a second dump of day 3 with `FlagKeys.PremadeMet("socrates")` and `("khwarizmi")` set (fresh otherwise) differs from the fresh dump only in slots that held or now hold a premade, in later same-day travellers of those slots' places (name, gender) and, for those renamed travellers, their look and day-3 lie and answers; every other slot is identical, and a rerun with the same flags is identical.
   2. **Stream contract** (§1.5, fresh state): every non-premade slot matches the step-0 line in claim, date, role and violator status on all three days; names match up to the day's first premade slot, and every later name difference is a same-place traveller whose name is another unused name of that place; on days 1–2 the lies and the answers match; on day 3 the answers of travellers honest in both runs match; violator slots are identical on all three days.
   3. **Premades**: day 1 slot 3 is Senenmut and day 2 slot 6 Socrates (fresh state); with their met flags set, those slots hold ordinary travellers; random premades appear only on non-violator slots and never twice in a day; with a test copy of day 2 at chance 1, every violator slot still holds its violator.
   4. **Socrates**: a liar, true home Republican Rome, tells on Papers or Answer only, registrable as in piece 3; his record shows his cover.
   5. **Looks**: skin tones and hair colours always have positive weight in the **claim's** table; faces match the cover age; grey from 60; premades have one `Whole` part.
   6. **Honest and non-dress liars**: every garment's source is the claim and none is a tell.
   7. **Dress tells**: exactly one tell garment, the true home's signature item for the gender; `CanLeak` holds; `DiscrepancyLog.Prove(CompareEvidence.ForAppearance(Culture, value, true), row.ToEvidence(), claimNationId, claimEraId)` gives ClaimMismatch against the claim's Culture row, ForeignOrigin with `actualOrigin == trueHomeLabel` against the home's row, nothing against every other row; honest garments register nothing against any row.
   8. **Keys**: every emitted key is in the validator's enumerated set.
   9. **Coverage** (sweep): the share of day-3 liars with a dress tell is reported (expected near one in ten); every day-3 home with a leakable signature appears as a dress home at least once; no dress tell on days 1–2 or with `appearanceReachable` false; no `NoPossibleLie` warning.
3. **EditMode suite** through `TestRunnerApi`: everything passes except the known third-party `UnitySkills.Tests.Core.PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly` (reported, ignored).
4. **Builder**:
   - run Build Office UI and save the scene;
   - `OfficeRoot/Traveller` has a `SortingGroup` (−20), a `TravellerView` with 9 wired renderers and no `SpriteRenderer` of its own;
   - `VisitorWindow` has a `TravellerPortraitView` (9 layers; buttons and highlights on the 7 garment layers, `HairBack` and `Whole` included, none on `Body` and `Head`), an icon `IconVisitor`, and `InvestigationUI` references both the view and its chrome;
   - `DocumentWindowTemplate` has the 4:5 `PhotoBox` with a non-clickable portrait, wired into `DocumentWindowController`;
   - `GameManager.travellerView` is wired; a second build leaves `OfficeScene.unity` byte-identical.
5. **Scripted play-through** (play mode, `[InitializeOnLoad]` + SessionState steps), with screenshots to the scratchpad:
   - day 1, a fresh run: slot 1 (generated) → the Visitor window is open on the desktop with the layered placeholder figure; hovering a garment outlines it; clicking it shows `Visitor · <slot>:  <label>`; the passport shows the photo crop, the permit none; comparing a garment with its own Costume Guide row shows MATCH and logs nothing; Escape shows the booth figure; the decision clears it;
   - slot 3: Senenmut → one whole figure in the Visitor window, the booth and the photo; the hub offers "Ask about the temple >"; "And if she is not?" swaps the picture to worried in both views; his record note shows in Citizen Records; accepting is correct and pays the bonus;
   - a day-3 run with a seed whose queue holds a dress-tell liar early, preferably one whose leaked garment is a lower layer (hair or facial hair) under a claimed headwear or accessory, so the alpha hit test is exercised: click the leaked garment, then the true home's Costume Guide row → "DRESS INCORRECT — traveller wears "...", which belongs to <home>"; the claim row then shows "ALREADY DOCUMENTED"; Deny is correct with no citation;
   - day 2 slot 6, Socrates: his Roman tell is logged from the papers or an answer → Deny correct; the save holds `premade:socrates:met`;
   - `Show(null, art)` (and `Show(null, art, compare)`) on the booth view and the Visitor view, called by the script: every layer is empty or inactive, every button inactive with no listener, no exception;
   - with the scene's `visitorChrome` reference cleared in a test copy, `VisitorReachable` is false, the wiring warning names both fields and no dress tell is generated that day;
   - force closing time; no warnings or errors except the one-time placeholder info line.
6. **Hygiene**: revert unrelated Unity-touched files (`*.csproj`, `ProjectSettings/*`, the TMP fallback atlas); commit the regenerated content and the rebuilt scene; delete the `_TimeDesk*` files and their metas; append the verification record to this spec.

## 7. Risks

- **Stacked on an unimplemented piece.** Piece 3's spec is committed but under review and not implemented; its names (`TellChannel`, `Prove`, `ClueLabels`, `InterviewDay`, `ScriptLine`, `FlagKeys`, `Interview.Opener`, `DialogChecks.MenuProblems`, `GenerateDayCases`) may change. The plan re-reads the committed piece-3 code before anchoring any edit.
- **Scene collision.** `OfficeScene.unity` is heavily edited in both checkouts; Codex's uncommitted hybrid office deactivates `Traveller` and uses a perspective camera (FOV 55 per the critique's re-read). The core of this piece is on the desktop canvas; the booth figure is a small builder block to redo in the booth rework. Codex's scene (art branch, main checkout) and the tracked `Assets/_Recovery/0.unity` both reference `traveller.png` (GUID `04b51760ee3e85041bb372382ecf3eff`), which is why the PNG is kept (R26); the booth rework deletes it together with the stale recovery scene once Codex's scene no longer points at it.
- **Art alignment.** Layers must share the mannequin exactly (review issue 1); misaligned art breaks the booth stack and the photo crop. The importer checks size, not alignment.
- **Readability and fairness.** Placeholders prove the mechanics, not the art: a string-distinct tell can still be hard to see, and small accessories may not read at the Visitor window's size (figure about 270×406 px; maximising helps). Confusable pairs are authored, not measured.
- **Text shortcut.** Pairing every garment with the claim's row finds a dress tell without looking at the art, as pairing every paper field does. R2 keeps the item name, not the Culture value, in the first click, so the shortcut still needs the book.
- **Content volume.** About 300 short item labels and 80 signature picks are authored by hand from long prose, with the 68 fixes applied; the 28-character cap forces terse names ("petasos hat / sakkos snood").
- **Sensitive data.** Skin and hair weights and real-person premades are representation choices; the starter values are marked for Saleh's review. Real figures are long dead and non-rulers; their art needs a chat without the no-likeness clause (review #18). "Socrates" is written as an impostor.
- **Memory.** About 8 textures of 1.5 MiB (plus readable copies) per traveller; `Retain` releases the previous traveller's. A future catalog that references every sprite would load hundreds of MiB.
- **Alpha hit tests** need readable, non-crunched textures. The importer (R10) makes every texture under `Assets/Art/Characters/` readable and uncrunched, `Resources.Load` only reads from there, and placeholders are created readable, so a non-readable garment means a broken import. It is not harmless: with rectangle hits the topmost full-canvas garment takes every click, so a leaked lower garment (hair, facial hair) could not be clicked and its dress tell could not be proven. The view logs an error (§2.9), and §6 step 5 clicks a leaked lower garment.
- **UI crowding.** The Visitor window, documents, books and the transcript overlap on the 1920×1080 desktop; all are draggable.
- **Replay after the end-of-shift save** (§1.8) and **the "(Soldier)" banner** beside civilian dress (W7) are known and accepted.
- **Day-3 difficulty.** Papers, answers and dress on one day with one tell each; the channel knob and `tellCount` tune it.

## Review notes

The piece-4 analysis's completeness check (`piece4_map.json` `critique`) listed missing items and wrong claims. Each is handled or deferred here:

| Critique item | Where |
|---|---|
| Conflicting art contracts (ART_ASSET_LIST, PRODUCTION_PLAN, Tier-1 traveller.png) | W1, R26, §2.13 art contract |
| Scene/art contracts say the booth is empty before READY | C19 + W2 (shown at presentation, after READY) |
| Incoming passport art fixes the photo layout; no Costume Guide cover | C20 + W11, R13; `refbook_cover_culture.png` in the art contract |
| Compare highlight would recolour garments | §2.9 highlight overlays, `Transition.None` |
| What a clicked garment displays | W3, R2 |
| 28-character fact cap | R1 (`FactTable.MaxValueLength`), generator and validator checks |
| Citation copy | R25, §2.16 |
| Premade intro has no surface | §2.8 opener (piece 3's transcript first line), `recordNote` in Citizen Records |
| Generator wiring for premades | R20, §2.13 |
| Forced premade slot plumbing | §2.7 `TryGetForcedPremade`, §2.10 orchestrator warning |
| Name roster | R16 (reservation, pool-name ban) |
| Met guard timing | W8, R16, §1.8 |
| Dev cheat | §2.5 `Premades.Roll(..., forcedByCheat, ...)`, §2.8 |
| Second scene and legacy path | §2.10 optional `travellerView`, legacy clear; `Test_DayLoop` unchanged |
| Looks fallbacks (no place, Unknown gender, no age) | §2.4 minimal look, R7, R8, §2.8 warnings |
| Layer-model gaps (kohl on the face; exposure) | signatures are garment slots only (§2.2); R4 coverage; art-contract rules 6, 14, 15 |
| Signature fixes | R23 import applies all 68 fixes |
| Git LFS | R9 runtime placeholders, nothing committed |
| Unity-side verification | §6 steps 2 and 5 |
| Testability | `Premades`, `LookCanvas`, `PixelShapes`, `LayerPlaceholder` tests; `ViolatorSlotsTests` |
| Piece-3 handoff conflict | R18; §2.20 (arcs); §4 (bubbles) |
| Existing intercom hook ("photo capture") | R12 (auto-open) and §3.3 :50 |
| FEATURES lines to edit | §3.3 |
| Real figures named but omitted | §2.14 (rulers excluded; Modern figures deferred to originals) |
| Photo crop geometry | §1.2, art contract section 2 |
| Premade true place | R17, validator checks |
| `Lies.Plan` ordering | R6 |

Claims the critique marked wrong, as corrected here:
- The hybrid camera's FOV is 55, not 58 (§7), and Codex is still editing that file.
- Auto-opening the Visitor window is a stated exception to FEATURES :35, not a reuse of the Scanner pattern (R12).
- The text fallback cannot produce an unproven denial (the gate is off there); the proof gap exists only in a rich scene without the Visitor window, which R14 closes.
- Moving the roll to `Seeds.ForLegendary` alone does not keep a day stable, because the name roster is shared by the day's slots. R16's pool-name ban and early reservation stop a premade's own name from shifting anyone; a premade slot still leaves one pool name unused, which can rename a later same-place traveller. §1.5 states this and §6 step 2.2 checks exactly that.
- The roll draws only when a valid premade exists (and the cheat skips the chance draw); `Premades.Roll` keeps that.
- The leaked slot needs no draw (R6).
- `apply_fixes.py`'s real blocker is the missing `result` wrapper (art contract section 9).

### Independent review 1 (2026-09-24)

41 findings (F1–F41, in the order received; several arrived twice). Each was checked against the committed piece-3 spec (`fbf4905`) and the worktree code before it was applied. All hold; none is rejected outright. Where a finding offered options, the one chosen is named, and the parts not taken are marked "not taken" with the reason.

| Id | Finding (short) | Verified against | Verdict | Where |
|---|---|---|---|---|
| F1, F27 | `GenerateDayCases` dropped piece 3's `answerTellCategories`; `CaseFactory.Opener` does not exist; the premade-liar `Lies.Plan` call lacked the list | piece-3 spec 490 (`GenerateDayCases(..., askable, answerTellCategories)`), 274-278 (`Lies.Plan`), 327 (`Interview.Opener`) | applied | header; §2.8 signature, opener call and both `Disguise` paths; §2.10 `GameManager` call; §6 step 2 |
| F2, F26 | `TimelineKeys.FiredFlag`/`DialogDoneFlag` do not exist; the met flag would be a second, untestable home for flag names | piece-3 spec 243-245 (`FlagKeys` in `Gates.cs`), 459 (`TimelineKeys` unchanged); `TimelineService.cs:8-27` | applied | R16; §1.8; §2.5 `FlagKeys.PremadeMet`; §2.7 (`TimelineKeys` unchanged); §2.8, §2.10; §2.17; `GatesTests` |
| F3 | Removing `blueprintOverride` breaks piece 3's generator and validator menu checks | piece-3 spec 685, 708 | applied | R15; §2.13 (piece 3's checks, changed); §3.1; §3.2 |
| F4, F32 | Premade `intro`, `recordNote`, `wholeFigureLabel` had no ASCII or length checks; `{name}` read the library's (stale) legendaries | piece-3 R8, R10, R18, spec 686; the note box `OfficeSceneUIBuilder.cs:184` | applied | §2.5 `Premades.MaxNoteLength`; §2.13 generator (premades, looks, piece 3's line-length check) and validator; §2.14 lengths |
| F5, F24 | Writing `forcedCases` from premades only wipes forced blueprints and strands `TryGetForcedCase` | `DayPlanSO.cs:143-159, 251-258`; `CaseFactory.cs:151`; `DayOrchestrator.cs:231-232`; the generator leaves `forcedCases` alone today (`WorldContentGenerator.cs:260-275`); FEATURES :70 | applied, option "add an optional `blueprint` to `days[].forced`" | C16; R20; §2.7; §2.13 source, `LoadAuthored`, checks, `MakeDay`; §2.14; §3.3 :70, :95 |
| F6 | "Answers" and "lies" of non-premade slots cannot be stable on day 3 | piece-3 spec (answers read the lie plan) | applied | §1.5; R19; §6 step 2.2 |
| F7 | FEATURES :44 ("legendaries never lie"), :41 and :96 ("five facts", listed legendaries) left stale | `docs/FEATURES.md:41,44,96` | applied | §3.3 :41, :44, :96 |
| F8 | `ShiftScoring.cs` and `DiscrepancyLogTests.cs` are CRLF; `grep $'\r'` is unreliable; §3.2 wrongly corrected piece 3 | `git ls-files --eol` (w/crlf), `core.autocrlf=true`; piece-3 spec 217 | applied | §2.1 lists; §3.2 bullet removed |
| F9, F38 (part) | §6 step 2.7 called the removed `TryRegister` without the claim ids | piece-3 R17, spec 1119 | applied | §6 step 2.7; `LiesTests` round trip |
| F10 | A fresh `RectOffset(120,0,0,0)` wipes the builder's 6-px row padding | `OfficeSceneUIBuilder.cs:403, 500-504` | applied | R13; §2.11 `DocumentWindowController` |
| F11 | R17 cited the wrong `ContentLibrarySO` lines | `ContentLibrarySO.cs:69-98, 106-120, 142-157` | applied | R17 |
| F12, F41 | Deleting `traveller.png` leaves a dangling reference in the tracked `Assets/_Recovery/0.unity` and in Codex's scene | both reference GUID `04b51760ee3e85041bb372382ecf3eff` (checked in the worktree and the main checkout) | applied, option "keep the PNG until the booth rework" | R26; §2.1; §2.12; §2.13 art contract 8; §2.17; §3.1; §7 |
| F13, F34 (a) | `VisitorReachable` ignored `visitorChrome` | piece-3 F10, spec 575 | applied | R14; §2.11; §2.16 warning; §6 step 5 |
| F34 (b) | "Garment layers" undefined, so `Whole` might not be clickable | — | applied | R3; §2.4 `Looks.IsGarmentLayer`; §2.9; §2.12; `LooksTests`; §6 step 4 |
| F14 | `Show` dereferenced a null look (null-blueprint early return) | `CaseFactory.cs:205-209` | applied | §2.8; §2.9 both views; §2.10; §6 step 5 |
| F15 | `OnDestroy`'s early return would skip the new unsubscribe and `Dispose` | `GameManager.cs:183-194` | applied | §2.10 |
| F16 | The unreached-content warning named forced premades already met | `DayOrchestrator.cs:216-236` (`_worldState` available) | applied | §2.10 (through `Premades.SlotSource`); §3.3 :70 |
| F17 | Expressions on choice lines were dropped silently | piece-3 spec 315, 367 | applied | R18; §2.5 `InterviewScript.Build`; §2.13 source; `InterviewScriptTests` |
| F18 | The premade-intro precedence was a rule in `CaseFactory` glue | piece-3 spec 327 | applied | §2.5 `Interview.Opener(..., authoredIntro)`; §2.8; `InterviewTests`; §2.17 |
| F19 | The FEATURES test claim covered loading that no EditMode test covers | house rule on "(tested: X)" | applied | §3.3 Characters bullet |
| F20 | "No day forces a premade into the first half" was false (day 1 slot 3) | `ViolatorSlots.cs` window (8+1)/2 = 4; day 1 has no rules | applied | §1.5; R19; §2.8 streams table; §2.14 |
| F21 | Moved `SetArray`/`DropMissing` errors would name the wrong tool | `WorldContentGenerator.cs:360, 374` | applied, neutral prefix | R28; §2.12 |
| F22 | Uniqueness was checked only on the whole Culture value, while the player reads one gender's label | R1, §2.4 | applied (every signature label, not only leakable ones, is unique per gender) | R1; §2.4 `Looks.LabelProblems`, `CanLeak` row 6; §2.13; `LooksTests`; §6 step 1 |
| F23 | A wig hair item stripped the facial-hair colour, emitting keys `Required` never lists | §2.3 vs §2.4 step 5 | applied | R5; §2.3; §2.4 step 5; `LooksTests`, `LookKeysTests` |
| F25 | New JSON defaults relied on `JsonUtility` initialisers (piece-3 R27) | piece-3 R27 | applied as a variant: `repeatable` and `skipNationScore` read safely when missing; `premadeChance`, a forced `slot`, impact deltas and face-band faces are required; `minDay`/`maxDay` are **removed** instead of renamed, because the generator-owned day pools already decide the days (the validator had to reconcile the two, `ContentLibraryValidator.cs:291-318, 355-357`) | R15; R29; §2.5 `IsRollable`; §2.7; §2.8; §2.13; §2.14; §3.1 |
| F28 | Met flags change the roster, then names, genders and day-3 lies; piece-3 §1.8/R23 were not listed as superseded | piece-3 spec 170-177; R23 | applied | §1.5; §2.8 streams table; §3.2; §3.3 :11; §6 step 2.1 (met-flag dump); §2.20 |
| F29 | Translating item labels would break the match with the Costume Guide | R1, R2; piece-6 U3 | applied | §2.20 piece 6 |
| F30 | R27's per-item art override and same-art `CanLeak` row would be dead (one Future place per day); piece 4 has no Future marker for the warning exemption | piece-5 draft R1 (one Future place, the leader's), its line 645 | applied | R27; §2.9 placeholder colours; §2.13 validator warning and art contract 4; §2.20 |
| F31 | A Culture-only 28-character cap would be a second home once piece 5 adds a general cap | piece-5 draft R12, R20 (`FactTable.MaxValueLength`) | applied (`FactTable.MaxValueLength`, a code constant like piece 5's) | R1; §2.4; §2.5 `FactTable.cs`; §2.13; §2.15; §2.20 |
| F33 | Piece-3 R25's hub count counted every premade dialog at once, and its legendary-blueprint term refers to the removed override | piece-3 R25, spec 383 | applied | R18; §2.5 `DialogChecks.MenuProblems`; §2.13; `InterviewScriptTests` |
| F35 | Nothing stopped one premade being forced twice in a day, or a forced premade taking a violator's first-half slot | `ResolveGivenName` skips `Reserve` for forced premades (§2.8); `ViolatorSlots.Pick` caps the count | applied | R16; §2.5 `ViolatorSlots.Window`; §2.13 generator and validator; `ViolatorSlotsTests` |
| F36 | `forcedToday` duplicated what the early name reservation already decides | §2.8 reservation before slot 1 | applied (field and parameter dropped; the reservation documented as the mechanism) | R16; §2.5; §2.8 |
| F37 | With threshold 0, the topmost full-canvas garment takes every click | uGUI rectangle raycasts on full-stretch Images | applied: an error instead of a warning, §7 states the importer rules the state out, §6 step 5 clicks a lower-layer leak. Not taken: gating the Appearance channel per traveller, because readability is known only when the texture loads at presentation, after the day is generated | §2.9; §6 step 5; §7 |
| F38 (rest) | `PlaceholderPalette.FromHsv` untested; R2's compare rule in untestable glue; the FEATURES claim | — | applied | §2.5 `CompareEvidence.MatchValue`; §2.6; §2.11; `PlaceholderPaletteTests`, `DiscrepancyLogTests`; §3.3 :54 and Characters bullet |
| F39 | R9 removes C10's editor tool but called itself a refinement | `piece4_decisions.md` C10 | applied, option "record as a deviation" (the thin editor menu is described in R9 for Saleh to choose) | C10 row; R9; W9 row |
| F40 | Empty face bands crash `Compose`; a Culture question would be accepted once the Costume Guide exists | `Compose` indexes `band.faces`; piece-3 `Forgery.IsProvableCategory` | applied | R29; §2.4 `EvidenceCategory` doc; §2.13 generator and validator; §6 step 1 |

Follow-ups for the parallel drafts (not edited here): piece 5's R18 (`artNation` and its `CanLeak` row) and its removal of `Looks.MaxCultureLength` (R20 there) become unnecessary under R27 and `FactTable.MaxValueLength`; its determinism statements should name the met-premade flags; its days 4–6 must author `premadeChance`. Piece 6 already plans to make `ValuesMatch` public; this piece does it first.

This spec has had one independent review (above). The plan's review should append its findings here.
