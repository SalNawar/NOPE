using System.Collections.Generic;
using TMPro;
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
/// booklet hides it. The shut cover is printed, not a plain card (Saleh's
/// playtest of 2026-11-07, item 2: "Passport feels like cardboard"): the
/// form's title is printed under the emblem in the emblem's ink (Title, from
/// the paper's text template, shrunk to fit the cover's width) and rides the
/// bow as the emblem does.
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

    /// <summary>The shut booklet's corner radius (metres): the cover's corners and the page block's are round (D63).</summary>
    [SerializeField, Range(0f, 0.01f)] private float cornerRadius = 0.003f;

    /// <summary>The cover boards' thickness on the shut booklet's sides (metres): the front board over the page block, the back board under it (D63).</summary>
    [SerializeField, Range(0.0001f, 0.002f)] private float boardThickness = 0.0006f;

    /// <summary>The cover's title line height as a share of the cover's width (a passport's "PASSPORT" line).</summary>
    [SerializeField, Range(0.03f, 0.2f)] private float titleShare = 0.075f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
    private static readonly int CutoffId = Shader.PropertyToID("_Cutoff");
    private static readonly int FlexId = Shader.PropertyToID("_Flex"), Flex2Id = Shader.PropertyToID("_Flex2"), FlexSizeId = Shader.PropertyToID("_FlexSize");

    /// <summary>The cover's bow at its swing's height (metres; its free edge bows twice it: 3 mm, GAME_FEEL_BIBLE A1's 2 to 4 mm).</summary>
    public const float FlipBow = 0.0015f;

    /// <summary>How many cells a cover's sheet is cut into along each side (Sheet: DocumentCel bends a mesh at its vertices).</summary>
    public const int SheetCuts = 8;

    /// <summary>The cover sheets' shared grid (Sheet), made once.</summary>
    private static Mesh _sheet;

    /// <summary>The title line under the emblem (made on the first Arrive from the paper's text template), its place on the flat cover and its half size.</summary>
    private TextMeshPro _title;
    private Vector3 _titleRest;
    private Vector2 _titleHalf;

    /// <summary>The bow last shown (metres; Show sets the sheets' bend only when it changes).</summary>
    private float _bowShown;

    /// <summary>The emblem's place on the flat cover (the hinge's space) and its half size (metres): Ride lifts it off the bowing front.</summary>
    private Vector3 _emblemRest;
    private Vector2 _emblemHalf;

    /// <summary>The sound bank's cues for the cover's swing (SoundBank_Default; GAME_FEEL_BIBLE A1).</summary>
    private const string OpenCue = "passport_open", CloseCue = "passport_close";

    /// <summary>The shut booklet's sides (Block: the spine and the page block between the two cover boards; D63), made on the first Arrive, and its mesh.</summary>
    private MeshRenderer _blockRenderer;
    private Mesh _blockMesh;

    /// <summary>The front's corner mask and spine crease (FrontMask), made on each Arrive for the cover's size.</summary>
    private Texture2D _frontMask;

    /// <summary>True while the booklet lies fully shut (the block shows and the open model is cut whole; last shown).</summary>
    private bool _closed;

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
    /// <paramref name="face"/> on its back, and <paramref name="title"/> (the
    /// form's title in the reading language; blank: none) printed under the
    /// emblem from <paramref name="textTemplate"/>. <paramref name="paper"/>
    /// and its <paramref name="drag"/> say when it opens. Fully shut it is a
    /// booklet <paramref name="thickness"/> deep under the cover (Block: a
    /// spine and a page block with light page edges on its three open sides,
    /// round corners; D63).
    /// </summary>
    public void Arrive(DeskDocument paper, DeskDraggable drag, float spine, Vector2 size, Color colour, Texture2D mark, Texture2D face, TextMeshPro textTemplate, string title, float thickness)
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
            _block.SetTexture(BaseMapId, FrontMask(halfWidth * 2f, below));
            _block.SetFloat(CutoffId, 0.5f);
            front.SetPropertyBlock(_block);
        }
        Block(halfWidth * 2f, below, spine, thickness, colour);
        float emblemSide = halfWidth * 2f * emblemShare;
        if (emblem != null)
        {
            float side = emblemSide;
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
        if (textTemplate != null && hinge != null)
        {
            if (_title == null)
            {
                _title = Instantiate(textTemplate, hinge);
                _title.name = "Title";
            }
            bool titled = !string.IsNullOrEmpty(title);
            _title.gameObject.SetActive(titled);
            if (titled)
            {
                float line = halfWidth * 2f * titleShare, width = halfWidth * 1.8f;
                TmpFormText.Style(_title, FormTextRole.Title, line);
                _title.fontStyle |= FontStyles.UpperCase;
                _title.text = title;
                _title.color = emblemInk;
                _title.alignment = TextAlignmentOptions.Center;
                _title.textWrappingMode = TextWrappingModes.NoWrap;
                // one line, shrunk to the cover's width: a long title never runs off the cover
                float wide = _title.GetPreferredValues(title).x;
                if (wide > width)
                    _title.fontSize *= width / wide;
                _title.rectTransform.sizeDelta = new Vector2(width, line * 1.5f);
                _titleRest = new Vector3(0f, -below / 2f - emblemSide / 2f - line, -PaperLayers.Seal);
                _titleHalf = new Vector2(halfWidth * 0.9f, line * 0.75f);
                _title.rectTransform.localPosition = _titleRest;
                _title.rectTransform.localRotation = Quaternion.identity;
                if (_title.TryGetComponent(out MeshRenderer titleRenderer))
                    titleRenderer.enabled = true;
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
        _closed = false;
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

    /// <summary>The cover's front while the booklet shows shut or swings (what is drawn over its data page, one page high), else null (open, or no booklet).</summary>
    public Renderer ShutFace => _shown && _flip.State != CoverState.Open ? front : null;

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
            if (_title != null && front != null)
                Ride(_title.transform, _titleRest, _titleHalf, front.transform, Vector2.down, bow);
        }
        bool shut = _flip.State != CoverState.Open, closed = _flip.State == CoverState.Closed;
        if (shut == _shut && closed == _closed && !force)
            return;
        _shut = shut;
        _closed = closed;
        if (_blockRenderer != null)
            _blockRenderer.enabled = closed;
        _paper.ShowShut(shut, _spine, closed);
    }

    /// <summary>
    /// The front's mask (D63): white, its four corners cut round at
    /// cornerRadius (alpha 0: DocumentCel's cut-out) and a darker crease
    /// along the spine (the cover's fold), for a front
    /// <paramref name="width"/> by <paramref name="height"/> metres (its
    /// top edge on the spine).
    /// </summary>
    private Texture2D FrontMask(float width, float height)
    {
        const int Side = 128;
        if (_frontMask == null)
            _frontMask = new Texture2D(Side, Side, TextureFormat.RGBA32, false) { name = "CoverMask", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var pixels = new Color32[Side * Side];
        float r = Mathf.Min(cornerRadius, width / 2f, height / 2f);
        for (int j = 0; j < Side; j++)
            for (int i = 0; i < Side; i++)
            {
                float x = (i + 0.5f) / Side * width, y = (j + 0.5f) / Side * height;
                float cx = Mathf.Clamp(x, r, width - r), cy = Mathf.Clamp(y, r, height - r);
                bool inside = (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
                // the fold: a band 3 to 4.5 mm under the spine, a little darker
                float fromSpine = height - y;
                byte k = fromSpine > 0.003f && fromSpine < 0.0045f ? (byte)190 : (byte)255;
                pixels[j * Side + i] = new Color32(k, k, k, inside ? (byte)255 : (byte)0);
            }
        _frontMask.SetPixels32(pixels);
        _frontMask.Apply(false);
        return _frontMask;
    }

    /// <summary>
    /// The shut booklet's sides (D63: "just make 3d objects"): a wall round
    /// the data page's outline (<paramref name="width"/> across,
    /// <paramref name="height"/> from the bottom edge up to the
    /// <paramref name="spine"/>, corners round at cornerRadius), from the
    /// cover (Lift over the sheet) down to the booklet's foot
    /// (<paramref name="thickness"/> under the sheet): on the three open sides
    /// the front board, the page block in light page lines and the back
    /// board; on the spine side the cover's <paramref name="colour"/> all the
    /// way down (the fold). Vertex colours carry it all (alpha 0: it never
    /// bends); it shows only while the cover lies fully shut (Show), when
    /// the open model under it is cut whole (DeskDocument.ShowShut).
    /// </summary>
    private void Block(float width, float height, float spine, float thickness, Color colour)
    {
        if (_blockRenderer == null)
        {
            if (front == null)
                return;
            var go = new GameObject("Block");
            go.transform.SetParent(transform, false);
            go.layer = front.gameObject.layer;
            _blockRenderer = go.AddComponent<MeshRenderer>();
            _blockRenderer.sharedMaterial = front.sharedMaterial;
            _blockRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _blockMesh = new Mesh { name = "BookletBlock", hideFlags = HideFlags.DontSave };
            go.AddComponent<MeshFilter>().sharedMesh = _blockMesh;
            var white = new MaterialPropertyBlock();
            white.SetColor(BaseColorId, Color.white);
            white.SetTexture(BaseMapId, Texture2D.whiteTexture);
            _blockRenderer.SetPropertyBlock(white);
        }
        float top = -Lift, foot = Mathf.Max(thickness, boardThickness * 3f);
        var board = new Color(colour.r, colour.g, colour.b, 0f);
        var fold = new Color(colour.r * 0.8f, colour.g * 0.8f, colour.b * 0.8f, 0f);
        Color page = new Color(0.98f, 0.95f, 0.86f, 0f), line = new Color(0.8f, 0.76f, 0.68f, 0f);
        // the bands down an open side: the front board, the page lines, the back board
        var bands = new List<(float z0, float z1, Color c)> { (top, top + boardThickness, board) };
        float pagesFrom = top + boardThickness, pagesTo = foot - boardThickness;
        const int PageBands = 10;
        for (int k = 0; k < PageBands; k++)
            bands.Add((Mathf.Lerp(pagesFrom, pagesTo, (float)k / PageBands), Mathf.Lerp(pagesFrom, pagesTo, (float)(k + 1) / PageBands), k % 2 == 0 ? page : line));
        bands.Add((pagesTo, foot, board));
        var spineBands = new List<(float z0, float z1, Color c)> { (top, foot, fold) };
        float radius = Mathf.Min(cornerRadius, width / 2f, height / 2f);
        List<Vector2> outline = RoundRect(-width / 2f, spine - height, width / 2f, spine, radius);
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var colours = new List<Color>();
        var triangles = new List<int>();
        for (int i = 0; i < outline.Count; i++)
        {
            Vector2 a = outline[i], b = outline[(i + 1) % outline.Count];
            if ((b - a).sqrMagnitude < 1e-12f)
                continue;
            Vector2 along = (b - a).normalized;
            var normal = new Vector3(along.y, -along.x, 0f); // the outline runs anticlockwise: its outside is on the right
            bool spineSide = (a.y + b.y) / 2f > spine - radius * 0.5f;
            foreach ((float z0, float z1, Color c) in spineSide ? spineBands : bands)
            {
                int n = vertices.Count;
                vertices.Add(new Vector3(a.x, a.y, z0));
                vertices.Add(new Vector3(b.x, b.y, z0));
                vertices.Add(new Vector3(b.x, b.y, z1));
                vertices.Add(new Vector3(a.x, a.y, z1));
                for (int v = 0; v < 4; v++)
                {
                    normals.Add(normal);
                    colours.Add(c);
                }
                triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
            }
        }
        _blockMesh.Clear();
        _blockMesh.SetVertices(vertices);
        _blockMesh.SetNormals(normals);
        _blockMesh.SetColors(colours);
        _blockMesh.SetUVs(0, vertices.ConvertAll(_ => new Vector2(0.5f, 0.5f)));
        _blockMesh.SetTriangles(triangles, 0);
        _blockMesh.RecalculateBounds();
    }

    /// <summary>A rectangle's outline from (<paramref name="x0"/>, <paramref name="y0"/>) to (<paramref name="x1"/>, <paramref name="y1"/>), anticlockwise, its corners round at <paramref name="radius"/> (four steps each).</summary>
    private static List<Vector2> RoundRect(float x0, float y0, float x1, float y1, float radius)
    {
        const int Steps = 4;
        var points = new List<Vector2>();
        var centres = new[] { new Vector2(x1 - radius, y0 + radius), new Vector2(x1 - radius, y1 - radius), new Vector2(x0 + radius, y1 - radius), new Vector2(x0 + radius, y0 + radius) };
        for (int c = 0; c < 4; c++)
            for (int step = 0; step <= Steps; step++)
            {
                float angle = (c - 1 + (float)step / Steps) * Mathf.PI / 2f;
                points.Add(centres[c] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        return points;
    }

    private void OnDestroy()
    {
        if (_blockMesh != null)
            Destroy(_blockMesh);
        if (_frontMask != null)
            Destroy(_frontMask);
    }
}
