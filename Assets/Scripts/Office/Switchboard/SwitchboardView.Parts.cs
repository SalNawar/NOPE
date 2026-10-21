using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The switchboard view's parts (SwitchboardView): its own overlay canvas
/// over the office (the office view's click area and call glow on the
/// painted board; the board's matte and plate with its live parts), built
/// at run time from the art slots (ArtSlots.SwitchboardFolder) and placed
/// by the plate's measured layout; and how each part is drawn every frame:
/// the turn to the board, the lamps, the module displays and veils, MASTER,
/// the cords and plugs, the targets while a plug is held, the flares and
/// the shakes, and the sounds.
/// </summary>
public sealed partial class SwitchboardView
{
    /// <summary>The overlay canvas's order: over the office's overlay (10: the PC frame, the newsletters), under the cheat menu (IMGUI).</summary>
    private const int SortingOrder = 20;

    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    private static readonly Color MatteColor = new Color(0.055f, 0.035f, 0.025f, 1f);
    private static readonly Color NameInk = new Color32(0x28, 0x19, 0x0f, 0xff);
    private static readonly Color TagInk = new Color32(0x3e, 0x1f, 0x0e, 0xff);
    private static readonly Color LampAmber = new Color(1f, 0.68f, 0.24f, 1f);
    private static readonly Color LampGreen = new Color(0.42f, 1f, 0.52f, 1f);
    private static readonly Color LampRed = new Color(1f, 0.32f, 0.26f, 1f);
    private static readonly Color LampWarm = new Color(1f, 0.92f, 0.72f, 1f);
    private static readonly Color DisplayGlass = new Color(0.045f, 0.03f, 0.022f, 0.94f);
    private static readonly Color VeilColor = new Color(0.04f, 0.025f, 0.018f, 0.5f);
    private static readonly Color PaperColor = new Color(0.94f, 0.9f, 0.8f, 1f);
    private static readonly Color TapeColor = new Color(0.86f, 0.82f, 0.64f, 0.85f);
    private static readonly Color PowerRubber = new Color(0.42f, 0.1f, 0.06f, 1f);

    /// <summary>Each module's place plug (decision: fixed colours per module, the fifth tinted) and the rubber of its two cords.</summary>
    private static readonly string[] PlacePlugs = { "plug_place_blue", "plug_place_green", "plug_place_pink", "plug_place_brass", "plug_tint_white" };
    private static readonly Color[] ModuleRubber =
    {
        new Color(0.27f, 0.42f, 0.64f, 1f), new Color(0.25f, 0.5f, 0.23f, 1f), new Color(0.64f, 0.26f, 0.34f, 1f),
        new Color(0.55f, 0.4f, 0.16f, 1f), new Color(0.72f, 0.72f, 0.72f, 1f),
    };

    /// <summary>A lamp's lit colours (the art's lit domes).</summary>
    private enum LampColour
    {
        Amber,
        Green,
        Red
    }

    private enum CordKind
    {
        Place,
        Period,
        Power
    }

    /// <summary>One cord: whose it is, where its plug is (its seat in its targets, or home), and how it moves.</summary>
    private sealed class Cord
    {
        public CordKind Kind;
        public int Index;
        public int Seat = -1;
        public int HeldFrom = -1;
        public bool Shown;
        public bool Held;
        public BoardPoint HeldAt;
        public BoardPoint Offset;
        public int Snap = -1;
        public bool Returning;
        public float ReturnT;
        public BoardPoint ReturnFrom;
        public float ReturnFromScale;
        public bool OnTop;
        public Image Plug;
        public Image Root;
        public CordGraphic Line;
        public Spring Sag = Spring.At(1f);
        public Spring Bounce = Spring.At(0f);
        public BoardPoint Drawn;
        public float DrawnScale = 1f;
    }

    /// <summary>One lamp: its lit dome and its glow.</summary>
    private sealed class Lamp
    {
        public Image Lit;
        public Image Glow;
        public float Phase;
        public float Shown;
    }

    /// <summary>One module's little display: its glass, its words and what they were made from (rebuilt only when that changes).</summary>
    private sealed class Display
    {
        public Image Glass;
        public TextMeshProUGUI Text;
        public int Shows = int.MinValue;
    }

    /// <summary>A flare, a spark or a rising word.</summary>
    private sealed class Fx
    {
        public RectTransform Rect;
        public Graphic Graphic;
        public Color Color;
        public Vector2 Velocity;
        public float Age;
        public float Life;
        public float Delay;
        public float FromScale;
        public float ToScale;
        public bool Falls;
    }

    private RectTransform _canvasRect;
    private RectTransform _wallLayer;
    private RectTransform _hitZone;
    private Image _wallGlow;
    private Image _wallDot;
    private Image _wallHover;
    private float _wallPhase;
    private RectTransform _boardLayer;
    private CanvasGroup _boardGroup;
    private Image _matte;
    private RectTransform _plate;
    private RectTransform _cordLayer;
    private RectTransform _ringLayer;
    private RectTransform _plugLayer;
    private RectTransform _heldLayer;
    private RectTransform _fxLayer;
    private RectTransform _grab;
    private RectTransform _hint;
    private TextMeshProUGUI _hintText;
    private TMP_FontAsset _font;
    private TMP_FontAsset _mono;
    private TextMeshProUGUI[] _names;
    private TextMeshProUGUI[][] _tags;
    private Lamp[] _callLamps;
    private Lamp[] _moduleLamps;
    private Lamp _masterLamp;
    private Display[] _displays;
    private Image[] _veils;
    private RectTransform[] _rods;
    private Vector2 _rodSize;
    private RectTransform _handle;
    private readonly List<Cord> _cords = new List<Cord>();
    private readonly List<BoardSocket> _placeJacks = new List<BoardSocket>();
    private readonly List<BoardSocket> _periodJacks = new List<BoardSocket>();
    private readonly List<BoardSocket> _modulePower = new List<BoardSocket>();
    private readonly List<Image> _rings = new List<Image>();
    private readonly List<Fx> _fx = new List<Fx>();
    private readonly List<BoardPoint> _curve = new List<BoardPoint>();
    private readonly List<Vector2> _curveLocal = new List<Vector2>();
    private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
    private readonly Dictionary<SwitchSound, AudioClip> _clips = new Dictionary<SwitchSound, AudioClip>();
    private Sprite[] _lit;
    private BoardPoint _cordAxis;
    private float[] _lastLeft;
    private AudioSource _audio;
    private float _shake;

