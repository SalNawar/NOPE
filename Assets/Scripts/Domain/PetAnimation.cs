using System;
using System.Collections.Generic;

/// <summary>One step of a pet's frame-by-frame animation: a drawn frame ("rest" is the mood's own picture) held for <see cref="hold"/> ticks.</summary>
[Serializable]
public sealed class PetFrameHold
{
    /// <summary>The frame's name ("rest", "blink", "tail_a", ...: Home/pet_&lt;kind&gt;_&lt;coat&gt;_&lt;mood&gt;_&lt;frame&gt;).</summary>
    public string frame;

    /// <summary>How many ticks it is held (at PetAnimations.fps; at least one).</summary>
    public int hold;
}

/// <summary>A mood's loop for one kind: its steps, played over and over.</summary>
[Serializable]
public sealed class PetLoop
{
    /// <summary>The kind's name (PetKind: "Dog", "Cat").</summary>
    public string kind;

    /// <summary>The look's name (PetLook: "Idle", "Happy", "Sad", "Sick").</summary>
    public string mood;

    /// <summary>Its steps.</summary>
    public PetFrameHold[] frames;
}

/// <summary>A reaction's steps (PetReaction), played once over the loop, then the loop again.</summary>
[Serializable]
public sealed class PetReactionLoop
{
    /// <summary>The reaction's name (PetReaction: "Pat", "Fed", "Play").</summary>
    public string reaction;

    /// <summary>Its steps.</summary>
    public PetFrameHold[] frames;
}

/// <summary>What just happened to the pet: the reaction its animation plays once.</summary>
public enum PetReaction
{
    /// <summary>Nothing: its mood's loop alone.</summary>
    None,

    /// <summary>Petted: its eyes shut, contented, a wag between.</summary>
    Pat,

    /// <summary>Fed (the corner opened on a night its food was paid): a happy wag.</summary>
    Fed,

    /// <summary>Played with a toy: a fast wag and an ear twitch.</summary>
    Play
}

/// <summary>
/// The pets' frame-by-frame animation as Home/pet_loops.json holds it
/// (JsonUtility; Saleh 2026-10-17: "just make normal animations", approved
/// "Yes, do all moods"): the ticks a second, each kind's loop per mood and
/// the reactions. The frames are drawn (tools/art/pets/make_pet_frames.py);
/// a step is held, never in-betweened, scaled or moved.
/// </summary>
[Serializable]
public sealed class PetAnimations
{
    /// <summary>Ticks a second.</summary>
    public int fps;

    /// <summary>The loops, one per kind and mood.</summary>
    public PetLoop[] loops;

    /// <summary>The reactions.</summary>
    public PetReactionLoop[] reactions;

    /// <summary>The steps of <paramref name="kind"/>'s loop for <paramref name="look"/>, or none.</summary>
    public PetFrameHold[] Loop(PetKind kind, PetLook look)
    {
        foreach (PetLoop l in loops ?? Array.Empty<PetLoop>())
            if (l != null && l.kind == kind.ToString() && l.mood == look.ToString())
                return l.frames ?? Array.Empty<PetFrameHold>();
        return Array.Empty<PetFrameHold>();
    }

    /// <summary>The steps of <paramref name="reaction"/>, or none (None has none).</summary>
    public PetFrameHold[] Reaction(PetReaction reaction)
    {
        foreach (PetReactionLoop r in reactions ?? Array.Empty<PetReactionLoop>())
            if (r != null && r.reaction == reaction.ToString())
                return r.frames ?? Array.Empty<PetFrameHold>();
        return Array.Empty<PetFrameHold>();
    }

    /// <summary>Every frame <paramref name="kind"/> in <paramref name="look"/> may show (its loop's and every reaction's), "rest" first.</summary>
    public List<string> FrameNames(PetKind kind, PetLook look)
    {
        var names = new List<string> { PetAnimation.Rest };
        void Add(PetFrameHold[] steps)
        {
            foreach (PetFrameHold s in steps)
                if (s != null && !string.IsNullOrEmpty(s.frame) && !names.Contains(s.frame))
                    names.Add(s.frame);
        }
        Add(Loop(kind, look));
        foreach (PetReaction r in (PetReaction[])Enum.GetValues(typeof(PetReaction)))
            Add(Reaction(r));
        return names;
    }
}

/// <summary>Which drawn frame a pet shows on a tick (pure).</summary>
public static class PetAnimation
{
    /// <summary>The mood's own picture, the frame every loop rests on and the one a missing frame falls back to.</summary>
    public const string Rest = "rest";

    /// <summary>The ticks <paramref name="steps"/> last (each step at least one).</summary>
    public static int Length(PetFrameHold[] steps)
    {
        int n = 0;
        foreach (PetFrameHold s in steps ?? Array.Empty<PetFrameHold>())
            n += s != null ? Math.Max(1, s.hold) : 0;
        return n;
    }

    /// <summary>
    /// The frame on tick <paramref name="tick"/> (0 at the start) of
    /// <paramref name="steps"/>: played over and over when
    /// <paramref name="loop"/>, else once, null after its end. "rest" with
    /// no steps (a looped empty loop) and before tick 0.
    /// </summary>
    public static string FrameAt(PetFrameHold[] steps, long tick, bool loop)
    {
        int length = Length(steps);
        if (length == 0)
            return loop ? Rest : null;
        if (tick < 0)
            return Rest;
        if (!loop && tick >= length)
            return null;
        long t = tick % length;
        foreach (PetFrameHold s in steps)
        {
            if (s == null)
                continue;
            t -= Math.Max(1, s.hold);
            if (t < 0)
                return string.IsNullOrEmpty(s.frame) ? Rest : s.frame;
        }
        return Rest;
    }
}
