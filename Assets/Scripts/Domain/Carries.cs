using System;
using System.Collections.Generic;

/// <summary>
/// An accepted liar carries their true home's fact into the place they
/// claimed: recorded at accept, latched at night once the (home, claim)
/// pair has been accepted often enough. Honest travellers never carry.
/// </summary>
public static class Carries
{
    /// <summary>
    /// The carry of an accepted liar: the true home's value for
    /// <paramref name="category"/> in today's table, headed for the claim.
    /// Null when an id is blank, home equals claim, the category is not
    /// editable, the table is null or has no value for the home, or the claim
    /// already has that value (DiscrepancyLog.ValuesMatch).
    /// </summary>
    public static CarryRecord Make(string fromNationId, string fromEraId, string toNationId, string toEraId,
                                   ClueCategory category, FactTable today, int day)
    {
        if (string.IsNullOrWhiteSpace(fromNationId) || string.IsNullOrWhiteSpace(fromEraId) ||
            string.IsNullOrWhiteSpace(toNationId) || string.IsNullOrWhiteSpace(toEraId) ||
            !History.IsEditable(category) || today == null)
            return null;

        if (string.Equals(fromNationId, toNationId, StringComparison.Ordinal) && string.Equals(fromEraId, toEraId, StringComparison.Ordinal))
            return null;

        string value = today.Get(fromNationId, fromEraId, category);
        if (string.IsNullOrWhiteSpace(value) || DiscrepancyLog.ValuesMatch(value, today.Get(toNationId, toEraId, category)))
            return null;

        return new CarryRecord
        {
            fromNationId = fromNationId, fromEraId = fromEraId, toNationId = toNationId, toEraId = toEraId,
            category = category, value = value, day = day
        };
    }

    /// <summary>
    /// Promotes the pending carries at night. Pairs (home, claim, category) are
    /// taken in order of their first record; a pair with at least
    /// <paramref name="threshold"/> records (below 1 counts as 1) is due: its
    /// latest record's value is latched for the claim from
    /// <paramref name="sinceDay"/> (History.Latch, EditCause.Carry, source =
    /// the home's label in <paramref name="world"/>), unless it equals the
    /// claim's current value (the value an edit latched earlier in this call
    /// gave it, else <paramref name="world"/>'s). A due pair's records are
    /// consumed either way; pairs below the threshold keep theirs. Returns the
    /// latched edits in order (empty for a null state or world).
    /// </summary>
    public static List<FactEdit> Promote(HistoryState history, int threshold, int sinceDay, FactTable world)
    {
        var latched = new List<FactEdit>();
        if (history == null || world == null || history.pendingCarries == null || history.pendingCarries.Count == 0)
            return latched;

        int need = Math.Max(1, threshold);
        var pairs = new List<(string from, string fromEra, string to, string toEra, ClueCategory category)>();
        var records = new Dictionary<(string, string, string, string, ClueCategory), List<CarryRecord>>();
        foreach (CarryRecord r in history.pendingCarries)
        {
            if (r == null)
                continue;
            var key = (r.fromNationId, r.fromEraId, r.toNationId, r.toEraId, r.category);
            if (!records.TryGetValue(key, out List<CarryRecord> list))
            {
                records.Add(key, list = new List<CarryRecord>());
                pairs.Add(key);
            }
            list.Add(r);
        }

        var consumed = new HashSet<CarryRecord>();
        foreach (var pair in pairs)
        {
            List<CarryRecord> list = records[pair];
            if (list.Count < need)
                continue;

            consumed.UnionWith(list);
            string value = list[list.Count - 1].value;
            FactEdit earlier = latched.FindLast(e => e.nationId == pair.to && e.eraId == pair.toEra && e.category == pair.category);
            string current = earlier != null ? earlier.value : world.Get(pair.to, pair.toEra, pair.category);
            if (DiscrepancyLog.ValuesMatch(value, current))
                continue;

            string source = world.OriginLabel(pair.from, pair.fromEra) ?? $"{pair.from}_{pair.fromEra}";
            var edit = new FactEdit(pair.to, pair.toEra, pair.category, value, sinceDay, EditCause.Carry, source);
            if (History.Latch(history, edit))
                latched.Add(edit);
        }

        history.pendingCarries.RemoveAll(r => r == null || consumed.Contains(r));
        return latched;
    }
}
