using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// What travellers say and how the interview's wording is filled. The lie
/// fixture: the claim Egypt (Currency "Deben", capital "Thebes") and Iraq
/// (Currency "Silver shekel", capital "Babylon"); books for both categories.
/// </summary>
public class InterviewTests
{
    private const string Cover = "3 Jun 1450 BCE";

    private static readonly HomeCandidate Egypt = new HomeCandidate("egypt", "ancient", -1470, -1452);
    private static readonly HomeCandidate Iraq = new HomeCandidate("iraq", "ancient", 0, 0);
    private static readonly HashSet<ClueCategory> Books = new HashSet<ClueCategory> { ClueCategory.Currency, ClueCategory.Geography };

    private static FactTable Facts()
    {
        var t = new FactTable();
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Currency, "Deben");
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Geography, "Thebes");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Currency, "Silver shekel");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Geography, "Babylon");
        return t;
    }

    private static List<DocumentField> Papers() => new List<DocumentField>
    {
        new DocumentField { category = ClueCategory.Currency, label = "Coin of Issue", value = "Deben", page = 0 }
    };

    /// <summary>A lie plan over today = [Egypt, Iraq], asking about the capital.</summary>
    private static LiePlan Plan(float chance, TellChannel channel, params ScriptStep[] script) =>
        Lies.Plan(chance, 1, "egypt", "ancient", Cover, new[] { Egypt, Iraq }, Papers(),
                  new[] { ClueCategory.Geography }, new[] { channel }, Facts(), Books, new ScriptedRandom(script));

    private static InterviewLines Lines() => new InterviewLines
    {
        opener = new LineText("interview.opener", "Next! Step forward, {honorific}."),
        openerLegendary = new LineText("interview.openerLegendary", "Priority arrival: {name}."),
        claim = new LineText("interview.claim", "I request passage home to {place}."),
        honorificMale = "sir",
        honorificFemale = "madam",
        honorificUnknown = "traveller"
    };

    // -----------------------------
    // Answers
    // -----------------------------

    [Test]
    public void Answer_WithoutASpokenTell_IsTheCover()
    {
        LiePlan honest = Plan(0f, TellChannel.Answer, ScriptStep.Value(0.5f));
        LiePlan noLie = Lies.Plan(1f, 1, "egypt", "ancient", Cover, new[] { Egypt }, Papers(), new[] { ClueCategory.Geography },
                                  new[] { TellChannel.Answer }, Facts(), Books, new ScriptedRandom(ScriptStep.Value(0f)));
        Assert.AreEqual(LieOutcome.Honest, honest.Outcome);
        Assert.AreEqual(LieOutcome.NoPossibleLie, noLie.Outcome);

        foreach (LiePlan plan in new[] { null, honest, noLie })
        {
            InterviewAnswer a = Interview.Answer(ClueCategory.Geography, "Thebes", plan);
            Assert.AreEqual(ClueCategory.Geography, a.category);
            Assert.AreEqual("Thebes", a.value);
            Assert.IsFalse(a.isTell);
        }
    }

    [Test]
    public void Answer_AnAnswerTell_GivesTheTrueHomesValue_AndIsATell()
    {
        LiePlan liar = Plan(1f, TellChannel.Answer, ScriptStep.Value(0f), ScriptStep.Range(0), ScriptStep.Range(0));
        Assert.AreEqual(TellChannel.Answer, liar.ChannelOf(ClueCategory.Geography));

        InterviewAnswer a = Interview.Answer(ClueCategory.Geography, "Thebes", liar);
        Assert.AreEqual("Babylon", a.value);
        Assert.IsTrue(a.isTell);

        InterviewAnswer other = Interview.Answer(ClueCategory.Currency, "Deben", liar);
        Assert.AreEqual("Deben", other.value, "a category that is not a tell answers with the cover");
        Assert.IsFalse(other.isTell);
    }

    [Test]
    public void Answer_APapersTell_AnswersWithTheCover()
    {
        LiePlan liar = Plan(1f, TellChannel.Papers, ScriptStep.Value(0f), ScriptStep.Range(0), ScriptStep.Range(0));
        Assert.AreEqual(TellChannel.Papers, liar.ChannelOf(ClueCategory.Currency));

        InterviewAnswer a = Interview.Answer(ClueCategory.Currency, "Deben", liar);
        Assert.AreEqual("Deben", a.value, "a Papers tell leaks on the papers only");
        Assert.IsFalse(a.isTell);
    }

    // -----------------------------
    // Opener and claim
    // -----------------------------

    [TestCase(TravellerGender.Male, "Next! Step forward, sir.")]
    [TestCase(TravellerGender.Female, "Next! Step forward, madam.")]
    [TestCase(TravellerGender.Unknown, "Next! Step forward, traveller.")]
    public void Opener_UsesTheHonorificOfTheRecordedGender(TravellerGender gender, string expected)
    {
        Assert.AreEqual(expected, Interview.Opener(Lines(), gender, null));
    }

    [Test]
    public void Opener_ALegendary_UsesTheLegendaryTemplate()
    {
        Assert.AreEqual("Priority arrival: Nikola Tesla.", Interview.Opener(Lines(), TravellerGender.Unknown, "Nikola Tesla"));
        Assert.AreEqual("Next! Step forward, sir.", Interview.Opener(Lines(), TravellerGender.Male, "  "), "a blank name is no legendary");
    }

    [Test]
    public void Opener_NullLines_GiveEmpty()
    {
        Assert.AreEqual(string.Empty, Interview.Opener(null, TravellerGender.Male, null));
    }

    [Test]
    public void Claim_FillsThePlace_OrGivesTheBareLabelWhenTheTemplateIsBlank()
    {
        Assert.AreEqual("I request passage home to Babylonia (Ancient).", Interview.Claim(Lines(), "Babylonia (Ancient)"));
        Assert.AreEqual("Babylonia (Ancient)", Interview.Claim(new InterviewLines(), "Babylonia (Ancient)"));
        Assert.AreEqual("Babylonia (Ancient)", Interview.Claim(null, "Babylonia (Ancient)"));
    }

    // -----------------------------
    // Small talk
    // -----------------------------

    private static readonly LineText[] PlaceLines = { new LineText("egypt_ancient.smalltalk.1", "The Nile rose."), new LineText("egypt_ancient.smalltalk.2", "The fields are black.") };
    private static readonly LineText[] EraLines = { new LineText("ancient.smalltalk.1", "The harvest was good.") };

    [Test]
    public void PickSmallTalk_ThePlacesLinesWin_OneDraw()
    {
        var rng = new ScriptedRandom(ScriptStep.Range(1));
        Assert.AreEqual("egypt_ancient.smalltalk.2", Interview.PickSmallTalk(PlaceLines, EraLines, rng).id);
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void PickSmallTalk_TheErasLines_WhenThePlaceHasNone()
    {
        var rng = new ScriptedRandom(ScriptStep.Range(0));
        Assert.AreEqual("ancient.smalltalk.1", Interview.PickSmallTalk(new LineText[0], EraLines, rng).id);
        Assert.IsTrue(rng.Done);

        var fromNull = new ScriptedRandom(ScriptStep.Range(0));
        Assert.AreEqual("ancient.smalltalk.1", Interview.PickSmallTalk(null, EraLines, fromNull).id);
        Assert.IsTrue(fromNull.Done);
    }

    [Test]
    public void PickSmallTalk_NoLines_IsNull_WithNoDraw()
    {
        var rng = new ScriptedRandom();
        Assert.IsNull(Interview.PickSmallTalk(new LineText[0], null, rng));
        Assert.AreEqual(0, rng.Draws);
        Assert.IsNull(Interview.PickSmallTalk(PlaceLines, EraLines, null), "no stream, no pick");
    }

    // -----------------------------
    // Token fills
    // -----------------------------

    [Test]
    public void WorstCaseLength_FillsEveryOccurrenceOfTheToken()
    {
        Assert.AreEqual("At home we speak .".Length + 28, Interview.WorstCaseLength("At home we speak {value}.", Interview.ValueToken, 28));
        Assert.AreEqual(", ".Length + 2 * 10, Interview.WorstCaseLength("{value}, {value}", Interview.ValueToken, 10));
        Assert.AreEqual("Here you are.".Length, Interview.WorstCaseLength("Here you are.", Interview.ValueToken, 50));
        Assert.AreEqual(0, Interview.WorstCaseLength(null, Interview.ValueToken, 50));
    }

    [Test]
    public void Fill_ReplacesEveryOccurrence_AndLeavesOtherTokens()
    {
        Assert.AreEqual("Rome and Rome", Interview.Fill("{place} and {place}", Interview.PlaceToken, "Rome"));
        Assert.AreEqual(string.Empty, Interview.Fill(null, Interview.PlaceToken, "Rome"));
        Assert.AreEqual("home to .", Interview.Fill("home to {place}.", Interview.PlaceToken, null));
        Assert.AreEqual("{name} sent Rome", Interview.Fill("{name} sent {place}", Interview.PlaceToken, "Rome"));
        Assert.AreEqual("{value}", Interview.Placeholder(Interview.ValueToken));
    }

    [Test]
    public void HoldsToken_OnlyTheTokensPlaceholderCounts()
    {
        Assert.IsTrue(Interview.HoldsToken("At home we speak {value}.", Interview.ValueToken));
        Assert.IsTrue(Interview.HoldsToken("{place}", Interview.PlaceToken));
        Assert.IsFalse(Interview.HoldsToken("At home we speak value.", Interview.ValueToken), "the bare word is no token");
        Assert.IsFalse(Interview.HoldsToken("I request passage home to {place}.", Interview.ValueToken), "another token");
        Assert.IsFalse(Interview.HoldsToken("{Value}", Interview.ValueToken), "tokens are case-sensitive, as Fill is");
        Assert.IsFalse(Interview.HoldsToken(null, Interview.ValueToken));
    }
}
