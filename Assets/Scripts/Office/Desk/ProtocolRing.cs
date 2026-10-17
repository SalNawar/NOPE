using UnityEngine;

/// <summary>
/// The Interview Protocols' card ring (the desk machines spec §4): four
/// question cards on a brass ring on a weighted stand. A click flicks through
/// them, each card swinging over the ring in turn with a soft tab (ui_tab);
/// a new traveller flicks the front card once (the protocol's next page).
/// Shown while the upgrade is owned (DeskUpgrades). Built by Build Office
/// UI from ProtocolRing.fbx (tools/props/machines).
/// </summary>
public sealed class ProtocolRing : MonoBehaviour
{
    /// <summary>The cards, front to back (each turns about its local X: positive brings its foot forward and up).</summary>
    [SerializeField] private Transform[] cards = System.Array.Empty<Transform>();

    /// <summary>The ring's click box.</summary>
    [SerializeField] private Clickable click;

    /// <summary>The cues' placeholder source.</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>The beat between two cards' flicks (seconds).</summary>
    [SerializeField] private float beat = 0.09f;

    private Quaternion[] _rest;
    private Spring[] _turn;
    private float[] _startAt;
    private float _now;

    private void Awake()
    {
        _rest = new Quaternion[cards.Length];
        _turn = new Spring[cards.Length];
        _startAt = new float[cards.Length];
        for (int i = 0; i < cards.Length; i++)
        {
            _rest[i] = cards[i] != null ? cards[i].localRotation : Quaternion.identity;
            _startAt[i] = float.PositiveInfinity;
        }
        if (click != null)
            click.onClick.AddListener(FlickAll);
    }

    /// <summary>The cards' turns so far (0 at rest, 1 a full turn), front to back (the probes read them).</summary>
    public float Turn(int card) => _turn != null && card >= 0 && card < _turn.Length ? _turn[card].Value : 0f;

    /// <summary>Every card flicks over the ring, front to back.</summary>
    public void FlickAll()
    {
        for (int i = 0; i < cards.Length; i++)
            Queue(i, i * beat);
    }

    /// <summary>The front card flicks over once (a new traveller).</summary>
    public void FlickFront()
    {
        if (cards.Length > 0 && isActiveAndEnabled)
            Queue(0, 0f);
    }

    private void Queue(int i, float delay)
    {
        if (_turn == null || !_turn[i].AtRest || !float.IsPositiveInfinity(_startAt[i]))
            return;
        _startAt[i] = _now + delay;
    }

    private void Update()
    {
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        _now += dt;
        for (int i = 0; i < cards.Length; i++)
        {
            if (_now >= _startAt[i])
            {
                _startAt[i] = float.PositiveInfinity;
                CueSounds.Play(SoundCues.UiTab, sound, 1f + 0.06f * i);
                _turn[i] = Spring.At(0f);
                DeskProps.Aim(ref _turn[i], 1f);
                _turn[i].Kick(2f);
            }
            if (!DeskProps.Step(ref _turn[i], dt, MotionFeel.Balanced))
                continue;
            if (_turn[i].AtRest)
                _turn[i] = Spring.At(0f); // a full turn: back where it hung
            if (cards[i] != null)
                cards[i].localRotation = _rest[i] * Quaternion.AngleAxis(360f * _turn[i].Value, Vector3.right);
        }
    }
}
