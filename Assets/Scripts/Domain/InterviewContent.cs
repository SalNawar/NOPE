using System;
using System.Collections.Generic;

// Serializable interview content. The generated ScriptableObjects
// (QuestionSO, DialogSO, ContentLibrarySO.interview) hold these directly, so
// the rules read them with no projection. Ids follow the generator's grammar
// (Tools > TimeDesk > Generate World) and key piece 6's string tables.

/// <summary>A line with a stable id.</summary>
[Serializable]
public sealed class LineText
{
    /// <summary>Stable id ("q_capital.answer", "interview.opener", "egypt_ancient.smalltalk.1", ...).</summary>
    public string id;

    /// <summary>The wording, possibly with {tokens}.</summary>
    public string text;

    /// <summary>An empty line (for serialization).</summary>
    public LineText()
    {
    }

    /// <summary>A line with its id and wording.</summary>
    public LineText(string id, string text)
    {
        this.id = id;
        this.text = text;
    }
}

/// <summary>One authored spoken line of a narrative dialog.</summary>
[Serializable]
public sealed class ScriptLine
{
    /// <summary>Stable id; starts with the dialog's id and a dot.</summary>
    public string id;

    /// <summary>Who says it.</summary>
    public DialogSpeaker speaker;

    /// <summary>The wording.</summary>
    public string text;

    /// <summary>Optional: a LookKeys.Expressions token; a premade's picture changes to it when the line is spoken.</summary>
    public string expression;
}

/// <summary>One authored reply the player can pick in a narrative dialog.</summary>
[Serializable]
public sealed class ScriptChoice
{
    /// <summary>Id within the dialog (the choice's line id is "{dialogId}.{id}").</summary>
    public string id;

    /// <summary>The menu entry, which the desk also speaks into the transcript.</summary>
    public string label;

    /// <summary>Lines spoken after the desk's label.</summary>
    public List<ScriptLine> lines = new();

    /// <summary>The node to go to; empty ends the dialog and returns to the hub.</summary>
    public string next;

    /// <summary>EffectSO asset name applied at the end of the shift (ending choices only); empty = none.</summary>
    public string effect;
}

/// <summary>One node of a narrative dialog.</summary>
[Serializable]
public sealed class ScriptNode
{
    /// <summary>Id within the dialog.</summary>
    public string id;

    /// <summary>Lines spoken on entering the node.</summary>
    public List<ScriptLine> lines = new();

    /// <summary>The replies offered at this node.</summary>
    public List<ScriptChoice> choices = new();
}

/// <summary>
/// An authored branching conversation, the data contract a runner consumes
/// (a future Yarn or Ink importer would produce this, too).
/// </summary>
[Serializable]
public sealed class AuthoredDialog
{
    /// <summary>Stable id (run memory: FlagKeys.DialogDone).</summary>
    public string id;

    /// <summary>The hub entry ("Any news from home? >").</summary>
    public string label;

    /// <summary>True when the dialog is offered at most once per run (the generator writes !repeatable).</summary>
    public bool oneShot = true;

    /// <summary>The nodes; nodes[0] is the start.</summary>
    public List<ScriptNode> nodes = new();
}

/// <summary>A line of one traveller kind (world_source.json interview.claims: one row per kind).</summary>
[Serializable]
public sealed class KindLine
{
    /// <summary>The kind that says it.</summary>
    public TravellerKind kind;

    /// <summary>The line ("interview.claims.{Kind}").</summary>
    public LineText line = new();
}

/// <summary>A request group's label (world_source.json interview.askGroups; traveller types I2): the forms sharing DocumentTemplateSO.askGroup are one papers-menu entry named by it ("Proof of means").</summary>
[Serializable]
public sealed class AskGroupLabel
{
    /// <summary>The group's id ("proof": AccountMaker.ProofGroup).</summary>
    public string id;

    /// <summary>The papers-menu entry ("Proof of means").</summary>
    public string label;
}

