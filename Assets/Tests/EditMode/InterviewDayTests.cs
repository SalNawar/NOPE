using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// What the office offers on a day. Questions: Currency (ungated), Capital
/// (DayAtLeast 2), Ruler (DayAtLeast 3), Date of birth (UpgradeOwned
/// interview_protocols) and a Language question gated by the flag
/// "met_tesla"; the one wheel's questions (the personalities spec's §3.2:
/// one per category, asked of every traveller) in OneWheelQuestions. Dialogs: the rumour (DayAtLeast 2, one-shot), its follow-up
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

    /// <summary>The one wheel's questions in library order (the personalities spec's §3.2): Currency and Device from day 4, Language, Capital and Ruler from day 5, Date of birth with Interview Protocols.</summary>
    private static List<Gated<InterviewQuestion>> OneWheelQuestions() => new List<Gated<InterviewQuestion>>
    {
        Question("q_currency", ClueCategory.Currency, Day(4)),
        Question("q_language", ClueCategory.Language, Day(5)),
        Question("q_device", ClueCategory.Technology, Day(4)),
        Question("q_capital", ClueCategory.Geography, Day(5)),
        Question("q_ruler", ClueCategory.Politics, Day(5)),
        Question("q_born", ClueCategory.BirthDate, new GateCondition(TriggerConditionType.UpgradeOwned, "interview_protocols", 0f))
    };

    private static InterviewDay OneWheelDay(GateSnapshot snapshot) =>
        new InterviewDay(new InterviewLines { menuCapacity = 8, backLabel = "< Back" }, OneWheelQuestions(), null, snapshot, new ShiftLedger(), null);

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
    public void Questions_AreTheDaysForEveryKind()
    {
        CollectionAssert.IsEmpty(OneWheelDay(Snap(3)).Questions, "no fact question before day 4");
        CollectionAssert.AreEqual(new[] { "q_currency", "q_device" }, OneWheelDay(Snap(4)).Questions.Select(q => q.id).ToArray());
        InterviewDay day5 = OneWheelDay(Snap(5));
        CollectionAssert.AreEqual(new[] { "q_currency", "q_language", "q_device", "q_capital", "q_ruler" }, day5.Questions.Select(q => q.id).ToArray());

        // One list for everyone: every kind's ask menu is the day's questions, in the same words and order.
        var lines = new InterviewLines { backLabel = "< Back", askLabel = "Ask about the trip >" };
        var menus = new List<string>();
        foreach (TravellerKind kind in (TravellerKind[])System.Enum.GetValues(typeof(TravellerKind)))
        {
            var c = new InterviewCase
            {
                kind = kind,
                claimPlace = "Periclean Athens (Ancient)",
                claimedEraId = "ancient",
                answers = day5.AskableCategories.Select(category => new InterviewAnswer { category = category, value = "v" }).ToList()
            };
            menus.Add(string.Join(" | ", InterviewScript.Build(lines, day5.Questions, null, c).Node(InterviewScript.AskNodeId).Choices.Select(x => $"{x.Id}:{x.Label}")));
        }
        Assert.AreEqual(1, menus.Distinct().Count(), string.Join("\n", menus));
    }

    [Test]
    public void AskableCategories_OnePerCategoryInLibraryOrder()
    {
        InterviewDay day = OneWheelDay(Snap(6, upgrades: new[] { "interview_protocols" }));

        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology, ClueCategory.Geography, ClueCategory.Politics, ClueCategory.BirthDate },
                                  day.AskableCategories);
        CollectionAssert.AllItemsAreUnique(day.AskableCategories, "one question per category: every traveller answers each once");
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Technology }, OneWheelDay(Snap(4)).AskableCategories);
    }

    [Test]
    public void AnswerTellCategories_AreTheDayGatedOnes()
    {
        InterviewDay day5 = OneWheelDay(Snap(5, upgrades: new[] { "interview_protocols" }));

        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology, ClueCategory.Geography, ClueCategory.Politics },
                                  day5.AnswerTellCategories, "the displaced's order of days 5-6 is kept (the personalities spec's T8); the upgrade's birth date is hint-only");
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Technology }, OneWheelDay(Snap(4)).AnswerTellCategories, "day 4: the smugglers' two");
        CollectionAssert.IsEmpty(OneWheelDay(Snap(3, upgrades: new[] { "interview_protocols" })).AnswerTellCategories, "before day 4 no question may carry a tell");
    }

    [Test]
    public void Problems_OneQuestionPerCategory()
    {
        List<InterviewQuestion> sound = OneWheelQuestions().Select(q => q.Item).ToList();
        CollectionAssert.IsEmpty(InterviewQuestions.Problems(sound));

        sound.Add(Question("q_trip_currency", ClueCategory.Currency).Item);
        List<string> problems = InterviewQuestions.Problems(sound);
        Assert.AreEqual(1, problems.Count, string.Join(" | ", problems));
        Assert.AreEqual("Question 'q_trip_currency' asks about Currency, as 'q_currency' does (one question per category: every traveller is asked each).", problems[0]);

        CollectionAssert.IsEmpty(InterviewQuestions.Problems(null));
        CollectionAssert.IsEmpty(InterviewQuestions.Problems(new List<InterviewQuestion> { null }));
    }

    [Test]
    public void Count_IsTheDaysQuestions()
    {
        List<InterviewQuestion> library = OneWheelQuestions().Select(q => q.Item).ToList();

        Assert.AreEqual(6, InterviewQuestions.Count(library), "every traveller is asked every question: the ask menu's worst case is the library's six");
        Assert.AreEqual(OneWheelDay(Snap(6, upgrades: new[] { "interview_protocols" })).Questions.Count, InterviewQuestions.Count(library));
        library.Insert(2, null);
        Assert.AreEqual(6, InterviewQuestions.Count(library), "an empty entry is no question");
        Assert.AreEqual(0, InterviewQuestions.Count(null));
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

    /// <summary>A forced slot's dialog (days 7-15 B7) is premade-bound like a premade's own: offered only while that appearance stands at the desk.</summary>
    [Test]
    public void OfferedDialogs_AForcedSlotsDialogOnlyWhileItsPremadeStands()
    {
        var dialogs = new List<Gated<AuthoredDialog>>
        {
            new Gated<AuthoredDialog>(Dialog("dlg_chat", false), null),
            new Gated<AuthoredDialog>(Dialog("dlg_pell_2", true), null)
        };
        var day = new InterviewDay(new InterviewLines { menuCapacity = 8 }, null, dialogs, Snap(10), new ShiftLedger(), new[] { "dlg_pell_2" });

        CollectionAssert.AreEqual(new[] { "dlg_chat" }, Offered(day), "an ordinary traveller in the slot");
        CollectionAssert.AreEqual(new[] { "dlg_chat", "dlg_pell_2" }, Offered(day, Premades.Voice("dlg_pell_2", string.Empty)), "Pell's appearance");
    }

    /// <summary>The appearance's dialog replaces the premade's own for that slot: the premade's is not offered beside it.</summary>
    [Test]
    public void OfferedDialogs_TheSlotsDialogReplacesThePremadesOwn()
    {
        var dialogs = new List<Gated<AuthoredDialog>>
        {
            new Gated<AuthoredDialog>(Dialog("dlg_auditor", true), null),
            new Gated<AuthoredDialog>(Dialog("dlg_auditor_found", true), null)
        };
        var day = new InterviewDay(new InterviewLines { menuCapacity = 8 }, null, dialogs, Snap(14), new ShiftLedger(), new[] { "dlg_auditor", "dlg_auditor_found" });

        CollectionAssert.AreEqual(new[] { "dlg_auditor_found" }, Offered(day, Premades.Voice("dlg_auditor_found", "dlg_auditor")));
        CollectionAssert.AreEqual(new[] { "dlg_auditor" }, Offered(day, Premades.Voice(string.Empty, "dlg_auditor")), "an appearance with no dialog of its own keeps the premade's");
    }
}
