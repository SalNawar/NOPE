using System.Collections.Generic;
using NUnit.Framework;

/// <summary>The pet's rules (the Home pet spec PS3-PS6): tonight's care settles the needs, the night's rolls move its health, neglect brings the Welfare Office, and the needs are words, never numbers.</summary>
public class PetRulesTests
{
    private const int Max = 3;
    private static readonly PetCare Nothing = new PetCare(false, false, false, false, false);
    private static readonly PetCare Essentials = new PetCare(true, true, true, false, false);

    [Test]
    public void Settle_AMetNeedFallsToWell_AnUnmetOneRisesAStep()
    {
        PetNeeds n = PetRules.Settle(new PetNeeds(2, 2, 1, 0), Essentials, Max);
        Assert.AreEqual(0, n.Hunger, "fed");
        Assert.AreEqual(0, n.Cold, "warm");
        Assert.AreEqual(2, n.Boredom, "no TV, no toy: a step more bored");
        n = PetRules.Settle(new PetNeeds(0, 0, 0, 0), Nothing, Max);
        Assert.AreEqual((1, 1, 1, 0), (n.Hunger, n.Cold, n.Boredom, n.Sickness));
    }

    [Test]
    public void Settle_NeverPastTheWorst()
    {
        PetNeeds n = PetRules.Settle(new PetNeeds(3, 3, 3, 3), Nothing, Max);
        Assert.AreEqual((3, 3, 3, 3), (n.Hunger, n.Cold, n.Boredom, n.Sickness));
    }

    [Test]
    public void Settle_TheHeatingStandsAlone_TheTvNeedsTheElectricity()
    {
        PetNeeds n = PetRules.Settle(new PetNeeds(0, 1, 2, 0), new PetCare(true, true, false, true, false), Max);
        Assert.AreEqual(0, n.Cold, "the heating warms without electricity (gas or district heat)");
        Assert.AreEqual(3, n.Boredom, "a TV without power is no company");
        n = PetRules.Settle(new PetNeeds(0, 1, 2, 0), new PetCare(true, true, true, true, false), Max);
        Assert.AreEqual(0, n.Cold);
        Assert.AreEqual(1, n.Boredom);
    }

    [Test]
    public void Settle_TheTvAndAToy_EachEaseBoredomAStep()
    {
        Assert.AreEqual(0, PetRules.Settle(new PetNeeds(0, 0, 2, 0), new PetCare(true, true, true, true, false, played: true), Max).Boredom);
        Assert.AreEqual(1, PetRules.Settle(new PetNeeds(0, 0, 2, 0), Essentials.WithPlay(true), Max).Boredom);
    }

    [Test]
    public void Settle_Medicine_TreatsAStep()
    {
        Assert.AreEqual(1, PetRules.Settle(new PetNeeds(0, 0, 0, 2), Essentials.With(HomeBill.Medicine, true), Max).Sickness);
        Assert.AreEqual(0, PetRules.Settle(new PetNeeds(0, 0, 0, 0), Essentials.With(HomeBill.Medicine, true), Max).Sickness, "never below well");
    }

    [Test]
    public void Toggle_PaysOnlyTheBillClicked_NeverTheElectricity()
    {
        // Saleh 2026-10-07: "when you pay for heating you auto pay for electricity too" was a bug: no hidden auto-pay.
        PetCare care = Nothing.Toggle(HomeBill.Heating);
        Assert.IsTrue(care.Heating && care.Warm, "the heating alone warms");
        Assert.IsFalse(care.Electricity, "paying the heating pays nothing else");
        Assert.AreEqual(care, care.Toggle(HomeBill.Tv), "no power: the TV cannot be chosen, and the electricity is not paid for it");
        Assert.IsFalse(care.Offers(HomeBill.Tv));
        care = care.Toggle(HomeBill.Electricity).Toggle(HomeBill.Tv);
        Assert.IsTrue(care.Electricity && care.Watched && care.Offers(HomeBill.Tv));
        care = care.Toggle(HomeBill.Electricity);
        Assert.IsFalse(care.Electricity || care.Tv, "skipping the electricity moves the TV to Skip");
        Assert.IsTrue(care.Heating, "and keeps the heating");
        Assert.IsTrue(Essentials.Toggle(HomeBill.Heating).Electricity, "skipping the heating keeps the electricity");
        Assert.IsTrue(Nothing.Toggle(HomeBill.Food).Food);
        Assert.IsFalse(Nothing.Toggle(HomeBill.Food).Toggle(HomeBill.Food).Food);
    }

