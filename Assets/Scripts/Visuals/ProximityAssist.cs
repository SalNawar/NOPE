using System;
using System.Collections.Generic;

/// <summary>Where a slider is in its slide-in.</summary>
public enum SlidePhase
{
    /// <summary>At home.</summary>
    Rest,

    /// <summary>Sliding out, or out at its slid spot while the cursor is near.</summary>
    Out,

    /// <summary>Out, the cursor gone: it waits DeskSlides.lingerSeconds before it goes home.</summary>
    Linger,

    /// <summary>Going home (an ease-in that arrives at speed) and its seat bounce.</summary>
    Back,

    /// <summary>The hand-off loser easing home at once (no linger, no seat).</summary>
    Retract,

    /// <summary>The hand-off winner, waiting until the loser is nearly seated.</summary>
    Wait,
}

/// <summary>A sync point of the slide-in (render/slides.json cues): what sounds, what the lamp and the shadow follow.</summary>
public enum SlideCueKind
{
    /// <summary>The trigger frame (out, t 0): the soft relay tick of the unlatch.</summary>
    Unlatch,

    /// <summary>The base starts to move (out, the anticipation's end): the felt shush.</summary>
    Slide,

    /// <summary>The overshoot peak (or the cut's frame): the muted knock, the lamp's pulse, the shadow's press, the mic's sway.</summary>
    Peak,

    /// <summary>The way home starts (back, t 0): the reversed shush.</summary>
    Back,

    /// <summary>The base hits its home stop at full speed (back, t backSeconds): the clack, on the frame the seat bounce starts.</summary>
    Seat,

    /// <summary>The hand-off loser starts its retract: the reversed shush cut short, no clack.</summary>
    HandOff,
}

/// <summary>One cue raised by a step: the slider and the sync point.</summary>
public readonly struct SlideCue
{
    /// <summary>The cue of <paramref name="kind"/> for the slider <paramref name="id"/>.</summary>
    public SlideCue(string id, SlideCueKind kind)
    {
        Id = id;
        Kind = kind;
    }

    /// <summary>The slider.</summary>
    public string Id { get; }

    /// <summary>The sync point.</summary>
    public SlideCueKind Kind { get; }
}

/// <summary>A slider present on the desk as the cursor finds it this frame.</summary>
public readonly struct SlideCandidate
{
    /// <summary>The slider <paramref name="id"/>: its silhouette's rank under the cursor (<paramref name="over"/>, the front-most 0; -1 when the cursor is not on it), whether the cursor's desk point lies in its rest footprint and its distance from the cursor to its silhouette (px at DeskSlides.referenceHeight; 0 on it).</summary>
    public SlideCandidate(string id, int over, bool onFootprint, float distance)
    {
        Id = id;
        Over = over;
        OnFootprint = onFootprint;
        Distance = distance;
    }

    /// <summary>The slider's id.</summary>
    public string Id { get; }

    /// <summary>Its silhouette's rank under the cursor (front-most 0), or -1.</summary>
    public int Over { get; }

    /// <summary>True when the cursor's point on the desk lies in its rest footprint.</summary>
    public bool OnFootprint { get; }

    /// <summary>The px from the cursor to its silhouette at the reference height (0 on it).</summary>
    public float Distance { get; }
}

/// <summary>A slider's pose this frame: what its slide transform, its shadow and its lamp show.</summary>
public readonly struct SlidePose
{
    /// <summary>The pose at <paramref name="position"/> of the travel, with its squash and sway (px), its shadow (alpha, blur px, widening; tightened at the peak) and its lamp's gain.</summary>
    public SlidePose(SlidePhase phase, float position, float squash, float sway, float shadow, float shadowBlur, float shadowWiden, bool tight, float lamp)
    {
        Phase = phase;
        Position = position;
        Squash = squash;
        Sway = sway;
        Shadow = shadow;
        ShadowBlur = shadowBlur;
        ShadowWiden = shadowWiden;
        Tight = tight;
        Lamp = lamp;
    }

    /// <summary>The phase.</summary>
    public SlidePhase Phase { get; }

    /// <summary>The share of its travel it stands at (0 home, 1 the slid spot; past 1 at the overshoot, above 0 in the seat bounce, below 0 in a track's anticipation).</summary>
    public float Position { get; }

    /// <summary>A lift's squash: px of height (1080p) its top comes down, its base staying down.</summary>
    public float Squash { get; }

    /// <summary>The secondary sway toward the mat (px at 1080p; the intercom's mic).</summary>
    public float Sway { get; }

    /// <summary>The contact shadow's alpha (a track's travels with its base; a lift's stays at its rest footprint).</summary>
    public float Shadow { get; }

    /// <summary>A lift's shadow blur (px at 1080p).</summary>
    public float ShadowBlur { get; }

    /// <summary>A lift's shadow widening (a factor on its rest footprint).</summary>
    public float ShadowWiden { get; }

    /// <summary>True for the frames around the overshoot peak, when a track's shadow tightens (the base presses the felt).</summary>
    public bool Tight { get; }

    /// <summary>The gain on its own next-tool lamp (1, or DeskSlides.lampPulse just after the peak when the lamp is lit).</summary>
    public float Lamp { get; }
}

