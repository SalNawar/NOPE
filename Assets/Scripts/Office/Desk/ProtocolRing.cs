using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The Interview Protocols' card ring (the desk machines spec §4, §10; Saleh
/// 2026-10-18, demo 1018a note 14: "every object the playyer unlocks should
/// be useable not just there like it should have a function"): laminated
/// question cards on a brass ring on a weighted stand, the diegetic twin of
/// the wheel's ask menu. Its cards are the questions the traveller at the
/// desk can be asked now (ProtocolDeck over InterviewPresenter.ProtocolCards:
/// the ask menu's open questions; a question unlocks as its detail is
/// logged), each printed on a card, the front card's first. A click on the
/// ring asks the front card's question, exactly as the wheel would (the
/// desk's prompt and the answer in the bubble and the transcript): the front
/// card swings over the ring (ui_tab) and the next question comes to the
/// front. A drag on the ring flicks the front card over without asking. With
/// nothing to ask (no traveller, no question unlocked) a click only sways the
/// ring. A new traveller flicks the front card once. Shown while the upgrade
/// is owned (DeskUpgrades). Built by Build Office UI from ProtocolRing.fbx
/// (tools/props/machines), a label printed on each card's face.
/// </summary>
public sealed class ProtocolRing : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>The cards, front to back (each turns about its local X: positive brings its foot forward and up).</summary>
    [SerializeField] private Transform[] cards = System.Array.Empty<Transform>();

    /// <summary>The question printed on each card (front to back; the builder's live text on each face).</summary>
    [SerializeField] private TMP_Text[] labels = System.Array.Empty<TMP_Text>();

    /// <summary>The ring the cards hang on (it sways about its local X when there is nothing to ask).</summary>
    [SerializeField] private Transform ring;

    /// <summary>The ring's click box.</summary>
    [SerializeField] private Clickable click;

    /// <summary>The cues' placeholder source.</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>How far the ring sways when there is nothing to ask (degrees).</summary>
    [SerializeField] private float swayDegrees = 14f;

    private readonly ProtocolDeck _deck = new ProtocolDeck();
    private InvestigationUIController _investigation;
    private int _version = -1;
    private Quaternion[] _rest;
    private Quaternion _ringRest;
    private Spring _turn, _sway;
    private bool _relabel, _dragged;

    /// <summary>The deck on the ring (the probes read it).</summary>
    public ProtocolDeck Deck => _deck;

    private void Awake()
    {
        _rest = new Quaternion[cards.Length];
        for (int i = 0; i < cards.Length; i++)
            _rest[i] = cards[i] != null ? cards[i].localRotation : Quaternion.identity;
        if (ring != null)
            _ringRest = ring.localRotation;
        if (click != null)
            click.onClick.AddListener(AskFront);
        Relabel();
    }

    /// <summary>Wires it to the investigation (the questions it asks; DeskUpgrades, from GameManager).</summary>
    public void Bind(InvestigationUIController investigation)
    {
        _investigation = investigation;
        _version = -1;
    }

    /// <summary>A click: the front card's question is asked and the card swings over to the next; with nothing to ask, the ring only sways.</summary>
    public void AskFront()
    {
        Refresh();
        if (_deck.Empty || _investigation == null || !_investigation.AskProtocol(_deck.Front.Id))
        {
            Sway();
            return;
        }
        Swing();
        Refresh(); // the asked card leaves; the next is at the front once the card is over
    }

    /// <summary>The front card flicks over once (a new traveller): their questions come to the front.</summary>
    public void FlickFront()
    {
        if (cards.Length > 0 && isActiveAndEnabled)
            Swing();
    }

    /// <inheritdoc/>
    public void OnBeginDrag(PointerEventData eventData) => _dragged = false;

    /// <summary>A drag on the ring flicks the front card over to the next (once per drag), without asking.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_dragged || eventData.delta.sqrMagnitude < 4f)
            return;
        _dragged = true;
        Refresh();
        if (_deck.Flick())
            Swing();
        else
            Sway();
    }

    /// <inheritdoc/>
    public void OnEndDrag(PointerEventData eventData) => _dragged = false;

    /// <summary>The deck follows the interview (a new traveller, a question asked, unlocked or added).</summary>
    private void Refresh()
    {
        int version = _investigation != null ? _investigation.InterviewVersion : 0;
        if (version == _version)
            return;
        _version = version;
        _deck.Set(_investigation != null ? _investigation.ProtocolCards() : null);
        if (_turn.AtRest)
            Relabel();
        else
            _relabel = true;
    }

    /// <summary>The front card swings over the ring (its labels change as it passes the top).</summary>
    private void Swing()
    {
        CueSounds.Play(SoundCues.UiTab, sound, 1f);
        _turn = Spring.At(0f);
        DeskProps.Aim(ref _turn, 1f);
        _turn.Kick(2f);
        _relabel = true;
        if (UiMotion.Amount.Still)
            Relabel();
    }

    /// <summary>The ring sways: nothing to ask.</summary>
    private void Sway()
    {
        CueSounds.Play(SoundCues.UiTab, sound, 0.8f);
        _sway = Spring.At(0f);
        _sway.Kick(UiMotion.Amount.Still ? 0f : 6f);
    }

    /// <summary>Each card prints its question: the deck from the front, card by card; a card past the deck is blank.</summary>
    private void Relabel()
    {
        _relabel = false;
        var deck = _deck.Cards;
        for (int i = 0; i < labels.Length; i++)
            if (labels[i] != null)
                labels[i].text = i < deck.Count ? deck[(_deck.FrontIndex + i) % deck.Count].Label : string.Empty;
    }

    private void Update()
    {
        if (_investigation != null && _investigation.InterviewVersion != _version)
            Refresh();
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        if (DeskProps.Step(ref _turn, dt, MotionFeel.Balanced))
        {
            if (_relabel && _turn.Value >= 0.5f)
                Relabel();
            if (_turn.AtRest)
            {
                _turn = Spring.At(0f); // a full turn: back where it hung
                if (_relabel)
                    Relabel();
            }
            if (cards.Length > 0 && cards[0] != null)
                cards[0].localRotation = _rest[0] * Quaternion.AngleAxis(360f * _turn.Value, Vector3.right);
        }
        if (ring != null && DeskProps.Step(ref _sway, dt, MotionFeel.Elastic))
            ring.localRotation = _ringRest * Quaternion.AngleAxis(swayDegrees * _sway.Value, Vector3.right);
    }
}
