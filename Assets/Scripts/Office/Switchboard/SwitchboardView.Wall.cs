using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The switchboard on the booth's right wall, seen from the desk (Saleh,
/// 2026-10-28: "Switchboard isnt visible instead it looks weird from the
/// desk"; the director's night review: "The switchboard is still not on the
/// right wall in the office view", a flat dark slab there). Under the painted
/// desk the view lays the board's own plate on the wall where concept B
/// paints the board (SwitchboardWall.Painted, in the painted desk's frame),
/// lit as the booth is (NOPE/Desk Painted, the booth wall's material, the
/// bake's grade), with what lives on it: the call lamps, the modules' lamps,
/// MASTER's lamp, the plugs and the cords, as the board view draws them. So
/// the clerk sees the board in the room, a call's lamp blinking on it, and
/// clicks it (or presses D) to turn to it: the board view grows out of where
/// the board stands on the screen. It is a world object: the office camera
/// draws it in every view that looks that way. The board on the wall takes
/// a click only in the plain office view (ControlRules.BoardOnTheWall), with
/// the office's hand cursor and hover outline (a Clickable on the
/// Interactable layer). Without the painted desk (the art desk) the overlay's
/// click area on the painting's board stands in (SwitchboardConfigSO.wallRect).
/// </summary>
public sealed partial class SwitchboardView
{
    /// <summary>The painted desk's shader (the board on the wall takes the booth's light).</summary>
    private const string PaintedShader = "NOPE/Desk Painted";

    /// <summary>How far each live part stands proud of the board's face (metres): a closed module's veil, the cords, the plugs, a lamp's glow, its lit dome.</summary>
    private const float VeilLift = 0.002f, CordLift = 0.003f, PlugLift = 0.005f, GlowLift = 0.006f, LitLift = 0.007f;

    /// <summary>Seconds between two looks for the painted desk while the board is not on the wall yet (the office binder lays the desk after the switchboard installs).</summary>
    private const float WallRetrySeconds = 1f;

    /// <summary>One lamp on the wall: its glow and lit dome.</summary>
    private sealed class WallLamp
    {
        public SpriteRenderer Glow;
        public SpriteRenderer Lit;
        public float Shown;
    }

    /// <summary>One cord on the wall: its plug, its root plug at home and its rubber.</summary>
    private sealed class WallCord
    {
        public SpriteRenderer Plug;
        public SpriteRenderer Root;
        public LineRenderer Line;
    }

    private bool _wallGaveUp;
    private float _wallNextTry;
    private Transform _wallRoot;
    private MeshRenderer _wallBoard;
    private BoxCollider _wallBox;
    private Material _wallMaterial;
    private Material _wallSprites;
    private RenderTexture _wallPaint;
    private Mesh _wallMesh;
    private WallLamp[] _wallCalls;
    private WallLamp[] _wallModules;
    private WallLamp _wallMaster;
    private SpriteRenderer[] _wallVeils;
    private Sprite _wallWhite;
    private readonly Dictionary<Cord, WallCord> _wallCords = new Dictionary<Cord, WallCord>();
    private readonly List<BoardPoint> _wallCurve = new List<BoardPoint>();
    private Vector3[] _wallLine = new Vector3[0];
    private readonly Vector3[] _wallCorners = new Vector3[4];

    /// <summary>True when the board stands on the wall in the world (the painted desk).</summary>
    private bool OnTheWall => _wallRoot != null;

    // ------------------------------------------------------------------ building

