using System.Collections.Generic;
using NUnit.Framework;

public class RankedArtNamingTests
{
    /// <summary>Every id the live ContentLibrary_Main registers today.</summary>
    private static readonly string[] NationIds =
    {
        "greece", "germany", "japan", "egypt", "china",
        "latia", "aegyptus", "norvik", "albion", "solaris", "helios"
    };

    private static readonly string[] AttributeIds =
    {
        "democracy", "science", "art",
        "militarism", "philosophy", "industry", "mysticism"
    };

    // --- Hash stability: these values are the contract. If they change, every
    // --- generated PNG silently changes name/color, so they are pinned here.

    [TestCase("latia", 1731218780u)]
    [TestCase("industry", 312130495u)]
    [TestCase("", 2166136261u)]
    public void Hash_IsPinnedToKnownValues(string id, uint expected)
    {
        Assert.AreEqual(expected, RankedArtNaming.Hash(id));
    }

    [Test]
    public void Hash_IsCaseInsensitive()
    {
        Assert.AreEqual(RankedArtNaming.Hash("latia"), RankedArtNaming.Hash("LATIA"));
        Assert.AreEqual(RankedArtNaming.Hash("latia"), RankedArtNaming.Hash("Latia"));
    }

    [Test]
    public void Hash_NullIsStableAndDoesNotThrow()
    {
        Assert.AreEqual(2166136261u, RankedArtNaming.Hash(null));
    }

    // --- HSV bands ---

    [Test]
    public void HsvFor_StaysInsideMutedBands()
    {
        var ids = new List<string>(NationIds);
        ids.AddRange(AttributeIds);

        foreach (string id in ids)
        {
            RankedArtNaming.HsvFor(id, out float h, out float s, out float v);

            Assert.That(h, Is.InRange(0f, 1f), $"hue out of range for '{id}'");
            Assert.That(s, Is.InRange(0.35f, 0.65f), $"saturation left the muted band for '{id}'");
            Assert.That(v, Is.InRange(0.45f, 0.75f), $"value left the muted band for '{id}'");
        }
    }

    [Test]
    public void HsvFor_IsDeterministic()
    {
        RankedArtNaming.HsvFor("norvik", out float h1, out float s1, out float v1);
        RankedArtNaming.HsvFor("norvik", out float h2, out float s2, out float v2);

        Assert.AreEqual(h1, h2, 0.0001f);
        Assert.AreEqual(s1, s2, 0.0001f);
        Assert.AreEqual(v1, v2, 0.0001f);
    }

    [Test]
    public void HsvFor_DistinctIdsGetDistinctHues()
    {
        var seen = new Dictionary<string, float>();

        foreach (string id in NationIds)
        {
            RankedArtNaming.HsvFor(id, out float h, out _, out _);

            foreach (KeyValuePair<string, float> other in seen)
            {
                Assert.That(Mathf01Distance(h, other.Value), Is.GreaterThan(0.004f),
                    $"'{id}' and '{other.Key}' produce near-identical hues");
            }

            seen[id] = h;
        }
    }

    /// <summary>Circular distance on the 0..1 hue wheel.</summary>
    private static float Mathf01Distance(float a, float b)
    {
        float d = a > b ? a - b : b - a;
        return d > 0.5f ? 1f - d : d;
    }

    // --- File naming ---

    [Test]
    public void FileNameFor_UsesCategorySlot()
    {
        Assert.AreEqual("ranked_nation_latia", RankedArtNaming.FileNameFor(RankCategory.TopNation, "latia"));
        Assert.AreEqual("ranked_attr_industry", RankedArtNaming.FileNameFor(RankCategory.TopAttribute, "industry"));
    }

    [Test]
    public void FileNameFor_PairReplacesColonWithSafeSeparator()
    {
        string name = RankedArtNaming.FileNameFor(RankCategory.TopProfileAttribute, "latia_rome:militarism");

        Assert.AreEqual("ranked_pair_latia_rome__militarism", name);
        Assert.IsFalse(name.Contains(":"), "':' is illegal in Windows filenames");
    }

    [TestCase("a b", "a_b")]
    [TestCase("a/b", "a_b")]
    [TestCase("a\\b", "a_b")]
    [TestCase("a*b?", "a_b_")]
    [TestCase("MiXeD", "mixed")]
    public void Sanitize_ReplacesUnsafeCharacters(string input, string expected)
    {
        Assert.AreEqual(expected, RankedArtNaming.Sanitize(input));
    }

    [Test]
    public void Sanitize_EmptyOrNullFallsBack()
    {
        Assert.AreEqual("unknown", RankedArtNaming.Sanitize(null));
        Assert.AreEqual("unknown", RankedArtNaming.Sanitize(""));
    }

    [Test]
    public void FileNames_AreUniqueAcrossAllKnownIds()
    {
        var names = new HashSet<string>();

        foreach (string id in NationIds)
            Assert.IsTrue(names.Add(RankedArtNaming.FileNameFor(RankCategory.TopNation, id)), $"duplicate file name for nation '{id}'");

        foreach (string id in AttributeIds)
            Assert.IsTrue(names.Add(RankedArtNaming.FileNameFor(RankCategory.TopAttribute, id)), $"duplicate file name for attribute '{id}'");
    }
}
