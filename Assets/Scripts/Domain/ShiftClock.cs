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

    /// <summary>True from Start() until the shift ends (closing time or Finish()).</summary>
    public bool IsStarted { get; private set; }

    /// <summary>True while at least one Pause() has no matching Resume().</summary>
    public bool IsPaused => _pauseCount > 0;

    /// <summary>True once the shift ended: the clock reached closing time, or the shift finished early (Finish). Permanent for this clock.</summary>
    public bool IsClosed { get; private set; }

    /// <summary>True when Tick advances time.</summary>
    public bool IsRunning => IsStarted && !IsPaused && !IsClosed;

    /// <summary>0 at opening, 1 at closing (drives time-of-day visuals).</summary>
    public float Progress01 => (CurrentMinute - StartMinute) / (EndMinute - StartMinute);

    /// <summary>Raised exactly once, when the shift ends: the clock reaches closing time, or the shift finishes early (Finish), so the closing bell rings either way (BUG-72).</summary>
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

    /// <summary>Starts the clock. No effect once closed.</summary>
    public void Start()
    {
        if (!IsClosed)
            IsStarted = true;
    }

    /// <summary>
    /// The shift ends before closing time (the queue ran out, End Shift): the
    /// clock stops at the minute it shows and closes, raising Closed once, so
    /// an early finish rings the closing bell too (BUG-72). Nothing when the
    /// shift never started (AVAILABLE was never pressed) or already closed.
    /// </summary>
    public void Finish()
    {
        if (!IsStarted || IsClosed)
            return;
        IsStarted = false;
        IsClosed = true;
        Closed?.Invoke();
    }

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

    /// <summary>
    /// Spends <paramref name="minutes"/> of shift time at once (a desk task that
    /// takes time, such as a waiver signed from the pad: GameConfigSO.waiverSignMinutes),
    /// whether or not the clock is paused; clamps at closing and raises Closed
    /// once. Nothing before Start, once closed, or for no positive minutes.
    /// </summary>
    public void Spend(float minutes)
    {
        if (!IsStarted || IsClosed || !(minutes > 0f))
            return;

        CurrentMinute = Math.Min(EndMinute, CurrentMinute + minutes);
        if (CurrentMinute >= EndMinute)
        {
            IsClosed = true;
            Closed?.Invoke();
        }
    }

    /// <summary>
    /// 24-hour "HH:MM" for a minute of day (fractions round down); the end of
    /// the day (1440) is "24:00", the night shift's closing hour (BUG-72), and
    /// a later minute wraps into the next day.
    /// </summary>
    public static string Format(float minuteOfDay)
    {
        int whole = (int)Math.Floor(minuteOfDay);
        if (whole == MinutesPerDay)
            return "24:00";
        whole = ((whole % MinutesPerDay) + MinutesPerDay) % MinutesPerDay;
        return $"{whole / 60:00}:{whole % 60:00}";
    }
}
