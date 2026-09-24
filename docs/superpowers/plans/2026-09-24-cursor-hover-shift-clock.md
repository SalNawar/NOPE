# Cursor, Hover Highlight and Shift Clock Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A game cursor with a hover outline on everything clickable, and a Papers, Please-style real-time shift clock that ends the day at closing time.

**Architecture:** Rules and pure maths live in pure assemblies with EditMode tests: `ShiftClock`, `ShiftFlow`, `NameRoster` and `ReadyGate.Disarm` in `TimeDesk.Domain`; `OutlineMask` in a new `TimeDesk.Visuals`. Thin MonoBehaviours host them: `ShiftClockDriver`, `ShiftClockReadouts` and `HoverHighlighter`. `GameManager` and `DayOrchestrator` get small, explicit hooks. `OfficeSceneUIBuilder` wires everything and stays authoritative.

**Tech Stack:** Unity 6000.4.11f1, URP 2D, uGUI + TextMeshPro, Input System 1.19, Cinemachine, NUnit (EditMode).

**Spec:** `docs/superpowers/specs/2026-09-24-cursor-hover-shift-clock-design.md`

---

## Working environment (read first)

- Work ONLY in the worktree `E:\unity\NOPE-feat-clock` (branch `feat/cursor-hover-shift-clock`). `E:\unity\NOPE` is the shared main checkout, and another agent (Codex) is editing files there right now. Never write to it.
- Unity is not open on the worktree. Verify with the out-of-Unity toolchain in the session scratchpad (`$S` = `C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad`):
  - Compile everything: `python "$S/compile_check.py" 'E:\unity\NOPE-feat-clock'` → expect `exit 0` for `Assembly-CSharp-Editor.csproj` and `TimeDeskEditMode.csproj`.
  - Run tests: `dotnet "$S/runner/bin/Debug/net10.0/runner.dll" "$S/cc/Temp/Bin/Debug" [NameFilter]` → expect `passed N, failed 0`.
- Every new file under `Assets/` needs a `.meta`. Generate one with `python "$S/make_meta.py" <path> [<path>...]` (Task 0).
- Code style (house rules): sealed classes, an XML doc comment on every member, null-safe optional wiring, tunable numbers in ScriptableObjects, no static mutable state in rule code, explicit Unity null checks (no `??` on `GetComponent`).

## File map

| File | Responsibility |
|---|---|
| `Assets/Scripts/Domain/ShiftClock.cs` (new) | Pure shift clock: minutes of day, tick, pause, stop, close, format, hand angles |
| `Assets/Scripts/Domain/ShiftFlow.cs` (new) | Closing decision table (`ClosingAction`) |
| `Assets/Scripts/Domain/NameRoster.cs` (new) | Unique visitor names within a day |
| `Assets/Scripts/Domain/ReadyGate.cs` (modify) | Add `Disarm()` |
| `Assets/Scripts/Visuals/TimeDesk.Visuals.asmdef` (new) | Pure presentation-maths assembly |
| `Assets/Scripts/Visuals/OutlineMask.cs` (new) | Ring-from-alpha (chamfer distance transform) |
| `Assets/Scripts/Shift/ShiftClockDriver.cs` (new) | MonoBehaviour host: configure, tick, re-raise `Closed` |
| `Assets/Scripts/Office/ShiftClockReadouts.cs` (new) | Tray `HH:MM` and wall-clock hands |
| `Assets/Scripts/UI/InteractionFeedbackSO.cs` (new) | Cursor and outline settings |
| `Assets/Scripts/UI/HoverUIOutline.cs` (new) | Dedicated `Outline` subclass for UI hover |
| `Assets/Scripts/UI/SpriteOutlineBuilder.cs` (new) | Sprite → outline Sprite (GPU readback + `OutlineMask`) |
| `Assets/Scripts/UI/HoverHighlighter.cs` (new) | Per-frame hover target, highlight, cursor |
| `Assets/Scripts/Core/GameConfigSO.cs` (modify) | "Shift clock" knobs |
| `Assets/Scripts/DayOrchestrator.cs` (modify) | `CloseAfterCurrentSlot()` / `CloseNow()` |
| `Assets/Scripts/GameManager.cs` (modify) | Start and stop the clock, closing, pause during the citation slip |
| `Assets/Scripts/CaseFactory.cs` (modify) | Use `NameRoster` |
| `Assets/Editor/OfficeSceneUIBuilder.cs` (modify) | Build and wire the settings, highlighter, driver, tray clock and wall clock |
| `Assets/Tests/EditMode/*Tests.cs` (new or modify) | Decision-table tests |
| `Assets/Tests/EditMode/TimeDeskEditMode.asmdef` (modify) | Reference `TimeDesk.Visuals` |
| `Assets/Data/Dayplan/*.asset`, `Assets/Data/Investigation/DayPlan_Inv_*.asset` (modify) | `visitorsCount: 12` |
| `docs/FEATURES.md` (modify) | Inventory |

---

### Task 0: Meta-file helper

**Files:**
- Create: `$S/make_meta.py`

- [ ] **Step 1: Write the helper**

```python
"""Writes Unity .meta files (fresh GUIDs) for new assets that lack one.
Usage: python make_meta.py <path> [<path> ...]  (files or folders)"""
import sys, uuid
from pathlib import Path

TEMPLATES = {
    ".cs": "MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
    ".asmdef": "AssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
}
FOLDER = "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"

for arg in sys.argv[1:]:
    p = Path(arg)
    meta = Path(str(p) + ".meta")
    if meta.exists():
        print("exists", meta); continue
    body = FOLDER if p.is_dir() else TEMPLATES[p.suffix]
    meta.write_text(f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\n{body}", encoding="utf-8", newline="\n")
    print("wrote", meta)
```

---

### Task 1: ShiftClock (pure)

**Files:**
- Create: `Assets/Scripts/Domain/ShiftClock.cs`
- Test: `Assets/Tests/EditMode/ShiftClockTests.cs`

- [ ] **Step 1: Write the failing tests** (`Assets/Tests/EditMode/ShiftClockTests.cs`)

```csharp
using System;
using NUnit.Framework;

public class ShiftClockTests
{
    /// <summary>09:00-17:00 over 480 real seconds = one game minute per real second.</summary>
    private static ShiftClock NineToFive() => new ShiftClock(9 * 60, 17 * 60, 480f);

    [Test]
    public void NewClock_StartsAtOpening_AndIsNotRunning()
    {
        var clock = NineToFive();
        Assert.AreEqual(540f, clock.CurrentMinute);
        Assert.AreEqual(0f, clock.Progress01);
        Assert.IsFalse(clock.IsRunning);
        Assert.IsFalse(clock.IsClosed);
    }

    [Test]
    public void Tick_BeforeStart_DoesNothing()
    {
        var clock = NineToFive();
        clock.Tick(60f);
        Assert.AreEqual(540f, clock.CurrentMinute);
    }

    [Test]
    public void Tick_AfterStart_AdvancesAtShiftRate()
    {
        var clock = NineToFive();
        clock.Start();
        clock.Tick(30f);
        Assert.AreEqual(570f, clock.CurrentMinute, 1e-3f);
        Assert.AreEqual(30f / 480f, clock.Progress01, 1e-4f);
    }

    [Test]
    public void ReachingClosing_ClampsAndFiresClosedOnce()
    {
        var clock = NineToFive();
        int closed = 0;
        clock.Closed += () => closed++;
        clock.Start();
        clock.Tick(1000f);
        clock.Tick(10f);
        Assert.AreEqual(1020f, clock.CurrentMinute);
        Assert.AreEqual(1f, clock.Progress01);
        Assert.IsTrue(clock.IsClosed);
        Assert.IsFalse(clock.IsRunning);
        Assert.AreEqual(1, closed);
    }

    [Test]
    public void Pause_HoldsTime_ResumeContinues()
    {
        var clock = NineToFive();
        clock.Start();
        clock.Pause();
        clock.Tick(30f);
        Assert.AreEqual(540f, clock.CurrentMinute);
        clock.Resume();
        clock.Tick(30f);
        Assert.AreEqual(570f, clock.CurrentMinute, 1e-3f);
    }

    [Test]
    public void NestedPauses_NeedMatchingResumes()
    {
        var clock = NineToFive();
        clock.Start();
        clock.Pause();
        clock.Pause();
        clock.Resume();
        clock.Tick(30f);
        Assert.IsTrue(clock.IsPaused);
        Assert.AreEqual(540f, clock.CurrentMinute);
        clock.Resume();
        clock.Tick(30f);
        Assert.AreEqual(570f, clock.CurrentMinute, 1e-3f);
    }

    [Test]
    public void Resume_WithoutPause_IsHarmless()
    {
        var clock = NineToFive();
        clock.Resume();
        clock.Start();
        clock.Tick(10f);
        Assert.IsFalse(clock.IsPaused);
        Assert.AreEqual(550f, clock.CurrentMinute, 1e-3f);
    }

    [Test]
    public void Stop_HaltsWithoutClosing()
    {
        var clock = NineToFive();
        int closed = 0;
        clock.Closed += () => closed++;
        clock.Start();
        clock.Tick(10f);
        clock.Stop();
        clock.Tick(1000f);
        Assert.AreEqual(550f, clock.CurrentMinute, 1e-3f);
        Assert.IsFalse(clock.IsClosed);
        Assert.AreEqual(0, closed);
    }

    [Test]
    public void Start_AfterClosing_StaysClosed()
    {
        var clock = NineToFive();
        clock.Start();
        clock.Tick(1000f);
        clock.Start();
        Assert.IsTrue(clock.IsClosed);
        Assert.IsFalse(clock.IsRunning);
    }

    [TestCase(0f)]
    [TestCase(-5f)]
    public void NonPositiveTick_IsIgnored(float seconds)
    {
        var clock = NineToFive();
        clock.Start();
        clock.Tick(seconds);
        Assert.AreEqual(540f, clock.CurrentMinute);
    }

    [TestCase(540f, "09:00")]
    [TestCase(545.9f, "09:05")]
    [TestCase(1020f, "17:00")]
    [TestCase(0f, "00:00")]
    [TestCase(1439.5f, "23:59")]
    public void Format_IsZeroPaddedTwentyFourHour(float minute, string expected)
    {
        Assert.AreEqual(expected, ShiftClock.Format(minute));
    }

    [TestCase(180f, 90f, 0f)]      // 03:00
    [TestCase(570f, 285f, 180f)]   // 09:30
    [TestCase(945f, 112.5f, 270f)] // 15:45
    [TestCase(720f, 0f, 0f)]       // 12:00
    public void HandAngles_AreClockwiseDegreesFromTwelve(float minute, float hourDeg, float minuteDeg)
    {
        (float hour, float min) = ShiftClock.HandAngles(minute);
        Assert.AreEqual(hourDeg, hour, 1e-3f);
        Assert.AreEqual(minuteDeg, min, 1e-3f);
    }

    [Test]
    public void Constructor_RejectsInvalidShifts()
    {
        Assert.Throws<ArgumentException>(() => new ShiftClock(600, 600, 60f));
        Assert.Throws<ArgumentException>(() => new ShiftClock(600, 540, 60f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShiftClock(540, 1020, 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShiftClock(-1, 1020, 60f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShiftClock(540, 1441, 60f));
    }
}
```

