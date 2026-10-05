using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Drives the Home phase (Phase 4; the Home pet spec): on scene load, the
/// night's break-in and the fixed bill (DayCycle.OpenHome), then walks the
/// player through Bills -> the pet's corner -> House -> Slot Machine -> Sleep.
/// The bills step offers the night's optional bills (food, heating,
/// electricity, TV, medicine; each paid or skipped, electricity powering the
/// heating and the TV) with the pet's needs in words; the corner shows the pet
/// as tonight's care leaves it, to pet and to play with an owned toy (once a
/// night). The House shows only the Home upgrades (UpgradeSO.venue Home: the
/// house upgrades' tree; the office's moved to the PC's Orders app, Saleh
/// 2026-09-29) and is skipped while none is stocked; with the radio owned
/// the sleep panel plays the night's radio line (ContentLibrarySO.Home).
/// Sleep settles the pet's night (HomeEconomy.PetNight) and hands off to
/// RunManager.Sleep() (day-boundary endings, the Welfare Office's among them,
/// else nightly resolve, day++ and the Orders app's deliveries, back to
/// Office). All HomeUIController panels are optional; unwired panels are
/// skipped.
/// </summary>
public sealed class HomeManager : MonoBehaviour
{
    /// <summary>UI controller for the Home panels.</summary>
    [SerializeField] private HomeUIController homeUI;

    /// <summary>Active run's world state.</summary>
    private WorldState _world;

    /// <summary>Content library (upgrades, slot outcomes, effects, the bills and the pet's words).</summary>
    private ContentLibrarySO _lib;

    /// <summary>Gameplay tuning (expenses, the pet's knobs, slot cost).</summary>
    private GameConfigSO _config;

    /// <summary>The run's day seed (the night's rolls).</summary>
    private int _daySeed;

    /// <summary>Tonight's fixed bill (rent and utilities, the sick pet's extra care, the house's upkeep, the break-in), paid when Home opened.</summary>
    private HomeEconomy.ExpenseReport _bill;

    /// <summary>Tonight's care: the bills chosen (paid at the bills step) and whether a toy was played with.</summary>
    private PetCare _care;

    /// <summary>True once tonight's bills are paid (the bills step is behind).</summary>
    private bool _paid;

    /// <summary>The pats tonight (the reactions' turn).</summary>
    private int _pats;

    /// <summary>Tonight's slot spins, drawn in turn from the run's own stream for the day (Seeds.ForSlot): a run replays, and Continue (Home again from the save made before it) cannot reroll a spin.</summary>
    private IRandomSource _slotRandom;

    /// <summary>Tonight's household costs so far (the fixed bill and the night's bills), for the clerk's statement.</summary>
    private int _household;

    /// <summary>Tonight's purchases at Home so far (Home upgrades and slot spins), for the clerk's statement (the shift's orders are added by ClerkAccountSource.RecordHome).</summary>
    private int _purchases;

    /// <summary>The pet's words (never null).</summary>
    private PetContent Words => _lib != null ? _lib.Home.pet : new PetContent();

    /// <summary>The run's pet (never null).</summary>
    private PetState Pet => _world.pet ?? (_world.pet = new PetState());

    /// <summary>Acquires the run, bills the fixed costs, and starts the panel flow.</summary>
    private void Start()
    {
        Debug.Log("[HomeManager] >>> Entering Start.");

        RunManager run = RunManager.GetOrCreate();

        if (run == null)
        {
            Debug.LogError("HomeManager could not acquire a RunManager (missing Resources/RunConfig).");
            return;
        }

        _world = run.World;
        _lib = run.Library;
        _config = run.Config != null ? run.Config.gameConfig : null;
        _daySeed = run.GetDaySeed();
        if (!Pet.Adopted)
            DayCycle.Adopt(_world, _lib, run.Config.startingPetKind, null);

        Debug.Log($"[HomeManager] Day {_world.day} home phase starting: money={_world.money}, stability={_world.timelineStability:0.00}, pet='{Pet.name}' ({Pet.kind}).");

        // The break-in and the fixed bill, deterministically seeded by the day so it's stable on reload.
        _bill = DayCycle.OpenHome(_world, _lib, _config, _daySeed);
        _care = PetRules.DefaultCare(Pet.Needs);
        _slotRandom = new SeededRandom(Seeds.ForSlot(_daySeed));
        _household = _bill.total;
        RecordStatement();

        RefreshHud();

        Debug.Log("[HomeManager] <<< Exiting Start (showing the bills).");

        ShowBills();
    }

