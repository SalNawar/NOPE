using System;

/// <summary>
/// A bone of a pet's puppet rig (PetRig), in its picture's own coordinates
/// (0..1, x right, y down): the <see cref="pivot"/> it turns about and the
/// ellipse (<see cref="center"/>, <see cref="radius"/>) of the fur it moves,
/// fully inside 0.6 of the radius and fading to nothing at its edge; a
/// tail's or an ear's <see cref="tip"/> (a tail bends more towards it).
/// Read from Home/pet_rigs.json (JsonUtility).
/// </summary>
[Serializable]
public sealed class PetBone
{
    /// <summary>The point it turns about (x, y).</summary>
    public float[] pivot;

    /// <summary>The centre of the fur it moves (x, y).</summary>
    public float[] center;

    /// <summary>The radii of the fur it moves (x, y).</summary>
    public float[] radius;

    /// <summary>Its tip (a tail's, an ear's; x, y), or none.</summary>
    public float[] tip;

    /// <summary>True when the bone is drawn (a pivot and a positive radius each way); a rig's missing tail is not.</summary>
    public bool Present => pivot != null && pivot.Length == 2 && center != null && center.Length == 2 && radius != null && radius.Length == 2 && radius[0] > 0f && radius[1] > 0f;

    /// <summary>How much of the point (<paramref name="x"/>, <paramref name="y"/>) it moves: 1 inside 0.6 of its ellipse, easing to 0 at the edge; 0 when absent.</summary>
    public float Weight(float x, float y)
    {
        if (!Present)
            return 0f;
        float dx = (x - center[0]) / radius[0], dy = (y - center[1]) / radius[1];
        return 1f - PetAnimator.SmoothStep(0.6f, 1f, (float)Math.Sqrt(dx * dx + dy * dy));
    }
}

/// <summary>One eye of a pet's picture (where the blink's closed lid is painted, tools/art/pets/make_pets.py).</summary>
[Serializable]
public sealed class PetEye
{
    /// <summary>Its centre (x, y).</summary>
    public float[] center;

    /// <summary>Its radii (x, y).</summary>
    public float[] radius;
}

/// <summary>
/// The puppet rig of one of a kind's pictures (one per mood; every coat of
/// that mood shares it): the head (the ears ride on it), the ears, the tail
/// (none when the picture hides it), the body (it breathes about its pivot,
/// the ground under it) and the eyes (none when the picture's eyes are shut,
/// so it never blinks). Saleh 2026-10-17: the pets "animate by squesh and
/// squeez instead of a real animation".
/// </summary>
[Serializable]
public sealed class PetRig
{
    /// <summary>The kind's name (PetKind: "Dog", "Cat").</summary>
    public string kind;

    /// <summary>The look's name (PetLook: "Idle", "Happy", "Sad", "Sick").</summary>
    public string mood;

    /// <summary>The head: tilts, droops and bobs.</summary>
    public PetBone head;

    /// <summary>The ears: flick (and ride on the head).</summary>
    public PetBone[] ears;

    /// <summary>The tail: wags, bending towards its tip.</summary>
    public PetBone tail;

    /// <summary>The body: breathes.</summary>
    public PetBone body;

    /// <summary>The open eyes (none: the picture's eyes are shut, no blink).</summary>
    public PetEye[] eyes;

    /// <summary>True when the picture's eyes are open (its blink picture exists).</summary>
    public bool CanBlink => eyes != null && eyes.Length > 0;
}

/// <summary>A mood's loop (PetAnimator.Pose): breathing, the head's sway and droop, the tail's swing, the ears' flicks, the blinks, the hops and the shivers; periods in seconds, angles in degrees, distances in picture heights, "every" 0 for never.</summary>
[Serializable]
public sealed class PetMoodMotion
{
    /// <summary>The look's name (PetLook).</summary>
    public string mood;

    /// <summary>A breath's length and its depth (the chest's stretch).</summary>
    public float breathPeriod, breathDepth;

    /// <summary>The head's sway (degrees) and its period.</summary>
    public float headSway, headSwayPeriod;

    /// <summary>How far the head sinks and rises (a sad pet's sighs).</summary>
    public float headDroop;

    /// <summary>The tail's swing (degrees) and its period.</summary>
    public float tailSwing, tailPeriod;

    /// <summary>About how often an ear flicks, and how far.</summary>
    public float earFlickEvery, earFlick;

    /// <summary>About how often it blinks, and how long its eyes stay shut.</summary>
    public float blinkEvery, blinkLength;

    /// <summary>About how often it hops, and how high.</summary>
    public float hopEvery, hopHeight;

    /// <summary>About how often a sick pet shivers.</summary>
    public float shiverEvery;
}

