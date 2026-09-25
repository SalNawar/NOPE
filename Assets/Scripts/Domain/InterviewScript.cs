using System.Collections.Generic;

/// <summary>One traveller's interview input, from their case.</summary>
public sealed class InterviewCase
{
    /// <summary>The desk's opener (CaseInstance.introLine); skipped when blank.</summary>
    public string introLine;

    /// <summary>The traveller's claim sentence (CaseInstance.claimLine).</summary>
    public string claimLine;

    /// <summary>The claimed era's id: picks each question's wording override.</summary>
    public string claimedEraId;

    /// <summary>The traveller's documents in paper order; only those handed over on request get a hub request.</summary>
    public IReadOnlyList<CaseDocument> documents;

    /// <summary>The traveller's answers to today's askable questions (CaseInstance.answers).</summary>
    public IReadOnlyList<InterviewAnswer> answers;

    /// <summary>The traveller's small-talk line; null means no small talk.</summary>
    public LineText smallTalk;

    /// <summary>The traveller's visible garments (TravellerLook.Garments); none means no look menu.</summary>
    public IReadOnlyList<Garment> garments;
}

/// <summary>
/// Builds a traveller's interview graph: the hub (document requests, "Ask
/// about home >", "Look >", today's narrative dialogs), the ask menu ("&lt;
/// Back" first, then the questions and small talk), the look menu ("&lt;
/// Back" first, then one choice per visible garment) and every authored
/// dialog's nodes. Pure, so every menu and line is tested headless.
/// </summary>
public static class InterviewScript
{
    /// <summary>The hub node's id.</summary>
    public const string HubNodeId = "hub";

    /// <summary>The ask menu's id.</summary>
    public const string AskNodeId = "ask";

    /// <summary>The look menu's id.</summary>
    public const string LookNodeId = "look";

    /// <summary>The id of the desk's opener line, composed per case at runtime (Generate World reserves it).</summary>
    public const string IntroLineId = "case.intro";

    /// <summary>The id of the traveller's claim line, composed per case at runtime (Generate World reserves it).</summary>
    public const string ClaimLineId = "case.claim";

    /// <summary>
    /// The id of an authored choice: "{dialogId}.{choiceId}". It is both the
    /// runtime choice id and the id of the label line the desk speaks, so
    /// Generate World checks every choice under this same id.
    /// </summary>
    public static string ChoiceLineId(string dialogId, string choiceId) => $"{dialogId}.{choiceId}";

    /// <summary>The transcript's first lines: the desk's opener (<see cref="IntroLineId"/>, skipped when blank), then the traveller's claim (<see cref="ClaimLineId"/>).</summary>
    public static IReadOnlyList<DialogLine> Opening(InterviewCase c)
    {
        var lines = new List<DialogLine>();
        if (c == null)
            return lines;

        if (!string.IsNullOrWhiteSpace(c.introLine))
            lines.Add(new DialogLine(IntroLineId, DialogSpeaker.Desk, c.introLine));
        lines.Add(new DialogLine(ClaimLineId, DialogSpeaker.Traveller, c.claimLine));
        return lines;
    }

    /// <summary>The desk asking <paramref name="q"/> of a traveller claiming <paramref name="eraId"/>.</summary>
    public static DialogLine PromptLine(InterviewQuestion q, string eraId)
    {
        LineText prompt = q.PromptFor(eraId);
        return new DialogLine(prompt.id, DialogSpeaker.Desk, prompt.text);
    }

    /// <summary>The traveller's answer line: the (era's) template with the canonical value, carrying the answer's fact.</summary>
    public static DialogLine AnswerLine(InterviewQuestion q, string eraId, InterviewAnswer a)
    {
        LineText answer = q.AnswerFor(eraId);
        return DialogLine.Answer(answer.id, Interview.Fill(answer.text, Interview.ValueToken, a != null ? a.value : null), a);
    }

    /// <summary>
    /// What the traveller said since transcript line <paramref name="from"/>:
    /// the texts of the Traveller lines at or after it, in order, joined with a
    /// new line; empty when there are none. A null transcript gives ""; a from
    /// below 0 counts as 0. (The reply the traveller wheel's bubble shows.)
    /// </summary>
    public static string SpokenSince(IReadOnlyList<DialogLine> transcript, int from)
    {
        if (transcript == null)
            return string.Empty;

        var said = new List<string>();
        for (int i = from < 0 ? 0 : from; i < transcript.Count; i++)
        {
            DialogLine line = transcript[i];
            if (line != null && line.Speaker == DialogSpeaker.Traveller)
                said.Add(line.Text);
        }

        return string.Join("\n", said);
    }

