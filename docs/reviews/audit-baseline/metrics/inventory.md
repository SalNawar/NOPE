# Type inventory (ff3a6e0228)

Tool: audit-static 1.0. 301 source files, 534 types (partial parts merged). Callers = other source files whose identifier tokens mention the type's simple name; asset refs = tracked scenes/prefabs/assets whose `m_Script` carries the script's meta guid (text assets scanned: 817, binary skipped: 8).

## Assembly-CSharp (180 types)

| Type | Kind | File | Lines | Code | Public members | Code callers (prod/test) | Asset refs |
|---|---|---|---:|---:|---:|---|---:|
| `CaseBlueprintSO` | class | Scripts/CaseBlueprintSO.cs:11 | 68 | 29 | 8 | 6 (6/0) | 1 |
| `CaseFactory` | class | Scripts/CaseFactory.cs:22 | 866 | 529 | 3 | 1 (1/0) | 0 |
| `CaseInstance` | class | Scripts/CaseInstance.cs:7 | 106 | 31 | 28 | 8 (8/0) | 0 |
| `DocumentInstance` | class | Scripts/CaseInstance.cs:117 | 17 | 8 | 5 | 3 (3/0) | 0 |
| `CharacterArt` | class | Scripts/Characters/CharacterArt.cs:15 | 222 | 172 | 7 | 9 (9/0) | 0 |
| `CharacterArt.Entry` | class | Scripts/Characters/CharacterArt.cs:33 | 11 | 6 | 3 | 3 (2/1) | 0 |
| `LookSpriteStack` | class | Scripts/Characters/LookSpriteStack.cs:9 | 79 | 60 | 4 | 3 (3/0) | 1 |
| `TravellerPortraitView` | class | Scripts/Characters/TravellerPortraitView.cs:9 | 40 | 32 | 2 | 2 (2/0) | 1 |
| `TravellerView` | class | Scripts/Characters/TravellerView.cs:12 | 77 | 48 | 5 | 5 (5/0) | 1 |
| `ClueSO` | class | Scripts/ClueSO.cs:9 | 27 | 16 | 6 | 3 (3/0) | 0 |
| `ContentLibrarySO` | class | Scripts/ContentLibrarySO.cs:13 | 520 | 286 | 39 | 21 (21/0) | 1 |
| `GameConfigSO` | class | Scripts/Core/GameConfigSO.cs:9 | 144 | 76 | 29 | 10 (10/0) | 1 |
| `RunConfigSO` | class | Scripts/Core/RunConfigSO.cs:10 | 47 | 22 | 13 | 7 (7/0) | 1 |
| `RunManager` | class | Scripts/Core/RunManager.cs:9 | 293 | 165 | 19 | 12 (12/0) | 0 |
| `SaveSystem` | class | Scripts/Core/SaveSystem.cs:9 | 161 | 115 | 5 | 2 (2/0) | 0 |
| `SaveSystem.SaveFile` | class | Scripts/Core/SaveSystem.cs:31 | 5 | 5 | 2 | 0 (0/0) | 0 |
| `UnityRandomSource` | class | Scripts/Core/UnityRandomSource.cs:5 | 14 | 10 | 2 | 1 (1/0) | 0 |
| `DayEventDirector` | class | Scripts/DayEventDirector.cs:9 | 53 | 32 | 3 | 2 (2/0) | 5 |
| `DayEventSO` | class | Scripts/DayEventSO.cs:9 | 7 | 4 | 1 | 3 (3/0) | 0 |
| `DayEventContext` | class | Scripts/DayEventSO.cs:21 | 17 | 10 | 3 | 2 (2/0) | 0 |
| `DayOrchestrator` | class | Scripts/DayOrchestrator.cs:16 | 251 | 137 | 8 | 5 (5/0) | 5 |
| `DayPlanSO` | class | Scripts/DayPlanSO.cs:17 | 273 | 143 | 18 | 9 (9/0) | 6 |
| `ForcedCaseSlot` | class | Scripts/DayPlanSO.cs:297 | 11 | 6 | 3 | 2 (2/0) | 0 |
| `EraWeight` | struct | Scripts/DayPlanSO.cs:313 | 8 | 5 | 2 | 1 (1/0) | 0 |
| `DayEventTrigger` | enum | Scripts/DayPlanSO.cs:325 | 8 | 5 | 2 | 2 (2/0) | 0 |
| `EventPlacement` | enum | Scripts/DayPlanSO.cs:337 | 11 | 6 | 3 | 0 (0/0) | 0 |
| `DayEventRule` | class | Scripts/DayPlanSO.cs:353 | 20 | 9 | 6 | 0 (0/0) | 0 |
| `DayRunner` | class | Scripts/DayRunner.cs:7 | 70 | 41 | 0 | 0 (0/0) | 0 |
| `DebugPanelController` | class | Scripts/DevTools/DebugPanelController.cs:14 | 365 | 278 | 0 | 1 (1/0) | 0 |
| `DevToolsState` | class | Scripts/DevTools/DevToolsState.cs:9 | 30 | 14 | 3 | 4 (4/0) | 0 |
| `DialogSO` | class | Scripts/Dialog/DialogSO.cs:10 | 8 | 5 | 2 | 4 (4/0) | 3 |
| `QuestionSO` | class | Scripts/Dialog/QuestionSO.cs:9 | 8 | 5 | 2 | 4 (4/0) | 6 |
| `DocumentTemplateSO` | class | Scripts/DocumentTemplateSO.cs:9 | 27 | 12 | 6 | 6 (6/0) | 2 |
| `EffectChannel` | enum | Scripts/EffectSO.cs:11 | 13 | 13 | 10 | 5 (5/0) | 0 |
| `EffectSO` | class | Scripts/EffectSO.cs:33 | 17 | 7 | 4 | 12 (12/0) | 26 |
| `EffectOp` | class | Scripts/EffectSO.cs:53 | 23 | 10 | 7 | 4 (4/0) | 0 |
| `EndingSO` | class | Scripts/Endings/EndingSO.cs:12 | 27 | 11 | 7 | 7 (7/0) | 6 |
| `EndingService` | class | Scripts/Endings/EndingService.cs:12 | 91 | 72 | 1 | 2 (2/0) | 0 |
| `EraSO` | class | Scripts/EraSO.cs:10 | 21 | 8 | 5 | 16 (16/0) | 6 |
| `GameManager` | class | Scripts/GameManager.cs:9 | 765 | 463 | 1 | 1 (1/0) | 5 |
| `HomeEconomy` | class | Scripts/Home/HomeEconomy.cs:10 | 159 | 104 | 4 | 2 (2/0) | 0 |
| `HomeEconomy.ExpenseReport` | struct | Scripts/Home/HomeEconomy.cs:13 | 26 | 16 | 6 | 2 (2/0) | 0 |
| `HomeManager` | class | Scripts/Home/HomeManager.cs:12 | 253 | 172 | 0 | 1 (1/0) | 1 |
| `SlotOutcomeSO` | class | Scripts/Home/SlotOutcomeSO.cs:11 | 37 | 18 | 10 | 3 (3/0) | 5 |
| `ReferenceBookSO` | class | Scripts/Investigation/ReferenceBookSO.cs:10 | 8 | 5 | 2 | 5 (5/0) | 6 |
| `TravelRuleType` | enum | Scripts/Investigation/TravelRuleSO.cs:6 | 11 | 6 | 3 | 2 (2/0) | 0 |
| `TravelRuleSO` | class | Scripts/Investigation/TravelRuleSO.cs:24 | 54 | 39 | 6 | 5 (5/0) | 6 |
| `LegendarySO` | class | Scripts/LegendarySO.cs:11 | 43 | 18 | 13 | 8 (8/0) | 10 |
| `BoothCoordinator` | class | Scripts/Office/BoothCoordinator.cs:22 | 207 | 158 | 3 | 3 (3/0) | 1 |
| `CinemachineCameraRig` | class | Scripts/Office/CinemachineCameraRig.cs:12 | 3 | 3 | 0 | 0 (0/0) | 3 |
| `DeskController` | class | Scripts/Office/Desk/DeskController.cs:31 | 506 | 374 | 15 | 4 (4/0) | 1 |
| `DeskDocument` | class | Scripts/Office/Desk/DeskDocument.cs:26 | 356 | 253 | 18 | 3 (3/0) | 1 |
| `DeskDocument.RowView` | class | Scripts/Office/Desk/DeskDocument.cs:56 | 11 | 11 | 8 | 0 (0/0) | 0 |
| `DeskDocument.PaperRowHighlight` | class | Scripts/Office/Desk/DeskDocument.cs:69 | 17 | 15 | 2 | 0 (0/0) | 0 |
| `DeskDraggable` | class | Scripts/Office/Desk/DeskDraggable.cs:16 | 70 | 45 | 9 | 3 (3/0) | 1 |
| `DeskItem` | class | Scripts/Office/Desk/DeskItem.cs:7 | 5 | 4 | 0 | 1 (1/0) | 1 |
| `DeskReaction` | class | Scripts/Office/Desk/DeskReaction.cs:15 | 86 | 61 | 4 | 3 (3/0) | 1 |
| `DeskReactionSO` | class | Scripts/Office/Desk/DeskReactionSO.cs:10 | 23 | 10 | 7 | 2 (2/0) | 10 |
| `DeskScanner` | class | Scripts/Office/Desk/DeskScanner.cs:11 | 38 | 23 | 5 | 3 (3/0) | 1 |
| `DeskSlotKind` | enum | Scripts/Office/Desk/DeskSlot.cs:4 | 8 | 5 | 2 | 1 (1/0) | 0 |
| `DeskSlot` | class | Scripts/Office/Desk/DeskSlot.cs:18 | 8 | 5 | 0 | 1 (1/0) | 1 |
| `DeskSurface` | class | Scripts/Office/Desk/DeskSurface.cs:12 | 50 | 35 | 4 | 8 (7/1) | 1 |
| `PaperExaminer` | class | Scripts/Office/Desk/PaperExaminer.cs:22 | 416 | 328 | 9 | 4 (4/0) | 1 |
| `PaperExaminer.Entry` | class | Scripts/Office/Desk/PaperExaminer.cs:34 | 15 | 15 | 12 | 3 (2/1) | 0 |
| `DeskConfigSO` | class | Scripts/Office/DeskConfigSO.cs:18 | 120 | 51 | 35 | 12 (12/0) | 1 |
| `DeskView` | class | Scripts/Office/DeskView.cs:27 | 219 | 161 | 9 | 5 (4/1) | 1 |
| `GlassFrame` | struct | Scripts/Office/GlassFrame.cs:9 | 70 | 42 | 9 | 2 (2/0) | 0 |
| `MonitorScreen` | class | Scripts/Office/MonitorScreen.cs:13 | 95 | 60 | 7 | 4 (4/0) | 1 |
| `NoteBacking` | class | Scripts/Office/NoteBacking.cs:12 | 45 | 29 | 0 | 1 (1/0) | 1 |
| `ResolvedAnchor` | class | Scripts/Office/OfficeAnchors.cs:6 | 29 | 12 | 9 | 2 (2/0) | 0 |
| `OfficeAnchors` | class | Scripts/Office/OfficeAnchors.cs:43 | 145 | 118 | 5 | 3 (3/0) | 0 |
| `OfficeHallCrowdPalette` (codex-light-touch) | class | Scripts/Office/OfficeHallCrowdPalette.cs:6 | 50 | 45 | 4 | 0 (0/0) | 1 |
| `OfficeHallCrowdPalette.PreviewMode` (codex-light-touch) | enum | Scripts/Office/OfficeHallCrowdPalette.cs:8 | 1 | 1 | 3 | 0 (0/0) | 0 |
| `OfficeHallCrowdPalette.Group` (codex-light-touch) | class | Scripts/Office/OfficeHallCrowdPalette.cs:9 | 6 | 6 | 3 | 0 (0/0) | 0 |
| `OfficeLayers` | class | Scripts/Office/OfficeLayers.cs:10 | 14 | 7 | 4 | 3 (3/0) | 0 |
| `OfficeReadouts` | class | Scripts/Office/OfficeReadouts.cs:14 | 77 | 52 | 1 | 2 (2/0) | 4 |
| `OfficeSceneBinder` | class | Scripts/Office/OfficeSceneBinder.cs:27 | 536 | 368 | 0 | 1 (1/0) | 1 |
| `OfficeSceneBinder.PropBinding` | class | Scripts/Office/OfficeSceneBinder.cs:31 | 14 | 7 | 4 | 1 (1/0) | 0 |
| `OfficeSceneContractSO` | class | Scripts/Office/OfficeSceneContractSO.cs:17 | 87 | 62 | 3 | 7 (7/0) | 1 |
| `OfficeSceneContractSO.AnchorSpec` | class | Scripts/Office/OfficeSceneContractSO.cs:21 | 32 | 19 | 7 | 1 (1/0) | 0 |
| `OfficeScenes` | class | Scripts/Office/OfficeScenes.cs:14 | 94 | 76 | 6 | 3 (3/0) | 0 |
| `OfficeTrafficVehicle` (codex-light-touch) | class | Scripts/Office/OfficeTrafficVehicle.cs:4 | 27 | 23 | 0 | 0 (0/0) | 2 |
| `OfficeView` | enum | Scripts/Office/OfficeViewController.cs:6 | 8 | 5 | 2 | 2 (2/0) | 0 |
| `OfficeViewController` | class | Scripts/Office/OfficeViewController.cs:21 | 43 | 27 | 4 | 4 (4/0) | 4 |
| `OfficeWindowGlass` (codex-light-touch) | class | Scripts/Office/OfficeWindowGlass.cs:5 | 66 | 54 | 1 | 0 (0/0) | 1 |
| `PcFrame` | class | Scripts/Office/PcFrame.cs:15 | 81 | 54 | 4 | 4 (4/0) | 1 |
| `PcScreenClone` | class | Scripts/Office/PcScreenClone.cs:11 | 114 | 85 | 4 | 3 (3/0) | 1 |
| `ShiftClockReadouts` | class | Scripts/Office/ShiftClockReadouts.cs:11 | 38 | 26 | 1 | 2 (2/0) | 1 |
| `OfficeUIController` | class | Scripts/OfficeUIController.cs:11 | 282 | 169 | 5 | 2 (2/0) | 5 |
| `ResolvedDaySchedule` | class | Scripts/ResolvedDaySchedule.cs:7 | 42 | 27 | 2 | 2 (2/0) | 0 |
| `ShiftClockDriver` | class | Scripts/Shift/ShiftClockDriver.cs:9 | 74 | 46 | 7 | 4 (4/0) | 1 |
| `ShiftScoring` | class | Scripts/Shift/ShiftScoring.cs:7 | 211 | 152 | 2 | 1 (1/0) | 0 |
| `ArchetypeSO` | class | Scripts/Timeline/ArchetypeSO.cs:12 | 21 | 9 | 5 | 6 (6/0) | 6 |
| `TimelineImpact` | class | Scripts/Timeline/ArchetypeSO.cs:40 | 14 | 7 | 4 | 5 (5/0) | 0 |
| `AttributeSO` | class | Scripts/Timeline/AttributeSO.cs:10 | 12 | 7 | 3 | 8 (8/0) | 3 |
| `HistoryService` | class | Scripts/Timeline/HistoryService.cs:11 | 201 | 137 | 5 | 4 (4/0) | 0 |
| `NationEraProfileSO` | class | Scripts/Timeline/NationEraProfileSO.cs:14 | 80 | 41 | 18 | 11 (11/0) | 48 |
| `ProfileFact` | class | Scripts/Timeline/NationEraProfileSO.cs:97 | 8 | 5 | 2 | 3 (3/0) | 0 |
| `AttributeBaseline` | class | Scripts/Timeline/NationEraProfileSO.cs:113 | 14 | 7 | 4 | 3 (3/0) | 0 |
| `NationSO` | class | Scripts/Timeline/NationSO.cs:10 | 11 | 6 | 3 | 15 (15/0) | 8 |
| `TimelineCueReceiver` | class | Scripts/Timeline/TimelineCueReceiver.cs:13 | 45 | 22 | 5 | 2 (2/0) | 0 |
| `TimelineEffects` | class | Scripts/Timeline/TimelineEffects.cs:10 | 145 | 93 | 7 | 6 (6/0) | 0 |
| `TimelineReactiveSprite` | class | Scripts/Timeline/TimelineReactiveSprite.cs:13 | 57 | 39 | 1 | 0 (0/0) | 3 |
| `TimelineReactiveSprite.CueSprite` | struct | Scripts/Timeline/TimelineReactiveSprite.cs:17 | 8 | 5 | 2 | 0 (0/0) | 0 |
| `TimelineKeys` | class | Scripts/Timeline/TimelineService.cs:9 | 17 | 8 | 5 | 1 (1/0) | 0 |
| `TimelineService` | class | Scripts/Timeline/TimelineService.cs:39 | 546 | 351 | 7 | 4 (4/0) | 0 |
| `TimelineTriggerSO` | class | Scripts/Timeline/TimelineTriggerSO.cs:16 | 27 | 12 | 8 | 5 (5/0) | 18 |
| `TriggerCondition` | class | Scripts/Timeline/TimelineTriggerSO.cs:46 | 20 | 9 | 6 | 5 (5/0) | 0 |
| `TriggerOutcome` | class | Scripts/Timeline/TimelineTriggerSO.cs:69 | 8 | 5 | 2 | 4 (4/0) | 0 |
| `TitleSceneController` | class | Scripts/TitleSceneController.cs:18 | 75 | 56 | 0 | 1 (1/0) | 1 |
| `TodaysWorld` | class | Scripts/TodaysWorld.cs:8 | 15 | 10 | 3 | 3 (3/0) | 0 |
| `CitizenRecordsWindowController` | class | Scripts/UI/CitizenRecordsWindowController.cs:11 | 113 | 90 | 2 | 2 (2/0) | 3 |
| `ClickCatcher` | class | Scripts/UI/ClickCatcher.cs:12 | 12 | 9 | 2 | 5 (4/1) | 1 |
| `Clickable` | class | Scripts/UI/Clickable.cs:15 | 27 | 14 | 5 | 8 (8/0) | 4 |
| `CompareController` | class | Scripts/UI/CompareController.cs:17 | 159 | 101 | 6 | 7 (7/0) | 4 |
| `ICompareHighlight` | interface | Scripts/UI/CompareHighlights.cs:12 | 5 | 4 | 1 | 5 (5/0) | 0 |
| `ImageHighlight` | class | Scripts/UI/CompareHighlights.cs:19 | 29 | 24 | 2 | 5 (5/0) | 0 |
| `DayFlowUIController` | class | Scripts/UI/DayFlowUIController.cs:12 | 165 | 105 | 4 | 2 (2/0) | 4 |
| `DesktopIcon` | class | Scripts/UI/DesktopIcon.cs:9 | 48 | 32 | 2 | 2 (2/0) | 4 |
| `DesktopShell` | class | Scripts/UI/DesktopShell.cs:11 | 74 | 51 | 4 | 1 (1/0) | 4 |
| `DocumentRowView` | class | Scripts/UI/DocumentRowView.cs:16 | 48 | 36 | 1 | 2 (2/0) | 0 |
| `DocumentReveal` | class | Scripts/UI/DocumentRowView.cs:66 | 12 | 10 | 1 | 1 (1/0) | 0 |
| `DocumentWindowController` | class | Scripts/UI/DocumentWindowController.cs:27 | 179 | 127 | 3 | 2 (2/0) | 4 |
| `DraggableWindow` | class | Scripts/UI/DraggableWindow.cs:15 | 103 | 73 | 8 | 1 (1/0) | 4 |
| `EvidencePicks` | class | Scripts/UI/EvidencePicks.cs:10 | 41 | 32 | 5 | 5 (5/0) | 0 |
| `HomeUIController` | class | Scripts/UI/HomeUIController.cs:18 | 544 | 326 | 9 | 2 (2/0) | 1 |
| `HoverHighlighter` | class | Scripts/UI/HoverHighlighter.cs:21 | 233 | 170 | 1 | 1 (1/0) | 0 |
| `HoverHint` | class | Scripts/UI/HoverHint.cs:9 | 21 | 13 | 2 | 1 (1/0) | 1 |
| `HoverUIOutline` | class | Scripts/UI/HoverUIOutline.cs:14 | 40 | 32 | 3 | 1 (1/0) | 0 |
| `InteractionFeedbackBootstrap` | class | Scripts/UI/InteractionFeedbackBootstrap.cs:9 | 28 | 22 | 0 | 0 (0/0) | 0 |
| `InteractionFeedbackSO` | class | Scripts/UI/InteractionFeedbackSO.cs:8 | 35 | 15 | 9 | 4 (4/0) | 1 |
| `InteractionAction` | struct | Scripts/UI/InteractionPanelController.cs:8 | 14 | 7 | 4 | 1 (1/0) | 0 |
| `InteractionPanelController` | class | Scripts/UI/InteractionPanelController.cs:30 | 100 | 65 | 2 | 3 (3/0) | 3 |
| `InvestigationUIController` | class | Scripts/UI/InvestigationUIController.cs:33 | 960 | 640 | 13 | 2 (2/0) | 4 |
| `OSWindowChrome` | class | Scripts/UI/OSWindowChrome.cs:10 | 84 | 62 | 4 | 4 (4/0) | 4 |
| `OfficeCaseHud` | class | Scripts/UI/OfficeCaseHud.cs:12 | 33 | 23 | 2 | 4 (4/0) | 1 |
| `OverlayCallout` | class | Scripts/UI/OverlayCallout.cs:15 | 85 | 57 | 5 | 5 (5/0) | 1 |
| `OverlayProjection` | class | Scripts/UI/OverlayProjection.cs:16 | 48 | 35 | 2 | 3 (3/0) | 0 |
| `PagedRowsWindow` | class | Scripts/UI/PagedRowsWindow.cs:13 | 99 | 59 | 6 | 2 (2/0) | 0 |
| `RadialLayoutGroup` | class | Scripts/UI/RadialLayoutGroup.cs:10 | 65 | 48 | 6 | 2 (2/0) | 1 |
| `RectHoleRaycastFilter` | class | Scripts/UI/RectHoleRaycastFilter.cs:10 | 9 | 6 | 1 | 1 (1/0) | 1 |
| `ReferenceBookWindowController` | class | Scripts/UI/ReferenceBookWindowController.cs:14 | 41 | 30 | 3 | 2 (2/0) | 4 |
| `SpeechBubbleInput` | class | Scripts/UI/SpeechBubbleInput.cs:12 | 35 | 25 | 2 | 1 (1/0) | 1 |
| `StampTray` | class | Scripts/UI/StampTray.cs:20 | 116 | 76 | 9 | 5 (5/0) | 1 |
| `CultureThemeBootstrap` | class | Scripts/UI/Theme/CultureThemeBootstrap.cs:10 | 28 | 23 | 0 | 0 (0/0) | 0 |
| `CultureThemeService` | class | Scripts/UI/Theme/CultureThemeService.cs:21 | 312 | 220 | 15 | 8 (8/0) | 0 |
| `CultureUiSettings` | class | Scripts/UI/Theme/CultureUiSettings.cs:11 | 17 | 8 | 5 | 4 (4/0) | 0 |
| `RuntimeFonts` | class | Scripts/UI/Theme/RuntimeFonts.cs:20 | 181 | 140 | 4 | 2 (2/0) | 0 |
| `RuntimeFonts.Result` | struct | Scripts/UI/Theme/RuntimeFonts.cs:26 | 27 | 16 | 6 | 2 (2/0) | 0 |
| `SettingsWindowController` | class | Scripts/UI/Theme/SettingsWindowController.cs:14 | 71 | 53 | 0 | 1 (1/0) | 1 |
| `ThemeSO` | class | Scripts/UI/Theme/ThemeSO.cs:14 | 60 | 29 | 13 | 9 (9/0) | 9 |
| `FontCandidate` | class | Scripts/UI/Theme/ThemeSO.cs:77 | 14 | 7 | 4 | 4 (4/0) | 0 |
| `PaletteEntry` | class | Scripts/UI/Theme/ThemeSO.cs:94 | 20 | 9 | 6 | 4 (4/0) | 0 |
| `ThemePart` | enum | Scripts/UI/Theme/ThemeTag.cs:5 | 8 | 5 | 2 | 4 (4/0) | 0 |
| `ThemeTextKind` | enum | Scripts/UI/Theme/ThemeTag.cs:15 | 11 | 6 | 3 | 4 (4/0) | 0 |
| `ThemeTag` | class | Scripts/UI/Theme/ThemeTag.cs:36 | 60 | 31 | 8 | 5 (5/0) | 1 |
| `UiLanguagePreference` | class | Scripts/UI/Theme/UiLanguagePreference.cs:10 | 22 | 15 | 1 | 2 (2/0) | 0 |
| `UiStringTableSO` | class | Scripts/UI/Theme/UiStringTableSO.cs:11 | 11 | 6 | 3 | 6 (6/0) | 7 |
| `UiText` | class | Scripts/UI/Theme/UiText.cs:13 | 113 | 72 | 7 | 23 (23/0) | 0 |
| `UiText.WalletForm` | enum | Scripts/UI/Theme/UiText.cs:16 | 11 | 6 | 3 | 7 (7/0) | 0 |
| `TitleUIController` | class | Scripts/UI/TitleUIController.cs:13 | 99 | 59 | 4 | 2 (2/0) | 1 |
| `TranscriptWindowController` | class | Scripts/UI/TranscriptWindowController.cs:18 | 48 | 35 | 4 | 2 (2/0) | 1 |
| `CaseTranslation` | class | Scripts/UI/Translation/CaseTranslation.cs:11 | 78 | 45 | 14 | 10 (10/0) | 0 |
| `MotionPreference` | class | Scripts/UI/Translation/MotionPreference.cs:9 | 22 | 15 | 1 | 3 (3/0) | 0 |
| `TextFlip` | class | Scripts/UI/Translation/TextFlip.cs:15 | 120 | 92 | 6 | 5 (5/0) | 0 |
| `TranslationPresenter` | class | Scripts/UI/Translation/TranslationPresenter.cs:17 | 93 | 78 | 2 | 1 (1/0) | 0 |
| `TranslationScript` | class | Scripts/UI/Translation/TranslationSettings.cs:7 | 11 | 6 | 3 | 2 (2/0) | 0 |
| `TranslationSettings` | class | Scripts/UI/Translation/TranslationSettings.cs:26 | 66 | 42 | 7 | 6 (6/0) | 0 |
| `TravellerWheel` | class | Scripts/UI/TravellerWheel.cs:28 | 379 | 252 | 15 | 6 (6/0) | 1 |
| `TravellerWheel.BubbleHighlight` | class | Scripts/UI/TravellerWheel.cs:61 | 17 | 15 | 2 | 0 (0/0) | 0 |
| `UpgradeSO` | class | Scripts/UpgradeSO.cs:9 | 19 | 10 | 5 | 8 (8/0) | 12 |
| `WorldState` | class | Scripts/WorldState.cs:10 | 157 | 61 | 25 | 20 (20/0) | 0 |
| `RunPhase` | enum | Scripts/WorldState.cs:169 | 8 | 5 | 2 | 2 (2/0) | 0 |
| `CounterEntry` | class | Scripts/WorldState.cs:180 | 8 | 5 | 2 | 1 (1/0) | 0 |
| `TimelineStateData` | class | Scripts/WorldState.cs:194 | 47 | 32 | 6 | 0 (0/0) | 0 |
| `ScoreEntry` | class | Scripts/WorldState.cs:244 | 8 | 5 | 2 | 1 (1/0) | 0 |
| `ActiveEffectEntry` | class | Scripts/WorldState.cs:258 | 18 | 9 | 5 | 3 (3/0) | 0 |
| `FamilyStateData` | class | Scripts/WorldState.cs:279 | 5 | 4 | 1 | 0 (0/0) | 0 |
| `FamilyMemberData` | class | Scripts/WorldState.cs:287 | 8 | 5 | 2 | 3 (3/0) | 0 |
| `TomorrowPackage` | class | Scripts/WorldState.cs:301 | 8 | 5 | 2 | 0 (0/0) | 0 |