    // ---------------------------------------------------------------
    // Step 1: the bills
    // ---------------------------------------------------------------

    /// <summary>Step 1: the fixed costs paid, the pet's needs, and the night's bills to pay or skip.</summary>
    private void ShowBills()
    {
        if (homeUI == null || !homeUI.HasExpensesPanel)
        {
            Debug.Log("[HomeManager] ShowBills: no bills panel, paying the essentials it can and moving on.");
            HandlePay();
            return;
        }

        int total = HomeEconomy.BillsTotal(_world, _lib, _care);
        bool canPay = total == 0 || _world.money >= total;
        var rows = new List<HomeUIController.BillView>();
        foreach (HomeBill bill in (HomeBill[])System.Enum.GetValues(typeof(HomeBill)))
            rows.Add(BillRow(bill));

        homeUI.ShowExpenses(UiText.Format("home.title", _world.day), BillsText(total), rows, UiText.Format("home.pay", Pet.name), canPay, HandleToggleBill, HandlePay);
    }

    /// <summary>A bill's row: its name, price and line (and "needs electricity" for the heating and the TV); Paying or Skip; the medicine offered only while the pet is unwell.</summary>
    private HomeUIController.BillView BillRow(HomeBill bill)
    {
        BillRow row = _lib != null ? _lib.Home.Bill(bill) : null;
        string name = row != null ? row.name : bill.ToString();
        string label = $"{name}  {HomeEconomy.BillPrice(_world, _lib, bill)} {UiText.Currency(UiText.WalletForm.Short)}";
        bool needless = bill == HomeBill.Medicine && Pet.sickness <= 0;
        string note = needless ? UiText.Format("home.bill.notNeeded", Pet.name)
            : bill == HomeBill.Heating || bill == HomeBill.Tv ? UiText.Get("home.bill.needsPower")
            : row != null ? row.line : string.Empty;
        if (!string.IsNullOrEmpty(note))
            label += "  ·  " + note;
        return new HomeUIController.BillView(bill, label, UiText.Get(_care.Pays(bill) ? "home.bill.paying" : "home.bill.skip"), !needless);
    }

    /// <summary>The bills panel's body: the break-in, the fixed costs paid and the wallet, the pet's needs now in words, last night's change and the Welfare Office's notice, then the night's bills' total.</summary>
    private string BillsText(int total)
    {
        var sb = new StringBuilder();
        string cr = UiText.Currency(UiText.WalletForm.Inline);
        if (_bill.breakInLoss > 0)
            sb.AppendLine(UiText.Format("home.breakIn", _bill.breakInLoss));
        sb.AppendLine(UiText.Get("home.fixed"));
        sb.AppendLine(UiText.Format("home.rent", _bill.baseAmount));
        if (_bill.conditionAmount > 0)
            sb.AppendLine(UiText.Format("home.drain", Pet.name, _bill.conditionAmount));
        if (_bill.upkeepAmount > 0)
            sb.AppendLine(UiText.Format("home.upkeep", _bill.upkeepAmount));
        sb.AppendLine(UiText.Format("home.fixedTotal", _bill.total, cr, _world.money));
        if (_world.money < 0)
            sb.AppendLine(UiText.Get("home.debt"));

        sb.AppendLine();
        PetContent words = Words;
        if (Pet.lastChange > 0)
            sb.AppendLine(words.Fill(words.worse, Pet));
        else if (Pet.lastChange < 0)
            sb.AppendLine(words.Fill(words.better, Pet));
        sb.AppendLine(NeedsText(Pet.Needs, " "));
        if (Pet.welfareNights > 0)
            sb.AppendLine("<b>" + words.Fill(words.welfareNotice, Pet) + "</b>");

        sb.AppendLine();
        sb.Append(_world.money >= total || total == 0
            ? UiText.Format("home.bills.total", total, cr, _world.money - total)
            : UiText.Format("home.bills.short", total, cr, _world.money));
        return sb.ToString();
    }