/// <summary>
/// Why a traveller lacks a form the desk asks for (traveller types I2, §6.1):
/// the line they answer with. Serialized in the library's missing-form
/// replies: append only (SerializedEnumsTests pins every value).
/// </summary>
public enum MissingFormVariant
{
    /// <summary>They never needed it (a rich tourist asked for a waiver: "It's a Premium unit, I don't need one").</summary>
    Honest,

    /// <summary>They should have it and left it out (a paper-set fault, the plan's phase 9: "I... didn't get round to that one").</summary>
    Missing
}

/// <summary>One missing-form reply (world_source.json interview.missingFormReplies): a kind's one-shot line when asked for a request (a form number or a group id) they carry no form of.</summary>
[Serializable]
public sealed class MissingFormReply
{
    /// <summary>The kind that says it.</summary>
    public TravellerKind kind;

    /// <summary>The request: a form number ("TC-310") or a request group's id ("proof"); FormRequests.IdOf.</summary>
    public string request;

    /// <summary>Why the form is missing.</summary>
    public MissingFormVariant variant;

    /// <summary>The line ("interview.missingFormReplies.{Kind}.{request}.{Variant}").</summary>
    public LineText line = new();
}

/// <summary>
/// A question's answer for some kinds of traveller, a claimed era, or both
/// (world_source.json questions[].overrides; the personalities spec's W3):
/// only the traveller's sentence changes, never the desk's words or the value.
/// The most specific override wins (ContextMatch: named kinds 2, a named era 1).
/// </summary>
[Serializable]
public sealed class WordingOverride
{
    /// <summary>The claimed era this answer is for (EraSO.id); blank: any era.</summary>
    public string eraId;

    /// <summary>The kinds of traveller this answer is for; empty: any kind. Serialized TravellerKind values (append only).</summary>
    public List<TravellerKind> kinds = new();

    /// <summary>The traveller's answer template ({value}).</summary>
    public LineText answer = new();
}

/// <summary>One interview question: a fact category, its menu label and wording; asked of every traveller in the same words (the personalities spec's W3), the answer's sentence by kind and era.</summary>
[Serializable]
public sealed class InterviewQuestion
{
    /// <summary>Stable id ("q_capital").</summary>
    public string id;

    /// <summary>The fact the answer gives (always provable: a book or the Citizen Record).</summary>
    public ClueCategory category;

    /// <summary>The ask-menu entry ("Capital").</summary>
    public string label;

    /// <summary>The desk's question, the same for every traveller ({place}: the claimed place's label).</summary>
    public LineText prompt = new();

    /// <summary>The traveller's default answer template (a 2150 citizen's); {value} is the canonical fact value.</summary>
    public LineText answer = new();

    /// <summary>The answer per kinds and claimed era (only the sentence changes, never the value).</summary>
    public List<WordingOverride> overrides = new();

    /// <summary>
    /// The answer template of a traveller of <paramref name="kind"/> claiming
    /// <paramref name="eraId"/>: the best-scoring override (ContextMatch:
    /// named kinds 2, a named era 1, summed; a tie keeps the first listed; an
    /// override of another kind or era never applies), else the default.
    /// </summary>
    public LineText AnswerFor(string eraId, TravellerKind kind)
    {
        LineText best = answer;
        int bestScore = ContextMatch.NoMatch;
        foreach (WordingOverride o in overrides ?? new List<WordingOverride>())
        {
            if (o == null)
                continue;
            int score = ContextMatch.Score(o.kinds, o.eraId, kind, eraId);
            if (score > bestScore)
            {
                bestScore = score;
                best = o.answer;
            }
        }

        return best;
    }
}

/// <summary>
/// The interview's fixed wording and spoken requests, plus the layout limit
/// content is checked against at run time and by the content validator: the most choices the
/// traveller wheel shows at once. (The longest line a transcript row holds is a
/// source-only limit, world_source.json interview.maxLineChars, which only
/// Generate World checks.)
/// </summary>
[Serializable]
public sealed class InterviewLines
{
    /// <summary>The desk's speaker label in the transcript ("DESK").</summary>
    public string deskName;

    /// <summary>The desk's opener ({honorific}).</summary>
    public LineText opener = new();