## Assembly-CSharp-Editor (62 types)

| Type | Kind | File | Lines | Code | Public members | Code callers (prod/test) | Asset refs |
|---|---|---|---:|---:|---:|---|---:|
| `CharacterArtImporter` | class | Editor/CharacterArtImporter.cs:12 | 35 | 28 | 0 | 0 (0/0) | 0 |
| `ContentLibraryValidator` | class | Editor/ContentLibraryValidator.cs:19 | 1290 | 967 | 6 | 2 (2/0) | 0 |
| `HomeSceneBuilder` | class | Editor/HomeSceneBuilder.cs:20 | 343 | 253 | 1 | 0 (0/0) | 0 |
| `OfficeSceneContractTools` | class | Editor/OfficeSceneContractTools.cs:18 | 119 | 88 | 3 | 0 (0/0) | 0 |
| `OfficeSceneUIBuilder` | class | Editor/OfficeSceneUIBuilder.Desk.cs:26, Editor/OfficeSceneUIBuilder.cs:56 | 3107 | 2256 | 1 | 0 (0/0) | 0 |
| `OfficeSceneUIBuilder.FallbackHud` | struct | Editor/OfficeSceneUIBuilder.Desk.cs:1157 | 8 | 8 | 5 | 1 (1/0) | 0 |
| `OfficeSceneUIBuilder.WindowShell` | struct | Editor/OfficeSceneUIBuilder.cs:577 | 9 | 9 | 6 | 0 (0/0) | 0 |
| `PlaceholderPng` | class | Editor/PlaceholderPng.cs:12 | 49 | 39 | 3 | 3 (3/0) | 0 |
| `SerializedArrays` | class | Editor/SerializedArrays.cs:9 | 38 | 32 | 2 | 3 (3/0) | 0 |
| `TitleSceneBuilder` | class | Editor/TitleSceneBuilder.cs:17 | 215 | 159 | 1 | 0 (0/0) | 0 |
| `UiContrastCheck` | class | Editor/UiContrastCheck.cs:25 | 207 | 174 | 1 | 2 (2/0) | 0 |
| `WorldContentGenerator` | class | Editor/WorldContentGenerator.Culture.cs:16, Editor/WorldContentGenerator.Translation.cs:13, Editor/WorldContentGenerator.cs:40 | 2377 | 1849 | 2 | 1 (1/0) | 0 |
| `WorldContentGenerator.CulturePlan` | class | Editor/WorldContentGenerator.Culture.cs:34 | 8 | 8 | 5 | 1 (1/0) | 0 |
| `WorldContentGenerator.ThemePlan` | class | Editor/WorldContentGenerator.Culture.cs:44 | 9 | 9 | 8 | 0 (0/0) | 0 |
| `WorldContentGenerator.UiData` | class | Editor/WorldContentGenerator.Culture.cs:321 | 12 | 12 | 9 | 1 (1/0) | 0 |
| `WorldContentGenerator.RoleRuleData` | class | Editor/WorldContentGenerator.Culture.cs:335 | 1 | 1 | 5 | 0 (0/0) | 0 |
| `WorldContentGenerator.CultureData` | class | Editor/WorldContentGenerator.Culture.cs:338 | 14 | 14 | 11 | 1 (1/0) | 0 |
| `WorldContentGenerator.FontData` | class | Editor/WorldContentGenerator.Culture.cs:354 | 1 | 1 | 4 | 1 (1/0) | 0 |
| `WorldContentGenerator.NamedHexData` | class | Editor/WorldContentGenerator.Culture.cs:357 | 1 | 1 | 2 | 0 (0/0) | 0 |
| `WorldContentGenerator.StringData` | class | Editor/WorldContentGenerator.Culture.cs:360 | 1 | 1 | 4 | 1 (1/0) | 0 |
| `WorldContentGenerator.LanguageData` | class | Editor/WorldContentGenerator.Culture.cs:363 | 1 | 1 | 3 | 0 (0/0) | 0 |
| `WorldContentGenerator.EntryData` | class | Editor/WorldContentGenerator.Culture.cs:366 | 1 | 1 | 2 | 0 (0/0) | 0 |
| `WorldContentGenerator.TranslationData` | class | Editor/WorldContentGenerator.Translation.cs:140 | 10 | 10 | 7 | 1 (1/0) | 0 |
| `WorldContentGenerator.ScriptData` | class | Editor/WorldContentGenerator.Translation.cs:152 | 1 | 1 | 3 | 0 (0/0) | 0 |
| `WorldContentGenerator.PackData` | class | Editor/WorldContentGenerator.Translation.cs:155 | 1 | 1 | 4 | 1 (1/0) | 0 |
| `WorldContentGenerator.Authored` | class | Editor/WorldContentGenerator.cs:159 | 9 | 9 | 6 | 1 (1/0) | 0 |
| `WorldContentGenerator.ConditionRefs` | class | Editor/WorldContentGenerator.cs:1510 | 13 | 12 | 4 | 0 (0/0) | 0 |
| `WorldContentGenerator.WorldSource` | class | Editor/WorldContentGenerator.cs:1708 | 19 | 19 | 16 | 2 (2/0) | 0 |
| `WorldContentGenerator.LooksData` | class | Editor/WorldContentGenerator.cs:1729 | 7 | 7 | 4 | 0 (0/0) | 0 |
| `WorldContentGenerator.FaceBandData` | class | Editor/WorldContentGenerator.cs:1737 | 1 | 1 | 2 | 0 (0/0) | 0 |
| `WorldContentGenerator.ConfusableData` | class | Editor/WorldContentGenerator.cs:1740 | 1 | 1 | 4 | 0 (0/0) | 0 |
| `WorldContentGenerator.LooksWeightData` | class | Editor/WorldContentGenerator.cs:1743 | 1 | 1 | 2 | 0 (0/0) | 0 |
| `WorldContentGenerator.HairWeightData` | class | Editor/WorldContentGenerator.cs:1745 | 1 | 1 | 2 | 0 (0/0) | 0 |
| `WorldContentGenerator.WardrobeData` | class | Editor/WorldContentGenerator.cs:1747 | 1 | 1 | 2 | 0 (0/0) | 0 |
| `WorldContentGenerator.GenderLookData` | class | Editor/WorldContentGenerator.cs:1750 | 9 | 9 | 6 | 0 (0/0) | 0 |
| `WorldContentGenerator.ItemData` | class | Editor/WorldContentGenerator.cs:1761 | 1 | 1 | 6 | 0 (0/0) | 0 |
| `WorldContentGenerator.PremadeData` | class | Editor/WorldContentGenerator.cs:1764 | 15 | 15 | 12 | 0 (0/0) | 0 |
| `WorldContentGenerator.ImpactData` | class | Editor/WorldContentGenerator.cs:1781 | 1 | 1 | 4 | 0 (0/0) | 0 |
| `WorldContentGenerator.ForcedData` | class | Editor/WorldContentGenerator.cs:1784 | 1 | 1 | 3 | 0 (0/0) | 0 |
| `WorldContentGenerator.ContentData` | class | Editor/WorldContentGenerator.cs:1787 | 9 | 9 | 6 | 0 (0/0) | 0 |
| `WorldContentGenerator.AttributeData` | class | Editor/WorldContentGenerator.cs:1797 | 1 | 1 | 2 | 0 (0/0) | 0 |
| `WorldContentGenerator.EraData` | class | Editor/WorldContentGenerator.cs:1800 | 1 | 1 | 5 | 0 (0/0) | 0 |
| `WorldContentGenerator.CountryData` | class | Editor/WorldContentGenerator.cs:1802 | 1 | 1 | 5 | 1 (1/0) | 0 |
| `WorldContentGenerator.BaselineData` | class | Editor/WorldContentGenerator.cs:1804 | 1 | 1 | 2 | 0 (0/0) | 0 |
| `WorldContentGenerator.FactData` | class | Editor/WorldContentGenerator.cs:1806 | 1 | 1 | 2 | 0 (0/0) | 0 |
| `WorldContentGenerator.PlaceData` | class | Editor/WorldContentGenerator.cs:1809 | 14 | 14 | 11 | 0 (0/0) | 0 |
| `WorldContentGenerator.RuleData` | class | Editor/WorldContentGenerator.cs:1824 | 1 | 1 | 5 | 0 (0/0) | 0 |
| `WorldContentGenerator.EraWeightData` | class | Editor/WorldContentGenerator.cs:1826 | 1 | 1 | 2 | 0 (0/0) | 0 |
| `WorldContentGenerator.DayData` | class | Editor/WorldContentGenerator.cs:1828 | 19 | 14 | 11 | 0 (0/0) | 0 |
| `WorldContentGenerator.InterviewData` | class | Editor/WorldContentGenerator.cs:1849 | 21 | 21 | 18 | 0 (0/0) | 0 |
| `WorldContentGenerator.RequestData` | class | Editor/WorldContentGenerator.cs:1872 | 1 | 1 | 4 | 0 (0/0) | 0 |
| `WorldContentGenerator.QuestionData` | class | Editor/WorldContentGenerator.cs:1875 | 12 | 12 | 9 | 0 (0/0) | 0 |
| `WorldContentGenerator.OverrideData` | class | Editor/WorldContentGenerator.cs:1888 | 1 | 1 | 3 | 0 (0/0) | 0 |
| `WorldContentGenerator.ConditionData` | class | Editor/WorldContentGenerator.cs:1891 | 1 | 1 | 6 | 0 (0/0) | 0 |
| `WorldContentGenerator.HistoryData` | class | Editor/WorldContentGenerator.cs:1894 | 1 | 1 | 2 | 0 (0/0) | 0 |
| `WorldContentGenerator.HistoryLinesData` | class | Editor/WorldContentGenerator.cs:1897 | 1 | 1 | 3 | 0 (0/0) | 0 |
| `WorldContentGenerator.HistoryRuleData` | class | Editor/WorldContentGenerator.cs:1900 | 1 | 1 | 5 | 0 (0/0) | 0 |
| `WorldContentGenerator.EditData` | class | Editor/WorldContentGenerator.cs:1903 | 1 | 1 | 3 | 0 (0/0) | 0 |
| `WorldContentGenerator.DialogData` | class | Editor/WorldContentGenerator.cs:1906 | 8 | 8 | 5 | 0 (0/0) | 0 |
| `WorldContentGenerator.NodeData` | class | Editor/WorldContentGenerator.cs:1915 | 1 | 1 | 3 | 0 (0/0) | 0 |
| `WorldContentGenerator.LineData` | class | Editor/WorldContentGenerator.cs:1917 | 1 | 1 | 4 | 0 (0/0) | 0 |
| `WorldContentGenerator.ChoiceData` | class | Editor/WorldContentGenerator.cs:1919 | 1 | 1 | 5 | 0 (0/0) | 0 |

