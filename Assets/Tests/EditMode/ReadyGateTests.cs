using NUnit.Framework;

public class ReadyGateTests
{
    [Test]
    public void Arm_ThenRelease_FiresReleasedOnce_AndDisarms()
    {
        var gate = new ReadyGate();
        int released = 0;
        gate.Released += () => released++;

        gate.Arm();
        Assert.IsTrue(gate.IsArmed);

        gate.Release();
        Assert.AreEqual(1, released);
        Assert.IsFalse(gate.IsArmed);
    }

    [Test]
    public void Release_WhenNotArmed_DoesNothing()
    {
        var gate = new ReadyGate();
        int released = 0;
        gate.Released += () => released++;

        gate.Release();

        Assert.AreEqual(0, released);
    }
}
