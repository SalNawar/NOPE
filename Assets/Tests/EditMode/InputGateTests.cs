using NUnit.Framework;

/// <summary>
/// The input gate (RUN-SAFE, BUG-68a): game input is blocked exactly while
/// an overlay (the cheat panel) holds it, each overlay's hold is its own, and
/// a hold taken twice or a release never held changes nothing.
/// </summary>
public class InputGateTests
{
    /// <summary>Every test starts and ends with nobody holding the gate (it is static).</summary>
    [SetUp, TearDown]
    public void Free()
    {
        InputGate.Release("panel");
        InputGate.Release("other");
    }

    [Test]
    public void NobodyHoldingIt_TheGameTakesInput()
    {
        Assert.IsFalse(InputGate.Blocked);
    }

    [Test]
    public void TheOpenPanel_BlocksInput_UntilItCloses()
    {
        InputGate.Hold("panel");
        Assert.IsTrue(InputGate.Blocked);
        InputGate.Release("panel");
        Assert.IsFalse(InputGate.Blocked);
    }

    [Test]
    public void AHoldTakenTwice_IsReleasedOnce()
    {
        InputGate.Hold("panel");
        InputGate.Hold("panel");
        InputGate.Release("panel");
        Assert.IsFalse(InputGate.Blocked);
    }

    [Test]
    public void OneOverlayClosing_NeverFreesAnothersHold()
    {
        InputGate.Hold("panel");
        InputGate.Hold("other");
        InputGate.Release("panel");
        Assert.IsTrue(InputGate.Blocked, "the other overlay still holds the input");
        InputGate.Release("other");
        Assert.IsFalse(InputGate.Blocked);
    }

    [Test]
    public void ReleasingAHoldNeverTaken_OrAnUnnamedOne_ChangesNothing()
    {
        InputGate.Release("panel");
        InputGate.Hold(null);
        InputGate.Hold(string.Empty);
        Assert.IsFalse(InputGate.Blocked);
        InputGate.Hold("panel");
        InputGate.Release(null);
        Assert.IsTrue(InputGate.Blocked);
        InputGate.Release("panel");
    }
}
