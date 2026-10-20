/// <summary>
/// The taskbar's clock (BUG-20a, UI-PC; Saleh's note 10: "when I said add the
/// date to the pc I meant like when you go inside on the start toolbar not a
/// massive box"): Windows' tray clock, the time over the date, at the
/// taskbar's right end. The time line is the shift clock's own
/// (ShiftClock.Format, written by ShiftClockReadouts with the office's clock,
/// so the two never disagree); the date line is today in the agency's
/// calendar, in capitals ("14 MAR 2150"). Pure.
/// </summary>
public static class TrayClock
{
    /// <summary>The date line for shift day <paramref name="day"/>: today in the agency's calendar in capitals ("14 MAR 2150"), or null when <paramref name="firstDate"/> is not a readable date (the caller writes the plain day).</summary>
    public static string Date(string firstDate, int day) => AgencyCalendar.Today(firstDate, day)?.ToUpperInvariant();
}
