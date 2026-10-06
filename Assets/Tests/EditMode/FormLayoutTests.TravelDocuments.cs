using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The travel documents (the travel documents spec, 2026-10-05, TD1-TD4): a
/// wide page prints at the size of every other paper (its print unit is its
/// width over the style's aspect), a booklet has its holder's cover round its
/// pages, its emblem with its nation's code at the header's left, its spine
/// at its fold and its visa page (no machine-readable zone: Saleh 2026-10-06); a card its chip; a folded card its
/// crease; a letterhead its seal's watermark; and the stamps' places.
/// </summary>
public partial class FormLayoutTests
{
    /// <summary>The passport as TC-101 prints it: the data page (the name beside the photo, the Citizen ID, the birth date and the expiry), the fold at 0.62, the visa page (destination and class, the visa stamp area), the emblem's watermark.</summary>
    private static FormSpec Booklet() => new FormSpec
    {
        look = new FormLook { frame = FormFrame.Booklet, accent = "#23305E", paper = "#F4EFE2", aspect = 0.7f, scale = 0.62f },
        blocks = new[]
        {
            new FormBlock { kind = FormBlockKind.Header, field = 6 },
            Row(Field(0, 8), new FormCell { slot = FormSlots.Photo, field = 7, span = 4, rows = 2 }),
            Row(Field(1, 8)),
            Row(Field(2, 7), Field(5, 5)),
            new FormBlock { kind = FormBlockKind.Fold, shares = new[] { 0.62f } },
            Block(FormBlockKind.Watermark),
            Row(Field(3, 8), Field(4, 4)),
            Block(FormBlockKind.Visa, "VISAS")
        }
    };

    /// <summary>The booklet's page height laid out a style page wide (print unit 1).</summary>
    private const float BookletHeight = 0.765f / 0.7f;

    private static FormData BookletData(string cover = "#1F4A2E", string emblem = "WingedSun") => new FormData
    {
        Agency = "TEMPORAL CUSTOMS",
        Programme = "Visa Office",
        FormNumber = "TC-101",
        Title = "Passport",
        Serial = "TC-101/000417",
        HasPhoto = true,
        Cover = cover,
        Emblem = emblem,
        NationCode = "EGY",
        FieldLabels = new[] { "Full Name", "Citizen ID", "Date of Birth", "Destination", "Visa Class", "Valid Until", "Issuing Seal", "Photo" },
        FieldValues = new[] { "Omar", "KTR-418", "3 May 2101", "Periclean Athens (Ancient)", "Standard", "9 Jun 2150", "Blue hexagon · VO", "m/skin3/face-a/black" }
    };

    /// <summary>A card 1.586 wide: the chip beside the Citizen ID, the class and the unit under them.</summary>
    private static FormSpec Card() => new FormSpec
    {
        look = new FormLook { frame = FormFrame.Card, accent = "#0F6E7A", paper = "#E6EEF2", aspect = 1.586f, scale = 0.36f },
        blocks = new[]
        {
            new FormBlock { kind = FormBlockKind.Header, field = 3 },
            Row(new FormCell { slot = FormSlots.Chip, span = 3 }, Field(0, 9)),
            Row(Field(1, 6), Field(2, 6))
        }
    };

    private static FormData CardData() => new FormData
    {
        Agency = "TEMPORAL CUSTOMS",
        Programme = "Portal Hall Dispatch",
        FormNumber = "TC-240",
        Title = "Transponder Card",
        FieldLabels = new[] { "Citizen ID", "Transponder", "Transponder Class", "Issuing Seal" },
        FieldValues = new[] { "C-4471-0213", "TX-77-0412", "Economy", "Green circle · PH" }
    };

    // ---------------- TD2: a wide page prints at one size ----------------

