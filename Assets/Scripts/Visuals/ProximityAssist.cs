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

    /// <summary>Busy on its way (a paper came onto it while it went home or gave way): it stays where it stood until it is empty.</summary>
    Held,
}

/// <summary>A sync point of the slide-in (render/slides.json cues): what sounds and what the shadow follows.</summary>
public enum SlideCueKind
{
    /// <summary>The trigger frame (out, t 0): the soft relay tick of the unlatch.</summary>
    Unlatch,

    /// <summary>The base starts to move (out, the anticipation's end): the felt shush.</summary>
    Slide,

    /// <summary>The overshoot peak (or the cut's frame): the muted knock, the shadow's press, the mic's sway.</summary>
    Peak,

    /// <summary>The way home starts (back, t 0): the reversed shush.</summary>
    Back,

    /// <summary>The base hits its home stop at full speed (back, t backSeconds): the clack, on the frame the seat bounce starts. A hand-off loser's way home (a retract, or a way home hurried by the winner) has none: the winner's unlatch tick covers it.</summary>
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

/// <summary>A slider attached to the desk as this frame finds it: whether it stands there, is in the player's hand or busy, and where the cursor is from it.</summary>
public readonly struct SlideCandidate
{
    /// <summary>
    /// The slider <paramref name="id"/>: its silhouette's rank under the
    /// cursor (<paramref name="over"/>, the front-most 0; -1 when the cursor
    /// is not on it), whether the cursor's desk point lies in its rest
    /// footprint, its distance from the cursor to its silhouette (px at
    /// DeskSlides.referenceHeight; 0 on it), whether it is
    /// <paramref name="busy"/> (scanning, reading, receiving a paper),
    /// <paramref name="taken"/> in the player's hand,
    /// <paramref name="present"/> on the desk and whether the paper docked
    /// in it <paramref name="rides"/> it home in a forced retract.
    /// </summary>
    public SlideCandidate(string id, int over, bool onFootprint, float distance, bool busy = false, bool taken = false, bool present = true, bool rides = false)
    {
        Id = id;
        Over = over;
        OnFootprint = onFootprint;
        Distance = distance;
        Busy = busy;
        Taken = taken;
        Present = present;
        Rides = rides;
    }

    /// <summary>The slider <paramref name="id"/> with the cursor nowhere near it (no pointer, the PC open): its state only.</summary>
    public static SlideCandidate Away(string id, bool busy = false, bool taken = false, bool present = true, bool rides = false) =>
        new SlideCandidate(id, -1, false, float.PositiveInfinity, busy, taken, present, rides);

    /// <summary>
    /// The slider <paramref name="id"/> as the desk finds it this frame
    /// (DeskSlideIn's rule): not <paramref name="present"/> on the desk, only
    /// that; the cursor away from the desk view (<paramref name="near"/>
    /// false: no pointer, the PC open) or the slider
    /// <paramref name="taken"/> in hand, its state only; else also its
    /// silhouette's <paramref name="rank"/> under the cursor, whether the
    /// cursor's desk point lies in its home's footprint
    /// (<paramref name="onFootprint"/>) and the cursor's
    /// <paramref name="distance"/> from its silhouette (px at the reference
    /// height: DeskSlides.ToReference).
    /// </summary>
    public static SlideCandidate Found(string id, bool present, bool busy, bool taken, bool rides, bool near, int rank, bool onFootprint, float distance)
    {
        if (!present)
            return Away(id, present: false);
        if (!near || taken)
            return Away(id, busy, taken, true, rides);
        return new SlideCandidate(id, rank, onFootprint, distance, busy, false, true, rides);
    }

    /// <summary>The slider's id.</summary>
    public string Id { get; }

    /// <summary>Its silhouette's rank under the cursor (front-most 0), or -1.</summary>
    public int Over { get; }

    /// <summary>True when the cursor's point on the desk lies in its rest footprint.</summary>
    public bool OnFootprint { get; }

    /// <summary>The px from the cursor to its silhouette at the reference height (0 on it).</summary>
    public float Distance { get; }

    /// <summary>True while it works or receives a paper (its own state; the drop hold is the assist's).</summary>
    public bool Busy { get; }

    /// <summary>True while its object is in the player's hand (the UV torch picked up).</summary>
    public bool Taken { get; }

    /// <summary>True while it stands on the desk (bound, shown, not waiting at zero scale for the crate).</summary>
    public bool Present { get; }

    /// <summary>True when the paper docked in it rides it home (one mover carries both): a forced retract takes it even while it is busy.</summary>
    public bool Rides { get; }
}

/// <summary>A slider's pose this frame: what its slide transform and its contact shadow show.</summary>
public readonly struct SlidePose
{
    /// <summary>The pose at <paramref name="position"/> of the travel, with its squash and sway (px) and its contact shadow (alpha, blur px, widening).</summary>
    public SlidePose(SlidePhase phase, float position, float squash, float sway, float shadow, float shadowBlur, float shadowWiden)
    {
        Phase = phase;
        Position = position;
        Squash = squash;
        Sway = sway;
        Shadow = shadow;
        ShadowBlur = shadowBlur;
        ShadowWiden = shadowWiden;
    }

    /// <summary>The phase.</summary>
    public SlidePhase Phase { get; }

    /// <summary>The share of its travel it stands at (0 home, 1 the slid spot; past 1 at the overshoot, above 0 in the seat bounce, below 0 in a track's anticipation).</summary>
    public float Position { get; }

    /// <summary>A lift's squash: px of height (1080p) its top comes down, its base staying down.</summary>
    public float Squash { get; }

    /// <summary>The secondary sway toward the mat (px at 1080p; the intercom's mic).</summary>
    public float Sway { get; }

    /// <summary>The contact shadow's alpha (a track's travels with its base, pressing darker at the peak; a lift's stays at its rest footprint).</summary>
    public float Shadow { get; }

    /// <summary>The contact shadow's blur (px at 1080p; a track's tightens at the peak).</summary>
    public float ShadowBlur { get; }

    /// <summary>How much wider than its rest footprint the contact shadow is (a factor; a lift's widens as it rises).</summary>
    public float ShadowWiden { get; }
}

/// <summary>
/// The desk's slide-in assist (desk v3; Saleh: "yes but they need to slide";
/// D2: "I want the mouse to auto summon the machine when the player goes near
/// it the machine will slide in a little bit to help the player"; UNITY_PLAN
/// 5.2, DESK-HOMES P2). Each frame (Step) the desk tells it every attached
/// slider as it finds it (SlideCandidate: present, in hand, busy, where the
/// cursor is) and whether a paper is dragged, and it decides:
/// <list type="bullet">
/// <item><b>Nearest wins, one at a time</b> (Pick): the front-most silhouette
/// under the cursor wins at once, whatever is out; else a home footprint
/// under the cursor's desk point (the one out keeps it if it is one of them);
/// else the nearest within the trigger radius, the one out keeping it unless
/// another is nearer by more than the hysteresis. A busy machine takes part:
/// the cursor on it keeps every other one home.</item>
/// <item><b>Hand-off:</b> the loser retracts at once (ease-in-out, no linger,
/// no seat) and the winner waits until the loser is nearly seated (within
/// winnerBelow of its travel of home, either side: a loser caught behind
/// home in its anticipation counts by how far it is from home), never
/// longer than winnerAfter; as the winner starts, the loser's last stretch
/// home takes the one frame of the winner's first move. A slider still on its ease-in home is hurried
/// (it arrives within winnerAfter, at speed, with no clack and no seat
/// bounce: a loser's way home) and one in its seat bounce rests first, so
/// one machine moves at a time.</item>
/// <item><b>Out:</b> the anticipation and its own spring, with one overshoot,
/// from wherever it stood. <b>Back:</b> after lingerSeconds outside the
/// trigger, the ease-in home that arrives at speed and the seat bounce on the
/// arrival frame.</item>
/// <item><b>Busy or occupied stays as it is:</b> a machine scanning, reading,
/// receiving a paper, or one a paper was let go on (dropHoldSeconds), stays
/// out (it never lingers home), stops where it stands if it was on its way
/// (Held), is never summoned from home and never loses a hand-off.</item>
/// <item><b>In hand:</b> it gives way at once as a hand-off loser. <b>Gone
/// from the desk:</b> home at once.</item>
/// <item><b>Forced retract</b> (Close: the shift is over): nothing is summoned
/// any more, every empty slider out retracts at once, a busy one only with
/// its docked paper riding it, else as soon as it is empty.</item>
/// <item><b>Reduced Motion</b> (or Motion intensity 0): every move is a
/// one-frame cut (no anticipation, overshoot, seat, squash or sway); the
/// travel never scales and the cues still sound.</item>
/// <item><b>Felt gap:</b> a track never goes past DeskSlider.Reach.</item>
/// <item><b>Nothing pops:</b> every channel carries on from where it stood
/// when a phase changes (the position, the squash, the sway, the shadow).</item>
/// </list>
/// It raises the frame's cues (Cues) and gives each slider's pose (Pose).
/// Pure; DeskSlideIn and MachineSlideIn apply it. Steps allocate nothing.
/// </summary>
public sealed class ProximityAssist
{
    private sealed class State
    {
        public DeskSlider Slider;
        public DeskHome Home;
        public SlidePhase Phase;
        public float T0, Start, LingerFrom;
        public State Loser;

