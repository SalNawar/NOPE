using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The voice lines' content rules (the personalities spec's C3, V7-V8, §9.2),
/// one rule Generate World and the content validator share: every row names
/// exactly one known voice, a known era and a key its slot knows; its text is
/// not blank, holds its slot's required token and no other, and fits the
/// transcript with the longest fills; a checkable value in a line warns (the
/// fact guard), as does a duplicate row; the small-talk weights and the kinds'
/// small talk; and each personality's coverage as an info line.
/// </summary>
public class VoiceChecksTests
{
    private static VoiceLine Row(string text, string personality = "curt", string premade = null, string era = null, string key = null,
                                 MissingFormVariant variant = MissingFormVariant.Honest, params TravellerKind[] kinds) =>
        new VoiceLine
        {
            personality = personality ?? string.Empty,
            premade = premade ?? string.Empty,
            kinds = kinds.ToList(),
            era = era ?? string.Empty,
            key = key ?? string.Empty,
            variant = variant,
            line = new LineText("id", text)
        };

    /// <summary>A sound input: the cast of two, the premades, the eras, the questions, the spoken requests, the requests the menus offer, the four kinds' small talk.</summary>
    private static VoiceCheckInput Input()
    {
        var input = new VoiceCheckInput
        {
            Cast = new List<Personality> { new Personality { id = "curt", name = "Curt", weight = 1f }, new Personality { id = "glum", name = "Glum", weight = 1f } },
            Premades = new[] { "senenmut", "socrates" },
            Eras = new[] { "ancient", "medieval", "future" },
            Questions = new[] { "q_currency", "q_device" },
            SpokenRequests = new[] { "step_closer", "speak_up" },
            Requests = new[] { "TC-230", "TC-310", "proof", "TC-620", "TC-630" },
            KindsInPlay = new[] { TravellerKind.RichTourist, TravellerKind.Displaced },
            Weights = new SmallTalkWeights { personality = 1f, home = 1f, kind = 1f },
            KindSmallTalk = new List<VoiceLine>
            {
                Row("We do three eras a year.", null, kinds: TravellerKind.RichTourist),
                Row("Everything in this century hums.", null, kinds: TravellerKind.Displaced)
            },
            MaxLineChars = 100,
            LongestPlace = 43,
            LongestValue = 28,
            LongestDocument = 28,
            FactValues = new[] { "Silver drachma (owl)", "Credits", "Wrist comm" },
            TransponderModels = new[] { "Chronos Elite", "Hopper Mk II" },
            Employers = new[] { "Nile Quarry Syndicate" }
        };
        input.Voices.claims.Add(Row("{place}. Home. Now.", kinds: TravellerKind.Displaced));
        input.Voices.claims.Add(Row("Send me home to {place}. But first: what is a home?", null, premade: "socrates"));
        input.Voices.handOver.Add(Row("Here."));
        input.Voices.missingForms.Add(Row("Premium units are exempt from the {document}.", key: "TC-310", kinds: TravellerKind.RichTourist));
        input.Voices.spoken.Add(Row("Said it once. That was billed.", key: "speak_up"));
        input.Voices.answers.Add(Row("{value}. Not that it'll be enough.", "glum", key: "q_currency"));
        input.Voices.smallTalk.Add(Row("The maglev was late. So was my pay.", "glum"));
        return input;
    }

    private static string Errors(VoiceCheckInput input) => string.Join("\n", VoiceChecks.Problems(input).Errors);

    private static string Warnings(VoiceCheckInput input) => string.Join("\n", VoiceChecks.Problems(input).Warnings);

    [Test]
    public void ASoundInput_HasNoErrorOrWarning()
    {
        VoiceCheckResult result = VoiceChecks.Problems(Input());
        CollectionAssert.IsEmpty(result.Errors);
        CollectionAssert.IsEmpty(result.Warnings);
    }

    [Test]
    public void ARowNamesExactlyOneVoice()
    {
        VoiceCheckInput input = Input();
        input.Voices.handOver.Add(Row("No one.", null));
        input.Voices.handOver.Add(Row("Both.", "curt", premade: "socrates"));

        StringAssert.Contains("interview.voices.handOver row 2 names no personality and no premade", Errors(input));
        StringAssert.Contains("interview.voices.handOver row 3 names both a personality and a premade", Errors(input));
    }

