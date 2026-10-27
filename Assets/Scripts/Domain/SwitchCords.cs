using System;
using System.Collections.Generic;

/// <summary>
/// The switchboard's cords as geometry (LOOP_v5: "cords as sagging curves
/// with plug heads that you drag between sockets and that snap and seat"):
/// a patch cord leaves each plug along the plug's axis (every plug on the
/// plate points the same way, its end cap down-left) and hangs under its own
/// weight between them: a cubic curve whose two handles run a reach along
/// the axis from each plug's tail and hang the sag lower (the parabola's sag
/// for a cord a little longer than the gap: sqrt(3 x chord x slack / 8)), so
/// a cord dips out of its plug and sweeps across the board; and where a
/// dropped plug seats: the nearest socket that accepts it within its hit
/// radius (times a magnet), or none. Pure; the office's cords draw and snap
/// through it.
/// </summary>
public static class SwitchCords
{
    /// <summary>The shortest handle a cord's curve gets (pixels): even a short cord leaves its plug along the plug's axis.</summary>
    public const float MinReach = 24f;

    /// <summary>How far a cord hangs when its ends are <paramref name="chord"/> apart and it is <paramref name="slack"/> longer than that (pixels; 0 with no slack).</summary>
    public static float Sag(float chord, float slack)
    {
        if (chord <= 0f || slack <= 0f)
            return 0f;
        return MathF.Sqrt(3f * chord * slack / 8f);
    }

    /// <summary>How far a cord's handles run along its plugs' axis: <paramref name="share"/> of the gap between its ends, between MinReach and <paramref name="max"/>.</summary>
    public static float Reach(float chord, float share, float max) => MathF.Max(MinReach, MathF.Min(max, chord * share));

    /// <summary>
    /// The point <paramref name="t"/> (0 at <paramref name="a"/>, 1 at
    /// <paramref name="b"/>) along the cord between two plug tails whose cord
    /// leaves along <paramref name="axis"/> (a unit direction, y down): its
    /// handles run <paramref name="reach"/> along the axis from each end and
    /// hang <paramref name="sag"/> lower, so its middle hangs three quarters
    /// of the sag below a straight cord's.
    /// </summary>
    public static BoardPoint At(BoardPoint a, BoardPoint b, BoardPoint axis, float reach, float sag, float t)
    {
        t = t < 0f ? 0f : t > 1f ? 1f : t;
        float p1x = a.X + axis.X * reach, p1y = a.Y + axis.Y * reach + sag;
        float p2x = b.X + axis.X * reach, p2y = b.Y + axis.Y * reach + sag;
        float u = 1f - t, k0 = u * u * u, k1 = 3f * u * u * t, k2 = 3f * u * t * t, k3 = t * t * t;
        return new BoardPoint(k0 * a.X + k1 * p1x + k2 * p2x + k3 * b.X, k0 * a.Y + k1 * p1y + k2 * p2y + k3 * b.Y);
    }

    /// <summary>Fills <paramref name="into"/> (cleared) with <paramref name="segments"/> + 1 points along the cord (At), ends included.</summary>
    public static void Sample(BoardPoint a, BoardPoint b, BoardPoint axis, float reach, float sag, int segments, List<BoardPoint> into)
    {
        into.Clear();
        segments = Math.Max(1, segments);
        for (int i = 0; i <= segments; i++)
            into.Add(At(a, b, axis, reach, sag, (float)i / segments));
    }

    /// <summary>
    /// The socket a plug dropped at <paramref name="p"/> seats in: of the
    /// sockets <paramref name="allowed"/> accepts (by index), the nearest whose
    /// hit radius times <paramref name="magnet"/> reaches <paramref name="p"/>;
    /// -1 when none does (the plug goes back).
    /// </summary>
    public static int Snap(IReadOnlyList<BoardSocket> sockets, BoardPoint p, Func<int, bool> allowed, float magnet = 1f)
    {
        int best = -1;
        float bestDistance = float.MaxValue;
        if (sockets == null)
            return best;
        for (int i = 0; i < sockets.Count; i++)
        {
            BoardSocket s = sockets[i];
            if (s == null || (allowed != null && !allowed(i)))
                continue;
            float d = s.Centre.DistanceTo(p);
            if (d <= s.HitRadius * magnet && d < bestDistance)
            {
                best = i;
                bestDistance = d;
            }
        }
        return best;
    }
}
