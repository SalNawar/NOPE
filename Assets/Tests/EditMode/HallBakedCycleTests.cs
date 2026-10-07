using NUnit.Framework;

public sealed class HallBakedCycleTests
{
    [TestCase(8,1,0,0,0)]
    [TestCase(12,0,1,0,0)]
    [TestCase(16.5f,0,0,1,0)]
    [TestCase(22,0,0,0,1)]
    [TestCase(3,0,0,0,1)]
    [TestCase(5.5f,0,0,0,1)]
    [TestCase(19.5f,0,0,0,1)]
    [TestCase(-1,0,0,0,1)]
    [TestCase(6.75f,.5f,0,0,.5f)]
    [TestCase(10,.5f,.5f,0,0)]
    [TestCase(14.25f,0,.5f,.5f,0)]
    [TestCase(18,0,0,.5f,.5f)]
    public void KeyStatesAndHeldNight(float hour,float a,float b,float c,float d)
    {
        var w=HallBakedCycle.Weights(hour);
        Assert.AreEqual(a,w.x,1e-5);Assert.AreEqual(b,w.y,1e-5);
        Assert.AreEqual(c,w.z,1e-5);Assert.AreEqual(d,w.w,1e-5);
    }
    [Test]
    public void WholeCycleIsContinuousNormalizedAndCyclic()
    {
        for(float h=-24;h<48;h+=.025f)
        {
            var w=HallBakedCycle.Weights(h);
            Assert.AreEqual(1,w.x+w.y+w.z+w.w,1e-5);
            Assert.That(w.x,Is.InRange(0f,1f));Assert.That(w.y,Is.InRange(0f,1f));
            Assert.That(w.z,Is.InRange(0f,1f));Assert.That(w.w,Is.InRange(0f,1f));
            var next=HallBakedCycle.Weights(h+.001f);
            Assert.Less(System.Math.Abs(w.x-next.x)+System.Math.Abs(w.y-next.y)+System.Math.Abs(w.z-next.z)+System.Math.Abs(w.w-next.w),.005);
            var repeat=HallBakedCycle.Weights(h+24);
            Assert.AreEqual(w.x,repeat.x,1e-5);Assert.AreEqual(w.w,repeat.w,1e-5);
        }
    }
    [Test] public void BadClockDefaultsToNoon()=>Assert.AreEqual(1,HallBakedCycle.Weights(float.NaN).y);
    [Test] public void InfiniteClockDefaultsToNoon()
    {
        Assert.AreEqual(1,HallBakedCycle.Weights(float.PositiveInfinity).y);
        Assert.AreEqual(1,HallBakedCycle.Weights(float.NegativeInfinity).y);
    }
}
