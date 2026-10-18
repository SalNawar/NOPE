using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The Diplomatic Contacts' Rolodex (the desk machines spec §4, §10): a drum of
/// index cards on a brass axle. Drag it up or down and the drum turns with
/// the pointer, then spins on and slows to a card; a click flicks it on a
/// few cards. Each card that passes the top flicks (rolodex_flick), and the
/// drum settles with a card facing the clerk. Its function (Saleh
/// 2026-10-18, demo 1018a note 14: "every object the playyer unlocks should
/// be useable"): each card is a contact, and the card it settles on reads
/// out in the overlay's tooltip what that contact hears today
/// (ContactCards.ForDay): a tip naming the place one of today's travellers
/// carrying a fault they watch is headed for, or their quiet line; the gold
/// card on top is the day's lead. The 24 cards run through the contacts in
/// turn. Shown while the upgrade is owned (DeskUpgrades). Built by Build
/// Office UI from Rolodex.fbx (tools/props/machines).
/// </summary>
public sealed class Rolodex : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>The drum and the knob (both turn about their local X).</summary>
    [SerializeField] private Transform drum, knob;

    /// <summary>The cues' placeholder source.</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>The cards' spacing (degrees), the drag's turn per screen pixel, a click's flick (degrees a second), the spin's slowing (per second) and the speed under which it settles on a card.</summary>
    [SerializeField] private float cardDegrees = 15f, degreesPerPixel = 0.6f, flickSpeed = 420f, friction = 2.6f, settleSpeed = 40f;

    /// <summary>How many cards the drum carries (the model's Card_00..23).</summary>
    [SerializeField] private int cardCount = 24;

    private Quaternion _drumRest, _knobRest;
    private float _angle, _speed, _lastCard;
    private bool _dragging;
    private Spring _settle;
    private bool _settling;
    private readonly List<ContactCard> _cards = new List<ContactCard>();
    private System.Action<string, Transform> _say;

    /// <summary>The card facing the clerk (0: the gold card, the day's lead).</summary>
    private int Facing => cardCount > 0 ? (int)Mathf.Repeat(-Mathf.Round(_angle / cardDegrees), cardCount) : 0;

    private void Awake()
    {
        if (drum != null)
            _drumRest = drum.localRotation;
        if (knob != null)
            _knobRest = knob.localRotation;
    }

    /// <summary>Wires the read-out of a card (DeskUpgrades: the overlay's tooltip over the Rolodex).</summary>
    public void Bind(System.Action<string, Transform> say) => _say = say;

    /// <summary>Today's contacts' cards (ContactCards.ForDay; the lead first, on the gold card).</summary>
    public void SetDay(IReadOnlyList<ContactCard> cards)
    {
        _cards.Clear();
        if (cards != null)
            _cards.AddRange(cards);
    }

    /// <summary>The contact on card <paramref name="card"/> (the cards run through the contacts in turn); default with none.</summary>
    private ContactCard ContactOn(int card) => _cards.Count > 0 ? _cards[(card % _cards.Count + _cards.Count) % _cards.Count] : default;

    /// <summary>A click flicks the drum on a few cards.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging)
            return;
        _settling = false;
        _speed += flickSpeed;
    }

    /// <inheritdoc/>
    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragging = true;
        _settling = false;
        _speed = 0f;
    }

    /// <inheritdoc/>
    public void OnDrag(PointerEventData eventData)
    {
        float turn = eventData.delta.y * degreesPerPixel;
        _angle += turn;
        float dt = Mathf.Max(UiMotion.Delta(Time.unscaledDeltaTime), 1e-3f);
        _speed = Mathf.Lerp(_speed, turn / dt, 0.5f);
        Pose();
    }

    /// <inheritdoc/>
    public void OnEndDrag(PointerEventData eventData)
    {
        _dragging = false;
        if (_speed == 0f)
            _speed = 1e-3f; // a drag let go still: settle on the nearest card and read it
    }

    private void Update()
    {
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        if (_dragging || dt <= 0f)
            return;
        if (UiMotion.Amount.Still && (_speed != 0f || _settling))
        {
            _angle = Mathf.Round((_angle + Mathf.Sign(_speed) * cardDegrees * 3f) / cardDegrees) * cardDegrees;
            _speed = 0f;
            _settling = false;
            Pose();
            Settled();
            return;
        }
        if (!_settling && Mathf.Abs(_speed) > settleSpeed)
        {
            _angle += _speed * dt;
            _speed *= Mathf.Exp(-friction * dt);
            Pose();
            return;
        }
        if (!_settling && _speed != 0f)
        {
            _settling = true;
            _settle = Spring.At(_angle);
            _settle.Velocity = _speed;
            _settle.Target = Mathf.Round(_angle / cardDegrees) * cardDegrees;
            _speed = 0f;
        }
        if (_settling && DeskProps.Step(ref _settle, dt, MotionFeel.Firm))
        {
            _angle = _settle.Value;
            Pose();
            if (_settle.AtRest)
            {
                _settling = false;
                Settled();
            }
        }
    }

    /// <summary>The drum came to rest: the card facing the clerk reads out its contact's line.</summary>
    private void Settled()
    {
        ContactCard card = ContactOn(Facing);
        if (_say != null && !string.IsNullOrEmpty(card.Line))
            _say(UiText.Format("rolodex.card", card.Name, card.Post, card.Line), drum != null ? drum : transform);
    }

    private void Pose()
    {
        float card = Mathf.Floor(_angle / cardDegrees);
        if (card != _lastCard)
        {
            _lastCard = card;
            CueSounds.Play(SoundCues.RolodexFlick, sound, 0.92f + 0.16f * Mathf.Repeat(card * 0.618f, 1f));
        }
        if (drum != null)
            drum.localRotation = _drumRest * Quaternion.AngleAxis(_angle, Vector3.right);
        if (knob != null)
            knob.localRotation = _knobRest * Quaternion.AngleAxis(_angle, Vector3.right);
    }
}
