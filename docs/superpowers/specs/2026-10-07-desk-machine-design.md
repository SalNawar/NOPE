# The desk as a machine: feel pass 2 (2026-10-07)

**Saleh, 2026-10-07:**
- On the moment-to-moment interactions: "I still need more in-game feel interactions, not little details". Then: "these are more features, I'm talking polish, game feel, pizzazz".
- On the stamps: "retro two-click stamps … one saying approved, one denied". His reference was a self-inking dater, the Printer S-401: a black body, a die that flips from the ink pad to the paper, date wheels, and a two-colour print with an outline word over a red date.
- He chose:
  - decision hardware;
  - shift rituals;
  - paper physics;
  - a scanner that does **both** things (it reveals the hidden layer *and* reads the chip);
  - three hardware verdicts;
  - "some of the desk objects should be things the player can move as well";
  - "detain only if the traveller breaks the law".
- He approved this design ("yes"), including build order C: the dater stamp and gate lever first as a prototype he judges from a capture, then the rest in parallel.

Track J's feel pass 1 (feat/juice) is the base. It provides:
- the motion core: `Springs`, `ControlMotion`, `MotionKnobs`;
- `UiJuice` and the motion driver;
- the sound cues, the Cinemachine camera and the hit-stop, which are in flight.

Every motion here uses those springs. There are no linear lerps and no DOTween.

**Saleh's rules** are the quotes above. Everything marked **Decision** below is the orchestrator's choice, written down so he can overrule it.

## 1. Dater stamps (the hero piece)

- **Two daters replace the two flat stamps:** APPROVED and DENIED, in the style of the S-401.
  - **Saleh, 2026-10-07:** "approve and deny stamp need to be green and red so they are easy to tell apart." The APPROVED dater's body is a deep, slightly glossy green, the DENIED dater's a deep red (kit colours, not neon), each with its side button in a lighter tint and a clear top window showing the impression. The S-401 shape stays.
  - A white frame.
  - Four date wheels you can see through the side: day, month, year (2150-style) and the era code.
- **Use is two clicks:**
  - **Press (mouse down):** the body travels down against a spring, its resistance building over about 60 ms. A **clack** as the die flips off the ink pad, then the impression lands. Hit-stop and an impulse shake follow (Track J's systems).
  - **Release (mouse up):** a second, lighter **clack** as the die flips back. The body springs up with overshoot.
  - Holding the button holds the stamp down. A quick click does both clacks fast.
- **The impression:**
  - the word in an outline display face, in the verdict's ink: APPROVED green, DENIED red (Saleh, 2026-10-07; this replaces the reference's violet-blue word and red date);
  - the date in a dark ink (near-black navy), from the dater's wheels, so every print shows its date clearly;
  - "BY: <clerk id>" in the same dark ink.
  - It is rendered into the paper where pressed, as the stamp lands today, inside the passport's visa frame as the guide.
- **Ink:**
  - Each print's density varies slightly, with edge breaks and a soft smudge offset.
  - The pad empties over the shift, so prints get lighter. Re-inking is a quick click on the pad lid (a squish sound).
  - **Decision:** the ink never runs out completely; it is a look, not a resource.
- **Setting the date (a ritual):**
  - Each morning the daters still show yesterday's date. The first time you pick one up, its wheels roll to today, one ratchet click per notch.
  - **Decision:** this is automatic, with a visible and audible roll. It is not a manual task the player can get wrong.
- **Art.**
  - The daters are new 3D props.
  - **Decision:** build them in-engine from primitives and kit materials (a bevelled green or red body, a lighter side button, white frame, textured wheels), shaped like the reference. The art side may replace the meshes later under the same prop contract.
  - The impression's outline face is drawn into a texture at runtime.

## 2. Decision hardware: three verdicts

**Saleh: three actions.** Approve, Deny, and Detain = a covered button, *only when the traveller breaks the law*.

