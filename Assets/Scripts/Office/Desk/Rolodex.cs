using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The Diplomatic Contacts' Rolodex (the desk machines spec §4): a drum of
/// index cards on a brass axle. Drag it up or down and the drum turns with
/// the pointer, then spins on and slows to a card; a click flicks it on a
/// few cards. Each card that passes the top flicks (rolodex_flick), and the
/// drum settles with a card facing the clerk. Shown while the upgrade is
/// owned (DeskUpgrades). Built by Build Office UI from Rolodex.fbx
/// (tools/props/machines).
/// </summary>
public sealed class Rolodex : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>The drum and the knob (both turn about their local X).</summary>
    [SerializeField] private Transform drum, knob;

    /// <summary>The cues' placeholder source.</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>The cards' spacing (degrees), the drag's turn per screen pixel, a click's flick (degrees a second), the spin's slowing (per second) and the speed under which it settles on a card.</summary>
    [SerializeField] private float cardDegrees = 15f, degreesPerPixel = 0.6f, flickSpeed = 420f, friction = 2.6f, settleSpeed = 40f;

    private Quaternion _drumRest, _knobRest;
    private float _angle, _speed, _lastCard;
    private bool _dragging;
    private Spring _settle;
    private bool _settling;

    private void Awake()
    {
        if (drum != null)
            _drumRest = drum.localRotation;
        if (knob != null)
            _knobRest = knob.localRotation;
    }

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
    public void OnEndDrag(PointerEventData eventData) => _dragging = false;

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
            if (_settle.AtRest)
                _settling = false;
            Pose();
        }
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
