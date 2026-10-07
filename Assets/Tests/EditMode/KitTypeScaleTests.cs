using NUnit.Framework;

/// <summary>The UI kit's type scale (KitTypeScale): sizes from the component's height, held between floor and ceiling; labels that do not fit grow their plate rather than shrinking under the floor.</summary>
public class KitTypeScaleTests
{
    [Test]
    public void Size_IsTheShareOfTheHeight()
    {
        Assert.AreEqual(25f, KitTypeScale.Size(0.42f, 16f, 34f, 60f));
        Assert.AreEqual(20f, KitTypeScale.Size(0.5f, 12f, 24f, 40f));
    }

    [Test]
    public void Size_IsHeldBetweenFloorAndCeiling()
    {
        Assert.AreEqual(16f, KitTypeScale.Size(0.42f, 16f, 34f, 20f));
        Assert.AreEqual(34f, KitTypeScale.Size(0.42f, 16f, 34f, 200f));
        Assert.AreEqual(34f, KitTypeScale.Size(0.42f, 34f, 16f, 200f), "a swapped range is read the right way round");
    }

    [Test]
    public void Size_WithoutAHeightIsTheCeiling()
    {
        Assert.AreEqual(22f, KitTypeScale.Size(0.42f, 14f, 22f, 0f));
        Assert.AreEqual(22f, KitTypeScale.Size(0f, 14f, 22f, 50f));
    }

    [Test]
    public void Fit_ALabelThatFitsKeepsItsSize()
    {
        Assert.AreEqual((24f, 0f), KitTypeScale.Fit(24f, 80f, 100f, 16f));
    }

    [Test]
    public void Fit_ALabelTooWideShrinksToFit()
    {
        (float size, float grow) = KitTypeScale.Fit(24f, 120f, 100f, 16f);
        Assert.AreEqual(20f, size);
        Assert.AreEqual(0f, grow);
    }

    [Test]
    public void Fit_NeverUnderTheFloor_TheComponentGrowsInstead()
    {
        (float size, float grow) = KitTypeScale.Fit(24f, 300f, 100f, 16f);
        Assert.AreEqual(16f, size);
        Assert.AreEqual(100f, grow, 0.01f);
    }

    [Test]
    public void Defaults_CoverEveryRoleOnce_AndTheBodyLinesHaveNoShare()
    {
        var seen = new System.Collections.Generic.HashSet<KitText>();
        foreach ((KitText kind, float share, float min, float max) in KitTypeScale.Defaults)
        {
            Assert.IsTrue(seen.Add(kind), $"{kind} twice");
            Assert.LessOrEqual(min, max, kind.ToString());
            bool body = kind == KitText.Body || kind == KitText.BodySmall || kind == KitText.BodyLarge;
            Assert.AreEqual(body, share == 0f, kind.ToString());
        }
        Assert.AreEqual(System.Enum.GetValues(typeof(KitText)).Length, seen.Count);
    }

    [Test]
    public void IsLabel_OnlyTheReadingRolesWrap()
    {
        Assert.IsTrue(KitTypeScale.IsLabel(KitText.PlateLabel));
        Assert.IsTrue(KitTypeScale.IsLabel(KitText.Keycap));
        Assert.IsFalse(KitTypeScale.IsLabel(KitText.Body));
        Assert.IsFalse(KitTypeScale.IsLabel(KitText.BodyLarge));
        Assert.IsFalse(KitTypeScale.IsLabel(KitText.ListTitle));
        Assert.IsFalse(KitTypeScale.IsLabel(KitText.Tooltip));
    }

    [Test]
    public void IsTracked_LabelsButTheMastheadAndTheReadouts()
    {
        Assert.IsTrue(KitTypeScale.IsTracked(KitText.PlateLabel));
        Assert.IsTrue(KitTypeScale.IsTracked(KitText.PullTabLabel));
        Assert.IsFalse(KitTypeScale.IsTracked(KitText.Masthead));
        Assert.IsFalse(KitTypeScale.IsTracked(KitText.Readout));
        Assert.IsFalse(KitTypeScale.IsTracked(KitText.Body));
    }

    [Test]
    public void Wraps_ReadingRolesTheHeadlineAndTheCards()
    {
        Assert.IsTrue(KitTypeScale.Wraps(KitText.Body));
        Assert.IsTrue(KitTypeScale.Wraps(KitText.Headline));
        Assert.IsTrue(KitTypeScale.Wraps(KitText.CardTitle));
        Assert.IsTrue(KitTypeScale.Wraps(KitText.CardSub));
        Assert.IsFalse(KitTypeScale.Wraps(KitText.PullTabLabel));
        Assert.IsFalse(KitTypeScale.Wraps(KitText.PlateLabel));
        Assert.IsFalse(KitTypeScale.Wraps(KitText.Keycap));
    }
}
