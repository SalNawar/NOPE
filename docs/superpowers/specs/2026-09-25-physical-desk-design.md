# Physical desk and traveller wheel: design (piece 7)

*2026-09-25 · decisions made by Claude under Saleh's instruction "dont stop until you finish everything" (2026-09-25), open to his review · branch `feat/physical-desk`, from `main` at `6477d8f`*

The booth becomes a desk you work at. The PC's desktop is drawn live on the CRT's glass, so the monitor shows the case while you sit in the booth. Clicking the CRT pushes the camera in until the screen fills most of the view (ReStory-style), and you leave with Escape, a click outside the screen or a taskbar button. The screen has a power button. Documents are physical papers: the traveller hands the passport over when they step up and the permit when you ask for it, you drag the papers around the desk, and you drop a paper on the desk scanner to open its scanned copy on the PC, where comparing and deciding stay. You talk to the traveller by clicking them (or the desk intercom): a wheel of choices opens around them, and their replies appear in a speech bubble as well as in the PC transcript. The other desk objects react to clicks, and some show readouts. Seams are laid for desk decoration (item 7) and translation (item 8).

Saleh's features for this piece, verbatim (numbered as he wrote them): "1. documents submitted are physical the player can move them around on the table. 2. There is a scanner on the table the documents can be fed to it to go to the PC 3. pc no longer completely takes the screen it should look like restory when the player interacts with it it focuses on it and takes most of the screen 4. the player should be able to turn the screen on and off of the pc 5. the monitor should actually display what it should in the office scene 6. other objects on the desk will also be interactable 7. in the future the player will be able to decorate the desk and customize it a bit 8. there are different upgrades to translate different languages two types written documents and dialogue. when the translation happens letters will flip one by one we will flesh it out later. 9. The player will be able to trigger some interactions with the traveller by clicking on the traveller which will open a wheel of options they can then select an option each option could be a question or a request etc. will be fleshed out later"

In this spec "item N" is that list and "piece N" is the roadmap (`docs/superpowers/plans/2026-09-25-code-audit-overhaul-plan.md:9-11`: pieces 7, 8, 4, 5, 6, 9, then the audit, then art). Pieces 4 to 6 exist only as parked drafts (`docs/superpowers/drafts/specs/`); piece 7 lands before all of them.

Line numbers refer to this branch at `6477d8f` (= `main`; the worktree was checked out fresh on `feat/physical-desk`). Every statement about existing code below was checked by opening the file at that commit. Baseline: `compile_check.py` 0 errors in every project; the offline runner `passed 337, failed 0`. The plan re-reads each file before editing it; the code wins over this spec. The independent review's findings are applied throughout; Review notes lists each with its verdict.

## 0. Decisions

Claude made these under Saleh's instruction "dont stop until you finish everything" (2026-09-25), from the piece-7 code analysis (session scratchpad `piece7_map.json`: its synthesis decisions in order = K1–K21, its completeness check's extra decisions = V1–V9, and its list of missing items, handled in §8). They bind this piece and are open to Saleh's review. The R rows under the table are the details this spec settles where a decision left them open. R31–R37 were added by the independent review (Review notes).

| Id | Decision | Rationale |
|---|---|---|
| K1 | **One World Space desktop canvas on the CRT glass, drawn by the main camera, no RenderTexture.** The existing desktop `Canvas` becomes a World Space canvas parented to a `ScreenAnchor` on the CRT glass, sorted just above the CRT. Focus is the monitor vcam push-in. Input is gated: the desktop `GraphicRaycaster` is on only while focused and after the blend settles; the CRT `Clickable` is inert while focused. `DraggableWindow`'s drag maths moves to `RectTransformUtility.ScreenPointToLocalPointInRectangle`. | One canvas both displays (item 5) and takes input (item 3) with stock uGUI: a world-space `GraphicRaycaster` raycasts through `canvas.worldCamera` (ugui `GraphicRaycaster.cs:297-309`). A RenderTexture breaks the raycaster, `TMP_InputField` and `DraggableWindow`, which read raw screen positions. |
| K2 | **Keep the current CRT art; a 4:3 desktop of 1440×1080 units in the glass's inscribed rectangle; the focus framing fills a knob fraction of the view.** The desktop keeps its 1080-unit height, so fonts and windows keep their size; its horizontal layout is re-laid for 4:3. New front-facing CRT art with a big glass (option C) is logged as an art request for the final art step (§3.3). | Readability forces the glass to fill most of the view whatever the aspect; keeping the height keeps every font and window; the glass rectangle is component data, so new art only changes numbers. |
| K3 | **The basic traveller wheel ships in piece 7; the PC intercom panel is retired.** Clicking the traveller (booth view, traveller at the desk) opens a radial menu on the office overlay canvas with the same interview choices (requests, "Ask about home >", questions, dialogs, "< Back"). Replies show in a minimal speech bubble near the traveller and in the PC transcript, whose answer rows stay compare-clickable. The desk intercom prop also opens the wheel. Piece 8 becomes wheel content (custom requests, choice kinds and icons, "Look >" garments after piece 4, bubble polish). | Without the wheel, physical papers force a PC → desk → PC loop for every request, and booth-view answers could be read only on the PC. It changes the audit plan's piece-8 line (§3.3). |
| K4 | **EventSystem drag handlers on `Collider2D` proxies through the existing `Physics2DRaycaster`; the pointer is projected onto the desk surface by a camera ray against its plane.** | Reuses `Clickable`, `HoverHighlighter` and the raycaster, proven in both office scenes. The plane projection serves the orthographic booth and a perspective 3D desk alike (R1). |
| K5 | **Scan = copy.** Drop a paper on the scanner; one scan at a time (duration knob; the shift clock keeps running); the paper pops back onto the desk; its scanned window opens on the PC. A Domain `DeskPapers` state machine per case, with decision-table tests. A `DeskReachable` wiring gate keeps today's request → window path where no desk is wired. | Keeps papers physical and the PC the single evidence surface (`CompareController.Select` needs a uGUI `Image`, `CompareController.cs:63`). Day 1's only tell channel is Papers and an unproven denial is penalised (`VerdictRules.cs`), so papers must always be able to reach the PC. |
| K6 | **A per-template knob `DocumentTemplateSO.handOver` = OnArrival or OnRequest.** Passport = OnArrival ("documents submitted": handed over when the traveller steps up), Permit = OnRequest. Generate World, the validator and `DialogChecks.MenuProblems` count only on-request documents as requests. | Item 1's "submitted" and item 9's "request" both hold, per document, without code. |
| K7 | **Screen-only power.** The display and desktop input go dark; the PC keeps running (scans still open windows, the clock keeps ticking). A power button on the CRT bezel; the Start menu's "Power" becomes "Turn off screen" plus a separate "Quit game". Wake rules are knobs (a finished scan and a newly presented traveller wake the screen); a pending citation slip holds the screen on (R31). | Saleh: "turn the screen on and off of the pc". Nothing the day needs can be locked behind a dark screen, and today's "Power" quits the game (`DesktopShell.cs:35-36, 56-63`). |
| K8 | **While focused, only the desktop and the bezel power button take input. Exits: Escape, a click outside the screen, and a desktop "< Desk" button.** | At the focus framing only bezel slivers are visible, so live props would only catch stray clicks. |
| K9 | **READY only releases the gate (no zoom).** The player stays in the booth to see the traveller arrive and to use the papers and the wheel. | Landing on the PC would hide the arrival and the hand-over; the live monitor already shows the case. |
| K10 | **A screen-space radial wheel on the office overlay canvas, placed by projecting the traveller's anchor; a sub-menu replaces the ring in place, with "< Back" in the centre.** | The overlay canvas is the only UI visible in the booth view (`OfficeSceneUIBuilder.cs:779-799`); projecting an anchor works for any camera. Replace-in-place maps 1:1 onto `DialogRunner` nodes. |
| K11 | **The verdict stays on the PC** (Accept/Deny, compare bar, transcript, Deviation Report). The desk stamp gets a click reaction only (the hook for later stamping). | None of Saleh's items moves the verdict; a second decision surface would duplicate one (law 2). |
| K12 | **A physical paper shows its title, the holder's name and a reserved photo slot; every field is read and compared on the scanned PC copy.** | A 1.5-unit paper is about 135 px wide at 1080p: glance-readable, not a form. |
| K13 | **Sorting bands on the `Default` sorting layer, as `DeskConfigSO` knobs** (focus-exit zone, bezel, screen canvas, papers, held paper). Named sorting layers are left to the code audit. | Both raycasters rank by sorting layer, then order (ugui `EventSystem.cs:202-257`); bands need no `TagManager` change and leave Codex's hybrid scene untouched. |
| K14 | **One generic `DeskReaction` component plus `DeskReactionSO`** (squash, wobble, nudge, pulse; optional clip, null-safe; optional tooltip) next to `Clickable` on each prop. Functional props: the intercom speaker (opens the wheel), the CRT (focus), the bezel power button, the scanner; calendar, stability monitor, till and wall clock show tooltip readouts; the rest are flavour. | One mechanism tuned by data (laws 2 and 3); no tween library and no Animator exist outside Codex's art, so animation is code-driven. |
| K15 | **One new `DeskConfigSO`** (created and assigned by the builder) for all tuning: fill fraction, blend time, scan duration, sorting bands, spawn slots, wake rules, screen-starts-on, wheel and bubble layout, the day-1 notes. Reaction tuning (including each tooltip's placement and duration) lives in the `DeskReactionSO` assets. | Law 3. Geometry that belongs to the art (glass rectangle, desk rectangle, scanner drop area, anchors) stays in scene components, so another office supplies its own. |
| K16 | **Decoration hooks only:** builder-created `DeskSlot` anchors (plant, mug, photo and free spots), a `DeskItem` id on each decor prop, and a generic `DeskDraggable` (used by papers now). No save data. | Nothing to persist until the decoration piece; empty catalogues or save fields would be dead code. |
| K17 | **Rename only the display text:** the desktop app "Scanner" becomes "Deviation Report", and the "Material Scanner" window becomes "Material Analysis". Code ids and object names stay. The desk device is the scanner. | Saleh's word "scanner" names the device. Renaming `IconScannerWindow`/`scannerWindow` would break the builder's lookups and the hybrid scene's wiring. |
| K18 | **Direct world-space rendering with a mipmapped wallpaper**; a RenderTexture miniature only if screenshots shimmer. | The office camera is static; TMP SDF text downsamples acceptably (screenshots judge it, §6). |
| K19 | **Translation (item 8): seams only.** One display-text method used for document values, transcript sentences and the traveller's spoken reply; the physical paper keeps its source text (§2.21). The scan's completion and the reply are the reveal points. No language data. | Evidence values must never become display text (piece-6 draft U3). The hard translation questions belong to piece 9. |
| K20 | **The booth traveller is clickable through a `TravellerHitZone` child** (`Collider2D` + `Clickable` + hidden `SpriteRenderer`), armed only while the traveller is at the desk. The piece-4 draft is amended (C4, W12 and related lines): garments will be inspected through the wheel, not a desktop Visitor window. | Item 9. A child hit zone survives piece 4's `SortingGroup` root, which has no renderer of its own (a collider there would report order 0, ugui `Physics2DRaycaster.cs:65-112`). |
| K21 | **Built in `Assets/Scenes/OfficeScene.unity` by the authoritative builder; every new reference optional and null-safe.** | The builder refuses other scenes (`OfficeSceneUIBuilder.cs:63-70`); Codex's `OfficeScene_HybridArt.unity` must keep working unchanged, and the desk must be able to move into the 3D office later. |
| V1 | **One traveller-presence seam:** `TravellerView.Show`/`Clear` shows the placeholder at presentation and clears it at the decision; piece 4's layered figure extends the same component. | One presence mechanism; piece 4's C19 named exactly this seam. |
| V2 | Folded into K17 (display rename; "Material Scanner" relabelled too). | The 2026-06-21 spec's desktop "Scanner = scanned documents" (:109) is realised by per-document icons (R2). |
| V3 | **A finished scan does not move the camera.** The scanner pulses and the new window appears on the live monitor. | Auto-focus would fight dragging the next paper. |
| V4 | **A paper dropped anywhere but the scanner stays where it was dropped**, its centre clamped to the desk. | Simple and physical. |
| V5 | **Booth input during the briefing, the results, a pending citation slip, between travellers and while the wheel is open are rows of one Domain decision table** (`BoothRules`, tested; the citation row is R31). | Rules live in Domain (manifesto). |
| V6 | **Desktop content is masked to the screen and windows are clamped inside it.** | A world-space canvas would otherwise draw dragged windows over the bezel and the booth. |
| V7 | **`InterviewReachable` counts the wheel**, so `spoken` and case generation stay byte-identical; the whole-day generation is compared before and after in Unity (§6 step 0 and 3). | Otherwise every day's travellers change (`GameManager.cs:146-149`). |
| V8 | **Day 1 teaches drag-to-scan with a one-line desk hint** (tells are papers-only on day 1), **and the traveller wheel with a second one** (R32). | A player who never scans, or never opens the wheel (the permit is handed over only on request), cannot prove every day-1 liar and is penalised for denying. |
| V9 | **The shift clock keeps running during scans and camera blends.** | FEATURES :76; the knobs are kept short. |

### Refinements settled by this spec

| Id | Refinement | Rationale |
|---|---|---|
| R1 (refines K4) | **The desk surface is one concrete component, `DeskSurface`**: a rectangle on its own transform's plane (position + forward). `TryProject` intersects the camera ray with that plane; `Clamp` keeps a point's centre inside the rectangle. No `IDeskSurface` interface. | A plane on the component's own transform already serves the orthographic booth and a perspective XZ desk; an interface with one implementation would be speculative (piece-3 R19). |
| R2 | **Requests are one-shot, and a document's window gets a desktop icon the first time it opens** (after its scan, or on request where no desk is wired). The icon reopens the window; icons are cleared with every new case (the existing `_docIcons` path). | Once a paper is on the desk it never goes back to the traveller mid-case, so the runner's one-shot rule hides the request with no new predicate. It revives the dead `isDocument` branch of `AddDesktopIcon` (`InvestigationUIController.cs:455-480`, only ever called with `false` at :445) and retires piece-3 Q11's "documents can only be reopened through the intercom". |
| R3 (refines K6) | **Arrival documents in both paths.** With a reachable desk (`DeskReachable`: the desk and all its parts wired), OnArrival papers slide onto the desk at presentation. Without one (no desk, or a partly wired one), OnArrival windows open (and get icons) at presentation. The hub holds a `request:{i}` only for OnRequest documents, `i` staying the paper index. | The same content must play in the hybrid scene and the rich fallback. |
| R4 (refines K3/K10) | **The wheel's ring reuses `InteractionPanelController`** as its list renderer (serialized name `interactionPanel` kept on `InvestigationUIController`), laid out by a new `RadialLayoutGroup`, with an optional centre slot for "< Back". A new `TravellerWheel` opens, closes and positions it. `DialogChoice` gains `Kind` (`Normal`, `Back`) so the wheel can centre "< Back"; piece 8 appends kinds. | Law 2: the intercom's renderer already turns `InteractionAction {label, execute}` into buttons (`InteractionPanelController.cs:8-15, 40-60`). Keeping the class keeps `InterviewReachable` unchanged (V7) and keeps the hybrid scene's intercom component from becoming a missing script (merge gate G1). |
| R5 | **The wheel is an elliptical ring** (radii 300 × 200 reference px, items 240 × 44, centre 150 × 44, gap 8). `RadialLayout.MaxFit` gives 8 for these numbers, equal to `interview.menuCapacity`; the builder reports a smaller fit, as it reported the intercom's. | A circle of radius 210 fits only 4 (at 5 items the two lowest already sit level, 246.9 px apart, under the 248 px they need); the ellipse fits every count from 1 to 8 (worked in §2.6). |
| R6 | **Overlay catchers exist only while needed.** The wheel's full-screen click-to-close catcher is its `Catcher` child, active only while the wheel is open (the `TravellerWheel` host itself stays active, R33). The focus exit is a world collider (`FocusExitZone`) sorted under the screen canvas, active only while focused. | Overlay raycast hits always outrank world hits (`GraphicRaycaster.sortOrderPriority` is the canvas order for overlay, `GraphicRaycaster.cs:48-58`; `EventSystem.cs:219-220`), so a permanent overlay catcher would block the whole booth and the live desktop. |
| R7 (refines K5) | **Drops are decided by the projected pointer, not `OnDrop`.** A dragged paper disables its own collider for the drag and re-enables it on release; release inside the scanner's drop rectangle feeds it. | With the collider off, the release is never over the pressed object, so no click follows the drag (`InputSystemUIInputModule.cs:696-745`: the click at 707 needs `pointerClick ==` the handler under the pointer); the projected-point test works for any camera and collider type. |
| R8 | **A paper is a `SortingGroup` root carrying its `SpriteRenderer`, `BoxCollider2D`, `Clickable` and `DeskDraggable`.** Lifting or restacking changes only the group's order. | `HoverHighlighter` outlines the renderer on the `Clickable`'s own object (`HoverHighlighter.cs:187-197`) and puts the outline one order above it inside the group, so the outline follows the paper with no highlighter change; the raycast reports the group's order (`Physics2DRaycaster.cs:101-113`). |
| R9 | **`HoverHighlighter` purges outlines of destroyed renderers** whenever it creates a new one (the sweep `HandleSceneUnloaded` already does, extracted). | Per-case papers would otherwise leak one generated outline texture each until the scene unloads (`HoverHighlighter.cs:30, 220-251, 291-309`). |
| R10 | **Booth phases are set by `GameManager` through one transition method** (`Newsletter`, `NoTraveller`, `TravellerAtDesk`), and one scene component, `BoothCoordinator`, applies `BoothRules` and the wake rules. A pending citation slip is not a phase but a flag set beside it (`SetCitationPending`, R31): the traveller has left, the slip is on the PC, and it holds the clock as today. | One presence transition feeds `_travellerAtDesk` (closing rule), `TravellerView` and the input table, so no second presence flag can disagree. |
| R11 | **"Settled" = the rig's brain shows the view's camera and is not blending** (`ICameraRig.IsSettled(view)`: `brain.ActiveVirtualCamera == cam && !brain.IsBlending`). With no rig, or a rig with no brain, the view counts as settled (R35). | `IsBlending` alone is false in the frame the priority changes, before the brain has started the blend. |
| R12 | **Monitor framing is computed at runtime** from `MonitorScreen`'s glass rectangle, the camera's aspect and the fill knob (`MonitorFraming.OrthoSize`, Visuals); the brain's default blend comes from the blend knob. `MonitorOrthoSize` goes. | A new CRT or a window resize only changes inputs; the hybrid scene (perspective lens, no `MonitorScreen`) is untouched. |
| R13 (refines K2) | **Glass geometry** (measured in `crt.png` with a colour mask, blue > red + 15 and green > red, ignoring the highlight): the largest 4:3 rectangle is 271 × 203 px, centre in sprite units (−0.144, 0.054), size (0.676, 0.507); world ≈ (5.50, −1.51), 2.34 × 1.76. The canvas is 1440 × 1080 units at a uniform scale of 0.507 / 1080 inside `ScreenAnchor`. At fill 0.85 the orthographic size is 1.034: the desktop is about 918 px tall at 1080p (0.85 px per canvas unit; 18 pt ≈ 15 px) and about 612 px at 720p (≈ 10 px). | The analysis measured 280 × 211 by eye; the mask gives a rectangle that is 4:3 and wholly inside the glass. The screenshots (§6) are the judge; the numbers are builder constants. |
| R14 | **Without a `MonitorScreen` (hybrid scene), `OfficeViewController` keeps today's `desktopRoot.SetActive` path.** | Codex's scene keeps its own wiring (`OfficeScene_HybridArt.unity:39767-39769`). Two display paths in one class is recorded as an audit target (§7). |
| R15 | **`OfficeViewController.InitForTest` and `Toggle` are removed.** | Neither has a caller in code or in any scene's persistent calls (checked: only `FocusMonitor`/`FocusOffice` are wired); the EditMode tests cannot reach Assembly-CSharp, so the "test seam" was never usable. |
| R16 (refines V8) | **The day-1 scan hint is a desk note** on the scanner tray, text and last day in `DeskConfigSO` (`scanHint`, `scanHintUntilDay` = 1), shown while a paper is on the desk and nothing has been scanned yet today (`DeskHints.ScanHintVisible`, tested). The wheel's note follows the same pattern (R32). | It shows where and when it helps; it is desk UI copy like the reaction tooltips, not world content, so it does not go through `world_source.json`. |
| R17 | **Between travellers the desktop shows an idle line** ("Waiting for the next traveller", large type) instead of a bare wallpaper. | The icon grid sits under `InvestigationRoot` (`OfficeSceneUIBuilder.cs:157`), hidden between cases (`InvestigationUIController.cs:249-253, 497-503`), which the live monitor now shows in the booth. |
| R18 | **The five leftovers under `Canvas/Office UI Controller`** (`VisitorText`, `Doc1Text`, `Doc2Text`, `ResultText`, `EraButtonsRoot`, all active; `OfficeScene.unity:17338-17399`) are destroyed by the builder. `OfficeUIController`'s code, the component and its HUD/citation wiring stay. | A live monitor would show "Visitor", "Document 1", "Document 2" faintly (visible in the scratchpad's `layout_monitor_4.png`). The legacy fields are live in build scene 0, `Test_DayLoop.unity` (`:151-156`; its `GameManager` wires only `officeUI`, `:1287-1291`). `ClaimBanner`'s default "Visitor" text (`OfficeScene.unity:20493`) is the live claim banner and stays. |
| R19 (refines K19) | **`DisplayText.For(canonical, medium)`** (Visuals) returns the canonical string today. Its callers display document values, transcript sentences and the traveller's reply; every compare call keeps passing the canonical value. The physical paper binds its canonical title and holder: it stays in its source script, and the scanned copy is what gets translated (§2.21). The reveal points are `InvestigationUIController.OpenDocumentWindow` (the scan-finished handler) and the reply that `InvestigationUIController.Choose` passes to `TravellerWheel.Say`; no event without a subscriber is added. | A seam with real callers; an unsubscribed "bubble shown" event would be dead code. Translating the paper too would break item 8's model, in which the upgrade translates the scanned copy. |
| R20 | **Tooltips reuse the live readouts:** a reaction's tooltip is its `DeskReactionSO.tooltip` filled with a readout TMP's current text through `Interview.Fill(template, Interview.ValueToken, value)`: the calendar's `DayNumber`, `StabilityPercent`, `CreditsNumber`, the tray clock. | Zero re-implementation: the readouts already hold the values (`OfficeReadouts`, `ShiftClockReadouts`), and `Interview.Fill` is the one token filler. |
| R21 (refines K16) | **`DeskSlot` and `DeskItem` expose no public members** (serialized ids only). The builder places the plant and the mug at their slots, so the slot is the one source of those positions; the runtime reader arrives with decoration. | K16 asks for hooks; with no public surface there is no public member without a caller. |
| R22 | **The builder's new booth and desk code goes in a partial file**, `Assets/Editor/OfficeSceneUIBuilder.Desk.cs`. | `OfficeSceneUIBuilder.cs` is 1804 lines; the audit plan allows splitting builders by area. |
| R23 | **Window spawn positions become serialized layout on `InvestigationUIController`** (documents cascade from (−195, 150) by (40, −40); books from (−180, −150), step 300 per column and (40, 40) per row), set by the builder for the 1440-wide desktop. The fields' initialisers are today's 16:9 constants: documents from (−330, 140) by (620, 0); books from (−380, −150), 320 per column and (40, 40) per row. | The 16:9 constants (`InvestigationUIController.cs:295, 441`) put document 0 over the icon column (x −720..−483 at 1440 wide); the builder owns layout. A scene that is not rebuilt (the hybrid scene, 1920 wide) loads the initialisers and keeps today's layout. |
| R24 | **The booth raycast buffer goes from 8 to 16.** | At one point the ray can cross up to 8 papers plus the scanner, a prop, the traveller zone and the exit zone; hits are truncated before sorting (`Physics2DRaycaster.cs:55-62`). |
| R25 | **Copy:** the compare label of an answer row becomes "Traveller · {category}"; the ledger's undocumented-denials hint becomes "(log a deviation before denying)"; every "intercom" in messages and docs becomes "the traveller wheel". | "Intercom" names a retired panel; "scan the evidence" now means the desk device. |
| R26 | **`InvestigationUIController._currentCase` is reset at the decision, before the decision callback runs** (the callback may present the next traveller at once where no READY sign is wired, and that case must not be cleared after it). | Its doc says "null between cases" (`:75-76`) but nothing resets it; `HandlePairCompared` guards on it. |
| R27 | **Papers never outlive their case, by construction.** They exist only between `ShowCase` and the decision; the day ends only after a decision or when closing time finds no traveller at the desk (`GameManager.cs:414-434`, `ShiftFlow.OnClosing`). No day-end cleanup call is added; §6 checks both closing paths. | No defensive dead code. |
| R28 | **The power button sits on the right bezel beside the glass's lower right corner** (sprite units (0.285, −0.19), world ≈ (6.99, −2.36)), inside the focus framing at 16:9 and 16:10. | The bottom bezel is outside the frame at fill 0.85 (its centre is at world y ≈ −2.96, the frame's bottom edge at −2.55). A 4:3 display cuts the button: §7. |
| R29 | **The desktop's `CanvasScaler` is removed; its wallpaper gets an `AspectRatioFitter` (EnvelopeParent)**, and the root `Canvas` a `RectMask2D`. | In World Space the scaler only sets `scaleFactor = dynamicPixelsPerUnit` (ugui `CanvasScaler.cs:269, 284-287`), which nothing reads once `DraggableWindow` stops dividing by it; the 1920×1080 wallpaper (`xp_bliss.png`) would otherwise be squeezed 25%; the mask clips the envelope overflow and its raycasts. |
| R30 | **Art contracts are amended by appended sections**, not rewritten (`OFFICE_RESET_BRIEF.md`, `HybridScene/SCENE_CONTRACT.md`, `docs/ART_ASSET_LIST.md`). | They forbid physical scanning (`OFFICE_RESET_BRIEF.md:10, 24`); appending keeps Codex's text intact for the art merge. |
| R31 (refines K7, V5, R10) | **A pending citation slip holds the screen on.** `PcScreen.SetHeld(bool)`: holding turns the screen on; while held, `Toggle` and `TurnOff` change nothing and raise nothing. `GameManager.ShowVerdictThen` calls `BoothCoordinator.SetCitationPending(true)` when it opens a citation slip and `false` in its continuation; the coordinator holds the screen and evaluates `BoothContext.CitationPending` (the power button is inert, §1.9). The citation wake and its knob go. | A decision is made on a lit, focused desktop (`DesktopInteractive` needs `ScreenOn`), and `Decide` → `HandleDecision` → `ShowVerdictThen` opens the slip in the same click (`GameManager.cs:553-641`, `OfficeUIController.cs:231-264`), so a wake at that moment could never fire. The real hazard is the player leaving focus and darkening the screen while the slip holds the clock and READY waits for Acknowledge (the camera pulls back only at the next slot, `GameManager.cs:386-392`): the booth would look frozen with no cue. The binding V5 names the citation as a row. |
| R32 (refines V8) | **A second day-1 note teaches the wheel**: a world note above the traveller ("Click the traveller to talk and ask for papers.", `DeskConfigSO.wheelHint`, `wheelHintUntilDay` = 1), shown while the traveller is at the desk until the wheel is first opened that day (`DeskHints.WheelHintVisible`, tested). `BoothCoordinator` shows it. | Day-1 tells are papers-only (one per liar), and the Transit Permit carries the only Technology field ("Declared Device", `DocTemplate_Permit.asset:19-20`), whose book is available on day 1. The permit is handed over only on request, and requests now exist only in the wheel; before this piece the always-visible intercom panel listed "Request Transit Permit". A player who never clicks the traveller could not prove a Device-tell liar. |
| R33 (refines K10, R6) | **Overlay components live on always-active hosts and toggle a child.** `TravellerWheel` toggles its `Catcher`; each `OverlayCallout` toggles its `Panel`. No overlay component's `Awake` deactivates its own GameObject. | A component on an object saved inactive first wakes inside the `SetActive(true)` that shows it; an `Awake` that closes would then deactivate the object during its own activation (Unity refuses: "GameObject is already being activated or deactivated"), so the first open would fail, and the config and camera would apply only then. A click on the catcher still reaches the host's `OnPointerClick`: `ExecuteEvents.GetEventHandler` walks up the parents (ugui `ExecuteEvents.cs:359-372`), while ring buttons handle their own clicks. |
| R34 (refines K3, R19) | **One `OverlayCallout` shows the speech bubble and the tooltips**: a timed label that takes no clicks, projected at a followed transform plus an offset. The builder makes two instances. The wheel shows the traveller's reply (`TravellerWheel.Say`, with the bubble knobs and the traveller's anchor); a reaction shows its tooltip (placement and duration on its `DeskReactionSO`). Callers apply `DisplayText`. | One mechanism, one component (MERGE_CRITERIA: no self-duplication within the PR). The bubble belongs to the conversation: it sits beside the wheel and needs the wheel's anchor and config. Tooltip placement differs per prop, so it is reaction data (K15). |
| R35 (refines R11, K21) | **Camera wiring never freezes the booth.** `CinemachineCameraRig` gets a serialized `brain`, set by the builder from Main Camera; without one (the hybrid scene) it looks the brain up in `Start` and warns once when none exists. `IsSettled` is true with no brain, and `OfficeViewController.IsSettled` is true with no rig. The monitor framing applies only when `brain.OutputCamera.orthographic`. | `CinemachineCore.FindPotentialTargetBrain` sees only brains that registered in their own `OnEnable` (`CinemachineBrain.cs:206-211`, `ActiveBrainCount` :418), and no execution order ties Main Camera to `OfficeRoot`, so a lookup in the rig's `Awake` can miss. `LensSettings.Orthographic` reads `m_OrthoFromCamera`, filled only when the camera state is pulled (`LensSettings.cs:169-170, 258-263`). A booth that never settles would keep every live input off. |
| R36 (refines K5, V4) | **The drop outcome is Domain.** `DeskPapers.Drop(i, overScanner)` returns `Stays`, `Scanning` or `Refused` and starts the scan itself; `DeskController` only animates the outcome. A paper is live only while `CanDrag` holds and it is not sliding. | MonoBehaviours orchestrate and render; they do not decide (MERGE_CRITERIA). A paper grabbed mid-slide would fight the slide, and the slide's done callback could snap it away after the drop. |
| R37 (refines K8) | **A glass zone keeps clicks on the screen inside focus.** `CRTMonitor/ScreenAnchor/GlassZone`: a hidden `SpriteRenderer`, a `BoxCollider2D` of `glassSize` and a `Clickable` that is never interactable, at `glassOrder` (between the exit zone and the bezel), active with the exit zone. | With the screen off the desktop raycaster is off, so a click on the dark glass would hit the exit zone under it and leave focus, against "a click outside the screen". A collider with no renderer reports order 0 (ugui `Physics2DRaycaster.cs:69-104`), below the exit zone, so the zone needs a renderer. The never-interactable `Clickable` takes the click with no hand cursor. |
| R38 (refines K8, R7, R36) | **Papers the booth puts away take no raycasts.** While `BoothInput.PapersLive` is false (focused, blending, a newsletter up, the wheel open), and while they leave at the decision, the papers' colliders are off: `DeskDocument.SetLive(live, raycastable)` passes the flag to `DeskDraggable.SetRaycastable`, which owns the proxy, keeps it off during a drag either way and applies the flag when the drag ends. A paper inert only for itself (sliding, scanning) keeps its collider. The rules never put the exit zone up while the papers take input (tested: `BoothRulesTests`). | Papers sort above the exit zone (30 and up against 10), and a raycast reports the group's order (R8), so an inert paper under the pointer takes the click and its non-interactable `Clickable` drops it. At 16:9 no paper shows in the focused view: the desk rectangle keeps a paper's right edge at x ≤ 3.65 and the view starts at x ≈ 3.66. On a wider screen it starts further left (x ≈ 3.05 at 2560 × 1080), so a paper at the desk's right end shows as a strip at the view's lower left and would swallow the click that leaves focus. Shrinking the desk rectangle cannot cover every aspect. A sliding or scanning paper still covers what lies under it (step 5.6: a press on the sliding permit hits the permit). |

