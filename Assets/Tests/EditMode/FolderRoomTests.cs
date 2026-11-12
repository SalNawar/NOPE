using NUnit.Framework;

/// <summary>
/// D66 rule 1 (FolderRoom.Aside): a desk paper under the rulebook folder,
/// as it comes out onto the mat or opens, slides the shorter way along the
/// desk's right or left that clears the folder and stays on the mat. The
/// numbers are the day 1 desk at the 1.8 sizes: the mat 1.43 m wide, the
/// open folder 0.83 m wide, the passport 0.146 by 0.222 m.
/// </summary>
public class FolderRoomTests
{
    private const float Eps = 1e-5f;

    private static readonly (float x, float y, float w, float h) Mat = (0f, 0f, 1.43f, 0.9f);
    private static readonly (float x, float y, float w, float h) OpenFolder = (-0.1f, 0f, 0.83f, 0.42f);

    [Test]
    public void APaperOnTheFoldersRightHalfMovesRightAndClearsIt()
    {
        var passport = (x: 0.2f, y: 0.02f, w: 0.146f, h: 0.222f);
        (float x, float y) = FolderRoom.Aside(passport, OpenFolder, Mat);
        Assert.AreEqual(-0.1f + 0.415f + 0.073f, x, Eps, "its left edge on the folder's right edge");
        Assert.AreEqual(passport.y, y, Eps, "it slides along the desk's right only");
    }

    [Test]
    public void APaperLeftOfTheFoldersMiddleMovesLeftWhenThatIsShorterAndStaysOnTheMat()
    {
        var folder = (x: 0.2f, y: 0f, w: 0.6f, h: 0.42f);
        var passport = (x: -0.05f, y: 0f, w: 0.146f, h: 0.222f);
        (float x, _) = FolderRoom.Aside(passport, folder, Mat);
        Assert.AreEqual(-0.1f - 0.073f, x, Eps, "its right edge on the folder's left edge");
        Assert.GreaterOrEqual(x - 0.073f, -0.715f - Eps, "it stays on the mat");
    }

    [Test]
    public void AShorterMoveThatLeavesTheMatGivesWayToTheOtherSide()
    {
        // left of the folder's middle, but the mat's left edge is too near: it goes right
        var folder = (x: -0.4f, y: 0f, w: 0.6f, h: 0.42f);
        var passport = (x: -0.6f, y: 0f, w: 0.146f, h: 0.222f);
        (float x, _) = FolderRoom.Aside(passport, folder, Mat);
        Assert.AreEqual(-0.1f + 0.073f, x, Eps);
    }

    [Test]
    public void APaperThatDoesNotOverlapDoesNotMove()
    {
        var beside = (x: 0.5f, y: 0f, w: 0.146f, h: 0.222f);
        var behind = (x: -0.1f, y: 0.35f, w: 0.146f, h: 0.222f);
        Assert.AreEqual((0.5f, 0f), FolderRoom.Aside(beside, OpenFolder, Mat));
        Assert.AreEqual((-0.1f, 0.35f), FolderRoom.Aside(behind, OpenFolder, Mat));
    }

    [Test]
    public void WithNoRoomItStopsAtTheMatsEdge()
    {
        var narrow = (x: 0f, y: 0f, w: 0.9f, h: 0.9f);
        var folder = (x: 0f, y: 0f, w: 0.83f, h: 0.42f);
        var passport = (x: 0.1f, y: 0f, w: 0.146f, h: 0.222f);
        (float x, _) = FolderRoom.Aside(passport, folder, narrow);
        Assert.AreEqual(0.45f - 0.073f, x, Eps, "its right edge on the mat's right edge");
    }
}
