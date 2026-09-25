# Static baseline metrics (ff3a6e0228)

Tool: audit-static 1.0; commit ff3a6e0228ac019628c2d7559e2e21a2e6388211. Source set: tracked C# under Assets/Scripts, Assets/Editor, Assets/Tests (Assets/Editor/OfficeArt excluded). Analyzer results included.

## Headline

| Metric | Value |
|---|---|
| Files | 301 |
| Types (partials merged) | 534 |
| Lines (all) / code lines | 47275 / 31975 |
| Method-like units (methods, ctors, accessors with bodies) / methods | 2485 / 2298 |
| Compiler warnings in our code | 16 |
| Types > 400 lines (all / production) | 18 / 12 |
| Methods > 60 lines (all / production) | 40 / 40 |
| Complexity > 15 (all / production) | 74 / 73 |
| Duplicate groups / duplicated lines, W=60 exact, production | 14 / 371 |
| Duplicate groups / duplicated lines, W=60 normalized, production | 119 / 1467 |
| Duplicate groups / duplicated lines, W=60 exact, tests | 1 / 26 |
| Duplicate groups / duplicated lines, W=60 normalized, tests | 108 / 849 |
| Magic numbers in rule code | 52 |
| String literals in rule code | 201 |
| Per-frame Find/GetComponent sites | 9 |
| Per-frame allocation sites | 69 |
| Static mutable state | 63 |
| Singleton access sites | 40 |
| Unused private members: analyzer / text fallback | 1 / 1 |
| Serialized fields never read in code | 3 |

## Size

Type lines = span of every part (partials summed; nested types included in their outer type and also listed on their own). Unit lines = header line to closing line (attributes and doc comments excluded; the > 60 threshold uses this); body = opening brace (or `=>`) to closing line. Code lines = lines holding a token or a preprocessor directive.

### 25 biggest types

| Type | Kind | Lines | Code | Methods | Role | Parts |
|---|---|---|---|---|---|---|
| `OfficeSceneUIBuilder` | class | 3107 | 2256 | 96 | editor | Editor/OfficeSceneUIBuilder.Desk.cs:26-1472, Editor/OfficeSceneUIBuilder.cs:56-1715 |
| `WorldContentGenerator` | class | 2377 | 1849 | 75 | editor | Editor/WorldContentGenerator.Culture.cs:16-367, Editor/WorldContentGenerator.Translation.cs:13-156, Editor/WorldContentGenerator.cs:40-1920 |
| `ContentLibraryValidator` | class | 1290 | 967 | 35 | editor | Editor/ContentLibraryValidator.cs:19-1308 |
| `InvestigationUIController` | class | 960 | 640 | 36 | runtime | Scripts/UI/InvestigationUIController.cs:33-992 |
| `CaseFactory` | class | 866 | 529 | 27 | runtime | Scripts/CaseFactory.cs:22-887 |
| `GameManager` | class | 765 | 463 | 19 | runtime | Scripts/GameManager.cs:9-773 |
| `LiesTests` | class | 697 | 585 | 48 | test | Tests/EditMode/LiesTests.cs:18-714 |
| `InterviewScriptTests` | class | 676 | 570 | 55 | test | Tests/EditMode/InterviewScriptTests.cs:13-688 |
| `TimelineService` | class | 546 | 351 | 18 | runtime | Scripts/Timeline/TimelineService.cs:39-584 |
| `HomeUIController` | class | 544 | 326 | 16 | runtime | Scripts/UI/HomeUIController.cs:18-561 |
| `OfficeSceneBinder` | class | 536 | 368 | 21 | runtime | Scripts/Office/OfficeSceneBinder.cs:27-562 |
| `ContentLibrarySO` | class | 520 | 286 | 20 | runtime | Scripts/ContentLibrarySO.cs:13-532 |
| `DeskController` | class | 506 | 374 | 25 | runtime | Scripts/Office/Desk/DeskController.cs:31-536 |
| `DiscrepancyLogTests` | class | 471 | 369 | 42 | test | Tests/EditMode/DiscrepancyLogTests.cs:8-478 |
| `LooksTests` | class | 466 | 381 | 38 | test | Tests/EditMode/LooksTests.cs:15-480 |
| `DeskPapersTests` | class | 458 | 400 | 33 | test | Tests/EditMode/DeskPapersTests.cs:12-469 |
| `PaperExaminer` | class | 416 | 328 | 26 | runtime | Scripts/Office/Desk/PaperExaminer.cs:22-437 |
| `SpeechQueueTests` | class | 408 | 366 | 27 | test | Tests/EditMode/SpeechQueueTests.cs:10-417 |
| `TravellerWheel` | class | 379 | 252 | 17 | runtime | Scripts/UI/TravellerWheel.cs:28-406 |
| `DebugPanelController` | class | 365 | 278 | 11 | runtime | Scripts/DevTools/DebugPanelController.cs:14-378 |
| `DeskDocument` | class | 356 | 253 | 18 | runtime | Scripts/Office/Desk/DeskDocument.cs:26-381 |
| `HomeSceneBuilder` | class | 343 | 253 | 6 | editor | Editor/HomeSceneBuilder.cs:20-362 |
| `Looks` | class | 324 | 224 | 13 | runtime | Scripts/Domain/Looks.cs:223-546 |
| `CultureThemeService` | class | 312 | 220 | 22 | runtime | Scripts/UI/Theme/CultureThemeService.cs:21-332 |
| `RunManager` | class | 293 | 165 | 15 | runtime | Scripts/Core/RunManager.cs:9-301 |

### 40 biggest methods

| Method | Where | Lines | Body | Code | CC (Roslyn) | CC (spec approx) |
|---|---|---|---|---|---|---|
| `OfficeSceneUIBuilder.Build()` | Editor/OfficeSceneUIBuilder.cs:101 | 386 | 385 | 293 | 26 | 26 |
| `WorldContentGenerator.CheckInterview(WorldSource, Authored, List<string>)` | Editor/WorldContentGenerator.cs:374 | 315 | 314 | 273 | 109 | 109 |
| `WorldContentGenerator.CheckCharacters(WorldSource, Authored, List<string>)` | Editor/WorldContentGenerator.cs:834 | 230 | 229 | 202 | 137 | 136 |
| `OfficeSceneUIBuilder.BuildOffice(OfficeViewController, MonitorScreen, Button, DeskConfigSO, OfficeSceneContractSO, TravellerWheel, OverlayCallout[], OverlayCallout, TMP_Text, ShiftClockDriver, ContentLibrarySO, FallbackHud, PcFrame, StampTray, OfficeCaseHud, Button, out Clickable)` | Editor/OfficeSceneUIBuilder.Desk.cs:552 | 173 | 168 | 151 | 16 | 16 |
| `HomeSceneBuilder.Build()` | Editor/HomeSceneBuilder.cs:24 | 168 | 167 | 127 | 8 | 8 |
| `GameManager.Start()` | Scripts/GameManager.cs:87 | 149 | 148 | 95 | 26 | 26 |
| `DebugPanelController.DrawCheatsTab(RunManager, WorldState, ContentLibrarySO)` | Scripts/DevTools/DebugPanelController.cs:95 | 146 | 145 | 122 | 29 | 29 |
| `InterviewScript.Build(InterviewLines, IReadOnlyList<InterviewQuestion>, IReadOnlyList<AuthoredDialog>, InterviewCase)` | Scripts/Domain/InterviewScript.cs:123 | 123 | 121 | 109 | 31 | 31 |
| `ContentLibraryValidator.CheckDayPlanPlaces(ContentLibrarySO)` | Editor/ContentLibraryValidator.cs:901 | 121 | 120 | 104 | 50 | 52 |
| `ContentLibraryValidator.CheckInterview(ContentLibrarySO)` | Editor/ContentLibraryValidator.cs:146 | 112 | 111 | 98 | 66 | 55 |
| `TitleSceneBuilder.Build()` | Editor/TitleSceneBuilder.cs:21 | 109 | 108 | 79 | 6 | 6 |
| `OfficeSceneUIBuilder.BuildPaperTemplate(Transform, DeskConfigSO)` | Editor/OfficeSceneUIBuilder.Desk.cs:844 | 103 | 102 | 91 | 1 | 1 |
| `DialogChecks.Problems(AuthoredDialog, int)` | Scripts/Domain/InterviewScript.cs:329 | 103 | 102 | 88 | 46 | 46 |
| `WorldContentGenerator.Generate()` | Editor/WorldContentGenerator.cs:59 | 94 | 93 | 68 | 16 | 13 |
| `CaseFactory.GenerateSingleCase(DayPlanSO, WorldState, int, int)` | Scripts/CaseFactory.cs:200 | 93 | 92 | 67 | 30 | 30 |
| `CaseFactory.BuildDocumentsAndClues(CaseInstance, EraSO, CaseBlueprintSO, WorldState)` | Scripts/CaseFactory.cs:704 | 90 | 89 | 59 | 28 | 28 |
| `WorldContentGenerator.CheckHistory(WorldSource, Authored, List<string>)` | Editor/WorldContentGenerator.cs:279 | 87 | 86 | 79 | 45 | 42 |
| `OfficeSceneUIBuilder.BuildDesktopShell(Canvas, Transform, Transform, ContentLibrarySO, OfficeViewController, MonitorScreen)` | Editor/OfficeSceneUIBuilder.cs:1409 | 86 | 84 | 63 | 9 | 9 |
| `DiscrepancyLog.Prove(CompareEvidence, CompareEvidence, string, string)` | Scripts/Domain/DiscrepancyLog.cs:206 | 84 | 83 | 64 | 18 | 18 |
| `InvestigationUIController.ShowRich(CaseInstance, ContentLibrarySO)` | Scripts/UI/InvestigationUIController.cs:392 | 84 | 83 | 64 | 27 | 27 |
| `GameManager.HandleDayCompleted()` | Scripts/GameManager.cs:280 | 82 | 81 | 61 | 19 | 18 |
| `Looks.Compose(LookSource, LookSource, TravellerGender, string, int, LookWeights, LookRules, IRandomSource)` | Scripts/Domain/Looks.cs:405 | 81 | 79 | 67 | 37 | 35 |
| `OfficeSceneUIBuilder.BuildPcFrame(Transform, Camera, OfficeViewController, out Image, out Button)` | Editor/OfficeSceneUIBuilder.Desk.cs:350 | 79 | 78 | 67 | 2 | 2 |
| `DayPlanSO.ResolveSchedule(int)` | Scripts/DayPlanSO.cs:210 | 79 | 78 | 57 | 13 | 18 |
| `ContentLibrarySO.EnsureLookups()` | Scripts/ContentLibrarySO.cs:456 | 76 | 75 | 63 | 31 | 31 |
| `DayOrchestrator.DayLoop()` | Scripts/DayOrchestrator.cs:140 | 75 | 74 | 47 | 14 | 9 |
| `Lies.Plan(float, int, string, string, string, IReadOnlyList<HomeCandidate>, IReadOnlyList<DocumentField>, IReadOnlyList<ClueCategory>, IReadOnlyList<TellChannel>, FactTable, ICollection<ClueCategory>, IRandomSource)` | Scripts/Domain/Lies.cs:177 | 75 | 70 | 64 | 24 | 24 |
| `OfficeSceneBinder.BindDesk(Vector3)` | Scripts/Office/OfficeSceneBinder.cs:313 | 75 | 74 | 65 | 17 | 17 |
| `ContentLibraryValidator.CheckCulture(ContentLibrarySO)` | Editor/ContentLibraryValidator.cs:657 | 71 | 70 | 61 | 25 | 25 |
| `ContentLibraryValidator.ValidateLibrary(ContentLibrarySO)` | Editor/ContentLibraryValidator.cs:57 | 70 | 69 | 51 | 3 | 3 |
| `Translation.Problems(TranslationRules, IEnumerable<string>, IEnumerable<KeyValuePair<string, string>>)` | Scripts/Domain/Translation.cs:43 | 69 | 67 | 62 | 27 | 27 |
| `GameManager.HandlePlayerChoseEra(EraSO)` | Scripts/GameManager.cs:570 | 69 | 68 | 44 | 10 | 9 |
| `GameManager.HandleDecision(bool)` | Scripts/GameManager.cs:645 | 68 | 67 | 48 | 13 | 13 |
| `HomeUIController.CreateRow(Transform, string, string, bool, Action, Color)` | Scripts/UI/HomeUIController.cs:494 | 67 | 66 | 49 | 2 | 2 |
| `OfficeSceneUIBuilder.BuildDesk(Transform, DeskConfigSO, PcFrame, out DeskScanner, out GameObject, out TextMeshPro)` | Editor/OfficeSceneUIBuilder.Desk.cs:767 | 66 | 65 | 59 | 1 | 1 |
| `InvestigationUIController.BuildFallbackBody(CaseInstance, ContentLibrarySO, FactTable, CitizenRegistry, InterviewDay)` | Scripts/UI/InvestigationUIController.cs:835 | 65 | 64 | 59 | 24 | 24 |
| `RuntimeFonts.Resolve(string, IReadOnlyList<FontCandidate>, string)` | Scripts/UI/Theme/RuntimeFonts.cs:82 | 64 | 63 | 60 | 16 | 16 |
| `ContentLibraryValidator.CheckEffects(ContentLibrarySO)` | Editor/ContentLibraryValidator.cs:534 | 63 | 62 | 57 | 41 | 38 |
| `ShiftScoring.ResolveDecision(CaseInstance, bool, int, WorldState, GameConfigSO, ContentLibrarySO, int)` | Scripts/Shift/ShiftScoring.cs:63 | 63 | 55 | 49 | 24 | 22 |
| `WorldContentGenerator.PlanCulture(WorldSource, List<string>)` | Editor/WorldContentGenerator.Culture.cs:62 | 61 | 60 | 54 | 28 | 24 |

