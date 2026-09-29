using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

/// <summary>
/// The voice resolver (the personalities spec's V1-V5, §4.3): a premade's own
/// rows first; else the traveller's personality's rows matching their kind and
/// claimed era, the most specific tier (named kinds 2, a named era 1, a named
/// optional key 4) as the pool; no row: the defaults; one line of the pool as
/// a value of the dialog seed and the slot key, never a draw. Small talk: a
/// source first (the personality's, the home's, the kind's) by the weights,
/// then a line, both values.
/// </summary>
public class VoicesTests
{
    private const int Seed = 424242;

    private static readonly VoiceContext RichAncient = new VoiceContext(TravellerKind.RichTourist, "ancient");
    private static readonly VoiceContext DisplacedAncient = new VoiceContext(TravellerKind.Displaced, "ancient");

    private static Voice Curt => new Voice("curt", null, Seed);

    private static VoiceLine Row(string text, string personality = null, string premade = null, string era = null, string key = null,
                                 MissingFormVariant variant = MissingFormVariant.Honest, params TravellerKind[] kinds) =>
        new VoiceLine
        {
            personality = personality ?? string.Empty,
            premade = premade ?? string.Empty,
            kinds = kinds.ToList(),
            era = era ?? string.Empty,
            key = key ?? string.Empty,
            variant = variant,
            line = new LineText("row." + text, text)
        };

    private static InterviewLines Lines() => new InterviewLines
    {
        claims =
        {
            new KindLine { kind = TravellerKind.RichTourist, line = new LineText("interview.claims.RichTourist", "One leisure departure to {place}, please.") },
            new KindLine { kind = TravellerKind.Displaced, line = new LineText("interview.claims.Displaced", "Please. Send me home to {place}.") }
        },
        requestReply = new LineText("interview.requestReply", "Here you are."),
        missingFormReplies =
        {
            new MissingFormReply { kind = TravellerKind.RichTourist, request = "TC-310", variant = MissingFormVariant.Honest, line = new LineText("default.rich.310", "It's a Premium unit, I don't need one.") }
        },
        requests = { new InterviewRequest { id = "step_closer", label = "Step closer", prompt = new LineText("p", "Step closer."), reply = new LineText("interview.requests.step_closer.reply", "Like this?") } },
        smallTalkWeights = new SmallTalkWeights { personality = 1f, home = 1f, kind = 1f }
    };

    // ---- Match, tier, fall back ----

    [Test]
    public void Line_APremadesOwnRowComesFirst()
    {
        InterviewLines lines = Lines();
        lines.voices.claims.Add(Row("{place}. Now.", personality: "curt"));
        lines.voices.claims.Add(Row("Send me home to {place}. But first: what is a home?", premade: "socrates"));
        lines.voices.claims.Add(Row("Somebody else's {place}.", premade: "senenmut"));

        Assert.AreEqual("Send me home to {place}. But first: what is a home?", Voices.Claim(lines, new Voice(null, "socrates", Seed), DisplacedAncient).text);
        Assert.AreEqual("Please. Send me home to {place}.", Voices.Claim(lines, new Voice(null, "aspasia", Seed), DisplacedAncient).text, "a premade with no row: the default, never a personality's");
    }

    [Test]
    public void Line_APersonalityRowBeatsTheDefault()
    {
        InterviewLines lines = Lines();
        lines.voices.handOver.Add(Row("Here.", "curt"));

        Assert.AreEqual("Here.", Voices.HandOver(lines, Curt, RichAncient, "TC-230").text);
        Assert.AreEqual("Here you are.", Voices.HandOver(lines, new Voice("chatty", null, Seed), RichAncient, "TC-230").text, "another personality: the default");
    }

