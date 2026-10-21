using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The hand on the switchboard (SwitchboardView), Papers, Please's controls
/// (Saleh 2026-10-06): a left-press on a plug picks it up and the cord comes
/// with it; let go over a jack it may seat in (SwitchCords.Snap, the jacks'
/// hit radii widened by the magnet) and it snaps in with a click; let go
/// anywhere else and it flies home. A left-press on MASTER's handle grabs
/// it: it follows the hand down its pull with weight (a spring), ticks past
/// its ratchet notches, and let go past 80 % it throws home with a clunk and
/// locks every patched route in (Switchboard.PullMaster); let go sooner it
/// rises and nothing changes. A right-click or Esc while holding lets go
/// with nothing changed (OfficeControls, CancelDrag). The rules change only
/// when a plug lands, never while it is in the hand.
/// </summary>
public sealed partial class SwitchboardView
{
    private static readonly float[] Notches = { 0.25f, 0.5f, 0.75f };

    private Cord _held;
    private bool _leverHeld;
    private bool _leverThrow;
    private float _leverRise = -1f;
    private float _leverGrab;
    private Spring _lever = Spring.At(0f);
    private System.Func<int, bool> _heldAllows;

    /// <summary>The hand this frame (the board fully turned to): holding MASTER, holding a plug, or a fresh press picking one of them up; the hand cursor over whatever can be picked up.</summary>
    private void Hand()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;
        BoardPoint p = PlatePoint(mouse.position.ReadValue());
        if (_leverHeld)
        {
            _lever.Target = Mathf.Clamp01(_layout.Master.PullOf(p) + _leverGrab);
            if (!mouse.leftButton.isPressed)
                ReleaseLever();
        }
        else if (_held != null)
        {
            MoveHeld(p);
            if (!mouse.leftButton.isPressed)
                DropHeld();
        }
        else if (mouse.leftButton.wasPressedThisFrame)
        {
            if (OnHandle(p))
            {
                _leverHeld = true;
                _leverThrow = false;
                _leverRise = -1f;
                _leverGrab = _lever.Value - _layout.Master.PullOf(p);
            }
            else
            {
                Cord c = Grabbable(p);
                if (c != null)
                    Grab(c, p);
            }
        }
        PlaceGrab(p);
    }

    // ------------------------------------------------------------------ plugs

    /// <summary>The plug under <paramref name="p"/> that can be picked up (a shown cord's plug, at home or seated, not flying), the nearest within its socket's hit radius; null for none.</summary>
    private Cord Grabbable(BoardPoint p)
    {
        if (_board == null)
            return null;
        Cord best = null;
        float bestDistance = float.MaxValue;
        foreach (Cord c in _cords)
        {
            if (!c.Shown || c.Returning || c.Held)
                continue;
            float d = c.Drawn.DistanceTo(p);
            if (d <= Radius(c) && d < bestDistance)
            {
                best = c;
                bestDistance = d;
            }
        }
        return best;
    }

    /// <summary>Picks <paramref name="c"/>'s plug up where the hand took it (no jump): the cord pulls a little taut; the rules still have it where it was until it lands.</summary>
    private void Grab(Cord c, BoardPoint p)
    {
        _held = c;
        c.Held = true;
        c.HeldFrom = c.Seat;
        c.Offset = new BoardPoint(c.Drawn.X - p.X, c.Drawn.Y - p.Y);
        c.HeldAt = c.Drawn;
        c.Snap = -1;
        c.Sag.Target = 0.8f;
        Play(c.Seat >= 0 ? SwitchSound.PlugOut : SwitchSound.Ratchet, 0.6f);
    }

    /// <summary>The held plug follows the hand; the jack it would seat in now pulls it a little closer.</summary>
    private void MoveHeld(BoardPoint p)
    {
        Cord c = _held;
        c.HeldAt = p.Plus(c.Offset.X, c.Offset.Y);
        _heldAllows ??= i => _held != null && Allowed(_held, i);
        c.Snap = SwitchCords.Snap(Targets(c), c.HeldAt, _heldAllows, _config.snapMagnet);
    }

    /// <summary>Lets the held plug go: into the jack it would seat in (the rules plug it there, unlocking the old route), else home (the rules stow it).</summary>
    private void DropHeld()
    {
        Cord c = _held;
        _held = null;
        c.Held = false;
        c.Sag.Target = 1f;
        int to = c.Snap;
        c.Snap = -1;
        c.ReturnFrom = c.Drawn;
        c.ReturnFromScale = c.DrawnScale;
        c.Returning = true;
        c.ReturnT = 0f;
        if (to >= 0)
        {
            Apply(c, to);
            c.Seat = to;
            c.ReturnSeconds = SnapSeconds; // the last of the way in: a snap
        }
        else
        {
            if (c.Seat >= 0)
                Apply(c, -1);
            c.Seat = -1;
            c.ReturnSeconds = HomeSeconds;
        }
    }

    /// <summary>The held plug goes back where it was picked up, the rules untouched (a right-click or Esc: CancelDrag).</summary>
    private void ReturnHeld()
    {
        Cord c = _held;
        _held = null;
        c.Held = false;
        c.Snap = -1;
        c.Sag.Target = 1f;
        c.Seat = c.HeldFrom;
        c.ReturnFrom = c.Drawn;
        c.ReturnFromScale = c.DrawnScale;
        c.Returning = true;
        c.ReturnT = 0f;
        c.ReturnSeconds = HomeSeconds;
    }

    /// <summary>A plug landed (in a jack, or home): its click, a bounce and a swing of its cord.</summary>
    private void Seated(Cord c, float volume)
    {
        Play(SwitchSound.PlugIn, volume);
        var amount = new MotionAmount(MotionPreference.Intensity, MotionPreference.Reduced);
        c.Bounce.Kick(SpringTuning.WithRatio(520f, 0.35f).KickFor(1f), amount);
        c.Sag.Kick(1.8f, amount);
    }

    /// <summary>The rules follow a plug that landed: <paramref name="c"/>'s plug into target <paramref name="to"/> (-1: home).</summary>
    private void Apply(Cord c, int to)
    {
        string module = c.Kind == CordKind.Power ? "power cord " + (c.Index + 1) : "module " + _layout.Modules[c.Index].Id;
        switch (c.Kind)
        {
            case CordKind.Place:
                if (to < 0)
                    _board.UnplugPlace(c.Index);
                else
                    _board.PlugPlace(c.Index, to);
                break;
            case CordKind.Period:
                if (to < 0)
                    _board.UnplugPeriod(c.Index);
                else
                    _board.PlugPeriod(c.Index, new SwitchRoute(to / _layout.PeriodRows, to % _layout.PeriodRows));
                break;
            case CordKind.Power:
                if (to < 0)
                    _board.UnplugPower(c.Index);
                else
                    _board.PlugPower(c.Index, to);
                break;
        }
        Debug.Log($"[Switchboard] {module} {c.Kind.ToString().ToUpperInvariant()} {(to < 0 ? "stowed" : "into " + TargetName(c, to))}.");
    }

    /// <summary>The jack <paramref name="i"/> of <paramref name="c"/>'s targets as the log names it.</summary>
    private string TargetName(Cord c, int i)
    {
        switch (c.Kind)
        {
            case CordKind.Place:
                return i < _directory.Destinations.Count ? _directory.Destinations[i].Label : "slot " + i;
            case CordKind.Period:
                int d = i / _layout.PeriodRows, r = i % _layout.PeriodRows;
                return d < _directory.Destinations.Count && r < _directory.Destinations[d].Periods.Count
                    ? _directory.Destinations[d].Label + " " + _directory.Destinations[d].Periods[r].Label
                    : "jack " + d + "/" + r;
            default:
                return "module " + _layout.Modules[i].Id;
        }
    }

    /// <summary>The jacks <paramref name="c"/>'s plug can go into: the destinations' place jacks, every period jack (by destination, then row) or the modules' POWER sockets.</summary>
    private System.Collections.Generic.IReadOnlyList<BoardSocket> Targets(Cord c) =>
        c.Kind == CordKind.Place ? _placeJacks : c.Kind == CordKind.Period ? (System.Collections.Generic.IReadOnlyList<BoardSocket>)_periodJacks : _modulePower;

    /// <summary>True when <paramref name="c"/>'s plug may seat in its target <paramref name="i"/> now: a printed destination's jack, or a module's POWER socket no placard covers, that the rules let it into (free, or its own).</summary>
    private bool Allowed(Cord c, int i)
    {
        if (_board == null || _directory == null || Targets(c)[i] == null || Targets(c)[i].Covered)
            return false;
        switch (c.Kind)
        {
            case CordKind.Place:
                return i < _directory.Destinations.Count && _board.CanPlugPlace(c.Index, i);
            case CordKind.Period:
                int d = i / _layout.PeriodRows, r = i % _layout.PeriodRows;
                return _periodJacks[i] != null && d < _directory.Destinations.Count && r < _directory.Destinations[d].Periods.Count
                       && _board.CanPlugPeriod(c.Index, new SwitchRoute(d, r));
            default:
                return _board.CanPlugPower(c.Index, i);
        }
    }

    /// <summary>A cord's home: its module's PLACE or PERIOD socket, or its place on the power row.</summary>
    private BoardPoint Home(Cord c) =>
        c.Kind == CordKind.Place ? _layout.Modules[c.Index].Place.Centre
        : c.Kind == CordKind.Period ? _layout.Modules[c.Index].Period.Centre
        : _layout.PowerRow[c.Index].Centre;

    /// <summary>A plug's size at home (the art: 0.85 in a module's socket, full size on the power row).</summary>
    private float HomeScale(Cord c) => c.Kind == CordKind.Power ? 1f : _config.moduleScale;

    /// <summary>Where <paramref name="c"/>'s seated plug is.</summary>
    private BoardPoint SeatPoint(Cord c) => Targets(c)[c.Seat].Centre;

    /// <summary>A seated plug's size (full size on a destination's jacks, 0.85 in a module's POWER socket).</summary>
    private float SeatScale(Cord c) => c.Kind == CordKind.Power ? _config.moduleScale : 1f;

    /// <summary>How near the hand must press to pick <paramref name="c"/>'s plug up: the hit radius of the socket it sits in.</summary>
    private float Radius(Cord c)
    {
        if (c.Seat >= 0)
            return Targets(c)[c.Seat].HitRadius;
        return c.Kind == CordKind.Place ? _layout.Modules[c.Index].Place.HitRadius
             : c.Kind == CordKind.Period ? _layout.Modules[c.Index].Period.HitRadius
             : _layout.PowerRow[c.Index].HitRadius;
    }

    // ------------------------------------------------------------------ MASTER

    /// <summary>MASTER's handle area where the handle is now (its rest box moved down its pull, grown by 12 px).</summary>
    private BoardRect HandleRect()
    {
        LayoutMaster master = _layout.Master;
        BoardPoint now = master.HandleAt(_lever.Value);
        BoardRect rest = master.HandleRest;
        return new BoardRect(rest.X + now.X - master.From.X, rest.Y + now.Y - master.From.Y, rest.W, rest.H).Grown(12f);
    }

    private bool OnHandle(BoardPoint p) => HandleRect().Contains(p);

    /// <summary>MASTER let go: past masterCommitAt it throws home (the clunk comes as it lands), else it rises and nothing changes.</summary>
    private void ReleaseLever()
    {
        _leverHeld = false;
        if (Mathf.Max(_lever.Value, _lever.Target) >= _config.masterCommitAt)
        {
            _leverThrow = true;
            _lever.Target = 1f;
        }
        else
        {
            _lever.Target = 0f;
        }
    }

    /// <summary>MASTER's spring this frame (cut under Reduced Motion): the notches' ticks on the way down, the clunk as a throw lands, and the rise a moment after.</summary>
    private void StepLever(float dt)
    {
        if (_leverRise > 0f && Time.unscaledTime >= _leverRise)
        {
            _leverRise = -1f;
            _lever.Target = 0f;
        }
        float before = _lever.Value;
        if (MotionPreference.Reduced)
            _lever.Snap(_lever.Target);
        else
            _lever.Step(dt, SpringTuning.WithRatio(_config.leverStiffness, _config.leverDamping, _config.leverMass));
        foreach (float notch in Notches)
            if (before < notch && _lever.Value >= notch)
                Play(SwitchSound.Ratchet, 0.5f);
        if (_leverThrow && _lever.Value >= 0.97f)
            Clunk();
    }

    /// <summary>MASTER lands home: the clunk, a shake, and every patched route locked in or fused (the rules); it rises again a moment later.</summary>
    private void Clunk()
    {
        _leverThrow = false;
        _leverRise = Time.unscaledTime + 0.14f;
        Play(SwitchSound.Clunk, 1f);
        Shake(_config.clunkShake);
        if (_board == null)
            return;
        MasterPull pull = _board.PullMaster();
        Debug.Log(pull.Idle ? "[Switchboard] MASTER pulled: no patched module waited for it."
                            : $"[Switchboard] MASTER pulled: {pull.Committed} route(s) locked in, {pull.Fused} fuse(s) popped.");
    }

    // ------------------------------------------------------------------ the hand cursor

    /// <summary>Puts the grab area (a Clickable: the game's hand cursor) over what the hand can pick up, or under the hand while it holds something; hidden otherwise.</summary>
    private void PlaceGrab(BoardPoint p)
    {
        bool show = false;
        Vector2 at = Vector2.zero, size = Vector2.zero;
        if (_held != null || _leverHeld)
        {
            show = true;
            at = Local(p);
            size = new Vector2(160f, 160f);
        }
        else if (OnHandle(p))
        {
            BoardRect r = HandleRect();
            show = true;
            at = Local(r.Centre);
            size = Size(r);
        }
        else
        {
            Cord c = Grabbable(p);
            if (c != null)
            {
                show = true;
                at = Local(c.Drawn);
                size = Vector2.one * 2f * Radius(c);
            }
        }
        if (_grab.gameObject.activeSelf != show)
            _grab.gameObject.SetActive(show);
        if (!show)
            return;
        _grab.anchoredPosition = at;
        _grab.sizeDelta = size;
    }

    /// <summary>A screen point on the plate, in its pixels (top-left origin, y down).</summary>
    private BoardPoint PlatePoint(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_plate, screen, null, out Vector2 local);
        return new BoardPoint(local.x + _layout.Width / 2f, _layout.Height / 2f - local.y);
    }
}
