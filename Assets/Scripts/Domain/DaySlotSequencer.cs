using System;

/// <summary>
/// The day loop's per-slot state around closing time (pure; DayOrchestrator
/// drives it and owns the coroutine and events). A slot starts only while the
/// booth is open and the queue has travellers; closing either lets the current
/// slot finish (<see cref="CloseAfterCurrentSlot"/>) or abandons a slot whose
/// traveller was never called in (<see cref="CloseNow"/>).
/// </summary>
public sealed class DaySlotSequencer
{
    /// <summary>Queue size for the day (at least 1).</summary>
    public int TotalSlots { get; }

    /// <summary>1-based slot being run (TotalSlots + 1 once the queue is done).</summary>
    public int CurrentSlot { get; private set; } = 1;

    /// <summary>True once the booth closed: no further slot starts.</summary>
    public bool CloseRequested { get; private set; }

    /// <summary>True while the loop waits for the current slot's decision.</summary>
    public bool IsWaiting { get; private set; }

    /// <summary>True when the current slot was abandoned before its traveller was called in.</summary>
    public bool SlotAbandoned { get; private set; }

    /// <summary>True when the next slot may start (booth open, queue not empty).</summary>
    public bool CanStartSlot => !CloseRequested && CurrentSlot <= TotalSlots;

    /// <summary>Creates the sequencer for a day; a non-positive queue counts as one slot.</summary>
    public DaySlotSequencer(int totalSlots)
    {
        TotalSlots = Math.Max(1, totalSlots);
    }

    /// <summary>The current slot's traveller is being handled; wait for the decision.</summary>
    public void BeginWaiting()
    {
        IsWaiting = true;
        SlotAbandoned = false;
    }

    /// <summary>The current slot's decision is in.</summary>
    public void MarkResolved() => IsWaiting = false;

    /// <summary>Moves to the next slot after the current one fully ended.</summary>
    public void Advance() => CurrentSlot++;

    /// <summary>Closing with a traveller at the desk: finish this slot, start no other.</summary>
    public void CloseAfterCurrentSlot() => CloseRequested = true;

    /// <summary>Closing with nobody at the desk: release any wait and abandon that slot.</summary>
    public void CloseNow()
    {
        CloseRequested = true;
        if (IsWaiting)
        {
            IsWaiting = false;
            SlotAbandoned = true;
        }
    }
}
