using NUnit.Framework;

/// <summary>The one clipboard (the PC redesign CP1-CP3): copy and replace, whether a paste is the current clip, a foreign clip's tongue, and a clip as a Notes clipping.</summary>
public class AppClipboardTests
{
    private static Clip Visa() => Clip.Plain("Premium", "field:0:4", "Visa · Visa Class", "Aster Vale");

    [Test]
    public void Empty_HasNoClip_AndNothingIsCurrent()
    {
        var clipboard = new AppClipboard();
        Assert.IsNull(clipboard.Current);
        Assert.IsFalse(clipboard.IsCurrent("Premium"));
        Assert.IsFalse(clipboard.IsCurrent(null));
    }

    [Test]
    public void Copy_ThenAnotherCopy_Replaces()
    {
        var clipboard = new AppClipboard();
        clipboard.Copy(Visa());
        Assert.AreEqual("Premium", clipboard.Current.Text);
        clipboard.Copy(Clip.Plain("Drachma", "book:Currency:greece:ancient", "Currency Ledger · Classical Athens", string.Empty));
        Assert.AreEqual("Drachma", clipboard.Current.Text);
        Assert.AreEqual("book:Currency:greece:ancient", clipboard.Current.SourceKey);
    }

    [Test]
    public void ABlankClip_IsNotCopied()
    {
        var clipboard = new AppClipboard();
        clipboard.Copy(Visa());
        clipboard.Copy(Clip.Plain("  ", "field:0:5", "Visa · Remarks", string.Empty));
        clipboard.Copy(null);
        Assert.AreEqual("Premium", clipboard.Current.Text);
    }

    [Test]
    public void IsCurrent_OnlyForTheClipsOwnText()
    {
        var clipboard = new AppClipboard();
        clipboard.Copy(Visa());
        Assert.IsTrue(clipboard.IsCurrent("Premium"));
        Assert.IsFalse(clipboard.IsCurrent("premium"));
        Assert.IsFalse(clipboard.IsCurrent("Premium class"));
    }

    [Test]
    public void AForeignClip_KeepsItsTongueAndCanonical()
    {
        Clip line = Clip.Untranslated("ΦΔΨ home ΞΠ Periclean Athens", "line:6", "Transcript · line 7", "Nikias", "greek", "Greek", "I go home to Periclean Athens");
        Assert.IsTrue(line.Foreign);
        Assert.AreEqual("greek", line.TongueId);
        Assert.AreEqual("Greek", line.TongueName);
        Assert.AreEqual("I go home to Periclean Athens", line.Canonical);
        Assert.AreEqual("ΦΔΨ home ΞΠ Periclean Athens", line.Text);

        var clipboard = new AppClipboard();
        clipboard.Copy(line);
        Assert.IsTrue(clipboard.IsCurrent("ΦΔΨ home ΞΠ Periclean Athens"));
        Assert.IsFalse(clipboard.IsCurrent("I go home to Periclean Athens"));
    }

    [Test]
    public void APlainClip_HasNoTongue()
    {
        Clip visa = Visa();
        Assert.IsFalse(visa.Foreign);
        Assert.IsNull(visa.TongueId);
        Assert.IsNull(visa.Canonical);
    }

    [Test]
    public void AsAClipping_CarriesItsSource_NeverTheCanonical()
    {
        Clipping visa = Visa().ToClipping();
        Assert.AreEqual("Premium", visa.text);
        Assert.AreEqual("Visa · Visa Class", visa.label);
        Assert.AreEqual("field:0:4", visa.sourceKey);
        Assert.AreEqual("Aster Vale", visa.traveller);
        Assert.IsFalse(visa.foreign);

        Clipping line = Clip.Untranslated("ΦΔΨ", "line:6", "Transcript · line 7", "Nikias", "greek", "Greek", "Home").ToClipping();
        Assert.AreEqual("ΦΔΨ", line.text);
        Assert.IsTrue(line.foreign);
    }

    [Test]
    public void NullSources_ReadAsEmpty()
    {
        Clip clip = Clip.Plain("x", null, null, null);
        Assert.AreEqual(string.Empty, clip.SourceKey);
        Assert.AreEqual(string.Empty, clip.SourceLabel);
        Assert.AreEqual(string.Empty, clip.Traveller);
    }
}
