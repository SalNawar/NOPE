using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The Stranding Waiver's rules (the endings and strandings spec §7; Saleh's
/// Q14 = A): valid = signed and the account's own (number and unit); on file
/// when any waiver is valid; the pad's answer by kind, papers and the
/// personality's refusal chance on the traveller's own stream; a denial of a
/// traveller the pad cured stays right.
/// </summary>
public class WaiversTests
{
    private static List<DocumentField> Paper(string number, string unit, string signature) => new List<DocumentField>
    {
        new DocumentField { category = ClueCategory.Name, value = "Pell" },
        new DocumentField { category = ClueCategory.TransponderId, value = unit },
        new DocumentField { category = ClueCategory.WaiverNo, value = number },
        new DocumentField { category = ClueCategory.Signature, value = signature }
    };

    [Test]
    public void IsValid_SignedAndTheAccountsOwn()
    {
        Assert.IsTrue(Waivers.IsValid(Paper("SW-204817", "HP-40718", "Pell"), "SW-204817", "HP-40718"));
        Assert.IsTrue(Waivers.IsValid(Paper(" sw-204817 ", "hp-40718", "Pell"), "SW-204817", "HP-40718"), "Values.Match: trimmed, any case");
        Assert.IsFalse(Waivers.IsValid(Paper("SW-204817", "HP-40718", Directives.UnsignedMark), "SW-204817", "HP-40718"), "unsigned");
        Assert.IsFalse(Waivers.IsValid(Paper("SW-204817", "HP-40718", " "), "SW-204817", "HP-40718"), "a blank signature");
        Assert.IsFalse(Waivers.IsValid(Paper("SW-999999", "HP-40718", "Pell"), "SW-204817", "HP-40718"), "L3: a number the account never registered");
        Assert.IsFalse(Waivers.IsValid(Paper("SW-204817", "HP-11111", "Pell"), "SW-204817", "HP-40718"), "L3: made out for another unit");
        Assert.IsFalse(Waivers.IsValid(Paper("SW-204817", "HP-40718", "Pell"), null, "HP-40718"), "no waiver registered (a Premium account)");
        Assert.IsFalse(Waivers.IsValid(null, "SW-204817", "HP-40718"), "not carried");
        var noSignatureBox = new List<DocumentField> { new DocumentField { category = ClueCategory.WaiverNo, value = "SW-204817" } };
        Assert.IsFalse(Waivers.IsValid(noSignatureBox, "SW-204817", "HP-40718"), "a waiver needs its signature");
    }

    [Test]
    public void OnFile_AnyValidWaiver()
    {
        var carried = Paper("SW-204817", "HP-40718", Directives.UnsignedMark);
        var desk = Paper("SW-204817", "HP-40718", "Pell");
        Assert.IsFalse(Waivers.OnFile(new[] { carried }, "SW-204817", "HP-40718"));
        Assert.IsTrue(Waivers.OnFile(new[] { carried, desk }, "SW-204817", "HP-40718"), "the pad's signed copy counts");
        Assert.IsFalse(Waivers.OnFile(null, "SW-204817", "HP-40718"));
    }

    /// <summary>A source whose draws are scripted and counted.</summary>
    private sealed class Scripted : IRandomSource
    {
        private readonly float _value;
        public Scripted(float value) => _value = value;
        public int Draws { get; private set; }
        public int Range(int minInclusive, int maxExclusive) => minInclusive;
        public float Value()
        {
            Draws++;
            return _value;
        }
    }

    [Test]
    public void PadReply_ByKindPapersAndRefusal_OneDrawWhateverTheAnswer()
    {
        var low = new Scripted(0.1f);
        Assert.AreEqual(WaiverPadReply.NotNeeded, Waivers.PadReply(false, false, 0.5f, low), "a kind that carries no waiver (a Premium tourist, the displaced)");
        Assert.AreEqual(WaiverPadReply.AlreadySigned, Waivers.PadReply(true, true, 0.5f, low));
        Assert.AreEqual(WaiverPadReply.Refuses, Waivers.PadReply(true, false, 0.5f, low), "under a Grand's 0.5");
        Assert.AreEqual(WaiverPadReply.Signs, Waivers.PadReply(true, false, 0.05f, low), "over a Sunny's 0.05");
        Assert.AreEqual(4, low.Draws, "one draw every time, so the answer never depends on the papers' draw count");
        Assert.AreEqual(WaiverPadReply.Signs, Waivers.PadReply(true, false, 0f, new Scripted(0f)), "a refusal of 0 always signs");
        Assert.AreEqual(WaiverPadReply.Signs, Waivers.PadReply(true, false, 0.5f, null), "no stream: signs");
    }

    [Test]
    public void PadReply_OnTheTravellersOwnStream_IsDeterministic()
    {
        int caseSeed = Seeds.ForCase(Seeds.Day(12345, 7), 3);
        WaiverPadReply a = Waivers.PadReply(true, false, 0.3f, new SeededRandom(Seeds.ForWaiverSign(caseSeed)));
        WaiverPadReply b = Waivers.PadReply(true, false, 0.3f, new SeededRandom(Seeds.ForWaiverSign(caseSeed)));
        Assert.AreEqual(a, b);
        int refusals = 0;
        for (int c = 1; c <= 400; c++)
            if (Waivers.PadReply(true, false, 0.3f, new SeededRandom(Seeds.ForWaiverSign(Seeds.ForCase(Seeds.Day(12345, 7), c)))) == WaiverPadReply.Refuses)
                refusals++;
        Assert.That(refusals, Is.InRange(80, 160), $"about 30 % of 400 refuse ({refusals})");
    }

    [Test]
    public void IsCorrect_ADenialOfATravellerThePadCuredStaysRight()
    {
        Assert.IsTrue(VerdictRules.IsCorrect(true, true, false));
        Assert.IsTrue(VerdictRules.IsCorrect(false, false, false));
        Assert.IsFalse(VerdictRules.IsCorrect(false, true, false), "denying a faultless traveller");
        Assert.IsTrue(VerdictRules.IsCorrect(true, true, true), "approving after the desk signature");
        Assert.IsTrue(VerdictRules.IsCorrect(false, true, true), "denying stays right: the rule refuses unsigned papers");
        Assert.IsFalse(VerdictRules.IsCorrect(true, false, true), "a cure never makes a faulty approval right");
    }
}
