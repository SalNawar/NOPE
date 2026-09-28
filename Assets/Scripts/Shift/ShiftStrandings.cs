using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The shift's end (the traveller-types spec's S1-S3; redesign phase 13b):
/// rolls the accepted travellers for strandings (Strandings.Roll on the day's
/// stream, the real class from each traveller's Citizen Account), and for
/// each stranded one records the carry and the morning paper's line
/// (HistoryService.RecordStranding), counts them on the ledger and takes the
/// fine from the wallet when they were let through without a valid signed
/// waiver (Strandings.Fine, agency.strandFine). Every decision is a Domain
/// call; this class reads the cases, the content and the config and writes
/// the world, the ledger and the log.
/// </summary>
public static class ShiftStrandings
{
    /// <summary>The counter of strandings across the run (WorldState.counters; history rules and effects may read it).</summary>
    public const string StrandedCounter = "stranded";

    /// <summary>
    /// Resolves tonight's strandings among the verdicts' accepted travellers, in
    /// queue order (the debug panel's "Force strandings" makes every Economy
    /// unit fail). Returns the fines taken from the wallet (0 when nothing was
    /// stranded or nothing fined), so the caller knows the wallet moved.
    /// </summary>
    public static int Resolve(WorldState world, ShiftLedger ledger, IReadOnlyList<CaseInstance> cases, TodaysWorld today, ContentLibrarySO lib, GameConfigSO config)
    {
        if (world == null || ledger == null || cases == null || lib == null)
            return 0;

        List<CaseInstance> accepted = ledger.verdicts
            .Where(v => v != null && v.accepted && v.caseIndex1Based >= 1 && v.caseIndex1Based <= cases.Count)
            .Select(v => cases[v.caseIndex1Based - 1])
            .Where(inst => inst != null)
            .ToList();
        if (accepted.Count == 0)
            return 0;

        float chance = DevToolsState.ForceStrandings ? 1f : lib.Agency.strandChance;
        var rng = new SeededRandom(Seeds.ForStrandings(Seeds.Day(world.runSeed, world.day)));
        List<int> stranded = Strandings.Roll(accepted.Select(inst => inst.account != null ? inst.account.TransponderClass : (TransponderClass?)null).ToList(), chance, rng);
        if (DevToolsState.ForceStrandings)
            Debug.Log($"[ShiftStrandings] Force strandings: every accepted Economy traveller is stranded tonight ({stranded.Count}).");

        int fines = 0;
        foreach (int i in stranded)
        {
            CaseInstance inst = accepted[i];
            int fine = Strandings.Fine(inst.waiverStanding, lib.Agency.strandFine);
            world.money -= fine;
            fines += fine;
            ledger.strandedCount++;
            world.AddCounter(StrandedCounter, 1);
            HistoryService.RecordStranding(world, inst, today, config);
            Debug.Log($"[ShiftStrandings] Stranded: '{inst.visitorDisplayName}' in {inst.originLabel} ({inst.account?.Transponder}, waiver {inst.waiverStanding}); fine {fine} cr.");
        }
        ledger.strandingFines += fines;

        Debug.Log($"[ShiftStrandings] Day {world.day}: {accepted.Count} accepted, {stranded.Count} stranded (chance {chance:0.##}), fines {fines} cr, money {world.money}.");
        return fines;
    }
}
