using System;
using UnityEngine;

/// <summary>
/// Scene host for the pure <see cref="ShiftClock"/>: builds today's clock from
/// GameConfigSO, ticks it with scaled time, and re-raises Closed. GameManager
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

    /// <summary>0 at opening, 1 at closing (<see cref="ShiftClock.Progress01"/>); 0 until Configure.</summary>
    public float Progress01 => Clock != null ? Clock.Progress01 : 0f;

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
