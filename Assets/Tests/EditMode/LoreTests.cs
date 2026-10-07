using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The scanner app spec §3, the citizen file: a premade's authored lines; a
/// random traveller's flavour drawn from the templates they fit, seeded so a
/// replay prints the same file; a clue only from the case's real fault; the
/// threads a recurring traveller's file grows; every template slot checked.
/// </summary>
public class LoreTests
{
    private static LoreContent Content() => new LoreContent
    {
        clueChance = 1f,
        linesMin = 2,
        linesMax = 3,
        yearMin = 2140,
        yearMax = 2149,
        relatives = new List<string> { "sister", "cousin" },
        premades = new List<LorePremade>
        {
            new LorePremade { premade = "newton", lines = new List<string> { "Registry note: laws of motion.", "Registry note: Master of the Mint." } }
        },
        templates = new List<LoreTemplate>
        {
            new LoreTemplate { id = "any1", text = "{first} keeps a {relative}'s photo, {year}." },
            new LoreTemplate { id = "any2", text = "Sector {sector} resident until {month}." },
            new LoreTemplate { id = "any3", text = "Declares: 'research', {count} times." },
            new LoreTemplate { id = "lab1", kinds = new List<TravellerKind> { TravellerKind.Labourer }, text = "Contracted to {employer} at {wage}." },
            new LoreTemplate { id = "debt1", kinds = new List<TravellerKind> { TravellerKind.PoorTourist, TravellerKind.Labourer }, traits = new List<string> { "debt" }, text = "Owes {debt}." },
            new LoreTemplate { id = "dis1", kinds = new List<TravellerKind> { TravellerKind.Displaced }, text = "Found {found} after {incident}." },
            new LoreTemplate { id = "glum1", personality = "glum", text = "Expects the worst." },
            new LoreTemplate { id = "clueFrozen", kinds = new List<TravellerKind> { TravellerKind.PoorTourist, TravellerKind.Labourer }, clue = "FrozenAccount", text = "Collections notice served: account {status}, frozen." },
            new LoreTemplate { id = "clueForged", clue = "ForgedContract", text = "Contract on file differs from the one carried before." }
        },
        threads = new List<LoreThread>
        {
            new LoreThread { id = "thDenied", verdict = "Denied", text = "Denied here on {date} ({days} days ago)." },
            new LoreThread { id = "thAny", verdict = "", text = "Seen here on {date}: {verdict}." }
        }
    };

    private static LoreSubject Labourer(DirectiveFault fault = DirectiveFault.None, LieKind? lie = null) => new LoreSubject
    {
        Kind = TravellerKind.Labourer,
        Name = "Ines Varga",
        Place = "Victorian London (Industrial)",
        Era = "Industrial",
        Role = "Weaver",
        Debt = 88200,
        Status = "Standard",
        Frozen = fault == DirectiveFault.FrozenAccount,
        Employer = "Tyburn Mills",
        Wage = "420 cr",
        Term = "180 days",
        Personality = "curt",
        Fault = fault,
        Lie = lie
    };

    [Test]
    public void Premade_PrintsItsAuthoredLines_AsWritten()
    {
        var subject = new LoreSubject { Kind = TravellerKind.Displaced, Premade = "newton", Name = "Isaac Newton" };
        CollectionAssert.AreEqual(new[] { "Registry note: laws of motion.", "Registry note: Master of the Mint." },
                                  CitizenFile.Lines(Content(), subject, 5, null, 3));
        Assert.IsEmpty(CitizenFile.Lines(Content(), new LoreSubject { Premade = "nobody" }, 5, null, 3), "a premade without lines draws no random ones");
    }

    [Test]
    public void Random_IsDeterministic_PerSeed_AndTheCountWithinBounds()
    {
        LoreContent content = Content();
        content.clueChance = 0f;
        var counts = new HashSet<int>();
        bool differ = false;
        List<string> first = null;
        for (int seed = 1; seed <= 200; seed++)
        {
            List<string> a = CitizenFile.Lines(content, Labourer(), Seeds.ForLore(seed), null, 9);
            List<string> b = CitizenFile.Lines(content, Labourer(), Seeds.ForLore(seed), null, 9);
            CollectionAssert.AreEqual(a, b, "the same seed, the same file");
            Assert.That(a.Count, Is.InRange(2, 3));
            Assert.AreEqual(a.Count, a.Distinct().Count(), "never the same template twice");
            counts.Add(a.Count);
            first ??= a;
            differ |= !a.SequenceEqual(first);
        }
        CollectionAssert.AreEquivalent(new[] { 2, 3 }, counts);
        Assert.IsTrue(differ, "different travellers, different files");
    }

