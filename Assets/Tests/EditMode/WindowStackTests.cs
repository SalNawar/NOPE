using System.Collections.Generic;
using NUnit.Framework;

/// <summary>The desktop's one window stack (the PC redesign WN1): open, focus, minimise, restore, the taskbar click, close, the z-order, the taskbar order and one Changed per change.</summary>
public class WindowStackTests
{
    private static (WindowStack stack, int[] changes) Watch(params string[] open)
    {
        var stack = new WindowStack();
        foreach (string id in open)
            stack.Open(id);
        var changes = new int[1];
        stack.Changed += () => changes[0]++;
        return (stack, changes);
    }

    private static void AssertZ(WindowStack stack, params string[] bottomToTop) =>
        CollectionAssert.AreEqual(bottomToTop, new List<string>(stack.ZOrder));

    private static void AssertTaskbar(WindowStack stack, params string[] openOrder) =>
        CollectionAssert.AreEqual(openOrder, new List<string>(stack.TaskbarOrder));

    [Test]
    public void ANewStack_IsEmpty_WithNothingFocused()
    {
        var stack = new WindowStack();
        AssertZ(stack);
        AssertTaskbar(stack);
        Assert.IsNull(stack.Focused);
        Assert.IsFalse(stack.IsOpen("a"));
        Assert.IsFalse(stack.IsMinimised("a"));
    }

    [Test]
    public void Open_ShowsAndFocuses_OnTopAndLastOnTheTaskbar()
    {
        (WindowStack stack, int[] changes) = Watch();
        stack.Open("a");
        stack.Open("b");
        AssertZ(stack, "a", "b");
        AssertTaskbar(stack, "a", "b");
        Assert.AreEqual("b", stack.Focused);
        Assert.IsTrue(stack.IsOpen("a"));
        Assert.IsFalse(stack.IsMinimised("a"));
        Assert.AreEqual(2, changes[0]);
    }

    [Test]
    public void Open_AnOpenWindow_RaisesAndFocusesIt_KeepingItsTaskbarPlace()
    {
        (WindowStack stack, int[] changes) = Watch("a", "b");
        stack.Open("a");
        AssertZ(stack, "b", "a");
        AssertTaskbar(stack, "a", "b");
        Assert.AreEqual("a", stack.Focused);
        Assert.AreEqual(1, changes[0]);
    }

    [Test]
    public void Open_TheFocusedTopWindow_ChangesNothing()
    {
        (WindowStack stack, int[] changes) = Watch("a", "b");
        stack.Open("b");
        stack.Focus("b");
        Assert.AreEqual(0, changes[0]);
    }

    [Test]
    public void Focus_RaisesAndFocuses_AnUnknownWindowIsIgnored()
    {
        (WindowStack stack, int[] changes) = Watch("a", "b", "c");
        stack.Focus("a");
        AssertZ(stack, "b", "c", "a");
        Assert.AreEqual("a", stack.Focused);
        stack.Focus("nope");
        AssertZ(stack, "b", "c", "a");
        Assert.IsFalse(stack.IsOpen("nope"));
        Assert.AreEqual(1, changes[0]);
    }

    [Test]
    public void Minimise_HidesTheWindow_KeepsItsTaskbarButton_AndFocusPassesToTheNextVisibleWindow()
    {
        (WindowStack stack, int[] changes) = Watch("a", "b", "c");
        stack.Minimise("c");
        AssertZ(stack, "a", "b");
        AssertTaskbar(stack, "a", "b", "c");
        Assert.IsTrue(stack.IsOpen("c"));
        Assert.IsTrue(stack.IsMinimised("c"));
        Assert.AreEqual("b", stack.Focused);
        Assert.AreEqual(1, changes[0]);
    }

    [Test]
    public void Minimise_AnUnfocusedWindow_KeepsTheFocus()
    {
        (WindowStack stack, _) = Watch("a", "b");
        stack.Minimise("a");
        Assert.AreEqual("b", stack.Focused);
        AssertZ(stack, "b");
    }

