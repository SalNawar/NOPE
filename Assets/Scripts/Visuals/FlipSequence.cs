using System;

/// <summary>The letter flip's knobs (ContentLibrarySO.Translation.flip, authored in world_source.json translation.flip).</summary>
[Serializable]
public sealed class FlipTiming
{
    /// <summary>Seconds from the reveal to the first letter starting.</summary>
    public float startDelay = 0.3f;

    /// <summary>Seconds between two letters starting, in reading order.</summary>
    public float letterInterval = 0.04f;

    /// <summary>Seconds a letter takes from starting to landing on its English letter.</summary>
    public float letterSeconds = 0.12f;

    /// <summary>Glyphs of its tongue a letter passes through on the way (0 = none: the foreign letter stays until it lands).</summary>
    public int scrambleSteps = 2;
}

/// <summary>Where a letter is in its flip.</summary>
public enum CellState
{
    /// <summary>Still the tongue's glyph.</summary>
    Foreign,

    /// <summary>Passing through its scramble glyphs.</summary>
    Flipping,

    /// <summary>Landed on its English letter.</summary>
    English
}

/// <summary>
/// The letter flip's timing (piece 9 T8; speech only since the redesign's
/// phase 1, papers being always English): letter k starts at startDelay +
/// k × letterInterval, passes through its scramble steps and lands
/// letterSeconds later. Only letters count (their rank skips everything
/// else). Negative knobs count as 0; an elapsed time that is NaN (not
/// revealed yet) is before every start.
/// </summary>
public static class FlipSequence
{
    /// <summary>The stride between scramble glyphs: any number coprime with 26, so consecutive steps differ from each other and from the letter.</summary>
    private const int ScrambleStride = 7;

    /// <summary>When the letter of this rank starts.</summary>
    public static float StartOf(int letterRank, FlipTiming t) =>
        NonNegative(t.startDelay) + Math.Max(0, letterRank) * NonNegative(t.letterInterval);

    /// <summary>A letter's state <paramref name="elapsed"/> seconds after the reveal; <paramref name="step"/> is its scramble step while Flipping (0 .. scrambleSteps-1), else 0.</summary>
    public static CellState StateAt(int letterRank, FlipTiming t, float elapsed, out int step)
    {
        step = 0;
        float start = StartOf(letterRank, t);
        float seconds = NonNegative(t.letterSeconds);
        if (!(elapsed >= start))
            return CellState.Foreign;
        if (elapsed >= start + seconds)
            return CellState.English;

        int steps = Math.Max(0, t.scrambleSteps);
        if (steps == 0)
            return CellState.Foreign;

        step = Math.Min(steps - 1, (int)((elapsed - start) / (seconds / steps)));
        return CellState.Flipping;
    }

    /// <summary>The table index a flipping letter shows at a step: (letter + 7 × (step + 1)) mod 26.</summary>
    public static int ScrambleIndex(int letter, int step) =>
        ((letter + ScrambleStride * (step + 1)) % Pseudoscript.TableSize + Pseudoscript.TableSize) % Pseudoscript.TableSize;

    /// <summary>When the last of <paramref name="letterCount"/> letters lands (0 for no letters).</summary>
    public static float Duration(int letterCount, FlipTiming t) =>
        letterCount <= 0 ? 0f : StartOf(letterCount - 1, t) + NonNegative(t.letterSeconds);

    /// <summary>
    /// How many change points (a letter starting, each scramble step, a
    /// letter landing) have passed for a text of <paramref name="letterCount"/>
    /// letters: the shown text changes exactly when this does.
    /// </summary>
    public static int Progress(int letterCount, FlipTiming t, float elapsed)
    {
        int progress = 0;
        int landed = Math.Max(0, t.scrambleSteps) + 1;
        for (int rank = 0; rank < letterCount; rank++)
        {
            CellState state = StateAt(rank, t, elapsed, out int step);
            if (state == CellState.Foreign)
                break; // later letters start later still
            progress += state == CellState.English ? landed : step + 1;
        }
        return progress;
    }

    /// <summary>A knob below 0 (or NaN) counts as 0.</summary>
    private static float NonNegative(float value) => value > 0f ? value : 0f;
}