/// <summary>
/// The desk's slide-in assist (desk v3; Saleh: "yes but they need to slide";
/// D2: "I want the mouse to auto summon the machine when the player goes near
/// it the machine will slide in a little bit to help the player"; UNITY_PLAN
/// 5.2, DESK-HOMES P2). Each frame (Step) the desk tells it every present
/// slider as the cursor finds it (SlideCandidate) and which are busy, and it
/// decides:
/// <list type="bullet">
/// <item><b>Nearest wins, one at a time</b> (Pick): the front-most silhouette
/// under the cursor wins at once, whatever is out; else a home footprint
/// under the cursor's desk point (the one out keeps it if it is one of them);
/// else the nearest within the trigger radius, the one out keeping it unless
/// another is nearer by more than the hysteresis.</item>
/// <item><b>Hand-off:</b> the loser retracts at once (ease-in-out, no linger,
/// no seat) and the winner waits until the loser is nearly seated (under
/// winnerBelow of its travel, or winnerAfter seconds into its retract). A
/// slider still going home counts as a loser too, so one machine moves at a
/// time and two are never both half out.</item>
/// <item><b>Out:</b> the anticipation and its own spring, with one overshoot.
/// <b>Back:</b> after lingerSeconds outside the trigger, the ease-in home that
/// arrives at speed and the seat bounce on the arrival frame.</item>
/// <item><b>Busy:</b> a machine that is scanning, reading or holding a paper is
/// left as it is: out, it stays out (it never lingers home) and never loses a
/// hand-off; at home, it is not summoned.</item>
/// <item><b>Reduced Motion</b> (or Motion intensity 0): every move is a
/// one-frame cut (no anticipation, overshoot, seat, squash, sway or tip); the
/// travel never scales and the cues still sound.</item>
/// <item><b>Felt gap:</b> a track never goes past DeskSlider.Reach.</item>
/// </list>
/// It raises the frame's cues (Cues: sound, lamp, shadow sync points) and gives
/// each slider's pose (Pose). Pure; MachineSlideIn and DeskSlideIn apply it.
/// </summary>
public sealed class ProximityAssist
{
    private sealed class State
    {
        public DeskSlider Slider;
        public SlidePhase Phase;
        public float T0, Start, LingerFrom;
        public State Loser;
    }

    private static readonly ICollection<string> NoneBusy = Array.Empty<string>();

    private readonly DeskSlides _slides;
    private readonly List<State> _states = new List<State>();
    private readonly Dictionary<string, State> _byId = new Dictionary<string, State>();
    private readonly List<SlideCue> _cues = new List<SlideCue>();
    private readonly List<SlideCandidate> _eligible = new List<SlideCandidate>();
    private float _now;
    private bool _still;

    /// <summary>The assist over <paramref name="slides"/>' moving sliders, every one at home.</summary>
    public ProximityAssist(DeskSlides slides)
    {
        _slides = slides ?? throw new ArgumentNullException(nameof(slides));
        foreach (DeskSlider slider in slides.sliders ?? Array.Empty<DeskSlider>())
        {
            if (slider == null || !slider.Moves || _byId.ContainsKey(slider.id))
                continue;
            var state = new State { Slider = slider };
            _states.Add(state);
            _byId.Add(slider.id, state);
        }
    }

    /// <summary>The cues the last Step raised, in order.</summary>
    public IReadOnlyList<SlideCue> Cues => _cues;

    /// <summary>The slider the last Step picked for the cursor (null: none).</summary>
    public string Target { get; private set; }

