# Travel documents (desk-first track D, items 3 and 13)

> **Superseded in part (2026-10-06):** the passport's machine-readable zone (TD3, decision 3) is gone and every compared agency number is short (Saleh: "there is a long number on the passport and many <<<<. ... let's simplify"); the nation's three-letter code prints under the emblem instead. See `2026-10-06-controls-design.md` §4.2.

Saleh, 2026-10-05: "papers should have distinct colors & shape" and "documents need to start
looking like actual travel documents: passports, travel permits, entry tickets, waivers etc".
Built on the document design (`2026-09-30-document-design.md`: FormLook, FormLayout's fixed
fields, the offices' seals). Decided on Saleh's behalf (the desk-first run's "no questions" rule);
each decision is his to overturn.

## TD1: six travel documents, each its own size, aspect, colour and silhouette

The existing templates keep their numbers, their fields, the fields' order (ids), categories and
labels, so every fault, record, request and finding keeps working; only their forms, looks and
three names change. One template is new.

| Document | Template | Frame (`FormFrame`) | Aspect | Size on the desk | Paper / accent | Silhouette |
|---|---|---|---|---|---|---|
| Passport | TC-101 (was the Leisure Departure Visa) | `Booklet` | 0.66 | 0.64 (a booklet) | ivory / the holder's nation's cover | the cover's edge round the pages, the spine across, the emblem, the zone |
| Entry Ticket | TC-230 (was the Departure Manifest) | `Ticket` | 1.15 (landscape) | 0.50 (a stub) | pale green / green | a landscape stub: the stub band and its perforation, a barcode with the ticket number |
| Waiver | TC-310 Stranding Waiver | `Framed` | 0.56 (a long sheet) | 1.30 (the largest) | pale rose / oxblood | a long legal sheet: the certificate's border, three paragraphs of fine print, the signature |
| Work Permit | TC-520 (was the Labour Contract) | `BottomBand` (letterhead) | 0.765 | 1.12 | white / black | a letterhead with the issuing office's seal as a watermark |
| Travel Permit | TC-610 (was the Displacement Certificate) | `Folded` | 1.00 (square) | 0.60 | pale blue card / slate | a folded card: a top band and the crease down its middle |
| Transponder Card | TC-240 (new) | `Card` | 1.586 (ID-1) | 0.36 (the smallest) | pearl / teal | a plastic card: rounded corners, a sheen band, a gold chip |

The proofs of means (TC-415 to TC-417), the intake declaration and the return order (TC-620,
TC-630) and the PC's page kinds (the Directive Memo, the Record Extract, the Statement, the
Register, the Seal Register) keep their looks.

New blocks (`FormBlockKind`, appended: 17 to 20): `Fold` (a booklet's spine at `shares[0]` of the
page), `Mrz` (the machine-readable zone), `Visa` (a visa page: the stamp area down to the bottom
margin, its caption inside), `Watermark` (the field's seal, else the nation's emblem, faint and
centred in the room below it). A `chip` cell (`FormSlots.Chip`) draws a card's chip. The new
frames draw only in the margins, except the folded card's crease, a faint shade and highlight down
the middle over the boxes (as a fold shows through print). A watermark is drawn over the boxes' fills,
under the lines and the words, so no box hides it.
Every look passes the forms' contrast pairs and every seal ink (Build Office UI checks them).

## TD2: every paper prints at one size; a long sheet is longer, a card shorter

Before, a look could only be narrower than the style's page and the PC drew it narrower at the same
height. Now the print unit is the page's width over the style's aspect (`FormLayout.PrintUnit`)
for every paper: on the PC each document is drawn 520 u wide like before, a long sheet taller (it
scrolls in its pane), a ticket or a card shorter. On the desk the paper's height is the desk paper's
times its scale and its width its aspect; held in the hand it fits the slot's height, or its width
for a paper wider than the desk's (`PaperExaminer`), so a ticket or a card reads at the desk paper's
size and the long waiver slightly smaller (0.81). Looks may set an aspect from 0.5 to 2 and a scale
from 0.3 to 1.3 (`FormLook.Problems`).

## TD3: the passport

- **The booklet.** TC-101 prints its data page (the name beside the photo, the Citizen ID as the
  passport number, the birth date and the expiry, then the machine-readable zone), the spine at
  0.57 of the page, and the visa page below it (Destination, Visa Class, then the visa stamp area
  where APPROVED or DENIED lands). The header keeps the seal's side at its left for the emblem.
- **The holder's nation.** `CaseInstance.passportNation`: a 2150 citizen's family country (the
  name list their name came from, a story character's family), anyone else's stated home. Its
  passport look is content: `world_source.json` `countries[].passport` `{cover, emblem, code}`
  (sheet `countries`, `NationSO.passport`), checked by Generate World (`PassportCovers.Problems`:
  a dark enough "#RRGGBB" cover, a drawn emblem, three capitals, none shared). The cover paints the
  booklet's edge (`FormData.Cover`) and inks the emblem; the emblem (`EmblemShapes`, code-drawn
  until the art: Egypt a winged sun, Iraq an eight-pointed star, Greece a laurel, Italy a star,
  China five stars, Japan a chrysanthemum, Britain a crown, Germany an eagle) sits at the header's
  left and as the visa page's watermark.
- **The machine-readable zone** (`MachineZone`): two lines of 36 in ICAO 9303 TD2 style with real
  check digits, built from what the page prints, so it never contradicts the page. It is not a
  check of its own (see the open questions).

## TD4: the stamps (track B presses them)

`StampSpots` places a mark: `Next(form, index, aspect)` in the paper's largest stamp area (the
passport's visa page, a form's footer box; the page's bottom right without one), in rows from
its top left, each mark beside the last with a gap; `At(form, x, y, aspect)` centred on a pressed point, kept on the page.
`DeskDocument.Stamp(approved, formPoint?)` prints it: the art's mark (`ArtSlots.VerdictMark`), else
a code-drawn stand-in (a double frame and the style's word, `FormStyleSO.approvedStamp` /
`deniedStamp`, in green or red, tilted). The verdict's ink as the papers leave (`ShowVerdict`) now
uses it, so the stand-in shows where nothing showed without the art.

## TD5: art (the ChatGPT side)

`docs/ART_ASSET_LIST.md`, section "Travel documents": the eight passport emblems
(`Forms/emblem_<emblem>`), a passport page per nation (`Forms/paper_tc101_<nation>`, tried before
the kind's face), the paper faces of the six documents (`Forms/paper_tc101` ... `paper_tc240`),
the two stamps, and the closed passport covers (for a later closed-booklet view; not drawn yet).

## TD6: the Canva documents (run 7, 2026-10-07)

Saleh: "Use Canva to design the documents, they look bad"; he approved the futuristic passport ("much better"), then "yes, do all documents in this style", "folder too", "go". Decided and built by Claude without questions (each decision is Saleh's to overturn):

1. **The art is the face; the game prints only what changes.** A template's look carries `FormArt` (`FormLook.art`): every field's value place, its baked label's place, the photo window, the seal spot, the visa box, the prints that are no field, as shares of the face. Set, `ArtLayout` replaces the blocks' layout (the blocks still name the seal, the photo and the signed fields, and the pages). The desk paper and the PC copy draw the same placed form, so nothing else changes: the slots (a click box is the value's place), the compare, the scanner's marks, the stamps (`StampSpots`), the hidden fields, the faults (a doctored value prints as the page shows it, a wrong photo in the window, a forged seal's own outline at the spot).
2. **The art's labels and the template's fields disagree on five papers** (the art was drawn before the fields were known). The data stays (every field, category, label and value: no case, fault or hash moves). Where an art row names a different fact, its label is painted out of the face and the game prints the template's label there (`relabel`) in the art's label ink and condensed capitals; where an art row has no field on that paper, it is painted out. Kept as printed: CITIZEN ID / REGISTRY ID over the displaced's Displacement No. (the same category: the agency's number for the person), TRANSPONDER MODEL over the Transponder, PLACE OF WORK (ERA) over the Worksite, RETURN BY (DATE) over the Departure. Relabelled: the ticket's PASSENGER, DESTINATION (ERA), PORTAL / GATE and TRAVEL CLASS rows (Transponder, Currency Carried, Declared Effects, Transponder Class), the card's TRAVEL CLASS (Transponder Class: the passport's Travel Class is the account's status, another fact, so the two never read alike), the waiver's DESTINATION (ERA) and DEPARTURE DATE (Transponder, Debt Passed to Kin), the travel permit's DESTINATION (Date of Birth), the return order's CURRENT ERA and RETURN GATE (Return To, Incident).
3. **Prints that are no field** say nothing a field does not: the passport's NATIONALITY prints the holder's nation's three-letter code, its SIGNATURE and the intake's DECLARANT SIGNATURE the name the page shows (doctored with it), the ticket stub's DATE the ticket's departure date and its GATE "DESK 3" (the rulebook folder's desk), the return order's ORDER No. the paper's serial, AUTHORISED BY the issuing office's name in a hand. None is picked or compared. The waiver's printed conditions are the art's three paragraphs (the template's paragraphs stay in the asset for the narrative workbook).
4. **The passport is an open booklet stacked, not side by side**: the visa page above the data page (as a real passport opens), each page the art's 1.29, the booklet 0.65. Side by side (2.6 wide) would read at the desk paper's width, the data page at half the size of the ticket; stacked it reads at the reading height like before. The visa page is the data page's paper with its content painted out; the code draws the ENTRY VISA · VISAS D'ENTRÉE box in the kit's oxblood (#8A2F3B, `FormArt.stampInk`) with the holder's nation's emblem faint inside, the cover margin in the nation's colour and the spine. A stamp still prints where it is pressed (the controls spec, §6); the box is the guide and where the verdict's ink goes.
5. **Values** print in GeistMono Bold (`FormStyleSO.artValueFont`, the kit's monospace: the typewriter) in each art's dark ink (slate navy on the passport, the card and the permits, ink on the ticket, oxblood-black on the waiver, near-black on the return order), centred down their place and shrunk to fit it down to half their size (the check measures every longest value at that floor). The relabels in BigShoulders Bold (`artLabelFont`). Hands (signatures, the office) in italics.
6. **Photos**: the art's sample portraits are painted out; the traveller's 4:5 photo fits each window (a card's narrower window shows its holo ground above and below) under a holographic laminate (`Forms/photo_holo`). The passport's ghost portrait is painted out (no second photo is drawn). The work permit's art has no window: the photo sits at the right of AUTHORISED BY, the seal in the art's circle. A seal on a paper whose art has no circle sits where official seals go: over the passport's signature row's right, the ticket's guilloche ring, the card's photo corner, the intake's empty checkbox half.
7. **Sizes**: each paper takes its art's aspect (TD1's aspects move a little: the waiver 0.56 to 0.75, the ticket 1.15 to 1.29, the card 1.586 to 1.75, the travel permit 1.0 to 1.08, the passport 0.66 to 0.65); its height on the desk (the look's scale) is unchanged. Round corners come from the art (`FormArt.corner`: the desk paper's mesh; the PC's face is clear outside them).
8. **ID photos are taken in 2150** (Saleh 2026-10-07: no era costume in a passport picture): `Looks.PhotoLook` keeps the body, the head and the facial hair, puts on the 2150 civilian outfit of the traveller's kind (`outfit_{m|f}_civil_2150_v1` a tourist, `v2` a labourer, `v3` the displaced) and the civilian hair in their own colour (`hair_{m|f}_civil_2150_{colour}`), and drops the headwear and the accessory; a premade's photo is `premade_{id}_photo`. Until that art lands, `CharacterArtFallbackSO` stands in (the `civil` row's neighbours' latest-era outfit and hair, `LookArtFallbackStep.CivilDress`; a premade's neutral picture cropped to the head and neck), so the delivery needs no code. The identity is the look's: the photo check and a stranger's photo are unchanged.
9. **The Citation (TC-900) replaces the citation slip**: one per wrong decision, a row per violation (the verdict's `citationReason` and the first line of its `citationDetail`), its one penalty on row 1 (the others read INCL.), the total the verdict's `citationConsequence`; more than four rows print a continuation sheet (CONT. 2/2, the total on the last sheet only, the earlier ones SEE NEXT SHEET). It flies onto the desk (`PaperArrival`: slide, twist and tumble, a springy settle, a thud and its shadow; `PaperFlight`), the Helix River pulses as it lands, and it stays in the papers' stack for the day (`DeskConfigSO.citation*`). Values in dark navy typewriter ink; the CITED stamp is baked into the art.
10. **The rulebook is Saleh's Canva folder** ("folder too"): open on the desk left of the papers (`DeskConfigSO.rulebookAt` -0.25, -0.06), its page printed on the right sheet over the art's lines (TODAY'S RULES, six rows), the heading and line numbers painted out and printed live (the language and the Translation Lens keep working). Each tab is its own cut-out (`Forms/rulebook_tab_*`, its word printed by the game in the type scale), shown only while its page is available (SEALS from `Feature.Book(Seal)`; GUIDE from the start, there is no guide feature to introduce it), the open one as drawn, a shut one a shade darker, with U-OFFICE's NEW badge. The reading view's paper spots moved right (viewport x 0.62, 0.78, 0.70) so the folder's page and a paper read side by side; a Citation lands on the mat's far half right of centre (`citationSpot` 0, 0.14, at `citationScale` 0.75), wholly on the screen in the office view and the reading view at 1080p and 720p (the probe measures its corners), with `citation_land` and `FeelDirector.Hit` (0.25) as it touches.

## Coordination with track A (the ramp)

Track A makes the passport the day-1 paper: it is TC-101. A's day 2 entry ticket is TC-230, day 6
waiver TC-310, day 7 transponder card TC-240 (A wires it into the blueprints, the offices, the
canon and the day's papers: it is in no blueprint yet), day 8 work permit TC-520. The bulletins and
rules that still say "visa", "Departure Manifest", "Labour Contract" or "Displacement Certificate"
are A's to rewrite with the bulletins. When A cuts fields, the forms here keep each remaining
field's cell; re-point the cells after the cut.

## Decisions (recorded as the defaults, 2026-10-05; Saleh's to overturn)

1. The visa stays the passport's visa page (TC-101); there is no separate visa sticker.
2. Tourists carry no travel permit: the Travel Permit (TC-610) is the displaced's paper.
3. The machine-readable zone is not a forgery check (streamlining): it only repeats the page.
4. The code-drawn covers and emblems stay as stand-ins; the art request (docs/ART_ASSET_LIST.md,
   "Travel documents") stands.

Track B (the desk stamps) presses its marks with `DeskDocument.Stamp` and `StampSpots` (TD4).
