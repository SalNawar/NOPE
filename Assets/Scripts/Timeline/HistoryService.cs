using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// History glue: records liar carries at accept, and at night latches the
/// timeline leader and promotes carries. Every decision is a Domain call
/// (Influence, ScoreRanking, NationLeader, Carries, History); this class reads
/// the content, writes WorldState.history, fills the news and logs.
/// </summary>
public static class HistoryService
{
    /// <summary>Source label prefix of the leader's effect (removed and re-activated every night).</summary>
    private const string LeaderSourcePrefix = "history:leader";

    /// <summary>
    /// At accept: an accepted liar's true home's fact (GameConfigSO.carryCategory)
    /// heads for the claimed place, read from today's facts (Carries.Make).
    /// Honest travellers and violators carry nothing.
    /// </summary>
    public static void RecordCarry(WorldState world, CaseInstance inst, FactTable today, GameConfigSO config)
    {
        if (world == null || inst == null || !inst.IsLiar || config == null)
            return;

        NationSO claimNation = inst.claimedNation != null ? inst.claimedNation : inst.nation;
        NationEraProfileSO home = inst.trueHome;
        if (claimNation == null || inst.claimedEra == null || home.nation == null || home.era == null)
            return;

        CarryRecord record = Carries.Make(home.nation.id, home.era.id, claimNation.id, inst.claimedEra.id, config.carryCategory, today, world.day);
        if (record == null)
            return;

        world.history.pendingCarries.Add(record);
        Debug.Log($"[HistoryService] Carry recorded: '{record.value}' ({record.category}) from {inst.HomeLabel} to {inst.originLabel}.");
    }

