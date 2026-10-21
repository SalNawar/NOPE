using System;
using System.Collections.Generic;

/// <summary>How a desk object moves when the cursor comes near it (desk v3, Saleh: "yes but they need to slide"). Serialized: append only.</summary>
public enum SlideKind
{
    /// <summary>It never moves (a decision object: DETAIN, AVAILABLE, the bell; the deal tray).</summary>
    Fixed = 0,

    /// <summary>It slides along its felt track toward the mat (the scanner, the chip reader, the intercom, the BABEL tuner), the way its home declares (DeskHome.slide).</summary>
    Track = 1,

    /// <summary>It rises in place out of a fixed pouch, stand, rack or pocket (the UV torch, the glasses, the daters, the rulebook, the Rolodex, the protocol cards).</summary>
    Lift = 2,
}

/// <summary>
/// One slider of the desk (a row of render/slides.json, written by the desk
/// pipeline's layout.SLIDES and slide_gate.py): which object, how it moves,
/// how far and its own spring, solved offline from the screen-px minimums
/// (an anticipation and an overshoot of about 3 px on the machines, capped at
/// 20 % of the travel or by the felt the track leaves). A track has no
/// direction of its own: it slides the way its home declares (DeskHome.slide,
/// DESK-PLAN's map; UNITY_PLAN 5.3: the axis is HomeOf(id)'s). Pure and
/// serializable (a row of DeskSlides.sliders).
/// </summary>
[Serializable]
public sealed class DeskSlider
{
    /// <summary>The slider's id: a home's id (DeskHomeMap) or, for an object that lies in another's home, its own ("stamp_approve" in the stamp rack, "rulebook" in its pocket).</summary>
    public string id = string.Empty;

    /// <summary>The home it rests in (DeskHomeMap's ids; empty: the id itself; none for a fixed object that has no home).</summary>
    public string home = string.Empty;

    /// <summary>How it moves.</summary>
    public SlideKind kind;

    /// <summary>How far it travels to its slid spot (metres; a lift: how high it rises).</summary>
    public float travel;

    /// <summary>A track: how far the felt recess runs past the rest footprint on the mat side (metres; painted in the plate).</summary>
    public float trackCut;

    /// <summary>A track: the felt showing between its silhouette and the track's inner brass lip at rest (metres; track_check.py; 0: not measured yet, no clamp).</summary>
    public float feltAtRest;

    /// <summary>How many screen pixels its anchor moves over the whole travel (1920x1080, the office pose): the px the springs were solved in.</summary>
    public float travelPx;

    /// <summary>The spring as fractions of the travel: the anticipation (a track backs away from the mat first; a lift has none), the overshoot past the slid spot and the seat bounce off the home stop on the way back.</summary>
    public float anticipation, overshoot, seat;

    /// <summary>The spring's damping ratio (below 1: one visible overshoot).</summary>
    public float damping;

    /// <summary>When the overshoot peaks, in seconds after the trigger frame.</summary>
    public float peakSeconds;

    /// <summary>A lift's tip as it rises (degrees at the slid spot) and the desk-frame direction its top tips toward (the glasses: 7 degrees toward the chair).</summary>
    public float tipDegrees, tipX, tipY;

    /// <summary>The secondary motion at the stop (the intercom's mic sways toward the mat on its gooseneck): px at 1080p, its frequency (Hz) and its decay (seconds); 0 px: none.</summary>
    public float swayPx, swayHz, swayDecay;

    /// <summary>An empty slider (for the serializer).</summary>
    public DeskSlider()
    {
    }

    /// <summary>A slider <paramref name="id"/> resting in <paramref name="home"/> that moves as <paramref name="kind"/> by <paramref name="travel"/> metres.</summary>
    public DeskSlider(string id, string home, SlideKind kind, float travel)
    {
        this.id = id ?? string.Empty;
        this.home = home ?? string.Empty;
        this.kind = kind;
        this.travel = travel;
    }

    /// <summary>The home it rests in (its id when none is named).</summary>
    public string Home => string.IsNullOrEmpty(home) ? id : home;