    // ------------------------------------------------------------------ building

    /// <summary>Builds the canvas and every part once (the day fills the words later).</summary>
    private void BuildParts()
    {
        gameObject.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 0;
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        gameObject.AddComponent<GraphicRaycaster>();
        _canvasRect = (RectTransform)transform;
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;
        if (Sounds.Bank != null)
            _audio.outputAudioMixerGroup = Sounds.Bank.deskGroup;
        _font = Font("Outfit-Bold SDF");
        _mono = Font("GeistMono-Bold SDF");
        _lit = new[] { Sprite("lamp_lit_amber"), Sprite("lamp_lit_green"), Sprite("lamp_lit_red") };
        float tail = Mathf.Max(1e-3f, Mathf.Sqrt(_layout.PlugTail.X * _layout.PlugTail.X + _layout.PlugTail.Y * _layout.PlugTail.Y));
        _cordAxis = new BoardPoint(_layout.PlugTail.X / tail, _layout.PlugTail.Y / tail);

        // The office view: the painted board's click area, its hover warmth and the call glow.
        _wallLayer = Stretch("Office view", transform);
        _wallHover = Picture("Hover", _wallLayer, Sprite("lamp_glow"), Clear(LampWarm), Vector2.zero, new Vector2(420f, 420f));
        _wallGlow = Picture("Call glow", _wallLayer, Sprite("lamp_glow"), Clear(LampAmber), Vector2.zero, new Vector2(84f, 84f));
        _wallDot = Picture("Call lamp", _wallLayer, Sprite("lamp_lit_amber"), Clear(Color.white), Vector2.zero, new Vector2(16f, 16f));
        _hitZone = Picture("Board on the wall", _wallLayer, null, Color.clear, Vector2.zero, Vector2.one, true).rectTransform;
        _hitZone.gameObject.AddComponent<Clickable>().onClick.AddListener(OpenFromWall);

        // The board: the matte, then the plate and its live parts in drawing order.
        _boardLayer = Stretch("Board", transform);
        _boardGroup = _boardLayer.gameObject.AddComponent<CanvasGroup>();
        _matte = Picture("Matte", _boardLayer, null, MatteColor, Vector2.zero, Vector2.zero, true);
        Fill(_matte.rectTransform);
        _plate = Layer("Plate", _boardLayer);
        Sprite plate = SlotArt.Sprite(ArtSlots.SwitchboardPlate);
        Picture("Plate art", _plate, plate, plate != null ? Color.white : new Color(0.16f, 0.1f, 0.07f, 1f), Vector2.zero, new Vector2(_layout.Width, _layout.Height));
        BuildWords(Layer("Words", _plate));
        BuildModules(Layer("Modules", _plate));
        BuildLamps(Layer("Lamps", _plate));
        BuildHint(_plate);
        BuildLever(Layer("MASTER", _plate));
        _cordLayer = Layer("Cords", _plate);
        _ringLayer = Layer("Targets", _plate);
        _plugLayer = Layer("Plugs", _plate);
        _heldLayer = Layer("In the hand", _plate);
        _fxLayer = Layer("Flares", _plate);
        BuildCords();
        _grab = Picture("Grab", _plate, null, Color.clear, Vector2.zero, new Vector2(60f, 60f), true).rectTransform;
        _grab.gameObject.AddComponent<Clickable>();
        _grab.gameObject.SetActive(false);
        _boardLayer.gameObject.SetActive(false);
        _lastLeft = new float[_layout.Modules.Count];
    }

    /// <summary>The blank name plates' and period tags' live words.</summary>
    private void BuildWords(RectTransform layer)
    {
        int n = _layout.Destinations.Count;
        _names = new TextMeshProUGUI[n];
        _tags = new TextMeshProUGUI[n][];
        for (int d = 0; d < n; d++)
        {
            LayoutDestination dest = _layout.Destinations[d];
            _names[d] = Label("Name " + d, layer, Local(dest.NameText.Centre), Size(dest.NameText), NameInk, 9f, 21f, _font);
            _tags[d] = new TextMeshProUGUI[dest.Periods.Count];
            for (int r = 0; r < dest.Periods.Count; r++)
                _tags[d][r] = Label("Period " + d + "." + r, layer, Local(dest.Periods[r].TagText.Centre), Size(dest.Periods[r].TagText), TagInk, 6f, 14f, _font);
        }
    }

    /// <summary>Each module's veil (not in service today) and display (between its number plate and its sockets).</summary>
    private void BuildModules(RectTransform layer)
    {
        int n = _layout.Modules.Count;
        _veils = new Image[n];
        _displays = new Display[n];
        for (int m = 0; m < n; m++)
        {
            LayoutModule module = _layout.Modules[m];
            _veils[m] = Picture("Closed " + module.Id, layer, null, VeilColor, Local(module.Rect.Centre), new Vector2(module.Rect.W - 10f, module.Rect.H - 10f));
            BoardPoint at = DisplayCentre(module);
            var display = new Display { Glass = Picture("Display " + module.Id, layer, null, DisplayGlass, Local(at), new Vector2(Mathf.Min(104f, module.Rect.W - 70f), 26f)) };
            display.Text = Label("Display text " + module.Id, display.Glass.rectTransform, Vector2.zero, new Vector2(display.Glass.rectTransform.sizeDelta.x - 8f, 22f), LampAmber, 8f, 17f, _mono);
            _displays[m] = display;
        }
    }

