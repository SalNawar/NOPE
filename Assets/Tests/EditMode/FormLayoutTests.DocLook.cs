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
        Assert.AreEqual(2, problems.Count, string.Join("; ", problems));
        StringAssert.Contains("field 0's value word", problems[0], "the paper would cut the name: a false discrepancy");
        StringAssert.Contains("field 0's Signature print word", problems[1], "and the signature that reprints it");
        Assert.IsEmpty(ArtLayout.CutWords(Booklet(), words, new FakeMeasure()), "a form off the art prints no cut words");
    }

    /// <summary>TC-101's name field and the Signature print that reprints it, placed as DocTemplate_TC101.asset places them (aspect 0.65435, valueShare 0.56, labelShare 0.6, the clean face printing its labels, grown x1.3 and x1.5; Booklet's blocks): the print is the tightest place a name prints, 0.232 of the page wide against the Name field's 0.370 (the re-verify of 5f80371, point 3).</summary>
    private static FormSpec Tc101Name()
    {
        FormSpec spec = Booklet();
        spec.look.aspect = 0.65435f;
        spec.look.art = new FormArt
        {
            valueShare = 0.56f,
            labelShare = 0.6f,
            clean = true,
            printLabels = true,
            labelGrowth = 1.3f,
            valueGrowth = 1.5f,
            fields = new[] { new ArtField { field = 0, value = Box(0.5f, 0.63245f, 0.87016f, 0.66095f), label = Box(0.49597f, 0.61847f, 0.675f, 0.63113f), labelText = "SURNAME / GIVEN NAMES" } },
            prints = new[] { new ArtPrint { kind = ArtPrintKind.Signature, field = 0, rect = Box(0.5f, 0.84828f, 0.73226f, 0.90264f) } }
        };
        return spec;
    }

    /// <summary>FakeMeasure's sizes, and the role each word was measured in (one entry per measure of a word in a place: CutWords measures it twice there, wrapped and not).</summary>
    private sealed class RoleMeasure : ITextMeasure
    {
        private readonly FakeMeasure _sizes = new FakeMeasure();
        private readonly System.Collections.Generic.List<(string Text, FormTextRole Role, float Width)> _calls = new System.Collections.Generic.List<(string Text, FormTextRole Role, float Width)>();

        public float Height(string text, FormTextRole role, float size, float width)
        {
            _calls.Add((text, role, width));
            return _sizes.Height(text, role, size, width);
        }

        /// <summary>The roles <paramref name="text"/> was measured in, one per place (its wrapped measure, the one at the place's width).</summary>
        public System.Collections.Generic.List<FormTextRole> Of(string text) =>
            _calls.Where(c => c.Text == text && c.Width < 1e5f).Select(c => c.Role).ToList();
    }

    [Test]
    public void CutWords_ANameReprintedAsASignatureOrACopy_IsMeasuredInThePrintsOwnPlace()
    {
        FormSpec spec = Tc101Name();
        var words = new System.Collections.Generic.List<System.Collections.Generic.IReadOnlyList<string>> { new[] { "GIOVANFRANCESCO" } };
        Assert.IsEmpty(ArtLayout.CutWords(spec, words, new FakeMeasure()), "the longest drawn name fits TC-101's signature at its least size");

        // 30 letters fit the Name field's place at its least size (65 to a line here) but not the signature's (22): only the print cuts it
        words[0] = new[] { "GIOVANFRANCESCO", new string('W', 30) };
        var problems = ArtLayout.CutWords(spec, words, new FakeMeasure());
        Assert.AreEqual(1, problems.Count, string.Join("; ", problems));
        StringAssert.Contains("field 0's Signature print word '" + new string('W', 30) + "'", problems[0], "the paper would cut the signed name in the hand's place");

        // in the print's own style: the signature in a hand (TextMeshPro sets it in italics, wider than a value), the name's field and a copy as a value
        var roles = new RoleMeasure();
        ArtLayout.CutWords(spec, words, roles);
        CollectionAssert.AreEquivalent(new[] { FormTextRole.Value, FormTextRole.Hand }, roles.Of(new string('W', 30)), "the name's place as a value, the signature's as a hand");

        spec.look.art.prints[0].kind = ArtPrintKind.Copy;
        problems = ArtLayout.CutWords(spec, words, new FakeMeasure());
        Assert.AreEqual(1, problems.Count, string.Join("; ", problems));
        StringAssert.Contains("field 0's Copy print word", problems[0], "a copy of the name is measured in its own place too");
        roles = new RoleMeasure();
        ArtLayout.CutWords(spec, words, roles);
        CollectionAssert.AreEquivalent(new[] { FormTextRole.Value, FormTextRole.Value }, roles.Of(new string('W', 30)), "a copy prints as a value");

        words[0] = null;
        Assert.IsEmpty(ArtLayout.CutWords(spec, words, new FakeMeasure()), "a print of a field with none of the content's words (TC-230's date stub) is not measured here");
    }

    [Test]
    public void FieldWords_NameAndSignatureFieldsMeasureThePlacesAndThePremadesNames_ADestinationTheOrigins()
    {
        // the validator's word sources (ContentLibraryValidator.Forms.ContentWords; the re-verify of ce69276, point 2)
        var fields = new[]
        {
            new DocumentFieldSpec { category = ClueCategory.Name }, new DocumentFieldSpec { category = ClueCategory.Signature },
            new DocumentFieldSpec { category = ClueCategory.Destination }, new DocumentFieldSpec { category = ClueCategory.CitizenId }, null
        };
        var words = FieldWords.Of(fields, new[] { "Omar", "Konstantinos", "  " }, new[] { "Giovanfrancesco Rossi" }, new[] { "Middle Egypt", "Athens" },
                                  texts => ArtLayout.LongestWords(texts, 2));

        Assert.AreEqual(5, words.Count, "one list a field, in the fields' order");
        CollectionAssert.AreEqual(new[] { "GIOVANFRANCESCO", "KONSTANTINOS" }, words[0], "a premade's own name is measured with the places' given names, word by word, in capitals, the longest first");
        Assert.IsNotNull(words[1], "a holder signs with the given name: a Signature field measures the names");
        CollectionAssert.AreEqual(words[0], words[1], "the same names as a Name field");
        CollectionAssert.AreEqual(new[] { "MIDDLE", "ATHENS" }, words[2], "a Destination field the places' origins (the first of two words as long kept first)");
        Assert.IsNull(words[3], "an ID is the probe's (ArtLayout.Problems)");
        Assert.IsNull(words[4]);

        // through CutWords: a premade's word too wide for the passport's Name place is the paper's cut, on its name field
        var cut = FieldWords.Of(fields, new[] { "Omar" }, new[] { new string('W', 90) }, new[] { "Athens" }, texts => ArtLayout.LongestWords(texts, 2));
        StringAssert.Contains("field 0", string.Join("; ", ArtLayout.CutWords(GrownPassport(1.3f, 1.5f), cut, new FakeMeasure())));
    }

    [Test]
    public void Problems_AValueWordWiderThanItsPlaceAtTheLeastSize_IsAProblem_EvenWhenItsLinesFit()
    {
        FormData single = BookletData();
        single.FieldValues = single.FieldValues.Select((v, i) => i == 1 ? new string('W', 30) : v).ToArray();
        string problems = string.Join("\n", FormLayout.Check(ArtPassport(), single, M, new FakeMeasure()));
        StringAssert.Contains("field 1 (Citizen ID)'s longest value has a word", problems, "a single-token value (an ID) the paper would cut with a full stop");
        StringAssert.DoesNotContain("does not fit its place", problems, "its two broken lines fit the place's height: only the word check sees it");

        FormData words = BookletData();
        words.FieldValues = words.FieldValues.Select((v, i) => i == 1 ? new string('W', 10) + " " + new string('W', 10) : v).ToArray();
        Assert.IsEmpty(FormLayout.Check(ArtPassport(), words, M, new FakeMeasure()), "as long in words that each fit, it wraps whole");
    }

    private static float Grow(float size, float growth, float room) => System.Math.Max(size, System.Math.Min(size * growth, room));
}
