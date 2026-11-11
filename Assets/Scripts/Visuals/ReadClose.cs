using System;

/// <summary>One real size in millimetres (width across, height down, as the table states it).</summary>
[Serializable]
public struct PaperMm
{
    /// <summary>The width in millimetres.</summary>
    public float width;

    /// <summary>The height in millimetres.</summary>
    public float height;

    /// <summary>A size <paramref name="width"/> by <paramref name="height"/> millimetres.</summary>
    public PaperMm(float width, float height)
    {
        this.width = width;
        this.height = height;
    }
}

/// <summary>
/// The real sizes of the papers and the read-close lift (doc-read-close,
/// Saleh 2026-11-11: "the documents sizes are unreadable documents are huge
/// folder is small", then "A: lift to read"). At rest on the desk every paper
/// takes its world width from this table at one common scale, so the
/// proportions hold and the folder is the largest thing. In inspect mode the
/// paper under the pointer lifts toward the camera until its print is
/// readable, and settles back when the pointer leaves it. Held by
/// DeskConfigSO.readClose; the knobs are edited there.
/// </summary>
[Serializable]
public sealed class PaperRealSizes
{
    /// <summary>A passport, open (millimetres).</summary>
    public PaperMm passportOpen = new PaperMm(176f, 125f);

    /// <summary>A plastic card (millimetres).</summary>
    public PaperMm card = new PaperMm(86f, 54f);

    /// <summary>A ticket (millimetres).</summary>
    public PaperMm ticket = new PaperMm(190f, 82f);

    /// <summary>A form or a permit (millimetres).</summary>
    public PaperMm form = new PaperMm(148f, 210f);

    /// <summary>World metres per real metre on the desk (one scale for every paper): the folder's own scale, measured 0.92 (doc-legibility REPORT, 3.3).</summary>
    public float realScale = 0.92f;

    /// <summary>The lift's bars (cap height in screen pixels): every value at least valueCapPx, every label at least labelCapPx, while the paper still fits the frame.</summary>
    public float valueCapPx = 11f, labelCapPx = 8f;

    /// <summary>How long the lift and the settle take (seconds; instant under Reduced Motion).</summary>
    public float seconds = 0.18f;

    /// <summary>The band at the screen's bottom (a share of its height) that a lifted paper never covers: the compare readout's place at the bottom centre.</summary>
    public float bottomMargin = 0.09f;

    /// <summary>The table's size for a paper drawn with <paramref name="frame"/>: a booklet is the open passport, a card the card, a ticket the ticket, any other a form.</summary>
    public PaperMm Of(FormFrame frame) =>
        frame == FormFrame.Booklet ? passportOpen : frame == FormFrame.Card ? card : frame == FormFrame.Ticket ? ticket : form;

    /// <summary>The world width (metres) at rest on the desk of a paper drawn with <paramref name="frame"/> whose art is <paramref name="aspect"/> wide per unit of height (ReadClose.RestWidth).</summary>
    public float RestWidth(FormFrame frame, float aspect) => ReadClose.RestWidth(aspect, Of(frame), realScale);
}

/// <summary>The read-close lift in numbers (PaperRealSizes; DeskDocument applies it). Pure, so it is tested headless.</summary>
public static class ReadClose
{
    /// <summary>
    /// A paper's world width at rest (metres): the side of <paramref name="real"/>
    /// that lies across its art, times <paramref name="realScale"/>. A portrait
    /// art (aspect under 1) takes the shorter side, a landscape art the longer
    /// (the passport's art opens top to bottom: its width is 125 mm). The art
    /// keeps its own aspect: its height is this width over the aspect.
    /// </summary>
    public static float RestWidth(float aspect, PaperMm real, float realScale)
    {
        bool wide = aspect >= 1f, tableWide = real.width >= real.height;
        float across = wide == tableWide ? real.width : real.height;
        return across / 1000f * realScale;
    }

    /// <summary>
    /// How much larger the lifted paper shows than at rest: enough for its
    /// smallest value (<paramref name="valueCapPx"/> at rest) to reach
    /// <paramref name="valueBar"/> and its smallest label
    /// (<paramref name="labelCapPx"/>) <paramref name="labelBar"/>, but never
    /// more than lets the paper (<paramref name="widthPx"/> by
    /// <paramref name="heightPx"/> at rest) fit a screen
    /// <paramref name="screenW"/> by <paramref name="screenH"/> above its
    /// bottom band <paramref name="bottomPx"/>; never under 1. A text of 0
    /// asks nothing.
    /// </summary>
    public static float Magnify(float valueCapPx, float labelCapPx, float valueBar, float labelBar, float widthPx, float heightPx, float screenW, float screenH, float bottomPx)
    {
        float need = 1f;
        if (valueCapPx > 0f)
            need = Math.Max(need, valueBar / valueCapPx);
        if (labelCapPx > 0f)
            need = Math.Max(need, labelBar / labelCapPx);
        float fit = widthPx > 0f && heightPx > 0f ? Math.Min(screenW / widthPx, (screenH - bottomPx) / heightPx) : 1f;
        return Math.Max(1f, Math.Min(need, fit));
    }

    /// <summary>
    /// Where the lifted paper's centre goes on the screen (pixels, y up from
    /// the bottom): the point (<paramref name="x"/>, <paramref name="y"/>)
    /// moved just enough that a paper <paramref name="halfW"/> by
    /// <paramref name="halfH"/> (half sizes) round it stays inside a screen
    /// <paramref name="screenW"/> by <paramref name="screenH"/> and above its
    /// bottom band <paramref name="bottomPx"/>; centred in an axis it does not fit.
    /// </summary>
    public static (float x, float y) Clamp(float x, float y, float halfW, float halfH, float screenW, float screenH, float bottomPx)
    {
        x = 2f * halfW >= screenW ? screenW / 2f : Math.Min(Math.Max(x, halfW), screenW - halfW);
        y = 2f * halfH >= screenH - bottomPx ? (screenH + bottomPx) / 2f : Math.Min(Math.Max(y, bottomPx + halfH), screenH - halfH);
        return (x, y);
    }
}

/// <summary>Which one paper is lifted to read (at most one at a time): a paper that asks for the lift takes it from the one before; a paper that lets go gives it up only if it held it. Pure; DeskDocument keeps one for the desk.</summary>
public sealed class ReadTurn
{
    /// <summary>The lifted paper's id, or -1 for none.</summary>
    public int Lifted { get; private set; } = -1;

    /// <summary>Paper <paramref name="id"/> asks for the lift (<paramref name="wanted"/>) or lets it go; true while it holds the lift.</summary>
    public bool Want(int id, bool wanted)
    {
        if (wanted)
            Lifted = id;
        else if (Lifted == id)
            Lifted = -1;
        return Lifted == id;
    }
}
