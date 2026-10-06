# Cheats: testing the game fast

*2026-10-06 · Saleh: "I want cheats to test easy". The behaviour contract is `docs/FEATURES.md` ("The cheat menu").*

## Where it works

- **The editor** (play mode) and **development builds**: always.
- **The Windows demo** (`Time Sorter > Build Windows Demo`, `DemoBuild.BuildWindowsDemo`): while `DemoBuild.DemoCheats` is `true` (it is, for the demo). The build then defines `DEMO_CHEATS` and its `_BuildReport.txt` says `Cheats: on`. Set `DemoCheats` to `false` in `Assets/Editor/DemoBuild.cs` for a build players should get without cheats.
- Any other release build carries none of it.

## Keys

| Key | What it does |
|---|---|
| **F9** or **~** (backquote) | Opens or closes the cheat menu (top left). |

The menu is clicked with the mouse. Its tabs: **Play** (the cheats below), **More cheats** (the older dev cheats: force a leader, flags, costume errors, personalities, strandings, upgrades one by one), **Timeline Inspector**.

## The Play tab

| Button | Effect |
|---|---|
| **1 … 15** | Jump to the start of that day's shift (the briefing). Forward: each night in between runs as Sleep's does, no shift played. Back: the run starts over from its seed and pet, then moves forward. The same day: restart. |
| **Restart shift** | Today's shift again from the briefing, the world as it stands (an ending reached is lifted). |
| **End shift** | Closing time now: nobody else is called; the traveller at the desk, if any, is still finished; then the shift report. |
| **Skip day (sleep)** | Sleeps through the day (endings, the night, the next morning). |
| **Decide correctly** | The traveller at the desk gets the right verdict (denied with the evidence a denial needs). Nobody at the desk: the AVAILABLE sign turns on and the next one is decided as they arrive. This is "skip the traveller". |
| **Reveal faults** | Says the verdict the traveller should get, the fault, the rule and the values a citation would name, and marks their papers' boxes that show it in red (papers handed over). |
| **Auto-decide: off / ON** | ON: every traveller is decided correctly as they arrive and the sign stays on, so a day plays itself. |
| **Money +100 / +1000** | Adds to the wallet. |
| **Stab. 0 / 70 / 100** | Sets the timeline stability (0 fires the clerk at the next decision: `GameConfigSO.firedAtStability` is 60; 70 is just above it). |
| **Unlock everything: off / ON** | Every feature the ramp introduces counts as introduced today: every paper's field, rule, tool, app, the scanner, the books. A running shift restarts with it. |
| **Give all items** | Every Orders and House item is owned (as if delivered: its effect from today, installed). A running shift restarts with them. |
| **Skip tutorial / Replay tutorial** | Ends the desk tutorial, or starts it from step 1. |
| **Time: Clock / Morning / Dusk / Night** | The hall's light at 09:00, 18:30 or 22:00 instead of the shift clock's hour (the clock itself runs on). |
| **First traveller of the shift** (a name or `D7 …`) | That premade (famous traveller or story character) or that day's story beat stands first: on its own day (a premade today when today's plan lists them, else on the first day a plan does), so the run jumps there, or today's shift restarts. |
| **More (other tracks)** | Cheats other systems add. The Translation Lens's (`TranslationLensCheats`): **Lens: Word / Sentence / Object** owns the lens up to that level (`TranslationLens.Grant`; the lens reads it at the next pointer move, even in week 1), **Lens: remove** takes the owned levels away (from day 8 the Bureau's Word lens still reads), **Language: lock now / unlock / follow the ramp** locks or frees Settings' language for the session (`CultureThemeService.LockOverride`) and re-applies the labels. "Unlock everything" introduces the lens too, so it locks the language and issues the Word lens. |

The last cheat's line shows under the day, money and stability line.

## Saves

Every cheat marks the run as cheated (`WorldState.cheated`). The mark is saved with the run (a jump or a restart saves at once; any other cheat at the next save) and stays for the run; a new run starts clean. A cheated run shows a small grey **CHEATS ON** at the screen's bottom left, the menu open or closed. Jumps and restarts leave a normal day-start save, so Continue works as ever.

## Adding a cheat from another system

```csharp
[RuntimeInitializeOnLoadMethod]
private static void RegisterCheats() =>
    DevCheats.Register("Translation: every tongue", run => { /* change run.World or a dev switch */ });
```

The button appears under "More (other tracks)" in the Play tab; running it marks the run cheated like any other cheat. Keep the code under the same symbols as the menu if it must not ship (`UNITY_EDITOR || DEVELOPMENT_BUILD || DEMO_CHEATS`).
