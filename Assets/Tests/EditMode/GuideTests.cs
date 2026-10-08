using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>The FTUE and the daily guide (Saleh 2026-10-06): pages grow with the ramp, the day's moment, the practice, the FTUE's steps, skip, replay and the shift's end.</summary>
public class GuideTests
{
    private static Introductions Ramp() => new Introductions(new (int, IEnumerable<string>)[]
    {
        (1, Introductions.DayKeys(new[] { "TC-101" }, new[] { "Rule_PaperDates" }, null, null, new[] { Feature.Calendar, Feature.Rulebook })),
        (2, Introductions.DayKeys(new[] { "TC-101", "TC-230" }, null, null, null, null)),
        (3, Introductions.DayKeys(null, null, null, null, new[] { Feature.Board })),
        (8, Introductions.DayKeys(new[] { "TC-520" }, null, null, null, null)),
    });

    private static GuideStep Step(string id, GuideAction action, string target = "sign", params ClueCategory[] categories) =>
        new GuideStep { id = id, action = action, target = target, text = "Do " + id, categories = categories.ToList() };

    private static GuidePage Page(string id, string feature, GuideStep practice = null) => new GuidePage
    {
        id = id, feature = feature, title = id.ToUpperInvariant(), check = "c", against = "a", fault = "f", point = "rulebook",
        practice = practice ?? new GuideStep()
    };

    private static GuideContent Content() => new GuideContent
    {
        guidedThroughDay = 7,
        basicsTitle = "BASICS",
        basics = new List<string> { "Press {inspect} to inspect, {stamps} for the stamps, {pc} for the PC; {back} backs out." },
        pages = new List<GuidePage>
        {
            Page("ticket", Feature.Paper("TC-230"), Step("p", GuideAction.Compare, "field:TC-230/CitizenId", ClueCategory.CitizenId)),
            Page("passport", Feature.Paper("TC-101")),
            Page("dates", Feature.Calendar),
            Page("board", Feature.Board, Step("b", GuideAction.Compare, "board", ClueCategory.Destination)),
            Page("permit", Feature.Paper("TC-520")),
        },
        ftue = new List<GuideStep>
        {
            Step("call", GuideAction.Call),
            Step("desk", GuideAction.OnDesk, "paper:TC-101"),
            Step("dest", GuideAction.Compare, "rulebook", ClueCategory.Destination),
            Step("back", GuideAction.HandBack, "counter"),
        },
        ftueDone = "Done."
    };

    [Test]
    public void PagesOn_GrowWithTheRamp_ByDayThenAuthoredOrder()
    {
        var guide = new Guide(Content(), Ramp());
        CollectionAssert.AreEqual(new[] { "passport", "dates" }, guide.PagesOn(1).Select(p => p.id).ToArray());
        CollectionAssert.AreEqual(new[] { "passport", "dates", "ticket" }, guide.PagesOn(2).Select(p => p.id).ToArray(), "the ticket comes after day 1's pages though authored first");
        CollectionAssert.AreEqual(new[] { "passport", "dates", "ticket", "board" }, guide.PagesOn(7).Select(p => p.id).ToArray());
        Assert.AreEqual(5, guide.PagesOn(8).Count);
        CollectionAssert.AreEqual(new[] { "board" }, guide.NewOn(3).Select(p => p.id).ToArray());
        Assert.IsEmpty(guide.NewOn(4));
    }

    [Test]
    public void Moment_FtueOnDay1_NewRuleThroughTheGuidedDays_ThenOnlyTheBadge()
    {
        var guide = new Guide(Content(), Ramp());
        Assert.AreEqual(GuideMoment.Ftue, guide.Moment(1));
        Assert.AreEqual(GuideMoment.NewRule, guide.Moment(2));
        Assert.AreEqual("ticket", guide.MomentPage(2).id);
        Assert.AreEqual(GuideMoment.None, guide.Moment(4), "nothing new");
        Assert.IsNull(guide.MomentPage(4));
        Assert.AreEqual(GuideMoment.Badge, guide.Moment(8), "after the guided week only the badge");
        Assert.IsNull(guide.MomentPage(8));
    }

    [Test]
    public void Badge_UntilTodaysNewPagesAreOpened()
    {
        var guide = new Guide(Content(), Ramp());
        var state = new GuideState();
        Assert.IsTrue(guide.Badge(8, state));
        Guide.Read(state, "permit");
        Guide.Read(state, "permit");
        Assert.IsFalse(guide.Badge(8, state));
        Assert.AreEqual(1, state.pagesRead.Count, "read once");
        Assert.IsFalse(guide.Badge(4, state), "no new page, no badge");
    }

