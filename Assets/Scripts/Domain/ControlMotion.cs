using System;

/// <summary>How far a control's face reaches past its rest rect on each side (px; positive out, negative in) and how dark it is now (0 to 1).</summary>
public readonly struct FaceEdges
{
    /// <summary>The left, right, bottom and top edges' reach past the rest rect.</summary>
    public readonly float Left, Right, Bottom, Top;

    /// <summary>How much the face darkens (a press).</summary>
    public readonly float Darken;

    /// <summary>A face from its parts.</summary>
    public FaceEdges(float left, float right, float bottom, float top, float darken)
    {
        Left = left;
        Right = right;
        Bottom = bottom;
        Top = top;
        Darken = darken;
    }

    /// <summary>At rest: the rest rect, undarkened.</summary>
    public static FaceEdges Rest => default;
}

/// <summary>
/// A control's physical feel (Saleh 2026-10-07, the "live component" reel;
/// round 2: "they go out of bounds of their borders, buttons overlap", "too
/// fast"): the control's frame never moves or grows; its FACE moves inside
/// it. Under the pointer the face lifts a pixel or two (MotionKnobs.hoverLift;
/// the kit's hover face adds the glint); pressed, its top comes down into its
/// bezel (pressDepth) while its bottom stays, it widens by the area's rule
/// and darkens (pressDarken; the Balanced feel, its peak at about 90 ms); let
/// go it springs back past its rest and wobbles in (Elastic, settling over
/// about 400 ms in two or three decaying wobbles); a confirmed click pops its
/// top up (popLift) and wobbles; a click on a disabled control shakes it
/// sideways, a short "no". A screen-edge pull tab (SetPull) also slides out
/// under the pointer and on a click, stretched along its travel by its speed
/// and squashed by its acceleration (SquashStretch.FromMotion). Every edge's
/// reach outward is capped by the control's room (SetRoom: the kit's border
/// inset, never into a neighbour's rest rect: ControlRoom), so no animated
/// face overlaps a neighbour. Reduced Motion and the Motion intensity scale
/// every amplitude (reduced, the face stays put; the darkening still shows
/// the press). The view (UiJuice) reads Edges after each Step.
/// </summary>
public sealed class ControlMotion
{
    private Spring _sink, _shake, _pull, _darken;
    private MotionFeel _sinkFeel = MotionFeel.Balanced, _pullFeel = MotionFeel.Balanced;
    private bool _hovered, _pressed;
    private float _roomLeft, _roomRight, _roomBottom, _roomTop;
    private int _pullX, _pullY;

    /// <summary>True while any part still moves (the view keeps stepping it).</summary>
    public bool Moving => !(_sink.AtRest && _shake.AtRest && _pull.AtRest && _darken.AtRest);

    /// <summary>True while it is held down.</summary>
    public bool Pressed => _pressed;

    /// <summary>True when it rests exactly at its rest pose (no reach, no darkening).</summary>
    public bool AtRestPose => !Moving && _sink.Value == 0f && _shake.Value == 0f && _pull.Value == 0f && _darken.Value == 0f;

    /// <summary>How far each edge of the face may reach past the rest rect (px, 0 or more): ControlRoom's answer for the control's place.</summary>
    public void SetRoom(float left, float right, float bottom, float top)
    {
        _roomLeft = left > 0f ? left : 0f;
        _roomRight = right > 0f ? right : 0f;
        _roomBottom = bottom > 0f ? bottom : 0f;
        _roomTop = top > 0f ? top : 0f;
    }

    /// <summary>Makes it a pull tab sliding out along (<paramref name="x"/>, <paramref name="y"/>) (each -1, 0 or 1; 0, 0: none).</summary>
    public void SetPull(int x, int y)
    {
        _pullX = Math.Sign(x);
        _pullY = Math.Sign(y);
    }

    /// <summary>True for a pull tab.</summary>
    public bool Pulls => _pullX != 0 || _pullY != 0;

    /// <summary>A pull tab's direction (-1, 0 or 1 on each axis).</summary>
    public int PullX => _pullX;

    /// <summary>A pull tab's direction on y.</summary>
    public int PullY => _pullY;

    /// <summary>The pointer came over the control (<paramref name="over"/>) or left it: the face lifts (a pull tab slides out) or comes back (not while pressed: the release decides).</summary>
    public void Hover(bool over, MotionKnobs knobs, MotionAmount amount)
    {
        _hovered = over;
        if (_pressed)
            return;
        Aim(ref _sink, over ? -knobs.hoverLift : 0f, amount);
        _sinkFeel = knobs.hoverFeel;
        if (Pulls)
        {
            Aim(ref _pull, over ? knobs.pullHover : 0f, amount);
            _pullFeel = knobs.slideFeel;
        }
    }