    [Test]
    public void AClaimWithoutPlace()
    {
        VoiceCheckInput input = Input();
        input.Voices.claims.Add(Row("Home. Now."));
        StringAssert.Contains("interview.voices.claims row 3 (curt) must hold {place}", Errors(input));
    }

    [Test]
    public void AnAnswerWithoutValue()
    {
        VoiceCheckInput input = Input();
        input.Voices.answers.Add(Row("Not that it'll be enough.", key: "q_device"));
        StringAssert.Contains("interview.voices.answers row 2 (curt) must hold {value}", Errors(input));
    }

    [Test]
    public void AnUnknownToken()
    {
        VoiceCheckInput input = Input();
        input.Voices.smallTalk.Add(Row("Hello {name}."));
        StringAssert.Contains("interview.voices.smallTalk row 2 (curt) holds the unknown token {name}", Errors(input));
    }

    [Test]
    public void ATokenOutsideItsSlot()
    {
        VoiceCheckInput input = Input();
        input.Voices.claims.Add(Row("{place} for {value}."));
        input.Voices.smallTalk.Add(Row("Another {document}."));
        string errors = Errors(input);
        StringAssert.Contains("interview.voices.claims row 3 (curt) holds {value}, which a claim cannot fill (only {place})", errors);
        StringAssert.Contains("interview.voices.smallTalk row 2 (curt) holds {document}, which small talk cannot fill (only {place})", errors);
        CollectionAssert.IsEmpty(VoiceChecks.Problems(Input()).Errors, "{document} in a refusal and {value} in an answer are theirs");
    }

    [Test]
    public void TooLongWithTheLongestFill()
    {
        VoiceCheckInput input = Input();
        input.Voices.claims.Add(Row("{place}: " + new string('x', 54) + "."));
        input.Voices.claims.Add(Row("{place}: " + new string('x', 55) + "."));
        string errors = Errors(input);
        StringAssert.DoesNotContain("row 3 (curt) can render", errors, "43 + 57 = 100 characters fit");
        StringAssert.Contains("interview.voices.claims row 4 (curt) can render 101 characters with the longest fills; the transcript holds at most 100", errors);
    }

    [Test]
    public void AnUnknownPersonalityOrPremade()
    {
        VoiceCheckInput input = Input();
        input.Voices.handOver.Add(Row("Here, darling.", "chatty"));
        input.Voices.handOver.Add(Row("Here, citizen.", null, premade: "napoleon"));
        string errors = Errors(input);
        StringAssert.Contains("interview.voices.handOver row 2 names the personality 'chatty', which personalities does not list", errors);
        StringAssert.Contains("interview.voices.handOver row 3 names the premade 'napoleon', which premades does not list", errors);
    }

    [Test]
    public void AnUnknownEra()
    {
        VoiceCheckInput input = Input();
        input.Voices.handOver.Add(Row("Here.", era: "jurassic"));
        StringAssert.Contains("interview.voices.handOver row 2 (curt) names the era 'jurassic', which eras does not list", Errors(input));
    }

    [Test]
    public void AnUnknownQuestion()
    {
        VoiceCheckInput input = Input();
        input.Voices.answers.Add(Row("{value}.", key: "q_weather"));
        input.Voices.answers.Add(Row("{value}."));
        string errors = Errors(input);
        StringAssert.Contains("interview.voices.answers row 2 (curt) names the question 'q_weather', which questions does not list", errors);
        StringAssert.Contains("interview.voices.answers row 3 (curt) names no question", errors);
    }

    [Test]
    public void AnUnknownSpokenRequest()
    {
        VoiceCheckInput input = Input();
        input.Voices.spoken.Add(Row("Louder?", key: "shout"));
        StringAssert.Contains("interview.voices.spoken row 2 (curt) names the spoken request 'shout', which interview.requests does not list", Errors(input));
    }