    /// <summary>The call lamps (full size), the modules' lamps (0.875) and MASTER's (1.42): the art's sizes, by each glass's radius.</summary>
    private void BuildLamps(RectTransform layer)
    {
        _callLamps = new Lamp[_layout.Destinations.Count];
        for (int d = 0; d < _callLamps.Length; d++)
            _callLamps[d] = MakeLamp("Call lamp " + d, layer, _layout.Destinations[d].Lamp);
        _moduleLamps = new Lamp[_layout.Modules.Count];
        for (int m = 0; m < _moduleLamps.Length; m++)
            _moduleLamps[m] = MakeLamp("Module lamp " + _layout.Modules[m].Id, layer, _layout.Modules[m].Lamp);
        _masterLamp = MakeLamp("MASTER lamp", layer, _layout.Master.Lamp);
    }

    private Lamp MakeLamp(string name, RectTransform layer, BoardLamp at)
    {
        float scale = at.GlassRadius / 12f;
        return new Lamp
        {
            Glow = Picture(name + " glow", layer, Sprite("lamp_glow"), Color.clear, Local(at.Centre), new Vector2(96f, 96f) * scale),
            Lit = Picture(name, layer, Sprite("lamp_lit_amber"), Color.clear, Local(at.Centre), new Vector2(30f, 30f) * scale),
        };
    }

    /// <summary>The day-1 hint card, pinned on the wood left of the board, a little crooked.</summary>
    private void BuildHint(RectTransform plate)
    {
        float x = Mathf.Max(130f, _layout.Board.X - 150f);
        _hint = Layer("Hint card", plate);
        _hint.sizeDelta = new Vector2(200f, 156f);
        _hint.anchoredPosition = Local(new BoardPoint(x, 430f));
        _hint.localRotation = Quaternion.Euler(0f, 0f, 4f);
        Picture("Shadow", _hint, null, new Color(0f, 0f, 0f, 0.35f), new Vector2(6f, -8f), _hint.sizeDelta);
        Picture("Paper", _hint, null, PaperColor, Vector2.zero, _hint.sizeDelta);
        Picture("Tape", _hint, null, TapeColor, new Vector2(0f, _hint.sizeDelta.y / 2f - 4f), new Vector2(74f, 20f)).rectTransform.localRotation = Quaternion.Euler(0f, 0f, -6f);
        _hintText = Label("Hint", _hint, new Vector2(0f, -4f), new Vector2(_hint.sizeDelta.x - 26f, _hint.sizeDelta.y - 30f), NameInk, 10f, 20f, _font);
        _hint.gameObject.SetActive(false);
    }

    /// <summary>MASTER's two copper rods (stretched from their pivots) and its red handle on top.</summary>
    private void BuildLever(RectTransform layer)
    {
        LayoutMaster master = _layout.Master;
        Sprite rod = Sprite("master_rod"), handle = Sprite("master_handle");
        Vector2 rodSize = rod != null ? rod.rect.size : new Vector2(34f, 362f);
        _rodSize = rodSize;
        _rods = new RectTransform[master.RodPivots.Count];
        for (int i = 0; i < _rods.Length; i++)
        {
            _rods[i] = Picture("Rod " + i, layer, rod, rod != null ? Color.white : new Color(0.72f, 0.4f, 0.22f, 1f), Local(master.RodPivots[i]), rodSize).rectTransform;
            _rods[i].pivot = new Vector2(master.RodAxisX / rodSize.x, (rodSize.y - master.RodPivotY) / rodSize.y);
        }
        Vector2 handleSize = handle != null ? handle.rect.size : new Vector2(148f, 61f);
        _handle = Picture("Handle", layer, handle, handle != null ? Color.white : LampRed, Local(master.From), handleSize).rectTransform;
        _handle.pivot = new Vector2(master.HandlePivot.X / handleSize.x, (handleSize.y - master.HandlePivot.Y) / handleSize.y);
    }

    /// <summary>Each module's PLACE and PERIOD cords (homed in its sockets) and the power cords (homed on the power row), with their targets.</summary>
    private void BuildCords()
    {
        foreach (LayoutDestination d in _layout.Destinations)
        {
            _placeJacks.Add(d.Place);
            for (int r = 0; r < _layout.PeriodRows; r++)
                _periodJacks.Add(r < d.Periods.Count ? d.Periods[r].Jack : null);
        }
        foreach (LayoutModule m in _layout.Modules)
            _modulePower.Add(m.Power);
        for (int m = 0; m < _layout.Modules.Count; m++)
        {
            Color rubber = ModuleRubber[m % ModuleRubber.Length];
            _cords.Add(MakeCord(CordKind.Place, m, PlacePlugs[m % PlacePlugs.Length], rubber));
            _cords.Add(MakeCord(CordKind.Period, m, "plug_period", rubber));
        }
        int power = Mathf.Min(_config.powerCords, _layout.PowerRow.Count);
        for (int c = 0; c < power; c++)
        {
            Cord cord = MakeCord(CordKind.Power, c, "plug_power", PowerRubber);
            cord.Shown = true;
            _cords.Add(cord);
        }
        int rings = Mathf.Max(_placeJacks.Count, Mathf.Max(_periodJacks.Count, _modulePower.Count));
        for (int i = 0; i < rings; i++)
            _rings.Add(Picture("Target " + i, _ringLayer, Sprite("lamp_glow"), Color.clear, Vector2.zero, new Vector2(64f, 64f)));
    }

