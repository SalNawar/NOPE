using NUnit.Framework;

/// <summary>
/// The desk's work mat (P0-A seam, DESK-PLAN's): a desk point is on the mat
/// inside it or on its edge.
/// </summary>
public class DeskMatTests
{
    [Test]
    public void DeskMat_IsOnMatInsideAndOnItsEdge()
    {
        var mat = new DeskMat(new DeskRect(0f, 0f, 0.4f, 0.3f));
        Assert.IsTrue(mat.IsOnMat(0f, 0f));
        Assert.IsTrue(mat.IsOnMat(0.2f, 0.15f));
        Assert.IsFalse(mat.IsOnMat(0.21f, 0f));
    }
}
