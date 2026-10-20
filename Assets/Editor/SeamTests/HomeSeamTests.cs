using NUnit.Framework;
using UnityEngine;

/// <summary>
/// HOME's P0-A seam NightRules (moved out of HomeManager; RUN-SAFE plays the
/// default-care night through it): the starting care is the default care when
/// the wallet covers it and what the wallet covers otherwise; the pay falls
/// back to what the wallet covers; DefaultNight is the starting care, its
/// bills paid and the pet's night on it. Tests in the "NeedsEditor" category
/// read the run's content library and game config (Resources/RunConfig) and
/// run only inside Unity.
/// </summary>
public class HomeSeamTests
{
    [Test]
    public void WithoutALibrary_EveryBillIsFree_TheDefaultCareIsPaidAtNoCost()
    {
        WorldState world = World(25);
        PetCare care = NightRules.StartingCare(world, null);
        Assert.AreEqual(PetRules.DefaultCare(world.pet.Needs), care);
        Assert.AreEqual(0, HomeEconomy.BillsTotal(world, null, NightRules.AffordableCare(world, null)));
        Assert.AreEqual(0, NightRules.PayBills(world, null, ref care));
        Assert.AreEqual(PetRules.DefaultCare(world.pet.Needs), care, "a covered care is paid as chosen");
        Assert.AreEqual(0, NightRules.DefaultNight(world, null, null, 7), "no config: no pet night");
        Assert.AreEqual(25, world.money);
        Assert.AreEqual(2, world.pet.hunger, "no config: the pet is untouched");
    }

    [Test, Category("NeedsEditor")]
    public void StartingCare_IsTheDefaultCare_WhenTheWalletCoversIt()
    {
        Load(out ContentLibrarySO lib, out _);
        WorldState world = World(100000);
        Assert.AreEqual(PetRules.DefaultCare(world.pet.Needs), NightRules.StartingCare(world, lib));
    }

    [Test, Category("NeedsEditor")]
    public void StartingCare_IsWhatTheWalletCovers_WhenTheDefaultIsMore()
    {
        Load(out ContentLibrarySO lib, out _);
        WorldState world = World(0);
        Assert.Greater(HomeEconomy.BillsTotal(world, lib, PetRules.DefaultCare(world.pet.Needs)), 0, "the default care costs something");
        PetCare care = NightRules.StartingCare(world, lib);
        Assert.AreEqual(NightRules.AffordableCare(world, lib), care);
        Assert.LessOrEqual(HomeEconomy.BillsTotal(world, lib, care), 0);
    }

    [Test, Category("NeedsEditor")]
    public void AffordableCare_NeverCostsMoreThanTheWallet()
    {
        Load(out ContentLibrarySO lib, out _);
        foreach (int money in new[] { 0, 1, 5, 12, 30, 80, 100000 })
        {
            WorldState world = World(money);
            Assert.LessOrEqual(HomeEconomy.BillsTotal(world, lib, NightRules.AffordableCare(world, lib)), money, $"{money} cr");
        }
    }

    [Test, Category("NeedsEditor")]
    public void PayBills_PaysACoveredCare_AndFallsBackToWhatTheWalletCovers()
    {
        Load(out ContentLibrarySO lib, out _);
        var everything = new PetCare(true, true, true, true, true);

        WorldState rich = World(100000);
        PetCare care = everything;
        int total = HomeEconomy.BillsTotal(rich, lib, everything);
        Assert.AreEqual(total, NightRules.PayBills(rich, lib, ref care));
        Assert.AreEqual(everything, care);
        Assert.AreEqual(100000 - total, rich.money);

        WorldState poor = World(1);
        Assert.Greater(HomeEconomy.BillsTotal(poor, lib, everything), 1, "every bill is more than 1 cr");
        PetCare covered = NightRules.AffordableCare(poor, lib);
        care = everything;
        int paid = NightRules.PayBills(poor, lib, ref care);
        Assert.AreEqual(covered, care);
        Assert.AreEqual(HomeEconomy.BillsTotal(poor, lib, covered), paid);
        Assert.AreEqual(1 - paid, poor.money);
        Assert.GreaterOrEqual(poor.money, 0);
    }

    [Test, Category("NeedsEditor")]
    public void DefaultNight_IsTheStartingCare_ItsBillsPaid_AndThePetsNight()
    {
        Load(out ContentLibrarySO lib, out GameConfigSO config);
        foreach (int money in new[] { 0, 6, 100000 })
        {
            WorldState once = World(money), steps = World(money);
            int paid = NightRules.DefaultNight(once, lib, config, 7);

            PetCare care = NightRules.StartingCare(steps, lib);
            int stepsPaid = NightRules.PayBills(steps, lib, ref care);
            HomeEconomy.PetNight(steps, lib, config, 7, care);

            Assert.AreEqual(stepsPaid, paid, $"{money} cr");
            Assert.AreEqual(money - paid, once.money, $"{money} cr");
            Assert.AreEqual(steps.pet.Needs, once.pet.Needs, $"{money} cr");
            Assert.AreEqual(steps.pet.welfareNights, once.pet.welfareNights, $"{money} cr");
            Assert.AreEqual(steps.pet.lastChange, once.pet.lastChange, $"{money} cr");
        }
    }

    /// <summary>A world with <paramref name="money"/> cr and an adopted pet that is hungry, cold, a little bored and unwell.</summary>
    private static WorldState World(int money)
    {
        var world = new WorldState { money = money };
        world.pet = new PetState { name = "Seam", hunger = 2, cold = 2, boredom = 1, sickness = 1 };
        return world;
    }

    /// <summary>The run's content library and game config (Resources/RunConfig, as BalanceSimulation loads them).</summary>
    private static void Load(out ContentLibrarySO lib, out GameConfigSO config)
    {
        var run = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        Assert.IsNotNull(run, "Resources/RunConfig");
        lib = run.contentLibrary;
        config = run.gameConfig;
        Assert.IsNotNull(lib, "RunConfig's content library");
        Assert.IsNotNull(config, "RunConfig's game config");
    }
}
