using NUnit.Framework;
using UnityEngine;

/// <summary>
/// TOOLS' lens sweep (ToolsOps.SweepEnds, pure; the night of 2026-10-28,
/// seam G): the sweep's ends are fractions of the screen, or of the named
/// target's screen rect, so the documents' leg hovers the paper's own
/// bounds wherever the desk's reading pose lays it.
/// </summary>
public class ToolsOpsTests
{
    [Test]
    public void WithoutATarget_TheEndsAreFractionsOfTheScreen()
    {
        (Vector2 a, Vector2 b) = ToolsOps.SweepEnds(null, 0.39f, 0.428f, 0.52f, 0.428f, new Vector2(1280f, 720f));
        Assert.AreEqual(new Vector2(0.39f * 1280f, 0.428f * 720f), a);
        Assert.AreEqual(new Vector2(0.52f * 1280f, 0.428f * 720f), b);
    }

    [Test]
    public void WithATarget_TheEndsAreFractionsOfItsRect()
    {
        var paper = new Rect(400f, 150f, 300f, 200f);
        (Vector2 a, Vector2 b) = ToolsOps.SweepEnds(paper, 0.2f, 0.3f, 0.8f, 0.7f, new Vector2(1280f, 720f));
        Assert.AreEqual(new Vector2(460f, 210f), a, "its lower left part");
        Assert.AreEqual(new Vector2(640f, 290f), b, "its upper right part");
        (Vector2 min, Vector2 max) = ToolsOps.SweepEnds(paper, 0f, 0f, 1f, 1f, new Vector2(1280f, 720f));
        Assert.AreEqual(paper.min, min, "0 is the rect's corner");
        Assert.AreEqual(paper.max, max, "1 the other");
    }
}
