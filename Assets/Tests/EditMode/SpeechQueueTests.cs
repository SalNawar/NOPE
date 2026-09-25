using NUnit.Framework;

/// <summary>
/// The speech bubble's pacing: lines type out, stay up once fully shown (the
/// hold when nothing follows, the minimum before a queued line), play in order
/// without cutting each other, and carry a premade's expression. A line may
/// carry a reveal time (piece 9's translation flip): it counts as fully shown
/// at the later of its typing and its reveal, and the reveal can be ended early.
/// </summary>
public class SpeechQueueTests
{
    /// <summary>10 characters a second, a 1 s minimum, a 4 s hold.</summary>
    private static SpeechQueue Queue() => new SpeechQueue(10f, 1f, 4f);

    [Test]
    public void Say_WhenIdle_StartsTheLineAtOnce_AndTypesItOut()
    {
        SpeechQueue q = Queue();
        Assert.IsFalse(q.Showing);
        Assert.AreEqual(0, q.LineNumber);

        q.Say("Hello there", null);
        Assert.IsTrue(q.Showing);
        Assert.AreEqual("Hello there", q.Text);
        Assert.AreEqual(1, q.LineNumber);
        Assert.AreEqual(0, q.VisibleCharacters);

        q.Tick(0.5f);
        Assert.AreEqual(5, q.VisibleCharacters);
        q.Tick(0.55f);
        Assert.AreEqual(10, q.VisibleCharacters);
        q.Tick(0.1f);
        Assert.AreEqual(11, q.VisibleCharacters, "complete at 1.1 s");
    }

    [Test]
    public void AtZeroCharactersASecond_TheWholeLineShowsAtOnce()
    {
        var q = new SpeechQueue(0f, 1f, 4f);
        q.Say("Here you are.", null);
        Assert.AreEqual(13, q.VisibleCharacters);
    }

    [Test]
    public void ALoneLine_StaysTheHoldOnceFullyShown_ThenNothingShows()
    {
        SpeechQueue q = Queue();
        q.Say("Hi", null);
        q.Tick(4.19f);
        Assert.IsTrue(q.Showing, "typed in 0.2 s, then up 4 s");
        q.Tick(0.02f);
        Assert.IsFalse(q.Showing);
        Assert.IsNull(q.Text);
        Assert.AreEqual(0, q.VisibleCharacters);
        Assert.AreEqual(1, q.LineNumber);
    }

    [Test]
    public void AQueuedLine_ReplacesTheCurrentOne_TheMinimumAfterItIsFullyShown()
    {
        SpeechQueue q = Queue();
        q.Say("Hi", null);
        q.Say("Bye", null);
        Assert.AreEqual("Hi", q.Text, "queued, not shown");
        Assert.AreEqual(1, q.LineNumber);

        q.Tick(1.19f);
        Assert.AreEqual("Hi", q.Text);
        q.Tick(0.02f);
        Assert.AreEqual("Bye", q.Text);
        Assert.AreEqual(2, q.LineNumber);
        Assert.AreEqual(0, q.VisibleCharacters, "0.01 s into the new line");
    }

    [Test]
    public void ANewReply_NeverCutsALine_BeforeItsMinimum()
    {
        SpeechQueue q = Queue();
        q.Say("Hello", null);
        q.Tick(0.3f);
        q.Say("Bye", null);
        Assert.AreEqual("Hello", q.Text, "still typing");
        q.Tick(1.19f);
        Assert.AreEqual("Hello", q.Text, "typed at 0.5 s, the minimum ends at 1.5 s");
        q.Tick(0.02f);
        Assert.AreEqual("Bye", q.Text);
    }

    [Test]
    public void ANewReply_DuringTheHold_ReplacesTheLineAtOnce_OnceItsMinimumHasPassed()
    {
        SpeechQueue q = Queue();
        q.Say("Hi", null);
        q.Tick(2f);
        q.Say("Bye", null);
        Assert.AreEqual("Bye", q.Text);
        Assert.AreEqual(2, q.LineNumber);
        Assert.AreEqual(0, q.VisibleCharacters, "starts from its first character");
    }

    [Test]
    public void ALongTick_PassesSeveralLines_InOrder()
    {
        SpeechQueue q = Queue();
        q.Say("Hi", "happy");
        q.Say("Hm", "worried");
        q.Say("Ok", null);

        q.Tick(2.55f);
        Assert.AreEqual("Ok", q.Text, "Hi ends at 1.2 s, Hm at 2.4 s");
        Assert.AreEqual(3, q.LineNumber);
        Assert.AreEqual(1, q.VisibleCharacters, "0.15 s into the last line");
        Assert.AreEqual("worried", q.Expression, "the latest started line that carries one");

        q.Tick(100f);
        Assert.IsFalse(q.Showing);
        Assert.AreEqual("worried", q.Expression, "what was said stays said");
    }

    [Test]
    public void BlankLines_AreIgnored()
    {
        SpeechQueue q = Queue();
        q.Say(null, "happy");
        q.Say("   ", "angry");
        Assert.IsFalse(q.Showing);
        Assert.AreEqual(0, q.LineNumber);
        Assert.IsNull(q.Expression);
    }

    [Test]
    public void Expression_IsTheLatestStartedLinesExpression_ABlankOneKeepsIt()
    {
        SpeechQueue q = new SpeechQueue(0f, 0.5f, 1f);
        q.Say("One.", null);
        Assert.IsNull(q.Expression, "none said yet");
        q.Say("Two.", "happy");
        Assert.IsNull(q.Expression, "Two. is queued, not started");
        q.Tick(0.5f);
        Assert.AreEqual("happy", q.Expression);
        q.Say("Three.", "");
        q.Tick(0.5f);
        Assert.AreEqual("Three.", q.Text);
        Assert.AreEqual("happy", q.Expression, "a line without one keeps the face");
    }

