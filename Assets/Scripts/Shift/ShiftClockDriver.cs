using System;
using UnityEngine;

/// <summary>
/// Scene host for the pure <see cref="ShiftClock"/>: builds today's clock from
/// the day's hours (DayPlanSO.Shift: the plan's own, else GameConfigSO's
/// standard day) and GameConfigSO's real seconds, ticks it with scaled time,
/// and re-raises Closed. GameManager
/// starts and stops it; readouts (and later, lighting) read <see cref="Clock"/>.
/// While enabled it is also the read-only shift hook for the art side
/// (<see cref="Live"/>, <see cref="IShiftProgress"/>).
/// </summary>
public sealed class ShiftClockDriver : MonoBehaviour, IShiftProgress
{
    /// <summary>
    /// The gameplay layer's shift while a driver is enabled; null otherwise (the art
    /// office on its own, edit mode). Presentation code outside the gameplay layer
    /// (the art office's crowd palette) reads it; only the driver sets it.
    /// </summary>
    public static IShiftProgress Live { get; private set; }

    /// <summary>Today's clock (null until Configure).</summary>
    public ShiftClock Clock { get; private set; }

    /// <summary>Where the clock's hour stands on the standard day (<see cref="ShiftHours.StandardProgress"/>); 0 until Configure.</summary>
    public float StandardProgress01 => Clock != null ? ShiftHours.StandardProgress(Clock.CurrentMinute, _standard) : 0f;

    /// <summary>The clock's minute of the day (<see cref="ShiftClock.CurrentMinute"/>); 09:00 until Configure.</summary>
    public float MinuteOfDay => Clock != null ? Clock.CurrentMinute : 540f;

    /// <summary>Today's desk hours as the clock runs them (the standard day until Configure).</summary>
    public ShiftHours Hours => Clock != null ? new ShiftHours(Clock.StartMinute, Clock.EndMinute) : _standard;

    /// <summary>The config's standard day (GameConfigSO.Standard), which StandardProgress01 reads the hour on.</summary>
    private ShiftHours _standard = new ShiftHours(540, 1020);

    /// <summary>Raised once, when the clock reaches closing time.</summary>
    public event Action Closed;

    /// <summary>
    /// Builds a fresh stopped clock for today: <paramref name="plan"/>'s hours
    /// (its own, else the config's standard day; DayPlanSO.Shift) over the
    /// config's real seconds. Invalid hours or a missing config fall back to
    /// GameConfigSO's default values, with a warning.
    /// </summary>
    public void Configure(GameConfigSO config, DayPlanSO plan)
    {
        if (Clock != null)
            Clock.Closed -= RaiseClosed;

        _standard = GameConfigSO.Standard(config);
        ShiftHours hours = plan != null ? plan.Shift(config) : _standard;
        Clock = TryBuild(hours, config != null ? config.shiftRealSeconds : 0f);
        if (Clock == null)
        {
            // Defaults live in GameConfigSO's field initializers: read them from a throwaway instance.
            GameConfigSO defaults = ScriptableObject.CreateInstance<GameConfigSO>();
            _standard = GameConfigSO.Standard(defaults);
            Clock = TryBuild(_standard, defaults.shiftRealSeconds);
            Destroy(defaults);
            Debug.LogWarning(config == null
                ? "ShiftClockDriver: no GameConfigSO, so the default shift hours are used."
                : $"ShiftClockDriver: today's shift is invalid (hours {hours}, {config.shiftRealSeconds}s; GameConfigSO '{config.name}' under 'Shift clock', or the day plan's world_source.json shiftStart / shiftEnd), so the defaults are used.", this);
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

    /// <summary>Spends <paramref name="minutes"/> of shift time at once (ShiftClock.Spend: a waiver signed from the desk's pad).</summary>
    public void Spend(float minutes) => Clock?.Spend(minutes);

    /// <summary>Advances the clock with scaled time.</summary>
    private void Update() => Clock?.Tick(Time.deltaTime);

    /// <summary>Publishes this driver as the live shift hook.</summary>
    private void OnEnable() => Live = this;

    /// <summary>Withdraws the hook, unless another driver has published since.</summary>
    private void OnDisable()
    {
        if (ReferenceEquals(Live, this))
            Live = null;
    }

    /// <summary>Unhooks from the clock.</summary>
    private void OnDestroy()
    {
        if (Clock != null)
            Clock.Closed -= RaiseClosed;
    }

    /// <summary>Forwards the clock's Closed event.</summary>
    private void RaiseClosed() => Closed?.Invoke();

    /// <summary>Builds a clock over <paramref name="hours"/> in <paramref name="realSeconds"/>, or null when they are invalid.</summary>
    private static ShiftClock TryBuild(ShiftHours hours, float realSeconds)
    {
        try
        {
            return new ShiftClock(hours.StartMinute, hours.EndMinute, realSeconds);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
