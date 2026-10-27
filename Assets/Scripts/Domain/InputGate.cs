using System.Collections.Generic;

/// <summary>
/// The game's input gate (P0-A seam; owner RUN-SAFE, BUG-68): while an
/// overlay that takes the input is open (the cheat panel), the office's input
/// consumers take none. OfficeControls, DeskInspect and DesktopWindowManager
/// check <see cref="Blocked"/> at the top of their input handlers. Each
/// overlay holds the gate under its own name while it is open and lets it go
/// when it closes (or is destroyed), so one overlay never releases another's
/// hold. Pure, so it is tested headless.
/// </summary>
public static class InputGate
{
    /// <summary>Who holds the gate now (each an overlay's name).</summary>
    private static readonly HashSet<string> Holders = new HashSet<string>();

    /// <summary>True while game input is blocked: at least one overlay holds the gate.</summary>
    public static bool Blocked => Holders.Count > 0;

    /// <summary><paramref name="holder"/> takes the input (DebugPanelController while the cheat panel is open). Holding twice is one hold; an unnamed holder is ignored.</summary>
    public static void Hold(string holder)
    {
        if (!string.IsNullOrEmpty(holder))
            Holders.Add(holder);
    }

    /// <summary><paramref name="holder"/> gives the input back; the game takes it again once nobody holds the gate. Releasing a hold never taken does nothing.</summary>
    public static void Release(string holder)
    {
        if (!string.IsNullOrEmpty(holder))
            Holders.Remove(holder);
    }
}