    /// <summary>True when its data moves it at all.</summary>
    public bool Moves => kind != SlideKind.Fixed && travel > 0f;

    /// <summary>The spring's position at its overshoot peak (1 + the overshoot).</summary>
    public float Peak => 1f + overshoot;

    /// <summary>
    /// A track's direction along the desk's x toward the mat, as its
    /// <paramref name="restsIn"/> home declares it (DeskHome.slide: -1 Left,
    /// +1 Right); 0 for a lift, a fixed object, no home or a home that
    /// declares no slide.
    /// </summary>
    public float Way(DeskHome restsIn)
    {
        if (kind != SlideKind.Track || restsIn == null)
            return 0f;
        return restsIn.slide == HomeSlide.Left ? -1f : restsIn.slide == HomeSlide.Right ? 1f : 0f;
    }

    /// <summary>
    /// True when it moves on the desk where it rests in
    /// <paramref name="restsIn"/>: its data moves it, and a track's home
    /// declares its slide (a track whose home declares none stays put until
    /// DESK-PLAN's map gives it one: the intercom on today's map).
    /// </summary>
    public bool MovesOn(DeskHome restsIn) => Moves && restsIn != null && (kind == SlideKind.Lift || Way(restsIn) != 0f);

    /// <summary>
    /// How far along its axis it may ever go (metres): a track keeps
    /// <paramref name="feltGap"/> of felt showing before its inner brass lip
    /// (the felt-gap clearance; feltAtRest less the gap), so the base never
    /// meets the lip; unbounded for a lift or a track not measured yet.
    /// </summary>
    public float Reach(float feltGap) => kind == SlideKind.Track && feltAtRest > 0f ? feltAtRest - feltGap : float.PositiveInfinity;

    /// <summary>The spring's own frequency (radians a second): its first peak lands at peakSeconds after an anticipation of <paramref name="anticipationSeconds"/>.</summary>
    public float DampedFrequency(float anticipationSeconds) => peakSeconds > anticipationSeconds ? (float)Math.PI / (peakSeconds - anticipationSeconds) : 0f;

    /// <summary>The damping ratio a first peak of overshoot after an anticipation of anticipation gives (the offline solver's rule; DeskSlides.Problems checks the stored ratio against it).</summary>
    public float DampingFromOvershoot()
    {
        if (overshoot <= 0f)
            return 1f;
        double r = Math.Log(overshoot / (1.0 + anticipation));
        return (float)(-r / Math.Sqrt(Math.PI * Math.PI + r * r));
    }
}

/// <summary>
/// The desk's slide-in (Saleh on desk v3, 2026-10-20: "yes but they need to
/// slide"; D2: "the machine will slide in a little bit to help the player";
/// D6: "when they slide in, we subtly zoom in a bit"; UNITY_PLAN section 5,
/// DESK-HOMES): the trigger (a screen radius around the silhouette, with
/// hysteresis), the timings (out: an anticipation then each slider's own
/// spring; back: after a linger, an ease-in that arrives at speed and the
/// seat bounce on the arrival frame; the hand-off loser's quick retract and
/// when the winner may start), the clearances the gate holds (felt and
/// lamps), a lift's squash, the contact shadows, the cues' sound ids, the D6
/// framing focus and every slider (Approved: render/slides.json of the desk
/// pipeline, the numbers as the offline gate passed them). Pure and
/// serializable: a section of the desk's plan (DeskLayoutSettings.slides);
/// ProximityAssist runs it, SlideCurve shapes it, SlideFocus frames it.
/// </summary>
[Serializable]
public sealed class DeskSlides
{
    /// <summary>The trigger: px around a slider's on-screen silhouette at referenceHeight (UNITY_PLAN 5.2: 120 px at A1), scaled by the screen's height.</summary>
    public float triggerPx = 120f;

    /// <summary>The extra px the slider already out keeps the slide by (hysteresis), and by which another must be nearer to take it.</summary>
    public float hysteresisPx = 40f;

    /// <summary>The screen height the px are measured at (1080p).</summary>
    public float referenceHeight = 1080f;

