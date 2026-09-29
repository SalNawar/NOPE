using System.Collections.Generic;

/// <summary>One traveller's interview input, from their case.</summary>
public sealed class InterviewCase
{
    /// <summary>The desk's opener (CaseInstance.introLine); skipped when blank.</summary>
    public string introLine;

    /// <summary>The traveller's kind (CaseInstance.kind): the claim is said in its words.</summary>
    public TravellerKind kind;

    /// <summary>The claimed place's label, the claim's {place} (CaseInstance.originLabel; the claim is only spoken, never printed: the personalities spec's B3).</summary>
    public string claimPlace;

    /// <summary>What of the traveller's lines stays English when they show untranslated (translation.keyWords); null keeps nothing.</summary>
    public KeyWordRule keyWords;

    /// <summary>The claimed era's id: with the kind, picks each question's answer override.</summary>
    public string claimedEraId;

    /// <summary>The traveller's documents in paper order; only those handed over on request get a hub request.</summary>
    public IReadOnlyList<CaseDocument> documents;

    /// <summary>The day's papers menu, the same for every traveller (InterviewDay.AskableForms: every on-request form of the days so far, the personalities spec's W4); null: only the carried papers can be asked for.</summary>
    public IReadOnlyList<AskableForm> askable;

    /// <summary>Why this traveller lacks a form they are asked for (the reply they give): Honest unless a paper-set fault left it out.</summary>
    public MissingFormVariant missingVariant;

    /// <summary>The traveller's answers to today's askable questions (CaseInstance.answers).</summary>
    public IReadOnlyList<InterviewAnswer> answers;

    /// <summary>The traveller's small-talk line, resolved at generation (Voices.SmallTalk; {place} filled here); null means no small talk.</summary>
    public LineText smallTalk;

    /// <summary>Who speaks (CaseInstance's personality or premade and dialog seed): every reply is resolved in their voice (Voices); null says the defaults.</summary>
    public Voice voice;

    /// <summary>The traveller's visible garments (TravellerLook.Garments); none means no look menu.</summary>
    public IReadOnlyList<Garment> garments;
}

/// <summary>
/// Builds a traveller's interview graph: the hub (a document request, or
/// "Request papers >" for two or more, the spoken requests, "Ask about the
/// trip >" (InterviewLines.askLabel, everyone's), "Look >",
/// today's narrative dialogs), the papers menu ("&lt; Back"
/// first, then one request per form or request group of the day's menu,
/// the same for everyone, FormRequests.Build: a carried paper is handed
/// over, a missing one answered with the kind's line), the ask menu ("&lt; Back" first,
/// then the kind's questions and small talk), the look menu ("&lt; Back" first, then
/// one choice per visible garment) and every authored dialog's nodes. Every traveller line carries its key-word spans
/// (KeyWords.Spans over its template and fills, InterviewCase.keyWords): the
/// parts that stay English when it shows untranslated. Pure, so every menu
/// and line is tested headless.
/// </summary>
public static class InterviewScript
{
    /// <summary>The hub node's id.</summary>
    public const string HubNodeId = "hub";

    /// <summary>The ask menu's id.</summary>
    public const string AskNodeId = "ask";

    /// <summary>The papers menu's id (traveller types I2): the documents a traveller hands over on request, when there are two or more.</summary>
    public const string PapersNodeId = "papers";

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

    /// <summary>
    /// The transcript's first lines: the desk's opener (<see cref="IntroLineId"/>,
    /// skipped when blank), then the traveller's claim (<see cref="ClaimLineId"/>:
    /// <see cref="Claim"/>, with its key-word spans taken over its template).
    /// </summary>
    public static IReadOnlyList<DialogLine> Opening(InterviewLines wording, InterviewCase c)
    {
        var lines = new List<DialogLine>();
        if (c == null)
            return lines;

        if (!string.IsNullOrWhiteSpace(c.introLine))
            lines.Add(new DialogLine(IntroLineId, DialogSpeaker.Desk, c.introLine));
        string template = ClaimTemplate(wording, c);
        var fills = new Dictionary<string, string> { { Interview.PlaceToken, c.claimPlace } };
        lines.Add(new DialogLine(ClaimLineId, DialogSpeaker.Traveller, Interview.Fill(template, Interview.PlaceToken, c.claimPlace), null,
                                 KeyWords.Spans(template, fills, c.keyWords)));
        return lines;
    }

