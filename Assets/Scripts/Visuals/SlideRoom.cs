using System;
using System.Collections.Generic;

/// <summary>
/// How far a track slider of the desk's slide-in may go toward the mat
/// before it meets a thing resting on the desk (FOLD-01, Saleh's playtest of
/// 2026-10-28, point 4; Papers, Please: a thing let go lies where it is let
/// go). A paper or the folder let go beside a machine stays there, in the
/// strip the machine would slide into; the slider stops short of it instead
/// of pushing it (a push would move the player's paper, the very fault) and
/// keeps its hover cue. Its whole slide shrinks to the room (Share: the same
/// curve, shorter, so its ease and overshoot stay), and the share eases to a
/// new room (EaseShare), so a paper let go in the strip while the slider is
/// out sees it back off smoothly, never a jump. Every rectangle is in the
/// desk's frame (DeskLayout.Frame: x right, y away from the chair). Pure.
/// </summary>
public static class SlideRoom
{
    /// <summary>The gap a slider keeps from a resting thing (metres; DropResolver's gap).</summary>
    public const float Gap = DropResolver.Gap;

    /// <summary>How long the share takes to ease most of the way to a new room (seconds).</summary>
    public const float EaseSeconds = 0.08f;

    /// <summary>
    /// How far (metres, 0 to <paramref name="reach"/>) a track resting on
    /// <paramref name="rest"/> may slide along x by <paramref name="way"/> (-1
    /// left, +1 right, toward the mat) before its leading edge comes within
    /// <paramref name="gap"/> of a thing of <paramref name="resting"/>: the
    /// things across its depth and wholly ahead of its leading edge count (a
    /// thing over its home is in it: a docked paper rides it). Its full
    /// <paramref name="reach"/> when nothing rests there, or for no way.
    /// </summary>
    public static float Room(DeskRect rest, float way, float reach, IReadOnlyList<DeskRect> resting, float gap)
    {
        float room = Math.Max(0f, reach);
        if (way == 0f || resting == null)
            return room;
        float lead = way < 0f ? rest.CentreX - rest.Width / 2f : rest.CentreX + rest.Width / 2f;
        float y0 = rest.CentreY - rest.Height / 2f, y1 = rest.CentreY + rest.Height / 2f;
        for (int i = 0; i < resting.Count; i++)
        {
            DeskRect r = resting[i];
            if (r.CentreY + r.Height / 2f <= y0 || r.CentreY - r.Height / 2f >= y1)
                continue; // not across its depth
            float near = way < 0f ? r.CentreX + r.Width / 2f : r.CentreX - r.Width / 2f;
            float ahead = way < 0f ? lead - near : near - lead;
            if (ahead < -1e-4f)
                continue; // over its home: in it, not in its way
            room = Math.Min(room, Math.Max(0f, ahead - gap));
        }
        return room;
    }

    /// <summary>The share of its slide a track may make with <paramref name="room"/> metres of room and <paramref name="reach"/> metres of travel at its farthest (1 with room for all of it).</summary>
    public static float Share(float room, float reach) => reach <= 1e-6f ? 1f : Math.Max(0f, Math.Min(1f, room / reach));

    /// <summary>The share eased from <paramref name="share"/> toward <paramref name="target"/> over <paramref name="dt"/> seconds (an exponential ease, EaseSeconds; at once with <paramref name="still"/>: Reduced Motion).</summary>
    public static float EaseShare(float share, float target, float dt, bool still)
    {
        if (still)
            return target;
        float k = 1f - (float)Math.Exp(-Math.Max(0f, dt) / EaseSeconds);
        float next = share + (target - share) * k;
        return Math.Abs(next - target) < 1e-4f ? target : next;
    }
}