    /// <summary>Out: the slide's time (seconds; bible C6: 250 ms) and its anticipation's (the base unlatches and backs away from the mat; a lift squashes instead).</summary>
    public float outSeconds = 0.25f, anticipationSeconds = 0.04f;

    /// <summary>Back: how long the cursor stays outside the trigger before it goes home (seconds), the way home (an ease-in that arrives at full speed) and the seat bounce that starts on the arrival frame.</summary>
    public float lingerSeconds = 0.15f, backSeconds = 0.30f, seatSeconds = 0.09f;

    /// <summary>The hand-off: the loser eases home in retractSeconds (ease-in-out, no linger, no seat); the winner starts once the loser is under winnerBelow of its travel (nearly seated) or after winnerAfter seconds, so one machine moves at a time.</summary>
    public float retractSeconds = 0.13f, winnerAfter = 0.13f, winnerBelow = 0.1f;

    /// <summary>
    /// How long a machine a paper was let go on stays out as if busy (seconds;
    /// DESK-HOMES P2, "busy or occupied stays out"): the paper's way onto its
    /// bed, pad or slot, until the machine's own busy state (the scan, the
    /// read) takes over, so the cursor leaving right after the drop never
    /// sends it home under a landing paper.
    /// </summary>
    public float dropHoldSeconds = 1f;

    /// <summary>How far the scanner's lid must stand open (degrees from shut) to hold the scanner as busy: a paper is on its way onto the bed, or a scan just ended and the next queued paper may follow (OfficeSceneBinder's stand-in read, SlideOccupancy).</summary>
    public float lidOpenDegrees = 0.5f;

    /// <summary>The felt a track keeps in front of its inner brass lip at the overshoot peak: metres and screen px at 1080p (both must hold).</summary>
    public float feltGap = 0.004f, feltGapPx = 3f;

    /// <summary>The px every slider keeps clear of every lamp housing settled and at its peak (a pair already closer at rest must not get closer; a lamp nearer the camera is an occluder, not a contact).</summary>
    public float lampGapPx = 3f;

    /// <summary>A lift's anticipation: it squashes up to squashPx of height over the anticipation (its base stays down) and releases over squashRelease seconds as it rises.</summary>
    public float squashPx = 1.5f, squashRelease = 0.06f;

    /// <summary>A lift's contact shadow left at its rest footprint: its alpha, how much it fades by the top, its blur (px, plus per px of lift) and how much wider it gets per px of lift.</summary>
    public float liftShadowAlpha = 0.85f, liftShadowFade = 0.35f, liftShadowBlurPx = 1f, liftShadowBlurPerPx = 0.3f, liftShadowWidenPerPx = 0.025f;

    /// <summary>A track's contact shadow: it leaves the well with the base, fading in to trackShadowAlpha over trackShadowIn seconds from the anticipation's end, and fades out over trackShadowOut seconds as the base re-enters its well; at the overshoot peak it tightens for peakTightSeconds.</summary>
    public float trackShadowAlpha = 0.35f, trackShadowIn = 0.08f, trackShadowOut = 0.06f, peakTightSeconds = 0.025f;

    /// <summary>A track's contact shadow's blur (px at 1080p; slides.json names none, so the lift's rest blur).</summary>
    public float trackShadowBlurPx = 1f;

    /// <summary>The press at the overshoot peak (slides.json's cue: "blur -30%, 10% darker"): the blur's and the alpha's factors while the pose is tight.</summary>
    public float peakTightBlur = 0.7f, peakTightDarken = 1.1f;

    /// <summary>How high the contact shadow lies over the desk under its object (metres), so it never fights the desk's own surface.</summary>
    public float shadowLift = 0.001f;

    /// <summary>The cues' sound ids (the sound list; a cue the bank has no clip for plays its stand-in): the unlatch tick on the trigger frame, the felt shush when the base moves, the knock at the peak, the reversed shush going home (and the hand-off loser's), the clack on the seat.</summary>
    public string unlatchCue = "machine_slide_unlatch", slideCue = "machine_slide", stopCue = "machine_slide_stop", backCue = "machine_slide_back", seatCue = "machine_seat";