    private Cord MakeCord(CordKind kind, int index, string plug, Color rubber)
    {
        var line = new GameObject(kind + " cord " + index, typeof(RectTransform), typeof(CordGraphic));
        line.layer = gameObject.layer;
        var rect = (RectTransform)line.transform;
        rect.SetParent(_cordLayer, false);
        rect.sizeDelta = new Vector2(_layout.Width, _layout.Height);
        var graphic = line.GetComponent<CordGraphic>();
        graphic.color = rubber;
        Sprite sprite = Sprite(plug);
        Vector2 size = sprite != null ? sprite.rect.size : new Vector2(40f, 40f);
        return new Cord
        {
            Kind = kind,
            Index = index,
            Line = graphic,
            Root = Picture(kind + " root " + index, _plugLayer, sprite, sprite != null ? Color.white : rubber, Vector2.zero, size),
            Plug = Picture(kind + " plug " + index, _plugLayer, sprite, sprite != null ? Color.white : rubber, Vector2.zero, size),
        };
    }

    // ------------------------------------------------------------------ the day's words and cords

    /// <summary>Prints today's destinations and periods on the blank plates (a slot or row with no place stays blank).</summary>
    private void PrintLabels()
    {
        if (_names == null)
            return;
        for (int d = 0; d < _names.Length; d++)
        {
            SwitchDestination dest = _directory != null && d < _directory.Destinations.Count ? _directory.Destinations[d] : null;
            _names[d].text = dest != null ? dest.Label : string.Empty;
            for (int r = 0; r < _tags[d].Length; r++)
                _tags[d][r].text = dest != null && r < dest.Periods.Count ? dest.Periods[r].Label : string.Empty;
        }
    }

    /// <summary>Hangs the hint card (or takes it down); a card with no words yet (Generate World not run) stays down.</summary>
    private void ShowHint(bool show)
    {
        if (_hint == null)
            return;
        string words = show ? Words(SwitchboardWords.Hint) : string.Empty;
        _hintText.text = words;
        _hint.gameObject.SetActive(words.Length > 0);
    }

    /// <summary>Every cord's plug where the rules have it (the day's start, the cheat's modules), shown for the modules in service.</summary>
    private void SyncCordsFromBoard()
    {
        if (_board == null)
            return;
        foreach (Cord c in _cords)
        {
            switch (c.Kind)
            {
                case CordKind.Place:
                    c.Shown = _board.Modules[c.Index].State != SwitchModuleState.Closed;
                    c.Seat = _board.Modules[c.Index].PlaceJack;
                    break;
                case CordKind.Period:
                    SwitchModule m = _board.Modules[c.Index];
                    c.Shown = m.State != SwitchModuleState.Closed;
                    c.Seat = m.HasPeriod ? m.PeriodJack.Destination * _layout.PeriodRows + m.PeriodJack.Period : -1;
                    break;
                case CordKind.Power:
                    c.Seat = _board.PowerCordModule(c.Index);
                    break;
            }
        }
    }

    // ------------------------------------------------------------------ the turn

    /// <summary>The turn to the board and back (eased, cut under Reduced Motion): the plate grows from the painted board on the wall (squeezed as the wall is seen at an angle) until it fills the screen (whole: at 16:9 exactly, on another shape with the dark matte beside it), fading in over its first third, the matte darkening behind.</summary>
    private void StepTurn(float dt)
    {
        if (_boardLayer == null)
            return;
        float target = _open ? 1f : 0f;
        float seconds = MotionPreference.Reduced ? 0f : Mathf.Max(0f, _config.easeSeconds);
        _openT = seconds <= 0f ? target : Mathf.MoveTowards(_openT, target, dt / seconds);
        bool shown = _openT > 0f;
        if (_boardLayer.gameObject.activeSelf != shown)
            _boardLayer.gameObject.SetActive(shown);
        if (!shown)
            return;
        float e = _openT * _openT * (3f - 2f * _openT);
        Vector2 canvas = _canvasRect.rect.size;
        float fit = Mathf.Min(canvas.x / _layout.Width, canvas.y / _layout.Height);
        Rect wall = WallRect(canvas);
        var from = new Vector2(wall.width / _layout.Board.W, wall.height / _layout.Board.H);
        Vector2 boardCentre = Local(_layout.Board.Centre);
        Vector2 start = wall.center - Vector2.Scale(boardCentre, from);
        Vector2 scale = Vector2.Lerp(from, new Vector2(fit, fit), e);
        _shake = MotionPreference.Reduced ? 0f : _shake * Mathf.Exp(-dt * 16f);
        Vector2 jitter = _shake > 0.05f ? Random.insideUnitCircle * _shake * fit : Vector2.zero;
        _plate.localScale = new Vector3(scale.x, scale.y, 1f);
        _plate.anchoredPosition = Vector2.Lerp(start, Vector2.zero, e) + jitter;
        _boardGroup.alpha = Mathf.Clamp01(_openT / 0.3f);
        _boardGroup.blocksRaycasts = _open;
        _matte.color = new Color(MatteColor.r, MatteColor.g, MatteColor.b, e * 0.97f);
    }

    /// <summary>The painted board's rectangle in the canvas (its centre at the origin): the config's viewport rectangle measured at 16:9, moved for the screen's shape (the office camera keeps its height of view).</summary>
    private Rect WallRect(Vector2 canvas)
    {
        Rect r = _config.wallRect;
        float k = canvas.y > 0f ? (16f / 9f) / (canvas.x / canvas.y) : 1f;
        float x0 = 0.5f + (r.xMin - 0.5f) * k, x1 = 0.5f + (r.xMax - 0.5f) * k;
        return Rect.MinMaxRect((x0 - 0.5f) * canvas.x, (r.yMin - 0.5f) * canvas.y, (x1 - 0.5f) * canvas.x, (r.yMax - 0.5f) * canvas.y);
    }

    /// <summary>A small shake of the board (a clunk, a fuse), in plate pixels; none under Reduced Motion.</summary>
    private void Shake(float pixels)
    {
        if (!MotionPreference.Reduced && Shown)
            _shake = Mathf.Max(_shake, pixels);
    }

