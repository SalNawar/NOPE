using System.Collections.Generic;
using NUnit.Framework;

/// <summary>A cue-reactive prop's pick (TimelineReactiveSprite, audit R3-004): the first mapping whose cue is active wins.</summary>
public class CueMatchTests
{
    private static int First(string[] mapped, params string[] active) => CueMatch.First(mapped.Length, i => mapped[i], active);

    [Test]
    public void TheFirstMappingWhoseCueIsActive_Wins_InMappingOrder()
    {
        var mapped = new[] { "roman", "steam", "robot" };
        Assert.AreEqual(1, First(mapped, "robot", "steam"), "mapping order, not the cues' order");
        Assert.AreEqual(2, First(mapped, "robot"));
        Assert.AreEqual(0, First(mapped, "steam", "roman"));
    }

    [Test]
    public void NoActiveCue_NoMapping_OrABlankId_MatchesNothing()
    {
        Assert.AreEqual(-1, First(new[] { "roman" }, "steam"));
        Assert.AreEqual(-1, First(new[] { "roman" }));
        Assert.AreEqual(-1, First(new string[0], "roman"));
        Assert.AreEqual(-1, First(new[] { "", null }, "", null), "a blank cue id never matches");
        Assert.AreEqual(-1, CueMatch.First(1, i => "roman", null), "no cue list is no active cue");
    }

    [Test]
    public void ABlankMapping_IsSkipped_NotAStop()
    {
        Assert.AreEqual(1, First(new[] { "", "roman", "steam" }, "steam", "roman"));
    }
}
