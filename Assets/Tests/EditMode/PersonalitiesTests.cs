using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The cast (the personalities spec's PS1-PS2, §2): one weighted draw on the
/// personality stream for a generated traveller, one weight per personality
/// for every kind, none (and no draw) for an empty cast; the cast's content
/// rules, which Generate World and the validator share.
/// </summary>
public class PersonalitiesTests
{
    private static Personality P(string id, float weight = 1f, string name = null) =>
        new Personality { id = id, name = name ?? id, weight = weight, note = "a note" };

    private static List<Personality> Cast() => new List<Personality> { P("chatty"), P("curt"), P("anxious", 2f), P("glum", 0f) };

    [Test]
    public void Pick_IsOneDraw()
    {
        var rng = new ScriptedRandom(ScriptStep.Value(0.1f));
        Assert.IsNotNull(Personalities.Pick(Cast(), rng));
        Assert.AreEqual(1, rng.Draws);
    }

    [Test]
    public void Pick_FollowsTheWeights()
    {
        // Weights 1, 1, 2, 0 (total 4): [0, 0.25) chatty, [0.25, 0.5) curt, [0.5, 1) anxious.
        Assert.AreEqual("chatty", Personalities.Pick(Cast(), new ScriptedRandom(ScriptStep.Value(0.2f))).id);
        Assert.AreEqual("curt", Personalities.Pick(Cast(), new ScriptedRandom(ScriptStep.Value(0.3f))).id);
        Assert.AreEqual("anxious", Personalities.Pick(Cast(), new ScriptedRandom(ScriptStep.Value(0.51f))).id);
        Assert.AreEqual("anxious", Personalities.Pick(Cast(), new ScriptedRandom(ScriptStep.Value(0.99f))).id);
    }

    [Test]
    public void Pick_NeverAZeroWeight()
    {
        for (float roll = 0f; roll < 1f; roll += 0.01f)
            Assert.AreNotEqual("glum", Personalities.Pick(Cast(), new ScriptedRandom(ScriptStep.Value(roll))).id, $"roll {roll}");
    }

    [Test]
    public void Pick_OfAnEmptyCastIsNoneWithNoDraw()
    {
        var rng = new ScriptedRandom();
        Assert.IsNull(Personalities.Pick(new List<Personality>(), rng));
        Assert.IsNull(Personalities.Pick(null, rng));
        Assert.IsNull(Personalities.Pick(new List<Personality> { P("glum", 0f) }, rng), "no weight above 0: none");
        Assert.AreEqual(0, rng.Draws);
    }

    [Test]
    public void Problems_BlankOrDuplicateId()
    {
        List<string> problems = Personalities.Problems(new List<Personality> { P("chatty"), P(" "), P("chatty"), null });
        string all = string.Join("\n", problems);
        StringAssert.Contains("personalities: a personality has a blank id.", all);
        StringAssert.Contains("personalities: 'chatty' is listed twice.", all);
        StringAssert.Contains("personalities: an entry is empty.", all);
        CollectionAssert.IsEmpty(Personalities.Problems(Cast()));
        CollectionAssert.IsEmpty(Personalities.Problems(new List<Personality>()), "an empty cast: every traveller says the defaults");
    }

    [Test]
    public void Problems_NegativeWeight()
    {
        StringAssert.Contains("personalities: 'curt' has a negative weight (-1).", string.Join("\n", Personalities.Problems(new List<Personality> { P("chatty"), P("curt", -1f) })));
    }

    [Test]
    public void Problems_NoPositiveWeight()
    {
        List<string> problems = Personalities.Problems(new List<Personality> { P("chatty", 0f), P("curt", 0f) });
        CollectionAssert.AreEqual(new[] { "personalities: no personality has a weight above 0, so none is ever drawn." }, problems);
    }

    [Test]
    public void Problems_BlankName()
    {
        CollectionAssert.AreEqual(new[] { "personalities: 'curt' has a blank name." },
                                  Personalities.Problems(new List<Personality> { P("chatty"), P("curt", 1f, " ") }));
    }
    /// <summary>The endings and strandings spec §6.2, §7.3: a refusal chance from 0 to 1, a tilt that names a fate.</summary>
    [Test]
    public void Problems_WaiverRefusalAndStrandingTilt()
    {
        Personality grand = P("grand");
        grand.waiverRefusal = 0.5f;
        grand.strandingFate = "Carry";
        Assert.AreEqual(StrandingFate.Carry, grand.StrandingTilt);
        CollectionAssert.IsEmpty(Personalities.Problems(new List<Personality> { grand }));

        grand.waiverRefusal = 1.5f;
        grand.strandingFate = "Brunch";
        string all = string.Join("\n", Personalities.Problems(new List<Personality> { grand }));
        StringAssert.Contains("'grand' has a waiverRefusal of 1.5; it is a chance from 0 to 1.", all);
        StringAssert.Contains("'grand' names the stranding fate 'Brunch'", all);
        Assert.IsNull(grand.StrandingTilt);
        Assert.IsNull(P("sunny").StrandingTilt, "blank: no tilt");
    }
}
