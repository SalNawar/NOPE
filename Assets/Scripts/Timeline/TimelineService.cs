using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stable score-key builders so every system reads/writes the same keys.
/// </summary>
public static class TimelineKeys
{
    /// <summary>Score of an attribute inside an authored profile.</summary>
    public static string ProfileAttr(NationEraProfileSO profile, AttributeSO attr) =>
        $"attr:{profile.id}:{attr.id}";

    /// <summary>Score of an attribute at an unauthored nation+era destination.</summary>
    public static string AdHocAttr(NationSO nation, EraSO era, AttributeSO attr) =>
        $"attr:{nation.id}@{era.id}:{attr.id}";

    /// <summary>Global score of an attribute across the whole timeline.</summary>
    public static string GlobalAttr(AttributeSO attr) => $"attrTotal:{attr.id}";

    /// <summary>Global score of a nation.</summary>
    public static string Nation(NationSO nation) => $"nation:{nation.id}";

    /// <summary>Dominance bookkeeping key for a profile attribute.</summary>
    public static string Dominance(NationEraProfileSO profile, AttributeSO attr) =>
        $"{profile.id}:{attr.id}";
}

/// <summary>
/// Core timeline logic:
/// - ApplyVerdictImpacts: every send moves attribute/nation scores (during shift).
/// - NightlyResolve: recompute dominance tiers, fire triggers, expire effects,
///   build the deterministic "tomorrow package" (run at sleep, before day++).
/// All state lives in WorldState; this class is stateless.
/// </summary>
public static class TimelineService
{
    /// <summary>Source label prefix for dominance-tier effects (cleared and rebuilt nightly).</summary>
    private const string TierSourcePrefix = "tier:";

    // =========================================================
    // Shift-time: impacts per decision
    // =========================================================

    /// <summary>
    /// Applies the timeline impacts of one decision. The visitor physically goes
    /// to the CHOSEN era, so impacts land on (case nation, chosen era) — authored
    /// profile if one exists, ad-hoc score keys otherwise.
    /// Also bumps tag counters used by trigger conditions.
    /// </summary>
    public static void ApplyVerdictImpacts(
        CaseInstance inst, EraSO chosenEra, bool correct,
        WorldState world, ContentLibrarySO lib)
    {
        if (inst == null || chosenEra == null || world == null || lib == null)
            return;

        // Gather impacts: archetype defaults + authored case/legendary impacts.
        var impacts = new List<TimelineImpact>();

        if (inst.archetype != null && inst.archetype.defaultImpacts != null)
            impacts.AddRange(inst.archetype.defaultImpacts);

        if (inst.authoredImpacts != null)
            impacts.AddRange(inst.authoredImpacts);

        NationEraProfileSO destProfile =
            inst.nation != null ? lib.GetProfile(inst.nation, chosenEra) : null;

        foreach (TimelineImpact impact in impacts)
        {
            if (impact == null || impact.attribute == null)
                continue;

            float delta = correct ? impact.deltaOnCorrect : impact.deltaOnWrong;

            if (Mathf.Approximately(delta, 0f))
                continue;

            // Profile-level (authored) or ad-hoc destination score.
            if (destProfile != null)
                world.timeline.AddScore(TimelineKeys.ProfileAttr(destProfile, impact.attribute), delta);
            else if (inst.nation != null)
                world.timeline.AddScore(TimelineKeys.AdHocAttr(inst.nation, chosenEra, impact.attribute), delta);

            // Global attribute trend (endings / global dominance read this).
            world.timeline.AddScore(TimelineKeys.GlobalAttr(impact.attribute), delta);

            // Nation total.
            if (impact.alsoAffectsNationScore && inst.nation != null)
                world.timeline.AddScore(TimelineKeys.Nation(inst.nation), delta);
        }

        // Tag counters for trigger conditions ("sent:tag:greek-warrior:greece480").
        if (inst.archetype != null && inst.archetype.tags != null)
        {
            foreach (string tag in inst.archetype.tags)
            {
                if (string.IsNullOrEmpty(tag))
                    continue;

                world.AddCounter($"sent:tag:{tag}", 1);
                world.AddCounter($"sent:tag:{tag}:{chosenEra.id}", 1);
            }
        }
    }

    /// <summary>
    /// Current score of an attribute in a profile: authored baseline + accumulated delta.
    /// </summary>
    public static float GetProfileAttributeScore(WorldState world, NationEraProfileSO profile, AttributeSO attr)
    {
        if (world == null || profile == null || attr == null)
            return 0f;

        AttributeBaseline baseline = profile.GetBaseline(attr);
        float baseScore = baseline != null ? baseline.baseScore : 0f;

        return baseScore + world.timeline.GetScore(TimelineKeys.ProfileAttr(profile, attr));
    }

    // =========================================================
    // Sleep-time: nightly resolve
    // =========================================================

