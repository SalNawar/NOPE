using NUnit.Framework;

/// <summary>
/// The area the scanner hides papers in (the desk-first redesign, item 4): its
/// footprint and the shadow its body casts away from the camera. The push out
/// of it is DropResolver's now (DOC-HANDLE, BUG-6: one joint solve over every
/// keep-out; the old Clear, ClearProp and Eject tests went with them, see
/// DropResolverTests).
/// </summary>
public class ScannerClearanceTests
{
    private const float Eps = 1e-4f;

    [Test]
    public void Shadow_IsTheBodysHeightOverTheElevationsTangent_Capped()
    {
        Assert.AreEqual(0.1f, ScannerClearance.Shadow(0.1f, 45f, 1f), Eps);
        Assert.AreEqual(0f, ScannerClearance.Shadow(0.1f, 90f, 1f), Eps, "straight down hides nothing behind");
        Assert.AreEqual(0.5f, ScannerClearance.Shadow(0.1f, 0f, 0.5f), Eps, "near level: the cap");
        Assert.AreEqual(0f, ScannerClearance.Shadow(0f, 30f, 1f), Eps);
    }

    [Test]
    public void TheBlockedArea_IsTheFootprintAndTheShadowBehindIt()
    {
        DeskRect blocked = ScannerClearance.Blocked(new DeskRect(0.6f, 0f, 0.4f, 0.3f), 0.2f);
        Assert.AreEqual(0.6f, blocked.CentreX, Eps);
        Assert.AreEqual(0.4f, blocked.Width, Eps);
        Assert.AreEqual(-0.15f, blocked.CentreY - blocked.Height / 2f, Eps, "its front edge is the scanner's");
        Assert.AreEqual(0.35f, blocked.CentreY + blocked.Height / 2f, Eps, "it reaches the shadow's depth behind it");
        Assert.AreEqual(0.3f, ScannerClearance.Blocked(new DeskRect(0f, 0f, 0.4f, 0.3f), -1f).Height, Eps, "no shadow: the footprint");
        var paper = new DeskRect(0.6f, 0.3f, 0.26f, 0.2f); // behind the scanner, in its shadow
        (float x, float y) = DropResolver.Rest(paper, new DeskRect(0f, 0f, 2f, 1f), new System.Collections.Generic.List<DeskRect> { blocked });
        Assert.AreEqual(0f, DropResolver.Overlap(new DeskRect(x, y, 0.26f, 0.2f), new System.Collections.Generic.List<DeskRect> { blocked }), "a paper dropped there rests clear of it");
    }
}
