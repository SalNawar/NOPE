using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The paper's look (DeskDocument's render half): the boxes' tints, the photo,
/// the stamps' prints, the marks and watermark, the paper's shape and depth,
/// its bend, the printed text, art, patches, seals and photo frame. The
/// paper's zone, scale, dock and drag stay in DeskDocument.cs.
/// Carved out of DeskDocument.cs by P0-A (pure move). Owner: DOC-LOOK.
/// DOC-LOOK (1019a): print and face are one surface (BUG-12a: the fills'
/// and lines' meshes bend vertex by vertex with the face, each other part
/// rides the bent face lifted by how far the curl rises past its plane,
/// PaperBend.Bulge, and every part clears the sheet model's lifted corner,
/// PaperDepth.ReliefOver); a stamp renews the parts' rest poses (BUG-12b); no
/// word breaks across lines and an Arabic text is laid line by line
/// (TmpFormText.Unbreak, BUG-9, 37c); a card never bends (D3); a hovered
/// paper lifts instead of taking an outline (D15, IHoverLift); a passport
/// arrives shut and opens, on the press frame of a click on its cover too
/// (D3, PaperCover; GAME_FEEL_BIBLE A1). The fills' and lines' quads are cut
/// into cells no wider than PaperBend.PrintCellShare of the page, so their
/// straight chords never sink under the bent face (BUG-12a).
/// </summary>
public sealed partial class DeskDocument : IHoverLift, IPointerDownHandler
{
    /// <summary>The passport's cover (shown on a booklet only; PaperCover: it arrives shut and opens on the desk).</summary>
    [SerializeField] private PaperCover cover;

    /// <summary>The paper's face art (ShowPaperArt's; the passport's cover shows its visa page from it as it swings).</summary>
    private Texture2D _faceArt;

    /// <summary>The paper's outline in the quad's unit space (BuildDepth's: its contact shadow; a shut booklet's shadow lies under its data page alone).</summary>
    private List<(float x, float y)> _depthOutline = new List<(float x, float y)>();

    /// <summary>The fills' and the lines' meshes as printed, and their vertices at rest (the corner's relief in) and bent now (reused: a bend allocates nothing).</summary>
    private Mesh _fillsMesh, _linesMesh;
    private Vector3[] _fillsRest, _linesRest, _fillsBent, _linesBent;

    /// <inheritdoc />
    public void HoverLift(float share)
    {
        if (depth != null)
            depth.HoverLift(share);
    }

