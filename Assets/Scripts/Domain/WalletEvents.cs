using System;

/// <summary>
/// The wallet's changed signal (P0-A seam; owner UI-PC, BUG-58a): every write
/// to the wallet (WorldState.money) raises <see cref="Changed"/> right after
/// it, so the PC's views can follow the wallet live: the Citizen Account
/// redraws on it while it shows (BUG-58a); Orders follows its own signature.
/// </summary>
public static class WalletEvents
{
    /// <summary>Raised right after every write to the wallet.</summary>
    public static event Action Changed;

    /// <summary>Raises <see cref="Changed"/> (each wallet write calls it once, after the write).</summary>
    public static void Raise() => Changed?.Invoke();
}