    /// <summary>
    /// The slider that should be out for the cursor (null: none), among
    /// <paramref name="candidates"/> (<paramref name="current"/>: the one the
    /// assist has out now): the front-most silhouette under the cursor at
    /// once; else a home footprint under it (the current one if it is among
    /// them, else the first listed); else the nearest within
    /// <paramref name="trigger"/> (the current within trigger plus
    /// <paramref name="hysteresis"/>), the current keeping it unless another
    /// is nearer by more than the hysteresis.
    /// </summary>
    public static string Pick(IReadOnlyList<SlideCandidate> candidates, string current, float trigger, float hysteresis)
    {
        if (candidates == null || candidates.Count == 0)
            return null;
        string front = null;
        int frontRank = int.MaxValue;
        foreach (SlideCandidate c in candidates)
            if (c.Over >= 0 && c.Over < frontRank)
            {
                front = c.Id;
                frontRank = c.Over;
            }
        if (front != null)
            return front;

        string first = null;
        foreach (SlideCandidate c in candidates)
            if (c.OnFootprint)
            {
                if (c.Id == current)
                    return current;
                first ??= c.Id;
            }
        if (first != null)
            return first;

        string near = null;
        float nearest = float.MaxValue, currentDistance = -1f;
        foreach (SlideCandidate c in candidates)
        {
            bool isCurrent = c.Id == current;
            if (c.Distance > trigger + (isCurrent ? hysteresis : 0f))
                continue;
            if (isCurrent)
                currentDistance = c.Distance;
            if (c.Distance < nearest)
            {
                near = c.Id;
                nearest = c.Distance;
            }
        }
        if (near == null)
            return null;
        return currentDistance >= 0f && currentDistance <= nearest + hysteresis ? current : near;
    }

    /// <summary>
    /// Advances the assist to <paramref name="now"/> (seconds, the game feel's
    /// clock) with the present sliders as the cursor finds them
    /// (<paramref name="candidates"/>; a slider not among them is not
    /// summoned), the <paramref name="busy"/> ones (scanning, reading or
    /// holding a paper) and whether every move is a cut
    /// (<paramref name="still"/>: Reduced Motion or Motion intensity 0). The
    /// frame's cues are in Cues.
    /// </summary>
    public void Step(float now, IReadOnlyList<SlideCandidate> candidates, ICollection<string> busy, bool still)
    {
        float was = _now;
        _now = now;
        _still = still;
        _cues.Clear();
        busy ??= NoneBusy;

        foreach (State s in _states)
            if (s.Phase == SlidePhase.Linger && busy.Contains(s.Slider.id))
                s.Phase = SlidePhase.Out;

        State current = Current(busy);
        _eligible.Clear();
        foreach (SlideCandidate c in candidates ?? Array.Empty<SlideCandidate>())
            if (c.Id != null && _byId.ContainsKey(c.Id) && !busy.Contains(c.Id))
                _eligible.Add(c);
        string target = Pick(_eligible, current?.Slider.id, _slides.triggerPx, _slides.hysteresisPx);
        Target = target;

        if (target != current?.Slider.id)
        {
            // A hand-off: the loser gives way at once. With no target, one out lingers (below); one still waiting to start never does.
            if (current != null && (target != null || current.Phase == SlidePhase.Wait))
                Lose(current);
            if (target != null)
                Summon(_byId[target]);
        }

        foreach (State s in _states)
        {
            float t = now - s.T0, before = was - s.T0;
            if (!still && (s.Phase == SlidePhase.Out || s.Phase == SlidePhase.Linger))
            {
                // The out spring runs on through a linger, so its shush and its knock sound on time even if the cursor already left.
                if (Crossed(before, t, _slides.anticipationSeconds))
                    _cues.Add(new SlideCue(s.Slider.id, SlideCueKind.Slide));
                if (Crossed(before, t, s.Slider.peakSeconds))
                    _cues.Add(new SlideCue(s.Slider.id, SlideCueKind.Peak));
            }
            switch (s.Phase)
            {
                case SlidePhase.Out:
                    if (s.Slider.id != target && !busy.Contains(s.Slider.id))
                    {
                        s.Phase = SlidePhase.Linger;
                        s.LingerFrom = now;
                    }
                    break;
                case SlidePhase.Linger:
                    if (s.Slider.id == target)
                        s.Phase = SlidePhase.Out;
                    else if (now - s.LingerFrom >= _slides.lingerSeconds)
                        GoHome(s);
                    break;
                case SlidePhase.Back:
                    if (still || t >= _slides.backSeconds + _slides.seatSeconds)
                        Settle(s);
                    if (!still && Crossed(before, t, _slides.backSeconds))
                        _cues.Add(new SlideCue(s.Slider.id, SlideCueKind.Seat));
                    break;
                case SlidePhase.Retract:
                    if (still || t >= _slides.retractSeconds)
                        Settle(s);
                    break;
            }
        }

        foreach (State s in _states)
            if (s.Phase == SlidePhase.Wait && !busy.Contains(s.Slider.id) && Seated(s.Loser))
                Begin(s, s.Start);
    }

