using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The wording every confront test shares: the ten base prompts (one per
/// proof and statement kind), a category row for the account, and the
/// default replies (a base per outcome, a panic Explain, a forged Crack).
/// </summary>
public static class ConfrontFixture
{
    /// <summary>A sound interview.confront.</summary>
    public static ConfrontWording Wording()
    {
        var w = new ConfrontWording { label = "Ask about a difference >", entryLabel = "{category}: {value}?" };
        int n = 0;
        foreach ((DiscrepancyProof proof, EvidenceKind source) in Confrontations.Kinds)
        {
            n++;
            string first = source == EvidenceKind.DocumentField ? "Your {document} says {value}." : source == EvidenceKind.Answer ? "You told me {value}." : "You're wearing {value}.";
            string then = proof == DiscrepancyProof.CrossMismatch ? "Your {otherDocument} says {other}."
                        : proof == DiscrepancyProof.ForeignOrigin ? "That belongs to {other}."
                        : proof == DiscrepancyProof.RecordMismatch ? "Your record says {other}."
                        : "The book for {place} says {other}.";
            w.prompts.Add(new ConfrontPrompt { proof = proof, source = source, line = new LineText($"interview.confront.prompts.{n}", first), then = new LineText($"interview.confront.prompts.{n}.then", then) });
        }
        w.prompts.Add(new ConfrontPrompt
        {
            proof = DiscrepancyProof.RecordMismatch, source = EvidenceKind.DocumentField, category = "AccountStatus",
            line = new LineText("interview.confront.prompts.account", "Your {document} says {value}."), then = new LineText("interview.confront.prompts.account.then", "Your account says {other}.")
        });
        w.replies.Add(Reply("Oh! A clerical slip.", ConfrontOutcome.Explain));
        w.replies.Add(Reply("The costume desk gave me {value}.", ConfrontOutcome.Explain, reason: "panic"));
        w.replies.Add(Reply("All right. It's {other}.", ConfrontOutcome.Crack));
        w.replies.Add(Reply("Fine. I changed it.", ConfrontOutcome.Crack, reason: "forged"));
        w.replies.Add(Reply("It's {value}. I insist.", ConfrontOutcome.DoubleDown));
        return w;
    }

    /// <summary>A reply row (a default: no voice) of <paramref name="outcome"/>.</summary>
    public static VoiceLine Reply(string text, ConfrontOutcome outcome, string personality = null, string premade = null, string reason = null, string lie = null) => new VoiceLine
    {
        personality = personality ?? string.Empty,
        premade = premade ?? string.Empty,
        outcome = outcome,
        reason = reason ?? string.Empty,
        lie = lie ?? string.Empty,
        line = new LineText("reply", text)
    };
}

/// <summary>
/// Wave 5, Papers, Please lesson 3: a logged difference becomes a question
/// on the traveller wheel about exactly it, the same verb for everyone; the
/// honest explain, a liar cracks or doubles down in their personality's voice.
/// </summary>
public class ConfrontationsTests
{
    private static Discrepancy Record(string stated = "Premium", string recorded = "Economy", int paper = 0) => new Discrepancy
    {
        category = ClueCategory.AccountStatus, documentValue = stated, expectedValue = recorded,
        provedBy = DiscrepancyProof.RecordMismatch, source = EvidenceKind.DocumentField, statementDocument = paper
    };

    private static InterviewLines Lines()
    {
        var lines = new InterviewLines { backLabel = "< Back", menuCapacity = 9, confront = ConfrontFixture.Wording() };
        lines.voices.confront.Add(ConfrontFixture.Reply("Fine. {other}. Happy?", ConfrontOutcome.Crack, personality: "curt"));
        lines.voices.confront.Add(ConfrontFixture.Reply("It says {value}. Read it again.", ConfrontOutcome.DoubleDown, personality: "curt"));
        lines.voices.confront.Add(ConfrontFixture.Reply("Clerical error. Not mine.", ConfrontOutcome.Explain, personality: "curt"));
        lines.voices.confront.Add(ConfrontFixture.Reply("{value}, darling.", ConfrontOutcome.DoubleDown, premade: "ada"));
        return lines;
    }

    private static InterviewCase Case(string personality = "curt", string premade = null) => new InterviewCase
    {
        kind = TravellerKind.RichTourist,
        claimPlace = "Periclean Athens (Ancient)",
        claimedEraId = "ancient",
        documents = new[] { new CaseDocument { name = "Leisure Departure Visa" }, new CaseDocument { name = "Departure Manifest" } },
        voice = new Voice(personality, premade, 77)
    };

    // ---- the outcome ----

    [Test]
    public void Outcome_TheHonestExplain_APremadeDoublesDown_OnceCrackedAlwaysCracked()
    {
        Assert.AreEqual(ConfrontOutcome.Explain, Confrontations.Outcome(ReactionIntent.Honest, false, 1f, 5, ClueCategory.Culture, true), "a costume error does not know: it explains");
        Assert.AreEqual(ConfrontOutcome.DoubleDown, Confrontations.Outcome(ReactionIntent.Lying, true, 1f, 5, ClueCategory.Currency, true), "a story beat never confesses (days 7-15 B7)");
        Assert.AreEqual(ConfrontOutcome.Crack, Confrontations.Outcome(ReactionIntent.Lying, false, 0f, 5, ClueCategory.Currency, true), "a liar who cracked once cracks again");
        Assert.AreEqual(ConfrontOutcome.Crack, Confrontations.Outcome(ReactionIntent.Lying, false, 1f, 5, ClueCategory.Currency, false));
        Assert.AreEqual(ConfrontOutcome.DoubleDown, Confrontations.Outcome(ReactionIntent.Lying, false, 0f, 5, ClueCategory.Currency, false));
    }

    [Test]
    public void Outcome_IsAValueOfTheDialogSeedAndTheCategory_NeverADraw()
    {
        int cracks = 0;
        for (int seed = 0; seed < 400; seed++)
        {
            ConfrontOutcome a = Confrontations.Outcome(ReactionIntent.Lying, false, 0.5f, seed, ClueCategory.Currency, false);
            Assert.AreEqual(a, Confrontations.Outcome(ReactionIntent.Lying, false, 0.5f, seed, ClueCategory.Currency, false), "the same traveller answers the same");
            cracks += a == ConfrontOutcome.Crack ? 1 : 0;
        }
        Assert.That(cracks, Is.InRange(150, 250), "a confess chance of 0.5 cracks about half the liars");
        Assert.IsTrue(Enumerable.Range(0, 50).Any(s => Confrontations.Outcome(ReactionIntent.Lying, false, 0.5f, s, ClueCategory.Currency, false)
                                                    != Confrontations.Outcome(ReactionIntent.Lying, false, 0.5f, s, ClueCategory.Technology, false)), "each difference its own value");
    }

    // ---- the prompt ----

    [Test]
    public void Prompt_ByProofAndStatement_ACategoryRowFirst_ACrossProofIsPapers()
    {
        ConfrontWording w = ConfrontFixture.Wording();
        Assert.AreEqual("Your account says {other}.", Confrontations.Prompt(w.prompts, Record()).then.text, "the category's row before the base one");
        Discrepancy wage = Record();
        wage.category = ClueCategory.Wage;
        Assert.AreEqual("Your record says {other}.", Confrontations.Prompt(w.prompts, wage).then.text, "another category: the base row");
        var said = new Discrepancy { category = ClueCategory.Currency, provedBy = DiscrepancyProof.ForeignOrigin, source = EvidenceKind.Answer };
        Assert.AreEqual("You told me {value}.", Confrontations.Prompt(w.prompts, said).line.text);
        var cross = new Discrepancy { category = ClueCategory.TransponderClass, provedBy = DiscrepancyProof.CrossMismatch, source = EvidenceKind.Answer };
        Assert.AreEqual("Your {otherDocument} says {other}.", Confrontations.Prompt(w.prompts, cross).then.text, "a cross proof is two papers whatever its source says");
        Assert.IsNull(Confrontations.Prompt(new List<ConfrontPrompt>(), said));
        Assert.IsNull(Confrontations.Prompt(w.prompts, null));
    }

    [Test]
    public void Problems_ASoundWording_None_AndEachRuleNamed()
    {
        CollectionAssert.IsEmpty(Confrontations.Problems(ConfrontFixture.Wording()));
        StringAssert.Contains("interview.confront is missing", Confrontations.Problems(null).Single());

        ConfrontWording w = ConfrontFixture.Wording();
        w.label = " ";
        w.entryLabel = "{place}?";
        w.prompts.RemoveAll(p => p.proof == DiscrepancyProof.CrossMismatch);
        w.prompts.Add(new ConfrontPrompt { proof = DiscrepancyProof.ClaimMismatch, source = EvidenceKind.Answer, category = "Nonsense", line = new LineText("x", "You said {value} to {document}. {weird}") });
        w.prompts.Add(new ConfrontPrompt { proof = DiscrepancyProof.CrossMismatch, source = EvidenceKind.Answer, line = new LineText("y", "{value} {other}") });
        w.prompts.Add(new ConfrontPrompt { proof = DiscrepancyProof.RecordMismatch, source = EvidenceKind.DocumentField, line = new LineText("z", "{value} and {otherDocument}"), then = new LineText("z2", "{other}") });
        string problems = string.Join("\n", Confrontations.Problems(w));
        StringAssert.Contains("interview.confront.label is blank", problems);
        StringAssert.Contains("interview.confront.entryLabel holds {place}", problems);
        StringAssert.Contains("has no base row for CrossMismatch · DocumentField", problems);
        StringAssert.Contains("names the category 'Nonsense'", problems);
        StringAssert.Contains("must hold {other}", problems);
        StringAssert.Contains("holds the unknown token {weird}", problems);
        StringAssert.Contains("holds {document}, but a statement that was said is on no paper", problems);
        StringAssert.Contains("is for CrossMismatch · Answer, which no logged difference is", problems);
        StringAssert.Contains("holds {otherDocument}, which only a cross proof", problems);
    }

    // ---- the question on the wheel ----

    [Test]
    public void Confront_TheDeskNamesExactlyTheDifference_TheTravellerAnswersInTheirVoice()
    {
        DialogChoice q = InterviewScript.Confront(Lines(), Case(), Record(), "VISA CLASS", ConfrontOutcome.Crack, Faults.Forged, LieKind.PoorPosingAsRich);
        Assert.AreEqual("confront:AccountStatus", q.Id);
        Assert.AreEqual("VISA CLASS: Premium?", q.Label);
        Assert.AreEqual(DialogChoiceKind.Question, q.Kind);
        Assert.IsTrue(q.OneShot);
        CollectionAssert.AreEqual(new[] { "Your Leisure Departure Visa says Premium.", "Your account says Economy.", "Fine. Economy. Happy?" }, q.Lines.Select(l => l.Text).ToArray());
        CollectionAssert.AreEqual(new[] { DialogSpeaker.Desk, DialogSpeaker.Desk, DialogSpeaker.Traveller }, q.Lines.Select(l => l.Speaker).ToArray());
        Assert.IsFalse(q.Lines.Any(l => l.IsAnswer), "an answer to a question about a difference is never evidence");

        Assert.AreEqual("It says Premium. Read it again.", InterviewScript.Confront(Lines(), Case(), Record(), "VISA CLASS", ConfrontOutcome.DoubleDown, Faults.Forged, null).Lines[2].Text);
        Assert.AreEqual("Premium, darling.", InterviewScript.Confront(Lines(), Case(null, "ada"), Record(), "VISA CLASS", ConfrontOutcome.DoubleDown, Faults.Forged, null).Lines[2].Text, "a premade speaks its own row");
        Assert.AreEqual("Fine. I changed it.", InterviewScript.Confront(Lines(), Case("glum"), Record(), "VISA CLASS", ConfrontOutcome.Crack, Faults.Forged, null).Lines[2].Text,
                        "no row of their own: the default of their fault reason before the base one");
        Assert.AreEqual("The costume desk gave me top hat.", InterviewScript.Confront(Lines(), Case("glum"), new Discrepancy
        {
            category = ClueCategory.Culture, documentValue = "top hat", expectedValue = "chiton", provedBy = DiscrepancyProof.ClaimMismatch, source = EvidenceKind.Appearance
        }, "DRESS", ConfrontOutcome.Explain, CostumeErrors.FaultReason, null).Lines[2].Text, "an honest costume error explains the slip");
    }

    [Test]
    public void Confront_ACrossProofNamesBothPapers_NoPromptNoQuestion()
    {
        var cross = new Discrepancy
        {
            category = ClueCategory.TransponderClass, documentValue = "Premium", expectedValue = "Economy",
            provedBy = DiscrepancyProof.CrossMismatch, source = EvidenceKind.DocumentField, statementDocument = 0, otherDocument = 1
        };
        DialogChoice q = InterviewScript.Confront(Lines(), Case(), cross, "TRANSPONDER CLASS", ConfrontOutcome.DoubleDown, Faults.Forged, null);
        CollectionAssert.AreEqual(new[] { "Your Leisure Departure Visa says Premium.", "Your Departure Manifest says Economy." }, q.Lines.Take(2).Select(l => l.Text).ToArray());

        InterviewLines bare = Lines();
        bare.confront.prompts.Clear();
        Assert.IsNull(InterviewScript.Confront(bare, Case(), cross, "X", ConfrontOutcome.Crack, Faults.Forged, null));
    }

    [Test]
    public void AddConfront_TheMenuJoinsTheHubOnTheFirst_ShowsWhileAQuestionIsLeft()
    {
        InterviewLines lines = Lines();
        var hub = new DialogNode { Id = InterviewScript.HubNodeId };
        hub.Choices.Add(new DialogChoice { Id = "ask", Label = "Ask >", Kind = DialogChoiceKind.Question });
        var graph = new DialogGraph(InterviewScript.HubNodeId);
        graph.Add(hub);
        var runner = new DialogRunner(graph, null);
        CollectionAssert.AreEqual(new[] { "ask" }, runner.Choices.Select(c => c.Id).ToArray(), "no difference logged: no entry");

        DialogChoice first = InterviewScript.Confront(lines, Case(), Record(), "VISA CLASS", ConfrontOutcome.Crack, Faults.Forged, null);
        Assert.IsTrue(InterviewScript.AddConfront(graph, lines, first, 9));
        Assert.IsFalse(InterviewScript.AddConfront(graph, lines, first, 9), "one question per difference");
        CollectionAssert.AreEqual(new[] { "ask", InterviewScript.DifferencesNodeId }, runner.Choices.Select(c => c.Id).ToArray());
        Assert.AreEqual("Ask about a difference >", runner.Choices[1].Label);

        runner.Choose(InterviewScript.DifferencesNodeId);
        CollectionAssert.AreEqual(new[] { "back", "confront:AccountStatus" }, runner.Choices.Select(c => c.Id).ToArray());
        runner.Choose("confront:AccountStatus");
        CollectionAssert.AreEqual(new[] { "Your Leisure Departure Visa says Premium.", "Your account says Economy.", "Fine. Economy. Happy?" }, runner.Transcript.Select(l => l.Text).ToArray());
        runner.Choose("back");
        CollectionAssert.AreEqual(new[] { "ask" }, runner.Choices.Select(c => c.Id).ToArray(), "every difference asked: the entry is gone");

        var culture = new Discrepancy { category = ClueCategory.Culture, documentValue = "top hat", expectedValue = "chiton", provedBy = DiscrepancyProof.ClaimMismatch, source = EvidenceKind.Appearance };
        Assert.IsTrue(InterviewScript.AddConfront(graph, lines, InterviewScript.Confront(lines, Case(), culture, "DRESS", ConfrontOutcome.Crack, Faults.Disguised, null), 9));
        CollectionAssert.AreEqual(new[] { "ask", InterviewScript.DifferencesNodeId }, runner.Choices.Select(c => c.Id).ToArray(), "a new difference: the entry is back");

        var tech = new Discrepancy { category = ClueCategory.Technology, documentValue = "a", expectedValue = "b", provedBy = DiscrepancyProof.ClaimMismatch, source = EvidenceKind.Answer };
        Assert.IsFalse(InterviewScript.AddConfront(graph, lines, InterviewScript.Confront(lines, Case(), tech, "DEVICE", ConfrontOutcome.Crack, Faults.Disguised, null), 3),
                       "a full menu (Back and two questions at a capacity of 3) takes no more");
        Assert.IsFalse(InterviewScript.AddConfront(null, lines, first, 9));
        Assert.IsFalse(InterviewScript.AddConfront(graph, lines, null, 9));
    }

    // ---- the voice ----

    [Test]
    public void VoicesConfront_ByOutcome_AReasonOrLieScoresFour_ElseTheDefaults()
    {
        InterviewLines lines = Lines();
        lines.voices.confront.Add(ConfrontFixture.Reply("Curt, forged and cracking.", ConfrontOutcome.Crack, personality: "curt", reason: Faults.Forged));
        lines.voices.confront.Add(ConfrontFixture.Reply("Curt, a smuggler cracking.", ConfrontOutcome.Crack, personality: "curt", lie: nameof(LieKind.Smuggling)));
        var context = new VoiceContext(TravellerKind.RichTourist, "ancient");
        var curt = new Voice("curt", null, 3);
        Assert.AreEqual("Curt, forged and cracking.", Voices.Confront(lines, curt, context, ConfrontOutcome.Crack, Faults.Forged, LieKind.PoorPosingAsRich, ClueCategory.AccountStatus).text);
        Assert.AreEqual("Curt, a smuggler cracking.", Voices.Confront(lines, curt, context, ConfrontOutcome.Crack, Faults.Smuggled, LieKind.Smuggling, ClueCategory.Currency).text);
        Assert.AreEqual("Fine. {other}. Happy?", Voices.Confront(lines, curt, context, ConfrontOutcome.Crack, Faults.Disguised, LieKind.FalseOrigin, ClueCategory.Currency).text, "another reason: the base row");
        Assert.AreEqual("Oh! A clerical slip.", Voices.Confront(lines, Voice.None, context, ConfrontOutcome.Explain, string.Empty, null, ClueCategory.Culture).text, "no voice: the defaults");
        Assert.IsNull(Voices.Confront(new InterviewLines(), curt, context, ConfrontOutcome.Crack, Faults.Forged, null, ClueCategory.Currency));
    }

    [Test]
    public void About_AFindingTheWorkbenchLogged_RaisesItsProvedDeviation_OnlyForADifference()
    {
        Discrepancy proof = Record();
        Finding Logged(FindingKind kind, Discrepancy deviation) => new Finding(kind, "a", "b", "Visa", "Premium", "Citizen record", "Economy", "Visa class", deviation);

        Assert.AreSame(proof, Confrontations.About(Logged(FindingKind.Differs, proof)), "a proof logged as evidence raises its question");
        Assert.AreSame(proof, Confrontations.About(Logged(FindingKind.Elsewhere, proof)), "a foreign-origin proof too");
        Assert.IsTrue(Logged(FindingKind.Differs, proof).Proof, "the finding reads as evidence");
        Assert.IsNull(Confrontations.About(Logged(FindingKind.Differs, null)), "a difference that proves nothing (an answer against a paper): no deviation, no question");
        Assert.IsNull(Confrontations.About(Logged(FindingKind.RuleBroken, null)), "a broken rule is the rule's to decide");
        Assert.IsNull(Confrontations.About(Logged(FindingKind.Expired, null)), "a date the calendar fails");
        Assert.IsNull(Confrontations.About(Logged(FindingKind.Match, null)), "a match");
        Assert.IsNull(Confrontations.About(null));
    }
}