    /// <summary>The D6 framing focus (DESK-HOMES P3; Q5's default: the reading view only): it starts focusDwellSeconds after the cursor's slider has finished its slide, at most once every focusEverySeconds, and re-aims to the next slider without letting go in between.</summary>
    public float focusDwellSeconds = 0.15f, focusEverySeconds = 0.8f;

    /// <summary>The focus's budgets as shares of the reading view's bound field of view: its narrowing (focusShare, 3 %), the push-in and the focus together (focusCap, 8 %), and its aim toward the machine (focusAim, 3 % of the frame).</summary>
    public float focusShare = 0.03f, focusCap = 0.08f, focusAim = 0.03f;

    /// <summary>Every slider (render/slides.json).</summary>
    public DeskSlider[] sliders;

    /// <summary>The approved slides (the serializer's default: ApprovedSliders).</summary>
    public DeskSlides()
    {
        sliders = ApprovedSliders();
    }

    /// <summary>The slider <paramref name="id"/>, or null.</summary>
    public DeskSlider Find(string id)
    {
        if (string.IsNullOrEmpty(id) || sliders == null)
            return null;
        foreach (DeskSlider slider in sliders)
            if (slider != null && slider.id == id)
                return slider;
        return null;
    }

    /// <summary>
    /// The obstacle <paramref name="restsIn"/>'s objects make while they may
    /// be out (DeskObstacles: a carried thing rises over it and never needs a
    /// live read; UNITY_PLAN 5.3, each slider's swept box): the home's
    /// footprint swept to each of its tracks' overshoot peak the way the home
    /// declares, and <paramref name="height"/> raised by the highest peak of
    /// the lifts resting in it (the stamp rack by its daters, the pocket by
    /// the rulebook, a pouch by its torch).
    /// </summary>
    public (DeskRect footprint, float height) Obstacle(DeskHome restsIn, float height)
    {
        if (restsIn == null)
            throw new ArgumentNullException(nameof(restsIn));
        DeskRect footprint = restsIn.Footprint, swept = footprint;
        float top = height;
        foreach (DeskSlider slider in sliders ?? Array.Empty<DeskSlider>())
        {
            if (slider == null || slider.Home != restsIn.id || !slider.MovesOn(restsIn))
                continue;
            float reach = Math.Min(slider.travel * slider.Peak, slider.Reach(feltGap));
            if (slider.kind == SlideKind.Lift)
                top = Math.Max(top, height + reach);
            else
                swept = DeskRect.Union(swept, new DeskRect(footprint.CentreX + slider.Way(restsIn) * reach, footprint.CentreY, footprint.Width, footprint.Height));
        }
        return (swept, top);
    }

    /// <summary>
    /// The felt-gap clearance on a measure (the gate's rule, UNITY_PLAN 5.1;
    /// track_check.py offline, the QA rig's slide gate on the real meshes):
    /// the felt still showing between a track's silhouette and its inner brass
    /// lip at the overshoot peak, <paramref name="metres"/> and
    /// <paramref name="px"/> at 1080p, keeps at least feltGap and feltGapPx.
    /// </summary>
    public bool FeltHolds(float metres, float px) => metres >= feltGap - 1e-5f && px >= feltGapPx;

    /// <summary>
    /// The lamp clearance on a measure (the gate's rule, UNITY_PLAN 4.3): a
    /// slider keeps lampGapPx clear px of a lamp's housing settled and at its
    /// peak (<paramref name="settled"/>, <paramref name="peak"/>), or, for a
    /// pair already closer than that at <paramref name="rest"/>, gets no
    /// closer; a lamp nearer the camera than the slider
    /// (<paramref name="lampInFront"/>) is an occluder the depth test draws
    /// over it, not a contact.
    /// </summary>
    public bool LampHolds(float rest, float settled, float peak, bool lampInFront)
    {
        float closest = Math.Min(settled, peak);
        return lampInFront || closest >= lampGapPx || closest >= rest;
    }

