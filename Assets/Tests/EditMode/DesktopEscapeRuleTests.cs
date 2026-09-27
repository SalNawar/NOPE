using NUnit.Framework;

/// <summary>The desktop's part of the Escape chain (the PC redesign KB3, section 3.5): one press does one thing, in the chain's full order; None lets the frame close.</summary>
public class DesktopEscapeRuleTests
{
    private static DesktopEscape Resolve(bool menu = false, bool card = false, bool results = false, bool searchFocused = false, bool searchText = false,
                                         bool field = false, bool startMenu = false, bool drag = false) =>
        DesktopEscapeRule.Resolve(new DesktopEscapeState(menu, card, results, searchFocused, searchText, field, startMenu, drag));

    [Test]
    public void NothingOpen_None() => Assert.AreEqual(DesktopEscape.None, Resolve());

    [Test]
    public void OneRowPerState()
    {
        Assert.AreEqual(DesktopEscape.CloseMenu, Resolve(menu: true));
        Assert.AreEqual(DesktopEscape.CloseCard, Resolve(card: true));
        Assert.AreEqual(DesktopEscape.CloseResults, Resolve(results: true));
        Assert.AreEqual(DesktopEscape.ClearSearch, Resolve(searchFocused: true, searchText: true, field: true));
        Assert.AreEqual(DesktopEscape.LeaveField, Resolve(field: true));
        Assert.AreEqual(DesktopEscape.CloseStartMenu, Resolve(startMenu: true));
        Assert.AreEqual(DesktopEscape.CancelDrag, Resolve(drag: true));
    }

    [Test]
    public void TheFullOrder_EachStepBeforeEveryLaterOne()
    {
        // Everything on: each press takes the first step still on, in the chain's order.
        Assert.AreEqual(DesktopEscape.CloseMenu, Resolve(true, true, true, true, true, true, true, true));
        Assert.AreEqual(DesktopEscape.CloseCard, Resolve(false, true, true, true, true, true, true, true));
        Assert.AreEqual(DesktopEscape.CloseResults, Resolve(false, false, true, true, true, true, true, true));
        Assert.AreEqual(DesktopEscape.ClearSearch, Resolve(false, false, false, true, true, true, true, true));
        Assert.AreEqual(DesktopEscape.LeaveField, Resolve(false, false, false, true, false, true, true, true));
        Assert.AreEqual(DesktopEscape.CloseStartMenu, Resolve(false, false, false, false, false, false, true, true));
        Assert.AreEqual(DesktopEscape.CancelDrag, Resolve(false, false, false, false, false, false, false, true));
    }

    [Test]
    public void AnEmptySearchField_IsLeft_NotCleared() =>
        Assert.AreEqual(DesktopEscape.LeaveField, Resolve(searchFocused: true, field: true));

    [Test]
    public void SearchTextWithoutTheFieldFocused_IsNotCleared() =>
        Assert.AreEqual(DesktopEscape.None, Resolve(searchText: true));

    [Test]
    public void AFocusedField_ComesBeforeTheStartMenuAndADrag()
    {
        Assert.AreEqual(DesktopEscape.LeaveField, Resolve(field: true, startMenu: true));
        Assert.AreEqual(DesktopEscape.LeaveField, Resolve(field: true, drag: true));
    }

    [Test]
    public void TheCard_ComesBeforeAFieldAndTheStartMenu()
    {
        Assert.AreEqual(DesktopEscape.CloseCard, Resolve(card: true, field: true));
        Assert.AreEqual(DesktopEscape.CloseCard, Resolve(card: true, startMenu: true));
    }
}
