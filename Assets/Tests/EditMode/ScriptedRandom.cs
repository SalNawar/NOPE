using NUnit.Framework;

/// <summary>One scripted answer for <see cref="ScriptedRandom"/>: a Value() roll or a Range() offset.</summary>
public readonly struct ScriptStep
{
    /// <summary>True for a Value() answer, false for a Range() answer.</summary>
    public readonly bool IsValue;

    /// <summary>The Value() answer.</summary>
    public readonly float Roll;

    /// <summary>The Range() answer, as an offset from minInclusive (clamped into the range).</summary>
    public readonly int Offset;

    private ScriptStep(bool isValue, float roll, int offset)
    {
        IsValue = isValue;
        Roll = roll;
        Offset = offset;
    }

    /// <summary>Answers the next draw, which must be a Value() draw, with <paramref name="roll"/>.</summary>
    public static ScriptStep Value(float roll) => new ScriptStep(true, roll, 0);

    /// <summary>Answers the next draw, which must be a Range() draw, with minInclusive + <paramref name="offset"/> (clamped).</summary>
    public static ScriptStep Range(int offset) => new ScriptStep(false, 0f, offset);
}

/// <summary>
/// The suite's one scripted random source: answers draws from an ordered
/// script, so tests can pin the exact draw order. Each draw takes the next
/// step; a draw of the other kind than the next step, or a draw past the end
/// of the script, fails the test.
/// </summary>
public sealed class ScriptedRandom : IRandomSource
{
    /// <summary>The answers, in draw order.</summary>
    private readonly ScriptStep[] _script;

    /// <summary>Creates a source that answers with <paramref name="script"/>, in order.</summary>
    public ScriptedRandom(params ScriptStep[] script)
    {
        _script = script ?? new ScriptStep[0];
    }

    /// <summary>Number of draws made so far.</summary>
    public int Draws { get; private set; }

    /// <summary>True when every scripted step has been drawn.</summary>
    public bool Done => Draws == _script.Length;

    /// <inheritdoc />
    public int Range(int minInclusive, int maxExclusive)
    {
        ScriptStep step = Take(false);
        if (maxExclusive <= minInclusive)
            return minInclusive;

        long answer = (long)minInclusive + step.Offset;
        return answer < minInclusive ? minInclusive : answer >= maxExclusive ? maxExclusive - 1 : (int)answer;
    }

    /// <inheritdoc />
    public float Value() => Take(true).Roll;

    /// <summary>The next step; fails the test on a draw of the wrong kind or past the end.</summary>
    private ScriptStep Take(bool wantValue)
    {
        string kind = wantValue ? "Value" : "Range";
        if (Draws >= _script.Length)
            Assert.Fail($"Draw {Draws + 1} ({kind}) is past the end of the {_script.Length}-step script.");

        ScriptStep step = _script[Draws];
        if (step.IsValue != wantValue)
            Assert.Fail($"Draw {Draws + 1} is a {kind} draw, but the script expects a {(step.IsValue ? "Value" : "Range")} draw.");

        Draws++;
        return step;
    }
}
