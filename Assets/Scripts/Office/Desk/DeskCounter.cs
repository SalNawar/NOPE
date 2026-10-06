using TMPro;
using UnityEngine;

/// <summary>
/// The counter (Papers, Please's, Saleh 2026-10-06: "the counter
/// (traveller's side) shows documents small; the desk shows them full size;
/// dragging a document back onto the counter hands it back"; then "this top
/// line is super annoying, it keeps shrinking the documents as I try to
/// stamp ... reduce the size of the top counter line"): a slim strip of the
/// desk's clamp area at its far edge along the office view
/// (DeskConfigSO.counterDepth deep), ending where the reading view shows the
/// desk at ViewTop of the screen's height, so it is always reachable while
/// reading. Papers handed over land small in a row along it
/// (Spot, DeskConfigSO.counterSpotInset from its far edge); a paper dropped
/// with the pointer on it (Contains) hands the papers back once the passport
/// carries its verdict and bounces back to the desk before that
/// (DeskPapers.Drop). It is drawn as an ivory see-through strip with its name
/// ("COUNTER"), with "▲ HAND BACK ▲", brighter, once the passport carries its
/// verdict, and lit while a dragged paper's pointer is over it (Show's
/// hover: the only cue, a dragged paper keeps its size there) with "STAMP
/// THE PASSPORT FIRST" before the verdict. The office binder binds it once
/// the desk and the reading view are placed; Build Office UI builds its
/// strip and label. The geometry is DeskZones'.
/// </summary>
public sealed class DeskCounter : MonoBehaviour
{
    /// <summary>The desk tuning (the counter's depth, spots and spacing).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The desk plane (its clamp area's far edge is the counter's).</summary>
    [SerializeField] private DeskSurface surface;

    /// <summary>The reading view (optional): the counter ends where it shows the desk at ViewTop of the screen.</summary>
    [SerializeField] private DeskView deskView;

    /// <summary>The strip's root (shown while a traveller is at the desk).</summary>
    [SerializeField] private GameObject zone;

    /// <summary>The strip's quad (sized at Bind to the counter: as wide as the desk allows either side of the view's centre line, counterDepth deep).</summary>
    [SerializeField] private Renderer strip;

    /// <summary>The strip's label: the counter's name, "▲ HAND BACK ▲" once the passport carries its verdict, or "STAMP THE PASSPORT FIRST" while a paper is dragged over it before that.</summary>
    [SerializeField] private TMP_Text label;

    /// <summary>The strip's tint while it is only the counter, and while the stamped passport can be handed back there.</summary>
    [SerializeField] private Color plainTint = new Color(1f, 0.96f, 0.84f, 0.14f), handBackTint = new Color(1f, 0.96f, 0.84f, 0.34f);

    /// <summary>The strip's tint while a dragged paper's pointer is over it: with the verdict (a drop hands the papers back), and before it (a drop bounces back).</summary>
    [SerializeField] private Color hoverHandBackTint = new Color(1f, 0.96f, 0.84f, 0.6f), hoverRefusedTint = new Color(0.95f, 0.55f, 0.45f, 0.45f);

    /// <summary>How far above the desk the strip lies (metres), over the desk's own top and under the papers.</summary>
    private const float ZoneLift = 0.0004f;

    /// <summary>The highest the counter reaches on the screen in the reading view (a share of its height from the bottom): under the overlay's top controls.</summary>
    private const float ViewTop = 0.9f;

    /// <summary>How far from the counter's ends the spots keep a paper's centre (metres).</summary>
    private const float EndMargin = 0.08f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private Vector3 _forward = Vector3.forward;
    private Vector3 _right = Vector3.right;
    private MaterialPropertyBlock _block;
    private bool _handBack;
    private bool _hover;

