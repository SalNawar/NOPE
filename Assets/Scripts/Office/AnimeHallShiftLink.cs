using TMPro;
using UnityEngine;

/// <summary>
/// The anime hall's time hook (docs/SCENE_CONTRACT_GAMEPLAY.md): drives the
/// art side's <see cref="AnimeHallPresentation.SetTime"/> (its daylight and
/// ambient, morning to evening) from the gameplay's shift clock through the
/// read-only hook <see cref="ShiftClockDriver.Live"/>, along the same curve
/// as the old office's crowds (<see cref="CrowdPaletteBlend"/>: morning until
/// DeskConfigSO's hall start, full evening from its full point, eased between).
/// The calendar's date is printed on the art's paper, which the evening dims to
/// nearly black, so its ink follows the same blend: the config's day ink, then
/// its evening ink from the config's point (CrowdPaletteBlend.LightInk), and it
/// reads 4.5:1 or better all day as drawn; the text stays the readouts'. The
/// other boards' digits are light on dark glass and keep their own colour.
/// OfficeSceneBinder adds it to the gameplay layer at load when the art office
/// carries a presentation, so no art file is touched. Without a gameplay clock
/// (the art office on its own, edit mode) it writes nothing and the hall keeps
/// the time and the ink its art authored; the presentation's pan (SetPan) is the art's.
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimeHallShiftLink : MonoBehaviour
{
    private AnimeHallPresentation _hall;
    private DeskConfigSO _config;
    private TMP_Text _calendar;

    /// <summary>The evening blend last written to the hall (-1 before the first write).</summary>
    private float _applied = -1f;

    /// <summary>Points the link at the hall's presentation and the art's calendar text (null: none), with the config's hall knobs (the evening's curve, the calendar's inks and their switch).</summary>
    public void Configure(AnimeHallPresentation hall, DeskConfigSO config, TMP_Text calendar)
    {
        _hall = hall;
        _config = config;
        _calendar = calendar;
        _applied = -1f;
    }

    private void Update()
    {
        IShiftProgress shift = ShiftClockDriver.Live;
        if (_hall == null || _config == null || shift == null)
            return;

        float blend = CrowdPaletteBlend.Evening(shift.Progress01, _config.hallEveningStartsAt, _config.hallEveningFullAt);
        if (Mathf.Approximately(_applied, blend))
            return;

        _applied = blend;
        _hall.SetTime(blend);
        if (_calendar != null)
            _calendar.color = CrowdPaletteBlend.LightInk(blend, _config.hallCalendarEveningInkFrom) ? _config.hallCalendarEveningInk : _config.hallCalendarDayInk;
    }
}
