# The documents (wave 5 track B): silhouettes, fixed fields, seals, photos, the fault canon

Saleh, 2026-09-30: apply the Papers, Please lessons to Time Sorter; this track owns the documents
(D1, D2, D3, D4, D8, D9). Decided on his behalf (autonomous run); each decision below is his to
overturn. The canon and the field audit are in `docs/DOCUMENT_FAULTS.md`.

## D1: each document type has its own silhouette

A form's look (`FormLook`, held by the form itself, `FormSpec.look`, so a paper and its scanned copy
share it) sets:

| Knob | What it does |
|---|---|
| `frame` (`FormFrame`) | coloured bands in the page's margins: `TopBand`, `SideBand`, `Framed` (a certificate's border), `Ticket` (a stub band and a perforation), `BottomBand` (a letterhead's foot and head); `Plain` for none |
| `accent` | the bands' colour; the section bars take a pale shade of it (16 % over the paper) |
| `paper` | the paper's tint (the box fills a lighter shade of it) |
| `aspect` | the page's width over its height, at most the style's 0.765: a narrower page on the desk and on the PC (the PC draws it narrower at the same page height, so the print keeps its size) |
| `scale` | the paper's height on the desk relative to Desk_Default's paper (0.7 to 1.3); held in the hand or scanned, every paper reads at the same size |

Plus the emblem: the issuing office's seal in the header's right (D4), a different outline and ink per
office. The ten traveller forms:

| Form | Frame | Accent | Paper | Aspect | Size |
|---|---|---|---|---|---|
| TC-101 visa | top band | navy | cool white | 0.72 | 0.86 (a card) |
| TC-230 manifest | ticket stub + perforation | green | pale green | 0.765 | 0.95 |
| TC-310 waiver | certificate frame | oxblood | pale rose | 0.765 | 1.05 (legal) |
| TC-415 credit agreement | side band | plum | pale lilac | 0.765 | 0.93 |
| TC-416 proof of funds | letterhead | plum | pale lilac | 0.765 | 0.90 |
| TC-417 insurance | top band | umber | cream | 0.765 | 0.90 |
| TC-520 labour contract | letterhead | black | grey | 0.765 | 1.12 (large) |
| TC-610 displacement certificate | certificate frame | slate | cool white | 0.72 | 1.00 |
| TC-620 intake declaration | side band | slate | off-white | 0.765 | 0.95 |
| TC-630 return order | top band | rust | peach | 0.765 | 0.92 |

The PC page kinds: the Directive Memo (today's rules) a red top band, the Record Extract (citizen
record, agency account extract) a navy side band, the Statement (the agency account) a green
letterhead, the Register (every reference book) and the Seal Register an umber certificate frame.
Aspects stay at or below the style's (0.72 at the narrowest) because the half-width boxes hold a
28-character value on two lines at the value floor only at about that width; the frames and tints
carry most of the recognition. The citation slip is an art slot (`ArtSlots.CitationSlip`), not a
form, and stays track C's.

Contrast: every look's palette passes the forms' pairs (`FormContrast.Problems`), and every seal ink
reads at the body-text minimum on every look's paper (`FormContrast.SealProblems`), checked by Build
Office UI. No text is ever printed on a band.

## D2: field positions are fixed per type

A document's box keeps the room its field's longest value needs (`FormData.FieldReserve` =
`FormLayout.Reserve` of `FieldLengths.Longest`; the Destination's longest is the content's longest
origin label, `ContentLibrarySO.LongestOriginLabel`), whatever it shows, so no field moves when a
value changes. A value that would still need more room shrinks below the floor (to 70 % of it)
instead of moving its box. `FormLayout.Check` lays each template out with its longest values and with
none and fails (Build Office UI and the validator) if any box moved; its old checks (a value past two
lines at the floor, a page overflowing its margin) now run at the look's aspect. Culture themes never
touch a form (`ThemeRoleId.DiegeticForm`; the desk paper carries no theme tag), and the only per-script
font on a form is the transcript's (a PC page kind, not a document), so nothing on a document reflows.

## D3: every field audited

`docs/DOCUMENT_FAULTS.md` lists every field of every document, what it is checked against, and which
canon faults can print there. Names are the record lookup key (never a tell). Fifteen fields are
checkable but never faulted, kept as **deliberate decoys** (a Citizen ID or Displacement No. on a
secondary paper, a repeated destination, an incident number): none was removed, since each ties its
paper to the traveller or repeats the claim, and removing one would change the layouts and the golden
masters for no new check. Whether any should become faultable is an open question.

## D4: seals

Each issuing office (`agency.offices`: eight, each issuing one to two forms) prints its seal on its
forms: an outline (`SealShape`: circle, hexagon, shield, diamond, octagon, square) in an ink
(`SealInk`: six dark inks) with a two-letter legend inside; no two offices share an outline and ink.
The office's name is the form's programme line. The seal sits in a box at the header's right (the
header's words stay clear of it) and is a field of the form (category `Seal`, "Issuing Seal"), so it
is picked like any value; its canonical value is its description ("Blue hexagon · VO"). The seals are
drawn by code (`SealOutlines`, `SealArt`: a ring and a hairline, tinted by the ink); the legend is
printed at 0.42 of the seal's side, about 14 px on a paper held at 720p.

The **Seal Register** is a reference book (`RefBook_Seals`, category `Seal`, the 7th book, TC-917),
drawn on its own page kind (`Form_SealRegister`: a `SealGrid` block, three seals to a row, each over
its office's name), each seal a pick (`EvidencePicks.ForSeal`, `PickKeys.Seal(office)`: the comparable
id track A's click-and-match needs). A paper's seal against its office's row: *Matching data* when
it is true; *Data differs* when forged (a `ClaimMismatch` proof, "SEAL INCORRECT — papers: … /
expected: …"). A paper carrying another office's true seal proves against that office's row (a
`ForeignOrigin` proof, "which belongs to Labour Placement Bureau"). Seals never cross-prove between two
papers (each is its own office's; `PaperChecks.IsCompared` excludes them, and so does the Analysis
Scanner).

The lie **ForgedSeal** (L11, every kind, from day 3): one of the traveller's papers carries a seal
that differs from its office's in one part: another outline, another ink, or the office's authored
wrong legend (`agency.offices.forgedLegend`, e.g. VO → VD). Every value printed is true. It draws on
the lie stream after the look: the variant (among shape, ink, legend), the paper, the other outline
or ink (`VisualLies.PlanSeal`).

## D8: photo vs the person

The lie **SwappedPhoto** (L12, every kind, from day 2): the primary form's photo (TC-101, TC-520,
TC-610) shows someone else, drawn from the same ChatGPT layer set: the same clothes, another skin
tone at least two steps from theirs and another hair colour (`Looks.Stranger`). The photo is a field
(category `Photo`, its value who it shows, `Looks.IdentityKey`), picked on the paper like a value
(shown as "the photo"). The wheel's **Look** menu now offers **Their face** first whenever a paper
shows a photo (the same entry for every traveller); it puts the person at the desk into the compare
(`EvidencePicks.ForFace`, `EvidenceKind.Person`). Photo against face: *Matching data* for their own
photo; for a stranger's, a `PersonMismatch` proof ("PHOTO INCORRECT — the photo on the papers is not
the traveller at the desk"). Skin and hair colour stay no evidence of *origin* (the art contract's
rule): here they only tell two people apart. A premade's whole picture is never swapped.

