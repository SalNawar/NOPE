using NUnit.Framework;

/// <summary>
/// The rulebook folder's home in the painted pocket (Saleh's playtest of
/// 2026-10-28, point 5: "isnt the big red binder for the folder? why is the
/// folder by default on the desk? still no place for it"): a let-go on the
/// pocket puts it away, anywhere else it rests on the desk; in the pocket it
/// shrinks under the book; its move eases.
/// </summary>
public class RulebookPocketTests
{
    /// <summary>The painted pocket's footprint (desk v3's rulebookPocket home: x -1.10 to -0.79, y -0.30 to -0.09).</summary>
    private static readonly DeskRect Pocket = new DeskRect(-0.945f, -0.195f, 0.31f, 0.21f);

    [Test]
    public void AFolderLetGoOnThePocket_GoesIn_ByThePointerOrByItsCentre()
    {
        Assert.IsTrue(RulebookPocket.Takes(Pocket, -0.94f, -0.2f, -0.6f, -0.1f), "the pointer on the pocket");
        Assert.IsTrue(RulebookPocket.Takes(Pocket, -0.6f, -0.1f, -0.95f, -0.18f), "the folder's centre on the pocket");
        Assert.IsTrue(RulebookPocket.Takes(Pocket, -0.77f, -0.2f, -0.5f, 0f), "just past its edge (Reach)");
    }

    [Test]
    public void AFolderLetGoOnTheMat_StaysOnTheDesk()
    {
        Assert.IsFalse(RulebookPocket.Takes(Pocket, -0.2f, 0f, -0.25f, -0.05f), "the mat's middle");
        Assert.IsFalse(RulebookPocket.Takes(Pocket, -0.70f, -0.2f, -0.55f, -0.2f), "the mat's left edge, beside the pocket");
        Assert.IsFalse(RulebookPocket.Takes(new DeskRect(0f, 0f, 0f, 0f), 0f, 0f, 0f, 0f), "no pocket");
    }

    [Test]
    public void InThePocket_TheFolderShrinksUnderTheBook_NeverPastIt_NeverGrowing()
    {
        float fit = RulebookPocket.Fit(0.27f, 0.17f, 0.23f, 0.25f);
        Assert.That(0.23f * fit, Is.LessThanOrEqualTo(0.27f * RulebookPocket.Under + 1e-5f));
        Assert.That(0.25f * fit, Is.LessThanOrEqualTo(0.17f * RulebookPocket.Under + 1e-5f), "its depth within the book's");
        Assert.AreEqual(1f, RulebookPocket.Fit(1f, 1f, 0.2f, 0.2f), "a book bigger than the folder: it keeps its size");
        Assert.AreEqual(RulebookPocket.LeastFit, RulebookPocket.Fit(0.01f, 0.01f, 0.2f, 0.2f), "never a speck");
        Assert.AreEqual(1f, RulebookPocket.Fit(0f, 0.1f, 0.2f, 0.2f), "no book measured: its own size");
    }

    [Test]
    public void TheMoveIntoAndOutOfThePocket_Eases_WithNoJolt()
    {
        Assert.AreEqual(0f, RulebookPocket.Ease(0f));
        Assert.AreEqual(1f, RulebookPocket.Ease(1f));
        Assert.AreEqual(0.5f, RulebookPocket.Ease(0.5f), 1e-6f);
        Assert.Less(RulebookPocket.Ease(0.05f), 0.05f, "it starts slow");
        Assert.Greater(RulebookPocket.Ease(0.95f), 0.95f, "it lands slow");
        Assert.AreEqual(1f, RulebookPocket.Ease(3f), "clamped");
    }
}
