using System;

/// <summary>
/// The double-click rule (the PC redesign DK6, WN3), used by title bars (and
/// later by icons) instead of the input module's clickCount: a click is the
/// second of a double when it comes at most maxSeconds after the last one and
/// at most maxDistance (desktop units) from it. Pure.
/// </summary>
public static class ClickTiming
{
    /// <summary>The last-click time that means "no click yet": nothing pairs with it.</summary>
    public const float Never = float.NegativeInfinity;

    /// <summary>True when the click at (x, y) at <paramref name="now"/> completes a double with the one at (lastX, lastY) at <paramref name="lastTime"/>.</summary>
    public static bool IsDouble(float lastTime, float lastX, float lastY, float now, float x, float y, float maxSeconds, float maxDistance)
    {
        float elapsed = now - lastTime;
        if (!(elapsed >= 0f && elapsed <= maxSeconds))
            return false;

        float dx = x - lastX;
        float dy = y - lastY;
        return Math.Sqrt(dx * dx + dy * dy) <= maxDistance;
    }
}

/// <summary>
/// One target's clicks (a title bar): each click either completes a double
/// with the one before it (ClickTiming.IsDouble) or starts a new pair, so a
/// third quick click never counts as a second double.
/// </summary>
public sealed class DoubleClick
{
    private float _lastTime = ClickTiming.Never;
    private float _lastX;
    private float _lastY;

    /// <summary>Records a click; true when it completes a double.</summary>
    public bool Click(float now, float x, float y, float maxSeconds, float maxDistance)
    {
        if (ClickTiming.IsDouble(_lastTime, _lastX, _lastY, now, x, y, maxSeconds, maxDistance))
        {
            _lastTime = ClickTiming.Never;
            return true;
        }

        _lastTime = now;
        _lastX = x;
        _lastY = y;
        return false;
    }
}
