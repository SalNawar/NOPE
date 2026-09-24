# Dialog & questions: design (piece 3)

*2026-09-24 · decisions made by Claude under Saleh's instruction "go with all pieces, don't stop" (2026-09-24), open to his review · branch `feat/dialog-questions` (stacked on `feat/identity-lies`)*

The intercom becomes an interview. The player can ask every traveller questions, and the answers appear in a transcript. A liar can now slip in speech as well as on paper: asked about home, they give their true home's value while their papers still show the cover. A spoken slip is proven exactly like a printed one, by comparing the answer with a reference book row (or with the Citizen Record). Questions open up over the first days: the capital on day 2, the ruler on day 3, and the birth date once an upgrade is bought (a hint-only question: it never carries a tell). A small authored dialog system carries narrative conversations, and their consequences apply at the end of the shift. This is Saleh's lie model: "you would know because they would fail one of the questions or because the passport has wrong info".

Piece 2 is complete. `efe385d` holds its final code (the review fixes: the `NoPossibleLie` warning text, `NameRoster.BaseName`, doc wording); Task 13's Unity verification passed there with no product fix (identity-lies spec §8, 216/216 offline, 345 EditMode passes plus the known third-party failure); `15302e1` committed the regenerated day plans and blueprint; `feat/identity-lies` ends at `dc676cd`. Line numbers refer to this branch's files at `8395470`, whose code is unchanged since `efe385d` (the first draft counted from `d7614f0`; Review notes, "Aligned with the code as implemented", lists what moved). This spec builds on piece 2's names as implemented:
- `CaseInstance.trueHome`/`trueHomeLabel`/`IsLiar`/`HomeLabel`/`gender`;
- `CaseFactory.Disguise`/`LiarChance`/`PlaceLabel`;
- `DayPlanSO.tellCount`/`TellCount`;
- `Seeds.ForLies`, `HomeCandidate`, `Forgery.IsProvableTell`, `Lies.Plan`/`LiePlan`, `VerdictRules`;
- `CaseVerdict.wasLiar`/`trueHomeLabel`;
- the fallback agency record and the Records wiring warning.

The piece-3 plan still re-reads each file before anchoring an edit; the implemented code wins.

## 0. Decisions

Claude made these under Saleh's instruction "go with all pieces, don't stop" (2026-09-24), from the piece-3 code analysis (session scratchpad `piece3_map.json`: its decisions Q1–Q18, its completeness check's extra decisions X1–X10, and its list of missing items). They bind this piece and are open to Saleh's review. Q2 was replaced by X1. The R rows under the table are the details this spec settles where a decision left them open.

| Id | Decision | Rationale |
|---|---|---|
| Q1 | **A small custom dialog system.** A pure Domain dialog graph and runner (`Dialog.cs`), with no Ink or Yarn. Content is generated into ScriptableObjects from `world_source.json`. The swap seam is the Domain data contract `AuthoredDialog` (R19). | Questions are built per traveller from today's `FactTable`, the liar's true home and the seeded tells. Ink and Yarn would each bring a second variable store, save format and RNG beside `WorldState`/`Seeds`. They would also break the zero-reference Domain assembly and the offline test runner. |
| X1 (replaces Q2) | **A tell is a (category, channel) pair.** There are two channels. **Papers:** every paper field of the category shows the true home's value (piece 2). **Answer:** the spoken answer to that category's question gives the true home's value, while the papers show the cover. A per-day knob says which channels tells may use: `days[].channels` in `world_source.json`, written into `DayPlanSO.tellChannels`. Day 1 uses Papers only; days 2–3 use Papers and Answer. An Answer tell needs the category's question to be askable that day (day-start snapshot) and something that proves it: a reference book, or the Citizen Record for a birth date. R23 narrows this further: only a question gated by day alone can carry a tell. Piece 2's `tellCount` stays the single budget. A category leaks on one channel only. | This is Saleh's "fail one of the questions OR the passport has wrong info". Every question gets a gameplay purpose. The difficulty ramp is an explicit knob instead of drifting as questions unlock. No new proof type is needed: once an answer can be the statement side, the existing ClaimMismatch/ForeignOrigin/RecordMismatch branches prove it (`DiscrepancyLog.cs:198-246` read only the statement's value and tell flag). |
| Q3 | **Compare to prove.** Transcript answer rows are compare-clickable (new `EvidenceKind.Answer`). An answer against a book row proves ClaimMismatch or ForeignOrigin; an answer against the Records "Born" row proves RecordMismatch. Nothing registers automatically. | It is the same deduction as for papers. `HandlePairCompared` and the deny gate keep their shape. |
| Q4 | **One system.** Every intercom entry is a dialog choice. Each case gets an interview hub with three kinds of entry: "Request <document>" choices (the traveller replies, then the window opens; repeatable); "Ask about home >", a sub-menu with "< Back" first, then the day's questions and small talk; and today's narrative dialogs. | `InteractionPanelController` already reserves its list for interrogation questions (`InteractionPanelController.cs:17-22`). A sub-menu keeps the panel within the 8 buttons the intercom fits (R25). |
| Q5 | **Proof lives in systemic fact questions.** The authored content of piece 3 is wording overrides (keyed by the claimed era) and flavour small talk (per place or per era). Flavour is never evidence and is identical for liars and honest travellers. A future authored topic that must be provable becomes a fact category with a book. | Proof needs a book (the `IsProvableTell` principle), and `FactTable` stays the only truth. |
| Q6 + X3 | **Unlock conditions reuse `TriggerCondition`**, with `TriggerConditionType.UpgradeOwned` appended. A pure Domain evaluator (`Gates`) evaluates them over a plain snapshot (`GateSnapshot`). `TimelineService` evaluates triggers through the same evaluator. "DayAtLeast" reads the snapshot's day. The nightly resolve sees today, before `day++`; the office's day-start snapshot sees the day being played. This difference is written into the enum doc and tested (R4). | Day, flag and counter gates are not written twice. The new branch and the snapshot semantics get decision-table tests, which the Assembly-CSharp evaluator could not have. |
| Q7 | **Two new books.** `RefBook_Capital` (Geography) and `RefBook_Ruler` (Politics), always on the desktop and listed in `content.books`. Tell eligibility is gated on the question being available, not on the book. | Answers must be checkable against a book row, and the rows already exist (`ContentLibrarySO.BuildFactTable` adds every category). |
| Q8 + X10 | **New `interview`, `questions` and `dialogs` sections** in `world_source.json`. `WorldContentGenerator` writes them as ScriptableObjects into an owned, pruned folder (`Assets/Data/World/Interview`), after checking every id and reference. `ContentLibraryValidator` checks the assets. From piece 3 on, `world_source.json` is the hand-maintained source, and the scratchpad `build_world_source.py` is retired. | One source and one generator run. The scratchpad script is not source-controlled and rewrites the whole file (`build_world_source.py:81-83`), so it would silently drop authored dialog. |
| Q9 + X4 | **The transcript is a builder-built window** that takes over the planned "Clue Log" app slot, as "Case Notes: Interview" (`OfficeSceneUIBuilder.cs:1280`). It uses the document/book row pattern, so answer rows are compare-clickable. Like the Scanner, it opens itself whenever a question, small talk or dialog line is added. It closes with every new case, like every window. Booth speech bubbles belong to piece 4. | Law 2: reuse the planned app slot and the row/paging pattern. FEATURES "every new case closes all open windows" keeps holding without an exception. |
| Q10 | **Paging like the books.** An `entriesPerPage` knob (8 for the transcript), and every new line jumps to the newest page. | Reuses the footer, the paging code and the `RectMask2D` rows; no ScrollRect. |
| Q11 | **Locked questions are hidden.** An asked question is one-shot per traveller, and its answer stays in the transcript. Requests are repeatable. | The panel fits about 8 buttons. Documents can only be reopened through the intercom. |
| Q12 | **One discrepancy per category, from any source.** The Summary names the source ("traveller said" or "papers show"). When a second proof of an already documented category is dropped, the compare bar says "ALREADY DOCUMENTED". | `evidenceCount` keeps meaning "distinct proven categories". Today a dropped proof gives no feedback. |
| Q13 | **No tell when the values match.** A category is never a tell when the claim's value equals the true home's value. | Already guaranteed by piece 2's `Forgery.IsProvableTell` (R3). |
| Q14 + X5 | **Narrative consequences through effects.** A dialog choice may name a library-listed `EffectSO`, applied through `TimelineService.ActivateEffect`. Chosen effects are recorded in the shift ledger and applied at the end of the shift, before the save, with a start day of tomorrow. A dialog that carries an effect must be one-shot per run. Completion is remembered by the flag `dlg:{id}:done`. Its builder lives in Domain `FlagKeys`, next to the trigger fired-flag's `trig:{id}:fired`, which moves there from `TimelineTriggerSO`. Dialog effects may carry only instant ops and briefing/news lines (R24). | An end-of-shift save followed by a Continue replay can never apply an effect twice. Flags, counters and briefing/news lines already exist, so there is no new consequence mechanism. The flag grammar is in Domain, so the one-shot rules that read it are tested (§2.5). |
| Q15 | **No explicit time cost.** The real-time shift clock keeps running during the interview. | One-shot questions cap spamming, and no clock API is needed (`ShiftClock` has Start/Stop/Pause/Resume/Tick, no spend). |
| Q16 | **Stable ids now.** Every authored line and choice gets a stable id (grammar in §2.13). Evidence values stay the canonical `FactTable` strings. | Ids are cheap now and costly to retrofit for piece-6 localization. |
| Q17 | **Unlock schedule.** Day 1: requests plus the Currency, Language and Device questions. Day 2: plus Capital. Day 3: plus Ruler. The birth-date question is gated on a new upgrade, **Interview Protocols** (R9). It is hint-only: its answer is always the cover, so it never carries a tell (R23). Answer tells start on day 2 (X1). | "A few options open up with time". A single upgrade-gated item exercises the upgrade path, and buying it never makes the game harder. |
| Q18 | **Fix the broken desktop icon upgrade ids** in the builder: Lexicon → `archive_access`, Material → `adv_scanner`, Dialect ungated and renamed to a plain placeholder (R9). The builder reports unknown ids, and the validator checks every upgrade-id string in content. The scene is rebuilt and committed. | Today the builder's ids "ArchiveAccess", "DialectFilter" and "AdvancedScanner" match no `UpgradeSO.id` (`OfficeSceneUIBuilder.cs:1277-1279`), so those icons can never unlock. |
| X2 | **Unlock announcements.** Every gated question (by day, upgrade or flag) is announced through the existing briefing/news path, from its own authored gate (R5). | The critique showed that a permanent `BriefingLine` would repeat every morning, and that day gates have no effect to carry a line. |
| X6 | **Typed injection.** The UI receives what it needs from `GameManager` through a typed injection, the day's `InterviewDay` (R7). This works with the session-local `WorldState` of standalone scenes (`GameManager.cs:100-104`). | It avoids reaching into the `RunManager` singleton. |
| X7 | **Answers are computed at case generation** in `CaseFactory`, from the same `ResolveFieldValue` and `LiePlan` values the papers use, placeholders included. The one seeded variant pick (small talk) uses a new salted stream, `Seeds.ForDialog`. | Papers and speech share one source of truth. `caseSeed` exists only inside generation (`CaseFactory.cs:91`). |
| X8 | **Flavour placement.** Timeline-reactive flavour lines will come through the existing `EffectChannel.Chatter` cues; piece 3 authors none (R16). Static per-place and per-era flavour lives in `world_source.json`. | The Chatter channel and `TimelineEffects.GetCues` are the designed path (`TimelineCueReceiver.cs:4-11`, `TimelineEffects.cs:123`). |
| X9 | **Gender gets a reader.** The desk's opener uses an honorific from piece 2's gender: sir, madam or traveller. | Piece 2 D10 justified the field by piece 3's need. |
| F1 | **Fallback text mode.** The text fallback lists the interview answers and only the claimed place's entry of each book, instead of every row of every book. | Five books × 24 rows would be 120 lines in a body of about 540 px on a 1080 px canvas (anchors 0.16–0.76 of a panel spanning 0.08–0.92, `InvestigationUIController.cs:470-477`; the book loop is 439-451). |
| C1 | **Copy.** The unproven-denial citation line and the scanner idle hint mention answers. | "Scan the papers next time" is wrong for a liar whose only tell is spoken (`ShiftScoring.cs:142`). |
| O1 | **Out of scope:** premade and legendary arcs, portraits and speech bubbles (piece 4); history-driven questions (piece 5, although the evaluator already handles score and dominance conditions); UI language (piece 6). | See §4. |

### Refinements settled by this spec

| Id | Refinement | Rationale |
|---|---|---|
| R1 | **Tell options.** A home's tell options are (category, channel) pairs. Papers options come first, in the papers' first-appearance order (piece-2 R3). Answer options follow, in the order of today's tell-carrying questions (`InterviewDay.AnswerTellCategories`, R23). Each tell is one uniform `Range` draw over the options still open; picking an option removes the other option of the same category. A place is a true-home candidate when it has at least one option (R1 of piece 2, now counting Answer options). The number of tells is `clamp(tellCount, 1, distinct categories among the options)`. The draw order keeps piece 2's shape: roll, home, one draw per tell, then the birth-date year if BirthDate was picked on either channel. | With Papers as the only channel (day 1), the options are exactly piece 2's printed eligible categories in the same order, so every day-1 lie is byte-for-byte piece 2's (verified in §6). A pair-uniform pick needs one draw per tell. |
| R2 | **One channel per category.** A Papers tell answers with the cover value; an Answer tell leaves the papers on the cover. An answer that disagrees with the traveller's own papers is a hint, never evidence: two statements prove nothing, since neither is a truth source. | This is X1's "a category leaks on one channel only". It also gives the day-1 questions a teaching role: a liar with a paper tell answers with the rehearsed cover, and the answer visibly disagrees with the passport. |
| R3 | **No new rule for Q13.** Piece 2's `IsProvableTell` already refuses a claim value that equals the home's value, and a home value that any other of today's places shares (piece-2 R13). So a ForeignOrigin proof can only name the true home. This is stricter than Q13, which allowed a ForeignOrigin naming another place that shares the value. On day 3, "Sultan Mustafa II" (Egypt and Greece, Early modern) is simply never a Politics tell for those two homes. | Adding a second collision rule would re-implement R13. Days 1–3 have no other shared Geography or Politics value (computed from `world_source.json`). |
| R4 | **What "DayAtLeast" means.** `Gates` compares the snapshot's `Day`. The nightly resolve snapshots before `day++` (`RunManager.cs:180-183`), so a trigger gated "DayAtLeast N" fires the night of day N and its effects start on day N+1. The office's day-start snapshot is the day being played, so a question gated "DayAtLeast N" is askable from day N. `Gates.UnlockNight(fromDay) = fromDay - 1` names the conversion, and `GatesTests` pins both readings. | This makes the difference explicit and tested (Q6). |
| R5 | **Announcing unlocks (X2).** A question's gate is authored once: its day as `questions[].fromDay`, plus optional `conditions` (upgrade, flag, counter, stability). From that one gate the generator writes two things. The question's conditions are `DayAtLeast fromDay` (when `fromDay > 1`) plus the authored ones, read by the day-start snapshot. And for every **gated** question (`fromDay > 1` or any authored condition) it writes a one-shot `TimelineTriggerSO` `unlock_{questionId}`: its conditions are the authored ones plus `DayAtLeast Gates.UnlockNight(fromDay)` (when `fromDay > 1`), and its `newsLineOnFire` is the question's `announce` text. Every plain condition type reads the same at the nightly resolve as at the next day start: Home purchases, end-of-shift flags and counters are already in the world, and the unlock triggers come after the authored triggers, so they see the flags those set that night. The trigger therefore fires on the night before the first day the question is askable. Whether a question is available never depends on the trigger having fired. Two cases arrive late, and both are documented (§1.3): a question whose authored conditions already hold on day 1 is announced on day 2 (there is no night before day 1), and a save from before piece 3 gets its pending notices on its next morning. The notices say "may now ask", never "from today". | The announcement reuses the trigger/news path with no new runtime code, and it cannot drift from the gate. Questions still open in standalone scenes (a session-local `WorldState` never runs a nightly resolve) and in saves from before piece 3. The critique's option B (gating on the fired flag) would break both. |
| R6 | **Condition projection.** The `TriggerConditionType` enum moves from `TimelineTriggerSO.cs` into Domain `Gates.cs` unchanged; its values are serialized as ints, so assets are untouched. `TriggerCondition` stays in Assembly-CSharp, because it holds ScriptableObject references. `TimelineService.ToGate` projects a condition to a plain `GateCondition` (type, key, threshold). `TimelineService.Snapshot` copies the world into a `GateSnapshot`. For score conditions it stores, under the condition's key, the value `GetProfileAttributeScore` (or the nation score) gives at that moment, so the baseline formula keeps one home. `world_source.json` accepts only plain condition types (counter, flag, day, stability, upgrade). The generator rejects types that need a profile, attribute or nation reference until piece 5 adds id resolution. | "The SO-referencing types are projected by glue" (critique X3). A missing reference projects to a null key, which never passes. That matches today's `c.profile != null && ...` checks. |
| R7 | **What the UI is given (X6).** `GameManager` injects an `InterviewDay` (Domain), not `WorldState`. `InterviewDay` decides, from the day-start `GateSnapshot`, today's askable questions, which of them may carry a tell, and the dialogs offered at day start (their conditions pass, their structure is sound, and a one-shot dialog is not already done). It also holds the interview lines and the shift ledger that records completions. With day-start availability (Q6) and end-of-shift effects (X5), the UI never reads or writes `WorldState`. | Injecting `WorldState` would give the UI a dependency it never uses. Every availability rule is in Domain and tested (`InterviewDayTests`); `GameManager` only projects conditions and copies the snapshot. |
| R8 | **Opener and speaker label (X9).** The honorific goes into the desk's opener, `CaseInstance.introLine` ("Next! Step forward, madam.", built by `Interview.Opener`), which becomes the transcript's first line. The traveller's speaker label in the transcript is their bare given name (`visitorGivenName`). | `introLine` was built at `CaseFactory.cs:187` but read only by the legacy `OfficeUIController`. The bare name is exactly what Citizen Records needs, which eases the "Name (Role)" lookup gotcha that piece 2 recorded. |
| R9 | **The upgrade and the icons.** A new `UpgradeSO` **Interview Protocols** (`interview_protocols`, 120 credits) gates the birth-date question, which is hint-only (R23). The Dialect placeholder icon becomes ungated: it stays a placeholder app, since no dialect feature exists. Its window's title "Dialect Filter" and body "Highlights anachronistic phrases. (upgrade)" become "Dialect" and "Notes on accents and phrasing. (placeholder)"; the builder destroys `IconDialectWindow` before building it, as it does `IconScannerWindow`, because `BuildOSWindow` keeps an existing window's texts (`Text()` returns an existing `TitleText`/`Body` unchanged, `OfficeSceneUIBuilder.cs:570-578`). Lexicon → `archive_access`, Material → `adv_scanner`. The builder logs an error for an icon id that `ContentLibrarySO.GetUpgradeById` does not know. | Honest naming: "Dialect Filter" would suggest a feature that is not built. The existing lookup is reused. |
| R10 | **Transcript layout.** A 620×460 window at anchored position (220, 70) on the 1920×1080 reference canvas, so it clears the intercom (x ≥ +557) and the compare bar (y ≤ −173). Rows keep the book rows' fixed 34 px height, so 8 rows fill the 319 px row area. Each row shows the speaker in a 150 px column and the sentence in the rest (about 400 px). The sentence **wraps** inside the row and auto-sizes 12–18 pt: a sentence of up to about 50 characters stays on one line, and a longer one takes two lines at 12–14 pt (two lines of Liberation Sans at 14 pt are about 32 px high). Two lines at 12 pt hold about 120 characters. A content knob, `interview.maxLineChars` (100), bounds every line's worst-case rendered length, and the generator enforces it (§2.13). Only answer rows are clickable. The paging code moves out of `ReferenceBookWindowController` into a shared base, `PagedRowsWindow`, so books and transcript page with one implementation. | `WindowLayer` renders under `IntercomPanel` and `CompareBar` (`OfficeSceneUIBuilder.cs:117,153,205`). Copying the book paging would be self-duplication. One line would not fit this piece's own content: the rumour lines are 70 and 92 characters, and the claim line reaches 70 ("I request passage home to Ottoman Iraq (Baghdad Vilayet) (Industrial)."), while one line at 12 pt holds about 66. Rows that grow with their text would push the newest rows out of the fixed row area, where the mask would clip them. |
| R11 | **The fallback's contents (F1).** It prints, in order: the papers; the agency record (piece-2 R15); "INTERVIEW" (each askable question's prompt and the traveller's answer sentence); and "REFERENCE (claimed place)", one line per book with the claim's value. | Five book lines replace up to 120. The evidence gate is off in the fallback, so origin proofs are not needed there. |
| R12 | **Interview wiring gates the Answer channel.** `InvestigationUIController.InterviewReachable` is true in the text fallback, and true in the rich desk only when the intercom, the transcript window and its chrome are all wired. When it is false, `GameManager` hands the case factory no askable and no tell-carrying categories, so no answer is computed and no Answer tell is generated that day, and the desk offers only the document requests. The rich desk also logs one warning that names the builder. | An OfficeScene that was never rebuilt would otherwise generate liars whose only tell is spoken and can never be proven. |
| R13 | **One label source.** `ClueLabels.Report` (Domain) gives: Geography → CAPITAL, Politics → RULER, Technology → DEVICE, BirthDate → BIRTH DATE, and every other category its upper-case name. Summaries, the compare-bar notes and transcript compare labels use it. The private `Discrepancy.CategoryLabel` (in `DiscrepancyLog.cs`) goes. | The paper says "Declared Device", the book "Index of Devices" and the question "Device"; the scanner printed "TECHNOLOGY". |
| R14 | **The id grammar (Q16)** is set by the generator only (§2.13). Every id, generated or authored, is unique across the whole world source, and an authored line id starts with its dialog's id and a dot. At runtime the transcript names each row `Line_{id}`, which the verification uses to find rows. | Ids get a real reader in piece 3 without dead code. Piece 6's string tables will key on these ids, so two strings must never share one. |
| R15 | **Small talk.** A traveller's small talk comes from their claimed place's lines, or from its era's lines when the place has none (`Interview.PickSmallTalk`). It is one `Range` draw on `Seeds.ForDialog(caseSeed)`, made only when lines exist. It is never evidence and never depends on the true home. | X7's stream. The draw keeps the case and lie streams untouched. |
| R16 | **No Chatter content yet (X8).** Timeline-reactive lines stay unauthored in piece 3, so no receiver is added: there is nothing to receive. Piece 5 adds them through `EffectChannel.Chatter` cues. | No dead code; the path is named. |
| R17 | **`DiscrepancyLog` split.** It becomes a pure static `Prove(a, b, claimNation, claimEra)` plus an instance `Add(proof)`, which refuses a category that is already documented. `TryRegister` is removed: its only production caller, `HandlePairCompared` (`InvestigationUIController.cs:118`), now calls `Prove` and `Add`. Its tests move to `Prove` and `Add` through a test-local helper. | The UI can tell "proves nothing" from "already documented" without copying the proof rules. Keeping `TryRegister` for tests alone would be dead code. |
| R18 | **ASCII-only authored text.** The generator rejects non-ASCII characters in authored interview, question, dialog and small-talk text. Fact values may still contain é, ¥, £ and ä, glyphs the books already render. | New glyphs dirty the TMP dynamic fallback atlas (commit `d3a9a49`, merge gate G6). |
| R19 | **The Q1 swap seam** is the Domain data contract `AuthoredDialog`: the runner never sees a ScriptableObject. A future Yarn or Ink importer produces `AuthoredDialog` data. No interface type is added now. | An interface with one implementation and no second caller would be speculative code. |
| R20 | **Question rules.** A day gate is authored only through `fromDay`; `DayAtLeast` inside a question's `conditions` is rejected. Each category has at most one question. A question's category must be provable: `Forgery.IsProvableCategory`, extracted from the category switch at the top of `Forgery.IsProvableTell` (`Forgery.cs:24-33`), which now calls it. BirthDate (the record proves it) or a category with a reference book; never Name. | Keeps one day value per question (R5), one answer per category, and no unprovable question tells. Questions and tells read one rule for which categories can ever be proven. |
| R21 | **Five book windows.** Book windows wrap after three: `x = -380 + (i % 3) * 320 + (i / 3) * 40`, `y = -150 + (i / 3) * 40`. | At `-380 + i*320` (`InvestigationUIController.cs:321`), the fifth book would open centred at x = +900, off-screen and under the intercom. |
| R22 | **Existing triggers are left as they are.** The three authored triggers (Art Renaissance, Science Boom, Democratic Collapse) use `AttributeScoreAtLeast`/`AtMost` with no profile, so they can never fire today. The projection keeps that result exactly, and piece 5 owns the fix. | The evaluator move must not change trigger behaviour; §6 checks it against a baseline. |
| R23 | **Only day-gated questions carry tells.** "Askable" and "can carry a tell" are split. `InterviewDay.AskableCategories` are the categories of today's askable questions; every one gets an answer. `InterviewDay.AnswerTellCategories` are the askable questions whose conditions are day gates only (`Gates.DayOnly`); only these add Answer options to `Lies.Plan`. A question gated by an upgrade, a flag, a counter or stability is **hint-only**: every traveller answers it with the cover. The birth-date question is hint-only, so a passport birth-date tell (piece 2) visibly disagrees with the spoken date, which the Citizen Record confirms. | X1 makes an unlocked question necessary for an Answer tell, not sufficient. Without the split, buying Interview Protocols would move part of the birth-date tells off the passport (caught by DOB against Records Born) into speech (caught only by asking one more question), and day 3's spoken share would rise from 55% to 60%: the player would pay 120 credits for more work and no new way to detect anything. With it, a purchase or a story flag never changes the lie draws (§1.8), and the upgrade buys a hint. |
| R24 | **What a dialog effect may do.** An effect a dialog names may hold only instant ops (SetFlag, ClearFlag, AddCounter, AddMoney, AddStability, UnlockUpgrade, AddAttributeScore, AddNationScore) and BriefingLine/NewsLine. The continuous ops that change play while active (LegendaryChanceBonus, ForgeryChanceBonus, PayRateBonus, VisitorTagWeight, ShopDiscountPercent, CaseBlueprintWeight, Cue; `EffectOps.ActsWhileActive`) are rejected by the generator and the validator, with a message naming piece 4/5 for timed modifiers. | `ActiveEffectEntry.IsActiveOnDay` has no lower bound (`WorldState.cs:257-258`) and `TimelineEffects.Active` filters on `world.day` (`TimelineEffects.cs:23`), so an entry activated at the end of shift N with start day N+1 already counts in that evening's Home phase (a ShopDiscountPercent would discount tonight's shop) and in a Continue replay of day N (a ForgeryChanceBonus would change the replayed liars). The lower bound cannot be added: `BuildTomorrowPackage` relies on today-filtered `Active()` including entries that start tomorrow. Instant ops apply once at the end of the shift, and lines reach only the next morning's paper, so their timing is what §1.6 promises. |
| R25 | **Menu capacity.** The intercom's `Actions` list fits 8 buttons (about 432 px after padding; 44 px buttons with 6 px spacing). A content knob, `interview.menuCapacity` (8), is checked three ways: the generator and the validator reject content whose ask menu (back + questions + small talk) or hub (the most document templates of any blueprint the day plans or legendaries use + ask + every dialog) or any authored node exceeds it (`DialogChecks.MenuProblems` and `Problems`), and the builder logs an error when the intercom it lays out fits fewer buttons than the knob. "< Back" is the ask menu's **first** choice and document requests are the hub's first choices, so an overflow clipped by the new `RectMask2D` can hide a question or a dialog but never the way back or a document. | With Interview Protocols the ask menu holds exactly 8 entries. One more question in `world_source.json` would otherwise hide "< Back" without any error, trap the player in the ask menu and make a Papers-tell liar unwinnable (documents open only through the intercom). The hub count is conservative (every dialog counted as offered at once); piece 4 revisits it when arcs outgrow it. |
| R26 | **The claim line is content.** The traveller's claim "I request passage home to {place}." moves from `CaseFactory.cs:224` into the `interview` section (`interview.claim`, token `{place}`). `CaseFactory` fills it with `Interview.Claim`, and the banner, the shift summary and the transcript's `case.claim` line share it. | Every spoken line has authored wording and a stable id (Q16); piece 6 gets no code string to localize. |
| R27 | **Source defaults never decide behaviour.** `questions[].fromDay` is required: the generator rejects a value below 1, so a missing key fails loudly whether `JsonUtility` reads it as 0 or not (the `days[].tells` precedent, `WorldContentGenerator.cs:193-194`). A dialog's run-level memory is authored as `repeatable` (default false): a missing key reads false, which is one-shot, and the generator writes `AuthoredDialog.oneShot = !repeatable`. A missing array or string reads as empty. Every piece-3 entry authors `fromDay` explicitly. | The only field initializers the generator relies on today are at the root (`WorldSource.travellerAgeMin = 18`, `travellerAgeMax = 70`). Whether `JsonUtility` runs initializers on elements of nested arrays does not matter under these rules. |

