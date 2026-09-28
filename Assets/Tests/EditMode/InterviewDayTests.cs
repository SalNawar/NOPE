using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// What the office offers on a day. Questions: Currency (ungated), Capital
/// (DayAtLeast 2), Ruler (DayAtLeast 3), Date of birth (UpgradeOwned
/// interview_protocols) and a Language question gated by the flag
/// "met_tesla". Dialogs: the rumour (DayAtLeast 2, one-shot), its follow-up
/// (FlagSet rumour_calculators, one-shot), a repeatable chat (ungated) and a
/// broken dialog with no nodes (DayAtLeast 99).
/// </summary>
public class InterviewDayTests
{
    private static GateCondition Day(int n) => new GateCondition(TriggerConditionType.DayAtLeast, null, n);

    private static GateSnapshot Snap(int day, string[] flags = null, string[] upgrades = null) =>
        new GateSnapshot(day, 100f, flags, upgrades, null, null, null, null);

    private static Gated<InterviewQuestion> Question(string id, ClueCategory category, params GateCondition[] conditions) =>
        new Gated<InterviewQuestion>(new InterviewQuestion { id = id, category = category, label = id }, conditions);

    /// <summary>A question the desk asks of <paramref name="kinds"/> only (world_source.json questions[].kinds).</summary>
    private static Gated<InterviewQuestion> KindQuestion(string id, ClueCategory category, TravellerKind[] kinds, params GateCondition[] conditions)
    {
        Gated<InterviewQuestion> q = Question(id, category, conditions);
        q.Item.kinds.AddRange(kinds);
        return q;
    }

    private static readonly TravellerKind[] Citizens = { TravellerKind.RichTourist, TravellerKind.PoorTourist, TravellerKind.Labourer };
    private static readonly TravellerKind[] Displaced = { TravellerKind.Displaced };

    /// <summary>Day 4's questions (traveller types I1): the home questions the displaced's, the trip questions (Currency, Technology, from day 4) the citizens', the birth date everyone's.</summary>
    private static List<Gated<InterviewQuestion>> KindQuestions() => new List<Gated<InterviewQuestion>>
    {
        KindQuestion("q_currency", ClueCategory.Currency, Displaced),
        KindQuestion("q_device", ClueCategory.Technology, Displaced),
        KindQuestion("q_capital", ClueCategory.Geography, Displaced, Day(2)),
        Question("q_born", ClueCategory.BirthDate, new GateCondition(TriggerConditionType.UpgradeOwned, "interview_protocols", 0f)),
        KindQuestion("q_trip_currency", ClueCategory.Currency, Citizens, Day(4)),
        KindQuestion("q_trip_device", ClueCategory.Technology, Citizens, Day(4))
    };

    private static InterviewDay KindDay(GateSnapshot snapshot) =>
        new InterviewDay(new InterviewLines { menuCapacity = 8 }, KindQuestions(), null, snapshot, new ShiftLedger(), null);

    private static List<Gated<InterviewQuestion>> Questions() => new List<Gated<InterviewQuestion>>
    {
        Question("q_currency", ClueCategory.Currency),
        Question("q_capital", ClueCategory.Geography, Day(2)),
        Question("q_ruler", ClueCategory.Politics, Day(3)),
        Question("q_born", ClueCategory.BirthDate, new GateCondition(TriggerConditionType.UpgradeOwned, "interview_protocols", 0f)),
        Question("q_tongue", ClueCategory.Language, new GateCondition(TriggerConditionType.FlagSet, "met_tesla", 0f))
    };

    /// <summary>A sound one-node dialog: one ending choice, with an effect for one-shot dialogs.</summary>
    private static AuthoredDialog Dialog(string id, bool oneShot) => new AuthoredDialog
    {
        id = id,
        label = id,
        oneShot = oneShot,
        nodes =
        {
            new ScriptNode
            {
                id = "start",
                choices = { new ScriptChoice { id = "bye", label = "Bye.", next = "", effect = oneShot ? "Effect_" + id : "" } }
            }
        }
    };

