using NUnit.Framework;

public class DaySlotSequencerTests
{
    [Test]
    public void NewDay_StartsAtSlotOne_AndCanStart()
    {
        var slots = new DaySlotSequencer(3);
        Assert.AreEqual(1, slots.CurrentSlot);
        Assert.IsTrue(slots.CanStartSlot);
        Assert.IsFalse(slots.IsWaiting);
    }

    [Test]
    public void NonPositiveQueue_IsTreatedAsOneSlot()
    {
        var slots = new DaySlotSequencer(0);
        Assert.AreEqual(1, slots.TotalSlots);
    }

    [Test]
    public void ResolvingEverySlot_EndsTheQueue()
    {
        var slots = new DaySlotSequencer(2);
        for (int i = 0; i < 2; i++)
        {
            slots.BeginWaiting();
            slots.MarkResolved();
            Assert.IsFalse(slots.SlotAbandoned);
            slots.Advance();
        }
        Assert.IsFalse(slots.CanStartSlot);
        Assert.IsFalse(slots.CloseRequested);
    }

    [Test]
    public void CloseNow_WhileWaitingBehindReady_AbandonsTheSlot()
    {
        var slots = new DaySlotSequencer(5);
        slots.BeginWaiting();
        slots.CloseNow();
        Assert.IsFalse(slots.IsWaiting, "the wait is released");
        Assert.IsTrue(slots.SlotAbandoned, "slot-ended and after-case events are skipped");
        Assert.IsFalse(slots.CanStartSlot);
    }

    [Test]
    public void CloseNow_BetweenSlots_LetsTheFinishedSlotEnd_ThenStops()
    {
        var slots = new DaySlotSequencer(5);
        slots.BeginWaiting();
        slots.MarkResolved();
        slots.CloseNow(); // e.g. during after-case events
        Assert.IsFalse(slots.SlotAbandoned);
        slots.Advance();
        Assert.IsFalse(slots.CanStartSlot);
    }

    [Test]
    public void CloseNow_DuringBeforeCaseEvents_StopsBeforeTheSlotStarts()
    {
        var slots = new DaySlotSequencer(5);
        slots.CloseNow(); // nothing waiting yet
        Assert.IsTrue(slots.CloseRequested);
        Assert.IsFalse(slots.CanStartSlot);
        Assert.IsFalse(slots.SlotAbandoned);
    }

    [Test]
    public void CloseAfterCurrentSlot_KeepsTheTravellerAtTheDesk_UntilResolved()
    {
        var slots = new DaySlotSequencer(5);
        slots.BeginWaiting();
        slots.CloseAfterCurrentSlot();
        Assert.IsTrue(slots.IsWaiting, "the player finishes the current traveller");
        slots.MarkResolved();
        Assert.IsFalse(slots.SlotAbandoned);
        slots.Advance();
        Assert.IsFalse(slots.CanStartSlot);
    }

    [Test]
    public void BeginWaiting_ClearsAnEarlierAbandon()
    {
        var slots = new DaySlotSequencer(5);
        slots.BeginWaiting();
        slots.CloseNow();
        slots.BeginWaiting();
        Assert.IsFalse(slots.SlotAbandoned);
        Assert.IsTrue(slots.IsWaiting);
    }
}