/// <summary>What a pet does when something happens to it (PetReaction): over <see cref="length"/> seconds its head tilts and dips, bobs (eating), its tail swings, it hops, its eyes shut (being petted).</summary>
[Serializable]
public sealed class PetReactionMotion
{
    /// <summary>The reaction's name (PetReaction).</summary>
    public string reaction;

    /// <summary>How long it lasts (seconds).</summary>
    public float length;

    /// <summary>The head's tilt (degrees) and dip (picture heights) at its height.</summary>
    public float headTilt, headDip;

    /// <summary>How many times the head bobs (eating).</summary>
    public int bobs;

    /// <summary>The tail's swing (degrees) and its period while it lasts.</summary>
    public float tailSwing, tailPeriod;

    /// <summary>How many hops, and how high.</summary>
    public int hops;

    /// <summary>A hop's height (picture heights).</summary>
    public float hopHeight;

    /// <summary>True when the eyes shut while it lasts (contentment).</summary>
    public bool eyesShut;
}

/// <summary>What just happened to the pet (its reaction's motion, PetRigs.Reaction).</summary>
public enum PetReaction
{
    /// <summary>Nothing: its mood's loop alone.</summary>
    None,

    /// <summary>Petted: leans into the hand, eyes shut, tail going.</summary>
    Pat,

    /// <summary>Fed (the night's food paid): head down to the bowl, bobbing as it eats.</summary>
    Fed,

    /// <summary>Played with a toy: hops, tail going.</summary>
    Play
}

/// <summary>The rigs and the motion's knobs, as Home/pet_rigs.json holds them (JsonUtility); the lookups.</summary>
[Serializable]
public sealed class PetRigs
{
    /// <summary>One rig per kind and mood.</summary>
    public PetRig[] rigs;

    /// <summary>One loop per mood.</summary>
    public PetMoodMotion[] moods;

    /// <summary>One motion per reaction.</summary>
    public PetReactionMotion[] reactions;

    /// <summary>The rig of <paramref name="kind"/>'s picture for <paramref name="look"/>, or null.</summary>
    public PetRig Rig(PetKind kind, PetLook look)
    {
        foreach (PetRig r in rigs ?? Array.Empty<PetRig>())
            if (r != null && r.kind == kind.ToString() && r.mood == look.ToString())
                return r;
        return null;
    }

    /// <summary>The loop for <paramref name="look"/>, or null.</summary>
    public PetMoodMotion Mood(PetLook look)
    {
        foreach (PetMoodMotion m in moods ?? Array.Empty<PetMoodMotion>())
            if (m != null && m.mood == look.ToString())
                return m;
        return null;
    }

    /// <summary>The motion of <paramref name="reaction"/>, or null (None has none).</summary>
    public PetReactionMotion Reaction(PetReaction reaction)
    {
        foreach (PetReactionMotion m in reactions ?? Array.Empty<PetReactionMotion>())
            if (m != null && m.reaction == reaction.ToString())
                return m;
        return null;
    }
}

/// <summary>Where a pet's puppet is at a moment (PetAnimator.Pose): the breath's stretch, the head's tilt (degrees, clockwise) and offset, each ear's flick, the tail's angle, the hop's height and the shiver (picture heights), and whether the eyes are shut.</summary>
public readonly struct PetPose
{
    /// <summary>The chest's stretch (a fraction: 0.012 is 1.2 %).</summary>
    public readonly float Breath;

    /// <summary>The head's tilt (degrees, clockwise on screen).</summary>
    public readonly float HeadAngle;

    /// <summary>The head's offset down (picture heights).</summary>
    public readonly float HeadDrop;

    /// <summary>The first and second ear's flick (degrees).</summary>
    public readonly float Ear0, Ear1;

    /// <summary>The tail's angle (degrees, at its tip).</summary>
    public readonly float TailAngle;

    /// <summary>How high it is off the ground (picture heights).</summary>
    public readonly float Hop;

    /// <summary>The shiver's sideways shake (picture widths).</summary>
    public readonly float Shiver;

    /// <summary>True while its eyes are shut (a blink, or petted).</summary>
    public readonly bool Blink;

    /// <summary>A pose.</summary>
    public PetPose(float breath, float headAngle, float headDrop, float ear0, float ear1, float tailAngle, float hop, float shiver, bool blink)
    {
        Breath = breath;
        HeadAngle = headAngle;
        HeadDrop = headDrop;
        Ear0 = ear0;
        Ear1 = ear1;
        TailAngle = tailAngle;
        Hop = hop;
        Shiver = shiver;
        Blink = blink;
    }
}