- [ ] **Step 2: Run the compile check. Expect it to FAIL** with `CS0246: The type or namespace name 'ShiftClock' could not be found`.

- [ ] **Step 3: Implement** (`Assets/Scripts/Domain/ShiftClock.cs`)

```csharp
using System;

/// <summary>
/// Papers, Please-style shift clock: in-game minutes of the day advance in
/// real time from opening to closing. Pure (no UnityEngine) so the rules run
/// headless; ShiftClockDriver ticks it from Update.
/// </summary>
public sealed class ShiftClock
{
    /// <summary>Minutes in a day; valid minute-of-day values are 0..1440.</summary>
    public const int MinutesPerDay = 24 * 60;

    /// <summary>Minute of day the booth opens (e.g. 540 = 09:00).</summary>
    public int StartMinute { get; }

    /// <summary>Minute of day the booth closes (e.g. 1020 = 17:00).</summary>
    public int EndMinute { get; }

    /// <summary>Real seconds the whole shift lasts.</summary>
    public float RealSecondsPerShift { get; }

    /// <summary>Current in-game minute of day, StartMinute..EndMinute.</summary>
    public float CurrentMinute { get; private set; }

    /// <summary>True between Start() and Stop().</summary>
    public bool IsStarted { get; private set; }

    /// <summary>True while at least one Pause() has no matching Resume().</summary>
    public bool IsPaused => _pauseCount > 0;

    /// <summary>True once the clock reached closing time (permanent for this clock).</summary>
    public bool IsClosed { get; private set; }

    /// <summary>True when Tick advances time.</summary>
    public bool IsRunning => IsStarted && !IsPaused && !IsClosed;

    /// <summary>0 at opening, 1 at closing (drives time-of-day visuals).</summary>
    public float Progress01 => (CurrentMinute - StartMinute) / (EndMinute - StartMinute);

    /// <summary>Raised exactly once, when the clock reaches closing time.</summary>
    public event Action Closed;

    /// <summary>Outstanding Pause() calls.</summary>
    private int _pauseCount;

    /// <summary>Creates a stopped clock at opening time.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A minute is outside the day, or the duration is not positive.</exception>
    /// <exception cref="ArgumentException">Closing is not after opening.</exception>
    public ShiftClock(int startMinute, int endMinute, float realSecondsPerShift)
    {
        if (startMinute < 0 || startMinute >= MinutesPerDay)
            throw new ArgumentOutOfRangeException(nameof(startMinute), startMinute, "Opening must be a minute of the day.");
        if (endMinute < 1 || endMinute > MinutesPerDay)
            throw new ArgumentOutOfRangeException(nameof(endMinute), endMinute, "Closing must be a minute of the day.");
        if (endMinute <= startMinute)
            throw new ArgumentException("Closing must be after opening.", nameof(endMinute));
        if (!(realSecondsPerShift > 0f))
            throw new ArgumentOutOfRangeException(nameof(realSecondsPerShift), realSecondsPerShift, "The shift must last some real time.");

        StartMinute = startMinute;
        EndMinute = endMinute;
        RealSecondsPerShift = realSecondsPerShift;
        CurrentMinute = startMinute;
    }

    /// <summary>Starts (or restarts after Stop) the clock. No effect once closed.</summary>
    public void Start()
    {
        if (!IsClosed)
            IsStarted = true;
    }

    /// <summary>Halts the clock without closing (e.g. the queue ran out early).</summary>
    public void Stop() => IsStarted = false;

    /// <summary>Holds time until a matching Resume(). Calls nest.</summary>
    public void Pause() => _pauseCount++;

    /// <summary>Releases one Pause(). Extra calls are ignored.</summary>
    public void Resume()
    {
        if (_pauseCount > 0)
            _pauseCount--;
    }

    /// <summary>Advances by real seconds while running; clamps at closing and raises Closed once.</summary>
    public void Tick(float realSeconds)
    {
        if (!IsRunning || !(realSeconds > 0f))
            return;

        float minutesPerSecond = (EndMinute - StartMinute) / RealSecondsPerShift;
        CurrentMinute = Math.Min(EndMinute, CurrentMinute + realSeconds * minutesPerSecond);

        if (CurrentMinute >= EndMinute)
        {
            IsClosed = true;
            Closed?.Invoke();
        }
    }

    /// <summary>24-hour "HH:MM" for a minute of day (fractions round down).</summary>
    public static string Format(float minuteOfDay)
    {
        int whole = (int)Math.Floor(minuteOfDay);
        whole = ((whole % MinutesPerDay) + MinutesPerDay) % MinutesPerDay;
        return $"{whole / 60:00}:{whole % 60:00}";
    }

    /// <summary>Clock-hand angles in degrees, clockwise from 12 o'clock.</summary>
    public static (float hourDegrees, float minuteDegrees) HandAngles(float minuteOfDay)
    {
        const float minutesPerDial = 12f * 60f;
        float m = ((minuteOfDay % MinutesPerDay) + MinutesPerDay) % MinutesPerDay;
        float minuteDegrees = (m % 60f) / 60f * 360f;
        float hourDegrees = (m % minutesPerDial) / minutesPerDial * 360f;
        return (hourDegrees, minuteDegrees);
    }
}
```

- [ ] **Step 4: Generate metas, compile, run the tests.** Run `python "$S/make_meta.py" Assets/Scripts/Domain/ShiftClock.cs Assets/Tests/EditMode/ShiftClockTests.cs`, then the compile check, then the runner with filter `ShiftClock`. Expect `passed 21, failed 0` (13 test methods, with the TestCases expanded).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Domain/ShiftClock.cs* Assets/Tests/EditMode/ShiftClockTests.cs*
git commit -m "feat(domain): ShiftClock — real-time shift clock with closing, pause, format and hand angles"
```

---

### Task 2: ShiftFlow + ReadyGate.Disarm (pure)

**Files:**
- Create: `Assets/Scripts/Domain/ShiftFlow.cs`, `Assets/Tests/EditMode/ShiftFlowTests.cs`
- Modify: `Assets/Scripts/Domain/ReadyGate.cs`, `Assets/Tests/EditMode/ReadyGateTests.cs`

- [ ] **Step 1: Failing tests.** `Assets/Tests/EditMode/ShiftFlowTests.cs`:

```csharp
using NUnit.Framework;

public class ShiftFlowTests
{
    [Test]
    public void Closing_WithTravellerAtDesk_FinishesCurrent()
    {
        Assert.AreEqual(ClosingAction.FinishCurrent, ShiftFlow.OnClosing(travellerAtDesk: true));
    }

    [Test]
    public void Closing_WithNobodyAtDesk_ClosesNow()
    {
        Assert.AreEqual(ClosingAction.CloseNow, ShiftFlow.OnClosing(travellerAtDesk: false));
    }
}
```

Append to `ReadyGateTests` (inside the class):

```csharp
    [Test]
    public void Disarm_CancelsTheWait_WithoutFiring()
    {
        var gate = new ReadyGate();
        int released = 0;
        gate.Released += () => released++;

        gate.Arm();
        gate.Disarm();
        gate.Release();

        Assert.IsFalse(gate.IsArmed);
        Assert.AreEqual(0, released);
    }
```

- [ ] **Step 2: Compile. Expect FAIL** (`ShiftFlow`, `ClosingAction` and `Disarm` are not defined).

- [ ] **Step 3: Implement.** `Assets/Scripts/Domain/ShiftFlow.cs`:

```csharp
/// <summary>What the booth does when the shift clock reaches closing time.</summary>
public enum ClosingAction
{
    /// <summary>A traveller is being processed: let the player finish, then end the day.</summary>
    FinishCurrent,

    /// <summary>Nobody at the desk: end the day now (a traveller behind READY is never called).</summary>
    CloseNow,
}

/// <summary>Shift closing rules (decision table covered by ShiftFlowTests).</summary>
public static class ShiftFlow
{
    /// <summary>Chooses the closing action from whether a traveller is at the desk.</summary>
    public static ClosingAction OnClosing(bool travellerAtDesk) =>
        travellerAtDesk ? ClosingAction.FinishCurrent : ClosingAction.CloseNow;
}
```

In `Assets/Scripts/Domain/ReadyGate.cs`, add after `Release()`:

```csharp

    /// <summary>Cancels a pending wait without firing <see cref="Released"/> (e.g. the booth closed).</summary>
    public void Disarm() => IsArmed = false;
