/// <summary>
/// Deterministic random source (SplitMix64). Fully specified here, so a seed
/// replays the same sequence on every runtime and Unity version (unlike
/// System.Random, whose sequence is not guaranteed across .NET versions).
/// </summary>
public sealed class SeededRandom : IRandomSource
{
    /// <summary>Generator state.</summary>
    private ulong _state;

    /// <summary>Creates a source whose sequence is fixed by the seed.</summary>
    public SeededRandom(int seed)
    {
        _state = unchecked((ulong)(uint)seed);
    }

    /// <inheritdoc />
    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            return minInclusive;

        ulong span = (ulong)((long)maxExclusive - minInclusive);
        return (int)(minInclusive + (long)(Next() % span));
    }

    /// <inheritdoc />
    public float Value()
    {
        // Top 24 bits -> exact floats in [0, 1).
        return (Next() >> 40) * (1f / (1 << 24));
    }

    /// <summary>Next 64-bit output (SplitMix64).</summary>
    private ulong Next()
    {
        unchecked
        {
            _state += 0x9E3779B97F4A7C15UL;
            ulong z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
