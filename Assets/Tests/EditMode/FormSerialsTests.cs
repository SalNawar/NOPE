using System.Linq;
using NUnit.Framework;

/// <summary>
/// A paper's serial (PC spec FO5): "{form number}/{6 digits}", from the
/// traveller's forms seed (Seeds.ForForms, a value, not a stream draw) and the
/// document's index, so it is the same on every run and never moves another
/// stream's draws.
/// </summary>
public class FormSerialsTests
{
    [Test]
    public void Serial_IsTheFormNumber_ASlash_AndSixDigits()
    {
        string serial = FormSerials.Make("TC-610", 12345, 0);
        StringAssert.IsMatch(@"^TC-610/\d{6}$", serial);
    }

    [Test]
    public void Serial_WithoutAFormNumber_IsTheDigitsAlone()
    {
        StringAssert.IsMatch(@"^\d{6}$", FormSerials.Make(string.Empty, 12345, 0));
        StringAssert.IsMatch(@"^\d{6}$", FormSerials.Make(null, 12345, 0));
    }

    [Test]
    public void Serial_IsDeterministic_AndDiffersByDocumentAndTraveller()
    {
        Assert.AreEqual(FormSerials.Make("TC-610", 777, 1), FormSerials.Make("TC-610", 777, 1));
        var byIndex = Enumerable.Range(0, 4).Select(i => FormSerials.Make("TC-610", 777, i).Substring(7)).ToList();
        CollectionAssert.AllItemsAreUnique(byIndex, "each paper of a traveller has its own digits");
        Assert.AreNotEqual(FormSerials.Make("TC-610", 777, 0), FormSerials.Make("TC-610", 778, 0), "each traveller their own");
    }

    [Test]
    public void Serial_DigitsArePinned()
    {
        // Pinned so a change to the maths shows here (the golden cases dump prints every serial).
        Assert.AreEqual($"TC-610/{(uint)Seeds.Mix(12345, 1) % 1000000:D6}", FormSerials.Make("TC-610", 12345, 0));
    }
}