    /// <summary>
    /// The traveller's graph. Hub: "request:{i}" per document handed over on
    /// request (one-shot, hands document i over), then "ask" when the ask menu has a question or
    /// small talk, then "look" when the traveller has a visible garment, then
    /// "dlg:{id}" per dialog (one-shot). Ask: "back" first (so an overlong
    /// menu can never hide the way back), then "q:{id}" per question the
    /// traveller has an answer for (one-shot), then "smalltalk". Look: "back"
    /// first, then "look:{i}" per garment, labelled with the item's name
    /// (InspectGarment, never one-shot, no line). Authored nodes become
    /// "{dialogId}/{nodeId}" and their choices "{dialogId}.{choiceId}" (the
    /// desk speaks the label); every authored line keeps its expression; an
    /// ending choice returns to the hub and completes the dialog with its effect.
    /// </summary>
    public static DialogGraph Build(InterviewLines lines, IReadOnlyList<InterviewQuestion> questions,
                                    IReadOnlyList<AuthoredDialog> dialogs, InterviewCase c)
    {
        lines = lines ?? new InterviewLines();
        var graph = new DialogGraph(HubNodeId);
        var hub = new DialogNode { Id = HubNodeId };
        var ask = new DialogNode { Id = AskNodeId };

        IReadOnlyList<CaseDocument> documents = c != null && c.documents != null ? c.documents : new CaseDocument[0];
        for (int i = 0; i < documents.Count; i++)
        {
            CaseDocument doc = documents[i];
            if (doc == null || !doc.Requested)
                continue;

            hub.Choices.Add(new DialogChoice
            {
                Id = $"request:{i}",
                Label = Interview.Fill(lines.requestLabel, Interview.DocumentToken, doc.name),
                Lines =
                {
                    new DialogLine(Id(lines.requestPrompt), DialogSpeaker.Desk, Interview.Fill(Text(lines.requestPrompt), Interview.DocumentToken, doc.name)),
                    new DialogLine(Id(lines.requestReply), DialogSpeaker.Traveller, Text(lines.requestReply))
                },
                Action = DialogAction.HandOverDocument,
                DocumentIndex = i,
                OneShot = true
            });
        }

        ask.Choices.Add(new DialogChoice { Id = "back", Label = lines.backLabel, Next = HubNodeId, Kind = DialogChoiceKind.Back });

        string eraId = c != null ? c.claimedEraId : null;
        if (questions != null)
        {
            foreach (InterviewQuestion q in questions)
            {
                InterviewAnswer a = q != null ? AnswerFor(c, q.category) : null;
                if (a == null)
                    continue;

                ask.Choices.Add(new DialogChoice
                {
                    Id = $"q:{q.id}",
                    Label = q.label,
                    Lines = { PromptLine(q, eraId), AnswerLine(q, eraId, a) },
                    OneShot = true
                });
            }
        }

        if (c != null && c.smallTalk != null)
        {
            ask.Choices.Add(new DialogChoice
            {
                Id = "smalltalk",
                Label = lines.smallTalkLabel,
                Lines =
                {
                    new DialogLine(Id(lines.smallTalkPrompt), DialogSpeaker.Desk, Text(lines.smallTalkPrompt)),
                    new DialogLine(c.smallTalk.id, DialogSpeaker.Traveller, c.smallTalk.text)
                },
                OneShot = true
            });
        }

        if (ask.Choices.Count > 1)
            hub.Choices.Add(new DialogChoice { Id = "ask", Label = lines.askLabel, Next = AskNodeId });

        var look = new DialogNode { Id = LookNodeId };
        look.Choices.Add(new DialogChoice { Id = "back", Label = lines.backLabel, Next = HubNodeId, Kind = DialogChoiceKind.Back });
        IReadOnlyList<Garment> garments = c != null && c.garments != null ? c.garments : new Garment[0];
        for (int i = 0; i < garments.Count; i++)
            if (garments[i] != null)
                look.Choices.Add(new DialogChoice { Id = $"look:{i}", Label = garments[i].Label, Action = DialogAction.InspectGarment, GarmentIndex = i });

        if (look.Choices.Count > 1)
            hub.Choices.Add(new DialogChoice { Id = "look", Label = lines.lookLabel, Next = LookNodeId });

        if (dialogs != null)
        {
            foreach (AuthoredDialog d in dialogs)
            {
                if (d == null || d.nodes == null || d.nodes.Count == 0 || d.nodes[0] == null)
                    continue;

                hub.Choices.Add(new DialogChoice { Id = $"dlg:{d.id}", Label = d.label, Next = NodeId(d, d.nodes[0].id), OneShot = true });
                foreach (ScriptNode node in d.nodes)
                    if (node != null)
                        graph.Add(BuildNode(d, node));
            }
        }

        graph.Add(hub);
        graph.Add(ask);
        graph.Add(look);
        return graph;
    }

