using UnityEngine;

/// <summary>
/// Retired (BUG-20a, Saleh's note 10: "when I said add the date to the pc I
/// meant like when you go inside on the start toolbar not a massive box"):
/// the desk CRT's clock plate is gone; the PC's clock is the taskbar's tray
/// clock, the time over the date (TrayClock). The component stays an empty,
/// never-shown marker only while other tracks' builder lines (DESK-PLAN's
/// .Desk.cs, the root's crtDateText wiring) still name it; INTEGRATION
/// deletes it with them (docs/pending/UI-PC.md, the cleanup list).
/// </summary>
public sealed class CrtClock : MonoBehaviour
{
}
