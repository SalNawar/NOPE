# Desk machine, step 1 (the prototype) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build-order step 1 of `docs/superpowers/specs/2026-10-07-desk-machine-design.md`: the two S-401 dater stamps, the gate lever, the three-verdict domain (APPROVED, DENIED, DETAINED) and RETURN / DETAIN as plain kit buttons, so Saleh can judge the feel from captures.

**Architecture:** The rules are pure (`TimeDesk.Domain`): `Law.Breaks`, the three-verdict correctness in `VerdictRules`, the hand-back and hardware commit in `StampFlow`, the dater's ink (`DaterInk`) and date wheels (`DaterWheels`), the lever's ratchet (`LeverTravel`). The impression's letter shapes are pure maths in `TimeDesk.Visuals` (`DaterLetters`). Unity code renders and orchestrates: `DaterImpressionArt` paints the impression texture, `DeskStampTray` drives the daters with springs, `GateLever` and `VerdictButtons` commit the verdict, `FeelDirector` gives the hit-stop and the Cinemachine impulse. The verdict travels as a `DeskStamp` from the hardware to `ShiftScoring` instead of a bool.

**Tech Stack:** Unity 6000.4.11f1, URP, Cinemachine 3.1.7, TMP, NUnit EditMode tests (offline runner + TimeDeskEditMode in Unity).

---

## File structure

| File | Responsibility |
|---|---|
| `Assets/Scripts/Domain/Law.cs` (new) | `Law.Breaks(directive, costume, lie)`: forgery, false identity, contraband |
| `Assets/Scripts/Domain/StampFlow.cs` | `DeskStamp.Detained` appended; `HandedBack`, `HandBack()`, `Commit(DeskStamp)` |
| `Assets/Scripts/Domain/VerdictRules.cs` | three-verdict `IsCorrect`, `IsUnprovenDenial`, `IsWrongDetention` |
| `Assets/Scripts/Domain/DaterInk.cs` (new) | ink density per print: fade over the shift, floor, re-ink, per-print variation |
| `Assets/Scripts/Domain/DaterWheels.cs` (new) | the wheels' notches for a date and the ratchet steps from yesterday to today |
| `Assets/Scripts/Domain/LeverTravel.cs` (new) | the lever's notches (15 degrees), crossings, home |
| `Assets/Scripts/Domain/MotionKnobs.cs` | `MotionFeel.Dater`, `MotionFeel.Lever` and their knobs |
| `Assets/Scripts/Domain/ShiftLedger.cs` | `CaseVerdict.detained`, `MistakeKey` "citation.detainedWrong", `ShiftLedger.DetainedCount` |
| `Assets/Scripts/Visuals/DaterLetters.cs` (new) | signed distance of the slab-serif capitals the outline word uses |
| `Assets/Scripts/UI/Forms/DaterImpressionArt.cs` (new) | paints an impression texture (outline word, red date, BY line) and the wheel bands |
| `Assets/Scripts/Office/Desk/DeskStampTray.cs` | daters: spring press/release, two clacks, wheel roll, ink, hand-back state, hardware commit |
| `Assets/Scripts/Office/Desk/DeskDocument.cs` | `Stamp` prints the dater impression; the code-drawn word path removed |
| `Assets/Scripts/Office/GateLever.cs` (new) | the floor lever: drag with resistance, ratchet, home, spring back, "no" wobble |
| `Assets/Scripts/Office/VerdictButtons.cs` (new) | RETURN and DETAIN kit buttons by the counter |
| `Assets/Scripts/UI/Motion/FeelDirector.cs` (new) | `FeelDirector.Hit(strength)`: hit-stop and the desk camera's impulse |
| `Assets/Scripts/Shift/ShiftScoring.cs`, `Core/DayCycle.cs`, `GameManager.cs`, `UI/InvestigationUIController.cs`, `Office/Desk/DeskController.cs`, `Editor/BalanceSimulation.cs`, `GameManager.Cheats.cs` | the verdict as a `DeskStamp` end to end; detained skip the return plan |
| `Assets/Scripts/WorldState.cs` | `totalDetained` (additive save field) |
| `Assets/Scripts/UI/DayFlowUIController.cs` | the results' detentions line |
| `Assets/Scripts/DevTools/LawBreakerCheats.cs` (new), `DevToolsState.cs`, `CaseFactory.cs` | "Law-breaker next" cheat (a forged seal on the next generated traveller) |
| `Assets/Editor/OfficeSceneUIBuilder.Hardware.cs` (new) + `.Desk.cs` | the dater props (`Body`, `Frame`, `Die`, `Wheels`, `Button`), the lever (`Base`, `Arm`, `Knob`), the buttons, the impulse listener |
| `Assets/Data/World/world_source.json` | the new UI strings |
| `tools/audit/unity/_TimeDeskAuditPlay.cs.txt` | the audit drives stamp, hand back, lever / RETURN / DETAIN |