```

- [ ] **Step 4:** Run `make_meta` on the two new files, compile, then run the runner with filters `ShiftFlow` and `ReadyGate`. Expect all to pass (2 + 3).

- [ ] **Step 5: Commit** with the message `feat(domain): ShiftFlow closing decision table + ReadyGate.Disarm`.

---

### Task 3: NameRoster (pure) + CaseFactory

**Files:**
- Create: `Assets/Scripts/Domain/NameRoster.cs`, `Assets/Tests/EditMode/NameRosterTests.cs`
- Modify: `Assets/Scripts/CaseFactory.cs` (field, `GenerateDayCases`, `ResolveGivenName`)

- [ ] **Step 1: Failing tests** (`NameRosterTests.cs`)

```csharp
using System.Collections.Generic;
using NUnit.Framework;

public class NameRosterTests
{
    private static readonly string[] Pool = { "Marcus", "Lucia", "Gaius" };

    /// <summary>Deterministic "random" index: always the first candidate.</summary>
    private static int First(int count) => 0;

    [Test]
    public void Take_GivesEveryPoolName_BeforeAnyRepeat()
    {
        var roster = new NameRoster();
        var names = new HashSet<string>();
        for (int i = 0; i < Pool.Length; i++)
            names.Add(roster.Take(Pool, First));
        CollectionAssert.AreEquivalent(Pool, names);
    }

    [Test]
    public void Take_WhenPoolExhausted_AddsRomanSuffixes()
    {
        var roster = new NameRoster();
        string[] pool = { "Marcus" };
        Assert.AreEqual("Marcus", roster.Take(pool, First));
        Assert.AreEqual("Marcus II", roster.Take(pool, First));
        Assert.AreEqual("Marcus III", roster.Take(pool, First));
    }

    [Test]
    public void Reserve_BlocksThatName_CaseAndSpaceInsensitive()
    {
        var roster = new NameRoster();
        Assert.IsTrue(roster.Reserve("Marcus"));
        Assert.IsFalse(roster.Reserve("marcus "));
        Assert.AreEqual("Lucia", roster.Take(new[] { "Marcus", "Lucia" }, First));
    }

    [Test]
    public void Take_TrimsNames_SkipsBlanks_AndTreatsCaseVariantsAsOne()
    {
        var roster = new NameRoster();
        string[] pool = { " Marcus ", "", null, "MARCUS" };
        Assert.AreEqual("Marcus", roster.Take(pool, First));
        Assert.AreEqual("Marcus II", roster.Take(pool, First));
    }

    [Test]
    public void Take_WithNoUsableNames_ReturnsNull()
    {
        var roster = new NameRoster();
        Assert.IsNull(roster.Take(null, First));
        Assert.IsNull(roster.Take(new string[0], First));
        Assert.IsNull(roster.Take(new[] { " ", null }, First));
    }

    [Test]
    public void Take_ClampsOutOfRangeRandomIndex()
    {
        var roster = new NameRoster();
        Assert.AreEqual("Gaius", roster.Take(Pool, n => 99));
        Assert.AreEqual("Marcus", roster.Take(Pool, n => -3));
    }

    [TestCase(2, "II")]
    [TestCase(4, "IV")]
    [TestCase(9, "IX")]
    [TestCase(14, "XIV")]
    [TestCase(40, "XL")]
    public void Roman_FormatsNumerals(int number, string expected)
    {
        Assert.AreEqual(expected, NameRoster.Roman(number));
    }
}
```

- [ ] **Step 2: Compile. Expect FAIL** (`NameRoster` is not defined).

- [ ] **Step 3: Implement** (`Assets/Scripts/Domain/NameRoster.cs`)

```csharp
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Hands out visitor names that are unique within one day, so Citizen Records
/// (first-match lookup) always finds the right person. Prefers unused pool
/// names; once a pool is exhausted, adds a numeral suffix ("Marcus II").
/// Make a new roster for every generated day.
/// </summary>
public sealed class NameRoster
{
    /// <summary>Names already given out today (trimmed, case-insensitive).</summary>
    private readonly HashSet<string> _used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Marks a fixed name (e.g. a legendary visitor) as taken today.</summary>
    /// <returns>False when the name is blank or already taken.</returns>
    public bool Reserve(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return _used.Add(name.Trim());
    }

    /// <summary>
    /// Takes a name nobody has today: a random unused pool name, or, once every
    /// pool name is taken, a pool name with the lowest free numeral suffix.
    /// Returns null when the pool holds no usable names.
    /// </summary>
    /// <param name="randomIndex">Returns an index in [0, count); out-of-range values are clamped.</param>
    public string Take(IReadOnlyList<string> pool, Func<int, int> randomIndex)
    {
        if (pool == null || randomIndex == null)
            return null;

        var bases = new List<string>();
        var unused = new List<string>();
        foreach (string raw in pool)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            string name = raw.Trim();
            bases.Add(name);
            if (!_used.Contains(name))
                unused.Add(name);
        }

        if (bases.Count == 0)
            return null;

        if (unused.Count > 0)
        {
            string pick = unused[Clamp(randomIndex(unused.Count), unused.Count)];
            _used.Add(pick);
            return pick;
        }

        string baseName = bases[Clamp(randomIndex(bases.Count), bases.Count)];
        for (int n = 2; ; n++)
        {
            string candidate = $"{baseName} {Roman(n)}";
            if (_used.Add(candidate))
                return candidate;
        }
    }

    /// <summary>Roman numeral for 1..3999 (name suffixes).</summary>
    public static string Roman(int number)
    {
        if (number < 1 || number > 3999)
            throw new ArgumentOutOfRangeException(nameof(number), number, "Roman numerals cover 1..3999.");

        int[] values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
        string[] numerals = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
        var sb = new StringBuilder();
        for (int i = 0; i < values.Length; i++)
        {
            while (number >= values[i])
            {
                sb.Append(numerals[i]);
                number -= values[i];
            }
        }
        return sb.ToString();
    }

    /// <summary>Clamps a random index into [0, count).</summary>
    private static int Clamp(int index, int count) => index < 0 ? 0 : index >= count ? count - 1 : index;
}
```

- [ ] **Step 4: Wire it into CaseFactory** (`Assets/Scripts/CaseFactory.cs`)

After `    private readonly ContentLibrarySO _lib;` add:

```csharp

    /// <summary>Today's visitor names (unique per generated day; see NameRoster).</summary>
    private NameRoster _roster = new NameRoster();
```

In `GenerateDayCases`, directly after `        int total = Mathf.Max(1, plan.VisitorsCount);` add:

```csharp

        // Fresh roster: names are unique within the day (records use first match).
        _roster = new NameRoster();
```

Replace the whole `ResolveGivenName` method (currently `private static string ResolveGivenName(...)`) with:

```csharp
    private string ResolveGivenName(LegendarySO legendary, ArchetypeSO archetype, NationSO nation, int caseIndex1Based)
    {
        if (legendary != null)
        {
            _roster.Reserve(legendary.displayName);
            return legendary.displayName;
        }

        // Prefer a name themed to the visitor's nation/era, then the archetype's.
        string picked = _roster.Take(nation != null ? nation.namePool : null, n => Random.Range(0, n))
                        ?? _roster.Take(archetype != null ? archetype.namePool : null, n => Random.Range(0, n));
        if (picked != null)
            return picked;

        string fallback = $"Subject #{caseIndex1Based}";
        _roster.Reserve(fallback);
        return fallback;
    }
```

(`??` on `string` is fine; the house rule only bans it on Unity objects.)

- [ ] **Step 5:** Run `make_meta`, compile, then the runner with filter `NameRoster` (expect 11 passing) and a full run (expect no failures).
- [ ] **Step 6: Commit** with the message `feat: unique visitor names per day (NameRoster) for the longer queue`.

---

### Task 4: OutlineMask in the new TimeDesk.Visuals assembly

**Files:**
- Create: `Assets/Scripts/Visuals/` (folder), `Assets/Scripts/Visuals/TimeDesk.Visuals.asmdef`, `Assets/Scripts/Visuals/OutlineMask.cs`, `Assets/Tests/EditMode/OutlineMaskTests.cs`
- Modify: `Assets/Tests/EditMode/TimeDeskEditMode.asmdef` (add `"TimeDesk.Visuals"` to `references`, after `"TimeDesk.Domain"`)

- [ ] **Step 1: Create the asmdef**

```json
{
    "name": "TimeDesk.Visuals",
    "rootNamespace": "",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": true
}
```

- [ ] **Step 2: Failing tests** (`OutlineMaskTests.cs`)

```csharp
using System;
using NUnit.Framework;

public class OutlineMaskTests
{
    private static byte At(byte[] grid, int width, int x, int y) => grid[y * width + x];

    [Test]
    public void OutputGrid_AddsMarginOnEverySide()
    {
        OutlineMask.BuildRing(new byte[4 * 3], 4, 3, 3, out int w, out int h);
        Assert.AreEqual(4 + 2 * 4, w);
        Assert.AreEqual(3 + 2 * 4, h);
    }

    [Test]
    public void SinglePixel_OrthogonalNeighboursFull_DiagonalsAntiAliased_FarEmpty()
    {
        byte[] ring = OutlineMask.BuildRing(new byte[] { 255 }, 1, 1, 1, out int w, out int _);
        Assert.AreEqual(5, w);
        Assert.AreEqual(0, At(ring, w, 2, 2), "inside stays empty");
        Assert.AreEqual(255, At(ring, w, 1, 2));
        Assert.AreEqual(255, At(ring, w, 3, 2));
        Assert.AreEqual(255, At(ring, w, 2, 1));
        Assert.AreEqual(255, At(ring, w, 2, 3));
        Assert.AreEqual(149, At(ring, w, 1, 1), "diagonal is anti-aliased");
        Assert.AreEqual(0, At(ring, w, 0, 2));
    }

    [Test]
    public void WideRing_ReachesItsWidth_AndStopsThere()
    {
        byte[] ring = OutlineMask.BuildRing(new byte[] { 255 }, 1, 1, 3, out int w, out int _);
        Assert.AreEqual(255, At(ring, w, 1, 4));
        Assert.AreEqual(0, At(ring, w, 0, 4));
    }

