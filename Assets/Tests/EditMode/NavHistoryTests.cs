using NUnit.Framework;

/// <summary>The back/forward stack (the browser's, each app pane's): Go, Back, Forward, the cap, Go clearing forward, and RemoveAll.</summary>
public class NavHistoryTests
{
    [Test]
    public void Empty_NothingToShowOrStepTo()
    {
        var h = new NavHistory<string>(30);
        Assert.IsFalse(h.HasCurrent);
        Assert.IsNull(h.Current);
        Assert.IsFalse(h.CanBack);
        Assert.IsFalse(h.CanForward);
        Assert.IsFalse(h.Back(out string back));
        Assert.IsNull(back);
        Assert.IsFalse(h.Forward(out string forward));
        Assert.IsNull(forward);
    }

    [Test]
    public void BackAndForward_WalkTheVisits()
    {
        var h = new NavHistory<string>(30);
        h.Go("a");
        h.Go("b");
        h.Go("c");
        Assert.AreEqual("c", h.Current);
        Assert.IsTrue(h.Back(out string at));
        Assert.AreEqual("b", at);
        Assert.IsTrue(h.Back(out at));
        Assert.AreEqual("a", at);
        Assert.IsFalse(h.CanBack);
        Assert.IsTrue(h.Forward(out at));
        Assert.AreEqual("b", at);
        Assert.AreEqual("b", h.Current);
        Assert.IsTrue(h.CanForward);
    }

    [Test]
    public void Go_ClearsForward()
    {
        var h = new NavHistory<string>(30);
        h.Go("a");
        h.Go("b");
        h.Go("c");
        h.Back(out _);
        h.Back(out _);
        h.Go("d");
        Assert.AreEqual("d", h.Current);
        Assert.IsFalse(h.CanForward, "b and c are gone");
        Assert.IsTrue(h.Back(out string at));
        Assert.AreEqual("a", at);
    }

    [Test]
    public void Go_TheCurrentAgain_ChangesNothing()
    {
        var h = new NavHistory<string>(30);
        h.Go("a");
        h.Go("b");
        h.Go("b");
        Assert.IsTrue(h.Back(out string at));
        Assert.AreEqual("a", at, "b was kept once");
        Assert.IsFalse(h.CanBack);
    }

    [Test]
    public void Cap_DropsTheOldest()
    {
        var h = new NavHistory<int>(3);
        for (int i = 1; i <= 5; i++)
            h.Go(i);
        Assert.AreEqual(5, h.Current);
        Assert.IsTrue(h.Back(out int at));
        Assert.AreEqual(4, at);
        Assert.IsTrue(h.Back(out at));
        Assert.AreEqual(3, at);
        Assert.IsFalse(h.CanBack, "1 and 2 dropped");

        var one = new NavHistory<int>(0);
        one.Go(1);
        one.Go(2);
        Assert.AreEqual(2, one.Current, "a cap below 1 counts as 1");
        Assert.IsFalse(one.CanBack);
    }

    [Test]
    public void RemoveAll_KeepsTheOthersInOrder_AndTheCurrentWhenKept()
    {
        var h = new NavHistory<string>(30);
        foreach (string s in new[] { "a", "B", "c", "D", "e" })
            h.Go(s);
        h.Back(out _);
        h.Back(out _);
        Assert.AreEqual("c", h.Current);
        h.RemoveAll(s => s == s.ToUpperInvariant());
        Assert.AreEqual("c", h.Current);
        Assert.IsTrue(h.Back(out string at));
        Assert.AreEqual("a", at);
        Assert.IsTrue(h.Forward(out at));
        Assert.IsTrue(h.Forward(out at));
        Assert.AreEqual("e", at);
        Assert.IsFalse(h.CanForward);
    }

    [Test]
    public void RemoveAll_ADroppedCurrent_HandsOverToTheKeptOneBeforeIt()
    {
        var h = new NavHistory<string>(30);
        foreach (string s in new[] { "a", "b", "C" })
            h.Go(s);
        h.RemoveAll(s => s == "C");
        Assert.AreEqual("b", h.Current);
        Assert.IsFalse(h.CanForward);

        var first = new NavHistory<string>(30);
        foreach (string s in new[] { "A", "b" })
            first.Go(s);
        first.Back(out _);
        first.RemoveAll(s => s == "A");
        Assert.AreEqual("b", first.Current, "nothing kept before it: the first kept one");
        Assert.IsFalse(first.CanBack);
    }

    [Test]
    public void RemoveAll_Everything_LeavesItEmpty()
    {
        var h = new NavHistory<string>(30);
        h.Go("a");
        h.Go("b");
        h.RemoveAll(_ => true);
        Assert.IsFalse(h.HasCurrent);
        Assert.IsFalse(h.CanBack);
        Assert.IsFalse(h.CanForward);
        h.Go("c");
        Assert.AreEqual("c", h.Current);
    }
}
