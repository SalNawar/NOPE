using UnityEngine;

/// <summary>A game-feel moment that punches (FeelDirector.Punch).</summary>
public enum FeelHit
{
    /// <summary>A stamp lands on the passport: a hit-stop and a small camera bump.</summary>
    Stamp,

    /// <summary>A citation is issued: a hit-stop, a medium camera shake, citation_hit.</summary>
    Citation,

    /// <summary>A famous traveller is let through: a hit-stop and famous_pass.</summary>
    Famous,

    /// <summary>The Helix River breaches (stability crosses into its warning or critical band): a large rumble and helix_breach.</summary>
    Breach
}

/// <summary>
/// The office's game-feel director (Saleh 2026-10-07): the HIT-STOP (gameplay
/// time drops to MotionKnobs.hitStopScale for hitStopSeconds of real time when
/// a stamp lands, a citation is issued or a famous traveller is let through;
/// the UI, the springs and the camera's impulses run on unscaled time, and no
/// rule reads the frame's length, so outcomes never change), the camera's
/// impulses through Cinemachine (CameraFeel: small for a stamp, medium for a
/// citation, large when the Helix River breaches), the shift's bells
/// (last_hour_alarm as the day's last hour begins, ShiftBells; shift_end_bell
/// at closing time), the PA chime as an accepted traveller's portal is
/// announced, and the hall's ambience (HallAmbience from the clock's minute
/// and the weather's rain, set on Sounds twice a second; silent when the
/// office goes). Made by GameManager at the shift's start (Attach); one per
/// office.
/// </summary>
public sealed class FeelDirector : MonoBehaviour
{
    /// <summary>The seconds between two looks at the hall's state for the ambience.</summary>
    private const float AmbienceEvery = 0.5f;

    private static FeelDirector _current;

    private GameManager _game;
    private ShiftClockDriver _clock;
    private HallCityExterior _city;
    private bool _cityLooked;
    private float _lastMinute = -1f, _nextAmbience, _stopUntil;
    private bool _stopped;

    /// <summary>The office's director (null outside the office).</summary>
    public static FeelDirector Current => _current;

    /// <summary>True while a hit-stop holds gameplay time down.</summary>
    public bool Stopped => _stopped;

    /// <summary>Gives <paramref name="game"/>'s object the office's director, listening to its decisions and <paramref name="clock"/>'s shift.</summary>
    public static FeelDirector Attach(GameManager game, ShiftClockDriver clock)
    {
        if (game == null)
            return null;
        if (!game.TryGetComponent(out FeelDirector director))
            director = game.gameObject.AddComponent<FeelDirector>();
        director.Listen(game, clock);
        return director;
    }

    /// <summary>A game-feel moment: its hit-stop, camera impulse and sound (nothing without the office's director).</summary>
    public static void Punch(FeelHit hit)
    {
        if (_current != null)
            _current.Play(hit);
    }

    /// <summary>
    /// A generic punch of <paramref name="strength"/> (0 to 1, clamped; the shared
    /// API the desk machines call): above 0 a hit-stop, and a camera bump whose
    /// force is the strength times MotionKnobs.shakeBreach (the largest shake),
    /// a rumble from 0.75 up. Nothing without the office's director.
    /// </summary>
    public static void Hit(float strength)
    {
        strength = Mathf.Clamp01(strength);
        if (_current == null || strength <= 0f)
            return;
        MotionKnobs knobs = UiMotion.Knobs;
        _current.HitStop(knobs);
        CameraFeel.Shake(knobs.shakeBreach * strength, Mathf.Lerp(knobs.shakeStampSeconds, knobs.shakeBreachSeconds, strength), strength >= 0.75f);
    }

    private void Listen(GameManager game, ShiftClockDriver clock)
    {
        Unlisten();
        _current = this;
        _game = game;
        _clock = clock;
        _game.Resolved += HandleResolved;
        _game.Departed += HandleDeparted;
        if (_clock != null)
            _clock.Closed += HandleClosed;
    }

    private void Unlisten()
    {
        if (_game != null)
        {
            _game.Resolved -= HandleResolved;
            _game.Departed -= HandleDeparted;
        }
        if (_clock != null)
            _clock.Closed -= HandleClosed;
    }

