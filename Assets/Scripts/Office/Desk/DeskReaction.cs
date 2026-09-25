using TMPro;
using UnityEngine;

/// <summary>
/// A desk prop's click reaction, tuned by a DeskReactionSO: a short animation
/// (ReactionCurve, relative to the rest pose captured at Awake), an optional
/// sound (when the reaction has a clip and the prop an AudioSource) and an
/// optional tooltip that shows a live readout's text (the calendar's day, the
/// till's money in the wallet's word...) through the overlay tooltip callout.
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

    private Vector3 _restPosition;
    private Quaternion _restRotation;
    private Vector3 _restScale;
    private float _elapsed;
    private bool _animating;

    private void Awake()
    {
        _restPosition = transform.localPosition;
        _restRotation = transform.localRotation;
        _restScale = transform.localScale;
        GetComponent<Clickable>().onClick.AddListener(Play);
    }

    /// <summary>Plays the reaction: the animation, the clip when there is one and a source, and the tooltip when its key is not blank.</summary>
    public void Play()
    {
        if (reaction == null)
            return;

        _elapsed = 0f;
        _animating = reaction.kind != ReactionKind.None;

        if (reaction.clip != null && audioSource != null)
            audioSource.PlayOneShot(reaction.clip);

        if (tooltip != null && !string.IsNullOrWhiteSpace(reaction.tooltipKey))
            tooltip.Show(UiText.Format(reaction.tooltipKey, readout != null ? readout.text : string.Empty, UiText.Currency(UiText.WalletForm.Label)),
                         transform, reaction.tooltipOffset, reaction.tooltipSeconds);
    }

    /// <summary>Only while animating: the pose relative to the rest pose; the rest pose again at the end.</summary>
    private void Update()
    {
        if (!_animating)
            return;

        _elapsed += Time.deltaTime;
        float t = _elapsed / reaction.seconds;
        if (t >= 1f)
        {
            transform.localPosition = _restPosition;
            transform.localRotation = _restRotation;
            transform.localScale = _restScale;
            _animating = false;
            return;
        }

        ReactionPose pose = ReactionCurve.Evaluate(reaction.kind, t, reaction.amplitude);
        transform.localPosition = _restPosition + new Vector3(0f, pose.OffsetY, 0f);
        transform.localRotation = _restRotation * Quaternion.Euler(0f, 0f, pose.AngleDeg);
        transform.localScale = new Vector3(_restScale.x * pose.ScaleX, _restScale.y * pose.ScaleY, _restScale.z);
    }
}