    [Test]
    public void Line_NamedKindsBeatBlankKinds()
    {
        InterviewLines lines = Lines();
        lines.voices.claims.Add(Row("Blank {place}.", "curt"));
        lines.voices.claims.Add(Row("{place}. Home. Now.", "curt", kinds: TravellerKind.Displaced));

        Assert.AreEqual("{place}. Home. Now.", Voices.Claim(lines, Curt, DisplacedAncient).text);
        Assert.AreEqual("Blank {place}.", Voices.Claim(lines, Curt, RichAncient).text);
    }

    [Test]
    public void Line_ANamedEraBeatsABlankEra()
    {
        InterviewLines lines = Lines();
        lines.voices.claims.Add(Row("Any era {place}.", "curt"));
        lines.voices.claims.Add(Row("Ancient {place}.", "curt", era: "ancient"));

        Assert.AreEqual("Ancient {place}.", Voices.Claim(lines, Curt, RichAncient).text);
        Assert.AreEqual("Any era {place}.", Voices.Claim(lines, Curt, new VoiceContext(TravellerKind.RichTourist, "medieval")).text);
    }

    [Test]
    public void Line_KindsOutrankEra()
    {
        InterviewLines lines = Lines();
        lines.voices.claims.Add(Row("Era {place}.", "curt", era: "ancient"));
        lines.voices.claims.Add(Row("Kind {place}.", "curt", kinds: TravellerKind.RichTourist));

        Assert.AreEqual("Kind {place}.", Voices.Claim(lines, Curt, RichAncient).text);
    }

    [Test]
    public void Line_ARowOfAnotherKindOrEraNeverMatches()
    {
        InterviewLines lines = Lines();
        lines.voices.claims.Add(Row("Displaced {place}.", "curt", kinds: TravellerKind.Displaced));
        lines.voices.claims.Add(Row("Medieval {place}.", "curt", era: "medieval"));

        Assert.AreEqual("One leisure departure to {place}, please.", Voices.Claim(lines, Curt, RichAncient).text);
    }

    [Test]
    public void Line_ARowOfAnotherPersonalityOrPremadeNeverMatches()
    {
        InterviewLines lines = Lines();
        lines.voices.claims.Add(Row("Chatty {place}.", "chatty"));
        lines.voices.claims.Add(Row("Socrates {place}.", premade: "socrates"));

        Assert.AreEqual("One leisure departure to {place}, please.", Voices.Claim(lines, Curt, RichAncient).text);
        Assert.AreEqual("Please. Send me home to {place}.", Voices.Claim(lines, new Voice("curt", "senenmut", Seed), DisplacedAncient).text,
                        "a premade speaks its own rows, never its personality's (it has none)");
    }

    [Test]
    public void Line_NoVoiceSaysTheDefault()
    {
        InterviewLines lines = Lines();
        lines.voices.claims.Add(Row("{place}. Now.", "curt"));
        lines.voices.handOver.Add(Row("Here.", "curt"));

        Assert.AreEqual("One leisure departure to {place}, please.", Voices.Claim(lines, Voice.None, RichAncient).text);
        Assert.AreEqual("Here you are.", Voices.HandOver(lines, Voice.None, RichAncient, "TC-230").text);
        Assert.AreEqual("It's a Premium unit, I don't need one.", Voices.Missing(lines, Voice.None, RichAncient, "TC-310", MissingFormVariant.Honest).text);
        Assert.AreEqual("Like this?", Voices.Spoken(lines, Voice.None, RichAncient, lines.requests[0]).text);
        Assert.IsNull(Voices.Claim(lines, Voice.None, new VoiceContext(TravellerKind.Labourer, "ancient")), "no line for the kind: none (the caller says the place alone)");
    }

