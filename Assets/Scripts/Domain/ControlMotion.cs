/// <summary>
/// A UI control's physical feel (Saleh 2026-10-07, the "live component"
/// reel: a press squashes before anything moves, the release springs back
/// past its rest and wobbles in, every touch reads as physical), as springs:
/// under the pointer it lifts (MotionKnobs.hoverScale); pressed it squashes
/// wider and shorter (pressScaleX/Y); let go it springs back with an
/// overshoot (releaseFeel); a confirmed click pops it (1 to 1 + popAmount and
/// back); a click on a disabled control shakes it sideways, a short "no",
/// and never pops. Reduced Motion and the Motion intensity scale every
/// amplitude (MotionAmount): reduced, the control never moves (its kit face
/// still shows the state). The view (UiJuice) reads ScaleX, ScaleY and
/// OffsetX after each Step.
/// </summary>
public sealed class ControlMotion
{
    private Spring _x = Spring.At(1f), _y = Spring.At(1f), _pop, _shake;
    private MotionFeel _baseFeel = MotionFeel.Balanced;
    private bool _hovered, _pressed;

    /// <summary>The horizontal scale now (the base pose and the pop).</summary>
    public float ScaleX => _x.Value + _pop.Value;

    /// <summary>The vertical scale now.</summary>
    public float ScaleY => _y.Value + _pop.Value;

    /// <summary>The sideways offset now (the "no" shake; px).</summary>
    public float OffsetX => _shake.Value;

    /// <summary>True while any part still moves (the view keeps stepping it).</summary>
    public bool Moving => !(_x.AtRest && _y.AtRest && _pop.AtRest && _shake.AtRest);

    /// <summary>True while it is held down.</summary>
    public bool Pressed => _pressed;

    /// <summary>The pointer came over the control (<paramref name="over"/>) or left it: it lifts or comes back down (not while pressed: the release decides).</summary>
    public void Hover(bool over, MotionKnobs knobs, MotionAmount amount)
    {
        _hovered = over;
        if (!_pressed)
            Aim(over ? knobs.hoverScale : 1f, over ? knobs.hoverScale : 1f, knobs.hoverFeel, amount);
    }

    /// <summary>Pressed down: the anticipation squash.</summary>
    public void Press(MotionKnobs knobs, MotionAmount amount)
    {
        _pressed = true;
        Aim(knobs.pressScaleX, knobs.pressScaleY, knobs.pressFeel, amount);
    }

    /// <summary>Let go: springs back (with overshoot) to its hover lift, or its rest when the pointer has left.</summary>
    public void Release(MotionKnobs knobs, MotionAmount amount)
    {
        if (!_pressed)
            return;
        _pressed = false;
        float rest = _hovered ? knobs.hoverScale : 1f;
        Aim(rest, rest, knobs.releaseFeel, amount);
    }

    /// <summary>A confirmed click: the pop (out to 1 + popAmount and back).</summary>
    public void Confirm(MotionKnobs knobs, MotionAmount amount)
    {
        if (amount.Still)
            return;
        _pop.Kick(knobs.Get(knobs.popFeel).KickFor(knobs.popAmount * amount.Share));
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
        _x.Snap(1f);
        _y.Snap(1f);
        _pop.Snap(0f);
        _shake.Snap(0f);
    }

    /// <summary>Advances every part by <paramref name="dt"/> seconds; true while any still moves.</summary>
    public bool Step(float dt, MotionKnobs knobs)
    {
        SpringTuning tuning = knobs.Get(_baseFeel);
        _x.Step(dt, tuning, knobs.settleValue, knobs.settleSpeed);
        _y.Step(dt, tuning, knobs.settleValue, knobs.settleSpeed);
        _pop.Step(dt, knobs.Get(knobs.popFeel), knobs.settleValue, knobs.settleSpeed);
        _shake.Step(dt, knobs.Get(knobs.refuseFeel), knobs.settleValue * 100f, knobs.settleSpeed * 100f); // px, not a scale
        return Moving;
    }

    /// <summary>Pulls the base pose toward <paramref name="x"/>, <paramref name="y"/> (scaled by the amount; a cut when nothing moves) with <paramref name="feel"/>.</summary>
    private void Aim(float x, float y, MotionFeel feel, MotionAmount amount)
    {
        _baseFeel = feel;
        float tx = amount.Scale(x), ty = amount.Scale(y);
        if (amount.Still)
        {
            _x.Snap(tx);
            _y.Snap(ty);
            return;
        }
        _x.Target = tx;
        _y.Target = ty;
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