        /// <summary>A way home hurried by a summoned winner: the ease-in's share at HurryFrom, finishing in HurryOver seconds, with no seat bounce.</summary>
        public bool Hurried;
        public float HurryFrom, HurryU, HurryOver;

        /// <summary>A loser the winner started on, on its last stretch home (under winnerBelow): home on the first step after this time, so its last stretch takes the one frame of the winner's first move.</summary>
        public float SeatBy = float.PositiveInfinity;

        /// <summary>The squash and sway it had when its last out ended (CarryAt): they release from there on their own clock; and the track shadow it had when its phase last changed, which its phase carries on from.</summary>
        public float SquashFrom, SwayFrom, CarryAt, ShadowFrom;

        public bool Busy, Taken, Rides, Forced;
        public float DroppedAt = float.NegativeInfinity;
    }

    private readonly DeskSlides _slides;
    private readonly List<State> _states = new List<State>();
    private readonly Dictionary<string, State> _byId = new Dictionary<string, State>();
    private readonly List<SlideCue> _cues = new List<SlideCue>();
    private readonly List<SlideCandidate> _eligible = new List<SlideCandidate>();
    private float _now;
    private bool _still, _dragging, _closed;
    private State _under, _picked;

    /// <summary>The assist over <paramref name="slides"/>' sliders that move where they rest on <paramref name="homes"/> (DeskSlider.MovesOn: a track whose home declares no slide stays put), every one at home.</summary>
    public ProximityAssist(DeskSlides slides, IReadOnlyList<DeskHome> homes)
    {
        _slides = slides ?? throw new ArgumentNullException(nameof(slides));
        foreach (DeskSlider slider in slides.sliders ?? Array.Empty<DeskSlider>())
        {
            if (slider == null || _byId.ContainsKey(slider.id))
                continue;
            DeskHome home = DeskHomeMap.Find(homes, slider.Home);
            if (!slider.MovesOn(home))
                continue;
            var state = new State { Slider = slider, Home = home };
            _states.Add(state);
            _byId.Add(slider.id, state);
        }
    }