    [Test]
    public void Line_TheKeysMustMatch_AHandOversNamedRequestScoresAbove()
    {
        InterviewLines lines = Lines();
        lines.voices.missingForms.Add(Row("Grand waiver.", "curt", key: "TC-310", kinds: TravellerKind.RichTourist));
        lines.voices.missingForms.Add(Row("Grand missing.", "curt", key: "TC-310", variant: MissingFormVariant.Missing));
        lines.voices.spoken.Add(Row("Said it once.", "curt", key: "speak_up"));
        lines.voices.handOver.Add(Row("Here.", "curt"));
        lines.voices.handOver.Add(Row("The manifest. Here.", "curt", key: "TC-230"));

        Assert.AreEqual("Grand waiver.", Voices.Missing(lines, Curt, RichAncient, "TC-310", MissingFormVariant.Honest).text);
        Assert.AreEqual("Grand missing.", Voices.Missing(lines, Curt, RichAncient, "TC-310", MissingFormVariant.Missing).text);
        Assert.IsNull(Voices.Missing(lines, Curt, RichAncient, "proof", MissingFormVariant.Honest), "no row for the request and no default: none");
        Assert.AreEqual("Like this?", Voices.Spoken(lines, Curt, RichAncient, lines.requests[0]).text, "a row of another spoken request never matches");
        Assert.AreEqual("The manifest. Here.", Voices.HandOver(lines, Curt, RichAncient, "TC-230").text, "a named request (4) beats a blank one");
        Assert.AreEqual("Here.", Voices.HandOver(lines, Curt, RichAncient, "TC-310").text);
    }

    [Test]
    public void Missing_AMissingVariantWithNoLineOfItsOwnSaysTheHonestLine()
    {
        InterviewLines lines = Lines();
        lines.voices.missingForms.Add(Row("Curt: not mine.", "curt", key: "TC-620"));

        Assert.AreEqual("Curt: not mine.", Voices.Missing(lines, Curt, RichAncient, "TC-620", MissingFormVariant.Missing).text, "the voice's Honest line before the default's");
        Assert.AreEqual("It's a Premium unit, I don't need one.", Voices.Missing(lines, Voice.None, RichAncient, "TC-310", MissingFormVariant.Missing).text);
    }

    // ---- The pick ----

    [Test]
    public void Pick_IsAValueOfTheSeedAndTheSlotKey()
    {
        Assert.AreEqual((int)((uint)Seeds.Mix(Seed, Seeds.OfKey("claim")) % 3u), Voices.Pick(Seed, VoiceKeys.Claim, 3));
        Assert.AreEqual(Voices.Pick(Seed, "claim", 5), Voices.Pick(Seed, "claim", 5), "the same seed and key: the same line");
        Assert.AreEqual(-1, Voices.Pick(Seed, "claim", 0));

        InterviewLines lines = Lines();
        for (int i = 0; i < 4; i++)
            lines.voices.claims.Add(Row($"Claim {i} {{place}}.", "curt"));
        var picks = new HashSet<string>();
        for (int seed = 0; seed < 64; seed++)
            picks.Add(Voices.Claim(lines, new Voice("curt", null, seed), RichAncient).text);
        Assert.AreEqual(4, picks.Count, "every line of the pool is reachable");
    }

    [Test]
    public void Pick_RowsUnderAnotherKeyNeverMoveIt()
    {
        InterviewLines lines = Lines();
        for (int i = 0; i < 3; i++)
            lines.voices.answers.Add(Row($"Currency {i}: {{value}}.", "curt", key: "q_currency"));
        InterviewQuestion currency = Question("q_currency");
        var before = Enumerable.Range(0, 40).Select(s => Voices.Answer(lines, new Voice("curt", null, s), RichAncient, currency).text).ToList();

        lines.voices.answers.Add(Row("Device: {value}.", "curt", key: "q_device"));
        lines.voices.claims.Add(Row("{place}. Go.", "curt"));
        var after = Enumerable.Range(0, 40).Select(s => Voices.Answer(lines, new Voice("curt", null, s), RichAncient, currency).text).ToList();

        CollectionAssert.AreEqual(before, after);
    }

