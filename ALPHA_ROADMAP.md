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
- [ ] Phase 3 — End-of-day results screen (ledger breakdown) + morning briefing panel
      (TomorrowPackage lines) in OfficeScene; "go home" button -> HomeScene.
- [ ] Phase 4 — HomeScene: expenses sheet (family conditions), upgrade shop
      (UpgradeSO needs cost field; use TimelineEffects.GetShopDiscountPercent),
      slot machine (SlotOutcomeSO -> tomorrow modifiers and/or effects),
      sleep -> RunManager.AdvanceToNextDay().
- [ ] Phase 5 — Progression & endings: fired/bankrupt game-over screen, ending
      conditions from timeline scores (e.g., attrTotal threshold), title scene
      (Continue/New Run), scene flow polish.
- [ ] Phase 6 — Dev tools: in-game debug panel (skip day, +money, set flags,
      force legendary, unlock upgrades), timeline inspector (live scores/dominance/
      active effects), editor ContentLibrary validator menu item.
- [ ] Phase 7 — Content pass: Day 1-3 plans, 3+ archetypes, 2 nations x profiles,
      4+ attributes, 2-3 triggers (e.g., scientists->discount), 3 upgrades with costs,
      slot outcomes, Tesla-style legendary with authored impacts, briefing/news copy.

## Scene wiring still needed (user, in editor)

- OfficeScene: wire OfficeUIController new fields — moneyText, stabilityText, dayText,
  citationPanel, citationText, citationContinueButton.
- ContentLibrary_Main: assign new arrays (attributes, nations, nationEraProfiles,
  archetypes, timelineTriggers) as content gets authored.
- HomeScene does not exist yet (Phase 4 creates it; add to Build Settings).

## Conventions

- Sealed classes, XML doc comments on every member, SO-driven data, null-safe UI.
- ScriptableObject menu root: "TimeDesk/". Timeline assets under "TimeDesk/Timeline/".
- New systems read state ONLY from WorldState + TimelineEffects queries.
