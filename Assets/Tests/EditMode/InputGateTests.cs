using NUnit.Framework;

/// <summary>
/// The input gate (P0-A seam, RUN-SAFE's; BUG-68): the stub never blocks, as
/// today. RUN-SAFE replaces this with the overlay's behaviour.
/// </summary>
public class InputGateTests
{
    [Test]
    public void InputGate_StubNeverBlocks()
    {
        Assert.IsFalse(InputGate.Blocked);
    }
}
