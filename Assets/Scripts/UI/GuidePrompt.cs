using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The guide's prompt on the office overlay (Saleh 2026-10-06: "short,
/// skippable, step-by-step prompts that point at the real thing on screen"):
/// a plate at the bottom centre (a header, one line, and its Skip or Got it
/// button) and an arrow that points at the real thing, placed by the guide
/// director every frame over a world point (through the office camera:
/// OverlayProjection; none while the point is off the screen) or over an
/// overlay control. Only the
/// buttons take clicks, so the prompt never blocks the desk. The director
/// decides what shows; this only draws it.
/// </summary>
public sealed class GuidePrompt : MonoBehaviour
{
    /// <summary>The plate (its header, line and buttons; raycast off but for the buttons).</summary>
    [SerializeField] private RectTransform plate;

    /// <summary>The plate's header ("TUTORIAL 2/8", "NEW TODAY", "PRACTICE").</summary>
    [SerializeField] private TMP_Text header;

    /// <summary>The plate's line.</summary>
    [SerializeField] private TMP_Text line;

    /// <summary>Skip the tutorial (shown with a tutorial step).</summary>
    [SerializeField] private Button skipButton;

    /// <summary>Got it: dismisses a moment, a practice or the tutorial's last line.</summary>
    [SerializeField] private Button okButton;

    /// <summary>The arrow (anchors and pivot (0.5, 0.5) under this full-screen host), its tip at its bottom.</summary>
    [SerializeField] private RectTransform arrow;

    /// <summary>How far above its target the arrow sits (canvas reference px), and how far it bobs.</summary>
    [SerializeField] private float arrowLift = 52f, arrowBob = 8f;

    private RectTransform _canvasRect;

    /// <summary>Raised by Skip.</summary>
    public event Action Skipped;

    /// <summary>Raised by Got it.</summary>
    public event Action Acknowledged;

    /// <summary>True while the plate shows.</summary>
    public bool IsShown => plate != null && plate.gameObject.activeSelf;

    /// <summary>The plate's header as shown (the probe and the audit read it).</summary>
    public string Header => header != null ? header.text : string.Empty;

    /// <summary>The plate's line as shown.</summary>
    public string Line => line != null ? line.text : string.Empty;

    /// <summary>True while the arrow shows.</summary>
    public bool ArrowShown => arrow != null && arrow.gameObject.activeSelf;

    /// <summary>The arrow's place on the overlay (canvas reference px from the centre), for the probe.</summary>
    public Vector2 ArrowAt => arrow != null ? arrow.anchoredPosition : Vector2.zero;

    private void Awake()
    {
        _canvasRect = OverlayProjection.CanvasRectOf(this);
        if (skipButton != null)
            skipButton.onClick.AddListener(() => Skipped?.Invoke());
        if (okButton != null)
            okButton.onClick.AddListener(() => Acknowledged?.Invoke());
        Hide();
    }

    /// <summary>Shows the plate with <paramref name="headerText"/> and <paramref name="lineText"/>, its Skip button when <paramref name="skip"/>, else its Got it button.</summary>
    public void Show(string headerText, string lineText, bool skip)
    {
        if (plate == null)
            return;
        if (header != null)
            header.text = headerText ?? string.Empty;
        if (line != null)
            line.text = lineText ?? string.Empty;
        if (skipButton != null)
            skipButton.gameObject.SetActive(skip);
        if (okButton != null)
            okButton.gameObject.SetActive(!skip);
        plate.gameObject.SetActive(true);
    }

    /// <summary>Hides the plate and the arrow.</summary>
    public void Hide()
    {
        if (plate != null)
            plate.gameObject.SetActive(false);
        HideArrow();
    }

    /// <summary>Hides the arrow (no target on screen).</summary>
    public void HideArrow()
    {
        if (arrow != null)
            arrow.gameObject.SetActive(false);
    }

    /// <summary>Points the arrow down at <paramref name="world"/> through <paramref name="office"/>; hidden while the point is off the screen (an arrow at the edge would point at the plate, not the thing).</summary>
    public void PointAt(Camera office, Vector3 world)
    {
        if (arrow == null)
            return;
        arrow.gameObject.SetActive(true);
        if (!OverlayProjection.TryPlace(arrow, _canvasRect, office, world, new Vector2(0f, arrowLift + Bob())))
            HideArrow();
    }

    /// <summary>Points the arrow down at the top of the overlay control <paramref name="target"/>; hidden while it is hidden.</summary>
    public void PointAt(RectTransform target)
    {
        if (arrow == null || _canvasRect == null || target == null || !target.gameObject.activeInHierarchy)
        {
            HideArrow();
            return;
        }
        Vector3 top = target.TransformPoint(new Vector3(target.rect.center.x, target.rect.yMax, 0f));
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, RectTransformUtility.WorldToScreenPoint(null, top), null, out Vector2 local))
        {
            HideArrow();
            return;
        }
        arrow.gameObject.SetActive(true);
        Rect bounds = _canvasRect.rect;
        Vector2 half = arrow.rect.size * 0.5f;
        Vector2 at = local + new Vector2(0f, arrowLift * 0.5f + half.y + Bob());
        at.x = Mathf.Clamp(at.x, bounds.xMin + half.x, bounds.xMax - half.x);
        at.y = Mathf.Clamp(at.y, bounds.yMin + half.y, bounds.yMax - half.y);
        arrow.anchoredPosition = at;
    }

    /// <summary>The arrow's bob this frame (unscaled time: it bobs while the clock is held).</summary>
    private float Bob() => Mathf.Sin(Time.unscaledTime * 5f) * arrowBob;
}