    /// <summary>Where <paramref name="id"/> stands now (a share of its travel; 0 for an unknown or fixed slider).</summary>
    public float Position(string id) => id != null && _byId.TryGetValue(id, out State s) ? Position(s) : 0f;

    /// <summary>The phase of <paramref name="id"/> (Rest for an unknown or fixed slider).</summary>
    public SlidePhase Phase(string id) => id != null && _byId.TryGetValue(id, out State s) ? s.Phase : SlidePhase.Rest;

    /// <summary>
    /// The pose of <paramref name="id"/> now: its position; a lift's squash
    /// over its anticipation; the sway after the peak; its contact shadow (a
    /// track's leaves the well with the base and fades in, presses at the
    /// peak, fades out as it re-enters; a lift's stays at the rest footprint,
    /// fading and widening as it rises); its lamp's pulse just after the peak
    /// when <paramref name="lampLit"/>. Under Reduced Motion: the position, a
    /// still shadow and the pulse, nothing else.
    /// </summary>
    public SlidePose Pose(string id, bool lampLit)
    {
        if (id == null || !_byId.TryGetValue(id, out State s))
            return new SlidePose(SlidePhase.Rest, 0f, 0f, 0f, 0f, 0f, 1f, false, 1f);
        DeskSlider slider = s.Slider;
        float f = Position(s), t = _now - s.T0;
        bool out_ = s.Phase == SlidePhase.Out || s.Phase == SlidePhase.Linger;
        float sincePeak = t - (_still ? 0f : slider.peakSeconds);
        float squash = 0f, sway = 0f, shadow = 0f, blur = 0f, widen = 1f;
        bool tight = false;
        if (out_ && !_still)
        {
            if (slider.kind == SlideKind.Lift)
                squash = SlideCurve.Squash(_slides, t);
            sway = SlideCurve.Sway(slider, sincePeak);
            tight = Math.Abs(sincePeak) <= _slides.peakTightSeconds;
        }
        if (slider.kind == SlideKind.Lift)
        {
            (shadow, blur, widen) = SlideCurve.LiftShadow(_slides, slider, f);
        }
        else
        {
            switch (s.Phase)
            {
                case SlidePhase.Out:
                case SlidePhase.Linger:
                    shadow = _still ? _slides.trackShadowAlpha : SlideCurve.TrackShadowOut(_slides, t);
                    break;
                case SlidePhase.Back:
                    shadow = SlideCurve.TrackShadowBack(_slides, t);
                    break;
                case SlidePhase.Retract:
                    shadow = SlideCurve.TrackShadowRetract(_slides, t);
                    break;
            }
        }
        float lamp = lampLit && out_ && sincePeak >= 0f && sincePeak < _slides.lampPulseSeconds ? _slides.lampPulse : 1f;
        return new SlidePose(s.Phase, f, squash, sway, shadow, blur, widen, tight, lamp);
    }

    /// <summary>The object <paramref name="id"/> is taken in hand (the UV torch picked up): if it is out it gives way at once as a hand-off loser does (the retract from where it is, no linger, no seat; a cut under Reduced Motion), so the hand lifts it from a pose that keeps moving smoothly.</summary>
    public void Release(string id)
    {
        if (id == null || !_byId.TryGetValue(id, out State s))
            return;
        if (s.Phase == SlidePhase.Out || s.Phase == SlidePhase.Linger || s.Phase == SlidePhase.Wait)
            Lose(s);
    }

    /// <summary>Cuts <paramref name="id"/> home at once (null: every slider): it left the desk, or a forced retract (a load, a new day). A winner waiting on it starts at the next Step.</summary>
    public void Home(string id)
    {
        foreach (State s in _states)
            if (id == null || s.Slider.id == id)
                Settle(s);
    }

    /// <summary>The slider the assist has out for the cursor: the first, in the data's order, that is out, lingering or waiting, and not busy.</summary>
    private State Current(ICollection<string> busy)
    {
        foreach (State s in _states)
            if ((s.Phase == SlidePhase.Out || s.Phase == SlidePhase.Linger || s.Phase == SlidePhase.Wait) && !busy.Contains(s.Slider.id))
                return s;
        return null;
    }

