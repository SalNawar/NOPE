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
| **1 … 15** | Jump to the start of that day's shift (the briefing). Forward: today closes as played and has its night; every day between is played with every traveller decided correctly, each night on the default care, so money, pet and story are a played run's. Back: the run replays from day 1 with its seed and pet the same way (asks first). The same day: restart (asks first). A run that has ended can only jump back or restart. |
| **Restart shift** | Today again from its morning (the morning snapshot): today's decisions, pay, citations and cheat changes are undone. Asks first. |
| **End shift** | Closing time now: nobody else is called; the traveller at the desk, if any, is still finished; then the shift report. With a citation slip open it waits for Acknowledge; a run that has ended goes to its ending. |
| **Skip day** | Closes today as played, Home's night on the default care (rent, bills, the pet's night), Sleep, the next morning. From Home (or over the shift report) it starts from the end-of-shift save: every cheat change made since is kept (replayed on it), but what you did at Home (bills paid, purchases, the care chosen) is replaced by the default care. To play Home's night your way, Sleep at Home instead. |
| **Decide correctly** | The traveller at the desk gets the right verdict (denied with the evidence a denial needs). Nobody at the desk: the AVAILABLE sign turns on and the next one is decided as they arrive. This is "skip the traveller". |
| **Reveal faults** | Says the verdict the traveller should get, the fault, the rule and the values a citation would name, and marks their papers' boxes that show it in red (papers handed over). |
| **Auto-decide: off / ON** | ON: every traveller is decided correctly as they arrive and the sign stays on, so a day plays itself. |
| **Money +100 / +1000** | Adds to the wallet. |
| **Stab. 0 / 70 / 100** | Sets the timeline stability (0 fires the clerk at the next decision: `GameConfigSO.firedAtStability` is 60; 70 is just above it). |
| **Unlock everything: off / ON** | Every feature the ramp introduces counts as introduced today: every paper's field, rule, tool, app, the scanner, the books. A running shift restarts with it. Asks first when it restarts a running shift or goes back a day. |
| **Give all items** | Every Orders and House item is owned (as if delivered: its effect from today, installed); a pending order for an item is cancelled and refunded. A running shift restarts with them. Asks first when it restarts a running shift or goes back a day. |
| **Skip tutorial / Replay tutorial** | Ends the desk tutorial, or starts it from step 1. |
| **Time: Clock / Morning / Dusk / Night** | The hall's light at 09:00, the day cycle's dusk (18:30 until LIGHT's cycle lands) or 22:00 instead of the shift clock's hour (the clock itself runs on), until the next morning. |
| **First traveller of the shift** (a name or `D7 …`) | That premade (famous traveller or story character) or that day's story beat stands first: on its own day (a premade today when today's plan lists them, else on the first day a plan does), so the run jumps there, or today's shift restarts. Asks first when it restarts a running shift or goes back a day. |
| **More (other tracks)** | Cheats other systems add. The game feel's (`FeelCheats`): **Feel: intensity** steps the Motion intensity through 100, 50 and 0 % (`MotionPreference.Intensity`); **Feel: stamp / citation / famous / breach** fire that moment's hit-stop, camera impulse and sound (`FeelDirector.Punch`) without touching the run. The Translation Lens's (`TranslationLensCheats`): **Lens: Word / Sentence / Object** owns the lens up to that level (`TranslationLens.Grant`; the lens reads it at the next pointer move, even in week 1), **Lens: remove** takes the owned levels away (from day 8 the Bureau's Word lens still reads), **Language: lock now / unlock / follow the ramp** makes today the culture day or not (the papers and the newspaper read the world's language unless the player chose Always English; Settings never locks: `CultureThemeService.LockOverride`) and re-applies the labels; shown on the CHEATS badge; it lasts until the next morning (`CultureThemeService.ResetForNewDay`), and a new or continued run follows the ramp again. "Unlock everything" introduces the lens too, so the culture day comes and the Bureau issues the Word lens. The desk machine's (`LawBreakerCheats`): **Law-breaker next** gives the next generated traveller someone else's photo (a false identity: `Law.Breaks`) and starts today's shift over, so DETAIN has a law-breaker to take. |

The last cheat's line shows under the day, money and stability line.

## Saves

Every cheat marks the run as cheated (`WorldState.cheated`). The mark is saved with the run (a jump or a restart saves at once; any other cheat at the next save) and stays for the run; a new run starts clean. A cheated run shows **CHEATS ON** and every sticky switch (Unlock everything, forced strandings, a forced personality, the language lock, anything waiting for the next generation) at the bottom left. A jump keeps what waits for the next generation for the day it lands on. Every morning clears the day's switches (the forced hour, Auto-decide, the forced leader, the hall overrides, the language lock). While the menu is open the game takes no input. Every morning is saved and kept as the restart's snapshot, so Continue works as ever.

## Adding a cheat from another system

```csharp
[RuntimeInitializeOnLoadMethod]
private static void RegisterCheats() =>
    DevCheats.Register("Translation: every tongue", run => { /* change run.World or a dev switch */ });
```

The button appears under "More (other tracks)" in the Play tab; running it marks the run cheated like any other cheat. A cheat that starts a running shift over registers with `DevCheats.Register(label, run, restartsShift: true)`, so the menu asks first. Keep the code under the same symbols as the menu if it must not ship (`UNITY_EDITOR || DEVELOPMENT_BUILD || DEMO_CHEATS`).
