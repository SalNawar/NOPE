using System;
using System.Collections.Generic;

/// <summary>
/// The desk's swept boxes (desk v3; UNITY_PLAN 5.3 and DESK-HOMES section
/// 6's precondition: the carry and the drop read them, never a live read of
/// a machine that slides out under a paper). Refresh makes every shown
/// home's obstacle from its objects' height at rest (RestHeight: a part of a
/// slider counted where it stands at rest, what its pose put on taken off, so
/// a lift raised when they are measured is not counted twice) as
/// DeskSlides.Obstacle sweeps it (a track's footprint to its overshoot peak
/// toward the mat, a lift's top raised by its peak), and keeps the swept boxes
/// of the homes something slides or lifts in (DeskSlides.Sweeps). A dragged
/// paper keeps over the box under it (TryTop: DeskController's carry floor),
/// and a paper let go keeps clear of each track's swept strip (Strips:
/// DeskController.ClearOfBlockers; 0 px over a resting paper). DeskLayout
/// holds one (RefreshObstacles, TrySweptTop, SweptStrips). Pure; TryTop and
/// Strips allocate nothing.
/// </summary>
public sealed class DeskSweeps
{
    private readonly List<(DeskHome home, DeskRect footprint, float height)> _boxes = new List<(DeskHome, DeskRect, float)>();

    /// <summary>How many swept boxes are kept.</summary>
    public int Count => _boxes.Count;

    /// <summary>Forgets every swept box (the desk unbound).</summary>
    public void Clear() => _boxes.Clear();

    /// <summary>
    /// How tall (metres over the desk) a home's objects stand at rest: the
    /// tallest of <paramref name="parts"/> (each drawn part's top over the
    /// desk as it stands now, and how far its slider's pose has it over its
    /// rest right then: MachineSlideIn.Raised, taken off), or
    /// <paramref name="homeHeight"/> with no part drawn.
    /// </summary>
    public static float RestHeight(IReadOnlyList<(float top, float raised)> parts, float homeHeight)
    {
        int count = parts != null ? parts.Count : 0;
        if (count == 0)
            return homeHeight;
        float top = float.NegativeInfinity;
        for (int i = 0; i < count; i++)
            top = Math.Max(top, parts[i].top - parts[i].raised);
        return top;
    }

    /// <summary>
    /// Every home of <paramref name="shown"/> (its objects drawn, standing
    /// <c>height</c> over the desk at rest: RestHeight) as the obstacle its
    /// objects make (<paramref name="slides"/>' Obstacle, or the home's own
    /// footprint and that height without slides), written into
    /// <paramref name="obstacles"/> (cleared first), in order; the swept
    /// boxes kept are those of the homes something slides or lifts in
    /// (DeskSlides.Sweeps), replacing the last refresh's.
    /// </summary>
    public void Refresh(DeskSlides slides, IReadOnlyList<(DeskHome home, float height)> shown, List<(string id, DeskRect footprint, float height)> obstacles)
    {
        _boxes.Clear();
        obstacles?.Clear();
        int count = shown != null ? shown.Count : 0;
        for (int i = 0; i < count; i++)
        {
            (DeskHome home, float height) = shown[i];
            if (home == null)
                continue;
            (DeskRect footprint, float top) = slides != null ? slides.Obstacle(home, height) : (home.Footprint, height);
            obstacles?.Add((home.id, footprint, top));
            if (slides != null && slides.Sweeps(home))
                _boxes.Add((home, footprint, top));
        }
    }

    /// <summary>
    /// The height (metres over the desk) of the tallest swept box under the
    /// desk point (<paramref name="x"/>, <paramref name="y"/>) with its
    /// footprint grown by <paramref name="grow"/> metres (a carried paper's
    /// corners); false over none. Allocates nothing.
    /// </summary>
    public bool TryTop(float x, float y, float grow, out float height)
    {
        height = float.NegativeInfinity;
        bool any = false;
        for (int i = 0; i < _boxes.Count; i++)
        {
            DeskRect f = _boxes[i].footprint;
            if (Math.Abs(x - f.CentreX) > f.Width / 2f + grow || Math.Abs(y - f.CentreY) > f.Height / 2f + grow)
                continue;
            height = Math.Max(height, _boxes[i].height);
            any = true;
        }
        return any;
    }

    /// <summary>
    /// The swept box of each track (its home's footprint and the strip its
    /// slide reaches past it toward the mat; a lift's box is its own
    /// footprint, raised), added to <paramref name="into"/>. Allocates
    /// nothing but what <paramref name="into"/> grows by.
    /// </summary>
    public void Strips(List<DeskRect> into)
    {
        if (into == null)
            return;
        for (int i = 0; i < _boxes.Count; i++)
            if (_boxes[i].footprint.Width > _boxes[i].home.Footprint.Width + 1e-4f)
                into.Add(_boxes[i].footprint);
    }
}