    /// <summary>The pet's four needs in words (PetContent.Need), joined by <paramref name="separator"/>; never a number.</summary>
    private string NeedsText(PetNeeds needs, string separator)
    {
        PetContent words = Words;
        int max = HomeEconomy.NeedMax(_config);
        var lines = new List<string>();
        foreach (PetNeed need in (PetNeed[])System.Enum.GetValues(typeof(PetNeed)))
        {
            string line = words.Need(need, Pet, needs, max);
            if (line.Length > 0)
                lines.Add(line);
        }
        return string.Join(separator, lines);
    }

    /// <summary>A bill's row clicked: paid or skipped (electricity going with the heating and the TV, PetCare.Toggle), then the panel again.</summary>
    private void HandleToggleBill(HomeBill bill)
    {
        if (_paid || (bill == HomeBill.Medicine && Pet.sickness <= 0))
            return;
        _care = _care.Toggle(bill);
        ShowBills();
    }

    /// <summary>Pay clicked: the chosen bills leave the wallet (HomeEconomy.PayBills; refused when it cannot cover them), then the pet's corner.</summary>
    private void HandlePay()
    {
        if (_paid)
            return;
        int paid = HomeEconomy.PayBills(_world, _lib, _care);
        if (paid < 0)
        {
            // Unwired panels pay what the wallet covers; a wired panel never offers Pay it cannot cover.
            _care = PetPolicy.Care(Pet.Needs, _world.money, bill => HomeEconomy.BillPrice(_world, _lib, bill), int.MaxValue, false);
            paid = Mathf.Max(0, HomeEconomy.PayBills(_world, _lib, _care));
        }
        _paid = true;
        _household += paid;
        RecordStatement();
        RefreshHud();
        Debug.Log($"[HomeManager] Paid tonight's bills: {paid} (food={_care.Food}, heating={_care.Heating}, electricity={_care.Electricity}, tv={_care.Tv}, medicine={_care.Medicine}); money={_world.money}.");
        ShowPet(string.Empty, false);
    }

    // ---------------------------------------------------------------
    // Step 2: the pet's corner
    // ---------------------------------------------------------------

    /// <summary>Step 2: the pet as tonight's care leaves it (PetRules.Settle), to pet and to play with; <paramref name="reaction"/> is what it just did.</summary>
    private void ShowPet(string reaction, bool played)
    {
        if (homeUI == null || !homeUI.HasPetPanel)
        {
            ShowShop();
            return;
        }

        PetContent words = Words;
        PetNeeds tonight = PetRules.Settle(Pet.Needs, _care, HomeEconomy.NeedMax(_config));
        var toys = new List<HomeUIController.ToyView>();
        foreach (UpgradeSO toy in HomeEconomy.OwnedToys(_world, _lib))
        {
            UpgradeSO chosen = toy;
            toys.Add(new HomeUIController.ToyView(toy.displayName, UiText.Get(_care.Played ? "home.pet.played" : "home.pet.play"), !_care.Played, () => HandlePlay(chosen)));
        }
        string body = NeedsText(tonight, "\n") + (_care.Electricity ? string.Empty : "\n" + UiText.Get("home.pet.dark"));
        homeUI.ShowPet(UiText.Format("home.pet.title", Pet.name), body, Pet.kind, PetRules.Look(tonight), _care.Electricity, reaction,
                       UiText.Format("home.pet.pat", Pet.name), HandlePat, toys, UiText.Get("home.pet.noToys"),
                       UiText.Get(ShopNext ? "home.next.house" : "home.next.slots"), ShowShop, played);
    }

    /// <summary>A pat: the kind's next reaction line (PetContent.InTurn), no change to its needs.</summary>
    private string HandlePat()
    {
        PetKindContent kind = Words.Kind(Pet.kind);
        return kind != null ? Words.InTurn(kind.reactions, _world.day + _pats++, Pet) : string.Empty;
    }