    /// <summary>An authored node as a runtime node: namespaced ids, the desk speaking each choice's label.</summary>
    private static DialogNode BuildNode(AuthoredDialog d, ScriptNode node)
    {
        var built = new DialogNode { Id = NodeId(d, node.id) };
        if (node.lines != null)
            foreach (ScriptLine line in node.lines)
                if (line != null)
                    built.Lines.Add(new DialogLine(line.id, line.speaker, line.text, line.expression));

        if (node.choices == null)
            return built;

        foreach (ScriptChoice choice in node.choices)
        {
            if (choice == null)
                continue;

            string id = ChoiceLineId(d.id, choice.id);
            var runtime = new DialogChoice { Id = id, Label = choice.label };
            runtime.Lines.Add(new DialogLine(id, DialogSpeaker.Desk, choice.label));
            if (choice.lines != null)
                foreach (ScriptLine line in choice.lines)
                    if (line != null)
                        runtime.Lines.Add(new DialogLine(line.id, line.speaker, line.text, line.expression));

            if (!string.IsNullOrEmpty(choice.next))
            {
                runtime.Next = NodeId(d, choice.next);
            }
            else
            {
                runtime.Next = HubNodeId;
                runtime.Action = DialogAction.CompleteDialog;
                runtime.DialogId = d.id;
                runtime.EffectName = choice.effect;
            }

            built.Choices.Add(runtime);
        }

        return built;
    }

    /// <summary>A dialog node's graph id.</summary>
    private static string NodeId(AuthoredDialog d, string nodeId) => $"{d.id}/{nodeId}";

    /// <summary>The traveller's answer about a category, or null.</summary>
    private static InterviewAnswer AnswerFor(InterviewCase c, ClueCategory category)
    {
        if (c == null || c.answers == null)
            return null;

        foreach (InterviewAnswer a in c.answers)
            if (a != null && a.category == category)
                return a;

        return null;
    }

    /// <summary>A line's id, or null for a null line.</summary>
    private static string Id(LineText line) => line != null ? line.id : null;

    /// <summary>A line's wording, or null for a null line.</summary>
    private static string Text(LineText line) => line != null ? line.text : null;
}

/// <summary>
/// Structure and capacity rules for dialogs and menus, pure so the generator
/// (before writing), the validator (on assets) and the day-start interview
/// (InterviewDay) share one set of rules.
/// </summary>
public static class DialogChecks
{
    /// <summary>
    /// Every structural problem of a dialog, each naming its node or choice:
    /// no nodes; duplicate node or choice ids; a next naming no node; a node
    /// unreachable from the start; a node without choices; no ending choice;
    /// a reachable node from which no ending can be reached; an effect on a
    /// choice that does not end the dialog, or on a dialog that is not
    /// one-shot; a node offering more than <paramref name="maxChoices"/>
    /// choices (skipped when it is 0 or less). Empty for a sound dialog.
    /// </summary>
    public static List<string> Problems(AuthoredDialog d, int maxChoices)
    {
        var problems = new List<string>();
        if (d == null || d.nodes == null || d.nodes.Count == 0 || d.nodes[0] == null)
        {
            problems.Add("the dialog has no nodes");
            return problems;
        }

        var nodes = new Dictionary<string, ScriptNode>();
        var choiceIds = new HashSet<string>();
        bool anyEnding = false;

        foreach (ScriptNode node in d.nodes)
        {
            if (node == null)
            {
                problems.Add("a node is empty");
                continue;
            }

            if (nodes.ContainsKey(node.id ?? string.Empty))
                problems.Add($"node '{node.id}' is listed twice");
            else
                nodes.Add(node.id ?? string.Empty, node);

            foreach (ScriptChoice choice in Choices(node))
            {
                if (!choiceIds.Add(choice.id ?? string.Empty))
                    problems.Add($"choice '{choice.id}' is listed twice");
                if (string.IsNullOrEmpty(choice.next))
                    anyEnding = true;
                else if (!string.IsNullOrEmpty(choice.effect))
                    problems.Add($"choice '{choice.id}' has an effect but does not end the dialog");
                if (!string.IsNullOrEmpty(choice.effect) && !d.oneShot)
                    problems.Add($"choice '{choice.id}' has an effect, but the dialog is repeatable (a dialog with a consequence must be one-shot)");
            }

            if (Choices(node).Count == 0)
                problems.Add($"node '{node.id}' has no choices");
            else if (maxChoices > 0 && Choices(node).Count > maxChoices)
                problems.Add($"node '{node.id}' offers {Choices(node).Count} choices; the traveller wheel shows at most {maxChoices}");
        }

        foreach (ScriptNode node in nodes.Values)
            foreach (ScriptChoice choice in Choices(node))
                if (!string.IsNullOrEmpty(choice.next) && !nodes.ContainsKey(choice.next))
                    problems.Add($"choice '{choice.id}' leads to unknown node '{choice.next}'");

        if (!anyEnding)
            problems.Add("no choice ends the dialog");

        // Forwards: what the player can reach from the start.
        string start = d.nodes[0].id ?? string.Empty;
        var reachable = new HashSet<string> { start };
        var queue = new Queue<string>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            foreach (ScriptChoice choice in Choices(nodes[queue.Dequeue()]))
                if (!string.IsNullOrEmpty(choice.next) && nodes.ContainsKey(choice.next) && reachable.Add(choice.next))
                    queue.Enqueue(choice.next);
        }

