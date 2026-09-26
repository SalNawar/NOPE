using System;
using NUnit.Framework;

/// <summary>The one-shot callback (audit R4-025): fired once, its slot emptied before it runs, so the callback may fill the slot again.</summary>
public class OneShotTests
{
    [Test]
    public void Fire_InvokesOnce_WithTheValue_AndEmptiesTheSlot()
    {
        int calls = 0;
        bool seen = false;
        Action<bool> slot = v => { calls++; seen = v; };
        OneShot.Fire(ref slot, true);
        Assert.AreEqual(1, calls);
        Assert.IsTrue(seen);
        Assert.IsNull(slot);
        OneShot.Fire(ref slot, false);
        Assert.AreEqual(1, calls, "an empty slot fires nothing");
    }

    [Test]
    public void Fire_EmptiesTheSlotBeforeTheCallback_WhichMayFillItAgain()
    {
        Action<int> slot = null;
        Action<int> next = _ => { };
        slot = _ => slot = next;
        OneShot.Fire(ref slot, 1);
        Assert.AreSame(next, slot, "the callback's new callback survives");
    }

    [Test]
    public void Fire_AnEmptySlot_DoesNothing()
    {
        Action<string> slot = null;
        Assert.DoesNotThrow(() => OneShot.Fire(ref slot, "x"));
        Assert.IsNull(slot);
    }
}