**Saleh, 2026-10-07 (on the prototype's lever and plain RETURN / DETAIN buttons):** "I don't like that return and detain are UI buttons. It breaks what I'm trying to build, where everything belongs in the game. And the lever for the portal is goofy and far away. It doesn't fit; it just feels like an extra step." His rules:
- **Everything diegetic.** No screen-space UI buttons for verdicts.
- **No lever. No extra commit step.**

- **The flow (Papers, Please's):**
  - Stamp the passport with a dater, then hand it back. That is the whole decision.
  - Handing back an APPROVED passport commits approval: the portal spins up (the portal sound, a hit, the hall's ring flares) and the traveller walks through.
  - Handing back a DENIED passport commits the denial: the traveller turns and leaves the way they came.
  - The gate lever and the RETURN button of the prototype are gone.
- **DETAIN (law-breakers):**
  - A red mushroom button under a hinged clear safety cover, an object on the desk within easy reach, near the daters on the counter side, built in-engine under a prop contract (`Base`, `Cover`, `Button`). Click the cover to flip it up (a spring hinge; it closes again by itself after a few seconds unused), then press the button (a deep press).
  - An alarm chirp, a red flash over the hall, a hit, guards take the traveller.
  - It works with or without a stamp, at any point after the traveller arrives.
- **Detain rules.**
  - **Saleh's rule:** detain only if the traveller breaks the law.
  - **Decision: what breaking the law means** (one place in Domain, `Law.Breaks(case)`): a **forgery** (a forged seal, a doctored value, a forged chip), a **false identity** (photo or biometric not the holder) and **contraband**, where the game has it.
  - Lies and costume errors are deviation faults, not crimes, so they are a DENY.
  - Missing papers and closed destinations are directive faults, also a DENY.
  - **Decision: correctness.**
    - Detaining a law-breaker is correct.
    - Denying a law-breaker is also correct; detain is the stronger, never-required option.
    - Detaining anyone who broke no law is **wrong: one citation** (Saleh's rule: one penalty per wrong decision).
    - Approving a law-breaker is wrong, as today.
  - Detain with zero logged evidence follows today's evidence rule for denials (`requireEvidenceToDeny`).
  - Detained travellers don't come back (recurring faces only return after a deny).
- **Verdict data.** `DeskStamp` and the verdict types gain `Detained`.
  - The audit, the ledger, the citation (its violation row reads "Detained a traveller who broke no law") and the save all carry it.
  - The day results count detentions.

## 3. Shift rituals

- **Saleh, 2026-10-07:** "Shift should not start until you press AVAILABLE." The shift clock starts on the first AVAILABLE press (Track P, on main). AVAILABLE is the start ritual.
- **Time card: dropped** (by the rule above). No punch-in starts the shift and no time card is built.
- **Intercom (calling the next traveller):**
  - Hold the intercom button. A buzzer starts, and on release a short tired "Next." plays (or a buzz, until the voice clip lands).
  - The booth shutter rolls up with a metal rattle and the traveller steps forward.
  - This replaces today's automatic arrival. It goes through the same AVAILABLE control (the sign, then the next traveller), not a second button.
  - **Decision:** the arrival waits for the player only when the queue has someone. While nobody is there, the AVAILABLE sign stays on and the intercom gives a dead click.
  - At shift start the shutter is down; the first "Next" raises it.

## 4. Paper physics

- **Throw:**
  - A paper released while moving keeps its velocity. It slides and spins slightly on the desk with friction, and stops against the desk's edge with a small bounce.
  - It never leaves the desk.
  - **Decision:** the speed is capped, so a paper can't be thrown off-screen or through the counter.
- **Hand-back slot:** dragging a paper onto the counter's slot pulls it in with a spring suck, with a paper slide sound.
- **Tear:** the citation's stub tears along the perforation. Drag the stub away; the strip peels in steps with a tear sound, and the stub becomes a separate paper you can bin or keep.
- **Till:**
  - Credits paid by travellers land as notes and coins on the counter.
  - Drag them into the till drawer and they drop in (coins clink, notes flutter), and the till total rolls up.
  - Slam the drawer by clicking it or pressing DOWN.
  - **Decision:** unbanked money still counts at shift end. Banking it is for feel, not a chore with a penalty.

## 5. Scanner: the hidden layer and the chip

**Saleh: both.**

- **Use:** lay a paper on the glass and close the lid by clicking it (thunk). Then pull the side handle across, a drag with resistance.
- **The sweep:**
  - A glowing green bar crosses the bed at the speed of your pull, with a carriage whirr.
  - Behind the bar, the paper shows its **hidden layer** in the scanner's lid window: UV watermarks, the ghost of an erased value under a doctored one, and a forged seal's missing microprint.
  - Each one found blinks, and a typewriter-bell ding marks a fault.
- **The chip.** When the sweep ends, the paper's chip record goes to the PC (Investigation > Papers, next to the scanned copy). The player compares chip against printed values with today's click-compare.
- **Content.**
  - Documents get an optional `hidden` layer (UV mark, ghost value) and an optional `chip` record.
  - Forgeries are authored or generated so that some are **perfect to the eye and caught only by the scanner** (a ghost value, a missing UV mark or a chip mismatch).
  - **Decision:** these "scanner-only" forgeries start appearing from day 5, when the scanner arrives, and are never more than one per day in week one.
  - The Analysis Scanner upgrade becomes **faster sweeps plus an automatic flag** (its old job). Auto-Feed lays papers on the glass by itself.
- The desk's own inspection is unchanged: what's printed is still checked at the desk.

## 6. Movable desk objects

- These become draggable with weight: the mug, desk lamp, plant, photo, calculator, pen pot and stapler.
- **Weight.** Each has a mass in `DeskPropSO`. Heavy things lag the pointer and settle with a thud; light things slide and wobble.
- Objects push papers aside on contact, a soft nudge with no physics chaos.
- Positions persist through the day and reset each morning.
- **Decision:** the objects keep their click reactions. A drag starts after 6 px of movement.

## 7. Not in this pass

Photo overlay, fingerprints, the glyph decoder and transponder tuning are new **features**, not feel. That includes Saleh's idea: "some people might have their transponder set to return them to the wrong date". Each gets its own design later.

## Build order (C)

1. **Prototype:** the two dater stamps, with the three-verdict domain; after Saleh's look at it, the hand-back commits approve and deny and DETAIN is the button on the desk (the lever and the plain buttons are gone).
   - Capture a short frame sequence and a video of: stamp press and release, the date roll, the lever pull with ratchet and thunk, and the portal spin-up.
   - Saleh judges the feel.
2. **After his OK, two tracks in parallel:**
   - **(a)** the intercom/shutter (through AVAILABLE; the time card is dropped; the DETAIN button already stands on the desk);
   - **(b)** paper physics, the scanner, and movable objects.
   - They share the desk code: (a) owns the counter and booth side, (b) owns the desk plane and scanner, and they merge through main.

## Verification (every step)

- EditMode suite, plus new Domain tests:
  - `Law.Breaks`;
  - three-verdict correctness: detaining an honest traveller → 1 citation; detaining a law-breaker → correct; a law-breaker denied → correct;
  - scanner-only forgery generation;
  - ink fade;
  - the date-wheel target.
- Idempotent rebuilds.
- The audit play, days 1-15, A/B identical. The audit drives the new hardware: AVAILABLE, stamp, then the hand-back or the DETAIN button.
- The transcripts change only where the new verdict and rituals add lines.
- Golden notes list every difference.
- Zero per-frame allocations in idle.
- Reduced Motion: the lever and stamps still work with snaps instead of springs.