    /// <summary>Plays with <paramref name="toy"/> (once a night: boredom a step lower tonight, PetRules.Settle), then the corner again with the kind's toy line.</summary>
    private void HandlePlay(UpgradeSO toy)
    {
        if (_care.Played)
            return;
        _care = _care.WithPlay(true);
        PetKindContent kind = Words.Kind(Pet.kind);
        string line = kind != null ? Words.InTurn(kind.toyLines, _world.day, Pet, toy.displayName) : string.Empty;
        Debug.Log($"[HomeManager] Played with '{toy.id}'.");
        ShowPet(line, true);
    }

    // ---------------------------------------------------------------
    // Step 3: the House
    // ---------------------------------------------------------------

    /// <summary>The upgrades Home sells (venue Home: household improvements), in the library's order.</summary>
    private List<UpgradeSO> HomeUpgrades()
    {
        var upgrades = new List<UpgradeSO>();
        if (_lib != null)
            foreach (UpgradeSO u in _lib.Upgrades)
                if (u != null && u.venue == UpgradeVenue.Home)
                    upgrades.Add(u);
        return upgrades;
    }

    /// <summary>True when the shop step shows (its panel is wired and Home stocks an upgrade).</summary>
    private bool ShopNext => homeUI != null && homeUI.HasShopPanel && HomeUpgrades().Count > 0;

    /// <summary>Step 3: the House (the Home upgrades' tree), skipped while Home stocks none.</summary>
    private void ShowShop()
    {
        Debug.Log("[HomeManager] >>> Entering ShowShop.");

        RefreshHud();

        if (ShopNext)
            homeUI.ShowShop(_world, _lib, HomeUpgrades(), HandleBuyUpgrade, ShowSlot);
        else
        {
            Debug.Log("[HomeManager] ShowShop: no shop panel or no Home upgrade stocked (the office's are in the PC's Orders app), skipping to Slot.");
            ShowSlot();
        }
    }

    /// <summary>Buys a Home upgrade when it is buyable (HomeEconomy.BuyHouseUpgrade: its prerequisites owned, the wallet covering its price): owned at once, its effects from tonight's night; then refreshes the House.</summary>
    private void HandleBuyUpgrade(UpgradeSO upgrade)
    {
        Debug.Log($"[HomeManager] >>> Entering HandleBuyUpgrade (upgrade='{upgrade?.displayName}').");

        int cost = HomeEconomy.BuyHouseUpgrade(_world, _lib, upgrade);
        if (cost < 0)
        {
            Debug.Log("[HomeManager] <<< Exiting HandleBuyUpgrade — not a buyable Home upgrade (owned, locked or too dear).");
            return;
        }

        _purchases += cost;
        RefreshHud();
        RecordStatement();

        // Re-show to refresh rows (costs/owned state) without advancing the flow.
        if (homeUI != null && homeUI.HasShopPanel)
            homeUI.ShowShop(_world, _lib, HomeUpgrades(), HandleBuyUpgrade, ShowSlot);

        Debug.Log($"[HomeManager] <<< Exiting HandleBuyUpgrade (bought '{upgrade.displayName}' for {cost}, money={_world.money}).");
    }

    // ---------------------------------------------------------------
    // Step 4: the slot machine; step 5: sleep
    // ---------------------------------------------------------------

    /// <summary>Step 4: slot machine.</summary>
    private void ShowSlot()
    {
        Debug.Log("[HomeManager] >>> Entering ShowSlot.");

        RefreshHud();

        if (homeUI != null && homeUI.HasSlotPanel)
            homeUI.ShowSlot(_world, _config, HandleSpin, ShowSleep);
        else
        {
            Debug.Log("[HomeManager] ShowSlot: no slot panel, skipping to Sleep.");
            ShowSleep();
        }
    }

