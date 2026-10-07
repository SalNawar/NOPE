using System;

/// <summary>
/// The desk's two zones in numbers (Papers, Please's counter and desk, Saleh
/// 2026-10-06): in the office view's frame on the desk (x right, y away from
/// the chair, metres from the desk's centre) the counter is the strip along
/// the traveller's side, its far edge where the desk ends (or the reading
/// view's top) and the given depth deep; everything nearer is the desk. A
/// document on the counter is small (the counter's scale of its own size); on
/// the desk it is full size (every paper the same reading height, a wider
/// paper by its width, as the held papers were). Papers handed over line up
/// along the counter (CounterSpot). Pure, so it is tested headless;
/// DeskCounter and DeskController apply it.
/// </summary>
public static class DeskZones
{
    /// <summary>True when a point <paramref name="y"/> metres along the view from the desk's centre lies on the counter: within <paramref name="depth"/> of its far edge <paramref name="far"/>.</summary>
    public static bool OnCounter(float y, float far, float depth) => y >= far - depth;

    /// <summary>
    /// Where paper <paramref name="k"/> of <paramref name="count"/> handed over
    /// lands by the counter: on the line <paramref name="inset"/> nearer than
    /// the far edge <paramref name="far"/> (the counter strip is slimmer than
    /// a small paper, so the row lies along it rather than in it), spaced
    /// <paramref name="spacing"/> apart round <paramref name="middle"/>, the
    /// row kept inside [<paramref name="left"/>, <paramref name="right"/>]
    /// (narrower when it does not fit). Papers past <paramref name="count"/>
    /// reuse the spots a row nearer by <paramref name="rowDepth"/> and half a
    /// step across (a staggered fan: each row's headers stay in view; 0 lays
    /// them on the same spots).
    /// </summary>
    public static (float x, float y) CounterSpot(int k, int count, float middle, float left, float right, float far, float inset, float spacing, float rowDepth = 0f)
    {
        count = Math.Max(1, count);
        float width = Math.Max(0f, right - left);
        float step = count > 1 ? Math.Min(spacing, width / (count - 1)) : 0f;
        float first = middle - step * (count - 1) / 2f;
        first = Math.Max(left, Math.Min(right - step * (count - 1), first));
        int i = ((k % count) + count) % count;
        int row = rowDepth > 0f && k > 0 ? k / count : 0;
        float x = first + step * i + (row % 2 == 1 ? (i < count - 1 || count == 1 ? step / 2f : -step / 2f) : 0f);
        return (x, far - inset - row * rowDepth);
    }

    /// <summary>A document's scale on the desk (full size): its height brought to <paramref name="readingHeight"/>, or for a paper wider than the desk paper's <paramref name="deskAspect"/> its width to that height's width; 1 for a paper of no size.</summary>
    public static float ReadingScale(float width, float height, float readingHeight, float deskAspect)
    {
        float tall = Math.Max(height, deskAspect > 0f ? width / deskAspect : height);
        return tall > 0f ? readingHeight / tall : 1f;
    }

    /// <summary>Smoothstep over [0, 1] (clamped): the stamp bar's slide and a document's change of size.</summary>
    public static float Ease(float t)
    {
        t = t < 0f ? 0f : t > 1f ? 1f : t;
        return t * t * (3f - 2f * t);
    }
}
