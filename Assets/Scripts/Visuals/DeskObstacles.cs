using System.Collections.Generic;

/// <summary>
/// The desk's obstacles (owner DOC-HANDLE; BUG-8: one obstacle registry
/// serves the carry, the docks' ways and the rest of a dropped paper): each
/// desk object's footprint and its height over the desk, by id. A footprint
/// is a DeskRect in WORLD x and z (DeskRect x = world x, DeskRect y = world
/// z: the axis-aligned bounds of what stands on the desk, e.g. a renderer's
/// bounds.min and max x and z), so every registrant uses the same frame; the
/// height is in metres over the desk's top. DOC-HANDLE registers the scanner
/// (its casing, as high as its open lid's sweep) and the chip reader (and the
/// UV torch's cradle until TOOLS registers its own); DESK-PLAN, STAMP, TOOLS and
/// FEEDBACK register their objects (the rack, the torch, the ticket...) in
/// <see cref="Desk"/> and unregister them when they leave the desk. A
/// resting paper never lies under one (DropResolver keeps it clear of every
/// footprint), and a held paper is drawn above the tallest (HeldOnTop).
/// </summary>
public sealed class DeskObstacles
{
    /// <summary>The desk's registry (every desk object's footprint).</summary>
    public static DeskObstacles Desk { get; } = new DeskObstacles();

    private readonly Dictionary<string, (DeskRect footprint, float height)> _byId = new Dictionary<string, (DeskRect, float)>();

    /// <summary>How many obstacles are registered.</summary>
    public int Count => _byId.Count;

    /// <summary>Registers (or moves) obstacle <paramref name="id"/>: its <paramref name="footprint"/> on the desk (world x, z) and its <paramref name="height"/> over it (metres; negative counts as 0: a footprint papers keep off that nothing stands on, such as the area a machine hides from the camera).</summary>
    public void Register(string id, DeskRect footprint, float height) => _byId[id] = (footprint, height > 0f ? height : 0f);

    /// <summary>Forgets obstacle <paramref name="id"/>; false when it was not registered.</summary>
    public bool Unregister(string id) => _byId.Remove(id);

    /// <summary>The height of the tallest obstacle whose footprint holds the desk point (<paramref name="x"/>, <paramref name="y"/>: world x, z); 0 over the bare desk.</summary>
    public float HeightAt(float x, float y)
    {
        float top = 0f;
        foreach ((DeskRect footprint, float height) in _byId.Values)
            if (height > top && footprint.Contains(x, y))
                top = height;
        return top;
    }

    /// <summary>The tallest obstacle's height anywhere on the desk (metres; 0 with none): what a held thing is drawn above (HeldOnTop).</summary>
    public float MaxHeight
    {
        get
        {
            float top = 0f;
            foreach ((DeskRect _, float height) in _byId.Values)
                if (height > top)
                    top = height;
            return top;
        }
    }

    /// <summary>Copies every registered footprint (world x, z) into <paramref name="into"/> after clearing it: the keep-outs a resting paper stays clear of (DropResolver).</summary>
    public void Footprints(List<DeskRect> into)
    {
        into.Clear();
        foreach ((DeskRect footprint, float _) in _byId.Values)
            into.Add(footprint);
    }
}