### Over the structure thresholds

- Types > 400 lines: 18: `OfficeSceneUIBuilder`, `WorldContentGenerator`, `ContentLibraryValidator`, `InvestigationUIController`, `CaseFactory`, `GameManager`, `LiesTests`, `InterviewScriptTests`, `TimelineService`, `HomeUIController`, `OfficeSceneBinder`, `ContentLibrarySO`, `DeskController`, `DiscrepancyLogTests`, `LooksTests`, `DeskPapersTests`, `PaperExaminer`, `SpeechQueueTests`
- Methods > 60 lines: 40 (all in the table above when <= 40).
- Units with complexity > 15: 74 (Roslyn CA1502 value).

### 30 most complex units

| Unit | Where | CC (Roslyn) | CC (spec approx) | Lines |
|---|---|---|---|---|
| `WorldContentGenerator.CheckCharacters(WorldSource, Authored, List<string>)` | Editor/WorldContentGenerator.cs:834 | 137 | 136 | 230 |
| `WorldContentGenerator.CheckInterview(WorldSource, Authored, List<string>)` | Editor/WorldContentGenerator.cs:374 | 109 | 109 | 315 |
| `ContentLibraryValidator.CheckInterview(ContentLibrarySO)` | Editor/ContentLibraryValidator.cs:146 | 66 | 55 | 112 |
| `ContentLibraryValidator.CheckDayPlanPlaces(ContentLibrarySO)` | Editor/ContentLibraryValidator.cs:901 | 50 | 52 | 121 |
| `DialogChecks.Problems(AuthoredDialog, int)` | Scripts/Domain/InterviewScript.cs:329 | 46 | 46 | 103 |
| `WorldContentGenerator.CheckHistory(WorldSource, Authored, List<string>)` | Editor/WorldContentGenerator.cs:279 | 45 | 42 | 87 |
| `ContentLibraryValidator.CheckEffects(ContentLibrarySO)` | Editor/ContentLibraryValidator.cs:534 | 41 | 38 | 63 |
| `Looks.Compose(LookSource, LookSource, TravellerGender, string, int, LookWeights, LookRules, IRandomSource)` | Scripts/Domain/Looks.cs:405 | 37 | 35 | 81 |
| `WorldContentGenerator.CheckReferences(WorldSource, Authored, List<string>)` | Editor/WorldContentGenerator.cs:214 | 35 | 35 | 55 |
| `ContentLibrarySO.EnsureLookups()` | Scripts/ContentLibrarySO.cs:456 | 31 | 31 | 76 |
| `InterviewScript.Build(InterviewLines, IReadOnlyList<InterviewQuestion>, IReadOnlyList<AuthoredDialog>, InterviewCase)` | Scripts/Domain/InterviewScript.cs:123 | 31 | 31 | 123 |
| `CaseFactory.GenerateSingleCase(DayPlanSO, WorldState, int, int)` | Scripts/CaseFactory.cs:200 | 30 | 30 | 93 |
| `ArabicShaper.ToVisual(string)` | Scripts/Visuals/ArabicShaper.cs:55 | 30 | 30 | 60 |
| `ContentLibraryValidator.CheckPlaceLook(NationEraProfileSO, ContentLibrarySO)` | Editor/ContentLibraryValidator.cs:840 | 29 | 26 | 48 |
| `DebugPanelController.DrawCheatsTab(RunManager, WorldState, ContentLibrarySO)` | Scripts/DevTools/DebugPanelController.cs:95 | 29 | 29 | 146 |
| `TranslationSettings.Problems(IEnumerable<KeyValuePair<string, string>>)` | Scripts/UI/Translation/TranslationSettings.cs:55 | 29 | 26 | 36 |
| `WorldContentGenerator.PlanCulture(WorldSource, List<string>)` | Editor/WorldContentGenerator.Culture.cs:62 | 28 | 24 | 61 |
| `CaseFactory.BuildDocumentsAndClues(CaseInstance, EraSO, CaseBlueprintSO, WorldState)` | Scripts/CaseFactory.cs:704 | 28 | 28 | 90 |
| `LayerPlaceholder.In(PlaceholderRegion, float, float)` | Scripts/Visuals/LayerPlaceholder.cs:157 | 28 | 27 | 26 |
| `ContentLibraryValidator.CheckFuture(ContentLibrarySO)` | Editor/ContentLibraryValidator.cs:404 | 27 | 27 | 60 |
| `Translation.Problems(TranslationRules, IEnumerable<string>, IEnumerable<KeyValuePair<string, string>>)` | Scripts/Domain/Translation.cs:43 | 27 | 27 | 69 |
| `InvestigationUIController.ShowRich(CaseInstance, ContentLibrarySO)` | Scripts/UI/InvestigationUIController.cs:392 | 27 | 27 | 84 |
| `OfficeSceneUIBuilder.Build()` | Editor/OfficeSceneUIBuilder.cs:101 | 26 | 26 | 386 |
| `WorldContentGenerator.PlanTheme(string, CultureData, List<PaletteRule>, Dictionary<string, Rgba>, bool, ThemePlan, CulturePlan, List<string>)` | Editor/WorldContentGenerator.Culture.cs:125 | 26 | 25 | 43 |
| `WorldContentGenerator.CheckConditions(ConditionData[], string, bool, Authored, WorldSource, List<string>)` | Editor/WorldContentGenerator.cs:706 | 26 | 26 | 52 |
| `Gates.Passes(GateCondition, GateSnapshot)` | Scripts/Domain/Gates.cs:181 | 26 | 25 | 24 |
| `Looks.LabelProblems(IReadOnlyList<(string placeId, PlaceWardrobe wardrobe)>)` | Scripts/Domain/Looks.cs:288 | 26 | 26 | 55 |
| `GameManager.Start()` | Scripts/GameManager.cs:87 | 26 | 26 | 149 |
| `ContentLibraryValidator.CheckCulture(ContentLibrarySO)` | Editor/ContentLibraryValidator.cs:657 | 25 | 25 | 71 |
| `Looks.CanLeak(LookSource, LookSource, TravellerGender, LookRules)` | Scripts/Domain/Looks.cs:354 | 25 | 23 | 33 |

### 15 most coupled types (CA1506 class coupling, per type part)

| Type | Where | Coupled types | Namespaces |
|---|---|---|---|
| `OfficeSceneUIBuilder` | Editor/OfficeSceneUIBuilder.cs:56 | 204 | 24 |
| `WorldContentGenerator` | Editor/WorldContentGenerator.cs:40 | 199 | 15 |
| `ContentLibraryValidator` | Editor/ContentLibraryValidator.cs:19 | 118 | 13 |
| `InvestigationUIController` | Scripts/UI/InvestigationUIController.cs:33 | 103 | 12 |
| `GameManager` | Scripts/GameManager.cs:9 | 74 | 7 |
| `CaseFactory` | Scripts/CaseFactory.cs:22 | 72 | 8 |
| `OfficeSceneBinder` | Scripts/Office/OfficeSceneBinder.cs:27 | 64 | 14 |
| `TimelineService` | Scripts/Timeline/TimelineService.cs:39 | 63 | 7 |
| `DeskController` | Scripts/Office/Desk/DeskController.cs:31 | 59 | 13 |
| `CultureThemeService` | Scripts/UI/Theme/CultureThemeService.cs:21 | 52 | 13 |
| `TravellerWheel` | Scripts/UI/TravellerWheel.cs:28 | 52 | 11 |
| `DeskDocument` | Scripts/Office/Desk/DeskDocument.cs:26 | 50 | 8 |
| `ContentLibrarySO` | Scripts/ContentLibrarySO.cs:13 | 48 | 7 |
| `DebugPanelController` | Scripts/DevTools/DebugPanelController.cs:14 | 45 | 11 |
| `HomeUIController` | Scripts/UI/HomeUIController.cs:18 | 42 | 10 |

## Cyclomatic complexity: Roslyn vs approximation

Roslyn value = CA1502 from the SDK's NetAnalyzers (CodeMetricsConfig `CA1502: 0` so every method is reported), matched to units by file + line + Roslyn symbol name (2485 matched, 0 unmatched; 164 analyzer entries had no unit: {'get_': 120, 'method/other': 6, 'set_': 38}, max complexity 1 - auto-property accessors and bodiless members).

- **Spec approximation**: 1 + `if` + `case` (not `default`) + `for` + `foreach` + `while` + `catch` + `&&` + `||` + `??` + ternary `?` + switch-expression arms (the `_` arm excluded) + `when` clauses.
- **Roslyn-calibrated approximation**: 1 + `if` + `case` + `default:` + `for` + `foreach` + `while` + `&&` + `||` + `??` + ternary `?` + `?.` + `?[` (probing Roslyn showed it counts every case label including default and conditional access, but not `catch`, `when`, switch-expression arms, `??=` or pattern `and`/`or`). Lambdas and local functions count toward their method in both.
- Ternary vs nullable `?`: a `?` is a nullable marker when followed by `>` `,` `)` `]` `;` `=` `{` `=>`, by `[]`, or by an identifier that is followed by `=` `;` `,` `)` `=>` `{` `in`.

| Rule | Exact agreement | Within 1 | Mean abs diff | > 15 (approx) | > 15 (Roslyn) |
|---|---|---|---|---|---|
| spec approximation | 2321 | 2439 | 0.106 | 72 | 74 |
| Roslyn-calibrated | 2485 | 2485 | 0.0 | 74 | 74 |

Biggest disagreements, spec approximation vs Roslyn:

