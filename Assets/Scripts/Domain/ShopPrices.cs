using System;

/// <summary>
/// The Home shop's price rule (audit R2-006, R4-013), pure so it is tested
/// headless and so the price the shop shows and the price a purchase charges
/// come from one place. The discount percent itself (the active shop-discount
/// effects, capped) is TimelineEffects.GetShopDiscountPercent.
/// </summary>
public static class ShopPrices
{
    /// <summary>
    /// What an upgrade listed at <paramref name="cost"/> costs with
    /// <paramref name="discountPercent"/> off: cost × (1 − percent / 100) in
    /// float, rounded to the nearest credit, half to even (Mathf.RoundToInt's
    /// rounding: 2.5 → 2, 3.5 → 4).
    /// </summary>
    public static int Discounted(int cost, float discountPercent)
    {
        float scaled = cost * (1f - discountPercent / 100f);
        return (int)Math.Round(scaled);
    }
}
