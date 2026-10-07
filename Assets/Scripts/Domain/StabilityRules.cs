using System;
using System.Globalization;

/// <summary>How close stability stands to the firing line (the Helix River flickers in the critical band; never serialized).</summary>
public enum StabilityBand
{
    /// <summary>Well above the firing line.</summary>
    Normal,

    /// <summary>Within the warning margin above the line.</summary>
    Warning,

    /// <summary>Within the critical margin, or at or under the line.</summary>
    Critical
}

/// <summary>
/// Timeline stability (redesign phase 23 part 1b; Saleh: "stability should be
/// much more resistant, let's have it x.xx and values change slowly but can
/// compound; we will flesh out this system in a later epic"): a value from 0
/// to 100 kept in hundredths (the player never sees it: the Helix River shows
/// it, HelixRiver; the cheat menu and the logs print "97.43%"), whose every change is a
/// share of where it stands: a loss takes a share of the current value, a
/// gain closes a share of the gap to 100, so changes compound and neither end
/// is ever reached by steps alone. The share is the change's size in points
/// times the rate (GameConfigSO.stabilityChangeRate, the share one point
/// moves). Pure, so every rule is tested headless.
/// </summary>
public static class StabilityRules
{
    /// <summary>The top of the scale (a new run's stability, RunConfigSO.startingStability).</summary>
    public const float Top = 100f;

    /// <summary>
    /// <paramref name="current"/> after a change of <paramref name="points"/>
    /// (negative loses, positive gains) at <paramref name="rate"/> (the share
    /// one point moves: 0.005 = 0.5 % a point): <see cref="ApplyPercent"/> of
    /// rate x points in percent.
    /// </summary>
    public static float Apply(float current, float points, float rate) => ApplyPercent(current, points * rate * 100f);

    /// <summary>
    /// <paramref name="current"/> after a change of <paramref name="percent"/>:
    /// a loss takes that share of the current value, a gain closes that share
    /// of the gap to 100 (a share past the whole takes it all); the result in
    /// hundredths, from 0 to 100 (<see cref="Round"/>).
    /// </summary>
    public static float ApplyPercent(float current, float percent)
    {
        float value = Round(current);
        float share = Math.Min(1f, Math.Abs(percent) / 100f);
        if (percent < 0f)
            value -= value * share;
        else if (percent > 0f)
            value += (Top - value) * share;
        return Round(value);
    }

    /// <summary>The value in hundredths (half away from zero, read at float precision), from 0 to 100.</summary>
    public static float Round(float value)
    {
        if (float.IsNaN(value))
            return 0f;
        float clamped = Math.Max(0f, Math.Min(Top, value));
        return (float)Math.Round((decimal)clamped, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Stability as whole hundredths (97.43 is 9743), as the save keeps it so it reads back exactly; from 0 to 10000.</summary>
    public static int ToHundredths(float value) => (int)Math.Round((decimal)Round(value) * 100m);

    /// <summary>Stability from whole hundredths (9743 is 97.43), from 0 to 100.</summary>
    public static float FromHundredths(int hundredths) => Round((float)(Math.Max(0, Math.Min(10000, hundredths)) / 100m));

    /// <summary>How the cheat menu and the logs print stability (the player never sees a number): two decimals and the percent sign ("97.43%").</summary>
    public static string Format(float value) => Round(value).ToString("0.00", CultureInfo.InvariantCulture) + "%";

    /// <summary>A change as the cheat menu and the logs print it: signed, two decimals ("-2.50", "+0.20"); "0.00" when it rounds to no change.</summary>
    public static string FormatChange(float delta)
    {
        float rounded = (float)Math.Round((decimal)delta, 2, MidpointRounding.AwayFromZero);
        return rounded == 0f ? "0.00" : rounded.ToString("+0.00;-0.00", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The band: Critical within <paramref name="criticalMargin"/>
    /// points above <paramref name="firedAt"/> (the firing line) or under it,
    /// Warning within <paramref name="warningMargin"/>, else Normal.
    /// </summary>
    public static StabilityBand Band(float stability, float firedAt, float warningMargin, float criticalMargin)
    {
        if (stability <= firedAt + criticalMargin)
            return StabilityBand.Critical;
        return stability <= firedAt + warningMargin ? StabilityBand.Warning : StabilityBand.Normal;
    }
}