    [Test]
    public void SicknessChance_RisesWithHungerAndCold_FallsWithMood_NeverBelowZero()
    {
        float well = PetRules.SicknessChance(0.15f, new PetNeeds(0, 0, 0, 0), 0.1f, 0f, 0f, 0.01f, 0.1f);
        float neglected = PetRules.SicknessChance(0.15f, new PetNeeds(2, 1, 0, 0), 0.1f, 0f, 0f, 0.01f, 0.1f);
        float cheerful = PetRules.SicknessChance(0.15f, new PetNeeds(0, 0, 0, 0), 0.1f, 0f, 5f, 0.01f, 0.1f);
        float bored = PetRules.SicknessChance(0.15f, new PetNeeds(0, 0, 3, 0), 0.1f, 0f, 3f, 0.01f, 0.1f);
        Assert.AreEqual(0.15f, well, 1e-5);
        Assert.AreEqual(0.45f, neglected, 1e-5, "three steps of want at 0.1 each");
        Assert.AreEqual(0.10f, cheerful, 1e-5, "mood 5 at 0.01 a point");
        Assert.AreEqual(0.15f, bored, 1e-5, "boredom eats the mood");
        Assert.AreEqual(0f, PetRules.SicknessChance(0.05f, new PetNeeds(0, 0, 0, 0), 0.1f, -0.2f, 0f, 0.01f, 0.1f));
    }

    [Test]
    public void RecoveryChance_TheMoodLessBoredom()
    {
        Assert.AreEqual(0.12f, PetRules.RecoveryChance(new PetNeeds(0, 0, 1, 1), 5f, 0.03f, 0.4f), 1e-5);
        Assert.AreEqual(0f, PetRules.RecoveryChance(new PetNeeds(0, 0, 3, 1), 2f, 0.03f, 0.4f));
    }

    [TestCase(1, false, true, false, 2)]
    [TestCase(1, true, true, false, 1, Description = "medicine tonight: no worse tonight")]
    [TestCase(2, false, false, true, 1)]
    [TestCase(3, false, true, false, 3, Description = "never past the worst")]
    [TestCase(0, false, false, true, 0)]
    public void Sickness_TheNightsRolls(int settled, bool medicine, bool worsens, bool recovers, int expected)
    {
        Assert.AreEqual(expected, PetRules.Sickness(settled, medicine, worsens, recovers, Max));
    }

    [Test]
    public void Welfare_TwoNightsAtTheWorst_TakeThePet()
    {
        Assert.IsTrue(PetRules.Neglected(new PetNeeds(3, 0, 0, 0), Max));
        Assert.IsTrue(PetRules.Neglected(new PetNeeds(0, 3, 0, 0), Max));
        Assert.IsTrue(PetRules.Neglected(new PetNeeds(0, 0, 0, 3), Max));
        Assert.IsFalse(PetRules.Neglected(new PetNeeds(2, 2, 3, 2), Max), "boredom alone is no neglect");

        int nights = PetRules.WelfareNights(0, true);
        Assert.AreEqual(1, nights);
        Assert.IsFalse(PetRules.Taken(nights, 2), "the first night brings a notice");
        nights = PetRules.WelfareNights(nights, true);
        Assert.IsTrue(PetRules.Taken(nights, 2));
        Assert.AreEqual(0, PetRules.WelfareNights(1, false), "a night of care clears it");
        Assert.IsFalse(PetRules.Taken(9, 0), "0: never");
    }

