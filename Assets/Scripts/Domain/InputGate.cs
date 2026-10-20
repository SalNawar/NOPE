/// <summary>
/// The game's input gate (P0-A seam; owner RUN-SAFE, BUG-68): while an
/// overlay that takes the input is open (the cheat panel), the office's input
/// consumers take none. OfficeControls, DeskInspect and DesktopWindowManager
/// check <see cref="Blocked"/> at the top of their input handlers. The P0 stub
/// never blocks (today's behaviour); RUN-SAFE gives it its overlay.
/// </summary>
public static class InputGate
{
    /// <summary>True while game input is blocked (an overlay holds it). The P0 stub: always false.</summary>
    public static bool Blocked => false;
}