## D9: the published fault canon

`world_source.json` `agency.faults` (55 rows): per row a document (or any paper, for a seal), a field,
the lie or directive, the variant, what proves it (the player's words) and what the forger did. The
record lies' variants moved from code into it unchanged (the generation of the existing lies is the
same draw for draw); the visual lies, the place lies' tell fields and the directives' date and paper
faults are rows too. The case factory draws only from it (see `docs/DOCUMENT_FAULTS.md`); the content
checks (`DocumentContentChecks`, in Generate World and the validator) refuse a canon that is unsound or
that leaves out a field a maker could write. The narrative workbook shows it on a read-only `Faults`
sheet.

## Balance

The liar chance per traveller is unchanged (each blueprint's `contradictionChance`, the knobs Saleh
already tunes). What changes is the mix: day 2 draws among five lies (the photo added), days 3-15
among the day's lies plus the photo and the seal, so the older lies are drawn a little less often.
The one-penalty rule holds: a missed seal or photo is "Approved forged papers." (`Faults.Forged`),
the same fine as any mistake. The balance simulation's numbers are in the track's report.

## Open questions for Saleh

1. The photo mismatch differs in skin tone and hair colour (the art has one face per skin). OK, or
   only hair/headwear once more faces exist?
2. The days: the photo from day 2 and the seal from day 3 (track C sequences one new check per day and
   may move them).
3. Should any of the 15 deliberate decoys (secondary Citizen IDs, repeated destinations, incident
   numbers) become faultable?
4. Seal art: the seals are code-drawn placeholders; ChatGPT art per office could replace them
   (`SealArt` is the one place to swap).
