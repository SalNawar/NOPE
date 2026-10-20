/// <summary>
/// One action per input (BUG-58a, UI-PC; UI-25: "places then cancels on a
/// double-click", "Enter fires buttons twice"): an action is taken, then any
/// further request within <see cref="Seconds"/> of it is refused, so a
/// double-click on Order places one order (the button turns into Cancel
/// order under the second click, which must not cancel it) and an Enter that
/// both the EventSystem and the desktop's keys see acts once. Times are the
/// caller's clock (unscaled seconds). Pure.
/// </summary>
public sealed class ActionDebounce
{
    /// <summary>How long after an action a further one is refused (seconds).</summary>
    public readonly float Seconds;

    /// <summary>When the last action was taken (seconds), or none yet.</summary>
    private float? _last;

    /// <summary>A debounce of <paramref name="seconds"/> (negative counts as 0: every request acts).</summary>
    public ActionDebounce(float seconds) => Seconds = seconds < 0f ? 0f : seconds;

    /// <summary>True when an action may be taken at <paramref name="now"/> (and records it); false within the window of the last one.</summary>
    public bool TryAct(float now)
    {
        if (_last.HasValue && now - _last.Value < Seconds && now >= _last.Value)
            return false;
        _last = now;
        return true;
    }
}