    [Test]
    public void Pool_IsTheBestTierOnly()
    {
        var rows = new List<VoiceLine>
        {
            Row("blank", "curt"), Row("kind a", "curt", kinds: TravellerKind.RichTourist), Row("kind b", "curt", kinds: new[] { TravellerKind.RichTourist, TravellerKind.PoorTourist }),
            Row("era", "curt", era: "ancient"), Row("other", "chatty", kinds: TravellerKind.RichTourist)
        };

        CollectionAssert.AreEqual(new[] { "kind a", "kind b" }, Voices.Pool(rows, Curt, RichAncient, _ => 0).Select(r => r.line.text));
        CollectionAssert.AreEqual(new[] { "kind b" }, Voices.Pool(rows, Curt, RichAncient, r => r.line.text == "kind a" ? ContextMatch.NoMatch : 0).Select(r => r.line.text),
                                  "a row whose keys do not match is out");
        CollectionAssert.AreEqual(new[] { "era" }, Voices.Pool(rows, Curt, RichAncient, r => r.line.text == "era" ? Voices.NamedKeyScore : r.line.text.StartsWith("kind") ? ContextMatch.NoMatch : 0).Select(r => r.line.text),
                                  "a named optional key (4) outranks the context");
        CollectionAssert.IsEmpty(Voices.Pool(rows, Voice.None, RichAncient, _ => 0));
    }

    // ---- Small talk ----

    private static InterviewLines TalkLines(float personality = 1f, float home = 1f, float kind = 1f)
    {
        InterviewLines lines = Lines();
        lines.smallTalkWeights = new SmallTalkWeights { personality = personality, home = home, kind = kind };
        lines.voices.smallTalk.Add(Row("Curt talk.", "curt"));
        lines.kindSmallTalk.Add(Row("Rich talk.", kinds: TravellerKind.RichTourist));
        lines.kindSmallTalk.Add(Row("Displaced talk.", kinds: TravellerKind.Displaced));
        return lines;
    }

    private static readonly LineText[] HomeLines = { new LineText("future.smalltalk.1", "The maglev was late again.") };

    [Test]
    public void SmallTalk_PicksASourceByTheWeightsThenALine()
    {
        InterviewLines lines = TalkLines();
        var said = new Dictionary<string, int>();
        for (int seed = 0; seed < 300; seed++)
        {
            string text = Voices.SmallTalk(lines, new Voice("curt", null, seed), RichAncient, HomeLines).text;
            said[text] = said.TryGetValue(text, out int n) ? n + 1 : 1;
        }
        CollectionAssert.AreEquivalent(new[] { "Curt talk.", "The maglev was late again.", "Rich talk." }, said.Keys, "each source is reachable; never the displaced's kind line");
        Assert.IsTrue(said.Values.All(n => n > 60), "weights 1, 1, 1: about a third each (" + string.Join(", ", said.Select(kv => $"{kv.Key}={kv.Value}")) + ")");

        double u = (uint)Seeds.Mix(7, Seeds.OfKey(VoiceKeys.SmallTalkSource)) / 4294967296.0;
        string expected = u < 1.0 / 3 ? "Curt talk." : u < 2.0 / 3 ? "The maglev was late again." : "Rich talk.";
        Assert.AreEqual(expected, Voices.SmallTalk(lines, new Voice("curt", null, 7), RichAncient, HomeLines).text, "the source is a value of the seed and \"smalltalk:source\"");
    }

    [Test]
    public void SmallTalk_SkipsASourceWithNoLine()
    {
        InterviewLines lines = TalkLines();
        for (int seed = 0; seed < 100; seed++)
        {
            Assert.AreNotEqual("Curt talk.", Voices.SmallTalk(lines, new Voice("chatty", null, seed), RichAncient, HomeLines).text, "chatty has no line");
            Assert.AreEqual("Curt talk.", Voices.SmallTalk(lines, new Voice("curt", null, seed), new VoiceContext(TravellerKind.Labourer, "ancient"), null).text,
                            "no home line, no labourer's line: the personality's");
        }
        Assert.IsNull(Voices.SmallTalk(lines, Voice.None, new VoiceContext(TravellerKind.Labourer, "ancient"), null), "no source has a line: no small talk");
    }

