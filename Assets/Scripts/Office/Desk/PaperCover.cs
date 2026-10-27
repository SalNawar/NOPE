using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A passport's cover (Saleh's D3: "yes" to the passport and folder opening and
/// closing; FIX_PLAN DOC-LOOK: the passport arrives closed and opens on its
/// first pick-up with a page flip; GAME_FEEL_BIBLE A1). A child of the paper's
/// sheet: a hinge on the booklet's spine carrying the cover's front (the
/// holder's nation's colour with its emblem in gold) and, back to back with
/// it, the visa page's art (the face's upper half), so the swing turns the
/// visa page down onto its place. Shut, it lies over the data page and the
/// booklet shows only that half (DeskDocument.ShowShut); it opens on the
/// press frame of a click on the shut cover (GAME_FEEL_BIBLE A1: "Click on
/// the cover = open on the press frame"; Press, from DeskDocument's pointer
/// down), as it is picked up or as it lies on the desk, and shuts again once
/// it comes back to lie on the counter (the hand-back: only as it arrives
/// there, so a booklet opened by a click on the counter stays open), on
/// unscaled time (BookletFlip: a 15 degree lift, then the swing, its
/// overshoot a rebound off the desk, never through it; at once under Reduced
/// Motion), its free edge bowing up to 3 mm behind the swing (GAME_FEEL_BIBLE
/// A1: a page bow of 2 to 4 mm on the flip; the cover's sheets are cut in a
/// grid, Sheet, so DocumentCel bends them, Bow; the emblem rides the bow,
/// Ride, so the board never comes up through it), with passport_open and
/// passport_close (the sound bank's cues, FEEDBACK's). Every paper but a
/// booklet hides it.
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
    private static readonly int FlexId = Shader.PropertyToID("_Flex"), Flex2Id = Shader.PropertyToID("_Flex2"), FlexSizeId = Shader.PropertyToID("_FlexSize");

    /// <summary>The cover's bow at its swing's height (metres; its free edge bows twice it: 3 mm, GAME_FEEL_BIBLE A1's 2 to 4 mm).</summary>
    public const float FlipBow = 0.0015f;

    /// <summary>How many cells a cover's sheet is cut into along each side (Sheet: DocumentCel bends a mesh at its vertices).</summary>
    public const int SheetCuts = 8;

    /// <summary>The cover sheets' shared grid (Sheet), made once.</summary>
    private static Mesh _sheet;

    /// <summary>The bow last shown (metres; Show sets the sheets' bend only when it changes).</summary>
    private float _bowShown;

    /// <summary>The emblem's place on the flat cover (the hinge's space) and its half size (metres): Ride lifts it off the bowing front.</summary>
    private Vector3 _emblemRest;
    private Vector2 _emblemHalf;

    /// <summary>The sound bank's cues for the cover's swing (SoundBank_Default; GAME_FEEL_BIBLE A1).</summary>
    private const string OpenCue = "passport_open", CloseCue = "passport_close";

    private readonly BookletFlip _flip = new BookletFlip();
    private DeskDocument _paper;
    private DeskDraggable _drag;
    private float _spine;
    private bool _shown, _shut;

    /// <summary>True while the booklet is held or lies on the desk (last frame): coming back to the counter from there shuts it.</summary>
    private bool _out;
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
        UseSheet(front);
        UseSheet(back);
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
            _emblemRest = new Vector3(0f, -below / 2f, -PaperLayers.Seal);
            _emblemHalf = new Vector2(side / 2f, side / 2f);
            emblem.transform.localPosition = _emblemRest;
            emblem.transform.localRotation = Quaternion.identity;
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
        _out = false;
        _bowShown = float.NaN;
        Show(true);
    }

    /// <summary>
    /// A unit quad as Unity's (facing -z, its UVs over it) cut into
    /// SheetCuts by SheetCuts cells, white in its vertex colours (DocumentCel's
    /// flex weight 1 everywhere): a cover's sheet that DocumentCel's bend
    /// (PaperBend.hlsl, vertex by vertex) curls; made once and shared.
    /// </summary>
    public static Mesh Sheet()
    {
        if (_sheet != null)
            return _sheet;
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var colours = new List<Color>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        for (int j = 0; j <= SheetCuts; j++)
            for (int i = 0; i <= SheetCuts; i++)
            {
                float u = (float)i / SheetCuts, v = (float)j / SheetCuts;
                vertices.Add(new Vector3(u - 0.5f, v - 0.5f, 0f));
                uvs.Add(new Vector2(u, v));
                colours.Add(Color.white);
                normals.Add(Vector3.back);
            }
        for (int j = 0; j < SheetCuts; j++)
            for (int i = 0; i < SheetCuts; i++)
            {
                int a = j * (SheetCuts + 1) + i, b = a + SheetCuts + 1;
                triangles.AddRange(new[] { a, b, b + 1, a, b + 1, a + 1 });
            }
        _sheet = new Mesh { name = "CoverSheet", hideFlags = HideFlags.DontSave };
        _sheet.SetVertices(vertices);
        _sheet.SetUVs(0, uvs);
        _sheet.SetColors(colours);
        _sheet.SetNormals(normals);
        _sheet.SetTriangles(triangles, 0);
        _sheet.RecalculateBounds();
        return _sheet;
    }

    /// <summary>Puts the shared grid (Sheet) on a cover sheet's mesh filter.</summary>
    public static void UseSheet(Renderer sheet)
    {
        if (sheet != null && sheet.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != Sheet())
            filter.sharedMesh = Sheet();
    }

    /// <summary>
    /// Bows a cover's <paramref name="sheet"/> (DocumentCel's _Flex, through
    /// <paramref name="block"/>, its other properties kept): its hinge end flat
    /// and its free edge, toward <paramref name="freeEdge"/> in the sheet's own
    /// plane, curled twice <paramref name="bow"/> (metres, along the sheet's
    /// own +z: positive away from its front); none at 0.
    /// </summary>
    public static void Bow(Renderer sheet, MaterialPropertyBlock block, Vector2 freeEdge, float bow)
    {
        if (sheet == null)
            return;
        Vector3 size = sheet.transform.localScale;
        PaperBend bend = CoverBow.Of(freeEdge.x, freeEdge.y, bow);
        sheet.GetPropertyBlock(block);
        block.SetVector(FlexId, new Vector4(bend.DirX, bend.DirY, bend.Bow, bend.Lag));
        block.SetVector(Flex2Id, Vector4.zero);
        block.SetVector(FlexSizeId, new Vector4(Mathf.Abs(size.x), Mathf.Abs(size.y), 0f, 0f));
        sheet.SetPropertyBlock(block);
    }

    /// <summary>
    /// Lays a flat mark printed on a cover's sheet (<paramref name="mark"/>:
    /// the passport's emblem, the folder's seal; at <paramref name="rest"/> on
    /// the flat sheet, both in the hinge's space, <paramref name="half"/>
    /// metres each way) on the bow Bow gives the sheet (its free edge toward
    /// <paramref name="freeEdge"/>, curled twice <paramref name="bow"/>), as a
    /// paper's prints ride its bend: CoverBow.Ride (pure, tested) says how
    /// far to move it along z and how to turn it, so the bowing board never
    /// comes up through the mark; flat, the mark lies at rest.
    /// </summary>
    public static void Ride(Transform mark, Vector3 rest, Vector2 half, Transform sheet, Vector2 freeEdge, float bow)
    {
        if (mark == null || sheet == null)
            return;
        Vector3 place = sheet.localPosition, size = sheet.localScale;
        (float depth, float pitch, float yaw) = CoverBow.Ride(rest.x, rest.y, half.x, half.y, place.x, place.y, size.x, size.y, CoverBow.Of(freeEdge.x, freeEdge.y, bow));
        mark.localPosition = rest + new Vector3(0f, 0f, depth);
        mark.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    /// <summary>A press on the booklet (DeskDocument's pointer down, the left button): a shut cover swings open on the press frame (GAME_FEEL_BIBLE A1); a drag from it works open or closed.</summary>
    public void Press()
    {
        if (!_shown || _paper == null)
            return;
        Swing(_flip.Open(UiMotion.Amount.Still), OpenCue);
        Show(false);
    }

    /// <summary>No cover (a paper that is no booklet).</summary>
    public void Hide()
    {
        _shown = false;
        _paper = null;
        gameObject.SetActive(false);
    }

    /// <summary>Opens on a pick-up or lying on the desk; shuts as it comes back to lie on the counter (from a hold or the desk); the swing on unscaled time.</summary>
    private void Update()
    {
        if (!_shown || _paper == null)
            return;
        bool held = _drag != null && _drag.IsDragging;
        bool still = UiMotion.Amount.Still;
        bool outThere = held || _paper.Zone == DeskZone.Desk;
        if (outThere)
            Swing(_flip.Open(still), OpenCue);
        else if (_out && _paper.Zone == DeskZone.Counter)
            Swing(_flip.Close(still), CloseCue);
        _out = outThere;
        _flip.Tick(Time.unscaledDeltaTime);
        Show(false);
    }

    /// <summary>The swing's cue when one started.</summary>
    private static void Swing(bool started, string cue)
    {
        if (started)
            Sounds.Play(cue);
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
        float bow = FlipBow * _flip.Flex;
        if (bow != _bowShown)
        {
            _bowShown = bow;
            _block ??= new MaterialPropertyBlock();
            // the front's free edge is its bottom; the back is turned over the spine (its free edge its top, its +z the other way)
            Bow(front, _block, Vector2.down, bow);
            Bow(back, _block, Vector2.up, -bow);
            if (emblem != null && front != null)
                Ride(emblem.transform, _emblemRest, _emblemHalf, front.transform, Vector2.down, bow);
        }
        bool shut = _flip.State != CoverState.Open;
        if (shut == _shut && !force)
            return;
        _shut = shut;
        _paper.ShowShut(shut, _spine);
    }
}
