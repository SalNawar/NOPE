using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The shift's end (the traveller-types spec's S1-S3; redesign phase 13b):
/// rolls the accepted travellers for strandings (Strandings.Roll on the day's
/// stream, the real class from each traveller's Citizen Account), and for
/// each stranded one records the carry and the morning paper's line
/// (HistoryService.RecordStranding) and counts them on the ledger. A
/// stranding is a world consequence and moves no money (redesign phase 23:
/// the one wrong-decision penalty is the clerk's only fine). Every decision
/// is a Domain call; this class reads the cases, the content and the config
/// and writes the world, the ledger and the log.
/// </summary>
public static class ShiftStrandings
{
    /// <summary>The counter of strandings across the run (WorldState.counters; history rules and effects may read it).</summary>
    public const string StrandedCounter = "stranded";

    /// <summary>
    /// Resolves tonight's strandings among the verdicts' accepted travellers, in
    /// queue order (the debug panel's "Force strandings" makes every Economy
    /// unit fail).
    /// </summary>
    public static void Resolve(WorldState world, ShiftLedger ledger, IReadOnlyList<CaseInstance> cases, TodaysWorld today, ContentLibrarySO lib, GameConfigSO config)
    {
        if (world == null || ledger == null || cases == null || lib == null)
            return;

        List<CaseInstance> accepted = ledger.verdicts
            .Where(v => v != null && v.accepted && v.caseIndex1Based >= 1 && v.caseIndex1Based <= cases.Count)
            .Select(v => cases[v.caseIndex1Based - 1])
            .Where(inst => inst != null)
            .ToList();
        if (accepted.Count == 0)
            return;

        float chance = DevToolsState.ForceStrandings ? 1f : lib.Agency.strandChance;
        var rng = new SeededRandom(Seeds.ForStrandings(Seeds.Day(world.runSeed, world.day)));
        List<int> stranded = Strandings.Roll(accepted.Select(inst => inst.account != null ? inst.account.TransponderClass : (TransponderClass?)null).ToList(), chance, rng);
        if (DevToolsState.ForceStrandings)
            Debug.Log($"[ShiftStrandings] Force strandings: every accepted Economy traveller is stranded tonight ({stranded.Count}).");

        foreach (int i in stranded)
        {
            CaseInstance inst = accepted[i];
            ledger.strandedCount++;
            world.AddCounter(StrandedCounter, 1);
            HistoryService.RecordStranding(world, inst, today, config);
            Debug.Log($"[ShiftStrandings] Stranded: '{inst.visitorDisplayName}' in {inst.originLabel} ({inst.account?.Transponder}).");
        }

        Debug.Log($"[ShiftStrandings] Day {world.day}: {accepted.Count} accepted, {stranded.Count} stranded (chance {chance:0.##}).");
    }
}