    [Test]
    public void ARefusalsRequestNoDayOffers()
    {
        VoiceCheckInput input = Input();
        input.Voices.missingForms.Add(Row("No contract.", key: "TC-520"));
        input.Voices.missingForms.Add(Row("No what?"));
        input.Voices.handOver.Add(Row("The contract.", key: "TC-999"));
        string errors = Errors(input);
        StringAssert.Contains("interview.voices.missingForms row 2 (curt) names the request 'TC-520', which no day's papers menu offers", errors);
        StringAssert.Contains("interview.voices.missingForms row 3 (curt) names no request", errors);
        StringAssert.Contains("interview.voices.handOver row 2 (curt) names the request 'TC-999', which no day's papers menu offers", errors);
    }

    [Test]
    public void BlankText()
    {
        VoiceCheckInput input = Input();
        input.Voices.smallTalk.Add(Row(" "));
        input.KindSmallTalk.Add(Row("", null, kinds: TravellerKind.Labourer));
        string errors = Errors(input);
        StringAssert.Contains("interview.voices.smallTalk row 2 (curt) is blank", errors);
        StringAssert.Contains("interview.kindSmallTalk row 3 is blank", errors);
    }

    [Test]
    public void AFactValueWarns()
    {
        VoiceCheckInput input = Input();
        input.Voices.smallTalk.Add(Row("I only ever pay in credits, you know."));
        StringAssert.Contains("interview.voices.smallTalk row 2 (curt) names the checkable value 'Credits'; a voice line is never evidence (the fact guard)", Warnings(input));
        CollectionAssert.IsEmpty(VoiceChecks.Problems(input).Errors, "a warning, never an error, outside a slip");
    }

    [Test]
    public void ATransponderModelWarns()
    {
        VoiceCheckInput input = Input();
        input.Voices.claims.Add(Row("{place}, on my Chronos Elite."));
        StringAssert.Contains("interview.voices.claims row 3 (curt) names the transponder model 'Chronos Elite'", Warnings(input));
    }

    [Test]
    public void AnEmployerWarns()
    {
        VoiceCheckInput input = Input();
        input.KindSmallTalk.Add(Row("The Nile Quarry Syndicate pays on time.", null, kinds: TravellerKind.Labourer));
        StringAssert.Contains("interview.kindSmallTalk row 3 names the employer 'Nile Quarry Syndicate'", Warnings(input));
    }

    [Test]
    public void ADuplicateRowWarns()
    {
        VoiceCheckInput input = Input();
        input.Voices.handOver.Add(Row("Here."));
        input.Voices.handOver.Add(Row("Here.", era: "ancient"));
        string warnings = Warnings(input);
        StringAssert.Contains("interview.voices.handOver row 2 (curt) repeats row 1 in every column", warnings);
        StringAssert.DoesNotContain("row 3 (curt) repeats", warnings, "a row naming an era is another row");
    }

    [Test]
    public void TheSmallTalkWeights()
    {
        VoiceCheckInput input = Input();
        input.Weights = new SmallTalkWeights { personality = -1f, home = 0f, kind = 0f };
        string errors = Errors(input);
        StringAssert.Contains("interview.smallTalkWeights.personality is negative (-1)", errors);
        StringAssert.Contains("interview.smallTalkWeights: no source has a weight above 0, so no traveller makes small talk", errors);

        input.Weights = new SmallTalkWeights { personality = 0f, home = 0f, kind = 2f };
        CollectionAssert.IsEmpty(VoiceChecks.Problems(input).Errors, "one positive weight is enough");
    }

    [Test]
    public void AKindInPlayWithoutKindSmallTalk()
    {
        VoiceCheckInput input = Input();
        input.KindsInPlay = new[] { TravellerKind.RichTourist, TravellerKind.Labourer, TravellerKind.Displaced };
        input.KindSmallTalk.Add(Row("Everywhere.", null));
        string errors = Errors(input);
        StringAssert.Contains("interview.kindSmallTalk: Labourer travellers are in play, but no row names them", errors);
        StringAssert.Contains("interview.kindSmallTalk row 3 names no kind", errors);
    }

    [Test]
    public void CoverageListsEachPersonalitysFallbacks()
    {
        List<string> info = VoiceChecks.Problems(Input()).Info;
        CollectionAssert.AreEqual(new[]
        {
            "Personality 'curt' (Curt) has its own lines for claims, handOver, missingForms, spoken; the defaults speak its answers, smallTalk.",
            "Personality 'glum' (Glum) has its own lines for answers, smallTalk; the defaults speak its claims, handOver, missingForms, spoken."
        }, info);
    }
}