    [Test]
    public void SmallTalk_AZeroWeightSourceIsNeverPicked()
    {
        InterviewLines lines = TalkLines(personality: 0f, home: 1f, kind: 0f);
        for (int seed = 0; seed < 100; seed++)
            Assert.AreEqual("The maglev was late again.", Voices.SmallTalk(lines, new Voice("curt", null, seed), RichAncient, HomeLines).text);
    }

    [Test]
    public void SmallTalk_APremadeSaysOnlyTheirOwn()
    {
        InterviewLines lines = TalkLines();
        lines.voices.smallTalk.Add(Row("My business in Strasbourg is private.", premade: "gutenberg"));
        for (int seed = 0; seed < 50; seed++)
            Assert.AreEqual("My business in Strasbourg is private.", Voices.SmallTalk(lines, new Voice(null, "gutenberg", seed), DisplacedAncient, HomeLines).text);

        var withoutRow = new HashSet<string>(Enumerable.Range(0, 60).Select(s => Voices.SmallTalk(lines, new Voice(null, "senenmut", s), DisplacedAncient, HomeLines).text));
        CollectionAssert.AreEquivalent(new[] { "The maglev was late again.", "Displaced talk." }, withoutRow, "no row of their own yet: the home's and the kind's, never a personality's");
    }

    [Test]
    public void SmallTalk_ACitizensHomeIsThePresent()
    {
        LineText[] athens = { new LineText("greece_ancient.smalltalk.1", "The Assembly met again.") };
        LineText[] ancient = { new LineText("ancient.smalltalk.1", "The harvest was good.") };
        LineText[] present = { new LineText("present.smalltalk.1", "The Directorate Tower hums.") };
        LineText[] future = { new LineText("future.smalltalk.1", "The maglev was late again.") };

        foreach (TravellerKind kind in new[] { TravellerKind.RichTourist, TravellerKind.PoorTourist, TravellerKind.Labourer })
        {
            CollectionAssert.AreEqual(present, Voices.Home(kind, athens, ancient, present, future), kind + ": the present, never the destination");
            CollectionAssert.AreEqual(future, Voices.Home(kind, athens, ancient, null, future), kind + ": the Future era when the present has none");
        }
        CollectionAssert.AreEqual(athens, Voices.Home(TravellerKind.Displaced, athens, ancient, present, future), "the displaced: the claimed place");
        CollectionAssert.AreEqual(ancient, Voices.Home(TravellerKind.Displaced, new LineText[0], ancient, present, future), "else its era");
        CollectionAssert.IsEmpty(Voices.Home(TravellerKind.Displaced, null, null, present, future));
    }

    // ---- Answers ----

    private static InterviewQuestion Question(string id) => new InterviewQuestion
    {
        id = id,
        category = ClueCategory.Currency,
        label = "Currency",
        prompt = new LineText(id + ".prompt", "What will you pay with in {place}?"),
        answer = new LineText(id + ".answer", "I've changed my money into {value}."),
        overrides = { new WordingOverride { kinds = { TravellerKind.Displaced }, answer = new LineText(id + ".overrides.1.answer", "We pay in {value}.") } }
    };

    [Test]
    public void Answer_FallsBackToTheQuestionsOverrideThenItsAnswer()
    {
        InterviewLines lines = Lines();
        lines.voices.answers.Add(Row("{value}. Not that it'll be enough.", "glum", key: "q_currency"));
        InterviewQuestion q = Question("q_currency");

        Assert.AreEqual("{value}. Not that it'll be enough.", Voices.Answer(lines, new Voice("glum", null, Seed), DisplacedAncient, q).text);
        Assert.AreEqual("We pay in {value}.", Voices.Answer(lines, Curt, DisplacedAncient, q).text, "no row: the question's override for the kind");
        Assert.AreEqual("I've changed my money into {value}.", Voices.Answer(lines, Curt, RichAncient, q).text, "no override: its answer");
        Assert.AreEqual("I've changed my money into {value}.", Voices.Answer(lines, new Voice("glum", null, Seed), RichAncient, Question("q_device")).text, "a row of another question never matches");
    }

