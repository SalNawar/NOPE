/// <summary>
/// The look-dev's random stream (CITY-R17): mulberry32, exactly as <c>App.mulberry32</c> in
/// <c>city/live/js/core.js</c>, so a C# port seeded the same draws the same numbers in the same order
/// (32-bit wrap-around arithmetic; the JS <c>Math.imul</c> is a 32-bit multiply).
/// </summary>
public sealed class CityR17Rng
{
    private uint _a;

    /// <summary>A stream from a seed (the page passes plain integers).</summary>
    public CityR17Rng(uint seed) => _a = seed;

    /// <summary>The next number in [0, 1).</summary>
    public double Next()
    {
        unchecked
        {
            _a += 0x6D2B79F5u;
            uint t = (_a ^ (_a >> 15)) * (1u | _a);
            t = (t + (t ^ (t >> 7)) * (61u | t)) ^ t;
            return (t ^ (t >> 14)) / 4294967296.0;
        }
    }
}
