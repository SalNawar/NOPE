/// <summary>
/// Engine-free random source, so rule code (case generation, weighted picks,
/// birth dates) can run seeded and headless. Mirrors UnityEngine.Random's
/// tolerant semantics.
/// </summary>
public interface IRandomSource
{
    /// <summary>Integer in [minInclusive, maxExclusive); returns minInclusive when max ≤ min (never throws).</summary>
    int Range(int minInclusive, int maxExclusive);

    /// <summary>Float in [0, 1).</summary>
    float Value();
}

/// <summary>
/// A random source that answers one value (0 to 1): a WeightedRandom.Pick
/// whose roll is a value the caller already has (a seed's value, or a draw
/// the caller made on its own stream), so the pick draws nothing itself.
/// </summary>
public sealed class FixedRandom : IRandomSource
{
    private readonly float _value;

    /// <summary>A source answering <paramref name="value"/>.</summary>
    public FixedRandom(float value) => _value = value;

    /// <summary>minInclusive plus the value's share of the range (minInclusive when max is at most min).</summary>
    public int Range(int minInclusive, int maxExclusive) => maxExclusive <= minInclusive ? minInclusive : minInclusive + (int)(_value * (maxExclusive - minInclusive));

    /// <summary>The value.</summary>
    public float Value() => _value;
}
