using NUnit.Framework;

/// <summary>
/// The wallet's changed signal (P0-A seam, UI-PC's; BUG-58a): a raise reaches
/// every listener once, and raising with no listener is safe.
/// </summary>
public class WalletEventsTests
{
    [Test]
    public void WalletEvents_RaiseCallsEveryListenerOnce()
    {
        int calls = 0;
        void Count() => calls++;
        WalletEvents.Changed += Count;
        try
        {
            WalletEvents.Raise();
        }
        finally
        {
            WalletEvents.Changed -= Count;
        }
        Assert.AreEqual(1, calls);
        Assert.DoesNotThrow(WalletEvents.Raise);
    }
}
