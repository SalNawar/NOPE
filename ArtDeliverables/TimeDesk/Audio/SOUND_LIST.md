# Time Sorter: sound list

Drop each file into `ArtDeliverables/TimeDesk/Audio/` under the exact name in the **File** column. The game picks it up by name, so no code changes are needed. A file that isn't there yet plays a generated placeholder, or nothing.

**Format**
- WAV, 48 kHz, 16- or 24-bit, mono. The ambience beds may be stereo.
- Trim the silence at the start: the sound must begin within 5 ms.
- Peak around -3 dBFS, no limiter pumping. One-shots should be dry (no reverb); the game adds the room.

**Variants**
- Where a row says ×3, supply `_v1`, `_v2` and `_v3` (e.g. `paper_drop_v1.wav`). The game rotates them and nudges pitch ±4%.

**Style reference**
- The stamp Saleh is generating with GPT is the reference for the whole set. Everything else is matched to it in weight, loudness and room.
- The overall tone is a heavy, analogue, 1980s office in a worn-out future: paper, wood, bakelite, metal, relays, CRT hum. No glossy modern UI blips.

**Priority:** P1 = the game feels empty without it. P2 = big improvement. P3 = polish.

## 1. Desk: papers and objects

| # | File | When it plays | Length | Character | Var | P |
|---|---|---|---|---|---|---|
| 1 | `stamp_approve` | APPROVED stamp hits the passport | 0.3 s | the reference stamp: heavy thunk with a rubber slap and a little wood resonance | ×3 | P1 |
| 2 | `stamp_deny` | DENIED stamp hits | 0.3 s | same family as 1, slightly lower and harder | ×3 | P1 |
| 3 | `stamp_miss` | stamp hits bare desk (no paper) | 0.25 s | dull wood knock, no paper slap | ×2 | P2 |
| 4 | `stamp_bar_out` | the stamp bar slides out | 0.4 s | wooden drawer on metal runners, ends in a soft stop | ×1 | P1 |
| 5 | `stamp_bar_in` | stamp bar slides back | 0.35 s | the reverse, ends in a firmer click | ×1 | P1 |
| 6 | `stamp_lift` | a stamp is raised before the slam | 0.15 s | small creak/whoosh, anticipation | ×2 | P3 |
| 7 | `paper_pickup` | a paper is picked up | 0.2 s | a crisp paper lift | ×3 | P1 |
| 8 | `paper_drop` | paper dropped on the desk | 0.3 s | soft paper flop | ×3 | P1 |
| 9 | `paper_slide` | papers slide across the counter / hand-back | 0.5 s | paper dragged across wood | ×3 | P1 |
| 10 | `passport_open` | passport booklet opened | 0.3 s | stiff booklet page, laminate crackle | ×2 | P2 |
| 11 | `passport_close` | booklet closed | 0.25 s | soft clap | ×2 | P2 |
| 12 | `card_drop` | plastic card (transponder, ID) dropped | 0.2 s | light plastic clack on wood | ×3 | P2 |
| 13 | `rulebook_page` | rulebook tab / page turn | 0.3 s | folder page turn, manila card | ×3 | P1 |
| 14 | `rulebook_drop` | rulebook dropped on desk | 0.35 s | heavier folder thump | ×2 | P2 |
| 15 | `citation_print` | the citation prints out | 1.2 s | dot-matrix/receipt printer burst | ×1 | P1 |
| 16 | `citation_tear` | citation torn off the printer / stub torn | 0.4 s | paper tear along perforation | ×2 | P1 |
| 17 | `citation_land` | citation lands on the desk after its twist | 0.3 s | paper slap, a little louder than paper_drop | ×2 | P1 |
| 18 | `newspaper_unfold` | morning newspaper opens | 0.8 s | big newsprint rustle | ×2 | P2 |
| 19 | `scanner_start` | paper placed on scanner bed | 0.3 s | relay click plus motor start | ×1 | P1 |
| 20 | `scanner_sweep` | scan light sweeps (loopable 1 s) | 1.0 s | mechanical carriage whirr | loop | P1 |
| 21 | `scanner_done` | scan finished | 0.4 s | two-tone confirmation beep, retro | ×1 | P1 |
| 22 | `scanner_flag` | scan finds a fault | 0.5 s | low buzzer | ×1 | P1 |
| 23 | `inspect_on` | inspect mode toggled on | 0.3 s | lamp switch click plus faint hum | ×1 | P2 |
| 24 | `inspect_link` | two fields linked in inspect mode | 0.2 s | soft marker tick | ×3 | P2 |
| 25 | `inspect_match` | a discrepancy confirmed | 0.5 s | sharp "found it" ding, typewriter bell | ×1 | P1 |
| 26 | `till_open` | cash till opens | 0.5 s | mechanical cash drawer ring | ×1 | P2 |
| 27 | `coins` | credits paid into the till | 0.6 s | coins dropping into a tray | ×3 | P2 |
| 28 | `mug_set` | coffee mug put down | 0.2 s | ceramic on wood | ×2 | P3 |
| 29 | `prop_click` | other desk props clicked (plant, photo) | 0.2 s | small thud | ×3 | P3 |
| 78 | `dater_wheel_click` | a dater's date wheel turned one notch | 0.03 s | small metal ratchet click, like a date stamp's band | ×3 | P2 |
| 79 | `dater_reink` | a dater pressed on its ink pad | 0.2 s | soft wet squish of a rubber die on a felt pad | ×2 | P2 |

