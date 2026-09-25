using System;

/// <summary>Why a dark screen may wake itself.</summary>
public enum WakeReason
{
    /// <summary>A traveller was presented at the desk.</summary>
    TravellerPresented,

    /// <summary>A desk scan finished (its scanned window has just opened on the PC).</summary>
    ScanFinished
}

/// <summary>Which events turn a dark screen on (a knob group held by DeskConfigSO).</summary>
[Serializable]
public sealed class PcWakeRules
{
    /// <summary>A newly presented traveller turns a dark screen on.</summary>
    public bool onTravellerPresented = true;

    /// <summary>A finished desk scan turns a dark screen on.</summary>
    public bool onScanFinished = true;
}

/// <summary>
/// The PC's screen power: only the display and the desktop's input go dark,
/// the PC keeps running. The screen can be toggled (the bezel button) or
/// turned off (Start > Turn off screen), wakes itself on the enabled reasons,
/// and a held screen is always on (a pending citation slip): holding lights
/// it, and while held nothing turns it off. Pure, so every rule is tested headless.
/// </summary>
public sealed class PcScreen
{
    /// <summary>The wake rules (null wakes on nothing).</summary>
    private readonly PcWakeRules _rules;

    /// <summary>True while a citation slip holds the screen on.</summary>
    private bool _held;

    /// <summary>Creates a screen that starts on or off; a null <paramref name="rules"/> never wakes.</summary>
    public PcScreen(bool startsOn, PcWakeRules rules)
    {
        IsOn = startsOn;
        _rules = rules;
    }

    /// <summary>True while the screen is on.</summary>
    public bool IsOn { get; private set; }

    /// <summary>Raised after the screen turns on or off (only on a real change).</summary>
    public event Action Changed;

    /// <summary>Flips the screen and returns true; while held it changes nothing and returns false.</summary>
    public bool Toggle()
    {
        if (_held)
            return false;

        Set(!IsOn);
        return true;
    }

    /// <summary>Turns the screen off; returns true when it did (false when it was off, or held).</summary>
    public bool TurnOff()
    {
        if (_held || !IsOn)
            return false;

        Set(false);
        return true;
    }

    /// <summary>Turns a dark screen on when the reason's rule is on; returns true when it did. An unknown reason never wakes.</summary>
    public bool Wake(WakeReason reason)
    {
        if (IsOn || !RuleOn(reason))
            return false;

        Set(true);
        return true;
    }

    /// <summary>A held screen is always on (a pending citation slip): holding turns a dark screen on; while held, Toggle and TurnOff refuse; releasing changes nothing else.</summary>
    public void SetHeld(bool held)
    {
        _held = held;
        if (held && !IsOn)
            Set(true);
    }

    /// <summary>True when the wake rules let this reason wake the screen.</summary>
    private bool RuleOn(WakeReason reason)
    {
        if (_rules == null)
            return false;

        switch (reason)
        {
            case WakeReason.TravellerPresented: return _rules.onTravellerPresented;
            case WakeReason.ScanFinished: return _rules.onScanFinished;
            default: return false;
        }
    }

    /// <summary>Changes the state and raises Changed (callers only pass a real change).</summary>
    private void Set(bool on)
    {
        IsOn = on;
        Changed?.Invoke();
    }
}