    /// <summary>
    /// At night (after the tiers, before the triggers): ranks the nations by
    /// influence (saved as history.ranking), decides the leader
    /// (NationLeader.Decide, or the debug override), announces a change and
    /// re-activates the leader's effect for tomorrow. Returns the number of
    /// history news lines added (0 or 1). With no config it warns and keeps
    /// the history as it is.
    /// </summary>
    public static int LatchLeader(WorldState world, ContentLibrarySO lib, GameConfigSO config, int tomorrow, List<string> news)
    {
        if (config == null)
        {
            Debug.LogWarning("[HistoryService] No GameConfigSO: the timeline leader is not decided tonight (history needs its knobs).");
            return 0;
        }

        HistoryState h = world.history;
        EraSO future = lib.FutureEra;
        List<RankedScore> influence = Influence.ByNation(
            lib.Nations.Where(n => n != null && !string.IsNullOrWhiteSpace(n.id)).Select(n => n.id).ToList(),
            world.timeline.scores.Where(s => s != null).Select(s => new KeyValuePair<string, float>(s.key, s.value)),
            PlaceOf(lib),
            future != null ? future.id : null);
        h.ranking = ScoreRanking.Rank(influence);

        string next;
        if (!string.IsNullOrEmpty(DevToolsState.ForcedLeaderId))
        {
            next = DevToolsState.ForcedLeaderId;
            Debug.Log($"[HistoryService] Leader kept by the debug override: '{next}' (influence decision skipped).");
        }
        else
        {
            next = NationLeader.Decide(h.leaderId, h.ranking, config.leaderFloor, config.leaderKeepFloor, config.leaderMargin);
        }

        Debug.Log($"[HistoryService] Influence ranking: {string.Join(", ", h.ranking.Select(r => $"{r.id} {r.score:0.#}"))}; leader '{h.leaderId}' -> '{next}'.");

        int added = 0;
        if (next != (h.leaderId ?? string.Empty))
        {
            string previous = h.leaderId;
            h.leaderId = next;
            h.leaderSinceDay = string.IsNullOrEmpty(next) ? 0 : tomorrow;

            HistoryLines lines = lib.HistoryLines;
            string line = string.IsNullOrEmpty(next)
                ? Interview.Fill(lines.leaderLost?.text, History.NationToken, NationName(lib, previous))
                : Interview.Fill(Interview.Fill(lines.leaderGained?.text, History.NationToken, NationName(lib, next)),
                                 Interview.PlaceToken, FuturePlaceLabel(lib, next));
            if (!string.IsNullOrWhiteSpace(line))
            {
                news.Add(line);
                added = 1;
            }
            else
            {
                Debug.LogWarning("[HistoryService] The leader changed but the content library has no history line for it. Run Tools > TimeDesk > Generate World.");
            }
        }

        RebuildLeaderEffect(world, lib, tomorrow);
        return added;
    }

    /// <summary>
    /// At night (after the triggers): warns about a history rule edit latched
    /// tonight that shares a value with another place, promotes the due carries
    /// (Carries.Promote over every place's facts, history applied) and
    /// announces them while the history news cap allows (History.NewsSlots;
    /// the rest are logged). With no config nothing is promoted.
    /// </summary>
    public static void PromoteCarries(WorldState world, ContentLibrarySO lib, GameConfigSO config, int tomorrow, List<string> news, int historyLinesSoFar)
    {
        if (config == null)
            return;

        FactTable worldFacts = lib.BuildWorldFacts(world.history);
        foreach (FactEdit edit in world.history.factEdits.Where(e => e != null && e.sinceDay == tomorrow && e.cause == EditCause.Rule))
            if (worldFacts.TryFindOtherPlaceWith(edit.category, edit.nationId, edit.eraId, edit.value, out FactRow other))
                Debug.LogWarning($"[HistoryService] History rule '{edit.source}' gives {worldFacts.OriginLabel(edit.nationId, edit.eraId)} the {edit.category} value '{edit.value}', which {other.OriginLabel} already has, so neither can leak a {edit.category} tell while both are in the world. Give the rule a distinct value (Tools > TimeDesk > Validate Content Library).");

        List<FactEdit> edits = Carries.Promote(world.history, config.carryThreshold, tomorrow, worldFacts);
        int slots = History.NewsSlots(historyLinesSoFar, config.maxHistoryNewsPerNight, edits.Count);
        LineText template = lib.HistoryLines.carry;
        for (int i = 0; i < edits.Count; i++)
        {
            string place = worldFacts.OriginLabel(edits[i].nationId, edits[i].eraId);
            Debug.Log($"[HistoryService] Carry latched: {place} {edits[i].category} = '{edits[i].value}' from day {tomorrow} (brought from {edits[i].source}).");
            if (i < slots)
                news.Add(Interview.Fill(Interview.Fill(template?.text, Interview.ValueToken, edits[i].value), Interview.PlaceToken, place));
        }

        if (edits.Count > slots)
            Debug.Log($"[HistoryService] {edits.Count - slots} carry line(s) over tonight's news cap ({config.maxHistoryNewsPerNight}): {string.Join("; ", edits.Skip(slots).Select(e => $"{e.nationId}_{e.eraId} = '{e.value}'"))}.");
    }

    /// <summary>
    /// Dev cheat: sets the leader for the rest of the session. The nightly
    /// leader step keeps it until "No leader" (a blank id) or a new run or
    /// Continue (DevToolsState.ResetAll); the leader's cue is active at once and
    /// the Future place follows at the next office day. A blank id clears the
    /// override and the leader. No news.
    /// </summary>
    public static void ForceLeader(WorldState world, ContentLibrarySO lib, string nationId)
    {
        if (world == null || lib == null)
            return;

        if (string.IsNullOrWhiteSpace(nationId))
        {
            DevToolsState.ForcedLeaderId = null;
            world.history.leaderId = string.Empty;
            world.history.leaderSinceDay = 0;
        }
        else
        {
            DevToolsState.ForcedLeaderId = nationId;
            world.history.leaderId = nationId;
            world.history.leaderSinceDay = world.day + 1;
        }

        Debug.Log($"[HistoryService] Leader forced to '{world.history.leaderId}' (kept every night this session).");
        RebuildLeaderEffect(world, lib, world.day);
    }

    /// <summary>
    /// The idempotent emission step: removes the leader effect entries, then
    /// activates the leader's effect (NationSO.leaderEffect, permanent, from
    /// <paramref name="startDay"/>), so the UI cue culture:{id} always matches
    /// history.leaderId. A leader without an effect logs a warning. Also called
    /// by RunManager.ContinueRun for today, so a save without the day's culture
    /// cue themes correctly (piece 6 Z5).
    /// </summary>
    public static void RebuildLeaderEffect(WorldState world, ContentLibrarySO lib, int startDay)
    {
        TimelineService.RemoveEffectsFrom(world, LeaderSourcePrefix);

        string leader = world.history.leaderId;
        if (string.IsNullOrEmpty(leader))
            return;

        NationSO nation = lib.GetNationById(leader);
        if (nation == null || nation.leaderEffect == null)
        {
            Debug.LogWarning($"[HistoryService] Leader '{leader}' has no leader effect, so no culture cue is broadcast. Run Tools > TimeDesk > Generate World.");
            return;
        }

        TimelineService.ActivateEffect(world, nation.leaderEffect, $"{LeaderSourcePrefix} {nation.displayName}", startDay, -1, applyInstantOps: false);
    }

    /// <summary>The (nation, era) of a place id, for Influence.ByNation (null for an unknown place).</summary>
    private static System.Func<string, PlaceRef?> PlaceOf(ContentLibrarySO lib) => profileId =>
    {
        NationEraProfileSO p = lib.GetProfileById(profileId);
        return p != null && p.nation != null && p.era != null ? new PlaceRef(p.nation.id, p.era.id) : (PlaceRef?)null;
    };

    /// <summary>A nation's display name (its id when unknown).</summary>
    private static string NationName(ContentLibrarySO lib, string nationId)
    {
        NationSO n = lib.GetNationById(nationId);
        return n != null && !string.IsNullOrWhiteSpace(n.displayName) ? n.displayName : nationId;
    }

    /// <summary>The label of a nation's Future place; the nation's name when the content has no Future place for it.</summary>
    private static string FuturePlaceLabel(ContentLibrarySO lib, string nationId)
    {
        NationEraProfileSO place = lib.GetProfile(lib.GetNationById(nationId), lib.FutureEra);
        return place != null ? place.OriginLabel : NationName(lib, nationId);
    }
}