## TimeDesk.Domain (151 types)

| Type | Kind | File | Lines | Code | Public members | Code callers (prod/test) | Asset refs |
|---|---|---|---:|---:|---:|---|---:|
| `BirthDates` | class | Scripts/Domain/BirthDates.cs:8 | 120 | 74 | 6 | 8 (6/2) | 0 |
| `BoothPhase` | enum | Scripts/Domain/BoothRules.cs:2 | 11 | 6 | 3 | 3 (2/1) | 0 |
| `BoothContext` | struct | Scripts/Domain/BoothRules.cs:15 | 39 | 22 | 9 | 2 (1/1) | 0 |
| `BoothInput` | struct | Scripts/Domain/BoothRules.cs:56 | 80 | 45 | 18 | 2 (1/1) | 0 |
| `BoothRules` | class | Scripts/Domain/BoothRules.cs:150 | 33 | 32 | 1 | 2 (1/1) | 0 |
| `Carries` | class | Scripts/Domain/Carries.cs:9 | 90 | 62 | 2 | 2 (1/1) | 0 |
| `CitizenRecord` | class | Scripts/Domain/CitizenRegistry.cs:8 | 14 | 7 | 4 | 4 (3/1) | 0 |
| `CitizenRegistry` | class | Scripts/Domain/CitizenRegistry.cs:29 | 42 | 25 | 5 | 4 (3/1) | 0 |
| `ClueCategory` | enum | Scripts/Domain/ClueCategory.cs:6 | 17 | 12 | 9 | 48 (33/15) | 0 |
| `ClueLabels` | class | Scripts/Domain/ClueLabels.cs:7 | 5 | 4 | 1 | 3 (2/1) | 0 |
| `ComparePick` | struct | Scripts/Domain/ComparePair.cs:8 | 23 | 14 | 5 | 6 (5/1) | 0 |
| `CompareStep` | enum | Scripts/Domain/ComparePair.cs:33 | 11 | 6 | 3 | 2 (1/1) | 0 |
| `ComparePair` | class | Scripts/Domain/ComparePair.cs:52 | 58 | 36 | 7 | 2 (1/1) | 0 |
| `PickKeys` | class | Scripts/Domain/ComparePair.cs:116 | 17 | 8 | 5 | 2 (1/1) | 0 |
| `CultureCue` | class | Scripts/Domain/CultureCue.cs:8 | 45 | 31 | 4 | 4 (3/1) | 0 |
| `DayPlans` | class | Scripts/Domain/DayPlans.cs:4 | 25 | 17 | 1 | 4 (3/1) | 0 |
| `DaySlotSequencer` | class | Scripts/Domain/DaySlotSequencer.cs:10 | 53 | 30 | 12 | 2 (1/1) | 0 |
| `DocumentHandOver` | enum | Scripts/Domain/DeskPapers.cs:4 | 8 | 5 | 2 | 5 (2/3) | 0 |
| `DocumentHandOvers` | class | Scripts/Domain/DeskPapers.cs:14 | 5 | 4 | 1 | 2 (1/1) | 0 |
| `CaseDocument` | class | Scripts/Domain/DeskPapers.cs:21 | 17 | 8 | 5 | 7 (4/3) | 0 |
| `CaseDocuments` | class | Scripts/Domain/DeskPapers.cs:40 | 13 | 12 | 1 | 2 (1/1) | 0 |
| `DropOutcome` | enum | Scripts/Domain/DeskPapers.cs:55 | 11 | 6 | 3 | 2 (1/1) | 0 |
| `ExamineSlot` | enum | Scripts/Domain/DeskPapers.cs:68 | 11 | 6 | 3 | 2 (1/1) | 0 |
| `HoldResult` | struct | Scripts/Domain/DeskPapers.cs:81 | 19 | 12 | 4 | 2 (1/1) | 0 |
| `DeskPapers` | class | Scripts/Domain/DeskPapers.cs:112 | 232 | 147 | 16 | 2 (1/1) | 0 |
| `DeskPapers.PaperState` | enum | Scripts/Domain/DeskPapers.cs:118 | 17 | 8 | 5 | 0 (0/0) | 0 |
| `HeldCover` | class | Scripts/Domain/DeskPapers.cs:351 | 10 | 9 | 1 | 2 (1/1) | 0 |
| `PaperClickAction` | enum | Scripts/Domain/DeskPapers.cs:363 | 14 | 7 | 4 | 2 (1/1) | 0 |
| `PaperClicks` | class | Scripts/Domain/DeskPapers.cs:379 | 17 | 11 | 1 | 2 (1/1) | 0 |
| `DeskHints` | class | Scripts/Domain/DeskPapers.cs:398 | 10 | 7 | 2 | 3 (2/1) | 0 |
| `DialogSpeaker` | enum | Scripts/Domain/Dialog.cs:5 | 8 | 5 | 2 | 9 (6/3) | 0 |
| `DialogAction` | enum | Scripts/Domain/Dialog.cs:15 | 14 | 7 | 4 | 3 (2/1) | 0 |
| `DialogChoiceKind` | enum | Scripts/Domain/Dialog.cs:35 | 20 | 9 | 6 | 5 (3/2) | 0 |
| `DialogChoiceKinds` | class | Scripts/Domain/Dialog.cs:57 | 44 | 32 | 3 | 3 (2/1) | 0 |
| `DialogLine` | class | Scripts/Domain/Dialog.cs:103 | 48 | 28 | 10 | 7 (5/2) | 0 |
| `DialogChoice` | class | Scripts/Domain/Dialog.cs:153 | 35 | 14 | 11 | 5 (2/3) | 0 |
| `DialogNode` | class | Scripts/Domain/Dialog.cs:190 | 11 | 6 | 3 | 3 (1/2) | 0 |
| `DialogGraph` | class | Scripts/Domain/Dialog.cs:203 | 29 | 19 | 4 | 5 (2/3) | 0 |
| `DialogRunner` | class | Scripts/Domain/Dialog.cs:237 | 83 | 61 | 4 | 3 (1/2) | 0 |
| `EvidenceKind` | enum | Scripts/Domain/DiscrepancyLog.cs:5 | 20 | 9 | 6 | 5 (1/4) | 0 |
| `DiscrepancyProof` | enum | Scripts/Domain/DiscrepancyLog.cs:27 | 11 | 6 | 3 | 4 (1/3) | 0 |
| `CompareEvidence` | struct | Scripts/Domain/DiscrepancyLog.cs:45 | 77 | 48 | 13 | 9 (5/4) | 0 |
| `Discrepancy` | class | Scripts/Domain/DiscrepancyLog.cs:124 | 48 | 19 | 9 | 6 (3/3) | 0 |
| `DiscrepancyLog` | class | Scripts/Domain/DiscrepancyLog.cs:186 | 136 | 85 | 6 | 14 (11/3) | 0 |
| `DocumentFieldSpec` | class | Scripts/Domain/DocumentField.cs:10 | 11 | 6 | 3 | 2 (2/0) | 0 |
| `DocumentField` | class | Scripts/Domain/DocumentField.cs:29 | 17 | 8 | 5 | 15 (9/6) | 0 |
| `DocumentRow` | struct | Scripts/Domain/DocumentRows.cs:8 | 19 | 12 | 4 | 7 (6/1) | 0 |
| `DocumentRows` | class | Scripts/Domain/DocumentRows.cs:33 | 68 | 55 | 4 | 5 (4/1) | 0 |
| `EffectOpType` | enum | Scripts/Domain/EffectOps.cs:6 | 62 | 21 | 18 | 8 (7/1) | 0 |
| `EffectOps` | class | Scripts/Domain/EffectOps.cs:70 | 35 | 20 | 2 | 3 (2/1) | 0 |
| `EndingConditionType` | enum | Scripts/Domain/EndingRules.cs:7 | 14 | 7 | 4 | 4 (3/1) | 0 |
| `EndingKind` | enum | Scripts/Domain/EndingRules.cs:23 | 11 | 6 | 3 | 1 (0/1) | 0 |
| `EndingMoment` | enum | Scripts/Domain/EndingRules.cs:36 | 8 | 5 | 2 | 4 (3/1) | 0 |
| `EndingCandidate` | struct | Scripts/Domain/EndingRules.cs:46 | 19 | 12 | 4 | 2 (1/1) | 0 |
| `EndingRules` | class | Scripts/Domain/EndingRules.cs:71 | 41 | 30 | 2 | 2 (1/1) | 0 |
| `FactRow` | struct | Scripts/Domain/FactTable.cs:8 | 31 | 18 | 7 | 6 (4/2) | 0 |
| `FactTable` | class | Scripts/Domain/FactTable.cs:49 | 103 | 58 | 8 | 18 (12/6) | 0 |
| `Forgery` | class | Scripts/Domain/Forgery.cs:8 | 52 | 31 | 2 | 4 (3/1) | 0 |
| `TriggerConditionType` | enum | Scripts/Domain/Gates.cs:7 | 51 | 17 | 14 | 6 (4/2) | 0 |
| `GateCondition` | struct | Scripts/Domain/Gates.cs:60 | 19 | 12 | 4 | 3 (1/2) | 0 |
| `GateSnapshot` | class | Scripts/Domain/Gates.cs:87 | 85 | 50 | 10 | 6 (3/3) | 0 |
| `Gates` | class | Scripts/Domain/Gates.cs:178 | 61 | 45 | 4 | 5 (4/1) | 0 |
| `Gated<T>` | struct | Scripts/Domain/Gates.cs:241 | 15 | 10 | 3 | 3 (2/1) | 0 |
| `FlagKeys` | class | Scripts/Domain/Gates.cs:258 | 11 | 6 | 3 | 7 (5/2) | 0 |
| `EditCause` | enum | Scripts/Domain/History.cs:5 | 8 | 5 | 2 | 8 (5/3) | 0 |
| `FactEdit` | class | Scripts/Domain/History.cs:16 | 40 | 23 | 9 | 10 (7/3) | 0 |
| `CarryRecord` | class | Scripts/Domain/History.cs:59 | 23 | 10 | 7 | 4 (3/1) | 0 |
| `HistoryState` | class | Scripts/Domain/History.cs:89 | 17 | 8 | 5 | 7 (5/2) | 0 |
| `HistoryLines` | class | Scripts/Domain/History.cs:109 | 11 | 6 | 3 | 3 (3/0) | 0 |
| `History` | class | Scripts/Domain/History.cs:126 | 74 | 36 | 9 | 9 (8/1) | 0 |
| `HistoryChecks` | class | Scripts/Domain/HistoryChecks.cs:8 | 66 | 49 | 1 | 3 (2/1) | 0 |
| `IRandomSource` | interface | Scripts/Domain/IRandomSource.cs:6 | 8 | 5 | 2 | 14 (10/4) | 0 |
| `PlaceRef` | struct | Scripts/Domain/Influence.cs:5 | 15 | 10 | 3 | 2 (1/1) | 0 |
| `Influence` | class | Scripts/Domain/Influence.cs:26 | 47 | 32 | 1 | 2 (1/1) | 0 |
| `InterviewAnswer` | class | Scripts/Domain/Interview.cs:4 | 11 | 6 | 3 | 8 (4/4) | 0 |
| `Interview` | class | Scripts/Domain/Interview.cs:22 | 115 | 65 | 13 | 9 (8/1) | 0 |
| `LineText` | class | Scripts/Domain/InterviewContent.cs:11 | 20 | 13 | 4 | 11 (8/3) | 0 |
| `ScriptLine` | class | Scripts/Domain/InterviewContent.cs:34 | 14 | 7 | 4 | 3 (2/1) | 0 |
| `ScriptChoice` | class | Scripts/Domain/InterviewContent.cs:51 | 17 | 8 | 5 | 6 (3/3) | 0 |
| `ScriptNode` | class | Scripts/Domain/InterviewContent.cs:71 | 11 | 6 | 3 | 6 (3/3) | 0 |
| `AuthoredDialog` | class | Scripts/Domain/InterviewContent.cs:88 | 14 | 7 | 4 | 10 (7/3) | 0 |
| `WordingOverride` | class | Scripts/Domain/InterviewContent.cs:105 | 11 | 6 | 3 | 3 (2/1) | 0 |
| `InterviewQuestion` | class | Scripts/Domain/InterviewContent.cs:119 | 47 | 28 | 8 | 10 (7/3) | 0 |
| `InterviewLines` | class | Scripts/Domain/InterviewContent.cs:175 | 53 | 20 | 17 | 11 (7/4) | 0 |
| `InterviewRequest` | class | Scripts/Domain/InterviewContent.cs:235 | 14 | 7 | 4 | 5 (3/2) | 0 |
| `InterviewDay` | class | Scripts/Domain/InterviewDay.cs:11 | 125 | 83 | 8 | 4 (3/1) | 0 |
| `DialogOutcomes` | class | Scripts/Domain/InterviewDay.cs:142 | 41 | 32 | 2 | 2 (1/1) | 0 |
| `InterviewCase` | class | Scripts/Domain/InterviewScript.cs:4 | 23 | 10 | 7 | 3 (1/2) | 0 |
| `InterviewScript` | class | Scripts/Domain/InterviewScript.cs:35 | 277 | 196 | 11 | 4 (2/2) | 0 |
| `DialogChecks` | class | Scripts/Domain/InterviewScript.cs:318 | 156 | 116 | 2 | 4 (3/1) | 0 |
| `HomeCandidate` | struct | Scripts/Domain/Lies.cs:4 | 27 | 16 | 6 | 5 (2/3) | 0 |
| `LieOutcome` | enum | Scripts/Domain/Lies.cs:33 | 11 | 6 | 3 | 3 (1/2) | 0 |
| `TellChannel` | enum | Scripts/Domain/Lies.cs:46 | 11 | 6 | 3 | 7 (5/2) | 0 |
| `LiePlan` | class | Scripts/Domain/Lies.cs:59 | 64 | 37 | 6 | 4 (2/2) | 0 |
| `Lies` | class | Scripts/Domain/Lies.cs:132 | 172 | 116 | 2 | 3 (1/2) | 0 |
| `Lies.TellOption` | struct | Scripts/Domain/Lies.cs:135 | 15 | 10 | 3 | 0 (0/0) | 0 |
| `LookSlot` | enum | Scripts/Domain/LookData.cs:10 | 17 | 8 | 5 | 10 (6/4) | 0 |
| `LookItem` | class | Scripts/Domain/LookData.cs:30 | 30 | 11 | 8 | 6 (4/2) | 0 |
| `GenderLook` | class | Scripts/Domain/LookData.cs:63 | 36 | 21 | 8 | 6 (4/2) | 0 |
| `PlaceWardrobe` | class | Scripts/Domain/LookData.cs:102 | 12 | 7 | 3 | 6 (4/2) | 0 |
| `HairColourWeight` | class | Scripts/Domain/LookData.cs:117 | 8 | 5 | 2 | 3 (2/1) | 0 |
| `LookWeights` | class | Scripts/Domain/LookData.cs:128 | 8 | 5 | 2 | 4 (3/1) | 0 |
| `FaceBand` | class | Scripts/Domain/LookData.cs:139 | 8 | 5 | 2 | 5 (3/2) | 0 |
| `ConfusablePair` | class | Scripts/Domain/LookData.cs:150 | 14 | 7 | 4 | 2 (1/1) | 0 |
| `LookRules` | class | Scripts/Domain/LookData.cs:167 | 35 | 23 | 5 | 8 (6/2) | 0 |
| `LookKeys` | class | Scripts/Domain/LookKeys.cs:11 | 149 | 101 | 17 | 8 (5/3) | 0 |
| `LookLayer` | enum | Scripts/Domain/Looks.cs:8 | 29 | 12 | 9 | 8 (6/2) | 0 |
| `LookKey` | struct | Scripts/Domain/Looks.cs:39 | 35 | 20 | 7 | 5 (3/2) | 0 |
| `LookPart` | struct | Scripts/Domain/Looks.cs:76 | 19 | 12 | 4 | 3 (2/1) | 0 |
| `Garment` | class | Scripts/Domain/Looks.cs:97 | 23 | 14 | 5 | 10 (5/5) | 0 |
| `LookSource` | class | Scripts/Domain/Looks.cs:122 | 17 | 8 | 5 | 2 (1/1) | 0 |
| `TravellerLook` | class | Scripts/Domain/Looks.cs:141 | 74 | 48 | 11 | 10 (9/1) | 0 |
| `Looks` | class | Scripts/Domain/Looks.cs:223 | 324 | 224 | 11 | 11 (9/2) | 0 |
| `NameRoster` | class | Scripts/Domain/NameRoster.cs:11 | 124 | 81 | 5 | 3 (2/1) | 0 |
| `NationLeader` | class | Scripts/Domain/NationLeader.cs:10 | 49 | 35 | 1 | 2 (1/1) | 0 |
| `OfficeAnchorId` | enum | Scripts/Domain/OfficeContract.cs:11 | 71 | 26 | 23 | 6 (5/1) | 0 |
| `AnchorSource` | enum | Scripts/Domain/OfficeContract.cs:84 | 14 | 7 | 4 | 4 (3/1) | 0 |
| `OfficeContract` | class | Scripts/Domain/OfficeContract.cs:100 | 46 | 28 | 7 | 4 (3/1) | 0 |
| `OriginLabels` | class | Scripts/Domain/OriginLabels.cs:5 | 6 | 5 | 1 | 4 (3/1) | 0 |
| `WakeReason` | enum | Scripts/Domain/PcScreen.cs:4 | 8 | 5 | 2 | 3 (2/1) | 0 |
| `PcWakeRules` | class | Scripts/Domain/PcScreen.cs:15 | 8 | 5 | 2 | 2 (1/1) | 0 |
| `PcScreen` | class | Scripts/Domain/PcScreen.cs:31 | 80 | 55 | 7 | 2 (1/1) | 0 |
| `PremadeSlot` | enum | Scripts/Domain/Premades.cs:2 | 11 | 6 | 3 | 3 (2/1) | 0 |
| `Premades` | class | Scripts/Domain/Premades.cs:19 | 40 | 19 | 4 | 5 (4/1) | 0 |
| `ReadyGate` | class | Scripts/Domain/ReadyGate.cs:8 | 24 | 14 | 5 | 2 (1/1) | 0 |
| `ScoreKeyKind` | enum | Scripts/Domain/ScoreKey.cs:2 | 14 | 7 | 4 | 2 (1/1) | 0 |
| `ParsedScoreKey` | struct | Scripts/Domain/ScoreKey.cs:18 | 17 | 8 | 5 | 2 (1/1) | 0 |
| `ScoreKey` | class | Scripts/Domain/ScoreKey.cs:42 | 86 | 55 | 6 | 3 (2/1) | 0 |
| `RankedScore` | struct | Scripts/Domain/ScoreRanking.cs:8 | 15 | 10 | 3 | 9 (5/4) | 0 |
| `ScoreRanking` | class | Scripts/Domain/ScoreRanking.cs:28 | 9 | 5 | 1 | 3 (1/2) | 0 |
| `DominanceTier` | enum | Scripts/Domain/ScoreRanking.cs:39 | 11 | 6 | 3 | 2 (1/1) | 0 |
| `DominanceTiers` | class | Scripts/Domain/ScoreRanking.cs:52 | 28 | 17 | 1 | 2 (1/1) | 0 |
| `SeededRandom` | class | Scripts/Domain/SeededRandom.cs:6 | 41 | 30 | 3 | 8 (1/7) | 0 |
| `Seeds` | class | Scripts/Domain/Seeds.cs:6 | 84 | 35 | 16 | 4 (3/1) | 0 |
| `ShiftClock` | class | Scripts/Domain/ShiftClock.cs:8 | 102 | 60 | 18 | 3 (2/1) | 0 |
| `ClosingAction` | enum | Scripts/Domain/ShiftFlow.cs:2 | 8 | 5 | 2 | 2 (1/1) | 0 |
| `ShiftFlow` | class | Scripts/Domain/ShiftFlow.cs:12 | 6 | 5 | 1 | 2 (1/1) | 0 |
| `ShiftLedger` | class | Scripts/Domain/ShiftLedger.cs:9 | 69 | 52 | 9 | 6 (4/2) | 0 |
| `DialogOutcome` | class | Scripts/Domain/ShiftLedger.cs:84 | 11 | 6 | 3 | 3 (2/1) | 0 |
| `CaseVerdict` | class | Scripts/Domain/ShiftLedger.cs:100 | 69 | 24 | 21 | 4 (3/1) | 0 |
| `TranslatorKind` | enum | Scripts/Domain/Translation.cs:6 | 8 | 5 | 2 | 5 (4/1) | 0 |
| `Translation` | class | Scripts/Domain/Translation.cs:21 | 92 | 69 | 4 | 11 (10/1) | 0 |
| `TranslationDay` | class | Scripts/Domain/Translation.cs:121 | 42 | 31 | 5 | 4 (3/1) | 0 |
| `Tongue` | class | Scripts/Domain/TranslationContent.cs:10 | 20 | 9 | 6 | 5 (4/1) | 0 |
| `TranslatorPack` | class | Scripts/Domain/TranslationContent.cs:33 | 8 | 5 | 2 | 5 (4/1) | 0 |
| `TranslationRules` | class | Scripts/Domain/TranslationContent.cs:44 | 11 | 6 | 3 | 4 (3/1) | 0 |
| `TravellerGender` | enum | Scripts/Domain/TravellerGender.cs:4 | 11 | 6 | 3 | 13 (9/4) | 0 |
| `TravellerGenders` | class | Scripts/Domain/TravellerGender.cs:20 | 43 | 28 | 1 | 2 (1/1) | 0 |
| `VerdictRules` | class | Scripts/Domain/VerdictRules.cs:6 | 14 | 6 | 2 | 3 (2/1) | 0 |
| `ViolatorSlots` | class | Scripts/Domain/ViolatorSlots.cs:8 | 38 | 24 | 2 | 4 (3/1) | 0 |
| `WeightedRandom` | class | Scripts/Domain/WeightedRandom.cs:8 | 41 | 27 | 1 | 4 (3/1) | 0 |