    /// <summary>Lays the counter along the office view's level <paramref name="levelForward"/> (the office binder, once the desk and the reading view are placed).</summary>
    public void Bind(Vector3 levelForward)
    {
        Vector3 forward = Vector3.ProjectOnPlane(levelForward, Vector3.up);
        if (forward.sqrMagnitude < 1e-6f)
            forward = Vector3.forward;
        _forward = forward.normalized;
        _right = Vector3.Cross(Vector3.up, _forward);
        if (zone == null || !Edges(out float far, out float left, out float right, out float middle))
            return;
        float depth = config.counterDepth;
        Vector3 centre = surface.transform.position + _right * middle + _forward * (far - depth / 2f) + Vector3.up * ZoneLift;
        zone.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(_forward, Vector3.up));
        if (strip != null)
            strip.transform.localScale = new Vector3(2f * Mathf.Min(middle - left, right - middle), depth, 1f);
    }

    /// <summary>True when <paramref name="point"/> (on the desk) lies on the counter (DeskZones.OnCounter); false without a desk.</summary>
    public bool Contains(Vector3 point) =>
        Edges(out float far, out _, out _, out _) && DeskZones.OnCounter(Vector3.Dot(point - surface.transform.position, _forward), far, config.counterDepth);

    /// <summary>Where paper <paramref name="k"/> of the counter's spots lands, on the desk plane (DeskZones.CounterSpot: a row along the counter, counterSpotInset from its far edge, round the view's centre line); the desk's centre without a desk.</summary>
    public Vector3 Spot(int k)
    {
        if (!Edges(out float far, out float left, out float right, out float middle))
            return surface != null ? surface.transform.position : transform.position;
        (float x, float y) = DeskZones.CounterSpot(k, config.counterSpots, middle, left + EndMargin, right - EndMargin, far, config.counterSpotInset, config.counterSpacing);
        return surface.transform.position + _right * x + _forward * y;
    }

    /// <summary>Shows the strip (<paramref name="shown"/>: a traveller is at the desk) with the counter's name, or "▲ HAND BACK ▲" brighter (<paramref name="handBack"/>: the passport carries its verdict); lit while a dragged paper's pointer is over it (<paramref name="hover"/>), reading "STAMP THE PASSPORT FIRST" before the verdict.</summary>
    public void Show(bool shown, bool handBack, bool hover = false)
    {
        if (zone != null && zone.activeSelf != shown)
            zone.SetActive(shown);
        if (handBack == _handBack && hover == _hover && label != null && !string.IsNullOrEmpty(label.text))
            return;
        _handBack = handBack;
        _hover = hover;
        if (label != null)
            label.text = UiText.Get(handBack ? "stamp.zone.handBack" : hover ? "stamp.zone.stampFirst" : "desk.counter");
        if (strip != null)
        {
            _block ??= new MaterialPropertyBlock();
            strip.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, hover ? (handBack ? hoverHandBackTint : hoverRefusedTint) : handBack ? handBackTint : plainTint);
            strip.SetPropertyBlock(_block);
        }
    }

    /// <summary>The counter's far edge along the office view (the desk's clamp area's far edge, or nearer: as far as the reading view shows, ViewTop), the desk's left and right ends and the counter's middle across (the reading view's centre line, else the desk's), in metres from the desk's centre; false without a desk.</summary>
    private bool Edges(out float far, out float left, out float right, out float middle)
    {
        far = right = float.MinValue;
        left = float.MaxValue;
        middle = 0f;
        if (surface == null || config == null)
            return false;
        Vector3 origin = surface.transform.position;
        foreach (Vector3 corner in surface.Corners())
        {
            far = Mathf.Max(far, Vector3.Dot(corner - origin, _forward));
            left = Mathf.Min(left, Vector3.Dot(corner - origin, _right));
            right = Mathf.Max(right, Vector3.Dot(corner - origin, _right));
        }
        middle = (left + right) / 2f;
        if (deskView != null && deskView.TryViewPoint(new Vector2(0.5f, ViewTop), origin.y, out Vector3 shown))
        {
            far = Mathf.Min(far, Vector3.Dot(shown - origin, _forward));
            middle = Mathf.Clamp(Vector3.Dot(shown - origin, _right), left, right);
        }
        return true;
    }
}
