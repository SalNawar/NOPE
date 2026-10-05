# Travel documents (desk-first track D, items 3 and 13)

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

## Coordination with track A (the ramp)

Track A makes the passport the day-1 paper: it is TC-101. A's day 2 entry ticket is TC-230, day 6
waiver TC-310, day 7 transponder card TC-240 (A wires it into the blueprints, the offices, the
canon and the day's papers: it is in no blueprint yet), day 8 work permit TC-520. The bulletins and
rules that still say "visa", "Departure Manifest", "Labour Contract" or "Displacement Certificate"
are A's to rewrite with the bulletins. When A cuts fields, the forms here keep each remaining
field's cell; re-point the cells after the cut.

## Open questions for Saleh

1. The visa is now the passport's visa page (TC-101). OK, or a separate visa sticker document?
2. The travel permit is the displaced's paper (TC-610). Should tourists carry a travel permit too?
3. Should the machine-readable zone become a forgery check (a doctored page whose zone still
   reads the true number or birth date)?
4. The nations' cover colours and stand-in emblems: keep, or ask the art side for real-looking ones?
