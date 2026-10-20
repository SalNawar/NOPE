using System;

/// <summary>
/// One moment of the PC frame's power move (CrtPower.At). Not serialized.
/// </summary>
public readonly struct CrtFrame
{
    /// <summary>The picture's width and height as shares of the glass (the CRT's line and dot), about the glass's centre.</summary>
    public readonly float X, Y;

    /// <summary>The frame's own alpha (the bezel, its controls and the dark glass over the picture); 1 except in the Reduced Motion fade.</summary>
    public readonly float Bezel;

    /// <summary>How dark the glass is over the picture (0 clear, 1 black): the warm-up before the picture and the Reduced Motion fade.</summary>
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
/// rule "knobs live in ScriptableObjects"). Opening: the degauss hum
/// begins on the press frame and the glass stays dark for the lead (F1:
/// "the CRT's degauss hum begins 80 ms before the picture"); then the
/// picture grows from a bright full-width line to full height (OutCubic).
/// Closing: it squeezes to a line over the first part of the collapse, the
/// line shrinks to a dot, and the dot glows on for a few frames (F1: "a
/// phosphor afterglow of 2 frames on close"). Reduced Motion: no squeeze; a
/// fade in two halves, the frame and its dark glass over the room, then the
/// dark glass off the picture (and back on closing), so the picture, drawn
/// by its own camera, is never cut in or out where the eye can see it. The
/// frame's input is live from the press frame; only the picture moves. Pure;
/// PcFrame draws it.
/// </summary>
public sealed class CrtPower
{
    /// <summary>The share of the collapse the vertical squeeze takes (the rest shrinks the line to a dot).</summary>
    private const float SqueezeShare = 0.66f;

    private readonly float _open, _close, _fade, _line, _lead;

    /// <summary>How many frames the phosphor dot glows on after the collapse.</summary>
    public readonly int AfterglowFrames;

    /// <summary>
    /// A power move: the picture grows over <paramref name="openSeconds"/>
    /// after a dark <paramref name="leadSeconds"/>, collapses over
    /// <paramref name="closeSeconds"/> and glows on
    /// <paramref name="afterglowFrames"/> frames; with Reduced Motion it fades
    /// over <paramref name="fadeSeconds"/>; the line is
    /// <paramref name="lineHeight"/> of the picture's height. Negative times
    /// and frames count as 0; the line is kept within (0, 1].
    /// </summary>
    public CrtPower(float openSeconds, float closeSeconds, float fadeSeconds, float lineHeight, float leadSeconds, int afterglowFrames)
    {
        _open = Math.Max(0f, openSeconds);
        _close = Math.Max(0f, closeSeconds);
        _fade = Math.Max(0f, fadeSeconds);
        _line = Math.Max(0.001f, Math.Min(1f, lineHeight));
        _lead = Math.Max(0f, leadSeconds);
        AfterglowFrames = Math.Max(0, afterglowFrames);
    }

    /// <summary>How long the move takes: opening (the lead and the power-on) or closing (the collapse; the afterglow is counted in frames), with Reduced Motion or not.</summary>
    public float Seconds(bool opening, bool reduced) => reduced ? _fade : opening ? _lead + _open : _close;

    /// <summary>True once the picture itself has started <paramref name="elapsed"/> seconds into the move (the power-on's glow belongs to that moment).</summary>
    public bool PictureStarted(float elapsed, bool opening, bool reduced) =>
        !opening || elapsed >= (reduced ? _fade / 2f : _lead);

    /// <summary>The frame <paramref name="elapsed"/> seconds into opening or closing; at or past the end, opening is the full picture and closing has no picture.</summary>
    public CrtFrame At(float elapsed, bool opening, bool reduced)
    {
        if (reduced)
            return Fade(Progress(elapsed, _fade), opening);
        if (opening)
        {
            if (elapsed < _lead)
                return new CrtFrame(1f, _line, 1f, 1f, true);
            float t = Progress(elapsed - _lead, _open);
            return new CrtFrame(1f, _line + (1f - _line) * OutCubic(t), 1f, 0f, true);
        }
        float c = Progress(elapsed, _close);
        if (c >= 1f)
            return new CrtFrame(0f, 0f, 1f, 0f, false);
        if (c < SqueezeShare)
            return new CrtFrame(1f, 1f - (1f - _line) * OutCubic(c / SqueezeShare), 1f, 0f, true);
        float dot = (c - SqueezeShare) / (1f - SqueezeShare);
        return new CrtFrame(1f - OutCubic(dot), _line * (1f - dot), 1f, 0f, true);
    }

    /// <summary>The phosphor dot's glow <paramref name="frame"/> frames after the collapse ended (0 from <see cref="AfterglowFrames"/> on).</summary>
    public float Afterglow(int frame) =>
        frame < 0 || frame >= AfterglowFrames ? 0f : (AfterglowFrames - frame) / (float)(AfterglowFrames + 1);

    /// <summary>The Reduced Motion fade at <paramref name="t"/> (0..1): the frame and its dark glass over the room, then the glass clears off the picture; closing, the reverse.</summary>
    private static CrtFrame Fade(float t, bool opening)
    {
        float u = opening ? t : 1f - t; // how far towards the open frame
        if (opening ? u < 0.5f : u <= 0.5f) // at the half the picture is on while opening, off while closing: either way under a fully dark glass
            return new CrtFrame(1f, 1f, u * 2f, 1f, false);
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
