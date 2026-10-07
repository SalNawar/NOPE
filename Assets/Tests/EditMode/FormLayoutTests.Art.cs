using System.Linq;
using NUnit.Framework;

/// <summary>
/// A document drawn on its art (the Canva documents, run 7; ArtLayout): the
/// values print and are picked at the art's places, a relabelled field prints
/// its own label, a field not introduced is patched from the blank face, the
/// photo fits its window at 4:5, the seal sits at its spot, a booklet's covers,
/// spine and ENTRY VISA box (the stamps' area), the prints that are no field,
/// and the art's face check.
/// </summary>
public partial class FormLayoutTests
{
    private static ArtBox Box(float x0, float y0, float x1, float y1) => new ArtBox { x0 = x0, y0 = y0, x1 = x1, y1 = y1 };

    /// <summary>The passport (Booklet's blocks: the header's seal is field 6, the photo cell's field 7) on an art 0.5 wide.</summary>
    private static FormSpec ArtPassport()
    {
        FormSpec spec = Booklet();
        spec.look.aspect = 0.5f;
        spec.look.art = new FormArt
        {
            valueShare = 0.5f,
            labelShare = 0.6f,
            fields = new[]
            {
                new ArtField { field = 0, value = Box(0.5f, 0.60f, 0.9f, 0.64f), label = Box(0.5f, 0.57f, 0.7f, 0.59f) },
                new ArtField { field = 1, value = Box(0.5f, 0.66f, 0.7f, 0.70f), label = Box(0.5f, 0.645f, 0.6f, 0.655f) },
                new ArtField { field = 2, value = Box(0.7f, 0.66f, 0.9f, 0.70f), label = Box(0.7f, 0.645f, 0.8f, 0.655f) },
                new ArtField { field = 3, value = Box(0.5f, 0.72f, 0.9f, 0.76f), label = Box(0.5f, 0.705f, 0.7f, 0.715f), relabel = true },
                new ArtField { field = 4, value = Box(0.5f, 0.78f, 0.7f, 0.82f) },
                new ArtField { field = 5, value = Box(0.7f, 0.78f, 0.9f, 0.82f) },
                new ArtField { field = 6, value = Box(0.75f, 0.85f, 0.85f, 0.9f) },
                new ArtField { field = 7, value = Box(0.1f, 0.6f, 0.4f, 0.8f) }
            },
            prints = new[]
            {
                new ArtPrint { kind = ArtPrintKind.NationCode, rect = Box(0.1f, 0.9f, 0.3f, 0.93f) },
                new ArtPrint { kind = ArtPrintKind.Signature, field = 0, rect = Box(0.5f, 0.85f, 0.7f, 0.9f) },
                new ArtPrint { kind = ArtPrintKind.Text, text = "DESK 3", rect = Box(0.1f, 0.95f, 0.3f, 0.98f) }
            },
            stamp = Box(0.1f, 0.1f, 0.9f, 0.4f),
            stampCaption = "ENTRY VISA",
            covers = new[] { Box(0f, 0f, 1f, 0.02f) },
            spine = Box(0.02f, 0.49f, 0.98f, 0.51f)
        };
        return spec;
    }

    private static PlacedForm OnArt(FormSpec spec, FormData data, float width = 100f) => FormLayout.Layout(spec, data, width, M, new FakeMeasure());

    [Test]
    public void Art_EachValuePrintsAndIsPickedAtItsPlace_ThePageTheArtsAspect()
    {
        PlacedForm form = OnArt(ArtPassport(), BookletData());

        Assert.AreEqual(200f, form.PageHeight, Eps, "a page 100 wide on an art 0.5 wide is 200 tall");
        FormSlot citizen = form.Slots.Single(s => s.Field == 1);
        Assert.AreEqual(50f, citizen.Hit.XMin, Eps);
        Assert.AreEqual(132f, citizen.Hit.YMin, Eps);
        Assert.AreEqual(70f, citizen.Hit.XMax, Eps);
        Assert.AreEqual(140f, citizen.Hit.YMax, Eps);
        FormItem value = form.Items.Single(i => i.Kind == FormItemKind.Text && i.Slot == citizen.Index && i.Role == FormTextRole.Value);
        Assert.AreEqual("KTR-418", value.Text);
        Assert.AreEqual(4f, value.Size, Eps, "half its place's height");
        Assert.AreEqual(1, FormLayout.SlotAt(form, 60f, 136f) == citizen.Index ? 1 : 0, "a click on the value picks its field");
        Assert.IsFalse(form.Items.Any(i => i.Kind == FormItemKind.Box || i.Kind == FormItemKind.Stripe), "the art draws the boxes and the frame");
    }

