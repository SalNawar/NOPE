using UnityEngine;

/// <summary>How a popup or a panel comes and goes (UiAppear).</summary>
public enum AppearStyle
{
    /// <summary>Grows from MotionKnobs.appearFromScale with an overshoot (a tooltip, a window, a wheel's pill).</summary>
    Pop,

    /// <summary>Slides up from below with a spring (a toast).</summary>
    Rise,

    /// <summary>Drops in from above and settles like paper (the newspaper, the citation slip).</summary>
    Drop,

    /// <summary>Whips in from the right, elastic and fast (a verdict ribbon).</summary>
    Whip
}

/// <summary>
/// A popup's or a panel's coming and going through the game feel's springs
/// (AppearMotion; Saleh 2026-10-07, round 2: what travels stretches along its
/// travel by its speed, overshoots, squashes on arrival and wobbles into
/// place: SquashStretch.FromMotion): Open after the object is switched on (it grows, slides or
/// drops in from its style's start, optionally from a point: a window from
/// its taskbar button, after a stagger's delay), Close to take it away (it
/// shrinks or slides back, optionally toward a point, takes no clicks while
/// it goes, then switches itself off). Its opacity is a CanvasGroup's (its
/// own, or one added). Reduced Motion fades only; Motion intensity 0 cuts.
/// The object's rest pose (scale, place, opacity) comes back exactly when it
/// settles or is switched off mid-motion.
/// </summary>
[DisallowMultipleComponent]
public sealed class UiAppear : MonoBehaviour, IMotionTick
{
    private readonly AppearMotion _motion = new AppearMotion();
    private AppearStyle _style;
    private CanvasGroup _group;
    private Vector3 _restScale = Vector3.one;
    private float _restAlpha = 1f;
    private bool _restBlocks = true;
    private Vector3 _from;
    private Vector3 _applied;
    private bool _posed, _closing, _finishing;
    private MotionAmount _amount;

    /// <summary>The tuning of the running motion (its acceleration shapes the squash).</summary>
    private SpringTuning _tuning;

    /// <summary>True while it is going (Close ran; it switches itself off when gone).</summary>
    public bool Closing => _closing;

    /// <summary>True while it moves (the probes wait for it).</summary>
    public bool Moving => _motion.Moving;

    /// <summary><paramref name="target"/>'s appear motion with <paramref name="style"/> (added when missing).</summary>
    public static UiAppear Of(GameObject target, AppearStyle style)
    {
        if (target == null)
            return null;
        if (!target.TryGetComponent(out UiAppear appear))
            appear = target.AddComponent<UiAppear>();
        appear._style = style;
        return appear;
    }

    /// <summary>True when <paramref name="target"/> is on its way out (Close ran and it is still showing).</summary>
    public static bool IsClosing(GameObject target) => target != null && target.TryGetComponent(out UiAppear appear) && appear._closing;

    /// <summary>
    /// Sets <paramref name="target"/>'s place outright while it may be moving
    /// (a window's maximise and restore, its drag, its pull back on screen):
    /// <paramref name="place"/> runs on the rest place, the motion's offset
    /// taken out first and put back after, so the motion goes on from the new
    /// place and settles exactly there. (A place set over the offset, then
    /// settled, ended off by the offset: the playtest's Investigation window,
    /// maximised on its first frame, came to rest pushed up off the screen.)
    /// </summary>
    public static void Place(GameObject target, System.Action place)
    {
        if (target == null || !target.TryGetComponent(out UiAppear appear) || !appear._posed)
        {
            place();
            return;
        }
        Transform t = appear.transform;
        t.localPosition -= appear._applied;
        place();
        t.localPosition += appear._applied;
    }

    /// <summary>Plays it in from its style's start, after <paramref name="delay"/> seconds (the object should be on).</summary>
    public void Open(float delay = 0f) => OpenFrom(StyleStart(), delay);

    /// <summary>Plays it in growing from <paramref name="world"/> (a window from its taskbar button, a wheel's pill from the ring's centre) after <paramref name="delay"/> seconds.</summary>
    public void Open(Vector3 world, float delay = 0f) => OpenFrom(LocalOffsetTo(world), delay);

    /// <summary>Takes it away toward its style's start, then switches it off.</summary>
    public void Close() => CloseTo(StyleStart());

    /// <summary>Takes it away shrinking toward <paramref name="world"/> (a window into its taskbar button), then switches it off.</summary>
    public void Close(Vector3 world) => CloseTo(LocalOffsetTo(world));

    private void OpenFrom(Vector3 from, float delay)
    {
        Rest();
        _closing = false;
        _from = from;
        _amount = UiMotion.Amount;
        if (_group != null)
            _group.blocksRaycasts = _restBlocks;
        if (!_motion.Open && !_motion.Moving || _motion.Gone)
            _motion.Snap(false);
        _motion.Show(true, _amount, delay);
        Apply();
        UiMotion.Run(this);
    }