    private void OnDestroy()
    {
        Unlisten();
        if (_stopped)
            Time.timeScale = 1f;
        Sounds.SetBeds(null);
        if (_current == this)
            _current = null;
    }

    /// <summary>A decision: a citation's punch, a famous traveller's, and the river's breach when stability crossed into its warning or critical band.</summary>
    private void HandleResolved(CaseInstance inst, CaseVerdict verdict, bool accepted, float stabilityBefore)
    {
        if (verdict != null && verdict.citationIssued)
            Play(FeelHit.Citation);
        if (accepted && inst != null && inst.IsFamous)
            Play(FeelHit.Famous);
        GameConfigSO config = _game.Config;
        WorldState world = _game.World;
        if (config == null || world == null)
            return;
        StabilityTier before = HelixRiver.Tier(stabilityBefore, config.firedAtStability, config.stabilityWarningMargin, config.stabilityCriticalMargin, null);
        StabilityTier after = HelixRiver.Tier(world.timelineStability, config.firedAtStability, config.stabilityWarningMargin, config.stabilityCriticalMargin, null);
        if (after > before && after >= StabilityTier.Breaching)
            Play(FeelHit.Breach);
    }

    /// <summary>An accepted traveller's portal is announced in the hall: the PA chime (the hall's ambience ducks under it).</summary>
    private void HandleDeparted(int portal) => Sounds.Play(SoundCues.PaChime);

    /// <summary>Closing time: the shift's end bell.</summary>
    private void HandleClosed() => Sounds.Play(SoundCues.ShiftEndBell);

    /// <summary>One moment's hit-stop, camera impulse and sound.</summary>
    private void Play(FeelHit hit)
    {
        MotionKnobs knobs = UiMotion.Knobs;
        switch (hit)
        {
            case FeelHit.Stamp:
                HitStop(knobs);
                CameraFeel.Shake(knobs.shakeStamp, knobs.shakeStampSeconds, false);
                break;
            case FeelHit.Citation:
                HitStop(knobs);
                CameraFeel.Shake(knobs.shakeCitation, knobs.shakeCitationSeconds, false);
                Sounds.Play(SoundCues.CitationHit);
                break;
            case FeelHit.Famous:
                HitStop(knobs);
                Sounds.Play(SoundCues.FamousPass);
                break;
            case FeelHit.Breach:
                CameraFeel.Shake(knobs.shakeBreach, knobs.shakeBreachSeconds, true);
                Sounds.Play(SoundCues.HelixBreach);
                break;
        }
    }

    /// <summary>Drops gameplay time to hitStopScale for hitStopSeconds of real time (a second one extends it; none without motion).</summary>
    private void HitStop(MotionKnobs knobs)
    {
        if (UiMotion.Amount.Still || knobs.hitStopSeconds <= 0f)
            return;
        _stopUntil = Mathf.Max(_stopUntil, Time.unscaledTime + knobs.hitStopSeconds);
        _stopped = true;
        Time.timeScale = Mathf.Clamp(knobs.hitStopScale, 0f, 1f);
    }

    /// <summary>Ends a hit-stop on time, rings the last hour as the clock crosses into it, and sets the hall's ambience twice a second.</summary>
    private void Update()
    {
        if (_stopped && Time.unscaledTime >= _stopUntil)
        {
            _stopped = false;
            Time.timeScale = 1f;
        }
        if (_clock == null || _clock.Clock == null)
            return;
        float minute = _clock.MinuteOfDay;
        SoundBankSO bank = Sounds.Bank;
        if (_lastMinute >= 0f && ShiftBells.LastHourBegan(_lastMinute, minute, _clock.Hours, bank != null ? bank.lastHourMinutes : 60))
            Sounds.Play(SoundCues.LastHourAlarm);
        _lastMinute = minute;
        if (bank == null || Time.unscaledTime < _nextAmbience)
            return;
        _nextAmbience = Time.unscaledTime + AmbienceEvery;
        if (!_cityLooked)
        {
            _cityLooked = true;
            _city = FindAnyObjectByType<HallCityExterior>();
        }
        Sounds.SetBeds(HallAmbience.For(minute, _city != null ? _city.rain : 0f, bank.nightFromMinute, bank.dayFromMinute, bank.bedFadeMinutes, bank.rushWindows));
    }
}