    /// <summary>True while the board shows (turned to, or turning): its flares and shakes play only then, never stored up for later.</summary>
    private bool Shown => _boardLayer != null && _boardLayer.gameObject.activeSelf;

    // ------------------------------------------------------------------ the office view

    /// <summary>The painted board in the office view: its click area (with the hand cursor) and a warm glow on hover only in the plain office view (ControlRules.BoardOnTheWall); its top lamp glows while a call waits (blinking while one is unanswered).</summary>
    private void DrawWall()
    {
        if (_wallLayer == null)
            return;
        bool live = _controls != null && !_open && _openT <= 0f && ControlRules.BoardOnTheWall(_controls.State());
        if (_hitZone.gameObject.activeSelf != live)
            _hitZone.gameObject.SetActive(live);
        Vector2 canvas = _canvasRect.rect.size;
        Rect wall = WallRect(canvas);
        _hitZone.anchoredPosition = wall.center;
        _hitZone.sizeDelta = wall.size;

        float dt = Time.unscaledDeltaTime;
        bool hover = live && UnityEngine.InputSystem.Mouse.current != null
                     && RectTransformUtility.RectangleContainsScreenPoint(_hitZone, UnityEngine.InputSystem.Mouse.current.position.ReadValue(), null);
        _wallHover.rectTransform.anchoredPosition = wall.center;
        _wallHover.color = Fade(_wallHover.color, LampWarm, hover ? 0.18f : 0f, dt * 6f);

        float hz = 0f;
        bool waiting = false;
        if (_board != null)
            for (int d = 0; d < _board.Destinations; d++)
            {
                SwitchCallLamp state = _board.CallLamp(d);
                if (state == SwitchCallLamp.Calling)
                    hz = Mathf.Max(hz, _board.BlinkHz(d));
                waiting |= _board.WaitingAt(d) > 0;
            }
        _wallPhase += dt * hz;
        float level = !live ? 0f : hz > 0f ? Blink(_wallPhase) : waiting ? 0.35f : 0f;
        Vector2 lamp = new Vector2((0.5f + (_config.wallLamp.x - 0.5f) * (canvas.y > 0f ? (16f / 9f) / (canvas.x / canvas.y) : 1f) - 0.5f) * canvas.x,
                                   (_config.wallLamp.y - 0.5f) * canvas.y);
        _wallGlow.rectTransform.anchoredPosition = lamp;
        _wallDot.rectTransform.anchoredPosition = lamp;
        _wallGlow.color = new Color(LampAmber.r, LampAmber.g, LampAmber.b, level * 0.9f);
        _wallDot.color = new Color(1f, 1f, 1f, level);
    }

    /// <summary>The painted board was clicked in the office view: the same as its key (OfficeControls closes what is in the way first).</summary>
    private void OpenFromWall()
    {
        if (_controls != null)
            _controls.Press(ControlKey.Switchboard);
    }

    // ------------------------------------------------------------------ drawing

    /// <summary>Draws the board's live parts for this frame.</summary>
    private void Draw(float dt)
    {
        if (!Shown)
            return;
        DrawLamps(dt);
        DrawModules();
        DrawLever(dt);
        DrawCords(dt);
        DrawTargets();
        DrawFx(dt);
    }

    /// <summary>The call lamps (blinking amber faster the longer a call waits, steady amber connected, green DONE), the modules' (amber while calibrating, slow when it lacks power; green open; red fuse) and MASTER's (green blinking while a route waits for the pull, steady while one runs).</summary>
    private void DrawLamps(float dt)
    {
        for (int d = 0; d < _callLamps.Length; d++)
        {
            SwitchCallLamp state = _board != null ? _board.CallLamp(d) : SwitchCallLamp.Dark;
            Lamp lamp = _callLamps[d];
            lamp.Phase += dt * (_board != null ? _board.BlinkHz(d) : 1f);
            switch (state)
            {
                case SwitchCallLamp.Calling: SetLamp(lamp, LampColour.Amber, Blink(lamp.Phase), dt); break;
                case SwitchCallLamp.Connected: SetLamp(lamp, LampColour.Amber, 0.8f, dt); break;
                case SwitchCallLamp.Done: SetLamp(lamp, LampColour.Green, 1f, dt); break;
                default: SetLamp(lamp, LampColour.Amber, 0f, dt); break;
            }
        }
        bool running = false;
        for (int m = 0; m < _moduleLamps.Length; m++)
        {
            Lamp lamp = _moduleLamps[m];
            SwitchModule module = _board != null ? _board.Modules[m] : null;
            SwitchModuleState state = module != null ? module.State : SwitchModuleState.Closed;
            switch (state)
            {
                case SwitchModuleState.Committed:
                    running |= module.Powered;
                    lamp.Phase += dt * (module.Powered ? 1.4f : 0.8f);
                    SetLamp(lamp, LampColour.Amber, module.Powered ? 0.55f + 0.45f * Mathf.Cos(lamp.Phase * 2f * Mathf.PI) : Blink(lamp.Phase), dt);
                    break;
                case SwitchModuleState.Open:
                    running = true;
                    SetLamp(lamp, LampColour.Green, 1f, dt);
                    break;
                case SwitchModuleState.Blown:
                    lamp.Phase += dt * 5f;
                    SetLamp(lamp, LampColour.Red, Blink(lamp.Phase), dt);
                    break;
                default:
                    SetLamp(lamp, LampColour.Amber, 0f, dt);
                    break;
            }
        }
        _masterLamp.Phase += dt * 2f;
        bool pending = _board != null && _board.MasterPending;
        SetLamp(_masterLamp, LampColour.Green, pending ? Blink(_masterLamp.Phase) : running ? 0.6f : 0f, dt);
    }

