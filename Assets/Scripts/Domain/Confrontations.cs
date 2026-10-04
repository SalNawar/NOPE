using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// How a traveller answers the desk's question about a logged difference
/// (wave 5, Papers, Please lesson 3): an honest traveller explains (a slip
/// with a plausible reason, which clears nothing), a liar cracks (a
/// confession) or doubles down. Serialized in the voice rows
/// (interview.confront.replies, interview.voices.confront): append only
/// (SerializedEnumsTests pins every value).
/// </summary>
public enum ConfrontOutcome
{
    /// <summary>An honest traveller (ReactionIntent.Honest: a costume error included) explains the slip.</summary>
    Explain,

    /// <summary>A liar gives up the story.</summary>
    Crack,

    /// <summary>A liar insists.</summary>
    DoubleDown
}

/// <summary>
/// The desk's question about one kind of logged difference
/// (interview.confront.prompts): the proof, the kind of statement and
/// optionally the category it is for, and the desk's words with the
/// difference's tokens.
/// </summary>
[Serializable]
public sealed class ConfrontPrompt
{
    /// <summary>How the difference was proved.</summary>
    public DiscrepancyProof proof;

    /// <summary>What the statement was: DocumentField (papers), Answer (said) or Appearance (worn); a cross proof is always papers.</summary>
    public EvidenceKind source;

    /// <summary>The category it is for (a ClueCategory's name); blank: any category.</summary>
    public string category = string.Empty;

    /// <summary>The desk's words ("interview.confront.prompts.{n}"): {value} and {other} required in it or its then line, {category}, {place}, {document} (papers only) and {otherDocument} (a cross proof only) allowed.</summary>
    public LineText line = new LineText();

    /// <summary>The desk's second line ("{id}.then"; blank text: none), so each line of a question fits a transcript row: the same tokens.</summary>
    public LineText then = new LineText();
}

/// <summary>The wording of the wheel's questions about logged differences (world_source.json interview.confront).</summary>
[Serializable]
public sealed class ConfrontWording
{
    /// <summary>The hub entry that opens the differences menu ("Ask about a difference >"); shown only while a logged difference is still to be asked about.</summary>
    public string label;

    /// <summary>One difference's entry in that menu ({category}, {value}: "{category}: {value}?").</summary>
    public string entryLabel;

    /// <summary>The desk's questions, by proof, statement kind and optionally category.</summary>
    public List<ConfrontPrompt> prompts = new List<ConfrontPrompt>();

    /// <summary>The default replies, by outcome and optionally a fault reason or a lie kind (a base row per outcome is required); a voice with no row of its own says these.</summary>
    public List<VoiceLine> replies = new List<VoiceLine>();
}

/// <summary>
/// The wheel's questions about logged differences (wave 5, lesson 3): when
/// the player logs a difference (DiscrepancyLog), the traveller wheel gains a
/// question about exactly that difference, the same verb for every traveller;
/// the answer is the traveller's, in their voice. Pure.
/// </summary>
public static class Confrontations
{
    /// <summary>The token of the value the statement is held against (Discrepancy.ReportOther): the expected or recorded value, the other paper's, or the place the stated value belongs to.</summary>
    public const string OtherToken = "other";

    /// <summary>The token of the category's word ("Coin of Issue").</summary>
    public const string CategoryToken = "category";

    /// <summary>The token of a cross proof's other paper's name.</summary>
    public const string OtherDocumentToken = "otherDocument";

    /// <summary>Every (proof, statement kind) a logged difference can have: the prompts need a row for each (a cross proof is always two papers).</summary>
    public static readonly (DiscrepancyProof proof, EvidenceKind source)[] Kinds =
    {
        (DiscrepancyProof.ClaimMismatch, EvidenceKind.DocumentField),
        (DiscrepancyProof.ClaimMismatch, EvidenceKind.Answer),
        (DiscrepancyProof.ClaimMismatch, EvidenceKind.Appearance),
        (DiscrepancyProof.ForeignOrigin, EvidenceKind.DocumentField),
        (DiscrepancyProof.ForeignOrigin, EvidenceKind.Answer),
        (DiscrepancyProof.ForeignOrigin, EvidenceKind.Appearance),
        (DiscrepancyProof.RecordMismatch, EvidenceKind.DocumentField),
        (DiscrepancyProof.RecordMismatch, EvidenceKind.Answer),
        (DiscrepancyProof.RecordMismatch, EvidenceKind.Appearance),
        (DiscrepancyProof.CrossMismatch, EvidenceKind.DocumentField)
    };

