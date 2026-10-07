# Night shifts: the hours grow (2026-10-07)

Saleh, 2026-10-07 morning: "We should have night." Of the ways offered he chose **the hours grow**: each day has its own
desk hours. Week one keeps 09:00-17:00; then the Bureau extends the hours as the debt crisis deepens, and the last days
are night shifts. The clock stays honest: the clock, the hall's light, the city's time and the crowds all follow the real
shift time, so the darkening hall tells the story. Before this, the night hall and the night city that the art side
painted never showed in normal play (every shift ended at 17:00, half an hour after the hall's sunset).

## Data

- `world_source.json` `days[].shiftStart` / `shiftEnd`: "HH:MM" on the 24-hour clock, the closing up to "24:00"
  (midnight, shown "00:00"). Both or neither; a day without them keeps GameConfigSO's standard day
  (`shiftStartHour` 9, `shiftEndHour` 17). Sheet `days`, two omitted-when-blank text columns after `queue`
  (`ContentSheetMap`).
- Generate World writes them to `DayPlanSO.shiftStartMinute` / `shiftEndMinute` (-1: the standard day).
- Generate World and the validator check them alike (`ShiftHours.TryParse`, `ShiftHours.Problems`): readable times,
  both or neither, opening before closing, closing at 24:00 at the latest, and a length within
  `GameConfigSO.shiftMinHours` (4) to `shiftMaxHours` (12), read from the run's `Resources/RunConfig`.
- Values shipped:

| Days | Hours | Why |
|---|---|---|
| 1-7 | (standard) 09:00-17:00 | week one, as before |
| 8-11 | 13:00-21:00 | the Drive's second week: the desk closes in the night hall |
| 12-15 | 16:00-24:00 | night shifts: dusk to midnight |

Every span is 8 hours, so with the shift's real length unchanged (`shiftRealSeconds`, 480 s) the clock runs at day 1's
pace on every day. **Decision:** the real length stays one knob for every day. A day authored with a longer span runs
its clock visibly faster, which reads as overtime; a per-day real-time knob is not added until a day needs it.

## Rules (TimeDesk.Domain, tested by `ShiftHoursTests`)

- `ShiftHours.For(planStart, planEnd, standard)`: the plan's hours, else the standard day (`DayPlanSO.Shift(config)`,
  `GameConfigSO.Standard(config)`, the field defaults without a config).
- The clock (`ShiftClock`, unchanged) runs from the opening minute to the closing minute over the real seconds;
  `ShiftClock.Format` wraps 1440 to "00:00", so the night shift's closing shows "00:00" on the desk's clock and the
  PC's tray. The calendar's date does not turn at midnight: the shift is the day's.
- The hour each visual system receives is the clock's real minute: `HallLightingRig` reads `IShiftProgress.MinuteOfDay`
  (`HallDayCycle.Hour`, 1440 wraps to 0, full night), its `HallBakedLighting` four states and the city view's paintings
  read `HallBakedCycle.Weights` of that hour (night from 19:30). Nothing there changed: the hours did.
- The art's evening curves that read shift progress (the crowds' palette `OfficeHallCrowdPalette`, and the hall's
  evening without its lights in `AnimeHallShiftLink`) now read `IShiftProgress.StandardProgress01`, the clock's hour on
  the standard day (`ShiftHours.StandardProgress`): identical on days 1-7, and a late shift opens in the evening instead
  of in the morning palette. `IShiftProgress.Progress01` (progress through today's own shift) had no other reader and is
  replaced (docs/SCENE_CONTRACT_GAMEPLAY.md).
- `ShiftHours.Announces(yesterday, today)`: the briefing names the new hours when they differ from yesterday's (days 8
  and 12; never day 1).
- `ShiftHours.Lateness(close, standard)`: 0 at the standard closing or earlier, 1 at midnight, linear between.

## What the player sees

- **Briefing (days 8 and 12):** the bulletin gains "New hours from today: the desk opens at 13:00 and closes at 21:00."
  (UI string `briefing.newHours`; also kept in the News site's back issue). Generated from the plan's hours, so the
  text never disagrees with the clock.
- **The paper:** two front-page story rules lead the morning paper: `hours_extended` (DayAtLeast 7: fires the night of
  day 7, so day 8's paper) "BUREAU EXTENDS HOURS AS DEBT QUEUES GROW: ..." and `night_shifts` (DayAtLeast 11, day 12's
  paper) "NIGHT SHIFTS BEGIN AT THE GATES: ...". They carry no pulls and no stability, and name no times (the times are
  the plan's). They sit right after `drive_begins`, so they lead any famous homecoming filed the same night.
- **Shift ledger:** opens with "Desk hours: 16:00 to 00:00" (`results.hours`), the day's scheduled hours (never the
  real closing minute, which depends on play speed and would make the golden transcript nondeterministic).
- **Home:** the flat's painting (`ArtBackground`, already a night scene) is tinted from white to
  `GameConfigSO.homeDeepNightTint` by the lateness: days 1-7 untouched, days 8-11 four sevenths, days 12-15 deep night.
  `Build Home UI` wires the existing art image into `HomeUIController.backdrop` (found by name, never created).
- **Hall and city:** day 1 opens in morning light and closes at dusk; day 8 opens at noon light, goes through the
  evening and closes in the night hall with the city's night painting; day 12 opens in the evening and is night from
  19:30 to midnight.
- **Cheat menu:** the hall's forced hour (Morning, Dusk, Night) still overrides the clock's; Jump to day lands on the
  day's hours.

## Not changed

- The UI builders' styling (Tracks U-OFFICE and T): the only builder edit is `HomeSceneBuilder.Backdrop`, its own method.
- The shift's real length, the queue sizes, the travellers and every draw: `cases.txt` does not move.
- The hall lighting knobs (sunrise 07:00, sunset 16:30).