    [Test]
    public void SolidBlock_HasNoRingInside_AndARingAtItsEdge()
    {
        var alpha = new byte[9];
        for (int i = 0; i < alpha.Length; i++)
            alpha[i] = 255;
        byte[] ring = OutlineMask.BuildRing(alpha, 3, 3, 2, out int w, out int _);
        int m = OutlineMask.Margin(2);
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
                Assert.AreEqual(0, At(ring, w, x + m, y + m));
        Assert.AreEqual(255, At(ring, w, m - 1, m + 1));
    }

    [Test]
    public void TransparentOrBelowThreshold_GivesNoRing()
    {
        byte[] ring = OutlineMask.BuildRing(new byte[] { 0, 100, 127, 0 }, 2, 2, 2, out _, out _);
        foreach (byte a in ring)
            Assert.AreEqual(0, a);
    }

    [Test]
    public void ShiftPivot_AddsTheMargin()
    {
        (float x, float y) = OutlineMask.ShiftPivot(10f, 5f, 2);
        Assert.AreEqual(13f, x);
        Assert.AreEqual(8f, y);
    }

    [Test]
    public void BadArguments_Throw()
    {
        Assert.Throws<ArgumentException>(() => OutlineMask.BuildRing(new byte[3], 2, 2, 1, out _, out _));
        Assert.Throws<ArgumentException>(() => OutlineMask.BuildRing(null, 1, 1, 1, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => OutlineMask.BuildRing(new byte[1], 1, 1, 0, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => OutlineMask.BuildRing(new byte[0], 0, 1, 1, out _, out _));
    }
}
```

- [ ] **Step 3: Compile. Expect FAIL** (`OutlineMask` is not defined).

- [ ] **Step 4: Implement** (`Assets/Scripts/Visuals/OutlineMask.cs`)

```csharp
using System;

/// <summary>
/// Pure image maths for the hover outline: from a sprite's alpha channel,
/// builds an anti-aliased ring of a given width hugging the silhouette from
/// outside. Grids are row-major (index = y * width + x, row 0 = bottom, as in
/// Unity's GetPixels32). No UnityEngine, so it is unit-tested headless.
/// </summary>
public static class OutlineMask
{
    /// <summary>Source alpha at or above this counts as solid (inside the silhouette).</summary>
    public const byte SolidThreshold = 128;

    /// <summary>Chamfer step cost for a diagonal neighbour (sqrt 2).</summary>
    private const float DiagonalStep = 1.41421356f;

    /// <summary>Transparent border added on every side of the output: ring width + 1.</summary>
    public static int Margin(int ringWidthPx) => ringWidthPx + 1;

    /// <summary>
    /// Pivot of the ring grid, in its own pixels, that keeps it aligned with a
    /// source pivot given in source pixels.
    /// </summary>
    public static (float x, float y) ShiftPivot(float sourcePivotX, float sourcePivotY, int ringWidthPx)
    {
        int m = Margin(ringWidthPx);
        return (sourcePivotX + m, sourcePivotY + m);
    }

    /// <summary>
    /// Returns ring alpha (0..255) for a width x height alpha grid. The output
    /// grid is (width + 2 * margin) x (height + 2 * margin) so the ring never
    /// clips. Solid source pixels are 0 in the output (the ring is outside only).
    /// </summary>
    public static byte[] BuildRing(byte[] alpha, int width, int height, int ringWidthPx, out int outWidth, out int outHeight)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "The grid must be at least 1x1.");
        if (alpha == null || alpha.Length != width * height)
            throw new ArgumentException("The alpha grid must hold width * height values.", nameof(alpha));
        if (ringWidthPx < 1)
            throw new ArgumentOutOfRangeException(nameof(ringWidthPx), "The ring must be at least 1 px wide.");

        int m = Margin(ringWidthPx);
        int w = width + 2 * m;
        int h = height + 2 * m;
        outWidth = w;
        outHeight = h;

        // Distance from each output pixel to the nearest solid pixel: a
        // two-pass chamfer transform with orthogonal 1 and diagonal sqrt(2) steps.
        var dist = new float[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int sx = x - m;
                int sy = y - m;
                bool solid = sx >= 0 && sy >= 0 && sx < width && sy < height && alpha[sy * width + sx] >= SolidThreshold;
                dist[y * w + x] = solid ? 0f : float.MaxValue;
            }
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                float d = dist[i];
                if (x > 0) d = Math.Min(d, dist[i - 1] + 1f);
                if (y > 0)
                {
                    d = Math.Min(d, dist[i - w] + 1f);
                    if (x > 0) d = Math.Min(d, dist[i - w - 1] + DiagonalStep);
                    if (x < w - 1) d = Math.Min(d, dist[i - w + 1] + DiagonalStep);
                }
                dist[i] = d;
            }
        }

        for (int y = h - 1; y >= 0; y--)
        {
            for (int x = w - 1; x >= 0; x--)
            {
                int i = y * w + x;
                float d = dist[i];
                if (x < w - 1) d = Math.Min(d, dist[i + 1] + 1f);
                if (y < h - 1)
                {
                    d = Math.Min(d, dist[i + w] + 1f);
                    if (x < w - 1) d = Math.Min(d, dist[i + w + 1] + DiagonalStep);
                    if (x > 0) d = Math.Min(d, dist[i + w - 1] + DiagonalStep);
                }
                dist[i] = d;
            }
        }

        // Coverage of the band [edge, edge + ring width]. The silhouette edge sits
        // half a pixel from a solid pixel's centre, so a pixel at centre distance
        // d lies (d - 0.5) from the edge; the extra 0.5 anti-aliases the far side.
        var ring = new byte[w * h];
        for (int i = 0; i < ring.Length; i++)
        {
            float d = dist[i];
            if (d <= 0f)
                continue; // inside the silhouette

            float coverage = ringWidthPx + 1f - d;
            if (coverage <= 0f)
                continue;

            ring[i] = coverage >= 1f ? (byte)255 : (byte)Math.Round(coverage * 255.0);
        }
        return ring;
    }
}
```

- [ ] **Step 5:** Run `make_meta` on the `Assets/Scripts/Visuals` folder, the asmdef, `OutlineMask.cs` and `OutlineMaskTests.cs`. Edit `TimeDeskEditMode.asmdef` so that `"references": ["TimeDesk.Domain", "TimeDesk.Visuals", "UnityEngine.TestRunner", "UnityEditor.TestRunner"]`. Then compile (the script creates the `TimeDesk.Visuals` csproj automatically) and run the runner with filter `OutlineMask`. Expect 7 passing.
- [ ] **Step 6: Commit** with the message `feat(visuals): OutlineMask ring-from-alpha in new pure TimeDesk.Visuals assembly`.

---

### Task 5: Shift clock knobs, driver, orchestrator and GameManager

**Files:**
- Modify: `Assets/Scripts/Core/GameConfigSO.cs`, `Assets/Scripts/DayOrchestrator.cs`, `Assets/Scripts/GameManager.cs`
- Create: `Assets/Scripts/Shift/ShiftClockDriver.cs`

- [ ] **Step 1: GameConfigSO.** Insert directly before `    [Header("Timeline stability")]`:

```csharp
    [Header("Shift clock")]
    /// <summary>Hour the booth opens (0-23). The clock shows this during the briefing and starts at Start Shift.</summary>
    [Range(0, 23)]
    public int shiftStartHour = 9;

    /// <summary>Hour the booth closes (1-24, after the opening hour). No new traveller is called after it.</summary>
    [Range(1, 24)]
    public int shiftEndHour = 17;

    /// <summary>Real seconds a whole shift lasts (the Papers, Please-style time pressure).</summary>
    [Min(10f)]
    public float shiftRealSeconds = 480f;

```

- [ ] **Step 2: ShiftClockDriver** (`Assets/Scripts/Shift/ShiftClockDriver.cs`)

```csharp
using System;
using UnityEngine;

/// <summary>
/// Scene host for the pure <see cref="ShiftClock"/>: builds today's clock from
/// GameConfigSO, ticks it with scaled time, and re-raises Closed. GameManager
/// starts and stops it; readouts (and later, lighting) read <see cref="Clock"/>.
/// </summary>
public sealed class ShiftClockDriver : MonoBehaviour
{
    /// <summary>Today's clock (null until Configure).</summary>
    public ShiftClock Clock { get; private set; }

    /// <summary>Raised once, when the clock reaches closing time.</summary>
    public event Action Closed;

    /// <summary>
    /// Builds a fresh stopped clock for today. A missing or invalid config falls
    /// back to GameConfigSO's default values, with a warning.
    /// </summary>
    public void Configure(GameConfigSO config)
    {
        if (Clock != null)
            Clock.Closed -= RaiseClosed;

        Clock = TryBuild(config);
        if (Clock == null)
        {
            // Defaults live in GameConfigSO's field initializers: read them from a throwaway instance.
            GameConfigSO defaults = ScriptableObject.CreateInstance<GameConfigSO>();
            Clock = TryBuild(defaults);
            Destroy(defaults);
            Debug.LogWarning(config == null
                ? "ShiftClockDriver: no GameConfigSO, so the default shift hours are used."
                : $"ShiftClockDriver: GameConfigSO '{config.name}' has an invalid shift (open {config.shiftStartHour}:00, close {config.shiftEndHour}:00, {config.shiftRealSeconds}s), so the defaults are used. Fix it under 'Shift clock'.", this);
        }

        if (Clock != null)
            Clock.Closed += RaiseClosed;
    }

    /// <summary>Starts today's clock (Start Shift).</summary>
    public void StartShift() => Clock?.Start();

    /// <summary>Freezes the clock without closing (the day ended early).</summary>
    public void StopShift() => Clock?.Stop();

    /// <summary>Holds time (e.g. while a citation slip is shown). Calls nest.</summary>
    public void Pause() => Clock?.Pause();