    [Test]
    public void Art_TheSlotsGoInReadingOrder()
    {
        PlacedForm form = OnArt(ArtPassport(), BookletData());
        CollectionAssert.AreEqual(new[] { 7, 0, 1, 2, 3, 4, 5, 6 }, form.Slots.Select(s => s.Field).ToArray(), "top to bottom, then left to right (the photo beside the name)");
    }

    [Test]
    public void Art_ARelabelledFieldPrintsItsOwnLabel_TheOthersKeepTheArts()
    {
        PlacedForm form = OnArt(ArtPassport(), BookletData());
        var labels = form.Items.Where(i => i.Kind == FormItemKind.Text && i.Role == FormTextRole.Label).ToList();
        Assert.AreEqual(1, labels.Count);
        Assert.AreEqual("DESTINATION", labels[0].Text);
        Assert.AreEqual(form.Slots.Single(s => s.Field == 3).Index, labels[0].Slot, "it goes with its field");
    }

    [Test]
    public void Art_AFieldNotIntroduced_IsPatchedFromTheBlankFace_AndNotPicked()
    {
        FormData data = BookletData();
        data.FieldHidden = new[] { false, false, true, false, false, false, false, false };
        PlacedForm form = OnArt(ArtPassport(), data);

        FormItem patch = form.Items.Single(i => i.Kind == FormItemKind.Patch);
        Assert.AreEqual(70f, patch.Rect.XMin, Eps, "over the birth date's baked label");
        Assert.AreEqual(129f, patch.Rect.YMin, Eps);
        FormSlot birth = form.Slots.Single(s => s.Field == 2);
        Assert.IsTrue(birth.Hidden);
        Assert.AreEqual(-1, FormLayout.SlotAt(form, 80f, 136f));
        Assert.IsFalse(form.Items.Any(i => i.Kind == FormItemKind.Text && i.Text == "3 May 2101"));
    }

    [Test]
    public void Art_ThePhotoFitsItsWindowAt4To5_TheSealSitsAtItsSpot()
    {
        PlacedForm form = OnArt(ArtPassport(), BookletData());
        FormItem photo = form.Items.Single(i => i.Kind == FormItemKind.Photo);
        Assert.AreEqual(LookCanvas.PhotoAspect, photo.Rect.Width / photo.Rect.Height, 1e-3f);
        Assert.AreEqual(25f, photo.Rect.CentreX, Eps, "centred in its window");
        Assert.AreEqual(140f, photo.Rect.CentreY, Eps);
        Assert.LessOrEqual(photo.Rect.Width, 30f + Eps);
        Assert.AreEqual(photo.Rect.XMin, form.Slots.Single(s => s.Field == 7).Hit.XMin, Eps, "the photo is picked where it shows");

        FormItem seal = form.Items.Single(i => i.Kind == FormItemKind.Seal);
        Assert.AreEqual("Blue hexagon · VO", seal.Text);
        Assert.AreEqual(75f, seal.Rect.XMin, Eps);
        Assert.AreEqual(170f, seal.Rect.YMin, Eps);
    }

    [Test]
    public void Art_ABookletHasItsCoverItsSpineAndItsVisaBox_WhereTheStampsGo()
    {
        PlacedForm form = OnArt(ArtPassport(), BookletData(cover: "#5A1F2B"));

        FormItem cover = form.Items.Single(i => i.Kind == FormItemKind.Cover);
        Assert.AreEqual("#5A1F2B", cover.Text, "in the holder's nation's colour");
        Assert.AreEqual(1, form.Items.Count(i => i.Kind == FormItemKind.Spine));
        FormItem visa = form.Items.Single(i => i.Kind == FormItemKind.StampArea);
        Assert.AreEqual(FormLayout.VisaBox, visa.Text);
        Assert.AreEqual(visa.Rect.XMin, StampSpots.Area(form).XMin, Eps, "the verdict's ink goes in the visa box");
        Assert.IsTrue(form.Items.Any(i => i.Kind == FormItemKind.Text && i.Text == "ENTRY VISA" && i.Role == FormTextRole.Caption));
        FormItem emblem = form.Items.Single(i => i.Kind == FormItemKind.Watermark);
        Assert.AreEqual("WingedSun", emblem.Text);
        Assert.IsTrue(emblem.Rect.XMin >= visa.Rect.XMin && emblem.Rect.YMax <= visa.Rect.YMax, "inside the visa box");
    }

