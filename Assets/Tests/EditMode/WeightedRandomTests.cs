using NUnit.Framework;

public class WeightedRandomTests
{
    private sealed class Item
    {
        public string name;
        public float weight;
    }

    [Test]
    public void ZeroWeightItem_IsNeverPicked_EvenOnARollOfZero()
    {
        var items = new[] { new Item { name = "a", weight = 0f }, new Item { name = "b", weight = 1f } };
        Assert.AreEqual("b", WeightedRandom.Pick(items, i => i.weight, new ScriptedRandom(ScriptStep.Value(0f))).name);
    }

    [Test]
    public void AllZeroOrEmpty_ReturnsDefault()
    {
        var items = new[] { new Item { name = "a", weight = 0f } };
        Assert.IsNull(WeightedRandom.Pick(items, i => i.weight, new ScriptedRandom(ScriptStep.Value(0.5f))));
        Assert.IsNull(WeightedRandom.Pick(new Item[0], i => i.weight, new ScriptedRandom(ScriptStep.Value(0.5f))));
        Assert.IsNull(WeightedRandom.Pick<Item>(null, i => i.weight, new ScriptedRandom(ScriptStep.Value(0.5f))));
    }

    [Test]
    public void SingleItem_IsAlwaysPicked()
    {
        var items = new[] { new Item { name = "only", weight = 2f } };
        Assert.AreEqual("only", WeightedRandom.Pick(items, i => i.weight, new ScriptedRandom(ScriptStep.Value(0.999f))).name);
    }

    [TestCase(0.0f, "a")]
    [TestCase(0.24f, "a")]
    [TestCase(0.25f, "b")]
    [TestCase(0.99f, "b")]
    public void Roll_MapsOntoCumulativeWeights(float roll, string expected)
    {
        var items = new[] { new Item { name = "a", weight = 1f }, new Item { name = "b", weight = 3f } };
        Assert.AreEqual(expected, WeightedRandom.Pick(items, i => i.weight, new ScriptedRandom(ScriptStep.Value(roll))).name);
    }

    [Test]
    public void Proportions_FollowWeights()
    {
        var items = new[] { new Item { name = "a", weight = 1f }, new Item { name = "b", weight = 3f } };
        var rng = new SeededRandom(2026);
        int b = 0;
        for (int i = 0; i < 4000; i++)
            if (WeightedRandom.Pick(items, it => it.weight, rng).name == "b")
                b++;
        Assert.That(b / 4000f, Is.InRange(0.72f, 0.78f));
    }
}
