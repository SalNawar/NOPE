using System;

/// <summary>
/// The D6 framing focus's target (Saleh, D6: "when they slide in, we subtly
/// zoom in a bit not too hard just a small focus to frame the shot";
/// DESK-HOMES P3 and UNITY_PLAN 5.2): which slider the reading view frames
/// now. It starts focusDwellSeconds after the cursor's slider has finished
/// its slide, at most once every focusEverySeconds; moving to the next slider
/// re-aims it once that one has settled, never out and back in; the cursor
/// leaving, or a slider that will not come (busy at home), lets it go. Off
/// whenever it is not allowed (the caller's rule: the reading view on,
/// nothing held, no tilt blend running, motion not still). It only picks:
/// FramingFocus hands the target to CameraFeel, the one lens writer. Pure.
/// </summary>
public sealed class SlideFocus
{
    private readonly DeskSlides _slides;
    private string _candidate;
    private float _candidateSince, _lastStart = float.NegativeInfinity;

    /// <summary>The focus over <paramref name="slides"/>' timings and budgets, framing nothing.</summary>
    public SlideFocus(DeskSlides slides)
    {
        _slides = slides ?? throw new ArgumentNullException(nameof(slides));
    }

    /// <summary>The slider framed now (null: none).</summary>
    public string Target { get; private set; }

    /// <summary>Advances to <paramref name="now"/> (seconds, the game feel's clock) with <paramref name="assist"/>'s pick and whether a focus may show at all (<paramref name="allowed"/>).</summary>
    public void Step(float now, ProximityAssist assist, bool allowed)
    {
        string pick = allowed && assist != null ? assist.Picked : null;
        if (pick == null)
        {
            Target = null;
            _candidate = null;
            return;
        }
        if (pick == Target)
        {
            _candidate = null;
            return;
        }
        if (!assist.SlideDone(pick))
        {
            // Still coming (waiting on a hand-off, or sliding): the focus holds where it is; one that will not come lets it go.
            SlidePhase phase = assist.Phase(pick);
            if (phase != SlidePhase.Out && phase != SlidePhase.Wait)
                Target = null;
            _candidate = null;
            return;
        }
        if (pick != _candidate)
        {
            _candidate = pick;
            _candidateSince = now;
        }
        if (now - _candidateSince < _slides.focusDwellSeconds - 1e-5f)
            return;
        if (Target == null)
        {
            if (now - _lastStart < _slides.focusEverySeconds - 1e-5f)
                return;
            _lastStart = now;
        }
        Target = pick;
        _candidate = null;
    }

    /// <summary>The aim toward a machine at viewport (<paramref name="x"/>, <paramref name="y"/>) (0 to 1, the frame's centre at 0.5): the way to it, at most <paramref name="limit"/> of the frame.</summary>
    public static (float x, float y) Aim(float x, float y, float limit)
    {
        float dx = x - 0.5f, dy = y - 0.5f;
        float length = (float)Math.Sqrt(dx * dx + dy * dy);
        if (length <= limit || length < 1e-6f)
            return (dx, dy);
        return (dx * limit / length, dy * limit / length);
    }

    /// <summary>The turn (degrees: a pan right, a tilt that is positive downward, as Cinemachine's recomposer takes them) that brings the frame's centre <paramref name="aim"/> of the frame toward a point, for a view <paramref name="verticalFov"/> degrees high at <paramref name="aspect"/>.</summary>
    public static (float pan, float tilt) Turn((float x, float y) aim, float verticalFov, float aspect)
    {
        double halfV = verticalFov * Math.PI / 360.0, tanV = Math.Tan(halfV), tanH = tanV * Math.Max(1e-3f, aspect);
        float pan = (float)(Math.Atan(2.0 * aim.x * tanH) * 180.0 / Math.PI);
        float tilt = (float)(-Math.Atan(2.0 * aim.y * tanV) * 180.0 / Math.PI);
        return (pan, tilt);
    }

    /// <summary>How many degrees the focus narrows a view whose bound field of view is <paramref name="baseFov"/>, on top of a push-in of <paramref name="push"/> degrees: <paramref name="share"/> of it, the two together never past <paramref name="cap"/> of it (and never below nothing).</summary>
    public static float Narrowing(float baseFov, float push, float share, float cap) => Math.Max(0f, Math.Min(share * baseFov, cap * baseFov - push));
}
