# NOPE — Alpha Build Roadmap & Status

Game: "Time Sorter" — Papers, Please-style time investigator (full plan: Notion page "Time Sorter").
Target: Alpha = full gameplay loop with all dev systems data-driven, ready for content.
This doc is the handoff state for any work session. Update the checkboxes as phases land.

## Architecture decisions (locked)

- Two scenes: OfficeScene (briefing/shift/results) + HomeScene (expenses/shop/slot/sleep).
- Full JSON save/load to disk (`SaveSystem`, single slot, versioned).
- Citations: Papers Please style — N free warnings/day, then escalating fines (GameConfigSO).
- Timeline Variation: AttributeSO (key attributes/cultures) scored per NationEraProfileSO
  (nation at a time period). Dominant/supporting tiers recomputed nightly from scores
  (top-N from GameConfigSO). Every send applies ArchetypeSO default impacts to the
  destination (chosen era + case nation); cases/legendaries add authored impacts.
  TimelineTriggerSO = special conditions (counters/scores/flags) firing EffectSO outcomes.
- EffectSO = generic channel-tagged op bundle (instant + continuous ops). Effects STACK
  in WorldState.timeline.activeEffects with per-entry duration. Any system connects via
  TimelineCueReceiver (channel) or TimelineEffects queries (Visuals, Music, UI,
  VisitorPool, Shop, Newsletter, TechTree, SpecialCases, Chatter).
- Nightly resolve at sleep (TimelineService.NightlyResolve, called in
  RunManager.AdvanceToNextDay BEFORE day++): dominance -> tier effects -> triggers ->
  expiry -> deterministic TomorrowPackage (briefing/news lines) stored in the save.
- Slot machine modifiers (legendaryChanceBonus/forgeryChanceModifier/payRateMultiplier)
  are set during Home for tomorrow; consumed by next shift; reset at end of that shift
  via RunManager.ResetTomorrowModifiers().
- Score keys (TimelineKeys): "attr:{profileId}:{attrId}", ad-hoc "attr:{nationId}@{eraId}:{attrId}",
  "attrTotal:{attrId}", "nation:{nationId}". Counters: "sent:era:{eraId}",
  "sent:tag:{tag}", "sent:tag:{tag}:{eraId}".
- RunConfig.asset must stay in Assets/Resources (loaded by name). GameConfig_Default in
  Assets/Data/Config. ContentLibrary_Main is the single content registry.

## Phase status

- [x] Phase 0 — Backbone: WorldState (full serializable state), SaveSystem,
      RunManager (persistent, scene flow, day seeds), RunConfigSO + asset,
      OfficeUIController rename fix, OnDayCompleted event.
- [x] Phase 1 — Economy: GameConfigSO + asset, ShiftLedger, ShiftScoring
      (pay/citations/stability/fired check), HUD + citation slip in OfficeUIController
      (optional fields, need scene wiring), GameManager verdict flow.
- [x] Phase 2 — Timeline Variation: AttributeSO, NationSO, NationEraProfileSO,
      ArchetypeSO (+TimelineImpact), TimelineTriggerSO, EffectSO rewrite (channels/ops),
      TimelineService (impacts + nightly resolve), TimelineEffects (stacking queries),
      TimelineCueReceiver, CaseFactory archetype/nation/effect-modifier integration,
      ContentLibrary timeline arrays.
- [x] Phase 3 — DayFlowUIController (briefing panel from TomorrowPackage, results
      panel from ShiftLedger), GameManager flow (briefing -> shift -> results -> home),
      RunManager.GoHomeOrAdvance (skips missing HomeScene), editor tool
      Tools > TimeDesk > Build Office UI (creates + wires HUD/citation/briefing/results).
- [x] Phase 4 — HomeScene: GameConfigSO Home/Family + Home/Slot Machine fields
      (expense rates, condition care, slot spin cost), UpgradeSO (description/cost/
      unlockEffect), SlotOutcomeSO (weighted outcomes -> money/tomorrow modifiers/
      effects), ContentLibrarySO.Upgrades + SlotOutcomes accessors, RunConfigSO
      startingFamilyMembers (seeded by RunManager.NewRun), HomeEconomy (expenses,
      family condition drift, Treat), HomeUIController (HUD + Expenses/Shop/Slot/
      Sleep panels, optional + runtime-spawned rows), HomeManager (Expenses -> Shop
      -> Slot -> Sleep -> RunManager.AdvanceToNextDay), HomeScene.unity + editor tool
      Tools > TimeDesk > Build Home UI (creates Canvas/EventSystem/HomeManager/
      HomeUI and wires every panel), both OfficeScene and HomeScene registered in
      Build Settings.