## 1. Behaviour

### 1.1 The booth view and the live monitor

- The booth looks as today, except that the CRT shows the PC desktop live inside its glass (4:3), with a thin teal margin of the painted glass around it. Everything on the desktop runs whether you look at it closely or not: windows that open, the transcript, the clock in the tray.
- Between travellers the desktop shows the wallpaper, the taskbar and "Waiting for the next traveller".
- The traveller is not there until you tap READY, and leaves when you decide.
- A small power button with an LED sits on the CRT's right bezel.
- A note on the scanner tray says "Drag papers onto the scanner to read them on the PC." on day 1, from the moment a paper lands on the desk until your first scan finishes.
- A note above the traveller says "Click the traveller to talk and ask for papers." on day 1, while a traveller is at the desk, until you first open the wheel that day (R32).

### 1.2 Focusing the PC

- **Click the CRT:** the camera pushes in (0.6 s, `focusBlendSeconds`) until the glass fills 85% of the view's height (`monitorFill`, or of its width on narrow screens). The bezel and slivers of desk stay visible around it.
- **The desktop takes clicks only once the push-in has settled.** Clicks during a blend do nothing.
- **While focused**, only the desktop and the bezel power button respond. The desk props, the papers and the traveller are inert.
- **Leave** with Escape, a click anywhere outside the screen, or the taskbar's "< Desk" button. A click on the screen itself never leaves, even while it is dark (R37). On a screen wider than 16:9 a paper at the desk's right end can show at the left edge of the focused view; it takes no clicks while focused, so a click on it leaves too (R38). Leaving clears the desktop's keyboard focus (a Records search field stops taking keys).
- READY no longer zooms in. After every decision the camera pulls back to the booth as today (`GameManager.cs:386-392`).

### 1.3 Screen power

- The bezel button toggles the screen in both views. Start ▸ "Turn off screen" turns it off; Start ▸ "Quit game" quits (the old "Power").
- **Off** means the glass shows its painted teal, the LED is dark, and the desktop takes no input. The PC keeps running: a finished scan still opens its window, the tray clock still ticks, the verdict and the transcript still update.
- **The screen wakes itself** when a traveller is presented and when a scan finishes (two `DeskConfigSO` toggles, both on). A dark screen can still be focused; press the power button to light it.
- **While a citation slip waits for Acknowledge, the screen stays on:** the power button and Turn off screen do nothing until you acknowledge it (R31).
- The screen starts on (`screenStartsOn`). Its state lasts for the shift and is not saved.

### 1.4 The traveller at the desk

- Tapping READY presents the traveller: the placeholder figure appears, the case appears on the live monitor, and the passport slides onto the desk (1.5).
- From then until the decision, clicking the traveller, or the desk intercom, opens the wheel (1.7).
- At the decision the figure clears, the papers slide back to the traveller and vanish, and the wheel and bubble close.

### 1.5 Papers on the desk

- **Hand-over:** every document is handed over once. Documents marked "on arrival" (the Travel Passport) land when the traveller is presented; documents marked "on request" (the Transit Permit) land when you choose "Request Transit Permit" on the wheel. That choice then leaves the wheel. Papers slide in from the traveller's side to fixed desk spots (`paperSpawnSlots`).
- **A paper** shows its title ("Travel Passport"), the holder's name (the traveller's registered given name, which is also what every Name field prints, `CaseFactory.cs:393-394`) and an empty photo slot, reserved and hidden until piece 4 fills it. Its fields are read on the PC.
- **Dragging:** press on a paper and drag (the EventSystem's 10 px threshold). It lifts above everything on the desk, follows the pointer, and its centre never leaves the desk area. Released anywhere but the scanner, it stays there, on top of the other papers. A click without a drag brings it to the top.
- **A sliding paper** (being handed over, coming back from the scanner or from a refused drop, or leaving at the decision) cannot be picked up or clicked until it lands (R36).
- Papers hover like every booth clickable: white outline, hand cursor.

### 1.6 The desk scanner

- **Drop a paper on the scanner tray** (the pointer inside the tray when you release): the paper snaps onto the glass bed and scans for 1.5 s (`scanSeconds`; the shift clock keeps running). It then slides back to where you picked it up, the scanner pulses, and the paper's scanned window opens on the PC. The camera does not move.
- The window is the existing scanned-page document window. The first time a paper's window opens, the paper also gets a desktop icon at the top of the icon grid, which reopens the window after you close it.
- **One scan at a time:** a paper dropped while another is scanning slides back to where you picked it up.
- A paper can be scanned again; its window reopens and comes to the front (no second icon).
- A paper is not draggable while it scans.
- Comparing, the Deviation Report, and Accept/Deny happen on the PC exactly as before.

### 1.7 The traveller wheel and the speech bubble

- **Open** it by clicking the traveller or the desk intercom while the traveller is at the desk, in the booth view, with no newsletter up. It opens around the traveller: an elliptical ring of up to 8 choices.
- **The hub** holds "Request <document>" for each document not yet handed over on request, "Ask about home >", and today's narrative dialogs: the same interview as piece 3, minus the arrival documents. Hovering outlines a choice (the desktop's amber hover).
- **A sub-menu replaces the ring in place**, with "< Back" in the centre. Authored dialog nodes have no Back, as in piece 3.
- **Close** it with Escape or a click outside the ring. It closes without choosing and reopens where the interview was. Choosing a request also closes it, so you can take the paper. Other choices leave it open.
- **Replies:** the traveller's lines added by a choice appear in a speech bubble to the right of the wheel for 4 s (`bubbleSeconds`), and every line goes into the PC transcript, which opens on the live monitor as in piece 3. Only transcript answer rows are compare-clickable ("Traveller · CAPITAL"). The bubble never takes clicks. It outlives the ring (a request's reply stays up while you take the paper) and clears when you focus the PC or the traveller leaves (whenever the wheel may not open).
- The desk intercom speaker is now a way to open the wheel; the PC intercom panel is gone.

### 1.8 Other desk objects

| Object | Click |
|---|---|
| Stamp | Squash ("thunk"); no stamping yet (K11) |
| Mug, plant, poster | Wobble |
| Intercom speaker | Squash, and opens the wheel when a traveller is at the desk |
| Scanner tray | Pulse (also played when a scan finishes) |
| Credits till | Nudge + tooltip "Credits: {value}" |
| Calendar (painted on the left partition) | Tooltip "Day {value}" |
| Stability monitor | Tooltip "Timeline stability: {value}" |
| Wall clock | Tooltip "{value}" (the tray clock) |
| CRT | Focus (1.2) |
| Power button | Screen on/off (1.3) |
| READY | Calls the next traveller (unchanged gate) |