/// <summary>
/// The pets' animation (Saleh 2026-10-17: "dog and cat animate by squesh and
/// squeez instead of a real animation"): a puppet rig per picture (PetRig)
/// whose bones bend the picture's mesh (Deform), posed each frame by its
/// mood's loop and the reaction under way (Pose). The Unity 2D Animation
/// package is not installed, so the skinning is this (PetPuppet draws the
/// mesh). Pure: a pose is a function of the time, so a replay is the same.
/// </summary>
public static class PetAnimator
{
    /// <summary>How long an ear's flick, a hop and a shiver take (seconds).</summary>
    private const float FlickLength = 0.3f, HopLength = 0.45f, ShiverLength = 0.4f;

    /// <summary>A shiver's sideways shake (picture widths) and its rate (shakes a second).</summary>
    private const float ShiverSize = 0.004f, ShiverRate = 18f;

    /// <summary>How much a breath widens the chest against how much it lifts it.</summary>
    private const float BreathWidth = 0.35f;

    /// <summary>A bob's depth while eating (picture heights).</summary>
    private const float BobDepth = 0.012f;

    /// <summary>
    /// The pose at <paramref name="time"/> seconds: <paramref name="mood"/>'s
    /// loop (null: still), with <paramref name="reaction"/> under way for
    /// <paramref name="since"/> seconds (none, or past its length: the loop
    /// alone). <paramref name="seed"/> places the loop's flicks, blinks, hops
    /// and shivers in their windows (each "every" seconds, at a seeded point
    /// of the window), so two pets do not blink together.
    /// </summary>
    public static PetPose Pose(PetMoodMotion mood, PetReactionMotion reaction, float since, float time, int seed)
    {
        float breath = 0f, head = 0f, drop = 0f, ear0 = 0f, ear1 = 0f, tail = 0f, hop = 0f, shiver = 0f;
        bool blink = false;
        if (mood != null)
        {
            breath = mood.breathDepth * Wave(time, mood.breathPeriod, 0f);
            head = mood.headSway * Wave(time, mood.headSwayPeriod, 0.7f);
            drop = mood.headDroop * (0.5f + 0.5f * Wave(time, mood.headSwayPeriod * 1.3f, 0f));
            tail = mood.tailSwing * Wave(time, mood.tailPeriod, 0f);
            float flick = Pulse(time, mood.earFlickEvery, FlickLength, seed, 1, out int window);
            if (flick > 0f)
            {
                if (Hash(window, seed + 2) < 0.5f)
                    ear0 = mood.earFlick * flick;
                else
                    ear1 = mood.earFlick * flick;
            }
            blink = Pulse(time, mood.blinkEvery, mood.blinkLength, seed, 3, out _) > 0f;
            hop = mood.hopHeight * Pulse(time, mood.hopEvery, HopLength, seed, 4, out _);
            if (Pulse(time, mood.shiverEvery, ShiverLength, seed, 5, out _) > 0f)
                shiver = ShiverSize * (float)Math.Sin(2.0 * Math.PI * ShiverRate * time);
        }

        if (reaction != null && reaction.length > 0f && since >= 0f && since < reaction.length)
        {
            float s = since / reaction.length;
            float rise = (float)Math.Sin(Math.PI * s);
            head += reaction.headTilt * rise;
            drop += reaction.headDip * rise;
            if (reaction.bobs > 0)
                drop += BobDepth * rise * (float)Math.Abs(Math.Sin(Math.PI * reaction.bobs * s));
            tail = tail * (1f - rise) + reaction.tailSwing * Wave(since, reaction.tailPeriod, 0f) * rise;
            if (reaction.hops > 0)
                hop = Math.Max(hop, reaction.hopHeight * (float)Math.Abs(Math.Sin(Math.PI * reaction.hops * s)));
            if (reaction.eyesShut && s > 0.1f && s < 0.85f)
                blink = true;
        }
        return new PetPose(breath, head, drop, ear0, ear1, tail, hop, shiver, blink);
    }