    /// <summary>The desk's opener for a legendary ({name}).</summary>
    public LineText openerLegendary = new();

    /// <summary>The traveller's claim per kind ({place}; traveller types §8: the displaced "Please. Send me home to {place}."): the spoken claim's default when their voice has no row (Voices.Claim).</summary>
    public List<KindLine> claims = new();

    /// <summary>Honorific for a traveller recorded as male ("sir").</summary>
    public string honorificMale;

    /// <summary>Honorific for a traveller recorded as female ("madam").</summary>
    public string honorificFemale;

    /// <summary>Honorific when the gender is unknown ("traveller").</summary>
    public string honorificUnknown;

    /// <summary>Hub entry for a traveller's one document on request ({document}).</summary>
    public string requestLabel;

    /// <summary>Hub entry that opens the papers menu when a traveller can be asked for two or more documents ("Request papers >"; traveller types I2).</summary>
    public string papersLabel;

    /// <summary>The request groups' labels (interview.askGroups): one papers-menu entry per group.</summary>
    public List<AskGroupLabel> askGroups = new();

    /// <summary>The missing-form replies (interview.missingFormReplies): what a kind says when asked for a request they carry no form of.</summary>
    public List<MissingFormReply> missingFormReplies = new();

    /// <summary>The desk's request ({document}).</summary>
    public LineText requestPrompt = new();

    /// <summary>The traveller's reply as they hand the document over.</summary>
    public LineText requestReply = new();

    /// <summary>Hub entry that opens the questions sub-menu, the same for every traveller ("Ask about the trip >": every traveller travels to their claim, the displaced home; the personalities spec's W2).</summary>
    public string askLabel;

    /// <summary>The ask menu's way back to the hub (always its first entry).</summary>
    public string backLabel;

    /// <summary>Ask-menu entry for small talk.</summary>
    public string smallTalkLabel;

    /// <summary>Hub entry that opens the look menu (the traveller's visible garments).</summary>
    public string lookLabel;

    /// <summary>The desk's small-talk question.</summary>
    public LineText smallTalkPrompt = new();

    /// <summary>The spoken requests every traveller is offered on the hub, after the document requests (world_source.json interview.requests).</summary>
    public List<InterviewRequest> requests = new();

    /// <summary>The most choices the traveller wheel shows at once (content never offers more).</summary>
    public int menuCapacity;

    /// <summary>How small talk picks its source: the personality's lines, the home's, the kind's (interview.smallTalkWeights; the personalities spec's V5).</summary>
    public SmallTalkWeights smallTalkWeights = new SmallTalkWeights();

    /// <summary>The kinds' small talk (interview.kindSmallTalk: rows naming kinds and optionally an era, no voice), one of small talk's three sources.</summary>
    public List<VoiceLine> kindSmallTalk = new List<VoiceLine>();

    /// <summary>The personalities' and premades' own lines, one list per slot (interview.voices); a voice with no matching row says the defaults above (Voices).</summary>
    public VoiceBook voices = new VoiceBook();

    /// <summary>The default reactions to the stamp (interview.reactions: verdict, intent, an optional reason, kinds and era, a line and an optional then line; the personalities spec's R1-R3): the four base rows are required.</summary>
    public List<VoiceLine> reactions = new List<VoiceLine>();

    /// <summary>The default slips (interview.slips, by lie kind; one with a blank lie is required; the personalities spec's T10).</summary>
    public List<VoiceLine> slips = new List<VoiceLine>();
}

/// <summary>
/// A request the desk makes that only makes the traveller answer ("Step
/// closer"): a hub entry of kind Request, once per traveller. It carries no
/// action; a request with a mechanic would add its DialogAction.
/// </summary>
[Serializable]
public sealed class InterviewRequest
{
    /// <summary>Stable id; the hub choice is "act:{id}" and the lines are "interview.requests.{id}.prompt" / ".reply".</summary>
    public string id;

    /// <summary>The hub entry ("Step closer").</summary>
    public string label;

    /// <summary>What the desk says.</summary>
    public LineText prompt = new();

    /// <summary>What the traveller answers (never evidence).</summary>
    public LineText reply = new();
}