    /// <summary>
    /// What is wrong with the sliders against the desk home map
    /// <paramref name="homes"/> (one line each; empty when they hold): an id
    /// twice; a moving slider with no home in the map; a track that slides
    /// further than <paramref name="slideReach"/> or whose overshoot peak
    /// passes its felt gap (Reach) or leaves its painted track (trackCut); a
    /// home that declares a slide no track of it slides; a spring that does
    /// not overshoot once, peaks before its anticipation ends or whose damping
    /// disagrees with its overshoot; the timings' order (the hand-off winner
    /// may never wait longer than the retract); the focus's budgets (its
    /// narrowing and aim inside the cap). A track whose home declares no
    /// slide is not a fault: it stays put until the map declares one.
    /// </summary>
    public List<string> Problems(IReadOnlyList<DeskHome> homes, float slideReach)
    {
        var problems = new List<string>();
        var seen = new HashSet<string>();
        foreach (DeskSlider s in sliders ?? Array.Empty<DeskSlider>())
        {
            if (s == null || string.IsNullOrEmpty(s.id))
            {
                problems.Add("A slider has no id.");
                continue;
            }
            if (!seen.Add(s.id))
                problems.Add($"The slider '{s.id}' is listed twice.");
            if (!s.Moves)
                continue;
            if (DeskHomeMap.Find(homes, s.Home) == null)
                problems.Add($"The slider '{s.id}' rests in '{s.Home}', which is no home of the map.");
            if (s.damping <= 0f || s.damping >= 1f)
                problems.Add($"The slider '{s.id}' has a damping of {s.damping}: its spring must overshoot once (0 < damping < 1).");
            if (s.peakSeconds <= anticipationSeconds || s.peakSeconds >= outSeconds)
                problems.Add($"The slider '{s.id}' peaks at {s.peakSeconds} s: after its anticipation and inside the {outSeconds} s slide.");
            if (Math.Abs(s.DampingFromOvershoot() - s.damping) > 0.01f)
                problems.Add($"The slider '{s.id}' has a damping of {s.damping}, but its overshoot after its anticipation needs {s.DampingFromOvershoot():0.000}.");
            if (s.kind != SlideKind.Track)
                continue;
            if (s.travel > slideReach + 1e-4f)
                problems.Add($"The track '{s.id}' slides {s.travel * 100f:0.0} cm, further than {slideReach * 100f:0.0} cm.");
            if (s.travel * s.Peak > s.Reach(feltGap) + 1e-5f)
                problems.Add($"The track '{s.id}' overshoots to {s.travel * s.Peak * 100f:0.00} cm, past its felt gap at {s.Reach(feltGap) * 100f:0.00} cm.");
            if (s.travel * s.Peak > s.trackCut + 1e-5f)
                problems.Add($"The track '{s.id}' overshoots to {s.travel * s.Peak * 100f:0.00} cm, out of its {s.trackCut * 100f:0.0} cm painted track.");
        }
        foreach (DeskHome home in homes ?? Array.Empty<DeskHome>())
            if (home != null && home.slide != HomeSlide.None && !HasTrack(home.id))
                problems.Add($"The home '{home.id}' declares a slide but no track slides it.");
        if (winnerAfter > retractSeconds + 1e-5f)
            problems.Add($"The hand-off winner waits up to {winnerAfter} s, longer than the loser's {retractSeconds} s retract.");
        if (winnerBelow <= 0f || winnerBelow >= 0.5f)
            problems.Add($"The hand-off winner starts below {winnerBelow} of the loser's travel: it must be nearly seated (0 < winnerBelow < 0.5), so two sliders are never both half out.");
        if (focusShare < 0f || focusShare > focusCap || focusAim < 0f || focusAim > focusCap)
            problems.Add($"The framing focus narrows {focusShare} and aims {focusAim} of the view: each between 0 and its {focusCap} cap.");
        return problems;
    }

    /// <summary>True when a moving track rests in the home <paramref name="homeId"/>.</summary>
    private bool HasTrack(string homeId)
    {
        foreach (DeskSlider slider in sliders ?? Array.Empty<DeskSlider>())
            if (slider != null && slider.kind == SlideKind.Track && slider.Moves && slider.Home == homeId)
                return true;
        return false;
    }

    /// <summary>The approved slides (a fresh copy each call).</summary>
    public static DeskSlides Approved() => new DeskSlides();