    /// <summary>The cues the last Step raised, in order.</summary>
    public IReadOnlyList<SlideCue> Cues => _cues;

    /// <summary>
    /// The shift is over (DeskSlideIn hears DayOrchestrator.OnDayCompleted:
    /// the last traveller was finished, or the booth shut with nobody at the
    /// desk; DESK-HOMES P2's forced retract at the shift's end). From the
    /// next Step on nothing is summoned any more, and every slider out,
    /// waiting or held goes home at once as a hand-off loser (no linger, no
    /// seat; a cut under Reduced Motion), with the hand-off's sound; a busy
    /// one only when the paper docked in it rides it home
    /// (SlideCandidate.Rides: one mover carries both), else as soon as it is
    /// empty. Lasting: a new day reloads the office (a new assist).
    /// </summary>
    public void Close() => _closed = true;

    /// <summary>
    /// The slider that should be out for the cursor (null: none), among
    /// <paramref name="candidates"/> (<paramref name="current"/>: the one the
    /// assist has out now): the front-most silhouette under the cursor at
    /// once; else a home footprint under it (the current one if it is among
    /// them, else the first listed); else the nearest within
    /// <paramref name="trigger"/> (the current within trigger plus
    /// <paramref name="hysteresis"/>), the current keeping it unless another
    /// is nearer by more than the hysteresis. Allocates nothing.
    /// </summary>
    public static string Pick(IReadOnlyList<SlideCandidate> candidates, string current, float trigger, float hysteresis)
    {
        int count = candidates != null ? candidates.Count : 0;
        if (count == 0)
            return null;
        string front = null;
        int frontRank = int.MaxValue;
        for (int i = 0; i < count; i++)
        {
            SlideCandidate c = candidates[i];
            if (c.Over >= 0 && c.Over < frontRank)
            {
                front = c.Id;
                frontRank = c.Over;
            }
        }
        if (front != null)
            return front;

        string first = null;
        for (int i = 0; i < count; i++)
        {
            SlideCandidate c = candidates[i];
            if (!c.OnFootprint)
                continue;
            if (c.Id == current)
                return current;
            first ??= c.Id;
        }
        if (first != null)
            return first;

        string near = null;
        float nearest = float.MaxValue, currentDistance = -1f;
        for (int i = 0; i < count; i++)
        {
            SlideCandidate c = candidates[i];
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
    /// The rank of <paramref name="id"/>'s silhouette under the cursor
    /// (SlideCandidate.Over): among the first <paramref name="count"/> of
    /// <paramref name="owners"/> (the slider owning each click box the
    /// cursor's ray hits, nearest first; null for a box no slider owns), how
    /// many other sliders show in front of its nearest box (each counted
    /// once, however many of its boxes are hit); -1 when none of its boxes is
    /// hit. Allocates nothing.
    /// </summary>
    public static int Rank(IReadOnlyList<string> owners, int count, string id)
    {
        if (id == null || owners == null)
            return -1;
        count = Math.Min(count, owners.Count);
        int rank = 0;
        for (int i = 0; i < count; i++)
        {
            string owner = owners[i];
            if (owner == null)
                continue;
            if (owner == id)
                return rank;
            bool seen = false;
            for (int j = 0; j < i && !seen; j++)
                seen = owners[j] == owner;
            if (!seen)
                rank++;
        }
        return -1;
    }

    /// <summary>
    /// Advances the assist to <paramref name="now"/> (seconds, the game feel's
    /// clock) with every attached slider as the frame finds it
    /// (<paramref name="sliders"/>; one not listed is never summoned), whether
    /// the player drags a paper (<paramref name="dragging"/>: a paper let go
    /// over a slider holds it out while it lands) and whether every move is a
    /// cut (<paramref name="still"/>: Reduced Motion or Motion intensity 0).
    /// The frame's cues are in Cues. Allocates nothing.
    /// </summary>
    public void Step(float now, IReadOnlyList<SlideCandidate> sliders, bool dragging, bool still)
    {
        float was = _now;
        _now = now;
        _still = still;
        _cues.Clear();
        Read(sliders, dragging);

        // Busy stays as it is: out stays out; one on its way stops where the paper found it (as it was shown last frame).
        foreach (State s in _states)
        {
            if (!s.Busy || s.Forced)
                continue;
            switch (s.Phase)
            {
                case SlidePhase.Linger:
                    s.Phase = SlidePhase.Out;
                    break;
                case SlidePhase.Back:
                case SlidePhase.Retract:
                    Hold(s, PositionAt(s, was));
                    break;
                case SlidePhase.Wait:
                    Hold(s, s.Start);
                    break;
            }
        }

        if (_closed)
        {
            // The shift is over: nothing is picked, everything out goes home (Close).
            _picked = null;
            Retire();
        }
        else
        {
            State current = Current();
            string target = Pick(_eligible, current?.Slider.id, _slides.triggerPx, _slides.hysteresisPx);
            _picked = target != null ? _byId[target] : null;
            if (_picked != current)
            {
                // A hand-off: the loser gives way at once (a busy one never does). With no target, one out lingers (below); one still waiting to start never does.
                if (current != null && !current.Busy && (_picked != null || current.Phase == SlidePhase.Wait))
                    Lose(current);
                if (_picked != null && !_picked.Busy)
                    Summon(_picked);
            }
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
                    if (s != _picked && !s.Busy)
                    {
                        s.Phase = SlidePhase.Linger;
                        s.LingerFrom = now;
                    }
                    break;
                case SlidePhase.Linger:
                    if (s == _picked)
                        s.Phase = SlidePhase.Out;
                    else if (now - s.LingerFrom >= _slides.lingerSeconds)
                        GoHome(s);
                    break;
                case SlidePhase.Back:
                    if (s.Hurried)
                    {
                        // A hand-off loser's way home: no clack (slides.json, the hand-off: "no seat clack, the winner's unlatch tick covers it").
                        if (still || now - s.HurryFrom >= s.HurryOver || now > s.SeatBy)
                            Settle(s);
                        break;
                    }
                    if (!still && Crossed(before, t, _slides.backSeconds))
                        _cues.Add(new SlideCue(s.Slider.id, SlideCueKind.Seat));
                    if (still || t >= _slides.backSeconds + _slides.seatSeconds)
                        Settle(s);
                    break;
                case SlidePhase.Retract:
                    if (still || t >= _slides.retractSeconds || now > s.SeatBy)
                        Settle(s);
                    break;
                case SlidePhase.Held:
                    // Empty again (one the cursor picked was summoned above, from where it stood): it goes home from there.
                    if (s.Busy)
                        break;
                    if (Math.Abs(s.Start) < 1e-4f)
                        Settle(s);
                    else
                        GoHome(s);
                    break;
            }
        }

        foreach (State s in _states)
            if (s.Phase == SlidePhase.Wait && Seated(s))
                Begin(s, s.Start);
    }

    /// <summary>
    /// The pose of <paramref name="id"/> now: its position; a lift's squash
    /// over its anticipation; the sway after the peak; its contact shadow (a
    /// track's leaves the well with the base and fades in, presses at the
    /// peak, fades out as it re-enters; a lift's stays at the rest footprint,
    /// fading and widening as it rises; pressed for peakTightSeconds from the
    /// peak's frame on). Each carries on from where it stood when the phase
    /// changed. Under Reduced Motion: the position and a still shadow,
    /// nothing else. An unknown or unmoving slider is at rest.
    /// </summary>
    public SlidePose Pose(string id)
    {
        if (id == null || !_byId.TryGetValue(id, out State s))
            return new SlidePose(SlidePhase.Rest, 0f, 0f, 0f, 0f, 0f, 1f);
        DeskSlider slider = s.Slider;
        float f = PositionAt(s, _now);
        (float squash, float sway, float trackShadow) = Channels(s);
        if (slider.kind == SlideKind.Lift)
        {
            (float alpha, float blur, float widen) = SlideCurve.LiftShadow(_slides, slider, f);
            return new SlidePose(s.Phase, f, squash, sway, alpha, blur, widen);
        }
        float shadowBlur = _slides.trackShadowBlurPx, sincePeak = _now - s.T0 - slider.peakSeconds;
        bool tight = !_still && (s.Phase == SlidePhase.Out || s.Phase == SlidePhase.Linger) && sincePeak >= 0f && sincePeak < _slides.peakTightSeconds;
        if (tight)
        {
            shadowBlur *= _slides.peakTightBlur;
            trackShadow = Math.Min(1f, trackShadow * _slides.peakTightDarken);
        }
        return new SlidePose(s.Phase, f, squash, sway, trackShadow, shadowBlur, 1f);
    }

    /// <summary>
    /// The forced retract, each step once the shift is over (Close; DESK-HOMES
    /// P2, UNITY_PLAN 5.2): every slider out, waiting or held retracts at
    /// once as a hand-off loser (no linger, no seat; a cut under Reduced
    /// Motion). A busy one goes only when its docked paper rides it home (one
    /// mover carries both, and nothing holds it on its way); otherwise it
    /// stays out until it is empty, and goes then.
    /// </summary>
    private void Retire()
    {
        foreach (State s in _states)
        {
            if (s.Phase != SlidePhase.Out && s.Phase != SlidePhase.Linger && s.Phase != SlidePhase.Wait && s.Phase != SlidePhase.Held)
                continue;
            if (s.Busy && !s.Rides)
                continue;
            bool riding = s.Busy;
            Lose(s);
            s.Forced = riding && s.Phase == SlidePhase.Retract;
        }
    }

    /// <summary>Takes the frame's facts: a paper let go lands on the slider that was under the cursor (the drop hold); a slider gone from the desk is home at once, one taken in hand gives way once; the busy ones and what rides them; the candidates for the pick.</summary>
    private void Read(IReadOnlyList<SlideCandidate> sliders, bool dragging)
    {
        if (_dragging && !dragging && _under != null)
            _under.DroppedAt = _now;
        _dragging = dragging;
        _under = null;
        int underRank = int.MaxValue;
        _eligible.Clear();
        foreach (State s in _states)
            s.Busy = s.Rides = false;
        int count = sliders != null ? sliders.Count : 0;
        for (int i = 0; i < count; i++)
        {
            SlideCandidate c = sliders[i];
            if (c.Id == null || !_byId.TryGetValue(c.Id, out State s))
                continue;
            if (!c.Present)
            {
                s.Taken = false;
                if (s.Phase != SlidePhase.Rest)
                    Cut(s);
                continue;
            }
            if (c.Taken)
            {
                if (!s.Taken && (s.Phase == SlidePhase.Out || s.Phase == SlidePhase.Linger || s.Phase == SlidePhase.Wait))
                    Lose(s);
                s.Taken = true;
                continue;
            }
            s.Taken = false;
            s.Busy = c.Busy || _now - s.DroppedAt < _slides.dropHoldSeconds;
            s.Rides = c.Rides;
            if (c.Over >= 0 && c.Over < underRank)
            {
                _under = s;
                underRank = c.Over;
            }
            _eligible.Add(c);
        }
    }

    /// <summary>The slider the assist has out for the cursor: the first, in the data's order, that is out, lingering or waiting and not busy; else a busy one that is out (it keeps the cursor by the hysteresis as any other).</summary>
    private State Current()
    {
        State busyOut = null;
        foreach (State s in _states)
        {
            if (s.Phase != SlidePhase.Out && s.Phase != SlidePhase.Linger && s.Phase != SlidePhase.Wait)
                continue;
            if (!s.Busy)
                return s;
            busyOut ??= s;
        }
        return busyOut;
    }

    /// <summary>The hand-off's loser: one waiting at home goes back to rest; any other retracts at once, from where it is.</summary>
    private void Lose(State s)
    {
        float from = PositionAt(s, _now);
        if (s.Phase == SlidePhase.Wait && Math.Abs(from) < 1e-4f)
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
        Change(s, SlidePhase.Retract, from);
    }

    /// <summary>
    /// The cursor's pick: one already out stays out; else it starts from
    /// where it stands, or waits while another slider is still on its way
    /// home: one on its ease-in is hurried home (no seat bounce), one in its
    /// seat bounce rests first, and one already on its last stretch (nearly
    /// seated) takes that stretch on the next step, the winner starting once
    /// it is home (a cursor darting back and forth never chains two machines
    /// moving together).
    /// </summary>
    private void Summon(State s)
    {
        if (s.Phase == SlidePhase.Out || s.Phase == SlidePhase.Linger)
        {
            s.Phase = SlidePhase.Out;
            return;
        }
        float from = PositionAt(s, _now);
        State blocker = null, finishing = null;
        float furthest = float.NegativeInfinity;
        foreach (State other in _states)
        {
            if (other == s)
                continue;
            if (other.Phase == SlidePhase.Back && !other.Hurried && _now - other.T0 < _slides.backSeconds)
                Hurry(other);
            if (other.Phase != SlidePhase.Back && other.Phase != SlidePhase.Retract)
                continue;
            bool seating = other.Phase == SlidePhase.Back && !other.Hurried;
            float at = PositionAt(other, _now);
            if (!seating && NearlySeated(at))
            {
                other.SeatBy = Math.Min(other.SeatBy, _now);
                finishing ??= other;
                continue;
            }
            if (blocker == null || Math.Abs(at) > furthest)
            {
                blocker = other;
                furthest = Math.Abs(at);
            }
        }
        if (blocker == null && finishing == null)
        {
            Begin(s, from);
            return;
        }
        Change(s, SlidePhase.Wait, from);
        s.Loser = blocker ?? finishing;
    }

    /// <summary>A slider on its ease-in home gives way to a summoned winner: what is left of its way home is played within winnerAfter (only ever faster), it arrives at speed and rests with no seat bounce and no clack (a hand-off loser's way home).</summary>
    private void Hurry(State s)
    {
        float t = _now - s.T0, tb = _slides.backSeconds;
        s.Hurried = true;
        s.HurryFrom = _now;
        s.HurryU = Clamp01(t / tb);
        s.HurryOver = Math.Max(1e-4f, Math.Min(tb - t, _slides.winnerAfter));
    }

    /// <summary>True when the winner <paramref name="waiter"/> may start: its loser is home or out again, nearly seated (NearlySeated; one already taking its last stretch once it is home; one in its seat bounce once it rests), or the winner has waited winnerAfter.</summary>
    private bool Seated(State waiter)
    {
        State loser = waiter.Loser;
        if (loser == null || (loser.Phase != SlidePhase.Back && loser.Phase != SlidePhase.Retract))
            return true;
        if (_now - waiter.T0 >= _slides.winnerAfter - 1e-5f)
            return true;
        if ((loser.Phase == SlidePhase.Back && !loser.Hurried) || !float.IsPositiveInfinity(loser.SeatBy))
            return false;
        return NearlySeated(PositionAt(loser, _now));
    }

    /// <summary>True when a hand-off loser at <paramref name="f"/> of its travel no longer keeps the winner waiting: within winnerBelow of home, either side (one caught behind home in its anticipation counts by how far it still is from home, never as seated because it is behind it).</summary>
    private bool NearlySeated(float f) => Math.Abs(f) < _slides.winnerBelow;

    /// <summary>
    /// The slide out starts from <paramref name="from"/>: the unlatch's tick
    /// (and, as a cut, the peak's). One machine moves at a time: a loser
    /// still on its last stretch home (under winnerBelow, which let the
    /// winner start) is home on the next step, its last stretch in the one
    /// frame of the winner's first move, never several frames of the two
    /// moving together (the ease-in-out's slow tail).
    /// </summary>
    private void Begin(State s, float from)
    {
        Change(s, SlidePhase.Out, from);
        _cues.Add(new SlideCue(s.Slider.id, SlideCueKind.Unlatch));
        if (_still)
            _cues.Add(new SlideCue(s.Slider.id, SlideCueKind.Peak));
        foreach (State other in _states)
            if (other != s && (other.Phase == SlidePhase.Retract || (other.Phase == SlidePhase.Back && other.Hurried)) && NearlySeated(PositionAt(other, _now)))
                other.SeatBy = Math.Min(other.SeatBy, _now);
    }

    /// <summary>The linger (or the hold) is over: home with the reversed shush (as a cut, the seat's clack at once).</summary>
    private void GoHome(State s)
    {
        if (_still)
        {
            Settle(s);
            _cues.Add(new SlideCue(s.Slider.id, SlideCueKind.Seat));
            return;
        }
        Change(s, SlidePhase.Back, PositionAt(s, _now));
        _cues.Add(new SlideCue(s.Slider.id, SlideCueKind.Back));
    }

    /// <summary>It stops where it stands (<paramref name="at"/>): busy on its way.</summary>
    private void Hold(State s, float at) => Change(s, SlidePhase.Held, at);

    /// <summary>At home; what it carried (a shadow left out by a hurried or forced way home) fades from there.</summary>
    private void Settle(State s) => Change(s, SlidePhase.Rest, 0f);

    /// <summary>At home at once with nothing carried: it left the desk.</summary>
    private void Cut(State s)
    {
        Settle(s);
        s.SquashFrom = s.SwayFrom = s.ShadowFrom = 0f;
    }

    /// <summary>The one way a phase begins: what its squash, sway and shadow show now carries on into it (so nothing pops; an out's squash and sway release from here on their own clock, one already releasing keeps its clock), its clock starts now from <paramref name="from"/>.</summary>
    private void Change(State s, SlidePhase phase, float from)
    {
        (float squash, float sway, float shadow) = Channels(s);
        if (s.Phase == SlidePhase.Out || s.Phase == SlidePhase.Linger)
        {
            s.SquashFrom = squash;
            s.SwayFrom = sway;
            s.CarryAt = _now;
        }
        s.ShadowFrom = shadow;
        s.Phase = phase;
        s.T0 = _now;
        s.Start = from;
        s.Loser = null;
        s.Hurried = false;
        s.Forced = false;
        s.SeatBy = float.PositiveInfinity;
    }

    /// <summary>
    /// Its squash, sway and track shadow now: what its last out left releasing
    /// (the squash over squashRelease, the sway over its decay, from CarryAt);
    /// out, its own on top (a lift's squash, the sway after the peak, the
    /// track shadow fading in); the shadow by its phase from the alpha it
    /// carried in (going home, giving way, waiting, held; at home, fading).
    /// Under Reduced Motion no squash or sway, and the shadow shows while it
    /// is out.
    /// </summary>
    private (float squash, float sway, float shadow) Channels(State s)
    {
        DeskSlider k = s.Slider;
        float t = _now - s.T0;
        if (_still)
            return (0f, 0f, k.kind == SlideKind.Track && PositionAt(s, _now) > 0f ? _slides.trackShadowAlpha : 0f);
        float squash = SlideCurve.Fade(s.SquashFrom, _now - s.CarryAt, _slides.squashRelease);
        float sway = SlideCurve.Fade(s.SwayFrom, _now - s.CarryAt, k.swayDecay);
        float shadow;
        switch (s.Phase)
        {
            case SlidePhase.Out:
            case SlidePhase.Linger:
                if (k.kind == SlideKind.Lift)
                    squash = Math.Max(squash, SlideCurve.Squash(_slides, t));
                sway += SlideCurve.Sway(k, t - k.peakSeconds);
                shadow = SlideCurve.TrackShadowOut(_slides, t, s.ShadowFrom);
                break;
            case SlidePhase.Back:
                shadow = s.Hurried ? s.ShadowFrom : SlideCurve.TrackShadowBack(_slides, t, s.ShadowFrom);
                break;
            case SlidePhase.Retract:
                shadow = SlideCurve.TrackShadowRetract(_slides, t, s.ShadowFrom);
                break;
            case SlidePhase.Wait:
            case SlidePhase.Held:
                shadow = s.ShadowFrom;
                break;
            default:
                shadow = SlideCurve.Fade(s.ShadowFrom, t, _slides.trackShadowOut);
                break;
        }
        return (squash, sway, k.kind == SlideKind.Track ? shadow : 0f);
    }

    /// <summary>Where <paramref name="s"/> stands at <paramref name="at"/> in its current phase, never past its felt gap.</summary>
    private float PositionAt(State s, float at)
    {
        float f;
        float t = at - s.T0;
        switch (s.Phase)
        {
            case SlidePhase.Out:
            case SlidePhase.Linger:
                f = _still ? 1f : SlideCurve.OutFrom(_slides, s.Slider, t, s.Start);
                break;
            case SlidePhase.Wait:
            case SlidePhase.Held:
                f = s.Start;
                break;
            case SlidePhase.Back:
                if (_still)
                    f = 0f;
                else if (s.Hurried)
                    f = SlideCurve.WayHome(s.Start, s.HurryU + (1f - s.HurryU) * Clamp01((at - s.HurryFrom) / s.HurryOver));
                else
                    f = SlideCurve.Back(_slides, s.Slider, t, s.Start);
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

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}
