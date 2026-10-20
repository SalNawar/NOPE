using TMPro;
using UnityEngine;

/// <summary>
/// The AVAILABLE sign's look (Saleh 2026-09-30: "the button should read
/// available"): writes the caption (DeskConfigSO.readyCaptionKey) on the art's
/// NEXT sign label, or on the gameplay's stand-in sign when the art has none,
/// and lights it in the label's own ink while the desk is available; paused
/// (a break, after closing) it dims to DeskConfigSO.readyPausedInk, still
/// readable. Until its first press opens the shift (the clock waits for it,
/// Saleh 2026-10-07) the caption invites the press: lit and steady while the
/// morning briefing covers the office, then, from the first frame the sign
/// can be pressed (the booth leaves its Newsletter phase at Start Shift:
/// InvitePulse.Since), it pulses gently between the two inks for
/// InvitePulse.Cycles beats (DeskConfigSO.readyInviteSeconds a beat), then
/// settles lit and steady (HALL-FX, BUG-67: it used to pulse forever at the
/// edge of vision, and its beats must not run out behind the briefing;
/// steady lit at once under Reduced Motion). It follows
/// <see cref="DeskAvailability.Changed"/>, and runs a frame only while it
/// waits for the briefing to close or pulses. OfficeSceneBinder adds it to
/// the gameplay layer at load, so no art file is touched.
/// </summary>
[DisallowMultipleComponent]
public sealed class AvailableSignLink : MonoBehaviour
{
    private TMP_Text _caption;
    private DeskConfigSO _config;
    private DeskAvailability _desk;

    /// <summary>The gameplay layer's booth (its phase says when the briefing covers the office), or null.</summary>
    private BoothCoordinator _booth;

    /// <summary>The label's own ink (the lit sign).</summary>
    private Color _lit;

    /// <summary>When (unscaled seconds) the sign began inviting its press; NaN while it does not invite.</summary>
    private float _invitingSince = float.NaN;

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
        _invitingSince = float.NaN;
        _booth = BoothOf(gameObject.scene);
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

    /// <summary>The invitation's pulse: once the sign can be pressed, the caption breathes between the paused and the lit ink for a few beats, then holds lit (InvitePulse).</summary>
    private void Update()
    {
        if (_caption == null || _desk == null || !Inviting)
        {
            enabled = false;
            return;
        }
        _invitingSince = InvitePulse.Since(_invitingSince, true, Pressable, Time.unscaledTime);
        if (float.IsNaN(_invitingSince))
            return; // the briefing still covers the office: the sign holds lit and waits
        float beat = Mathf.Max(0.1f, _config.readyInviteSeconds);
        float elapsed = Time.unscaledTime - _invitingSince;
        bool reduced = MotionPreference.Reduced;
        _caption.color = Color.Lerp(_config.readyPausedInk, _lit, InvitePulse.Level(elapsed, beat, reduced));
        if (InvitePulse.Settled(elapsed, beat, reduced))
            enabled = false;
    }

    /// <summary>The sign can be pressed: no briefing (or shift report) covers the office (BoothPhase.Newsletter); true without a booth.</summary>
    private bool Pressable => _booth == null || _booth.Phase != BoothPhase.Newsletter;

    /// <summary>The booth in <paramref name="scene"/> (the gameplay layer's one), or null.</summary>
    private static BoothCoordinator BoothOf(UnityEngine.SceneManagement.Scene scene)
    {
        foreach (BoothCoordinator booth in FindObjectsByType<BoothCoordinator>())
            if (booth.gameObject.scene == scene)
                return booth;
        return null;
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

    /// <summary>The lit ink while the desk is available or the sign invites its first press, the paused ink otherwise; the invitation's pulse waits for a frame where the sign can be pressed (Update), never starting here at load.</summary>
    private void Apply()
    {
        if (_caption == null)
            return;
        bool inviting = Inviting;
        _invitingSince = InvitePulse.Since(_invitingSince, inviting, false, Time.unscaledTime);
        _caption.color = _desk.IsAvailable || inviting ? _lit : _config.readyPausedInk;
        enabled = inviting && (float.IsNaN(_invitingSince)
            || !InvitePulse.Settled(Time.unscaledTime - _invitingSince, Mathf.Max(0.1f, _config.readyInviteSeconds), MotionPreference.Reduced));
    }
}
