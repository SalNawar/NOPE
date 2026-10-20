using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// RUN-SAFE's cheat changes since the save (BUG-28a): a skip or a jump from
/// Home starts its night from the end-of-shift save, and the cheat changes
/// made since that save are replayed on it, in order, so none is lost
/// (RunManager.CheatEdits). Pure: these run outside Unity as well as in its
/// EditMode suite. The whole route with them is CheatLedgerTests'.
/// </summary>
public class CheatEditsTests
{
    [Test]
    public void TheChanges_AreReplayedOnTheSave_InTheOrderTheyWereMade()
    {
        var edits = new CheatEdits();
        edits.Note("stability set 70", w => w.timelineStability = 70f);
        edits.Note("stability -9.5", w => w.timelineStability -= 9.5f);
        edits.Note("money +100", w => w.money += 100);
        edits.Note("flag rook set", w => w.SetFlag("rook"));

        var save = new WorldState { money = 50, timelineStability = 90f };
        Assert.AreSame(save, edits.ReplayOn(save));

        Assert.AreEqual(60.5f, save.timelineStability, 0.0001f, "set, then stepped: in order");
        Assert.AreEqual(150, save.money);
        Assert.IsTrue(save.HasFlag("rook"));
        Assert.AreEqual(4, edits.Count, "a replay keeps them (the route may need them again only if it is played again)");
        CollectionAssert.AreEqual(new List<string> { "stability set 70", "stability -9.5", "money +100", "flag rook set" }, edits.Labels);
    }

    [Test]
    public void ASave_ForgetsThem()
    {
        var edits = new CheatEdits();
        edits.Note("money +100", w => w.money += 100);
        edits.Clear();

        var save = new WorldState { money = 50 };
        edits.ReplayOn(save);

        Assert.AreEqual(0, edits.Count);
        Assert.AreEqual(50, save.money, "a change the save already holds is never applied twice");
    }

    [Test]
    public void NoSave_AndNoEdit_ChangeNothing()
    {
        var edits = new CheatEdits();
        edits.Note("nothing", null);
        Assert.AreEqual(0, edits.Count, "a null edit is not kept");
        Assert.IsNull(edits.ReplayOn(null));
    }
}