    [Test]
    public void Clear_DropsEverything_ReturningTheLastExpressionNotYetShown()
    {
        SpeechQueue q = Queue();
        q.Say("Hi", "happy");
        q.Say("Hm", "worried");
        q.Say("Ok", "");
        Assert.AreEqual("worried", q.Clear(), "the face catches up with what was said");
        Assert.IsFalse(q.Showing);
        Assert.IsNull(q.Text);
        Assert.IsNull(q.Expression, "a new traveller starts with no expression");
        Assert.AreEqual(1, q.LineNumber, "never counts back");

        q.Say("Next.", null);
        Assert.AreEqual("Next.", q.Text, "starts at once");
        Assert.AreEqual(2, q.LineNumber);
        Assert.IsNull(q.Clear(), "the current line's expression, if any, was applied when it started");
        Assert.IsNull(Queue().Clear(), "nothing said");
    }

    [Test]
    public void Knobs_NegativeCountAsZero()
    {
        var q = new SpeechQueue(-5f, -1f, -2f);
        q.Say("Hi", null);
        Assert.AreEqual(2, q.VisibleCharacters, "no typing");
        q.Tick(0.001f);
        Assert.IsFalse(q.Showing, "no hold");
    }

    [Test]
    public void TheMinimum_NeverExceedsTheHold()
    {
        var q = new SpeechQueue(10f, 9f, 2f);
        q.Say("Hi", null);
        q.Say("Yo", null);
        q.Tick(2.19f);
        Assert.AreEqual("Hi", q.Text);
        q.Tick(0.02f);
        Assert.AreEqual("Yo", q.Text, "replaced at the hold, 2.2 s, not the 9 s minimum");
    }

    [Test]
    public void ARevealLongerThanTheTyping_DelaysTheHold_AShorterOneChangesNothing()
    {
        SpeechQueue q = Queue();
        q.Say("Hi", null, 2f);
        q.Tick(5.99f);
        Assert.IsTrue(q.Showing, "revealed at 2 s, then held 4 s");
        q.Tick(0.02f);
        Assert.IsFalse(q.Showing);

        SpeechQueue shorter = Queue();
        shorter.Say("Hello", null, 0.1f);
        shorter.Tick(4.49f);
        Assert.IsTrue(shorter.Showing, "typed at 0.5 s, then held 4 s");
        shorter.Tick(0.02f);
        Assert.IsFalse(shorter.Showing);
    }

    [Test]
    public void AQueuedLine_WaitsForTheRevealPlusTheMinimum()
    {
        SpeechQueue q = Queue();
        q.Say("Hi", null, 2f);
        q.Say("Bye", null);
        q.Tick(2.99f);
        Assert.AreEqual("Hi", q.Text);
        q.Tick(0.02f);
        Assert.AreEqual("Bye", q.Text, "the reveal ends at 2 s, the minimum at 3 s");
    }

    [Test]
    public void LineSeconds_CountsFromTheLineStart_AndRestartsWithTheNextLine()
    {
        SpeechQueue q = Queue();
        Assert.AreEqual(0f, q.LineSeconds, "nothing shows");
        q.Say("Hi", null);
        q.Say("Bye", null);
        q.Tick(0.7f);
        Assert.AreEqual(0.7f, q.LineSeconds, 1e-5f);
        q.Tick(0.6f);
        Assert.AreEqual("Bye", q.Text);
        Assert.AreEqual(0.1f, q.LineSeconds, 1e-5f, "Hi ended at 1.2 s");
        q.Tick(100f);
        Assert.AreEqual(0f, q.LineSeconds);
    }

    [Test]
    public void EndReveal_StartsTheHoldNow_ButNeverBeforeTheTypingEnds()
    {
        SpeechQueue q = Queue();
        q.Say("Hi", null, 3f);
        q.Tick(1f);
        q.EndReveal();
        q.Tick(3.99f);
        Assert.IsTrue(q.Showing, "held 4 s from the skip at 1 s");
        q.Tick(0.02f);
        Assert.IsFalse(q.Showing);

        SpeechQueue typing = Queue();
        typing.Say("Hello there", null, 3f);
        typing.Tick(0.5f);
        typing.EndReveal();
        typing.Tick(4.59f);
        Assert.IsTrue(typing.Showing, "typed at 1.1 s, then held 4 s");
        typing.Tick(0.02f);
        Assert.IsFalse(typing.Showing);

        SpeechQueue idle = Queue();
        idle.EndReveal();
        Assert.IsFalse(idle.Showing, "nothing to end");
    }

    [Test]
    public void NegativeOrNaNReveals_CountAsZero()
    {
        foreach (float reveal in new[] { -2f, float.NaN })
        {
            SpeechQueue q = Queue();
            q.Say("Hi", null, reveal);
            q.Tick(4.19f);
            Assert.IsTrue(q.Showing, reveal.ToString());
            q.Tick(0.02f);
            Assert.IsFalse(q.Showing, reveal.ToString());
        }
    }

    [Test]
    public void NonPositiveTicks_ChangeNothing()
    {
        SpeechQueue q = Queue();
        q.Say("Hello", null);
        q.Tick(0.25f);
        q.Tick(0f);
        q.Tick(-3f);
        q.Tick(float.NaN);
        Assert.AreEqual(2, q.VisibleCharacters);
        Assert.AreEqual("Hello", q.Text);
    }
}
