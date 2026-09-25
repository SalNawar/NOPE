using TMPro;
using UnityEngine;

/// <summary>
/// A desk prop's click reaction, tuned by a DeskReactionSO: a short animation
/// of the prop (ReactionCurve, relative to its rest pose: squash and pulse
/// scale it, wobble tilts it, nudge bobs it), an optional sound (when the
/// reaction has a clip and the prop an AudioSource) and an optional tooltip
/// that shows a live readout's text (the calendar's day, the till's money in
/// the wallet's word...) through the overlay tooltip callout. The animated
/// object is the art prop the office binder hands over (SetTarget), else this
/// object.
/// </summary>
[RequireComponent(typeof(Clickable))]
public sealed class DeskReaction : MonoBehaviour
{
    /// <summary>The reaction's tuning.</summary>
    [SerializeField] private DeskReactionSO reaction;

    /// <summary>Optional: the live text the tooltip's {0} shows.</summary>
    [SerializeField] private TMP_Text readout;

    /// <summary>The overlay tooltip (shared by every prop).</summary>
    [SerializeField] private OverlayCallout tooltip;

    /// <summary>Optional: plays the reaction's clip.</summary>
    [SerializeField] private AudioSource audioSource;

    private Transform _target;
    private Transform _tooltipPoint;
    private Vector3 _restPosition;
    private Quaternion _restRotation;
    private Vector3 _restScale;
    private float _elapsed;
    private bool _animating;

    /// <summary>The object that animates.</summary>
    private Transform Target => _target != null ? _target : transform;

    private void Awake()
    {
        GetComponent<Clickable>().onClick.AddListener(Play);
    }

    /// <summary>Animates <paramref name="prop"/> (the art prop this click box stands for) instead of this object.</summary>
    public void SetTarget(Transform prop) => _target = prop;

    /// <summary>Where the tooltip hangs from (the office binder: the top of the art, so the tooltip never covers the readout it reads); unset, this object.</summary>
    public void SetTooltipPoint(Transform point) => _tooltipPoint = point;

    /// <summary>Sets the live text the tooltip's {0} shows (the office binder: the art's readout).</summary>
    public void SetReadout(TMP_Text text) => readout = text;

    /// <summary>Plays the reaction: the animation, the clip when there is one and a source, and the tooltip when its key is not blank.</summary>
    public void Play()
    {
        if (reaction == null)
            return;

        if (!_animating)
        {
            _restPosition = Target.localPosition;
            _restRotation = Target.localRotation;
            _restScale = Target.localScale;
        }

        _elapsed = 0f;
        _animating = reaction.kind != ReactionKind.None;
        if (reaction.clip != null && audioSource != null)
            audioSource.PlayOneShot(reaction.clip);
        if (tooltip != null && !string.IsNullOrWhiteSpace(reaction.tooltipKey))
            tooltip.Show(UiText.Format(reaction.tooltipKey, readout != null ? readout.text : string.Empty, UiText.Currency(UiText.WalletForm.Label)),
                         _tooltipPoint != null ? _tooltipPoint : transform, reaction.tooltipOffset, reaction.tooltipSeconds);
    }

    /// <summary>Only while animating: the pose relative to the rest pose (squash widens across the desk's plane); the rest pose again at the end.</summary>
    private void Update()
    {
        if (!_animating)
            return;

        Transform t = Target;
        _elapsed += Time.deltaTime;
        float progress = _elapsed / reaction.seconds;
        if (progress >= 1f)
        {
            t.localPosition = _restPosition;
            t.localRotation = _restRotation;
            t.localScale = _restScale;
            _animating = false;
            return;
        }

        ReactionPose pose = ReactionCurve.Evaluate(reaction.kind, progress, reaction.amplitude);
        Vector3 bob = Vector3.up * pose.OffsetY;
        t.localPosition = _restPosition + (t.parent != null ? t.parent.InverseTransformVector(bob) : bob);
        t.localRotation = _restRotation * Quaternion.Euler(0f, 0f, pose.AngleDeg);
        t.localScale = new Vector3(_restScale.x * pose.ScaleX, _restScale.y * pose.ScaleY, _restScale.z * pose.ScaleX);
    }
}
