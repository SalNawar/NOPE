using System;
using System.Collections.Generic;

/// <summary>How a requested flash may be drawn now (FlashPolicy.Request). Not serialized.</summary>
public enum FlashStyle
{
    /// <summary>Not at all: the budget of flashes this second is spent (draw nothing).</summary>
    None,

    /// <summary>As designed: a flash.</summary>
    Flash,

    /// <summary>As a static tint held <see cref="FlashBudget.TintSeconds"/> (Reduce flashing), never a flash.</summary>
    Tint
}

/// <summary>
/// The flash budget (GAME_FEEL_BIBLE G3, gap 2: "UI-PC builds the setting
/// and the static FlashPolicy"): at most <see cref="PerSecond"/> flashes in
/// any one second anywhere on screen (a request past it draws nothing); with
/// Reduce flashing every flash is a static tint held
/// <see cref="TintSeconds"/> instead (counted the same). The two numbers are
/// the caller's knobs (DesktopConfigSO). Full-screen flashes stay the event
/// tier's, and never saturated red (G3): a rule for the flash's owner,
/// measured by QA-RIG's flash meter. Times are the caller's clock (unscaled
/// seconds). Pure.
/// </summary>
public sealed class FlashBudget
{
    /// <summary>The most flashes drawn in any one second (G3: 3; at least 1).</summary>
    public readonly int PerSecond;

    /// <summary>How long a Reduce-flashing tint holds (seconds; G3: "a static tint held 200 ms"; at least 0).</summary>
    public readonly float TintSeconds;

    /// <summary>When each flash of the last second started.</summary>
    private readonly Queue<float> _recent;

    /// <summary>A budget of <paramref name="perSecond"/> flashes a second (at least 1), a tint held <paramref name="tintSeconds"/> (at least 0).</summary>
    public FlashBudget(int perSecond, float tintSeconds)
    {
        PerSecond = Math.Max(1, perSecond);
        TintSeconds = Math.Max(0f, tintSeconds);
        _recent = new Queue<float>(PerSecond);
    }

    /// <summary>How a flash asked for at <paramref name="now"/> is drawn, with Reduce flashing <paramref name="reduce"/>; a granted one counts against the budget.</summary>
    public FlashStyle Request(float now, bool reduce)
    {
        while (_recent.Count > 0 && (now - _recent.Peek() >= 1f || now < _recent.Peek()))
            _recent.Dequeue();
        if (_recent.Count >= PerSecond)
            return FlashStyle.None;
        _recent.Enqueue(now);
        return reduce ? FlashStyle.Tint : FlashStyle.Flash;
    }
}

/// <summary>
/// The one flash budget every flash goes through (G3): the Reduce flashing
/// setting (Settings; kept per player by FlashPreference) and the shared
/// <see cref="FlashBudget"/>, whose knobs the office's config sets when it
/// loads (<see cref="Configure"/>, from DesktopConfigSO; until then G3's
/// own numbers, 3 a second and a 200 ms tint). Static, so a flash anywhere
/// asks the same budget.
/// </summary>
public static class FlashPolicy
{
    /// <summary>The shared budget.</summary>
    private static FlashBudget _budget = new FlashBudget(3, 0.2f);

    /// <summary>Reduce flashing (the Settings switch): every flash becomes a static tint.</summary>
    public static bool ReduceFlashing { get; set; }

    /// <summary>How long a Reduce-flashing tint holds now (seconds).</summary>
    public static float TintSeconds => _budget.TintSeconds;

    /// <summary>The budget's knobs (DesktopConfigSO, when the office loads); a change starts a fresh budget.</summary>
    public static void Configure(int perSecond, float tintSeconds)
    {
        if (perSecond != _budget.PerSecond || Math.Abs(tintSeconds - _budget.TintSeconds) > 1e-6f)
            _budget = new FlashBudget(perSecond, tintSeconds);
    }

    /// <summary>How a flash at <paramref name="now"/> is drawn (the shared budget, the setting).</summary>
    public static FlashStyle Request(float now) => _budget.Request(now, ReduceFlashing);
}