    private void CloseTo(Vector3 to)
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            return;
        }
        Rest();
        _amount = UiMotion.Amount;
        if (_amount.Still && !_amount.Reduced)
        {
            Finish();
            return;
        }
        _closing = true;
        _from = to;
        if (_group != null)
            _group.blocksRaycasts = false;
        _motion.Show(false, _amount);
        UiMotion.Run(this);
    }

    /// <summary>Steps the motion and draws it; switches the object off once it has gone; false once settled.</summary>
    public bool TickMotion(float dt)
    {
        if (this == null)
            return false;
        MotionKnobs knobs = UiMotion.Knobs;
        MotionFeel feel = _closing ? MotionFeel.Heavy : _style == AppearStyle.Pop ? knobs.appearFeel : _style == AppearStyle.Drop ? knobs.paperFeel : knobs.slideFeel;
        _tuning = knobs.Get(feel);
        bool moving = _motion.Step(dt, _tuning, knobs.reducedFadeSeconds, knobs.settleValue, knobs.settleSpeed);
        if (_closing && _motion.Gone)
        {
            Finish();
            return false;
        }
        Apply();
        if (!moving)
            Settle();
        return moving;
    }

    /// <summary>Switched off mid-motion: the rest pose at once, and gone (the next Open plays in from the start; unless it is its own close finishing).</summary>
    private void OnDisable()
    {
        if (_finishing)
            return;
        _closing = false;
        Settle();
        _motion.Snap(false);
    }

    /// <summary>Gone: the rest pose back, then off.</summary>
    private void Finish()
    {
        _closing = false;
        Settle();
        _motion.Snap(false);
        _finishing = true;
        gameObject.SetActive(false);
        _finishing = false;
    }

    /// <summary>Takes note of the rest pose (once per motion: a motion that starts while one runs keeps the first one's).</summary>
    private void Rest()
    {
        if (_posed)
            return;
        if (_group == null && !TryGetComponent(out _group))
            _group = gameObject.AddComponent<CanvasGroup>();
        _restScale = transform.localScale;
        _restAlpha = _group.alpha;
        _restBlocks = _group.blocksRaycasts;
        _applied = Vector3.zero;
        _posed = true;
    }

    /// <summary>Draws the presence: the scale from the start's, the offset from the start (only its change applied, so a layout keeps the place), the opacity.</summary>
    private void Apply()
    {
        if (!_posed)
            return;
        MotionKnobs knobs = UiMotion.Knobs;
        float from = _style == AppearStyle.Pop || _from != StyleStart() ? knobs.appearFromScale : 1f;
        float s = _motion.Scale(from, _amount);
        // A piece that travels (a slide, a window from its taskbar button, a wheel pill from the ring's centre) stretches along its travel by its speed and squashes by its acceleration.
        float distance = _from.magnitude * _amount.Share;
        Stretch st = distance > 0f
            ? SquashStretch.FromMotion(distance * _motion.Speed, distance * _motion.Acceleration(_tuning), knobs.stretchPerSpeed, knobs.squashPerAccel, knobs.maxStretch)
            : Stretch.None;
        bool alongY = Mathf.Abs(_from.y) > Mathf.Abs(_from.x);
        float sx = s * (alongY ? st.Across : st.Along), sy = s * (alongY ? st.Along : st.Across);
        // Never more than appearGrowMax px past its rest size either way (a big window stretched by a quarter would leave the screen).
        Rect rect = ((RectTransform)transform).rect;
        if (rect.width > 0f)
            sx = Mathf.Min(sx, 1f + knobs.appearGrowMax / rect.width);
        if (rect.height > 0f)
            sy = Mathf.Min(sy, 1f + knobs.appearGrowMax / rect.height);
        transform.localScale = new Vector3(_restScale.x * sx, _restScale.y * sy, _restScale.z * s);
        Vector3 offset = _from * _motion.Offset(1f, _amount);
        if (offset != _applied)
        {
            transform.localPosition += offset - _applied;
            _applied = offset;
        }
        _group.alpha = _restAlpha * _motion.Alpha;
    }

    /// <summary>The rest pose exactly.</summary>
    private void Settle()
    {
        if (!_posed)
            return;
        transform.localScale = _restScale;
        transform.localPosition -= _applied;
        _applied = Vector3.zero;
        if (_group != null)
        {
            _group.alpha = _restAlpha;
            _group.blocksRaycasts = _restBlocks;
        }
        _posed = false;
    }

    /// <summary>Where the style starts from, as a local offset (reference px).</summary>
    private Vector3 StyleStart()
    {
        float d = UiMotion.Knobs.slideDistance;
        switch (_style)
        {
            case AppearStyle.Rise:
                return new Vector3(0f, -d, 0f);
            case AppearStyle.Drop:
                return new Vector3(0f, d, 0f);
            case AppearStyle.Whip:
                return new Vector3(d * 2f, 0f, 0f);
            default:
                return Vector3.zero;
        }
    }

    /// <summary>The offset from the object's place to <paramref name="world"/>, in its parent's space.</summary>
    private Vector3 LocalOffsetTo(Vector3 world)
    {
        Transform parent = transform.parent;
        Vector3 local = parent != null ? parent.InverseTransformPoint(world) : world;
        Vector3 offset = local - (transform.localPosition - _applied);
        offset.z = 0f;
        return offset;
    }
}