    // ---- What a line may depend on (T2), the keys ----

    [Test]
    public void ContextHoldsOnlyWhatTheDeskSees()
    {
        // The personalities spec's T2: before the stamp a line depends only on what the desk can see. Adding a field
        // here (a lie, a fault, the true home) would let the hidden truth choose a line: read T2 before changing this.
        CollectionAssert.AreEquivalent(new[] { "kind", "eraId" }, typeof(VoiceContext).GetFields(BindingFlags.Public | BindingFlags.Instance).Select(f => f.Name));
        CollectionAssert.IsEmpty(typeof(VoiceContext).GetProperties(BindingFlags.Public | BindingFlags.Instance));
        Assert.AreEqual((TravellerKind.Displaced, "ancient"), (DisplacedAncient.kind, DisplacedAncient.eraId));
    }

    [Test]
    public void SlotKeys_ArePinned()
    {
        Assert.AreEqual("claim", VoiceKeys.Claim);
        Assert.AreEqual("handover:TC-230", VoiceKeys.HandOver("TC-230"));
        Assert.AreEqual("missing:TC-310:Honest", VoiceKeys.Missing("TC-310", MissingFormVariant.Honest));
        Assert.AreEqual("missing:proof:Missing", VoiceKeys.Missing("proof", MissingFormVariant.Missing));
        Assert.AreEqual("spoken:step_closer", VoiceKeys.Spoken("step_closer"));
        Assert.AreEqual("answer:q_currency", VoiceKeys.Answer("q_currency"));
        Assert.AreEqual("smalltalk:source", VoiceKeys.SmallTalkSource);
        Assert.AreEqual("smalltalk", VoiceKeys.SmallTalk);
    }

    // ---- The reaction's intent (the personalities spec's R2) ----

    [Test]
    public void Intent_OfAPlaceLieSmugglingOrARecordLieIsLying()
    {
        Assert.AreEqual(ReactionIntent.Lying, ReactionIntents.Of(true, false), "a place lie, smuggling included (IsLiar)");
        Assert.AreEqual(ReactionIntent.Lying, ReactionIntents.Of(false, true), "a record lie (IsForger)");
        Assert.AreEqual(ReactionIntent.Lying, ReactionIntents.Of(true, true));
    }

    [Test]
    public void Intent_OfADirectiveFaultACostumeErrorOrNoFaultIsHonest()
    {
        Assert.AreEqual(ReactionIntent.Honest, ReactionIntents.Of(false, false), "no lie: a directive fault, a costume error or no fault at all");
    }

    // ---- The reaction (R1-R3, §6) ----

    private static VoiceLine React(string text, string personality, ReactionVerdict verdict, ReactionIntent intent, string reason = null, string then = null,
                                   string premade = null, params TravellerKind[] kinds)
    {
        VoiceLine row = Row(text, personality, premade, kinds: kinds);
        row.verdict = verdict;
        row.intent = intent;
        row.reason = reason ?? string.Empty;
        row.then = new LineText("row." + text + ".then", then);
        return row;
    }

    private static InterviewLines ReactionLines()
    {
        InterviewLines lines = Lines();
        foreach (ReactionVerdict verdict in new[] { ReactionVerdict.Accepted, ReactionVerdict.Denied })
            foreach (ReactionIntent intent in new[] { ReactionIntent.Honest, ReactionIntent.Lying })
                lines.reactions.Add(React($"Default {verdict} {intent}.", null, verdict, intent));
        return lines;
    }

