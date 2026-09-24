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