    /// <summary>Each module's veil (not in service, but for 04's and 05's painted placards) and display: PULL while MASTER waits for it, the calibration's countdown (alternating with POWER while it lacks a cord), OPEN, DONE, FUSE, CLOSED.</summary>
    private void DrawModules()
    {
        float now = Time.unscaledTime;
        for (int m = 0; m < _displays.Length; m++)
        {
            LayoutModule layout = _layout.Modules[m];
            SwitchModule module = _board != null ? _board.Modules[m] : null;
            SwitchModuleState state = module != null ? module.State : SwitchModuleState.Closed;
            bool veiled = state == SwitchModuleState.Closed && !layout.Placarded;
            if (_veils[m].gameObject.activeSelf != veiled)
                _veils[m].gameObject.SetActive(veiled);
            bool showDisplay = !layout.Placarded;
            if (_displays[m].Glass.gameObject.activeSelf != showDisplay)
                _displays[m].Glass.gameObject.SetActive(showDisplay);
            if (!showDisplay)
                continue;
            bool flash = Mathf.Repeat(now, 0.9f) < 0.55f, alternate = Mathf.Repeat(now, 1.4f) < 0.7f;
            int left = state == SwitchModuleState.Committed ? Mathf.CeilToInt(module.CalibrationLeft) : 0;
            if (state == SwitchModuleState.Committed)
                Tick(m, module);
            else
                _lastLeft[m] = 0f;
            int shows = ((((((int)state * 2 + (flash ? 1 : 0)) * 2 + (alternate ? 1 : 0)) * 2 + (module != null && module.Sent > 0 ? 1 : 0)) * 2
                          + (module != null && module.Patched ? 1 : 0)) * 2 + (module != null && module.Powered ? 1 : 0)) * 10000 + Mathf.Clamp(left, 0, 9999);
            if (shows == _displays[m].Shows)
                continue;
            _displays[m].Shows = shows;
            string text = string.Empty;
            Color ink = LampAmber;
            switch (state)
            {
                case SwitchModuleState.Closed:
                    text = Words(SwitchboardWords.Closed);
                    ink = new Color(LampRed.r, LampRed.g, LampRed.b, 0.55f);
                    break;
                case SwitchModuleState.Patching:
                    text = module.Patched && flash ? Words(SwitchboardWords.Pull) : string.Empty;
                    break;
                case SwitchModuleState.Committed:
                    string clock = (left / 60) + ":" + (left % 60).ToString("00");
                    text = module.Powered || alternate ? clock : Words(SwitchboardWords.Power);
                    if (text.Length == 0)
                        text = clock;
                    break;
                case SwitchModuleState.Open:
                    text = Words(module.Sent > 0 ? SwitchboardWords.Done : SwitchboardWords.Open);
                    ink = LampGreen;
                    break;
                case SwitchModuleState.Blown:
                    text = flash ? Words(SwitchboardWords.Fuse) : string.Empty;
                    ink = LampRed;
                    break;
            }
            _displays[m].Text.text = text;
            _displays[m].Text.color = ink;
        }
    }

    /// <summary>The last seconds of a powered calibration tick.</summary>
    private void Tick(int m, SwitchModule module)
    {
        float left = module.CalibrationLeft;
        if (module.Powered && _lastLeft[m] > 0f && Mathf.CeilToInt(left) < Mathf.CeilToInt(_lastLeft[m]) && Mathf.CeilToInt(_lastLeft[m]) <= _config.tickLastSeconds)
            Play(SwitchSound.Tick, 0.5f);
        _lastLeft[m] = left;
    }