    [Test]
    public void EveryPaper_PrintsInItsWidthOverTheStylesAspect_ALongOneTallerAWideOneShorter()
    {
        Assert.AreEqual(520f / M.aspect, FormLayout.PrintUnit(Card(), 520f, M), 1e-3f, "a card drawn 520 wide prints as a style page 520 wide");
        Assert.AreEqual(520f / M.aspect, FormLayout.PrintUnit(Booklet(), 520f, M), 1e-3f, "so does a narrower page");
        var landscape = new FormSpec { fixedPage = false, landscape = true, look = new FormLook { aspect = 1.5f } };
        Assert.AreEqual(600f * 1.5f, FormLayout.PrintUnit(landscape, 600f, M), 1e-3f, "a landscape page kind keeps its own unit");

        PlacedForm card = FormLayout.Layout(Card(), CardData(), 520f, M, new FakeMeasure());
        Assert.AreEqual(520f / 1.586f, card.PageHeight, 1e-2f, "the card is as wide as a style page and less tall");
        Assert.AreEqual(520f / M.aspect, card.Unit, 1e-3f);
        Assert.AreEqual(M.valueSize * card.Unit, TextOf(card, FormTextRole.Value).Size, 1e-3f, "its values print at every paper's size");
        foreach (FormItem item in card.Items)
            Assert.IsTrue(Inside(item.Rect, new FaceRect(0f, 0f, card.Width, card.PageHeight)), $"{item.Kind} '{item.Text}' on the card");
        List<string> problems = FormLayout.Check(Card(), CardData(), M, new FakeMeasure());
        CollectionAssert.IsEmpty(problems, "the card's fields fit its page: " + string.Join("; ", problems));

        PlacedForm booklet = FormLayout.Layout(Booklet(), BookletData(), 520f, M, new FakeMeasure());
        Assert.AreEqual(520f / 0.7f, booklet.PageHeight, 1e-2f, "a narrower look is a longer page");
        Assert.AreEqual(M.valueSize * 520f / M.aspect, TextOf(booklet, FormTextRole.Value).Size, 1e-3f, "at the same print");
    }

    // ---------------- TD1, TD3: the booklet ----------------

    [Test]
    public void ABooklet_HasItsCoverRoundItsPages_InTheHoldersColour_OutsideEveryBox()
    {
        PlacedForm f = FormLayout.Layout(Booklet(), BookletData(), M.aspect, M, new FakeMeasure());
        List<FormItem> cover = Of(f, FormItemKind.Cover).ToList();
        Assert.AreEqual(4, cover.Count, "top, bottom and both sides");
        Assert.IsTrue(cover.All(c => c.Text == "#1F4A2E"), "the holder's nation's cover");
        foreach (FormItem band in cover)
            foreach (FormSlot s in f.Slots)
                Assert.IsFalse(Overlaps(band.Rect, s.Hit), $"the cover over slot {s.Index}");
        Assert.IsTrue(Of(FormLayout.Layout(Booklet(), BookletData(cover: ""), M.aspect, M, new FakeMeasure()), FormItemKind.Cover).All(c => c.Text == string.Empty),
                      "no nation: blank, the look's accent paints it");
        CollectionAssert.IsEmpty(Of(f, FormItemKind.Seal).Where(s => s.Text == string.Empty), "no faint agency seal on a booklet");
    }

    [Test]
    public void ABooklet_PrintsItsEmblemAtTheHeadersLeft_AndKeepsItsRoomWithoutOne()
    {
        PlacedForm f = FormLayout.Layout(Booklet(), BookletData(), M.aspect, M, new FakeMeasure());
        FormItem emblem = Of(f, FormItemKind.Emblem).Single();
        Assert.AreEqual("WingedSun", emblem.Text);
        Assert.AreEqual(M.marginX + M.sealSize / 2f, (emblem.Rect.XMin + emblem.Rect.XMax) / 2f, Eps, "centred in the seal's column at the content's left");
        Assert.LessOrEqual(emblem.Rect.Width, M.sealSize + Eps, "within the seal's side (its nation's code under it)");
        foreach (FormTextRole role in new[] { FormTextRole.Agency, FormTextRole.Title })
            Assert.IsFalse(Overlaps(TextOf(f, role).Rect, emblem.Rect), $"the {role} stays clear of the emblem");

        PlacedForm none = FormLayout.Layout(Booklet(), BookletData(emblem: ""), M.aspect, M, new FakeMeasure());
        CollectionAssert.IsEmpty(Of(none, FormItemKind.Emblem));
        for (int i = 0; i < f.Slots.Count; i++)
            Assert.AreEqual(f.Slots[i].Hit.YMin, none.Slots[i].Hit.YMin, Eps, $"slot {i} keeps its place without an emblem");
        Assert.AreEqual(TextOf(f, FormTextRole.Title).Rect.XMin, TextOf(none, FormTextRole.Title).Rect.XMin, Eps, "the words keep their place");
    }