    [Test]
    public void Practice_OnTheFirstCarrier_CompletesOnItsDetailOnly_Once()
    {
        var guide = new Guide(Content(), Ramp());
        var state = new GuideState();
        GuidePage practice = guide.Practice(2, state);
        Assert.AreEqual("ticket", practice.id);
        Assert.IsFalse(Guide.Carries(practice, new[] { "TC-101" }), "a passport-only traveller does not carry the ticket");
        Assert.IsTrue(Guide.Carries(practice, new[] { "TC-101", "TC-230" }));
        Assert.IsTrue(Guide.Carries(guide.Practice(3, state), new[] { "TC-101" }), "a tool's page: every traveller");
        Assert.IsFalse(Guide.Practise(state, practice, new GuideEvent(GuideAction.Compare, ClueCategory.Expiry)), "another detail");
        Assert.IsTrue(Guide.Practise(state, practice, new GuideEvent(GuideAction.Compare, ClueCategory.CitizenId)));
        Assert.IsFalse(Guide.Practise(state, practice, new GuideEvent(GuideAction.Compare, ClueCategory.CitizenId)), "once");
        Assert.IsNull(guide.Practice(2, state), "practised");
        Assert.IsNull(guide.Practice(8, state), "no practice after the guided days");
        Assert.IsNull(guide.Practice(1, state), "day 1 has the FTUE");
    }

    [Test]
    public void Ftue_StepsCompleteByTheirActions_ALaterOneEarly_ThenOver()
    {
        var guide = new Guide(Content(), Ramp());
        var state = new GuideState();
        Assert.IsTrue(Guide.FtueOpen(state));
        Assert.AreEqual("call", guide.CurrentStep(state).id);
        Assert.AreEqual(1, guide.StepNumber(state));
        Assert.IsFalse(guide.Record(state, new GuideEvent(GuideAction.Inspect)), "no step waits for it");
        Assert.IsTrue(guide.Record(state, new GuideEvent(GuideAction.Call)));
        Assert.AreEqual("desk", guide.CurrentStep(state).id);
        Assert.IsTrue(guide.Record(state, new GuideEvent(GuideAction.HandBack)), "a later step done early");
        Assert.AreEqual("desk", guide.CurrentStep(state).id, "the prompt still asks the first open step");
        Assert.IsTrue(guide.Record(state, new GuideEvent(GuideAction.OnDesk, form: "TC-230")));
        Assert.IsFalse(guide.Record(state, new GuideEvent(GuideAction.Compare, ClueCategory.Expiry)), "not the destination");
        Assert.IsTrue(guide.Record(state, new GuideEvent(GuideAction.Compare, ClueCategory.Destination)));
        Assert.IsNull(guide.CurrentStep(state));
        Assert.IsTrue(state.ftueDone);
        Assert.IsFalse(Guide.FtueOpen(state));
        CollectionAssert.AreEqual(new[] { "call", "back", "desk", "dest" }, state.ftueSteps);
    }

    [Test]
    public void Ftue_Skip_Replay_AndTheShiftsEndClosesIt()
    {
        var guide = new Guide(Content(), Ramp());
        var state = new GuideState();
        guide.Record(state, new GuideEvent(GuideAction.Call));
        Guide.Skip(state);
        Assert.IsNull(guide.CurrentStep(state));
        Assert.IsFalse(guide.Record(state, new GuideEvent(GuideAction.OnDesk)), "skipped: nothing recorded");
        Guide.Replay(state);
        Assert.AreEqual("call", guide.CurrentStep(state).id, "from the first step again");
        Guide.CloseShift(state);
        Assert.IsTrue(state.ftueDone, "it never runs into the next day by itself");
        Assert.IsFalse(state.ftueSkipped);
    }

    [Test]
    public void Step_MatchesItsActionDetailsAndForm()
    {
        GuideStep any = Step("a", GuideAction.Compare);
        Assert.IsTrue(any.Matches(new GuideEvent(GuideAction.Compare)), "no details named: any comparison");
        GuideStep two = Step("b", GuideAction.Compare, "board", ClueCategory.AccountStatus, ClueCategory.TransponderClass);
        Assert.IsTrue(two.Matches(new GuideEvent(GuideAction.Compare, ClueCategory.TransponderClass)));
        Assert.IsFalse(two.Matches(new GuideEvent(GuideAction.Compare)), "a comparison about no detail");
        GuideStep waiver = new GuideStep { id = "w", action = GuideAction.OnDesk, form = "TC-310", target = "counter", text = "x" };
        Assert.IsFalse(waiver.Matches(new GuideEvent(GuideAction.OnDesk, form: "TC-101")));
        Assert.IsTrue(waiver.Matches(new GuideEvent(GuideAction.OnDesk, form: "TC-310")));
        Assert.IsFalse(new GuideStep().Matches(new GuideEvent(GuideAction.None)), "an unset step waits for nothing");
    }