    /// <summary>
    /// The sliders as the desk pipeline's gate passed them (render/slides.json,
    /// 2026-10-21, rereview 5): the scanner 4.1 cm and the chip reader 3.7 cm
    /// along their 6 cm tracks (the chip reader's overshoot keeps 3 px clear
    /// of its lamp), the intercom 2.5 cm and the BABEL tuner 3.5 cm, each the
    /// way its home declares; the UV torch, the glasses (tipping 7 degrees
    /// toward the chair), the Rolodex, the protocol cards, both daters and the
    /// rulebook rise in place; DETAIN, AVAILABLE, the bell and the deal tray
    /// never move. A fresh copy each call.
    /// </summary>
    public static DeskSlider[] ApprovedSliders() => new[]
    {
        Track(DeskHomeMap.Scanner, 0.041f, 0.060f, 0.0547f, 17.98f, 0.1836f, 0.1836f, 0.1224f, 0.510f, 0.1419f),
        Track(DeskHomeMap.ChipReader, 0.037f, 0.060f, 0.056f, 19.82f, 0.1665f, 0.1665f, 0.111f, 0.527f, 0.1752f),
        Sway(Track(DeskHomeMap.Intercom, 0.025f, 0.045f, 0.0376f, 9.31f, 0.2f, 0.2f, 0.2f, 0.495f, 0.182f), 4f, 7f, 0.16f),
        Track(DeskHomeMap.BabelTuner, 0.035f, 0.045f, 0f, 14.51f, 0.2f, 0.2f, 0.1516f, 0.495f, 0.1766f),
        Lift(DeskHomeMap.UvTorch, null, 0.015f, 7.43f, 0.2f, 0.2f, 0.456f, 0.1856f),
        Tip(Lift(DeskHomeMap.Glasses, null, 0.020f, 6.28f, 0.2f, 0.2f, 0.456f, 0.1889f), 7f, 0f, -1f),
        Lift(DeskHomeMap.Rolodex, null, 0.015f, 4.72f, 0.2f, 0.2f, 0.456f, 0.1962f),
        Lift(DeskHomeMap.ProtocolRing, null, 0.015f, 4.72f, 0.2f, 0.2f, 0.456f, 0.1962f),
        Lift("stamp_approve", DeskHomeMap.StampRack, 0.012f, 4.26f, 0.2f, 0.2f, 0.456f, 0.1996f),
        Lift("stamp_deny", DeskHomeMap.StampRack, 0.012f, 4.12f, 0.2f, 0.2f, 0.456f, 0.2008f),
        Lift("rulebook", DeskHomeMap.RulebookPocket, 0.020f, 9.09f, 0.075f, 0.2f, 0.636f, 0.2122f),
        new DeskSlider(DeskHomeMap.Detain, null, SlideKind.Fixed, 0f),
        new DeskSlider("available", null, SlideKind.Fixed, 0f),
        new DeskSlider(DeskHomeMap.Bell, null, SlideKind.Fixed, 0f),
        new DeskSlider(DeskHomeMap.DealTray, null, SlideKind.Fixed, 0f),
    };

    private static DeskSlider Track(string id, float travel, float cut, float felt, float px, float a, float ov, float seat, float damping, float peak) =>
        new DeskSlider(id, null, SlideKind.Track, travel)
        {
            trackCut = cut, feltAtRest = felt, travelPx = px, anticipation = a, overshoot = ov, seat = seat, damping = damping, peakSeconds = peak,
        };

    private static DeskSlider Lift(string id, string home, float travel, float px, float ov, float seat, float damping, float peak) =>
        new DeskSlider(id, home, SlideKind.Lift, travel)
        {
            travelPx = px, overshoot = ov, seat = seat, damping = damping, peakSeconds = peak,
        };

    private static DeskSlider Sway(DeskSlider s, float px, float hz, float decay)
    {
        s.swayPx = px;
        s.swayHz = hz;
        s.swayDecay = decay;
        return s;
    }

    private static DeskSlider Tip(DeskSlider s, float degrees, float x, float y)
    {
        s.tipDegrees = degrees;
        s.tipX = x;
        s.tipY = y;
        return s;
    }
}