    /// <summary>
    /// Where the picture's point (<paramref name="x"/>, <paramref name="y"/>;
    /// 0..1, y down) goes in <paramref name="pose"/> on <paramref name="rig"/>
    /// (null: nowhere): each ear turns about its pivot by its flick; the head
    /// (and the ears riding on it) turns about its pivot by its tilt and sinks
    /// by its drop; the tail turns about its pivot, more towards its tip; the
    /// body stretches about its pivot with the breath (the head rides on the
    /// breath at its pivot, so the neck never tears); then the whole pet hops
    /// and shivers.
    /// </summary>
    public static void Deform(PetRig rig, PetPose pose, float x, float y, out float outX, out float outY)
    {
        outX = x;
        outY = y;
        if (rig == null)
            return;

        float ears = 0f;
        PetBone[] earBones = rig.ears ?? Array.Empty<PetBone>();
        for (int i = 0; i < earBones.Length; i++)
        {
            PetBone ear = earBones[i];
            if (ear == null || !ear.Present)
                continue;
            float w = ear.Weight(x, y);
            ears = Math.Max(ears, w);
            Rotate(ref outX, ref outY, ear.pivot[0], ear.pivot[1], (i == 0 ? pose.Ear0 : pose.Ear1) * w);
        }

        float wh = 0f;
        if (rig.head != null && rig.head.Present)
        {
            wh = Math.Max(rig.head.Weight(x, y), ears);
            Rotate(ref outX, ref outY, rig.head.pivot[0], rig.head.pivot[1], pose.HeadAngle * wh);
            outY += pose.HeadDrop * wh;
        }

        PetBone tail = rig.tail;
        if (tail != null && tail.Present)
        {
            float w = tail.Weight(x, y);
            if (w > 0f)
            {
                float reach = 1f;
                if (tail.tip != null && tail.tip.Length == 2)
                {
                    float len = Distance(tail.pivot[0], tail.pivot[1], tail.tip[0], tail.tip[1]);
                    reach = len > 1e-4f ? Math.Min(1f, Distance(x, y, tail.pivot[0], tail.pivot[1]) / len) : 1f;
                }
                Rotate(ref outX, ref outY, tail.pivot[0], tail.pivot[1], pose.TailAngle * w * reach);
            }
        }

        PetBone body = rig.body;
        if (body != null && body.Present && pose.Breath != 0f)
        {
            Breathe(body, pose.Breath, x, y, out float bx, out float by);
            if (wh > 0f && rig.head.Present)
            {
                Breathe(body, pose.Breath, rig.head.pivot[0], rig.head.pivot[1], out float hx, out float hy);
                bx = bx * (1f - wh) + hx * wh;
                by = by * (1f - wh) + hy * wh;
            }
            outX += bx;
            outY += by;
        }

        outX += pose.Shiver;
        outY -= pose.Hop;
    }

    /// <summary>The breath's push at (<paramref name="x"/>, <paramref name="y"/>): away from the body's pivot, mostly upward, as much as the body moves the point.</summary>
    private static void Breathe(PetBone body, float breath, float x, float y, out float dx, out float dy)
    {
        float w = body.Weight(x, y);
        dx = (x - body.pivot[0]) * breath * BreathWidth * w;
        dy = (y - body.pivot[1]) * breath * w;
    }

    /// <summary>Turns (<paramref name="x"/>, <paramref name="y"/>) about (<paramref name="cx"/>, <paramref name="cy"/>) by <paramref name="degrees"/>, clockwise on screen (y down).</summary>
    private static void Rotate(ref float x, ref float y, float cx, float cy, float degrees)
    {
        if (degrees == 0f)
            return;
        double a = degrees * Math.PI / 180.0;
        float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
        float dx = x - cx, dy = y - cy;
        x = cx + dx * c - dy * s;
        y = cy + dx * s + dy * c;
    }

    /// <summary>The distance between two points.</summary>
    private static float Distance(float ax, float ay, float bx, float by) => (float)Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));

    /// <summary>A sine of <paramref name="period"/> seconds at <paramref name="time"/>, from <paramref name="phase"/> radians; 0 for no period.</summary>
    private static float Wave(float time, float period, float phase) => period > 0f ? (float)Math.Sin(2.0 * Math.PI * time / period + phase) : 0f;

    /// <summary>
    /// A seeded once-a-window event: windows of <paramref name="every"/>
    /// seconds, each holding one event of <paramref name="length"/> seconds at
    /// a seeded point of its first 60 %; its rise and fall (0..1, 0 outside
    /// the event, a half sine) and its window's number. 0 when it never happens.
    /// </summary>
    private static float Pulse(float time, float every, float length, int seed, int salt, out int window)
    {
        window = 0;
        if (every <= 0f || length <= 0f || time < 0f)
            return 0f;
        window = (int)Math.Floor(time / every);
        float start = window * every + Hash(window, seed * 31 + salt) * every * 0.6f;
        float into = time - start;
        return into >= 0f && into < length ? (float)Math.Sin(Math.PI * into / length) : 0f;
    }

    /// <summary>A number in 0..1 from <paramref name="n"/> and <paramref name="seed"/>, the same every time (an integer hash).</summary>
    private static float Hash(int n, int seed)
    {
        unchecked
        {
            uint h = (uint)n * 374761393u + (uint)seed * 668265263u;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }

    /// <summary>0 below <paramref name="edge0"/>, 1 above <paramref name="edge1"/>, a smooth S between.</summary>
    internal static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Math.Max(0f, Math.Min(1f, (x - edge0) / (edge1 - edge0)));
        return t * t * (3f - 2f * t);
    }
}
