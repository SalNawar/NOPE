using TMPro;
using UnityEngine;

/// <summary>
/// Shows the shift clock: HH:MM in the monitor's taskbar tray and the hands of
/// the booth's wall clock. Polls the driver each frame (as OfficeReadouts polls
/// WorldState). Every reference is optional and null-safe.
/// </summary>
public sealed class ShiftClockReadouts : MonoBehaviour
{
    /// <summary>Today's shift clock host.</summary>
    [SerializeField] private ShiftClockDriver driver;

    [Header("Monitor taskbar tray")]
    /// <summary>Tray clock text ("09:00").</summary>
    [SerializeField] private TMP_Text trayClockText;

    [Header("Booth wall clock")]
    /// <summary>Hour hand; its pivot must sit at the dial centre, pointing to 12 at rotation 0.</summary>
    [SerializeField] private Transform hourHand;

    /// <summary>Minute hand; same pivot rule as the hour hand.</summary>
    [SerializeField] private Transform minuteHand;

    /// <summary>Whole minute last written to the tray (avoids a string per frame).</summary>
    private int _shownMinute = -1;

    private void Update()
    {
        if (driver == null || driver.Clock == null)
            return;

        Apply(driver.Clock.CurrentMinute);
    }

    /// <summary>Shows a minute of day on every wired readout.</summary>
    public void Apply(float minuteOfDay)
    {
        (float hourDegrees, float minuteDegrees) = ShiftClock.HandAngles(minuteOfDay);

        // Unity's z rotation turns counter-clockwise; clock hands turn clockwise.
        if (hourHand != null)
            hourHand.localRotation = Quaternion.Euler(0f, 0f, -hourDegrees);
        if (minuteHand != null)
            minuteHand.localRotation = Quaternion.Euler(0f, 0f, -minuteDegrees);

        int whole = Mathf.FloorToInt(minuteOfDay);
        if (trayClockText != null && whole != _shownMinute)
        {
            trayClockText.text = ShiftClock.Format(minuteOfDay);
            _shownMinute = whole;
        }
    }
}
