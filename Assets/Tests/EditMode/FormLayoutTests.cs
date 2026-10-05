using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The forms engine (PC spec FO1-FO10, §6): FormLayout turns a FormSpec and
/// its content into placed items and pickable slots in reading order, in the
/// caller's units with the origin at the page's top-left and y down; sizes
/// are fractions of the page height H (FO6). A fake measure stands in for
/// TextMeshPro: every character is 0.5 em wide (0.55 bold), a line is 1.15
/// em, and words wrap. Piece 10's PaperFace tests are folded in here (SlotAt replaces RowAt);
/// the travel documents' (2026-10-05) are in FormLayoutTests.TravelDocuments.cs.
/// </summary>
public partial class FormLayoutTests
{
    private const float Eps = 1e-4f;

    /// <summary>A stand-in for TMP's preferred height: 0.5 em per character (0.55 bold), words wrap, 1.15 em a line, at least one line.</summary>
    private sealed class FakeMeasure : ITextMeasure
    {
        public int Calls;

        public float Height(string text, FormTextRole role, float size, float width)
        {
            Calls++;
            float charWidth = size * (FormTextStyles.IsBold(role) ? 0.55f : 0.5f);
            int perLine = (int)Math.Max(1.0, Math.Min(1e6, Math.Floor(width / charWidth)));
            int lines = 1, used = 0;
            foreach (string word in (text ?? string.Empty).Split(' '))
            {
                int need = used == 0 ? word.Length : used + 1 + word.Length;
                if (need <= perLine)
                {
                    used = need;
                    continue;
                }
                lines += used == 0 ? 0 : 1;
                int w = word.Length;
                while (w > perLine)
                {
                    lines++;
                    w -= perLine;
                }
                used = w;
            }
            return lines * size * 1.15f;
        }
    }

    private static readonly FormMetrics M = new FormMetrics();

    private static FormCell Field(int field, int span) => new FormCell { field = field, span = span };

    private static FormCell Photo(int span) => new FormCell { slot = FormSlots.Photo, span = span, rows = 2 };

    private static FormBlock Row(params FormCell[] cells) => new FormBlock { kind = FormBlockKind.FieldRow, cells = cells };

    private static FormBlock Block(FormBlockKind kind, string text = "") => new FormBlock { kind = kind, text = text };

    /// <summary>Today's passport as a form: the name beside the photo, the birth date under it, the coin and the tongue a row each (a place fact may run to 28 characters).</summary>
    private static FormSpec Passport() => new FormSpec
    {
        blocks = new[]
        {
            Block(FormBlockKind.Header),
            Block(FormBlockKind.Section, "1  HOLDER"),
            Row(Field(0, 8), Photo(4)),
            Row(Field(1, 8)),
            Block(FormBlockKind.Section, "2  PARTICULARS"),
            Row(Field(2, 12)),
            Row(Field(3, 12)),
            Block(FormBlockKind.Footer, "Property of Temporal Customs. Any alteration voids this document.")
        }
    };

    /// <summary>TC-610 from the PC spec §6.2: six fields and the photo, the budget's worst case.</summary>
    private static FormSpec Tc610() => new FormSpec
    {
        blocks = new[]
        {
            Block(FormBlockKind.Header),
            Block(FormBlockKind.Section, "1  DISPLACED PERSON"),
            Row(Field(0, 8), Photo(4)),
            Row(Field(1, 8)),
            Row(Field(2, 6), Field(5, 6)),
            Block(FormBlockKind.Section, "2  INCIDENT"),
            Row(Field(3, 12)),
            Row(Field(4, 6)),
            Block(FormBlockKind.Footer, "Issued under the Temporal Customs Act of 2139. Any alteration voids this document. Property of Temporal Customs; surrender on request.")
        }
    };

    private static FormData PassportData() => new FormData
    {
        Agency = "TEMPORAL CUSTOMS",
        Programme = "Debt Relief Departures",
        FormNumber = "TC-010",
        Title = "Travel Passport",
        Serial = "TC-010/583021",
        HasPhoto = true,
        FieldLabels = new[] { "Full Name", "Date of Birth", "Coin of Issue", "Native Tongue" },
        FieldValues = new[] { "Lysimache", "8 Feb 466 BCE", "Silver drachma (owl)", "Attic Greek" }
    };

    private static FormData Tc610Data() => new FormData
    {
        Agency = "TEMPORAL CUSTOMS",
        Programme = "Debt Relief Departures",
        FormNumber = "TC-610",
        Title = "Displacement Certificate",
        Serial = "TC-610/000417",
        HasPhoto = true,
        FieldLabels = new[] { "Full Name", "Displacement No.", "Date of Birth", "Origin", "Incident", "Valid Until" },
        FieldValues = new[] { "Lysimache", "DP-4471-02", "3 Apr 1476 BCE", "Periclean Athens (Ancient)", "R-0311-07", "9 Jun 2150" }
    };

    /// <summary>The desk paper: page height 1, width the aspect.</summary>
    private static PlacedForm Desk(FormSpec spec, FormData data) => FormLayout.Layout(spec, data, M.aspect, M, new FakeMeasure());

    private static bool Overlaps(FaceRect a, FaceRect b) =>
        a.XMin < b.XMax - Eps && b.XMin < a.XMax - Eps && a.YMin < b.YMax - Eps && b.YMin < a.YMax - Eps;

    private static bool Inside(FaceRect inner, FaceRect outer) =>
        inner.XMin >= outer.XMin - Eps && inner.XMax <= outer.XMax + Eps && inner.YMin >= outer.YMin - Eps && inner.YMax <= outer.YMax + Eps;

    private static IEnumerable<FormItem> Of(PlacedForm f, FormItemKind kind) => f.Items.Where(i => i.Kind == kind);

    private static FormItem TextOf(PlacedForm f, FormTextRole role) => f.Items.First(i => i.Kind == FormItemKind.Text && i.Role == role);

    [Test]
    public void Passport_NoTwoBoxesOrTextsOverlap_AndEveryTextSitsInsideOneBoxOrNone()
    {
        PlacedForm f = Desk(Passport(), PassportData());
        var frames = f.Items.Where(i => i.Kind == FormItemKind.Box || i.Kind == FormItemKind.RowBand || i.Kind == FormItemKind.StampArea).ToList();
        var texts = f.Items.Where(i => i.Kind == FormItemKind.Text).ToList();
        for (int i = 0; i < frames.Count; i++)
            for (int j = i + 1; j < frames.Count; j++)
                Assert.IsFalse(Overlaps(frames[i].Rect, frames[j].Rect), $"{frames[i].Kind} {i} overlaps {frames[j].Kind} {j}");
        for (int i = 0; i < texts.Count; i++)
            for (int j = i + 1; j < texts.Count; j++)
                Assert.IsFalse(Overlaps(texts[i].Rect, texts[j].Rect), $"'{texts[i].Text}' overlaps '{texts[j].Text}'");
        foreach (FormItem t in texts)
        {
            FormItem[] under = frames.Where(b => Overlaps(t.Rect, b.Rect)).ToArray();
            Assert.LessOrEqual(under.Length, 1, $"'{t.Text}' straddles frames");
            if (under.Length == 1)
                Assert.IsTrue(Inside(t.Rect, under[0].Rect), $"'{t.Text}' pokes out of its frame");
        }
    }

    [Test]
    public void Tc610_EveryItemLiesInsideThePage_AtTheSpecSizes()
    {
        PlacedForm f = Desk(Tc610(), Tc610Data());
        var page = new FaceRect(0f, 0f, M.aspect, 1f);
        Assert.AreEqual(1f, f.Height, Eps, "one fixed page");
        Assert.AreEqual(1f, f.PageHeight, Eps);
        foreach (FormItem item in f.Items)
            Assert.IsTrue(Inside(item.Rect, page), $"{item.Kind} '{item.Text}' leaves the page: {item.Rect.XMin:0.000} {item.Rect.YMin:0.000} {item.Rect.XMax:0.000} {item.Rect.YMax:0.000}");
        Assert.AreEqual(M.valueSize, TextOf(f, FormTextRole.Value).Size, Eps, "a short value keeps the full size");
        Assert.AreEqual(M.labelSize, TextOf(f, FormTextRole.Label).Size, Eps);
    }