## Task 1: Law.Breaks (TDD)

- [ ] Write `Assets/Tests/EditMode/LawTests.cs`:

```csharp
[TestCase(LieKind.ForgedSeal, true)] [TestCase(LieKind.DoctoredIdentity, true)] [TestCase(LieKind.PoorPosingAsRich, true)]
[TestCase(LieKind.DebtorPosingAsTourist, true)] [TestCase(LieKind.ForgedContract, true)] [TestCase(LieKind.FakeWaiver, true)]
[TestCase(LieKind.ForgedProof, true)] [TestCase(LieKind.SwappedPhoto, true)] [TestCase(LieKind.Smuggling, true)]
[TestCase(LieKind.FalseOrigin, false)] [TestCase(LieKind.FakeDisplaced, false)]
public void ALie_BreaksTheLaw_OnlyAsForgeryFalseIdentityOrContraband(LieKind lie, bool breaks) =>
    Assert.AreEqual(breaks, Law.Breaks(DirectiveFault.None, CostumeError.None, lie));
// plus: costume error, every directive fault (missing papers, closed), honest -> false
```

- [ ] Run compile check: fails (no `Law`). Implement `Law.Breaks`: `lie != null && (LieKinds.IsRecordLie || IsVisualLie || Smuggling)`; directive and costume never. Pass. Commit `feat(domain): Law.Breaks`.

## Task 2: three verdicts (TDD)

- [ ] Tests in `VerdictRulesTests.cs`: honest detained → not correct and `IsWrongDetention`; law-breaker detained → correct; law-breaker denied → correct; law-breaker detained with zero evidence and the gate on → unproven; a liar who broke no law detained → wrong detention; approved/denied rows unchanged.
- [ ] `StampFlowTests.cs`: hand back locks the verdict (`HandedBack`); `Commit(Approved)` only after an APPROVED hand-back; `Commit(Denied)` only after a DENIED one; `Commit(Detained)` any time a case is on (passport given); a new case clears it.
- [ ] `ShiftLedgerTests.cs`: `MistakeKey` of a wrong detention is `citation.detainedWrong`; `DetainedCount`.
- [ ] Implement: append `DeskStamp.Detained`; `VerdictRules.IsCorrect(DeskStamp, shouldAccept, curedAtDesk, breaksLaw)`, `IsWrongDetention`, `IsUnprovenDenial(..., DeskStamp, ...)`; `StampFlow.HandBack/HandedBack/Commit`; `CaseVerdict.detained`, `ShiftLedger.DetainedCount`. Pass. Commit.

## Task 3: verdict as DeskStamp end to end

- [ ] `ShiftScoring.ResolveDecision(inst, DeskStamp verdict, ...)`, `DayCycle.Decide(inst, DeskStamp, ...)` (detained: no return plan, `world.totalDetained++`), `InvestigationUIController.Decide(DeskStamp)` / `ShowCase(..., Action<DeskStamp>)`, `GameManager.HandleDecision(DeskStamp)`, `CheatDecide`, `BalanceSimulation`. Wrong detention's citation: mistake "citation.detainedWrong", the honest traveller's facts. Results: `results.detained`. Strings in `world_source.json`. Compile, offline runner. Commit.

