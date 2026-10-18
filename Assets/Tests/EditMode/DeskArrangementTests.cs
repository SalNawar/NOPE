using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The desk's plan (the desk layout spec): bays on the desk and apart, and a
/// dragged decoration settling inside a decor zone, clear of the others, as
/// near as it can to where it was let go.
/// </summary>
public class DeskArrangementTests
{
    private static readonly DeskRect Zone = new DeskRect(0f, 0f, 0.6f, 0.4f);

    [Test]
    public void ADecoration_LetGoInsideAZone_StaysWhereItWasLetGo()
    {
        Assert.IsTrue(DeskArrangement.TrySettle(0.1f, 0.1f, 0.05f, 0.02f, new[] { Zone }, new DeskRect[0], 0.01f, out float x, out float y));
        Assert.AreEqual(0.05f, x, 1e-4f);
        Assert.AreEqual(0.02f, y, 1e-4f);
    }

    [Test]
    public void ADecoration_LetGoOutside_SettlesOnTheZonesEdge_WhollyInside()
    {
        Assert.IsTrue(DeskArrangement.TrySettle(0.1f, 0.1f, 2f, 0f, new[] { Zone }, new DeskRect[0], 0.01f, out float x, out float y));
        Assert.AreEqual(0.25f, x, 1e-4f, "its right edge on the zone's");
        Assert.IsTrue(DeskArrangement.Holds(Zone, new DeskRect(x, y, 0.1f, 0.1f)));
    }

    [Test]
    public void ADecoration_NeverLandsOnAnother()
    {
        var other = new DeskRect(0f, 0f, 0.1f, 0.1f);
        Assert.IsTrue(DeskArrangement.TrySettle(0.1f, 0.1f, 0f, 0f, new[] { Zone }, new[] { other }, 0.01f, out float x, out float y));
        Assert.IsFalse(DeskArrangement.Overlap(other, new DeskRect(x, y, 0.1f, 0.1f)));
        Assert.AreEqual(0.1f, System.Math.Max(System.Math.Abs(x), System.Math.Abs(y)), 0.011f, "right beside it");
    }

    [Test]
    public void ADecoration_TakesTheNearestZone()
    {
        var far = new DeskRect(5f, 0f, 0.6f, 0.4f);
        Assert.IsTrue(DeskArrangement.TrySettle(0.1f, 0.1f, 4.5f, 0f, new[] { Zone, far }, new DeskRect[0], 0.01f, out float x, out _));
        Assert.Greater(x, 4f);
    }

    [Test]
    public void NoRoom_IsNowhere()
    {
        Assert.IsFalse(DeskArrangement.TrySettle(1f, 1f, 0f, 0f, new[] { Zone }, new DeskRect[0], 0.01f, out _, out _), "too big for the zone");
        Assert.IsFalse(DeskArrangement.TrySettle(0.1f, 0.1f, 0f, 0f, new[] { Zone }, new[] { Zone }, 0.01f, out _, out _), "the zone is full");
    }

    [Test]
    public void Problems_NameBaysOffTheDesk_AndOverlappingPairs()
    {
        var desk = new DeskRect(0f, 0f, 2f, 1f);
        var bays = new List<KeyValuePair<string, DeskRect>>
        {
            new KeyValuePair<string, DeskRect>("scanner", new DeskRect(0.5f, 0f, 0.4f, 0.3f)),
            new KeyValuePair<string, DeskRect>("chip", new DeskRect(0.5f, 0.3f, 0.4f, 0.3f)),
        };
        CollectionAssert.IsEmpty(DeskArrangement.Problems(bays, desk), "touching edges is fine");
        bays.Add(new KeyValuePair<string, DeskRect>("rack", new DeskRect(0.6f, 0.1f, 0.2f, 0.2f)));
        bays.Add(new KeyValuePair<string, DeskRect>("shelf", new DeskRect(1.1f, 0f, 0.4f, 0.2f)));
        List<string> problems = DeskArrangement.Problems(bays, desk);
        Assert.AreEqual(3, problems.Count, string.Join("\n", problems));
        StringAssert.Contains("'shelf' runs off", string.Join("\n", problems));
    }

    [Test]
    public void Places_RoundTrip_InTheSavedText()
    {
        var places = new Dictionary<string, (float x, float y)> { ["calculator"] = (-2.5f, 1.02f), ["pen_pot"] = (1.5f, -0.25f) };
        string text = DeskArrangement.FormatPlaces(places);
        Assert.AreEqual("calculator:-2.5,1.02;pen_pot:1.5,-0.25", text);
        CollectionAssert.AreEquivalent(places, DeskArrangement.ParsePlaces(text));
    }

    [Test]
    public void ADamagedSave_LosesOnlyTheBrokenEntries()
    {
        Dictionary<string, (float x, float y)> places = DeskArrangement.ParsePlaces("calculator:1,2;broken;pen_pot:x,1;:3,4;stapler:0.5,NaN;binder:0.25,-1");
        CollectionAssert.AreEquivalent(new[] { "calculator", "binder" }, places.Keys);
        Assert.AreEqual((0.25f, -1f), places["binder"]);
        Assert.IsEmpty(DeskArrangement.ParsePlaces(null));
    }
}
