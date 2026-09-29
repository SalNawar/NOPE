using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// The House panel's effect line (the Home upgrades spec §6): a house
/// upgrade's household ops in words ("Sick nights -4 % · Mood +1"), in the
/// order its effect lists them. Pure, so the wording is tested headless.
/// </summary>
public static class HouseEffects
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>One household op in words ("Rent and utilities -4 cr a night", "Sick nights -5 %"); "" for 0 or an op that is not a household one.</summary>
    public static string Describe(EffectOpType op, float value)
    {
        if (value == 0f)
            return string.Empty;
        switch (op)
        {
            case EffectOpType.HouseholdExpense: return $"Rent and utilities {Signed(value)} cr a night";
            case EffectOpType.SicknessChance: return $"Sick nights {Signed(value * 100f)} %";
            case EffectOpType.CareCost: return $"Treatment {Signed(value)} cr";
            case EffectOpType.Upkeep: return $"Upkeep {value.ToString("0.##", Inv)} cr a night";
            case EffectOpType.MedicalDrain: return $"Medical drain {Signed(value)} cr a point";
            case EffectOpType.Mood: return $"Mood {Signed(value)}";
            case EffectOpType.BreakInChance: return $"Break-ins {Signed(value * 100f)} %";
            case EffectOpType.BreakInShare: return $"A break-in's take {Signed(value * 100f)} %";
            default: return string.Empty;
        }
    }

    /// <summary>The ops' words joined by " · ", in order, skipping what <see cref="Describe"/> leaves empty.</summary>
    public static string Line(IEnumerable<(EffectOpType op, float value)> ops)
    {
        var parts = new List<string>();
        foreach ((EffectOpType op, float value) in ops ?? Array.Empty<(EffectOpType, float)>())
        {
            string words = Describe(op, value);
            if (words.Length > 0)
                parts.Add(words);
        }
        return string.Join(" · ", parts);
    }

    /// <summary>A value with its sign, rounded to two decimals at most ("+2", "-0.5").</summary>
    private static string Signed(float value)
    {
        float rounded = (float)Math.Round(value, 2);
        return (rounded > 0f ? "+" : string.Empty) + rounded.ToString("0.##", Inv);
    }
}