    /// <summary>Pressed down: the face goes into its bezel and darkens (a pull tab is pushed back in).</summary>
    public void Press(MotionKnobs knobs, MotionAmount amount)
    {
        _pressed = true;
        Aim(ref _sink, knobs.pressDepth, amount);
        _sinkFeel = knobs.pressFeel;
        AimDarken(knobs.pressDarken, amount);
        if (Pulls)
        {
            Aim(ref _pull, 0f, amount);
            _pullFeel = knobs.pressFeel;
        }
    }

    /// <summary>Let go: the face springs back past its rest (or its hover lift) and wobbles in.</summary>
    public void Release(MotionKnobs knobs, MotionAmount amount)
    {
        if (!_pressed)
            return;
        _pressed = false;
        Aim(ref _sink, _hovered ? -knobs.hoverLift : 0f, amount);
        _sinkFeel = knobs.releaseFeel;
        AimDarken(0f, amount);
        if (Pulls)
        {
            Aim(ref _pull, _hovered ? knobs.pullHover : 0f, amount);
            _pullFeel = knobs.releaseFeel;
        }
    }

    /// <summary>A confirmed click: the face's top pops up and wobbles back (a pull tab pops further out).</summary>
    public void Confirm(MotionKnobs knobs, MotionAmount amount)
    {
        if (amount.Still)
            return;
        _sink.Kick(-knobs.Get(knobs.popFeel).KickFor(knobs.popLift * amount.Share));
        _sinkFeel = knobs.popFeel;
        if (Pulls)
        {
            _pull.Kick(knobs.Get(knobs.popFeel).KickFor(knobs.pullPop * amount.Share));
            _pullFeel = knobs.popFeel;
        }
    }

    /// <summary>A click on a disabled control: the sideways "no" shake, no pop.</summary>
    public void Refuse(MotionKnobs knobs, MotionAmount amount)
    {
        if (amount.Still)
            return;
        _shake.Kick(knobs.Get(knobs.refuseFeel).KickFor(knobs.refuseShake * amount.Share));
    }

    /// <summary>Back at rest at once (the control was hidden or disabled mid-motion).</summary>
    public void Reset()
    {
        _hovered = _pressed = false;
        _sink.Snap(0f);
        _shake.Snap(0f);
        _pull.Snap(0f);
        _darken.Snap(0f);
    }

    /// <summary>Advances every part by <paramref name="dt"/> seconds; true while any still moves.</summary>
    public bool Step(float dt, MotionKnobs knobs)
    {
        float v = knobs.settleValue * 100f, s = knobs.settleSpeed * 100f; // px
        _sink.Step(dt, knobs.Get(_sinkFeel), v, s);
        _shake.Step(dt, knobs.Get(knobs.refuseFeel), v, s);
        _pull.Step(dt, knobs.Get(_pullFeel), v, s);
        _darken.Step(dt, knobs.Get(knobs.pressFeel), knobs.settleValue, knobs.settleSpeed);
        return Moving;
    }

    /// <summary>
    /// The face of a <paramref name="width"/> x <paramref name="height"/>
    /// control now: its top down by the sink (its bottom fixed), wider or
    /// narrower by the area's rule; a pull tab's slide, stretched along its
    /// travel; the shake; then each edge's outward reach capped by its room.
    /// </summary>
    public FaceEdges Edges(float width, float height, MotionKnobs knobs)
    {
        if (width <= 0f || height <= 0f)
            return FaceEdges.Rest;
        float h = Math.Max(1f, height - _sink.Value); // the top moves, the bottom stays
        float w = width * height / h;
        float midY = h / 2f;
        if (Pulls)
        {
            Stretch st = SquashStretch.FromMotion(_pull.Velocity, _pull.Acceleration(knobs.Get(_pullFeel)), knobs.pullStretchPerSpeed, knobs.pullSquashPerAccel, knobs.maxStretch);
            if (_pullX != 0)
            {
                w *= st.Along;
                h *= st.Across;
            }
            else
            {
                w *= st.Across;
                h *= st.Along;
            }
        }
        float dx = _shake.Value + _pull.Value * _pullX, dy = _pull.Value * _pullY;
        float left = (w - width) / 2f - dx, right = (w - width) / 2f + dx;
        float bottom = -(midY - h / 2f) - dy, top = midY + h / 2f - height + dy;
        return new FaceEdges(Math.Min(left, _roomLeft), Math.Min(right, _roomRight), Math.Min(bottom, _roomBottom), Math.Min(top, _roomTop),
                             _darken.Value < 0f ? 0f : _darken.Value);
    }

    /// <summary>Pulls <paramref name="spring"/> toward <paramref name="target"/> scaled by the amount (a cut when nothing moves).</summary>
    private static void Aim(ref Spring spring, float target, MotionAmount amount)
    {
        float t = target * amount.Share;
        if (amount.Still)
            spring.Snap(t);
        else
            spring.Target = t;
    }