## 1. Behaviour

### 1.1 The interview

- **Every traveller's transcript starts with two lines.** The desk's opener, "Next! Step forward, sir." (the honorific follows the traveller's recorded gender: "sir", "madam", or "traveller" when unknown; a legendary gets "Priority arrival: <name>."). Then the traveller's claim, "I request passage home to <place>." Both sentences are authored content now (R26); the claim banner shows the same claim sentence as before.
- **The intercom is a menu of dialog choices.** The hub offers:
  - one "Request <document>" per document, in paper order. The desk says "Your <document>, please.", the traveller says "Here you are.", and the document window opens. Requests are repeatable and never open the transcript;
  - "Ask about home >", which opens the questions sub-menu. It is shown only when there is at least one question or a small-talk line;
  - one entry per narrative dialog available today (§1.6).
- **The questions sub-menu** lists "< Back" first, then today's askable questions (§1.3) in library order, labelled with the question's label ("Currency", "Capital", …), then "Small talk". Asking a question adds the desk's prompt and the traveller's answer to the transcript, and the question leaves the menu for this traveller. Small talk works the same way. Locked questions are never shown.
- **The menus always fit.** Content never offers more choices than the intercom shows (8), and "< Back" and the document requests come first, so they can never be pushed out of view (R25).
- **The transcript** is the "Clue Log" app ("Case Notes: Interview"). Each row shows the speaker ("DESK" or the traveller's given name) and the sentence; a long sentence wraps onto a second line in a smaller size (R10). It pages 8 rows at a time and jumps to the newest page when a line is added. Any choice other than a document request opens it and brings it to the front. The Clue Log icon opens it too. Like every window, it closes when a new traveller is called (the transcript is per traveller).
- **Only answer rows are clickable.** Clicking one puts the canonical fact value (for example `Babylon (Babili)`) into the compare bar as "Intercom · CAPITAL". The sentence always contains the value verbatim.
- Leaving the monitor (Escape) and coming back keeps the transcript: nothing is animated or timed.
- Asking costs no game time beyond the real clock, which keeps running (Q15).

### 1.2 What travellers say

- **Honest traveller:** every answer is the claimed place's value (the same string its book row shows), or the registered birth date. Answers always match their papers.
- **Liar:** answers with the cover (the claimed place's value, or the registered birth date), except for a tell on the **Answer** channel. There they give the true home's value: the true home's fact, or, for a birth date asked by a day-gated question, the cover's day and month with a year from the true home's birth years. The papers of an Answer-tell category still show the cover. A hint-only question (R23; in piece 3, "When were you born?") is always answered with the cover.
- **A category leaks on one channel only.** A liar whose Currency tell is on the papers answers the Currency question with the cover currency. The answer then visibly disagrees with their passport. That is a hint; the proof is the passport field against the book.
- **Small talk** is flavour from the claimed place, or its era when the place has none. It is the same for liars and honest travellers, and it is never evidence.
- **A missing fact** is answered with the same placeholder the papers would print (`Politics:ancient`); the validator reports such gaps.
- **Wording follows the claim.** A question can have per-era wording (the Currency question asks an Ancient claim "What do you trade with at home?"). Only the sentence changes, never the value.

### 1.3 Questions over time

| Question | Category (menu label) | Askable | Announced | Can carry a tell |
|---|---|---|---|---|
| "What money do you pay with at home?" | Currency ("Currency") | from day 1 | — | from day 2 (the day's channels) |
| "What language do you speak at home?" | Language ("Language") | from day 1 | — | from day 2 |
| "What tool do you use every day?" | Technology ("Device") | from day 1 | — | from day 2 |
| "What is the capital of your land?" | Geography ("Capital") | from day 2 | day 2 morning paper | from day 2 |
| "Who rules your land?" | Politics ("Ruler") | from day 3 | day 3 morning paper | from day 3 |
| "When were you born?" | BirthDate ("Date of birth") | once **Interview Protocols** (120 credits, Home shop) is owned | the morning after it is bought | never (hint-only, R23) |

- What is askable is fixed at the start of the day. An upgrade bought at Home counts from the next office day, and its notice is in that morning's paper.
- **The birth-date question is a hint.** Every traveller answers it with their registered date. When a liar's passport carries a birth-date tell, the spoken date visibly disagrees with the passport; comparing the passport's Date of Birth with the Citizen Record's Born row proves it, as in piece 2. The upgrade's description ("Clearance to ask travellers when they were born.") is authored like the other upgrades' but, like theirs, is not shown in the shop (`HomeUIController.BuildShopRows` prints only name and price); the morning notice is the announcement.
- The Capitals Gazetteer and Rulers & Regents books are on the desktop from day 1.
- The notices say "may now ask", never "from today". A save made before piece 3 still gets every question on the right day, but its notices arrive late: its next nightly resolve fires every notice whose night has passed. A day-3 save gets both the capital notice (two nights late) and the ruler notice (one night late) in its day-4 paper.

### 1.4 Catching a liar by their answers

- Comparing an Answer tell with the claimed place's book row logs, for example, `CAPITAL INCORRECT — traveller said: "Babylon (Babili)"  /  expected: "Thebes (Waset)"`.
- The same answer against the true home's row logs `CAPITAL INCORRECT — traveller said "Babylon (Babili)", which belongs to Babylonia (Ancient)`.
- The birth-date question is hint-only in piece 3 (R23), so its answer never logs anything. A liar whose passport carries a birth-date tell answers it with the registered date, which visibly disagrees with the passport; the passport's Date of Birth against the Born row logs, as in piece 2. The proof rules still cover a spoken birth date: should a later piece add a day-gated birth question, its tell against the Born row would log `BIRTH DATE INCORRECT — traveller said: "3 Jun 1801 BCE"  /  agency records: "3 Jun 1510 BCE"` (tested in `DiscrepancyLogTests` and `LiesTests`).
- Paper proofs keep their wording ("papers: …", "papers show …"). Technology now reads DEVICE: `DEVICE INCORRECT — papers: …`.
- These comparisons log nothing:
  - an honest answer against any row;
  - an answer against the traveller's own papers or another answer;
  - a Papers-tell category's (cover) answer against anything;
  - an answer against a different category.
- One discrepancy per category, whatever the source. Proving an already documented category again shows `●  ALREADY DOCUMENTED — CAPITAL is in the Deviation Report` in the compare bar, and nothing is added.
- The Scanner (Deviation Report) lists answer proofs like paper proofs and opens on the first one.

### 1.5 Tells by day

The day plan's channels say where tells may appear. The shares below come from `world_source.json`, averaging over claims (eras equally weighted, then countries) and uniformly over each claim's eligible homes. Tell count is 1.

| Day | Channels | Askable | Liars whose tell is spoken |
|---|---|---|---|
| 1 | Papers | Currency, Language, Device | 0% (exactly piece 2's tells) |
| 2 | Papers, Answer | + Capital | 50% (Capital answers 12.5%) |
| 3 | Papers, Answer | + Ruler | 55% (Ruler answers 10.2%, lower because of the shared "Sultan Mustafa II") |
| any day with Interview Protocols | unchanged | + Date of birth (hint-only) | unchanged: the lie draws are identical with and without the upgrade (R23) |

- No rolled liar is left without a possible tell on days 1–3.
- In a rich scene where the interview is not wired (R12), no tell is spoken, and the desk warns once.

### 1.6 Narrative dialogs

- A narrative dialog is an authored branching conversation, offered in the hub as its own entry while its conditions hold at day start.
- Choosing an entry plays the traveller's lines. The player picks replies, and the desk "speaks" the reply's label into the transcript. An ending choice returns to the hub. From every node of a dialog some path leads to an ending, so the player can never be trapped away from the hub (§2.5).
- A malformed dialog asset (for example one edited by hand) is never offered; the desk logs an error naming the problem.
- Once completed with any traveller, a dialog is not offered to later travellers that shift. A one-shot dialog is never offered again in the run.
- An ending choice may carry a consequence (an `EffectSO`). Consequences are applied at the end of the shift, before the save:
  - instant ops (flags, counters, money, stability, upgrades, scores) apply at once. The HUD refreshes and endings are checked; if an ending matches, the shift report leads to the title scene instead of Home. Flags set here gate the next day's dialogs and questions;
  - briefing and news lines appear in the next morning's paper, once for an effect that lasts one day.
- A dialog effect cannot carry a timed modifier (a pay, liar, legendary, shop, visitor or blueprint bonus, or a cue): the generator and the validator reject it (R24), because an effect activated at the end of the shift already counts in that evening's Home phase and in a Continue replay of the day. Timed consequences of dialogs belong to pieces 4 and 5.
- A dialog that carries a consequence is always one-shot, so replaying a day after an end-of-shift save can never apply it twice.
- Piece-3 content: "Any news from home? >" (from day 2, one-shot). A traveller mentions pocket calculators being smuggled to the ancients. "Tell me more." leads to a second line and "Noted. Thank you.", which sets the flag `rumour_calculators` and puts a tip in the next morning's briefing. "Not my business." ends without a consequence. The flag unlocks "About those calculators... >", a one-shot follow-up in which a later traveller denies everything.

### 1.7 Verdicts

- The piece-2 verdict table is unchanged: a liar is denied correctly with at least one logged deviation of any source.
- One copy change: the unproven-denial citation becomes "Deviation denied without documented evidence. Log a deviation from the papers or the traveller's answers before denying."
- The scanner idle hint becomes "Compare a document field or a traveller's answer against the claimed place's reference entry, the entry it really belongs to, or the Citizen Record to log evidence."

### 1.8 Determinism and saves

- **Determinism:** same run + same day (and the same interview wiring, R12) = same travellers, same liars, same tells, same small talk; with the same askable questions, also the same answers.
- **Stream contract:**
  - claims, names, registered dates, roles and violator slots are identical to piece 2 for every seed;
  - on day 1 the lies are too (liars, homes, tells, paper values);
  - on days 2–3 the lie stream draws over a larger option pool, so which travellers carry which tell differs from piece 2;
  - purchases and story flags never change the lie draws: only day-gated questions add Answer options (R23), so a day's liars, homes, tells and paper values depend only on the run seed and the day. Buying Interview Protocols adds the birth-date answer to every traveller and nothing else.
- **Saves:** `WorldState` keeps its shape. Dialog memory uses `flags` (`dlg:{id}:done`), consequences use `timeline.activeEffects`, and unlock triggers use their fired flags. So `SaveSystem.SaveVersion` stays 2.
- **Continue replays the day.** Continue after the end-of-shift save replays the same day (`TitleSceneController.cs:59-66`; world-model spec §4 line 70). The replay sees that shift's consequences, just as it already keeps that shift's pay:
  - a completed one-shot dialog is not offered again;
  - a flag-gated follow-up can appear a day early;
  - no effect is applied twice, and no dialog effect acts while active (R24), so no bonus or weight from that shift reaches the replay;
  - a flag- or upgrade-gated question can become askable a day early; it is hint-only (R23), so the replay's tells are unchanged.
  - An instant op that feeds generation (an `UnlockUpgrade` of a clue-gating upgrade, a counter a trigger reads) could still change the replayed travellers; that is the known "save the day's generation inputs" gap (§4). Piece 3's only effect sets a flag and a briefing line, and no generation input reads that flag.

### 1.9 Text fallback

When the rich desk is not built, the fallback panel prints, in order:
1. the papers;
2. the agency record (piece 2);
3. INTERVIEW: every askable question with the traveller's answer sentence;
4. REFERENCE (claimed place): one line per book with the claimed place's value.

The evidence gate stays off in the fallback, as before.

### 1.10 Desktop apps

- Clue Log is the transcript. The remaining placeholder apps are Internet, Lexicon, Dialect, Material and Notes.
- Lexicon unlocks with Archive Access and Material with Advanced Scanner. Dialect is always available and still a placeholder; its window reads "Dialect" / "Notes on accents and phrasing. (placeholder)" instead of promising a "Dialect Filter".

## 2. Design

### 2.1 Where things live

- **TimeDesk.Domain** (`Assets/Scripts/Domain`, pure, EditMode-tested):
  - new: `Gates.cs`, `EffectOps.cs`, `Dialog.cs`, `InterviewContent.cs`, `Interview.cs`, `InterviewScript.cs`, `InterviewDay.cs`, `ClueLabels.cs`;
  - changed: `Lies.cs`, `DiscrepancyLog.cs`, `Seeds.cs`, `ShiftLedger.cs`, `Forgery.cs` (`IsProvableCategory` extracted).
- **Assembly-CSharp:**
  - new: `Dialog/QuestionSO.cs`, `Dialog/DialogSO.cs`, `UI/PagedRowsWindow.cs`, `UI/TranscriptWindowController.cs`;
  - changed: `EffectSO.cs` (the op enum moves to Domain), `Timeline/TimelineTriggerSO.cs`, `Timeline/TimelineService.cs`, `ContentLibrarySO.cs`, `DayPlanSO.cs`, `EraSO.cs`, `Timeline/NationEraProfileSO.cs`, `CaseInstance.cs`, `CaseFactory.cs`, `GameManager.cs`, `Shift/ShiftScoring.cs`, `UI/InvestigationUIController.cs`, `UI/ReferenceBookWindowController.cs`, `UI/CompareController.cs`, `UI/InteractionPanelController.cs` (doc), `UpgradeSO.cs` (doc), `WorldState.cs` (doc).
- **Assembly-CSharp-Editor** (`Assets/Editor`, no asmdef): `WorldContentGenerator.cs`, `ContentLibraryValidator.cs`, `OfficeSceneUIBuilder.cs`.
- **Content:** `world_source.json`; generated `Assets/Data/World/Interview/*`, eras, places and day plans; hand-authored books, the upgrade and the effect (§2.14); `ContentLibrary_Main.asset`; `OfficeScene.unity` (rebuilt).
- **Tests:** `Assets/Tests/EditMode`. The test assembly references only `TimeDesk.Domain` and `TimeDesk.Visuals`, so every new test targets Domain.

Line endings, to preserve:
- **LF:** `Lies.cs`, `Seeds.cs`, `Forgery.cs`, `ReferenceBookWindowController.cs`, `OfficeSceneUIBuilder.cs`, `WorldContentGenerator.cs`, `world_source.json`, `LiesTests.cs`, `SeedsTests.cs`, `ForgeryTests.cs`, `FactTableTests.cs`.
- **CRLF:** `DiscrepancyLog.cs`, `ShiftLedger.cs`, `EffectSO.cs`, `TimelineTriggerSO.cs`, `TimelineService.cs`, `ContentLibrarySO.cs`, `DayPlanSO.cs`, `EraSO.cs`, `NationEraProfileSO.cs`, `CaseInstance.cs`, `CaseFactory.cs`, `GameManager.cs`, `ShiftScoring.cs`, `InvestigationUIController.cs`, `CompareController.cs`, `InteractionPanelController.cs`, `UpgradeSO.cs`, `WorldState.cs`, `ContentLibraryValidator.cs`, `DiscrepancyLogTests.cs`, `docs/FEATURES.md`.
- **New files:** LF.

### 2.2 Domain: gates, flag keys and effect ops (`Gates.cs`, `EffectOps.cs`, new)

- **`TriggerConditionType`** moves here from `TimelineTriggerSO.cs:9-40`, with its values and order unchanged. Its doc says "Condition type for gates (timeline triggers, interview questions, dialogs). Serialized as ints: append only."
  - `DayAtLeast`'s doc gains the R4 reading.
  - Appended: `UpgradeOwned`, "The upgrade is owned (WorldState.HasUpgrade). key = UpgradeSO.id; never the 'upgrade:x' flag an unlock effect may set."
- **`public readonly struct GateCondition`** has the fields `Type` (`TriggerConditionType`), `Key` (string) and `Threshold` (float), and the constructor `GateCondition(TriggerConditionType type, string key, float threshold)`. `Key` is the counter, flag, upgrade, score or dominance key; null means an unresolved reference.
- **`public sealed class GateSnapshot`** is a frozen copy of what gates read. Its constructor copies every collection:

  ```csharp
  GateSnapshot(int day, float stability,
               IEnumerable<string> flags, IEnumerable<string> upgradeIds,
               IEnumerable<KeyValuePair<string, int>> counters,
               IEnumerable<KeyValuePair<string, float>> scores,
               IEnumerable<string> dominantKeys, IEnumerable<string> supportingKeys)
  ```

  Members: `int Day`, `float Stability`, `bool HasFlag(string)`, `bool HasUpgrade(string)`, `int Counter(string)`, `float Score(string)`, `bool IsDominant(string)`, `bool IsSupporting(string)`. A null or blank argument gives false or 0, and `Counter` and `Score` give 0 when the key is missing. This mirrors `WorldState.HasFlag`/`GetCounter` and `TimelineStateData.GetScore`.
- **`public static class Gates`:**
  - `bool Passes(GateCondition c, GateSnapshot s)` is the decision table below. A null snapshot passes nothing.
  - `bool AllPass(IEnumerable<GateCondition> conditions, GateSnapshot s)` is true for a null or empty list and false as soon as one condition fails.
  - `bool DayOnly(IEnumerable<GateCondition> conditions)` is true when every condition is `DayAtLeast` (a null or empty list counts). Caller: `InterviewDay.AnswerTellCategories` (R23).
  - `int UnlockNight(int fromDay) => fromDay - 1` (R4).
- **`public readonly struct Gated<T>`** pairs content with its projected gate: `T Item`, `IReadOnlyList<GateCondition> Conditions` (null counts as none), constructor `Gated(T item, IReadOnlyList<GateCondition> conditions)`. `GameManager` wraps each `QuestionSO`/`DialogSO` in one so `InterviewDay` can decide availability in Domain.
- **`public static class FlagKeys`**, the run-flag grammar, doc "Flag names the run writes into WorldState.flags; one home for the format":
  - `string TriggerFired(string triggerId) => $"trig:{triggerId}:fired"` (moved from `TimelineTriggerSO.FiredFlag`, `TimelineTriggerSO.cs:74`; the format is unchanged, so saves keep their fired flags);
  - `string DialogDone(string dialogId) => $"dlg:{dialogId}:done"`.
- The moved `TriggerConditionType` keeps every int value; `GatesTests` pins them, because assets store the ints.

| Type | Passes when |
|---|---|
| CounterAtLeast | `s.Counter(Key) >= Threshold` (a null key reads 0, as today) |
| FlagSet / FlagNotSet | `s.HasFlag(Key)` / `!s.HasFlag(Key)` |
| AttributeScoreAtLeast / AtMost | `Key != null` and `s.Score(Key) >=` / `<= Threshold` |
| AttributeIsDominant / Supporting | `Key != null` and `s.IsDominant(Key)` / `s.IsSupporting(Key)` |
| NationScoreAtLeast | `Key != null` and `s.Score(Key) >= Threshold` |
| DayAtLeast | `s.Day >= Threshold` |
| StabilityAtMost | `s.Stability <= Threshold` |
| UpgradeOwned | `s.HasUpgrade(Key)` |
| any other value | false |

**`EffectOps.cs`** (new, Domain):
- **`EffectOpType`** moves here from `EffectSO.cs:30-52` with its values and order unchanged. Its trailing `//` comments become `/// <summary>` docs with the same text. Assets store the ints, and every C# reference is in the global namespace, so no other file changes. Doc: "What an effect op does. Serialized as ints: append only."
- **`public static class EffectOps`**, doc "Rules over effect ops, pure so they are tested headless":
  - `bool ActsWhileActive(EffectOpType type)`: true for LegendaryChanceBonus, ForgeryChanceBonus, PayRateBonus, VisitorTagWeight, ShopDiscountPercent, CaseBlueprintWeight and Cue, the continuous ops that change play for as long as the effect is active; false for the instant ops and for BriefingLine/NewsLine, which act once or only through the next morning's paper. Callers: the generator's `CheckInterview` and `ContentLibraryValidator` (R24).

### 2.3 Domain: tells on two channels (`Lies.cs`)

- **New `public enum TellChannel { Papers, Answer }`**, doc "Where a liar's tell shows; serialized in DayPlanSO, append only".
- **`Lies.Plan`** gains two parameters, after `papers`:

  ```csharp
  public static LiePlan Plan(float liarChance, int tellCount,
                             string claimNationId, string claimEraId, string coverBirthDate,
                             IReadOnlyList<HomeCandidate> todays, IReadOnlyList<DocumentField> papers,
                             IReadOnlyList<ClueCategory> answerTellCategories, IReadOnlyList<TellChannel> channels,
                             FactTable facts, ICollection<ClueCategory> bookCategories, IRandomSource rng)
  ```

  `answerTellCategories` doc: "today's question categories that may carry an Answer tell, in question order (InterviewDay.AnswerTellCategories)".

  1. **Roll:** unchanged. A null `rng` is Honest with no draw.
  2. **Options per place:** for each other place (not the claim), build its options.
     - Papers options, when `channels` contains Papers: the printed categories (`PrintedCategories`, unchanged) for which `Forgery.IsProvableTell` holds.
     - Answer options, when `channels` contains Answer: the distinct `answerTellCategories`, in order, for which `IsProvableTell` holds.
     - A null list counts as empty. A place with no option is not a candidate. With no candidate the plan is `NoPossibleLie`, after one draw.
  3. **Home:** unchanged: one `Range` over the candidates, and `HomeIndex` indexes `todays`.
  4. **Tells:** `n = clamp(tellCount, 1, distinct categories among the home's options)`. For each tell, one `Range(0, options.Count)`; record the category and channel, then remove every option of that category.
  5. **Values:** a place fact gets `facts.Get(home, category)`. If BirthDate was picked on either channel, one `BirthDates.PickOtherYear` draw follows, after all tell draws.
- **Private struct `TellOption { ClueCategory Category; TellChannel Channel; }`.**
- **`LiePlan`:**
  - gains `private readonly Dictionary<ClueCategory, TellChannel> _channels`; the internal constructor and `Without` take and pass it;
  - `public TellChannel? ChannelOf(ClueCategory category)` gives the tell's channel, or null when the category is not a tell;
  - `public string TellValue(ClueCategory category)` gives the tell's value (the home's fact or the tell birth date), or null;
  - `ApplyTo` rewrites and flags only the fields whose category is a **Papers** tell. Its doc says so;
  - `Tells` (categories in pick order) is unchanged.
- **Class and member docs:**
  - `LiePlan`: "the true home and the tells the liar leaks, on the papers or in speech";
  - `Lies`: "Who lies about their home, where they really come from, and which tells they leak on their papers or in their answers ... one pick per tell (a category/channel option) ...";
  - `LieOutcome.Liar` (`Lies.cs:34`): "The traveller comes from another of today's places; their papers or answers leak tells." `NoPossibleLie` keeps its text;
  - `Plan`'s summary (109-119), which today says a category is eligible "when the papers print it" and counts "that home's eligible categories", describes the options: a (category, channel) option is open when today's channels allow it and `Forgery.IsProvableTell` holds (Papers for a printed category, Answer for one of `answerTellCategories`), and a liar gets max(1, min(`tellCount`, the distinct categories among the home's options)) tells;
  - `LiePlan._values` (47): "The value of each tell category, printed or spoken (the home's fact, or the tell birth date)".
- **`Forgery.cs`:**
  - new `public static bool IsProvableCategory(ClueCategory category, ICollection<ClueCategory> bookCategories)`: false for Name; true for BirthDate (the Citizen Record proves it); otherwise true exactly when `bookCategories` is non-null and contains the category. Doc: "Whether any tell in this category could ever be proven: the one rule questions (R20) and tells share."
  - `IsProvableTell` starts with `if (!IsProvableCategory(category, bookCategories)) return false;`, then keeps its BirthDate year test and its place-fact checks. Its results are unchanged for every input (the old switch and `!bookCategories.Contains` test are exactly this rule), which `ForgeryTests` keeps green.
  - Class doc: "Which categories can carry a liar's tell, on the papers or in an answer: only those the player can disprove ...".
  - With the capital and ruler books in the library, Geography and Politics become provable, and they are never printed, so they can only be Answer options.

### 2.4 Domain: interview content and answers

**`InterviewContent.cs`** (new). These are serializable content types. The generated ScriptableObjects hold them directly, like the Domain `DocumentFieldSpec` inside `DocumentTemplateSO`, so no projection code is needed. Every nested object field is initialised (`= new()`).

| Type | Fields | Notes |
|---|---|---|
| `LineText` | `string id; string text;` | a line with a stable id |
| `ScriptLine` | `string id; DialogSpeaker speaker; string text;` | one authored spoken line |
| `ScriptChoice` | `string id; string label; List<ScriptLine> lines; string next; string effect;` | `next` empty = ends the dialog; `effect` = EffectSO asset name, applied at end of shift |
| `ScriptNode` | `string id; List<ScriptLine> lines; List<ScriptChoice> choices;` | `lines` are spoken on entering |
| `AuthoredDialog` | `string id; string label; bool oneShot = true; List<ScriptNode> nodes;` | `nodes[0]` is the start; the generator writes `oneShot = !repeatable` (R27) |
| `WordingOverride` | `string eraId; LineText prompt; LineText answer;` | chosen by the traveller's **claimed** era |
| `InterviewQuestion` | `string id; ClueCategory category; string label; LineText prompt; LineText answer; List<WordingOverride> overrides;` | `label` is the ask-menu entry ("Capital"); adds `LineText PromptFor(string eraId)` and `LineText AnswerFor(string eraId)`: the matching override, else the default |
| `InterviewLines` | `string deskName; LineText opener; LineText openerLegendary; LineText claim; string honorificMale, honorificFemale, honorificUnknown; string requestLabel; LineText requestPrompt; LineText requestReply; string askLabel; string backLabel; string smallTalkLabel; LineText smallTalkPrompt; int menuCapacity;` | the interview's fixed wording, plus the layout limit the runtime, the validator and the builder check content against: the most choices the intercom shows at once (R25). The longest line a transcript row holds (R10) is not copied here: it stays in `world_source.json` (`interview.maxLineChars`), whose only reader is Generate World's line-length check (§2.13) |

**`Interview.cs`** (new):

- `public sealed class InterviewAnswer { public ClueCategory category; public string value; public bool isTell; }`: one spoken answer, computed at generation. `value` is the canonical string (a `FactTable` value, a placeholder or a date). `isTell` is true only for an Answer-channel tell.
- `public static class Interview` has five token constants: `ValueToken = "value"`, `HonorificToken = "honorific"`, `NameToken = "name"`, `DocumentToken = "document"`, `PlaceToken = "place"`.
- `public static InterviewAnswer Answer(ClueCategory category, string coverValue, LiePlan lie)` decides as follows. When `lie?.ChannelOf(category) == TellChannel.Answer` it returns `(lie.TellValue(category), isTell: true)`. In every other case (honest, `NoPossibleLie`, a non-tell category, a Papers tell) it returns `(coverValue, false)`.
- `public static string Opener(InterviewLines lines, TravellerGender gender, string legendaryName)`: for a non-blank `legendaryName`, `Fill(lines.openerLegendary.text, NameToken, legendaryName)`; otherwise `Fill(lines.opener.text, HonorificToken, honorific)`, where the private `Honorific(gender, lines)` picks `honorificMale`, `honorificFemale` or `honorificUnknown`. A null `lines` gives "". Caller: `CaseFactory` (R8).
- `public static string Claim(InterviewLines lines, string placeLabel)`: `Fill(lines.claim.text, PlaceToken, placeLabel)`, or the bare `placeLabel` when the template is blank (so the banner never goes empty). Caller: `CaseFactory` (R26).
- `public static LineText PickSmallTalk(IReadOnlyList<LineText> placeLines, IReadOnlyList<LineText> eraLines, IRandomSource rng)` implements R15: the place's lines when it has any, else the era's; one `rng.Range(0, count)` when there is at least one line; null with no draw when there is none (or `rng` is null). Caller: `CaseFactory`.
- `public static int WorstCaseLength(string template, string token, int longestValue)`: the template's length with every `{token}` replaced by a value `longestValue` characters long (a null template gives 0). Caller: the generator's line-length check (R10).
- `public static string Placeholder(string token) => "{" + token + "}"`. Callers: `Fill`, `WorstCaseLength`, and the generator's "template holds its token" checks.
- `public static string Fill(string template, string token, string value)` replaces every `{token}`. A null template gives "", a null value inserts "", and other tokens are left as they are.
- R20's "can this category be asked" is `Forgery.IsProvableCategory` (§2.3); `Interview` has no copy of it.

### 2.5 Domain: dialog graph, interview script, checks, day

**`Dialog.cs`** (new) holds the runtime graph, built per traveller.

- `public enum DialogSpeaker { Desk, Traveller }` and `public enum DialogAction { None, OpenDocument, CompleteDialog }`.
- `DialogLine` is immutable. Its fields are `Id`, `Speaker`, `Text`, `IsAnswer`, `Category`, `Value`, `IsTell`. There are two constructors: a spoken line `(id, speaker, text)`, and `DialogLine.Answer(id, text, InterviewAnswer a)`, a Traveller line carrying the answer's category, value and tell flag.
- `DialogChoice` has public fields `Id`, `Label`, `Lines` (the lines appended when chosen), `Next` (null or empty = stay on the current node), `OneShot`, `Action`, `DocumentIndex`, `DialogId`, `EffectName`.
- `DialogNode` has `Id`, `Lines` (appended each time the node is entered) and `Choices`.
- `DialogGraph` has the constructor `DialogGraph(string startNodeId)`. `Add(DialogNode)` throws `ArgumentException` on a duplicate id. `DialogNode Node(string id)` returns null when missing. It also exposes `StartNodeId`.
- `DialogRunner`:
  - the constructor `DialogRunner(DialogGraph graph, IEnumerable<DialogLine> opening)` sets the transcript to the opening lines and enters the start node, appending its lines;
  - the current node is private state (no public accessor: the UI reads only `Choices` and `Transcript`, and the tests observe the node through `Choices`);
  - `IReadOnlyList<DialogLine> Transcript` is append-only;
  - `IReadOnlyList<DialogChoice> Choices` gives the current node's choices minus the used one-shot ones, as a fresh list;
  - `DialogChoice Choose(string choiceId)` returns null and changes nothing when the id is not among `Choices`. Otherwise it appends the choice's lines, marks it used if one-shot, and moves to `Next`. A null, empty or unknown `Next` stays; entering a node appends that node's lines. It returns the choice.

**`InterviewScript.cs`** (new):

- `public sealed class InterviewCase` is the per-traveller input: `string introLine; string claimLine; string claimedEraId; IReadOnlyList<string> documentNames; IReadOnlyList<InterviewAnswer> answers; LineText smallTalk;`, where a null `smallTalk` means no small talk.
- `public static class InterviewScript` has the node ids `HubNodeId = "hub"` and `AskNodeId = "ask"` and these methods:
  - **`IReadOnlyList<DialogLine> Opening(InterviewCase c)`** returns Desk `introLine` (id `case.intro`, skipped when blank), then Traveller `claimLine` (id `case.claim`).
  - **`DialogLine PromptLine(InterviewQuestion q, string eraId)`** is a Desk line: `q.PromptFor(eraId)`.
  - **`DialogLine AnswerLine(InterviewQuestion q, string eraId, InterviewAnswer a)`** is `DialogLine.Answer(AnswerFor(eraId).id, Fill(AnswerFor(eraId).text, ValueToken, a.value), a)`.
  - **`DialogGraph Build(InterviewLines lines, IReadOnlyList<InterviewQuestion> questions, IReadOnlyList<AuthoredDialog> dialogs, InterviewCase c)`** builds these nodes:
    - **hub**:
      - Choices `request:{i}`, one per document, labelled `Fill(requestLabel, document, name)`. The lines are Desk `Fill(requestPrompt.text, document, name)` (id `requestPrompt.id`) and Traveller `requestReply`. `Action = OpenDocument`, `DocumentIndex = i`, `Next` = null, and the choice is repeatable.
      - Then `ask` (`askLabel`, `Next = "ask"`, no lines). It appears only if the ask node has a question or small talk.
      - Then one choice `dlg:{d.id}` per dialog (`d.label`, no lines, `Next = "{d.id}/{d.nodes[0].id}"`, one-shot).
    - **ask**:
      - first `back` (`backLabel`, `Next = "hub"`), so a menu too long for the intercom can never hide the way back (R25);
      - then one choice `q:{q.id}` per question that has an answer in `c.answers` for its category, in the given order, labelled `q.label`. Its lines are `PromptLine` and `AnswerLine`. It is one-shot, with `Next` null;
      - then `smalltalk` when `c.smallTalk` is set: `smallTalkLabel`; lines are Desk `smallTalkPrompt` and Traveller `c.smallTalk`; one-shot.
    - **Authored nodes:** each node becomes `{d.id}/{node.id}`, with its lines (`ScriptLine` → `DialogLine`). Each choice becomes `{d.id}.{choice.id}`, with lines [Desk `label` (id `{d.id}.{choice.id}`)] followed by its authored lines.
      - A non-empty `next` becomes `{d.id}/{next}`.
      - An empty `next` becomes `Next = "hub"`, `Action = CompleteDialog`, `DialogId = d.id`, `EffectName = choice.effect`.
- `public static class DialogChecks` holds the structure and capacity rules:
  - `List<string> Problems(AuthoredDialog d, int maxChoices)` reports each of these as a message naming the node or choice:
    - no nodes;
    - duplicate node ids;
    - duplicate choice ids;
    - a `next` that names no node;
    - a node unreachable from `nodes[0]`;
    - a node without choices;
    - no ending choice;
    - "node {id} cannot reach an ending": computed backwards, starting from the nodes that have an ending choice and adding every node with a choice whose `next` is already in the set; every reachable node must end up in it (a `start → a → b → a` loop whose only ending sits in a branch the player has left traps them away from the hub, where the document requests are);
    - "choice {id} has an effect but does not end the dialog" (a non-empty `effect` with a non-empty `next`: `InterviewScript.Build` copies effects only onto ending choices, so it would be dropped silently);
    - an effect on a dialog that is not one-shot;
    - "node {id} offers {n} choices; the intercom shows at most {maxChoices}" (skipped when `maxChoices` ≤ 0).
  - `List<string> MenuProblems(int questions, bool smallTalk, int maxDocuments, int dialogs, int maxChoices)` reports an ask menu (1 back + questions + 1 when `smallTalk`) or a hub (`maxDocuments` + 1 ask + `dialogs`) larger than `maxChoices` (R25).

  The generator (before writing) and the validator (on assets) both call them, and `InterviewDay` calls `Problems` at day start, so there is one set of structure rules.

**`InterviewDay.cs`** (new), the day's interview, fixed at day start. Every availability rule lives here, so `InterviewDayTests` covers what the office offers:

- **Constructor:** `InterviewDay(InterviewLines lines, IReadOnlyList<Gated<InterviewQuestion>> questions, IReadOnlyList<Gated<AuthoredDialog>> dialogs, GateSnapshot snapshot, ShiftLedger ledger)`. It decides, once:
  - `Questions`: the questions whose conditions all pass on `snapshot` (`Gates.AllPass`), in the given order;
  - `AskableCategories`: `Questions`' categories, in the same order;
  - `AnswerTellCategories`: the categories of the `Questions` whose conditions are day gates only (`Gates.DayOnly`), in the same order (R23);
  - the dialogs offered at day start: those whose conditions pass, whose `DialogChecks.Problems(d, lines.menuCapacity)` is empty, and that are not one-shot dialogs already done (`d.oneShot && snapshot.HasFlag(FlagKeys.DialogDone(d.id))`); a repeatable dialog is offered whatever flags are set;
  - `ContentProblems`: "Dialog '{id}' is not offered: {problem}" for every problem of every dialog, whether or not its conditions pass. `GameManager` logs each as an error.
- **Properties:** `Lines`, `Questions`, `AskableCategories`, `AnswerTellCategories`, `ContentProblems`.
- **Methods:**
  - `IReadOnlyList<AuthoredDialog> OfferedDialogs()`: the day-start dialogs, minus those the ledger has completed this shift;
  - `bool Complete(string dialogId, string effectName)`: adds a `DialogOutcome { dialogId, effectName, oneShot }` to the ledger. `oneShot` is taken from the dialog. It returns false and changes nothing for an unknown or already completed id.
- **`public static class DialogOutcomes`** (same file) plans what the end of the shift applies, so the one-shot memory and the apply-once rule are tested:
  - `IReadOnlyList<string> FlagsToSet(IReadOnlyList<DialogOutcome> outcomes)`: `FlagKeys.DialogDone(id)` for every **one-shot** outcome, once per dialog id, in ledger order;
  - `IReadOnlyList<DialogOutcome> EffectsToApply(IReadOnlyList<DialogOutcome> outcomes)`: the outcomes with a non-blank `effectName`, the first per dialog id, in ledger order.

  A null list gives an empty result. Caller: `GameManager.ApplyDialogOutcomes`.

### 2.6 Domain: evidence, labels, seeds, ledger

**`DiscrepancyLog.cs`** (CRLF):
- `EvidenceKind` gains a value, appended: `Answer`, "A traveller's spoken answer in the interview transcript".
- The `DiscrepancyProof` value docs (`DiscrepancyLog.cs:23-30`) say "statement" instead of "papers": ClaimMismatch "The statement (papers or answer) differs from the claimed place's reference entry"; ForeignOrigin "The statement matches a reference entry that belongs to a different origin"; RecordMismatch "The statement differs from the agency's citizen record".
- The `CompareEvidence.isAnachronism` doc (piece 2 wording) becomes "Statement side (document field or answer): true if the value is a liar's tell".
- New `public static CompareEvidence ForAnswer(ClueCategory category, string value, bool isTell)` sets `kind = Answer` and `isAnachronism = isTell`.
- `Discrepancy`:
  - gains `public EvidenceKind source;`, "Where the tell was stated: DocumentField (papers) or Answer (the traveller said it)";
  - the `documentValue` doc becomes "The tell's value, as printed or as spoken";
  - the `actualOrigin` doc (107-110, "where the printed value actually belongs") says "the printed or spoken value";
  - `Summary` uses `ClueLabels.Report(category)` and the source:
    - ClaimMismatch: `papers: "{v}"` or `traveller said: "{v}"`, then `  /  expected: "{e}"`;
    - ForeignOrigin: `papers show "{v}", which belongs to {o}` or `traveller said "{v}", which belongs to {o}`;
    - RecordMismatch: `papers: "{v}"` or `traveller said: "{v}"`, then `  /  agency records: "{e}"`.
  - The private `CategoryLabel` (134-136) is removed.
- `DiscrepancyLog`:
  - `public static Discrepancy Prove(CompareEvidence a, CompareEvidence b, string claimedNationId, string claimedEraId)` holds today's rules (167-250). They change in one way: the statement side is the one of kind DocumentField **or** Answer, and it proves something only against exactly one truth source (ReferenceEntry or RecordField). Two statements or two truths give null. It sets `source`.
  - `public bool Add(Discrepancy proof)` adds, or returns false when the proof is null or its category is already documented (252-255 move here).
  - `TryRegister` (doc and body, 162-259) is removed (R17). Its only production caller, `HandlePairCompared`, calls `Prove` and `Add` (§2.11). Its test callers move to `Prove` and `Add` (§5): `DiscrepancyLogTests` (22 calls, through a private helper `Register(log, a, b, nation, era)` that returns `Prove`'s proof when `Add` accepts it, else null), `LiesTests` (the two round-trip calls, which only prove) and `FactTableTests` (two calls, which only prove).
  - The class doc says "Per-case list of documented contradictions (the Deviation Report). `Prove` decides whether a compared pair is a true contradiction: a statement (document field or answer) against one truth source ...; `Add` documents it once per category."

**`ClueLabels.cs`** (new): `public static string Report(ClueCategory category)`, per R13.

**`Seeds.cs`** (LF):
- `public const int DialogSalt = 0x44494147;` ("DIAG").
- `public static int ForDialog(int caseSeed) => Mix(caseSeed, DialogSalt);`, doc "Seed for one traveller's dialog variant picks (small talk), apart from the case and lie streams so content never changes who travellers are or who lies."

**`ShiftLedger.cs`** (CRLF):
- New `[Serializable] public sealed class DialogOutcome { public string dialogId; public string effectName; public bool oneShot; }`, "A narrative dialog completed this shift; its effect is applied at the end of the shift (DialogOutcomes)".
- `ShiftLedger` gains `public readonly List<DialogOutcome> dialogOutcomes = new();`.
- The class doc says "and every narrative dialog completed this shift".

### 2.7 Assembly-CSharp: content assets

- **`Dialog/QuestionSO.cs`** (new; `Assets/Scripts/Dialog` folder + meta), generated only: `public InterviewQuestion question = new();` and `public List<TriggerCondition> conditions = new();`.
- **`Dialog/DialogSO.cs`** (new), generated only: `public AuthoredDialog dialog = new();` and `public List<TriggerCondition> conditions = new();`.
- **`ContentLibrarySO`** (CRLF) gains, under a new `[Header("Interview")]`:
  - fields: `[SerializeField] private InterviewLines interview = new();`, `QuestionSO[] questions`, `DialogSO[] dialogs`;
  - properties: `InterviewLines Interview`, `IReadOnlyList<QuestionSO> Questions`, `IReadOnlyList<DialogSO> Dialogs` (empty when null);
  - doc: the `ReferenceBookCategories` doc (piece 2 wording) gets "... a place-fact tell (on papers or in an answer)".
- **`DayPlanSO`** (CRLF):
  - `[SerializeField] private TellChannel[] tellChannels = { TellChannel.Papers };`, doc "Where today's liars may leak tells: Papers (their documents) and/or Answer (their answers to today's questions). Written by Generate World from world_source.json days[].channels";
  - `public IReadOnlyList<TellChannel> TellChannels => tellChannels ?? Array.Empty<TellChannel>();`;
  - a class-doc bullet.
  - The default keeps hand-made or test day plans on piece-2 behaviour.
- **`EraSO`** (CRLF) gains `public List<LineText> smallTalk = new();`, "Small-talk lines of travellers claiming this era (used when their place has none)".
- **`NationEraProfileSO`** (CRLF) gains `public List<LineText> smallTalk = new();`, "Small-talk lines of travellers claiming this place (flavour, never evidence)".
- **`EffectSO.cs`** (CRLF): `EffectOpType` (30-52) moves to Domain `EffectOps.cs` (§2.2); the class and `EffectOp` are unchanged.
- **`UpgradeSO`** class doc: "... gating clue generation, interview questions (UpgradeOwned) and the Home shop".
- **`WorldState.unlockedUpgradeIds`** doc: "... clue generation, interview questions and shop state".

### 2.8 Assembly-CSharp: timeline glue

- **`TimelineKeys`** (`TimelineService.cs:8-27`) is unchanged: the run-flag grammar lives in Domain `FlagKeys` (§2.2), where the Domain one-shot rules read it.
- **`TimelineTriggerSO.cs`:**
  - the enum is removed (it moves to Domain `Gates.cs`);
  - `FiredFlag => FlagKeys.TriggerFired(id)` (same format);
  - `TriggerCondition.key` doc (84): "Counter key, flag name, or upgrade id (UpgradeOwned)";
  - the class doc now also mentions that the generator writes question-unlock triggers.
- **`TimelineService`** (CRLF):
  - `public static GateCondition ToGate(TriggerCondition c)` gives these keys:
    - `c.key` for CounterAtLeast, FlagSet, FlagNotSet and UpgradeOwned;
    - `TimelineKeys.ProfileAttr(c.profile, c.attribute)` for the score types when both are set;
    - `TimelineKeys.Dominance(...)` for the tier types when both are set;
    - `TimelineKeys.Nation(c.nation)` for NationScoreAtLeast;
    - null otherwise.
  - `public static GateSnapshot Snapshot(WorldState world, IEnumerable<TriggerCondition> conditions)` copies `day`, `timelineStability`, `flags`, `unlockedUpgradeIds`, `counters`, `timeline.dominantKeys` and `timeline.supportingKeys`. It fills `scores` only for the keys the given conditions read: `GetProfileAttributeScore(world, profile, attribute)` for the profile score types, and `world.timeline.GetScore(key)` for nations (R6).
  - `public static List<GateCondition> ToGates(IEnumerable<TriggerCondition> conditions)`: the non-null conditions projected with `ToGate`, in order (a null list gives an empty one). Callers: `AllConditionsPass` and `GameManager.BuildInterviewDay`.
  - The private `AllConditionsPass(TimelineTriggerSO, WorldState)` (330-367) keeps its name, so the §6 baseline can call it by reflection. Its body becomes `Gates.AllPass(ToGates(trigger.conditions), Snapshot(world, trigger.conditions))`, one snapshot per trigger, so a trigger still sees the flags that earlier triggers set that night.

### 2.9 Case generation

**`CaseInstance`** (CRLF):
- `introLine` doc: "The desk's opener for this traveller (interview lines, with the traveller's honorific); the transcript's first line."
- `claimLine` doc: "The traveller's claim sentence (interview.claim with the claimed place's label); the banner, the shift summary and the transcript's second line."
- `IsLiar` doc (73), "(their papers leak tells)", says "(their papers or answers leak tells)".
- New `public readonly List<InterviewAnswer> answers = new();`, "The traveller's answer to each question askable today, in question order (computed at generation from the same values as the papers)."
- New `public LineText smallTalk;`, "What the traveller says when asked small talk (their claimed place's or era's flavour; null when none is authored)."

**`CaseFactory`** (CRLF):
- **Fields:**
  - `private IReadOnlyList<ClueCategory> _askable = Array.Empty<ClueCategory>();` ("today's askable question categories; every traveller answers each");
  - `private IReadOnlyList<ClueCategory> _answerTellCategories = Array.Empty<ClueCategory>();` ("today's question categories that may carry an Answer tell (day-gated questions only)");
  - `private IRandomSource _dialogRng = new SeededRandom(0);` ("the current traveller's dialog stream, Seeds.ForDialog").
- **`GenerateDayCases(DayPlanSO plan, WorldState state, int daySeed, IReadOnlyList<ClueCategory> askable, IReadOnlyList<ClueCategory> answerTellCategories)`:**
  - sets `_askable` and `_answerTellCategories` (null counts as empty);
  - its doc names both lists (`InterviewDay.AskableCategories` and `AnswerTellCategories`);
  - in the loop (88-96), after piece 2's `_lieRng` (94), adds `_dialogRng = new SeededRandom(Seeds.ForDialog(caseSeed));`.
- **Opener:** line 187 becomes `string intro = Interview.Opener(_lib.Interview, gender, legendary != null ? legendary.displayName : null);`, which needs piece 2's `gender`, computed after the name. A blank result logs one warning naming Generate World.
- **Claim (224):** `inst.claimLine = Interview.Claim(_lib.Interview, originLabel);` (R26). A blank `interview.claim` template gives the bare label and one warning naming Generate World.
- **`Disguise`** (piece 2) passes `_answerTellCategories` and `plan.TellChannels` to `Lies.Plan`. Its summary (281-287) becomes "... a liar gets a true home among today's other places; every field of each Papers-tell category is rewritten with that home's value, and an Answer tell leaves the papers on the cover (AddAnswers speaks it) ...". Its `NoPossibleLie` warning (the `efe385d` text, 312) becomes "[CaseFactory] Case {n}: rolled a liar, but no other place today can carry a provable tell against '{originLabel}' (no book-covered fact that its papers print or today's day-gated questions ask, that differs from the claim's and belongs to that place alone, and no birth year other than the record's), so the traveller stays honest. Widen the day's eras or countries, add a reference book, allow more tell channels, or give places that share a fact value distinct values."
- **After `Disguise`:**
  - new private `AddAnswers(CaseInstance inst, LiePlan lie)`: for each `c` in `_askable`, adds `Interview.Answer(c, ResolveFieldValue(c, inst), lie)`. `ResolveFieldValue` supplies the registered birth date and the placeholder grammar. It is called before or independently of `ApplyTo`: it reads the claim, never the fields;
  - `inst.smallTalk = Interview.PickSmallTalk(place's smallTalk, era's smallTalk, _dialogRng)`, where the era is `place.era`, or `claimedEra` for a place-less traveller (glue: it only resolves the two ScriptableObject lists).
- **`ResolveFieldValue`** (343-362): its summary (336-342, the `efe385d` text) adds "also each spoken answer's cover value (AddAnswers)", and its last sentence, "A liar's tells overwrite these values afterwards (Disguise).", becomes "A liar's Papers tells overwrite the printed values afterwards (Disguise); an Answer tell replaces only the spoken value (Interview.Answer)." Its warning (360) becomes "'{origin}' has no {category} fact today; using a placeholder on the papers and in answers. Check the place's facts (Tools > TimeDesk > Validate Content Library)." A missing fact now warns once per printed field and once per askable question; the validator reports the gap before play.
- **Case log (231):** tells print with their channel (`tells=[Geography/Answer]`), plus `answers={inst.answers.Count}`.
- **Class summary:** mentions answers, the claim and opener wording, and the dialog stream.

**Streams after piece 3:**

| Stream | Seed | Draws | Change |
|---|---|---|---|
| Case (`_rng`) | `ForCase` | as piece 2 | none (the opener draws nothing) |
| Clue (`_clueRng`) | `ForClues` | legacy clues | none |
| Violators | `ForViolators` | slots and places | none |
| Lies (`_lieRng`) | `ForLies` | roll; home; one per tell over (category, channel) options; BirthDate year | the option pool grows when Answer is allowed and day-gated questions are askable; identical to piece 2 otherwise; never depends on purchases or flags (R23) |
| Dialog (`_dialogRng`) | `ForDialog` (new) | small-talk pick (0 or 1 draw) | new |

### 2.10 `GameManager` (CRLF)

- **`Start`**, after the ledger (130) and the facts (134):

  ```csharp
  InterviewDay interview = BuildInterviewDay();
  bool spoken = investigationUI != null && investigationUI.InterviewReachable;
  _dayCases = _caseFactory.GenerateDayCases(dayPlan, _worldState, seed,
      spoken ? interview.AskableCategories : Array.Empty<ClueCategory>(),
      spoken ? interview.AnswerTellCategories : Array.Empty<ClueCategory>());
  ```

  The injection block (144-149) adds `investigationUI.SetInterviewDay(interview);`. The day-start log lists the askable and the tell-carrying categories.
- **New private `InterviewDay BuildInterviewDay()`** (glue only; every decision is `InterviewDay`'s):
  1. `snapshot = TimelineService.Snapshot(_worldState, every question's and dialog's conditions)`.
  2. Wrap each library `QuestionSO` as `new Gated<InterviewQuestion>(q.question, TimelineService.ToGates(q.conditions))`, and each `DialogSO` likewise.
  3. `interview = new InterviewDay(contentLibrary.Interview, questions, dialogs, snapshot, _ledger)`.
  4. Log each `interview.ContentProblems` entry as an error ("[GameManager] ... Run Tools > TimeDesk > Generate World, then Validate Content Library.").
- **`HandleDayCompleted`** (200-253), before the `RunManager` block:
  1. `bool changed = ApplyDialogOutcomes();`
  2. If changed: `officeUI.UpdateHud(_worldState)`, and, when `_gameConfig != null`, `EndingService.Evaluate`. A match sets `_worldState.endingId`.
  3. Then the existing `ResetTomorrowModifiers` and `SaveNow`.
  4. The results continuation is `ending != null ? HandleEndingReached : HandleGoHome`, in both branches (237-252).
- **New private `bool ApplyDialogOutcomes()`** (glue over `DialogOutcomes`):
  1. `_worldState.SetFlag(flag)` for each `DialogOutcomes.FlagsToSet(_ledger.dialogOutcomes)`;
  2. for each `DialogOutcomes.EffectsToApply(_ledger.dialogOutcomes)`, resolve `effectName` with `contentLibrary.GetEffectByAssetName`. A missing effect gets a warning: "[GameManager] Dialog '{id}' names effect '{name}', which ContentLibrary_Main does not list; add it to the library's effects." A found effect is applied with `TimelineService.ActivateEffect(_worldState, fx, $"Dialog: {dialogId}", _worldState.day + 1, fx.defaultDurationDays, applyInstantOps: true)`.

  It returns true when any effect was applied.
- **When a dialog effect acts** (R24):
  - its instant ops apply here, at the end of the shift, before the save; that is why the HUD refreshes and endings are checked right after;
  - its briefing and news lines are collected by that night's `BuildTomorrowPackage` and appear in the next morning's paper. `startDay` is tomorrow, like the nightly triggers (`TimelineService.cs:319`), so an effect of `defaultDurationDays` 1 survives that night's `ExpireEffects(tomorrow)`, shows its line once, and expires the night after. A start day of today would expire before `BuildTomorrowPackage` (`ExpireEffects(tomorrow)` runs first);
  - the entry is also "active" from this moment on, because `IsActiveOnDay` has no lower bound (`WorldState.cs:257-258`) and `TimelineEffects.Active` filters on `world.day`. That is harmless only because a dialog effect may not hold ops that act while active (`EffectOps.ActsWhileActive`), which the generator and validator enforce. `IsActiveOnDay` keeps its shape: `BuildTomorrowPackage` relies on today-filtered `Active()` including entries that start tomorrow.

### 2.11 UI

**`PagedRowsWindow`** (new, abstract `MonoBehaviour`, Assembly-CSharp):
- It takes over `ReferenceBookWindowController`'s paging and row cloning, with the **same serialized field names**: `titleText`, `pageText`, `prevButton`, `nextButton`, `entryRowsRoot`, `entryRowTemplate`, `[Min(1)] entriesPerPage = 6`. The existing book template keeps its references.
- `protected virtual void Awake()` wires the prev/next buttons and hides the template.
- Members:
  - `protected void SetTitle(string)`;
  - `public void ShowPage(int page)`, clamped: it updates the footer and rebuilds rows;
  - `public void ShowLastPage()`;
  - `protected abstract int RowCount { get; }`;
  - `protected abstract void FillRow(int index, GameObject row, TMP_Text[] texts, Image background, Button button)`.

**`ReferenceBookWindowController`** (LF) derives from `PagedRowsWindow`. `SetBook` (42-53) keeps its signature and calls `SetTitle` and `ShowPage(0)`. `Rows()` stays. `PageCount`/`ShowPage`/`Rebuild` (59-124) move to the base, and `FillRow` sets the two texts and the compare click as today.

**`TranscriptWindowController`** (new, derives from `PagedRowsWindow`):
- `public void Bind(IReadOnlyList<DialogLine> transcript, string deskName, string travellerName, CompareController compare)` shows the last page.
- `public void Refresh()` shows the last page, called after lines are appended.
- `FillRow`:
  - names the row `Line_{id}`;
  - sets `texts[0]` to `deskName` or `travellerName` and `texts[1]` to `line.Text`;
  - `button.enabled = line.IsAnswer`, so other rows are neither clickable nor hover-highlighted (`HoverHighlighter` ignores disabled selectables);
  - an answer click calls `compare.Select($"Intercom · {ClueLabels.Report(line.Category)}", line.Value, background, CompareEvidence.ForAnswer(line.Category, line.Value, line.IsTell))`.
- Coroutines are never used: the desktop canvas is deactivated outside monitor focus (`OfficeViewController.cs:92-93`).

**`InvestigationUIController`** (CRLF):
- **Fields:**
  - `[Header("Interview")]`, then `[SerializeField] private TranscriptWindowController transcriptWindow;` and `[SerializeField] private OSWindowChrome transcriptChrome;`;
  - private `InterviewDay _day`, `DialogRunner _runner`.
- **`public bool InterviewReachable => !RichMode || (interactionPanel != null && transcriptWindow != null && transcriptChrome != null);`** Every serialized reference is tested with `!= null`, never `?.`: an unassigned serialized field is Unity's fake null in the editor, which `?.` does not catch.
- **`Awake`**, after piece 2's Records warning: when `RichMode && !InterviewReachable`, log once "[InvestigationUIController] Intercom or interview transcript not wired: questions are hidden and no tell is spoken today. Run Tools > TimeDesk > Build Office UI."
- **`public void SetInterviewDay(InterviewDay day)`**, injected by `GameManager`.
- **`HandlePairCompared`** (113-131):
  1. `Discrepancy proof = DiscrepancyLog.Prove(a, b, nation, era);` If it is null, return.
  2. If `!_discrepancies.Add(proof)`, call `compareController.ShowAlreadyDocumented(ClueLabels.Report(proof.category))` and return.
  3. Otherwise, as today: refresh the scanner, show the deviation, open the scanner.
- **`RefreshScannerText`:** the §1.7 idle hint.
- **`ShowRich`:** the action loop (240-274) keeps creating the hidden document windows. Instead of building the actions, it collects the document names and calls the new private `StartInterview(inst, names)`:
  1. `questions = InterviewReachable ? _day.Questions : none` and `dialogs = InterviewReachable ? _day.OfferedDialogs() : none`, and the case's small talk only when reachable: without a wired transcript, nothing spoken could be read, so only the requests remain. The choices that open the transcript (everything but requests) therefore exist only when `transcriptChrome` is wired.
  2. `graph = InterviewScript.Build(_day.Lines, questions, dialogs, case)`. The case is built from `inst.introLine`, `inst.claimLine`, the claimed era id, the names, `inst.answers` and `inst.smallTalk`.
  3. `_runner = new DialogRunner(graph, InterviewScript.Opening(case))`.
  4. When `transcriptWindow != null`: `transcriptWindow.Bind(_runner.Transcript, _day.Lines.deskName, inst.visitorGivenName, compareController)`.
  5. `RefreshChoices()`.

  With no `_day` it logs an error, because `GameManager` always injects one.
- **`RefreshChoices()`** sets `interactionPanel.SetActions(_runner.Choices → InteractionAction { label = choice.Label, execute = () => Choose(choice.Id) })`.
- **`Choose(string id)`:**
  1. `choice = _runner.Choose(id)`; return if it is null.
  2. When `transcriptWindow != null`: `transcriptWindow.Refresh()`.
  3. OpenDocument opens and raises that document's window, as the old action did. Any other action opens the transcript: `if (transcriptChrome != null) transcriptChrome.Open();` (always true in practice, by step 1).
  4. CompleteDialog calls `_day.Complete(choice.DialogId, choice.EffectName)`.
  5. `RefreshChoices()`.
- **`BuildBookShelf`** (321): R21 placement.
- **Fallback:** `BuildFallbackBody` (a private static method) also takes `_day`. After piece 2's "— AGENCY RECORD —" block it prints "— INTERVIEW —": for each `_day.Questions` entry with an answer, `PromptLine` text and `"    {name}: {AnswerLine text}"`. It replaces the "— REFERENCE BOOKS (cross-check) —" loop (439-451) with "— REFERENCE (claimed place) —": `"{book.displayName}: {facts.Get(claim, book.category) ?? "(no entry)"}"`. Its summary (405-408, "the papers, the traveller's agency record ... and today's books") lists the papers, the agency record, the interview and the claimed place's book entries.
- **Docs:** the class summary mentions the interview.

**`InteractionPanelController`** (doc only): "Actions are the current interview node's choices (requests, questions, dialog replies), supplied per step by the investigation controller."

**`CompareController`** (CRLF):
- `public void ShowAlreadyDocumented(string categoryLabel)` sets the neutral colour and the text `$"●  ALREADY DOCUMENTED — {categoryLabel} is in the Deviation Report"`. The "●" and "—" glyphs are already used by `ShowDeviation`.
- The class doc (5-11), which names only document and book rows as `Select` callers, becomes "... Document field rows, reference-book entry rows, Citizen Records rows and interview transcript answer rows call Select."

### 2.12 Builder (`OfficeSceneUIBuilder.cs`, LF)

- **Intercom (151-167):** after `AddVLayout(intercomActions, 6f)`, add a `RectMask2D` to `Actions` when missing (idempotent). The children are unchanged.
- **Intercom capacity (R25):** the builder computes how many action buttons its `Actions` rect fits at the 1080 px reference height, from the numbers it lays out (panel anchors 0.36–0.85, `Actions` anchors 0.02–0.86, padding 6 + 6, spacing 6, button height 44): `floor((height − 12 + 6) / (44 + 6))` = 8. The anchors, spacing and button height become named local constants shared by the layout calls and this computation. After the library lookup (223), when `library != null` and `library.Interview.menuCapacity` exceeds the fit, it logs `"[TimeDesk] The intercom fits {fit} choices, but the content library's interview menu capacity is {n}; lower interview.menuCapacity in world_source.json or enlarge the intercom."`.
- **Transcript window**, a new block after Citizen Records (after 201):
  1. `DestroyChildIfPresent(windowLayer, "IconClueLogWindow")` (the retired placeholder).
  2. `Panel(windowLayer, "TranscriptWindow", Center, Center, new Vector2(220f, 70f), new Vector2(620f, 460f), Paper)`.
  3. `BuildWindowShell(win, "Case Notes: Interview")`.
  4. Get or add a `TranscriptWindowController` and set `titleText`, `pageText`, `prevButton`, `nextButton`, `entryRowsRoot`, `entryRowTemplate`, and `entriesPerPage = 8`.
  5. On every build, re-apply the row template's layout (R10). The row keeps `BuildRowTemplate`'s fixed 34 px height. `HorizontalLayoutGroup.childForceExpandWidth = false`. `Label` (the speaker) gets a `LayoutElement` with minWidth and preferredWidth 150 and flexibleWidth 0, `enableAutoSizing` 12–18 pt, no wrapping and `TextOverflowModes.Ellipsis`. `Value` (the sentence) gets a `LayoutElement` with minWidth 0, preferredWidth 0 and flexibleWidth 1 (so its width never follows its text), `enableAutoSizing` 12–18 pt and `TextWrappingModes.Normal`.
  6. `transcriptChrome = win.GetComponent<OSWindowChrome>()`, added by `BuildWinControls`.
  7. Leave the window inactive.
  8. `BuildDesktopIcon(bookShelf, "IconClueLog", "Clue Log", transcriptChrome, "")`.
- **`soInvest` (288-304):** `SetRef` `transcriptWindow` and `transcriptChrome`.
- **`BuildDesktopShell(Canvas, Transform, Transform, ContentLibrarySO library)`:** the call at 247 passes `library`, which is found at 223 and may be null (231 then warns that no library was found).
  - `apps` (1274-1282) drops the Clue Log row, and its upgrade ids become Lexicon `archive_access`, Dialect `""`, Material `adv_scanner`.
  - The Dialect row's title and body become "Dialect" and "Notes on accents and phrasing. (placeholder)" (R9). Before the loop, `DestroyChildIfPresent(windowLayer, "IconDialectWindow")`, as at 145 for `IconScannerWindow`, so the rebuilt scene gets the new copy (`BuildOSWindow` keeps an existing window's texts).
  - For each non-empty id that `library.GetUpgradeById` does not know: `Debug.LogError("[TimeDesk] Desktop icon '{name}' requires unknown upgrade '{id}' (not in the content library's upgrades); it could never unlock.")`. With a null `library` the check is skipped; the warning at 231 already names the missing library.
- **The final log line** mentions the interview transcript.
- **After the change:** the scene is rebuilt in the worktree's Unity and `OfficeScene.unity` is committed.

### 2.13 Content pipeline

**`world_source.json`** (LF, hand-maintained from now on; the scratchpad `build_world_source.py` is retired, X10):

- `content.books` gains `Assets/Data/Investigation/RefBook_Capital.asset` and `RefBook_Ruler.asset`.
- New `interview` object, with the `InterviewLines` field names: the wording as plain strings (`deskName`, `opener`, `openerLegendary`, `claim`, `honorificMale`, `honorificFemale`, `honorificUnknown`, `requestLabel`, `requestPrompt`, `requestReply`, `askLabel`, `backLabel`, `smallTalkLabel`, `smallTalkPrompt`) and the two limits as ints (`menuCapacity`, `maxLineChars`). Generate World writes `menuCapacity` into `InterviewLines`; `maxLineChars` stays in the source for its own line-length check.
- New `questions[]`: `{ id, category, label, prompt, answer, fromDay, announce, conditions[], overrides[] }`. `fromDay` is required (R27). Each condition is `{ type, key, threshold }`; each override is `{ era, prompt, answer }`.
- New `dialogs[]`: `{ id, label, repeatable, conditions[], nodes[] }`. `repeatable` is optional and false when missing, so a dialog is one-shot unless it says otherwise (R27). Each node is `{ id, lines[], choices[] }`, each line `{ id, speaker, text }`, and each choice `{ id, label, lines[], next, effect }`.
- A missing array or string anywhere reads as empty.
- Optional `eras[].smallTalk` and `places[].smallTalk` (string arrays).
- `days[].channels`: `["Papers"]`, `["Papers", "Answer"]`, `["Papers", "Answer"]`.

**Id grammar (Q16).** Only the generator composes these:

| What | Id |
|---|---|
| interview line | `interview.{field}` (`opener`, `openerLegendary`, `claim`, `requestPrompt`, `requestReply`, `smallTalkPrompt`) |
| question lines | `{questionId}.prompt`, `{questionId}.answer` |
| override lines | `{questionId}.{eraId}.prompt`, `{questionId}.{eraId}.answer` |
| authored line | its explicit JSON `id`, which must start with `{dialogId}.` |
| authored choice label | `{dialogId}.{choiceId}` |
| small talk | `{placeId}.smalltalk.{i}`, `{eraId}.smalltalk.{i}` |
| per-case lines (runtime, reserved) | `case.intro`, `case.claim` |

Every id in this table, generated or authored, goes into one set; a repeat is a generator error naming both owners, for example "Line id 'dlg_rumour.more' is used by dialog 'dlg_rumour' line 'start' and by choice 'more' of dialog 'dlg_rumour'". The set starts with the two reserved runtime ids.

**`WorldContentGenerator`** (LF):
- `OwnedFolders` gains `"Interview"`.
- The source classes gain:
  - `WorldSource.interview`, `questions` and `dialogs`;
  - `EraData.smallTalk` and `PlaceData.smallTalk`;
  - `DayData.channels`;
  - `InterviewData`, `QuestionData`, `OverrideData`, `ConditionData`, `DialogData`, `NodeData`, `LineData`, `ChoiceData`, with flat arrays and string ids.
- **Checks** (a new `CheckInterview`, called from `Generate` before anything is written):
  - **interview:** every text field is non-blank; `opener` holds `{honorific}`; `openerLegendary` holds `{name}`; `claim` holds `{place}`; `requestLabel` and `requestPrompt` hold `{document}`; `menuCapacity` and `maxLineChars` are at least 1 (a missing int reads 0 and is rejected).
  - **questions:**
    - ids are unique and non-blank;
    - the category parses, and `Forgery.IsProvableCategory` holds against the loaded books' categories (R20);
    - one question per category;
    - label and prompt are non-blank, and the answer holds `{value}`;
    - `fromDay ≥ 1` (so a missing `fromDay` is an error, R27); `announce` is non-blank exactly when the question is gated: `fromDay > 1` or any authored condition (R5).
  - **conditions (questions and dialogs):**
    - the type parses;
    - types that need a profile, attribute or nation are rejected with a message naming piece 5;
    - `DayAtLeast` is rejected inside a question (use `fromDay`, R20);
    - an `UpgradeOwned` key must satisfy `authored.library.GetUpgradeById`;
    - a flag key is non-blank.
  - **overrides:** the era is a known era id, unique per question, and the answer holds `{value}`.
  - **dialogs:**
    - ids are unique;
    - the label is non-blank;
    - speakers parse (`Desk`, `Traveller`);
    - line ids are non-blank and start with `{dialogId}.`;
    - every effect satisfies `authored.library.GetEffectByAssetName`, and no op of that effect `EffectOps.ActsWhileActive`: "Dialog '{id}' choice '{choice}' names effect '{name}', whose {op} op would already act this evening and in a replay of the day; dialog effects may only hold instant ops and briefing/news lines (timed modifiers from dialogs are piece 4/5 work)" (R24);
    - `DialogChecks.Problems(dialog, menuCapacity)` on the `AuthoredDialog` built from the JSON is empty.
  - **ids:** the global id set above has no repeat (R14).
  - **menus:** `DialogChecks.MenuProblems(questions.Length, any era or place has small talk, the most document templates of the wired blueprint (`content.blueprint`) and of every listed legendary's blueprint override, dialogs.Length, menuCapacity)` is empty (R25).
  - **line length:** for every line the transcript can show, `Interview.WorstCaseLength` is at most `maxLineChars` (R10). Each token is filled with the longest value it can take: `{value}` with the longest fact value of the question's category across all places (for BirthDate, the longest registered date the places' birth years give, about 15 characters), `{place}` with the longest place label (`OriginLabels.Format` of a place and its era), `{name}` with the longest legendary display name in the library, `{honorific}` with the longest honorific, `{document}` with the longest document template display name. The lines are the opener, legendary opener, claim, request prompt and reply, small-talk prompt, every question prompt and answer (and override), every authored line and choice label (the desk speaks the label), and every small-talk line. The error names the id and both lengths.
  - **small talk:** no blank line.
  - **days:** `channels` is non-empty and every entry parses as `TellChannel`.
  - **ASCII:** all authored interview, question, dialog and small-talk text is ASCII (R18). The error names the id.
- **Builders:**
  - `MakeEra`/`MakePlace` write `smallTalk` with generated ids;
  - `MakeDay` writes `tellChannels`; its doc (260-263) becomes "Writes the day's queue, tell count, tell channels, eras, countries and rules. ...";
  - new `MakeQuestion` writes `Interview/Question_{id}.asset`. Its `conditions` are `DayAtLeast fromDay` when `fromDay > 1`, plus the authored ones;
  - new `MakeDialog` writes `Interview/Dialog_{id}.asset`, with `oneShot = !repeatable`;
  - new `MakeUnlockTrigger`, for every gated question (`fromDay > 1` or any authored condition, R5), writes `Interview/Trigger_Unlock_{questionId}.asset`: `id = unlock_{questionId}`, `displayName = "Unlock: {label}"`, `oneShot`, `newsLineOnFire = announce`, conditions `[DayAtLeast Gates.UnlockNight(fromDay)]` when `fromDay > 1`, plus the authored ones, no outcomes.
- **`WireLibrary`:**
  - sets `interview` through `SerializedProperty.boxedValue`;
  - sets `questions` and `dialogs` (authoritative);
  - sets `timelineTriggers` = the existing non-null entries outside `Assets/Data/World/Interview/`, in order, then the generated unlock triggers. The hand-authored triggers survive, as `FEATURES.md:95` promises.
- The class doc says "reads the hand-maintained world source" and lists the interview.

**`ContentLibraryValidator`** (CRLF) adds these checks:
- null entries and duplicate ids for `Questions` (`q.question.id`) and `Dialogs` (`d.dialog.id`);
- the interview lines are non-blank, and `menuCapacity` is at least 1 (the asset holds no line-length limit: Generate World checks line lengths against the source's `interview.maxLineChars`);
- every question's category satisfies `Forgery.IsProvableCategory(category, lib.ReferenceBookCategories())`, else an error "answers in this category can never be proven";
- one question per category;
- answer templates hold `{value}`;
- `DialogChecks.Problems(d, menuCapacity)` for every dialog, and `DialogChecks.MenuProblems` with the day plans' blueprints (possible and forced) and the legendaries' overrides;
- dialog effects resolve, and none holds an op that `EffectOps.ActsWhileActive` (error, same message as the generator's, R24);
- a dialog effect that is permanent (`defaultDurationDays < 0`) and carries a `BriefingLine`/`NewsLine` op gets a warning ("would repeat every morning");
- every upgrade-id string resolves through `lib.GetUpgradeById`: `UpgradeOwned` keys in questions, dialogs and triggers, and `UnlockUpgrade`/non-empty `ShopDiscountPercent` string params in effects;
- every day plan has at least one tell channel;
- every era used by a day plan has small talk, or all of its places do (warning).

`RequiredFacts`' doc (95) says "papers + books + questions". `CheckPlaces`' missing-fact error (114) ends "(papers and answers would use a placeholder)" instead of "(papers would print a placeholder)".

### 2.14 Content

**Hand-authored assets** (YAML with `.meta`, like the other authored content):

| Asset | Values |
|---|---|
| `Assets/Data/Investigation/RefBook_Capital.asset` | `displayName: Capitals Gazetteer`, `category: 5` (Geography) |
| `Assets/Data/Investigation/RefBook_Ruler.asset` | `displayName: Rulers & Regents`, `category: 2` (Politics) |
| `Assets/Data/Upgrades/Upgrade_InterviewProtocols.asset` | `id: interview_protocols`, `displayName: Interview Protocols`, `description: Clearance to ask travellers when they were born.` (authored like the other upgrades' descriptions; the shop does not show descriptions, §1.3), `cost: 120`, no unlock effect |
| `Assets/Data/Effects/Effect_Dialog_RumourHeard.asset` | `displayName: Rumour: calculators`, channel General, `defaultDurationDays: 1`, ops `SetFlag rumour_calculators` and `BriefingLine "A traveller's tip: someone is smuggling pocket calculators into the past. Keep your eyes open."` |
| `Assets/Data/Content Library/ContentLibrary_Main.asset` | `upgrades` + Interview Protocols; `effects` + Effect_Dialog_RumourHeard. Generate World writes the books, questions, dialogs, interview and triggers. |

**`interview`:**

| Field | Text |
|---|---|
| deskName | DESK |
| opener | Next! Step forward, {honorific}. |
| openerLegendary | Priority arrival: {name}. |
| claim | I request passage home to {place}. |
| honorificMale | sir |
| honorificFemale | madam |
| honorificUnknown | traveller |
| requestLabel | Request {document} |
| requestPrompt | Your {document}, please. |
| requestReply | Here you are. |
| askLabel | Ask about home > |
| backLabel | < Back |
| smallTalkLabel | Small talk |
| smallTalkPrompt | How is life back home? |
| menuCapacity | 8 (the intercom's fit, R25) |
| maxLineChars | 100 (two lines at 12 pt hold about 120, R10) |

**`questions`**, in this order. The prompts are as in §1.3; the labels, answers and extras are below. Every entry authors `fromDay` explicitly (R27).

| id | label | answer | fromDay | conditions / overrides / announce |
|---|---|---|---|---|
| `q_currency` | Currency | We pay in {value}. | 1 | override `ancient`: "What do you trade with at home?" / "We trade with {value}." |
| `q_language` | Language | At home we speak {value}. | 1 | — |
| `q_device` | Device | Every day I use {value}. | 1 | — |
| `q_capital` | Capital | Our capital is {value}. | 2 | announce "BUREAU NOTICE: desk officers may now ask travellers for their capital. Check answers against the Capitals Gazetteer." |
| `q_ruler` | Ruler | We are ruled by {value}. | 3 | announce "BUREAU NOTICE: desk officers may now ask travellers who rules them. Check answers against Rulers & Regents." |
| `q_born` | Date of birth | I was born on {value}. | 1 | condition `UpgradeOwned interview_protocols` (hint-only, R23); announce "BUREAU NOTICE: with Interview Protocols, desk officers may now ask travellers when they were born. Check answers against Citizen Records." |

Worst-case rendered lengths (the generator's check, R10): the longest answer is Language's at 46 characters ("At home we speak " + a 28-character value + "."); the longest small-talk line is 59; the claim reaches 70; the longest dialog line is 92. All are within 100.

**`dialogs`:**

Both dialogs are one-shot (no `repeatable` key).

- `dlg_rumour`: label "Any news from home? >", conditions `DayAtLeast 2`.
  - Node `start`. Traveller (`dlg_rumour.start.1`): "News? Only a rumour: someone sells pocket calculators to the ancients." Choices:
    - `more`, "Tell me more.", next `detail`;
    - `ignore`, "Not my business.", ends.
  - Node `detail`. Traveller (`dlg_rumour.detail.1`): "They say a courier from your own century carries them. Watch for anyone who counts too fast." Choice: `noted`, "Noted. Thank you.", ends, effect `Effect_Dialog_RumourHeard`.
- `dlg_rumour_followup`: label "About those calculators... >", conditions `FlagSet rumour_calculators`.
  - Node `start`. Traveller (`dlg_rumour_followup.start.1`): "Calculators? I saw nothing. Nothing at all." Choice: `bye`, "Of course. Next!", ends.

**Small talk.** Two lines for each era with travellers, plus one own line for each day-1 place:

| Owner | Lines |
|---|---|
| ancient | "The harvest was good this year, thanks be to the gods."; "The roads are safe, as long as you travel by day." |
| medieval | "The bells rang all week for the feast."; "Taxes again. Always taxes." |
| earlymodern | "Every week a new pamphlet, every month a new war."; "The post rides faster than ever." |
| industrial | "The smoke never lifts over the mills."; "Everyone talks about the railway." |
| modern | "The news never stops, day or night."; "Everything is on the radio now." |
| egypt_ancient | "The Nile rose right on time; the fields are black and rich." |
| iraq_ancient | "The scribes are busy; every jar of barley gets its tablet." |
| greece_ancient | "Everyone argues in the agora, and nobody agrees." |
| italy_ancient | "The Senate talks, the legions march." |

### 2.15 Knobs

| Knob | Where | Value | Authored in |
|---|---|---|---|
| Tell channels per day | `DayPlanSO.tellChannels` | day 1 Papers; days 2–3 Papers, Answer | `world_source.json` `days[].channels` |
| Tell count | `DayPlanSO.tellCount` (piece 2) | 1 | `days[].tells` |
| Question unlocks (and whether a question can carry a tell) | `QuestionSO.conditions` (+ unlock trigger) | §1.3 | `questions[].fromDay`/`conditions` |
| Question and interview wording | `QuestionSO.question`, `ContentLibrarySO.interview` | §2.14 | `questions[]`, `interview` |
| Intercom menu capacity | `InterviewLines.menuCapacity` | 8 | `interview.menuCapacity` (the builder checks the intercom fits it) |
| Longest transcript line | `world_source.json` only (Generate World's line-length check reads it) | 100 | `interview.maxLineChars` |
| Dialog availability, one-shot, consequences | `DialogSO` | §2.14 | `dialogs[]` |
| Small talk | `EraSO.smallTalk`, `NationEraProfileSO.smallTalk` | §2.14 | `eras[]`/`places[].smallTalk` |
| Transcript rows per page | `TranscriptWindowController.entriesPerPage` | 8 | builder |
| Interview Protocols price | `Upgrade_InterviewProtocols.cost` | 120 | inspector |
| Rumour consequence | `Effect_Dialog_RumourHeard` | flag + 1-day briefing line | inspector |

### 2.16 Copy

| Where | Text |
|---|---|
| `ShiftScoring.cs:142` | "Deviation denied without documented evidence. Log a deviation from the papers or the traveller's answers before denying." |
| `InvestigationUIController.RefreshScannerText` | the §1.7 hint |
| `Discrepancy.Summary` | §2.6 |
| `CompareController.ShowAlreadyDocumented` | §2.11 |
| `CaseFactory` `NoPossibleLie` warning and `ResolveFieldValue` placeholder warning | §2.9 |
| `InvestigationUIController` interview wiring warning | §2.11 |
| `GameManager` dialog content-problem error | §2.10 |
| builder unknown-upgrade and intercom-capacity errors; Dialect window title and body | §2.12 |
| generator and validator messages (including `CheckPlaces`' placeholder note) | §2.13 |
| announce lines, the claim sentence | §2.14 |

### 2.17 Every file that changes

| File | Assembly | Change |
|---|---|---|
| `Assets/Scripts/Domain/Gates.cs` (+meta) | Domain | new: `TriggerConditionType` (moved, + `UpgradeOwned`), `GateCondition`, `GateSnapshot`, `Gates` (incl. `DayOnly`), `Gated<T>`, `FlagKeys` |
| `Assets/Scripts/Domain/EffectOps.cs` (+meta) | Domain | new: `EffectOpType` (moved unchanged), `EffectOps.ActsWhileActive` |
| `Assets/Scripts/Domain/Dialog.cs` (+meta) | Domain | new: runtime graph and runner |
| `Assets/Scripts/Domain/InterviewContent.cs` (+meta) | Domain | new: serializable content types |
| `Assets/Scripts/Domain/Interview.cs` (+meta) | Domain | new: `InterviewAnswer`, `Interview` (`Answer`, `Opener`, `Claim`, `PickSmallTalk`, `WorstCaseLength`, `Fill`, `Placeholder`) |
| `Assets/Scripts/Domain/InterviewScript.cs` (+meta) | Domain | new: `InterviewCase`, `InterviewScript`, `DialogChecks` (`Problems`, `MenuProblems`) |
| `Assets/Scripts/Domain/InterviewDay.cs` (+meta) | Domain | new: `InterviewDay`, `DialogOutcomes` |
| `Assets/Scripts/Domain/ClueLabels.cs` (+meta) | Domain | new |
| `Assets/Scripts/Domain/Lies.cs` | Domain | `TellChannel`; `Plan` options (`answerTellCategories`); `LiePlan.ChannelOf`/`TellValue`; `ApplyTo` Papers only; docs, including `LieOutcome.Liar` (34-35), the `Lies`/`LiePlan` summaries, `Plan`'s summary (109-119) and `LiePlan._values` (47) |
| `Assets/Scripts/Domain/DiscrepancyLog.cs` | Domain | `EvidenceKind.Answer`, `ForAnswer`, `source`, `Summary`, `Prove`/`Add`, `TryRegister` removed; docs, including the `DiscrepancyProof` values (23-30), `Discrepancy.actualOrigin` (107-110) and the class doc |
| `Assets/Scripts/Domain/Seeds.cs` | Domain | `DialogSalt`, `ForDialog` |
| `Assets/Scripts/Domain/ShiftLedger.cs` | Domain | `DialogOutcome`, `dialogOutcomes` |
| `Assets/Scripts/Domain/Forgery.cs` | Domain | `IsProvableCategory` extracted and called by `IsProvableTell`; class doc |
| `Assets/Scripts/Dialog.meta`, `Dialog/QuestionSO.cs`, `Dialog/DialogSO.cs` (+metas) | Assembly-CSharp | new |
| `Assets/Scripts/EffectSO.cs` | Assembly-CSharp | `EffectOpType` moved out |
| `Assets/Scripts/Timeline/TimelineTriggerSO.cs` | Assembly-CSharp | enum moved out; `FiredFlag` via `FlagKeys`; `TriggerCondition.key` doc (84); class doc |
| `Assets/Scripts/Timeline/TimelineService.cs` | Assembly-CSharp | `ToGate`, `ToGates`, `Snapshot`; `AllConditionsPass` via `Gates` (`TimelineKeys` unchanged) |
| `Assets/Scripts/ContentLibrarySO.cs` | Assembly-CSharp | `interview`, `questions`, `dialogs`; doc |
| `Assets/Scripts/DayPlanSO.cs` | Assembly-CSharp | `tellChannels`/`TellChannels`; doc |
| `Assets/Scripts/EraSO.cs`, `Assets/Scripts/Timeline/NationEraProfileSO.cs` | Assembly-CSharp | `smallTalk` |
| `Assets/Scripts/CaseInstance.cs` | Assembly-CSharp | `answers`, `smallTalk`; `introLine`, `claimLine` and `IsLiar` (73) docs |
| `Assets/Scripts/CaseFactory.cs` | Assembly-CSharp | §2.9, including the `Disguise` summary (281-287) and the `ResolveFieldValue` summary and warning (336-360) |
| `Assets/Scripts/GameManager.cs` | Assembly-CSharp | §2.10 |
| `Assets/Scripts/Shift/ShiftScoring.cs` | Assembly-CSharp | citation text (142) |
| `Assets/Scripts/UI/PagedRowsWindow.cs` (+meta) | Assembly-CSharp | new |
| `Assets/Scripts/UI/TranscriptWindowController.cs` (+meta) | Assembly-CSharp | new |
| `Assets/Scripts/UI/ReferenceBookWindowController.cs` | Assembly-CSharp | derives from `PagedRowsWindow` |
| `Assets/Scripts/UI/InvestigationUIController.cs` | Assembly-CSharp | §2.11, including the `BuildFallbackBody` summary (405-408) |
| `Assets/Scripts/UI/CompareController.cs` | Assembly-CSharp | `ShowAlreadyDocumented`; class doc (5-11) |
| `Assets/Scripts/UI/InteractionPanelController.cs` | Assembly-CSharp | doc |
| `Assets/Scripts/UpgradeSO.cs`, `Assets/Scripts/WorldState.cs` | Assembly-CSharp | docs |
| `Assets/Editor/WorldContentGenerator.cs` | Editor | §2.13, including the `MakeDay` doc (260-263) |
| `Assets/Editor/ContentLibraryValidator.cs` | Editor | §2.13, including the `RequiredFacts` doc (95) and the `CheckPlaces` message (114) |
| `Assets/Editor/OfficeSceneUIBuilder.cs` | Editor | §2.12 |
| `Assets/Data/World/world_source.json` | content | §2.13–2.14 |
| `Assets/Data/Investigation/RefBook_Capital.asset`, `RefBook_Ruler.asset` (+metas) | content | new |
| `Assets/Data/Upgrades/Upgrade_InterviewProtocols.asset` (+meta) | content | new |
| `Assets/Data/Effects/Effect_Dialog_RumourHeard.asset` (+meta) | content | new |
| `Assets/Data/Content Library/ContentLibrary_Main.asset` | content | upgrades/effects by hand; the rest by Generate World |
| `Assets/Data/World/Interview/` (folder, 6 questions, 2 dialogs, 3 unlock triggers, metas) | generated | new |
| `Assets/Data/World/Eras/Era_*.asset`, `Places/Place_*.asset` | generated | `smallTalk` (every place gains the field) |
| `Assets/Data/Investigation/DayPlan_Inv_Day1..3.asset` | generated | `tellChannels` |
| `Assets/Scenes/OfficeScene.unity` | scene | rebuilt by the builder |
| `Assets/Tests/EditMode/{Gates,EffectOps,DialogRunner,InterviewScript,Interview,InterviewDay}Tests.cs` (+metas) | tests | new, §5 |
| `Assets/Tests/EditMode/{Lies,DiscrepancyLog,Seeds,Forgery,FactTable}Tests.cs` | tests | §5 (`DiscrepancyLogTests`' header "Decision table for DiscrepancyLog.TryRegister" becomes "Decision table for DiscrepancyLog.Prove and Add") |
| `docs/FEATURES.md` | docs | §3.3 |
| `docs/superpowers/specs/2026-09-24-dialog-questions-design.md` | docs | this spec, committed first |

`SCRATCH/build_world_source.py` is retired: not run again and not committed. The scratchpad `HOUSE_RULES.md` (line 16) already says so: the JSON is the hand-maintained source and is never regenerated from the script.

The following were checked and do not change: `DocumentWindowController`, `CitizenRecordsWindowController`, `DesktopIcon`, `OSWindowChrome`, `OfficeUIController`, `DayFlowUIController`, `SaveSystem`, `RunManager`, `HomeManager`, `HomeUIController` (the shop still shows no descriptions, §1.3), `TimelineEffects`, `TimelineCueReceiver`, `ShiftClock`, `VerdictRules`, `FactTable`, `BirthDates`, `DocumentField`, `CitizenRegistry`, `WorldState.ActiveEffectEntry.IsActiveOnDay` (R24).

### 2.18 Why some logic stays outside Domain

The glue is:
- `CaseFactory` (`AddAnswers`; the calls to `Interview.Opener`, `Claim` and `PickSmallTalk`, which only resolve the ScriptableObject lists; `ResolveFieldValue`, which reads the `FactTable` through SO ids and logs);
- `GameManager` (`BuildInterviewDay`: condition projection, the snapshot copy and the error log; `ApplyDialogOutcomes`: `SetFlag`, effect lookup and `ActivateEffect`);
- `TimelineService` (`ToGate`, `ToGates`, `Snapshot`: they read `TriggerCondition`'s SO references and `WorldState`);
- the UI controllers;
- the generator, the validator and the builder.

It reads ScriptableObjects or `WorldState`, calls `ActivateEffect`, `EndingService` and `UpdateHud`, renders UI, or runs in the editor. Every rule it applies is a call into tested Domain code: `Gates` (including `DayOnly`), `FlagKeys`, `EffectOps.ActsWhileActive`, `Forgery.IsProvableCategory`, `Lies.Plan`, `LiePlan.ChannelOf`/`TellValue`/`ApplyTo`, `Interview.*` (the answer, opener, claim, small-talk and line-length rules), `InterviewScript.*`, `DialogChecks`, `DialogRunner`, `InterviewDay` (which questions are askable, which carry tells, which dialogs are offered: conditions, sound structure, one-shot memory), `DialogOutcomes` (which done flags to set and which effects to apply, once each), `DiscrepancyLog.Prove`/`Add` and `ClueLabels`. What stays in glue is arithmetic on SO data (`startDay = day + 1`) and lookups. The whole-day behaviour of the glue is proven by the Unity checks in §6.

### 2.19 Intent audit: the existing mechanism tried for each new one

| New | Existing mechanism reused or tried | Why something new was still needed |
|---|---|---|
| Dialog graph and runner | `InteractionPanelController` (reused as the choice renderer); Ink/Yarn (rejected, Q1) | No dialog system exists; `InteractionAction` stays as it is |
| Transcript window | the Clue Log placeholder slot, `BuildWindowShell`, the book row/paging code (extracted to `PagedRowsWindow`), `CompareController`, `OSWindowChrome` | A per-traveller transcript source is new |
| `Gates` evaluator | `TriggerCondition` and `TimelineService.AllConditionsPass` | Moved into Domain and shared with triggers (X3), not duplicated |
| Unlock announcements | one-shot `TimelineTriggerSO` + `newsLineOnFire` | none (generated instances only, one per gated question) |
| Narrative consequences | `EffectSO` + `ActivateEffect`; flags; the fired-flag grammar (moved with the new done flag into Domain `FlagKeys`) | Only the end-of-shift timing is new (X5), and the op restriction it needs (R24) |
| Op classification | the Instant/Continuous grouping comments in `EffectOpType` | The grouping existed only as comments; `ActsWhileActive` makes it a tested rule (the enum moves to Domain, as `TriggerConditionType` does) |
| Menu capacity | the builder's own intercom layout numbers | A content knob the generator and validator can read, checked against the layout by the builder (R25) |
| Provable question categories | `Forgery.IsProvableTell`'s category switch | Extracted as `IsProvableCategory`, not copied (R20) |
| Answer evidence | the `DiscrepancyLog` proof branches, `CompareEvidence`, `HandlePairCompared`, the deny gate | One enum value and the statement-side check |
| Labels | the private `Discrepancy.CategoryLabel` | Made public and complete (R13) |
| Answers | `CaseFactory.ResolveFieldValue`, `LiePlan` values | none |
| Small talk pick | `Seeds`/`SeededRandom` | A salted stream, following the `ForClues`/`ForLies` precedent |
| Upgrade gate | `WorldState.HasUpgrade`, `UpgradeSO.id`, `ContentLibrarySO.GetUpgradeById` | One enum value |
| Honorific | `TravellerGender` (piece 2), `introLine` | none |
| Timeline-reactive talk | `EffectChannel.Chatter` + `TimelineEffects.GetCues` | deferred (R16) |

## 3. Retired or superseded

### 3.1 Code removed or replaced

- `TriggerConditionType` in `TimelineTriggerSO.cs:9-40` moves to Domain `Gates.cs`, and `AllConditionsPass`'s switch (`TimelineService.cs:332-364`) is replaced by `Gates`.
- `TimelineTriggerSO.FiredFlag`'s inline format (74) moves to `FlagKeys.TriggerFired`.
- `EffectOpType` in `EffectSO.cs:30-52` moves to Domain `EffectOps.cs`.
- `Discrepancy.CategoryLabel` (`DiscrepancyLog.cs:134-136`) is removed.
- `DiscrepancyLog.TryRegister` (162-259) is removed: its proof rules move into `Prove` and its dedupe loop (252-255) into `Add` (R17).
- The category switch at the top of `Forgery.IsProvableTell` (24-33) becomes `IsProvableCategory`.
- `ReferenceBookWindowController`'s serialized fields, `Awake`, `PageCount`, `ShowPage` and `Rebuild` (15-39, 59-124) move to `PagedRowsWindow`.
- The per-document `InteractionAction` loop (`InvestigationUIController.cs:240-274`) is replaced by the interview hub.
- The fallback's full book listing (`InvestigationUIController.cs:439-451`) is removed.
- The builder's Clue Log placeholder window and its `apps` row (`OfficeSceneUIBuilder.cs:1280`) are removed.
- The literal intro "Next subject for reassignment." (`CaseFactory.cs:187`) and the claim template "I request passage home to {originLabel}." (`CaseFactory.cs:224`) become content (R8, R26).

### 3.2 Earlier spec lines superseded (kept as approved records, not edited)

**`docs/superpowers/specs/2026-09-24-identity-lies-design.md` (piece 2):**
- **R3 (line 39):** "A category can be a tell only if the traveller's papers print it". Now a category is a Papers tell only if printed, or an Answer tell only if its question is askable that day and gated by day alone (X1, R1, R23).
- **§1.1 line 67:** "Names, capitals (Geography) and rulers (Politics) are never tells." Capitals and rulers can be Answer tells from the day their question is askable. Names are still never tells.
- **§1.1 lines 64-66 and §1.2 lines 84-87:** tells "leak" and are logged from the papers only. They may now be spoken, and they are logged from answers the same way (§1.4).
- **§1.3 line 98 and R12 (line 48):** the citation text and the scanner idle hint are replaced by §1.7.
- **§1.4 lines 108-110:** days 2–3 lie draws differ from piece 2's (their day-gated questions add Answer options), and answers also depend on the day's askable questions; lie draws still never depend on purchases or flags (§1.8, R23).
- **§2.3 lines 213-239:** the `LiePlan.ApplyTo` comment (213), the `Lies.Plan` signature, candidate step 2, tell step 4 and "`ApplyTo` sets … every field whose category is a tell" are replaced by §2.3 of this spec (options, channels; `ApplyTo` covers Papers tells only).
- **R15 (line 51) and §2.7 line 390:** the fallback prints only the claimed place's entry per book, plus the interview (R11).
- **§3.3 lines 492 and 495:** the piece-2 FEATURES rewrites of :62 and :85 (now :63 and :86) are rewritten again (§3.3 below).
- **§5 line 533** ("an unprinted category is never a tell"): the test stays, renamed `AnUnprintedCategory_IsNeverAPapersTell`, because it runs with Papers only.
- **§2.6 line 381** ("The scratchpad build script `build_world_source.py` (lines 71-79) gets the same key so a rebuild keeps it"): superseded by X10. `world_source.json` is now the hand-maintained source of truth, and the script is never run again.
- **Piece-2 plan (`docs/superpowers/plans/2026-09-24-identity-lies.md`), Task 11:** the "Modify (scratch, not committed): `SCRATCH/build_world_source.py`" file line (2787), the Step 1 `apply(S + r'\build_world_source.py', …)` block (2828-2832) and Step 2 "Prove the build script reproduces the edited source" (2840-2843), as well as the file-table row at line 64. They were right for piece 2 and are superseded from piece 3 on by X10.
- **§6 step 3.3 (line 596)** ("at least one field has `isAnachronism`"): replaced by "at least one tell on a channel allowed that day" (§6 below).
- **§7 line 620** ("Cross-piece gate trap"): resolved. Answers are registrable (`EvidenceKind.Answer`) before any Answer tell is generated, and R12 switches the Answer channel off where answers cannot be seen.
- **§7 line 623** ("Gender is data only"): gender is now read by the opener (X9).
- **§4 line 502:** the piece-3 list is delivered here. Its "capital and ruler question books" are `RefBook_Capital` and `RefBook_Ruler`.

**`docs/superpowers/specs/2026-07-03-scanner-evidence-design.md` (approved by Saleh):**
- **Rule 1 (lines 14-19):** "Comparing a document field against …" now reads "comparing a *statement* (a document field or a traveller's answer) against …". Saleh's quote asks for a failed question to catch a liar. The proof rules themselves are unchanged: same category, same claim test, same honesty gate on the statement's tell flag, and one discrepancy per category.
- **Rule 2 (lines 20-23):** the report wording names the source ("papers" or "traveller said").
- **Components (lines 37-46):** `DiscrepancyLog` gains `Prove`/`Add`; `CompareEvidence` gains the Answer kind.
- **Intent-audit argument:** the change widens the statement side of an existing mechanism instead of adding a second proof system. `evidenceCount` and the deny gate keep their meaning (MERGE_CRITERIA.md:48-51).

**`docs/superpowers/specs/2026-09-24-world-model-design.md` (piece 1):**
- §4 line 69 "Question books for capital and ruler (piece 3)" is delivered.
- §6 line 87 "capital and ruler have no books yet" is no longer true.
- §6 line 99 (the "Sultan Mustafa II" follow-up) is handled by piece-2 R13 (R3 here).
- §4 line 70 (saving the day's generation inputs) stays out of scope; its interplay with dialog consequences is described in §1.8.
- §6 line 94 "owns `Assets/Data/World/{Eras,Nations,Places,Rules}`" gains `Interview`.

### 3.3 `docs/FEATURES.md` (same commits as the behaviour)

Line numbers are those of the current `FEATURES.md`, with all of piece 2's edits in (the piece-2 review reworded :84-85 in place, so no line has moved since `d7614f0`); the new text replaces piece 2's text where both touch a line.

- **:11:** add "a per-traveller dialog stream (`Seeds.ForDialog`, small-talk pick); the day's lie draws depend only on the run and the day (and the interview wiring), never on purchases or flags" (seeding tested: `SeedsTests`; tell eligibility tested: `InterviewDayTests`).
- **:12:** "Endings evaluated after every verdict, and at the end of a shift that applied dialog consequences".
- **:31:** "Upgrade-gated icons (Lexicon needs Archive Access, Material needs Advanced Scanner) dim until the upgrade is owned; the builder reports icon upgrade ids the library does not know".
- **:33:** "Placeholder apps: Internet, Lexicon, Dialect, Material, Notes".
- **New bullet after :33:** "Clue Log = Case Notes: Interview, the current traveller's transcript (speaker + line, 8 per page, long lines wrap onto two, jumps to the newest page); answer rows are compare-clickable; any question, small talk or dialog choice opens it; it closes with every new case".
- **:36** (Citizen Records bullet, piece-2 text): append "; likewise the rich desk warns once at start when the intercom, the interview transcript or its window chrome is not wired: that day questions are hidden, no answer is computed and no tell is spoken, and the intercom offers only document requests".
- **:44** (piece-2 liar bullet): the tells sentence becomes "… the disguise leaks tells carrying the true home's value, on the papers (a Currency, Language or Technology tell rewrites every field of that category; a birth-date tell keeps the record's day and month with a year from the true home's birth years) or in speech (their answer to one of today's day-gated questions gives the true home's value while the papers show the cover). A category leaks on one channel only. The day plan's tell channels set where tells may appear (day 1 papers only; days 2–3 papers and answers); a spoken tell needs its question askable that day, gated by day alone, and a book (or the Citizen Record for a birth date)". Its test claim becomes "(tell selection, channels, eligibility and the may-lie exemptions tested: `LiesTests`, `ForgeryTests`, `InterviewDayTests`; answers tested: `InterviewTests`; the liar chance, cover records and whole-day behaviour are checked in Unity, not by the EditMode suite)".
- **New bullets under "Investigation loop":**
  - "Intercom = the interview: every entry is a dialog choice. Hub: 'Request <document>' (repeatable; the traveller hands it over and the window opens), 'Ask about home >' ('< Back' first, then today's questions, one-shot per traveller, and small talk) and today's narrative dialogs; content never offers more choices than the intercom shows (8), and the way back and the requests come first (hub and menus tested: `InterviewScriptTests`, `DialogRunnerTests`; locked questions hidden and dialogs offered tested: `InterviewDayTests`; capacity rules tested: `InterviewScriptTests`)". This replaces :50.
  - "Questions: Currency, Language, Device from day 1; Capital from day 2; Ruler from day 3 (each announced in that morning's paper); Date of birth once Interview Protocols is owned (announced the next morning). Unlocks are fixed at the start of the day. Only day-gated questions can carry a spoken tell: the birth-date question is always answered with the registered date, a hint against a passport birth-date tell (gates tested: `GatesTests`; availability and tell eligibility tested: `InterviewDayTests`)".
  - "Honest answers equal the claimed place's book entries and the Citizen Record; a liar answers with the cover except for a spoken tell; small talk comes from the claimed place (or its era) and is never evidence (answers tested: `InterviewTests`)".
  - "Reference books: Currency Ledger, Tongues & Scripts, Index of Devices, Capitals Gazetteer, Rulers & Regents".
- **:55-61 (Scanner):**
  - "true contradictions auto-register from a document field or a traveller's answer";
  - the proofs say "a liar's tell (printed or spoken)";
  - junk adds "answer vs papers, answer vs answer";
  - "One discrepancy per category from any source; a second proof of a documented category shows 'ALREADY DOCUMENTED'";
  - new: "Reports say where the tell was ('papers show …' / 'traveller said …') and use CAPITAL, RULER, DEVICE, BIRTH DATE" (tested: `DiscrepancyLogTests`).
- **:63:** "Fallback text-mode investigation when the rich desk isn't built (papers, the traveller's agency record, their answers to today's questions, and the claimed place's entry in each book)".
- **:71:** "The clock pauses only while a citation slip is shown (the interview takes real time and costs nothing else)".
- **:86** (piece-2 text): "Only provable tells are generated … a place fact is a tell only when a reference book covers it and the true home's value differs from the claim's and belongs to no other of today's places; it shows on the papers only when a paper prints it, in speech only when its question is askable that day and gated by day alone; … names are never tells" (tested: `ForgeryTests`, `LiesTests`, `BirthDatesTests`, `InterviewDayTests`).
- **New bullet under "Scoring & consequences":** "Narrative dialogs may carry a consequence: an effect limited to instant ops (flags, counters, money, stability, upgrades, scores) and briefing/news lines; the generator and validator reject timed modifiers (pay, liar, legendary, shop, visitor and blueprint bonuses, cues) in a dialog's effect. A dialog with a consequence is one-shot per run. Consequences apply at the end of the shift, before the save: instant ops at once (the HUD refreshes and endings are checked), briefing and news lines in the next morning's paper. A dialog asset whose structure is broken (a node that cannot reach an ending, an effect on a non-ending choice, too many choices) is never offered and an error is logged (recording, one-shot memory and apply-once tested: `InterviewDayTests`; structure tested: `InterviewScriptTests`; the op rule tested: `EffectOpsTests`; the effect application is Assembly-CSharp and checked in Unity)".
- **:95:** add "questions (with an unlock-announcement trigger for every gated question), dialogs, interview wording and menu capacity (the longest-line limit stays in the source), small talk and each day's tell channels; checks every id is unique, authored text is ASCII, no line is longer than the transcript holds, no menu is fuller than the intercom shows, and no dialog effect carries a timed modifier; also owns `Assets/Data/World/Interview`, and rewires the library's triggers (hand-authored triggers kept, generated unlock triggers appended); `world_source.json` is hand-maintained (the scratchpad build script is retired)".
- **:96:** add "questions (a book or the record proves every question's category; one per category), dialogs (structure, reachable endings, effects and their op types, one-shot), menu capacity, upgrade ids, tell channels, small talk".
- **:97** (Build Office UI): append "; it reports an intercom that fits fewer choices than the content's menu capacity" (icon upgrade ids are in :31).
- **:45** (piece-2 gender bullet): "… is recorded from the claimed place's name lists; the desk's opener uses it for the honorific (sir/madam/traveller)".

## 4. Out of scope

- **Piece 4 (characters):**
  - premade and legendary conversations and multi-day arcs (on this runner);
  - portraits and expressions;
  - speech bubbles in the booth view;
  - clothing tells (a third channel on the same option pool);
  - lying legendaries;
  - repeatable dialogs with consequences (bribes), which need a replay-safe design beyond one-shot;
  - timed consequences of dialogs (bonuses, weights, discounts, cues), which need a start-day lower bound that does not break `BuildTomorrowPackage` (R24);
  - a paged or scrolling intercom menu, when arcs outgrow the 8-choice capacity (R25).
- **Piece 5 (history):**
  - history-dependent facts, which reach answers automatically through `FactTable`;
  - questions gated on scores or dominance: `world_source.json` needs profile/attribute/nation id resolution, while the evaluator already handles them;
  - timeline-reactive talk through `EffectChannel.Chatter`;
  - fixing the three authored triggers that can never fire (R22);
  - Future travellers.
- **Piece 6 (PC/UI):**
  - string tables keyed by the line ids;
  - UI language and fonts;
  - moving `ClueLabels` into tables;
  - per-traveller report lines;
  - the redirect outcome.
- **Not scheduled:**
  - a per-question time cost;
  - a ScrollRect;
  - soft or evasive non-evidence tells;
  - `DayEventSO` dialogs;
  - dialog in the legacy Test_DayLoop `OfficeUIController` path;
  - a per-channel weight knob;
  - showing upgrade descriptions in the Home shop (`HomeUIController.BuildShopRows` shows none today);
  - saving the day's generation inputs (the Continue replay gap).

## 5. Tests (EditMode, `Assets/Tests/EditMode`)

- **`GatesTests`** (new):
  - one decision-table row per condition type, pass and fail, including: a null key for each keyed type (CounterAtLeast with threshold 0 passes and FlagNotSet passes, as today; score, tier, nation and upgrade types fail); an unknown enum value fails;
  - `UpgradeOwned` reads upgrades, not flags: the flag `upgrade:x` alone fails;
  - `AllPass` is true for null or empty, false when any one fails, false with a null snapshot;
  - `GateSnapshot` copies its inputs: mutating the source lists afterwards changes nothing;
  - **R4:** a trigger condition `DayAtLeast UnlockNight(3)` passes on a snapshot with `Day = 2` (the nightly resolve of day 2); a question condition `DayAtLeast 3` fails on `Day = 2` and passes on `Day = 3` (the day-start snapshot); `UnlockNight(2) == 1`;
  - `DayOnly`: true for null, empty and all-`DayAtLeast` lists; false as soon as one condition of another type is present (`UpgradeOwned`, `FlagSet`, …);
  - `FlagKeys.TriggerFired("x") == "trig:x:fired"` (the format saves already hold) and `FlagKeys.DialogDone("x") == "dlg:x:done"`;
  - the moved `TriggerConditionType` keeps its ints: one assert per value (`CounterAtLeast == 0` … `StabilityAtMost == 9`, `UpgradeOwned == 10`).
- **`EffectOpsTests`** (new):
  - `ActsWhileActive`, one `[TestCase]` per `EffectOpType` value: true for the six continuous modifiers and `Cue`, false for the eight instant ops and for `BriefingLine`/`NewsLine`;
  - the moved `EffectOpType` keeps its ints: one assert per value (`SetFlag == 0` … `NewsLine == 16`).
- **`ForgeryTests`** (changed, LF): `IsProvableCategory`: Name false (with every book); BirthDate true with no books and with a null book set; Currency true exactly when a book covers it; a null book set gives false for every place-fact category. The existing `IsProvableTell` tests keep their assertions, which pins that the extraction changes no result.
- **`FactTableTests`** (changed, LF): the two `TryRegister` calls (63, 75) become `DiscrepancyLog.Prove` with the same assertions.
- **`LiesTests`** (changed):
  - the helper `Plan(...)` and the two direct `Lies.Plan` calls pass `answerTellCategories: none, channels: [Papers]`. Every existing test keeps its draws and assertions;
  - `AnUnprintedCategory_IsNeverATell` becomes `AnUnprintedCategory_IsNeverAPapersTell`.
  - New fixture: `AnswerFacts()` is `Facts()` plus Geography for Egypt "Thebes", the twin " thebes ", Iraq "Babylon" and Italy "Rome". `AnswerBooks` is `Books` plus Geography.
  - **Golden order, answer:** answer-tell categories [Currency, Geography], channels [Papers, Answer]. Iraq's options are P:BD, P:Cur, P:Lang, P:Tech, A:Cur, A:Geo. The script `[V0, R0, R5]` gives `Tells == [Geography]`, `ChannelOf(Geography) == Answer`, `TellValue == "Babylon"`, `ApplyTo` changes no field, and exactly 3 draws are made. `[V0, R0, R4]` gives a Currency Answer tell, with the papers' Currency fields unchanged and unflagged.
  - **One channel per category:** tell count 9 with `[V0, R0, R0, R0, R0, R0, R0, R0]` gives tells [BD, Cur, Lang, Tech, Geo] on channels P, P, P, P, A: each category once, the year drawn last, `Done` after 8 draws.
  - **Papers only reproduces piece 2:** for seeds 0..300, channels [Papers] with answer-tell categories [Currency, Geography] gives the same `Outcome`, `HomeIndex`, `Tells` and paper values as channels [Papers] with none.
  - **Answer only, no tell-carrying question:** channels [Answer] with no answer-tell categories gives `NoPossibleLie` after one draw.
  - **A home eligible only through an answer is a candidate:** a place that differs from the claim only in Geography is picked with Answer allowed, and gives `NoPossibleLie` with Papers only.
  - **Birth-date answer tell** (the generic rule, for a future day-gated birth question): answer-tell categories [BirthDate], channels [Answer] gives a BirthDate Answer tell. The year draw happens; the papers' Date of Birth stays the cover date; `TellValue` keeps the cover's day and month with a year that is not the cover's.
  - **Shared ruler:** the claim and the home share a Politics value, so Politics is not an option (R3).
  - **Round trip:** every Answer tell proves (`DiscrepancyLog.Prove`) as `ForAnswer` against the claim row (ClaimMismatch, source Answer) and the home row (ForeignOrigin naming the home), and against no third row. A BirthDate Answer tell against the record gives RecordMismatch. (The two existing round-trip calls, 323 and 330, move from `TryRegister` to `Prove`.)
- **`InterviewTests`** (new):
  - `Answer`: an honest plan (null, Honest, NoPossibleLie) gives the cover and false; an Answer tell gives the tell value and true; a Papers tell gives the cover and false; a non-tell category gives the cover;
  - `Opener`: Male, Female and Unknown get their honorific; a legendary name uses the legendary template; null lines give "";
  - `Claim`: the template filled with the label; a blank template gives the bare label;
  - `PickSmallTalk`: place lines win over era lines; era lines when the place has none; exactly one draw when there are lines (a `ScriptedRandom` with one value, then `Done`); no draw and null when both are empty; a null rng gives null;
  - `WorstCaseLength`: one and two occurrences of the token; a template without the token keeps its length; null gives 0;
  - `Fill`: every occurrence replaced; a null template gives ""; a null value inserts ""; other tokens untouched.
- **`DialogRunnerTests`** (new):
  - the opening lines come first, then the start node's lines;
  - `Choose` appends the choice's lines, moves to `Next` and appends that node's lines on every entry;
  - a null, empty or unknown `Next` stays;
  - a one-shot choice leaves `Choices`, a repeatable one stays;
  - an id not currently offered returns null and changes nothing;
  - `Choices` is a fresh list each time;
  - a duplicate node id throws.
- **`InterviewScriptTests`** (new):
  - **Hub:** the requests come in document order, with labels and lines filled with the document name, `OpenDocument` with the right index, repeatable; then `ask`; then one entry per dialog. `ask` is absent when there are no questions and no small talk.
  - **Ask:** `back` is always the first choice and returns to the hub; then the questions in the given order, labelled with `q.label`, one-shot; a question whose category has no answer is skipped; small talk comes last and appears only when a line is set.
  - **Answer lines** carry the category, canonical value and tell flag; their text is the template with the value, and the claimed era's override wins.
  - **`Opening`** gives two lines, or one when the intro is blank, with ids `case.intro` and `case.claim`.
  - **Authored dialog merged:** namespaced node and choice ids; the label is spoken by the desk; an ending choice returns to the hub with `CompleteDialog`, the dialog id and the effect name. A full walk through `dlg_rumour`-shaped data ends back at the hub.
  - **`DialogChecks.Problems`:** one case per problem (no nodes, duplicate ids, dangling `next`, unreachable node, a node without choices, no ending, a `start → a → b → a` loop whose only ending is in a branch left behind ("cannot reach an ending"), an effect on a choice with a `next`, an effect on a repeatable dialog, a node with more choices than `maxChoices`, and no capacity problem when `maxChoices` is 0) and a clean dialog with no problems.
  - **`DialogChecks.MenuProblems`:** 6 questions + small talk + back at capacity 8 passes; one more question fails; the hub count (documents + ask + dialogs) passes at 8 and fails at 9.
- **`InterviewDayTests`** (new):
  - `Questions` keeps only those whose conditions pass on the snapshot, in order, and `AskableCategories` follows them;
  - `AnswerTellCategories`: a day-gated question (no conditions, or `DayAtLeast` only) is in it; a question gated by `UpgradeOwned` or `FlagSet` is askable (when its gate passes) but not in it (R23);
  - offered dialogs: a dialog whose conditions fail is not offered; a one-shot dialog whose `dlg:{id}:done` flag is in the snapshot is not offered; a repeatable dialog with that flag set still is; a structurally broken dialog is not offered and `ContentProblems` names it, even when its conditions fail;
  - `OfferedDialogs` drops a dialog completed this shift;
  - `Complete` records `DialogOutcome` in the ledger with the dialog's `oneShot` and the effect name, and returns false for an unknown or repeated id;
  - `DialogOutcomes.FlagsToSet` gives one `dlg:{id}:done` per one-shot outcome, none for a repeatable one, once per dialog id, in ledger order; `EffectsToApply` gives each outcome with an effect once, skips outcomes without one, keeps ledger order; null gives empty.
- **`DiscrepancyLogTests`** (changed, CRLF):
  - the header becomes "Decision table for DiscrepancyLog.Prove and Add — the core verification rule". The existing tests' `TryRegister` calls (22) go through a private helper `Register(log, a, b, nation, era)`, which returns `Prove`'s proof when `log.Add` accepts it, else null, so every existing assertion stays as it is;
  - an Answer tell against the claim row gives ClaimMismatch with source Answer, and the Summary contains `traveller said: "`;
  - against a foreign row it gives ForeignOrigin, and the Summary contains `traveller said "`, which belongs to;
  - against the record it gives RecordMismatch;
  - an honest answer never registers, in either order;
  - answer vs document field and answer vs answer register nothing;
  - paper Summaries still say "papers";
  - `Add` refuses a second proof of a documented category from the other source, and refuses null;
  - `Prove` is pure: proving a pair twice gives equal proofs and leaves any log's `Count` unchanged;
  - `ClueLabels.Report` has one `[TestCase]` per category.
  - The existing tests keep their assertions; "BIRTH DATE" still holds.
- **`SeedsTests`** (changed): `DialogStream_IsDistinctFromCaseClueLieAndViolatorStreams`. For cases 1..20, `ForDialog(ForCase(d, c))` is not a case seed and differs from `ForClues`/`ForLies` of the same case and from `ForViolators(d)`.
- **Unchanged and green:** `BirthDatesTests`, `VerdictRulesTests`, `TravellerGendersTests`, `ScriptedRandomTests`, `ShiftLedgerTests` and the rest.

## 6. Verification plan

**Offline, after every change:** `compile_check.py` reports 0 errors in every project, and the reflection runner reports every Domain test passing. The baseline at the piece-2 head is `passed 216, failed 0` (re-run at `8395470`); Unity's EditMode run gave 345 passes plus the known third-party failure (identity-lies spec §8). The plan states the expected count after each task.

**In the branch's own Unity 6000.4.11f1**, through temporary `_TimeDesk*` `-executeMethod` scripts. Their pattern comes from the piece-2 plan Task 13 and `SCRATCH/prev__TimeDesk*.cs.txt`. They are never committed, and reports go to the scratchpad.

0. **Baseline, before any piece-3 code** (at the piece-2 head):
   - dump seeds 12345 and 999, days 1–3, one line per slot: claim, name, registered date, role, `claimAllowed`, violator slot, liar, home, tells, and every paper value;
   - record, by reflection on the private `TimelineService.AllConditionsPass`, the result of each authored trigger against about 20 crafted `WorldState`s (days 1–5, flags, counters, stability, scores).
1. **Content:**
   - Generate World twice; the second run changes no file;
   - `Assets/Data/World/Interview` holds 6 questions, 2 dialogs and 3 unlock triggers (`unlock_q_capital`, `unlock_q_ruler`, `unlock_q_born`; the last one's only condition is `UpgradeOwned interview_protocols`);
   - the library holds 5 books, 6 questions, 2 dialogs, the 3 authored triggers plus the 3 unlock triggers, 4 upgrades and the rumour effect; `interview.menuCapacity` is 8;
   - both `DialogSO`s have `oneShot = true` with no `repeatable` key in the JSON, and every `QuestionSO`'s conditions match §2.14 (`q_currency`, `q_language`, `q_device` and `q_born` carry no `DayAtLeast`);
   - the day plans hold their channels;
   - Validate Content Library reports no issues;
   - negative checks, each on an edited in-memory copy of the source handed to the generator's private `CheckInterview` by reflection (nothing written): a question without `fromDay`, a seventh question (menu capacity), an authored line id equal to a choice-label id, a line of 101 characters, and a dialog naming an effect with a `PayRateBonus` op each give exactly the expected error.
2. **Gates and triggers:**
   - the step-0 trigger results are identical (the three authored triggers still never fire);
   - `NightlyResolve` on a day-1 world fires `unlock_q_capital` (the news line is in `tomorrow.newsLines`) and not `unlock_q_ruler`; on a day-2 world it fires `unlock_q_ruler`; on a world owning `interview_protocols` it fires `unlock_q_born`, and never without it; each fires once;
   - the day-start `InterviewDay` gives askable [Currency, Language, Technology] on day 1, + Geography on day 2, + Politics on day 3, + BirthDate only with `interview_protocols` owned; its `AnswerTellCategories` equal the askable ones without BirthDate on every day, with or without the upgrade.
3. **World check** on seeds 12345 and 999 × days 1–3, each with and without Interview Protocols, plus a 200-seed sweep. The cases come from `CaseFactory.GenerateDayCases(plan, state, Seeds.Day(seed, d), askable, answerTellCategories)`, with both lists from step 2. The script must prove:
   1. **Determinism:** identical dumps including answers and small talk on a rerun; a different seed differs.
   2. **Stream contract:** claims, names, dates, roles and violator slots equal the step-0 dump on every day. Day-1 liars, homes, tells and paper values equal step 0 too. On every day, liars, homes, tells (with channels) and paper values are identical with and without Interview Protocols; only the answers gain the birth date (R23).
   3. **Tells:** every liar has at least one tell; each tell's channel is allowed by the day; an Answer tell's category is in `AnswerTellCategories`; no category carries two tells.
   4. **Papers vs answers:** a Papers tell rewrites every field of its category (piece 2). For an Answer tell, every paper field equals the claim or registered value, and the answer equals `facts.Get(trueHome, c)` or a date with the record's day and month and a home-range year that is not the record's.
   5. **Answers:** every non-tell answer equals `ResolveFieldValue` of the claim (the claim fact, the registered date, or the placeholder). A Papers-tell category answers with the cover. The birth-date answer (with the upgrade) always equals the registered date.
   6. **Provable:** every Answer tell proves through `DiscrepancyLog.Prove(CompareEvidence.ForAnswer(...), row.ToEvidence(), claimNation, claimEra)`: ClaimMismatch against the claim's row; ForeignOrigin with `actualOrigin == trueHomeLabel` against the home's row; null against every other row.
   7. **Honest answers never prove anything**, against any row or the record.
   8. **Shared values:** no Politics tell appears between Egypt and Greece Early modern on day 3.
   9. **Coverage (sweep):** the spoken-tell share is close to §1.5 (0% / 50% / 55%, the same with the upgrade); every (category, channel) option allowed by the day occurs, except BirthDate on the Answer channel, which never occurs; no `NoPossibleLie` warning.
   10. **Small talk, opener and claim:** every small-talk line comes from the claimed place's list or its era's (never the true home's). Every opener holds the honorific matching the traveller's gender. Every `claimLine` is `interview.claim` filled with the case's origin label.
4. **EditMode suite** through `TestRunnerApi`: everything passes except the known third-party `UnitySkills.Tests.Core.PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly` (reported, ignored).
5. **Builder:**
   - run Build Office UI and save the scene;
   - the scene has `TranscriptWindow` with the controller and chrome wired, `IconClueLog` targets it, and no `IconClueLogWindow` remains;
   - `IntercomPanel/Actions` has a `RectMask2D`;
   - the icon ids are Lexicon `archive_access`, Dialect `""`, Material `adv_scanner`; `IconDialectWindow`'s `TitleText` reads "Dialect" and its `Body` "Notes on accents and phrasing. (placeholder)"; the builder logs no error (so the intercom fits the content's menu capacity);
   - **idempotence, checked semantically:** run Build Office UI a second time and save. After each build a temporary `_TimeDesk` script dumps the scene: every GameObject's hierarchy path and active flag, its component types in order, each `RectTransform`'s anchors, pivot, anchored position and size (rounded to 0.1), every TMP text's string, font-size range, wrapping and overflow mode, and every object reference the builder sets through `SetRef` (the serialized `ObjectReference` properties of the project's own MonoBehaviours), written as the target's hierarchy path. The two dumps must be equal. The YAML itself is not compared: the builder destroys and recreates `MinBtn`/`MaxBtn`/`CloseBtn` on every window (`OfficeSceneUIBuilder.cs:737-743`), the document template (336), the newsletters (92-95), the Scanner window (145), the whole Citizen Records window (171, including its input and rows, 1358 and 1378), the Start menu (1294) and now `IconDialectWindow` on every run, so Unity gives them new fileIDs (the last rebuild, `002bbfc`, changed about 27k lines of `OfficeScene.unity`).
6. **Scripted play-through** on the rebuilt OfficeScene (play mode, `[InitializeOnLoad]` + SessionState steps). The script prepares a run at day 2 with a seed whose queue holds three travellers: an honest one, a liar with a Geography Answer tell, and a liar with a Papers tell. It picks the seed with the same `CaseFactory` call. It then drives the real UI:
   - the hub lists the 2 requests, "Ask about home >" and "Any news from home? >". The ask menu lists "< Back", Currency, Language, Device, Capital and Small talk, in that order (no Ruler, no Date of birth);
   - every transcript row the play-through shows, in both texts, reports `isTextOverflowing == false` after `ForceMeshUpdate`. The script also fills a row with the worst-case strings of §2.14 (the 70-character claim, the 92-character rumour line, and a 100-character sentence of ordinary English words, the knob's bound) and checks the same;
   - "Request Travel Passport": the passport window opens, the transcript gains the prompt and reply, and the transcript stays closed;
   - the Geography liar: asking Capital opens the transcript. Clicking the `Line_q_capital.answer` row, then the Capitals Gazetteer claim row, logs "CAPITAL INCORRECT — traveller said: …". Clicking the home's row shows "ALREADY DOCUMENTED". Deny is correct, with no citation;
   - the honest traveller: every answer against its claim row gives MATCH and nothing is logged. Accept is correct;
   - the Papers-tell liar: its tell category's answer is the cover and logs nothing. The paper field against the book logs. Deny is correct;
   - "Any news from home? >" → "Tell me more." → "Noted. Thank you.": the entry disappears from the next traveller's hub;
   - Escape and back mid-interview: the transcript and its page are unchanged;
   - force closing time, then the results panel (the script looks `DayFlowUIController` up including inactive objects: its `DayFlowUI` host sits under the desktop `Canvas`, which `FocusOffice()` turns off at day end; the piece-2 play-through hit this, identity-lies spec §8). The save holds `dlg:dlg_rumour:done` and `rumour_calculators`, and one `ActiveEffectEntry` for `Effect_Dialog_RumourHeard` with `startDay = 3`;
   - `AdvanceToNextDay`: the day-3 briefing shows the rumour tip and the ruler notice. Ruler is askable; "About those calculators... >" is offered and "Any news from home? >" is not;
   - no warnings or errors (the interview wiring warning stays silent).
7. **Hygiene:**
   - revert unrelated Unity-touched files (`*.csproj`, `ProjectSettings/*`, the TMP fallback atlas);
   - commit the regenerated content and the rebuilt `OfficeScene.unity`;
   - delete the `_TimeDesk*` files and their metas;
   - append the verification record to this spec;
   - the plan's merge step says how `OfficeScene.unity` is merged (§7 "Scene merge"): never by hand; take one side and re-run Build Office UI on the merged branch, then repeat step 5's semantic check.

## 7. Risks

- **Difficulty jump on day 2.** About half of day-2 liars (55% on day 3) can be caught only by asking the right question, in a real-time shift tuned without an interview (8/10/12 travellers, 480 s). The knob is the day's channel set. A per-channel weight or a question time cost can follow playtests.
- **Replay after the end-of-shift save** shows that shift's consequences a day early: a flag-gated follow-up dialog can be offered on the replayed day. No effect can apply twice, no dialog effect acts while active (R24), and a flag- or upgrade-gated question is hint-only (R23), so none of these changes the replayed tells. An instant op that feeds generation (an unlocked clue-gating upgrade, a counter a trigger reads) still could; that belongs to the known "save the day's generation inputs" gap.
- **Late announcements.** A save from before piece 3 gets the notices whose night has passed on its next morning: a day-2 save gets both notices on day 3 (the capital one a night late); a day-3 save gets both on day 4 (the capital one two nights late). A question whose authored conditions already hold on day 1 would be announced on day 2 (none in piece 3). The notices say "may now ask", so a late one is not wrong. Availability is unaffected (R5).
- **The upgrade buys only a hint.** Interview Protocols makes the birth date askable and never adds a tell (R23), so its value is a visible disagreement with a passport birth-date tell, which Records already proves. That is modest for 120 credits. It never makes the game harder; playtests may lower the price or give the upgrade more (piece 4/5).
- **Conservative capacity.** The hub check counts every dialog as offered at once (R25). With piece 4's arcs this will bind early; piece 4 replaces it with paging or a per-day count.
- **Scene merge.** `main` (and `origin/main`) has moved to `b7f8671`, Codex's art checkpoint, which changed `Assets/Scenes/OfficeScene.unity` (about 12k lines) and is not in this branch (merge base `413230c`), so the conflict is certain. The main checkout `E:\unity\NOPE` (branch `art`, also at `b7f8671`) still has further uncommitted Codex changes to the scene (`git status` shows ` M`). This branch commits a rebuilt `OfficeScene.unity`, and every builder run rewrites tens of thousands of YAML lines with new fileIDs (`002bbfc` changed about 27k), so a textual merge is impractical. Mitigation: coordinate with the Codex scene work before merging (ideally Codex commits its remaining scene changes first). On a conflict, never hand-merge the YAML: take the other side's `OfficeScene.unity`, re-run Build Office UI on the merged branch (the builder is authoritative and idempotent), repeat the §6 step 5 semantic check and the §6 step 6 play-through, and commit the rebuilt scene. The plan's merge step says so.
- **Old scenes.** Until the scene is rebuilt, the transcript is missing: questions are hidden, no tell is spoken, and a warning names the builder (R12). The rebuilt scene is committed with this piece.
- **`PagedRowsWindow` refactor.** A field-name mismatch would empty the book windows. The field names are kept, and the play-through reads the books.
- **Moving `TriggerConditionType` and `EffectOpType`** changes their assembly, not their values. Assets store ints, and every C# reference is in the global namespace. `GatesTests` and `EffectOpsTests` pin the ints, and the step-0 baseline proves the triggers behave the same.
- **Layout.** The transcript overlaps documents and books (all draggable). The text column is about 400 px (570 px of rows, minus padding, spacing and the 150 px speaker column). One line at 12 pt holds about 66 characters, but piece 3's own content reaches 92 (the rumour line) and the claim 70, so sentences wrap to two lines inside the fixed 34 px row (12–14 pt), which hold about 120 characters. The generator bounds every line's worst case at 100 (R10), and §6 step 6 checks that no row overflows. A sentence that needs three lines is impossible by construction; if the knob is ever raised past what two lines hold, that check fails.
- **The TMP atlas (G6).** The ASCII rule (R18) keeps authored text from adding glyphs. The plan still checks and reverts the fallback atlas.
- **Stacked branches.** This branch sits on `feat/identity-lies` (`dc676cd`), which is complete: Task 13 passed at `efe385d` with no product fix, so the anchors here are final. Neither branch contains `main`'s `b7f8671` (see "Scene merge"). The plan still re-reads each file before editing it. Never commit in `E:\unity\NOPE`.
- **Content depth.** Piece 3 authors two dialogs and minimal small talk. The system is ready for piece 4's arcs, but players will see few narrative lines until then.
- **English-only wording** (honorifics, labels, `ClueLabels`). Piece 6 moves it into tables keyed by the line ids.

## Review notes

The piece-3 analysis's completeness check (`piece3_map.json` `critique`) listed items the synthesis missed and claims it got wrong. Each is handled or deferred here:

| Critique item | Where |
|---|---|
| Piece-2 API (trueHome, IsLiar, HomeLabel, gender, LiePlan; tells not stored) | §2.3 (`ChannelOf`/`TellValue`), §2.9 (answers stored on the case, no separate tell list) |
| Piece-2 rules and tests to supersede (R3, §1.1:67, FEATURES :85, LiesTests, "Cross-piece gate trap") | §3.2, §5 |
| Scanner spec rule 1 superseded by name, with the intent argument | §3.2 |
| Determinism: option pool, the snapshot's dependence on flags and upgrades, stream contract, LiesTests | R1, §1.8, §2.9 streams, §5, §6 steps 0 and 3.2 |
| Save and replay effects; SaveVersion | X5, §1.8, §2.10, §7 |
| `build_world_source.py` overwriting | X10: retired, JSON hand-maintained |
| Player-facing copy (citation 142, idle hint) | C1, §1.7, §2.16 |
| FEATURES lines :31, :35, :54-60, :62, :85, :94, :95 (the critique's numbering; now :31, :35, :55-61, :63, :86, :95, :96) | §3.3 (:35 holds unchanged, Q9) |
| Named mechanisms: Clue Log, Dialect Filter, Chatter/GetCues/TimelineCueReceiver, `introLine`, the `FiredFlag` grammar | Q9, R9, X8/R16, R8, X5/§2.8, §2.19 |
| Interview wiring feeding tell eligibility | R12 |
| WorldState access; dialog effects vs HUD and endings | R7, §2.10 |
| Answers for missing facts; the BirthDate tell value | X7, §2.9 (`ResolveFieldValue`, `TellValue`) |
| `Seeds.ForDialog` needs `caseSeed` | X7, §2.9 |
| Layout constraints (WindowLayer under intercom/compare bar) | R10, §2.12 |
| The canvas deactivated outside monitor focus (coroutines) | §2.11 |
| Unity verification plan | §6 |
| Label consistency, including Technology | R13 |
| Gender consumer | X9, R8 |
| Fallback overflow | F1, R11 |
| The DayAtLeast meaning; SO-referencing conditions in JSON | R4, R6, `GatesTests` |

Claims the critique marked wrong, as corrected here:
- `HasOtherValue`/`PickOtherValue` are gone; the extension point is `Lies.Plan`'s option pool over `IsProvableTell` (§2.3).
- Piece 2's spec exists, and its Tasks 1–12 are committed at `d7614f0`.
- `ShiftClock` also has Start and Stop (Q15).
- A permanent `BriefingLine` cannot announce unlocks; R5 uses one-shot triggers.
- Option B needs no new proof type (X1).
- Under option A, difficulty would drift; X1 makes it a knob.
- `isForged` is gone in piece 2; answers read `LiePlan` inside `CaseFactory`.

### Independent review (2026-09-24)

26 findings. Each was checked against the code at `efe385d` (read-only worktree) before it was applied. 25 are applied in full; one is applied with a corrected premise (F9a). None is rejected outright.

| # | Finding | Verdict | Where |
|---|---|---|---|
| F1 | §6 step 5's "second build leaves `OfficeScene.unity` byte-identical" cannot hold: the builder recreates `MinBtn`/`MaxBtn`/`CloseBtn`, the document template, newsletters, Start menu and Records input/rows on every run (verified at 92-95, 336, 737-739, 1294, 1358, 1378) | applied | §6 step 5: a semantic dump (paths, components, rects, texts, `SetRef` targets) compared across two builds; "no error logged" kept |
| F2, F19 | `TryRegister` loses its only production caller (`InvestigationUIController.cs:118`, verified) and would be test-only | applied | R17, §2.6 (removed; tests through a test-local helper), §3.1, §5 (`DiscrepancyLogTests` header and helper, `FactTableTests` 63/75, `LiesTests` 323/330), §6 3.6 uses `Prove` |
| F3 | `UpgradeSO.description` is never shown (`HomeUIController.BuildShopRows` prints name and price only, verified), so "announced: shop description" was false | applied | §1.3 now "the morning after it is bought" via the unlock notice (with F21); the description stays authored like the other three upgrades' and is documented as not shown; showing descriptions is listed as not scheduled (§4) |
| F4, F14 | "take effect from the next day" is wrong: instant ops apply at the end of the shift, and continuous ops count at once because `IsActiveOnDay` has no lower bound and `Active()` filters on `world.day` (verified `WorldState.cs:257-258`, `TimelineEffects.cs:23`) | applied | R24: dialog effects limited to instant ops and briefing/news lines (`EffectOps.ActsWhileActive`, generator and validator errors naming piece 4/5); §1.6, §1.8, §2.10 state the real timing; `IsActiveOnDay` unchanged; FEATURES scoring bullet |
| F5 | Dialect stays titled "Dialect Filter" / "(upgrade)", and `BuildOSWindow` would keep the old texts (`Text()` returns the existing object, verified 570-578) | applied | R9, §1.10, §2.12: "Dialect" / "Notes on accents and phrasing. (placeholder)"; `DestroyChildIfPresent(windowLayer, "IconDialectWindow")` first; §6 step 5 checks the copy |
| F6, F18 | The one-shot filter, the done-flag rule, the small-talk rule and the flag grammar sat in untestable glue, so §2.18's claim was false | applied | `FlagKeys`, `Gated<T>`, `Gates.DayOnly` (§2.2); `InterviewDay` decides questions, tell categories and offered dialogs from the snapshot; `DialogOutcomes` plans flags and effects; `Interview.PickSmallTalk`/`Opener`/`Claim` (§2.4–2.5); `TimelineKeys` unchanged; §2.18 rewritten; `InterviewDayTests`/`InterviewTests` cases (§5) |
| F7 | Comments and messages that become wrong were not listed | applied | `TriggerCondition.key` (§2.8), `DiscrepancyProof` values and class doc (§2.6), `LieOutcome.Liar` (§2.3), `Disguise` summary, `ResolveFieldValue` summary and warning (§2.9), `CompareController` class doc (§2.11), `MakeDay` doc and `CheckPlaces` message (§2.13); all in the §2.17 table |
| F8 | `DialogRunner.CurrentNodeId` had no production reader; the `q:{id}` label source and two question labels were unspecified | applied | `CurrentNodeId` dropped (§2.5); the choice label is `q.label`; all six labels in §2.14 and §1.3 |
| F9 | (a) "committed :44 has no test claim to extend"; (b) the R12 behaviour is missing from FEATURES | (a) premise incorrect, applied anyway; (b) applied | (a) `d7614f0` and `efe385d` :44 already end "(tell selection, eligibility and the may-lie exemptions tested: `LiesTests`, `ForgeryTests`; …)"; §3.3 now writes the full new claim instead of "adds `InterviewTests`". (b) :36 gains the interview-wiring warning and its effect |
| F10 | `InterviewReachable` ignored `transcriptChrome`, which `Choose` opens unguarded; the builder's `library` can be null | applied | §2.11: `InterviewReachable` includes the chrome, `StartInterview` offers spoken choices only when reachable, explicit `!= null` checks (see also V1 below); §2.12: the icon and capacity checks are skipped with a null library (231 already warns) |
| F11 | Committing a rebuilt `OfficeScene.unity` will conflict with Codex's uncommitted scene work in `E:\unity\NOPE` (verified ` M`) | applied | §7 "Scene merge" and §6 step 7: coordinate; on conflict take one side, re-run the builder, repeat the semantic check and play-through |
| F12 | `fromDay`/`oneShot` defaults relied on `JsonUtility` running initializers on nested array elements | applied | R27: `fromDay` required (≥ 1 check, like `days[].tells`); the JSON key is `repeatable` (missing = false = one-shot); every piece-3 entry authors `fromDay`; §6 step 1 checks the generated values and a missing `fromDay` |
| F13 | Interview Protocols made the game harder: it moved part of the birth-date tells from the passport into speech and changed later lie draws | applied | R23: `AnswerTellCategories` (day-gated questions only) feeds `Lies.Plan`; upgrade- and flag-gated questions are hint-only; §1.3, §1.5 (60% row removed), §1.8 (purchase exception removed), §2.9–2.10, §5, §6 steps 2, 3.2, 3.9 |
| F15 | `DialogChecks` missed "a reachable node cannot reach an ending" and "an effect on a non-ending choice"; a malformed asset could take over the hub | applied | §2.5 two new checks; `InterviewDay` skips any dialog with problems and exposes `ContentProblems`, which `GameManager` logs as errors; §1.6; §5 |
| F16 | With the upgrade the ask menu is exactly 8, "< Back" came last, and the new mask would clip it silently on a ninth entry | applied | R25: "< Back" first; `interview.menuCapacity` (8) checked by generator and validator (`DialogChecks.MenuProblems`, per-node limit in `Problems`) and by the builder against its own layout; §5 capacity tests |
| F17 | One 400 px line cannot show the 70- and 92-character rumour lines; the §7 "about 50 characters" premise was wrong | applied, option (a) adapted | R10: the sentence wraps inside the fixed 34 px row (two lines at 12–14 pt, about 120 characters) rather than rows growing (which would push the newest rows under the mask); `interview.maxLineChars` (100) enforced on worst-case rendered lengths (`Interview.WorstCaseLength`); §6 step 6 checks `isTextOverflowing`; §7 corrected. The rumour lines stay whole |
| F20 | `Interview.CanBeAsked` repeated `Forgery.IsProvableTell`'s category switch (verified `Forgery.cs:24-33`) | applied | `Forgery.IsProvableCategory` extracted and called by `IsProvableTell` (§2.3); generator and validator use it (§2.13); `CanBeAsked` dropped; tests in `ForgeryTests` |
| F21 | Announcements came from `fromDay` only: a day- plus upgrade-gated question would be announced while hidden, and upgrade/flag-gated questions never were | applied | R5: an unlock trigger for every gated question, with the authored conditions plus the day gate; `announce` required for every gated question; `q_born` gets a notice; §4 item removed; §6 steps 1–2 |
| F22 | "only the announcement arrives a night late" was inaccurate for pre-piece-3 saves | applied | §1.3 and §7 state the real behaviour (up to two nights late, two notices together); notices say "may now ask" |
| F23 | FEATURES gaps: R12 behaviour, the "locked questions hidden" test claim, the ASCII rule and trigger rewiring | applied | §3.3: :36 extended, intercom bullet cites `InterviewDayTests`, :95 lists ASCII, ids, line length, capacity, op rule and trigger rewiring |
| F24 | Retiring `build_world_source.py` supersedes piece-2 spec line 380 and plan Task 11's script steps | applied | §3.2 (piece-2 §2.6 line 380; plan lines 64, 2787, 2828-2832, 2840-2843); §2.17 notes that `HOUSE_RULES.md` line 16 must be updated |
| F25 | Authored line ids were checked only against other authored lines | applied | R14, §2.13: one global id set (generated and authored), authored ids must start with `{dialogId}.`, reserved runtime ids; errors name both owners; §6 step 1 negative check |
| F26 | The claim sentence stayed hard-coded while its transcript line got a stable id | applied | R26: `interview.claim` with `{place}`, filled by `Interview.Claim`; banner, shift summary and transcript share it |

Found while verifying, and fixed here:
- **V1.** The spec used `?.` on serialized Unity references (`transcriptWindow?.Bind`, `?.Refresh`). An unassigned serialized field is Unity's fake null in the editor, which `?.` does not catch. §2.11 now uses explicit `!= null` checks.
- **V2.** The claim line reaches 70 characters ("… Ottoman Iraq (Baghdad Vilayet) (Industrial)."), so F17 affected every traveller from that place, not only the dialog lines. The line-length check covers the claim and every `{token}` fill.
- **V3.** `TimelineService.ConditionsPass` would have had only a private caller once `InterviewDay` evaluates gates. It is replaced by `ToGates`, which `GameManager` and `AllConditionsPass` both call.
- **V4.** The `NoPossibleLie` warning changed after `d7614f0` (`f3a68f9`); §2.9 now extends the `efe385d` text.

### Aligned with the code as implemented (2026-09-24)

The spec was drafted while piece 2's Task 13 was pending. Piece 2 has since finished (`efe385d` verified with no product fix, `15302e1` regenerated content, `dc676cd` verification record), so every name, signature, file, line reference, count and content claim above was re-checked against this branch at `8395470` (code unchanged since `efe385d`; offline suite re-run: `passed 216, failed 0`). Names, signatures, file paths, line endings, the content figures of §2.14 and the other line references held. Corrected:

- **A1. Anchors.** The header said line numbers refer to `d7614f0` and Task 13 was pending. It now states that piece 2 is complete and that line numbers refer to `8395470`. §7 "Stacked branches" no longer expects anchors to move.
- **A2. Piece-2 spec lines (§3.2).** The piece-2 review fixes (`324579b`..`efe385d`) added lines to the identity-lies spec, so the `d7614f0` numbers had moved: §2.6 380 → 381, §2.7 389 → 390, §3.3 491/493 → 492/495, §4 500 → 502, §5 531 → 533, §6 step 3.3 593 → 596, §7 617 → 620 and 620 → 623. §2.3's range now starts at 213, the superseded `ApplyTo` comment. The §3.3 note about `FEATURES.md` line numbers now says they are current (the piece-2 review reworded :84-85 in place).
- **A3. Code line references.** `ReferenceBookWindowController.SetBook` is 42-53 (not 42-54). The fallback's book loop is 439-451 (§2.11 and §3.1 said 439-452). R13 and §2.19 named `DiscrepancyLog.CategoryLabel`; the private method is `Discrepancy.CategoryLabel` (in `DiscrepancyLog.cs`, as §2.6 and §3.1 already said).
- **A4. Counts.** `DiscrepancyLogTests` has 22 `TryRegister` calls (§2.6 and §5 said "about 20"). §6 now records the offline baseline (216, re-run here) and Unity's 345 EditMode passes. F1's fallback body is about 540 px on a 1080 px canvas (anchors at `InvestigationUIController.cs:470-477`), not "roughly 650 px". R27: the root has two field initializers (`travellerAgeMin = 18`, `travellerAgeMax = 70`).
- **A5. Piece-2 review text.** `ResolveFieldValue`'s `efe385d` summary ends "A liar's tells overwrite these values afterwards (Disguise).", which an Answer tell would make false; §2.9 rewrites that sentence too.
- **A6. Doc comments the spec missed** (same F7 rule): `CaseInstance.IsLiar` (73, "their papers leak tells"), `Lies.Plan`'s summary (109-119, "when the papers print it") and `LiePlan._values` (47, "prints"), `Discrepancy.actualOrigin` (107-110, "the printed value") and `InvestigationUIController.BuildFallbackBody` (405-408, "today's books"). Added to §2.3, §2.6, §2.9, §2.11 and the §2.17 table.
- **A7. Repository state.** `HOUSE_RULES.md` line 16 already retires `build_world_source.py` (§2.17 said it still named the script). `main` moved to `b7f8671`, Codex's checkpoint with a committed `OfficeScene.unity` change that this branch lacks; §7 "Scene merge" now says the conflict is certain and that the main checkout still has further uncommitted scene changes. The historical F11 and F24 rows above keep their original wording.
- **A8. Verification gotcha.** The piece-2 play-through found `DayFlowUIController` only when it searched inactive objects (identity-lies spec §8); §6 step 6 says so.

The plan's review should append its findings here.
