using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// The dev overlay's Timeline Inspector tab and its console state dump
/// (DebugPanelController draws it): live scores, dominance tiers, active
/// effects, history and the present culture. The score and effect lines are
/// written once for both (audit R2-027). Read-only: it changes nothing.
/// </summary>
internal static class DebugInspector
{
    /// <summary>Draws the tab (inside the overlay's GUI pass).</summary>
    public static void Draw(WorldState world, ContentLibrarySO lib)
    {
        if (GUILayout.Button("Dump full state to console"))
            Dump(world, lib);

        GUILayout.Space(6f);
        GUILayout.Label($"Scores ({world.timeline.scores.Count}):");
        foreach (string line in ScoreLines(world))
            GUILayout.Label(line);

        GUILayout.Space(6f);
        GUILayout.Label($"Dominant keys ({world.timeline.dominantKeys.Count}):");
        foreach (string key in world.timeline.dominantKeys)
            GUILayout.Label("  " + key);

        GUILayout.Space(6f);
        GUILayout.Label($"Supporting keys ({world.timeline.supportingKeys.Count}):");
        foreach (string key in world.timeline.supportingKeys)
            GUILayout.Label("  " + key);

        GUILayout.Space(6f);
        GUILayout.Label($"Active effects ({world.timeline.activeEffects.Count}):");
        foreach (ActiveEffectEntry entry in world.timeline.activeEffects)
        {
            string remaining = entry.durationDays < 0
                ? "permanent"
                : $"{Mathf.Max(0, entry.startDay + entry.durationDays - world.day)}d left";
            GUILayout.Label($"  {EffectName(lib, entry)} — {entry.sourceLabel} (started day {entry.startDay}, {remaining})");
        }

        GUILayout.Space(6f);
        foreach (string line in HistorySummary(world))
            GUILayout.Label(line);

        GUILayout.Space(6f);
        foreach (string line in CultureSummary())
            GUILayout.Label(line);
    }

    /// <summary>Logs a full snapshot of WorldState + timeline data to the console.</summary>
    public static void Dump(WorldState world, ContentLibrarySO lib)
    {
        var sb = new StringBuilder();

        sb.AppendLine("[DebugPanelController] ---- State dump ----");
        sb.AppendLine($"Day {world.day}, money={world.money}, stability={world.timelineStability:0.#}, endingId='{world.endingId}'.");
        sb.AppendLine($"legendaryChanceBonus={world.legendaryChanceBonus:0.##}, forgeryChanceModifier={world.forgeryChanceModifier:0.##}, payRateMultiplier={world.payRateMultiplier:0.##}.");
        sb.AppendLine($"Flags ({world.flags.Count}): {string.Join(", ", world.flags)}");
        sb.AppendLine($"Unlocked upgrades ({world.unlockedUpgradeIds.Count}): {string.Join(", ", world.unlockedUpgradeIds)}");

        sb.AppendLine($"Counters ({world.counters.Count}):");
        foreach (CounterEntry c in world.counters)
            sb.AppendLine($"  {c.key} = {c.value}");

        sb.AppendLine($"Scores ({world.timeline.scores.Count}):");
        foreach (string line in ScoreLines(world))
            sb.AppendLine(line);

        sb.AppendLine($"Dominant: {string.Join(", ", world.timeline.dominantKeys)}");
        sb.AppendLine($"Supporting: {string.Join(", ", world.timeline.supportingKeys)}");

        sb.AppendLine($"Active effects ({world.timeline.activeEffects.Count}):");
        foreach (ActiveEffectEntry entry in world.timeline.activeEffects)
            sb.AppendLine($"  {EffectName(lib, entry)} — {entry.sourceLabel} (startDay={entry.startDay}, durationDays={entry.durationDays})");

        foreach (string line in HistorySummary(world))
            sb.AppendLine(line);

        Debug.Log(sb.ToString());
    }

    /// <summary>One line per timeline score, as the tab and the dump print them.</summary>
    private static IEnumerable<string> ScoreLines(WorldState world)
    {
        foreach (ScoreEntry s in world.timeline.scores)
            yield return $"  {s.key} = {s.value:0.##}";
    }

    /// <summary>An active effect's display name, or its asset name when the library has no such effect.</summary>
    private static string EffectName(ContentLibrarySO lib, ActiveEffectEntry entry)
    {
        EffectSO effect = lib != null ? lib.GetEffectByAssetName(entry.effectId) : null;
        return effect != null ? effect.displayName : entry.effectId;
    }

    /// <summary>The present culture as the inspector prints it (piece 6): the cue's culture, the theme, label language, font, wallet word and missing UI strings.</summary>
    private static IEnumerable<string> CultureSummary()
    {
        CultureThemeService s = CultureThemeService.Instance;
        if (s == null)
        {
            yield return "Present culture: no theme service (run Tools > TimeDesk > Generate World)";
            yield break;
        }
        yield return $"Present culture: {s.ActiveCultureId ?? "neutral"} (cue)";
        yield return $"Theme: {s.ActiveTheme.displayName} · labels: {s.Language} · font: {s.FontName} · wallet: {UiText.Currency(UiText.WalletForm.Label)}";
        yield return $"Missing UI strings: {s.Strings.MissingKeys.Count}";
    }

    /// <summary>The history as the inspector and the state dump print it: leader, ranking, fact edits, pending carries.</summary>
    private static IEnumerable<string> HistorySummary(WorldState world)
    {
        HistoryState h = world.history;
        string forced = !string.IsNullOrEmpty(DevToolsState.ForcedLeaderId) ? " (forced)" : string.Empty;
        yield return $"History: leader '{h.leaderId}' (since day {h.leaderSinceDay}){forced}";
        yield return $"Ranking: {string.Join(", ", h.ranking.Select(r => $"{r.id} {r.score:0.#}"))}";
        yield return $"Fact edits ({h.factEdits.Count}):";
        foreach (FactEdit e in h.factEdits.Where(e => e != null))
            yield return $"  {e.nationId}_{e.eraId} {e.category} = '{e.value}' (from day {e.sinceDay}, {e.cause}: {e.source})";
        yield return $"Pending carries ({h.pendingCarries.Count}):";
        foreach (CarryRecord c in h.pendingCarries.Where(c => c != null))
            yield return $"  {c.fromNationId}_{c.fromEraId} -> {c.toNationId}_{c.toEraId}: '{c.value}' ({c.category}, day {c.day})";
    }
}
