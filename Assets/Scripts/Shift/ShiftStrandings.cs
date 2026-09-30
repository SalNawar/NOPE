using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The shift's end (the traveller-types spec's S1; the endings and strandings
/// spec §6-§7, Saleh's answers of 2026-09-30): rolls the accepted travellers
/// for strandings (Strandings.Roll on the day's stream, the real class from
/// each traveller's Citizen Account; unchanged), then gives each stranded one
/// a fate on the day's fate stream (StrandingFates.Pick: the column of their
/// waiver, their personality's tilt) and applies it: a carry of the present's
/// Technology (falling back to the news when none can be made), a tremor's
/// stability loss, or nothing but the paper's line (or, for the forgotten, not
/// even that). When no valid signed waiver was on file the agency's failure
/// report charges the stranding fine (GameConfigSO.strandingFine; Q10 = D).
/// Each stranding is kept for the next morning's paper and in the run's log
/// (the Mail report), counted on the ledger and in the run's counters. Every
/// decision is a Domain call; this class reads the cases, the content and the
/// config and writes the world, the ledger and the log.
/// </summary>
public static class ShiftStrandings
{
    /// <summary>The counter of strandings across the run (WorldState.counters; history rules and effects may read it).</summary>
    public const string StrandedCounter = "stranded";

    /// <summary>The counter of strandings that met <paramref name="fate"/> ("stranded:police").</summary>
    public static string FateCounter(StrandingFate fate) => StrandedCounter + ":" + fate.ToString().ToLowerInvariant();

    /// <summary>The counter of the stranding fines charged across the run, in cr.</summary>
    public const string FinesCounter = "strandingFines";

    /// <summary>
    /// Resolves tonight's strandings among the verdicts' accepted travellers, in
    /// queue order (the debug panel's "Force strandings" makes every Economy
    /// unit fail; its forced fate replaces the draw's). True when a fine or a
    /// tremor moved the wallet or stability (the caller checks the endings).
    /// </summary>
    public static bool Resolve(WorldState world, ShiftLedger ledger, IReadOnlyList<CaseInstance> cases, TodaysWorld today, ContentLibrarySO lib, GameConfigSO config)
    {
        if (world == null || ledger == null || cases == null || lib == null)
            return false;

        List<CaseInstance> accepted = ledger.verdicts
            .Where(v => v != null && v.accepted && v.caseIndex1Based >= 1 && v.caseIndex1Based <= cases.Count)
            .Select(v => cases[v.caseIndex1Based - 1])
            .Where(inst => inst != null)
            .ToList();
        if (accepted.Count == 0)
            return false;

        int daySeed = Seeds.Day(world.runSeed, world.day);
        float chance = DevToolsState.ForceStrandings ? 1f : lib.Agency.strandChance;
        var rng = new SeededRandom(Seeds.ForStrandings(daySeed));
        List<int> stranded = Strandings.Roll(accepted.Select(inst => inst.account != null ? inst.account.TransponderClass : (TransponderClass?)null).ToList(), chance, rng);
        if (DevToolsState.ForceStrandings)
            Debug.Log($"[ShiftStrandings] Force strandings: every accepted Economy traveller is stranded tonight ({stranded.Count}).");

        var fates = new SeededRandom(Seeds.ForStrandingFates(daySeed));
        bool moved = false;
        foreach (int i in stranded)
        {
            float fateValue = fates.Value();
            float lineValue = fates.Value();
            moved |= Strand(world, ledger, accepted[i], fateValue, lineValue, today, lib, config);
        }

        Debug.Log($"[ShiftStrandings] Day {world.day}: {accepted.Count} accepted, {stranded.Count} stranded (chance {chance:0.##}), fines {ledger.strandingFines} cr.");
        return moved;
    }

    /// <summary>One stranded traveller: the fate (<paramref name="fateValue"/>) and its line (<paramref name="lineValue"/>), both values of the day's fate stream; the fate's effect, the fine, the record and the counters. True when the wallet or stability moved.</summary>
    private static bool Strand(WorldState world, ShiftLedger ledger, CaseInstance inst, float fateValue, float lineValue, TodaysWorld today, ContentLibrarySO lib, GameConfigSO config)
    {
        List<StrandingFateRow> rows = lib.Agency.strandingFates;
        bool waivered = inst.Waivered;
        Personality personality = lib.Personalities.FirstOrDefault(p => p != null && p.id == inst.personality);
        StrandingFateRow row = DevToolsState.ForcedStrandingFate.HasValue
            ? StrandingFates.Of(rows, DevToolsState.ForcedStrandingFate.Value)
            : StrandingFates.Pick(rows, waivered, personality?.StrandingTilt, config != null ? config.strandingFateTilt : 1f, fateValue);
        if (row == null)
            Debug.LogWarning($"[ShiftStrandings] No stranding fate could be drawn for '{inst.visitorDisplayName}' (agency.strandingFates is empty or weightless): they are forgotten. Run Tools > TimeDesk > Generate World.");
        StrandingFate fate = row != null ? row.fate : StrandingFate.Forgotten;
        bool moved = false;

        if (fate == StrandingFate.Carry && !HistoryService.RecordStrandingCarry(world, inst, today, config))
        {
            fate = StrandingFate.News;
            row = StrandingFates.Of(rows, StrandingFate.News) ?? row;
        }
        else if (fate == StrandingFate.Tremor && row != null)
        {
            float before = StabilityRules.Round(world.timelineStability);
            world.timelineStability = StabilityRules.ApplyPercent(before, -row.stability);
            ledger.strandingStabilityDelta += world.timelineStability - before;
            moved = true;
        }

        int fine = waivered || config == null ? 0 : Mathf.Max(0, config.strandingFine);
        if (fine > 0)
        {
            world.money -= fine;
            ledger.strandingFines += fine;
            world.AddCounter(FinesCounter, fine);
            moved = true;
        }

        var record = new StrandingRecord
        {
            travellerName = inst.visitorDisplayName,
            placeLabel = inst.originLabel,
            day = world.day,
            fate = fate,
            waivered = waivered,
            fine = fine,
            citizenId = inst.account?.CitizenId ?? string.Empty,
            transponder = inst.account?.Transponder ?? string.Empty,
            waiverNo = inst.account?.WaiverNo ?? string.Empty,
            debt = inst.account != null ? inst.account.Debt : 0,
            line = StrandingFates.Line(row, inst.claimedEra != null ? inst.claimedEra.id : null, lib.News.stranded, inst.visitorDisplayName, inst.originLabel, lineValue)
        };
        world.history.pendingStrandings ??= new List<StrandingRecord>();
        world.history.pendingStrandings.Add(record);
        world.history.strandingLog ??= new List<StrandingRecord>();
        world.history.strandingLog.Add(record);

        ledger.strandedCount++;
        world.AddCounter(StrandedCounter, 1);
        world.AddCounter(FateCounter(fate), 1);
        Debug.Log($"[ShiftStrandings] Stranded: '{inst.visitorDisplayName}' in {inst.originLabel} ({inst.account?.Transponder}): {fate}, waiver {(waivered ? "on file" : "none")}, fine {fine} cr, stability {StabilityRules.Format(world.timelineStability)}.");
        return moved;
    }
}
