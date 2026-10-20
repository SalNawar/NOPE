using System.Collections.Generic;

/// <summary>
/// The desk's obstacles (P0-A seam; owner DOC-HANDLE): each desk object's
/// footprint in the desk's plane (a DeskRect, metres) and its height over the
/// desk, by id, so a carried thing keeps over what lies under it. DESK-PLAN,
/// STAMP, TOOLS and FEEDBACK register their objects. The P0 stub is the
/// registry alone: nothing registers yet (today's behaviour).
/// </summary>
public sealed class DeskObstacles
{
    private readonly Dictionary<string, (DeskRect footprint, float height)> _byId = new Dictionary<string, (DeskRect, float)>();

    /// <summary>How many obstacles are registered.</summary>
    public int Count => _byId.Count;

    /// <summary>Registers (or moves) obstacle <paramref name="id"/>: its <paramref name="footprint"/> on the desk and its <paramref name="height"/> over it (metres; negative counts as 0).</summary>
    public void Register(string id, DeskRect footprint, float height) => _byId[id] = (footprint, height > 0f ? height : 0f);

    /// <summary>Forgets obstacle <paramref name="id"/>; false when it was not registered.</summary>
    public bool Unregister(string id) => _byId.Remove(id);

    /// <summary>The height of the tallest obstacle whose footprint holds the desk point (<paramref name="x"/>, <paramref name="y"/>); 0 over the bare desk.</summary>
    public float HeightAt(float x, float y)
    {
        float top = 0f;
        foreach ((DeskRect footprint, float height) in _byId.Values)
            if (height > top && footprint.Contains(x, y))
                top = height;
        return top;
    }
}
