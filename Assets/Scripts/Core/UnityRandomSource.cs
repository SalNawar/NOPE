/// <summary>
/// IRandomSource backed by UnityEngine.Random (unseeded global state). For
/// places that are deliberately not replayable, such as the Home slot machine.
/// </summary>
public sealed class UnityRandomSource : IRandomSource
{
    /// <inheritdoc />
    public int Range(int minInclusive, int maxExclusive) =>
        maxExclusive <= minInclusive ? minInclusive : UnityEngine.Random.Range(minInclusive, maxExclusive);

    /// <inheritdoc />
    public float Value()
    {
        // UnityEngine.Random.value includes 1.0; keep the [0, 1) contract.
        float v = UnityEngine.Random.value;
        return v >= 1f ? 0.99999994f : v;
    }
}
