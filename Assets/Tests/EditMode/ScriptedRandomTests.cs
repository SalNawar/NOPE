using NUnit.Framework;

/// <summary>
/// The contract the golden-order tests rely on: a draw of the wrong kind or
/// past the end of the script fails the test, Range answers are offsets from
/// minInclusive clamped into the range, and Draws/Done track the script.
/// </summary>
public class ScriptedRandomTests
{
    [Test]
    public void ADrawOfTheWrongKind_FailsTheTest()
    {
        var onValue = new ScriptedRandom(ScriptStep.Value(0.5f));
        Assert.Throws<AssertionException>(() => onValue.Range(0, 3));

        var onRange = new ScriptedRandom(ScriptStep.Range(0));
        Assert.Throws<AssertionException>(() => onRange.Value());
    }

    [Test]
    public void ADrawPastTheEndOfTheScript_FailsTheTest()
    {
        var rng = new ScriptedRandom(ScriptStep.Range(0));
        rng.Range(0, 3);
        Assert.Throws<AssertionException>(() => rng.Range(0, 3));
        Assert.Throws<AssertionException>(() => rng.Value());
        Assert.Throws<AssertionException>(() => new ScriptedRandom().Value());
    }

    [Test]
    public void ARangeAnswer_IsAnOffsetFromMin_ClampedIntoTheRange()
    {
        var rng = new ScriptedRandom(ScriptStep.Range(1), ScriptStep.Range(5), ScriptStep.Range(-3));
        Assert.AreEqual(11, rng.Range(10, 13));
        Assert.AreEqual(12, rng.Range(10, 13), "clamped to maxExclusive - 1");
        Assert.AreEqual(10, rng.Range(10, 13), "clamped to minInclusive");
    }

    [Test]
    public void DrawsAndDone_TrackTheScript()
    {
        var rng = new ScriptedRandom(ScriptStep.Value(0.25f), ScriptStep.Range(0));
        Assert.AreEqual(0, rng.Draws);
        Assert.IsFalse(rng.Done);

        Assert.AreEqual(0.25f, rng.Value());
        Assert.AreEqual(1, rng.Draws);
        Assert.IsFalse(rng.Done);

        Assert.AreEqual(4, rng.Range(4, 9));
        Assert.AreEqual(2, rng.Draws);
        Assert.IsTrue(rng.Done);
    }
}
