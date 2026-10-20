using NUnit.Framework;

/// <summary>
/// The desk's one current hold (P0-A seam, DOC-HANDLE's): a new hold cancels
/// the old one, losing the focus cancels the hold, a hold its holder ended is
/// not cancelled.
/// </summary>
public class HoldRegistryTests
{
    private sealed class Held : IHoldable
    {
        public int Cancels;

        public void CancelHold() => Cancels++;
    }

    [Test]
    public void HoldRegistry_ANewHoldCancelsTheOldOneAndFocusLossCancelsTheHold()
    {
        var registry = new HoldRegistry();
        var first = new Held();
        var second = new Held();
        Assert.IsFalse(registry.IsHolding);

        registry.Begin(first);
        registry.Begin(second);
        Assert.AreEqual(1, first.Cancels);
        Assert.AreSame(second, registry.Current);

        registry.OnFocusLost();
        Assert.AreEqual(1, second.Cancels);
        Assert.IsFalse(registry.IsHolding);

        registry.Begin(first);
        registry.End(first);
        registry.Cancel();
        Assert.AreEqual(1, first.Cancels, "a hold ended by its holder is not cancelled");
        Assert.IsNotNull(HoldRegistry.Desk);
    }
}
