using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Score-key builders over the content types, so every system reads and
/// writes the same keys; the grammar itself lives in Domain ScoreKey.
/// </summary>
public static class TimelineKeys
{
    /// <summary>Score of an attribute inside an authored profile.</summary>
    public static string ProfileAttr(NationEraProfileSO profile, AttributeSO attr) => ScoreKey.ProfileAttr(profile.id, attr.id);

    /// <summary>Score of an attribute at an unauthored nation+era destination.</summary>
    public static string AdHocAttr(NationSO nation, EraSO era, AttributeSO attr) => ScoreKey.AdHocAttr(nation.id, era.id, attr.id);

    /// <summary>Global score of an attribute across the whole timeline.</summary>
    public static string GlobalAttr(AttributeSO attr) => ScoreKey.GlobalAttr(attr.id);

    /// <summary>Global score of a nation.</summary>
    public static string Nation(NationSO nation) => ScoreKey.Nation(nation.id);

    /// <summary>Dominance bookkeeping key for a profile attribute.</summary>
    public static string Dominance(NationEraProfileSO profile, AttributeSO attr) => ScoreKey.Dominance(profile.id, attr.id);
}

/// <summary>
/// Core timeline logic:
/// - ApplyVerdictImpacts: every send moves attribute/nation scores (during shift).
/// - NightlyResolve: recompute dominance tiers, latch the timeline leader, fire
///   triggers (history rules latch fact edits), promote carries, expire
///   effects, build the deterministic "tomorrow package" (run at sleep, before day++).
/// - BuildInterviewDay: the day's interview, its questions and dialogs gated
///   on a snapshot of the day-start world (run at day start).
/// - BuildTranslationDay: which tongues are foreign and translated today, read
///   from the same day-start snapshot (run at day start).
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
    /// to the CHOSEN era, so impacts land on (claimed nation, chosen era): where
    /// the traveller is sent, liar or not — authored profile if one exists,
    /// ad-hoc score keys otherwise. The attribute keys it writes are what
    /// history counts as influence (HistoryService).
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
    /// Order: dominance (news only for tomorrow's places) -> tier effects ->
    /// the timeline leader -> triggers (history rules latch here) -> carries ->
    /// expiry -> tomorrow package.
    /// </summary>
    public static void NightlyResolve(WorldState world, ContentLibrarySO lib, GameConfigSO config)
    {
        if (world == null || lib == null)
        {
            Debug.LogError("TimelineService.NightlyResolve missing world/library.");
            return;
        }

        Debug.Log($"[TimelineService] >>> Entering NightlyResolve (day {world.day} -> {world.day + 1}).");

        int tomorrow = world.day + 1;
        var news = new List<string>();

        RecomputeDominance(world, lib, config, news, TomorrowPlaces(world, lib));
        RebuildTierEffects(world, lib, tomorrow);
        int historyLines = HistoryService.LatchLeader(world, lib, config, tomorrow, news);
        EvaluateTriggers(world, lib, tomorrow, news);
        HistoryService.PromoteCarries(world, lib, config, tomorrow, news, historyLines);
        ExpireEffects(world, tomorrow);
        BuildTomorrowPackage(world, lib, news);

        Debug.Log($"[TimelineService] <<< Exiting NightlyResolve (activeEffects={world.timeline.activeEffects.Count}, dominant={world.timeline.dominantKeys.Count}, supporting={world.timeline.supportingKeys.Count}, briefingLines={world.tomorrow.briefingLines.Count}, newsLines={world.tomorrow.newsLines.Count}).");
    }

    /// <summary>
    /// Ranks every place's attributes from the baselines alone, without news.
    /// Called once when a run starts, so the first night reports exactly the
    /// tier changes the player's day-1 sends caused.
    /// </summary>
    public static void SeedDominance(WorldState world, ContentLibrarySO lib, GameConfigSO config)
    {
        if (world == null || lib == null)
            return;

        RecomputeDominance(world, lib, config, null, null);
    }

    /// <summary>
    /// Tomorrow's places as the dominance news filter reads them (the plan
    /// tomorrow uses, without a Future place: Future places are never ranked);
    /// empty when there is no plan.
    /// </summary>
    private static HashSet<NationEraProfileSO> TomorrowPlaces(WorldState world, ContentLibrarySO lib) =>
        new HashSet<NationEraProfileSO>(lib.TodaysProfiles(lib.GetDayPlan(world.day + 1), null));

    /// <summary>
    /// Recomputes dominant/supporting attributes per authored profile (places
    /// with baselines; Future places have none) and reports a newly DOMINANT
    /// attribute as news, only for <paramref name="tomorrowPlaces"/> (none when
    /// <paramref name="news"/> or the set is null, or when no earlier ranking
    /// exists to compare with). Tiers rank baseline + delta (they describe a
    /// place); influence ranks deltas only (it describes the player's
    /// deviation). Both are night snapshots through ScoreRanking.
    /// </summary>
    private static void RecomputeDominance(WorldState world, ContentLibrarySO lib, GameConfigSO config, List<string> news,
                                           HashSet<NationEraProfileSO> tomorrowPlaces)
    {
        Debug.Log("[TimelineService] >>> Entering RecomputeDominance.");

        int dominantCount = config != null ? config.dominantPerProfile : 1;
        int supportingCount = config != null ? config.supportingPerProfile : 2;

        var newDominant = new List<string>();
        var newSupporting = new List<string>();

        // Only changes against an earlier ranking are news: SeedDominance ranks
        // the baselines at run start, and a run without that seed stays silent
        // on its first night instead of announcing every place's starting tiers.
        bool announce = news != null && tomorrowPlaces != null &&
                        (world.timeline.dominantKeys.Count > 0 || world.timeline.supportingKeys.Count > 0);

        foreach (NationEraProfileSO profile in lib.Profiles)
        {
            if (profile == null || profile.baselines == null || profile.baselines.Count == 0)
                continue;

            // This profile's attributes (baseline + delta) in baseline order, tiered by the Domain ranking.
            List<AttributeSO> attrs = profile.baselines.Where(b => b != null && b.attribute != null).Select(b => b.attribute).ToList();
            var scores = attrs.Select(a => new RankedScore(a.id, GetProfileAttributeScore(world, profile, a))).ToList();
            DominanceTier[] tiers = DominanceTiers.Classify(scores, dominantCount, supportingCount);

            Debug.Log($"[TimelineService] RecomputeDominance: profile '{profile.displayName}' scores — {string.Join(", ", attrs.Select((a, i) => $"{a.displayName}={scores[i].score:0.#} ({tiers[i]})"))}.");

            for (int i = 0; i < attrs.Count; i++)
            {
                string key = TimelineKeys.Dominance(profile, attrs[i]);

                if (tiers[i] == DominanceTier.Dominant)
                {
                    newDominant.Add(key);

                    if (announce && tomorrowPlaces.Contains(profile) && !world.timeline.dominantKeys.Contains(key))
                        news.Add($"{attrs[i].displayName} is now DOMINANT in {profile.displayName}.");
                }
                else if (tiers[i] == DominanceTier.Supporting)
                {
                    newSupporting.Add(key);
                }
            }
        }

        world.timeline.dominantKeys = newDominant;
        world.timeline.supportingKeys = newSupporting;

        Debug.Log($"[TimelineService] <<< Exiting RecomputeDominance (dominant={newDominant.Count}, supporting={newSupporting.Count}, announced={announce}).");
    }

    /// <summary>
    /// Clears previous tier-sourced effects and re-activates the effects of
    /// every currently dominant/supporting attribute. Tier effects persist
    /// while the tier holds (re-added every night).
    /// </summary>
    private static void RebuildTierEffects(WorldState world, ContentLibrarySO lib, int startDay)
    {
        Debug.Log("[TimelineService] >>> Entering RebuildTierEffects.");

        int removed = RemoveEffectsFrom(world, TierSourcePrefix);

        int activated = 0;

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
                    activated++;
                }
                else if (world.timeline.supportingKeys.Contains(key) && b.supportingEffect != null)
                {
                    ActivateEffect(world, b.supportingEffect,
                        $"{TierSourcePrefix}Supporting {b.attribute.displayName} ({profile.displayName})",
                        startDay, 1, applyInstantOps: false);
                    activated++;
                }
            }
        }

        Debug.Log($"[TimelineService] <<< Exiting RebuildTierEffects (removed {removed} old tier effect(s), activated {activated} new).");
    }

    /// <summary>
    /// One idempotent-rebuild step: an effect family (dominance tiers, the
    /// timeline leader) removes its own active entries, whose source label
    /// starts with <paramref name="sourcePrefix"/>, before re-activating.
    /// Returns how many were removed.
    /// </summary>
    internal static int RemoveEffectsFrom(WorldState world, string sourcePrefix) =>
        world.timeline.activeEffects.RemoveAll(e => e != null && e.sourceLabel != null && e.sourceLabel.StartsWith(sourcePrefix, System.StringComparison.Ordinal));

    /// <summary>
    /// Evaluates all triggers; fires those whose conditions all pass.
    /// </summary>
    private static void EvaluateTriggers(WorldState world, ContentLibrarySO lib, int startDay, List<string> news)
    {
        int total = lib.Triggers != null ? lib.Triggers.Count : 0;
        int fired = 0;

        Debug.Log($"[TimelineService] >>> Entering EvaluateTriggers ({total} trigger(s) to check).");

        foreach (TimelineTriggerSO trigger in lib.Triggers)
        {
            if (trigger == null)
                continue;

            if (trigger.oneShot && world.HasFlag(trigger.FiredFlag))
            {
                Debug.Log($"[TimelineService] Trigger '{trigger.displayName}' skipped (already fired, one-shot).");
                continue;
            }

            if (!AllConditionsPass(trigger, world))
                continue;

            Debug.Log($"[Timeline] Trigger fired: {trigger.displayName}");
            fired++;

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

        Debug.Log($"[TimelineService] <<< Exiting EvaluateTriggers ({fired}/{total} fired).");
    }

    /// <summary>
    /// The day's interview from the library's questions and dialogs and the
    /// day-start world (glue only; InterviewDay decides what is askable and
    /// offered): each item's conditions projected with ToGates, and one
    /// snapshot of the world holding the scores every question's and dialog's
    /// conditions read. Null library entries are skipped.
    /// </summary>
    public static InterviewDay BuildInterviewDay(ContentLibrarySO lib, WorldState world, ShiftLedger ledger)
    {
        var conditions = new List<TriggerCondition>();
        var questions = new List<Gated<InterviewQuestion>>();
        foreach (QuestionSO q in lib.Questions)
        {
            if (q == null)
                continue;
            if (q.conditions != null)
                conditions.AddRange(q.conditions);
            questions.Add(new Gated<InterviewQuestion>(q.question, ToGates(q.conditions)));
        }

        var dialogs = new List<Gated<AuthoredDialog>>();
        foreach (DialogSO d in lib.Dialogs)
        {
            if (d == null)
                continue;
            if (d.conditions != null)
                conditions.AddRange(d.conditions);
            dialogs.Add(new Gated<AuthoredDialog>(d.dialog, ToGates(d.conditions)));
        }

        var premadeDialogs = new List<string>();
        foreach (LegendarySO premade in lib.Legendaries)
            if (premade != null && !string.IsNullOrWhiteSpace(premade.dialogId))
                premadeDialogs.Add(premade.dialogId);

        return new InterviewDay(lib.Interview, questions, dialogs, Snapshot(world, conditions), ledger, premadeDialogs);
    }

    /// <summary>
    /// Today's translation (piece 9): which tongues are foreign and which are
    /// translated, from the library's rules (none without translation data:
    /// nothing is foreign) and the day-start snapshot BuildInterviewDay also reads.
    /// </summary>
    public static TranslationDay BuildTranslationDay(ContentLibrarySO lib, WorldState world) =>
        new TranslationDay(lib != null && lib.Translation.HasData ? lib.Translation.rules : null, Snapshot(world, null));

    /// <summary>
    /// Returns true if every condition on the trigger passes (Gates.AllPass):
    /// one snapshot per trigger, so a trigger sees the flags that earlier
    /// triggers set tonight.
    /// </summary>
    private static bool AllConditionsPass(TimelineTriggerSO trigger, WorldState world) =>
        Gates.AllPass(ToGates(trigger.conditions), Snapshot(world, trigger.conditions));

    /// <summary>
    /// A condition as the Domain gates read it: its type, threshold and plain
    /// key (the counter, flag or upgrade key; the profile-attribute score key,
    /// the dominance key, the nation score key, the nation id or the global
    /// attribute key for the reference types; null when a needed reference is
    /// missing, which never passes).
    /// </summary>
    private static GateCondition ToGate(TriggerCondition c)
    {
        string key;
        switch (c.type)
        {
            case TriggerConditionType.CounterAtLeast:
            case TriggerConditionType.FlagSet:
            case TriggerConditionType.FlagNotSet:
            case TriggerConditionType.UpgradeOwned:
                key = c.key;
                break;
            case TriggerConditionType.AttributeScoreAtLeast:
            case TriggerConditionType.AttributeScoreAtMost:
                key = c.profile != null && c.attribute != null ? TimelineKeys.ProfileAttr(c.profile, c.attribute) : null;
                break;
            case TriggerConditionType.AttributeIsDominant:
            case TriggerConditionType.AttributeIsSupporting:
                key = c.profile != null && c.attribute != null ? TimelineKeys.Dominance(c.profile, c.attribute) : null;
                break;
            case TriggerConditionType.NationScoreAtLeast:
                key = c.nation != null ? TimelineKeys.Nation(c.nation) : null;
                break;
            case TriggerConditionType.NationIsLeader:
                key = c.nation != null ? c.nation.id : null;
                break;
            case TriggerConditionType.GlobalAttrAtLeast:
            case TriggerConditionType.GlobalAttrAtMost:
                key = c.attribute != null ? TimelineKeys.GlobalAttr(c.attribute) : null;
                break;
            default:
                key = null;
                break;
        }

        return new GateCondition(c.type, key, c.threshold);
    }

    /// <summary>The non-null conditions projected with <see cref="ToGate"/>, in order (empty for null).</summary>
    private static List<GateCondition> ToGates(IEnumerable<TriggerCondition> conditions)
    {
        var gates = new List<GateCondition>();
        if (conditions != null)
            foreach (TriggerCondition c in conditions)
                if (c != null)
                    gates.Add(ToGate(c));
        return gates;
    }

    /// <summary>
    /// Copies what gates read from the world: day, stability, flags, owned
    /// upgrades, counters, dominance tiers and the timeline leader, plus the
    /// scores the given conditions read, each under its gate key with the value
    /// it has now (GetProfileAttributeScore for profile scores, so the baseline
    /// formula keeps one home; the timeline score for nations and global totals).
    /// </summary>
    private static GateSnapshot Snapshot(WorldState world, IEnumerable<TriggerCondition> conditions)
    {
        var scores = new List<KeyValuePair<string, float>>();
        if (conditions != null)
        {
            foreach (TriggerCondition c in conditions)
            {
                if (c == null)
                    continue;

                if ((c.type == TriggerConditionType.AttributeScoreAtLeast || c.type == TriggerConditionType.AttributeScoreAtMost) &&
                    c.profile != null && c.attribute != null)
                    scores.Add(new KeyValuePair<string, float>(TimelineKeys.ProfileAttr(c.profile, c.attribute), GetProfileAttributeScore(world, c.profile, c.attribute)));
                else if (c.type == TriggerConditionType.NationScoreAtLeast && c.nation != null)
                    scores.Add(new KeyValuePair<string, float>(TimelineKeys.Nation(c.nation), world.timeline.GetScore(TimelineKeys.Nation(c.nation))));
                else if ((c.type == TriggerConditionType.GlobalAttrAtLeast || c.type == TriggerConditionType.GlobalAttrAtMost) && c.attribute != null)
                    scores.Add(new KeyValuePair<string, float>(TimelineKeys.GlobalAttr(c.attribute), world.timeline.GetScore(TimelineKeys.GlobalAttr(c.attribute))));
            }
        }

        return new GateSnapshot(
            world.day,
            world.timelineStability,
            world.flags,
            world.unlockedUpgradeIds,
            world.counters.Where(e => e != null).Select(e => new KeyValuePair<string, int>(e.key, e.value)),
            scores,
            world.timeline.dominantKeys,
            world.timeline.supportingKeys,
            world.history.leaderId);
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
                    case EffectOpType.SetFact:
                        // A history rule's fact edit, latched for good from startDay (tomorrow at night).
                        if (op.profile != null && op.profile.nation != null && op.profile.era != null)
                            History.Latch(world.history, new FactEdit(op.profile.nation.id, op.profile.era.id, op.category, op.stringParam,
                                                                      startDay, EditCause.Rule, sourceLabel));
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

        Debug.Log($"[TimelineService] ActivateEffect: effectId='{effect.name}', sourceLabel='{sourceLabel}', startDay={startDay}, durationDays={durationDays}, applyInstantOps={applyInstantOps}.");
    }

    /// <summary>Removes effects that are no longer active on the given day.</summary>
    private static void ExpireEffects(WorldState world, int day)
    {
        int before = world.timeline.activeEffects.Count;

        world.timeline.activeEffects.RemoveAll(e => e == null || !e.IsActiveOnDay(day));

        int removed = before - world.timeline.activeEffects.Count;

        Debug.Log($"[TimelineService] ExpireEffects (day {day}): removed {removed} effect(s), {world.timeline.activeEffects.Count} remain active.");
    }

    /// <summary>
    /// Builds the deterministic tomorrow package: dominance, history and
    /// trigger news plus briefing/news lines contributed by effects active tomorrow.
    /// </summary>
    private static void BuildTomorrowPackage(WorldState world, ContentLibrarySO lib, List<string> news)
    {
        Debug.Log("[TimelineService] >>> Entering BuildTomorrowPackage.");

        world.tomorrow.briefingLines.Clear();
        world.tomorrow.newsLines.Clear();

        world.tomorrow.newsLines.AddRange(news);

        // Lines from active effects (note: world.day is still "today" here, but
        // expiry has already removed everything not active tomorrow).
        world.tomorrow.briefingLines.AddRange(TimelineEffects.GetLines(world, lib, EffectOpType.BriefingLine));
        world.tomorrow.newsLines.AddRange(TimelineEffects.GetLines(world, lib, EffectOpType.NewsLine));

        Debug.Log($"[TimelineService] <<< Exiting BuildTomorrowPackage (briefingLines={world.tomorrow.briefingLines.Count}, newsLines={world.tomorrow.newsLines.Count} [{news.Count} from dominance/triggers]).");
    }
}
