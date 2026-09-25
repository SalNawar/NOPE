using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The letter flip's timing (piece 9 T8): letters start in reading order, a
/// row later per document row, each passes through its scramble steps and
/// lands; the output changes exactly at the change points.
/// </summary>
public class FlipSequenceTests
{
    /// <summary>The spec's knobs: 0.3 s delay, 0.04 s between letters, 0.12 s to land, 2 steps, 0.15 s per row.</summary>
    private static FlipTiming T() => new FlipTiming { startDelay = 0.3f, letterInterval = 0.04f, letterSeconds = 0.12f, scrambleSteps = 2, rowStagger = 0.15f };

    private static CellState State(int rank, int row, float elapsed, FlipTiming t = null) =>
        FlipSequence.StateAt(rank, row, t ?? T(), elapsed, out _);

    [Test]
    public void BeforeTheDelay_EveryLetterIsForeign()
    {
        for (int rank = 0; rank < 30; rank++)
        {
            Assert.AreEqual(CellState.Foreign, State(rank, 0, 0f));
            Assert.AreEqual(CellState.Foreign, State(rank, 0, 0.299f));
        }
        Assert.AreEqual(CellState.Foreign, State(0, 0, float.NaN), "an unrevealed text (NaN) is foreign");
    }

    [Test]
    public void LettersStartInRankOrder_ALetterIntervalApart_ARowLater()
    {
        Assert.AreEqual(0.3f, FlipSequence.StartOf(0, 0, T()), 1e-6f);
        Assert.AreEqual(0.34f, FlipSequence.StartOf(1, 0, T()), 1e-6f);
        Assert.AreEqual(0.3f + 19 * 0.04f, FlipSequence.StartOf(19, 0, T()), 1e-6f);
        Assert.AreEqual(0.45f, FlipSequence.StartOf(0, 1, T()), 1e-6f);
        Assert.AreEqual(0.6f + 0.08f, FlipSequence.StartOf(2, 2, T()), 1e-6f);

        Assert.AreEqual(CellState.Flipping, State(1, 0, 0.341f));
        Assert.AreEqual(CellState.Foreign, State(2, 0, 0.341f), "the next letter has not started");
        Assert.AreEqual(CellState.Foreign, State(0, 1, 0.449f), "the second row starts a stagger later");
    }

    [Test]
    public void ALetter_PassesThroughItsSteps_AndLandsAfterLetterSeconds()
    {
        Assert.AreEqual(CellState.Flipping, FlipSequence.StateAt(0, 0, T(), 0.301f, out int first));
        Assert.AreEqual(0, first);
        Assert.AreEqual(CellState.Flipping, FlipSequence.StateAt(0, 0, T(), 0.361f, out int second));
        Assert.AreEqual(1, second);
        Assert.AreEqual(CellState.Flipping, FlipSequence.StateAt(0, 0, T(), 0.419f, out int last));
        Assert.AreEqual(1, last);
        Assert.AreEqual(CellState.English, State(0, 0, 0.4201f));
        Assert.AreEqual(CellState.English, State(0, 0, 100f));
    }

    [Test]
    public void ZeroScrambleSteps_GoStraightToEnglish()
    {
        FlipTiming t = T();
        t.scrambleSteps = 0;
        Assert.AreEqual(CellState.Foreign, State(0, 0, 0.35f, t), "no glyph between: the foreign letter stays until it lands");
        Assert.AreEqual(CellState.English, State(0, 0, 0.4201f, t));
    }

    [Test]
    public void ALetter_NeverGoesBack()
    {
        FlipTiming t = T();
        int previous = -1;
        for (float e = 0f; e < 1.5f; e += 0.001f)
        {
            CellState state = FlipSequence.StateAt(3, 1, t, e, out int step);
            int phase = state == CellState.Foreign ? 0 : state == CellState.English ? 99 : 1 + step;
            Assert.GreaterOrEqual(phase, previous, $"at {e}");
            previous = phase;
        }
        Assert.AreEqual(99, previous);
    }

    [Test]
    public void Duration_IsTheLastLanding_AndZeroForNoLetters()
    {
        Assert.AreEqual(1.18f, FlipSequence.Duration(20, 0, T()), 1e-5f, "a 20-letter value: 0.3 + 19 x 0.04 + 0.12");
        Assert.AreEqual(1.53f, FlipSequence.Duration(25, 1, T()), 1e-5f, "the passport's second foreign row");
        Assert.AreEqual(2.10f, FlipSequence.Duration(43, 0, T()), 1e-5f, "the arrival claim's 43 letters");
        Assert.AreEqual(0f, FlipSequence.Duration(0, 3, T()));
        Assert.AreEqual(CellState.English, State(19, 0, FlipSequence.Duration(20, 0, T())));
    }

    [Test]
    public void Progress_IsMonotonic_AndChangesExactlyAtTheChangePoints()
    {
        FlipTiming t = T();
        const int letters = 5;
        const int row = 1;
        var points = new List<float>();
        for (int rank = 0; rank < letters; rank++)
        {
            float start = FlipSequence.StartOf(rank, row, t);
            points.Add(start);
            points.Add(start + 0.06f);
            points.Add(start + 0.12f);
        }

        foreach (float p in points)
            Assert.Less(FlipSequence.Progress(letters, row, t, p - 0.001f), FlipSequence.Progress(letters, row, t, p + 0.001f), $"a change at {p}");

        int previous = 0;
        int changes = 0;
        for (float e = 0f; e < 2f; e += 0.0005f)
        {
            int now = FlipSequence.Progress(letters, row, t, e);
            Assert.GreaterOrEqual(now, previous);
            if (now != previous)
                changes++;
            previous = now;
        }
        Assert.AreEqual(points.Select(p => System.Math.Round(p, 4)).Distinct().Count(), changes, "no change between the points (a landing and a later start may coincide)");
        Assert.AreEqual(points.Count, previous, "every letter started, stepped and landed");
        Assert.AreEqual(0, FlipSequence.Progress(letters, row, t, 0f));
        Assert.AreEqual(0, FlipSequence.Progress(0, row, t, 5f));
    }

    [Test]
    public void NegativeKnobs_CountAsZero()
    {
        var t = new FlipTiming { startDelay = -1f, letterInterval = -1f, letterSeconds = -1f, scrambleSteps = -3, rowStagger = -1f };
        Assert.AreEqual(0f, FlipSequence.StartOf(5, 5, t));
        Assert.AreEqual(0f, FlipSequence.Duration(10, 2, t));
        Assert.AreEqual(CellState.English, State(9, 2, 0f, t));
    }

    [Test]
    public void ScrambleIndex_DiffersFromTheLetter_AndStaysInRange()
    {
        for (int letter = 0; letter < 26; letter++)
        {
            for (int step = 0; step < 4; step++)
            {
                int index = FlipSequence.ScrambleIndex(letter, step);
                Assert.That(index, Is.InRange(0, 25));
                Assert.AreNotEqual(letter, index);
            }
            Assert.AreNotEqual(FlipSequence.ScrambleIndex(letter, 0), FlipSequence.ScrambleIndex(letter, 1));
        }
    }
}
