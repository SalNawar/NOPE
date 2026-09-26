using NUnit.Framework;

/// <summary>The desktop's part of the Escape chain (the PC redesign KB3, section 3.5): one press does one thing, in the chain's order; None lets the frame close.</summary>
public class DesktopEscapeRuleTests
{
    private static DesktopEscape Resolve(bool field = false, bool startMenu = false, bool drag = false, bool menu = false) =>
        DesktopEscapeRule.Resolve(new DesktopEscapeState(menu, field, startMenu, drag));

    [Test]
    public void NothingOpen_None() => Assert.AreEqual(DesktopEscape.None, Resolve());

    [Test]
    public void AFocusedField_IsLeft() => Assert.AreEqual(DesktopEscape.LeaveField, Resolve(field: true));

    [Test]
    public void AContextMenu_Closes() => Assert.AreEqual(DesktopEscape.CloseMenu, Resolve(menu: true));

    [Test]
    public void AContextMenu_ComesFirst() =>
        Assert.AreEqual(DesktopEscape.CloseMenu, Resolve(true, true, true, true));

    [Test]
    public void TheStartMenu_Closes() => Assert.AreEqual(DesktopEscape.CloseStartMenu, Resolve(startMenu: true));

    [Test]
    public void ADrag_IsCancelled() => Assert.AreEqual(DesktopEscape.CancelDrag, Resolve(drag: true));

    [Test]
    public void AFocusedField_ComesBeforeTheStartMenuAndADrag()
    {
        Assert.AreEqual(DesktopEscape.LeaveField, Resolve(field: true, startMenu: true));
        Assert.AreEqual(DesktopEscape.LeaveField, Resolve(field: true, drag: true));
        Assert.AreEqual(DesktopEscape.LeaveField, Resolve(true, true, true));
    }

    [Test]
    public void TheStartMenu_ComesBeforeADrag() =>
        Assert.AreEqual(DesktopEscape.CloseStartMenu, Resolve(startMenu: true, drag: true));
}