    [Test]
    public void Minimise_TheLastVisibleWindow_LeavesNothingFocused()
    {
        (WindowStack stack, _) = Watch("a");
        stack.Minimise("a");
        Assert.IsNull(stack.Focused);
        AssertZ(stack);
        AssertTaskbar(stack, "a");
    }

    [Test]
    public void Minimise_TwiceOrUnknown_ChangesNothing()
    {
        (WindowStack stack, int[] changes) = Watch("a", "b");
        stack.Minimise("a");
        stack.Minimise("a");
        stack.Minimise("nope");
        Assert.AreEqual(1, changes[0]);
    }

    [Test]
    public void OpenAndFocus_RestoreAMinimisedWindow_OnTop()
    {
        (WindowStack stack, _) = Watch("a", "b");
        stack.Minimise("a");
        stack.Open("a");
        Assert.IsFalse(stack.IsMinimised("a"));
        AssertZ(stack, "b", "a");
        Assert.AreEqual("a", stack.Focused);

        stack.Minimise("b");
        stack.Focus("b");
        Assert.IsFalse(stack.IsMinimised("b"));
        AssertZ(stack, "a", "b");
        AssertTaskbar(stack, "a", "b");
    }

    [Test]
    public void TaskbarClick_MinimisesTheFocusedWindow_ThenRestoresAndFocusesIt()
    {
        (WindowStack stack, int[] changes) = Watch("a", "b");
        stack.TaskbarClick("b");
        Assert.IsTrue(stack.IsMinimised("b"));
        Assert.AreEqual("a", stack.Focused);

        stack.TaskbarClick("b");
        Assert.IsFalse(stack.IsMinimised("b"));
        Assert.AreEqual("b", stack.Focused);
        AssertZ(stack, "a", "b");
        Assert.AreEqual(2, changes[0]);
    }

    [Test]
    public void TaskbarClick_AVisibleUnfocusedWindow_RaisesAndFocusesIt()
    {
        (WindowStack stack, _) = Watch("a", "b");
        stack.TaskbarClick("a");
        Assert.IsFalse(stack.IsMinimised("a"));
        Assert.AreEqual("a", stack.Focused);
        AssertZ(stack, "b", "a");
    }

    [Test]
    public void Close_RemovesTheWindow_AndFocusPassesToTheNextVisibleWindow()
    {
        (WindowStack stack, int[] changes) = Watch("a", "b", "c");
        stack.Minimise("a");
        stack.Close("c");
        AssertZ(stack, "b");
        AssertTaskbar(stack, "a", "b");
        Assert.AreEqual("b", stack.Focused);
        Assert.IsFalse(stack.IsOpen("c"));

        stack.Close("a");
        Assert.IsFalse(stack.IsOpen("a"));
        Assert.IsFalse(stack.IsMinimised("a"));
        AssertTaskbar(stack, "b");
        Assert.AreEqual(3, changes[0]);
    }

    [Test]
    public void Close_TheLastWindow_LeavesNothingFocused_AndAClosedWindowReopensLast()
    {
        (WindowStack stack, _) = Watch("a", "b");
        stack.Close("b");
        stack.Close("a");
        Assert.IsNull(stack.Focused);
        stack.Open("a");
        stack.Open("b");
        stack.Close("a");
        stack.Open("a");
        AssertTaskbar(stack, "b", "a");
    }

    [Test]
    public void Close_AnUnknownWindow_ChangesNothing()
    {
        (WindowStack stack, int[] changes) = Watch("a");
        stack.Close("nope");
        Assert.AreEqual(0, changes[0]);
    }

    [Test]
    public void ClearFocus_LeavesTheZOrder_AndATaskbarClickThenRaisesInsteadOfMinimising()
    {
        (WindowStack stack, int[] changes) = Watch("a", "b");
        stack.ClearFocus();
        Assert.IsNull(stack.Focused);
        AssertZ(stack, "a", "b");
        stack.ClearFocus();
        Assert.AreEqual(1, changes[0]);

        stack.TaskbarClick("b");
        Assert.IsFalse(stack.IsMinimised("b"));
        Assert.AreEqual("b", stack.Focused);
        Assert.AreEqual(2, changes[0]);
    }
}