    [Test]
    public void ABookletsFold_LaysItsSpineAcrossThePage_AndTheVisaPageBelowIt()
    {
        PlacedForm f = FormLayout.Layout(Booklet(), BookletData(), M.aspect, M, new FakeMeasure());
        FormItem spine = Of(f, FormItemKind.Spine).Single();
        Assert.AreEqual(0.62f * BookletHeight, spine.Rect.CentreY, Eps, "at the fold's share of the page");
        Assert.AreEqual(0f, spine.Rect.XMin, Eps);
        Assert.AreEqual(M.aspect, spine.Rect.XMax, Eps, "across the whole page");
        Assert.IsTrue(f.Slots.Where(s => s.Field == 3 || s.Field == 4).All(s => s.Hit.YMin > spine.Rect.YMax), "the visa's fields on the facing page");
        Assert.IsTrue(f.Slots.Where(s => s.Field == 0 || s.Field == 5).All(s => s.Hit.YMax < spine.Rect.YMin), "the data page's above it");
        List<string> problems = FormLayout.Check(Booklet(), BookletData(), M, new FakeMeasure());
        CollectionAssert.IsEmpty(problems, "the booklet fits its page: " + string.Join("; ", problems));

        FormSpec tight = Booklet();
        tight.blocks.First(b => b.kind == FormBlockKind.Fold).shares = new[] { 0.2f };
        Assert.IsTrue(FormLayout.Check(tight, BookletData(), M, new FakeMeasure()).Any(p => p.Contains("into the spine")), "a data page past its fold is reported");
    }

    [Test]
    public void TheNationsCode_PrintsUnderTheEmblem_InTheEmblemsRoom_AndMovesNothing()
    {
        PlacedForm f = FormLayout.Layout(Booklet(), BookletData(), M.aspect, M, new FakeMeasure());
        FormItem emblem = Of(f, FormItemKind.Emblem).Single();
        FormItem code = f.Items.Single(i => i.Kind == FormItemKind.Text && i.Text == "EGY");
        Assert.AreEqual(FormTextAlign.Centre, code.Align);
        Assert.GreaterOrEqual(code.Rect.YMin, emblem.Rect.YMax - Eps, "under the emblem");
        Assert.LessOrEqual(code.Rect.YMax, f.Slots.Where(s => s.Field == 0).Min(s => s.Hit.YMin) + Eps, "in the header, above the data page");
        Assert.Less(code.Slot, 0, "the code is never picked");

        FormData blank = BookletData();
        blank.NationCode = string.Empty;
        PlacedForm none = FormLayout.Layout(Booklet(), blank, M.aspect, M, new FakeMeasure());
        Assert.IsFalse(none.Items.Any(i => i.Kind == FormItemKind.Text && i.Text == "EGY"));
        Assert.Greater(Of(none, FormItemKind.Emblem).Single().Rect.Width, emblem.Rect.Width, "without a code the emblem takes the whole room");
        for (int i = 0; i < f.Slots.Count; i++)
            Assert.AreEqual(f.Slots[i].Hit.YMin, none.Slots[i].Hit.YMin, Eps, $"slot {i}");
    }

    [Test]
    public void AVisaPage_IsAStampAreaDownToTheBottomMargin()
    {
        PlacedForm f = FormLayout.Layout(Booklet(), BookletData(), M.aspect, M, new FakeMeasure());
        FormItem area = Of(f, FormItemKind.StampArea).Single();
        Assert.AreEqual(BookletHeight - M.marginBottom, area.Rect.YMax, Eps, "to the page's bottom margin");
        Assert.AreEqual(M.marginX, area.Rect.XMin, Eps);
        Assert.AreEqual(M.aspect - M.marginX, area.Rect.XMax, Eps, "across the content");
        Assert.GreaterOrEqual(area.Rect.Height, 1.25f * M.stampHeight - Eps);
        Assert.IsTrue(f.Items.Any(i => i.Kind == FormItemKind.Text && i.Text == "VISAS" && Inside(i.Rect, area.Rect)), "its caption inside its top");
    }