## TimeDesk.Visuals (60 types)

| Type | Kind | File | Lines | Code | Public members | Code callers (prod/test) | Asset refs |
|---|---|---|---:|---:|---:|---|---:|
| `ArabicShaper` | class | Scripts/Visuals/ArabicShaper.cs:14 | 170 | 134 | 2 | 5 (3/2) | 0 |
| `ContrastClass` | enum | Scripts/Visuals/Contrast.cs:6 | 17 | 8 | 5 | 6 (4/2) | 0 |
| `ContrastRules` | class | Scripts/Visuals/Contrast.cs:26 | 33 | 20 | 7 | 6 (4/2) | 0 |
| `ContrastPair` | struct | Scripts/Visuals/Contrast.cs:61 | 23 | 14 | 5 | 4 (2/2) | 0 |
| `Contrast` | class | Scripts/Visuals/Contrast.cs:89 | 118 | 68 | 8 | 6 (4/2) | 0 |
| `LabelLanguage` | enum | Scripts/Visuals/CultureChoice.cs:2 | 17 | 8 | 5 | 2 (1/1) | 0 |
| `CultureChoice` | class | Scripts/Visuals/CultureChoice.cs:24 | 42 | 26 | 4 | 5 (4/1) | 0 |
| `CulturePlaceholders` | class | Scripts/Visuals/CulturePlaceholders.cs:8 | 70 | 51 | 1 | 2 (1/1) | 0 |
| `CursorHotspot` | class | Scripts/Visuals/CursorHotspot.cs:9 | 46 | 34 | 2 | 2 (1/1) | 0 |
| `CursorHotspot.Kind` | enum | Scripts/Visuals/CursorHotspot.cs:15 | 8 | 5 | 2 | 12 (9/3) | 0 |
| `DeskRect` | struct | Scripts/Visuals/DeskGeometry.cs:8 | 53 | 35 | 9 | 4 (3/1) | 0 |
| `RectClamp` | class | Scripts/Visuals/DeskGeometry.cs:63 | 19 | 13 | 1 | 3 (2/1) | 0 |
| `PaperStack` | class | Scripts/Visuals/DeskGeometry.cs:87 | 24 | 16 | 4 | 2 (1/1) | 0 |
| `DeskViewTuning` | class | Scripts/Visuals/DeskViewPose.cs:10 | 14 | 7 | 4 | 3 (2/1) | 0 |
| `DeskViewPose` | class | Scripts/Visuals/DeskViewPose.cs:31 | 23 | 13 | 3 | 2 (1/1) | 0 |
| `ForeignText` | class | Scripts/Visuals/DisplayText.cs:5 | 15 | 10 | 3 | 3 (2/1) | 0 |
| `RevealKind` | enum | Scripts/Visuals/DisplayText.cs:22 | 11 | 6 | 3 | 3 (3/0) | 0 |
| `Reveal` | struct | Scripts/Visuals/DisplayText.cs:35 | 31 | 17 | 7 | 6 (5/1) | 0 |
| `DisplayText` | class | Scripts/Visuals/DisplayText.cs:78 | 93 | 68 | 4 | 3 (2/1) | 0 |
| `ExamineTuning` | class | Scripts/Visuals/ExamineLayout.cs:10 | 41 | 16 | 13 | 3 (2/1) | 0 |
| `ScreenBox` | struct | Scripts/Visuals/ExamineLayout.cs:53 | 19 | 12 | 4 | 2 (1/1) | 0 |
| `ExamineLayout` | class | Scripts/Visuals/ExamineLayout.cs:81 | 120 | 77 | 6 | 2 (1/1) | 0 |
| `FlipTiming` | class | Scripts/Visuals/FlipSequence.cs:5 | 17 | 8 | 5 | 6 (4/2) | 0 |
| `CellState` | enum | Scripts/Visuals/FlipSequence.cs:24 | 11 | 6 | 3 | 2 (1/1) | 0 |
| `FlipSequence` | class | Scripts/Visuals/FlipSequence.cs:43 | 58 | 39 | 5 | 3 (1/2) | 0 |
| `PlaceholderRegion` | enum | Scripts/Visuals/LayerPlaceholder.cs:4 | 29 | 12 | 9 | 2 (1/1) | 0 |
| `PlaceholderMark` | enum | Scripts/Visuals/LayerPlaceholder.cs:35 | 17 | 8 | 5 | 2 (1/1) | 0 |
| `LayerPlaceholder` | class | Scripts/Visuals/LayerPlaceholder.cs:61 | 187 | 139 | 3 | 2 (1/1) | 0 |
| `LookCanvas` | class | Scripts/Visuals/LookCanvas.cs:9 | 64 | 25 | 20 | 10 (7/3) | 0 |
| `Paging` | class | Scripts/Visuals/Paging.cs:8 | 21 | 14 | 4 | 3 (2/1) | 0 |
| `PaletteRule` | class | Scripts/Visuals/Palette.cs:7 | 17 | 8 | 5 | 2 (1/1) | 0 |
| `PaletteOverride` | class | Scripts/Visuals/Palette.cs:27 | 11 | 6 | 3 | 2 (1/1) | 0 |
| `ResolvedRole` | class | Scripts/Visuals/Palette.cs:40 | 14 | 7 | 4 | 3 (2/1) | 0 |
| `Palette` | class | Scripts/Visuals/Palette.cs:61 | 167 | 124 | 4 | 3 (2/1) | 0 |
| `PaperFaceTuning` | class | Scripts/Visuals/PaperFace.cs:11 | 35 | 14 | 11 | 2 (1/1) | 0 |
| `FaceRect` | struct | Scripts/Visuals/PaperFace.cs:48 | 38 | 19 | 10 | 2 (1/1) | 0 |
| `FaceRow` | struct | Scripts/Visuals/PaperFace.cs:88 | 19 | 12 | 4 | 2 (1/1) | 0 |
| `FaceLayout` | class | Scripts/Visuals/PaperFace.cs:109 | 23 | 14 | 5 | 3 (2/1) | 0 |
| `PaperFace` | class | Scripts/Visuals/PaperFace.cs:142 | 62 | 46 | 3 | 3 (2/1) | 0 |
| `ScreenRect` | struct | Scripts/Visuals/PaperLanding.cs:5 | 49 | 28 | 11 | 3 (2/1) | 0 |
| `LandingChoice` | struct | Scripts/Visuals/PaperLanding.cs:56 | 15 | 10 | 3 | 2 (1/1) | 0 |
| `PaperLanding` | class | Scripts/Visuals/PaperLanding.cs:81 | 155 | 112 | 3 | 2 (1/1) | 0 |
| `PixelShapes` | class | Scripts/Visuals/PixelShapes.cs:2 | 26 | 22 | 2 | 4 (3/1) | 0 |
| `PlaceholderPalette` | class | Scripts/Visuals/PlaceholderPalette.cs:6 | 71 | 51 | 5 | 2 (1/1) | 0 |
| `Pseudoscript` | class | Scripts/Visuals/Pseudoscript.cs:12 | 102 | 78 | 6 | 6 (4/2) | 0 |
| `RadialLayout` | class | Scripts/Visuals/RadialLayout.cs:9 | 58 | 40 | 3 | 4 (3/1) | 0 |
| `ReactionKind` | enum | Scripts/Visuals/ReactionCurve.cs:4 | 17 | 8 | 5 | 4 (3/1) | 0 |
| `ReactionPose` | struct | Scripts/Visuals/ReactionCurve.cs:23 | 26 | 15 | 6 | 2 (1/1) | 0 |
| `ReactionCurve` | class | Scripts/Visuals/ReactionCurve.cs:51 | 28 | 21 | 1 | 2 (1/1) | 0 |
| `RevealClock` | class | Scripts/Visuals/RevealClock.cs:8 | 30 | 22 | 4 | 6 (5/1) | 0 |
| `Rgba` | struct | Scripts/Visuals/Rgba.cs:9 | 48 | 30 | 7 | 10 (6/4) | 0 |
| `ScreenMapping` | class | Scripts/Visuals/ScreenMapping.cs:8 | 39 | 25 | 2 | 3 (2/1) | 0 |
| `SpeechQueue` | class | Scripts/Visuals/SpeechQueue.cs:17 | 173 | 106 | 13 | 2 (1/1) | 0 |
| `ThemeRoleId` | enum | Scripts/Visuals/ThemeRoles.cs:7 | 140 | 49 | 46 | 13 (11/2) | 0 |
| `ThemeRoles` | class | Scripts/Visuals/ThemeRoles.cs:149 | 5 | 4 | 1 | 6 (4/2) | 0 |
| `GlossStyle` | enum | Scripts/Visuals/UiStrings.cs:8 | 11 | 6 | 3 | 2 (1/1) | 0 |
| `StringTier` | enum | Scripts/Visuals/UiStrings.cs:21 | 8 | 5 | 2 | 2 (1/1) | 0 |
| `UiStringEntry` | class | Scripts/Visuals/UiStrings.cs:32 | 14 | 7 | 4 | 5 (4/1) | 0 |
| `UiStrings` | class | Scripts/Visuals/UiStrings.cs:56 | 246 | 203 | 6 | 5 (4/1) | 0 |
| `WheelIconPlaceholder` | class | Scripts/Visuals/WheelIconPlaceholder.cs:11 | 88 | 70 | 2 | 3 (1/2) | 0 |

