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

/// <summary>Per-era wording of a question: chosen by the traveller's claimed era.</summary>
[Serializable]
public sealed class WordingOverride
{
    /// <summary>The claimed era this wording is for (EraSO.id).</summary>
    public string eraId;

    /// <summary>The desk's question.</summary>
    public LineText prompt = new();

    /// <summary>The traveller's answer template ({value}).</summary>
    public LineText answer = new();
}

/// <summary>One interview question: a fact category, its menu label and wording.</summary>
[Serializable]
public sealed class InterviewQuestion
{
    /// <summary>Stable id ("q_capital").</summary>
    public string id;

    /// <summary>The fact the answer gives (always provable: a book or the Citizen Record).</summary>
    public ClueCategory category;

    /// <summary>The ask-menu entry ("Capital").</summary>
    public string label;

    /// <summary>The desk's question.</summary>
    public LineText prompt = new();

    /// <summary>The traveller's answer template; {value} is the canonical fact value.</summary>
    public LineText answer = new();

    /// <summary>Wording per claimed era (only the sentence changes, never the value).</summary>
    public List<WordingOverride> overrides = new();

    /// <summary>The question as asked of a traveller claiming <paramref name="eraId"/>: that era's override, else the default.</summary>
    public LineText PromptFor(string eraId)
    {
        WordingOverride o = OverrideFor(eraId);
        return o != null ? o.prompt : prompt;
    }

    /// <summary>The answer template for a traveller claiming <paramref name="eraId"/>: that era's override, else the default.</summary>
    public LineText AnswerFor(string eraId)
    {
        WordingOverride o = OverrideFor(eraId);
        return o != null ? o.answer : answer;
    }

    /// <summary>The override for an era, or null.</summary>
    private WordingOverride OverrideFor(string eraId)
    {
        if (string.IsNullOrEmpty(eraId) || overrides == null)
            return null;

        foreach (WordingOverride o in overrides)
            if (o != null && o.eraId == eraId)
                return o;

        return null;
    }
}

/// <summary>
/// The interview's fixed wording, plus the layout limit content is checked
/// against at run time and by the content validator: the most choices the
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

    /// <summary>The traveller's claim ({place}); also the banner and the shift summary.</summary>
    public LineText claim = new();

    /// <summary>Honorific for a traveller recorded as male ("sir").</summary>
    public string honorificMale;

    /// <summary>Honorific for a traveller recorded as female ("madam").</summary>
    public string honorificFemale;

    /// <summary>Honorific when the gender is unknown ("traveller").</summary>
    public string honorificUnknown;

    /// <summary>Hub entry per document ({document}).</summary>
    public string requestLabel;

    /// <summary>The desk's request ({document}).</summary>
    public LineText requestPrompt = new();

    /// <summary>The traveller's reply as they hand the document over.</summary>
    public LineText requestReply = new();

    /// <summary>Hub entry that opens the questions sub-menu.</summary>
    public string askLabel;

    /// <summary>The ask menu's way back to the hub (always its first entry).</summary>
    public string backLabel;

    /// <summary>Ask-menu entry for small talk.</summary>
    public string smallTalkLabel;

    /// <summary>The desk's small-talk question.</summary>
    public LineText smallTalkPrompt = new();

    /// <summary>The most choices the traveller wheel shows at once (content never offers more).</summary>
    public int menuCapacity;
}
