using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The scanner app spec §2.4-§2.5: the scan layers (the stub until the
/// document track's hidden and chip data land: an intact watermark, a chip
/// that stores what is printed) and the overlay compare (a line shimmers
/// only where the data really differs).
/// </summary>
public class ScanLayersTests
{
    private static DocumentField F(ClueCategory c, string value, string issuer = "") => new DocumentField { category = c, value = value, label = c.ToString(), issuer = issuer };

    private static readonly DocumentField[] Passport =
    {
        F(ClueCategory.Name, "Pell Quimby"), F(ClueCategory.CitizenId, "NHA-512"), F(ClueCategory.BirthDate, "4 Jul 2124"), F(ClueCategory.Seal, "Blue hexagon · VO", "visa")
    };

    [Test]
    public void Stub_AnIntactWatermark_AChipThatStoresThePrint_NeverADifference()
    {
        var stub = new StubScanLayers();
        IReadOnlyList<UvMark> uv = stub.Uv(0, Passport);
        Assert.AreEqual(1, uv.Count);
        Assert.AreEqual(UvMarkKind.Watermark, uv[0].Kind);
        Assert.IsFalse(uv[0].Fault);
        IReadOnlyList<DocumentField> chip = stub.Chip(0, Passport);
        Assert.AreEqual(Passport.Length, chip.Count, "a field per printed field, in the print's order");
        Assert.IsEmpty(ScanLayers.ChipDifferences(Passport, chip));
        Assert.AreNotSame(Passport[0], chip[0], "a copy: the chip never edits the print");
        Assert.IsNull(stub.Chip(1, new[] { F(ClueCategory.Expiry, "1 Jan 2151") }), "a paper without a Citizen ID has no chip");
    }

    [Test]
    public void ChipDifferences_OnlyValuesThatAreNotTheSameValue()
    {
        var chip = Passport.Select(f => F(f.category, f.value, f.issuer)).ToList();
        chip[2] = F(ClueCategory.BirthDate, "4 Jul 2120");
        chip[0] = F(ClueCategory.Name, " pell quimby ");
        chip[1] = F(ClueCategory.CitizenId, "");
        CollectionAssert.AreEqual(new[] { 2 }, ScanLayers.ChipDifferences(Passport, chip), "case and spaces never differ; a blank chip value states nothing");
        Assert.IsEmpty(ScanLayers.ChipDifferences(Passport, null));
    }

    [Test]
    public void Overlay_LinesUpSharedDetails_ShimmersOnlyRealDifferences()
    {
        var passport = new OverlaySource("Travel Passport", Passport.Select(f => new OverlayValue(f.category, f.label, f.value, f.issuer))
                                                                       .Append(new OverlayValue(ClueCategory.Photo, "Photo", "face:12")));
        var face = new OverlaySource("Traveller", new[] { new OverlayValue(ClueCategory.Photo, "Face", "face:12") });
        List<OverlayRow> rows = OverlayCompare.Rows(passport, face);
        Assert.AreEqual(1, rows.Count, "only the detail both state");
        Assert.IsFalse(rows[0].Differs, "their own photo: no shimmer");

        var stranger = new OverlaySource("Traveller", new[] { new OverlayValue(ClueCategory.Photo, "Face", "face:99") });
        Assert.IsTrue(OverlayCompare.Rows(passport, stranger).Single().Differs, "someone else's photo shimmers");

        var register = new OverlaySource("Seal Register", new[]
        {
            new OverlayValue(ClueCategory.Seal, "Permit Office", "Red circle · PO", "permit"), new OverlayValue(ClueCategory.Seal, "Visa Office", "Blue hexagon · VO", "visa")
        });
        OverlayRow seal = OverlayCompare.Rows(passport, register).Single();
        Assert.AreEqual("Blue hexagon · VO", seal.B, "a seal lines up with its own office's");
        Assert.IsFalse(seal.Differs);

        var forged = new OverlaySource("Seal Register", new[] { new OverlayValue(ClueCategory.Seal, "Visa Office", "Blue circle · VO", "visa") });
        Assert.IsTrue(OverlayCompare.Rows(passport, forged).Single().Differs);
        Assert.IsEmpty(OverlayCompare.Rows(passport, null));
    }
}
