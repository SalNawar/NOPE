using TMPro;
using UnityEngine;

/// <summary>
/// The AVAILABLE sign's look (Saleh 2026-09-30: "the button should read
/// available"): writes the caption (DeskConfigSO.readyCaptionKey) on the art's
/// NEXT sign label, or on the gameplay's stand-in sign when the art has none,
/// and lights it in the label's own ink while the desk is available; paused
/// (the shift's start, a break, after closing) it dims to
/// DeskConfigSO.readyPausedInk, still readable. It follows
/// <see cref="DeskAvailability.Changed"/>, so it never polls. OfficeSceneBinder
/// adds it to the gameplay layer at load, so no art file is touched.
/// </summary>
[DisallowMultipleComponent]
public sealed class AvailableSignLink : MonoBehaviour
{
    private TMP_Text _caption;
    private DeskConfigSO _config;
    private DeskAvailability _desk;

    /// <summary>The label's own ink (the lit sign).</summary>
    private Color _lit;

    /// <summary>Points the link at the sign's label and the desk's availability, writes the caption and shows the current state.</summary>
    public void Configure(TMP_Text caption, DeskConfigSO config, DeskAvailability desk)
    {
        Unhook();
        _caption = caption;
        _config = config;
        _desk = desk;
        if (_caption == null || _config == null || _desk == null)
            return;

        _lit = _caption.color;
        if (!string.IsNullOrWhiteSpace(_config.readyCaptionKey))
            _caption.text = UiText.Get(_config.readyCaptionKey);
        _desk.Changed += Apply;
        Apply();
    }

    private void OnDestroy() => Unhook();

    private void Unhook()
    {
        if (_desk != null)
            _desk.Changed -= Apply;
    }

    /// <summary>The lit ink while the desk is available, the paused ink otherwise.</summary>
    private void Apply()
    {
        if (_caption != null)
            _caption.color = _desk.IsAvailable ? _lit : _config.readyPausedInk;
    }
}
