using System.Linq;
using NUnit.Framework;

/// <summary>
/// DOC-LOOK's re-layout of the documents drawn on their art (BUG-9; Saleh's
/// big mat: papers show at about 67%, so their text grows): the paper prints
/// every label over its clean face (FormArt.printLabels), a label stacked over
/// its value takes a band at its cell's top and the value the rest, each
/// larger (labelGrowth, valueGrowth), a label beside its value grows in its
/// own place, and a grown text may shrink to the same absolute size as before
/// and no less (ArtLayout.FitFloorOf).
/// </summary>
public partial class FormLayoutTests
{
    /// <summary>The art passport with its clean face printing its labels, grown by <paramref name="labels"/> and <paramref name="values"/>.</summary>
    private static FormSpec GrownPassport(float labels, float values)
    {
        FormSpec spec = ArtPassport();
        spec.look.art.clean = true;
        spec.look.art.printLabels = true;
        spec.look.art.labelGrowth = labels;
        spec.look.art.valueGrowth = values;
        return spec;
    }

    [Test]
    public void PrintLabels_TheCleanFacePrintsEveryLabel_InEnglish_AndPatchesNothing()
    {
        FormSpec spec = GrownPassport(1f, 1f);
        FormData data = BookletData();
        data.FieldHidden = new[] { false, false, true, false, false, false, false, false };
        PlacedForm form = OnArt(spec, data);

        Assert.IsTrue(spec.look.art.Worded(data), "the clean face shows in English too");
        Assert.IsFalse(data.Translated);
        var labels = form.Items.Where(i => i.Kind == FormItemKind.Text && i.Role == FormTextRole.Label).ToList();
        Assert.AreEqual(3, labels.Count, "fields 0, 1 and 3 print their labels; field 2 is not introduced yet");
        Assert.IsFalse(form.Items.Any(i => i.Kind == FormItemKind.Patch), "a clean face has no baked label to patch");
    }

    [Test]
    public void Growth_AStackedCell_ItsLabelABandAtTheTop_ItsValueLargerBelow()
    {
        PlacedForm form = OnArt(GrownPassport(1.3f, 1.5f), BookletData());
        FormSlot citizen = form.Slots.Single(s => s.Field == 1);
        FormItem value = form.Items.Single(i => i.Kind == FormItemKind.Text && i.Slot == citizen.Index && i.Role == FormTextRole.Value);
        FormItem label = form.Items.Single(i => i.Kind == FormItemKind.Text && i.Slot == citizen.Index && i.Role == FormTextRole.Label);

        Assert.AreEqual(6f, value.Size, Eps, "1.5 times the art's 4");
        Assert.AreEqual(0.6f * 2f * 1.3f, label.Size, Eps, "1.3 times the art's label");
        Assert.AreEqual(129f, label.Rect.YMin, Eps, "the label starts where the art's did");
        Assert.AreEqual(label.Rect.YMax, value.Rect.YMin, Eps, "the value right under the label's band");
        Assert.AreEqual(label.Size * ArtLayout.LabelLine, label.Rect.Height, Eps);
        Assert.AreEqual(140f, value.Rect.YMax, Eps, "the value keeps the art's foot");
        Assert.LessOrEqual(value.Size, ArtLayout.ValueFill * value.Rect.Height + Eps, "one line of the value fits its place");
        Assert.AreEqual(value.Rect.YMin, citizen.Hit.YMin, Eps, "the field is picked where its value prints");
    }

    [Test]
    public void Growth_One_LeavesEverySizeAsTheArtSetsIt()
    {
        PlacedForm plain = OnArt(GrownPassport(1f, 1f), BookletData());
        FormItem value = plain.Items.Single(i => i.Kind == FormItemKind.Text && i.Slot == plain.Slots.Single(s => s.Field == 1).Index && i.Role == FormTextRole.Value);
        Assert.AreEqual(4f, value.Size, Eps);
        Assert.IsFalse(GrownPassport(1f, 1f).look.art.Regrown);
    }

