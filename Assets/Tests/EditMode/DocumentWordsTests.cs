using NUnit.Framework;

/// <summary>A document word's UI string key (DocumentWords.Key): case and punctuation folded, long texts cut, nothing for a text with no ASCII letter or digit.</summary>
public class DocumentWordsTests
{
    [Test]
    public void Key_FoldsCaseAndPunctuation()
    {
        Assert.AreEqual("doc.citizen_id", DocumentWords.Key("CITIZEN ID"));
        Assert.AreEqual("doc.citizen_id", DocumentWords.Key("Citizen ID"));
        Assert.AreEqual("doc.destination_era", DocumentWords.Key("DESTINATION (ERA)"));
        Assert.AreEqual("doc.1_signatory_and_terms", DocumentWords.Key("1  SIGNATORY AND TERMS"));
        Assert.AreEqual("doc.entry_visa_visas_d_entr_e", DocumentWords.Key("ENTRY VISA · VISAS D'ENTRÉE"), "a non-ASCII letter parts the key like punctuation");
    }

    [Test]
    public void Capitals_DropGreekAccents_AndKnowCapitals()
    {
        Assert.AreEqual("ΥΠΗΡΕΣΙΑ ΧΡΟΝΙΚΟΥ ΤΕΛΩΝΕΙΟΥ", ArtLayout.Capitals("Υπηρεσία Χρονικού Τελωνείου"));
        Assert.AreEqual("ΠΡΩΤΕΪΝΗ", ArtLayout.Capitals("πρωτεΐνη"), "the diaeresis stays");
        Assert.AreEqual("BÜRGER-ID", ArtLayout.Capitals("Bürger-ID"));
        Assert.AreEqual("入境签证", ArtLayout.Capitals("入境签证"));
        Assert.AreEqual("ΔΕΧΟΜΑΙ\n<size=60%><noparse>Accept</noparse></size>".Replace("Accept", "ACCEPT"),
                        ArtLayout.Capitals("Δέχομαι\n<size=60%><noparse>Accept</noparse></size>"), "a tag is kept as it is");
        Assert.IsTrue(ArtLayout.IsCapitals("CITIZEN ID"));
        Assert.IsFalse(ArtLayout.IsCapitals("Citizen ID"));
        Assert.IsFalse(ArtLayout.IsCapitals("· 3"));
    }

    [Test]
    public void Key_IsNullWithoutLettersOrDigits_AndCutWhenLong()
    {
        Assert.IsNull(DocumentWords.Key(" · "));
        Assert.IsNull(DocumentWords.Key(null));
        Assert.IsNull(DocumentWords.Key("الإعدادات"));
        string key = DocumentWords.Key(new string('a', 200));
        Assert.AreEqual(DocumentWords.Prefix.Length + DocumentWords.MaxBody, key.Length);
    }
}