    /// <summary>
    /// The traveller's claim template (the only claim: nothing prints it, the
    /// personalities spec's B3): their voice's (Voices.Claim), else their
    /// kind's (interview.claims), or "{place}" alone when that is missing or blank.
    /// </summary>
    public static string ClaimTemplate(InterviewLines wording, InterviewCase c)
    {
        LineText line = c != null ? Voices.Claim(wording, c.voice, Context(c)) : null;
        return string.IsNullOrWhiteSpace(line?.text) ? Interview.Placeholder(Interview.PlaceToken) : line.text;
    }

    /// <summary>The traveller's claim sentence (<see cref="ClaimTemplate"/> with the claimed place's label): what they say as they step up.</summary>
    public static string Claim(InterviewLines wording, InterviewCase c) => Interview.Fill(ClaimTemplate(wording, c), Interview.PlaceToken, c?.claimPlace);

    /// <summary>What a line may depend on before the stamp (VoiceContext, T2): the kind and the claimed era.</summary>
    private static VoiceContext Context(InterviewCase c) => new VoiceContext(c != null ? c.kind : default, c?.claimedEraId);

    /// <summary>The desk asking <paramref name="q"/>, in the question's own words for every traveller (the personalities spec's W3), the claimed place's label filling {place} ("What will you pay with in {place}?").</summary>
    public static DialogLine PromptLine(InterviewQuestion q, string placeLabel = null)
    {
        LineText prompt = q.prompt ?? new LineText();
        return new DialogLine(prompt.id, DialogSpeaker.Desk, Interview.Fill(prompt.text, Interview.PlaceToken, placeLabel));
    }

    /// <summary>
    /// The traveller's answer line: the template in their voice for their kind
    /// and claimed era (Voices.Answer: the voice's row, else the question's
    /// override, else its answer) with the canonical value and the claimed
    /// place, carrying the answer's fact (the sentence around a tell's value is
    /// the honest sentence, T3) and its key-word spans.
    /// </summary>
    public static DialogLine AnswerLine(InterviewLines lines, InterviewQuestion q, InterviewCase c, InterviewAnswer a)
    {
        LineText answer = Voices.Answer(lines, c?.voice, Context(c), q);
        string value = a != null ? a.value : null;
        var fills = new Dictionary<string, string> { { Interview.ValueToken, value }, { Interview.PlaceToken, c?.claimPlace } };
        string text = Interview.Fill(Interview.Fill(answer.text, Interview.ValueToken, value), Interview.PlaceToken, c?.claimPlace);
        return DialogLine.Answer(answer.id, text, a, KeyWords.Spans(answer.text, fills, c?.keyWords));
    }

    /// <summary>
    /// What the traveller said since transcript line <paramref name="from"/>:
    /// the Traveller lines at or after it, in order (each with its expression);
    /// empty when there are none or for no transcript. A from below 0 counts as
    /// 0. (What the traveller wheel's bubble says: the claim from line 0 on
    /// arrival, then each choice's reply.)
    /// </summary>
    public static IReadOnlyList<DialogLine> SaidSince(IReadOnlyList<DialogLine> transcript, int from)
    {
        var said = new List<DialogLine>();
        for (int i = from < 0 ? 0 : from; transcript != null && i < transcript.Count; i++)
        {
            DialogLine line = transcript[i];
            if (line != null && line.Speaker == DialogSpeaker.Traveller)
                said.Add(line);
        }

        return said;
    }

