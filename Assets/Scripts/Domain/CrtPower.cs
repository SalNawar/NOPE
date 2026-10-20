using System;

/// <summary>
/// One moment of the PC frame's power move (CrtPower.At, CrtPower.Leaving). Not serialized.
/// </summary>
public readonly struct CrtFrame
{
    /// <summary>The picture's width and height as shares of the glass (the CRT's line and dot), about the glass's centre.</summary>
    public readonly float X, Y;

    /// <summary>The frame's own alpha (the bezel, its controls and the dark glass over the picture): it fades in on opening and out after the afterglow, so the frame never pops in or out whole.</summary>
    public readonly float Bezel;

    /// <summary>How dark the glass is over the picture (0 clear, 1 black): while the frame comes and goes, the warm-up before the picture, and the Reduced Motion fade.</summary>
    public readonly float Veil;

    /// <summary>True while the desktop's picture is drawn into the glass at all.</summary>
    public readonly bool Picture;

    /// <summary>A moment.</summary>
    public CrtFrame(float x, float y, float bezel, float veil, bool picture)
    {
        X = x;
        Y = y;
        Bezel = bezel;
        Veil = veil;
        Picture = picture;
    }
}

/// <summary>
/// The CRT's power-on line and collapse (GAME_FEEL_BIBLE F1, BUG-32c,
/// UI-PC), with the knobs the caller passes (DesktopConfigSO: the house
/// rule "knobs live in ScriptableObjects"). Opening: on the press frame the
/// frame (the bezel and its dark glass) starts to fade in from
/// <see cref="FrameFirstStep"/> (F1: "no frame-0 UI pop": the frame is
/// 63-77 % of the screen, and shown whole in one frame it is a cut); the
/// degauss hum begins 80 ms before the picture (F1); the picture comes on as
/// the frame is whole, a bright full-width line growing to full height
/// (OutCubic). Its camera is off until then, so its black clear never cuts
/// in under a glass that is not yet opaque. Closing: it squeezes to a line
/// over the first part of the collapse, the line shrinks to a dot, the glass
/// is dark as the picture goes, the dot glows on for a few frames (F1: "a
/// phosphor afterglow of 2 frames on close"), then the frame fades out
/// (<see cref="Leaving"/>). Reduced Motion: no squeeze; a fade in two
/// halves, the frame and its dark glass over the room, then the dark glass
/// off the picture (and back on closing), so the picture, drawn by its own
/// camera, is never cut in or out where the eye can see it. Pure; PcFrame
/// draws it.
/// </summary>
public sealed class CrtPower
{
    /// <summary>The share of the collapse the vertical squeeze takes (the rest shrinks the line to a dot).</summary>
    private const float SqueezeShare = 0.66f;

    /// <summary>The frame's alpha on the press frame: the press is answered at once, and the step stays under a cut's (seqtools: 40 luma over 60 % of the pixels) against the room.</summary>
    public const float FrameFirstStep = 0.2f;

    private readonly float _open, _close, _fade, _line, _lead, _frame;

    /// <summary>How many frames the phosphor dot glows on after the collapse.</summary>
    public readonly int AfterglowFrames;

    /// <summary>
    /// A power move: the frame fades in over <paramref name="frameSeconds"/>,
    /// the picture grows over <paramref name="openSeconds"/> with the hum
    /// <paramref name="leadSeconds"/> ahead of it, collapses over
    /// <paramref name="closeSeconds"/>, glows on <paramref name="afterglowFrames"/>
    /// frames, then the frame fades out over <paramref name="frameSeconds"/>;
    /// with Reduced Motion it fades over <paramref name="fadeSeconds"/>; the
    /// line is <paramref name="lineHeight"/> of the picture's height. Negative
    /// times and frames count as 0; the line is kept within (0, 1].
    /// </summary>
    public CrtPower(float openSeconds, float closeSeconds, float fadeSeconds, float lineHeight, float leadSeconds, int afterglowFrames, float frameSeconds)
    {
        _open = Math.Max(0f, openSeconds);
        _close = Math.Max(0f, closeSeconds);
        _fade = Math.Max(0f, fadeSeconds);
        _line = Math.Max(0.001f, Math.Min(1f, lineHeight));
        _lead = Math.Max(0f, leadSeconds);
        _frame = Math.Max(0f, frameSeconds);
        AfterglowFrames = Math.Max(0, afterglowFrames);
    }

    /// <summary>How long the frame's fade out after the afterglow takes (full motion; Reduced Motion's closing fade ends with the frame gone).</summary>
    public float LeaveSeconds => _frame;

