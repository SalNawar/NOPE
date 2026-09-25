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
}