## 2. The booth and travellers

| # | File | When it plays | Length | Character | Var | P |
|---|---|---|---|---|---|---|
| 30 | `call_next` | the next traveller is called | 0.8 s | intercom buzz plus a short "Next." in a tired clerk voice (or a buzz only) | ×2 | P1 |
| 31 | `available_on` | the AVAILABLE sign turns on | 0.4 s | neon/relay clunk plus a buzz | ×1 | P2 |
| 32 | `footsteps_arrive` | a traveller walks up | 1.5 s | footsteps on stone, approaching | ×3 | P2 |
| 33 | `footsteps_leave` | a traveller walks away | 1.5 s | footsteps receding | ×3 | P2 |
| 34 | `booth_shutter_open` | shift starts, shutter goes up | 1.2 s | metal roller shutter | ×1 | P1 |
| 35 | `booth_shutter_close` | shift ends, shutter down | 1.2 s | metal roller shutter down, final clang | ×1 | P1 |
| 36 | `portal_through` | an approved traveller goes through the portal | 1.5 s | rising warp whoosh, tonal | ×2 | P2 |
| 37 | `detain` | a traveller is detained | 1.0 s | alarm chirp plus guard boots | ×1 | P2 |
| 80 | `lever_ratchet` | the gate lever pulled through each notch | 0.05 s | heavy iron ratchet tooth | ×3 | P2 |
| 81 | `lever_home` | the gate lever springs back home | 0.3 s | deep metal thunk with a little ring | ×1 | P2 |
| 82 | `return_buzz` | RETURN sends a denied traveller back | 0.5 s | button clunk plus a short low buzzer | ×1 | P2 |
| 38 | `dialogue_blip` | each line of dialogue appears | 0.05 s | tiny voice-like blip (Papers, Please murmur) | ×4 | P2 |

## 3. The PC (terminal)

| # | File | When it plays | Length | Character | Var | P |
|---|---|---|---|---|---|---|
| 39 | `pc_on` | the PC wakes / monitor zoom-in | 1.0 s | CRT power-on thump plus a high whine | ×1 | P1 |
| 40 | `pc_off` | leave the PC | 0.6 s | CRT collapse | ×1 | P2 |
| 41 | `key_tap` | typing / keyboard actions | 0.08 s | mechanical key | ×5 | P1 |
| 42 | `mouse_click` | clicks on the PC screen | 0.05 s | old mouse button | ×3 | P1 |
| 43 | `window_open` | a PC window opens | 0.25 s | soft retro UI "pop" | ×1 | P1 |
| 44 | `window_close` | a window closes | 0.2 s | reverse pop | ×1 | P1 |
| 45 | `pc_error` | invalid action | 0.3 s | low two-note error | ×1 | P1 |
| 46 | `pc_notify` | new message / order arrives | 0.5 s | three-note chime | ×1 | P2 |
| 47 | `purchase` | an upgrade/order bought | 0.6 s | register "ka-ching", digital | ×1 | P2 |
| 77 | `evidence_pin` | a finding pinned on the PC's case board (the scanner app) | 0.3 s | a pin pushed into a corkboard, a soft thunk | ×2 | P2 |

## 4. Interface (menus, buttons)

