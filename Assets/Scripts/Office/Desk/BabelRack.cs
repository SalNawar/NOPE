using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The BABEL rack (the desk machines spec §4): the Speech translators' four
/// cartridges (translation.packs in order: Near East, Mediterranean, East
/// Asia, North Europe), each seated in its bay while its pack is owned
/// (Translation.UpgradeId). A cartridge delivered today drops into its bay
/// with a clunk (cartridge_in) once the supply crate is open. While a
/// traveller speaks a tongue its pack translates, that cartridge's lamp
/// glows and the VU needle dances. Its functions (Saleh 2026-10-18, demo
/// 1018a note 14: "every object the playyer unlocks should be useable"; the
/// desk machines spec §10): while a traveller whose tongue is foreign today
/// is at the desk, the cartridge that covers their tongue keeps a steady
/// pilot glow (none glows: no cartridge of the rack understands them, so
/// their answers prove nothing); a click on the rack replays the traveller's
/// last line in the bubble (TravellerWheel.Replay), flipping into English
/// again where a cartridge covers it, with its chirp (translator_chirp) and
/// the needle dancing; with no line to replay the click is the test chirp:
/// the needle sweeps to the red and back and the seated cartridges' lamps
/// blink. Built by Build Office UI from BabelRack.fbx (tools/props/machines).
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

    private static readonly Color LampOn = new Color(0.24f, 1f, 0.53f), LampPilot = new Color(0.12f, 0.55f, 0.28f), LampOff = new Color(0.05f, 0.16f, 0.09f);
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
    private System.Func<bool> _replay;
    private int _speaker = -1;
    private float _replaying;

    /// <summary>The bay whose cartridge covers the traveller at the desk (-1: none; the probes read it).</summary>
    public int Speaker => _speaker;

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
        _speaker = -1;
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

    /// <summary>Wires the replay of the traveller's last line (DeskUpgrades: InvestigationUIController.ReplayLastLine).</summary>
    public void Bind(System.Func<bool> replay) => _replay = replay;

    /// <summary>The traveller at the desk speaks a tongue bay <paramref name="bay"/>'s pack covers (-1: none, or a tongue no seated cartridge covers): its pilot glows while they are at the desk.</summary>
    public void SetSpeaker(int bay)
    {
        Rest();
        _speaker = bay >= 0 && bay < cartridges.Length && _owned[bay] ? bay : -1;
        for (int i = 0; i < cartridges.Length && i < lamps.Length; i++)
            DeskProps.Tint(lamps[i], LampOf(i), _block);
    }

    /// <summary>A click on the rack: the traveller's last line again (its chirp and the needle's dance), else the test chirp: the needle sweeps to the red and back and the lamps blink.</summary>
    public void Test()
    {
        if (_replay != null && _replay())
        {
            CueSounds.Play(SoundCues.TranslatorChirp, sound, 1.15f);
            _replaying = speakSeconds; // the needle dances with the line (a translated line lights its cartridge: TongueTranslated)
            return;
        }
        CueSounds.Play(SoundCues.TranslatorChirp, sound);
        _test = 0.7f;
        DeskProps.Aim(ref _needle, 1f);
        _needle.Kick(4f);
    }

    /// <summary>Lamp <paramref name="i"/>'s glow at rest: the traveller's cartridge's pilot, else off.</summary>
    private Color LampOf(int i) => _owned[i] && i == _speaker ? LampPilot : LampOff;

    private void Update()
    {
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        _now += dt;
        bool speaking = _replaying > 0f;
        _replaying = Mathf.Max(0f, _replaying - dt);
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
                DeskProps.Tint(lamps[i], lit ? LampOn : LampOf(i), _block);
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