    [Test]
    public void Reaction_APremadesOwnPairComesFirst()
    {
        InterviewLines lines = ReactionLines();
        lines.voices.reactions.Add(React("You ask good questions.", null, ReactionVerdict.Denied, ReactionIntent.Lying, then: "I taught you that.", premade: "socrates"));
        lines.voices.reactions.Add(React("Figures.", "curt", ReactionVerdict.Denied, ReactionIntent.Lying));

        Assert.AreEqual("You ask good questions.", Voices.Reaction(lines, new Voice(null, "socrates", Seed), DisplacedAncient, ReactionVerdict.Denied, ReactionIntent.Lying, "forged").line.text);
        Assert.AreEqual("Default Denied Lying.", Voices.Reaction(lines, new Voice(null, "aspasia", Seed), DisplacedAncient, ReactionVerdict.Denied, ReactionIntent.Lying, "forged").line.text);
    }

    [Test]
    public void Reaction_ANamedReasonBeatsNamedKinds()
    {
        InterviewLines lines = ReactionLines();
        lines.voices.reactions.Add(React("Fine. Keep it.", "curt", ReactionVerdict.Denied, ReactionIntent.Lying, reason: "smuggled"));
        lines.voices.reactions.Add(React("Rich people problems.", "curt", ReactionVerdict.Denied, ReactionIntent.Lying, kinds: TravellerKind.RichTourist));

        Assert.AreEqual("Fine. Keep it.", Voices.Reaction(lines, Curt, RichAncient, ReactionVerdict.Denied, ReactionIntent.Lying, "smuggled").line.text);
        Assert.AreEqual("Rich people problems.", Voices.Reaction(lines, Curt, RichAncient, ReactionVerdict.Denied, ReactionIntent.Lying, "forged").line.text);
    }

    [Test]
    public void Reaction_ARowOfAnotherReasonNeverMatches()
    {
        InterviewLines lines = ReactionLines();
        lines.voices.reactions.Add(React("Fine. Keep it.", "curt", ReactionVerdict.Denied, ReactionIntent.Lying, reason: "smuggled"));

        Assert.AreEqual("Default Denied Lying.", Voices.Reaction(lines, Curt, RichAncient, ReactionVerdict.Denied, ReactionIntent.Lying, "forged").line.text);
        Assert.AreEqual("Default Denied Lying.", Voices.Reaction(lines, Curt, RichAncient, ReactionVerdict.Denied, ReactionIntent.Lying, string.Empty).line.text);
    }

    [Test]
    public void Reaction_FallsBackToTheDefaults()
    {
        InterviewLines lines = ReactionLines();
        lines.reactions.Add(React("Then how do I get home?", null, ReactionVerdict.Denied, ReactionIntent.Honest, kinds: TravellerKind.Displaced));

        Assert.AreEqual("Then how do I get home?", Voices.Reaction(lines, Curt, DisplacedAncient, ReactionVerdict.Denied, ReactionIntent.Honest, "closed").line.text, "the defaults tier the same way");
        Assert.AreEqual("Default Denied Honest.", Voices.Reaction(lines, Curt, RichAncient, ReactionVerdict.Denied, ReactionIntent.Honest, "closed").line.text);
        Assert.IsNull(Voices.Reaction(Lines(), Curt, RichAncient, ReactionVerdict.Denied, ReactionIntent.Honest, null), "no row at all: none");
    }

    [Test]
    public void Reaction_TheFourDefaultsCoverEveryVerdictAndIntent()
    {
        InterviewLines lines = ReactionLines();
        foreach (ReactionVerdict verdict in new[] { ReactionVerdict.Accepted, ReactionVerdict.Denied })
            foreach (ReactionIntent intent in new[] { ReactionIntent.Honest, ReactionIntent.Lying })
                Assert.AreEqual($"Default {verdict} {intent}.", Voices.Reaction(lines, Voice.None, RichAncient, verdict, intent, null).line.text);
    }

