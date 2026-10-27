/// <summary>
/// The shape a desk magnifier showed (the loupe's round glass; the bar's and
/// the Fresnel's rectangle). The magnifiers were cut by P0-C (Saleh D5: "not
/// sold"); nothing reads the microprint at the desk any more. Kept only
/// because DeskHardware.TrackLens (DESK-PLAN's file) still names it in its
/// signature: INTEGRATION's cleanup list deletes both (docs/pending/TOOLS.md).
/// </summary>
public enum LensShape
{
    /// <summary>A circle of the lens's half width.</summary>
    Round,

    /// <summary>A rectangle of the lens's half width and half height.</summary>
    Rectangle
}

/// <summary>
/// When a paper's microprint counts as read (FIX_PLAN_v2 TOOLS, BUG-2b; Saleh
/// D5: the magnifier is "not sold", so "the scanner's hidden-layer view
/// reveals the microprint of the ReprintedPaper lie"). The line along a
/// paper's foot (MicroLine: the Bureau's mark, the form and the Citizen ID)
/// is printed too fine to read at the desk; the scan view's hidden layer
/// prints it large (ScanMicroprint). A read is credited only when the player
/// has the paper's hidden layer open and the line rendered there with
/// capitals at least <see cref="ScanMicroprint.MinCapPx"/> tall on the
/// screen, measured from the drawn glyphs (MicroprintView.CapPx), never from
/// geometry: 1019a credited a line drawn 0.024 px tall because a lens's shape
/// covered it. Pure (MicroLensTests). The scan view that draws the line and
/// calls this is UI-PC's (ScanLayerSwitch, FIX_PLAN_v2 #13).
/// </summary>
public static class MicroLens
{
    /// <summary>True when the line counts as read: the paper's hidden layer is open (<paramref name="layerOpen"/>) and the line rendered with capitals <paramref name="renderedCapPx"/> screen pixels tall, at least ScanMicroprint.MinCapPx (0, NaN: not drawn).</summary>
    public static bool Readable(bool layerOpen, float renderedCapPx) => layerOpen && renderedCapPx >= ScanMicroprint.MinCapPx;
}
