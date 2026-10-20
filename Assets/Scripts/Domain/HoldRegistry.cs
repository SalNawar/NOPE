/// <summary>Something the player can hold on the desk (a paper, the folder, a stamp, a tool): the hold registry lets go of it.</summary>
public interface IHoldable
{
    /// <summary>Lets go of the hold now (a cancelled drag: back where it was picked up).</summary>
    void CancelHold();
}

/// <summary>
/// The desk's one current hold (P0-A seam; owner DOC-HANDLE): what the player
/// holds now, if anything. A new hold replaces the one before it (cancelling
/// it), <see cref="Cancel"/> lets go, and losing the focus
/// (<see cref="OnFocusLost"/>) cancels the hold. The P0 stub tracks a hold;
/// nothing registers one yet (today's behaviour).
/// </summary>
public sealed class HoldRegistry
{
    /// <summary>The desk's registry (the holds of every desk object).</summary>
    public static HoldRegistry Desk { get; } = new HoldRegistry();

    /// <summary>What is held now (null: nothing).</summary>
    public IHoldable Current { get; private set; }

    /// <summary>True while something is held.</summary>
    public bool IsHolding => Current != null;

    /// <summary><paramref name="holdable"/> is picked up: another held thing is cancelled first.</summary>
    public void Begin(IHoldable holdable)
    {
        if (Current != null && Current != holdable)
            Current.CancelHold();
        Current = holdable;
    }

    /// <summary><paramref name="holdable"/> was let go by its holder (a drop): forgotten without a cancel.</summary>
    public void End(IHoldable holdable)
    {
        if (Current == holdable)
            Current = null;
    }

    /// <summary>Lets go of what is held (it cancels its hold); nothing when nothing is held.</summary>
    public void Cancel()
    {
        IHoldable held = Current;
        Current = null;
        held?.CancelHold();
    }

    /// <summary>The game lost the focus (the window, a pause): the hold is cancelled.</summary>
    public void OnFocusLost() => Cancel();
}