    [Test]
    public void Header_PrintsTheAgencyProgrammeNumberAndTitle_InCapitals_OverTheSeal()
    {
        PlacedForm f = Desk(Passport(), PassportData());
        Assert.AreEqual("TEMPORAL CUSTOMS", TextOf(f, FormTextRole.Agency).Text);
        Assert.AreEqual("Debt Relief Departures", TextOf(f, FormTextRole.Programme).Text);
        Assert.AreEqual("TC-010", TextOf(f, FormTextRole.FormNumber).Text);
        Assert.AreEqual(FormTextAlign.Right, TextOf(f, FormTextRole.FormNumber).Align);
        Assert.AreEqual("TRAVEL PASSPORT", TextOf(f, FormTextRole.Title).Text);
        FormItem seal = Of(f, FormItemKind.Seal).Single();
        Assert.Less(seal.Rect.YMin, TextOf(f, FormTextRole.Title).Rect.YMax, "the seal is behind the header");
        Assert.Less(TextOf(f, FormTextRole.Title).Rect.YMax, f.Slots[0].Hit.YMin, "the header is above the first box");
    }

    [Test]
    public void ALongTitle_ShrinksToOneLine()
    {
        FormData data = PassportData();
        data.Title = "Debt Relief Labour Contract";
        PlacedForm f = Desk(Passport(), data);
        FormItem title = TextOf(f, FormTextRole.Title);
        Assert.Less(title.Size, M.titleSize);
        Assert.AreEqual(title.Size * M.capsLead, title.Rect.Height, Eps, "one line of capitals");
    }