    [Test]
    public void Look_SickFirst_ThenSad_ThenHappy()
    {
        Assert.AreEqual(PetLook.Sick, PetRules.Look(new PetNeeds(0, 0, 0, 1)));
        Assert.AreEqual(PetLook.Sad, PetRules.Look(new PetNeeds(2, 0, 0, 0)));
        Assert.AreEqual(PetLook.Sad, PetRules.Look(new PetNeeds(0, 0, 2, 0)));
        Assert.AreEqual(PetLook.Happy, PetRules.Look(new PetNeeds(0, 0, 0, 0)));
        Assert.AreEqual(PetLook.Idle, PetRules.Look(new PetNeeds(1, 0, 1, 0)));
    }

    [Test]
    public void DefaultCare_TheEssentials_AndMedicineWhenUnwell()
    {
        PetCare care = PetRules.DefaultCare(new PetNeeds(0, 0, 3, 0));
        Assert.IsTrue(care.Food && care.Heating && care.Electricity);
        Assert.IsFalse(care.Tv || care.Medicine);
        Assert.IsTrue(PetRules.DefaultCare(new PetNeeds(0, 0, 0, 1)).Medicine);
    }

    [Test]
    public void Total_ThePaidBills()
    {
        var prices = new Dictionary<HomeBill, int> { [HomeBill.Food] = 10, [HomeBill.Heating] = 8, [HomeBill.Electricity] = 6, [HomeBill.Tv] = 4, [HomeBill.Medicine] = 12 };
        Assert.AreEqual(24, PetRules.Total(Essentials, b => prices[b]));
        Assert.AreEqual(0, PetRules.Total(Nothing, b => prices[b]));
        Assert.AreEqual(40, PetRules.Total(new PetCare(true, true, true, true, true), b => prices[b]));
    }

    [TestCase(0, 3, 4, 0)]
    [TestCase(1, 3, 4, 1)]
    [TestCase(3, 3, 4, 3)]
    [TestCase(1, 6, 4, 1, Description = "a first step is never the best word")]
    [TestCase(9, 3, 4, 3)]
    [TestCase(2, 3, 1, 0)]
    public void Band_LevelToWord(int level, int max, int count, int expected)
    {
        Assert.AreEqual(expected, PetRules.Band(level, max, count));
    }

    [Test]
    public void Policy_TheCarefulCarer()
    {
        var prices = new Dictionary<HomeBill, int> { [HomeBill.Food] = 10, [HomeBill.Heating] = 8, [HomeBill.Electricity] = 6, [HomeBill.Tv] = 4, [HomeBill.Medicine] = 12 };
        PetCare rich = PetPolicy.Care(new PetNeeds(0, 0, 1, 1), 200, b => prices[b], 60, true);
        Assert.IsTrue(rich.Food && rich.Warm && rich.Medicine && rich.Watched && rich.Played);
        PetCare poor = PetPolicy.Care(new PetNeeds(0, 0, 1, 1), 20, b => prices[b], 60, false);
        Assert.IsTrue(poor.Food && poor.Heating && poor.Warm, "the heating's 8 alone warms");
        Assert.IsFalse(poor.Medicine, "2 left: not the medicine's 12");
        Assert.IsFalse(poor.Electricity || poor.Tv, "nor the electricity's 6, so no TV");
        PetCare sick = PetPolicy.Care(new PetNeeds(0, 0, 0, 1), 32, b => prices[b], 60, false);
        Assert.IsTrue(sick.Food && sick.Heating && sick.Medicine && !sick.Electricity, "medicine before the electricity");
        Assert.IsFalse(PetPolicy.Care(new PetNeeds(0, 0, 0, 0), 200, b => prices[b], 60, false).Tv, "not bored: no TV");
        Assert.IsFalse(PetPolicy.Care(new PetNeeds(0, 0, 2, 0), 85, b => prices[b], 60, false).Tv, "the TV keeps the reserve");
    }
}
