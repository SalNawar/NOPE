using System.Collections.Generic;
using NUnit.Framework;

/// <summary>The balance simulation's bribe-taking clerk (Saleh's Q6, 2026-09-29): every offered dialog with a paying choice is finished on it; nothing else is touched.</summary>
public class BribePolicyTests
{
    private static AuthoredDialog Dialog(string id, params (string node, string effect)[] choices)
    {
        var d = new AuthoredDialog { id = id };
        foreach ((string node, string effect) in choices)
        {
            ScriptNode n = d.nodes.Find(x => x.id == node);
            if (n == null)
            {
                n = new ScriptNode { id = node };
                d.nodes.Add(n);
            }
            n.choices.Add(new ScriptChoice { id = node + "." + n.choices.Count, effect = effect });
        }
        return d;
    }

    private static float Pay(string effect) => effect == "Effect_Bribe" ? 50f : effect == "Effect_Fine" ? -10f : 0f;

    [Test]
    public void Take_TheFirstPayingChoiceOfEachDialog()
    {
        var dialogs = new List<AuthoredDialog>
        {
            Dialog("dlg_rook", ("start", ""), ("read", "Effect_Bribe"), ("read", "")),
            Dialog("dlg_news", ("start", "Effect_Rumour")),
        };
        CollectionAssert.AreEqual(new[] { ("dlg_rook", "Effect_Bribe") }, BribePolicy.Take(dialogs, Pay));
    }

    [Test]
    public void Take_NothingWithoutAPayingChoice()
    {
        Assert.IsEmpty(BribePolicy.Take(new[] { Dialog("dlg_fine", ("start", "Effect_Fine")), Dialog("", ("start", "Effect_Bribe")), null }, Pay), "a cost is no bribe; a dialog needs an id");
        Assert.IsEmpty(BribePolicy.Take(null, Pay));
        Assert.IsEmpty(BribePolicy.Take(new[] { Dialog("dlg_rook", ("read", "Effect_Bribe")) }, null));
    }
}
