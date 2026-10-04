using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>One value a citation slip names (lesson 6): where it was read and what it said ("Signature on the Stranding Waiver", "UNSIGNED").</summary>
public readonly struct CitationValue
{
    /// <summary>Where the value was read (a paper's box, the record, the calendar).</summary>
    public readonly string Label;

    /// <summary>What it said.</summary>
    public readonly string Value;

    /// <summary>Creates a value.</summary>
    public CitationValue(string label, string value)
    {
        Label = label;
        Value = value;
    }
}

/// <summary>
/// What a citation slip names about a traveller (Papers Please lesson 6, "the
/// citation slip names the exact rule and the exact values involved"; wave 5
/// track C): the rule broken, as its own line (a directive's summary) or a
/// check's line from the UI strings, its row on today's Directive Memo, and
/// the values that break it. Built once per traveller at generation
/// (CaseFactory, CaseInstance.citation), for the fault they carry or, for an
/// honest traveller, the line a wrong denial prints; runtime only.
/// </summary>
public sealed class CitationFacts
{
    /// <summary>The rule's own line (a directive's summary); null when the check's line is a UI string (<see cref="RuleKey"/>).</summary>
    public string RuleText;

    /// <summary>The UI string key of the check's line, read when <see cref="RuleText"/> is blank ("citation.rule.record").</summary>
    public string RuleKey;

    /// <summary>The rule's row on today's Directive Memo, from 1 (Citations.MemoNumber); 0 when the rule is no row of it.</summary>
    public int DirectiveNumber;

    /// <summary>The values involved, in the order the slip prints them.</summary>
    public readonly List<CitationValue> Values = new List<CitationValue>();
}

/// <summary>
/// The citation slip's rule and values lines (lesson 6), and the decisions
/// behind them: the Directive Memo's row of a rule (numbered as
/// DirectiveMemoPage.Rows numbers it), and which condition of a paper set
/// broke. The first mistake of a day stays a free warning
/// (VerdictRules.IsFreeWarning); these lines only name what was wrong. Pure.
/// </summary>
public static class Citations
{
    /// <summary>The breach keys of a paper set, a UI string key's suffix ("citation.rule.paperSet." + key).</summary>
    public const string EconomyManifest = "economyManifest", PremiumManifest = "premiumManifest", WaiverMissing = "waiverMissing",
                        WaiverUnsigned = "waiverUnsigned", ProofMissing = "proofMissing", ContractMissing = "contractMissing";

    /// <summary>
    /// The row of the rule at <paramref name="index"/> of today's rules on the
    /// Directive Memo, whose lines are <paramref name="summaries"/>: rows
    /// count the rules with a line, from 1 (DirectiveMemoPage.Rows); 0 for an
    /// index outside the list or a rule with no line.
    /// </summary>
    public static int MemoNumber(IReadOnlyList<string> summaries, int index)
    {
        if (summaries == null || index < 0 || index >= summaries.Count || string.IsNullOrWhiteSpace(summaries[index]))
            return 0;
        int row = 0;
        for (int i = 0; i <= index; i++)
            if (!string.IsNullOrWhiteSpace(summaries[i]))
                row++;
        return row;
    }

    /// <summary>
    /// Which condition of the paper set <paramref name="facts"/> breaks, in
    /// the predicate's order (Directives: a Premium visa on an Economy
    /// manifest, a Standard visa on a Premium manifest, a missing contract, a
    /// missing or unsigned waiver, a missing proof of means; a form not
    /// issued yet is never asked for): its breach key, or null when the set
    /// holds (or the kind has none).
    /// </summary>
    public static string PaperSetBreach(CaseFacts facts)
    {
        if (facts == null)
            return null;
        bool tourist = facts.Kind == TravellerKind.RichTourist || facts.Kind == TravellerKind.PoorTourist;
        if (tourist && facts.VisaClass == CitizenStatus.Premium)
            return facts.ManifestClass == TransponderClass.Economy ? EconomyManifest : null;
        if (tourist && facts.VisaClass == null)
            return null;
        if (!tourist && facts.Kind != TravellerKind.Labourer)
            return null;
        if (facts.Kind == TravellerKind.Labourer && facts.IsIssued(Directives.Contract) && !facts.Carries(Directives.Contract))
            return ContractMissing;
        if (facts.ManifestClass == TransponderClass.Premium)
            return PremiumManifest;
        if (facts.IsIssued(Directives.Waiver) && !facts.WaiverSigned)
            return facts.Carries(Directives.Waiver) ? WaiverUnsigned : WaiverMissing;
        if (tourist && Directives.Proofs.Any(facts.IsIssued) && !Directives.Proofs.Any(facts.Carries))
            return ProofMissing;
        return null;
    }

    /// <summary>
    /// The slip's rule line: the rule's own line, else the check's line
    /// (<paramref name="text"/> of the facts' RuleKey), prefixed with its memo
    /// row through <paramref name="numbered"/> ("Directive {0}: {1}") when it
    /// has one; empty without facts or a line.
    /// </summary>
    public static string RuleLine(CitationFacts facts, Func<string, string> text, string numbered)
    {
        if (facts == null)
            return string.Empty;
        string rule = !string.IsNullOrWhiteSpace(facts.RuleText) ? facts.RuleText.Trim()
            : !string.IsNullOrEmpty(facts.RuleKey) && text != null ? text(facts.RuleKey) : null;
        if (string.IsNullOrWhiteSpace(rule))
            return string.Empty;
        return facts.DirectiveNumber > 0 && !string.IsNullOrEmpty(numbered) ? string.Format(numbered, facts.DirectiveNumber, rule) : rule;
    }

    /// <summary>
    /// The slip's values line: each value through <paramref name="format"/>
    /// ("{0}: {1}.", label then value), joined by <paramref name="separator"/>;
    /// a value with a blank label prints the value alone, a blank value is
    /// skipped. Empty without values.
    /// </summary>
    public static string ValuesLine(IReadOnlyList<CitationValue> values, string format, string separator)
    {
        if (values == null)
            return string.Empty;
        var parts = new List<string>();
        foreach (CitationValue v in values)
        {
            if (string.IsNullOrWhiteSpace(v.Value))
                continue;
            parts.Add(string.IsNullOrWhiteSpace(v.Label) || string.IsNullOrEmpty(format) ? v.Value.Trim() : string.Format(format, v.Label.Trim(), v.Value.Trim()));
        }
        return string.Join(separator ?? " ", parts);
    }
}
