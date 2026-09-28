using NUnit.Framework;

/// <summary>The Directives' rules over the rule types: closures, a rule's first day, and which rules guarantee a faulty traveller today.</summary>
public class DirectivesTests
{
    [TestCase(TravelRuleType.EraForbidden, true)]
    [TestCase(TravelRuleType.NationForbidden, true)]
    [TestCase(TravelRuleType.NationEraForbidden, true)]
    [TestCase(TravelRuleType.DressForDestination, false)]
    [TestCase(TravelRuleType.Procedure, false)]
    [TestCase(TravelRuleType.ReturnHome, false)]
    public void IsClosure_TheThreeForbiddenTypes(TravelRuleType type, bool expected)
    {
        Assert.AreEqual(expected, Directives.IsClosure(type));
    }

    [Test]
    public void FirstDay_TheSmallestDayListed_ZeroForNone()
    {
        Assert.AreEqual(0, Directives.FirstDay(null));
        Assert.AreEqual(0, Directives.FirstDay(new int[0]));
        Assert.AreEqual(5, Directives.FirstDay(new[] { 5, 6 }));
        Assert.AreEqual(5, Directives.FirstDay(new[] { 6, 5, 7 }), "unordered plans");
    }

    /// <summary>P4: a closure guarantees a violator every day; the displaced's return home guarantees a liar on its first day only; procedures never.</summary>
    [TestCase(TravelRuleType.EraForbidden, 3, 2, true)]
    [TestCase(TravelRuleType.NationForbidden, 2, 2, true)]
    [TestCase(TravelRuleType.NationEraForbidden, 6, 2, true)]
    [TestCase(TravelRuleType.ReturnHome, 5, 5, true)]
    [TestCase(TravelRuleType.ReturnHome, 6, 5, false)]
    [TestCase(TravelRuleType.ReturnHome, 5, 0, false, Description = "a rule no plan lists guarantees nothing")]
    [TestCase(TravelRuleType.DressForDestination, 2, 2, false)]
    [TestCase(TravelRuleType.Procedure, 1, 1, false)]
    public void Guarantees_ClosuresEveryDay_ReturnHomeOnItsFirstDay(TravelRuleType type, int today, int firstDay, bool expected)
    {
        Assert.AreEqual(expected, Directives.Guarantees(type, today, firstDay));
    }
}
