using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// The House panel's effect line (the Home upgrades spec §6): a house
/// upgrade's household ops in words ("Fewer sick nights · A little cheerier
/// at home"), in the order its effect lists them; the sick nights and the
/// mood are the pet's (the Home pet spec PS8). Saleh's Q3 (2026-09-29): "dont
/// reveal numbers to the player": a chance, a percentage or the mood is only
/// ever words; a price in cr stays a number. Pure, so the wording is tested
/// headless.
/// </summary>
public static class HouseEffects
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>One household op in words ("Rent and utilities -4 cr a night", "Far fewer sick nights"): a cost in cr, a chance, share or mood in words only; "" for 0 or an op that is not a household one.</summary>
    public static string Describe(EffectOpType op, float value)
    {
        if (value == 0f)
            return string.Empty;
        switch (op)
        {
            case EffectOpType.HouseholdExpense: return $"Rent and utilities {Signed(value)} cr a night";
            case EffectOpType.SicknessChance: return value < 0f ? Degree(-value, "Slightly fewer", "Fewer", "Far fewer") + " sick nights" : Degree(value, "Slightly more", "More", "Far more") + " sick nights";
            case EffectOpType.CareCost: return $"Medicine {Signed(value)} cr";
            case EffectOpType.Upkeep: return $"Upkeep {value.ToString("0.##", Inv)} cr a night";
            case EffectOpType.MedicalDrain: return $"Sick pet's extra care {Signed(value)} cr a step";
            case EffectOpType.Mood: return value < 0f ? "Gloomier at home" : (value < 1.5f ? "A little cheerier" : value < 2.5f ? "Cheerier" : "Much cheerier") + " at home";
            case EffectOpType.BreakInChance: return value < 0f ? "Fewer break-ins" : "More break-ins";
            case EffectOpType.BreakInShare: return value < 0f ? "A break-in takes less" : "A break-in takes more";
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

    /// <summary>A chance's size in words: under 0.03 <paramref name="small"/>, from 0.05 <paramref name="large"/>, else <paramref name="middle"/>.</summary>
    private static string Degree(float size, string small, string middle, string large) =>
        size < 0.03f - 1e-6f ? small : size >= 0.05f - 1e-6f ? large : middle;

    /// <summary>A value with its sign, rounded to two decimals at most ("+2", "-0.5").</summary>
    private static string Signed(float value)
    {
        float rounded = (float)Math.Round(value, 2);
        return (rounded > 0f ? "+" : string.Empty) + rounded.ToString("0.##", Inv);
    }
}
