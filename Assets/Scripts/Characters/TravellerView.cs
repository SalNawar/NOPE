using UnityEngine;

/// <summary>
/// The traveller in the office: the layered figure standing behind the desk,
/// shown from presentation until the decision (GameManager.SetTravellerAtDesk),
/// empty otherwise. The office binder stands it at the art office's traveller
/// anchor (Stand: a billboard facing the office camera, sized in metres and
/// tinted into the room's light, since the art is unlit). Its anchor (the
/// figure's shoulders) is the one point the traveller wheel's ring and the
/// speech bubble centre on; its hit zone (the figure above the desk) opens the wheel.
/// </summary>
public sealed class TravellerView : MonoBehaviour
{
    /// <summary>How far above the head top (canvas px) the hit zone reaches: tall hats and buns.</summary>
    private const int HatReach = 120;

    /// <summary>The layered figure (Figure child: a SortingGroup with one renderer per LookLayer; one canvas unit tall, feet at its origin).</summary>
    [SerializeField] private LookSpriteStack figure;

    /// <summary>Where the wheel and the bubble centre (the traveller's shoulders).</summary>
    [SerializeField] private Transform anchor;

    /// <summary>The click box over the figure (opens the wheel).</summary>
    [SerializeField] private BoxCollider hitZone;

    /// <summary>Where the wheel and the bubble centre.</summary>
    public Transform Anchor => anchor;

    /// <summary>The head in world space, from the shoulders to the top of the head (a match line meets the face there; the desk-first redesign, item 11).</summary>
    public Bounds Face
    {
        get
        {
            if (figure == null)
                return new Bounds(transform.position, Vector3.zero);
            Transform body = figure.transform;
            Vector3 shoulders = body.TransformPoint(0f, LookCanvas.LocalY(LookCanvas.Shoulders), 0f);
            Vector3 top = body.TransformPoint(0f, LookCanvas.LocalY(LookCanvas.HeadTop), 0f);
            float size = Vector3.Distance(shoulders, top);
            return new Bounds((shoulders + top) / 2f, new Vector3(size * 0.8f, size, size * 0.1f));
        }
    }

    /// <summary>The top of the head in world space (the speech bubble sits just above it: BubbleLayout).</summary>
    public Vector3 HeadTop => Landmark(LookCanvas.HeadTop);

    /// <summary>The mouth in world space (the speech bubble's tail points at it).</summary>
    public Vector3 Mouth => Landmark(LookCanvas.Mouth);

    /// <summary>A landmark of the look's canvas (pixels from its top) on the figure's centre line, in world space (the anchor without a figure).</summary>
    private Vector3 Landmark(int canvasY) =>
        figure != null ? figure.transform.TransformPoint(0f, LookCanvas.LocalY(canvasY), 0f) : anchor != null ? anchor.position : transform.position;

    private void Awake() => Clear();

    /// <summary>
    /// Stands the traveller with their feet at <paramref name="feet"/>, turned
    /// to face <paramref name="viewer"/> (around world up only),
    /// <paramref name="height"/> metres from the soles to the top of the head,
    /// tinted <paramref name="tint"/>; the anchor moves to the shoulders and the
    /// hit zone covers the figure from <paramref name="visibleFromY"/> (the desk
    /// top that hides the legs) up past the head.
    /// </summary>
    public void Stand(Vector3 feet, Vector3 viewer, float height, Color tint, float visibleFromY)
    {
        Vector3 away = feet - viewer;
        away.y = 0f;
        transform.SetPositionAndRotation(feet, away.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(away, Vector3.up) : Quaternion.identity);

        if (figure == null)
            return;

        Transform body = figure.transform;
        body.localPosition = Vector3.zero;
        body.localRotation = Quaternion.identity;
        body.localScale = Vector3.one * (height / LookCanvas.LocalY(LookCanvas.HeadTop));
        figure.SetTint(tint);

        if (anchor != null)
            anchor.position = body.TransformPoint(0f, LookCanvas.LocalY(LookCanvas.Shoulders), 0f);

        if (hitZone != null)
        {
            float scale = body.localScale.y;
            float halfWidth = LookCanvas.LocalX(LookCanvas.CenterX + LookCanvas.ArmReach) * scale;
            float top = LookCanvas.LocalY(LookCanvas.HeadTop - HatReach) * scale;
            float bottom = Mathf.Clamp(visibleFromY - feet.y, 0f, top);
            hitZone.center = new Vector3(0f, (top + bottom) / 2f, 0f);
            hitZone.size = new Vector3(2f * halfWidth, top - bottom, 0.1f);
        }
    }

    /// <summary>Re-tints the figure (the anime hall's time of day, AnimeHallShiftLink: the art is unlit, the tint sits it into the hall's light).</summary>
    public void Tint(Color tint)
    {
        if (figure != null)
            figure.SetTint(tint);
    }

    /// <summary>The pending leave (Leave), or null.</summary>
    private Coroutine _leaving;

    /// <summary>What runs when the traveller has left (Leave), or null.</summary>
    private System.Action _left;

    /// <summary>
    /// The traveller leaves after <paramref name="seconds"/> (their reaction's
    /// linger, the personalities spec's R4): the figure clears and
    /// <paramref name="left"/> runs; at once for 0 or less (or while this view
    /// is inactive). Showing the next traveller ends the linger at once.
    /// </summary>
    public void Leave(float seconds, System.Action left)
    {
        EndLinger();
        if (seconds <= 0f || !isActiveAndEnabled)
        {
            Clear();
            left?.Invoke();
            return;
        }
        _left = left;
        _leaving = StartCoroutine(LeaveAfter(seconds));
    }

    private System.Collections.IEnumerator LeaveAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _leaving = null;
        EndLinger();
    }

    /// <summary>Ends a pending leave now: the figure clears and its callback runs (nothing when none is pending).</summary>
    public void EndLinger()
    {
        if (_leaving != null)
        {
            StopCoroutine(_leaving);
            _leaving = null;
        }
        System.Action left = _left;
        _left = null;
        if (left != null)
        {
            Clear();
            left();
        }
    }

    /// <summary>Shows the traveller's look (presentation), ending a previous traveller's linger first; a null look or art shows nobody.</summary>
    public void Show(TravellerLook look, CharacterArt art)
    {
        EndLinger();
        if (figure != null)
            figure.Show(look, art);
    }

    /// <summary>A premade's picture changes to what they say (TravellerWheel, as each line with an expression starts in the speech bubble).</summary>
    public void SetExpression(string expression)
    {
        if (figure != null)
            figure.SetExpression(expression);
    }

    /// <summary>The traveller takes a pose's frame on a dialogue beat (TravellerWheel; TravellerPose: an instant swap, held until the next beat; Show starts neutral).</summary>
    public void SetPose(string category)
    {
        if (figure != null)
            figure.SetPose(category);
    }

    /// <summary>Hides the traveller (the decision, or no traveller yet).</summary>
    public void Clear()
    {
        if (figure != null)
            figure.Clear();
    }
}
