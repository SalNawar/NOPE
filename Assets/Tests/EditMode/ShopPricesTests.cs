using NUnit.Framework;

/// <summary>
/// The Home shop's price rule (audit R2-006, R4-013): the listed cost less the
/// discount percent, rounded to the nearest credit, half to even (as
/// Mathf.RoundToInt did at both call sites). One rule, so the price the shop
/// shows is the price the purchase charges.
/// </summary>
public class ShopPricesTests
{
    [TestCase(100, 0f, 100, Description = "no discount")]
    [TestCase(100, 25f, 75)]
    [TestCase(150, 10f, 135)]
    [TestCase(300, 33f, 201, Description = "200.99998 in float rounds up")]
    [TestCase(250, 90f, 25, Description = "the effects' 90% cap")]
    [TestCase(99, 15f, 84)]
    [TestCase(5, 50f, 2, Description = "2.5: half to even")]
    [TestCase(7, 50f, 4, Description = "3.5: half to even")]
    [TestCase(45, 10f, 40, Description = "40.5: half to even")]
    [TestCase(0, 50f, 0)]
    public void Discounted(int cost, float percent, int expected)
    {
        Assert.AreEqual(expected, ShopPrices.Discounted(cost, percent));
    }
}
