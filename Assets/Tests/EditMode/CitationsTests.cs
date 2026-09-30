using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Papers Please lesson 6 (wave 5 track C): the citation slip names the exact
/// rule, with its row on today's Directive Memo, and the exact values
/// involved (Citations).
/// </summary>
public class CitationsTests
{
    private static readonly Dictionary<string, string> Text = new Dictionary<string, string>
    {
        { "citation.rule.paperSet.waiverUnsigned", "This departure needs a signed Stranding Waiver." }
    };

    private static string Get(string key) => Text.TryGetValue(key, out string text) ? text : key;

    [Test]
    public void MemoNumber_CountsTheRulesWithALine_LikeTheMemo()
    {
        var lines = new[] { "Embargo: no travel to New Kingdom Egypt today.", "", "Leisure departures: ...", null, "A Premium visa travels on a Premium transponder." };
        Assert.AreEqual(1, Citations.MemoNumber(lines, 0));
        Assert.AreEqual(0, Citations.MemoNumber(lines, 1), "a rule with no line is no row");
        Assert.AreEqual(2, Citations.MemoNumber(lines, 2));
        Assert.AreEqual(3, Citations.MemoNumber(lines, 4));
        Assert.AreEqual(0, Citations.MemoNumber(lines, 9));
        Assert.AreEqual(0, Citations.MemoNumber(lines, -1));
        CollectionAssert.AreEqual(new[] { "3", lines[4] }, DirectiveMemoPage.Rows(lines)[2], "the memo numbers the same rows");
    }

    [Test]
    public void RuleLine_TheDirectiveNumberAndTheRule()
    {
        var directive = new CitationFacts { RuleText = "Embargo: no travel to New Kingdom Egypt today.", DirectiveNumber = 1 };
        Assert.AreEqual("Directive 1: Embargo: no travel to New Kingdom Egypt today.", Citations.RuleLine(directive, Get, "Directive {0}: {1}"));

        var check = new CitationFacts { RuleKey = "citation.rule.paperSet.waiverUnsigned", DirectiveNumber = 3 };
        Assert.AreEqual("Directive 3: This departure needs a signed Stranding Waiver.", Citations.RuleLine(check, Get, "Directive {0}: {1}"),
                        "a check's line from the strings, numbered by the rule that holds it");

        var unlisted = new CitationFacts { RuleKey = "citation.rule.paperSet.waiverUnsigned" };
        Assert.AreEqual("This departure needs a signed Stranding Waiver.", Citations.RuleLine(unlisted, Get, "Directive {0}: {1}"), "no row: no number");
        Assert.AreEqual(string.Empty, Citations.RuleLine(null, Get, "Directive {0}: {1}"));
        Assert.AreEqual(string.Empty, Citations.RuleLine(new CitationFacts(), Get, "Directive {0}: {1}"));
    }

    [Test]
    public void ValuesLine_EachValueWithWhereItWasRead()
    {
        var values = new List<CitationValue>
        {
            new CitationValue("Signature on the Stranding Waiver", "UNSIGNED"),
            new CitationValue("Today", " "),
            new CitationValue("", "open today")
        };
        Assert.AreEqual("Signature on the Stranding Waiver: UNSIGNED. open today", Citations.ValuesLine(values, "{0}: {1}.", " "),
                        "a blank value is skipped, a blank label prints the value alone");
        Assert.AreEqual(string.Empty, Citations.ValuesLine(null, "{0}: {1}.", " "));
    }

    private static CaseFacts Poor(params string[] forms) => new CaseFacts
    {
        Kind = TravellerKind.PoorTourist,
        VisaClass = CitizenStatus.Standard,
        ManifestClass = TransponderClass.Economy,
        Forms = forms,
        WaiverSigned = true
    };

    [Test]
    public void PaperSetBreach_NamesTheConditionThatBroke_InThePredicatesOrder()
    {
        string[] full = { Directives.Visa, Directives.Manifest, Directives.Waiver, "TC-415" };
        Assert.IsNull(Citations.PaperSetBreach(Poor(full)), "a complete set");

        CaseFacts unsigned = Poor(full);
        unsigned.WaiverSigned = false;
        Assert.AreEqual(Citations.WaiverUnsigned, Citations.PaperSetBreach(unsigned));

        CaseFacts missing = Poor(Directives.Visa, Directives.Manifest, "TC-415");
        missing.WaiverSigned = false;
        Assert.AreEqual(Citations.WaiverMissing, Citations.PaperSetBreach(missing));

        Assert.AreEqual(Citations.ProofMissing, Citations.PaperSetBreach(Poor(Directives.Visa, Directives.Manifest, Directives.Waiver)));
        CaseFacts early = Poor(Directives.Visa, Directives.Manifest, Directives.Waiver);
        early.Issued = new[] { Directives.Visa, Directives.Manifest, Directives.Waiver };
        Assert.IsNull(Citations.PaperSetBreach(early), "no proof issued yet (day 4): nothing missing");

        CaseFacts premiumUnit = Poor(full);
        premiumUnit.ManifestClass = TransponderClass.Premium;
        Assert.AreEqual(Citations.PremiumManifest, Citations.PaperSetBreach(premiumUnit));

        var rich = new CaseFacts { Kind = TravellerKind.RichTourist, VisaClass = CitizenStatus.Premium, ManifestClass = TransponderClass.Economy, Forms = full };
        Assert.AreEqual(Citations.EconomyManifest, Citations.PaperSetBreach(rich));

        var labourer = new CaseFacts { Kind = TravellerKind.Labourer, ManifestClass = TransponderClass.Economy, Forms = new[] { Directives.Manifest, Directives.Waiver }, WaiverSigned = true };
        Assert.AreEqual(Citations.ContractMissing, Citations.PaperSetBreach(labourer));
        Assert.IsNull(Citations.PaperSetBreach(new CaseFacts { Kind = TravellerKind.Displaced }), "the displaced have no paper set");
    }

    /// <summary>The breach names the fault the predicate finds: whenever the paper set breaks, a breach key names why (and never when it holds).</summary>
    [Test]
    public void PaperSetBreach_AgreesWithThePredicate()
    {
        string[][] sets =
        {
            new[] { Directives.Visa, Directives.Manifest, Directives.Waiver, "TC-416" },
            new[] { Directives.Visa, Directives.Manifest, Directives.Waiver },
            new[] { Directives.Visa, Directives.Manifest },
            new[] { Directives.Contract, Directives.Manifest, Directives.Waiver },
            new[] { Directives.Manifest, Directives.Waiver }
        };
        foreach (TravellerKind kind in new[] { TravellerKind.RichTourist, TravellerKind.PoorTourist, TravellerKind.Labourer })
            foreach (string[] forms in sets)
                foreach (bool signed in new[] { true, false })
                    foreach (TransponderClass unit in new[] { TransponderClass.Economy, TransponderClass.Premium })
                    {
                        var facts = new CaseFacts
                        {
                            Kind = kind,
                            VisaClass = kind == TravellerKind.RichTourist ? CitizenStatus.Premium : kind == TravellerKind.PoorTourist ? CitizenStatus.Standard : (CitizenStatus?)null,
                            ManifestClass = unit,
                            Forms = forms,
                            WaiverSigned = signed && System.Array.IndexOf(forms, Directives.Waiver) >= 0
                        };
                        Assert.AreEqual(Directives.Breaks(TravelRuleType.PaperSet, facts), Citations.PaperSetBreach(facts) != null,
                                        $"{kind} [{string.Join(",", forms)}] signed={signed} unit={unit}");
                    }
    }
}
