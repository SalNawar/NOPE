using NUnit.Framework;

/// <summary>
/// Decision table for the Chrono Converter's pure rules (CalendarConverter,
/// TimeDesk.Domain). Era-native months parse against the era's table, BCE
/// epochs convert to the negative-modern convention, span checks are
/// inclusive, and Gregorian dates still work for the imperial eras.
/// </summary>
public class CalendarConverterTests
{
    private static readonly string[] AtticMonths =
        { "Hekatombaion", "Metageitnion", "Boedromion", "Pyanepsion", "Maimakterion", "Poseideon",
          "Gamelion", "Anthesterion", "Elaphebolion", "Munychion", "Thargelion", "Skirophorion" };

    private static readonly string[] EgyptianMonths =
        { "Thoth", "Phaophi", "Athyr", "Choiak", "Tybi", "Mechir",
          "Phamenoth", "Pharmuthi", "Pachons", "Payni", "Epiphi", "Mesore" };

    [Test]
    public void Greek_NativeMonth_ParsesAndMarksNative()
    {
        Assert.IsTrue(CalendarConverter.TryParse("21 Elaphebolion 437", AtticMonths, out CalendarConverter.ParsedDate p));
        Assert.AreEqual(21, p.day);
        Assert.AreEqual(8, p.monthIndex); // Elaphebolion is the 9th month
        Assert.AreEqual(437, p.year);
        Assert.IsTrue(p.nativeMonth);
    }

    [Test]
    public void Egyptian_NativeMonth_Parses()
    {
        Assert.IsTrue(CalendarConverter.TryParse("9 Choiak 1204", EgyptianMonths, out CalendarConverter.ParsedDate p));
        Assert.AreEqual(3, p.monthIndex); // Choiak is the 4th month (0-based 3)
        Assert.AreEqual(1204, p.year);
        Assert.IsTrue(p.nativeMonth);
    }

    [Test]
    public void Gregorian_Abbrev_Parses_AsNonNative_InAtticEra()
    {
        Assert.IsTrue(CalendarConverter.TryParse("3 Mar 1908", AtticMonths, out CalendarConverter.ParsedDate p));
        Assert.AreEqual(2, p.monthIndex);
        Assert.IsFalse(p.nativeMonth, "Mar is Gregorian even when the era table is Attic");
    }

    [Test]
    public void BCE_Era_ConvertsYear_Negative()
    {
        Assert.AreEqual(-437, CalendarConverter.ToModernYear(437, true));
        Assert.AreEqual(1908, CalendarConverter.ToModernYear(1908, false));
        Assert.AreEqual(437, CalendarConverter.ToEraYear(-437, true));
        Assert.AreEqual(1908, CalendarConverter.ToEraYear(1908, false));
    }

    [Test]
    public void FormatModern_RendersBCEAndCE()
    {
        Assert.AreEqual("437 BCE", CalendarConverter.FormatModern(-437));
        Assert.AreEqual("1908 CE", CalendarConverter.FormatModern(1908));
    }

    [Test]
    public void SpanCheck_IsInclusive_AndSeparatesEras()
    {
        // Classical Greece span: -450..-350 (BCE counts backward).
        Assert.IsTrue(CalendarConverter.WithinSpan(-437, -450, -350), "437 BCE is within 450-350 BCE");
        Assert.IsTrue(CalendarConverter.WithinSpan(-450, -450, -350), "start is inclusive");
        Assert.IsTrue(CalendarConverter.WithinSpan(-350, -450, -350), "end is inclusive");
        Assert.IsFalse(CalendarConverter.WithinSpan(1908, -450, -350), "a modern year is not classical");
        Assert.IsFalse(CalendarConverter.WithinSpan(-500, -450, -350), "earlier than the span");
    }

    [Test]
    public void FormatEra_RoundTrips_YearAndNativeMonth()
    {
        Assert.AreEqual("21 Elaphebolion 437 BCE", CalendarConverter.FormatEra(-437, true, AtticMonths, 21, 8));
        Assert.AreEqual("3 Mar 1908", CalendarConverter.FormatEra(1908, false, null, 3, 2));
        Assert.AreEqual("year 1878", CalendarConverter.FormatEra(1878, false, null));
    }

    [Test]
    public void Garbage_And_ForgedMarkers_DoNotParse()
    {
        Assert.IsFalse(CalendarConverter.TryParse("", AtticMonths, out _));
        Assert.IsFalse(CalendarConverter.TryParse("not a date", AtticMonths, out _));
        Assert.IsFalse(CalendarConverter.TryParse("3 Mar 1908 (?)", AtticMonths, out _), "forged marker suffix breaks the parse");
        Assert.IsFalse(CalendarConverter.TryParse("Mar 1908", AtticMonths, out _), "missing day does not parse");
    }

    [Test]
    public void BareYear_Detects_ModernYearMode()
    {
        Assert.IsTrue(CalendarConverter.IsBareYear("1899"));
        Assert.IsTrue(CalendarConverter.IsBareYear(" -437 "));
        Assert.IsFalse(CalendarConverter.IsBareYear("3 Mar 1908"));
        Assert.IsFalse(CalendarConverter.IsBareYear(""));
    }

    [Test]
    public void CaseInsensitive_MonthMatch()
    {
        Assert.IsTrue(CalendarConverter.TryParse("21 elaphebolion 437", AtticMonths, out CalendarConverter.ParsedDate p));
        Assert.AreEqual(8, p.monthIndex);
    }
}