    private static readonly Regex Token = new Regex(@"\{(\w+)\}");

    /// <summary>
    /// The difference a finding the workbench logged asks about (wave 5,
    /// lesson 3; the workbench's FindingLog raises the question): its proved
    /// deviation (Finding.Deviation, DiscrepancyLog.Prove's, the Deviation
    /// Report's evidence) when it is a difference; null for a match, a rule's
    /// or the calendar's verdict and a difference that proves nothing (an
    /// answer against a paper): no question, since no deviation is logged and
    /// a broken rule is the rule's to decide, not the traveller's to explain.
    /// </summary>
    public static Discrepancy About(Finding finding) =>
        finding != null && FindingRules.IsDifference(finding.Kind) ? finding.Deviation : null;

    /// <summary>The differences menu's entry id of a difference in <paramref name="category"/> ("confront:Currency"; one per category, as the Deviation Report holds).</summary>
    public static string ChoiceId(ClueCategory category) => "confront:" + category;

    /// <summary>
    /// How the traveller answers the question about a difference in
    /// <paramref name="category"/>: Explain for the honest (a costume error
    /// included: they do not know); a lying premade always doubles down (a
    /// story beat never confesses, days 7-15 B7); a generated liar who cracked
    /// earlier this case cracks again (<paramref name="crackedBefore"/>), else
    /// cracks when a value of the dialog seed and the category is below their
    /// personality's <paramref name="confess"/> chance (0 to 1; a value, never
    /// a draw, so asking in any order gives the same answers).
    /// </summary>
    public static ConfrontOutcome Outcome(ReactionIntent intent, bool premade, float confess, int dialogSeed, ClueCategory category, bool crackedBefore)
    {
        if (intent == ReactionIntent.Honest)
            return ConfrontOutcome.Explain;
        if (premade)
            return ConfrontOutcome.DoubleDown;
        if (crackedBefore)
            return ConfrontOutcome.Crack;
        double u = (uint)Seeds.Mix(dialogSeed, Seeds.OfKey(VoiceKeys.ConfrontRoll(category))) / 4294967296.0;
        return u < confess ? ConfrontOutcome.Crack : ConfrontOutcome.DoubleDown;
    }

    /// <summary>
    /// The desk's question for <paramref name="difference"/>: the row of its
    /// proof and statement kind (a cross proof's is papers) naming its
    /// category before a blank one, the first of the best in list order;
    /// null when none matches.
    /// </summary>
    public static ConfrontPrompt Prompt(IReadOnlyList<ConfrontPrompt> prompts, Discrepancy difference)
    {
        if (prompts == null || difference == null)
            return null;
        EvidenceKind source = difference.provedBy == DiscrepancyProof.CrossMismatch ? EvidenceKind.DocumentField : difference.source;
        ConfrontPrompt best = null;
        int bestScore = -1;
        foreach (ConfrontPrompt p in prompts)
        {
            if (p == null || p.proof != difference.provedBy || p.source != source)
                continue;
            int score = string.IsNullOrWhiteSpace(p.category) ? 0 : p.category == difference.category.ToString() ? 1 : -1;
            if (score > bestScore)
            {
                best = p;
                bestScore = score;
            }
        }
        return best;
    }

    /// <summary>
    /// The fills of a question and its answer: {value} the stated value,
    /// {other} what it is held against, {category} <paramref name="categoryWord"/>,
    /// {place} the claimed place, {document} and {otherDocument} the papers'
    /// names (blank when unknown).
    /// </summary>
    public static Dictionary<string, string> Fills(Discrepancy difference, string categoryWord, string place, string document, string otherDocument) =>
        new Dictionary<string, string>
        {
            { Interview.ValueToken, difference != null ? difference.documentValue : null },
            { OtherToken, difference != null ? difference.ReportOther : null },
            { CategoryToken, categoryWord },
            { Interview.PlaceToken, place },
            { Interview.DocumentToken, document },
            { OtherDocumentToken, otherDocument }
        };

