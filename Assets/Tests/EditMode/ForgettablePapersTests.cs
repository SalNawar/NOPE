using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The forgettable paper (Saleh 2026-10-17: "travelers never drop the travel
/// ticket ... they should forget to give it to you some time"): the Entry
/// Ticket comes with the arrival papers unless the traveller forgets it (one
/// seeded draw against their personality's forgetPaper), and the desk asks for
/// it only then. DocumentHandOvers, Seeds.ForForgetting, FormRequests.Build,
/// MissingPapers and CaseDocuments.ArrivalIndices.
/// </summary>
public class ForgettablePapersTests
{
    [TestCase(DocumentHandOver.OnRequest, false, DocumentHandOver.OnRequest)]
    [TestCase(DocumentHandOver.OnRequest, true, DocumentHandOver.OnRequest)]
    [TestCase(DocumentHandOver.OnArrival, false, DocumentHandOver.OnArrival)]
    [TestCase(DocumentHandOver.OnArrival, true, DocumentHandOver.OnArrival)]
    [TestCase(DocumentHandOver.Forgettable, false, DocumentHandOver.OnArrival)]
    [TestCase(DocumentHandOver.Forgettable, true, DocumentHandOver.OnRequest)]
    public void ForTraveller_AForgettablePaper_ComesOnArrival_UnlessForgotten(DocumentHandOver template, bool forgot, DocumentHandOver expected)
    {
        Assert.AreEqual(expected, DocumentHandOvers.ForTraveller(template, forgot));
    }

    [Test]
    public void AForgettablePaper_IsOnThePapersMenu_LikeAPaperOnRequest()
    {
        Assert.IsTrue(DocumentHandOvers.IsRequested(DocumentHandOver.OnRequest));
        Assert.IsTrue(DocumentHandOvers.IsRequested(DocumentHandOver.Forgettable), "any traveller may forget it, so the desk can ask for it");
        Assert.IsFalse(DocumentHandOvers.IsRequested(DocumentHandOver.OnArrival));
    }

    [Test]
    public void Forgets_ARollUnderTheChance()
    {
        Assert.IsTrue(DocumentHandOvers.Forgets(0.25f, new FixedValue(0.2f)));
        Assert.IsFalse(DocumentHandOvers.Forgets(0.25f, new FixedValue(0.25f)));
        Assert.IsFalse(DocumentHandOvers.Forgets(0f, new FixedValue(0f)), "a chance of 0 never forgets");
        Assert.IsTrue(DocumentHandOvers.Forgets(1f, new FixedValue(0.999f)), "a chance of 1 always forgets");
        Assert.IsFalse(DocumentHandOvers.Forgets(0.5f, null), "no stream: never");
    }

    [Test]
    public void Forgets_DrawsOnce_WhateverTheChance()
    {
        var rng = new Counting();
        DocumentHandOvers.Forgets(0f, rng);
        DocumentHandOvers.Forgets(1f, rng);
        Assert.AreEqual(2, rng.Draws, "one draw per traveller, so the chance never moves a later draw");
    }

    /// <summary>Over many travellers on their own streams, about the chance forget: the rate the content's ~0.22 average gives.</summary>
    [Test]
    public void OverManyTravellers_TheRateIsTheChance()
    {
        int forgot = 0;
        const int travellers = 4000;
        for (int day = 1; day <= 20; day++)
        {
            int daySeed = Seeds.Day(12345, day);
            for (int c = 1; c <= travellers / 20; c++)
                if (DocumentHandOvers.Forgets(0.22f, new SeededRandom(Seeds.ForForgetting(Seeds.ForCase(daySeed, c)))))
                    forgot++;
        }
        Assert.That(forgot / (float)travellers, Is.InRange(0.19f, 0.25f));
    }

    private static CaseDocument Doc(string form, DocumentHandOver handOver) =>
        new CaseDocument { name = form, formNumber = form, handOver = handOver };

    private static readonly AskableForm[] Menu = { new AskableForm("TC-230", "Entry Ticket", null, true) };

    [Test]
    public void ARememberedTicket_ArrivesWithThePassport_AndItsRequestFindsItOnTheDesk()
    {
        var documents = new[] { Doc("TC-101", DocumentHandOver.OnArrival), Doc("TC-230", DocumentHandOvers.ForTraveller(DocumentHandOver.Forgettable, false)) };
        CollectionAssert.AreEqual(new[] { 0, 1 }, CaseDocuments.ArrivalIndices(documents));

        List<FormRequest> requests = FormRequests.Build(Menu, documents, null);
        Assert.AreEqual(1, requests.Count);
        Assert.AreEqual(1, requests[0].Document, "carried: the request matches the ticket on the desk, never 'not carried'");

        var papers = new CasePapers(documents.Length);
        papers.HandOver(0);
        papers.HandOver(1);
        CollectionAssert.IsEmpty(new MissingPapers(Menu, requests).Open(papers), "nothing to flag missing");
    }

    [Test]
    public void AForgottenTicket_StaysWithTheTraveller_UntilTheDeskAsks()
    {
        var documents = new[] { Doc("TC-101", DocumentHandOver.OnArrival), Doc("TC-230", DocumentHandOvers.ForTraveller(DocumentHandOver.Forgettable, true)) };
        CollectionAssert.AreEqual(new[] { 0 }, CaseDocuments.ArrivalIndices(documents));

        List<FormRequest> requests = FormRequests.Build(Menu, documents, null);
        Assert.AreEqual(1, requests[0].Document);

        var papers = new CasePapers(documents.Length);
        papers.HandOver(0);
        List<FormRequest> open = new MissingPapers(Menu, requests).Open(papers);
        Assert.AreEqual(1, open.Count, "the ticket can be flagged missing and asked for");
        Assert.AreEqual("TC-230", open[0].Id);
    }

    [Test]
    public void APersonalitysForgetChance_IsAChance()
    {
        var p = new Personality { id = "chatty", name = "Chatty", weight = 1f, forgetPaper = 1.5f };
        StringAssert.Contains("personalities: 'chatty' has a forgetPaper chance of 1.5; it is a chance from 0 to 1.",
                              string.Join("\n", Personalities.Problems(new List<Personality> { p })));
        p.forgetPaper = 0.3f;
        CollectionAssert.IsEmpty(Personalities.Problems(new List<Personality> { p }));
    }

    private sealed class FixedValue : IRandomSource
    {
        private readonly float _value;
        public FixedValue(float value) => _value = value;
        public int Range(int minInclusive, int maxExclusive) => minInclusive;
        public float Value() => _value;
    }

    private sealed class Counting : IRandomSource
    {
        public int Draws;
        public int Range(int minInclusive, int maxExclusive) { Draws++; return minInclusive; }
        public float Value() { Draws++; return 0.5f; }
    }
}
