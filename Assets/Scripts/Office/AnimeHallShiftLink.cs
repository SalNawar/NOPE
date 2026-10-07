using TMPro;
using UnityEngine;

/// <summary>
/// The anime hall's time hook (docs/SCENE_CONTRACT_GAMEPLAY.md): drives the
/// hall from the gameplay's shift clock through the read-only hook
/// <see cref="ShiftClockDriver.Live"/>. With the hall's lights
/// (HallLightingRig, docs/HALL_LIGHTING.md) it hands them the clock's minute,
/// so their day-night cycle shows the shift's hour, and gives the art side's
/// <see cref="AnimeHallPresentation.SetTime"/> (its daylight, which lights
/// the preserved 3D desk) the cycle's evening, and the traveller's figure the
/// cycle's shade over DeskConfigSO.travellerTint. Without them (or with them
/// switched off) the evening follows the old office's crowd curve
/// (<see cref="CrowdPaletteBlend"/>: morning until DeskConfigSO's hall start,
/// full evening from its full point, eased between) on the clock's hour as it
/// stands on the standard day (<see cref="IShiftProgress.StandardProgress01"/>),
/// so a late shift opens in the evening.
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
    private HallLightingRig _lights;
    private TravellerView _traveller;

    /// <summary>The evening blend last written to the hall (-1 before the first write).</summary>
    private float _applied = -1f;

    /// <summary>The traveller's shade last written (clear before the first write).</summary>
    private Color _shade = Color.clear;

    /// <summary>
    /// Points the link at the hall's presentation, the art's calendar text
    /// (null: none), the hall's lights (null: none) and the traveller (null:
    /// none), with the config's hall knobs (the evening's curve without the
    /// lights, the calendar's inks and their switch, the traveller's tint).
    /// </summary>
    public void Configure(AnimeHallPresentation hall, DeskConfigSO config, TMP_Text calendar, HallLightingRig lights, TravellerView traveller)
    {
        _hall = hall;
        _config = config;
        _calendar = calendar;
        _lights = lights;
        _traveller = traveller;
        _applied = -1f;
        _shade = Color.clear;
    }

    private void Update()
    {
        IShiftProgress shift = ShiftClockDriver.Live;
        if (_hall == null || _config == null || shift == null)
            return;

        bool lit = _lights != null && _lights.isActiveAndEnabled && _lights.Settings != null && _lights.Settings.lightingOn;
        if (lit)
            _lights.SetClock(shift.MinuteOfDay);
        float blend = lit ? _lights.Evening : CrowdPaletteBlend.Evening(shift.StandardProgress01, _config.hallEveningStartsAt, _config.hallEveningFullAt);
        Color shade = lit ? _lights.TravellerShade : Color.white;

        if (_traveller != null && shade != _shade)
        {
            _shade = shade;
            _traveller.Tint(_config.travellerTint * shade);
        }

        if (Mathf.Approximately(_applied, blend))
            return;

        _applied = blend;
        _hall.SetTime(blend);
        if (_calendar != null)
            _calendar.color = CrowdPaletteBlend.LightInk(blend, _config.hallCalendarEveningInkFrom) ? _config.hallCalendarEveningInk : _config.hallCalendarDayInk;
    }
}