- A tooltip shows above the object for 2.5 s (the reaction's `tooltipSeconds` and `tooltipOffset`). No object has sound yet (no clips exist); a clip set on a reaction plays through the object's `AudioSource` when it has one.
- The plant and the mug stand at named desk slots (`DeskSlot`); photo and free slots exist, empty, for decoration later. Only papers can be dragged in piece 7.

### 1.9 Who takes input when

| Situation | Desktop | CRT | Power button | Exit zone | Props | Papers | Traveller / wheel |
|---|---|---|---|---|---|---|---|
| Briefing or results newsletter up | no | no | no | – | no | – | no |
| Booth view, settled, no traveller | no | yes | yes | – | yes | – | no |
| Booth view, settled, traveller at desk | no | yes | yes | – | yes | yes | yes |
| Wheel open | no | no | no | – | no | no | the wheel only |
| Camera blending (either way) | no | only when heading to the booth | no | no | no | no | no |
| Focused, settled, screen on | yes | no | yes | yes | no | no | no |
| Focused, settled, screen off | no | no | yes | yes | no | no | no |
| Citation slip pending (the traveller has left) | as the view's row | as the view's row | no | as the view's row | as the view's row | – | no |

While a citation slip is pending, the screen is held on (R31), so the "screen off" row cannot occur; the slip holds the clock until acknowledged, as today. While focused, the exit zone's column also governs the glass zone (R37): a click on the glass, lit or dark, never leaves focus. Where the Papers column says no, the papers take no clicks at all, and a click on one reaches what lies under it (R38).

### 1.10 The desktop at 4:3

- The desktop is 1440 × 1080 units: the same height as before, three quarters of the width. The claim strip, icon grid, compare bar, Accept/Deny and taskbar keep their anchors, and therefore their heights and font sizes.
- The intercom column is gone, freeing the right side for the transcript window.
- Scanned documents open cascaded on the left, clear of the icon column. Book windows open in two staggered rows. Every window is clamped inside the screen while dragged: if it is taller than the screen, its title bar stays visible.
- The taskbar gains "< Desk". The Start menu holds Settings, Turn off screen and Quit game.
- The desktop apps "Scanner" and "Material Scanner" are now titled "Deviation Report" and "Material Analysis".

### 1.11 Scenes without the new pieces

- **`OfficeScene_HybridArt.unity`** (Codex's scene, not rebuilt and not edited) has no `MonitorScreen`, desk, wheel, bubble or booth coordinator. Its desktop stays a full-screen overlay shown only when focused (R14). Its intercom panel keeps listing the interview (R4). Requests open windows directly, arrival documents open at presentation (R3), and one warning names the builder. Its Start menu's "Power" entry still quits: the field keeps its saved reference under its new name, `quitButton` (§2.10). Its window spawn positions are unchanged (R23), and its camera rig finds the brain itself (R35).
- **`Test_DayLoop.unity`** (build scene 0) runs the legacy era-pick path. Every new `GameManager` reference is null there, and nothing changes.
- **The text fallback** (no rich desk) prints all papers as today.

### 1.12 Determinism, time and saves

- Desk interactions draw no random numbers. Case generation is byte-identical to `main`: `spoken` still reads `InterviewReachable`, whose inputs (the ring's `InteractionPanelController`, the transcript and its chrome) are all wired (V7). Checked in §6.
- The shift clock keeps running during scans, blends and wheel use (V9). With scans (1.5 s each) and walking between desk and PC, a day effectively gets shorter (§7).
- Nothing is saved mid-shift: screen power, paper positions and scans are per shift or per case. `SaveSystem.SaveVersion` stays 2.

### 1.13 Edge cases

- **READY while focused:** impossible, since the exit zone covers the booth.
- **Decision during a scan:** the scan is cancelled; no window opens, and every paper leaves.
- **A citation slip pending:** the decision was made on a lit, focused desktop, and the camera stays on the monitor until the next slot. If the player leaves focus, the screen cannot be turned off until Acknowledge (R31), so the slip stays visible on the live monitor, and clicking the CRT returns to it.
- **A paper grabbed mid-slide:** impossible; a sliding paper is not live (R36).
- **The first click on the traveller** opens the wheel: its host is active from load (R33).
- **Closing time with a traveller at the desk:** they stay, as do the papers and the wheel, until you decide (`ClosingAction.FinishCurrent`, `GameManager.cs:416-423`). **Closing time behind READY:** nothing was handed over (R27).
- **A focus request while dragging:** none exists. A case starts only after a decision taken on the PC, so `GameManager.FocusOffice` never arrives mid-drag.
- **Wheel open when the traveller leaves:** leaving the `TravellerAtDesk` phase closes it.
- **More papers than spawn slots:** slots are reused in order. The builder reports a desk with fewer slots than the most papers any traveller carries.
- **No interview day injected** (a content error): the wheel opens empty and closes normally. The existing error names the problem.

## 2. Design

### 2.1 Where things live

- **TimeDesk.Domain** (`Assets/Scripts/Domain`, EditMode-tested):
  - new: `BoothRules.cs`, `PcScreen.cs`, `DeskPapers.cs`;
  - changed: `Dialog.cs`, `InterviewScript.cs`, `InterviewContent.cs` (docs), `DiscrepancyLog.cs` (doc).
- **TimeDesk.Visuals** (`Assets/Scripts/Visuals`, engine-free, EditMode-tested): new `DeskGeometry.cs`, `RadialLayout.cs`, `MonitorFraming.cs`, `ReactionCurve.cs`, `DisplayText.cs`.
- **Assembly-CSharp:**
  - new `Assets/Scripts/Office/`: `DeskConfigSO.cs`, `MonitorScreen.cs`, `BoothCoordinator.cs`;
  - new folder `Assets/Scripts/Office/Desk/` (+ meta): `DeskSurface.cs`, `DeskDraggable.cs`, `DeskDocument.cs`, `DeskScanner.cs`, `DeskController.cs`, `DeskReaction.cs`, `DeskReactionSO.cs`, `DeskSlot.cs`, `DeskItem.cs`;
  - new folder `Assets/Scripts/Characters/` (+ meta): `TravellerView.cs`, the file piece 4's draft planned to create (`characters-design.md:168`);
  - new `Assets/Scripts/UI/`: `TravellerWheel.cs`, `RadialLayoutGroup.cs`, `OverlayCallout.cs`, `OverlayProjection.cs`;
  - changed: `Office/OfficeViewController.cs`, `Office/ICameraRig.cs`, `Office/CinemachineCameraRig.cs`, `UI/InvestigationUIController.cs`, `UI/InteractionPanelController.cs`, `UI/DraggableWindow.cs`, `UI/HoverHighlighter.cs`, `UI/TranscriptWindowController.cs`, `UI/DocumentWindowController.cs`, `UI/DesktopShell.cs`, `UI/CompareController.cs` (doc), `UI/DayFlowUIController.cs` (copy), `GameManager.cs`, `DocumentTemplateSO.cs`.
- **Assembly-CSharp-Editor:** `OfficeSceneUIBuilder.cs` (becomes `partial`) + new `OfficeSceneUIBuilder.Desk.cs`; `ContentLibraryValidator.cs`; `WorldContentGenerator.cs`.
- **Tests:** `Assets/Tests/EditMode`. The test assembly references only TimeDesk.Domain and TimeDesk.Visuals, so every new rule and every piece of maths is there.

Line endings, as in the working tree (`git ls-files --eol`, `w/` column):
- **CRLF:** `OfficeViewController.cs`, `ICameraRig.cs`, `CinemachineCameraRig.cs`, `InvestigationUIController.cs`, `InteractionPanelController.cs`, `DraggableWindow.cs`, `DocumentWindowController.cs`, `DesktopShell.cs`, `CompareController.cs`, `DayFlowUIController.cs`, `GameManager.cs`, `DocumentTemplateSO.cs`, `DiscrepancyLog.cs`, `ContentLibraryValidator.cs`, `DocTemplate_Passport.asset`, `docs/FEATURES.md`, `docs/ART_ASSET_LIST.md`, the two `ArtDeliverables` briefs, the audit plan.
- **LF:** `HoverHighlighter.cs`, `TranscriptWindowController.cs`, `OfficeSceneUIBuilder.cs`, `WorldContentGenerator.cs`, `Dialog.cs`, `InterviewScript.cs`, `InterviewContent.cs`, `InterviewScriptTests.cs`, `InterviewDayTests.cs`, the three draft specs.
- **New files:** LF. Edits go through `SCRATCH/subs.py`, never `sed -i`.

### 2.2 Domain: booth input rules (`BoothRules.cs`, new)

- `public enum BoothPhase { NoTraveller, TravellerAtDesk, Newsletter }`: "Where the shift is, as the booth's input rules need it. Set by GameManager." (`NoTraveller` first: the default before `Start`.)
- `public readonly struct BoothContext`:
  - fields: `bool Focused` (the view is the monitor), `bool Settled` (the rig shows that view with no blend running), `bool ScreenOn`, `BoothPhase Phase`, `bool WheelOpen`, `bool CitationPending` (a citation slip waits for Acknowledge, R31);
  - one constructor taking all six.
- `public readonly struct BoothInput` (outputs; one bool each, with docs):
  - `DesktopInteractive` = `Focused && Settled && ScreenOn && Phase != Newsletter`;
  - `CrtFocusable` = `!Focused && Phase != Newsletter && !WheelOpen`;
  - `PowerButtonLive` = `Settled && Phase != Newsletter && !WheelOpen && !CitationPending`;
  - `FocusExitLive` = `Focused && Settled` (the exit zone and the glass zone, R37);
  - `PropsLive` = `!Focused && Settled && Phase != Newsletter && !WheelOpen`;
  - `PapersLive` = `PropsLive && Phase == TravellerAtDesk`;
  - `WheelAllowed` = `!Focused && Settled && Phase == TravellerAtDesk` (false closes an open wheel);
  - `TravellerLive` = `WheelAllowed && !WheelOpen` (the hit zone).
- `public static class BoothRules`: `public static BoothInput Evaluate(BoothContext c)`. Its summary names the table in §1.9.

### 2.3 Domain: screen power (`PcScreen.cs`, new)

- `public enum WakeReason { TravellerPresented, ScanFinished }`.
- `[Serializable] public sealed class PcWakeRules` with fields `bool onTravellerPresented = true` and `bool onScanFinished = true` (docs on each). It is held by `DeskConfigSO`, following the precedent of `InterviewLines` held by `ContentLibrarySO`. A citation slip is not a wake rule and has no knob: it always holds the screen (R31).
- `public sealed class PcScreen`:
  - `PcScreen(bool startsOn, PcWakeRules rules)`; a null `rules` wakes on nothing;
  - `bool IsOn { get; }`;
  - `event Action Changed`, raised only on a real change;
  - `bool Toggle()` flips the screen and returns true; while held it changes nothing and returns false;
  - `bool TurnOff()` returns true when it turned the screen off (false when it was off, or held);
  - `bool Wake(WakeReason reason)` returns true when it turned the screen on (off, and the reason's rule is on). An unknown reason never wakes;
  - `void SetHeld(bool held)`: holding turns the screen on (raising `Changed` when it was off); while held, `Toggle` and `TurnOff` refuse; releasing changes nothing else. Its summary: "A held screen is always on (a pending citation slip)".

### 2.4 Domain: papers (`DeskPapers.cs`, new)

- `public enum DocumentHandOver { OnRequest, OnArrival }`. Doc: "When the traveller hands a document over. Serialized on DocumentTemplateSO: append only." `OnRequest` is 0, so existing assets read as OnRequest.
- `public sealed class CaseDocument`:
  - fields `string name` (display name), `string holder` (the name the paper shows: the traveller's registered given name), `DocumentHandOver handOver`;
  - `bool Requested => DocumentHandOvers.IsRequested(handOver)`.
- `public static class DocumentHandOvers`: `bool IsRequested(DocumentHandOver handOver)` = `handOver == OnRequest`, the one home of "waits for a request" (callers: `CaseDocument.Requested` and `ContentLibraryValidator.MaxRequestedDocuments`).
- `public static class CaseDocuments`: `IReadOnlyList<int> ArrivalIndices(IReadOnlyList<CaseDocument> documents)`, the documents handed over on arrival in paper order (null entries skipped, a null list gives none). Callers: the `DeskPapers` constructor and `InvestigationUIController.ShowRich`'s no-desk path.
- A private nested `enum PaperState { WithTraveller, OnDesk, Scanning, Returned }` inside `DeskPapers` (no public signature uses it).
- `public enum DropOutcome { Stays, Scanning, Refused }`: "What happens to a paper released on the desk: it stays where it was dropped, it starts scanning, or it goes back to where it was picked up."
- `public sealed class DeskPapers`: one case's papers.
  - `DeskPapers(IReadOnlyList<CaseDocument> documents, float scanSeconds)`: every paper starts `WithTraveller`. A null list means no papers. `scanSeconds` below 0.01 is raised to 0.01.
  - `int Count`; `bool ScannerBusy`; `IReadOnlyList<int> ArrivalIndices` (the OnArrival papers in paper order, from `CaseDocuments.ArrivalIndices`); `int OnDeskCount` (papers `OnDesk` or `Scanning`). A paper's state is private (`StateOf`); callers and tests see it through these members and the methods below.
  - `bool HandOver(int i)`: `WithTraveller` → `OnDesk`, true. Any other state, or an index out of range, gives false.
  - `bool CanDrag(int i)`: the paper is `OnDesk` (false out of range).
  - `DropOutcome Drop(int i, bool overScanner)`: a paper that is not `OnDesk` (or an index out of range) gives `Refused` and nothing changes; an `OnDesk` paper not over the scanner gives `Stays`; over the scanner it gives `Scanning` while the scanner is idle (the paper → `Scanning`, the timer at 0) and `Refused` while it is busy (nothing changes). A scanned paper may be scanned again. (The scan start is private; `Drop` is its only caller.)
  - `int Tick(float seconds)`: advances a running scan by a positive amount (0 and negative amounts are ignored). When the elapsed time reaches `scanSeconds`, the paper goes back to `OnDesk` and the method returns its index. Otherwise it returns −1. With no scan running it returns −1.
  - `void ReturnAll()`: every paper → `Returned`; a running scan is cancelled and never finishes.
- `public static class DeskHints`:
  - `bool ScanHintVisible(string hint, int day, int untilDay, int scansToday, bool paperOnDesk)` = the hint is non-blank, `day <= untilDay`, `scansToday == 0` and `paperOnDesk`;
  - `bool WheelHintVisible(string hint, int day, int untilDay, bool wheelOpenedToday, bool travellerAtDesk)` = the hint is non-blank, `day <= untilDay`, `!wheelOpenedToday` and `travellerAtDesk` (R32).

### 2.5 Domain: interview changes

**`Dialog.cs`** (LF):
- `DialogAction.OpenDocument` (20-21) → `HandOverDocument`: "The traveller hands a document over (DialogChoice.DocumentIndex): onto the desk, or straight to its window where no desk is wired." It is runtime-only (`ScriptChoice` has no action field, `InterviewContent.cs:47-64`), so the rename is serialization-safe.
- New `public enum DialogChoiceKind { Normal, Back }`: "What a choice is, for renderers: the wheel puts Back in its centre. Piece 8 appends kinds."
- `DialogChoice.Kind` (default `Normal`). `Label`'s doc (79) becomes "The choice's label on the traveller wheel". `DocumentIndex`'s doc (94) becomes "HandOverDocument: index of the document in paper order".

**`InterviewScript.cs`** (LF):
- `InterviewCase.documentNames` (15-16) → `public IReadOnlyList<CaseDocument> documents;`: "The traveller's documents in paper order; only those handed over on request get a hub request."
- `Build` (97-113): loop over `documents`; skip entries that are not `Requested`. Each request keeps its id `request:{i}` (i = paper index), label, lines and `DocumentIndex = i`, and gains `Action = HandOverDocument` and `OneShot = true`.
- The ask menu's `back` (115) gets `Kind = DialogChoiceKind.Back`.
- New `public static string SpokenSince(IReadOnlyList<DialogLine> transcript, int from)`: "What the traveller said since transcript line `from`: the texts of the Traveller lines at or after it, in order, joined with a new line; empty when there are none." A null transcript gives ""; `from` below 0 counts as 0. Caller: `InvestigationUIController.Choose` (the reply the wheel shows, R34).
- The summary (79-88): "request:{i} per document handed over on request (one-shot, hands document i over)".
- `DialogChecks.Problems` message (296): "...; the traveller wheel shows at most {maxChoices}".
- `MenuProblems(int questions, bool smallTalk, int maxRequestedDocuments, int dialogs, int maxChoices)`: the parameter is renamed; the doc (359-364) and messages (373, 377) say "the most documents one traveller hands over on request" and "the traveller wheel shows at most {maxChoices}".

**`InterviewContent.cs`** (LF): the `InterviewLines` doc (167) and `menuCapacity` (216) say "the traveller wheel" instead of "the intercom".

**`DiscrepancyLog.cs`** (CRLF): `CompareEvidence.value` (50) doc becomes "The canonical value (never display text: see DisplayText)".

### 2.6 Visuals: desk maths (engine-free, new)

**`DeskGeometry.cs`:**
- `public readonly struct DeskRect`:
  - `DeskRect(float centreX, float centreY, float width, float height)`; negative sizes count as 0;
  - `bool Contains(float x, float y)`, edges inclusive;
  - `(float x, float y) Clamp(float x, float y)`;
  - `(float x, float y) PointAt(float u, float v)`, with u and v clamped to 0..1 (0,0 = bottom left).
- `public static class RectClamp`: `float Shift(float min, float max, float lo, float hi, bool keepMax)` is the offset that moves the span [min, max] inside [lo, hi]:
  - 0 when it already is inside;
  - when the span is longer than [lo, hi], it aligns `max` to `hi` if `keepMax` (the title bar stays visible), else `min` to `lo`.
- `public sealed class PaperStack`, the z order of papers by id, bottom first (papers never leave the stack one by one; the case's end clears it):
  - `Add(int id)` puts the paper on top (re-adding moves it);
  - `BringToFront(int id)`; `Clear()`;
  - `int IndexOf(int id)`, −1 when absent.
- `public static class SortingBands`: `List<string> Problems(int maxPropOrder, int focusExitOrder, int glassOrder, int bezelOrder, int screenCanvasOrder, int paperBaseOrder, int maxPapers, int heldPaperOrder)`. Each band must sit strictly above the one before: props < exit zone < glass zone < bezel < screen < paper base, and held > paper base + maxPapers − 1. Each problem names both values.

**`RadialLayout.cs`:**
- `(float x, float y) Point(int i, int n, float radiusX, float radiusY)`: item 0 at the top, then clockwise, at angle 90° − i·360°/n on the ellipse. n ≤ 0 gives (0, 0).
- private `bool Fits(int n, float radiusX, float radiusY, float itemW, float itemH, float centreW, float centreH, float gap)`: no two ring items overlap, and no ring item overlaps the centre box. Two boxes are apart when |dx| ≥ (wa + wb)/2 + gap or |dy| ≥ (ha + hb)/2 + gap. n ≤ 1 checks only the centre. Its only caller is `MaxFit`, so it is private and tested through `MaxFit`.
- `int MaxFit(float radiusX, float radiusY, float itemW, float itemH, float centreW, float centreH, float gap, int limit)`: the largest m ≤ limit such that `Fits(k)` holds for every k in 1..m (the wheel shows any count up to its capacity); 0 when even one item does not fit. Caller: the builder's wheel check.
- Worked numbers (the defaults, pinned by `MaxFit` tests), with radii 300 × 200, items 240 × 44, centre 150 × 44, gap 8:
  - n = 7: the lowest pair sits at (±130.1, −180.2), 260.2 ≥ 248 apart;
  - n = 8: the top neighbours are (0, 200) and (212.1, 141.4), with dy 58.6 ≥ 52;
  - n = 9: items 4 and 5 sit level (±102.6, −187.9), 205.2 < 248 apart;
  - so `MaxFit(limit 16) == 8`. A circle of radius 210 fits 4: n = 5 already collides (items 2 and 3 sit level at (±123.4, −169.9), 246.9 < 248 apart).
- `(float width, float height) Extent(float radiusX, float radiusY, float itemW, float itemH)` = (2 · radiusX + itemW, 2 · radiusY + itemH), the box around every item the ring can place ((840, 444) for the defaults). Caller: `TravellerWheel.Awake` sizes the ring's rect from it, so `OverlayProjection` keeps the whole ring on screen, not only its centre.

**`MonitorFraming.cs`:** `float OrthoSize(float width, float height, float aspect, float fill)` = max(height, width / aspect) / (2 · fill). `fill` is clamped to [0.05, 1]; an aspect ≤ 0 counts as 1. Worked example: 2.343 × 1.758 at 16:9 and fill 0.85 gives 1.034.

**`ReactionCurve.cs`:**
- `public enum ReactionKind { None, Squash, Wobble, Nudge, Pulse }` (serialized on `DeskReactionSO`: append only).
- `public readonly struct ReactionPose { float ScaleX, ScaleY, AngleDeg, OffsetY }`, with `Identity`.
- `ReactionPose Evaluate(ReactionKind kind, float t, float amplitude)`. t is clamped to 0..1, and s = sin(πt), which is 0 at both ends.
  - `Squash`: scale (1 + a·s, 1 − a·s);
  - `Pulse`: scale (1 + a·s, 1 + a·s);
  - `Nudge`: offset −a·s;
  - `Wobble`: angle a · 30° · sin(3πt) · (1 − t);
  - `None`: identity.
- Every kind is identity at t = 0 and t = 1.

**`DisplayText.cs`:**
- `public enum TextMedium { Written, Spoken }`.
- `public static string For(string canonical, TextMedium medium)` returns `canonical ?? ""`.
- Summary: "The one place displayed text may differ from its canonical value (item 8's translation reveals, piece 9). Never pass its result to CompareController or CompareEvidence."

### 2.7 Knobs

**`DeskConfigSO`** (new, `[CreateAssetMenu(menuName = "TimeDesk/Office/Desk Config")]`; the asset is `Assets/Data/Config/Desk_Default.asset`, created by the builder). Every field has a doc comment:

| Group | Field | Default |
|---|---|---|
| Monitor focus | `monitorFill` `[Range(0.5, 1)]` | 0.85 |
| | `focusBlendSeconds` `[Min(0)]` | 0.6 |
| Screen power | `screenStartsOn` | true |
| | `wake` (`PcWakeRules`) | both true |
| | `ledOnColor` / `ledOffColor` | (0.35, 0.95, 0.45) / (0.15, 0.17, 0.15) |
| Scanner | `scanSeconds` `[Min(0.1)]` | 1.5 |
| | `scanHint` (ASCII) | "Drag papers onto the scanner to read them on the PC." |
| | `scanHintUntilDay` `[Min(0)]` | 1 |
| Papers | `paperSpawnSlots` (Vector2[], 0..1 in the desk rectangle) | (0.58, 0.71), (0.76, 0.66), (0.62, 0.29), (0.84, 0.26) |
| | `paperSlideSeconds` `[Min(0)]` | 0.25 |
| Sorting bands | `focusExitOrder`, `glassOrder`, `bezelOrder`, `screenCanvasOrder`, `paperBaseOrder`, `heldPaperOrder` | 10, 11, 12, 20, 30, 60 |
| Wheel (overlay reference px) | `wheelRadii`, `wheelItemSize`, `wheelCentreSize`, `wheelItemGap` | (300, 200), (240, 44), (150, 44), 8 |
| | `wheelHint` (ASCII) | "Click the traveller to talk and ask for papers." |
| | `wheelHintUntilDay` `[Min(0)]` | 1 |
| Reply bubble | `bubbleSeconds` `[Min(0.1)]`, `bubbleOffset` (overlay reference px from the traveller's anchor) | 4, (650, 100) |

**`DeskReactionSO`** (new, `[CreateAssetMenu(menuName = "TimeDesk/Office/Desk Reaction")]`):
- fields: `ReactionKind kind`, `[Min(0.05)] float seconds = 0.35`, `float amplitude = 0.12`, `AudioClip clip` (optional; the project has none yet), `[TextArea] string tooltip` ("{value}" is the readout's text; empty = no tooltip), `[Min(0.1)] float tooltipSeconds = 2.5`, `Vector2 tooltipOffset = (0, 60)` (overlay reference px above the object) (R34);
- ten assets under `Assets/Data/Config/DeskReactions/`: `Reaction_Stamp` (Squash), `Reaction_Mug`, `Reaction_Plant` and `Reaction_Poster` (Wobble), `Reaction_Intercom` (Squash), `Reaction_Scanner` (Pulse), `Reaction_Till` (Nudge, "Credits: {value}"), `Reaction_Calendar` (None, "Day {value}"), `Reaction_Stability` (None, "Timeline stability: {value}"), `Reaction_Clock` (None, "{value}");
- the builder creates a missing asset with these values and keeps a designer's edits (the `InteractionFeedback_Default` pattern, `OfficeSceneUIBuilder.cs:1334-1378`).

### 2.8 Office components

**`MonitorScreen`** (new, on `CRTMonitor`):
- **Fields:**
  - `Canvas desktopCanvas`;
  - `GraphicRaycaster desktopRaycaster`;
  - `Transform glass` (the `ScreenAnchor`; its local XY is the screen plane);
  - `Vector2 glassSize` (the glass rectangle in the anchor's local units);
  - `SpriteRenderer powerLed`;
  - `DeskConfigSO config`.
- **Awake:** builds a `PcScreen(config.screenStartsOn, config.wake)`; with no config, `(true, null)` plus one warning naming the builder. It subscribes to `PcScreen.Changed`, applies, and turns interaction off.
- **Members:**
  - `bool IsOn`;
  - `event Action PowerChanged`;
  - `void TogglePower()`, the bezel button's persistent call;
  - `void TurnOff()`, from the Start menu;
  - `void Wake(WakeReason reason)`;
  - `void SetHeld(bool held)`, from `BoothCoordinator.SetCitationPending` (R31);
  - `void SetInteractive(bool on)`: sets `desktopRaycaster.enabled`. Turning off also clears `EventSystem.current`'s selection when the selected object is under the desktop canvas;
  - `Vector3 GlassCentre`;
  - `Vector2 GlassWorldSize`: `glassSize` times the anchor's lossy scale.
- **Apply:** `desktopCanvas.enabled = IsOn`; the LED colour from the config.

**`OfficeViewController`** (CRLF):
- **New field:** `[SerializeField] private MonitorScreen monitorScreen;`: "Optional: the live desktop on the CRT. When set, the desktop canvas stays active (the booth coordinator gates its input); when not (the hybrid scene), it is shown only in MonitorFocus."
- **Kept:** the serialized `cameraRigBehaviour` and `desktopRoot`. The docs of the class (15-20) and of `desktopRoot` (26) describe both paths.
- **`bool IsSettled`**, plus `event Action<OfficeView> Settled`, raised once per view change when `_rig.IsSettled(Current)` first holds. It is polled in `Update` only while unsettled. With no rig (`_rig == null`) the view is settled at once: `ApplyState` sets `IsSettled` and raises `Settled` itself (R35).
- **`ViewChanged`** (35) keeps its meaning and now has a subscriber (`BoothCoordinator`).
- **`ApplyState`** (88-102): `desktopRoot.SetActive(monitor)` runs only when `monitorScreen == null`, and it clears `IsSettled` when a rig exists.
- **`Update`:** the settle poll runs first, in either view, so blending back to the booth also settles; then the Escape check, which still returns early unless `Current == MonitorFocus` (49-57).
- **Docs:** the `Update` summary (45-48, "temporary 'back' affordance until the desktop has a dedicated minimize-to-office control") becomes "Escape leaves the monitor, like the desktop's '< Desk' button and a click outside the screen", and it names the settle poll.
- **Removed:** `InitForTest` (59-66) and `Toggle` (74-76) (R15).
- **Allocations:** none per frame.

**`ICameraRig`** (CRLF): gains `bool IsSettled(OfficeView view)`: "True when the rig shows that view's camera and no blend is running."

**`CinemachineCameraRig`** (CRLF):
- **New optional fields:** `[SerializeField] private CinemachineBrain brain;` ("The brain on the camera these cameras drive; set by the builder. Unset (the hybrid scene), it is looked up in Start"), `[SerializeField] private MonitorScreen monitorScreen;` and `[SerializeField] private DeskConfigSO config;` (R35).
- **`Start`** (after every `OnEnable`, so every brain has registered): when `brain` is unset it is looked up once with `CinemachineCore.FindPotentialTargetBrain(officeCam)`; when none is found, one warning names the builder. With a brain and a `config` it sets `brain.DefaultBlend = new CinemachineBlendDefinition(EaseInOut, config.focusBlendSeconds)`; otherwise the scene's blend stays (2 s today, `OfficeScene.unity:23667-23669`).
- **`ShowMonitor`:** priorities as today. The reframe runs only when `brain`, `brain.OutputCamera`, `monitorScreen` and `config` are all set and `brain.OutputCamera.orthographic`: it moves `monitorCam` to the glass centre (keeping z) and sets `Lens.OrthographicSize = MonitorFraming.OrthoSize(size.x, size.y, brain.OutputCamera.aspect, config.monitorFill)`. A perspective camera (the hybrid scene) is left alone. (`Lens.Orthographic` is not used: it reads a flag Cinemachine fills only when it pulls the camera state, `LensSettings.cs:169-170, 258-263`.)
- **`IsSettled(view)`:** true with no brain (the camera never moves, so the booth must not wait for it); otherwise `!brain.IsBlending && brain.ActiveVirtualCamera == (ICinemachineCamera)cam` (R11).

**`BoothCoordinator`** (new, on `OfficeRoot`), "applies BoothRules and the wake rules to the booth":
- **Fields (all optional):**
  - `OfficeViewController view`, `MonitorScreen screen`, `DeskController desk`, `TravellerWheel wheel`;
  - `Clickable crt`, `Clickable powerButton`, `Clickable focusExit`, `Clickable glassZone`, `Clickable travellerHitZone`;
  - `Clickable[] props`;
  - `TMP_Text wheelHint` (the day-1 note above the traveller, R32) and `DeskConfigSO config` (its text and last day).
- **Members** (the phase, the day, the citation flag and "wheel opened today" are private state; nothing outside reads them):
  - `void SetPhase(BoothPhase phase)`: entering `TravellerAtDesk` also calls `screen.Wake(WakeReason.TravellerPresented)`;
  - `void BeginDay(int day)`: keeps the day, clears "wheel opened today", and forwards to the desk;
  - `void SetCitationPending(bool pending)`: keeps the flag, calls `screen.SetHeld(pending)` and re-applies (R31).
- **Awake:** sets the wheel hint's text from the config.
- **Subscriptions** (in `OnEnable`, removed in `OnDisable`): `view.ViewChanged`, `view.Settled`, `screen.PowerChanged`, `wheel.OpenChanged` (an open sets "wheel opened today") and `desk.ScanFinished` (which calls `Wake(ScanFinished)`). Each one re-applies.
- **Apply** evaluates `BoothRules` with the view, settledness, power, phase, wheel state and citation flag, then sets:
  - `screen.SetInteractive(DesktopInteractive)`;
  - `crt.Interactable = CrtFocusable` and `powerButton.Interactable = PowerButtonLive`;
  - `focusExit.gameObject.SetActive(FocusExitLive)` and `glassZone.gameObject.SetActive(FocusExitLive)` (R37);
  - every prop's `Interactable = PropsLive`;
  - `desk.SetPapersLive(PapersLive)`;
  - `wheel.SetCanOpen(WheelAllowed)`;
  - `travellerHitZone.Interactable = TravellerLive`;
  - `wheelHint.gameObject.SetActive(DeskHints.WheelHintVisible(config.wheelHint, day, config.wheelHintUntilDay, wheelOpenedToday, phase == TravellerAtDesk))` (hidden with no config).
- **Null references:** each missing one is skipped. A missing view counts as the booth, settled; a missing screen counts as on.

**`TravellerView`** (new, `Assets/Scripts/Characters`, on `Traveller`):
- **Fields:**
  - `[SerializeField] private Renderer[] figure;`: "What shows while the traveller is at the desk (the placeholder sprite now; piece 4's layers)";
  - `[SerializeField] private Transform anchor;`: "Where the wheel and the bubble centre (the traveller's chest)".
- **Members:** `Transform Anchor` (read by `TravellerWheel` for the ring and the reply bubble: the one reference to that point), `void Show()` and `void Clear()` (called by `GameManager.SetTravellerAtDesk`). There is no presence getter: nothing reads one.
- **Awake:** calls `Clear()`, so the figure is hidden until presented.
- **Piece 4 extends this component** (§2.21).

### 2.9 Desk components (`Assets/Scripts/Office/Desk`, new)

**`DeskSurface`:**
- `[SerializeField] private Vector2 size;`: the rectangle that paper centres stay in, in local XY, centred on the transform;
- `bool TryProject(Camera cam, Vector2 screenPoint, out Vector3 world)`: the camera ray against the plane (position, forward); false when the ray is parallel or the hit is behind the camera;
- `Vector3 Clamp(Vector3 world)`: to local, `DeskRect.Clamp`, back to world;
- `Vector3 PointAt(Vector2 uv)`.

**`DeskDraggable`** (`IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`; generic, used by papers now):
- **Fields:** `Collider2D proxy` (orders go through the owner, `DeskDocument.SetOrder`, so the draggable holds no sorting reference).
- **Members:**
  - `void Init(DeskSurface surface)`;
  - `event Action<DeskDraggable> DragBegan`;
  - `event Action<DeskDraggable, Vector3> DragEnded`, carrying the projected pointer point, or the paper's position when the projection fails;
  - `Vector3 PickUpPosition`;
  - `void SetRaycastable(bool raycastable)`: the proxy's state outside a drag; during a drag the flag is only remembered (R38).
- **Begin:** records the pick-up position and the grab offset, and turns the proxy off.
- **Drag:** `position = surface.Clamp(projected + offset)`.
- **End:** turns the proxy back on (unless the owner has taken the object out of the raycast, R38) and raises `DragEnded`.
- **When disabled** (by the owner), the EventSystem sends it nothing (`ExecuteEvents.ShouldSendToComponent` requires `isActiveAndEnabled`, ugui `ExecuteEvents.cs:304-314`).

**`DeskDocument`** (the paper; on a `SortingGroup` root):
- **Fields** (the paper's `SpriteRenderer` sits on the root itself, R8, and no code reads it, so there is no field for it): `TextMeshPro title`, `TextMeshPro holder`, `GameObject photoSlot` (inactive: "reserved for piece 4's passport photo"), `Clickable click`, `DeskDraggable drag`, `SortingGroup group`.
- **Members:**
  - `int Index` (read by `DeskController` to map a drag or click to its paper);
  - `void Bind(int index, CaseDocument doc)`: the title and the holder are the canonical strings (the paper stays in its source script; the scanned copy is what piece 9 translates, R19);
  - `void SetOrder(int order)`;
  - `void SetLive(bool live, bool raycastable)`: `drag.enabled`, `click.Interactable` and `drag.SetRaycastable(raycastable)` (R38);
  - `void SlideTo(Vector3 target, float seconds, Action done)`: a linear move in `Update`, only while sliding;
  - `bool IsSliding` (read by `DeskController`'s liveness rule, R36).

**`DeskScanner`** (on `ScannerTray`):
- **Fields:** `Vector2 dropSize` (local, centred), `Vector2 bedCentre` (local), `DeskReaction reaction`.
- **Members:**
  - `bool Contains(Vector3 world)`: local, then `DeskRect.Contains`;
  - `Vector3 BedPoint`;
  - `void Pulse()`: `reaction.Play()` when set.

**`DeskController`** (on `OfficeRoot/DeskSurface`), the per-case glue:
- **Fields:**
  - `DeskSurface surface`, `DeskScanner scanner`;
  - `DeskDocument paperTemplate` (inactive);
  - `Transform paperRoot`;
  - `Transform handOverPoint` (where papers slide from and back to: the traveller's side of the desk);
  - `TMP_Text scanHint`;
  - `DeskConfigSO config`.
- **`bool IsReachable`:** surface, scanner, template, root, hand-over point and config are all set.
- **`event Action<int> ScanFinished`.**
- **`void BeginDay(int day)`:** keeps the day and resets the day's scan count.
- **`void BeginCase(IReadOnlyList<CaseDocument> docs)`:** creates `DeskPapers(docs, config.scanSeconds)`, and hands over every `ArrivalIndices` paper.
- **`void HandOver(int i)`** (its one caller, `InvestigationUIController.Choose`, needs no result; a paper that cannot be handed over is `DeskPapers.HandOver`'s tested "no change"): calls `DeskPapers.HandOver(i)`; when that succeeds, it clones the template under `paperRoot`, binds it, places it at the hand-over point, adds it to the `PaperStack`, and slides it to `surface.PointAt(config.paperSpawnSlots[k % n])`, where k is the count of papers handed over so far this case.
- **`void EndCase()`:** calls `ReturnAll()`, slides every paper, inert and out of the raycast, to the hand-over point, destroys it, and clears the stack.
- **`void SetPapersLive(bool live)`:** remembers the flag (also for new papers) and re-applies each paper's liveness.
- **Liveness (R36, R38):** a paper is live when `live && DeskPapers.CanDrag(i) && !paper.IsSliding`, and in the raycast when `live`. Every slide (hand-over, back from the scanner, back from a refused drop, to the bed, and away at `EndCase`) re-applies the paper's liveness when it starts, so a sliding paper is inert, and again in its done callback.
- **Drag begin:** the paper's order becomes `heldPaperOrder`.
- **Drag end:** `DeskPapers.Drop(i, scanner.Contains(point))` decides, and the controller animates the outcome:
  - `Scanning`: the paper slides to the bed and is inert while scanning;
  - `Refused`: it slides back to its pick-up position;
  - `Stays`: it stays where it was dropped.
  - In every case the stack's top is updated and orders are re-applied: `paperBaseOrder + stack.IndexOf(i)`.
- **A click on a paper** brings it to the front.
- **`Update`**, only while `ScannerBusy`: `Tick(Time.deltaTime)`. When a scan finishes, the paper slides back to its pick-up position (live again when it lands), the scanner pulses, the day's scan count goes up, and `ScanFinished(i)` is raised.
- **The hint** is re-evaluated at every hand-over, drop, finish and end: `scanHint.gameObject.SetActive(DeskHints.ScanHintVisible(config.scanHint, day, config.scanHintUntilDay, scansToday, OnDeskCount > 0))`. Its text is set from the config at `Awake`.

**`DeskReaction`** (`[RequireComponent(typeof(Clickable))]`):
- **Fields:** `DeskReactionSO reaction`, `TMP_Text readout` (optional), `OverlayCallout tooltip`, `AudioSource audioSource` (optional).
- **Awake:** captures the rest pose (local position, rotation and scale) and adds `Play` to the `Clickable`'s `onClick`.
- **`void Play()`:** starts the animation. It plays the clip when both the clip and the source exist, and shows the tooltip when the template is non-blank: `tooltip.Show(Interview.Fill(template, Interview.ValueToken, readout != null ? readout.text : ""), transform, reaction.tooltipOffset, reaction.tooltipSeconds)` (R34).
- **`Update`** runs only while animating: it applies `ReactionCurve.Evaluate` relative to the rest pose, then restores the rest pose at the end.

**`DeskSlot`:** serialized `string slotId` and `DeskSlotKind kind` (`Decoration`, `Free`). **`DeskItem`:** serialized `string itemId`. Neither has a public member (R21). Their docs say "Decoration hook (item 7): read by the builder now, by the decoration piece later".

### 2.10 UI

**`InteractionPanelController`** (CRLF):
- `InteractionAction` gains `public bool centre;`: "Shown in the panel's centre slot when it has one (the wheel's '< Back'); otherwise listed like any action".
- New `[SerializeField] private Transform centreSlot;`: "Optional: where centre actions go (the traveller wheel); none keeps every action in the list".
- `SetActions` parents a centre action's button under `centreSlot` when that slot is set.
- `Clear` (62-69) deactivates each spawned button before `Destroy`, which is deferred to the end of the frame, so a layout group never counts a dying button (the ring would otherwise be laid out once for the old and the new choices together).
- The class doc becomes "A list of the interview's current choices: the traveller wheel's ring (laid out by RadialLayoutGroup), or a plain vertical list (the hybrid scene's intercom)".

**`RadialLayoutGroup`** (new, `LayoutGroup`):
- serialized `Vector2 radii`, `Vector2 itemSize`, with public setters;
- places its active, non-ignored children at `RadialLayout.Point` with `itemSize` (a child with `LayoutElement.ignoreLayout`, the centre slot, is left alone);
- `CalculateLayoutInput*` report no preferred size.

**`TravellerWheel`** (new, `IPointerClickHandler`, on the always-active host `OfficeOverlayCanvas/TravellerWheel`, R33):
- **The host** is a full-screen `RectTransform` with no graphic; it is never deactivated. Its child **`Catcher`** is a full-screen transparent raycast-target `Image` (the click-to-close catcher, R6), active only while the wheel is open; `Ring` and the centre slot sit under it. A click on the catcher reaches the host's `OnPointerClick` (`GetEventHandler` walks up the parents); ring buttons handle their own clicks. The catcher has no `Selectable`, so the hover shows the arrow.
- **Fields:** `GameObject catcher`, `RectTransform ring` (the ring's root; its `InteractionPanelController` is driven through `InvestigationUIController.interactionPanel`, R4, so the wheel holds no reference to it), `RadialLayoutGroup layout`, `RectTransform centreSlot`, `TravellerView traveller` (its `Anchor` places the ring and the reply: one source for that point, piece 4 refits it once), `OverlayCallout bubble`, `DeskConfigSO config`.
- **Members:**
  - `bool IsOpen` = `catcher.activeSelf`;
  - `event Action OpenChanged`;
  - `void Open()`: does nothing unless allowed and closed; also the persistent call of the traveller hit zone and the intercom;
  - `void Close()`: closes the ring (the reply bubble stays until its timer ends);
  - `void SetCanOpen(bool can)`: false closes an open wheel and hides the reply bubble (the view left the booth or the traveller left);
  - `void Say(string text)`: shows the traveller's reply beside them, `bubble.Show(text, traveller.Anchor, config.bubbleOffset, config.bubbleSeconds)` (R34); the caller has already applied `DisplayText`;
  - `OnPointerClick`: `Close()`.
- **Awake** (the host is active at load, so it runs then): applies the config's radii and item size to the layout and its centre size to the centre slot, sizes the ring's rect to `RadialLayout.Extent`, caches `Camera.main` and its overlay canvas's rect (`OverlayProjection.CanvasRectOf`), and deactivates the catcher (never its own GameObject). The ring's `InteractionPanelController` wakes at the first open; until then `SetActions` is a plain call that instantiates under the inactive ring, which is safe (its `Awake` only hides the template the builder saves inactive).
- **`LateUpdate`**, only while open: `OverlayProjection.TryPlace(ring, canvasRect, camera, traveller.Anchor.position, Vector2.zero)`, and Escape → `Close()`.
- **Null safety:** with no traveller or anchor the ring stays centred on the screen; with no bubble `Say` does nothing.

**`OverlayCallout`** (new, R34), a timed label on the overlay canvas that takes no clicks; the builder makes two instances, `OfficeOverlayCanvas/SpeechBubble` (the traveller's reply) and `OfficeOverlayCanvas/DeskTooltip` (reaction tooltips):
- **The host** is always active (R33); its child **`Panel`** (`Image` + TMP, raycast targets off, anchors and pivot (0.5, 0.5)) is shown and hidden.
- **Fields:** `RectTransform panel`, `TMP_Text label`.
- **Members:**
  - `void Show(string text, Transform follow, Vector2 offset, float seconds)`: sets the text as given (callers apply `DisplayText`), restarts the timer and shows the panel; a blank text or a null `follow` hides it;
  - `void Hide()`.
- **Awake:** caches `Camera.main` and its overlay canvas's rect (`OverlayProjection.CanvasRectOf`), and hides the panel (never its own GameObject).
- **`LateUpdate`**, only while shown: the timer, and `OverlayProjection.TryPlace(panel, canvasRect, camera, follow.position, offset)`; the callout hides when the timer ends, when `follow` is destroyed, or when the point projects outside the viewport (the bubble while focused).

**`OverlayProjection`** (new, static): `RectTransform CanvasRectOf(Component host)` (the rect of the root canvas above the host, or null) and `bool TryPlace(RectTransform target, RectTransform canvasRect, Camera camera, Vector3 world, Vector2 offset)`:
- uses the given canvas rect and camera, which each caller resolves once at `Awake` (`CanvasRectOf`, `Camera.main`), so the per-frame call looks nothing up (MERGE_CRITERIA: no `GetComponent` in hot paths); false when either is missing;
- world → screen, false when behind the camera or outside the viewport;
- `ScreenPointToLocalPointInRectangle(canvasRect, screen, null)`, then `+ offset`, assigned to the target's `anchoredPosition`: the target needs anchors (0.5, 0.5) under a full-screen parent (the builder sets both);
- clamped inside the canvas with `RectClamp` (not `keepMax`).
- One helper, two callers (`TravellerWheel`, `OverlayCallout`).

**`DraggableWindow`** (CRLF, 92 lines):
- **Begin and drag** use `RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)windowRoot.parent, eventData.position, eventData.pressEventCamera, out local)`. The delta is taken in the parent's space, so it is right in overlay and world space alike.
- **Clamp:** after moving, the window's corners in parent space are clamped per axis inside the parent's rect with `RectClamp.Shift` (y `keepMax`, x not).
- **`_canvas`** (declared at 17, assigned at 31) **and the `scaleFactor` division** (59-60) go.

**`HoverHighlighter`** (LF): the dead-key sweep in `HandleSceneUnloaded` (291-304) moves into `PurgeDeadOutlines()`, called there and in `EnsureWorldOutline` before a new entry is added (225-229) (R9).

**`TranscriptWindowController`** (LF):
- the class doc (6-13) drops "No coroutines: the desktop canvas is switched off outside monitor focus";
- the sentence text goes through `DisplayText.For(line.Text, Spoken)`;
- the compare label (56) becomes `$"Traveller · {ClueLabels.Report(line.Category)}"`;
- the value stays `line.Value`.

**`DocumentWindowController`** (CRLF): the displayed value (96) goes through `DisplayText.For(f.value, Written)`; the compare call (103-107) keeps `f.value`.

**`DesktopShell`** (CRLF):
- `powerButton` (21) becomes `[FormerlySerializedAs("powerButton")] quitButton` ("Start-menu 'Quit game' entry; quits"): it quits, so every scene's saved Power entry (the hybrid scene's included) keeps quitting. Renamed rather than kept, because `BoothCoordinator.powerButton` names the bezel's screen power button;
- new `screenOffButton` ("Start-menu 'Turn off screen' entry; optional, with the monitor screen") and `MonitorScreen monitorScreen`, both without `FormerlySerializedAs`;
- `Start` wires Settings, quit (`Quit`) and, when set, screen-off (`TurnOffScreen`);
- `TurnOffScreen()` calls `monitorScreen.TurnOff()` when a screen is wired and closes the menu. The builder wires the entry and the screen together, so no warning path is needed: a scene without a screen (the hybrid scene) has no screen-off entry;
- the class doc (4-8) is updated.

**`CompareController`** (CRLF): the `Select` docs (58-63) say "value: the canonical value (it drives MATCH/MISMATCH; never display text)".

**`DayFlowUIController`** (CRLF): line 147's hint becomes "(log a deviation before denying)".

### 2.11 `InvestigationUIController` (CRLF)

- **New fields:**
  - `[Header("Desk")] [SerializeField] private DeskController desk;`: "The physical papers and the scanner (optional: without it documents open on request, straight to their windows)";
  - `[SerializeField] private TravellerWheel wheel;` ("The traveller wheel: closed after a hand-over, and it shows the traveller's replies");
  - `[SerializeField] private GameObject idleScreen;`: "Shown on the desktop between travellers";
  - `[Header("Window layout")]` with `documentWindowOrigin`, `documentWindowStep`, `bookWindowOrigin`, `bookWindowColumnStep` and `bookWindowRowStep`. Their initialisers are today's constants, (−330, 140), (620, 0), (−380, −150), 320 and (40, 40), so a scene that is not rebuilt keeps its layout; the builder writes the 4:3 values into `OfficeScene` (−195, 150), (40, −40), (−180, −150), 300 and (40, 40) (R23).
- **`private bool DeskReachable => RichMode && desk != null && desk.IsReachable;`** Every desk branch below tests `DeskReachable`, never `desk != null`: a partly wired desk takes the no-desk path, so papers always reach the PC (K5).
- **`Awake`:** when `RichMode && !DeskReachable`, warn once: "Desk scanner not wired: documents open on the PC when handed over (no physical papers). Run Tools > TimeDesk > Build Office UI." When `DeskReachable`, it subscribes `desk.ScanFinished += OpenDocumentWindow` (unsubscribed in `OnDestroy`). It shows `idleScreen`.
- **`ShowRich`:**
  - hides `idleScreen`;
  - builds a `CaseDocument` per document, with `name` as today (298), `holder = inst.visitorGivenName` (the registered identity, which every Name field prints: `CaseFactory.cs:393-394`; one source) and `handOver = doc.template.handOver` (OnRequest for a null template);
  - places clones at `origin + i * step`;
  - when `DeskReachable`, `desk.BeginCase(docs)`; otherwise `OpenDocumentWindow(i)` for each of `CaseDocuments.ArrivalIndices(docs)` (the rule `DeskPapers` uses);
  - `StartInterview(inst, docs)`.
- **`RefreshChoices`:** `centre = choice.Kind == DialogChoiceKind.Back`.
- **`Choose`:**
  1. note `before = _runner.Transcript.Count`;
  2. choose, and refresh the transcript;
  3. for `HandOverDocument`: when `DeskReachable` `desk.HandOver(i)`, otherwise `OpenDocumentWindow(i)`; then `wheel.Close()` when a wheel is wired;
  4. any other action opens the transcript chrome, as today;
  5. `reply = InterviewScript.SpokenSince(_runner.Transcript, before)`; when a wheel is wired and the reply is not empty, `wheel.Say(DisplayText.For(reply, TextMedium.Spoken))`: the traveller's reply, the spoken reveal point (R19, R34). A choice that adds no traveller line ("Ask about home >", "< Back") leaves the last reply up (§1.7);
  6. `CompleteDialog` and `RefreshChoices` as today.
- **`OpenDocumentWindow(int i)`** (new, private): activates and raises `_docWindows[i]`; the first time, `AddDesktopIcon(name, window, true)`. It is the scan reveal point (R19).
- **`BuildBookShelf`:** book position `origin + (i % 3) * column + (i / 3) * rowStep`.
- **`Decide`:** `desk.EndCase()` when `DeskReachable`, `Hide()`, `_currentCase = null` (R26), then the callback (which may present the next traveller at once). The ring cannot be open at a decision (deciding needs focus, and focusing needs the wheel closed, §1.9), and the reply bubble clears when the callback's phase change reaches `wheel.SetCanOpen(false)` (§2.8), so `Decide` does not touch the wheel.
- **`Hide`:** shows `idleScreen`.
- **Text changes:**
  - the class doc (8-21) mentions the traveller wheel and the desk;
  - the `interactionPanel` doc (47) reads "The traveller wheel's ring: shows the current interview node's choices";
  - the `InterviewReachable` doc (93-99): "the wheel's ring";
  - the warning (129): "Traveller wheel or interview transcript not wired: ...";
  - the `ShowRich` comment (283-284, "the windows spawn hidden and open on request"): arrival documents are handed over at presentation, on-request ones through the wheel, onto the desk or straight to their windows;
  - the `StartInterview` doc (313-319, "the hub with a request per document"): "a request per document handed over on request";
  - the error (325): "..., so the traveller wheel is empty.";
  - `RefreshChoices`' doc (354);
  - the `Choose` doc (370-374, "a document request opens and raises that document's window"): a request hands the document over (onto the desk, or to its window) and closes the wheel; the traveller's reply goes to the wheel's bubble.
- **Null safety:** every new reference (`desk`, `wheel`, `idleScreen`) is optional and tested with `!= null` (for the desk, `DeskReachable`) before use (the hybrid scene has none of them).
- **Unchanged:** `InterviewReachable`'s expression (V7), `EvidenceSystemActive`, `RichMode`, the fallback.

### 2.12 `GameManager` (CRLF)

- **New fields:** `[SerializeField] private TravellerView travellerView;` ("Optional: the booth figure, from presentation until the decision", the name piece 4's draft uses) and `[SerializeField] private BoothCoordinator booth;` ("Optional: the booth's input and wake rules").
- **`SetTravellerAtDesk(bool at)`** (new, private), the one presence transition (R10): it sets `_travellerAtDesk`, calls `travellerView.Show()` or `Clear()`, and `booth.SetPhase(at ? TravellerAtDesk : NoTraveller)`. It replaces the assignments at 454 (`ShowActiveCase`), 481 (`HandlePlayerChoseEra`) and 556 (`HandleDecision`). `HandleDayCompleted` (231) keeps its plain `false` (no traveller can be there, R27).
- **`Start`:**
  - `booth.BeginDay(_worldState.day)`;
  - before `ShowBriefing` (182), `booth.SetPhase(Newsletter)`;
  - the `BeginShift` callback path sets `NoTraveller` first.
- **`HandleDayCompleted`:** `booth.SetPhase(Newsletter)` before `ShowResults` (291).
- **`ShowVerdictThen`:** when `verdict.citationIssued`, `booth.SetCitationPending(true)` before `officeUI.ShowVerdict`, and `booth.SetCitationPending(false)` in the continuation, before `onContinue` (R31). A slip that is not wired continues at once, so the flag is set and cleared in the same call.
- Every new serialized reference is tested with `!= null`.

### 2.13 Content

- **`DocumentTemplateSO`** (CRLF) gains `[Header("Desk")] public DocumentHandOver handOver = DocumentHandOver.OnRequest;`: "When the traveller hands this document over: when they step up (OnArrival) or when asked (OnRequest)".
- **`DocTemplate_Passport.asset`** (hand-authored, not generated) gains `handOver: 1`. `DocTemplate_Permit.asset` is unchanged (it reads OnRequest).
- **`world_source.json`, the generated assets and the day plans are unchanged.**

### 2.14 Generator and validator

**`ContentLibraryValidator`** (CRLF):
- **New** `public static int MaxRequestedDocuments(IEnumerable<CaseBlueprintSO> blueprints)`: the most templates with `DocumentHandOvers.IsRequested(handOver)` in one blueprint. Callers: the menu check (201) and the generator.
- **`MaxDocuments`** stays, with its doc updated: "the most papers one traveller carries". Caller: the builder's desk-slot check.
- **`TravellerBlueprints`** (220) becomes `public`, so the builder counts the same blueprints.
- The `CheckInterview` doc (117) says "the traveller wheel".

**`WorldContentGenerator`** (LF): the menu check (442) calls `MaxRequestedDocuments`; the comment (439) says "the traveller wheel".

### 2.15 Builder (`OfficeSceneUIBuilder.cs` + `OfficeSceneUIBuilder.Desk.cs`, LF)

The class becomes `public static partial class`. New booth and desk code goes in the `.Desk.cs` partial (R22). Each numbered item re-applies its layout on every run (the builder is authoritative).

**`Build()` order.** Every step must find what it wires on a fresh scene as well as on a saved one, or the second build would differ from the first. Today `BuildBooth` (310) runs before `BuildShiftClockReadouts` (323), which creates `WallClock` (1221). `Build()` therefore keeps its order, with two insertions and one new last step:
- **a.** Right after the newsletters (122-131): `EnsureDeskConfig()` (item 11), then the traveller wheel (item 7) and the two callouts (item 8), so every later persistent call and reference finds them.
- **b.** `BuildBooth` (310) builds geometry and cameras only: the booth sprites, cameras, rig and view (item 3), the monitor screen (item 2), the READY clear (item 4), the desk surface, paper template and scanner geometry, the traveller's anchor, the slots (item 10), the readouts and the poster. It wires the rig and the view; their new references (`brain`, `monitorScreen`, `config`) exist by then.
- **c.** `BuildDesktopShell` (item 5), `BuildInteractionFeedback` and `BuildShiftClockReadouts` (which makes `WallClock`) run as today.
- **d.** New `BuildDeskInteraction` (`.Desk.cs`), after `BuildShiftClockReadouts`: the prop `Clickable`s and `DeskReaction`s (WallClock included), the calendar and traveller hit zones, the persistent `TravellerWheel.Open` calls, the scan and wheel notes, the `BoothCoordinator` and its wiring (item 10), and the checks (item 11).
- **e.** Only then the wiring block (`soOffice` … `soGm`, 327-390) with the item-12 references.

1. **Desktop canvas** (`EnsureCanvas`, 555-578):
   - it finds the `Canvas` named "Canvas" (not the first canvas that is not the overlay);
   - it sets `RenderMode.WorldSpace`, `sizeDelta` (1440, 1080) (`DesktopSize`, a new constant) and pivot (0.5, 0.5);
   - it destroys the `CanvasScaler`, adds a `RectMask2D`, and keeps the `GraphicRaycaster`.
   - `ConfigureScaler` (587-592) and `ReferenceResolution` (43-49) now serve only the overlay canvas; their docs say so.
2. **Monitor screen** (`BuildMonitorScreen`, called from `BuildBooth`):
   - `CRTMonitor/ScreenAnchor` sits at `CrtGlassCentre` = (−0.144, 0.054), updated from (−0.15, 0.05) (R13). The desktop canvas is parented under it, with local position 0, scale `CrtGlassSize.y / 1080` (`CrtGlassSize` = (0.676, 0.507)), `worldCamera` = Main Camera, sorting layer Default, order `screenCanvasOrder`.
   - `CRTMonitor/PowerButton` (`EnsureOfficeShape("crt_power", 28, 28, ...)`, a round placeholder) sits at (0.285, −0.19), 0.06 wide, at order `bezelOrder`, with `EnsureClickable` and a persistent `MonitorScreen.TogglePower`.
   - `CRTMonitor/PowerLed` (`EnsureOfficeShape("crt_led", 8, 8, ...)`) sits at (0.33, −0.19), 0.02 wide, at order `bezelOrder`.
   - `CRTMonitor/FocusExitZone` is `EnsureHitZone` (a new helper: hidden `SpriteRenderer` with no sprite, `BoxCollider2D` of an explicit size, `Clickable`), 8 × 6 local units, at order `focusExitOrder`, with a persistent `OfficeViewController.FocusOffice`, inactive.
   - `CRTMonitor/ScreenAnchor/GlassZone` is `EnsureHitZone` sized to the glass (`CrtGlassSize` in the CRT's units, placed in the anchor's space), at order `glassOrder`, with its `Clickable.Interactable` false and no persistent call, inactive (R37).
   - `MonitorScreen` gets `glassSize` = `CrtGlassSize`, `desktopCanvas`, `desktopRaycaster` (the canvas's `GraphicRaycaster`), `glass` (the anchor), `powerLed` and `config`.
3. **Framing:** `MonitorOrthoSize` (997) goes. The `MonitorVCam` starts at the glass centre with `MonitorFraming.OrthoSize` at the reference aspect 16:9 and the config's fill; the rig reframes at runtime. The rig's `brain` is set to Main Camera's `CinemachineBrain` (R35).
4. **READY:** `WireClickToFocusMonitor(signClick, view)` (1128) is replaced by `ClearPersistentCalls(signClick, "onClick")`, a new helper that empties `m_PersistentCalls.m_Calls` (`WirePersistentVoid` empties them through it before adding its call, so the clearing has one home). The scene's saved call (`OfficeScene.unity:28181`) would otherwise survive.
5. **Taskbar:**
   - `BuildBackToOfficeButton` (1451-1457) goes; `DestroyChildIfPresent(root, "BackToOfficeButton")`;
   - `Taskbar/DeskButton` "< Desk" (anchors (0.125, 0.1)–(0.245, 0.9), with `SetAnchors` re-applied) has a persistent `FocusOffice`; it is built in `BuildDesktopShell` (step c), which runs after the view exists;
   - the Start menu (1505-1512) grows to three 46-px entries, `SettingsEntry`, `ScreenOffEntry` "Turn off screen" and `QuitEntry` "Quit game" (anchors (0, 0)–(0.2, 0), position (0, 119), size (0, 158)); the old `PowerEntry` is destroyed with the menu, which is rebuilt each run;
   - `DesktopShell` is wired with `quitButton` (= `QuitEntry`), `screenOffButton` (= `ScreenOffEntry`) and `monitorScreen`, together.
6. **Retired intercom:** `DestroyChildIfPresent(investRoot, "IntercomPanel")`. The intercom block (183-208) and `intercomFit` (199-200, 297-298) go.
7. **Traveller wheel** under `OfficeOverlayCanvas`, all rebuilt each run (R33):
   - `TravellerWheel`: the host, full-screen, no graphic, **active**, with the `TravellerWheel` component;
   - `TravellerWheel/Catcher`: full-screen, `Image` alpha 0, raycast target, **inactive**;
   - `Catcher/Ring`: anchors and pivot (0.5, 0.5) (`OverlayProjection` places it by `anchoredPosition`), with `InteractionPanelController` (`actionsRoot` = `Ring`, `actionButtonTemplate`, `centreSlot` = `Centre`), `RadialLayoutGroup` and `ActionButtonTemplate` (`MakeButton`, the `wheelItemSize`, TMP auto-size 12–20, inactive);
   - `Ring/Centre` (the centre slot): anchors and pivot (0.5, 0.5) at the ring's centre, `wheelCentreSize`, and a `LayoutElement` with `ignoreLayout` so the ring never places it;
   - the wheel's fields: `catcher`, `ring`, `layout`, `centreSlot`, `traveller`, `bubble` (the `SpeechBubble` callout) and `config`; `traveller` is set in step d.
   - After the library lookup (288), when `RadialLayout.MaxFit(...) < library.Interview.menuCapacity`, the builder logs "[TimeDesk] The traveller wheel fits {fit} choices, but the content library's interview menu capacity is {n}; lower interview.menuCapacity in world_source.json or enlarge the wheel (Desk_Default: wheelRadii, wheelItemSize)."
8. **Two `OverlayCallout`s** under `OfficeOverlayCanvas`, rebuilt each run (R33, R34): hosts `SpeechBubble` and `DeskTooltip` (full-screen, no graphic, **active**), each with a `Panel` child (anchors and pivot (0.5, 0.5); 420 × 110 and 360 × 60; `Image` and TMP auto-size 14–24, raycast targets off; **inactive**) and the callout's `panel` and `label` set.
9. **Desktop:**
   - the wallpaper `Desktop` gets an `AspectRatioFitter` (EnvelopeParent, the sprite's aspect);
   - `EnsureWallpaper` sets `mipmapEnabled = true` on `xp_bliss.png`'s importer when it differs (K18);
   - `IdleText` (TMP 80 pt bold, "Waiting for the next traveller", centred, inactive; its raycast target is off) goes under the canvas after `Desktop`;
   - the leftovers of R18 are destroyed from the `OfficeUIController`'s object;
   - the Deviation Report title (178) becomes "Deviation Report" and its icon label (181) "Deviation Report". The window is rebuilt each run (177), so its title takes; the icon is not, and `MakeButton` returns an existing button with its label untouched (716-724). So `BuildDesktopIcon` (1600-1603) sets the `Label` TMP's text to `label` on every run, next to `FitIconLabel` (whose doc, 1633-1638, says `MakeButton` keeps labels; it gains "the label text is set by `BuildDesktopIcon`"). `IconScanner` is not destroyed: a rebuilt icon would move to the end of the grid;
   - the Material title (1485) becomes "Material Analysis";
   - `DestroyChildIfPresent(windowLayer, "IconMaterialWindow")` runs before the app loop, like `IconDialectWindow` (1490), because `BuildOSWindow` keeps existing texts.
   - The transcript window (251) moves to (395, 60), which clears the compare bar (y ≤ −173).
10. **Booth.** Geometry in `BuildBooth` (1054-1136, step b); interaction in `BuildDeskInteraction` (step d):
    - **Desk surface (b):** `OfficeRoot/DeskSurface` at (−2.25, −4.15, 0), size (10.3, 3.5), which keeps paper centres left of the CRT (x ≤ 2.9) and lets them reach the scanner. On it: `DeskController`, `Papers` (the paper root), `HandOver` at **world** (0, −2.3, 0), which is local (2.25, 1.85, 0) under `DeskSurface` (just past the desk's far edge at y −2.4, below the traveller, so papers slide in from the traveller's side), and the inactive `PaperTemplate`.
    - **Paper template (b):** `EnsureOfficeSprite("paper", cream, 150, 200)`, 1.5 wide; a `SortingGroup`, `BoxCollider2D` fitted to the art, `Clickable`, `DeskDraggable` (`proxy`), `DeskDocument` (`title`, `holder`, `photoSlot`, `click`, `drag`, `group`); `Title` and `Holder` via `WorldText` inside the group (orders 2 and 3); an inactive `PhotoSlot` box.
    - **Scanner (b, d):** `ScannerTray` gets `EnsureClickable`, `DeskScanner` (drop size = the tray sprite's own size; bed centre (0.1, 0.3) local; `reaction`) and `DeskReaction` (`Reaction_Scanner`). `ScanHint` is a `WorldText` on the tray, inactive (the controller sets its text).
    - **Props (d):** `EnsureClickable` + `DeskReaction` (each with `reaction`, `tooltip` = the `DeskTooltip` callout) on DeskStamp, DeskMug, DeskPlant, ReactivePoster, DeskIntercom (plus a persistent `TravellerWheel.Open`), CreditsTill (readout `CreditsNumber`, and its existing `AudioSource`), StabilityMonitor (readout `StabilityPercent`) and WallClock (readout: the tray clock). A calendar hit zone (`EnsureHitZone` on `LeftPartition` over the `DayNumber` area, order −49) gets `DeskReaction` with readout `DayNumber`. The comment "Desk props (decoration only)" (1071) goes.
    - **Traveller (b, d):** `Traveller` keeps its placeholder sprite (`TravellerView.figure` = that renderer), with `Anchor` (world ≈ (0, 1.0, 2); `TravellerView.anchor`) and `TravellerHitZone` (`EnsureHitZone` over the part above the desk's far edge, order −19, persistent `TravellerWheel.Open`). The wheel's `traveller` is set to the `TravellerView`.
    - **Wheel note (d, R32):** `OfficeRoot/WheelHint`, a `WorldText` above the traveller's head at (0, 2.75), box 4.4 × 0.5, order −9 (over the back wall and the desk art, under every prop), inactive (the coordinator sets its text and shows it).
    - **Decoration hooks (b):** `OfficeRoot/DeskSlots/{plant, mug, photo, free_1, free_2}` with `DeskSlot`. The plant and mug are placed at their slots, the photo slot is at (2.4, −1.6), and the free slots are at (−6.8, −5.6) and (2.5, −5.6). `DeskItem` ids go on the stamp, mug, plant and poster.
    - **Coordinator (d):** `BoothCoordinator` on `OfficeRoot`, wired to everything above (item 12).
    - `BoothRaycastHits` (1201) becomes 16 (R24).
11. **Config:** `EnsureDeskConfig()` and `EnsureDeskReaction(name, kind, tooltip)` create missing assets (`Assets/Data/Config/Desk_Default.asset`, `Assets/Data/Config/DeskReactions/`). Checks, logged as errors:
    - `SortingBands.Problems(6, focusExitOrder, glassOrder, bezelOrder, ...)` (the highest prop order is 6, `CreditsNumber`);
    - `paperSpawnSlots.Length < ContentLibraryValidator.MaxDocuments(TravellerBlueprints(library))`: "[TimeDesk] The desk has {n} paper spawn slots but a traveller can carry {m} papers; add slots in Desk_Default."
12. **Wiring** (every reference below is set on every run; §6 step 4 checks each is non-null):
    - `soInvest` (355-373): `desk`, `wheel`, `idleScreen`, the window layout, and `interactionPanel` = the ring's `InteractionPanelController`;
    - `soGm` (380-390): `travellerView`, `booth`;
    - `soView` (in `BuildBooth`): `monitorScreen`;
    - `soRig` (in `BuildBooth`): `brain`, `monitorScreen`, `config`;
    - `MonitorScreen`: `desktopCanvas`, `desktopRaycaster`, `glass`, `glassSize`, `powerLed`, `config` (item 2);
    - `TravellerWheel`: `catcher`, `ring`, `layout`, `centreSlot`, `traveller`, `bubble`, `config`; the ring's `InteractionPanelController`: `actionsRoot`, `actionButtonTemplate`, `centreSlot` (item 7);
    - each `OverlayCallout`: `panel`, `label` (item 8);
    - `TravellerView`: `figure`, `anchor`;
    - `DeskController`: `surface`, `scanner`, `paperTemplate`, `paperRoot`, `handOverPoint`, `scanHint`, `config`; `DeskScanner.reaction`; `DeskDocument` and `DeskDraggable` on the template (item 10);
    - each `DeskReaction`: `reaction`, `tooltip`, and `readout` and `audioSource` where item 10 names them;
    - `DesktopShell`: `quitButton`, `screenOffButton`, `monitorScreen` (item 5);
    - `BoothCoordinator`: `view`, `screen`, `desk`, `wheel`, `crt`, `powerButton`, `focusExit`, `glassZone`, `travellerHitZone`, `props` (the ten prop `Clickable`s: stamp, mug, plant, poster, intercom, scanner tray, till, stability monitor, wall clock, calendar zone), `wheelHint`, `config`.
13. **Comments and logs:**
    - the stale comments at 119-121, 308-309 and 774-777 (the desktop "hidden outside MonitorFocus"), 183-186 and 1125-1126 are rewritten;
    - the `BuildBooth` doc (1047-1053, "wires OfficeViewController + the CRT/READY clickables"): READY is no longer wired to the view (it is cleared), and the booth now builds the monitor screen, the desk and the traveller's anchor;
    - the `BuildDesktopShell` doc (1459-1463, "a Start menu (Settings + Power)"): "Settings, Turn off screen and Quit game";
    - the class summary (10-24) lists the live monitor, the desk and the wheel;
    - the final log (393) names them.
14. **After the change:** Build Office UI runs in the worktree's Unity, and `OfficeScene.unity` is committed with the builder change. The builder runs twice and the semantic dumps must match (§6).

### 2.16 Sorting bands and raycasts

| Band | Order (Default layer) | Source |
|---|---|---|
| Back wall, partitions, wall props, traveller, desk | −100 … −10 | builder, unchanged |
| Traveller hit zone, calendar zone | −19, −49 | builder |
| CRT, intercom, scanner, READY, plant, mug, stamp, till | 0 … 6 | builder, unchanged |
| Wheel note (world text, no collider) | −9 | builder |
| Focus exit zone | 10 | `focusExitOrder` |
| Glass zone (R37) | 11 | `glassOrder` |
| Power button, LED | 12 | `bezelOrder` |
| Desktop canvas (World Space) | 20 | `screenCanvasOrder` |
| Papers (a `SortingGroup` each; outline and texts inside) | 30 + stack index | `paperBaseOrder` |
| Held paper | 60 | `heldPaperOrder` |
| Overlay canvas (wheel, bubble, tooltip, newsletters) | above every world object | `OfficeOverlayCanvas` order 10, Screen Space Overlay |

A world-space canvas hit carries the canvas's layer and order (ugui `GraphicRaycaster.cs:279-280`). A 2D hit carries its renderer's, or its group's (`Physics2DRaycaster.cs:95-113`). One camera ranks them by layer, then order (`EventSystem.cs:227-238`). That is why the screen beats the glass zone, which beats the exit zone, which beats the props. A collider with no renderer reports order 0 (`Physics2DRaycaster.cs:69-104`), so every hit zone carries a hidden `SpriteRenderer` for its order.

### 2.17 Copy

| Text | Where |
|---|---|
| "Turn off screen", "Quit game", "< Desk" | builder |
| "Waiting for the next traveller" | builder (`IdleText`) |
| "Deviation Report" (window and icon), "Material Analysis" | builder |
| "Drag papers onto the scanner to read them on the PC." | `DeskConfigSO.scanHint` |
| "Click the traveller to talk and ask for papers." | `DeskConfigSO.wheelHint` (R32) |
| Tooltips "Credits: {value}", "Day {value}", "Timeline stability: {value}", "{value}" | `DeskReactionSO` assets |
| "Traveller · {category}" | `TranscriptWindowController` |
| "(log a deviation before denying)" | `DayFlowUIController` |
| Desk and wheel wiring warnings, the rig's missing-brain warning, the builder's wheel-fit, slot and band errors | §2.8–2.15 |
| "the traveller wheel shows at most {n}" | `DialogChecks` |

All new text is ASCII except "·" and "▸", which the transcript and desktop already render. The TMP fallback atlas is still checked and reverted (G6).

### 2.18 Every file that changes

| File | Assembly | Change |
|---|---|---|
| `Assets/Scripts/Domain/BoothRules.cs` (+meta) | Domain | new §2.2 |
| `Assets/Scripts/Domain/PcScreen.cs` (+meta) | Domain | new §2.3 |
| `Assets/Scripts/Domain/DeskPapers.cs` (+meta) | Domain | new §2.4 |
| `Assets/Scripts/Domain/Dialog.cs` | Domain | `HandOverDocument`, `DialogChoiceKind`, `Kind`, docs |
| `Assets/Scripts/Domain/InterviewScript.cs` | Domain | `documents`, one-shot requests, Back kind, `SpokenSince`, wording |
| `Assets/Scripts/Domain/InterviewContent.cs` | Domain | docs |
| `Assets/Scripts/Domain/DiscrepancyLog.cs` | Domain | `CompareEvidence.value` doc |
| `Assets/Scripts/Visuals/{DeskGeometry,RadialLayout,MonitorFraming,ReactionCurve,DisplayText}.cs` (+metas) | Visuals | new §2.6 |
| `Assets/Scripts/Office/{DeskConfigSO,MonitorScreen,BoothCoordinator}.cs` (+metas) | Assembly-CSharp | new |
| `Assets/Scripts/Office/Desk/` (+ folder meta) with `{DeskSurface,DeskDraggable,DeskDocument,DeskScanner,DeskController,DeskReaction,DeskReactionSO,DeskSlot,DeskItem}.cs` (+metas) | Assembly-CSharp | new §2.9 |
| `Assets/Scripts/Characters/` (+ folder meta), `TravellerView.cs` (+meta) | Assembly-CSharp | new |
| `Assets/Scripts/UI/{TravellerWheel,RadialLayoutGroup,OverlayCallout,OverlayProjection}.cs` (+metas) | Assembly-CSharp | new §2.10 |
| `Assets/Scripts/Office/{OfficeViewController,ICameraRig,CinemachineCameraRig}.cs` | Assembly-CSharp | §2.8 |
| `Assets/Scripts/UI/{InvestigationUIController,InteractionPanelController,DraggableWindow,HoverHighlighter,TranscriptWindowController,DocumentWindowController,DesktopShell,CompareController,DayFlowUIController}.cs` | Assembly-CSharp | §2.10–2.11 |
| `Assets/Scripts/GameManager.cs` | Assembly-CSharp | §2.12 |
| `Assets/Scripts/DocumentTemplateSO.cs` | Assembly-CSharp | `handOver` |
| `Assets/Editor/OfficeSceneUIBuilder.cs`; new `Assets/Editor/OfficeSceneUIBuilder.Desk.cs` (+meta) | Editor | §2.15 |
| `Assets/Editor/ContentLibraryValidator.cs`, `Assets/Editor/WorldContentGenerator.cs` | Editor | §2.14 |
| `Assets/Data/Investigation/DocTemplate_Passport.asset` | content | `handOver: 1` |
| `Assets/Data/Config/Desk_Default.asset`, `Assets/Data/Config/DeskReactions/*.asset` (+metas, folder meta) | content | new, builder-created |
| `Assets/Art/Office/Placeholder/{paper,crt_power,crt_led}.png` (+metas) | art (LFS) | new, builder-generated placeholders |
| `Assets/Art/Generated/xp_bliss.png.meta` | art | mipmaps on |
| `Assets/Scenes/OfficeScene.unity` | scene | rebuilt by the builder |
| `Assets/Tests/EditMode/{BoothRules,PcScreen,DeskPapers,DeskGeometry,RadialLayout,MonitorFraming,ReactionCurve,DisplayText}Tests.cs` (+metas) | tests | new, §5 |
| `Assets/Tests/EditMode/{InterviewScript,InterviewDay}Tests.cs` | tests | §5 |
| `docs/FEATURES.md` | docs | §3.4 |
| `docs/superpowers/specs/2026-09-25-physical-desk-design.md`, `docs/superpowers/plans/2026-09-25-physical-desk.md` | docs | this spec, the plan |
| `docs/superpowers/drafts/specs/2026-09-24-characters-design.md`, `.../2026-09-24-ui-reacts-design.md` | docs | amendment blocks, §3.2 |
| `docs/superpowers/plans/2026-09-25-code-audit-overhaul-plan.md` | docs | piece-8 line, §3.3 |
| `ArtDeliverables/TimeDesk/OFFICE_RESET_BRIEF.md`, `ArtDeliverables/TimeDesk/HybridScene/SCENE_CONTRACT.md`, `docs/ART_ASSET_LIST.md` | docs | appended amendments, §3.3 |

Checked and unchanged: `Clickable` (the collider-off drag makes a guard unnecessary, R7), `OfficeUIController` (legacy fields kept for `Test_DayLoop`), `OfficeReadouts`, `ShiftClockReadouts`, `ShiftClockDriver`, `DesktopIcon`, `OSWindowChrome`, `ReferenceBookWindowController`, `CitizenRecordsWindowController`, `InteractionFeedbackSO` (no grab cursor, §4), `DialogRunner`, `InterviewDay`, `Seeds`, `SaveSystem`, `WorldState`, `world_source.json`, every generated asset, `Assets/Scenes/OfficeScene_HybridArt.unity`, `Assets/Scenes/Test_DayLoop.unity`, `ProjectSettings/TagManager.asset`, the draft spec `2026-09-24-history-facts-design.md` (it has no office, intercom or desktop dependency: grep).

### 2.19 Why some logic stays outside Domain, and per-frame code

Every rule is Domain or Visuals and tested:
- which input is live (`BoothRules`), power, wake and the citation hold (`PcScreen`), paper states, drop outcomes, scanning and timing (`DeskPapers`), and the two day-1 notes (`DeskHints`);
- clamping, containment, stacking and bands (`DeskGeometry`), the wheel's layout and fit (`RadialLayout`), the framing (`MonitorFraming`), the reaction poses (`ReactionCurve`), and the display seam (`DisplayText`);
- which choices are requests and one-shot, Back's kind, and which lines are the traveller's reply (`InterviewScript`).

The holder name is not a rule: it is one field read, `inst.visitorGivenName`.

What stays in Assembly-CSharp is glue: it raycasts, reads transforms and cameras, instantiates papers, drives uGUI and Cinemachine, and wires scenes. The EditMode test assembly cannot reach it, and there is no PlayMode assembly (`docs/TESTING_STRATEGY.md` Tier 2 is "NEXT", blocked on the asmdef migration). §6 proves its behaviour in Unity.

New per-frame code (TESTING_STRATEGY Tier 4 asks for a justification). None of it allocates: no LINQ, closures or string building per frame.
- `OfficeViewController.Update`: a settle poll that runs only while unsettled, then Escape (existing).
- `DeskController.Update`: runs only while a scan runs (one `Tick`).
- `DeskDocument.Update`: runs only while sliding.
- `DeskReaction.Update`: runs only while animating.
- `TravellerWheel.LateUpdate` and `OverlayCallout.LateUpdate` (two instances): run only while shown; one projection each, and a timer for the callouts.

A world canvas rebuilds only what changes (the tray clock text, as today).

### 2.20 Intent audit: the existing mechanism tried for each new one

| New | Existing mechanism reused or tried | Why something new was still needed |
|---|---|---|
| Live monitor | the desktop canvas itself (re-hosted in World Space), `OSWindowChrome`, every window | only `MonitorScreen` (glass geometry, power) is new |
| Focus gating | `OfficeViewController`, `ICameraRig`, `ViewChanged` (unused until now), `Clickable.Interactable` | the rules table and the settle signal did not exist; the 2026-06-21 spec's "enable on blend complete" (:79-80) was never built |
| Exit zone, hit zones | `Clickable` + hidden `SpriteRenderer` (the hand-cursor hit-zone pattern, `HoverHighlighter.cs:192-196`) | one builder helper, `EnsureHitZone` |
| Paper drag | `DraggableWindow` (uGUI only) | world objects had no drag; the desk-plane projection is new |
| Scanner → window | the hidden per-case window clones, `AddDesktopIcon(isDocument)` (dead until now) | the paper state machine and the scan timer |
| Wheel | `InteractionPanelController`, `InteractionAction`, `DialogRunner`, `InterviewScript` | a radial layout group, the open/close host, the Back kind |
| Speech bubble and tooltips | the transcript (the evidence record, unchanged); the readout TMP texts and `Interview.Fill` for the tooltip values | one overlay label, `OverlayCallout`, with two instances (R34); a component per use would duplicate the mechanism |
| Traveller's reply text | the runner's transcript | `InterviewScript.SpokenSince` picks the reply from it (tested) |
| Drop outcome | `DeskPapers`' scan rule | `Drop` returns the outcome, so the glue only animates (R36) |
| Reactions | `Clickable.onClick`, `HoverHighlighter` | the reaction data and curve; no tween library exists |
| Screen power | nothing existed; `DesktopShell`'s Power entry (quit) is reused for "Quit game" (the field renamed `quitButton`, its data kept) | `PcScreen` |
| Knobs | `GameConfigSO` (scoring tuning), `InteractionFeedbackSO` (look) | desk tuning is a separate concern; `DeskConfigSO` follows `InteractionFeedbackSO`'s builder-created pattern |
| Hand-over timing | piece 3's request choice (`DialogAction`) | one enum value renamed and a template knob |
| Sorting | the builder's order constants | bands for the new dynamic objects |
| Decoration hooks | the builder's named anchors | marker components with ids |

### 2.21 Contracts for later pieces

- **Piece 8 (wheel content):**
  - appends `DialogChoiceKind` values (Request, Question, Submenu, …) for icons and grouping;
  - adds non-document requests as new runtime `DialogAction` values;
  - may pace the bubble and show the claim on arrival;
  - adds "Look >" garments once piece 4 exists.
  - The wheel's capacity stays `interview.menuCapacity`, and the builder checks it against `RadialLayout.MaxFit`.
- **Piece 4 (characters):**
  - `TravellerView` is the booth seam. Its `Show()` gains `(TravellerLook look, CharacterArt art)`, and `figure` becomes its layer renderers.
  - `Anchor` and `TravellerHitZone` stay. The hit zone is refitted to the figure, as a child with its own renderer (K20). `TravellerView.Anchor` is the one reference to the point the wheel and the reply bubble use (`TravellerWheel.traveller`), so a refit moves both.
  - The passport photo fills `DeskDocument.photoSlot` as well as the scanned page's `PhotoBox`.
  - Garments are inspected through the wheel (a "Look >" sub-menu whose observations are compare-clickable transcript rows), replacing the draft's desktop Visitor window (§3.2).
  - Both pieces edit `DocumentTemplateSO` (`handOver` here, `showsPhoto` there); piece 4's plan re-reads it.
  - Piece 4's R15 (`blueprintOverride` removed) edits `ContentLibraryValidator.TravellerBlueprints`, which this piece makes public.
- **Piece 6 (UI reacts):**
  - the desktop is a World Space canvas that is always active;
  - the wheel and the two callouts (the bubble and the tooltip, one `OverlayCallout` component) are new overlay surfaces; theming them is piece 6's call, and piece 7 builds them neutral;
  - the intercom roles and keys go, and this piece's new copy becomes keys (§3.2).
- **Piece 9 (translation):**
  - the only place displayed text may differ from evidence is `DisplayText.For`;
  - the reveal points are `InvestigationUIController.OpenDocumentWindow` after a scan and the reply `InvestigationUIController.Choose` passes to `TravellerWheel.Say`;
  - the paper stays in its source script (`DeskDocument.Bind` does not call `DisplayText`) and the scanned copy is what gets translated.
- **Art sync (last):**
  - `MonitorScreen`, `DeskSurface`, `DeskScanner`, `DeskSlot`, `TravellerView.Anchor` and the hit zones re-host on the 3D props;
  - `Clickable` still requires a `Collider2D`, so 3D papers need proxies or a later collider generalisation (§7).

## 3. Retired or superseded

### 3.1 Code removed or replaced

- `OfficeViewController`: the unconditional `desktopRoot.SetActive` (92-93) becomes the legacy path only; `InitForTest` (59-66) and `Toggle` (74-76) are removed.
- `DraggableWindow`: `_canvas` (17, 31) and the `scaleFactor` division (59-60).
- `DesktopShell`: `powerButton` is renamed `quitButton` (its saved data kept, `FormerlySerializedAs`).
- `DialogAction.OpenDocument` becomes `HandOverDocument`; repeatable requests become one-shot; `InterviewCase.documentNames` becomes `documents`.
- `InvestigationUIController`: the `OpenDocument` branch (384-392) becomes `HandOverDocument` → desk or `OpenDocumentWindow`; the spawn constants (295, 441) become serialized layout.
- Builder:
  - the intercom panel and its fit check (183-208, 297-298);
  - `BuildBackToOfficeButton` (1451-1457);
  - the READY `FocusMonitor` wiring (1128);
  - `MonitorOrthoSize` (997);
  - the Start menu's `PowerEntry` (1510);
  - `ConfigureScaler` on the desktop canvas (575);
  - "Desk props (decoration only)" (1071).
- Scene (`OfficeScene.unity`): `IntercomPanel`, `BackToOfficeButton`, and the five leftovers of R18.
- `HoverHighlighter.HandleSceneUnloaded`'s inline sweep (291-304) moves into `PurgeDeadOutlines`.

### 3.2 Earlier spec lines superseded (kept as approved records, not edited)

**`docs/superpowers/specs/2026-06-21-office-scene-two-states-design.md` (approved design):**
- **:27-28, :46, :65:** the Screen Space Overlay desktop that is disabled by default and full-screen in Monitor Focus. The desktop is now a World Space canvas on the CRT glass, always active, with gated input (K1).
- **:79-83:**
  - "on blend complete, enable DesktopCanvas" is realised as "desktop input on once settled" (R11);
  - the exits are Escape, a click outside the screen and "< Desk" (K8);
  - "office-only interactables ignore clicks" in Monitor Focus is realised by `BoothRules` and the exit zone.
- **:92-93:** Power (quit) becomes Turn off screen + Quit game (K7).
- **:109:** the desktop "Scanner" as the scanned-document window is realised by per-document icons after a desk scan (R2). The app titled "Scanner" is the Deviation Report, now titled so (K17).
- **:113:** "Material Scanner" is now titled "Material Analysis".

**`docs/superpowers/specs/2026-07-03-scanner-evidence-design.md` (approved by Saleh):**
- **:8** ("document it by scanning (comparing)") and **:20-23** ("Scanner window = Deviation Report. The Scanner desktop app …"): the Deviation Report app is titled "Deviation Report", and "scanning" now names the desk device that copies papers to the PC. Documenting evidence is still comparing, and the proof rules are unchanged.

**`docs/superpowers/specs/2026-09-24-dialog-questions-design.md` (piece 3):**
- **Q4 (:26), §1.1 (:87-88):** "Every intercom entry … 'Request <document>' … the window opens; repeatable" now reads: every wheel entry; requests exist only for on-request documents, are one-shot, and hand the paper over (onto the desk, or to its window where no desk is wired) (K3, K6, R2, R3).
- **Q9 (:31) and O1 (:48), §4 (:1001):** "Booth speech bubbles belong to piece 4" is superseded; a minimal bubble ships in piece 7 and polish goes to piece 8.
- **Q11 (:33):** "Requests are repeatable … Documents can only be reopened through the intercom" is replaced by one-shot requests plus document icons (R2).
- **R10 (:63):** the transcript at (220, 70) "on the 1920×1080 reference canvas … clears the intercom (x ≥ +557)" is now at (395, 60) on the 1440×1080 desktop.
- **R12 (:65), §2.11 (:576):** "the intercom, the transcript window and its chrome" now reads "the wheel's ring, …". The expression is unchanged (V7).
- **R21 (:74):** the book formula becomes serialized layout for 4:3 (R23).
- **R25 (:78), §2.12 (:610-611), §2.15 (:799):**
  - the intercom's `Actions` fit of 8 and "both canvas scalers" are replaced by the wheel's `RadialLayout.MaxFit` (8, R5);
  - the desktop has no scaler (R29);
  - the knob keeps its name and value.
- **§1.1 (:92, :94, :95):** "than the intercom shows" now reads "the wheel"; the compare label is "Traveller · CAPITAL" (R25); "Leaving the monitor (Escape) … nothing is animated or timed" still holds, but the canvas is no longer switched off.
- **§2.11 (:568-569):** the "Intercom · …" label, and "Coroutines are never used: the desktop canvas is deactivated outside monitor focus": the premise is gone (the class still uses none).
- **§4 (:1006):** "a paged or scrolling intercom menu" now reads "wheel menu".
- **§3.3 FEATURES rewrites (:974, :977, :993):** rewritten again in §3.4.

**`docs/superpowers/drafts/specs/2026-09-24-characters-design.md` (piece 4 draft, not yet approved):**
- The plan inserts, right after the draft's second paragraph, a block "Amendments from piece 7 (2026-09-25), read first", listing the items below. The draft's own lines are otherwise not edited.
  - **C4 (:27), §1.2 (:100-106), R12 (:67), R14 (:69), C8 (:31, the Visitor window use), C18 (:41, "In the Visitor window a premade is one clickable region"), §2.9 `TravellerPortraitView` as the Visitor window (:427-437), §2.11 Visitor fields and `ShowRich` opening (:469-481), §2.12 Visitor window block (:500-509), §3.3 FEATURES :35/:827 lines (:819, :827), §6 steps (:926-927), §7 UI crowding (:946):**
    - C4's premise ("the desktop canvas and the booth are never visible together") is false after piece 7, and the booth figure is clickable (K20).
    - Garments are inspected through the wheel ("Look >", piece 8 content once piece 4 exists): each visible garment is a choice, and its observation becomes a compare-clickable transcript row carrying `EvidenceKind.Appearance` (the item's label as text, the Culture value as evidence).
    - The Visitor window, its auto-open (R12) and `VisitorReachable` (R14) are dropped; Appearance tells are gated by `InterviewReachable`.
    - `TravellerPortraitView` stays only for the passport photo.
    - Piece 4 re-decides these rows before its plan.
  - **C19 (:42), §2.9 `TravellerView` (:421-425), §2.10 (:441, :457-460):** `TravellerView` exists (piece 7, `Assets/Scripts/Characters/TravellerView.cs`) with `Show()`/`Clear()`, `figure`, `Anchor`; piece 4 extends it (`Show(look, art)`, layers) instead of creating it. `GameManager.travellerView` exists; its `Show`/`Clear` calls sit in `SetTravellerAtDesk`.
  - **R11 (:66), §2.12 `BuildTraveller` (:493-499):** the builder must keep `Traveller/Anchor` and `Traveller/TravellerHitZone` when it replaces the placeholder with layer children, and refit the hit zone.
  - **W12 (:50), §4 (:845):** speech bubbles are delivered (minimal) by piece 7; polish goes to piece 8; walk-in and walk-out stay with the booth rework.
  - **§4 (:849) "clickable booth figure" (not scheduled):** delivered by piece 7.
  - **C20 / R13 (:43, :68):** the photo also fills the physical paper's reserved `photoSlot`.
  - **Line numbers and paths:** the builder anchors it cites are stale again (piece 7 adds a partial file), as are the `DocumentWindowController` photo layout (the window stays) and the menu-capacity wording ("intercom" → "wheel").

**`docs/superpowers/drafts/specs/2026-09-24-ui-reacts-design.md` (piece 6 draft):**
- The plan inserts the same kind of block ("Amendments from piece 7"). The items:
  - **U11 (:27), the surfaces table (:94-97):** the intercom panel row goes. The wheel, bubble and tooltip are new overlay surfaces: themed or neutral is piece 6's decision (built neutral).
  - **Theme roles 13, 17, 18 (:297, :301-302):** the intercom's `ActionButtonTemplate`, `IntercomPanel` and title are gone. The wheel's ring buttons replace role 13, and roles 17 and 18 have no target.
  - **Keys (:492-494, :506, :508, :511, :528, :543, :743, :748, :756, :759).** Every player-visible string this piece adds or changes (§2.17):
    - `intercom.title` and `intercom.actionSample` go (with the flavour row at :748);
    - `window.scanner` and `icon.scanner` read "Deviation Report" (flavour rows :756, :759 re-translated);
    - `window.material` reads "Material Analysis";
    - `compare.intercomLabel` becomes a traveller label, "Traveller · {0}";
    - `desktop.back` ("< Office", :506) goes with the desktop's Back button; the taskbar's "< Desk" is a new key;
    - `startmenu.power` ("Power", :511, flavour row :743) reads "Quit game"; "Turn off screen" is a new key;
    - `results.unproven` (:528) ends "(log a deviation before denying)";
    - new keys: the idle line "Waiting for the next traveller", the scan note "Drag papers onto the scanner to read them on the PC.", the wheel note "Click the traveller to talk and ask for papers.", and the four tooltip templates "Credits: {0}", "Day {0}", "Timeline stability: {0}", "{0}".
    - **Copy held in ScriptableObjects** (`DeskConfigSO.scanHint`, `wheelHint`, the `DeskReactionSO.tooltip` templates) is UI copy, not content and not diegetic evidence (Z4 covers documents, books and records), so it becomes keys under piece 6's R2: piece 6 decides whether the fields then hold a key or move into the table. Piece 6's key walk must include these SO fields, which its builder and code walk would not find.
  - **Apply walk (:423) and completeness check (:599):** the desktop canvas is World Space and always active, under `OfficeRoot/CRTMonitor/ScreenAnchor`; the overlay canvas gains `TravellerWheel` and two `OverlayCallout`s (`SpeechBubble`, `DeskTooltip`); the world gains the scan and wheel notes (world TMP texts).
  - **Verification (:1020):** "READY, focus the monitor": READY no longer focuses; click the CRT.

### 3.3 Plan and art documents amended (in this piece's docs commit)

- **`docs/superpowers/plans/2026-09-25-code-audit-overhaul-plan.md:9`:** "8 (traveller wheel)" → "8 (traveller wheel content: piece 7 ships the basic wheel)". The Phase 3 PlayMode list (:77-78) already names "desk scanner → PC, PC focus/power, traveller wheel".
- **`ArtDeliverables/TimeDesk/OFFICE_RESET_BRIEF.md`:** an appended section "Amendment (piece 7, 2026-09-25)":
  - supersedes :10 ("do not invent physical scanning mechanics") and :24 (the physical intercom and document slot are deferred; "The PC currently hosts intercom requests and scanned-document investigation"): the desk has physical papers, a working scanner tray and an intercom speaker that opens the traveller wheel;
  - supersedes :19's "focus binding" (READY no longer focuses);
  - the CRT needs a power button and LED on its bezel, and a blank glass whose inscribed 4:3 rectangle the game fills.
- **`ArtDeliverables/TimeDesk/HybridScene/SCENE_CONTRACT.md`:** an appended section. The "Existing code anchors" (:49-57) gain:
  - `MonitorScreen` (screen plane + glass size, power) on the PC's glass;
  - `DeskSurface` (the desk plane and paper area);
  - `DeskScanner` (the drop area);
  - `DeskSlot` anchors;
  - `TravellerView.Anchor` and `TravellerHitZone`;
  - `BoothCoordinator`;
  - the focus framing computed from the glass (orthographic now; a perspective adapter at adoption).
  The 4:3 CRT requirement (:39) is confirmed.
- **`docs/ART_ASSET_LIST.md`:**
  - item 5 (`crt.png`) gains the art request (K2 option C): a front-facing CRT whose glass is a large 4:3 rectangle, blank, with a bezel power button and LED;
  - §A: `intercom.png` "(clicked to open the traveller wheel)", `scanner_tray.png` "(desk scanner: drop papers on its glass bed)";
  - new Tier-2 rows: `paper.png` (paper stock per document kind), `crt_power.png`/`crt_led.png`, and grab/grabbing cursors.

### 3.4 `docs/FEATURES.md` (same commits as the behaviour)

Line numbers are the current file's.

- **:11:** append "; desk interactions (papers, scans, the wheel) draw no random numbers and never change generation".
- **:17:** "World-space booth (back wall, partitions, desk, CRT with its live desktop and power button, READY sign, desk scanner and props; the traveller appears when READY is tapped and leaves at the decision) with Cinemachine office/monitor cameras".
- **:18:** "Click the CRT → the camera pushes in until the screen fills most of the view (`DeskConfigSO`: fill 0.85, 0.6 s); the desktop takes input only once the push-in settles; while focused only the desktop and the bezel power button take input; leave with Escape, a click outside the screen (also on a paper showing at the edge of a screen wider than 16:9: papers take no clicks while focused) or the taskbar '< Desk' button (input table, and that the exit zone is never up while the papers take input, tested: `BoothRulesTests`; framing tested: `MonitorFramingTests`)".
- **New after :18:**
  - "Live monitor: the PC desktop is drawn on the CRT glass in the booth view (4:3, 1440×1080 units, masked to the screen), and keeps running while you work at the desk; between travellers it reads 'Waiting for the next traveller'";
  - "Screen power: the bezel button (both views) or Start ▸ Turn off screen darkens the screen; the PC keeps running (scans open windows, the clock ticks); a new traveller or a finished scan wakes it (`DeskConfigSO` toggles); while a citation slip waits for Acknowledge the screen stays on and cannot be turned off (power, wake and hold rules tested: `PcScreenTests`; the power button's citation row tested: `BoothRulesTests`)".
- **:19:** append "; clicking the calendar, stability monitor, till or wall clock shows its value as a tooltip".
- **:21-22:** append "(the booth ignores clicks while a newsletter is up; tested: `BoothRulesTests`)". In :22, the undocumented-denials line says "log a deviation before denying".
- **:23:** "Per-case READY gate: visitor is presented only after the player taps READY (READY no longer zooms in) (tested: `ReadyGateTests`)".
- **New section "Desk" after :24:**
  - "Papers: a traveller hands each document over once, the passport when they step up and the permit when requested (`DocumentTemplateSO` hand-over); papers show title, the traveller's name and a reserved photo slot; drag them anywhere on the desk (they stay where dropped, on top); a sliding paper cannot be picked up (hand-over and which states can be dragged tested: `DeskPapersTests`; clamping and stacking tested: `DeskGeometryTests`)";
  - "Desk scanner: drop a paper on it to scan (1.5 s, one at a time, the clock keeps running); the paper returns to where it was picked up and its scanned window opens on the PC with a desktop icon; a paper dropped while the scanner is busy slides back (scan states, drop outcomes and one-at-a-time tested: `DeskPapersTests`)";
  - "Day 1 desk notes: by the scanner until the first scan, and above the traveller until the wheel is first opened that day (when each shows tested: `DeskPapersTests`)";
  - "Traveller wheel: click the traveller or the desk intercom while they are at the desk; the interview's choices on an elliptical ring (up to 8), sub-menus in place with '< Back' in the centre; Escape or a click outside closes it; a request closes it (layout and fit tested: `RadialLayoutTests`)";
  - "Speech bubble: the traveller's reply to a wheel choice shows beside them for 4 s (read-only; the PC transcript is the evidence; which lines form the reply tested: `InterviewScriptTests`)";
  - "Desk objects react to clicks (squash, wobble, nudge, pulse; `DeskReactionSO`) (poses tested: `ReactionCurveTests`); plant and mug stand at named desk slots for decoration later".
- **:28:** add "and a '< Desk' button".
- **:29:** "Start menu: Settings (stub window), Turn off screen, Quit game; fixed-height entries".
- **:30:** "(documents come via the intercom, not icons)" → "(a document's window gets an icon at the top of the grid the first time it opens)".
- **:32:** add "; windows stay inside the screen while dragged".
- **:33:** Material's window title is "Material Analysis".
- **:34:** "every intercom choice except a document request opens it" → "every wheel choice except a document request opens it".
- **:36:** append "(scanned-document icons are cleared too)".
- **:37:** the interview warning names "the traveller wheel"; append "; likewise it warns once when the desk scanner is not wired: documents then open on the PC when handed over".
- **:51:** "Traveller wheel = the interview: every entry is a dialog choice. Hub: 'Request <document>' for each document handed over on request (one-shot; the traveller hands it over onto the desk, or straight to its window where no desk is wired), 'Ask about home >' ('< Back' first, in the wheel's centre, then today's questions, one-shot per traveller, and small talk) and today's narrative dialogs; content never offers more choices than the wheel shows (8), and the way back and the requests come first (…existing test claims…)".
- **:54:** "A paper's scanned copy renders as a SCANNED page (white page + photo placeholder on dark scanner backing), multi-page, structured fields".
- **:59:** "Deviation Report (the desktop app formerly titled 'Scanner'): …".
- **:76:** "The clock pauses only while a citation slip is shown, and the slip keeps the PC screen on until acknowledged (the interview, scans and camera moves take real time and cost nothing else)".
- **:82:** "white outline on visible booth clickables (CRT, READY, desk props, papers; a clickable whose sprite is hidden, a hit zone over other art such as the traveller, the calendar or the power button, gets the hand cursor only)".
- **:101-102:** "no menu is fuller than the traveller wheel shows (requests count only documents handed over on request)".
- **:103:**
  - "it reports a traveller wheel that fits fewer choices than the content's menu capacity, a desk with fewer paper slots than a traveller's papers, and overlapping sorting bands (bands tested: `DeskGeometryTests`)";
  - "the desktop is a 1440×1080 World Space canvas on the CRT glass; the office-overlay canvas scales from 1920×1080 and never below it".

## 4. Out of scope

- **Piece 8 (wheel content):**
  - choice kinds and icons beyond Back;
  - non-document requests;
  - bubble pacing, a claim bubble on arrival, expressions;
  - wheel theming;
  - "Look >" garments (after piece 4);
  - a paged ring beyond 8.
- **Piece 4 (characters):**
  - the layered figure and its hit-zone refit;
  - photos on the scanned page and on the paper;
  - the amended garment path (§3.2).
- **Piece 6:** theming the wheel, bubble and tooltip; language of the new copy.
- **Piece 9 (translation):**
  - tongue ids;
  - Written/Spoken upgrades;
  - the letter-flip model behind `DisplayText`;
  - untranslated rows not comparable;
  - a manual path so tells stay provable.
- **Decoration (its own piece):**
  - `DeskItemSO` catalogue, shop, and `WorldState` desk block;
  - placement rules;
  - movable decor through `DeskDraggable`;
  - whether a decorate mode pauses the clock.
- **Art (last):**
  - the front-facing CRT (K2 option C) and paper stock (per-country document kits exist under `Assets/Art/UI/Documents`, 128 PNGs, 4 art eras against 6 game eras);
  - grab/grabbing cursors (and their `CursorHotspot` kinds);
  - a scanner light;
  - sounds;
  - the RenderTexture miniature, only if screenshots shimmer (K18);
  - named sorting layers (K13, code audit);
  - hybrid-office adoption, with 3D proxies or a `Collider` generalisation of `Clickable`.
- **Not scheduled:**
  - desk-side compare or stamping verdicts (K11);
  - handing a paper back early;
  - a scanner queue;
  - the power state saved across shifts.

## 5. Tests (EditMode, `Assets/Tests/EditMode`)

- **`BoothRulesTests`** (new):
  - one test per output, each over the rows of §1.9: Newsletter in each view; NoTraveller booth settled; TravellerAtDesk booth settled; wheel open; blending in the booth (`Focused = false, Settled = false`); blending to the monitor (`Focused = true, Settled = false`); focused and settled with the screen on and off;
  - `WheelAllowed` holds while the wheel is open (so opening it never closes it), and `TravellerLive` does not;
  - `CrtFocusable` is true while blending back to the booth;
  - the citation row (R31): with `CitationPending`, `PowerButtonLive` is false in every view, and every other output equals the same context without it;
  - the default context (booth, unsettled, NoTraveller, screen off, wheel closed, no citation) → only `CrtFocusable` true.
- **`PcScreenTests`** (new):
  - the start state follows `startsOn`;
  - `Toggle` twice returns to start, returns true each time and raises `Changed` twice;
  - `TurnOff` when off returns false and raises nothing;
  - `Wake` for each reason × rule on/off × screen on/off: it turns on only when off and the rule is on, and returns true exactly then;
  - null rules never wake;
  - an undefined reason never wakes;
  - holding (R31): `SetHeld(true)` on a dark screen turns it on and raises `Changed` once; on a lit one it raises nothing; while held, `Toggle` and `TurnOff` return false, keep it on and raise nothing; `SetHeld(false)` changes nothing, and afterwards `Toggle` and `TurnOff` work again.
- **`DeskPapersTests`** (new; the state is private, so each test observes it through `CanDrag`, `ScannerBusy`, `OnDeskCount`, `HandOver`, `Drop` and `Tick`):
  - `HandOver` from each state (only `WithTraveller` succeeds; a second hand-over fails);
  - out-of-range indices give false (`HandOver`, `CanDrag`) and `Refused` (`Drop`);
  - `ArrivalIndices` keep paper order; `CaseDocuments.ArrivalIndices` skips a null entry and gives none for a null list; `DocumentHandOvers.IsRequested` holds only for `OnRequest`;
  - `Drop`, a decision table over the paper's state (`WithTraveller`, `OnDesk`, `Scanning`, `Returned`) × `overScanner` × scanner idle or busy: `Stays` only for an `OnDesk` paper not over the scanner; `Scanning` only for an `OnDesk` paper over an idle scanner (then `ScannerBusy` and `CanDrag` false); `Refused` otherwise, with nothing changed (the busy paper keeps scanning, the refused one stays draggable);
  - a scanned paper can be scanned again;
  - `Tick`: −1 before the duration; the index exactly at it; the index on overshoot; 0 and negative amounts ignored; −1 with no scan; after finishing, the paper is draggable again and `ScannerBusy` is false;
  - `ReturnAll` mid-scan → no paper is draggable, `OnDeskCount` 0, `Tick` returns −1 forever, `ScannerBusy` false;
  - `CanDrag` per state;
  - `OnDeskCount` counts scanning papers;
  - `scanSeconds` 0 is raised to 0.01;
  - null document list → `Count` 0;
  - `DeskHints.ScanHintVisible`: blank hint, day past `untilDay`, a scan today and no paper each make it false; all conditions met → true; `untilDay` 0 → never;
  - `DeskHints.WheelHintVisible` (R32): blank hint, day past `untilDay`, the wheel opened today and no traveller at the desk each make it false; all conditions met → true; `untilDay` 0 → never.
- **`DeskGeometryTests`** (new):
  - `DeskRect` `Contains` on edges and outside;
  - `Clamp` per side and inside;
  - `PointAt` corners, centre and clamping of u/v;
  - negative sizes;
  - `RectClamp.Shift`: inside 0, left/right/below/above; an oversized span with and without `keepMax`;
  - `PaperStack`: order after `Add`, re-`Add`, `BringToFront`, `Clear`, and `IndexOf` of absent;
  - `SortingBands.Problems`: the defaults give none; each band equal to the one below (the glass zone included) gives exactly one problem naming both; held inside the paper band.
- **`RadialLayoutTests`** (new):
  - `Point`: item 0 at the top; clockwise (item 1 has x > 0 for n = 8); n = 4 on the axes; n ≤ 0 gives (0, 0);
  - `MaxFit` with the worked numbers of §2.6: `MaxFit(limit 16) == 8` for the defaults (so 7 and 8 fit and 9 does not); a circle of radius 210 gives 4 (n = 5 already collides); a centre box as large as the ring gives 0 (centre overlap is detected); `limit` is respected (`limit 5` → 5);
  - `Extent`: (840, 444) for the defaults.
- **`MonitorFramingTests`** (new): height-limited at 16:9 (the worked 1.034); width-limited at 4:3 and at 1:1; fill clamped (0 and 2); aspect ≤ 0 counts as 1.
- **`ReactionCurveTests`** (new): every kind is identity at t = 0 and 1; peaks at 0.5 for Squash, Pulse and Nudge; Wobble at t = 1/6 gives 25·a°; t is clamped; `None` is always identity.
- **`DisplayTextTests`** (new): returns the canonical string for both media (the identity piece 9 will change); null → "".
- **`InterviewScriptTests`** (changed, LF):
  - the fixture's `documentNames` (58) becomes two OnRequest `CaseDocument`s, so the hub, the ask menu and the walk (110, 142, 259) keep their assertions;
  - `Request_SpeaksPromptAndReply_OpensItsDocument_AndIsRepeatable` (124-135) becomes `…_HandsItsDocumentOver_AndIsOneShot` (`HandOverDocument`, `OneShot` true);
  - new: with Passport OnArrival and Permit OnRequest, the hub is `["request:1", "ask", "dlg:dlg_rumour"]` and the request's `DocumentIndex` is 1;
  - new: every document OnArrival → no request;
  - new: `back` has `Kind == Back` and every other choice `Normal`;
  - new, `SpokenSince`: desk lines are skipped and traveller lines keep their order, joined with a new line; lines before `from` are left out; `from` below 0 counts as 0; `from` at or past the end, or a null transcript, gives "";
  - the wording tests (380-403) expect "the traveller wheel shows at most …";
  - `MenuProblems`' hub test uses the renamed parameter.
- **`InterviewDayTests`** (changed, LF): 110, 131 and 134 say "the traveller wheel".
- **Unchanged and green:** `DialogRunnerTests` (one-shot semantics already covered), `ReadyGateTests`, `ShiftFlowTests`, `CursorHotspotTests`, `OutlineMaskTests` and the rest. Expected offline count after the piece: 337 + the new tests (the plan states the exact number per task).

## 6. Verification plan

**Offline, after every change:** `compile_check.py` 0 errors in every project; the runner `passed N, failed 0`.

**In the worktree's Unity 6000.4.11f1** (GUI mode, `-executeMethod`, one Unity at a time; temporary `_TimeDesk*` scripts patterned on `SCRATCH/prev__TimeDeskAutomation.cs.txt`, `SCRATCH/prev__TimeDeskPlaySmoke.cs.txt` and the screenshot helper in `SCRATCH/cp_play.cs.txt:714-721`; reports and screenshots in SCRATCH; never committed):

0. **Baseline before any piece-7 code** (at `6477d8f`): dump case generation for seeds 12345 and 999 × days 1–3, one line per slot (claim, name, date, role, violator, liar, home, tells with channels, every paper value, every answer). Record `InterviewReachable` and `spoken`.
1. **EditMode suite** through `TestRunnerApi`: everything passes except the known `UnitySkills.Tests.Core.PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly` (reported, ignored).
2. **Content:**
   - Generate World twice: the second run changes no file, and no generated asset differs from `main`;
   - Validate Content Library reports nothing;
   - `MaxRequestedDocuments` is 1 and `MaxDocuments` 2 for the shipped blueprint.
3. **Generation unchanged:** the step-0 dump is re-run and must be byte-identical; `spoken` is true in the rebuilt scene (V7).
4. **Builder:**
   - Build Office UI and save. It logs no error: the wheel fits 8, 4 slots ≥ 2 papers, the bands are clean;
   - the scene has the desktop `Canvas` under `CRTMonitor/ScreenAnchor` (World Space, order 20, no `CanvasScaler`, with a `RectMask2D`);
   - no `IntercomPanel`, `BackToOfficeButton`, `VisitorText`, `Doc1Text`, `Doc2Text`, `ResultText` or `EraButtonsRoot`;
   - `ReadySign`'s `onClick` has 0 persistent calls; `CRTMonitor` has 1 (`FocusMonitor`); `PowerButton` has `TogglePower`; `FocusExitZone` has `FocusOffice` and is inactive; `GlassZone` is inactive, has no persistent call, a non-interactable `Clickable`, and order `glassOrder` on its renderer;
   - `TravellerHitZone` and `DeskIntercom` have `TravellerWheel.Open`;
   - the overlay hosts `TravellerWheel`, `SpeechBubble` and `DeskTooltip` are active, and `Catcher` and both `Panel`s are inactive (R33); `Ring/Centre` has `LayoutElement.ignoreLayout`; `Ring` and both panels have anchors (0.5, 0.5);
   - the `IconScanner` label reads "Deviation Report" and its window title "Deviation Report"; the Material window reads "Material Analysis";
   - **no missed wiring:** every `ObjectReference` property (arrays included, walked with `SerializedObject`) of every new component in the scene (`BoothCoordinator`, `MonitorScreen`, `DeskController`, `DeskScanner`, `DeskDocument` and `DeskDraggable` on the template, the ten `DeskReaction`s, `TravellerView`, `TravellerWheel`, both `OverlayCallout`s) and of every changed one (`InvestigationUIController`, the ring's `InteractionPanelController`, `DesktopShell`, `CinemachineCameraRig`, `OfficeViewController`, `GameManager`) is non-null, except a named allow-list: `DeskReaction.readout` and `audioSource` on the props to which item 10 gives none, and any field of a changed component that is also null in the step-0 baseline scene (listed by name in the report);
   - `Desk_Default.asset` and ten reaction assets exist (their `clip` is null by design); the physics raycaster holds 16 hits.
   - **Idempotence, checked semantically:** build a second time, and the two semantic dumps (every GameObject path and active flag, component types, `RectTransform` geometry to 0.1, TMP strings and sizes, transforms to 0.001, `SetRef` targets, and persistent calls) must be equal (piece-3 §6 step 5's method).
5. **Scripted play-through** on the rebuilt `OfficeScene` (play mode, `[InitializeOnLoad]` + SessionState steps; the game view set with `UnityEditor.PlayModeWindow.SetCustomRenderingResolution`, falling back to the `GameView` reflection if that API is absent). A new run (seed 12345) is started on day 1.
   - **Targets through the real raycast.** Before any press, the step resolves the target with `EventSystem.current.RaycastAll` at the screen point projected from the world target, and asserts that the top hit is the intended object (the paper, the tray, the prop, the traveller zone, the glass zone); only then does it invoke the handlers. So the sorting bands are exercised for every press.
   - **Handler-level input** for most steps: synthetic `PointerEventData` through `ExecuteEvents` (begin/drag/end for drags, `pointerClickHandler` on the resolved hit for clicks).
   - **The real input stack** for at least one drag-to-scan (step 6) and one plain paper click (step 7): a virtual mouse (`InputSystem.AddDevice<Mouse>()`) driven by `InputSystem.QueueStateEvent` press, moves and release, with `InputSystem.settings.editorInputBehaviorInPlayMode = AllDeviceInputAlwaysGoesToGameView` for the run (restored afterwards; `ProjectSettings` is reverted in step 8's hygiene). `InputSystemUIInputModule` then picks the pressed object, applies the 10 px threshold and decides the click, which is what R7 argues from.
   1. During the briefing: `BoothRules` gives the CRT, props and power button inert (read back from `Interactable`); neither day-1 note shows. START SHIFT.
   2. READY armed: the traveller is hidden, the idle line is shown, and no paper exists. **Screenshot `p7_booth_idle_1080.png`.**
   3. READY: the traveller is shown, the passport paper slides to slot 0 (not live until it lands), the permit is absent, and both day-1 notes are visible (scan and wheel). The hub (read from the ring) is `Request Transit Permit`, `Ask about home >`. **Screenshot `p7_booth_papers_1080.png`.**
   4. The **first** click on the traveller hit zone opens the wheel (R33), and the wheel note hides for the rest of the day. **Screenshot `p7_wheel_hub_1080.png`.** "Ask about home >" → centre "< Back"; the ring holds exactly the new choices (no dying buttons laid out). **Screenshot `p7_wheel_ask_1080.png`.** Currency → the bubble shows the answer sentence and the transcript opens on the live monitor. **Screenshot `p7_bubble_1080.png`.** Escape closes the wheel. The intercom prop opens it again; click outside → it closes.
   5. "Request Transit Permit" → the wheel closes, the reply stays in the bubble, the permit lands at slot 1, and the request is gone from the hub.
   6. Drag the passport onto the scanner **with the virtual mouse**: the press resolves to the paper, the drag starts past 10 px, and the scan starts on release. While it scans, drag the permit onto the scanner: refused, back at its pick-up point; a press on it during that slide starts no drag (it is not live, R36). **Screenshot `p7_scanning_1080.png`** (mid-scan). The shift clock advances by at least `scanSeconds` of real time during the scan. At the finish: the passport is back at its pick-up point, its window is open on the live monitor, the icon "Travel Passport" is first in the grid, the scan note is hidden, and the camera did not move. After the virtual-mouse release, no `onClick` fired on the tray or on the paper. Scan the permit.
   7. Drag the permit elsewhere, then outside the desk: its centre is clamped. A plain **virtual-mouse** press and release under 10 px on the passport brings it to the front (orders read back). During a drag, the paper's order is 60 and its collider is off.
   8. **Props and tooltips:** click each of the ten props through the raycast path. The stamp, mug, plant, poster, intercom and tray animate; the tooltips read "Day 01" (the calendar's two-digit text), "Credits: {n}", "Timeline stability: {n}%" and the tray clock's text; each prop is back at its rest pose afterwards (position, rotation and scale read back); the intercom opened the wheel, which is closed again. **Screenshot `p7_tooltip_1080.png`** (the till's tooltip showing).
   9. Click the CRT: `DesktopInteractive` stays false until `IsSettled`, then turns true. **Screenshot `p7_focus_1080.png`.** Set 1280×720 and repeat. **Screenshot `p7_focus_720.png`.** Drag a window partly off-screen: it is clamped and clipped. Compare the passport's tell field (the seed's day-1 papers liar) with its book row: DEVIATION LOGGED. Deny: papers slide back and are destroyed, the traveller is cleared, the bubble and wheel are closed, and `_currentCase` is null.
   10. Next slot: the camera is back in the booth. The bezel power button turns the screen off (the canvas is disabled and the LED dark). **Screenshot `p7_screen_off_booth_1080.png`.** READY → the screen wakes (TravellerPresented). Focus, Start ▸ Turn off screen: the desktop is not interactive, the glass dark, a click on the dark glass keeps the focus (it resolves to `GlassZone`, R37), and the power button (in frame) turns it on. **Screenshot `p7_screen_off_focus_1080.png`** (taken before turning on). Exit by the exit zone, by '< Desk' and by Escape: all three work, and the Records search field loses focus on exit.
   11. Screen off in the booth with a paper scanning: the finish wakes the screen and opens the window.
   12. **Citation hold (R31):** deny an innocent traveller so that a citation slip opens. Start ▸ Turn off screen does nothing (the screen stays on); Escape; the bezel button is not interactable and the screen stays on; the slip is visible on the live monitor; the clock is paused and READY is not armed. Click the CRT, Acknowledge: the clock resumes, the camera returns to the booth, and READY arms.
   13. Force closing time with the traveller at the desk **while a paper scans**: the scan finishes and opens its window; papers and wheel remain until Deny, then are cleared. Force closing on the next slot with READY armed: nothing is handed over and the ledger shows (R27).
   14. `AdvanceToNextDay` to day 2: neither day-1 note shows (day 2 > both last days). Capital on a day-2 Answer-tell liar (a seed picked with the same `CaseFactory` call): the bubble plus the transcript row "Traveller · CAPITAL"; compare it with the Capitals Gazetteer claim row → logged. Deny correct. Day 3 is played to closing without error.
   15. Hover checks (R9, reflection on `HoverHighlighter`'s outline cache): after three travellers, its destroyed keys are at most the last case's papers (R9 purges only when a new outline is created); hovering the next traveller's passport purges them, leaving none. **Screenshot `p7_booth_720.png`** (booth with papers at 720p).
   16. The log holds no warning or error (none is expected in the rebuilt OfficeScene).
6. **Other scenes:**
   - open `Test_DayLoop.unity`, play one case through its era buttons: no exception or new warning;
   - open `OfficeScene_HybridArt.unity` and play **one full case**: READY → the passport window opens with its icon at presentation (R3) → request the permit from its intercom list (the entry is one-shot, and the window gets an icon) → compare a field → Deny. No exception; the new null guards (desk, wheel, idle screen) and the rig's brain lookup hold. Expected warnings: "Desk scanner not wired" (and none about the brain). Codex's scene must show `git status` clean afterwards (never saved).
7. **Screenshots are read and judged** (the Read tool, each against its checklist). Any failure is fixed and re-shot before the piece is done:
   - `p7_booth_idle_1080`:
     - the desktop inside the glass, with teal margins on all four sides and nothing drawn over the bezel;
     - the idle line legible at a glance;
     - no "Visitor"/"Document 1/2" leftovers;
     - the power LED lit and the button on the bezel, not on the glass;
     - no shimmer or broken text on the miniature.
   - `p7_booth_papers_1080`:
     - the traveller visible;
     - the passport lying on the blotter, title and name readable;
     - the scan note on the tray readable and not covered;
     - the wheel note above the traveller readable, clear of the clock and the poster;
     - no paper over the CRT or the READY sign.
   - `p7_wheel_hub_1080` / `p7_wheel_ask_1080`:
     - the ring centred on the traveller, fully on screen;
     - no two items overlapping; labels unclipped;
     - "< Back" centred, touching no ring item.
   - `p7_bubble_1080`: the bubble beside the ring, not over it, fully on screen, text unclipped; the transcript window visible on the live monitor.
   - `p7_scanning_1080`: the paper on the scanner bed, above the tray; the refused paper back on the desk.
   - `p7_tooltip_1080`: the tooltip above the till, fully on screen, text unclipped, over nothing it hides that matters (the CRT's glass).
   - `p7_focus_1080`:
     - the glass fills about 85% of the height, with a bezel visible;
     - the power button visible;
     - the desktop legible (claim banner, window rows, taskbar);
     - no window over the icon column;
     - the transcript clear of the compare bar;
     - "< Desk" in the taskbar.
   - `p7_focus_720`: the same, legible at 720p (window rows readable). If not, the fill knob is raised and re-judged, and the result is recorded in §8 with the art request.
   - `p7_screen_off_*`: the glass shows the painted teal with no desktop; the LED dark.
   - `p7_booth_720`: props, papers and the monitor miniature read correctly at 720p.
8. **Hygiene:**
   - revert Unity-touched unrelated files (`*.csproj`, `ProjectSettings/*`, the TMP fallback atlas);
   - commit the rebuilt `OfficeScene.unity`, the new assets and the placeholders;
   - delete the `_TimeDesk*` files and their metas;
   - append the verification record (with the screenshot verdicts) to this spec as §9.

## 7. Risks

- **Readability.** At fill 0.85 the desktop's 18 pt text is about 15 px at 1080p and 10 px at 720p. The 720p screenshot decides. The fallbacks are a higher fill (less bezel) and the logged CRT art request (K2 option C). Text sizes are not changed in this piece.
- **4:3 and narrower displays** cut the right-bezel power button (the framing's half-width at 4:3 is 1.38, while the button needs 1.49). The game targets 16:9/16:10; the art request moves the button onto a front-facing bezel.
- **Displays wider than 16:9** show the desk's right end at the left of the focused view (at 2560 × 1080 a paper there shows as a strip up to about 300 px wide). The paper takes no clicks while focused (R38), so a click on it still leaves focus; the framing is unchanged.
- **Always-active desktop canvas.** Every controller under it now wakes at scene load instead of at the first focus: `DayFlowUIController` (which removes the analysis's latent START SHIFT ordering hazard), `InvestigationUIController`, `CompareController`, `DesktopShell`, and `DesktopIcon.Start`'s upgrade check. The play-through covers each.
- **World-space UI under the URP 2D Renderer.** A World Space canvas renders in the 2D renderer's transparent sorting by sorting layer and order, and is unlit. The screenshots confirm order against the CRT and the papers, and the `RectMask2D` clipping. The fallback is a dedicated sorting layer for the screen.
- **Input routing** depends on the bands (§2.16). A wrong value would let booth clicks pass through the screen, or the exit zone swallow the desktop or the glass. The builder's band check, the raycast-resolved press targets and play-through steps 6–10 catch it.
- **Drag pitfalls** handled by design: click-after-drag (R7), drops without `OnDrop` (R7), the 8-hit truncation (R24), stale outline order (R8), outline leaks (R9) and grabs mid-slide (R36).
- **Day-1 provability.** Every path must let papers reach the PC: the desk path (scan), the unwired path (direct windows, R3), the fallback (text). A decision mid-scan cancels the scan, but the player can only decide from the PC, after comparing. The permit is reached only through the wheel, which day 1 teaches (R32). Covered by `DeskPapersTests` and steps 5.3–5.13.
- **Balance.** Scans (1.5 s), walking between the desk and the PC, and the wheel take clock time, and pieces 2–3 were tuned with instant windows. The knobs are short, and the play-through records how many travellers a scripted day handles. A queue or shift-length retune is a later decision (V9).
- **Hybrid-scene compatibility.**
  - Its `OfficeViewController` keeps the legacy path, so there are two display paths in one class (an audit target).
  - `DesktopShell`'s "Power" there still quits (`quitButton` keeps the saved `powerButton` data); it has no screen-off entry.
  - Its window layout keeps today's constants (R23), and its rig finds the brain in `Start` (R35).
  - Its intercom panel keeps working through the reused class (R4).
  - The scene is never edited.
  - `Clickable` requires `Collider2D`, so 3D desk papers need proxies at adoption.
- **Builder authority vs the final art sync.** More builder-owned placement (screen anchor, desk surface, slots, hit zones, the 4:3 desktop) must later respect art-sync D3 ("existing-wins") and D4 (Codex's UI skins). The 4:3 re-layout enlarges that merge.
- **Scene merge.** `art` (`0db426f`) diverges heavily from `main` in `OfficeScene.unity` and the builder (merge base `b7f8671`; 119 files differ from `6477d8f`). Rebuilt scenes are never hand-merged: take one side, re-run Build Office UI, repeat §6 step 4.
- **Piece-order coupling.** Pieces 4 and 6 are drafts built on premises this piece removes. The amendment blocks (§3.2) must go in with this piece's docs commit, or those pieces would be planned against stale text.
- **Glass measurement.** R13's rectangle comes from a colour mask. If the screenshot shows canvas over the glass edge or bezel, the constants shrink (they are builder constants; no code change).
- **Scope.** This is the largest piece yet: canvas re-architecture, 4:3 re-layout, drag framework, scanner, power, props, wheel. The plan orders it so that each commit compiles and passes the offline suite. The Unity verification gates the end.
- **Asset hygiene (G6).** The new world text (paper labels, the scan and wheel notes) and the new copy are ASCII apart from "·"/"▸" (already rendered). The TMP fallback atlas is reverted after every Unity run.

## 8. Critique coverage

Every item of the analysis's `critique.missing` list is handled or deferred here:

| # | Missing item | Where |
|---|---|---|
| 1 | `Test_DayLoop.unity` (build scene 0) runs the legacy path | §1.11, §2.12 (every new reference null-checked), R18 (legacy code kept), §6 step 6 |
| 2 | The traveller never arrives or leaves | V1, R10, `TravellerView` §2.8, `SetTravellerAtDesk` §2.12 |
| 3 | Desktop content spills out of the glass | V6, R29 (`RectMask2D`), `DraggableWindow` clamp §2.10 |
| 4 | Overlay raycast priority blocks the booth | R6 |
| 5 | `HoverHighlighter` only knows `Clickable`/`Selectable`; renderer on the same object; cursor override | R8 (paper root carries the renderer and `Clickable`); grab cursors deferred to art (§4); `HoverHighlighter` otherwise unchanged except R9 |
| 6 | READY's `FocusMonitor` is saved in the scene | §2.15 item 4 (`ClearPersistentCalls`), §6 step 4 |
| 7 | Tests assert the intercom wording | §5 `InterviewScriptTests` (380-403), `InterviewDayTests` (110-134) |
| 8 | Player-visible "Intercom" text; piece-6 keys and roles | R25, §2.11 texts, §3.2 piece-6 amendments |
| 9 | Code comments state the old contract | `TranscriptWindowController` 6-13, `OfficeViewController` 15-20, 26 and 45-48, `InvestigationUIController` 283-284, 313-319 and 370-374, builder 119-121, 308-309, 774-777, 1047-1053, 1459-1463 and 1633-1638 (§2.8, §2.10, §2.11, §2.15 items 9 and 13) |
| 10 | Art contracts contradict the feature | R30, §3.3 |
| 11 | Existing hooks: the 2026-06-21 spec's Scanner icon, Dialect, Material Scanner | V2, K17, §3.2 (:109, :113). Dialect stays a placeholder for piece 9 (§4) |
| 12 | More FEATURES lines (:11, :36, :37, :101-102) | §3.4 |
| 13 | Determinism and `InterviewReachable` | V7, R4, §1.12, §6 steps 0 and 3 |
| 14 | Modal states (newsletters, citation, between cases) | V5, R10, R31, §1.9 |
| 15 | Incomplete stale-object list | R18 (all five; `ClaimBanner` kept) |
| 16 | Between cases the monitor shows only the wallpaper | R17 |
| 17 | Day-1 onboarding | V8, R16, R32 |
| 18 | No PlayMode tier; per-frame justification | §2.19, §6 |
| 19 | The item-8 seam covers the compare value and the evidence doc | R19, §2.5 (`DiscrepancyLog` doc), §2.10 (`CompareController` doc) |
| 20 | Poster left out; grab-cursor hotspots; both pieces edit `DocumentTemplateSO` | §1.8 (poster reacts), §4 (cursors deferred), §2.21 (piece 4 re-reads the SO) |

Wrong claims in the analysis, corrected here:
- **Base commit.** The branch base is `main` = `6477d8f`; the characters spec is not on this branch (it exists only as a draft).
- **`OfficeScene.unity:20493` is the live `ClaimBanner`,** not a leftover (R18).
- **`doc1Text` is live in `Test_DayLoop`,** so only the scene children go (R18).
- **READY needs an explicit clear** (§2.15 item 4).
- **Line endings** follow the working-tree column (§2.1).
- **The document kits are 128 PNGs** (§4).
- **The spawn problem is overlap with the icon column,** not overflow (R23).
- **`DraggableWindow` has no clamp code at all.** The analysis's "DraggableWindow.cs:147-155" cites lines past the file's end (the file has 92 lines).
- **The glass rectangle** is re-measured (R13).

## Review notes

### Independent review (2026-09-25)

27 findings, several of them reported twice (F1/F15, F2/F23, F5/F17, F8/F24, F10/F19). Each was checked against the code at `6477d8f` (and the ugui, Cinemachine and Input System package sources in `Library/PackageCache`) before it was applied. All 27 are applied. None is rejected outright; three are applied with a corrected detail (F8's mechanism, F11's line numbers, F22's calendar text), and where two findings proposed different fixes for one defect, the Verdict column names the one taken and why.

| # | Finding | Verdict | Where |
|---|---|---|---|
| F1 | `TravellerWheel` sat on the wheel root, saved inactive, so its `Awake` first ran inside `Open()`'s `SetActive(true)` and closed the wheel during its own activation; the config and camera applied only then | applied (with F15) | R33: the component sits on an always-active host and toggles a `Catcher` child; `Awake` never deactivates its own object; the same rule for both callouts. §2.10, §2.15 items 7–8, §1.13, §6 steps 4 and 5.4 (the first click opens the wheel) |
| F2 | `FindPotentialTargetBrain` in the rig's `Awake` can miss the brain (brains register in their own `OnEnable`, `CinemachineBrain.cs:206-211`; no execution order ties Main Camera to `OfficeRoot`), leaving blend, framing and `IsSettled` broken for the scene; `Lens.Orthographic` depends on `m_OrthoFromCamera` (`LensSettings.cs:169-170, 258-263`) | applied (with F23) | R35: a serialized `brain` set by the builder; otherwise a lookup in `Start` with one warning; `brain.OutputCamera.orthographic`; guards in `ShowMonitor`; `IsSettled` true with no brain. §2.8, §2.15 items 3 and 12, R11 |
| F3 | `BuildBooth` (310) runs before `BuildShiftClockReadouts` creates `WallClock` (1221), and the booth step wires the wheel, bubble and tooltip that were never placed in `Build()` | applied | §2.15 "`Build()` order": config and overlay components right after the newsletters, `BuildBooth` geometry only, the clock as today, then the new `BuildDeskInteraction`, then the wiring block. Item 10 marks each part (b) or (d) |
| F4 | `MakeButton` returns an existing button with its label untouched (716-724), so "Deviation Report" never reached the saved `IconScanner` label | applied | §2.15 item 9: `BuildDesktopIcon` sets the label text every run (the icon is not destroyed, so it keeps its grid place); `FitIconLabel`'s doc updated; §6 step 4 checks the label |
| F5 | `[FormerlySerializedAs("powerButton")] screenOffButton` would turn the hybrid scene's saved Power entry (quit) into a screen-off that only warns | applied, this finding's naming | `[FormerlySerializedAs("powerButton")] quitButton` plus a new `screenOffButton`; the warning path is gone (the builder wires the entry and the screen together). F17 proposed keeping the field name `powerButton`; renaming was preferred because `BoothCoordinator.powerButton` names the bezel's screen button. §2.10, §1.11, §2.20, §3.1, §4 (the "quit from the hybrid desktop" item goes), §7 |
| F6 | `DraggableWindow` citations: the file has 92 lines; `_canvas` is at 17 and 31, the division at 59-60 (88-89 is `Toggle`) | applied | §2.10, §3.1, §8 |
| F7 | Nothing made a sliding paper inert: a paper grabbed mid-slide fought the slide, and the done callback could snap it away; `IsSliding` had no reader | applied | R36: a paper is live only when `live && CanDrag(i) && !IsSliding`, re-applied at each slide's start and in its done callback. §1.5, §1.13, §2.9, §6 steps 5.3 and 5.6 |
| F8 | With the screen off while focused, a click on the dark glass hits the exit zone and leaves focus | applied through F24's mechanism | The defect is real (the desktop raycaster is off, so only world colliders answer). The proposed bare `BoxCollider2D` would report order 0 (`Physics2DRaycaster.cs:69-104`: order comes from a renderer), below the exit zone at 10, so it could not sit "between `focusExitOrder` and `screenCanvasOrder`". R37 uses F24's hit zone with a hidden renderer |
| F9 | (a) `Centre` under `Ring` would be laid out as a ring item without `ignoreLayout`, and `Ring` lacked centred anchors; (b) the wiring list and the §6 non-null check left out many references | applied | (a) §2.10 `RadialLayoutGroup`, §2.15 item 7 (`LayoutElement.ignoreLayout`, anchors (0.5, 0.5) for the ring and both callout panels), `OverlayProjection`'s anchor requirement; (b) §2.15 item 12 lists every reference; §6 step 4 walks every `ObjectReference` (F22c) |
| F10 | Public members with no real caller: `DeskReachable`, `TravellerView.Anchor`/`IsPresent`, `DeskPapers.IsScanned`/`Scanning`, `DeskDraggable.group` | applied (with F19) | See F19 |
| F11 | Stale comments missing from the lists (`StartInterview`, `Choose` and `ShowRich` in `InvestigationUIController`; `OfficeViewController.Update`; the builder's `BuildDesktopShell` and `BuildBooth` docs) | applied, lines corrected | §2.8, §2.11, §2.15 item 13, §8 row 9. The `BuildBooth` doc is 1047-1053 and the `BuildDesktopShell` doc 1459-1463 (the finding said 1047-1052 and 1458-1463); `FitIconLabel`'s doc (1633-1638) is added for F4 |
| F12 | `Update` returns early unless `MonitorFocus` (49-52), so a settle poll placed with Escape never runs while blending back to the booth; `IsSettled` with no rig was unspecified | applied (with F23) | §2.8: the poll runs first in either view, then the MonitorFocus-only Escape check; with no rig the view is settled at once. R11, R35 |
| F13 | `HandOver (0, −2.3, 0)` under `DeskSurface` at (−2.25, −4.15) would land at the player's edge of the desk | applied | §2.15 item 10: world (0, −2.3, 0) = local (2.25, 1.85, 0), just past the desk's far edge (y −2.4) |
| F14 | The citation wake could never fire (a decision needs a lit, focused desktop and opens the slip in the same click, `GameManager.cs:553-641`, `OfficeUIController.cs:231-264`), and V5's citation row had been dropped; the real hazard, a dark screen hiding a pending slip while the clock is paused and READY waits, was unhandled | applied | R31: `PcScreen.SetHeld`, `BoothContext.CitationPending` (power button inert), `BoothCoordinator.SetCitationPending`, called by `ShowVerdictThen`. `WakeReason.CitationShown` and the `onCitation` knob are removed rather than renamed: a `holdOnCitation` set to false would bring the frozen booth back. K7, V5, R10, §1.3, §1.9, §1.13, §2.2, §2.3, §2.8, §2.12, §3.4 (screen-power line and :76), §5 (`PcScreenTests` holding, `BoothRulesTests` row), §6 step 5.12 |
| F15 | The wheel's lifecycle (as F1), plus `InteractionPanelController.Clear` only calls the deferred `Destroy`, so the ring could be laid out for old and new buttons together | applied | As F1; §2.10: `Clear` deactivates each button before `Destroy`. The effect lasts at most until the destroyed children leave the hierarchy (their removal dirties the layout again), but the fix is one line; §6 step 5.4 checks the ring's count |
| F16 | Day 1 never teaches the wheel, yet the permit (the only Technology field, "Declared Device", `DocTemplate_Permit.asset:19-20`) is reachable only through it; the old always-visible intercom listed the request | applied | R32: `DeskHints.WheelHintVisible` (tested), `DeskConfigSO.wheelHint`/`wheelHintUntilDay`, a world note above the traveller owned by `BoothCoordinator` (it knows the day, the phase and the wheel). V8, §1.1, §2.4, §2.7, §2.8, §2.15 item 10, §2.16, §2.17, §3.2 (piece-6 keys), §3.4, §5, §6 steps 5.1, 5.3, 5.4 and 5.14 |
| F17 | (a) As F5; (b) the new window-layout fields had no stated initialisers, so the unrebuilt hybrid scene would load whatever the C# defaults were | applied | (a) as F5, with `quitButton`; (b) R23, §2.11: initialisers equal today's constants, the builder writes the 4:3 values into `OfficeScene` only |
| F18 | Player-facing decisions left in untestable glue (the drop outcome, the bubble text, a two-source holder name) and FEATURES claims wider than their tests | applied | (a) R36, `DeskPapers.Drop` → `DropOutcome` (decision-table test), `BeginScan` private; (b) `InterviewScript.SpokenSince` (tested) in §2.5, §2.11, §5; (c) `holder = inst.visitorGivenName` (which every Name field prints, `CaseFactory.cs:393-394`), §1.5, §2.4, §2.11; §3.4 claims narrowed to what each test covers |
| F19 | More members with no caller (`StateOf`, `PaperStack.Remove`/`Count`, `IsSliding`, `BoothCoordinator.Phase`), and three references to the traveller's anchor | applied | Removed: `IsScanned`, `Scanning`, `PaperStack.Remove`/`Count`, `TravellerView.IsPresent`, the `BoothCoordinator.Phase` getter, `DeskDraggable.group`. Private: `StateOf`, `DeskReachable`. Kept with a named caller: `IsSliding` (F7's liveness rule), `TravellerView.Anchor` (read by `TravellerWheel` through its `traveller` field; the wheel's and bubble's own `anchor` fields are gone). Tests of removed members are dropped or rewritten through behaviour (§5) |
| F20 | `SpeechBubble` and `DeskTooltip` were one mechanism in two components | applied | R34: one `OverlayCallout` (`Show(text, follow, offset, seconds)`, `Hide`), two builder instances. The bubble is shown through `TravellerWheel.Say`, which holds the traveller's anchor and the bubble knobs; tooltip placement and duration move to `DeskReactionSO` (K15); callers apply `DisplayText`. §2.1, §2.7, §2.9, §2.10, §2.11, §2.15 item 8, §2.18–§2.21, §3.2 |
| F21 | The play-through bypassed `InputSystemUIInputModule`, so press picking, the drag threshold and R7's no-click-after-drag were never proven | applied | §6 step 5 preamble: every press target is resolved with `EventSystem.RaycastAll` first; one drag-to-scan (5.6) and one plain click (5.7) go through a virtual mouse with `AllDeviceInputAlwaysGoesToGameView` |
| F22 | Verification gaps: props and tooltips never exercised; the hybrid scene only idled; the non-null check covered six types; the outline-cache check contradicted R9; closing during a scan untested | applied, one text corrected | (a) step 5.8 and `p7_tooltip_1080`; the calendar tooltip reads "Day 01", not "Day 1" (the readout is two-digit, `OfficeReadouts.cs:67`); (b) §6 step 6 plays a full hybrid case; (c) §6 step 4 walks every `ObjectReference` with a named allow-list; (d) step 5.15: at most the last case's keys, purged by the next hover; (e) step 5.13 |
| F23 | Missing camera wiring would freeze the whole booth (no rule for a null rig; `IsSettled` false with no brain) | applied | As F2 and F12 |
| F24 | A click on the dark glass leaves focus; add an inert glass hit zone | applied | R37: `GlassZone` (hidden renderer, glass-sized `BoxCollider2D`, never-interactable `Clickable`), `glassOrder` 11 with the bezel moved to 12, active with the exit zone. §1.2, §1.9, §2.2, §2.6 (`SortingBands` gains the band), §2.7, §2.8, §2.15 item 2, §2.16, §6 steps 4 and 5.10 |
| F25 | The branch points said "with a desk", which could be read as `desk != null` and swallow every hand-over on a partly wired desk | applied | §2.11: every branch and the `ScanFinished` subscription test `DeskReachable`; R3 |
| F26 | Paper labels through `DisplayText` contradicted §2.21 ("the paper stays in its source script") | applied, the paper keeps its source text | K19 (the binding K19 never listed paper labels), R19, §2.9 `DeskDocument.Bind`, §2.21 |
| F27 | The piece-6 amendment missed this piece's new strings, including copy held in ScriptableObjects | applied | §3.2 piece-6 block: every changed or new key (also `desktop.back` :506, `startmenu.power` :511 and flavour :743, `results.unproven` :528); SO-held copy (scan note, wheel note, tooltip templates) is UI copy, not Z4 evidence, so it becomes keys and piece 6's walk must include those fields |

Found while verifying, and fixed here:
- **V1.** `RadialLayout.Fits` had only `MaxFit` and the tests as callers (the rule F10 and F19 apply). It is private; the worked numbers are pinned through `MaxFit` (a radius-210 circle gives 4; the first draft said 6, corrected by the implementation plan's review), and `MaxFit`'s parameters are spelled out (§2.6, §5).
- **V2.** `DeskPapers.BeginScan` loses its only caller to `Drop`, so it is private (§2.4).
- **V3.** `Decide`'s `wheel.Close()` was unreachable: deciding needs focus, and focusing needs the wheel closed. The reply bubble now clears through `wheel.SetCanOpen(false)` when the traveller leaves, so `Decide` does not touch the wheel and `InvestigationUIController` has no `bubble` field (§2.11).
- **V4.** `PcScreen.Toggle` returned "the new state", which a refused toggle while held would report as "on" and a caller could misread. It now returns whether it changed the screen, like `TurnOff` and `Wake` (§2.3).
- **V5.** The play-through gained steps, so §7's step references are renumbered (5.3–5.13, 6–10).
- **V6.** With `StateOf` private, `PaperState` appeared in no public signature; it becomes a private nested enum of `DeskPapers` (§2.4).
- **V7.** `Taskbar/DeskButton` needs the view for its persistent `FocusOffice`, so it is built in `BuildDesktopShell`, after `BuildBooth` (§2.15 item 5); `BuildTaskbar` runs before the view exists.

### Implementation plan departures (2026-09-25)

The implementation plan (`docs/superpowers/plans/2026-09-25-physical-desk.md`) settled these while it was written and reviewed; the sections above now say the same:
- **The radius-210 worked number:** such a circle fits 4, not 6 (R5, §2.6, §5, V1); the shipped ellipse fits 8, as before.
- **`RadialLayout.Extent`** (tested) sizes the wheel's ring, so the projection keeps all of it on screen (§2.6, §2.10).
- **Two fields dropped as dead** (the F10/F19 rule): `TravellerWheel.panel` (§2.10, §2.15 items 7 and 12) and `DeskDocument.paper` (§2.9, §2.15 item 10).
- **`DeskController.HandOver` returns nothing** (§2.9).
- **`_currentCase` is cleared before the decision callback** (R26, §2.11).
- **One home per rule:** `CaseDocuments.ArrivalIndices` (the desk and the no-desk path) and `DocumentHandOvers.IsRequested` (`CaseDocument.Requested` and the validator) (§2.4, §2.11, §2.14, §5); `WirePersistentVoid` clears through `ClearPersistentCalls` (§2.15 item 4).
- **`OverlayProjection` takes the canvas rect** each caller resolves once at `Awake` (`CanvasRectOf`), so nothing is looked up per frame (§2.10).
- **The reply bubble** changes only for a choice that adds a traveller line; "Ask about home >" and "< Back" leave the last reply up (§1.7, §2.11).

## 9. Verification (2026-09-25)

Run in the branch's own Unity 6000.4.11f1 editor through temporary `-executeMethod` scripts (not committed), at `0640de8`:

- Baseline before any piece-7 code (Task 0): seeds 12345 and 999 × days 1–3, 60 slots dumped (claim, name, date, role, verdict, lie, tells with channels, every paper value and answer); `spoken` true in the base scene; 0 null references in the changed components.
- EditMode suite (step 1): 562 passed in the first session-A run; the one failure is the known third-party `UnitySkills.Tests.Core.PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly`. The two later session-A runs, on the same code, passed 561: a second third-party test, `UnitySkills.Tests.Core.SkillsModeManagerTests.Migration_RepeatLoad_IsIdempotent_NoDuplicateAuditEvent`, failed too ("Migration must not re-run when PrefKeyMigrationDone is already true"). It checks the UnitySkills package's own EditorPrefs and its audit log in `Library`, and it passed in the first run, so its result depends on that state. No TimeDesk test failed in any run. Offline Domain and Visuals run: 433/433.
- Content (step 2): Generate World ran twice (Task 19) and twice again (Task 20) and changed no file; Validate Content Library: no issues; a traveller hands over at most 1 document on request and carries at most 2 papers.
- Generation unchanged (step 3): the 60 slots are byte-identical to the baseline; `spoken` is true in the rebuilt scene.
- Builder (step 4): Build Office UI ran twice on OfficeScene with no error, and the two semantic dumps (1546 lines) are equal. Two more builds of the committed scene (Task 20) dump the same, and so does the committed scene as loaded. The comparison leaves out TextMeshPro's own `MeshFilter` (7 entries in Task 19's dump): TextMeshPro adds it with `HideAndDontSave`, so no scene file holds it. A freshly built TMP object has one, and an inactive one in a loaded scene never wakes to add it. The desktop `Canvas` is World Space under `CRTMonitor/ScreenAnchor` (order 20, 1440×1080, `RectMask2D`, no scaler); the retired objects are gone; READY has no persistent call, the CRT focuses, the power button toggles, the exit zone leaves focus and the glass zone is inert; the traveller's zone and the intercom open the wheel; the overlay hosts are active with their `Catcher` and panels inactive; "Deviation Report" and "Material Analysis"; 16 raycast hits; `Desk_Default` and ten reactions. No missed wiring (143 references walked; allowed nulls: `DeskReaction.readout` on DeskPlant, DeskStamp, DeskIntercom, ReactivePoster, ScannerTray and DeskMug, and `DeskReaction.audioSource` on every prop but CreditsTill, the 15 that item 10 leaves empty; no baseline null). An unsaved third build reported exactly the wheel-capacity, spawn-slot and sorting-band errors and changed no file.
- Play-through (step 5; run seed 12349, a new run on day 1; travellers Archestrate (day 1 slot 1, a passport Currency tell), Caecilia (slot 2, honest), Beltani (slot 3) and Awil-Ninurta (day 2 slot 1, the spoken capital)). The report holds 101 `PASS` lines, no `FAIL`, and the twelve screenshots:
  - 5.1–5.3: during the briefing the CRT, the power button and every prop were inert and neither note showed. READY armed with the traveller hidden, the idle line up and no paper. READY brought the traveller in; the passport slid in inert and landed live at slot 0; both day-1 notes showed; the hub read `Request Transit Permit`, `Ask about home >`.
  - 5.4–5.5: the first click on the traveller opened the wheel and hid its note. "Ask about home >" put "< Back" in the centre with exactly the four new choices on the ring. Currency put "We trade with Silver drachma (owl)." in the bubble and opened the transcript on the live monitor, and "< Back" kept the reply up. Escape closed the wheel, the intercom opened it and a click outside the ring closed it. "Request Transit Permit" closed the wheel with "Here you are." in the bubble; the permit landed at slot 1 and its request left the hub.
  - 5.6–5.7: with the virtual mouse, the press resolved to the passport, the drag started past 10 px (order 60, collider off) and the release over the tray started the scan. The permit dropped on the busy scanner slid back inert (a press on it reached no drag handler) and was back at its pick-up point while the passport still scanned. The scan took 1.50 s while the clock ran 1.49 shift minutes. Both papers were back where they had been picked up; the passport's window was open with its icon first; the note hid; the camera stayed; neither the tray nor the paper got a click. The permit then scanned. Dropped elsewhere, the permit stayed where it was dropped; dropped off the desk, it was clamped to the corner (−7.40, −5.90). A 3 px virtual-mouse click brought the passport forward (order 31 over 30, one click), and a held paper sat at order 60 with its collider off.
  - 5.8: all ten props reacted and returned to their rest pose. The tooltips read "Credits: 50", "Timeline stability: 100%", "09:10" and "Day 01", and the intercom opened the wheel.
  - 5.9: the desktop took input only once the push-in had settled; shots were taken at 1080p and 720p. The passport's Coin of Issue against its Currency Ledger row logged "DEVIATION LOGGED — CURRENCY INCORRECT". A window dragged 2000 px off the screen was clamped inside it. Deny cleared the traveller, the wheel, the bubble and the case, and the papers left.
  - 5.10–5.11: the bezel button darkened the screen and the LED, and READY woke the screen. Start ▸ Turn off screen darkened it and took away the desktop's input; a click on the dark glass kept the focus; the power button turned the screen back on. The bezel outside the glass, "< Desk" and Escape each left the focus, and Escape also took the keyboard from the Records search. A scan that finished on a dark screen woke it and opened its window.
  - 5.12: denying the honest Caecilia opened a citation slip on a lit screen, and Turn off screen did nothing. In the booth the power button was inert, the slip stayed on the live monitor, the clock was paused and READY was not armed. Acknowledge resumed the clock and READY armed.
  - 5.13: at closing time, with Beltani at the desk and a scan running, the scan finished and opened its window, the paper stayed and the wheel still opened; Deny ended the day with papers and traveller cleared. On day 2, closing behind READY handed nothing over and the ledger showed.
  - 5.14: day 2 showed no day-1 note. Capital put "Our capital is Kaifeng (Bianjing)." in the bubble. Its transcript row read "Traveller · CAPITAL" and, against the Capitals Gazetteer's claim row, logged "CAPITAL INCORRECT"; the denial was correct, with no citation. Day 3 accepted one traveller and played to closing.
  - 5.15: after two decided travellers the outline cache held one dead paper, and hovering the next traveller's passport purged it.
  - No warnings or errors.
- Other scenes (step 6): Test_DayLoop played one case through its 6 era buttons with no error and no piece-7 warning (its log holds only the scene's existing "no ShiftClockDriver wired" warning). OfficeScene_HybridArt played one full case on the no-desk path: the passport's window and icon at presentation, the one-shot permit request, "DEVIATION LOGGED — CURRENCY INCORRECT", then Deny. Its log held one "Desk scanner not wired" warning and the scene's existing "Traveller wheel or interview transcript not wired" warning (it wires no transcript, so its day generated with `spoken=false`). It also held the scene's existing "no ShiftClockDriver wired" warning, and nothing about the brain. The hybrid scene is unchanged.
- Screenshots (step 7), each read and judged against §6 step 7's checklist:
  - `p7_booth_idle_1080`: pass. The desktop sits inside the glass with teal margins on all four sides and nothing on the bezel. "Waiting for the next traveller" is legible; there are no leftovers. The LED is lit and the button is on the bezel. No shimmer or broken text.
  - `p7_booth_papers_1080`: pass. The traveller (placeholder) is visible. The passport lies on the blotter with "Travel Passport" and "Archestrate" readable. The scan note under the tray is readable and uncovered; its right half sits on the dark blotter, with less contrast. The wheel note above the traveller is clear of the clock and the poster. No paper is over the CRT or READY. Note for the art pass: at slot 0 the passport's top edge overhangs the blotter's edge by about 20 px and the paper partly hides the stamp (`paperSpawnSlots` in `Desk_Default`).
  - `p7_wheel_hub_1080`: pass. Two items sit above and below the traveller, on screen and unclipped, with no overlap.
  - `p7_wheel_ask_1080`: pass. Four items surround "< Back", which is centred at least 100 px clear of each.
  - `p7_bubble_1080`: pass. The bubble is up and to the right of the ring, not over it, fully on screen, with its text unclipped. The transcript window is on the live monitor. Note: while shown, the bubble covers the stability monitor.
  - `p7_scanning_1080`: pass. The passport is on the scanner bed, above the tray, and the refused permit is back at slot 1. Note: the paper is larger than the tray and covers part of the scan note while it scans (the note hides when the scan finishes).
  - `p7_tooltip_1080`: pass. "Credits: 50" sits above the till, fully on screen and unclipped. It covers only the till's own display, clear of the CRT's glass.
  - `p7_focus_1080`: pass. The desktop is 918 of 1080 px high (85%), with the bezel visible left, right and below. The power button and LED are visible. The claim banner, window rows and taskbar are legible, and no window is over the icon column. The transcript sits clear of the compare bar, which was not up in this frame. "< Desk" is in the taskbar.
  - `p7_focus_720`: pass at fill 0.85 (unchanged). Window and transcript rows are readable at 720p. The claim banner's muted text is the weakest, but legible.
  - `p7_screen_off_booth_1080`: pass. The painted teal glass shows no desktop; the LED is dark.
  - `p7_screen_off_focus_1080`: pass. The same in focus, with the power button and the dark LED in frame.
  - `p7_booth_720`: pass. The props, the passport ("Travel Passport", "Beltani") and the monitor miniature read correctly.
- Fixes made during verification: none in the product. The temporary scripts as the plan wrote them were wrong in ten places. Each was corrected in the script, as Task 20's "When a check fails" says, and the affected session re-ran:
  - Session A left TMP's `HideAndDontSave` `MeshFilter` out of the dump comparison.
  - Session B synced 2D physics before it aimed at or raycast a paper that had moved that frame. Otherwise `Collider2D.bounds` lagged, and the drop point was off by the last slide step.
  - Session B pressed the Start menu entry a frame after opening the menu, because a menu opened that frame has no layout and its graphics have no depth yet.
  - Session B remembered the virtual mouse's last queued position, because reading it back from the editor update gave (0, 0) and turned the 3 px click into a drag.
  - Session B disabled the real mouse and keyboard for the run.
  - Session B waited for the push-out to settle after Acknowledge.
  - Session B took `p7_scanning_1080` once the refused permit had landed; three frames caught it mid-slide.
  - Session B's "READY arms" waits also required GameManager's ready gate to be armed. The sign's `Clickable` is serialized interactable since before piece 7, and the day loop arms the gate a frame or more after START SHIFT. Day 2 stalled on a READY pressed in between.
  - Session C waited for the ready gate in the same way.
  - Session C waited up to 30 s for Test_DayLoop's era buttons, because its 4 s timer had included entering play mode.

  A re-run (the plan's "Merging this branch", step 3) needs the same corrections. Observed, not fixed, because it has been true since before piece 7 and is outside this plan: READY is interactable from scene load, so a press between START SHIFT and the first slot's start is a silent no-op.
- Balance (§7): the scripted play-through forces closing time, so it measures no throughput. Scans take 1.5 s each (1.49 shift minutes at the shipped clock rate), and a queue or shift-length retune stays a later decision (V9).

## 10. Final review (2026-09-25)

Five findings from the review after §9, each re-checked against the code at `6856c50` before it was fixed. The fixes change no scene or asset, so the builder is not re-run; the offline suite passes after each one.

- **Inert papers swallowed the click that leaves focus on screens wider than 16:9** (fixed): a paper at the desk's right end shows at the left of the focused view there, and its collider, above the exit zone, took the click. R38: papers the booth puts away take no raycasts; `BoothRulesTests` pins that the exit zone is never up while the papers take input. §1.2, §1.9, §2.9, §3.4 (:18), §7. The collider switch itself is Assembly-CSharp; a merge re-run can check it with the Game view at 2560 × 1080 (focus, then press on a paper dragged to the desk's right end: the view leaves focus).
