using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The supply crate (the desk machines spec §4: Saleh's answer "supply crate
/// yes"): on a morning the Orders app's deliveries arrive
/// (Orders.DeliveredOn), a taped kraft crate stands on the mat. A click opens
/// it: the red tape tears off (crate_tear), the four flaps pop open one after
/// another (crate_pop) and a tag with what came springs up out of it (the
/// upgrades' names), then the crate sinks away and the delivered objects are
/// on the desk. Left shut, it opens by itself when the shift opens (the
/// AVAILABLE sign), so it never stands in the way of a case. Built by Build
/// Office UI from SupplyCrate.fbx (tools/props/machines).
/// </summary>
public sealed class SupplyCrate : MonoBehaviour
{
    /// <summary>The flaps (Left and Right turn about local Z, Front and Back about X), the tape and the point the tag rises from.</summary>
    [SerializeField] private Transform flapLeft, flapRight, flapFront, flapBack, tape, itemPoint;

    /// <summary>The tag that springs out (its text: the delivered upgrades' names).</summary>
    [SerializeField] private TMP_Text tag;

    /// <summary>The crate's click box.</summary>
    [SerializeField] private Clickable click;

    /// <summary>The cues' placeholder source.</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>The flaps' open angles (degrees: left, right, front, back), the tag's rise (metres, model space), the beat between flaps and how long it stays open before it sinks.</summary>
    [SerializeField] private Vector4 openAngles = new Vector4(118f, -112f, -125f, 105f);

    /// <inheritdoc cref="openAngles"/>
    [SerializeField] private float tagRise = 0.15f, flapBeat = 0.08f, openSeconds = 2.6f;

    private enum Phase { Gone, Shut, Tearing, Open, Sinking }

    private Phase _phase = Phase.Gone;
    private float _at;
    private int _flapsOpen;
    private Spring _tape, _tag, _sink;
    private readonly Spring[] _flaps = new Spring[4];
    private Quaternion[] _flapRest;
    private Vector3 _tagRest, _rootRest;
    private bool _rested;

    /// <summary>True while the crate is on the desk.</summary>
    public bool Present => _phase != Phase.Gone;

    /// <summary>Raised as the flaps pop open (what came appears on the desk then: DeskUpgrades).</summary>
    public event System.Action Opening;

    /// <summary>True once it has been opened today (or there was none).</summary>
    public bool Opened => _phase == Phase.Gone || _phase >= Phase.Open;

    private void Awake()
    {
        Rest();
        if (click != null)
            click.onClick.AddListener(Open);
    }

    private void Rest()
    {
        if (_rested)
            return;
        _rested = true;
        _flapRest = new[] { Rot(flapLeft), Rot(flapRight), Rot(flapFront), Rot(flapBack) };
        if (tag != null)
            _tagRest = tag.transform.localPosition;
    }

    private static Quaternion Rot(Transform t) => t != null ? t.localRotation : Quaternion.identity;

    /// <summary>
    /// A morning's deliveries: the crate stands shut with a tag naming
    /// <paramref name="names"/>; none, and there is no crate today.
    /// </summary>
    public void Deliver(IReadOnlyList<string> names)
    {
        Rest();
        bool any = names != null && names.Count > 0;
        gameObject.SetActive(any);
        _phase = any ? Phase.Shut : Phase.Gone;
        if (!any)
            return;
        _rootRest = transform.localPosition;
        _flapsOpen = 0;
        for (int i = 0; i < _flaps.Length; i++)
            _flaps[i] = Spring.At(0f);
        _tape = Spring.At(0f);
        _tag = Spring.At(0f);
        _sink = Spring.At(0f);
        Pose();
        if (tag != null)
        {
            tag.text = string.Join("\n", names);
            tag.gameObject.SetActive(false);
        }
        if (tape != null)
            tape.gameObject.SetActive(true);
    }

    /// <summary>Opens the shut crate (a click, or the shift opening).</summary>
    public void Open()
    {
        if (_phase != Phase.Shut)
            return;
        _phase = Phase.Tearing;
        _at = 0f;
        DeskProps.Aim(ref _tape, 1f);
        CueSounds.Play(SoundCues.CrateTear, sound);
    }

    /// <summary>The laid crate's place (the binder lays it; it sinks from there).</summary>
    public void Laid() => _rootRest = transform.localPosition;

    private void Update()
    {
        if (_phase == Phase.Gone || _phase == Phase.Shut)
            return;
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        _at += dt;
        switch (_phase)
        {
            case Phase.Tearing:
                if (_at >= 0.22f || UiMotion.Amount.Still)
                {
                    if (tape != null)
                        tape.gameObject.SetActive(false);
                    _phase = Phase.Open;
                    _at = 0f;
                }
                break;
            case Phase.Open:
                while (_flapsOpen < 4 && (_at >= _flapsOpen * flapBeat || UiMotion.Amount.Still))
                {
                    DeskProps.Aim(ref _flaps[_flapsOpen], 1f);
                    _flaps[_flapsOpen].Kick(3f);
                    if (_flapsOpen == 0)
                    {
                        CueSounds.Play(SoundCues.CratePop, sound);
                        if (tag != null)
                            tag.gameObject.SetActive(true);
                        DeskProps.Aim(ref _tag, 1f);
                        _tag.Kick(4f);
                        Opening?.Invoke();
                    }
                    _flapsOpen++;
                }
                if (_at >= openSeconds)
                {
                    _phase = Phase.Sinking;
                    _at = 0f;
                    DeskProps.Aim(ref _sink, 1f);
                }
                break;
            case Phase.Sinking:
                if (_sink.AtRest || UiMotion.Amount.Still)
                {
                    _phase = Phase.Gone;
                    transform.localPosition = _rootRest;
                    gameObject.SetActive(false);
                    return;
                }
                break;
        }
        DeskProps.Step(ref _tape, dt, MotionFeel.Firm);
        for (int i = 0; i < _flaps.Length; i++)
            DeskProps.Step(ref _flaps[i], dt, MotionFeel.Elastic);
        DeskProps.Step(ref _tag, dt, MotionFeel.Elastic);
        DeskProps.Step(ref _sink, dt, MotionFeel.Balanced);
        Pose();
    }

    private void Pose()
    {
        Flap(flapLeft, 0, Vector3.forward, openAngles.x);
        Flap(flapRight, 1, Vector3.forward, openAngles.y);
        Flap(flapFront, 2, Vector3.right, openAngles.z);
        Flap(flapBack, 3, Vector3.right, openAngles.w);
        if (tape != null)
            tape.localScale = new Vector3(1f, 1f, 1f - 0.9f * Mathf.Clamp01(_tape.Value));
        if (tag != null)
            tag.transform.localPosition = _tagRest + Vector3.up * (tagRise * _tag.Value);
        transform.localPosition = _rootRest + Vector3.down * (0.16f * _sink.Value);
    }

    private void Flap(Transform flap, int i, Vector3 axis, float angle)
    {
        if (flap != null)
            flap.localRotation = _flapRest[i] * Quaternion.AngleAxis(angle * _flaps[i].Value, axis);
    }
}
