using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Query API over the stacked active effects in WorldState.
/// ANY system (case generation, scoring, shop, visuals, music, UI, newsletter...)
/// reads its inputs from here; effects from different sources stack additively
/// (sums) or multiplicatively (weights) without knowing about each other.
/// </summary>
public static class TimelineEffects
{
    /// <summary>
    /// Enumerates active effects resolved to their EffectSO assets.
    /// Entries whose assets are missing are skipped (with a warning).
    /// </summary>
    public static IEnumerable<(ActiveEffectEntry entry, EffectSO effect)> Active(WorldState world, ContentLibrarySO lib)
    {
        if (world == null || lib == null)
            yield break;

        foreach (ActiveEffectEntry entry in world.timeline.activeEffects)
        {
            if (entry == null || !entry.IsActiveOnDay(world.day))
                continue;

            EffectSO effect = lib.GetEffectByAssetName(entry.effectId);

            if (effect == null)
            {
                Debug.LogWarning($"TimelineEffects: active effect '{entry.effectId}' not found in ContentLibrary (source: {entry.sourceLabel}).");
                continue;
            }

            yield return (entry, effect);
        }
    }

    /// <summary>
    /// Sums floatParam across all active ops of a type (additive bonuses:
    /// LegendaryChanceBonus, ForgeryChanceBonus, PayRateBonus...).
    /// </summary>
    public static float SumFloat(WorldState world, ContentLibrarySO lib, EffectOpType opType)
    {
        float sum = 0f;

        foreach (var (_, effect) in Active(world, lib))
            foreach (EffectOp op in effect.ops)
                if (op != null && op.type == opType)
                    sum += op.floatParam;

        return sum;
    }

    /// <summary>
    /// Multiplies VisitorTagWeight modifiers matching any of the given tags.
    /// Returns 1 when nothing applies. Used by archetype selection.
    /// </summary>
    public static float GetVisitorTagWeightMultiplier(WorldState world, ContentLibrarySO lib, IReadOnlyList<string> tags)
    {
        if (tags == null || tags.Count == 0)
            return 1f;

        float mult = 1f;

        foreach (var (_, effect) in Active(world, lib))
        {
            foreach (EffectOp op in effect.ops)
            {
                if (op == null || op.type != EffectOpType.VisitorTagWeight || string.IsNullOrEmpty(op.stringParam))
                    continue;

                for (int i = 0; i < tags.Count; i++)
                {
                    if (tags[i] == op.stringParam)
                    {
                        mult *= Mathf.Max(0f, op.floatParam);
                        break;
                    }
                }
            }
        }

        return mult;
    }

    /// <summary>
    /// Total discount percent for an upgrade (ops with matching upgrade id,
    /// or empty stringParam = storewide). Clamped to 0..90.
    /// </summary>
    public static float GetShopDiscountPercent(WorldState world, ContentLibrarySO lib, string upgradeId)
    {
        float percent = 0f;

        foreach (var (_, effect) in Active(world, lib))
            foreach (EffectOp op in effect.ops)
                if (op != null && op.type == EffectOpType.ShopDiscountPercent &&
                    (string.IsNullOrEmpty(op.stringParam) || op.stringParam == upgradeId))
                    percent += op.floatParam;

        return Mathf.Clamp(percent, 0f, 90f);
    }

    /// <summary>
    /// Multiplier for a case blueprint's selection weight (by blueprint asset name).
    /// </summary>
    public static float GetBlueprintWeightMultiplier(WorldState world, ContentLibrarySO lib, string blueprintName)
    {
        float mult = 1f;

        foreach (var (_, effect) in Active(world, lib))
            foreach (EffectOp op in effect.ops)
                if (op != null && op.type == EffectOpType.CaseBlueprintWeight && op.stringParam == blueprintName)
                    mult *= Mathf.Max(0f, op.floatParam);

        return mult;
    }

    /// <summary>
    /// All cue ids broadcast on a channel right now (visual sets, music tracks,
    /// UI skins, chatter ids...). A receiver picks the cues it understands;
    /// multiple cues from different sources can coexist on the same day.
    /// </summary>
    public static List<string> GetCues(WorldState world, ContentLibrarySO lib, EffectChannel channel)
    {
        var cues = new List<string>();

        foreach (var (_, effect) in Active(world, lib))
        {
            if (effect.channel != channel)
                continue;

            foreach (EffectOp op in effect.ops)
                if (op != null && op.type == EffectOpType.Cue && !string.IsNullOrEmpty(op.stringParam))
                    cues.Add(op.stringParam);
        }

        return cues;
    }

    /// <summary>
    /// All text lines of a given type (BriefingLine / NewsLine) from active effects.
    /// </summary>
    public static List<string> GetLines(WorldState world, ContentLibrarySO lib, EffectOpType lineType)
    {
        var lines = new List<string>();

        foreach (var (_, effect) in Active(world, lib))
            foreach (EffectOp op in effect.ops)
                if (op != null && op.type == lineType && !string.IsNullOrEmpty(op.stringParam))
                    lines.Add(op.stringParam);

        return lines;
    }
}
