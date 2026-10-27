using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// UI-PC's rig, the part that needs no game: uipcPowerOn's two motion passes
/// (<see cref="UiPcRigOps.EachMotion"/>), driven as the QA runner drives an
/// op (QaRunner.Guard over it, nested enumerators flattened), with passes
/// that throw as a timed-out wait does (QaGame.Until's TimeoutException).
/// Reduced Motion is a saved player preference, so whatever a pass does, both
/// passes' asserts are recorded and the player's choice is put back (fix
/// round 4, item 1; the reverify of 1eab563 found no run that showed it).
/// Pure: tools/qa/editor_runner runs it offline, Unity's EditMode suite too.
/// </summary>
public class UiPcRigTests
{
    /// <summary>The saved Reduced Motion preference, as EachMotion reads and writes it, with every write kept in order.</summary>
    private sealed class Preference
    {
        public bool Value;
        public readonly List<bool> Writes = new List<bool>();

        public Preference(bool chosen) => Value = chosen;

        public bool Read() => Value;

        public void Write(bool reduced)
        {
            Value = reduced;
            Writes.Add(reduced);
        }
    }

    /// <summary>A pass that waits <paramref name="frames"/> frames, then throws <paramref name="error"/> (none: it finishes); <paramref name="log"/> keeps the setting it ran at and whether it finished.</summary>
    private static IEnumerator Pass(Preference preference, List<string> log, int frames, Exception error)
    {
        log.Add((preference.Value ? "reduced" : "full") + " started");
        for (int i = 0; i < frames; i++)
            yield return null;
        if (error != null)
            throw error;
        log.Add((preference.Value ? "reduced" : "full") + " finished");
    }

    /// <summary>Drives <paramref name="op"/> to its end as QaRunner.Run drives a step (under QaRunner.Guard); returns the exception that escaped the op, or null.</summary>
    private static Exception Drive(IEnumerator op, out int frames)
    {
        Exception escaped = null;
        IEnumerator run = QaRunner.Guard(op, e => escaped = e);
        frames = 0;
        while (run.MoveNext())
            frames++;
        return escaped;
    }

    private static TimeoutException TimedOut() => new TimeoutException("waited 3 s for the PC frame to close");

    [Test]
    public void EachMotion_AWaitThatTimesOutInEachPass_RecordsBothPasses_AndPutsTheChoiceBack()
    {
        var preference = new Preference(false);
        var log = new List<string>();
        var stopped = new List<string>();
        IEnumerator op = UiPcRigOps.EachMotion(preference.Read, preference.Write,
            reduced => Pass(preference, log, 4, TimedOut()),
            (reduced, e) => stopped.Add((reduced ? "reduced" : "full") + ": " + e.GetType().Name + ": " + e.Message));

        Exception escaped = Drive(op, out int frames);

        Assert.IsNull(escaped, "no wait escapes the op: the runner would end the session inside it");
        CollectionAssert.AreEqual(new[] { "full started", "reduced started" }, log, "each pass ran, at the setting it names");
        CollectionAssert.AreEqual(new[] { "full: TimeoutException: waited 3 s for the PC frame to close", "reduced: TimeoutException: waited 3 s for the PC frame to close" }, stopped,
            "both passes' asserts are recorded, each with the exception's text");
        CollectionAssert.AreEqual(new[] { false, true, false }, preference.Writes, "full motion, Reduced Motion, then the player's choice");
        Assert.IsFalse(preference.Value, "the player's choice is put back");
        Assert.AreEqual(8, frames, "each pass's frames pass through to the runner");
    }

    [Test]
    public void EachMotion_APassThatThrowsAtOnce_TheOtherStillRuns_AndAReducedChoiceIsPutBack()
    {
        var preference = new Preference(true); // the player chose Reduced Motion
        var log = new List<string>();
        var stopped = new List<bool>();
        IEnumerator op = UiPcRigOps.EachMotion(preference.Read, preference.Write,
            reduced => Pass(preference, log, reduced ? 2 : 0, reduced ? null : new InvalidOperationException("no PC frame")),
            (reduced, e) => stopped.Add(reduced));

        Assert.IsNull(Drive(op, out _));
        CollectionAssert.AreEqual(new[] { "full started", "reduced started", "reduced finished" }, log, "the reduced pass runs after the full one stopped");
        CollectionAssert.AreEqual(new[] { false }, stopped, "only the pass that threw is recorded as stopped");
        CollectionAssert.AreEqual(new[] { false, true, true }, preference.Writes);
        Assert.IsTrue(preference.Value, "Reduced Motion stays the player's choice, not the last pass's");
    }

    [Test]
    public void EachMotion_PassesThatFinish_RecordNoStop_AndPutTheChoiceBack()
    {
        var preference = new Preference(true);
        var log = new List<string>();
        var stopped = new List<bool>();
        IEnumerator op = UiPcRigOps.EachMotion(preference.Read, preference.Write, reduced => Pass(preference, log, 3, null), (reduced, e) => stopped.Add(reduced));

        Assert.IsNull(Drive(op, out int frames));
        CollectionAssert.AreEqual(new[] { "full started", "full finished", "reduced started", "reduced finished" }, log);
        Assert.IsEmpty(stopped);
        CollectionAssert.AreEqual(new[] { false, true, true }, preference.Writes);
        Assert.AreEqual(6, frames);
    }

    [Test]
    public void EachMotion_DisposedDuringAPass_PutsTheChoiceBack()
    {
        // A run stopped from outside, by a harness that disposes the enumerators it stops (docs/pending/UI-PC.md, Requests: QA-RIG).
        var preference = new Preference(true);
        var log = new List<string>();
        IEnumerator op = UiPcRigOps.EachMotion(preference.Read, preference.Write, reduced => Pass(preference, log, 5, null), (reduced, e) => { });

        Assert.IsTrue(op.MoveNext(), "the op hands the first pass to the runner");
        Assert.IsInstanceOf<IEnumerator>(op.Current);
        Assert.IsFalse(preference.Value, "the first pass runs with full motion");
        ((IDisposable)op).Dispose();

        Assert.IsTrue(preference.Value, "disposed mid-pass, the op still puts the player's choice back (its finally)");
        CollectionAssert.AreEqual(new[] { false, true }, preference.Writes);
    }
}
