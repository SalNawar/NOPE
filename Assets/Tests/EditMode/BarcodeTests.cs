using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The printed barcode (PC spec FO5): drawn by code from the serial, guard
/// bars at both ends, exactly the given width in modules, deterministic and
/// different for different serials. Decorative: nothing decodes it.
/// </summary>
public class BarcodeTests
{
    private const int Modules = 60;

    private static void AssertWellFormed(IReadOnlyList<(int start, int width)> bars, int modules)
    {
        Assert.Greater(bars.Count, 4);
        int end = -1;
        foreach ((int start, int width) in bars)
        {
            Assert.GreaterOrEqual(width, 1);
            Assert.Greater(start, end, "bars never touch or overlap: a space between each");
            end = start + width;
        }
        Assert.AreEqual(0, bars[0].start, "the left guard starts the code");
        Assert.AreEqual(modules, bars[bars.Count - 1].start + bars[bars.Count - 1].width, "the right guard ends it at the full width");
    }

    [Test]
    public void Bars_HaveGuardsAtBothEnds_AndFillExactlyTheModules()
    {
        foreach (string serial in new[] { "TC-610/583021", "TC-010/000001", "x", string.Empty })
        {
            IReadOnlyList<(int start, int width)> bars = Barcode.Bars(serial, Modules);
            AssertWellFormed(bars, Modules);
            CollectionAssert.AreEqual(new[] { (0, 1), (2, 1) }, bars.Take(2).ToArray(), "left guard: bar, space, bar");
            CollectionAssert.AreEqual(new[] { (Modules - 3, 1), (Modules - 1, 1) }, bars.Skip(bars.Count - 2).ToArray(), "right guard: bar, space, bar");
        }
    }

    [Test]
    public void Bars_AreDeterministic_AndDifferBySerial()
    {
        CollectionAssert.AreEqual(Barcode.Bars("TC-610/583021", Modules).ToArray(), Barcode.Bars("TC-610/583021", Modules).ToArray());
        CollectionAssert.AreNotEqual(Barcode.Bars("TC-610/583021", Modules).ToArray(), Barcode.Bars("TC-610/583022", Modules).ToArray());
        CollectionAssert.AreNotEqual(Barcode.Bars("TC-610/583021", Modules).ToArray(), Barcode.Bars("TC-620/583021", Modules).ToArray());
    }

    [TestCase(20)]
    [TestCase(61)]
    [TestCase(120)]
    public void Bars_NeverRunWiderThanTheModules(int modules)
    {
        AssertWellFormed(Barcode.Bars("TC-230/771201", modules), modules);
    }
}
