using System;

/// <summary>
/// The careful carer at Home (the Home pet spec PS9): the balance
/// simulation (Tools > TimeDesk > Balance) pays every run's bills this way
/// each night, so the figures show what a careful clerk's pet costs; the
/// game starts the bills step with it (no TV) when its default choice is more
/// than the wallet. Food first,
/// then the heating (it needs no electricity), then medicine when the pet is
/// unwell, then the electricity (the corner's light and the TV's power), each
/// while the wallet covers it; the TV only when the pet is bored, the
/// electricity is paid and the wallet keeps the reserve after it; a toy is
/// played with when one is owned. Pure, so the picks are tested headless.
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
        if (left >= Cost(HomeBill.Heating))
        {
            care = care.With(HomeBill.Heating, true);
            left -= Cost(HomeBill.Heating);
        }
        if (needs.Sickness > 0 && left >= Cost(HomeBill.Medicine))
        {
            care = care.With(HomeBill.Medicine, true);
            left -= Cost(HomeBill.Medicine);
        }
        if (left >= Cost(HomeBill.Electricity))
        {
            care = care.With(HomeBill.Electricity, true);
            left -= Cost(HomeBill.Electricity);
        }
        if (needs.Boredom > 0 && care.Offers(HomeBill.Tv) && left - Cost(HomeBill.Tv) >= reserve)
            care = care.With(HomeBill.Tv, true);
        return care;
    }
}