## Task 4: dater ink and wheels (TDD)

- [ ] `DaterInkTests`: density 1 after re-ink; falls by `fadePerPrint` per print; never below `floor`; variation within ±`variation` and the same for the same print number; re-ink restores.
- [ ] `DaterWheelsTests`: `Notches(14 Mar 2150)` = (13, 2, 0); steps 14→15 Mar = (1,0,0); 31 Mar→1 Apr = (1,1,0); 28 Feb→1 Mar 2151 = (4,1,1); 31 Dec 2159→1 Jan 2160 = (1,1,1); `Shown(day)` is yesterday before the day's roll.
- [ ] `LeverTravelTests`: notch of an angle; crossings counted once per notch; home at the full travel.
- [ ] Implement, pass, commit.

## Task 5: the impression texture

- [ ] `DaterLetters.Distance(char, x, y)` (Visuals, pure): A P R O V E D N I as unions of boxes, bowls and slanted stems with slab serifs; test that each letter's centre stroke is inside and the counter (hole) outside.
- [ ] `DaterImpressionArt.Paint(word, date, clerk, ink seed/density)`: outline word (|d| < line), red date and BY line sampled from GeistMono-Bold's SDF atlas (readable), edge breaks from value noise, a soft smudge offset, alpha × density. Wheel band textures (digits / months) from the same atlas.

## Task 6: daters in the tray and the builder

- [ ] Builder: each dater a prop root with `Body` (black, rounded top, window showing the die texture), `Frame` (white), `Die`, `Wheels` (four cylinders: day, month, year), `Button` (green / red side button), the click box and its DeskDraggable, `Die` empty at the foot.
- [ ] `DeskStampTray`: press (spring down with building resistance, clack 1 `StampPress`, impression, `FeelDirector.Hit`), release (clack 2 `StampRelease`, spring up with overshoot); a click on a hanging dater holds while the button is held (pointer down after `daterHoldDelay`, up releases); a drag's drop is a quick stroke. Wheels roll the first pick-up each day (ratchet clicks). Ink fades per print; the side button re-inks.
- [ ] `DeskDocument.Stamp` prints the impression texture; remove the code-drawn word path (ShowVerdict uses the impression too).

## Task 7: lever, buttons, feel

- [ ] `FeelDirector.Hit(strength)`: hit-stop (timeScale 0 for `hitStopSeconds × strength`, unscaled), `CinemachineImpulseSource.GenerateImpulseWithForce`; Reduced Motion: no impulse; intensity scales it. Builder adds a `CinemachineImpulseListener` to the desk vcam.
- [ ] `GateLever`: drag down (pointer travel → target angle through a resistance curve), spring (Lever feel), ratchet click each 15°, home thunk → `StampFlow.Commit(Approved)` → `Decided(Approved)`; early release springs back; refused → "no" wobble.
- [ ] `VerdictButtons`: RETURN (plate_brass) commits Denied after a DENIED hand-back, else wobble; DETAIN (plate_red) commits Detained any time a case is on.
- [ ] Hand-back: the papers leave, the view returns to the hall, the hint names the hardware.
- [ ] Builder: lever beside the desk (binder lays it), buttons by the counter. Rebuild OfficeGameplay.

## Task 8: cheat, audit, verification, captures

- [ ] "Law-breaker next" cheat (DevCheats.Register): `DevToolsState.ForcedLie = ForgedSeal`, consumed by `CaseFactory.RollLie`.
- [ ] Audit play: stamp, hand back, lever (APPROVED) / RETURN (DENIED); DETAIN every law-breaker the plan denies with evidence on days 6-15 (correct, same pay); run A/B, golden notes.
- [ ] Unity: compile, EditMode, rebuild, smoke, probes, frame captures → GIF/MP4 in `R7\DM\captures`.