    [Test]
    public void Reaction_KeepsItsThenLine()
    {
        InterviewLines lines = ReactionLines();
        lines.voices.reactions.Add(React("Figures.", "curt", ReactionVerdict.Denied, ReactionIntent.Lying, then: "Same time tomorrow, then."));

        VoiceLine row = Voices.Reaction(lines, Curt, RichAncient, ReactionVerdict.Denied, ReactionIntent.Lying, "forged");
        Assert.AreEqual(("Figures.", "Same time tomorrow, then."), (row.line.text, row.then.text));
    }

    [Test]
    public void Reaction_PickIsAValueOfItsKey()
    {
        InterviewLines lines = ReactionLines();
        for (int i = 0; i < 5; i++)
            lines.voices.reactions.Add(React($"Line {i}.", "curt", ReactionVerdict.Accepted, ReactionIntent.Honest));

        int index = Voices.Pick(Seed, VoiceKeys.Reaction(ReactionVerdict.Accepted, ReactionIntent.Honest), 5);
        Assert.AreEqual($"Line {index}.", Voices.Reaction(lines, Curt, RichAncient, ReactionVerdict.Accepted, ReactionIntent.Honest, null).line.text);
        Assert.AreEqual("reaction:Accepted:Honest", VoiceKeys.Reaction(ReactionVerdict.Accepted, ReactionIntent.Honest));
    }

    // ---- The slip's line (T10) ----

    private static VoiceLine SlipRow(string text, string personality, string lie = null, string premade = null)
    {
        VoiceLine row = Row(text, personality, premade);
        row.lie = lie ?? string.Empty;
        return row;
    }

    [Test]
    public void Slip_ALieKindRowBeatsABlankOne()
    {
        InterviewLines lines = Lines();
        lines.voices.slips.Add(SlipRow("Everything's in order. Don't check.", "curt"));
        lines.voices.slips.Add(SlipRow("Premium. Obviously. Next question.", "curt", "PoorPosingAsRich"));

        Assert.AreEqual("Premium. Obviously. Next question.", Voices.Slip(lines, Curt, RichAncient, LieKind.PoorPosingAsRich).text);
        Assert.AreEqual("Everything's in order. Don't check.", Voices.Slip(lines, Curt, RichAncient, LieKind.Smuggling).text, "another lie kind's row never matches");
    }

    [Test]
    public void Slip_APremadesOwnRow()
    {
        InterviewLines lines = Lines();
        lines.slips.Add(SlipRow("Default slip.", null));
        lines.voices.slips.Add(SlipRow("I know that I know nothing. Especially about where I was born.", null, premade: "socrates"));

        Assert.AreEqual("I know that I know nothing. Especially about where I was born.", Voices.Slip(lines, new Voice(null, "socrates", Seed), DisplacedAncient, LieKind.FalseOrigin).text);
    }

    [Test]
    public void Slip_FallsBackToTheDefaultsByLieKind()
    {
        InterviewLines lines = Lines();
        lines.slips.Add(SlipRow("Default slip.", null));
        lines.slips.Add(SlipRow("Nothing from home in my luggage.", null, "Smuggling"));

        Assert.AreEqual("Nothing from home in my luggage.", Voices.Slip(lines, Curt, RichAncient, LieKind.Smuggling).text);
        Assert.AreEqual("Default slip.", Voices.Slip(lines, Curt, RichAncient, LieKind.DoctoredIdentity).text);
    }

    [Test]
    public void Slip_PickIsAValueOfItsKey()
    {
        InterviewLines lines = Lines();
        for (int i = 0; i < 4; i++)
            lines.voices.slips.Add(SlipRow($"Slip {i}.", "curt"));
        Assert.AreEqual($"Slip {Voices.Pick(Seed, VoiceKeys.Slip, 4)}.", Voices.Slip(lines, Curt, RichAncient, LieKind.Smuggling).text);
    }
}
