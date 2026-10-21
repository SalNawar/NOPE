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

    /// <summary>Metres its travel moves per screen px at 1080p (the px its springs, its squash, its sway and its shadow's blur are set in); 0 without a measured travel.</summary>
    public float MetresPerPx => travelPx > 0f ? travel / travelPx : 0f;

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
/// lamps: the felt gap), a lift's squash, the contact shadows, the cues'
/// sound ids and their stand-ins, and every slider (Approved:
/// render/slides.json of the desk pipeline, the numbers as the offline gate
/// passed them). Pure and serializable: a section of the desk's plan
/// (DeskLayoutSettings.slides); ProximityAssist runs it, SlideCurve shapes it.
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

    /// <summary>
    /// The drive-by guard (the slide review of the Unity capture: one sweep
    /// across the desk slid three machines in turn, which reads as popping;
    /// decision mine, not Saleh's rule): a slider the cursor is only near
    /// (inside its trigger, not on its silhouette or its home's footprint)
    /// takes over from one already out only after the cursor has stayed near
    /// it, slower than sweepPxPerSecond, for dwellSeconds; none is summoned
    /// by nearness alone while the cursor sweeps faster than that. On it, it
    /// still wins at once, and from a quiet desk the first one near a slow
    /// cursor still starts on the trigger frame.
    /// </summary>
    public float dwellSeconds = 0.07f;

    /// <inheritdoc cref="dwellSeconds"/>
    public float sweepPxPerSecond = 1200f;

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

    /// <summary>The felt a track keeps in front of its inner brass lip at the overshoot peak (metres; DeskSlider.Reach clamps the travel to it; the offline gate also held 3 px at 1080p, and the lamps 3 px clear: DeskSlidesTests reads both from slides.json).</summary>
    public float feltGap = 0.004f;

    /// <summary>A lift's anticipation: it squashes up to squashPx of height over the anticipation (its base stays down) and releases over squashRelease seconds as it rises.</summary>
    public float squashPx = 1.5f, squashRelease = 0.06f;

    /// <summary>A lift's contact shadow left at its rest footprint: its alpha, how much it fades by the top, its blur (px, plus per px of lift) and how much wider it gets per px of lift.</summary>
    public float liftShadowAlpha = 0.85f, liftShadowFade = 0.35f, liftShadowBlurPx = 1f, liftShadowBlurPerPx = 0.3f, liftShadowWidenPerPx = 0.025f;

    /// <summary>A track's contact shadow: it leaves the well with the base, fading in to trackShadowAlpha over trackShadowIn seconds from the anticipation's end, and fades out over trackShadowOut seconds as the base re-enters its well; from the overshoot peak's frame it tightens for peakTightSeconds (slides.json: "for 2 frames", two frames at 60 fps).</summary>
    public float trackShadowAlpha = 0.35f, trackShadowIn = 0.08f, trackShadowOut = 0.06f, peakTightSeconds = 2f / 60f;

    /// <summary>A track's contact shadow's blur (px at 1080p; slides.json names none, so the lift's rest blur).</summary>
    public float trackShadowBlurPx = 1f;

    /// <summary>The press at the overshoot peak (slides.json's cue: "blur -30%, 10% darker"): the blur's and the alpha's factors while the pose is tight.</summary>
    public float peakTightBlur = 0.7f, peakTightDarken = 1.1f;

    /// <summary>How high the contact shadow lies over the desk under its object (metres), so it never fights the desk's own surface.</summary>
    public float shadowLift = 0.001f;

    /// <summary>The cues' sound ids (the sound list; a cue the bank has no clip for plays its stand-in): the unlatch tick on the trigger frame, the felt shush when the base moves, the knock at the peak, the reversed shush going home (and the hand-off loser's), the clack on the seat.</summary>
    public string unlatchCue = "machine_slide_unlatch", slideCue = "machine_slide", stopCue = "machine_slide_stop", backCue = "machine_slide_back", seatCue = "machine_seat";

    /// <summary>
    /// What each cue plays until the sound list delivers its clip, for every
    /// slider (DESK-HOMES 7's fallback, through the house fallback: the bank's
    /// clip of the stand-in, else CueSounds' placeholder of it): the bank's
    /// paper slide for the shushes, the relay's clack low for the unlatch
    /// tick, the cartridge's thunk low for the knock at the peak, and the
    /// cover's snap for the seat's clack (drawer_close is no bank cue: the
    /// stamp tray plays its own clip).
    /// </summary>
    public string shushStandIn = "paper_slide", unlatchStandIn = "relay_clack", stopStandIn = "cartridge_in", seatStandIn = "detain_cover";

    /// <summary>The pitch each stand-in plays at (the unlatch tick "very low", the knock muted).</summary>
    public float unlatchPitch = 0.6f, stopPitch = 0.7f, seatPitch = 1f;

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

    /// <summary>True when something rests in <paramref name="restsIn"/> that slides or lifts there (DeskSlider.MovesOn): its obstacle is a swept box (Obstacle).</summary>
    public bool Sweeps(DeskHome restsIn)
    {
        if (restsIn == null)
            return false;
        foreach (DeskSlider slider in sliders ?? Array.Empty<DeskSlider>())
            if (slider != null && slider.Home == restsIn.id && slider.MovesOn(restsIn))
                return true;
        return false;
    }

    /// <summary>
    /// The obstacle <paramref name="restsIn"/>'s objects make while they may
    /// be out (UNITY_PLAN 5.3, each slider's swept box; DESK-HOMES section 6):
    /// the home's footprint swept to each of its tracks' overshoot peak the
    /// way the home declares, and <paramref name="height"/> (its objects'
    /// height at rest) raised by the highest peak of the lifts resting in it
    /// (the stamp rack by its daters, the pocket by the rulebook, a pouch by
    /// its torch). DeskLayout registers it (RefreshObstacles); a dragged paper
    /// keeps over it and a paper let go clears a track's swept strip
    /// (DeskLayout.TrySweptTop, SweptStrips, read by DeskController), so the
    /// carry never needs a live read of a machine that slides out under it.
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

    /// <summary><paramref name="px"/> on a screen <paramref name="screenHeight"/> px high, as px at referenceHeight: the trigger and the hysteresis are set at 1080p and scale with the screen's height (UNITY_PLAN 5.2).</summary>
    public float ToReference(float px, float screenHeight) => px * referenceHeight / Math.Max(1f, screenHeight);

    /// <summary>The sound of <paramref name="kind"/>: its id in the sound list, the stand-in that sounds until the list delivers that clip, and the stand-in's pitch (the hand-off loser's is the reversed shush, cut short with its retract).</summary>
    public (string id, string standIn, float pitch) Sound(SlideCueKind kind)
    {
        switch (kind)
        {
            case SlideCueKind.Unlatch:
                return (unlatchCue, unlatchStandIn, unlatchPitch);
            case SlideCueKind.Slide:
                return (slideCue, shushStandIn, 1f);
            case SlideCueKind.Peak:
                return (stopCue, stopStandIn, stopPitch);
            case SlideCueKind.Seat:
                return (seatCue, seatStandIn, seatPitch);
            default:
                return (backCue, shushStandIn, 1f);
        }
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
    /// may never wait longer than the retract). A track whose home declares
    /// no slide is not a fault: it stays put until the map declares one.
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
    /// toward the chair), the Rolodex, the protocol cards, both daters (tipping
    /// 5 degrees toward the hand: DaterTip) and the rulebook rise in place;
    /// DETAIN, AVAILABLE, the bell and the deal tray
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
        Tip(Lift("stamp_approve", DeskHomeMap.StampRack, 0.012f, 4.26f, 0.2f, 0.2f, 0.456f, 0.1996f), DaterTip, 0f, -1f),
        Tip(Lift("stamp_deny", DeskHomeMap.StampRack, 0.012f, 4.12f, 0.2f, 0.2f, 0.456f, 0.2008f), DaterTip, 0f, -1f),
        Lift("rulebook", DeskHomeMap.RulebookPocket, 0.020f, 9.09f, 0.075f, 0.2f, 0.636f, 0.2122f),
        new DeskSlider(DeskHomeMap.Detain, null, SlideKind.Fixed, 0f),
        new DeskSlider("available", null, SlideKind.Fixed, 0f),
        new DeskSlider(DeskHomeMap.Bell, null, SlideKind.Fixed, 0f),
        new DeskSlider(DeskHomeMap.DealTray, null, SlideKind.Fixed, 0f),
    };

    /// <summary>The daters' tip toward the hand as they rise (degrees; slides.json's note on stamp_approve: "the dater rises 1.2 cm and tips 5 deg toward the hand", STAMP B3), toward the chair as the glasses'.</summary>
    public const float DaterTip = 5f;

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