    [Test]
    public void Art_ThePrintsThatAreNoField_RepeatTheShownValues_AndAreNeverPicked()
    {
        FormData data = BookletData();
        data.FieldValues = data.FieldValues.Select((v, i) => i == 0 ? "Omar Doctored" : v).ToArray();
        PlacedForm form = OnArt(ArtPassport(), data);

        FormItem hand = form.Items.Single(i => i.Kind == FormItemKind.Text && i.Role == FormTextRole.Hand);
        Assert.AreEqual("Omar Doctored", hand.Text, "the signature repeats the name the page shows");
        Assert.AreEqual(-1, hand.Slot);
        Assert.IsTrue(form.Items.Any(i => i.Kind == FormItemKind.Text && i.Text == "EGY" && i.Slot == -1));
        Assert.IsTrue(form.Items.Any(i => i.Kind == FormItemKind.Text && i.Text == "DESK 3" && i.Slot == -1));

        data.FieldHidden = new[] { true, false, false, false, false, false, false, false };
        Assert.IsFalse(OnArt(ArtPassport(), data).Items.Any(i => i.Role == FormTextRole.Hand), "a hidden field's copy is not printed either");
    }

    [Test]
    public void Art_ASignedFieldPrintsInAHand_UnsignedWhenBlank()
    {
        FormSpec spec = ArtPassport();
        spec.blocks = spec.blocks.Concat(new[] { new FormBlock { kind = FormBlockKind.Signature, field = 4 } }).ToArray();
        FormData data = BookletData();
        Assert.AreEqual(FormTextRole.Hand, OnArt(spec, data).Items.Single(i => i.Kind == FormItemKind.Text && i.Text == "Standard").Role);

        data.FieldValues = data.FieldValues.Select((v, i) => i == 4 ? "" : v).ToArray();
        Assert.IsTrue(OnArt(spec, data).Items.Any(i => i.Kind == FormItemKind.Text && i.Text == FormLayout.Unsigned));
    }

    [Test]
    public void Art_APhotoWindowOnAPaperWithoutAPhotoField_ShowsTheHolder_NeverPicked()
    {
        FormSpec spec = ArtPassport();
        spec.blocks = spec.blocks.Select(b => b.kind == FormBlockKind.FieldRow ? new FormBlock { kind = b.kind, cells = b.cells.Where(c => !c.IsPhoto).ToArray() } : b).ToArray();
        spec.look.art.fields = spec.look.art.fields.Where(f => f.field != 7).ToArray();
        spec.look.art.photo = Box(0.1f, 0.6f, 0.4f, 0.8f);
        FormData data = BookletData();
        data.HasPhoto = false;
        PlacedForm form = OnArt(spec, data);
        FormItem photo = form.Items.Single(i => i.Kind == FormItemKind.Photo);
        Assert.AreEqual(-1, photo.Slot);
        Assert.AreEqual(LookCanvas.PhotoAspect, photo.Rect.Width / photo.Rect.Height, 1e-3f);
        Assert.IsTrue(ArtLayout.ShowsPhoto(spec, data));
        Assert.IsFalse(ArtLayout.ShowsPhoto(Booklet(), data), "a paper off its art with no photo shows none");
    }

    [Test]
    public void Art_TheCheck_FindsAFieldWithoutAPlace_APlaceOffTheFace_AndAValueTooLong()
    {
        FormData probe = BookletData();
        Assert.IsEmpty(FormLayout.Check(ArtPassport(), probe, M, new FakeMeasure()), "the art passport fits");

        FormSpec missing = ArtPassport();
        missing.look.art.fields = missing.look.art.fields.Where(f => f.field != 5).ToArray();
        StringAssert.Contains("field 5 (Valid Until) has no place on the art", string.Join("\n", FormLayout.Check(missing, probe, M, new FakeMeasure())));

        FormSpec off = ArtPassport();
        off.look.art.fields[1].value = Box(0.5f, 0.66f, 1.2f, 0.7f);
        StringAssert.Contains("off the face", string.Join("\n", FormLayout.Check(off, probe, M, new FakeMeasure())));

        FormData longer = BookletData();
        longer.FieldValues = longer.FieldValues.Select((v, i) => i == 1 ? new string('W', 60) + " " + new string('W', 60) + " " + new string('W', 60) : v).ToArray();
        StringAssert.Contains("does not fit its place", string.Join("\n", FormLayout.Check(ArtPassport(), longer, M, new FakeMeasure())));
    }
}
