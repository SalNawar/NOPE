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
/// Reduce flashing every flash is a static tint instead (counted the same).
/// Full-screen flashes stay the event tier's, and never saturated red (G3):
/// a rule for the flash's owner, measured by QA-RIG's flash meter. Times are
/// the caller's clock (unscaled seconds). Pure.
/// </summary>
public sealed class FlashBudget
{
    /// <summary>The most flashes drawn in any one second (G3).</summary>
    public const int PerSecond = 3;

    /// <summary>How long a Reduce-flashing tint holds (seconds; G3: "a static tint held 200 ms").</summary>
    public const float TintSeconds = 0.2f;

    /// <summary>When each flash of the last second started.</summary>
    private readonly Queue<float> _recent = new Queue<float>(PerSecond);

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
/// <see cref="FlashBudget"/>. Static, so a flash anywhere asks the same budget.
/// </summary>
public static class FlashPolicy
{
    /// <summary>The shared budget.</summary>
    private static readonly FlashBudget Budget = new FlashBudget();

    /// <summary>Reduce flashing (the Settings switch): every flash becomes a static tint.</summary>
    public static bool ReduceFlashing { get; set; }

    /// <summary>How a flash at <paramref name="now"/> is drawn (the shared budget, the setting).</summary>
    public static FlashStyle Request(float now) => Budget.Request(now, ReduceFlashing);
}
