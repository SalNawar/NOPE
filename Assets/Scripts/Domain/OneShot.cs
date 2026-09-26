using System;

/// <summary>
/// A callback that fires once (audit R4-025: hide the panel, then fire its
/// callback once). The slot is emptied before the callback runs, so the
/// callback may fill it again (a decision presenting the next traveller at
/// once). Pure.
/// </summary>
public static class OneShot
{
    /// <summary>Takes the callback out of <paramref name="slot"/> and invokes it with <paramref name="value"/> (an empty slot does nothing).</summary>
    public static void Fire<T>(ref Action<T> slot, T value)
    {
        Action<T> callback = slot;
        slot = null;
        callback?.Invoke(value);
    }
}
