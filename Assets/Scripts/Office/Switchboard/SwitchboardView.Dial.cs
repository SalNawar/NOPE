using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The Stargate dialer (SwitchboardConfigSO.look Stargate; Saleh 2026-11-07:
/// "a switchboard that is intresting ( my friends suggested something based
/// on stargate)"; the director's D1, docs/pending/SWITCHBOARD.md "Stargate
/// dialer"). The board keeps its rules (Switchboard): place, period, power,
/// the 20 s calibration only while powered, the fuse, the group send and its
/// bonus, the ramp, the call lamps, the hint card, its words and its sounds.
/// What changes is how a connection is made. A slate face (GatePaint.Face)
/// covers the Classic jacks; MASTER's panel stays the plate's. The call row
/// shows the nations' emblems, each with its call lamp and the pips of the
/// eras that wait there. Each module in service is a dial (GateDial): the
/// left hand turns its inner ring (it follows the hand on a spring and ticks
/// past each glyph); let go, it eases into its detent (ringSettleSeconds) and
/// there the ring's rule lands (GateDial.Resolve): a nation under the top
/// chevron locks the PLACE connection, then an era under the second locks the
/// PERIOD, each with a click, its chevron lit in one frame and held. Power is
/// "Move the units": cells in the rack, carried into a dial's cradle (one
/// cell a bar; a route needs its era's bars, the mark over the cradle); a
/// cell lifted out darkens its dial at once; a let-go cell seats with a click
/// and a 2 px settle, or flies home to the rack. MASTER is the final lock: it
/// engages every dial whose chevrons are locked and whose bars reach the
/// mark; the seven chevrons engage one every calibration / 7 while powered;
/// then the hub fills with the horizon and the group leaves. A wrong era: the
/// two lock chevrons flicker red while the fuse is out. Reduced Motion: the
/// ring goes to its detent at once, the chevrons light with no flash, the
/// horizon stands still. Esc or a right-click lets go of what the hand holds
/// with nothing changed (CancelDrag), then backs out.
/// </summary>
public sealed partial class SwitchboardView
{
    /// <summary>The eras with a glyph on the ring (Ancient to Modern: EraSO.order 0 to 4; GatePaint.EraInked).</summary>
    private const int GateEras = 5;

    /// <summary>The settle of a spring on its target: the fraction left after the ring's settle time (2 %, critical damping).</summary>
    private const float SettleOmegaSeconds = 5.8f;

    /// <summary>Seconds a refused lock's chevron shows red (the route is another dial's).</summary>
    private const float RefuseSeconds = 0.45f;

    private static readonly Color GlyphInk = new Color(0.16f, 0.11f, 0.07f, 1f);
    private static readonly Color EraInk = new Color(0.47f, 0.13f, 0.1f, 1f);
    private static readonly Color OriginInk = new Color(0.46f, 0.33f, 0.15f, 1f);
    private static readonly Color EmblemCream = new Color(0.94f, 0.9f, 0.8f, 1f);
    private static readonly Color PipDark = new Color(0.55f, 0.45f, 0.3f, 0.28f);

    /// <summary>A dial not in service today: a darker veil than a Classic module's (a dial's brass reads bright under the Classic one).</summary>
    private static readonly Color DialVeilColor = new Color(0.04f, 0.025f, 0.018f, 0.68f);

    /// <summary>One dial: its parts and how its ring moves.</summary>
    private sealed class Dial
    {
        public int Index;
        public RectTransform Ring;
        public Image[] Lenses;
        public Image[] LensGlows;
        public Image Horizon;
        public Image Mark;
        public Image[] Bars;
        public Spring Turn = Spring.At(0f);
        public float HandLast;
        public float GrabDetent;
        public int LastSlot;
        public float LastTick;
        public bool Settling;
        public float SettleAge;
        public DialAction Pending;
        public GateGlyph PendingTop;
        public GateGlyph PendingSecond;
        public readonly float[] Flash = { 99f, 99f, 99f, 99f, 99f, 99f, 99f };
        public int Engaged;
        public float Refused;
        public int RefusedChevron;
        public float Horizon01;
        public float Breath;
        public float FusePhase;
    }

    /// <summary>One power cell: its picture and where it is (in the rack, in a cradle slot, in the hand, flying).</summary>
    private sealed class CellPart
    {
        public int Index;
        public Image Body;
        public Image Glow;
        public int Module = -1;
        public int Slot = -1;
        public int FromModule = -1;
        public int FromSlot = -1;
        public bool Held;
        public BoardPoint HeldAt;
        public BoardPoint Offset;
        public int Hover = -1;
        public bool Flying;
        public float FlyT;
        public float FlySeconds;
        public BoardPoint FlyFrom;
        public float FlyFromScale = 1f;
        public Spring Settle = Spring.At(0f);
        public BoardPoint Drawn;
        public float DrawnScale = 1f;
    }

    /// <summary>One nation's call slot: its emblem and its eras' pips.</summary>
    private sealed class CallSlot
    {
        public Image Emblem;
        public Image[] Pips;
    }

    private GateBoardLayout _gate;
    private GateGlyph[] _ring;
    private Dial[] _dials;
    private CellPart[] _cellParts;
    private CallSlot[] _callSlots;
    private string[] _nationIds = new string[0];
    private string[] _nationNames = new string[0];
    private string[] _eraNames = new string[0];
    private readonly List<SwitchRoute> _todayRoutes = new List<SwitchRoute>();
    private readonly List<Object> _gateArt = new List<Object>();
    private Texture2D _faceTexture;
    private Sprite _ringSprite;
    private Sprite _discSprite;
    private Sprite[] _eraSprites;
    private RectTransform _faceLayer;
    private RectTransform _callLayer;
    private RectTransform _dialLayer;
    private RectTransform _moduleLayer;
    private RectTransform _lampLayer;
    private RectTransform _cellLayer;
    private Dial _turning;
    private CellPart _carried;

    /// <summary>True when the board is the Stargate dialer (SwitchboardConfigSO.look).</summary>
    internal bool Dialer => _config != null && _config.Dialer;

    // ------------------------------------------------------------------ building

    /// <summary>The dialer's empty layers in drawing order, under the hint card and MASTER (BuildParts; filled once the day knows the nations: BuildGate).</summary>
    private void BuildGateLayers()
    {
        _faceLayer = Layer("Dialer face", _plate);
        _callLayer = Layer("Calls", _plate);
        _dialLayer = Layer("Dials", _plate);
        _moduleLayer = Layer("Modules", _plate);
        _lampLayer = Layer("Lamps", _plate);
    }

    /// <summary>Builds the dialer's parts once, from today's library: the face, the call row, the dials (rings, chevrons, cradles, horizons), the cells and their glows.</summary>
    private void BuildGate(ContentLibrarySO library)
    {
        if (_dials != null)
            return;
        var ids = new List<string>();
        var names = new List<string>();
        var emblems = new List<string>();
        if (library != null)
            foreach (NationSO n in library.Nations)
            {
                if (n == null || string.IsNullOrEmpty(n.id))
                    continue;
                ids.Add(n.id);
                names.Add(Caps(n.displayName, n.id));
                emblems.Add(n.passport != null ? n.passport.emblem : string.Empty);
            }
        _nationIds = ids.ToArray();
        _nationNames = names.ToArray();
        var eras = new string[GateEras];
        if (library != null)
            foreach (EraSO e in library.Eras)
                if (e != null && e.group == null && !e.isFuture && e.order >= 0 && e.order < GateEras)
                    eras[e.order] = Caps(e.displayName, e.id);
        _eraNames = eras;
        int modules = _layout.Modules.Count;
        _gate = new GateBoardLayout(Mathf.Max(1, _nationIds.Length), Mathf.Max(0, _config.powerCells), Mathf.Max(1, _config.cradleSlots));
        _ring = GateDial.Ring(_nationIds.Length, GateEras);

        _faceTexture = Art(GatePaint.Face(_gate, modules, GateEras), "Switchboard dialer face");
        Picture("Face", _faceLayer, SpriteOf(_faceTexture), Color.white, Local(_gate.Cover.Centre), Size(_gate.Cover));
        _ringSprite = SpriteOf(Art(GatePaint.Ring(_ring.Length), "Switchboard dial ring"));
        _discSprite = SpriteOf(Art(GatePaint.Disc(64), "Switchboard disc"));
        _eraSprites = new Sprite[GateEras + 1];
        for (int e = -1; e < GateEras; e++)
            _eraSprites[e + 1] = SpriteOf(Art(GatePaint.Glyph(e, 64), "Switchboard glyph " + e));
        Sprite chevron = SpriteOf(Art(GatePaint.Chevron(), "Switchboard chevron"));
        Sprite lens = SpriteOf(Art(GatePaint.Lens(), "Switchboard lens"));
        Sprite horizon = SpriteOf(Art(GatePaint.Horizon((int)GateBoardLayout.HubRadius), "Switchboard horizon"));
        Sprite cell = SpriteOf(Art(GatePaint.Cell(), "Switchboard cell"));
        Sprite mark = SpriteOf(Art(GatePaint.MarkArrow(), "Switchboard mark"));

        // The call row: each nation's emblem and its eras' pips.
        _callSlots = new CallSlot[_nationIds.Length];
        for (int d = 0; d < _callSlots.Length; d++)
        {
            Sprite emblem = EmblemArt.Sprite(emblems[d]);
            var slot = new CallSlot
            {
                Emblem = Picture("Emblem " + _nationIds[d], _callLayer, emblem, emblem != null ? EmblemCream : Color.clear, Local(_gate.CallEmblem(d)),
                                 Vector2.one * GateBoardLayout.CallEmblemSize),
                Pips = new Image[GateEras],
            };
            for (int e = 0; e < GateEras; e++)
                slot.Pips[e] = Picture("Pip " + _nationIds[d] + " " + e, _callLayer, _eraSprites[e + 1], PipDark, Local(_gate.CallPip(d, e, GateEras)),
                                       Vector2.one * GateBoardLayout.PipSize);
            _callSlots[d] = slot;
        }

        // The dials: the horizon in the hub, the ring and its glyphs, the chevrons and their lenses, the number; the cradle's bars and its mark.
        _dials = new Dial[modules];
        for (int m = 0; m < modules; m++)
        {
            var dial = new Dial { Index = m };
            _dials[m] = dial;
            if (!GateBoardLayout.Big(m))
                continue;
            BoardPoint c = _gate.DialCentre(m);
            RectTransform root = Layer("Dial " + _layout.Modules[m].Id, _dialLayer);
            dial.Horizon = Picture("Horizon", root, horizon, Clear(_config.horizonColour), Local(c), Vector2.one * (2f * GateBoardLayout.HubRadius + 2f));
            Image ring = Picture("Ring", root, _ringSprite, Color.white, Local(c), _ringSprite != null ? _ringSprite.rect.size : Vector2.one * 284f);
            dial.Ring = ring.rectTransform;
            float step = GateDial.SlotDegrees(_ring.Length);
            for (int s = 0; s < _ring.Length; s++)
            {
                GateGlyph g = _ring[s];
                if (g.Kind == GateGlyphKind.Blank)
                    continue;
                Sprite sprite = g.Kind == GateGlyphKind.Nation ? EmblemArt.Sprite(emblems[g.Index]) : _eraSprites[g.Kind == GateGlyphKind.Era ? g.Index + 1 : 0];
                Color ink = g.Kind == GateGlyphKind.Nation ? GlyphInk : g.Kind == GateGlyphKind.Era ? EraInk : OriginInk;
                float a = s * step * Mathf.Deg2Rad;
                Image glyph = Picture("Glyph " + g, dial.Ring, sprite, sprite != null ? ink : Color.clear,
                                      new Vector2(GateBoardLayout.GlyphRadius * Mathf.Sin(a), GateBoardLayout.GlyphRadius * Mathf.Cos(a)), Vector2.one * GateBoardLayout.GlyphSize);
                glyph.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -s * step);
            }
            dial.Lenses = new Image[GateDial.Chevrons];
            dial.LensGlows = new Image[GateDial.Chevrons];
            for (int k = 0; k < GateDial.Chevrons; k++)
            {
                BoardPoint at = _gate.ChevronAt(m, k);
                Quaternion turn = Quaternion.Euler(0f, 0f, -GateDial.ChevronAngle(k));
                dial.LensGlows[k] = Picture("Chevron glow " + (k + 1), root, Sprite("lamp_glow"), Color.clear, Local(at), Vector2.one * 70f);
                Image wedge = Picture("Chevron " + (k + 1), root, chevron, Color.white, Local(at), new Vector2(40f, 36f));
                wedge.rectTransform.localRotation = turn;
                dial.Lenses[k] = Picture("Lens " + (k + 1), root, lens, Color.clear, Local(at), new Vector2(18f, 14f));
                dial.Lenses[k].rectTransform.localRotation = turn;
                dial.Lenses[k].rectTransform.anchoredPosition += (Vector2)(turn * new Vector2(0f, 3f));
            }
            TextMeshProUGUI number = Label("Number " + _layout.Modules[m].Id, root, Local(c.Plus(0f, 54f)), new Vector2(60f, 26f), EmblemCream, 12f, 20f, _mono);
            number.text = _layout.Modules[m].Id;
            dial.Bars = new Image[_gate.CradleSlots];
            for (int s = 0; s < dial.Bars.Length; s++)
                dial.Bars[s] = Picture("Bar " + (s + 1), root, Sprite("lamp_glow"), Clear(LampAmber), Local(_gate.CradleSlot(m, s).Centre), new Vector2(96f, 120f));
            dial.Mark = Picture("Era mark", root, mark, Color.white, Local(_gate.Mark(m, 1)), new Vector2(18f, 12f));
            dial.Mark.gameObject.SetActive(false);
        }
        BuildModules(_moduleLayer);
        BuildLamps(_lampLayer);

        // The cells, in the rack.
        int cells = Mathf.Max(0, _config.powerCells);
        _cellParts = new CellPart[cells];
        for (int i = 0; i < cells; i++)
        {
            var part = new CellPart
            {
                Index = i,
                Glow = Picture("Cell glow " + (i + 1), _cellLayer, Sprite("lamp_glow"), Clear(LampAmber), Local(_gate.Bay(i)), new Vector2(110f, 150f)),
                Body = Picture("Cell " + (i + 1), _cellLayer, cell, Color.white, Local(_gate.Bay(i)), new Vector2(GateBoardLayout.CellW, GateBoardLayout.CellH)),
            };
            part.Drawn = _gate.Bay(i);
            _cellParts[i] = part;
        }
    }

    /// <summary>A painted canvas as a texture (rows flipped bottom up), kept to be destroyed with the view.</summary>
    private Texture2D Art(GateCanvas canvas, string name)
    {
        var texture = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var flipped = new byte[canvas.Pixels.Length];
        int row = canvas.Width * 4;
        for (int y = 0; y < canvas.Height; y++)
            System.Buffer.BlockCopy(canvas.Pixels, y * row, flipped, (canvas.Height - 1 - y) * row, row);
        texture.LoadRawTextureData(flipped);
        texture.Apply(false, true);
        _gateArt.Add(texture);
        return texture;
    }

    /// <summary>A texture as a sprite, a plain quad (the textures are not readable once painted, so no tight mesh), kept to be destroyed with the view.</summary>
    private Sprite SpriteOf(Texture2D texture)
    {
        if (texture == null)
            return null;
        Sprite sprite = UnityEngine.Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        _gateArt.Add(sprite);
        return sprite;
    }

    private void DestroyGateArt()
    {
        foreach (Object o in _gateArt)
            if (o != null)
                Destroy(o);
        _gateArt.Clear();
    }

    // ------------------------------------------------------------------ the day

    /// <summary>The dialer's day: the rules on today's nations and the five eras, the modules the ramp opens, every cell in the rack, today's routes (for the cheat), the hint card.</summary>
    private void BeginGateDay(GameManager game, ContentLibrarySO library, DayPlanSO plan, SwitchboardRules rules)
    {
        BuildGate(library);
        _board = new Switchboard(rules, _layout.Modules.Count, _nationIds.Length, GateEras);
        for (int m = 0; m < _layout.Modules.Count; m++)
            if (rules.ModuleOpen(m, _day) && GateBoardLayout.Big(m))
                _board.SetInService(m, true);
        _todayRoutes.Clear();
        foreach (SwitchPlace place in TodaysPlaces(game, library, plan))
        {
            int d = NationSlot(place.NationId);
            var route = new SwitchRoute(d, place.EraOrder);
            if (d >= 0 && place.EraOrder >= 0 && place.EraOrder < GateEras && !_todayRoutes.Contains(route))
                _todayRoutes.Add(route);
        }
        SyncCellsFromBoard();
        var listed = new List<string>();
        foreach (SwitchRoute r in _todayRoutes)
            listed.Add(RouteName(r));
        Debug.Log($"[Switchboard] Day {_day} (dialer): today's places {string.Join("; ", listed)}; dials in service: {InServiceList()}; {_board.PowerCords} cells in the rack.");
    }

    /// <summary>The destination index of nation <paramref name="nationId"/> on the dialer (its call slot and its emblem on the ring), or -1.</summary>
    internal int NationSlot(string nationId) => System.Array.IndexOf(_nationIds, nationId);

    /// <summary>A route as the log names it ("EGYPT, ANCIENT").</summary>
    private string RouteName(SwitchRoute r) =>
        (r.Destination >= 0 && r.Destination < _nationNames.Length ? _nationNames[r.Destination] : "nation " + r.Destination) + ", "
        + (r.Period >= 0 && r.Period < _eraNames.Length && !string.IsNullOrEmpty(_eraNames[r.Period]) ? _eraNames[r.Period] : "era " + r.Period);

    /// <summary>An approved traveller's call on the dialer: their nation's emblem and their era's glyph (EraSO.order); an era with no glyph has no route (they leave at once, logged).</summary>
    private void GateCall(CaseInstance inst)
    {
        int d = NationSlot(inst.claimedNation.id), e = inst.claimedEra.order;
        if (d < 0 || e < 0 || e >= GateEras)
        {
            Debug.Log($"[Switchboard] {inst.visitorDisplayName} ({inst.claimedNation.id}, {inst.claimedEra.id}): no glyph on the dials for it; they leave at once.");
            return;
        }
        var route = new SwitchRoute(d, e);
        _board.AddCall(route);
        Debug.Log($"[Switchboard] Call: {inst.visitorDisplayName} for {RouteName(route)} (route {route}); {_board.Waiting(route)} waiting there.");
    }

    /// <summary>The cheat's three calls on the dialer: dials 01 to 03 in service, two calls on today's first place and one on another (another era where it can).</summary>
    private string GateTestCalls()
    {
        for (int m = 0; m < GateBoardLayout.BigDials && m < _board.Modules.Count; m++)
            _board.SetInService(m, true);
        SyncCellsFromBoard();
        if (_todayRoutes.Count == 0)
            _todayRoutes.Add(new SwitchRoute(0, 0));
        SwitchRoute first = _todayRoutes[0];
        SwitchRoute second = _todayRoutes.Count > 1 ? _todayRoutes[1] : new SwitchRoute((first.Destination + 1) % Mathf.Max(1, _nationIds.Length), (first.Period + 1) % GateEras);
        foreach (SwitchRoute r in _todayRoutes)
            if (r.Period != first.Period && r != first)
            {
                second = r;
                break;
            }
        var said = new List<string>();
        foreach (SwitchRoute r in new[] { first, first, second })
        {
            _board.AddCall(r);
            said.Add(RouteName(r));
        }
        string line = "Switchboard: 3 test calls (" + string.Join("; ", said) + "); dials " + InServiceList() + " in service.";
        Debug.Log("[Switchboard] " + line);
        return line;
    }

    /// <summary>Every cell where the rules have it: in a cradle (its slot the lowest free), else home in the rack.</summary>
    private void SyncCellsFromBoard()
    {
        if (_cellParts == null || _board == null)
            return;
        foreach (CellPart c in _cellParts)
        {
            int m = _board.PowerCordModule(c.Index);
            c.Held = false;
            c.Flying = false;
            c.Module = m;
            c.Slot = m >= 0 ? FreeSlot(m, c) : -1;
            c.Drawn = Rest(c);
        }
        _carried = null;
    }

    /// <summary>The lowest slot of dial <paramref name="m"/>'s cradle no other cell sits in (but <paramref name="self"/>), or -1.</summary>
    private int FreeSlot(int m, CellPart self)
    {
        for (int s = 0; s < _gate.CradleSlots; s++)
        {
            bool taken = false;
            foreach (CellPart c in _cellParts)
                if (c != self && c.Module == m && c.Slot == s)
                    taken = true;
            if (!taken)
                return s;
        }
        return -1;
    }

    /// <summary>Where a cell rests: its cradle slot (settled a little in), else its rack bay.</summary>
    private BoardPoint Rest(CellPart c) => c.Module >= 0 && c.Slot >= 0 ? _gate.CradleSlot(c.Module, c.Slot).Centre : _gate.Bay(c.Index);

    // ------------------------------------------------------------------ the hand

    /// <summary>The ring in the hand, a cell in the hand, or a fresh press taking one of them (MASTER is Hand's).</summary>
    private void DialHand(Mouse mouse, BoardPoint p)
    {
        if (_turning != null)
        {
            Dial d = _turning;
            float hand = GateDial.AngleOf(_gate.DialCentre(d.Index), p);
            d.Turn.Target += GateDial.Delta(d.HandLast, hand);
            d.HandLast = hand;
            if (!mouse.leftButton.isPressed)
                ReleaseRing();
            return;
        }
        if (_carried != null)
        {
            CellPart c = _carried;
            c.HeldAt = p.Plus(c.Offset.X, c.Offset.Y);
            c.Hover = CradleUnder(c.HeldAt, c);
            if (!mouse.leftButton.isPressed)
                DropCell();
            return;
        }
        if (!mouse.leftButton.wasPressedThisFrame || _board == null)
            return;
        CellPart cell = CellUnder(p);
        if (cell != null)
        {
            GrabCell(cell, p);
            return;
        }
        Dial ring = RingUnder(p);
        if (ring != null)
            GrabRing(ring, p);
    }

    /// <summary>The cell under <paramref name="p"/> that the hand can take (resting, not flying), or null.</summary>
    private CellPart CellUnder(BoardPoint p)
    {
        if (_cellParts == null)
            return null;
        foreach (CellPart c in _cellParts)
            if (!c.Flying && !c.Held && new BoardRect(c.Drawn.X - 26f, c.Drawn.Y - 42f, 52f, 84f).Contains(p))
                return c;
        return null;
    }

    /// <summary>The in-service dial whose ring is under <paramref name="p"/>, or null.</summary>
    private Dial RingUnder(BoardPoint p)
    {
        if (_dials == null)
            return null;
        foreach (Dial d in _dials)
            if (d.Ring != null && _board.Modules[d.Index].State != SwitchModuleState.Closed && _gate.OnRing(d.Index, p))
                return d;
        return null;
    }

    /// <summary>The ring is taken: it follows the hand's angle from here (no jump); nothing changes in the rules until it lands.</summary>
    private void GrabRing(Dial d, BoardPoint p)
    {
        _turning = d;
        d.HandLast = GateDial.AngleOf(_gate.DialCentre(d.Index), p);
        d.Settling = false;
        d.Pending = DialAction.None;
        d.GrabDetent = GateDial.Detent(d.Turn.Value, _ring.Length);
        d.Turn.Target = d.Turn.Value;
        Play(SwitchSound.PlugOut, 0.25f);
    }

    /// <summary>The ring let go: it eases into the nearest detent, where the ring's rule lands (GateDial.Resolve, judged on where it rests).</summary>
    private void ReleaseRing()
    {
        Dial d = _turning;
        _turning = null;
        int slots = _ring.Length;
        float detent = GateDial.Detent(d.Turn.Target, slots);
        d.Turn.Target = detent;
        SwitchModule m = _board.Modules[d.Index];
        d.PendingTop = _ring[GateDial.SlotUnderChevron(detent, GateDial.PlaceChevron, slots)];
        d.PendingSecond = _ring[GateDial.SlotUnderChevron(detent, GateDial.PeriodChevron, slots)];
        d.Pending = GateDial.Resolve(m.PlaceJack >= 0, Mathf.Abs(detent - d.GrabDetent) > 0.5f, d.PendingTop, d.PendingSecond);
        d.Settling = true;
        d.SettleAge = 0f;
        if (MotionPreference.Reduced)
            d.Turn.Snap(detent);
    }

    /// <summary>The ring reached its detent: the rule lands (a lock's click and its chevron lit in this frame; a refusal's red; a reset's clack).</summary>
    private void Land(Dial d)
    {
        d.Settling = false;
        int i = d.Index;
        SwitchModule m = _board.Modules[i];
        string id = _layout.Modules[i].Id;
        switch (d.Pending)
        {
            case DialAction.LockPlace:
                if (_board.CanPlugPlace(i, d.PendingTop.Index) && _board.PlugPlace(i, d.PendingTop.Index))
                {
                    Locked(d, GateDial.PlaceChevron);
                    Debug.Log($"[Switchboard] Dial {id}: PLACE locked on {Nation(d.PendingTop.Index)}.");
                }
                else
                    Refuse(d, GateDial.PlaceChevron);
                break;
            case DialAction.LockPeriod:
            {
                var jack = new SwitchRoute(m.PlaceJack, d.PendingSecond.Index);
                if (m.HasPeriod && m.PeriodJack == jack)
                    break;
                if (_board.CanPlugPeriod(i, jack) && _board.PlugPeriod(i, jack))
                {
                    Locked(d, GateDial.PeriodChevron);
                    Debug.Log($"[Switchboard] Dial {id}: PERIOD locked on {RouteName(jack)}; it needs {m.Need} bar(s), {m.Cells} in.");
                }
                else
                    Refuse(d, GateDial.PeriodChevron);
                break;
            }
            case DialAction.Reset:
                _board.UnplugPeriod(i);
                _board.UnplugPlace(i);
                Play(SwitchSound.PlugOut, 0.6f);
                Debug.Log($"[Switchboard] Dial {id}: reset (no era under chevron 2).");
                break;
            default:
                Play(SwitchSound.Ratchet, 0.2f);
                break;
        }
        d.Pending = DialAction.None;
    }

    private string Nation(int d) => d >= 0 && d < _nationNames.Length ? _nationNames[d] : "nation " + d;

    /// <summary>A lock landed on chevron <paramref name="chevron"/>: the click, and the chevron's flash starts (it lights this frame).</summary>
    private void Locked(Dial d, int chevron)
    {
        Play(SwitchSound.PlugIn, 0.8f);
        d.Flash[chevron] = 0f;
    }

    /// <summary>A lock the rules refuse (the route is another dial's): a dull pop and the chevron red for a moment; nothing changes.</summary>
    private void Refuse(Dial d, int chevron)
    {
        Play(SwitchSound.PlugOut, 0.5f);
        d.Refused = RefuseSeconds;
        d.RefusedChevron = chevron;
        Debug.Log($"[Switchboard] Dial {_layout.Modules[d.Index].Id}: lock refused on chevron {chevron + 1} (another dial holds that route).");
    }

    /// <summary>A cell is taken: out of the rack or out of its cradle (that dial darkens at once: the rules lose the cell now).</summary>
    private void GrabCell(CellPart c, BoardPoint p)
    {
        _carried = c;
        c.Held = true;
        c.FromModule = c.Module;
        c.FromSlot = c.Slot;
        c.Offset = new BoardPoint(c.Drawn.X - p.X, c.Drawn.Y - p.Y);
        c.HeldAt = c.Drawn;
        c.Hover = -1;
        if (c.Module >= 0)
        {
            _board.UnplugPower(c.Index);
            Debug.Log($"[Switchboard] Cell {c.Index + 1} out of dial {_layout.Modules[c.Module].Id}.");
        }
        c.Module = -1;
        c.Slot = -1;
        Play(c.FromModule >= 0 ? SwitchSound.PlugOut : SwitchSound.Ratchet, 0.5f);
    }

    /// <summary>The in-service dial whose cradle is under <paramref name="p"/> and takes <paramref name="c"/> now, or -1.</summary>
    private int CradleUnder(BoardPoint p, CellPart c)
    {
        if (_dials == null)
            return -1;
        foreach (Dial d in _dials)
            if (GateBoardLayout.Big(d.Index) && _gate.Cradle(d.Index).Contains(p) && _board.CanPlugPower(c.Index, d.Index))
                return d.Index;
        return -1;
    }

    /// <summary>The cell let go: into the cradle under it (the rules take it there; it eases into the lowest free slot and seats with a click), else home to the rack.</summary>
    private void DropCell()
    {
        CellPart c = _carried;
        _carried = null;
        c.Held = false;
        int to = c.Hover;
        c.Hover = -1;
        if (to >= 0 && _board.PlugPower(c.Index, to))
        {
            c.Module = to;
            c.Slot = FreeSlot(to, c);
            Debug.Log($"[Switchboard] Cell {c.Index + 1} into dial {_layout.Modules[to].Id}: {_board.Modules[to].Cells} bar(s) of {_board.Modules[to].Need}.");
            Fly(c, _config.cellSeatSeconds);
        }
        else
        {
            c.Module = -1;
            c.Slot = -1;
            Fly(c, HomeSeconds);
        }
    }

    /// <summary>The held cell goes back where it was taken from, the rules as they were (a right-click or Esc: CancelDrag).</summary>
    private void ReturnCell()
    {
        CellPart c = _carried;
        _carried = null;
        c.Held = false;
        c.Hover = -1;
        if (c.FromModule >= 0 && _board.PlugPower(c.Index, c.FromModule))
        {
            c.Module = c.FromModule;
            c.Slot = c.FromSlot >= 0 ? c.FromSlot : FreeSlot(c.FromModule, c);
        }
        Fly(c, HomeSeconds);
    }

    private void Fly(CellPart c, float seconds)
    {
        c.Flying = true;
        c.FlyT = 0f;
        c.FlySeconds = Mathf.Max(0.01f, seconds);
        c.FlyFrom = c.Drawn;
        c.FlyFromScale = c.DrawnScale;
    }

    /// <summary>The ring in the hand goes back to where it was picked up, nothing locked or unlocked (CancelDrag).</summary>
    private void ReturnRing()
    {
        Dial d = _turning;
        _turning = null;
        d.Turn.Target = d.GrabDetent;
        d.Settling = false;
        d.Pending = DialAction.None;
        if (MotionPreference.Reduced)
            d.Turn.Snap(d.GrabDetent);
    }

    /// <summary>True while the ring or a cell is in the hand.</summary>
    private bool DialHolding => _turning != null || _carried != null;

    /// <summary>Lets go of the ring or the cell with nothing changed; false when the hand held neither.</summary>
    private bool CancelDial()
    {
        if (_turning != null)
        {
            ReturnRing();
            return true;
        }
        if (_carried != null)
        {
            ReturnCell();
            return true;
        }
        return false;
    }

    /// <summary>The hand cursor's area on the dialer: over a cell or a ring the hand can take; null for none.</summary>
    private BoardRect? DialGrabArea(BoardPoint p)
    {
        if (_board == null || _gate == null)
            return null;
        CellPart c = CellUnder(p);
        if (c != null)
            return new BoardRect(c.Drawn.X - 26f, c.Drawn.Y - 42f, 52f, 84f);
        Dial d = RingUnder(p);
        if (d == null)
            return null;
        BoardPoint centre = _gate.DialCentre(d.Index);
        float r = GateBoardLayout.OuterRing;
        return new BoardRect(centre.X - r, centre.Y - r, 2f * r, 2f * r);
    }

    // ------------------------------------------------------------------ drawing

    /// <summary>The dialer's live parts this frame: the call row, the dials (rings, chevrons, horizons, cradles), the cells.</summary>
    private void DrawGate(float dt)
    {
        if (_dials == null || _board == null)
            return;
        DrawCalls();
        foreach (Dial d in _dials)
            if (d.Ring != null)
                DrawDial(d, dt);
        foreach (CellPart c in _cellParts)
            DrawCell(c, dt);
    }

    /// <summary>The call row: an emblem bright while its nation calls; each era pip amber while a traveller waits on that route.</summary>
    private void DrawCalls()
    {
        for (int d = 0; d < _callSlots.Length; d++)
        {
            CallSlot slot = _callSlots[d];
            bool calling = _board.CallLamp(d) == SwitchCallLamp.Calling;
            if (slot.Emblem.sprite != null)
                slot.Emblem.color = new Color(EmblemCream.r, EmblemCream.g, EmblemCream.b, calling || _board.WaitingAt(d) > 0 ? 1f : 0.55f);
            for (int e = 0; e < slot.Pips.Length; e++)
                slot.Pips[e].color = _board.Waiting(new SwitchRoute(d, e)) > 0 ? LampAmber : PipDark;
        }
    }

    /// <summary>
    /// One dial: its ring on its spring (it follows the hand, then eases into
    /// its detent, where the rule lands; it ticks past each glyph), its veil
    /// while closed, its chevrons (the locks amber; the strobe's engaged ones
    /// while calibrating, each with a flash; all green once open; dim while
    /// short of bars; the locks red while the fuse is out), its horizon, its
    /// cradle's bars and its era mark.
    /// </summary>
    private void DrawDial(Dial d, float dt)
    {
        SwitchModule m = _board.Modules[d.Index];
        int slots = _ring.Length;
        float omega = SettleOmegaSeconds / Mathf.Max(0.02f, _config.ringSettleSeconds);
        if (MotionPreference.Reduced && _turning != d)
            d.Turn.Snap(d.Turn.Target);
        else
            d.Turn.Step(dt, SpringTuning.Critical(omega * omega));
        d.Ring.localRotation = Quaternion.Euler(0f, 0f, -d.Turn.Value);
        int top = GateDial.SlotUnderChevron(d.Turn.Value, GateDial.PlaceChevron, slots);
        if (top != d.LastSlot)
        {
            if (_turning == d && Time.unscaledTime - d.LastTick > 0.04f)
            {
                Play(SwitchSound.Ratchet, 0.22f);
                d.LastTick = Time.unscaledTime;
            }
            d.LastSlot = top;
        }
        if (d.Settling)
        {
            d.SettleAge += dt;
            if (Mathf.Abs(d.Turn.Value - d.Turn.Target) < 0.25f && Mathf.Abs(d.Turn.Velocity) < 30f || d.SettleAge > 2f * _config.ringSettleSeconds)
            {
                d.Turn.Snap(d.Turn.Target);
                d.Ring.localRotation = Quaternion.Euler(0f, 0f, -d.Turn.Value);
                Land(d);
            }
        }

        bool closed = m.State == SwitchModuleState.Closed;

        // The strobe: one chevron engages every calibration / 7 while powered.
        float calibration = _board.Rules.CalibrationSeconds;
        int engaged = m.State == SwitchModuleState.Committed ? GateDial.Engaged(calibration - m.CalibrationLeft, calibration)
                    : m.State == SwitchModuleState.Open ? GateDial.Chevrons : 0;
        if (engaged > d.Engaged && m.State == SwitchModuleState.Committed)
        {
            for (int k = d.Engaged; k < engaged; k++)
                d.Flash[k] = 0f;
            Play(SwitchSound.PlugIn, 0.35f);
        }
        d.Engaged = engaged;
        for (int k = 0; k < GateDial.Chevrons; k++)
            d.Flash[k] += dt;
        d.Refused = Mathf.Max(0f, d.Refused - dt);
        d.FusePhase += dt * _config.fuseFlickerHz;

        bool placeLit = m.PlaceJack >= 0, periodLit = m.HasPeriod;
        bool rehearsing = _turning == d || d.Settling;
        for (int k = 0; k < GateDial.Chevrons; k++)
        {
            Color colour = LampAmber;
            float level = 0f;
            switch (m.State)
            {
                case SwitchModuleState.Blown:
                    if (k <= GateDial.PeriodChevron)
                    {
                        colour = LampRed;
                        level = Blink(d.FusePhase);
                    }
                    break;
                case SwitchModuleState.Committed:
                    level = k <= GateDial.PeriodChevron || k < engaged ? (m.Powered ? 1f : 0.4f) : 0f;
                    break;
                case SwitchModuleState.Open:
                    colour = LampGreen;
                    level = m.Powered ? 1f : 0.4f;
                    break;
                case SwitchModuleState.Closed:
                    break;
                default:
                    if (k == GateDial.PlaceChevron && placeLit)
                        level = 1f;
                    if (k == GateDial.PeriodChevron && periodLit)
                        level = rehearsing ? 0.5f : 1f;
                    break;
            }
            if (d.Refused > 0f && k == d.RefusedChevron)
            {
                colour = LampRed;
                level = Mathf.Clamp01(d.Refused / RefuseSeconds * 1.5f);
            }
            float flash = MotionPreference.Reduced || level <= 0f ? 0f : 1f - Mathf.Clamp01(d.Flash[k] / Mathf.Max(0.05f, _config.chevronFlashSeconds));
            flash *= flash;
            Color lit = Color.Lerp(colour, Color.white, 0.35f * flash);
            lit.a = level;
            d.Lenses[k].color = lit;
            d.LensGlows[k].color = new Color(colour.r, colour.g, colour.b, level * (0.55f + 0.45f * flash));
            d.LensGlows[k].rectTransform.localScale = Vector3.one * (1f + 0.35f * flash);
        }

        // The horizon: the hub fills as the route goes live (eased; at once under Reduced Motion), and breathes slowly.
        bool live = m.State == SwitchModuleState.Open;
        float to = live ? (m.Powered ? 1f : 0.35f) : 0f;
        d.Horizon01 = MotionPreference.Reduced ? to : Mathf.MoveTowards(d.Horizon01, to, dt / (live ? 0.4f : 0.25f));
        float ease = d.Horizon01 * d.Horizon01 * (3f - 2f * d.Horizon01);
        d.Breath += dt * 0.35f;
        float breathe = MotionPreference.Reduced ? 1f : 1f + 0.012f * Mathf.Sin(d.Breath * 2f * Mathf.PI);
        Color h = _config.horizonColour;
        d.Horizon.color = new Color(h.r, h.g, h.b, 0.92f * ease);
        d.Horizon.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, ease) * breathe;

        // The cradle: a bar lights under each seated cell (amber; green once the bars reach the mark), the mark over the bar the era needs.
        bool atMark = m.Cells > 0 && m.Cells >= m.Need;
        for (int s = 0; s < d.Bars.Length; s++)
        {
            bool lit = false;
            foreach (CellPart c in _cellParts)
                if (c.Module == d.Index && c.Slot == s && !c.Flying)
                    lit = true;
            Color bar = atMark ? LampGreen : LampAmber;
            d.Bars[s].color = new Color(bar.r, bar.g, bar.b, lit ? 0.55f : 0f);
        }
        bool showMark = periodLit && !closed;
        if (d.Mark.gameObject.activeSelf != showMark)
            d.Mark.gameObject.SetActive(showMark);
        if (showMark)
        {
            d.Mark.rectTransform.anchoredPosition = Local(_gate.Mark(d.Index, m.Need));
            d.Mark.color = atMark ? Color.Lerp(Color.white, LampGreen, 0.6f) : Color.white;
        }
    }

    /// <summary>One cell: in the hand (lifted, toward the eye), flying home or into its slot (eased; a click and a 2 px settle as it seats), or at rest; its glow on while it is in a cradle or the hand.</summary>
    private void DrawCell(CellPart c, float dt)
    {
        BoardPoint at;
        float scale;
        if (c.Held)
        {
            at = c.HeldAt;
            if (c.Hover >= 0)
            {
                int slot = FreeSlot(c.Hover, c);
                if (slot >= 0)
                    at = at.Lerp(_gate.CradleSlot(c.Hover, slot).Centre, 0.35f);
            }
            scale = _config.heldScale;
        }
        else if (c.Flying)
        {
            c.FlyT = MotionPreference.Reduced ? 1f : c.FlyT + dt / c.FlySeconds;
            float e = Mathf.Clamp01(c.FlyT);
            e = e * e * (3f - 2f * e);
            at = c.FlyFrom.Lerp(Rest(c), e);
            scale = Mathf.Lerp(c.FlyFromScale, 1f, e);
            if (c.FlyT >= 1f)
            {
                c.Flying = false;
                at = Rest(c);
                Play(SwitchSound.PlugIn, c.Module >= 0 ? 0.65f : 0.35f);
                if (c.Module >= 0 && !MotionPreference.Reduced)
                    c.Settle.Kick(SpringTuning.WithRatio(700f, 0.45f).KickFor(_config.cellSettlePixels));
            }
        }
        else
        {
            at = Rest(c);
            scale = 1f;
        }
        if (MotionPreference.Reduced)
            c.Settle.Snap(0f);
        else
            c.Settle.Step(dt, SpringTuning.WithRatio(700f, 0.45f));
        c.Drawn = at;
        c.DrawnScale = scale;
        BoardPoint drawn = at.Plus(0f, Mathf.Clamp(c.Settle.Value, -_config.cellSettlePixels, _config.cellSettlePixels));
        Transform parent = c.Held || c.Flying ? _heldLayer : _cellLayer;
        if (c.Body.rectTransform.parent != parent)
        {
            c.Glow.rectTransform.SetParent(parent, false);
            c.Body.rectTransform.SetParent(parent, false);
        }
        PlaceImage(c.Body, drawn, scale);
        PlaceImage(c.Glow, drawn, scale);
        bool glowing = c.Held || c.Module >= 0;
        c.Glow.color = new Color(LampAmber.r, LampAmber.g, LampAmber.b, glowing ? 0.45f : 0.12f);
    }

    // ------------------------------------------------------------------ for the rig and the hall

    /// <summary>The dialer's layout (null on the Classic board or before the day).</summary>
    internal GateBoardLayout GateLayout => _gate;

    /// <summary>The dial's ring glyphs (null before the day).</summary>
    internal GateGlyph[] GateRing => _ring;

    /// <summary>Today's places as dial routes (nation slot, era), in the board's priority.</summary>
    internal IReadOnlyList<SwitchRoute> TodaysRoutes => _todayRoutes;

    /// <summary>Dial <paramref name="m"/>'s ring rotation (degrees clockwise) and whether it rests in its detent with nothing pending.</summary>
    internal float DialTurn(int m, out bool resting)
    {
        Dial d = _dials != null && m >= 0 && m < _dials.Length ? _dials[m] : null;
        resting = d != null && !d.Settling && _turning != d && Mathf.Abs(d.Turn.Value - d.Turn.Target) < 0.05f && Mathf.Abs(d.Turn.Velocity) < 1f;
        return d != null ? d.Turn.Value : 0f;
    }

    /// <summary>True when the board is not shown at all (turned away and the turn finished).</summary>
    internal bool Hidden => !_open && _openT <= 0f;

    /// <summary>Dial <paramref name="m"/>'s chevron lenses this frame: the alpha of each (0 dark to 1 lit).</summary>
    internal float[] DialLenses(int m)
    {
        var a = new float[GateDial.Chevrons];
        Dial d = _dials != null && m >= 0 && m < _dials.Length ? _dials[m] : null;
        for (int k = 0; d != null && d.Lenses != null && k < a.Length; k++)
            a[k] = d.Lenses[k].color.a;
        return a;
    }

    /// <summary>Cell <paramref name="c"/>'s drawn place on the plate and the dial it sits in (-1: the rack or the hand).</summary>
    internal BoardPoint CellPoint(int c, out int module)
    {
        CellPart part = _cellParts != null && c >= 0 && c < _cellParts.Length ? _cellParts[c] : null;
        module = part != null ? part.Module : -1;
        return part != null ? part.Drawn : default;
    }

    /// <summary>How many cells the rack holds now (none in a cradle or the hand).</summary>
    internal int CellsInRack()
    {
        int n = 0;
        if (_cellParts != null)
            foreach (CellPart c in _cellParts)
                if (c.Module < 0 && !c.Held)
                    n++;
        return n;
    }

    /// <summary>A plate point on the screen (pixels, origin bottom left).</summary>
    internal Vector2 ScreenOf(BoardPoint p) => RectTransformUtility.WorldToScreenPoint(null, _plate.TransformPoint(Local(p)));

    /// <summary>MASTER's handle centre on the plate (at rest) and its fully thrown place.</summary>
    internal BoardPoint MasterFrom => _layout.Master.From;

    /// <summary>MASTER's handle centre when thrown home.</summary>
    internal BoardPoint MasterTo => _layout.Master.To;

    /// <summary>True when the view is fully turned to the board.</summary>
    internal bool Turned => _open && _openT >= 1f;
}