    /// <summary>Lays the board on the painted booth's right wall once the painted desk is bound and showing (looked for once a second until then; never under the art desk, whose painting draws its own board).</summary>
    private void TryBuildWall()
    {
        if (OnTheWall || _wallGaveUp || !DeskLayout.Bound || Time.unscaledTime < _wallNextTry)
            return;
        _wallNextTry = Time.unscaledTime + WallRetrySeconds;
        DeskPainted desk = FindFirstObjectByType<DeskPainted>();
        if (desk == null || _layout == null || Dialer && _dials == null)
            return; // the dialer's parts come with the day (BuildGate)
        Shader shader = Shader.Find(PaintedShader);
        Sprite plate = SlotArt.Sprite(ArtSlots.SwitchboardPlate);
        if (shader == null || plate == null)
        {
            _wallGaveUp = true;
            Debug.LogWarning("[Switchboard] The painted desk shows, but the board's plate or the painted shader is missing: the board stays off the wall.");
            return;
        }

        var root = new GameObject("Switchboard on the wall");
        root.layer = 0;
        root.transform.SetParent(desk.transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        _wallRoot = root.transform;

        BuildWallBoard(desk, shader, plate);
        Shader sprites = Shader.Find("Sprites/Default");
        if (sprites != null)
            _wallSprites = new Material(sprites) { name = "Switchboard on the wall (sprites)" };
        if (Dialer)
            BuildWallGate();
        _wallCalls = new WallLamp[_callLamps.Length];
        for (int d = 0; d < _wallCalls.Length; d++)
            _wallCalls[d] = MakeWallLamp("Call lamp " + d, CallLampOf(d));
        _wallModules = new WallLamp[_layout.Modules.Count];
        for (int m = 0; m < _wallModules.Length; m++)
            _wallModules[m] = MakeWallLamp("Module lamp " + _layout.Modules[m].Id, ModuleLampOf(m));
        _wallMaster = MakeWallLamp("MASTER lamp", _layout.Master.Lamp);
        _wallWhite = UnityEngine.Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        _wallVeils = new SpriteRenderer[_layout.Modules.Count];
        for (int m = 0; m < _wallVeils.Length; m++)
        {
            LayoutModule module = _layout.Modules[m];
            if (Dialer)
                continue; // the dialer's state on the wall is its hub lamps
            _wallVeils[m] = WallSprite("Closed " + module.Id, _wallWhite, module.Rect.Centre, 0f, VeilLift, 0);
            float mpp = SwitchboardWall.Painted.MetresPerPixel(_layout.Board);
            _wallVeils[m].transform.localScale = new Vector3((module.Rect.W - VeilInset) * mpp, (module.Rect.H - VeilInset) * mpp, 1f);
            _wallVeils[m].color = VeilColor;
        }
        foreach (Cord c in _cords)
            _wallCords[c] = MakeWallCord(c);
        Debug.Log("[Switchboard] The board stands on the booth's right wall (the painted desk).");
    }

    /// <summary>The board's plate on the wall: a quad at SwitchboardWall.Painted carrying the plate's board (cut out, mipmapped for the steep view), in the booth wall's material at the bake's grade, with the click box.</summary>
    private void BuildWallBoard(DeskPainted desk, Shader shader, Sprite plate)
    {
        BoardRect board = _layout.Board;
        _wallPaint = new RenderTexture(Mathf.Max(16, Mathf.RoundToInt(board.W)), Mathf.Max(16, Mathf.RoundToInt(board.H)), 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
        {
            name = "Switchboard on the wall (paint)",
            useMipMap = true,
            autoGenerateMips = true,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 8,
            wrapMode = TextureWrapMode.Clamp,
        };
        PaintWall(plate);

        var go = new GameObject("Board");
        go.layer = OfficeLayers.InteractableLayer >= 0 ? OfficeLayers.InteractableLayer : 0;
        go.transform.SetParent(_wallRoot, false);
        SwitchboardWall wall = SwitchboardWall.Painted;
        _wallMesh = new Mesh { name = "Switchboard on the wall" };
        _wallMesh.vertices = new[]
        {
            WallLocal(new BoardPoint(board.X, board.Y), 0f), WallLocal(new BoardPoint(board.X + board.W, board.Y), 0f),
            WallLocal(new BoardPoint(board.X, board.Y + board.H), 0f), WallLocal(new BoardPoint(board.X + board.W, board.Y + board.H), 0f),
        };
        _wallMesh.uv = new[] { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f) };
        _wallMesh.normals = new[] { Vector3.left, Vector3.left, Vector3.left, Vector3.left };
        _wallMesh.triangles = new[] { 0, 1, 3, 0, 3, 2 };
        _wallMesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = _wallMesh;
        _wallBoard = go.AddComponent<MeshRenderer>();

        Material template = null;
        foreach (Renderer r in desk.GetComponentsInChildren<Renderer>(true))
            if (r.name == DeskPaintedMap.BoothWallRight && r.sharedMaterial != null && r.sharedMaterial.shader == shader)
            {
                template = r.sharedMaterial;
                break;
            }
        _wallMaterial = template != null ? new Material(template) : new Material(shader);
        _wallMaterial.name = "Switchboard on the wall";
        _wallMaterial.SetTexture("_BaseMap", _wallPaint);
        _wallMaterial.SetTextureScale("_BaseMap", Vector2.one);
        _wallMaterial.SetTextureOffset("_BaseMap", Vector2.zero);
        _wallMaterial.SetColor("_BaseColor", _config.wallGrade);
        _wallMaterial.DisableKeyword("_MASKMAP");
        if (_wallMaterial.HasProperty("_UseMask"))
            _wallMaterial.SetFloat("_UseMask", 0f);
        if (_wallMaterial.HasProperty("_Locked"))
            _wallMaterial.SetFloat("_Locked", 0f);
        if (_wallMaterial.HasProperty("_Cull"))
            _wallMaterial.SetFloat("_Cull", (float)CullMode.Off);
        _wallBoard.sharedMaterial = _wallMaterial;
        _wallBoard.sortingOrder = -1; // under every live part on it, whatever the camera's distance to each (sprites write no depth)
        _wallBoard.shadowCastingMode = ShadowCastingMode.Off;
        _wallBoard.receiveShadows = false;
        _wallBoard.lightProbeUsage = LightProbeUsage.Off;
        _wallBoard.reflectionProbeUsage = ReflectionProbeUsage.Off;

        // The click box: the board's face, 2 cm deep, on the Interactable layer (the office camera's PhysicsRaycaster); on only in the plain office view (DrawWall).
        Bounds b = _wallMesh.bounds;
        _wallBox = go.AddComponent<BoxCollider>();
        _wallBox.center = b.center + Vector3.right * 0.01f;
        _wallBox.size = new Vector3(0.02f, b.size.y, b.size.z);
        _wallBox.enabled = false;
        var click = go.AddComponent<Clickable>();
        click.onClick.AddListener(OpenFromWall);
        click.SetOutline(new Renderer[] { _wallBoard });
    }

    /// <summary>Copies the plate's board out of its sprite into the wall's mipmapped paint (again if the paint was lost).</summary>
    private void PaintWall(Sprite plate)
    {
        if (_wallPaint == null || plate == null || plate.texture == null)
            return;
        if (!_wallPaint.IsCreated())
            _wallPaint.Create();
        Texture texture = plate.texture;
        Rect tr = plate.textureRect;
        BoardRect board = _layout.Board;
        float u0 = (tr.x + board.X / _layout.Width * tr.width) / texture.width;
        float uw = board.W / _layout.Width * tr.width / texture.width;
        float v0 = (tr.y + (1f - (board.Y + board.H) / _layout.Height) * tr.height) / texture.height;
        float vh = board.H / _layout.Height * tr.height / texture.height;
        Graphics.Blit(texture, _wallPaint, new Vector2(uw, vh), new Vector2(u0, v0));
    }

    /// <summary>The dialer on the wall: one picture over the plate's jacks, its slate face with each big dial's ring stamped on it at rest (too far from the desk to read a turn), in the booth's grade (an unlit sprite, so tinted by wallGrade like the board's paint), drawn over the board by its order.</summary>
    private void BuildWallGate()
    {
        GateCanvas paint = GatePaint.Face(_gate, _layout.Modules.Count, GateEras);
        GateCanvas ring = GatePaint.Ring(_ring.Length);
        for (int m = 0; m < _dials.Length; m++)
            if (_dials[m].Ring != null)
            {
                BoardPoint c = _gate.DialCentre(m);
                GatePaint.Stamp(paint, ring, Mathf.RoundToInt(c.X - _gate.Cover.X - ring.Width / 2f), Mathf.RoundToInt(c.Y - _gate.Cover.Y - ring.Height / 2f));
            }
        SpriteRenderer face = WallSprite("Dialer face", SpriteOf(Art(paint, "Switchboard dialer on the wall")), _gate.Cover.Centre, _gate.Cover.W, VeilLift * 0.5f, 1);
        face.color = _config.wallGrade;
    }

    /// <summary>A lamp on the wall at <paramref name="at"/>: its glow and its lit dome, the board view's sizes (by the glass's radius).</summary>
    private WallLamp MakeWallLamp(string name, BoardLamp at)
    {
        float scale = at.GlassRadius / LampGlassPixels;
        return new WallLamp
        {
            Glow = WallSprite(name + " glow", Sprite("lamp_glow"), at.Centre, GlowPixels * scale, GlowLift, 3),
            Lit = WallSprite(name, Sprite("lamp_lit_amber"), at.Centre, LitPixels * scale, LitLift, 4),
        };
    }

    /// <summary>A cord on the wall: its plug, its root plug and its rubber in its module's colour (the board view's).</summary>
    private WallCord MakeWallCord(Cord c)
    {
        Sprite sprite = c.Plug != null ? c.Plug.sprite : null;
        var cord = new WallCord
        {
            Plug = WallSprite(c.Kind + " plug " + c.Index, sprite, Home(c), 0f, PlugLift, 2),
            Root = WallSprite(c.Kind + " root " + c.Index, sprite, Home(c), 0f, PlugLift, 2),
        };
        var go = new GameObject(c.Kind + " cord " + c.Index);
        go.layer = 0;
        go.transform.SetParent(_wallRoot, false);
        go.transform.localRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
        cord.Line = go.AddComponent<LineRenderer>();
        cord.Line.useWorldSpace = true;
        cord.Line.alignment = LineAlignment.TransformZ;
        cord.Line.textureMode = LineTextureMode.Stretch;
        cord.Line.numCapVertices = 2;
        cord.Line.numCornerVertices = 2;
        cord.Line.widthMultiplier = _config.cordWidth * SwitchboardWall.Painted.MetresPerPixel(_layout.Board);
        Color rubber = c.Line != null ? c.Line.color : Color.black;
        cord.Line.startColor = cord.Line.endColor = rubber;
        cord.Line.shadowCastingMode = ShadowCastingMode.Off;
        cord.Line.receiveShadows = false;
        cord.Line.sortingOrder = 1;
        if (_wallSprites != null)
            cord.Line.sharedMaterial = _wallSprites;
        cord.Line.positionCount = 0;
        return cord;
    }

    /// <summary>A sprite lying on the wall at the plate point <paramref name="at"/>, <paramref name="lift"/> proud of the board, <paramref name="pixels"/> plate pixels wide (0: its own size), drawn at <paramref name="order"/>.</summary>
    private SpriteRenderer WallSprite(string name, Sprite sprite, BoardPoint at, float pixels, float lift, int order)
    {
        var go = new GameObject(name);
        go.layer = 0;
        go.transform.SetParent(_wallRoot, false);
        go.transform.localPosition = WallLocal(at, lift);
        go.transform.localRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        if (_wallSprites != null)
            renderer.sharedMaterial = _wallSprites;
        renderer.sortingOrder = order;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.color = pixels > 0f ? Color.clear : Color.white;
        SizeWallSprite(renderer, pixels > 0f ? pixels : sprite != null ? sprite.rect.width : 0f);
        return renderer;
    }

    /// <summary>Scales a wall sprite so it is <paramref name="pixels"/> plate pixels wide on the wall.</summary>
    private void SizeWallSprite(SpriteRenderer renderer, float pixels)
    {
        Sprite sprite = renderer.sprite;
        float native = sprite != null && sprite.pixelsPerUnit > 0f ? sprite.rect.width / sprite.pixelsPerUnit : 0f;
        float metres = pixels * SwitchboardWall.Painted.MetresPerPixel(_layout.Board);
        renderer.transform.localScale = Vector3.one * (native > 0f ? metres / native : 0f);
    }

    /// <summary>The plate point <paramref name="p"/> on the wall, <paramref name="lift"/> metres toward the room, in the painted desk's local space ((x, z, y) of its frame).</summary>
    private Vector3 WallLocal(BoardPoint p, float lift)
    {
        (float x, float y, float z) at = SwitchboardWall.Painted.At(_layout.Board, p);
        return new Vector3(at.x - lift, at.z, at.y);
    }

    // ------------------------------------------------------------------ drawing

    /// <summary>The board on the wall this frame: its click box on only in the plain office view, its lamps as the board view lights them, the closed modules' veils, its plugs and cords where they rest.</summary>
    private void DrawOnTheWall(bool live, float dt)
    {
        if (_wallBox != null && _wallBox.enabled != live)
            _wallBox.enabled = live;
        if (_wallPaint != null && !_wallPaint.IsCreated())
            PaintWall(SlotArt.Sprite(ArtSlots.SwitchboardPlate));
        for (int d = 0; d < _wallCalls.Length; d++)
            SetWallLamp(_wallCalls[d], CallLook(d), dt);
        bool running = false;
        for (int m = 0; m < _wallModules.Length; m++)
            SetWallLamp(_wallModules[m], ModuleLook(m, ref running), dt);
        SetWallLamp(_wallMaster, MasterLook(running), dt);
        for (int m = 0; m < _wallVeils.Length; m++)
        {
            bool veiled = (_board == null || _board.Modules[m].State == SwitchModuleState.Closed) && !_layout.Modules[m].Placarded;
            if (_wallVeils[m] != null && _wallVeils[m].enabled != veiled)
                _wallVeils[m].enabled = veiled;
        }
        foreach (KeyValuePair<Cord, WallCord> pair in _wallCords)
            DrawWallCord(pair.Key, pair.Value);
    }

    /// <summary>A lamp on the wall lit as the board view lights its own, easing the same way.</summary>
    private void SetWallLamp(WallLamp lamp, LampLook look, float dt)
    {
        lamp.Shown = Mathf.MoveTowards(lamp.Shown, Mathf.Clamp01(look.Level), dt * 14f);
        Sprite lit = _lit[(int)look.Colour];
        if (lit != null && lamp.Lit.sprite != lit)
            lamp.Lit.sprite = lit;
        Color glow = LampTint(look.Colour);
        lamp.Lit.color = new Color(1f, 1f, 1f, lamp.Shown);
        lamp.Glow.color = new Color(glow.r, glow.g, glow.b, lamp.Shown * 0.75f);
    }

    /// <summary>A cord on the wall where it rests (the board view's own drawing shows it in the hand): a plug at home alone, or the root plug at home, the plug in its jack and the rubber hanging between them.</summary>
    private void DrawWallCord(Cord c, WallCord wall)
    {
        bool shown = c.Shown && _board != null;
        bool loose = shown && c.Seat >= 0;
        if (wall.Plug.enabled != shown)
            wall.Plug.enabled = shown;
        if (wall.Root.enabled != loose)
            wall.Root.enabled = loose;
        if (!shown)
        {
            wall.Line.positionCount = 0;
            return;
        }
        BoardPoint home = Home(c);
        float homeScale = HomeScale(c);
        BoardPoint at = loose ? SeatPoint(c) : home;
        float scale = loose ? SeatScale(c) : homeScale;
        PlaceWallSprite(wall.Plug, at, scale);
        if (!loose)
        {
            wall.Line.positionCount = 0;
            return;
        }
        PlaceWallSprite(wall.Root, home, homeScale);
        BoardPoint a = home.Plus(_layout.PlugTail.X * homeScale, _layout.PlugTail.Y * homeScale);
        BoardPoint b = at.Plus(_layout.PlugTail.X * scale, _layout.PlugTail.Y * scale);
        float chord = a.DistanceTo(b);
        SwitchCords.Sample(a, b, _cordAxis, SwitchCords.Reach(chord, _config.cordReach, _config.cordReachMax), SwitchCords.Sag(chord, _config.cordSlack),
                           _config.cordSegments, _wallCurve);
        if (_wallLine.Length != _wallCurve.Count)
            _wallLine = new Vector3[_wallCurve.Count];
        for (int i = 0; i < _wallCurve.Count; i++)
            _wallLine[i] = _wallRoot.TransformPoint(WallLocal(_wallCurve[i], CordLift));
        wall.Line.positionCount = _wallLine.Length;
        wall.Line.SetPositions(_wallLine);
    }

    /// <summary>A plug on the wall at the plate point <paramref name="at"/>, at <paramref name="scale"/> of its sprite.</summary>
    private void PlaceWallSprite(SpriteRenderer renderer, BoardPoint at, float scale)
    {
        renderer.transform.localPosition = WallLocal(at, PlugLift);
        SizeWallSprite(renderer, renderer.sprite != null ? renderer.sprite.rect.width * scale : 0f);
    }

    /// <summary>
    /// Where the board stands on the screen (the canvas's units, its centre at
    /// the origin): the board on the wall's corners through the office camera,
    /// not cut at the screen's edges, so the turn grows the plate out of the
    /// board where it stands; false when there is no board on the wall or it is
    /// behind the camera.
    /// </summary>
    private bool WallOnScreen(Vector2 canvas, out Rect rect)
    {
        rect = default;
        Camera camera = Camera.main;
        if (!OnTheWall || camera == null || Screen.width <= 0 || Screen.height <= 0)
            return false;
        BoardRect board = _layout.Board;
        _wallCorners[0] = _wallRoot.TransformPoint(WallLocal(new BoardPoint(board.X, board.Y), 0f));
        _wallCorners[1] = _wallRoot.TransformPoint(WallLocal(new BoardPoint(board.X + board.W, board.Y), 0f));
        _wallCorners[2] = _wallRoot.TransformPoint(WallLocal(new BoardPoint(board.X, board.Y + board.H), 0f));
        _wallCorners[3] = _wallRoot.TransformPoint(WallLocal(new BoardPoint(board.X + board.W, board.Y + board.H), 0f));
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        foreach (Vector3 corner in _wallCorners)
        {
            Vector3 s = camera.WorldToScreenPoint(corner);
            if (s.z <= 0f)
                return false;
            float x = (s.x / Screen.width - 0.5f) * canvas.x, y = (s.y / Screen.height - 0.5f) * canvas.y;
            x0 = Mathf.Min(x0, x);
            x1 = Mathf.Max(x1, x);
            y0 = Mathf.Min(y0, y);
            y1 = Mathf.Max(y1, y);
        }
        rect = Rect.MinMaxRect(x0, y0, x1, y1);
        return rect.width > 1f && rect.height > 1f;
    }

    /// <summary>Takes the board off the wall (the view is going).</summary>
    private void DestroyWall()
    {
        if (_wallRoot != null)
            Destroy(_wallRoot.gameObject);
        if (_wallMaterial != null)
            Destroy(_wallMaterial);
        if (_wallSprites != null)
            Destroy(_wallSprites);
        if (_wallMesh != null)
            Destroy(_wallMesh);
        if (_wallWhite != null)
            Destroy(_wallWhite);
        if (_wallPaint != null)
        {
            _wallPaint.Release();
            Destroy(_wallPaint);
        }
        _wallRoot = null;
    }
}