## TimeDeskEditMode (81 types)

| Type | Kind | File | Lines | Code | Public members | Code callers (prod/test) | Asset refs |
|---|---|---|---:|---:|---:|---|---:|
| `ArabicShaperTests` | class | Tests/EditMode/ArabicShaperTests.cs:9 | 55 | 50 | 5 | 0 (0/0) | 0 |
| `BirthDatesTests` | class | Tests/EditMode/BirthDatesTests.cs:3 | 166 | 148 | 15 | 0 (0/0) | 0 |
| `BoothRulesTests` | class | Tests/EditMode/BoothRulesTests.cs:18 | 274 | 225 | 23 | 0 (0/0) | 0 |
| `BoothRulesTests.Row` | class | Tests/EditMode/BoothRulesTests.cs:20 | 6 | 6 | 3 | 3 (3/0) | 0 |
| `CarriesTests` | class | Tests/EditMode/CarriesTests.cs:11 | 120 | 107 | 8 | 0 (0/0) | 0 |
| `CitizenRegistryTests` | class | Tests/EditMode/CitizenRegistryTests.cs:4 | 50 | 44 | 6 | 0 (0/0) | 0 |
| `ComparePairTests` | class | Tests/EditMode/ComparePairTests.cs:12 | 152 | 138 | 13 | 0 (0/0) | 0 |
| `ContrastTests` | class | Tests/EditMode/ContrastTests.cs:6 | 172 | 149 | 15 | 0 (0/0) | 0 |
| `CultureChoiceTests` | class | Tests/EditMode/CultureChoiceTests.cs:4 | 56 | 51 | 6 | 0 (0/0) | 0 |
| `CultureCueTests` | class | Tests/EditMode/CultureCueTests.cs:5 | 41 | 38 | 4 | 0 (0/0) | 0 |
| `CulturePlaceholdersTests` | class | Tests/EditMode/CulturePlaceholdersTests.cs:5 | 45 | 39 | 3 | 0 (0/0) | 0 |
| `CursorHotspotTests` | class | Tests/EditMode/CursorHotspotTests.cs:3 | 63 | 58 | 4 | 0 (0/0) | 0 |
| `DayPlansTests` | class | Tests/EditMode/DayPlansTests.cs:4 | 21 | 20 | 2 | 0 (0/0) | 0 |
| `DaySlotSequencerTests` | class | Tests/EditMode/DaySlotSequencerTests.cs:3 | 90 | 83 | 8 | 0 (0/0) | 0 |
| `DeskGeometryTests` | class | Tests/EditMode/DeskGeometryTests.cs:8 | 125 | 99 | 9 | 0 (0/0) | 0 |
| `DeskPapersTests` | class | Tests/EditMode/DeskPapersTests.cs:12 | 458 | 400 | 29 | 0 (0/0) | 0 |
| `DeskPapersTests.State` | enum | Tests/EditMode/DeskPapersTests.cs:36 | 1 | 1 | 4 | 1 (0/1) | 0 |
| `DeskViewPoseTests` | class | Tests/EditMode/DeskViewPoseTests.cs:8 | 55 | 48 | 6 | 0 (0/0) | 0 |
| `DialogChoiceKindsTests` | class | Tests/EditMode/DialogChoiceKindsTests.cs:7 | 88 | 79 | 6 | 0 (0/0) | 0 |
| `DialogRunnerTests` | class | Tests/EditMode/DialogRunnerTests.cs:12 | 136 | 118 | 8 | 0 (0/0) | 0 |
| `DiscrepancyLogTests` | class | Tests/EditMode/DiscrepancyLogTests.cs:8 | 471 | 369 | 35 | 0 (0/0) | 0 |
| `DisplayTextTests` | class | Tests/EditMode/DisplayTextTests.cs:11 | 133 | 113 | 10 | 0 (0/0) | 0 |
| `DocumentRowsTests` | class | Tests/EditMode/DocumentRowsTests.cs:11 | 124 | 108 | 10 | 0 (0/0) | 0 |
| `DominanceTiersTests` | class | Tests/EditMode/DominanceTiersTests.cs:4 | 38 | 34 | 4 | 0 (0/0) | 0 |
| `EffectOpsTests` | class | Tests/EditMode/EffectOpsTests.cs:7 | 55 | 53 | 3 | 0 (0/0) | 0 |
| `EndingRulesTests` | class | Tests/EditMode/EndingRulesTests.cs:8 | 58 | 52 | 6 | 0 (0/0) | 0 |
| `ExamineLayoutTests` | class | Tests/EditMode/ExamineLayoutTests.cs:12 | 194 | 177 | 15 | 0 (0/0) | 0 |
| `FactTableTests` | class | Tests/EditMode/FactTableTests.cs:8 | 117 | 105 | 10 | 0 (0/0) | 0 |
| `FlipSequenceTests` | class | Tests/EditMode/FlipSequenceTests.cs:10 | 138 | 124 | 9 | 0 (0/0) | 0 |
| `ForgeryTests` | class | Tests/EditMode/ForgeryTests.cs:10 | 122 | 93 | 12 | 0 (0/0) | 0 |
| `GatesTests` | class | Tests/EditMode/GatesTests.cs:12 | 251 | 221 | 18 | 0 (0/0) | 0 |
| `HistoryChecksTests` | class | Tests/EditMode/HistoryChecksTests.cs:9 | 70 | 61 | 6 | 0 (0/0) | 0 |
| `HistoryTests` | class | Tests/EditMode/HistoryTests.cs:9 | 140 | 123 | 11 | 0 (0/0) | 0 |
| `InfluenceTests` | class | Tests/EditMode/InfluenceTests.cs:10 | 73 | 63 | 7 | 0 (0/0) | 0 |
| `InterviewDayTests` | class | Tests/EditMode/InterviewDayTests.cs:13 | 212 | 182 | 11 | 0 (0/0) | 0 |
| `InterviewScriptTests` | class | Tests/EditMode/InterviewScriptTests.cs:13 | 676 | 570 | 40 | 0 (0/0) | 0 |
| `InterviewTests` | class | Tests/EditMode/InterviewTests.cs:9 | 199 | 157 | 14 | 0 (0/0) | 0 |
| `LayerPlaceholderTests` | class | Tests/EditMode/LayerPlaceholderTests.cs:6 | 111 | 95 | 6 | 0 (0/0) | 0 |
| `LiesTests` | class | Tests/EditMode/LiesTests.cs:18 | 697 | 585 | 34 | 0 (0/0) | 0 |
| `LookCanvasTests` | class | Tests/EditMode/LookCanvasTests.cs:4 | 38 | 35 | 4 | 0 (0/0) | 0 |
| `LookKeysTests` | class | Tests/EditMode/LookKeysTests.cs:5 | 163 | 147 | 10 | 0 (0/0) | 0 |
| `LooksTests` | class | Tests/EditMode/LooksTests.cs:15 | 466 | 381 | 23 | 0 (0/0) | 0 |
| `NameRosterTests` | class | Tests/EditMode/NameRosterTests.cs:4 | 124 | 111 | 11 | 0 (0/0) | 0 |
| `NationLeaderTests` | class | Tests/EditMode/NationLeaderTests.cs:8 | 70 | 63 | 6 | 0 (0/0) | 0 |
| `OfficeContractTests` | class | Tests/EditMode/OfficeContractTests.cs:5 | 53 | 48 | 6 | 0 (0/0) | 0 |
| `OriginLabelsTests` | class | Tests/EditMode/OriginLabelsTests.cs:3 | 15 | 14 | 2 | 0 (0/0) | 0 |
| `PagingTests` | class | Tests/EditMode/PagingTests.cs:4 | 43 | 40 | 4 | 0 (0/0) | 0 |
| `PaletteTests` | class | Tests/EditMode/PaletteTests.cs:6 | 174 | 156 | 10 | 0 (0/0) | 0 |
| `PaperFaceTests` | class | Tests/EditMode/PaperFaceTests.cs:11 | 102 | 94 | 7 | 0 (0/0) | 0 |
| `PaperLandingTests` | class | Tests/EditMode/PaperLandingTests.cs:12 | 106 | 89 | 8 | 0 (0/0) | 0 |
| `PcScreenTests` | class | Tests/EditMode/PcScreenTests.cs:4 | 133 | 118 | 11 | 0 (0/0) | 0 |
| `PixelShapesTests` | class | Tests/EditMode/PixelShapesTests.cs:4 | 51 | 44 | 5 | 0 (0/0) | 0 |
| `PlaceholderPaletteTests` | class | Tests/EditMode/PlaceholderPaletteTests.cs:4 | 48 | 44 | 5 | 0 (0/0) | 0 |
| `PremadesTests` | class | Tests/EditMode/PremadesTests.cs:4 | 57 | 52 | 6 | 0 (0/0) | 0 |
| `PseudoscriptTests` | class | Tests/EditMode/PseudoscriptTests.cs:10 | 160 | 141 | 13 | 0 (0/0) | 0 |
| `RadialLayoutTests` | class | Tests/EditMode/RadialLayoutTests.cs:9 | 77 | 63 | 9 | 0 (0/0) | 0 |
| `ReactionCurveTests` | class | Tests/EditMode/ReactionCurveTests.cs:5 | 62 | 54 | 6 | 0 (0/0) | 0 |
| `ReadyGateTests` | class | Tests/EditMode/ReadyGateTests.cs:3 | 44 | 36 | 3 | 0 (0/0) | 0 |
| `RevealClockTests` | class | Tests/EditMode/RevealClockTests.cs:10 | 52 | 48 | 5 | 0 (0/0) | 0 |
| `RgbaTests` | class | Tests/EditMode/RgbaTests.cs:4 | 43 | 39 | 4 | 0 (0/0) | 0 |
| `ScoreKeyTests` | class | Tests/EditMode/ScoreKeyTests.cs:8 | 76 | 67 | 7 | 0 (0/0) | 0 |
| `ScoreRankingTests` | class | Tests/EditMode/ScoreRankingTests.cs:6 | 36 | 31 | 4 | 0 (0/0) | 0 |
| `ScreenMappingTests` | class | Tests/EditMode/ScreenMappingTests.cs:4 | 66 | 58 | 7 | 0 (0/0) | 0 |
| `ScriptStep` | struct | Tests/EditMode/ScriptedRandom.cs:4 | 24 | 14 | 5 | 7 (0/7) | 0 |
| `ScriptedRandom` | class | Tests/EditMode/ScriptedRandom.cs:35 | 46 | 30 | 5 | 7 (0/7) | 0 |
| `ScriptedRandomTests` | class | Tests/EditMode/ScriptedRandomTests.cs:8 | 47 | 41 | 4 | 0 (0/0) | 0 |
| `SeededRandomTests` | class | Tests/EditMode/SeededRandomTests.cs:4 | 50 | 45 | 5 | 0 (0/0) | 0 |
| `SeedsTests` | class | Tests/EditMode/SeedsTests.cs:5 | 125 | 116 | 9 | 0 (0/0) | 0 |
| `ShiftClockTests` | class | Tests/EditMode/ShiftClockTests.cs:4 | 145 | 132 | 12 | 0 (0/0) | 0 |
| `ShiftFlowTests` | class | Tests/EditMode/ShiftFlowTests.cs:3 | 14 | 13 | 2 | 0 (0/0) | 0 |
| `ShiftLedgerTests` | class | Tests/EditMode/ShiftLedgerTests.cs:3 | 44 | 38 | 3 | 0 (0/0) | 0 |
| `SpeechQueueTests` | class | Tests/EditMode/SpeechQueueTests.cs:10 | 408 | 366 | 26 | 0 (0/0) | 0 |
| `ThemeRolesTests` | class | Tests/EditMode/ThemeRolesTests.cs:4 | 33 | 31 | 3 | 0 (0/0) | 0 |
| `TranslationTests` | class | Tests/EditMode/TranslationTests.cs:11 | 269 | 231 | 24 | 0 (0/0) | 0 |
| `TravellerGendersTests` | class | Tests/EditMode/TravellerGendersTests.cs:7 | 64 | 54 | 9 | 0 (0/0) | 0 |
| `UiStringsTests` | class | Tests/EditMode/UiStringsTests.cs:7 | 143 | 127 | 11 | 0 (0/0) | 0 |
| `VerdictRulesTests` | class | Tests/EditMode/VerdictRulesTests.cs:3 | 24 | 22 | 2 | 0 (0/0) | 0 |
| `ViolatorSlotsTests` | class | Tests/EditMode/ViolatorSlotsTests.cs:4 | 89 | 80 | 9 | 0 (0/0) | 0 |
| `WeightedRandomTests` | class | Tests/EditMode/WeightedRandomTests.cs:3 | 53 | 48 | 5 | 0 (0/0) | 0 |
| `WeightedRandomTests.Item` | class | Tests/EditMode/WeightedRandomTests.cs:5 | 5 | 5 | 2 | 7 (6/1) | 0 |
| `WheelIconPlaceholderTests` | class | Tests/EditMode/WheelIconPlaceholderTests.cs:6 | 75 | 67 | 4 | 0 (0/0) | 0 |