    [Test]
    public void Problems_None_ForSoundContent_AndNamesEachFault()
    {
        GuideContent good = Content();
        CollectionAssert.IsEmpty(good.Problems(Ramp(), 3));

        GuideContent bad = Content();
        bad.pages.Add(Page("ghost", Feature.Paper("TC-999")));
        bad.pages.Add(Page("ticket", Feature.Paper("TC-230")));
        bad.pages[0].point = "nowhere";
        bad.ftue.Add(new GuideStep { id = "idle", target = "sign", text = "x" });
        bad.ftue[0].target = "field:TC-101/NotACategory";
        bad.basics.Clear();
        List<string> problems = bad.Problems(Ramp(), 4);
        Assert.IsTrue(problems.Any(p => p.Contains("'ghost'") && p.Contains("no day introduces")));
        Assert.IsTrue(problems.Any(p => p.Contains("'ticket' is a blank or repeated id")));
        Assert.IsTrue(problems.Any(p => p.Contains("point 'nowhere'")));
        Assert.IsTrue(problems.Any(p => p.Contains("'idle' waits for no action")));
        Assert.IsTrue(problems.Any(p => p.Contains("target 'field:TC-101/NotACategory'")));
        Assert.IsTrue(problems.Any(p => p.Contains("BASICS")));
        Assert.IsTrue(problems.Any(p => p.StartsWith("Day 4 has no guide page")));
    }

    [Test]
    public void BulletinProblems_EachDaysBulletinNamesItsNewPages_AnyCase()
    {
        GuideContent content = Content();
        foreach (GuidePage page in content.pages)
            page.named = page.id == "board" ? "Departure Board" : page.title;
        var good = new[] { (1, "NEW: your desk. THE PASSPORT and DATES."), (2, "NEW: the entry ticket."), (3, "NEW: the Departure Board."), (8, "NEW: the permit.") };
        CollectionAssert.IsEmpty(content.BulletinProblems(Ramp(), good));

        var stale = new[] { (1, "the passport and dates"), (2, "NEW: debt standing."), (3, "NEW: the Departure Board."), (8, "NEW: the permit.") };
        List<string> problems = content.BulletinProblems(Ramp(), stale);
        Assert.AreEqual(1, problems.Count, string.Join(" | ", problems));
        StringAssert.StartsWith("Day 2's bulletin does not name 'TICKET'", problems[0]);

        content.pages[0].named = " ";
        Assert.IsTrue(content.BulletinProblems(Ramp(), good).Any(p => p.Contains("'ticket'") && p.Contains("blank")), "a page naming nothing");
    }

    [Test]
    public void Targets_NamedPapersAndFields()
    {
        Assert.IsTrue(GuideTargets.IsKnown("sign"));
        Assert.IsTrue(GuideTargets.IsKnown("paper:TC-230"));
        Assert.IsTrue(GuideTargets.TryField("field:TC-230/CitizenId", out string form, out ClueCategory category));
        Assert.AreEqual("TC-230", form);
        Assert.AreEqual(ClueCategory.CitizenId, category);
        Assert.IsTrue(GuideTargets.TryForm("paper:TC-101", out form));
        Assert.AreEqual("TC-101", form);
        Assert.IsFalse(GuideTargets.TryForm("rulebook", out _));
        Assert.IsFalse(GuideTargets.IsKnown("field:TC-230/3"), "a number is no category");
        Assert.IsFalse(GuideTargets.IsKnown("paper:"));
        Assert.IsFalse(GuideTargets.IsKnown(""));
    }

    [Test]
    public void Text_KeysComeFromTheControls()
    {
        Assert.AreEqual($"Press {ControlRules.InspectKey}, {ControlRules.StampsKey}, {ControlRules.PcKey}; {ControlRules.BackKeys}.",
                        GuideText.Keys("Press {inspect}, {stamps}, {pc}; {back}."));
        Assert.AreEqual(string.Empty, GuideText.Keys(null));
    }

    [Test]
    public void CompareEnds_PointAtTheValueFirst_ThenWhatItIsHeldAgainst()
    {
        // Saleh's 1008a playtest: the arrows pointed at the folder, not at the Passport's value.
        Assert.AreEqual(("field:TC-101/Destination", "rulebook"), GuideTargets.CompareEnds(Step("d", GuideAction.Compare, "rulebook", ClueCategory.Destination)));
        Assert.AreEqual(("field:TC-101/Expiry", "calendar"), GuideTargets.CompareEnds(Step("e", GuideAction.Compare, "calendar", ClueCategory.Expiry)));
        Assert.AreEqual(("field:TC-230/CitizenId", "field:TC-101/CitizenId"), GuideTargets.CompareEnds(Step("t", GuideAction.Compare, "field:TC-230/CitizenId", ClueCategory.CitizenId)));
        Assert.AreEqual(("field:TC-240/TransponderClass", "rulebook"), GuideTargets.CompareEnds(Step("c", GuideAction.Compare, "field:TC-240/TransponderClass", ClueCategory.AccountStatus, ClueCategory.TransponderClass)));
        Assert.AreEqual(("stamps", "stamps"), GuideTargets.CompareEnds(Step("s", GuideAction.StampsOut, "stamps")));
        Assert.IsTrue(GuideTargets.IsKnown(GuideTargets.CompareEnds(Step("d", GuideAction.Compare, "rulebook", ClueCategory.Destination)).pick));
    }
}