    [Test]
    public void AWatermark_IsCentredUnderEverything_TheFieldsSealElseTheEmblem()
    {
        PlacedForm f = FormLayout.Layout(Booklet(), BookletData(), M.aspect, M, new FakeMeasure());
        Assert.AreEqual(FormItemKind.Watermark, f.Items[0].Kind, "first in drawing order: under every box");
        Assert.AreEqual("WingedSun", f.Items[0].Text, "no field: the holder's emblem");
        FormItem spine = Of(f, FormItemKind.Spine).Single();
        Assert.AreEqual(M.aspect / 2f, f.Items[0].Rect.CentreX, Eps, "across the page's middle");
        Assert.Greater(f.Items[0].Rect.YMin, spine.Rect.YMax, "on the visa page, below the fold");
        Assert.Less(f.Items[0].Rect.YMax, BookletHeight - M.marginBottom + Eps);

        FormSpec letterhead = Tc610Sealed(new FormLook { frame = FormFrame.BottomBand, accent = "#1F4F5A" });
        letterhead.blocks = letterhead.blocks.Take(1).Concat(new[] { new FormBlock { kind = FormBlockKind.Watermark, field = 6 } }).Concat(letterhead.blocks.Skip(1)).ToArray();
        PlacedForm sealed_ = Desk(letterhead, Tc610SealedData());
        Assert.AreEqual("Violet circle · DP", Of(sealed_, FormItemKind.Watermark).Single().Text, "the issuing office's seal");
        CollectionAssert.IsEmpty(FormLayout.Check(letterhead, Tc610SealedData(), M, new FakeMeasure()).Where(p => p.Contains("placed")), "a watermark places no field");

        PlacedForm none = FormLayout.Layout(Booklet(), BookletData(emblem: ""), M.aspect, M, new FakeMeasure());
        CollectionAssert.IsEmpty(Of(none, FormItemKind.Watermark), "nothing to draw");
    }

    // ---------------- TD1: the card and the folded card ----------------

    [Test]
    public void ACardsChip_SitsInItsCell_AndIsNeverPicked()
    {
        PlacedForm f = FormLayout.Layout(Card(), CardData(), 520f, M, new FakeMeasure());
        FormItem chip = Of(f, FormItemKind.Chip).Single();
        FormSlot id = f.Slots.First(s => s.Field == 0);
        Assert.Less(chip.Rect.XMax, id.Hit.XMin, "left of the Citizen ID");
        Assert.AreEqual(1.3f, chip.Rect.Width / chip.Rect.Height, 1e-3f, "a chip's shape");
        Assert.IsTrue(chip.Rect.YMin >= id.Hit.YMin - Eps && chip.Rect.YMax <= id.Hit.YMax + Eps, "inside its row");
        Assert.AreEqual(-1, FormLayout.SlotAt(f, chip.Rect.CentreX, chip.Rect.CentreY), "nothing to pick");
        Assert.IsNotEmpty(Of(f, FormItemKind.Stripe), "the card's sheen band");
    }

    [Test]
    public void AFoldedCardsCrease_RunsDownTheWholeMiddle_AFaintShadeOverTheBoxes()
    {
        PlacedForm f = Desk(Tc610Sealed(new FormLook { frame = FormFrame.Folded, accent = "#3E5C76" }), Tc610SealedData());
        FormItem crease = Of(f, FormItemKind.Crease).Single();
        Assert.AreEqual(M.aspect / 2f, crease.Rect.CentreX, Eps);
        Assert.AreEqual(1f, crease.Rect.Height, Eps, "down the whole page");
        var palette = new FormPalette { Rule = new Rgba(0.3f, 0.3f, 0.3f), BoxFill = new Rgba(1f, 1f, 1f), Band = new Rgba(0.9f, 0.9f, 0.9f), Accent = new Rgba(0.2f, 0.3f, 0.4f) };
        List<FormQuad> shade = FormPaint.Quads(f, palette, M).Where(q => Inside(q.Rect, crease.Rect)).ToList();
        Assert.AreEqual(2, shade.Count, "a shade and a highlight");
        Assert.IsTrue(shade.All(q => q.Layer == FormPaintLayer.Line && q.Colour.A < 1f), "faint, over the boxes' fills");
    }