    [Test]
    public void Random_OnlyTemplatesTheyFit_EverySlotFilled()
    {
        LoreContent content = Content();
        content.clueChance = 0f;
        var seen = new HashSet<string>();
        for (int seed = 1; seed <= 300; seed++)
            foreach (string line in CitizenFile.Lines(content, Labourer(), seed, null, 9))
            {
                Assert.IsFalse(line.Contains("{"), $"every slot filled: {line}");
                Assert.IsFalse(line.StartsWith("Found "), "a displaced template never describes a labourer");
                Assert.IsFalse(line.StartsWith("Expects"), "another personality's template never");
                seen.Add(line.Split(' ')[0]);
            }
        Assert.Contains("Contracted", seen.ToList(), "the labourer's own templates come up");
        Assert.Contains("Owes", seen.ToList(), "a debtor's template for a debtor");
        string owes = Enumerable.Range(1, 300).SelectMany(s => CitizenFile.Lines(content, Labourer(), s, null, 9)).First(l => l.StartsWith("Owes"));
        Assert.AreEqual("Owes 88,200 cr.", owes, "the debt slot reads the account");
    }

    [Test]
    public void Clue_OnlyForTheCasesRealFault_AndItAgreesWithTheData()
    {
        LoreContent content = Content();
        for (int seed = 1; seed <= 100; seed++)
        {
            List<string> honest = CitizenFile.Lines(content, Labourer(), seed, null, 9);
            Assert.IsFalse(honest.Any(l => l.StartsWith("Collections") || l.StartsWith("Contract on file")), "no fault, no clue");

            List<string> frozen = CitizenFile.Lines(content, Labourer(DirectiveFault.FrozenAccount), seed, null, 9);
            Assert.AreEqual(1, frozen.Count(l => l.StartsWith("Collections")), "a frozen account's clue at a chance of 1");
            Assert.AreEqual("Collections notice served: account Standard, frozen.", frozen.First(l => l.StartsWith("Collections")), "the clue reads the case's own status");
            Assert.IsFalse(frozen.Any(l => l.StartsWith("Contract on file")), "never another fault's clue");
            Assert.That(frozen.Count, Is.InRange(2, 3), "the clue replaces a flavour line");

            List<string> forged = CitizenFile.Lines(content, Labourer(lie: LieKind.ForgedContract), seed, null, 9);
            Assert.AreEqual(1, forged.Count(l => l.StartsWith("Contract on file")));
        }
        content.clueChance = 0f;
        Assert.IsFalse(CitizenFile.Lines(content, Labourer(DirectiveFault.FrozenAccount), 3, null, 9).Any(l => l.StartsWith("Collections")), "a chance of 0: no clue");
    }

    [Test]
    public void Threads_GrowALinePerEarlierVisit_TheLatestTwo_ByVerdict()
    {
        var visits = new List<VisitEntry>
        {
            new VisitEntry { record = "NHA-512", day = 3, date = "16 Mar 2150", verdict = "Accepted" },
            new VisitEntry { record = "NHA-512", day = 7, date = "20 Mar 2150", verdict = "Denied" },
            new VisitEntry { record = "NHA-512", day = 11, date = "24 Mar 2150", verdict = "Detained" },
            new VisitEntry { record = "NHA-512", day = 15, date = "28 Mar 2150", verdict = "Denied" }
        };
        var subject = new LoreSubject { Kind = TravellerKind.Displaced, Premade = "newton", Name = "Isaac Newton" };
        List<string> lines = CitizenFile.Lines(Content(), subject, 5, visits, 14);
        CollectionAssert.AreEqual(new[]
        {
            "Registry note: laws of motion.", "Registry note: Master of the Mint.",
            "Denied here on 20 Mar 2150 (7 days ago).", "Seen here on 24 Mar 2150: detained."
        }, lines, "the latest two earlier visits (never today's or later), oldest first; a verdict without its own line takes the blank one");

        List<string> before = CitizenFile.Lines(Content(), Labourer(), 9, visits.Take(1).ToList(), 5);
        List<string> after = CitizenFile.Lines(Content(), Labourer(), 9, visits.Take(2).ToList(), 9);
        CollectionAssert.AreEqual(before, after.Take(before.Count).ToList(), "a returning traveller's file comes back the same and grows");
        Assert.AreEqual(before.Count + 1, after.Count);
    }

    [Test]
    public void Split_ALinesOwnShortLabel_ElseTheFallback()
    {
        Assert.AreEqual(("Registry note", "published the Principia, 1687: the laws of motion."), CitizenFile.Split("Registry note: published the Principia, 1687: the laws of motion.", "Note"));
        Assert.AreEqual(("Note", "Archive sources claim he moved house ninety times."), CitizenFile.Split("Archive sources claim he moved house ninety times.", "Note"));
        Assert.AreEqual(("Note", "Answered the advert CLEAR YOUR DEBT: 180 days in the mills."), CitizenFile.Split("Answered the advert CLEAR YOUR DEBT: 180 days in the mills.", "Note"), "a colon far in is no label");
    }