| Unit | Where | Roslyn | Spec approx |
|---|---|---|---|
| `ContentLibraryValidator.CheckInterview(ContentLibrarySO)` | Editor/ContentLibraryValidator.cs:146 | 66 | 55 |
| `WorldContentGenerator.ReadingEntry(StringData, List<string>)` | Editor/WorldContentGenerator.Culture.cs:250 | 13 | 5 |
| `WorldContentGenerator.PaletteRuleOf(RoleRuleData, List<string>)` | Editor/WorldContentGenerator.Culture.cs:261 | 11 | 4 |
| `WorldContentGenerator.ToGenderLook(GenderLookData)` | Editor/WorldContentGenerator.cs:1249 | 8 | 3 |
| `CaseFactory.GenerateDayCases(DayPlanSO, WorldState, int, IReadOnlyList<ClueCategory>, IReadOnlyList<ClueCategory>, bool)` | Scripts/CaseFactory.cs:93 | 24 | 19 |
| `DayOrchestrator.DayLoop()` | Scripts/DayOrchestrator.cs:140 | 14 | 9 |
| `DayPlanSO.ResolveSchedule(int)` | Scripts/DayPlanSO.cs:210 | 13 | 18 |
| `WorldContentGenerator.PlanCulture(WorldSource, List<string>)` | Editor/WorldContentGenerator.Culture.cs:62 | 28 | 24 |
| `WorldContentGenerator.EffectiveLooks(LooksWeightData, LooksWeightData)` | Editor/WorldContentGenerator.cs:1272 | 11 | 7 |
| `Looks.CultureValue(PlaceWardrobe)` | Scripts/Domain/Looks.cs:269 | 10 | 6 |
| `Looks.Whole(string, LookSource, LookRules)` | Scripts/Domain/Looks.cs:488 | 6 | 2 |
| `ContentLibraryValidator.CheckEffects(ContentLibrarySO)` | Editor/ContentLibraryValidator.cs:534 | 41 | 38 |
| `ContentLibraryValidator.CheckPlaceLook(NationEraProfileSO, ContentLibrarySO)` | Editor/ContentLibraryValidator.cs:840 | 29 | 26 |
| `ContentLibraryValidator.CheckCultureUnique(ContentLibrarySO)` | Editor/ContentLibraryValidator.cs:1202 | 10 | 7 |
| `WorldContentGenerator.Generate()` | Editor/WorldContentGenerator.cs:59 | 16 | 13 |

## Compiler warnings in our code

