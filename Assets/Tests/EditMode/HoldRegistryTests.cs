using NUnit.Framework;

/// <summary>
/// The desk's one current hold (DOC-HANDLE, BUG-55): a new hold cancels the
/// old one, losing the focus cancels the hold (Alt+Tab never commits it), a
/// view key while holding cancels it first (ControlRules), a hold its holder
/// ended is not cancelled.
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

    [Test]
    public void AViewKeyWhileHolding_CancelsTheHoldFirst_AndChangesNoView()
    {
        var registry = new HoldRegistry();
        var held = new Held();
        registry.Begin(held);
        // OfficeControls reads the registry into the controls' state, and applies the rules' answer.
        var state = new ControlState(travellerAtDesk: true, dragging: registry.IsHolding);
        foreach (ControlKey key in new[] { ControlKey.Pc, ControlKey.Stamps, ControlKey.CityLeft, ControlKey.Help })
        {
            Assert.AreEqual(ControlAction.CancelHold, ControlRules.Resolve(key, state), key.ToString());
        }
        registry.Cancel(); // CancelHold
        Assert.AreEqual(1, held.Cancels, "let go: back where it was picked up, nothing committed");
        Assert.IsFalse(registry.IsHolding);
        Assert.AreEqual(ControlAction.OpenPc, ControlRules.Resolve(ControlKey.Pc, new ControlState(travellerAtDesk: true, dragging: registry.IsHolding)),
            "the next press does what it says");
    }

    [Test]
    public void LosingTheFocus_CancelsWithoutCommitting_AndOnlyOnce()
    {
        var registry = new HoldRegistry();
        var held = new Held();
        registry.Begin(held);
        registry.OnFocusLost();
        registry.OnFocusLost();
        Assert.AreEqual(1, held.Cancels, "cancelled once: the Input System's release that follows finds nothing held");
        registry.End(held);
        Assert.IsFalse(registry.IsHolding);
    }
}