    /// <summary>
    /// The traveller's graph, the same entries for every traveller of the day
    /// (the personalities spec's W1): only the replies differ. Hub: the one
    /// request entry (FormRequests.Build over the day's papers menu and the
    /// documents: "request:{id}", the request's id whatever the traveller
    /// carries, one-shot; a carried paper is handed over, a request the
    /// traveller carries no form of gets the desk's prompt and their
    /// missing-form line, no hand-over), or,
    /// with two or more, "papers" (the papers menu: "back" first, then one
    /// entry per request, labelled with the form's name or the group's label,
    /// staying in the menu), then "act:{id}" per spoken
    /// request (one-shot, the desk's prompt and the traveller's reply, no
    /// action), then "ask" when the ask menu has a question or small talk, then
    /// "look" when the traveller has a visible garment, then "dlg:{id}" per
    /// dialog (one-shot). Ask: "back" first (so an overlong menu can never hide
    /// the way back), then "q:{id}" per question the traveller has an answer
    /// for (one-shot), then "smalltalk". Look: "back" first, then "look:{i}"
    /// per garment, labelled with the item's name (InspectGarment, never
    /// one-shot, no line). Authored nodes become "{dialogId}/{nodeId}" and their
    /// choices "{dialogId}.{choiceId}" (the desk speaks the label); every
    /// authored line keeps its expression; an ending choice returns to the hub
    /// and completes the dialog with its effect. Kinds: requests Request; "ask",
    /// the questions and small talk Question; "look" and the garments Look;
    /// "dlg:{id}" Dialog; "back" Back; authored replies Normal.
    /// </summary>
    public static DialogGraph Build(InterviewLines lines, IReadOnlyList<InterviewQuestion> questions,
                                    IReadOnlyList<AuthoredDialog> dialogs, InterviewCase c)
    {
        lines = lines ?? new InterviewLines();
        KeyWordRule keyWords = c != null ? c.keyWords : null;
        var graph = new DialogGraph(HubNodeId);
        var hub = new DialogNode { Id = HubNodeId };
        var ask = new DialogNode { Id = AskNodeId };
        var papers = new DialogNode { Id = PapersNodeId };
        papers.Choices.Add(new DialogChoice { Id = "back", Label = lines.backLabel, Next = HubNodeId, Kind = DialogChoiceKind.Back });

        IReadOnlyList<CaseDocument> documents = c != null && c.documents != null ? c.documents : new CaseDocument[0];
        List<FormRequest> requests = FormRequests.Build(c != null ? c.askable : null, documents, lines.askGroups);

        if (requests.Count == 1)
        {
            hub.Choices.Add(Request(lines, requests[0], Interview.Fill(lines.requestLabel, Interview.DocumentToken, requests[0].Label), c, keyWords));
        }
        else if (requests.Count > 1)
        {
            hub.Choices.Add(new DialogChoice { Id = "papers", Label = lines.papersLabel, Next = PapersNodeId, Kind = DialogChoiceKind.Request });
            foreach (FormRequest r in requests)
                papers.Choices.Add(Request(lines, r, r.Label, c, keyWords));
        }

        if (lines.requests != null)
        {
            foreach (InterviewRequest r in lines.requests)
            {
                if (r == null)
                    continue;

                hub.Choices.Add(new DialogChoice
                {
                    Id = $"act:{r.id}",
                    Label = r.label,
                    Lines =
                    {
                        new DialogLine(Id(r.prompt), DialogSpeaker.Desk, Text(r.prompt)),
                        Say(Voices.Spoken(lines, c?.voice, Context(c), r), c, null)
                    },
                    OneShot = true,
                    Kind = DialogChoiceKind.Request
                });
            }
        }

        ask.Choices.Add(new DialogChoice { Id = "back", Label = lines.backLabel, Next = HubNodeId, Kind = DialogChoiceKind.Back });

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
                    Lines = { PromptLine(q, c != null ? c.claimPlace : null), AnswerLine(lines, q, c, a) },
                    OneShot = true,
                    Kind = DialogChoiceKind.Question
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
                    Say(c.smallTalk, c, null)
                },
                OneShot = true,
                Kind = DialogChoiceKind.Question
            });
        }

        if (ask.Choices.Count > 1)
            hub.Choices.Add(new DialogChoice { Id = "ask", Label = lines.askLabel, Next = AskNodeId, Kind = DialogChoiceKind.Question });

        var look = new DialogNode { Id = LookNodeId };
        look.Choices.Add(new DialogChoice { Id = "back", Label = lines.backLabel, Next = HubNodeId, Kind = DialogChoiceKind.Back });
        IReadOnlyList<Garment> garments = c != null && c.garments != null ? c.garments : new Garment[0];
        for (int i = 0; i < garments.Count; i++)
            if (garments[i] != null)
                look.Choices.Add(new DialogChoice { Id = $"look:{i}", Label = garments[i].Label, Action = DialogAction.InspectGarment, GarmentIndex = i, Kind = DialogChoiceKind.Look });

        if (look.Choices.Count > 1)
            hub.Choices.Add(new DialogChoice { Id = "look", Label = lines.lookLabel, Next = LookNodeId, Kind = DialogChoiceKind.Look });

        if (dialogs != null)
        {
            foreach (AuthoredDialog d in dialogs)
            {
                if (d == null || d.nodes == null || d.nodes.Count == 0 || d.nodes[0] == null)
                    continue;

                hub.Choices.Add(new DialogChoice { Id = $"dlg:{d.id}", Label = d.label, Next = NodeId(d, d.nodes[0].id), OneShot = true, Kind = DialogChoiceKind.Dialog });
                foreach (ScriptNode node in d.nodes)
                    if (node != null)
                        graph.Add(BuildNode(d, node, keyWords));
            }
        }

        graph.Add(hub);
        graph.Add(papers);
        graph.Add(ask);
        graph.Add(look);
        return graph;
    }

    /// <summary>
    /// A request entry (one-shot, staying where it was chosen): the desk's
    /// prompt naming the request, then, for a carried paper, the traveller's
    /// reply in their voice (Voices.HandOver) and the hand-over, or, for a
    /// request the traveller carries no form of, their refusal in their voice
    /// for the case's variant (Voices.Missing: a voice row, else the kind's
    /// line; a Missing variant with no line for the request says the Honest
    /// one; only the prompt when none is authored) and no action. {document}
    /// is the request's label, {place} the claimed place. Its id is
    /// "request:{id}" either way, so the menu is the same for every traveller (W1).
    /// </summary>
    private static DialogChoice Request(InterviewLines lines, FormRequest request, string label, InterviewCase c, KeyWordRule keyWords)
    {
        var choice = new DialogChoice
        {
            Id = $"request:{request.Id}",
            Label = label,
            Lines = { new DialogLine(Id(lines.requestPrompt), DialogSpeaker.Desk, Interview.Fill(Text(lines.requestPrompt), Interview.DocumentToken, request.Label)) },
            OneShot = true,
            Kind = DialogChoiceKind.Request
        };

        if (request.Carried)
        {
            choice.Lines.Add(Say(Voices.HandOver(lines, c?.voice, Context(c), request.Id), c, request.Label));
            choice.Action = DialogAction.HandOverDocument;
            choice.DocumentIndex = request.Document;
        }
        else
        {
            LineText reply = Voices.Missing(lines, c?.voice, Context(c), request.Id, c != null ? c.missingVariant : MissingFormVariant.Honest);
            if (reply != null)
                choice.Lines.Add(Say(reply, c, request.Label));
        }

        return choice;
    }

    /// <summary>A traveller's reply in their voice: <paramref name="line"/>'s template with {place} (the claimed place) and {document} (<paramref name="document"/>) filled, carrying its key-word spans over the template and its fills.</summary>
    private static DialogLine Say(LineText line, InterviewCase c, string document)
    {
        string template = line != null ? line.text : null;
        string place = c != null ? c.claimPlace : null;
        var fills = new Dictionary<string, string> { { Interview.PlaceToken, place }, { Interview.DocumentToken, document } };
        string text = Interview.Fill(Interview.Fill(template, Interview.PlaceToken, place), Interview.DocumentToken, document);
        return new DialogLine(line != null ? line.id : null, DialogSpeaker.Traveller, text, null, KeyWords.Spans(template, fills, c != null ? c.keyWords : null));
    }

    /// <summary>An authored node as a runtime node: namespaced ids, the desk speaking each choice's label, the traveller's lines with their key-word spans.</summary>
    private static DialogNode BuildNode(AuthoredDialog d, ScriptNode node, KeyWordRule keyWords)
    {
        var built = new DialogNode { Id = NodeId(d, node.id) };
        if (node.lines != null)
            foreach (ScriptLine line in node.lines)
                if (line != null)
                    built.Lines.Add(Authored(line, keyWords));

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
                        runtime.Lines.Add(Authored(line, keyWords));

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

    /// <summary>A traveller's line with no fill, carrying its key-word spans.</summary>
    private static DialogLine Said(string id, string text, string expression, KeyWordRule keyWords) =>
        new DialogLine(id, DialogSpeaker.Traveller, text, expression, KeyWords.Spans(text, null, keyWords));

    /// <summary>An authored line: the traveller's with its key-word spans; the desk's as it is (always English).</summary>
    private static DialogLine Authored(ScriptLine line, KeyWordRule keyWords) =>
        line.speaker == DialogSpeaker.Traveller
            ? Said(line.id, line.text, line.expression, keyWords)
            : new DialogLine(line.id, line.speaker, line.text, line.expression);

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
        var choicesOf = new Dictionary<string, List<ScriptChoice>>(); // each listed node's choices, gathered once (audit R1-019)
        var choiceIds = new HashSet<string>();
        bool anyEnding = false;

        foreach (ScriptNode node in d.nodes)
        {
            if (node == null)
            {
                problems.Add("a node is empty");
                continue;
            }

            List<ScriptChoice> choices = Choices(node);
            if (nodes.ContainsKey(node.id ?? string.Empty))
                problems.Add($"node '{node.id}' is listed twice");
            else
            {
                nodes.Add(node.id ?? string.Empty, node);
                choicesOf.Add(node.id ?? string.Empty, choices);
            }

            foreach (ScriptChoice choice in choices)
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

            if (choices.Count == 0)
                problems.Add($"node '{node.id}' has no choices");
            else if (maxChoices > 0 && choices.Count > maxChoices)
                problems.Add($"node '{node.id}' offers {choices.Count} choices; the traveller wheel shows at most {maxChoices}");
        }

        foreach (List<ScriptChoice> choices in choicesOf.Values)
            foreach (ScriptChoice choice in choices)
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
            foreach (ScriptChoice choice in choicesOf[queue.Dequeue()])
                if (!string.IsNullOrEmpty(choice.next) && nodes.ContainsKey(choice.next) && reachable.Add(choice.next))
                    queue.Enqueue(choice.next);
        }

        // Backwards: every node from which some path ends the dialog.
        var ending = new HashSet<string>();
        foreach (KeyValuePair<string, List<ScriptChoice>> node in choicesOf)
            foreach (ScriptChoice choice in node.Value)
                if (string.IsNullOrEmpty(choice.next))
                    ending.Add(node.Key);

        bool grew = true;
        while (grew)
        {
            grew = false;
            foreach (string id in nodes.Keys)
            {
                if (ending.Contains(id))
                    continue;

                foreach (ScriptChoice choice in choicesOf[id])
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
    /// garment per LookSlot), the papers menu when one traveller can be asked
    /// for two or more requests (1 back + the most requests one kind may be
    /// asked for: a form outside a group or a request group each one,
    /// FormRequests.Count) or the hub (one entry for those requests, a
    /// direct request or the papers menu, + every spoken request + the ask
    /// entry + the look entry + every dialog bound to no premade, counted as
    /// offered at once, + 1 when any dialog is bound to a premade: at most one
    /// premade stands at the desk). Skipped when <paramref name="maxChoices"/>
    /// is 0 or less.
    /// </summary>
    public static List<string> MenuProblems(int questions, bool smallTalk, int maxRequests, int spokenRequests, int dialogs, int premadeDialogs, int maxChoices)
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

        int papers = 1 + maxRequests;
        if (maxRequests > 1 && papers > maxChoices)
            problems.Add($"The papers menu holds {papers} choices (< Back, {maxRequests} request(s)); the traveller wheel shows at most {maxChoices}.");

        string paperEntry = maxRequests > 1 ? "the papers menu" : maxRequests == 1 ? "1 document request" : "no document request";
        int hub = (maxRequests > 0 ? 1 : 0) + spokenRequests + 2 + dialogs + (premadeDialogs > 0 ? 1 : 0);
        if (hub > maxChoices)
            problems.Add($"The hub holds {hub} choices ({paperEntry}, {spokenRequests} spoken request(s), the ask and look entries, {dialogs} dialog(s){(premadeDialogs > 0 ? ", one premade's dialog" : string.Empty)}); the traveller wheel shows at most {maxChoices}.");

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