16 warnings. csproj NoWarn (kept, as Unity's compiler uses it): CS0169, CS0649 - those are not reported.

| Code | Count |
|---|---|
| CS0618 | 16 |

| File | Warnings |
|---|---|
| Editor/HomeSceneBuilder.cs | CS0618x4 |
| Editor/OfficeSceneUIBuilder.cs | CS0618x7 |
| Editor/TitleSceneBuilder.cs | CS0618x4 |
| Scripts/UI/InvestigationUIController.cs | CS0618x1 |

| Where | Code | Message |
|---|---|---|
| Editor/HomeSceneBuilder.cs:27 | CS0618 | 'Object.FindFirstObjectByType<T>()' is obsolete: 'FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.' |
| Editor/HomeSceneBuilder.cs:45 | CS0618 | 'Object.FindFirstObjectByType<T>()' is obsolete: 'FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.' |
| Editor/HomeSceneBuilder.cs:52 | CS0618 | 'Object.FindFirstObjectByType<T>()' is obsolete: 'FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.' |
| Editor/HomeSceneBuilder.cs:61 | CS0618 | 'Object.FindFirstObjectByType<T>()' is obsolete: 'FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.' |
| Editor/OfficeSceneUIBuilder.cs:129 | CS0618 | 'Object.FindFirstObjectByType<T>()' is obsolete: 'FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.' |
| Editor/OfficeSceneUIBuilder.cs:218 | CS0618 | 'Object.FindFirstObjectByType<T>()' is obsolete: 'FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.' |
| Editor/OfficeSceneUIBuilder.cs:366 | CS0618 | 'Object.FindFirstObjectByType<T>()' is obsolete: 'FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.' |
| Editor/OfficeSceneUIBuilder.cs:370 | CS0618 | 'Object.FindFirstObjectByType<T>()' is obsolete: 'FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.' |
| Editor/OfficeSceneUIBuilder.cs:710 | CS0618 | 'Object.FindObjectsByType<T>(FindObjectsInactive, FindObjectsSortMode)' is obsolete: 'FindObjectsByType with FindObjectsSortMode parameter has been deprecated. Use FindObjectsByType<T>() or FindObjectsByType<T>(FindObjectsInactive) instead. InstanceID will be replaced in the future with EntityId and previous sort order cannot be maintained.' |
| Editor/OfficeSceneUIBuilder.cs:710 | CS0618 | 'FindObjectsSortMode' is obsolete: 'FindObjectsSortMode has been deprecated. Use the FindObjectsByType overloads that do not take a FindObjectsSortMode parameter.' |
| Editor/OfficeSceneUIBuilder.cs:755 | CS0618 | 'Object.FindFirstObjectByType<T>()' is obsolete: 'FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.' |
| Editor/TitleSceneBuilder.cs:24 | CS0618 | 'Object.FindFirstObjectByType<T>()' is obsolete: 'FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.' |
| Editor/TitleSceneBuilder.cs:42 | CS0618 | 'Object.FindFirstObjectByType<T>()' is obsolete: 'FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.' |
| Editor/TitleSceneBuilder.cs:49 | CS0618 | 'Object.FindFirstObjectByType<T>()' is obsolete: 'FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.' |
| Editor/TitleSceneBuilder.cs:58 | CS0618 | 'Object.FindFirstObjectByType<T>()' is obsolete: 'FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.' |
| Scripts/UI/InvestigationUIController.cs:910 | CS0618 | 'Object.FindFirstObjectByType<T>()' is obsolete: 'FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.' |

## Unused private members

Analyzer = IDE0051 (never used), IDE0052 (assigned, never read), CA1823 (unused field); 99 members flagged raw, minus Unity/reflection entry points {'reflection-entry attribute': 3, 'unity message': 92}. Text fallback = private member whose identifier occurs in its type's files only as often as it is declared (same exclusions: Unity messages, UnityEvent m_MethodName in assets, names in string literals, reflection-entry attributes, overrides, explicit interface implementations). IDE0052 needs data flow the text check lacks.

Analyzer: 1; text: 1; both: 1; analyzer only: 0; text only: 0.

| Where | Member | Kind | Source |
|---|---|---|---|
| Scripts/GameManager.cs:769 | `GameManager.ResolveCurrentCase` | method | IDE0051 |

Unused parameters (IDE0060, info, all methods incl. public): 2; Editor/WorldContentGenerator.Culture.cs:284 Remove unused parameter 'where'; Scripts/Visuals/PaperFace.cs:191 Remove unused parameter 'photo' if it is not part of a shipped public API

Serialized, never read in code ([SerializeField] fields Unity writes but no code reads; `in strings` = the name also appears as a string literal, e.g. written by an editor builder through SerializedObject.FindProperty):

| Where | Field | Source | Note |
|---|---|---|---|
| Scripts/Office/Desk/DeskItem.cs:10 | `DeskItem.itemId` | CA1823, IDE0051, text | in strings |
| Scripts/Office/Desk/DeskSlot.cs:21 | `DeskSlot.slotId` | CA1823, IDE0051, text | in strings |
| Scripts/Office/Desk/DeskSlot.cs:24 | `DeskSlot.kind` | CA1823, IDE0051, text | in strings |

## Duplicate code

Token-window hashing over lexed tokens (comments/whitespace, `using` directives and declaration attributes ignored). Exact = tokens verbatim; normalized = identifiers/numbers/strings replaced by placeholders (keywords and punctuation kept). Equal windows are extended to maximal clone pairs; self-overlapping pairs dropped; pairs with the same token sequence merged into groups. Duplicated lines = code lines covered by any clone location, once per file. Production (runtime + editor) and test code are scanned separately.

| Scope | Mode | W=40 groups / lines | W=60 groups / lines | W=100 groups / lines |
|---|---|---|---|---|
| production | exact | 28 / 515 | 14 / 371 | 2 / 198 |
| production | normalized | 507 / 3432 | 119 / 1467 | 15 / 447 |
| test | exact | 18 / 175 | 1 / 26 | 0 / 0 |
| test | normalized | 393 / 2419 | 108 / 849 | 18 / 177 |

### Largest groups, production/exact, W=60 (14 groups)

| Tokens | Lines | Locations | Starts with |
|---|---|---|---|
| 612 | 103 | Editor/HomeSceneBuilder.cs:226-328<br>Editor/TitleSceneBuilder.cs:128-230 | `img.raycastTarget = false; \| }` |
| 163 | 29 | Editor/HomeSceneBuilder.cs:21-49<br>Editor/TitleSceneBuilder.cs:18-46 | `{ \| /// <summary>Builds and wires the Home UI in the open scene.</summary> \| [MenuItem("To` |
| 75 | 13 | Editor/OfficeSceneUIBuilder.cs:932-944<br>Editor/OfficeSceneUIBuilder.cs:977-990 | `return et; \| } \| // Same-named non-text leftover from an older build: replace it.` |
| 70 | 14 | Editor/HomeSceneBuilder.cs:232-245<br>Editor/HomeSceneBuilder.cs:336-349<br>Editor/TitleSceneBuilder.cs:134-147 | `Vector2 anchoredPos, Vector2 size, bool withBackground, Color bgColor = default) \| { \| Tra` |
| 67 | 10 | Editor/HomeSceneBuilder.cs:266-275<br>Editor/HomeSceneBuilder.cs:294-303<br>Editor/TitleSceneBuilder.cs:168-177<br>Editor/TitleSceneBuilder.cs:196-205 | `return existing.GetComponent<TMP_Text>(); \| var go = new GameObject(name, typeof(RectTrans` |
| 66 | 23 | Scripts/UI/StampTray.cs:86-108<br>Scripts/UI/TravellerWheel.cs:208-235 | `return; \| _openedFrame = Time.frameCount;` |
| 65 | 12 | Editor/HomeSceneBuilder.cs:266-277<br>Editor/HomeSceneBuilder.cs:341-352<br>Editor/TitleSceneBuilder.cs:168-179 | `return existing.GetComponent<TMP_Text>(); \| var go = new GameObject(name, typeof(RectTrans` |
| 64 | 10 | Editor/HomeSceneBuilder.cs:294-303<br>Editor/HomeSceneBuilder.cs:341-350<br>Editor/TitleSceneBuilder.cs:196-205 | `return existing.GetComponent<Button>(); \| var go = new GameObject(name, typeof(RectTransfo` |
| 64 | 7 | Editor/OfficeSceneUIBuilder.cs:510-516<br>Editor/OfficeSceneUIBuilder.cs:564-570 | `DocumentWindowController c = GetOrAdd<DocumentWindowController>(win.gameObject); \| var so ` |
| 63 | 3 | Editor/OfficeSceneUIBuilder.Desk.cs:1302-1304<br>Editor/OfficeSceneUIBuilder.Desk.cs:1430-1433 | `Transform host = Panel(overlay, "StampTray", Vector2.zero, Vector2.one, Vector2.zero, Vect` |
| 63 | 14 | Scripts/Domain/DocumentRows.cs:50-63<br>Scripts/Domain/DocumentRows.cs:68-81 | `for (int i = 0; i < fields.Count; i++) \| { \| DocumentField f = fields[i];` |
| 63 | 22 | Scripts/Visuals/PaperFace.cs:51-72<br>Scripts/Visuals/PaperLanding.cs:8-29 | `public FaceRect(float xMin, float yMin, float xMax, float yMax) \| { \| XMin = xMin;` |
| 62 | 14 | Scripts/Shift/ShiftScoring.cs:128-141<br>Scripts/Shift/ShiftScoring.cs:181-194 | `private static void ApplyWrongDecision(CaseVerdict v, WorldState world, GameConfigSO confi` |
| 60 | 8 | Editor/HomeSceneBuilder.cs:65-72<br>Editor/TitleSceneBuilder.cs:62-69 | `var go = new GameObject("HomeUI", typeof(RectTransform)); \| go.transform.SetParent(root, f` |

Duplicated lines per file (top 12): Editor/HomeSceneBuilder.cs 119, Editor/TitleSceneBuilder.cs 106, Editor/OfficeSceneUIBuilder.cs 36, Scripts/Domain/DocumentRows.cs 24, Scripts/Shift/ShiftScoring.cs 22, Scripts/UI/StampTray.cs 17, Scripts/UI/TravellerWheel.cs 17, Scripts/Visuals/PaperFace.cs 12, Scripts/Visuals/PaperLanding.cs 12, Editor/OfficeSceneUIBuilder.Desk.cs 6

### Largest groups, production/normalized, W=60 (119 groups)

| Tokens | Lines | Locations | Starts with |
|---|---|---|---|
| 612 | 103 | Editor/HomeSceneBuilder.cs:226-328<br>Editor/TitleSceneBuilder.cs:128-230 | `img.raycastTarget = false; \| }` |
| 355 | 62 | Editor/HomeSceneBuilder.cs:20-81<br>Editor/TitleSceneBuilder.cs:17-78 | `public static class HomeSceneBuilder \| { \| /// <summary>Builds and wires the Home UI in th` |
| 267 | 22 | Editor/HomeSceneBuilder.cs:123-144<br>Editor/TitleSceneBuilder.cs:79-102 | `TextAlignmentOptions.Center, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.98f)); \| TMP_` |
| 203 | 16 | Editor/HomeSceneBuilder.cs:113-128<br>Editor/TitleSceneBuilder.cs:83-100 | `Transform shopRows = FindOrCreateRowsContainer(shop, "ShopRows", \| new Vector2(0.06f, 0.16` |
| 181 | 14 | Editor/HomeSceneBuilder.cs:100-113<br>Editor/HomeSceneBuilder.cs:126-139<br>Editor/TitleSceneBuilder.cs:83-96 | `Transform familyRows = FindOrCreateRowsContainer(expenses, "FamilyRows", \| new Vector2(0.0` |
| 147 | 25 | Editor/HomeSceneBuilder.cs:161-185<br>Editor/TitleSceneBuilder.cs:102-127 | `soUi.FindProperty("slotTitleText").objectReferenceValue = slotTitle; \| soUi.FindProperty("` |
| 135 | 11 | Editor/HomeSceneBuilder.cs:90-100<br>Editor/HomeSceneBuilder.cs:129-139<br>Editor/TitleSceneBuilder.cs:86-96 | `EnsureHudBacking(hud); \| // --- Expenses panel ---` |
| 122 | 17 | Editor/OfficeSceneUIBuilder.Desk.cs:635-651<br>Editor/OfficeSceneUIBuilder.Desk.cs:814-829 | `var soClock = new SerializedObject(clockReadouts); \| SetRef(soClock, "driver", clock); \| S` |
| 119 | 10 | Editor/OfficeSceneUIBuilder.Desk.cs:1305-1314<br>Editor/OfficeSceneUIBuilder.cs:332-343 | `((RectTransform)panel).pivot = Center; \| Button accept = MakeButton(panel, "AcceptButton",` |
| 116 | 17 | Editor/HomeSceneBuilder.cs:308-324<br>Editor/TitleSceneBuilder.cs:210-226<br>Scripts/UI/HomeUIController.cs:538-554 | `Button btn = go.AddComponent<Button>(); \| btn.targetGraphic = img;` |
| 114 | 18 | Editor/HomeSceneBuilder.cs:261-278<br>Editor/HomeSceneBuilder.cs:289-306<br>Editor/TitleSceneBuilder.cs:163-180<br>Editor/TitleSceneBuilder.cs:191-208 | `TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax) \| { \| Transform exis` |
| 103 | 13 | Scripts/Domain/Translation.cs:57-69<br>Scripts/Domain/Translation.cs:74-86 | `var packIds = new HashSet<string>(); \| var packNames = new HashSet<string>(); \| foreach (T` |
| 102 | 9 | Editor/HomeSceneBuilder.cs:90-98<br>Editor/HomeSceneBuilder.cs:103-111<br>Editor/HomeSceneBuilder.cs:116-124<br>Editor/HomeSceneBuilder.cs:129-137<br>Editor/TitleSceneBuilder.cs:75-83<br>Editor/TitleSceneBuilder.cs:86-94 | `EnsureHudBacking(hud); \| // --- Expenses panel ---` |
| 102 | 6 | Editor/OfficeSceneUIBuilder.Desk.cs:1299-1304<br>Editor/OfficeSceneUIBuilder.Desk.cs:1427-1433 | `private static StampTray BuildStampTray(Transform overlay, DeskConfigSO config) \| { \| Dest` |
| 101 | 5 | Editor/OfficeSceneUIBuilder.cs:1542-1546<br>Editor/OfficeSceneUIBuilder.cs:1546-1551 | `Button follow = MakeButton(win, "FollowHistoryButton", null, new Vector2(0.05f, 0.64f), ne` |

Periodic regions (same-shaped rows repeating; not counted as clones): Editor/ContentLibraryValidator.cs:59-88 (409 tokens), Editor/ContentLibraryValidator.cs:90-123 (136 tokens), Editor/HomeSceneBuilder.cs:90-126 (491 tokens), Editor/HomeSceneBuilder.cs:143-170 (255 tokens), Editor/OfficeSceneUIBuilder.Desk.cs:772-776 (163 tokens), Editor/OfficeSceneUIBuilder.cs:59-72 (181 tokens), Editor/OfficeSceneUIBuilder.cs:429-446 (154 tokens), Editor/OfficeSceneUIBuilder.cs:1159-1163 (176 tokens), Scripts/ContentLibrarySO.cs:219-252 (203 tokens), Scripts/ContentLibrarySO.cs:343-443 (335 tokens), Scripts/Office/OfficeSceneContractSO.cs:64-79 (325 tokens), Scripts/Visuals/ArabicShaper.cs:19-31 (504 tokens)

Duplicated lines per file (top 12): Editor/HomeSceneBuilder.cs 215, Editor/OfficeSceneUIBuilder.cs 195, Editor/OfficeSceneUIBuilder.Desk.cs 174, Editor/TitleSceneBuilder.cs 157, Scripts/GameManager.cs 84, Scripts/UI/HomeUIController.cs 65, Editor/WorldContentGenerator.cs 54, Scripts/Shift/ShiftScoring.cs 42, Scripts/UI/TravellerWheel.cs 42, Scripts/ContentLibrarySO.cs 39, Scripts/Domain/Translation.cs 33, Scripts/WorldState.cs 32

### Largest groups, test/exact, W=60 (1 groups)

| Tokens | Lines | Locations | Starts with |
|---|---|---|---|
| 62 | 13 | Tests/EditMode/LiesTests.cs:549-561<br>Tests/EditMode/LiesTests.cs:701-713 | `Assert.AreEqual(EvidenceKind.Answer, d.source, where); \| } \| else if (row.NationId == "ira` |

Duplicated lines per file (top 12): Tests/EditMode/LiesTests.cs 26

### Largest groups, test/normalized, W=60 (108 groups)

| Tokens | Lines | Locations | Starts with |
|---|---|---|---|
| 189 | 19 | Tests/EditMode/EffectOpsTests.cs:29-47<br>Tests/EditMode/GatesTests.cs:241-259 | `Assert.AreEqual(expected, EffectOps.ActsWhileActive(type)); \| }` |
| 180 | 13 | Tests/EditMode/EffectOpsTests.cs:36-48<br>Tests/EditMode/GatesTests.cs:247-259 | `Assert.AreEqual(1, (int)EffectOpType.ClearFlag); \| Assert.AreEqual(2, (int)EffectOpType.Ad` |
| 168 | 12 | Tests/EditMode/EffectOpsTests.cs:41-52<br>Tests/EditMode/GatesTests.cs:247-258 | `Assert.AreEqual(6, (int)EffectOpType.AddAttributeScore); \| Assert.AreEqual(7, (int)EffectO` |
| 166 | 12 | Tests/EditMode/EffectOpsTests.cs:35-46<br>Tests/EditMode/GatesTests.cs:248-259 | `Assert.AreEqual(0, (int)EffectOpType.SetFlag); \| Assert.AreEqual(1, (int)EffectOpType.Clea` |
| 154 | 11 | Tests/EditMode/EffectOpsTests.cs:42-52<br>Tests/EditMode/GatesTests.cs:247-257 | `Assert.AreEqual(7, (int)EffectOpType.AddNationScore); \| Assert.AreEqual(8, (int)EffectOpTy` |
| 153 | 17 | Tests/EditMode/SeedsTests.cs:49-65<br>Tests/EditMode/SeedsTests.cs:68-84 | `Assert.AreEqual(Seeds.ForViolators(daySeed), Seeds.ForViolators(daySeed)); \| }` |
| 152 | 11 | Tests/EditMode/EffectOpsTests.cs:35-45<br>Tests/EditMode/GatesTests.cs:249-259 | `Assert.AreEqual(0, (int)EffectOpType.SetFlag); \| Assert.AreEqual(1, (int)EffectOpType.Clea` |
| 140 | 10 | Tests/EditMode/EffectOpsTests.cs:43-52<br>Tests/EditMode/GatesTests.cs:247-256 | `Assert.AreEqual(8, (int)EffectOpType.LegendaryChanceBonus); \| Assert.AreEqual(9, (int)Effe` |
| 138 | 10 | Tests/EditMode/EffectOpsTests.cs:35-44<br>Tests/EditMode/GatesTests.cs:250-259 | `Assert.AreEqual(0, (int)EffectOpType.SetFlag); \| Assert.AreEqual(1, (int)EffectOpType.Clea` |
| 136 | 15 | Tests/EditMode/SeedsTests.cs:62-76<br>Tests/EditMode/SeedsTests.cs:82-96 | `Assert.IsFalse(cases.Contains(lie), $"case {c}: the lie seed is a case seed"); \| Assert.Ar` |

Periodic regions (same-shaped rows repeating; not counted as clones): Tests/EditMode/EffectOpsTests.cs:35-52 (252 tokens), Tests/EditMode/GatesTests.cs:247-259 (180 tokens)

Duplicated lines per file (top 12): Tests/EditMode/LiesTests.cs 125, Tests/EditMode/DiscrepancyLogTests.cs 85, Tests/EditMode/GatesTests.cs 65, Tests/EditMode/SpeechQueueTests.cs 61, Tests/EditMode/SeedsTests.cs 47, Tests/EditMode/CarriesTests.cs 38, Tests/EditMode/ComparePairTests.cs 37, Tests/EditMode/FactTableTests.cs 31, Tests/EditMode/EffectOpsTests.cs 27, Tests/EditMode/ExamineLayoutTests.cs 26, Tests/EditMode/InterviewTests.cs 25, Tests/EditMode/ShiftClockTests.cs 24

## Magic numbers and string literals

Tiers: **rule** = TimeDesk.Domain + Assets/Scripts root, Core, Shift, Timeline, Home, Endings, Investigation, Dialog, Characters; **presentation** = UI, Office, Visuals, DevTools; **editor**; **test**.

Numbers: allowed values [0, 1, -1, 2, 0.5, 100] (by value, sign included). Not magic: const fields and local consts, enum values, attribute arguments; initializers of serialized fields (`serialized_default`, tunable knobs) and of other fields/properties (`field_initializer`, a named value) are counted separately; `new T[n]` sizes stay magic (flagged `array_size`). Numbers inside interpolation holes count.

Strings: not counted as literals: const, attribute arguments (incl. [MenuItem] paths), empty strings, and log/exception text (arguments of Debug.Log*/Debug.Assert*/Assert.*/`new *Exception(...)`, counted as `log`). Field initializers bucketed as for numbers. An interpolated string is one literal.

| Tier | Magic numbers | Allowed | Const | Field init | Serialized default | String literals | Log/exception text | Const strings | String field init |
|---|---|---|---|---|---|---|---|---|---|
| rule | 52 | 491 | 24 | 11 | 58 | 201 | 248 | 29 | 28 |
| presentation | 310 | 802 | 49 | 83 | 194 | 293 | 40 | 15 | 46 |
| editor | 857 | 609 | 19 | 152 | 5 | 1308 | 96 | 25 | 26 |
| test | 1475 | 1891 | 34 | 88 | 0 | 1602 | 1469 | 15 | 157 |

Top files, rule tier (magic numbers / string literals): Scripts/Domain/InterviewScript.cs 0/27, Scripts/Domain/NameRoster.cs 11/14, Scripts/Domain/Looks.cs 0/16, Scripts/Domain/Translation.cs 0/16, Scripts/CaseFactory.cs 0/15, Scripts/Shift/ShiftScoring.cs 0/15, Scripts/Domain/LookKeys.cs 0/11, Scripts/Domain/DiscrepancyLog.cs 0/10, Scripts/OfficeUIController.cs 0/9, Scripts/Domain/ComparePair.cs 0/8

Top files, presentation tier (magic numbers / string literals): Scripts/DevTools/DebugPanelController.cs 29/65, Scripts/Visuals/WheelIconPlaceholder.cs 76/11, Scripts/UI/InvestigationUIController.cs 35/34, Scripts/Visuals/LayerPlaceholder.cs 54/0, Scripts/UI/HomeUIController.cs 11/32, Scripts/Visuals/PlaceholderPalette.cs 23/5, Scripts/Visuals/CulturePlaceholders.cs 26/0, Scripts/Visuals/Palette.cs 0/19, Scripts/Visuals/UiStrings.cs 1/17, Scripts/Visuals/Contrast.cs 12/4

Top files, editor tier (magic numbers / string literals): Editor/OfficeSceneUIBuilder.cs 426/361, Editor/OfficeSceneUIBuilder.Desk.cs 257/355, Editor/WorldContentGenerator.cs 3/302, Editor/HomeSceneBuilder.cs 109/78, Editor/ContentLibraryValidator.cs 1/114, Editor/TitleSceneBuilder.cs 46/35, Editor/WorldContentGenerator.Culture.cs 2/35, Editor/WorldContentGenerator.Translation.cs 0/16, Editor/OfficeSceneContractTools.cs 3/9, Editor/UiContrastCheck.cs 5/3

### Rule code: magic numbers (52)

| Where | Literal | Member | Note |
|---|---|---|---|
| Scripts/Characters/CharacterArt.cs:143 | `0.7f` | `CharacterArt.DrawPlaceholder` |  |
| Scripts/Characters/CharacterArt.cs:153 | `240` | `CharacterArt.DrawPlaceholder` |  |
| Scripts/Characters/CharacterArt.cs:153 | `236` | `CharacterArt.DrawPlaceholder` |  |
| Scripts/Characters/CharacterArt.cs:153 | `224` | `CharacterArt.DrawPlaceholder` |  |
| Scripts/Characters/CharacterArt.cs:158 | `0.55f` | `CharacterArt.DrawPlaceholder` |  |
| Scripts/Characters/TravellerView.cs:43 | `1e-6f` | `TravellerView.Stand` |  |
| Scripts/Characters/TravellerView.cs:64 | `0.1f` | `TravellerView.Stand` |  |
| Scripts/Core/UnityRandomSource.cs:16 | `0.99999994f` | `UnityRandomSource.Value` |  |
| Scripts/DayOrchestrator.cs:178 | `30f` | `DayOrchestrator.DayLoop` |  |
| Scripts/Domain/BirthDates.cs:32 | `4` | `BirthDates.TryParse` |  |
| Scripts/Domain/BirthDates.cs:32 | `3` | `BirthDates.TryParse` |  |
| Scripts/Domain/BirthDates.cs:33 | `3` | `BirthDates.TryParse` |  |
| Scripts/Domain/BirthDates.cs:72 | `29` | `BirthDates.Generate` |  |
| Scripts/Domain/Dialog.cs:67 | `3` | `DialogChoiceKinds.Rank` |  |
| Scripts/Domain/Dialog.cs:68 | `4` | `DialogChoiceKinds.Rank` |  |
| Scripts/Domain/Dialog.cs:69 | `5` | `DialogChoiceKinds.Rank` |  |
| Scripts/Domain/NameRoster.cs:97 | `1000` | `NameRoster.Roman` |  |
| Scripts/Domain/NameRoster.cs:97 | `900` | `NameRoster.Roman` |  |
| Scripts/Domain/NameRoster.cs:97 | `500` | `NameRoster.Roman` |  |
| Scripts/Domain/NameRoster.cs:97 | `400` | `NameRoster.Roman` |  |
| Scripts/Domain/NameRoster.cs:97 | `90` | `NameRoster.Roman` |  |
| Scripts/Domain/NameRoster.cs:97 | `50` | `NameRoster.Roman` |  |
| Scripts/Domain/NameRoster.cs:97 | `40` | `NameRoster.Roman` |  |
| Scripts/Domain/NameRoster.cs:97 | `10` | `NameRoster.Roman` |  |
| Scripts/Domain/NameRoster.cs:97 | `9` | `NameRoster.Roman` |  |
| Scripts/Domain/NameRoster.cs:97 | `5` | `NameRoster.Roman` |  |
| Scripts/Domain/NameRoster.cs:97 | `4` | `NameRoster.Roman` |  |
| Scripts/Domain/SeededRandom.cs:31 | `40` | `SeededRandom.Value` |  |
| Scripts/Domain/SeededRandom.cs:31 | `24` | `SeededRandom.Value` |  |
| Scripts/Domain/SeededRandom.cs:39 | `0x9E3779B97F4A7C15UL` | `SeededRandom.Next` |  |
| Scripts/Domain/SeededRandom.cs:41 | `30` | `SeededRandom.Next` |  |
| Scripts/Domain/SeededRandom.cs:41 | `0xBF58476D1CE4E5B9UL` | `SeededRandom.Next` |  |
| Scripts/Domain/SeededRandom.cs:42 | `27` | `SeededRandom.Next` |  |
| Scripts/Domain/SeededRandom.cs:42 | `0x94D049BB133111EBUL` | `SeededRandom.Next` |  |
| Scripts/Domain/SeededRandom.cs:43 | `31` | `SeededRandom.Next` |  |
| Scripts/Domain/Seeds.cs:34 | `397` | `Seeds.Day` |  |
| Scripts/Domain/Seeds.cs:34 | `7919` | `Seeds.Day` |  |
| Scripts/Domain/Seeds.cs:43 | `0x9E3779B97F4A7C15UL` | `Seeds.Mix` |  |
| Scripts/Domain/Seeds.cs:44 | `30` | `Seeds.Mix` |  |
| Scripts/Domain/Seeds.cs:44 | `0xBF58476D1CE4E5B9UL` | `Seeds.Mix` |  |
| Scripts/Domain/Seeds.cs:45 | `27` | `Seeds.Mix` |  |
| Scripts/Domain/Seeds.cs:45 | `0x94D049BB133111EBUL` | `Seeds.Mix` |  |
| Scripts/Domain/Seeds.cs:46 | `31` | `Seeds.Mix` |  |
| Scripts/Domain/ShiftClock.cs:107 | `60` | `ShiftClock.Format` |  |
| Scripts/Domain/ShiftClock.cs:107 | `60` | `ShiftClock.Format` |  |
| Scripts/Endings/EndingService.cs:70 | `-100` | `EndingService.Matches` |  |
| Scripts/Home/HomeEconomy.cs:133 | `10` | `HomeEconomy.AdvanceFamilyConditions` |  |
| Scripts/Home/HomeEconomy.cs:152 | `397` | `HomeEconomy.AdvanceFamilyConditions` |  |
| Scripts/Home/HomeEconomy.cs:152 | `104729` | `HomeEconomy.AdvanceFamilyConditions` |  |
| Scripts/Shift/ShiftClockDriver.cs:75 | `60` | `ShiftClockDriver.TryBuild` |  |
| Scripts/Shift/ShiftClockDriver.cs:75 | `60` | `ShiftClockDriver.TryBuild` |  |
| Scripts/Timeline/TimelineEffects.cs:100 | `90f` | `TimelineEffects.GetShopDiscountPercent` |  |

### Rule code: string literals (201; first 80, full list in JSON)

| Where | Literal | Member |
|---|---|---|
| Scripts/CaseFactory.cs:233 | `"Traveler"` | `CaseFactory.GenerateSingleCase` |
| Scripts/CaseFactory.cs:234 | `$"{givenName} ({role})"` | `CaseFactory.GenerateSingleCase` |
| Scripts/CaseFactory.cs:287 | `", "` | `CaseFactory.GenerateSingleCase` |
| Scripts/CaseFactory.cs:287 | `$"{t}/{lie.ChannelOf(t)}"` | `CaseFactory.GenerateSingleCase` |
| Scripts/CaseFactory.cs:288 | `"none"` | `CaseFactory.GenerateSingleCase` |
| Scripts/CaseFactory.cs:296 | `"an unlisted land"` | `CaseFactory.FallbackOriginLabel` |
| Scripts/CaseFactory.cs:496 | `"unknown"` | `CaseFactory.ResolveFieldValue` |
| Scripts/CaseFactory.cs:498 | `$"{category}:{e}"` | `CaseFactory.ResolveFieldValue` |
| Scripts/CaseFactory.cs:565 | `$"Subject #{caseIndex1Based}"` | `CaseFactory.ResolveGivenName` |
| Scripts/CaseFactory.cs:574 | `"Unknown"` | `CaseFactory.GenerateBirthDate` |
| Scripts/CaseFactory.cs:602 | `"No remarks on file."` | `CaseFactory.BuildRegistry` |
| Scripts/CaseFactory.cs:604 | `"Priority subject. Records sealed above your clearance."` | `CaseFactory.BuildRegistry` |
| Scripts/CaseFactory.cs:876 | `"Document"` | `CaseFactory.RenderDocText` |
| Scripts/CaseFactory.cs:880 | `"----------------"` | `CaseFactory.RenderDocText` |
| Scripts/CaseFactory.cs:883 | `"• "` | `CaseFactory.RenderDocText` |
| Scripts/Characters/CharacterArt.cs:79 | `"_photo"` | `CharacterArt.GetPhoto` |
| Scripts/Characters/CharacterArt.cs:113 | `$"{ResourcesFolder}/{key.Name}"` | `CharacterArt.EntryOf` |
| Scripts/Core/RunManager.cs:48 | `"RunManager"` | `RunManager.GetOrCreate` |
| Scripts/Core/SaveSystem.cs:90 | `".tmp"` | `SaveSystem.Save` |
| Scripts/DayOrchestrator.cs:234 | `$"{e.name} ({trigger}, slot {s})"` | `DayOrchestrator.WarnAboutUnreachedContent` |
| Scripts/DayOrchestrator.cs:238 | `$"forced case {forced.name} (slot {s})"` | `DayOrchestrator.WarnAboutUnreachedContent` |
| Scripts/DayOrchestrator.cs:242 | `$"forced premade {premade.displayName} (slot {s})"` | `DayOrchestrator.WarnAboutUnreachedContent` |
| Scripts/Domain/BirthDates.cs:19 | `$"{day} {Months[monthIndex0]} {-year} {Bce}"` | `BirthDates.Format` |
| Scripts/Domain/BirthDates.cs:19 | `$"{day} {Months[monthIndex0]} {year}"` | `BirthDates.Format` |
| Scripts/Domain/Carries.cs:89 | `$"{pair.from}_{pair.fromEra}"` | `Carries.Promote` |
| Scripts/Domain/ClueLabels.cs:10 | `"category."` | `ClueLabels.Key` |
| Scripts/Domain/ComparePair.cs:119 | `"field:"` | `PickKeys.Field` |
| Scripts/Domain/ComparePair.cs:119 | `":"` | `PickKeys.Field` |
| Scripts/Domain/ComparePair.cs:122 | `"line:"` | `PickKeys.Line` |
| Scripts/Domain/ComparePair.cs:125 | `"garment:"` | `PickKeys.Garment` |
| Scripts/Domain/ComparePair.cs:128 | `"book:"` | `PickKeys.BookRow` |
| Scripts/Domain/ComparePair.cs:128 | `":"` | `PickKeys.BookRow` |
| Scripts/Domain/ComparePair.cs:128 | `":"` | `PickKeys.BookRow` |
| Scripts/Domain/ComparePair.cs:131 | `"record:"` | `PickKeys.Record` |
| Scripts/Domain/Dialog.cs:99 | `"wheel_"` | `DialogChoiceKinds.IconName` |
| Scripts/Domain/DiscrepancyLog.cs:94 | `"agency records"` | `CompareEvidence.ForRecordField` |
| Scripts/Domain/DiscrepancyLog.cs:164 | `"foreignOrigin"` | `Discrepancy.ReportKeyFor` |
| Scripts/Domain/DiscrepancyLog.cs:165 | `"recordMismatch"` | `Discrepancy.ReportKeyFor` |
| Scripts/Domain/DiscrepancyLog.cs:166 | `"claimMismatch"` | `Discrepancy.ReportKeyFor` |
| Scripts/Domain/DiscrepancyLog.cs:167 | `"said"` | `Discrepancy.ReportKeyFor` |
| Scripts/Domain/DiscrepancyLog.cs:167 | `"worn"` | `Discrepancy.ReportKeyFor` |
| Scripts/Domain/DiscrepancyLog.cs:167 | `"papers"` | `Discrepancy.ReportKeyFor` |
| Scripts/Domain/DiscrepancyLog.cs:168 | `"deviation."` | `Discrepancy.ReportKeyFor` |
| Scripts/Domain/DiscrepancyLog.cs:168 | `"."` | `Discrepancy.ReportKeyFor` |
| Scripts/Domain/DiscrepancyLog.cs:282 | `"a different era"` | `DiscrepancyLog.Prove` |
| Scripts/Domain/Gates.cs:261 | `$"trig:{triggerId}:fired"` | `FlagKeys.TriggerFired` |
| Scripts/Domain/Gates.cs:264 | `$"dlg:{dialogId}:done"` | `FlagKeys.DialogDone` |
| Scripts/Domain/Gates.cs:267 | `$"premade:{premadeId}:met"` | `FlagKeys.PremadeMet` |
| Scripts/Domain/HistoryChecks.cs:31 | `$"History edit '{e.source}' ({e.nationId}_{e.eraId} {e.category} = '{e` | `HistoryChecks.Problems` |
| Scripts/Domain/HistoryChecks.cs:34 | `$"{what} has a blank value."` | `HistoryChecks.Problems` |
| Scripts/Domain/HistoryChecks.cs:39 | `$"{what} is {e.value.Length} characters long; a book row holds at most` | `HistoryChecks.Problems` |
| Scripts/Domain/HistoryChecks.cs:43 | `$"{what} changes {e.category}, which history may not edit (only {strin` | `HistoryChecks.Problems` |
| Scripts/Domain/HistoryChecks.cs:49 | `$"{what} names a place the world does not have."` | `HistoryChecks.Problems` |
| Scripts/Domain/HistoryChecks.cs:54 | `$"{what} equals the place's own value, so it would change nothing."` | `HistoryChecks.Problems` |
| Scripts/Domain/HistoryChecks.cs:56 | `$"{what} equals {other.OriginLabel}'s {e.category}, so neither could l` | `HistoryChecks.Problems` |
| Scripts/Domain/HistoryChecks.cs:65 | `$"{what} gives the value of history edit '{o.source}' to another place` | `HistoryChecks.Problems` |
| Scripts/Domain/Interview.cs:111 | `"{"` | `Interview.Placeholder` |
| Scripts/Domain/Interview.cs:111 | `"}"` | `Interview.Placeholder` |
| Scripts/Domain/InterviewDay.cs:60 | `$"Dialog '{d.Item.id}' is not offered: {problem}"` | `InterviewDay.InterviewDay` |
| Scripts/Domain/InterviewScript.cs:57 | `$"{dialogId}.{choiceId}"` | `InterviewScript.ChoiceLineId` |
| Scripts/Domain/InterviewScript.cs:140 | `$"request:{i}"` | `InterviewScript.Build` |
| Scripts/Domain/InterviewScript.cs:163 | `$"act:{r.id}"` | `InterviewScript.Build` |
| Scripts/Domain/InterviewScript.cs:176 | `"back"` | `InterviewScript.Build` |
| Scripts/Domain/InterviewScript.cs:189 | `$"q:{q.id}"` | `InterviewScript.Build` |
| Scripts/Domain/InterviewScript.cs:202 | `"smalltalk"` | `InterviewScript.Build` |
| Scripts/Domain/InterviewScript.cs:215 | `"ask"` | `InterviewScript.Build` |
| Scripts/Domain/InterviewScript.cs:218 | `"back"` | `InterviewScript.Build` |
| Scripts/Domain/InterviewScript.cs:222 | `$"look:{i}"` | `InterviewScript.Build` |
| Scripts/Domain/InterviewScript.cs:225 | `"look"` | `InterviewScript.Build` |
| Scripts/Domain/InterviewScript.cs:234 | `$"dlg:{d.id}"` | `InterviewScript.Build` |
| Scripts/Domain/InterviewScript.cs:291 | `$"{d.id}/{nodeId}"` | `InterviewScript.NodeId` |
| Scripts/Domain/InterviewScript.cs:334 | `"the dialog has no nodes"` | `DialogChecks.Problems` |
| Scripts/Domain/InterviewScript.cs:346 | `"a node is empty"` | `DialogChecks.Problems` |
| Scripts/Domain/InterviewScript.cs:351 | `$"node '{node.id}' is listed twice"` | `DialogChecks.Problems` |
| Scripts/Domain/InterviewScript.cs:358 | `$"choice '{choice.id}' is listed twice"` | `DialogChecks.Problems` |
| Scripts/Domain/InterviewScript.cs:362 | `$"choice '{choice.id}' has an effect but does not end the dialog"` | `DialogChecks.Problems` |
| Scripts/Domain/InterviewScript.cs:364 | `$"choice '{choice.id}' has an effect, but the dialog is repeatable (a ` | `DialogChecks.Problems` |
| Scripts/Domain/InterviewScript.cs:368 | `$"node '{node.id}' has no choices"` | `DialogChecks.Problems` |
| Scripts/Domain/InterviewScript.cs:370 | `$"node '{node.id}' offers {Choices(node).Count} choices; the traveller` | `DialogChecks.Problems` |
| Scripts/Domain/InterviewScript.cs:376 | `$"choice '{choice.id}' leads to unknown node '{choice.next}'"` | `DialogChecks.Problems` |

Rule code also has 29 literals in named field initializers and 68 serialized defaults (JSON).

## Per-frame paths

Roots: Update/LateUpdate/FixedUpdate/OnGUI of runtime types (Assets/Scripts), plus method groups registered with `+=` on EditorApplication.update, Canvas.willRenderCanvases, RenderPipelineManager.*, Application.onBeforeRender, Camera.onPre*/onPostRender. Reachability follows calls resolved by simple name to methods of the same (partial) type, and bare uses of the type's properties to their accessors; calls into other types, base classes, events, coroutines and callbacks are NOT followed. Sites are reported once, with the first root that reaches them (`roots` in JSON lists all).

21 roots: `DebugPanelController.Update()`, `DebugPanelController.OnGUI()`, `DeskController.Update()`, `DeskDocument.Update()`, `DeskReaction.Update()`, `PaperExaminer.LateUpdate()`, `DeskView.Update()`, `NoteBacking.LateUpdate()`, `OfficeHallCrowdPalette.Update()`, `OfficeReadouts.Update()`, `OfficeTrafficVehicle.Update()`, `OfficeViewController.Update()`, `OfficeWindowGlass.Update()`, `PcFrame.LateUpdate()`, `ShiftClockReadouts.Update()`, `ShiftClockDriver.Update()`, `DocumentWindowController.Update()`, `HoverHighlighter.LateUpdate()`, `OverlayCallout.LateUpdate()`, `StampTray.LateUpdate()`, `TravellerWheel.LateUpdate()`

### Find / GetComponent (9): {'FindObject*': 1, 'GetComponent*': 2, 'GetComponent*(includeInactive)': 1, 'TryGetComponent (non-allocating)': 5}

| Where | Kind | Chain (callee <- root) | Code |
|---|---|---|---|
| Scripts/Office/Desk/DeskController.cs:132 | GetComponent* | DeskController.Update() | `Slide(paper, paper.GetComponent<DeskDraggable>().PickUpPosition);` |
| Scripts/Office/Desk/DeskController.cs:443 | GetComponent* | DeskController.Release(DeskDocument, bool) <- DeskController.PutBackAll(bool) <- DeskController.Update() | `paper.GetComponent<DeskDraggable>().GrabAtCentre = false;` |
| Scripts/Office/OfficeHallCrowdPalette.cs:37 | FindObject* | OfficeHallCrowdPalette.Refresh() <- OfficeHallCrowdPalette.Update() | `orchestrator = UnityEngine.Object.FindAnyObjectByType<DayOrchestrator>();` |
| Scripts/Office/OfficeWindowGlass.cs:48 | GetComponent*(includeInactive) | OfficeWindowGlass.Apply() <- OfficeWindowGlass.Update() | `panes = GetComponentsInChildren<Renderer>(true);` |
| Scripts/UI/HoverHighlighter.cs:158 | TryGetComponent (non-allocating) | HoverHighlighter.NearestCandidate(GameObject) <- HoverHighlighter.FindHoverTarget() <- HoverHighlighter.LateUpdate() | `if (t.TryGetComponent(out Clickable clickable))` |
| Scripts/UI/HoverHighlighter.cs:160 | TryGetComponent (non-allocating) | HoverHighlighter.NearestCandidate(GameObject) <- HoverHighlighter.FindHoverTarget() <- HoverHighlighter.LateUpdate() | `if (t.TryGetComponent(out Selectable selectable))` |
| Scripts/UI/HoverHighlighter.cs:187 | TryGetComponent (non-allocating) | HoverHighlighter.SetHighlighted(Component, bool) <- HoverHighlighter.LateUpdate() | `if (r != null && r.TryGetComponent(out MeshFilter filter) && filter.sharedMesh !` |
| Scripts/UI/HoverHighlighter.cs:195 | TryGetComponent (non-allocating) | HoverHighlighter.SetHighlighted(Component, bool) <- HoverHighlighter.LateUpdate() | `if (!host.TryGetComponent(out HoverUIOutline uiOutline))` |
| Scripts/UI/HoverHighlighter.cs:204 | TryGetComponent (non-allocating) | HoverHighlighter.SetHighlighted(Component, bool) <- HoverHighlighter.LateUpdate() | `bool rings = theme != null && theme.ActiveTheme != null && host.TryGetComponent(` |

### Allocations (69): {'LINQ .Select()': 1, 'LINQ .Where()': 2, 'ToString()': 3, 'foreach over LINQ': 2, 'lambda': 4, 'new MaterialPropertyBlock': 2, 'new PointerEventData': 2, 'new StringBuilder': 1, 'string concatenation': 3, 'string interpolation': 44, 'string.Join': 5}

Kinds: `new` of reference types (Unity/BCL value types and structs declared in our code excluded), arrays, string interpolation/concatenation/Format/Join/Concat, ToString(), lambdas and anonymous methods (delegate/closure), LINQ calls (files with `using System.Linq`; Contains/Reverse skipped as ambiguous), ToList/ToArray, foreach over LINQ. Boxing and params arrays are not detected.

| Where | Kind | Chain (callee <- root) | Code |
|---|---|---|---|
| Scripts/DevTools/DebugPanelController.cs:48 | string interpolation | DebugPanelController.Update() | `Debug.Log($"[DebugPanelController] Overlay {(_visible ? "opened" : "closed")} (~` |
| Scripts/DevTools/DebugPanelController.cs:77 | string interpolation | DebugPanelController.OnGUI() | `GUILayout.Label($"Day {world.day}   Money {world.money}   Stability {world.timel` |
| Scripts/DevTools/DebugPanelController.cs:128 | string interpolation | DebugPanelController.DrawCheatsTab(RunManager, WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `Debug.Log($"[DebugPanelController] Cheat: Force leader '{nation.id}'.");` |
| Scripts/DevTools/DebugPanelController.cs:161 | string interpolation | DebugPanelController.DrawCheatsTab(RunManager, WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `Debug.Log($"[DebugPanelController] Cheat: SetFlag('{_flagInput.Trim()}').");` |
| Scripts/DevTools/DebugPanelController.cs:167 | string interpolation | DebugPanelController.DrawCheatsTab(RunManager, WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `Debug.Log($"[DebugPanelController] Cheat: ClearFlag('{_flagInput.Trim()}').");` |
| Scripts/DevTools/DebugPanelController.cs:173 | string interpolation | DebugPanelController.DrawCheatsTab(RunManager, WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `GUILayout.Label($"Active flags ({world.flags.Count}):");` |
| Scripts/DevTools/DebugPanelController.cs:180 | string concatenation | DebugPanelController.DrawCheatsTab(RunManager, WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `GUILayout.Label("  " + flag);` |
| Scripts/DevTools/DebugPanelController.cs:184 | string interpolation | DebugPanelController.DrawCheatsTab(RunManager, WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `Debug.Log($"[DebugPanelController] Cheat: ClearFlag('{flag}') (from active list)` |
| Scripts/DevTools/DebugPanelController.cs:198 | string interpolation | DebugPanelController.DrawCheatsTab(RunManager, WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `Debug.Log($"[DebugPanelController] Cheat: ForceLegendaryNextCase set to {newForc` |
| Scripts/DevTools/DebugPanelController.cs:219 | string interpolation | DebugPanelController.DrawCheatsTab(RunManager, WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `GUILayout.Label($"  {upgrade.displayName} ({upgrade.id})", GUILayout.Width(260f)` |
| Scripts/DevTools/DebugPanelController.cs:225 | string interpolation | DebugPanelController.DrawCheatsTab(RunManager, WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `Debug.Log($"[DebugPanelController] Cheat: removing upgrade '{upgrade.id}'.");` |
| Scripts/DevTools/DebugPanelController.cs:233 | string interpolation | DebugPanelController.DrawCheatsTab(RunManager, WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `Debug.Log($"[DebugPanelController] Cheat: unlocking upgrade '{upgrade.id}'.");` |
| Scripts/DevTools/DebugPanelController.cs:249 | string interpolation | DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `GUILayout.Label($"Scores ({world.timeline.scores.Count}):");` |
| Scripts/DevTools/DebugPanelController.cs:252 | string interpolation | DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `GUILayout.Label($"  {s.key} = {s.value:0.##}");` |
| Scripts/DevTools/DebugPanelController.cs:255 | string interpolation | DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `GUILayout.Label($"Dominant keys ({world.timeline.dominantKeys.Count}):");` |
| Scripts/DevTools/DebugPanelController.cs:258 | string concatenation | DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `GUILayout.Label("  " + key);` |
| Scripts/DevTools/DebugPanelController.cs:261 | string interpolation | DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `GUILayout.Label($"Supporting keys ({world.timeline.supportingKeys.Count}):");` |
| Scripts/DevTools/DebugPanelController.cs:264 | string concatenation | DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `GUILayout.Label("  " + key);` |
| Scripts/DevTools/DebugPanelController.cs:267 | string interpolation | DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `GUILayout.Label($"Active effects ({world.timeline.activeEffects.Count}):");` |
| Scripts/DevTools/DebugPanelController.cs:276 | string interpolation | DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `: $"{Mathf.Max(0, entry.startDay + entry.durationDays - world.day)}d left";` |
| Scripts/DevTools/DebugPanelController.cs:278 | string interpolation | DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `GUILayout.Label($"  {name} — {entry.sourceLabel} (started day {entry.startDay}, ` |
| Scripts/DevTools/DebugPanelController.cs:299 | string interpolation | DebugPanelController.CultureSummary() <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `yield return $"Present culture: {s.ActiveCultureId ?? "neutral"} (cue)";` |
| Scripts/DevTools/DebugPanelController.cs:300 | string interpolation | DebugPanelController.CultureSummary() <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `yield return $"Theme: {s.ActiveTheme.displayName} · labels: {s.Language} · font:` |
| Scripts/DevTools/DebugPanelController.cs:301 | string interpolation | DebugPanelController.CultureSummary() <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `yield return $"Missing UI strings: {s.Strings.MissingKeys.Count}";` |
| Scripts/DevTools/DebugPanelController.cs:309 | string interpolation | DebugPanelController.HistorySummary(WorldState) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `yield return $"History: leader '{h.leaderId}' (since day {h.leaderSinceDay}){for` |
| Scripts/DevTools/DebugPanelController.cs:310 | LINQ .Select() | DebugPanelController.HistorySummary(WorldState) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `yield return $"Ranking: {string.Join(", ", h.ranking.Select(r => $"{r.id} {r.sco` |
| Scripts/DevTools/DebugPanelController.cs:310 | lambda | DebugPanelController.HistorySummary(WorldState) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `yield return $"Ranking: {string.Join(", ", h.ranking.Select(r => $"{r.id} {r.sco` |
| Scripts/DevTools/DebugPanelController.cs:310 | string interpolation | DebugPanelController.HistorySummary(WorldState) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `yield return $"Ranking: {string.Join(", ", h.ranking.Select(r => $"{r.id} {r.sco` |
| Scripts/DevTools/DebugPanelController.cs:310 | string interpolation | DebugPanelController.HistorySummary(WorldState) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `yield return $"Ranking: {string.Join(", ", h.ranking.Select(r => $"{r.id} {r.sco` |
| Scripts/DevTools/DebugPanelController.cs:310 | string.Join | DebugPanelController.HistorySummary(WorldState) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `yield return $"Ranking: {string.Join(", ", h.ranking.Select(r => $"{r.id} {r.sco` |
| Scripts/DevTools/DebugPanelController.cs:311 | string interpolation | DebugPanelController.HistorySummary(WorldState) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `yield return $"Fact edits ({h.factEdits.Count}):";` |
| Scripts/DevTools/DebugPanelController.cs:312 | LINQ .Where() | DebugPanelController.HistorySummary(WorldState) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `foreach (FactEdit e in h.factEdits.Where(e => e != null))` |
| Scripts/DevTools/DebugPanelController.cs:312 | foreach over LINQ | DebugPanelController.HistorySummary(WorldState) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `foreach (FactEdit e in h.factEdits.Where(e => e != null))` |
| Scripts/DevTools/DebugPanelController.cs:312 | lambda | DebugPanelController.HistorySummary(WorldState) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `foreach (FactEdit e in h.factEdits.Where(e => e != null))` |
| Scripts/DevTools/DebugPanelController.cs:313 | string interpolation | DebugPanelController.HistorySummary(WorldState) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `yield return $"  {e.nationId}_{e.eraId} {e.category} = '{e.value}' (from day {e.` |
| Scripts/DevTools/DebugPanelController.cs:314 | string interpolation | DebugPanelController.HistorySummary(WorldState) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `yield return $"Pending carries ({h.pendingCarries.Count}):";` |
| Scripts/DevTools/DebugPanelController.cs:315 | LINQ .Where() | DebugPanelController.HistorySummary(WorldState) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `foreach (CarryRecord c in h.pendingCarries.Where(c => c != null))` |
| Scripts/DevTools/DebugPanelController.cs:315 | foreach over LINQ | DebugPanelController.HistorySummary(WorldState) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `foreach (CarryRecord c in h.pendingCarries.Where(c => c != null))` |
| Scripts/DevTools/DebugPanelController.cs:315 | lambda | DebugPanelController.HistorySummary(WorldState) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `foreach (CarryRecord c in h.pendingCarries.Where(c => c != null))` |
| Scripts/DevTools/DebugPanelController.cs:316 | string interpolation | DebugPanelController.HistorySummary(WorldState) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `yield return $"  {c.fromNationId}_{c.fromEraId} -> {c.toNationId}_{c.toEraId}: '` |
| Scripts/DevTools/DebugPanelController.cs:324 | string interpolation | DebugPanelController.AddMoney(WorldState, int) <- DebugPanelController.DrawCheatsTab(RunManager, WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `Debug.Log($"[DebugPanelController] Cheat: money {before} -> {world.money} ({delt` |
| Scripts/DevTools/DebugPanelController.cs:332 | string interpolation | DebugPanelController.AddStability(WorldState, float) <- DebugPanelController.DrawCheatsTab(RunManager, WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `Debug.Log($"[DebugPanelController] Cheat: stability {before:0.#} -> {world.timel` |
| Scripts/DevTools/DebugPanelController.cs:340 | string interpolation | DebugPanelController.SetStability(WorldState, float) <- DebugPanelController.DrawCheatsTab(RunManager, WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `Debug.Log($"[DebugPanelController] Cheat: stability {before:0.#} -> {world.timel` |
| Scripts/DevTools/DebugPanelController.cs:346 | new StringBuilder | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `var sb = new StringBuilder();` |
| Scripts/DevTools/DebugPanelController.cs:349 | string interpolation | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `sb.AppendLine($"Day {world.day}, money={world.money}, stability={world.timelineS` |
| Scripts/DevTools/DebugPanelController.cs:350 | string interpolation | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `sb.AppendLine($"legendaryChanceBonus={world.legendaryChanceBonus:0.##}, forgeryC` |
| Scripts/DevTools/DebugPanelController.cs:351 | string interpolation | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `sb.AppendLine($"Flags ({world.flags.Count}): {string.Join(", ", world.flags)}");` |
| Scripts/DevTools/DebugPanelController.cs:351 | string.Join | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `sb.AppendLine($"Flags ({world.flags.Count}): {string.Join(", ", world.flags)}");` |
| Scripts/DevTools/DebugPanelController.cs:352 | string interpolation | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `sb.AppendLine($"Unlocked upgrades ({world.unlockedUpgradeIds.Count}): {string.Jo` |
| Scripts/DevTools/DebugPanelController.cs:352 | string.Join | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `sb.AppendLine($"Unlocked upgrades ({world.unlockedUpgradeIds.Count}): {string.Jo` |
| Scripts/DevTools/DebugPanelController.cs:354 | string interpolation | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `sb.AppendLine($"Counters ({world.counters.Count}):");` |
| Scripts/DevTools/DebugPanelController.cs:356 | string interpolation | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `sb.AppendLine($"  {c.key} = {c.value}");` |
| Scripts/DevTools/DebugPanelController.cs:358 | string interpolation | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `sb.AppendLine($"Scores ({world.timeline.scores.Count}):");` |
| Scripts/DevTools/DebugPanelController.cs:360 | string interpolation | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `sb.AppendLine($"  {s.key} = {s.value:0.##}");` |
| Scripts/DevTools/DebugPanelController.cs:362 | string interpolation | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `sb.AppendLine($"Dominant: {string.Join(", ", world.timeline.dominantKeys)}");` |
| Scripts/DevTools/DebugPanelController.cs:362 | string.Join | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `sb.AppendLine($"Dominant: {string.Join(", ", world.timeline.dominantKeys)}");` |
| Scripts/DevTools/DebugPanelController.cs:363 | string interpolation | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `sb.AppendLine($"Supporting: {string.Join(", ", world.timeline.supportingKeys)}")` |
| Scripts/DevTools/DebugPanelController.cs:363 | string.Join | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `sb.AppendLine($"Supporting: {string.Join(", ", world.timeline.supportingKeys)}")` |
| Scripts/DevTools/DebugPanelController.cs:365 | string interpolation | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `sb.AppendLine($"Active effects ({world.timeline.activeEffects.Count}):");` |
| Scripts/DevTools/DebugPanelController.cs:370 | string interpolation | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `sb.AppendLine($"  {name} — {entry.sourceLabel} (startDay={entry.startDay}, durat` |
| Scripts/DevTools/DebugPanelController.cs:376 | ToString() | DebugPanelController.DumpStateToConsole(WorldState, ContentLibrarySO) <- DebugPanelController.DrawInspectorTab(WorldState, ContentLibrarySO) <- DebugPanelController.OnGUI() | `Debug.Log(sb.ToString());` |
| Scripts/Office/Desk/DeskController.cs:497 | lambda | DeskController.Slide(DeskDocument, Vector3) <- DeskController.Update() | `paper.SlideTo(target, config.paperSlideSeconds, () => ApplyLive(paper));` |
| Scripts/Office/DeskView.cs:212 | new PointerEventData | DeskView.TopHandler(Vector2, out bool) <- DeskView.OnEmptySpace(Vector2) <- DeskView.Update() | `events.RaycastAll(new PointerEventData(events) { position = screen }, _hits);` |
| Scripts/Office/OfficeHallCrowdPalette.cs:41 | new MaterialPropertyBlock | OfficeHallCrowdPalette.Refresh() <- OfficeHallCrowdPalette.Update() | `appliedBlend=blend;properties??=new MaterialPropertyBlock();` |
| Scripts/Office/OfficeReadouts.cs:69 | ToString() | OfficeReadouts.Apply(WorldState) <- OfficeReadouts.Update() | `_dayText.text = world.day.ToString("00");` |
| Scripts/Office/OfficeReadouts.cs:73 | string interpolation | OfficeReadouts.Apply(WorldState) <- OfficeReadouts.Update() | `_stabilityText.text = $"{world.timelineStability:0}%";` |
| Scripts/Office/OfficeReadouts.cs:81 | ToString() | OfficeReadouts.Apply(WorldState) <- OfficeReadouts.Update() | `_creditsText.text = world.money.ToString();` |
| Scripts/Office/OfficeWindowGlass.cs:52 | new MaterialPropertyBlock | OfficeWindowGlass.Apply() <- OfficeWindowGlass.Update() | `properties ??= new MaterialPropertyBlock();` |
| Scripts/UI/HoverHighlighter.cs:143 | new PointerEventData | HoverHighlighter.PointerHitObject() <- HoverHighlighter.FindHoverTarget() <- HoverHighlighter.LateUpdate() | `_pointerData = new PointerEventData(eventSystem);` |

## Static mutable state (63): {'static auto-property with setter': 2, 'static field': 8, 'static readonly mutable collection': 53}

Static non-const non-readonly fields; static readonly fields of a mutable collection/array/StringBuilder type; static auto-properties with a set/init accessor; static events.

| Where | Member | Type | Kind | Access | Assembly | Role |
|---|---|---|---|---|---|---|
| Editor/ContentLibraryValidator.cs:779 | `ContentLibraryValidator.RequiredFacts` | `ClueCategory[]` | static readonly mutable collection | private | Assembly-CSharp-Editor | editor |
| Editor/OfficeSceneUIBuilder.Desk.cs:98 | `OfficeSceneUIBuilder.DeskSlots` | `(string id, DeskSlotKind kind, Vector3 position)[]` | static readonly mutable collection | private | Assembly-CSharp-Editor | editor |
| Editor/OfficeSceneUIBuilder.cs:1276 | `OfficeSceneUIBuilder.ArrowCursorShape` | `(float x, float y)[]` | static readonly mutable collection | private | Assembly-CSharp-Editor | editor |
| Editor/OfficeSceneUIBuilder.cs:1282 | `OfficeSceneUIBuilder.HandCursorShape` | `(float x, float y)[]` | static readonly mutable collection | private | Assembly-CSharp-Editor | editor |
| Editor/UiContrastCheck.cs:28 | `UiContrastCheck.Textures` | `Dictionary<string, Texture2D>` | static readonly mutable collection | private | Assembly-CSharp-Editor | editor |
| Editor/WorldContentGenerator.Culture.cs:28 | `WorldContentGenerator.WallpaperColours` | `(string art, string seed)[]` | static readonly mutable collection | private | Assembly-CSharp-Editor | editor |
| Editor/WorldContentGenerator.Translation.cs:22 | `WorldContentGenerator.TranslationKeys` | `string[]` | static readonly mutable collection | private | Assembly-CSharp-Editor | editor |
| Editor/WorldContentGenerator.cs:49 | `WorldContentGenerator.OwnedFolders` | `string[]` | static readonly mutable collection | private | Assembly-CSharp-Editor | editor |
| Scripts/Core/RunManager.cs:15 | `RunManager.Instance` | `RunManager` | static auto-property with setter | public | Assembly-CSharp | runtime |
| Scripts/DevTools/DebugPanelController.cs:21 | `DebugPanelController.TabLabels` | `string[]` | static readonly mutable collection | private | Assembly-CSharp | runtime |
| Scripts/DevTools/DevToolsState.cs:15 | `DevToolsState.ForceLegendaryNextCase` | `bool` | static field | public | Assembly-CSharp | runtime |
| Scripts/DevTools/DevToolsState.cs:22 | `DevToolsState.ForcedLeaderId` | `string` | static field | public | Assembly-CSharp | runtime |
| Scripts/Domain/BirthDates.cs:11 | `BirthDates.Months` | `string[]` | static readonly mutable collection | private | TimeDesk.Domain | runtime |
| Scripts/Domain/History.cs:129 | `History.EditableCategories` | `ClueCategory[]` | static readonly mutable collection | public | TimeDesk.Domain | runtime |
| Scripts/Domain/Lies.cs:62 | `LiePlan.NoTells` | `ClueCategory[]` | static readonly mutable collection | private | TimeDesk.Domain | runtime |
| Scripts/Domain/NameRoster.cs:26 | `NameRoster.Suffixes` | `Lazy<HashSet<string>>` | static readonly mutable collection | private | TimeDesk.Domain | runtime |
| Scripts/Office/OfficeScenes.cs:16 | `OfficeScenes._config` | `RunConfigSO` | static field | private | Assembly-CSharp | runtime |
| Scripts/Office/OfficeScenes.cs:17 | `OfficeScenes._gameplayRequested` | `bool` | static field | private | Assembly-CSharp | runtime |
| Scripts/Office/OfficeScenes.cs:18 | `OfficeScenes._artRequested` | `bool` | static field | private | Assembly-CSharp | runtime |
| Scripts/UI/Theme/CultureThemeService.cs:24 | `CultureThemeService.Instance` | `CultureThemeService` | static auto-property with setter | public | Assembly-CSharp | runtime |
| Scripts/UI/Theme/UiText.cs:29 | `UiText._readingOnly` | `UiStrings` | static field | private | Assembly-CSharp | runtime |
| Scripts/UI/Theme/UiText.cs:32 | `UiText._readingEntries` | `List<UiStringEntry>` | static field | private | Assembly-CSharp | runtime |
| Scripts/UI/Theme/UiText.cs:35 | `UiText._configLibrary` | `ContentLibrarySO` | static field | private | Assembly-CSharp | runtime |
| Scripts/UI/Theme/UiText.cs:38 | `UiText.WarnedKeys` | `HashSet<string>` | static readonly mutable collection | private | Assembly-CSharp | runtime |
| Scripts/Visuals/ArabicShaper.cs:17 | `ArabicShaper.Forms` | `Dictionary<char, (char iso, char fin, char ini, char med)>` | static readonly mutable collection | private | TimeDesk.Visuals | runtime |
| Scripts/Visuals/ArabicShaper.cs:35 | `ArabicShaper.LamAlef` | `Dictionary<char, (char iso, char fin)>` | static readonly mutable collection | private | TimeDesk.Visuals | runtime |
| Scripts/Visuals/ArabicShaper.cs:41 | `ArabicShaper.Mirror` | `Dictionary<char, char>` | static readonly mutable collection | private | TimeDesk.Visuals | runtime |
| Scripts/Visuals/LayerPlaceholder.cs:86 | `LayerPlaceholder.Torso` | `(float x, float y)[]` | static readonly mutable collection | private | TimeDesk.Visuals | runtime |
| Scripts/Visuals/LayerPlaceholder.cs:90 | `LayerPlaceholder.Arms` | `(float x, float y)[][]` | static readonly mutable collection | private | TimeDesk.Visuals | runtime |
| Scripts/Visuals/LayerPlaceholder.cs:97 | `LayerPlaceholder.Legs` | `(float x, float y)[][]` | static readonly mutable collection | private | TimeDesk.Visuals | runtime |
| Scripts/Visuals/LayerPlaceholder.cs:104 | `LayerPlaceholder.Dress` | `(float x, float y)[]` | static readonly mutable collection | private | TimeDesk.Visuals | runtime |
| Scripts/Visuals/LayerPlaceholder.cs:108 | `LayerPlaceholder.Sleeves` | `(float x, float y)[][]` | static readonly mutable collection | private | TimeDesk.Visuals | runtime |
| Scripts/Visuals/PlaceholderPalette.cs:9 | `PlaceholderPalette.SkinSwatches` | `(byte r, byte g, byte b)[]` | static readonly mutable collection | private | TimeDesk.Visuals | runtime |
| Tests/EditMode/BoothRulesTests.cs:34 | `BoothRulesTests.Rows` | `Row[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/DominanceTiersTests.cs:6 | `DominanceTiersTests.Athens` | `RankedScore[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/ForgeryTests.cs:13 | `ForgeryTests.BookCategories` | `HashSet<ClueCategory>` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/InfluenceTests.cs:12 | `InfluenceTests.Nations` | `string[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/InterviewScriptTests.cs:344 | `InterviewScriptTests.Said3` | `DialogLine[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/InterviewTests.cs:15 | `InterviewTests.Books` | `HashSet<ClueCategory>` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/InterviewTests.cs:142 | `InterviewTests.PlaceLines` | `LineText[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/InterviewTests.cs:143 | `InterviewTests.EraLines` | `LineText[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/LiesTests.cs:23 | `LiesTests.Books` | `HashSet<ClueCategory>` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/LiesTests.cs:26 | `LiesTests.None` | `ClueCategory[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/LiesTests.cs:29 | `LiesTests.PapersOnly` | `TellChannel[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/LiesTests.cs:32 | `LiesTests.AnswerOnly` | `TellChannel[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/LiesTests.cs:35 | `LiesTests.Both` | `TellChannel[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/LiesTests.cs:41 | `LiesTests.Today4` | `HomeCandidate[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/LiesTests.cs:368 | `LiesTests.AnswerBooks` | `HashSet<ClueCategory>` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/LiesTests.cs:575 | `LiesTests.All3` | `TellChannel[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/LiesTests.cs:578 | `LiesTests.BooksAndDress` | `HashSet<ClueCategory>` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/NameRosterTests.cs:6 | `NameRosterTests.Pool` | `string[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/PaletteTests.cs:10 | `PaletteTests.BaseSeeds` | `Dictionary<string, Rgba>` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/PaletteTests.cs:17 | `PaletteTests.Culture` | `Dictionary<string, Rgba>` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/PaletteTests.cs:133 | `PaletteTests.NeutralDiegetic` | `List<ResolvedRole>` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/PixelShapesTests.cs:6 | `PixelShapesTests.Square` | `(float x, float y)[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/PixelShapesTests.cs:9 | `PixelShapesTests.Arrow` | `(float x, float y)[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/PseudoscriptTests.cs:18 | `PseudoscriptTests.Starters` | `string[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/TranslationTests.cs:31 | `TranslationTests.Scripts` | `string[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/TravellerGendersTests.cs:9 | `TravellerGendersTests.Male` | `string[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/TravellerGendersTests.cs:10 | `TravellerGendersTests.Female` | `string[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/UiStringsTests.cs:12 | `UiStringsTests.Reading` | `UiStringEntry[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/UiStringsTests.cs:23 | `UiStringsTests.Culture` | `UiStringEntry[]` | static readonly mutable collection | private | TimeDeskEditMode | test |
| Tests/EditMode/WheelIconPlaceholderTests.cs:8 | `WheelIconPlaceholderTests.Names` | `string[]` | static readonly mutable collection | private | TimeDeskEditMode | test |

## Singleton access (40 sites)

`X.Instance` / `X.HasInstance` for any identifier X, and `X.Current` where X declares a static Current. Types declaring such static accessors: `CultureThemeService` (Instance), `RunManager` (HasInstance, Instance)

| Type | Sites | Files | By role | Accessors |
|---|---|---|---|---|
| `RunManager` | 29 | 8 | {'runtime': 29} | {'HasInstance': 13, 'Instance': 16} |
| `CultureThemeService` | 11 | 7 | {'runtime': 11} | {'Instance': 11} |

FindObject*-style searches in runtime code (3), by type: {'Canvas': 1, 'DayOrchestrator': 1, 'HoverHighlighter': 1}

| Where | Call | Type |
|---|---|---|
| Scripts/Office/OfficeHallCrowdPalette.cs:37 | FindAnyObjectByType | `DayOrchestrator` |
| Scripts/UI/InteractionFeedbackBootstrap.cs:18 | FindAnyObjectByType | `HoverHighlighter` |
| Scripts/UI/InvestigationUIController.cs:910 | FindFirstObjectByType | `Canvas` |