    /// <summary>Releases one Pause().</summary>
    public void Resume() => Clock?.Resume();

    /// <summary>Advances the clock with scaled time.</summary>
    private void Update() => Clock?.Tick(Time.deltaTime);

    /// <summary>Unhooks from the clock.</summary>
    private void OnDestroy()
    {
        if (Clock != null)
            Clock.Closed -= RaiseClosed;
    }

    /// <summary>Forwards the clock's Closed event.</summary>
    private void RaiseClosed() => Closed?.Invoke();

    /// <summary>Builds a clock from config, or null when the config is missing or invalid.</summary>
    private static ShiftClock TryBuild(GameConfigSO config)
    {
        if (config == null)
            return null;

        try
        {
            return new ShiftClock(config.shiftStartHour * 60, config.shiftEndHour * 60, config.shiftRealSeconds);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
```

- [ ] **Step 3: DayOrchestrator.** After `    private bool _waitingForCaseResolution;` (and its doc comment) add:

```csharp

    /// <summary>True once the booth closed: no further case slot starts.</summary>
    private bool _closeRequested;

    /// <summary>True when the current slot was abandoned before its traveller was called in.</summary>
    private bool _abortCurrentSlot;
```

In `StartDay`, after `        _caseIndex1Based = 1;` add:

```csharp
        _closeRequested = false;
        _abortCurrentSlot = false;
```

After the `MarkCaseResolved()` method add:

```csharp

    /// <summary>
    /// Closing time with a traveller at the desk: finish the current case slot
    /// normally, then end the day instead of starting the next one.
    /// </summary>
    public void CloseAfterCurrentSlot()
    {
        _closeRequested = true;
    }

    /// <summary>
    /// Closing time with nobody at the desk: end the day at once. If the loop is
    /// waiting on a slot whose traveller was never called in, that slot is
    /// abandoned (no slot-ended or after-case events).
    /// </summary>
    public void CloseNow()
    {
        _closeRequested = true;
        if (_waitingForCaseResolution)
        {
            _abortCurrentSlot = true;
            _waitingForCaseResolution = false;
        }
    }
```

In `DayLoop`:
- Change `        while (_caseIndex1Based <= total)` to `        while (_caseIndex1Based <= total && !_closeRequested)`.
- After `            yield return RunScheduledEvents(DayEventTrigger.BeforeCase, _caseIndex1Based);` add:

```csharp

            // The booth may have closed while those events ran.
            if (_closeRequested)
                break;
```

- Change `            _waitingForCaseResolution = true;` to set both flags, in this order, so a synchronous `CloseNow()` inside the event is honoured:

```csharp
            _waitingForCaseResolution = true;
            _abortCurrentSlot = false;
```

- Directly before `            // 4) Notify slot ended.` add:

```csharp
            // Closed before this traveller was called in: skip the slot's end and after-case events.
            if (_abortCurrentSlot)
            {
                Debug.Log($"[DayOrchestrator] Case slot {_caseIndex1Based} abandoned at closing time.");
                break;
            }

```

- Change the comment `        // All case slots resolved: the shift is over.` to `        // Queue done or booth closed: the shift is over.`

- [ ] **Step 4: GameManager.**

(a) After `    [SerializeField] private Clickable readySign;` add:

```csharp

    /// <summary>Scene clock for today's shift (optional: without it the day ends only when the queue is empty).</summary>
    [SerializeField] private ShiftClockDriver shiftClock;
```

(b) After `    private readonly ReadyGate _readyGate = new ReadyGate();` add:

```csharp

    /// <summary>True from presenting a traveller until the player's decision (closing-time rule).</summary>
    private bool _travellerAtDesk;
```

(c) Directly before `        // Fresh ledger for this shift.` in `Start()` add:

```csharp
        // Shift clock (Papers, Please-style closing time).
        if (shiftClock != null)
        {
            shiftClock.Configure(_gameConfig);
            shiftClock.Closed += HandleShiftClosed;
        }

```

(d) Replace `dayFlowUI.ShowBriefing(_worldState, () => orchestrator.StartDay(_worldState, planToRun, seedToUse));` with `dayFlowUI.ShowBriefing(_worldState, () => BeginShift(planToRun, seedToUse));`. Replace `            orchestrator.StartDay(_worldState, dayPlan, seed);` with `            BeginShift(dayPlan, seed);`.

(e) In `OnDestroy()`, directly after its opening brace, add:

```csharp
        if (shiftClock != null)
            shiftClock.Closed -= HandleShiftClosed;

```

(f) In `HandleDayCompleted()`, after its `>>> Entering` log line, add:

```csharp

        // The booth is shut: freeze the clock (the queue may have run out before closing).
        _travellerAtDesk = false;
        if (shiftClock != null)
            shiftClock.StopShift();
```

(g) Before `    /// <summary>One-shot handler so the gate shows the case a single time.</summary>` insert:

```csharp
    /// <summary>Starts the day loop and the shift clock together (after the briefing).</summary>
    private void BeginShift(DayPlanSO plan, int daySeed)
    {
        orchestrator.StartDay(_worldState, plan, daySeed);

        if (shiftClock != null)
            shiftClock.StartShift();
    }

    /// <summary>
    /// Closing time: a traveller already at the desk may be finished; otherwise
    /// the booth closes at once, and a traveller still behind READY is never called.
    /// </summary>
    private void HandleShiftClosed()
    {
        ClosingAction action = ShiftFlow.OnClosing(_travellerAtDesk);
        Debug.Log($"[GameManager] Closing time (travellerAtDesk={_travellerAtDesk}) -> {action}.");

        if (action == ClosingAction.FinishCurrent)
        {
            orchestrator.CloseAfterCurrentSlot();
            return;
        }

        if (_readyGate.IsArmed)
        {
            _readyGate.Released -= ShowActiveCaseOnce;
            _readyGate.Disarm();
            if (readySign != null)
                readySign.Interactable = false;
        }

        orchestrator.CloseNow();
    }

```

(h) In `ShowActiveCase(CaseInstance inst)`, make the first statement `        _travellerAtDesk = true;`.

(i) In `HandlePlayerChoseEra` and in `HandleDecision`, directly after each method's `>>> Entering` log line, add `        _travellerAtDesk = false;`.

(j) In `HandlePlayerChoseEra`, replace `officeUI.ShowVerdict(verdict, HandleEndingReached);` with `ShowVerdictThen(verdict, HandleEndingReached);` and `officeUI.ShowVerdict(verdict, () => orchestrator.MarkCaseResolved());` with `ShowVerdictThen(verdict, () => orchestrator.MarkCaseResolved());`.

(k) Replace the body of `ShowVerdictThen` with:

```csharp
    private void ShowVerdictThen(CaseVerdict verdict, System.Action onContinue)
    {
        if (officeUI == null)
        {
            onContinue?.Invoke();
            return;
        }

        // A citation slip holds the day, and the shift clock, until acknowledged.
        bool holdsClock = shiftClock != null && verdict != null && verdict.citationIssued;
        if (holdsClock)
            shiftClock.Pause();

        officeUI.ShowVerdict(verdict, () =>
        {
            if (holdsClock)
                shiftClock.Resume();
            onContinue?.Invoke();
        });
    }
```

(Keep the method's existing `/// <summary>` line, updated to: `Shows the verdict slip if a UI is wired (pausing the shift clock while a citation slip is up), then runs the continuation.`)

- [ ] **Step 5:** Run `make_meta` on `ShiftClockDriver.cs`, then the compile check (expect exit 0) and the full test run (no failures).
- [ ] **Step 6: Commit** with the message `feat: Papers, Please-style shift clock drives closing time (driver, orchestrator close hooks, GameManager)`.

---

### Task 6: Clock readouts

**Files:**
- Create: `Assets/Scripts/Office/ShiftClockReadouts.cs`

- [ ] **Step 1: Implement**

```csharp
using TMPro;
using UnityEngine;

/// <summary>
/// Shows the shift clock: HH:MM in the monitor's taskbar tray and the hands of
/// the booth's wall clock. Polls the driver each frame (as OfficeReadouts polls
/// WorldState). Every reference is optional and null-safe.
/// </summary>
public sealed class ShiftClockReadouts : MonoBehaviour
{
    /// <summary>Today's shift clock host.</summary>
    [SerializeField] private ShiftClockDriver driver;

    [Header("Monitor taskbar tray")]
    /// <summary>Tray clock text ("09:00").</summary>
    [SerializeField] private TMP_Text trayClockText;

    [Header("Booth wall clock")]
    /// <summary>Hour hand; its pivot must sit at the dial centre, pointing to 12 at rotation 0.</summary>
    [SerializeField] private Transform hourHand;

    /// <summary>Minute hand; same pivot rule as the hour hand.</summary>
    [SerializeField] private Transform minuteHand;

    /// <summary>Whole minute last written to the tray (avoids a string per frame).</summary>
    private int _shownMinute = -1;

    private void Update()
    {
        if (driver == null || driver.Clock == null)
            return;

        Apply(driver.Clock.CurrentMinute);
    }

    /// <summary>Shows a minute of day on every wired readout.</summary>
    public void Apply(float minuteOfDay)
    {
        (float hourDegrees, float minuteDegrees) = ShiftClock.HandAngles(minuteOfDay);

        // Unity's z rotation turns counter-clockwise; clock hands turn clockwise.
        if (hourHand != null)
            hourHand.localRotation = Quaternion.Euler(0f, 0f, -hourDegrees);
        if (minuteHand != null)
            minuteHand.localRotation = Quaternion.Euler(0f, 0f, -minuteDegrees);

        int whole = Mathf.FloorToInt(minuteOfDay);
        if (trayClockText != null && whole != _shownMinute)
        {
            trayClockText.text = ShiftClock.Format(minuteOfDay);
            _shownMinute = whole;
        }
    }
}
```

- [ ] **Step 2:** Run `make_meta` and compile. **Commit** with the message `feat: tray and wall-clock readouts for the shift clock`.

---

### Task 7: Interaction feedback (cursor and hover outline)

**Files:**
- Create: `Assets/Scripts/UI/InteractionFeedbackSO.cs`, `HoverUIOutline.cs`, `SpriteOutlineBuilder.cs`, `HoverHighlighter.cs`

- [ ] **Step 1: InteractionFeedbackSO**

```csharp
using UnityEngine;

/// <summary>
/// Look of the interaction feedback: the game cursor (arrow, and a hand over
/// anything clickable) and the hover outline on booth objects and desktop UI.
/// </summary>
[CreateAssetMenu(fileName = "InteractionFeedback_", menuName = "TimeDesk/UI/Interaction Feedback", order = 10)]
public sealed class InteractionFeedbackSO : ScriptableObject
{
    [Header("Cursor")]
    /// <summary>Default cursor (Texture Type: Cursor). Null keeps the OS cursor.</summary>
    public Texture2D arrowCursor;

    /// <summary>Arrow click point, in pixels from the texture's top-left.</summary>
    public Vector2 arrowHotspot = Vector2.zero;

    /// <summary>Cursor over anything clickable (Texture Type: Cursor).</summary>
    public Texture2D handCursor;

    /// <summary>Hand click point (the fingertip), in pixels from the top-left.</summary>
    public Vector2 handHotspot = new Vector2(12f, 1f);

    [Header("Hover outline")]
    /// <summary>Outline colour for booth objects and UI.</summary>
    public Color outlineColor = Color.white;

    /// <summary>Booth outline thickness in world units.</summary>
    [Min(0.005f)]
    public float worldOutlineWidth = 0.05f;

    /// <summary>UI outline offset in pixels (uGUI Outline effect distance).</summary>
    public Vector2 uiOutlineDistance = new Vector2(3f, -3f);

    /// <summary>Unlit sprite material for booth outlines, so lighting never dims the highlight.</summary>
    public Material outlineMaterial;
}
```

- [ ] **Step 2: HoverUIOutline**

```csharp
using UnityEngine.UI;

/// <summary>
/// The hover highlight on desktop UI. A dedicated Outline subclass, so the
/// HoverHighlighter never toggles an Outline a designer added for styling.
/// </summary>
public sealed class HoverUIOutline : Outline
{
}
```

- [ ] **Step 3: SpriteOutlineBuilder**

```csharp
using UnityEngine;

/// <summary>
/// Turns a sprite into its hover-outline sprite: reads the sprite's pixels
/// through a temporary RenderTexture (so the texture needs no Read/Write),
/// builds the ring with OutlineMask, and wraps it in a Sprite whose pivot keeps
/// it aligned with the source. The caller owns the result and its texture.
/// </summary>
public static class SpriteOutlineBuilder
{
    /// <summary>Builds the outline sprite (white; tint with SpriteRenderer.color). Null if the sprite has no texture.</summary>
    public static Sprite Build(Sprite source, int ringWidthPx)
    {
        if (source == null || source.texture == null)
            return null;

        Rect region;
        Vector2 trimOffset;
        try
        {
            region = source.textureRect;         // where the sprite sits in its (possibly atlased) texture
            trimOffset = source.textureRectOffset;
        }
        catch (UnityException)
        {
            region = source.rect;                // tightly packed atlas: fall back to the sprite rect
            trimOffset = Vector2.zero;
        }

        int width = Mathf.Max(1, Mathf.RoundToInt(region.width));
        int height = Mathf.Max(1, Mathf.RoundToInt(region.height));
        byte[] alpha = ReadAlpha(source.texture, region, width, height);
        byte[] ring = OutlineMask.BuildRing(alpha, width, height, ringWidthPx, out int outWidth, out int outHeight);

        var pixels = new Color32[ring.Length];
        for (int i = 0; i < ring.Length; i++)
            pixels[i] = new Color32(255, 255, 255, ring[i]);

        var texture = new Texture2D(outWidth, outHeight, TextureFormat.RGBA32, false)
        {
            name = source.name + "_HoverOutline",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = source.texture.filterMode,
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        Vector2 pivot = source.pivot - trimOffset;
        (float pivotX, float pivotY) = OutlineMask.ShiftPivot(pivot.x, pivot.y, ringWidthPx);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, outWidth, outHeight),
            new Vector2(pivotX / outWidth, pivotY / outHeight), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        return sprite;
    }

    /// <summary>Reads the alpha of a texture region via a GPU blit (row 0 = bottom).</summary>
    private static byte[] ReadAlpha(Texture2D texture, Rect region, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;
        Texture2D readback = null;
        try
        {
            Graphics.Blit(texture, rt);
            RenderTexture.active = rt;
            readback = new Texture2D(width, height, TextureFormat.RGBA32, false);
            readback.ReadPixels(new Rect(region.x, region.y, width, height), 0, 0, false);
            readback.Apply(false);

            Color32[] pixels = readback.GetPixels32();
            var alpha = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                alpha[i] = pixels[i].a;
            return alpha;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            if (readback != null)
                Object.Destroy(readback);
        }
    }
}
```

- [ ] **Step 4: HoverHighlighter**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// One per scene. Each frame it finds what the pointer is over, through the
/// EventSystem's raycasters (the same ones clicks use), and, when that changes,
/// moves the hover highlight and swaps the cursor: a white outline and hand
/// cursor on any interactable Clickable (booth sprite) or Selectable (UI), the
/// arrow elsewhere. Objects need no setup of their own.
/// </summary>
public sealed class HoverHighlighter : MonoBehaviour
{
    /// <summary>Upper bound on the generated ring width, keeping outline generation cheap.</summary>
    private const int MaxRingWidthPx = 64;

    /// <summary>Cursor textures and outline look.</summary>
    [SerializeField] private InteractionFeedbackSO settings;

    /// <summary>Reused raycast results.</summary>
    private readonly List<RaycastResult> _hits = new List<RaycastResult>();

    /// <summary>Generated outlines per booth sprite renderer.</summary>
    private readonly Dictionary<SpriteRenderer, WorldOutline> _worldOutlines = new Dictionary<SpriteRenderer, WorldOutline>();

    /// <summary>Reused pointer data for raycasts.</summary>
    private PointerEventData _pointerData;

    /// <summary>Currently highlighted Clickable or Selectable (null = nothing).</summary>
    private Component _hovered;

    /// <summary>True when the hand cursor is showing.</summary>
    private bool _handCursor;

    /// <summary>True once a cursor was set by this component.</summary>
    private bool _cursorApplied;

    /// <summary>Generated outline for one booth sprite (rebuilt when its sprite changes).</summary>
    private sealed class WorldOutline
    {
        /// <summary>Sprite the outline was built from.</summary>
        public Sprite source;

        /// <summary>Generated outline sprite (null if generation failed).</summary>
        public Sprite outline;

        /// <summary>Child renderer that shows the outline.</summary>
        public SpriteRenderer renderer;
    }

    private void OnEnable()
    {
        if (settings == null)
            Debug.LogWarning("HoverHighlighter: no InteractionFeedbackSO assigned, so there is no custom cursor or hover outline. Run Tools > TimeDesk > Build Office UI.", this);
    }

    private void Update()
    {
        if (settings == null)
            return;

        Component target = FindHoverTarget();
        if (target != _hovered)
        {
            SetHighlighted(_hovered, false);
            _hovered = target;
            SetHighlighted(_hovered, true);
        }

        ApplyCursor(_hovered != null);
    }

    private void OnDisable()
    {
        SetHighlighted(_hovered, false);
        _hovered = null;
        if (_cursorApplied)
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        _cursorApplied = false;
    }

    private void OnDestroy()
    {
        foreach (WorldOutline o in _worldOutlines.Values)
        {
            DestroyOutlineSprite(o);
            if (o.renderer != null)
                Destroy(o.renderer.gameObject);
        }
        _worldOutlines.Clear();
    }

    /// <summary>Topmost interactable under the pointer, or null.</summary>
    private Component FindHoverTarget()
    {
        EventSystem eventSystem = EventSystem.current;
        Pointer pointer = Pointer.current;
        if (eventSystem == null || pointer == null)
            return null;

        if (_pointerData == null)
            _pointerData = new PointerEventData(eventSystem);
        _pointerData.position = pointer.position.ReadValue();

        _hits.Clear();
        eventSystem.RaycastAll(_pointerData, _hits);
        return _hits.Count > 0 ? InteractiveOn(_hits[0].gameObject) : null;
    }

    /// <summary>
    /// The interactable Clickable or Selectable on this object or its nearest
    /// parent that has one; null if that one is not interactable (or there is none).
    /// </summary>
    private static Component InteractiveOn(GameObject go)
    {
        for (Transform t = go.transform; t != null; t = t.parent)
        {
            Clickable clickable = t.GetComponent<Clickable>();
            if (clickable != null && clickable.isActiveAndEnabled)
                return clickable.Interactable ? clickable : null;

            Selectable selectable = t.GetComponent<Selectable>();
            if (selectable != null && selectable.isActiveAndEnabled)
                return selectable.IsInteractable() ? selectable : null;
        }
        return null;
    }

    /// <summary>Turns the highlight on a target on or off.</summary>
    private void SetHighlighted(Component target, bool on)
    {
        if (target == null)
            return;

        if (target is Clickable clickable)
        {
            SpriteRenderer sr = clickable.GetComponent<SpriteRenderer>();
            if (sr == null)
                return;

            WorldOutline outline = on ? EnsureWorldOutline(sr) : Lookup(sr);
            if (outline != null && outline.renderer != null)
                outline.renderer.enabled = on && outline.outline != null;
        }
        else if (target is Selectable selectable && selectable.targetGraphic != null)
        {
            GameObject host = selectable.targetGraphic.gameObject;
            HoverUIOutline uiOutline = host.GetComponent<HoverUIOutline>();
            if (uiOutline == null)
            {
                if (!on)
                    return;
                uiOutline = host.AddComponent<HoverUIOutline>();
            }

            uiOutline.effectColor = settings.outlineColor;
            uiOutline.effectDistance = settings.uiOutlineDistance;
            uiOutline.enabled = on;
        }
    }

    /// <summary>Existing outline for a renderer, or null.</summary>
    private WorldOutline Lookup(SpriteRenderer sr) =>
        _worldOutlines.TryGetValue(sr, out WorldOutline o) ? o : null;

    /// <summary>Creates or refreshes the outline child for a booth sprite.</summary>
    private WorldOutline EnsureWorldOutline(SpriteRenderer sr)
    {
        if (sr.sprite == null)
            return null;

        if (!_worldOutlines.TryGetValue(sr, out WorldOutline o))
        {
            o = new WorldOutline();
            _worldOutlines[sr] = o;
        }

        if (o.renderer == null)
        {
            var child = new GameObject("HoverOutline");
            child.transform.SetParent(sr.transform, false);
            o.renderer = child.AddComponent<SpriteRenderer>();
        }

        // Rebuild only when the sprite changed (e.g. the timeline poster swapped art).
        if (o.source != sr.sprite)
        {
            DestroyOutlineSprite(o);
            o.source = sr.sprite;
            try
            {
                o.outline = SpriteOutlineBuilder.Build(sr.sprite, RingWidthPx(sr));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"HoverHighlighter: could not build an outline for '{sr.sprite.name}' ({e.Message}).", sr);
            }
        }

        SpriteRenderer r = o.renderer;
        r.sprite = o.outline;
        r.sortingLayerID = sr.sortingLayerID;
        r.sortingOrder = sr.sortingOrder + 1;
        r.flipX = sr.flipX;
        r.flipY = sr.flipY;
        r.color = settings.outlineColor;
        if (settings.outlineMaterial != null)
            r.sharedMaterial = settings.outlineMaterial;
        return o;
    }

    /// <summary>Ring width in source pixels for the configured world-space width.</summary>
    private int RingWidthPx(SpriteRenderer sr)
    {
        Vector3 s = sr.transform.lossyScale;
        float scale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
        if (scale <= 0f)
            return 1;

        return Mathf.Clamp(Mathf.RoundToInt(settings.worldOutlineWidth * sr.sprite.pixelsPerUnit / scale), 1, MaxRingWidthPx);
    }

    /// <summary>Sets the arrow or the hand cursor (only on change).</summary>
    private void ApplyCursor(bool hand)
    {
        if (_cursorApplied && hand == _handCursor)
            return;

        bool useHand = hand && settings.handCursor != null;
        Texture2D texture = useHand ? settings.handCursor : settings.arrowCursor;
        Vector2 hotspot = useHand ? settings.handHotspot : settings.arrowHotspot;
        Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        _handCursor = hand;
        _cursorApplied = true;
    }

    /// <summary>Destroys a generated outline sprite and its texture.</summary>
    private static void DestroyOutlineSprite(WorldOutline o)
    {
        if (o.outline == null)
            return;

        Texture2D texture = o.outline.texture;
        Destroy(o.outline);
        if (texture != null)
            Destroy(texture);
        o.outline = null;
    }
}
```

- [ ] **Step 5:** Run `make_meta` on the four files, then compile (expect exit 0) and run the full tests.
- [ ] **Step 6: Commit** with the message `feat: game cursor + hover outline on clickables and desktop UI (HoverHighlighter)`.

---

### Task 8: Builder wiring (OfficeSceneUIBuilder)

**Files:**
- Modify: `Assets/Editor/OfficeSceneUIBuilder.cs`

- [ ] **Step 1: Tray clock.** Change the call `BuildTaskbar(root, out TMP_Text dayText, out TMP_Text moneyText, out TMP_Text stabilityText);` to `BuildTaskbar(root, out TMP_Text dayText, out TMP_Text moneyText, out TMP_Text stabilityText, out TMP_Text trayClockText);`. Replace `BuildTaskbar`'s signature and its tray block (from `Transform tray = Panel(bar, "Tray", ...` through the `stabilityText = ...` line) with:

```csharp
    private static void BuildTaskbar(Transform root, out TMP_Text dayText, out TMP_Text moneyText, out TMP_Text stabilityText, out TMP_Text clockText)
```

```csharp
        // System tray: Day | Credits | Stability | Clock. Text() returns existing
        // objects unchanged, so the slot anchors are re-applied here (authoritative).
        Transform tray = Panel(bar, "Tray", new Vector2(0.64f, 0.12f), new Vector2(0.995f, 0.88f), Vector2.zero, Vector2.zero, new Color(0.1f, 0.32f, 0.78f, 1f));
        dayText = Text(tray, "DayText", "Day 1", 18, TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(0.2f, 1f), Color.white);
        moneyText = Text(tray, "MoneyText", "Credits: 0", 18, TextAlignmentOptions.Center, new Vector2(0.2f, 0f), new Vector2(0.48f, 1f), Color.white);
        stabilityText = Text(tray, "StabilityText", "Stability: 100%", 18, TextAlignmentOptions.Center, new Vector2(0.48f, 0f), new Vector2(0.8f, 1f), Color.white);
        clockText = Text(tray, "ClockText", "09:00", 18, TextAlignmentOptions.Center, new Vector2(0.8f, 0f), new Vector2(1f, 1f), Color.white);
        SetAnchors(dayText.transform, new Vector2(0f, 0f), new Vector2(0.2f, 1f));
        SetAnchors(moneyText.transform, new Vector2(0.2f, 0f), new Vector2(0.48f, 1f));
        SetAnchors(stabilityText.transform, new Vector2(0.48f, 0f), new Vector2(0.8f, 1f));
        SetAnchors(clockText.transform, new Vector2(0.8f, 0f), new Vector2(1f, 1f));
```

- [ ] **Step 2: Build calls.** Directly after `        BuildDesktopShell(canvas, bookShelf, windowLayer);` add:

```csharp

        // Cursor + hover highlight: one scene component; clickables need no setup.
        BuildInteractionFeedback();

        // Shift clock: driver beside the GameManager, tray + wall-clock readouts.
        ShiftClockDriver shiftClock = gameManager.GetComponent<ShiftClockDriver>();
        if (shiftClock == null)
            shiftClock = gameManager.gameObject.AddComponent<ShiftClockDriver>();
        BuildShiftClockReadouts(shiftClock, trayClockText);
```

After the `SetRef(soGm, "readySign", ...)` line add `        SetRef(soGm, "shiftClock", shiftClock);`.

- [ ] **Step 3: New builder methods.** Insert before `    /// <summary>Adds a visible "Back to Office" button to the desktop canvas.</summary>`:

```csharp
    // ----------------------------- Shift clock (booth + tray) -----------------------------

    /// <summary>Ink colour of the placeholder wall clock.</summary>
    private static readonly Color32 ClockInk = new Color32(30, 28, 26, 255);

    /// <summary>
    /// Builds the booth wall clock (face plus hour and minute hands; placeholder
    /// art until the real clock pieces land) and wires ShiftClockReadouts on
    /// OfficeRoot to the driver, the tray clock and the hands. Idempotent.
    /// </summary>
    private static void BuildShiftClockReadouts(ShiftClockDriver driver, TMP_Text trayClockText)
    {
        GameObject root = GameObject.Find("OfficeRoot");
        if (root == null)
        {
            Debug.LogWarning("[TimeDesk] No OfficeRoot, so the wall clock was not built (the booth is built first in Build()).");
            return;
        }

        SpriteRenderer face = EnsureSprite(root.transform, "WallClock",
            EnsureOfficeShape("clock_face", 100, 100, new Vector2(0.5f, 0.5f), ClockFacePixel),
            new Vector3(6.2f, 3.3f, 4.5f), -40);
        SpriteRenderer hourHand = EnsureSprite(face.transform, "HourHand",
            EnsureOfficeShape("clock_hand_hour", 8, 30, new Vector2(0.5f, 0.1f), (x, y) => ClockInk),
            new Vector3(0f, 0f, -0.01f), -39);
        SpriteRenderer minuteHand = EnsureSprite(face.transform, "MinuteHand",
            EnsureOfficeShape("clock_hand_minute", 6, 42, new Vector2(0.5f, 0.07f), (x, y) => ClockInk),
            new Vector3(0f, 0f, -0.02f), -38);

        ShiftClockReadouts readouts = root.GetComponent<ShiftClockReadouts>();
        if (readouts == null)
            readouts = root.AddComponent<ShiftClockReadouts>();
        var so = new SerializedObject(readouts);
        SetRef(so, "driver", driver);
        SetRef(so, "trayClockText", trayClockText);
        SetRef(so, "hourHand", hourHand.transform);
        SetRef(so, "minuteHand", minuteHand.transform);
        so.ApplyModifiedProperties();
    }

    /// <summary>Placeholder clock face: cream dial, dark rim, hour ticks, centre cap.</summary>
    private static Color32 ClockFacePixel(int x, int y)
    {
        float dx = x - 49.5f;
        float dy = y - 49.5f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d > 49f)
            return new Color32(0, 0, 0, 0);
        if (d > 45f)
            return ClockInk;
        float angle = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg, 30f);
        if (d > 37f && (angle < 2.5f || angle > 27.5f))
            return ClockInk;
        if (d < 3f)
            return ClockInk;
        return new Color32(242, 237, 220, 255);
    }

    /// <summary>
    /// Returns a placeholder Sprite at Assets/Art/Office/Placeholder/{name}.png drawn
    /// by a pixel function, with a custom pivot (e.g. clock hands pivot at their
    /// base). Created once; swap the PNG for final art and keep the .meta (the pivot lives there).
    /// </summary>
    private static Sprite EnsureOfficeShape(string name, int w, int h, Vector2 pivot, System.Func<int, int, Color32> pixel)
    {
        string folder = "Assets/Art/Office/Placeholder";
        string assetPath = $"{folder}/{name}.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (existing != null)
            return existing;

        EnsureFolderTree(folder);

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = pixel(x, y);
        tex.SetPixels32(pixels);
        tex.Apply();

        string abs = System.IO.Path.Combine(Application.dataPath, $"Art/Office/Placeholder/{name}.png");
        System.IO.File.WriteAllBytes(abs, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        if (AssetImporter.GetAtPath(assetPath) is TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    /// <summary>Sets a RectTransform's anchors and zeroes its offsets (stretch within the anchors).</summary>
    private static void SetAnchors(Transform t, Vector2 aMin, Vector2 aMax)
    {
        var rt = (RectTransform)t;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ----------------------------- Cursor + hover highlight -----------------------------

    /// <summary>Settings asset for the cursor and hover outline.</summary>
    private const string InteractionFeedbackPath = "Assets/Data/Config/InteractionFeedback_Default.asset";

    /// <summary>URP's unlit sprite material (outlines ignore 2D lighting).</summary>
    private const string UnlitSpriteMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

    /// <summary>Where generated placeholder cursors live (never mistaken for final art).</summary>
    private const string PlaceholderCursorFolder = "Assets/Art/Generated/Cursors";

    /// <summary>Placeholder arrow outline, in top-left pixel coordinates of a 32x32 cursor.</summary>
    private static readonly Vector2[] ArrowCursorShape =
    {
        new Vector2(0, 0), new Vector2(0, 22), new Vector2(5, 17), new Vector2(9, 26),
        new Vector2(12, 25), new Vector2(8, 16), new Vector2(15, 16),
    };

    /// <summary>Placeholder pointing hand (fingertip at 12,1), top-left pixel coordinates.</summary>
    private static readonly Vector2[] HandCursorShape =
    {
        new Vector2(10, 1), new Vector2(13, 1), new Vector2(14, 2), new Vector2(14, 12),
        new Vector2(21, 13), new Vector2(23, 15), new Vector2(23, 25), new Vector2(19, 30),
        new Vector2(10, 30), new Vector2(6, 24), new Vector2(5, 18), new Vector2(7, 17),
        new Vector2(10, 19),
    };

    /// <summary>
    /// Ensures the interaction-feedback settings (cursor art by file name when
    /// present, placeholders otherwise; unlit outline material) and a
    /// HoverHighlighter on the EventSystem wired to them. Idempotent.
    /// </summary>
    private static void BuildInteractionFeedback()
    {
        InteractionFeedbackSO settings = AssetDatabase.LoadAssetAtPath<InteractionFeedbackSO>(InteractionFeedbackPath);
        if (settings == null)
        {
            EnsureFolderTree("Assets/Data/Config");
            settings = ScriptableObject.CreateInstance<InteractionFeedbackSO>();
            AssetDatabase.CreateAsset(settings, InteractionFeedbackPath);
        }

        // Replace only empty or placeholder cursors, so final art wins but a designer's pick is kept.
        if (settings.arrowCursor == null || IsPlaceholderCursor(settings.arrowCursor))
            settings.arrowCursor = EnsureCursorTexture("cursor_arrow", ArrowCursorShape);
        if (settings.handCursor == null || IsPlaceholderCursor(settings.handCursor))
            settings.handCursor = EnsureCursorTexture("cursor_hand", HandCursorShape);
        if (settings.outlineMaterial == null)
            settings.outlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(UnlitSpriteMaterialPath);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("[TimeDesk] No EventSystem, so no HoverHighlighter was added.");
            return;
        }

        HoverHighlighter highlighter = eventSystem.GetComponent<HoverHighlighter>();
        if (highlighter == null)
            highlighter = eventSystem.gameObject.AddComponent<HoverHighlighter>();
        var so = new SerializedObject(highlighter);
        SetRef(so, "settings", settings);
        so.ApplyModifiedProperties();
    }

    /// <summary>True for a cursor generated by this builder.</summary>
    private static bool IsPlaceholderCursor(Texture2D texture) =>
        AssetDatabase.GetAssetPath(texture).StartsWith(PlaceholderCursorFolder);

    /// <summary>
    /// Returns the cursor texture named <paramref name="artName"/> (final art, anywhere
    /// under Assets) or a generated placeholder, with Cursor import settings applied.
    /// </summary>
    private static Texture2D EnsureCursorTexture(string artName, Vector2[] placeholderShape)
    {
        Texture2D art = FindTextureByName(artName, PlaceholderCursorFolder);
        string path = art != null ? AssetDatabase.GetAssetPath(art) : $"{PlaceholderCursorFolder}/placeholder_{artName}.png";

        if (art == null && AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
        {
            EnsureFolderTree(PlaceholderCursorFolder);
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = CursorPixel(placeholderShape, x, y, size);
            tex.SetPixels32(pixels);
            tex.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        if (AssetImporter.GetAtPath(path) is TextureImporter imp &&
            (imp.textureType != TextureImporterType.Cursor || !imp.isReadable || imp.mipmapEnabled))
        {
            imp.textureType = TextureImporterType.Cursor;
            imp.isReadable = true;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    /// <summary>First Texture2D whose file name is exactly <paramref name="name"/>, outside a folder.</summary>
    private static Texture2D FindTextureByName(string name, string excludeFolder)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{name} t:Texture2D"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.StartsWith(excludeFolder) || System.IO.Path.GetFileNameWithoutExtension(p) != name)
                continue;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }
        return null;
    }

    /// <summary>Placeholder cursor pixel: white inside the polygon, 1 px black edge, clear outside.</summary>
    private static Color32 CursorPixel(Vector2[] polygon, int x, int y, int size)
    {
        bool Inside(int px, int py) => InPolygon(polygon, px + 0.5f, (size - 1 - py) + 0.5f);

        if (!Inside(x, y))
            return new Color32(0, 0, 0, 0);
        bool edge = !Inside(x - 1, y) || !Inside(x + 1, y) || !Inside(x, y - 1) || !Inside(x, y + 1);
        return edge ? new Color32(0, 0, 0, 255) : new Color32(255, 255, 255, 255);
    }

    /// <summary>Even-odd point-in-polygon test.</summary>
    private static bool InPolygon(Vector2[] polygon, float px, float py)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if ((polygon[i].y > py) != (polygon[j].y > py) &&
                px < (polygon[j].x - polygon[i].x) * (py - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                inside = !inside;
        }
        return inside;
    }

```

- [ ] **Step 4:** Compile (expect exit 0). **Commit** with the message `feat(builder): wire cursor settings, HoverHighlighter, shift clock driver, tray + wall clock`.

---

### Task 9: Queue size and docs

**Files:**
- Modify: `Assets/Data/Dayplan/DayPlan_1.asset`, `DayPlan_2.asset`, `DayPlan_3.asset`, `Assets/Data/Investigation/DayPlan_Inv_Day1.asset`, `DayPlan_Inv_Day2.asset`, `DayPlan_Inv_Day3.asset` (`visitorsCount:` → `12`)
- Modify: `docs/FEATURES.md`, and the spec (§2: drop the `ShouldStartSlot` mention; the orchestrator owns that check)

- [ ] **Step 1:** Run `sed -i 's/^  visitorsCount: [0-9]*$/  visitorsCount: 12/' <the six files>`. Verify with `grep -n visitorsCount` that all six show 12.
- [ ] **Step 2:** Update FEATURES.md. Under **Office scene**, add `- [ ] Analog wall clock (placeholder face + hands) driven by the shift clock`. Under **Fake-OS desktop**, change the taskbar line to include `Clock`. Add two new sections before **Scoring & consequences**:

```markdown
## Shift clock & queue

- [ ] Papers, Please-style clock: 09:00–17:00 over 8 real minutes (GameConfigSO "Shift clock"); starts at Start Shift (tested: `ShiftClockTests`)
- [ ] The day plan's visitor count is the queue size (12); the day ends at closing time or when the queue empties
- [ ] Closing: a traveller at the desk may be finished; one still behind READY is never called (tested: `ShiftFlowTests`, `ReadyGateTests`)
- [ ] The clock pauses only while a citation slip is shown
- [ ] Visitor names are unique within a day ("Marcus II" once a pool runs out) (tested: `NameRosterTests`)

## Interaction feedback

- [ ] Game cursor: arrow, or a hand over anything clickable (`InteractionFeedbackSO`)
- [ ] Hover highlight: white outline on booth clickables and desktop UI buttons and icons (tested: `OutlineMaskTests`)
```

Also change "citation slip pauses the day until acknowledged" to "citation slip pauses the day (and the shift clock) until acknowledged".

- [ ] **Step 3: Commit** with the message `feat(content,docs): 12-traveller queues; FEATURES inventory for clock + hover`.

---

### Task 10: Verification in Unity (on the branch, before any merge)

*Revised after review: the scene is built on the branch, so a merge never ships the queue change without the clock.*

- [ ] Open the WORKTREE (`E:\unity\NOPE-feat-clock`) as its own Unity project; the main checkout stays untouched for Codex. Run **Tools > TimeDesk > Build Office UI (HUD + Panels)** on its OfficeScene and save.
- [ ] Commit on the branch: `OfficeScene.unity`, `InteractionFeedback_Default.asset`, `RunConfig.asset` (interactionFeedback reference), the generated placeholders (`Generated/Cursors/*`, `clock_face`, `clock_hand_hour`, `clock_hand_minute`) and all their `.meta` files. Check that the scene diff only adds the shift-clock pieces.
- [ ] **Window > General > Test Runner > EditMode > Run All**: all green.
- [ ] Play: hover the CRT and READY sign (outline + hand), hover desktop buttons (outline + hand), watch the tray and wall clock from 09:00, shorten `shiftRealSeconds` to 30 in GameConfig_Default to test closing both ways (mid-traveller, and waiting at READY), and confirm the citation slip pauses the clock. Check that the Title and Home screens show the game cursor too.
- [ ] Merge only when Saleh confirms Codex is idle. If Codex committed scene changes first, rebase and re-run the builder (it is authoritative) before merging.
