using TMPro;
using UnityEngine;

/// <summary>
/// A timed label on the office overlay canvas that takes no clicks, projected
/// at a followed transform plus an offset (OverlayProjection): the traveller's
/// speech bubble (TravellerWheel, which types each line out with Reveal and
/// times it itself, and flips a translated line through its Label) and the
/// desk props' tooltips (DeskReaction). The host stays active; its Panel child
/// is shown and hidden. It hides when its time is up, when the followed object
/// is destroyed, or when that object leaves the view (unless it keeps on
/// screen, as the speech bubble does: it then waits at the screen's edge).
/// Callers apply DisplayText.
/// </summary>
public sealed class OverlayCallout : MonoBehaviour
{
    /// <summary>The shown box (anchors and pivot (0.5, 0.5) under this full-screen host; raycast targets off).</summary>
    [SerializeField] private RectTransform panel;

    /// <summary>The box's text.</summary>
    [SerializeField] private TMP_Text label;

    /// <summary>True when the box stays at the screen's edge while its object is out of view (the speech bubble: the desk view tilts the traveller's head above the top); false hides it (the tooltips).</summary>
    [SerializeField] private bool keepOnScreen;

    private Camera _camera;
    private RectTransform _canvasRect;

    /// <summary>The callout's text, for a caller that animates it (the wheel's translation flip).</summary>
    public TMP_Text Label => label;
    private Transform _follow;
    private Vector2 _offset;
    private float _remaining;

    /// <summary>Resolves the overlay canvas once, and hides the box (never this host). The office camera comes from the office binder (SetCamera).</summary>
    private void Awake()
    {
        _canvasRect = OverlayProjection.CanvasRectOf(this);
        Hide();
    }

    /// <summary>The office camera the box is placed through (the office binder's, from the art office).</summary>
    public void SetCamera(Camera office) => _camera = office;

    /// <summary>
    /// Shows all of <paramref name="text"/> (as given) over <paramref name="follow"/>
    /// plus <paramref name="offset"/> for <paramref name="seconds"/> (infinity:
    /// until hidden), restarting the timer; a blank text or a null follow hides it.
    /// </summary>
    public void Show(string text, Transform follow, Vector2 offset, float seconds)
    {
        if (string.IsNullOrWhiteSpace(text) || follow == null || panel == null)
        {
            Hide();
            return;
        }

        if (label != null)
        {
            label.text = text;
            label.maxVisibleCharacters = text.Length;
        }
        _follow = follow;
        _offset = offset;
        _remaining = seconds;
        panel.gameObject.SetActive(true);
        if (!OverlayProjection.TryPlace(panel, _canvasRect, _camera, follow.position, offset, keepOnScreen))
            Hide();
    }

    /// <summary>Shows only the first <paramref name="characters"/> characters of the text (the rest keep their place, so the box does not reflow as a line types out).</summary>
    public void Reveal(int characters)
    {
        if (label != null)
            label.maxVisibleCharacters = characters < 0 ? 0 : characters;
    }

    /// <summary>Hides the box.</summary>
    public void Hide()
    {
        _follow = null;
        if (panel != null)
            panel.gameObject.SetActive(false);
    }

    /// <summary>Only while shown: counts down and follows the object.</summary>
    private void LateUpdate()
    {
        if (panel == null || !panel.gameObject.activeSelf)
            return;

        _remaining -= Time.deltaTime;
        if (_remaining <= 0f || _follow == null || !OverlayProjection.TryPlace(panel, _canvasRect, _camera, _follow.position, _offset, keepOnScreen))
            Hide();
    }
}