    /// <summary>A left press on the paper: a passport's shut cover swings open on the press frame (GAME_FEEL_BIBLE A1, PaperCover.Press); nothing else here (the pick-up is the drag's, the click to the front the click's).</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && cover != null)
            cover.Press();
    }
    /// <summary>A box's quad colour: the pick's colour, else the hover tint while hovered, else clear.</summary>
    private void ApplySlotTint(int slot)
    {
        if (slot < 0 || slot >= _slots.Count || _slots[slot].Highlight == null)
            return;

        SlotView view = _slots[slot];
        Color colour = view.Picked ? view.PickColour
            : slot == _hoveredSlot && style != null ? style.hoverTint
            : view.MarkColour.a > 0f ? view.MarkColour
            : Inspecting ? inspectTint
            : Color.clear;
        _block ??= new MaterialPropertyBlock();
        view.Highlight.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, colour);
        view.Highlight.SetPropertyBlock(_block);
    }

    /// <summary>Shows the traveller's photo in the frame (tinted into the room's light); a null look hides the frame (a document without a photo).</summary>
    public void ShowPhoto(TravellerLook look, CharacterArt art, Color tint)
    {
        if (photoSlot != null)
            photoSlot.SetActive(look != null && _form != null && HasPhotoItem());
        if (photo != null)
        {
            photo.Show(look, art);
            photo.SetTint(tint);
        }
    }

    /// <summary>The verdict's ink as the papers leave unstamped (redesign phase 27; the cheat menu's decision): <paramref name="impression"/> (a dater's print, DeskStampTray.Impression) at the next place of the form's stamp area.</summary>
    public void ShowVerdict(Texture2D impression) => Stamp(impression);

    /// <summary>
    /// Prints a dater's <paramref name="impression"/> on the paper (the desk
    /// machine spec §1: the dater's print, DaterImpressionArt; the paper owns
    /// it from here and destroys it with itself), at the impression's own
    /// aspect, turned <paramref name="tilt"/> degrees, centred at
    /// <paramref name="formPoint"/> (from the page's top-left, y down, in the
    /// paper's metres: where the dater's die pressed, over the boxes too) and
    /// kept whole on the page (StampSpots.AtPoint) or, without one (the
    /// verdict's ink as the papers leave), at the next place of its largest
    /// stamp area (StampSpots.Next: a passport's visa page, a form's footer
    /// box). Returns the mark's place, from the page's top-left in the
    /// paper's metres (an empty one on a paper that prints no form, or for
    /// no impression).
    /// </summary>
    public FaceRect Stamp(Texture2D impression, Vector2? formPoint = null, float tilt = 0f)
    {
        if (inkMark == null || _form == null || impression == null)
            return new FaceRect(0f, 0f, 0f, 0f);
        // BUG-12b: every part back at its rest before the mark moves, and the rest poses taken again on the next bend, so a
        // stamp made while the paper still settles from its drop stays where the die pressed it (the first mark's old
        // rest pose, the page's centre, used to win). ShowBend flattens the model in its shader as well, so a physics bend
        // that comes back exactly flat on the next frame finds face and print already flat together.
        ShowBend(PaperBend.Flat);
        RestorePrints();
        _impressions.Add(impression);
        float aspect = impression.height > 0 ? (float)impression.width / impression.height : 1f;
        FaceRect place = formPoint.HasValue ? StampSpots.AtPoint(_form, formPoint.Value.x / _scale, formPoint.Value.y / _scale, aspect) : StampSpots.Next(_form, _stamps, aspect);
        _stamps++;

        Renderer mark = _stamps == 1 ? inkMark : Instantiate(inkMark, inkMark.transform.parent);
        mark.name = $"Stamp_{_stamps}";
        Rect r = Local(place);
        mark.transform.localPosition = new Vector3(r.center.x, r.center.y, -InkLift - Relief(r.center, r.size / 2f));
        mark.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
        mark.transform.localScale = new Vector3(r.width, r.height, 1f);
        _block ??= new MaterialPropertyBlock();
        mark.GetPropertyBlock(_block);
        _block.SetTexture(BaseMapId, impression);
        _block.SetColor(BaseColorId, Color.white);
        mark.SetPropertyBlock(_block);
        mark.gameObject.SetActive(true);
        return new FaceRect(place.XMin * _scale, place.YMin * _scale, place.XMax * _scale, place.YMax * _scale);
    }

    /// <summary>The verdict word a dater prints on this paper's form (FormStyleSO.approvedStamp, deniedStamp), in the reading language (UiText.DocumentWord).</summary>
    public string StampWord(bool approved) => UiText.DocumentWord(style == null ? (approved ? "APPROVED" : "DENIED") : approved ? style.approvedStamp : style.deniedStamp);

    /// <summary>A mark of the form named <paramref name="name"/> (an emblem, a watermark): a clone of the seal's quad over <paramref name="rect"/> showing <paramref name="texture"/> in <paramref name="ink"/>, <paramref name="lift"/> over the sheet; nothing without a texture or a seal quad.</summary>
    private Renderer PlaceMark(string name, FaceRect rect, Texture2D texture, Color ink, float lift)
    {
        if (seal == null || texture == null)
            return null;
        Renderer mark = Instantiate(seal, seal.transform.parent);
        mark.name = name;
        Rect r = Local(rect);
        mark.transform.localPosition = new Vector3(r.center.x, r.center.y, -lift);
        mark.transform.localScale = new Vector3(r.width, r.height, 1f);
        _block ??= new MaterialPropertyBlock();
        mark.GetPropertyBlock(_block);
        _block.SetTexture(BaseMapId, texture);
        _block.SetColor(BaseColorId, ink);
        mark.SetPropertyBlock(_block);
        mark.gameObject.SetActive(true);
        return mark;
    }

    /// <summary>A watermark (the travel documents spec, TD1) drawn just after the fills' layer (its own copy of the seal's material, one render queue later), so the boxes' fills never hide it; the lines and texts print over it.</summary>
    private void OverTheFills(Renderer mark)
    {
        if (mark == null || fills == null || !fills.TryGetComponent(out Renderer fillRenderer) || fillRenderer.sharedMaterial == null)
            return;
        _ownMarkMaterial ??= new Material(mark.sharedMaterial) { name = "Paper_Watermark", renderQueue = fillRenderer.sharedMaterial.renderQueue + 1 };
        mark.sharedMaterial = _ownMarkMaterial;
    }

    /// <summary>
    /// The paper's outline (the travel documents spec, TD1): a card's corners
    /// rounded by <paramref name="corner"/> (metres) in a mesh of its own (a fan
    /// round the centre, PaperSilhouette), every other paper the quad's own
    /// unit square. The mesh is in the quad's unit space (the quad is scaled
    /// to the paper's size), so its corners are round once scaled.
    /// </summary>
    /// <returns>The outline in the quad's unit space (the edge follows it).</returns>
    private List<(float x, float y)> ShapePaper(float corner)
    {
        List<(float x, float y)> square = PaperEdge.Rectangle(1f, 1f);
        if (paperQuad == null || !paperQuad.TryGetComponent(out MeshFilter filter))
            return square;
        if (_quadMesh == null)
            _quadMesh = filter.sharedMesh;
        if (corner <= 0f || Size.x <= 0f || Size.y <= 0f)
        {
            filter.sharedMesh = _quadMesh;
            return square;
        }
        List<(float x, float y)> outline = PaperSilhouette.Outline(Size.x, Size.y, corner);
        var vertices = new List<Vector3> { Vector3.zero };
        var uvs = new List<Vector2> { new Vector2(0.5f, 0.5f) };
        foreach ((float x, float y) in outline)
        {
            vertices.Add(new Vector3(x / Size.x, y / Size.y, 0f));
            uvs.Add(new Vector2(x / Size.x + 0.5f, y / Size.y + 0.5f));
        }
        // The fan faces the way the quad's own triangles do (its winding and its normal), so the camera sees its front.
        Vector3[] quad = _quadMesh.vertices;
        int[] tris = _quadMesh.triangles;
        bool counterClockwise = tris.Length < 3 || Vector3.Cross(quad[tris[1]] - quad[tris[0]], quad[tris[2]] - quad[tris[0]]).z > 0f;
        Vector3 normal = _quadMesh.normals.Length > 0 ? _quadMesh.normals[0] : Vector3.back;
        var triangles = new List<int>();
        for (int i = 1; i <= outline.Count; i++)
            triangles.AddRange(counterClockwise ? new[] { 0, i, i % outline.Count + 1 } : new[] { 0, i % outline.Count + 1, i });
        if (_shapedMesh == null)
            _shapedMesh = new Mesh { name = "PaperShape" };
        _shapedMesh.Clear();
        _shapedMesh.SetVertices(vertices);
        _shapedMesh.SetUVs(0, uvs);
        _shapedMesh.SetNormals(vertices.ConvertAll(_ => normal));
        _shapedMesh.SetTriangles(triangles, 0);
        _shapedMesh.RecalculateBounds();
        filter.sharedMesh = _shapedMesh;
        return outline.ConvertAll(p => (p.x / Size.x, p.y / Size.y));
    }

    /// <summary>
    /// The paper as a modelled object (DeskPaperDepth; Saleh 2026-10-17: "use
    /// canva and blender to make actual 3d documents"): it wears its
    /// <paramref name="kind"/>'s model (a sheet with a gentle curl and a lifted
    /// corner, an open passport booklet, a thick card), its face art mapped
    /// where the flat quad showed it, and its contact shadow lies under its
    /// <paramref name="outline"/> (the quad's unit space).
    /// </summary>
    private void BuildDepth(List<(float x, float y)> outline, PaperKind kind)
    {
        _kind = kind;
        _depthOutline = outline ?? new List<(float x, float y)>();
        RestFlatPrints(kind);
        if (depth != null && _config != null)
        {
            depth.Wear(kind);
            depth.Build(_depthOutline, Thickness, Size, _config);
        }
        ShowCover(kind);
    }

    /// <summary>The bend the physics gives the paper now: its bow along its travel (the trailing end lagging, MotionKnobs.paperBowLag), its corners' twist and its ripple, by its kind's flex (none on a card: PaperBend.FlexOf, D3).</summary>
    private PaperBend BendNow(MotionKnobs knobs)
    {
        float flex = PaperBend.FlexOf(_kind, knobs.sheetFlex, knobs.bookletFlex);
        if (flex <= 0f || (_physics.Bow == 0f && _physics.Twist == 0f && _physics.Flutter == 0f))
            return PaperBend.Flat;
        // the physics' frame is the paper's on the desk (x right, z away); the face's y runs up the page, away from the chair
        return new PaperBend(_physics.BowDirX, _physics.BowDirZ, _physics.Bow * flex, knobs.paperBowLag, _physics.Twist * flex, _physics.Flutter * flex, _physics.FlutterPhase, knobs.paperFlutterWaves);
    }

    /// <summary>
    /// Bends the paper (Saleh's 1018a playtest: "all documents are stiff"):
    /// its model takes the bend in its shader (DeskPaperDepth.SetBend); the
    /// fills' and lines' meshes take it vertex by vertex (BendPrintMeshes:
    /// print and face are one surface, BUG-12a); each other printed part (a
    /// text, the photo, the seal, a stamp, a highlight) moves to the bent face
    /// under its centre, turned to its slope, a hair above it and lifted by
    /// as much as the curl rises past its flat piece (PaperBend.Bulge: no
    /// line ever vanishes under the landing curl); flat again, every part goes
    /// back exactly where it was printed. True when the bend changed.
    /// </summary>
    private bool ShowBend(PaperBend bend)
    {
        if (bend.Equals(_bend))
            return false;
        _bend = bend;
        if (depth != null)
            depth.SetBend(bend, Size);
        if (sheet == null)
            return true;
        if (bend.IsFlat)
        {
            RestorePrints();
            return true;
        }
        if (_printRest.Count == 0 || _printRest.Count != PrintPartCount())
        {
            RestorePrints();
            foreach (Transform part in sheet)
                if (IsPrintPart(part))
                    _printRest.Add((part, part.localPosition, part.localRotation, new Vector2(part.localPosition.x, part.localPosition.y)));
        }
        float w = Mathf.Max(Size.x, 1e-4f), h = Mathf.Max(Size.y, 1e-4f);
        foreach ((Transform part, Vector3 position, Quaternion rotation, Vector2 centre) in _printRest)
        {
            if (part == null)
                continue;
            float x = centre.x * 2f / w, y = centre.y * 2f / h;
            float drop = bend.Depth(x, y, w, h);
            (float dx, float dy) = bend.Slope(x, y, w, h);
            Vector2 half = HalfSize(part);
            float bulge = bend.Bulge(x, y, half.x, half.y, w, h);
            // the sheet's +z runs away from the viewer: a face sloping deeper to its right turns about its up
            part.localPosition = position + new Vector3(0f, 0f, drop - BentPrintLift - bulge);
            part.localRotation = Quaternion.Euler(Mathf.Atan(dy) * Mathf.Rad2Deg, -Mathf.Atan(dx) * Mathf.Rad2Deg, 0f) * rotation;
        }
        BendPrintMeshes(bend);
        return true;
    }

    /// <summary>Every printed part back where it was printed (the paper lies flat), the fills and lines flat again.</summary>
    private void RestorePrints()
    {
        foreach ((Transform part, Vector3 position, Quaternion rotation, Vector2 _) in _printRest)
            if (part != null)
            {
                part.localPosition = position;
                part.localRotation = rotation;
            }
        _printRest.Clear();
        BendPrintMeshes(PaperBend.Flat);
    }

    /// <summary>True for a part of the sheet that rides the bend as one piece: anything but the face itself, the fills' and the lines' meshes (they bend vertex by vertex) and the passport's cover (it hinges on its own).</summary>
    private bool IsPrintPart(Transform part) =>
        part != null && (paperQuad == null || part != paperQuad.transform) && (fills == null || part != fills.transform)
        && (lines == null || part != lines.transform) && (cover == null || part != cover.transform);

    /// <summary>How many of the sheet's children ride the bend as one piece (IsPrintPart).</summary>
    private int PrintPartCount()
    {
        int count = 0;
        foreach (Transform part in sheet)
            if (IsPrintPart(part))
                count++;
        return count;
    }

    /// <summary>A part's half size on the sheet (metres): a text's box, else its quad's scale.</summary>
    private static Vector2 HalfSize(Transform part)
    {
        Vector3 scale = part.localScale;
        if (part is RectTransform rect)
            return new Vector2(rect.sizeDelta.x * scale.x, rect.sizeDelta.y * scale.y) / 2f;
        return new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)) / 2f;
    }

    /// <summary>How far the sheet model's face stands over its plane under a part centred at <paramref name="centre"/> (sheet metres) of <paramref name="half"/> size: the lifted corner and the edge curl (PaperDepth.ReliefOver); 0 on any other kind.</summary>
    private float Relief(Vector2 centre, Vector2 half)
    {
        if (_kind != PaperKind.Sheet || Size.x <= 0f || Size.y <= 0f)
            return 0f;
        return PaperDepth.ReliefOver(centre.x / Size.x + 0.5f, centre.y / Size.y + 0.5f, half.x / Size.x, half.y / Size.y);
    }

    /// <summary>
    /// The printed parts at rest clear of the sheet model's relief (BUG-12a:
    /// its lifted corner stood over the print in about 13% of the corner):
    /// each part lifts by the most the face stands under it, the fills' and
    /// lines' vertices each by the face under them. Runs once the paper is
    /// printed (its kind known). The fills' and lines' rest vertices are kept
    /// for the bend.
    /// </summary>
    private void RestFlatPrints(PaperKind kind)
    {
        RestorePrints();
        _bend = PaperBend.Flat;
        if (sheet != null && kind == PaperKind.Sheet)
            foreach (Transform part in sheet)
                if (IsPrintPart(part))
                {
                    Vector3 p = part.localPosition;
                    part.localPosition = new Vector3(p.x, p.y, p.z - Relief(new Vector2(p.x, p.y), HalfSize(part)));
                }
        _fillsMesh = RestMesh(fills, ref _fillsRest, ref _fillsBent);
        _linesMesh = RestMesh(lines, ref _linesRest, ref _linesBent);
    }

    /// <summary>A print mesh's rest vertices (its quads cut into cells, Subdivide; each vertex lifted by the relief under it), written back to it; null without one.</summary>
    private Mesh RestMesh(MeshFilter filter, ref Vector3[] rest, ref Vector3[] bent)
    {
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        if (mesh == null)
            return null;
        Subdivide(mesh);
        rest = mesh.vertices;
        for (int i = 0; i < rest.Length; i++)
            rest[i].z -= Relief(new Vector2(rest[i].x, rest[i].y), Vector2.zero);
        bent = new Vector3[rest.Length];
        mesh.vertices = rest;
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// Cuts each of a print mesh's quads (MeshBuilder.Rect's: four corners,
    /// two triangles) into a grid of cells no wider and no taller than
    /// PaperBend.PrintCellShare of the page (BUG-12a: a quad bent at its four
    /// corners only lay on the straight chord between them, up to the bow's
    /// whole depth behind the drooping face, and its rules and boxes vanished
    /// mid-span). A mesh of any other shape is left as it is.
    /// </summary>
    private void Subdivide(Mesh mesh)
    {
        Vector3[] corners = mesh.vertices;
        Color[] colours = mesh.colors;
        int[] triangles = mesh.triangles;
        int quads = corners.Length / 4;
        if (quads == 0 || corners.Length % 4 != 0 || triangles.Length != quads * 6 || colours.Length != corners.Length)
            return;
        var vertices = new List<Vector3>(corners.Length);
        var tints = new List<Color>(corners.Length);
        var tris = new List<int>(triangles.Length);
        for (int q = 0; q < quads; q++)
        {
            // MeshBuilder.Rect's order: (xMin, yMin), (xMin, yMax), (xMax, yMax), (xMax, yMin)
            Vector3 low = corners[q * 4], high = corners[q * 4 + 2];
            int nx = PaperBend.PrintCells(Mathf.Abs(high.x - low.x), Size.x), ny = PaperBend.PrintCells(Mathf.Abs(high.y - low.y), Size.y);
            int first = vertices.Count;
            for (int j = 0; j <= ny; j++)
                for (int i = 0; i <= nx; i++)
                {
                    vertices.Add(new Vector3(Mathf.Lerp(low.x, high.x, (float)i / nx), Mathf.Lerp(low.y, high.y, (float)j / ny), low.z));
                    tints.Add(colours[q * 4]);
                }
            for (int j = 0; j < ny; j++)
                for (int i = 0; i < nx; i++)
                {
                    int a = first + j * (nx + 1) + i, b = a + nx + 1;
                    // the same winding as MeshBuilder.Rect: (xMin, yMin), (xMin, yMax), (xMax, yMax) then (xMin, yMin), (xMax, yMax), (xMax, yMin)
                    tris.AddRange(new[] { a, b, b + 1, a, b + 1, a + 1 });
                }
        }
        if (vertices.Count == corners.Length)
            return;
        mesh.Clear();
        mesh.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetColors(tints);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
    }

    /// <summary>The fills' and lines' meshes on the bent face (flat: their rest), vertex by vertex, a hair above it.</summary>
    private void BendPrintMeshes(PaperBend bend)
    {
        BendMesh(fills, _fillsMesh, _fillsRest, _fillsBent, bend);
        BendMesh(lines, _linesMesh, _linesRest, _linesBent, bend);
    }

    private void BendMesh(MeshFilter filter, Mesh mesh, Vector3[] rest, Vector3[] bent, PaperBend bend)
    {
        if (filter == null || mesh == null || filter.sharedMesh != mesh || rest == null || bent == null || bent.Length != rest.Length)
            return;
        if (bend.IsFlat)
        {
            mesh.vertices = rest;
            return;
        }
        float w = Mathf.Max(Size.x, 1e-4f), h = Mathf.Max(Size.y, 1e-4f);
        for (int i = 0; i < rest.Length; i++)
        {
            Vector3 v = rest[i];
            bent[i] = new Vector3(v.x, v.y, v.z + bend.Depth(Mathf.Clamp(v.x * 2f / w, -1f, 1f), Mathf.Clamp(v.y * 2f / h, -1f, 1f), w, h) - BentPrintLift);
        }
        mesh.vertices = bent;
    }

    /// <summary>The passport's cover on a booklet (shut: it arrives closed, PaperCover opens it on the desk), none on any other paper.</summary>
    private void ShowCover(PaperKind kind)
    {
        if (cover == null)
            return;
        if (kind != PaperKind.Booklet || _form == null)
        {
            cover.Hide();
            return;
        }
        float spine = 0f;
        string colour = null;
        string emblem = null;
        foreach (FormItem item in _form.Items)
        {
            if (item.Kind == FormItemKind.Spine)
                spine = Local(item.Rect).center.y;
            else if (item.Kind == FormItemKind.Cover && !string.IsNullOrEmpty(item.Text))
                colour = item.Text;
            else if (item.Kind == FormItemKind.Watermark && !Seals.TryParse(item.Text, out _))
                emblem = item.Text;
        }
        Color ink = EmblemArt.Ink(colour, new Color(0.14f, 0.19f, 0.37f, 1f));
        cover.Arrive(this, drag, spine, Size, ink, string.IsNullOrEmpty(emblem) ? null : EmblemArt.Texture(emblem), _faceArt);
    }

    /// <summary>
    /// A booklet shut (<paramref name="shut"/>: its cover over the data page,
    /// PaperCover) or open: shut, its visa page above <paramref name="spine"/>
    /// (sheet metres) is cut from its model (DeskPaperDepth.ClipAbove), its
    /// prints there and its fills and lines hidden, its contact shadow lies
    /// under the data page alone, and the visa page takes no pointer: the
    /// sheet's box (the click's, the hover's and the drag's) covers the data
    /// page alone (ShutBox) and every printed part's box over the spine is off
    /// (DOC-LOOK review, S3: the hidden half used to take hovers, clicks,
    /// presses and drags over the bare counter); open, all of it shows and
    /// takes the pointer again. Each print and box is judged where it lies,
    /// so a layer's marks (UvPaperLayer, MicroPaperLayer) on the data page stay.
    /// </summary>
    public void ShowShut(bool shut, float spine)
    {
        float h = Mathf.Max(Size.y, 1e-4f);
        if (depth != null)
        {
            depth.ClipAbove(shut ? spine / h : 10f);
            if (_config != null)
                depth.Build(shut ? PaperEdge.Rectangle(1f, spine / h + 0.5f).ConvertAll(p => (p.x, p.y + (spine / h - 0.5f) / 2f)) : _depthOutline, Thickness, Size, _config);
        }
        if (sheet == null)
            return;
        if (sheet.TryGetComponent(out BoxCollider box))
        {
            (float centre, float tall) = shut ? ShutBox.Keep(0f, Size.y, spine, true) : (0f, Size.y);
            box.center = new Vector3(box.center.x, centre, box.center.z);
            box.size = new Vector3(box.size.x, tall, box.size.z);
        }
        foreach (Transform part in sheet)
        {
            bool printMesh = (fills != null && part == fills.transform) || (lines != null && part == lines.transform);
            if (!printMesh && !IsPrintPart(part))
                continue;
            foreach (Renderer r in part.GetComponentsInChildren<Renderer>(true))
                r.forceRenderingOff = shut && (printMesh || OverSpine(r.transform, spine));
            foreach (Collider c in part.GetComponentsInChildren<Collider>(true))
                ShutOut(c, shut && OverSpine(c.transform, spine));
        }
    }

    /// <summary>The printed parts' boxes a shut booklet turned off (ShowShut: over its spine); only these come back on as it opens.</summary>
    private readonly HashSet<Collider> _shutOut = new HashSet<Collider>();

    /// <summary>True when <paramref name="part"/> lies over the booklet's <paramref name="spine"/> (sheet metres): on the visa page.</summary>
    private bool OverSpine(Transform part, float spine) => sheet.InverseTransformPoint(part.position).y > spine;

    /// <summary>Turns a printed part's box off (<paramref name="off"/>) while its booklet is shut, or back on once it opens when ShowShut turned it off.</summary>
    private void ShutOut(Collider box, bool off)
    {
        if (off && box.enabled)
        {
            box.enabled = false;
            _shutOut.Add(box);
        }
        else if (!off && _shutOut.Remove(box))
            box.enabled = true;
    }

    // ---------------- Printing ----------------

    /// <summary>Prints one text item: a clone of the template in its role's style and ink, over its rectangle, shrunk just enough that its widest word fits its box (TmpFormText.WordFit, measured at the layout's scale), as on the PC; on a document drawn on its <paramref name="art"/>, in the art's fonts and inks, centred down its place and shrunk to fit it (TmpFormText.OnArt); then no word breaks across lines and an Arabic text is laid line by line (TmpFormText.Unbreak, BUG-9, 37c).</summary>
    private void Print(FormItem item, FormArt art)
    {
        TextMeshPro text = Instantiate(textTemplate, textTemplate.transform.parent);
        text.name = item.Role.ToString();
        float fit = art != null ? 1f : TmpFormText.WordFit(textTemplate, item.Text, item.Role, item.Size, item.Rect.Width);
        TmpFormText.Style(text, item.Role, item.Size * _scale * fit);
        text.text = item.Text;
        text.color = style.Ink(item.Role);
        text.alignment = item.Align == FormTextAlign.Right ? TextAlignmentOptions.TopRight
            : item.Align == FormTextAlign.Centre ? TextAlignmentOptions.Top
            : TextAlignmentOptions.TopLeft;
        Rect r = Local(item.Rect);
        if (art != null)
            TmpFormText.OnArt(text, item, style, art, textTemplate);
        text.rectTransform.sizeDelta = new Vector2(r.width, r.height);
        TmpFormText.Unbreak(text, r.width);
        text.rectTransform.localPosition = new Vector3(r.center.x, r.center.y, -TextLift);
        text.GetComponent<MeshRenderer>().enabled = true;
    }

    /// <summary>
    /// The paper's art when delivered (redesign phase 27, ArtSlots): its kind's
    /// face by <paramref name="formNumber"/> (a passport's for its holder's
    /// nation, <paramref name="issuer"/>, first), else the agency's plain face, on
    /// the paper quad (on the counter or the desk: the block outlives the
    /// reading material's swap); without a face, the look's paper <paramref name="tint"/> (the
    /// document design spec, D1) on a plain sheet; the photo frame's art over
    /// the photo; the agency seal's on the seal. A missing file keeps the
    /// placeholder paper, the grey frame and the code-drawn ring.
    /// </summary>
    private void ShowPaperArt(string formNumber, string issuer, Color? tint, bool onArt, bool clean)
    {
        _block ??= new MaterialPropertyBlock();
        Texture2D face = paperQuad != null ? SlotArt.Texture(ArtSlots.PaperFaces(formNumber, issuer, clean)) : null;
        _faceArt = face;
        if (face != null)
        {
            paperQuad.GetPropertyBlock(_block);
            _block.SetTexture(BaseMapId, face);
            _block.SetColor(BaseColorId, Color.white);
            paperQuad.SetPropertyBlock(_block);
        }
        else if (paperQuad != null && tint != null)
        {
            paperQuad.GetPropertyBlock(_block);
            _block.SetTexture(BaseMapId, Texture2D.whiteTexture);
            _block.SetColor(BaseColorId, tint.Value);
            paperQuad.SetPropertyBlock(_block);
        }

        Texture2D frame = photoFrame != null ? SlotArt.Texture(new[] { onArt ? ArtSlots.PhotoHolo : ArtSlots.PhotoFrame }) : null;
        if (frame != null)
        {
            photoFrame.GetPropertyBlock(_block);
            _block.SetTexture(BaseMapId, frame);
            photoFrame.SetPropertyBlock(_block);
            photoFrame.gameObject.SetActive(true);
        }

        Texture2D mark = seal != null ? SlotArt.Texture(new[] { ArtSlots.AgencySeal }) : null;
        if (mark != null)
        {
            seal.GetPropertyBlock(_block);
            _block.SetTexture(BaseMapId, mark);
            seal.SetPropertyBlock(_block);
        }
    }

    /// <summary>
    /// A patch of the blank face (FormItemKind.Patch; ArtSlots.PaperBlank)
    /// over <paramref name="rect"/>: a clone of the seal's quad just over the
    /// paper showing that piece of <paramref name="blank"/> (its texture's
    /// scale and offset), so a field not introduced yet shows no label on its
    /// art. Nothing without the blank face.
    /// </summary>
    private void PlacePatch(FaceRect rect, Texture2D blank)
    {
        if (seal == null || blank == null)
            return;
        Renderer patch = Instantiate(seal, seal.transform.parent);
        patch.name = "Patch";
        Rect r = Local(rect);
        patch.transform.localPosition = new Vector3(r.center.x, r.center.y, -SealLift);
        patch.transform.localScale = new Vector3(r.width, r.height, 1f);
        float w = rect.Width / _form.Width, h = rect.Height / _form.PageHeight;
        _block ??= new MaterialPropertyBlock();
        patch.GetPropertyBlock(_block);
        _block.SetTexture(BaseMapId, blank);
        _block.SetVector(BaseMapStId, new Vector4(w, h, rect.XMin / _form.Width, 1f - rect.YMax / _form.PageHeight));
        _block.SetColor(BaseColorId, Color.white);
        patch.SetPropertyBlock(_block);
        patch.gameObject.SetActive(true);
    }

    /// <summary>
    /// The seal: its quad over the header's seal rectangle, faint (the
    /// agency's), or, for a seal the form names (the issuing office's, the
    /// document design spec D4; its value Seals.Describe's words), its
    /// outline (SealArt) in its ink with its legend printed in the middle.
    /// </summary>
    private void PlaceSeal(FormItem item)
    {
        if (seal == null)
            return;
        Rect r = Local(item.Rect);
        seal.transform.localPosition = new Vector3(r.center.x, r.center.y, -SealLift);
        seal.transform.localScale = new Vector3(r.width, r.height, 1f);
        _block ??= new MaterialPropertyBlock();
        seal.GetPropertyBlock(_block);
        bool office = Seals.TryParse(item.Text, out Seal mark);
        if (office)
        {
            _block.SetTexture(BaseMapId, SealArt.Texture(mark.Shape));
            _block.SetColor(BaseColorId, SealArt.Ink(mark.Ink));
        }
        else
        {
            _block.SetColor(BaseColorId, style.seal);
        }
        seal.SetPropertyBlock(_block);
        seal.gameObject.SetActive(true);
        if (office)
            PrintLegend(r, mark);
    }

    /// <summary>A seal's legend: a clone of the text template in bold capitals and the seal's ink, centred in <paramref name="r"/> (the seal's local rectangle).</summary>
    private void PrintLegend(Rect r, Seal mark)
    {
        TextMeshPro text = Instantiate(textTemplate, textTemplate.transform.parent);
        text.name = "SealLegend";
        TmpFormText.Style(text, FormTextRole.Title, r.height * SealArt.LegendShare);
        text.text = mark.Legend;
        text.color = SealArt.Ink(mark.Ink);
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.sizeDelta = new Vector2(r.width, r.height);
        text.rectTransform.localPosition = new Vector3(r.center.x, r.center.y, -TextLift);
        text.GetComponent<MeshRenderer>().enabled = true;
    }

    /// <summary>The photo frame over the photo's rectangle (its quad is PhotoAspect by 1, so it scales by the height).</summary>
    private void PlacePhoto(FaceRect rect)
    {
        if (photoSlot == null)
            return;
        Rect r = Local(rect);
        photoSlot.transform.localPosition = new Vector3(r.center.x, r.center.y, -PhotoLift);
        photoSlot.transform.localScale = new Vector3(r.height, r.height, 1f);
        _keys.Add(Share(r));
    }

    /// <summary>True when the placed form prints a photo.</summary>
    private bool HasPhotoItem()
    {
        foreach (FormItem item in _form.Items)
            if (item.Kind == FormItemKind.Photo)
                return true;
        return false;
    }

    /// <summary>Collects coloured quads (FormPaint's) in the sheet's plane into one mesh.</summary>
    private sealed class MeshBuilder
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color> _colours = new List<Color>();
        private readonly List<int> _triangles = new List<int>();
        private float _z;

        /// <summary>A filled rectangle.</summary>
        public void Rect(Rect r, Color colour)
        {
            if (r.width <= 0f || r.height <= 0f)
                return;
            int v = _vertices.Count;
            _vertices.Add(new Vector3(r.xMin, r.yMin, 0f));
            _vertices.Add(new Vector3(r.xMin, r.yMax, 0f));
            _vertices.Add(new Vector3(r.xMax, r.yMax, 0f));
            _vertices.Add(new Vector3(r.xMax, r.yMin, 0f));
            for (int i = 0; i < 4; i++)
                _colours.Add(colour);
            _triangles.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
        }

        /// <summary>Puts the quads into <paramref name="filter"/>'s mesh, <paramref name="lift"/> above the sheet.</summary>
        public void Apply(MeshFilter filter, float lift)
        {
            if (filter == null)
                return;
            _z = -lift;
            for (int i = 0; i < _vertices.Count; i++)
                _vertices[i] = new Vector3(_vertices[i].x, _vertices[i].y, _z);
            var mesh = new Mesh { name = filter.name };
            mesh.SetVertices(_vertices);
            mesh.SetColors(_colours);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
        }
    }
}