    // ---------------- FormPaint: the new strokes ----------------

    [Test]
    public void TheNewStrokes_PaintTheCoverInItsColour_TheSpineTheCreaseAndTheChip()
    {
        var palette = new FormPalette { Ink = new Rgba(0.1f, 0.1f, 0.1f), Rule = new Rgba(0.3f, 0.3f, 0.3f), BoxFill = new Rgba(1f, 1f, 1f), Band = new Rgba(0.9f, 0.9f, 0.9f),
                                        StampDash = new Rgba(0.4f, 0.4f, 0.4f), Accent = new Rgba(0.2f, 0.2f, 0.5f) };
        PlacedForm booklet = FormLayout.Layout(Booklet(), BookletData(), M.aspect, M, new FakeMeasure());
        List<FormQuad> quads = FormPaint.Quads(booklet, palette, M);
        Assert.IsTrue(quads.Any(q => q.Layer == FormPaintLayer.Fill && System.Math.Abs(q.Colour.R - 0x1F / 255f) < 1e-3f && System.Math.Abs(q.Colour.G - 0x4A / 255f) < 1e-3f), "the cover in the holder's colour");
        FormItem spine = Of(booklet, FormItemKind.Spine).Single();
        Assert.IsTrue(quads.Any(q => q.Layer == FormPaintLayer.Fill && q.Rect.YMin == spine.Rect.YMin && q.Colour.A < 1f), "the spine's shade");
        Assert.Greater(quads.Count(q => q.Layer == FormPaintLayer.Line && q.Rect.YMin > spine.Rect.YMin && q.Rect.YMax < spine.Rect.YMax), 3, "the stitches along it");

        PlacedForm noCover = FormLayout.Layout(Booklet(), BookletData(cover: ""), M.aspect, M, new FakeMeasure());
        Assert.IsTrue(FormPaint.Quads(noCover, palette, M).Any(q => q.Colour.B == palette.Accent.B && q.Colour.R == palette.Accent.R), "a blank cover is the accent");

        PlacedForm card = FormLayout.Layout(Card(), CardData(), 520f, M, new FakeMeasure());
        FormItem chip = Of(card, FormItemKind.Chip).Single();
        List<FormQuad> chipQuads = FormPaint.Quads(card, palette, M).Where(q => Inside(q.Rect, chip.Rect)).ToList();
        Assert.IsTrue(chipQuads.Any(q => q.Colour.R == FormPaint.ChipGold.R), "the chip's gold");
        Assert.IsTrue(chipQuads.All(q => q.Layer == FormPaintLayer.Line), "over the card's fills");
        Assert.AreEqual(M.ruleWidth * card.Unit, FormPaint.Quads(card, palette, M).First(q => q.Colour.R == palette.Rule.R && q.Layer == FormPaintLayer.Line).Rect.Height, 1e-3f,
                        "a wide page's rule is in its print unit");
    }

    // ---------------- TD4: where the stamps land ----------------

    [Test]
    public void AStamp_LandsInTheLargestStampArea_EachNextOneBesideTheLast()
    {
        PlacedForm f = FormLayout.Layout(Booklet(), BookletData(), M.aspect, M, new FakeMeasure());
        FaceRect visa = Of(f, FormItemKind.StampArea).Single().Rect;
        Assert.AreEqual(visa.XMin, StampSpots.Area(f).XMin, Eps);
        FaceRect first = StampSpots.Next(f, 0, 2.8f);
        Assert.AreEqual(StampSpots.MarkHeight * f.Unit, first.Height, Eps);
        Assert.AreEqual(2.8f, first.Width / first.Height, 1e-3f, "at the mark's aspect");
        Assert.Less(first.XMin - visa.XMin, first.Width, "the first at the area's left");
        Assert.Less(first.YMin - visa.YMin, first.Height, "and its top");
        Assert.Greater(StampSpots.Next(f, 1, 2.8f).XMin, first.XMax, "the second beside the first, clear of it");
        int places = 0;
        while (places < 50 && !(places > 0 && StampSpots.Next(f, places, 2.8f).XMin == first.XMin && StampSpots.Next(f, places, 2.8f).YMin == first.YMin))
            places++;
        for (int i = 0; i < places; i++)
        {
            FaceRect mark = StampSpots.Next(f, i, 2.8f);
            Assert.IsTrue(Inside(mark, visa), $"mark {i} inside the visa page");
            for (int j = 0; j < i; j++)
                Assert.IsFalse(Overlaps(mark, StampSpots.Next(f, j, 2.8f)), $"mark {i} hides mark {j}");
        }
        Assert.Greater(places, 1, "the visa page holds more than one mark before the rows start over");
    }

