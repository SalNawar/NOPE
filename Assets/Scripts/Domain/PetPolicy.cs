using System;

/// <summary>
/// The balance simulation's careful carer at Home (the Home pet spec PS9;
/// Tools > TimeDesk > Balance): every run pays the pet's bills this way each
/// night, so the figures show what a careful clerk's pet costs. Food first,
/// then the heating with its electricity, then medicine when the pet is
/// unwell, each while the wallet covers it; the TV only when the pet is bored
/// and the wallet keeps the reserve after it; a toy is played with when one
/// is owned. Pure, so the picks are tested headless.
/// </summary>
public static class PetPolicy
{
    /// <summary>Tonight's care for a pet at <paramref name="needs"/> with <paramref name="money"/> in the wallet, the bills at <paramref name="price"/>, keeping <paramref name="reserve"/> for the TV; <paramref name="hasToy"/> plays.</summary>
    public static PetCare Care(PetNeeds needs, int money, Func<HomeBill, int> price, int reserve, bool hasToy)
    {
        int Cost(HomeBill bill) => Math.Max(0, price != null ? price(bill) : 0);

        var care = new PetCare(false, false, false, false, false, hasToy);
        int left = money;
        if (left >= Cost(HomeBill.Food))
        {
            care = care.With(HomeBill.Food, true);
            left -= Cost(HomeBill.Food);
        }
        int warmth = Cost(HomeBill.Heating) + Cost(HomeBill.Electricity);
        if (left >= warmth)
        {
            care = care.With(HomeBill.Heating, true).With(HomeBill.Electricity, true);
            left -= warmth;
        }
        if (needs.Sickness > 0 && left >= Cost(HomeBill.Medicine))
        {
            care = care.With(HomeBill.Medicine, true);
            left -= Cost(HomeBill.Medicine);
        }
        if (needs.Boredom > 0 && care.Electricity && left - Cost(HomeBill.Tv) >= reserve)
            care = care.With(HomeBill.Tv, true);
        return care;
    }
}
