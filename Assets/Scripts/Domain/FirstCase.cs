/// <summary>
/// The first traveller of the game (Saleh 2026-11-11: "the first case is not accept"): slot 1 of day 1 is an honest
/// traveller, like a traveller from an honest kind entry (FaultOrder), so the desk's tutorial teaches the approve path first.
/// Days 2 and later, and day 1's other slots, draw as before. Pure.
/// </summary>
public static class FirstCase
{
    /// <summary>True when slot <paramref name="slot"/> (1-based) of day <paramref name="day"/> must be clean: <paramref name="knob"/> on (GameConfigSO.firstCaseClean), day 1, slot 1.</summary>
    public static bool IsClean(int day, int slot, bool knob) => knob && day == 1 && slot == 1;
}
