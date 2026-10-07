using System.Linq;
using NUnit.Framework;

/// <summary>The slot panel's three reels (the UI kit, sheet 04): a win lands one symbol on every reel, anything else three different ones, the same for the same outcome every time.</summary>
public class SlotReelsTests
{
    [Test]
    public void AWin_LandsOneSymbolOnEveryReel()
    {
        int[] faces = SlotReels.Faces(2, true, 5);
        Assert.AreEqual(SlotReels.Count, faces.Length);
        Assert.That(faces, Is.All.EqualTo(faces[0]));
    }

    [Test]
    public void ALossOrANothing_LandsThreeDifferentSymbols()
    {
        for (int outcome = 0; outcome < 7; outcome++)
        {
            int[] faces = SlotReels.Faces(outcome, false, 5);
            Assert.AreEqual(SlotReels.Count, faces.Distinct().Count(), $"outcome {outcome}");
        }
    }

    [Test]
    public void Faces_AreSymbolsOfTheSet_AndStableForAnOutcome()
    {
        for (int outcome = -3; outcome < 12; outcome++)
            foreach (bool win in new[] { true, false })
            {
                int[] faces = SlotReels.Faces(outcome, win, 4);
                Assert.That(faces, Is.All.InRange(0, 3));
                CollectionAssert.AreEqual(faces, SlotReels.Faces(outcome, win, 4));
            }
    }

    [Test]
    public void DifferentWins_ShowDifferentSymbols()
    {
        Assert.AreNotEqual(SlotReels.Faces(0, true, 5)[0], SlotReels.Faces(1, true, 5)[0]);
    }

    [Test]
    public void TooFewSymbols_StillGivesThreeFaces()
    {
        Assert.AreEqual(SlotReels.Count, SlotReels.Faces(1, false, 1).Length);
        Assert.That(SlotReels.Faces(1, false, 0), Is.All.EqualTo(0));
    }
}