    /// <summary>Papers, Please's stamps (Saleh 2026-10-06, "the stamp mark must land exactly where the stamp is pressed"): a mark pressed anywhere on the passport is centred on the pressed point, over the boxes too, outside the ENTRY VISA box too, and only moved to stay whole on the page.</summary>
    [Test]
    public void AStampPressedAnywhereOnThePassport_IsCentredWherePressed_AndKeptOnThePage()
    {
        PlacedForm f = FormLayout.Layout(Booklet(), BookletData(), M.aspect, M, new FakeMeasure());
        FormItem visaItem = Of(f, FormItemKind.StampArea).Single();
        Assert.AreEqual(FormLayout.VisaBox, visaItem.Text, "the passport's visa box is marked as such (the guide)");
        FaceRect visa = visaItem.Rect;
        (float w, float h) = StampSpots.MarkSize(f, visa, 2f);
        var page = new FaceRect(0f, 0f, f.Width, f.PageHeight);
        foreach ((float x, float y, string where) in new[]
                 {
                     ((visa.XMin + visa.XMax) / 2f, (visa.YMin + visa.YMax) / 2f, "in the visa box"),
                     (f.Width * 0.3f, f.PageHeight * 0.25f, "over the boxes above the visa box"),
                     (f.Width * 0.7f, f.PageHeight * 0.5f, "in the page's middle"),
                 })
        {
            FaceRect at = StampSpots.AtPoint(f, x, y, 2f);
            Assert.AreEqual(x, at.CentreX, Eps, where);
            Assert.AreEqual(y, at.CentreY, Eps, where);
            Assert.AreEqual(w, at.Width, Eps, $"{where}: the form's mark size");
            Assert.AreEqual(h, at.Height, Eps, $"{where}: the form's mark size");
        }
        FaceRect corner = StampSpots.AtPoint(f, 0.001f, f.PageHeight - 0.001f, 2f);
        Assert.IsTrue(Inside(corner, page), "a press by the page's corner is kept whole on the page");
        Assert.AreEqual(w / 2f, corner.CentreX, Eps, "moved just enough");
        Assert.AreEqual(0f, StampSpots.AtPoint(null, 0.5f, 0.5f, 2f).Width, Eps, "no form: no mark");
        var plain = new PlacedForm(1f, 1f, 1f, new FormItem[0], new FormSlot[0], new[] { 0f });
        Assert.AreEqual(1f - StampSpots.FallbackShare, StampSpots.Area(plain).XMin, Eps, "no stamp area: the bottom right");
    }

    [Test]
    public void ThePassportsVisaBox_IsDrawnBolderThanAnyOtherStampArea()
    {
        PlacedForm f = FormLayout.Layout(Booklet(), BookletData(), M.aspect, M, new FakeMeasure());
        FaceRect visa = Of(f, FormItemKind.StampArea).Single().Rect;
        var palette = new FormPalette { Ink = new Rgba(0f, 0f, 0f), Rule = new Rgba(0.3f, 0.3f, 0.3f), BoxFill = new Rgba(1f, 1f, 1f), Band = new Rgba(0.9f, 0.9f, 0.9f),
                                        StampDash = new Rgba(0.4f, 0.4f, 0.4f), Accent = new Rgba(0.2f, 0.2f, 0.5f) };
        FormQuad dash = FormPaint.Quads(f, palette, M).First(q => q.Colour.Equals(palette.StampDash) && q.Rect.YMin == visa.YMin);
        Assert.AreEqual(M.ruleWidth * f.Unit * FormPaint.VisaRules, dash.Rect.Height, 1e-3f);
    }

