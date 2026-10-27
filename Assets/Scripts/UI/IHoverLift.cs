/// <summary>
/// Something on the desk that answers the pointer's hover by lifting instead
/// of drawing an outline (Saleh's D15: "papers lift and props get an
/// outline"): a paper, the rulebook folder and its tabs. HoverHighlighter finds
/// it on a hovered Clickable or above it.
/// </summary>
public interface IHoverLift
{
    /// <summary>Lifts by <paramref name="share"/> of a hold's lift (0: lying again).</summary>
    void HoverLift(float share);
}
