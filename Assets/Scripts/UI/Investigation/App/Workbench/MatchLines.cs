using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The line between two compared values (the PC workbench spec §4.1; the
/// prototype's labelled line, Papers, Please style), drawn over the two
/// panes: a curve from the side of each value's box facing the other one,
/// dots at both ends, the two boxes outlined, all in the result's colour,
/// and its label on a plate at the curve's middle. While a value is held the
/// line runs dashed, in the holding colour, from the held box to the
/// pointer. It follows the boxes every frame (a page scrolled, a pane
/// resized) and hides while either box is out of its pane's view. Draws
/// nothing by itself: MatchBoard says what to draw. Never a raycast target.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MatchLines : MaskableGraphic
{
    [Header("Label")]
    /// <summary>The label's plate (its colour is the line's).</summary>
    [SerializeField] private Image labelPlate;

    /// <summary>The label's text (white on the plate).</summary>
    [SerializeField] private TMP_Text labelText;

    [Header("Look (the theme's: ApplyTheme)")]
    /// <summary>A match, a rule met, a date that holds.</summary>
    [SerializeField] private Color matchColour = new Color(0.184f, 0.369f, 0.2f, 1f);

    /// <summary>A difference, a rule broken, a date that fails.</summary>
    [SerializeField] private Color differColour = new Color(0.604f, 0.18f, 0.173f, 1f);

    /// <summary>A note: nothing logged (dashed).</summary>
    [SerializeField] private Color infoColour = new Color(0.435f, 0.431f, 0.416f, 1f);

    /// <summary>The held value and the dashed line to the pointer.</summary>
    [SerializeField] private Color holdColour = new Color(0.122f, 0.424f, 0.624f, 1f);

    [Header("Geometry (desktop units)")]
    /// <summary>The line's width.</summary>
    [SerializeField, Min(1f)] private float width = 3f;

    /// <summary>A box's outline width.</summary>
    [SerializeField, Min(1f)] private float outline = 2f;

    /// <summary>An end dot's size.</summary>
    [SerializeField, Min(1f)] private float dot = 9f;

    /// <summary>A dash and its gap along a dashed line.</summary>
    [SerializeField] private Vector2 dash = new Vector2(12f, 9f);

    private const int Samples = 48;
    private readonly Vector3[] _corners = new Vector3[4];
    private readonly Vector2[] _points = new Vector2[Samples + 1];
    private RectTransform _a;
    private RectTransform _b;
    private FindingLook _look;
    private bool _holding;
    private bool _drawn;

    /// <summary>True while a line or a held value is drawn.</summary>
    public bool Showing => _drawn;

    /// <summary>The theme's colours (CultureThemeService, from the FindingMatch, FindingDiffer, Info and Holding roles' inks).</summary>
    public void ApplyTheme(Color match, Color differ, Color info, Color hold)
    {
        matchColour = match;
        differColour = differ;
        infoColour = info;
        holdColour = hold;
        SetVerticesDirty();
    }

    /// <summary>Joins the boxes <paramref name="a"/> and <paramref name="b"/> with a line of <paramref name="look"/> labelled <paramref name="label"/>.</summary>
    public void ShowLink(RectTransform a, RectTransform b, FindingLook look, string label)
    {
        _a = a;
        _b = b;
        _look = look;
        _holding = false;
        if (labelText != null)
            labelText.text = label ?? string.Empty;
        Refresh();
    }

    /// <summary>Draws the held box <paramref name="held"/> (null: held somewhere off the PC, nothing to draw) and the dashed line from it to the pointer.</summary>
    public void ShowHold(RectTransform held)
    {
        _a = held;
        _b = null;
        _holding = true;
        Refresh();
    }

    /// <summary>Draws nothing.</summary>
    public void Clear()
    {
        _a = null;
        _b = null;
        _holding = false;
        Refresh();
    }

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    /// <summary>The boxes move with their pages: redrawn every frame while something shows.</summary>
    private void LateUpdate()
    {
        if (_a != null || _drawn)
            Refresh();
    }

    /// <summary>Redraws the mesh and places the label.</summary>
    private void Refresh()
    {
        SetVerticesDirty();
        bool link = !_holding && Visible(_a) && Visible(_b);
        if (labelPlate != null && labelPlate.gameObject.activeSelf != link)
            labelPlate.gameObject.SetActive(link);
        if (!link || labelPlate == null)
            return;
        labelPlate.color = Colour(_look);
        Rect ra = Local(_a), rb = Local(_b);
        Curve(Anchor(ra, rb.center.x), Anchor(rb, ra.center.x));
        var plate = (RectTransform)labelPlate.transform;
        plate.anchoredPosition = _points[Samples / 2];
    }

    /// <summary>The line, its dots and the outlined boxes (or the held box and the dashed line to the pointer).</summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        _drawn = false;
        if (_holding)
        {
            if (!Visible(_a))
                return;
            Rect held = Local(_a);
            Color c = holdColour;
            Box(vh, held, c);
            if (TryPointer(out Vector2 pointer))
            {
                Vector2 from = Anchor(held, pointer.x);
                Curve(from, pointer);
                Stroke(vh, c, true);
                Dot(vh, from, c);
                Dot(vh, pointer, c, 0.7f);
            }
            _drawn = true;
            return;
        }
        if (!Visible(_a) || !Visible(_b))
            return;
        Rect ra = Local(_a), rb = Local(_b);
        Color colour = Colour(_look);
        Vector2 p = Anchor(ra, rb.center.x), q = Anchor(rb, ra.center.x);
        Curve(p, q);
        Stroke(vh, colour, _look == FindingLook.Info);
        Box(vh, ra, colour);
        Box(vh, rb, colour);
        Dot(vh, p, colour);
        Dot(vh, q, colour);
        _drawn = true;
    }

    /// <summary>The colour of a result.</summary>
    private Color Colour(FindingLook look) => look == FindingLook.Match ? matchColour : look == FindingLook.Differ ? differColour : infoColour;

    /// <summary>True when the box is on screen: alive, shown, and its centre inside the view its page scrolls in.</summary>
    private bool Visible(RectTransform box)
    {
        if (box == null || !box.gameObject.activeInHierarchy)
            return false;
        RectMask2D clip = box.GetComponentInParent<RectMask2D>();
        if (clip == null)
            return true;
        box.GetWorldCorners(_corners);
        Vector3 centre = (_corners[0] + _corners[2]) / 2f;
        var view = (RectTransform)clip.transform;
        Vector2 local = view.InverseTransformPoint(centre);
        return view.rect.Contains(local);
    }

    /// <summary>A box's rect in this graphic's space.</summary>
    private Rect Local(RectTransform box)
    {
        box.GetWorldCorners(_corners);
        Vector2 min = rectTransform.InverseTransformPoint(_corners[0]);
        Vector2 max = rectTransform.InverseTransformPoint(_corners[2]);
        return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
    }

    /// <summary>The middle of the box's side facing <paramref name="towardX"/>.</summary>
    private static Vector2 Anchor(Rect box, float towardX) => new Vector2(towardX >= box.center.x ? box.xMax : box.xMin, box.center.y);

    /// <summary>The pointer in this graphic's space (the desktop canvas's camera maps the screen).</summary>
    private bool TryPointer(out Vector2 local)
    {
        local = default;
        Mouse mouse = Mouse.current;
        if (mouse == null || canvas == null)
            return false;
        Camera cam = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mouse.position.ReadValue(), cam, out local);
    }

    /// <summary>Samples the curve from <paramref name="p"/> to <paramref name="q"/> (horizontal tangents, as the prototype's) into the points.</summary>
    private void Curve(Vector2 p, Vector2 q)
    {
        float dx = Mathf.Max(40f, Mathf.Abs(q.x - p.x) * 0.45f) * (q.x >= p.x ? 1f : -1f);
        Vector2 c1 = p + new Vector2(dx, 0f), c2 = q - new Vector2(dx, 0f);
        for (int i = 0; i <= Samples; i++)
        {
            float t = i / (float)Samples, u = 1f - t;
            _points[i] = u * u * u * p + 3f * u * u * t * c1 + 3f * u * t * t * c2 + t * t * t * q;
        }
    }

    /// <summary>The sampled curve as a strip of quads (<paramref name="dashed"/>: dashes and gaps along its length).</summary>
    private void Stroke(VertexHelper vh, Color c, bool dashed)
    {
        float along = 0f, period = dash.x + dash.y;
        for (int i = 0; i < Samples; i++)
        {
            Vector2 a = _points[i], b = _points[i + 1];
            float length = Vector2.Distance(a, b);
            bool on = !dashed || (along % period) < dash.x;
            along += length;
            if (on && length > 0.01f)
                Segment(vh, a, b, c);
        }
    }

    /// <summary>One straight piece of the stroke.</summary>
    private void Segment(VertexHelper vh, Vector2 a, Vector2 b, Color c)
    {
        Vector2 n = (b - a).normalized;
        Vector2 side = new Vector2(-n.y, n.x) * (width / 2f);
        Vector2 ext = n * (width / 2f);
        Quad(vh, a - ext - side, a - ext + side, b + ext + side, b + ext - side, c);
    }

    /// <summary>A box's outline, just outside it.</summary>
    private void Box(VertexHelper vh, Rect r, Color c)
    {
        float w = outline;
        Rect o = Rect.MinMaxRect(r.xMin - w, r.yMin - w, r.xMax + w, r.yMax + w);
        Quad(vh, new Vector2(o.xMin, o.yMax - w), new Vector2(o.xMin, o.yMax), new Vector2(o.xMax, o.yMax), new Vector2(o.xMax, o.yMax - w), c);
        Quad(vh, new Vector2(o.xMin, o.yMin), new Vector2(o.xMin, o.yMin + w), new Vector2(o.xMax, o.yMin + w), new Vector2(o.xMax, o.yMin), c);
        Quad(vh, new Vector2(o.xMin, o.yMin), new Vector2(o.xMin, o.yMax), new Vector2(o.xMin + w, o.yMax), new Vector2(o.xMin + w, o.yMin), c);
        Quad(vh, new Vector2(o.xMax - w, o.yMin), new Vector2(o.xMax - w, o.yMax), new Vector2(o.xMax, o.yMax), new Vector2(o.xMax, o.yMin), c);
    }

    /// <summary>An end dot (an octagon) at <paramref name="at"/>.</summary>
    private void Dot(VertexHelper vh, Vector2 at, Color c, float scale = 1f)
    {
        float r = dot * scale / 2f;
        int start = vh.currentVertCount;
        vh.AddVert(at, c, Vector4.zero);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            vh.AddVert(at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, c, Vector4.zero);
        }
        for (int i = 0; i < 8; i++)
            vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 8);
    }

    /// <summary>A quad from four corners in order.</summary>
    private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color colour)
    {
        int start = vh.currentVertCount;
        vh.AddVert(a, colour, Vector4.zero);
        vh.AddVert(b, colour, Vector4.zero);
        vh.AddVert(c, colour, Vector4.zero);
        vh.AddVert(d, colour, Vector4.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }
}
