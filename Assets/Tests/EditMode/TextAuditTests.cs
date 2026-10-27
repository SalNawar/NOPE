using System.Collections.Generic;
using NUnit.Framework;

/// <summary>TEXT-LANG's text scans (BUG-3a takeover grep, BUG-38a Greek capitals).</summary>
public class TextAuditTests
{
    [Test]
    public void AccentedGreekCapitals_FindsTonosAndDialytika()
    {
        CollectionAssert.AreEqual(new[] { 'Ά', 'Ϊ' }, TextAudit.AccentedGreekCapitals("ΆΛΛΗ ΣΕΛΙΔΑ ΠΡΟΪΟΝ"));
        CollectionAssert.IsEmpty(TextAudit.AccentedGreekCapitals(ArtLayout.Capitals("Άλλη σελίδα")), "ArtLayout.Capitals drops the accents");
        CollectionAssert.IsEmpty(TextAudit.AccentedGreekCapitals("άλλη σελίδα"), "small letters keep theirs");
        CollectionAssert.IsEmpty(TextAudit.AccentedGreekCapitals(null));
    }

    [Test]
    public void CultureOnPlayerSurfaces_FlagsACultureLabelOnTheChromeOrThePc_NeverOnAPaper()
    {
        var labels = new List<(string, string)> { ("ΕΓΚΡΙΣΗ", "APPROVED"), ("OK", "OK"), ("Νέα", "News") };
        var shown = new List<(string, TextSurface)>
        {
            ("Shift report: ΕΓΚΡΙΣΗ 3", TextSurface.Desk),
            ("Mail: ΕΓΚΡΙΣΗ", TextSurface.Pc),
            ("ΕΓΚΡΙΣΗ", TextSurface.Paper),
            ("ΕΓΚΡΙΣΗ", TextSurface.Sign),
            ("OK", TextSurface.Desk),
            ("News", TextSurface.Desk),
            ("Νέα σήμερα", TextSurface.Desk)
        };
        List<string> found = TextAudit.CultureOnPlayerSurfaces(shown, labels);
        Assert.AreEqual(3, found.Count, string.Join(" | ", found));
        StringAssert.StartsWith("Desk:", found[0]);
        StringAssert.StartsWith("Pc:", found[1]);
        StringAssert.Contains("Νέα", found[2], "a short label (3 letters) still counts");
    }

    [Test]
    public void CultureOnPlayerSurfaces_IgnoresSharedAndTooShortLabels()
    {
        var labels = new List<(string, string)> { ("OK", "OK"), ("Ja", "Yes"), ("Name", "Name") };
        CollectionAssert.IsEmpty(TextAudit.CultureOnPlayerSurfaces(new[] { ("OK Ja Name", TextSurface.Desk) }, labels));
        CollectionAssert.IsEmpty(TextAudit.CultureOnPlayerSurfaces(null, null));
    }
}
