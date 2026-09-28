using UnityEngine;

/// <summary>
/// The anime hall's time hook (docs/SCENE_CONTRACT_GAMEPLAY.md): drives the
/// art side's <see cref="AnimeHallPresentation.SetTime"/> (its daylight and
/// ambient, morning to evening) from the gameplay's shift clock through the
/// read-only hook <see cref="ShiftClockDriver.Live"/>, along the same curve
/// as the old office's crowds (<see cref="CrowdPaletteBlend"/>: morning until
/// DeskConfigSO's hall start, full evening from its full point, eased between).
/// OfficeSceneBinder adds it to the gameplay layer at load when the art office
/// carries a presentation, so no art file is touched. Without a gameplay clock
/// (the art office on its own, edit mode) it writes nothing and the hall keeps
/// the time its art authored; the presentation's pan (SetPan) is the art's.
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimeHallShiftLink : MonoBehaviour
{
    private AnimeHallPresentation _hall;
    private float _eveningStartsAt = 0.5f;
    private float _eveningFullAt = 0.9f;

    /// <summary>The evening blend last written to the hall (-1 before the first write).</summary>
    private float _applied = -1f;

    /// <summary>Points the link at the hall's presentation and sets its curve (shift progress at which the evening starts and at which it is full, 0..1).</summary>
    public void Configure(AnimeHallPresentation hall, float eveningStartsAt, float eveningFullAt)
    {
        _hall = hall;
        _eveningStartsAt = eveningStartsAt;
        _eveningFullAt = eveningFullAt;
        _applied = -1f;
    }

    private void Update()
    {
        IShiftProgress shift = ShiftClockDriver.Live;
        if (_hall == null || shift == null)
            return;

        float blend = CrowdPaletteBlend.Evening(shift.Progress01, _eveningStartsAt, _eveningFullAt);
        if (Mathf.Approximately(_applied, blend))
            return;

        _applied = blend;
        _hall.SetTime(blend);
    }
}