- [x] Phase 5 — Progression & endings: WorldState.endingId, GameConfigSO.
      bankruptcyMoneyThreshold, EndingSO (TimeDesk/Endings/Ending — Fired/
      Bankrupt/AttrTotalAtLeast/DayAtLeast conditions + priority),
      ContentLibrarySO.Endings + GetEndingById, EndingService.Evaluate
      (highest-priority match). GameManager checks for an ending after each
      verdict (catches Fired) and HomeManager checks before sleep (catches
      Bankrupt/score/day endings); a match sets WorldState.endingId, saves,
      and loads TitleScene. RunConfigSO.titleSceneName + RunManager.
      LoadTitleScene(). TitleScene.unity (registered in Build Settings),
      TitleSceneController (ending display + Continue/New Run via
      SaveSystem.HasSave()/RunManager.NewRun()), TitleUIController (optional
      title/ending panels), editor tool Tools > TimeDesk > Build Title UI.
- [ ] Phase 6 — Dev tools: in-game debug panel (skip day, +money, set flags,
      force legendary, unlock upgrades), timeline inspector (live scores/dominance/
      active effects), editor ContentLibrary validator menu item.
- [x] Phase 7 — Content pass: editor tool Tools > TimeDesk > Generate Phase 7 Content
      (Assets/Editor/Phase7ContentGenerator.cs) authors Day 2-3 plans, 6 archetypes,
      5 nations x era profiles (Greece/NGermany/Japan/Egypt/China) with 3 attributes
      (Democracy, Science, Art) and 30 dominant/supporting BriefingLine effects,
      21 nation legendaries (3 per nation) + 6 unaffiliated "famous" legendaries
      (2 per attribute), 3 timeline triggers (science boom/democracy collapse/art
      renaissance), 3 upgrades with costs, 5 slot outcomes, 6 endings (Fired/
      Bankrupt/3x AttrTotalAtLeast/DayAtLeast retirement), all wired into
      ContentLibrary_Main. Run the menu item once in the Unity Editor to materialize
      the ~92 new ScriptableObject assets under Assets/Data/.
      KNOWN GAP: Japan/Egypt/China have no dedicated ClueSO/DocTemplateSO assets yet —
      cases set in these eras draw red-herring clues from the existing Greece/NGermany
      pool (CaseFactory degrades gracefully). Author era-specific clues in a future pass.

## Scene wiring still needed (user, in editor)

- OfficeScene: open the scene and run Tools > TimeDesk > Build Office UI to create
  and wire HUD/citation/briefing/results (OfficeUIController new fields — moneyText,
  stabilityText, dayText, citationPanel, citationText, citationContinueButton).
- HomeScene: open the scene and run Tools > TimeDesk > Build Home UI to create
  Canvas/EventSystem/HomeManager/HomeUIController and wire all four panels
  (Expenses/Shop/Slot/Sleep) + HUD. Re-running is safe (finds existing pieces by name).
- TitleScene: open the scene and run Tools > TimeDesk > Build Title UI to create
  Canvas/EventSystem/TitleSceneController/TitleUIController and wire the Title
  panel (Continue/New Run) and Ending panel (display name + body + New Run).
  Re-running is safe (finds existing pieces by name).
- ContentLibrary_Main: assign new arrays (attributes, nations, nationEraProfiles,
  archetypes, timelineTriggers, upgrades, slotOutcomes, endings) as content
  gets authored. No EndingSO assets exist yet — until Phase 7 authors at least
  a "Fired" ending, EndingService.Evaluate always returns null and
  verdict.firedNow only logs a warning (run continues).

## Conventions

- Sealed classes, XML doc comments on every member, SO-driven data, null-safe UI.
- ScriptableObject menu root: "TimeDesk/". Timeline assets under "TimeDesk/Timeline/".
- New systems read state ONLY from WorldState + TimelineEffects queries.