    [Test]
    public void Groups_TheFile_ThenSeenBeforeWithItsFlag_NewestFirst_NothingEvidence()
    {
        var visits = new List<VisitEntry>
        {
            new VisitEntry { record = "NHA-512", day = 7, date = "20 Mar 2150", place = "Periclean Athens (Ancient)", verdict = "Denied", citation = "Denied without logged evidence." },
            new VisitEntry { record = "NHA-512", day = 11, date = "24 Mar 2150", place = "Periclean Athens (Ancient)", verdict = "Detained" },
            new VisitEntry { record = "NHA-512", day = 15, date = "28 Mar 2150", place = "Periclean Athens (Ancient)", verdict = "Accepted" }
        };
        string Text(string key)
        {
            switch (key)
            {
                case "records.group.file": return "FILE";
                case "records.row.file": return "Note";
                case "records.group.seen": return "SEEN BEFORE · {0}";
                case "visit.flag.days": return "{0} {1} DAYS AGO";
                case "visit.flag.yesterday": return "{0} YESTERDAY";
                case "visit.verdict.Denied": return "Denied";
                case "visit.row": return "{0} · {1}";
                case "visit.citation": return "· Citation: {0}";
                default: return key;
            }
        }
        List<RecordGroup> groups = CitizenFile.Groups(new[] { "Clerk note: honeymoon booked for two.", "Saved for six years." }, visits, 14, Text);
        Assert.AreEqual(2, groups.Count);
        Assert.AreEqual("FILE", groups[0].Title);
        Assert.AreEqual("Clerk note", groups[0].Rows[0].Label);
        Assert.AreEqual("Note", groups[0].Rows[1].Label);
        Assert.AreEqual("SEEN BEFORE · DETAINED 3 DAYS AGO", groups[1].Title, "the latest earlier verdict; a verdict word without a string reads as itself");
        CollectionAssert.AreEqual(new[] { "24 Mar 2150", "20 Mar 2150" }, groups[1].Rows.Select(r => r.Label).ToArray(), "newest first, never today's or later");
        Assert.AreEqual("Periclean Athens (Ancient) · Denied · Citation: Denied without logged evidence.", groups[1].Rows[1].Value);
        Assert.IsFalse(groups.SelectMany(g => g.Rows).Any(r => r.IsEvidence), "the file and the history are read, never picked");

        Assert.AreEqual(1, CitizenFile.Groups(new[] { "One." }, visits, 7, Text).Count, "a first visit: no SEEN BEFORE");
        Assert.IsEmpty(CitizenFile.Groups(null, null, 7, Text));
    }

    [Test]
    public void Problems_EveryTemplateSlotChecked()
    {
        Assert.IsEmpty(CitizenFile.Problems(Content(), new[] { "newton" }, new[] { "glum", "curt" }, true));

        LoreContent bad = Content();
        bad.templates.Add(new LoreTemplate { id = "x1", text = "Owes {debt}." });
        bad.templates.Add(new LoreTemplate { id = "x2", kinds = new List<TravellerKind> { TravellerKind.RichTourist }, text = "Works at {employer}." });
        bad.templates.Add(new LoreTemplate { id = "x3", text = "Has {wings}." });
        bad.templates.Add(new LoreTemplate { id = "x4", kinds = new List<TravellerKind> { TravellerKind.Displaced, TravellerKind.Labourer }, text = "{status}" });
        bad.templates.Add(new LoreTemplate { id = "x5", traits = new List<string> { "rich" }, clue = "Nope", personality = "jolly", text = "Fine." });
        bad.templates.Add(new LoreTemplate { id = "x5", text = "Twice." });
        bad.threads.Add(new LoreThread { id = "t9", verdict = "Denied", text = "On {date} owed {debt}." });
        bad.premades.Add(new LorePremade { premade = "ghost", lines = new List<string> { "One {name}." } });
        List<string> problems = CitizenFile.Problems(bad, new[] { "newton", "darwin" }, new[] { "glum" }, true);

        foreach (string expected in new[] { "'x1': slot {debt}", "'x2': slot {employer}", "'x3': slot {wings}", "'x4': slot {status}", "trait 'rich'", "clue 'Nope'",
                                            "personality 'jolly'", "'x5': a blank or repeated id", "'t9': slot {debt}", "'ghost' is no premade", "'ghost' has 1 lines",
                                            "has a {slot}", "premade 'darwin' has no file lines" })
            Assert.IsTrue(problems.Any(p => p.Contains(expected)), $"expected a problem with \"{expected}\" in:\n{string.Join("\n", problems)}");
    }

    [Test]
    public void Problems_LengthAsciiAndBounds()
    {
        LoreContent bad = Content();
        bad.templates.Add(new LoreTemplate { id = "long", text = new string('a', LoreContent.MaxLineLength + 1) });
        bad.templates.Add(new LoreTemplate { id = "uni", text = "Café." });
        bad.linesMax = 6;
        bad.clueChance = 2f;
        bad.relatives.Clear();
        List<string> problems = CitizenFile.Problems(bad, new[] { "newton" }, new string[0], false);
        foreach (string expected in new[] { "longer than", "not plain ASCII", "linesMax 6", "clueChance 2", "relatives is empty" })
            Assert.IsTrue(problems.Any(p => p.Contains(expected)), $"expected \"{expected}\" in:\n{string.Join("\n", problems)}");
    }
}
