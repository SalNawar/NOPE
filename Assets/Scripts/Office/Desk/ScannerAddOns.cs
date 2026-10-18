using UnityEngine;

/// <summary>
/// The scanner's two upgrades as objects on it (the desk machines spec §4,
/// Saleh 2026-10-17: "every upgrade should have a visual representation on
/// the desk" "and a satysfing interaction"): the Auto-Feed tray on its left
/// with the Auto-Feed Scanner, the Analysis beacon on its right with the
/// Analysis Scanner (ScannerDay, fixed at day start; DeskScanner.ShowUpgrades
/// shows them). A paper the Auto-Feed takes in runs its rollers with their
/// zip (adf_feed) and blinks its green feed lamp; the rollers turn while any
/// scan runs with the tray fitted. An analysis pass that marks a pair pops
/// the beacon's pod up (beacon_pop), lights its amber lamp and spins its
/// mirror for a few seconds, then it sinks back; a pass that marks nothing
/// pops it half way, unlit, and back. Their functions (Saleh 2026-10-18, demo
/// 1018a note 14: "every object the playyer unlocks should be useable"; the
/// desk machines spec §10): a click on the tray feeds the stack in one go
/// (DeskController.FeedStack), the guides' snap and the rollers' whirr, or
/// only a dry whirr with nothing left to scan; a click on the beacon jumps
/// to the pair the last analysis marked (DeskController.ShowFlagged): the
/// pod pops lit again. Built from the props' models by Build Office UI
/// (tools/props/machines/upgrade_props_model.py), each with its click box.
/// </summary>
public sealed class ScannerAddOns : MonoBehaviour
{
    /// <summary>The tray's and the beacon's click boxes (children of each object: hidden with it).</summary>
    [SerializeField] private Clickable trayClick, beaconClick;

    /// <summary>Raised when the tray is clicked (DeskScanner passes it on to the desk: feed the stack).</summary>
    public event System.Action TrayClicked;

    /// <summary>Raised when the beacon is clicked (DeskScanner passes it on to the desk: show the flagged pair).</summary>
    public event System.Action BeaconClicked;

    /// <summary>The Auto-Feed tray's rollers (they turn about their local Z) and its feed lamp.</summary>
    [SerializeField] private Transform rollers;

    /// <inheritdoc cref="rollers"/>
    [SerializeField] private Renderer feedLed;

    /// <summary>The beacon's pod (it pops up along its local Y), its mirror (it spins about Y) and its lamp.</summary>
    [SerializeField] private Transform beaconPod, reflector;

    /// <inheritdoc cref="beaconPod"/>
    [SerializeField] private Renderer beaconLamp;

    /// <summary>The cues' placeholder source.</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>How far the pod pops (metres, model space), how long it stays up after a mark, the mirror's spin (degrees a second) and the rollers' (degrees a second).</summary>
    [SerializeField] private float podLift = 0.03f, beaconSeconds = 2.6f, beaconSpin = 540f, rollerSpin = 720f;

    private static readonly Color FeedOn = new Color(0.24f, 1f, 0.53f), FeedOff = new Color(0.06f, 0.2f, 0.11f),
                                  LampOn = new Color(1f, 0.7f, 0.24f), LampOff = new Color(0.25f, 0.16f, 0.06f);

    private Vector3 _podRest;
    private Spring _pod;
    private float _feedFor, _beaconFor;
    private bool _scanning, _lit, _rested;
    private MaterialPropertyBlock _block;

    private void Awake()
    {
        Rest();
        if (trayClick != null)
            trayClick.onClick.AddListener(() => TrayClicked?.Invoke());
        if (beaconClick != null)
            beaconClick.onClick.AddListener(() => BeaconClicked?.Invoke());
    }

    /// <summary>The tray took the stack: <paramref name="count"/> papers queued (the rollers whirr and the lamp blinks once per paper), or none (a short dry whirr).</summary>
    public void Stacked(int count)
    {
        Rest();
        if (rollers == null || !rollers.gameObject.activeInHierarchy)
            return;
        _feedFor = count > 0 ? 0.2f * count + 0.3f : 0.3f;
        CueSounds.Play(SoundCues.AdfFeed, sound, count > 0 ? 1.05f : 0.75f);
    }

    private void Rest()
    {
        if (_rested)
            return;
        _rested = true;
        if (beaconPod != null)
            _podRest = beaconPod.localPosition;
        Tint(feedLed, FeedOff);
        Tint(beaconLamp, LampOff);
    }

    /// <summary>A scan runs (or stopped): the rollers turn while one runs.</summary>
    public void Scanning(bool on)
    {
        Rest();
        _scanning = on;
    }

    /// <summary>The Auto-Feed took a paper in: the rollers' zip and the lamp's blink.</summary>
    public void Fed()
    {
        Rest();
        if (rollers == null || !rollers.gameObject.activeInHierarchy)
            return;
        _feedFor = 0.6f;
        CueSounds.Play(SoundCues.AdfFeed, sound);
    }

    /// <summary>An analysis pass ended: <paramref name="marked"/> when it marked a pair (the pod pops, lit, and spins), else it pops half way and sinks.</summary>
    public void Flag(bool marked)
    {
        Rest();
        if (beaconPod == null || !beaconPod.gameObject.activeInHierarchy)
            return;
        _lit = marked;
        _beaconFor = marked ? beaconSeconds : 0.45f;
        Aim(ref _pod, marked ? 1f : 0.5f);
        _pod.Kick(marked ? 6f : 3f);
        Tint(beaconLamp, marked ? LampOn : LampOff);
        CueSounds.Play(SoundCues.BeaconPop, sound, marked ? 1f : 0.85f);
    }

    private void Update()
    {
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        bool still = UiMotion.Amount.Still;
        if (rollers != null && (_scanning || _feedFor > 0f) && !still)
            rollers.localRotation *= Quaternion.AngleAxis(rollerSpin * dt, Vector3.forward);
        if (_feedFor > 0f)
        {
            _feedFor -= dt;
            Tint(feedLed, _feedFor > 0f && Mathf.Repeat(_feedFor, 0.2f) > 0.1f ? FeedOn : FeedOff);
        }
        if (_beaconFor > 0f)
        {
            _beaconFor -= dt;
            if (_lit && reflector != null && !still)
                reflector.localRotation *= Quaternion.AngleAxis(beaconSpin * dt, Vector3.up);
            if (_beaconFor <= 0f)
            {
                _lit = false;
                Tint(beaconLamp, LampOff);
                Aim(ref _pod, 0f);
            }
        }
        if (beaconPod != null && !_pod.AtRest)
        {
            _pod.Step(dt, UiMotion.Knobs.Get(MotionFeel.Elastic), 1e-4f, 1e-3f);
            beaconPod.localPosition = _podRest + Vector3.up * (podLift * _pod.Value);
        }
    }

    private static void Aim(ref Spring spring, float target)
    {
        spring.Target = target;
        if (UiMotion.Amount.Still)
            spring.Snap(target);
    }

    /// <summary>Sets a lamp's glow (its Unlit base colour, per renderer).</summary>
    private void Tint(Renderer r, Color c)
    {
        if (r == null)
            return;
        _block ??= new MaterialPropertyBlock();
        DeskProps.Tint(r, c, _block);
    }
}
