using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The BABEL rack (the desk machines spec §4): the Speech translators' four
/// cartridges (translation.packs in order: Near East, Mediterranean, East
/// Asia, North Europe), each seated in its bay while its pack is owned
/// (Translation.UpgradeId). A cartridge delivered today drops into its bay
/// with a clunk (cartridge_in) once the supply crate is open. While a
/// traveller speaks a tongue its pack translates, that cartridge's lamp
/// glows and the VU needle dances; a click on the rack plays its test
/// chirp (translator_chirp): the needle sweeps to the red and back and the
/// seated cartridges' lamps blink. Built by Build Office UI from
/// BabelRack.fbx (tools/props/machines).
/// </summary>
public sealed class BabelRack : MonoBehaviour
{
    /// <summary>The cartridges and their lamps, bay by bay (the packs' order).</summary>
    [SerializeField] private Transform[] cartridges = System.Array.Empty<Transform>();

    /// <inheritdoc cref="cartridges"/>
    [SerializeField] private Renderer[] lamps = System.Array.Empty<Renderer>();

    /// <summary>The VU needle (it turns about its local Z: +50 rests at the left, -50 is the red).</summary>
    [SerializeField] private Transform needle;

    /// <summary>The rack's click box.</summary>
    [SerializeField] private Clickable click;

    /// <summary>The cues' placeholder source.</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>How long a translated line keeps a lamp lit (seconds), and how high a new cartridge starts over its bay (metres, model space).</summary>
    [SerializeField] private float speakSeconds = 1.8f, dropHeight = 0.07f;

    private static readonly Color LampOn = new Color(0.24f, 1f, 0.53f), LampOff = new Color(0.05f, 0.16f, 0.09f);
    private const float NeedleRest = 50f, NeedleRed = -50f;

    private Quaternion _needleRest;
    private Vector3[] _seat;
    private Spring[] _drop;
    private bool[] _owned, _waiting;
    private float[] _speak, _dropIn;
    private Spring _needle;
    private float _test, _now;
    private MaterialPropertyBlock _block;
    private bool _rested;

    private void Awake()
    {
        Rest();
        if (click != null)
            click.onClick.AddListener(Test);
    }

    private void Rest()
    {
        if (_rested)
            return;
        _rested = true;
        int n = cartridges.Length;
        _seat = new Vector3[n];
        _drop = new Spring[n];
        _owned = new bool[n];
        _waiting = new bool[n];
        _speak = new float[n];
        _dropIn = new float[n];
        for (int i = 0; i < n; i++)
            _seat[i] = cartridges[i] != null ? cartridges[i].localPosition : Vector3.zero;
        if (needle != null)
            _needleRest = needle.localRotation; // the model's needle stands straight up
        _needle = Spring.At(0f);
        PoseNeedle();
        _block = new MaterialPropertyBlock();
        for (int i = 0; i < lamps.Length; i++)
            DeskProps.Tint(lamps[i], LampOff, _block);
    }

    /// <summary>
    /// The day's cartridges (UpgradeObjects.Bays, bay by bay): each owned one
    /// seated; one new today waits over its bay for Unpacked unless
    /// <paramref name="rackNew"/> (the whole rack waits for the crate).
    /// False when none is owned (the rack is not on the desk).
    /// </summary>
    public bool SetDay(IReadOnlyList<UpgradeObjects.Bay> bays, bool rackNew)
    {
        Rest();
        bool any = false;
        for (int i = 0; i < cartridges.Length; i++)
        {
            UpgradeObjects.Bay bay = bays != null && i < bays.Count ? bays[i] : default;
            _owned[i] = bay.Owned;
            _waiting[i] = bay.NewToday && !rackNew;
            any |= _owned[i];
            _drop[i] = Spring.At(_waiting[i] ? 1f : 0f);
            _speak[i] = 0f;
            _dropIn[i] = 0f;
            if (cartridges[i] != null)
            {
                cartridges[i].gameObject.SetActive(_owned[i] && !_waiting[i]);
                cartridges[i].localPosition = _seat[i] + Vector3.up * (dropHeight * _drop[i].Value);
            }
        }
        return any;
    }

    /// <summary>The crate is open: today's new cartridges drop into their bays, one after another.</summary>
    public void Unpacked()
    {
        int k = 0;
        for (int i = 0; i < cartridges.Length; i++)
            if (_waiting[i])
            {
                _waiting[i] = false;
                if (cartridges[i] != null)
                    cartridges[i].gameObject.SetActive(true);
                _dropIn[i] = 0.15f + 0.15f * k++; // the stagger before it drops
            }
    }

    /// <summary>A line in a tongue of bay <paramref name="bay"/>'s pack was translated: its lamp glows and the needle dances.</summary>
    public void Spoke(int bay)
    {
        if (bay < 0 || bay >= cartridges.Length || !_owned[bay] || !isActiveAndEnabled)
            return;
        _speak[bay] = speakSeconds;
    }

    /// <summary>The test chirp (a click on the rack).</summary>
    public void Test()
    {
        CueSounds.Play(SoundCues.TranslatorChirp, sound);
        _test = 0.7f;
        DeskProps.Aim(ref _needle, 1f);
        _needle.Kick(4f);
    }

    private void Update()
    {
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        _now += dt;
        bool speaking = false;
        for (int i = 0; i < cartridges.Length; i++)
        {
            if (_dropIn[i] > 0f)
            {
                _dropIn[i] -= dt;
                if (_dropIn[i] <= 0f)
                {
                    _dropIn[i] = 0f;
                    DeskProps.Aim(ref _drop[i], 0f);
                    CueSounds.Play(SoundCues.CartridgeIn, sound, 1f + 0.05f * i);
                }
            }
            if (_speak[i] > 0f)
            {
                _speak[i] = Mathf.Max(0f, _speak[i] - dt);
                speaking = true;
            }
            if (DeskProps.Step(ref _drop[i], dt, MotionFeel.Firm) && cartridges[i] != null)
                cartridges[i].localPosition = _seat[i] + Vector3.up * (dropHeight * _drop[i].Value);
            bool lit = _owned[i] && (_speak[i] > 0f || (_test > 0f && Mathf.Repeat(_test, 0.2f) > 0.1f));
            if (i < lamps.Length)
                DeskProps.Tint(lamps[i], lit ? LampOn : LampOff, _block);
        }
        if (_test > 0f)
        {
            _test -= dt;
            if (_test <= 0.35f && _needle.Target > 0f)
                DeskProps.Aim(ref _needle, 0f);
        }
        if (speaking && !UiMotion.Amount.Still)
            _needle.Target = 0.35f + 0.3f * Mathf.PerlinNoise(_now * 6f, 0.5f);
        else if (!speaking && _test <= 0f && _needle.Target != 0f)
            DeskProps.Aim(ref _needle, 0f);
        if (DeskProps.Step(ref _needle, dt, MotionFeel.Balanced))
            PoseNeedle();
    }

    /// <summary>The needle at the spring's value: 0 rests at the low end (+50 degrees), 1 is the red (-50).</summary>
    private void PoseNeedle()
    {
        if (needle != null)
            needle.localRotation = _needleRest * Quaternion.AngleAxis(Mathf.LerpUnclamped(NeedleRest, NeedleRed, _needle.Value), Vector3.forward);
    }
}
