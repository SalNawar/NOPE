using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>The fault reasons (traveller types §5.2): one reason per traveller, and each reason's citation line in the UI strings.</summary>
public class FaultsTests
{
    [TestCase(DirectiveFault.None, "")]
    [TestCase(DirectiveFault.ClosedDestination, "closed")]
    public void Reason_OfADirectiveFault(DirectiveFault fault, string expected)
    {
        Assert.AreEqual(expected, Faults.Reason(fault));
    }

    [Test]
    public void Reason_TheDirectiveFirst_ThenTheCostume_TheRecordLie_ThePlaceLie_ElseNone()
    {
        Assert.AreEqual("closed", Faults.Reason(DirectiveFault.ClosedDestination, CostumeError.OtherPlace, true, true), "a directive fault outranks the rest (never generated together, K5)");
        Assert.AreEqual("panic", Faults.Reason(DirectiveFault.None, CostumeError.PresentClothes, false, false));
        Assert.AreEqual("forged", Faults.Reason(DirectiveFault.None, CostumeError.None, true, false));
        Assert.AreEqual("disguised", Faults.Reason(DirectiveFault.None, CostumeError.None, false, true));
        Assert.AreEqual(string.Empty, Faults.Reason(DirectiveFault.None, CostumeError.None, false, false));
        Assert.AreEqual(CostumeErrors.FaultReason, Faults.Reason(DirectiveFault.None, CostumeError.PresentAccessory, true, true));
    }

    /// <summary>The English UI string of <paramref name="key"/> in world_source.json (null when missing).</summary>
    private static string UiString(string key, [CallerFilePath] string here = "")
    {
        const string source = "Assets/Data/World/world_source.json";
        string path = File.Exists(source) ? source : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", source);
        ContentNode strings = ContentJson.Parse(File.ReadAllText(path)).Get("ui").Get("strings");
        ContentNode entry = strings.Items.FirstOrDefault(s => s.Get("key").Text == key);
        return entry?.Get("text").Text;
    }

    [TestCase("forged", "forged papers")]
    [TestCase("disguised", "disguised traveller")]
    [TestCase("closed", "closed destination")]
    [TestCase("panic", "would cause a panic in {0}")]
    public void EveryReason_HasItsCitationLine(string reason, string says)
    {
        string key = new CaseVerdict { accepted = true, faultReason = reason }.MistakeKey;
        Assert.AreEqual("citation.acceptedWrong." + reason, key);
        StringAssert.Contains(says, UiString(key));
    }
}