| # | File | When it plays | Length | Character | Var | P |
|---|---|---|---|---|---|---|
| 48 | `ui_hover` | pointer over a button | 0.04 s | very soft tick | ×2 | P2 |
| 49 | `ui_press` | button pressed down | 0.06 s | plastic switch down | ×2 | P1 |
| 50 | `ui_release` | button released (the action) | 0.08 s | switch up with a little spring | ×2 | P1 |
| 51 | `ui_toggle` | toggle / segmented control | 0.15 s | chunky slider switch | ×2 | P1 |
| 52 | `ui_tab` | tab change | 0.1 s | card flick | ×2 | P2 |
| 53 | `ui_error` | locked button pressed | 0.2 s | dull "nope" thunk | ×1 | P1 |
| 54 | `ui_popup` | a pop-up appears | 0.25 s | soft paper pop | ×1 | P2 |
| 55 | `wheel_open` | dialogue wheel opens | 0.3 s | quick fan-out flutter | ×1 | P2 |
| 83 | `slot_lever` | the Home slot machine's lever clicks past a ratchet notch | 0.05 s | one heavy metal ratchet tooth | ×2 | P1 |
| 84 | `slot_spin` | the slot machine's reels start spinning | 0.6 s | a motor whirr spinning up, a mechanical rattle | ×1 | P2 |
| 85 | `slot_stop` | a reel lands on the payline | 0.15 s | a solid clunk, a reel's stop catching | ×3 | P1 |
| 86 | `slot_win` | a winning spin lands | 1.2 s | a bright arcade bell ringing out | ×1 | P1 |
| 87 | `slot_lose` | a spin lands on nothing | 0.6 s | a soft descending "womp" | ×1 | P2 |

## 5. Shift and time

| # | File | When it plays | Length | Character | Var | P |
|---|---|---|---|---|---|---|
| 56 | `clock_tick` | desk clock tick (very quiet, only near the clock) | 0.03 s | mechanical tick | ×2 | P3 |
| 57 | `last_hour_alarm` | the last hour of the shift begins | 1.5 s | an alarm ding: an old wind-up desk alarm or a two-tone ding-ding, clear but not shrill | ×1 | P1 |
| 58 | `shift_end_bell` | shift ends | 2.5 s | big mechanical bell, decays | ×1 | P1 |
| 59 | `pa_chime` | before a hall announcement | 1.5 s | three-note station chime, slightly detuned | ×1 | P1 |
| 60 | `board_flip` | departure board letters flip (loopable 1 s) | 1.0 s | split-flap clatter | loop | P1 |
| 61 | `day_start` | morning briefing card appears | 1.0 s | short musical sting, warm | ×1 | P2 |

## 6. Stability (the Helix River) and penalties

| # | File | When it plays | Length | Character | Var | P |
|---|---|---|---|---|---|---|
| 62 | `citation_hit` | a citation is issued (with the hit-stop) | 0.6 s | low impact plus a dissonant sting | ×2 | P1 |
| 63 | `helix_pulse` | the Helix River pulses on a citation | 1.0 s | deep swelling hum, glitchy tail | ×1 | P2 |
| 64 | `helix_breach` | the river breaches (the screen shakes) | 2.0 s | sub-boom plus a tearing reverse cymbal | ×1 | P1 |
| 65 | `famous_pass` | a famous traveller is let through | 1.5 s | tonal "history shifts" swell | ×1 | P2 |

## 7. Ambience beds (loops, 30-60 s, seamless)

| # | File | When it plays | Character | P |
|---|---|---|---|---|
| 66 | `amb_hall_day` | the hall in the daytime | crowd murmur in a big stone hall, distant footsteps, no clear words | P1 |
| 67 | `amb_hall_rush` | busy hours (layered over day) | denser crowd, luggage wheels | P2 |
| 68 | `amb_hall_night` | night shifts (days 12-15) | sparse, echoing, a cleaner's cart, a hum | P1 |
| 69 | `amb_portal_hum` | always, very low | tonal machinery drone, slow beating | P1 |
| 70 | `amb_rain` | wet weather | rain on high windows | P2 |
| 71 | `amb_booth_room` | inside the booth | small room tone, a fluorescent buzz, a CRT whine | P1 |
| 72 | `amb_city` | look-left city view | far traffic, wind, distant sirens | P2 |
| 73 | `amb_home` | Home screen | apartment room tone, fridge hum, rain on the glass | P2 |

## 8. Music (optional, later)

| # | File | When | P |
|---|---|---|---|
| 74 | `mus_title` | title screen loop | P2 |
| 75 | `mus_desk_calm` | early shift, low and sparse (synth plus tape warble) | P3 |
| 76 | `mus_desk_tense` | last hour / low stability layer | P3 |

**Total:** 87 entries; about 170 files counting the variants. **P1 alone:** 42 entries.