    /// <summary>The hand-off's loser: one waiting goes back to rest (from where it waited); one out retracts at once, from where it is.</summary>
    private void Lose(State s)
    {
        float from = Position(s);
        if (s.Phase == SlidePhase.Wait && from <= 0f)
        {
            Settle(s);
            return;
        }
        _cues.Add(new SlideCue(s.Slider.id, SlideCueKind.HandOff));
        if (_still)
        {
            Settle(s);
            return;
        }
        s.Phase = SlidePhase.Retract;
        s.T0 = _now;
        s.Start = from;
    }

    /// <summary>The cursor's pick: one already out stays out; else it starts from where it is, or waits while another slider going home is still more than nearly seated.</summary>
    private void Summon(State s)
    {
        if (s.Phase == SlidePhase.Out || s.Phase == SlidePhase.Linger)
        {
            s.Phase = SlidePhase.Out;
            return;
        }
        float from = Math.Max(0f, Position(s));
        State blocker = null;
        float furthest = 0f;
        foreach (State other in _states)
        {
            if (other == s || (other.Phase != SlidePhase.Back && other.Phase != SlidePhase.Retract) || Seated(other))
                continue;
            float at = Position(other);
            if (blocker == null || at > furthest)
            {
                blocker = other;
                furthest = at;
            }
        }
        if (blocker == null)
        {
            Begin(s, from);
            return;
        }
        s.Phase = SlidePhase.Wait;
        s.T0 = _now;
        s.Start = from;
        s.Loser = blocker;
    }

    /// <summary>True when <paramref name="loser"/> no longer holds a winner back: it is home or out again, under winnerBelow of its travel, or winnerAfter seconds into its retract.</summary>
    private bool Seated(State loser)
    {
        if (loser == null || (loser.Phase != SlidePhase.Back && loser.Phase != SlidePhase.Retract))
            return true;
        if (loser.Phase == SlidePhase.Retract && _now - loser.T0 >= _slides.winnerAfter - 1e-5f)
            return true;
        return Position(loser) < _slides.winnerBelow;
    }

    /// <summary>The slide out starts from <paramref name="from"/>: the unlatch's tick (and, as a cut, the peak's).</summary>
    private void Begin(State s, float from)
    {
        s.Phase = SlidePhase.Out;
        s.T0 = _now;
        s.Start = from;
        s.Loser = null;
        _cues.Add(new SlideCue(s.Slider.id, SlideCueKind.Unlatch));
        if (_still)
            _cues.Add(new SlideCue(s.Slider.id, SlideCueKind.Peak));
    }

    /// <summary>The linger is over: home with the reversed shush (as a cut, the seat's clack at once).</summary>
    private void GoHome(State s)
    {
        if (_still)
        {
            Settle(s);
            _cues.Add(new SlideCue(s.Slider.id, SlideCueKind.Seat));
            return;
        }
        float from = Position(s);
        s.Phase = SlidePhase.Back;
        s.T0 = _now;
        s.Start = from;
        _cues.Add(new SlideCue(s.Slider.id, SlideCueKind.Back));
    }

    /// <summary>At home.</summary>
    private static void Settle(State s)
    {
        s.Phase = SlidePhase.Rest;
        s.Start = 0f;
        s.Loser = null;
    }

    /// <summary>Where <paramref name="s"/> stands now, never past its felt gap.</summary>
    private float Position(State s)
    {
        float f;
        float t = _now - s.T0;
        switch (s.Phase)
        {
            case SlidePhase.Out:
            case SlidePhase.Linger:
                f = _still ? 1f : s.Start + (1f - s.Start) * SlideCurve.Out(_slides, s.Slider, t);
                break;
            case SlidePhase.Wait:
                f = s.Start;
                break;
            case SlidePhase.Back:
                f = _still ? 0f : SlideCurve.Back(_slides, s.Slider, t, s.Start);
                break;
            case SlidePhase.Retract:
                f = _still ? 0f : SlideCurve.Retract(_slides, t, s.Start);
                break;
            default:
                f = 0f;
                break;
        }
        float reach = s.Slider.Reach(_slides.feltGap);
        return s.Slider.travel > 0f && f * s.Slider.travel > reach ? reach / s.Slider.travel : f;
    }

    /// <summary>True when the clock passed <paramref name="mark"/> between <paramref name="before"/> (exclusive) and <paramref name="after"/> (inclusive).</summary>
    private static bool Crossed(float before, float after, float mark) => before < mark && after >= mark;
}