    /// <summary>
    /// Runs the full nightly resolve. Call at sleep, BEFORE world.day increments.
    /// Order: dominance -> tier effects -> triggers -> expiry -> tomorrow package.
    /// </summary>
    public static void NightlyResolve(WorldState world, ContentLibrarySO lib, GameConfigSO config)
    {
        if (world == null || lib == null)
        {
            Debug.LogError("TimelineService.NightlyResolve missing world/library.");
            return;
        }

        int tomorrow = world.day + 1;
        var news = new List<string>();

        RecomputeDominance(world, lib, config, news);
        RebuildTierEffects(world, lib, tomorrow);
        EvaluateTriggers(world, lib, tomorrow, news);
        ExpireEffects(world, tomorrow);
        BuildTomorrowPackage(world, lib, news);
    }

    /// <summary>
    /// Recomputes dominant/supporting attributes per authored profile and
    /// reports tier changes as news lines.
    /// </summary>
    private static void RecomputeDominance(WorldState world, ContentLibrarySO lib, GameConfigSO config, List<string> news)
    {
        int dominantCount = config != null ? config.dominantPerProfile : 1;
        int supportingCount = config != null ? config.supportingPerProfile : 2;

        var newDominant = new List<string>();
        var newSupporting = new List<string>();

        foreach (NationEraProfileSO profile in lib.Profiles)
        {
            if (profile == null || profile.baselines == null || profile.baselines.Count == 0)
                continue;

            // Rank this profile's attributes by current score.
            var ranked = new List<(AttributeSO attr, float score)>();

            foreach (AttributeBaseline b in profile.baselines)
            {
                if (b == null || b.attribute == null)
                    continue;

                ranked.Add((b.attribute, GetProfileAttributeScore(world, profile, b.attribute)));
            }

            ranked.Sort((a, b) => b.score.CompareTo(a.score));

            for (int i = 0; i < ranked.Count; i++)
            {
                string key = TimelineKeys.Dominance(profile, ranked[i].attr);

                if (i < dominantCount)
                {
                    newDominant.Add(key);

                    if (!world.timeline.dominantKeys.Contains(key))
                        news.Add($"{ranked[i].attr.displayName} is now DOMINANT in {profile.displayName}.");
                }
                else if (i < dominantCount + supportingCount)
                {
                    newSupporting.Add(key);

                    if (!world.timeline.supportingKeys.Contains(key) && !world.timeline.dominantKeys.Contains(key))
                        news.Add($"{ranked[i].attr.displayName} is rising in {profile.displayName}.");
                }
            }
        }

        world.timeline.dominantKeys = newDominant;
        world.timeline.supportingKeys = newSupporting;
    }

    /// <summary>
    /// Clears previous tier-sourced effects and re-activates the effects of
    /// every currently dominant/supporting attribute. Tier effects persist
    /// while the tier holds (re-added every night).
    /// </summary>
    private static void RebuildTierEffects(WorldState world, ContentLibrarySO lib, int startDay)
    {
        world.timeline.activeEffects.RemoveAll(e =>
            e != null && e.sourceLabel != null && e.sourceLabel.StartsWith(TierSourcePrefix));

        foreach (NationEraProfileSO profile in lib.Profiles)
        {
            if (profile == null || profile.baselines == null)
                continue;

            foreach (AttributeBaseline b in profile.baselines)
            {
                if (b == null || b.attribute == null)
                    continue;

                string key = TimelineKeys.Dominance(profile, b.attribute);

                if (world.timeline.dominantKeys.Contains(key) && b.dominantEffect != null)
                {
                    ActivateEffect(world, b.dominantEffect,
                        $"{TierSourcePrefix}Dominant {b.attribute.displayName} ({profile.displayName})",
                        startDay, 1, applyInstantOps: false);
                }
                else if (world.timeline.supportingKeys.Contains(key) && b.supportingEffect != null)
                {
                    ActivateEffect(world, b.supportingEffect,
                        $"{TierSourcePrefix}Supporting {b.attribute.displayName} ({profile.displayName})",
                        startDay, 1, applyInstantOps: false);
                }
            }
        }
    }

    /// <summary>
    /// Evaluates all triggers; fires those whose conditions all pass.
    /// </summary>
    private static void EvaluateTriggers(WorldState world, ContentLibrarySO lib, int startDay, List<string> news)
    {
        foreach (TimelineTriggerSO trigger in lib.Triggers)
        {
            if (trigger == null)
                continue;

            if (trigger.oneShot && world.HasFlag(trigger.FiredFlag))
                continue;

            if (!AllConditionsPass(trigger, world))
                continue;

            Debug.Log($"[Timeline] Trigger fired: {trigger.displayName}");

            if (!string.IsNullOrEmpty(trigger.newsLineOnFire))
                news.Add(trigger.newsLineOnFire);

            foreach (TriggerOutcome outcome in trigger.outcomes)
            {
                if (outcome == null || outcome.effect == null)
                    continue;

                int duration = outcome.durationDaysOverride != 0
                    ? outcome.durationDaysOverride
                    : outcome.effect.defaultDurationDays;

                ActivateEffect(world, outcome.effect, $"Trigger: {trigger.displayName}", startDay, duration, applyInstantOps: true);
            }

            if (trigger.oneShot)
                world.SetFlag(trigger.FiredFlag);
        }
    }

