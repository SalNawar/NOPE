using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The RING SERVICE panel (the desk machines spec §4): five relay modules,
/// one per portal (01 to 05 from the left; 03 the Return Gate), each with its
/// pilot lamp lit while its ring is in service today (PortalDay), its
/// breaker up, and a green SERVICED sticker once its repair is owned. A
/// click on a breaker clacks its relay (relay_clack): the breaker snaps
/// down and back and its lamp blinks. A traveller who goes through a
/// portal flashes its lamp. On the desk once any ring's repair is owned
/// (DeskUpgrades). Built by Build Office UI from RingPanel.fbx
/// (tools/props/machines).
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

    private static readonly Color LampOn = new Color(0.24f, 1f, 0.53f), LampOff = new Color(0.07f, 0.2f, 0.12f), LampFlash = new Color(0.85f, 1f, 0.9f);

    private Quaternion[] _rest;
    private Spring[] _breaker;
    private bool[] _service;
    private float[] _flash, _blink;
    private MaterialPropertyBlock _block;
    private bool _rested;

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
        _service = new bool[lamps.Length];
        _flash = new float[lamps.Length];
        _blink = new float[lamps.Length];
        _block = new MaterialPropertyBlock();
    }

    /// <summary>
    /// The day's modules (UpgradeObjects.Modules, 01 to 05): each lamp lit and
    /// breaker up while its portal is in service, its sticker once its repair
    /// is owned.
    /// </summary>
    public void SetDay(IReadOnlyList<UpgradeObjects.Module> modules)
    {
        Rest();
        for (int i = 0; i < lamps.Length; i++)
        {
            UpgradeObjects.Module m = modules != null && i < modules.Count ? modules[i] : default;
            _service[i] = m.InService;
            if (i < stickers.Length && stickers[i] != null)
                stickers[i].gameObject.SetActive(m.Serviced);
            if (i < _breaker.Length)
                _breaker[i] = Spring.At(_service[i] ? 1f : -1f);
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

    private void Clack(int i)
    {
        if (i < 0 || i >= _breaker.Length)
            return;
        CueSounds.Play(SoundCues.RelayClack, sound, 0.95f + 0.03f * i);
        float home = _service[i] ? 1f : -1f;
        _breaker[i].Snap(-home);
        DeskProps.Aim(ref _breaker[i], home);
        _breaker[i].Kick(home * 2f);
        _blink[i] = 0.45f;
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

    private void Lamp(int i)
    {
        Color c = _service[i] ? LampOn : LampOff;
        if (_blink[i] > 0f && Mathf.Repeat(_blink[i], 0.15f) > 0.075f)
            c = _service[i] ? LampOff : LampOn;
        if (_flash[i] > 0f)
            c = Color.Lerp(c, LampFlash, Mathf.Clamp01(_flash[i] / flashSeconds) * (0.6f + 0.4f * Mathf.Sin(_flash[i] * 40f)));
        DeskProps.Tint(lamps[i], c, _block);
    }
}
