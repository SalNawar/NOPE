using NUnit.Framework;

public class ShiftFlowTests
{
    [Test]
    public void Closing_WithTravellerAtDesk_FinishesCurrent()
    {
        Assert.AreEqual(ClosingAction.FinishCurrent, ShiftFlow.OnClosing(travellerAtDesk: true));
    }

    [Test]
    public void Closing_WithNobodyAtDesk_ClosesNow()
    {
        Assert.AreEqual(ClosingAction.CloseNow, ShiftFlow.OnClosing(travellerAtDesk: false));
    }
}