    [Test]
    public void Growth_NeverPastALineOfThePlace()
    {
        PlacedForm form = OnArt(GrownPassport(1.3f, 5f), BookletData());
        foreach (FormItem v in form.Items.Where(i => i.Kind == FormItemKind.Text && (i.Role == FormTextRole.Value || i.Role == FormTextRole.Hand)))
            Assert.LessOrEqual(v.Size, ArtLayout.ValueFill * v.Rect.Height + Eps, v.Text);
    }

    [Test]
    public void Growth_ALabelBesideItsValue_GrowsInItsOwnPlace()
    {
        FormSpec spec = GrownPassport(1.3f, 1.5f);
        // field 4 gets a label to its left, on the value's row
        spec.look.art.fields[4].label = Box(0.3f, 0.78f, 0.48f, 0.82f);
        spec.look.art.fields[4].labelText = "CLASS";
        PlacedForm form = OnArt(spec, BookletData());
        FormSlot slot = form.Slots.Single(s => s.Field == 4);
        FormItem label = form.Items.Single(i => i.Kind == FormItemKind.Text && i.Slot == slot.Index && i.Role == FormTextRole.Label);
        Assert.IsFalse(ArtLayout.Stacked(spec.look.art.fields[4]));
        Assert.AreEqual(156f, label.Rect.YMin, Eps, "the art's place");
        Assert.AreEqual(Grow(0.6f * 8f, 1.3f, 8f / ArtLayout.LabelLine), label.Size, Eps);
        Assert.AreEqual(156f, slot.Hit.YMin, Eps, "the value keeps its own place");
    }

    [Test]
    public void FitFloorOf_AGrownText_ShrinksToTheSameAbsoluteSizeAsBefore()
    {
        FormArt art = GrownPassport(1.3f, 1.5f).look.art;
        Assert.AreEqual(TextFit.Least / 1.5f, ArtLayout.FitFloorOf(art, FormTextRole.Value), Eps);
        Assert.AreEqual(TextFit.Least / 1.3f, ArtLayout.FitFloorOf(art, FormTextRole.Label), Eps);
        Assert.AreEqual(TextFit.Least, ArtLayout.FitFloorOf(new FormArt(), FormTextRole.Value), Eps);
        Assert.AreEqual(TextFit.Least, ArtLayout.FitFloor, Eps);
    }

    [Test]
    public void CapHeight_Smallest_TheFormsSmallestData_AndLabels()
    {
        PlacedForm form = OnArt(GrownPassport(1.3f, 1.5f), BookletData());
        float data = form.Items.Where(i => i.Kind == FormItemKind.Text && i.Slot >= 0 && (i.Role == FormTextRole.Value || i.Role == FormTextRole.Hand)).Min(i => i.Size);
        Assert.AreEqual(data, CapHeight.Smallest(form, false), Eps);
        Assert.AreEqual(form.Items.Where(i => i.Kind == FormItemKind.Text && i.Role == FormTextRole.Label).Min(i => i.Size), CapHeight.Smallest(form, true), Eps);
        Assert.AreEqual(0f, CapHeight.Smallest(null, false));
    }

    [Test]
    public void CutWords_AContentWordWiderThanItsPlaceAtTheLeastSize_IsAProblem_AShortOneIsNot()
    {
        FormSpec spec = GrownPassport(1.3f, 1.5f);
        var words = new System.Collections.Generic.List<System.Collections.Generic.IReadOnlyList<string>> { new[] { "OMAR", "KONSTANTINOS" }, null, null, new[] { "ATHENS" } };
        Assert.IsEmpty(ArtLayout.CutWords(spec, words, new FakeMeasure()), "every name and origin word fits its place");

        words[0] = new[] { "OMAR", new string('W', 90) };
        var problems = ArtLayout.CutWords(spec, words, new FakeMeasure());
        Assert.AreEqual(1, problems.Count, string.Join("; ", problems));
        StringAssert.Contains("field 0", problems[0], "the paper would cut the name: a false discrepancy");
        Assert.IsEmpty(ArtLayout.CutWords(Booklet(), words, new FakeMeasure()), "a form off the art prints no cut words");
    }

    private static float Grow(float size, float growth, float room) => System.Math.Max(size, System.Math.Min(size * growth, room));
}