    /// <summary>Returns true if every condition on the trigger passes.</summary>
    private static bool AllConditionsPass(TimelineTriggerSO trigger, WorldState world)
    {
        foreach (TriggerCondition c in trigger.conditions)
        {
            if (c == null)
                continue;

            bool pass = c.type switch
            {
                TriggerConditionType.CounterAtLeast => world.GetCounter(c.key) >= c.threshold,
                TriggerConditionType.FlagSet => world.HasFlag(c.key),
                TriggerConditionType.FlagNotSet => !world.HasFlag(c.key),
                TriggerConditionType.AttributeScoreAtLeast =>
                    c.profile != null && c.attribute != null &&
                    GetProfileAttributeScore(world, c.profile, c.attribute) >= c.threshold,
                TriggerConditionType.AttributeScoreAtMost =>
                    c.profile != null && c.attribute != null &&
                    GetProfileAttributeScore(world, c.profile, c.attribute) <= c.threshold,
                TriggerConditionType.AttributeIsDominant =>
                    c.profile != null && c.attribute != null &&
                    world.timeline.dominantKeys.Contains(TimelineKeys.Dominance(c.profile, c.attribute)),
                TriggerConditionType.AttributeIsSupporting =>
                    c.profile != null && c.attribute != null &&
                    world.timeline.supportingKeys.Contains(TimelineKeys.Dominance(c.profile, c.attribute)),
                TriggerConditionType.NationScoreAtLeast =>
                    c.nation != null &&
                    world.timeline.GetScore(TimelineKeys.Nation(c.nation)) >= c.threshold,
                TriggerConditionType.DayAtLeast => world.day >= c.threshold,
                TriggerConditionType.StabilityAtMost => world.timelineStability <= c.threshold,
                _ => false
            };

            if (!pass)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Activates an effect: applies its instant ops (optionally) and registers
    /// it in the stacked active-effect list. Effects from any source coexist.
    /// </summary>
    public static void ActivateEffect(
        WorldState world, EffectSO effect, string sourceLabel,
        int startDay, int durationDays, bool applyInstantOps)
    {
        if (world == null || effect == null)
            return;

        if (applyInstantOps)
        {
            foreach (EffectOp op in effect.ops)
            {
                if (op == null)
                    continue;

                switch (op.type)
                {
                    case EffectOpType.SetFlag: world.SetFlag(op.stringParam); break;
                    case EffectOpType.ClearFlag: world.ClearFlag(op.stringParam); break;
                    case EffectOpType.AddCounter: world.AddCounter(op.stringParam, Mathf.RoundToInt(op.floatParam)); break;
                    case EffectOpType.AddMoney: world.money += Mathf.RoundToInt(op.floatParam); break;
                    case EffectOpType.AddStability:
                        world.timelineStability = Mathf.Clamp(world.timelineStability + op.floatParam, 0f, 100f);
                        break;
                    case EffectOpType.UnlockUpgrade: world.UnlockUpgrade(op.stringParam); break;
                    case EffectOpType.AddAttributeScore:
                        if (op.profile != null && op.attribute != null)
                            world.timeline.AddScore(TimelineKeys.ProfileAttr(op.profile, op.attribute), op.floatParam);
                        break;
                    case EffectOpType.AddNationScore:
                        if (op.nation != null)
                            world.timeline.AddScore(TimelineKeys.Nation(op.nation), op.floatParam);
                        break;
                }
            }
        }

        world.timeline.activeEffects.Add(new ActiveEffectEntry
        {
            effectId = effect.name,
            sourceLabel = sourceLabel,
            startDay = startDay,
            durationDays = durationDays
        });
    }

    /// <summary>Removes effects that are no longer active on the given day.</summary>
    private static void ExpireEffects(WorldState world, int day)
    {
        world.timeline.activeEffects.RemoveAll(e => e == null || !e.IsActiveOnDay(day));
    }

    /// <summary>
    /// Builds the deterministic tomorrow package: dominance/trigger news plus
    /// briefing/news lines contributed by effects active tomorrow.
    /// </summary>
    private static void BuildTomorrowPackage(WorldState world, ContentLibrarySO lib, List<string> news)
    {
        world.tomorrow.briefingLines.Clear();
        world.tomorrow.newsLines.Clear();

        world.tomorrow.newsLines.AddRange(news);

        // Lines from active effects (note: world.day is still "today" here, but
        // expiry has already removed everything not active tomorrow).
        world.tomorrow.briefingLines.AddRange(TimelineEffects.GetLines(world, lib, EffectOpType.BriefingLine));
        world.tomorrow.newsLines.AddRange(TimelineEffects.GetLines(world, lib, EffectOpType.NewsLine));
    }
}