    /// <summary>How long the move takes: opening (the frame's fade or the lead, then the power-on) or closing (the collapse; the afterglow is counted in frames, the frame's fade out is <see cref="LeaveSeconds"/>), with Reduced Motion or not.</summary>
    public float Seconds(bool opening, bool reduced) => reduced ? _fade : opening ? PictureAt(false) + _open : _close;

    /// <summary>True once the picture itself has started <paramref name="elapsed"/> seconds into the move (the power-on's glow belongs to that moment).</summary>
    public bool PictureStarted(float elapsed, bool opening, bool reduced) => !opening || elapsed >= PictureAt(reduced);

    /// <summary>True once the degauss hum is due <paramref name="elapsed"/> seconds into opening: the lead before the picture (F1: 80 ms), from the press frame at the earliest.</summary>
    public bool HumStarted(float elapsed, bool reduced) => elapsed >= Math.Max(0f, PictureAt(reduced) - _lead);

    /// <summary>The frame <paramref name="elapsed"/> seconds into opening or closing; at or past the end, opening is the full picture and closing has no picture, under a dark glass.</summary>
    public CrtFrame At(float elapsed, bool opening, bool reduced)
    {
        if (reduced)
            return Fade(Progress(elapsed, _fade), opening);
        if (opening)
        {
            float picture = PictureAt(false);
            if (elapsed < picture)
                return new CrtFrame(1f, _line, FrameIn(elapsed), 1f, false);
            float t = Progress(elapsed - picture, _open);
            return new CrtFrame(1f, _line + (1f - _line) * OutCubic(t), 1f, 0f, true);
        }
        float c = Progress(elapsed, _close);
        if (c >= 1f)
            return new CrtFrame(0f, 0f, 1f, 1f, false);
        if (c < SqueezeShare)
            return new CrtFrame(1f, 1f - (1f - _line) * OutCubic(c / SqueezeShare), 1f, 0f, true);
        float dot = (c - SqueezeShare) / (1f - SqueezeShare);
        return new CrtFrame(1f - OutCubic(dot), _line * (1f - dot), 1f, 0f, true);
    }

    /// <summary>The frame fading out <paramref name="elapsed"/> seconds after the afterglow (full motion): the bezel and its dark glass over the room, gone at <see cref="LeaveSeconds"/>.</summary>
    public CrtFrame Leaving(float elapsed) => new CrtFrame(0f, 0f, 1f - Progress(elapsed, _frame), 1f, false);

    /// <summary>The phosphor dot's glow <paramref name="frame"/> frames after the collapse ended (0 from <see cref="AfterglowFrames"/> on).</summary>
    public float Afterglow(int frame) =>
        frame < 0 || frame >= AfterglowFrames ? 0f : (AfterglowFrames - frame) / (float)(AfterglowFrames + 1);

    /// <summary>When the picture comes on, opening: once the frame is whole and the hum has led it (full motion), or at the fade's half (Reduced Motion).</summary>
    private float PictureAt(bool reduced) => reduced ? _fade / 2f : Math.Max(_lead, _frame);

    /// <summary>The frame's alpha <paramref name="elapsed"/> seconds into its fade in: <see cref="FrameFirstStep"/> on the press frame, whole at the frame's time.</summary>
    private float FrameIn(float elapsed) => _frame <= 0f ? 1f : FrameFirstStep + (1f - FrameFirstStep) * Progress(elapsed, _frame);

    /// <summary>The Reduced Motion fade at <paramref name="t"/> (0..1): the frame and its dark glass over the room (from the first step, opening), then the glass clears off the picture; closing, the reverse, the frame down to nothing.</summary>
    private static CrtFrame Fade(float t, bool opening)
    {
        float u = opening ? t : 1f - t; // how far towards the open frame
        if (opening ? u < 0.5f : u <= 0.5f) // at the half the picture is on while opening, off while closing: either way under a fully dark glass
            return new CrtFrame(1f, 1f, opening ? FrameFirstStep + (1f - FrameFirstStep) * u * 2f : u * 2f, 1f, false);
        return new CrtFrame(1f, 1f, 1f, 1f - (u - 0.5f) * 2f, true);
    }

    /// <summary>How far through a move of <paramref name="length"/> seconds (0..1; a zero-length move is at its end).</summary>
    private static float Progress(float elapsed, float length) => length <= 0f ? 1f : Clamp01(elapsed / length);

    private static float OutCubic(float t)
    {
        float u = 1f - Clamp01(t);
        return 1f - u * u * u;
    }

    private static float Clamp01(float v) => Math.Max(0f, Math.Min(1f, v));
}