        // Backwards: every node from which some path ends the dialog.
        var ending = new HashSet<string>();
        foreach (ScriptNode node in nodes.Values)
            foreach (ScriptChoice choice in Choices(node))
                if (string.IsNullOrEmpty(choice.next))
                    ending.Add(node.id ?? string.Empty);

        bool grew = true;
        while (grew)
        {
            grew = false;
            foreach (ScriptNode node in nodes.Values)
            {
                string id = node.id ?? string.Empty;
                if (ending.Contains(id))
                    continue;

                foreach (ScriptChoice choice in Choices(node))
                {
                    if (!string.IsNullOrEmpty(choice.next) && ending.Contains(choice.next))
                    {
                        ending.Add(id);
                        grew = true;
                        break;
                    }
                }
            }
        }

        foreach (string id in nodes.Keys)
        {
            if (!reachable.Contains(id))
                problems.Add($"node '{id}' cannot be reached from '{start}'");
            else if (anyEnding && !ending.Contains(id))
                problems.Add($"node '{id}' cannot reach an ending");
        }

        return problems;
    }

    /// <summary>
    /// Menus larger than the traveller wheel shows: the ask menu (1 back + the
    /// questions + 1 when there is small talk), the look menu (1 back + one
    /// garment per LookSlot) or the hub (the most documents one traveller hands
    /// over on request + the ask entry + the look entry + every dialog bound to
    /// no premade, counted as offered at once, + 1 when any dialog is bound to
    /// a premade: at most one premade stands at the desk). Skipped when
    /// <paramref name="maxChoices"/> is 0 or less.
    /// </summary>
    public static List<string> MenuProblems(int questions, bool smallTalk, int maxRequestedDocuments, int dialogs, int premadeDialogs, int maxChoices)
    {
        var problems = new List<string>();
        if (maxChoices <= 0)
            return problems;

        int ask = 1 + questions + (smallTalk ? 1 : 0);
        if (ask > maxChoices)
            problems.Add($"The ask menu holds {ask} choices (< Back, {questions} question(s){(smallTalk ? ", small talk" : string.Empty)}); the traveller wheel shows at most {maxChoices}.");

        int look = 1 + Looks.Slots.Count;
        if (look > maxChoices)
            problems.Add($"The look menu holds up to {look} choices (< Back, one per garment slot); the traveller wheel shows at most {maxChoices}.");

        int hub = maxRequestedDocuments + 2 + dialogs + (premadeDialogs > 0 ? 1 : 0);
        if (hub > maxChoices)
            problems.Add($"The hub holds {hub} choices ({maxRequestedDocuments} document request(s), the ask and look entries, {dialogs} dialog(s){(premadeDialogs > 0 ? ", one premade's dialog" : string.Empty)}); the traveller wheel shows at most {maxChoices}.");

        return problems;
    }

    /// <summary>A node's non-null choices (empty for none).</summary>
    private static List<ScriptChoice> Choices(ScriptNode node)
    {
        var choices = new List<ScriptChoice>();
        if (node != null && node.choices != null)
            foreach (ScriptChoice c in node.choices)
                if (c != null)
                    choices.Add(c);
        return choices;
    }
}
