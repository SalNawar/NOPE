using TMPro;
using UnityEngine;

/// <summary>
/// Shows the shift clock as HH:MM in the desktop's taskbar tray, on the desk
/// CRT's clock plate (CrtClock) and on the
/// office's digital clock (the art office's own text, handed over by the
/// office binder through Bind, or the fallback HUD's). Polls the driver each
/// frame (as OfficeReadouts polls WorldState) and writes only when the minute
/// changes. It shows the driver's ShownMinute: the cheat menu's forced hour
/// while one is forced (GATE-22, HourOverride), else the clock's.
/// Every reference is optional and null-safe.
/// </summary>
public sealed class ShiftClockReadouts : MonoBehaviour
{
    /// <summary>Today's shift clock host.</summary>
    [SerializeField] private ShiftClockDriver driver;

    /// <summary>Tray clock text on the desktop's taskbar ("09:00").</summary>
    [SerializeField] private TMP_Text trayClockText;

    /// <summary>The desk CRT's clock plate's time ("09:00"; optional).</summary>
    [SerializeField] private TMP_Text crtClockText;

    /// <summary>The office's digital clock text.</summary>
    private TMP_Text _officeClockText;

    /// <summary>Whole minute last written (avoids a string per frame).</summary>
    private int _shownMinute = -1;

    /// <summary>Sets the office clock's text (the office binder: the art's digital clock, or the fallback HUD's).</summary>
    public void Bind(TMP_Text officeClock)
    {
        _officeClockText = officeClock;
        _shownMinute = -1;
    }

    private void Update()
    {
        if (driver == null || driver.Clock == null)
            return;

        float minute = driver.ShownMinute;
        int whole = Mathf.FloorToInt(minute);
        if (whole == _shownMinute)
            return;

        string text = ShiftClock.Format(minute);
        if (trayClockText != null)
            trayClockText.text = text;
        if (crtClockText != null)
            crtClockText.text = text;
        if (_officeClockText != null)
            _officeClockText.text = text;
        _shownMinute = whole;
    }
}
