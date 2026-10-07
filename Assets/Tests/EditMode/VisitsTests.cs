using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The scanner app spec §2.6: the run's visits to the desk, recorded at each
/// verdict under the record's identity; a record's earlier visits and the
/// recurring face's flag ("DENIED 3 DAYS AGO"); verdicts are words, so the
/// desk machine's Detained slots in.
/// </summary>
public class VisitsTests
{
    [Test]
    public void Key_TheNumberFirst_ElseTheName()
    {
        Assert.AreEqual("NHA-512", Visits.Key(" NHA-512 ", "Pell Quimby"));
        Assert.AreEqual("Pell Quimby", Visits.Key("", " Pell Quimby "));
        Assert.AreEqual(string.Empty, Visits.Key(null, null));
    }

    [Test]
    public void Record_KeepsVerdictOrder_OneVisitADay_NothingForABlankRecordOrVerdict()
    {
        var log = new List<VisitEntry>();
        Visits.Record(log, "NHA-512", 7, "20 Mar 2150", "Periclean Athens (Ancient)", Visits.Denied, "Denied without logged evidence");
        Visits.Record(log, "RKW-773", 8, "21 Mar 2150", "Victorian London (Industrial)", Visits.Accepted, "");
        Visits.Record(log, "NHA-512", 7, "20 Mar 2150", "Periclean Athens (Ancient)", "Detained", "");
        Visits.Record(log, " ", 9, "", "", Visits.Accepted, "");
        Visits.Record(log, "X", 9, "", "", " ", "");
        Visits.Record(null, "X", 9, "", "", Visits.Accepted, "");

        Assert.AreEqual(2, log.Count);
        Assert.AreEqual("RKW-773", log[0].record);
        Assert.AreEqual("Detained", log[1].verdict, "a second verdict the same day replaces the first; any verdict word is kept");
    }

    [Test]
    public void Before_OnlyThatRecord_OnlyEarlierDays_OldestFirst()
    {
        var log = new List<VisitEntry>
        {
            new VisitEntry { record = "NHA-512", day = 11, verdict = Visits.Denied },
            new VisitEntry { record = "NHA-512", day = 7, verdict = Visits.Denied },
            new VisitEntry { record = "RKW-773", day = 8, verdict = Visits.Accepted },
            new VisitEntry { record = "NHA-512", day = 15, verdict = Visits.Accepted },
            null
        };
        CollectionAssert.AreEqual(new[] { 7, 11 }, Visits.Before(log, " NHA-512", 15).Select(v => v.day).ToArray());
        Assert.IsEmpty(Visits.Before(log, "NHA-512", 7), "a first visit has no history");
        Assert.IsEmpty(Visits.Before(log, "", 15));
    }

    [Test]
    public void Flag_TheLatestEarlierVerdict_AndItsDaysAgo()
    {
        var earlier = new List<VisitEntry>
        {
            new VisitEntry { record = "NHA-512", day = 7, verdict = Visits.Accepted },
            new VisitEntry { record = "NHA-512", day = 11, verdict = Visits.Denied }
        };
        SeenBefore? flag = Visits.Flag(earlier, 14);
        Assert.IsTrue(flag.HasValue);
        Assert.AreEqual(Visits.Denied, flag.Value.Verdict);
        Assert.AreEqual(3, flag.Value.DaysAgo);
        Assert.IsNull(Visits.Flag(new List<VisitEntry>(), 14), "no earlier visit: no flag");
        Assert.IsNull(Visits.Flag(null, 14));
    }

    [Test]
    public void FlagText_TheVerdictInCapitals_DaysOrYesterday_AnyVerdictWord()
    {
        Assert.AreEqual("DENIED 3 DAYS AGO", Visits.FlagText(new SeenBefore(Visits.Denied, 3), null, null, null));
        Assert.AreEqual("ACCEPTED YESTERDAY", Visits.FlagText(new SeenBefore(Visits.Accepted, 1), null, null, null));
        Assert.AreEqual("DETAINED 2 DAYS AGO", Visits.FlagText(new SeenBefore("Detained", 2), null, null, null), "a verdict the desk machine adds needs no code here");
        Assert.AreEqual("REFUSED 4 DAYS AGO", Visits.FlagText(new SeenBefore(Visits.Denied, 4), "Refused", "{0} {1} DAYS AGO", null), "the shown word may differ from the stored one");
        Assert.AreEqual("DENIED, 2d", Visits.FlagText(new SeenBefore(Visits.Denied, 2), null, "{0}, {1}d", "{0}, 1d"));
    }
}