    /// <summary>
    /// Spends the spin cost (if affordable), picks a weighted SlotOutcomeSO
    /// from tonight's seeded stream, and applies its money/modifier/effect
    /// results. Returns the line shown to the player.
    /// </summary>
    private string HandleSpin()
    {
        Debug.Log($"[HomeManager] >>> Entering HandleSpin (money={_world.money}).");

        int spinCost = _config != null ? _config.slotSpinCost : 0;

        if (_world.money < spinCost)
        {
            Debug.Log($"[HomeManager] <<< Exiting HandleSpin — not enough credits ({_world.money} < {spinCost}).");
            return $"Not enough {UiText.Currency(UiText.WalletForm.Inline)} to spin.";
        }

        IReadOnlyList<SlotOutcomeSO> outcomes = _lib != null ? _lib.SlotOutcomes : System.Array.Empty<SlotOutcomeSO>();

        if (outcomes.Count == 0)
        {
            Debug.Log("[HomeManager] <<< Exiting HandleSpin — no slot outcomes configured.");
            return "The slot machine is out of order.";
        }

        _world.money -= spinCost;
        _purchases += spinCost;

        SlotOutcomeSO outcome = WeightedRandom.Pick(outcomes, o => o != null ? o.weight : 0f, _slotRandom);

        if (outcome == null)
        {
            RefreshHud();
            RecordStatement();
            Debug.Log("[HomeManager] <<< Exiting HandleSpin — no outcome picked (nothing happens).");
            return "...nothing happens.";
        }

        _world.money += outcome.moneyDelta;
        _world.legendaryChanceBonus += outcome.legendaryChanceBonus;
        _world.forgeryChanceModifier += outcome.forgeryChanceModifier;
        _world.payRateMultiplier += outcome.payRateMultiplierDelta;

        if (outcome.effect != null)
        {
            int duration = outcome.durationDaysOverride != 0
                ? outcome.durationDaysOverride
                : outcome.effect.defaultDurationDays;

            TimelineService.ActivateEffect(
                _world, outcome.effect, $"Slot: {outcome.displayName}",
                _world.day, duration, applyInstantOps: true);
        }

        RefreshHud();
        RecordStatement();

        string line = !string.IsNullOrEmpty(outcome.resultLine) ? outcome.resultLine : outcome.displayName;

        Debug.Log($"[HomeManager] <<< Exiting HandleSpin (outcome='{outcome.displayName}', spinCost={spinCost}, moneyDelta={outcome.moneyDelta}, money={_world.money}).");

        return outcome.moneyDelta != 0
            ? $"{line} ({outcome.moneyDelta:+0;-0} {UiText.Currency(UiText.WalletForm.Inline)})"
            : line;
    }

    /// <summary>Writes tonight's household costs, purchases and the wallet into the day's row of the clerk's statement (redesign phase 25; saved when Sleep saves).</summary>
    private void RecordStatement() =>
        ClerkAccountSource.RecordHome(_world, _household, _purchases, _lib, _config);

    /// <summary>Step 5: sleep prompt.</summary>
    private void ShowSleep()
    {
        Debug.Log("[HomeManager] >>> Entering ShowSleep.");

        RefreshHud();

        if (homeUI != null && homeUI.HasSleepPanel)
            homeUI.ShowSleep(_world, RadioLine(), HandleSleep);
        else
        {
            Debug.Log("[HomeManager] ShowSleep: no sleep panel, sleeping immediately.");
            HandleSleep();
        }
    }

    /// <summary>Tonight's radio line when the house owns the radio (ContentLibrarySO.Home: HomeContent.RadioLine by the day), else "".</summary>
    private string RadioLine()
    {
        HomeContent home = _lib != null ? _lib.Home : null;
        return home != null && _world.HasUpgrade(home.radioUpgrade) ? home.RadioLine(_world.day) : string.Empty;
    }

    /// <summary>
    /// Ends the day: the pet's night on tonight's care (HomeEconomy.PetNight:
    /// its needs, the sickness and recovery rolls, the Welfare Office), then
    /// RunManager.Sleep: the day-boundary ending check (failures, the pet
    /// taken among them, and the run's last day), else the nightly resolve,
    /// day++, save, back to Office.
    /// </summary>
    private void HandleSleep()
    {
        Debug.Log($"[HomeManager] HandleSleep (day {_world?.day}): the pet's night, then RunManager.Sleep.");

        if (!RunManager.HasInstance)
        {
            Debug.LogError("HomeManager.HandleSleep called with no RunManager instance.");
            return;
        }

        HomeEconomy.PetNight(_world, _lib, _config, _daySeed, _care);
        RunManager.Instance.Sleep();
    }

    /// <summary>Refreshes the HUD when the UI is wired (Unity's own null check, never ?. on a serialized reference: audit R2-013).</summary>
    private void RefreshHud()
    {
        if (homeUI != null)
            homeUI.UpdateHud(_world);
    }
}
