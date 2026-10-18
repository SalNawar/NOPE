using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The RING SERVICE panel (the desk machines spec §4, §10): five relay
/// modules, one per portal (01 to 05 from the left; 03 the Return Gate), each
/// with a SERVICED sticker once its repair is owned. Its function (Saleh
/// 2026-10-18, demo 1018a note 14: "every object the playyer unlocks should
/// be useable"): it is the desk's own departure board, the rule the clerk
/// checks a destination against. Each module's pilot lamp shows its ring
/// today (UpgradeObjects.LampOf): green on an open route, red on a route
/// CLOSED today, amber with no route, blue for the Return Gate in service,
/// dark under maintenance; its breaker stands up while the ring is in
/// service. A click on a breaker clacks its relay (relay_clack): the breaker
/// snaps down and back, its lamp blinks, and the module reads its ring out
/// in the overlay's tooltip, the board's own line ("02 Rear left: Weimar
/// Berlin, Modern: CLOSED today"; PortalText.TooltipLine). A traveller who
/// goes through a portal flashes its lamp. On the desk once any ring's
/// repair is owned (DeskUpgrades). Built by Build Office UI from
/// RingPanel.fbx (tools/props/machines).
/// </summary>
public sealed class RingPanel : MonoBehaviour
{
    /// <summary>The modules' lamps, breakers (they turn about local X, +25 up, -25 down), stickers and breakers' click boxes, 01 to 05.</summary>
    [SerializeField] private Renderer[] lamps = System.Array.Empty<Renderer>();

    /// <inheritdoc cref="lamps"/>
    [SerializeField] private Transform[] breakers = System.Array.Empty<Transform>(), stickers = System.Array.Empty<Transform>();

    /// <inheritdoc cref="lamps"/>
    [SerializeField] private Clickable[] clicks = System.Array.Empty<Clickable>();

    /// <summary>The cues' placeholder source.</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>The breakers' throw (degrees) and how long a flash lasts (seconds).</summary>
    [SerializeField] private float throwDegrees = 25f, flashSeconds = 0.9f;

    private static readonly Color Green = new Color(0.24f, 1f, 0.53f), Red = new Color(1f, 0.26f, 0.2f), Amber = new Color(1f, 0.7f, 0.24f),
                                  Blue = new Color(0.36f, 0.72f, 1f), Dark = new Color(0.07f, 0.2f, 0.12f), LampFlash = new Color(0.85f, 1f, 0.9f);

    private Quaternion[] _rest;
    private Spring[] _breaker;
    private RingLamp[] _lamp;
    private string[] _lines;
    private float[] _flash, _blink;
    private MaterialPropertyBlock _block;
    private bool _rested;
    private System.Action<string, Transform> _say;

    /// <summary>Each module's lamp today, 01 to 05 (the probes read them).</summary>
    public IReadOnlyList<RingLamp> Lamps => _lamp;

    private void Awake()
    {
        Rest();
        for (int i = 0; i < clicks.Length; i++)
        {
            int module = i;
            if (clicks[i] != null)
                clicks[i].onClick.AddListener(() => Clack(module));
        }
    }

    private void Rest()
    {
        if (_rested)
            return;
        _rested = true;
        int n = breakers.Length;
        _rest = new Quaternion[n];
        _breaker = new Spring[n];
        for (int i = 0; i < n; i++)
            _rest[i] = breakers[i] != null ? breakers[i].localRotation : Quaternion.identity;
        _lamp = new RingLamp[lamps.Length];
        _lines = new string[lamps.Length];
        _flash = new float[lamps.Length];
        _blink = new float[lamps.Length];
        _block = new MaterialPropertyBlock();
    }

    /// <summary>Wires the read-out of a module (DeskUpgrades: the overlay's tooltip over the breaker).</summary>
    public void Bind(System.Action<string, Transform> say) => _say = say;