    [Test]
    public void Slots_AreTheFieldBoxes_InReadingOrder()
    {
        PlacedForm f = Desk(Tc610(), Tc610Data());
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 5, 3, 4 }, f.Slots.Select(s => s.Field).ToArray(), "top to bottom, left to right");
        for (int i = 0; i < f.Slots.Count; i++)
        {
            Assert.AreEqual(i, f.Slots[i].Index);
            Assert.AreEqual(0, f.Slots[i].Page);
            Assert.AreEqual(-1, f.Slots[i].Row);
        }
        for (int i = 1; i < f.Slots.Count; i++)
        {
            FaceRect a = f.Slots[i - 1].Hit, b = f.Slots[i].Hit;
            Assert.IsTrue(b.YMin > a.YMin + Eps || (Math.Abs(b.YMin - a.YMin) < Eps && b.XMin > a.XMax - Eps), $"slot {i} comes after slot {i - 1}");
        }
        foreach (FormSlot s in f.Slots)
        {
            Assert.IsTrue(f.Items.Any(i => i.Kind == FormItemKind.Box && i.Slot == s.Index && Math.Abs(i.Rect.XMin - s.Hit.XMin) < Eps && Math.Abs(i.Rect.YMax - s.Hit.YMax) < Eps), $"slot {s.Index}'s hit is its box");
            Assert.AreEqual(Tc610Data().FieldValues[s.Field], f.Items.First(i => i.Kind == FormItemKind.Text && i.Role == FormTextRole.Value && i.Slot == s.Index).Text);
            Assert.AreEqual(Tc610Data().FieldLabels[s.Field], f.Items.First(i => i.Kind == FormItemKind.Text && i.Role == FormTextRole.Label && i.Slot == s.Index).Text);
        }
    }

    [Test]
    public void Spans_ShareTheGrid_AndThePhotoSpansTwoRows_NarrowingNothingItDoesNotCover()
    {
        PlacedForm f = Desk(Tc610(), Tc610Data());
        float content = M.aspect - 2f * M.marginX;
        float column = (content - 11f * M.gutter) / 12f;
        FaceRect name = f.Slots[0].Hit, number = f.Slots[1].Hit, born = f.Slots[2].Hit, until = f.Slots[3].Hit, origin = f.Slots[4].Hit, incident = f.Slots[5].Hit;
        Assert.AreEqual(M.marginX, name.XMin, Eps);
        Assert.AreEqual(8f * column + 7f * M.gutter, name.Width, Eps, "span 8");
        Assert.AreEqual(name.Width, number.Width, Eps, "the row beside the photo keeps span 8");
        Assert.AreEqual(6f * column + 5f * M.gutter, born.Width, Eps, "span 6");
        Assert.AreEqual(born.XMax + M.gutter, until.XMin, Eps, "one gutter between boxes");
        Assert.AreEqual(content, origin.Width, Eps, "below the photo a span-12 box is full width");
        Assert.AreEqual(born.Width, incident.Width, Eps);

        FormItem frame = f.Items.Single(i => i.Kind == FormItemKind.Box && i.Slot < 0 && Math.Abs(i.Rect.YMin - name.YMin) < Eps);
        Assert.AreEqual(name.XMax + M.gutter, frame.Rect.XMin, Eps, "the photo's box takes the last four columns");
        Assert.AreEqual(number.YMax, frame.Rect.YMax, Eps, "it spans the two rows");
        FormItem photo = Of(f, FormItemKind.Photo).Single();
        Assert.IsTrue(Inside(photo.Rect, frame.Rect));
        Assert.AreEqual(LookCanvas.PhotoAspect, photo.Rect.Width / photo.Rect.Height, 1e-3f, "the portrait keeps its 4:5");
    }

    [Test]
    public void NoPhoto_DrawsTheEmptyFrameOnly()
    {
        FormData data = PassportData();
        data.HasPhoto = false;
        PlacedForm f = Desk(Passport(), data);
        Assert.IsFalse(Of(f, FormItemKind.Photo).Any());
        Assert.AreEqual(5, Of(f, FormItemKind.Box).Count(), "four field boxes and the photo's frame");
        Assert.AreEqual(4, f.Slots.Count);
    }

    [Test]
    public void AValueTooLongForOneLineAtFullSize_ShrinksToTheFloor_WhereItWrapsToTwoLines()
    {
        FormData data = PassportData();
        var measure = new FakeMeasure();
        PlacedForm plain = Desk(Passport(), data);
        float width = TextOf(plain, FormTextRole.Value).Rect.Width;
        int fullChars = (int)Math.Floor(width / (M.valueSize * 0.5f)), floorChars = (int)Math.Floor(width / (M.valueFloor * 0.5f));
        Assume.That(floorChars, Is.GreaterThan(fullChars + 1));

        data.FieldValues = new[] { new string('x', fullChars + 1), "b", "c", "d" };
        FormItem shrunk = Desk(Passport(), data).Items.First(i => i.Role == FormTextRole.Value);
        Assert.AreEqual(M.valueFloor, shrunk.Size, Eps, "one line at the floor");
        Assert.AreEqual(measure.Height("Hg", FormTextRole.Value, M.valueFloor, 1e6f), shrunk.Rect.Height, Eps);

        data.FieldValues = new[] { new string('x', floorChars + 2), "b", "c", "d" };
        PlacedForm two = Desk(Passport(), data);
        FormItem wrapped = two.Items.First(i => i.Role == FormTextRole.Value);
        Assert.AreEqual(M.valueFloor, wrapped.Size, Eps, "two lines at the floor (a box reserves the lines its value needs at the floor)");
        Assert.AreEqual(2f * measure.Height("Hg", FormTextRole.Value, M.valueFloor, 1e6f), wrapped.Rect.Height, Eps);
        Assert.Greater(two.Slots[0].Hit.Height, plain.Slots[0].Hit.Height, "the box grows by the line");
        Assert.AreEqual(two.Slots[0].Hit.Height, two.Items.Single(i => i.Kind == FormItemKind.Box && i.Slot < 0).Rect.Height - two.Slots[1].Hit.Height - M.rowGap, Eps, "the photo follows its rows");
    }

    [Test]
    public void SlotAt_FindsEachBox_AndMissesGuttersHeaderPhotoAndOffThePage()
    {
        PlacedForm f = Desk(Tc610(), Tc610Data());
        foreach (FormSlot s in f.Slots)
        {
            Assert.AreEqual(s.Index, FormLayout.SlotAt(f, s.Hit.CentreX, s.Hit.CentreY), $"slot {s.Index} centre");
            Assert.AreEqual(s.Index, FormLayout.SlotAt(f, s.Hit.XMin + 0.001f, s.Hit.YMin + 0.001f), $"slot {s.Index} top-left");
            Assert.AreEqual(s.Index, FormLayout.SlotAt(f, s.Hit.XMax - 0.001f, s.Hit.YMax - 0.001f), $"slot {s.Index} bottom-right");
        }
        FaceRect born = f.Slots[2].Hit, until = f.Slots[3].Hit;
        Assert.AreEqual(-1, FormLayout.SlotAt(f, (born.XMax + until.XMin) / 2f, born.CentreY), "the gutter");
        Assert.AreEqual(-1, FormLayout.SlotAt(f, f.Slots[1].Hit.CentreX, (f.Slots[1].Hit.YMax + born.YMin) / 2f), "the gap between rows");
        FormItem title = TextOf(f, FormTextRole.Title);
        Assert.AreEqual(-1, FormLayout.SlotAt(f, title.Rect.CentreX, title.Rect.CentreY), "the header");
        FormItem photo = Of(f, FormItemKind.Photo).Single();
        Assert.AreEqual(-1, FormLayout.SlotAt(f, photo.Rect.CentreX, photo.Rect.CentreY), "the photo");
        Assert.AreEqual(-1, FormLayout.SlotAt(f, 5f, 5f), "off the page");
        Assert.AreEqual(-1, FormLayout.SlotAt(null, 0f, 0f), "no layout");
    }

    [Test]
    public void Footer_PrintsTheIssuingLine_TheBarcodeAndSerial_TheStampAreaAndTheFinePrint()
    {
        FormData data = Tc610Data();
        PlacedForm f = Desk(Tc610(), data);
        Assert.AreEqual("Issued by Temporal Customs", f.Items.First(i => i.Role == FormTextRole.Caption).Text);
        Assert.AreEqual(Barcode.Bars(data.Serial, M.barcodeModules).Count, Of(f, FormItemKind.Bar).Count());
        FormItem serial = TextOf(f, FormTextRole.Serial);
        Assert.AreEqual("TC-610/000417", serial.Text);
        FormItem stamp = Of(f, FormItemKind.StampArea).Single();
        Assert.AreEqual(M.aspect - M.marginX, stamp.Rect.XMax, Eps, "the stamp area sits at the right");
        Assert.IsTrue(f.Items.Any(i => i.Kind == FormItemKind.Text && Inside(i.Rect, stamp.Rect) && i.Text == FormLayout.StampCaption));
        FormItem fine = TextOf(f, FormTextRole.FinePrint);
        StringAssert.StartsWith("Issued under the Temporal Customs Act", fine.Text);
        Assert.Greater(fine.Rect.YMin, stamp.Rect.YMax - Eps, "the fine print runs under the stamp area");
        Assert.Greater(Of(f, FormItemKind.Rule).Single().Rect.YMin, f.Slots.Max(s => s.Hit.YMax), "a rule opens the footer below the last box");
    }

    [Test]
    public void NoSerial_NoBarcode()
    {
        FormData data = PassportData();
        data.Serial = string.Empty;
        PlacedForm f = Desk(Passport(), data);
        Assert.IsFalse(Of(f, FormItemKind.Bar).Any());
        Assert.IsFalse(f.Items.Any(i => i.Role == FormTextRole.Serial));
    }

    [Test]
    public void PageBreak_StartsThePageBelow_AndPageOfFollowsIt()
    {
        var spec = new FormSpec
        {
            blocks = new[]
            {
                Block(FormBlockKind.Header),
                Row(Field(0, 12)),
                Block(FormBlockKind.PageBreak),
                Row(Field(1, 6), Field(2, 6)),
                Block(FormBlockKind.Footer)
            }
        };
        Assert.AreEqual(0, spec.PageOf(0));
        Assert.AreEqual(1, spec.PageOf(1));
        Assert.AreEqual(1, spec.PageOf(2));
        Assert.AreEqual(-1, spec.PageOf(3), "never placed");
        Assert.AreEqual(2, spec.PageCount);

        var data = new FormData { FieldLabels = new[] { "A", "B", "C" }, FieldValues = new[] { "a", "b", "c" } };
        PlacedForm f = Desk(spec, data);
        Assert.AreEqual(2f, f.Height, Eps, "two fixed pages");
        CollectionAssert.AreEqual(new[] { 0f, 1f }, f.PageTops.ToArray());
        CollectionAssert.AreEqual(new[] { 0, 1, 1 }, f.Slots.Select(s => s.Page).ToArray());
        Assert.AreEqual(1f + M.marginTop, f.Slots[1].Hit.YMin, Eps, "page 2's first row starts at its top margin");
        Assert.IsTrue(f.Items.Where(i => i.Slot == 1 || i.Slot == 2).All(i => i.Rect.YMin >= 1f - Eps));
    }

    [Test]
    public void AFlowPage_GrowsWithItsTableRows_EachRowASlot()
    {
        var spec = new FormSpec
        {
            formNumber = "TC-920",
            title = "Interview Record",
            fixedPage = false,
            blocks = new[]
            {
                Block(FormBlockKind.Header),
                new FormBlock { kind = FormBlockKind.Table, columns = new[] { "NO", "SPEAKER", "STATEMENT" }, shares = new[] { 0.08f, 0.22f, 0.70f }, slot = "rows" },
                Block(FormBlockKind.FinePrint, "Recorded at the desk.")
            }
        };
        const float width = 542f;
        PlacedForm Flow(int rows)
        {
            var data = new FormData
            {
                FormNumber = spec.formNumber,
                Title = spec.title,
                Rows = new Dictionary<string, IReadOnlyList<string[]>>
                {
                    { "rows", Enumerable.Range(1, rows).Select(r => new[] { r.ToString(), "Desk", "Where were you born?" }).ToList() }
                }
            };
            return FormLayout.Layout(spec, data, width, M, new FakeMeasure());
        }

        PlacedForm two = Flow(2), five = Flow(5);
        Assert.Greater(five.Height, two.Height, "a flow page grows with its rows");
        Assert.AreEqual(width / M.aspect, two.PageHeight, 1e-3f, "sizes are in H, the document page height at this width");
        Assert.AreEqual(5, five.Slots.Count);
        CollectionAssert.AreEqual(Enumerable.Range(0, 5).ToArray(), five.Slots.Select(s => s.Row).ToArray());
        Assert.IsTrue(five.Slots.All(s => s.Field == -1 && s.Source == "rows"));
        Assert.AreEqual(3, five.Items.Count(i => i.Kind == FormItemKind.Text && i.Role == FormTextRole.Label), "the column heads");
        Assert.AreEqual(15, five.Items.Count(i => i.Kind == FormItemKind.Text && i.Role == FormTextRole.Cell));
        FormItem statement = five.Items.First(i => i.Role == FormTextRole.Cell && i.Text.StartsWith("Where"));
        float content = width - 2f * M.marginX * five.PageHeight;
        Assert.AreEqual(0.70f * content, statement.Rect.Width + 2f * M.boxPadding * five.PageHeight, 0.5f, "the column's share of the width");
    }

    [Test]
    public void ARowLinksRoom_WrapsATablesLastColumnShortOfTheLink_TheOtherColumnsAndTheRowBoxKeepTheirWidth()
    {
        var spec = new FormSpec
        {
            fixedPage = false,
            blocks = new[] { new FormBlock { kind = FormBlockKind.Table, columns = new[] { "NO", "SPEAKER", "STATEMENT" }, shares = new[] { 0.12f, 0.24f, 0.64f }, slot = "rows" } }
        };
        const float width = 542f, room = 28f;
        float h = width / M.aspect, pad = M.boxPadding * h, size = M.cellSize * h;
        float statementWidth = 0.64f * (width - 2f * M.marginX * h) - 2f * pad;
        // Short words exactly filling one line of the whole column (FakeMeasure: half an em a letter), so any room wraps the last one.
        int letters = (int)Math.Floor(statementWidth / (size * 0.5f));
        char[] line = string.Concat(Enumerable.Repeat("abcd ", letters / 5 + 1)).Substring(0, letters).ToCharArray();
        line[letters - 1] = 'x';
        string statement = new string(line);
        var data = new FormData { Rows = new Dictionary<string, IReadOnlyList<string[]>> { { "rows", new List<string[]> { new[] { "1", "Desk", statement } } } } };

        PlacedForm plain = FormLayout.Layout(spec, data, width, M, new FakeMeasure());
        PlacedForm linked = FormLayout.Layout(spec, data, width, M, new FakeMeasure(), room);

        FormItem[] a = plain.Items.Where(i => i.Kind == FormItemKind.Text && i.Role == FormTextRole.Cell).OrderBy(i => i.Rect.XMin).ToArray();
        FormItem[] b = linked.Items.Where(i => i.Kind == FormItemKind.Text && i.Role == FormTextRole.Cell).OrderBy(i => i.Rect.XMin).ToArray();
        Assert.AreEqual(3, b.Length);
        for (int i = 0; i < 2; i++)
            Assert.AreEqual(a[i].Rect.Width, b[i].Rect.Width, Eps, "the other columns keep their width");
        Assert.AreEqual(a[2].Rect.Width - (room - pad), b[2].Rect.Width, Eps, "the last column gives up the room less its padding");
        Assert.AreEqual(linked.Slots[0].Hit.XMax - room, b[2].Rect.XMax, Eps, "its text ends where the link's box begins");
        Assert.AreEqual(plain.Slots[0].Hit.Width, linked.Slots[0].Hit.Width, Eps, "the row's box keeps the table's width");
        Assert.AreEqual(1, Math.Round(a[2].Rect.Height / (size * 1.15f)), "the whole column holds the statement on one line");
        Assert.AreEqual(2, Math.Round(b[2].Rect.Height / (size * 1.15f)), "short of the link it wraps");
        Assert.Greater(linked.Slots[0].Hit.Height, plain.Slots[0].Hit.Height, "and its row is as tall as the wrapped text");
        Assert.AreEqual(plain.Items.Count(i => i.Role == FormTextRole.Label), linked.Items.Count(i => i.Role == FormTextRole.Label));
    }

    [Test]
    public void Checkboxes_TickTheOptionEqualToTheFieldsValue_AndPickAsTheField()
    {
        var spec = new FormSpec
        {
            blocks = new[]
            {
                Row(Field(0, 12)),
                new FormBlock { kind = FormBlockKind.Checkboxes, text = "Visa Class", options = new[] { "Premium", "Standard" }, field = 1 }
            }
        };
        var data = new FormData { FieldLabels = new[] { "Name", "Visa Class" }, FieldValues = new[] { "Ada", "standard" } };
        PlacedForm f = Desk(spec, data);
        FormItem[] boxes = Of(f, FormItemKind.Checkbox).ToArray();
        Assert.AreEqual(2, boxes.Length);
        Assert.AreEqual(string.Empty, boxes[0].Text);
        Assert.AreEqual(FormLayout.Tick, boxes[1].Text, "ticked, case aside");
        Assert.AreEqual(1, f.Slots[1].Field);
        Assert.IsTrue(boxes.All(b => Inside(b.Rect, f.Slots[1].Hit)));
    }

    [Test]
    public void Signature_DrawsItsField_OrUnsigned()
    {
        var spec = new FormSpec { blocks = new[] { new FormBlock { kind = FormBlockKind.Signature, text = "Signature of the traveller", field = 0 } } };
        PlacedForm signed = Desk(spec, new FormData { FieldLabels = new[] { "Signature" }, FieldValues = new[] { "A. Lovelace" } });
        PlacedForm blank = Desk(spec, new FormData { FieldLabels = new[] { "Signature" }, FieldValues = new[] { string.Empty } });
        Assert.AreEqual("A. Lovelace", TextOf(signed, FormTextRole.Value).Text);
        Assert.AreEqual(FormLayout.Unsigned, TextOf(blank, FormTextRole.Value).Text);
        Assert.AreEqual("Signature of the traveller", TextOf(blank, FormTextRole.Caption).Text);
        Assert.AreEqual(1, Of(blank, FormItemKind.Rule).Count());
        Assert.AreEqual(0, blank.Slots.Single().Field, "the signature line picks as its field");
    }

    [Test]
    public void Paragraph_PrintsItsTextOrItsSlotsContent()
    {
        var spec = new FormSpec
        {
            blocks = new[]
            {
                Block(FormBlockKind.Paragraph, "The declarant states the above to be true."),
                new FormBlock { kind = FormBlockKind.Paragraph, slot = "note" }
            }
        };
        var data = new FormData { Text = new Dictionary<string, string> { { "note", "No remarks on file." } } };
        FormItem[] paragraphs = Desk(spec, data).Items.Where(i => i.Role == FormTextRole.Paragraph).ToArray();
        CollectionAssert.AreEqual(new[] { "The declarant states the above to be true.", "No remarks on file." }, paragraphs.Select(p => p.Text).ToArray());
        Assert.Greater(paragraphs[1].Rect.YMin, paragraphs[0].Rect.YMax - Eps);
    }

    /// <summary>Today's permit as a form: the device and the bond currency a row each, and the declaration.</summary>
    private static FormSpec Permit() => new FormSpec
    {
        blocks = new[]
        {
            Block(FormBlockKind.Header),
            Block(FormBlockKind.Section, "1  DECLARATION"),
            Row(Field(0, 12)),
            Row(Field(1, 12)),
            Block(FormBlockKind.Paragraph, "The bearer declares the above to be carried and true."),
            Block(FormBlockKind.Footer, "Property of Temporal Customs. Any alteration voids this document.")
        }
    };

    [Test]
    public void Check_TodaysPassportAndPermit_FitAtTheirLongestValues()
    {
        var measure = new FakeMeasure();
        List<string> passport = FormLayout.Check(Passport(), FormLayout.Probe(PassportData(), new[] { 28, 15, 28, 28 }), M, measure);
        Assert.IsTrue(passport.Count == 0, "passport: " + string.Join(" | ", passport));
        var permitData = new FormData { Agency = "TEMPORAL CUSTOMS", Title = "Transit Permit", FormNumber = "TC-020", Serial = "TC-020/000001", FieldLabels = new[] { "Declared Device", "Bond Currency" }, FieldValues = new[] { "a", "b" } };
        List<string> permit = FormLayout.Check(Permit(), FormLayout.Probe(permitData, new[] { 28, 28 }), M, measure);
        Assert.IsTrue(permit.Count == 0, "permit: " + string.Join(" | ", permit));
    }

    [Test]
    public void Probe_FillsEachValueWithItsLongestLength_AndKeepsTheRest()
    {
        FormData probe = FormLayout.Probe(PassportData(), new[] { 28, 15, 3, 0 });
        CollectionAssert.AreEqual(new[] { 28, 15, 3, 0 }, probe.FieldValues.Select(v => v.Length).ToArray());
        Assert.IsTrue(probe.FieldValues[0].Contains(" "), "a probe wraps at words, like a real value");
        CollectionAssert.AreEqual(PassportData().FieldLabels, probe.FieldLabels);
        Assert.AreEqual("TRAVEL PASSPORT", probe.Title.ToUpperInvariant());
        Assert.AreEqual(PassportData().Serial, probe.Serial);
    }

    [Test]
    public void Check_ReportsUnplacedTwicePlacedAndUnknownFields()
    {
        var spec = new FormSpec { blocks = new[] { Row(Field(0, 6), Field(0, 6)), Row(Field(7, 12)) } };
        var data = new FormData { FieldLabels = new[] { "A", "B" }, FieldValues = new[] { "a", "b" } };
        List<string> problems = FormLayout.Check(spec, data, M, new FakeMeasure());
        Assert.IsTrue(problems.Any(p => p.Contains("field 0") && p.Contains("twice")), string.Join("\n", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("field 1") && p.Contains("never")), string.Join("\n", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("field 7") && p.Contains("2 fields")), string.Join("\n", problems));
    }

    [Test]
    public void Check_ReportsAFaceThatDoesNotFit_AValueOverTwoLines_TooManyColumns_AndAPhotoMismatch()
    {
        var measure = new FakeMeasure();
        var tall = new FormSpec { blocks = Enumerable.Range(0, 10).Select(i => Row(Field(i, 12))).Prepend(Block(FormBlockKind.Header)).ToArray() };
        var tallData = new FormData { FieldLabels = Enumerable.Range(0, 10).Select(i => "Label " + i).ToArray(), FieldValues = Enumerable.Range(0, 10).Select(i => "v").ToArray() };
        Assert.IsTrue(FormLayout.Check(tall, tallData, M, measure).Any(p => p.Contains("past the page")));

        FormData longValue = FormLayout.Probe(PassportData(), new[] { 28, 15, 200, 28 });
        Assert.IsTrue(FormLayout.Check(Passport(), longValue, M, measure).Any(p => p.Contains("Coin of Issue") && p.Contains("lines")));

        var wide = new FormSpec { blocks = new[] { Row(Field(0, 8), Field(1, 8)) } };
        Assert.IsTrue(FormLayout.Check(wide, new FormData { FieldLabels = new[] { "A", "B" }, FieldValues = new[] { "a", "b" } }, M, measure).Any(p => p.Contains("columns")));

        FormData noPhoto = PassportData();
        noPhoto.HasPhoto = false;
        Assert.IsTrue(FormLayout.Check(Passport(), noPhoto, M, measure).Any(p => p.Contains("photo")));
        var photoless = new FormSpec { blocks = new[] { Row(Field(0, 12)), Row(Field(1, 12)), Row(Field(2, 12)), Row(Field(3, 12)) } };
        Assert.IsTrue(FormLayout.Check(photoless, PassportData(), M, measure).Any(p => p.Contains("photo")));
    }

    [Test]
    public void Layout_IsDeterministic_AndMeasuresEachTextOnce()
    {
        var measure = new FakeMeasure();
        PlacedForm a = FormLayout.Layout(Tc610(), Tc610Data(), M.aspect, M, measure);
        int calls = measure.Calls;
        PlacedForm b = FormLayout.Layout(Tc610(), Tc610Data(), M.aspect, M, new FakeMeasure());
        Assert.AreEqual(a.Items.Count, b.Items.Count);
        for (int i = 0; i < a.Items.Count; i++)
        {
            Assert.AreEqual(a.Items[i].Kind, b.Items[i].Kind);
            Assert.AreEqual(a.Items[i].Text, b.Items[i].Text);
            Assert.AreEqual(a.Items[i].Rect.YMin, b.Items[i].Rect.YMin, Eps);
        }
        Assert.Less(calls, 80, "measuring stays cheap (one bind per paper)");
    }

    [Test]
    public void TextStyles_BoldAndCapitalsFollowTheRole()
    {
        foreach (FormTextRole role in Enum.GetValues(typeof(FormTextRole)))
        {
            bool caps = role == FormTextRole.Agency || role == FormTextRole.Title || role == FormTextRole.Section || role == FormTextRole.Label;
            Assert.AreEqual(caps, FormTextStyles.IsBold(role), role.ToString());
            Assert.AreEqual(caps, FormTextStyles.IsCapitals(role), role.ToString());
            Assert.AreEqual(role == FormTextRole.Label, FormTextStyles.IsSmallCaps(role), role.ToString());
        }
    }

    [Test]
    public void ALineOfCapitals_TakesTheCapsLead_AndAWrappedLabelAMeasuredLineMore()
    {
        FormData data = PassportData();
        PlacedForm f = Desk(Passport(), data);
        FormItem label = TextOf(f, FormTextRole.Label);
        Assert.AreEqual(M.labelSize * M.capsLead, label.Rect.Height, Eps, "capitals have no descenders");
        FormItem section = f.Items.First(i => i.Role == FormTextRole.Section);
        Assert.AreEqual(M.sectionSize * M.capsLead, section.Rect.Height, Eps);

        data.FieldLabels = new[] { "Full Name of the Bearer at Birth", "Date of Birth", "Coin of Issue", "Native Tongue" };
        FormItem wrapped = TextOf(Desk(Passport(), data), FormTextRole.Label);
        var measure = new FakeMeasure();
        Assert.AreEqual(measure.Height("Hg", FormTextRole.Label, M.labelSize, 1e6f) + M.labelSize * M.capsLead, wrapped.Rect.Height, Eps, "a long label wraps: a measured line, then a line of capitals");
    }

    // ---------------- Phase 5: the PC width and the page kinds ----------------

    /// <summary>The PC's page width (PC spec §6.3): 542 u, the 580 u pane less its padding and scrollbar.</summary>
    private const float PcWidth = 542f;

    /// <summary>A page kind laid out at the PC width.</summary>
    private static PlacedForm Pc(FormSpec spec, FormData data) => FormLayout.Layout(spec, data, PcWidth, M, new FakeMeasure());

    [Test]
    public void AtThePcWidth_ADocumentPageIs708u_AndPrintsAtTheSpecSizes()
    {
        PlacedForm f = Pc(Tc610(), Tc610Data());
        Assert.AreEqual(708.5f, f.PageHeight, 0.1f, "H = 542 / 0.765");
        Assert.AreEqual(f.PageHeight, f.Height, Eps, "a document is one fixed page");
        float Size(FormTextRole role) => f.Items.Where(i => i.Kind == FormItemKind.Text && i.Role == role).Min(i => i.Size);
        Assert.AreEqual(34.7f, f.Items.Where(i => i.Role == FormTextRole.Value).Max(i => i.Size), 0.1f, "a value at 0.049 H");
        Assert.GreaterOrEqual(Size(FormTextRole.Value), 0.042f * f.PageHeight - Eps, "no value under the floor");
        Assert.AreEqual(26.9f, Size(FormTextRole.Label), 0.1f, "a label at 0.038 H");
        Assert.AreEqual(25.5f, Size(FormTextRole.Section), 0.1f, "a section head at 0.036 H");
        Assert.AreEqual(14.2f, Size(FormTextRole.FinePrint), 0.1f, "fine print at 0.020 H");
        Assert.IsTrue(f.Items.All(i => Inside(i.Rect, new FaceRect(0f, 0f, PcWidth, f.PageHeight))), "every item on the page");
        PlacedForm desk = Desk(Tc610(), Tc610Data());
        Assert.AreEqual(desk.Items.Count, f.Items.Count, "the same form as the desk paper");
        for (int i = 0; i < f.Items.Count; i++)
        {
            Assert.AreEqual(desk.Items[i].Text, f.Items[i].Text);
            Assert.AreEqual(desk.Items[i].Rect.YMin * f.PageHeight, f.Items[i].Rect.YMin, 0.01f, "the same place in page heights");
        }
    }

    /// <summary>Today's record extract (PC spec §6.2, TC-901): the query line, the record's groups, the note, the stamp area and fine print.</summary>
    private static FormSpec RecordExtract() => new FormSpec
    {
        formNumber = "TC-901",
        title = "Record Extract",
        fixedPage = false,
        blocks = new[]
        {
            Block(FormBlockKind.Header),
            new FormBlock { kind = FormBlockKind.Paragraph, slot = "query" },
            new FormBlock { kind = FormBlockKind.RecordGroups, slot = "groups" },
            new FormBlock { kind = FormBlockKind.Paragraph, slot = "note" },
            Block(FormBlockKind.StampArea),
            Block(FormBlockKind.FinePrint, "Extract of the agency's records, valid on the day of issue.")
        }
    };

    [Test]
    public void RecordGroups_EachGroupANumberedSection_RowsTwoToARow_ALongValueAcross_EachRowASlot()
    {
        var data = FormData.Page(RecordExtract(), "Temporal Customs", "Debt Relief Departures");
        data.Text = new Dictionary<string, string> { { "query", "Query \"552-1804-33\" · 1 record on file" }, { "note", "No remarks on file." } };
        data.Groups = new[]
        {
            new FormGroup("Records", new[] { ("Name", "Oren Hale"), ("Citizen ID", "552-1804-33"), ("Born", "2 Feb 2117") }),
            new FormGroup("Travel", new[] { ("Booked departure", "Debt Relief departure to the Industrial era, gate 4, on the first free day"), ("History", "None") })
        };
        PlacedForm f = Pc(RecordExtract(), data);

        CollectionAssert.AreEqual(new[] { "1  RECORDS", "2  TRAVEL" }, f.Items.Where(i => i.Role == FormTextRole.Section).Select(i => i.Text).ToArray());
        Assert.AreEqual(5, f.Slots.Count, "a slot per row");
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, f.Slots.Select(s => s.Row).ToArray(), "rows counted across the groups");
        Assert.IsTrue(f.Slots.All(s => s.Source == "groups" && s.Field == -1));
        float content = PcWidth - 2f * M.marginX * f.PageHeight;
        Assert.AreEqual(f.Slots[0].Hit.YMin, f.Slots[1].Hit.YMin, Eps, "Name and Citizen ID share a row");
        Assert.Less(f.Slots[0].Hit.Width, content / 2f, "a half-row box");
        Assert.Greater(f.Slots[2].Hit.YMin, f.Slots[0].Hit.YMax - Eps, "Born starts the next row");
        Assert.AreEqual(content, f.Slots[3].Hit.Width, 0.5f, "the long departure goes across the row");
        Assert.Greater(f.Slots[4].Hit.YMin, f.Slots[3].Hit.YMax - Eps, "the row after the long one");
        FormItem section2 = f.Items.First(i => i.Text == "2  TRAVEL");
        Assert.Greater(section2.Rect.YMin, f.Slots[2].Hit.YMax - Eps, "a group's section follows the last row of the one before");
        Assert.IsTrue(f.Items.Any(i => i.Role == FormTextRole.Value && i.Text == "552-1804-33") && f.Items.Any(i => i.Role == FormTextRole.Label && i.Text == "Citizen ID"));
        FormItem note = f.Items.Last(i => i.Kind == FormItemKind.Text && i.Role == FormTextRole.Paragraph);
        Assert.AreEqual("No remarks on file.", note.Text);
        Assert.Greater(note.Rect.YMin, f.Slots[4].Hit.YMax - Eps, "the note under the groups");

        data.Groups = Array.Empty<FormGroup>();
        PlacedForm none = Pc(RecordExtract(), data);
        Assert.AreEqual(0, none.Slots.Count, "no record: no groups");
        Assert.Less(none.Height, f.Height, "a flow page is as tall as its content");
    }

    [Test]
    public void ATable_OneCellRowIsAHeadingAcross_NotASlot_AndTheRowsKeepTheirIndex()
    {
        var spec = new FormSpec
        {
            formNumber = "TC-916",
            title = "Register",
            fixedPage = false,
            blocks = new[] { new FormBlock { kind = FormBlockKind.Table, columns = new[] { "PLACE", "ERA", "VALUE", "NOTE" }, shares = new[] { 0.34f, 0.16f, 0.36f, 0.14f }, slot = "rows" } }
        };
        var data = FormData.Page(spec, "Temporal Customs", "Debt Relief Departures");
        data.Rows = new Dictionary<string, IReadOnlyList<string[]>>
        {
            { "rows", new List<string[]> { new[] { "Medieval" }, new[] { "Florence", "Medieval", "Guild robe", "" }, new[] { "Modern" }, new[] { "Berlin", "Modern", "Suit", "" } } }
        };
        PlacedForm f = Pc(spec, data);
        CollectionAssert.AreEqual(new[] { 1, 3 }, f.Slots.Select(s => s.Row).ToArray(), "the headings are no slots; each row keeps its index");
        CollectionAssert.AreEqual(new[] { "MEDIEVAL", "MODERN" }, f.Items.Where(i => i.Role == FormTextRole.Section).Select(i => i.Text).ToArray());
        FormItem medieval = f.Items.First(i => i.Text == "MEDIEVAL");
        Assert.Less(medieval.Rect.YMax, f.Slots[0].Hit.YMin + Eps, "the heading comes before its rows");
        Assert.AreEqual(2, f.Items.Count(i => i.Kind == FormItemKind.RowBand && i.Rect.Width > PcWidth * 0.8f && i.Rect.YMin > f.Items.First(h => h.Text == "PLACE").Rect.YMax), "a band across the table under each heading");
    }

    [Test]
    public void OnAPageKind_TheSignatureIsItsSlotsText()
    {
        var spec = new FormSpec { fixedPage = false, blocks = new[] { new FormBlock { kind = FormBlockKind.Signature, text = "For the Customs Directorate", slot = "signature" } } };
        var data = new FormData { Text = new Dictionary<string, string> { { "signature", "Customs Directorate" } } };
        PlacedForm f = Pc(spec, data);
        Assert.AreEqual("Customs Directorate", TextOf(f, FormTextRole.Value).Text);
        Assert.AreEqual("For the Customs Directorate", TextOf(f, FormTextRole.Caption).Text);
        Assert.AreEqual(FormLayout.Unsigned, TextOf(Pc(spec, new FormData()), FormTextRole.Value).Text, "no text: the line is unsigned");
    }

    [Test]
    public void APageKindsHeading_IsTheAgencyOverItsOwnNumberAndTitle()
    {
        FormData data = FormData.Page(RecordExtract(), "Temporal Customs", "Debt Relief Departures");
        Assert.AreEqual(("Temporal Customs", "Debt Relief Departures", "TC-901", "Record Extract"), (data.Agency, data.Programme, data.FormNumber, data.Title));
        PlacedForm f = Pc(RecordExtract(), data);
        Assert.AreEqual("RECORD EXTRACT", TextOf(f, FormTextRole.Title).Text);
        Assert.AreEqual("TC-901", TextOf(f, FormTextRole.FormNumber).Text);
        Assert.AreEqual(string.Empty, FormData.Page(null, null, null).FormNumber);
    }

    [Test]
    public void ALandscapePage_TakesHAsItsWidthTimesTheAspect_SoAWideTableKeepsPortraitSizes()
    {
        var spec = new FormSpec
        {
            fixedPage = false,
            landscape = true,
            blocks = new[] { new FormBlock { kind = FormBlockKind.Table, columns = new[] { "DAY", "WAGES", "FINES", "DEBT RELIEF", "HOUSEHOLD", "PURCHASES", "BALANCE", "OWED" }, slot = "rows" } }
        };
        var data = new FormData { Rows = new Dictionary<string, IReadOnlyList<string[]>> { { "rows", new List<string[]> { new[] { "1", "1,250", "0", "312", "180", "120", "3,450", "124,806" } } } } };
        const float width = 980f;
        PlacedForm f = FormLayout.Layout(spec, data, width, M, new FakeMeasure());
        Assert.AreEqual(width * M.aspect, f.PageHeight, 0.01f, "the long side across");
        Assert.AreEqual(M.cellSize * width * M.aspect, f.Items.First(i => i.Role == FormTextRole.Cell).Size, 0.01f);
        spec.landscape = false;
        Assert.AreEqual(width / M.aspect, FormLayout.Layout(spec, data, width, M, new FakeMeasure()).PageHeight, 0.01f, "portrait: width over the aspect");
    }

    [Test]
    public void ATableCell_WhoseWordDoesNotFitItsColumn_PrintsSmaller_DownToTheFloor_SoNoWordBreaks()
    {
        var spec = new FormSpec
        {
            fixedPage = false,
            blocks = new[] { new FormBlock { kind = FormBlockKind.Table, columns = new[] { "No.", "Place", "Contradicted by" }, shares = new[] { 0.1f, 0.2f, 0.7f }, slot = "rows" } }
        };
        var data = new FormData
        {
            Rows = new Dictionary<string, IReadOnlyList<string[]>>
            {
                { "rows", new List<string[]> { new[] { "1", "Florence", "Oil lamp and a long line that wraps at its spaces across the wide column" }, new[] { "Transponder", "Rome", "x" } } }
            }
        };
        PlacedForm f = Pc(spec, data);
        float h = f.PageHeight, full = M.cellSize * h, floor = M.cellFloor * h;
        FormItem Cell(string text) => f.Items.First(i => i.Text == text);

        Assert.AreEqual(full, Cell("1").Size, 0.01f, "a word that fits keeps the cell size");
        Assert.Less(Cell("Florence").Size, full - 0.01f, "a word wider than its column prints smaller");
        Assert.GreaterOrEqual(Cell("Florence").Size, floor - 0.01f);
        FormItem florence = Cell("Florence");
        Assert.AreEqual(1, (int)System.Math.Round(florence.Rect.Height / (florence.Size * 1.15f)), "and keeps one line: the word does not break");
        FormItem wide = f.Items.First(i => i.Text.StartsWith("Oil lamp"));
        Assert.AreEqual(full, wide.Size, 0.01f, "a long text of short words wraps at its spaces at the cell size");
        Assert.Less(Cell("No.").Size, full - 0.01f, "a head is sized the same way");
        Assert.GreaterOrEqual(Cell("No.").Size, floor - 0.01f);
        Assert.AreEqual(full, Cell("Contradicted by").Size, 0.01f, "a head that fits keeps the cell size");
        Assert.AreEqual(floor, Cell("Transponder").Size, 0.01f, "a word that does not fit even at the floor prints at the floor");
    }

    [Test]
    public void APageKindAtAPaneWidth_PrintsLarger_TheWidthIsTheCallers()
    {
        var spec = new FormSpec { fixedPage = false, blocks = new[] { new FormBlock { kind = FormBlockKind.Table, columns = new[] { "PLACE", "VALUE" }, slot = "rows" } } };
        var data = new FormData { Rows = new Dictionary<string, IReadOnlyList<string[]>> { { "rows", new List<string[]> { new[] { "Florence (Medieval)", "Wool tunic" } } } } };
        float Cell(float width) => FormLayout.Layout(spec, data, width, M, new FakeMeasure()).Items.First(i => i.Role == FormTextRole.Cell).Size;
        Assert.AreEqual(M.cellSize * PcWidth / M.aspect, Cell(PcWidth), 0.01f, "at 542 u a cell is 0.034 H of 708 u");
        const float pane = 860f;
        Assert.AreEqual(Cell(PcWidth) * pane / PcWidth, Cell(pane), 0.01f, "at a pane's width it grows with the width");
    }

    // ---------------- The document design spec (2026-09-30): D1, D2, D4, D8 ----------------

    /// <summary>TC-610 as the templates print it now: the seal field (6) in the header, the photo cell naming the photo field (7).</summary>
    private static FormSpec Tc610Sealed(FormLook look = null)
    {
        FormSpec spec = Tc610();
        spec.blocks[0].field = 6;
        foreach (FormBlock b in spec.blocks)
            foreach (FormCell c in b.cells ?? new FormCell[0])
                if (c.IsPhoto)
                    c.field = 7;
        spec.look = look ?? new FormLook();
        return spec;
    }

    private static FormData Tc610SealedData(params string[] values)
    {
        FormData data = Tc610Data();
        data.FieldLabels = data.FieldLabels.Concat(new[] { "Issuing Seal", "Photo" }).ToArray();
        data.FieldValues = values.Length > 0 ? values : data.FieldValues.Concat(new[] { "Violet circle · DP", "m/skin3/face-a/black" }).ToArray();
        data.FieldReserve = FormLayout.Reserve(data.FieldValues, new[] { 28, 16, 16, 28, 16, 16, 28, 28 });
        return data;
    }

    [Test]
    public void AReservedBox_NeverMoves_WhateverItsValue()
    {
        PlacedForm longest = Desk(Tc610Sealed(), Tc610SealedData());
        FormData shortValues = Tc610SealedData("Io", "D", "1 Jan 1", "Rome", "R", "1 Jan 2150", "Violet circle · DP", "x");
        shortValues.FieldReserve = Tc610SealedData().FieldReserve;
        PlacedForm brief = Desk(Tc610Sealed(), shortValues);
        FormData blank = Tc610SealedData();
        blank.FieldValues = blank.FieldValues.Select(_ => string.Empty).ToArray();
        PlacedForm empty = Desk(Tc610Sealed(), blank);
        foreach (PlacedForm other in new[] { brief, empty })
            for (int i = 0; i < longest.Slots.Count; i++)
            {
                Assert.AreEqual(longest.Slots[i].Hit.YMin, other.Slots[i].Hit.YMin, Eps, $"slot {i}'s top");
                Assert.AreEqual(longest.Slots[i].Hit.YMax, other.Slots[i].Hit.YMax, Eps, $"slot {i}'s bottom");
            }
        List<string> problems = FormLayout.Check(Tc610Sealed(), Tc610SealedData(), M, new FakeMeasure());
        Assert.IsFalse(problems.Any(p => p.Contains("moves")), "the check lays the form out full and empty, no box moves: " + string.Join("; ", problems));
    }

    [Test]
    public void AValueLongerThanItsReserve_ShrinksBelowTheFloor_InsteadOfMovingItsBox()
    {
        FormData data = Tc610SealedData();
        PlacedForm reserved = Desk(Tc610Sealed(), data);
        FormData over = Tc610SealedData();
        over.FieldReserve = data.FieldReserve;
        var values = over.FieldValues.ToArray();
        values[3] = "Periclean Athens and the Long Walls to the Piraeus (Ancient)";
        over.FieldValues = values;
        PlacedForm squeezed = Desk(Tc610Sealed(), over);
        for (int i = 0; i < reserved.Slots.Count; i++)
            Assert.AreEqual(reserved.Slots[i].Hit.YMax, squeezed.Slots[i].Hit.YMax, Eps, $"slot {i}");
        FormItem origin = squeezed.Items.First(i => i.Kind == FormItemKind.Text && i.Text == values[3]);
        Assert.Less(origin.Size, M.valueFloor, "below the floor, so the box keeps its size");
    }

    [Test]
    public void TheOfficeSeal_IsAPickableBoxAtTheHeadersRight_ClearOfTheWords()
    {
        PlacedForm f = Desk(Tc610Sealed(), Tc610SealedData());
        FormSlot seal = f.Slots.First(s => s.Field == 6);
        Assert.AreEqual(0, seal.Index, "the header's seal is the first slot in reading order");
        FormItem mark = Of(f, FormItemKind.Seal).Single();
        Assert.AreEqual("Violet circle · DP", mark.Text, "the seal's value, for the renderers to draw");
        Assert.AreEqual(seal.Index, mark.Slot);
        Assert.AreEqual(M.aspect - M.marginX, seal.Hit.XMax, Eps, "at the content's right edge");
        Assert.AreEqual(M.sealSize, seal.Hit.Width, Eps);
        foreach (FormTextRole role in new[] { FormTextRole.Agency, FormTextRole.Programme, FormTextRole.Title, FormTextRole.FormNumber })
            Assert.IsFalse(Overlaps(TextOf(f, role).Rect, seal.Hit), $"the {role} stays clear of the seal");
        Assert.AreEqual(6, FormLayout.SlotAt(f, seal.Hit.CentreX, seal.Hit.CentreY) >= 0 ? f.Slots[FormLayout.SlotAt(f, seal.Hit.CentreX, seal.Hit.CentreY)].Field : -1);
    }

    [Test]
    public void ThePhoto_IsAPickableBox_OfItsField()
    {
        PlacedForm f = Desk(Tc610Sealed(), Tc610SealedData());
        FormSlot photo = f.Slots.First(s => s.Field == 7);
        FormItem picture = Of(f, FormItemKind.Photo).Single();
        Assert.IsTrue(Inside(picture.Rect, photo.Hit), "the picture lies in its box");
        Assert.AreEqual(photo.Index, picture.Slot);
        Assert.AreEqual(photo.Index, FormLayout.SlotAt(f, picture.Rect.CentreX, picture.Rect.CentreY));
    }

    [Test]
    public void EachFrame_LiesInTheMargins_AndMovesNothing()
    {
        PlacedForm plain = Desk(Tc610Sealed(), Tc610SealedData());
        Assert.IsEmpty(Of(plain, FormItemKind.Stripe));
        foreach (FormFrame frame in (FormFrame[])Enum.GetValues(typeof(FormFrame)))
        {
            if (frame == FormFrame.Plain)
                continue;
            PlacedForm f = Desk(Tc610Sealed(new FormLook { frame = frame, accent = "#1C3A78" }), Tc610SealedData());
            Assert.IsNotEmpty(Of(f, FormItemKind.Stripe).Concat(Of(f, FormItemKind.Cover)), frame.ToString());
            foreach (FormItem band in Of(f, FormItemKind.Stripe).Concat(Of(f, FormItemKind.Perforation)).Concat(Of(f, FormItemKind.Cover)))
            {
                Assert.IsTrue(Inside(band.Rect, new FaceRect(0f, 0f, M.aspect, 1f)), $"{frame}: on the page");
                foreach (FormSlot s in f.Slots)
                    Assert.IsFalse(Overlaps(band.Rect, s.Hit), $"{frame}: a band over slot {s.Index}");
            }
            for (int i = 0; i < plain.Slots.Count; i++)
                Assert.AreEqual(plain.Slots[i].Hit.YMin, f.Slots[i].Hit.YMin, Eps, $"{frame}: slot {i}");
        }
        Assert.IsNotEmpty(Of(Desk(Tc610Sealed(new FormLook { frame = FormFrame.Ticket, accent = "#1C3A78" }), Tc610SealedData()), FormItemKind.Perforation), "a ticket's perforation");
    }

    [Test]
    public void ALooksAspect_SetsThePageHeight_AndThePrintFollowsTheWidth()
    {
        const float aspect = 0.7f;
        PlacedForm f = FormLayout.Layout(Tc610Sealed(new FormLook { aspect = aspect }), Tc610SealedData(), M.aspect, M, new FakeMeasure());
        Assert.AreEqual(M.aspect / aspect, f.PageHeight, Eps, "the page's height = width / the look's aspect: a narrower look is a longer page");
        Assert.AreEqual(1f, f.Unit, Eps, "the print unit = width / the style's aspect (the travel documents spec, TD2)");
        Assert.AreEqual(M.valueSize, TextOf(f, FormTextRole.Value).Size, Eps, "the print keeps the style page's size");
        Assert.AreEqual(M.aspect - 2f * M.marginX, f.Slots.Max(s => s.Hit.XMax) - M.marginX, Eps, "the grid fills the page's width");
    }

    [Test]
    public void TheSealRegister_PicturesEachSealOverItsOffice_ThreeToARow()
    {
        var spec = new FormSpec { fixedPage = false, blocks = new[] { new FormBlock { kind = FormBlockKind.SealGrid, slot = FormSlots.Seals } } };
        var data = new FormData
        {
            Seals = new[] { ("Visa Office", "Blue hexagon · VO"), ("Portal Hall Dispatch", "Green circle · PH"), ("Debt Relief Directorate", "Red shield · DR"), ("Returns Office", "Green shield · RO") }
        };
        PlacedForm f = FormLayout.Layout(spec, data, PcWidth, M, new FakeMeasure());
        Assert.AreEqual(4, f.Slots.Count);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, f.Slots.Select(s => s.Row));
        Assert.IsTrue(f.Slots.All(s => s.Source == FormSlots.Seals && s.Field == -1));
        Assert.AreEqual(f.Slots[0].Hit.YMin, f.Slots[2].Hit.YMin, Eps, "three to a row");
        Assert.Greater(f.Slots[3].Hit.YMin, f.Slots[0].Hit.YMax, "the fourth starts the next row");
        List<FormItem> seals = Of(f, FormItemKind.Seal).ToList();
        CollectionAssert.AreEqual(data.Seals.Select(s => s.Value), seals.Select(s => s.Text));
        for (int i = 0; i < seals.Count; i++)
            Assert.IsTrue(Inside(seals[i].Rect, f.Slots[i].Hit), $"seal {i} in its box");
        Assert.IsTrue(f.Items.Any(i => i.Kind == FormItemKind.Text && i.Text == "Returns Office" && i.Slot == 3));
    }

    [Test]
    public void ALooksPalette_TintsThePaperAndBands_AndKeepsTheInks()
    {
        var style = new FormPalette { Paper = new Rgba(0.95f, 0.92f, 0.82f), Ink = new Rgba(0.1f, 0.1f, 0.1f), Band = new Rgba(0.87f, 0.84f, 0.76f), BoxFill = new Rgba(1f, 1f, 1f) };
        FormPalette p = new FormLook { paper = "#EEF2F6", accent = "#1C3A78" }.Palette(style);
        Assert.AreEqual(0xEE / 255f, p.Paper.R, 1e-3f);
        Assert.AreEqual(0x1C / 255f, p.Accent.R, 1e-3f);
        Assert.Greater(p.BoxFill.R, p.Paper.R - 1e-4f, "a box is a lighter shade of the paper");
        Assert.Less(p.Band.B, p.Paper.B + 1e-4f, "a section band is a pale shade of the accent over the paper");
        Assert.AreEqual(style.Ink.R, p.Ink.R, 1e-6f, "the inks are the style's");
        Assert.AreEqual(style.Paper.R, new FormLook().Palette(style).Paper.R, 1e-6f, "no tint: the style's paper");
        CollectionAssert.IsEmpty(new FormLook { frame = FormFrame.TopBand, accent = "#1C3A78", aspect = 0.7f, scale = 0.9f }.Problems(M.aspect));
        CollectionAssert.IsEmpty(new FormLook { frame = FormFrame.Card, accent = "#1C3A78", aspect = 1.586f, scale = 0.35f }.Problems(M.aspect), "a card: wider than the style's page, small on the desk");
        Assert.AreEqual(4, new FormLook { frame = FormFrame.TopBand, paper = "blue", aspect = 2.5f, scale = 2f }.Problems(M.aspect).Count);
        Assert.AreEqual(2, new FormLook { aspect = 0.4f, scale = 0.2f }.Problems(M.aspect).Count, "too narrow, too small");
    }
}
