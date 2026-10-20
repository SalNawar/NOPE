using System;

/// <summary>A point of the desk's world for the UV torch's geometry (metres, y up; the engine's world space).</summary>
public readonly struct TorchPoint
{
    /// <summary>The point (<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>).</summary>
    public TorchPoint(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>Across.</summary>
    public float X { get; }

    /// <summary>Up.</summary>
    public float Y { get; }

    /// <summary>Along.</summary>
    public float Z { get; }
}

/// <summary>How the held torch points: its tilt below the level and its turn off the straight-ahead line (degrees), and how much of its lit patch the eye sees past its body (0..1).</summary>
public readonly struct TorchAim
{
    /// <summary>An aim.</summary>
    public TorchAim(float tilt, float yaw, float unoccluded)
    {
        Tilt = tilt;
        Yaw = yaw;
        Unoccluded = unoccluded;
    }

    /// <summary>Degrees the lens points below the level (UvCone.MinTilt..MaxTilt).</summary>
    public float Tilt { get; }

    /// <summary>Degrees the beam is turned off the away line, seen from above (positive: to the right of away).</summary>
    public float Yaw { get; }

    /// <summary>The share of the lit patch the eye sees past the torch's body at this aim.</summary>
    public float Unoccluded { get; }
}

/// <summary>
/// How the UV torch is held over the desk (FIX_PLAN TOOLS, BUG-2c: "the torch
/// beam lights the paper without its body covering the patch, and the pool is
/// drawn only on a surface"; GAME_FEEL_BIBLE C3: "the beam lights the paper
/// and the body never covers the patch", a short lag of at most 8 mm behind
/// the cursor, the revealed watermark fluorescing in over 120 ms). The pointer
/// holds the grip (Saleh 2026-10-18: "when you hold the UV the mouse should be
/// on the base not the light"); the beam lands ahead of it (UvCone.Ahead). The
/// 1019a hold always pointed straight away at 40 degrees, so from the reading
/// view's high camera the lens end lay over the patch it lit. Now the hold is
/// chosen per view (<see cref="Hold"/>): the aim nearest the built tilt, straight
/// ahead, whose patch the camera sees past the torch's body (a capsule from
/// its tail to its lens). Pure; the torch's MonoBehaviour feeds it the camera,
/// the grip and the torch's measured body (UvTorchHoldTests).
/// </summary>
public static class UvTorchHold
{
    /// <summary>The plan's pass line: at least this share of the revealed area is not covered by the torch's body (FIX_PLAN TOOLS, torch).</summary>
    public const float UnoccludedShare = 0.95f;

    /// <summary>What the hold aims for: one sample of the patch above the pass line, so a camera blend or the grip's lag never dips under it.</summary>
    public const float TargetShare = 0.97f;

    /// <summary>The longest the held grip trails the pointer (metres; GAME_FEEL_BIBLE C3: "a short lag of ≤8 mm behind the cursor").</summary>
    public const float MaxLag = 0.008f;

    /// <summary>The grip's ease after the pointer (seconds to close about two thirds of the way: a light aluminium torch).</summary>
    public const float LagSeconds = 0.03f;

    /// <summary>Seconds the revealed marks take to fluoresce in once the torch is lit (GAME_FEEL_BIBLE C3: "fluoresces in over 120 ms").</summary>
    public const float FluoresceSeconds = 0.12f;

    /// <summary>The lit spot's soft lag behind the aim (seconds: about two frames at 60 fps; GAME_FEEL_BIBLE C3, "the cone's edge has a soft 2-frame lag").</summary>
    public const float TrailSeconds = 1f / 30f;

    /// <summary>The seconds the held torch takes to swing to a new aim (Hold's answer for a new view or place: about two thirds of the way in this time; at once under Reduced Motion).</summary>
    public const float SwingSeconds = 0.08f;

    /// <summary>Degrees the resting torch tips its lens up out of its cradle while the pointer is over it (GAME_FEEL_BIBLE C3: "It tips 10° up out of its cradle on hover").</summary>
    public const float HoverTipDegrees = 10f;

    /// <summary>How far the grip or the eye moves (metres) before the hold is worked out again (Hold is not run every frame).</summary>
    public const float ResolveDistance = 0.005f;

    /// <summary>The farthest the hold turns the beam off the straight-ahead line (degrees each way).</summary>
    public const float MaxYaw = 60f;

    /// <summary>The steps the hold tries (degrees of yaw, degrees of tilt), and what a degree of turn costs against a degree of tilt.</summary>
    private const float YawStep = 5f, TiltStep = 2f, YawCost = 0.5f;

    /// <summary>When no aim reaches the target: how many of the nearest aims are measured in full for the best one.</summary>
    private const int FallbackTries = 120;

    /// <summary>The patch's samples: its centre and three rings (8, 12, 16 points).</summary>
    private static readonly (float ring, int count)[] Rings = { (1f / 3f, 8), (2f / 3f, 12), (1f, 16) };

    /// <summary>How many samples the patch has (its centre and the rings').</summary>
    private const int Samples = 1 + 8 + 12 + 16;

    /// <summary>The aims in the order the hold tries them, for the preferred tilt they were sorted for.</summary>
    private static (float tilt, float yaw)[] _order;

    private static float _orderFor = float.NaN;

    /// <summary>
    /// The torch's body as a capsule axis: held by its grip at
    /// <paramref name="grip"/>, its lens pointing at <paramref name="aim"/>,
    /// the lens end <paramref name="lensLength"/> ahead of the grip and the
    /// tail cap <paramref name="tailLength"/> behind it along that line.
    /// </summary>
    public static void Body(TorchPoint grip, TorchPoint aim, float tailLength, float lensLength, out TorchPoint tail, out TorchPoint lens)
    {
        float dx = aim.X - grip.X, dy = aim.Y - grip.Y, dz = aim.Z - grip.Z;
        float length = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (length < 1e-6f)
        {
            dx = 0f;
            dy = -1f;
            dz = 0f;
        }
        else
        {
            dx /= length;
            dy /= length;
            dz /= length;
        }
        lens = new TorchPoint(grip.X + dx * lensLength, grip.Y + dy * lensLength, grip.Z + dz * lensLength);
        tail = new TorchPoint(grip.X - dx * tailLength, grip.Y - dy * tailLength, grip.Z - dz * tailLength);
    }

    /// <summary>
    /// The share (0 to 1) of the lit patch (a disc of <paramref name="radius"/>
    /// round <paramref name="centre"/>, level) the eye at <paramref name="eye"/>
    /// sees past the torch's body (a capsule of <paramref name="bodyRadius"/>
    /// round the segment from <paramref name="tail"/> to <paramref name="lens"/>):
    /// a sample of the patch is hidden when the line from the eye to it passes
    /// through the body. Sampled at its centre and on three rings (37 points; no allocation).
    /// </summary>
    public static float Unoccluded(TorchPoint eye, TorchPoint tail, TorchPoint lens, float bodyRadius, TorchPoint centre, float radius) =>
        1f - Hidden(eye, tail, lens, bodyRadius, centre, radius, Samples) / (float)Samples;

    /// <summary>
    /// How to hold the torch for the eye at <paramref name="eye"/>: the grip
    /// at (<paramref name="gripX"/>, <paramref name="deskY"/> + <paramref name="height"/>,
    /// <paramref name="gripZ"/>), the beam pointing away from the chair along
    /// (<paramref name="awayX"/>, <paramref name="awayZ"/>) onto the desk at
    /// <paramref name="deskY"/>. Of the aims (a tilt within UvCone's range, a
    /// turn of at most <see cref="MaxYaw"/>) whose patch the eye sees past the
    /// body (<see cref="TargetShare"/>), the one nearest the built
    /// <paramref name="preferredTilt"/> straight ahead (a degree of turn
    /// costing half a degree of tilt); when none does, the one that shows the
    /// most of it. The body's numbers are the torch's own
    /// (<paramref name="tailLength"/>, <paramref name="lensLength"/>,
    /// <paramref name="bodyRadius"/>) and the patch's radius the cone's.
    /// </summary>
    public static TorchAim Hold(TorchPoint eye, float gripX, float gripZ, float deskY, float height, float awayX, float awayZ, float preferredTilt,
                                float tailLength, float lensLength, float bodyRadius, float coneRadius)
    {
        float away = (float)Math.Sqrt(awayX * awayX + awayZ * awayZ);
        if (away < 1e-6f)
        {
            awayX = 0f;
            awayZ = 1f;
        }
        else
        {
            awayX /= away;
            awayZ /= away;
        }
        var grip = new TorchPoint(gripX, deskY + height, gripZ);
        (float tilt, float yaw)[] order = Order(Clamp(preferredTilt, UvCone.MinTilt, UvCone.MaxTilt));
        // The nearest aim that reaches the target; a try gives up at its first hidden sample past the target's allowance.
        int allowed = (int)Math.Floor((1f - TargetShare) * Samples + 1e-4f);
        foreach ((float tilt, float yaw) in order)
        {
            AimAt(grip, gripX, gripZ, deskY, height, awayX, awayZ, tilt, yaw, tailLength, lensLength, out TorchPoint aim, out TorchPoint tail, out TorchPoint lens);
            int hidden = Hidden(eye, tail, lens, bodyRadius, aim, coneRadius, allowed + 1);
            if (hidden <= allowed)
                return new TorchAim(tilt, yaw, 1f - hidden / (float)Samples);
        }
        // None does (an eye that looks along the beam from close by): the nearest that shows the most.
        TorchAim best = default;
        for (int i = 0; i < order.Length && i < FallbackTries; i++)
        {
            (float tilt, float yaw) = order[i];
            AimAt(grip, gripX, gripZ, deskY, height, awayX, awayZ, tilt, yaw, tailLength, lensLength, out TorchPoint aim, out TorchPoint tail, out TorchPoint lens);
            float share = Unoccluded(eye, tail, lens, bodyRadius, aim, coneRadius);
            if (i == 0 || share > best.Unoccluded)
                best = new TorchAim(tilt, yaw, share);
        }
        return best;
    }

    /// <summary>The patch's centre and the body's axis for the grip at <paramref name="grip"/> held at <paramref name="tilt"/>, turned <paramref name="yaw"/> off away (normalised).</summary>
    private static void AimAt(TorchPoint grip, float gripX, float gripZ, float deskY, float height, float awayX, float awayZ, float tilt, float yaw,
                              float tailLength, float lensLength, out TorchPoint aim, out TorchPoint tail, out TorchPoint lens)
    {
        double radians = yaw * Math.PI / 180.0;
        float c = (float)Math.Cos(radians), s = (float)Math.Sin(radians);
        // Turned to the right of away (seen from above, y up): right of (ax, az) is (az, -ax).
        float dirX = awayX * c + awayZ * s, dirZ = awayZ * c - awayX * s;
        (float x, float z) = UvCone.Ahead(gripX, gripZ, dirX, dirZ, height, tilt);
        aim = new TorchPoint(x, deskY, z);
        Body(grip, aim, tailLength, lensLength, out tail, out lens);
    }

    /// <summary>How many of the patch's samples the body hides from the eye, counting no further than <paramref name="stopAt"/>.</summary>
    private static int Hidden(TorchPoint eye, TorchPoint tail, TorchPoint lens, float bodyRadius, TorchPoint centre, float radius, int stopAt)
    {
        int hidden = Seen(eye, tail, lens, bodyRadius, centre.X, centre.Y, centre.Z) ? 0 : 1;
        foreach ((float ring, int count) in Rings)
            for (int k = 0; k < count && hidden < stopAt; k++)
            {
                double a = 2.0 * Math.PI * k / count;
                float x = centre.X + radius * ring * (float)Math.Cos(a), z = centre.Z + radius * ring * (float)Math.Sin(a);
                if (!Seen(eye, tail, lens, bodyRadius, x, centre.Y, z))
                    hidden++;
            }
        return hidden;
    }

    /// <summary>True when the cone's centre (<paramref name="aimX"/>, <paramref name="aimZ"/>) lies on the desk: its nearest point on the desk area (<paramref name="onDeskX"/>, <paramref name="onDeskZ"/>, DeskSurface.Clamp) is the centre itself, within a millimetre. Only then is the pool drawn and the reveal lit (a miss: nothing under the beam to light, BUG-2c's floating oval).</summary>
    public static bool PoolShows(float aimX, float aimZ, float onDeskX, float onDeskZ)
    {
        float dx = aimX - onDeskX, dz = aimZ - onDeskZ;
        return dx * dx + dz * dz <= 1e-6f;
    }

    /// <summary>
    /// The held grip after a frame of <paramref name="dt"/> seconds, from
    /// (<paramref name="x"/>, <paramref name="z"/>) toward the pointer at
    /// (<paramref name="toX"/>, <paramref name="toZ"/>): it eases after it
    /// (<see cref="LagSeconds"/>) and never trails it by more than
    /// <see cref="MaxLag"/>.
    /// </summary>
    public static (float x, float z) Follow(float x, float z, float toX, float toZ, float dt)
    {
        float k = 1f - (float)Math.Exp(-Math.Max(0f, dt) / LagSeconds);
        float nx = x + (toX - x) * k, nz = z + (toZ - z) * k;
        float lagX = nx - toX, lagZ = nz - toZ;
        float lag = (float)Math.Sqrt(lagX * lagX + lagZ * lagZ);
        if (lag > MaxLag)
        {
            nx = toX + lagX / lag * MaxLag;
            nz = toZ + lagZ / lag * MaxLag;
        }
        return (nx, nz);
    }

    /// <summary>The reveal's strength after a frame of <paramref name="dt"/>: it rises over <see cref="FluoresceSeconds"/> while the torch is lit (<paramref name="on"/>; at once when <paramref name="still"/>, Reduced Motion) and is gone at once when it is not.</summary>
    public static float Fluoresce(float glow, bool on, float dt, bool still)
    {
        if (!on)
            return 0f;
        if (still)
            return 1f;
        return Math.Min(1f, Math.Max(0f, glow) + Math.Max(0f, dt) / FluoresceSeconds);
    }

    /// <summary>The share of the way a soft follower closes in a frame of <paramref name="dt"/> with time constant <paramref name="seconds"/> (1 at once for none).</summary>
    public static float Ease(float dt, float seconds) => seconds <= 0f ? 1f : 1f - (float)Math.Exp(-Math.Max(0f, dt) / seconds);

    /// <summary>True when the line from the eye to the patch point (x, y, z) clears the body.</summary>
    private static bool Seen(TorchPoint eye, TorchPoint tail, TorchPoint lens, float bodyRadius, float x, float y, float z) =>
        SegmentDistance(eye.X, eye.Y, eye.Z, x, y, z, tail.X, tail.Y, tail.Z, lens.X, lens.Y, lens.Z) >= bodyRadius;

    /// <summary>The shortest distance between the segments p1-q1 and p2-q2 (the closest points of two segments).</summary>
    private static float SegmentDistance(float p1x, float p1y, float p1z, float q1x, float q1y, float q1z, float p2x, float p2y, float p2z, float q2x, float q2y, float q2z)
    {
        float d1x = q1x - p1x, d1y = q1y - p1y, d1z = q1z - p1z;
        float d2x = q2x - p2x, d2y = q2y - p2y, d2z = q2z - p2z;
        float rx = p1x - p2x, ry = p1y - p2y, rz = p1z - p2z;
        float a = d1x * d1x + d1y * d1y + d1z * d1z, e = d2x * d2x + d2y * d2y + d2z * d2z, f = d2x * rx + d2y * ry + d2z * rz;
        const float eps = 1e-12f;
        float s, t;
        if (a <= eps && e <= eps)
        {
            s = t = 0f;
        }
        else if (a <= eps)
        {
            s = 0f;
            t = Clamp(f / e, 0f, 1f);
        }
        else
        {
            float c = d1x * rx + d1y * ry + d1z * rz;
            if (e <= eps)
            {
                t = 0f;
                s = Clamp(-c / a, 0f, 1f);
            }
            else
            {
                float b = d1x * d2x + d1y * d2y + d1z * d2z, denom = a * e - b * b;
                s = denom > eps ? Clamp((b * f - c * e) / denom, 0f, 1f) : 0f;
                t = (b * s + f) / e;
                if (t < 0f)
                {
                    t = 0f;
                    s = Clamp(-c / a, 0f, 1f);
                }
                else if (t > 1f)
                {
                    t = 1f;
                    s = Clamp((b - c) / a, 0f, 1f);
                }
            }
        }
        float dx = rx + d1x * s - d2x * t, dy = ry + d1y * s - d2y * t, dz = rz + d1z * s - d2z * t;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>The aims in the order the hold tries them for <paramref name="preferred"/>: nearest first (tilt off the preferred plus half the turn), straight ahead before a turn.</summary>
    private static (float tilt, float yaw)[] Order(float preferred)
    {
        if (_order != null && _orderFor == preferred)
            return _order;
        int tilts = (int)Math.Floor((UvCone.MaxTilt - UvCone.MinTilt) / TiltStep) + 1, yaws = (int)(2f * MaxYaw / YawStep) + 1;
        var order = new (float tilt, float yaw, float cost)[tilts * yaws + 1];
        int n = 0;
        order[n++] = (preferred, 0f, -1f); // the built hold first, exactly
        for (int i = 0; i < tilts; i++)
            for (int j = 0; j < yaws; j++)
            {
                float tilt = UvCone.MinTilt + i * TiltStep, yaw = -MaxYaw + j * YawStep;
                order[n++] = (tilt, yaw, Math.Abs(tilt - preferred) + YawCost * Math.Abs(yaw));
            }
        Array.Sort(order, (p, q) =>
        {
            int by = p.cost.CompareTo(q.cost);
            if (by == 0)
                by = Math.Abs(p.yaw).CompareTo(Math.Abs(q.yaw));
            if (by == 0)
                by = p.yaw.CompareTo(q.yaw);
            return by != 0 ? by : p.tilt.CompareTo(q.tilt);
        });
        var result = new (float tilt, float yaw)[n];
        for (int i = 0; i < n; i++)
            result[i] = (order[i].tilt, order[i].yaw);
        _order = result;
        _orderFor = preferred;
        return result;
    }

    private static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
}