    /// <summary>
    /// The day's modules (UpgradeObjects.Modules, 01 to 05) and each one's
    /// line (PortalText.TooltipLine of its portal; null: none): each lamp
    /// shows its ring's route, each breaker stands up while its ring is in
    /// service, each sticker once its repair is owned.
    /// </summary>
    public void SetDay(IReadOnlyList<UpgradeObjects.Module> modules, IReadOnlyList<string> lines)
    {
        Rest();
        for (int i = 0; i < lamps.Length; i++)
        {
            UpgradeObjects.Module m = modules != null && i < modules.Count ? modules[i] : default;
            _lamp[i] = m.Lamp;
            _lines[i] = lines != null && i < lines.Count ? lines[i] : null;
            if (i < stickers.Length && stickers[i] != null)
                stickers[i].gameObject.SetActive(m.Serviced);
            if (i < _breaker.Length)
                _breaker[i] = Spring.At(m.InService ? 1f : -1f);
            _flash[i] = _blink[i] = 0f;
        }
        PoseAll();
    }

    /// <summary>A traveller went through portal <paramref name="portal"/> (its number): its lamp flashes.</summary>
    public void Departed(int portal)
    {
        int i = portal - 1;
        if (i < 0 || i >= lamps.Length || !isActiveAndEnabled)
            return;
        _flash[i] = flashSeconds;
    }

    /// <summary>Module <paramref name="i"/>'s breaker clicked: the relay clacks, the lamp blinks and the module reads its ring out.</summary>
    private void Clack(int i)
    {
        if (i < 0 || i >= _breaker.Length)
            return;
        CueSounds.Play(SoundCues.RelayClack, sound, 0.95f + 0.03f * i);
        float home = i < _lamp.Length && _lamp[i] != RingLamp.Dark ? 1f : -1f;
        _breaker[i].Snap(-home);
        DeskProps.Aim(ref _breaker[i], home);
        _breaker[i].Kick(home * 2f);
        if (i < _blink.Length)
            _blink[i] = 0.45f;
        if (_say != null && i < _lines.Length && !string.IsNullOrEmpty(_lines[i]))
            _say(_lines[i], breakers[i] != null ? breakers[i] : transform);
    }

    private void Update()
    {
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        for (int i = 0; i < _breaker.Length; i++)
            if (DeskProps.Step(ref _breaker[i], dt, MotionFeel.Firm) && breakers[i] != null)
                breakers[i].localRotation = _rest[i] * Quaternion.AngleAxis(-throwDegrees * _breaker[i].Value, Vector3.right);
        for (int i = 0; i < lamps.Length; i++)
        {
            if (_flash[i] <= 0f && _blink[i] <= 0f)
                continue;
            _flash[i] = Mathf.Max(0f, _flash[i] - dt);
            _blink[i] = Mathf.Max(0f, _blink[i] - dt);
            Lamp(i);
        }
    }

    private void PoseAll()
    {
        for (int i = 0; i < _breaker.Length; i++)
            if (breakers[i] != null)
                breakers[i].localRotation = _rest[i] * Quaternion.AngleAxis(-throwDegrees * _breaker[i].Value, Vector3.right);
        for (int i = 0; i < lamps.Length; i++)
            Lamp(i);
    }

    /// <summary>A lamp's colour for its ring's route today.</summary>
    private static Color Colour(RingLamp lamp) =>
        lamp == RingLamp.Open ? Green : lamp == RingLamp.Closed ? Red : lamp == RingLamp.NoRoute ? Amber : lamp == RingLamp.ReturnGate ? Blue : Dark;

    private void Lamp(int i)
    {
        Color c = Colour(_lamp[i]);
        if (_blink[i] > 0f && Mathf.Repeat(_blink[i], 0.15f) > 0.075f)
            c = _lamp[i] == RingLamp.Dark ? Green * 0.5f : Dark;
        if (_flash[i] > 0f)
            c = Color.Lerp(c, LampFlash, Mathf.Clamp01(_flash[i] / flashSeconds) * (0.6f + 0.4f * Mathf.Sin(_flash[i] * 40f)));
        DeskProps.Tint(lamps[i], c, _block);
    }
}
