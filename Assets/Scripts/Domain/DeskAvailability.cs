using System;

/// <summary>
/// The desk's AVAILABLE sign (Saleh, 2026-09-30: "the button should read
/// available and you only need to click it once and cases will come one after
/// another if you click it again it will pause"): a toggle, off when the shift
/// starts. While it is on, the traveller waiting in the queue is called as
/// soon as the desk is free: one is waiting (their slot started: Arm) and the
/// last one has left (their reaction's linger is over: SetDeparting(false)); a
/// citation slip holds the next slot itself, so it holds the call too. The
/// sign's first turn on opens the shift (Saleh 2026-10-07: "shift should not
/// start until you press AVAILABLE"): Opened is raised once and the shift
/// clock starts from it; later presses only pause and resume. Turned
/// off, the traveller at the desk is finished normally and nobody new is
/// called until it is turned on again (the shift clock never stops for it: a
/// break is time off the queue, not off the clock). Closing time and the day's
/// end close it: nobody is called any more and the sign stays off. GameManager
/// drives it; pure, so every transition is tested (DeskAvailabilityTests).
/// </summary>
public sealed class DeskAvailability
{
    /// <summary>The sign is on: travellers are called one after another.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>A traveller waits in the queue for the desk (their slot started and they have not been called).</summary>
    public bool IsWaiting { get; private set; }

    /// <summary>The last traveller has not left yet (their reaction lingers at the desk).</summary>
    public bool IsDeparting { get; private set; }

    /// <summary>The shift is over (closing time or the day's end): nobody is called and the sign no longer turns on.</summary>
    public bool IsClosed { get; private set; }

    /// <summary>The sign has been turned on once: the shift is open (its clock runs from then).</summary>
    public bool HasOpened { get; private set; }

    /// <summary>Raised once, at the sign's first turn on, before the waiting traveller is called: the shift opens and its clock starts.</summary>
    public event Action Opened;

    /// <summary>Raised when the waiting traveller is called to the desk (at most once per Arm).</summary>
    public event Action Called;

    /// <summary>Raised after <see cref="IsAvailable"/> changes (the sign lights up or dims).</summary>
    public event Action Changed;

    /// <summary>The sign's click: turns the desk available (the first time, opening the shift: Opened; then calling the waiting traveller at once when the desk is free) or pauses it; nothing once closed.</summary>
    public void Toggle()
    {
        if (IsClosed)
            return;

        IsAvailable = !IsAvailable;
        if (IsAvailable && !HasOpened)
        {
            HasOpened = true;
            Opened?.Invoke();
        }
        Changed?.Invoke();
        TryCall();
    }

    /// <summary>A traveller's slot started: they wait for the desk (called at once when it is available and free); nothing once closed.</summary>
    public void Arm()
    {
        if (IsClosed)
            return;

        IsWaiting = true;
        TryCall();
    }

    /// <summary>The last traveller starts to leave (true: their reaction lingers) or has left (false: the desk is free again).</summary>
    public void SetDeparting(bool departing)
    {
        IsDeparting = departing;
        TryCall();
    }

    /// <summary>Closing time or the day's end: a waiting traveller is never called and the sign goes off for good.</summary>
    public void Close()
    {
        bool wasAvailable = IsAvailable;
        IsClosed = true;
        IsWaiting = false;
        IsAvailable = false;
        if (wasAvailable)
            Changed?.Invoke();
    }

    /// <summary>Calls the waiting traveller when the desk is available and free.</summary>
    private void TryCall()
    {
        if (!IsAvailable || !IsWaiting || IsDeparting)
            return;

        IsWaiting = false;
        Called?.Invoke();
    }
}