## Candidate dead types

No code callers in other files, no mentions elsewhere in their own file, no asset refs. Entry points called by reflection are annotated; discount them.

| Type | Kind | File | Lines | Annotation |
|---|---|---|---:|---|
| `DayRunner` | class | Assets/Scripts/DayRunner.cs:7 | 70 | MonoBehaviour with no asset refs |
| `InteractionFeedbackBootstrap` | class | Assets/Scripts/UI/InteractionFeedbackBootstrap.cs:9 | 28 | [RuntimeInitializeOnLoadMethod] |
| `CultureThemeBootstrap` | class | Assets/Scripts/UI/Theme/CultureThemeBootstrap.cs:10 | 28 | [RuntimeInitializeOnLoadMethod] |
| `CharacterArtImporter` | class | Assets/Editor/CharacterArtImporter.cs:12 | 35 | base AssetPostprocessor |
| `HomeSceneBuilder` | class | Assets/Editor/HomeSceneBuilder.cs:20 | 343 | [MenuItem] |
| `OfficeSceneContractTools` | class | Assets/Editor/OfficeSceneContractTools.cs:18 | 119 | [MenuItem] |
| `OfficeSceneUIBuilder` | class | Assets/Editor/OfficeSceneUIBuilder.Desk.cs:26 | 3107 | [MenuItem] |
| `TitleSceneBuilder` | class | Assets/Editor/TitleSceneBuilder.cs:17 | 215 | [MenuItem] |
| `ArabicShaperTests` | class | Assets/Tests/EditMode/ArabicShaperTests.cs:9 | 55 | [Test], [TestCase] |
| `BirthDatesTests` | class | Assets/Tests/EditMode/BirthDatesTests.cs:3 | 166 | [Test], [TestCase] |
| `BoothRulesTests` | class | Assets/Tests/EditMode/BoothRulesTests.cs:18 | 274 | [Test] |
| `CarriesTests` | class | Assets/Tests/EditMode/CarriesTests.cs:11 | 120 | [Test] |
| `CitizenRegistryTests` | class | Assets/Tests/EditMode/CitizenRegistryTests.cs:4 | 50 | [Test] |
| `ComparePairTests` | class | Assets/Tests/EditMode/ComparePairTests.cs:12 | 152 | [Test] |
| `ContrastTests` | class | Assets/Tests/EditMode/ContrastTests.cs:6 | 172 | [Test], [TestCase] |
| `CultureChoiceTests` | class | Assets/Tests/EditMode/CultureChoiceTests.cs:4 | 56 | [Test], [TestCase] |
| `CultureCueTests` | class | Assets/Tests/EditMode/CultureCueTests.cs:5 | 41 | [Test], [TestCase] |
| `CulturePlaceholdersTests` | class | Assets/Tests/EditMode/CulturePlaceholdersTests.cs:5 | 45 | [Test], [TestCase] |
| `CursorHotspotTests` | class | Assets/Tests/EditMode/CursorHotspotTests.cs:3 | 63 | [Test] |
| `DayPlansTests` | class | Assets/Tests/EditMode/DayPlansTests.cs:4 | 21 | [Test], [TestCase] |
| `DaySlotSequencerTests` | class | Assets/Tests/EditMode/DaySlotSequencerTests.cs:3 | 90 | [Test] |
| `DeskGeometryTests` | class | Assets/Tests/EditMode/DeskGeometryTests.cs:8 | 125 | [Test] |
| `DeskPapersTests` | class | Assets/Tests/EditMode/DeskPapersTests.cs:12 | 458 | [Test], [TestCase] |
| `DeskViewPoseTests` | class | Assets/Tests/EditMode/DeskViewPoseTests.cs:8 | 55 | [Test] |
| `DialogChoiceKindsTests` | class | Assets/Tests/EditMode/DialogChoiceKindsTests.cs:7 | 88 | [Test] |
| `DialogRunnerTests` | class | Assets/Tests/EditMode/DialogRunnerTests.cs:12 | 136 | [Test] |
| `DiscrepancyLogTests` | class | Assets/Tests/EditMode/DiscrepancyLogTests.cs:8 | 471 | [Test], [TestCase] |
| `DisplayTextTests` | class | Assets/Tests/EditMode/DisplayTextTests.cs:11 | 133 | [Test] |
| `DocumentRowsTests` | class | Assets/Tests/EditMode/DocumentRowsTests.cs:11 | 124 | [Test] |
| `DominanceTiersTests` | class | Assets/Tests/EditMode/DominanceTiersTests.cs:4 | 38 | [Test] |
| `EffectOpsTests` | class | Assets/Tests/EditMode/EffectOpsTests.cs:7 | 55 | [Test], [TestCase] |
| `EndingRulesTests` | class | Assets/Tests/EditMode/EndingRulesTests.cs:8 | 58 | [Test], [TestCase] |
| `ExamineLayoutTests` | class | Assets/Tests/EditMode/ExamineLayoutTests.cs:12 | 194 | [Test] |
| `FactTableTests` | class | Assets/Tests/EditMode/FactTableTests.cs:8 | 117 | [Test] |
| `FlipSequenceTests` | class | Assets/Tests/EditMode/FlipSequenceTests.cs:10 | 138 | [Test] |
| `ForgeryTests` | class | Assets/Tests/EditMode/ForgeryTests.cs:10 | 122 | [Test] |
| `GatesTests` | class | Assets/Tests/EditMode/GatesTests.cs:12 | 251 | [Test] |
| `HistoryChecksTests` | class | Assets/Tests/EditMode/HistoryChecksTests.cs:9 | 70 | [Test] |
| `HistoryTests` | class | Assets/Tests/EditMode/HistoryTests.cs:9 | 140 | [Test], [TestCase] |
| `InfluenceTests` | class | Assets/Tests/EditMode/InfluenceTests.cs:10 | 73 | [Test] |
| `InterviewDayTests` | class | Assets/Tests/EditMode/InterviewDayTests.cs:13 | 212 | [Test] |
| `InterviewScriptTests` | class | Assets/Tests/EditMode/InterviewScriptTests.cs:13 | 676 | [Test] |
| `InterviewTests` | class | Assets/Tests/EditMode/InterviewTests.cs:9 | 199 | [Test], [TestCase] |
| `LayerPlaceholderTests` | class | Assets/Tests/EditMode/LayerPlaceholderTests.cs:6 | 111 | [Test] |
| `LiesTests` | class | Assets/Tests/EditMode/LiesTests.cs:18 | 697 | [Test] |
| `LookCanvasTests` | class | Assets/Tests/EditMode/LookCanvasTests.cs:4 | 38 | [Test] |
| `LookKeysTests` | class | Assets/Tests/EditMode/LookKeysTests.cs:5 | 163 | [Test], [TestCase] |
| `LooksTests` | class | Assets/Tests/EditMode/LooksTests.cs:15 | 466 | [Test], [TestCase] |
| `NameRosterTests` | class | Assets/Tests/EditMode/NameRosterTests.cs:4 | 124 | [Test], [TestCase] |
| `NationLeaderTests` | class | Assets/Tests/EditMode/NationLeaderTests.cs:8 | 70 | [Test], [TestCase] |
| `OfficeContractTests` | class | Assets/Tests/EditMode/OfficeContractTests.cs:5 | 53 | [Test], [TestCase] |
| `OriginLabelsTests` | class | Assets/Tests/EditMode/OriginLabelsTests.cs:3 | 15 | [Test] |
| `PagingTests` | class | Assets/Tests/EditMode/PagingTests.cs:4 | 43 | [Test], [TestCase] |
| `PaletteTests` | class | Assets/Tests/EditMode/PaletteTests.cs:6 | 174 | [Test] |
| `PaperFaceTests` | class | Assets/Tests/EditMode/PaperFaceTests.cs:11 | 102 | [Test] |
| `PaperLandingTests` | class | Assets/Tests/EditMode/PaperLandingTests.cs:12 | 106 | [Test] |
| `PcScreenTests` | class | Assets/Tests/EditMode/PcScreenTests.cs:4 | 133 | [Test], [TestCase] |
| `PixelShapesTests` | class | Assets/Tests/EditMode/PixelShapesTests.cs:4 | 51 | [Test], [TestCase] |
| `PlaceholderPaletteTests` | class | Assets/Tests/EditMode/PlaceholderPaletteTests.cs:4 | 48 | [Test], [TestCase] |
| `PremadesTests` | class | Assets/Tests/EditMode/PremadesTests.cs:4 | 57 | [Test], [TestCase] |
| `PseudoscriptTests` | class | Assets/Tests/EditMode/PseudoscriptTests.cs:10 | 160 | [Test], [TestCase] |
| `RadialLayoutTests` | class | Assets/Tests/EditMode/RadialLayoutTests.cs:9 | 77 | [Test] |
| `ReactionCurveTests` | class | Assets/Tests/EditMode/ReactionCurveTests.cs:5 | 62 | [Test] |
| `ReadyGateTests` | class | Assets/Tests/EditMode/ReadyGateTests.cs:3 | 44 | [Test] |
| `RevealClockTests` | class | Assets/Tests/EditMode/RevealClockTests.cs:10 | 52 | [Test] |
| `RgbaTests` | class | Assets/Tests/EditMode/RgbaTests.cs:4 | 43 | [Test], [TestCase] |
| `ScoreKeyTests` | class | Assets/Tests/EditMode/ScoreKeyTests.cs:8 | 76 | [Test], [TestCase] |
| `ScoreRankingTests` | class | Assets/Tests/EditMode/ScoreRankingTests.cs:6 | 36 | [Test] |
| `ScreenMappingTests` | class | Assets/Tests/EditMode/ScreenMappingTests.cs:4 | 66 | [Test], [TestCase] |
| `ScriptedRandomTests` | class | Assets/Tests/EditMode/ScriptedRandomTests.cs:8 | 47 | [Test] |
| `SeededRandomTests` | class | Assets/Tests/EditMode/SeededRandomTests.cs:4 | 50 | [Test], [TestCase] |
| `SeedsTests` | class | Assets/Tests/EditMode/SeedsTests.cs:5 | 125 | [Test], [TestCase] |
| `ShiftClockTests` | class | Assets/Tests/EditMode/ShiftClockTests.cs:4 | 145 | [Test], [TestCase] |
| `ShiftFlowTests` | class | Assets/Tests/EditMode/ShiftFlowTests.cs:3 | 14 | [Test] |
| `ShiftLedgerTests` | class | Assets/Tests/EditMode/ShiftLedgerTests.cs:3 | 44 | [Test] |
| `SpeechQueueTests` | class | Assets/Tests/EditMode/SpeechQueueTests.cs:10 | 408 | [Test] |
| `ThemeRolesTests` | class | Assets/Tests/EditMode/ThemeRolesTests.cs:4 | 33 | [Test], [TestCase] |
| `TranslationTests` | class | Assets/Tests/EditMode/TranslationTests.cs:11 | 269 | [Test], [TestCase] |
| `TravellerGendersTests` | class | Assets/Tests/EditMode/TravellerGendersTests.cs:7 | 64 | [Test] |
| `UiStringsTests` | class | Assets/Tests/EditMode/UiStringsTests.cs:7 | 143 | [Test], [TestCase] |
| `VerdictRulesTests` | class | Assets/Tests/EditMode/VerdictRulesTests.cs:3 | 24 | [TestCase] |
| `ViolatorSlotsTests` | class | Assets/Tests/EditMode/ViolatorSlotsTests.cs:4 | 89 | [Test], [TestCase] |
| `WeightedRandomTests` | class | Assets/Tests/EditMode/WeightedRandomTests.cs:3 | 53 | [Test], [TestCase] |
| `WheelIconPlaceholderTests` | class | Assets/Tests/EditMode/WheelIconPlaceholderTests.cs:6 | 75 | [Test] |