    // ---------------- The silhouettes and the emblems ----------------

    [Test]
    public void APapersOutline_IsItsRectangle_OrACardsRoundedOne_ConvexAndInside()
    {
        Assert.AreEqual(4, PaperSilhouette.Outline(2f, 1f, 0f).Count);
        Assert.AreEqual(0f, PaperSilhouette.Corner(FormFrame.Booklet, 2f, 1f, 1f));
        float r = PaperSilhouette.Corner(FormFrame.Card, 2f, 1f, 1f);
        Assert.AreEqual(PaperSilhouette.CardCorner, r, Eps);
        List<(float x, float y)> card = PaperSilhouette.Outline(2f, 1f, r);
        Assert.AreEqual(4 * (PaperSilhouette.CornerSteps + 1), card.Count);
        Assert.IsTrue(card.All(p => p.x >= -1f - Eps && p.x <= 1f + Eps && p.y >= -0.5f - Eps && p.y <= 0.5f + Eps), "inside the page");
        for (int i = 0; i < card.Count; i++)
        {
            (float x, float y) a = card[i], b = card[(i + 1) % card.Count], c = card[(i + 2) % card.Count];
            float cross = (b.x - a.x) * (c.y - b.y) - (b.y - a.y) * (c.x - b.x);
            Assert.GreaterOrEqual(cross, -1e-6f, $"convex and counter-clockwise at {i}");
        }
        Assert.AreEqual(0.25f, PaperSilhouette.Corner(FormFrame.Card, 2f, 1f, 100f), Eps, "at most a quarter of the shorter side");
    }

    [Test]
    public void EveryEmblem_InksItsOwnShape_AndAnUnknownOneNothing()
    {
        var seen = new HashSet<string>();
        foreach (string name in EmblemShapes.Names)
        {
            Assert.IsTrue(EmblemShapes.Has(name) && EmblemShapes.Has(name.ToUpperInvariant()), name);
            var bits = new System.Text.StringBuilder();
            int inked = 0;
            for (int y = 0; y < 24; y++)
                for (int x = 0; x < 24; x++)
                {
                    bool on = EmblemShapes.Inked(name, x / 11.5f - 1f, y / 11.5f - 1f);
                    bits.Append(on ? '1' : '0');
                    inked += on ? 1 : 0;
                }
            Assert.That(inked, Is.InRange(40, 480), $"{name} inks a mark, not the whole square");
            Assert.IsTrue(seen.Add(bits.ToString()), $"{name} differs from every other emblem");
        }
        Assert.IsFalse(EmblemShapes.Has("Dragon"));
        Assert.IsFalse(EmblemShapes.Inked("Dragon", 0f, 0f));
    }

    [Test]
    public void ThePassportCovers_RefuseABadColourEmblemOrCode_AndASharedOne()
    {
        var page = new Rgba(0.95f, 0.92f, 0.82f);
        var egypt = new PassportLook { cover = "#1F4A2E", emblem = "WingedSun", code = "EGY" };
        var iraq = new PassportLook { cover = "#1E1E22", emblem = "Octastar", code = "IRQ" };
        CollectionAssert.IsEmpty(PassportCovers.Problems(new[] { ("egypt", egypt), ("iraq", iraq) }, page));
        var bad = new PassportLook { cover = "#F0F0E0", emblem = "Dragon", code = "eg" };
        Assert.AreEqual(3, PassportCovers.Problems(new[] { ("x", bad) }, page).Count, "too light, no such emblem, not three capitals");
        Assert.AreEqual(1, PassportCovers.Problems(new[] { ("x", new PassportLook { cover = "green", emblem = "Star", code = "ITA" }) }, page).Count);
        Assert.AreEqual(3, PassportCovers.Problems(new[] { ("egypt", egypt), ("copy", egypt) }, page).Count, "a shared cover, emblem and code");
        Assert.AreEqual(1, PassportCovers.Problems(new[] { ("none", (PassportLook)null) }, page).Count);
    }
}
