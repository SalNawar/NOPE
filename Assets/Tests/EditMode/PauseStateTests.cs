using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The game's pause sources (FLOW, BUG-34; the BELL-SIGN P2 requirement):
/// the clock runs only while no source holds it, a Full source (Settings,
/// focus loss) also pauses audio and input, a Clock source (the booth closed,
/// a citation slip) only the clock, and one source's release never restarts
/// a clock another holds, in any order.
/// </summary>
public class PauseStateTests
{
    [Test]
    public void PauseState_ClockScopeHoldsTheClockOnly_FullScopeHoldsBoth()
    {
        var pause = new PauseState();
        Assert.IsFalse(pause.ClockHeld);
        Assert.IsFalse(pause.FullHeld);

        pause.Request("booth", PauseScope.Clock);
        Assert.IsTrue(pause.ClockHeld);
        Assert.IsFalse(pause.FullHeld, "a booth closed by the AVAILABLE sign keeps audio and input running");

        pause.Request(PauseState.Settings, PauseScope.Full);
        Assert.IsTrue(pause.ClockHeld);
        Assert.IsTrue(pause.FullHeld);

        pause.Release(PauseState.Settings);
        Assert.IsTrue(pause.ClockHeld, "Settings' release never restarts the clock the booth holds");
        Assert.IsFalse(pause.FullHeld);

        pause.Release("booth");
        Assert.IsFalse(pause.ClockHeld);
    }

    [Test]
    public void PauseState_BoothSettingsFocusAndASlip_InEveryOrder_LeaveNoHold()
    {
        var sources = new[] { ("booth", PauseScope.Clock), (PauseState.Settings, PauseScope.Full), (PauseState.Focus, PauseScope.Full), (PauseState.Citation, PauseScope.Clock) };
        int orders = 0;
        foreach (int[] requests in Permutations(4))
            foreach (int[] releases in Permutations(4))
            {
                var pause = new PauseState();
                foreach (int i in requests)
                    pause.Request(sources[i].Item1, sources[i].Item2);
                for (int k = 0; k < releases.Length; k++)
                {
                    Assert.IsTrue(pause.ClockHeld, "the clock stays held while any source is left");
                    pause.Release(sources[releases[k]].Item1);
                    bool fullLeft = false;
                    for (int j = k + 1; j < releases.Length; j++)
                        fullLeft |= sources[releases[j]].Item2 == PauseScope.Full;
                    Assert.AreEqual(fullLeft, pause.FullHeld);
                }
                Assert.IsFalse(pause.ClockHeld, "0 leaks");
                Assert.IsFalse(pause.FullHeld, "0 leaks");
                Assert.AreEqual(0, pause.Count);
                orders++;
            }
        Assert.AreEqual(576, orders);
    }

    [Test]
    public void PauseState_ARepeatedRequestIsOneHold_AndAnUnknownReleaseIsIgnored()
    {
        var pause = new PauseState();
        pause.Request(PauseState.Citation, PauseScope.Clock);
        pause.Request(PauseState.Citation, PauseScope.Clock);
        Assert.AreEqual(1, pause.Count);
        pause.Release("nobody");
        Assert.IsTrue(pause.ClockHeld);
        pause.Release(PauseState.Citation);
        Assert.IsFalse(pause.ClockHeld);
        Assert.IsFalse(pause.Holds(PauseState.Citation));
    }

    [Test]
    public void PauseState_ARequestAgainWithAnotherScopeTakesIt()
    {
        var pause = new PauseState();
        pause.Request("x", PauseScope.Clock);
        pause.Request("x", PauseScope.Full);
        Assert.IsTrue(pause.FullHeld);
        pause.Request("x", PauseScope.Clock);
        Assert.IsFalse(pause.FullHeld);
        Assert.IsTrue(pause.ClockHeld);
    }

    [Test]
    public void PauseState_ChangedIsRaisedOnlyWhenTheHeldStateChanges()
    {
        var pause = new PauseState();
        int changes = 0;
        pause.Changed += () => changes++;
        pause.Request("a", PauseScope.Clock);
        Assert.AreEqual(1, changes);
        pause.Request("b", PauseScope.Clock);
        Assert.AreEqual(1, changes, "the clock was already held");
        pause.Request("c", PauseScope.Full);
        Assert.AreEqual(2, changes, "now audio and input too");
        pause.Release("a");
        pause.Release("b");
        Assert.AreEqual(2, changes);
        pause.Release("c");
        Assert.AreEqual(3, changes);
        pause.Release("c");
        Assert.AreEqual(3, changes);
    }

    [Test]
    public void PauseState_ClearReleasesEverySource_AndTheSharedOneExists()
    {
        var pause = new PauseState();
        pause.Request("a", PauseScope.Full);
        pause.Request("b", PauseScope.Clock);
        int changes = 0;
        pause.Changed += () => changes++;
        pause.Clear();
        Assert.AreEqual(0, pause.Count);
        Assert.IsFalse(pause.ClockHeld);
        Assert.AreEqual(1, changes);
        Assert.IsNotNull(PauseState.Shared);
        Assert.Throws<System.ArgumentException>(() => pause.Request("", PauseScope.Clock));
    }

    [Test]
    public void FocusLoss_PausesOnlyWhenTheGameDoesNotRunInTheBackground()
    {
        Assert.IsTrue(PauseState.PausesOnFocusLoss(false), "the player: runInBackground off");
        Assert.IsFalse(PauseState.PausesOnFocusLoss(true), "the QA rig and the editor job runner run in the background on purpose");
    }

    /// <summary>Every order of 0..n-1.</summary>
    private static IEnumerable<int[]> Permutations(int n)
    {
        var items = new List<int>();
        for (int i = 0; i < n; i++)
            items.Add(i);
        return Permute(items);
    }

    private static IEnumerable<int[]> Permute(List<int> items)
    {
        if (items.Count <= 1)
        {
            yield return items.ToArray();
            yield break;
        }
        for (int i = 0; i < items.Count; i++)
        {
            var rest = new List<int>(items);
            int first = rest[i];
            rest.RemoveAt(i);
            foreach (int[] tail in Permute(rest))
            {
                var all = new int[tail.Length + 1];
                all[0] = first;
                tail.CopyTo(all, 1);
                yield return all;
            }
        }
    }
}
