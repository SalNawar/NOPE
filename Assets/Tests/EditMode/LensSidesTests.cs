using NUnit.Framework;

/// <summary>The Lens screen's side rule (the PC Lens spec, section 2): select, put, place, change places, put away, remove.</summary>
public class LensSidesTests
{
    private static readonly LinkTarget A = LinkTarget.ToTab(AppTab.Records);
    private static readonly LinkTarget B = LinkTarget.ToTab(AppTab.Rules);
    private static readonly LinkTarget C = LinkTarget.ToTab(AppTab.Calendar);

    [Test]
    public void StartsEmpty_LeftSelected()
    {
        var s = new LensSides();
        Assert.IsTrue(s.Left.IsNone);
        Assert.IsTrue(s.Right.IsNone);
        Assert.AreEqual(LensSide.Left, s.Selected);
        Assert.AreEqual(LensSide.Right, LensSides.Other(LensSide.Left));
        Assert.AreEqual(LensSide.Left, LensSides.Other(LensSide.Right));
    }

    [Test]
    public void Select_SetsTheSide()
    {
        var s = new LensSides();
        s.Select(LensSide.Right);
        Assert.AreEqual(LensSide.Right, s.Selected);
    }

    [Test]
    public void Put_OpensOnTheSelectedSide_ThenTheOtherSideIsSelected()
    {
        var s = new LensSides();
        s.Put(A);
        Assert.AreEqual(A, s[LensSide.Left]);
        Assert.AreEqual(LensSide.Right, s.Selected);
        s.Put(B);
        Assert.AreEqual(B, s.Right);
        Assert.AreEqual(LensSide.Left, s.Selected);
    }

    [Test]
    public void Put_OnAFullSide_TheOldPaperLeaves()
    {
        var s = new LensSides();
        s.Put(A);
        s.Put(B);
        s.Put(C);
        Assert.AreEqual(C, s.Left);
        Assert.AreEqual(B, s.Right);
    }

    [Test]
    public void Place_FromTheOtherSide_TheTwoChangePlaces()
    {
        var s = new LensSides();
        s.Place(A, LensSide.Left);
        s.Place(B, LensSide.Right);
        s.Place(B, LensSide.Left, LensSide.Right);
        Assert.AreEqual(B, s.Left);
        Assert.AreEqual(A, s.Right);
        Assert.AreEqual(LensSide.Right, s.Selected);
    }

    [Test]
    public void Place_FromTheOtherSide_ToAnEmptySide_TheOtherSideGetsNone()
    {
        var s = new LensSides();
        s.Place(A, LensSide.Left);
        s.Place(A, LensSide.Right, LensSide.Left);
        Assert.IsTrue(s.Left.IsNone);
        Assert.AreEqual(A, s.Right);
    }

    [Test]
    public void Place_FromTheSameSide_NoChange()
    {
        var s = new LensSides();
        s.Place(A, LensSide.Left);
        s.Select(LensSide.Left);
        s.Place(A, LensSide.Left, LensSide.Left);
        Assert.AreEqual(A, s.Left);
        Assert.IsTrue(s.Right.IsNone);
        Assert.AreEqual(LensSide.Left, s.Selected);
    }

    [Test]
    public void Place_ADocumentAlreadyOnTheOtherSide_Moves()
    {
        var s = new LensSides();
        s.Place(A, LensSide.Left);
        s.Place(A, LensSide.Right);
        Assert.IsTrue(s.Left.IsNone);
        Assert.AreEqual(A, s.Right);
        Assert.AreEqual(LensSide.Left, s.Selected);
    }

    [Test]
    public void Place_Otherwise_TakesThePlace_OldOneLeaves()
    {
        var s = new LensSides();
        s.Place(A, LensSide.Left);
        s.Place(B, LensSide.Left);
        Assert.AreEqual(B, s.Left);
        Assert.IsTrue(s.Right.IsNone);
        Assert.AreEqual(LensSide.Right, s.Selected);
    }

    [Test]
    public void Place_None_DoesNothing()
    {
        var s = new LensSides();
        s.Place(A, LensSide.Left);
        s.Place(LinkTarget.None, LensSide.Left);
        Assert.AreEqual(A, s.Left);
        Assert.AreEqual(LensSide.Right, s.Selected);
    }

    [Test]
    public void PutAway_EmptiesTheSide()
    {
        var s = new LensSides();
        s.Place(A, LensSide.Left);
        s.PutAway(LensSide.Left);
        Assert.IsTrue(s.Left.IsNone);
    }

    [Test]
    public void RemoveWhere_DropsOnlyTheDocumentsThatAreGone()
    {
        var s = new LensSides();
        s.Place(A, LensSide.Left);
        s.Place(B, LensSide.Right);
        s.RemoveWhere(d => d.Tab == AppTab.Records);
        Assert.IsTrue(s.Left.IsNone);
        Assert.AreEqual(B, s.Right);
    }

    [Test]
    public void Same_TheTabAndTheItem_NoneIsNeverSame()
    {
        Assert.IsTrue(LensSides.Same(A, A));
        Assert.IsFalse(LensSides.Same(A, B));
        Assert.IsTrue(LensSides.Same(LinkTarget.ToTab(AppTab.Records, 1), LinkTarget.ToTab(AppTab.Records, 1)));
        Assert.IsFalse(LensSides.Same(LinkTarget.ToTab(AppTab.Records, 1), LinkTarget.ToTab(AppTab.Records, 2)));
        Assert.IsTrue(LensSides.Same(LinkTarget.ToTab(AppTab.Records, 1), LinkTarget.ToTab(AppTab.Records)), "an item below 0 matches any item");
        Assert.IsFalse(LensSides.Same(LinkTarget.None, LinkTarget.None));
        Assert.IsFalse(LensSides.Same(LinkTarget.None, A));
    }
}