    /// <summary>MASTER at its spring's place on its pull: the handle down its path, the rods stretched from their pivots to it.</summary>
    private void DrawLever(float dt)
    {
        StepLever(dt);
        LayoutMaster master = _layout.Master;
        float t = Mathf.Clamp(_lever.Value, -0.06f, 1f);
        BoardPoint hc = master.From.Lerp(master.To, t);
        _handle.anchoredPosition = Local(hc);
        for (int i = 0; i < _rods.Length; i++)
        {
            BoardPoint top = hc.Plus(master.RodTops[i].X, master.RodTops[i].Y);
            Vector2 pivot = Local(master.RodPivots[i]), dir = Local(top) - pivot;
            float k = dir.magnitude / Mathf.Max(1f, master.RodPivotY - master.RodTopY);
            _rods[i].anchoredPosition = pivot;
            _rods[i].sizeDelta = new Vector2(_rodSize.x, _rodSize.y * k);
            _rods[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        }
    }

    /// <summary>Every cord: a plug at home alone, or the root plug at home, the plug where it is (seated, in the hand, or flying home) and the cord hanging between their tails, swinging as it settles.</summary>
    private void DrawCords(float dt)
    {
        var sagTuning = SpringTuning.WithRatio(80f, 0.3f);
        var bounceTuning = SpringTuning.WithRatio(520f, 0.35f);
        foreach (Cord c in _cords)
        {
            bool shown = c.Shown && _board != null;
            if (c.Plug.gameObject.activeSelf != shown)
                c.Plug.gameObject.SetActive(shown);
            if (!shown)
            {
                if (c.Root.gameObject.activeSelf)
                    c.Root.gameObject.SetActive(false);
                c.Line.SetPoints(null, 0f);
                continue;
            }
            if (MotionPreference.Reduced)
            {
                c.Sag.Snap(c.Sag.Target);
                c.Bounce.Snap(0f);
            }
            else
            {
                c.Sag.Step(dt, sagTuning);
                c.Bounce.Step(dt, bounceTuning);
            }
            if (c.Returning)
            {
                c.ReturnT = MotionPreference.Reduced ? 1f : c.ReturnT + dt / 0.22f;
                if (c.ReturnT >= 1f)
                {
                    c.Returning = false;
                    Seated(c, c.Seat >= 0 ? 0.6f : 0.4f);
                }
            }
            bool loose = c.Seat >= 0 || c.Held || c.Returning;
            BoardPoint home = Home(c);
            float homeScale = HomeScale(c);
            BoardPoint at;
            float scale;
            if (c.Held)
            {
                at = c.Snap >= 0 ? c.HeldAt.Lerp(Targets(c)[c.Snap].Centre, 0.55f) : c.HeldAt;
                scale = _config.heldScale;
            }
            else if (c.Returning)
            {
                float e = c.ReturnT * c.ReturnT * (3f - 2f * c.ReturnT);
                at = c.ReturnFrom.Lerp(c.Seat >= 0 ? SeatPoint(c) : home, e);
                scale = Mathf.Lerp(c.ReturnFromScale, c.Seat >= 0 ? SeatScale(c) : homeScale, e);
            }
            else if (c.Seat >= 0)
            {
                at = SeatPoint(c);
                scale = SeatScale(c);
            }
            else
            {
                at = home;
                scale = homeScale;
            }
            c.Drawn = at;
            c.DrawnScale = scale;
            PlaceImage(c.Plug, at, scale * (1f + 0.14f * c.Bounce.Value));
            OnTop(c, c.Held || c.Returning);
            if (c.Root.gameObject.activeSelf != loose)
                c.Root.gameObject.SetActive(loose);
            if (!loose)
            {
                c.Line.SetPoints(null, 0f);
                continue;
            }
            PlaceImage(c.Root, home, homeScale);
            BoardPoint a = home.Plus(_layout.PlugTail.X * homeScale, _layout.PlugTail.Y * homeScale);
            BoardPoint b = at.Plus(_layout.PlugTail.X * scale, _layout.PlugTail.Y * scale);
            float chord = a.DistanceTo(b);
            float sag = SwitchCords.Sag(chord, _config.cordSlack) * Mathf.Max(0f, c.Sag.Value);
            SwitchCords.Sample(a, b, _cordAxis, SwitchCords.Reach(chord, _config.cordReach, _config.cordReachMax), sag, _config.cordSegments, _curve);
            _curveLocal.Clear();
            foreach (BoardPoint p in _curve)
                _curveLocal.Add(Local(p));
            c.Line.SetPoints(_curveLocal, _config.cordWidth);
        }
    }

    /// <summary>A cord in the hand (or flying home) draws over every other cord and plug; back in its layers once it lands.</summary>
    private void OnTop(Cord c, bool top)
    {
        if (c.OnTop == top)
            return;
        c.OnTop = top;
        c.Line.rectTransform.SetParent(top ? _heldLayer : _cordLayer, false);
        c.Plug.rectTransform.SetParent(top ? _heldLayer : _plugLayer, false);
    }

    /// <summary>While a plug is in the hand: a faint glow on every jack it may seat in, a strong one on the jack it would seat in now.</summary>
    private void DrawTargets()
    {
        IReadOnlyList<BoardSocket> targets = _held != null ? Targets(_held) : null;
        for (int i = 0; i < _rings.Count; i++)
        {
            bool on = targets != null && i < targets.Count && targets[i] != null && Allowed(_held, i);
            Image ring = _rings[i];
            if (ring.gameObject.activeSelf != on)
                ring.gameObject.SetActive(on);
            if (!on)
                continue;
            BoardSocket s = targets[i];
            ring.rectTransform.anchoredPosition = Local(s.Centre);
            ring.rectTransform.sizeDelta = Vector2.one * s.RingRadius * 3.2f;
            ring.color = new Color(LampWarm.r, LampWarm.g, LampWarm.b, _held.Snap == i ? 0.65f : 0.2f);
        }
    }

    // ------------------------------------------------------------------ flares

    /// <summary>A glow blooming at <paramref name="at"/> to <paramref name="size"/> times a lamp's glow and fading (a departure, an open route).</summary>
    private void Flare(BoardPoint at, Color color, float size)
    {
        if (!Shown)
            return;
        Image glow = Picture("Flare", _fxLayer, Sprite("lamp_glow"), color, Local(at), new Vector2(96f, 96f));
        _fx.Add(new Fx { Rect = glow.rectTransform, Graphic = glow, Color = color, Life = 0.8f, FromScale = 0.8f, ToScale = size });
    }

    /// <summary>Sparks flying from <paramref name="at"/> and falling (a popped fuse), and a red bloom.</summary>
    private void Sparks(BoardPoint at)
    {
        Flare(at, LampRed, 2.4f);
        if (MotionPreference.Reduced || !Shown)
            return;
        for (int i = 0; i < 12; i++)
        {
            Image spark = Picture("Spark", _fxLayer, Sprite("lamp_glow"), LampWarm, Local(at), new Vector2(14f, 14f));
            Vector2 v = Random.insideUnitCircle.normalized * Random.Range(140f, 320f) + new Vector2(0f, 160f);
            _fx.Add(new Fx { Rect = spark.rectTransform, Graphic = spark, Color = LampWarm, Velocity = v, Life = Random.Range(0.35f, 0.6f), FromScale = 1f, ToScale = 0.4f, Falls = true });
        }
    }

    /// <summary>Words rising from <paramref name="at"/> and fading (a group's tally, its bonus); nothing for empty words.</summary>
    private void Rise(BoardPoint at, string words, Color color, float delay = 0f)
    {
        if (string.IsNullOrEmpty(words) || !Shown)
            return;
        TextMeshProUGUI text = Label("Rise", _fxLayer, Local(at), new Vector2(180f, 28f), color, 12f, 24f, _font);
        text.text = words;
        text.color = Clear(color);
        _fx.Add(new Fx { Rect = text.rectTransform, Graphic = text, Color = color, Velocity = MotionPreference.Reduced ? Vector2.zero : new Vector2(0f, 46f), Life = 1.4f, Delay = delay, FromScale = 1f, ToScale = 1f });
    }

    private void DrawFx(float dt)
    {
        for (int i = _fx.Count - 1; i >= 0; i--)
        {
            Fx f = _fx[i];
            if (f.Delay > 0f)
            {
                f.Delay -= dt;
                continue;
            }
            f.Age += dt;
            float t = Mathf.Clamp01(f.Age / f.Life);
            if (f.Falls)
                f.Velocity += new Vector2(0f, -900f) * dt;
            f.Rect.anchoredPosition += f.Velocity * dt;
            f.Rect.localScale = Vector3.one * Mathf.Lerp(f.FromScale, f.ToScale, 1f - (1f - t) * (1f - t));
            f.Graphic.color = new Color(f.Color.r, f.Color.g, f.Color.b, f.Color.a * (1f - t * t));
            if (t >= 1f)
            {
                Destroy(f.Rect.gameObject);
                _fx.RemoveAt(i);
            }
        }
    }

    // ------------------------------------------------------------------ sounds

    /// <summary>Plays <paramref name="sound"/>: the sound bank's file for its cue when there is one, else the switchboard's own placeholder at <paramref name="volume"/> (the bank's placeholder volume).</summary>
    private void Play(SwitchSound sound, float volume)
    {
        if (Sounds.Play(SwitchboardSounds.CueId(sound)) || _audio == null)
            return;
        if (!_clips.TryGetValue(sound, out AudioClip clip))
        {
            const int rate = 44100;
            float[] samples = SwitchboardSounds.Make(sound, rate);
            clip = AudioClip.Create("Switchboard " + sound, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            _clips[sound] = clip;
        }
        float placeholder = Sounds.Bank != null ? Sounds.Bank.placeholderVolume : 0.5f;
        _audio.PlayOneShot(clip, volume * placeholder * 2f);
    }

    private void DestroySounds()
    {
        foreach (AudioClip clip in _clips.Values)
            if (clip != null)
                Destroy(clip);
        _clips.Clear();
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>A point of the plate in the plate's own space (its centre at the origin, y up).</summary>
    private Vector2 Local(BoardPoint p) => new Vector2(p.X - _layout.Width / 2f, _layout.Height / 2f - p.Y);

    /// <summary>Where a module's display sits: centred on its face, between its number plate and its sockets.</summary>
    private static BoardPoint DisplayCentre(LayoutModule module) =>
        new BoardPoint(module.Rect.Centre.X, (module.NumberPlate.Y + module.NumberPlate.H + module.Place.Centre.Y - module.Place.RingRadius) / 2f);

    /// <summary>A lamp lit at <paramref name="level"/> (0 dark to 1) in <paramref name="colour"/>, easing like a filament.</summary>
    private void SetLamp(Lamp lamp, LampColour colour, float level, float dt)
    {
        lamp.Shown = Mathf.MoveTowards(lamp.Shown, Mathf.Clamp01(level), dt * 14f);
        Sprite lit = _lit[(int)colour];
        if (lit != null && lamp.Lit.sprite != lit)
            lamp.Lit.sprite = lit;
        Color glow = colour == LampColour.Green ? LampGreen : colour == LampColour.Red ? LampRed : LampAmber;
        lamp.Lit.color = new Color(1f, 1f, 1f, lamp.Shown);
        lamp.Glow.color = new Color(glow.r, glow.g, glow.b, lamp.Shown * 0.75f);
    }

    /// <summary>A blink at <paramref name="phase"/> (cycles): mostly on or off, with soft edges.</summary>
    private static float Blink(float phase)
    {
        float c = 0.5f + 0.5f * Mathf.Cos(phase * 2f * Mathf.PI);
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((c - 0.3f) / 0.4f));
    }

    private static Color Clear(Color c) => new Color(c.r, c.g, c.b, 0f);

    private static Color Fade(Color now, Color to, float alpha, float step) =>
        new Color(to.r, to.g, to.b, Mathf.MoveTowards(now.a, alpha, step));

    private void PlaceImage(Image image, BoardPoint at, float scale)
    {
        image.rectTransform.anchoredPosition = Local(at);
        image.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>A switchboard sprite by its file name (cached; null without the file).</summary>
    private Sprite Sprite(string name)
    {
        if (!_sprites.TryGetValue(name, out Sprite sprite))
        {
            sprite = SlotArt.Sprite(ArtSlots.SwitchboardSprite(name));
            _sprites[name] = sprite;
        }
        return sprite;
    }

    /// <summary>A loaded TextMesh Pro font by its asset's name, else the default font.</summary>
    private static TMP_FontAsset Font(string name)
    {
        foreach (TMP_FontAsset font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            if (font != null && font.name == name)
                return font;
        return TMP_Settings.defaultFontAsset;
    }

    /// <summary>A child stretched over its parent.</summary>
    private static RectTransform Stretch(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        Fill(rect);
        return rect;
    }

    private static void Fill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    /// <summary>A layer of the plate: the plate's size, centred, so its children place by the plate's pixels (Local).</summary>
    private RectTransform Layer(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(_layout.Width, _layout.Height);
        return rect;
    }

    /// <summary>An image centred at <paramref name="at"/> in its parent's space.</summary>
    private static Image Picture(string name, Transform parent, Sprite sprite, Color color, Vector2 at, Vector2 size, bool raycast = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = at;
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private static Vector2 Size(BoardRect r) => new Vector2(r.W, r.H);

    /// <summary>Words centred at <paramref name="at"/> in <paramref name="parent"/>'s space, fitted into <paramref name="size"/>, shrinking from <paramref name="max"/> to <paramref name="min"/> to fit.</summary>
    private static TextMeshProUGUI Label(string name, Transform parent, Vector2 at, Vector2 size, Color ink, float min, float max, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = at;
        rect.sizeDelta = size;
        var text = go.GetComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.color = ink;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = min;
        text.fontSizeMax = max;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        text.text = string.Empty;
        return text;
    }
}
