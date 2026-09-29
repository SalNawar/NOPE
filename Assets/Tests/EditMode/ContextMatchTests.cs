using NUnit.Framework;

/// <summary>
/// The one matcher of a content row's context against a traveller (the
/// personalities spec's V3): a blank list of kinds or a blank era matches
/// anything; named kinds score 2 and a named era 1, summed; a row naming
/// another kind or era never matches. The questions' answer overrides use it
/// (phase V1), the voice rows reuse it (phase V2).
/// </summary>
public class ContextMatchTests
{
    private static readonly TravellerKind[] AnyKind = new TravellerKind[0];

    [Test]
    public void Score_BlankMatchesAnything()
    {
        Assert.AreEqual(0, ContextMatch.Score(AnyKind, null, TravellerKind.RichTourist, "ancient"));
        Assert.AreEqual(0, ContextMatch.Score(null, string.Empty, TravellerKind.Displaced, null), "no kinds and no era: any traveller, even one with no claimed era");
        Assert.AreEqual(0, ContextMatch.Score(AnyKind, " ", TravellerKind.Labourer, "modern"), "a blank era reads as none");
    }

    [Test]
    public void Score_NamedKindsTwoNamedEraOne()
    {
        Assert.AreEqual(2, ContextMatch.Score(new[] { TravellerKind.Displaced }, null, TravellerKind.Displaced, "ancient"));
        Assert.AreEqual(2, ContextMatch.Score(new[] { TravellerKind.RichTourist, TravellerKind.PoorTourist }, null, TravellerKind.PoorTourist, "ancient"), "named kinds score 2 however many");
        Assert.AreEqual(1, ContextMatch.Score(AnyKind, "ancient", TravellerKind.RichTourist, "ancient"));
        Assert.AreEqual(3, ContextMatch.Score(new[] { TravellerKind.Displaced }, "ancient", TravellerKind.Displaced, "ancient"), "summed: kinds outrank an era");
    }

    [Test]
    public void Score_AnotherKindOrEraNeverMatches()
    {
        Assert.AreEqual(ContextMatch.NoMatch, ContextMatch.Score(new[] { TravellerKind.Displaced }, null, TravellerKind.RichTourist, "ancient"));
        Assert.AreEqual(ContextMatch.NoMatch, ContextMatch.Score(AnyKind, "medieval", TravellerKind.Displaced, "ancient"));
        Assert.AreEqual(ContextMatch.NoMatch, ContextMatch.Score(AnyKind, "ancient", TravellerKind.Displaced, null), "a named era never matches a traveller with none");
        Assert.AreEqual(ContextMatch.NoMatch, ContextMatch.Score(new[] { TravellerKind.Displaced }, "ancient", TravellerKind.Displaced, "medieval"), "both must hold");
        Assert.Less(ContextMatch.NoMatch, 0, "below every match's score");
    }
}
