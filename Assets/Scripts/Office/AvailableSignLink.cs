using TMPro;
using UnityEngine;

/// <summary>
/// The AVAILABLE sign's look (Saleh 2026-09-30: "the button should read
/// available"): writes the caption (DeskConfigSO.readyCaptionKey) on the art's
/// NEXT sign label, or on the gameplay's stand-in sign when the art has none,
/// and lights it in the label's own ink while the desk is available; paused
/// (a break, after closing) it dims to DeskConfigSO.readyPausedInk, still
/// readable. Until its first press opens the shift (the clock waits for it,
/// Saleh 2026-10-07) the caption invites the press: it pulses gently between
/// the two inks (DeskConfigSO.readyInviteSeconds a beat; steady lit under
/// Reduced Motion). It follows <see cref="DeskAvailability.Changed"/>, and
/// runs a frame only while it invites. OfficeSceneBinder adds it to the
/// gameplay layer at load, so no art file is touched.
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
        PrintCaption();
        CultureThemeService.LabelsChanged += PrintCaption;
        _desk.Changed += Apply;
        Apply();
    }

    /// <summary>The caption's words in the reading language (again whenever the labels' language changes).</summary>
    private void PrintCaption()
    {
        if (_caption != null && _config != null && !string.IsNullOrWhiteSpace(_config.readyCaptionKey))
            _caption.text = UiText.Get(_config.readyCaptionKey);
    }

    /// <summary>The invitation's pulse: the caption breathes between the paused and the lit ink until the shift opens or the desk closes.</summary>
    private void Update()
    {
        if (_caption == null || _desk == null || !Inviting)
        {
            enabled = false;
            return;
        }
        float beat = Mathf.Max(0.1f, _config.readyInviteSeconds);
        float t = MotionPreference.Reduced ? 1f : 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI / beat);
        _caption.color = Color.Lerp(_config.readyPausedInk, _lit, t);
    }

    /// <summary>True until the sign's first press opens the shift (and not once the desk is closed).</summary>
    private bool Inviting => !_desk.HasOpened && !_desk.IsClosed;

    private void OnDestroy() => Unhook();

    private void Unhook()
    {
        CultureThemeService.LabelsChanged -= PrintCaption;
        if (_desk != null)
            _desk.Changed -= Apply;
    }

    /// <summary>The lit ink while the desk is available, the paused ink otherwise; the pulse runs while the sign invites its first press.</summary>
    private void Apply()
    {
        if (_caption != null)
            _caption.color = _desk.IsAvailable ? _lit : _config.readyPausedInk;
        enabled = _caption != null && Inviting;
    }
}
