using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The world's outcomes in play (the endings spec E2-E3; the rules are the
/// Domain's WorldPulls): each decision at the desk adds its pulls (an
/// accepted traveller through their role and the destination's leaning, or a
/// famous traveller's authored pulls; a denial toward each "as you found it"
/// outcome), each night latches every factor's answer and prints a changed
/// one in the morning paper (the history news cap), and an older save
/// continued mid-run is seeded once from its per-place attribute deltas. Story
/// rules and dialog choices pull through their effects' PullOutcome ops
/// (TimelineService.ActivateEffect). The player never sees a pull or a weight:
/// only the answers, in words. Stateless; all state lives in WorldState.
/// </summary>
public static class WorldOutcomeService
{
    /// <summary>
    /// One decision's pulls onto <paramref name="world"/>.pulls (DayCycle.Decide,
    /// the one path of the game and the balance simulation): accepted, the
    /// traveller's pulls toward their destination (the claimed place, where
    /// they go; a famous traveller's authored pulls instead), scaled by their
    /// kind (GameConfigSO); denied, the denial pull toward each factor's "as
    /// you found it" outcome.
    /// </summary>
    public static void RecordDecision(WorldState world, CaseInstance inst, bool accepted, ContentLibrarySO lib, GameConfigSO config)
    {
        if (world == null || inst == null || lib == null)
            return;

        WorldContent content = lib.World;
        List<OutcomePull> pulls;
        if (accepted)
        {
            NationEraProfileSO destination = inst.claimedNation != null && inst.claimedEra != null ? lib.GetProfile(inst.claimedNation, inst.claimedEra) : null;
            List<OutcomePull> famous = inst.isLegendary && inst.legendarySource != null ? inst.legendarySource.pulls : null;
            float scale = config != null ? config.WorldKindScale(inst.kind) : 1f;
            pulls = WorldPulls.ForAccept(famous, content.roles, inst.archetype != null ? inst.archetype.id : null, destination != null ? destination.leanings : null, scale);
        }
        else
        {
            pulls = WorldPulls.ForDenial(content.PullFactors(), config != null ? config.worldDenialPull : 0f);
        }

        int added = WorldPulls.AddAll(world.pulls, pulls);
        if (added > 0)
            Debug.Log($"[WorldOutcomeService] {(accepted ? "Accepted" : "Denied")} '{inst.visitorDisplayName}': {string.Join(", ", pulls.Select(p => $"{p.factor}/{p.outcome} +{p.amount:0.##}"))}.");
    }

    /// <summary>
    /// At night (after the triggers, whose PullOutcome ops have landed; before
    /// the carries): latches each factor answered by pulls (WorldPulls.Latch,
    /// GameConfigSO's world knobs), dated <paramref name="tomorrow"/>, and
    /// prints a changed answer's headline (WorldContent.Headline) while the
    /// history news cap allows after <paramref name="historyLinesSoFar"/>
    /// (History.NewsSlots; the rest are logged). Returns the lines added.
    /// </summary>
    public static int Latch(WorldState world, ContentLibrarySO lib, GameConfigSO config, int tomorrow, List<string> news, int historyLinesSoFar)
    {
        if (world == null || lib == null)
            return 0;

        WorldContent content = lib.World;
        List<FactorLead> changed = WorldPulls.Latch(content.PullFactors(), world.pulls, world.leads,
            config != null ? config.worldStatusQuoWeight : GameConfigSO.DefaultWorldStatusQuoWeight,
            config != null ? config.worldLeadMargin : GameConfigSO.DefaultWorldLeadMargin, tomorrow);

        int cap = config != null ? config.maxHistoryNewsPerNight : 3;
        int slots = History.NewsSlots(historyLinesSoFar, cap, changed.Count);
        int added = 0;
        for (int i = 0; i < changed.Count; i++)
        {
            string line = content.Headline(changed[i]);
            Debug.Log($"[WorldOutcomeService] '{changed[i].factor}' now answers '{content.Words(changed[i])}' from day {tomorrow}.");
            if (i < slots && !string.IsNullOrWhiteSpace(line) && news != null)
            {
                news.Add(line);
                added++;
            }
        }

        if (changed.Count > slots)
            Debug.Log($"[WorldOutcomeService] {changed.Count - slots} world line(s) over tonight's history news cap ({cap}).");
        return added;
    }

    /// <summary>
    /// An older save continued mid-run (the endings spec §9): when it has no
    /// pull and no latched lead and its day is past 1, seeds the pulls once from the saved
    /// per-place attribute deltas (WorldPulls.FromScores; which attribute
    /// seeds which factor is WorldPulls.SeedFactors over the archetypes'
    /// default impacts and the roles). True when it seeded.
    /// </summary>
    public static bool SeedOlderSave(WorldState world, ContentLibrarySO lib)
    {
        if (world == null || lib == null)
            return false;

        var moves = new List<(string archetype, string attribute)>();
        foreach (ArchetypeSO a in lib.Archetypes.Where(a => a != null))
            foreach (TimelineImpact impact in a.defaultImpacts ?? new List<TimelineImpact>())
                if (impact != null && impact.attribute != null)
                    moves.Add((a.id, impact.attribute.id));
        Dictionary<string, string> seeds = WorldPulls.SeedFactors(moves, lib.World.roles);

        var deltas = new List<(string place, string attribute, float delta)>();
        foreach (ScoreEntry s in world.timeline.scores.Where(s => s != null))
            if (ScoreKey.TryParse(s.key, out ParsedScoreKey key) && key.kind == ScoreKeyKind.ProfileAttr)
                deltas.Add((key.profileId, key.attributeId, s.value));

        var places = lib.Profiles.Where(p => p != null && !string.IsNullOrEmpty(p.id)).GroupBy(p => p.id).ToDictionary(g => g.Key, g => g.First());
        bool seeded = WorldPulls.FromScores(world.pulls, world.leads, world.day, deltas, seeds, id => id != null && places.TryGetValue(id, out NationEraProfileSO p) ? p.leanings : null);
        if (seeded)
            Debug.Log($"[WorldOutcomeService] An older save (day {world.day}) had no world pulls: seeded once from its per-place attribute deltas ({world.pulls.Count} outcome(s) pulled).");
        return seeded;
    }
}