    /// <summary>The darkening toward <paramref name="target"/> (a state, not a motion: it shows under Reduced Motion too, at once).</summary>
    private void AimDarken(float target, MotionAmount amount)
    {
        if (amount.Still)
            _darken.Snap(target);
        else
            _darken.Target = target;
    }
}

/// <summary>
/// A popup's or a panel's coming and going (a tooltip, a toast, a ribbon, a
/// window, the dialogue wheel's pills, the newspaper) as one spring, its
/// presence: 0 gone, 1 shown (an under-damped feel swings past 1 and
/// settles). The view scales it up from MotionKnobs.appearFromScale, slides
/// it in from slideDistance and fades it with the presence. Under Reduced
/// Motion it only fades, linearly over reducedFadeSeconds (no scale, no
/// slide, no overshoot); at Motion intensity 0 it cuts.
/// </summary>
public sealed class AppearMotion
{
    private Spring _presence;
    private bool _open, _fading;
    private float _delay, _alpha;

    /// <summary>The presence now (0 gone, 1 shown; can swing past 1).</summary>
    public float Presence => _presence.Value;

    /// <summary>True when it was asked to show.</summary>
    public bool Open => _open;

    /// <summary>True when it is closed and all the way gone (the view may switch it off).</summary>
    public bool Gone => !_open && _presence.AtRest && _alpha <= 0f;

    /// <summary>True while it still moves (or waits out its stagger).</summary>
    public bool Moving => !_presence.AtRest || _delay > 0f;

    /// <summary>Its opacity now, 0..1.</summary>
    public float Alpha => _alpha;

    /// <summary>Shows it (<paramref name="open"/>) or takes it away, after <paramref name="delay"/> seconds (a stagger), with <paramref name="amount"/> of motion.</summary>
    public void Show(bool open, MotionAmount amount, float delay = 0f)
    {
        _open = open;
        _fading = amount.Reduced;
        _delay = amount.Still || delay <= 0f ? 0f : delay; // no stagger without motion (Reduced Motion fades at once)
        float target = open ? 1f : 0f;
        if (amount.Still && !amount.Reduced)
        {
            _presence.Snap(target);
            _alpha = target;
            return;
        }
        _presence.Target = target;
    }

    /// <summary>At once shown or gone (the view's first frame, a reset).</summary>
    public void Snap(bool open)
    {
        _open = open;
        _delay = 0f;
        _presence.Snap(open ? 1f : 0f);
        _alpha = open ? 1f : 0f;
    }

    /// <summary>The scale to draw at: from <paramref name="from"/> (scaled toward 1 by the amount) up to 1, past it while the spring overshoots; 1 under Reduced Motion.</summary>
    public float Scale(float from, MotionAmount amount) => _fading ? 1f : 1f - (1f - from) * amount.Share * (1f - _presence.Value);

    /// <summary>The offset to draw at: <paramref name="distance"/> away (scaled by the amount) when gone, none when shown; 0 under Reduced Motion.</summary>
    public float Offset(float distance, MotionAmount amount) => _fading ? 0f : distance * amount.Share * (1f - _presence.Value);

    /// <summary>How fast the presence changes (per second; 0 under Reduced Motion): an offset of distance d travels at d · share · this.</summary>
    public float Speed => _fading ? 0f : _presence.Velocity;

    /// <summary>The presence's acceleration under <paramref name="tuning"/> (per second²; 0 under Reduced Motion): a travelling piece squashes by it.</summary>
    public float Acceleration(SpringTuning tuning) => _fading ? 0f : _presence.Acceleration(tuning);

    /// <summary>Advances it by <paramref name="dt"/> seconds with <paramref name="tuning"/> (a fade over <paramref name="fadeSeconds"/> under Reduced Motion); true while it still moves.</summary>
    public bool Step(float dt, SpringTuning tuning, float fadeSeconds, float settleValue, float settleSpeed)
    {
        if (_delay > 0f)
        {
            _delay -= dt;
            if (_delay > 0f)
                return true;
            dt = -_delay;
            _delay = 0f;
        }
        if (_fading)
        {
            float rate = fadeSeconds > 0f ? dt / fadeSeconds : 1f;
            float v = _presence.Value + (_presence.Target > _presence.Value ? rate : -rate);
            if ((_presence.Target - _presence.Value) * (_presence.Target - v) <= 0f)
                _presence.Snap(_presence.Target);
            else
                _presence.Value = v;
            _presence.Velocity = 0f;
        }
        else
            _presence.Step(dt, tuning, settleValue, settleSpeed);
        float p = _presence.Value;
        _alpha = _fading ? Clamp01(p) : Clamp01(p * 2f);
        return Moving;
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}