    /// <summary><paramref name="template"/> with every token of <paramref name="fills"/> filled (Interview.Fill; a null fill reads blank).</summary>
    public static string Fill(string template, IReadOnlyDictionary<string, string> fills)
    {
        string text = template;
        if (fills != null)
            foreach (KeyValuePair<string, string> fill in fills)
                text = Interview.Fill(text, fill.Key, fill.Value);
        return text;
    }

    /// <summary>
    /// Every problem of the wording: a blank label or entry label, an entry
    /// label with a token other than {category} and {value}; a prompt row that
    /// is empty, names an unknown category, holds no {value} or {other}, holds
    /// an unknown token, {document} on a statement that is not papers or
    /// {otherDocument} on a proof that is not a cross one; and a missing base
    /// row (blank category) for any of <see cref="Kinds"/>. The replies are
    /// voice rows, checked with the voices (VoiceChecks).
    /// </summary>
    public static List<string> Problems(ConfrontWording wording)
    {
        var problems = new List<string>();
        if (wording == null)
        {
            problems.Add("interview.confront is missing: the wheel cannot ask about a logged difference.");
            return problems;
        }
        if (string.IsNullOrWhiteSpace(wording.label))
            problems.Add("interview.confront.label is blank.");
        if (string.IsNullOrWhiteSpace(wording.entryLabel))
            problems.Add("interview.confront.entryLabel is blank.");
        else
            foreach (string token in Tokens(wording.entryLabel))
                if (token != CategoryToken && token != Interview.ValueToken)
                    problems.Add($"interview.confront.entryLabel holds {{{token}}}; an entry names only {{category}} and {{value}}.");

        List<ConfrontPrompt> prompts = wording.prompts ?? new List<ConfrontPrompt>();
        for (int i = 0; i < prompts.Count; i++)
        {
            ConfrontPrompt p = prompts[i];
            string at = $"interview.confront.prompts row {i + 1}";
            if (p == null || p.line == null || string.IsNullOrWhiteSpace(p.line.text))
            {
                problems.Add($"{at} is empty.");
                continue;
            }
            if (!string.IsNullOrWhiteSpace(p.category) && !(Enum.TryParse(p.category, out ClueCategory c) && Enum.IsDefined(typeof(ClueCategory), c) && c.ToString() == p.category))
                problems.Add($"{at} names the category '{p.category}', which is no clue category.");
            if (Array.IndexOf(Kinds, (p.proof, p.source)) < 0)
                problems.Add($"{at} is for {p.proof} · {p.source}, which no logged difference is (a cross proof is always DocumentField).");
            string both = p.line.text + " " + (p.then != null ? p.then.text : string.Empty);
            foreach (string required in new[] { Interview.ValueToken, OtherToken })
                if (!Interview.HoldsToken(both, required))
                    problems.Add($"{at} must hold {{{required}}} (in its text or its then line): the question names exactly the difference.");
            foreach (string token in Tokens(both))
            {
                if (token != Interview.ValueToken && token != OtherToken && token != CategoryToken && token != Interview.PlaceToken &&
                    token != Interview.DocumentToken && token != OtherDocumentToken)
                    problems.Add($"{at} holds the unknown token {{{token}}}.");
                else if (token == Interview.DocumentToken && p.source != EvidenceKind.DocumentField)
                    problems.Add($"{at} holds {{document}}, but a statement that was {(p.source == EvidenceKind.Answer ? "said" : "worn")} is on no paper.");
                else if (token == OtherDocumentToken && p.proof != DiscrepancyProof.CrossMismatch)
                    problems.Add($"{at} holds {{otherDocument}}, which only a cross proof (two papers) has.");
            }
        }
        foreach ((DiscrepancyProof proof, EvidenceKind source) in Kinds)
            if (!prompts.Exists(p => p != null && p.proof == proof && p.source == source && string.IsNullOrWhiteSpace(p.category) && p.line != null && !string.IsNullOrWhiteSpace(p.line.text)))
                problems.Add($"interview.confront.prompts has no base row for {proof} · {source} (blank category): every logged difference needs a question.");
        return problems;
    }

    /// <summary>The tokens a template holds, by name, in order.</summary>
    private static IEnumerable<string> Tokens(string template)
    {
        foreach (Match m in Token.Matches(template ?? string.Empty))
            yield return m.Groups[1].Value;
    }
}