    private static List<Gated<AuthoredDialog>> Dialogs() => new List<Gated<AuthoredDialog>>
    {
        new Gated<AuthoredDialog>(Dialog("dlg_rumour", true), new[] { Day(2) }),
        new Gated<AuthoredDialog>(Dialog("dlg_rumour_followup", true), new[] { new GateCondition(TriggerConditionType.FlagSet, "rumour_calculators", 0f) }),
        new Gated<AuthoredDialog>(Dialog("dlg_chat", false), null),
        new Gated<AuthoredDialog>(new AuthoredDialog { id = "dlg_broken", label = "Broken" }, new[] { Day(99) })
    };

    private static InterviewDay DayOf(GateSnapshot snapshot, ShiftLedger ledger = null) =>
        new InterviewDay(new InterviewLines { menuCapacity = 8 }, Questions(), Dialogs(), snapshot, ledger ?? new ShiftLedger(), null);

    private static string[] Offered(InterviewDay day, string premadeDialogId = null) => day.OfferedDialogs(premadeDialogId).Select(d => d.id).ToArray();

    [Test]
    public void Questions_AreThoseWhoseConditionsPass_InLibraryOrder()
    {
        InterviewDay day1 = DayOf(Snap(1));
        CollectionAssert.AreEqual(new[] { "q_currency" }, day1.Questions.Select(q => q.id).ToArray());
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency }, day1.AskableCategories);

        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Geography }, DayOf(Snap(2)).AskableCategories);
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Geography, ClueCategory.Politics }, DayOf(Snap(3)).AskableCategories);

        InterviewDay everything = DayOf(Snap(3, new[] { "met_tesla" }, new[] { "interview_protocols" }));
        CollectionAssert.AreEqual(new[] { "q_currency", "q_capital", "q_ruler", "q_born", "q_tongue" }, everything.Questions.Select(q => q.id).ToArray());
    }

    [Test]
    public void AnswerTellCategories_AreTheDayGatedQuestionsOnly()
    {
        InterviewDay day = DayOf(Snap(3, new[] { "met_tesla" }, new[] { "interview_protocols" }));
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Geography, ClueCategory.Politics, ClueCategory.BirthDate, ClueCategory.Language }, day.AskableCategories);
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Geography, ClueCategory.Politics }, day.AnswerTellCategories,
                                  "upgrade- and flag-gated questions are hint-only");

        CollectionAssert.AreEqual(DayOf(Snap(3)).AnswerTellCategories, day.AnswerTellCategories, "a purchase or a flag never changes the tell categories");
    }

    [Test]
    public void QuestionsFor_AreTheKindsAskableQuestions_InLibraryOrder_TheTripQuestionsFromDay4()
    {
        InterviewDay day3 = KindDay(Snap(3, upgrades: new[] { "interview_protocols" }));
        CollectionAssert.AreEqual(new[] { "q_currency", "q_device", "q_capital", "q_born" }, day3.QuestionsFor(TravellerKind.Displaced).Select(q => q.id).ToArray());
        CollectionAssert.AreEqual(new[] { "q_born" }, day3.QuestionsFor(TravellerKind.RichTourist).Select(q => q.id).ToArray(), "a citizen is never asked about home");
        CollectionAssert.AreEqual(new[] { ClueCategory.BirthDate }, day3.AskableCategoriesFor(TravellerKind.Labourer));

        InterviewDay day4 = KindDay(Snap(4));
        CollectionAssert.AreEqual(new[] { "q_trip_currency", "q_trip_device" }, day4.QuestionsFor(TravellerKind.PoorTourist).Select(q => q.id).ToArray());
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Technology }, day4.AskableCategoriesFor(TravellerKind.RichTourist));
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Technology, ClueCategory.Geography }, day4.AskableCategoriesFor(TravellerKind.Displaced));
        CollectionAssert.AreEqual(new[] { "q_currency", "q_device", "q_capital", "q_trip_currency", "q_trip_device" }, day4.Questions.Select(q => q.id).ToArray(), "every kind's, in library order");
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Technology, ClueCategory.Geography, ClueCategory.Currency, ClueCategory.Technology }, day4.AskableCategories);
    }

    [Test]
    public void AnswerTellCategoriesFor_AreTheKindsDayGatedQuestions()
    {
        InterviewDay day4 = KindDay(Snap(4, upgrades: new[] { "interview_protocols" }));
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Technology }, day4.AnswerTellCategoriesFor(TravellerKind.Labourer), "the trip questions may carry a smuggler's spoken tell");
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Technology, ClueCategory.Geography }, day4.AnswerTellCategoriesFor(TravellerKind.Displaced));
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Technology, ClueCategory.Geography, ClueCategory.BirthDate, ClueCategory.Currency, ClueCategory.Technology }, day4.AskableCategories);
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Technology, ClueCategory.Geography, ClueCategory.Currency, ClueCategory.Technology }, day4.AnswerTellCategories, "the upgrade-gated birth date is hint-only for everyone");
        CollectionAssert.IsEmpty(KindDay(Snap(3)).AnswerTellCategoriesFor(TravellerKind.RichTourist), "before day 4 a citizen has no question that could carry a tell");
    }

    [Test]
    public void AQuestionNamingNoKind_IsEveryKinds()
    {
        InterviewDay day = DayOf(Snap(3));
        foreach (TravellerKind kind in (TravellerKind[])System.Enum.GetValues(typeof(TravellerKind)))
        {
            CollectionAssert.AreEqual(day.Questions, day.QuestionsFor(kind), kind.ToString());
            CollectionAssert.AreEqual(day.AskableCategories, day.AskableCategoriesFor(kind), kind.ToString());
            CollectionAssert.AreEqual(day.AnswerTellCategories, day.AnswerTellCategoriesFor(kind), kind.ToString());
        }
    }

    [Test]
    public void InterviewQuestions_OneQuestionPerCategoryPerKind()
    {
        List<InterviewQuestion> sound = KindQuestions().Select(q => q.Item).ToList();
        CollectionAssert.IsEmpty(InterviewQuestions.Problems(sound), "the home and trip questions share categories across disjoint kinds");
        Assert.AreEqual(4, InterviewQuestions.MostForOneKind(sound), "the displaced's four");

        // A trip question asking every kind collides with the home question of the displaced, and only there.
        sound[4].kinds.Clear();
        List<string> problems = InterviewQuestions.Problems(sound);
        Assert.AreEqual(1, problems.Count, string.Join(" | ", problems));
        Assert.AreEqual("Question 'q_trip_currency' asks about Currency for Displaced, as 'q_currency' does (one question per category per kind).", problems[0]);
        Assert.AreEqual(5, InterviewQuestions.MostForOneKind(sound), "the displaced are now asked five");

        // Two questions naming no kind: reported once per kind, in enum order.
        List<InterviewQuestion> twice = new[] { Question("a", ClueCategory.Politics).Item, Question("b", ClueCategory.Politics).Item }.ToList();
        problems = InterviewQuestions.Problems(twice);
        Assert.AreEqual(4, problems.Count, string.Join(" | ", problems));
        StringAssert.StartsWith("Question 'b' asks about Politics for RichTourist, as 'a' does", problems[0]);

        CollectionAssert.IsEmpty(InterviewQuestions.Problems(null));
        Assert.AreEqual(0, InterviewQuestions.MostForOneKind(null));
        CollectionAssert.IsEmpty(InterviewQuestions.Problems(new List<InterviewQuestion> { null }));
    }

    [Test]
    public void Dialogs_WhoseConditionsFail_AreNotOffered()
    {
        CollectionAssert.AreEqual(new[] { "dlg_chat" }, Offered(DayOf(Snap(1))));
        CollectionAssert.AreEqual(new[] { "dlg_rumour", "dlg_chat" }, Offered(DayOf(Snap(2))));
        CollectionAssert.AreEqual(new[] { "dlg_rumour", "dlg_rumour_followup", "dlg_chat" }, Offered(DayOf(Snap(3, new[] { "rumour_calculators" }))));
    }

    [Test]
    public void AOneShotDialogAlreadyDone_IsNotOffered_ARepeatableOneStillIs()
    {
        InterviewDay day = DayOf(Snap(3, new[] { FlagKeys.DialogDone("dlg_rumour"), FlagKeys.DialogDone("dlg_chat") }));
        CollectionAssert.AreEqual(new[] { "dlg_chat" }, Offered(day));
    }

    [Test]
    public void ABrokenDialog_IsNeverOffered_AndContentProblemsNameIt_EvenWhenItsConditionsFail()
    {
        InterviewDay day = DayOf(Snap(1));
        CollectionAssert.DoesNotContain(Offered(day), "dlg_broken");
        Assert.AreEqual(1, day.ContentProblems.Count, string.Join(" | ", day.ContentProblems));
        StringAssert.StartsWith("Dialog 'dlg_broken' is not offered: ", day.ContentProblems[0]);
        StringAssert.Contains("no nodes", day.ContentProblems[0]);

        // A rumour-shaped start node with two choices: too many for a traveller wheel of one, fine with no capacity set.
        var twoChoices = new List<Gated<AuthoredDialog>> { new Gated<AuthoredDialog>(new AuthoredDialog
        {
            id = "dlg_two",
            label = "Two",
            oneShot = true,
            nodes =
            {
                new ScriptNode
                {
                    id = "start",
                    choices =
                    {
                        new ScriptChoice { id = "more", label = "Tell me more.", next = "" },
                        new ScriptChoice { id = "noted", label = "Noted.", next = "", effect = "Effect_dlg_two" }
                    }
                }
            }
        }, null) };

        var one = new InterviewDay(new InterviewLines { menuCapacity = 1 }, null, twoChoices, Snap(1), new ShiftLedger(), null);
        CollectionAssert.IsEmpty(Offered(one), "two choices never fit a traveller wheel of one");
        Assert.AreEqual(1, one.ContentProblems.Count, string.Join(" | ", one.ContentProblems));
        StringAssert.StartsWith("Dialog 'dlg_two' is not offered: ", one.ContentProblems[0]);
        StringAssert.Contains("node 'start' offers 2 choices; the traveller wheel shows at most 1", one.ContentProblems[0]);

        var unlimited = new InterviewDay(new InterviewLines { menuCapacity = 0 }, null, twoChoices, Snap(1), new ShiftLedger(), null);
        CollectionAssert.AreEqual(new[] { "dlg_two" }, Offered(unlimited), "no capacity, no capacity problem");
        CollectionAssert.IsEmpty(unlimited.ContentProblems);
    }

    [Test]
    public void OfferedDialogs_DropsADialogCompletedThisShift()
    {
        var ledger = new ShiftLedger();
        InterviewDay day = DayOf(Snap(2), ledger);
        Assert.IsTrue(day.Complete("dlg_chat", ""));
        CollectionAssert.AreEqual(new[] { "dlg_rumour" }, Offered(day), "any completed dialog leaves the hub for the rest of the shift");
    }

    [Test]
    public void Complete_RecordsTheOutcome_AndRefusesAnUnknownOrRepeatedId()
    {
        var ledger = new ShiftLedger();
        InterviewDay day = DayOf(Snap(2), ledger);

        Assert.IsTrue(day.Complete("dlg_rumour", "Effect_dlg_rumour"));
        Assert.IsTrue(day.Complete("dlg_chat", ""));
        Assert.IsFalse(day.Complete("dlg_rumour", "Effect_dlg_rumour"), "already completed this shift");
        Assert.IsFalse(day.Complete("dlg_rumour_followup", ""), "not offered today");
        Assert.IsFalse(day.Complete("missing", ""));

        Assert.AreEqual(2, ledger.dialogOutcomes.Count);
        Assert.AreEqual("dlg_rumour", ledger.dialogOutcomes[0].dialogId);
        Assert.AreEqual("Effect_dlg_rumour", ledger.dialogOutcomes[0].effectName);
        Assert.IsTrue(ledger.dialogOutcomes[0].oneShot);
        Assert.IsFalse(ledger.dialogOutcomes[1].oneShot);
    }

    [Test]
    public void FlagsToSet_OneDoneFlagPerOneShotDialog_InLedgerOrder()
    {
        var outcomes = new List<DialogOutcome>
        {
            new DialogOutcome { dialogId = "b", effectName = "", oneShot = true },
            new DialogOutcome { dialogId = "chat", effectName = "", oneShot = false },
            new DialogOutcome { dialogId = "a", effectName = "Effect_A", oneShot = true },
            new DialogOutcome { dialogId = "b", effectName = "Effect_B", oneShot = true }
        };
        CollectionAssert.AreEqual(new[] { "dlg:b:done", "dlg:a:done" }, DialogOutcomes.FlagsToSet(outcomes));
    }

    [Test]
    public void EffectsToApply_EachOutcomeWithAnEffect_OncePerDialog_InLedgerOrder()
    {
        var outcomes = new List<DialogOutcome>
        {
            new DialogOutcome { dialogId = "b", effectName = "", oneShot = true },
            new DialogOutcome { dialogId = "a", effectName = "Effect_A", oneShot = true },
            new DialogOutcome { dialogId = "b", effectName = "Effect_B", oneShot = true },
            new DialogOutcome { dialogId = "a", effectName = "Effect_A2", oneShot = true }
        };
        CollectionAssert.AreEqual(new[] { "Effect_A", "Effect_B" }, DialogOutcomes.EffectsToApply(outcomes).Select(o => o.effectName).ToArray());
    }

    [Test]
    public void NullInputs_AreEmpty()
    {
        CollectionAssert.IsEmpty(DialogOutcomes.FlagsToSet(null));
        CollectionAssert.IsEmpty(DialogOutcomes.EffectsToApply(null));

        var day = new InterviewDay(null, null, null, null, null, null);
        CollectionAssert.IsEmpty(day.Questions);
        CollectionAssert.IsEmpty(day.OfferedDialogs(null));
        Assert.IsNotNull(day.Lines);
        Assert.IsFalse(day.Complete("x", ""));
    }

    [Test]
    public void APremadeBoundDialog_IsOfferedOnlyWhileItsPremadeIsAtTheDesk_AndUnboundOnesForAnyone()
    {
        var dialogs = new List<Gated<AuthoredDialog>>
        {
            new Gated<AuthoredDialog>(Dialog("dlg_chat", false), null),
            new Gated<AuthoredDialog>(Dialog("dlg_senenmut", true), null),
            new Gated<AuthoredDialog>(Dialog("dlg_socrates", true), null)
        };
        var day = new InterviewDay(new InterviewLines { menuCapacity = 8 }, null, dialogs, Snap(1), new ShiftLedger(), new[] { "dlg_senenmut", "dlg_socrates", "" });

        CollectionAssert.AreEqual(new[] { "dlg_chat" }, Offered(day), "an ordinary traveller");
        CollectionAssert.AreEqual(new[] { "dlg_chat", "dlg_senenmut" }, Offered(day, "dlg_senenmut"));
        CollectionAssert.AreEqual(new[] { "dlg_chat", "dlg_socrates" }, Offered(day, "dlg_socrates"));
        CollectionAssert.AreEqual(new[] { "dlg_chat" }, Offered(day, "dlg_unknown"), "a premade without a dialog of today");
    }
}
