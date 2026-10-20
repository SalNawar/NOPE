using UnityEngine;

/// <summary>
/// A passport's cover (Saleh's D3: "yes" to the passport and folder opening and
/// closing; FIX_PLAN DOC-LOOK: the passport arrives closed and opens on its
/// first pick-up with a page flip; GAME_FEEL_BIBLE A1). A child of the paper's
/// sheet: a hinge on the booklet's spine carrying the cover's front (the
/// holder's nation's colour with its emblem in gold) and, back to back with
/// it, the visa page's art (the face's upper half), so the swing turns the
/// visa page down onto its place. Shut, it lies over the data page and the
/// booklet shows only that half (DeskDocument.ShowShut); it opens as soon as
/// it is picked up or lies on the desk, and shuts again lying on the counter
/// (the hand-back), on unscaled time (BookletFlip: a 15 degree lift, then
/// the swing with an overshoot; at once under Reduced Motion). Every paper
/// but a booklet hides it.
/// </summary>
public sealed class PaperCover : MonoBehaviour
{
    /// <summary>The hinge on the spine (it turns about its x axis: 0 shut, 180 open).</summary>
    [SerializeField] private Transform hinge;

    /// <summary>The cover's front (a unit quad, the nation's colour).</summary>
    [SerializeField] private Renderer front;

    /// <summary>The nation's emblem on the front (a unit quad, gold).</summary>
    [SerializeField] private Renderer emblem;

    /// <summary>The visa page's art on the cover's back (a unit quad, turned to face away while shut).</summary>
    [SerializeField] private Renderer back;

    /// <summary>The emblem's gold on the cover.</summary>
    [SerializeField] private Color emblemInk = new Color(0.86f, 0.72f, 0.38f, 1f);

    /// <summary>The emblem's size as a share of the cover's width.</summary>
    [SerializeField, Range(0.1f, 0.9f)] private float emblemShare = 0.42f;

    /// <summary>How far over the sheet the shut cover lies (metres toward the viewer): over every printed part (PaperLayers.PartsDepth) and the stack's gap.</summary>
    public const float Lift = PaperLayers.PartsDepth + PaperLayers.StackGap;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");

    private readonly BookletFlip _flip = new BookletFlip();
    private DeskDocument _paper;
    private DeskDraggable _drag;
    private float _spine;
    private bool _shown, _shut;
    private MaterialPropertyBlock _block;

    /// <summary>
    /// A booklet arrives shut: the cover lies over its data page (below
    /// <paramref name="spine"/>, sheet metres, on a sheet <paramref name="size"/>
    /// metres), in <paramref name="colour"/> with <paramref name="mark"/> (the
    /// nation's emblem; null: none), the visa page's half of
    /// <paramref name="face"/> on its back. <paramref name="paper"/> and its
    /// <paramref name="drag"/> say when it opens.
    /// </summary>
    public void Arrive(DeskDocument paper, DeskDraggable drag, float spine, Vector2 size, Color colour, Texture2D mark, Texture2D face)
    {
        _paper = paper;
        _drag = drag;
        _spine = spine;
        _shown = true;
        gameObject.SetActive(true);
        float halfWidth = size.x / 2f, below = spine + size.y / 2f, above = size.y / 2f - spine;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        if (hinge != null)
            hinge.localPosition = new Vector3(0f, spine, -Lift);
        _block ??= new MaterialPropertyBlock();
        if (front != null)
        {
            front.transform.localPosition = new Vector3(0f, -below / 2f, 0f);
            front.transform.localScale = new Vector3(halfWidth * 2f, below, 1f);
            front.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, colour);
            front.SetPropertyBlock(_block);
        }
        if (emblem != null)
        {
            float side = halfWidth * 2f * emblemShare;
            emblem.gameObject.SetActive(mark != null);
            emblem.transform.localPosition = new Vector3(0f, -below / 2f, -PaperLayers.Seal);
            emblem.transform.localScale = new Vector3(side, side, 1f);
            if (mark != null)
            {
                emblem.GetPropertyBlock(_block);
                _block.SetTexture(BaseMapId, mark);
                _block.SetColor(BaseColorId, emblemInk);
                emblem.SetPropertyBlock(_block);
            }
        }
        if (back != null)
        {
            // turned to face away while shut; the swing turns it up the right way where the visa page lies
            back.transform.localPosition = new Vector3(0f, -above / 2f, PaperLayers.Seal);
            back.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            back.transform.localScale = new Vector3(halfWidth * 2f, above, 1f);
            back.GetPropertyBlock(_block);
            if (face != null)
                _block.SetTexture(BaseMapId, face);
            _block.SetColor(BaseColorId, Color.white);
            float v0 = (spine + size.y / 2f) / Mathf.Max(size.y, 1e-4f);
            _block.SetVector(BaseMapStId, new Vector4(1f, 1f - v0, 0f, v0));
            back.SetPropertyBlock(_block);
        }
        _flip.Shut();
        _shut = false;
        Show(true);
    }

    /// <summary>No cover (a paper that is no booklet).</summary>
    public void Hide()
    {
        _shown = false;
        _paper = null;
        gameObject.SetActive(false);
    }

    /// <summary>Opens on a pick-up or lying on the desk, shuts lying on the counter; only while it changes.</summary>
    private void Update()
    {
        if (!_shown || _paper == null)
            return;
        bool held = _drag != null && _drag.IsDragging;
        bool still = UiMotion.Amount.Still;
        if (held || _paper.Zone == DeskZone.Desk)
            _flip.Open(still);
        else
            _flip.Close(still);
        _flip.Tick(Time.unscaledDeltaTime);
        Show(false);
    }

    /// <summary>The hinge at the cover's angle; the booklet shut (its visa page hidden) unless it lies open.</summary>
    private void Show(bool force)
    {
        if (hinge != null)
        {
            bool swinging = _flip.State != CoverState.Open;
            if (hinge.gameObject.activeSelf != swinging)
                hinge.gameObject.SetActive(swinging);
            hinge.localRotation = Quaternion.Euler(_flip.Angle, 0f, 0f);
        }
        bool shut = _flip.State != CoverState.Open;
        if (shut == _shut && !force)
            return;
        _shut = shut;
        _paper.ShowShut(shut, _spine);
    }
}
