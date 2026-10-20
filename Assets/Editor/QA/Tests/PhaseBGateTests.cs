using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// DemoGate's Phase B rules (FIX_PLAN_v2 rules 9 and 13): a merged track
/// needs an approval line, a bundle is whole or absent. Pure; runs outside
/// Unity.
/// </summary>
public class PhaseBGateTests
{
    [Test]
    public void Merged_IsThePhaseBTracksAmongTheCheckOwners()
    {
        CollectionAssert.AreEquivalent(new[] { "BELL-SIGN", "GLASSES" }, PhaseBGate.Merged(new[] { "QA-RIG", "bell-sign", "GLASSES", "DOC-HANDLE", null }));
    }

    [Test]
    public void Approved_ReadsApprovalLines_NotOtherMentions()
    {
        string text = "# Approvals\n- BELL-SIGN: approved by Saleh 2026-10-21 \"go\"\n- DETAIN: spec sent, waiting\n- GLASSES: not approved yet\nPC-LANG-FX: Approved\n";
        HashSet<string> a = PhaseBGate.Approved(text);
        CollectionAssert.AreEquivalent(new[] { "BELL-SIGN", "PC-LANG-FX" }, a);
    }

    [Test]
    public void Unapproved_ListsMergedTracksWithoutALine()
    {
        CollectionAssert.AreEqual(new[] { "DETAIN" }, PhaseBGate.Unapproved(new[] { "BELL-SIGN", "DETAIN" }, new HashSet<string> { "BELL-SIGN" }));
    }

    [Test]
    public void PartialBundle_IsReported_WholeOrAbsentIsNot()
    {
        Assert.AreEqual(1, PhaseBGate.PartialBundles(new[] { "LANGUAGE", "GLASSES" }).Count);
        Assert.IsEmpty(PhaseBGate.PartialBundles(new[] { "LANGUAGE", "BABEL-TUNER", "GLASSES" }));
        Assert.IsEmpty(PhaseBGate.PartialBundles(new[] { "PC-LANG-FX", "BELL-SIGN" }));
    }
}
